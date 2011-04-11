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
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics;

namespace Microsoft.Cci.ILToCodeModel {
  /// <summary>
  /// Given a delegate that processes the element of a list at a given position, this traverser
  /// goes through a list of statements, which could be basic blocks, and processes the element in linear 
  /// order, that is, as if the list is flattened. 
  /// </summary>
  internal abstract class LinearCodeTraverser : CodeTraverser {
    /// <summary>
    /// One step of processing the elements in a list in LinearCoderTraverser. Such a step, in addition to
    /// processing the element at the <paramref name="index"/> location in <paramref name="list"/>
    /// may move the index to reflect adding elements to or removing elements from the list 
    /// and may indicate whether whole list processing of the traverser should stop. 
    /// </summary>
    /// <param name="list">List of elements</param>
    /// <param name="index">In: position of element to be processed; out: position of the last processed element. </param>
    internal abstract void Process(List<IStatement> list, int index);

    /// <summary>
    /// Reset the stopLinearTraversal flag for the traverser to run again. 
    /// </summary>
    internal virtual void Reset() {
      this.stopLinearTraversal = false;
    }

    /// <summary>
    /// A flag that stops linear Traversal. When set to true, the linear traversal stops.  
    /// </summary>
    protected bool stopLinearTraversal = false;

    /// <summary>
    /// Visit every statement in the block, including those in the nested blocks. 
    /// </summary>
    /// <param name="block">block of statements, which themselves may be blocks.</param>
    public override void TraverseChildren(IBlockStatement block) {
      if (this.stopLinearTraversal) return;
      BlockStatement blockStatement = (BlockStatement)block;
      for (int i = 0; i < blockStatement.Statements.Count; i++) {
        var statement = blockStatement.Statements[i];
        base.Traverse(statement);
        if (this.stopLinearTraversal) return;
        Process(blockStatement.Statements, i);
        if (this.stopLinearTraversal) return;
      }
    }
  }

  /// <summary>
  /// Decompilation of a MoveNext method into an iterator method. The structure of 
  /// a MoveNext method may contain:
  /// 1) An optional try catch block, if the source iterator method has, for example, an foreach statement.
  ///    We need to get rid of this try statement and decompile it back to foreach. Currently, we only remove
  ///    this try statement.
  /// 2) The body of the MoveNext is a switch statement testing the state field of the closure class.
  ///    The state may be:
  ///    a) An initial state, the handling of which contains most of the logic from the original
  ///    iterator method. This part we are going to keep.
  ///    b) One of the continuing states, the handling of which simply go to (may be through a series of 
  ///    gotos) a label inside the handling of the initial state. (The label follows a location where a yield
  ///    return should be.)
  ///    c) A default (finishing) state, in which the control goes to a return false by the MoveNext.
  ///    We need to remove the switch statement, the goto chains for the continuing state, and code to handle 
  ///    finishing states. 
  /// 3) Assignments to the current field of the closure, which is followed by a return true. These correspond 
  ///    to a yield return. We should replace such sequence with yield return. 
  /// 4) Return false, which should be replaced by yield break.
  /// 5) Any other accesses of iterator class internals, whcih should be removed. These include calls to m__finally, assignments
  ///    to state, current, and other fields that do not capture a variable in the iterator method.
  /// 6) References to closure fields that captures a local or a parameter (including self), which ought to be replaced by 
  ///    the captured variable.
  /// 7) References to generic type parameter(s) of the closure class. These should be replaced by corresponding generic
  ///    method parameters of the iterator method. 
  /// </summary>
  internal class MoveNextDecompiler {
    /// <summary>
    /// The containing type of the MoveNext method, that is, the closure class. May be specialized.
    /// </summary>
    internal ITypeDefinition containingType;

    /// <summary>
    /// Unspecialized version of the containing type is used often. Keep a cached value here. 
    /// </summary>
    internal ITypeReference unspecializedContainingType;

    IMetadataHost host;

    internal ILocalDefinition stateLocal;
    internal Dictionary<ILocalDefinition, bool> returnLocals;

    /// <summary>
    /// Decompile of a MoveNext method body and transform the result into the body of the original iterator method. 
    /// This involves the removal of the state machine, handling of possible compiled code for for-each, introducing
    /// yield returns and yield breaks, and tranforming the code to the context of the iterator method. 
    /// </summary>
    internal MoveNextDecompiler(ITypeDefinition iteratorClosureType, IMetadataHost host) {
      this.host = host;
      this.containingType = iteratorClosureType;
      this.unspecializedContainingType = UnspecializedMethods.AsUnspecializedTypeReference(this.containingType);
    }

    internal const string stateFieldPostfix = "__state";
    internal const string closureFieldPrefix = "<>";
    internal const string currentFieldPostfix = "__current";
    internal const string thisFieldPostfix = "__this";
    internal const string initialThreadIdPostfix = "__initialThreadId";

    /// <summary>
    /// Entry point of the decompiler: 
    /// 1) Remove a TryCatchFinally statement in the MoveNext, if there is one.
    /// 2) Remove the state machine, including control flow that tests states, returns from
    /// a state and reenters in another state.
    /// 3) Replace return statements with approppriate yield return or yield breaks.
    /// </summary>
    internal IBlockStatement Decompile(BlockStatement block) {
      RemovePossibleTryCatchBlock(block);
      this.RemoveAssignmentsFromOrToThisDotState(block);
      this.ReplaceReturnWithYield(block);
      this.RemoveToplevelSwitch(block);
      this.RemoveCompilerIntroducedLocals(block);
      this.RemoveDummyStatements(block);
      return block;
    }

