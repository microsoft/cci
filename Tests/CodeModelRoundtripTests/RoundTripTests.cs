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
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.CodeDom.Compiler;
using System.Collections.Generic;

using Microsoft.Cci;
using Microsoft.CSharp;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.ILToCodeModel;
using Xunit;
using Microsoft.Cci.MutableCodeModel.Contracts;
using System.Diagnostics.Contracts;
using GenericTypeInstanceReference = Microsoft.Cci.MutableCodeModel.GenericTypeInstanceReference;

public class CodeModelRoundTripTests {

  PdbReader pdbReader;
  PdbWriter pdbWriter;
  HostEnvironment host;

  public CodeModelRoundTripTests() {
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

  //[Fact]
  //public void Repro6WithCode() {
  //  ExtractAndCompile("Repro6.cs");
  //  RoundTripWithCodeMutator("Repro6.dll", "Repro6.pdb");
  //}

  [Fact]
  public void IteratorWithIL() {
    ExtractAndCompile("IteratorRoundTripTest.cs");
    RoundTripWithILMutator("IteratorRoundTripTest.dll", "IteratorRoundTripTest.pdb");
  }

  [Fact]
  public void IteratorWithCode() {
    ExtractAndCompile("IteratorRoundTripTest.cs");
    RoundTripWithCodeMutator("IteratorRoundTripTest.dll", "IteratorRoundTripTest.pdb");
  }

  [Fact]
  public void DecompilationClosureRoundtrip() {
    ExtractAndCompile("ClosureRoundtrip.cs");
    RoundTripNoCopyTestDecompilation("ClosureRoundtrip.dll", "ClosureRoundtrip.pdb");
  }

  //[Fact]
  public void SystemCoreWithCode() {
    ExtractResource("CodeModelRoundtripTests.TestData.v4.System.Core.dll", "System.Core.dll");
    ExtractResource("CodeModelRoundtripTests.TestData.v4.System.Core.pdb", "System.Core.pdb");

    RoundTripWithCodeMutator("System.Core.dll", "System.Core.pdb");
  }

  [Fact]
  public void MutableCopy1() {
    ExtractAndCompile("MutableCopyTest.cs");
    RoundTripWithCodeCopier("MutableCopyTest.dll", "MutableCopyTest.pdb");
  }

  [Fact]
  public void MutableCopy2() {
    ExtractAndCompile("IteratorRoundTripTest.cs");
    RoundTripWithCodeCopier("IteratorRoundTripTest.dll", "IteratorRoundTripTest.pdb");
  }
  [Fact]
  public void LargeAddGenericParameter() {
    Assert.True(File.Exists("QuickGraph.dll"));
    this.RoundTripMutableCopyAndAddGenericParameter2("QuickGraph.dll");
  }

  [Fact]
  public void AddGenericParameter1() {
    ExtractAndCompile("MutableCopyTest.cs");
    this.RoundTripMutableCopyAndAddGenericParameter("MutableCopyTest.dll", "MutableCopyTest.pdb");
  }

  [Fact]
  public void AddGenericMethod() {
    ExtractAndCompile("GenericMethods.cs");
    this.RoundTripAddGenericMethodParameter("GenericMethods.dll", "GenericMethods.pdb");
  }

  [Fact]
  public void DecompilationTest1() {
    ExtractAndCompile("MutableCopyTest.cs");
    RoundTripNoCopyTestDecompilation("MutableCopyTest.dll", "MutableCopyTest.pdb");
  }

  [Fact]
  public void DecompilationTest2() {
    ExtractAndCompile("IteratorRoundTripTest.cs");
    RoundTripNoCopyTestDecompilation("IteratorRoundTripTest.dll", "IteratorRoundTripTest.pdb");
  }

  [Fact]
  public void AddGenericParameterNoCopy() {
    ExtractAndCompile("MutableCopyTest.cs");
    this.RoundTripAddGenericParameterNoCopyTestDecompilation("MutableCopyTest.dll", "MutableCopyTest.pdb");
  }

  [Fact]
  public void CopyMarkedNodes() {
    ExtractAndCompile("MutableCopyTest.cs");
    this.RoundTripMutableCopyMarkedNodes("MutableCopyTest.dll", "MutableCopyTest.pdb");
  }

  [Fact]
  public void CodeCopierTest1() {
    ExtractAndCompile("MutableCopyTest.cs");
    this.RoundTripCodeCopier("MutableCopyTest.dll", "MutableCopyTest.pdb", false);
  }

  [Fact]
  public void CodeCopierClosureRoundtrip() {
    ExtractAndCompile("ClosureRoundtrip.cs");
    this.RoundTripCodeCopier("ClosureRoundtrip.dll", "ClosureRoundtrip.pdb", false);
  }

  /// <summary>
  /// Quickgraph has an anonymous delegate node that is not recompiled correctly. 
  /// </summary>
  //[Ignore]
  //[TestMethod]
  //public void LargeCodeCopierTest() {
  //  Debug.Assert(File.Exists("QuickGraph.dll"));
  //  this.RoundTripCodeCopier("QuickGraph.dll", null, true);
  //}

  [Fact]
  public void CodeCopyAndExecute1() {
    ExtractAndCompileExe("TestClass1.cs");
    this.RoundTripCopyAndExecute("TestClass1.exe", "TestClass1.pdb", true);
  }

  [Fact]
  public void CodeCopyRewriteAndExecute1() {
    ExtractAndCompileExe("TestClass1.cs");
    this.RoundTripCopyRewriteAndExecute("TestClass1.exe", "TestClass1.pdb", true);
  }

  CodeAndContractRewriter CreateCodeMutator(IAssembly assembly, string pdbName) {
    return new CodeAndContractRewriter(host);
  }

  void RoundTripWithMutator(PeVerifyResult expectedResult, IAssembly assembly, MetadataRewriter mutator) {
    this.VisitAndMutate(mutator, ref assembly);
    AssertWriteToPeFile(expectedResult, assembly);
  }

  void RoundTripWithILMutator(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    RoundTripWithMutator(expectedResult, LoadAssembly(assemblyName), new MetadataRewriter(host));
  }

  void RoundTripWithCodeMutator(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    RoundTripWithCodeMutator(expectedResult, assemblyName, pdbName);
  }

  void RoundTripWithCodeMutator(PeVerifyResult expectedResult, string assemblyName, string pdbName) {
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        RoundTripWithMutator(expectedResult, codeAssembly, CreateCodeMutator(codeAssembly, pdbName));
      }
    }
  }

  void AssertWriteToPeFile(PeVerifyResult expectedResult, IAssembly assembly) {
    using (FileStream rewrittenFile = File.Create(assembly.Location)) {
      using (this.pdbReader) {
        PeWriter.WritePeToStream(assembly, this.host, rewrittenFile, this.pdbReader, this.pdbReader, this.pdbWriter);
      }
    }

    Assert.True(File.Exists(assembly.Location));
    PeVerify.Assert(expectedResult, PeVerify.VerifyAssembly(assembly.Location));
  }

  void VisitAndMutate(MetadataRewriter mutator, ref IAssembly assembly) {
    assembly = mutator.Rewrite(assembly);
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
    Assert.False(module == null || module == Dummy.Module || module == Dummy.Assembly, "Failed to load the module...");

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
    ExtractAndCompile(sourceFile, ".dll");
  }

  static void ExtractAndCompileExe(string sourceFile) {
    ExtractAndCompile(sourceFile, ".exe");
  }

  static void ExtractAndCompile(string sourceFile, string extension) {
    string assemblyName = Path.ChangeExtension(sourceFile, extension);

    ExtractResource("CodeModelRoundtripTests.TestData.source." + sourceFile, sourceFile);

    CompilerParameters parameters = new CompilerParameters();
    parameters.GenerateExecutable = Path.GetExtension(assemblyName) == ".exe";
    parameters.IncludeDebugInformation = true;
    parameters.OutputAssembly = assemblyName;
    parameters.CompilerOptions += " -unsafe";

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

  void RoundTripWithCodeCopier(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        codeAssembly = new CodeDeepCopier(host).Copy(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Traverse(codeAssembly);
        Debug.Assert(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
      }
    }
  }

  void AssertWriteToPeFile(PeVerifyResult expectedResult, IAssembly assembly, PdbReader pdbReader) {
    using (var rewrittenFile = File.Create(assembly.Location)) {
      using (var pdbWriter = new PdbWriter(Path.GetFullPath(assembly.Location + ".pdb"), pdbReader)) {
        PeWriter.WritePeToStream(assembly, host, rewrittenFile, pdbReader, pdbReader, pdbWriter);
      }
    }
    Assert.True(File.Exists(assembly.Location));
    PeVerify.Assert(expectedResult, PeVerify.VerifyAssembly(assembly.Location, true));
  }

  void RoundTripMutableCopyAndAddGenericParameter2(string assemblyName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    var copier1 = new MetadataDeepCopier(host);
    var codeAssembly = copier1.Copy(assembly);
    for (int i = 0; i < 30; i++) {
      AddGenericParameters adder = new AddGenericParameters(host, codeAssembly.AllTypes, i);
      codeAssembly = (Assembly)adder.Rewrite(codeAssembly);
    }
    AssertWriteToPeFile(expectedResult, codeAssembly, null);
  }

  void RoundTripMutableCopyAndAddGenericParameter(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        codeAssembly = new CodeDeepCopier(host).Copy(codeAssembly);
        AddGenericParameters adder = new AddGenericParameters(host, codeAssembly.AllTypes, 0);
        codeAssembly = (Assembly)adder.Rewrite(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Traverse(codeAssembly);
        Debug.Assert(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
      }
    }
  }

  void RoundTripAddGenericMethodParameter(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        var copier = new CodeDeepCopier(this.host);
        codeAssembly = copier.Copy(codeAssembly);
        var adder = new AddGenericMethodParameters(host);
        var rewrittenAssembly = adder.Rewrite(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Traverse(rewrittenAssembly);
        Debug.Assert(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, rewrittenAssembly, pdbReader);
      }
    }
  }

  void RoundTripAddGenericParameterNoCopyTestDecompilation(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        AddGenericParameters adder = new AddGenericParameters(host, codeAssembly.AllTypes, 0);
        codeAssembly = (Assembly)adder.Rewrite(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Traverse(codeAssembly);
        Debug.Assert(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
      }
    }
  }

  void RoundTripNoCopyTestDecompilation(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        Checker checker = new Checker(this.host);
        checker.Traverse(codeAssembly);
        Debug.Assert(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
      }
    }
  }


  void RoundTripMutableCopyMarkedNodes(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var adder = new CopyMarkedNodes(host);
        var codeAssembly = adder.Rewrite(assembly);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
      }
    }
  }

  void RoundTripCodeCopier(string assemblyName, string pdbName, bool allowCheckerFail) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    if (pdbName != null) {
      using (var f = File.OpenRead(pdbName)) {
        using (var pdbReader = new PdbReader(f, host)) {
          var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
          CodeDeepCopier copier = new CodeDeepCopier(host);
          codeAssembly = copier.Copy(codeAssembly);
          Checker checker = new Checker(this.host);
          checker.Traverse(codeAssembly);
          Debug.Assert(checker.Errors.Count == 0);
          AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
        }
      }
    } else {
      var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, null);
      CodeDeepCopier copier = new CodeDeepCopier(host);
      codeAssembly = copier.Copy(codeAssembly);
      Checker checker = new Checker(this.host);
      checker.Traverse(codeAssembly);
      Debug.Assert(allowCheckerFail || checker.Errors.Count == 0);
      AssertWriteToPeFile(expectedResult, codeAssembly, null);
    }
  }

  void RoundTripCopyAndExecute(string assemblyName, string pdbName) {
    this.RoundTripCopyAndExecute(assemblyName, pdbName, false);
  }

  void RoundTripCopyAndExecute(string assemblyName, string pdbName, bool verificationMayFail) {
    PeVerifyResult expectedResult = !verificationMayFail
      ? PeVerify.VerifyAssembly(assemblyName)
      : PeVerify.RunPeVerifyOnAssembly(assemblyName);
    string expectedOutput = Execute(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        var copier = new CodeDeepCopier(host);
        codeAssembly = copier.Copy(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Traverse(codeAssembly);
        Debug.Assert(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
        AssertExecute(expectedOutput, assemblyName);
      }
    }
  }

  void RoundTripCopyRewriteAndExecute(string assemblyName, string pdbName) {
    this.RoundTripCopyRewriteAndExecute(assemblyName, pdbName, false);
  }

  void RoundTripCopyRewriteAndExecute(string assemblyName, string pdbName, bool verificationMayFail) {
    PeVerifyResult expectedResult = !verificationMayFail
      ? PeVerify.VerifyAssembly(assemblyName)
      : PeVerify.RunPeVerifyOnAssembly(assemblyName);
    string expectedOutput = Execute(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        var copier = new CodeDeepCopier(host);
        codeAssembly = copier.Copy(codeAssembly);
        NameChanger namechanger = new NameChanger(this.host, new Regex("[A-Za-z0-9_]*"), new MatchEvaluator(this.eval));
        codeAssembly = namechanger.Change(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Traverse(codeAssembly);
        Debug.Assert(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
        AssertExecute(expectedOutput, assemblyName);
      }
    }
  }

  string eval(Match match) {
    return ("AA" + match.Value);
  }

  public static void AssertExecute(string expectedOutput, string assemblyName) {
    string result = Execute(assemblyName);
    Debug.Assert(result == expectedOutput);
  }

  public static string Execute(string assemblyName) {
    Process p = new Process();
    p.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
    p.StartInfo.FileName = assemblyName;
    p.StartInfo.CreateNoWindow = true;
    p.StartInfo.RedirectStandardOutput = true;
    p.StartInfo.UseShellExecute = false;
    p.Start();
    p.WaitForExit();
    return p.StandardOutput.ReadToEnd();
  }
} // class

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

  public override IBinaryDocumentMemoryBlock/*?*/ OpenBinaryDocument(IBinaryDocument sourceDocument) {
    try {
      var block = UnmanagedBinaryMemoryBlock.CreateUnmanagedBinaryMemoryBlock(sourceDocument.Location, sourceDocument);
      this.disposableObjectAllocatedByThisHost.Add((IDisposable)block);
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
      this.disposableObjectAllocatedByThisHost.Add((IDisposable)block);
      return block;
    } catch (IOException) {
      return null;
    }
  }
}

