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
using CSharpSourceEmitter;
using Microsoft.Cci;
using Microsoft.Cci.Contracts;

namespace PeToText {
  class Program {
    static void Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: peToText [path]fileName.ext");
        return;
      }
      bool noIL = args.Length >= 2;
      bool noStack = args.Length >= 3;
      using (var host = new PeReader.DefaultHost())
      {
        IModule/*?*/ module = host.LoadUnitFrom(args[0]) as IModule;
        if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
          Console.WriteLine(args[0] + " is not a PE file containing a CLR module or assembly.");
          return;
        }

        PdbReader/*?*/ pdbReader = null;
        string pdbFile = Path.ChangeExtension(module.Location, "pdb");
        if (File.Exists(pdbFile)) {
          Stream pdbStream = File.OpenRead(pdbFile);
          pdbReader = new PdbReader(pdbStream, host);
        }
        using (pdbReader) {
          SourceEmitterOutputString sourceEmitterOutput = new SourceEmitterOutputString();
          SourceEmitter csSourceEmitter = new SourceEmitter(sourceEmitterOutput, host, pdbReader, noIL, true, noStack);
          csSourceEmitter.Traverse((INamespaceDefinition)module.UnitNamespaceRoot);
          string txtFile = Path.ChangeExtension(pdbFile, "txt");
          File.WriteAllText(txtFile, sourceEmitterOutput.Data);
        }
      }
    }
  }

}
