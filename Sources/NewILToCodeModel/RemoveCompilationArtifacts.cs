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
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {

  internal class CompilationArtifactRemover : CodeRewriter {

    internal CompilationArtifactRemover(SourceMethodBody sourceMethodBody)
      : base(sourceMethodBody.host) {
      Contract.Requires(sourceMethodBody != null);
      this.numberOfAssignmentsToLocal = sourceMethodBody.numberOfAssignmentsToLocal; Contract.Assume(this.numberOfAssignmentsToLocal != null);
      this.numberOfReferencesToLocal = sourceMethodBody.numberOfReferencesToLocal; Contract.Assume(this.numberOfReferencesToLocal != null);
    }

    HashtableForUintValues<object> numberOfAssignmentsToLocal;
    HashtableForUintValues<object> numberOfReferencesToLocal;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
    }


    public override IExpression Rewrite(IBlockExpression blockExpression) {
      var e = base.Rewrite(blockExpression);
      var be = e as IBlockExpression;
      if (be == null) return e;
      if (IteratorHelper.EnumerableIsEmpty(be.BlockStatement.Statements))
        return be.Expression;
      return be;
    }

    public override IExpression Rewrite(IAssignment assignment) {
      var binOp = assignment.Source as BinaryOperation;
      if (binOp != null) {
        var addressDeref = binOp.LeftOperand as IAddressDereference;
        if (addressDeref != null) {
          var dupValue = addressDeref.Address as IDupValue;
          if (dupValue != null) {
            if (binOp is IAddition || binOp is IBitwiseAnd || binOp is IBitwiseOr || binOp is IDivision || binOp is IExclusiveOr ||
            binOp is ILeftShift || binOp is IModulus || binOp is IMultiplication || binOp is IRightShift || binOp is ISubtraction) {
              binOp.LeftOperand = assignment.Target;
              return binOp;
            }
          }
        } else {
          var boundExpr = binOp.LeftOperand as IBoundExpression;
          if (boundExpr != null && boundExpr.Definition == assignment.Target.Definition && boundExpr.Instance is IDupValue) {
            if (binOp is IAddition || binOp is IBitwiseAnd || binOp is IBitwiseOr || binOp is IDivision || binOp is IExclusiveOr ||
            binOp is ILeftShift || binOp is IModulus || binOp is IMultiplication || binOp is IRightShift || binOp is ISubtraction) {
              binOp.LeftOperand = assignment.Target;
              return binOp;
            }
          }
        }
      }
      return base.Rewrite(assignment);
    }

    public override IExpression Rewrite(ICreateObjectInstance createObjectInstance) {
      var mutableCreateObjectInstance = createObjectInstance as CreateObjectInstance;
      if (mutableCreateObjectInstance != null && mutableCreateObjectInstance.Arguments.Count == 2) {
        AddressOf/*?*/ aexpr = mutableCreateObjectInstance.Arguments[1] as AddressOf;
        if (aexpr != null && aexpr.Expression.Definition is IMethodReference) {
          CreateDelegateInstance createDel = new CreateDelegateInstance();
          createDel.Instance = mutableCreateObjectInstance.Arguments[0];
          createDel.IsVirtualDelegate = aexpr.Expression.Instance != null;
          createDel.MethodToCallViaDelegate = (IMethodReference)aexpr.Expression.Definition;
          createDel.Locations = mutableCreateObjectInstance.Locations;
          createDel.Type = createObjectInstance.Type;
          return this.Rewrite(createDel);
        }
      }
      return base.Rewrite(createObjectInstance);
    }

    public override IExpression Rewrite(IGreaterThan greaterThan) {
      var castIfPossible = greaterThan.LeftOperand as ICastIfPossible;
      if (castIfPossible != null) {
        var compileTimeConstant = greaterThan.RightOperand as ICompileTimeConstant;
        if (compileTimeConstant != null && compileTimeConstant.Value == null) {
          return this.Rewrite(new CheckIfInstance() {
            Operand = castIfPossible.ValueToCast,
            TypeToCheck = castIfPossible.TargetType,
            Type = greaterThan.Type,
          });
        }
      }
      castIfPossible = greaterThan.RightOperand as ICastIfPossible;
      if (castIfPossible != null) {
        var compileTimeConstant = greaterThan.LeftOperand as ICompileTimeConstant;
        if (compileTimeConstant != null && compileTimeConstant.Value == null) {
          return this.Rewrite(new CheckIfInstance() {
            Operand = castIfPossible.ValueToCast,
            TypeToCheck = castIfPossible.TargetType,
            Type = greaterThan.Type,
          });
        }
      }
      return base.Rewrite(greaterThan);
    }


    public override IExpression Rewrite(ILogicalNot logicalNot) {
      if (logicalNot.Type is Dummy)
        return IfThenElseReplacer.InvertCondition(this.Rewrite(logicalNot.Operand));
      else if (logicalNot.Operand.Type.TypeCode == PrimitiveTypeCode.Int32)
        return new Equality() {
          LeftOperand = this.Rewrite(logicalNot.Operand),
          RightOperand = new CompileTimeConstant() { Value = 0, Type = this.host.PlatformType.SystemInt32 },
          Type = this.host.PlatformType.SystemBoolean,
        };
      else {
        var castIfPossible = logicalNot.Operand as CastIfPossible;
        if (castIfPossible != null) {
          var mutableLogicalNot = logicalNot as LogicalNot;
          if (mutableLogicalNot != null) {
            var operand = new CheckIfInstance() {
              Locations = castIfPossible.Locations,
              Operand = castIfPossible.ValueToCast,
              Type = this.host.PlatformType.SystemBoolean,
              TypeToCheck = castIfPossible.TargetType,
            };
            mutableLogicalNot.Operand = operand;
            return mutableLogicalNot;
          }
        }
        return base.Rewrite(logicalNot);
      }
    }

    public override IExpression Rewrite(IMethodCall methodCall) {
      var mutableMethodCall = methodCall as MethodCall;
      if (mutableMethodCall != null) {
        if (mutableMethodCall.Arguments.Count == 1) {
          var tokenOf = mutableMethodCall.Arguments[0] as TokenOf;
          if (tokenOf != null) {
            var typeRef = tokenOf.Definition as ITypeReference;
            if (typeRef != null && methodCall.MethodToCall.InternedKey == this.GetTypeFromHandle.InternedKey) {
              return new TypeOf() { Locations = mutableMethodCall.Locations, Type = methodCall.Type, TypeToGet = typeRef };
            }
          }
        }
      }
      return base.Rewrite(methodCall);
    }

    public override IExpression Rewrite(INotEquality notEquality) {
      base.Rewrite(notEquality);
      var cc1 = notEquality.LeftOperand as CompileTimeConstant;
      var cc2 = notEquality.RightOperand as CompileTimeConstant;
      if (cc1 != null && cc2 != null) {
        if (cc1.Type.TypeCode == PrimitiveTypeCode.Int32 && cc2.Type.TypeCode == PrimitiveTypeCode.Int32) {
          Contract.Assume(cc1.Value is int);
          Contract.Assume(cc2.Value is int);
          return new CompileTimeConstant() { Value = ((int)cc1.Value) != ((int)cc2.Value), Type = notEquality.Type };
        }
      } else if (cc2 != null && ExpressionHelper.IsNumericZero(cc2) && notEquality.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        return notEquality.LeftOperand;
      }
      return notEquality;
    }

    /// <summary>
    /// A reference to System.Type.GetTypeFromHandle(System.Runtime.TypeHandle).
    /// </summary>
    IMethodReference GetTypeFromHandle {
      get {
        if (this.getTypeFromHandle == null) {
          this.getTypeFromHandle = new MethodReference(this.host, this.host.PlatformType.SystemType, CallingConvention.Default, this.host.PlatformType.SystemType,
          this.host.NameTable.GetNameFor("GetTypeFromHandle"), 0, this.host.PlatformType.SystemRuntimeTypeHandle);
        }
        return this.getTypeFromHandle;
      }
    }
    IMethodReference/*?*/ getTypeFromHandle;


  }

}
