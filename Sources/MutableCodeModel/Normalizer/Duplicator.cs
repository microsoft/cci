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
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace Microsoft.Cci.MutableCodeModel {
  /// <summary>
  /// A rewriter that takes a copy of the body of an iterator method and turns it into the body of a MoveNext method
  /// by replacing parameters and locals with iterator state fields and replacing occurrences of the generic
  /// method parameters of the iterator with generic type parameter of the iterator state class. 
  /// </summary>
  internal class RewriteAsMoveNext : CodeRewriter {

    Dictionary<object, BoundField> fieldForCapturedLocalOrParameter;
    Dictionary<IBlockStatement, uint>/*?*/ iteratorLocalCount;
    Dictionary<uint, IGenericTypeParameter> genericParameterMapping;
    IteratorClosureInformation iteratorClosure;

    /// <summary>
    /// A rewriter that takes a copy of the body of an iterator method and turns it into the body of a MoveNext method
    /// by replacing parameters and locals with iterator state fields and replacing occurrences of the generic
    /// method parameters of the iterator with generic type parameter of the iterator state class. 
    /// </summary>
    /// <param name="fieldForCapturedLocalOrParameter">A map from captured locals and parameters to the closure class fields that hold their state for the method
    /// corresponding to the anonymous delegate.</param>
    /// <param name="genericParameterMapping">The mapping between generic type parameter(s) of the closure class, if any, to the generic method parameter(s).</param>
    /// <param name="closure">Information regarding the closure created for the iterator.</param>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="iteratorLocalCount">A map that indicates how many iterator locals are present in a given block. Only useful for generated MoveNext methods.</param>
    internal RewriteAsMoveNext(Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, Dictionary<IBlockStatement, uint> iteratorLocalCount, 
      Dictionary<uint, IGenericTypeParameter> genericParameterMapping,  IteratorClosureInformation closure, IMetadataHost host)
      : base(host) {
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
      this.iteratorClosure = closure;
      this.genericParameterMapping = genericParameterMapping;
      this.iteratorLocalCount = iteratorLocalCount;
    }

    public override void RewriteChildren(AddressableExpression addressableExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(addressableExpression.Definition, out boundField)) {
        addressableExpression.Instance = new ThisReference();
        addressableExpression.Definition = iteratorClosure.GetReferenceOfFieldUsedByPeers(boundField.Field);
        addressableExpression.Type = boundField.Type;
        return;
      }
      base.RewriteChildren(addressableExpression);
    }

    public override void RewriteChildren(BlockStatement blockStatement) {
      var savedCurrentBlockStatement = this.currentBlockStatement;
      this.currentBlockStatement = blockStatement;
      base.RewriteChildren(blockStatement);
      this.currentBlockStatement = savedCurrentBlockStatement;
    }

    BlockStatement currentBlockStatement;

    public override void RewriteChildren(BoundExpression boundExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(boundExpression.Definition, out boundField)) {
        boundExpression.Instance = new ThisReference();
        boundExpression.Definition = iteratorClosure.GetReferenceOfFieldUsedByPeers(boundField.Field);
        boundExpression.Type = boundField.Type;
        return;
      }
      base.RewriteChildren(boundExpression);
    }

    public override void RewriteChildren(TargetExpression targetExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(targetExpression.Definition, out boundField)) {
        targetExpression.Instance = new ThisReference();
        targetExpression.Definition = iteratorClosure.GetReferenceOfFieldUsedByPeers(boundField.Field);
        targetExpression.Type = boundField.Type;
        return;
      }
      base.RewriteChildren(targetExpression);
    }

    public override IExpression Rewrite(IThisReference thisReference) {
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisReference;
      boundExpression.Definition = iteratorClosure.ThisFieldReference;
      boundExpression.Type = iteratorClosure.ThisFieldReference.Type;
      return boundExpression;
    }

    public override IStatement Rewrite(ILocalDeclarationStatement localDeclarationStatement) {
      if (this.iteratorLocalCount != null) {
        uint count = 0;
        this.iteratorLocalCount.TryGetValue(this.currentBlockStatement, out count);
        this.iteratorLocalCount[this.currentBlockStatement] = ++count;
      }

      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(localDeclarationStatement.LocalVariable, out boundField)) {
        if (localDeclarationStatement.InitialValue != null && !localDeclarationStatement.InitialValue.Equals(Dummy.Expression)) {
          ExpressionStatement assignToLocal = new ExpressionStatement() {
            Expression = new Assignment() {
              Source = localDeclarationStatement.InitialValue,
              Target = new TargetExpression() { Definition = localDeclarationStatement.LocalVariable, Instance = null, Type = localDeclarationStatement.LocalVariable.Type },
              Type = localDeclarationStatement.LocalVariable.Type
            },
            Locations = IteratorHelper.EnumerableIsEmpty(localDeclarationStatement.Locations) ? null : new List<ILocation>(localDeclarationStatement.Locations)
          };
          base.RewriteChildren(assignToLocal);
          return assignToLocal;
        }
      }
      return base.Rewrite(localDeclarationStatement);
    }

    public override ITypeReference Rewrite(IGenericMethodParameterReference genericMethodParameterReference) {
      IGenericTypeParameter targetType;
      if (this.genericParameterMapping.TryGetValue(genericMethodParameterReference.InternedKey, out targetType))
        return targetType;
      return base.Rewrite(genericMethodParameterReference);
    }

  }

  /// <summary>
  /// Copy a type reference used by an iterator method to a type reference used by the iterator closure. That is, replace
  /// occurences of the generic method parameters with corresponding generic type parameters. 
  /// </summary>
