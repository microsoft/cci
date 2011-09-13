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

namespace Microsoft.Cci.MutableContracts {

  /// <summary>
  /// Contract extraction assumes that method bodies are in a special form where
  /// the only place block statements appear are as the last statement in the
  /// list of statements within a block statement, i.e., that they form a linear
  /// linked list.
  /// </summary>
  public class ContractExtractor : MethodBodyCodeMutator {

    #region Public API
    /// <summary>
    /// Extracts calls to precondition and postcondition methods from the type
    /// System.Diagnostics.Contracts.Contract from the ISourceMethodBody and
    /// returns a pair of the extracted method contract and the residual method
    /// body. If the <paramref name="sourceMethodBody"/> is mutable, then it
    /// has the side-effect of updating the body so that it contains only the
    /// residual code in addition to returning it.
    /// Contract extraction assumes that method bodies are in a special form where
    /// the only place block statements appear are as the last statement in the
    /// list of statements within a block statement, i.e., that they form a linear
    /// linked list.
    /// </summary>
    public static MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(IContractAwareHost host, ISourceMethodBody sourceMethodBody, PdbReader/*?*/ pdbReader) {
      var e = new ContractExtractor(sourceMethodBody, host, pdbReader);
      #region Special case for iterators (until decompiler always handles them)
      var moveNext = IteratorContracts.FindClosureMoveNext(host, sourceMethodBody);
      if (moveNext != null) {
        var mc = IteratorContracts.GetMethodContractFromMoveNext(host, e, sourceMethodBody, moveNext, pdbReader);
        return new MethodContractAndMethodBody(mc, sourceMethodBody.Block);
      }
      #endregion
      return e.SplitMethodBodyIntoContractAndCode(sourceMethodBody.Block);
    }

    /// <summary>
    /// Extracts calls to Contract.Invariant contained in invariant methods within
    /// <paramref name="typeDefinition"/> and returns the resulting type contract.
    /// If <paramref name="typeDefinition"/> is mutable, then all invariant methods
    /// are removed as members of <paramref name="typeDefinition"/>.
    /// Contract extraction assumes that method bodies are in a special form where
    /// the only place block statements appear are as the last statement in the
    /// list of statements within a block statement, i.e., that they form a linear
    /// linked list.
    /// </summary>
    public static ITypeContract/*?*/ GetObjectInvariant(IContractAwareHost host, ITypeDefinition typeDefinition, PdbReader/*?*/ pdbReader, ILocalScopeProvider/*?*/ localScopeProvider) {
      TypeContract cumulativeContract = new TypeContract();
      NamedTypeDefinition mutableTypeDefinition = typeDefinition as NamedTypeDefinition;
      var invariantMethods = new List<IMethodDefinition>(ContractHelper.GetInvariantMethods(typeDefinition));
      foreach (var invariantMethod in invariantMethods) {
        IMethodBody methodBody = invariantMethod.Body;
        ISourceMethodBody/*?*/ sourceMethodBody = methodBody as ISourceMethodBody;
        if (sourceMethodBody == null) {
          sourceMethodBody = new Microsoft.Cci.ILToCodeModel.SourceMethodBody(methodBody, host, pdbReader, localScopeProvider);
        }
        var e = new Microsoft.Cci.MutableContracts.ContractExtractor(sourceMethodBody, host, pdbReader);
        BlockStatement b = sourceMethodBody.Block as BlockStatement;
        if (b != null) {
          var tc = e.ExtractObjectInvariants(b);
          if (tc != null) {
            cumulativeContract.Invariants.AddRange(tc.Invariants);
          }
        }
      }

      return (cumulativeContract.Invariants.Count == 0)
        ? null
        : cumulativeContract;
    }

    #endregion

    private MethodContract/*?*/ currentMethodContract;
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

    private ISourceMethodBody sourceMethodBody;
    private PdbReader/*?*/ pdbReader;
    private IContractAwareHost contractAwarehost;
    private OldAndResultExtractor oldAndResultExtractor;
    private bool extractingFromACtorInAClass;
    /// <summary>
    /// When not null, this is the abstract type for which the contract class
    /// is holding the contracts for.
    /// </summary>
    private ITypeReference/*?*/ extractingFromAMethodInAContractClass;

    private ITypeReference contractClass;
    private ITypeReference contractClassDefinedInReferenceAssembly;

    private bool methodIsInReferenceAssembly;

    private static ITypeReference/*?*/ TemporaryKludge(IEnumerable<ICustomAttribute> attributes, string attributeTypeName) {
      foreach (ICustomAttribute attribute in attributes) {
        if (TypeHelper.GetTypeName(attribute.Type) == attributeTypeName) {

          List<IMetadataExpression> args = new List<IMetadataExpression>(attribute.Arguments);
          if (args.Count < 1) continue;
          IMetadataTypeOf abstractTypeMD = args[0] as IMetadataTypeOf;
          if (abstractTypeMD == null) continue;
          ITypeReference referencedTypeReference = ContractHelper.Unspecialized(abstractTypeMD.TypeToGet);
          ITypeDefinition referencedTypeDefinition = referencedTypeReference.ResolvedType;
          return referencedTypeDefinition;

        }
      }
      return null;
    }

