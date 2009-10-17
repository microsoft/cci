//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Cci.Ast;

namespace Microsoft.Cci.SmallBasic {

  public static class SmallBasicCommandLineHost {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main(string[] args) {
      if (args == null || args.Length == 0) return 0;
      if (0 < args.Length && args[0] == "/break") {
        string[] newArgs = new string[args.Length - 1];
        Array.Copy(args, 1, newArgs, 0, newArgs.Length);
        args = newArgs;
        System.Diagnostics.Debugger.Break();
      }
      string fileName = args[args.Length-1];
      if (fileName.EndsWith(".sb")) {
        SmallBasicCommandLineHost.Compile(fileName);
      } else {
        SmallBasicCommandLineHost.RunSuite(fileName);
      }
      return 0;
    }

    private static void Compile(string fileName) {
      HostEnvironment hostEnvironment = new HostEnvironment();
      IName name = hostEnvironment.NameTable.GetNameFor(fileName);
      IDictionary<string, string> options = new Dictionary<string, string>();
      List<IAssemblyReference> assemblyReferences = new List<IAssemblyReference>();
      List<IModuleReference> moduleReferences = new List<IModuleReference>();
      assemblyReferences.Add(hostEnvironment.LoadAssembly(hostEnvironment.CoreAssemblySymbolicIdentity));
      assemblyReferences.Add((IAssembly)hostEnvironment.LoadUnitFrom(typeof(Microsoft.SmallBasic.Library.ConsoleWindow).Assembly.Location));
      StreamReader instream = File.OpenText(fileName);
      List<SmallBasicDocument> programSources = new List<SmallBasicDocument>(1);
      SmallBasicAssembly assem = new SmallBasicAssembly(name, Path.GetFullPath(fileName), hostEnvironment, options, assemblyReferences, moduleReferences, programSources);
      SmallBasicCompilationHelper helper = new SmallBasicCompilationHelper(assem.Compilation);
      programSources.Add(new SmallBasicDocument(helper, name, Path.GetFullPath(fileName), instream));
      var exeFile = File.Create(Path.ChangeExtension(fileName, "exe"));
      var sourceLocationProvider = assem.Compilation.SourceLocationProvider;
      //var localScopeProvider = assem.Compilation.LocalScopeProvider;
      using (var pdbWriter = new PdbWriter(Path.ChangeExtension(fileName, "pdb"), sourceLocationProvider)) {
        PeWriter.WritePeToStream(assem, hostEnvironment, exeFile, sourceLocationProvider, null, pdbWriter);
      }
    }

    private static void RunSuite(string suiteName) {
      SmallBasicCommandLineHost.RunSuite(suiteName, File.OpenText(suiteName));
    }

