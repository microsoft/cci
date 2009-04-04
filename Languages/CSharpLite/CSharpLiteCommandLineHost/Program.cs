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

namespace Microsoft.Cci.CSharp {
  
  public class CSharpCommandLineHost {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args) {
      HostEnvironment hostEnvironment = new HostEnvironment();
      hostEnvironment.Errors += hostEnvironment.HandleErrors;
      CSharpOptions commandLineOptions = OptionParser.ParseCommandLineArguments(hostEnvironment, args);
      if (hostEnvironment.hasError) return;
      if (commandLineOptions.DisplayCommandLineHelp)
        DisplayCommandLineHelp();
      else if (commandLineOptions.RunTestSuite)
        RunTestSuite(commandLineOptions);
      else
        TranslateToExe(commandLineOptions);
    }

    private static void TranslateToExe(CSharpOptions commandLineOptions)
      //^ requires commandLineOptions.FileNames.Count > 0;
    {
      HostEnvironment hostEnvironment = new HostEnvironment();
      hostEnvironment.Errors += hostEnvironment.HandleErrors;
      hostEnvironment.displayFileName = true;
      List<IAssemblyReference> assemblyReferences = GetAssemblyReferences(commandLineOptions, hostEnvironment);
      List<IModuleReference> moduleReferences = new List<IModuleReference>();
      List<CSharpSourceDocument> programSources = new List<CSharpSourceDocument>(1);
      IName name = hostEnvironment.NameTable.GetNameFor(Path.GetFileNameWithoutExtension(commandLineOptions.FileNames[0]));
      CSharpAssembly assem = new CSharpAssembly(name, Path.GetFullPath(name.Value), hostEnvironment, commandLineOptions, assemblyReferences, moduleReferences, programSources);
      CSharpCompilationHelper helper = new CSharpCompilationHelper(assem.Compilation);
      foreach (string fileName in commandLineOptions.FileNames) {
        name = hostEnvironment.NameTable.GetNameFor(fileName);
        StreamReader instream = File.OpenText(fileName);
        programSources.Add(new CSharpSourceDocument(helper, name, Path.GetFullPath(fileName), instream));
      }

      var sourceLocationProvider = assem.Compilation.SourceLocationProvider;
      //var localScopeProvider = assem.Compilation.LocalScopeProvider;
      var pdbWriter = new PdbWriter(Path.ChangeExtension(assem.Location, "pdb"), sourceLocationProvider);
      PeWriter.WritePeToStream(assem, hostEnvironment, File.Create(Path.ChangeExtension(assem.Location, "exe")), sourceLocationProvider, null, pdbWriter);
    }

