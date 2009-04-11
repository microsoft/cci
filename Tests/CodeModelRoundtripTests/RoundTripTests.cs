using System;
using System.IO;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Collections.Generic;

using Microsoft.Cci;
using Microsoft.CSharp;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Xunit;

public class CodeModelRoundTripTests {

  PdbReader pdbReader;
  PdbWriter pdbWriter;
  HostEnvironment host;

  public CodeModelRoundTripTests() {
    Assert.True(File.Exists(PeVerify.PeVerifyPathv3), "Can't find PEVerify, please update the const.");

    pdbReader = null;
    pdbWriter = null;
    host = new HostEnvironment();
  }

  [Fact]
  public void Repro1WithCode() {
    ExtractAndCompile("Repro1.cs");
    RoundTripWithCodeMutator("Repro1.dll", "Repro1.pdb");
  }

  [Fact]
  public void Repro2WithCode() {
    ExtractAndCompile("Repro2.cs");
    RoundTripWithCodeMutator("Repro2.dll", "Repro2.pdb");
  }

  [Fact]
  public void Repro3WithCode() {
    ExtractAndCompile("Repro3.cs");
    RoundTripWithCodeMutator("Repro3.dll", "Repro3.pdb");
  }

  [Fact]
  public void Repro5WithCode() {
    ExtractAndCompile("Repro5.cs");
    RoundTripWithCodeMutator("Repro5.dll", "Repro5.pdb");
  }

  [Fact]
  public void Repro6WithCode() {
    ExtractAndCompile("Repro6.cs");
    RoundTripWithCodeMutator("Repro6.dll", "Repro6.pdb");
  }

  //[Fact]
  public void SystemCoreWithCode() {
    ExtractResource("CodeModelRoundtripTests.TestData.v4.System.Core.dll", "System.Core.dll");
    ExtractResource("CodeModelRoundtripTests.TestData.v4.System.Core.pdb", "System.Core.pdb");

    RoundTripWithCodeMutator("System.Core.dll", "System.Core.pdb");
  }

  CodeAndContractMutator CreateCodeMutator(IAssembly assembly, string pdbName) {

    LoadPdbReaderWriter(pdbName, assembly);

    SourceMethodBodyProvider ilToSourceProvider = delegate(IMethodBody methodBody) {
      return new Microsoft.Cci.ILToCodeModel.SourceMethodBody(methodBody, host, null, pdbReader);
    };

    SourceToILConverterProvider sourceToILProvider =
            delegate(IMetadataHost host2, ISourceLocationProvider sourceLocationProvider, IContractProvider contractProvider2) {
          return new CodeModelToILConverter(host2, sourceLocationProvider, contractProvider2);
        };

    return new CodeAndContractMutator(host, ilToSourceProvider, sourceToILProvider, pdbReader, null);
  }

  void RoundTripWithMutator(PeVerifyResult expectedResult, IAssembly assembly, MetadataMutator mutator) {
    this.VisitAndMutate(mutator, ref assembly);
    AssertWriteToPeFile(expectedResult, assembly);
  }

  void RoundTripWithILMutator(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    RoundTripWithMutator(expectedResult, LoadAssembly(assemblyName), new MetadataMutator(host));
  }

  void RoundTripWithCodeMutator(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    RoundTripWithCodeMutator(expectedResult, assemblyName, pdbName);
  }

  void RoundTripWithCodeMutator(PeVerifyResult expectedResult, string assemblyName, string pdbName) {
    IAssembly assembly = LoadAssembly(assemblyName);
    RoundTripWithMutator(expectedResult, assembly, CreateCodeMutator(assembly, pdbName));
  }

  void LoadPdbReaderWriter(string pdbPath, IAssembly assembly) {
    using (var f = File.OpenRead(pdbPath)) {
      pdbReader = new PdbReader(f, host);
    }
    pdbWriter = new PdbWriter(Path.GetFullPath(assembly.Location + ".pdb"), pdbReader);
  }

