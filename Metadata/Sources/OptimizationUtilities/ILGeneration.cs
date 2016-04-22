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
  /// A base class for converters that populate ILGenerator instances with the IL operations found in ControlAndDataFlowGraph instances.
  /// </summary>
  public class ControlFlowToMethodBodyConverter<BasicBlock, Instruction>
    where BasicBlock : Microsoft.Cci.Analysis.BasicBlock<Instruction>, new()
    where Instruction : Microsoft.Cci.Analysis.Instruction, new() {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cdfg"></param>
    /// <param name="ilGenerator"></param>
    /// <param name="localScopeProvider"></param>
    /// <param name="sourceLocationProvider"></param>
    public ControlFlowToMethodBodyConverter(ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg, ILGenerator ilGenerator,
      ILocalScopeProvider/*?*/ localScopeProvider, ISourceLocationProvider/*?*/ sourceLocationProvider) {
      Contract.Requires(cdfg != null);
      Contract.Requires(ilGenerator != null);

      this.cdfg = cdfg;
      this.ilGenerator = ilGenerator;
      this.localScopeProvider = localScopeProvider;
      this.sourceLocationProvider = sourceLocationProvider;
    }

    ILGenerator ilGenerator;
    ControlAndDataFlowGraph<BasicBlock, Instruction> cdfg;
    ILocalScopeProvider/*?*/ localScopeProvider;
    ISourceLocationProvider/*?*/ sourceLocationProvider;

    Hashtable<ILGeneratorLabel> labelFor = new Hashtable<ILGeneratorLabel>();
    Dictionary<ILocalDefinition, ushort> localIndex = new Dictionary<ILocalDefinition, ushort>();
    List<ILocalDefinition> localVariables = new List<ILocalDefinition>();
    Stack<ILocalScope> scopeStack = new Stack<ILocalScope>();
    IEnumerator<ILocalScope>/*?*/ scopeEnumerator;
    bool scopeEnumeratorIsValid;


    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.cdfg != null);
      Contract.Invariant(this.ilGenerator != null);
      Contract.Invariant(this.labelFor != null);
      Contract.Invariant(this.localIndex != null);
      Contract.Invariant(this.localVariables != null);
      Contract.Invariant(this.scopeStack != null);
    }

    /// <summary>
    /// 
    /// </summary>
    protected ControlAndDataFlowGraph<BasicBlock, Instruction> Cdfg {
      get {
        Contract.Ensures(Contract.Result<ControlAndDataFlowGraph<BasicBlock, Instruction>>() != null);
        return this.cdfg;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    protected ILGenerator ILGenerator {
      get {
        Contract.Ensures(Contract.Result<ILGenerator>() != null);
        return this.ilGenerator;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public List<ILocalDefinition> Locals {
      get { return this.localVariables; }
    }

    /// <summary>
    /// 
    /// </summary>
    public ushort MaxStack {
      get { return (ushort)this.maxStack; }
    }
    private uint maxStack;

    /// <summary>
    /// 
    /// </summary>
    protected uint StackHeight {
      get { return this.stackHeight; }
      set {
        //Contract.Assume(value != uint.MaxValue);
        this.stackHeight = value; 
        if (value >= this.maxStack) this.maxStack = value; 
      }
    }
    private uint stackHeight;

    /// <summary>
    /// 
    /// </summary>
    public void PopulateILGenerator() {
      this.InitializeLocals();
      var numberOfBlocks = (uint)this.cdfg.SuccessorEdges.Count;
      this.labelFor = new Hashtable<ILGeneratorLabel>(numberOfBlocks);
      var methodBody = this.cdfg.MethodBody;

      foreach (var exceptionInfo in methodBody.OperationExceptionInformation) {
        Contract.Assume(exceptionInfo != null);
        this.ILGenerator.AddExceptionHandlerInformation(exceptionInfo.HandlerKind, exceptionInfo.ExceptionType,
          this.GetLabelFor(exceptionInfo.TryStartOffset), this.GetLabelFor(exceptionInfo.TryEndOffset),
          this.GetLabelFor(exceptionInfo.HandlerStartOffset), this.GetLabelFor(exceptionInfo.HandlerEndOffset),
          exceptionInfo.HandlerKind == HandlerKind.Filter ? this.GetLabelFor(exceptionInfo.FilterDecisionStartOffset) : null);
      }

      if (this.localScopeProvider == null) {
        foreach (var localDef in this.Locals) {
          Contract.Assume(localDef != null);
          this.ILGenerator.AddVariableToCurrentScope(localDef);
        }
        foreach (var scope in this.ILGenerator.GetLocalScopes())
          this.scopeStack.Push(scope);
      } else {
        foreach (var ns in this.localScopeProvider.GetNamespaceScopes(methodBody)) {
          Contract.Assume(ns != null);
          foreach (var uns in ns.UsedNamespaces) {
            Contract.Assume(uns != null);
            this.ILGenerator.UseNamespace(uns.NamespaceName.Value);
          }
        }
        this.scopeEnumerator = this.localScopeProvider.GetLocalScopes(methodBody).GetEnumerator();
        this.scopeEnumeratorIsValid = this.scopeEnumerator.MoveNext();
      }

      Contract.Assume(this.cdfg != null);
      //TODO: rather follow the control flow graph so that dead code is not visited.
      foreach (var block in this.cdfg.AllBlocks) {
        Contract.Assume(block != null);
        this.GenerateILFor(block);
      }

      Contract.Assume(this.scopeStack != null);
      while (this.scopeStack.Count > 0) {
        this.ILGenerator.EndScope();
        this.scopeStack.Pop();
      }

      this.ILGenerator.AdjustBranchSizesToBestFit(eliminateBranchesToNext: true);
      this.localVariables.TrimExcess();
    }

    private void InitializeLocals() {
      this.PopulateLocals();
      for (int i = 0, n = this.localVariables.Count; i < n; i++) {
        var local = this.localVariables[i];
        this.localIndex.Add(local, (ushort)i);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    protected virtual void PopulateLocals() {
      if (this.localScopeProvider != null) {
        var localAlreadySeen = new SetOfObjects();
        foreach (var scope in this.localScopeProvider.GetLocalScopes(this.cdfg.MethodBody)) {
          Contract.Assume(scope != null);
          foreach (var local in this.localScopeProvider.GetVariablesInScope(scope)) {
            if (localAlreadySeen.Add(local)) {
              if (this.sourceLocationProvider != null) {
                bool isCompilerGenerated;
                this.sourceLocationProvider.GetSourceNameFor(local, out isCompilerGenerated);
                if (isCompilerGenerated) continue;
              }
              this.localVariables.Add(local);
            }
          }
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    protected virtual void EmitScopeInformationFor(Instruction instruction) {
      Contract.Requires(instruction != null);

      IOperation operation = instruction.Operation;
      if (this.scopeEnumerator == null) return;
      ILocalScope/*?*/ currentScope = null;
      while (this.scopeStack.Count > 0) {
        currentScope = this.scopeStack.Peek();
        Contract.Assume(currentScope != null);
        if (operation.Offset < currentScope.Offset+currentScope.Length) break;
        this.scopeStack.Pop();
        this.ilGenerator.EndScope();
        currentScope = null;
      }
      while (this.scopeEnumeratorIsValid) {
        currentScope = this.scopeEnumerator.Current;
        Contract.Assume(currentScope != null);
        if (currentScope.Offset <= operation.Offset && operation.Offset < currentScope.Offset+currentScope.Length) {
          this.scopeStack.Push(currentScope);
          this.ilGenerator.BeginScope();
          Contract.Assume(this.localScopeProvider != null);
          foreach (var local in this.localScopeProvider.GetVariablesInScope(currentScope)) {
            Contract.Assume(local != null);
            if (this.localIndex.ContainsKey(local))
              this.ilGenerator.AddVariableToCurrentScope(local);
          }
          foreach (var constant in this.localScopeProvider.GetConstantsInScope(currentScope)) {
            Contract.Assume(constant != null);
            this.ilGenerator.AddConstantToCurrentScope(constant);
          }
          this.scopeEnumeratorIsValid = this.scopeEnumerator.MoveNext();
        } else
          break;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    protected virtual void EmitSourceLocationFor(Instruction instruction) {
      Contract.Requires(instruction != null);

      IOperation operation = instruction.Operation;
      this.ilGenerator.MarkSequencePoint(operation.Location);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="block"></param>
    protected virtual void GenerateILFor(BasicBlock block) {
      Contract.Requires(block != null);

      this.StackHeight = (uint)block.OperandStack.Count;
      this.ilGenerator.MarkLabel(this.GetLabelFor(block.Offset));
      for (int i = 0, n = block.Instructions.Count; i < n; i++) {
        var instruction = block.Instructions[i];
        var operation = instruction.Operation;
        this.EmitOperandsFor(instruction);
        this.EmitScopeInformationFor(instruction);
        this.EmitSourceLocationFor(instruction);
        this.EmitOperationFor(instruction);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    protected virtual void EmitOperandsFor(Instruction instruction) {
      Contract.Requires(instruction != null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="instruction"></param>
    protected virtual void EmitOperationFor(Instruction instruction) {
      Contract.Requires(instruction != null);
      var operation = instruction.Operation;
      switch (operation.OperationCode) {
        case OperationCode.Arglist:
        case OperationCode.Dup:
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
        case OperationCode.Ldftn:
        case OperationCode.Ldnull:
        case OperationCode.Ldsfld:
        case OperationCode.Ldsflda:
        case OperationCode.Ldstr:
        case OperationCode.Ldtoken:
          this.StackHeight += 1;
          break;
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
        case OperationCode.And:
        case OperationCode.Ceq:
        case OperationCode.Cgt:
        case OperationCode.Cgt_Un:
        case OperationCode.Clt:
        case OperationCode.Clt_Un:
        case OperationCode.Div:
        case OperationCode.Div_Un:
        case OperationCode.Initobj:
        case OperationCode.Ldelem:
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
        case OperationCode.Mkrefany:
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
        case OperationCode.Or:
        case OperationCode.Pop:
        case OperationCode.Rem:
        case OperationCode.Rem_Un:
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Shr_Un:
        case OperationCode.Stsfld:
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
        case OperationCode.Switch:
        case OperationCode.Throw:
        case OperationCode.Xor:
          this.StackHeight -= 1;
          break;
        case OperationCode.Array_Addr:
        case OperationCode.Array_Get:
          Contract.Assume(operation.Value is IArrayTypeReference);
          var arrayType = (IArrayTypeReference)operation.Value;
          this.StackHeight -= arrayType.Rank;
          break;
        case OperationCode.Array_Set:
          Contract.Assume(operation.Value is IArrayTypeReference);
          arrayType = (IArrayTypeReference)operation.Value;
          this.StackHeight -= arrayType.Rank+1;
          break;
        case OperationCode.Array_Create:
          Contract.Assume(operation.Value is IArrayTypeReference);
          arrayType = (IArrayTypeReference)operation.Value;
          this.StackHeight -= arrayType.Rank-1;
          break;
        case OperationCode.Array_Create_WithLowerBound:
          Contract.Assume(operation.Value is IArrayTypeReference);
          arrayType = (IArrayTypeReference)operation.Value;
          this.StackHeight -= arrayType.Rank*2-1;
          break;
        case OperationCode.Br:
        case OperationCode.Br_S:
        case OperationCode.Leave:
        case OperationCode.Leave_S:
          Contract.Assume(operation.Value is uint);
          this.ilGenerator.Emit(operation.OperationCode, this.GetLabelFor((uint)operation.Value));
          return;
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
          this.StackHeight -= 2;
          Contract.Assume(operation.Value is uint);
          this.ilGenerator.Emit(operation.OperationCode, this.GetLabelFor((uint)operation.Value));
          return;
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          this.StackHeight -= 1;
          Contract.Assume(operation.Value is uint);
          this.ilGenerator.Emit(operation.OperationCode, this.GetLabelFor((uint)operation.Value));
          return;
        case OperationCode.Call:
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Newobj:
          Contract.Assume(operation.Value is ISignature);
          var signature = (ISignature)operation.Value;
          var adjustment = IteratorHelper.EnumerableCount(signature.Parameters);
          if (operation.OperationCode == OperationCode.Newobj)
            adjustment--;
          else {
            if (operation.OperationCode == OperationCode.Calli) adjustment++;
            if (!signature.IsStatic) adjustment++;
            if (signature.Type.TypeCode != PrimitiveTypeCode.Void) adjustment--;
          }
          this.StackHeight -= adjustment;
          break;
        case OperationCode.Cpobj:
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
          this.StackHeight -= 2;
          break;
        case OperationCode.Cpblk:
        case OperationCode.Initblk:
        case OperationCode.Stelem:
        case OperationCode.Stelem_I:
        case OperationCode.Stelem_I1:
        case OperationCode.Stelem_I2:
        case OperationCode.Stelem_I4:
        case OperationCode.Stelem_I8:
        case OperationCode.Stelem_R4:
        case OperationCode.Stelem_R8:
        case OperationCode.Stelem_Ref:
          this.StackHeight -= 3;
          break;
        case OperationCode.Ldarg:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S:
          this.StackHeight += 1;
          this.LoadParameter(operation.Value as IParameterDefinition);
          return;
        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
          this.StackHeight += 1;
          this.LoadParameterAddress(operation.Value as IParameterDefinition);
          return;
        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S:
          this.StackHeight += 1;
          Contract.Assume(operation.Value is ILocalDefinition);
          this.LoadLocal((ILocalDefinition)operation.Value);
          return;
        case OperationCode.Ldloca:
        case OperationCode.Ldloca_S:
          this.StackHeight += 1;
          Contract.Assume(operation.Value is ILocalDefinition);
          LoadLocalAddress((ILocalDefinition)operation.Value);
          return;
        case OperationCode.Starg:
        case OperationCode.Starg_S:
          this.StackHeight -= 1;
          this.StoreParameter(operation.Value as IParameterDefinition);
          return;
        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
          this.StackHeight -= 1;
          Contract.Assume(operation.Value is ILocalDefinition);
          this.StoreLocal((ILocalDefinition)operation.Value);
          return;
        case OperationCode.Box:
        case OperationCode.Break:
        case OperationCode.Castclass:
        case OperationCode.Ckfinite:
        case OperationCode.Constrained_:
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
        case OperationCode.Endfilter:
        case OperationCode.Endfinally:
        case OperationCode.Isinst:
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
        case OperationCode.Ldobj:
        case OperationCode.Ldvirtftn:
        case OperationCode.Localloc:
        case OperationCode.Neg:
        case OperationCode.Newarr:
        case OperationCode.No_:
        case OperationCode.Nop:
        case OperationCode.Not:
        case OperationCode.Readonly_:
        case OperationCode.Refanytype:
        case OperationCode.Refanyval:
        case OperationCode.Rethrow:
        case OperationCode.Sizeof:
        case OperationCode.Tail_:
        case OperationCode.Unaligned_:
        case OperationCode.Unbox:
        case OperationCode.Unbox_Any:
        case OperationCode.Volatile_:
          break;
        case OperationCode.Ret:
          if (this.Cdfg.MethodBody.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void)
            this.StackHeight -= 1;
          break;
        default:
          Contract.Assume(false);
          break;
      }
      this.ilGenerator.Emit(operation.OperationCode, operation.Value);      
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected ILGeneratorLabel GetLabelFor(uint offset) {
      var result = this.labelFor[offset];
      if (result == null)
        this.labelFor[offset] = result = new ILGeneratorLabel();
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="local"></param>
    /// <returns></returns>
    protected ushort GetLocalIndex(ILocalDefinition local) {
      Contract.Requires(local != null);

      ushort localIndex;
      if (this.localIndex.TryGetValue(local, out localIndex)) return localIndex;
      localIndex = (ushort)this.localIndex.Count;
      this.localIndex.Add(local, localIndex);
      this.localVariables.Add(local);
      return localIndex;
    }

    /// <summary>
    /// Translates the parameter list position of the given parameter to an IL parameter index. In other words,
    /// it adds 1 to the parameterDefinition.Index value if the containing method has an implicit this parameter.
    /// </summary>
    private static ushort GetParameterIndex(IParameterDefinition/*?*/ parameterDefinition) {
      if (parameterDefinition == null) return 0;
      ushort parameterIndex = parameterDefinition.Index;
      if (!parameterDefinition.ContainingSignature.IsStatic) parameterIndex++;
      return parameterIndex;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="local"></param>
    protected void LoadLocal(ILocalDefinition local) {
      Contract.Requires(local != null);

      ushort localIndex = this.GetLocalIndex(local);
      if (localIndex == 0) this.ilGenerator.Emit(OperationCode.Ldloc_0, local);
      else if (localIndex == 1) this.ilGenerator.Emit(OperationCode.Ldloc_1, local);
      else if (localIndex == 2) this.ilGenerator.Emit(OperationCode.Ldloc_2, local);
      else if (localIndex == 3) this.ilGenerator.Emit(OperationCode.Ldloc_3, local);
      else if (localIndex <= byte.MaxValue) this.ilGenerator.Emit(OperationCode.Ldloc_S, local);
      else this.ilGenerator.Emit(OperationCode.Ldloc, local);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="local"></param>
    protected void LoadLocalAddress(ILocalDefinition local) {
      Contract.Requires(local != null);

      ushort locIndex = GetLocalIndex(local);
      if (locIndex <= byte.MaxValue)
        this.ilGenerator.Emit(OperationCode.Ldloca_S, local);
      else
        this.ilGenerator.Emit(OperationCode.Ldloca, local);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameter"></param>
    protected void LoadParameter(IParameterDefinition/*?*/ parameter) {
      ushort parIndex = GetParameterIndex(parameter);
      if (parIndex == 0) this.ilGenerator.Emit(OperationCode.Ldarg_0, parameter);
      else if (parIndex == 1) this.ilGenerator.Emit(OperationCode.Ldarg_1, parameter);
      else if (parIndex == 2) this.ilGenerator.Emit(OperationCode.Ldarg_2, parameter);
      else if (parIndex == 3) this.ilGenerator.Emit(OperationCode.Ldarg_3, parameter);
      else if (parIndex <= byte.MaxValue) this.ilGenerator.Emit(OperationCode.Ldarg_S, parameter);
      else this.ilGenerator.Emit(OperationCode.Ldarg, parameter);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameter"></param>
    protected void LoadParameterAddress(IParameterDefinition/*?*/ parameter) {
      ushort parIndex = GetParameterIndex(parameter);
      if (parIndex <= byte.MaxValue)
        this.ilGenerator.Emit(OperationCode.Ldarga_S, parameter);
      else
        this.ilGenerator.Emit(OperationCode.Ldarga, parameter);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="local"></param>
    protected void StoreLocal(ILocalDefinition local) {
      Contract.Requires(local != null);

      ushort localIndex = this.GetLocalIndex(local);
      if (localIndex == 0) this.ilGenerator.Emit(OperationCode.Stloc_0, local);
      else if (localIndex == 1) this.ilGenerator.Emit(OperationCode.Stloc_1, local);
      else if (localIndex == 2) this.ilGenerator.Emit(OperationCode.Stloc_2, local);
      else if (localIndex == 3) this.ilGenerator.Emit(OperationCode.Stloc_3, local);
      else if (localIndex <= byte.MaxValue) this.ilGenerator.Emit(OperationCode.Stloc_S, local);
      else this.ilGenerator.Emit(OperationCode.Stloc, local);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameter"></param>
    protected void StoreParameter(IParameterDefinition/*?*/ parameter) {
      ushort parIndex = GetParameterIndex(parameter);
      if (parIndex <= byte.MaxValue) this.ilGenerator.Emit(OperationCode.Starg_S, parameter);
      else this.ilGenerator.Emit(OperationCode.Starg, parameter);
    }

  }

}