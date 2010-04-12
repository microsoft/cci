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
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.Contracts;
using System.IO;
using Microsoft.Cci.ILToCodeModel;
using System.Diagnostics;
using CSharpSourceEmitter;
using ContractHelper=Microsoft.Cci.ILToCodeModel.ContractHelper;

namespace HelloContracts {

  class Options : OptionParsing {

    [OptionDescription("The name of the reference assembly that will be used to pull contract information from. Ex. 'foo.contracts.dll'", ShortForm = "a")]
    public string assembly = null;

    [OptionDescription("Show inherited contracts", ShortForm = "i")]
    public bool inherited = false;

    [OptionDescription("Search paths for assembly dependencies.", ShortForm = "lp")]
    public List<string> libpaths = new List<string>();

  }

  class Program {
    static int Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: HelloContracts [path]fileName.Contracts.dll [-libpaths ...]*");
        return 1;
      }
      #region Parse options
      var options = new Options();
      options.Parse(args);
      if (options.HasErrors) {
        if (options.HelpRequested)
          options.PrintOptions("");
        return 1;
      }
      #endregion
      #region Collect and write contracts
      var host = new CodeContractAwareHostEnvironment(options.libpaths);
      var fileName = String.IsNullOrEmpty(options.assembly) ? options.GeneralArguments[0] : options.assembly;
      IModule module = host.LoadUnitFrom(fileName) as IModule;
      if (module == null || module == Dummy.Module || module == Dummy.Assembly) {
        Console.WriteLine("'{0}' is not a PE file containing a CLR module or assembly.", fileName);
        Environment.Exit(1);
      }
      var t = new Traverser(host, options.inherited);
      t.Visit(module);
      #endregion
      return 0;
    }
  }

  sealed class Traverser : BaseMetadataTraverser {

    private IContractAwareHost host;
    private bool showInherited;
    private int indentLevel = 0;

    public Traverser(IContractAwareHost host, bool showInherited) {
      this.host = host;
      this.showInherited = showInherited;
    }

    #region Print Helpers

    private void Indent() {
      for (int i = 0; i < this.indentLevel; i++) Console.Write("\t");
    }
    private string PrintExpression(IExpression expression) {
      SourceEmitterOutputString sourceEmitterOutput = new SourceEmitterOutputString();
      SourceEmitter CSSourceEmitter = new SourceEmitter(sourceEmitterOutput);
      CSSourceEmitter.Visit(expression);
      return sourceEmitterOutput.Data;
    }
    private void PrintMethodContract(IMethodContract/*?*/ methodContract) {
      if (methodContract == null) {
        this.Indent();
        Console.WriteLine("no contract");
        return;
      }
      this.Indent();
      Console.WriteLine(methodContract.IsPure ? "pure" : "not pure");
      if (IteratorHelper.EnumerableIsEmpty(methodContract.Preconditions) && IteratorHelper.EnumerableIsEmpty(methodContract.Postconditions))
        return;
      foreach (var p in methodContract.Preconditions) {
        this.Indent();
        Console.Write("requires ");
        if (!String.IsNullOrEmpty(p.OriginalSource)) {
          Console.Write(p.OriginalSource);
        } else {
          Console.Write(PrintExpression(p.Condition));
        }
        Console.WriteLine();
      }
      foreach (var p in methodContract.Postconditions) {
        Indent();
        Console.Write("ensures ");
        if (!String.IsNullOrEmpty(p.OriginalSource)) {
          Console.Write(p.OriginalSource);
        } else {
          Console.Write(PrintExpression(p.Condition));
        }
        Console.WriteLine();
      }
    }
    private void PrintTypeContract(ITypeContract/*?*/ typeContract) {
      if (typeContract == null || IteratorHelper.EnumerableIsEmpty(typeContract.Invariants)) {
        this.Indent();
        Console.WriteLine("no invariant");
        return;
      }
      foreach (var i in typeContract.Invariants) {
        Indent();
        Console.Write("invariant ");
        if (!String.IsNullOrEmpty(i.OriginalSource)) {
          Console.Write(i.OriginalSource);
        } else {
          Console.Write(PrintExpression(i.Condition));
        }
        Console.WriteLine();
      }
    }

    #endregion Print Helpers

    #region Visitors

    public override void Visit(ITypeDefinition typeDefinition) {
      if (AttributeHelper.Contains(typeDefinition.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) return;
      if (ContractHelper.IsContractClass(this.host, typeDefinition)) return;
      if (typeDefinition.IsEnum) return;
      Console.WriteLine(TypeHelper.GetTypeName(typeDefinition, NameFormattingOptions.TypeParameters));
      this.indentLevel++;
      var unit = TypeHelper.GetDefiningUnit(typeDefinition);
      if (unit != null) {
        var ce = this.host.GetContractExtractor(unit.UnitIdentity);
        if (ce != null)
          PrintTypeContract(ce.GetTypeContractFor(typeDefinition));
      }
      base.Visit(typeDefinition);
      this.indentLevel--;
    }

    public override void Visit(IPropertyDefinition propertyDefinition) {
      string propertyId = MemberHelper.GetMemberSignature(propertyDefinition, NameFormattingOptions.SmartTypeName);
      Indent();
      Console.WriteLine(propertyId);
      this.indentLevel++;
      if (propertyDefinition.Getter != null) {
        var getterMethod = propertyDefinition.Getter.ResolvedMethod;
        IMethodContract getterContract;
        if (this.showInherited)
          getterContract = ContractHelper.GetMethodContractForIncludingInheritedContracts(this.host, getterMethod);
        else
          getterContract = ContractHelper.GetMethodContractFor(this.host, getterMethod);
        Indent();
        Console.WriteLine("get");
        this.indentLevel++;
        PrintMethodContract(getterContract);
        this.indentLevel--;
      }
      if (propertyDefinition.Setter != null) {
        var setterMethod = propertyDefinition.Setter.ResolvedMethod;
        IMethodContract setterContract;
        if (this.showInherited)
          setterContract = ContractHelper.GetMethodContractForIncludingInheritedContracts(this.host, setterMethod);
        else
          setterContract = ContractHelper.GetMethodContractFor(this.host, setterMethod);
        Indent();
        Console.WriteLine("set");
        this.indentLevel++;
        PrintMethodContract(setterContract);
        this.indentLevel--;
      }
      this.indentLevel--;
    }

    public override void Visit(IMethodDefinition methodDefinition) {
      if (AttributeHelper.Contains(methodDefinition.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) return;
      if (Microsoft.Cci.ILToCodeModel.ContractHelper.IsInvariantMethod(this.host, methodDefinition)) return;
      if (IsGetter(methodDefinition) || IsSetter(methodDefinition)) return;
      IMethodContract methodContract;
      if (this.showInherited)
        methodContract = ContractHelper.GetMethodContractForIncludingInheritedContracts(this.host, methodDefinition);
      else
        methodContract = ContractHelper.GetMethodContractFor(this.host, methodDefinition);
      Indent();
      var methodSig = MemberHelper.GetMethodSignature(methodDefinition, NameFormattingOptions.Signature | NameFormattingOptions.ParameterName | NameFormattingOptions.ParameterModifiers);
      Console.WriteLine(methodSig);
      this.indentLevel++;
      PrintMethodContract(methodContract);
      this.indentLevel--;
    }

    #endregion Visitors

    #region Helper methods

    private static bool IsGetter(IMethodDefinition methodDefinition) {
      return methodDefinition.IsSpecialName && methodDefinition.Name.Value.StartsWith("get_");
    }

    private static bool IsSetter(IMethodDefinition methodDefinition) {
      return methodDefinition.IsSpecialName && methodDefinition.Name.Value.StartsWith("set_");
    }
    #endregion
  }

}
