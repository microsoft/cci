//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.ILToCodeModel;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;
using CSharpSourceEmitter;

namespace HelloContracts {

  class Options : OptionParsing {

    [OptionDescription("The name of the assembly to use as input", ShortForm = "a")]
    public string assembly = null;

    [OptionDescription("Print any contracts found in the input assembly", ShortForm= "p")]
    public bool printContracts = false;

    [OptionDescription("Show inherited contracts (used with -p)", ShortForm = "i")]
    public bool inherited = false;

    [OptionDescription("Inject non-null postconditions (used when -p is *not* used)")]
    public bool inject = true;

    [OptionDescription("Search paths for assembly dependencies.", ShortForm = "lp")]
    public List<string> libpaths = new List<string>();

  }

  class Program {
    static int Main(string[] args) {
      if (args == null || args.Length == 0) {
        Console.WriteLine("usage: HelloContracts [path]fileName.Contracts.dll [-libpaths ...]* [-p [-i] | -inject]");
        return 1;
      }
      #region Parse options
      var options = new Options();
      options.Parse(args);
      if (options.HelpRequested) {
        options.PrintOptions("");
        return 1;
      }
      if (options.HasErrors) {
        options.PrintErrorsAndExit(Console.Out);
      }
      #endregion

      var fileName = String.IsNullOrEmpty(options.assembly) ? options.GeneralArguments[0] : options.assembly;

      if (options.printContracts) {
        #region Collect and write contracts
        using (var host = new CodeContractAwareHostEnvironment(options.libpaths)) {
          IModule module = host.LoadUnitFrom(fileName) as IModule;
          if (module == null || module is Dummy) {
            Console.WriteLine("'{0}' is not a PE file containing a CLR module or assembly.", fileName);
            Environment.Exit(1);
          }
          var t = new Traverser(host, options.inherited);
          t.Traverse(module);
        }
        #endregion
        return 0;
      } else {
        using (var host = new CodeContractAwareHostEnvironment(options.libpaths, true, true)) {
          // Read the Metadata Model from the PE file
          var module = host.LoadUnitFrom(fileName) as IModule;
          if (module == null || module is Dummy) {
            Console.WriteLine(fileName + " is not a PE file containing a CLR module or assembly.");
            return 1;
          }

          // Get a PDB reader if there is a PDB file.
          PdbReader/*?*/ pdbReader = null;
          string pdbFile = Path.ChangeExtension(module.Location, "pdb");
          if (File.Exists(pdbFile)) {
            using (var pdbStream = File.OpenRead(pdbFile)) {
              pdbReader = new PdbReader(pdbStream, host);
            }
          }

          using (pdbReader) {

            ISourceLocationProvider sourceLocationProvider = pdbReader;

            // Construct a Code Model from the Metadata model via decompilation
            var mutableModule = Decompiler.GetCodeModelFromMetadataModel(host, module, pdbReader, DecompilerOptions.AnonymousDelegates | DecompilerOptions.Loops);
            ILocalScopeProvider localScopeProvider = new Decompiler.LocalScopeProvider(pdbReader);

            // Extract contracts (side effect: removes them from the method bodies)
            var contractProvider = Microsoft.Cci.MutableContracts.ContractHelper.ExtractContracts(host, mutableModule, pdbReader, localScopeProvider);

            // Inject non-null postconditions
            if (options.inject) {
              new NonNullInjector(host, contractProvider).Traverse(mutableModule);
            }

            // Put the contracts back in as method calls at the beginning of each method
            Microsoft.Cci.MutableContracts.ContractHelper.InjectContractCalls(host, mutableModule, contractProvider, sourceLocationProvider);

            // Write out the resulting module. Each method's corresponding IL is produced
            // lazily using CodeModelToILConverter via the delegate that the mutator stored in the method bodies.
            using (var peStream = File.Create(mutableModule.Location + ".pe")) {
              if (pdbReader == null) {
                PeWriter.WritePeToStream(mutableModule, host, peStream);
              } else {
                using (var pdbWriter = new PdbWriter(mutableModule.Location + ".pdb", sourceLocationProvider)) {
                  PeWriter.WritePeToStream(mutableModule, host, peStream, sourceLocationProvider, localScopeProvider, pdbWriter);
                }
              }
            }
          }
        }
        return 0;
      }
    }
  }

  sealed class Traverser : MetadataTraverser {

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
      CSSourceEmitter.Traverse(expression);
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
      if (IteratorHelper.EnumerableIsEmpty(methodContract.Preconditions) && IteratorHelper.EnumerableIsEmpty(methodContract.Postconditions)
          && IteratorHelper.EnumerableIsEmpty(methodContract.ThrownExceptions))
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
      foreach (var p in methodContract.ThrownExceptions) {
        Indent();
        Console.Write("throws ");
        Console.Write(TypeHelper.GetTypeName(p.ExceptionType, NameFormattingOptions.OmitContainingNamespace));
        Console.Write(" when ");
        if (!String.IsNullOrEmpty(p.Postcondition.OriginalSource)) {
          Console.Write(p.Postcondition.OriginalSource);
        } else {
          Console.Write(PrintExpression(p.Postcondition.Condition));
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

    public override void TraverseChildren(ITypeDefinition typeDefinition) {
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
      base.TraverseChildren(typeDefinition);
      this.indentLevel--;
    }

    public override void TraverseChildren(IPropertyDefinition propertyDefinition) {
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

    public override void TraverseChildren(IMethodDefinition methodDefinition) {
      if (AttributeHelper.Contains(methodDefinition.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute)) return;
      if (ContractHelper.IsInvariantMethod(this.host, methodDefinition)) return;
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

  class NonNullInjector : CodeTraverser {

    IMetadataHost host;
    Microsoft.Cci.MutableContracts.ContractProvider contractProvider;

    public NonNullInjector(
      IMetadataHost host,
      Microsoft.Cci.MutableContracts.ContractProvider contractProvider) {
      this.host = host;
      this.contractProvider = contractProvider;
    }

    public override void TraverseChildren(IMethodDefinition method) {
      if (!MemberHelper.IsVisibleOutsideAssembly(method)) return;
      var returnType = method.Type;
      if (returnType == this.host.PlatformType.SystemVoid
        || returnType.IsEnum
        || returnType.IsValueType
        ) return;

      var newContract = new Microsoft.Cci.MutableContracts.MethodContract();
      var post = new List<IPostcondition>();
      var p = new Microsoft.Cci.MutableContracts.Postcondition() {
        Condition = new NotEquality() {
          LeftOperand = new ReturnValue() { Type = returnType, },
          RightOperand = new CompileTimeConstant() {
            Type = returnType,
            Value = null,
          },
          Type = this.host.PlatformType.SystemBoolean,
        },
        OriginalSource = "result != null",
      };
      post.Add(p);
      newContract.Postconditions = post;

      var contract = this.contractProvider.GetMethodContractFor(method);
      if (contract != null) {
        Microsoft.Cci.MutableContracts.ContractHelper.AddMethodContract(newContract, contract);
      }
      this.contractProvider.AssociateMethodWithContract(method, newContract);

      base.TraverseChildren(method);
    }
  }
}
