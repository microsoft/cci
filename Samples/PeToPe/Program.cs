//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;

namespace PeToPe {
  class Program
  {
    #region snippet PeToPeProgramMain
    static void Main(string[] args)
    {
      #region replace
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: PeToPe [path]fileName.ext [decompile]");
        return;
      }
      #endregion
      #region snippet PeToPeProgramMainModuleLoad
      HostEnvironment host = new HostEnvironment();
      IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
      #endregion
      #region replace
      if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
        Console.WriteLine(args[0]+" is not a PE file containing a CLR module or assembly.");
        return;
      }
      #endregion

      #region snippet PeToPeProgramMainPdbReaderPdbWriter
      PdbReader/*?*/ pdbReader = null;
      PdbWriter/*?*/ pdbWriter = null;
      string pdbFile = Path.ChangeExtension(module.Location, "pdb");
      if (File.Exists(pdbFile)) {
        Stream pdbStream = File.OpenRead(pdbFile);
        pdbReader = new PdbReader(pdbStream, host);
        pdbWriter = new PdbWriter(module.Location + ".pdb", pdbReader);
      }
      #endregion

      #region snippet PeToPeProgramMainVisit
      var mutator = new MetadataMutator(host);
      var assembly = module as IAssembly;
      if (assembly != null)
        module = mutator.Visit(mutator.GetMutableCopy(assembly));
      else
        module = mutator.Visit(mutator.GetMutableCopy(module));
      #endregion

      #region snippet PeToPeProgramMainWritePe
      Stream peStream = File.Create(module.Location + ".pe");
      PeWriter.WritePeToStream(module, host, peStream, 
        pdbReader, pdbReader, pdbWriter);
      #endregion
    }
    #endregion
  }

  #region snippet PeToPeHostEnvironment
  internal class HostEnvironment : MetadataReaderHost {
    PeReader peReader;
    internal HostEnvironment() {
      this.peReader = new PeReader(this);
    }
    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(
        BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }
  }
  #endregion
}
