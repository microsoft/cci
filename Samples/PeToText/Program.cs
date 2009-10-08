using System;
using System.IO;
using CSharpSourceEmitter;
using Microsoft.Cci;
using Microsoft.Cci.Contracts;

namespace PeToText {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: pe2pe [path]fileName.ext");
        return;
      }
      bool noIL = args.Length == 2;
      HostEnvironment host = new HostEnvironment();
      IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
      if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
        Console.WriteLine(args[0] + " is not a PE file containing a CLR module or assembly.");
        return;
      }
      ContractProvider contractProvider = null;// new ContractProvider(new ContractMethods(host), module);

      PdbReader/*?*/ pdbReader = null;
      string pdbFile = Path.ChangeExtension(module.Location, "pdb");
      if (File.Exists(pdbFile)) {
        Stream pdbStream = File.OpenRead(pdbFile);
        pdbReader = new PdbReader(pdbStream, host);
      }

      SourceEmitterContext sourceEmitterContext = new SourceEmitterContext();
      SourceEmitterOutputString sourceEmitterOutput = new SourceEmitterOutputString(sourceEmitterContext);
      SourceEmitter csSourceEmitter = new SourceEmitter(sourceEmitterOutput, host, contractProvider, pdbReader, noIL);

      csSourceEmitter.Visit((INamespaceDefinition)module.UnitNamespaceRoot);
      string txtFile = Path.ChangeExtension(pdbFile, "txt");
      File.WriteAllText(txtFile, sourceEmitterOutput.Data);

    }
  }

  internal class HostEnvironment : MetadataReaderHost {
    PeReader peReader;
    internal HostEnvironment() {
      this.peReader = new PeReader(this);
    }

    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }

  }

}
