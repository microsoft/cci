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

          module = RewriteStoreLocal.RewriteModule(host, localScopeProvider, pdbReader, module);

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
  /// A rewriter that modifies method bodies at the IL level.
  /// It injects a call to Console.WriteLine for each store
  /// to a local for which the PDB reader is able to provide a name.
  /// This is meant to distinguish programmer-defined locals from
  /// those introduced by the compiler.
  /// </summary>
  public class RewriteStoreLocal : MetadataRewriter {

    private ILRewriter ilRewriter;

    private RewriteStoreLocal(IMetadataHost host, ILRewriter rewriter)
      : base(host) {
        this.ilRewriter = rewriter;
    }
    public static IModule RewriteModule(IMetadataHost host, ILocalScopeProvider localScopeProvider, ISourceLocationProvider sourceLocationProvider, IModule module) {
      var rew = new MyILRewriter(host, localScopeProvider, sourceLocationProvider);
      var me = new RewriteStoreLocal(host, rew);
      return me.Rewrite(module);
    }

    public override IMethodBody Rewrite(IMethodBody methodBody) {
      return this.ilRewriter.Rewrite(methodBody);
    }

    private class MyILRewriter : ILRewriter {

      IMethodReference consoleDotWriteLine;

      public MyILRewriter(IMetadataHost host, ILocalScopeProvider localScopeProvider, ISourceLocationProvider sourceLocationProvider)
        : base(host, localScopeProvider, sourceLocationProvider) {
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
            0, false, false, true, PrimitiveTypeCode.NotPrimitive);
        this.consoleDotWriteLine = new Microsoft.Cci.MethodReference(
          host, SystemDotConsoleType,
          CallingConvention.Default,
          systemVoid,
          nameTable.GetNameFor("WriteLine"),
          0, systemString);
        #endregion Get reference to Console.WriteLine
      }

      protected override void EmitOperation(IOperation operation) {
        base.EmitOperation(operation);
        switch (operation.OperationCode) {
          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            base.TrackLocal(operation.Value);
            ILocalDefinition loc = operation.Value as ILocalDefinition;
            string localName;
            if (TryGetLocalName(loc, out localName)) {
              CallWriteLine(localName);
            }
          break;
        }

      }

      private void CallWriteLine(string localName) {
        base.EmitOperation(new Operation() { OperationCode = Microsoft.Cci.OperationCode.Ldstr, Value = localName, });
        base.EmitOperation(new Operation() { OperationCode = Microsoft.Cci.OperationCode.Call, Value = this.consoleDotWriteLine, });
      }

      private bool TryGetLocalName(ILocalDefinition local, out string localNameFromPDB) {
        bool isCompilerGenerated = true;
        localNameFromPDB = null;
        if (this.sourceLocationProvider != null)
          localNameFromPDB = this.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
        return !isCompilerGenerated && !string.IsNullOrEmpty(localNameFromPDB);
      }
    }

  }

}
