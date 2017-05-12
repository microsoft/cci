// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

using Microsoft.DiaSymReader.PortablePdb;
using Microsoft.DiaSymReader.Tools;

namespace Microsoft.Cci.Pdb {
  /// <summary>
  /// Portable PDB provider.
  /// </summary>
  internal static class PdbFormatProvider {

    /// <summary>
    /// Detect whether a given stream contains portable or Windows PDB format data
    /// and deserialize CCI PDB information from the given format.
    /// </summary>
    /// <param name="peFilePath">Path to IL PE module (needed only for portable PDB's)</param>
    /// <param name="standalonePdbPath">Path to standalone Windows PDB (not used for portable PDBs)</param>
    public static PdbInfo TryLoadFunctions(
      string peFilePath,
      string standalonePdbPath)
    {
      if (!string.IsNullOrEmpty(peFilePath))
      {
        using (Stream peStream = new FileStream(peFilePath, FileMode.Open, FileAccess.Read))
        using (PEReader peReader = new PEReader(peStream))
        {
          MetadataReaderProvider pdbReaderProvider;
          string pdbPath;
          if (peReader.TryOpenAssociatedPortablePdb(peFilePath, File.OpenRead, out pdbReaderProvider, out pdbPath))
          {
            using (pdbReaderProvider)
            {
              // Load associated portable PDB
              PdbWriterForCci cciWriter = new PdbWriterForCci();
  
              new PdbConverter().ConvertPortableToWindows<int>(
                peReader,
                pdbReaderProvider.GetMetadataReader(),
                cciWriter,
                PdbConversionOptions.SuppressSourceLinkConversion);
  
              PdbInfo pdbInfo = new PdbInfo()
              {
                Functions = cciWriter.Functions,
                TokenToSourceMapping = cciWriter.TokenToSourceMapping,
                Age = cciWriter.Age,
                Guid = cciWriter.Guid,
                // Ignored for portable PDBs to avoid bringing in a dependency on Newtonsoft.Json
                SourceServerData = null
              };
  
              return pdbInfo;
            }
          }
        }
      }

      if (File.Exists(standalonePdbPath))
      {
        using (FileStream pdbInputStream = new FileStream(standalonePdbPath, FileMode.Open, FileAccess.Read))
        {
          if (!PdbConverter.IsPortable(pdbInputStream))
          {
            // Load CCI data from Windows PDB
            return PdbFile.LoadFunctions(pdbInputStream);
          }
        }
      }

      // Non-existent Windows PDB or mismatched portable PDB
      return null;
    }

    /// <summary>
    /// The basic idea of the portable PDB conversion is that we let the converter run
    /// and use this PdbWriterForCci class to construct the CCI-expected data structures.
    /// </summary>
    class PdbWriterForCci : Microsoft.DiaSymReader.PdbWriter<int>
    {
      /// <summary>
      /// List of functions exposed by the PDB.
      /// </summary>
      public List<PdbFunction> Functions { get; private set; }

      /// <summary>
      /// Map from method tokens to source location linked lists.
      /// </summary>
      public Dictionary<uint, PdbTokenLine> TokenToSourceMapping { get; private set; }

      /// <summary>
      /// Encoded age information for the portable PDB.
      /// </summary>
      public int Age { get; private set; }

      /// <summary>
      /// Encoded GUID information for the portable PDB.
      /// </summary>
      public Guid Guid { get; private set; }

      /// <summary>
      /// List of previously defined PdbSource documents
      /// </summary>
      private List<PdbSource> _sourceDocuments;

      /// <summary>
      /// The currently open function instance.
      /// </summary>
      private PdbFunction _currentMethod;

      /// <summary>
      /// Scope stack for current method.
      /// </summary>
      private Stack<PdbScopeBuilder> _scopeStackForCurrentMethod;

      /// <summary>
      /// Currently open scope
      /// </summary>
      private PdbScopeBuilder _currentScope;

      /// <summary>
      /// Top level (non-nested) scopes for the current method.
      /// </summary>
      private List<PdbScope> _topLevelScopesForCurrentMethod;

      /// <summary>
      /// All namespaces used by the current method.
      /// </summary>
      private HashSet<string> _usedNamespacesForCurrentMethod;

      /// <summary>
      /// Map from document indices to line numbers for the currently open method.
      /// </summary>
      private Dictionary<int, List<PdbLine>> _linesForCurrentMethod;