  void AssertWriteToPeFile(PeVerifyResult expectedResult, IAssembly assembly) {
    using (FileStream rewrittenFile = File.Create(assembly.Location)) {
      PeWriter.WritePeToStream(assembly, host, rewrittenFile, pdbReader, pdbReader, pdbWriter);
    }

    Assert.True(File.Exists(assembly.Location));
    PeVerify.Assert(expectedResult, PeVerify.VerifyAssembly(assembly.Location));
  }

  void VisitAndMutate(MetadataMutator mutator, ref IAssembly assembly) {
    assembly = mutator.Visit(mutator.GetMutableCopy(assembly));
    var ccMutator = mutator as CodeAndContractMutator;
    if (ccMutator != null) {
      var normalizer = new CodeModelNormalizer(ccMutator);
      assembly = normalizer.Visit((Assembly)assembly);
    }
    Assert.NotNull(assembly);
  }

  static int StartAndWaitForResult(string fileName, string arguments, ref string stdOut, ref string stdErr) {
    ProcessStartInfo info = new ProcessStartInfo(fileName, arguments);
    info.UseShellExecute = false;
    info.ErrorDialog = false;
    info.CreateNoWindow = true;
    info.RedirectStandardOutput = true;
    info.RedirectStandardError = true;

    using (Process p = Process.Start(info)) {
      stdOut = p.StandardOutput.ReadToEnd();
      stdErr = p.StandardError.ReadToEnd();
      return p.ExitCode;
    }
  }

  IAssembly LoadAssembly(string assemblyName) {
    Assert.True(File.Exists(assemblyName));

    IModule module = host.LoadUnitFrom(assemblyName) as IModule;
    Assert.False (module == null || module == Dummy.Module || module == Dummy.Assembly, "Failed to load the module...");

    IAssembly assembly = module as IAssembly;
    Assert.NotNull(module);

    return assembly;
  }

  static void ExtractResource(string resource, string targetFile) {
    System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
    using (Stream srcStream = a.GetManifestResourceStream(resource)) {
      byte[] bytes = new byte[srcStream.Length];
      srcStream.Read(bytes, 0, bytes.Length);
      File.WriteAllBytes(targetFile, bytes);
    }
  }

  static void ExtractAndCompile(string sourceFile) {
    string assemblyName = Path.ChangeExtension(sourceFile, ".dll");

    ExtractResource("CodeModelRoundtripTests.TestData.source." + sourceFile, sourceFile);

    CompilerParameters parameters = new CompilerParameters();
    parameters.GenerateExecutable = Path.GetExtension(assemblyName) == ".exe";
    parameters.IncludeDebugInformation = true;
    parameters.OutputAssembly = assemblyName;

    string tempFile = Path.GetRandomFileName();
    File.Move(sourceFile, Path.Combine(Path.GetDirectoryName(sourceFile), tempFile));

    CompilerResults results;
    using (CodeDomProvider icc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } })) {
      results = icc.CompileAssemblyFromFile(parameters, tempFile);
    }

    File.Delete(tempFile);

    foreach (var s in results.Errors) {
      Debug.WriteLine(s);
    }

    Assert.Equal(0, results.Errors.Count);
    Assert.True(File.Exists(assemblyName), string.Format("Failed to compile {0} from {1}", assemblyName, sourceFile));
  }

} // class

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

  public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument sourceDocument) {
    try {
      return UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(sourceDocument.Location, sourceDocument);
    } catch (IOException) {
      return null;
    }
  }

  public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument parentSourceDocument, string childDocumentName) {
    try {
      string directory = Path.GetDirectoryName(parentSourceDocument.Location);
      string fullPath = Path.Combine(directory, childDocumentName);
      IBinaryDocument newBinaryDocument = BinaryDocument.GetBinaryDocumentForFile(fullPath, this);
      return UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(newBinaryDocument.Location, newBinaryDocument);
    } catch (IOException) {
      return null;
    }
  }
}

