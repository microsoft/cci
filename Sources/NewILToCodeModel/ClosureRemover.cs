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

    internal ClosureRemover(SourceMethodBody body, Hashtable<object> closures, Hashtable<Expression> closureFieldToLocalOrParameterMap)
      : base(body.host) {
      Contract.Requires(body != null);
      Contract.Requires(closures != null);
      Contract.Requires(closureFieldToLocalOrParameterMap != null);
      this.closures = closures;
      this.closureFieldToLocalOrParameterMap = closureFieldToLocalOrParameterMap;
      Contract.Assume(body.numberOfAssignmentsToLocal != null);
      this.numberOfAssignmentsToLocal = body.numberOfAssignmentsToLocal;
      Contract.Assume(body.numberOfReferencesToLocal != null);
      this.numberOfReferencesToLocal = body.numberOfReferencesToLocal;
    }

    Hashtable<object> closures;
    Hashtable<Expression> closureFieldToLocalOrParameterMap;
    HashtableForUintValues<object> numberOfReferencesToLocal;
    HashtableForUintValues<object> numberOfAssignmentsToLocal;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.closures != null);
      Contract.Invariant(this.closureFieldToLocalOrParameterMap != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
    }

    public override IAddressableExpression Rewrite(IAddressableExpression addressableExpression) {
      var result = base.Rewrite(addressableExpression);
      var field = addressableExpression.Definition as IFieldReference;
      if (field == null) {
        var capturedLocal = addressableExpression.Definition as CapturedLocalDefinition;
        if (capturedLocal != null) field = capturedLocal.capturingField;
      }
      if (field != null) {
        var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey);
        if (locOrPar != null) {
          object definition = locOrPar;
          var boundExpression = locOrPar as IBoundExpression;
          if (boundExpression != null) definition = boundExpression.Definition;
          Contract.Assume(definition is ILocalDefinition || definition is IParameterDefinition ||
          definition is IFieldReference || definition is IMethodReference || definition is IExpression);
          if (definition is LocalDefinition) {
            this.numberOfAssignmentsToLocal[definition]++;
            this.numberOfReferencesToLocal[definition]++;
          }
          return new AddressableExpression() { Definition = definition, Type = field.Type };
        }
      }
      return result;
    }

    public override IExpression Rewrite(IBoundExpression boundExpression) {
      var result = base.Rewrite(boundExpression);
      var field = boundExpression.Definition as IFieldReference;
      if (field == null) {
        var capturedLocal = boundExpression.Definition as CapturedLocalDefinition;
        if (capturedLocal != null) field = capturedLocal.capturingField;
      }
      if (field != null) {
        var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey);
        if (locOrPar != null) {
          var be = locOrPar as BoundExpression;
          if (be != null && be.Definition is LocalDefinition) {
            this.numberOfReferencesToLocal[be.Definition]++;
          }
          return locOrPar;
        }
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
          //in this case we may be initializing a closure field with the parameter that it captures.
          var closureInstance = assignment.Target.Instance;
          if (closureInstance != null && this.closures.Find(closureInstance.Type.InternedKey) != null) {
            var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey) as IBoundExpression;
            if (locOrPar != null) {
              var binding = assignment.Source as IBoundExpression;
              if (binding != null) {
                if (binding.Definition == locOrPar.Definition) return CodeDummy.Block;
              }
            } else {
              var thisRef = assignment.Source as IThisReference;
              if (thisRef != null) return CodeDummy.Block;
            }
          }
          //If we get here, we just have a normal assignment to a closure field. We need to replace the field with the local or parameter.
          //The base call will do that by calling Rewrite(ITargetExpression).
        } else {
          var local = assignment.Target.Definition as ILocalDefinition;
          if (local != null && this.closures.Find(local.Type.InternedKey) != null) {
            Contract.Assume(assignment.Source is ICreateObjectInstance);
            return CodeDummy.Block;
          }
        }
      }
      return base.Rewrite(expressionStatement);
    }

    public override IStatement Rewrite(ILocalDeclarationStatement localDeclarationStatement) {
      var capturedLocal = localDeclarationStatement.LocalVariable as CapturedLocalDefinition;
      if (capturedLocal != null) {
        Contract.Assume(capturedLocal.capturingField != null);
        var binding = this.closureFieldToLocalOrParameterMap[capturedLocal.capturingField.InternedKey] as BoundExpression;
        if (binding != null && binding.Definition != capturedLocal) return CodeDummy.Block;
      }
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
      if (field == null) {
        var capturedLocal = targetExpression.Definition as CapturedLocalDefinition;
        if (capturedLocal != null) field = capturedLocal.capturingField;
      }
      if (field != null) {
        var locOrPar = this.closureFieldToLocalOrParameterMap.Find(field.InternedKey);
        if (locOrPar != null) {
          object definition = locOrPar;
          var boundExpression = locOrPar as IBoundExpression;
          if (boundExpression != null) definition = boundExpression.Definition;
          Contract.Assume(definition is ILocalDefinition || definition is IParameterDefinition || 
          definition is IFieldReference || definition is IArrayIndexer || 
          definition is IAddressDereference || definition is IPropertyDefinition);
          if (definition is LocalDefinition) {
            this.numberOfAssignmentsToLocal[definition]++;
          }
          return new TargetExpression() { Definition = definition, Type = field.Type };
        }
      }
      return result;
    }

  }
}