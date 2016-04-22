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
  /// An inclusive numeric interval.
  /// </summary>
  public sealed class Interval {

    /// <summary>
    /// An inclusive numeric interval from minus infinity to plus infinity.
    /// </summary>
    public Interval() {
      this.lowerBound = Dummy.Constant;
      this.upperBound = Dummy.Constant;
    }

    /// <summary>
    /// An inclusive numeric interval. If the bounds are floating point numbers, they are finite. The bounds must contain values of the same type.
    /// </summary>
    /// <param name="lowerBound">The inclusive lower bound of the interval. If this value is null or Dummy.Constant, it means the lower bound is unknown.</param>
    /// <param name="upperBound">The inclusive upper bound of the interval. If this value is null or Dummy.Constant, it means the lower bound is unknown.</param>
    /// <param name="excludesMinusOne">If true, an expression that results in this interval at compile time can never result in -1 at runtime. If false,
    /// the value of this.ExcludesMinusOne will be determined by the given lower and upper bounds.</param>
    /// <param name="excludesZero">If true, an expression that results in this interval at compile time can never result in zero at runtime. If false,
    /// the value of this.ExcludesZero will be determined by the given lower and upper bounds.</param>
    /// <param name="includesDivisionByZero">If true, an expression that results in this interval at compile time may result in division by zero at runtime.</param>
    /// <param name="includesOverflow">If true, an expression that results in this integer interval at compile time may result in an overflow value at runtime.</param>
    /// <param name="includesUnderflow">If true, an expression that results in this integer interval at compile time may result in an underflow value at runtime.</param>
    public Interval(IMetadataConstant/*?*/ lowerBound, IMetadataConstant/*?*/ upperBound, bool excludesMinusOne = false, bool excludesZero = false,
      bool includesDivisionByZero = false, bool includesOverflow = false, bool includesUnderflow = false) {
      this.lowerBound = lowerBound??Dummy.Constant;
      this.upperBound = upperBound??Dummy.Constant;
      this.includesDivisionByZero = includesDivisionByZero;
      this.includesOverflow = includesOverflow;
      this.includesUnderflow = includesUnderflow;
      this.excludesMinusOne = excludesMinusOne || !Evaluator.IsNegative(this.LowerBound) || Evaluator.IsSmallerThanMinusOne(this.UpperBound);
      this.excludesZero = excludesZero || Evaluator.IsPositive(this.LowerBound) || Evaluator.IsNegative(this.UpperBound);
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.lowerBound != null);
      Contract.Invariant(this.upperBound != null);
    }

    /// <summary>
    /// True if -1 is not a member of this interval.
    /// </summary>
    public bool ExcludesMinusOne {
      get { return this.excludesMinusOne; }
    }
    bool excludesMinusOne;

    /// <summary>
    /// True if 0 is not a member of this interval.
    /// </summary>
    public bool ExcludesZero {
      get { return this.excludesZero; }
    }
    bool excludesZero;

    /// <summary>
    /// If true, an expression that results in this interval at compile time may result in division by zero at runtime.
    /// </summary>
    public bool IncludesDivisionByZero {
      get { return this.includesDivisionByZero; }
    }
    bool includesDivisionByZero;

    /// <summary>
    /// If true, an expression that results in this integer interval at compile time may result in an overflow value at runtime.
    /// If expression does not check for overflow then then lower and upper bounds of the 
    /// interval will be the minimum and maximum values of the appropriate integer type, respectively.
    /// </summary>
    public bool IncludesOverflow {
      get { return this.includesOverflow; }
    }
    bool includesOverflow;

    /// <summary>
    /// If true, an expression that results in this integer interval at compile time may result in an underflow value at runtime.
    /// If expression does not check for overflow then then lower and upper bounds of the 
    /// interval will be the minimum and maximum values of the appropriate integer type, respectively.
    /// </summary>
    public bool IncludesUnderflow {
      get { return this.includesUnderflow; }
    }
    bool includesUnderflow;

    /// <summary>
    /// True if both the lower and upper bounds of the interval are known and finite and if the interval does not include division by zero or overflow values.
    /// </summary>
    public bool IsFinite {
      get {
        return !(this.IncludesDivisionByZero || this.IncludesOverflow || this.IncludesUnderflow);
      }
    }

    /// <summary>
    /// True if the expression that results in this interval is known at compile time to always result in a runtime exception.
    /// </summary>
    public bool IsUnusable {
      get {
        return Evaluator.IsNumericallyGreaterThan(this.LowerBound, this.UpperBound);
      }
    }

    /// <summary>
    /// The inclusive lower bound of the interval.
    /// </summary>
    public IMetadataConstant LowerBound {
      get {
        Contract.Ensures(Contract.Result<IMetadataConstant>() != null);
        return this.lowerBound;
      }
    }
    private IMetadataConstant lowerBound;

    /// <summary>
    /// The inclusive upper bound of the interval.
    /// </summary>
    public IMetadataConstant UpperBound {
      get {
        Contract.Ensures(Contract.Result<IMetadataConstant>() != null);
        return this.upperBound;
      }
    }
    private IMetadataConstant upperBound;

    /// <summary>
    /// Returns a shallow copy of this Interval.
    /// </summary>
    /// <returns></returns>
    public Interval Clone() {
      return new Interval(this.LowerBound, this.UpperBound, excludesMinusOne: this.ExcludesMinusOne, excludesZero: this.ExcludesZero, includesDivisionByZero: this.IncludesDivisionByZero,
        includesOverflow: this.IncludesOverflow, includesUnderflow: this.IncludesUnderflow);
    }

    /// <summary>
    /// True if the lower bound of this integer interval is greater than the minimum value of the given type.
    /// </summary>
    private bool ExcludesMinimumValue(ITypeReference type) {
      Contract.Requires(type != null);
      return Evaluator.IsNumericallyGreaterThan(this.LowerBound, Evaluator.GetMinValue(type));
    }

    /// <summary>
    /// If the interval includes a single number return that number.
    /// </summary>
    public IMetadataConstant/*?*/ GetAsSingleton() {
      if (this.IsFinite && Evaluator.IsNumericallyEqual(this.LowerBound, this.UpperBound))
        return this.lowerBound;
      return null;
    }

    /// <summary>
    /// Returns the smallest interval that contains the intervals obtained for each of the variables whose values are joined
    /// together by the given join. If no such interval exists, the result is null.
    /// </summary>
    private static Interval/*?*/ GetIntervalFor<Instruction>(Join join, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(join != null);
      Contract.Requires(mappings != null);

      Contract.Assume(join.Join1 != null);
      var interval = GetIntervalFor(join.Join1, join.Block1, mappings);
      if (interval == null) return null;

      if (join.Join2 != null) {
        var interval2 = GetIntervalFor(join.Join2, join.Block2, mappings);
        if (interval2 == null) return null;
        interval = interval.Join(interval2);

        if (join.OtherJoins != null) {
          Contract.Assume(join.OtherBlocks != null && join.OtherBlocks.Count == join.OtherJoins.Count);
          for (int i = 0, n = join.OtherJoins.Count; i < n; i++) {
            Contract.Assume(join.OtherJoins[i] != null);
            Contract.Assert(join.OtherBlocks.Count == n);
            Contract.Assume(join.OtherBlocks[i] != null);
            var intervalI = GetIntervalFor(join.OtherJoins[i], join.OtherBlocks[i], mappings);
            if (intervalI == null) return null;
            interval = interval.Join(intervalI);
          }
        }
      }
      return interval;
    }

    /// <summary>
    /// Returns the smallest interval that contains all possible runtime values the given variable may take on in while control is inside the given block
    /// </summary>
    private static Interval/*?*/ GetIntervalFor<Instruction>(INamedEntity variable, object block, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(variable != null);
      Contract.Requires(mappings != null);

      var cv = mappings.GetCompileTimeConstantValueFor(variable);
      if (cv != null) return new Interval(cv, cv);
      var definingExpression = mappings.GetDefiningExpressionFor(variable);
      if (definingExpression == null) return null;
      Contract.Assume(block is AiBasicBlock<Instruction>);
      var interval = mappings.GetIntervalFor(definingExpression, (AiBasicBlock<Instruction>)block);
      return interval;
    }

    /// <summary>
    /// Returns an interval with includes this interval as well as the other interval.
    /// </summary>
    public Interval/*?*/ Join(Interval/*?*/ other) {
      Contract.Ensures(other == null || Contract.Result<Interval>() != null);
      if (other == null) return this;
      if (this.IsUnusable) return other;
      if (other.IsUnusable) return this;
      return new Interval(
        Evaluator.Min(this.LowerBound, other.LowerBound),
        Evaluator.Max(this.UpperBound, other.UpperBound),
        excludesMinusOne: this.ExcludesMinusOne && other.ExcludesMinusOne,
        excludesZero: this.ExcludesZero && other.ExcludesZero,
        includesDivisionByZero: this.IncludesDivisionByZero || other.IncludesDivisionByZero,
        includesOverflow: this.IncludesOverflow || other.IncludesOverflow,
        includesUnderflow: this.IncludesUnderflow || other.IncludesUnderflow);
    }

    /// <summary>
    /// Narrows the given interval which is associated with the given expression, by applying the constraints that are known to hold inside the referring block.
    /// If the joinBlock is not null, the interval is first narrowed with any constraints that apply to the transition from definingBlock to joinBlock.
    /// </summary>
    public static Interval Narrow<Instruction>(Instruction expression, Interval interval, AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock,
      AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(interval != null);
      Contract.Requires(expression != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);
      Contract.Ensures(Contract.Result<Interval>() != null);

      if (joinBlock != null) {
        var predecessors = joinBlock.Predecessors;
        Contract.Assume(predecessors != null);
        var n = predecessors.Count;
        Contract.Assume(n == joinBlock.ConstraintsAtEntry.Count);
        for (int i = 0; i < n; i++) {
          if (predecessors[i] == definingBlock) {
            if (joinBlock.ConstraintsAtEntry[i] == null) continue;
            interval = Narrow(expression, interval.Clone(), joinBlock.ConstraintsAtEntry[i], referringBlock, mappings);
            break;
          }
        }
        if (joinBlock == referringBlock) return interval;
      }

      Interval union = null;
      foreach (var constraintList in referringBlock.ConstraintsAtEntry) {
        if (constraintList == null) continue;
        var narrowedInterval = Narrow(expression, interval.Clone(), constraintList, referringBlock, mappings);
        if (union == null)
          union = narrowedInterval;
        else
          union = union.Join(narrowedInterval);
      }
      return union??interval;
    }

    /// <summary>
    /// Narrows the given interval, which is associated with the given expression, by applying the given list of constraints that are known to hold inside the referring block
    /// whenever it referes to the given expression.
    /// </summary>
    private static Interval Narrow<Instruction>(Instruction expression, Interval interval, List<Instruction> constraintList, AiBasicBlock<Instruction> referringBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(interval != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(constraintList != null);
      Contract.Requires(mappings != null);
      Contract.Ensures(Contract.Result<Interval>() != null);

      foreach (var constraint in constraintList) {
        if (constraint == null) continue;
        Narrow(expression, interval, constraint, referringBlock, mappings);
      }
      return interval;
    }

    /// <summary>
    /// Narrows the given interval, which is associated with the given expression, by applying the given constraint that is known to hold inside the referring block
    /// whenever it refers to the given expression.
    /// </summary>
    private static void Narrow<Instruction>(Instruction expression, Interval interval, Instruction constraint, AiBasicBlock<Instruction> block, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(interval != null);
      Contract.Requires(block != null);
      Contract.Requires(constraint != null);
      Contract.Requires(mappings != null);

      var operand1 = constraint.Operand1 as Instruction;
      if (operand1 == null) return;
      var operand2 = constraint.Operand2 as Instruction;
      if (operand2 == null) return;
      if (operand1 == expression) {
        var cv = mappings.GetCompileTimeConstantValueFor(operand2);
        if (cv != null) {
          Narrow(interval, constraint.Operation.OperationCode, cv);
        }
      } else if (operand2 == expression) {
        var cv = mappings.GetCompileTimeConstantValueFor(operand1);
        if (cv != null) {
          Narrow2(cv, constraint.Operation.OperationCode, interval);
        }
      } else if (constraint.Operation.OperationCode == OperationCode.And) {
        Narrow(expression, interval, operand1, block, mappings);
        Narrow(expression, interval, operand2, block, mappings);
      } else if (constraint.Operation.OperationCode == OperationCode.Or) {
        var interval1 = interval.Clone();
        var interval2 = interval.Clone();
        Narrow(expression, interval1, operand1, block, mappings);
        Narrow(expression, interval2, operand2, block, mappings);
        var interval1join2 = interval1.Join(interval2);
        Narrow(interval, OperationCode.Beq, interval1join2);
      }
    }

    /// <summary>
    /// Narrows the given interval, which is associated with the given local or parameter, by applying the constraints that are known to hold inside the referring block.
    /// If the joinBlock is not null, the interval is first narrowed with any constraints that apply to the transition from definingBlock to joinBlock.
    /// </summary>
    public static Interval Narrow<Instruction>(INamedEntity localOrParameter, Interval interval, AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock,
      AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(localOrParameter != null);
      Contract.Requires(interval != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);
      Contract.Ensures(Contract.Result<Interval>() != null);

      if (joinBlock != null) {
        var predecessors = joinBlock.Predecessors;
        Contract.Assume(predecessors != null);
        var n = predecessors.Count;
        Contract.Assume(n == joinBlock.ConstraintsAtEntry.Count);
        for (int i = 0; i < n; i++) {
          if (predecessors[i] == definingBlock) {
            if (joinBlock.ConstraintsAtEntry[i] == null) continue;
            interval = Narrow(localOrParameter, interval.Clone(), joinBlock.ConstraintsAtEntry[i], referringBlock, mappings);
            break;
          }
        }
        if (joinBlock == referringBlock) return interval;
      }

      Interval union = null;
      foreach (var constraintList in referringBlock.ConstraintsAtEntry) {
        if (constraintList == null) continue;
        var narrowedInterval = Narrow(localOrParameter, interval.Clone(), constraintList, referringBlock, mappings);
        if (union == null)
          union = narrowedInterval;
        else
          union = union.Join(narrowedInterval);
      }
      return union??interval;
    }

    /// <summary>
    /// Narrows the given interval, which is associated with the given local or parameter, by applying the given list of constraints that are known to hold inside the referring block
    /// whenever it referes to the given local or parameter.
    /// </summary>
    private static Interval Narrow<Instruction>(INamedEntity localOrParameter, Interval interval, List<Instruction> constraintList, AiBasicBlock<Instruction> referringBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(localOrParameter != null);
      Contract.Requires(interval != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(constraintList != null);
      Contract.Requires(mappings != null);
      Contract.Ensures(Contract.Result<Interval>() != null);

      foreach (var constraint in constraintList) {
        if (constraint == null) continue;
        Narrow(localOrParameter, interval, constraint, referringBlock, mappings);
      }
      return interval;
    }

    /// <summary>
    /// Narrows the given interval, which is associated with the given local or parameter, by applying the given constraint that is known to hold inside the referring block
    /// whenever it referes to the given local or parameter.
    /// </summary>
    private static void Narrow<Instruction>(INamedEntity localOrParameter, Interval interval, Instruction constraint, AiBasicBlock<Instruction> block, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(localOrParameter != null);
      Contract.Requires(interval != null);
      Contract.Requires(block != null);
      Contract.Requires(constraint != null);
      Contract.Requires(mappings != null);

      var operand1 = constraint.Operand1 as Instruction;
      if (operand1 == null) return;
      var operand2 = constraint.Operand2 as Instruction;
      if (operand2 == null) return;
      if (operand1.Operation.Value == localOrParameter) {
        if (operand1.Operation.OperationCode != OperationCode.Ldarg && operand1.Operation.OperationCode != OperationCode.Ldloc) return;
        var cv = mappings.GetCompileTimeConstantValueFor(operand2);
        if (cv != null) {
          Narrow(interval, constraint.Operation.OperationCode, cv);
        }
      } else if (operand2.Operation.Value == localOrParameter) {
        if (operand2.Operation.OperationCode != OperationCode.Ldarg && operand2.Operation.OperationCode != OperationCode.Ldloc) return;
        var cv = mappings.GetCompileTimeConstantValueFor(operand1);
        if (cv != null) {
          Narrow2(cv, constraint.Operation.OperationCode, interval);
        }
      }
      if (constraint.Operation.OperationCode == OperationCode.And) {
        Narrow(localOrParameter, interval, operand1, block, mappings);
        Narrow(localOrParameter, interval, operand2, block, mappings);
      } else if (constraint.Operation.OperationCode == OperationCode.Or) {
        var interval1 = interval.Clone();
        var interval2 = interval.Clone();
        Narrow(localOrParameter, interval1, operand1, block, mappings);
        Narrow(localOrParameter, interval2, operand2, block, mappings);
        var interval1join2 = interval1.Join(interval2);
        Narrow(interval, OperationCode.Beq, interval1join2);
      }
    }

    /// <summary>
    /// Narrows the given interval by applying a constraint expressed by the given operation code and compile time constant.
    /// Think of this as enforcing the constraint that x op cv must be true for any value x in the given interval.
    /// For example, if the operation code is Bge and the constant is 10 then the lower bound of the interval is set to 10, if
    /// this will result in a narrower interval. The change is peformed by mutating the given interval.
    /// </summary>
    private static void Narrow(Interval interval, OperationCode operationCode, IMetadataConstant cv) {
      Contract.Requires(interval != null);
      Contract.Requires(cv != null);

      if (!MetadataExpressionHelper.IsFiniteNumeric(cv)) return;
      switch (operationCode) {
        //interval == cv
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          interval.lowerBound = cv;
          interval.upperBound = cv;
          interval.excludesMinusOne |= !MetadataExpressionHelper.IsIntegralMinusOne(cv);
          interval.excludesZero |= !MetadataExpressionHelper.IsIntegralZero(cv);
          break;

        //interval >= cv
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          if (interval.LowerBound == Dummy.Constant || Evaluator.IsNumericallyGreaterThan(cv, interval.LowerBound)) {
            interval.lowerBound = cv;
            interval.includesUnderflow = false;
          }
          interval.excludesMinusOne |= !Evaluator.IsNegative(cv);
          interval.excludesZero |= Evaluator.IsPositive(cv);
          break;

        //interval > cv
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          if (interval.LowerBound == Dummy.Constant || Evaluator.IsNumericallyGreaterThan(cv, interval.LowerBound)) {
            interval.lowerBound = Evaluator.IncreaseBySmallestInterval(cv);
            interval.includesUnderflow = false;
          }
          interval.excludesMinusOne |= !Evaluator.IsNegative(cv);
          interval.excludesZero |= !Evaluator.IsNegative(cv);
          break;

        //interval <= cv
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          if (interval.UpperBound == Dummy.Constant || Evaluator.IsNumericallyLessThan(cv, interval.UpperBound)) {
            interval.upperBound = cv;
            interval.includesOverflow = false;
          }
          interval.excludesMinusOne |= Evaluator.IsNegative(cv) && !MetadataExpressionHelper.IsIntegralMinusOne(cv);
          interval.excludesZero |= Evaluator.IsNegative(cv);
          break;

        //interval < cv
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          if (interval.UpperBound == Dummy.Constant || Evaluator.IsNumericallyLessThan(cv, interval.UpperBound)) {
            interval.upperBound = Evaluator.DecreaseBySmallestInterval(cv);
            interval.includesOverflow = false;
          }
          interval.excludesMinusOne |= !Evaluator.IsPositive(cv) && !MetadataExpressionHelper.IsIntegralMinusOne(cv);
          interval.excludesZero |= !Evaluator.IsPositive(cv);
          break;

        //interval != cv
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          if (Evaluator.IsNumericallyEqual(interval.LowerBound, cv))
            interval.lowerBound = Evaluator.IncreaseBySmallestInterval(cv);
          if (Evaluator.IsNumericallyEqual(interval.UpperBound, cv))
            interval.upperBound = Evaluator.DecreaseBySmallestInterval(cv);
          if (MetadataExpressionHelper.IsIntegralMinusOne(cv))
            interval.excludesMinusOne = true;
          if (MetadataExpressionHelper.IsIntegralZero(cv))
            interval.excludesZero = true;
          break;
      }
    }

    /// <summary>
    /// Narrows interval1 by applying a constraint expressed by the given operation code and another interval, interval2.
    /// Think of this as enforcing the constraint that x op y must be true for any value x in interval1 and any value y in interval2.
    /// For example, if the operation code is Bge and the other interval is 10..20 then the lower bound of the interval is set to 20, if
    /// this will result in a narrower interval. The change is peformed by mutating the given interval.
    /// </summary>
    private static void Narrow(Interval interval1, OperationCode operationCode, Interval interval2) {
      Contract.Requires(interval1 != null);
      Contract.Requires(interval2 != null);

      switch (operationCode) {
        //interval == interval2
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          if (interval1.LowerBound == Dummy.Constant || Evaluator.IsNumericallyGreaterThan(interval2.LowerBound, interval1.LowerBound)) {
            interval1.lowerBound = interval2.LowerBound;
            interval1.includesUnderflow = false;
          }
          if (interval1.UpperBound == Dummy.Constant || Evaluator.IsNumericallyLessThan(interval2.UpperBound, interval1.UpperBound)) {
            interval1.upperBound = interval2.UpperBound;
            interval1.includesOverflow = false;
          }
          interval1.excludesMinusOne = interval2.ExcludesMinusOne;
          interval1.excludesZero |= interval2.ExcludesZero;
          break;

        //interval >= interval2
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          if (interval1.LowerBound == Dummy.Constant || Evaluator.IsNumericallyGreaterThan(interval2.UpperBound, interval1.LowerBound)) {
            interval1.lowerBound = interval2.UpperBound;
            interval1.includesUnderflow = false;
          }
          break;

        //interval > interval2
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          if (interval1.LowerBound == Dummy.Constant || Evaluator.IsNumericallyGreaterThan(interval2.UpperBound, interval1.LowerBound)) {
            interval1.lowerBound = Evaluator.IncreaseBySmallestInterval(interval2.UpperBound);
            interval1.includesUnderflow = false;
          }
          break;

        //interval <= interval2
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          if (interval1.UpperBound == Dummy.Constant || Evaluator.IsNumericallyLessThan(interval2.LowerBound, interval1.UpperBound)) {
            interval1.upperBound = interval2.LowerBound;
            interval1.includesOverflow = false;
          }
          break;

        //interval < interval2
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          if (interval1.UpperBound == Dummy.Constant || Evaluator.IsNumericallyLessThan(interval2.LowerBound, interval1.UpperBound)) {
            interval1.upperBound = Evaluator.DecreaseBySmallestInterval(interval2.LowerBound);
            interval1.includesOverflow = false;
          }
          break;

      }
    }

    /// <summary>
    /// Narrows the given interval by applying a constraint expressed by the given operation code and compile time constant.
    /// Think of this as enforcing the constraint that cv op x must be true for any value x in the given interval.
    /// For example, if the operation code is Bge and the constant is 10 then the upper bound of the interval is set to 10, if
    /// this will result in a narrower interval. The change is peformed by mutating the given interval.
    /// </summary>
    private static void Narrow2(IMetadataConstant cv, OperationCode operationCode, Interval interval) {
      Contract.Requires(interval != null);
      Contract.Requires(cv != null);

      switch (operationCode) {
        //cv == interval
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          interval.lowerBound = cv;
          interval.upperBound = cv;
          interval.excludesMinusOne |= !MetadataExpressionHelper.IsIntegralMinusOne(cv);
          interval.excludesZero |= !MetadataExpressionHelper.IsIntegralZero(cv);
          break;

        // cv >= interval
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          if (interval.UpperBound == Dummy.Constant || Evaluator.IsNumericallyLessThan(cv, interval.UpperBound)) {
            interval.upperBound = cv;
            interval.includesOverflow = false;
          }
          interval.excludesMinusOne |= Evaluator.IsNegative(cv) && !MetadataExpressionHelper.IsIntegralMinusOne(cv);
          interval.excludesZero |= Evaluator.IsNegative(cv);
          break;

        //cv > interval
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          if (interval.UpperBound == Dummy.Constant || Evaluator.IsNumericallyLessThan(cv, interval.UpperBound)) {
            interval.upperBound = Evaluator.DecreaseBySmallestInterval(cv);
            interval.includesOverflow = false;
          }
          interval.excludesMinusOne |= Evaluator.IsNegative(cv);
          interval.excludesZero |= !Evaluator.IsPositive(cv);
          break;

        //cv <= interval
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          if (interval.LowerBound == Dummy.Constant || Evaluator.IsNumericallyGreaterThan(cv, interval.LowerBound)) {
            interval.lowerBound = cv;
            interval.includesUnderflow = false;
          }
          interval.excludesMinusOne |= !Evaluator.IsNegative(cv);
          interval.excludesZero |= Evaluator.IsPositive(cv);
          break;

        //cv < interval
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          if (interval.LowerBound == Dummy.Constant || Evaluator.IsNumericallyGreaterThan(cv, interval.LowerBound)) {
            interval.lowerBound = Evaluator.IncreaseBySmallestInterval(cv);
            interval.includesUnderflow = false;
          }
          interval.excludesMinusOne |= !Evaluator.IsNegative(cv) || MetadataExpressionHelper.IsIntegralMinusOne(cv);
          interval.excludesZero |= !Evaluator.IsNegative(cv);
          break;

        // cv != interval
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          if (Evaluator.IsNumericallyEqual(interval.LowerBound, cv))
            interval.lowerBound = Evaluator.IncreaseBySmallestInterval(cv);
          if (Evaluator.IsNumericallyEqual(interval.UpperBound, cv))
            interval.upperBound = Evaluator.DecreaseBySmallestInterval(cv);
          if (MetadataExpressionHelper.IsIntegralMinusOne(cv))
            interval.excludesMinusOne = true;
          if (MetadataExpressionHelper.IsIntegralZero(cv))
            interval.excludesZero = true;
          break;
      }
    }

    /// <summary>
    /// Computes a numeric interval to bound the value that the given expression results in. 
    /// If the expression cannot be bounded by a numeric interval, the result is null.
    /// </summary>
    /// <param name="expression">An instruction that results in a value.</param>
    /// <param name="referringBlock">A block providing the context for the interval. The entry contraints of the block are used to narrow the interval if possible.</param>
    /// <param name="joinBlock">If the expression is a component of a join ("phi" node), then joinBlock is the block in which the join is defined.</param>
    /// <param name="definingBlock">If the expression is a component of a join ("phi" node), then definingBlock is the block from which the component expression flowed to joinBlock.</param>
    /// <param name="mappings">Provides several maps from expressions to concrete and abstract values.</param>
    /// <returns>
    /// An interval that bounds the values of the expression to a value between upper and lower bounds.
    /// If the expression cannot be bounded, for instance because it does not result int a numeric type, the result is null.
    /// </returns>
    /// <remarks>
    /// This method does not add its result to the cache, nor does it check the cache for existing result.
    /// The reason for this is that while the abstract interpretation proceeds, intervals can become more refined.
    /// Once abstract interpretation is done, clients will obtain intervals via mappings, not this class.
    /// If there is a cache miss, mappings will call this class and do the caching.
    /// </remarks>
    [ContractVerification(false)]
    internal static Interval/*?*/ TryToGetAsInterval<Instruction>(Instruction expression, AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock,
      AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);

      Interval result = null;
      var singleton = mappings.GetCompileTimeConstantValueFor(expression);
      if (singleton != null && TypeHelper.IsPrimitiveInteger(singleton.Type))
        return new Interval(singleton, singleton);
      else {
        if (expression.Operation.OperationCode == OperationCode.Nop && expression.Operation.Value is INamedEntity)
          result = TryToGetAsJoinedInterval(expression, referringBlock, joinBlock, definingBlock, mappings);
        else {
          var operand1 = expression.Operand1 as Instruction;
          if (operand1 == null)
            result = TryToGetAsInterval0(expression, referringBlock, joinBlock, definingBlock, mappings);
          else {
            if (expression.Operand2 == null)
              result = TryToGetAsInterval1(expression, operand1, referringBlock, joinBlock, definingBlock, mappings);
            else {
              var operand2 = expression.Operand2 as Instruction;
              if (operand2 != null)
                result = TryToGetAsInterval2(expression, operand1, operand2, referringBlock, joinBlock, definingBlock, mappings);
              else {
                Contract.Assume(expression.Operand2 is Instruction[]);
                result = TryToGetAsIntervalN(expression, operand1, (Instruction[])expression.Operand2, mappings);
              }
            }
          }
        }
      }
      if (result != null) {
        return Narrow(expression, result, referringBlock, joinBlock, definingBlock, mappings);
      }
      return result;
    }

    /// <summary>
    /// Computes a numeric interval to bound the value that the given nullary expression results in. 
    /// If the expression cannot be bounded by a numeric interval, the result is null.
    /// </summary>
    /// <param name="expression">An instruction that results in a value.</param>
    /// <param name="referringBlock">A block providing the context for the interval. The entry contraints of the block are used to narrow the interval if possible.</param>
    /// <param name="joinBlock">If the expression is a component of a join ("phi" node), then joinBlock is the block in which the join is defined.</param>
    /// <param name="definingBlock">If the expression is a component of a join ("phi" node), then definingBlock is the block from which the component expression flowed to joinBlock.</param>
    /// <param name="mappings">Provides several maps from expressions to concrete and abstract values.</param>
    /// <returns>
    /// An interval that bounds the values of the expression to a value between upper and lower bounds.
    /// If the expression cannot be bounded, for instance because it does not result int a numeric type, the result is null.
    /// </returns>
    private static Interval/*?*/ TryToGetAsInterval0<Instruction>(Instruction expression, AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock,
      AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);

      var typeInterval = GetIntervalFor(expression.Type, mappings);
      if (typeInterval == null) return null;
      var operation = expression.Operation;
      switch (operation.OperationCode) {
        //Instructions that are side effect free and whose results can be cached and reused, but whose result values can never be known at compile time.
        case OperationCode.Arglist:
        case OperationCode.Call:
        case OperationCode.Ldftn:
        case OperationCode.Ldtoken:
        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
        case OperationCode.Ldloca:
        case OperationCode.Ldloca_S:
        case OperationCode.Ldsflda:
          return null;

        //Instructions that transfer control to a successor block.
        case OperationCode.Br:
        case OperationCode.Br_S:
        case OperationCode.Leave:
        case OperationCode.Leave_S:
          return null;

        //Instructions that are side-effect free and that result in compile time constant values.
        case OperationCode.Ldc_I4:
        case OperationCode.Ldc_I4_0:
        case OperationCode.Ldc_I4_1:
        case OperationCode.Ldc_I4_2:
        case OperationCode.Ldc_I4_3:
        case OperationCode.Ldc_I4_4:
        case OperationCode.Ldc_I4_5:
        case OperationCode.Ldc_I4_6:
        case OperationCode.Ldc_I4_7:
        case OperationCode.Ldc_I4_8:
        case OperationCode.Ldc_I4_M1:
        case OperationCode.Ldc_I4_S:
        case OperationCode.Ldc_I8:
          return null; //Don't bother with an interval, clients are not expect to ask for an interval unless they know the expression is not a compile time constant.
        case OperationCode.Ldc_R4:
        case OperationCode.Ldc_R8:
        case OperationCode.Ldnull:
        case OperationCode.Ldstr:
          return null; //Don't bother with an interval, clients are not expect to ask for an interval unless they know the expression is not a compile time constant.

        //Instructions that are side-effect free and cacheable and that *could* result in compile time constant values.
        //We attempt to compute the compile time values.
        case OperationCode.Ldarg:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S:
          var arg = operation.Value as IParameterDefinition;
          if (arg == null) return null;
          var definingJoin = mappings.GetDefiningJoinFor(arg);
          if (definingJoin != null) {
            var joinedInterval = GetIntervalFor(definingJoin, mappings);
            return Narrow(arg, joinedInterval??typeInterval, referringBlock, joinBlock, definingBlock, mappings);
          }
          var definingExpression = mappings.GetDefiningExpressionFor(arg);
          if (definingExpression != null && !Evaluator.Contains(definingExpression, expression))
            return Narrow(arg, TryToGetAsInterval(definingExpression, referringBlock, joinBlock, definingBlock, mappings)??typeInterval, referringBlock, joinBlock, definingBlock, mappings);
          else
            return Narrow(arg, typeInterval, referringBlock, joinBlock, definingBlock, mappings);

        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S:
          var local = operation.Value as ILocalDefinition;
          Contract.Assume(local != null);
          definingJoin = mappings.GetDefiningJoinFor(local);
          if (definingJoin != null) {
            var joinedInterval = GetIntervalFor(definingJoin, mappings);
            return Narrow(local, joinedInterval??typeInterval, referringBlock, joinBlock, definingBlock, mappings);
          }
          definingExpression = mappings.GetDefiningExpressionFor(local);
          if (definingExpression != null && !Evaluator.Contains(definingExpression, expression))
            return Narrow(local, TryToGetAsInterval(definingExpression, referringBlock, joinBlock, definingBlock, mappings)??typeInterval, referringBlock, joinBlock, definingBlock, mappings);
          else
            return Narrow(local, typeInterval, referringBlock, joinBlock, definingBlock, mappings);

        //Instructions that are side-effect free and that *could* result in compile time constant values.
        //We do NOT attempt to compute the compile time values at this time.
        case OperationCode.Ldsfld:
          return typeInterval;

        //Instructions that transfer control out of the method being interpreted.
        case OperationCode.Jmp:
        case OperationCode.Rethrow:
        case OperationCode.Ret:
          return null;

        //Instruction modifier to track in the future.
        case OperationCode.Volatile_:
          return null;

        default:
          Contract.Assume(false);
          return null;
      }
    }

    /// <summary>
    /// Computes a numeric interval to bound the value that the given unary expression results in. 
    /// If the expression cannot be bounded by a numeric interval, the result is null.
    /// </summary>
    /// <param name="expression">An instruction that results in a value.</param>
    /// <param name="operand"></param>
    /// <param name="referringBlock">A block providing the context for the interval. The entry contraints of the block are used to narrow the interval if possible.</param>
    /// <param name="joinBlock">If the expression is a component of a join ("phi" node), then joinBlock is the block in which the join is defined.</param>
    /// <param name="definingBlock">If the expression is a component of a join ("phi" node), then definingBlock is the block from which the component expression flowed to joinBlock.</param>
    /// <param name="mappings">Provides several maps from expressions to concrete and abstract values.</param>
    /// <returns>
    /// An interval that bounds the values of the expression to a value between upper and lower bounds.
    /// If the expression cannot be bounded, for instance because it does not result int a numeric type, the result is null.
    /// </returns>
    private static Interval/*?*/ TryToGetAsInterval1<Instruction>(Instruction expression, Instruction operand, AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock,
      AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(operand != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);

      var operandInterval = TryToGetAsInterval(operand, referringBlock, joinBlock, definingBlock, mappings);
      var typeInterval = GetIntervalFor(expression.Type, mappings);
      if (typeInterval == null) return null;
      IOperation operation = expression.Operation;
      switch (operation.OperationCode) {
        //Instructions that cause or depend on side-effects. We'll keep them as is.
        case OperationCode.Box:
        case OperationCode.Call:
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Initobj:
        case OperationCode.Ldobj:
        case OperationCode.Localloc:
        case OperationCode.Mkrefany:
        case OperationCode.Newarr:
        case OperationCode.Newobj:
        case OperationCode.Pop:
        case OperationCode.Stsfld:
          return null;
        case OperationCode.Unbox:
        case OperationCode.Unbox_Any:
          return typeInterval;

        //Insructions that are side effect free and whose results can be cached and reused, but whose result values can never be known at compile time.
        case OperationCode.Castclass:
        case OperationCode.Ckfinite:
        case OperationCode.Isinst:
        case OperationCode.Ldvirtftn:
        case OperationCode.Refanytype:
        case OperationCode.Refanyval: //TODO: If we track object contents, we might be able to know the value of this at compile time.
          return null;
        case OperationCode.Ldlen:
          return mappings.Int64Interval;
        case OperationCode.Sizeof:
          return mappings.Int32Interval;

        //Instructions that conditionally affect control flow. We keep them as is, but update the control flow appropriately.
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          return null;

        //Instructions that are side-effect free and that could result in concrete compile time values.
        //We attempt to compute the compile time values.
        case OperationCode.Conv_I:
        case OperationCode.Conv_I1:
        case OperationCode.Conv_I2:
        case OperationCode.Conv_I4:
        case OperationCode.Conv_I8:
        case OperationCode.Conv_Ovf_I:
        case OperationCode.Conv_Ovf_I_Un:
        case OperationCode.Conv_Ovf_I1:
        case OperationCode.Conv_Ovf_I1_Un:
        case OperationCode.Conv_Ovf_I2:
        case OperationCode.Conv_Ovf_I2_Un:
        case OperationCode.Conv_Ovf_I4:
        case OperationCode.Conv_Ovf_I4_Un:
        case OperationCode.Conv_Ovf_I8:
        case OperationCode.Conv_Ovf_I8_Un:
        case OperationCode.Conv_Ovf_U:
        case OperationCode.Conv_Ovf_U_Un:
        case OperationCode.Conv_Ovf_U1:
        case OperationCode.Conv_Ovf_U1_Un:
        case OperationCode.Conv_Ovf_U2:
        case OperationCode.Conv_Ovf_U2_Un:
        case OperationCode.Conv_Ovf_U4:
        case OperationCode.Conv_Ovf_U4_Un:
        case OperationCode.Conv_Ovf_U8:
        case OperationCode.Conv_Ovf_U8_Un:
        case OperationCode.Conv_R_Un:
        case OperationCode.Conv_R4:
        case OperationCode.Conv_R8:
        case OperationCode.Conv_U:
        case OperationCode.Conv_U1:
        case OperationCode.Conv_U2:
        case OperationCode.Conv_U4:
        case OperationCode.Conv_U8:
          if (operandInterval == null) return typeInterval;
          var convertedLower = Evaluator.Evaluate(operation, operandInterval.LowerBound);
          Contract.Assume(convertedLower != null);
          var convertedUpper = Evaluator.Evaluate(operation, operandInterval.UpperBound);
          Contract.Assume(convertedUpper != null);
          var lowerUnderflows = convertedLower == Dummy.Constant || !Evaluator.IsNumericallyEqual(convertedLower, operandInterval.LowerBound);
          var upperOverflows = convertedUpper == Dummy.Constant || !Evaluator.IsNumericallyEqual(convertedUpper, operandInterval.UpperBound);
          if (lowerUnderflows && upperOverflows && Evaluator.IsNumericallyEqual(convertedLower, convertedUpper)) {
            lowerUnderflows = Evaluator.IsNegative(operandInterval.LowerBound);
            upperOverflows = Evaluator.IsPositive(operandInterval.UpperBound);
          }
          if (lowerUnderflows || upperOverflows) { convertedLower = typeInterval.LowerBound; convertedUpper = typeInterval.UpperBound; }
          return new Interval(convertedLower, convertedUpper, includesOverflow: upperOverflows, includesUnderflow: lowerUnderflows,
            excludesMinusOne: operandInterval.ExcludesMinusOne, excludesZero: operandInterval.ExcludesZero && !lowerUnderflows && !upperOverflows);
        case OperationCode.Dup:
          if (operandInterval == null) return typeInterval;
          return operandInterval;
        case OperationCode.Neg:
          if (operandInterval == null) return typeInterval;
          var negatedLowerBound = Evaluator.Evaluate(operation, operandInterval.LowerBound);
          Contract.Assume(negatedLowerBound != null);
          var negatedUpperBound = Evaluator.Evaluate(operation, operandInterval.UpperBound);
          Contract.Assume(negatedUpperBound != null);
          var lowerOverflows = Evaluator.IsNumericallyEqual(negatedLowerBound, operandInterval.LowerBound);
          if (lowerOverflows) { negatedUpperBound = typeInterval.LowerBound; negatedLowerBound = typeInterval.UpperBound; }
          return new Interval(negatedUpperBound, negatedLowerBound, includesOverflow: lowerOverflows,
            excludesMinusOne: operandInterval.ExcludesMinusOne, excludesZero: operandInterval.ExcludesZero);
        case OperationCode.Not:
          if (operandInterval == null) return typeInterval;
          var complementedLowerBound = Evaluator.Evaluate(operation, operandInterval.LowerBound);
          Contract.Assume(complementedLowerBound != null);
          var complementedUpperBound = Evaluator.Evaluate(operation, operandInterval.UpperBound);
          Contract.Assume(complementedUpperBound != null);
          return new Interval(complementedUpperBound, complementedLowerBound);

        //Instructions that can be cached in the absence of volatility, aliasing and multiple writes.
        case OperationCode.Ldfld:
        case OperationCode.Ldflda:
        case OperationCode.Ldind_I:
        case OperationCode.Ldind_I1:
        case OperationCode.Ldind_I2:
        case OperationCode.Ldind_I4:
        case OperationCode.Ldind_I8:
        case OperationCode.Ldind_R4:
        case OperationCode.Ldind_R8:
        case OperationCode.Ldind_Ref:
        case OperationCode.Ldind_U1:
        case OperationCode.Ldind_U2:
        case OperationCode.Ldind_U4:
          return typeInterval;
        //TODO: track what the pointer points to and see if that has a known value or interval.

        //Instructions that affect the SSA environment.
        case OperationCode.Starg:
        case OperationCode.Starg_S:
        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
          return null;

        //Instructions that transfer control out of the method being interpreted.
        case OperationCode.Ret:
        case OperationCode.Throw:
          return null;

        default:
          Contract.Assume(false);
          return null;
      }

    }

    private static readonly Operation add_ovf = new Operation() { OperationCode = OperationCode.Add_Ovf };
    private static readonly Operation mul_ovf = new Operation() { OperationCode = OperationCode.Mul_Ovf };
    private static readonly Operation sub_ovf = new Operation() { OperationCode = OperationCode.Sub_Ovf };

    /// <summary>
    /// Computes a numeric interval to bound the value that the given binary expression results in. 
    /// If the expression cannot be bounded by a numeric interval, the result is null.
    /// </summary>
    /// <param name="expression">An instruction that results in a value.</param>
    /// <param name="operand1"></param>
    /// <param name="operand2"></param>
    /// <param name="referringBlock">A block providing the context for the interval. The entry contraints of the block are used to narrow the interval if possible.</param>
    /// <param name="joinBlock">If the expression is a component of a join ("phi" node), then joinBlock is the block in which the join is defined.</param>
    /// <param name="definingBlock">If the expression is a component of a join ("phi" node), then definingBlock is the block from which the component expression flowed to joinBlock.</param>
    /// <param name="mappings">Provides several maps from expressions to concrete and abstract values.</param>
    /// <returns>
    /// An interval that bounds the values of the expression to a value between upper and lower bounds.
    /// If the expression cannot be bounded, for instance because it does not result int a numeric type, the result is null.
    /// </returns>
    private static Interval/*?*/ TryToGetAsInterval2<Instruction>(Instruction expression, Instruction operand1, Instruction operand2, AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock,
      AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);

      var typeInterval = GetIntervalFor(expression.Type, mappings);
      if (typeInterval == null) return null;
      var operand1Interval = TryToGetAsInterval(operand1, referringBlock, joinBlock, definingBlock, mappings);
      if (operand1Interval != null) {
        var singleton1 = operand1Interval.GetAsSingleton();
        if (singleton1 != null) return TryToGetAsInterval2(expression, typeInterval, singleton1, operand2, referringBlock, joinBlock, definingBlock, mappings);
      }
      var operand2Interval = TryToGetAsInterval(operand2, referringBlock, joinBlock, definingBlock, mappings);
      if (operand2Interval != null) {
        var singleton2 = operand2Interval.GetAsSingleton();
        if (singleton2 != null) return TryToGetAsInterval2(expression, typeInterval, operand1, singleton2, referringBlock, joinBlock, definingBlock, mappings);
      }

      IOperation operation = expression.Operation;
      switch (operation.OperationCode) {
        //Instructions that are side-effect free and cacheable and that could result in compile time values.
        //We attempt to compute the compile time values.
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          if (operand1Interval == null || operand2Interval == null) return typeInterval;
          var op = operation.OperationCode == OperationCode.Add ? add_ovf : operation;
          var lowerBound = Evaluator.Evaluate(op, operand1Interval.LowerBound, operand2Interval.LowerBound);
          var upperBound = Evaluator.Evaluate(op, operand1Interval.UpperBound, operand2Interval.UpperBound);
          var lowerBoundUnderflows = lowerBound == Dummy.Constant;
          var upperBoundOverflows = upperBound == Dummy.Constant;
          if (lowerBoundUnderflows || upperBoundOverflows) { lowerBound = typeInterval.LowerBound; upperBound = typeInterval.UpperBound; };
          return new Interval(lowerBound, upperBound, includesOverflow: upperBoundOverflows, includesUnderflow: lowerBoundUnderflows);

        case OperationCode.And:
          if (operand1Interval == null || operand2Interval == null) return typeInterval;
          if (Evaluator.IsNonNegative(operand1Interval.LowerBound)) {
            if (Evaluator.IsNonNegative(operand2Interval.LowerBound))
              return new Interval(Evaluator.GetZero(expression.Type), Evaluator.Min(operand1Interval.UpperBound, operand2Interval.UpperBound));
            else
              return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          } else if (Evaluator.IsNonNegative(operand2Interval.LowerBound)) {
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          } else {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          }

        case OperationCode.Ceq:
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
          return null;

        case OperationCode.Div:
        case OperationCode.Div_Un:
          if (operand1Interval == null || operand2Interval == null) return typeInterval;
          //p1..p2 / p3..p4    p1/p4..p2/p3
          //p1..p2 / 0..p4     p1/p4..p2 + zero div
          //p1..p2 / n3..p4    -p2..p2 + zero div
          //p1..p2 / n3..0     -p2..p2 + zero div
          //p1..p2 / n3..n4    p2/n4..p1/n3
          if (Evaluator.IsPositive(operand1Interval.LowerBound) && Evaluator.IsPositive(operand2Interval.UpperBound)) {
            if (Evaluator.IsPositive(operand2Interval.UpperBound)) {
              if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
                return new Interval(Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.UpperBound),
                  Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero);
              } else if (!Evaluator.IsNegative(operand2Interval.LowerBound)) {
                return new Interval(Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.UpperBound),
                  operand1Interval.UpperBound, excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
              } else {
                return new Interval(Evaluator.Negate(operand1Interval.UpperBound),
                  operand1Interval.UpperBound, excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
              }
            } else if (!Evaluator.IsNegative(operand2Interval.UpperBound)) {
              return new Interval(Evaluator.Negate(operand1Interval.UpperBound),
                operand1Interval.UpperBound, excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else {
              return new Interval(Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.UpperBound),
                Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.UpperBound), excludesZero: operand1Interval.ExcludesZero);
            }
          }
          //n1..p2 / p3..p4   n1/p3..p2/p3
          //n1..p2 / 0..p4   n1..p2 + zero div
          //n1..p2 / n3..p4   n1..max(-n1, p2) + zero div
          //min..p2 / n3..p4  min..max + zero div + overflow
          //n1..p2 / n3..0   -p2..-n1 + zero div
          //min..p2 / n3..0  min..max + zero div + overflow
          //min..p2 / n3..-1  min..max + overflow
          //n1..p2 / n3..n4   p2/n4..n1/n4
          if (Evaluator.IsNegative(operand1Interval.LowerBound) && Evaluator.IsPositive(operand1Interval.UpperBound)) {
            if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
              return new Interval(Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.LowerBound),
                Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero);
            } else if (!Evaluator.IsNegative(operand2Interval.LowerBound)) {
              return new Interval(operand1Interval.LowerBound, operand1Interval.UpperBound, excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else {
              if (Evaluator.IsPositive(operand2Interval.UpperBound)) {
                if (operand1Interval.ExcludesMinimumValue(expression.Type)) {
                  return new Interval(operand1Interval.LowerBound,
                    Evaluator.Max(Evaluator.Negate(operand1Interval.LowerBound), operand1Interval.UpperBound), excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
                } else {
                  return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero:
                    !operand2Interval.ExcludesZero, includesOverflow: !operand2Interval.ExcludesMinusOne);
                }
              } else if (!Evaluator.IsNegative(operand2Interval.UpperBound)) {
                if (operand1Interval.ExcludesMinimumValue(expression.Type)) {
                  return new Interval(Evaluator.Negate(operand1Interval.UpperBound), Evaluator.Negate(operand1Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero,
                    includesDivisionByZero: !operand2Interval.ExcludesZero);
                } else {
                  return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, excludesZero: operand1Interval.ExcludesZero,
                    includesDivisionByZero: !operand2Interval.ExcludesZero, includesOverflow: !operand2Interval.ExcludesMinusOne);
                }
              } else {
                if (!operand1Interval.ExcludesMinimumValue(expression.Type) && !operand2Interval.ExcludesMinusOne) {
                  return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, excludesZero: operand1Interval.ExcludesZero, includesOverflow: true);
                } else {
                  return new Interval(Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.UpperBound),
                    Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.UpperBound), excludesZero: operand1Interval.ExcludesZero);
                }
              }
            }
          }
          //n1..n2 / p3..p4   n1/p3..n2/p3
          //n1..n2 / 0..p4    n1..-n1 + zero div
          //n1..n2 / n3..p4   n1..-n1 + zero div
          //n1..n2 / n3..0   -n2..-n1 + zero div
          //n1..n2 / n3..n4   n2/n3..n1/n4 
          //min..n2 / p3..p4   min/p3..n2/p3
          //min..n2 / 0..p4   n1..-n1 + zero div
          //min..n2 / n3..p4  min..max + zero div + overflow
          //min..n2 / n3..0   min..max + zero div + overflow
          //min..n2 / n3..-1  min..max + overflow
          //min..n2 / n3..n4  n2/n3..min/n4
          Contract.Assume(Evaluator.IsNegative(operand1Interval.LowerBound) && Evaluator.IsNegative(operand1Interval.UpperBound)); //since the lower bound is never larger than the upper bound
          {
            if (operand1Interval.ExcludesMinimumValue(expression.Type)) {
              if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
                return new Interval(Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.LowerBound),
                  Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero);
              } else if (!Evaluator.IsNegative(operand2Interval.LowerBound)) {
                return new Interval(operand1Interval.LowerBound, Evaluator.Negate(operand1Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
              } else {
                if (Evaluator.IsPositive(operand2Interval.UpperBound)) {
                  return new Interval(operand1Interval.LowerBound, Evaluator.Negate(operand1Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
                } else if (!Evaluator.IsNegative(operand2Interval.UpperBound)) {
                  return new Interval(Evaluator.Negate(operand1Interval.LowerBound), Evaluator.Negate(operand1Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero,
                    includesDivisionByZero: !operand2Interval.ExcludesZero);
                } else {
                  return new Interval(Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.LowerBound),
                    Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.UpperBound), excludesZero: operand1Interval.ExcludesZero);
                }
              }
            } else {
              if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
                return new Interval(Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2Interval.LowerBound),
                  Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero);
              } else if (!Evaluator.IsNegative(operand2Interval.LowerBound)) {
                return new Interval(operand1Interval.LowerBound, Evaluator.Negate(operand1Interval.LowerBound), excludesZero: operand1Interval.ExcludesZero, includesDivisionByZero: !operand2Interval.ExcludesZero);
              } else {
                if (Evaluator.IsPositive(operand2Interval.UpperBound)) {
                  return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, excludesZero: operand1Interval.ExcludesZero,
                    includesDivisionByZero: !operand2Interval.ExcludesZero, includesOverflow: true);
                } else if (!Evaluator.IsNegative(operand2Interval.UpperBound)) {
                  return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, excludesZero: operand1Interval.ExcludesZero,
                    includesDivisionByZero: !operand2Interval.ExcludesZero, includesOverflow: !operand2Interval.ExcludesMinusOne);
                } else if (!operand2Interval.ExcludesMinusOne) {
                  return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, excludesZero: operand1Interval.ExcludesZero, includesOverflow: !operand2Interval.ExcludesMinusOne);
                } else {
                  return new Interval(Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2Interval.LowerBound),
                    Evaluator.Evaluate(operation, typeInterval.LowerBound, operand2Interval.UpperBound), excludesZero: operand1Interval.ExcludesZero);
                }
              }
            }
          }

        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un: {
            if (operand1Interval == null || operand2Interval == null) return typeInterval;
            var excludesZero = operand1Interval.ExcludesZero && operand2Interval.ExcludesZero;
            op = operation.OperationCode == OperationCode.Mul ? mul_ovf : operation;
            var llrl = Evaluator.Evaluate(op, operand1Interval.LowerBound, operand2Interval.LowerBound);
            var lurl = Evaluator.Evaluate(op, operand1Interval.UpperBound, operand2Interval.LowerBound);
            var llru = Evaluator.Evaluate(op, operand1Interval.LowerBound, operand2Interval.UpperBound);
            var luru = Evaluator.Evaluate(op, operand1Interval.UpperBound, operand2Interval.UpperBound);
            if (llrl == null || lurl == null || llru == null || luru == null || 
                llrl == Dummy.Constant || lurl == Dummy.Constant || llru == Dummy.Constant || luru == Dummy.Constant) {
              return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, includesOverflow: true);
            }
            lowerBound = Evaluator.Min(Evaluator.Min(llrl, lurl), Evaluator.Min(llru, luru));
            upperBound = Evaluator.Max(Evaluator.Max(llrl, lurl), Evaluator.Max(llru, luru));
            return new Interval(lowerBound, upperBound, excludesZero: excludesZero);
          }

        case OperationCode.Or:
          if (operand1Interval == null || operand2Interval == null) return typeInterval;
          if (Evaluator.IsNegative(operand1Interval.LowerBound)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else if (Evaluator.IsNegative(operand2Interval.LowerBound)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else {
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          }

        case OperationCode.Rem:
          if (operand2Interval == null) return typeInterval;
          if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
            if (operand1Interval != null) {
              if (Evaluator.IsNonNegative(operand1Interval.LowerBound)) {
                if (Evaluator.IsNumericallyGreaterThan(operand2Interval.LowerBound, operand1Interval.UpperBound)) {
                  //Since the divisor is always greater than the dividend, the remainder is always equal to the dividend.
                  return operand1Interval;
                }
                return new Interval(Evaluator.GetZero(expression.Type), operand2Interval.UpperBound);
              } else if (Evaluator.IsNegative(operand1Interval.UpperBound)) {
                var negLower = Evaluator.Negate(operand1Interval.LowerBound);
                if (Evaluator.IsNumericallyLessThan(operand1Interval.LowerBound, negLower)) {
                  //Since the divisor is always greater than the absolute dividend, the remainder is always equal to the dividend.
                  return operand1Interval;
                }
                return new Interval(Evaluator.Negate(operand2Interval.UpperBound), Evaluator.GetZero(expression.Type));
              }
            }
            return new Interval(Evaluator.Negate(operand2Interval.UpperBound), operand2Interval.UpperBound);
          }
          if (operand2Interval.ExcludesZero) return typeInterval;
          return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, includesDivisionByZero: true);

        case OperationCode.Rem_Un:
          if (operand2Interval == null) return typeInterval;
          var cop2lb = Evaluator.ConvertToUnsigned(operand2Interval.LowerBound);
          var cop2up = Evaluator.ConvertToUnsigned(operand2Interval.UpperBound);
          var op2up = Evaluator.Max(cop2lb, cop2up);
          if (operand1Interval != null) {
            var op2lb = Evaluator.Min(cop2lb, cop2up);
            var cop1lb = Evaluator.ConvertToUnsigned(operand1Interval.LowerBound);
            var cop1up = Evaluator.ConvertToUnsigned(operand1Interval.UpperBound);
            var op1up = Evaluator.Max(cop1lb, cop1up);
            if (Evaluator.IsNumericallyGreaterThan(op2lb, op1up)) {
              //Since the divisor is always greater than the dividend, the remainder is always equal to the dividend.
              return new Interval(Evaluator.Min(cop1lb, cop1up), op1up); //need a new interval to capture the potential conversion of the dividend to unsigned.
            }
          }
          return new Interval(Evaluator.GetZero(expression.Type), op2up);

        case OperationCode.Shl:
          goto case OperationCode.Mul;

        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          goto case OperationCode.Div;

        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
          if (operand1Interval == null || operand2Interval == null) return typeInterval;
          op = operation.OperationCode == OperationCode.Sub ? sub_ovf : operation;
          lowerBound = Evaluator.Evaluate(op, operand1Interval.LowerBound, operand2Interval.UpperBound);
          upperBound = Evaluator.Evaluate(op, operand1Interval.UpperBound, operand2Interval.LowerBound);
          var underflow = lowerBound == Dummy.Constant;
          var overflow = upperBound == Dummy.Constant;
          if (underflow || overflow) { lowerBound = typeInterval.LowerBound; upperBound = typeInterval.UpperBound; }
          return new Interval(lowerBound, upperBound, includesUnderflow: underflow, includesOverflow: overflow);

        case OperationCode.Xor:
          return typeInterval;

        //Instructions that conditionally affect control flow
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          return null;

        //Instructions that cause side-effect that we do not currently track.
        case OperationCode.Call:
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Cpblk:
        case OperationCode.Cpobj:
        case OperationCode.Initblk:
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
          return null;

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
          return typeInterval;

        default:
          Contract.Assume(false);
          return null;
      }
    }

    private static Interval/*?*/ TryToGetAsInterval2<Instruction>(Instruction expression, Interval typeInterval, IMetadataConstant operand1, Instruction operand2,
      AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock, AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(typeInterval != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);

      IOperation operation = expression.Operation;
      var operand2Interval = TryToGetAsInterval(operand2, referringBlock, joinBlock, definingBlock, mappings);
      if (operand2Interval == null) return null;
      var singleton2 = operand2Interval.GetAsSingleton();
      if (singleton2 != null) {
        var op = operation;
        switch (operation.OperationCode) {
          case OperationCode.Add: op = add_ovf; break;
          case OperationCode.Mul: op = mul_ovf; break;
          case OperationCode.Sub: op = sub_ovf; break;
        }
        var result = Evaluator.Evaluate(op, operand1, singleton2);
        if (result != null && result != Dummy.Constant)
          return new Interval(result, result);
      }
      switch (operation.OperationCode) {
        //Instructions that are side-effect free and cacheable and that could result in compile time values.
        //We attempt to compute the compile time values.
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un: {
            if (operand2Interval == null) return typeInterval;
            var op = operation.OperationCode == OperationCode.Add ? add_ovf : operation;
            var lowerBound = Evaluator.Evaluate(op, operand1, operand2Interval.LowerBound);
            var upperBound = Evaluator.Evaluate(op, operand1, operand2Interval.UpperBound);
            var lowerBoundUnderflows = lowerBound == Dummy.Constant;
            var upperBoundOverflows = upperBound == Dummy.Constant;
            if (lowerBoundUnderflows || upperBoundOverflows) { lowerBound = typeInterval.LowerBound; upperBound = typeInterval.UpperBound; };
            return new Interval(lowerBound, upperBound, includesOverflow: upperBoundOverflows, includesUnderflow: lowerBoundUnderflows);
          }

        case OperationCode.And:
          if (operand2Interval == null) return typeInterval;
          if (Evaluator.IsNonNegative(operand1)) {
            if (Evaluator.IsNonNegative(operand2Interval.LowerBound))
              return new Interval(Evaluator.GetZero(expression.Type), Evaluator.Min(operand1, operand2Interval.UpperBound));
            else
              return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          } else if (Evaluator.IsNegative(operand2Interval.UpperBound)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else {
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          }

        case OperationCode.Ceq:
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
          return null;

        case OperationCode.Div:
        case OperationCode.Div_Un:
          //p1 / p3..p4    p1/p4..p1/p3
          //p1 / 0..p4     p1/p4..p1 + zero div
          //p1 / n3..p4   -p1..p1 + zero div
          //p1 / n3..0    -p1..p1/n3 + zero div
          //p1 / n3..n4    p1/n4..p1/n3

          //n1 / p3..p4    n1/p3..n1/p4
          //n1 / 0..p4     n1..n1/p4 + zero div
          //n1 / n3..p4    n1..-n1 + zero div
          //n1 / n3..0     n1/n3..-n1 + zero div
          //n1 / n3..n4    n1/n3..n1/n4 

          if (Evaluator.IsNumericallyEqual(operand1, Evaluator.GetMinValue(expression.Type)) && !operand2Interval.ExcludesMinusOne)
            return new Interval(typeInterval.LowerBound, typeInterval.UpperBound,
              includesDivisionByZero: !operand2Interval.ExcludesZero, includesOverflow: true);
          if (Evaluator.IsPositive(operand1)) {
            if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
              return new Interval(Evaluator.Evaluate(operation, operand1, operand2Interval.UpperBound), Evaluator.Evaluate(operation, operand1, operand2Interval.LowerBound));
            } else if (!Evaluator.IsNegative(operand2Interval.LowerBound)) {
              return new Interval(Evaluator.Evaluate(operation, operand1, operand2Interval.UpperBound), operand1, includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else if (Evaluator.IsPositive(operand2Interval.UpperBound)) {
              return new Interval(Evaluator.Negate(operand1), operand1, includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else if (!Evaluator.IsNegative(operand2Interval.UpperBound)) {
              return new Interval(Evaluator.Negate(operand1), Evaluator.Evaluate(operation, operand1, operand2Interval.LowerBound), includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else {
              return new Interval(Evaluator.Evaluate(operation, operand1, operand2Interval.UpperBound), Evaluator.Evaluate(operation, operand1, operand2Interval.LowerBound));
            }
          } else if (Evaluator.IsNegative(operand1)) {
            if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
              return new Interval(Evaluator.Evaluate(operation, operand1, operand2Interval.LowerBound), Evaluator.Evaluate(operation, operand1, operand2Interval.UpperBound));
            } else if (!Evaluator.IsNegative(operand2Interval.LowerBound)) {
              return new Interval(operand1, Evaluator.Evaluate(operation, operand1, operand2Interval.UpperBound), includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else if (Evaluator.IsPositive(operand2Interval.UpperBound)) {
              return new Interval(operand1, Evaluator.Negate(operand1), includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else if (!Evaluator.IsNegative(operand2Interval.UpperBound)) {
              return new Interval(Evaluator.Evaluate(operation, operand1, operand2Interval.LowerBound), Evaluator.Negate(operand1), includesDivisionByZero: !operand2Interval.ExcludesZero);
            } else {
              return new Interval(Evaluator.Evaluate(operation, operand1, operand2Interval.LowerBound), Evaluator.Evaluate(operation, operand1, operand2Interval.UpperBound));
            }
          }
          Contract.Assume(MetadataExpressionHelper.IsIntegralZero(operand1));
          return new Interval(operand1, operand1);

        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un: {
            if (operand2Interval == null) return typeInterval;
            var excludesZero = MetadataExpressionHelper.IsIntegralNonzero(operand1) && operand2Interval.ExcludesZero;
            var op = operation.OperationCode == OperationCode.Mul ? mul_ovf : operation;
            var lrl = Evaluator.Evaluate(op, operand1, operand2Interval.LowerBound);
            var lru = Evaluator.Evaluate(op, operand1, operand2Interval.UpperBound);
            if (lrl == null || lru == null || lrl == Dummy.Constant || lru == Dummy.Constant) {
              return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, includesOverflow: true);
            }
            var lower = Evaluator.Min(lrl, lru);
            var upper = Evaluator.Max(lrl, lru);
            return new Interval(lower, upper, excludesZero: excludesZero);
          }

        case OperationCode.Or:
          if (operand2Interval == null) return typeInterval;
          if (Evaluator.IsNegative(operand1)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else if (Evaluator.IsNegative(operand2Interval.UpperBound)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else if (Evaluator.IsNonNegative(operand2Interval.LowerBound)) {
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          } else {
            return typeInterval;
          }

        case OperationCode.Rem:
          if (operand2Interval == null) return typeInterval;
          if (Evaluator.IsPositive(operand2Interval.LowerBound)) {
            if (Evaluator.IsNonNegative(operand1)) {
              if (Evaluator.IsNumericallyGreaterThan(operand2Interval.LowerBound, operand1)) {
                //Since the divisor is always greater than the dividend, the remainder is always equal to the dividend.
                return new Interval(operand1, operand1);
              }
              return new Interval(Evaluator.GetZero(expression.Type), operand2Interval.UpperBound);
            }
          }
          return typeInterval;

        case OperationCode.Rem_Un:
          if (operand2Interval == null) return typeInterval;
          var cop2lb = Evaluator.ConvertToUnsigned(operand2Interval.LowerBound);
          var cop2up = Evaluator.ConvertToUnsigned(operand2Interval.UpperBound);
          var op2up = Evaluator.Max(cop2lb, cop2up);
          var op2lb = Evaluator.Min(cop2lb, cop2up);
          var op1 = Evaluator.ConvertToUnsigned(operand1);
          if (Evaluator.IsNumericallyGreaterThan(op2lb, op1)) {
            //Since the divisor is always greater than the dividend, the remainder is always equal to the dividend.
            return new Interval(op1, op1);
          }
          return new Interval(Evaluator.GetZero(expression.Type), op2up);

        case OperationCode.Shl: {
            var operand2aLower = Evaluator.Evaluate(operation, Evaluator.GetOne(expression.Type), operand2Interval.LowerBound);
            var operand2aUpper = Evaluator.Evaluate(operation, Evaluator.GetOne(expression.Type), operand2Interval.UpperBound);
            if (Evaluator.IsNumericallyLessThan(operand2aLower, operand2Interval.LowerBound) || Evaluator.IsNumericallyLessThan(operand2aUpper, operand2Interval.UpperBound))
              return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, includesOverflow: true);
            operation = mul_ovf;
            operand2Interval = new Interval(operand2aLower, operand2aUpper, excludesZero: true);
            goto case OperationCode.Mul;
          }

        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          goto case OperationCode.Div;

        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un: {
            if (operand2Interval == null) return typeInterval;
            var op = operation.OperationCode == OperationCode.Sub ? sub_ovf : operation;
            var lowerBound = Evaluator.Evaluate(op, operand1, operand2Interval.LowerBound);
            var upperBound = Evaluator.Evaluate(op, operand1, operand2Interval.UpperBound);
            var underflow = lowerBound == Dummy.Constant;
            var overflow = upperBound == Dummy.Constant;
            if (underflow || overflow) { lowerBound = typeInterval.LowerBound; upperBound = typeInterval.UpperBound; }
            return new Interval(lowerBound, upperBound, includesUnderflow: underflow, includesOverflow: overflow);
          }

        case OperationCode.Xor:
          return typeInterval;

        //Instructions that conditionally affect control flow
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          return null;

        //Instructions that cause side-effect that we do not currently track.
        case OperationCode.Call:
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Cpblk:
        case OperationCode.Cpobj:
        case OperationCode.Initblk:
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
          return null;

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
          return typeInterval;

        default:
          Contract.Assume(false);
          return null;
      }
    }

    private static Interval/*?*/ TryToGetAsInterval2<Instruction>(Instruction expression, Interval typeInterval, Instruction operand1, IMetadataConstant operand2,
      AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock, AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(typeInterval != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);

      IOperation operation = expression.Operation;
      var operand1Interval = TryToGetAsInterval(operand1, referringBlock, joinBlock, definingBlock, mappings);
      if (operand1Interval == null) return null;
      var singleton1 = operand1Interval.GetAsSingleton();
      if (singleton1 != null) {
        var op = operation;
        switch (operation.OperationCode) {
          case OperationCode.Add: op = add_ovf; break;
          case OperationCode.Mul: op = mul_ovf; break;
          case OperationCode.Sub: op = sub_ovf; break;
        }
        var result = Evaluator.Evaluate(op, singleton1, operand2);
        if (result != null && result != Dummy.Constant)
          return new Interval(result, result);
      }
      switch (operation.OperationCode) {
        //Instructions that are side-effect free and cacheable and that could result in compile time values.
        //We attempt to compute the compile time values.
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un: {
            if (operand1Interval == null) return typeInterval;
            var op = operation.OperationCode == OperationCode.Add ? add_ovf : operation;
            var lowerBound = Evaluator.Evaluate(op, operand1Interval.LowerBound, operand2);
            var upperBound = Evaluator.Evaluate(op, operand1Interval.UpperBound, operand2);
            var lowerBoundUnderflows = lowerBound == Dummy.Constant;
            var upperBoundOverflows = upperBound == Dummy.Constant;
            if (lowerBoundUnderflows || upperBoundOverflows) { lowerBound = typeInterval.LowerBound; upperBound = typeInterval.UpperBound; };
            return new Interval(lowerBound, upperBound, includesOverflow: upperBoundOverflows, includesUnderflow: lowerBoundUnderflows);
          }

        case OperationCode.And:
          if (operand1Interval == null) return typeInterval;
          if (Evaluator.IsNonNegative(operand2)) {
            if (Evaluator.IsNonNegative(operand1Interval.LowerBound))
              return new Interval(Evaluator.GetZero(expression.Type), Evaluator.Min(operand1Interval.LowerBound, operand2));
            else
              return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          } else if (Evaluator.IsNegative(operand1Interval.UpperBound)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else {
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          }

        case OperationCode.Ceq:
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
          return null;

        case OperationCode.Div:
        case OperationCode.Div_Un:
          if (operand1Interval == null) return null;
          if (MetadataExpressionHelper.IsIntegralZero(operand2)) return null;
          if (MetadataExpressionHelper.IsIntegralMinusOne(operand2) && !operand1Interval.ExcludesMinimumValue(expression.Type))
            return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, includesOverflow: true);
          if (Evaluator.IsPositive(operand2))
            return new Interval(Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2), Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2));
          else
            return new Interval(Evaluator.Evaluate(operation, operand1Interval.UpperBound, operand2), Evaluator.Evaluate(operation, operand1Interval.LowerBound, operand2));

        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un: {
            if (operand1Interval == null) return typeInterval;
            var excludesZero = operand1Interval.ExcludesZero && MetadataExpressionHelper.IsIntegralNonzero(operand2);
            var op = operation.OperationCode == OperationCode.Mul ? mul_ovf : operation;
            Contract.Assume(operand2 != null);
            var llr = Evaluator.Evaluate(op, operand1Interval.LowerBound, operand2);
            var lur = Evaluator.Evaluate(op, operand1Interval.UpperBound, operand2);
            if (llr == null || lur == null || llr == Dummy.Constant || lur == Dummy.Constant) {
              return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, includesOverflow: true);
            }
            var lower = Evaluator.Min(llr, lur);
            var upper = Evaluator.Max(llr, lur);
            return new Interval(lower, upper, excludesZero: excludesZero);
          }

        case OperationCode.Or:
          if (operand1Interval == null) return typeInterval;
          if (Evaluator.IsNegative(operand2)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else if (Evaluator.IsNegative(operand1Interval.UpperBound)) {
            return new Interval(Evaluator.GetMinValue(expression.Type), Evaluator.GetMinusOne(expression.Type));
          } else if (Evaluator.IsNonNegative(operand1Interval.LowerBound)) {
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMaxValue(expression.Type));
          } else {
            return typeInterval;
          }

        case OperationCode.Rem:
          if (operand1Interval == null) return typeInterval;
          if (MetadataExpressionHelper.IsIntegralZero(operand2))
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMinusOne(expression.Type), includesDivisionByZero: true);
          if (Evaluator.IsPositive(operand2)) {
            if (Evaluator.IsNonNegative(operand1Interval.LowerBound)) {
              if (Evaluator.IsNumericallyGreaterThan(operand2, operand1Interval.UpperBound)) {
                //Since the divisor is always greater than the dividend, the remainder is always equal to the dividend.
                return operand1Interval;
              }
              return new Interval(Evaluator.GetZero(expression.Type), operand2);
            }
          }
          return typeInterval;

        case OperationCode.Rem_Un:
          if (operand1Interval == null) return typeInterval;
          if (MetadataExpressionHelper.IsIntegralZero(operand2))
            return new Interval(Evaluator.GetZero(expression.Type), Evaluator.GetMinusOne(expression.Type), includesDivisionByZero: true);
          var cop1lb = Evaluator.ConvertToUnsigned(operand1Interval.LowerBound);
          var cop1up = Evaluator.ConvertToUnsigned(operand1Interval.UpperBound);
          var op1up = Evaluator.Max(cop1lb, cop1up);
          var op1lb = Evaluator.Min(cop1lb, cop1up);
          var op2 = Evaluator.ConvertToUnsigned(operand2);
          if (Evaluator.IsNumericallyGreaterThan(op2, op1up)) {
            //Since the divisor is always greater than the dividend, the remainder is always equal to the dividend.
            return new Interval(op1lb, op1up);
          }
          return new Interval(Evaluator.GetZero(expression.Type), op2);

        case OperationCode.Shl: {
            var operand2a = Evaluator.Evaluate(operation, Evaluator.GetOne(expression.Type), operand2);
            if (Evaluator.IsNumericallyLessThan(operand2a, operand2))
              return new Interval(typeInterval.LowerBound, typeInterval.UpperBound, includesOverflow: true);
            operation = mul_ovf;
            operand2 = operand2a;
            goto case OperationCode.Mul;
          }

        case OperationCode.Shr:
        case OperationCode.Shr_Un:
          goto case OperationCode.Div;

        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un: {
            if (operand1Interval == null) return typeInterval;
            var op = operation.OperationCode == OperationCode.Sub ? sub_ovf : operation;
            var lowerBound = Evaluator.Evaluate(op, operand1Interval.LowerBound, operand2);
            var upperBound = Evaluator.Evaluate(op, operand1Interval.UpperBound, operand2);
            var underflow = lowerBound == Dummy.Constant;
            var overflow = upperBound == Dummy.Constant;
            if (underflow || overflow) { lowerBound = typeInterval.LowerBound; upperBound = typeInterval.UpperBound; }
            return new Interval(lowerBound, upperBound, includesUnderflow: underflow, includesOverflow: overflow);
          }

        case OperationCode.Xor:
          return typeInterval;

        //Instructions that conditionally affect control flow
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          return null;

        //Instructions that cause side-effect that we do not currently track.
        case OperationCode.Call:
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Cpblk:
        case OperationCode.Cpobj:
        case OperationCode.Initblk:
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
          return null;

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
          return typeInterval;

        default:
          Contract.Assume(false);
          return null;
      }
    }

    private static Interval/*?*/ TryToGetAsIntervalN<Instruction>(Instruction expression, Instruction operand1, Instruction[] operands2ToN, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operands2ToN != null);
      Contract.Requires(mappings != null);

      return GetIntervalFor(expression.Type, mappings);
    }

    private static Interval/*?*/ TryToGetAsJoinedInterval<Instruction>(Instruction expression, AiBasicBlock<Instruction> referringBlock, AiBasicBlock<Instruction> joinBlock,
      AiBasicBlock<Instruction> definingBlock, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(expression != null);
      Contract.Requires(referringBlock != null);
      Contract.Requires(mappings != null);

      var exprDefBlock = mappings.GetDefiningBlockFor(expression);
      if (exprDefBlock == null) return null;
      var predecessorBlocks = exprDefBlock.Predecessors;
      Contract.Assume(predecessorBlocks != null);
      var operand1 = expression.Operand1 as Instruction;
      if (operand1 == null) return null;
      Contract.Assume(predecessorBlocks.Count > 0);
      var block1 = predecessorBlocks[0];
      var interval1 = TryToGetAsInterval(operand1, referringBlock, exprDefBlock, block1, mappings);
      if (interval1 == null) return null;
      var operand2 = expression.Operand2 as Instruction;
      if (operand2 != null) {
        Contract.Assume(predecessorBlocks.Count > 1);
        var block2 = predecessorBlocks[1];
        if (operand2.Operation.OperationCode == OperationCode.Add) {
          var singleton1 = interval1.GetAsSingleton();
          if (singleton1 != null && operand2.Operand2 is Instruction) {
            var increment = mappings.GetCompileTimeConstantValueFor((Instruction)operand2.Operand2);
            if (increment != null && Evaluator.IsPositive(increment)) {
              var typeInterval = GetIntervalFor(expression.Type, mappings)??new Interval();
              var result = new Interval(singleton1, typeInterval.UpperBound);
              Contract.Assume(operand2.Operand1 is Instruction);
              result = Narrow(operand2, result, referringBlock, exprDefBlock, block2, mappings);
              return result;
            }
          }
        } else if (operand2.Operation.OperationCode == OperationCode.Sub) {
          var singleton1 = interval1.GetAsSingleton();
          if (singleton1 != null && operand2.Operand2 is Instruction) {
            var decrement = mappings.GetCompileTimeConstantValueFor((Instruction)operand2.Operand2);
            if (decrement != null && Evaluator.IsPositive(decrement)) {
              var typeInterval = GetIntervalFor(expression.Type, mappings)??new Interval();
              var result = new Interval(typeInterval.LowerBound, singleton1);
              Contract.Assume(operand2.Operand1 is Instruction);
              result = Narrow(operand2, result, referringBlock, exprDefBlock, block2, mappings);
              return result;
            }
          }
        }
        var interval2 = TryToGetAsInterval(operand2, referringBlock, exprDefBlock, block2, mappings);
        if (interval2 == null) return null;
        return interval1.Join(interval2);
      }
      var operands2ToN = expression.Operand2 as Instruction[];
      if (operands2ToN == null) return interval1;
      var interval = interval1;
      for (int i = 0, n = operands2ToN.Length; i < n; i++) {
        var operandi = operands2ToN[i];
        Contract.Assume(operandi != null);
        Contract.Assume(predecessorBlocks.Count > i+1);
        var blocki = predecessorBlocks[i+1];
        var intervalI = TryToGetAsInterval(operandi, referringBlock, exprDefBlock, blocki, mappings);
        if (intervalI == null) return null;
        interval = interval.Join(intervalI);
      }
      return interval;
    }

    private static Interval/*?*/ GetIntervalFor<Instruction>(ITypeReference type, ValueMappings<Instruction> mappings)
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {
      Contract.Requires(type != null);
      Contract.Requires(mappings != null);

      switch (type.TypeCode) {
        case PrimitiveTypeCode.Int16: return mappings.Int16Interval;
        case PrimitiveTypeCode.Int32: return mappings.Int32Interval;
        case PrimitiveTypeCode.Int64: return mappings.Int64Interval;
        case PrimitiveTypeCode.Int8: return mappings.Int8Interval;
        case PrimitiveTypeCode.UInt16: return mappings.UInt16Interval;
        case PrimitiveTypeCode.UInt32: return mappings.UInt32Interval;
        case PrimitiveTypeCode.UInt64: return mappings.UInt64Interval;
        case PrimitiveTypeCode.UInt8: return mappings.UInt8Interval;
      }
      return null;
    }

    /// <summary>
    /// Returns a <see cref="System.String"/> that represents this instance.
    /// </summary>
    /// <returns>
    /// A <see cref="System.String"/> that represents this instance.
    /// </returns>
    public override string ToString() {
      return "[" + this.LowerBound.ToString() + "..." + this.UpperBound.ToString() + "]";
    }

  }
}