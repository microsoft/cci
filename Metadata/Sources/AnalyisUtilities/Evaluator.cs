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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci.UtilityDataStructures;
using Microsoft.Cci.MutableCodeModel;
using System;

namespace Microsoft.Cci.Analysis {

  /// <summary>
  /// Provides methods that carry out IL operations with compile time constant operand values at compile time, if possible.
  /// </summary>
  public static class Evaluator {

    internal static bool Contains(Instruction definingExpression, Instruction oldDefiningExpression) {
      Contract.Requires(definingExpression != null);
      Contract.Requires(oldDefiningExpression != null);

      if (definingExpression == oldDefiningExpression) return true;
      if (definingExpression.Operand1 == null) return false;
      if (definingExpression.Operand1 == oldDefiningExpression) return true;
      if (Contains((Instruction)definingExpression.Operand1, oldDefiningExpression)) return true;
      if (definingExpression.Operand2 == null) return false;
      if (definingExpression.Operand2 == oldDefiningExpression) return true;
      var operand2 = definingExpression.Operand2 as Instruction;
      if (operand2 != null) return Contains(operand2, oldDefiningExpression);
      Contract.Assume(definingExpression.Operand2 is Instruction[]);
      var operands2toN = (Instruction[])definingExpression.Operand2;
      foreach (var operandi in operands2toN) {
        if (operandi == oldDefiningExpression) return true;
        Contract.Assume(operandi != null);
        if (Contains(operandi, oldDefiningExpression)) return true;
      }
      return false;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand"></param>
    /// <returns></returns>
    public static IMetadataConstant ConvertToUnsigned(IMetadataConstant operand) {
      Contract.Requires(operand != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      ITypeReference type = Dummy.TypeReference;
      object value = null;
      switch (operand.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          value = (byte)(sbyte)operand.Value;
          type = operand.Type.PlatformType.SystemUInt8;
          break;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          value = (ushort)(short)operand.Value;
          type = operand.Type.PlatformType.SystemUInt16;
          break;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          value = (uint)(int)operand.Value;
          type = operand.Type.PlatformType.SystemUInt32;
          break;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          value = (ulong)(long)operand.Value;
          type = operand.Type.PlatformType.SystemUInt64;
          break;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          value = (UIntPtr)(long)(IntPtr)operand.Value;
          type = operand.Type.PlatformType.SystemUIntPtr;
          break;
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.UInt8:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UIntPtr:
          return operand;
        default:
          return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = type };
    }

    /// <summary>
    /// Returns a compile time constant that is the same as the given constant, except that numeric values
    /// are decremented by the smallest interval appropriate to its type. If the decrement causes underflow to 
    /// happen, the result is just the given constant.
    /// </summary>
    public static IMetadataConstant DecreaseBySmallestInterval(IMetadataConstant operand) {
      Contract.Requires(operand != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      object value = null;
      switch (operand.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          var sb = (sbyte)operand.Value;
          if (sb == sbyte.MinValue) return operand;
          value = --sb;
          break;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          var s = (short)operand.Value;
          if (s == short.MinValue) return operand;
          value = --s;
          break;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          var i = (int)operand.Value;
          if (i == int.MinValue) return operand;
          value = --i;
          break;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          var l = (long)operand.Value;
          if (l == long.MinValue) return operand;
          value = --l;
          break;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          var iptr = (long)(IntPtr)operand.Value;
          if (iptr == long.MinValue) return operand;
          value = (IntPtr)(--iptr);
          break;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand.Value is byte);
          var b = (byte)operand.Value;
          if (b == byte.MinValue) return operand;
          value = --b;
          break;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand.Value is ushort);
          var us = (ushort)operand.Value;
          if (us == ushort.MinValue) return operand;
          value = --us;
          break;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand.Value is uint);
          var ui = (uint)operand.Value;
          if (ui == uint.MinValue) return operand;
          value = --ui;
          break;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand.Value is ulong);
          var ul = (ulong)operand.Value;
          if (ul == ulong.MinValue) return operand;
          value = --ul;
          break;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand.Value is UIntPtr);
          var uptr = (ulong)(UIntPtr)operand.Value;
          if (uptr == ulong.MinValue) return operand;
          value = (UIntPtr)(--uptr);
          break;
        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand.Value is float);
          var f = (float)operand.Value;
          var incr = float.Epsilon;
          var fincr = f - incr;
          while (fincr == f) {
            incr *= 2;
            fincr -= incr;
          }
          if (float.IsNegativeInfinity(fincr)) return operand;
          value = fincr;
          break;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand.Value is double);
          var d = (double)operand.Value;
          var incrd = double.Epsilon;
          var dincr = d + incrd;
          while (dincr == d) {
            incrd *= 2;
            dincr += incrd;
          }
          if (double.IsNegativeInfinity(dincr)) return operand;
          value = dincr;
          break;
        default:
          return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = operand.Type };
    }

    /// <summary>
    /// Evaluates the given unary operation for the given compile time constant.
    /// If the operation will fail if carried out at runtime, the result is Dummy.Constant.
    /// If the result of the operation cannot be known until runtime, for example because the size of IntPtr is only known at runtime, then the result is null.
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="operand"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Evaluate(IOperation operation, IMetadataConstant operand) {
      Contract.Requires(operation != null);
      Contract.Requires(operand != null);

      if (operand == Dummy.Constant) return Dummy.Constant;
      var platfomType = operand.Type.PlatformType;
      object resultValue = null;
      ITypeReference resultType = null;
      switch (operand.Type.TypeCode) {
        //these cases all push a 32-bit value on the operand stack, with sign propagation as appropriate. 
        //The operations then treat the 32-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack. 
        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand.Value is bool);
          var u4 = ((bool)operand.Value) ? 1u : 0u;
          var i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand.Value is byte);
          u4 = (byte)operand.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand.Value is char);
          u4 = (char)operand.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand.Value is ushort);
          u4 = (ushort)operand.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand.Value is uint);
          u4 = (uint)operand.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          i4 = (int)(sbyte)operand.Value;
          u4 = (byte)i4;
          goto do4;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          i4 = (short)operand.Value;
          u4 = (ushort)i4;
          goto do4;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          i4 = (int)operand.Value;
          u4 = (uint)i4;
          goto do4;

        //These cases push 32 bit or 64 bit values on the stack, depending on the platform, with sign propagation as appropriate. 
        //The operations then treat the 32/64-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack. 
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          var i8 = (long)(IntPtr)operand.Value;
          var u8 = (ulong)(IntPtr)operand.Value;
          goto do8;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand.Value is IntPtr);
          i8 = (long)(IntPtr)operand.Value;
          u8 = (ulong)(IntPtr)operand.Value;
          goto do8;

        //These cases push 64 bit values on the stack, with sign propagation as appropriate. 
        //The operations then treat the 64-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack.  
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          i8 = (long)operand.Value;
          u8 = (ulong)i8;
          goto do8;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand.Value is ulong);
          u8 = (ulong)operand.Value;
          i8 = (long)u8;
          goto do8;

        do4:
          u8 = (ulong)u4;
          i8 = (long)i4;
        do8:
          switch (operation.OperationCode) {
            case OperationCode.Conv_I1:
              resultValue = (sbyte)i8;
              resultType = platfomType.SystemInt8;
              break;
            case OperationCode.Conv_I2:
              resultValue = (short)i8;
              resultType = platfomType.SystemInt16;
              break;
            case OperationCode.Conv_I4:
              resultValue = (int)i8;
              resultType = platfomType.SystemInt32;
              break;
            case OperationCode.Conv_I8:
              resultValue = (long)i8;
              resultType = platfomType.SystemInt64;
              break;
            case OperationCode.Conv_R4:
              resultValue = (float)i8;
              resultType = platfomType.SystemFloat32;
              break;
            case OperationCode.Conv_R8:
              resultValue = (double)i8;
              resultType = platfomType.SystemFloat64;
              break;
            case OperationCode.Conv_U1:
              resultValue = (byte)i8;
              resultType = platfomType.SystemUInt8;
              break;
            case OperationCode.Conv_U2:
              resultValue = (ushort)i8;
              resultType = platfomType.SystemUInt16;
              break;
            case OperationCode.Conv_U4:
              resultValue = (uint)i8;
              resultType = platfomType.SystemUInt32;
              break;
            case OperationCode.Conv_U8:
              resultValue = (ulong)i8;
              resultType = platfomType.SystemUInt64;
              break;
            case OperationCode.Conv_I:
              resultValue = (IntPtr)i8;
              resultType = platfomType.SystemIntPtr;
              break;
            case OperationCode.Conv_U:
              resultValue = (UIntPtr)i8;
              resultType = platfomType.SystemUIntPtr;
              break;
            case OperationCode.Conv_R_Un:
              resultValue = (double)u8;
              resultType = platfomType.SystemFloat64;
              break;

            case OperationCode.Conv_Ovf_I1:
              if (i8 < sbyte.MinValue || i8 > sbyte.MaxValue) return Dummy.Constant; //known to fail.
              goto case OperationCode.Conv_I1;
            case OperationCode.Conv_Ovf_I2:
              if (i8 < short.MinValue || i8 > short.MaxValue) return Dummy.Constant; //known to fail.
              goto case OperationCode.Conv_I2;
            case OperationCode.Conv_Ovf_I4:
              if (i8 < int.MinValue || i8 > int.MaxValue) return Dummy.Constant; //known to fail.
              goto case OperationCode.Conv_I4;
            case OperationCode.Conv_Ovf_I8:
              goto case OperationCode.Conv_I8;
            case OperationCode.Conv_Ovf_U1:
              if (i8 < 0 || i8 > byte.MaxValue) return Dummy.Constant; //known to fail.
              goto case OperationCode.Conv_U1;
            case OperationCode.Conv_Ovf_U2:
              if (i8 < 0 || i8 > ushort.MaxValue) return Dummy.Constant; //known to fail.
              goto case OperationCode.Conv_U2;
            case OperationCode.Conv_Ovf_U4:
              if (i8 < 0 || i8 > uint.MaxValue) return Dummy.Constant; //known to fail.
              goto case OperationCode.Conv_U4;
            case OperationCode.Conv_Ovf_U8:
              if (i8 < 0) return Dummy.Constant; //known to fail.
              goto case OperationCode.Conv_U8;
            case OperationCode.Conv_Ovf_I:
              if (i8 < int.MinValue || i8 > int.MaxValue) return Dummy.Constant; //might fail.
              goto case OperationCode.Conv_I;
            case OperationCode.Conv_Ovf_U:
              if (i8 < 0) return Dummy.Constant; //known to fail.
              if (i8 > uint.MaxValue) return Dummy.Constant; //might fail.
              goto case OperationCode.Conv_U;

            case OperationCode.Conv_Ovf_I1_Un:
              if (u8 > (long)sbyte.MaxValue) return Dummy.Constant; //known to fail.
              resultValue = (sbyte)u8;
              resultType = platfomType.SystemInt8;
              break;
            case OperationCode.Conv_Ovf_I2_Un:
              if (u8 > (long)short.MaxValue) return Dummy.Constant; //known to fail.
              resultValue = (short)u8;
              resultType = platfomType.SystemInt16;
              break;
            case OperationCode.Conv_Ovf_I4_Un:
              if (u8 > int.MaxValue) return Dummy.Constant; //known to fail.
              resultValue = (int)u8;
              resultType = platfomType.SystemInt32;
              break;
            case OperationCode.Conv_Ovf_I8_Un:
              if (u8 > long.MaxValue) return Dummy.Constant; //known to fail.
              resultValue = (long)u8;
              resultType = platfomType.SystemInt64;
              break;
            case OperationCode.Conv_Ovf_U1_Un:
              if (u8 > byte.MaxValue) return Dummy.Constant; //known to fail.
              resultValue = (byte)u8;
              resultType = platfomType.SystemUInt8;
              break;
            case OperationCode.Conv_Ovf_U2_Un:
              if (u8 > ushort.MaxValue) return Dummy.Constant; //known to fail.
              resultValue = (ushort)u8;
              resultType = platfomType.SystemUInt16;
              break;
            case OperationCode.Conv_Ovf_U4_Un:
              if (u8 > uint.MaxValue) return Dummy.Constant; //known to fail.
              resultValue = u8;
              resultType = platfomType.SystemUInt32;
              break;
            case OperationCode.Conv_Ovf_U8_Un:
              resultValue = u8;
              resultType = platfomType.SystemUInt64;
              break;
            case OperationCode.Conv_Ovf_I_Un:
              if (u8 > int.MaxValue) return Dummy.Constant; //might fail.
              resultValue = (IntPtr)u8;
              resultType = platfomType.SystemIntPtr;
              break;
            case OperationCode.Conv_Ovf_U_Un:
              if (u8 > uint.MaxValue) return Dummy.Constant; //might fail.
              resultValue = (UIntPtr)u8;
              resultType = platfomType.SystemUIntPtr;
              break;

            case OperationCode.Dup:
              return operand;

            case OperationCode.Neg:
              var ni8 = i8 == long.MinValue ? i8 : -i8;
              switch (operand.Type.TypeCode) {
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.UInt64:
                  resultValue = ni8;
                  resultType = platfomType.SystemInt64;
                  break;
                case PrimitiveTypeCode.IntPtr:
                case PrimitiveTypeCode.UIntPtr:
                  resultValue = (IntPtr)ni8;
                  resultType = platfomType.SystemIntPtr;
                  break;
                default:
                  resultValue = (int)ni8;
                  resultType = platfomType.SystemInt32;
                  break;
              }
              break;

            case OperationCode.Not:
              var ci8 = ~i8;
              switch (operand.Type.TypeCode) {
                case PrimitiveTypeCode.Int64:
                case PrimitiveTypeCode.UInt64:
                  resultValue = ci8;
                  resultType = platfomType.SystemInt64;
                  break;
                case PrimitiveTypeCode.IntPtr:
                case PrimitiveTypeCode.UIntPtr:
                  resultValue = (IntPtr)ci8;
                  resultType = platfomType.SystemIntPtr;
                  break;
                default:
                  resultValue = (int)ci8;
                  resultType = platfomType.SystemInt32;
                  break;
              }
              break;

          }
          break;

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand.Value is float);
          var f = (float)operand.Value;
          double d = (double)f;
          i8 = (long)(int)f;
          u8 = (ulong)(uint)(int)f;
          goto doDouble;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand.Value is double);
          d = (double)operand.Value;
          i8 = (long)d;
          u8 = (ulong)i8;
        doDouble:
          switch (operation.OperationCode) {
            case OperationCode.Conv_R4:
              resultValue = (float)d;
              resultType = platfomType.SystemFloat32;
              break;
            case OperationCode.Conv_R8:
              resultValue = d;
              resultType = platfomType.SystemFloat64;
              break;
            case OperationCode.Conv_R_Un:
              resultValue = (double)u8;
              resultType = platfomType.SystemFloat64;
              break;

            case OperationCode.Dup:
              return operand;

            default:
              goto do8; //Only for operations that deal with floats after they have been converted to integers.

          }
          break;

        case PrimitiveTypeCode.Pointer:
        case PrimitiveTypeCode.Reference:
          //TODO: we might know that the pointer is 0
          return Dummy.Constant; //We only known the value of the pointer at runtime.

        case PrimitiveTypeCode.String:
        case PrimitiveTypeCode.NotPrimitive:
          //TODO: might be able to remove a castclass and fold an isinst.
          return Dummy.Constant; //We only known the value of the pointer at runtime.
      }

      if (resultValue != null && resultType != null) {
        var result = new MetadataConstant() { Value = resultValue, Type = resultType };
        if (result.Locations == null) result.Locations = new List<ILocation>(1);
        result.Locations.Add(operation.Location);
        return result;
      }
      return null; //force runtime evaluation.
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Evaluate(IOperation operation, IMetadataConstant operand1, IMetadataConstant operand2) {
      Contract.Requires(operation != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);

      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return Dummy.Constant;
      var platfomType = operand1.Type.PlatformType;
      switch (operand1.Type.TypeCode) {
        //these cases all push a 32-bit value on the operand stack, with sign propagation as appropriate. 
        //The operations then treat the 32-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack. 
        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          var u4 = ((bool)operand1.Value) ? 1u : 0u;
          var i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          u4 = (byte)operand1.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          u4 = (char)operand1.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          u4 = (ushort)operand1.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          u4 = (uint)operand1.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          i4 = (int)(sbyte)operand1.Value;
          u4 = (byte)i4;
          goto do4;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          i4 = (short)operand1.Value;
          u4 = (ushort)i4;
          goto do4;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          i4 = (int)operand1.Value;
          u4 = (uint)i4;
        do4:
          return Evaluate(operation, u4, i4, operand2);

        //These cases push 32 bit or 64 bit values on the stack, depending on the platform, with sign propagation as appropriate. 
        //The operations then treat the 32/64-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack. 
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          var i8 = (long)(IntPtr)operand1.Value;
          var u8 = (ulong)(IntPtr)operand1.Value;
          goto do8;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          i8 = (long)(IntPtr)operand1.Value;
          u8 = (ulong)(IntPtr)operand1.Value;
          goto do8;

        //These cases push 64 bit values on the stack, with sign propagation as appropriate. 
        //The operations then treat the 64-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack.  
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          i8 = (long)operand1.Value;
          u8 = (ulong)i8;
          goto do8;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          u8 = (ulong)operand1.Value;
          i8 = (long)u8;
        do8:
          return Evaluate(operation, u8, i8, operand2);

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var f = (float)operand1.Value;
          double d = (double)f;
          i8 = (long)(int)f;
          u8 = (ulong)(uint)(int)f;
          goto doDouble;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d = (double)operand1.Value;
          i8 = (long)d;
          u8 = (ulong)i8;
        doDouble:
          return Evaluate(operation, d, operand2);

        case PrimitiveTypeCode.Pointer:
        case PrimitiveTypeCode.Reference:
          //TODO: we might know that the pointer is 0
          return Dummy.Constant; //We only known the value of the pointer at runtime.

        case PrimitiveTypeCode.String:
        case PrimitiveTypeCode.NotPrimitive:
          //TODO: might be able to remove a castclass and fold an isinst.
          return Dummy.Constant; //We only known the value of the pointer at runtime.
      }
      return null; //force runtime evaluation.
    }

    private static IMetadataConstant Evaluate(IOperation operation, uint u41, int i41, IMetadataConstant operand2) {
      Contract.Requires(operation != null);
      Contract.Requires(operand2 != null);

      switch (operand2.Type.TypeCode) {
        //these cases all push a 32-bit value on the operand stack, with sign propagation as appropriate. 
        //The operations then treat the 32-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack. 
        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand2.Value is bool);
          var u4 = ((bool)operand2.Value) ? 1u : 0u;
          var i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand2.Value is byte);
          u4 = (byte)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand2.Value is char);
          u4 = (char)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand2.Value is ushort);
          u4 = (ushort)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand2.Value is uint);
          u4 = (uint)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand2.Value is sbyte);
          i4 = (int)(sbyte)operand2.Value;
          u4 = (byte)i4;
          goto do4;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand2.Value is short);
          i4 = (short)operand2.Value;
          u4 = (ushort)i4;
          goto do4;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand2.Value is int);
          i4 = (int)operand2.Value;
          u4 = (uint)i4;
        do4:
          return Evaluate(operation, operand2.Type.PlatformType, u41, i41, u4, i4);
      }
      return null; //force runtime evaluation.
    }

    private static IMetadataConstant Evaluate(IOperation operation, ulong u81, long i81, IMetadataConstant operand2) {
      Contract.Requires(operation != null);
      Contract.Requires(operand2 != null);

      switch (operand2.Type.TypeCode) {
        //these cases all push a 32-bit value on the operand stack, with sign propagation as appropriate. 
        //The operations then treat the 32-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack. 
        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand2.Value is bool);
          var u4 = ((bool)operand2.Value) ? 1u : 0u;
          var i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand2.Value is byte);
          u4 = (byte)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand2.Value is char);
          u4 = (char)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand2.Value is ushort);
          u4 = (ushort)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand2.Value is uint);
          u4 = (uint)operand2.Value;
          i4 = (int)u4;
          goto do4;
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand2.Value is sbyte);
          i4 = (int)(sbyte)operand2.Value;
          u4 = (byte)i4;
          goto do4;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand2.Value is short);
          i4 = (short)operand2.Value;
          u4 = (ushort)i4;
          goto do4;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand2.Value is int);
          i4 = (int)operand2.Value;
          u4 = (uint)i4;
        do4:
          long i8 = i4;
          ulong u8 = u4;
          goto do8;

        //These cases push 32 bit or 64 bit values on the stack, depending on the platform, with sign propagation as appropriate. 
        //The operations then treat the 32/64-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack. 
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand2.Value is IntPtr);
          i8 = (long)(IntPtr)operand2.Value;
          u8 = (ulong)(IntPtr)operand2.Value;
          goto do8;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand2.Value is IntPtr);
          i8 = (long)(IntPtr)operand2.Value;
          u8 = (ulong)(IntPtr)operand2.Value;
          goto do8;

        //These cases push 64 bit values on the stack, with sign propagation as appropriate. 
        //The operations then treat the 64-bit value as either signed or unsigned, based on the operation code, regardless of how the value got onto the stack.  
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand2.Value is long);
          i8 = (long)operand2.Value;
          u8 = (ulong)i8;
          goto do8;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand2.Value is ulong);
          u8 = (ulong)operand2.Value;
          i8 = (long)u8;
          goto do8;
        do8:
          return Evaluate(operation, operand2.Type.PlatformType, u81, i81, u8, i8);

      }
      return null; //force runtime evaluation.
    }

    private static IMetadataConstant Evaluate(IOperation operation, double d1, IMetadataConstant operand2) {
      Contract.Requires(operation != null);
      Contract.Requires(operand2 != null);

      var platfomType = operand2.Type.PlatformType;
      switch (operand2.Type.TypeCode) {
        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand2.Value is float);
          var f = (float)operand2.Value;
          var d2 = (double)f;
          goto doDouble;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand2.Value is double);
          d2 = (double)operand2.Value;
        doDouble:
          return Evaluate(operation, operand2.Type.PlatformType, d1, d2);
      }
      return null; //force runtime evaluation.
    }

    private static IMetadataConstant Evaluate(IOperation operation, IPlatformType platformType, uint u41, int i41, uint u42, int i42) {
      Contract.Requires(operation != null);
      Contract.Requires(platformType != null);

      object resultValue = null;
      ITypeReference resultType = platformType.SystemInt32;
      switch (operation.OperationCode) {
        case OperationCode.Add:
          resultValue = i41 + i42;
          break;
        case OperationCode.Add_Ovf:
          var i8 = i41 + (long)i42;
          var i4 = (int)i8;
          if (i8 != (long)i4) return Dummy.Constant; //known to fail.
          resultValue = i4;
          break;
        case OperationCode.Add_Ovf_Un:
          var u8 = u41 + (ulong)u42;
          var u4 = (uint)u8;
          if (u8 != (ulong)u4) return Dummy.Constant; //known to fail
          resultValue = (int)u4;
          break;
        case OperationCode.And:
          resultValue = i41 & i42;
          break;
        case OperationCode.Ceq:
          resultValue = i41 == i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Cgt:
          resultValue = i41 > i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Cgt_Un:
          resultValue = u41 > u42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Clt:
          resultValue = i41 < i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Clt_Un:
          resultValue = u41 < u42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Div:
          if (i42 == 0) return Dummy.Constant; //known to fail.
          if (i41 == int.MinValue && i42 == -1) return Dummy.Constant;  //known to fail.
          resultValue = i41 / i42;
          break;
        case OperationCode.Div_Un:
          if (u42 == 0) return Dummy.Constant; //known to fail.
          resultValue = u41 / u42;
          break;
        case OperationCode.Mul:
          resultValue = i41 * i42;
          break;
        case OperationCode.Mul_Ovf:
          i4 = i41 * i42;
          if (i4 == int.MinValue && (i41 == -1 || i42 == -1)) return Dummy.Constant; //This can happen when the other value is int.MinValue, 
          //in which case there is an overflow and this operation is known to fail.
          if (i42 != 0 && i4 / i42 != i41) return Dummy.Constant; //known to fail.
          resultValue = i4;
          break;
        case OperationCode.Mul_Ovf_Un:
          u4 = u41 * u42;
          if (u42 != 0 && u4 / u42 != u41) return Dummy.Constant; //known to fail.
          resultValue = u4;
          break;
        case OperationCode.Or:
          resultValue = i41 | i42;
          break;
        case OperationCode.Rem:
          if (i42 == 0) return Dummy.Constant; //known to fail.
          resultValue = i41 % i42;
          break;
        case OperationCode.Rem_Un:
          if (u42 == 0) return Dummy.Constant; //known to fail.
          resultValue = u41 % u42;
          break;
        case OperationCode.Shl:
          resultValue = i41 << i42;
          break;
        case OperationCode.Shr:
          resultValue = i41 >> i42;
          break;
        case OperationCode.Shr_Un:
          resultValue = u41 >> i42;
          break;
        case OperationCode.Sub:
          resultValue = i41 - i42;
          break;
        case OperationCode.Sub_Ovf:
          i4 = i41 - i42;
          if (i41 < 0) {
            if (i42 > 0) {
              if (i4 > i41) return Dummy.Constant; //known to fail.
            }
          } else if (i42 < 0) {
            if (i4 < i41) return Dummy.Constant; //known to fail.
          }
          resultValue = i4;
          break;
        case OperationCode.Sub_Ovf_Un:
          if (u41 < u42) return Dummy.Constant; //known to fail
          u4 = u41 - u42;
          resultValue = (int)u4;
          break;
        case OperationCode.Xor:
          resultValue = i41 ^ i42;
          break;

        //These instructions result in no values, but it is interesting to know the values of their conditions.
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          resultValue = i41 == i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
          resultValue = i41 >= i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          resultValue = u41 >= u42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
          resultValue = i41 > i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          resultValue = u41 > u42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
          resultValue = i41 <= i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          resultValue = u41 <= u42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
          resultValue = i41 < i42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          resultValue = u41 < u42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          resultValue = u41 != u42;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Nop:
          if (i41 == i42) 
            resultValue = i41;
          break;
      }

      if (resultValue != null && resultType != null) {
        var result = new MetadataConstant() { Value = resultValue, Type = resultType };
        if (result.Locations == null) result.Locations = new List<ILocation>(1);
        result.Locations.Add(operation.Location);
        return result;
      }
      return null; //force runtime evaluation.
    }

    private static IMetadataConstant Evaluate(IOperation operation, IPlatformType platformType, ulong u81, long i81, ulong u82, long i82) {
      Contract.Requires(operation != null);
      Contract.Requires(platformType != null);

      object resultValue = null;
      ITypeReference resultType = platformType.SystemInt64;
      switch (operation.OperationCode) {
        case OperationCode.Add:
          resultValue = i81 + i82;
          break;
        case OperationCode.Add_Ovf:
          var i8 = i81 + i82;
          if (i81 < 0) {
            if (i82 < 0) {
              if (i8 > i81) return Dummy.Constant; //known to fail.
            }
          } else if (i82 > 0) {
            if (i8 < i81) return Dummy.Constant; //known to fail.
          }
          resultValue = i8;
          break;
        case OperationCode.Add_Ovf_Un:
          var u8 = u81 + u82;
          if (u8 < u81) return Dummy.Constant; //known to fail
          resultValue = (long)u8;
          break;
        case OperationCode.And:
          resultValue = i81 & i82;
          break;
        case OperationCode.Ceq:
          resultValue = i81 == i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Cgt:
          resultValue = i81 > i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Cgt_Un:
          resultValue = u81 > u82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Clt:
          resultValue = i81 < i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Clt_Un:
          resultValue = u81 < u82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Div:
          if (i82 == 0) return Dummy.Constant; //known to fail.
          if (i81 == long.MinValue && i82 == -1) return Dummy.Constant; //known to fail.
          resultValue = i81 / i82;
          break;
        case OperationCode.Div_Un:
          if (u82 == 0) return Dummy.Constant; //known to fail.
          resultValue = u81 / u82;
          break;
        case OperationCode.Mul:
          resultValue = i81 * i82;
          break;
        case OperationCode.Mul_Ovf:
          i8 = i81 * i82;
          if (i8 == long.MinValue && (i81 == -1 || i82 == -1)) return Dummy.Constant;  //This can happen when the other value is long.MinValue, 
          //in which case there is an overflow and this operation is known to fail.
          if (i82 != 0 && i8 / i82 != i81) return Dummy.Constant; //known to fail.
          resultValue = i8;
          break;
        case OperationCode.Mul_Ovf_Un:
          u8 = u81 * u82;
          if (u82 != 0 && u8 / u82 != u81) return Dummy.Constant; //known to fail.
          resultValue = u8;
          break;
        case OperationCode.Or:
          resultValue = i81 | i82;
          break;
        case OperationCode.Rem:
          if (i82 == 0) return Dummy.Constant; //known to fail.
          resultValue = i81 % i82;
          break;
        case OperationCode.Rem_Un:
          if (u82 == 0) return Dummy.Constant; //known to fail.
          resultValue = u81 % u82;
          break;
        case OperationCode.Shl:
          resultValue = i81 << (int)i82;
          break;
        case OperationCode.Shr:
          resultValue = i81 >> (int)i82;
          break;
        case OperationCode.Shr_Un:
          resultValue = u81 >> (int)i82;
          break;
        case OperationCode.Sub:
          resultValue = i81 - i82;
          break;
        case OperationCode.Sub_Ovf:
          i8 = i81 - i82;
          if (i81 < 0) {
            if (i82 > 0) {
              if (i8 > i81) return Dummy.Constant; //known to fail.
            }
          } else if (i82 < 0) {
            if (i8 < i81) return Dummy.Constant; //known to fail.
          }
          resultValue = i8;
          break;
        case OperationCode.Sub_Ovf_Un:
          if (u81 < u82) return Dummy.Constant; //known to fail
          u8 = u81 - u82;
          resultValue = (long)u8;
          break;
        case OperationCode.Xor:
          resultValue = i81 ^ i82;
          break;

        //These instructions result in no values, but it is interesting to know the values of their conditions.
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          resultValue = i81 == i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
          resultValue = i81 >= i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          resultValue = u81 >= u82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
          resultValue = i81 > i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          resultValue = u81 > u82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
          resultValue = i81 <= i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          resultValue = u81 <= u82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
          resultValue = i81 < i82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          resultValue = u81 < u82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          resultValue = u81 != u82;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Nop:
          if (i81 == i82)
            resultValue = i81;
          break;
      }

      if (resultValue != null && resultType != null) {
        var result = new MetadataConstant() { Value = resultValue, Type = resultType };
        if (result.Locations == null) result.Locations = new List<ILocation>(1);
        result.Locations.Add(operation.Location);
        return result;
      }
      return null; //force runtime evaluation.
    }

    private static IMetadataConstant Evaluate(IOperation operation, IPlatformType platformType, double d1, double d2) {
      Contract.Requires(operation != null);
      Contract.Requires(platformType != null);

      object resultValue = null;
      ITypeReference resultType = platformType.SystemFloat64;
      switch (operation.OperationCode) {
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          resultValue = d1 + d2;
          break;
        case OperationCode.And:
          return Dummy.Constant; //known to fail. (illegal instruction.) 
        case OperationCode.Ceq:
          resultValue = d1 == d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Cgt:
          resultValue = d1 > d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Cgt_Un:
          resultValue = d1 > d2 || double.IsNaN(d1) || double.IsNaN(d2);
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Clt:
          resultValue = d1 < d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Clt_Un:
          resultValue = d1 < d2 || double.IsNaN(d1) || double.IsNaN(d2);
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Div:
          resultValue = d1 / d2;
          break;
        case OperationCode.Div_Un:
          return Dummy.Constant; //known to fail. (illegal instruction.) 
        case OperationCode.Mul:
          resultValue = d1 * d2;
          break;
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
        case OperationCode.Or:
          return Dummy.Constant; //known to fail. (illegal instruction.) 
        case OperationCode.Rem:
          resultValue = d1 % d2;
          break;
        case OperationCode.Rem_Un:
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          return Dummy.Constant; //known to fail. (illegal instruction.) 
        case OperationCode.Sub:
          resultValue = d1 - d2;
          break;
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
        case OperationCode.Xor:
          return Dummy.Constant; //known to fail. (illegal instruction.) 

        //These instructions result in no values, but it is interesting to know the values of their conditions.
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          resultValue = d1 == d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
          resultValue = d1 >= d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          resultValue = !(d1 < d2);
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
          resultValue = d1 > d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          resultValue = !(d1 <= d2);
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
          resultValue = d1 <= d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          resultValue = !(d1 > d2);
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
          resultValue = d1 < d2;
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          resultValue = !(d1 >= d2);
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          resultValue = !(d1 == d2);
          resultType = platformType.SystemBoolean;
          break;
        case OperationCode.Nop:
          if (d1 == d2)
            resultValue = d1;
          break;
      }

      if (resultValue != null && resultType != null) {
        var result = new MetadataConstant() { Value = resultValue, Type = resultType };
        if (result.Locations == null) result.Locations = new List<ILocation>(1);
        result.Locations.Add(operation.Location);
        return result;
      }
      return null; //force runtime evaluation.
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="operation"></param>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <param name="mappings"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Evaluate<Instruction>(IOperation operation, IMetadataConstant operand1, Instruction operand2, ValueMappings<Instruction> mappings, AiBasicBlock<Instruction>/*?*/ block = null)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(operation != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Requires(mappings != null);

      if (operand1 == Dummy.Constant) return Dummy.Constant;
      bool operand1IsZero = MetadataExpressionHelper.IsIntegralZero(operand1);
      bool operand1IsMinusOne = MetadataExpressionHelper.IsIntegralMinusOne(operand1);
      IMetadataConstant cv2 = null;
      Interval interval2 = null;
      if (block != null) {
        //In this case the expression is being evaluated with respect to a particular block
        //so any constraints in the block can be taken into account. We get hold of these via the Interval domain.
        interval2 = mappings.GetIntervalFor(operand2, block);
        if (interval2 != null) cv2 = interval2.GetAsSingleton();
      }
      if (cv2 != null) return Evaluate(operation, operand1, cv2);

      switch (operation.OperationCode) {
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          break;
        case OperationCode.And:
          if (operand1IsZero) return operand1;
          break;
        case OperationCode.Ceq:
          break;
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
          goto case OperationCode.Bgt;
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
          goto case OperationCode.Blt;
        case OperationCode.Div:
        case OperationCode.Div_Un:
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
          if (operand1IsZero) return operand1;
          break;
        case OperationCode.Or:
          if (operand1IsMinusOne) return operand1;
          break;
        case OperationCode.Rem:
        case OperationCode.Rem_Un:
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          if (operand1IsZero) return operand1;
          break;
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
          break;
        case OperationCode.Xor:
          break;

        //These instructions result in no values, but it is interesting to know the values of their conditions.
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          if (interval2 != null && interval2.IsFinite) {
            if (Evaluator.IsNumericallyLessThan(operand1, interval2.LowerBound) || Evaluator.IsNumericallyGreaterThan(operand1, interval2.UpperBound))
              return new MetadataConstant() { Value = false, Type = operand1.Type.PlatformType.SystemBoolean };
          }
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          if (interval2 != null && interval2.UpperBound != Dummy.Constant)
            return NullUnlessTrue(Evaluate(operation, operand1, interval2.UpperBound));
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          if (interval2 != null && interval2.LowerBound != Dummy.Constant)
            return NullUnlessTrue(Evaluate(operation, operand1, interval2.LowerBound));
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          if (interval2 != null && interval2.IsFinite) {
            if (Evaluator.IsNumericallyLessThan(operand1, interval2.LowerBound) || Evaluator.IsNumericallyGreaterThan(operand1, interval2.UpperBound))
              return new MetadataConstant() { Value = true, Type = operand1.Type.PlatformType.SystemBoolean };
          }
          break;
      }
      return null;
    }

    internal static IMetadataConstant Negate(IMetadataConstant compileTimeConstant) {
      Contract.Requires(compileTimeConstant != null);
      var val = compileTimeConstant.Value as IConvertible;
      if (val == null) return compileTimeConstant;
      switch (val.GetTypeCode()) {
        case TypeCode.Double: return new MetadataConstant() { Value = -val.ToDouble(null), Type = compileTimeConstant.Type };
        case TypeCode.Int16: return new MetadataConstant() { Value = (short)-val.ToInt16(null), Type = compileTimeConstant.Type };
        case TypeCode.Int32: return new MetadataConstant() { Value = -val.ToInt32(null), Type = compileTimeConstant.Type };
        case TypeCode.Int64: return new MetadataConstant() { Value = -val.ToInt64(null), Type = compileTimeConstant.Type };
        case TypeCode.SByte: return new MetadataConstant() { Value = (sbyte)-val.ToSByte(null), Type = compileTimeConstant.Type };
        case TypeCode.Single: return new MetadataConstant() { Value = -val.ToSingle(null), Type = compileTimeConstant.Type };
      }
      return compileTimeConstant;
    }

    private static IMetadataConstant/*?*/ NullUnlessTrue(IMetadataConstant compileTimeConstant) {
      if (compileTimeConstant == null) return null;
      if (!(compileTimeConstant.Value is bool)) return null;
      if ((bool)compileTimeConstant.Value) return compileTimeConstant;
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="operation"></param>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <param name="mappings"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Evaluate<Instruction>(IOperation operation, Instruction operand1, IMetadataConstant operand2, ValueMappings<Instruction> mappings, AiBasicBlock<Instruction>/*?*/ block = null)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(operation != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Requires(mappings != null);

      IMetadataConstant cv1 = null;
      Interval interval1 = null;
      if (block != null) {
        //In this case the expression is being evaluated with respect to a particular block
        //so any constraints in the block can be taken into account. We get hold of these via the Interval domain.
        interval1 = mappings.GetIntervalFor(operand1, block);
        if (interval1 != null) cv1 = interval1.GetAsSingleton();
      }
      if (cv1 != null) return Evaluate(operation, cv1, operand2);
      if (operand2 == Dummy.Constant) return Dummy.Constant;
      bool operand2IsZero = MetadataExpressionHelper.IsIntegralZero(operand2);

      switch (operation.OperationCode) {
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          break;
        case OperationCode.And:
          if (operand2IsZero) return operand2;
          break;
        case OperationCode.Ceq:
          break;
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
          goto case OperationCode.Bgt;
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
          goto case OperationCode.Blt;
        case OperationCode.Div:
        case OperationCode.Div_Un:
          if (operand2IsZero) return Dummy.Constant; //known to fail.
          break;
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
          if (operand2IsZero) return operand2;
          break;
        case OperationCode.Or:
          break;
        case OperationCode.Rem:
        case OperationCode.Rem_Un:
          if (operand2IsZero) return Dummy.Constant; //known to fail.
          break;
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          break;
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
          break;
        case OperationCode.Xor:
          break;

        //These instructions result in no values, but it is interesting to know the values of their conditions.
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          if (interval1 != null && interval1.IsFinite) {
            if (Evaluator.IsNumericallyLessThan(operand2, interval1.LowerBound) || Evaluator.IsNumericallyGreaterThan(operand2, interval1.UpperBound))
              return new MetadataConstant() { Value = false, Type = operand1.Type.PlatformType.SystemBoolean };
          }
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          if (interval1 != null && interval1.LowerBound != Dummy.Constant)
            return NullUnlessTrue(Evaluate(operation, interval1.LowerBound, operand2));
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          if (interval1 != null && interval1.UpperBound != Dummy.Constant)
            return NullUnlessTrue(Evaluate(operation, interval1.UpperBound, operand2));
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          if (interval1 != null && interval1.IsFinite) {
            if (Evaluator.IsNumericallyLessThan(interval1.UpperBound, operand2) || Evaluator.IsNumericallyGreaterThan(interval1.LowerBound, operand2))
              return new MetadataConstant() { Value = true, Type = operand1.Type.PlatformType.SystemBoolean };
          }
          break;
      }

      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="operation"></param>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <param name="mappings"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Evaluate<Instruction>(IOperation operation, Instruction operand1, Instruction operand2, ValueMappings<Instruction> mappings, AiBasicBlock<Instruction>/*?*/ block = null)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(operation != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Requires(mappings != null);

      IMetadataConstant cv1 = null;
      Interval interval1 = null;
      IMetadataConstant cv2 = null;
      Interval interval2 = null;
      if (block != null) {
        //In this case the expression is being evaluated with respect to a particular block
        //so any constraints in the block can be taken into account. We get hold of these via the Interval domain.
        interval1 = mappings.GetIntervalFor(operand1, block);
        if (interval1 != null) cv1 = interval1.GetAsSingleton();
        interval2 = mappings.GetIntervalFor(operand2, block);
        if (interval2 != null) cv2 = interval2.GetAsSingleton();
      }
      if (cv1 != null) {
        if (cv2 != null) return Evaluate(operation, cv1, cv2);
        return Evaluate(operation, cv1, operand2, mappings, block);
      }
      if (cv2 != null)
        return Evaluate(operation, operand1, cv2, mappings, block);
      bool floatingPoint = operand1.Type.TypeCode == PrimitiveTypeCode.Float32 || operand1.Type.TypeCode == PrimitiveTypeCode.Float64;

      object resultValue = null;
      ITypeReference resultType = null;
      switch (operation.OperationCode) {
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          break;
        case OperationCode.And:
          break;
        case OperationCode.Ceq:
          if (operand1 == operand2 && !floatingPoint) {
            resultValue = true;
            resultType = operand1.Type.PlatformType.SystemBoolean;
          }
          break;
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
          goto case OperationCode.Bgt;
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
          goto case OperationCode.Blt;
        case OperationCode.Div:
          if (operand1 == operand2 && !floatingPoint && interval1 != null && interval1.ExcludesZero) {
            if (TypeHelper.SizeOfType(operand1.Type) == 4) {
              resultValue = 1;
              resultType = operand1.Type.PlatformType.SystemInt32;
            } else {
              resultValue = 1L;
              resultType = operand1.Type.PlatformType.SystemInt64;
            }
          }
          break;
        case OperationCode.Div_Un:
          if (operand1 == operand2 && !floatingPoint && interval1 != null && interval1.ExcludesZero) {
            if (TypeHelper.SizeOfType(operand1.Type) == 4) {
              resultValue = 1u;
              resultType = operand1.Type.PlatformType.SystemUInt32;
            } else {
              resultValue = 1UL;
              resultType = operand1.Type.PlatformType.SystemUInt64;
            }
          }
          break;
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
          break;
        case OperationCode.Or:
          break;
        case OperationCode.Rem:
        case OperationCode.Rem_Un:
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          break;
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
        case OperationCode.Xor:
          if (operand1 == operand2 && !floatingPoint) {
            if (TypeHelper.SizeOfType(operand1.Type) == 4) {
              resultValue = 0;
              resultType = operand1.Type.PlatformType.SystemInt32;
            } else {
              resultValue = 0L;
              resultType = operand1.Type.PlatformType.SystemInt64;
            }
          }
          break;

        //These instructions result in no values, but it is interesting to know the values of their conditions.
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          if (operand1 == operand2) {
            resultValue = true;
            resultType = operand1.Type.PlatformType.SystemBoolean;
          }
          if (operation.OperationCode == OperationCode.Beq || operation.OperationCode == OperationCode.Beq_S) break;
          goto case OperationCode.Bgt;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          if (operand1 == operand2 && !floatingPoint) {
            resultValue = false;
            resultType = operand1.Type.PlatformType.SystemBoolean;
          }
          if (interval1 != null && interval2 != null && interval1.LowerBound != Dummy.Constant && interval2.UpperBound != Dummy.Constant)
            return NullUnlessTrue(Evaluator.Evaluate(operation, interval1.LowerBound, interval2.UpperBound));
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          if (operand1 == operand2) {
            resultValue = true;
            resultType = operand1.Type.PlatformType.SystemBoolean;
          }
          goto case OperationCode.Blt;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          if (operand1 == operand2 && !floatingPoint) {
            resultValue = false;
            resultType = operand1.Type.PlatformType.SystemBoolean;
          }
          if (interval1 != null && interval2 != null && interval1.UpperBound != Dummy.Constant && interval2.LowerBound != Dummy.Constant)
            return NullUnlessTrue(Evaluator.Evaluate(operation, interval1.UpperBound, interval2.LowerBound));
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          if (operand1 == operand2 && !floatingPoint) {
            resultValue = false;
            resultType = operand1.Type.PlatformType.SystemBoolean;
          }
          if (interval1 != null && interval1.IsFinite && interval2 != null && interval2.IsFinite) {
            if (Evaluator.IsNumericallyLessThan(interval1.UpperBound, interval2.LowerBound) || Evaluator.IsNumericallyGreaterThan(interval1.LowerBound, interval2.UpperBound))
              return new MetadataConstant() { Value = true, Type = operand1.Type.PlatformType.SystemBoolean };
          }
          break;
      }

      if (resultValue != null && resultType != null) {
        var result = new MetadataConstant() { Value = resultValue, Type = resultType };
        if (result.Locations == null) result.Locations = new List<ILocation>(1);
        result.Locations.Add(operation.Location);
        return result;
      }
      return null; //force runtime evaluation.
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="operation"></param>
    /// <param name="operand1"></param>
    /// <param name="operands2toN"></param>
    /// <param name="mappings"></param>
    /// <param name="block"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Evaluate<Instruction>(IOperation operation, Instruction operand1, Instruction[] operands2toN, ValueMappings<Instruction> mappings, AiBasicBlock<Instruction>/*?*/ block = null)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(operation != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operands2toN != null);
      Contract.Requires(mappings != null);

      switch (operation.OperationCode) {
        case OperationCode.Nop:
          var phiVar = operation.Value as INamedEntity;
          if (phiVar == null) return null;;
          //We have a phi node. If all operands are constants and are moreover equal, we can reduce the phi node to a constant.
          var cv1 = mappings.GetCompileTimeConstantValueFor(operand1, block);
          if (cv1 == null) return null;
          foreach (var operandi in operands2toN) {
            Contract.Assume(operandi != null);
            var cvi = mappings.GetCompileTimeConstantValueFor(operandi, block);
            if (cvi == null) return null;
            if (!Evaluator.IsNumericallyEqual(cv1, cvi)) return null;
          }
          return cv1;
      }
      return null;
    }

    /// <summary>
    /// Returns an IMetadataConstant instance that corresponds to the value that the given instruction will evaluate to at runtime.
    /// </summary>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public static IMetadataConstant GetAsCompileTimeConstantValue(Instruction instruction) {
      Contract.Requires(instruction != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      var operation = instruction.Operation;
      object value = null;
      switch (operation.OperationCode) {
        case OperationCode.Ldc_I4_0: value = 0; break;
        case OperationCode.Ldc_I4_1: value = 1; break;
        case OperationCode.Ldc_I4_2: value = 2; break;
        case OperationCode.Ldc_I4_3: value = 3; break;
        case OperationCode.Ldc_I4_4: value = 4; break;
        case OperationCode.Ldc_I4_5: value = 5; break;
        case OperationCode.Ldc_I4_6: value = 6; break;
        case OperationCode.Ldc_I4_7: value = 7; break;
        case OperationCode.Ldc_I4_8: value = 8; break;
        case OperationCode.Ldc_I4_M1: value = -1; break;
        case OperationCode.Ldc_I4:
        case OperationCode.Ldc_I4_S:
        case OperationCode.Ldc_I8:
        case OperationCode.Ldc_R4:
        case OperationCode.Ldc_R8:
        case OperationCode.Ldnull:
        case OperationCode.Ldstr: value = operation.Value; break;
        default:
          Contract.Assume(false);
          break;
      }
      return new MetadataConstant() { Value = value, Type = instruction.Type };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IMetadataConstant GetMaxValue(ITypeReference type) {
      Contract.Requires(type != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      object value = null;
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean: value = true; break;
        case PrimitiveTypeCode.Char: value = char.MaxValue; break;
        case PrimitiveTypeCode.Int8: value = sbyte.MaxValue; break;
        case PrimitiveTypeCode.Int16: value = short.MaxValue; break;
        case PrimitiveTypeCode.Int32: value = int.MaxValue; break;
        case PrimitiveTypeCode.Int64: value = long.MaxValue; break;
        case PrimitiveTypeCode.IntPtr: return Dummy.Constant;
        case PrimitiveTypeCode.UInt8: value = byte.MaxValue; break;
        case PrimitiveTypeCode.UInt16: value = ushort.MaxValue; break;
        case PrimitiveTypeCode.UInt32: value = uint.MaxValue; break;
        case PrimitiveTypeCode.UInt64: value = ulong.MaxValue; break;
        case PrimitiveTypeCode.UIntPtr: return Dummy.Constant;
        case PrimitiveTypeCode.Float32: value = float.MaxValue; break;
        case PrimitiveTypeCode.Float64: value = double.MaxValue; break;
        default: return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = type };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IMetadataConstant GetMinValue(ITypeReference type) {
      Contract.Requires(type != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      object value = null;
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean: value = false; break;
        case PrimitiveTypeCode.Char: value = char.MinValue; break;
        case PrimitiveTypeCode.Int8: value = sbyte.MinValue; break;
        case PrimitiveTypeCode.Int16: value = short.MinValue; break;
        case PrimitiveTypeCode.Int32: value = int.MinValue; break;
        case PrimitiveTypeCode.Int64: value = long.MinValue; break;
        case PrimitiveTypeCode.IntPtr: return Dummy.Constant;
        case PrimitiveTypeCode.UInt8: value = byte.MinValue; break;
        case PrimitiveTypeCode.UInt16: value = ushort.MinValue; break;
        case PrimitiveTypeCode.UInt32: value = uint.MinValue; break;
        case PrimitiveTypeCode.UInt64: value = ulong.MinValue; break;
        case PrimitiveTypeCode.UIntPtr: return Dummy.Constant;
        case PrimitiveTypeCode.Float32: value = float.MinValue; break;
        case PrimitiveTypeCode.Float64: value = double.MinValue; break;
        default: return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = type };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IMetadataConstant GetMinusOne(ITypeReference type) {
      Contract.Requires(type != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      object value = null;
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean: value = true; break;
        case PrimitiveTypeCode.Char: value = (char)0xFFFF; break;
        case PrimitiveTypeCode.Int8: value = (sbyte)-1; break;
        case PrimitiveTypeCode.Int16: value = (short)-1; break;
        case PrimitiveTypeCode.Int32: value = (int)-1; break;
        case PrimitiveTypeCode.Int64: value = (long)-1; break;
        case PrimitiveTypeCode.IntPtr: value = (IntPtr)(-1); break;
        case PrimitiveTypeCode.UInt8: value = byte.MaxValue; break;
        case PrimitiveTypeCode.UInt16: value = ushort.MaxValue; break;
        case PrimitiveTypeCode.UInt32: value = uint.MaxValue; break;
        case PrimitiveTypeCode.UInt64: value = ulong.MaxValue; break;
        case PrimitiveTypeCode.UIntPtr: value = (UIntPtr)ulong.MaxValue; break;
        case PrimitiveTypeCode.Float32: value = (float)-1; break;
        case PrimitiveTypeCode.Float64: value = (double)-1; break;
        default: return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = type };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IMetadataConstant GetOne(ITypeReference type) {
      Contract.Requires(type != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      object value = null;
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean: value = true; break;
        case PrimitiveTypeCode.Char: value = (char)1; break;
        case PrimitiveTypeCode.Int8: value = (sbyte)1; break;
        case PrimitiveTypeCode.Int16: value = (short)1; break;
        case PrimitiveTypeCode.Int32: value = (int)1; break;
        case PrimitiveTypeCode.Int64: value = (long)1; break;
        case PrimitiveTypeCode.IntPtr: value = (IntPtr)1; break;
        case PrimitiveTypeCode.UInt8: value = (byte)1; break;
        case PrimitiveTypeCode.UInt16: value = (ushort)1; break;
        case PrimitiveTypeCode.UInt32: value = (uint)1; break;
        case PrimitiveTypeCode.UInt64: value = (ulong)1; break;
        case PrimitiveTypeCode.UIntPtr: value = (UIntPtr)1; break;
        case PrimitiveTypeCode.Float32: value = (float)1; break;
        case PrimitiveTypeCode.Float64: value = (double)1; break;
        default: return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = type };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IMetadataConstant GetZero(ITypeReference type) {
      Contract.Requires(type != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      object value = null;
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean: value = false; break;
        case PrimitiveTypeCode.Char: value = (char)0; break;
        case PrimitiveTypeCode.Int8: value = (sbyte)0; break;
        case PrimitiveTypeCode.Int16: value = (short)0; break;
        case PrimitiveTypeCode.Int32: value = (int)0; break;
        case PrimitiveTypeCode.Int64: value = (long)0; break;
        case PrimitiveTypeCode.IntPtr: value = IntPtr.Zero; break;
        case PrimitiveTypeCode.UInt8: value = (byte)0; break;
        case PrimitiveTypeCode.UInt16: value = (ushort)0; break;
        case PrimitiveTypeCode.UInt32: value = (uint)0; break;
        case PrimitiveTypeCode.UInt64: value = (ulong)0; break;
        case PrimitiveTypeCode.UIntPtr: value = UIntPtr.Zero; break;
        case PrimitiveTypeCode.Float32: value = (float)0; break;
        case PrimitiveTypeCode.Float64: value = (double)0; break;
        default: return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = type };
    }

    /// <summary>
    /// Returns a compile time constant that is the same as the given constant, except that numeric values
    /// are incremented by the smallest interval appropriate to its type. If the increment causes overflow to 
    /// happen, the result is the given constant.
    /// </summary>
    public static IMetadataConstant IncreaseBySmallestInterval(IMetadataConstant operand) {
      Contract.Requires(operand != null);
      Contract.Ensures(Contract.Result<IMetadataConstant>() != null);

      object value = null;
      switch (operand.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          var sb = (sbyte)operand.Value;
          if (sb == sbyte.MaxValue) return operand;
          value = ++sb;
          break;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          var s = (short)operand.Value;
          if (s == short.MaxValue) return operand;
          value = ++s;
          break;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          var i = (int)operand.Value;
          if (i == int.MaxValue) return operand;
          value = ++i;
          break;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          var l = (long)operand.Value;
          if (l == long.MaxValue) return operand;
          value = ++l;
          break;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          var iptr = (long)(IntPtr)operand.Value;
          if (iptr == long.MaxValue) return operand;
          value = (IntPtr)(++iptr);
          break;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand.Value is byte);
          var b = (byte)operand.Value;
          if (b == byte.MaxValue) return operand;
          value = ++b;
          break;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand.Value is ushort);
          var us = (ushort)operand.Value;
          if (us == ushort.MaxValue) return operand;
          value = ++us;
          break;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand.Value is uint);
          var ui = (uint)operand.Value;
          if (ui == uint.MaxValue) return operand;
          value = ++ui;
          break;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand.Value is ulong);
          var ul = (ulong)operand.Value;
          if (ul == ulong.MaxValue) return operand;
          value = ++ul;
          break;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand.Value is UIntPtr);
          var uptr = (ulong)(UIntPtr)operand.Value;
          if (uptr == ulong.MaxValue) return operand;
          value = (UIntPtr)(++uptr);
          break;
        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand.Value is float);
          var f = (float)operand.Value;
          var incr = float.Epsilon;
          var fincr = f + incr;
          while (fincr == f) {
            incr *= 2;
            fincr += incr;
          }
          if (float.IsPositiveInfinity(fincr)) return operand;
          value = fincr;
          break;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand.Value is double);
          var d = (double)operand.Value;
          var incrd = double.Epsilon;
          var dincr = d + incrd;
          while (dincr == d) {
            incrd *= 2;
            dincr += incrd;
          }
          if (double.IsPositiveInfinity(dincr)) return operand;
          value = dincr;
          break;
        default:
          return Dummy.Constant;
      }
      return new MetadataConstant() { Value = value, Type = operand.Type };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    [ContractVerification(false)]
    public static bool IsNumericallyEqual(IMetadataConstant/*?*/ operand1, IMetadataConstant/*?*/ operand2) {
      if (operand1 == null || operand2 == null) return false;
      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return false;

      long signed1 = 0;
      ulong unsigned1 = 0;
      switch (operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          signed1 = (long)(sbyte)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          signed1 = (long)(short)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          signed1 = (long)(int)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          signed1 = (long)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          signed1 = (long)(IntPtr)operand1.Value;
        doSigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              var signed2 = (long)(sbyte)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              signed2 = (long)(short)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              signed2 = (long)(int)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              signed2 = (long)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand1.Value is IntPtr);
              signed2 = (long)(IntPtr)operand2.Value;
              goto doSigned2;
            doSigned2:
              return signed1 == signed2;
            default:
              unsigned1 = (ulong)signed1;
              goto doUnsigned1;
          }

        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          unsigned1 = ((bool)operand1.Value) ? 1UL : 0UL;
          goto doUnsigned1;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          unsigned1 = (ulong)(char)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          unsigned1 = (ulong)(byte)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          unsigned1 = (ulong)(ushort)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          unsigned1 = (ulong)(uint)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          unsigned1 = (ulong)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is UIntPtr);
          unsigned1 = (ulong)(UIntPtr)operand1.Value;
        doUnsigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              Contract.Assume(operand2.Value is bool);
              var unsigned2 = ((bool)operand2.Value) ? 1UL : 0UL;
              goto doUnsigned2;
            case PrimitiveTypeCode.Char:
              Contract.Assume(operand2.Value is char);
              unsigned2 = (ulong)(char)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              unsigned2 = (ulong)(sbyte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              unsigned2 = (ulong)(short)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              unsigned2 = (ulong)(int)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand1.Value is IntPtr);
              unsigned2 = (ulong)(IntPtr)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt8:
              Contract.Assume(operand2.Value is byte);
              unsigned2 = (ulong)(byte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt16:
              Contract.Assume(operand2.Value is ushort);
              unsigned2 = (ulong)(ushort)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt32:
              Contract.Assume(operand2.Value is uint);
              unsigned2 = (ulong)(uint)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt64:
              Contract.Assume(operand2.Value is ulong);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UIntPtr:
              Contract.Assume(operand2.Value is UIntPtr);
              unsigned2 = (ulong)(UIntPtr)operand2.Value;
            doUnsigned2:
              return unsigned1 == unsigned2;
            default:
              return false;
          }

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var d1 = (double)(float)operand1.Value;
          goto doFloat1;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d1 = (double)operand1.Value;
        doFloat1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Float32:
              Contract.Assume(operand2.Value is float);
              var d2 = (double)(float)operand2.Value;
              goto doFloat2;
            case PrimitiveTypeCode.Float64:
              Contract.Assume(operand2.Value is double);
              d2 = (double)operand2.Value;
            doFloat2:
              return d1 == d2;
            default:
              return false;
          }
      }
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    public static bool IsNumericallyGreaterThan(IMetadataConstant/*?*/ operand1, IMetadataConstant/*?*/ operand2) {
      if (operand1 == null || operand2 == null) return false;
      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return false;

      long signed1 = 0;
      ulong unsigned1 = 0;
      switch (operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          signed1 = (long)(sbyte)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          signed1 = (long)(short)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          signed1 = (long)(int)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          signed1 = (long)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          signed1 = (long)(IntPtr)operand1.Value;
        doSigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              var signed2 = (long)(sbyte)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              signed2 = (long)(short)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              signed2 = (long)(int)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              signed2 = (long)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              signed2 = (long)(IntPtr)operand2.Value;
            doSigned2:
              return signed1 > signed2;
            default:
              unsigned1 = (ulong)signed1;
              goto doUnsigned1;
          }

        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          unsigned1 = ((bool)operand1.Value) ? 1UL : 0UL;
          goto doUnsigned1;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          unsigned1 = (ulong)(char)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          unsigned1 = (ulong)(byte)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          unsigned1 = (ulong)(ushort)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          unsigned1 = (ulong)(uint)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          unsigned1 = (ulong)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is UIntPtr);
          unsigned1 = (ulong)(UIntPtr)operand1.Value;
        doUnsigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              Contract.Assume(operand2.Value is bool);
              var unsigned2 = ((bool)operand2.Value) ? 1UL : 0UL;
              goto doUnsigned2;
            case PrimitiveTypeCode.Char:
              Contract.Assume(operand2.Value is char);
              unsigned2 = (ulong)(char)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              unsigned2 = (ulong)(sbyte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              unsigned2 = (ulong)(short)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              unsigned2 = (ulong)(int)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              unsigned2 = (ulong)(IntPtr)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt8:
              Contract.Assume(operand2.Value is byte);
              unsigned2 = (ulong)(byte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt16:
              Contract.Assume(operand2.Value is ushort);
              unsigned2 = (ulong)(ushort)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt32:
              Contract.Assume(operand2.Value is uint);
              unsigned2 = (ulong)(uint)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt64:
              Contract.Assume(operand2.Value is ulong);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UIntPtr:
              Contract.Assume(operand2.Value is UIntPtr);
              unsigned2 = (ulong)(UIntPtr)operand2.Value;
            doUnsigned2:
              return unsigned1 > unsigned2;
            default:
              return false;
          }

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var d1 = (double)(float)operand1.Value;
          goto doFloat1;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d1 = (double)operand1.Value;
        doFloat1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Float32:
              Contract.Assume(operand2.Value is float);
              var d2 = (double)(float)operand2.Value;
              goto doFloat2;
            case PrimitiveTypeCode.Float64:
              Contract.Assume(operand2.Value is double);
              d2 = (double)operand2.Value;
            doFloat2:
              return d1 > d2;
            default:
              return false;
          }
      }
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    public static bool IsNumericallyGreaterThanOrEqualTo(IMetadataConstant/*?*/ operand1, IMetadataConstant/*?*/ operand2) {
      if (operand1 == null || operand2 == null) return false;
      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return false;

      long signed1 = 0;
      ulong unsigned1 = 0;
      switch (operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          signed1 = (long)(sbyte)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          signed1 = (long)(short)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          signed1 = (long)(int)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          signed1 = (long)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          signed1 = (long)(IntPtr)operand1.Value;
        doSigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              var signed2 = (long)(sbyte)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              signed2 = (long)(short)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              signed2 = (long)(int)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              signed2 = (long)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              signed2 = (long)(IntPtr)operand2.Value;
            doSigned2:
              return signed1 >= signed2;
            default:
              unsigned1 = (ulong)signed1;
              goto doUnsigned1;
          }

        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          unsigned1 = ((bool)operand1.Value) ? 1UL : 0UL;
          goto doUnsigned1;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          unsigned1 = (ulong)(char)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          unsigned1 = (ulong)(byte)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          unsigned1 = (ulong)(ushort)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          unsigned1 = (ulong)(uint)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          unsigned1 = (ulong)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is UIntPtr);
          unsigned1 = (ulong)(UIntPtr)operand1.Value;
        doUnsigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              Contract.Assume(operand2.Value is bool);
              var unsigned2 = ((bool)operand2.Value) ? 1UL : 0UL;
              goto doUnsigned2;
            case PrimitiveTypeCode.Char:
              Contract.Assume(operand2.Value is char);
              unsigned2 = (ulong)(char)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              unsigned2 = (ulong)(sbyte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              unsigned2 = (ulong)(short)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              unsigned2 = (ulong)(int)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              unsigned2 = (ulong)(IntPtr)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt8:
              Contract.Assume(operand2.Value is byte);
              unsigned2 = (ulong)(byte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt16:
              Contract.Assume(operand2.Value is ushort);
              unsigned2 = (ulong)(ushort)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt32:
              Contract.Assume(operand2.Value is uint);
              unsigned2 = (ulong)(uint)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt64:
              Contract.Assume(operand2.Value is ulong);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UIntPtr:
              Contract.Assume(operand2.Value is UIntPtr);
              unsigned2 = (ulong)(UIntPtr)operand2.Value;
            doUnsigned2:
              return unsigned1 >= unsigned2;
            default:
              return false;
          }

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var d1 = (double)(float)operand1.Value;
          goto doFloat1;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d1 = (double)operand1.Value;
        doFloat1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Float32:
              Contract.Assume(operand2.Value is float);
              var d2 = (double)(float)operand2.Value;
              goto doFloat2;
            case PrimitiveTypeCode.Float64:
              Contract.Assume(operand2.Value is double);
              d2 = (double)operand2.Value;
            doFloat2:
              return d1 >= d2;
            default:
              return false;
          }
      }
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    public static bool IsNumericallyLessThan(IMetadataConstant/*?*/ operand1, IMetadataConstant/*?*/ operand2) {
      if (operand1 == null || operand2 == null) return false;
      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return false;

      long signed1 = 0;
      ulong unsigned1 = 0;
      switch (operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          signed1 = (long)(sbyte)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          signed1 = (long)(short)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          signed1 = (long)(int)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          signed1 = (long)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          signed1 = (long)(IntPtr)operand1.Value;
        doSigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              var signed2 = (long)(sbyte)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              signed2 = (long)(short)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              signed2 = (long)(int)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              signed2 = (long)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              signed2 = (long)(IntPtr)operand2.Value;
            doSigned2:
              return signed1 < signed2;
            default:
              unsigned1 = (ulong)signed1;
              goto doUnsigned1;
          }

        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          unsigned1 = ((bool)operand1.Value) ? 1UL : 0UL;
          goto doUnsigned1;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          unsigned1 = (ulong)(char)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          unsigned1 = (ulong)(byte)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          unsigned1 = (ulong)(ushort)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          unsigned1 = (ulong)(uint)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          unsigned1 = (ulong)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is UIntPtr);
          unsigned1 = (ulong)(UIntPtr)operand1.Value;
        doUnsigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              Contract.Assume(operand2.Value is bool);
              var unsigned2 = ((bool)operand2.Value) ? 1UL : 0UL;
              goto doUnsigned2;
            case PrimitiveTypeCode.Char:
              Contract.Assume(operand2.Value is char);
              unsigned2 = (ulong)(char)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              unsigned2 = (ulong)(sbyte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              unsigned2 = (ulong)(short)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              unsigned2 = (ulong)(int)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              unsigned2 = (ulong)(IntPtr)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt8:
              Contract.Assume(operand2.Value is byte);
              unsigned2 = (ulong)(byte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt16:
              Contract.Assume(operand2.Value is ushort);
              unsigned2 = (ulong)(ushort)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt32:
              Contract.Assume(operand2.Value is uint);
              unsigned2 = (ulong)(uint)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt64:
              Contract.Assume(operand2.Value is ulong);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UIntPtr:
              Contract.Assume(operand2.Value is UIntPtr);
              unsigned2 = (ulong)(UIntPtr)operand2.Value;
            doUnsigned2:
              return unsigned1 < unsigned2;
            default:
              return false;
          }

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var d1 = (double)(float)operand1.Value;
          goto doFloat1;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d1 = (double)operand1.Value;
        doFloat1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Float32:
              Contract.Assume(operand2.Value is float);
              var d2 = (double)(float)operand2.Value;
              goto doFloat2;
            case PrimitiveTypeCode.Float64:
              Contract.Assume(operand2.Value is double);
              d2 = (double)operand2.Value;
            doFloat2:
              return d1 < d2;
            default:
              return false;
          }
      }
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    public static bool IsNumericallyLessThanOrEqualTo(IMetadataConstant/*?*/ operand1, IMetadataConstant/*?*/ operand2) {
      if (operand1 == null || operand2 == null) return false;
      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return false;

      long signed1 = 0;
      ulong unsigned1 = 0;
      switch (operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          signed1 = (long)(sbyte)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          signed1 = (long)(short)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          signed1 = (long)(int)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          signed1 = (long)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          signed1 = (long)(IntPtr)operand1.Value;
        doSigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              var signed2 = (long)(sbyte)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              signed2 = (long)(short)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              signed2 = (long)(int)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              signed2 = (long)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              signed2 = (long)(IntPtr)operand2.Value;
            doSigned2:
              return signed1 <= signed2;
            default:
              unsigned1 = (ulong)signed1;
              goto doUnsigned1;
          }

        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          unsigned1 = ((bool)operand1.Value) ? 1UL : 0UL;
          goto doUnsigned1;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          unsigned1 = (ulong)(char)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          unsigned1 = (ulong)(byte)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          unsigned1 = (ulong)(ushort)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          unsigned1 = (ulong)(uint)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          unsigned1 = (ulong)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is UIntPtr);
          unsigned1 = (ulong)(UIntPtr)operand1.Value;
        doUnsigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              Contract.Assume(operand2.Value is bool);
              var unsigned2 = ((bool)operand2.Value) ? 1UL : 0UL;
              goto doUnsigned2;
            case PrimitiveTypeCode.Char:
              Contract.Assume(operand2.Value is char);
              unsigned2 = (ulong)(char)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              unsigned2 = (ulong)(sbyte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              unsigned2 = (ulong)(short)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              unsigned2 = (ulong)(int)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              unsigned2 = (ulong)(IntPtr)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt8:
              Contract.Assume(operand2.Value is byte);
              unsigned2 = (ulong)(byte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt16:
              Contract.Assume(operand2.Value is ushort);
              unsigned2 = (ulong)(ushort)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt32:
              Contract.Assume(operand2.Value is uint);
              unsigned2 = (ulong)(uint)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt64:
              Contract.Assume(operand2.Value is ulong);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UIntPtr:
              Contract.Assume(operand2.Value is UIntPtr);
              unsigned2 = (ulong)(UIntPtr)operand2.Value;
            doUnsigned2:
              return unsigned1 <= unsigned2;
            default:
              return false;
          }

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var d1 = (double)(float)operand1.Value;
          goto doFloat1;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d1 = (double)operand1.Value;
        doFloat1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Float32:
              Contract.Assume(operand2.Value is float);
              var d2 = (double)(float)operand2.Value;
              goto doFloat2;
            case PrimitiveTypeCode.Float64:
              Contract.Assume(operand2.Value is double);
              d2 = (double)operand2.Value;
            doFloat2:
              return d1 <= d2;
            default:
              return false;
          }
      }
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand"></param>
    /// <returns></returns>
    public static bool IsNegative(IMetadataConstant operand) {
      Contract.Requires(operand != null);

      switch (operand.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          return 0 > (sbyte)operand.Value;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          return 0 > (short)operand.Value;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          return 0 > (int)operand.Value;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          return 0 > (long)operand.Value;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          return 0 > (long)(IntPtr)operand.Value;
        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand.Value is float);
          return 0 > (float)operand.Value;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand.Value is double);
          return 0 > (double)operand.Value;
        default:
          return false;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand"></param>
    /// <returns></returns>
    public static bool IsNonNegative(IMetadataConstant operand) {
      Contract.Requires(operand != null);

      switch (operand.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          return 0 <= (sbyte)operand.Value;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          return 0 <= (short)operand.Value;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          return 0 <= (int)operand.Value;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          return 0 <= (long)operand.Value;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          return 0 <= (long)(IntPtr)operand.Value;
        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand.Value is float);
          return 0 <= (float)operand.Value;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand.Value is double);
          return 0 <= (double)operand.Value;
        default:
          return true;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand"></param>
    /// <returns></returns>
    public static bool IsPositive(IMetadataConstant operand) {
      Contract.Requires(operand != null);

      switch (operand.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          return 0 < (sbyte)operand.Value;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          return 0 < (short)operand.Value;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          return 0 < (int)operand.Value;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          return 0 < (long)operand.Value;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          return 0 < (long)(IntPtr)operand.Value;
        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand.Value is float);
          return 0 < (float)operand.Value;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand.Value is double);
          return 0 < (double)operand.Value;
        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand.Value is bool);
          return (bool)operand.Value;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand.Value is char);
          return 0 < (char)operand.Value;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand.Value is byte);
          return 0 < (byte)operand.Value;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand.Value is ushort);
          return 0 < (ushort)operand.Value;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand.Value is uint);
          return 0 < (uint)operand.Value;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand.Value is ulong);
          return 0 < (ulong)operand.Value;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand.Value is UIntPtr);
          return 0 < (ulong)(UIntPtr)operand.Value;
        default:
          return false;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand"></param>
    /// <returns></returns>
    public static bool IsSmallerThanMinusOne(IMetadataConstant operand) {
      Contract.Requires(operand != null);

      switch (operand.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand.Value is sbyte);
          return -1 > (sbyte)operand.Value;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand.Value is short);
          return -1 > (short)operand.Value;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand.Value is int);
          return -1 > (int)operand.Value;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand.Value is long);
          return -1 > (long)operand.Value;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand.Value is IntPtr);
          return -1 > (long)(IntPtr)operand.Value;
        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand.Value is float);
          return -1 > (float)operand.Value;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand.Value is double);
          return -1 > (double)operand.Value;
        default:
          return false;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Max(IMetadataConstant/*?*/ operand1, IMetadataConstant/*?*/ operand2) {
      if (operand1 == null || operand2 == null) return null;
      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return Dummy.Constant;

      long signed1 = 0;
      ulong unsigned1 = 0;
      switch (operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          signed1 = (long)(sbyte)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          signed1 = (long)(short)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          signed1 = (long)(int)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          signed1 = (long)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          signed1 = (long)(IntPtr)operand1.Value;
        doSigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              var signed2 = (long)(sbyte)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              signed2 = (long)(short)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              signed2 = (long)(int)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              signed2 = (long)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              signed2 = (long)(IntPtr)operand2.Value;
            doSigned2:
              if (signed1 >= signed2) return operand1;
              return operand2;
            default:
              unsigned1 = (ulong)signed1;
              goto doUnsigned1;
          }

        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          unsigned1 = ((bool)operand1.Value) ? 1UL : 0UL;
          goto doUnsigned1;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          unsigned1 = (ulong)(char)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          unsigned1 = (ulong)(byte)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          unsigned1 = (ulong)(ushort)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          unsigned1 = (ulong)(uint)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          unsigned1 = (ulong)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is UIntPtr);
          unsigned1 = (ulong)(UIntPtr)operand1.Value;
        doUnsigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              Contract.Assume(operand2.Value is bool);
              var unsigned2 = ((bool)operand2.Value) ? 1UL : 0UL;
              goto doUnsigned2;
            case PrimitiveTypeCode.Char:
              Contract.Assume(operand2.Value is char);
              unsigned2 = (ulong)(char)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              unsigned2 = (ulong)(sbyte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              unsigned2 = (ulong)(short)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              unsigned2 = (ulong)(int)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              unsigned2 = (ulong)(IntPtr)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt8:
              Contract.Assume(operand2.Value is byte);
              unsigned2 = (ulong)(byte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt16:
              Contract.Assume(operand2.Value is ushort);
              unsigned2 = (ulong)(ushort)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt32:
              Contract.Assume(operand2.Value is uint);
              unsigned2 = (ulong)(uint)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt64:
              Contract.Assume(operand2.Value is ulong);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UIntPtr:
              Contract.Assume(operand2.Value is UIntPtr);
              unsigned2 = (ulong)(UIntPtr)operand2.Value;
            doUnsigned2:
              if (unsigned1 >= unsigned2) return operand1;
              return operand2;
            default:
              return null;
          }

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var d1 = (double)(float)operand1.Value;
          goto doFloat1;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d1 = (double)operand1.Value;
        doFloat1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Float32:
              Contract.Assume(operand2.Value is float);
              var d2 = (double)(float)operand2.Value;
              goto doFloat2;
            case PrimitiveTypeCode.Float64:
              Contract.Assume(operand2.Value is double);
              d2 = (double)operand2.Value;
            doFloat2:
              if (d1 >= d2) return operand1;
              return operand2;
            default:
              return null;
          }
      }
      return null;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <returns></returns>
    public static IMetadataConstant/*?*/ Min(IMetadataConstant/*?*/ operand1, IMetadataConstant/*?*/ operand2) {
      if (operand1 == null || operand2 == null) return null;
      if (operand1 == Dummy.Constant || operand2 == Dummy.Constant) return Dummy.Constant;

      long signed1 = 0;
      ulong unsigned1 = 0;
      switch (operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int8:
          Contract.Assume(operand1.Value is sbyte);
          signed1 = (long)(sbyte)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int16:
          Contract.Assume(operand1.Value is short);
          signed1 = (long)(short)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int32:
          Contract.Assume(operand1.Value is int);
          signed1 = (long)(int)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.Int64:
          Contract.Assume(operand1.Value is long);
          signed1 = (long)operand1.Value;
          goto doSigned1;
        case PrimitiveTypeCode.IntPtr:
          Contract.Assume(operand1.Value is IntPtr);
          signed1 = (long)(IntPtr)operand1.Value;
        doSigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              var signed2 = (long)(sbyte)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              signed2 = (long)(short)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              signed2 = (long)(int)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              signed2 = (long)operand2.Value;
              goto doSigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              signed2 = (long)(IntPtr)operand2.Value;
            doSigned2:
              if (signed1 <= signed2) return operand1;
              return operand2;
            default:
              unsigned1 = (ulong)signed1;
              goto doUnsigned1;
          }

        case PrimitiveTypeCode.Boolean:
          Contract.Assume(operand1.Value is bool);
          unsigned1 = ((bool)operand1.Value) ? 1UL : 0UL;
          goto doUnsigned1;
        case PrimitiveTypeCode.Char:
          Contract.Assume(operand1.Value is char);
          unsigned1 = (ulong)(char)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt8:
          Contract.Assume(operand1.Value is byte);
          unsigned1 = (ulong)(byte)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt16:
          Contract.Assume(operand1.Value is ushort);
          unsigned1 = (ulong)(ushort)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt32:
          Contract.Assume(operand1.Value is uint);
          unsigned1 = (ulong)(uint)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UInt64:
          Contract.Assume(operand1.Value is ulong);
          unsigned1 = (ulong)operand1.Value;
          goto doUnsigned1;
        case PrimitiveTypeCode.UIntPtr:
          Contract.Assume(operand1.Value is UIntPtr);
          unsigned1 = (ulong)(UIntPtr)operand1.Value;
        doUnsigned1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              Contract.Assume(operand2.Value is bool);
              var unsigned2 = ((bool)operand2.Value) ? 1UL : 0UL;
              goto doUnsigned2;
            case PrimitiveTypeCode.Char:
              Contract.Assume(operand2.Value is char);
              unsigned2 = (ulong)(char)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int8:
              Contract.Assume(operand2.Value is sbyte);
              unsigned2 = (ulong)(sbyte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int16:
              Contract.Assume(operand2.Value is short);
              unsigned2 = (ulong)(short)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int32:
              Contract.Assume(operand2.Value is int);
              unsigned2 = (ulong)(int)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.Int64:
              Contract.Assume(operand2.Value is long);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.IntPtr:
              Contract.Assume(operand2.Value is IntPtr);
              unsigned2 = (ulong)(IntPtr)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt8:
              Contract.Assume(operand2.Value is byte);
              unsigned2 = (ulong)(byte)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt16:
              Contract.Assume(operand2.Value is ushort);
              unsigned2 = (ulong)(ushort)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt32:
              Contract.Assume(operand2.Value is uint);
              unsigned2 = (ulong)(uint)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UInt64:
              Contract.Assume(operand2.Value is ulong);
              unsigned2 = (ulong)operand2.Value;
              goto doUnsigned2;
            case PrimitiveTypeCode.UIntPtr:
              Contract.Assume(operand2.Value is UIntPtr);
              unsigned2 = (ulong)(UIntPtr)operand2.Value;
            doUnsigned2:
              if (unsigned1 <= unsigned2) return operand1;
              return operand2;
            default:
              return null;
          }

        case PrimitiveTypeCode.Float32:
          Contract.Assume(operand1.Value is float);
          var d1 = (double)(float)operand1.Value;
          goto doFloat1;
        case PrimitiveTypeCode.Float64:
          Contract.Assume(operand1.Value is double);
          d1 = (double)operand1.Value;
        doFloat1:
          switch (operand2.Type.TypeCode) {
            case PrimitiveTypeCode.Float32:
              Contract.Assume(operand2.Value is float);
              var d2 = (double)(float)operand2.Value;
              goto doFloat2;
            case PrimitiveTypeCode.Float64:
              Contract.Assume(operand2.Value is double);
              d2 = (double)operand2.Value;
            doFloat2:
              if (d1 <= d2) return operand1;
              return operand2;
            default:
              return null;
          }
      }
      return null;

    }


  }

}