    public static void RunSuite(string suiteName, TextReader instream) {
      System.Diagnostics.Debug.Listeners.Remove("Default");
      HostEnvironment hostEnvironment = new HostEnvironment();
      hostEnvironment.Errors += HandleErrors;
      StringBuilder source = null;
      StringBuilder expectedOutput = null;
      StringBuilder actualOutput = null;
      List<string> suiteParameters = new List<string>();
      List<string> compilerParameters = null;
      List<string> testCaseParameters = null;
      int errors = 0;
      try {
        int ch = instream.Read();
        int line = 1;
        while (ch >= 0) {
          compilerParameters = new List<string>(suiteParameters);
          bool skipTest = false;
          if (ch == '`') {
            ch = instream.Read();
            bool parametersAreForEntireSuite = false;
            if (ch == '`') {
              parametersAreForEntireSuite = true;
              ch = instream.Read();
            }
            while (ch == '/') {
              //compiler parameters
              StringBuilder cParam = new StringBuilder();
              do {
                cParam.Append((char)ch);
                ch = instream.Read();
              } while (ch != '/' && ch != 0 && ch != 10 && ch != 13);
              for (int i = cParam.Length-1; i >= 0; i--) {
                if (!Char.IsWhiteSpace(cParam[i])) break;
                cParam.Length = i;
              }
              string cp = cParam.ToString();
              compilerParameters.Add(cp);
            }
            if (parametersAreForEntireSuite)
              suiteParameters.AddRange(compilerParameters);
            if (ch == 13) ch = instream.Read();
            if (ch == 10) {
              line++;
              ch = instream.Read();
              if (parametersAreForEntireSuite && ch == '`') continue;
            }
          }
          if (ch == ':') {
            ch = instream.Read();
            while (ch == '=') {
              //test case parameters
              StringBuilder tcParam = new StringBuilder();
              ch = instream.Read(); //discard =
              while (ch != '=' && ch != 0 && ch != 10 && ch != 13) {
                tcParam.Append((char)ch);
                ch = instream.Read();
              }
              for (int i = tcParam.Length-1; i >= 0; i--) {
                if (!Char.IsWhiteSpace(tcParam[i])) break;
                tcParam.Length = i;
              }
              if (testCaseParameters == null) testCaseParameters = new List<string>();
              testCaseParameters.Add(tcParam.ToString());
            }
            if (ch == 13) ch = instream.Read();
            if (ch == 10) {
              ch = instream.Read();
              line++;
            }
          }
          source = new StringBuilder();
          while (ch >= 0 && ch != '`') {
            source.Append((char)ch);
            ch = instream.Read();
            if (ch == 10) line++;
          }
          if (ch < 0) {
            Console.WriteLine("The last test case in the suite has not been provided with expected output");
            errors++;
            break;
          }
          ch = instream.Read();
          if (ch == 13) ch = instream.Read();
          if (ch == 10) {
            line++;
            ch = instream.Read();
          }
          int errLine = line;
          expectedOutput = new StringBuilder();
          while (ch >= 0 && ch != '`') {
            expectedOutput.Append((char)ch);
            ch = instream.Read();
            if (ch == 10) line++;
          }
          if (expectedOutput.Length > 0 && expectedOutput[expectedOutput.Length-1] == 10)
            expectedOutput.Length -= 1;
          if (expectedOutput.Length > 0 && expectedOutput[expectedOutput.Length-1] == 13)
            expectedOutput.Length -= 1;
          ch = instream.Read();
          if (ch == 13) ch = instream.Read();
          if (ch == 10) {
            ch = instream.Read();
            line++;
          }
          if (skipTest) continue;
          actualOutput = new StringBuilder();
          TextWriter savedOut = Console.Out;
          Console.SetOut(new StringWriter(actualOutput));
          System.Diagnostics.TextWriterTraceListener myWriter = new System.Diagnostics.TextWriterTraceListener(System.Console.Out);
          System.Diagnostics.Debug.Listeners.Add(myWriter);
          try {
            RunTest(hostEnvironment, Path.GetFileNameWithoutExtension(suiteName), source.ToString(), actualOutput, compilerParameters, testCaseParameters);
          } catch (Exception e) {
            actualOutput.Append(e.Message);
          }
          compilerParameters = null;
          testCaseParameters = null;
          Console.SetOut(savedOut);
          System.Diagnostics.Debug.Listeners.Remove(myWriter);
          if (actualOutput.Length > 0 && actualOutput[actualOutput.Length - 1] == 10)
            actualOutput.Length -= 1;
          if (actualOutput.Length > 0 && actualOutput[actualOutput.Length - 1] == 13)
            actualOutput.Length -= 1;
          if (!expectedOutput.ToString().Equals(actualOutput.ToString())) {
            if (errors++ == 0) Console.WriteLine(suiteName+" failed\n");
            Console.WriteLine("source({0}):", errLine);
            if (source != null)
              Console.WriteLine(source);
            Console.WriteLine("actual output:");
            Console.WriteLine(actualOutput);
            Console.WriteLine("expected output:");
            if (expectedOutput != null)
              Console.WriteLine(expectedOutput);
          }
        }
        instream.Close();
        if (errors == 0)
          Console.WriteLine(suiteName+" passed");
        else {
          Console.WriteLine();
          Console.WriteLine(suiteName+" had "+errors+ (errors > 1 ? " failures" : " failure"));
        }
      } catch {
        Console.WriteLine(suiteName+" failed\n");
        Console.WriteLine("source:");
        if (source != null)
          Console.WriteLine(source);
        Console.WriteLine("actual output:");
        Console.WriteLine(actualOutput);
        Console.WriteLine("expected output:");
        if (expectedOutput != null)
          Console.WriteLine(expectedOutput);
      }
    }

