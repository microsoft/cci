//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SpecSharp {
  public class CompilerOptions : System.CodeDom.Compiler.CompilerParameters {
    public IDictionary<string,string>/*?*/ AliasesForReferencedAssemblies;
    //public ModuleKindFlags ModuleKind = ModuleKindFlags.ConsoleApplication;
    public bool EmitManifest = true;
    public List<string>/*?*/ DefinedPreProcessorSymbols;
    public string/*?*/ XMLDocFileName;
    public string/*?*/ RecursiveWildcard;
    public List<string>/*?*/ ReferencedModules;
    public string/*?*/ Win32Icon;
    public bool PDBOnly;
    public bool Optimize;
    public bool IncrementalCompile;
    public List<int>/*?*/ SuppressedWarnings;
    public bool CheckedArithmetic;
    public bool AllowUnsafeCode;
    public bool DisplayCommandLineHelp;
    public bool SuppressLogo;
    public long BaseAddress; //TODO: default value
    public string/*?*/ BugReportFileName;
    public object/*?*/ CodePage; //must be an int if not null
    public bool EncodeOutputInUTF8;
    public bool FullyQualifyPaths;
    public int FileAlignment;
    public bool NoStandardLibrary;
    public List<string>/*?*/ AdditionalSearchPaths;
    public bool HeuristicReferenceResolution;
    public string/*?*/ RootNamespace;
    public bool CompileAndExecute;
    public object/*?*/ UserLocaleId; //must be an int if not null
    public string/*?*/ StandardLibraryLocation;
    public PlatformType/*?*/ TargetPlatform; //TODO: rename this to TargetRuntime
    //public ProcessorType TargetProcessor;
    public string/*?*/ TargetPlatformLocation;
    public string/*?*/ AssemblyKeyFile;
    public string/*?*/ AssemblyKeyName;
    public bool DelaySign;
    //public TargetInformation TargetInformation;
    public List<int>/*?*/ SpecificWarningsToTreatAsErrors;
    public List<int>/*?*/ SpecificWarningsNotToTreatAsErrors;
    public string/*?*/ OutputPath;
    public string/*?*/ ExplicitOutputExtension;
    public AppDomain/*?*/ TargetAppDomain;
    public bool MayLockFiles;
    public string/*?*/ ShadowedAssembly;
    public bool UseStandardConfigFile;
    /// <summary>
    /// Do not emit run-time checks for requires clauses of non-externally-accessible methods, assert statements, loop invariants, and ensures clauses.
    /// </summary>
    public bool DisableInternalChecks;
    /// <summary>
    /// Do not emit run-time checks for assume statements.
    /// </summary>
    public bool DisableAssumeChecks;
    /// <summary>
    /// Do not emit run-time checks for requires clauses of externally accessible methods.
    /// </summary>
    public bool DisableDefensiveChecks;
    /// <summary>
    /// Disable the guarded classes feature, which integrates run-time enforcement of object invariants, ownership, and safe concurrency.
    /// </summary>
    public bool DisableGuardedClassesChecks;
    public bool DisableInternalContractsMetadata;
    public bool DisablePublicContractsMetadata;
    /// <summary>
    /// Disable the runtime test against null on non-null typed parameters on public methods
    /// </summary>
    public bool DisableNullParameterValidation;

    public CompilerOptions() {
    }

    //^ [NotDelayed]
    public CompilerOptions(CompilerOptions source) {
      if (source == null) { Debug.Assert(false); return; }
      this.AdditionalSearchPaths = source.AdditionalSearchPaths; //REVIEW: clone the list?
      this.AliasesForReferencedAssemblies = source.AliasesForReferencedAssemblies;
      this.AllowUnsafeCode = source.AllowUnsafeCode;
      this.AssemblyKeyFile = source.AssemblyKeyFile;
      this.AssemblyKeyName = source.AssemblyKeyName;
      this.BaseAddress = source.BaseAddress;
      this.BugReportFileName = source.BugReportFileName;
      this.CheckedArithmetic = source.CheckedArithmetic;
      this.CodePage = source.CodePage;
      this.CompileAndExecute = source.CompileAndExecute;
      this.CompilerOptions = source.CompilerOptions;
      this.DefinedPreProcessorSymbols = source.DefinedPreProcessorSymbols;
      this.DelaySign = source.DelaySign;
      this.DisableAssumeChecks = source.DisableAssumeChecks;
      this.DisableDefensiveChecks = source.DisableDefensiveChecks;
      this.DisableGuardedClassesChecks = source.DisableGuardedClassesChecks;
      this.DisableInternalChecks = source.DisableInternalChecks;
      this.DisableInternalContractsMetadata = source.DisableInternalContractsMetadata;
      this.DisablePublicContractsMetadata = source.DisablePublicContractsMetadata;
      this.DisplayCommandLineHelp = source.DisplayCommandLineHelp;
      StringCollection/*?*/ embeddedResources = source.EmbeddedResources;
      if (embeddedResources != null) {
        //^ assume false; //TODO: need to teach Boogie about StringCollections
        foreach (string/*?*/ s in embeddedResources) {
          if (s == null) continue;
          this.EmbeddedResources.Add(s);
        }
      }
      this.EmitManifest = source.EmitManifest;
      this.EncodeOutputInUTF8 = source.EncodeOutputInUTF8;
      this.Evidence = source.Evidence;
      this.ExplicitOutputExtension = source.ExplicitOutputExtension;
      this.FileAlignment = source.FileAlignment;
      this.FullyQualifyPaths = source.FullyQualifyPaths;
      this.GenerateExecutable = source.GenerateExecutable;
      this.GenerateInMemory = source.GenerateInMemory;
      this.HeuristicReferenceResolution = source.HeuristicReferenceResolution;
      this.IncludeDebugInformation = source.IncludeDebugInformation;
      this.IncrementalCompile = source.IncrementalCompile;
      if (source.LinkedResources != null)
        foreach (string s in source.LinkedResources) this.LinkedResources.Add(s);
      this.MainClass = source.MainClass;
      this.MayLockFiles = source.MayLockFiles;
      //this.ModuleKind = source.ModuleKind;
      this.NoStandardLibrary = source.NoStandardLibrary;
      this.Optimize = source.Optimize;
      this.OutputAssembly = source.OutputAssembly;
      this.OutputPath = source.OutputPath;
      this.PDBOnly = source.PDBOnly;
      this.RecursiveWildcard = source.RecursiveWildcard;
      if (source.ReferencedAssemblies != null)
        foreach (string s in source.ReferencedAssemblies) this.ReferencedAssemblies.Add(s);
      this.ReferencedModules = source.ReferencedModules;
      this.RootNamespace = source.RootNamespace;
      this.ShadowedAssembly = source.ShadowedAssembly;
      this.SpecificWarningsToTreatAsErrors = source.SpecificWarningsToTreatAsErrors;
      this.StandardLibraryLocation = source.StandardLibraryLocation;
      this.SuppressLogo = source.SuppressLogo;
      this.SuppressedWarnings = source.SuppressedWarnings;
      this.TargetAppDomain = source.TargetAppDomain;
      //this.TargetInformation = source.TargetInformation;
      this.TargetPlatform = source.TargetPlatform;
      this.TargetPlatformLocation = source.TargetPlatformLocation;
      this.TreatWarningsAsErrors = source.TreatWarningsAsErrors;
      this.UserLocaleId = source.UserLocaleId;
      this.UserToken = source.UserToken;
      this.WarningLevel = source.WarningLevel;
      this.Win32Icon = source.Win32Icon;
      this.Win32Resource = source.Win32Resource;
      this.XMLDocFileName = source.XMLDocFileName;
    }
    public virtual string/*?*/ GetOptionHelp() {
      return null;
    }
  }
  public enum LanguageVersionType {
    Default,
    ISO1,
    CSharpVersion2,
  }
  public class SpecSharpCompilerOptions : CompilerOptions {
    public bool Compatibility; //TODO: make this go away. Use LanguageVersion.
    public bool DummyCompilation;
    public LanguageVersionType LanguageVersion;
    public bool ReferenceTypesAreNonNullByDefault;
    public bool RunProgramVerifier;
    public bool RunProgramVerifierWhileEditing;
    public List<string>/*?*/ ProgramVerifierCommandLineOptions; // things to pass through to the static verifier

    public SpecSharpCompilerOptions() {
    }

    //^ [NotDelayed]
    public SpecSharpCompilerOptions(CompilerOptions options)
      : base(options) {
      SpecSharpCompilerOptions/*?*/ coptions = options as SpecSharpCompilerOptions;
      if (coptions == null) return;
      this.Compatibility = coptions.Compatibility;
      this.ReferenceTypesAreNonNullByDefault = coptions.ReferenceTypesAreNonNullByDefault;
      this.RunProgramVerifier = coptions.RunProgramVerifier;
      this.RunProgramVerifierWhileEditing = coptions.RunProgramVerifierWhileEditing;
      this.ProgramVerifierCommandLineOptions = coptions.ProgramVerifierCommandLineOptions;
    }
  }
}