    /// <summary>
    /// If the body of MoveNext has a try/catch block at the beginning inserted by compiler and the catch block contains
    /// a call to call this (the closure class).dispose, remove the try block. Also remove all the calls to m__finally. 
    /// </summary>
    private static void RemovePossibleTryCatchBlock(BlockStatement blockStatement) {
      // Remove the try catch block
      var tryCatchRemover = new RemovePossibleTryCatchBlock();
      tryCatchRemover.Traverse(blockStatement);
    }

    /// <summary>
    /// Remove any assignment to <code>this.&lt;&gt;?_state</code> and collect all the local variables that hold the
    /// value of the state field of the closure instance. 
    /// </summary>
    /// <param name="blockStatement">A BlockStatement representing the body of MoveNext.</param>
    private void RemoveAssignmentsFromOrToThisDotState(BlockStatement blockStatement) {
      var Remover = new RemoveStateFieldAccessAndMFinallyCall(this);
      Remover.Traverse(blockStatement);
      this.stateLocal = Remover.thisDotStateLocal;
    }

    /// <summary>
    /// Replace every return true with yield return, and every return false with yield break. 
    /// </summary>
    private void ReplaceReturnWithYield(BlockStatement blockStatement) {
      // First pass, replace this.<>?__current = exp with "yield return exp";
      //             remove return x. Side effect: remember the label before return and the local variable corresponding to return value.
      //             which are useful in inserting yield breaks. 
      var yieldInserter = new ProcessCurrentAndReturn(this);
      yieldInserter.Traverse(blockStatement);
      this.returnLocals = yieldInserter.returnLocals;

      // Second pass: remove goto statement to the label before return statement
      //              remove assignment to locals that holds the return value and replace it with yield break if source of 
      //              the assignment is false.
      var yieldBreakInserter = new YieldBreakInserter(this, yieldInserter.returnLocals, yieldInserter.labelBeforeReturn);
      yieldBreakInserter.Traverse(blockStatement);
    }

    /// <summary>
    /// Remove the switch statement, if any, on <code>this.&lt;&gt;?_state</code>
    /// </summary>
    private void RemoveToplevelSwitch(BlockStatement blockStatement) {
      // First: collect the goto statements in the switch case bodies.
      List<IGotoStatement> gotosFromSwitchCases = new List<IGotoStatement>();
      var pass1 = new RemoveSwitchAndCollectGotos(this.stateLocal, gotosFromSwitchCases);
      pass1.Traverse(blockStatement);
      // Second, follow the link from gotos in switch cases, find and remove the gotoes whose targets are either
      // the starting point of the iterator method, a yield break, or a label that is internal to the state machine
      // the MoveNext.
      var gotoRemover = new RemoveGotoInIndirection();
      int startingPointsLength = gotosFromSwitchCases.Count;
      for (int i = 0; i < startingPointsLength; i++) {
        IGotoStatement gotoStatement = gotosFromSwitchCases[i] as IGotoStatement;
        if (gotoStatement != null) {
          gotoRemover.RemoveGotoStatements(blockStatement, gotoStatement);
        }
      }
    }

    private void RemoveCompilerIntroducedLocals(BlockStatement block) {
      var remover = new RemoveCompilerIntroducedLocals(this);
      remover.Traverse(block);
    }

    /// <summary>
    /// Remove any statements that were marked as dummy.
    /// </summary>
    /// <param name="block"></param>
    private void RemoveDummyStatements(BlockStatement block) {
      var remover = new RemoveDummyStatements();
      remover.Traverse(block);
    }

    /// <summary>
    /// Test whether a field is the state field of the iterator closure.
    /// </summary>
    internal bool IsClosureStateField(IFieldReference closureField) {
      if (this.closureStateField == null) {
        ITypeReference closure = UnspecializedMethods.AsUnspecializedTypeReference(closureField.ContainingType);
        if (!TypeHelper.TypesAreEquivalent(closure, this.unspecializedContainingType))
          return false;
        if (closureField == null || !closureField.Name.Value.StartsWith(closureFieldPrefix, System.StringComparison.Ordinal) || 
          !closureField.Name.Value.EndsWith(stateFieldPostfix, System.StringComparison.Ordinal)) 
          return false;
        this.closureStateField = closureField;
        return true;
      }
      if (closureField.InternedKey == this.closureStateField.InternedKey) return true;
      return false;
    }
    IFieldReference/*?*/ closureStateField;

    /// <summary>
    /// Test whether a field is the current field of the iterator closure.
    /// </summary>
    internal bool IsClosureCurrentField(IFieldReference closureField) {
      if (this.closureCurrentField == null) {
        ITypeReference closure = UnspecializedMethods.AsUnspecializedTypeReference(closureField.ContainingType);
        if (!TypeHelper.TypesAreEquivalent(closure, this.unspecializedContainingType)) return false;
        if (closureField == null || !closureField.Name.Value.StartsWith(closureFieldPrefix, System.StringComparison.Ordinal) ||
          !closureField.Name.Value.EndsWith(currentFieldPostfix, System.StringComparison.Ordinal))
          return false;
        this.closureCurrentField = closureField;
        return true;
      }
      return this.closureCurrentField.InternedKey == closureField.InternedKey;
    }
    IFieldReference/*?*/ closureCurrentField;

