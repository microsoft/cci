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
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Xunit;

using Microsoft.Cci;
using Microsoft.CSharp;
using Microsoft.Cci.MutableCodeModel;

public class RoundTripTests {

    HostEnvironment host;

    public RoundTripTests() {
        // we assume peverify.exe is in the path
        host = new HostEnvironment();

       // Debug.Listeners.Clear();
       // Debug.Listeners.Add(new MyTraceListener());
    }

    [Fact]
    public void Repro1WithIL() {
        ExtractAndCompile("Repro1.cs");
        RoundTripWithILMutator("Repro1.dll", "Repro1.pdb");
    }

    [Fact]
    public void Repro3WithIL() {
        ExtractAndCompile("Repro3.cs");
        RoundTripWithILMutator("Repro3.dll", "Repro3.pdb");
    }

   [Fact]
    public void Repro4WithIL() {
        ExtractAndCompile("Repro4.cs");
        RoundTripWithILMutator("Repro4.dll", "Repro4.pdb");
    }

    [Fact]
    public void Repro5WithIL() {
        ExtractAndCompile("Repro5.cs");
        RoundTripWithILMutator("Repro5.dll", "Repro5.pdb");
    }

    [Fact]
    public void Repro6WithIL() {
        ExtractAndCompile("Repro6.cs");
        RoundTripWithILMutator("Repro6.dll", "Repro6.pdb");
    }


    [Fact]
    public void SystemCoreWithIL() {
        ExtractResource("RoundtripTests.TestData.v4.System.Core.dll", "System.Core.dll");
        ExtractResource("RoundtripTests.TestData.v4.System.Core.pdb", "System.Core.pdb");

        RoundTripWithILMutator("System.Core.dll", "System.Core.pdb", true);
    }

    [Fact]
    public void Pe2PeNoPdbWithv2() {
        ExtractResource("RoundtripTests.TestData.v2.mscorlib.dll", "mscorlib.dll");
        RoundTripWithILMutator("mscorlib.dll", null, true);
    }

    [Fact]
    public void Pe2PeWithPdbWithv2() {
        ExtractResource("RoundtripTests.TestData.v2.mscorlib.dll", "mscorlib.dll");
        ExtractResource("RoundtripTests.TestData.v2.mscorlib.pdb", "mscorlib.pdb");

        RoundTripWithILMutator("mscorlib.dll", "mscorlib.pdb", true);
    }

    [Fact]
    public void Pe2PeNoPdbWithv4() {
        ExtractResource("RoundtripTests.TestData.v4.mscorlib.dll", "mscorlib.dll");
        RoundTripWithILMutator("mscorlib.dll", null, true);
    }

    [Fact]
    public void Pe2PeWithPdbWithv4() {

        ExtractResource("RoundtripTests.TestData.v4.mscorlib.dll", "mscorlib.dll");
        ExtractResource("RoundtripTests.TestData.v4.mscorlib.pdb", "mscorlib.pdb");

        RoundTripWithILMutator("mscorlib.dll", "mscorlib.pdb", true);
    }

    void RoundTripWithMutator(PeVerifyResult expectedResult, IAssembly assembly, MetadataRewriter mutator, string pdbPath) {
      var result = this.VisitAndMutate(mutator, assembly);
      AssertWriteToPeFile(expectedResult, result, pdbPath);
    }

    void RoundTripWithILMutator(string assemblyName, string pdbPath) {
      this.RoundTripWithILMutator(assemblyName, pdbPath, false);
    }

