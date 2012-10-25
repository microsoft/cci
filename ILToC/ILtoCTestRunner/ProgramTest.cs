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
using ILtoC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Diagnostics;
using Microsoft.VisualC;
using Microsoft.Win32;
//using DummyObject = xxxx::System.Object;

namespace ILtoCTestRunner {


  /// <summary>
  ///This is a test class for ProgramTest and is intended
  ///to contain all ProgramTest Unit Tests
  ///</summary>
  [TestClass()]
  public class ProgramTest {

    //private DummyObject dummy = null;


    private TestContext testContextInstance;

    static string folderName = "";

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext {
      get {
        return testContextInstance;
      }
      set {
        testContextInstance = value;
      }
    }

    #region Additional test attributes
    // 
    //You can use the following additional attributes as you write your tests:
    //
    //Use ClassInitialize to run code before running the first test in the class
    [ClassInitialize()]
    public static void MyClassInitialize(TestContext testContext) {
      ExtractResource("ILtoCTestRunner.TestData.mscorlib.dll", "mscorlib.dll");
      ExtractResource("ILtoCTestRunner.TestData.mscorlib.pdb", "mscorlib.pdb");
      string[] args = { "mscorlib.dll" };
      Program_Accessor.Main(args);
      RegistryKey regkey = Registry.CurrentUser;
      regkey = regkey.OpenSubKey("Software\\Microsoft\\VisualStudio\\10.0_Config");
      string shellFolder = regkey.GetValue("ShellFolder") as string;

      String pwd = Directory.GetCurrentDirectory() as string;

      string compileCmd = "\"" + shellFolder + "VC\\bin\\cl.exe\" mscorlib.c OverflowChecker.c Platform_msvc.c Platform.c /c /Zi";
      string temp = Path.ChangeExtension(Path.GetRandomFileName(), ".bat"); ;
      File.WriteAllText(temp, "\"" + shellFolder + "VC\\vcvarsall.bat\" & " + compileCmd + "\r\n");
      ProcessStartInfo processSI = new ProcessStartInfo(@temp);
      processSI.RedirectStandardError = true;
      processSI.RedirectStandardOutput = true;
      processSI.WindowStyle = ProcessWindowStyle.Hidden;
      processSI.UseShellExecute = false;
      Process process = Process.Start(processSI);
      StreamReader stdout = process.StandardOutput;
      process.WaitForExit(10000);
      if (process.HasExited) {
      } else {
        Assert.Fail("Compiling mscorlib.c and OverflowChecker.c did not finish executing in 10000 milliseconds");
      }
      Assert.IsTrue(File.Exists("mscorlib.obj"), "Failed to compile mscorlib.c");
      Assert.IsTrue(File.Exists("OverflowChecker.obj"), "Failed to compile OverflowChecker.c");
      Assert.IsTrue(File.Exists("Platform_msvc.obj"), "Failed to compile Platform_msvc.c");
      Assert.IsTrue(File.Exists("Platform.obj"), "Failed to compile Platform.c");
    }
    //
    //Use ClassCleanup to run code after all tests in a class have run
    //[ClassCleanup()]
    //public static void MyClassCleanup()
    //{
    //}
    //
    //Use TestInitialize to run code before running each test
    //[TestInitialize()]
    //public void MyTestInitialize()
    //{
    //}
    //
    //Use TestCleanup to run code after each test has run
    //[TestCleanup()]
    //public void MyTestCleanup()
    //{
    //}
    //
    #endregion

    [TestMethod()]
    public void Arrays() {
      ProgramTest.folderName = "";
      CompileTranslateAndRun("Arrays.cs");
    }

    [TestMethod()]
    public void Casts() {
      ProgramTest.folderName = "";
      CompileTranslateAndRun("Cast.cs");
    }

    [TestMethod()]
    public void Delegates() {
      ProgramTest.folderName = "";
      CompileTranslateAndRun("Delegates.cs");
    }

    [TestMethod()]
    public void ExceptionHandling() {
      ProgramTest.folderName = "";
      CompileTranslateAndRun("ExceptionHandling.cs");
    }

    [TestMethod()]
    public void ModifiedIL() {
      ProgramTest.folderName = "ModifiedIL.";
      CompileTranslateAndRun("Ckfinite.il");
      CompileTranslateAndRun("ExclicitOverride.il");
      CompileTranslateAndRun("SimpleOveriddenMethod.il");
      CompileTranslateAndRun("Cpobj.il");
      CompileTranslateAndRun("Cpblk.il");
      CompileTranslateAndRun("NoPrefix.exe");
    }

    [TestMethod()]
    public void SimpleInstructions() {
      ProgramTest.folderName = "Simple.";
      CompileTranslateAndRun("Add.cs");
      CompileTranslateAndRun("Div.cs");
    }

    [TestMethod()]
    public void Static() {
      ProgramTest.folderName = "Static.";
      CompileTranslateAndRun("StaticFieldAccess.cs");
    }

    [TestMethod()]
    public void OverflowCheckInstructions() {
      ProgramTest.folderName = "Overflow.";
      CompileTranslateAndRun("CheckedAddInt32.cs");
      CompileTranslateAndRun("CheckedMultiplyInt32.cs");
      CompileTranslateAndRun("CheckedConvert.cs");
    }