    /// <summary>
    /// Test whether a constant is zero (as an int) or false (as a boolean). Return false for anything else. 
    /// </summary>
    internal bool IsZeroConstant(IExpression expression) {
      ICompileTimeConstant constant = expression as ICompileTimeConstant;
      if (constant == null) return false;
      var typeCode = constant.Type.TypeCode;
      if (typeCode == PrimitiveTypeCode.Boolean) return !(bool)constant.Value;
      if (typeCode == PrimitiveTypeCode.Int32) return ((int)constant.Value == 0);
      return false;
    }
  }

  /// <summary>
  /// An iterator method is compiled to a closure class with a MoveNext method. This class 
  /// substitutes every occurrance of a iterator closure type parameter with a method generic parameter and every reference 
  /// to this.field to the locals or parameters captured by the closure field.
  /// </summary>
  internal class MoveNextToIteratorBlockTransformer : MethodBodyMappingMutator {
    /// <summary>
    /// A mapping from a closure field (a unique key of its name) to the local or parameter it captures.
    /// </summary>
    private Dictionary<int, object>/*!*/ fieldMapping;
    /// <summary>
    /// A mapping from a type parameter of the closure class to a generic method parameter of the iterator method.
    /// </summary>
    private Dictionary<IGenericTypeParameter, IGenericMethodParameter> typeParameterMapping;
    /// <summary>
    /// Cached unspecialized version of the iterator closure. 
    /// </summary>
    private ITypeReference unspecializedClosureType;

    /// <summary>
    /// An iterator method is compiled to a closure class with a MoveNext method. This class 
    /// substitutes every occurrance of a type parameter with a method generic parameter and every reference 
    /// to this.field to the locals or parameters captured by the closure field.
    /// </summary>
    private MoveNextToIteratorBlockTransformer(Dictionary<IGenericTypeParameter, IGenericMethodParameter> typeParameterMapping, Dictionary<int, object> fieldMapping, IMetadataHost host, ITypeReference closureType)
      : base(host) {
      this.fieldMapping = fieldMapping;
      this.typeParameterMapping = typeParameterMapping;
      this.unspecializedClosureType = UnspecializedMethods.AsUnspecializedTypeReference(closureType);
    }

    /// <summary>
    /// Given method definitions of the original iterator method and the MoveNext Method, and the decompiled body of MoveNext, performs 
    /// the necessary substitution so that the method body becomes a legitimate, decompiled body of the iterator method.
    /// 
    /// "decompiled MoveNext body" refers to the method body that has it state machine and TryCatchFinallyBlock removed, but references
    /// to closure fields remain. 
    /// </summary>
    internal static IBlockStatement Transform(IMethodDefinition iteratorMethod, IMethodDefinition moveNextMethod, BlockStatement decompiledMoveNextBody, IMetadataHost host) {
      var fieldMapping = GetMapping(iteratorMethod, moveNextMethod);
      var typeParameterMapping = GetGenericParameterMapping(iteratorMethod, moveNextMethod.ContainingTypeDefinition);
      var transformer = new MoveNextToIteratorBlockTransformer(typeParameterMapping, fieldMapping, host, moveNextMethod.ContainingType);
      return transformer.Visit(decompiledMoveNextBody);
    }

    /// <summary>
    /// Whether (the unspecialized version of) a type reference is equivalent to the (unspecialized version of) closure class. 
    /// </summary>
    private bool IsClosureType(ITypeReference typeReference) {
      typeReference = UnspecializedMethods.AsUnspecializedTypeReference(typeReference);
      return TypeHelper.TypesAreEquivalent(typeReference, this.unspecializedClosureType);
    }

    /// <summary>
    /// Replace reference to this.field by reference to corresponding local or parameter.
    /// </summary>
    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      var closureFieldRef = addressableExpression.Definition as IFieldReference;
      if (closureFieldRef != null && IsClosureType(closureFieldRef.ContainingType)) {
        object localOrParameter = null;
        if (this.fieldMapping.TryGetValue(closureFieldRef.Name.UniqueKey, out localOrParameter)) {
          addressableExpression.Definition = localOrParameter;
          addressableExpression.Instance = null;
          return addressableExpression;
        }
      }
      return base.Visit(addressableExpression);
    }

    /// <summary>
    /// Replace the field binding in a target expression with an approppriate local or parameter.
    /// </summary>
    public override ITargetExpression Visit(TargetExpression targetExpression) {
      // ^Requires: targetExpression.Definition is not the field in the closure class that captures THIS of the original.
      var closureFieldRef = targetExpression.Definition as IFieldReference;
      if (closureFieldRef != null && IsClosureType(closureFieldRef.ContainingType)) {
        object localOrParameter = null;
        if (this.fieldMapping.TryGetValue(closureFieldRef.Name.UniqueKey, out localOrParameter)) {
          targetExpression.Definition = localOrParameter;
          targetExpression.Instance = null;
          return targetExpression;
        }
      }
      return base.Visit(targetExpression);
    }

    /// <summary>
    /// Replace the field binding in a bound expression with an approppriate local or parameter.
    /// If the field is _this that captures this, replace the whole bound expression with the self
    /// of the original method. 
    /// </summary>
    public override IExpression Visit(BoundExpression boundExpression) {
      var closureFieldRef = boundExpression.Definition as IFieldReference;
      if (closureFieldRef != null && IsClosureType(closureFieldRef.ContainingType)) {
        object/*?*/ localOrParameter = null;
        if (this.fieldMapping.TryGetValue(closureFieldRef.Name.UniqueKey, out localOrParameter)) {
          var thisReference = localOrParameter as IThisReference;
          if (thisReference != null)
            return thisReference;
          LocalDefinition ld = localOrParameter as LocalDefinition;
          if (ld != null) {
            // When the locals were created, they were given the type of the corresponding field.
            // That type might contain references to generic type parameters of the MoveNext method's
            // containing type and they also need to get replaced with generic method type parameters.
            ld.Type = this.Visit(ld.Type);
          }
          boundExpression.Definition = localOrParameter;
          boundExpression.Instance = null;
          return boundExpression;
        }
      }
      return base.Visit(boundExpression);
    }

