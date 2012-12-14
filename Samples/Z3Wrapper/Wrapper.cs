using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Z3;
using Microsoft.Cci.UtilityDataStructures;

namespace Z3Wrapper {
  public class Wrapper : ISatSolver {

    public Wrapper(IPlatformType platformType) {
      this.platformType = platformType;
      this.solver = new Context();
    }

    IPlatformType platformType;
    Context solver;
    Hashtable<Sort> typeToSort = new Hashtable<Sort>();
    Hashtable<Expr> variableForName = new Hashtable<Expr>();
    Expr thisArg;

    public ISatSolverContext GetNewContext() {
      return new ContextWrapper(this.solver);
    }

    private Sort GetSortFor(ITypeReference type) {
      var key = type.InternedKey;
      var result = this.typeToSort[key];
      if (result == null) {
        switch (type.TypeCode) {
          case PrimitiveTypeCode.Boolean:
            result = this.solver.BoolSort;
            break;
          case PrimitiveTypeCode.Char:
          case PrimitiveTypeCode.Int16:
          case PrimitiveTypeCode.Int8:
          case PrimitiveTypeCode.UInt16:
          case PrimitiveTypeCode.UInt32:
          case PrimitiveTypeCode.UInt8:
            result = this.GetSortFor(type.PlatformType.SystemInt32);
            break;
          case PrimitiveTypeCode.Int32:
            result = this.solver.MkBitVecSort(32);
            break;
          case PrimitiveTypeCode.UInt64:
            result = this.GetSortFor(type.PlatformType.SystemInt64);
            break;
          case PrimitiveTypeCode.Int64:
            result = this.solver.MkBitVecSort(64);
            break;
          case PrimitiveTypeCode.IntPtr:
          case PrimitiveTypeCode.UIntPtr:
            if (type.PlatformType.PointerSize == 4)
              result = this.GetSortFor(type.PlatformType.SystemInt32);
            else
              result = this.GetSortFor(type.PlatformType.SystemInt64);
            break;
          default:
            result = this.solver.MkUninterpretedSort(TypeHelper.GetTypeName(type));
            break;
        }
        this.typeToSort[key] = result;
      }
      return result;
    }

    private Expr GetVariableFor(INamedEntity variable, ITypeReference expressionType) {
      var sort = this.GetSortFor(expressionType);
      if (variable == null) {
        if (this.thisArg == null)
          this.thisArg = this.solver.MkConst("this", sort);
        return this.thisArg;
      }
      var key = (uint)variable.Name.UniqueKey;
      var result = this.variableForName[key];
      if (result == null) {
        result = this.solver.MkConst(variable.Name.Value, sort);
        this.variableForName[key] = result;
      }
      return result;
    }

    private static bool HasBitVectorSort(ITypeReference type) {
      switch (type.TypeCode) {
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.IntPtr:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UInt8:
        case PrimitiveTypeCode.UIntPtr:
          return true;
        default:
          return false;
      }
    }

