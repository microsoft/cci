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
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.MutableCodeModel.Contracts;
using Microsoft.Cci.ILToCodeModel;
using System.Diagnostics.Contracts;

namespace Microsoft.Cci.MutableContracts {

  /// <summary>
  /// Extracts the contracts into a contract provider, but also mutates the method
  /// bodies (if they are mutable) to delete the code that represented the contracts.
  /// </summary>
  public class ContractExtractor {

    #region Public API
    /// <summary>
    /// Extracts calls to precondition and postcondition methods from the type
    /// System.Diagnostics.Contracts.Contract from the ISourceMethodBody and
    /// returns a pair of the extracted method contract and the residual method
    /// body. If the <paramref name="sourceMethodBody"/> is mutable, then it
    /// has the side-effect of updating the body so that it contains only the
    /// residual code in addition to returning it.
    /// </summary>
    public static MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(IContractAwareHost host, ISourceMethodBody sourceMethodBody, PdbReader/*?*/ pdbReader) {
      var e = new ContractExtractor(sourceMethodBody, host, pdbReader);
      #region Special case for iterators (until decompiler always handles them)
      var moveNext = IteratorContracts.FindClosureMoveNext(host, sourceMethodBody);
      if (moveNext != null) {
        var sourceMoveNext = moveNext as ISourceMethodBody;
        if (sourceMoveNext == null) {
          sourceMoveNext = Decompiler.GetCodeModelFromMetadataModel(host, moveNext, pdbReader, DecompilerOptions.AnonymousDelegates);
        }
        var mc = IteratorContracts.GetMethodContractFromMoveNext(host, e, sourceMethodBody, sourceMoveNext, pdbReader);
        return new MethodContractAndMethodBody(mc, sourceMethodBody.Block);
      }
      #endregion
      return e.SplitMethodBodyIntoContractAndCode(sourceMethodBody.Block);
    }

    /// <summary>
    /// Extracts calls to Contract.Invariant contained in invariant methods within
    /// <paramref name="typeDefinition"/> and returns the resulting type contract.
    /// </summary>
    public static ITypeContract/*?*/ GetTypeContract(IContractAwareHost host, ITypeDefinition typeDefinition, PdbReader/*?*/ pdbReader, ILocalScopeProvider/*?*/ localScopeProvider) {
      var cumulativeContract = new TypeContract();
      var mutableTypeDefinition = typeDefinition as NamedTypeDefinition;
      var invariantMethods = ContractHelper.GetInvariantMethods(typeDefinition);
      var validContract = false;
      foreach (var invariantMethod in invariantMethods) {
        IMethodBody methodBody = invariantMethod.Body;
        ISourceMethodBody/*?*/ sourceMethodBody = methodBody as ISourceMethodBody;
        if (sourceMethodBody == null) {
          sourceMethodBody = Decompiler.GetCodeModelFromMetadataModel(host, methodBody, pdbReader, DecompilerOptions.AnonymousDelegates);
        }
        var e = new Microsoft.Cci.MutableContracts.ContractExtractor(sourceMethodBody, host, pdbReader);
        BlockStatement b = sourceMethodBody.Block as BlockStatement;
        if (b != null) {
          var tc = e.ExtractObjectInvariants(b);
          if (tc != null) {
            cumulativeContract.Invariants.AddRange(tc.Invariants);
            validContract = true;
          }
        }
      }
      var contractFields = new List<IFieldDefinition>();
      foreach (var f in typeDefinition.Fields) {
        if (ContractHelper.IsModel(f) != null) {
          var smd = f as ISpecializedFieldDefinition;
          if (smd != null)
            contractFields.Add(smd.UnspecializedVersion);
          else
            contractFields.Add(f);
          validContract = true;
        }
      }
      if (0 < contractFields.Count)
        cumulativeContract.ContractFields = contractFields;
      var contractMethods = new List<IMethodDefinition>();
      foreach (var m in typeDefinition.Methods) {
        if (ContractHelper.IsModel(m) != null) {
          var smd = m as ISpecializedMethodDefinition;
          if (smd != null)
            contractMethods.Add(smd.UnspecializedVersion);
          else
            contractMethods.Add(m);
          validContract = true;
        }
      }
      if (0 < contractMethods.Count)
        cumulativeContract.ContractMethods = contractMethods;

      return validContract ? cumulativeContract : null;
    }

    #endregion

    internal MethodContract/*?*/ currentMethodContract;
    private MethodContract/*!*/ CurrentMethodContract {
      get {
        if (this.currentMethodContract == null) {
          this.currentMethodContract = new MethodContract();
        }
        return this.currentMethodContract;
      }
    }
    private TypeContract/*?*/ currentTypeContract;
    private TypeContract/*!*/ CurrentTypeContract {
      get {
        if (this.currentTypeContract == null) {
          this.currentTypeContract = new TypeContract();
        }
        return this.currentTypeContract;
      }
    }

    private PdbReader/*?*/ pdbReader;
    private IContractAwareHost host;
    private OldAndResultExtractor oldAndResultExtractor;
    /// <summary>
    /// When not null, this is the abstract type for which the contract class
    /// is holding the contracts for.
    /// </summary>

    private bool methodIsInReferenceAssembly;
    private IMethodDefinition currentMethod;
    private bool inCtor;

    internal bool IsContractMethod(IMethodReference/*?*/ method) {
      if (method == null) return false;
      // Just use name matching: pre-v4 assemblies can have the method defined in many places
      // It might not even be the same for all of the assemblies loaded by the host this extractor
      // is using.
      var t = method.ContainingType;
      return (TypeHelper.GetTypeName(t).Equals("System.Diagnostics.Contracts.Contract"));
    }

    private ContractExtractor(ISourceMethodBody sourceMethodBody, IContractAwareHost host, PdbReader/*?*/ pdbReader) {

      this.host = host;
      this.pdbReader = pdbReader;

      this.currentMethod = sourceMethodBody.MethodDefinition;
      this.inCtor = this.currentMethod.IsConstructor;
 
      // TODO: these fields make sense only if extracting a method contract and not a type contract.

      var definingUnit = TypeHelper.GetDefiningUnit(this.currentMethod.ContainingType.ResolvedType);

      this.methodIsInReferenceAssembly = definingUnit == null ? false : ContractHelper.IsContractReferenceAssembly(this.host, definingUnit);

      #region Set contract purity based on whether the method definition has the pure attribute
      if (ContractHelper.IsPure(this.host, this.currentMethod)) {
        this.CurrentMethodContract.IsPure = true;
      }
      #endregion Set contract purity based on whether the method definition has the pure attribute

      this.oldAndResultExtractor = new OldAndResultExtractor(this.host, sourceMethodBody, this.IsContractMethod);

    }

