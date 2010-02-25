//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
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

    private IExpression ClosureInstanceFor(ITypeDefinition closure) {
      ThisReference thisRef = new ThisReference();
      thisRef.Type = closure;
      if (closure == this.closure) return thisRef;
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisRef;
      foreach (var closureField in this.outerClosures) {
        boundExpression.Definition = closureField;
        if (closureField.Type == closure) return boundExpression;
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
        addressableExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
        addressableExpression.Definition = boundField.Field;
        return addressableExpression;
      }
      return base.Visit(addressableExpression);
    }

    public override IExpression Visit(BoundExpression boundExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(boundExpression.Definition, out boundField)) {
        boundExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
        boundExpression.Definition = boundField.Field;
        return boundExpression;
      }
      return base.Visit(boundExpression);
    }

    public override ITargetExpression Visit(TargetExpression targetExpression) {
      BoundField/*?*/ boundField;
      if (this.fieldForCapturedLocalOrParameter.TryGetValue(targetExpression.Definition, out boundField)) {
        targetExpression.Instance = this.ClosureInstanceFor(boundField.Field.ContainingTypeDefinition);
        targetExpression.Definition = boundField.Field;
        return targetExpression;
      }
      return base.Visit(targetExpression);
    }

    public override IExpression Visit(ThisReference thisReference) {
      TypeDefinition closure = this.closure;
      var boundExpression = new BoundExpression();
      boundExpression.Instance = thisReference;
      foreach (var closureField in this.outerClosures) {
        boundExpression.Definition = closureField;
        closure = (TypeDefinition)closureField.Type;
        var be = new BoundExpression();
        be.Instance = boundExpression;
        boundExpression = be;
      }
      boundExpression.Definition = closure.Fields[0];
      return boundExpression;
    }
  }
}