      /// <summary>
      /// Construct the converter writer used for building the CCI data graph representing the PDB.
      /// </summary>
      public PdbWriterForCci()
      {
        Functions = new List<PdbFunction>();
        TokenToSourceMapping = new Dictionary<uint, PdbTokenLine>();
        _sourceDocuments = new List<PdbSource>();
        _currentMethod = null;
      }

      /// <summary>
      /// Define an indexed document to be subsequently referred to by sequence points.
      /// </summary>
      /// <returns>Document index that the converter will subsequently pass to DefineSequencePoints</returns>
      public override int DefineDocument(string name, Guid language, Guid vendor, Guid type, Guid algorithmId, byte[] checksum)
      {
        int documentIndex = _sourceDocuments.Count;
        _sourceDocuments.Add(new PdbSource(
          name: name,
          doctype: type,
          language: language,
          vendor: vendor,
          checksumAlgorithm: algorithmId,
          checksum: checksum));
        return documentIndex;
      }

      /// <summary>
      /// Add a set of sequence points in a given document to the currently open method.
      /// </summary>
      /// <param name="documentIndex">Zero-based index of the source document previously allocated in DefineDocument</param>
      /// <param name="count">Number of sequence points to add</param>
      /// <param name="offsets">IL offsets for the individual sequence points</param>
      /// <param name="startLines">Start line numbers</param>
      /// <param name="startColumns">Start column indices</param>
      /// <param name="endLines">Ending line numbers</param>
      /// <param name="endColumns">Ending column indices</param>
      public override void DefineSequencePoints(
        int documentIndex,
        int count,
        int[] offsets,
        int[] startLines,
        int[] startColumns,
        int[] endLines,
        int[] endColumns)
      {
        Contract.Assert(_currentMethod != null);

        List<PdbLine> linesForCurrentDocument;
        if (!_linesForCurrentMethod.TryGetValue(documentIndex, out linesForCurrentDocument))
        {
          linesForCurrentDocument = new List<PdbLine>();
          _linesForCurrentMethod.Add(documentIndex, linesForCurrentDocument);
        }

        PdbTokenLine firstTokenLine = null;
        PdbTokenLine lastTokenLine = null;
        for (int sequencePointIndex = 0; sequencePointIndex < count; sequencePointIndex++)
        {
          PdbTokenLine newTokenLine = new PdbTokenLine(
            token: _currentMethod.token,
            file_id: (uint)documentIndex,
            line: (uint)startLines[sequencePointIndex],
            column: (uint)startColumns[sequencePointIndex],
            endLine: (uint)endLines[sequencePointIndex],
            endColumn: (uint)endColumns[sequencePointIndex]);
          if (firstTokenLine == null)
          {
            firstTokenLine = newTokenLine;
          }
          else
          {
            lastTokenLine.nextLine = newTokenLine;
          }
          lastTokenLine = newTokenLine;
          linesForCurrentDocument.Add(new PdbLine(
            offset: (uint)offsets[sequencePointIndex],
            lineBegin: (uint)startLines[sequencePointIndex],
            colBegin: (ushort)startColumns[sequencePointIndex],
            lineEnd: (uint)endLines[sequencePointIndex],
            colEnd: (ushort)endColumns[sequencePointIndex]));
        }

        PdbTokenLine existingTokenLine;
        if (TokenToSourceMapping.TryGetValue(_currentMethod.token, out existingTokenLine))
        {
          while (existingTokenLine.nextLine != null)
          {
            existingTokenLine = existingTokenLine.nextLine;
          }
          existingTokenLine.nextLine = firstTokenLine;
        }
        else
        {
          TokenToSourceMapping.Add(_currentMethod.token, firstTokenLine);
        }
      }

      /// <summary>
      /// Start populating symbol information pertaining to a given method.
      /// </summary>
      /// <param name="methodToken">MSIL metadata token representing the method</param>
      public override void OpenMethod(int methodToken)
      {
        // Nested method opens are not supported
        Contract.Assert(_currentMethod == null);

        _currentMethod = new PdbFunction();
        _currentMethod.token = (uint)methodToken;
        _linesForCurrentMethod = new Dictionary<int, List<PdbLine>>();

        _scopeStackForCurrentMethod = new Stack<PdbScopeBuilder>();
        _currentScope = null;
        _topLevelScopesForCurrentMethod = new List<PdbScope>();
        _usedNamespacesForCurrentMethod = new HashSet<string>();
      }

