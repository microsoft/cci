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
using Microsoft.Cci;

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
        IUnit unit = host.LoadUnitFrom(strFileName);

        CSharpSourceEmitter.SourceEmitterOutputString sourceEmitterOutput = new CSharpSourceEmitter.SourceEmitterOutputString();
        CSharpSourceEmitter.SourceEmitter CSSourceEmitter = new CSharpSourceEmitter.SourceEmitter(sourceEmitterOutput);

        CSSourceEmitter.Traverse(unit.UnitNamespaceRoot);

        Console.WriteLine(sourceEmitterOutput.Data);
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
