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
using System.Diagnostics.Contracts;

namespace Microsoft.Cci {
  /// <summary>
  /// Class containing helper routines for Expressions
  /// </summary>
  public static class ExpressionHelper {

    /// <summary>
    /// Returns true if the given constant expression contains a finite numeric value. In other words, infinities and NaN are excluded.
    /// </summary>
    /// <param name="constExpression"></param>
    /// <returns></returns>
    public static bool IsFiniteNumeric(ICompileTimeConstant constExpression) {
      IConvertible/*?*/ ic = constExpression.Value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.SByte:
        case System.TypeCode.Int16:
        case System.TypeCode.Int32:
        case System.TypeCode.Int64:
        case System.TypeCode.Byte: 
        case System.TypeCode.UInt16:
        case System.TypeCode.UInt32:
        case System.TypeCode.UInt64:
          return true;
        case System.TypeCode.Double:
          var d = ic.ToDouble(null);
          return !(Double.IsNaN(d) || Double.IsInfinity(d));
        case System.TypeCode.Single:
          var s = ic.ToSingle(null);
          return !(Single.IsNaN(s) || Single.IsInfinity(s));
      }
      return false;
    }

    /// <summary>
    /// Returns true if the constant is an integral value that falls in the range of the target type. 
    /// The target type does have to be an integral type. If it is not, this method always returns false.
    /// </summary>
    public static bool IsIntegerInRangeOf(ICompileTimeConstant constExpression, ITypeReference targetType) {
      switch (targetType.TypeCode) {
        case PrimitiveTypeCode.UInt8: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.Byte:
                return true;
              case System.TypeCode.SByte:
                return byte.MinValue <= ic.ToSByte(null);
              case System.TypeCode.Int16:
                short s = ic.ToInt16(null);
                return byte.MinValue <= s && s <= byte.MaxValue;
              case System.TypeCode.Int32:
                int i = ic.ToInt32(null);
                return byte.MinValue <= i && i <= byte.MaxValue;
              case System.TypeCode.Int64:
                long lng = ic.ToInt64(null);
                return byte.MinValue <= lng && lng <= byte.MaxValue;
              case System.TypeCode.UInt16:
                return ic.ToUInt16(null) <= byte.MaxValue;
              case System.TypeCode.UInt32:
                return ic.ToUInt32(null) <= byte.MaxValue;
              case System.TypeCode.UInt64:
                return ic.ToUInt64(null) <= byte.MaxValue;
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return byte.MinValue <= d && d <= byte.MaxValue;
            }
            return false;
          }
        case PrimitiveTypeCode.UInt16: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.Byte:
              case System.TypeCode.UInt16:
                return true;
              case System.TypeCode.SByte:
                return ushort.MinValue <= ic.ToSByte(null);
              case System.TypeCode.Int16:
                return ushort.MinValue <= ic.ToInt16(null);
              case System.TypeCode.Int32:
                int i = ic.ToInt32(null);
                return ushort.MinValue <= i && i <= ushort.MaxValue;
              case System.TypeCode.Int64:
                long lng = ic.ToInt64(null);
                return ushort.MinValue <= lng && lng <= ushort.MaxValue;
              case System.TypeCode.UInt32:
                return ic.ToUInt32(null) <= ushort.MaxValue;
              case System.TypeCode.UInt64:
                return ic.ToUInt64(null) <= ushort.MaxValue;
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return ushort.MinValue <= d && d <= ushort.MaxValue;
            }
            return false;
          }
        case PrimitiveTypeCode.UInt32: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.Byte:
              case System.TypeCode.UInt16:
              case System.TypeCode.UInt32:
                return true;
              case System.TypeCode.SByte:
                return uint.MinValue <= ic.ToSByte(null);
              case System.TypeCode.Int16:
                return uint.MinValue <= ic.ToInt16(null);
              case System.TypeCode.Int32:
                return uint.MinValue <= ic.ToInt32(null);
              case System.TypeCode.Int64:
                long lng = ic.ToInt64(null);
                return uint.MinValue <= lng && lng <= uint.MaxValue;
              case System.TypeCode.UInt64:
                return ic.ToUInt64(null) <= uint.MaxValue;
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return uint.MinValue <= d && d <= uint.MaxValue;
            }
            return false;
          }
        case PrimitiveTypeCode.UInt64: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.Byte:
              case System.TypeCode.UInt16:
              case System.TypeCode.UInt32:
              case System.TypeCode.UInt64:
                return true;
              case System.TypeCode.SByte:
                return 0 <= ic.ToSByte(null);
              case System.TypeCode.Int16:
                return 0 <= ic.ToInt16(null);
              case System.TypeCode.Int32:
                return 0 <= ic.ToInt32(null);
              case System.TypeCode.Int64:
                return 0 <= ic.ToInt64(null);
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return 0 <= d && d <= ulong.MaxValue;
            }
            return false;
          }
        case PrimitiveTypeCode.Int8: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.SByte:
                return true;
              case System.TypeCode.Int16:
                short s = ic.ToInt16(null);
                return sbyte.MinValue <= s && s <= sbyte.MaxValue;
              case System.TypeCode.Int32:
                int i = ic.ToInt32(null);
                return sbyte.MinValue <= i && i <= sbyte.MaxValue;
              case System.TypeCode.Int64:
                long lng = ic.ToInt64(null);
                return sbyte.MinValue <= lng && lng <= sbyte.MaxValue;
              case System.TypeCode.Byte:
                return ic.ToByte(null) <= sbyte.MaxValue;
              case System.TypeCode.UInt16:
                return ic.ToUInt16(null) <= sbyte.MaxValue;
              case System.TypeCode.UInt32:
                return ic.ToUInt32(null) <= sbyte.MaxValue;
              case System.TypeCode.UInt64:
                return ic.ToUInt64(null) <= (ulong)sbyte.MaxValue;
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return sbyte.MinValue <= d && d <= sbyte.MaxValue;
            }
            return false;
          }
        case PrimitiveTypeCode.Int16: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.SByte:
              case System.TypeCode.Byte:
              case System.TypeCode.Int16:
                return true;
              case System.TypeCode.Int32:
                int i = ic.ToInt32(null);
                return short.MinValue <= i && i <= short.MaxValue;
              case System.TypeCode.Int64:
                long lng = ic.ToInt64(null);
                return short.MinValue <= lng && lng <= short.MaxValue;
              case System.TypeCode.UInt16:
                return ic.ToUInt16(null) <= short.MaxValue;
              case System.TypeCode.UInt32:
                return ic.ToUInt32(null) <= short.MaxValue;
              case System.TypeCode.UInt64:
                return ic.ToUInt64(null) <= (ulong)short.MaxValue;
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return short.MinValue <= d && d <= short.MaxValue;
            }
            return false;
          }
        case PrimitiveTypeCode.Int32: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.SByte:
              case System.TypeCode.Byte:
              case System.TypeCode.Int16:
              case System.TypeCode.UInt16:
              case System.TypeCode.Int32:
                return true;
              case System.TypeCode.Int64:
                long lng = ic.ToInt64(null);
                return int.MinValue <= lng && lng <= int.MaxValue;
              case System.TypeCode.UInt32:
                return ic.ToUInt32(null) <= int.MaxValue;
              case System.TypeCode.UInt64:
                return ic.ToUInt64(null) <= int.MaxValue;
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return int.MinValue <= d && d <= int.MaxValue;
            }
            return false;
          }
        case PrimitiveTypeCode.Int64: {
            IConvertible/*?*/ ic = constExpression.Value as IConvertible;
            if (ic == null) return false;
            switch (ic.GetTypeCode()) {
              case System.TypeCode.SByte:
              case System.TypeCode.Byte:
              case System.TypeCode.Int16:
              case System.TypeCode.UInt16:
              case System.TypeCode.Int32:
              case System.TypeCode.UInt32:
              case System.TypeCode.Int64:
                return true;
              case System.TypeCode.UInt64:
                return ic.ToUInt64(null) <= int.MaxValue;
              case System.TypeCode.Decimal:
                decimal d = ic.ToDecimal(null);
                return long.MinValue <= d && d <= long.MaxValue;
            }
            return false;
          }
      }
      return false;
    }

    /// <summary>
    /// True if the given expression is a compile time constant with a value that is a boxed -1 of type byte, int, long, sbyte, short, uint, ulong or ushort.
    /// </summary>
    [Pure]
    public static bool IsIntegralMinusOne(IExpression expression) {
      ICompileTimeConstant/*?*/ constExpression = expression as ICompileTimeConstant;
      if (constExpression == null) return false;
      return ExpressionHelper.IsIntegralMinusOne(constExpression);
    }

    /// <summary>
    /// True if the value is a boxed -1 of type byte, int, long, sbyte, short, uint, ulong or ushort.
    /// </summary>
    [Pure]
    public static bool IsIntegralMinusOne(ICompileTimeConstant constExpression) {
      IConvertible/*?*/ ic = constExpression.Value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.SByte: return ic.ToSByte(null) == -1;
        case System.TypeCode.Int16: return ic.ToInt16(null) == -1;
        case System.TypeCode.Int32: return ic.ToInt32(null) == -1;
        case System.TypeCode.Int64: return ic.ToInt64(null) == -1;
        case System.TypeCode.Byte: return ic.ToByte(null) == byte.MaxValue;
        case System.TypeCode.UInt16: return ic.ToUInt16(null) == ushort.MaxValue;
        case System.TypeCode.UInt32: return ic.ToUInt32(null) == uint.MaxValue;
        case System.TypeCode.UInt64: return ic.ToUInt64(null) == ulong.MaxValue;
      }
      return false;
    }

    /// <summary>
    /// True if the given expression is a compile time constant with a value that is a boxed integer of type byte, int, long, sbyte, short, uint, ulong or ushort
    /// that is not equal to 0.
    /// </summary>
    [Pure]
    public static bool IsIntegralNonzero(IExpression expression) {
      ICompileTimeConstant/*?*/ constExpression = expression as ICompileTimeConstant;
      if (constExpression == null) return false;
      return ExpressionHelper.IsIntegralNonzero(constExpression);
    }

    /// <summary>
    /// True if the value is a boxed zero of type byte, int, long, sbyte, short, uint, ulong, ushort or bool that is not equal to 0.
    /// </summary>
    [Pure]
    public static bool IsIntegralNonzero(ICompileTimeConstant constExpression) {
      IConvertible/*?*/ ic = constExpression.Value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.SByte: return ic.ToSByte(null) != 0;
        case System.TypeCode.Int16: return ic.ToInt16(null) != 0;
        case System.TypeCode.Int32: return ic.ToInt32(null) != 0;
        case System.TypeCode.Int64: return ic.ToInt64(null) != 0;
        case System.TypeCode.Byte: return ic.ToByte(null) != 0;
        case System.TypeCode.UInt16: return ic.ToUInt16(null) != 0;
        case System.TypeCode.UInt32: return ic.ToUInt32(null) != 0;
        case System.TypeCode.UInt64: return ic.ToUInt64(null) != 0;
        case System.TypeCode.Boolean: return ic.ToBoolean(null);
      }
      return false;
    }

    /// <summary>
    /// True if the given expression is a compile time constant with a value that is a boxed 1 of type byte, int, long, sbyte, short, uint, ulong, ushort or bool.
    /// </summary>
    [Pure]
    public static bool IsIntegralOne(IExpression expression) {
      ICompileTimeConstant/*?*/ constExpression = expression as ICompileTimeConstant;
      if (constExpression == null) return false;
      return ExpressionHelper.IsIntegralOne(constExpression);
    }

    /// <summary>
    /// True if the value is a boxed 1 of type byte, int, long, sbyte, short, uint, ulong, ushort or bool.
    /// </summary>
    [Pure]
    public static bool IsIntegralOne(ICompileTimeConstant constExpression) {
      IConvertible/*?*/ ic = constExpression.Value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.SByte: return ic.ToSByte(null) == 1;
        case System.TypeCode.Int16: return ic.ToInt16(null) == 1;
        case System.TypeCode.Int32: return ic.ToInt32(null) == 1;
        case System.TypeCode.Int64: return ic.ToInt64(null) == 1;
        case System.TypeCode.Byte: return ic.ToByte(null) == 1;
        case System.TypeCode.UInt16: return ic.ToUInt16(null) == 1;
        case System.TypeCode.UInt32: return ic.ToUInt32(null) == 1;
        case System.TypeCode.UInt64: return ic.ToUInt64(null) == 1;
        case System.TypeCode.Boolean: return ic.ToBoolean(null);
      }
      return false;
    }

    /// <summary>
    /// True if the value is a boxed zero of type byte, int, long, sbyte, short, uint, ulong, ushort, float, double or decimal.
    /// </summary>
    [Pure]
    public static bool IsNumericOne(ICompileTimeConstant constExpression) {
      IConvertible/*?*/ ic = constExpression.Value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.SByte: return ic.ToSByte(null) == 1;
        case System.TypeCode.Int16: return ic.ToInt16(null) == 1;
        case System.TypeCode.Int32: return ic.ToInt32(null) == 1;
        case System.TypeCode.Int64: return ic.ToInt64(null) == 1;
        case System.TypeCode.Byte: return ic.ToByte(null) == 1;
        case System.TypeCode.UInt16: return ic.ToUInt16(null) == 1;
        case System.TypeCode.UInt32: return ic.ToUInt32(null) == 1;
        case System.TypeCode.UInt64: return ic.ToUInt64(null) == 1;
        case System.TypeCode.Single: return ic.ToSingle(null) == 1;
        case System.TypeCode.Double: return ic.ToDouble(null) == 1;
        case System.TypeCode.Decimal: return ic.ToDecimal(null) == 1;
      }
      return false;
    }

    /// <summary>
    /// True if the given expression is a compile time constant with a value that is a boxed zero of type byte, int, long, sbyte, short, uint, ulong, ushort or bool.
    /// </summary>
    [Pure]
    public static bool IsIntegralZero(IExpression expression) {
      ICompileTimeConstant/*?*/ constExpression = expression as ICompileTimeConstant;
      if (constExpression == null) return false;
      return ExpressionHelper.IsIntegralZero(constExpression);
    }

    /// <summary>
    /// True if the value is a boxed zero of type byte, int, long, sbyte, short, uint, ulong, ushort or bool.
    /// </summary>
    [Pure]
    public static bool IsIntegralZero(ICompileTimeConstant constExpression) {
      IConvertible/*?*/ ic = constExpression.Value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.SByte: return ic.ToSByte(null) == 0;
        case System.TypeCode.Int16: return ic.ToInt16(null) == 0;
        case System.TypeCode.Int32: return ic.ToInt32(null) == 0;
        case System.TypeCode.Int64: return ic.ToInt64(null) == 0;
        case System.TypeCode.Byte: return ic.ToByte(null) == 0;
        case System.TypeCode.UInt16: return ic.ToUInt16(null) == 0;
        case System.TypeCode.UInt32: return ic.ToUInt32(null) == 0;
        case System.TypeCode.UInt64: return ic.ToUInt64(null) == 0;
        case System.TypeCode.Boolean: return !ic.ToBoolean(null);
      }
      return false;
    }

    /// <summary>
    /// True if the value is a boxed zero of type byte, int, long, sbyte, short, uint, ulong, ushort, float, double or decimal.
    /// </summary>
    [Pure]
    public static bool IsNumericZero(ICompileTimeConstant constExpression) {
      IConvertible/*?*/ ic = constExpression.Value as IConvertible;
      if (ic == null) return false;
      switch (ic.GetTypeCode()) {
        case System.TypeCode.SByte: return ic.ToSByte(null) == 0;
        case System.TypeCode.Int16: return ic.ToInt16(null) == 0;
        case System.TypeCode.Int32: return ic.ToInt32(null) == 0;
        case System.TypeCode.Int64: return ic.ToInt64(null) == 0;
        case System.TypeCode.Byte: return ic.ToByte(null) == 0;
        case System.TypeCode.UInt16: return ic.ToUInt16(null) == 0;
        case System.TypeCode.UInt32: return ic.ToUInt32(null) == 0;
        case System.TypeCode.UInt64: return ic.ToUInt64(null) == 0;
        case System.TypeCode.Single: return ic.ToSingle(null) == 0;
        case System.TypeCode.Double: return ic.ToDouble(null) == 0;
        case System.TypeCode.Decimal: return ic.ToDecimal(null) == 0;
      }
      return false;
    }

    /// <summary>
    /// True if the given expression is a compile time literal with a null value.
    /// </summary>
    [Pure]
    public static bool IsNullLiteral(IExpression expression) {
      ICompileTimeConstant/*?*/ constExpression = expression as ICompileTimeConstant;
      return constExpression != null && constExpression.Value == null;
    }

    /// <summary>
    /// True if the given expression is a compile time constant with a value that is equal to IntPtr.Zero or UIntPtr.Zero.
    /// </summary>
    public static bool IsZeroIntPtr(IExpression expression) {
      ICompileTimeConstant/*?*/ constExpression = expression as ICompileTimeConstant;
      if (constExpression == null) return false;
      var value = constExpression.Value;
      var tc = constExpression.Type.TypeCode;
      if (tc == PrimitiveTypeCode.IntPtr) return ((IntPtr)value) == IntPtr.Zero;
      else if (tc == PrimitiveTypeCode.UIntPtr) return ((UIntPtr)value) == UIntPtr.Zero;
      else return false;
    }

  }
}
