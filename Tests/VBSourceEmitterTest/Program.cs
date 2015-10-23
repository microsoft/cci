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
using Microsoft.Cci.ILToCodeModel;

namespace CSharpSourceEmitterTest {
  class Program {
    [STAThreadAttribute()]
    static void Main(string[] args) {
      string strFileName;
      if (args.Length > 0) {
        strFileName = args[0];
      } else {
        return;
      }

      using (var host = new HostEnvironment()) {
        IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module is Dummy) {
          Console.WriteLine(args[0] + " is not a PE file containing a CLR module or assembly.");
          return;
        }

        PdbReader/*?*/ pdbReader = null;
        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          using (var pdbStream = File.OpenRead(pdbFile)) {
            pdbReader = new PdbReader(pdbStream, host);
          }
        }
        using (pdbReader) {
          module = Decompiler.GetCodeModelFromMetadataModel(host, module, pdbReader, DecompilerOptions.AnonymousDelegates | DecompilerOptions.Iterators | DecompilerOptions.Loops);
          VBSourceEmitter.SourceEmitterOutputString sourceEmitterOutput = new VBSourceEmitter.SourceEmitterOutputString();
          VBSourceEmitter.SourceEmitter CSSourceEmitter = new VBSourceEmitter.SourceEmitter(host, sourceEmitterOutput);

          CSSourceEmitter.Traverse(module.UnitNamespaceRoot);

          Console.WriteLine(sourceEmitterOutput.Data);
        }
      }
      Console.ReadLine();
    }
  }

  internal class HostEnvironment : MetadataReaderHost {
    PeReader peReader;

    internal HostEnvironment()
      : base(new NameTable(), new InternFactory(), 0, null, false) {
      this.peReader = new PeReader(this);
      string/*?*/ loc = typeof(object).Assembly.Location;
      if (loc == null) {
        loc = string.Empty;
      }
    }

    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }
  }
}
