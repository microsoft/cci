//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci.ILToCodeModel {

  internal class ContractExtractor : MethodBodyCodeMutator {

    private MethodContract/*?*/ currentMethodContract;
    private MethodContract/*!*/ CurrentMethodContract {
      get {
        if (this.currentMethodContract == null) {
          this.currentMethodContract = new MethodContract();
          this.contractProvider.AssociateMethodWithContract(this.sourceMethodBody.MethodDefinition, this.currentMethodContract);
        }
        return this.currentMethodContract;
      }
    }
    private TypeContract/*?*/ currentTypeContract;
    private TypeContract/*!*/ CurrentTypeContract {
      get {
        if (this.currentTypeContract == null) {
          this.currentTypeContract = new TypeContract();
          this.contractProvider.AssociateTypeWithContract(this.sourceMethodBody.MethodDefinition.ContainingTypeDefinition, this.currentTypeContract);
        }
        return this.currentTypeContract;
      }
    }

    ContractProvider contractProvider;
    SourceMethodBody sourceMethodBody;
    private OldAndResultExtractor oldAndResultExtractor;
    private bool extractingFromACtorInAClass;
    private bool extractingTypeContract;
    private ITypeReference contractClass;
    private ITypeReference contractClassDefinedInReferenceAssembly;


    private static bool TemporaryKludge(IEnumerable<ICustomAttribute> attributes, string attributeTypeName) {
      foreach (ICustomAttribute attribute in attributes) {
        if (TypeHelper.GetTypeName(attribute.Type) == attributeTypeName) return true;
      }
      return false;
    }

    public bool IsContractMethod(IMethodReference/*?*/ method) {
      if (method == null) return false;
      if (method.ContainingType.InternedKey == this.contractClass.InternedKey) return true;
      // Reference assemblies define their own internal versions of the contract methods
      return this.contractClassDefinedInReferenceAssembly != null &&
        (method.ContainingType.InternedKey == this.contractClassDefinedInReferenceAssembly.InternedKey);
    }

    internal ContractExtractor(SourceMethodBody sourceMethodBody, ContractProvider contractProvider)
      : base(sourceMethodBody.host, true) {
      this.sourceMethodBody = sourceMethodBody;
      this.contractProvider = contractProvider;
      // TODO: these fields make sense only if extracting a method contract and not a type contract.

      NamespaceTypeReference contractTypeAsSeenByThisUnit;
      AssemblyReference contractAssemblyReference = new AssemblyReference(host, contractProvider.Unit.ContractAssemblySymbolicIdentity);
      contractTypeAsSeenByThisUnit = ContractHelper.CreateTypeReference(this.host, contractAssemblyReference, "System.Diagnostics.Contracts.Contract");
      this.contractClass = contractTypeAsSeenByThisUnit;

      IUnitReference/*?*/ ur = TypeHelper.GetDefiningUnitReference(sourceMethodBody.MethodDefinition.ContainingType);
      IAssemblyReference ar = ur as IAssemblyReference;
      if (ar != null) {
        // Check for the attribute which is defined in the assembly that defines the contract class.
        var refAssemblyAttribute = ContractHelper.CreateTypeReference(this.host, contractAssemblyReference, "System.Diagnostics.Contracts.ContractReferenceAssemblyAttribute");
        if (AttributeHelper.Contains(ar.Attributes, refAssemblyAttribute)) {
          // then we're extracting contracts from a reference assembly
          var contractTypeAsDefinedInReferenceAssembly = ContractHelper.CreateTypeReference(this.host, ar, "System.Diagnostics.Contracts.Contract");
          this.contractClassDefinedInReferenceAssembly = contractTypeAsDefinedInReferenceAssembly;
        }
        // If that fails, check for the attribute which is defined in the assembly itself
        refAssemblyAttribute = ContractHelper.CreateTypeReference(this.host, ar, "System.Diagnostics.Contracts.ContractReferenceAssemblyAttribute");
        if (AttributeHelper.Contains(ar.Attributes, refAssemblyAttribute))
        {
          // then we're extracting contracts from a reference assembly
          var contractTypeAsDefinedInReferenceAssembly = ContractHelper.CreateTypeReference(this.host, ar, "System.Diagnostics.Contracts.Contract");
          this.contractClassDefinedInReferenceAssembly = contractTypeAsDefinedInReferenceAssembly;
        }

      }

      #region Set contract purity based on whether the method definition has the pure attribute
      var pureAttribute = ContractHelper.CreateTypeReference(this.host, contractAssemblyReference, "System.Diagnostics.Contracts.PureAttribute");
      if (AttributeHelper.Contains(sourceMethodBody.MethodDefinition.Attributes, pureAttribute)) {
        this.CurrentMethodContract.IsPure = true;
      }
      #endregion Set contract purity based on whether the method definition has the pure attribute

      this.oldAndResultExtractor = new OldAndResultExtractor(sourceMethodBody, this.cache, this.referenceCache, contractTypeAsSeenByThisUnit);

      this.extractingFromACtorInAClass = 
        sourceMethodBody.MethodDefinition.IsConstructor
        &&
        !sourceMethodBody.MethodDefinition.ContainingType.IsValueType;
      // TODO: this field makes sense only if extracting a type contract and not a method contract
      // TODO: Need the type loaded so don't have to use a string comparison
      this.extractingTypeContract = TemporaryKludge(sourceMethodBody.MethodDefinition.Attributes, "System.Diagnostics.Contracts.ContractInvariantMethodAttribute");

    }

    //private static NamespaceTypeReference CreateTypeReference(IMetadataHost host, AssemblyIdentity assemblyIdentity, string typeName) {
    //  var assemblyReference = new AssemblyReference(host, assemblyIdentity);
    //  return CreateTypeReference(host, assemblyReference, typeName);
    //}
    public override IBlockStatement Visit(BlockStatement blockStatement) {
      if (blockStatement == null) return blockStatement;
      var stmts = blockStatement.Statements;
      List<IStatement> newStmts = new List<IStatement>();
      if (this.extractingTypeContract) {
        var i = 0;
        // Zero or more local declarations, as long as they don't have initial values
        // REVIEW: If the decompiler was better at finding the first use of a local, then there wouldn't be any of these here
        // REVIEW: But this is an invariant method, so what does it mean to leave these out of the invariants?
        while (i < stmts.Count && IsLocalDeclarationWithoutInitializer(stmts[i])) {
          newStmts.Add(stmts[i]);
          i++;
        }
        // One or more calls to Contract.Invariant
        while (i < stmts.Count && IsInvariant(stmts[i])) {
          ExpressionStatement expressionStatement = stmts[i] as ExpressionStatement;
          IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
          IMethodReference methodToCall = methodCall.MethodToCall;
          List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
          List<ILocation> locations = new List<ILocation>(methodCall.Locations);
          int numArgs = arguments.Count;
          TypeInvariant invariant = new TypeInvariant() {
            Condition = this.Visit(arguments[0]), // REVIEW: Does this need to be visited?
            Description = numArgs >= 2 ? arguments[1] : null,
            OriginalSource = numArgs == 3 ? GetStringFromArgument(arguments[2]) : null,
            Locations = locations
          };
          this.CurrentTypeContract.Invariants.Add(invariant);
          i++;
        }
        while (i < stmts.Count) { // This should be at most just a return statement
          newStmts.Add(stmts[i]);
          i++;
        }
        blockStatement.Statements = newStmts;
        return blockStatement;
      }
      int indexOfLastContractCall = IndexOfLastContractCall(stmts);
      if (indexOfLastContractCall == -1) return blockStatement;
      var beginning = 0;
      if (this.extractingFromACtorInAClass) {
        beginning = 1;
        newStmts.Add(stmts[0]);
      }

      // REVIEW: If the decompiler was better at finding the first use of a local, then there wouldn't be any of these here
      // Take zero or more local declarations, as long as they don't have initial values, and treat them
      // as part of the method body, not of any contract
      while (beginning < stmts.Count && IsLocalDeclarationWithoutInitializer(stmts[beginning])) {
        newStmts.Add(stmts[beginning]);
        beginning++;
      }

      {
        int i = beginning;
        while (i <= indexOfLastContractCall) {

          int j = i;
          while (IsAssignmentToLocal(stmts[j])
            || IsDefinitionOfLocalWithInitializer(stmts[j])
            )
            j++;

          while (j <= indexOfLastContractCall) {
            ConditionalStatement conditionalStatement = stmts[j] as ConditionalStatement;
            if (conditionalStatement != null && IsLegacyRequires(conditionalStatement)) {
              ExtractLegacyRequires(conditionalStatement);
              i = j;
              break;
            }
            ExpressionStatement exprStmt = stmts[j] as ExpressionStatement;
            if (exprStmt != null && IsPreconditionOrPostcondition(exprStmt)) {
              ExtractContractCall(stmts, i, j);
              i = j;
              break;
            }
            j++;
          }
          i++;
        }
      }

      for (int j = indexOfLastContractCall + 1; j < stmts.Count; j++)
        newStmts.Add(stmts[j]);
      blockStatement.Statements = newStmts;
      return blockStatement;
    }

    private int IndexOfLastContractCall(List<IStatement> statements) {
      // search from the end, stop at first call to Requires, Ensures, or EndContractBlock
      for (int i = statements.Count - 1; 0 <= i; i--) {
        IExpressionStatement expressionStatement = statements[i] as IExpressionStatement;
        if (expressionStatement == null) continue;
        IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
        if (methodCall == null) continue;
        IMethodReference methodToCall = methodCall.MethodToCall;
        if (!IsContractMethod(methodToCall))
          continue;
        string mname = methodToCall.Name.Value;
        if (mname == "EndContractBlock") return i;
        if (IsPreconditionOrPostcondition(expressionStatement)) return i;
      }
      return -1;
    }

    private static bool IsLocalDeclarationWithoutInitializer(IStatement statement) {
      ILocalDeclarationStatement localDeclarationStatement = statement as ILocalDeclarationStatement;
      if (localDeclarationStatement == null) return false;
      return localDeclarationStatement.InitialValue == null;
    }

    private bool IsPreconditionOrPostcondition(IStatement statement) {
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement == null) return false;
      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      if (methodCall == null) return false;
      IMethodReference methodToCall = methodCall.MethodToCall;
      if (!IsContractMethod(methodToCall))
          return false;
      string mname = methodToCall.Name.Value;
      if (mname == "EnsuresOnThrow") {
        IGenericMethodInstanceReference/*?*/ genericMethodToCall = methodToCall as IGenericMethodInstanceReference;
        return IteratorHelper.EnumerableCount(genericMethodToCall.GenericArguments) == 1;
      }
      return mname == "Requires" || mname == "Ensures";
    }

    private static bool IsLegacyRequires(IStatement statement) {
      ConditionalStatement conditional = statement as ConditionalStatement;
      if (conditional == null) return false;
      EmptyStatement empty = conditional.FalseBranch as EmptyStatement;
      if (empty == null) return false;
      IBlockStatement blockStatement = conditional.TrueBranch as IBlockStatement;
      if (blockStatement == null) return false;
      List<IStatement> statements = new List<IStatement>(blockStatement.Statements);
      if (statements.Count != 1) return false;
      IStatement stmt = statements[0];
      IThrowStatement throwStatement = stmt as IThrowStatement;
      if (throwStatement != null) {
        return true;
      } else {
        IExpressionStatement es = stmt as IExpressionStatement;
        if (es == null) return false;
        IMethodCall methodCall = es.Expression as IMethodCall;
        if (methodCall == null) return false;
        if (methodCall.Type.TypeCode != PrimitiveTypeCode.Void) return false;
        return true;
      }
    }

    private bool IsInvariant(IStatement statement) {
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement == null) return false;
      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      if (methodCall == null) return false;
      IMethodReference methodToCall = methodCall.MethodToCall;
      if (!IsContractMethod(methodToCall)) return false;
      string mname = methodToCall.Name.Value;
      return mname == "Invariant";
    }

    private bool IsInvariantMethodBody(List<IStatement> statements) {
      var i = 0;
      // Zero or more local declarations, as long as they don't have initial values
      // REVIEW: If the decompiler was better at finding the first use of a local, then there wouldn't be any of these here
      while (i < statements.Count && IsLocalDeclarationWithoutInitializer(statements[i])) i++;
      if (i == statements.Count) return false;
      // One or more calls to Contract.Invariant
      if (!(IsInvariant(statements[i]))) return false;
      while (i < statements.Count && IsInvariant(statements[i])) i++;
      return i == statements.Count || (statements[i] is IReturnStatement && i == statements.Count - 1);
    }

    private static bool IsAssignmentToLocal(IStatement statement) {
      IExpressionStatement exprStatement = statement as IExpressionStatement;
      if (exprStatement == null) return false;
      IAssignment assignment = exprStatement.Expression as IAssignment;
      if (assignment == null) return false;
      ITargetExpression targetExpression = assignment.Target as ITargetExpression;
      if (targetExpression == null) return false;
      return targetExpression.Definition is ILocalDefinition && targetExpression.Instance == null;
    }

    private static bool IsDefinitionOfLocalWithInitializer(IStatement statement) {
      ILocalDeclarationStatement stmt = statement as ILocalDeclarationStatement;
      return stmt != null && stmt.InitialValue != null;
    }

    private static List<T> MkList<T>(T t) { var xs = new List<T>(); xs.Add(t); return xs; }

    private void ExtractContractCall(List<IStatement> statements, int lo, int hi) {
      //^ requires lo <= hi;
      //^ requires IsPreconditionOrPostCondition(statements[hi]);

      BlockExpression be = null;
      if (lo < hi) {
        List<IStatement> stmts = new List<IStatement>();
        List<ILocation> locations = new List<ILocation>();
        for (int i = lo; i < hi; i++) {
          stmts.Add(statements[i]);
          locations.AddRange(statements[i].Locations);
        }
        BlockStatement bs = new BlockStatement() {
          Statements = stmts,
          Locations = locations,
        };
        be = new BlockExpression() {
          BlockStatement = bs,
        };
      }

      ExpressionStatement expressionStatement = statements[hi] as ExpressionStatement;
      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      IMethodReference methodToCall = methodCall.MethodToCall;
      IGenericMethodInstanceReference/*?*/ genericMethodToCall = methodToCall as IGenericMethodInstanceReference;
      if (false && genericMethodToCall != null) { // REVIEW: What difference does it make if it is generic?
        //TODO: ensuresOnThrow
      } else {
        if (IsContractMethod(methodToCall)) {
          string mname = methodToCall.Name.Value;
          List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
          List<ILocation> locations = new List<ILocation>(methodCall.Locations);
          int numArgs = arguments.Count;
          if (numArgs == 0) {
            if (mname == "EndContractBlock") return;
          }
          if (numArgs == 1 || numArgs == 2 || numArgs == 3) {
            if (mname == "Ensures") {
              if (be != null) {
                be.BlockStatement = this.oldAndResultExtractor.Visit(be.BlockStatement);
              }
              var arg = this.oldAndResultExtractor.Visit(arguments[0]);
              if (be != null) {
                be.Expression = arg;
                arg = be;
                locations.AddRange(be.BlockStatement.Locations);
              }
              PostCondition postcondition = new PostCondition() {
                Condition = this.Visit(arg), // REVIEW: Does this need to be visited?
                Description = numArgs >=2 ? arguments[1] : null,
                OriginalSource = numArgs == 3 ? GetStringFromArgument(arguments[2]) : null,
                Locations = locations
              };

              this.CurrentMethodContract.Postconditions.Add(postcondition);
              return;
            }
            if (mname == "EnsuresOnThrow" && genericMethodToCall != null) {
              var genericArgs = new List<ITypeReference>(genericMethodToCall.GenericArguments); // REVIEW: Better way to get the single element from the enumerable?
              var arg = this.oldAndResultExtractor.Visit(arguments[0]);
              if (be != null) {
                be.Expression = arg;
                arg = be;
                locations.AddRange(be.BlockStatement.Locations);
              }
              ThrownException exceptionalPostcondition = new ThrownException() {
                ExceptionType = genericArgs[0],
                Postcondition = new PostCondition() {
                  Condition = this.Visit(arg), // REVIEW: Does this need to be visited?
                  Description = numArgs >= 2 ? arguments[1] : null,
                  OriginalSource = numArgs == 3 ? GetStringFromArgument(arguments[2]) : null,
                  Locations = locations
                }
              };
              this.CurrentMethodContract.ThrownExceptions.Add(exceptionalPostcondition);
              return;
            }
            if (mname == "Requires") {
              var arg = arguments[0];
              if (be != null) {
                be.Expression = arg;
                arg = be;
                locations.AddRange(be.BlockStatement.Locations);
              }
              IExpression thrownException = null;
              IGenericMethodInstanceReference genericMethodInstance = methodToCall as IGenericMethodInstanceReference;
              if (genericMethodInstance != null && 0 < genericMethodInstance.GenericParameterCount) {
                foreach (var a in genericMethodInstance.GenericArguments) {
                  thrownException = new TypeOf() {
                    Type = this.sourceMethodBody.platformType.SystemType,
                    TypeToGet = a,
                    Locations = new List<ILocation>(a.Locations),
                  };
                  break;
                }
              }

              Precondition precondition = new Precondition() {
                AlwaysCheckedAtRuntime = false,
                Condition = this.Visit(arg), // REVIEW: Does this need to be visited?
                Description = numArgs >= 2 ? arguments[1] : null,
                OriginalSource = numArgs == 3 ? GetStringFromArgument(arguments[2]) : null,
                Locations = locations,
                ExceptionToThrow = thrownException,
              };
              this.CurrentMethodContract.Preconditions.Add(precondition);
              return;
            }
          }
        }
      }
      return;
    }

    private static string GetStringFromArgument(IExpression arg) {
      string s = null;
      ICompileTimeConstant c = arg as ICompileTimeConstant;
      if (c != null) {
        s = c.Value as string;
      }
      return s;
    }

    // Extract preconditions from legacy-requires, i.e., preconditions
    // of the form:
    //
    //  if (!P) throw E;
    //
    // or:
    //
    //  if (!P) call MethodReturningVoid(...);
    //
    // where MethodReturningVoid is assumed to never return, but throw (no good
    // way to check that though!!).
    //
    // For the first form, the ExceptionToThrow is E, for the second form
    // it is the call.
    //
    private void ExtractLegacyRequires(ConditionalStatement conditional) {
      //^ requires IsLegacyRequires(conditional);
      EmptyStatement empty = conditional.FalseBranch as EmptyStatement;
      IBlockStatement blockStatement = conditional.TrueBranch as IBlockStatement;
      List<IStatement> statements = new List<IStatement>(blockStatement.Statements);
      IStatement statement = statements[0];
      var locations = new List<ILocation>(conditional.TrueBranch.Locations);
      IExpression failureBehavior;
      IThrowStatement throwStatement = statement as IThrowStatement;
      if (throwStatement != null) {
        failureBehavior = throwStatement.Exception;
        locations.AddRange(throwStatement.Locations);
      } else {
        IExpressionStatement es = statement as IExpressionStatement;
        IMethodCall methodCall = es.Expression as IMethodCall;
        failureBehavior = methodCall; // REVIEW: Does this need to be visited?
        locations.AddRange(es.Locations);
      }
      Precondition precondition = new Precondition() {
        AlwaysCheckedAtRuntime = true,
        Condition = new LogicalNot() { Operand = conditional.Condition }, // REVIEW: Does this need to be visited?
        ExceptionToThrow = failureBehavior,
        Locations = new List<ILocation>(locations)
      };
      this.CurrentMethodContract.Preconditions.Add(precondition);
      return;
    }

    private class OldAndResultExtractor : MethodBodyCodeMutator {
      SourceMethodBody sourceMethodBody;
      ITypeReference contractClass;
      internal OldAndResultExtractor(SourceMethodBody sourceMethodBody, Dictionary<object, object> cache, Dictionary<object, object> referenceCache, ITypeReference contractClass)
        : base(sourceMethodBody.host, true) {
        this.sourceMethodBody = sourceMethodBody;
        this.cache = cache;
        this.referenceCache = referenceCache;
        this.contractClass = contractClass;
      }

      public override IExpression Visit(MethodCall methodCall) {
        IGenericMethodInstanceReference/*?*/ methodToCall = methodCall.MethodToCall as IGenericMethodInstanceReference;
        if (methodToCall != null) {
          if (methodToCall.GenericMethod.ContainingType.InternedKey == this.contractClass.InternedKey){
            //TODO: exists, forall
            if (methodToCall.GenericMethod.Name.Value == "Result") {
              ReturnValue returnValue = new ReturnValue() {
                Type = methodToCall.Type,
                Locations = methodCall.Locations,
              };
              return returnValue;
            }
            if (methodToCall.GenericMethod.Name.Value == "OldValue") {
              OldValue oldValue = new OldValue() {
                Expression = this.Visit(methodCall.Arguments[0]),
                Type = methodToCall.Type,
                Locations = methodCall.Locations,
              };
              return oldValue;
            }

            if (methodToCall.GenericMethod.Name.Value == "ValueAtReturn") {
              AddressDereference addressDereference = new AddressDereference() {
                Address = methodCall.Arguments[0],
                Locations = methodCall.Locations,
                Type = methodToCall.Type,
              };
              return this.Visit(addressDereference);
            }

          }
        }
        return base.Visit(methodCall);
      }
    }
  }


}
