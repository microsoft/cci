//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
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
  internal class CopyToIteratorClosure : CopyTypeFromIteratorToClosure {

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
    internal CopyToIteratorClosure(Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, Dictionary<IBlockStatement, uint> iteratorLocalCount, Dictionary<IGenericMethodParameter, IGenericTypeParameter> genericParameterMapping,
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
  /// occurences of the generic method parameters with corresponding generic method parameters. 
  /// </summary>
  internal class CopyTypeFromIteratorToClosure : MethodBodyMappingMutator {
    protected Dictionary<IGenericMethodParameter, IGenericTypeParameter> mapping;
    internal CopyTypeFromIteratorToClosure(IMetadataHost host, Dictionary<IGenericMethodParameter, IGenericTypeParameter> mapping)
      : base(host) {
      this.mapping = mapping;
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      IGenericMethodParameterReference genericMethodParameterReference = typeReference as IGenericMethodParameterReference;
      if (genericMethodParameterReference != null) {
        var genericMethodParameter = genericMethodParameterReference.ResolvedType;
        if (this.mapping.ContainsKey(genericMethodParameter))
          return this.mapping[genericMethodParameter];
      }
      return base.Visit(typeReference);
    }
  }

  /// <summary>
  /// Use this as a base class when you try to mutate the method body of method M1 in class C1
  /// to be used as method body of method M2 in class C2. supporting the substitution of
  /// parameters as well as type parameters. 
  /// 
  /// TODO: CodeMutator is not suitable base class, as its default behavior is copying definitions. 
  /// An immediate work item is to write a more suitable duplicator class. 
  /// </summary>
  /// <remarks>
  /// This class is the base class for copying a method body, the actual substitution is expected 
  /// in subclasses. Copying in this class does the following to make the substitution possible. 
  /// 1) Making copies of references. A general rule is that if a reference is a definition, copy 
  /// the definition as a reference.
  /// 2) Namespace Type Definition is given special treatment. Because if we copy it as a namespace
  /// type reference using MetadataMutator's copy, the containing namespace is duplicated, resulting
  /// in duplicated copies of definitions. We simply return the namespace type definition.
  /// 3) SpecializedNestedTypeReference is copied here simply because MetadataMutator's copy 
  /// of a type reference would treat it as a nestedtypereference. 
  /// 4) Make sure we do not copy generic method parameters and generic type parameters. 
  /// 
  /// Typically we set copyOnlyIfNotAlreadyMutable to be false to make sure references are copied no matter
  /// what. If one wants to set this flag to true, presumably one needs to copy those references by himself.
  /// </remarks>
  public class MethodBodyMappingMutator : CodeMutator {
    /// <summary>
    /// Use this as a base class when you try to mutate the method body of method M1 in class C1
    /// to be used as method body of method M2 in class C2. Anticipating the substitution of
    /// parameters as well as type parameters, this mutator copies references but not definitions. 
    /// If a reference is itself a definition, a new reference will be created. 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public MethodBodyMappingMutator(IMetadataHost host)
      : base(host, false) { }

    /// <summary>
    /// Visits the specified namespace type reference. If the type reference is a definition, return the definition. 
    /// </summary>
    /// <param name="namespaceTypeReference">The namespace type reference.</param>
    public override INamespaceTypeReference Visit(INamespaceTypeReference namespaceTypeReference) {
      INamespaceTypeDefinition/*?*/ namespaceTypeDefinition = namespaceTypeReference as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null)
        return this.Visit(namespaceTypeDefinition);
      return this.Visit(this.GetMutableCopy(namespaceTypeReference));
    }
    /// <summary>
    /// Visits the specified nested type reference. Do not copy nested type definitions. If the nestedTypeReference is
    /// a ISpecializedNestedTypeDefinition, it will be treated as a specializedNestedTypeReference, for which a new
    /// reference is created and visited. 
    /// </summary>
    /// <param name="nestedTypeReference">The nested type reference.</param>
    public override INestedTypeReference Visit(INestedTypeReference nestedTypeReference) {
      ISpecializedNestedTypeReference/*?*/ specializedNestedTypeReference = nestedTypeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null) {
        return this.Visit(this.GetMutableCopy(specializedNestedTypeReference));
      }
      return this.Visit(this.GetMutableCopy(nestedTypeReference));
    }

    /// <summary>
    /// 
    /// </summary>
    public override INamespaceTypeDefinition Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      return namespaceTypeDefinition;
    }
  
    /// <summary>
    /// Visits the specified field reference. Do not copy field definitions.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    public override IFieldReference Visit(IFieldReference fieldReference) {
      if (this.stopTraversal) return fieldReference;
      if (fieldReference == Dummy.FieldReference || fieldReference == Dummy.Field) return Dummy.FieldReference;
      ISpecializedFieldReference/*?*/ specializedFieldReference = fieldReference as ISpecializedFieldReference;
      if (specializedFieldReference != null)
        return this.Visit(this.GetMutableCopy(specializedFieldReference));
      return this.Visit(this.GetMutableCopy(fieldReference));
    }

    /// <summary>
    /// Visits the specified method reference. Do not copy definition. 
    /// </summary>
    /// <param name="methodReference">The method reference.</param>
    /// <returns></returns>
    public override IMethodReference Visit(IMethodReference methodReference) {
      if (this.stopTraversal) return methodReference;
      if (methodReference == Dummy.MethodReference || methodReference == Dummy.Method) return Dummy.MethodReference;
      ISpecializedMethodReference/*?*/ specializedMethodReference = methodReference as ISpecializedMethodReference;
      if (specializedMethodReference != null)
        return this.Visit(this.GetMutableCopy(specializedMethodReference));
      IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null)
        return this.Visit(this.GetMutableCopy(genericMethodInstanceReference));
      else {
        return this.Visit(this.GetMutableCopy(methodReference));
      }
    }

    /// <summary>
    /// Visit a genericTypeParameterReference. Do not copy if it is a generic type parameter. 
    /// </summary>
    public override IGenericTypeParameterReference Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      IGenericTypeParameter genericTypeParameter = genericTypeParameterReference as IGenericTypeParameter;
      if (genericTypeParameter != null) return genericTypeParameter;
      return base.Visit(genericTypeParameterReference);
    }

    /// <summary>
    /// Visit a genericMethodParameterReference. Do not copy if it is a genericMethodParameter.
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    /// <returns></returns>
    public override IGenericMethodParameterReference Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      IGenericMethodParameter genericMethodParameter = genericMethodParameterReference as IGenericMethodParameter;
      if (genericMethodParameter != null) return genericMethodParameter;
      return base.Visit(genericMethodParameterReference);
    }
  }
}
