using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using ILGarbageCollect.Sweep;
using ILGarbageCollect.Mark;
using ILGarbageCollect.Summaries;
using ILGarbageCollect.Reasons;

namespace ILGarbageCollect {

  /// <summary>
  /// A class that contains the options for the ILGarbageCollector tool.
  /// </summary>
  class ILGarbageCollectorOptions : OptionParsing {

    [OptionDescription("Mode in which to run the collector.\n" +
      "\t\tMark: Run analysis and write to report directory.\n" +
      "\t\tSweep: Remove unused code using report from previous Mark run.\n" +
      "\t\tMarkAndSweep: Perform analysis and remove unused code at the same time.")]
    public RunMode mode = RunMode.MarkAndSweep;

    [OptionDescription("In Mark phase, directory to which to write report. In Sweep phase, directory from which to read report.")]
    public string report = "";

    [OptionDescription("In Sweep phase, directory in which to write transformed binaries.")]
    public string transform = "ILGarbageCollectTransformedBinaries";


    [OptionDescription("What to do with unreachable methods.\n" +
      "\t\tRemove: Attempt to remove methods, when possible. At the moment, this may cause the resulting binary to not verify.\n" +
      "\t\tStubs: Leave behind minimal stubs.\n" +
      "\t\tDebug: Replace body with code that emits debugging information when called.\n" +
      "\t\tNone: Don't remove methods at all, but still rewrite the binaries. This is basically a sanity check.")]
    public MethodRemoval removal = MethodRemoval.Stubs;

    [OptionDescription("File containing methods to consider as entry points specified in Doc Comment format, one per line." +
                        "If no entry points are specified, the main assembly's entry point will be used. If this flag is present, the main entry points will be ignored.")]
    public string entrypoints = null;

    [OptionDescription("File containing attribute types (in Doc Comment format, one per line) that mark a method as an entry point.")]
    public string entryattributes = null;

    // t-devinc: Maybe some day we can auto detect this from the binaries?
    [OptionDescription("Profile under which the code will run.")]
    public TargetProfile profile = TargetProfile.Desktop;

    [OptionDescription("File containing reflection summaries.")]
    public string summaries = null;
  }

  public enum TargetProfile {
    Desktop,
    Phone
  }

  public enum RunMode {
    Mark,
    Sweep,
    MarkAndSweep
  }

  public enum MethodRemoval {
    Remove,
    Stubs,
    Debug,
    None
  }

  class Program {

    /// <summary>
    /// Parse command line options and returns an ILGarbageCollector object on sucess.  
    /// On error, this method prints help information and returns null
    /// </summary>
    /// <param name="args">The array of command line arguments</param>
    /// <returns>On success: a populated ILGarbageCollector. On failure: null.</returns>
    private static ILGarbageCollectorOptions ParseOptions(string[] args) {
      var options = new ILGarbageCollectorOptions();
      options.Parse(args);

      if (options.HelpRequested) {
        options.PrintOptions("");
        return null;
      }

      if (options.HasErrors) {
        options.PrintErrorsAndExit(Console.Out);
        return null;
      }

      return options;
    }

    static Stopwatch stopwatch = new Stopwatch();
      
    static int Main(string[] args) {

      // populate options with command line args
      var options = Program.ParseOptions(args);
      if (options == null) return -1;


      stopwatch.Start();

      using (var host = GetHostFromOptions(options)) {
        ISet<IAssembly> rootAssemblies = GetRootAssembliesFromOptions(options, host);

        WholeProgram wholeProgram = new WholeProgram(rootAssemblies, host);

        string reportPath = "ILGarbageCollectReport";
        if (!String.IsNullOrWhiteSpace(options.report))
          reportPath = options.report + @"\" + reportPath;


        bool performMark = (options.mode == RunMode.Mark || options.mode == RunMode.MarkAndSweep);
        bool performSweep = (options.mode == RunMode.Sweep || options.mode == RunMode.MarkAndSweep);

        if (performMark) {
          IEnumerable<IMethodReference> entryPoints = GetEntryPointsFromOptions(options, wholeProgram);

          IEnumerable<IMethodSummarizer> reflectionSummarizers = GetReflectionSummarizersFromOptions(options, wholeProgram);
          RunRTA(wholeProgram, reflectionSummarizers, entryPoints, reportPath, options.profile);
        }

        if (performSweep) {
          string transformPath = options.transform;

          TransformProgram(wholeProgram, transformPath, reportPath, options.removal, options.profile);
        }

      }

      Console.WriteLine("Elapsed time: {0}", stopwatch.Elapsed);

      return 0;
    }

