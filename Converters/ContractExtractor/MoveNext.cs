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
using System.Linq;
using Microsoft.Cci.ILToCodeModel;

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
    /// <param name="host"></param>
    /// <param name="possibleIterator">The (potential) iterator method.</param>
    /// <returns>Dummy.MethodBody if <paramref name="possibleIterator"/> does not fit into the code pattern of an iterator method, 
    /// or the body of the MoveNext method of the corresponding closure class if it does.
    /// </returns>
    public static IMethodBody/*?*/ FindClosureMoveNext(IMetadataHost host, ISourceMethodBody/*!*/ possibleIterator) {
      if (possibleIterator is Dummy) return null;
      var nameTable = host.NameTable;
      var possibleIteratorBody = possibleIterator.Block;
      foreach (var statement in possibleIteratorBody.Statements) {
        var expressionStatement = statement as IExpressionStatement;
        if (expressionStatement != null)
          return FirstStatementIsIteratorCreation(host, possibleIterator, nameTable, statement);
        break;
      }
      foreach (var statement in possibleIteratorBody.Statements){
        //var lds = statement as ILocalDeclarationStatement;
        //if (lds != null) {
        //  if (lds.InitialValue != null)
        //    return null;
        //  else
        //    continue;
        //}
        var returnStatement = statement as IReturnStatement;
        if (returnStatement == null) return null;
        var blockExpression = returnStatement.Expression as IBlockExpression;
        if (blockExpression == null) return null;
        foreach (var s in blockExpression.BlockStatement.Statements) {
          return FirstStatementIsIteratorCreation(host, possibleIterator, nameTable, s);
        }
      }
      return null;
    }

    private static IMethodBody/*?*/ FirstStatementIsIteratorCreation(IMetadataHost host, ISourceMethodBody possibleIterator, INameTable nameTable, IStatement statement) {
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
            return moveNext.Body;
          }
        }
      }
      return null;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="closureClass"></param>
    /// <returns></returns>
    public static ISourceMethodBody/*?*/ FindClosureGetEnumerator(IMetadataHost host, INestedTypeDefinition closureClass) {
      return null;
    }
    
    // TODO: First search the moveNextBody to see if there are any contracts at all.
    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="extractor"></param>
    /// <param name="iteratorMethodBody"></param>
    /// <param name="moveNextBody"></param>
    /// <param name="pdbReader"></param>
    /// <returns></returns>
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
      Dictionary<uint, IExpression> capturedThings = new Dictionary<uint, IExpression>();
      // Find all of the state captured for the IEnumerable
      // REVIEW: Is this state ever used in the contracts? Since they're all sitting in the MoveNext
      // method, maybe they always use the IEnumerator state?
      if (1 < iteratorStmts.Count) {
        // First statement should be the creation of the iterator class
        int j = 1;
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
      } else {
        var ret = iteratorStmts[0] as IReturnStatement;
        if (ret != null) {
          var be = ret.Expression as IBlockExpression;
          if (be != null) {
            var beStmts = new List<IStatement>(be.BlockStatement.Statements);
            var j = 1;
            while (j < beStmts.Count) {
              var es = beStmts[j++] as IExpressionStatement;
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
          }
        }
      }
      // Find all of the state captured for the IEnumerator
      // That state is captured at the beginning of the IEnumerable<T>.GetEnumerator method
      IMethodDefinition getEnumerator = null;
      var t = moveNextBody.MethodDefinition.ContainingTypeDefinition;
      foreach (IMethodImplementation methodImplementation in t.ExplicitImplementationOverrides) {
        if (methodImplementation.ImplementedMethod.Name == host.NameTable.GetNameFor("GetEnumerator")) {
          var gtir = methodImplementation.ImplementedMethod.ContainingType as IGenericTypeInstanceReference;
          if (gtir != null && TypeHelper.TypesAreEquivalent(gtir.GenericType, host.PlatformType.SystemCollectionsGenericIEnumerable)) {
            getEnumerator = methodImplementation.ImplementingMethod.ResolvedMethod as IMethodDefinition;
            break;
          }
        }
      }
      if (getEnumerator != null) {
        IMethodBody geBody = getEnumerator.Body;
        var sourceGeBody = geBody as ISourceMethodBody;
        if (sourceGeBody == null)
          sourceGeBody = Decompiler.GetCodeModelFromMetadataModel(host, geBody, pdbReader, DecompilerOptions.AnonymousDelegates);
        foreach (var stmt in sourceGeBody.Block.Statements) {
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
          capturedThings[k] = capturedThing;
        }
      }

      var mc = HermansAlwaysRight.ExtractContracts(host, pdbReader, extractor, moveNextBody);

      if (mc == null) return mc;

      // substitute all field references in contract with the captured state
      var replacer = new Replacer(host, capturedThings, iteratorMethodBody.MethodDefinition, moveNextBody.MethodDefinition);
      replacer.RewriteChildren(mc);

      if (moveNextBody.MethodDefinition.ContainingTypeDefinition.IsGeneric) {
        var genericParameterMapper = new GenericMethodParameterMapper(host, iteratorMethodBody.MethodDefinition, moveNextBody.MethodDefinition.ContainingType as INestedTypeReference);
        mc = genericParameterMapper.Rewrite(mc) as MethodContract;
      }

      return mc;

    }

    /// <summary>
    /// Substitutes all field references with the expressions that are in the captured things table
    /// passed to the constructor.
    /// Also replaces locals from the MoveNext method with new locals that belong to the interator method.
    /// </summary>
    private sealed class Replacer : CodeAndContractRewriter {

      Dictionary<uint, IExpression> capturedThings = new Dictionary<uint, IExpression>();
      private IMethodDefinition moveNext;
      private IMethodDefinition iterator;
      private Dictionary<ILocalDefinition, ILocalDefinition> localTable = new Dictionary<ILocalDefinition, ILocalDefinition>();

      public Replacer(IMetadataHost host, Dictionary<uint, IExpression> capturedThings, IMethodDefinition iteratorMethod,
      IMethodDefinition moveNext)
        : base(host, true) {
        this.capturedThings = capturedThings;
        this.iterator = iteratorMethod;
        this.moveNext = moveNext; 
      }

      /// <summary>
      /// If the <paramref name="boundExpression"/> represents a parameter of the target method,
      /// it is replaced with the equivalent parameter of the source method.
      /// </summary>
      /// <param name="boundExpression">The bound expression.</param>
      public override IExpression Rewrite(IBoundExpression boundExpression) {
        IExpression capturedThing;
        var field = boundExpression.Definition as IFieldReference;
        if (field != null) {
          var k = field.InternedKey;
          var spec = field as ISpecializedFieldReference;
          if (spec != null) k = spec.UnspecializedVersion.InternedKey;
          if (this.capturedThings.TryGetValue(k, out capturedThing))
            return capturedThing;
        }
        return base.Rewrite(boundExpression);
      }

      public override void RewriteChildren(AddressableExpression addressableExpression) {
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
            return;
          }
        }
        base.RewriteChildren(addressableExpression);
      }

      /// <summary>
      /// TODO: This is necessary only because the base rewriter for things like TargetExpression call
      /// this method and its definition in the base rewriter is to not visit it, but to just return it.
      /// </summary>
      public override object RewriteReference(ILocalDefinition localDefinition) {
        return this.Rewrite(localDefinition);
      }

      public override ILocalDefinition Rewrite(ILocalDefinition localDefinition) {
        if (localDefinition.MethodDefinition != this.moveNext) // might happen to visit an already replaced local
          return localDefinition;
        ILocalDefinition loc;
        if (!this.localTable.TryGetValue(localDefinition, out loc)) {
          loc = new LocalDefinition() {
            MethodDefinition = this.iterator,
            Name = localDefinition.Name,
            Type = localDefinition.Type,
          };
          this.localTable.Add(localDefinition, loc);
        }
        return loc;
      }

      public override void RewriteChildren(ContractElement contractElement) {
        base.RewriteChildren(contractElement);
        var be = contractElement.Condition as IBlockExpression;
        if (be != null) {
          var lb = new LocalBinder(this.host);
          contractElement.Condition = lb.Rewrite(contractElement.Condition);
          if (lb.localDeclarations.Any()) {
            var be2 = new BlockExpression() {
              BlockStatement = new BlockStatement() {
                Statements = lb.localDeclarations,
              },
              Expression = contractElement.Condition,
            };
            contractElement.Condition = be2;
          }
        }
      }
    }

    internal class HermansAlwaysRight : CodeRewriter {

      ContractExtractor extractor;
      IContractAwareHost contractAwareHost;
      ISourceMethodBody sourceMethodBody;
      private bool methodIsInReferenceAssembly;
      OldAndResultExtractor oldAndResultExtractor;
      private PdbReader pdbReader;

      private HermansAlwaysRight(IContractAwareHost contractAwareHost, ContractExtractor extractor, ISourceMethodBody sourceMethodBody, bool methodIsInReferenceAssembly, OldAndResultExtractor oldAndResultExtractor, PdbReader/*?*/ pdbReader)
        : base(contractAwareHost)
      {
        this.contractAwareHost = contractAwareHost;
        this.extractor = extractor;
        this.sourceMethodBody = sourceMethodBody;
        this.methodIsInReferenceAssembly = methodIsInReferenceAssembly;
        this.oldAndResultExtractor = oldAndResultExtractor;
        this.pdbReader = pdbReader;
      }

      public static MethodContract/*?*/ ExtractContracts(IContractAwareHost contractAwareHost, PdbReader/*?*/ pdbReader, ContractExtractor extractor, ISourceMethodBody methodBody) {
        var definingUnit = TypeHelper.GetDefiningUnit(methodBody.MethodDefinition.ContainingType.ResolvedType);
        var methodIsInReferenceAssembly = ContractHelper.IsContractReferenceAssembly(contractAwareHost, definingUnit);
        var oldAndResultExtractor = new OldAndResultExtractor(contractAwareHost, methodBody, extractor.IsContractMethod);
        var localsInitializedWithFields = FindLocals.FindSetOfLocals(methodBody);

        var har = new HermansAlwaysRight(contractAwareHost, extractor, methodBody, methodIsInReferenceAssembly, oldAndResultExtractor, pdbReader);
        har.Rewrite(methodBody);

        if (har.extractor.currentMethodContract == null) return null;

        // The decompiler will have introduced locals if there were any anonymous delegates in the contracts
        // Such locals are initialized with the fields of the iterator class.
        // The contract that comes back from here will have those fields replaced with whatever the iterator captured
        // (parameters, locals). So the locals in the contract need to be replaced with the iterator fields so that
        // next replacement will see the right thing (i.e., the fields) and replace them! Phew!
        var localReplacer = new LocalReplacer(contractAwareHost, localsInitializedWithFields);
        localReplacer.Rewrite(har.extractor.currentMethodContract);
        // also need to rewrite the remainder of the body
        localReplacer.Rewrite(methodBody);

        return har.extractor.currentMethodContract;
      }

      /// <summary>
      /// There might be more than one block in the iterator's MoveNext method.
      /// So let the base rewriter navigate down into the list of statements unless
      /// this is the one containing the contracts.
      /// This method assumes that a single contract does *not* span multiple
      /// blocks, but is fully contained within a single list of statements.
      /// </summary>
      public override List<IStatement> Rewrite(List<IStatement> statements) {

        if (this.extractor.IndexOfLastContractCall(statements) == -1)
          return base.Rewrite(statements);

        // walk the list until the labeled statement is found that has the contracts in it
        var n = statements.Count;
        var indexOfContract = FindNextContractStatement(statements, 0);
        if (indexOfContract == -1) return base.Rewrite(statements);
        // back up to the beginning of the switch-case in which the contracts sit
        var i = indexOfContract-1;
        while (0 <= i && !(statements[i] is ILabeledStatement)) i--;
        if (0 <= i) {
          i += 2; // skip the label and the update to the state machine's state variable
        } else {
          // the body was not decompiled into a switch statement, but an if-then-else
          // so just start at the statement containing the method call to a contract method
          // and hope that the decompiler was good enough to make this statement be self-contained.
          i = indexOfContract;
        }
        while (indexOfContract != -1) {
          var clump = new List<IStatement>();
          for (int j = i; j <= indexOfContract; j++) {
            clump.Add(statements[j]);
            statements[j] = CodeDummy.LabeledStatement;
          }
          this.extractor.ExtractContract(clump);
          // reset state
          i = indexOfContract + 1;
          indexOfContract = FindNextContractStatement(statements, i);
        }
        return statements;
      }

      private int FindNextContractStatement(List<IStatement> statements, int i) {
        var n = statements.Count;
        for (; i < n; i++) {
          var s = statements[i];
          if (this.extractor.IsPrePostEndOrLegacy(s, true)) return i;
        }
        return -1;
      }

      private class FindLocals : CodeTraverser {
        Dictionary<ILocalDefinition, IBoundExpression> localsToInitializers = new Dictionary<ILocalDefinition, IBoundExpression>();
        public static Dictionary<ILocalDefinition, IBoundExpression> FindSetOfLocals(ISourceMethodBody s) {
          var fl = new FindLocals();
          fl.Traverse(s);
          return fl.localsToInitializers;
        }
        private FindLocals() {
        }
        public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
          var be = localDeclarationStatement.InitialValue as IBoundExpression;
          if (be != null) {
            this.localsToInitializers.Add(localDeclarationStatement.LocalVariable, be);
          }
        }
        public override void TraverseChildren(IAssignment assignment) {
          var loc = assignment.Target.Definition as ILocalDefinition;
          if (loc != null) {
            var be = assignment.Source as IBoundExpression;
            if (be != null)
              this.localsToInitializers.Add(loc, be);
          }
          base.TraverseChildren(assignment);
        }
      }

      private class LocalReplacer : CodeAndContractRewriter {
        private Dictionary<ILocalDefinition, IBoundExpression> table;
        public LocalReplacer(IMetadataHost host, Dictionary<ILocalDefinition, IBoundExpression> table)
          : base(host) {
            this.table = table;
        }
        public override IExpression Rewrite(IBoundExpression boundExpression) {
          var loc = boundExpression.Definition as ILocalDefinition;
          if (loc != null) {
            IBoundExpression be;
            if (this.table.TryGetValue(loc, out be))
              return be;
          }
          return base.Rewrite(boundExpression);
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
  internal class GenericMethodParameterMapper : CodeAndContractRewriter {

    Dictionary<uint, ITypeReference> map = new Dictionary<uint, ITypeReference>();

    /// <summary>
    /// The original generic method in which the anonymous delegate is being re-created.
    /// </summary>
    readonly IMethodDefinition targetMethod;
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
      : base(host, true) {
      this.targetMethod = targetMethod;
      this.targetMethodGenericParameters = new List<IGenericMethodParameter>(targetMethod.GenericParameters);
    }

    public override ITypeReference Rewrite(IGenericMethodParameterReference genericMethodParameterReference) {
      if (this.sourceMethod != null && MemberHelper.MethodsAreEquivalent(genericMethodParameterReference.DefiningMethod.ResolvedMethod, this.sourceMethod))
        return this.targetMethodGenericParameters[genericMethodParameterReference.Index];
      return genericMethodParameterReference;
    }
    public override ITypeReference Rewrite(IGenericTypeParameterReference genericTypeParameterReference) {
      if (this.sourceType != null && TypeHelper.TypesAreEquivalent(genericTypeParameterReference.DefiningType, this.sourceType))
        return this.targetMethodGenericParameters[genericTypeParameterReference.Index];
      return genericTypeParameterReference;
    }

    /// <summary>
    /// This is necessary only because the base rewriter for things like TargetExpression call
    /// this method and its definition in the base rewriter is to not visit it, but to just return it.
    /// </summary>
    public override object RewriteReference(ILocalDefinition localDefinition) {
      return this.Rewrite(localDefinition);
    }


  }

}