    [TestMethod()]
    public void Strings() {
      ProgramTest.folderName = "Strings.";
      CompileTranslateAndRun("SimpleStrings.cs");
    }

    [TestMethod()]
    public void Threads() {
      ProgramTest.folderName = "";
      CompileTranslateAndRun("Threads.cs");
    }

    [TestMethod()]
    public void VirtualCalls() {
      ProgramTest.folderName = "VirtualCalls.";
      CompileTranslateAndRun("VirtualCalls.cs");
    }

    [TestMethod()]
    public void Generics() {
      ProgramTest.folderName = "";
      CompileTranslateAndRun("Generics.cs");
    }

    static void CompileTranslateAndRun(string sourceFile) {


      string assemblyName = Path.ChangeExtension(sourceFile, ".exe");

      string tempFile = Path.GetRandomFileName();
      
      bool assembleIL = Path.GetExtension(sourceFile).Equals(".il");
      bool isExe = Path.GetExtension(sourceFile).Equals(".exe");
      RegistryKey regkey = Registry.CurrentUser;
      regkey = regkey.OpenSubKey("Software\\Microsoft\\VisualStudio\\10.0_Config");
      string shellFolder = regkey.GetValue("ShellFolder") as string;
      string generatedExeName = "Generated" + assemblyName;

      if (!isExe) {
        ExtractResource("ILtoCTestRunner.TestData." + folderName + sourceFile, tempFile);
        if (assembleIL) {
          string assembleCmd = "ilasm " + tempFile + " /OUTPUT=" + assemblyName;
          createAndRunBat(shellFolder, tempFile, assemblyName, assembleCmd, "IL file");
        } else {
          CompilerParameters parameters = new CompilerParameters();
          parameters.GenerateExecutable = true;
          parameters.IncludeDebugInformation = true;
          parameters.OutputAssembly = assemblyName;
          parameters.CompilerOptions += " -unsafe";

          CompilerResults results;
          using (CodeDomProvider icc = new CSharpCodeProvider()) {
            results = icc.CompileAssemblyFromFile(parameters, tempFile);
          }

          foreach (var s in results.Errors) {
            Debug.WriteLine(s);
          }

          Assert.AreEqual(0, results.Errors.Count);
          Assert.IsTrue(File.Exists(assemblyName), string.Format("Failed to compile {0}", sourceFile));
        }
      } else {
        ExtractResource("ILtoCTestRunner.TestData." + folderName + sourceFile, assemblyName);
      }

      string[] args = { assemblyName };
      Program_Accessor.Main(args);

      string generatedFileName = Path.ChangeExtension(sourceFile, ".c");
      Assert.IsTrue(File.Exists(generatedFileName), string.Format("Failed to translate {0}", generatedFileName));

      string compileCmd = "cl /Zi -Fe" + generatedExeName + " " + generatedFileName + " mscorlib.obj OverflowChecker.obj Platform_msvc.obj Platform.obj";
      string output;
      ProgramTest.createAndRunBat(shellFolder, generatedExeName, generatedFileName, compileCmd, "translated c file");

      ProcessStartInfo processSI = new ProcessStartInfo(generatedExeName);
      processSI.RedirectStandardOutput = true;
      processSI.WindowStyle = ProcessWindowStyle.Hidden;
      processSI.UseShellExecute = false;
      Process process = Process.Start(processSI);
      StreamReader stdout = process.StandardOutput;
      process.WaitForExit(2000);
      if (process.HasExited) {
        output = stdout.ReadToEnd();
        if (process.ExitCode > 0) {
          Assert.Fail("Compiled c program for {0} returned exit code {1}", generatedFileName, process.ExitCode);
        }
      } else {
        Assert.Fail("Compiled c program for {0} did not finish executing in 2000 milliseconds", generatedFileName);
      }
      
    }

    private static void createAndRunBat(string shellFolder, string target, string source, string cmd, string comment) {
      string temp = Path.ChangeExtension(Path.GetRandomFileName(), ".bat"); ;
      File.WriteAllText(temp, "\"" + shellFolder + "VC\\vcvarsall.bat\" & " + cmd + "\r\n");
      ProcessStartInfo processSI = new ProcessStartInfo(@temp);
      processSI.RedirectStandardError = true;
      processSI.RedirectStandardOutput = true;
      processSI.WindowStyle = ProcessWindowStyle.Hidden;
      processSI.UseShellExecute = false;
      processSI.CreateNoWindow = true;
      Process process = Process.Start(processSI);
      StreamReader stdout = process.StandardOutput;

      process.WaitForExit(10000);
      if (process.HasExited) {
      } else {
        Assert.Fail(comment + " for {0} did not finish executing in 10000 milliseconds", source);
      }

      Assert.IsTrue(File.Exists(target), string.Format("Failed to compile " + comment + " {0}", source));
      File.Delete(temp);
    }

    static void ExtractResource(string resource, string targetFile) {
      System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();
      using (Stream srcStream = a.GetManifestResourceStream(resource)) {
        byte[] bytes = new byte[srcStream.Length];
        srcStream.Read(bytes, 0, bytes.Length);
        File.WriteAllBytes(targetFile, bytes);
      }
    }
  }
}
