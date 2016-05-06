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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {
  internal class TypeInferencer : CodeTraverser {

    INamedTypeReference containingType;
    IMetadataHost host;
    IPlatformType platformType;

    internal TypeInferencer(INamedTypeReference containingType, IMetadataHost host) {
      Contract.Requires(containingType != null);
      Contract.Requires(host != null);
      this.containingType = containingType;
      this.host = host;
      this.platformType = containingType.PlatformType;
    }

    [ContractInvariantMethod]
    void ObjectInvariant() {
      Contract.Invariant(this.containingType != null);
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.platformType != null);
    }


    private ITypeReference GetBinaryNumericOperationType(IBinaryOperation binaryOperation, bool operandsAreTreatedAsUnsigned) {
      Contract.Requires(binaryOperation != null);
      var result = this.GetBinaryNumericOperationType(binaryOperation);
      if (operandsAreTreatedAsUnsigned) result = TypeHelper.UnsignedEquivalent(result);
      return result;
    }

    private ITypeReference GetBinaryNumericOperationType(IBinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      Contract.Ensures(Contract.Result<ITypeReference>() != null);

      var leftOperand = binaryOperation.LeftOperand;
      var rightOperand = binaryOperation.RightOperand;
      PrimitiveTypeCode leftTypeCode = leftOperand.Type.TypeCode;
      PrimitiveTypeCode rightTypeCode = rightOperand.Type.TypeCode;
      switch (leftTypeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt8:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
              return this.platformType.SystemUInt32;

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
              return this.platformType.SystemUInt32; //code generators will tend to make both operands be of the same type. Assume this happened because the right operand is a polymorphic constant.

            //The cases below are not expected to happen in practice
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Int64:
              return this.platformType.SystemUInt64;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.IntPtr:
              return this.platformType.SystemUIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            default:
              return rightOperand.Type; //code generators will tend to make both operands be of the same type. Assume this happened because the right operand is an enum.
          }

        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
              return this.platformType.SystemUInt32; //code generators will tend to make both operands be of the same type. Assume this happened because the left operand is a polymorphic constant.

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
              return this.platformType.SystemInt32;

            //The cases below are not expected to happen in practice
            case PrimitiveTypeCode.UInt64:
              return this.platformType.SystemUInt64;

            case PrimitiveTypeCode.Int64:
              return this.platformType.SystemInt64;

            case PrimitiveTypeCode.UIntPtr:
              return this.platformType.SystemUIntPtr;

            case PrimitiveTypeCode.IntPtr:
              return this.platformType.SystemIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            default:
              return rightOperand.Type; //code generators will tend to make both operands be of the same type. Assume this happened because the right operand is an enum.
          }

        case PrimitiveTypeCode.UInt64:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt64:
              return this.platformType.SystemUInt64;

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
              return this.platformType.SystemUInt64; //code generators will tend to make both operands be of the same type. Assume this happened because the right operand is a polymorphic constant.

            case PrimitiveTypeCode.UIntPtr:
              return this.platformType.SystemUIntPtr;

            case PrimitiveTypeCode.IntPtr:
              return this.platformType.SystemIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            default:
              return rightOperand.Type; //code generators will tend to make both operands be of the same type. Assume this happened because the right operand is an enum.
          }

        case PrimitiveTypeCode.Int64:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt64:
              return this.platformType.SystemUInt64; //code generators will tend to make both operands be of the same type. Assume this happened because the left operand is a polymorphic constant.

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
              return this.platformType.SystemInt64;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.IntPtr:
              return this.platformType.SystemIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            default:
              return rightOperand.Type; //code generators will tend to make both operands be of the same type. Assume this happened because the right operand is an enum.
          }

        case PrimitiveTypeCode.UIntPtr:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.UIntPtr:
              return this.platformType.SystemUIntPtr;

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.IntPtr:
              return this.platformType.SystemUIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              return rightOperand.Type;

            default:
              Contract.Assume(false);
              return Dummy.TypeReference;
          }

        case PrimitiveTypeCode.IntPtr:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.UIntPtr:
              return this.platformType.SystemUIntPtr;

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.IntPtr:
              return this.platformType.SystemIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              return rightOperand.Type;

            default:
              Contract.Assume(false);
              return Dummy.TypeReference;
          }

        case PrimitiveTypeCode.Float32:
        case PrimitiveTypeCode.Float64:
          return rightOperand.Type;

        case PrimitiveTypeCode.Pointer:
        case PrimitiveTypeCode.Reference:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              return this.platformType.SystemUIntPtr;
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              return leftOperand.Type;
            default:
              Contract.Assume(false);
              return Dummy.TypeReference;
          }

        default:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt64:
              //assume that the left operand has an enum type.
              return leftOperand.Type;
          }
          return leftOperand.Type; //assume they are both enums
      }
    }

    private ITypeReference GetBitwiseOperationType(IBinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      Contract.Ensures(Contract.Result<ITypeReference>() != null);

      if (binaryOperation.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Boolean && binaryOperation.RightOperand.Type.TypeCode == PrimitiveTypeCode.Boolean)
        return this.platformType.SystemBoolean;
      return this.GetBinaryNumericOperationType(binaryOperation);
    }

    public override void TraverseChildren(IAddition addition) {
      base.TraverseChildren(addition);
      ((Addition)addition).Type = this.GetBinaryNumericOperationType(addition, addition.TreatOperandsAsUnsignedIntegers);
    }

    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      base.TraverseChildren(addressableExpression);
      ITypeReference type = Dummy.TypeReference;
      ILocalDefinition/*?*/ local = addressableExpression.Definition as ILocalDefinition;
      if (local != null)
        type = local.Type;
      else {
        IParameterDefinition/*?*/ parameter = addressableExpression.Definition as IParameterDefinition;
        if (parameter != null) {
          type = parameter.Type;
          if (parameter.IsByReference)
            type = Immutable.ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory);
        } else {
          IFieldReference/*?*/ field = addressableExpression.Definition as IFieldReference;
          if (field != null)
            type = field.Type;
          else {
            IExpression/*?*/ expression = addressableExpression.Definition as IExpression;
            if (expression != null)
              type = expression.Type;
          }
        }
      }
      ((AddressableExpression)addressableExpression).Type = type;
    }

    public override void TraverseChildren(IAddressOf addressOf) {
      base.TraverseChildren(addressOf);
      ITypeReference targetType = addressOf.Expression.Type;
      if (targetType is Dummy) {
        IMethodReference/*?*/ method = addressOf.Expression.Definition as IMethodReference;
        if (method != null) {
          ((AddressOf)addressOf).Type = new Immutable.FunctionPointerType(method.CallingConvention, method.ReturnValueIsByRef, method.Type,
            method.ReturnValueIsModified ? method.ReturnValueCustomModifiers : null, method.Parameters, null, this.host.InternFactory);
          return;
        }
      }
      ((AddressOf)addressOf).Type = Immutable.ManagedPointerType.GetManagedPointerType(targetType, this.host.InternFactory);
    }

    public override void TraverseChildren(IAddressDereference addressDereference) {
      base.TraverseChildren(addressDereference);
      IPointerTypeReference/*?*/ pointerTypeReference = addressDereference.Address.Type as IPointerTypeReference;
      if (pointerTypeReference != null) {
        if (!(pointerTypeReference.TargetType is Dummy)) {
          if (addressDereference.Type is Dummy)
            ((AddressDereference)addressDereference).Type = pointerTypeReference.TargetType;
          else if (!TypeHelper.TypesAreEquivalent(addressDereference.Type, pointerTypeReference.TargetType)) {
            var targetPointerType = Immutable.PointerType.GetPointerType(addressDereference.Type, this.host.InternFactory);
            ((AddressDereference)addressDereference).Address = new Conversion() {
              ValueToConvert = addressDereference.Address, TypeAfterConversion = targetPointerType, Type = targetPointerType
            };
          }
          return;
        }
      }
      IManagedPointerTypeReference/*?*/ managedPointerTypeReference = addressDereference.Address.Type as IManagedPointerTypeReference;
      if (managedPointerTypeReference != null) {
        if (!(managedPointerTypeReference.TargetType is Dummy)) {
          if (addressDereference.Type is Dummy)
            ((AddressDereference)addressDereference).Type = managedPointerTypeReference.TargetType;
          else if (!TypeHelper.TypesAreEquivalent(addressDereference.Type, managedPointerTypeReference.TargetType)) {
            if (managedPointerTypeReference.TargetType.TypeCode == PrimitiveTypeCode.Boolean && addressDereference.Type.TypeCode == PrimitiveTypeCode.Int8)
              ((AddressDereference)addressDereference).Type = managedPointerTypeReference.TargetType;
            else {
              var targetPointerType = Immutable.ManagedPointerType.GetManagedPointerType(addressDereference.Type, this.host.InternFactory);
              ((AddressDereference)addressDereference).Address = new Conversion() {
                ValueToConvert = addressDereference.Address, TypeAfterConversion = targetPointerType, Type = targetPointerType
              };
            }
          }
          return;
        }
      }
    }

    public override void TraverseChildren(IArrayIndexer arrayIndexer) {
      base.TraverseChildren(arrayIndexer);
      IArrayTypeReference/*?*/ arrayType = arrayIndexer.IndexedObject.Type as IArrayTypeReference;
      if (arrayType == null) return;
      ((ArrayIndexer)arrayIndexer).Type = arrayType.ElementType;
    }

    public override void TraverseChildren(IAssignment assignment) {
      base.TraverseChildren(assignment);
      ((Assignment)assignment).Source = Convert(assignment.Source, assignment.Target.Type);
      ((Assignment)assignment).Type = assignment.Target.Type;
    }

    public override void TraverseChildren(IBinaryOperation binaryOperation) {
      base.TraverseChildren(binaryOperation);
      if (binaryOperation.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Char) {
        if (binaryOperation.RightOperand.Type.TypeCode == PrimitiveTypeCode.Int32)
          ((BinaryOperation)binaryOperation).RightOperand = new Conversion() {
            ValueToConvert = binaryOperation.RightOperand,
            Type = binaryOperation.LeftOperand.Type, TypeAfterConversion = binaryOperation.LeftOperand.Type
          };
      } else if (binaryOperation.RightOperand.Type.TypeCode == PrimitiveTypeCode.Char) {
        if (binaryOperation.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Int32)
          ((BinaryOperation)binaryOperation).LeftOperand = new Conversion() {
            ValueToConvert = binaryOperation.LeftOperand,
            Type = binaryOperation.RightOperand.Type, TypeAfterConversion = binaryOperation.RightOperand.Type
          };
      }
    }

    public override void TraverseChildren(IBitwiseAnd bitwiseAnd) {
      base.TraverseChildren(bitwiseAnd);
      ((BitwiseAnd)bitwiseAnd).Type = this.GetBitwiseOperationType(bitwiseAnd);
    }

    public override void TraverseChildren(IBitwiseOr bitwiseOr) {
      base.TraverseChildren(bitwiseOr);
      ((BitwiseOr)bitwiseOr).Type = this.GetBitwiseOperationType(bitwiseOr);
    }

    public override void TraverseChildren(IBlockExpression blockExpression) {
      base.TraverseChildren(blockExpression);
      ((BlockExpression)blockExpression).Type = blockExpression.Expression.Type;
    }

    public override void TraverseChildren(IBoundExpression boundExpression) {
      base.TraverseChildren(boundExpression);
      ITypeReference type = Dummy.TypeReference;
      ILocalDefinition/*?*/ local = boundExpression.Definition as ILocalDefinition;
      if (local != null) {
        type = local.Type;
        if (local.IsReference) {
          if (local.IsPinned) {
            type = Immutable.PointerType.GetPointerType(type, this.host.InternFactory);
          } else {
            type = Immutable.ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory);
          }
        }
      } else {
        IParameterDefinition/*?*/ parameter = boundExpression.Definition as IParameterDefinition;
        if (parameter != null) {
          type = parameter.Type;
          if (parameter.IsByReference)
            type = Immutable.ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory);
        } else {
          IFieldReference/*?*/ field = boundExpression.Definition as IFieldReference;
          if (field != null)
            type = field.Type;
        }
      }
      ((BoundExpression)boundExpression).Type = type;
    }

    public override void TraverseChildren(ICastIfPossible castIfPossible) {
      base.TraverseChildren(castIfPossible);
      ((CastIfPossible)castIfPossible).Type = castIfPossible.TargetType;
    }

    public override void TraverseChildren(ICheckIfInstance checkIfInstance) {
      base.TraverseChildren(checkIfInstance);
      ((CheckIfInstance)checkIfInstance).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(IConversion conversion) {
      base.TraverseChildren(conversion);
      Conversion/*?*/ conv = conversion as Conversion;
      if (conv != null) {
        if (conv.TypeAfterConversion.TypeCode == PrimitiveTypeCode.IntPtr || conv.Type.TypeCode == PrimitiveTypeCode.UIntPtr) {
          if (conv.ValueToConvert.Type is IPointerTypeReference || conv.ValueToConvert.Type is IManagedPointerTypeReference) {
            conv.Type = conv.TypeAfterConversion = conv.ValueToConvert.Type;
            //TODO: hmm. Perhaps this is all wrong. In IL the point of the conversion is to "forget" that the pointer has type conv.ValueToConvert.Type.
            //so that the verifier will allow the pointer to be abused as a pointer to something else
            return;
          }
        }
        conv.Type = conversion.TypeAfterConversion;
      }
    }

    public override void TraverseChildren(ICompileTimeConstant constant) {
      Contract.Assume(!(constant.Type is Dummy));
      //The type should already be filled in
    }

    public override void TraverseChildren(IConditional conditional) {
      base.TraverseChildren(conditional);
      FixUpType((Conditional)conditional);
    }

    internal static Conditional FixUpType(Conditional conditional) {
      Contract.Requires(conditional != null);
      conditional.Condition = ConvertToBoolean(conditional.Condition);
      var mergedType = conditional.ResultIfTrue.Type;
      if (!TypeHelper.TypesAreEquivalent(conditional.ResultIfTrue.Type, conditional.ResultIfFalse.Type)) {
        mergedType = TypeHelper.MergedType(TypeHelper.StackType(conditional.ResultIfTrue.Type), TypeHelper.StackType(conditional.ResultIfFalse.Type));
        if (mergedType.TypeCode == PrimitiveTypeCode.Int32) {
          if (conditional.ResultIfTrue.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            conditional.ResultIfFalse = ConvertToBoolean(conditional.ResultIfFalse);
            mergedType = conditional.ResultIfTrue.Type;
          } else if (conditional.ResultIfFalse.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            conditional.ResultIfTrue = ConvertToBoolean(conditional.ResultIfTrue);
            mergedType = conditional.ResultIfFalse.Type;
          }
        }
      }
      conditional.Type = mergedType;
      return conditional;
    }

    public override void TraverseChildren(IConditionalStatement conditionalStatement) {
      base.TraverseChildren(conditionalStatement);
      var condStat = (ConditionalStatement)conditionalStatement;
      condStat.Condition = ConvertToBoolean(condStat.Condition);
    }

    internal static IExpression Convert(IExpression expression, ITypeReference targetType) {
      Contract.Requires(expression != null);
      Contract.Requires(targetType != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var expressionType = expression.Type;
      if (TypeHelper.TypesAreEquivalent(expressionType, targetType)) return expression;
      Contract.Assume(!(targetType is Dummy)); //Nice check, but too onerous for client to prove statically.
      if (targetType.TypeCode == PrimitiveTypeCode.Boolean && TypeHelper.IsPrimitiveInteger(expressionType))
        return ConvertToBoolean(expression);
      if (targetType.TypeCode == PrimitiveTypeCode.Char && expressionType.TypeCode == PrimitiveTypeCode.Int32)
        return ConvertToCharacter(expression);
      if (targetType.TypeCode == PrimitiveTypeCode.UInt32 && expressionType.TypeCode == PrimitiveTypeCode.Int32)
        return new Conversion() { ValueToConvert = expression, TypeAfterConversion = targetType, Type = targetType };
      if (targetType.TypeCode == PrimitiveTypeCode.UInt64 && expressionType.TypeCode == PrimitiveTypeCode.Int64)
        return new Conversion() { ValueToConvert = expression, TypeAfterConversion = targetType, Type = targetType };
      if (targetType is IPointerTypeReference || targetType is IManagedPointerTypeReference || targetType is IFunctionPointerTypeReference) {
        if (expressionType.TypeCode == PrimitiveTypeCode.UIntPtr || expressionType.TypeCode == PrimitiveTypeCode.IntPtr || TypeHelper.IsPrimitiveInteger(expressionType) || 
        expressionType is IPointerTypeReference || expressionType is IManagedPointerTypeReference || expressionType is IFunctionPointerTypeReference)
          return new Conversion() { ValueToConvert = expression, TypeAfterConversion = targetType, Type = targetType };
      }
      if (expression is IPopValue) return new PopValue() { Type = targetType };
      if (expression is IDupValue) return new DupValue() { Type = targetType };

      // Null is polymorphic in IL: it can be assigned to any reference type. But we want assignments to not depend on implicit conversions.
      var ctc = expression as ICompileTimeConstant;
      if (ctc != null && ctc.Value == null && !TypeHelper.TypesAreEquivalent(targetType, targetType.PlatformType.SystemObject))
        return new Conversion() { ValueToConvert = expression, TypeAfterConversion = targetType, Type = targetType };

      return expression;
    }

    private static IExpression ConvertToBoolean(IExpression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      IPlatformType platformType = expression.Type.PlatformType;
      var cc = expression as CompileTimeConstant;
      if (cc != null && TypeHelper.IsPrimitiveInteger(cc.Type)) {
        cc.Value = !ExpressionHelper.IsIntegralZero(cc);
        cc.Type = platformType.SystemBoolean;
        return cc;
      }
      var conditional = expression as Conditional;
      if (conditional != null) {
        conditional.ResultIfTrue = ConvertToBoolean(conditional.ResultIfTrue);
        conditional.ResultIfFalse = ConvertToBoolean(conditional.ResultIfFalse);
        conditional.Type = platformType.SystemBoolean;
        return conditional;
      }
      object/*?*/ val = null;
      ITypeReference type = platformType.SystemObject;
      ITypeReference expressionType = expression.Type;
      IExpression rightOperand = null; // zero or null, but has to be type-specific
      switch (expressionType.TypeCode) {
        case PrimitiveTypeCode.Boolean: {
            var addrDeref = expression as AddressDereference;
            Conversion conversion;
            IManagedPointerTypeReference mgdPtr;
            if (addrDeref != null && (conversion = addrDeref.Address as Conversion) != null && 
              (mgdPtr = conversion.ValueToConvert.Type as IManagedPointerTypeReference) != null) {
              expressionType = mgdPtr.TargetType;
              addrDeref.Address = conversion.ValueToConvert;
              addrDeref.Type = expressionType;
              expression = addrDeref;
              goto default;
            }
            return expression;
          }
        case PrimitiveTypeCode.Char: val = (char)0; type = platformType.SystemChar; break;
        case PrimitiveTypeCode.Float32: val = (float)0; type = platformType.SystemFloat32; break;
        case PrimitiveTypeCode.Float64: val = (double)0; type = platformType.SystemFloat64; break;
        case PrimitiveTypeCode.Int16: val = (short)0; type = platformType.SystemInt16; break;
        case PrimitiveTypeCode.Int32: val = (int)0; type = platformType.SystemInt32; break;
        case PrimitiveTypeCode.Int64: val = (long)0; type = platformType.SystemInt64; break;
        case PrimitiveTypeCode.Int8: val = (sbyte)0; type = platformType.SystemInt8; break;
        case PrimitiveTypeCode.IntPtr: val = IntPtr.Zero; type = platformType.SystemIntPtr; break;
        case PrimitiveTypeCode.UInt16: val = (ushort)0; type = platformType.SystemUInt16; break;
        case PrimitiveTypeCode.UInt32: val = (uint)0; type = platformType.SystemUInt32; break;
        case PrimitiveTypeCode.UInt64: val = (ulong)0; type = platformType.SystemUInt64; break;
        case PrimitiveTypeCode.UInt8: val = (byte)0; type = platformType.SystemUInt8; break;
        case PrimitiveTypeCode.UIntPtr: val = UIntPtr.Zero; type = platformType.SystemUIntPtr; break;
        default:
          rightOperand = new DefaultValue() {
            DefaultValueType = expressionType,
            Type = expressionType,
          };
          break;
      }
      if (rightOperand == null) {
        rightOperand = new CompileTimeConstant() {
          Value = val,
          Type = type,
        };
      }
      NotEquality result = new NotEquality() {
        LeftOperand = expression,
        RightOperand = rightOperand,
        Type = platformType.SystemBoolean,
      };
      return result;
    }

    private static IExpression ConvertToCharacter(IExpression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      IPlatformType platformType = expression.Type.PlatformType;
      var cc = expression as CompileTimeConstant;
      if (cc != null && cc.Value is int) {
        cc.Value = (char)(int)cc.Value;
        cc.Type = platformType.SystemChar;
        return cc;
      }
      return expression;
    }

    public override void TraverseChildren(ICreateArray createArray) {
      base.TraverseChildren(createArray);
      IArrayTypeReference arrayType;
      if (createArray.Rank == 1 && IteratorHelper.EnumerableIsEmpty(createArray.LowerBounds))
        arrayType = Immutable.Vector.GetVector(createArray.ElementType, this.host.InternFactory);
      else
        arrayType = Immutable.Matrix.GetMatrix(createArray.ElementType, createArray.Rank, this.host.InternFactory);
      ((CreateArray)createArray).Type = arrayType;

    }

    public override void TraverseChildren(ICreateDelegateInstance createDelegateInstance) {
      //The type should already be filled in
    }

    public override void TraverseChildren(ICreateObjectInstance createObjectInstance) {
      base.TraverseChildren(createObjectInstance);
      var ps = new List<IParameterTypeInformation>(createObjectInstance.MethodToCall.Parameters);
      int i = 0;
      foreach (var a in createObjectInstance.Arguments) {
        Contract.Assume(i < ps.Count); //assume that argument count matches the parameter count. I.e. assume the IL being decompiled is valid.
        var p = ps[i++];
        Contract.Assume(p != null);
        var ctc = a as CompileTimeConstant;
        if (ctc == null) continue;
        if (p.Type.TypeCode == PrimitiveTypeCode.Boolean && ctc.Type.TypeCode == PrimitiveTypeCode.Int32) {
          Contract.Assume(ctc.Value != null && ctc.Value is int);
          ctc.Value = ((int)ctc.Value) == 0 ? false : true;
          ctc.Type = this.host.PlatformType.SystemBoolean;
        }
      }
      ((CreateObjectInstance)createObjectInstance).Type = createObjectInstance.MethodToCall.ContainingType;
    }

    public override void TraverseChildren(IDefaultValue defaultValue) {
      base.TraverseChildren(defaultValue);
      ((DefaultValue)defaultValue).Type = defaultValue.DefaultValueType;
    }

    public override void TraverseChildren(IDivision division) {
      base.TraverseChildren(division);
      ((Division)division).Type = this.GetBinaryNumericOperationType(division, division.TreatOperandsAsUnsignedIntegers);
    }

    public override void TraverseChildren(IEquality equality) {
      base.TraverseChildren(equality);
      ((Equality)equality).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(IExclusiveOr exclusiveOr) {
      base.TraverseChildren(exclusiveOr);
      ((ExclusiveOr)exclusiveOr).Type = this.GetBitwiseOperationType(exclusiveOr);
    }

    public override void TraverseChildren(IGetTypeOfTypedReference getTypeOfTypedReference) {
      base.TraverseChildren(getTypeOfTypedReference);
      ((GetTypeOfTypedReference)getTypeOfTypedReference).Type = this.platformType.SystemType;
    }

    public override void TraverseChildren(IGetValueOfTypedReference getValueOfTypedReference) {
      base.TraverseChildren(getValueOfTypedReference);
      var type = getValueOfTypedReference.TargetType;
      if (type.IsValueType) type = Immutable.ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory);
      ((GetValueOfTypedReference)getValueOfTypedReference).Type = type;
    }

    public override void TraverseChildren(IGreaterThan greaterThan) {
      base.TraverseChildren(greaterThan);
      ((GreaterThan)greaterThan).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(IGreaterThanOrEqual greaterThanOrEqual) {
      base.TraverseChildren(greaterThanOrEqual);
      ((GreaterThanOrEqual)greaterThanOrEqual).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(ILeftShift leftShift) {
      base.TraverseChildren(leftShift);
      ((LeftShift)leftShift).Type = leftShift.LeftOperand.Type;
    }

    public override void TraverseChildren(ILessThan lessThan) {
      base.TraverseChildren(lessThan);
      ((LessThan)lessThan).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(ILessThanOrEqual lessThanOrEqual) {
      base.TraverseChildren(lessThanOrEqual);
      ((LessThanOrEqual)lessThanOrEqual).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(ILogicalNot logicalNot) {
      base.TraverseChildren(logicalNot);
      ((LogicalNot)logicalNot).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(IMakeTypedReference makeTypedReference) {
      base.TraverseChildren(makeTypedReference);
      ((MakeTypedReference)makeTypedReference).Type = this.platformType.SystemTypedReference;
    }

    public override void TraverseChildren(IMetadataCreateArray createArray) {
      //The type should already be filled in
    }

    public override void TraverseChildren(IMetadataConstant constant) {
      //The type should already be filled in
    }

    public override void TraverseChildren(IMetadataTypeOf typeOf) {
      //The type should already be filled in
    }

    public override void TraverseChildren(IMetadataNamedArgument namedArgument) {
      //The type should already be filled in
    }

    public override void TraverseChildren(IMethodCall methodCall) {
      base.TraverseChildren(methodCall);
      var ps = new List<IParameterTypeInformation>(methodCall.MethodToCall.Parameters);
      var args = new List<IExpression>(methodCall.Arguments);
      for (int i = 0, n = args.Count; i < n; i++) {
        Contract.Assume(args[i] != null);
        Contract.Assume(i < ps.Count);
        Contract.Assume(ps[i] != null);
        args[i] = Convert(args[i], ps[i].Type);
      }
      ((MethodCall)methodCall).Type = methodCall.MethodToCall.Type;
    }

    public override void TraverseChildren(IModulus modulus) {
      base.TraverseChildren(modulus);
      ((Modulus)modulus).Type = this.GetBinaryNumericOperationType(modulus, modulus.TreatOperandsAsUnsignedIntegers);
    }

    public override void TraverseChildren(IMultiplication multiplication) {
      base.TraverseChildren(multiplication);
      ((Multiplication)multiplication).Type = this.GetBinaryNumericOperationType(multiplication, multiplication.TreatOperandsAsUnsignedIntegers);
    }

    public override void TraverseChildren(INamedArgument namedArgument) {
      //TODO: get rid of INamedArgument
    }

    public override void TraverseChildren(INotEquality notEquality) {
      base.TraverseChildren(notEquality);
      ((NotEquality)notEquality).Type = this.platformType.SystemBoolean;
    }

    public override void TraverseChildren(IOldValue oldValue) {
      base.TraverseChildren(oldValue);
      ((OldValue)oldValue).Type = oldValue.Expression.Type;
    }

    public override void TraverseChildren(IOnesComplement onesComplement) {
      base.TraverseChildren(onesComplement);
      ((OnesComplement)onesComplement).Type = onesComplement.Operand.Type;
    }

    public override void TraverseChildren(IOutArgument outArgument) {
      base.TraverseChildren(outArgument);
      ((OutArgument)outArgument).Type = outArgument.Type;
    }

    public override void TraverseChildren(IPointerCall pointerCall) {
      IFunctionPointerTypeReference pointerType = (IFunctionPointerTypeReference)pointerCall.Pointer.Type;
      this.Traverse(pointerCall.Pointer);
      ((Expression)pointerCall.Pointer).Type = pointerType;
      this.Traverse(pointerCall.Arguments);
      ((PointerCall)pointerCall).Type = pointerType.Type;
    }

    public override void TraverseChildren(IRefArgument refArgument) {
      base.TraverseChildren(refArgument);
      ((RefArgument)refArgument).Type = refArgument.Expression.Type;
    }

    public override void TraverseChildren(IReturnValue returnValue) {
      base.TraverseChildren(returnValue);
      ((ReturnValue)returnValue).Type = this.containingType;
    }

    public override void TraverseChildren(IRightShift rightShift) {
      base.TraverseChildren(rightShift);
      ((RightShift)rightShift).Type = rightShift.LeftOperand.Type;
    }

    public override void TraverseChildren(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      base.TraverseChildren(runtimeArgumentHandleExpression);
      ((RuntimeArgumentHandleExpression)runtimeArgumentHandleExpression).Type = this.platformType.SystemRuntimeArgumentHandle;
    }

    public override void TraverseChildren(ISizeOf sizeOf) {
      base.TraverseChildren(sizeOf);
      ((SizeOf)sizeOf).Type = this.platformType.SystemInt32;
    }

    public override void TraverseChildren(IStackArrayCreate stackArrayCreate) {
      base.TraverseChildren(stackArrayCreate);
      ((StackArrayCreate)stackArrayCreate).Type = this.platformType.SystemIntPtr;
    }

    public override void TraverseChildren(ISubtraction subtraction) {
      base.TraverseChildren(subtraction);
      ((Subtraction)subtraction).Type = this.GetBinaryNumericOperationType(subtraction, subtraction.TreatOperandsAsUnsignedIntegers);
    }

    public override void TraverseChildren(ITargetExpression targetExpression) {
      base.TraverseChildren(targetExpression);
      ITypeReference type = Dummy.TypeReference;
      ILocalDefinition/*?*/ local = targetExpression.Definition as ILocalDefinition;
      if (local != null) {
        if (local.IsReference) {
          if (local.IsPinned)
            type = Immutable.PointerType.GetPointerType(local.Type, this.host.InternFactory);
          else
            type = Immutable.ManagedPointerType.GetManagedPointerType(local.Type, this.host.InternFactory);
        } else
          type = local.Type;
      } else {
        IParameterDefinition/*?*/ parameter = targetExpression.Definition as IParameterDefinition;
        if (parameter != null)
          type = parameter.Type;
        else {
          IFieldReference/*?*/ field = targetExpression.Definition as IFieldReference;
          if (field != null)
            type = field.Type;
          else {
            IPropertyDefinition/*?*/ property = targetExpression.Definition as IPropertyDefinition;
            if (property != null)
              type = property.Type;
            else {
              IExpression/*?*/ expression = targetExpression.Definition as IExpression;
              if (expression != null)
                type = expression.Type;
            }
          }
        }
      }
      ((TargetExpression)targetExpression).Type = type;
    }

    public override void TraverseChildren(IThisReference thisReference) {
      base.TraverseChildren(thisReference);
      var typeForThis = this.containingType.ResolvedType;
      if (typeForThis.IsValueType)
        ((ThisReference)thisReference).Type = Immutable.ManagedPointerType.GetManagedPointerType(NamedTypeDefinition.SelfInstance(typeForThis, this.host.InternFactory), this.host.InternFactory);
      else
        ((ThisReference)thisReference).Type = NamedTypeDefinition.SelfInstance(typeForThis, this.host.InternFactory);
    }

    public override void TraverseChildren(ITokenOf tokenOf) {
      base.TraverseChildren(tokenOf);
      ITypeReference type;
      IFieldReference/*?*/ field = tokenOf.Definition as IFieldReference;
      if (field != null)
        type = this.platformType.SystemRuntimeFieldHandle;
      else {
        IMethodReference/*?*/ method = tokenOf.Definition as IMethodReference;
        if (method != null)
          type = this.platformType.SystemRuntimeMethodHandle;
        else {
          Contract.Assume(tokenOf.Definition is ITypeReference);
          type = this.platformType.SystemRuntimeTypeHandle;
        }
      }
      ((TokenOf)tokenOf).Type = type;
    }

    public override void TraverseChildren(ITypeOf typeOf) {
      base.TraverseChildren(typeOf);
      ((TypeOf)typeOf).Type = this.platformType.SystemType;
    }

    public override void TraverseChildren(IUnaryNegation unaryNegation) {
      base.TraverseChildren(unaryNegation);
      ((UnaryNegation)unaryNegation).Type = unaryNegation.Operand.Type;
    }

    public override void TraverseChildren(IUnaryPlus unaryPlus) {
      base.TraverseChildren(unaryPlus);
      ((UnaryPlus)unaryPlus).Type = unaryPlus.Operand.Type;
    }

    public override void TraverseChildren(IVectorLength vectorLength) {
      base.TraverseChildren(vectorLength);
      ((VectorLength)vectorLength).Type = this.platformType.SystemIntPtr;
    }
  }

}