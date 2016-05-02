//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.IO;
using Microsoft.Cci;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization; // needed for defining exception .ctors
using System.Text;
using System.Diagnostics.Contracts;

namespace AsmMeta {
  internal class AsmMetaRewriter : MetadataRewriter {

    private PdbReader pdbReader;
    private readonly IName MoveNextName;

    /// <summary>
    /// Maps contract method name to three arg version
    /// Maps generic Requires method as RequiresE.
    /// </summary>
    private Dictionary<string, IMethodReference> threeArgumentVersionofContractMethod = new Dictionary<string, IMethodReference>();
    private NamespaceTypeDefinition/*?*/ classHoldingThreeArgVersions = null;

    /// <summary>
    /// Used to recognize calls to methods in the Contract class
    /// </summary>
    private readonly ITypeReference compilerGeneratedAttributeType = null;
    private readonly ITypeReference contractClassType = null;
    private readonly ITypeReference systemAttributeType = null;
    private readonly ITypeReference systemBooleanType = null;
    private readonly ITypeReference systemStringType = null;
    private readonly ITypeReference systemObjectType = null;
    private readonly ITypeReference systemVoidType = null;
    private readonly ITypeReference systemArgumentExceptionType = null;
    CustomAttribute ContractReferenceAssemblyAttributeInstance;
    /// <summary>
    /// The list of all types in the assembly being mutated. Any types introduced to the assembly must be added to this list
    /// as well as to the members list of their containing namespaces or containing types.
    /// </summary>
    List<INamedTypeDefinition> allTypes; // TODO: See if this can be deleted.
    private IMethodDefinition currentMethod;


    private AsmMetaRewriter(
      IMetadataHost host,
      PdbReader pdbReader,
      ITypeReference contractClassType,
      ITypeReference compilerGeneratedAttributeType,
      ITypeReference systemAttributeType,
      ITypeReference systemBooleanType,
      ITypeReference systemObjectType,
      ITypeReference systemStringType,
      ITypeReference systemVoidType
      )
      : base(host) {
      this.pdbReader = pdbReader;
      this.MoveNextName = host.NameTable.GetNameFor("MoveNext");
      this.contractClassType = contractClassType;
      this.compilerGeneratedAttributeType = compilerGeneratedAttributeType;
      this.systemAttributeType = systemAttributeType;
      this.systemBooleanType = systemBooleanType;
      this.systemObjectType = systemObjectType;
      this.systemStringType = systemStringType;
      this.systemVoidType = systemVoidType;
    }

    public static IAssembly RewriteModule(IMetadataHost host, PdbReader pdbReader, IAssembly assembly, ITypeReference contractClassType,
      ITypeReference compilerGeneratedAttributeType,
      ITypeReference systemAttributeType,
      ITypeReference systemBooleanType,
      ITypeReference systemObjectType,
      ITypeReference systemStringType,
      ITypeReference systemVoidType) {
      var me = new AsmMetaRewriter(host, pdbReader, contractClassType, compilerGeneratedAttributeType, systemAttributeType, systemBooleanType, systemObjectType, systemStringType, systemVoidType);
      return me.Rewrite(assembly);
    }

    public override void RewriteChildren(Assembly assembly) {
      this.allTypes = assembly.AllTypes;
      base.RewriteChildren(assembly);
      #region Add assembly-level attribute marking this assembly as a reference assembly
      if (assembly.AssemblyAttributes == null) assembly.AssemblyAttributes = new List<ICustomAttribute>(1);
      assembly.AssemblyAttributes.Add(this.ContractReferenceAssemblyAttributeInstance);
      #endregion Add assembly-level attribute marking this assembly as a reference assembly
      #region Remove assembly-level attribute marking this as a declarative assembly
      var contractDeclarativeAssemblyAttribute = UnitHelper.FindType(this.host.NameTable, assembly, "System.Diagnostics.Contracts.ContractDeclarativeAssemblyAttribute")
                          as INamespaceTypeDefinition;
      if (contractDeclarativeAssemblyAttribute != null) {
        assembly.AssemblyAttributes.RemoveAll(ca => TypeHelper.TypesAreEquivalent(ca.Type, contractDeclarativeAssemblyAttribute));
      }

      #endregion
      return;
    }