    private static MetadataReaderHost GetHostFromOptions(ILGarbageCollectorOptions options) {
      MetadataReaderHost host;

      switch (options.profile) {
        case TargetProfile.Phone:
          host = new PhoneMetadataHost(GetRootAssemblyParentDirectoriesFromOptions(options));
          break;

        default:
          host = new PeReader.DefaultHost();
          break;
      }

      return host;
    }

    private static ISet<string> GetRootAssemblyPathsFromOptions(ILGarbageCollectorOptions options) {
      return new HashSet<string>(options.GeneralArguments);
    }

    private static ISet<string> GetRootAssemblyParentDirectoriesFromOptions(ILGarbageCollectorOptions options) {
      return new HashSet<string>(GetRootAssemblyPathsFromOptions(options).Select(assemblyPath => Path.GetDirectoryName(assemblyPath)));
    }

    private static ISet<IAssembly> GetRootAssembliesFromOptions(ILGarbageCollectorOptions options, IMetadataReaderHost host) {
      ISet<IAssembly> rootAssemblies = new HashSet<IAssembly>();

      foreach (var pathToAssembly in GetRootAssemblyPathsFromOptions(options)) {
        var assembly = host.LoadUnitFrom(pathToAssembly) as IAssembly;
        if (assembly == null) {
          throw new FileNotFoundException("Couldn't load assembly " + pathToAssembly);
        }

        rootAssemblies.Add(assembly);
      }

      return rootAssemblies;
    }

    private static IEnumerable<IMethodReference> GetEntryPointsFromOptions(ILGarbageCollectorOptions options, WholeProgram wholeProgram) {

      ISet<IEntryPointDetector> entryPointDetectors = new HashSet<IEntryPointDetector>();

      bool ignoreMainEntryPoints = false;

      if (options.entrypoints != null) {
        entryPointDetectors.Add(new DocCommentFileEntryPointDetector(options.entrypoints));
        ignoreMainEntryPoints = true;
      }

      if (options.entryattributes != null) {
        entryPointDetectors.Add(new AttributeFileEntryPointDetector(options.entryattributes));
      }

      // If no entrypoints were directly specified are used, we'll just get the entry points
      // from the main assemblies.
      if (!ignoreMainEntryPoints) {
        entryPointDetectors.Add(new RootAssembliesEntryPointDetector());
      }

      ISet<IMethodReference> entryPoints = new HashSet<IMethodReference>();

      foreach (IEntryPointDetector entryPointDetector in entryPointDetectors) {
        entryPoints.UnionWith(entryPointDetector.GetEntryPoints(wholeProgram));
      }

      if (entryPoints.Count() == 0) {
        Console.WriteLine("Error: Could not find any entry points.");
        System.Environment.Exit(-1);
      }

      return entryPoints;
    }

    private static IEnumerable<IMethodSummarizer> GetReflectionSummarizersFromOptions(ILGarbageCollectorOptions options, WholeProgram wholeProgram) {

      ISet<IMethodSummarizer> summarizers = DefaultReflectionSummarizers();

      string textSummariesPath = options.summaries;

      if (textSummariesPath != null) {
        TextFileMethodSummarizer textFileSummarizer = TextFileMethodSummarizer.CreateSummarizerFromPath(textSummariesPath, wholeProgram);
        summarizers.Add(textFileSummarizer);
      }

      return summarizers;
    }

    private static bool AssemblyShouldBeRewritten(IAssembly assembly) {

      // Fragile

      if (GarbageCollectHelper.AssemblyMayBeSystemOrFramework(assembly)) {
        return false;
      }

      return true;
    }