    internal bool IsContractMethod(IMethodReference/*?*/ method) {
      if (method == null) return false;
      if (method.ContainingType.InternedKey == this.contractClass.InternedKey) return true;
      // Reference assemblies define their own internal versions of the contract methods
      return this.contractClassDefinedInReferenceAssembly != null &&
        (method.ContainingType.InternedKey == this.contractClassDefinedInReferenceAssembly.InternedKey);
    }

    private ContractExtractor(
      ISourceMethodBody sourceMethodBody,
      IContractAwareHost host,
      PdbReader/*?*/ pdbReader)
      : base(host, true, pdbReader) {
      this.sourceMethodBody = sourceMethodBody;
      this.contractAwarehost = host;
      this.pdbReader = pdbReader;

      // TODO: these fields make sense only if extracting a method contract and not a type contract.

      var definingUnit = TypeHelper.GetDefiningUnit(sourceMethodBody.MethodDefinition.ContainingType.ResolvedType);

      this.methodIsInReferenceAssembly = definingUnit == null ? false : ContractHelper.IsContractReferenceAssembly(this.host, definingUnit);

      var contractAssemblyReference = new Immutable.AssemblyReference(host, this.host.ContractAssemblySymbolicIdentity);
      this.contractClass = ContractHelper.CreateTypeReference(this.host, contractAssemblyReference, "System.Diagnostics.Contracts.Contract");

      IUnitReference/*?*/ ur = TypeHelper.GetDefiningUnitReference(sourceMethodBody.MethodDefinition.ContainingType);
      IAssemblyReference ar = ur as IAssemblyReference;
      if (ar != null) {
        // Check for the attribute which is defined in the assembly that defines the contract class.
        var refAssemblyAttribute = ContractHelper.CreateTypeReference(this.host, contractAssemblyReference, "System.Diagnostics.Contracts.ContractReferenceAssemblyAttribute");
        if (AttributeHelper.Contains(ar.Attributes, refAssemblyAttribute)) {
          // then we're extracting contracts from a reference assembly
          var contractTypeAsDefinedInReferenceAssembly = ContractHelper.CreateTypeReference(this.host, ar, "System.Diagnostics.Contracts.Contract");
          this.contractClassDefinedInReferenceAssembly = contractTypeAsDefinedInReferenceAssembly;
        } else {
          // If that fails, check for the attribute which is defined in the assembly itself
          refAssemblyAttribute = ContractHelper.CreateTypeReference(this.host, ar, "System.Diagnostics.Contracts.ContractReferenceAssemblyAttribute");
          if (AttributeHelper.Contains(ar.Attributes, refAssemblyAttribute)) {
            // then we're extracting contracts from a reference assembly
            var contractTypeAsDefinedInReferenceAssembly = ContractHelper.CreateTypeReference(this.host, ar, "System.Diagnostics.Contracts.Contract");
            this.contractClassDefinedInReferenceAssembly = contractTypeAsDefinedInReferenceAssembly;
          }
        }

      }

      #region Set contract purity based on whether the method definition has the pure attribute
      var pureAttribute = ContractHelper.CreateTypeReference(this.host, contractAssemblyReference, "System.Diagnostics.Contracts.PureAttribute");
      if (AttributeHelper.Contains(sourceMethodBody.MethodDefinition.Attributes, pureAttribute)) {
        this.CurrentMethodContract.IsPure = true;
      }
      #endregion Set contract purity based on whether the method definition has the pure attribute

      this.oldAndResultExtractor = new OldAndResultExtractor(this.host, sourceMethodBody, this.referenceCache, this.contractClass);

      this.extractingFromACtorInAClass =
        sourceMethodBody.MethodDefinition.IsConstructor
        &&
        !sourceMethodBody.MethodDefinition.ContainingType.IsValueType;

      // TODO: this should be true only if it is a contract class for an interface
      this.extractingFromAMethodInAContractClass =
        TemporaryKludge(sourceMethodBody.MethodDefinition.ContainingType.Attributes, "System.Diagnostics.Contracts.ContractClassForAttribute");

    }

    private MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(IBlockStatement blockStatement) {
      // Don't start with an empty contract because the ctor may have set some things in it
      var bs = this.Visit(blockStatement);
      bs = new AssertAssumeExtractor(this, this.sourceMethodBody, this.host, this.pdbReader).Visit(bs);
      if (this.currentMethodContract != null)
        this.currentMethodContract = ReplacePrivateFieldsThatHavePublicProperties(this.host, this.sourceMethodBody.MethodDefinition.ContainingTypeDefinition, this.currentMethodContract);
      return new MethodContractAndMethodBody(this.currentMethodContract, bs);
    }