    public override void RewriteChildren(RootUnitNamespace rootUnitNamespace) {
      this.classHoldingThreeArgVersions = this.GetContractClass(rootUnitNamespace);

      base.RewriteChildren(rootUnitNamespace);

      #region Possibly add class for any contract methods that were defined.
      if (0 < this.threeArgumentVersionofContractMethod.Count) {
        // Only add class to assembly if any 3 arg versions were actually created
        rootUnitNamespace.Members.Add(classHoldingThreeArgVersions);
        this.allTypes.Add(classHoldingThreeArgVersions);
      }
      #endregion Possibly add class for any contract methods that were defined.

      #region Create a reference to [ContractReferenceAssembly] to mark the assembly with
      INamespaceTypeDefinition contractReferenceAssemblyAttribute = null;
      #region First see if we can find it in the same assembly as the one we are rewriting
      var unit = rootUnitNamespace.Unit;
      contractReferenceAssemblyAttribute = UnitHelper.FindType(this.host.NameTable, unit, "System.Diagnostics.Contracts.ContractReferenceAssemblyAttribute")
                          as INamespaceTypeDefinition;
      #endregion First see if we can find it in the same assembly as the one we are rewriting
      #region If it doesn't exist there, then define it in the same place that the three-argument versions are defined
      if (contractReferenceAssemblyAttribute is Dummy) {
        contractReferenceAssemblyAttribute = CreateContractReferenceAssemblyAttribute(rootUnitNamespace);
      }
      #endregion If it doesn't exist there, then define it in the same place that the three-argument versions are defined
      #region Create a reference to the ctor
      var ctorRef = new Microsoft.Cci.MutableCodeModel.MethodReference() {
        CallingConvention = CallingConvention.HasThis,
        ContainingType = contractReferenceAssemblyAttribute,
        InternFactory = this.host.InternFactory,
        Name = host.NameTable.Ctor,
        Type = systemVoidType,
      };
      var rm = ctorRef.ResolvedMethod;
      this.ContractReferenceAssemblyAttributeInstance = new CustomAttribute() {
        Constructor = ctorRef,
      };
      #endregion Create a reference to the ctor
      #endregion Create a reference to [ContractReferenceAssembly] to mark the assembly with

      return;
    }

    public override void RewriteChildren(MethodBody methodBody) {

      this.currentMethod = methodBody.MethodDefinition;

      var compilerGenerated = TypeHelper.IsCompilerGenerated(currentMethod.ContainingTypeDefinition);
      // Method contracts from iterators (and async methods) end up in the MoveNext method in the compiler generated
      // class that implements the state machine. So they need to be treated specially.
      var isMoveNextMethodInCompilerGeneratedMethod = compilerGenerated && currentMethod.Name == this.MoveNextName;

      if ((compilerGenerated && !isMoveNextMethodInCompilerGeneratedMethod) || this.IsIteratorOrAsyncMethod(currentMethod)) {
        return;
      }

      uint lastOffset;
      if (!TryGetOffsetOfLastContractCall(methodBody.Operations, out lastOffset))
        return;
      // And here is the special handling: currently the contract extractors expect to find the contracts
      // in the MoveNext method. So don't move them to the iterator/async method, just convert all of the
      // contracts in the MoveNext method into the three-arg version and keep the whole method body. No
      // point trying to truncate it since the contracts are not in a prefix of the instructions, but are
      // buried within the state machine.
      if (isMoveNextMethodInCompilerGeneratedMethod) {
        var lastIndex = methodBody.Operations.Count - 1;
        lastOffset = methodBody.Operations[lastIndex].Offset;
      }

      try {

        var ilRewriter = new MyILRewriter(this.host, this, lastOffset, isMoveNextMethodInCompilerGeneratedMethod);
        var rewrittenMethodBody = ilRewriter.Rewrite(methodBody);

        var locals = rewrittenMethodBody.LocalVariables;

        methodBody.LocalVariables = locals as List<ILocalDefinition> ?? new List<ILocalDefinition>(locals);

        var ops = rewrittenMethodBody.Operations;
        methodBody.Operations = ops as List<IOperation> ?? new List<IOperation>(ops);

        methodBody.OperationExceptionInformation = new List<IOperationExceptionInformation>();

        methodBody.MaxStack = rewrittenMethodBody.MaxStack;
        methodBody.MaxStack++;

        //// REVIEW: Is this okay to do here? Or does it need to be done in the IL Rewriter?
        //#region All done: add return statement
        //if (!isMoveNextMethodInCompilerGeneratedMethod) {
        //  if (currentMethod.Type.TypeCode != PrimitiveTypeCode.Void) {
        //    LocalDefinition ld = new LocalDefinition() {
        //      Type = currentMethod.Type,
        //    };
        //    if (methodBody.LocalVariables == null) methodBody.LocalVariables = new List<ILocalDefinition>();
        //    methodBody.LocalVariables.Add(ld);
        //    methodBody.Operations.Add(new Operation(){  OperationCode= OperationCode.Ldloc, Value = ld, });
        //    methodBody.MaxStack++;
        //  }
        //  methodBody.Operations.Add(new Operation() { OperationCode = OperationCode.Ret, });
        //}
        //#endregion All done: add return statement

        return;

      } catch (ExtractorException) {
        Console.WriteLine("Warning: Unable to extract contract from the method '{0}'.",
          MemberHelper.GetMemberSignature(currentMethod, NameFormattingOptions.SmartTypeName));
        methodBody.OperationExceptionInformation = new List<IOperationExceptionInformation>();
        methodBody.Operations = new List<IOperation>();
        methodBody.MaxStack = 0;
        return;
      }
    }