    /// <summary>
    /// Replace type paramter with generic method parameter.
    /// </summary>
    public override ITypeReference Visit(ITypeReference typeReference) {
      var genericTypeParameterReference = typeReference as IGenericTypeParameterReference;
      if (genericTypeParameterReference != null) {
        var genericTypeParameter = genericTypeParameterReference.ResolvedType;
        if (this.typeParameterMapping.ContainsKey(genericTypeParameter)) {
          return this.typeParameterMapping[genericTypeParameter];
        }
      }
      return base.Visit(typeReference);
    }

    /// <summary>
    /// Get the mapping between the type parameters of the closure class and the generic method parameters of 
    /// the iterator methods, if any. The two have the same number of generic parameters.
    /// </summary>
    private static Dictionary<IGenericTypeParameter, IGenericMethodParameter> GetGenericParameterMapping(IMethodDefinition iteratorMethod, ITypeDefinition closureType) {
      var result = new Dictionary<IGenericTypeParameter, IGenericMethodParameter>();
      if (!iteratorMethod.IsGeneric) return result;
      var itor1 = iteratorMethod.GenericParameters.GetEnumerator();
      var itor2 = closureType.GenericParameters.GetEnumerator();
      while (itor1.MoveNext() && itor2.MoveNext()) {
        IGenericMethodParameter genericMethodParameter = itor1.Current;
        IGenericTypeParameter genericTypeParameter = itor2.Current;
        result.Add(genericTypeParameter, genericMethodParameter);
      }
      return result;
    }

