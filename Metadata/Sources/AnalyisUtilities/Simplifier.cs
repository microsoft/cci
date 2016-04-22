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
using Microsoft.Cci;
using Microsoft.Cci.UtilityDataStructures;
using Microsoft.Cci.MutableCodeModel;
using System;

namespace Microsoft.Cci.Analysis {

  /// <summary>
  /// Uses Arithmetic and Boolean laws to simplify expressions.
  /// </summary>
  public static class Simplifier {

    /// <summary>
    /// Uses Arithmetic and Boolean laws to simplify expressions.
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="instruction"></param>
    /// <param name="mappings"></param>
    /// <param name="canonicalizer"></param>
    /// <returns></returns>
    internal static Instruction Simplify<Instruction>(Instruction instruction, ValueMappings<Instruction> mappings, ExpressionCanonicalizer<Instruction> canonicalizer)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(mappings != null);
      Contract.Requires(canonicalizer != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var operand1 = instruction.Operand1 as Instruction;
      if (operand1 == null)
        return SimplifyNullary(instruction, mappings);
      var operand2 = instruction.Operand2 as Instruction;
      if (operand2 != null)
        return SimplifyBinary(instruction, mappings, canonicalizer);
      else if (instruction.Operand2 == null)
        return SimplifyUnary(instruction, mappings, canonicalizer);
      else
        return SimplifyNary(instruction, mappings, canonicalizer);
    }

    private static Instruction SimplifyNary<Instruction>(Instruction instruction, ValueMappings<Instruction> mappings, ExpressionCanonicalizer<Instruction> canonicalizer)
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(mappings != null);
      Contract.Requires(canonicalizer != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      Contract.Assume(instruction.Operand1 is Instruction);
      instruction.Operand1 = Simplify((Instruction)instruction.Operand1, mappings, canonicalizer);
      var operand2 = instruction.Operand2 as Instruction;
      if (operand2 != null) {
        instruction.Operand2 = Simplify(operand2, mappings, canonicalizer);
      } else {
        var operand2andBeyond = instruction.Operand2 as Instruction[];
        Contract.Assume(operand2andBeyond != null);
        for (int i = 0, n = operand2andBeyond.Length; i < n; i++) {
          Contract.Assume(operand2andBeyond[i] != null);
          operand2andBeyond[i] = Simplify(operand2andBeyond[i], mappings, canonicalizer);
        }
      }
      return instruction;
    }

    private static Instruction SimplifyNullary<Instruction>(Instruction instruction, ValueMappings<Instruction> mappings)
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(mappings != null);
      var oldOp = instruction.Operation;
      OperationCode newCode = OperationCode.Invalid;
      switch (oldOp.OperationCode) {
        case OperationCode.Ldarga_S: newCode = OperationCode.Ldarga; break;
        case OperationCode.Ldloca_S: newCode = OperationCode.Ldloca; break;
        case OperationCode.Br_S: newCode = OperationCode.Br; break;
        case OperationCode.Leave_S: newCode = OperationCode.Leave; break;
        case OperationCode.Ldc_I4_0: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_1: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_2: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_3: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_4: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_5: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_6: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_7: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_8: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_M1: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldc_I4_S: newCode = OperationCode.Ldc_I4; break;
        case OperationCode.Ldarg_0: newCode = OperationCode.Ldarg; break;
        case OperationCode.Ldarg_1: newCode = OperationCode.Ldarg; break;
        case OperationCode.Ldarg_2: newCode = OperationCode.Ldarg; break;
        case OperationCode.Ldarg_3: newCode = OperationCode.Ldarg; break;
        case OperationCode.Ldarg_S: newCode = OperationCode.Ldarg; break;
        case OperationCode.Ldloc_0: newCode = OperationCode.Ldloc; break;
        case OperationCode.Ldloc_1: newCode = OperationCode.Ldloc; break;
        case OperationCode.Ldloc_2: newCode = OperationCode.Ldloc; break;
        case OperationCode.Ldloc_3: newCode = OperationCode.Ldloc; break;
        case OperationCode.Ldloc_S: newCode = OperationCode.Ldloc; break;
      }
      switch (newCode) {
        case OperationCode.Ldarg:
        case OperationCode.Ldloc:
          var localOrParameter = oldOp.Value as INamedEntity;
          if (localOrParameter == null) break;
          var definingExpression = mappings.GetDefiningExpressionFor(localOrParameter);
          if (definingExpression != null) return definingExpression;
          break;
      }
      if (newCode == OperationCode.Invalid) return instruction;
      var newOp = new Operation() { OperationCode = newCode, Location = oldOp.Location, Offset = oldOp.Offset, Value = oldOp.Value };
      return new Instruction() { Operation = newOp, Operand1 = instruction.Operand1, Operand2 = instruction.Operand2, Type = instruction.Type };
    }

