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

namespace Microsoft.Cci.MutableCodeModel {
  /// <summary>
  /// A mutator that copies statements, expressions, type references previously in the iterator method world to ones
  /// in the iterator closure. Replace parameters and locals with closure fields. Replace occurrences of the generic
  /// method parameter with generic type parameter of the closure class. 
  /// </summary>
  internal class CopyToIteratorClosure : TypeReferenceSubstitutor {

    Dictionary<object, BoundField> fieldForCapturedLocalOrParameter;
    Dictionary<IBlockStatement, uint>/*?*/ iteratorLocalCount;
    IteratorClosureInformation iteratorClosure;

    /// <summary>
    /// Allocates a mutator that visits an anonymous delegate body and produces a copy that has been changed to
    /// reference captured locals and parameters via fields on a closure class.
    /// </summary>
    /// <param name="fieldForCapturedLocalOrParameter">A map from captured locals and parameters to the closure class fields that hold their state for the method
    /// corresponding to the anonymous delegate.</param>
    /// <param name="genericParameterMapping">The mapping between generic type parameter(s) of the closure class, if any, to the generic method parameter(s).</param>
    /// <param name="closure">Information regarding the closure created for the iterator.</param>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="iteratorLocalCount">A map that indicates how many iterator locals are present in a given block. Only useful for generated MoveNext methods.</param>
    internal CopyToIteratorClosure(Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, Dictionary<IBlockStatement, uint> iteratorLocalCount, Dictionary<uint, IGenericTypeParameter> genericParameterMapping,
      IteratorClosureInformation closure, IMetadataHost host)
      : base(host, genericParameterMapping) {
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
      this.iteratorClosure = closure;
      this.iteratorLocalCount = iteratorLocalCount;
    }

    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(addressableExpression.Definition, out boundField)) {
        addressableExpression.Instance = new ThisReference();
        addressableExpression.Definition = iteratorClosure.GetReferenceOfFieldUsedByPeers(boundField.Field);
        return addressableExpression;
      }
      return base.Visit(addressableExpression);
    }

    public override IBlockStatement Visit(BlockStatement blockStatement) {
      var savedCurrentBlockStatement = this.currentBlockStatement;
      this.currentBlockStatement = blockStatement;
      var result = base.Visit(blockStatement);
      this.currentBlockStatement = savedCurrentBlockStatement;
      return result;
    }

    BlockStatement currentBlockStatement;

    public override IExpression Visit(BoundExpression boundExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(boundExpression.Definition, out boundField)) {
        boundExpression.Instance = new ThisReference();
        boundExpression.Definition = iteratorClosure.GetReferenceOfFieldUsedByPeers(boundField.Field);
        return boundExpression;
      }
      return base.Visit(boundExpression);
    }

    public override ITargetExpression Visit(TargetExpression targetExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(targetExpression.Definition, out boundField)) {
        targetExpression.Instance = new ThisReference();
        targetExpression.Definition = iteratorClosure.GetReferenceOfFieldUsedByPeers(boundField.Field);
        return targetExpression;
      }
      return base.Visit(targetExpression);
    }

    public override IExpression Visit(ThisReference thisReference) {
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisReference;
      boundExpression.Definition = iteratorClosure.ThisFieldReference;
      return boundExpression;
    }

    public override IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
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
            Locations = localDeclarationStatement.Locations
          };
          return base.Visit(assignToLocal);
        }
      }
      return base.Visit(localDeclarationStatement);
    }

  }

  /// <summary>
  /// Copy a type reference used by an iterator method to a type reference used by the iterator closure. That is, replace
  /// occurences of the generic method parameters with corresponding generic type parameters. 
  /// </summary>
  internal class CopyTypeFromIteratorToClosure : MethodBodyMappingMutator {

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
  /// Substitute type references as mapped through a provided table of corresponding
  /// type references.
  /// </summary>
  internal class TypeReferenceSubstitutor : MethodBodyMappingMutator {
    protected Dictionary<uint, ITypeReference> typeMap;
    internal TypeReferenceSubstitutor(IMetadataHost host, Dictionary<uint, ITypeReference> internedKey2typeReference)
      : base(host) {
        this.typeMap = internedKey2typeReference;
    }
    internal TypeReferenceSubstitutor(IMetadataHost host, Dictionary<uint, IGenericTypeParameter> internedKey2typeReference)
      : base(host) {
        this.typeMap = new Dictionary<uint, ITypeReference>(internedKey2typeReference.Count);
        foreach (var e in internedKey2typeReference) {
        this.typeMap.Add(e.Key, e.Value);
      }
    }
    internal TypeReferenceSubstitutor(IMetadataHost host, Dictionary<ITypeReference, ITypeReference> typeReference2typeReference)
      : base(host) {
      this.typeMap = new Dictionary<uint, ITypeReference>(typeReference2typeReference.Count);
      foreach (var e in typeReference2typeReference) {
        this.typeMap.Add(e.Key.InternedKey, e.Value);
      }
    }
    internal TypeReferenceSubstitutor(IMetadataHost host, Dictionary<IGenericMethodParameter, IGenericTypeParameter> genericParameterMapping)
      : base(host) {
      this.typeMap = new Dictionary<uint, ITypeReference>(genericParameterMapping.Count);
      foreach (var e in genericParameterMapping) {
        this.typeMap.Add(e.Key.InternedKey, e.Value);
      }
    }

    /// <summary>
    /// If the interned key of the <paramref name="typeReference"/> is in the domain of the substitution
    /// table, return the type reference that it is mapped to. Otherwise, do a base visit on it.
    /// </summary>
    public override ITypeReference Visit(ITypeReference typeReference) {
      ITypeReference targetType;
      if (this.typeMap.TryGetValue(typeReference.InternedKey, out targetType))
        return targetType;
      return base.Visit(typeReference);
    }
  }


  /// <summary>
  /// Use this as a base class when you try to mutate the method body of method M1 in class C1
  /// to be used as method body of method M2 in class C2. supporting the substitution of
  /// parameters as well as type parameters. 
  /// </summary>
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