    /// <summary>
    /// Methods that are iterators or async methods must have their bodies preserved
    /// in the reference assembly because (for now) their contracts are left in the
    /// MoveNext method found in the nested type defined in the containing type of this
    /// method. The contract extraction that is done by downstream tools depends on
    /// *this* method containing the preamble that creates an instance of that nested
    /// type, otherwise they assume the method does *not* have any contracts (which
    /// is incorrect).
    /// </summary>
    private bool IsIteratorOrAsyncMethod(IMethodDefinition currentMethod) {
      // walk the Operations looking for the first newobj instruction
      IMethodReference ctor = null;
      foreach (var op in currentMethod.Body.Operations) {
        if (op.OperationCode == OperationCode.Newobj) {
          ctor = op.Value as IMethodReference;
          break;
        }
      }
      if (ctor == null) return false;
      var nestedType = ctor.ContainingType as INestedTypeReference;
      if (nestedType == null) return false;
      if (nestedType.ContainingType.InternedKey != currentMethod.ContainingType.InternedKey) return false;
      if (!TypeHelper.IsCompilerGenerated(ctor.ResolvedMethod.ContainingTypeDefinition)) return false;
      var m = TypeHelper.GetMethod(nestedType.ResolvedType, this.host.NameTable.GetNameFor("MoveNext"));
      return !(m is Dummy);
    }

    private bool TryGetOffsetOfLastContractCall(List<IOperation>/*?*/ instrs, out uint offset) {
      offset = uint.MaxValue;
      if (instrs == null) return false; // not found
      for (int i = instrs.Count - 1; 0 <= i; i--) {
        IOperation op = instrs[i];
        if (op.OperationCode != OperationCode.Call) continue;
        IMethodReference method = op.Value as IMethodReference;
        if (method == null) continue;
        if (Microsoft.Cci.MutableContracts.ContractHelper.IsValidatorOrAbbreviator(method)) {
          offset = op.Offset;
          return true;
        }
        var methodName = method.Name.Value;
        if (TypeHelper.TypesAreEquivalent(method.ContainingType, this.contractClassType)
          && IsNameOfPublicContractMethod(methodName)
          ) {
          offset = op.Offset;
          return true;
        }
      }
      return false; // not found
    }

    private bool IsNameOfPublicContractMethod(string methodName) {
      switch (methodName) {
        case "Requires":
        case "RequiresAlways":
        case "Ensures":
        case "EnsuresOnThrow":
        case "Invariant":
        case "EndContractBlock":
          return true;
        default:
          return false;
      }
    }

    private class MyILRewriter : ILRewriter {
      private AsmMetaRewriter parent;
      private uint lastOffset;
      private bool moveNextMethod;

