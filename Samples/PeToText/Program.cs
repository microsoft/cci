using System;
using System.IO;
using CSharpSourceEmitter;
using Microsoft.Cci;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.MetadataReader;

namespace PeToText {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: peToText [path]fileName.ext [noIL] [noStack]");
        return;
      }
      bool noIL = args.Length >= 2;
      bool noStack = args.Length >= 3;
      using (var host = new HostEnvironment())
      {
        IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module is Dummy) {
          Console.WriteLine(args[0]+" is not a PE file containing a CLR module or assembly.");
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
          var options = DecompilerOptions.AnonymousDelegates | DecompilerOptions.Iterators | DecompilerOptions.Loops;
          if (noStack) options |= DecompilerOptions.Unstack;
          module = Decompiler.GetCodeModelFromMetadataModel(host, module, pdbReader, options);
          SourceEmitterOutputString sourceEmitterOutput = new SourceEmitterOutputString();
          SourceEmitter csSourceEmitter = new SourceEmitter(sourceEmitterOutput, host, pdbReader, noIL, printCompilerGeneratedMembers:true);
          csSourceEmitter.Traverse(module);
          string txtFile = Path.ChangeExtension(pdbFile, "txt");
          File.WriteAllText(txtFile, sourceEmitterOutput.Data, System.Text.Encoding.UTF8);
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

  }

}