    /// <summary>
    /// Uses Arithmetic and Boolean laws to simplify expressions.
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="instruction"></param>
    /// <param name="mappings"></param>
    /// <param name="canonicalizer"></param>
    /// <returns></returns>
    internal static Instruction SimplifyBinary<Instruction>(Instruction instruction, ValueMappings<Instruction> mappings, ExpressionCanonicalizer<Instruction> canonicalizer)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(mappings != null);
      Contract.Requires(canonicalizer != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var operation = instruction.Operation;
      Contract.Assume(instruction.Operand1 is Instruction);
      var operand1 = (Instruction)instruction.Operand1;
      Contract.Assume(instruction.Operand2 is Instruction);
      var operand2 = (Instruction)instruction.Operand2;
      IMetadataConstant constantResult = null;
      var compileTimeConstant1 = mappings.GetCompileTimeConstantValueFor(operand1);
      var compileTimeConstant2 = mappings.GetCompileTimeConstantValueFor(operand2);
      if (compileTimeConstant1 != null) {
        if (compileTimeConstant2 != null)
          constantResult = Evaluator.Evaluate(instruction.Operation, compileTimeConstant1, compileTimeConstant2);
        else
          constantResult = Evaluator.Evaluate(instruction.Operation, compileTimeConstant1, operand2, mappings);
      } else if (compileTimeConstant2 != null) {
        constantResult = Evaluator.Evaluate(instruction.Operation, operand1, compileTimeConstant2, mappings);
      } else {
        constantResult = Evaluator.Evaluate(instruction.Operation, operand1, operand2, mappings);
      }
      if (constantResult != null) return canonicalizer.GetAsCanonicalizedLoadConstant(constantResult, instruction);

      //If we get here, the instruction does not simplify to a constant, but it could still simplify to a simpler expression.
      bool operand1IsZero = compileTimeConstant1 != null && MetadataExpressionHelper.IsIntegralZero(compileTimeConstant1);
      bool operand1IsOne = compileTimeConstant1 != null && (operand1IsZero ? false : MetadataExpressionHelper.IsIntegralOne(compileTimeConstant1));
      bool operand1IsMinusOne = compileTimeConstant1 != null && ((operand1IsZero || operand1IsOne) ? false : MetadataExpressionHelper.IsIntegralMinusOne(compileTimeConstant1));
      bool operand2IsZero = compileTimeConstant2 != null && MetadataExpressionHelper.IsIntegralZero(compileTimeConstant2);
      bool operand2IsOne = compileTimeConstant2 != null && (operand1IsZero ? false : MetadataExpressionHelper.IsIntegralOne(compileTimeConstant2));
      bool operand2IsMinusOne = compileTimeConstant2 != null && ((operand2IsZero || operand2IsOne) ? false : MetadataExpressionHelper.IsIntegralMinusOne(compileTimeConstant2));
      operand1 = Simplify(operand1, mappings, canonicalizer);
      operand2 = Simplify(operand2, mappings, canonicalizer);

      switch (operation.OperationCode) {
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          if (operand1IsZero) return operand2;
          if (operand2IsZero) return operand1;
          //TODO: factor out common mults/divs/etc (subject to overflow checks).
          break;
        case OperationCode.And:
          if (operand1IsZero) return operand1;
          if (operand2IsZero) return operand2;
          if (operand1IsMinusOne) return operand2;
          if (operand2IsMinusOne) return operand1;
          if (operand1.Operation.OperationCode == OperationCode.Not && operand2.Operation.OperationCode == OperationCode.Not) {
            var opnd11 = operand1.Operand1 as Instruction;
            var opnd21 = operand2.Operand1 as Instruction;
            Contract.Assume(opnd11 != null && opnd21 != null);
            var or = new Operation() { OperationCode = OperationCode.Or, Location = operation.Location, Offset = operation.Offset };
            var orInst = new Instruction() { Operation = or, Operand1 = opnd11, Operand2 = opnd21, Type = instruction.Type };
            var not = new Operation { OperationCode = OperationCode.Not, Location = operation.Location, Offset = operation.Offset };
            return new Instruction() { Operation = not, Operand1 = orInst, Type = instruction.Type };
          }
          break;
        case OperationCode.Ceq:
          //If one of the operands is const 0 and the other is a boolean expression, invert the boolean expression
          if (operand2IsZero && operand1.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            var not = new Operation() { Location = instruction.Operation.Location, Offset = instruction.Operation.Offset, OperationCode = OperationCode.Not };
            instruction = new Instruction() { Operation = not, Operand1 = operand1, Type = instruction.Type };
            return SimplifyUnary(instruction, mappings, canonicalizer);
          } else if (operand1IsZero && operand2.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            var not = new Operation() { Location = instruction.Operation.Location, Offset = instruction.Operation.Offset, OperationCode = OperationCode.Not };
            instruction = new Instruction() { Operation = not, Operand1 = operand2, Type = instruction.Type };
            return SimplifyUnary(instruction, mappings, canonicalizer);
          } else {
            operation = new Operation() { Location = instruction.Operation.Location, Offset = instruction.Operation.Offset, OperationCode = OperationCode.Beq };
          }
          break;
        case OperationCode.Cgt:
          operation = new Operation() { Location = instruction.Operation.Location, Offset = instruction.Operation.Offset, OperationCode = OperationCode.Bgt };
          break;
        case OperationCode.Cgt_Un:
          operation = new Operation() { Location = instruction.Operation.Location, Offset = instruction.Operation.Offset, OperationCode = OperationCode.Bgt_Un };
          break;
        case OperationCode.Clt:
          operation = new Operation() { Location = instruction.Operation.Location, Offset = instruction.Operation.Offset, OperationCode = OperationCode.Blt };
          break;
        case OperationCode.Clt_Un:
          operation = new Operation() { Location = instruction.Operation.Location, Offset = instruction.Operation.Offset, OperationCode = OperationCode.Blt_Un };
          break;
        case OperationCode.Div:
        case OperationCode.Div_Un:
          if (operand2IsOne) return operand1;
          break;
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
          if (operand1IsOne) return operand2;
          if (operand2IsOne) return operand1;
          break;
        case OperationCode.Or:
          if (operand1IsZero) return operand2;
          if (operand2IsZero) return operand1;
          if (operand1.Operation.OperationCode == OperationCode.Not && operand2.Operation.OperationCode == OperationCode.Not) {
            var opnd11 = operand1.Operand1 as Instruction;
            var opnd21 = operand2.Operand1 as Instruction;
            Contract.Assume(opnd11 != null && opnd21 != null);
            var and = new Operation() { OperationCode = OperationCode.And, Location = operation.Location, Offset = operation.Offset };
            var andInst = new Instruction() { Operation = and, Operand1 = opnd11, Operand2 = opnd21, Type = instruction.Type };
            var not = new Operation { OperationCode = OperationCode.Not, Location = operation.Location, Offset = operation.Offset };
            return new Instruction() { Operation = not, Operand1 = andInst, Type = instruction.Type };
          }
          if (operand1.Operand1 == operand2.Operand1 && operand1.Operand2 == operand2.Operand2 && 
            operand1.Operation.OperationCode != operand2.Operation.OperationCode && operand2.Operand1 != null &&
            operand1.Operation.OperationCode == GetInverse(operand2.Operation.OperationCode,
              operand2.Operand1.Type.TypeCode == PrimitiveTypeCode.Float32 || operand2.Operand1.Type.TypeCode == PrimitiveTypeCode.Float64)) {
            return canonicalizer.GetAsCanonicalizedLoadConstant(new MetadataConstant() { Value = true, Type = instruction.Type }, instruction);
          }
          break;
        case OperationCode.Rem:
        case OperationCode.Rem_Un:
          break;
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          if (operand2IsZero) return operand1;
          break;
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
          if (operand2IsZero) return operand1;
          break;
        case OperationCode.Xor:
          break;
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          if (operand1IsZero && operand2.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            var operand2inv = TryToGetSimplerLogicalInverse(operand2);
            if (operand2inv != null) return operand2inv;
          } else if (operand2IsZero && operand1.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            var operand1inv = TryToGetSimplerLogicalInverse(operand1);
            if (operand1inv != null) return operand1inv;
          }
          goto case OperationCode.Bge_S;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          if (operand1IsZero && operand2.Type.TypeCode == PrimitiveTypeCode.Boolean) return operand2;
          if (operand2IsZero && operand1.Type.TypeCode == PrimitiveTypeCode.Boolean) return operand1;
          goto case OperationCode.Bge_S;
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un_S:
          operation = new Operation() {
            Location = operation.Location, Offset = operation.Offset,
            OperationCode = LongVersionOf(operation.OperationCode), Value = operation.Value
          };
          break;
      }
      if (operation != instruction.Operation || operand1 != instruction.Operand1 || operand2 != instruction.Operand2)
        return new Instruction() { Operation = operation, Operand1 = operand1, Operand2 = operand2, Type = instruction.Type };
      return instruction;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="instruction"></param>
    /// <returns></returns>
    public static Instruction HoistPhiNodes<Instruction>(Instruction instruction)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

      var operand1 = instruction.Operand1 as Instruction;
      if (operand1 == null) return instruction;
      var operand2 = instruction.Operand2 as Instruction;
      if (operand2 == null) return instruction; //TODO: unary ops

      bool operand1IsPhiNode = operand1.Operation.OperationCode == OperationCode.Nop && operand1.Operation.Value is INamedEntity;
      bool operand2IsPhiNode = operand2.Operation.OperationCode == OperationCode.Nop && operand2.Operation.Value is INamedEntity;
      if (operand1IsPhiNode) {
        if (operand2IsPhiNode)
          return HoistPhiPhi(instruction, operand1, operand2);
        else
          return HoistPhiOp(instruction, operand1, operand2);
      } else {
        if (operand2IsPhiNode)
          return HoistOpPhi(instruction, operand1, operand2);
      }
      return instruction;
    }