/// <summary>
/// Add a generic parameter to a selected type. 
/// </summary>
internal class AddGenericParameters : CodeRewriter {
  internal AddGenericParameters(IMetadataHost host, List<INamedTypeDefinition> alltypes, int number)
    : base(host) {
    this.allTypes = alltypes;
    this.number = number;
  }
  List<INamedTypeDefinition> allTypes;
  int number;
  bool once = false;

  public override void RewriteChildren(NestedUnitNamespace unitNamespace) {
    if (!this.once) {
      List<INamespaceTypeDefinition> genericTypes = new List<INamespaceTypeDefinition>();
      int count = 0;
      for (int i = 0, n = unitNamespace.Members.Count; i < n; i++) {
        INamespaceTypeDefinition typ = unitNamespace.Members[i] as INamespaceTypeDefinition;
        if (typ != null && typ.IsGeneric && !typ.Name.Value.Contains("<<>>")) {
          this.once = true;
          if (count == this.number) {
            genericTypes.Add(typ);
            break;
          }
          count++;
        }
      }
      if (genericTypes.Count > 0) {
        foreach (var t in this.WithMoreGenericParameters(genericTypes)) {
          unitNamespace.Members.Add(t);
        }
      }
    }
    base.RewriteChildren(unitNamespace);
  }

