// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.MutableContracts;

namespace AsmMeta {
  delegate void ErrorLogger(string format, params string[] args);

  class AsmMetaOptions : OptionParsing {

    [OptionDescription("Attribute to exempt from whatever polarity keepAttributes is", ShortForm = "a")]
    public List<string> attrs = new List<string>();

    [OptionDescription("Behave as the original AsmMeta", ShortForm = "b")]
    public bool backwardCompatibility = false;

    [OptionDescription("Emit contracts", ShortForm = "c")]
    public bool contracts = true;

    [OptionDescription("Specify what elements to keep", ShortForm = "k")]
    public KeepOptions whatToEmit = KeepOptions.All;

    [OptionDescription("Only emit security transparent & safe API's", ShortForm = "ot")]
    public bool onlySecurityTransparent = false;

    [OptionDescription("Emit attributes", ShortForm = "ka")]
    public bool emitAttributes = true;

    [OptionDescription("Output (full) path for the reference assembly.", ShortForm = "out")]
    public string output = null;

    [OptionDescription("Rename the assembly itself.", ShortForm = "r")]
    public bool rename = true;

    [OptionDescription("Just rename the assembly, don't modify it in any other way.", ShortForm = "ro")]
    public bool renameOnly = false;

    [OptionDescription("When emitting contracts, include the source text of the condition. (Ignored if /contracts is not specified.)", ShortForm = "st")]
    public bool includeSourceTextInContract = true;

    [OptionDescription("Produce a PDB for output", ShortForm = "pdb")]
    public bool writePDB = false;

    [OptionDescription("Break into debugger", ShortForm = "break")]
    public bool doBreak = false;

    [OptionDescription("Search path for referenced assemblies")]
    public List<string> libPaths = new List<string>();

    [OptionDescription("Full paths to candidate dlls to load for resolution.")]
    public List<string> resolvedPaths = new List<string>();

    [OptionDescription("Search the GAC for assemblies", ShortForm = "gac")]
    public bool searchGAC = false;
  }

  class AsmMeta {

    internal ITypeReference/*?*/ compilerGeneratedAttributeType = null;
    internal ITypeReference/*?*/ contractClassType = null;
    internal ITypeReference/*?*/ systemAttributeType = null;
    internal ITypeReference/*?*/ systemBooleanType = null;
    internal ITypeReference/*?*/ systemStringType = null;
    internal ITypeReference/*?*/ systemObjectType = null;
    internal ITypeReference/*?*/ systemVoidType = null;
    public readonly AsmMetaOptions options;

    private ErrorLogger errorLogger;

    public AsmMeta(ErrorLogger errorLogger) {
      this.errorLogger = errorLogger;
      this.options = new AsmMetaOptions();
    }

    static int Main(string[] args) {
      // Make this thing as robust as possible even against JIT failures: put all processing in a sub-method
      // and call it from here. This method should have as few dependencies as possible.
      int result = 0;
      var startTime = DateTime.Now;
      try {
        #region Turn off all Debug.Assert calls in the infrastructure so this try-catch will report any errors
        System.Diagnostics.Debug.Listeners.Clear();
        System.Diagnostics.Trace.Listeners.Clear();
        #endregion Turn off all Debug.Assert calls in the infrastructure so this try-catch will report any errors
        result = RealMain(args);
      } catch (Exception e) { // swallow everything and just return an error code
        Console.WriteLine("AsmMeta failed with uncaught exception: {0}", e.Message);
        Console.WriteLine("Stack trace: {0}", e.StackTrace);
        return 1;
      } finally {
        var delta = DateTime.Now - startTime;
        Console.WriteLine("elapsed time: {0}ms", delta.TotalMilliseconds);
      }
      return result; // success
    }

    static int RealMain(string[] args) {
      int errorReturnValue = -1;

      #region Parse the command-line arguments.
      AsmMeta asmmeta = new AsmMeta(ConsoleErrorLogger);
      asmmeta.options.Parse(args);
      if (asmmeta.options.HelpRequested) {
        asmmeta.options.PrintOptions("");
        return errorReturnValue;
      }
      if (asmmeta.options.HasErrors) {
        asmmeta.options.PrintErrorsAndExit(Console.Out);
      }
      #endregion

      return asmmeta.Run();
    }

