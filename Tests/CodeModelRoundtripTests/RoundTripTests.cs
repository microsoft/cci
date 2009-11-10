using System;
using System.IO;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Collections.Generic;

using Microsoft.Cci;
using Microsoft.CSharp;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RoundTripTests {

    PdbReader pdbReader;
    HostEnvironment host;

    [TestInitialize]
    public void Initialize() {
        Assert.IsTrue(File.Exists(PeVerify.PeVerifyPathv3), "Can't find PEVerify, please update the const.");

        pdbReader = null;
        host = new HostEnvironment();

        Debug.Listeners.Clear();
        Debug.Listeners.Add(new MyTraceListener());
    }

    [TestMethod]
    public void Repro1WithIL() {
        ExtractAndCompile("Repro1.cs");
        RoundTripWithILMutator("Repro1.dll", "Repro1.pdb");
    }

    [TestMethod]
    public void Repro1WithCode() {
        ExtractAndCompile("Repro1.cs");
        RoundTripWithCodeMutator("Repro1.dll", "Repro1.pdb");
    }

    [TestMethod]
    public void Repro2WithCode() {
        ExtractAndCompile("Repro2.cs");
        RoundTripWithCodeMutator("Repro2.dll", "Repro2.pdb");
    }

    [TestMethod]
    public void Repro3WithIL() {
        ExtractAndCompile("Repro3.cs");
        RoundTripWithILMutator("Repro3.dll", "Repro3.pdb");
    }

    [TestMethod]
    public void Repro3WithCode() {
        ExtractAndCompile("Repro3.cs");
        RoundTripWithCodeMutator("Repro3.dll", "Repro3.pdb");
    }

    [TestMethod]
    public void Repro4WithIL() {
        ExtractAndCompile("Repro4.cs");
        RoundTripWithILMutator("Repro4.dll", "Repro4.pdb");
    }

    [TestMethod]
    public void Repro5WithIL() {
        ExtractAndCompile("Repro5.cs");
        RoundTripWithILMutator("Repro5.dll", "Repro5.pdb");
    }

    [TestMethod]
    public void Repro5WithCode() {
        ExtractAndCompile("Repro5.cs");
        RoundTripWithCodeMutator("Repro5.dll", "Repro5.pdb");
    }

    [TestMethod]
    public void Repro6WithIL() {
        ExtractAndCompile("Repro6.cs");
        RoundTripWithILMutator("Repro6.dll", "Repro6.pdb");
    }

    [TestMethod]
    public void IteratorWithIL() {
      ExtractAndCompile("IteratorRoundTripTest.cs");
      RoundTripWithILMutator("IteratorRoundTripTest.dll", "IteratorRoundTripTest.pdb");
    }

    //[TestMethod]
    //public void Repro6WithCode() {
    //    ExtractAndCompile("Repro6.cs");
    //    RoundTripWithCodeMutator("Repro6.dll", "Repro6.pdb");
    //}

    [Ignore]
    [TestMethod]
    public void SystemCoreWithCode() {
        string dll = Path.ChangeExtension(Path.GetRandomFileName(), "dll");
        string pdb = Path.ChangeExtension(dll, "pdb");
        ExtractResource("RoundtripTests.TestData.v4.System.Core.dll", dll);
        ExtractResource("RoundtripTests.TestData.v4.System.Core.pdb", pdb);

        RoundTripWithCodeMutator(dll, pdb);
    }

    [TestMethod]
    public void SystemCoreWithIL() {
        string dll = Path.ChangeExtension(Path.GetRandomFileName(), "dll");
        string pdb = Path.ChangeExtension(dll, "pdb");
        ExtractResource("RoundtripTests.TestData.v4.System.Core.dll", dll);
        ExtractResource("RoundtripTests.TestData.v4.System.Core.pdb", pdb);

        RoundTripWithILMutator(dll, pdb);
    }

    [TestMethod]
    public void Pe2PeNoPdbWithv2() {
        string dll = Path.ChangeExtension(Path.GetRandomFileName(), "dll");
        ExtractResource("RoundtripTests.TestData.v2.mscorlib.dll", dll);
        RoundTripWithILMutator(dll, null);
    }

    [TestMethod]
    public void Pe2PeWithPdbWithv2() {
        string dll = Path.ChangeExtension(Path.GetRandomFileName(), "dll");
        string pdb = Path.ChangeExtension(dll, "pdb");
        ExtractResource("RoundtripTests.TestData.v2.mscorlib.dll", dll);
        ExtractResource("RoundtripTests.TestData.v2.mscorlib.pdb", pdb);

        RoundTripWithILMutator(dll, pdb);
    }

    [TestMethod]
    public void Pe2PeNoPdbWithv4() {
        string dll = Path.ChangeExtension(Path.GetRandomFileName(), "dll");
        ExtractResource("RoundtripTests.TestData.v4.mscorlib.dll", dll);
        RoundTripWithILMutator(dll, null);
    }

    [TestMethod]
    public void Pe2PeWithPdbWithv4() {

        string dll = Path.ChangeExtension(Path.GetRandomFileName(), "dll");
        string pdb = Path.ChangeExtension(dll, "pdb");
        ExtractResource("RoundtripTests.TestData.v4.mscorlib.dll", dll);
        ExtractResource("RoundtripTests.TestData.v4.mscorlib.pdb", pdb);

        RoundTripWithILMutator(dll, pdb);
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
    }

    void AssertWriteToPeFile(PeVerifyResult expectedResult, IAssembly assembly) {
        using (var rewrittenFile = File.Create(assembly.Location)){
          using (var pdbWriter = new PdbWriter(Path.GetFullPath(assembly.Location + ".pdb"), pdbReader)) {
            PeWriter.WritePeToStream(assembly, host, rewrittenFile, pdbReader, pdbReader, pdbWriter);
          }
        }

        Assert.IsTrue(File.Exists(assembly.Location));
        PeVerify.Assert(expectedResult, PeVerify.VerifyAssembly(assembly.Location));
    }

    void VisitAndMutate(MetadataMutator mutator, ref IAssembly assembly) {
        assembly = mutator.Visit(mutator.GetMutableCopy(assembly));
        var ccMutator = mutator as CodeAndContractMutator;
        if (ccMutator != null) {
          var normalizer = new CodeModelNormalizer(ccMutator);
          assembly = normalizer.Visit((Assembly)assembly);
        }
        Assert.IsNotNull(assembly);
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
        Assert.IsTrue(File.Exists(assemblyName));

        IModule module = host.LoadUnitFrom(assemblyName) as IModule;
        if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
            Assert.Fail("Failed to load the module...");
        }

        IAssembly assembly = module as IAssembly;
        Assert.IsNotNull(module);

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

        string tempFile = Path.GetRandomFileName();
        ExtractResource("RoundtripTests.TestData.source." + sourceFile, tempFile);

        CompilerParameters parameters = new CompilerParameters();
        parameters.GenerateExecutable = Path.GetExtension(assemblyName) == ".exe";
        parameters.IncludeDebugInformation = true;
        parameters.OutputAssembly = assemblyName;

        CompilerResults results;
        using (CodeDomProvider icc = new CSharpCodeProvider(new Dictionary<string, string>() { { "CompilerVersion", "v3.5" } })) {
            results = icc.CompileAssemblyFromFile(parameters, tempFile);
        }

        File.Delete(tempFile);

        foreach (var s in results.Errors) {
            Debug.WriteLine(s);
        }

        Assert.AreEqual(0, results.Errors.Count);
        Assert.IsTrue(File.Exists(assemblyName), string.Format("Failed to compile {0} from {1}", assemblyName, sourceFile));
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

class MyTraceListener : TraceListener {

    public override void Fail(string message, string detailMessage) {
        Console.WriteLine("Fail:");
        Console.WriteLine(message);
        Console.WriteLine();
        Console.WriteLine(detailMessage);

        Assert.Fail(message);
    }

    public override void Fail(string message) {
        Console.WriteLine("Fail:");
        Console.WriteLine(message);

        Assert.Fail(message);
    }

    public override void Write(string message) {
        Console.Write(message);
    }

    public override void WriteLine(string message) {
        Console.WriteLine(message);
    }
}