  protected List<NamespaceTypeDefinition> WithMoreGenericParameters(List<INamespaceTypeDefinition> genericTypes) {
    var copier = new CodeDeepCopier(this.host);
    var copies = new List<NamespaceTypeDefinition>();
    foreach (var genericType in genericTypes) {
      var copy = copier.Copy(genericType);
      copy.Name = this.host.NameTable.GetNameFor(genericType.Name.Value + "<<>>");
      copies.Add(copy);
      this.AddToAllTypes(copy);
      this.AddGenericParamInPlace(copy);
      var fixer = new ReferenceFixer(this.host, copy);
      fixer.Rewrite(copy);
    }
    return copies;
  }

  private void AddToAllTypes(INamedTypeDefinition type) {
    this.allTypes.Add(type);
    foreach (var nestedType in type.NestedTypes)
      this.AddToAllTypes(nestedType);
  }

  public void AddGenericParamInPlace(NamespaceTypeDefinition namespaceTypeDefinition) {
    var oneMoreGP = new List<IGenericTypeParameter>();
    var gp1 = new GenericTypeParameter() {
      InternFactory = this.host.InternFactory,
      PlatformType = this.host.PlatformType,
      Name = this.host.NameTable.GetNameFor("_SX_"),
      DefiningType = namespaceTypeDefinition,
      Index = 0
    };
    oneMoreGP.Add(gp1);
    if (namespaceTypeDefinition.GenericParameters != null) {
      foreach (GenericTypeParameter gp in namespaceTypeDefinition.GenericParameters) {
        gp.Index++;
        oneMoreGP.Add(gp);
      }
    }
    namespaceTypeDefinition.GenericParameters = oneMoreGP;
  }