    private static RapidTypeAnalysis RunRTA(WholeProgram wholeProgram,
                                            IEnumerable<ILGarbageCollect.Summaries.IMethodSummarizer> reflectionSummarizers,
                                            IEnumerable<IMethodReference> entryPoints,
                                            string reportPath,
                                            TargetProfile profile) {





      var rta = new RapidTypeAnalysis(wholeProgram, profile);

      rta.ReflectionSummarizers = reflectionSummarizers;


      Console.WriteLine("Running Rapid Type Analysis with {0} entry points", entryPoints.Count());



      rta.Run(entryPoints);

      stopwatch.Stop();

      OutputRTAStatistics(rta, reportPath);


      return rta;
    }

    // Eventually we'll get rid of these
    private static ISet<IMethodSummarizer> DefaultReflectionSummarizers() {
      ISet<IMethodSummarizer> summarizers = new HashSet<IMethodSummarizer>();
      //summarizers.Add(new CciSummarizer());


      return summarizers;
    }

    private const string ConstructedTypesFileName = "ConstructedTypes.txt";



    private const string ConstructedGenericParametersFileName = "ConstructedGenericParameters.txt";

    private const string ReflectionSummaryRequiredFileName = "ReflectionSummaryRequired.txt";

    private const string UnresolvedReferencesFileName = "UnresolvedReferences.txt";

    private static void OutputRTAStatistics(RapidTypeAnalysis rta, string reportingDirectory) {
      Console.WriteLine("Writing mark report to {0}", reportingDirectory);

      System.IO.Directory.CreateDirectory(reportingDirectory);

      OutputPerAssemblyReports(rta, reportingDirectory);

      OutputWholeProgramReports(rta, reportingDirectory);

      OutputAnalysisReasons(rta, reportingDirectory);

    }

    private static void OutputPerAssemblyReports(RapidTypeAnalysis rta, string reportingDirectory) {
      foreach (IAssembly assembly in rta.WholeProgram().AllAssemblies()) {
        AssemblyReport report = AssemblyReport.CreateAssemblyReportFromRTA(assembly, rta);

        string assemblyName = assembly.Name.Value;
        int reachableMethodsCount = report.ReachableMethods.Count;

        report.WriteReportToDirectory(reportingDirectory);
      }
    }

    private static void OutputWholeProgramReports(RapidTypeAnalysis rta, string reportingDirectory) {
      using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + ConstructedTypesFileName)) {
        foreach (var constructedType in rta.ConstructedTypes()) {
          outfile.WriteLine(constructedType);
        }
      }