      public MyILRewriter(IMetadataHost host, AsmMetaRewriter parent, uint lastOffset, bool moveNextMethod)
        : base(host, parent.pdbReader, parent.pdbReader) {
        this.parent = parent;
        this.lastOffset = lastOffset;
        this.moveNextMethod = moveNextMethod;
      }

      protected override void EmitOperation(IOperation operation) {
        if (operation.Offset <= this.lastOffset) {
          if (this.parent.IsCallToContractMethod(operation)) {
            EmitContractCall(operation);
          } else {
            base.EmitOperation(operation);
          }
          if (!this.moveNextMethod && operation.Offset == this.lastOffset) {
            #region All done: add return statement
            if (this.parent.currentMethod.Type.TypeCode != PrimitiveTypeCode.Void) {
              var ld = new LocalDefinition() {
                Type = this.parent.currentMethod.Type,
              };
              base.EmitOperation(new Operation() { OperationCode = OperationCode.Ldloc, Value = ld, });
              base.TrackLocal(ld);
            }
            base.EmitOperation(new Operation() { OperationCode = OperationCode.Ret, });
            #endregion All done: add return statement
          }
        }

      }

      private void EmitContractCall(IOperation op) {
        IMethodReference originalMethod = op.Value as IMethodReference;
        IMethodReference methodToUse = this.parent.GetCorrespondingThreeArgContractMethod(originalMethod);
        string sourceText = null;
        if (this.parent.pdbReader != null) {
          int startColumn;
          string sourceLanguage;
          this.parent.GetSourceTextFromOperation(op, out sourceText, out startColumn, out sourceLanguage);
          if (sourceText != null) {
            int firstSourceTextIndex = sourceText.IndexOf('(');
            int lastSourceTextIndex = this.parent.GetLastSourceTextIndex(sourceText, originalMethod, firstSourceTextIndex + 1);
            firstSourceTextIndex = firstSourceTextIndex == -1 ? 0 : firstSourceTextIndex + 1; // the +1 is to skip the opening paren
            if (lastSourceTextIndex <= firstSourceTextIndex) {
              //Console.WriteLine(sourceText);
              lastSourceTextIndex = sourceText.Length; // if something went wrong, at least get the whole source text.
            }
            sourceText = sourceText.Substring(firstSourceTextIndex, lastSourceTextIndex - firstSourceTextIndex);
            var indentSize = firstSourceTextIndex + (startColumn - 1); // -1 because columns start at one, not zero
            sourceText = AdjustIndentationOfMultilineSourceText(sourceText, indentSize);
          }
        }
        if (originalMethod.ParameterCount == 1) {
          // add null user message
          base.EmitOperation(new Operation() { OperationCode = OperationCode.Ldnull, });
        }
        if (sourceText == null) {
          base.EmitOperation(new Operation() { OperationCode = OperationCode.Ldnull });
        } else {
          base.EmitOperation(new Operation() { OperationCode = OperationCode.Ldstr, Value = sourceText });
        }
        base.EmitOperation(new Operation() { OperationCode = OperationCode.Call, Value = methodToUse });
        return;
      }

      private static string AdjustIndentationOfMultilineSourceText(string sourceText, int trimLength) {
        if (!sourceText.Contains("\n")) return sourceText;
        var lines = sourceText.Split('\n');
        if (lines.Length == 1) return sourceText;
        for (int i = 1; i < lines.Length; i++) {
          var currentLine = lines[i];
          if (trimLength < currentLine.Length) {
            var prefix = currentLine.Substring(0, trimLength);
            if (All(prefix, ' ')) {
              lines[i] = currentLine.Substring(trimLength);
            }
          }
        }
        var numberOfLinesToJoin = String.IsNullOrEmpty(lines[lines.Length - 1].TrimStart(whiteSpace)) ? lines.Length - 1 : lines.Length;
        return String.Join("\n", lines, 0, numberOfLinesToJoin);
      }
      static char[] whiteSpace = { ' ', '\t' };
      private static bool All(string s, char c) {
        foreach (var x in s)
          if (x != c) return false;
        return true;
      }




    }

