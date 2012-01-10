using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;

namespace CdfgToText {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: CdfgToText [path]fileName.ext");
        return;
      }
      using (var host = new PeReader.DefaultHost()) {
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

        FileStream profileReader = null;
        string proFile = Path.ChangeExtension(module.Location, "profile");
        if (File.Exists(proFile))
          profileReader = File.OpenRead(proFile);

        using (pdbReader) {
          string txtFile = Path.ChangeExtension(pdbFile, "txt");
          var writer = new StreamWriter(txtFile);
          SourceEmitter csSourceEmitter = new SourceEmitter(writer, host, pdbReader, profileReader);

          csSourceEmitter.Traverse(module.UnitNamespaceRoot);
          writer.Close();
        }
      }
    }
  }
}
