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
  /// Rewrites Boolean expressions to exclude references to variables that have been updated.
  /// </summary>
  public static class Purger {

    /// <summary>
    /// Rewrites Boolean expressions to exclude references to variables that have been updated.
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="instruction"></param>
    /// <param name="variable"></param>
    /// <param name="canonicalizer"></param>
    /// <returns></returns>
    internal static Instruction/*?*/ Purge<Instruction>(Instruction instruction, INamedEntity variable, ExpressionCanonicalizer<Instruction> canonicalizer)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(variable != null);
      Contract.Requires(canonicalizer != null);

      var operand1 = instruction.Operand1 as Instruction;
      if (operand1 == null)
        return PurgeNullary(instruction, variable);
      var operand2 = instruction.Operand2 as Instruction;
      if (operand2 != null)
        return PurgeBinary(instruction, variable, canonicalizer);
      else if (instruction.Operand2 == null)
        return PurgeUnary(instruction, variable, canonicalizer);
      else
        return PurgeNary(instruction, variable, canonicalizer);
    }

    private static Instruction/*?*/ PurgeNary<Instruction>(Instruction instruction, INamedEntity variable, ExpressionCanonicalizer<Instruction> canonicalizer)
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(variable != null);
      Contract.Requires(canonicalizer != null);

      Contract.Assume(instruction.Operand1 is Instruction);
      var operand1 = Purge((Instruction)instruction.Operand1, variable, canonicalizer);
      if (operand1 == null) return null;
      var operand2andBeyond = instruction.Operand2 as Instruction[];
      Contract.Assume(operand2andBeyond != null);
      var n = operand2andBeyond.Length;
      Instruction[] copy = null;
      for (int i = 0; i < n; i++) {
        Contract.Assume(operand2andBeyond[i] != null);
        var opi = Purge(operand2andBeyond[i], variable, canonicalizer);
        if (opi == null) return null;
        if (opi != operand2andBeyond[i]) {
          if (copy == null) {
            copy = new Instruction[n];
            for (int j = 0; j < i; j++) copy[j] = operand2andBeyond[j];
          }
          Contract.Assume(copy.Length == n);
          copy[i] = opi;
        }
      }
      if (operand1 != instruction.Operand1 || copy != null)
        return canonicalizer.GetCanonicalExpression(instruction, operand1, null, copy);
      return instruction;
    }

    private static Instruction/*?*/ PurgeNullary<Instruction>(Instruction instruction, INamedEntity variable)
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(variable != null);

      if (instruction.Operation.Value == variable) return null;
      return instruction;
    }

    /// <summary>
    /// Uses Arithmetic and Boolean laws to simplify expressions.
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="instruction"></param>
    /// <param name="variable"></param>
    /// <param name="canonicalizer"></param>
    /// <returns></returns>
    internal static Instruction/*?*/ PurgeBinary<Instruction>(Instruction instruction, INamedEntity variable, ExpressionCanonicalizer<Instruction> canonicalizer)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(variable != null);
      Contract.Requires(canonicalizer != null);

      var operation = instruction.Operation;
      Contract.Assume(instruction.Operand1 is Instruction);
      var operand1 = (Instruction)instruction.Operand1;
      Contract.Assume(instruction.Operand2 is Instruction);
      var operand2 = (Instruction)instruction.Operand2;
      operand1 = Purge(operand1, variable, canonicalizer);
      operand2 = Purge(operand2, variable, canonicalizer);

      switch (operation.OperationCode) {
        case OperationCode.And:
        case OperationCode.Or:
          if (operand1 == null) return operand2;
          if (operand2 == null) return operand1;
          if (operand1 != instruction.Operand1 || operand2 != instruction.Operand2)
            return canonicalizer.GetCanonicalExpression(instruction, operand1, operand2);
          break;
      }
      if (operand1 != instruction.Operand1 || operand2 != instruction.Operand2) return null;
      return instruction;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="Instruction"></typeparam>
    /// <param name="instruction"></param>
    /// <param name="variable"></param>
    /// <param name="canonicalizer"></param>
    /// <returns></returns>
    internal static Instruction/*?*/ PurgeUnary<Instruction>(Instruction instruction, INamedEntity variable, ExpressionCanonicalizer<Instruction> canonicalizer)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(instruction != null);
      Contract.Requires(variable != null);
      Contract.Requires(canonicalizer != null);

      var operation = instruction.Operation;
      Contract.Assume(instruction.Operand1 is Instruction);
      var operand1 = (Instruction)instruction.Operand1;
      var operand = Purge(operand1, variable, canonicalizer);
      if (operand != operand1) return null;
      return instruction;
    }

  }
}