    public ISatExpressionWrapper/*?*/ MakeExpression(IOperation operation, ITypeReference expressionType) {
      Expr expr = null;
      if (expressionType.TypeCode == PrimitiveTypeCode.Boolean) {
        switch (operation.OperationCode) {
          case OperationCode.Ldc_I4: expr = this.solver.MkBool(((int)operation.Value) != 0); break;
          case OperationCode.Ldc_I4_0: 
          case OperationCode.Ldc_I4_1: 
          case OperationCode.Ldc_I4_2: 
          case OperationCode.Ldc_I4_3: 
          case OperationCode.Ldc_I4_4: 
          case OperationCode.Ldc_I4_5: 
          case OperationCode.Ldc_I4_6: 
          case OperationCode.Ldc_I4_7: 
          case OperationCode.Ldc_I4_8: 
          case OperationCode.Ldc_I4_M1: expr = this.solver.MkBool(true); break;
          case OperationCode.Ldc_I4_S: expr = this.solver.MkBool(((int)operation.Value) != 0); break;
        }
        if (expr != null)
          return new ExpressionWrapper(expr, expressionType);
      }

      switch (operation.OperationCode) {
        //Instructions that are side effect free and whose results can be cached and reused, but whose result values can never be known at compile time.
        case OperationCode.Arglist:
        case OperationCode.Ldftn:
        case OperationCode.Ldtoken:
        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
        case OperationCode.Ldloca:
        case OperationCode.Ldloca_S:
        case OperationCode.Ldsflda:
          goto default;

        //Instructions that transfer control to a successor block.
        case OperationCode.Br:
        case OperationCode.Br_S:
        case OperationCode.Leave:
        case OperationCode.Leave_S:
          goto default;

        //Instructions that are side-effect free and that result in compile time constant values.
        case OperationCode.Ldc_I4: expr = this.solver.MkBV((int)operation.Value, 32); break;
        case OperationCode.Ldc_I4_0: expr = this.solver.MkBV(0, 32); break;
        case OperationCode.Ldc_I4_1: expr = this.solver.MkBV(1, 32); break;
        case OperationCode.Ldc_I4_2: expr = this.solver.MkBV(2, 32); break;
        case OperationCode.Ldc_I4_3: expr = this.solver.MkBV(3, 32); break;
        case OperationCode.Ldc_I4_4: expr = this.solver.MkBV(4, 32); break;
        case OperationCode.Ldc_I4_5: expr = this.solver.MkBV(5, 32); break;
        case OperationCode.Ldc_I4_6: expr = this.solver.MkBV(6, 32); break;
        case OperationCode.Ldc_I4_7: expr = this.solver.MkBV(7, 32); break;
        case OperationCode.Ldc_I4_8: expr = this.solver.MkBV(8, 32); break;
        case OperationCode.Ldc_I4_M1: expr = this.solver.MkBV(-1, 32); break;
        case OperationCode.Ldc_I4_S: expr = this.solver.MkBV((int)operation.Value, 32); break;
        case OperationCode.Ldc_I8: expr = this.solver.MkBV((long)operation.Value, 64); break;

        case OperationCode.Ldc_R4:
        case OperationCode.Ldc_R8:
        case OperationCode.Ldnull:
        case OperationCode.Ldstr:
          goto default;

        //Instructions that are side-effect free and that *could* result in compile time constant values.
        //We attempt to compute the compile time values.
        case OperationCode.Ldarg:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S:
        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S:
          var v = operation.Value as INamedEntity;
          if (v != null && !(v.Name is Dummy)) {
            expr = this.GetVariableFor(operation.Value as INamedEntity, expressionType);
            break;
          }
          goto default;

        //Instructions that are side-effect free and that *could* result in compile time constant values.
        case OperationCode.Ldsfld:
          goto default;

        case OperationCode.Call:
        case OperationCode.Endfinally:
        case OperationCode.Newobj:
        case OperationCode.Nop:
          goto default;

        default:
          expr = this.solver.MkConst(operation.ToString(), this.GetSortFor(expressionType));
          break;
      }
      return new ExpressionWrapper(expr, expressionType);
    }

