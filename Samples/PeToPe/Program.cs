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
        Console.WriteLine("usage: PeToPe [path]fileName.ext [noStack]");
        return;
      }
      bool noStack = args.Length == 2;
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
            var options = DecompilerOptions.None;
            if (noStack) options |= DecompilerOptions.Unstack;
            var decompiledModule = Decompiler.GetCodeModelFromMetadataModel(host, module, pdbReader, options);
          ISourceLocationProvider sourceLocationProvider = pdbReader; //The decompiler preserves the Locations from the IOperation values, so the PdbReader still works.
          //Recompiling the CodeModel to IL might change the IL offsets, so a new provider is needed.
          ILocalScopeProvider localScopeProvider = new Decompiler.LocalScopeProvider(pdbReader);

          //Get a mutable copy of the Code Model. The ISourceLocationProvider is needed to provide copied source method bodies with the
          //ability to find out where to mark sequence points when compiling themselves back into IL.
          //(ISourceMethodBody does not know about the Source Model, so this information must be provided explicitly.)
          var copier = new CodeDeepCopier(host, sourceLocationProvider);
          var mutableModule = copier.Copy(decompiledModule);

          //Traverse the mutable copy. In a real application the traversal will collect information to be used during rewriting.
          var traverser = new CodeTraverser() { PreorderVisitor = new CodeVisitor() };
          traverser.Traverse(mutableModule);

          //Rewrite the mutable Code Model. In a real application CodeRewriter would be a subclass that actually does something.
          //(This is why decompiled source method bodies must recompile themselves, rather than just use the IL from which they were decompiled.)
          var rewriter = new CodeRewriter(host);
          var rewrittenModule = rewriter.Rewrite(mutableModule);

          //Write out the Code Model by traversing it as the Metadata Model that it also is.
          using (var peStream = File.Create(rewrittenModule.Location + ".pe")) {
            if (pdbReader == null) {
              PeWriter.WritePeToStream(rewrittenModule, host, peStream);
            } else {
              using (var pdbWriter = new PdbWriter(rewrittenModule.Location + ".pdb", pdbReader)) {
                PeWriter.WritePeToStream(rewrittenModule, host, peStream, sourceLocationProvider, localScopeProvider, pdbWriter);
              }
            }
          }
        }
      }
    }
  }

}
