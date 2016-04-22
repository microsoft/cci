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
using Microsoft.Cci.UtilityDataStructures;
using System;
using Microsoft.Cci.MutableCodeModel;
using System.Diagnostics;

namespace Microsoft.Cci.Optimization {

  /// <summary>
  /// Introduces temporary variables and transfer instructions in order to ensure that the operand stack is empty at the start of every basic block.
  /// </summary>
  [ContractVerification(false)]
  public class StackEliminator<BasicBlock, Instruction>
    where BasicBlock :PeBasicBlock<Instruction>, new()
    where Instruction : PeInstruction, new() {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="cdfg"></param>
    /// <param name="localScopeProvider"></param>
    /// <param name="sourceLocationProvider"></param>
    public StackEliminator(IMetadataHost host,
      ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ILocalScopeProvider/*?*/ localScopeProvider, ISourceLocationProvider/*?*/ sourceLocationProvider) {
      Contract.Requires(host != null);
      Contract.Requires(cdfg != null);

      this.host = host;
      this.localScopeProvider = localScopeProvider;
      this.sourceLocationProvider = sourceLocationProvider;
      this.cdfg = cdfg;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.cdfg != null);
    }

    IMetadataHost host;
    ILocalScopeProvider/*?*/ localScopeProvider;
    ISourceLocationProvider/*?*/ sourceLocationProvider;
    ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IMethodBody GetNewBody() {
      this.Eliminate();
      var methodBody = this.cdfg.MethodBody;
      var ilGenerator = new ILGenerator(host, methodBody.MethodDefinition);
      var converter = new PeILConverter<BasicBlock, Instruction>(this.cdfg, ilGenerator, this.localScopeProvider, this.sourceLocationProvider);
      converter.PopulateILGenerator();
      return new ILGeneratorMethodBody(ilGenerator, methodBody.LocalsAreZeroed, converter.MaxStack, methodBody.MethodDefinition,
        converter.Locals.AsReadOnly(), Enumerable<ITypeDefinition>.Empty);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private void Eliminate() {
      var blocksAlreadyVisited = new SetOfObjects();
      foreach (var block in this.cdfg.RootBlocks) {
        Contract.Assume(block != null);
        blocksAlreadyVisited.Clear();
        this.Eliminate(block, blocksAlreadyVisited);
      }
    }

    private void Eliminate(BasicBlock block, SetOfObjects blocksAlreadyVisited) {
      Contract.Requires(block != null);
      Contract.Requires(blocksAlreadyVisited != null);

      var successors = this.cdfg.SuccessorsFor(block);
      var n = successors.Count;
      if (n == 0) return;
      for (var i = 0; i < n; i++) {
        var succ = successors[i];
        this.AddTransferInstructions(block, succ, i);
        if (!blocksAlreadyVisited.Add(succ)) continue;
        this.Eliminate(succ, blocksAlreadyVisited);
      }
    }

    private void AddTransferInstructions(BasicBlock block, BasicBlock succ, int successorIndex) {
      Contract.Requires(block != null);
      Contract.Requires(succ != null);

      foreach (var stackLoad in succ.OperandStack) {
        if (stackLoad.temporaryForResult != null)
          stackLoad.temporaryForResult = new GeneratorLocal() { Type = stackLoad.Type };
        var joins = stackLoad.Operand2 as Instruction[];
        Contract.Assume(joins != null);
        for (int i = 0, n = joins.Length; i < n; i++) {
          var join = joins[i];
          Contract.Assume(join != null);
          Contract.Assume(BelongsTo(block, join));
          if (join.temporaryForResult == null) {
            join.temporaryForResult = new GeneratorLocal() { Type = stackLoad.Type };
            join.MustBeCachedInTemporary = true;
            join.LeaveResultOnStack = false;
          } else {
            Contract.Assume(join.MustBeCachedInTemporary);
            Contract.Assume(!join.LeaveResultOnStack);
          }
          this.AddTransferInstruction(block, succ, successorIndex, stackLoad.temporaryForResult, join.temporaryForResult, stackLoad.Type);
        }
      }
    }

    private void AddTransferInstruction(BasicBlock block, BasicBlock succ, int successorIndex, object targetLocal, object sourceLocal, ITypeReference type) {
      Contract.Requires(block != null);
      Contract.Requires(succ != null);
      Contract.Requires(type != null);

      if (block.transferBlocks == null)
        block.transferBlocks = new PeBasicBlock<Instruction>[this.cdfg.SuccessorsFor(block).Count];
      Contract.Assume(0 <= successorIndex && successorIndex < block.transferBlocks.Length);
      var transferBlock = block.transferBlocks[successorIndex];
      if (transferBlock == null)
        transferBlock = block.transferBlocks[successorIndex] = new PeBasicBlock<Instruction>();
      var transferInstructions = transferBlock.transferInstructions;
      if (transferInstructions == null)
        transferInstructions = transferBlock.transferInstructions = new List<Instruction>();
      var ldlocOp = new Operation() { OperationCode = Cci.OperationCode.Ldloc, Value = sourceLocal };
      var ldlocInstr = new Instruction() { Operation = ldlocOp, Type = type };
      transferInstructions.Add(ldlocInstr);
      var stlocOp = new Operation() { OperationCode = Cci.OperationCode.Stloc, Value = targetLocal };
      var stlocInstr = new Instruction() { Operation = stlocOp, Type = type.PlatformType.SystemVoid };
      transferInstructions.Add(stlocInstr);
    }

    [Pure]
    private static bool BelongsTo(BasicBlock block, Instruction instruction) {
      if (block == null) return false;
      foreach (var instr in block.Instructions) {
        if (instr == instruction) return true;
      }
      return false;
    }

  }


}