    public ISatExpressionWrapper/*?*/ MakeExpression(IOperation operation, ITypeReference expressionType, ISatExpressionWrapper operand1) {
      Expr expr;
      ExpressionWrapper wrapper1 = operand1 as ExpressionWrapper;
      var operationSize = Math.Max(TypeHelper.SizeOfType(expressionType), TypeHelper.SizeOfType(operand1.Type)) <= 4  ? 32u : 64u;
      switch (operation.OperationCode) {
        case OperationCode.Conv_I1:
        case OperationCode.Conv_Ovf_I1:
        case OperationCode.Conv_Ovf_I1_Un:
          expr = this.solver.MkSignExt(24, this.solver.MkExtract(7, 0, this.ConvertToBitVector(operand1, operationSize)));
          break;
        case OperationCode.Conv_I2:
        case OperationCode.Conv_Ovf_I2:
        case OperationCode.Conv_Ovf_I2_Un:
          expr = this.solver.MkSignExt(16, this.solver.MkExtract(16, 0, this.ConvertToBitVector(operand1, operationSize)));
          break;
        case OperationCode.Conv_I4:
        case OperationCode.Conv_Ovf_I4:
        case OperationCode.Conv_Ovf_I4_Un:
          expr = this.solver.MkExtract(32, 0, this.ConvertToBitVector(operand1, operationSize));
          break;
        case OperationCode.Conv_I8:
        case OperationCode.Conv_Ovf_I8:
        case OperationCode.Conv_Ovf_I8_Un:
          if (TypeHelper.SizeOfType(operand1.Type) == 8)
            expr = this.ConvertToBitVector(operand1, 64);
          else
            expr = this.solver.MkSignExt(32, this.ConvertToBitVector(operand1, operationSize));
          break;
        case OperationCode.Conv_R4:
          goto default;
        case OperationCode.Conv_R8:
          goto default;
        case OperationCode.Conv_U1:
        case OperationCode.Conv_Ovf_U1:
        case OperationCode.Conv_Ovf_U1_Un:
          expr = this.solver.MkZeroExt(24, this.solver.MkExtract(7, 0, this.ConvertToBitVector(operand1, operationSize)));
          break;
        case OperationCode.Conv_U2:
        case OperationCode.Conv_Ovf_U2:
        case OperationCode.Conv_Ovf_U2_Un:
          expr = this.solver.MkZeroExt(16, this.solver.MkExtract(16, 0, this.ConvertToBitVector(operand1, operationSize)));
          break;
        case OperationCode.Conv_U4:
        case OperationCode.Conv_Ovf_U4:
        case OperationCode.Conv_Ovf_U4_Un:
          expr = this.solver.MkExtract(32, 0, this.ConvertToBitVector(operand1, operationSize));
          break;
        case OperationCode.Conv_U8:
        case OperationCode.Conv_Ovf_U8:
        case OperationCode.Conv_Ovf_U8_Un:
          if (TypeHelper.SizeOfType(operand1.Type) == 8)
            expr = this.ConvertToBitVector(operand1, 64);
          else
            expr = this.solver.MkZeroExt(32, this.ConvertToBitVector(operand1, 32));
          break;
        case OperationCode.Conv_I:
        case OperationCode.Conv_Ovf_I:
        case OperationCode.Conv_Ovf_I_Un:
          if (expressionType.PlatformType.PointerSize == 8)
            goto case OperationCode.Conv_I8;
          else
            goto case OperationCode.Conv_I4;
        case OperationCode.Conv_U:
        case OperationCode.Conv_Ovf_U:
        case OperationCode.Conv_Ovf_U_Un:
          if (expressionType.PlatformType.PointerSize == 8)
            goto case OperationCode.Conv_U8;
          else
            goto case OperationCode.Conv_U4;
        case OperationCode.Conv_R_Un:
          goto default;

        case OperationCode.Dup:
          return operand1;

        case OperationCode.Neg:
          expr = this.solver.MkBVNeg(this.ConvertToBitVector(operand1, operationSize));
          break;

        case OperationCode.Nop:
          var v = operation.Value as INamedEntity;
          if (v != null && !(v.Name is Dummy)) { //phi node
            expr = this.GetVariableFor(v, expressionType);
            break;
          }
          goto default;

        case OperationCode.Not:
          if (operand1.Type.TypeCode == PrimitiveTypeCode.Boolean)
            expr = this.solver.MkNot(operand1.Unwrap<BoolExpr>());
          else
            expr = this.solver.MkBVNot(this.ConvertToBitVector(operand1, operationSize));
          break;

        default:
          var hashCode = operation.GetHashCode();
          if (operation is Dummy) hashCode = new object().GetHashCode();
          expr = this.solver.MkConst("unary"+hashCode, this.GetSortFor(expressionType));
          break;
      }
      return new ExpressionWrapper(expr, expressionType);
    }

