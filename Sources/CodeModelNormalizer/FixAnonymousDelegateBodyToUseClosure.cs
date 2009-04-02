//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Cci {

  internal class FixAnonymousDelegateBodyToUseClosure : CodeMutator {

    Dictionary<object, BoundField> fieldForCapturedLocalOrParameter;
    TypeDefinition closure;
    List<IFieldDefinition> outerClosures;

    internal FixAnonymousDelegateBodyToUseClosure(Dictionary<object, BoundField> fieldForCapturedLocalOrParameter, Dictionary<object, object> cache,
      TypeDefinition closure, List<IFieldDefinition> outerClosures, 
      IMetadataHost host, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, ilToSourceProvider, sourceToILProvider, sourceLocationProvider) {
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