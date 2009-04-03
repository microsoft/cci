//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using Microsoft.Cci;
using Microsoft.Cci.Ast;
using System.Collections.Generic;
using System.IO;
using System.Text;

public sealed class SpecSharpOptions : FrameworkOptions {
  public List<string> BoogieOptions = new List<string>();
  public List<string> DefinedSymbols = new List<string>();
  public bool RunTestSuite;
  public bool TranslateToBPL;
  public bool Verify;
  public List<string> Z3Options = new List<string>();
}

public class OptionParser : OptionParser<SpecSharpOptions> {

  private OptionParser(MetadataHostEnvironment hostEnvironment) 
    : base(hostEnvironment)
  {
  }

  public static SpecSharpOptions ParseCommandLineArguments(MetadataHostEnvironment hostEnvironment, IEnumerable<string> arguments) {
    OptionParser parser = new OptionParser(hostEnvironment);
    parser.ParseCommandLineArguments(arguments, true);
    return parser.options;
  }

  protected override bool ParseCompilerOption(string arg) {
    int n = arg.Length;
    if (n <= 1) return false;
    char ch = arg[0];
    if (ch != '/' && ch != '-') return false;
    ch = arg[1];
    switch (Char.ToLower(ch)) {
      case 'b':
        List<string>/*?*/ boogieOptions = this.ParseNamedArgumentList(arg, "boogie", "b");
        if (boogieOptions == null || boogieOptions.Count == 0) return false;
        this.options.BoogieOptions.AddRange(boogieOptions);
        return true;
      case 'h':
        if (this.ParseName(arg, "help", "help")) {
          this.options.DisplayCommandLineHelp = true;
          return true;
        }
        return false;
      case '?':
        this.options.DisplayCommandLineHelp = true;
        return true;
      case 'c':
        bool? checkedArithmetic = this.ParseNamedBoolean(arg, "checked", "c");
        if (checkedArithmetic != null) {
          this.options.CheckedArithmetic = (bool)checkedArithmetic;
          return true;
        }
        return false;
      case 'd':
        List<string>/*?*/ definedSymbols = this.ParseNamedArgumentList(arg, "define", "d");
        if (definedSymbols == null || definedSymbols.Count == 0) return false;
        this.options.DefinedSymbols = definedSymbols;
        return true;
      case 'r':
        List<string>/*?*/ referencedAssemblies = this.ParseNamedArgumentList(arg, "reference", "r");
        if (referencedAssemblies == null || referencedAssemblies.Count == 0) return false;
        this.options.ReferencedAssemblies = referencedAssemblies;
        return true;
      case 's':
        if (this.ParseName(arg, "suite", "s")) {
          this.options.RunTestSuite = true;
          return true;
        }
        return false;
      case 't':
        this.options.TranslateToBPL = this.ParseName(arg, "translate", "t");
        return true;
      case 'v':
        if (this.ParseName(arg, "verify", "v")) {
          this.options.Verify = true;
          return true;
        }
        return false;
      case 'z':
        List<string>/*?*/ z3Options = this.ParseNamedArgumentList(arg, "z3", "z");
        if (z3Options == null || z3Options.Count == 0) return false;
        this.options.Z3Options.AddRange(z3Options);
        return true;
      default: 
        break;
    }
    return false;
  }

}