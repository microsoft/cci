//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
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
using Microsoft.Cci.MutableCodeModel;

namespace PeToPe {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: PeToPe [path]fileName.ext");
        return;
      }

      using (var host = new PeReader.DefaultHost()) {
        var module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module is Dummy) {
          Console.WriteLine(args[0]+" is not a PE file containing a CLR module or assembly.");
          return;
        }

        PdbReader/*?*/ pdbReader = null;
        string pdbFile = module.DebugInformationLocation;
        if (string.IsNullOrEmpty(pdbFile))
          pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          Stream pdbStream = File.OpenRead(pdbFile);
          pdbReader = new PdbReader(pdbStream, host);
        }

        using (pdbReader) {
          //Make a mutable copy of the module.
          var copier = new MetadataDeepCopier(host);
          var mutableModule = copier.Copy(module);

          //Traverse the module. In a real application the MetadataVisitor and/or the MetadataTravers will be subclasses
          //and the traversal will gather information to use during rewriting.
          var traverser = new MetadataTraverser() { PreorderVisitor = new MetadataVisitor(), TraverseIntoMethodBodies = true };
          traverser.Traverse(mutableModule);

          //Rewrite the mutable copy. In a real application the rewriter would be a subclass of MetadataRewriter that actually does something.
          var rewriter = new MetadataRewriter(host);
          var rewrittenModule = rewriter.Rewrite(mutableModule); 

          //Write out rewritten module.
          using (var peStream = File.Create(rewrittenModule.Location + ".pe")) {
            if (pdbReader == null) {
              PeWriter.WritePeToStream(rewrittenModule, host, peStream);
            } else {
              //Note that the default copier and rewriter preserves the locations collections, so the original pdbReader is still a valid ISourceLocationProvider.
              //However, if IL instructions were rewritten, the pdbReader will no longer be an accurate ILocalScopeProvider
              using (var pdbWriter = new PdbWriter(pdbFile + ".pdb", pdbReader)) {
                PeWriter.WritePeToStream(rewrittenModule, host, peStream, pdbReader, pdbReader, pdbWriter);
              }
            }
          }
        }
      }
    }
  }
}
