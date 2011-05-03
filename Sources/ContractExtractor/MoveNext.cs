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
  /// This entire class (file) should go away when iterators are always decompiled. But if they aren't
  /// then this class finds the MoveNext method and gets any contracts that the original iterator method
  /// had, but which the compiler put into the first state of the MoveNext state machine.
  /// </summary>
  public class IteratorContracts {

    private static ICreateObjectInstance/*?*/ GetICreateObjectInstance(IStatement statement) {
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement != null) {
        IAssignment assignment = expressionStatement.Expression as IAssignment;
        if (assignment == null) return null;
        ICreateObjectInstance createObjectInstance = assignment.Source as ICreateObjectInstance;
        return createObjectInstance;
      }
      ILocalDeclarationStatement localDeclaration = statement as ILocalDeclarationStatement;
      if (localDeclaration != null) {
        ICreateObjectInstance createObjectInstance = localDeclaration.InitialValue as ICreateObjectInstance;
        return createObjectInstance;
      }
      return null;
    }
    /// <summary>
    /// For an iterator method, find the closure class' MoveNext method and return its body.
    /// </summary>
    /// <param name="possibleIterator">The (potential) iterator method.</param>
    /// <returns>Dummy.MethodBody if <paramref name="possibleIterator"/> does not fit into the code pattern of an iterator method, 
    /// or the body of the MoveNext method of the corresponding closure class if it does.
    /// </returns>
    public static ISourceMethodBody/*?*/ FindClosureMoveNext(IMetadataHost host, ISourceMethodBody/*!*/ possibleIterator) {
      if (possibleIterator == Dummy.MethodBody) return null;
      var nameTable = host.NameTable;
      var possibleIteratorBody = possibleIterator.Block;
      foreach (var statement in possibleIteratorBody.Statements) {
        ICreateObjectInstance createObjectInstance = GetICreateObjectInstance(statement);
        if (createObjectInstance == null) {
          // If the first statement in the method body is not the creation of iterator closure, return a dummy.
          // Possible corner case not handled: a local is used to hold the constant value for the initial state of the closure.
          return null;
        }
        ITypeReference closureType/*?*/ = createObjectInstance.MethodToCall.ContainingType;
        ITypeReference unspecializedClosureType = ContractHelper.Unspecialized(closureType);
        if (!AttributeHelper.Contains(unspecializedClosureType.Attributes, host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
          return null;
        INestedTypeReference closureTypeAsNestedTypeReference = unspecializedClosureType as INestedTypeReference;
        if (closureTypeAsNestedTypeReference == null) return null;
        ITypeReference unspecializedClosureContainingType = ContractHelper.Unspecialized(closureTypeAsNestedTypeReference.ContainingType);
        if (closureType != null && TypeHelper.TypesAreEquivalent(possibleIterator.MethodDefinition.ContainingTypeDefinition, unspecializedClosureContainingType)) {
          IName MoveNextName = nameTable.GetNameFor("MoveNext");
          foreach (ITypeDefinitionMember member in closureType.ResolvedType.GetMembersNamed(MoveNextName, false)) {
            IMethodDefinition moveNext = member as IMethodDefinition;
            if (moveNext != null) {
              ISpecializedMethodDefinition moveNextGeneric = moveNext as ISpecializedMethodDefinition;
              if (moveNextGeneric != null)
                moveNext = moveNextGeneric.UnspecializedVersion.ResolvedMethod;
              return moveNext.Body as ISourceMethodBody;
            }
          }
        }
        return null;
      }
      return null;
    }
    public static ISourceMethodBody/*?*/ FindClosureGetEnumerator(IMetadataHost host, INestedTypeDefinition closureClass) {
      return null;
    }

    public static MethodContract GetMethodContractFromMoveNext(
      IContractAwareHost host,
      ContractExtractor extractor,
      ISourceMethodBody iteratorMethodBody,
      ISourceMethodBody moveNextBody,
      PdbReader pdbReader
      ) {
      // Walk the iterator method and collect all of the state that is assigned to fields in the iterator class
      // That state needs to replace any occurrences of the fields in the contracts (if they exist...)
      var iteratorStmts = new List<IStatement>(iteratorMethodBody.Block.Statements);
      // First statement should be the creation of the iterator class
      int j = 1;
      Dictionary<uint, IExpression> capturedThings = new Dictionary<uint, IExpression>();
      // Find all of the state captured for the IEnumerable
      // REVIEW: Is this state ever used in the contracts? Since they're all sitting in the MoveNext
      // method, maybe they always use the IEnumerator state?
      while (j < iteratorStmts.Count) {
        var es = iteratorStmts[j++] as IExpressionStatement;
        if (es == null) break;
        var assign = es.Expression as IAssignment;
        if (assign == null) break;
        var field = assign.Target.Definition as IFieldReference;
        var capturedThing = assign.Source;
        var k = field.InternedKey;
        var spec = field as ISpecializedFieldReference;
        if (spec != null) k = spec.UnspecializedVersion.InternedKey;
        capturedThings.Add(k, capturedThing);
      }
      // Find all of the state captured for the IEnumerator
      // That state is captured at the beginning of the IEnumerable<T>.GetEnumerator method
      MethodDefinition getEnumerator = null;
      var t = moveNextBody.MethodDefinition.ContainingTypeDefinition;
      foreach (IMethodImplementation methodImplementation in t.ExplicitImplementationOverrides) {
        if (methodImplementation.ImplementedMethod.Name == host.NameTable.GetNameFor("GetEnumerator")) {
          var gtir = methodImplementation.ImplementedMethod.ContainingType as IGenericTypeInstanceReference;
          if (gtir != null && TypeHelper.TypesAreEquivalent(gtir.GenericType, host.PlatformType.SystemCollectionsGenericIEnumerable)) {
            getEnumerator = methodImplementation.ImplementingMethod.ResolvedMethod as MethodDefinition;
            break;
          }
        }
      }
      if (getEnumerator != null) {
        ISourceMethodBody geBody = (ISourceMethodBody)getEnumerator.Body;
        foreach (var stmt in geBody.Block.Statements) {
          var es = stmt as IExpressionStatement;
          if (es == null) continue;
          var assign = es.Expression as IAssignment;
          if (assign == null) continue;
          var field2 = assign.Target.Definition as IFieldReference;
          if (field2 == null) continue;
          var k = field2.InternedKey;
          var spec = field2 as ISpecializedFieldReference;
          if (spec != null) k = spec.UnspecializedVersion.InternedKey;

          var sourceBe = assign.Source as IBoundExpression;
          if (sourceBe == null) continue;
          var field3 = sourceBe.Definition as IFieldReference;
          if (field3 == null) continue;
          var k3 = field3.InternedKey;
          var spec3 = field3 as ISpecializedFieldReference;
          if (spec3 != null) k3 = spec3.UnspecializedVersion.InternedKey;
          IExpression capturedThing = null;
          if (!capturedThings.TryGetValue(k3, out capturedThing)) continue;
          capturedThings.Add(k, capturedThing);
        }
      }

      var mc = HermansAlwaysRight.ExtractContracts(host, pdbReader, extractor, moveNextBody);

      // substitute all field references in contract with the captured state
      var replacer = new Replacer(host, capturedThings);
      mc = (MethodContract)replacer.Visit(mc);

      if (moveNextBody.MethodDefinition.ContainingTypeDefinition.IsGeneric) {
        var genericParameterMapper = new GenericMethodParameterMapper(host, iteratorMethodBody.MethodDefinition, moveNextBody.MethodDefinition.ContainingType as INestedTypeReference);
        mc = genericParameterMapper.Visit(mc) as MethodContract;
        //foreach (var v in this.capturedBinding.Values) {
        //  // Do NOT visit any of the parameters in the table because that
        //  // will cause them to (possibly) have their types changed. But
        //  // they already have the correct type because they are parameters
        //  // of the enclosing method.
        //  // But the locals are ones that were created by this visitor so
        //  // they need their types updated.
        //  LocalDefinition ld = v.Definition as LocalDefinition;
        //  if (ld != null) {
        //    ld.Type = this.genericParameterMapper.Visit(ld.Type);
        //    ld.MethodDefinition = this.sourceMethodBody.MethodDefinition;
        //  }
        //}
      }

      return mc;

      //var b = FindBlock.FindBlockLeadingToContract(extractor, moveNextBody.Block) as BlockStatement;
      //if (b != null) {
      //  var index = extractor.LinearizeBlocks(b);
      //  int endBlock;
      //  int endStmt;
      //  var found = extractor.IndexOf(s => extractor.IsPrePostEndOrLegacy(s, true),
      //    index, 0, 0, out endBlock, out endStmt);
      //while (found) {
      //  var locals = FindLocals.FindSetOfLocals(b.Statements[endStmt]);
      //  if (0 == locals.Count) {
      //    extractor.

      //}

      //}

      //return mc;
    }

    private sealed class Replacer : MethodBodyCodeAndContractMutator {

      Dictionary<uint, IExpression> capturedThings = new Dictionary<uint, IExpression>();

      public Replacer(IMetadataHost host, Dictionary<uint, IExpression> capturedThings)
        : base(host, true) {
        this.capturedThings = capturedThings;
      }

      /// <summary>
      /// If the <paramref name="boundExpression"/> represents a parameter of the target method,
      /// it is replaced with the equivalent parameter of the source method.
      /// </summary>
      /// <param name="boundExpression">The bound expression.</param>
      public override IExpression Visit(BoundExpression boundExpression) {
        IExpression capturedThing;
        var field = boundExpression.Definition as IFieldReference;
        if (field != null) {
          var k = field.InternedKey;
          var spec = field as ISpecializedFieldReference;
          if (spec != null) k = spec.UnspecializedVersion.InternedKey;
          if (this.capturedThings.TryGetValue(k, out capturedThing))
            return capturedThing;
        }
        return base.Visit(boundExpression);
      }

      public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
        IExpression capturedThing;
        var field = addressableExpression.Definition as IFieldReference;
        if (field != null) {
          var k = field.InternedKey;
          var spec = field as ISpecializedFieldReference;
          if (spec != null) k = spec.UnspecializedVersion.InternedKey;
          if (this.capturedThings.TryGetValue(k, out capturedThing)) {
            var be = capturedThing as IBoundExpression;
            if (be == null) {
              System.Diagnostics.Debug.Assert(false);
            }
            addressableExpression.Definition = be.Definition;
            addressableExpression.Instance = be.Instance;
            return addressableExpression;
          }
        }
        return base.Visit(addressableExpression);
      }

    }

    //public static MethodContract GetMethodContractFromMoveNext2(
    //  IContractAwareHost host,
    //  ISourceMethodBody iteratorMethodBody,
    //  ISourceMethodBody moveNextBody,
    //  PdbReader pdbReader,
    //  ILocalScopeProvider localScopeProvider
    //  ) {
    //  // Walk the iterator method and collect all of the state that is assigned to fields in the iterator class
    //  // That state needs to replace any occurrences of the fields in the contracts (if they exist...)
    //  var iteratorStmts = new List<IStatement>(iteratorMethodBody.Block.Statements);
    //  // First statement should be the creation of the iterator class
    //  int j = 1;
    //  Dictionary<object, IExpression> capturedThings = new Dictionary<object, IExpression>();
    //  // Find all of the state captured for the IEnumerable
    //  // REVIEW: Is this state ever used in the contracts? Since they're all sitting in the MoveNext
    //  // method, maybe they always use the IEnumerator state?
    //  while (j < iteratorStmts.Count) {
    //    var es = iteratorStmts[j++] as IExpressionStatement;
    //    if (es == null) break;
    //    var assign = es.Expression as IAssignment;
    //    if (assign == null) break;
    //    var field = assign.Target.Definition;
    //    var capturedThing = assign.Source;
    //    capturedThings.Add(field, capturedThing);
    //  }
    //  // Find all of the state captured for the IEnumerator
    //  // Get the IEnumerable<T>.GetEnumerator
    //  MethodDefinition getEnumerator = null;
    //  var t = moveNextBody.MethodDefinition.ContainingTypeDefinition;
    //  foreach (IMethodImplementation methodImplementation in t.ExplicitImplementationOverrides) {
    //    if (methodImplementation.ImplementedMethod.Name == host.NameTable.GetNameFor("GetEnumerator")) {
    //      var gtir = methodImplementation.ImplementedMethod.ContainingType as IGenericTypeInstanceReference;
    //      if (gtir != null && TypeHelper.TypesAreEquivalent(gtir.GenericType, host.PlatformType.SystemCollectionsGenericIEnumerable)) {
    //        getEnumerator = methodImplementation.ImplementingMethod.ResolvedMethod as MethodDefinition;
    //        break;
    //      }
    //    }
    //  }
    //  if (getEnumerator != null) {
    //    ISourceMethodBody geBody = (ISourceMethodBody)getEnumerator.Body;
    //    foreach (var stmt in geBody.Block.Statements) {
    //      var es = stmt as IExpressionStatement;
    //      if (es == null) continue;
    //      var assign = es.Expression as IAssignment;
    //      if (assign == null) continue;
    //      var field2 = assign.Target.Definition as IFieldReference;
    //      if (field2 == null) continue;

    //      var sourceBe = assign.Source as IBoundExpression;
    //      if (sourceBe == null) continue;
    //      IExpression capturedThing = null;
    //      if (!capturedThings.TryGetValue(sourceBe.Definition, out capturedThing)) continue;
    //      capturedThings.Add(field2, capturedThing);
    //    }
    //  }

    //  // Find the first statement in the first state of the MoveNext state machine where the contracts (might) start
    //  var currentBlock = moveNextBody.Block as BlockStatement;
    //  var stmts = new List<IStatement>(currentBlock.Statements);
    //  var i = 0;
    //  var n = stmts.Count;

    //  // Skip any local declaration statements that have no initial value
    //  while (i < n) {
    //    var lds = stmts[i] as ILocalDeclarationStatement;
    //    if (lds == null || lds.InitialValue != null) break;
    //    i++;
    //  }
    //  if (i == n) throw new InvalidDataException("Could not find switch statement in iterator's MoveNext method");

    //  // MoveNext might consist of a try-fault statement
    //  var tryFault = stmts[i] as ITryCatchFinallyStatement;
    //  if (tryFault != null) {
    //    currentBlock = tryFault.TryBody as BlockStatement;
    //    stmts = new List<IStatement>(currentBlock.Statements);
    //    i = 0;
    //    n = stmts.Count;
    //  }
    //  ISwitchStatement switchStmt = null;
    //  while (switchStmt == null && i < n) {
    //    switchStmt = stmts[i++] as ISwitchStatement;
    //  }
    //  if (i == n) throw new InvalidDataException("Could not find switch statement in iterator's MoveNext method");
    //  ISwitchCase switchCase = IteratorHelper.First(switchStmt.Cases);
    //  IName label = null;
    //  foreach (var s in switchCase.Body) {
    //    var gotoStmt = s as IGotoStatement;
    //    if (gotoStmt != null) {
    //      var labeledStmt = gotoStmt.TargetStatement as ILabeledStatement;
    //      if (labeledStmt != null) {
    //        label = labeledStmt.Label;
    //        break;
    //      }
    //    }
    //  }
    //  if (label == null) throw new InvalidDataException("Could not find goto statement in iterator's MoveNext method");
    //  // Now find labeled statement has that label
    //  while (i < n) {
    //    var labeledStmt = stmts[i++] as ILabeledStatement;
    //    if (labeledStmt != null && labeledStmt.Label == label) {
    //      break;
    //    }
    //  }
    //  if (i == n) throw new InvalidDataException("Could not find labeled statement in iterator's MoveNext method");
    //  // stmts[i] should be a goto statement targetting the *real* start of the first state
    //  var gotoStmt2 = stmts[i] as IGotoStatement;
    //  if (gotoStmt2 == null) throw new InvalidDataException("Could not find goto2 statement in iterator's MoveNext method");
    //  var labeledStmt2 = gotoStmt2.TargetStatement as ILabeledStatement;
    //  if (labeledStmt2 == null) throw new InvalidDataException("goto2 statement in iterator's MoveNext method did not point to a labeled statement");
    //  // Now find labeled statement has that label
    //  while (i < n) {
    //    var labeledStmt = stmts[i++] as ILabeledStatement;
    //    if (labeledStmt != null && labeledStmt.Label == labeledStmt2.Label) {
    //      break;
    //    }
    //  }
    //  if (i == n) throw new InvalidDataException("Could not real start of state 0 in iterator's MoveNext method");
    //  // stmts[i] is the assignment to the iterator's state field of the next state (assume that for now)
    //  var result = ContractExtractor.MoveNextExtractor(host, pdbReader, localScopeProvider, moveNextBody,
    //    currentBlock, 0, i + 1, 0, stmts.Count - 1);

    //  var initialStmts = currentBlock.Statements.GetRange(0, i);
    //  initialStmts.AddRange(result.BlockStatement.Statements);
    //  currentBlock.Statements = initialStmts;

    //  var mc = result.MethodContract as MethodContract;

    //  // substitute all field references in contract with the captured state
    //  var replacer = new Replacer(host, capturedThings);
    //  mc = (MethodContract)replacer.Visit(mc);

    //  return mc;
    //}

    public class HermansAlwaysRight : BaseCodeTraverser {

      ContractExtractor extractor;
      IContractAwareHost contractAwareHost;
      ISourceMethodBody sourceMethodBody;
      private bool methodIsInReferenceAssembly;
      OldAndResultExtractor oldAndResultExtractor;
      private PdbReader pdbReader;

      private MethodContract/*?*/ currentMethodContract;
      private MethodContract/*!*/ CurrentMethodContract {
        get {
          if (this.currentMethodContract == null) {
            this.currentMethodContract = new MethodContract();
          }
          return this.currentMethodContract;
        }
      }

      private HermansAlwaysRight(IContractAwareHost contractAwareHost, ContractExtractor extractor, ISourceMethodBody sourceMethodBody, bool methodIsInReferenceAssembly, OldAndResultExtractor oldAndResultExtractor, PdbReader/*?*/ pdbReader) {
        this.contractAwareHost = contractAwareHost;
        this.extractor = extractor;
        this.sourceMethodBody = sourceMethodBody;
        this.methodIsInReferenceAssembly = methodIsInReferenceAssembly;
        this.oldAndResultExtractor = oldAndResultExtractor;
        this.pdbReader = pdbReader;
      }

      public static MethodContract/*?*/ ExtractContracts(IContractAwareHost contractAwareHost, PdbReader/*?*/ pdbReader, ContractExtractor extractor, ISourceMethodBody methodBody) {
        // Need to tweak this to handle finding contracts in MoveNext methods within (compiler-generated) iterator classes.
        //if (!FindAnyContract.Find(methodBody.Block)) return null;
        var definingUnit = TypeHelper.GetDefiningUnit(methodBody.MethodDefinition.ContainingType.ResolvedType);
        var methodIsInReferenceAssembly = ContractHelper.IsContractReferenceAssembly(contractAwareHost, definingUnit);
        var contractAssemblyReference = new Immutable.AssemblyReference(contractAwareHost, definingUnit.ContractAssemblySymbolicIdentity);
        var contractClass = ContractHelper.CreateTypeReference(contractAwareHost, contractAssemblyReference, "System.Diagnostics.Contracts.Contract");
        var oldAndResultExtractor = new OldAndResultExtractor(contractAwareHost, methodBody, null, contractClass);
        var har = new HermansAlwaysRight(contractAwareHost, extractor, methodBody, methodIsInReferenceAssembly, oldAndResultExtractor, pdbReader);
        har.Visit(methodBody);
        return har.currentMethodContract;
      }
      public override void Visit(IStatement statement) {
        if (extractor.IsPrePostEndOrLegacy(statement, true)) {

          //this.stopTraversal = true;

          var tempStack = new System.Collections.Stack();

          var b = this.path.Pop() as BlockStatement;
          tempStack.Push(b);
          if (b == null) goto RestorePathAndReturn;

          var stmts = new List<IStatement>(b.Statements);
          var lastStmtIndex = stmts.IndexOf(statement);

          // TODO: later contracts might re-use locals defined
          // just before the first contract.
          // TODO: what if there are statements having to do with
          // closure initialization or ctor stuff that are in between
          // the local definitions and the first contract? Need to
          // recognize them and not include them in any contract...
          var locs = FindLocals.FindSetOfLocals(statement);
          int firstStmtIndex = lastStmtIndex - 1;

          var clump = new List<IStatement>();

          while (0 < locs.Count) {
            // walk backwards until all locals accounted for by either definitions or assignments

            while (0 <= firstStmtIndex && 0 < locs.Count) {
              var s = stmts[firstStmtIndex];
              var lds = s as ILocalDeclarationStatement;
              if (lds != null) {
                if (locs.Contains(lds.LocalVariable)) locs.Remove(lds.LocalVariable);
              } else {
                var loc = GetLocalTargetIfItExists(s);
                if (loc != null && locs.Contains(loc)) locs.Remove(loc);
              }
              firstStmtIndex--;
            }

            if (0 < locs.Count) {
              // then we've walked backwards over all of the statements in the current block.
              // need to reset state to previous block in path

              // walking backwards means need to prepend onto the accumulating clump
              clump.InsertRange(0, stmts.GetRange(firstStmtIndex + 1, lastStmtIndex - firstStmtIndex));
              stmts.RemoveRange(firstStmtIndex + 1, lastStmtIndex - firstStmtIndex);
              b.Statements = stmts;

              b = this.path.Pop() as BlockStatement;
              System.Diagnostics.Debug.Assert(b != null);
              tempStack.Push(b);
              stmts = new List<IStatement>(b.Statements);
              lastStmtIndex = stmts.Count - 2; // remember: last statement in b is actually the next block!
              firstStmtIndex = lastStmtIndex;
            }

          }

          // walking backwards means need to prepend onto the accumulating clump
          clump.InsertRange(0, stmts.GetRange(firstStmtIndex + 1, lastStmtIndex - firstStmtIndex));
          stmts.RemoveRange(firstStmtIndex + 1, lastStmtIndex - firstStmtIndex);
          b.Statements = stmts;

          ExtractContract(clump);

        RestorePathAndReturn:
          while (0 < tempStack.Count) {
            this.path.Push(tempStack.Pop());
          }
          return;
        }
        base.Visit(statement);
        return;
      }

      private ILocalDefinition/*?*/ GetLocalTargetIfItExists(IStatement statement) {
        var es = statement as IExpressionStatement;
        if (es == null) return null;
        var assgn = es.Expression as IAssignment;
        if (assgn == null) return null;
        // simple case: loc := e;
        var loc = assgn.Target.Definition as ILocalDefinition;
        if (loc != null) return loc;
        // next case: &loc := e; (because loc is a pointer)
        var addr = assgn.Target.Definition as IAddressDereference;
        if (addr != null) {
          var addrOf = addr.Address as IAddressOf;
          if (addrOf != null) {
            var ae = addrOf.Expression as IAddressableExpression;
            if (ae != null) {
              return ae.Definition as ILocalDefinition;
            }
          }
        }

        return null;
      }
      private class FindLocals : BaseCodeTraverser {
        List<ILocalDefinition> undeclaredLocals = new List<ILocalDefinition>();
        List<ILocalDefinition> declaredLocals = new List<ILocalDefinition>();
        public static List<ILocalDefinition> FindSetOfLocals(IStatement s) {
          var fl = new FindLocals();
          fl.Visit(s);
          return fl.undeclaredLocals;
        }
        private FindLocals() {
        }
        public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
          this.declaredLocals.Add(localDeclarationStatement.LocalVariable);
          base.Visit(localDeclarationStatement);
        }
        public override void Visit(ILocalDefinition localDefinition) {
          if (!this.declaredLocals.Contains(localDefinition) && !this.undeclaredLocals.Contains(localDefinition))
            this.undeclaredLocals.Add(localDefinition);
        }
        public override void VisitReference(ILocalDefinition localDefinition) {
          if (!this.declaredLocals.Contains(localDefinition) && !this.undeclaredLocals.Contains(localDefinition))
            this.undeclaredLocals.Add(localDefinition);
        }

      }
      internal void ExtractContract(List<IStatement> clump) {
        var lastIndex = clump.Count - 1;
        ExpressionStatement/*?*/ expressionStatement = clump[lastIndex] as ExpressionStatement;
        if (expressionStatement == null) {
          if (ContractExtractor.IsLegacyRequires(clump[lastIndex])) {
            ExtractLegacyRequires(clump);
          }
        } else {
          IMethodCall/*?*/ methodCall = expressionStatement.Expression as IMethodCall;
          if (methodCall != null) {
            IMethodReference methodToCall = methodCall.MethodToCall;
            if (extractor.IsContractMethod(methodToCall)) {
              string mname = methodToCall.Name.Value;
              List<IExpression> arguments = new List<IExpression>(methodCall.Arguments);
              int numArgs = arguments.Count;
              if (numArgs == 0 && mname == "EndContractBlock") return;
              if (!(numArgs == 1 || numArgs == 2 || numArgs == 3)) return;

              var locations = new List<ILocation>(methodCall.Locations);
              if (locations.Count == 0) {
                for (int i = lastIndex; 0 <= i; i--) {
                  if (IteratorHelper.EnumerableIsNotEmpty(clump[i].Locations)) {
                    locations.AddRange(clump[i].Locations);
                    break;
                  }
                }
              }

              // Create expression for contract
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
                  Type = this.contractAwareHost.PlatformType.SystemBoolean,
                };
              }

              var isModel = FindModelMembers.ContainsModelMembers(contractExpression);

              string/*?*/ origSource = null;
              if (this.methodIsInReferenceAssembly) {
                origSource = GetStringFromArgument(arguments[2]);
              } else {
                if (this.pdbReader != null)
                  ContractExtractor.TryGetConditionText(this.pdbReader, locations, numArgs, out origSource);
              }
              IExpression/*?*/ description = numArgs >= 2 ? arguments[1] : null;

              IGenericMethodInstanceReference/*?*/ genericMethodToCall = methodToCall as IGenericMethodInstanceReference;

              if (mname == "Ensures") {
                contractExpression = this.oldAndResultExtractor.Visit(contractExpression);
                var postcondition = new Postcondition() {
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
                var exceptionalPostcondition = new ThrownException() {
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
                      Type = this.contractAwareHost.PlatformType.SystemType,
                      TypeToGet = a,
                      Locations = new List<ILocation>(a.Locations),
                    };
                    break;
                  }
                }
                var precondition = new Precondition() {
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
            } else if (ContractExtractor.IsValidatorOrAbbreviator(expressionStatement)) {
              var gmir = methodToCall as IGenericMethodInstanceReference;
              IMethodDefinition abbreviatorDef;
              if (gmir != null)
                abbreviatorDef = gmir.GenericMethod.ResolvedMethod;
              else
                abbreviatorDef = methodToCall.ResolvedMethod;
              var mc = ContractHelper.GetMethodContractFor(this.contractAwareHost, abbreviatorDef);
              if (mc != null) {
                mc = ContractHelper.InlineAndStuff(this.contractAwareHost, mc, this.sourceMethodBody.MethodDefinition, abbreviatorDef, new List<IExpression>(methodCall.Arguments));
                if (this.currentMethodContract == null)
                  this.currentMethodContract = new MethodContract(mc);
                else
                  ContractHelper.AddMethodContract(this.currentMethodContract, mc);
              }
            }
          }
        }
      }
      internal void ExtractLegacyRequires(List<IStatement> clump) {
        var lastIndex = clump.Count - 1;
        ConditionalStatement conditional = clump[lastIndex] as ConditionalStatement;
        //^ requires IsLegacyRequires(conditional);
        EmptyStatement empty = conditional.FalseBranch as EmptyStatement;
        IBlockStatement blockStatement = conditional.TrueBranch as IBlockStatement;
        List<IStatement> statements = new List<IStatement>(blockStatement.Statements);
        var statement = statements[statements.Count - 1];
        IExpression failureBehavior;
        IThrowStatement throwStatement = statement as IThrowStatement;
        var locations = new List<ILocation>(conditional.Condition.Locations);
        if (locations.Count == 0) {
          locations.AddRange(conditional.Locations);
        }

        string origSource = null;
        if (0 < locations.Count) {
          if (this.pdbReader != null)
            ContractExtractor.TryGetConditionText(this.pdbReader, locations, 1, out origSource);
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
        if (clump.Count == 1) {
          contractExpression = conditional.Condition;
        } else {
          var allButLastStatement = new List<IStatement>();
          for (int i = 0; i < lastIndex; i++) {
            allButLastStatement.Add(clump[i]);
          }
          contractExpression = new BlockExpression() {
            BlockStatement = new BlockStatement() {
              Statements = allButLastStatement,
            },
            Expression = conditional.Condition,
            Type = this.contractAwareHost.PlatformType.SystemBoolean,
          };
        }

        Precondition precondition = new Precondition() {
          AlwaysCheckedAtRuntime = true,
          Condition = new LogicalNot() {
            Locations = new List<ILocation>(locations),
            Operand = contractExpression,
            Type = this.contractAwareHost.PlatformType.SystemBoolean,
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
      private string AdjustIndentationOfMultilineSourceText(string sourceText, int trimLength) {
        if (!sourceText.Contains("\n")) return sourceText;
        var lines = sourceText.Split('\n');
        if (lines.Length == 1) return sourceText;
        var trimmedSecondLine = lines[1].TrimStart(ContractExtractor.WhiteSpace);
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
        var numberOfLinesToJoin = String.IsNullOrEmpty(lines[lines.Length - 1].TrimStart(ContractExtractor.WhiteSpace)) ? lines.Length - 1 : lines.Length;
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

      /// <summary>
      /// TODO: Don't use string comparisons
      /// </summary>
      private class FindAnyContract : BaseCodeTraverser {
        bool found = false;
        public static bool Find(IBlockStatement block) {
          var fac = new FindAnyContract();
          fac.Visit(block);
          return fac.found;
        }
        private FindAnyContract() {
        }
        public override void Visit(IMethodCall methodCall) {
          var ct = TypeHelper.GetTypeName(methodCall.MethodToCall.ContainingType);
          if (ct.Equals("System.Diagnostics.Contracts")) {
            var mName = methodCall.MethodToCall.Name.Value;
            if (mName.Equals("Requires") || mName.Equals("Ensures") || mName.Equals("EndContractBlock")) {
              this.found = true;
              this.stopTraversal = true;
              return;
            }
          }
          base.Visit(methodCall);
        }
      }

    }
  }

  /// <summary>
  /// If the original method that contained the anonymous delegate is generic, then
  /// the code generated by the compiler, the "closure method", is also generic.
  /// If the anonymous delegate didn't capture any locals or parameters, then a
  /// (generic) static method was generated to implement the lambda.
  /// If it did capture things, then the closure method is a non-generic instance
  /// method in a generic class.
  /// In either case, any references to those generic parameters need to be mapped back
  /// to become references to the original method's generic parameters.
  /// Create an instance of this class for each anonymous delegate using the appropriate
  /// constructor. This is known from whether the closure method is (static and generic)
  /// or (instance and not-generic, but whose containing type is generic).
  /// Those are the only two patterns created by the compiler.
  /// </summary>
  internal class GenericMethodParameterMapper : CodeAndContractMutatingVisitor {

    /// <summary>
    /// The original generic method in which the anonymous delegate is being re-created.
    /// </summary>
    readonly IMethodDefinition targetMethod;
    /// <summary>
    /// The constructed reference to the targetMethod that is used within any
    /// generated generic method parameter reference.
    /// </summary>
    readonly Microsoft.Cci.MutableCodeModel.MethodReference targetMethodReference;
    /// <summary>
    /// Just a short-cut to the generic parameters so the list can be created once
    /// and then the individual parameters can be accessed with an indexer.
    /// </summary>
    readonly List<IGenericMethodParameter> targetMethodGenericParameters;
    /// <summary>
    /// Used only when mapping from a generic method (i.e., a static closure method) to
    /// the original generic method.
    /// </summary>
    readonly IMethodDefinition/*?*/ sourceMethod;
    /// <summary>
    /// Used only when mapping from a method in a generic class (i.e., a closure class)
    /// to the original generic method.
    /// </summary>
    readonly INestedTypeReference/*?*/ sourceType;

    //^ Contract.Invariant((this.sourceMethod == null) != (this.sourceType == null));

    /// <summary>
    /// Use this constructor when the anonymous delegate did not capture any locals or parameters
    /// and so was implemented as a static, generic closure method.
    /// </summary>
    public GenericMethodParameterMapper(IMetadataHost host, IMethodDefinition targetMethod, IMethodDefinition sourceMethod)
      : this(host, targetMethod) {
      this.sourceMethod = sourceMethod;
    }
    /// <summary>
    /// Use this constructor when the anonymous delegate did capture a local or parameter
    /// and so was implemented as an instance, non-generic closure method within a generic
    /// class.
    /// </summary>
    public GenericMethodParameterMapper(IMetadataHost host, IMethodDefinition targetMethod, INestedTypeReference sourceType)
      : this(host, targetMethod) {
      this.sourceType = sourceType;
    }

    private GenericMethodParameterMapper(IMetadataHost host, IMethodDefinition targetMethod)
      : base(host) {
      this.targetMethod = targetMethod;
      this.targetMethodGenericParameters = new List<IGenericMethodParameter>(targetMethod.GenericParameters);
      this.targetMethodReference = new Microsoft.Cci.MutableCodeModel.MethodReference();
      this.targetMethodReference.Copy(this.targetMethod, this.host.InternFactory);
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      // No sense in doing this more than once
      ITypeReference result = null;
      IReference mappedTo = null;
      if (this.referenceCache.TryGetValue(typeReference, out mappedTo)) {
        result = (ITypeReference)mappedTo;
        return result;
      }
      if (this.sourceMethod != null) {
        IGenericMethodParameterReference gmpr = typeReference as IGenericMethodParameterReference;
        if (gmpr != null && gmpr.DefiningMethod == this.sourceMethod) {
          result = this.targetMethodGenericParameters[gmpr.Index];
        }
      } else { // this.sourceType != null
        IGenericTypeParameterReference gtpr = typeReference as IGenericTypeParameterReference;
        if (gtpr != null && gtpr.DefiningType == this.sourceType) {
          result = this.targetMethodGenericParameters[gtpr.Index];
        }
      }
      if (result == null)
        result = base.Visit(typeReference);
      // The base visit might have resulted in a copy (if typeReference is an immutable object)
      // in which case the base visit will already have put it into the cache (associated with
      // result). So need to check before adding it.
      if (!this.referenceCache.ContainsKey(typeReference))
        this.referenceCache.Add(typeReference, result);
      return result;
    }
  }

}