    private NamespaceTypeDefinition GetContractClass(RootUnitNamespace unitNamespace) {
      var contractClass = UnitHelper.FindType(this.host.NameTable, (IModule)unitNamespace.Unit, "System.Diagnostics.Contracts.Contract")
                          as NamespaceTypeDefinition;

      if (contractClass != null) return contractClass;
      return CreateContractClass(unitNamespace);
    }
    private NamespaceTypeDefinition CreateContractClass(UnitNamespace unitNamespace) {

      var contractTypeName = this.host.NameTable.GetNameFor("Contract");
      var contractNamespaceName = this.host.NameTable.GetNameFor("System.Diagnostics.Contracts");

      Microsoft.Cci.MethodReference compilerGeneratedCtor =
        new Microsoft.Cci.MethodReference(
          this.host,
          this.compilerGeneratedAttributeType,
          CallingConvention.HasThis,
          this.systemVoidType,
          this.host.NameTable.Ctor,
          0);
      CustomAttribute compilerGeneratedAttribute = new CustomAttribute();
      compilerGeneratedAttribute.Constructor = compilerGeneratedCtor;

      var contractsNs = new NestedUnitNamespace() {
        ContainingUnitNamespace = unitNamespace,
        Name = contractNamespaceName,
      };
      NamespaceTypeDefinition result = new NamespaceTypeDefinition() {
        // NB: The string name must be kept in sync with the code that recognizes contract
        // methods!!
        Name = contractTypeName,
        Attributes = new List<ICustomAttribute>{ compilerGeneratedAttribute },
        BaseClasses = new List<ITypeReference>{ this.systemObjectType },
        ContainingUnitNamespace = contractsNs,
        InternFactory = this.host.InternFactory,
        IsBeforeFieldInit = true,
        IsClass = true,
        IsSealed = true,
        Layout = LayoutKind.Auto,
        StringFormat = StringFormatKind.Ansi,
      };
      return result;
    }
    private NamespaceTypeDefinition CreateContractReferenceAssemblyAttribute(IRootUnitNamespace rootNs) {

      var internFactory = this.host.InternFactory;
      var nameTable = this.host.NameTable;

      var contractReferenceAssemblyAttributeName = nameTable.GetNameFor("ContractReferenceAssemblyAttribute");
      var contractNamespaceName = nameTable.GetNameFor("System.Diagnostics.Contracts");

      #region Define type
      CustomAttribute compilerGeneratedAttribute = new CustomAttribute() {
        Constructor = new Microsoft.Cci.MethodReference(
          this.host,
          this.compilerGeneratedAttributeType,
          CallingConvention.HasThis,
          this.systemVoidType,
          this.host.NameTable.Ctor,
          0)
      };

      var contractsNs = new NestedUnitNamespace() {
        ContainingUnitNamespace = rootNs,
        Name = contractNamespaceName,
      };
      NamespaceTypeDefinition result = new NamespaceTypeDefinition() {
        Name = contractReferenceAssemblyAttributeName,
        Attributes = new List<ICustomAttribute>{ compilerGeneratedAttribute },
        BaseClasses = new List<ITypeReference>{ this.systemAttributeType },
        ContainingUnitNamespace = contractsNs, //unitNamespace,
        InternFactory = internFactory,
        IsBeforeFieldInit = true,
        IsClass = true,
        IsSealed = true,
        Methods = new List<IMethodDefinition>(),
        Layout = LayoutKind.Auto,
        StringFormat = StringFormatKind.Ansi,
      };
      contractsNs.Members.Add(result);
      this.allTypes.Add(result);
      #endregion Define type
      #region Define the ctor
      List<IStatement> statements = new List<IStatement>();
      SourceMethodBody body = new SourceMethodBody(this.host) {
        LocalsAreZeroed = true,
        Block = new BlockStatement() { Statements = statements },
      };
      MethodDefinition ctor = new MethodDefinition() {
        Body = body,
        CallingConvention = CallingConvention.HasThis,
        ContainingTypeDefinition = result,
        InternFactory = internFactory,
        IsRuntimeSpecial = true,
        IsStatic = false,
        IsSpecialName = true,
        Name = nameTable.Ctor,
        Type = this.systemVoidType,
        Visibility = TypeMemberVisibility.Public,
      };
      body.MethodDefinition = ctor;
      var thisRef = new ThisReference() { Type = result, };
      // base();
      foreach (var baseClass in result.BaseClasses) {
        var baseCtor = new Microsoft.Cci.MutableCodeModel.MethodReference() {
          CallingConvention = CallingConvention.HasThis,
          ContainingType = baseClass,
          GenericParameterCount = 0,
          InternFactory = this.host.InternFactory,
          Name = nameTable.Ctor,
          Type = this.systemVoidType,
        };
        statements.Add(
          new ExpressionStatement() {
            Expression = new MethodCall() {
              MethodToCall = baseCtor,
              IsStaticCall = false, // REVIEW: Is this needed in addition to setting the ThisArgument?
              ThisArgument = new ThisReference() { Type = result, },
              Type = this.systemVoidType, // REVIEW: Is this the right way to do this?
              Arguments = new List<IExpression>(),
            }
          }
          );
        break;
      }

      // return;
      statements.Add(new ReturnStatement());
      result.Methods.Add(ctor);
      #endregion Define the ctor
      return result;
    }

