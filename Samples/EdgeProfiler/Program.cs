using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;

namespace EdgeProfiler {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: EdgeProfiler [path]fileName.ext");
        return;
      }
      using (var host = new PeReader.DefaultHost()) {
        IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module is Dummy) {
          Console.WriteLine(args[0]+" is not a PE file containing a CLR module or assembly.");
          return;
        }

        var coreIdentity = host.CoreAssemblySymbolicIdentity; //force host to let args[0] determine the target platform
        var profiler = (IAssembly)host.LoadUnitFrom(typeof(Program).Assembly.Location);
        var logger = (INamespaceTypeDefinition)UnitHelper.FindType(host.NameTable, profiler, "Logger");

        PdbReader/*?*/ pdbReader = null;
        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          using (var pdbStream = File.OpenRead(pdbFile)) {
            pdbReader = new PdbReader(pdbStream, host);
          }
        }

        using (pdbReader) {
          var instrumentedModule = Instrumenter.GetInstrumented(host, module, pdbReader, logger);
          var newRoot = Path.GetFileNameWithoutExtension(module.Location)+".instrumented";
          var newName = newRoot+Path.GetExtension(module.Location);
          using (var peStream = File.Create(newName)) {
            if (pdbReader == null) {
              PeWriter.WritePeToStream(instrumentedModule, host, peStream);
            } else {
              var localScopeProvider = new ILGenerator.LocalScopeProvider(pdbReader);
              using (var pdbWriter = new PdbWriter(newRoot + ".pdb", pdbReader)) {
                PeWriter.WritePeToStream(instrumentedModule, host, peStream, pdbReader, localScopeProvider, pdbWriter);
              }
            }
          }
        }
      }
    }
  }

}
