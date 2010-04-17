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

  //[Fact]
  public void SystemCoreWithCode() {
    ExtractResource("CodeModelRoundtripTests.TestData.v4.System.Core.dll", "System.Core.dll");
    ExtractResource("CodeModelRoundtripTests.TestData.v4.System.Core.pdb", "System.Core.pdb");

    RoundTripWithCodeMutator("System.Core.dll", "System.Core.pdb");
  }

  [Fact]
  public void MutableCopy1() {
    ExtractAndCompile("MutableCopyTest.cs");
    RoundTripWithMetadataCopier("MutableCopyTest.dll", "MutableCopyTest.pdb");
  }

  [Fact]
  public void MutableCopy2() {
    ExtractAndCompile("IteratorRoundTripTest.cs");
    RoundTripWithMetadataCopier("IteratorRoundTripTest.dll", "IteratorRoundTripTest.pdb");
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

  //[Fact]
  //public void DecompilationTest1() {
  //  ExtractAndCompile("MutableCopyTest.cs");
  //  RoundTripNoCopyTestDecompilation("MutableCopyTest.dll", "MutableCopyTest.pdb");
  //}

  //[Fact]
  //public void DecompilationTest2() {
  //  ExtractAndCompile("IteratorRoundTripTest.cs");
  //  RoundTripNoCopyTestDecompilation("IteratorRoundTripTest.dll", "IteratorRoundTripTest.pdb");
  //}

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
    this.RoundTripCopyAndExecute("TestClass1.exe", "TestClass1.pdb");
  }

  [Fact]
  public void CodeCopyRewriteAndExecute1() {
    ExtractAndCompileExe("TestClass1.cs");
    this.RoundTripCopyRewriteAndExecute("TestClass1.exe", "TestClass1.pdb");
  }

  CodeAndContractMutator CreateCodeMutator(IAssembly assembly, string pdbName) {
    return new CodeAndContractMutator(host, pdbReader, null);
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

  void VisitAndMutate(MetadataMutator mutator, ref IAssembly assembly) {
    assembly = mutator.Visit(mutator.GetMutableCopy(assembly));
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

  void RoundTripWithMetadataCopier(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        List<INamedTypeDefinition> list;
        MetadataCopier copier1 = new MetadataCopier(host, codeAssembly, out list);
        codeAssembly = (Assembly)copier1.Substitute(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Visit(codeAssembly);
        Assert.True(checker.Errors.Count == 0);
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
    PeVerify.Assert(expectedResult, PeVerify.VerifyAssembly(assembly.Location));
  }

  void RoundTripMutableCopyAndAddGenericParameter2(string assemblyName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
    List<INamedTypeDefinition> list;
    MetadataCopier copier1 = new MetadataCopier(host, codeAssembly, out list);
    codeAssembly = (Assembly)copier1.Substitute(codeAssembly);
    for (int i = 0; i < 30; i++) {
      AddGenericParameters adder = new AddGenericParameters(host, codeAssembly.AllTypes, i);
      codeAssembly = (Assembly)adder.Visit(codeAssembly);
    }
    AssertWriteToPeFile(expectedResult, codeAssembly, null);
  }

  void RoundTripMutableCopyAndAddGenericParameter(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        List<INamedTypeDefinition> list;
        MetadataCopier copier1 = new MetadataCopier(host, codeAssembly, out list);
        codeAssembly = (Assembly)copier1.Substitute(codeAssembly);
        AddGenericParameters adder = new AddGenericParameters(host, codeAssembly.AllTypes, 0);
        codeAssembly = (Assembly)adder.Visit(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Visit(codeAssembly);
        Assert.True(checker.Errors.Count == 0);
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
        var adder = new AddGenericMethodParameters(host);
        codeAssembly = (Assembly)adder.Visit(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Visit(codeAssembly);
        Assert.True(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
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
        codeAssembly = (Assembly)adder.Visit(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Visit(codeAssembly);
        Assert.True(checker.Errors.Count == 0);
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
        var codeAssembly = adder.Visit(assembly);
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
          CodeCopier copier = new CodeCopier(host, null);
          List<INamedTypeDefinition> ignore;
          copier.AddDefinition(codeAssembly, out ignore);
          codeAssembly.AllTypes = ignore;
          codeAssembly = (Assembly)copier.Substitute(codeAssembly);
          Checker checker = new Checker(this.host);
          checker.Visit(codeAssembly);
          Assert.True(checker.Errors.Count == 0);
          AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
        }
      }
    } else {
      var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, null);
      CodeCopier copier = new CodeCopier(host, null);
      List<INamedTypeDefinition> ignore;
      copier.AddDefinition(codeAssembly, out ignore);
      codeAssembly.AllTypes = ignore;
      codeAssembly = (Assembly)copier.Substitute(codeAssembly);
      Checker checker = new Checker(this.host);
      checker.Visit(codeAssembly);
      Assert.True(allowCheckerFail || checker.Errors.Count == 0);
      AssertWriteToPeFile(expectedResult, codeAssembly, null);
    }
  }

  void RoundTripCopyAndExecute(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    string expectedOutput = Execute(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        CodeCopier copier = new CodeCopier(host, null);
        List<INamedTypeDefinition> ignore;
        copier.AddDefinition(codeAssembly, out ignore);
        codeAssembly.AllTypes = ignore;
        codeAssembly = (Assembly)copier.Substitute(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Visit(codeAssembly);
        Assert.True(checker.Errors.Count == 0);
        AssertWriteToPeFile(expectedResult, codeAssembly, pdbReader);
        AssertExecute(expectedOutput, assemblyName);
      }
    }
  }

  void RoundTripCopyRewriteAndExecute(string assemblyName, string pdbName) {
    PeVerifyResult expectedResult = PeVerify.VerifyAssembly(assemblyName);
    string expectedOutput = Execute(assemblyName);
    IAssembly assembly = LoadAssembly(assemblyName);
    using (var f = File.OpenRead(pdbName)) {
      using (var pdbReader = new PdbReader(f, host)) {
        var codeAssembly = Decompiler.GetCodeModelFromMetadataModel(this.host, assembly, pdbReader);
        CodeCopier copier = new CodeCopier(host, null);
        List<INamedTypeDefinition> ignore;
        copier.AddDefinition(codeAssembly, out ignore);
        codeAssembly.AllTypes = ignore;
        codeAssembly = (Assembly)copier.Substitute(codeAssembly);
        NameChanger namechanger = new NameChanger(this.host, new Regex("[A-Za-z0-9_]*"), new MatchEvaluator(this.eval));
        codeAssembly = namechanger.Change(codeAssembly);
        Checker checker = new Checker(this.host);
        checker.Visit(codeAssembly);
        Assert.True(checker.Errors.Count == 0);
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

/// <summary>
/// Add a generic parameter to a selected type. 
/// </summary>
internal class AddGenericParameters : MutatingVisitor {
  internal AddGenericParameters(IMetadataHost host, List<INamedTypeDefinition> alltypes, int number)
    : base(host) {
    this.allTypes = alltypes;
    this.number = number;
  }
  List<INamedTypeDefinition> allTypes;
  int number;
  bool once = false;

  protected List<INamedTypeDefinition> WithMoreGenericParameters(List<INamedTypeDefinition> ssc) {
    Helper helper = new Helper(this.host);
    foreach (var member in ssc) {
      List<INamedTypeDefinition> list;
      helper.AddDefinition(member, out list);
      foreach (var t in list) this.allTypes.Add(t);
      helper.AddGenericParamInPlace(member);
    }
    List<INamedTypeDefinition> result = new List<INamedTypeDefinition>();
    foreach (var member in ssc) {
      INamedTypeDefinition copy;
      INestedTypeDefinition nested = member as INestedTypeDefinition;
      if (nested != null) {
        copy = helper.Substitute(nested);
        // Change the references
        var fixer = new ReferenceFixer(this.host, member, copy);
        fixer.Visit((INestedTypeDefinition)copy);
      } else {
        INamespaceTypeDefinition nType = member as INamespaceTypeDefinition;
        copy = helper.Substitute(nType);
        // Change the references
        var fixer = new ReferenceFixer(this.host, member, copy);
        fixer.Visit((INamespaceTypeDefinition)copy);
      }
      result.Add(copy);
    }
    return result;
  }

  internal class Helper : MetadataCopier {
    internal Helper(IMetadataHost host)
      : base(host) {
    }
    public Dictionary<uint, IName> nameMapping = new Dictionary<uint, IName>();
    public void AddGenericParamInPlace(INamedTypeDefinition old) {
      if (old.IsGeneric) {
        INamedTypeDefinition copy = null;
        INestedTypeDefinition nested = old as INestedTypeDefinition;
        if (nested != null) {
          var nestedCopy = (NestedTypeDefinition)this.cache[old];
          var oneMoreGP = new List<IGenericTypeParameter>();
          var gp1 = new GenericTypeParameter() {
            Name = this.host.NameTable.GetNameFor("_SX_"),
            DefiningType = nestedCopy,
            Index = 0
          };
          oneMoreGP.Add(gp1);
          this.cache.Add(gp1, gp1);
          foreach (var gp in old.GenericParameters) {
            var newgp = new GenericTypeParameter();
            newgp.Copy(gp, this.host.InternFactory);
            newgp.Index = (ushort)(gp.Index + 1);
            this.cache[gp] = newgp;
            this.cache.Add(newgp, newgp);
            oneMoreGP.Add(newgp);
          }
          nestedCopy.GenericParameters = oneMoreGP;
          nestedCopy.Name = this.host.NameTable.GetNameFor(old.Name.Value + "<<>>");
          copy = nestedCopy;
        } else {
          INamespaceTypeDefinition nType = old as INamespaceTypeDefinition;
          if (nType != null) {
            var nscopy = (NamespaceTypeDefinition)this.cache[old];
            var oneMoreGP = new List<IGenericTypeParameter>();
            var gp1 = new GenericTypeParameter() {
              Name = this.host.NameTable.GetNameFor("_SX_"),
              DefiningType = nscopy,
              Index = 0
            };
            oneMoreGP.Add(gp1);
            this.cache.Add(gp1, gp1);
            foreach (var gp in old.GenericParameters) {
              var newgp = new GenericTypeParameter();
              newgp.Copy(gp, this.host.InternFactory);
              newgp.Index = (ushort)(gp.Index + 1);
              this.cache[gp] = newgp;
              this.cache.Add(newgp, newgp);
              oneMoreGP.Add(newgp);
            }
            nscopy.GenericParameters = oneMoreGP;
            nscopy.Name = this.host.NameTable.GetNameFor(old.Name.Value + "<<>>");
            copy = nscopy;
          }
        }
        if (copy == null) throw new Exception();
        this.nameMapping.Add(old.InternedKey, copy.Name);
      }
    }
  }
  internal class ReferenceFixer : MutatingVisitor {
    internal ReferenceFixer(IMetadataHost host, INamedTypeDefinition root, INamedTypeDefinition newRoot)
      : base(host) {
      this.root = root;
      this.newRoot = newRoot;
    }
    Dictionary<object, object> alreadyAdded = new Dictionary<object, object>();
    INamedTypeDefinition root;
    INamedTypeDefinition newRoot;

    public override IGenericTypeInstanceReference Mutate(IGenericTypeInstanceReference genericTypeInstanceReference) {
      genericTypeInstanceReference = base.Mutate(genericTypeInstanceReference);
      INamedTypeReference nestedTypeReference = genericTypeInstanceReference.GenericType as INamedTypeReference;
      if (nestedTypeReference != null && nestedTypeReference.InternedKey == this.newRoot.InternedKey) {
        if (!alreadyAdded.ContainsKey(genericTypeInstanceReference)) {
          var newArgs = new List<ITypeReference>();
          IGenericTypeParameter gtp = null;
          foreach (var gp in this.newRoot.GenericParameters) {
            gtp = gp; break;
          }
          newArgs.Add(gtp);
          foreach (var arg in genericTypeInstanceReference.GenericArguments) {
            newArgs.Add(arg);
          }
          var result = MutableModelHelper.GetGenericTypeInstanceReference(newArgs, genericTypeInstanceReference.GenericType, this.host.InternFactory, genericTypeInstanceReference);
          alreadyAdded.Add(result, result);
          return result;
        }
      }
      return genericTypeInstanceReference;
    }

    public override IGenericTypeParameterReference Mutate(IGenericTypeParameterReference genericTypeParameterReference) {
      genericTypeParameterReference = base.Mutate(genericTypeParameterReference);
      INamedTypeReference nested = genericTypeParameterReference.DefiningType as INamedTypeReference;
      if (nested != null && nested.InternedKey == this.newRoot.InternedKey) {
        foreach (var gp in this.newRoot.GenericParameters) {
          if (genericTypeParameterReference.Name == gp.Name) {
            var result = MutableModelHelper.GetGenericTypeParameterReference(nested, genericTypeParameterReference.Name, gp.Index, this.host.InternFactory, genericTypeParameterReference);
            return result;
          }
        }
      }
      return genericTypeParameterReference;
    }

    public override INestedTypeReference Mutate(INestedTypeReference nestedTypeReference) {
      nestedTypeReference = base.Mutate(nestedTypeReference);
      if (this.root.InternedKey == nestedTypeReference.InternedKey) {
        var result = MutableModelHelper.GetNestedTypeReference(nestedTypeReference.ContainingType, this.newRoot.GenericParameterCount, nestedTypeReference.MangleName, this.newRoot.Name, this.host.InternFactory, nestedTypeReference);
        return result;
      }
      return nestedTypeReference;
    }

    public override INamespaceTypeReference Mutate(INamespaceTypeReference namespaceTypeReference) {
      namespaceTypeReference = base.Mutate(namespaceTypeReference);
      if (this.root.InternedKey == namespaceTypeReference.InternedKey) {
        var result = MutableModelHelper.GetNamespaceTypeReference(namespaceTypeReference.ContainingUnitNamespace, this.newRoot.GenericParameterCount, namespaceTypeReference.MangleName, this.newRoot.Name,
          this.host.InternFactory, namespaceTypeReference);
        return result;
      }
      return namespaceTypeReference;
    }

    public override ISpecializedNestedTypeReference Mutate(ISpecializedNestedTypeReference specializedNestedTypeReference) {
      specializedNestedTypeReference = base.Mutate(specializedNestedTypeReference);
      if (specializedNestedTypeReference.Name.UniqueKey != specializedNestedTypeReference.UnspecializedVersion.Name.UniqueKey) {
        var result = MutableModelHelper.GetSpecializedNestedTypeReference(specializedNestedTypeReference.ContainingType, specializedNestedTypeReference.UnspecializedVersion.GenericParameterCount, specializedNestedTypeReference.MangleName,
          specializedNestedTypeReference.UnspecializedVersion.Name, specializedNestedTypeReference.UnspecializedVersion, this.host.InternFactory, specializedNestedTypeReference);
        return result;
      }
      return specializedNestedTypeReference;
    }
    public override IEventDefinition Visit(IEventDefinition eventDefinition) {
      return base.Visit(eventDefinition);
    }
  }
  public override NestedUnitNamespace Mutate(NestedUnitNamespace unitNamespace) {
    if (!once) {
      var newMembers = new List<INamedTypeDefinition>();
      List<INamedTypeDefinition> ssc = new List<INamedTypeDefinition>();
      int count = 0;
      for (int i = 0, n = unitNamespace.Members.Count; i < n; i++) {
        INamespaceTypeDefinition typ = unitNamespace.Members[i] as INamespaceTypeDefinition;
        if (typ != null && typ.IsGeneric && !typ.Name.Value.Contains("<<>>")) {
          once = true;
          if (count == this.number) {
            ssc.Add(typ);
            break;
          }
          count++;
        }
      }
      if (ssc.Count > 0) {
        newMembers.AddRange(this.WithMoreGenericParameters(ssc));
        foreach (var m in newMembers) {
          unitNamespace.Members.Add((INamespaceTypeDefinition)m);
        }
      }
    }
    return base.Mutate(unitNamespace);
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
internal class AddGenericMethodParameters : MutatingVisitor {
  internal AddGenericMethodParameters(IMetadataHost host)
    : base(host) {
  }

  public override NamespaceTypeDefinition Mutate(NamespaceTypeDefinition namespaceTypeDefinition) {
    NamespaceTypeDefinition mutableType = namespaceTypeDefinition;
    var newMembers = new System.Collections.Generic.List<IMethodDefinition>();
    List<IMethodDefinition> ssc = new List<IMethodDefinition>();
    for (int i = 0, n = mutableType.Methods.Count; i < n; i++) {
      var member = mutableType.Methods[i];
      if (member.IsGeneric) {
        ssc.Add(member);
      }
    }
    this.WithMoreGenericParameters(ssc, mutableType.Methods);
    foreach (var m in newMembers) {
      mutableType.Methods.Add(m);
    }
    return base.Mutate(mutableType);
  }

  public override NestedTypeDefinition Mutate(NestedTypeDefinition nestedTypeDefinition) {
    NestedTypeDefinition mutableType = nestedTypeDefinition;
    var newMembers = new System.Collections.Generic.List<IMethodDefinition>();
    List<IMethodDefinition> ssc = new List<IMethodDefinition>();
    for (int i = 0, n = mutableType.Methods.Count; i < n; i++) {
      var member = mutableType.Methods[i];
      if (member.IsGeneric) {
        ssc.Add(member);
      }
    }
    this.WithMoreGenericParameters(ssc, mutableType.Methods);
    foreach (var m in newMembers) {
      mutableType.Methods.Add(m);
    }
    return base.Mutate(mutableType);
  }

  private void WithMoreGenericParameters(List<IMethodDefinition> ssc, List<IMethodDefinition> result) {
    Helper helper = new Helper(this.host);
    foreach (var member in ssc) {
      List<INamedTypeDefinition> l;
      helper.AddDefinition(member, out l);
      helper.PopulateCache(member);
    }
    var list = new List<IMethodDefinition>();
    foreach (var member in ssc) {
      var copy = helper.Substitute(member);
      result.Add(copy);
      list.Add(copy);
    }
    foreach (var copy in list) {
      var fixer = new ReferenceFixer(this.host, copy, helper.nameMapping, helper.methodCopies);
      fixer.Visit(copy);
    }
  }

  internal class Helper : MetadataCopier {
    internal Helper(IMetadataHost host)
      : base(host) {
    }
    public Dictionary<int, IMethodDefinition> methodCopies = new Dictionary<int, IMethodDefinition>();
    public Dictionary<int, int> nameMapping = new Dictionary<int, int>();
    public MethodDefinition PopulateCache(IMethodDefinition old) {
      if (old.IsGeneric) {
        var copy = new MethodDefinition();
        copy.Copy(old, this.host.InternFactory);
        var oneMoreGP = new List<IGenericMethodParameter>();
        var gp1 = new GenericMethodParameter();
        gp1.Name = this.host.NameTable.GetNameFor("_SX_");
        gp1.DefiningMethod = copy;
        gp1.Index = 0;
        gp1.InternFactory = this.host.InternFactory;
        oneMoreGP.Add(gp1);
        this.cache.Add(gp1, gp1);
        foreach (var gp in old.GenericParameters) {
          var newgp = new GenericMethodParameter();
          newgp.Copy(gp, this.host.InternFactory);
          newgp.Index = (ushort)(gp.Index + 1);
          this.cache[gp] = newgp;
          this.cache.Add(newgp, newgp);
          oneMoreGP.Add(newgp);
        }
        copy.GenericParameters = oneMoreGP;
        copy.Name = this.host.NameTable.GetNameFor(old.Name.Value + "<<>>");
        this.methodCopies.Add(copy.Name.UniqueKey, copy);
        this.nameMapping.Add(old.Name.UniqueKey, copy.Name.UniqueKey);
        this.cache[old] = copy;
        this.cache.Add(copy, copy);
        return copy;
      }
      return null;
    }
  }
  class ReferenceFixer : MutatingVisitor {
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

    public override Microsoft.Cci.MutableCodeModel.GenericMethodInstanceReference Mutate(Microsoft.Cci.MutableCodeModel.GenericMethodInstanceReference genericMethodInstanceReference) {
      genericMethodInstanceReference = base.Mutate(genericMethodInstanceReference);
      if (genericMethodInstanceReference.Name.UniqueKey != genericMethodInstanceReference.GenericMethod.Name.UniqueKey) {
        genericMethodInstanceReference.Name = genericMethodInstanceReference.GenericMethod.Name;
      }
      if (genericMethodInstanceReference.Name.Value.EndsWith("<<>>")) {
        if (!alreadyAdded.ContainsKey(genericMethodInstanceReference)) {
          var newArgs = new List<ITypeReference>();
          // add the first generic parameter of the current method.
          foreach (var gmpr in root.GenericParameters) {
            newArgs.Add(gmpr);
            break;
          }
          foreach (var arg in genericMethodInstanceReference.GenericArguments) {
            newArgs.Add(arg);
          }
          genericMethodInstanceReference.GenericArguments = newArgs;
          alreadyAdded.Add(genericMethodInstanceReference, genericMethodInstanceReference);
        }
      }
      return genericMethodInstanceReference;
    }

    public override Microsoft.Cci.MutableCodeModel.MethodReference Mutate(Microsoft.Cci.MutableCodeModel.MethodReference methodReference) {
      methodReference = base.Mutate(methodReference);
      if (this.nameMapping.ContainsKey(methodReference.Name.UniqueKey)) {
        int newMethodNameKey = this.nameMapping[methodReference.Name.UniqueKey];
        methodReference.Name = this.newMethods[newMethodNameKey].Name;
      }
      return methodReference;
    }

    public override SpecializedMethodReference Mutate(SpecializedMethodReference specializedMethodReference) {
      specializedMethodReference = base.Mutate(specializedMethodReference);
      if (specializedMethodReference.Name.UniqueKey != specializedMethodReference.UnspecializedVersion.Name.UniqueKey) {
        specializedMethodReference.Name = specializedMethodReference.UnspecializedVersion.Name;
      }
      return specializedMethodReference;
    }

    public override Microsoft.Cci.MutableCodeModel.GenericMethodParameterReference Mutate(Microsoft.Cci.MutableCodeModel.GenericMethodParameterReference genericMethodParameterReference) {
      var result = base.Mutate(genericMethodParameterReference);
      if (result.DefiningMethod.Name.Value.Contains("<<>>")) {
        var definingMethodDef = this.newMethods[result.DefiningMethod.Name.UniqueKey];
        foreach (var gpm in definingMethodDef.GenericParameters) {
          if (gpm.Name.UniqueKey == genericMethodParameterReference.Name.UniqueKey) {
            genericMethodParameterReference.Index = gpm.Index;
            break;
          }
        }
      }
      return result;
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
internal class CopyMarkedNodes : MutatingVisitor {
  internal CopyMarkedNodes(IMetadataHost host)
    : base(host) {
  }

  bool Marked(ITypeDefinitionMember definition) {
    if (definition.Attributes != null) {
      foreach (var attr in definition.Attributes) {
        return true;
      }
    }
    return false;
  }

  public override NamespaceTypeDefinition Mutate(NamespaceTypeDefinition namespaceTypeDefinition) {
    var mutableType = namespaceTypeDefinition;
    List<IMethodDefinition> methodsToDuplicate = new List<IMethodDefinition>();
    List<IMethodDefinition> newMethods = new List<IMethodDefinition>();
    for (int i = 0, n = mutableType.Methods.Count; i < n; i++) {
      var member = mutableType.Methods[i];
      if (Marked(member)) {
        methodsToDuplicate.Add(member);
      } else {
        newMethods.Add(member);
      }
    }
    MetadataCopier helper = new MetadataCopier(this.host);
    foreach (var m in methodsToDuplicate) {
      List<INamedTypeDefinition> list;
      helper.AddDefinition(m, out list);
    }
    foreach (var m in methodsToDuplicate) {
      var newm = helper.Substitute(m);
      newMethods.Add(newm);
    }
    mutableType.Methods = newMethods;
    List<IFieldDefinition> fieldsToDuplicate = new List<IFieldDefinition>();
    var newFields = new List<IFieldDefinition>();
    for (int i = 0, n = mutableType.Fields.Count; i < n; i++) {
      var member = mutableType.Fields[i];
      if (Marked(member)) {
        fieldsToDuplicate.Add(member);
      } else
        newFields.Add(member);
    }
    helper = new MetadataCopier(this.host);
    foreach (var f in fieldsToDuplicate) {
      List<INamedTypeDefinition> list;
      helper.AddDefinition(f, out list);
    }
    foreach (var f in fieldsToDuplicate) {
      var newf = helper.Substitute(f);
      newFields.Add(newf);
    }
    mutableType.Fields = newFields;
    List<IPropertyDefinition> propertiesToDuplicate = new List<IPropertyDefinition>();
    var newProperties = new List<IPropertyDefinition>();
    for (int i = 0, n = mutableType.Properties.Count; i < n; i++) {
      var member = mutableType.Properties[i];
      if (Marked(member)) {
        propertiesToDuplicate.Add(member);
      } else newProperties.Add(member);
    }
    helper = new MetadataCopier(this.host);
    foreach (var p in propertiesToDuplicate) {
      List<INamedTypeDefinition> list;
      helper.AddDefinition(p, out list);
    }
    foreach (var p in propertiesToDuplicate) {
      var newp = helper.Substitute(p);
      newProperties.Add(newp);
    }
    mutableType.Properties = newProperties;
    return base.Mutate(mutableType);
  }

  public override NestedTypeDefinition Mutate(NestedTypeDefinition nestedTypeDefinition) {
    NestedTypeDefinition mutableType = nestedTypeDefinition;
    List<IMethodDefinition> methodsToDuplicate = new List<IMethodDefinition>();
    List<IMethodDefinition> newMethods = new List<IMethodDefinition>();
    for (int i = 0, n = mutableType.Methods.Count; i < n; i++) {
      var member = mutableType.Methods[i];
      if (Marked(member)) {
        methodsToDuplicate.Add(member);
      } else {
        newMethods.Add(member);
      }
    }
    MetadataCopier helper = new MetadataCopier(this.host);
    foreach (var m in methodsToDuplicate) {
      List<INamedTypeDefinition> list;
      helper.AddDefinition(m, out list);
    }
    foreach (var m in methodsToDuplicate) {
      var newm = helper.Substitute(m);
      newMethods.Add(newm);
    }
    mutableType.Methods = newMethods;
    List<IFieldDefinition> fieldsToDuplicate = new List<IFieldDefinition>();
    var newFields = new List<IFieldDefinition>();
    for (int i = 0, n = mutableType.Fields.Count; i < n; i++) {
      var member = mutableType.Fields[i];
      if (Marked(member)) {
        fieldsToDuplicate.Add(member);
      } else
        newFields.Add(member);
    }
    helper = new MetadataCopier(this.host);
    foreach (var f in fieldsToDuplicate) {
      List<INamedTypeDefinition> ignore;
      helper.AddDefinition(f, out ignore);
    }
    foreach (var f in fieldsToDuplicate) {
      var newf = helper.Substitute(f);
      newFields.Add(newf);
    }
    mutableType.Fields = newFields;
    List<IPropertyDefinition> propertiesToDuplicate = new List<IPropertyDefinition>();
    var newProperties = new List<IPropertyDefinition>();
    for (int i = 0, n = mutableType.Properties.Count; i < n; i++) {
      var member = mutableType.Properties[i];
      if (Marked(member)) {
        propertiesToDuplicate.Add(member);
      } else newProperties.Add(member);
    }
    helper = new MetadataCopier(this.host);
    foreach (var p in propertiesToDuplicate) {
      List<INamedTypeDefinition> list;
      helper.AddDefinition(p, out list);
    }
    foreach (var p in propertiesToDuplicate) {
      var newp = helper.Substitute(p);
      newProperties.Add(newp);
    }
    mutableType.Properties = newProperties;
    return base.Mutate(mutableType);
  }
}

// Ideally will use metadatatraverser. Use mutatingvisitor here to test it. 
internal class FindItemsToCopy : MutatingVisitor {
  IAssembly assembly;
  internal FindItemsToCopy(IMetadataHost host, IAssembly assembly)
    : base(host, true) {
    this.assembly = assembly;
  }
  enum CopiableNode {
    Module, GenericTypeParameter, GenericMethodParameter, ParameterDefinition, GlobalMethodDefinition,
    GlobalFieldDefinition, RootUnitNamespace, NestedUnitNamespace
  }
  CopiableNode target;
  internal void FindModuleToCopy() {
    this.target = CopiableNode.Module;
    this.copier = new MetadataCopier(this.host);
    this.Visit((IModule)assembly);
  }
  internal void FindGenericTypeParameterToCopy() {
    this.target = CopiableNode.GenericTypeParameter;
    this.Visit(assembly);
  }
  internal void FindGenericMethodParameterToCopy() {
    this.target = CopiableNode.GenericMethodParameter;
    this.Visit(assembly);
  }
  internal void FindParameterDefinitionToCopy() {
    this.target = CopiableNode.ParameterDefinition;
    this.Visit(assembly);
  }
  internal void FindGlobalFieldDefinitionToCopy() {
    this.target = CopiableNode.GlobalFieldDefinition;
    this.Visit(assembly);
  }
  internal void FindGlobalMethodDefinitionToCopy() {
    this.target = CopiableNode.GlobalMethodDefinition;
    this.Visit(assembly);
  }
  internal void FindRootUnitNamespaceToCopy() {
    this.target = CopiableNode.RootUnitNamespace;
    this.Visit(assembly);
  }
  internal void FindNestedUnitNamespaceToCopy() {
    this.target = CopiableNode.NestedUnitNamespace;
    this.Visit(assembly);
  }

  MetadataCopier copier;

  public override IModule Visit(IModule module) {
    if (this.target == CopiableNode.Module) {
      List<INamedTypeDefinition> list;
      this.copier.AddDefinition(module, out list);
      copier.Substitute(module);
    }
    return base.Visit(module);
  }

  public override INestedUnitNamespace Visit(INestedUnitNamespace nestedUnitNamespace) {
    if (this.target == CopiableNode.NestedUnitNamespace) {
      List<INamedTypeDefinition> list;
      MetadataCopier copier = new MetadataCopier(host, nestedUnitNamespace, out list);
      return copier.Substitute(nestedUnitNamespace);
    }
    return base.Visit(nestedUnitNamespace);
  }

  public override IRootUnitNamespace Visit(IRootUnitNamespace rootUnitNamespace) {
    if (this.target == CopiableNode.RootUnitNamespace) {
      List<INamedTypeDefinition> list;
      MetadataCopier copier = new MetadataCopier(host, rootUnitNamespace, out list);
      return copier.Substitute(rootUnitNamespace);
    }
    return base.Visit(rootUnitNamespace);
  }

  public override IGlobalFieldDefinition Visit(IGlobalFieldDefinition globalFieldDefinition) {
    if (this.target == CopiableNode.GlobalFieldDefinition) {
      List<INamedTypeDefinition> list;
      MetadataCopier copier = new MetadataCopier(host, globalFieldDefinition, out list);
      return copier.Substitute(globalFieldDefinition);
    }
    return base.Visit(globalFieldDefinition);
  }

  public override IGlobalMethodDefinition Visit(IGlobalMethodDefinition globalMethodDefinition) {
    if (this.target == CopiableNode.GlobalMethodDefinition) {
      List<INamedTypeDefinition> list;
      MetadataCopier copier = new MetadataCopier(host, globalMethodDefinition, out list);
      return copier.Substitute(globalMethodDefinition);
    }
    return base.Visit(globalMethodDefinition);
  }

  public override IParameterDefinition Visit(IParameterDefinition parameterDefinition) {
    if (this.target == CopiableNode.ParameterDefinition) {
      List<INamedTypeDefinition> list;
      MetadataCopier copier = new MetadataCopier(host, parameterDefinition, out list);
      return copier.Substitute(parameterDefinition);
    }
    return base.Visit(parameterDefinition);
  }

  public override IGenericMethodParameter Visit(IGenericMethodParameter genericMethodParameter) {
    if (this.target == CopiableNode.GenericMethodParameter) {
      List<INamedTypeDefinition> list;
      MetadataCopier copier = new MetadataCopier(host, genericMethodParameter, out list);
      return copier.Substitute(genericMethodParameter);
    }
    return base.Visit(genericMethodParameter);
  }

  public override IGenericTypeParameter Visit(IGenericTypeParameter genericTypeParameter) {
    if (this.target == CopiableNode.GenericTypeParameter) {
      List<INamedTypeDefinition> list;
      MetadataCopier copier = new MetadataCopier(host, genericTypeParameter, out list);
      return copier.Substitute(genericTypeParameter);
    }
    return base.Visit(genericTypeParameter);
  }

  public override ITypeReference Visit(ITypeReference typeReference) {
    if (this.target == CopiableNode.Module) {
      this.copier.Substitute(typeReference);
    }
    return base.Visit(typeReference);
  }

  public override IAssemblyReference Visit(IAssemblyReference assemblyReference) {
    if (this.target == CopiableNode.Module) {
      this.copier.Substitute(assemblyReference);
    }
    return base.Visit(assemblyReference);
  }

  public override IFieldReference Visit(IFieldReference fieldReference) {
    if (this.target == CopiableNode.Module) {
      this.copier.Substitute(fieldReference);
    }
    return base.Visit(fieldReference);
  }

  public override IMethodReference Visit(IMethodReference methodReference) {
    if (this.target == CopiableNode.Module) {
      this.copier.Substitute(methodReference);
    }
    return base.Visit(methodReference);
  }
}
class NameChanger : CodeMutatingVisitor {
  MatchEvaluator evaluator;
  Regex pattern;
  Dictionary<uint, uint> InternedKeysOfChangedTypeDef = new Dictionary<uint, uint>();
  public NameChanger(IMetadataHost host, Regex pattern, MatchEvaluator matchEvaluator)
    : base(host) {
    this.evaluator = matchEvaluator;
    this.pattern = pattern;
  }

  public override NamespaceTypeDefinition Mutate(NamespaceTypeDefinition namespaceTypeDefinition) {
    if (!this.InternedKeysOfChangedTypeDef.ContainsKey(namespaceTypeDefinition.InternedKey)) {
      this.InternedKeysOfChangedTypeDef.Add(namespaceTypeDefinition.InternedKey, namespaceTypeDefinition.InternedKey);
      namespaceTypeDefinition.Name = this.host.NameTable.GetNameFor(this.pattern.Replace(namespaceTypeDefinition.Name.Value, this.evaluator));
    } else {
      // we changed one name to another?
    }
    return base.Mutate(namespaceTypeDefinition);
  }

  public Assembly Change(IAssembly assembly) {
    var result = (Assembly)this.Visit(assembly);
    ReferenceNameFixer fixer = new ReferenceNameFixer(this.host, this.pattern, this.evaluator, this.InternedKeysOfChangedTypeDef);
    result = (Assembly)fixer.Visit(result);
    return result;
  }

  class ReferenceNameFixer : CodeMutatingVisitor {
    readonly MatchEvaluator evaluator;
    readonly Regex pattern;
    readonly Dictionary<uint, uint> InternedKeysOfChangedTypeDef = new Dictionary<uint, uint>();
    public ReferenceNameFixer(IMetadataHost host, Regex pattern, MatchEvaluator matchEvaluator, Dictionary<uint, uint> InternedKeysOfChangedTypeDef)
      : base(host) {
      this.evaluator = matchEvaluator;
      this.pattern = pattern;
      this.InternedKeysOfChangedTypeDef = InternedKeysOfChangedTypeDef;
    }
    public override INamespaceTypeReference Mutate(INamespaceTypeReference namespaceTypeReference) {
      var result = base.Mutate(namespaceTypeReference);
      if (this.InternedKeysOfChangedTypeDef.ContainsKey(result.InternedKey)) {
        var name = this.host.NameTable.GetNameFor(this.pattern.Replace(namespaceTypeReference.Name.Value, this.evaluator));
        if (name != namespaceTypeReference.Name) {
          return MutableModelHelper.GetNamespaceTypeReference(result.ContainingUnitNamespace, result.GenericParameterCount, result.MangleName, result.Name, this.host.InternFactory, result);
        }
      }
      return result;
    }
  }
}