      /// <summary>
      /// Finalize method info emission and add the new element to the function list.
      /// </summary>
      public override void CloseMethod()
      {
        Contract.Assert(_currentMethod != null);
        
        List<PdbLines> documentLineSets = new List<PdbLines>();
        foreach (KeyValuePair<int, List<PdbLine>> tokenLinePair in _linesForCurrentMethod)
        {
          int lineCount = tokenLinePair.Value.Count;
          PdbLines lines = new PdbLines(_sourceDocuments[tokenLinePair.Key], (uint)lineCount);
          for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
          {
            lines.lines[lineIndex] = tokenLinePair.Value[lineIndex];
          }
          documentLineSets.Add(lines);
        }
        _currentMethod.scopes = _topLevelScopesForCurrentMethod.ToArray();
        _currentMethod.lines = documentLineSets.ToArray();
        _currentMethod.usedNamespaces = _usedNamespacesForCurrentMethod.ToArray();
        Functions.Add(_currentMethod);
        _currentMethod = null;
        _linesForCurrentMethod = null;

        _scopeStackForCurrentMethod = null;
        _currentScope = null;
        _topLevelScopesForCurrentMethod = null;
        _usedNamespacesForCurrentMethod = null;
      }

      /// <summary>
      /// Open scope at given IL offset within the method. The scopes may be nested.
      /// </summary>
      /// <param name="startOffset">Starting IL offset for the scope</param>
      public override void OpenScope(int startOffset)
      {
        if (_currentScope != null)
        {
          _scopeStackForCurrentMethod.Push(_currentScope);
        }
        _currentScope = new PdbScopeBuilder((uint)startOffset);
      }

      /// <summary>
      /// Close scope at given IL offset within the method.
      /// </summary>
      /// <param name="endOffset">Ending IL offset for the scope</param>
      public override void CloseScope(int endOffset)
      {
        Contract.Assert(_currentScope != null);
        PdbScope scope = _currentScope.Close((uint)endOffset);
        if (_scopeStackForCurrentMethod.Count != 0)
        {
          _currentScope = _scopeStackForCurrentMethod.Pop();
          _currentScope.AddChildScope(scope);
        }
        else
        {
          _currentScope = null;
          _topLevelScopesForCurrentMethod.Add(scope);
        }
      }

      /// <summary>
      /// Define local variable within the current scope
      /// </summary>
      /// <param name="index">Slot index</param>
      /// <param name="name">Variable name</param>
      /// <param name="attributes">Variable properties</param>
      /// <param name="localSignatureToken">Signature token representing the variable type</param>
      public override void DefineLocalVariable(int index, string name, LocalVariableAttributes attributes, int localSignatureToken)
      {
        Contract.Assert(_currentScope != null);

        PdbSlot localVariable = new PdbSlot(
          slot: (uint)index,
          typeToken: (uint)localSignatureToken,
          name: name,
          flags: (ushort)attributes);

        _currentScope.AddSlot(localVariable);
      }

      /// <summary>
      /// Define constant within the current scope
      /// </summary>
      /// <param name="name">Constant name</param>
      /// <param name="value">Constant value</param>
      /// <param name="constantSignatureToken">Signature token representing the constant type</param>
      public override void DefineLocalConstant(string name, object value, int constantSignatureToken)
      {
        Contract.Assert(_currentScope != null);

        if ((constantSignatureToken & 0xFFFFFF) != 0)
        {
          PdbConstant pdbConstant = new PdbConstant(
            name: name,
            token: (uint)constantSignatureToken,
            value: value);
  
          _currentScope.AddConstant(pdbConstant);
        }
      }

      /// <summary>
      /// Add a 'using' namespace clause to the current scope.
      /// </summary>
      /// <param name="importString">Namespace name to add</param>
      public override void UsingNamespace(string importString)
      {
        Contract.Assert(_currentScope != null);

        _currentScope.AddUsedNamespace(importString);
        _usedNamespacesForCurrentMethod.Add(importString);
      }

