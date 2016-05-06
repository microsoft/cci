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

namespace Microsoft.Cci.Analysis {

  /// <summary>
  /// Provides several maps from expressions to concrete and abstract values.
  /// </summary>
  /// <typeparam name="Instruction">An instruction that results in value.</typeparam>
  public class ValueMappings<Instruction>
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// Provides several maps from expressions to concrete and abstract values.
    /// </summary>
    public ValueMappings(IPlatformType platformType, ISatSolver/*?*/ satSolver = null) {
      Contract.Requires(platformType != null);
      if (satSolver != null)
        this.satSolverHelper = new SatSolverHelper<Instruction>(satSolver, this);
      this.Int8Interval = new Interval(new MetadataConstant() { Value = sbyte.MinValue, Type = platformType.SystemInt8 },
        new MetadataConstant() { Value = sbyte.MaxValue, Type = platformType.SystemInt8 });
      this.Int16Interval = new Interval(new MetadataConstant() { Value = short.MinValue, Type = platformType.SystemInt16 },
        new MetadataConstant() { Value = short.MaxValue, Type = platformType.SystemInt16 });
      this.Int32Interval = new Interval(new MetadataConstant() { Value = int.MinValue, Type = platformType.SystemInt32 },
        new MetadataConstant() { Value = int.MaxValue, Type = platformType.SystemInt32 });
      this.Int64Interval = new Interval(new MetadataConstant() { Value = long.MinValue, Type = platformType.SystemInt64 },
        new MetadataConstant() { Value = long.MaxValue, Type = platformType.SystemInt64 });
      this.UInt8Interval = new Interval(new MetadataConstant() { Value = byte.MinValue, Type = platformType.SystemUInt8 },
        new MetadataConstant() { Value = byte.MaxValue, Type = platformType.SystemUInt8 });
      this.UInt16Interval = new Interval(new MetadataConstant() { Value = ushort.MinValue, Type = platformType.SystemUInt16 },
        new MetadataConstant() { Value = ushort.MaxValue, Type = platformType.SystemUInt16 });
      this.UInt32Interval = new Interval(new MetadataConstant() { Value = uint.MinValue, Type = platformType.SystemUInt32 },
        new MetadataConstant() { Value = uint.MaxValue, Type = platformType.SystemUInt32 });
      this.UInt64Interval = new Interval(new MetadataConstant() { Value = ulong.MinValue, Type = platformType.SystemUInt64 },
        new MetadataConstant() { Value = ulong.MaxValue, Type = platformType.SystemUInt64 });
    }

    Hashtable<Instruction, object> compileTimeConstantValueForExpression = new Hashtable<Instruction, object>();
    Hashtable<IMetadataConstant> compileTimeConstantForSSAVariable = new Hashtable<IMetadataConstant>();
    Hashtable<Instruction, AiBasicBlock<Instruction>> definingBlockForExpression = new Hashtable<Instruction, AiBasicBlock<Instruction>>();
    Hashtable<Instruction> definingExpressionForSSAVariable = new Hashtable<Instruction>();
    Hashtable<Join> definingJoinForSSAVariable = new Hashtable<Join>();
    Hashtable<Instruction, Instruction> expressionForExpression = new Hashtable<Instruction, Instruction>();
    Interval dummyInterval = new Interval();
    SatSolverHelper<Instruction>/*?*/ satSolverHelper;
    HashtableForUintValues<Instruction> recursiveExpressions = new HashtableForUintValues<Instruction>();