    private bool IsCallToContractMethod(IOperation op) {
      if (op.OperationCode != OperationCode.Call) return false;
      IMethodReference method = op.Value as IMethodReference;
      if (method == null) return false;
      ITypeReference contractClass = method.ContainingType;
      if (!TypeHelper.TypesAreEquivalent(contractClass, this.contractClassType)) return false;
      switch (method.Name.Value) {
        case "Requires":
        case "RequiresAlways": // TODO: Remove once RequiresAlways is gone from the library
        case "Ensures":
        case "EnsuresOnThrow":
        case "Invariant":
        case "Assert":
        case "Assume":
          return true;
        default:
          return false;
      }
    }

    private IMethodReference GetCorrespondingThreeArgContractMethod(IMethodReference originalMethod) {
      ushort genericParameters = 0;
      string contractMethodName = originalMethod.Name.Value;
      string keyName = contractMethodName;
      IGenericMethodInstanceReference methodInstance = originalMethod as IGenericMethodInstanceReference;
      if (methodInstance != null) {
        originalMethod = methodInstance.GenericMethod;
        genericParameters = originalMethod.GenericParameterCount;
        keyName = originalMethod.Name.Value + genericParameters;
      }

      #region Backward compatibility with v4 Beta 1 which went out with RequiresAlways in it (REMOVE WHEN THAT IS DELETED)
      bool backwardCompat = false;
      if (contractMethodName.Equals("RequiresAlways")) {
        contractMethodName = "Requires1"; // The one is for the generic parameter
        genericParameters = 1;
        backwardCompat = true;
      }
      #endregion Backward compatibility with v4 Beta 1 which went out with RequiresAlways in it (REMOVE WHEN THAT IS DELETED)

      IMethodReference methodToUse;
      this.threeArgumentVersionofContractMethod.TryGetValue(keyName, out methodToUse);
      if (methodToUse == null) {
        #region Create a method
        methodToUse = CreateThreeArgVersionOfMethod(contractMethodName, keyName, genericParameters, backwardCompat);
        #endregion Create a method
      }
      if (genericParameters != 0) {
        // instantiate method to use
        methodToUse = new Microsoft.Cci.Immutable.GenericMethodInstanceReference(methodToUse,
          backwardCompat ? IteratorHelper.GetSingletonEnumerable<ITypeReference>(this.systemArgumentExceptionType) : methodInstance.GenericArguments,
          this.host.InternFactory);
        var key = methodToUse.InternedKey;
      }
      return methodToUse;
    }