    static void ConsoleErrorLogger(string format, params string[] args) {
      Console.WriteLine(format, args);
    }

    internal int Run() {
      SecurityKeepOptions securityKeepOptions = options.onlySecurityTransparent ? SecurityKeepOptions.OnlyNonCritical : SecurityKeepOptions.All;

      if (options.doBreak) {
          System.Diagnostics.Debugger.Launch();
      }

      string assemblyName = null;
      if (options.GeneralArguments.Count == 1) {
        assemblyName = options.GeneralArguments[0];
      }
      if (assemblyName == null) {
        errorLogger("Must specify an input file.");
        return 1;
      }

      using (var host = new AsmMetaHostEnvironment(options.libPaths.ToArray(), options.searchGAC))
      {
        foreach (var p in options.resolvedPaths)
        {
          host.AddResolvedPath(p);
        }

        IAssembly/*?*/ assembly = host.LoadUnitFrom(assemblyName) as IAssembly;
        if (assembly == null || assembly is Dummy)
        {
          errorLogger(assemblyName + " is not a PE file containing a CLR assembly, or an error occurred when loading it.");
          return 1;
        }

        var rewrittenAttribute = CreateTypeReference(host, assembly, "System.Diagnostics.Contracts.RuntimeContractsAttribute");
        if (AttributeHelper.Contains(assembly.Attributes, rewrittenAttribute))
        {
          errorLogger(assemblyName + " is already rewritten, cannot generate a reference assembly from it.");
          return 1;
        }

        if (options.backwardCompatibility)
        { // redundant because RemoveMethodBodies.ctor also does this when the flag is set
          options.whatToEmit = KeepOptions.ExtVis;
          options.emitAttributes = false;
        }

        PdbReader/*?*/ pdbReader = null;
        if (options.includeSourceTextInContract)
        { // No point getting the PDB file unless we want to use it for source text
          string pdbFile = Path.ChangeExtension(assembly.Location, "pdb");
          if (File.Exists(pdbFile))
          {
            using (var pdbStream = File.OpenRead(pdbFile))
            {
              pdbReader = new PdbReader(pdbStream, host);
            }
          }
          else
          {
            errorLogger("Could not load the PDB file for the assembly '" + assembly.Name.Value + "' . Source text will not be preserved in the reference assembly. Proceeding anyway.");
          }
        }
        using (pdbReader)
        {


          // We might be working on the assembly that defines the contract class and/or the type System.Void.
          // (Or any other platform type!)
          // But the host.PlatformType object has not been duplicated, so its properties will continue to be references to the immutable ones.
          // This is OK, except if the assembly is being renamed.
          this.compilerGeneratedAttributeType = host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute;
          this.contractClassType = host.PlatformType.SystemDiagnosticsContractsContract;
          this.systemAttributeType = host.PlatformType.SystemAttribute;
          this.systemBooleanType = host.PlatformType.SystemBoolean;
          this.systemStringType = host.PlatformType.SystemString;
          this.systemObjectType = host.PlatformType.SystemObject;
          this.systemVoidType = host.PlatformType.SystemVoid;
          //new FindPlatformTypes(this).Traverse(assembly); //update the above fields if any of those types are defined in mutable

          Assembly mutable = new MetadataDeepCopier(host).Copy(assembly);

          #region Rename the assembly in a separate pass because things done in later passes depend on interned keys that are computed based (in part) on the assembly identity

          if (options.rename)
          {
            if (options.output != null)
              mutable.Name = host.NameTable.GetNameFor(Path.GetFileNameWithoutExtension(options.output));
            else
              mutable.Name = host.NameTable.GetNameFor(assembly.Name.Value + ".Contracts");
            mutable.ModuleName = mutable.Name;
            mutable.Kind = ModuleKind.DynamicallyLinkedLibrary;
            mutable = (Assembly)RenameAssembly.ReparentAssemblyIdentity(host, assembly.AssemblyIdentity, mutable.AssemblyIdentity, mutable);
          }

          #endregion Rename the assembly in a separate pass because things done in later passes depend on interned keys that are computed based (in part) on the assembly identity
          if (!options.renameOnly)
          {
            if (options.contracts)
            {

              if (options.rename)
              {
                // We might be working on the assembly that defines the contract class and/or the type System.Void.
                // (Or any other platform type!)
                // But the host.PlatformType object has not been duplicated, so its properties will continue to be references to the immutable ones.
                // This is OK, except if the assembly is being renamed.

                var systemNamespace = GetFirstMatchingNamespace(host, mutable.UnitNamespaceRoot, "System");
                if (systemNamespace != null)
                {
                  var typeDefinition = GetFirstMatchingTypeDefinition(host, systemNamespace, "Attribute");
                  if (typeDefinition != null) this.systemAttributeType = typeDefinition;
                  typeDefinition = GetFirstMatchingTypeDefinition(host, systemNamespace, "Boolean");
                  if (typeDefinition != null) this.systemBooleanType = typeDefinition;
                  typeDefinition = GetFirstMatchingTypeDefinition(host, systemNamespace, "Object");
                  if (typeDefinition != null) this.systemObjectType = typeDefinition;
                  typeDefinition = GetFirstMatchingTypeDefinition(host, systemNamespace, "String");
                  if (typeDefinition != null) this.systemStringType = typeDefinition;
                  typeDefinition = GetFirstMatchingTypeDefinition(host, systemNamespace, "Void");
                  if (typeDefinition != null) this.systemVoidType = typeDefinition;

                  var systemRuntimeNamespace = GetFirstMatchingNamespace(host, systemNamespace, "Runtime");
                  if (systemRuntimeNamespace != null)
                  {
                    var systemRuntimeCompilerServicesNamespace = GetFirstMatchingNamespace(host, systemRuntimeNamespace, "CompilerServices");
                    if (systemRuntimeCompilerServicesNamespace != null)
                    {
                      typeDefinition = GetFirstMatchingTypeDefinition(host, systemRuntimeCompilerServicesNamespace, "CompilerGeneratedAttribute");
                      if (typeDefinition != null) this.compilerGeneratedAttributeType = typeDefinition;
                    }
                  }

                  var systemDiagnosticsNamespace = GetFirstMatchingNamespace(host, systemNamespace, "Diagnostics");
                  if (systemDiagnosticsNamespace != null)
                  {
                    var systemDiagnosticsContractsNamespace = GetFirstMatchingNamespace(host, systemDiagnosticsNamespace, "Contracts");
                    if (systemDiagnosticsContractsNamespace != null)
                    {
                      typeDefinition = GetFirstMatchingTypeDefinition(host, systemDiagnosticsContractsNamespace, "Contract");
                      if (typeDefinition != null) this.contractClassType = typeDefinition;
                    }
                  }
                }
              }

              mutable = AsmMetaRewriter.RewriteModule(host,
                pdbReader,
                mutable,
                contractClassType,
                compilerGeneratedAttributeType,
                systemAttributeType,
                systemBooleanType,
                systemObjectType,
                systemStringType,
                systemVoidType
                ) as Assembly;
            }
            #region Delete things that are not to be kept
            if (options.backwardCompatibility || options.whatToEmit != KeepOptions.All || !options.emitAttributes || (options.attrs != null && 0 < options.attrs.Count))
            {
              DeleteThings thinner = new DeleteThings(host, options.whatToEmit, securityKeepOptions, options.emitAttributes, options.attrs.ToArray(), options.backwardCompatibility);
              if (securityKeepOptions == SecurityKeepOptions.OnlyNonCritical && host.PlatformType.SystemSecuritySecurityCriticalAttribute.ResolvedType is Dummy)
              {
                errorLogger("You asked to remove security critical methods, but the version of mscorlib doesn't support the SecurityCriticalAttribute.");
                return 1;
              }
              thinner.RewriteChildren(mutable);
              #region Fix up dangling references to things that were deleted
              FixUpReferences patcher = new FixUpReferences(host, thinner.WhackedMethods, thinner.WhackedTypes);
              patcher.RewriteChildren(mutable);
              #endregion Fix up dangling references to things that were deleted
            }
            #endregion Delete things that are not to be kept
            #region Output is always a dll, so mark the assembly as that
            mutable.EntryPoint = Dummy.MethodReference;
            mutable.Kind = ModuleKind.DynamicallyLinkedLibrary;
            #endregion Output is always a dll, so mark the assembly as that
          }

          assembly = mutable;

          string outputPath;
          if (options.output != null) // user specified, they'd better make sure it doesn't conflict with anything
            outputPath = options.output;
          else if (options.rename) // A.dll ==> A.Contracts.dll (Always! Even if the input is an exe!)
            outputPath = assembly.Name.Value + ".dll";
          else // A.dll ==> A.dll.meta
            outputPath = assembly.Name.Value + Path.GetExtension(assemblyName) + ".meta";
          // NB: Do *not* pass a pdbWriter to WritePeToStream. No need to create a PDB file and if
          // it is provided, then it might find things (like constants in a scope) that don't get
          // visited and so don't have any type references modified as they should be.
          using (var outputFile = File.Create(outputPath))
          {
            if (pdbReader != null && options.writePDB)
            {
              using (var pdbWriter = new PdbWriter(Path.ChangeExtension(outputPath, "pdb"), pdbReader))
              {
                // Need to not pass in a local scope provider until such time as we have one that will use the mutator
                // to remap things (like the type of a scope constant) from the original assembly to the mutated one.
                PeWriter.WritePeToStream(assembly, host, outputFile, pdbReader, null /*pdbReader*/, pdbWriter);
              }
            }
            else
            {
              PeWriter.WritePeToStream(assembly, host, outputFile);
            }
          }
        }
      }
      return 0; // success
    }