      using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + ConstructedGenericParametersFileName)) {
        foreach (var constructedParameter in rta.ConstructedGenericParameters()) {
          if (constructedParameter is IGenericTypeParameter) {
            outfile.WriteLine("{0} from type {1}", constructedParameter, ((IGenericTypeParameter)constructedParameter).DefiningType);
          }
          else {
            outfile.WriteLine("{0} from method {1}", constructedParameter, ((IGenericMethodParameter)constructedParameter).DefiningMethod);
          }
        }
      }

      ISet<IMethodDefinition> methodsRequiringReflectionSummary = rta.MethodsRequiringReflectionSummary();

      if (methodsRequiringReflectionSummary.Count() > 0) {
        Console.WriteLine("Found {0} methods requiring a reflection summary. List written in report directory to {1}",
           methodsRequiringReflectionSummary.Count(),
           ReflectionSummaryRequiredFileName);

        using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + ReflectionSummaryRequiredFileName)) {
          foreach (var constructedType in methodsRequiringReflectionSummary) {
            outfile.WriteLine(constructedType);
          }
        }
      }

      ISet<IReference> unresolvedReferences = rta.UnresolvedReferences();

      if (unresolvedReferences.Count() > 0) {
        Console.WriteLine("Found {0} unresolved references. List written in report directory to {1}",
           unresolvedReferences.Count(),
           UnresolvedReferencesFileName);

        using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + UnresolvedReferencesFileName)) {
          foreach (var reference in unresolvedReferences) {
            outfile.WriteLine(reference);
          }
        }
      }
    }

    private const string ConstructedTypesReasonsFileName = "ConstructedTypesReasons.txt";

    private const string ReachedNonVirtualDispatchReasonsFileName = "ReachedNonvirtualDispatchesReasons.txt";

    private const string ReachedVirtualDispatchReasonsFileName = "ReachedVirtualDispatchesReasons.txt";

    private const string MethodReachedReasonsFileName = "MethodReachedReasons.txt";


    private static void OutputAnalysisReasons(RapidTypeAnalysis rta, string reportingDirectory) {
      // Clearly we could do better here with some factoring out, etc.

      AnalysisReasons reasons = rta.GetAnalysisReasons();


      //Console.WriteLine("Calculating Best Reasons");

      //reasons.CalculateBestReasons();


      // Type reached reasons

      using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + ConstructedTypesReasonsFileName)) {
        foreach (var constructedType in rta.ConstructedTypes()) {
          outfile.WriteLine(constructedType);

          HashSet<TypeConstructedReason> constructedReasons = reasons.GetReasonsTypeWasConstructed(constructedType);

          if (constructedReasons.Count() == 0) {
            outfile.WriteLine("\t<UNKNOWN>");
          }
          else {
            foreach (TypeConstructedReason reason in constructedReasons) {
              outfile.WriteLine("\t" + reason);
            }
          }
        }
      }

      // Dispatch Reached Reasons (t-devinc: CLEAN THIS UP)

      using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + ReachedNonVirtualDispatchReasonsFileName)) {
        foreach (var methodDispatchedAgainst in reasons.AllMethodsNonVirtuallyDispatchedAgainst()) {
          outfile.WriteLine(methodDispatchedAgainst);

          HashSet<DispatchReachedReason> dispatchReasons = reasons.GetReasonsNonVirtualDispatchWasReached(methodDispatchedAgainst);

          if (dispatchReasons.Count() == 0) {
            outfile.WriteLine("\t<UNKNOWN>");
          }
          else {
            foreach (DispatchReachedReason reason in dispatchReasons) {
              outfile.WriteLine("\t" + reason);
            }
          }
        }
      }


      using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + ReachedVirtualDispatchReasonsFileName)) {
        foreach (var methodDispatchedAgainst in reasons.AllMethodsVirtuallyDispatchedAgainst()) {
          outfile.WriteLine(methodDispatchedAgainst);

          HashSet<DispatchReachedReason> dispatchReasons = reasons.GetReasonsVirtualDispatchWasReached(methodDispatchedAgainst);

          if (dispatchReasons.Count() == 0) {
            outfile.WriteLine("\t<UNKNOWN>");
          }
          else {
            foreach (DispatchReachedReason reason in dispatchReasons) {
              outfile.WriteLine("\t" + reason);
            }
          }
        }
      }

      // Method reached reasons


      using (StreamWriter outfile = new StreamWriter(reportingDirectory + @"\" + MethodReachedReasonsFileName)) {
        foreach (var reachedMethod in rta.ReachableMethods()) {
          outfile.WriteLine(reachedMethod);

          HashSet<MethodReachedReason> methodReachedReason = reasons.GetReasonsMethodWasReached(reachedMethod);

          if (methodReachedReason.Count() == 0) {
            outfile.WriteLine("\t<UNKNOWN>");
          }
          else {
            foreach (MethodReachedReason reason in methodReachedReason) {
              outfile.WriteLine("\t" + reason);
            }
          }
        }
      }
    }


    private static StubMethodBodyEmitter GetStubMethodBodyEmitterForProfile(TargetProfile profile, WholeProgram wholeProgram) {
      StubMethodBodyEmitter emitter;

      switch (profile) {
        case TargetProfile.Desktop:
          emitter = new DotNetDesktopStubMethodBodyEmitter(wholeProgram.Host());
          break;
        case TargetProfile.Phone:
          emitter = new WindowsPhoneStubMethodBodyEmitter(wholeProgram.Host());
          break;
        default:
          emitter = new DotNetDesktopStubMethodBodyEmitter(wholeProgram.Host());
          break;
      }

      return emitter;
    }

    private static void TransformProgram(WholeProgram wholeProgram,
        string transformedBinariesPath,
        string reportsPath,
        MethodRemoval methodRemoval,
        TargetProfile profile) {

      System.IO.Directory.CreateDirectory(transformedBinariesPath);

      StubMethodBodyEmitter stubEmitter = GetStubMethodBodyEmitterForProfile(profile, wholeProgram);

      foreach (IAssembly assembly in wholeProgram.AllAssemblies()) {
        if (AssemblyShouldBeRewritten(assembly)) {

          if (assembly.PublicKeyToken.Count() > 0) {
            Console.WriteLine("Warning: rewriting assembly with a public key token. {0}", assembly);
          }

          string outputBinary = transformedBinariesPath + @"\" + Path.GetFileName(assembly.Location);

          var copy = new MetadataDeepCopier(wholeProgram.Host()).Copy(assembly);


          DocumentationCommentDefinitionIdStringMap idMap = new DocumentationCommentDefinitionIdStringMap(new IAssembly[] { copy });

          AssemblyReport assemblyReport = AssemblyReport.CreateAssemblyReportFromPath(copy, reportsPath, idMap);

          stopwatch.Start();
          RewriteBinary(copy, assemblyReport, wholeProgram.Host(), outputBinary, methodRemoval, stubEmitter);

          stopwatch.Start();
        }
        else {
          //Console.WriteLine("Skipping rewrite of of assembly {0}", assembly.Name.Value);
        }
      }
    }




    private static void RewriteBinary(
        Assembly copy,
        AssemblyReport assemblyReport,
        MetadataReaderHost host,
        string outputPath,
        MethodRemoval methodRemoval,
        StubMethodBodyEmitter stubEmitter) {


      /* This is an attempt to decouple the MethodRemoval commandline options
       * from the tree shaker, but it doesn't really seem to be working.
       * Might be better to just pass the method removal directly to
       * the rewriter.
       */

      bool removeMethods = (methodRemoval == MethodRemoval.Remove);
      bool fullDebugStubs = (methodRemoval == MethodRemoval.Debug);
      bool dryRun = (methodRemoval == MethodRemoval.None);

      PdbReader/*?*/ pdbReader = null;
      string pdbFile = Path.ChangeExtension(copy.Location, "pdb");
      if (File.Exists(pdbFile)) {
        using (var pdbStream = File.OpenRead(pdbFile)) {
          pdbReader = new PdbReader(pdbStream, host);
        }
      }
      else {
        Console.WriteLine("Could not load the PDB file for '" + copy.Name.Value + "' . Proceeding anyway.");
      }

      using (pdbReader) {
        var localScopeProvider = pdbReader == null ? null : new ILGenerator.LocalScopeProvider(pdbReader);
        var pdbPath = Path.ChangeExtension(outputPath, ".pdb");
        var outputFileName = Path.GetFileNameWithoutExtension(outputPath);
        using (var peStream = File.Create(outputPath)) {
          using (var pdbWriter = new PdbWriter(pdbPath, pdbReader)) {
            var rewriter = new TreeShakingRewriter(host, assemblyReport, dryRun, removeMethods, fullDebugStubs, stubEmitter);
            IAssembly rewrittenCopy = rewriter.Rewrite(copy);

            PeWriter.WritePeToStream(rewrittenCopy, host, peStream, pdbReader, localScopeProvider, pdbWriter);
          }
        }
      }
    }


  }

  class PhoneMetadataHost : PeReader.DefaultHost {
    private readonly ISet<string> pathDirectories;


    public PhoneMetadataHost(ISet<string> pathDirectories) {
      this.pathDirectories = pathDirectories;
    }

    public override IAssembly LoadAssembly(AssemblyIdentity assemblyIdentity) {
      // If we are trying to load a dll *first* check to see if it
      // is in a path directory. This mirrors how we (presume) the phone
      // looks up assemblies (where some apps include their own versions
      // of system dlls to [again, presumably] avoid dll hell).

      // This is a hack until we figure out how to do this the right way.
      // (Somehow hook into resolving assemblies?)

      string assumedAssemblyFileName = assemblyIdentity.Name.Value + ".dll";

      foreach (string pathDirectory in pathDirectories) {
        string pathAsDLL = Path.Combine(pathDirectory, assumedAssemblyFileName);
        if (File.Exists(pathAsDLL)) {
          IAssembly localVersion = base.LoadUnitFrom(pathAsDLL) as IAssembly;

          if (localVersion != null && !(localVersion is Dummy)) {

            //Console.WriteLine("!!!Loading local version of DLL {0}", localVersion); 
            return localVersion;
          }
        }
      }

      return base.LoadAssembly(assemblyIdentity);
    }
  }

}
