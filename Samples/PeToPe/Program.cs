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
using Microsoft.Cci.Contracts;
using Microsoft.Cci.ILToCodeModel;

namespace PeToPe {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: PeToPe [path]fileName.ext [decompile]");
        return;
      }
      using (var host = new PeReader.DefaultHost()) {

        //Read the Metadata Model from the PE file
        var module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
          Console.WriteLine(args[0]+" is not a PE file containing a CLR module or assembly.");
          return;
        }

        //Get a PDB reader if there is a PDB file.
        PdbReader/*?*/ pdbReader = null;
        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          Stream pdbStream = File.OpenRead(pdbFile);
          pdbReader = new PdbReader(pdbStream, host);
        }
        using (pdbReader) {
          //Construct a Code Model from the Metadata model via decompilation
          module = Decompiler.GetCodeModelFromMetadataModel(host, module, pdbReader);

          //Get a mutable copy of the Code Model
          var copier = new CodeDeepCopier(host, pdbReader);
          var mutableModule = copier.Copy(module);

          //Rewrite the mutable Code Model
          var rewriter = new CodeRewriter(host); //In a real application CodeRewriter would be a subclass that actually does something.
          module = rewriter.Rewrite(mutableModule);

          //Write out the Code Model by traversing it as the Metadata Model that it also is.
          //Note that the decompiled method bodies know how to compile themselves back into IL
          //and that they have to be able to this since the rewrite step above might have modified the Code Model
          //and thus have invalidated the original IL from which the unrewritten Code Model was constructed.
          Stream peStream = File.Create(module.Location + ".pe");
          if (pdbReader == null) {
            PeWriter.WritePeToStream(module, host, peStream);
          } else {
            using (var pdbWriter = new PdbWriter(module.Location + ".pdb", pdbReader)) {
              PeWriter.WritePeToStream(module, host, peStream, pdbReader, pdbReader, pdbWriter);
            }
          }
        }
      }
    }
  }

}