  internal class ReferenceFixer : CodeRewriter {
    internal ReferenceFixer(IMetadataHost host, INamedTypeDefinition newRoot)
      : base(host) {
      this.newRoot = newRoot;
    }
    Dictionary<object, object> alreadyAdded = new Dictionary<object, object>();
    INamedTypeDefinition newRoot;

    public override ISourceMethodBody Rewrite(ISourceMethodBody sourceMethodBody) {
      return base.Rewrite(sourceMethodBody);
    }

    public override void RewriteChildren(GenericTypeParameterReference genericTypeParameterReference) {
      if (genericTypeParameterReference.DefiningType.InternedKey == this.newRoot.InternedKey)
        genericTypeParameterReference.Index++;
      base.RewriteChildren(genericTypeParameterReference);
    }

    public override void RewriteChildren(GenericTypeInstanceReference genericTypeInstanceReference) {
      if (genericTypeInstanceReference.GenericType.InternedKey == this.newRoot.InternedKey)
        genericTypeInstanceReference.GenericArguments.Insert(0, IteratorHelper.First(this.newRoot.GenericParameters));
      base.RewriteChildren(genericTypeInstanceReference);
    }

  }
}

/// <summary>
/// A simple test: add one generic parameter to generic methods.
/// 
/// B {
///   f1[T] {
///     call f1[T];
///     call f2[T, T];
///   }
///   f2[T1, T2] {}
/// } 
/// 
/// will become:
/// 
/// B {
///   f1[T]... //unchanged
///   new_f1[_SX, T] {
///      call new_f1[_SX, T];
///      call new_f2[_SX, T, T];
///   }
///   f2[T1, T2]// unchanged;
///   new_f2[_SX, T1, T2] {}
/// }
/// </summary>
internal class AddGenericMethodParameters : CodeRewriter {
  internal AddGenericMethodParameters(IMetadataHost host)
    : base(host) {
  }
  Dictionary<int, int> nameMapping = new Dictionary<int, int>();
  Dictionary<int, IMethodDefinition> newMethods = new Dictionary<int, IMethodDefinition>();