      public override void SetAsyncInfo(int moveNextMethodToken, int kickoffMethodToken, int catchHandlerOffset, int[] yieldOffsets, int[] resumeOffsets)
      {
        Contract.Assert(_currentMethod != null);
        _currentMethod.synchronizationInformation = new PdbSynchronizationInformation(
          moveNextMethodToken,
          kickoffMethodToken,
          catchHandlerOffset,
          yieldOffsets,
          resumeOffsets);
      }

      public override void DefineCustomMetadata(byte[] metadata)
      {
        Contract.Assert(_currentMethod != null);
        _currentMethod.ReadMD2CustomMetadata(new BitAccess(metadata));
      }

      public override void SetEntryPoint(int entryPointMethodToken)
      {
        // NO-OP for CCI
      }

      public override void UpdateSignature(Guid guid, uint stamp, int age)
      {
        Guid = guid;
        Age = age;
      }

      public override void SetSourceServerData(byte[] sourceServerData)
      {
        // NO-OP for CCI
      }

      public override void SetSourceLinkData(byte[] sourceLinkData)
      {
        // NO-OP for CCI
      }

      /// <summary>
      /// Helper class used to compose a hierarchical tree of method scopes.
      /// At method level, we maintain a stack of these builders. Whenever we Open
      /// a scope, we push a new scope builder to the stack. Once we close a scope,
      /// we complete construction of the PdbScope object at the top of the stack
      /// and pop it off, either adding to children of its parent scope or to the list
      /// of top-level scopes accessible from the PdbFunction object.
      /// </summary>
      private class PdbScopeBuilder
      {
        /// <summary>
        /// Starting IL offset for the scope gets initialized in the constructor.
        /// </summary>
        private readonly uint _startOffset;
        
        /// <summary>
        /// Lazily constructed list of child scopes.
        /// </summary>
        private List<PdbScope> _childScopes;
        
        /// <summary>
        /// Lazily constructed list of per-scope constants.
        /// </summary>
        private List<PdbConstant> _constants;
        
        /// <summary>
        /// Lazily constructed list of 'using' namespaces within the scope.
        /// </summary>
        private List<string> _usedNamespaces;
        
        /// <summary>
        /// Lazily constructed list of slots (local variables).
        /// </summary>
        private List<PdbSlot> _slots;
        
        /// <summary>
        /// Constructor stores the starting IL offset for the scope.
        /// </summary>
        public PdbScopeBuilder(uint startOffset)
        {
          _startOffset = startOffset;
          _childScopes = new List<PdbScope>();
          _constants = new List<PdbConstant>();
          _slots = new List<PdbSlot>();
        }
        
        /// <summary>
        /// Finalize construction of the PdbScope and return the complete PdbScope object.
        /// </summary>
        /// <param name="endOffset">Ending IL offset for the scope</param>
        public PdbScope Close(uint endOffset)
        {
          PdbScope scope = new PdbScope(
            address: 0,
            offset: _startOffset,
            length: endOffset - _startOffset,
            slots: _slots.ToArray(),
            constants: _constants.ToArray(),
            usedNamespaces: _usedNamespaces?.ToArray());
          scope.scopes = _childScopes.ToArray();
          return scope;
        }
        
        /// <summary>
        /// Add a scope to the list of immediate child scopes of this scope.
        /// </summary>
        /// <param name="childScope">Child scope to add to this scope</param>
        public void AddChildScope(PdbScope childScope)
        {
          _childScopes.Add(childScope);
        }
        
        /// <summary>
        /// Add a slot (local variable) to the list of slots for this scope.
        /// </summary>
        /// <param name="slot">Slot to add to this scope</param>
        public void AddSlot(PdbSlot slot)
        {
          _slots.Add(slot);
        }
        
        /// <summary>
        /// Add a constant to the list of constants available within this scope.
        /// </summary>
        /// <param name="constant">Constant to add to this scope</param>
        public void AddConstant(PdbConstant pdbConstant)
        {
          _constants.Add(pdbConstant);
        }
        
        /// <summary>
        /// Add a used namespace to the list of namespaces used by this scope.
        /// </summary>
        /// <param name="usedNamespaceName">Used namespace name to add to this scope</param>
        public void AddUsedNamespace(string usedNamespaceName)
        {
          if (_usedNamespaces == null)
          {
            _usedNamespaces = new List<string>();
          }
          _usedNamespaces.Add(usedNamespaceName);
        }
      }
    }
  }
}
