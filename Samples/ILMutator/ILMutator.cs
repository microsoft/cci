//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization; // needed for defining exception .ctors
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;

namespace ILMutator {

  class Program {
    static int Main(string[] args) {
      if (args == null || args.Length < 1) {
        Console.WriteLine("Usage: ILMutator <assembly> [<outputPath>]");
        return 1;
      }

      using (var host = new PeReader.DefaultHost()) {
        IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module is Dummy) {
          Console.WriteLine(args[0] + " is not a PE file containing a CLR assembly, or an error occurred when loading it.");
          return 1;
        }
        module = new MetadataDeepCopier(host).Copy(module);

        PdbReader/*?*/ pdbReader = null;
        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          using (var pdbStream = File.OpenRead(pdbFile)) {
            pdbReader = new PdbReader(pdbStream, host);
          }
        } else {
          Console.WriteLine("Could not load the PDB file for '" + module.Name.Value + "' . Proceeding anyway.");
        }
        using (pdbReader) {
          var localScopeProvider = pdbReader == null ? null : new ILGenerator.LocalScopeProvider(pdbReader);
          ILMutator mutator = new ILMutator(host, pdbReader);
          module = mutator.Rewrite(module);

          string newName;
          if (args.Length == 2) {
            newName = args[1];
          } else {
            var loc = module.Location;
            var path = Path.GetDirectoryName(loc)??"";
            var fileName = Path.GetFileNameWithoutExtension(loc);
            var ext = Path.GetExtension(loc);
            newName = Path.Combine(path, fileName + "1" + ext);
          }

          using (var peStream = File.Create(newName)) {
            using (var pdbWriter = new PdbWriter(Path.ChangeExtension(newName, ".pdb"), pdbReader)) {
              PeWriter.WritePeToStream(module, host, peStream, pdbReader, localScopeProvider, pdbWriter);
            }
          }
        }
        return 0; // success
      }
    }
  }

  /// <summary>
  /// A mutator that modifies method bodies at the IL level.
  /// It injects a call to Console.WriteLine for each store
  /// to a local for which the PDB reader is able to provide a name.
  /// This is meant to distinguish programmer-defined locals from
  /// those introduced by the compiler.
  /// </summary>
  public class ILMutator : MetadataRewriter {

    PdbReader/*?*/ pdbReader = null;
    IMethodReference consoleDotWriteLine;

    public ILMutator(IMetadataHost host, PdbReader/*?*/ pdbReader)
      : base(host) {
      this.pdbReader = pdbReader;
      #region Get reference to Console.WriteLine
      var nameTable = host.NameTable;
      var platformType = host.PlatformType;
      var systemString = platformType.SystemString;
      var systemVoid = platformType.SystemVoid;
      var SystemDotConsoleType =
        new Microsoft.Cci.Immutable.NamespaceTypeReference(
          host,
          systemString.ContainingUnitNamespace,
          nameTable.GetNameFor("Console"),
          0, false, false, PrimitiveTypeCode.NotPrimitive);
      this.consoleDotWriteLine = new Microsoft.Cci.MethodReference(
        this.host, SystemDotConsoleType,
        CallingConvention.Default,
        systemVoid,
        nameTable.GetNameFor("WriteLine"),
        0, systemString);
      #endregion Get reference to Console.WriteLine
    }

    List<ILocalDefinition> currentLocals;
    ILGenerator currentGenerator;
    IEnumerator<ILocalScope>/*?*/ scopeEnumerator;
    bool scopeEnumeratorIsValid;
    Stack<ILocalScope> scopeStack = new Stack<ILocalScope>();

    public override IMethodBody Rewrite(IMethodBody methodBody) {
      this.currentLocals = new List<ILocalDefinition>(methodBody.LocalVariables);

      try {
        ProcessOperations(methodBody);
        var newBody = new ILGeneratorMethodBody(this.currentGenerator, methodBody.LocalsAreZeroed, (ushort)(methodBody.MaxStack + 1),
          methodBody.MethodDefinition, this.currentLocals ?? new List<ILocalDefinition>(), Enumerable<ITypeDefinition>.Empty);
        return newBody;
      } catch (ILMutatorException) {
        Console.WriteLine("Internal error during IL mutation for the method '{0}'.",
          MemberHelper.GetMemberSignature(methodBody.MethodDefinition, NameFormattingOptions.SmartTypeName));
        return methodBody;
      } finally {
        this.currentLocals = null;
      }
    }

    Dictionary<uint, ILGeneratorLabel> offset2Label;
    private ILGeneratorLabel GetLabelFor(uint offset) {

      ILGeneratorLabel label;
      var result = this.offset2Label.TryGetValue(offset, out label);
      if (!result) {
        label = new ILGeneratorLabel();
        this.offset2Label.Add(offset, label);
      }
      return label;
    }    

    private void ProcessOperations(IMethodBody methodBody) {

      this.offset2Label = new Dictionary<uint, ILGeneratorLabel>();

      List<IOperation> operations = ((methodBody.Operations == null) ? new List<IOperation>() : new List<IOperation>(methodBody.Operations));
      int count = operations.Count;

      ILGenerator generator = new ILGenerator(this.host, methodBody.MethodDefinition);
      if (this.pdbReader != null) {
        foreach (var ns in this.pdbReader.GetNamespaceScopes(methodBody)) {
          foreach (var uns in ns.UsedNamespaces)
            generator.UseNamespace(uns.NamespaceName.Value);
        }
      }

      this.currentGenerator = generator;
      this.scopeEnumerator = this.pdbReader == null ? null : this.pdbReader.GetLocalScopes(methodBody).GetEnumerator();
      this.scopeEnumeratorIsValid = this.scopeEnumerator != null && this.scopeEnumerator.MoveNext();

      var methodName = MemberHelper.GetMemberSignature(methodBody.MethodDefinition, NameFormattingOptions.SmartTypeName);

      #region Record exception handler information
      foreach (var exceptionInfo in methodBody.OperationExceptionInformation) {
        generator.AddExceptionHandlerInformation(exceptionInfo.HandlerKind, exceptionInfo.ExceptionType,
          this.GetLabelFor(exceptionInfo.TryStartOffset), this.GetLabelFor(exceptionInfo.TryEndOffset),
          this.GetLabelFor(exceptionInfo.HandlerStartOffset), this.GetLabelFor(exceptionInfo.HandlerEndOffset),
          exceptionInfo.HandlerKind == HandlerKind.Filter ? this.GetLabelFor(exceptionInfo.FilterDecisionStartOffset) : null);
      }
      #endregion Record exception handler information

      #region Pass 1: Make a label for each branch target
      for (int i = 0; i < count; i++) {
        IOperation op = operations[i];
        switch (op.OperationCode) {
          case OperationCode.Beq:
          case OperationCode.Bge:
          case OperationCode.Bge_Un:
          case OperationCode.Bgt:
          case OperationCode.Bgt_Un:
          case OperationCode.Ble:
          case OperationCode.Ble_Un:
          case OperationCode.Blt:
          case OperationCode.Blt_Un:
          case OperationCode.Bne_Un:
          case OperationCode.Br:
          case OperationCode.Brfalse:
          case OperationCode.Brtrue:
          case OperationCode.Leave:
          case OperationCode.Beq_S:
          case OperationCode.Bge_S:
          case OperationCode.Bge_Un_S:
          case OperationCode.Bgt_S:
          case OperationCode.Bgt_Un_S:
          case OperationCode.Ble_S:
          case OperationCode.Ble_Un_S:
          case OperationCode.Blt_S:
          case OperationCode.Blt_Un_S:
          case OperationCode.Bne_Un_S:
          case OperationCode.Br_S:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue_S:
          case OperationCode.Leave_S:
            uint x = (uint)op.Value;
            if (!offset2Label.ContainsKey(x))
              offset2Label.Add(x, new ILGeneratorLabel());
            break;
          case OperationCode.Switch:
            uint[] offsets = op.Value as uint[];
            foreach (var offset in offsets) {
              if (!offset2Label.ContainsKey(offset))
                offset2Label.Add(offset, new ILGeneratorLabel());
            }
            break;
          default:
            break;
        }
      }
      #endregion Pass 1: Make a label for each branch target

      #region Pass 2: Emit each operation, along with labels
      for (int i = 0; i < count; i++) {
        IOperation op = operations[i];
        ILGeneratorLabel label;
        this.EmitDebugInformationFor(op);
        #region Mark operation if it is a label for a branch
        if (offset2Label.TryGetValue(op.Offset, out label)) {
          generator.MarkLabel(label);
        }
        #endregion Mark operation if it is a label for a branch

        #region Emit operation along with any injection
        switch (op.OperationCode) {
          #region Branches
          case OperationCode.Beq:
          case OperationCode.Bge:
          case OperationCode.Bge_Un:
          case OperationCode.Bgt:
          case OperationCode.Bgt_Un:
          case OperationCode.Ble:
          case OperationCode.Ble_Un:
          case OperationCode.Blt:
          case OperationCode.Blt_Un:
          case OperationCode.Bne_Un:
          case OperationCode.Br:
          case OperationCode.Brfalse:
          case OperationCode.Brtrue:
          case OperationCode.Leave:
          case OperationCode.Beq_S:
          case OperationCode.Bge_S:
          case OperationCode.Bge_Un_S:
          case OperationCode.Bgt_S:
          case OperationCode.Bgt_Un_S:
          case OperationCode.Ble_S:
          case OperationCode.Ble_Un_S:
          case OperationCode.Blt_S:
          case OperationCode.Blt_Un_S:
          case OperationCode.Bne_Un_S:
          case OperationCode.Br_S:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue_S:
          case OperationCode.Leave_S:
            generator.Emit(ILGenerator.LongVersionOf(op.OperationCode), offset2Label[(uint)op.Value]);
            break;
          case OperationCode.Switch:
            uint[] offsets = op.Value as uint[];
            ILGeneratorLabel[] labels = new ILGeneratorLabel[offsets.Length];
            for (int j = 0, n = offsets.Length; j < n; j++) {
              labels[j] = offset2Label[offsets[j]];
            }
            generator.Emit(OperationCode.Switch, labels);
            break;
          #endregion Branches
          #region Everything else
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
            generator.Emit(op.OperationCode);
            var local = (ILocalDefinition)op.Value;
            string localName;
            if (TryGetLocalName(local, out localName))
              this.CallWriteLine(generator, localName);            
            break;
          case OperationCode.Stloc:
          case OperationCode.Stloc_S:
            generator.Emit(op.OperationCode, op.Value);
            local = (ILocalDefinition)op.Value;
            if (TryGetLocalName(local, out localName))
              this.CallWriteLine(generator, localName);            
            break;
          default:
            if (op.Value == null) {
              generator.Emit(op.OperationCode);
              break;
            }
            var typeCode = System.Convert.GetTypeCode(op.Value);
            switch (typeCode) {
              case TypeCode.Byte:
                generator.Emit(op.OperationCode, (byte)op.Value);
                break;
              case TypeCode.Double:
                generator.Emit(op.OperationCode, (double)op.Value);
                break;
              case TypeCode.Int16:
                generator.Emit(op.OperationCode, (short)op.Value);
                break;
              case TypeCode.Int32:
                generator.Emit(op.OperationCode, (int)op.Value);
                break;
              case TypeCode.Int64:
                generator.Emit(op.OperationCode, (long)op.Value);
                break;
              case TypeCode.Object:
                IFieldReference fieldReference = op.Value as IFieldReference;
                if (fieldReference != null) {
                  generator.Emit(op.OperationCode, this.Rewrite(fieldReference));
                  break;
                }
                ILocalDefinition localDefinition = op.Value as ILocalDefinition;
                if (localDefinition != null) {
                  generator.Emit(op.OperationCode, localDefinition);
                  break;
                }
                IMethodReference methodReference = op.Value as IMethodReference;
                if (methodReference != null) {
                  generator.Emit(op.OperationCode, this.Rewrite(methodReference));
                  break;
                }
                IParameterDefinition parameterDefinition = op.Value as IParameterDefinition;
                if (parameterDefinition != null) {
                  generator.Emit(op.OperationCode, parameterDefinition);
                  break;
                }
                ISignature signature = op.Value as ISignature;
                if (signature != null) {
                  generator.Emit(op.OperationCode, signature);
                  break;
                }
                ITypeReference typeReference = op.Value as ITypeReference;
                if (typeReference != null) {
                  generator.Emit(op.OperationCode, this.Rewrite(typeReference));
                  break;
                }
                throw new ILMutatorException("Should never get here: no other IOperation argument types should exist");
              case TypeCode.SByte:
                generator.Emit(op.OperationCode, (sbyte)op.Value);
                break;
              case TypeCode.Single:
                generator.Emit(op.OperationCode, (float)op.Value);
                break;
              case TypeCode.String:
                generator.Emit(op.OperationCode, (string)op.Value);
                break;
              default:
                // The other cases are the other enum values that TypeCode has.
                // But no other argument types should be in the Operations. ILGenerator cannot handle anything else,
                // so such IOperations should never exist.
                //case TypeCode.Boolean:
                //case TypeCode.Char:
                //case TypeCode.DateTime:
                //case TypeCode.DBNull:
                //case TypeCode.Decimal:
                //case TypeCode.Empty: // this would be the value for null, but the case when op.Value is null is handled before the switch statement
                //case TypeCode.UInt16:
                //case TypeCode.UInt32:
                //case TypeCode.UInt64:
                throw new ILMutatorException("Should never get here: no other IOperation argument types should exist");
            }
            break;
          #endregion Everything else
        }
        #endregion Emit operation along with any injection

      }
      while (this.scopeStack.Count > 0) {
        this.currentGenerator.EndScope();
        this.scopeStack.Pop();
      }
      #endregion Pass 2: Emit each operation, along with labels

    }

    private void EmitDebugInformationFor(IOperation operation) {
      this.currentGenerator.MarkSequencePoint(operation.Location);
      if (this.scopeEnumerator == null) return;
      ILocalScope/*?*/ currentScope = null;
      while (this.scopeStack.Count > 0) {
        currentScope = this.scopeStack.Peek();
        if (operation.Offset < currentScope.Offset + currentScope.Length) break;
        this.scopeStack.Pop();
        this.currentGenerator.EndScope();
        currentScope = null;
      }
      while (this.scopeEnumeratorIsValid) {
        currentScope = this.scopeEnumerator.Current;
        if (currentScope.Offset <= operation.Offset && operation.Offset < currentScope.Offset + currentScope.Length) {
          this.scopeStack.Push(currentScope);
          this.currentGenerator.BeginScope();
          foreach (var local in this.pdbReader.GetVariablesInScope(currentScope))
            this.currentGenerator.AddVariableToCurrentScope(local);
          foreach (var constant in this.pdbReader.GetConstantsInScope(currentScope))
            this.currentGenerator.AddConstantToCurrentScope(constant);
          this.scopeEnumeratorIsValid = this.scopeEnumerator.MoveNext();
        } else
          break;
      }
    }

    private void CallWriteLine(ILGenerator generator, string localName) {
      generator.Emit(OperationCode.Ldstr, localName);
      generator.Emit(OperationCode.Call, this.consoleDotWriteLine);
    }

    private bool TryGetLocalName(ILocalDefinition local, out string localNameFromPDB) {
      bool isCompilerGenerated = true;
      localNameFromPDB = null;
      if (this.pdbReader != null)
        localNameFromPDB = this.pdbReader.GetSourceNameFor(local, out isCompilerGenerated);
      return !isCompilerGenerated && !string.IsNullOrEmpty(localNameFromPDB);
    }

    /// <summary>
    /// Exceptions thrown during extraction. Should not escape this class.
    /// </summary>
    private class ILMutatorException : Exception {
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException() { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException(string s) : base(s) { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException(string s, Exception inner) : base(s, inner) { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ILMutatorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
  }

}