    private ITypeContract/*?*/ ExtractObjectInvariants(BlockStatement blockStatement) {
      var stmts = new List<IStatement>(blockStatement.Statements);

      if (stmts.Count == 0) return null;

      var linearBlockIndex = LinearizeBlocks(blockStatement);

      int lastBlockIndex;
      int lastStatementIndex;
      if (!LastIndexOf(s => IsInvariant(s), linearBlockIndex, out lastBlockIndex, out lastStatementIndex)) return null;

      var currentBlockIndex = 0;
      var currentStatementIndex = 0;

      int blockIndexOfNextInvariantCall;
      int stmtIndexOfNextInvariantCall;
      var found = IndexOf(s => IsInvariant(s), linearBlockIndex, 0, 0, out blockIndexOfNextInvariantCall, out stmtIndexOfNextInvariantCall);

      List<ITypeInvariant> invariants = new List<ITypeInvariant>();

      while (found && (blockIndexOfNextInvariantCall <= lastBlockIndex && stmtIndexOfNextInvariantCall <= lastStatementIndex)) {
        var currentClump = ExtractClump(linearBlockIndex, currentBlockIndex, currentStatementIndex, blockIndexOfNextInvariantCall, stmtIndexOfNextInvariantCall);

        // Add current clump to current contract
        TypeInvariant invariant = ExtractObjectInvariant(currentClump);
        invariants.Add(invariant);

        // Move pair to point to next statement
        currentBlockIndex = blockIndexOfNextInvariantCall;
        currentStatementIndex = stmtIndexOfNextInvariantCall;
        currentStatementIndex++;
        if (currentStatementIndex >= linearBlockIndex[currentBlockIndex].Statements.Count) {
          currentBlockIndex++;
          currentStatementIndex = 0;
        }
        // Find next contract (if any)
        found = IndexOf(s => IsInvariant(s), linearBlockIndex, currentBlockIndex, currentStatementIndex, out blockIndexOfNextInvariantCall, out stmtIndexOfNextInvariantCall);
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
        if (this.pdbReader != null)
          TryGetConditionText(this.pdbReader, locations, numArgs, out origSource);
      }

      TypeInvariant invariant = new TypeInvariant() {
        Condition = this.Visit(contractExpression),
        Description = numArgs >= 2 ? arguments[1] : null,
        OriginalSource = origSource,
        Locations = locations,
      };
      return invariant;
    }

    public override IBlockStatement Visit(BlockStatement blockStatement) {
      if (blockStatement == null) return blockStatement;
      var stmts = blockStatement.Statements;
      if (stmts.Count == 0) return blockStatement;

      var linearBlockIndex = LinearizeBlocks(blockStatement);

      int lastBlockIndex;
      int lastStatementIndex;
      if (!LastIndexOf(s => IsPrePostEndOrLegacy(s, false), linearBlockIndex, out lastBlockIndex, out lastStatementIndex)) return blockStatement;

      List<IStatement> newStmts = new List<IStatement>();

      int blockIndexOfNextContractCall;
      int stmtIndexOfNextContractCall;

      var found = IndexOf(s => IsPrePostEndOrLegacy(s, true), linearBlockIndex, 0, 0, out blockIndexOfNextContractCall, out stmtIndexOfNextContractCall);
      if (!found || !BlockStatementIndicesLessThanOrEqual(blockIndexOfNextContractCall, stmtIndexOfNextContractCall, lastBlockIndex, lastStatementIndex)) return blockStatement;

      var localDeclarationStatements = new List<IStatement>();
      var firstStatementIndex = 0;
      while (firstStatementIndex < blockStatement.Statements.Count) {
        var s = blockStatement.Statements[firstStatementIndex];
        ILocalDeclarationStatement lds = s as ILocalDeclarationStatement;
        var empty = s as IEmptyStatement;
        if (empty == null && (lds == null || lds.InitialValue != null)) break;
        firstStatementIndex++;
        localDeclarationStatements.Add(s);
      }

      var currentBlockIndex = 0;
      var currentStatementIndex = firstStatementIndex;

      if (this.extractingFromACtorInAClass) {
        // Find the expression statement that is a call to a ctor with its
        // ThisArgument being either "this" or "base"
        int blockIndex; int stmtIndex;
        var foundCtorCall = IndexOf(s => {
          var mc = IsMethodCall(s);
          return mc != null && mc.MethodToCall.ResolvedMethod.IsConstructor && mc.ThisArgument is IThisReference;
        }, linearBlockIndex, 0, // start at first block
          firstStatementIndex, // start at first statement after any local declaration statements
          out blockIndex, out stmtIndex);
        // TODO: Signal error if not foundCtorCall?
        System.Diagnostics.Debug.Assert(foundCtorCall);
        // Need to also keep any nops with source contexts in the method body and not in the contract
        //int eb;
        //int es;
        //var foundNops = IndexOf(s => s is IEmptyStatement && IteratorHelper.EnumerableIsNotEmpty(s.Locations),
        //  linearBlockIndex, blockIndex, stmtIndex, out eb, out es);
        //if (foundNops) {
        //  blockIndex = eb;
        //  stmtIndex = es;
        //}
        newStmts.AddRange(ExtractClump(linearBlockIndex, 0, firstStatementIndex, blockIndex, stmtIndex));
        currentStatementIndex = stmtIndex + 1;
        var lastIndexInBlock = linearBlockIndex[blockIndex].Statements.Count-1;
        if (currentStatementIndex <= lastIndexInBlock) {
          // if there is a closure in the ctor that captures a field, then just after the base ctor call
          // there will be a statement "local := this"
          var possibleAssignmentOfThis = linearBlockIndex[blockIndex].Statements[currentStatementIndex] as ExpressionStatement;
          if (possibleAssignmentOfThis != null) {
            IAssignment assign = possibleAssignmentOfThis.Expression as IAssignment;
            if (assign != null && assign.Source is IThisReference) {
              newStmts.Add(possibleAssignmentOfThis);
              currentStatementIndex++;
            }
          }
        }
        if (currentStatementIndex >= lastIndexInBlock && (linearBlockIndex[blockIndex].Statements[lastIndexInBlock] is IBlockStatement)) {
          currentBlockIndex++;
          currentStatementIndex = 0;
        } else {
          currentBlockIndex = blockIndex;
        }
      }

      var remaining = ExtractContractsAndReturnRemainingStatements(linearBlockIndex, currentBlockIndex, currentStatementIndex, lastBlockIndex, lastStatementIndex);
      if (0 < localDeclarationStatements.Count)
        remaining.InsertRange(0, localDeclarationStatements);
      newStmts.AddRange(remaining);
      blockStatement.Statements = newStmts;
      return blockStatement;
    }