    internal Interval Int8Interval;
    internal Interval Int16Interval;
    internal Interval Int32Interval;
    internal Interval Int64Interval;
    internal Interval UInt8Interval;
    internal Interval UInt16Interval;
    internal Interval UInt32Interval;
    internal Interval UInt64Interval;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.compileTimeConstantValueForExpression != null);
      Contract.Invariant(this.compileTimeConstantForSSAVariable != null);
      Contract.Invariant(this.definingBlockForExpression != null);
      Contract.Invariant(this.definingExpressionForSSAVariable != null);
      Contract.Invariant(this.definingJoinForSSAVariable != null);
      Contract.Invariant(this.expressionForExpression != null);
      Contract.Invariant(this.recursiveExpressions != null);
    }

    /// <summary>
    /// Returns a compile time constant value that is known to be the value of the given expression in all possible executions of the analyzed method.
    /// If the value is Dummy.Constant, the expression is known to fail at runtime in all possible executions of the analyzed method.
    /// </summary>
    /// <param name="expression">The expression for which a compile time constant value is desired.</param>
    /// <param name="block">A block providing the context for the interval. The entry contraints of the block are used to narrow the interval if possible. May be null.</param>
    [Pure]
    public IMetadataConstant/*?*/ GetCompileTimeConstantValueFor(Instruction expression, AiBasicBlock<Instruction>/*?*/ block = null) {
      Contract.Requires(expression != null);

      var result = this.compileTimeConstantValueForExpression[expression] as IMetadataConstant;
      if (result != null) return result;
      var ce = this.GetCanonicalExpressionFor(expression);
      if (ce == null) return null;
      result = this.compileTimeConstantValueForExpression[ce] as IMetadataConstant;
      if (result != null || block == null) return result;
      result = block.ConstantForExpression[expression] as IMetadataConstant;
      if (result != null) {
        if (result == Dummy.Constant) return null;
        return result;
      }
      var operand1 = ce.Operand1 as Instruction;
      if (operand1 == null) return null;
      var operand2 = ce.Operand2 as Instruction;
      if (operand2 != null) {
        result = Evaluator.Evaluate(ce.Operation, operand1, operand2, this, block);
        block.ConstantForExpression[expression] = result??Dummy.Constant;
        if (result != null && result != Dummy.Constant) {
          return result;
        }
      } else {
        var operands2toN = ce.Operand2 as Instruction[];
        if (operands2toN != null) {
          result = Evaluator.Evaluate(ce.Operation, operand1, operands2toN, this, block);
          block.ConstantForExpression[expression] = result??Dummy.Constant;
          if (result != null && result != Dummy.Constant) {
            return result;
          }
        }
      }
      return null;
    }

    /// <summary>
    /// Returns a compile time constant that is known to be the value that is assigned to the given variable in all possible executions of the analyzed method.
    /// </summary>
    public IMetadataConstant/*?*/ GetCompileTimeConstantValueFor(INamedEntity variable) {
      Contract.Requires(variable != null);
      return this.compileTimeConstantForSSAVariable[(uint)variable.Name.UniqueKey];
    }

    /// <summary>
    /// Returns an expression that results in the same value as the given expression in all possible executions of the analyzed method.
    /// May be null if no such expression can be found.
    /// </summary>
    public Instruction/*?*/ GetCanonicalExpressionFor(Instruction expression) {
      Contract.Requires(expression != null);

      return this.expressionForExpression[expression];
    }

    /// <summary>
    /// Returns the expression which computes the value of the sole assignment to the given variable.
    /// This can be a NOP instruction, which is a "phi" node in the SSA. Can be null.
    /// </summary>
    public Instruction/*?*/ GetDefiningExpressionFor(INamedEntity variable) {
      Contract.Requires(variable != null);
      return this.definingExpressionForSSAVariable[(uint)variable.Name.UniqueKey];
    }

    /// <summary>
    /// Returns the block that defined the given "phi" node expression. This is useful because the entry contraints of this block might provide
    /// additional information about the expression. If the block has not been defined earlier via SetDefiningBlockFor, the result will be null.
    /// </summary>
    internal AiBasicBlock<Instruction>/*?*/ GetDefiningBlockFor(Instruction expression) {
      Contract.Requires(expression != null);
      return this.definingBlockForExpression[expression];
    }

    /// <summary>
    /// Return the Join information of the "phi" node that is the right hand side of the sole assignment to the given variable. Returns null if the variable is not
    /// defined by a "phi" node.
    /// </summary>
    /// <param name="variable"></param>
    /// <returns></returns>
    public Join/*?*/ GetDefiningJoinFor(INamedEntity variable) {
      Contract.Requires(variable != null);
      return this.definingJoinForSSAVariable[(uint)variable.Name.UniqueKey];
    }

    /// <summary>
    /// Computes an inclusive numerical interval that contains the runtime value of the given expression. If no such interval can be found, the result is null.
    /// </summary>
    /// <param name="expression">The expression for which a containing interval is desired.</param>
    /// <param name="block">A block providing the context for the interval. The entry contraints of the block are used to narrow the interval if possible.</param>
    public Interval/*?*/ GetIntervalFor(Instruction expression, AiBasicBlock<Instruction> block) {
      Contract.Requires(expression != null);
      Contract.Requires(block != null);

      var interval = block.IntervalForExpression[expression];
      if (interval == this.dummyInterval) return null;
      if (interval == null) {
        block.IntervalForExpression[expression] = this.dummyInterval;
        interval = Interval.TryToGetAsInterval(expression, block, null, null, this);
        block.IntervalForExpression[expression] = interval??this.dummyInterval;
      }
      return interval;
    }

    /// <summary>
    /// Returns true if the given expression is the value of a variable that is updated (inside of a loop) with a value that depends on the value of the variable in a an earlier iteration of the loop.
    /// </summary>
    internal bool IsRecursive(Instruction expression) {
      Contract.Requires(expression != null);

      var tag = this.recursiveExpressions[expression];
      if (tag == 0) {
        tag = 1;
        var operand1 = expression.Operand1 as Instruction;
        if (operand1 != null) {
          if (this.IsRecursive(operand1))
            tag = 2;
          else {
            var operand2 = expression.Operand2 as Instruction;
            if (operand2 != null) {
              if (this.IsRecursive(operand2)) tag = 2;
            } else {
              var operand2toN = expression.Operand2 as Instruction[];
              if (operand2toN != null) {
                for (int i = 0, n = operand2toN.Length; i < n; i++) {
                  var operandi = operand2toN[i];
                  Contract.Assume(operandi != null);
                  if (this.IsRecursive(operandi)) {
                    tag = 2;
                    break;
                  }
                }
              }
            }
          }
        }
        this.recursiveExpressions[expression] = tag;
      }
      return tag == 2;
    }

    /// <summary>
    /// Uses the SAT solver, if supplied, to check if the given Boolean expression is true in the context of the given block.
    /// Since this problem is not decidable, the solver may not be able to return an answer, in which case the return result is null
    /// rather than false or true. Likewise, if no solver is available, the result is null.
    /// </summary>
    public bool? CheckIfExpressionIsTrue(Instruction expression, AiBasicBlock<Instruction> block) {
      Contract.Requires(expression != null);
      Contract.Requires(block != null);

      var satSolverHelper = this.satSolverHelper;
      if (satSolverHelper == null) return null;
      var context = block.SatSolverContext;
      if (context == null) {
        block.SatSolverContext = context = satSolverHelper.SatSolver.GetNewContext();
        Contract.Assume(context != null);
        var constraintsAtEntry = satSolverHelper.GetSolverExpressionFor(block.ConstraintsAtEntry);
        if (constraintsAtEntry != null) context.Add(constraintsAtEntry);
      }
      var solverExpression = this.satSolverHelper.GetSolverExpressionFor(expression, block.ConstraintsAtEntry);
      context.MakeCheckPoint();
      if (!this.IsRecursive(expression))
        satSolverHelper.AddPhiNodeConstraints(expression, context);
      //context.MakeCheckPoint();
      context.Add(solverExpression);
      var result = context.Check();
      context.RestoreCheckPoint();
      if (result != null && !result.Value) {
        //context.RestoreCheckPoint();
        return false; //The expression is never satisfied, so it is known to be false.
      }
      context.MakeCheckPoint();
      if (!this.IsRecursive(expression))
        satSolverHelper.AddPhiNodeConstraints(expression, context);
      context.AddInverse(solverExpression);
      result = context.Check();
      context.RestoreCheckPoint();
      //context.RestoreCheckPoint();

      if (result != null && !result.Value) return true; //The inverse expression is never satisfied, so the expression is known to be true.
      return null;
    }

    /// <summary>
    /// Associates the given expression with a canonical version that will always evaluate to the same value as the given expression. 
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="canonicalExpression"></param>
    internal void SetCanonicalExpressionFor(Instruction expression, Instruction canonicalExpression) {
      Contract.Requires(expression != null);
      Contract.Requires(canonicalExpression != null);

      this.expressionForExpression[expression] = canonicalExpression;
    }

    /// <summary>
    /// Associates the given SSA variable with the compile time constant that is always the value of the right side of the single assignment to this variable.
    /// </summary>
    public void SetCompileTimeConstantValueFor(INamedEntity variable, IMetadataConstant compileTimeConstant) {
      Contract.Requires(variable != null);
      Contract.Requires(compileTimeConstant != null);

      this.compileTimeConstantForSSAVariable[(uint)variable.Name.UniqueKey] = compileTimeConstant;
    }

    /// <summary>
    /// Associates the given expression with a compile time constant value that is always its result when evaluated at runtime.
    /// </summary>
    internal void SetCompileTimeConstantValueFor(Instruction expression, IMetadataConstant compileTimeConstant) {
      Contract.Requires(expression != null);
      Contract.Requires(compileTimeConstant != null);

      this.compileTimeConstantValueForExpression[expression] = compileTimeConstant;
    }

    /// <summary>
    /// Keeps track of the block in which a "phi" node expression is defined. This is useful because the entry contraints of this block might provide
    /// additional information about the expression.
    /// </summary>
    internal void SetDefininingBlockFor(Instruction expression, AiBasicBlock<Instruction> block) {
      Contract.Requires(expression != null);
      Contract.Requires(block != null);

      this.definingBlockForExpression[expression] = block;
    }

    /// <summary>
    /// Associates the given SSA variable with the expression (expected to be canonicalized) that is the right hand side of the single assignment to this variable.
    /// </summary>
    internal void SetDefininingExpressionFor(INamedEntity variable, Instruction expression) {
      Contract.Requires(variable != null);
      Contract.Requires(expression != null);

      this.definingExpressionForSSAVariable[(uint)variable.Name.UniqueKey] = expression;
    }

    /// <summary>
    /// Associates the given SSA variable with the Join information of the "phi" node that that is the right hand side of the single assignment to this variable.
    /// </summary>
    internal void SetDefininingJoinFor(INamedEntity variable, Join join) {
      Contract.Requires(variable != null);
      Contract.Requires(join != null);

      this.definingJoinForSSAVariable[(uint)variable.Name.UniqueKey] = join;
    }

    /// <summary>
    /// Records that the given expression is the value of a variable that is updated (inside of a loop) with a value that depends on the value of the variable in a an earlier iteration of the loop.
    /// </summary>
    internal void SetIsRecursive(Instruction expression) {
      Contract.Requires(expression != null);
      this.recursiveExpressions[expression] = 2;
    }
  }

}