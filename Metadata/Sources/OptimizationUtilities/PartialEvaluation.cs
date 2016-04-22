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
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.Optimization {

  /// <summary>
  /// 
  /// </summary>
  public class PartialEvaluator {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="localScopeProvider"></param>
    /// <param name="sourceLocationProvider"></param>
    public PartialEvaluator(IMetadataHost host, ILocalScopeProvider/*?*/ localScopeProvider, ISourceLocationProvider/*?*/ sourceLocationProvider) {
      Contract.Requires(host != null);
      this.host = host;
      this.localScopeProvider = localScopeProvider;
      this.sourceLocationProvider = sourceLocationProvider;
      this.copier = new MetadataShallowCopier(host);
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.labelFor != null);
      Contract.Invariant(copier != null);
    }

    IMetadataHost host;
    ILocalScopeProvider/*?*/ localScopeProvider;
    ISourceLocationProvider/*?*/ sourceLocationProvider;
    MetadataShallowCopier copier;
    Hashtable<ILGeneratorLabel> labelFor = new Hashtable<ILGeneratorLabel>();

    private ControlAndDataFlowGraph<PeBasicBlock<PeInstruction>, PeInstruction> Cdfg {
      get {
        Contract.Ensures(Contract.Result<ControlAndDataFlowGraph<PeBasicBlock<PeInstruction>, PeInstruction>>() != null);
        Contract.Assume(this.cdfg != null);
        return this.cdfg; 
      }
    }
    ControlAndDataFlowGraph<PeBasicBlock<PeInstruction>, PeInstruction> cdfg;

    private ControlGraphQueries<PeBasicBlock<PeInstruction>, PeInstruction> CfgQueries {
      get {
        Contract.Ensures(Contract.Result<ControlGraphQueries<PeBasicBlock<PeInstruction>, PeInstruction>>() != null);
        Contract.Assume(this.cfgQueries != null);
        return this.cfgQueries;
      }
    }
    ControlGraphQueries<PeBasicBlock<PeInstruction>, PeInstruction> cfgQueries;

    private ValueMappings<PeInstruction> ValueMappings {
      get {
        Contract.Ensures(Contract.Result<ValueMappings<PeInstruction>>() != null);
        Contract.Assume(this.valueMappings != null);
        return this.valueMappings;
      }
    }
    ValueMappings<PeInstruction> valueMappings;

    /// <summary>
    /// Given the body of method, and optionally the actual values of some of the arguments of the method, partially evaluate the body and return
    /// a new body that has been specialized with respect to the partial input. It is to be expected that the resulting body will execute faster
    /// than the original, because some computation is likely to be removed by the partial evaluator. In some cases, this will even be true when
    /// no actual input values are supplied because partial evaluator will discover and remove any redundant code in the given method body.
    /// </summary>
    /// <param name="methodBody">The method body to partially evaluate.</param>
    /// <param name="arguments">The actual values of some of the parameters of the method. Unknown values are reprsented by CodeDummy.Constant.</param>
    /// <returns></returns>
    public IMethodBody PartiallyEvaluate(IMethodBody methodBody, params IMetadataConstant[] arguments) {
      Contract.Requires(methodBody != null);
      Contract.Requires(arguments != null);

      //First analyze the method body to find out what is known at compile time about the values of variables and expressions.
      this.cdfg = ControlAndDataFlowGraph<PeBasicBlock<PeInstruction>, PeInstruction>.GetControlAndDataFlowGraphFor(host, methodBody, this.localScopeProvider);
      this.cfgQueries = new ControlGraphQueries<PeBasicBlock<PeInstruction>, PeInstruction>(this.cdfg);
      SingleAssigner<PeBasicBlock<PeInstruction>, PeInstruction>.GetInSingleAssignmentForm(host.NameTable, this.cdfg, this.cfgQueries, this.sourceLocationProvider);
      this.valueMappings = new ValueMappings<PeInstruction>(this.host.PlatformType);
      this.SeedValueMappingsWithPartialInput(methodBody.MethodDefinition, arguments);
      AbstractInterpreter<PeBasicBlock<PeInstruction>, PeInstruction>.InterpretUsingAbstractValues(this.Cdfg, this.CfgQueries, this.ValueMappings);

      //TODO: if it turns out that a local or parameter can be aliased in a method body because its address was taken and then used
      //in the body itself instead of just passed to a method call, then report an error and give up.

      this.GetSourceLocations();

      //First rewrite the graph to eliminate instructions that push things on to the stack or store things into temporary variables.
      //After this rewrite, only "sinks" (writes to memory, method calls, conditional branches, etc.) will remain and they will
      //use operands that are canonicalized full expressions. Canonicalized expressions already have had constant folding done on them.
      //Note that this step introduces redundancy. The next step removes it again, along with any other redundancy.
      this.NormalizeExpressions();

      //TODO: loop fusion

      //TODO: loop unrolling

      //Now eliminate redundant computation. 
      this.FactorOutCommonSubexpressions();
      //TODO: eliminate store load sequences where the local is not used again after the load
      this.RemoveUselessBranches(); //Also fixes up any branches that need to go via transfer blocks.

      //Undo the SSA
      var ssaUndoer = new MultipleAssigner<PeBasicBlock<PeInstruction>, PeInstruction>(this.host, this.Cdfg, this.CfgQueries);
      ssaUndoer.ReuseDeadLocals();

      //Now serialize the mutated graph into a new method and return that.
      return this.GenerateNewBody();
    }

    private void GetSourceLocations() {
      var provider = this.sourceLocationProvider;
      if (provider == null) return;

      foreach (var block in this.Cdfg.AllBlocks) {
        Contract.Assume(block != null);
        for (int i = 0, n = block.Instructions.Count; i < n; i++) {
          var instruction = block.Instructions[i];

          foreach (var sloc in provider.GetPrimarySourceLocationsFor(instruction.Operation.Location)) {
            instruction.sourceLocation = sloc;
            break;
          }
        }
      }
    }

    private void NormalizeExpressions() {
      //TODO: rather follow the control flow graph so that dead code is not visited.
      foreach (var block in this.Cdfg.AllBlocks) {
        Contract.Assume(block != null);
        this.NormalizeExpressions(block);
      }
    }

    [ContractVerification(false)]
    private void NormalizeExpressions(PeBasicBlock<PeInstruction> block) {
      Contract.Requires(block != null);

      for (int i = 0, n = block.Instructions.Count; i < n; i++) {
        var instruction = block.Instructions[i];
        Contract.Assume(instruction != null);
        //TODO: if any of the operands of the instruction is a value that is not computed in this basic block,
        //but which is expected to be on the operand stack when the block is entered, then keep
        //the instruction without any modification.
        //this.lastInstruction = instruction;
        var opCode = instruction.Operation.OperationCode;
        switch (opCode) {
          case OperationCode.Br:
          case OperationCode.Br_S:
          case OperationCode.Leave:
          case OperationCode.Leave_S:
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
            this.NormalizeExpressions(block, instruction);
            break;

          case OperationCode.Brfalse:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue:
          case OperationCode.Brtrue_S:
            this.NormalizeExpressions(block, instruction);
            block.Instructions[i] = this.TryToUseBinaryBranch(instruction, opCode == OperationCode.Brfalse_S || opCode == OperationCode.Brtrue_S);
            break;

          case OperationCode.Array_Create:
          case OperationCode.Array_Create_WithLowerBound:
          case OperationCode.Array_Set:
          case OperationCode.Call:
          case OperationCode.Calli:
          case OperationCode.Callvirt:
          case OperationCode.Cpblk:
          case OperationCode.Cpobj:
          case OperationCode.Endfilter:
          case OperationCode.Endfinally:
          case OperationCode.Initblk:
          case OperationCode.Initobj:
          case OperationCode.Jmp:
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
          case OperationCode.Ldlen:
          case OperationCode.Ldloca:
          case OperationCode.Ldloca_S:
          case OperationCode.Ldvirtftn:
          case OperationCode.Newobj:
          case OperationCode.Ret:
          case OperationCode.Stelem:
          case OperationCode.Stelem_I:
          case OperationCode.Stelem_I1:
          case OperationCode.Stelem_I2:
          case OperationCode.Stelem_I4:
          case OperationCode.Stelem_I8:
          case OperationCode.Stelem_R4:
          case OperationCode.Stelem_R8:
          case OperationCode.Stelem_Ref:
          case OperationCode.Stfld:
          case OperationCode.Stind_I:
          case OperationCode.Stind_I1:
          case OperationCode.Stind_I2:
          case OperationCode.Stind_I4:
          case OperationCode.Stind_I8:
          case OperationCode.Stind_R4:
          case OperationCode.Stind_R8:
          case OperationCode.Stind_Ref:
          case OperationCode.Stsfld:
          case OperationCode.Switch:
          case OperationCode.Throw:
            this.NormalizeExpressions(block, instruction);
            if (instruction.Type.TypeCode != PrimitiveTypeCode.Void) {
              instruction.MustBeCachedInTemporary = true;
              instruction.LeaveResultOnStack = false;
            }
            break;

          case OperationCode.Pop:
            this.NormalizeExpressions(block, instruction);
            break;

          case OperationCode.Starg:
          case OperationCode.Starg_S:
          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            Contract.Assume(instruction.Operand1 != null);
            var canonExpr = this.ValueMappings.GetCanonicalExpressionFor(instruction.Operand1);
            if (canonExpr != null) goto default; //This instruction will be ignored entirely and the local may go away in the resulting body.
            break;

          default:
            //Replace the instruction with a new instruction that will be skipped.
            //The instruction itself is an operand for another instruction and will be emitted as part of emitting its containing instruction.
            block.Instructions[i] = new PeInstruction() { Operation = new Operation() { Offset = instruction.Operation.Offset },
              OmitInstruction = true, sourceLocation = instruction.sourceLocation };
            break;
        }
      }
    }

    private PeInstruction TryToUseBinaryBranch(PeInstruction instruction, bool shortBranch) {
      Contract.Requires(instruction != null);
      Contract.Ensures(Contract.Result<Instruction>() != null);

      var operand1 = instruction.Operand1;
      Contract.Assume(operand1 != null);
      var opCode = instruction.Operation.OperationCode;
      OperationCode newOpCode = opCode;
      switch (operand1.Operation.OperationCode) {
        case OperationCode.Ceq: newOpCode = shortBranch ? OperationCode.Beq_S : OperationCode.Beq; break;
        case OperationCode.Cgt: newOpCode = shortBranch ? OperationCode.Bgt_S : OperationCode.Bgt; break;
        case OperationCode.Cgt_Un: newOpCode = shortBranch ? OperationCode.Bgt_Un_S : OperationCode.Bgt_Un; break;
        case OperationCode.Clt: newOpCode = shortBranch ? OperationCode.Blt_S : OperationCode.Blt; break;
        case OperationCode.Clt_Un: newOpCode = shortBranch ? OperationCode.Blt_Un_S : OperationCode.Blt_Un_S; break;
      }
      if (newOpCode == instruction.Operation.OperationCode) return instruction;
      if (operand1.Operation.OperationCode == OperationCode.Ceq) {
        Contract.Assume(operand1.Operand1 != null);
        var cv1 = this.ValueMappings.GetCompileTimeConstantValueFor(operand1.Operand1);
        Contract.Assume(operand1.Operand2 is PeInstruction);
        if (cv1 != null && MetadataExpressionHelper.IsIntegralZero(cv1))
          return TryToUseInvertedBinaryBranch(instruction, (PeInstruction)operand1.Operand2, shortBranch);
        var cv2 = this.ValueMappings.GetCompileTimeConstantValueFor((PeInstruction)operand1.Operand2);
        if (cv2 != null && MetadataExpressionHelper.IsIntegralZero(cv2))
          return TryToUseInvertedBinaryBranch(instruction, (PeInstruction)operand1.Operand1, shortBranch);
      }
      var newOp = this.copier.Copy(instruction.Operation);
      newOp.OperationCode = newOpCode;
      instruction.Operation = newOp;
      instruction.Operand1 = operand1.Operand1;
      instruction.Operand2 = operand1.Operand2;
      return instruction;
    }

    private PeInstruction TryToUseInvertedBinaryBranch(PeInstruction instruction, PeInstruction operand, bool shortBranch) {
      Contract.Requires(instruction != null);
      Contract.Requires(operand != null);

      Contract.Assume(operand.Operand1 != null);
      var comparisonOperandType = operand.Operand1.Type;
      var floatComparison = comparisonOperandType.TypeCode == PrimitiveTypeCode.Float32 || comparisonOperandType.TypeCode == PrimitiveTypeCode.Float64;
      var opCode = instruction.Operation.OperationCode;
      switch (operand.Operation.OperationCode) {
        case OperationCode.Ceq: 
          opCode = shortBranch ? OperationCode.Bne_Un_S : OperationCode.Bne_Un; 
          break;
        case OperationCode.Cgt: 
          if (floatComparison)
            opCode = shortBranch ? OperationCode.Blt_Un_S : OperationCode.Bgt_Un; 
          else
            opCode = shortBranch ? OperationCode.Blt_S : OperationCode.Blt; 
          break;
        case OperationCode.Cgt_Un:
          if (floatComparison)
            opCode = shortBranch ? OperationCode.Blt_S : OperationCode.Blt;
          else
            opCode = shortBranch ? OperationCode.Blt_Un_S : OperationCode.Bgt_Un; 
          break;
        case OperationCode.Clt:
          if (floatComparison)
            opCode = shortBranch ? OperationCode.Bgt_Un_S : OperationCode.Bgt_Un;
          else
            opCode = shortBranch ? OperationCode.Bgt_S : OperationCode.Bgt; 
          break;
        case OperationCode.Clt_Un:
          if (floatComparison)
            opCode = shortBranch ? OperationCode.Bgt_S : OperationCode.Bgt;
          else
            opCode = shortBranch ? OperationCode.Bgt_Un_S : OperationCode.Bgt_Un;
          break;
      }
      if (opCode == instruction.Operation.OperationCode) return instruction;
      var newOp = this.copier.Copy(instruction.Operation);
      newOp.OperationCode = opCode;
      instruction.Operation = newOp;
      instruction.Operand1 = operand.Operand1;
      instruction.Operand2 = operand.Operand2;
      return instruction;

    }

    private bool WorthCaching(PeInstruction instruction) {
      Contract.Requires(instruction != null);

      switch (instruction.Operation.OperationCode) {
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
          return false;
      }
      return true;
    }

    private void NormalizeExpressions(PeBasicBlock<PeInstruction> block, PeInstruction instruction) {
      Contract.Requires(block != null);
      Contract.Requires(instruction != null);

      var operand1 = instruction.Operand1;
      if (operand1 == null) return;
      var canonExpr = this.ValueMappings.GetCanonicalExpressionFor(operand1);
      if (canonExpr != null && WorthCaching(canonExpr)) {
        if (operand1 != canonExpr) {
          operand1.OmitInstruction = true;
          if (canonExpr.sourceLocation == null) canonExpr.sourceLocation = operand1.sourceLocation;
        }
        canonExpr.MustBeCachedInTemporary = true;
        canonExpr.LeaveResultOnStack = true;
        instruction.Operand1 = canonExpr;
      }
      var operand2 = instruction.Operand2 as PeInstruction;
      if (operand2 != null) {
        canonExpr = this.ValueMappings.GetCanonicalExpressionFor(operand2);
        if (canonExpr != null && WorthCaching(canonExpr)) {
          if (operand2 != canonExpr) {
            operand2.OmitInstruction = true;
            if (canonExpr.sourceLocation == null) canonExpr.sourceLocation = operand2.sourceLocation;
          }
          canonExpr.MustBeCachedInTemporary = true;
          canonExpr.LeaveResultOnStack = true;
          instruction.Operand2 = canonExpr;
        }
      } else {
        var operands2toN = instruction.Operand2 as PeInstruction[];
        if (operands2toN != null) {
          for (int i = 0, n = operands2toN.Length; i < n; i++) {
            var operandi = operands2toN[i];
            Contract.Assume(operandi != null);
            canonExpr = this.ValueMappings.GetCanonicalExpressionFor(operandi);
            if (canonExpr != null && WorthCaching(canonExpr)) {
              if (operandi != canonExpr) {
                operandi.OmitInstruction = true;
                if (canonExpr.sourceLocation == null) canonExpr.sourceLocation = operandi.sourceLocation;
              }
              canonExpr.MustBeCachedInTemporary = true;
              canonExpr.LeaveResultOnStack = true;
              operands2toN[i] = canonExpr;
            }
          }
        }
      }
    }

    //If any sink uses a subexpression that was computed by an earlier sink,
    //try not to repeat that computation, but store the result of the subexpression in a temporary variable.
    //If the common subexpression is not computed on all paths leading to the sink, then introduce new blocks
    //that compute the common subexpressions when control flows via those paths.
    private void FactorOutCommonSubexpressions() {
      var postOrder = this.CfgQueries.BlocksInPostorder;
      var n = postOrder.Length;
      for (int i = n-1; i >= 0; i--) { //traverse in reverse post order so that a block visited before any of its successors (other than itself).
        var block = postOrder[i];
        Contract.Assume(block != null);
        this.FactorOutCommonSubexpressions(block);
      }
    }

    private void FactorOutCommonSubexpressions(PeBasicBlock<PeInstruction> block) {
      Contract.Requires(block != null);

      var instructions = block.Instructions;
      var n = instructions.Count;
      for (int i = 0; i < n; i++) {
        var instruction = instructions[i];
        if (instruction.OmitInstruction) continue;
        this.FactorOutCommonSubexpressions(block, instruction, instruction);
      }
    }

    private void FactorOutCommonSubexpressions(PeBasicBlock<PeInstruction> block, PeInstruction instruction, PeInstruction sink) {
      Contract.Requires(block != null);
      Contract.Requires(instruction != null);
      Contract.Requires(sink != null);

      if (instruction.Operand1 == null) return;

      if (instruction != sink) {
        //Then this instruction computes a value. That value may already live in a temporary variable computed by a previous instruction.
        var temp = instruction.temporaryForResult;
        if (temp != null) {
          //Perhaps block or one of its dominators has already defined temp. (Life is oh so simple if that is the case.)
          if (BlockOrDominatorDefines(block, temp)) return;

          //If one or more of our predecessors defines temp, we can save work by not defining it on control paths from those predecessors.
          var predecessors = this.CfgQueries.PredeccessorsFor(block);
          bool predecessorDefinedTemp = false;
          foreach (var predecessor in predecessors) {
            Contract.Assume(predecessor != null);
            if (BlockOrPredecessorDefines(predecessor, new SetOfObjects(), temp)) {
              predecessorDefinedTemp = true;
              break;
            }
          }
          if (predecessorDefinedTemp) {
            for (int i = 0, n = predecessors.Count; i < n; i++) {
              var predecessor = predecessors[i];
              if (predecessor.definedTemporaries != null && predecessor.definedTemporaries.Contains(temp)) continue;
              this.EnsureTempIsDefinedBeforeControlReachesBlockFrom(temp, instruction, block, predecessor);
            }
            return;
          }
        } else if (instruction.MustBeCachedInTemporary) {
          //sink is the unlucky first instruction that has to do the caching.
          temp = new GeneratorLocal();
          temp.Type = instruction.Type;
          instruction.temporaryForResult = temp;
          if (block.definedTemporaries == null) block.definedTemporaries = new SetOfObjects();
          block.definedTemporaries.Add(temp);
        }
      }

      //We are going to do some work for this occurrence of instruction, but perhaps we can get lucky with some of its subexpressions, if any.
      var operand1 = instruction.Operand1 as PeInstruction;
      if (operand1 != null) {
        this.FactorOutCommonSubexpressions(block, operand1, sink);
        var operand2 = instruction.Operand2 as PeInstruction;
        if (operand2 != null) {
          this.FactorOutCommonSubexpressions(block, operand2, sink);
        } else {
          var operands2toN = instruction.Operand2 as PeInstruction[];
          if (operands2toN != null) {
            for (int i = 0, n = operands2toN.Length; i < n; i++) {
              Contract.Assume(operands2toN[i] != null);
              this.FactorOutCommonSubexpressions(block, operands2toN[i], sink);
            }
          }
        }
      }      
    }

    private void EnsureTempIsDefinedBeforeControlReachesBlockFrom(GeneratorLocal temp, PeInstruction instruction, PeBasicBlock<PeInstruction> block, PeBasicBlock<PeInstruction> predecessor) {
      Contract.Requires(temp != null);
      Contract.Requires(instruction != null);
      Contract.Requires(block != null);
      Contract.Requires(predecessor != null);

      //If one or more of predecessor's predecessors defines temp, we can save work by not defining it on control paths from those predecessors.
      //TODO: But.... if block is not the only successor to predecessor and predecessor has a predecessor that does not define temp
      //then doing this recursive delegation may result in more work done on paths that do not flow to block.
      //Either avoid this altogether by not recursing in such cases, or minimize the impact by looking at path profiles and doing it 
      //only if the path to block is much hotter than the other path.
      var predecessorPedecessors = this.CfgQueries.PredeccessorsFor(predecessor);
      bool predecessorDefinedTemp = false;
      foreach (var predecessorPredecessor in predecessorPedecessors) {
        Contract.Assume(predecessorPredecessor != null);
        if (BlockOrDominatorDefines(predecessorPredecessor, temp)) {
          predecessorDefinedTemp = true;
          break;
        }
      }
      if (predecessorDefinedTemp) {
        for (int j = 0, m = predecessorPedecessors.Count; j < m; j++) {
          var predecessorPredecessor = predecessorPedecessors[j];
          if (predecessorPredecessor.definedTemporaries != null && predecessorPredecessor.definedTemporaries.Contains(temp)) continue;
          this.EnsureTempIsDefinedBeforeControlReachesBlockFrom(temp, instruction, predecessor, predecessorPredecessor);
        }
        return;
      }

      //Now add an instruction to initialize temp.
      var successors = this.Cdfg.SuccessorsFor(predecessor);
      var n = successors.Count;
      var transferBlocks = predecessor.transferBlocks;
      if (transferBlocks == null) predecessor.transferBlocks = transferBlocks = new PeBasicBlock<PeInstruction>[n];
      Contract.Assume(n == transferBlocks.Length);
      int i = 0;
      for (; i < n; i++) if (successors[i] == block) break;
      Contract.Assume(i < n);
      var transferBlock = transferBlocks[i];
      if (transferBlock == null) {
        transferBlock = transferBlocks[i] = new PeBasicBlock<PeInstruction>();
        var masterList = new List<PeInstruction>(1);
        masterList.Add(new PeInstruction() { Operation = new Operation() { Offset = uint.MaxValue-block.Offset } });
        transferBlock.Instructions = new Sublist<PeInstruction>(masterList, 0, 1);
        if (predecessor.FallThroughBlock == block) transferBlock.FallThroughBlock = block;
      }
      var transferInstructions = transferBlock.transferInstructions;
      if (transferInstructions == null) transferInstructions = transferBlock.transferInstructions = new List<PeInstruction>();
      var duplicateInstruction = new PeInstruction() { Operation = instruction.Operation, Operand1 = instruction.Operand1, Operand2 = instruction.Operand2};
      var transferInstructon = new PeInstruction() { Operation = new Operation() { OperationCode = OperationCode.Stloc, Value = temp }, 
        Operand1 = duplicateInstruction, sourceLocation = instruction.sourceLocation };
      transferInstructions.Add(transferInstructon);
    }

    private bool BlockOrDominatorDefines(PeBasicBlock<PeInstruction> block, GeneratorLocal temp) {
      Contract.Requires(block != null);
      Contract.Requires(temp != null);

      //First check if block defines the temp.
      if (block.definedTemporaries != null && block.definedTemporaries.Contains(temp)) return true;

      //Next check the dominators.
      var dominator = this.CfgQueries.ImmediateDominator(block);
      if (dominator == block) return false;
      if (BlockOrDominatorDefines(dominator, temp)) {
        if (block.definedTemporaries == null) block.definedTemporaries = new SetOfObjects();
        block.definedTemporaries.Add(temp);
      }
      return false;
    }

    private bool BlockOrPredecessorDefines(PeBasicBlock<PeInstruction> block, SetOfObjects traversedPredecessors, GeneratorLocal temp) {
      Contract.Requires(block != null);
      Contract.Requires(traversedPredecessors != null);
      Contract.Requires(temp != null);

      //First check if block defines the temp.
      if (block.definedTemporaries != null && block.definedTemporaries.Contains(temp)) return true;

      //Next check the precedecessors.
      var predecessors = this.CfgQueries.PredeccessorsFor(block);
      foreach (var predecessor in predecessors) {
        Contract.Assume(predecessor != null);
        if (!traversedPredecessors.Add(predecessor)) continue;
        if (BlockOrPredecessorDefines(predecessor, traversedPredecessors, temp)) return true;
      }
      return false;
    }

    //In particular, if a conditional branch is followed by an unconditional branch that goes to the same place, remove the conditional branch.
    //Also, if a branch branches to the fall through block, remove the branch.
    private void RemoveUselessBranches() {
      foreach (var block in this.Cdfg.AllBlocks) {
        Contract.Assume(block != null);
        this.RemoveUselessBranches(block);
      }
    }

    [ContractVerification(false)]
    private void RemoveUselessBranches(PeBasicBlock<PeInstruction> block) {
      Contract.Requires(block != null);

      var n = block.Instructions.Count;
      if (n <= 0) return;
      var instruction = block.Instructions[n-1];
      switch (instruction.Operation.OperationCode) {
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
        case OperationCode.Br:
        case OperationCode.Br_S:
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          Contract.Assume(instruction.Operation.Value is uint);
          var targetOffset = (uint)instruction.Operation.Value;

          //If the branch targets a successor block which is associated with a transfer block, then
          //redirect the branch to the transfer block. Obviously, the branch cannot be useless, so we keep it.
          if (block.transferBlocks != null) {
            var transferBlockOffset = uint.MaxValue - targetOffset;
            for (int i = 0, m = block.transferBlocks.Length; i < m; i++) {
              var tb = block.transferBlocks[i];
              if (tb == null) continue;
              if (tb.Offset == transferBlockOffset) continue;
              var copy =  this.copier.Copy(instruction.Operation);
              copy.Value = transferBlockOffset;
              return;
            }
          }

          var fallThroughBlock = (PeBasicBlock<PeInstruction>)block.FallThroughBlock;
          var fallthroughInstruction = GetFallThroughInstruction(ref fallThroughBlock, targetOffset);
          bool branchIsUseless = fallThroughBlock != null && fallThroughBlock.Offset == targetOffset;
          if (!branchIsUseless && fallthroughInstruction != null)
            branchIsUseless = HasSameBranchTarget(fallthroughInstruction, targetOffset);
          if (branchIsUseless) {    
            //If this branch were not present, control would end up in the same place as the branch is going to, so the branch is useless and we remove it.
            //But, if the branch has a source location, we can only do so if the next instruction cannot be reached any other way, otherwise debugging gets weird.
            if (instruction.Operation.Location is IPrimarySourceLocation) {
              if (fallthroughInstruction == null || fallThroughBlock == null) break;
              if (!this.CfgQueries.Dominates(block, fallThroughBlock)) break;
              var copy = this.copier.Copy(fallthroughInstruction.Operation);
              copy.Location = instruction.Operation.Location;
              fallthroughInstruction.Operation = copy;
            }
            block.Instructions[n-1].OmitInstruction = true;
          }
          break;
      }
    }

    private static bool HasSameBranchTarget(PeInstruction instruction, uint targetOffset) {
      Contract.Requires(instruction != null);

      switch (instruction.Operation.OperationCode) {
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
        case OperationCode.Br:
        case OperationCode.Br_S:
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          Contract.Assume(instruction.Operation.Value is uint);
          var instructionTargetOffset = (uint)instruction.Operation.Value;
          return instructionTargetOffset == targetOffset;
        default:
          return false;
      }
    }

    private static PeInstruction/*?*/ GetFallThroughInstruction(ref PeBasicBlock<PeInstruction>/*?*/ block, uint targetOffset) {
      while (block != null) {
        for (int i = 0, n = block.Instructions.Count; i < n; i++) {
          var instruction = block.Instructions[i];
          if (instruction.Operation.OperationCode == OperationCode.Nop) continue;
          return instruction;
        }
        if (block.Offset == targetOffset) return null;
        block = (PeBasicBlock<PeInstruction>)block.FallThroughBlock;
      }
      return null;
    }

    private IMethodBody GenerateNewBody() {
      var methodBody = this.Cdfg.MethodBody;
      var ilGenerator = new ILGenerator(host, methodBody.MethodDefinition);
      var converter = new PeILConverter<PeBasicBlock<PeInstruction>, PeInstruction>(this.Cdfg, ilGenerator, this.localScopeProvider, this.sourceLocationProvider);
      converter.PopulateILGenerator();
      return new ILGeneratorMethodBody(ilGenerator, methodBody.LocalsAreZeroed, converter.MaxStack, methodBody.MethodDefinition,
        converter.Locals.AsReadOnly(), Enumerable<ITypeDefinition>.Empty);
    }

    private void SeedValueMappingsWithPartialInput(IMethodDefinition methodDefinition, IMetadataConstant[] arguments) {
      Contract.Requires(methodDefinition != null);
      Contract.Requires(arguments != null);

      var n = arguments.Length;
      if (n == 0) return;
      var i = 0;
      if (!methodDefinition.IsStatic) {
        //TODO: set the value for the this argument.
        i++;
      }
      foreach (var parameter in methodDefinition.Parameters) {
        Contract.Assume(parameter != null);
        if (i >= n) return;
        var argument = arguments[i++];
        if (argument == null || argument is Dummy) continue;
        this.ValueMappings.SetCompileTimeConstantValueFor(parameter, argument);
      }

    }

  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="Instruction"></typeparam>
  public class PeBasicBlock<Instruction> : AiBasicBlock<Instruction>
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// A possibly null set of temporary variables that are defined when control leaves this block.
    /// </summary>
    internal SetOfObjects/*?*/ definedTemporaries;

    /// <summary>
    /// If a basic block needs to compute something for a particular successor only, this list will
    /// be non null and will contain a non null entry at the index matching the successor's index in this
    /// block's successor list.
    /// </summary>
    internal PeBasicBlock<Instruction>/*?*/[]/*?*/ transferBlocks;

    /// <summary>
    /// Will be non null only in blocks that are transfer blocks. Contains instructions that provide
    /// the successor of the transfer block with a consistent environment from all predecessors.
    /// </summary>
    internal List<Instruction>/*?*/ transferInstructions;


  }

  /// <summary>
  /// 
  /// </summary>
  public class PeInstruction : Microsoft.Cci.Analysis.Instruction {
    internal bool MustBeCachedInTemporary;
    internal bool HasBeenEmitted;
    internal bool LeaveResultOnStack;
    internal bool OmitInstruction;
    internal GeneratorLocal/*?*/ temporaryForResult;
    internal IPrimarySourceLocation/*?*/ sourceLocation;

    /// <summary>
    /// 
    /// </summary>
    public new PeInstruction Operand1 {
      get {
        return base.Operand1 as PeInstruction;
      }
      set {
        base.Operand1 = value;
      }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="BasicBlock"></typeparam>
  /// <typeparam name="Instruction"></typeparam>
  public class PeILConverter<BasicBlock, Instruction> : ControlFlowToMethodBodyConverter<BasicBlock, Instruction>
    where BasicBlock : PeBasicBlock<Instruction>, new()
    where Instruction : PeInstruction, new() {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cdfg"></param>
    /// <param name="ilGenerator"></param>
    /// <param name="localScopeProvider"></param>
    /// <param name="sourceLocationProvider"></param>
    public PeILConverter(ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ILGenerator ilGenerator,
      ILocalScopeProvider/*?*/ localScopeProvider, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(cdfg, ilGenerator, localScopeProvider, sourceLocationProvider) {
      Contract.Requires(cdfg != null);
      Contract.Requires(ilGenerator != null);

      this.useCount = new Dictionary<ILocalDefinition, int>();
    }

    Dictionary<ILocalDefinition, int> useCount;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.useCount != null);
    }

    /// <summary>
    /// 
    /// </summary>
    protected override void PopulateLocals() {
      foreach (var block in this.Cdfg.AllBlocks) {
        Contract.Assume(block != null);
        foreach (var instruction in block.Instructions) {
          Contract.Assume(instruction != null);
          if (instruction.OmitInstruction) continue;
          this.CountLocalUses(instruction);
        }
        if (block.transferInstructions != null) {
          foreach (var instruction in block.transferInstructions) {
            Contract.Assume(instruction != null);
            this.CountLocalUses(instruction);
          }
        }
      }
      foreach (var block in this.Cdfg.AllBlocks) {
        Contract.Assume(block != null);
        Instruction previous = null;
        foreach (var instruction in block.Instructions) {
          Contract.Assume(instruction != null);
          if (instruction.OmitInstruction) continue;
          previous = instruction;
        }
      }
      var counts = new List<KeyValuePair<ILocalDefinition, int>>(this.useCount);
      counts.Sort((KeyValuePair<ILocalDefinition, int> pair1, KeyValuePair<ILocalDefinition, int> pair2) => pair2.Value - pair1.Value);
      var locals = this.Locals;
      locals.Clear();
      foreach (var pair in counts) locals.Add(pair.Key);
    }

    private void CountLocalUses(Instruction instruction) {
      Contract.Requires(instruction != null);

      var local = instruction.Operation.Value as ILocalDefinition;
      if (local == null) local = instruction.temporaryForResult;
      if (local != null) {
        int count = 0;
        useCount.TryGetValue(local, out count);
        useCount[local] = ++count;
      }

      var operand1 = instruction.Operand1 as Instruction;
      if (operand1 != null) {
        this.CountLocalUses(operand1);
        var operand2 = instruction.Operand2 as Instruction;
        if (operand2 != null) {
          this.CountLocalUses(operand2);
        } else {
          var operands2toN = instruction.Operand2 as Instruction[];
          if (operands2toN != null) {
            for (int i = 0, n = operands2toN.Length; i < n; i++) {
              Contract.Assume(operands2toN[i] != null);
              this.CountLocalUses(operands2toN[i]);
            }
          }
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="block"></param>
    protected override void GenerateILFor(BasicBlock block) {
      if (block.transferInstructions != null) {
        this.StackHeight = 0;
        this.ILGenerator.MarkLabel(this.GetLabelFor(block.Offset));
        foreach (var instruction in block.transferInstructions) {
          Contract.Assume(instruction != null);
          this.EmitOperandsFor(instruction, alsoEmitOperation: true);
        }
        return;
      }

      base.GenerateILFor(block);

      if (block.transferBlocks != null) {
        var successors = this.Cdfg.SuccessorsFor(block);
        var n = successors.Count;
        Contract.Assume(n == block.transferBlocks.Length);
        for (int i = 0; i < n; i++) {
          var transferBlock = block.transferBlocks[i] as BasicBlock;
          if (transferBlock == null) continue;
          var successor = successors[i];
          this.GenerateILFor(transferBlock);
          if (block.FallThroughBlock == successor) {
            //If no transfer code is generated before or after this transfer block, we can just treat it as an extension
            //of block and can rely on fall through to get us to the successor block.
            var noBranchNeeded = true;
            for (int j = 0; j < n; j++) {
              if (j == i) continue;
              if (block.transferBlocks[j] != null) {
                noBranchNeeded = false;
                break;
              }
            }
            if (noBranchNeeded) break;
          }
          this.ILGenerator.Emit(OperationCode.Br, this.GetLabelFor(successor.Offset));
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    protected override void EmitSourceLocationFor(Instruction instruction) {
      var sloc = instruction.sourceLocation;
      if (sloc != null && sloc.StartLine != 0xfeefee)
        this.ILGenerator.MarkSequencePoint(sloc);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    [ContractVerification(false)]
    protected override void EmitOperandsFor(Instruction instruction) {
      this.EmitOperandsFor(instruction, alsoEmitOperation: false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    /// <param name="alsoEmitOperation"></param>
    [ContractVerification(false)]
    private void EmitOperandsFor(Instruction instruction, bool alsoEmitOperation) {
      Contract.Requires(instruction != null);

      if (instruction.OmitInstruction) return;
      if (instruction.temporaryForResult != null && instruction.HasBeenEmitted) {
        this.StackHeight++;
        this.LoadLocal(instruction.temporaryForResult);
        return;
      }

      var operand1 = instruction.Operand1 as Instruction;
      if (operand1 != null) {
        if (instruction.Operation.OperationCode == OperationCode.Dup && operand1.temporaryForResult != null) {
          instruction.temporaryForResult = operand1.temporaryForResult;
          instruction.HasBeenEmitted = true;
          if (!operand1.HasBeenEmitted) {
            operand1.LeaveResultOnStack = false;
            this.EmitOperandsFor(operand1, alsoEmitOperation: true);
          }
        } else
          this.EmitOperandsFor(operand1, alsoEmitOperation: true);
        var operand2 = instruction.Operand2 as Instruction;
        if (operand2 != null) {
          this.EmitOperandsFor(operand2, alsoEmitOperation: true);
        } else {
          var operands2toN = instruction.Operand2 as Instruction[];
          if (operands2toN != null) {
            for (int i = 0, n = operands2toN.Length; i < n; i++) {
              Contract.Assume(operands2toN[i] != null);
              this.EmitOperandsFor(operands2toN[i], alsoEmitOperation: true);
            }
          }
        }
      }
      if (alsoEmitOperation) {
        this.EmitScopeInformationFor(instruction);
        this.EmitOperationFor(instruction);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    [ContractVerification(false)]
    protected override void EmitOperationFor(Instruction instruction) {
      if (instruction.OmitInstruction) return;
      if (instruction.temporaryForResult != null && instruction.HasBeenEmitted) {
        this.StackHeight++;
        this.LoadLocal(instruction.temporaryForResult);
        return;
      }
      instruction.HasBeenEmitted = true;
      base.EmitOperationFor(instruction);
      if (instruction.temporaryForResult != null) {
        if (instruction.LeaveResultOnStack) {
          this.ILGenerator.Emit(OperationCode.Dup);
          this.StackHeight++;
        }
        this.StoreLocal(instruction.temporaryForResult);
      }
    }

  }

}