    private static Instruction HoistPhiPhi<Instruction>(Instruction instruction, Instruction operand1, Instruction operand2)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var result = new Instruction() { Operation = new Operation() { Value = Dummy.LocalVariable }, Type = instruction.Type };
      var operand11 = operand1.Operand1 as Instruction;
      var operand21 = operand2.Operand1 as Instruction;
      if (operand11 == null) {
        Contract.Assume(operand21 == null);
        return result;
      }
      result.Operand1 = new Instruction() { Operation = instruction.Operation, Operand1 = operand11, Operand2 = operand21, Type = instruction.Type };
      var operand12 = operand1.Operand2 as Instruction;
      var operand22 = operand2.Operand2 as Instruction;
      if (operand12 != null && operand22 != null) {
        result.Operand2 = new Instruction() { Operation = instruction.Operation, Operand1 = operand12, Operand2 = operand22, Type = instruction.Type };
      } else {
        var operand12toN = operand1.Operand2 as Instruction[];
        var operand22toN = operand2.Operand2 as Instruction[];
        if (operand12toN != null && operand22toN != null) {
          var n = operand12toN.Length;
          Contract.Assume(n == operand22toN.Length);
          var resultOperands2ToN = new Instruction[n];
          result.Operand2 = resultOperands2ToN;
          for (int i = 0; i < n; i++) {
            var operand1i = operand12toN[i];
            var operand2i = operand22toN[i];
            resultOperands2ToN[i] = new Instruction() { Operation = instruction.Operation, Operand1 = operand1i, Operand2 = operand2i, Type = instruction.Type };
          }
        }
      }
      return result;
    }

    private static Instruction HoistPhiOp<Instruction>(Instruction instruction, Instruction operand1, Instruction operand2)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var result = new Instruction() { Operation = new Operation() { Value = Dummy.LocalVariable }, Type = instruction.Type };
      var operand11 = operand1.Operand1 as Instruction;
      if (operand11 == null) {
        Contract.Assume(false);
        return result;
      }
      result.Operand1 = new Instruction() { Operation = instruction.Operation, Operand1 = operand11, Operand2 = operand2, Type = instruction.Type };
      var operand12 = operand1.Operand2 as Instruction;
      if (operand12 != null) {
        result.Operand2 = new Instruction() { Operation = instruction.Operation, Operand1 = operand12, Operand2 = operand2, Type = instruction.Type };
      } else {
        var operand12toN = operand1.Operand2 as Instruction[];
        if (operand12toN != null) {
          var n = operand12toN.Length;
          var resultOperands2ToN = new Instruction[n];
          result.Operand2 = resultOperands2ToN;
          for (int i = 0; i < n; i++) {
            var operand1i = operand12toN[i];
            resultOperands2ToN[i] = new Instruction() { Operation = instruction.Operation, Operand1 = operand1i, Operand2 = operand2, Type = instruction.Type };
          }
        }
      }
      return result;
    }

    private static Instruction HoistOpPhi<Instruction>(Instruction instruction, Instruction operand1, Instruction operand2)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var result = new Instruction() { Operation = new Operation() { Value = Dummy.LocalVariable }, Type = instruction.Type };
      var operand21 = operand2.Operand1 as Instruction;
      if (operand21 == null) {
        Contract.Assume(false);
        return result;
      }
      result.Operand1 = new Instruction() { Operation = instruction.Operation, Operand1 = operand1, Operand2 = operand21, Type = instruction.Type };
      var operand22 = operand2.Operand2 as Instruction;
      if (operand22 != null) {
        result.Operand2 = new Instruction() { Operation = instruction.Operation, Operand1 = operand1, Operand2 = operand22, Type = instruction.Type };
      } else {
        var operand22toN = operand2.Operand2 as Instruction[];
        if (operand22toN != null) {
          var n = operand22toN.Length;
          var resultOperands2ToN = new Instruction[n];
          result.Operand2 = resultOperands2ToN;
          for (int i = 0; i < n; i++) {
            var operand2i = operand22toN[i];
            resultOperands2ToN[i] = new Instruction() { Operation = instruction.Operation, Operand1 = operand1, Operand2 = operand2i, Type = instruction.Type };
          }
        }
      }
      return result;
    }

    /// <summary>
    /// If the given operation code is a short branch, return the corresponding long branch. Otherwise return the given operation code.
    /// </summary>
    /// <param name="operationCode">An operation code.</param>
    public static OperationCode LongVersionOf(OperationCode operationCode) {
      switch (operationCode) {
        case OperationCode.Beq_S: return OperationCode.Beq;
        case OperationCode.Bge_S: return OperationCode.Bge;
        case OperationCode.Bge_Un_S: return OperationCode.Bge_Un;
        case OperationCode.Bgt_S: return OperationCode.Bgt;
        case OperationCode.Bgt_Un_S: return OperationCode.Bgt_Un;
        case OperationCode.Ble_S: return OperationCode.Ble;
        case OperationCode.Ble_Un_S: return OperationCode.Ble_Un;
        case OperationCode.Blt_S: return OperationCode.Blt;
        case OperationCode.Blt_Un_S: return OperationCode.Blt_Un;
        case OperationCode.Bne_Un_S: return OperationCode.Bne_Un;
        case OperationCode.Br_S: return OperationCode.Br;
        case OperationCode.Brfalse_S: return OperationCode.Brfalse;
        case OperationCode.Brtrue_S: return OperationCode.Brtrue;
        case OperationCode.Leave_S: return OperationCode.Leave;
        default: return operationCode;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="instruction"></param>
    /// <param name="mappings"></param>
    /// <param name="canonicalizer"></param>
    /// <returns></returns>
    internal static Instruction SimplifyUnary<Instruction>(Instruction instruction, ValueMappings<Instruction> mappings, ExpressionCanonicalizer<Instruction> canonicalizer)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(mappings != null);
      Contract.Requires(canonicalizer != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var operation = instruction.Operation;
      Contract.Assume(instruction.Operand1 is Instruction);
      var operand1 = (Instruction)instruction.Operand1;
      var operand = Simplify(operand1, mappings, canonicalizer);
      var compileTimeConstant = mappings.GetCompileTimeConstantValueFor(operand);
      if (compileTimeConstant != null) {
        var constantResult = Evaluator.Evaluate(instruction.Operation, compileTimeConstant);
        if (constantResult != null) return canonicalizer.GetAsCanonicalizedLoadConstant(constantResult, instruction);
      }

      switch (operation.OperationCode) {
        case OperationCode.Neg:
          if (operand.Operation.OperationCode == OperationCode.Neg) {
            Contract.Assume(operand.Operand1 is Instruction);
            return (Instruction)operand.Operand1;
          }
          //TODO: if the operand is a binary operation with arithmetic operands where one of them is a Neg
          //distribute the neg over the binary operation, if doing so is safe w.r.t. overflow.
          break;
        case OperationCode.Not:
          var simplerInverse = TryToGetSimplerLogicalInverse(operand);
          if (simplerInverse != null) return simplerInverse;
          if (operand != operand1) {
            var operation1 = operand1.Operation;
            switch (operation1.OperationCode) {
              case OperationCode.Bne_Un:
              case OperationCode.Bne_Un_S:
              case OperationCode.Beq:
              case OperationCode.Beq_S:
                OperationCode newOpcode = GetInverse(operation1.OperationCode, operand1.Type.TypeCode == PrimitiveTypeCode.Float32 || operand1.Type.TypeCode == PrimitiveTypeCode.Float64);
                return new Instruction() {
                  Operation = new Operation() { OperationCode = newOpcode, Offset = operation.Offset, Location = operation.Location },
                  Operand1 = operand1.Operand1,
                  Operand2 = operand1.Operand2,
                  Type = instruction.Type
                };
            }
          }
          return new Instruction() { Operation = operation, Operand1 = operand, Type = instruction.Type };
      }
      return instruction;
    }

    private static Instruction/*?*/ TryToGetSimplerLogicalInverse<Instruction>(Instruction instruction)
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Not:
          Contract.Assume(instruction.Operand1 is Instruction);
          return (Instruction)instruction.Operand1;
        case OperationCode.And: {
            var opnd1 = instruction.Operand1 as Instruction;
            var opnd2 = instruction.Operand2 as Instruction;
            Contract.Assume(opnd1 != null && opnd2 != null);
            var opnd1inv = TryToGetSimplerLogicalInverse(opnd1);
            var opnd2inv = TryToGetSimplerLogicalInverse(opnd2);
            if (opnd1inv == null) {
              if (opnd2inv == null) return null;
              var not = new Operation() { OperationCode = OperationCode.Not, Location = opnd1.Operation.Location, Offset = opnd1.Operation.Offset };
              opnd1inv = new Instruction() { Operation = not, Operand1 = opnd1, Type = instruction.Type };
            } else if (opnd2inv == null) {
              var not = new Operation() { OperationCode = OperationCode.Not, Location = opnd2.Operation.Location, Offset = opnd2.Operation.Offset };
              opnd2inv = new Instruction() { Operation = not, Operand1 = opnd2, Type = instruction.Type };
            }
            var or = new Operation() { OperationCode = OperationCode.Or, Location = instruction.Operation.Location, Offset = instruction.Operation.Offset };
            return new Instruction() { Operation = or, Operand1 = opnd1inv, Operand2 = opnd2inv, Type = instruction.Type };
          }
        case OperationCode.Or: {
            var opnd1 = instruction.Operand1 as Instruction;
            var opnd2 = instruction.Operand2 as Instruction;
            Contract.Assume(opnd1 != null && opnd2 != null);
            var opnd1inv = TryToGetSimplerLogicalInverse(opnd1);
            var opnd2inv = TryToGetSimplerLogicalInverse(opnd2);
            if (opnd1inv == null) {
              if (opnd2inv == null) return null;
              var not = new Operation() { OperationCode = OperationCode.Not, Location = opnd1.Operation.Location, Offset = opnd1.Operation.Offset };
              opnd1inv = new Instruction() { Operation = not, Operand1 = opnd1, Type = instruction.Type };
            } else if (opnd2inv == null) {
              var not = new Operation() { OperationCode = OperationCode.Not, Location = opnd2.Operation.Location, Offset = opnd2.Operation.Offset };
              opnd2inv = new Instruction() { Operation = not, Operand1 = opnd2, Type = instruction.Type };
            }
            var and = new Operation() { OperationCode = OperationCode.And, Location = instruction.Operation.Location, Offset = instruction.Operation.Offset };
            return new Instruction() { Operation = and, Operand1 = opnd1inv, Operand2 = opnd2inv, Type = instruction.Type };
          }
        case OperationCode.Beq:
        case OperationCode.Bge:
        case OperationCode.Bge_Un:
        case OperationCode.Bgt:
        case OperationCode.Bgt_Un:
        case OperationCode.Ble:
        case OperationCode.Ble_Un:
        case OperationCode.Blt:
        case OperationCode.Blt_Un:
        case OperationCode.Bne_Un:
        case OperationCode.Ceq:
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
        case OperationCode.Clt:
        case OperationCode.Clt_Un: {
            var opnd1 = instruction.Operand1 as Instruction;
            var opnd2 = instruction.Operand2 as Instruction;
            Contract.Assume(opnd1 != null && opnd2 != null);
            OperationCode newOpcode = GetInverse(instruction.Operation.OperationCode, opnd1.Type.TypeCode == PrimitiveTypeCode.Float32 || opnd1.Type.TypeCode == PrimitiveTypeCode.Float64);
            if (newOpcode != instruction.Operation.OperationCode) {
              var cmp = new Operation() { OperationCode = newOpcode, Location = instruction.Operation.Location, Offset = instruction.Operation.Offset };
              return new Instruction() { Operation = cmp, Operand1 = opnd1, Operand2 = opnd2, Type = instruction.Type };
            }
            break;
          }
      }
      return null;
    }

    private static OperationCode GetInverse(OperationCode operationCode, bool isFloatingPointOperation) {
      if (isFloatingPointOperation) {
        switch (operationCode) {
          case OperationCode.Beq: return OperationCode.Bne_Un;
          case OperationCode.Bge: return OperationCode.Blt_Un;
          case OperationCode.Bge_Un: return OperationCode.Blt;
          case OperationCode.Bgt: return OperationCode.Ble_Un;
          case OperationCode.Bgt_Un: return OperationCode.Ble;
          case OperationCode.Ble: return OperationCode.Bgt_Un;
          case OperationCode.Ble_Un: return OperationCode.Bgt;
          case OperationCode.Blt: return OperationCode.Bge_Un;
          case OperationCode.Blt_Un: return OperationCode.Bge;
          case OperationCode.Bne_Un: return OperationCode.Beq;
          case OperationCode.Ceq: return OperationCode.Bne_Un;
          case OperationCode.Cgt: return OperationCode.Ble_Un;
          case OperationCode.Cgt_Un: return OperationCode.Ble;
          case OperationCode.Clt: return OperationCode.Bge_Un;
          case OperationCode.Clt_Un: return OperationCode.Bge;
        }
      } else {
        switch (operationCode) {
          case OperationCode.Beq: return OperationCode.Bne_Un;
          case OperationCode.Bge: return OperationCode.Blt;
          case OperationCode.Bge_Un: return OperationCode.Blt_Un;
          case OperationCode.Bgt: return OperationCode.Ble;
          case OperationCode.Bgt_Un: return OperationCode.Ble_Un;
          case OperationCode.Ble: return OperationCode.Bgt;
          case OperationCode.Ble_Un: return OperationCode.Bgt_Un;
          case OperationCode.Blt: return OperationCode.Bge;
          case OperationCode.Blt_Un: return OperationCode.Bge_Un;
          case OperationCode.Bne_Un: return OperationCode.Beq;
          case OperationCode.Ceq: return OperationCode.Bne_Un;
          case OperationCode.Cgt: return OperationCode.Ble;
          case OperationCode.Cgt_Un: return OperationCode.Ble;
          case OperationCode.Clt: return OperationCode.Bge;
          case OperationCode.Clt_Un: return OperationCode.Bge_Un;
        }
      }
      return operationCode;
    }

  }
}