  public override void RewriteChildren(NamedTypeDefinition typeDefinition) {
    List<IMethodDefinition> genericMethods = new List<IMethodDefinition>();
    for (int i = 0, n = typeDefinition.Methods == null ? 0 : typeDefinition.Methods.Count; i < n; i++) {
      var member = typeDefinition.Methods[i];
      if (member.IsGeneric) {
        genericMethods.Add(member);
      }
    }
    if (typeDefinition.Methods == null) typeDefinition.Methods = new List<IMethodDefinition>();
    this.WithMoreGenericParameters(genericMethods, typeDefinition.Methods);
    base.RewriteChildren(typeDefinition);
  }

  private void WithMoreGenericParameters(List<IMethodDefinition> genericMethods, List<IMethodDefinition> result) {
    var copier = new CodeDeepCopier(this.host);
    var copies = new List<MethodDefinition>();
    foreach (var method in genericMethods) {
      var copy = copier.Copy(method);
      copy.Name = this.host.NameTable.GetNameFor(method.Name.Value + "<<>>");
      this.nameMapping[method.Name.UniqueKey] = copy.Name.UniqueKey;
      this.newMethods[copy.Name.UniqueKey] = copy;
      result.Add(copy);
      copies.Add(copy);
    }
    foreach (var copy in copies) {
      this.AddGenericParameter(copy);
      var fixer = new ReferenceFixer(this.host, copy, this.nameMapping, this.newMethods);
      fixer.Rewrite(copy);
    }
  }

