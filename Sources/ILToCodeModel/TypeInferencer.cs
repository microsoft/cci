//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Cci.ILToCodeModel {

  internal class TypeInferencer : BaseCodeTraverser {

    ITypeReference containingType;
    IMetadataHost host;
    IPlatformType platformType;

    internal TypeInferencer(ITypeReference containingType, IMetadataHost host) {
      this.containingType = containingType;
      this.host = host;
      this.platformType = containingType.PlatformType;
    }

    private ITypeReference GetBinaryNumericOperationType(IBinaryOperation binaryOperation) {
      PrimitiveTypeCode leftTypeCode = binaryOperation.LeftOperand.Type.TypeCode;
      PrimitiveTypeCode rightTypeCode = binaryOperation.RightOperand.Type.TypeCode;
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
              return this.platformType.SystemInt32;

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
              return binaryOperation.RightOperand.Type;
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
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
              return this.platformType.SystemInt32;

            case PrimitiveTypeCode.UInt64:
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
              return binaryOperation.RightOperand.Type;
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
              return binaryOperation.RightOperand.Type;
          }

        case PrimitiveTypeCode.Int64:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt64:
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
              return binaryOperation.RightOperand.Type;
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
              return this.platformType.SystemIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            default:
              return binaryOperation.RightOperand.Type;
          }

        case PrimitiveTypeCode.IntPtr:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt64:
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.IntPtr:
              return this.platformType.SystemIntPtr;

            case PrimitiveTypeCode.Float32:
              return this.platformType.SystemFloat32;

            case PrimitiveTypeCode.Float64:
              return this.platformType.SystemFloat64;

            default:
              return binaryOperation.RightOperand.Type;
          }

        case PrimitiveTypeCode.Float32:
        case PrimitiveTypeCode.Float64:
          return binaryOperation.RightOperand.Type;

        case PrimitiveTypeCode.Pointer:
        case PrimitiveTypeCode.Reference:
          switch (rightTypeCode) {
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              return this.platformType.SystemIntPtr;
            default:
              return binaryOperation.LeftOperand.Type;
          }

        default:
          return binaryOperation.RightOperand.Type;
      }
    }

    public override void Visit(IAddition addition) {
      base.Visit(addition);
      ((Addition)addition).Type = this.GetBinaryNumericOperationType(addition);
    }

    public override void Visit(IAddressableExpression addressableExpression) {
      base.Visit(addressableExpression);
      ITypeReference type = Dummy.TypeReference;
      ILocalDefinition/*?*/ local = addressableExpression.Definition as ILocalDefinition;
      if (local != null)
        type = local.Type;
      else {
        IParameterDefinition/*?*/ parameter = addressableExpression.Definition as IParameterDefinition;
        if (parameter != null)
          type = parameter.Type;
        else {
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

    public override void Visit(IAddressOf addressOf) {
      base.Visit(addressOf);
      ITypeReference targetType = addressOf.Expression.Type;
      if (targetType == Dummy.TypeReference) {
        IMethodReference/*?*/ method = addressOf.Expression.Definition as IMethodReference;
        if (method != null) {
          ((AddressOf)addressOf).Type = new FunctionPointerType(method.CallingConvention, method.ReturnValueIsByRef, method.Type,
            method.ReturnValueIsModified ? method.ReturnValueCustomModifiers : null, method.Parameters, null, this.host.InternFactory);
          return;
        }
      }
      ((AddressOf)addressOf).Type = ManagedPointerType.GetManagedPointerType(targetType, this.host.InternFactory);
    }

    public override void Visit(IAddressDereference addressDereference) {
      base.Visit(addressDereference);
      IPointerTypeReference/*?*/ pointerTypeReference = addressDereference.Address.Type as IPointerTypeReference;
      if (pointerTypeReference != null) {
        ((AddressDereference)addressDereference).Type = pointerTypeReference.TargetType;
        return;
      }
      IManagedPointerTypeReference/*?*/ managedPointerTypeReference = addressDereference.Address.Type as IManagedPointerTypeReference;
      if (managedPointerTypeReference != null) {
        ((AddressDereference)addressDereference).Type = managedPointerTypeReference.TargetType;
        return;
      }
      //TODO: error
    }

    public override void Visit(IArrayIndexer arrayIndexer) {
      base.Visit(arrayIndexer);
      IArrayTypeReference/*?*/ arrayType = arrayIndexer.IndexedObject.Type as IArrayTypeReference;
      if (arrayType == null) return;
      ((ArrayIndexer)arrayIndexer).Type = arrayType.ElementType;
    }

    public override void Visit(IAssignment assignment) {
      base.Visit(assignment);
      ((Assignment)assignment).Type = assignment.Target.Type;
    }

    public override void Visit(IBaseClassReference baseClassReference) {
      base.Visit(baseClassReference);
      ITypeReference type = this.containingType;
      foreach (ITypeReference baseClass in type.ResolvedType.BaseClasses) {
        type = baseClass;
        break;
      }
      ((BaseClassReference)baseClassReference).Type = type;
    }

    public override void Visit(IBitwiseAnd bitwiseAnd) {
      base.Visit(bitwiseAnd);
      ((BitwiseAnd)bitwiseAnd).Type = this.GetBinaryNumericOperationType(bitwiseAnd);
    }

    public override void Visit(IBitwiseOr bitwiseOr) {
      base.Visit(bitwiseOr);
      ((BitwiseOr)bitwiseOr).Type = this.GetBinaryNumericOperationType(bitwiseOr);
    }

    public override void Visit(IBlockExpression blockExpression) {
      base.Visit(blockExpression);
      ((BlockExpression)blockExpression).Type = blockExpression.Expression.Type;
    }

    public override void Visit(IBoundExpression boundExpression) {
      base.Visit(boundExpression);
      ITypeReference type = Dummy.TypeReference;
      ILocalDefinition/*?*/ local = boundExpression.Definition as ILocalDefinition;
      if (local != null) {
        type = local.Type;
        if (local.IsReference)
          type = ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory);
      } else {
        IParameterDefinition/*?*/ parameter = boundExpression.Definition as IParameterDefinition;
        if (parameter != null) {
          type = parameter.Type;
          if (parameter.IsByReference)
            type = ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory);
        } else {
          IFieldReference/*?*/ field = boundExpression.Definition as IFieldReference;
          if (field != null)
            type = field.Type;
        }
      }
      ((BoundExpression)boundExpression).Type = type;
    }

    public override void Visit(ICastIfPossible castIfPossible) {
      base.Visit(castIfPossible);
      ((CastIfPossible)castIfPossible).Type = castIfPossible.TargetType;
    }

    public override void Visit(ICheckIfInstance checkIfInstance) {
      base.Visit(checkIfInstance);
      ((CheckIfInstance)checkIfInstance).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(IConversion conversion) {
      base.Visit(conversion);
      Conversion conv = (Conversion)conversion;
      if (conv.TypeAfterConversion.TypeCode == PrimitiveTypeCode.IntPtr || conv.Type.TypeCode == PrimitiveTypeCode.UIntPtr) {
        if (conv.ValueToConvert.Type is IPointerTypeReference || conv.ValueToConvert.Type is IManagedPointerTypeReference) {
          conv.Type = conv.ValueToConvert.Type;
          return;
        }
      }
      conv.Type = conversion.TypeAfterConversion;
    }

    public override void Visit(ICompileTimeConstant constant) {
      Debug.Assert(constant.Type != Dummy.TypeReference);
      //The type should already be filled in
    }

    public override void Visit(IConditional conditional) {
      base.Visit(conditional);
      Conditional cond = (Conditional)conditional;
      cond.Condition = ConvertToBoolean(cond.Condition);
      cond.Type = conditional.ResultIfTrue.Type;
    }

    private static IExpression ConvertToBoolean(IExpression expression) {
      object/*?*/ val = null;
      IPlatformType platformType = expression.Type.PlatformType;
      ITypeReference type = platformType.SystemObject;
      ITypeReference expressionType = expression.Type;
      IExpression rightOperand = null; // zero or null, but has to be type-specific
      switch (expressionType.TypeCode) {
        case PrimitiveTypeCode.Boolean: return expression;
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

    public override void Visit(ICreateArray createArray) {
      base.Visit(createArray);
      IArrayTypeReference arrayType;
      if (createArray.Rank == 1 && IteratorHelper.EnumerableIsEmpty(createArray.LowerBounds))
        arrayType = Vector.GetVector(createArray.ElementType, this.host.InternFactory);
      else
        arrayType = Matrix.GetMatrix(createArray.ElementType, createArray.Rank, this.host.InternFactory);
      ((CreateArray)createArray).Type = arrayType;
    }

    public override void Visit(ICreateDelegateInstance createDelegateInstance) {
      //The type should already be filled in
    }

    public override void Visit(ICreateObjectInstance createObjectInstance) {
      base.Visit(createObjectInstance);
      ((CreateObjectInstance)createObjectInstance).Type = createObjectInstance.MethodToCall.ContainingType;
    }

    public override void Visit(IDefaultValue defaultValue) {
      base.Visit(defaultValue);
      ((DefaultValue)defaultValue).Type = defaultValue.DefaultValueType;
    }

    public override void Visit(IDivision division) {
      base.Visit(division);
      ((Division)division).Type = this.GetBinaryNumericOperationType(division);
    }

    public override void Visit(IEquality equality) {
      base.Visit(equality);
      ((Equality)equality).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(IExclusiveOr exclusiveOr) {
      base.Visit(exclusiveOr);
      ((ExclusiveOr)exclusiveOr).Type = this.GetBinaryNumericOperationType(exclusiveOr);
    }

    public override void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
      base.Visit(getTypeOfTypedReference);
      ((GetTypeOfTypedReference)getTypeOfTypedReference).Type = this.platformType.SystemType;
    }

    public override void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
      base.Visit(getValueOfTypedReference);
      ((GetValueOfTypedReference)getValueOfTypedReference).Type = getValueOfTypedReference.TargetType;
    }

    public override void Visit(IGreaterThan greaterThan) {
      base.Visit(greaterThan);
      ((GreaterThan)greaterThan).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
      base.Visit(greaterThanOrEqual);
      ((GreaterThanOrEqual)greaterThanOrEqual).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(ILeftShift leftShift) {
      base.Visit(leftShift);
      ((LeftShift)leftShift).Type = this.GetBinaryNumericOperationType(leftShift);
    }

    public override void Visit(ILessThan lessThan) {
      base.Visit(lessThan);
      ((LessThan)lessThan).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(ILessThanOrEqual lessThanOrEqual) {
      base.Visit(lessThanOrEqual);
      ((LessThanOrEqual)lessThanOrEqual).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(ILogicalNot logicalNot) {
      base.Visit(logicalNot);
      ((LogicalNot)logicalNot).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(IMakeTypedReference makeTypedReference) {
      base.Visit(makeTypedReference);
      ((MakeTypedReference)makeTypedReference).Type = this.platformType.SystemTypedReference;
    }

    public override void Visit(IMetadataCreateArray createArray) {
      //The type should already be filled in
    }

    public override void Visit(IMetadataConstant constant) {
      //The type should already be filled in
    }

    public override void Visit(IMetadataTypeOf typeOf) {
      //The type should already be filled in
    }

    public override void Visit(IMetadataNamedArgument namedArgument) {
      //The type should already be filled in
    }

    public override void Visit(IMethodCall methodCall) {
      base.Visit(methodCall);
      ((MethodCall)methodCall).Type = methodCall.MethodToCall.Type;
    }

    public override void Visit(IModulus modulus) {
      base.Visit(modulus);
      ((Modulus)modulus).Type = this.GetBinaryNumericOperationType(modulus);
    }

    public override void Visit(IMultiplication multiplication) {
      base.Visit(multiplication);
      ((Multiplication)multiplication).Type = this.GetBinaryNumericOperationType(multiplication);
    }

    public override void Visit(INamedArgument namedArgument) {
      //TODO: get rid of INamedArgument
    }

    public override void Visit(INotEquality notEquality) {
      base.Visit(notEquality);
      ((NotEquality)notEquality).Type = this.platformType.SystemBoolean;
    }

    public override void Visit(IOldValue oldValue) {
      base.Visit(oldValue);
      ((OldValue)oldValue).Type = oldValue.Expression.Type;
    }

    public override void Visit(IOnesComplement onesComplement) {
      base.Visit(onesComplement);
      ((OnesComplement)onesComplement).Type = onesComplement.Operand.Type;
    }

    public override void Visit(IOutArgument outArgument) {
      base.Visit(outArgument);
      ((OutArgument)outArgument).Type = outArgument.Type;
    }

    public override void Visit(IPointerCall pointerCall) {
      IFunctionPointerTypeReference pointerType = (IFunctionPointerTypeReference)pointerCall.Pointer.Type;
      this.Visit(pointerCall.Pointer);
      ((Expression)pointerCall.Pointer).Type = pointerType;
      this.Visit(pointerCall.Arguments);
      ((PointerCall)pointerCall).Type = pointerType.Type;
    }

    public override void Visit(IRefArgument refArgument) {
      base.Visit(refArgument);
      ((RefArgument)refArgument).Type = refArgument.Expression.Type;
    }

    public override void Visit(IReturnValue returnValue) {
      base.Visit(returnValue);
      ((ReturnValue)returnValue).Type = this.containingType;
    }

    public override void Visit(IRightShift rightShift) {
      base.Visit(rightShift);
      ((RightShift)rightShift).Type = this.GetBinaryNumericOperationType(rightShift);
    }

    public override void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      base.Visit(runtimeArgumentHandleExpression);
      ((RuntimeArgumentHandleExpression)runtimeArgumentHandleExpression).Type = this.platformType.SystemRuntimeArgumentHandle;
    }

    public override void Visit(ISizeOf sizeOf) {
      base.Visit(sizeOf);
      ((SizeOf)sizeOf).Type = this.platformType.SystemInt32;
    }

    public override void Visit(IStackArrayCreate stackArrayCreate) {
      base.Visit(stackArrayCreate);
      ((StackArrayCreate)stackArrayCreate).Type = Vector.GetVector(stackArrayCreate.ElementType, this.host.InternFactory);
    }

    public override void Visit(ISubtraction subtraction) {
      base.Visit(subtraction);
      ((Subtraction)subtraction).Type = this.GetBinaryNumericOperationType(subtraction);
    }

    public override void Visit(ITargetExpression targetExpression) {
      base.Visit(targetExpression);
      ITypeReference type = Dummy.TypeReference;
      ILocalDefinition/*?*/ local = targetExpression.Definition as ILocalDefinition;
      if (local != null)
        type = local.Type;
      else {
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

    public override void Visit(IThisReference thisReference) {
      base.Visit(thisReference);
      ((ThisReference)thisReference).Type = this.containingType;
    }

    public override void Visit(ITokenOf tokenOf) {
      base.Visit(tokenOf);
      ITypeReference type;
      IFieldReference/*?*/ field = tokenOf.Definition as IFieldReference;
      if (field != null)
        type = this.platformType.SystemRuntimeFieldHandle;
      else {
        IMethodReference/*?*/ method = tokenOf.Definition as IMethodReference;
        if (method != null)
          type = this.platformType.SystemRuntimeMethodHandle;
        else {
          Debug.Assert(tokenOf.Definition is ITypeReference);
          type = this.platformType.SystemRuntimeTypeHandle;
        }
      }
      ((TokenOf)tokenOf).Type = type;
    }

    public override void Visit(ITypeOf typeOf) {
      base.Visit(typeOf);
      ((TypeOf)typeOf).Type = this.platformType.SystemType;
    }

    public override void Visit(IUnaryNegation unaryNegation) {
      base.Visit(unaryNegation);
      ((UnaryNegation)unaryNegation).Type = unaryNegation.Operand.Type;
    }

    public override void Visit(IUnaryPlus unaryPlus) {
      base.Visit(unaryPlus);
      ((UnaryPlus)unaryPlus).Type = unaryPlus.Operand.Type;
    }

    public override void Visit(IVectorLength vectorLength) {
      base.Visit(vectorLength);
      ((VectorLength)vectorLength).Type = this.platformType.SystemIntPtr;
    }
  }
}