    private MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(IBlockStatement blockStatement) {
      // Don't start with an empty contract because the ctor may have set some things in it
      var bs = this.ExtractContractsAndPossiblyMutateBody(blockStatement);
      if (this.currentMethodContract != null) {
        this.currentMethodContract = ReplacePrivateFieldsThatHavePublicProperties(this.host, this.currentMethod.ContainingTypeDefinition, this.currentMethodContract);
      }
      return new MethodContractAndMethodBody(this.currentMethodContract, bs);
    }

    private ITypeContract/*?*/ ExtractObjectInvariants(IBlockStatement blockStatement) {
      List<ITypeInvariant> invariants = new List<ITypeInvariant>();
      List<IStatement> currentClump = new List<IStatement>();
      foreach (var s in blockStatement.Statements) {
        currentClump.Add(s);
        if (IsInvariant(s)) {
          var inv = ExtractObjectInvariant(currentClump);
          invariants.Add(inv);
          currentClump = new List<IStatement>();
        }
      }
      if (0 < invariants.Count)
        return new TypeContract() {
          Invariants = invariants,
        };
      else
        return null;
    }

    private TypeInvariant ExtractObjectInvariant(List<IStatement> clump) {
      var lastIndex = clump.Count - 1;
      ExpressionStatement/*?*/ expressionStatement = clump[lastIndex] as ExpressionStatement;

      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      IMethodReference methodToCall = methodCall.MethodToCall;
      List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
      List<ILocation> locations = new List<ILocation>(methodCall.Locations);
      for (int i = lastIndex; 0 <= i; i--) {
        if (IteratorHelper.EnumerableIsNotEmpty(clump[i].Locations)) {
          locations.AddRange(clump[i].Locations);
          break;
        }
      }
      int numArgs = arguments.Count;

      IExpression contractExpression;
      if (clump.Count == 1) {
        contractExpression = arguments[0];
      } else {
        var allButLastStatement = new List<IStatement>();
        for (int i = 0; i < lastIndex; i++) {
          allButLastStatement.Add(clump[i]);
        }
        contractExpression = new BlockExpression() {
          BlockStatement = new BlockStatement() {
            Statements = allButLastStatement,
          },
          Expression = arguments[0],
          Type = this.host.PlatformType.SystemBoolean,
        };
      }

      string/*?*/ origSource = null;
      if (this.methodIsInReferenceAssembly) {
        origSource = GetStringFromArgument(arguments[2]);
      } else {
        TryGetConditionText(this.pdbReader, locations, numArgs, out origSource);
      }

      TypeInvariant invariant = new TypeInvariant() {
        Condition = contractExpression,
        Description = numArgs >= 2 ? arguments[1] : null,
        OriginalSource = origSource,
        Locations = locations,
      };
      return invariant;
    }

    /// <summary>
    /// If the decompiler were perfect, the contracts would all be either method calls (to the contract
    /// class) or conditional statements (for legacy requires). But it is not yet perfect... so partially
    /// decompiled contracts can also contain push statements, gotos, labels, and conditional statements.
    /// </summary>
    public IBlockStatement/*?*/ ExtractContractsAndPossiblyMutateBody(IBlockStatement block) {

      var contractBlock = ContainsContract(block);
      if (contractBlock == null) return block;

      List<IStatement> stmts;
      var mutableBlock = contractBlock as BlockStatement;
      if (mutableBlock != null)
        stmts = mutableBlock.Statements;
      else
        stmts = new List<IStatement>(contractBlock.Statements);

      List<IStatement> newStmts = null;

      var i = this.inCtor ? IndexOfStatementAfterCallToCtor(stmts) : 0;

      // Find last statement that could be a contract
      var n = IndexOfLastContractCall(stmts);
        

      // skip prefix of nops, locals.
      // Also skip calls to ctors (i.e., "base" or "this" calls within ctors)
      // Also skip calls that assign to fields of a class. these are present in constructors.
      while (i < n) {
        var s = stmts[i++];
        if (s is IEmptyStatement)
          continue;
        else if (s is ILocalDeclarationStatement)
          continue;
        else if (this.inCtor && IsFieldInitializer(s))
          continue;
        else {
          i--;
          break;
        }
      }

      // consequence of the decompiler not being perfect: need to find the last call to a contract method and use that to stop the search
      var j = i;
      IStatement lastContract = null;
      var localDecls = new List<IStatement>();

      if (n < stmts.Count)
        lastContract = stmts[n];

      if (lastContract == null)
        return block;
      Contract.Assert(lastContract != null);

      // back up over any local declaration statements for locals used
      // in the first contract
      if (0 < i && stmts[i - 1] is ILocalDeclarationStatement) {
        var k = i - 1;
        while (0 <= k && stmts[k] is ILocalDeclarationStatement) k--;
        i = k + 1;
      }

      if (mutableBlock != null) {
        newStmts = new List<IStatement>();
        newStmts.AddRange(stmts.GetRange(0,i));
        if (0 < localDecls.Count)
          newStmts.AddRange(localDecls);
      }

      var foundNonLegacyContract = false; // extract legacy requires only until a non-legacy is found
      var currentClump = new List<IStatement>();
      while (i < n+1) {
        var s = stmts[i++];
        currentClump.Add(s);
        if (IsPrePostEndOrLegacy(s, !foundNonLegacyContract)) {
          if (IsPreconditionOrPostcondition(s)) foundNonLegacyContract = true;
          ExtractContract(currentClump);
          if (s == lastContract)
            break;
          currentClump = new List<IStatement>();
        } 
      }

      if (mutableBlock != null) {
        newStmts.AddRange(stmts.GetRange(i, /*n*/stmts.Count - i));
        IntroduceLocalDeclarationsForUndeclaredLocals(newStmts);
        mutableBlock.Statements = newStmts;
      }

      return block;
    }

    private void IntroduceLocalDeclarationsForUndeclaredLocals(List<IStatement> newStmts) {
      var undeclaredLocals = UndeclaredLocalFinder.GetDeclarationsForUndeclaredLocals(newStmts);
      newStmts.InsertRange(0, undeclaredLocals);
    }

    internal int IndexOfLastContractCall(List<IStatement> stmts) {
      var k = stmts.Count - 1;
      while (0 <= k) {
        var s = stmts[k];
        if (IsPrePostEndOrLegacy(s, false)) break;
        k--;
      }
      return k;
    }

