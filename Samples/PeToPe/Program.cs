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
using Microsoft.Cci;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.MetadataReader;

namespace PeToPe {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: PeToPe [path]fileName.ext [decompile] [noStack]");
        return;
      }
      bool decompile = args.Length >= 2;
      bool noStack = args.Length >= 3;

      using (var host = new HostEnvironment()) {
        //Read the Metadata Model from the PE file
        var module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module is Dummy) {
          Console.WriteLine(args[0]+" is not a PE file containing a CLR module or assembly.");
          return;
        }

        //Get a PDB reader if there is a PDB file.
        PdbReader/*?*/ pdbReader = null;
        string pdbFile = module.DebugInformationLocation;
        if (string.IsNullOrEmpty(pdbFile) || !File.Exists(pdbFile))
          pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          using (var pdbStream = File.OpenRead(pdbFile)) {
            pdbReader = new PdbReader(pdbStream, host);
          }
        }
        using (pdbReader) {
          ISourceLocationProvider sourceLocationProvider = pdbReader;
          ILocalScopeProvider localScopeProvider = pdbReader;
          if (decompile) {
            //Construct a Code Model from the Metadata model via decompilation
            var options = DecompilerOptions.AnonymousDelegates | DecompilerOptions.Iterators | DecompilerOptions.Loops;
            if (noStack) options |= DecompilerOptions.Unstack;
            module = Decompiler.GetCodeModelFromMetadataModel(host, module, pdbReader, options);
            if (pdbReader != null)
              localScopeProvider = new Decompiler.LocalScopeProvider(pdbReader);
          }

          MetadataRewriter rewriter;
          MetadataDeepCopier copier;
          if (decompile) {
            copier = new CodeDeepCopier(host, pdbReader, pdbReader);
            rewriter = new CodeRewriter(host);
          } else {
            copier = new MetadataDeepCopier(host);
            rewriter = new MetadataRewriter(host);
          }

          var mutableModule = copier.Copy(module);
          module = rewriter.Rewrite(mutableModule);

          //var validator = new MetadataValidator(host);
          //List<Microsoft.Cci.ErrorEventArgs> errorEvents = new List<Microsoft.Cci.ErrorEventArgs>();
          //host.Errors += (object sender, Microsoft.Cci.ErrorEventArgs e) => errorEvents.Add(e);
          //var assem = module as IAssembly;
          //validator.Validate(assem);
          //if (errorEvents.Count != 0)
          //{
          //    foreach (var e in errorEvents)
          //    {
          //        foreach (var err in e.Errors)
          //        {
          //            Console.WriteLine(err.Message);
          //        }
          //    }
          //}

#if DEBUG
          var newRoot = Path.GetFileNameWithoutExtension(module.Location)+"1";
          var newName = newRoot+Path.GetExtension(module.Location);
          using (Stream peStream = File.Create(newName)) {
            if (pdbReader == null) {
              PeWriter.WritePeToStream(module, host, peStream);
            } else {
              using (var pdbWriter = new PdbWriter(newRoot+".pdb", pdbReader, emitTokenSourceInfo: true)) {
                PeWriter.WritePeToStream(module, host, peStream, sourceLocationProvider, localScopeProvider, pdbWriter);
              }
            }
          }
#else
          using (Stream peStream = File.Create(module.Location)) {
            if (pdbReader == null) {
              PeWriter.WritePeToStream(module, host, peStream);
            } else {
              using (var pdbWriter = new PdbWriter(pdbFile, pdbReader, emitTokenSourceInfo:true)) {
                PeWriter.WritePeToStream(module, host, peStream, sourceLocationProvider, localScopeProvider, pdbWriter);
              }
            }
          }
#endif
        }
      }
    }
  }

  internal class HostEnvironment : WindowsRuntimeMetadataReaderHost {
    PeReader peReader;
    internal HostEnvironment()
      : base(new NameTable(), new InternFactory(), 0, null, false, true) {
      this.peReader = new PeReader(this);
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

    public override AssemblyIdentity UnifyAssembly(AssemblyIdentity assemblyIdentity)
    {
        if (assemblyIdentity.Name.Value.Equals("mscorlib")) return this.CoreAssemblySymbolicIdentity;
        return base.UnifyAssembly(assemblyIdentity);
    }
  }

}
