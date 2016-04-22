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
using System.Collections.Generic;

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
    ILocalDefinition/*?*/ currentSingleUseSingleReferenceLocal;
    IExpression/*?*/ expressionToSubstituteForSingleUseSingleReferenceLocal;
    SetOfObjects/*?*/ localsToEliminate;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
    }


    public override IExpression Rewrite(IAssignment assignment) {
      var binOp = assignment.Source as BinaryOperation;
      if (binOp != null) {
        var addressDeref = binOp.LeftOperand as IAddressDereference;
        if (addressDeref != null) {
          var dupValue = addressDeref.Address as IDupValue;
          if (dupValue != null && assignment.Target.Definition is IAddressDereference) {
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
      } else {
        var assign2 = assignment.Source as Assignment;
        if (assign2 != null) {
          var targetLocal = assign2.Target.Definition as ILocalDefinition;
          if (targetLocal != null) {
            binOp = assign2.Source as BinaryOperation;
            if (binOp != null) {
              var addressDeref = binOp.LeftOperand as IAddressDereference;
              if (addressDeref != null) {
                var dupValue = addressDeref.Address as IDupValue;
                if (dupValue != null && assignment.Target.Definition is IAddressDereference) {
                  if (binOp is IAddition || binOp is IBitwiseAnd || binOp is IBitwiseOr || binOp is IDivision || binOp is IExclusiveOr ||
                  binOp is ILeftShift || binOp is IModulus || binOp is IMultiplication || binOp is IRightShift || binOp is ISubtraction) {
                    binOp.LeftOperand = assignment.Target;
                    if (this.numberOfReferencesToLocal[targetLocal] == 1 && this.numberOfAssignmentsToLocal[targetLocal] == 1)
                      this.currentSingleUseSingleReferenceLocal = targetLocal;
                    return assign2;
                  }
                }
              }
            }
          }
        } else {
          var conversion = assignment.Source as IConversion;
          if (conversion != null) {
            binOp = conversion.ValueToConvert as BinaryOperation;
            if (binOp != null) {
              var addressDeref = binOp.LeftOperand as IAddressDereference;
              if (addressDeref != null) {
                var dupValue = addressDeref.Address as IDupValue;
                if (dupValue != null && assignment.Target.Definition is IAddressDereference) {
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
                    binOp.RightOperand = TypeInferencer.Convert(binOp.RightOperand, assignment.Target.Type);
                    return binOp;
                  }
                }
              }
            } else {
              // For a character-typed field, c, the C# source expressions:
              //   o.c += (char)0, o.c -= (char)0, and o.c *= (char)1
              // produce the IL: "load o; dup; ldfld c; stfld c;".
              // (For some reason, the C# compiler does not do the same thing for "o.c /= (char)1".)
              // Such IL shows up here as "o.c = convert(ushort, dup.c)".
              // Arbitrarily turn it back into "o.c += (char)0".
              if (IsBoundExpressionWithDupInstance(conversion.ValueToConvert)) {
                var t = conversion.ValueToConvert.Type;
                if (t.TypeCode == PrimitiveTypeCode.Char) {
                  return new Addition() {
                    LeftOperand = assignment.Target,
                    RightOperand = new Conversion() {
                      TypeAfterConversion = t,
                      ValueToConvert = new CompileTimeConstant() { Value = 0, Type = assignment.Type, },
                    },
                    ResultIsUnmodifiedLeftOperand = false,
                    Type = assignment.Type,
                  };
                }
              }
            }
          } else {
            // There are several C# source expressions that produce the IL: "load o; dup; ldfld f; stfld f;".
            // Examples are: o.f += 0, o.f -= 0, o.f *= 1, o.f &= true, o.f |= false.
            // (For some reason, the C# compiler does not do the same thing for "o.f /= 1".)
            // Such IL shows up here as "o.f = dup.f".
            // Arbitrarily turn it back into "o.f += 0" for arithmetic types and "o.f |= false" for boolean.
            if (IsBoundExpressionWithDupInstance(assignment.Source)) {
              if (TypeHelper.IsPrimitiveInteger(assignment.Type) && assignment.Type.TypeCode != PrimitiveTypeCode.Char) {
                return new Addition() {
                  LeftOperand = assignment.Target,
                  RightOperand = new CompileTimeConstant() { Value = 0, Type = assignment.Type, },
                  ResultIsUnmodifiedLeftOperand = false,
                  Type = assignment.Type,
                };
              } else if (assignment.Type.TypeCode == PrimitiveTypeCode.Boolean) {
                return new BitwiseOr() {
                  LeftOperand = assignment.Target,
                  RightOperand = new CompileTimeConstant() { Value = false, Type = assignment.Type, },
                  ResultIsUnmodifiedLeftOperand = false,
                  Type = assignment.Type,
                };
              }
            }
          }
        }
      }
      return base.Rewrite(assignment);
    }

    private bool IsBoundExpressionWithDupInstance(IExpression expression) {
      var boundExpression = expression as IBoundExpression;
      if (boundExpression == null) return false;
      var dupValue = boundExpression.Instance as IDupValue;
      return dupValue != null;
    }

    public override IExpression Rewrite(IBlockExpression blockExpression) {
      var e = base.Rewrite(blockExpression);
      var be = e as IBlockExpression;
      if (be == null) return e;
      if (IteratorHelper.EnumerableIsEmpty(be.BlockStatement.Statements))
        return be.Expression;
      return be;
    }

    public override void RewriteChildren(BlockStatement block) {
      base.RewriteChildren(block);
      if (this.localsToEliminate != null && this.localsToEliminate.Count > 0) {
        var statements = block.Statements;
        var n = statements.Count;
        var j = 0;
        for (int i = 0; i < n; i++) {
          var s = statements[i];
          var localDecl = s as LocalDeclarationStatement;
          if (localDecl != null) {
            if (this.localsToEliminate.Contains(localDecl.LocalVariable)) continue;
          } else {
            var exprSt = s as ExpressionStatement;
            if (exprSt != null) {
              var assign = exprSt.Expression as Assignment;
              if (assign != null && this.localsToEliminate.Contains(assign.Target.Definition)) continue;
            }
          }          
          statements[j++] = s;
        }
        if (j < n) statements.RemoveRange(j, n-j);
      }
      PatternReplacer.ReplacePushPopPattern(block, this.host);
    }

    public override IExpression Rewrite(IBoundExpression boundExpression) {
      if (this.expressionToSubstituteForSingleUseSingleReferenceLocal != null && boundExpression.Definition == this.currentSingleUseSingleReferenceLocal) {
        if (this.localsToEliminate == null) this.localsToEliminate = new SetOfObjects();
        this.localsToEliminate.Add(this.currentSingleUseSingleReferenceLocal);
        return this.expressionToSubstituteForSingleUseSingleReferenceLocal;
      }
      return base.Rewrite(boundExpression);
    }

    public override IStatement Rewrite(IConditionalStatement conditionalStatement) {
      var result = base.Rewrite(conditionalStatement);
      var mutableConditionalStatement = result as ConditionalStatement;
      if (mutableConditionalStatement == null) return result;
      var expressionPushedByTrueBranch = GetPushedExpressionFrom(conditionalStatement.TrueBranch);
      var expressionPushedByFalseBranch = GetPushedExpressionFrom(conditionalStatement.FalseBranch);
      if (expressionPushedByFalseBranch != null && expressionPushedByTrueBranch != null) {
        return new PushStatement() {
          ValueToPush = TypeInferencer.FixUpType(new Conditional() {
            Condition = conditionalStatement.Condition, ResultIfFalse = expressionPushedByFalseBranch,
            ResultIfTrue = expressionPushedByTrueBranch}),
          Locations = mutableConditionalStatement.Locations
        };
      }
      return result;
    }

    private static IExpression/*?*/ GetPushedExpressionFrom(IStatement statement) {
      var pushStatement = statement as PushStatement;
      if (pushStatement == null) {
        var blockStatement = statement as BlockStatement;
        if (blockStatement == null || blockStatement.Statements.Count != 1) return null;
        pushStatement = blockStatement.Statements[0] as PushStatement;
      }
      if (pushStatement == null) return null;
      return pushStatement.ValueToPush;
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

    public override IExpression Rewrite(IEquality equality) {
      base.Rewrite(equality);
      var cc2 = equality.RightOperand as CompileTimeConstant;
      if (cc2 != null && ExpressionHelper.IsNumericZero(cc2) && equality.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Boolean) {
        return IfThenElseReplacer.InvertCondition(equality.LeftOperand);
      }
      return equality;
    }

    public override IExpression Rewrite(IExpression expression) {
      var result = base.Rewrite(expression);
      this.expressionToSubstituteForSingleUseSingleReferenceLocal = null;
      return result;
    }

    public override IStatement Rewrite(IExpressionStatement expressionStatement) {
      this.currentSingleUseSingleReferenceLocal = null;
      var result = base.Rewrite(expressionStatement);
      if (this.currentSingleUseSingleReferenceLocal != null) {
        var exprStat = result as IExpressionStatement;
        if (exprStat != null) {
          var assign = exprStat.Expression as IAssignment;
          if (assign != null && assign.Target.Definition == this.currentSingleUseSingleReferenceLocal)
            this.expressionToSubstituteForSingleUseSingleReferenceLocal = assign.Source;
        }
      }
      return result;
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
      return base.Rewrite(greaterThan);
    }

    public override IExpression Rewrite(ILessThanOrEqual lessThanOrEqual) {
      var castIfPossible = lessThanOrEqual.LeftOperand as ICastIfPossible;
      if (castIfPossible != null) {
        var compileTimeConstant = lessThanOrEqual.RightOperand as ICompileTimeConstant;
        if (compileTimeConstant != null && compileTimeConstant.Value == null) {
          var locations = lessThanOrEqual.Locations;
          return this.Rewrite(
            new LogicalNot() {
              Locations = locations as List<ILocation> ?? new List<ILocation>(locations),
              Operand = new CheckIfInstance() {
                Operand = castIfPossible.ValueToCast,
                TypeToCheck = castIfPossible.TargetType,
                Type = lessThanOrEqual.Type,
              },
              Type = this.host.PlatformType.SystemBoolean,
            });
        }
      }
      return base.Rewrite(lessThanOrEqual);
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
        Contract.Ensures(Contract.Result<IMethodReference>() != null);
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