    private int IndexOfStatementAfterCallToCtor(List<IStatement> stmts) {
      if (this.currentMethod.ContainingType.IsValueType) return IndexOfStatementAfterCallToCtorInStruct(stmts);
      var i = stmts.Count - 1;
      while (0 <= i) {
        var s = stmts[i];
        if (IsCallToCtor(s)) return i+1;
        i--;
      }
      return 0;
    }

    /// <summary>
    /// Bizarre, but true code found in an assembly:
    /// 
    /// public struct S {
    ///   private S(int x) : this() {
    ///      ...
    ///   }
    /// }
    /// 
    /// Completely useless, but it messes up the contract extraction because any
    /// contracts coming after the call to "this" (which shows up as an assignment
    /// of the default value of S to "this") won't get found unless this "preamble"
    /// is skipped over.
    /// </summary>
    private int IndexOfStatementAfterCallToCtorInStruct(List<IStatement> stmts) {
      if (stmts.Count == 0) return 0;
      var es = stmts[0] as IExpressionStatement;
      if (es == null) return 0;
      var assign = es.Expression as IAssignment;
      if (assign == null) return 0;
      if (!(assign.Source is IDefaultValue)) return 0;
      var te = assign.Target;
      var addrDeref = te.Definition as IAddressDereference;
      if (addrDeref == null) return 0;
      return (addrDeref.Address) is IThisReference ? 1 : 0;
    }

    private IBlockStatement/*?*/ ContainsContract(IBlockStatement block) {
      foreach (var s in block.Statements) {
        if (IsPrePostEndOrLegacy(s, false)) {
          return block;
        } else {
          var bs = s as IBlockStatement;
          if (bs != null) {
            var found = ContainsContract(bs);
            if (found != null) return found;
          }
        }
      }
      return null;
    }

    private bool IsCallToCtor(IStatement s) {
      var mc = IsMethodCall(s);
      return mc != null && mc.MethodToCall.ResolvedMethod.IsConstructor;
    }
    private bool IsFieldInitializer(IStatement s) {
      var es = s as IExpressionStatement;
      if (es == null) return false;
      var assign = es.Expression as IAssignment;
      if (assign == null) return false;
      var te = assign.Target;
      var f = te.Definition as IFieldReference;
      if (f != null) return true;
      // fields of struct type have an initializer that looks like *(&(addressableExpr(null,f)))
      var addrDeref = te.Definition as IAddressDereference;
      if (addrDeref != null) {
        var addrOf = addrDeref.Address as IAddressOf;
        if (addrOf != null) {
          f = addrOf.Expression.Definition as IFieldReference;
          return f != null;
        }
      }
      return false;
    }
    private bool IsPotentialPartOfContract(IStatement s) {
      if (s is IConditionalStatement)
        return true;
      else if (s is IPushStatement)
        return true;
      else if (s is ILabeledStatement)
        return true;
      else if (s is IGotoStatement)
        return true;
      else if (s is ILocalDeclarationStatement)
        return true;
      var es = s as IExpressionStatement;
      if (es != null && es.Expression is IMethodCall)
        return true;
      return false;
    }

    internal void ExtractContract(List<IStatement> currentClump) {
      var lastIndex = currentClump.Count - 1;
      ExpressionStatement/*?*/ expressionStatement = currentClump[lastIndex] as ExpressionStatement;
      if (expressionStatement == null) {
        if (IsLegacyRequires(currentClump[lastIndex])) {
          ExtractLegacyRequires(currentClump);
        }
      } else {
        IMethodCall/*?*/ methodCall = expressionStatement.Expression as IMethodCall;
        if (methodCall != null) {
          IMethodReference methodToCall = methodCall.MethodToCall;
          if (IsContractMethod(methodToCall)) {
            string mname = methodToCall.Name.Value;
            List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
            int numArgs = arguments.Count;
            if (numArgs == 0 && mname == "EndContractBlock") {
              this.CurrentMethodContract.Locations.AddRange(methodCall.Locations);
              return;
            }
            if (!(numArgs == 1 || numArgs == 2 || numArgs == 3)) return;

            var locations = new List<ILocation>(methodCall.Locations);
            if (locations.Count == 0) {
              for (int i = lastIndex; 0 <= i; i--) {
                if (IteratorHelper.EnumerableIsNotEmpty(currentClump[i].Locations)) {
                  locations.AddRange(currentClump[i].Locations);
                  break;
                }
              }
            }

            // Create expression for contract
            IExpression contractExpression;
            if (currentClump.Count == 1) {
              contractExpression = arguments[0];
            } else {
              var allButLastStatement = new List<IStatement>();
              for (int i = 0; i < lastIndex; i++) {
                allButLastStatement.Add(currentClump[i]);
              }
              contractExpression = new BlockExpression() {
                BlockStatement = new BlockStatement() {
                  Statements = allButLastStatement,
                },
                Expression = arguments[0],
                Type = this.host.PlatformType.SystemBoolean,
              };
            }

            var isModel = FindModelMembers.ContainsModelMembers(contractExpression);

            string/*?*/ origSource = null;
            if (this.methodIsInReferenceAssembly) {
              origSource = GetStringFromArgument(arguments[2]);
            } else {
              TryGetConditionText(this.pdbReader, locations, numArgs, out origSource);
            }
            IExpression/*?*/ description = numArgs >= 2 ? arguments[1] : null;

            IGenericMethodInstanceReference/*?*/ genericMethodToCall = methodToCall as IGenericMethodInstanceReference;

            if (mname == "Ensures") {
              contractExpression = this.oldAndResultExtractor.Rewrite(contractExpression);
              Postcondition postcondition = new Postcondition() {
                Condition = contractExpression,
                Description = description,
                IsModel = isModel,
                OriginalSource = origSource,
                Locations = locations,
              };
              this.CurrentMethodContract.Postconditions.Add(postcondition);
              this.CurrentMethodContract.Locations.AddRange(postcondition.Locations);
              return;
            }
            if (mname == "EnsuresOnThrow" && genericMethodToCall != null) {
              var genericArgs = new List<ITypeReference>(genericMethodToCall.GenericArguments); // REVIEW: Better way to get the single element from the enumerable?
              contractExpression = this.oldAndResultExtractor.Rewrite(contractExpression);
              ThrownException exceptionalPostcondition = new ThrownException() {
                ExceptionType = genericArgs[0],
                Postcondition = new Postcondition() {
                  Condition = contractExpression,
                  Description = description,
                  IsModel = isModel,
                  OriginalSource = origSource,
                  Locations = locations,
                }
              };
              this.CurrentMethodContract.ThrownExceptions.Add(exceptionalPostcondition);
              this.CurrentMethodContract.Locations.AddRange(exceptionalPostcondition.Postcondition.Locations);
              return;
            }
            if (mname == "Requires") {
              IExpression thrownException = null;
              IGenericMethodInstanceReference genericMethodInstance = methodToCall as IGenericMethodInstanceReference;
              if (genericMethodInstance != null && 0 < genericMethodInstance.GenericMethod.GenericParameterCount) {
                foreach (var a in genericMethodInstance.GenericArguments) {
                  thrownException = new TypeOf() {
                    Type = this.host.PlatformType.SystemType,
                    TypeToGet = a,
                    Locations = new List<ILocation>(a.Locations),
                  };
                  break;
                }
              }
              Precondition precondition = new Precondition() {
                AlwaysCheckedAtRuntime = false,
                Condition = contractExpression,
                Description = description,
                IsModel = isModel,
                OriginalSource = origSource,
                Locations = locations,
                ExceptionToThrow = thrownException,
              };
              this.CurrentMethodContract.Preconditions.Add(precondition);
              this.CurrentMethodContract.Locations.AddRange(precondition.Locations);
              return;
            }
          } else if (IsValidatorOrAbbreviator(expressionStatement)) {
            var abbreviatorDef = methodToCall.ResolvedMethod;
            var mc = ContractHelper.GetMethodContractFor(this.host, abbreviatorDef);
            if (mc != null) {

              var copier = new CodeAndContractDeepCopier(this.host);
              var copyOfContract = copier.Copy(mc);

              //var gmir = methodToCall as IGenericMethodInstanceReference;
              //if (gmir != null) {
              //  // If there are any references to the abbreviator's parameters in the contract,
              //  // then they are to the unspecialized parameters. So the abbreviatorDef needs
              //  // to get unspecialized so that the BetaReducer can get the right parameters
              //  // out of it to use in its mapping table.
              //  abbreviatorDef = gmir.GenericMethod.ResolvedMethod;
              //}

              var brewriter = new BetaReducer(host, abbreviatorDef, this.currentMethod, new List<IExpression>(methodCall.Arguments));
              brewriter.RewriteChildren(copyOfContract);

              //if (gmir != null) {
              //  var typeMap = new Dictionary<uint, ITypeReference>();
              //  IteratorHelper.Zip(abbreviatorDef.GenericParameters, gmir.GenericArguments,
              //    (i, j) => typeMap.Add(i.InternedKey, j));

              //  var cs2 = new CodeSpecializer2(this.host, typeMap);
              //  cs2.RewriteChildren(copyOfContract);
              //}
              copyOfContract.Locations.InsertRange(0, methodCall.Locations);
              if (this.currentMethodContract == null)
                this.currentMethodContract = new MethodContract(copyOfContract);
              else
                ContractHelper.AddMethodContract(this.currentMethodContract, copyOfContract);

            }
          }
        }
      }
    }