#pragma warning disable 618
  internal class CopyTypeFromIteratorToClosure : MethodBodyMappingMutator {
#pragma warning restore 618

    protected Dictionary<uint, IGenericTypeParameter> mapping;

    internal CopyTypeFromIteratorToClosure(IMetadataHost host, Dictionary<uint, IGenericTypeParameter> mapping)
      : base(host) {
      this.mapping = mapping;
    }

    /// <summary>
    /// Visit a type reference. 
    /// </summary>
    /// <param name="typeReference"></param>
    /// <returns></returns>
    public override ITypeReference Visit(ITypeReference typeReference) {
      IGenericMethodParameterReference genericMethodParameterReference = typeReference as IGenericMethodParameterReference;
      if (genericMethodParameterReference != null) {
        IGenericTypeParameter targetType;
        if (this.mapping.TryGetValue(genericMethodParameterReference.InternedKey, out targetType))
          return targetType;
      }
      return base.Visit(typeReference);
    }
  }

  /// <summary>
  /// Use this as a base class when you try to mutate the method body of method M1 in class C1
  /// to be used as method body of method M2 in class C2. supporting the substitution of
  /// parameters as well as type parameters. 
  /// </summary>
  [Obsolete("Please use CodeRewriter")]
  public class MethodBodyMappingMutator : CodeMutatingVisitor {

    /// <summary>
    /// Use this as a base class when you try to mutate the method body of method M1 in class C1
    /// to be used as method body of method M2 in class C2. Anticipating the substitution of
    /// parameters as well as type parameters, this mutator copies references but not definitions. 
    /// If a reference is itself a definition, a new reference will be created. 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public MethodBodyMappingMutator(IMetadataHost host)
      : base(host) { 
    }

    /// <summary>
    /// Leaves namespace type references alone since they cannot involve type parameters of the target class or method.
    /// </summary>
    /// <param name="namespaceTypeReference">The namespace type reference.</param>
    public override INamespaceTypeReference Visit(INamespaceTypeReference namespaceTypeReference) {
      return namespaceTypeReference;
    }

    /// <summary>
    /// Copies specialized nested type references since their containing types may involve type parameters, but
    /// leaves other kinds of nested type references alone.
    /// </summary>
    /// <param name="nestedTypeReference">The nested type reference.</param>
    public override INestedTypeReference Visit(INestedTypeReference nestedTypeReference) {
      ISpecializedNestedTypeReference/*?*/ specializedNestedTypeReference = nestedTypeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null)
        return this.Visit(specializedNestedTypeReference);
      return nestedTypeReference;
    }

    /// <summary>
    /// Visit a genericTypeParameterReference. Do not copy if it is a generic type parameter. 
    /// </summary>
    public override IGenericTypeParameterReference Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      return genericTypeParameterReference;
    }

    /// <summary>
    /// Visit a genericMethodParameterReference. Do not copy if it is a genericMethodParameter.
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    /// <returns></returns>
    public override IGenericMethodParameterReference Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      return genericMethodParameterReference;
    }
  }
}