    public ISatExpressionWrapper MakeExpression(IOperation operation, ITypeReference expressionType, ISatExpressionWrapper operand1, ISatExpressionWrapper operand2) {
      Expr expr;
      var operationSize = Math.Max(Math.Max(TypeHelper.SizeOfType(expressionType), TypeHelper.SizeOfType(operand1.Type)), TypeHelper.SizeOfType(operand2.Type)) <= 4  ? 32u : 64u;
      switch (operation.OperationCode) {
        //Instructions that are side-effect free and cacheable and that could result in compile time values.
        //We attempt to compute the compile time values.
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          expr = this.solver.MkBVAdd(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.And:
          if (operand1.Type.TypeCode == PrimitiveTypeCode.Boolean)
            expr = this.solver.MkAnd(operand1.Unwrap<BoolExpr>(), this.ConvertToBool(operand2));
          else
            expr = this.solver.MkBVAND(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Div:
          expr = this.solver.MkBVSDiv(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Div_Un:
          expr = this.solver.MkBVUDiv(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
          expr = this.solver.MkBVMul(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Or:
          if (operand1.Type.TypeCode == PrimitiveTypeCode.Boolean)
            expr = this.solver.MkOr(operand1.Unwrap<BoolExpr>(), this.ConvertToBool(operand2));
          else
            expr = this.solver.MkBVOR(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Rem:
          expr = this.solver.MkBVSRem(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Rem_Un:
          expr = this.solver.MkBVURem(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Shl:
          expr = this.solver.MkBVURem(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Shr:
          expr = this.solver.MkBVASHR(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Shr_Un:
          expr = this.solver.MkBVLSHR(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
          expr = this.solver.MkBVSub(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Xor:
          if (operand1.Type.TypeCode == PrimitiveTypeCode.Boolean)
            expr = this.solver.MkXor(operand1.Unwrap<BoolExpr>(), this.ConvertToBool(operand2));
          else
            expr = this.solver.MkBVXOR(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;

        //Boolean expression
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Ceq:
          expr = this.solver.MkEq(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
          expr = this.solver.MkBVSGE(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          expr = this.solver.MkBVUGE(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Cgt:
          expr = this.solver.MkBVSGT(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Cgt_Un:
          expr = this.solver.MkBVUGT(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
          expr = this.solver.MkBVSLE(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          expr = this.solver.MkBVULE(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Clt:
          expr = this.solver.MkBVSLT(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Clt_Un:
          expr = this.solver.MkBVULT(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize));
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          expr = this.solver.MkNot(this.solver.MkEq(this.ConvertToBitVector(operand1, operationSize), this.ConvertToBitVector(operand2, operationSize)));
          break;

        //Instructions that cause side-effect that we do not currently track.
        case OperationCode.Call:
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Cpblk:
        case OperationCode.Cpobj:
        case OperationCode.Initblk:
        case OperationCode.Newobj:
        case OperationCode.Stfld:
        case OperationCode.Stind_I:
        case OperationCode.Stind_I1:
        case OperationCode.Stind_I2:
        case OperationCode.Stind_I4:
        case OperationCode.Stind_I8:
        case OperationCode.Stind_R4:
        case OperationCode.Stind_R8:
        case OperationCode.Stind_Ref:
        case OperationCode.Stobj:
          goto default;

        //Instructions that are side-effect free and cacheable and that could result in compile time values.
        //We do NOT attempt to compute the compile time values at this time.
        case OperationCode.Ldelem_I:
        case OperationCode.Ldelem_I1:
        case OperationCode.Ldelem_I2:
        case OperationCode.Ldelem_I4:
        case OperationCode.Ldelem_I8:
        case OperationCode.Ldelem_R4:
        case OperationCode.Ldelem_R8:
        case OperationCode.Ldelem_Ref:
        case OperationCode.Ldelem_U1:
        case OperationCode.Ldelem_U2:
        case OperationCode.Ldelem_U4:
        case OperationCode.Ldelema:
          goto default;

        case OperationCode.Nop:
          var v = operation.Value as INamedEntity;
          if (v != null && !(v.Name is Dummy)) { //phi node
            expr = this.GetVariableFor(v, expressionType);
            break;
          }
          goto default;

        default:
          var hashCode = operation.GetHashCode();
          if (operation is Dummy) hashCode = new object().GetHashCode();
          expr = this.solver.MkConst("binary"+hashCode, this.GetSortFor(expressionType));
          break;
      }
      return new ExpressionWrapper(expr, expressionType);
    }

    public ISatExpressionWrapper MakeImplication(ISatExpressionWrapper operand1, ISatExpressionWrapper operand2) {
      return new ExpressionWrapper(this.solver.MkImplies(operand1.Unwrap<BoolExpr>(), operand2.Unwrap<BoolExpr>()), operand1.Type);
    }

    private BoolExpr ConvertToBool(ISatExpressionWrapper expr) {
      if (expr.Type.TypeCode == PrimitiveTypeCode.Boolean) return expr.Unwrap<BoolExpr>();
      if (HasBitVectorSort(expr.Type)) {
        var bv = expr.Unwrap<BitVecExpr>();
        return this.solver.MkNot(this.solver.MkEq(bv, this.solver.MkBV(0, bv.SortSize)));
      }
      return this.solver.MkBoolConst("bool"+expr.GetHashCode().ToString());
    }

    private BitVecExpr ConvertToBitVector(ISatExpressionWrapper expr, uint size) {
      if (HasBitVectorSort(expr.Type)) return expr.Unwrap<BitVecExpr>();
      BitVecExpr result = null;
      if (expr.Type.TypeCode == PrimitiveTypeCode.Boolean)
        result = this.solver.MkITE(expr.Unwrap<BoolExpr>(), this.solver.MkBV(1, 32), this.solver.MkBV(0, 32)) as BitVecExpr; //TODO: this cast might never work
      if (result != null) return result;
      return this.solver.MkInt2BV(size, this.solver.MkIntConst("int"+expr.GetHashCode().ToString()));
    }

    public ISatExpressionWrapper Dummy {
      get { return new ExpressionWrapper(this.solver.MkFalse(), this.platformType.SystemVoid); }
    }

    public ISatExpressionWrapper False {
      get { return new ExpressionWrapper(this.solver.MkFalse(), this.platformType.SystemBoolean); }
    }

    public ISatExpressionWrapper True {
      get { return new ExpressionWrapper(this.solver.MkTrue(), this.platformType.SystemBoolean); }
    }


  }

  internal class ContextWrapper : ISatSolverContext {

    internal ContextWrapper(Context solver) {
      this.solver = solver;
      this.solverContext = solver.MkSolver();
    }

    Context solver;
    Solver solverContext;
    bool isDummy;

    public void Add(ISatExpressionWrapper expression) {
      if (expression.Type.TypeCode != PrimitiveTypeCode.Boolean)
        this.isDummy = true;
      else
        this.solverContext.Assert(expression.Unwrap<BoolExpr>());
    }

    public void AddInverse(ISatExpressionWrapper expression) {
      if (expression.Type.TypeCode != PrimitiveTypeCode.Boolean)
        this.isDummy = true;
      else
        this.solverContext.Assert(this.solver.MkNot(expression.Unwrap<BoolExpr>()));
    }

    public bool? Check() {
      if (this.isDummy) return null;
      switch (this.solverContext.Check()) {
        case Status.SATISFIABLE: return true;
        case Status.UNSATISFIABLE: return false;
      }
      return null;
    }

    public void MakeCheckPoint() {
      this.solverContext.Push();
    }

    public uint NumberOfCheckPoints {
      get {
        return this.solverContext.NumScopes;
      }
    }

    public void RestoreCheckPoint() {
      this.isDummy = false;
      this.solverContext.Pop();
    }

  }

  internal class ExpressionWrapper : ISatExpressionWrapper {

    internal ExpressionWrapper(Expr satExpression, ITypeReference type) {
      this.satExpression = satExpression;
      this.type = type;
    }

    object satExpression;
    ITypeReference type;

    public ITypeReference Type {
      get { return this.type; }
    }

    public T Unwrap<T>() {
      return (T)this.satExpression;
    }
  }
}
