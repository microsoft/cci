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
  internal class ClosureRemover : CodeRewriter {

    internal ClosureRemover(IMetadataHost host, Hashtable<object> closures, Hashtable<Expression> closureFieldToLocalOrParameterMap)
      : base(host) {
      Contract.Requires(host != null);
      Contract.Requires(closures != null);
      Contract.Requires(closureFieldToLocalOrParameterMap != null);
      this.closures = closures;
      this.closureFieldToLocalOrParameterMap = closureFieldToLocalOrParameterMap;
    }

    Hashtable<object> closures;
    Hashtable<Expression> closureFieldToLocalOrParameterMap;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.closures != null);
      Contract.Invariant(this.closureFieldToLocalOrParameterMap != null);
    }

    public override IAddressableExpression Rewrite(IAddressableExpression addressableExpression) {
      var result = base.Rewrite(addressableExpression);
      var field = addressableExpression.Definition as IFieldReference;
      if (field != null) {
        var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey);
        if (locOrPar != null) {
          object definition = locOrPar;
          var boundExpression = locOrPar as IBoundExpression;
          if (boundExpression != null) definition = boundExpression.Definition;
          Contract.Assume(definition is ILocalDefinition || definition is IParameterDefinition ||
          definition is IFieldReference || definition is IMethodReference || definition is IExpression);
          return new AddressableExpression() { Definition = definition, Type = field.Type };
        }
      }
      return result;
    }

    public override IExpression Rewrite(IBoundExpression boundExpression) {
      var result = base.Rewrite(boundExpression);
      var field = boundExpression.Definition as IFieldReference;
      if (field != null) {
        var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey);
        if (locOrPar != null) return locOrPar;
      }
      return result;
    }

    public override IStatement Rewrite(IExpressionStatement expressionStatement) {
      var assignment = expressionStatement.Expression as Assignment;
      if (assignment != null) {
        var field = assignment.Target.Definition as IFieldReference;
        if (field != null) {
          if (this.closures.Find(field.Type.InternedKey) != null) {
            //Assigning the closure instance to a field of an outer closure or an interator state class.
            return CodeDummy.Block;
          }
          var closureInstance = assignment.Target.Instance;
          if (closureInstance != null && this.closures.Find(closureInstance.Type.InternedKey) != null) {
            var binding = assignment.Source as IBoundExpression;
            if (binding != null) {
              if (binding.Definition is IParameterDefinition) return CodeDummy.Block;
              var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey) as IBoundExpression;
              if (locOrPar != null) {
                var loc = locOrPar.Definition as ILocalDefinition;
                if (loc != null && this.closures.Find(loc.Type.InternedKey) != null) {
                  var source = this.Rewrite(assignment.Source);
                  return new LocalDeclarationStatement() { LocalVariable = loc, InitialValue = source, Locations = assignment.Locations };
                }
              }
            }
            return CodeDummy.Block;
          } 
        }
      }
      return base.Rewrite(expressionStatement);
    }

    public override IStatement Rewrite(ILocalDeclarationStatement localDeclarationStatement) {
      if (this.closures.Find(localDeclarationStatement.LocalVariable.Type.InternedKey) != null) {
        return CodeDummy.Block;
      }
      return base.Rewrite(localDeclarationStatement);
    }

    public override IExpression Rewrite(IPopValue popValue) {
      return base.Rewrite(popValue);
    }

    public override IStatement Rewrite(IPushStatement pushStatement) {
      var result = base.Rewrite(pushStatement);
      if (this.closures.Find(pushStatement.ValueToPush.Type.InternedKey) != null) return CodeDummy.Block;
      return result;
    }

    public override ITargetExpression Rewrite(ITargetExpression targetExpression) {
      var result = base.Rewrite(targetExpression);
      var field = targetExpression.Definition as IFieldReference;
      if (field != null) {
        var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey);
        if (locOrPar != null) {
          object definition = locOrPar;
          var boundExpression = locOrPar as IBoundExpression;
          if (boundExpression != null) definition = boundExpression.Definition;
          Contract.Assume(definition is ILocalDefinition || definition is IParameterDefinition || 
          definition is IFieldReference || definition is IArrayIndexer || 
          definition is IAddressDereference || definition is IPropertyDefinition);
          return new TargetExpression() { Definition = definition, Type = field.Type };
        }
      }
      return result;
    }

  }
}