    void RoundTripWithILMutator(string assemblyName, string pdbPath, bool verificationMayFail) {
        PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName, verificationMayFail);
        RoundTripWithMutator(expectedResult, LoadAssembly(assemblyName), new MetadataRewriter(host), pdbPath);
    }

    void AssertWriteToPeFile(PeVerifyResult expectedResult, IAssembly assembly, string pdbPath) {
      var validator = new MetadataValidator(this.host);
      List<Microsoft.Cci.ErrorEventArgs> errorEvents = new List<Microsoft.Cci.ErrorEventArgs>();
      this.host.Errors += (object sender, Microsoft.Cci.ErrorEventArgs e) => errorEvents.Add(e);
      validator.Validate(assembly);
      Debug.Assert(errorEvents.Count == 0);
      using (var rewrittenFile = File.Create(assembly.Location)) {
        if (pdbPath != null) {
          using (var f = File.OpenRead(pdbPath)) {
            using (var pdbReader = new PdbReader(f, host)) {
              using (var pdbWriter = new PdbWriter(Path.GetFullPath(assembly.Location + ".pdb"), pdbReader)) {
                PeWriter.WritePeToStream(assembly, host, rewrittenFile, pdbReader, pdbReader, pdbWriter);
              }
            }
          }
        } else {
          using (var pdbWriter = new PdbWriter(Path.GetFullPath(assembly.Location + ".pdb"), null)) {
            PeWriter.WritePeToStream(assembly, host, rewrittenFile, null, null, pdbWriter);
          }
        }
      }

      Assert.True(File.Exists(assembly.Location));
      PeVerify.Assert(expectedResult, PeVerify.VerifyAssembly(assembly.Location, true));
    }

    IAssembly VisitAndMutate(MetadataRewriter mutator, IAssembly assembly) {
      var mutableAssembly = new MetadataDeepCopier(host).Copy(assembly);
      assembly = mutator.Rewrite(mutableAssembly);
      Assert.NotNull(assembly);
      return assembly;
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
        Assert.False(module == null || module == Dummy.Module || module == Dummy.Assembly,
            "Failed to load the module...");

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

        ExtractResource("RoundtripTests.TestData.source." + sourceFile, sourceFile);

        CompilerParameters parameters = new CompilerParameters();
        parameters.GenerateExecutable = Path.GetExtension(assemblyName) == ".exe";
        parameters.IncludeDebugInformation = true;
        parameters.OutputAssembly = assemblyName;

        string tempFile = Path.GetRandomFileName();
        File.Move(sourceFile, Path.Combine(Path.GetDirectoryName(sourceFile), tempFile));

        CompilerResults results;
        using (CodeDomProvider icc = new CSharpCodeProvider()) {
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

    internal HostEnvironment() 
      : base(new NameTable(), new InternFactory(), 0, null, false)
    {
        this.peReader = new PeReader(this);
    }

    public override IUnit LoadUnitFrom(string location) {
        IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
        this.RegisterAsLatest(result);
        return result;
    }

    public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument sourceDocument) {
      try {
        var block = UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(sourceDocument.Location, sourceDocument);
        this.disposableObjectAllocatedByThisHost.Add(block);
        return block;
      } catch (IOException) {
        return null;
      }
    }

    public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument parentSourceDocument, string childDocumentName) {
      try {
        string directory = Path.GetDirectoryName(parentSourceDocument.Location);
        string fullPath = Path.Combine(directory, childDocumentName);
        IBinaryDocument newBinaryDocument = BinaryDocument.GetBinaryDocumentForFile(fullPath, this);
        var block = UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(newBinaryDocument.Location, newBinaryDocument);
        this.disposableObjectAllocatedByThisHost.Add(block);
        return block;
      } catch (IOException) {
        return null;
      }
    }
}

//class MyTraceListener : TraceListener {

//    public override void Fail(string message, string detailMessage) {
//        Console.WriteLine("Fail:");
//        Console.WriteLine(message);
//        Console.WriteLine();
//        Console.WriteLine(detailMessage);

//        Assert.Fail(message);
//    }

//    public override void Fail(string message) {
//        Console.WriteLine("Fail:");
//        Console.WriteLine(message);

//        Assert.Fail(message);
//    }

//    public override void Write(string message) {
//        Console.Write(message);
//    }

//    public override void WriteLine(string message) {
//        Console.WriteLine(message);
//    }
//}
