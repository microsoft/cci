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

namespace Microsoft.Cci.Optimization {

  /// <summary>
  /// Removes SSA locals that are used in redundant store, load sequences.
  /// </summary>
  [ContractVerification(false)]
  public class LocalMinimizer<BasicBlock, Instruction> 
      where BasicBlock : SSABasicBlock<Instruction>, new()
      where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="cdfg"></param>
    /// <param name="cfgQueries"></param>
    /// <param name="localScopeProvider"></param>
    /// <param name="sourceLocationProvider"></param>
    public LocalMinimizer(IMetadataHost host, 
      ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ControlGraphQueries<BasicBlock, Instruction> cfgQueries,
      ILocalScopeProvider/*?*/ localScopeProvider, ISourceLocationProvider/*?*/ sourceLocationProvider) {
      Contract.Requires(host != null);
      Contract.Requires(cdfg != null);
      Contract.Requires(cfgQueries != null);

      this.host = host;
      this.localScopeProvider = localScopeProvider;
      this.sourceLocationProvider = sourceLocationProvider;
      this.cdfg = cdfg;
      this.cfgQueries = cfgQueries;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.cdfg != null);
      Contract.Invariant(this.cfgQueries != null);
    }

    IMetadataHost host;
    ILocalScopeProvider/*?*/ localScopeProvider;
    ISourceLocationProvider/*?*/ sourceLocationProvider;
    ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg;
    ControlGraphQueries<BasicBlock, Instruction> cfgQueries;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IMethodBody MinimizeLocals() {
      //First eliminate redundant store load pairs

      //now run a post order traversal of blocks, and a reverse traversal of instructions in each block. Once a write to an SSA local is processed, return its
      //minimized local to a pool of available locals.

      var methodBody = this.cdfg.MethodBody;
      var ilGenerator = new ILGenerator(host, methodBody.MethodDefinition);
      var converter = new ILConverter(this.cdfg, ilGenerator, this.localScopeProvider, this.sourceLocationProvider);
      converter.PopulateILGenerator();
      return new ILGeneratorMethodBody(ilGenerator, methodBody.LocalsAreZeroed, converter.MaxStack, methodBody.MethodDefinition,
        converter.Locals.AsReadOnly(), Enumerable<ITypeDefinition>.Empty);
    }

    class ILConverter : ControlFlowToMethodBodyConverter<BasicBlock, Instruction> {

      internal ILConverter(ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ILGenerator ilGenerator,
        ILocalScopeProvider/*?*/ localScopeProvider, ISourceLocationProvider/*?*/ sourceLocationProvider)
        : base(cdfg, ilGenerator, localScopeProvider, sourceLocationProvider) {
        Contract.Requires(cdfg != null);
        Contract.Requires(ilGenerator != null);

        this.localFor = new Hashtable<object, GeneratorLocal>();
        this.redundantLocals = new SetOfObjects();
        this.storesThatShouldBecomePops = new SetOfObjects();
        this.useCount = new Dictionary<ILocalDefinition, int>();
      }

      Hashtable<object, GeneratorLocal> localFor;
      SetOfObjects redundantLocals;
      SetOfObjects storesThatShouldBecomePops;
      Dictionary<ILocalDefinition, int> useCount;

      [ContractInvariantMethod]
      private void ObjectInvariant() {
        Contract.Invariant(this.localFor != null);
        Contract.Invariant(this.redundantLocals != null);
        Contract.Invariant(this.storesThatShouldBecomePops != null);
        Contract.Invariant(this.useCount != null);
      }

      protected override void PopulateLocals() {
        foreach (var block in this.Cdfg.AllBlocks) {
          Contract.Assume(block != null);
          foreach (var instruction in block.Instructions) {
            Contract.Assume(instruction != null);
            //if (instruction.OmitInstruction) continue;
            this.CountLocalUses(instruction);
          }
        }
        foreach (var block in this.Cdfg.AllBlocks) {
          Contract.Assume(block != null);
          Instruction previous = null;
          foreach (var instruction in block.Instructions) {
            Contract.Assume(instruction != null);
            //if (instruction.OmitInstruction) continue;
            this.CheckForRedundantLocals(instruction, previous);
            previous = instruction;
          }
        }
        var counts = new List<KeyValuePair<ILocalDefinition, int>>(this.useCount);
        counts.Sort((KeyValuePair<ILocalDefinition, int> pair1, KeyValuePair<ILocalDefinition, int> pair2) => pair2.Value - pair1.Value);
        var locals = this.Locals;
        locals.Clear();
        foreach (var pair in counts) locals.Add(pair.Key);
      }

      private void CheckForRedundantLocals(Instruction instruction, Instruction/*?*/ previous) {
        Contract.Requires(instruction != null);

        var local = instruction.Operation.Value as ILocalDefinition;
        if (local != null) {
          switch (instruction.Operation.OperationCode) {
            case OperationCode.Stloc:
            case OperationCode.Stloc_0:
            case OperationCode.Stloc_1:
            case OperationCode.Stloc_2:
            case OperationCode.Stloc_3:
            case OperationCode.Stloc_S:
              if (this.useCount[local] == 1) {
                this.storesThatShouldBecomePops.Add(instruction);
                this.useCount.Remove(local);
              }
              break;
          }
          local = null;
        }

        if (previous == null) return;
        while (local == null && instruction.Operand1 is Instruction) {
          instruction = (Instruction)instruction.Operand1;
          local = instruction.Operation.Value as ILocalDefinition;
        }
        if (local == null || local != previous.Operation.Value) return;

        switch (previous.Operation.OperationCode) {
          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            break;
          default:
            return;
        }
        switch (instruction.Operation.OperationCode) {
          case OperationCode.Ldloc:
          case OperationCode.Ldloc_0:
          case OperationCode.Ldloc_1:
          case OperationCode.Ldloc_2:
          case OperationCode.Ldloc_3:
          case OperationCode.Ldloc_S:
            break;
          default:
            return;
        }

        if (this.useCount[local] == 2) {
          this.redundantLocals.Add(previous.Operation.Value);
          this.useCount.Remove(local);
        }
      }

      private void CountLocalUses(Instruction instruction) {
        Contract.Requires(instruction != null);

        var local = instruction.Operation.Value as ILocalDefinition;
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

      [ContractVerification(false)]
      protected override void EmitOperationFor(Instruction instruction) {
        var operation = instruction.Operation;
        switch (operation.OperationCode) {
          case OperationCode.Ldarg:
          case OperationCode.Ldarg_0:
          case OperationCode.Ldarg_1:
          case OperationCode.Ldarg_2:
          case OperationCode.Ldarg_3:
          case OperationCode.Ldarg_S:
            var local = this.localFor[operation.Value];
            if (local == null) break;
            this.LoadLocal(local);
            return;

          case OperationCode.Ldarga:
          case OperationCode.Ldarga_S:
            var ssaParameter = operation.Value as SSAParameterDefinition;
            if (ssaParameter == null) break;
            local = this.localFor[ssaParameter.OriginalParameter];
            if (local == null)
              this.LoadParameter(ssaParameter.OriginalParameter);
            else
              this.LoadLocal(local);
            this.localFor[ssaParameter] = local = new GeneratorLocal() { Name = ssaParameter.Name, Type = ssaParameter.Type };
            this.StoreLocal(local);
            this.LoadLocalAddress(local);
            return;

          case OperationCode.Starg:
          case OperationCode.Starg_S:
            ssaParameter = operation.Value as SSAParameterDefinition;
            if (ssaParameter == null) break;
            this.localFor[ssaParameter] = local = new GeneratorLocal() { Name = ssaParameter.Name, Type = ssaParameter.Type };
            this.StoreLocal(local);
            return;

          case OperationCode.Ldloc:
          case OperationCode.Ldloc_0:
          case OperationCode.Ldloc_1:
          case OperationCode.Ldloc_2:
          case OperationCode.Ldloc_3:
          case OperationCode.Ldloc_S:
            if (this.redundantLocals.Contains(operation.Value)) return;
            local = this.localFor[operation.Value];
            if (local == null) break;
            this.LoadLocal(local);
            return;

          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            if (this.storesThatShouldBecomePops.Contains(instruction)) {
              this.ILGenerator.Emit(OperationCode.Pop);
              return;
            }
            if (this.redundantLocals.Contains(operation.Value)) return;
            var ssaLocal = operation.Value as SSALocalDefinition;
            if (ssaLocal == null) break;
            this.localFor[ssaLocal] = local = new GeneratorLocal() { Name = ssaLocal.Name, Type = ssaLocal.Type };
            this.StoreLocal(local);
            return;

        }
        base.EmitOperationFor(instruction);
      }

    }


  }
}