    internal bool IsPrePostEndOrLegacy(IStatement statement, bool countLegacy) {
      if (statement is IBlockStatement) return false; // each block is searched separately, don't descend down into nested blocks
      if (countLegacy && IsLegacyRequires(statement)) return true;
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement == null) return false; ;
      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      if (methodCall == null) return false;
      IMethodReference methodToCall = methodCall.MethodToCall;
      if (ContractHelper.IsValidatorOrAbbreviator(methodToCall)) return true;
      if (!IsContractMethod(methodToCall)) return false;
      string mname = methodToCall.Name.Value;
      if (mname == "EndContractBlock") return true;
      if (IsPreconditionOrPostcondition(expressionStatement)) return true;
      return false;
    }

    private IMethodCall/*?*/ IsMethodCall(IStatement statement) {
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement == null) return null;
      return expressionStatement.Expression as IMethodCall;
    }

    internal bool IsPreconditionOrPostcondition(IStatement statement) {
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
    private static bool IsValidatorOrAbbreviator(IStatement statement) {
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement == null) return false;
      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      if (methodCall == null) return false;
      IMethodReference methodToCall = methodCall.MethodToCall;
      return ContractHelper.IsValidatorOrAbbreviator(methodToCall);
    }
    private static bool IsLegacyRequires(IStatement statement) {
      ConditionalStatement conditional = statement as ConditionalStatement;
      if (conditional == null) return false;
      EmptyStatement empty = conditional.FalseBranch as EmptyStatement;
      if (empty == null) return false;
      IBlockStatement blockStatement = conditional.TrueBranch as IBlockStatement;
      if (blockStatement == null) return false;
      List<IStatement> statements = new List<IStatement>(blockStatement.Statements);
      // Zero or more assignments to locals
      //TODO: Make sure they are compiler-generated locals
      var count = statements.Count;
      int i = 0;
      while (i < count && statements[i] is IEmptyStatement) i++;
      while (i < count && IsAssignmentToLocal(statements[i])) i++;
      if (i == count) return false;
      IStatement stmt = statements[i];
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

    /// <summary>
    /// Returns true iff statement is "loc := e" or "loc[i] := e"
    /// </summary>
    private static bool IsAssignmentToLocal(IStatement statement) {
      IExpressionStatement exprStatement = statement as IExpressionStatement;
      if (exprStatement == null) return false;
      IAssignment assignment = exprStatement.Expression as IAssignment;
      if (assignment == null) return false;
      ITargetExpression targetExpression = assignment.Target as ITargetExpression;
      if (targetExpression == null) return false;
      if (targetExpression.Definition is ILocalDefinition && targetExpression.Instance == null) return true;
      IArrayIndexer iai = targetExpression.Definition as IArrayIndexer;
      if (iai == null) return false;
      IBoundExpression be = targetExpression.Instance as IBoundExpression;
      if (be == null) return false;
      return (be.Definition is ILocalDefinition && be.Instance == null);
    }

    private static bool TryGetConditionText(PdbReader pdbReader, IEnumerable<ILocation> locations, int numArgs, out string sourceText, bool pleaseNegate = false) {
      int startColumn;
      if (!TryGetSourceText(pdbReader, locations, out sourceText, out startColumn)) return false;
      int firstSourceTextIndex = sourceText.IndexOf('(');
      // Special case: for VB legacy preconditions, no open paren, sourceText is of the form: "If ... Then"
      if (firstSourceTextIndex == -1 && pleaseNegate) {
        var match = new System.Text.RegularExpressions.Regex(@"If (.+) Then").Match(sourceText);
        if (match.Success) {
          sourceText = match.Groups[1].Value;
          sourceText = String.Format("Not({0})", sourceText);
          return true;
        }
      }
      // Special case: for VB Requires<E>, the exception type is written as "(Of E)" so need to skip all of that.
      if (firstSourceTextIndex != -1 && firstSourceTextIndex + 3 < sourceText.Length && (String.Compare(sourceText, firstSourceTextIndex+1, "Of ", 0, 3) == 0)) {
        firstSourceTextIndex += sourceText.Substring(firstSourceTextIndex + 1).IndexOf('(') + 1;
      }
      firstSourceTextIndex = firstSourceTextIndex == -1 ? 0 : firstSourceTextIndex + 1; // the +1 is to skip the opening paren
      int lastSourceTextIndex = sourceText.LastIndexOf(')'); // hopefully the closing paren of the contract method call
      if (numArgs != 1) {
        // need to back up to the character after the first argument to the contract method call
        lastSourceTextIndex = IndexOfWhileSkippingBalancedThings(sourceText, lastSourceTextIndex, ',');
      }
      var len = lastSourceTextIndex - firstSourceTextIndex;
      // check precondition of Substring
      if (lastSourceTextIndex <= firstSourceTextIndex || firstSourceTextIndex + len >= sourceText.Length) {
        //Console.WriteLine(sourceText);
        len = sourceText.Length - firstSourceTextIndex; // if something went wrong, at least get the whole source text.
      }
      sourceText = sourceText.Substring(firstSourceTextIndex, len);
      if (sourceText != null) {
        sourceText = new System.Text.RegularExpressions.Regex(@"\s+").Replace(sourceText, " ");
        sourceText = sourceText.Trim();
      } 
      if (pleaseNegate)
        sourceText = BrianGru.NegatePredicate(sourceText);

      // This commented-out code was used when we wanted the text to be formatted as it was in the source file, line breaks and all
      //var indentSize = firstSourceTextIndex + startColumn;
      //sourceText = AdjustIndentationOfMultilineSourceText(sourceText, indentSize);
      return true;
    }
    private static bool TryGetSourceText(PdbReader pdbReader, IEnumerable<ILocation> locations, out string/*?*/ sourceText, out int startColumn) {
      sourceText = null;
      startColumn = 0; // columns begin at 1, so this can work as a null value
      foreach (var loc in locations) {
        if (pdbReader != null)
        {
          foreach (IPrimarySourceLocation psloc in pdbReader.GetClosestPrimarySourceLocationsFor(loc))
          {
            if (!String.IsNullOrEmpty(psloc.Source))
            {
              sourceText = psloc.Source;
              startColumn = psloc.StartColumn;
              break;
            }
          }
        }
        else
        {
          var psloc = loc as IPrimarySourceLocation;
          if (psloc != null)
          {
            if (!String.IsNullOrEmpty(psloc.Source))
            {
              sourceText = psloc.Source;
              startColumn = psloc.StartColumn;
              break;
            }
          }
        }
        if (sourceText != null) break;
      }
      return sourceText != null;
    }
    private static int IndexOfWhileSkippingBalancedThings(string source, int endIndex, char targetChar) {
      int i = endIndex;
      while (0 <= i) {
        if (source[i] == targetChar) break;
        else if (source[i] == '"') i = IndexOfWhileSkippingBalancedThings(source, i - 1, '"') - 1;
        else if (source[i] == '>') i = IndexOfWhileSkippingBalancedThings(source, i - 1, '<') - 1;
        else if (targetChar != '"' && source[i] == '\'') {
          // then we're not within a string, so assume this is the end of a character
          // skip the closing single quote, the char, and the open single quote
          if (source[i - 2] == '\\') { // then source[i-1] is an escaped character, need to skip one more position
            i -= 4;
          } else {
            i -= 3;
          }
        } else i--;
      }
      return i;
    }
    private static char[] WhiteSpace = { ' ', '\t' };
    private static string AdjustIndentationOfMultilineSourceText(string sourceText, int trimLength) {
      if (!sourceText.Contains("\n")) return sourceText;
      var lines = sourceText.Split('\n');
      if (lines.Length == 1) return sourceText;
      var trimmedSecondLine = lines[1].TrimStart(WhiteSpace);
      for (int i = 1; i < lines.Length; i++) {
        var currentLine = lines[i];
        if (trimLength < currentLine.Length) {
          var prefix = currentLine.Substring(0, trimLength);
          bool allBlank = true;
          for (int j = 0, m = prefix.Length; j < m; j++)
            if (prefix[j] != ' ')
              allBlank = false;
          if (allBlank)
            lines[i] = currentLine.Substring(trimLength);
        }
      }
      var numberOfLinesToJoin = String.IsNullOrEmpty(lines[lines.Length - 1].TrimStart(WhiteSpace)) ? lines.Length - 1 : lines.Length;
      return String.Join("\n", lines, 0, numberOfLinesToJoin);
    }

    private static string/*?*/ GetStringFromArgument(IExpression arg) {
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
    private void ExtractLegacyRequires(List<IStatement> currentClump) {
      var lastIndex = currentClump.Count - 1;
      ConditionalStatement conditional = currentClump[lastIndex] as ConditionalStatement;
      //^ requires IsLegacyRequires(conditional);
      EmptyStatement empty = conditional.FalseBranch as EmptyStatement;
      IBlockStatement blockStatement = conditional.TrueBranch as IBlockStatement;
      List<IStatement> statements = new List<IStatement>(blockStatement.Statements);
      IStatement statement = statements.FindLast(s => !(s is IEmptyStatement));
      IExpression failureBehavior;
      IThrowStatement throwStatement = statement as IThrowStatement;
      var locations = new List<ILocation>(conditional.Condition.Locations);
      if (locations.Count == 0) {
        locations.AddRange(conditional.Locations);
      }

      string origSource = null;
      if (0 < locations.Count) {
        TryGetConditionText(this.pdbReader, locations, 1, out origSource, true);
      }

      if (throwStatement != null) {
        if (statements.Count == 1) {
          failureBehavior = throwStatement.Exception;
        } else {
          var localAssignments = new List<IStatement>();
          for (int i = 0; i < statements.Count - 1; i++) {
            localAssignments.Add(statements[i]);
          }
          failureBehavior = new BlockExpression() {
            BlockStatement = new BlockStatement() {
              Statements = localAssignments,
            },
            Expression = throwStatement.Exception,
          };
        }
        locations.AddRange(throwStatement.Locations);
      } else {
        IExpressionStatement es = statement as IExpressionStatement;
        IMethodCall methodCall = es.Expression as IMethodCall;
        failureBehavior = methodCall;
        locations.AddRange(es.Locations);
      }

      // Create expression for contract
      IExpression contractExpression;
      if (currentClump.Count == 1) {
        contractExpression = conditional.Condition;
      } else {
        var allButLastStatement = new List<IStatement>();
        for (int i = 0; i < lastIndex; i++) {
          allButLastStatement.Add(currentClump[i]);
        }
        contractExpression = new BlockExpression() {
          BlockStatement = new BlockStatement() {
            Statements = allButLastStatement,
          },
          Expression = conditional.Condition,
          Type = this.host.PlatformType.SystemBoolean,
        };
      }

      Precondition precondition = new Precondition() {
        AlwaysCheckedAtRuntime = true,
        Condition = new LogicalNot() {
          Locations = new List<ILocation>(locations),
          Operand = contractExpression,
          Type = this.host.PlatformType.SystemBoolean,
        },
        ExceptionToThrow = failureBehavior,
        //        Locations = new List<ILocation>(IteratorHelper.GetConversionEnumerable<IPrimarySourceLocation, ILocation>(this.pdbReader.GetClosestPrimarySourceLocationsFor(locations))), //new List<ILocation>(locations),
        Locations = new List<ILocation>(locations),
        OriginalSource = origSource,
      };
      this.CurrentMethodContract.Preconditions.Add(precondition);
      this.CurrentMethodContract.Locations.AddRange(precondition.Locations);
      return;
    }

    private void ExtractValidatorOrAbbreviator(ExpressionStatement expressionStatement) {
      //^ requires expressionStatement.Expression is IMethodCall;
      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      IMethodReference methodToCall = methodCall.MethodToCall;
      IMethodDefinition methodDefinition = methodToCall.ResolvedMethod;
      IMethodContract/*?*/ validatorContract = null;
      validatorContract = ContractHelper.GetMethodContractFor(this.host, methodDefinition);
      if (validatorContract != null) {
        validatorContract = new CodeAndContractDeepCopier(host).Copy(validatorContract);
        var bpta = new ReplaceParametersWithArguments(this.host, methodDefinition, methodCall);
        validatorContract = bpta.Rewrite(validatorContract);
        Microsoft.Cci.MutableContracts.ContractHelper.AddMethodContract(this.CurrentMethodContract, validatorContract);
      }
    }


    /// <summary>
    /// A mutator that replaces the parameters of a method with the arguments from a method call.
    /// It does *not* make a copy of the contract, so any client using this needs to make a copy
    /// of the entire contract if that is needed.
    /// </summary>
    private sealed class ReplaceParametersWithArguments : CodeAndContractRewriter {
      private IMethodDefinition methodDefinition;
      private IMethodCall methodCall;
      private List<IExpression> arguments;
      /// <summary>
      /// Creates a mutator that replaces all occurrences of parameters from the target method with those from the source method.
      /// </summary>
      public ReplaceParametersWithArguments(IMetadataHost host, IMethodDefinition methodDefinition, IMethodCall methodCall)
        : base(host) { 
        this.methodDefinition = methodDefinition;
        this.methodCall = methodCall;
        this.arguments = new List<IExpression>(methodCall.Arguments);
      }

      /// <summary>
      /// Visits the specified bound expression.
      /// </summary>
      /// <param name="boundExpression">The bound expression.</param>
      /// <returns></returns>
      public override IExpression Rewrite(IBoundExpression boundExpression) {
        ParameterDefinition/*?*/ par = boundExpression.Definition as ParameterDefinition;
        if (par != null && par.ContainingSignature == this.methodDefinition) {
          return this.arguments[par.Index];
        } else {
          return base.Rewrite(boundExpression);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class AssertAssumeExtractor : CodeRewriter {

      PdbReader/*?*/ pdbReader;

      /// <summary>
      /// 
      /// </summary>
      /// <param name="host"></param>
      /// <param name="pdbReader"></param>
      public AssertAssumeExtractor(IMetadataHost host, PdbReader/*?*/ pdbReader)
        : base(host) {
        this.pdbReader = pdbReader;
      }

      /// <summary>
      /// Rewrites the given expression statement.
      /// </summary>
      /// <param name="expressionStatement"></param>
      /// <returns></returns>
      public override IStatement Rewrite(IExpressionStatement expressionStatement) {
        IMethodCall/*?*/ methodCall = expressionStatement.Expression as IMethodCall;
        if (methodCall == null) goto JustVisit;
        IMethodReference methodToCall = methodCall.MethodToCall;
        var containingType = methodToCall.ContainingType;
        var typeName = TypeHelper.GetTypeName(containingType);
        if (!(typeName.Equals("System.Diagnostics.Contracts.Contract"))) goto JustVisit;
        string mname = methodToCall.Name.Value;
        List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
        List<ILocation> locations = new List<ILocation>(methodCall.Locations);
        int numArgs = arguments.Count;
        if (numArgs != 1 && numArgs != 2) goto JustVisit;
        if (mname != "Assert" && mname != "Assume") goto JustVisit;

        string/*?*/ origSource = null;
        ContractExtractor.TryGetConditionText(this.pdbReader, locations, numArgs, out origSource);


        var locations2 = this.pdbReader == null ? new List<ILocation>() :
          new List<ILocation>(IteratorHelper.GetConversionEnumerable<IPrimarySourceLocation, ILocation>(this.pdbReader.GetClosestPrimarySourceLocationsFor(locations)));
        if (mname == "Assert") {
          AssertStatement assertStatement = new AssertStatement() {
            Condition = this.Rewrite(arguments[0]),
            Description = numArgs >= 2? arguments[1] : null,
            OriginalSource = origSource,
            Locations = locations2,
          };
          return assertStatement;
        }
        if (mname == "Assume") {
          AssumeStatement assumeStatement = new AssumeStatement() {
            Condition = this.Rewrite(arguments[0]),
            Description = numArgs >= 2 ? arguments[1] : null,
            OriginalSource = origSource,
            Locations = locations2,
          };
          return assumeStatement;
        }
      JustVisit:
        return base.Rewrite(expressionStatement);
      }



    }

    /// <summary>
    /// Replaces any BoundExpressions of the form (this,x) in the methodContract with a method call where the method
    /// being called is P where x is a private field of the source type that has been marked as [ContractPublicPropertyName("P")].
    /// </summary>
    private static MethodContract ReplacePrivateFieldsThatHavePublicProperties(IMetadataHost host, ITypeDefinition sourceType, MethodContract methodContract) {

      Dictionary<IName, IMethodReference> field2Getter = new Dictionary<IName, IMethodReference>();

      foreach (var mem in sourceType.Members) {
        IFieldDefinition f = mem as IFieldDefinition;
        if (f == null) continue;
        string propertyName = ContractHelper.GetStringArgumentFromAttribute(f.Attributes, "System.Diagnostics.Contracts.ContractPublicPropertyNameAttribute");
        if (propertyName != null) {
          foreach (var p in sourceType.Properties) {
            if (p.Name.Value == propertyName) {
              field2Getter.Add(f.Name, p.Getter);
              break;
            }
          }
        }
      }
      if (0 < field2Getter.Count) {
        SubstitutePropertyGetterForField s = new SubstitutePropertyGetterForField(host, sourceType, field2Getter);
        methodContract.Preconditions = (List<IPrecondition>)s.Rewrite(methodContract.Preconditions);
        //methodContract = (MethodContract)s.Visit(methodContract);
      }
      return methodContract;
    }

    private class SubstitutePropertyGetterForField : CodeAndContractRewriter {
      ITypeDefinition sourceType;
      Dictionary<IName, IMethodReference> substitution;
      public SubstitutePropertyGetterForField(IMetadataHost host, ITypeDefinition sourceType, Dictionary<IName, IMethodReference> substitutionTable)
        : base(host) {
        this.sourceType = sourceType;
        this.substitution = substitutionTable;
      }
      public override IExpression Rewrite(IBoundExpression boundExpression) {
        IFieldDefinition f = boundExpression.Definition as IFieldDefinition;
        if (f != null && f.ContainingType.InternedKey == this.sourceType.InternedKey && this.substitution.ContainsKey(f.Name)) {
          IMethodReference m = this.substitution[f.Name];
          return new MethodCall() {
            IsVirtualCall = true,
            MethodToCall = m,
            Type = m.Type,
            ThisArgument = boundExpression.Instance,
          };
        } else {
          return base.Rewrite(boundExpression);
        }
      }
    }

    private class FindModelMembers : CodeTraverser {
      private bool foundModelMember = false;
      private FindModelMembers() { }
      public static bool ContainsModelMembers(IExpression expression) {
        var fmm = new FindModelMembers();
        fmm.Traverse(expression);
        return fmm.foundModelMember;
      }
      public override void TraverseChildren(IMethodCall methodCall) {
        if (ContractHelper.IsModel(methodCall.MethodToCall) != null)
          this.foundModelMember = true;
        base.TraverseChildren(methodCall);
      }
    }

    private class UndeclaredLocalFinder : CodeTraverser {
      private UndeclaredLocalFinder() { }
      private List<IStatement> newDeclarations = new List<IStatement>();
      private HashSet<ILocalDefinition> declaredLocals = new HashSet<ILocalDefinition>();
      public static List<IStatement> GetDeclarationsForUndeclaredLocals(List<IStatement> stmts) {
        var me = new UndeclaredLocalFinder();
        me.Traverse(stmts);
        return me.newDeclarations;
      }
      public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
        this.declaredLocals.Add(localDeclarationStatement.LocalVariable);
        base.TraverseChildren(localDeclarationStatement);
      }
      public override void TraverseChildren(ILocalDefinition localDefinition) {
        if (!this.declaredLocals.Contains(localDefinition)) {
          this.declaredLocals.Add(localDefinition);
          this.newDeclarations.Add(new LocalDeclarationStatement() { InitialValue = null, LocalVariable = localDefinition, });
        }
      }
    }

  }

  internal class OldAndResultExtractor : CodeRewriter {
    ISourceMethodBody sourceMethodBody;
    AnonymousDelegate/*?*/ currentAnonymousDelegate;
    private Predicate<IMethodReference> isContractMethod;

    internal OldAndResultExtractor(IMetadataHost host, ISourceMethodBody sourceMethodBody, Predicate<IMethodReference> isContractMethod)
      : base(host) {
      this.sourceMethodBody = sourceMethodBody;
      this.isContractMethod = isContractMethod;
    }

    public override void RewriteChildren(AnonymousDelegate anonymousDelegate) {
      var savedAnonymousDelegate = this.currentAnonymousDelegate;
      this.currentAnonymousDelegate = anonymousDelegate;
      base.RewriteChildren(anonymousDelegate);
      this.currentAnonymousDelegate = savedAnonymousDelegate;
      return;
    }

    public override IExpression Rewrite(IMethodCall methodCall) {
      IGenericMethodInstanceReference/*?*/ methodToCall = methodCall.MethodToCall as IGenericMethodInstanceReference;
      if (methodToCall != null) {
        if (this.isContractMethod(methodToCall)) {
          //TODO: exists, forall
          if (methodToCall.GenericMethod.Name.Value == "Result") {
            ReturnValue returnValue = new ReturnValue() {
              Type = methodToCall.Type,
              Locations = new List<ILocation>(methodCall.Locations),
            };
            if (this.currentAnonymousDelegate != null)
              this.currentAnonymousDelegate.CallingConvention |= CallingConvention.HasThis;
            return returnValue;
          }
          if (methodToCall.GenericMethod.Name.Value == "OldValue") {
            OldValue oldValue = new OldValue() {
              Expression = this.Rewrite(IteratorHelper.First(methodCall.Arguments)),
              Type = methodToCall.Type,
              Locations = new List<ILocation>(methodCall.Locations),
            };
            return oldValue;
          }

          if (methodToCall.GenericMethod.Name.Value == "ValueAtReturn") {
            AddressDereference addressDereference = new AddressDereference() {
              Address = IteratorHelper.First(methodCall.Arguments),
              Locations = new List<ILocation>(methodCall.Locations),
              Type = methodToCall.Type,
            };
            return this.Rewrite(addressDereference);
          }

        }
      }
      return base.Rewrite(methodCall);
    }
  }
  internal class FindModelMembers : CodeTraverser {
    private bool foundModelMember = false;
    private FindModelMembers() { }
    public static bool ContainsModelMembers(IExpression expression) {
      var fmm = new FindModelMembers();
      fmm.Traverse(expression);
      return fmm.foundModelMember;
    }
    public override void TraverseChildren(IMethodCall methodCall) {
      if (ContractHelper.IsModel(methodCall.MethodToCall) != null)
        this.foundModelMember = true;
      base.TraverseChildren(methodCall);
    }
  }
  internal static class BrianGru {
    #region Code from BrianGru to negate predicates coming from if-then-throw preconditions
    // Recognize some common predicate forms, and negate them.  Also, fall back to a correct default.
    internal static String NegatePredicate(String predicate) {
      if (String.IsNullOrEmpty(predicate)) return "";
      // "(p)", but avoiding stuff like "(p && q) || (!p)"
      if (predicate[0] == '(' && predicate[predicate.Length - 1] == ')') {
        if (predicate.IndexOf('(', 1) == -1)
          return '(' + NegatePredicate(predicate.Substring(1, predicate.Length - 2)) + ')';
      }

      // "!p"
      if (predicate[0] == '!' && (ContainsNoOperators(predicate, 1, predicate.Length - 1) || IsSimpleFunctionCall(predicate, 1, predicate.Length - 1)))
        return predicate.Substring(1);

      // "a < b" or "a <= b"
      if (predicate.Contains("<")) {
        int aStart = 0, aEnd, bStart, bEnd = predicate.Length;
        int ltIndex = predicate.IndexOf('<');
        bool ltOrEquals = predicate[ltIndex + 1] == '=';
        aEnd = ltIndex;
        bStart = ltOrEquals ? ltIndex + 2 : ltIndex + 1;

        String a = predicate.Substring(aStart, aEnd - aStart);
        String b = predicate.Substring(bStart, bEnd - bStart);
        if (ContainsNoOperators(a) && ContainsNoOperators(b))
          return a + (ltOrEquals ? ">" : ">=") + b;
      }

      // "a > b" or "a >= b"
      if (predicate.Contains(">")) {
        int aStart = 0, aEnd, bStart, bEnd = predicate.Length;
        int gtIndex = predicate.IndexOf('>');
        bool gtOrEquals = predicate[gtIndex + 1] == '=';
        aEnd = gtIndex;
        bStart = gtOrEquals ? gtIndex + 2 : gtIndex + 1;

        String a = predicate.Substring(aStart, aEnd - aStart);
        String b = predicate.Substring(bStart, bEnd - bStart);
        if (ContainsNoOperators(a) && ContainsNoOperators(b))
          return a + (gtOrEquals ? "<" : "<=") + b;
      }

      // "a == b"  or  "a != b"
      if (predicate.Contains("=")) {
        int aStart = 0, aEnd = -1, bStart = -1, bEnd = predicate.Length;
        int eqIndex = predicate.IndexOf('=');
        bool skip = false;
        bool equalsOperator = false;
        if (predicate[eqIndex - 1] == '!') {
          aEnd = eqIndex - 1;
          bStart = eqIndex + 1;
          equalsOperator = false;
        } else if (predicate[eqIndex + 1] == '=') {
          aEnd = eqIndex;
          bStart = eqIndex + 2;
          equalsOperator = true;
        } else
          skip = true;

        if (!skip) {
          String a = predicate.Substring(aStart, aEnd - aStart);
          String b = predicate.Substring(bStart, bEnd - bStart);
          if (ContainsNoOperators(a) && ContainsNoOperators(b))
            return a + (equalsOperator ? "!=" : "==") + b;
        }
      }

      if (predicate.Contains("&&") || predicate.Contains("||")) {
        // Consider predicates like "(P) && (Q)", "P || Q", "(P || Q) && R", etc.
        // Apply DeMorgan's law, and recurse to negate both sides of the binary operator.
        int aStart = 0, aEnd, bStart, bEnd = predicate.Length;
        int parenCount = 0;
        bool skip = false;
        bool foundAnd = false, foundOr = false;
        aEnd = 0;
        while (aEnd < predicate.Length && ((predicate[aEnd] != '&' && predicate[aEnd] != '|') || parenCount > 0)) {
          if (predicate[aEnd] == '(')
            parenCount++;
          else if (predicate[aEnd] == ')')
            parenCount--;
          aEnd++;
        }
        if (aEnd >= predicate.Length - 1)
          skip = true;
        else {
          if (aEnd + 1 < predicate.Length && predicate[aEnd] == '&' && predicate[aEnd + 1] == '&')
            foundAnd = true;
          else if (aEnd + 1 < predicate.Length && predicate[aEnd] == '|' && predicate[aEnd + 1] == '|')
            foundOr = true;
          if (!foundAnd && !foundOr)
            skip = true;
        }

        if (!skip) {
          bStart = aEnd + 2;
          while (Char.IsWhiteSpace(predicate[aEnd - 1]))
            aEnd--;
          while (Char.IsWhiteSpace(predicate[bStart]))
            bStart++;

          String a = predicate.Substring(aStart, aEnd - aStart);
          String b = predicate.Substring(bStart, bEnd - bStart);
          String op = foundAnd ? " || " : " && ";
          return NegatePredicate(a) + op + NegatePredicate(b);
        }
      }

      return String.Format("!({0})", predicate);
    }
    private static bool ContainsNoOperators(String s) {
      return ContainsNoOperators(s, 0, s.Length);
    }
    // These aren't operators like + per se, but ones that will cause evaluation order to possibly change,
    // or alter the semantics of what might be in a predicate.
    // @TODO: Consider adding '~'
    static readonly String[] Operators = new String[] { "==", "!=", "=", "<", ">", "(", ")", "//", "/*", "*/" };
    private static bool ContainsNoOperators(String s, int start, int end) {
      foreach (String op in Operators)
        if (s.IndexOf(op) >= 0)
          return false;
      return true;
    }
    private static bool ArrayContains<T>(T[] array, T item) {
      foreach (T x in array)
        if (item.Equals(x))
          return true;
      return false;
    }
    // Recognize only SIMPLE method calls, like "System.String.Equals("", "")".
    private static bool IsSimpleFunctionCall(String s, int start, int end) {
      char[] badChars = { '+', '-', '*', '/', '~', '<', '=', '>', ';', '?', ':' };
      int parenCount = 0;
      int index = start;
      bool foundMethod = false;
      for (; index < end; index++) {
        if (s[index] == '(') {
          parenCount++;
          if (parenCount > 1)
            return false;
          if (foundMethod == true)
            return false;
          foundMethod = true;
        } else if (s[index] == ')') {
          parenCount--;
          if (index != end - 1)
            return false;
        } else if (ArrayContains(badChars, s[index]))
          return false;
      }
      return foundMethod;
    }
    #endregion Code from BrianGru to negate predicates coming from if-then-throw preconditions
  }


}
