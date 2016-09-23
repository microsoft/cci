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
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;

namespace Microsoft.Cci {

  class ILMergeOptions : OptionParsing {

    [OptionDescription("Allow public types to be renamed when they would be duplicates", ShortForm="ad")]
    public bool allowDup = false;

    [OptionDescription("Break into debugger", ShortForm = "break")]
    public bool breakIntoDebugger = false;

    [OptionDescription("Additional paths to search for assemblies.")]
    public List<string> libpaths = null;

    [OptionDescription("Output path for the rewritten assembly. When not specified \".meta\" is appended to the input assembly.", ShortForm = "out")]
    public string output = null;

    [OptionDescription("Version number for target assembly", ShortForm = "v")]
    public string version = null;

  }

  class ILMerge {

    private static ILMergeOptions options;

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
        Console.WriteLine("ILMerge failed with uncaught exception: {0}", e.Message);
        Console.WriteLine("Stack trace: {0}", e.StackTrace);
        result = 1;
      } finally {
        var delta = DateTime.Now - startTime;
        Console.WriteLine("elapsed time: {0}ms", delta.TotalMilliseconds);
      }
      return result; // success
    }

    static int RealMain(string[] args) {

      int errorReturnValue = -1;

      #region Check options

      ILMerge.options = new ILMergeOptions();
      options.Parse(args);

      if (options.HelpRequested)
      {
        options.PrintOptions("");
        return errorReturnValue;
      }
      if (options.HasErrors)
      {
        options.PrintErrorsAndExit(Console.Out);
      }

      if (options.breakIntoDebugger) {
        System.Diagnostics.Debugger.Break();
      }
      #endregion

      Version version = null;
      if (options.version != null) {
        TryGetVersionNumber(options.version, out version);
      }

      using (var host = new ILMergeHost(options.libpaths)) {
        if (options.libpaths != null) {
          foreach (var libpath in options.libpaths) {
            host.AddLibPath(libpath);
          }
        }

        Assembly/*?*/ primaryAssembly = null;
        var modules = new List<Module>();
        var unit2SourceLocationProviderMap = new Dictionary<IUnit, ISourceLocationProvider>();
        var unit2LocalScopeProviderMap = new Dictionary<IUnit, ILocalScopeProvider>();
        try {
          for (int i = 0; i < options.GeneralArguments.Count; i++) {
            var unitName = options.GeneralArguments[i];
            var u = host.LoadUnitFrom(unitName) as IModule;
            if (i == 0) {
              IAssembly/*?*/ assembly = u as IAssembly;
              if (assembly == null || assembly is Dummy) {
                Console.WriteLine(unitName + " is not a PE file containing a CLR assembly, or an error occurred when loading it.");
                return errorReturnValue;
              }
              // Use (the copy of) the first input assembly as the merged assembly!
              primaryAssembly = new MetadataDeepCopier(host).Copy(assembly);
            } else {
              var copier = new MetadataDeepCopier(host,primaryAssembly);
              var mutableModule = copier.Copy(u);
              modules.Add(mutableModule);
            }
            PdbReader/*?*/ pdbReader = null;
            string pdbFile = Path.ChangeExtension(u.Location, "pdb");
            if (File.Exists(pdbFile)) {
              using (var pdbStream = File.OpenRead(pdbFile)) {
                pdbReader = new PdbReader(pdbStream, host);
              }
              unit2SourceLocationProviderMap.Add(u, pdbReader);
              unit2LocalScopeProviderMap.Add(u, pdbReader);
            } else {
              Console.WriteLine("Could not load the PDB file for the unit '" + u.Name.Value + "' . Proceeding anyway.");
              unit2SourceLocationProviderMap.Add(u, null);
            }
          }

          //PdbWriter/*?*/ pdbWriter = null;

          RewriteUnitReferences renamer = new RewriteUnitReferences(host, modules, options);
          renamer.targetAssembly = primaryAssembly;
          renamer.originalAssemblyIdentity = primaryAssembly.AssemblyIdentity;

          int totalNumberOfTypes = primaryAssembly.AllTypes.Count;

          #region Pass 1: Mutate each input module (including the primary assembly) so everything is re-parented to the merged assembly
          renamer.RewriteChildren(primaryAssembly);
          for (int i = 0, n = modules.Count; i < n; i++) {
            var mutableModule = modules[i];
            // call Rewrite and not RewriteChildren so dynamic dispatch can call the right rewriter method
            // otherwise, it just rewrites it as a module, not whatever subtype it is.
            renamer.Rewrite(mutableModule);
            // However, the rewriter does *not* rewrite parents. So need to re-parent the root unit namespace
            // of the mutable assembly so it points to the merged assembly. Otherwise, interning (among other
            // things) will not work correctly.
            var rootUnitNs = (RootUnitNamespace) mutableModule.UnitNamespaceRoot;
            rootUnitNs.Unit = primaryAssembly;
            totalNumberOfTypes += mutableModule.AllTypes.Count;
          }
          #endregion

          #region Pass 2: Collect all of the types into the merged assembly

          var mergedTypes = new List<INamedTypeDefinition>(totalNumberOfTypes);

          #region Merge together all of the <Module> classes from the input assemblies
          // TODO: Merge all of the <Module> classes, i.e., type 0 from each of the input assemblies
          mergedTypes.Add(primaryAssembly.AllTypes[0]);
          #endregion

          var internedKeys = new HashSet<string>(); // keep track of all namespace type definitions

          #region Types from the primary assembly
          for (int i = 1, n = primaryAssembly.AllTypes.Count; i < n; i++) {
            var t = primaryAssembly.AllTypes[i];
            mergedTypes.Add(t);
            if (t is INamespaceTypeDefinition) { // don't care about nested types
              var key = TypeHelper.GetTypeName(t, NameFormattingOptions.None);
              internedKeys.Add(key);
            }
          }
          #endregion

          #region Types from the other input assemblies, taking care of duplicates

          for (int i = 0, n = modules.Count; i < n; i++) {
            var module = modules[i];
            var unitName = module.Name.Value;
            for (int j = 1, m = module.AllTypes.Count; j < m; j++) {
              var t = module.AllTypes[j];
              var namespaceTypeDefinition = t as NamespaceTypeDefinition;
              // duplicates can be only at the top-level: namespace type definitions
              // if a namespace type definition is unique, then so are all of its nested types
              if (namespaceTypeDefinition != null) {
                var typeName = TypeHelper.GetTypeName(namespaceTypeDefinition, NameFormattingOptions.UseGenericTypeNameSuffix);
                if (internedKeys.Contains(typeName)) { // error: duplicate!
                  if (!namespaceTypeDefinition.IsPublic || options.allowDup) {
                    var newName = String.Format("{0}_from_{1}", namespaceTypeDefinition.Name.Value, unitName);
                    namespaceTypeDefinition.Name = host.NameTable.GetNameFor(newName);
                    var newTypeName = TypeHelper.GetTypeName(namespaceTypeDefinition, NameFormattingOptions.UseGenericTypeNameSuffix);
                    Console.WriteLine("Adding '{0}' as '{1}'", typeName, newTypeName);
                    internedKeys.Add(typeName);
                    t = namespaceTypeDefinition;
                  } else {
                    Console.WriteLine("Error: Duplicate type '{0}'", typeName);
                    continue; //TODO: set a flag somewhere to force a failure.
                  }
                } else {
                  //Console.WriteLine("Adding '{0}'", typeName);
                  internedKeys.Add(typeName);
                }
              }
              mergedTypes.Add(t);
            }
          }
          #endregion

          primaryAssembly.AllTypes = mergedTypes;

          #endregion

          CopyResourcesToPrimaryAssembly(primaryAssembly, modules);

          if (version != null) {
            primaryAssembly.Version = version;
          }

          string outputPath;
          if (options.output != null)
            outputPath = options.output;
          else
            outputPath = primaryAssembly.Name.Value + Path.GetExtension(options.GeneralArguments[0]) + ".meta";

          using (var aggregateSourceLocationProvider = new AggregatingSourceLocationProvider(unit2SourceLocationProviderMap)) {
            using (var aggregateLocalScopeProvider = new AggregatingLocalScopeProvider(unit2LocalScopeProviderMap)) {
              using (var peStream = File.Create(outputPath)) {
                using (var pdbWriter = new PdbWriter(Path.ChangeExtension(outputPath, "pdb"), aggregateSourceLocationProvider)) {
                  PeWriter.WritePeToStream(primaryAssembly, host, peStream, aggregateSourceLocationProvider, aggregateLocalScopeProvider, pdbWriter);
                }
              }
            }
          }
        } finally {
        }

        return 0; // success
      }
    }

    private static void CopyResourcesToPrimaryAssembly(Assembly primaryAssembly, List<Module> modules) {
      foreach (var m in modules) {
        var a = m as IAssembly;
        if (a == null) continue;
        if (primaryAssembly.Resources == null) primaryAssembly.Resources = new List<IResourceReference>();
        primaryAssembly.Resources.AddRange(a.Resources);
      }
    }

    private static bool TryGetVersionNumber(string versionString, out Version version) {
      version = null;
      if (!String.IsNullOrEmpty(versionString)) {
        try {
          version = new System.Version(versionString);
          // still need to make sure that all components are at most UInt16.MaxValue - 1,
          // per the spec.
          if (!(version.Major < UInt16.MaxValue)) {
            Console.WriteLine("Invalid major version '{0}' specified. It must be less than UInt16.MaxValue (0xffff).", version.Major);
            return false;
          } else if (!(version.Minor < UInt16.MaxValue)) {
            Console.WriteLine("Invalid minor version '{0}' specified. It must be less than UInt16.MaxValue (0xffff).", version.Minor);
            return false;
          } else if (!(version.Build < UInt16.MaxValue)) {
            Console.WriteLine("Invalid build '{0}' specified. It must be less than UInt16.MaxValue (0xffff).", version.Build);
            return false;
          } else if (!(version.Revision < UInt16.MaxValue)) {
            Console.WriteLine("Invalid revision '{0}' specified. It must be less than UInt16.MaxValue (0xffff).", version.Revision);
            return false;
          }
          return true;
        } catch (System.ArgumentOutOfRangeException) {
          Console.WriteLine("Invalid version '{0}' specified. A major, minor, build, or revision component is less than zero.", versionString);
          return false;
        } catch (System.ArgumentException) {
          Console.WriteLine("Invalid version '{0}' specified. It has fewer than two components or more than four components.", versionString);
          return false;
        } catch (System.FormatException) {
          Console.WriteLine("Invalid version '{0}' specified. At least one component of version does not parse to an integer.", versionString);
          return false;
        } catch (System.OverflowException) {
          Console.WriteLine("Invalid version '{0}' specified. At least one component of version represents a number greater than System.Int32.MaxValue.", versionString);
          return false;
        }
      } else {
        Console.WriteLine("/ver option specified, but no version number.");
        return false;
      }
    }

    private class RewriteUnitReferences : MetadataRewriter {
      private Dictionary<UnitIdentity, bool> sourceUnitIdentities = new Dictionary<UnitIdentity, bool>();
      internal IAssembly/*?*/ targetAssembly = null;
      internal AssemblyIdentity/*?*/ originalAssemblyIdentity = null;
      ILMergeOptions options;

      Dictionary<uint, bool> internedKeys = new Dictionary<uint, bool>();

      public RewriteUnitReferences(IMetadataHost host, IList<Module> sourceUnits, ILMergeOptions options)
        : base(host) {
        this.options = options;
        foreach (var s in sourceUnits) {
          this.sourceUnitIdentities.Add(s.UnitIdentity, true);
        }
      }

      public override IModuleReference Rewrite(IModuleReference moduleReference) {
        if (this.sourceUnitIdentities.ContainsKey(moduleReference.UnitIdentity)) {
            return this.targetAssembly;
        }
        return base.Rewrite(moduleReference);
      }

      public override IAssemblyReference Rewrite(IAssemblyReference assemblyReference) {
        if (this.sourceUnitIdentities.ContainsKey(assemblyReference.UnitIdentity)) {
          return this.targetAssembly;
        }
        return base.Rewrite(assemblyReference);
      }

    }

  }

  internal class ILMergeHost : MetadataReaderHost {
    PeReader peReader;
    List<string> libPaths = new List<string>();
    internal ILMergeHost(List<string>/*?*/ libPaths)
      : base(new NameTable(), new InternFactory(), 0, libPaths, true) {
      this.peReader = new PeReader(this);
      if (libPaths != null) {
        foreach (var p in libPaths)
          this.libPaths.Add(p);
      }
    }

    public override IUnit LoadUnitFrom(string location) {
      if (File.Exists(location)) {
        IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
        this.RegisterAsLatest(result);
        return result;
      } else {
        foreach (var p in this.libPaths) {
          var fullPath = Path.Combine(p, location);
          if (File.Exists(fullPath)) {
            IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(fullPath, this));
            this.RegisterAsLatest(result);
            return result;
          }
        }
        return Dummy.Unit;
      }
    }

  }

}