    /// <summary>
    /// The mapping from closure fields (their names) to the locals or parameters captured by the field. 
    /// </summary>
    private static Dictionary<int, object> GetMapping(IMethodDefinition originalMethodDefinition, IMethodDefinition moveNextMethod) {
      var result = new Dictionary<int, object>();
      ITypeDefinition closureType = moveNextMethod.ContainingTypeDefinition;
      bool seenStateField = false, seenCurrentField = false, seenInitialThreadIdField = false, seenThisField = false;
      // Go over the fields of the closure class, analyze its naming pattern to decide which parameter or local variable
      // it captures. Remember this capturing relation in the result. 
      foreach (IFieldDefinition field in closureType.Fields) {
        string fieldName = field.Name.Value;
        bool parameterMatched = false;
        foreach (var parameter in originalMethodDefinition.Parameters) {
          if (field.Name == parameter.Name) {
            result[field.Name.UniqueKey] = parameter;
            parameterMatched = true;
            break;
          } else {
            if (fieldName.Contains("<") && fieldName.EndsWith(parameter.Name.Value, System.StringComparison.Ordinal)) {
              int length = fieldName.Length;
              int parameterNameLength = parameter.Name.Value.Length;
              if (length >= parameterNameLength + 2 && (fieldName[length - parameterNameLength - 2] == '_' && fieldName[length - parameterNameLength - 1] == '_')) {
                result[field.Name.UniqueKey] = parameter;
                break;
              }
            }
          }
        }
        if (parameterMatched) continue;
        if (!seenStateField && fieldName.StartsWith(MoveNextDecompiler.closureFieldPrefix, System.StringComparison.Ordinal) && fieldName.EndsWith(MoveNextDecompiler.stateFieldPostfix, System.StringComparison.Ordinal)) {
          seenStateField = true;
          continue;
        }
        if (!seenCurrentField && fieldName.StartsWith(MoveNextDecompiler.closureFieldPrefix, System.StringComparison.Ordinal) && fieldName.EndsWith(MoveNextDecompiler.currentFieldPostfix, System.StringComparison.Ordinal)) {
          seenCurrentField = true;
          continue;
        }
        if (!seenInitialThreadIdField && fieldName.StartsWith(MoveNextDecompiler.closureFieldPrefix, System.StringComparison.Ordinal) && fieldName.EndsWith(MoveNextDecompiler.initialThreadIdPostfix, System.StringComparison.Ordinal)) {
          seenInitialThreadIdField = true;
          continue;
        }
        if (!seenThisField && fieldName.StartsWith(MoveNextDecompiler.closureFieldPrefix, System.StringComparison.Ordinal) && fieldName.EndsWith(MoveNextDecompiler.thisFieldPostfix, System.StringComparison.Ordinal)) {
          result[field.Name.UniqueKey] = new ThisReference() { Type = field.Type }; seenThisField = true;
          continue;
        }
        LocalDefinition newLocal = new LocalDefinition() { Name = field.Name, Type = field.Type };
        result[field.Name.UniqueKey] = newLocal;
      }
      return result;
    }
  }

  /// <summary>
  /// Turn an assignment of create object instance into a local declaration so that closure decompilation can pick it up. 
  /// </summary>
  internal class ClosureLocalVariableDeclarationAdder : MethodBodyCodeMutator {
    private Dictionary<ILocalDefinition, bool> locals = new Dictionary<ILocalDefinition, bool>();

    /// <summary>
    /// Turn an assignment of create object instance into a local declaration so that closure decompilation can pick it up. 
    /// </summary>
    internal ClosureLocalVariableDeclarationAdder(MoveNextSourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host, true) {
    }
    /// <summary>
    /// Before visiting subnodes, add local declarations to the assignment to a local that creates a display class object. 
    /// </summary>
    public override IBlockStatement Visit(BlockStatement blockStatement) {
      this.AddLocalDeclarationIfNecessary(blockStatement.Statements);
      return blockStatement;
    }
    /// <summary>
    /// Add a local declaration to the assignment to a local that creates a display class objects.
    /// </summary>
    void AddLocalDeclarationIfNecessary(List<IStatement> statements) {
      for (int i = 0; i < statements.Count; i++) {
        base.Visit(statements[i]);
        var localDeclaration = statements[i] as ILocalDeclarationStatement;
        if (localDeclaration != null) {
          if (!this.locals.ContainsKey(localDeclaration.LocalVariable))
            this.locals.Add(localDeclaration.LocalVariable, localDeclaration.InitialValue != null);
          continue;
        }
        var expressionStatement = statements[i] as IExpressionStatement;
        if (expressionStatement != null) {
          var assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            var localDefinition = assignment.Target.Definition as ILocalDefinition;
            if (localDefinition != null && (!this.locals.ContainsKey(localDefinition))) {
              var localDeclarationStatement = new LocalDeclarationStatement() {
                LocalVariable = localDefinition,
                InitialValue = assignment.Source
              };
              localDeclarationStatement.Locations.AddRange(expressionStatement.Locations);
              this.locals[localDefinition] = true;
              statements[i] = localDeclarationStatement;
            }
            continue;
          }
        }
      }
    }

    /// <summary>
    /// Test to see if the type reference is to a compiler generated class. 
    /// </summary>
    private bool IsCompilerGeneratedClass(ITypeReference typeReference) {
      typeReference = UnspecializedMethods.AsUnspecializedTypeReference(typeReference);
      ITypeDefinition typeDefinition = typeReference.ResolvedType;
      if (AttributeHelper.Contains(typeDefinition.Attributes, this.host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return true;
      return false;
    }
  }

  /// <summary>
  /// Helper class to remove try-catch block generated by foreach statements
  /// in iterators.
  /// Inlines (recursively) all try blocks and throws away any catch blocks
  /// and finally/fault blocks. (There shouldn't be any catch blocks when
  /// the code is generated by a compiler to implement an iterator.)
  /// TODO: Decompile foreach statement
  /// </summary>
  internal sealed class RemovePossibleTryCatchBlock : CodeTraverser {

    /// <summary>
    /// Inlines (recursively) all try blocks and throws away any catch blocks
    /// and finally/fault blocks. (There shouldn't be any catch blocks when
    /// the code is generated by a compiler to implement an iterator.)
    /// </summary>
    public override void TraverseChildren(IBlockStatement block) {
      BlockStatement blockStatement = (BlockStatement)block;
      var newStmts = new List<IStatement>();
      var statements = blockStatement.Statements;
      for (int i = 0; i < statements.Count; i++) {
        base.Traverse(statements[i]);
        var tryCatchFinallyStatement = statements[i] as ITryCatchFinallyStatement;
        if (tryCatchFinallyStatement != null) {
          newStmts.AddRange(tryCatchFinallyStatement.TryBody.Statements);
          continue;
        }
        newStmts.Add(statements[i]);
      }
      blockStatement.Statements = newStmts;
    }
  }

  /// <summary>
  /// Helper class that:
  /// 1) Removes assignments to this.&lt;&gt;__state,
  /// 2) Collect local variables that hold the value of this.&lt;&gt;__state.
  /// 3) Removes calls to the m__finally method.
  /// </summary>
  internal sealed class RemoveStateFieldAccessAndMFinallyCall : CodeTraverser {
    /// <summary>
    /// The local variable that holds the value of the state field of the iterator closure instance (self in movenext).
    /// We assume there is only one such local variable. 
    /// </summary>
    internal ILocalDefinition thisDotStateLocal;
    private MoveNextDecompiler decompiler;

    /// <summary>
    /// Helper class that removes assignments to this.&lt;&gt;__state. 
    /// </summary>
    internal RemoveStateFieldAccessAndMFinallyCall(MoveNextDecompiler decompiler) {
      this.decompiler = decompiler;
    }

    /// <summary>
    /// 1) Removes assignments to this.&lt;&gt;__state,
    /// 2) Collect local variables that hold the value of this.&lt;&gt;__state.
    /// 3) Removes calls to the m__finally method.
    /// </summary>
    public override void TraverseChildren(IBlockStatement block) {
      var statements = ((BlockStatement)block).Statements;
      for (int i = 0; i < statements.Count; i++) {
        var statement = statements[i];
        this.Traverse(statement);
        var expressionStatement = statement as IExpressionStatement;
        if (expressionStatement != null) {
          var assignment = expressionStatement.Expression as IAssignment;
          if (assignment != null) {
            var closureField = assignment.Target.Definition as IFieldReference;
            if (closureField != null && decompiler.IsClosureStateField(closureField)) {
              statements[i] = CodeDummy.LabeledStatement;
              continue;
            }
            var boundExpression = assignment.Source as IBoundExpression;
            if (boundExpression != null) {
              closureField = boundExpression.Definition as IFieldReference;
              if (closureField != null && this.decompiler.IsClosureStateField(closureField)) {
                var stateLocal = assignment.Target.Definition as ILocalDefinition;
                if (assignment.Target.Instance == null && stateLocal != null) {
                  if (this.thisDotStateLocal == null) {
                    this.thisDotStateLocal = stateLocal;
                  } else {
                    Debug.Assert(this.thisDotStateLocal.Equals(stateLocal), "Assumption that there is only one local for this dot state in MoveNext is wrong!");
                  }
                  statements[i] = CodeDummy.LabeledStatement;
                  continue;
                } else continue; // assign thisDotState to a non-local, shouldnt happen from csc generated code. 
              }
            }
            if (assignment.Source is IThisReference) {
              Debug.Assert(assignment.Target.Definition is TempVariable);
              //The this value of the MoveNext is being placed in a temp variable as a result of Unstacker
              //It must be also be deleted since later references to the "this" of MoveNext will happen in field accesses that become locals.
              statements[i] = CodeDummy.LabeledStatement;
            }
          }
          var methodCall = expressionStatement.Expression as IMethodCall;
          if (methodCall != null) {
            if (methodCall.MethodToCall.Name.Value.Contains("m__Finally") && methodCall.ThisArgument is IThisReference) {
              statements[i] = CodeDummy.LabeledStatement;
              continue;
            }
          }
        }
      }
    }

    public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      var boundExpression = localDeclarationStatement.InitialValue as IBoundExpression;
      if (boundExpression != null) {
        var closureField = boundExpression.Definition as IFieldReference;
        if (closureField != null && this.decompiler.IsClosureStateField(closureField)) {
          this.thisDotStateLocal = localDeclarationStatement.LocalVariable;
        }
      }
    }
  }

  /// <summary>
  /// Helper class to:
  /// 1) replace assignments to the current field with yield returns;
  /// 2) replace return 0 with yield break;
  /// 3) remember the labels right before return statements;
  /// 4) remember the locals that hold the return value.
  /// </summary>
  internal sealed class ProcessCurrentAndReturn : LinearCodeTraverser {
    MoveNextDecompiler decompiler;

    /// <summary>
    /// Helper class to:
    /// 1) replace assignments to the current field with yield returns;
    /// 2) replace return 0 with yield break;
    /// 3) remember the labels right before return statements;
    /// 4) remember the locals that hold the return value.
    /// </summary>
    internal ProcessCurrentAndReturn(MoveNextDecompiler decompiler) {
      this.decompiler = decompiler;
    }

    /// <summary>
    /// Whether a statement is an assignment to the current field of the closure class. If so, out 
    /// parameter <paramref name="expression"/> is set to the source of the assignment. 
    /// </summary>
    bool IsAssignmentToThisDotCurrent(IStatement/*?*/ statement, out IExpression/*?*/ expression) {
      expression = null;
      var expressionStatement = statement as IExpressionStatement;
      if (expressionStatement == null)
        return false;
      var assignment = expressionStatement.Expression as IAssignment;
      if (assignment == null)
        return false;
      var closureField = assignment.Target.Definition as IFieldReference;
      if (closureField == null || !this.decompiler.IsClosureCurrentField(closureField))
        return false;
      expression = assignment.Source;
      return true;
    }

    /// <summary>
    /// Remember the most recently seen labeled statement. We assume there is a label before a return 
    /// statement. 
    /// </summary>
    private ILabeledStatement/*?*/ currentLabeledStatement;

    /// <summary>
    /// Locals that holds the return value(s) of the method. This piece of information is later useful 
    /// in yield break inserter. 
    /// </summary>
    internal Dictionary<ILocalDefinition, bool> returnLocals = new Dictionary<ILocalDefinition, bool>();
    /// <summary>
    /// Labels right before the return statement. Later on, this is used to match patterns like: 
    /// local:= true; goto L; ... L: return local;
    /// </summary>
    internal Dictionary<int, bool> labelBeforeReturn = new Dictionary<int, bool>();

    /// <summary>
    /// 1) replace assignments to the current field with yield returns;
    /// 2) replace return 0 with yield break;
    /// 3) remember the labels right before return statements;
    /// 4) remember the locals that hold the return value.
    /// </summary>
    internal override void Process(List<IStatement> statements, int i) {
      IExpression exp;
      IStatement statement = statements[i];
      if (this.IsAssignmentToThisDotCurrent(statement, out exp)) {
        var yieldReturnStatement = new YieldReturnStatement();
        yieldReturnStatement.Expression = exp;
        yieldReturnStatement.Locations = ((Statement)statement).Locations;
        statements[i] = yieldReturnStatement;
        this.currentLabeledStatement = null;
        return;
      }
      var returnStatement = statement as IReturnStatement;
      if (returnStatement != null) {
        statements[i] = CodeDummy.LabeledStatement;
        if (this.currentLabeledStatement != null) {
          this.labelBeforeReturn[this.currentLabeledStatement.Label.UniqueKey] = true;
        }
        var boundExpression = returnStatement.Expression as IBoundExpression;
        if (boundExpression != null) {
          var localDefinition = boundExpression.Definition as ILocalDefinition;
          if (localDefinition != null) this.returnLocals[localDefinition] = true;
        } else {
          var ctc = returnStatement.Expression as ICompileTimeConstant;
          if (this.decompiler.IsZeroConstant(ctc)) {
            YieldBreakStatement yieldBreak = new YieldBreakStatement() { };
            // This insertion happens only rarely for non csc generated code. 
            statements.Insert(++i, yieldBreak);
          } else { }// return true, current must have been set, do nothing. 
        }
      }
      var labeledStatement = statement as ILabeledStatement;
      if (labeledStatement != null) {
        this.currentLabeledStatement = labeledStatement;
      } else {
        this.currentLabeledStatement = null;
      }
    }

    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      ;// do nothing: don't visit any children, otherwise return statements within a lambda are removed!
    }
  }

  /// <summary>
  /// Helper class to replace assignments of false to return local with yield break and remove some unnecessary gotos. 
  /// </summary>
  internal class YieldBreakInserter : LinearCodeTraverser {
    MoveNextDecompiler decompiler;
    internal YieldBreakInserter(MoveNextDecompiler decompiler, Dictionary<ILocalDefinition, bool> returnLocals, Dictionary<int, bool> labelBeforeReturn) {
      this.labelBeforeReturn = labelBeforeReturn;
      this.returnLocals = returnLocals;
      this.decompiler = decompiler;
    }
    private Dictionary<int, bool> labelBeforeReturn;
    private Dictionary<ILocalDefinition, bool> returnLocals;

    /// <summary>
    /// Removes the gotoes to the label right before a return because it has become dead code after 
    /// the assignment of false to return local has been replaced by yield break and assignment to current
    /// has been replaced by yield return. 
    /// </summary>
    internal override void Process(List<IStatement> statements, int i) {
      IStatement statement = statements[i];
      var gotoStatement = statement as IGotoStatement;
      if (gotoStatement != null) {
        if (this.labelBeforeReturn.ContainsKey(gotoStatement.TargetStatement.Label.UniqueKey)) {
          statements[i] = CodeDummy.LabeledStatement;
        };
        return;
      }
      var expressionStatement = statement as IExpressionStatement;
      if (expressionStatement != null) {
        var assignment = expressionStatement.Expression as IAssignment;
        if (assignment != null) {
          if (assignment.Target.Instance == null) {
            var localDef = assignment.Target.Definition as ILocalDefinition;
            if (localDef != null) {
              if (this.returnLocals.ContainsKey(localDef)) {
                var ctc = assignment.Source as ICompileTimeConstant;
                Debug.Assert(ctc != null);
                if (this.decompiler.IsZeroConstant(ctc)) {
                  var yieldBreakStatement = new YieldBreakStatement();
                  yieldBreakStatement.Locations.AddRange(expressionStatement.Locations);
                  statements[i] = yieldBreakStatement;
                } else {
                  statements[i] = CodeDummy.LabeledStatement;
                }
              }
            }
          }
        }
      }
      return;
    }

  }

  /// <summary>
  /// Traverse method body to collect gotos in a goto chain left after a switch statement has been deleted. A 
  /// goto chain is formed by indirection introduced by the decompilation of a switch statement. A typical goto-chain is like:
  /// 
  /// case 1: goto L1;
  ///  ...
  /// L1: ;
  /// goto State1Entry;
  ///  ...
  /// // code for yield return;
  /// State1Entry;
  /// 
  /// The end of the goto chain is at either a label like State1Entry, which points to a location after a yield return,
  /// a label that leads to a return, or the entrance of iterator method. All these gotos are introduced during the decompilation
  /// of the swtich statement for the MoveNext the state machine. 
  /// 
  /// </summary>
  internal class RemoveGotoInIndirection : LinearCodeTraverser {
    /// <summary>
    /// Set to a goto target when we hit the target label. 
    /// </summary>
    private IStatement targetOfGotoFromSwitchCase;

    /// <summary>
    /// The current Goto statement for which we need to collect goto(s) in chain.
    /// </summary>
    private IGotoStatement gotoFromSwitchCase;

    /// <summary>
    /// Traverse method body to collect gotos in a goto chain left after a switch statement has been deleted. 
    /// 
    /// Currently we assume there is always one intermediate goto in the goto chain for a continuing state, and none
    /// for the default case.
    /// </summary>
    internal void RemoveGotoStatements(IBlockStatement block, IGotoStatement gotoFromSwitch) {
      this.targetOfGotoFromSwitchCase = null;
      base.Reset();
      this.gotoFromSwitchCase = gotoFromSwitch;
      this.Traverse(block);
    }

    /// <summary>
    /// Given a gotoStatement, collect the next goto (the intermediate goto in the chain) and stop. See pattern
    /// above. Or if the current chain leads to the default case, nothing is collected.
    /// </summary>
    internal override void Process(List<IStatement> statements, int j) {
      var labeledStatement = statements[j] as ILabeledStatement;
      if (labeledStatement != null && labeledStatement != CodeDummy.LabeledStatement) {
        if (labeledStatement.Label.UniqueKey == this.gotoFromSwitchCase.TargetStatement.Label.UniqueKey) {
          this.targetOfGotoFromSwitchCase = labeledStatement;
        }
        return;
      }
      IGotoStatement goStmt = statements[j] as IGotoStatement;
      // If there is a goto right after the previous goto target, we finished one step.
      if (goStmt != null && targetOfGotoFromSwitchCase != null) {
        statements[j] = CodeDummy.LabeledStatement;
        this.gotoFromSwitchCase = null;
        this.stopLinearTraversal = true;
        return;
      }
      if (this.targetOfGotoFromSwitchCase != null) {
        // We have hit the target, but the next statement is not goto nor label. This must be the default case. 
        // We set the gotoStatement to null and finish. 
        this.gotoFromSwitchCase = null;
        this.stopLinearTraversal = true;
        return;
      }
    }
  }

  /// <summary>
  /// Remove test on the state machine state. When such a test is by a switch statement, 
  /// collect goto statements in switch cases and remove the switch statement. It is assumed
  /// that when an iterator method contains only yield breaks, an if statement may be used
  /// in the test. It is always of the form:
  /// 
  /// if (state !=0) {
  /// } else {
  ///   //body of iterator method
  /// }
  /// </summary>
  internal class RemoveSwitchAndCollectGotos : CodeTraverser {
    /// <summary>
    /// Used for detecting whether whether thisdotstate is tested.
    /// </summary>
    private ILocalDefinition localForThisDotState;

    /// <summary>
    /// A list of goto statements from the switch cases. They are starting points of an goto indirection. Both these gotos and the gotos
    /// in the indirection should be removed. 
    /// </summary>
    internal List<IGotoStatement> gotosFromSwitchCases;

    /// <summary>
    /// Collect goto statements in switch cases and remove the switch statement. 
    /// </summary>
    internal RemoveSwitchAndCollectGotos(ILocalDefinition localForThisDotState, List<IGotoStatement> gotosFromSwitchCases) {
      this.localForThisDotState = localForThisDotState;
      this.gotosFromSwitchCases = gotosFromSwitchCases;
    }

    /// <summary>
    /// Collect the gotos in the swiches cases. Visit only the top level block.
    /// </summary>
    public override void TraverseChildren(IBlockStatement block) {
      BlockStatement blockStatement = (BlockStatement)block;
      var statements = blockStatement.Statements;
      var newStatements = new List<IStatement>(statements.Count);
      for (int i = 0, n = statements.Count; i < n; i++) {
        IStatement statement = statements[i];
        var switchStatement = statement as ISwitchStatement;
        if (switchStatement != null) {
          foreach (ISwitchCase casee in switchStatement.Cases) {
            foreach (IStatement st in casee.Body) {
              IGotoStatement gotoStatement = st as IGotoStatement;
              if (gotoStatement != null) {
                if (!this.gotosFromSwitchCases.Contains(gotoStatement))
                  this.gotosFromSwitchCases.Add(gotoStatement);
              }
            }
          }
          for (int j = i+1; j < n; j++)
            newStatements.Add(statements[j]);
          break;
        }
        var conditionalStatement = statement as IConditionalStatement;
        if (conditionalStatement != null) {
          int offset;
          var nestedStatements = ((BlockStatement)conditionalStatement.TrueBranch).Statements;
          offset = nestedStatements.Count == 0 ? 1 : 3;
          var bb = conditionalStatement.FalseBranch as BlockStatement;
          if (bb != null) {
            nestedStatements = bb.Statements;
            for (int j = offset, m = nestedStatements.Count; j < m; j++)
              newStatements.Add(nestedStatements[j]);
            if (offset == 1) {
              ILabeledStatement target = ((GotoStatement)nestedStatements[0]).TargetStatement;
              while (i < n && statements[i] != target) i++;
            }
          }
          for (int j = i+1; j < n; j++)
            newStatements.Add(statements[j]);
          break;
        }
        newStatements.Add(statement);
      }
      blockStatement.Statements = newStatements;
    }
  }

  internal class RemoveCompilerIntroducedLocals : CodeTraverser {

    MoveNextDecompiler decompiler;

    internal RemoveCompilerIntroducedLocals(MoveNextDecompiler decompiler) {
      this.decompiler = decompiler;
    }

    public override void TraverseChildren(IBlockStatement block) {
      var blockStatement = (BlockStatement)block;
      var statements = blockStatement.Statements;
      for (int i = 0, n = statements.Count; i < n; i++) {
        var statement = statements[i];
        var localDecl = statement as LocalDeclarationStatement;
        if (localDecl != null) {
          if (localDecl.LocalVariable == this.decompiler.stateLocal || this.decompiler.returnLocals.ContainsKey(localDecl.LocalVariable) ||
            TypeHelper.TypesAreEquivalent(UnspecializedMethods.AsUnspecializedTypeReference(localDecl.LocalVariable.Type), this.decompiler.unspecializedContainingType))
            statements[i] = CodeDummy.LabeledStatement;
          continue;
        }
        var expressionStatement = statement as ExpressionStatement;
        if (expressionStatement != null) {
          var assignment = expressionStatement.Expression as Assignment;
          if (assignment != null) {
            var targetLocal = assignment.Target.Definition as ILocalDefinition;
            if (targetLocal != null && (targetLocal == this.decompiler.stateLocal || this.decompiler.returnLocals.ContainsKey(targetLocal) ||
            TypeHelper.TypesAreEquivalent(UnspecializedMethods.AsUnspecializedTypeReference(targetLocal.Type), this.decompiler.unspecializedContainingType)))
              statements[i] = CodeDummy.LabeledStatement;
          }
          continue;
        }
        this.Traverse(statement);
      }
    }

  }

  /// <summary>
  /// Remove null statements from list of statements in a block. 
  /// 
  /// This class is supposed to use with a linear code traverser that removes unwanted statements. The expectation is
  /// that the linear code traverser will nullify the unwanted statements and this class removes the null ones from 
  /// the statement list to avoid O(n^2) complexity.
  /// </summary>
  internal class RemoveDummyStatements : CodeTraverser {

    /// <summary>
    /// Remove all the null elements in a statement list. Time complexity is O(n). 
    /// </summary>
    /// <param name="block"></param>
    public override void TraverseChildren(IBlockStatement block) {
      var blockStatement = (BlockStatement)block;
      var statements = blockStatement.Statements;
      int count = 0;
      for (int i = 0, n = statements.Count; i < n; i++) {
        var statement = statements[i];
        if (statement != CodeDummy.LabeledStatement) {
          count++;
          this.Traverse(statement);
        }
      }
      if (count != statements.Count) {
        List<IStatement> newStatements = new List<IStatement>(count);
        for (int i = 0, n = statements.Count; i < n; i++) {
          var statement = statements[i];
          if (statement != CodeDummy.LabeledStatement) {
            newStatements.Add(statement);
          }
        }
        blockStatement.Statements = newStatements;
      }
    }
  }
}
