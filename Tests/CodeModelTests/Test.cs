using System;
using Microsoft.Cci;
using System.IO;
using CSharpSourceEmitter;
using SourceEmitter=PeToText.SourceEmitter;
using Microsoft.Cci.Contracts;
using Xunit;

namespace CodeModelTests {
  /// <summary>
  /// Summary description for UnitTest1
  /// </summary>
  public class Test {

    [Fact]
    public void TestCodeModel() {
      HostEnvironment host = new HostEnvironment();
      var location = typeof(CodeModelTestInput.Class1).Assembly.Location;
      IAssembly/*?*/ assembly = host.LoadUnitFrom(location) as IAssembly;
      Assert.True(assembly != null, "Failed to read in test executable as test data");

      PdbReader/*?*/ pdbReader = null;
      string pdbFile = Path.ChangeExtension(assembly.Location, "pdb");
      if (File.Exists(pdbFile)) {
        Stream pdbStream = File.OpenRead(pdbFile);
        pdbReader = new PdbReader(pdbStream, host);
      }
      ContractProvider contractProvider = new ContractProvider(new ContractMethods(host));

      SourceEmitterContext sourceEmitterContext = new SourceEmitterContext();
      SourceEmitterOutputString sourceEmitterOutput = new SourceEmitterOutputString(sourceEmitterContext);
      SourceEmitter csSourceEmitter = new SourceEmitter(sourceEmitterOutput, host, contractProvider, pdbReader, true);

      csSourceEmitter.Visit((INamespaceDefinition)assembly.UnitNamespaceRoot);
      Stream resource = typeof(Test).Assembly.GetManifestResourceStream("CodeModelTests.CodeModelTestInput.txt");
      StreamReader reader = new StreamReader(resource);
      string expected = reader.ReadToEnd();
      Assert.True(sourceEmitterOutput.Data == expected);
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
