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

  internal class ExpressionCanonicalizer<Instruction>
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    internal ExpressionCanonicalizer(ValueMappings<Instruction> mappings) {
      Contract.Requires(mappings != null);
      this.mappings = mappings;
    }

    ValueMappings<Instruction> mappings;
    Dictionary<Instruction, Instruction> cache = new Dictionary<Instruction, Instruction>(new ExpressionComparer());
    Instruction dummyInstruction = new Instruction();

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.mappings != null);
      Contract.Invariant(this.cache != null);
      Contract.Invariant(this.dummyInstruction != null);
    }

    internal class ExpressionComparer : IEqualityComparer<Instruction> {

      [ContractVerification(false)]
      public bool Equals(Instruction x, Instruction y) {
        if (x == null) return y == null;
        if (y == null) return false;
        if (object.ReferenceEquals(x, y)) return true;
        if (x.Operation.OperationCode != y.Operation.OperationCode) return false;
        if (x.Operation.Value == null) {
          if (y.Operation.Value != null) return false;
        } else {
          if (!x.Operation.Value.Equals(y.Operation.Value)) return false;
        }
        if (x.Operand1 == null) return y.Operand1 == null;
        bool result = this.Equals((Instruction)x.Operand1, (Instruction)y.Operand1);
        if (x.Operand2 == null) return result && y.Operand2 == null;
        var operand2x = x.Operand2 as Instruction;
        var operand2y = y.Operand2 as Instruction;
        if (operand2x != null) {
          if (operand2y == null) return false;
          if (this.Equals(operand2x, operand2y)) return result;
          if (result) return false;
          if (OperationIsCummutative(x.Operation.OperationCode)) {
            return (this.Equals((Instruction)x.Operand1, operand2y) && this.Equals((Instruction)y.Operand1, operand2x));
          }
          return false;
        }
        if (!result) return false;
        var operandsx = x.Operand2 as Instruction[];
        var operandsy = y.Operand2 as Instruction[];
        Contract.Assume(operandsx != null);
        var n = operandsx.Length;
        if (operandsy == null || operandsy.Length != n) return false;
        for (int i = 0; i < n; i++) {
          var opx = operandsx[i];
          var opy = operandsy[i];
          if (!this.Equals(opx, opy)) return false;
        }
        return true;
      }

      private static bool OperationIsCummutative(OperationCode operationCode) {
        switch (operationCode) {
          case OperationCode.Add:
          case OperationCode.Add_Ovf:
          case OperationCode.Add_Ovf_Un:
          case OperationCode.And:
          case OperationCode.Beq:
          case OperationCode.Beq_S:
          case OperationCode.Bne_Un:
          case OperationCode.Bne_Un_S:
          case OperationCode.Ceq:
          case OperationCode.Mul:
          case OperationCode.Mul_Ovf:
          case OperationCode.Mul_Ovf_Un:
          case OperationCode.Or:
          case OperationCode.Xor:
            return true;
        }
        return false;
      }

      [ContractVerification(false)]
      public int GetHashCode(Instruction instruction) {
        if (instruction == null) return 0;
        int result = (int)instruction.Operation.OperationCode;
        if (instruction.Operation.Value != null)
          result = (result << 2) ^ instruction.Operation.Value.GetHashCode();
        if (instruction.Operand1 == null) return result;
        var hash1 = this.GetHashCode((Instruction)instruction.Operand1);
        if (instruction.Operand2 == null) return (result << 2) ^ hash1;
        var operand2 = instruction.Operand2 as Instruction;
        if (operand2 != null) {
          var hash2 = this.GetHashCode(operand2);
          return (result << 3) ^ (hash1 ^ hash2);
        }
        var operands = instruction.Operand2 as Instruction[];
        Contract.Assume(operands != null);
        for (int i = 0, n = operands.Length; i < n; i++) {
          result = (result << 2) ^ this.GetHashCode(operands[i]);
        }
        return result;
      }

    }

    /// <summary>
    /// Returns the canonical form of an expression that results in the given constant at runtime. 
    /// </summary>
    /// <param name="compileTimeConstant">The compile time constant that should be equal to the value the resulting expression will evaluate to at runtime.</param>
    /// <param name="originalInstruction">An instruction that will result in the given constant at runtime. The result of this method is a canonical form of this instruction.</param>
    internal Instruction GetAsCanonicalizedLoadConstant(IMetadataConstant compileTimeConstant, Instruction originalInstruction) {
      Contract.Requires(compileTimeConstant != null);
      Contract.Requires(originalInstruction != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var ic = compileTimeConstant.Value as IConvertible;
      if (ic != null)
        return this.GetAsLoadConstant(ic, originalInstruction);
      else if (compileTimeConstant.Value == null)
        return new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Ldnull, Location = originalInstruction.Operation.Location }, Type = compileTimeConstant.Type };
      else
        return originalInstruction;
    }

    /// <summary>
    /// Returns the canonical form of an expression that results in the given constant at runtime. 
    /// </summary>
    /// <param name="convertible">The value that the resulting expression must evaluate to at runtime.</param>
    /// <param name="originalInstruction">An instruction that will result in the given constant at runtime. The result of this method is a canonical form of this instruction.</param>
    private Instruction GetAsLoadConstant(IConvertible convertible, Instruction originalInstruction) {
      Contract.Requires(convertible != null);
      Contract.Requires(originalInstruction != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      Instruction result = originalInstruction;
      var operation = originalInstruction.Operation;
      var location = originalInstruction.Operation.Location;
      TypeCode tc = convertible.GetTypeCode();
      switch (tc) {
        case TypeCode.Boolean:
        case TypeCode.SByte:
        case TypeCode.Byte:
        case TypeCode.Char:
        case TypeCode.Int16:
        case TypeCode.UInt16:
        case TypeCode.Int32:
        case TypeCode.UInt32:
        case TypeCode.Int64:
          long n = convertible.ToInt64(null);
          if (int.MinValue <= n && n <= int.MaxValue)
            result = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Ldc_I4, Value = (int)n, Location = location } };
          else
            result = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Ldc_I8, Value = n, Location = location } };
          break;

        case TypeCode.UInt64:
          result = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Ldc_I8, Value = (long)convertible.ToUInt64(null), Location = location } };
          break;

        case TypeCode.Single:
          result = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Ldc_R4, Value = convertible.ToSingle(null), Location = location } };
          break;

        case TypeCode.Double:
          result = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Ldc_R8, Value = convertible.ToDouble(null), Location = location } };
          break;

        case TypeCode.String:
          result = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Ldstr, Value = convertible.ToString(null), Location = location } };
          break;
      }
      result.Type = originalInstruction.Type;
      return this.GetCanonicalExpression(result);
    }

    /// <summary>
    /// If an expression equivalent to the given expression can be found in the cache, the result is that expression.
    /// Otherwise the result is the given expression and it is added to the cache as well.
    /// </summary>
    /// <param name="expression">An instruction that computes a value.</param>
    internal Instruction GetCanonicalExpression(Instruction expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      Instruction result;
      if (!this.cache.TryGetValue(expression, out result)) {
        this.cache[expression] = result = expression;
      }
      Contract.Assume(result != null);
      return result;
    }

    /// <summary>
    /// If the cache contains an expression with an Operation structurally equivalent to unaryInstruction.Operation and Operand1 structurally equivalent to
    /// operand1, then return that expression. Otherwise construct such an expression, add it to the cache and return it.
    /// </summary>
    /// <param name="unaryInstruction">An instruction with a single operand.</param>
    /// <param name="operand1">The already canonicalized version of unaryInstruction.Operand1, if available, otherwise unaryInstruction.Operand1.</param>
    internal Instruction GetCanonicalExpression(Instruction unaryInstruction, Instruction operand1) {
      Contract.Requires(unaryInstruction != null);
      Contract.Requires(operand1 != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var expression = this.dummyInstruction;
      expression.Operation = unaryInstruction.Operation;
      expression.Operand1 = operand1;
      expression.Operand2 = null;
      expression.Type = unaryInstruction.Type;

      Instruction result;
      if (!this.cache.TryGetValue(expression, out result)) {
        result = this.GetCanonicalExpression(Simplifier.SimplifyUnary(expression, this.mappings, this));
        this.cache[expression] = result;
        this.dummyInstruction = new Instruction();
      }
      Contract.Assume(result != null);
      return result;
    }

    /// <summary>
    /// If the cache contains an expression with an Operation structurally equivalent to binaryInstruction.Operation, Operand1 structurally equivalent to
    /// operand1 and Operand2 structurally equivalent to the operand2, then return that expression. 
    /// Otherwise construct such an expression, simplify it and then add it to the cache and return it.
    /// </summary>
    /// <param name="binaryInstruction">An instruction with a two operands.</param>
    /// <param name="operand1">The already canonicalized version of binaryInstruction.Operand1, if available, otherwise binaryInstruction.Operand1.</param>
    /// <param name="operand2">The already canonicalized version of binaryInstruction.Operand2, if available, otherwise binaryInstruction.Operand2.</param>
    internal Instruction GetCanonicalExpression(Instruction binaryInstruction, Instruction operand1, Instruction operand2) {
      Contract.Requires(binaryInstruction != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var expression = this.dummyInstruction;
      expression.Operation = binaryInstruction.Operation;
      expression.Operand1 = operand1;
      expression.Operand2 = operand2;
      expression.Type = binaryInstruction.Type;

      Instruction result;
      if (!this.cache.TryGetValue(expression, out result)) {
        result = this.GetCanonicalExpression(Simplifier.SimplifyBinary(expression, this.mappings, this));
        this.cache[expression] = result;
        this.dummyInstruction = new Instruction();
      }
      Contract.Assume(result != null);
      return result;
    }

    internal Instruction GetCanonicalExpression(Instruction naryInstruction, Instruction operand1, Instruction/*?*/ operand2, Instruction[]/*?*/ operands2andBeyond) {
      Contract.Requires(naryInstruction != null);
      Contract.Requires(operand1 != null);

      var expression = this.dummyInstruction;
      expression.Operation = naryInstruction.Operation;
      expression.Operand1 = this.GetCanonicalExpression(Simplifier.Simplify(operand1, this.mappings, this));
      if (operand2 != null)
        expression.Operand2 = this.GetCanonicalExpression(Simplifier.Simplify(operand2, this.mappings, this));
      else if (operands2andBeyond != null) {
        var n = operands2andBeyond.Length;
        var canonOps = new Instruction[n];
        for (int i = 0; i < n; i++) {
          Contract.Assume(operands2andBeyond[i] != null);
          canonOps[i] = this.GetCanonicalExpression(Simplifier.Simplify(operands2andBeyond[i], this.mappings, this));
        }
        expression.Operand2 = canonOps;
      }
      expression.Type = naryInstruction.Type;

      Instruction result;
      if (!this.cache.TryGetValue(expression, out result)) {
        this.cache[expression] = result = expression;
        this.dummyInstruction = new Instruction();
      }
      Contract.Assume(result != null);
      return result;
    }

  }


}