    private static List<IAssemblyReference> GetAssemblyReferences(CSharpOptions commandLineOptions, HostEnvironment hostEnvironment) {
      List<IAssemblyReference> assemblyReferences = new List<IAssemblyReference>();
      assemblyReferences.Add(hostEnvironment.LoadAssembly(hostEnvironment.CoreAssemblySymbolicIdentity));
      foreach (string assemblyReference in commandLineOptions.ReferencedAssemblies) {
        IUnit unit = hostEnvironment.LoadUnitFrom(assemblyReference);
        if (unit == Dummy.Unit) {
          unit = hostEnvironment.LoadUnitFrom(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), assemblyReference));
        }
        IAssembly/*?*/ aref = unit as IAssembly;
        if (aref != null)
          assemblyReferences.Add(aref);
        else {
          //TODO: error message
        }
      }
      return assemblyReferences;
    }

    private static void DisplayCommandLineHelp() {
      Console.Out.WriteLine("please write something here");
    }

    static void RunTestSuite(CSharpOptions commandLineOptions) {
      foreach (string fileName in commandLineOptions.FileNames)
        RunTestSuite(fileName, commandLineOptions);
    }

    static void RunTestSuite(string fileName, CSharpOptions commandLineOptions) {
      if (Directory.Exists(fileName)) {
        int errorCount = 0;
        foreach (FileInfo fi in new DirectoryInfo(fileName).GetFiles("*", SearchOption.AllDirectories)) {
          if (!RunTestSuite(fi.Name, new StreamReader(fi.Open(FileMode.Open, FileAccess.Read))))
            errorCount++;
        }
        if (errorCount != 0)
          Console.WriteLine("\n\n*** {0} error(s) ***\n", errorCount);
      } else {
        RunTestSuite(fileName, File.OpenText(fileName));
      }
    }

    public static bool RunTestSuite(string suiteName, StreamReader instream) {
      System.Diagnostics.Debug.Listeners.Remove("Default");
      HostEnvironment hostEnvironment = new HostEnvironment();
      hostEnvironment.Errors += hostEnvironment.HandleErrors;
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
            int returnCode = RunTest(hostEnvironment, Path.GetFileNameWithoutExtension(suiteName), source.ToString(), actualOutput, compilerParameters, testCaseParameters);
            if (returnCode != 0)
              actualOutput.Append("Non zero return code: "+returnCode);
          } catch (System.Reflection.TargetInvocationException e) {
            actualOutput.Append(e.InnerException.Message);
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
      return errors == 0;
    }

    private static int RunTest(HostEnvironment hostEnvironment, string suiteName, string test, StringBuilder actualOutput, List<string> compilerParameters, List<string> testCaseParameters) {
      hostEnvironment.hasError = false;
      IName name = hostEnvironment.NameTable.GetNameFor(suiteName);
      CSharpOptions options = new CSharpOptions(); //TODO: extract from params
      List<IAssemblyReference> assemblyReferences = new List<IAssemblyReference>();
      List<IModuleReference> moduleReferences = new List<IModuleReference>();
      assemblyReferences.Add(hostEnvironment.LoadAssembly(hostEnvironment.CoreAssemblySymbolicIdentity));
      IUnit unit;
      CSharpAssembly/*?*/ assem = null;
      CSharpCompilationHelper helper;
      if (hostEnvironment.previousDocument != null && compilerParameters.Contains("/incremental")) {
        unit = hostEnvironment.GetIncrementalUnit(test);
        helper = (CSharpCompilationHelper)hostEnvironment.previousDocument.CSharpCompilationPart.Helper;
      } else {
        List<CSharpSourceDocument> programSources = new List<CSharpSourceDocument>(1);
        assem = new CSharpAssembly(name, "", hostEnvironment, options, assemblyReferences, moduleReferences, programSources);
        helper = new CSharpCompilationHelper(assem.Compilation);
        programSources.Add(hostEnvironment.previousDocument = new CSharpSourceDocument(helper, name, "", test));
        unit = assem;
      }
      if (assem != null && assem.EntryPoint.ResolvedMethod != Dummy.Method) {
        var memStream = new MemoryStream();
        PeWriter.WritePeToStream(assem, hostEnvironment, memStream);
        var runtimeAssembly = System.Reflection.Assembly.Load(memStream.ToArray());
        object result = runtimeAssembly.EntryPoint.Invoke(null, null);
        if (result is int) return (int)result;
        return 0;
      }

      BaseCodeTraverser traverser = new BaseCodeTraverser();
      unit.Dispatch(traverser);
      return 0;
    }
  }

  internal class HostEnvironment : SourceEditHostEnvironment {
    PeReader peReader;
    readonly AssemblyIdentity mscorlibIdentity;
    internal bool hasError = false;
    internal bool displayFileName = false;

    internal HostEnvironment()
      : base(new NameTable(), 4) {
      this.peReader = new PeReader(this);
      string/*?*/ loc = typeof(object).Assembly.Location;
      if (loc == null) {
        loc = string.Empty;
      }
      this.mscorlibIdentity =
      new AssemblyIdentity(
          this.NameTable.GetNameFor("mscorlib"),
          string.Empty,
          new Version(2, 0, 0, 0),
          new byte[] { 0xB7, 0x7A, 0x5C, 0x56, 0x19, 0x34, 0xE0, 0x89 },
          loc
        );
      this.RegisterAsLatest(this.peReader.OpenAssembly(BinaryDocument.GetBinaryDocumentForFile(this.mscorlibIdentity.Location, this)));
    }

    internal void HandleErrors(object sender, Microsoft.Cci.ErrorEventArgs args) {
      foreach (IErrorMessage error in args.Errors) {
        ISourceLocation/*?*/ sourceLocation = error.Location as ISourceLocation;
        if (sourceLocation == null) continue;
        if (!error.IsWarning) hasError = true;
        CompositeSourceDocument/*?*/ compositeDocument = sourceLocation.SourceDocument as CompositeSourceDocument;
        if (compositeDocument != null) {
          foreach (ISourceLocation sl in compositeDocument.GetFragmentLocationsFor(sourceLocation)) {
            sourceLocation = sl;
            break;
          }
        }
        IPrimarySourceLocation/*?*/ primarySourceLocation = sourceLocation as IPrimarySourceLocation;
        if (primarySourceLocation == null) {
          Console.Out.WriteLine(error.Message);
          continue;
        }
        string docName = primarySourceLocation.SourceDocument.Name.Value;
        int startLine = primarySourceLocation.StartLine;
        int startColumn = primarySourceLocation.StartColumn;
        int endLine = primarySourceLocation.EndLine;
        int endColumn = primarySourceLocation.EndColumn;
        IncludedSourceLocation/*?*/ includedSourceLocation = primarySourceLocation as IncludedSourceLocation;
        if (includedSourceLocation != null) {
          docName = includedSourceLocation.OriginalSourceDocumentName;
          startLine = includedSourceLocation.OriginalStartLine;
          endLine = includedSourceLocation.OriginalEndLine;
        }
        if (!displayFileName) docName = "";
        Console.Out.WriteLine("{6}({0},{1})-({2},{3}): {4}: {5}", startLine, startColumn, endLine, endColumn,
          error.IsWarning ? "warning" : "error", error.Message, docName);
        foreach (ILocation relatedLocation in error.RelatedLocations) {
          ISourceLocation/*?*/ sloc = relatedLocation as ISourceLocation;
          if (sloc != null) {
            compositeDocument = sloc.SourceDocument as CompositeSourceDocument;
            if (compositeDocument != null) {
              foreach (ISourceLocation sl in compositeDocument.GetFragmentLocationsFor(sloc)) {
                sloc = sl;
                break;
              }
            }
            primarySourceLocation = sloc as IPrimarySourceLocation;
            if (primarySourceLocation == null) continue;
            docName = primarySourceLocation.SourceDocument.Name.Value;
            startLine = primarySourceLocation.StartLine;
            startColumn = primarySourceLocation.StartColumn;
            endLine = primarySourceLocation.EndLine;
            endColumn = primarySourceLocation.EndColumn;
            includedSourceLocation = primarySourceLocation as IncludedSourceLocation;
            if (includedSourceLocation != null) {
              docName = includedSourceLocation.OriginalSourceDocumentName;
              startLine = includedSourceLocation.OriginalStartLine;
              endLine = includedSourceLocation.OriginalEndLine;
            }
            Console.Out.WriteLine("({0},{1})-({2},{3}): (Location of symbol related to previous {4}.)", startLine, startColumn, endLine, endColumn, error.IsWarning ? "warning" : "error");
          }
          //TODO: deal with non source locations
        }
      }
    }

    internal IUnit GetIncrementalUnit(string newText) {
      string[] lines = newText.Split('$');
      if (lines.Length != 4) return Dummy.Unit;
      string prefix = lines[0];
      string textToReplace = lines[1];
      string replacement = lines[2];
      ICSharpSourceDocument updatedDocument = this.previousDocument.GetUpdatedDocument(prefix.Length, textToReplace.Length, replacement);
      return updatedDocument.CSharpCompilationPart.Compilation.Result;
    }

    internal CSharpSourceDocument/*?*/ previousDocument;

    protected override AssemblyIdentity GetCoreAssemblySymbolicIdentity() {
      return this.mscorlibIdentity;
    }

    public override IUnit LoadUnitFrom(string location) {
      if (!File.Exists(location)) return Dummy.Unit;
      IModule result = this.peReader.OpenModule(BinaryDocument.GetBinaryDocumentForFile(location, this));
      if (result == Dummy.Module) return Dummy.Unit;
      this.RegisterAsLatest(result);
      return result;
    }

    public override void ReportErrors(Microsoft.Cci.ErrorEventArgs errorEventArguments) {
      this.SynchronousReportErrors(errorEventArguments);
    }
  }
}