    private IMethodReference CreateThreeArgVersionOfMethod(string originalMethodName, string keyName, ushort genericParameters, bool backwardCompat) {
      MethodBody body = new MethodBody() {
        Operations = new List<IOperation>{ new Operation() { OperationCode = OperationCode.Ret, } },
      };

      MethodDefinition threeArgVersion = new MethodDefinition() {
        Body = body,
        CallingConvention = CallingConvention.Default, // Isn't it the default for the calling convention to be the default?
        ContainingTypeDefinition = this.classHoldingThreeArgVersions,
        GenericParameters = new List<IGenericMethodParameter>(genericParameters),
        Name = backwardCompat ? this.host.NameTable.GetNameFor("Requires") : this.host.NameTable.GetNameFor(originalMethodName),
        Type = this.systemVoidType,
        Visibility = TypeMemberVisibility.Public,
        IsStatic = true,
        InternFactory = this.host.InternFactory, // NB: without this, the method has an interned key of zero, which gets confused with other dummy interned keys!!
      };
      if (genericParameters != 0) {
        threeArgVersion.CallingConvention = CallingConvention.Generic;
        var typeArg = new GenericMethodParameter() {
          Name = this.host.NameTable.GetNameFor("TException"),
          DefiningMethod = threeArgVersion,
          InternFactory = this.host.InternFactory,
        };
        // TODO: add SystemException base type?
        threeArgVersion.GenericParameters.Add(typeArg);
      }
      List<IParameterDefinition> paramList = new List<IParameterDefinition>();
      paramList.Add(
        new ParameterDefinition() {
          ContainingSignature = threeArgVersion,
          Name = this.host.NameTable.GetNameFor("condition"),
          Type = this.systemBooleanType,
          Index = 0,
        });
      paramList.Add(
        new ParameterDefinition() {
          ContainingSignature = threeArgVersion,
          Name = this.host.NameTable.GetNameFor("userSuppliedString"),
          Type = this.systemStringType,
          Index = 1,
        });
      paramList.Add(
        new ParameterDefinition() {
          ContainingSignature = threeArgVersion,
          Name = this.host.NameTable.GetNameFor("sourceText"),
          Type = this.systemStringType,
          Index = 2,
        });
      threeArgVersion.Parameters = paramList;
      body.MethodDefinition = threeArgVersion;
      this.threeArgumentVersionofContractMethod.Add(keyName, threeArgVersion);
      if (this.classHoldingThreeArgVersions.Methods == null) this.classHoldingThreeArgVersions.Methods = new List<IMethodDefinition>(1);
      this.classHoldingThreeArgVersions.Methods.Add(threeArgVersion);

      return threeArgVersion;
    }

    private int GetLastSourceTextIndex(string sourceText, IMethodReference originalMethod, int startSourceTextIndex) {
      if (originalMethod.ParameterCount == 1) {
        return sourceText.LastIndexOf(')'); // supposedly the character after the first (and only) argument
      } else {
        return IndexOfWhileSkippingBalancedThings(sourceText, startSourceTextIndex, ','); // supposedly the character after the first argument
      }
    }

    private int IndexOfWhileSkippingBalancedThings(string source, int startIndex, char targetChar) {
      int i = startIndex;
      while (i < source.Length) {
        if (source[i] == targetChar) break;
        else if (source[i] == '(') i = IndexOfWhileSkippingBalancedThings(source, i + 1, ')') + 1;
        else if (source[i] == '"') i = IndexOfWhileSkippingBalancedThings(source, i + 1, '"') + 1;
        else i++;
      }
      return i;
    }

    private void GetSourceTextFromOperation(IOperation op, out string sourceText, out int startColumn, out string sourceLanguage) {
      sourceText = null;
      startColumn = 0;
      sourceLanguage = "unknown";
      foreach (IPrimarySourceLocation psloc in this.pdbReader.GetClosestPrimarySourceLocationsFor(op.Location)) {
        if (!String.IsNullOrEmpty(psloc.Source)) {
          sourceText = psloc.Source;
          startColumn = psloc.StartColumn;
          if (psloc.SourceDocument != null) {
            sourceLanguage = psloc.SourceDocument.SourceLanguage;
          }
          //Console.WriteLine("[{0}]: {1}", i, psloc.Source);
          break;
        }
      }
    }



    /// <summary>
    /// Exceptions thrown during extraction. Should not escape this class.
    /// </summary>
    private class ExtractorException : Exception {
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ExtractorException() { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ExtractorException(string s) : base(s) { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ExtractorException(string s, Exception inner) : base(s, inner) { }
      /// <summary>
      /// Exception specific to an error occurring in the contract extractor
      /// </summary>
      public ExtractorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

  }
}