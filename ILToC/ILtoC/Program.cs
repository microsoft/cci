// ==++==
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
using System;
using System.Diagnostics.Contracts;
using System.IO;
using Microsoft.Cci;

namespace ILtoC {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: ILtoC [path]fileName.ext");
        return;
      }
      using (var host = new Host()) {
        var mscorlib = host.LoadUnitFrom("mscorlib.dll") as IAssembly;
        if (mscorlib == null || mscorlib is Dummy) {
          Console.WriteLine("mscorlib.dll must be in the same directory as "+args[0]);
          return;
        }
        var coreIdentity = host.CoreAssemblySymbolicIdentity; //Force the host to select the local mscorlib as the core assembly.
        if (coreIdentity != mscorlib.AssemblyIdentity) {
          Console.WriteLine("bug in host");
          return;
        }
        var module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module is Dummy) {
          Console.WriteLine(args[0]+" is not a PE file containing a CLR module or assembly.");
          return;
        }
        var moduleLocation = args[0];
        Contract.Assume(moduleLocation.Length > 0);

        PdbReader/*?*/ pdbReader = null;
        string pdbFileName = Path.ChangeExtension(moduleLocation, "pdb");
        if (File.Exists(pdbFileName)) {
          using (var pdbStream = File.OpenRead(pdbFileName)) {
            pdbReader = new PdbReader(pdbStream, host);
          }
        }
        var cfile = Path.ChangeExtension(moduleLocation, ".c");
        var hfile = Path.ChangeExtension(moduleLocation, ".h");
        var moduleFileName = Path.GetFileName(moduleLocation);
        Contract.Assume(moduleFileName != null && moduleFileName.Length > 0);
        string location = Path.GetFullPath(moduleLocation).Replace(moduleFileName, ""); ;
        using (var cStreamWriter = File.CreateText(cfile)) {
          using (var hStreamWriter = File.CreateText(hfile)) {
            var cEmitter = new SourceEmitter(cStreamWriter);
            cEmitter.EmitString("#include \"");
            cEmitter.EmitString(hfile);
            cEmitter.EmitString("\"");
            cEmitter.EmitNewLine();
            var hEmitter = new SourceEmitter(hStreamWriter);
            new Translator(host, module, mscorlib, cEmitter, hEmitter, location, pdbReader).TranslateToC();
          }
        }
      }
    }
  }

  class Host : PeReader.DefaultHost {

    public override AssemblyIdentity UnifyAssembly(AssemblyIdentity assemblyIdentity) {
      if (assemblyIdentity.Name.UniqueKey == this.CoreAssemblySymbolicIdentity.Name.UniqueKey)
        return this.CoreAssemblySymbolicIdentity;
      return base.UnifyAssembly(assemblyIdentity);
    }

  }
}