  public void AddGenericParameter(MethodDefinition method) {
    var oneMoreGP = new List<IGenericMethodParameter>();
    var gp1 = new GenericMethodParameter();
    gp1.Name = this.host.NameTable.GetNameFor("_SX_");
    gp1.DefiningMethod = method;
    gp1.Index = 0;
    gp1.InternFactory = this.host.InternFactory;
    oneMoreGP.Add(gp1);
    if (method.GenericParameterCount > 0) {
      foreach (GenericMethodParameter gp in method.GenericParameters) {
        gp.Index++;
        oneMoreGP.Add(gp);
      }
    }
    method.GenericParameters = oneMoreGP;
  }
  class ReferenceFixer : CodeRewriter {
    internal ReferenceFixer(IMetadataHost host, IMethodDefinition root, Dictionary<int, int> nameMapping, Dictionary<int, IMethodDefinition> newMethods)
      : base(host) {
      this.root = root;
      this.nameMapping = nameMapping;
      this.newMethods = newMethods;
    }
    Dictionary<object, object> alreadyAdded = new Dictionary<object, object>();
    Dictionary<int, IMethodDefinition> newMethods;
    Dictionary<int, int> nameMapping;
    IMethodDefinition root;

    public override void RewriteChildren(Microsoft.Cci.MutableCodeModel.GenericMethodInstanceReference genericMethodInstanceReference) {
      base.RewriteChildren(genericMethodInstanceReference);
      if (genericMethodInstanceReference.GenericMethod.Name.Value.EndsWith("<<>>")) {
        genericMethodInstanceReference.Name = genericMethodInstanceReference.Name;
        var newArgs = new List<ITypeReference>();
        // add the first generic parameter of the current method.
        foreach (var gmpr in root.GenericParameters) {
          newArgs.Add(gmpr);
          break;
        }
        foreach (var arg in ((IGenericMethodInstanceReference)genericMethodInstanceReference).GenericArguments) {
          newArgs.Add(arg);
        }
        genericMethodInstanceReference.GenericArguments = newArgs;
      }
    }

    public override void RewriteChildren(Microsoft.Cci.MutableCodeModel.GenericMethodParameterReference genericMethodParameterReference) {
      if ((genericMethodParameterReference.Index > 0 || genericMethodParameterReference.Name.Value != "_SX_") &&
        genericMethodParameterReference.DefiningMethod.Name.Value.EndsWith("<<>>"))
        genericMethodParameterReference.Index++;
      base.RewriteChildren(genericMethodParameterReference);
    }

    public override IMethodReference RewriteUnspecialized(IMethodReference methodReference) {
      if (this.nameMapping.ContainsKey(methodReference.Name.UniqueKey)) {
        int newMethodNameKey = this.nameMapping[methodReference.Name.UniqueKey];
        return this.newMethods[newMethodNameKey];
      }
      return base.RewriteUnspecialized(methodReference);
    }

  }
}

/// <summary>
/// To test mutable duplication of type definition members. 
/// 
/// Copy marked nodes: 
///   C {
///     [MarkedForCopy]
///     f1;
///     f2;
///     [MarkedForCopy]
///     m1();
///     m2();
///   }
/// 
/// We will have:
/// 
///   C {
///     f1;
///     f1's new copy;
///     m1;
///     m1's new copy;
///     f2;
///     m2;
///   }
/// 
/// 
/// 
/// </summary>
internal class CopyMarkedNodes : MetadataRewriter {
  internal CopyMarkedNodes(IMetadataHost host)
    : base(host) {
    this.deepCopier = new MetadataDeepCopier(host);
  }

  MetadataDeepCopier deepCopier;

  bool Marked(ITypeDefinitionMember definition) {
    if (definition.Attributes != null) {
      foreach (var attr in definition.Attributes) {
        return true;
      }
    }
    return false;
  }

  public override IAssembly Rewrite(IAssembly assembly) {
    var mutableAssembly = this.deepCopier.Copy(assembly);
    return base.Rewrite(mutableAssembly);
  }