    private INamespaceDefinition/*?*/ GetFirstMatchingNamespace(AsmMetaHostEnvironment host, INamespaceDefinition nameSpace, string name) {
      foreach (var mem in nameSpace.GetMembersNamed(host.NameTable.GetNameFor(name), false)) {
        var ns = mem as INamespaceDefinition;
        if (ns != null) return ns;
      }
      return null;
    }
    private INamespaceTypeDefinition/*?*/ GetFirstMatchingTypeDefinition(AsmMetaHostEnvironment host, INamespaceDefinition nameSpace, string name) {
      foreach (var mem in nameSpace.GetMembersNamed(host.NameTable.GetNameFor(name), false)) {
        var ntd = mem as INamespaceTypeDefinition;
        if (ntd != null) return ntd;
      }
      return null;
    }
    /// <summary>
    /// Creates a type reference anchored in the given assembly reference and whose names are relative to the given host.
    /// When the type name has periods in it, a structured reference with nested namespaces is created.
    /// </summary>
    private static INamespaceTypeReference CreateTypeReference(IMetadataHost host, IAssemblyReference assemblyReference, string typeName) {
      IUnitNamespaceReference ns = new Microsoft.Cci.Immutable.RootUnitNamespaceReference(assemblyReference);
      string[] names = typeName.Split('.');
      for (int i = 0, n = names.Length - 1; i < n; i++)
        ns = new Microsoft.Cci.Immutable.NestedUnitNamespaceReference(ns, host.NameTable.GetNameFor(names[i]));
      return new Microsoft.Cci.Immutable.NamespaceTypeReference(host, ns, host.NameTable.GetNameFor(names[names.Length - 1]), 0, false, false, true, PrimitiveTypeCode.NotPrimitive);
    }

  }

  internal class AsmMetaHostEnvironment : FullyResolvedPathHost {

    internal AsmMetaHostEnvironment(string[] libPaths, bool searchGAC)
      : base() {
      foreach (var p in libPaths) {
        this.AddLibPath(p);
      }
      this.SearchInGAC = searchGAC;
    }

    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }

    /// <summary>
    /// override this here to not use memory mapped files since we want to use asmmeta in msbuild and it is sticky
    /// </summary>
    public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument sourceDocument) {
      try {
        IBinaryDocumentMemoryBlock binDocMemoryBlock = UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(sourceDocument.Location, sourceDocument);
        this.disposableObjectAllocatedByThisHost.Add((IDisposable)binDocMemoryBlock);
        return binDocMemoryBlock;
      } catch (IOException) {
        return null;
      }
    }
  }

}
