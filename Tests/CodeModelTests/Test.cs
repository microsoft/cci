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
using Microsoft.Cci;
using System.IO;
using CSharpSourceEmitter;
using SourceEmitter=PeToText.SourceEmitter;
using Xunit;

namespace CodeModelTests {
  /// <summary>
  /// Summary description for UnitTest1
  /// </summary>
  public class Test {

    [Fact]
    public static void TestCodeModel() {
      using (var host = new HostEnvironment()) {
        var location = typeof(CodeModelTestInput.Class1).Assembly.Location;
        IAssembly/*?*/ assembly = host.LoadUnitFrom(location) as IAssembly;
        Assert.True(assembly != null, "Failed to read in test executable as test data");

        PdbReader/*?*/ pdbReader = null;
        string pdbFile = Path.ChangeExtension(assembly.Location, "pdb");
        if (File.Exists(pdbFile)) {
          using (var pdbStream = File.OpenRead(pdbFile)) {
            pdbReader = new PdbReader(pdbStream, host);
          }
        }
        using (pdbReader) {
          SourceEmitterOutputString sourceEmitterOutput = new SourceEmitterOutputString();
            SourceEmitter csSourceEmitter = new SourceEmitter(sourceEmitterOutput, host, pdbReader, noIL: true, printCompilerGeneratedMembers: false, noStack: true);
          csSourceEmitter.Traverse((INamespaceDefinition)assembly.UnitNamespaceRoot);
          string result = sourceEmitterOutput.Data;
          string expected;
          using (var resource = typeof(Test).Assembly.GetManifestResourceStream("CodeModelTests.CodeModelTestInput.txt")) {
            using (var reader = new StreamReader(resource)) {
              expected = reader.ReadToEnd();
            }
          }

          if (result != expected) {
            string resultFile = Path.GetFullPath("CodeModelTestOutput.txt");
            File.WriteAllText(resultFile, result);
            Assert.True(false, "Output didn't match CodeModelTestInput.txt: " + resultFile);
          }
        }
      }
    }
  }

  internal class HostEnvironment : MetadataReaderHost {
    PeReader peReader;
    internal HostEnvironment()
      : base(new NameTable(), new InternFactory(), 0, null, false) {
      this.peReader = new PeReader(this);
    }

    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }
  }

}