  public override void RewriteChildren(NamedTypeDefinition typeDefinition) {
    List<IDefinition> methodsToDuplicate = new List<IDefinition>();
    List<IMethodDefinition> newMethods = new List<IMethodDefinition>();
    for (int i = 0, n = typeDefinition.Methods == null ? 0 : typeDefinition.Methods.Count; i < n; i++) {
      var member = typeDefinition.Methods[i];
      if (Marked(member)) {
        methodsToDuplicate.Add(member);
      } else {
        newMethods.Add(member);
      }
    }
    this.deepCopier.Copy(methodsToDuplicate);
    foreach (IMethodDefinition m in methodsToDuplicate) {
      newMethods.Add(m);
    }
    typeDefinition.Methods = newMethods;

    List<IDefinition> fieldsToDuplicate = new List<IDefinition>();
    var newFields = new List<IFieldDefinition>();
    for (int i = 0, n = typeDefinition.Fields == null ? 0 : typeDefinition.Fields.Count; i < n; i++) {
      var member = typeDefinition.Fields[i];
      if (Marked(member)) {
        fieldsToDuplicate.Add(member);
      } else
        newFields.Add(member);
    }
    this.deepCopier.Copy(fieldsToDuplicate);
    foreach (IFieldDefinition f in fieldsToDuplicate) {
      newFields.Add(f);
    }
    typeDefinition.Fields = newFields;

    List<IDefinition> propertiesToDuplicate = new List<IDefinition>();
    var newProperties = new List<IPropertyDefinition>();
    for (int i = 0, n = typeDefinition.Properties == null ? 0 : typeDefinition.Properties.Count; i < n; i++) {
      var member = typeDefinition.Properties[i];
      if (Marked(member)) {
        propertiesToDuplicate.Add(member);
      } else newProperties.Add(member);
    }
    this.deepCopier.Copy(propertiesToDuplicate);
    foreach (IPropertyDefinition p in propertiesToDuplicate) {
      newProperties.Add(p);
    }
    typeDefinition.Properties = newProperties;

  }

}

class NameChanger : CodeRewriter {
  MatchEvaluator evaluator;
  Regex pattern;
  public NameChanger(IMetadataHost host, Regex pattern, MatchEvaluator matchEvaluator)
    : base(host) {
    this.evaluator  = matchEvaluator;
    this.pattern = pattern;
  }

  public override INamespaceTypeDefinition Rewrite(INamespaceTypeDefinition namespaceTypeDefinition) {
    var mutableNamespaceTypeDefinition = namespaceTypeDefinition as NamespaceTypeDefinition;
    if (mutableNamespaceTypeDefinition == null) return namespaceTypeDefinition;
    mutableNamespaceTypeDefinition.Name = this.host.NameTable.GetNameFor(this.pattern.Replace(namespaceTypeDefinition.Name.Value, this.evaluator));
    this.RewriteChildren(mutableNamespaceTypeDefinition);
    return mutableNamespaceTypeDefinition;
  }

  public Assembly Change(Assembly assembly) {
    var result = (Assembly)this.Rewrite(assembly);
    ReferenceNameFixer fixer = new ReferenceNameFixer(this.host, this.pattern, this.evaluator, assembly);
    result = (Assembly)fixer.Rewrite(result);
    return result;
  }

  class ReferenceNameFixer : CodeRewriter {
    readonly IAssembly assembly;
    readonly MatchEvaluator evaluator;
    readonly Regex pattern;
    public ReferenceNameFixer(IMetadataHost host, Regex pattern, MatchEvaluator matchEvaluator, IAssembly assembly)
      : base(host) {
      this.assembly = assembly;
      this.evaluator = matchEvaluator;
      this.pattern = pattern;
    }

    public override INamespaceTypeReference Rewrite(INamespaceTypeReference namespaceTypeReference) {
      if (TypeHelper.GetDefiningUnitReference(namespaceTypeReference) != this.assembly) return namespaceTypeReference;
      var mutableNamespaceTypeReference = namespaceTypeReference as Microsoft.Cci.MutableCodeModel.NamespaceTypeReference;
      if (mutableNamespaceTypeReference == null || mutableNamespaceTypeReference.IsFrozen) return namespaceTypeReference;
      object result;
      if (this.referenceRewrites.TryGetValue(mutableNamespaceTypeReference, out result)) return (INamespaceTypeReference)result;
      this.referenceRewrites[mutableNamespaceTypeReference] = mutableNamespaceTypeReference;
      mutableNamespaceTypeReference.Name = this.host.NameTable.GetNameFor(this.pattern.Replace(namespaceTypeReference.Name.Value, this.evaluator));
      this.RewriteChildren(mutableNamespaceTypeReference);
      return mutableNamespaceTypeReference;
    }
  }
}
