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
using System.Diagnostics.Contracts;
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {
  internal class AnonymousDelegateCachingRemover : CodeRewriter {

    internal AnonymousDelegateCachingRemover(IMetadataHost host, Hashtable<IAnonymousDelegate>/*?*/ delegatesCachedInFields,
      Hashtable<LocalDefinition, AnonymousDelegate>/*?*/ delegatesCachedInLocals)
      : base(host) {
      Contract.Requires(host != null);
      this.delegatesCachedInFields = delegatesCachedInFields;
      this.delegatesCachedInLocals = delegatesCachedInLocals;
    }

    Hashtable<IAnonymousDelegate>/*?*/ delegatesCachedInFields;
    Hashtable<LocalDefinition, AnonymousDelegate>/*?*/ delegatesCachedInLocals;

    public override IExpression Rewrite(IAnonymousDelegate anonymousDelegate) {
      return anonymousDelegate;
    }

    public override IExpression Rewrite(IBoundExpression boundExpression) {
      var fieldReference = boundExpression.Definition as IFieldReference;
      if (fieldReference != null) {
        if (this.delegatesCachedInFields != null) {
          var cachedDelegate = this.delegatesCachedInFields.Find(fieldReference.InternedKey);
          if (cachedDelegate != null) return cachedDelegate;
        }
      } else if (this.delegatesCachedInLocals != null) {
        var local = boundExpression.Definition as LocalDefinition;
        if (local != null) {
          AnonymousDelegate cachedDelegate;
          if (this.delegatesCachedInLocals.TryGetValue(local, out cachedDelegate)) {
            Contract.Assume(cachedDelegate != null);
            return cachedDelegate;
          }
        }
      }
      return base.Rewrite(boundExpression);
    }

    public override IStatement Rewrite(IConditionalStatement conditionalStatement) {
      var condition = conditionalStatement.Condition;
      var logicalNot = condition as ILogicalNot;
      if (logicalNot != null) condition = logicalNot.Operand;
      var equal = condition as IEquality;
      if (equal != null && equal.RightOperand is IDefaultValue) condition = equal.LeftOperand;
      var boundExpression = condition as IBoundExpression;
      if (boundExpression != null) {
        var locations = conditionalStatement.Locations;
        var fieldReference = boundExpression.Definition as IFieldReference;
        if (fieldReference != null) {
          if (this.delegatesCachedInFields != null && this.delegatesCachedInFields.Find(fieldReference.InternedKey) != null)
            return CodeDummy.Block;
        } else if (this.delegatesCachedInLocals != null) {
          var local = boundExpression.Definition as LocalDefinition;
          if (local != null && this.delegatesCachedInLocals.ContainsKey(local))
            return CodeDummy.Block;
        }
      }
      return base.Rewrite(conditionalStatement);
    }

    public override IStatement Rewrite(IExpressionStatement expressionStatement) {
      var assignment = expressionStatement.Expression as Assignment;
      if (assignment != null) {
        var local = assignment.Target.Definition as LocalDefinition;
        if (local != null && this.delegatesCachedInLocals != null && this.delegatesCachedInLocals.ContainsKey(local))
          return CodeDummy.Block;
      }
      return base.Rewrite(expressionStatement);
    }

    public override IStatement Rewrite(ILocalDeclarationStatement localDeclarationStatement) {
      var local = localDeclarationStatement.LocalVariable as LocalDefinition;
      if (local != null && this.delegatesCachedInLocals != null && this.delegatesCachedInLocals.ContainsKey(local))
        return CodeDummy.Block;
      return base.Rewrite(localDeclarationStatement);
    }


  }

}