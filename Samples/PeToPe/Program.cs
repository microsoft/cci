//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;

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
      string pdbFile = Path.ChangeExtension(module.Location, "pdb");
      if (File.Exists(pdbFile)) {
        Stream pdbStream = File.OpenRead(pdbFile);
        pdbReader = new PdbReader(pdbStream, host);
      }
      #endregion

      #region snippet PeToPeProgramMainILToSourceProvider
      SourceMethodBodyProvider ilToSourceProvider =
        delegate(IMethodBody methodBody) {
          return new Microsoft.Cci.ILToCodeModel.SourceMethodBody(
            methodBody, host, null, pdbReader);
        };
      #endregion
      #region snippet PeToPeProgramMainSourceToILProvider
      SourceToILConverterProvider sourceToILProvider =
        delegate(IMetadataHost host2, 
          ISourceLocationProvider/*?*/ sourceLocationProvider, 
          IContractProvider/*?*/ contractProvider2) {
          return new CodeModelToILConverter(
            host2, sourceLocationProvider, contractProvider2);
        };
      #endregion

      MetadataMutator mutator;
      bool decompile = args.Length == 2;
      if (decompile) {
        #region snippet PeToPeProgramMainCodeMutator
        mutator = new CodeMutator(
          host, ilToSourceProvider, sourceToILProvider, pdbReader);
        #endregion
      } else {
        mutator = new MetadataMutator(host);
      }

      #region snippet PeToPeProgramMainVisit
      IAssembly/*?*/ assembly = module as IAssembly;
      if (assembly != null)
        module = mutator.Visit(mutator.GetMutableCopy(assembly));
      else
        module = mutator.Visit(mutator.GetMutableCopy(module));
      #endregion

      if (decompile) {
        #region snippet PeToPeProgramMainNormalize
        CodeModelNormalizer cmn = new CodeModelNormalizer(
          host, ilToSourceProvider, sourceToILProvider, pdbReader, null);
        assembly = module as IAssembly;
        if (assembly != null)
          module = cmn.Visit(cmn.GetMutableCopy(assembly));
        else
          module = cmn.Visit(cmn.GetMutableCopy(module));
        #endregion
      }

      #region snippet PeToPeProgramMainWritePe
      Stream peStream = File.Create(module.Location + ".pe");
      if (pdbReader == null) {
        PeWriter.WritePeToStream(module, host, peStream);
      } else {
        using (var pdbWriter = new PdbWriter(module.Location + ".pdb", pdbReader)) {
          PeWriter.WritePeToStream(module, host, peStream, pdbReader, pdbReader, pdbWriter);
        }
      }
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