    private List<IStatement> ExtractContractsAndReturnRemainingStatements(
      List<BlockStatement> linearBlockIndex,
      int startBlock,
      int startStatement,
      int lastBlockIndex,
      int lastStatementIndex
      ) {

      int currentBlockIndex = startBlock;
      int currentStatementIndex = startStatement;
      int blockIndexOfNextContractCall;
      int stmtIndexOfNextContractCall;

      var found = IndexOf(s => IsPrePostEndOrLegacy(s, true), linearBlockIndex, startBlock, startStatement, out blockIndexOfNextContractCall, out stmtIndexOfNextContractCall);

      while (found && BlockStatementIndicesLessThanOrEqual(blockIndexOfNextContractCall, stmtIndexOfNextContractCall, lastBlockIndex, lastStatementIndex)) {
        var currentClump = ExtractClump(linearBlockIndex, currentBlockIndex, currentStatementIndex, blockIndexOfNextContractCall, stmtIndexOfNextContractCall);
        currentClump = LocalBinder.CloseClump(this.host, currentClump);
        // Add current clump to current contract
        ExtractContract(currentClump);
        // Move pair to point to next statement
        currentBlockIndex = blockIndexOfNextContractCall;
        currentStatementIndex = stmtIndexOfNextContractCall;
        currentStatementIndex++;
        if (currentStatementIndex >= linearBlockIndex[currentBlockIndex].Statements.Count) {
          currentBlockIndex++;
          currentStatementIndex = 0;
        }
        // Find next contract (if any)
        found = IndexOf(s => IsPrePostEndOrLegacy(s, true), linearBlockIndex, currentBlockIndex, currentStatementIndex, out blockIndexOfNextContractCall, out stmtIndexOfNextContractCall);
      }

      var lastBlock = linearBlockIndex[linearBlockIndex.Count - 1];
      var currentClump2 = ExtractClump(linearBlockIndex, currentBlockIndex, currentStatementIndex, linearBlockIndex.Count - 1, lastBlock.Statements.Count - 1);
      currentClump2 = LocalBinder.CloseClump(this.host, currentClump2);
      return currentClump2;
    }

    internal static MethodContractAndMethodBody MoveNextExtractor(
      IContractAwareHost host,
      PdbReader pdbReader,
      ISourceMethodBody moveNextBody,
      BlockStatement blockStatement,
      int startBlock,
      int startStatement,
      int lastBlockIndex,
      int lastStatementIndex
      ) {
      var e = new ContractExtractor(moveNextBody, host, pdbReader);
      var linearBlockIndex = e.LinearizeBlocks(blockStatement);
      var result = e.ExtractContractsAndReturnRemainingStatements(linearBlockIndex, startBlock, startStatement, lastBlockIndex, lastStatementIndex);
      return new MethodContractAndMethodBody(e.currentMethodContract, new BlockStatement() { Statements = result, });
    }