    private static void RunTest(HostEnvironment hostEnvironment, string suiteName, string test, StringBuilder actualOutput, List<string> compilerParameters, List<string> testCaseParameters) {
      IName name = hostEnvironment.NameTable.GetNameFor(suiteName);
      IDictionary<string, string> options = new Dictionary<string, string>();
      List<IAssemblyReference> assemblyReferences = new List<IAssemblyReference>();
      List<IModuleReference> moduleReferences = new List<IModuleReference>();
      assemblyReferences.Add(hostEnvironment.LoadAssembly(hostEnvironment.CoreAssemblySymbolicIdentity));
      assemblyReferences.Add((IAssembly)hostEnvironment.LoadUnitFrom(typeof(Microsoft.SmallBasic.Library.ConsoleWindow).Assembly.Location));
      SmallBasicAssembly/*?*/ assem = null;
      SmallBasicCompilationHelper helper;
      List<SmallBasicDocument> programSources = new List<SmallBasicDocument>(1);
      assem = new SmallBasicAssembly(name, "", hostEnvironment, options, assemblyReferences, moduleReferences, programSources);
      helper = new SmallBasicCompilationHelper(assem.Compilation);
      programSources.Add(/*hostEnvironment.previousDocument = */new SmallBasicDocument(helper, name, "", test));
      var memStream = new MemoryStream();
      PeWriter.WritePeToStream(assem, hostEnvironment, memStream);
      var runtimeAssembly = System.Reflection.Assembly.Load(memStream.ToArray());
      runtimeAssembly.EntryPoint.Invoke(null, null);
    }

    private static void HandleErrors(object sender, Microsoft.Cci.ErrorEventArgs args) {
      foreach (IErrorMessage error in args.Errors) {
        IPrimarySourceLocation/*?*/ sourceLocation = error.Location as IPrimarySourceLocation;
        if (sourceLocation == null) continue;
        Console.Out.WriteLine("{0}({1},{2})-({3},{4}): {5} {6}{7}: {8}", sourceLocation.SourceDocument.Location, sourceLocation.StartLine, sourceLocation.StartColumn, sourceLocation.EndLine, sourceLocation.EndColumn,
          "error", error.ErrorReporterIdentifier, error.Code.ToString(), error.Message);
      }
    }
  }

  internal class HostEnvironment : SourceEditHostEnvironment {
    PeReader peReader;
    internal HostEnvironment()
      : base(new NameTable(), 4) {
      this.peReader = new PeReader(this);
      string/*?*/ loc = typeof(object).Assembly.Location;
      if (loc == null) loc = "";
      System.Reflection.AssemblyName coreAssemblyName = new System.Reflection.AssemblyName(typeof(object).Assembly.FullName);
      this.coreAssemblySymbolicIdentity =
        new AssemblyIdentity(this.NameTable.GetNameFor(coreAssemblyName.Name), "", coreAssemblyName.Version, coreAssemblyName.GetPublicKeyToken(), loc);
      this.RegisterAsLatest(this.peReader.OpenAssembly(BinaryDocument.GetBinaryDocumentForFile(loc, this)));
      loc = typeof(Microsoft.SmallBasic.Library.ConsoleTextColor).Assembly.Location;
      if (loc == null) loc = "";
      //System.Reflection.AssemblyName runtimeName = new System.Reflection.AssemblyName(typeof(Microsoft.SmallBasic.Library.ConsoleTextColor).Assembly.FullName);
      //this.smallBasicRuntimeAssemblyIdentity =
      //  new AssemblyIdentity(this.NameTable.GetNameFor(runtimeName.Name), "", runtimeName.Version, runtimeName.GetPublicKeyToken(), loc);
      this.RegisterAsLatest(this.peReader.OpenAssembly(BinaryDocument.GetBinaryDocumentForFile(loc, this)));

    }

    //internal SmallBasicDocument/*?*/ previousDocument;

    AssemblyIdentity coreAssemblySymbolicIdentity;

    protected override AssemblyIdentity GetCoreAssemblySymbolicIdentity() {
      return this.coreAssemblySymbolicIdentity;
    }

    //public AssemblyIdentity SmallBasicRuntimeAssemblyIdentity {
    //  get { return this.smallBasicRuntimeAssemblyIdentity; }
    //}
    //readonly AssemblyIdentity smallBasicRuntimeAssemblyIdentity;

    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }

    public override void ReportErrors(Microsoft.Cci.ErrorEventArgs errorEventArguments) {
      this.SynchronousReportErrors(errorEventArguments);
    }
  }
}

