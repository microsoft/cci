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
  /// Changes a control flow graph from SSA form to a version where the SSA variables are unified into the smallest number of locals.
  /// This is somewhat like register allocation where the number of registers can grow as large as needed, but registers are typed.
  /// </summary>
  public class MultipleAssigner<BasicBlock, Instruction>
    where BasicBlock : PeBasicBlock<Instruction>, new()
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// Changes a control flow graph from SSA form to a version where the SSA variables are unified into the smallest number of locals.
    /// This is somewhat like register allocation where the number of registers can grow as large as needed, but registers are typed.
    /// </summary>
    /// <param name="host"></param>
    /// <param name="cdfg"></param>
    /// <param name="cfgQueries"></param>
    public MultipleAssigner(IMetadataHost host,  ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ControlGraphQueries<BasicBlock, Instruction> cfgQueries) {
      Contract.Requires(host != null);
      Contract.Requires(cdfg != null);
      Contract.Requires(cfgQueries != null);

      this.host = host;
      this.cdfg = cdfg;
      this.cfgQueries = cfgQueries;
      this.unifiedLocalFor = new Hashtable<object, GeneratorLocal>();
      this.availableLocalsFor = new MultiHashtable<GeneratorLocal>();
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.cdfg != null);
      Contract.Invariant(this.cfgQueries != null);
      Contract.Invariant(this.unifiedLocalFor != null);
      Contract.Invariant(this.availableLocalsFor != null);
    }

    IMetadataHost host;
    ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg;
    ControlGraphQueries<BasicBlock, Instruction> cfgQueries;
    Hashtable<object, GeneratorLocal> unifiedLocalFor;
    MultiHashtable<GeneratorLocal> availableLocalsFor;

    /// <summary>
    /// Changes a control flow graph from SSA form to a version where the SSA variables are unified into the smallest number of locals.
    /// This is somewhat like register allocation where the number of registers can grow as large as needed, but registers are typed.
    /// </summary>
    public void ReuseDeadLocals() {
      this.AddTransferInstructions();
      this.ReplaceSSALocalsInBlocks();
      this.ReplaceSSALocalsInTransferInstructions();
    }

    private void AddTransferInstructions() {
      var definedSSAVariables = new SetOfObjects();
      var blocksAlreadyVisited = new SetOfObjects();
      foreach (var block in this.cdfg.RootBlocks) {
        Contract.Assume(block != null);
        definedSSAVariables.Clear();
        blocksAlreadyVisited.Clear();
        this.AddTransferInstructions(block, definedSSAVariables, blocksAlreadyVisited);
      }
    }

    private void AddTransferInstructions(BasicBlock block, SetOfObjects definedSSAVariables, SetOfObjects blocksAlreadyVisited) {
      Contract.Requires(block != null);
      Contract.Requires(definedSSAVariables != null);
      Contract.Requires(blocksAlreadyVisited != null);

      if (block.Joins != null) {
        foreach (var join in block.Joins) {
          Contract.Assume(join.NewLocal != null);
          definedSSAVariables.Add(join.NewLocal);
        }
      }

      foreach (var instruction in block.Instructions) {
        var ssaLocal = instruction.Operation.Value as SSALocalDefinition;
        if (ssaLocal != null)
          definedSSAVariables.Add(ssaLocal);
        else {
          var ssaParam = instruction.Operation.Value as SSAParameterDefinition;
          if (ssaParam != null)
            definedSSAVariables.Add(ssaParam);
        }
      }

      var successors = this.cdfg.SuccessorsFor(block);
      var n = successors.Count;
      if (n == 0) return;
      for (var i = 0; i < n-1; i++) {
        var succ = successors[i];
        this.AddTransferInstructions(block, succ, i, definedSSAVariables);
        if (!blocksAlreadyVisited.Add(succ)) continue;
        var copyOfssaVariableFor = new SetOfObjects(definedSSAVariables);
        this.AddTransferInstructions(succ, copyOfssaVariableFor, blocksAlreadyVisited);
      }
      var lastSucc = successors[n-1];
      if (blocksAlreadyVisited.Add(lastSucc)) {
        this.AddTransferInstructions(block, lastSucc, n-1, definedSSAVariables);
        this.AddTransferInstructions(lastSucc, definedSSAVariables, blocksAlreadyVisited);
      }
    }

    private void AddTransferInstructions(BasicBlock block, BasicBlock succ, int successorIndex, SetOfObjects definedSSAVariables) {
      Contract.Requires(block != null);
      Contract.Requires(succ != null);
      Contract.Requires(definedSSAVariables != null);

      if (succ.Joins == null) return;
      foreach (var join in succ.Joins) {
        Contract.Assume(join.Join1 != null);
        if (definedSSAVariables.Contains(join.Join1)) {
          this.AddTransferInstruction(block, succ, successorIndex, join.NewLocal, join.Join1, join.Type);
          continue;
        }
        if (join.Join2 == null) continue;
        if (definedSSAVariables.Contains(join.Join2)) {
          this.AddTransferInstruction(block, succ, successorIndex, join.NewLocal, join.Join2, join.Type);
          continue;
        }
        if (join.OtherJoins == null) continue;
        foreach (var joini in join.OtherJoins) {
          if (!definedSSAVariables.Contains(joini)) continue;
          this.AddTransferInstruction(block, succ, successorIndex, join.NewLocal, joini, join.Type);
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

    private void ReplaceSSALocalsInBlocks() {
      foreach (var block in this.cfgQueries.BlocksInPostorder) {
        Contract.Assume(block != null);
        var n = block.Instructions.Count;
        for (int i = n-1; i >= 0; i--) {
          var instruction = block.Instructions[i];
          var ssaLocal = instruction.Operation.Value as SSALocalDefinition;
          if (ssaLocal != null)
            this.ReplaceSSALocal(instruction, ssaLocal);
          else {
            var ssaParam = instruction.Operation.Value as SSAParameterDefinition;
            if (ssaParam != null)
              this.ReplaceSSAParameter(instruction, ssaParam);
          }
        }
        if (block.Joins == null) continue;
        foreach (var join in block.Joins) {
          Contract.Assume(join.NewLocal != null);
          var unifiedLocal = this.unifiedLocalFor[join.NewLocal];
          if (unifiedLocal == null) continue;
          this.availableLocalsFor.Add(join.Type.InternedKey, unifiedLocal);
        }
      }
    }

    private void ReplaceSSALocal(Instruction instruction, SSALocalDefinition ssaLocal) {
      Contract.Requires(instruction != null);
      Contract.Requires(ssaLocal != null);

      var unifiedLocal = this.unifiedLocalFor[ssaLocal];
      if (unifiedLocal == null) {
        foreach (var local in this.availableLocalsFor.GetValuesFor(ssaLocal.Type.InternedKey)) {
          unifiedLocal = local;
          break;
        }
        if (unifiedLocal == null) {
          unifiedLocal = new GeneratorLocal() { Name = ssaLocal.Name, Type = ssaLocal.Type };
        }
        this.unifiedLocalFor[ssaLocal] = unifiedLocal;
      }
      var oldOperation = instruction.Operation;
      OperationCode newOp = OperationCode.Ldloc;
      switch (oldOperation.OperationCode) {
        case OperationCode.Ldloca:
        case OperationCode.Ldloca_S:
          newOp = OperationCode.Ldloca;
          goto freeUnifiedLocal;
        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
          newOp = OperationCode.Stloc;
      freeUnifiedLocal:
          this.availableLocalsFor.Add(ssaLocal.Type.InternedKey, unifiedLocal);
          break;
      }
      instruction.Operation = new Operation() { Location = oldOperation.Location, Offset = oldOperation.Offset, OperationCode = newOp, Value = unifiedLocal };
    }

    private void ReplaceSSAParameter(Instruction instruction, SSAParameterDefinition ssaParameter) {
      Contract.Requires(instruction != null);
      Contract.Requires(ssaParameter != null);

      var unifiedLocal = this.unifiedLocalFor[ssaParameter];
      if (unifiedLocal == null) {
        foreach (var local in this.availableLocalsFor.GetValuesFor(ssaParameter.Type.InternedKey)) {
          unifiedLocal = local;
          break;
        }
        if (unifiedLocal == null) {
          unifiedLocal = new GeneratorLocal() { Name = ssaParameter.Name, Type = ssaParameter.Type };
        }
        this.unifiedLocalFor[ssaParameter] = unifiedLocal;
      }
      var oldOperation = instruction.Operation;
      OperationCode newOp = OperationCode.Ldloc;
      switch (oldOperation.OperationCode) {
        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
          newOp = OperationCode.Ldloca;
          goto freeUnifiedLocal;
        case OperationCode.Starg:
        case OperationCode.Starg_S:
          newOp = OperationCode.Stloc;
      freeUnifiedLocal:
          this.availableLocalsFor.Add(ssaParameter.Type.InternedKey, unifiedLocal);
          break;
      }
      instruction.Operation = new Operation() { Location = oldOperation.Location, Offset = oldOperation.Offset, OperationCode = newOp, Value = unifiedLocal };
    }

    private void ReplaceSSALocalsInTransferInstructions() {
      foreach (var block in this.cdfg.AllBlocks) {
        Contract.Assume(block != null);
        if (block.transferBlocks == null) continue;
        foreach (var transferBlock in block.transferBlocks) {
          if (transferBlock == null) continue;
          if (transferBlock.transferInstructions == null) continue;
          foreach (var instruction in transferBlock.transferInstructions) {
            Contract.Assume(instruction != null);
            if (instruction.Operation.Value == null) continue;
            var unifiedLocal = this.unifiedLocalFor[instruction.Operation.Value];
            if (unifiedLocal == null) continue;
            var operation = instruction.Operation;
            var mutableOperation = operation as Operation;
            if (mutableOperation == null) mutableOperation = new Operation() { OperationCode = operation.OperationCode, Offset = operation.Offset, Location = operation.Location };
            mutableOperation.Value = unifiedLocal;
          }
        }
      }
    }

  }


}