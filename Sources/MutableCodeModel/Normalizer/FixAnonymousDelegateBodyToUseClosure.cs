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
  /// A mutator that visits an anonymous delegate body and produces a copy that has been changed to
  /// reference captured locals and parameters via fields on a closure class.
  /// </summary>
  internal class FixAnonymousDelegateBodyToUseClosure : MethodBodyCodeMutator {

    Dictionary<object, BoundField>/*!*/ fieldForCapturedLocalOrParameter;
    TypeDefinition closure;
    List<IFieldDefinition> outerClosures;

    /// <summary>
    /// Allocates a mutator that visits an anonymous delegate body and produces a copy that has been changed to
    /// reference captured locals and parameters via fields on a closure class.
    /// </summary>
    /// <param name="fieldForCapturedLocalOrParameter">A map from captured locals and parameters to the closure class fields that hold their state for the method
    /// corresponding to the anonymous delegate.</param>
    /// <param name="cache">A cache for any duplicates created by this mutator. The cache is used both to save space and to detect when the traversal of a cycle should stop.</param>
    /// <param name="closure">The definition of the class that contains the fields that hold the values of captured locals and parameters.</param>
    /// <param name="outerClosures">A potentially empty list of closures that for any anonymous delegates that enclose the anonymous delegate that will be
    /// traversed by this mutator.</param>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    internal FixAnonymousDelegateBodyToUseClosure(Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, Dictionary<object, object> cache,
      TypeDefinition closure, List<IFieldDefinition> outerClosures, IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, sourceLocationProvider) {
      this.cache = cache;
      this.fieldForCapturedLocalOrParameter = fieldForCapturedLocalOrParameter;
      this.closure = closure;
      this.outerClosures = outerClosures;
    }

    /// <summary>
    /// Given a closure type that we created, return an expression that is of the form this[.f]*. The [.f] repeats 
    /// more than zero times if the closure is embedded. 
    /// </summary>
    /// <param name="closure">The closure class</param>
    /// <returns></returns>
    private IExpression ClosureInstanceFor(ITypeDefinition closure) {
      ThisReference thisRef = new ThisReference();
      thisRef.Type = this.GetSelfReferenceForPrivateHelperTypes(closure);
      if (closure.InternedKey == this.closure.InternedKey) {
        return thisRef;
      }
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisRef;
      foreach (var closureField in this.outerClosures) {
        boundExpression.Definition = closureField;
        if (closureField.Type.InternedKey == closure.InternedKey) {
          boundExpression.Type = this.GetSelfReferenceForPrivateHelperTypes(closure);
          return boundExpression;
        }
        var be = new BoundExpression();
        be.Instance = boundExpression;
        boundExpression = be;
      }
      Debug.Assert(false);
      return CodeDummy.Expression;
    }

    public override IAddressableExpression Visit(AddressableExpression addressableExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(addressableExpression.Definition, out boundField)) {
        var selfReference = boundField.Field.ContainingTypeDefinition;
        addressableExpression.Instance = this.ClosureInstanceFor(selfReference);
        addressableExpression.Definition = this.GetSelfReference(boundField.Field);
        return addressableExpression;
      }
      return base.Visit(addressableExpression);
    }

    public override IExpression Visit(BoundExpression boundExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(boundExpression.Definition, out boundField)) {
        var selfReference = boundField.Field.ContainingTypeDefinition;
        boundExpression.Instance = this.ClosureInstanceFor(selfReference);
        boundExpression.Definition = this.GetSelfReference(boundField.Field);
        return boundExpression;
      }
      return base.Visit(boundExpression);
    }

    public override ITargetExpression Visit(TargetExpression targetExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(targetExpression.Definition, out boundField)) {
        var selfReference = boundField.Field.ContainingTypeDefinition;
        targetExpression.Instance = this.ClosureInstanceFor(selfReference);
        targetExpression.Definition = this.GetSelfReference(boundField.Field);
        return targetExpression;
      }
      return base.Visit(targetExpression);
    }

    public override IExpression Visit(ThisReference thisReference) {
      TypeDefinition closure = this.closure;
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisReference;
      // Code Review: thisReference.Type is not the closure type yet... Set it? 
      foreach (var closureField in this.outerClosures) {
        boundExpression.Definition = this.GetSelfReference(closureField);
        closure = (TypeDefinition)closureField.Type;
        var be = new BoundExpression();
        be.Instance = boundExpression;
        boundExpression = be;
      }
      boundExpression.Definition = this.GetSelfReference(closure.Fields[0]);
      return boundExpression;
    }

   /// <summary>
   /// Get a reference to private helper type of which resolution is possible. 
   /// </summary>
   /// <param name="privateHelperType"></param>
   /// <returns></returns>
    private ITypeReference GetSelfReferenceForPrivateHelperTypes(ITypeDefinition privateHelperType) {
      var nested = privateHelperType as INestedTypeDefinition;
      if (nested == null) return TypeDefinition.SelfInstance(privateHelperType, this.host.InternFactory);
      var containingType = TypeDefinition.SelfInstance(nested.ContainingTypeDefinition, this.host.InternFactory);
      SpecializedNestedTypeDefinition specializedContainingType = containingType as SpecializedNestedTypeDefinition;
      GenericTypeInstance genericTypeInstance = containingType as GenericTypeInstance;
      if (specializedContainingType != null || genericTypeInstance != null) {
        var resolved = Dummy.SpecializedNestedTypeDefinition;
        if (specializedContainingType != null)
          resolved = (ISpecializedNestedTypeDefinition)specializedContainingType.SpecializeMember(nested, this.host.InternFactory);
        else
          resolved = (ISpecializedNestedTypeDefinition)genericTypeInstance.SpecializeMember(nested, this.host.InternFactory);
        var specailizedResult = new SpecializedNestedTypeReferencePrivateHelper();
        specailizedResult.Copy(resolved, this.host.InternFactory);
        if (specailizedResult.ResolvedType == null) {
        }
        return specailizedResult;
      } else {
        SpecializedNestedTypeReference specializedContainingTypeReference = containingType as SpecializedNestedTypeReference;
        if (specializedContainingTypeReference != null) {
          // assert^ specializedContainingTypeReference.ResolvedType != Dummy.Type;
          var resolved = specializedContainingTypeReference.ResolvedType;
          if (resolved != Dummy.Type) {
            var specailizedResult = new SpecializedNestedTypeReferencePrivateHelper();
            specailizedResult.Copy(resolved, this.host.InternFactory);
            return specailizedResult;
          }
        }
      }
      var result = new NestedTypeReferencePrivateHelper();
      result.Copy(nested, this.host.InternFactory);
      result.ContainingType = containingType;
      return result;
    }
    /// <summary>
    /// Get a reference to a field of a private helper type of which resolution is possible. 
    /// </summary>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    private IFieldReference GetSelfReference(IFieldDefinition fieldDefinition) {
      var containingType = this.GetSelfReferenceForPrivateHelperTypes(fieldDefinition.ContainingTypeDefinition);
      if (containingType is IGenericTypeInstanceReference || containingType is ISpecializedNestedTypeReference) {
        var sFieldReference = new SpecializedFieldReference();
        ((FieldReference)sFieldReference).Copy(fieldDefinition, this.closure.InternFactory);
        sFieldReference.ContainingType = containingType;
        sFieldReference.UnspecializedVersion = fieldDefinition;
        return sFieldReference;
      }
      var result = new FieldReference();
      result.Copy(fieldDefinition, this.host.InternFactory);
      result.ContainingType = containingType;
      return result;
    }
  }
}
