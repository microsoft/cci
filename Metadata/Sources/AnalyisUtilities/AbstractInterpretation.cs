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
  /// Provides a static method that interprets a control and data flow graph in order to compute the concrete (if possible) or abstract values of the local variables defined
  /// in the graph. This analysis requires the graph to be in SSA format.
  /// </summary>
  /// <typeparam name="BasicBlock">A type that is a subtype of Microsoft.Cci.Analysis.SSABasicBlock.</typeparam>
  /// <typeparam name="Instruction">A type that is a subtype of Microsoft.Cci.Analysis.Instruction and that has a default constructor.</typeparam>
  public class AbstractInterpreter<BasicBlock, Instruction>
    where BasicBlock : Microsoft.Cci.Analysis.AiBasicBlock<Instruction>, new()
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// Interprets the instructions in the given control and data flow graph, computing an abstract value for each instruction that produces a value.
    /// In some cases, it is also possible to compute concrete values. When these values are stored into variables, this is recorded in the given
    /// environment. The input graph is required to be in SSA format, so the recorded values will be accurate at all points where the variables
    /// are accessed in the graph.
    /// </summary>
    /// <param name="cdfg">A control and data flow graph in SSA form, to interpret in order to compute concrete and abstract values for variables in the graph.</param>
    /// <param name="cfgQueries">Presents information derived from a simple control flow graph. For example, traversal orders, predecessors, dominators and dominance frontiers.</param>
    /// <param name="mappings">Provides several maps from expressions to concrete and abstract values.</param>
    public static void InterpretUsingAbstractValues(ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ControlGraphQueries<BasicBlock, Instruction> cfgQueries, ValueMappings<Instruction> mappings) {
      Contract.Requires(cdfg != null);
      Contract.Requires(cfgQueries != null);
      Contract.Requires(mappings != null);
      var interpreter = new AbstractInterpreter<BasicBlock, Instruction>(cdfg, cfgQueries, mappings);
      interpreter.Interpret();
    }

    /// <summary>
    /// Creates an object that interprets a control and data flow graph in order to compute the concrete (if possible) or abstract values of the local variables defined
    /// in the graph. This analysis requires the graph to be in SSA format.
    /// </summary>
    /// <param name="cdfg">A control and data flow graph in SSA form, to interpret in order to compute concrete and abstract values for variables in the graph.</param>
    /// <param name="cfgQueries">Presents information derived from a simple control flow graph. For example, traversal orders, predecessors, dominators and dominance frontiers.</param>
    /// <param name="mappings">Provides several maps from expressions to concrete and abstract values.</param>
    private AbstractInterpreter(ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ControlGraphQueries<BasicBlock, Instruction> cfgQueries, ValueMappings<Instruction> mappings) {
      Contract.Requires(cdfg != null);
      Contract.Requires(cfgQueries != null);
      Contract.Requires(mappings != null);
      this.cdfg = cdfg;
      this.cfgQueries = cfgQueries;
      this.mappings = mappings;
      this.expressionCanonicalizer = new ExpressionCanonicalizer<Instruction>(mappings);
    }

    /// <summary>
    /// A control and data flow graph in SSA form, to interpret in order to compute concrete and abstract values for variables in the graph.
    /// </summary>
    ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg;
    /// <summary>
    /// Presents information derived from a simple control flow graph. For example, traversal orders, predecessors, dominators and dominance frontiers.
    /// </summary>
    ControlGraphQueries<BasicBlock, Instruction> cfgQueries;
    /// <summary>
    /// Provides several maps from expressions to concrete and abstract values.
    /// </summary>
    ValueMappings<Instruction> mappings;
    /// <summary>
    /// Keeps track of all of the blocks that should be considered again by the interpreter. A block can show up in here more than once.
    /// </summary>
    Queue<BasicBlock> blocksToInterpret = new Queue<BasicBlock>();
    /// <summary>
    /// A set of all basic blocks that have been interpreted at least once by the abstract interpreter.
    /// A block can be interpreted more than once when loops are encountered and a fix point is being calculated.
    /// The main reason to have this set is to allow the fix point to be detected in such a way that each
    /// block is interpreted at least once.
    /// </summary>
    SetOfObjects blocksInterpretedAtLeastOnce = new SetOfObjects();
    /// <summary>
    /// Zero or more expressions that are currently known to be true.
    /// </summary>
    List<Instruction> constraints = new List<Instruction>(2);
    /// <summary>
    /// The basic block that is currently being interpreted.
    /// </summary>
    BasicBlock currentBlock = new BasicBlock();
    /// <summary>
    /// A special kind of hash table that maps expressions to equivalent expressions that have been canonicalized.
    /// </summary>
    ExpressionCanonicalizer<Instruction> expressionCanonicalizer;
    /// <summary>
    /// True if the last interpreted instruction was an unconditional branch. (This includes condition branches whose conditions are compile time constants.)
    /// </summary>
    bool lastStatementWasUnconditionalTransfer;
    /// <summary>
    /// The set of blocks that are not known at this point to be unreachable from the block currently being interpreted.
    /// Add the target block to this set whenever interpreting a branch instruction that might be taken. Also add the fall through block
    /// if the last interpreted instruction was not an unconditional transfer.
    /// </summary>
    SetOfObjects liveSuccessorBlocks = new SetOfObjects();

    MultiHashtable<object> readVariables = new MultiHashtable<object>();
    DoubleHashtable<Instruction> inputExpressions = new DoubleHashtable<Instruction>();

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.cdfg != null);
      Contract.Invariant(this.cfgQueries != null);
      Contract.Invariant(this.mappings != null);
      Contract.Invariant(this.blocksToInterpret != null);
      Contract.Invariant(this.blocksInterpretedAtLeastOnce != null);
      Contract.Invariant(this.constraints != null);
      Contract.Invariant(this.currentBlock != null);
      Contract.Invariant(this.expressionCanonicalizer != null);
      Contract.Invariant(this.liveSuccessorBlocks != null);
      Contract.Invariant(this.readVariables != null);
      Contract.Invariant(this.inputExpressions != null);
    }

    /// <summary>
    /// Starting with each root, run through all reachable blocks and abstractly interpret their instructions.
    /// At the end of this, every instruction that computes a value will be associated with an expression that
    /// represents that value and optionally with a compile time constant if the value is known at compile time.
    /// Note that expressions are just instructions plus their data flow graphs (for which an instruction happens to be a convenient object model)
    /// but the "expression object" associated with a particular instruction is not necessarily the same object as the instruction,
    /// because the instruction, or a predecessor instruction in its data flow graph, may be equivalent to an earlier instruction. 
    /// In other words common subexpression elimination is performed. This reason for this is to make it easier to reason about the relationship between
    /// different expressions.
    /// </summary>
    private void Interpret() {
      //First set up the fall through block map. Note that this map is not quite a subset of the successor map, since we set it up for all 
      //blocks, including blocks that end on an unconditional transfers that do not target the blocks that immediately follow them.
      BasicBlock previousBlock = null;
      foreach (var block in this.cdfg.AllBlocks) {
        Contract.Assume(block != null);
        if (previousBlock != null)
          previousBlock.FallThroughBlock = block;
        previousBlock = block;
        //Also track the predecessors so that we don't always need to pass around this.cfgQueries.
        //This may not seem so bad, but it would require a lot more type parameters in a lot more places.
        //Instead, we pay a runtime price for compile time ease.
        var predecessors = this.cfgQueries.PredeccessorsFor(block);
        block.Predecessors = predecessors;
        //We also need to make the order of operands in stack setup instructions deterministic (i.e. operands must appear in the order of the precedessor blocks).
        if (block.OperandStack.Count > 0) MakeStackSetupInstructionsDeterministic(block, predecessors);
      }
      //Starting with each root, run through all blocks that are reachable from that root.
      foreach (var rootblock in this.cdfg.RootBlocks) {
        rootblock.ConstraintsAtEntry.Add(new List<Instruction>(0));
        this.blocksToInterpret.Enqueue(rootblock);
        while (this.blocksToInterpret.Count > 0) {
          var block = this.blocksToInterpret.Dequeue();
          Contract.Assume(block != null);
          this.Interpret(block); //This will add blocks that can be reached from block to this.blocksToInterpret.
        }
      }
    }

    private static void MakeStackSetupInstructionsDeterministic(BasicBlock block, Sublist<BasicBlock> predecessors) {
      Contract.Requires(block != null);
      int pc = predecessors.Count;
      if (pc == 0) return; //Can only happen if block is the first block of an exception handler
      var predecessorOperands = new Microsoft.Cci.Analysis.Instruction[pc];
      for (int i = 0, n = block.OperandStack.Count; i < n; i++) {
        var stackSetupInstruction = block.OperandStack[i];
        predecessorOperands[0] = stackSetupInstruction.Operand1;
        if (pc == 2) {
          Contract.Assume(stackSetupInstruction.Operand2 is Microsoft.Cci.Analysis.Instruction);
          predecessorOperands[1] = (Microsoft.Cci.Analysis.Instruction)stackSetupInstruction.Operand2;
        } else {
          Contract.Assume(stackSetupInstruction.Operand2 is Microsoft.Cci.Analysis.Instruction[]);
          var operands2ToN = stackSetupInstruction.Operand2 as Microsoft.Cci.Analysis.Instruction[];
          if (operands2ToN != null) {
            Contract.Assume(operands2ToN.Length == pc-1);
            for (int k = 1; k < pc; k++) predecessorOperands[k] = operands2ToN[k-1];
          }
        }
        Array.Sort(predecessorOperands, (x, y) => ((int)x.Operation.Offset) - (int)y.Operation.Offset);
        stackSetupInstruction.Operand1 = predecessorOperands[0];
        if (pc == 2)
          stackSetupInstruction.Operand2 = predecessorOperands[1];
        else {
          var operands2ToN = stackSetupInstruction.Operand2 as Microsoft.Cci.Analysis.Instruction[];
          Contract.Assume(operands2ToN != null);
          Contract.Assume(pc == operands2ToN.Length);
          for (int k = 1; k < pc; k++) operands2ToN[k-1] = predecessorOperands[k];
        }
      }
    }

    /// <summary>
    /// Abstractly interprets the instructions in the given block, if appropriate.
    /// By appropriate we mean that either the block has not yet been interpreted,
    /// or that that something has changed in the environment since we last interpreted
    /// the block. In the latter case, we interpret once again. As it happens, no precision
    /// is gained since the expressions that are produced by this interpreter retain full precision
    /// at all times. What we do gain from the additional iterations is further normalization of expressions
    /// that can lead to better subexpression identification and more constant folding.
    /// </summary>
    /// <param name="block"></param>
    private void Interpret(BasicBlock block) {
      Contract.Requires(block != null);

      //First set up the SSA local environment for this block and at the same time see if we actually need to interpret this block again.
      bool joinsAreTheSameAsLastTime = this.blocksInterpretedAtLeastOnce.Contains(block);
      if (!joinsAreTheSameAsLastTime) this.SetupReadVariablesFor(block);
      if (block.Joins != null) {
        foreach (var join in block.Joins) {
          var newLocal = join.NewLocal as INamedEntity;
          Contract.Assume(newLocal != null);
          if (this.mappings.GetCompileTimeConstantValueFor(newLocal) != null) continue; //nothing is going to change because of this variable.
          this.mappings.SetDefininingJoinFor(newLocal, join);
          var currentValue = this.mappings.GetDefiningExpressionFor(newLocal); //will be normalized via common subexpresion elimination.
          var newValue = this.UnionOfJoinedValues(join, block); //will be normalized via common subexpresion elimination.
          this.mappings.SetDefininingBlockFor(newValue, block);
          if (currentValue != newValue) {
            //The last time we interpreted this block (if we did do so), we did it with an expression for newLocal
            //that is different from the expression that we now get (after we have (again) interpreted "other" blocks that branch back to this block).
            if (currentValue == null || !this.mappings.IsRecursive(currentValue)) {
              this.mappings.SetDefininingExpressionFor(newLocal, newValue);
              joinsAreTheSameAsLastTime = false;
              if (currentValue != null && Evaluator.Contains(newValue, currentValue))
                this.mappings.SetIsRecursive(newValue);
            } else {
              //If newValue contains currentValue, then this is a loop variable and further iterations will not tell us more about it.
            }
          }
        }
      }
      if (this.AllReadVariablesHaveTheSameDefiningExpressionsAsLastTime(block) && joinsAreTheSameAsLastTime) {
        return; //We have reached a fixpoint. Another pass through this block will not be helpful.
      }
      this.blocksInterpretedAtLeastOnce.Add(block);
      this.currentBlock = block;
      block.IntervalForExpression.Clear();
      block.SatSolverContext = null;

      //Get constraints from the predecessors
      Contract.Assume(block.Predecessors != null);
      var pcount = block.Predecessors.Count;
      while (block.ConstraintsAtEntry.Count < pcount) block.ConstraintsAtEntry.Add(null);
      for (int i = 0; i < pcount; i++) {
        var predecessor = block.Predecessors[i] as BasicBlock;
        Contract.Assume(predecessor != null);
        var predSuccessors = this.cdfg.SuccessorsFor(predecessor);
        for (int j = 0, m = predSuccessors.Count; j < m; j++) {
          if (predSuccessors[j] == block && predecessor.ConstraintsAtExit.Count > j)
            block.ConstraintsAtEntry[i] = new List<Instruction>(predecessor.ConstraintsAtExit[j]);
        }
      }

      for (int i = 0, n = block.OperandStack.Count; i < n; i++) {
        Contract.Assume(n == block.OperandStack.Count);
        var stackSetupInstruction = block.OperandStack[i];
        if (stackSetupInstruction.Operation.Value == null)
          stackSetupInstruction.Operation = new Operation() { Value = Dummy.LocalVariable, Offset = (uint)i };
        if (stackSetupInstruction.Operand1 != null)
        {
          var canon = this.expressionCanonicalizer.GetCanonicalExpression(stackSetupInstruction, (Instruction)stackSetupInstruction.Operand1,
            stackSetupInstruction.Operand2 as Instruction, stackSetupInstruction.Operand2 as Instruction[]);
          stackSetupInstruction.Operand1 = canon.Operand1;
          stackSetupInstruction.Operand2 = canon.Operand2;
        }
        this.mappings.SetCanonicalExpressionFor(stackSetupInstruction, this.expressionCanonicalizer.GetCanonicalExpression(stackSetupInstruction));
        this.mappings.SetDefininingBlockFor(stackSetupInstruction, block);
      }
      //Combine the constraints established by the predecessor blocks into a single disjunction of conjunctions.
      Instruction disjunction = null;
      for (int i = 0, n = block.ConstraintsAtEntry.Count; i < n; i++) {
        Instruction conjunction = null;
        var constraintsFromParticularPredecessor = block.ConstraintsAtEntry[i];
        if (constraintsFromParticularPredecessor == null) continue;
        for (int j = 0, m = constraintsFromParticularPredecessor.Count; j < m; j++) {
          var constraint = constraintsFromParticularPredecessor[j];
          if (constraint == null) continue;
          if (conjunction == null)
            conjunction = constraint;
          else {
            var and = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.And }, Operand1 = conjunction, Operand2 = constraint, Type = constraint.Type };
            conjunction = this.expressionCanonicalizer.GetCanonicalExpression(and, conjunction, constraint);
          }
        }
        if (conjunction == null) {
          //constraintsFromParticularPredecessor is empty, so this is effectively false and the combined constraint is useless
          disjunction = null;
          break;
        }
        if (disjunction == null)
          disjunction = conjunction;
        else {
          var or = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Or }, Operand1 = disjunction, Operand2 = conjunction, Type = conjunction.Type };
          disjunction = this.expressionCanonicalizer.GetCanonicalExpression(or, disjunction, conjunction);
        }
      }
      this.constraints.Clear();
      if (disjunction != null && !(disjunction.Operation.OperationCode == OperationCode.Ldc_I4 && disjunction.Operation.Value is int && 1 == (int)disjunction.Operation.Value))
        this.constraints.Add(disjunction);

      //Now run through the instructions until we encounter an unconditional branch or we reach the end of the block.
      var successors = this.cdfg.SuccessorsFor(block);
      this.liveSuccessorBlocks.Clear();
      this.lastStatementWasUnconditionalTransfer = false;
      foreach (var instruction in block.Instructions) {
        Contract.Assume(instruction != null);
        this.lastStatementWasUnconditionalTransfer = false;
        this.Interpret(instruction, block);
        if (this.lastStatementWasUnconditionalTransfer) break;
      }
      if (!this.lastStatementWasUnconditionalTransfer) {
        var fallThroughBlock = block.FallThroughBlock as BasicBlock;
        if (fallThroughBlock != null) { //it might be null if this block is the very last one and it erroneously does not end on an unconditional branch.
          var i = successors.Find(fallThroughBlock);
          if (i >= 0) {
            this.liveSuccessorBlocks.Add(fallThroughBlock);
            if (this.constraints.Count > 0) {
              while (block.ConstraintsAtExit.Count <= i) block.ConstraintsAtExit.Add(new List<Instruction>(4));
              var constraintsAtExit = block.ConstraintsAtExit[i];
              Contract.Assume(constraintsAtExit != null);
              constraintsAtExit.Clear();
              constraintsAtExit.AddRange(this.constraints);
            }
          }
        }
      }

      //Now add all of the blocks that can actually be reached from this block to this.blocksToInterpet.
      //Note that this block might be its own successor. The code at the start of this method ensures that the resulting loop terminates.
      foreach (var successor in this.cdfg.SuccessorsFor(block)) {
        Contract.Assume(successor != null);
        if (this.liveSuccessorBlocks.Contains(successor)) {
          this.blocksToInterpret.Enqueue(successor);
        }
      }
    }

    private bool AllReadVariablesHaveTheSameDefiningExpressionsAsLastTime(BasicBlock block) {
      var result = true;
      foreach (INamedEntity variable in this.readVariables.GetValuesFor(block.Offset)) {
        var key = (uint)variable.Name.UniqueKey;
        var lastExpression = this.inputExpressions.Find(block.Offset, key);
        Contract.Assume(lastExpression != null);
        var currentExpression = this.mappings.GetDefiningExpressionFor(variable);
        Contract.Assume(currentExpression != null);
        if (lastExpression != currentExpression && !this.mappings.IsRecursive(lastExpression)) {
          this.inputExpressions.Add(block.Offset, key, currentExpression);
          result = false;
        }
      }
      return result;
    }

    private void SetupReadVariablesFor(BasicBlock block) {
      Contract.Requires(block != null);
      var writtenVariables = new SetOfUints();
      foreach (var instruction in block.Instructions) {
        var operation = instruction.Operation;
        switch (operation.OperationCode) {
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
          case OperationCode.Ldloc_S: {
              var variable = operation.Value as INamedEntity;
              if (variable == null) break; //happens when ldarg_0 refers to the this pointer of an instance method
              var key = (uint)variable.Name.UniqueKey;
              if (!writtenVariables.Contains(key)) {
                this.readVariables.Add(block.Offset, variable);
                var definingExpression = this.mappings.GetDefiningExpressionFor(variable);
                if (definingExpression == null) {
                  definingExpression = this.GetCanonicalizedLoadInstruction(operation.Value??Dummy.ParameterDefinition);
                  this.mappings.SetDefininingExpressionFor(variable, definingExpression);
                }
                this.inputExpressions.Add(block.Offset, key, definingExpression);
              }
              break;
            }
          case OperationCode.Ldarga:
          case OperationCode.Ldarga_S:
          case OperationCode.Ldloca:
          case OperationCode.Ldloca_S:
          case OperationCode.Starg:
          case OperationCode.Starg_S:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S: {
              var variable = operation.Value as INamedEntity;
              Contract.Assume(variable != null);
              writtenVariables.Add((uint)variable.Name.UniqueKey);
              break;
            }
        }
      }
    }

    /// <summary>
    /// Interprets the given instruction using abstract values and associates an abstract value with the instruction (if it computes a value).
    /// Also updates this.liveSuccessorBlocks if instruction is a branch that might be taken. Sets this.lastStatementWasUnconditionalTransfer to
    /// true if instruction is an unconditional branch (which can be a conditional branch whose condition is a compile time constant).
    /// </summary>
    [ContractVerification(false)]
    private void Interpret(Instruction instruction, BasicBlock block) {
      Contract.Requires(instruction != null);
      Contract.Requires(block != null);

      if (this.mappings.GetCompileTimeConstantValueFor(instruction) != null) return; //Already as simple as we can get.

      Contract.Assume(instruction.Operand1 is Instruction || instruction.Operand1 == null); //The type of the field is Microsoft.Cci.Analysis.Instruction.
      var operand1 = (Instruction)instruction.Operand1;
      if (operand1 == null)
        this.InterpretNullary(instruction);
      else {
        var operand2 = instruction.Operand2 as Instruction;
        if (operand2 != null) {
          this.InterpretBinary(instruction, operand1, operand2, block);
        } else {
          var operandArray = instruction.Operand2 as Instruction[];
          if (operandArray == null)
            this.InterpretUnary(instruction, operand1, block);
          else
            this.InterpretNary(instruction, operand1, operandArray);
        }
      }
    }

    /// <summary>
    /// Interprets an instruction with no operands, using abstract values from the SSA environment if appropriate.
    /// </summary>
    private void InterpretNullary(Instruction instruction) {
      Contract.Requires(instruction != null);

      var operation = instruction.Operation;
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
          this.mappings.SetCanonicalExpressionFor(instruction, this.expressionCanonicalizer.GetCanonicalExpression(instruction));
          break;

        //Instructions that transfer control to a successor block.
        case OperationCode.Br:
        case OperationCode.Br_S:
        case OperationCode.Leave:
        case OperationCode.Leave_S:
          Contract.Assume(operation.Value is uint); //This is an informally specified property of the Metadata model.
          var targetOffset = (uint)instruction.Operation.Value;
          var targetBlock = this.cdfg.BlockFor[targetOffset];
          this.liveSuccessorBlocks.Add(targetBlock);
          this.lastStatementWasUnconditionalTransfer = true;
          var i = this.cdfg.SuccessorsFor(this.currentBlock).Find(targetBlock);
          if (i >= 0) {
            while (this.currentBlock.ConstraintsAtExit.Count <= i) this.currentBlock.ConstraintsAtExit.Add(new List<Instruction>(4));
            var constraintsForTarget = this.currentBlock.ConstraintsAtExit[i];
            Contract.Assume(constraintsForTarget != null);
            constraintsForTarget.Clear();
            constraintsForTarget.AddRange(this.constraints);
          }
          break;

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
        case OperationCode.Ldc_R4:
        case OperationCode.Ldc_R8:
        case OperationCode.Ldnull:
        case OperationCode.Ldstr:
          var constval = Evaluator.GetAsCompileTimeConstantValue(instruction);
          this.mappings.SetCompileTimeConstantValueFor(instruction, constval);
          var constLoad = this.expressionCanonicalizer.GetAsCanonicalizedLoadConstant(constval, instruction);
          this.mappings.SetCanonicalExpressionFor(instruction, constLoad);
          this.mappings.SetCompileTimeConstantValueFor(constLoad, constval);
          break;

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
          var variable = operation.Value as INamedEntity;
          var constantValue = variable == null ? null : this.mappings.GetCompileTimeConstantValueFor(variable);
          if (constantValue != null) {
            this.mappings.SetCompileTimeConstantValueFor(instruction, constantValue);
            constLoad = this.expressionCanonicalizer.GetAsCanonicalizedLoadConstant(constantValue, instruction);
            this.mappings.SetCanonicalExpressionFor(instruction, constLoad);
            this.mappings.SetCompileTimeConstantValueFor(constLoad, constantValue);
          } else {
            var definingExpression = variable == null ? null : this.mappings.GetDefiningExpressionFor(variable);
            if (definingExpression != null)
              this.mappings.SetCanonicalExpressionFor(instruction, definingExpression);
            else {
              var canonicalExpr = this.GetCanonicalizedLoadInstruction(operation.Value??Dummy.ParameterDefinition);
              this.mappings.SetCanonicalExpressionFor(instruction, canonicalExpr);
            }
          }
          break;

        //Instructions that are side-effect free and that *could* result in compile time constant values.
        //We do NOT attempt to compute the compile time values at this time.
        case OperationCode.Ldsfld:
          //TODO: track and map this when not affected by a volatile modifier.
          break;

        case OperationCode.Call:
        case OperationCode.Endfinally:
        case OperationCode.Newobj:
        case OperationCode.Nop:
          break;

        //Instructions that transfer control out of the method being interpreted.
        case OperationCode.Jmp:
        case OperationCode.Rethrow:
        case OperationCode.Ret:
          this.lastStatementWasUnconditionalTransfer = true;
          break;

        //Instruction modifier to track in the future.
        case OperationCode.Volatile_:
          //TODO: track its occurrence and disable any CSE on the next field load. None happens right now.
          break;

        default:
          Contract.Assume(false);
          break;
      }
    }

    /// <summary>
    /// Interprets an instruction with a single operand (which was computed by a previous instruction), using values from the SSA environment.
    /// </summary>
    private void InterpretUnary(Instruction unaryInstruction, Instruction operand1, BasicBlock block) {
      Contract.Requires(unaryInstruction != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(block != null);

      IOperation operation = unaryInstruction.Operation;
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
        case OperationCode.Unbox:
        case OperationCode.Unbox_Any:
          break;

        //Insructions that are side effect free and whose results can be cached and reused, but whose result values can never be known at compile time.
        case OperationCode.Castclass:
        case OperationCode.Ckfinite:
        case OperationCode.Isinst:
        case OperationCode.Ldlen:
        case OperationCode.Ldvirtftn:
        case OperationCode.Refanytype:
        case OperationCode.Refanyval: //TODO: If we track object contents, we might be able to know the value of this at compile time.
        case OperationCode.Sizeof:
          this.mappings.SetCanonicalExpressionFor(unaryInstruction, this.expressionCanonicalizer.GetCanonicalExpression(unaryInstruction, operand1));
          break;

        //Instructions that conditionally affect control flow. We keep them as is, but update the control flow appropriately.
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
          operand1.Type = operand1.Type.PlatformType.SystemBoolean;
          var canonicalOperand1 = this.mappings.GetCanonicalExpressionFor(operand1);
          if (canonicalOperand1 != null) {
            var cv1 = this.TryToGetCompileTimeConstantValueFor(canonicalOperand1);
            if (cv1 != null) {
              if (MetadataExpressionHelper.IsIntegralNonzero(cv1)) break;
              this.lastStatementWasUnconditionalTransfer = true;
            } else {
              var result = this.mappings.CheckIfExpressionIsTrue(canonicalOperand1, block);
              if (result != null) {
                if (result.Value) break;
                this.lastStatementWasUnconditionalTransfer = true;
              }
            }
            canonicalOperand1 = this.ConvertUnionIntoConditionIfPossible(canonicalOperand1, block);
            canonicalOperand1 = this.expressionCanonicalizer.GetCanonicalExpression(new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Not }, Type = operand1.Type }, operand1);
          }
          goto addTargetToLiveSuccessorSet;
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          operand1.Type = operand1.Type.PlatformType.SystemBoolean;
          canonicalOperand1 = this.mappings.GetCanonicalExpressionFor(operand1);
          if (canonicalOperand1 != null) {
            var cv1 = this.TryToGetCompileTimeConstantValueFor(canonicalOperand1);
            if (cv1 != null) {
              if (MetadataExpressionHelper.IsIntegralZero(cv1)) break;
              this.lastStatementWasUnconditionalTransfer = true;
            } else {
              var result = this.mappings.CheckIfExpressionIsTrue(canonicalOperand1, block);
              if (result != null) {
                if (!result.Value) break;
                this.lastStatementWasUnconditionalTransfer = true;
              }
            }
            canonicalOperand1 = this.ConvertUnionIntoConditionIfPossible(canonicalOperand1, block);
          }
        addTargetToLiveSuccessorSet:
          Contract.Assume(unaryInstruction.Operation.Value is uint); //This is an informally specified property of the Metadata model.
          var targetOffset = (uint)unaryInstruction.Operation.Value;
          var targetBlock = this.cdfg.BlockFor[targetOffset];
          var i = this.cdfg.SuccessorsFor(this.currentBlock).Find(targetBlock);
          if (i >= 0) {
            while (this.currentBlock.ConstraintsAtExit.Count <= i) this.currentBlock.ConstraintsAtExit.Add(new List<Instruction>(4));
            var constraintsForTarget = this.currentBlock.ConstraintsAtExit[i];
            Contract.Assume(constraintsForTarget != null);
            constraintsForTarget.Clear();
            constraintsForTarget.AddRange(this.constraints);
            if (operand1.Type.TypeCode == PrimitiveTypeCode.Boolean)
              constraintsForTarget.Add(canonicalOperand1??operand1);
          }
          if (operand1.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            if (canonicalOperand1 != null) {
              var invertedBranchCondition = this.expressionCanonicalizer.GetCanonicalExpression(new Instruction() { Operation = new Operation { OperationCode = OperationCode.Not }, Type = operand1.Type }, canonicalOperand1);
              this.constraints.Add(invertedBranchCondition);
            } else {
              this.constraints.Add(new Instruction() { Operation = new Operation { OperationCode = OperationCode.Not }, Operand1 = operand1, Type = operand1.Type });
            }
          }
          this.liveSuccessorBlocks.Add(targetBlock);
          break;

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
        case OperationCode.Dup:
        case OperationCode.Neg:
        case OperationCode.Not:
          canonicalOperand1 = this.mappings.GetCanonicalExpressionFor(operand1);
          if (canonicalOperand1 != null) {
            //If the operand has been canonicalized (i.e. if it is side-effect free) we can canonicalize the unary expression and potentially constant fold it.
            var canonicalExpression = this.expressionCanonicalizer.GetCanonicalExpression(unaryInstruction, canonicalOperand1);
            this.mappings.SetCanonicalExpressionFor(unaryInstruction, canonicalExpression);
            var cv1 = this.TryToGetCompileTimeConstantValueFor(operand1);
            if (cv1 != null) {
              var cr = Evaluator.Evaluate(operation, cv1);
              if (cr != null) {
                this.mappings.SetCompileTimeConstantValueFor(unaryInstruction, cr);
                this.mappings.SetCompileTimeConstantValueFor(canonicalExpression, cr);
              }
            }
          }
          break;

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
          //TODO: track the values that pointers point to
          break;

        //Instructions that affect the SSA environment.
        case OperationCode.Starg:
        case OperationCode.Starg_S:
        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
          {
            var variable = operation.Value as INamedEntity;
            if (variable != null)
            {
              var cv1 = this.TryToGetCompileTimeConstantValueFor(operand1);
              if (cv1 != null)
              {
                var canon = this.GetCanonicalizedLoadInstruction(variable);
                canon = this.expressionCanonicalizer.GetAsCanonicalizedLoadConstant(cv1, canon);
                this.mappings.SetDefininingExpressionFor(variable, canon);
                this.mappings.SetCompileTimeConstantValueFor(variable, cv1);
                this.mappings.SetCompileTimeConstantValueFor(canon, cv1);
              }
              else
              {
                var canon = this.mappings.GetCanonicalExpressionFor(operand1);
                if (canon != null)
                {
                  var oldExpr = this.mappings.GetDefiningExpressionFor(variable);
                  if (oldExpr == null || !this.mappings.IsRecursive(oldExpr))
                  {
                    this.mappings.SetDefininingExpressionFor(variable, canon);
                    if (oldExpr != null && Evaluator.Contains(canon, oldExpr))
                    {
                      this.mappings.SetIsRecursive(canon);
                      if (this.constraints.Count > 0 && this.constraints[0] != null)
                      {
                        this.constraints[0] = Purger.Purge(this.constraints[0], variable, this.expressionCanonicalizer);
                      }
                    }
                  }
                }
              }
            }
          }
          break;

        //Instructions that transfer control out of the method being interpreted.
        case OperationCode.Ret:
        case OperationCode.Throw:
          this.lastStatementWasUnconditionalTransfer = true;
          break;

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
          {
            var variable = operation.Value as INamedEntity;
            var constantValue = variable == null ? null : this.mappings.GetCompileTimeConstantValueFor(variable);
            if (constantValue != null)
            {
              this.mappings.SetCompileTimeConstantValueFor(unaryInstruction, constantValue);
              var constLoad = this.expressionCanonicalizer.GetAsCanonicalizedLoadConstant(constantValue, unaryInstruction);
              this.mappings.SetCanonicalExpressionFor(unaryInstruction, constLoad);
              this.mappings.SetCompileTimeConstantValueFor(constLoad, constantValue);
            }
            else
            {
              var definingExpression = variable == null ? null : this.mappings.GetDefiningExpressionFor(variable);
              if (definingExpression != null)
                this.mappings.SetCanonicalExpressionFor(unaryInstruction, definingExpression);
              else
              {
                var canonicalExpr = this.GetCanonicalizedLoadInstruction(operation.Value ?? Dummy.ParameterDefinition);
                this.mappings.SetCanonicalExpressionFor(unaryInstruction, canonicalExpr);
              }
            }
          }
          break;

        default:
          Contract.Assume(false);
          break;
      }

    }

    private Instruction ConvertUnionIntoConditionIfPossible(Instruction expression, AiBasicBlock<Instruction> block) {
      Contract.Requires(expression != null);
      Contract.Requires(block != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      if (expression.Operation.OperationCode != OperationCode.Nop || !(expression.Operation.Value is INamedEntity)) return expression; //Not a union
      var operand1 = expression.Operand1 as Instruction;
      if (operand1 == null) return expression;
      var operand2 = expression.Operand2 as Instruction;
      if (operand2 == null) return expression;
      if (block.ConstraintsAtEntry.Count != 2) return expression;
      var cv1 = this.mappings.GetCompileTimeConstantValueFor(operand1);
      if (cv1 != null) {
        if (MetadataExpressionHelper.IsIntegralOne(cv1) && operand2.Type.TypeCode == PrimitiveTypeCode.Boolean) {
          var predecessorConstraints = block.ConstraintsAtEntry[0];
          Contract.Assume(predecessorConstraints != null);
          if (predecessorConstraints.Count == 0) return expression;
          var condition1 = predecessorConstraints[predecessorConstraints.Count-1];
          Contract.Assume(condition1 != null);
          var disjuntion = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Or }, Operand1 = condition1, Operand2 = operand2, Type = operand2.Type };
          return this.expressionCanonicalizer.GetCanonicalExpression(disjuntion, condition1, operand2);
        }
      } else {
        var cv2 = this.mappings.GetCompileTimeConstantValueFor(operand2);
        if (cv2 != null) {
          if (MetadataExpressionHelper.IsIntegralOne(cv2) && operand1.Type.TypeCode == PrimitiveTypeCode.Boolean) {
            var predecessorConstraints = block.ConstraintsAtEntry[1];
            Contract.Assume(predecessorConstraints != null);
            if (predecessorConstraints.Count == 0) return expression;
            var condition2 = predecessorConstraints[predecessorConstraints.Count-1];
            Contract.Assume(condition2 != null);
            var disjuntion = new Instruction() { Operation = new Operation() { OperationCode = OperationCode.Or }, Operand1 = condition2, Operand2 = operand1, Type = operand1.Type };
            return this.expressionCanonicalizer.GetCanonicalExpression(disjuntion, condition2, operand1);
          }
        }
      }
      return expression;
    }

    /// <summary>
    /// Interprets an instruction with two operands (which were computed by previous instructions), using values from the SSA environment.
    /// </summary>
    private Instruction InterpretBinary(Instruction binaryInstruction, Instruction operand1, Instruction operand2, AiBasicBlock<Instruction> block) {
      Contract.Requires(binaryInstruction != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operand2 != null);
      Contract.Requires(block != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      IOperation operation = binaryInstruction.Operation;
      switch (operation.OperationCode) {
        //Instructions that are side-effect free and cacheable and that could result in compile time values.
        //We attempt to compute the compile time values.
        case OperationCode.Add:
          goto case OperationCode.Add_Ovf_Un;
        case OperationCode.Add_Ovf:
          goto case OperationCode.Add_Ovf_Un;
        case OperationCode.Add_Ovf_Un:
        case OperationCode.And:
        case OperationCode.Ceq:
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
        case OperationCode.Div:
        case OperationCode.Div_Un:
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
        case OperationCode.Or:
        case OperationCode.Rem:
        case OperationCode.Rem_Un:
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Shr_Un:
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
        case OperationCode.Xor:
          var canonicalOperand1 = this.mappings.GetCanonicalExpressionFor(operand1);
          var canonicalOperand2 = this.mappings.GetCanonicalExpressionFor(operand2);
          if (canonicalOperand1 != null && canonicalOperand2 != null) {
            //Both operands are side effect free, so we can try to canonicalize the binary expression and perhaps constant fold it.
            var canonicalExpression = this.expressionCanonicalizer.GetCanonicalExpression(binaryInstruction, canonicalOperand1, canonicalOperand2);
            this.mappings.SetCanonicalExpressionFor(binaryInstruction, canonicalExpression);
            var cv1 = this.TryToGetCompileTimeConstantValueFor(operand1);
            if (cv1 != null) {
              var cv2 = this.TryToGetCompileTimeConstantValueFor(operand2);
              if (cv2 != null) {
                var cr = Evaluator.Evaluate(operation, cv1, cv2);
                if (cr != null) {
                  this.mappings.SetCompileTimeConstantValueFor(binaryInstruction, cr);
                  this.mappings.SetCompileTimeConstantValueFor(canonicalExpression, cr);
                  break;
                }
              } else {
                var cr = Evaluator.Evaluate(operation, cv1, operand2, this.mappings);
                if (cr != null) {
                  this.mappings.SetCompileTimeConstantValueFor(binaryInstruction, cr);
                  this.mappings.SetCompileTimeConstantValueFor(canonicalExpression, cr);
                  break;
                }
              }
            } else {
              var cv2 = this.TryToGetCompileTimeConstantValueFor(operand2);
              if (cv2 != null) {
                var cr = Evaluator.Evaluate(operation, operand1, cv2, this.mappings);
                if (cr != null) {
                  this.mappings.SetCompileTimeConstantValueFor(canonicalExpression, cr);
                  this.mappings.SetCompileTimeConstantValueFor(binaryInstruction, cr);
                  break;
                }
              } else {
                var cr = Evaluator.Evaluate(operation, operand1, operand2, this.mappings);
                if (cr != null) {
                  this.mappings.SetCompileTimeConstantValueFor(binaryInstruction, cr);
                  this.mappings.SetCompileTimeConstantValueFor(canonicalExpression, cr);
                  break;
                }
              }
            }
          }
          break;

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
          Contract.Assume(binaryInstruction.Operation.Value is uint); //This is an informally specified property of the Metadata model.
          var targetOffset = (uint)binaryInstruction.Operation.Value;
          var targetBlock = this.cdfg.BlockFor[targetOffset];
          canonicalOperand1 = this.mappings.GetCanonicalExpressionFor(operand1);
          canonicalOperand2 = this.mappings.GetCanonicalExpressionFor(operand2);
          Instruction branchCondition = binaryInstruction;
          if (canonicalOperand1 != null && canonicalOperand2 != null) {
            branchCondition = this.expressionCanonicalizer.GetCanonicalExpression(binaryInstruction, canonicalOperand1, canonicalOperand2);
            branchCondition.Type = binaryInstruction.Type.PlatformType.SystemBoolean;
            var cv1 = this.TryToGetCompileTimeConstantValueFor(operand1);
            if (cv1 != null) {
              var cv2 = this.TryToGetCompileTimeConstantValueFor(operand2);
              if (cv2 != null) {
                var cr = Evaluator.Evaluate(operation, cv1, cv2);
                if (cr != null) {
                  if (MetadataExpressionHelper.IsIntegralZero(cr)) {
                    //We now know this instruction does not affect control flow, so cache the result so that we don't consider this instruction again.
                    this.mappings.SetCompileTimeConstantValueFor(binaryInstruction, cr);
                    this.mappings.SetCompileTimeConstantValueFor(branchCondition, cr);
                    break;
                  }
                  this.lastStatementWasUnconditionalTransfer = true;
                }
              }
            }
          }
          if (!this.lastStatementWasUnconditionalTransfer) {
            var result = this.mappings.CheckIfExpressionIsTrue(branchCondition, block);
            if (result != null) {
              if (!result.Value) break;
              this.lastStatementWasUnconditionalTransfer = true;
            }
          }
          var i = this.cdfg.SuccessorsFor(this.currentBlock).Find(targetBlock);
          if (i >= 0) {
            while (this.currentBlock.ConstraintsAtExit.Count <= i) this.currentBlock.ConstraintsAtExit.Add(new List<Instruction>(4));
            var constraintsForTarget = this.currentBlock.ConstraintsAtExit[i];
            Contract.Assume(constraintsForTarget != null);
            constraintsForTarget.Clear();
            constraintsForTarget.AddRange(this.constraints);
            constraintsForTarget.Add(branchCondition);
          }
          var invertedBranchCondition = this.expressionCanonicalizer.GetCanonicalExpression(
            new Instruction() { Operation = new Operation { OperationCode = OperationCode.Not }, Type = binaryInstruction.Type }, branchCondition);
          this.constraints.Add(invertedBranchCondition);
          this.liveSuccessorBlocks.Add(targetBlock);
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
          break;

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
          //if (this.expressionCanonicalizer.HasCachedEntryFor(operand1) && this.expressionCanonicalizer.HasCachedEntryFor(operand2)) {
          //  this.mappings.SetCanonicalExpressionFor(binaryInstruction, this.expressionCanonicalizer.GetCanonicalExpression(binaryInstruction, operand1, operand2));
          //}
          break;

        default:
          Contract.Assume(false);
          break;
      }

      return binaryInstruction;
    }

    /// <summary>
    /// Interprets an instruction with three or more operands.
    /// </summary>
    private Instruction InterpretNary(Instruction naryInstruction, Instruction operand1, Instruction[] operands2toN) {
      Contract.Requires(naryInstruction != null);
      Contract.Requires(operand1 != null);
      Contract.Requires(operands2toN != null);

      IOperation operation = naryInstruction.Operation;
      switch (operation.OperationCode) {
        //Instructions that cause or depend on side-effects. We'll keep them as is.
        case OperationCode.Array_Addr:
        case OperationCode.Array_Create:
        case OperationCode.Array_Create_WithLowerBound:
        case OperationCode.Array_Get:
        case OperationCode.Array_Set:
        case OperationCode.Call: //TODO: traverse into calls, if possible, but just one level deep. Also parameterize with a contract provider.
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Newobj:
        case OperationCode.Stelem:
        case OperationCode.Stelem_I:
        case OperationCode.Stelem_I1:
        case OperationCode.Stelem_I2:
        case OperationCode.Stelem_I4:
        case OperationCode.Stelem_I8:
        case OperationCode.Stelem_R4:
        case OperationCode.Stelem_R8:
        case OperationCode.Stelem_Ref:
          //TODO: update the environment to track the element values.
          break;

        default:
          Contract.Assume(false);
          break;
      }

      return naryInstruction;
    }

    /// <summary>
    /// If the value that the given expression evaluates to at runtime is known at compile time, return that value as an IMetadataConstant instance.
    /// If it is known at compile time that the expression will fail at runtime, or if its runtime value is not known at compile time, the result of this method is null.
    /// </summary>
    /// <param name="expression">An instruction that results in a value at runtime.</param>
    private IMetadataConstant/*?*/ TryToGetCompileTimeConstantValueFor(Instruction expression) {
      Contract.Requires(expression != null);

      var cc = this.mappings.GetCompileTimeConstantValueFor(expression);
      if (cc == Dummy.Constant) return null; //Dummy.Constant signals that the expression is known to fail at runtime.
      if (cc == null) {
        var canonicalExpression = this.expressionCanonicalizer.GetCanonicalExpression(expression);
        if (canonicalExpression != expression) {
          cc = this.mappings.GetCompileTimeConstantValueFor(canonicalExpression);
          if (cc == Dummy.Constant) return null;
        }
      }
      return cc;
    }

    /// <summary>
    /// Returns a canonicalized expression with a Dummy operation value and a set of operands that
    /// represent all of the values that may be assigned to this variable by preceding instructions.
    /// </summary>
    /// <remarks>
    /// When canonicalizing expressions that contain unions as subexpressions, 
    /// distribute over unions so that the union is always the outer expression of the canonical expression.
    /// For example i + union(j, k) becomes union(i+j, i+k) 
    /// and union (i, j) + union (j, k) becomes union(i+j, i+k, j+j, j+k).
    /// </remarks>
    [ContractVerification(false)]
    private Instruction UnionOfJoinedValues(Join join, BasicBlock block) {
      Contract.Requires(join != null);
      Contract.Requires(block != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var predecessors = this.cfgQueries.PredeccessorsFor(block);
      var n = predecessors.Count;
      var joinOperation = new Operation() { Value = join.NewLocal };
      var result = new Instruction() { Operation = joinOperation, Type = join.Type };
      if (n > 0) {
        result.Operand1 = this.GetDefininingExpressionFor(join, predecessors[0], block);
        if (n > 1) {
          result.Operand2 = this.GetDefininingExpressionFor(join, predecessors[1], block);
          if (n > 2) {
            var operands2ToN = new Instruction[n-1];
            operands2ToN[0] = (Instruction)result.Operand2;
            result.Operand2 = operands2ToN;
            for (int i = 1; i < n-1; i++) {
              operands2ToN[i] = this.GetDefininingExpressionFor(join, predecessors[i+1], block);
            }
          }
        }
      }
      return this.expressionCanonicalizer.GetCanonicalExpression(result, (Instruction)result.Operand1, result.Operand2 as Instruction, result.Operand2 as Instruction[]);
    }

    private Instruction GetDefininingExpressionFor(Join join, AiBasicBlock<Instruction> predecessor, BasicBlock blockDefiningTheJoin) {
      Contract.Requires(join != null);
      Contract.Requires(predecessor != null);

      var ssaName = join.Join1;
      if (join.Block1 != predecessor) {
        ssaName = join.Join2;
        if (join.Block2 != predecessor) {
          ssaName = null;
          if (join.OtherBlocks != null) {
            for (int i = 0, n = join.OtherBlocks.Count; i < n; i++) {
              if (join.OtherBlocks[i] == predecessor) {
                Contract.Assume(join.OtherJoins != null && join.OtherJoins.Count == n);
                ssaName = join.OtherJoins[i];
                break;
              }
            }
          }
        }
      }
      if (ssaName == null) {
        Contract.Assume(join.OriginalLocal is INamedEntity);
        ssaName = (INamedEntity)join.OriginalLocal;
      }
      var result = this.mappings.GetDefiningExpressionFor(ssaName);
      if (result == null)
        result = this.GetCanonicalizedLoadInstruction(ssaName);
      return result;
    }

    /// <summary>
    /// Given a local or a parameter, return a canonicalized expression that will load the value of the local or parameter at runtime.
    /// </summary>
    private Instruction GetCanonicalizedLoadInstruction(object localOrParameter) {
      Contract.Requires(localOrParameter != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      OperationCode operationCode;
      ITypeReference type = null;
      var local = localOrParameter as ILocalDefinition;
      if (local != null) {
        type = local.Type; operationCode = OperationCode.Ldloc;
      } else {
        Contract.Assume(localOrParameter is IParameterDefinition);
        var parameter = (IParameterDefinition)localOrParameter;
        type = parameter.Type;
        if (type is Dummy) {
          Contract.Assume(parameter == Dummy.ParameterDefinition); //Should be the this argument.
          type = this.cdfg.MethodBody.MethodDefinition.ContainingTypeDefinition;
          localOrParameter = null;
          operationCode = OperationCode.Ldarg_0;
        } else {
          operationCode = OperationCode.Ldarg;
        }
      }
      var loadVar = new Operation() { OperationCode = operationCode, Value = localOrParameter };
      var result = new Instruction() { Operation = loadVar, Type = type };
      return this.expressionCanonicalizer.GetCanonicalExpression(result);
    }


  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="Instruction"></typeparam>
  public class AiBasicBlock<Instruction> : Microsoft.Cci.Analysis.SSABasicBlock<Instruction>
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// The block, if any, that follows this block in the sequence of instructions. It may not be reachable from the block.
    /// If this block is the last block, the value will be null.
    /// </summary>
    public AiBasicBlock<Instruction>/*?*/ FallThroughBlock;

    /// <summary>
    /// A non null (but delay initialized) list of the blocks that transfer control to this block.
    /// </summary>
    public ISimpleReadonlyList<AiBasicBlock<Instruction>> Predecessors;

    /// <summary>
    /// A table keeping track of Interval values that have been computed for instructions in the context
    /// of this block. (An expression can result in a different interval or constnat in this block because this
    /// block might have entry constraints that narrow the interval.)
    /// </summary>
    internal Hashtable<Instruction, object> ConstantForExpression {
      get {
        Contract.Ensures(Contract.Result<Hashtable<Instruction, object>>() != null);
        if (this.constantForExpression == null)
          this.constantForExpression = new Hashtable<Instruction, object>();
        return this.constantForExpression;
      }
    }
    Hashtable<Instruction, object>/*?*/ constantForExpression;

    /// <summary>
    /// A list of lists where every element list is a list of constraints that were established by one of the predecessors to this block.
    /// </summary>
    public List<List<Instruction>> ConstraintsAtEntry {
      get {
        Contract.Ensures(Contract.Result<List<List<Instruction>>>() != null);
        if (this.constraintsAtEntry == null)
          this.constraintsAtEntry = new List<List<Instruction>>(4);
        return this.constraintsAtEntry;
      }
    }
    private List<List<Instruction>>/*?*/ constraintsAtEntry;

    /// <summary>
    /// A list of lists where every element list is a list of constraints that hold when transfer controls to a successor block.
    /// The indices of the outer list match the indices of the Successor list.
    /// </summary>
    public List<List<Instruction>> ConstraintsAtExit {
      get {
        Contract.Ensures(Contract.Result<List<List<Instruction>>>() != null);
        if (this.constraintsAtExit == null)
          this.constraintsAtExit = new List<List<Instruction>>(4);
        return constraintsAtExit;
      }
    }
    private List<List<Instruction>>/*?*/ constraintsAtExit;

    /// <summary>
    /// A table keeping track of Interval values that have been computed for instructions in the context
    /// of this block. (An expression can result in a different interval or constnat in this block because this
    /// block might have entry constraints that narrow the interval.)
    /// </summary>
    internal Hashtable<Instruction, Interval> IntervalForExpression {
      get {
        Contract.Ensures(Contract.Result<Hashtable<Instruction, Interval>>() != null);
        if (this.intervalForExpression == null)
          this.intervalForExpression = new Hashtable<Instruction, Interval>();
        return this.intervalForExpression;
      }
    }
    Hashtable<Instruction, Interval>/*?*/ intervalForExpression;

    /// <summary>
    /// A context in which to keep the constraints at entry in SAT solver format, so that they can be used
    /// together with particular expressions to determine satisfiablity.
    /// </summary>
    internal ISatSolverContext/*?*/ SatSolverContext {
      get {
        return this.satSolverContext;
      }
      set {
        this.satSolverContext = value;
      }
    }
    ISatSolverContext/*?*/ satSolverContext;
  }

}