    private static bool BlockStatementIndicesLessThanOrEqual(int b1, int s1, int b2, int s2) {
      return ((b1 < b2) || (b1 == b2 && s1 <= s2));
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
              if (this.pdbReader != null)
                TryGetConditionText(this.pdbReader, locations, numArgs, out origSource);
            }
            IExpression/*?*/ description = numArgs >= 2 ? arguments[1] : null;

            IGenericMethodInstanceReference/*?*/ genericMethodToCall = methodToCall as IGenericMethodInstanceReference;

            if (mname == "Ensures") {
              contractExpression = this.oldAndResultExtractor.Visit(contractExpression);
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
              contractExpression = this.oldAndResultExtractor.Visit(contractExpression);
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
            var gmir = methodToCall as IGenericMethodInstanceReference;
            IMethodDefinition abbreviatorDef;
            if (gmir != null)
              abbreviatorDef = gmir.GenericMethod.ResolvedMethod;
            else
              abbreviatorDef = methodToCall.ResolvedMethod;
            var mc = ContractHelper.GetMethodContractFor(this.contractAwarehost, abbreviatorDef);
            if (mc != null) {
              var mutableMC = ContractHelper.InlineAndStuff(this.host, mc, this.sourceMethodBody.MethodDefinition, abbreviatorDef, new List<IExpression>(methodCall.Arguments));
              mutableMC.Locations.InsertRange(0, methodCall.Locations);
              if (this.currentMethodContract == null)
                this.currentMethodContract = new MethodContract(mutableMC);
              else
                ContractHelper.AddMethodContract(this.currentMethodContract, mutableMC);
            }
          }
        }
      }
    }

    /// <summary>
    /// Creates an index to the blocks within the <paramref name="blockStatement"/>.
    /// Assumes that nested blocks are found *only* as the last statement in the
    /// list of statements in a BlockStatement.
    /// </summary>
    /// <returns>A list of the blocks. Each block is unchanged.</returns>
    internal List<BlockStatement> LinearizeBlocks(BlockStatement blockStatement) {
      var result = new List<BlockStatement>();
      if (blockStatement.Statements.Count == 0) return result;
      BlockStatement bs = blockStatement;
      do {
        result.Add(bs);
        bs = bs.Statements[bs.Statements.Count - 1] as BlockStatement;
      } while (bs != null);
      return result;
    }

    private List<IStatement> ExtractClump(
      List<BlockStatement> blockList,
      int startBlockIndex,
      int startStmtIndex,
      int endBlockIndex,
      int endStmtIndex) {

      //Contract.Requires(startBlockIndex <= endBlockIndex);

      List<IStatement> clump = new List<IStatement>();
      var currentBlockIndex = startBlockIndex;
      var currentStartIndex = startStmtIndex;
      var stmts = blockList[currentBlockIndex].Statements;
      var currentEndIndex = startBlockIndex == endBlockIndex ? endStmtIndex : stmts.Count - 2;
      do {
        // Take all of the statements in the current block
        // (starting with the start statement) except for
        // the last statement which *must* be (a pointer to) the
        // block which is next in the blockList.
        for (int i = currentStartIndex; i <= currentEndIndex; i++) {
          clump.Add(stmts[i]);
        }
        if (currentBlockIndex == endBlockIndex) {
          break;
        }

        BlockStatement nextBlock = stmts[stmts.Count - 1] as BlockStatement;

        // Clump should span multiple blocks iff parameters said to
        System.Diagnostics.Debug.Assert((nextBlock != null) == (startBlockIndex < endBlockIndex));

        currentBlockIndex++;
        currentStartIndex = 0;
        stmts = blockList[currentBlockIndex].Statements;
        currentEndIndex = currentBlockIndex == endBlockIndex ? endStmtIndex : stmts.Count - 2;

      } while (currentBlockIndex <= endBlockIndex);
      return clump;
    }

    internal bool IndexOf(Predicate<IStatement> test, List<BlockStatement> blockList, int startBlock, int startStmt, out int endBlock, out int endStmt) {

      endBlock = -1;
      endStmt = -1;
      for (int i = startBlock; i < blockList.Count; i++) {
        BlockStatement currentBlock = blockList[i];
        for (int j = i == startBlock ? startStmt : 0; j < currentBlock.Statements.Count; j++) {
          if (test(currentBlock.Statements[j])) {
            endBlock = i;
            endStmt = j;
            return true;
          }

        }
      }
      return false;
    }
    private bool LastIndexOf(Predicate<IStatement> predicate, List<BlockStatement> blocks, out int blockIndex, out int stmtIndex) {
      for (int bIndex = blocks.Count - 1; 0 <= bIndex; bIndex--) {
        List<IStatement> statements = blocks[bIndex].Statements;
        // search from the end, stop at first statement which satisfies predicate
        for (int i = statements.Count - 1; 0 <= i; i--) {
          if (predicate(statements[i])) {
            blockIndex = bIndex; stmtIndex = i;
            return true;
          }
        }
      }
      blockIndex = -1; stmtIndex = -1;
      return false;
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
    private int OffsetOfLastContractCallOrNegativeOne() {
      List<IOperation> instrs = new List<IOperation>(this.sourceMethodBody.Operations);
      for (int i = instrs.Count - 1; 0 <= i; i--) {
        IOperation op = instrs[i];
        if (op.OperationCode != OperationCode.Call) continue;
        IMethodReference method = op.Value as IMethodReference;
        if (method == null) continue;
        var methodName = method.Name.Value;
        if (
          (this.IsContractMethod(method) && !(methodName.Equals("Assert") || methodName.Equals("Assume")))
          || ContractHelper.IsValidatorOrAbbreviator(method)
          ) return (int)op.Offset;
      }
      return -1; // not found
    }


    private static bool IsLocalDeclarationWithoutInitializer(IStatement statement) {
      ILocalDeclarationStatement localDeclarationStatement = statement as ILocalDeclarationStatement;
      if (localDeclarationStatement == null) return false;
      return localDeclarationStatement.InitialValue == null;
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
    internal static bool IsValidatorOrAbbreviator(IStatement statement) {
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement == null) return false;
      IMethodCall methodCall = expressionStatement.Expression as IMethodCall;
      if (methodCall == null) return false;
      IMethodReference methodToCall = methodCall.MethodToCall;
      return ContractHelper.IsValidatorOrAbbreviator(methodToCall);
    }
    internal static bool IsLegacyRequires(IStatement statement) {
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

    private static bool IsDefinitionOfLocalWithInitializer(IStatement statement) {
      ILocalDeclarationStatement stmt = statement as ILocalDeclarationStatement;
      return stmt != null && stmt.InitialValue != null;
    }

    private static List<T> MkList<T>(T t) { var xs = new List<T>(); xs.Add(t); return xs; }

    internal static bool TryGetConditionText(PdbReader pdbReader, IEnumerable<ILocation> locations, int numArgs, out string sourceText) {
      int startColumn;
      if (!TryGetSourceText(pdbReader, locations, out sourceText, out startColumn)) return false;
      int firstSourceTextIndex = sourceText.IndexOf('(');
      firstSourceTextIndex = firstSourceTextIndex == -1 ? 0 : firstSourceTextIndex + 1; // the +1 is to skip the opening paren
      int lastSourceTextIndex;
      if (numArgs == 1) {
        lastSourceTextIndex = sourceText.LastIndexOf(')'); // supposedly the character after the first (and only) argument
      } else {
        lastSourceTextIndex = IndexOfWhileSkippingBalancedThings(sourceText, firstSourceTextIndex, ','); // supposedly the character after the first argument
      }
      if (lastSourceTextIndex <= firstSourceTextIndex) {
        //Console.WriteLine(sourceText);
        lastSourceTextIndex = sourceText.Length; // if something went wrong, at least get the whole source text.
      }
      sourceText = sourceText.Substring(firstSourceTextIndex, lastSourceTextIndex - firstSourceTextIndex);
      var indentSize = firstSourceTextIndex + startColumn;
      sourceText = AdjustIndentationOfMultilineSourceText(sourceText, indentSize);
      return true;
    }
    internal static bool TryGetSourceText(PdbReader pdbReader, IEnumerable<ILocation> locations, out string/*?*/ sourceText, out int startColumn) {
      sourceText = null;
      startColumn = 0; // columns begin at 1, so this can work as a null value
      foreach (var loc in locations) {
        foreach (IPrimarySourceLocation psloc in pdbReader.GetClosestPrimarySourceLocationsFor(loc)) {
          if (!String.IsNullOrEmpty(psloc.Source)) {
            sourceText = psloc.Source;
            startColumn = psloc.StartColumn;
            break;
          }
        }
        if (sourceText != null) break;
      }
      return sourceText != null;
    }
    internal static int IndexOfWhileSkippingBalancedThings(string source, int startIndex, char targetChar) {
      int i = startIndex;
      while (i < source.Length) {
        if (source[i] == targetChar) break;
        else if (source[i] == '(') i = IndexOfWhileSkippingBalancedThings(source, i + 1, ')') + 1;
        else if (source[i] == '"') i = IndexOfWhileSkippingBalancedThings(source, i + 1, '"') + 1;
        else i++;
      }
      return i;
    }
    internal static char[] WhiteSpace = { ' ', '\t' };
    internal static string AdjustIndentationOfMultilineSourceText(string sourceText, int trimLength) {
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
    internal void ExtractLegacyRequires(List<IStatement> currentClump) {
      var lastIndex = currentClump.Count - 1;
      ConditionalStatement conditional = currentClump[lastIndex] as ConditionalStatement;
      //^ requires IsLegacyRequires(conditional);
      EmptyStatement empty = conditional.FalseBranch as EmptyStatement;
      IBlockStatement blockStatement = conditional.TrueBranch as IBlockStatement;
      List<IStatement> statements = new List<IStatement>(blockStatement.Statements);
      IStatement statement = statements[statements.Count - 1];
      IExpression failureBehavior;
      IThrowStatement throwStatement = statement as IThrowStatement;
      var locations = new List<ILocation>(conditional.Condition.Locations);
      if (locations.Count == 0) {
        locations.AddRange(conditional.Locations);
      }

      string origSource = null;
      if (0 < locations.Count) {
        if (this.pdbReader != null)
          TryGetConditionText(this.pdbReader, locations, 1, out origSource);
        if (origSource != null) {
          origSource = BrianGru.NegatePredicate(origSource);
        }
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
      IContractAwareHost caw = this.host as IContractAwareHost;
      if (caw != null)
        validatorContract = ContractHelper.GetMethodContractFor(caw, methodDefinition);
      if (validatorContract != null) {
        var bpta = new ReplaceParametersWithArguments(this.host, methodDefinition, methodCall);
        validatorContract = bpta.Visit(validatorContract);
        Microsoft.Cci.MutableContracts.ContractHelper.AddMethodContract(this.CurrentMethodContract, validatorContract);
      }
    }


    /// <summary>
    /// A mutator that replaces the parameters of a method with the arguments from a method call.
    /// </summary>
    private sealed class ReplaceParametersWithArguments : MethodBodyCodeAndContractMutator {
      private IMethodDefinition methodDefinition;
      private IMethodCall methodCall;
      private List<IExpression> arguments;
      /// <summary>
      /// Creates a mutator that replaces all occurrences of parameters from the target method with those from the source method.
      /// </summary>
      public ReplaceParametersWithArguments(IMetadataHost host, IMethodDefinition methodDefinition, IMethodCall methodCall)
        : base(host, false) { // NB: Important to pass "false": this mutator needs to make a copy of the entire expression!
        this.methodDefinition = methodDefinition;
        this.methodCall = methodCall;
        this.arguments = new List<IExpression>(methodCall.Arguments);
      }

      /// <summary>
      /// Visits the specified bound expression.
      /// </summary>
      /// <param name="boundExpression">The bound expression.</param>
      /// <returns></returns>
      public override IExpression Visit(BoundExpression boundExpression) {
        ParameterDefinition/*?*/ par = boundExpression.Definition as ParameterDefinition;
        if (par != null && par.ContainingSignature == this.methodDefinition) {
          return this.arguments[par.Index];
        } else {
          return base.Visit(boundExpression);
        }
      }
    }

    private sealed class AssertAssumeExtractor : MethodBodyCodeMutator {

      ContractExtractor parent;
      ISourceMethodBody sourceMethodBody;
      PdbReader/*?*/ pdbReader;

      internal AssertAssumeExtractor(
        ContractExtractor parent,
        ISourceMethodBody sourceMethodBody,
        IMetadataHost host,
        PdbReader/*?*/ pdbReader
        )
        : base(host, true, pdbReader) {
        this.parent = parent;
        this.sourceMethodBody = sourceMethodBody;
        this.pdbReader = pdbReader;
      }

      public override IStatement Visit(ExpressionStatement expressionStatement) {
        IMethodCall/*?*/ methodCall = expressionStatement.Expression as IMethodCall;
        if (methodCall == null) goto JustVisit;
        IMethodReference methodToCall = methodCall.MethodToCall;
        if (!parent.IsContractMethod(methodToCall)) goto JustVisit;
        string mname = methodToCall.Name.Value;
        List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
        List<ILocation> locations = new List<ILocation>(methodCall.Locations);
        int numArgs = arguments.Count;
        if (numArgs != 1 && numArgs != 2) goto JustVisit;
        if (mname != "Assert" && mname != "Assume") goto JustVisit;

        string/*?*/ origSource = null;
        if (this.pdbReader != null)
          ContractExtractor.TryGetConditionText(this.pdbReader, locations, numArgs, out origSource);


        var locations2 = this.pdbReader == null ? new List<ILocation>() :
          new List<ILocation>(IteratorHelper.GetConversionEnumerable<IPrimarySourceLocation, ILocation>(this.pdbReader.GetClosestPrimarySourceLocationsFor(locations)));
        if (mname == "Assert") {
          AssertStatement assertStatement = new AssertStatement() {
            Condition = this.Visit(arguments[0]),
            Description = numArgs >= 2? arguments[1] : null,
            OriginalSource = origSource,
            Locations = locations2,
          };
          return assertStatement;
        }
        if (mname == "Assume") {
          AssumeStatement assumeStatement = new AssumeStatement() {
            Condition = this.Visit(arguments[0]),
            Description = numArgs >= 2 ? arguments[1] : null,
            OriginalSource = origSource,
            Locations = locations2,
          };
          return assumeStatement;
        }
      JustVisit:
        return base.Visit(expressionStatement);
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
        methodContract = (MethodContract)s.Visit(methodContract);
      }
      return methodContract;
    }

    private class SubstitutePropertyGetterForField : CodeAndContractMutatingVisitor {
      ITypeDefinition sourceType;
      Dictionary<IName, IMethodReference> substitution;
      public SubstitutePropertyGetterForField(IMetadataHost host, ITypeDefinition sourceType, Dictionary<IName, IMethodReference> substitutionTable)
        : base(host) {
        this.sourceType = sourceType;
        this.substitution = substitutionTable;
      }
      public override IExpression Visit(BoundExpression boundExpression) {
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
          return base.Visit(boundExpression);
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

    private class LocalBinder : MethodBodyCodeMutator {
      Dictionary<ILocalDefinition, ILocalDefinition> tableForLocalDefinition = new Dictionary<ILocalDefinition, ILocalDefinition>();
      List<IStatement> localDeclarations = new List<IStatement>();
      private int counter = 0;
      private LocalBinder(IMetadataHost host) : base(host, true) { }

      public static List<IStatement> CloseClump(IMetadataHost host, List<IStatement> clump) {
        var cc = new LocalBinder(host);
        cc.Visit(clump);
        cc.localDeclarations.AddRange(clump);
        return cc.localDeclarations;
      }

      private ILocalDefinition PossiblyReplaceLocal(ILocalDefinition localDefinition) {
        ILocalDefinition localToUse;
        if (!this.tableForLocalDefinition.TryGetValue(localDefinition, out localToUse)) {
          localToUse = new LocalDefinition() {
            MethodDefinition = localDefinition.MethodDefinition,
            Name = this.host.NameTable.GetNameFor("loc" + counter),
            Type = localDefinition.Type,
          };
          this.counter++;
          this.tableForLocalDefinition.Add(localDefinition, localToUse);
          this.localDeclarations.Add(
            new LocalDeclarationStatement() {
              InitialValue = null,
              LocalVariable = localToUse,
            });
        }
        return localToUse == null ? localDefinition : localToUse;
      }

      public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
        // then we don't need to replace this local, so put a dummy value in the table
        // so we don't muck with it.
        this.tableForLocalDefinition.Add(localDeclarationStatement.LocalVariable, null);
        if (localDeclarationStatement.InitialValue != null) {
          localDeclarationStatement.InitialValue = this.Visit(localDeclarationStatement.InitialValue);
        }
        return localDeclarationStatement;
      }

      /// <summary>
      /// Need to have this override because the base class doesn't visit down into local definitions
      /// (and besides, this is where this visitor is doing its work anyway).
      /// </summary>
      public override ILocalDefinition Visit(ILocalDefinition localDefinition) {
        return PossiblyReplaceLocal(localDefinition);
      }

      public override ILocalDefinition VisitReferenceTo(ILocalDefinition localDefinition) {
        return PossiblyReplaceLocal(localDefinition);
      }

    }

  }

  internal class OldAndResultExtractor : MethodBodyCodeMutator {
    ISourceMethodBody sourceMethodBody;
    ITypeReference contractClass;
    AnonymousDelegate/*?*/ currentAnonymousDelegate;
    internal OldAndResultExtractor(IMetadataHost host, ISourceMethodBody sourceMethodBody, Dictionary<IReference, IReference>/*?*/ referenceCache, ITypeReference contractClass)
      : base(host, true) {
      this.sourceMethodBody = sourceMethodBody;
      if (referenceCache != null)
        this.referenceCache = referenceCache;
      this.contractClass = contractClass;
    }

    public override IExpression Visit(AnonymousDelegate anonymousDelegate) {
      var savedAnonymousDelegate = this.currentAnonymousDelegate;
      this.currentAnonymousDelegate = anonymousDelegate;
      var result = base.Visit(anonymousDelegate);
      this.currentAnonymousDelegate = savedAnonymousDelegate;
      return result;
    }

    public override IExpression Visit(MethodCall methodCall) {
      IGenericMethodInstanceReference/*?*/ methodToCall = methodCall.MethodToCall as IGenericMethodInstanceReference;
      if (methodToCall != null) {
        if (methodToCall.GenericMethod.ContainingType.InternedKey == this.contractClass.InternedKey) {
          //TODO: exists, forall
          if (methodToCall.GenericMethod.Name.Value == "Result") {
            ReturnValue returnValue = new ReturnValue() {
              Type = methodToCall.Type,
              Locations = methodCall.Locations,
            };
            if (this.currentAnonymousDelegate != null)
              this.currentAnonymousDelegate.CallingConvention |= CallingConvention.HasThis;
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
  internal class FindModelMembers : BaseCodeTraverser {
    private bool foundModelMember = false;
    private FindModelMembers() { }
    public static bool ContainsModelMembers(IExpression expression) {
      var fmm = new FindModelMembers();
      fmm.Visit(expression);
      return fmm.foundModelMember;
    }
    public override void Visit(IMethodCall methodCall) {
      if (ContractHelper.IsModel(methodCall.MethodToCall) != null)
        this.foundModelMember = true;
      base.Visit(methodCall);
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
