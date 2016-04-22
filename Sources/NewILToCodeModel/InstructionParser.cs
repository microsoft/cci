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
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {

  internal class InstructionParser : CodeTraverser {

    SourceMethodBody sourceMethodBody;
    IMetadataHost host;
    IMethodBody ilMethodBody;
    IMethodDefinition MethodDefinition;
    INameTable nameTable;
    ISourceLocationProvider/*?*/ sourceLocationProvider;
    ILocalScopeProvider/*?*/ localScopeProvider;
    DecompilerOptions options;
    IPlatformType platformType;
    ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction> cdfg;
    SetOfObjects bindingsThatMakeALastUseOfALocalVersion;
    SetOfObjects instructionsThatMakeALastUseOfALocalVersion = new SetOfObjects();
    HashtableForUintValues<object> numberOfAssignmentsToLocal = new HashtableForUintValues<object>();
    HashtableForUintValues<object> numberOfReferencesToLocal = new HashtableForUintValues<object>();
    Hashtable<List<IGotoStatement>> gotosThatTarget;
    Hashtable<LabeledStatement> targetStatementFor = new Hashtable<LabeledStatement>();
    Stack<Expression> operandStack = new Stack<Expression>();
    ISourceLocation/*?*/ lastSourceLocation;
    ILocation/*?*/ lastLocation;
    SynchronizationPointLocation/*?*/ lastSynchronizationLocation;
    ContinuationLocation/*?*/ lastContinuationLocation;
    Hashtable<SynchronizationPointLocation>/*?*/ synchronizatonPointLocationFor;
    bool sawReadonly;
    bool sawTailCall;
    bool sawVolatile;
    byte alignment;

    internal InstructionParser(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);

      this.sourceMethodBody = sourceMethodBody;
      this.host = sourceMethodBody.host; Contract.Assume(this.host != null);
      this.ilMethodBody = sourceMethodBody.ilMethodBody; Contract.Assume(this.ilMethodBody != null);
      this.MethodDefinition = sourceMethodBody.MethodDefinition;
      this.nameTable = sourceMethodBody.nameTable; Contract.Assume(this.nameTable != null);
      this.sourceLocationProvider = sourceMethodBody.sourceLocationProvider;
      this.localScopeProvider = sourceMethodBody.localScopeProvider;
      this.options = sourceMethodBody.options;
      this.platformType = sourceMethodBody.platformType; Contract.Assume(this.platformType != null);
      this.numberOfAssignmentsToLocal = sourceMethodBody.numberOfAssignmentsToLocal; Contract.Assume(this.numberOfAssignmentsToLocal != null);
      this.numberOfReferencesToLocal = sourceMethodBody.numberOfReferencesToLocal; Contract.Assume(this.numberOfReferencesToLocal != null);
      this.gotosThatTarget = sourceMethodBody.gotosThatTarget; Contract.Assume(this.gotosThatTarget != null);
      this.cdfg = sourceMethodBody.cdfg; Contract.Assume(this.cdfg != null);
      this.bindingsThatMakeALastUseOfALocalVersion = sourceMethodBody.bindingsThatMakeALastUseOfALocalVersion; Contract.Assume(this.bindingsThatMakeALastUseOfALocalVersion != null);

      if (this.localScopeProvider != null) {
        var syncInfo = this.localScopeProvider.GetSynchronizationInformation(sourceMethodBody);
        if (syncInfo != null) {
          var syncPointFor = this.synchronizatonPointLocationFor = new Hashtable<SynchronizationPointLocation>();
          IDocument doc = Dummy.Document;
          foreach (var loc in this.MethodDefinition.Locations) { doc = loc.Document; break; }
          foreach (var syncPoint in syncInfo.SynchronizationPoints) {
            Contract.Assume(syncPoint != null);
            var syncLoc = new SynchronizationPointLocation(doc, syncPoint);
            syncPointFor[syncPoint.SynchronizeOffset] = syncLoc;
            if (syncPoint.ContinuationMethod == null)
              syncPointFor[syncPoint.ContinuationOffset] = syncLoc;
          }
        }
      }
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.sourceMethodBody != null);
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.ilMethodBody != null);
      Contract.Invariant(this.MethodDefinition != null);
      Contract.Invariant(this.nameTable != null);
      Contract.Invariant(this.targetStatementFor != null);
      Contract.Invariant(this.platformType != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.numberOfReferencesToLocal != null);
      Contract.Invariant(this.gotosThatTarget != null);
      Contract.Invariant(this.operandStack != null);
      Contract.Invariant(this.cdfg != null);
      Contract.Invariant(this.bindingsThatMakeALastUseOfALocalVersion != null);
      Contract.Invariant(this.instructionsThatMakeALastUseOfALocalVersion != null);
      //Contract.Invariant(this.alignment == 0 || this.alignment == 1 || this.alignment == 2 || this.alignment == 4);
    }

    public override void TraverseChildren(IBlockStatement block) {
      base.TraverseChildren(block);
      Contract.Assume(block is DecompiledBlock);
      var b = (DecompiledBlock)block;
      int i = 0;
      int n = b.ContainedBlocks.Count;
      for (int j = 0, m = b.Statements.Count; j < m; j++) {
        var nb = b.Statements[j] as DecompiledBlock;
        if (nb == null) continue;
        while (i < n) {
          var bb = b.ContainedBlocks[i++];
          if (DecompiledBlock.GetStartOffset(bb) == nb.StartOffset) continue;
          if (DecompiledBlock.GetStartOffset(bb) >= nb.EndOffset) break;
        }
      }
      while (i < n) {
        var bb = b.ContainedBlocks[i++];
        this.ParseBasicBlock(b.Statements, bb);
      }
    }

    private void ParseBasicBlock(List<IStatement> list, BasicBlock<Instruction> bb) {
      Contract.Requires(list != null);
      Contract.Requires(bb != null);
      this.FindLastUsesOfLocals(bb);
      this.operandStack.Clear();
      foreach (var stackSetupInstruction in bb.OperandStack) {
        Contract.Assume(stackSetupInstruction != null);
        Contract.Assume(!(stackSetupInstruction.Type is Dummy));
        this.operandStack.Push(new PopValue() { Type = stackSetupInstruction.Type });
      }
      if (bb.Instructions.Count > 0) { //It might be 0 if there are no il instructions, which happens for some kinds of reference assemblies.
        var label = this.GetTargetStatement(bb.Instructions[0].Operation.Offset);
        list.Add(label);
        foreach (var instruction in bb.Instructions) {
          Contract.Assume(instruction != null);
          this.ParseInstruction(instruction, list);
        }
      }
      if (list.Count > 0 && !(list[list.Count-1] is GotoStatement))
        this.TurnOperandStackIntoPushStatements(list);
    }

    private void FindLastUsesOfLocals(BasicBlock<Instruction> bb) {
      Contract.Requires(bb != null);
      for (int i = 0, n = bb.Instructions.Count; i < n; i++) {
        Contract.Assume(i < bb.Instructions.Count);
        var instruction = bb.Instructions[i];
        switch (instruction.Operation.OperationCode) {
          case OperationCode.Ldloc:
          case OperationCode.Ldloc_0:
          case OperationCode.Ldloc_1:
          case OperationCode.Ldloc_2:
          case OperationCode.Ldloc_3:
          case OperationCode.Ldloc_S:
          case OperationCode.Ldloca:
          case OperationCode.Ldloca_S:
            var local = (ILocalDefinition)instruction.Operation.Value;
            if (this.NextReferenceIsAssignment(local, bb, i+1, new SetOfObjects()))
              this.instructionsThatMakeALastUseOfALocalVersion.Add(instruction);
            break;
        }
      }
    }

    private bool NextReferenceIsAssignment(ILocalDefinition local, BasicBlock<Instruction> bb, int offset, SetOfObjects blocksAlreadyVisited) {
      Contract.Requires(bb != null);
      Contract.Requires(offset >= 0);
      Contract.Requires(blocksAlreadyVisited != null);

      blocksAlreadyVisited.Add(bb);
      for (int i = offset, n = bb.Instructions.Count; i < n; i++) {
        var instruction = bb.Instructions[i];
        switch (instruction.Operation.OperationCode) {
          case OperationCode.Ldloc:
          case OperationCode.Ldloc_0:
          case OperationCode.Ldloc_1:
          case OperationCode.Ldloc_2:
          case OperationCode.Ldloc_3:
          case OperationCode.Ldloc_S:
          case OperationCode.Ldloca:
          case OperationCode.Ldloca_S:
            if (instruction.Operation.Value == local) return false;
            break;

          case OperationCode.Stloc:
          case OperationCode.Stloc_0:
          case OperationCode.Stloc_1:
          case OperationCode.Stloc_2:
          case OperationCode.Stloc_3:
          case OperationCode.Stloc_S:
            if (instruction.Operation.Value == local) return true;
            break;
        }
      }
      var result = true;
      foreach (var successor in this.cdfg.SuccessorsFor(bb)) {
        Contract.Assume(successor != null);
        if (blocksAlreadyVisited.Contains(successor)) continue;
        result &= this.NextReferenceIsAssignment(local, successor, 0, blocksAlreadyVisited);
        if (!result) break;
      }
      return result;
    }

    [ContractVerification(false)]
    private void ParseInstruction(Instruction instruction, List<IStatement> statements) {
      Contract.Requires(instruction != null);
      Contract.Requires(statements != null);
      Statement/*?*/ statement = null;
      Expression/*?*/ expression = null;
      ITypeReference/*?*/ elementType = null;
      IOperation currentOperation = instruction.Operation;
      OperationCode currentOpcode = currentOperation.OperationCode;
      if (this.host.PreserveILLocations) {
        if (this.lastLocation == null)
          this.lastLocation = currentOperation.Location;
      } else if (this.sourceLocationProvider != null) {
        if (this.lastSourceLocation == null) {
          foreach (var sourceLocation in this.sourceLocationProvider.GetPrimarySourceLocationsFor(currentOperation.Location)) {
            Contract.Assume(sourceLocation != null);
            if (sourceLocation.StartLine != 0x00feefee) {
              this.lastSourceLocation = sourceLocation;
              break;
            }
          }
        }
      }
      if (this.synchronizatonPointLocationFor != null) {
        uint currentOffset = currentOperation.Offset;
        var syncPointLocation = this.synchronizatonPointLocationFor[currentOffset];
        if (syncPointLocation != null) {
          if (syncPointLocation.SynchronizationPoint.ContinuationOffset == currentOffset)
            this.lastContinuationLocation = new ContinuationLocation(syncPointLocation);
          else
            this.lastSynchronizationLocation = syncPointLocation;
        }
      }
      switch (currentOpcode) {
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
          expression = this.ParseBinaryOperation(currentOpcode);
          break;

        case OperationCode.Arglist:
          expression = new RuntimeArgumentHandleExpression();
          break;

        case OperationCode.Array_Addr:
          elementType = ((IArrayTypeReference)currentOperation.Value).ElementType;
          expression = this.ParseArrayElementAddres(currentOperation, elementType);
          break;

        case OperationCode.Ldelema:
          elementType = (ITypeReference)currentOperation.Value;
          expression = this.ParseArrayElementAddres(currentOperation, elementType, treatArrayAsSingleDimensioned: true);
          break;

        case OperationCode.Array_Create:
        case OperationCode.Array_Create_WithLowerBound:
        case OperationCode.Newarr:
          expression = this.ParseArrayCreate(currentOperation);
          break;

        case OperationCode.Array_Get:
          elementType = ((IArrayTypeReference)currentOperation.Value).ElementType;
          expression = this.ParseArrayIndexer(currentOperation, elementType??this.platformType.SystemObject, treatArrayAsSingleDimensioned: false);
          break;

        case OperationCode.Ldelem:
          elementType = (ITypeReference)currentOperation.Value;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_I:
          elementType = this.platformType.SystemIntPtr;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_I1:
          elementType = this.platformType.SystemInt8;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_I2:
          elementType = this.platformType.SystemInt16;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_I4:
          elementType = this.platformType.SystemInt32;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_I8:
          elementType = this.platformType.SystemInt64;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_R4:
          elementType = this.platformType.SystemFloat32;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_R8:
          elementType = this.platformType.SystemFloat64;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_U1:
          elementType = this.platformType.SystemUInt8;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_U2:
          elementType = this.platformType.SystemUInt16;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_U4:
          elementType = this.platformType.SystemUInt32;
          goto case OperationCode.Ldelem_Ref;
        case OperationCode.Ldelem_Ref:
          expression = this.ParseArrayIndexer(currentOperation, elementType??this.platformType.SystemObject, treatArrayAsSingleDimensioned: true);
          break;

        case OperationCode.Array_Set:
          statement = this.ParseArraySet(currentOperation);
          break;

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
          statement = this.ParseBinaryConditionalBranch(currentOperation);
          break;

        case OperationCode.Box:
          expression = this.ParseConversion(currentOperation);
          break;

        case OperationCode.Br:
        case OperationCode.Br_S:
        case OperationCode.Leave:
        case OperationCode.Leave_S:
          statement = this.ParseUnconditionalBranch(currentOperation);
          break;

        case OperationCode.Break:
          statement = new DebuggerBreakStatement();
          break;

        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          statement = this.ParseUnaryConditionalBranch(currentOperation);
          break;

        case OperationCode.Call:
        case OperationCode.Callvirt:
          MethodCall call = this.ParseCall(currentOperation);
          if (call.MethodToCall.Type.TypeCode == PrimitiveTypeCode.Void) {
            call.Locations.Add(currentOperation.Location); // turning it into a statement prevents the location from being attached to the expresssion
            ExpressionStatement es = new ExpressionStatement();
            es.Expression = call;
            statement = es;
          } else
            expression = call;
          break;

        case OperationCode.Calli:
          expression = this.ParsePointerCall(currentOperation);
          break;

        case OperationCode.Castclass:
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
        case OperationCode.Unbox:
        case OperationCode.Unbox_Any:
          expression = this.ParseConversion(currentOperation);
          break;

        case OperationCode.Ckfinite:
          var operand = this.PopOperandStack();
          var chkfinite = new MutableCodeModel.MethodReference() {
            CallingConvention = Cci.CallingConvention.FastCall,
            ContainingType = host.PlatformType.SystemFloat64,
            Name = this.host.NameTable.GetNameFor("__ckfinite__"),
            Type = host.PlatformType.SystemFloat64,
            InternFactory = host.InternFactory,
          };
          expression = new MethodCall() { Arguments = new List<IExpression>(1) { operand }, IsStaticCall = true, Type = operand.Type, MethodToCall = chkfinite };
          break;

        case OperationCode.Constrained_:
          //This prefix is redundant and is not represented in the code model.
          break;

        case OperationCode.Cpblk:
          var copyMemory = new CopyMemoryStatement();
          copyMemory.NumberOfBytesToCopy = this.PopOperandStack();
          copyMemory.SourceAddress = this.PopOperandStack();
          copyMemory.TargetAddress = this.PopOperandStack();
          statement = copyMemory;
          break;

        case OperationCode.Cpobj:
          expression = this.ParseCopyObject();
          break;

        case OperationCode.Dup:
          expression = this.ParseDup(instruction.Type);
          break;

        case OperationCode.Endfilter:
          statement = this.ParseEndfilter();
          break;

        case OperationCode.Endfinally:
          statement = new EndFinally();
          break;

        case OperationCode.Initblk:
          var fillMemory = new FillMemoryStatement();
          fillMemory.NumberOfBytesToFill = this.PopOperandStack();
          fillMemory.FillValue = this.PopOperandStack();
          fillMemory.TargetAddress = this.PopOperandStack();
          statement = fillMemory;
          break;

        case OperationCode.Initobj:
          statement = this.ParseInitObject(currentOperation);
          break;

        case OperationCode.Isinst:
          expression = this.ParseCastIfPossible(currentOperation);
          break;

        case OperationCode.Jmp:
          var methodToCall = (IMethodReference)currentOperation.Value;
          expression = new MethodCall() { IsJumpCall = true, MethodToCall = methodToCall, Type = methodToCall.Type };
          break;

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
        case OperationCode.Ldfld:
        case OperationCode.Ldsfld:
          expression = this.ParseBoundExpression(instruction);
          break;

        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
        case OperationCode.Ldflda:
        case OperationCode.Ldsflda:
        case OperationCode.Ldloca:
        case OperationCode.Ldloca_S:
        case OperationCode.Ldftn:
        case OperationCode.Ldvirtftn:
          expression = this.ParseAddressOf(instruction);
          break;

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
          expression = this.ParseCompileTimeConstant(currentOperation);
          break;

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
        case OperationCode.Ldobj:
          expression = this.ParseAddressDereference(currentOperation);
          break;

        case OperationCode.Ldlen:
          expression = this.ParseVectorLength();
          break;

        case OperationCode.Ldtoken:
          expression = ParseToken(currentOperation);
          break;

        case OperationCode.Localloc:
          expression = this.ParseStackArrayCreate();
          break;

        case OperationCode.Mkrefany:
          expression = this.ParseMakeTypedReference(currentOperation);
          break;

        case OperationCode.Neg:
          expression = this.ParseUnaryOperation(new UnaryNegation());
          break;

        case OperationCode.Not:
          expression = this.ParseUnaryOperation(new OnesComplement());
          break;

        case OperationCode.Newobj:
          expression = this.ParseCreateObjectInstance(currentOperation);
          break;

        case OperationCode.No_:
          Contract.Assume(false); //if code out there actually uses this, I need to know sooner rather than later.
          //TODO: need object model support
          break;

        case OperationCode.Nop:
          statement = new EmptyStatement();
          break;

        case OperationCode.Pop:
          statement = this.ParsePop();
          break;

        case OperationCode.Readonly_:
          this.sawReadonly = true;
          break;

        case OperationCode.Refanytype:
          expression = this.ParseGetTypeOfTypedReference();
          break;

        case OperationCode.Refanyval:
          expression = this.ParseGetValueOfTypedReference(currentOperation);
          break;

        case OperationCode.Ret:
          statement = this.ParseReturn();
          break;

        case OperationCode.Rethrow:
          statement = new RethrowStatement();
          break;

        case OperationCode.Sizeof:
          expression = ParseSizeOf(currentOperation);
          break;

        case OperationCode.Starg:
        case OperationCode.Starg_S:
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
        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
        case OperationCode.Stobj:
        case OperationCode.Stsfld:
          statement = this.ParseAssignment(currentOperation);
          break;

        case OperationCode.Switch:
          statement = this.ParseSwitchInstruction(currentOperation);
          break;

        case OperationCode.Tail_:
          this.sawTailCall = true;
          break;

        case OperationCode.Throw:
          statement = this.ParseThrow();
          break;

        case OperationCode.Unaligned_:
          Contract.Assume(currentOperation.Value is byte);
          var alignment = (byte)currentOperation.Value;
          Contract.Assume(alignment == 1 || alignment == 2 || alignment == 4);
          this.alignment = alignment;
          break;

        case OperationCode.Volatile_:
          this.sawVolatile = true;
          break;

      }
      if (expression != null) {
        if (expression.Type is Dummy)
          expression.Type = instruction.Type;
        Contract.Assume(!(expression.Type is Dummy));
        if (expression.Type.TypeCode != PrimitiveTypeCode.Void) {
          if (this.host.PreserveILLocations) {
            expression.Locations.Add(currentOperation.Location);
          }
          this.operandStack.Push(expression);
        }
      } else if (statement != null) {
        this.TurnOperandStackIntoPushStatements(statements);
        statements.Add(statement);
        if (this.host.PreserveILLocations) {
          if (this.lastLocation != null) {
            statement.Locations.Add(this.lastLocation);
            this.lastLocation = null;
          }
        } else if (this.lastSourceLocation != null) {
          statement.Locations.Add(this.lastSourceLocation);
          this.lastSourceLocation = null;
        }
        if (this.lastSynchronizationLocation != null) {
          statement.Locations.Add(this.lastSynchronizationLocation);
          this.lastSynchronizationLocation = null;
        } else if (this.lastContinuationLocation != null) {
          statement.Locations.Add(this.lastContinuationLocation);
          this.lastContinuationLocation = null;
        }
      }
    }

    private void TurnOperandStackIntoPushStatements(List<IStatement> statements) {
      Contract.Requires(statements != null);
      List<Expression> correspondingPops = null;
      int insertPoint = statements.Count;
      while (this.operandStack.Count > 0) {
        Expression operand = this.PopOperandStack();
        if (operand is PopValue) { this.operandStack.Push(operand); break; }
        Contract.Assume(!(operand.Type is Dummy));
        PushStatement push = new PushStatement();
        push.ValueToPush = operand;
        statements.Insert(insertPoint, push);
        if (correspondingPops == null) correspondingPops = new List<Expression>(this.operandStack.Count);
        correspondingPops.Add(new PopValue() { Type = operand.Type });
      }
      if (correspondingPops == null) return;
      for (int i = correspondingPops.Count-1; i >= 0; i--)
        this.operandStack.Push(correspondingPops[i]);
    }

    private BinaryOperation ParseAddition(OperationCode currentOpcode) {
      Addition addition = new Addition();
      addition.CheckOverflow = currentOpcode != OperationCode.Add;
      if (currentOpcode == OperationCode.Add_Ovf_Un) {
        addition.TreatOperandsAsUnsignedIntegers = true; //force use of unsigned addition, even for cases where the operands are expressions that result in signed values
        return this.ParseUnsignedBinaryOperation(addition);
      } else
        return this.ParseBinaryOperation(addition);
    }

    private Expression ParseAddressOf(Instruction instruction) {
      Contract.Requires(instruction != null);

      var currentOperation = instruction.Operation;
      AddressableExpression addressableExpression = new AddressableExpression();
      if (currentOperation.Value == null) {
        Contract.Assume(currentOperation.OperationCode == OperationCode.Ldarga || currentOperation.OperationCode == OperationCode.Ldarga_S);
        var thisType = this.cdfg.MethodBody.MethodDefinition.ContainingTypeDefinition;
        addressableExpression.Definition = new ThisReference() { Type = thisType };
        addressableExpression.Type = thisType;
      } else {
        var definition = currentOperation.Value;
        Contract.Assume(definition is ILocalDefinition || definition is IParameterDefinition ||
          definition is IFieldReference || definition is IMethodReference || definition is IExpression);
        addressableExpression.Definition = definition;
        addressableExpression.Type = GetTypeFrom(definition);
      }
      if (currentOperation.OperationCode == OperationCode.Ldflda || currentOperation.OperationCode == OperationCode.Ldvirtftn)
        addressableExpression.Instance = this.PopOperandStack();
      if (currentOperation.OperationCode == OperationCode.Ldloca || currentOperation.OperationCode == OperationCode.Ldloca_S) {
        var local = this.GetLocalWithSourceName((ILocalDefinition)currentOperation.Value);
        addressableExpression.Definition = local;
        this.numberOfReferencesToLocal[local] = this.numberOfReferencesToLocal.ContainsKey(local) ? this.numberOfReferencesToLocal[local] + 1 : 1;
        //Treat this as an assignment as well, so that the local does not get deleted because it contains a constant and has only one assignment to it.
        this.numberOfAssignmentsToLocal[local] = this.numberOfAssignmentsToLocal.ContainsKey(local) ? this.numberOfAssignmentsToLocal[local] + 1 : 1;
        if (this.instructionsThatMakeALastUseOfALocalVersion.Contains(instruction)) {
          this.instructionsThatMakeALastUseOfALocalVersion.Remove(instruction);
          this.bindingsThatMakeALastUseOfALocalVersion.Add(addressableExpression);
        }
      }
      return new AddressOf() { Expression = addressableExpression };
    }

    private static ITypeReference GetTypeFrom(object definition) {
      var local = definition as ILocalDefinition;
      if (local != null) return local.Type;
      var param = definition as IParameterDefinition;
      if (param != null) return param.Type;
      var field = definition as IFieldReference;
      if (field != null) return field.Type;
      var method = definition as IMethodReference;
      if (method != null) return method.Type;
      var expr = definition as IExpression;
      if (expr != null) return expr.Type;
      return Dummy.TypeReference;
    }

    private Expression ParseAddressDereference(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      AddressDereference result = new AddressDereference();
      result.Address = this.PopOperandStack();
      result.Alignment = this.alignment;
      result.IsVolatile = this.sawVolatile;
      this.alignment = 0;
      this.sawVolatile = false;
      return result;
    }

    private Expression ParseArrayCreate(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      Contract.Assume(currentOperation.Value is IArrayTypeReference);
      IArrayTypeReference arrayType = (IArrayTypeReference)currentOperation.Value;
      CreateArray result = new CreateArray();
      result.ElementType = arrayType.ElementType;
      result.Rank = arrayType.Rank;
      if (currentOperation.OperationCode == OperationCode.Array_Create_WithLowerBound) {
        for (uint i = 0; i < arrayType.Rank; i++)
          result.LowerBounds.Add(ConvertToInt(this.PopOperandStack()));
        result.LowerBounds.Reverse();
      }
      for (uint i = 0; i < arrayType.Rank; i++)
        result.Sizes.Add(this.PopOperandStack());
      result.Sizes.Reverse();
      return result;
    }

    private Expression ParseArrayElementAddres(IOperation currentOperation, ITypeReference elementType, bool treatArrayAsSingleDimensioned = false) {
      Contract.Requires(currentOperation != null);
      Contract.Requires(elementType != null);
      AddressOf result = new AddressOf();
      result.ObjectControlsMutability = this.sawReadonly;
      AddressableExpression addressableExpression = new AddressableExpression();
      result.Expression = addressableExpression;
      ArrayIndexer indexer = this.ParseArrayIndexer(currentOperation, elementType, treatArrayAsSingleDimensioned);
      addressableExpression.Definition = indexer;
      addressableExpression.Instance = indexer.IndexedObject;
      addressableExpression.Type = elementType;
      this.sawReadonly = false;
      return result;
    }

    private ArrayIndexer ParseArrayIndexer(IOperation currentOperation, ITypeReference elementType, bool treatArrayAsSingleDimensioned = false) {
      Contract.Requires(currentOperation != null);
      Contract.Requires(elementType != null);

      uint rank = 1;
      IArrayTypeReference/*?*/ arrayType = null;
      if (!treatArrayAsSingleDimensioned) //then currentOperation.Value contains the type of the array, not the type of the indexed element.
        arrayType = currentOperation.Value as IArrayTypeReference;
      if (arrayType != null) rank = arrayType.Rank;
      ArrayIndexer result = new ArrayIndexer();
      for (uint i = 0; i < rank; i++)
        result.Indices.Add(this.PopOperandStack());
      result.Indices.Reverse();
      var indexedObject = this.PopOperandStack();
      result.Type = elementType; //obtained from the instruction, but could be a lossy abstraction, or null
      if (arrayType == null)
        arrayType = indexedObject.Type as IArrayTypeReference;
      if (arrayType != null) //rather use its element type than the caller's element type (which is derived from the operation code).
        result.Type = arrayType.ElementType;
      else
        arrayType = Immutable.Vector.GetVector(elementType, this.host.InternFactory);
      if (!TypeHelper.TypesAreEquivalent(indexedObject.Type, arrayType))
        indexedObject = new Conversion() { ValueToConvert = indexedObject, TypeAfterConversion = arrayType };
      Contract.Assume(indexedObject.Type is IArrayTypeReference);
      result.IndexedObject = indexedObject;
      Contract.Assume(!(result.Type is Dummy));
      return result;
    }

    private Statement ParseArraySet(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      ExpressionStatement result = new ExpressionStatement();
      Assignment assignment = new Assignment();
      result.Expression = assignment;
      assignment.Source = this.PopOperandStack();
      TargetExpression targetExpression = new TargetExpression();
      assignment.Target = targetExpression;
      Contract.Assume(currentOperation.Value is IArrayTypeReference);
      ArrayIndexer indexer = this.ParseArrayIndexer(currentOperation, ((IArrayTypeReference)currentOperation.Value).ElementType);
      targetExpression.Definition = indexer;
      targetExpression.Instance = indexer.IndexedObject;
      targetExpression.Type = indexer.Type;
      assignment.Source = TypeInferencer.Convert(assignment.Source, indexer.Type);
      assignment.Type = indexer.Type;
      return result;
    }

    private Statement ParseAssignment(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      TargetExpression target = new TargetExpression();
      ITypeReference/*?*/ elementType = null;
      if (this.alignment > 0) {
        Contract.Assume(this.alignment == 1 || this.alignment == 2 || this.alignment == 4);
        target.Alignment = this.alignment;
      }
      target.IsVolatile = this.sawVolatile;
      Assignment assignment = new Assignment();
      assignment.Target = target;
      assignment.Source = this.PopOperandStack();
      ExpressionStatement result = new ExpressionStatement();
      result.Expression = assignment;
      switch (currentOperation.OperationCode) {
        case OperationCode.Starg:
        case OperationCode.Starg_S: {
            var definition = currentOperation.Value;
            if (definition == null) {
              target.Definition = new ThisReference();
              var typeForThis = (INamedTypeDefinition)this.MethodDefinition.ContainingTypeDefinition;
              if (typeForThis.IsValueType)
                target.Type = Immutable.ManagedPointerType.GetManagedPointerType(Microsoft.Cci.MutableCodeModel.NamedTypeDefinition.SelfInstance(typeForThis, this.host.InternFactory), this.host.InternFactory);
              else
                target.Type = NamedTypeDefinition.SelfInstance(typeForThis, this.host.InternFactory);
            } else {
              var par = definition as IParameterDefinition;
              Contract.Assume(par != null);
              target.Definition = definition;
              target.Type = par.Type;
            }
            break;
          }
        case OperationCode.Stfld:
          target.Instance = this.PopOperandStack();
          goto case OperationCode.Stsfld;
        case OperationCode.Stsfld:
          Contract.Assume(currentOperation.Value is IFieldReference);
          var field = (IFieldReference)currentOperation.Value;
          target.Definition = field;
          target.Type = field.Type;
          break;
        case OperationCode.Stelem:
          elementType = (ITypeReference)currentOperation.Value;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_I:
          elementType = this.platformType.SystemIntPtr;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_I1:
          elementType = this.platformType.SystemInt8;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_I2:
          elementType = this.platformType.SystemInt16;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_I4:
          elementType = this.platformType.SystemInt32;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_I8:
          elementType = this.platformType.SystemInt64;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_R4:
          elementType = this.platformType.SystemFloat32;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_R8:
          elementType = this.platformType.SystemFloat64;
          goto case OperationCode.Stelem_Ref;
        case OperationCode.Stelem_Ref:
          ArrayIndexer indexer = this.ParseArrayIndexer(currentOperation, elementType??this.platformType.SystemObject, treatArrayAsSingleDimensioned: true);
          target.Definition = indexer;
          target.Instance = indexer.IndexedObject;
          target.Type = indexer.Type;
          break;
        case OperationCode.Stind_I:
          elementType = this.platformType.SystemIntPtr;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stind_I1:
          elementType = this.platformType.SystemInt8;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stind_I2:
          elementType = this.platformType.SystemInt16;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stind_I4:
          elementType = this.platformType.SystemInt32;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stind_I8:
          elementType = this.platformType.SystemInt64;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stind_R4:
          elementType = this.platformType.SystemFloat32;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stind_R8:
          elementType = this.platformType.SystemFloat64;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stobj:
          elementType = (ITypeReference)currentOperation.Value;
          goto case OperationCode.Stind_Ref;
        case OperationCode.Stind_Ref:
          AddressDereference addressDereference = new AddressDereference();
          addressDereference.Address = this.PopOperandStack();
          addressDereference.Alignment = this.alignment;
          addressDereference.IsVolatile = this.sawVolatile;
          target.Definition = addressDereference;
          var pointerType = addressDereference.Address.Type as IPointerTypeReference;
          if (pointerType != null)
            addressDereference.Type = pointerType.TargetType;
          else {
            var managedPointerType = addressDereference.Address.Type as IManagedPointerTypeReference;
            if (managedPointerType != null)
              addressDereference.Type = managedPointerType.TargetType;
            else {
              //The pointer itself is untyped, so the instruction must have specified the element type
              addressDereference.Type = elementType??this.platformType.SystemObject;
            }
          }
          target.Type = addressDereference.Type;
          break;
        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
          Contract.Assume(currentOperation.Value is ILocalDefinition);
          var local = this.GetLocalWithSourceName((ILocalDefinition)currentOperation.Value);
          target.Definition = local;
          this.numberOfAssignmentsToLocal[local] =
            this.numberOfAssignmentsToLocal.ContainsKey(local) ?
            this.numberOfAssignmentsToLocal[local] + 1 :
            1;
          target.Type = local.Type;
          break;
        default: {
            var definition = currentOperation.Value;
            Contract.Assume(definition is ILocalDefinition || definition is IParameterDefinition || 
            definition is IFieldReference || definition is IArrayIndexer || 
            definition is IAddressDereference || definition is IPropertyDefinition);
            target.Definition = definition;
            break;
          }
      }
      assignment.Source = TypeInferencer.Convert(assignment.Source, target.Type); //mainly to convert (u)ints to bools, chars and pointers.
      assignment.Type = target.Type;
      Contract.Assume(assignment.Target.Type.TypeCode != PrimitiveTypeCode.Boolean || assignment.Source.Type.TypeCode == PrimitiveTypeCode.Boolean || IsByRef(assignment.Target.Definition));
      this.alignment = 0;
      this.sawVolatile = false;
      return result;
    }

    private static bool IsByRef(object definition) {
      var local = definition as ILocalDefinition;
      if (local != null) return local.IsReference;
      var parameter = definition as IParameterDefinition;
      if (parameter != null) return parameter.IsByReference;
      return false;
    }

    private ITypeReference TypeFor(OperationCode operationCode) {
      switch (operationCode) {
        case OperationCode.Stelem_I: return this.host.PlatformType.SystemIntPtr;
        case OperationCode.Stelem_I1: return this.host.PlatformType.SystemInt8;
        case OperationCode.Stelem_I2: return this.host.PlatformType.SystemInt16;
        case OperationCode.Stelem_I4: return this.host.PlatformType.SystemInt32;
        case OperationCode.Stelem_I8: return this.host.PlatformType.SystemInt64;
        case OperationCode.Stelem_R4: return this.host.PlatformType.SystemFloat32;
        case OperationCode.Stelem_R8: return this.host.PlatformType.SystemFloat64;
        case OperationCode.Stelem_Ref: return this.host.PlatformType.SystemObject;
      }
      return Dummy.TypeReference;
    }

    private BinaryOperation ParseBinaryOperation(BinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      binaryOperation.RightOperand = this.PopOperandStack();
      binaryOperation.LeftOperand = this.PopOperandStack();
      return binaryOperation;
    }

    private Expression ParseBinaryOperation(OperationCode currentOpcode) {
      switch (currentOpcode) {
        default:
          Contract.Assume(false);
          goto case OperationCode.Xor;
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          return this.ParseAddition(currentOpcode);
        case OperationCode.And:
          return this.ParseBinaryOperation(new BitwiseAnd());
        case OperationCode.Ceq:
          return this.ParseEquality();
        case OperationCode.Cgt:
          return this.HarmonizeOperands(this.ParseBinaryOperation(new GreaterThan()));
        case OperationCode.Cgt_Un:
          return this.HarmonizeOperands(this.ParseBinaryOperation(new GreaterThan() { IsUnsignedOrUnordered = true }));
        case OperationCode.Clt:
          return this.HarmonizeOperands(this.ParseBinaryOperation(new LessThan()));
        case OperationCode.Clt_Un:
          return this.HarmonizeOperands(this.ParseBinaryOperation(new LessThan() { IsUnsignedOrUnordered = true }));
        case OperationCode.Div:
          return this.ParseBinaryOperation(new Division());
        case OperationCode.Div_Un:
          return this.ParseUnsignedBinaryOperation(new Division() { TreatOperandsAsUnsignedIntegers = true });
        case OperationCode.Mul:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
          return this.ParseMultiplication(currentOpcode);
        case OperationCode.Or:
          return this.ParseBinaryOperation(new BitwiseOr());
        case OperationCode.Rem:
          return this.ParseBinaryOperation(new Modulus());
        case OperationCode.Rem_Un:
          return this.ParseUnsignedBinaryOperation(new Modulus() { TreatOperandsAsUnsignedIntegers = true });
        case OperationCode.Shl:
          return this.ParseBinaryOperation(new LeftShift());
        case OperationCode.Shr:
          return this.ParseBinaryOperation(new RightShift());
        case OperationCode.Shr_Un:
          RightShift shrun = new RightShift();
          shrun.RightOperand = this.PopOperandStack();
          shrun.LeftOperand = this.PopOperandStackAsUnsigned();
          return shrun;
        case OperationCode.Sub:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un:
          return this.ParseSubtraction(currentOpcode);
        case OperationCode.Xor:
          return this.ParseBinaryOperation(new ExclusiveOr());
      }
    }

    private BinaryOperation HarmonizeOperands(BinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      Contract.Ensures(Contract.Result<BinaryOperation>() != null);

      var leftType = binaryOperation.LeftOperand.Type;
      var rightType = binaryOperation.RightOperand.Type;
      if (TypeHelper.TypesAreEquivalent(leftType, rightType)) return binaryOperation;
      switch (leftType.TypeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
          binaryOperation.RightOperand = TypeInferencer.Convert(binaryOperation.RightOperand, leftType);
          return binaryOperation;
        case PrimitiveTypeCode.NotPrimitive:
          if (leftType.IsEnum || leftType.ResolvedType.IsEnum) {
            binaryOperation.RightOperand = new Conversion() { ValueToConvert = binaryOperation.RightOperand, TypeAfterConversion = leftType };
            return binaryOperation;
          }
          break;
      }
      switch (rightType.TypeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
          binaryOperation.LeftOperand = TypeInferencer.Convert(binaryOperation.LeftOperand, rightType);
          return binaryOperation;
        case PrimitiveTypeCode.NotPrimitive:
          if (rightType.IsEnum || rightType.ResolvedType.IsEnum) {
            binaryOperation.LeftOperand = new Conversion() { ValueToConvert = binaryOperation.LeftOperand, TypeAfterConversion = rightType };
            return binaryOperation;
          }
          break;
      }
      return binaryOperation;
    }

    private Expression ParseEquality() {
      var rightOperand = this.PopOperandStack();
      var leftOperand = this.PopOperandStack();
      if (leftOperand.Type.TypeCode == PrimitiveTypeCode.Boolean && ExpressionHelper.IsIntegralZero(rightOperand))
        return new LogicalNot() { Operand = leftOperand, Type = leftOperand.Type };
      else
        return this.HarmonizeOperands(new Equality() { LeftOperand = leftOperand, RightOperand = rightOperand, Type = this.host.PlatformType.SystemBoolean });
    }

    private Statement ParseBinaryConditionalBranch(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      BinaryOperation condition;
      switch (currentOperation.OperationCode) {
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          condition = this.HarmonizeOperands(this.ParseBinaryOperation(new Equality()));
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
          condition = this.HarmonizeOperands(this.ParseBinaryOperation(new GreaterThanOrEqual()));
          break;
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          condition = this.HarmonizeOperands(this.ParseUnsignedBinaryOperation(new GreaterThanOrEqual()));
          break;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
          condition = this.HarmonizeOperands(this.ParseBinaryOperation(new GreaterThan()));
          break;
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          condition = this.HarmonizeOperands(this.ParseUnsignedBinaryOperation(new GreaterThan()));
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
          condition = this.HarmonizeOperands(this.ParseBinaryOperation(new LessThanOrEqual()));
          break;
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          condition = this.HarmonizeOperands(this.ParseUnsignedBinaryOperation(new LessThanOrEqual()));
          break;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
          condition = this.HarmonizeOperands(this.ParseBinaryOperation(new LessThan()));
          break;
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          condition = this.HarmonizeOperands(this.ParseUnsignedBinaryOperation(new LessThan()));
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
        default:
          condition = this.HarmonizeOperands(this.ParseBinaryOperation(new NotEquality()));
          break;
      }
      condition.Type = this.platformType.SystemBoolean;
      if (this.host.PreserveILLocations) {
        condition.Locations.Add(currentOperation.Location);
      }
      GotoStatement gotoStatement = this.MakeGoto(currentOperation);
      ConditionalStatement ifStatement = new ConditionalStatement();
      ifStatement.Condition = condition;
      ifStatement.TrueBranch = gotoStatement;
      ifStatement.FalseBranch = new EmptyStatement();
      return ifStatement;
    }

    private Expression ParseBoundExpression(Instruction instruction) {
      Contract.Requires(instruction != null);
      IOperation currentOperation = instruction.Operation;
      if (currentOperation.Value == null)
        return new ThisReference();
      BoundExpression result = new BoundExpression();
      result.Alignment = this.alignment;
      result.Definition = currentOperation.Value;
      result.IsVolatile = this.sawVolatile;
      switch (currentOperation.OperationCode) {
        case OperationCode.Ldarg:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S:
          var par = result.Definition as IParameterDefinition;
          Contract.Assume(par != null);
          result.Type = par.IsByReference ? new ManagedPointerTypeReference() { TargetType = par.Type, InternFactory = this.host.InternFactory } :  par.Type;
          break;
        case OperationCode.Ldsfld:
          var field = result.Definition as IFieldReference;
          Contract.Assume(field != null);
          result.Type = field.Type;
          break;
        case OperationCode.Ldfld:
          result.Instance = this.PopOperandStack();
          goto case OperationCode.Ldsfld;
        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S:
          if (this.instructionsThatMakeALastUseOfALocalVersion.Contains(instruction)) {
            this.instructionsThatMakeALastUseOfALocalVersion.Remove(instruction);
            this.bindingsThatMakeALastUseOfALocalVersion.Add(result);
          }
          Contract.Assume(result.Definition is ILocalDefinition);
          var locDef = (ILocalDefinition)result.Definition;
          var local = result.Definition = this.GetLocalWithSourceName(locDef);
          this.numberOfReferencesToLocal[local] =
            this.numberOfReferencesToLocal.ContainsKey(local) ?
            this.numberOfReferencesToLocal[local] + 1 :
            1;
          result.Type = locDef.IsReference ? new ManagedPointerTypeReference() { TargetType = locDef.Type, InternFactory = this.host.InternFactory } :  locDef.Type;
          break;
      }
      this.alignment = 0;
      this.sawVolatile = false;
      return result;
    }

    private MethodCall ParseCall(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      Contract.Assume(currentOperation.Value is IMethodReference);
      IMethodReference methodRef = (IMethodReference)currentOperation.Value;
      MethodCall result = new MethodCall();
      result.IsTailCall = this.sawTailCall;
      foreach (var par in methodRef.Parameters)
        result.Arguments.Add(this.PopOperandStack());
      foreach (var par in methodRef.ExtraParameters)
        result.Arguments.Add(this.PopOperandStack());
      result.Arguments.Reverse();
      //Convert ints to bools and enums
      var i = 0;
      foreach (var par in methodRef.Parameters) {
        Contract.Assume(par != null);
        Contract.Assume(i < result.Arguments.Count);
        Contract.Assume(result.Arguments[i] != null);
        result.Arguments[i] = TypeInferencer.Convert(result.Arguments[i++], par.Type);
        //TODO: special case out arguments and ref arguments
      }
      foreach (var par in methodRef.ExtraParameters) {
        Contract.Assume(par != null);
        Contract.Assume(i < result.Arguments.Count);
        Contract.Assume(result.Arguments[i] != null);
        result.Arguments[i] = TypeInferencer.Convert(result.Arguments[i++], par.Type);
      }
      result.IsVirtualCall = currentOperation.OperationCode == OperationCode.Callvirt;
      result.MethodToCall = methodRef;
      result.Type = methodRef.Type;
      if (!methodRef.IsStatic)
        result.ThisArgument = this.PopOperandStack();
      else
        result.IsStaticCall = true;
      this.sawTailCall = false;
      return result;
    }

    private Expression ParseCastIfPossible(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      CastIfPossible result = new CastIfPossible();
      result.ValueToCast = this.PopOperandStack();
      result.TargetType = (ITypeReference)currentOperation.Value;
      return result;
    }

    private Expression ParseCompileTimeConstant(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      CompileTimeConstant result = new CompileTimeConstant();
      result.Value = currentOperation.Value;
      switch (currentOperation.OperationCode) {
        case OperationCode.Ldc_I4_0: result.Value = 0; break;
        case OperationCode.Ldc_I4_1: result.Value = 1; break;
        case OperationCode.Ldc_I4_2: result.Value = 2; break;
        case OperationCode.Ldc_I4_3: result.Value = 3; break;
        case OperationCode.Ldc_I4_4: result.Value = 4; break;
        case OperationCode.Ldc_I4_5: result.Value = 5; break;
        case OperationCode.Ldc_I4_6: result.Value = 6; break;
        case OperationCode.Ldc_I4_7: result.Value = 7; break;
        case OperationCode.Ldc_I4_8: result.Value = 8; break;
        case OperationCode.Ldc_I4_M1: result.Value = -1; break;
      }
      return result;
    }

    private Expression ParseConversion(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      Conversion result = new Conversion();
      Expression valueToConvert = this.PopOperandStack();
      result.ValueToConvert = valueToConvert;
      switch (currentOperation.OperationCode) {
        case OperationCode.Conv_R_Un:
          result.ValueToConvert = this.ConvertToUnsigned(valueToConvert); break;
        case OperationCode.Conv_Ovf_I_Un:
        case OperationCode.Conv_Ovf_I1_Un:
        case OperationCode.Conv_Ovf_I2_Un:
        case OperationCode.Conv_Ovf_I4_Un:
        case OperationCode.Conv_Ovf_I8_Un:
        case OperationCode.Conv_Ovf_U_Un:
        case OperationCode.Conv_Ovf_U1_Un:
        case OperationCode.Conv_Ovf_U2_Un:
        case OperationCode.Conv_Ovf_U4_Un:
        case OperationCode.Conv_Ovf_U8_Un:
          result.ValueToConvert = this.ConvertToUnsigned(valueToConvert);
          result.CheckNumericRange = true; break;
        case OperationCode.Conv_Ovf_I:
        case OperationCode.Conv_Ovf_I1:
        case OperationCode.Conv_Ovf_I2:
        case OperationCode.Conv_Ovf_I4:
        case OperationCode.Conv_Ovf_I8:
        case OperationCode.Conv_Ovf_U:
        case OperationCode.Conv_Ovf_U1:
        case OperationCode.Conv_Ovf_U2:
        case OperationCode.Conv_Ovf_U4:
        case OperationCode.Conv_Ovf_U8:
          result.CheckNumericRange = true; break;
      }
      switch (currentOperation.OperationCode) {
        case OperationCode.Box:
          Contract.Assume(currentOperation.Value is ITypeReference);
          var type = (ITypeReference)currentOperation.Value;
          var innerConversion = result.ValueToConvert as Conversion;
          if (innerConversion != null)
            innerConversion.TypeAfterConversion = type;
          else
            ((Expression)result.ValueToConvert).Type = type;
          var cc = result.ValueToConvert as CompileTimeConstant;
          if (cc != null) cc.Value = this.ConvertBoxedValue(cc.Value, cc.Type);
          result.TypeAfterConversion = this.platformType.SystemObject;
          break;
        case OperationCode.Castclass:
          result.TypeAfterConversion = (ITypeReference)currentOperation.Value;
          if (result.TypeAfterConversion.IsValueType)
            //This is not legal IL according to ECMA, but the CLR accepts it if the value to convert is a boxed value type.
            //Moreover, the CLR seems to leave the boxed object on the stack if the cast succeeds.
            result = new Conversion() { ValueToConvert = result, TypeAfterConversion = this.platformType.SystemObject };
          break;
        case OperationCode.Conv_I:
        case OperationCode.Conv_Ovf_I:
        case OperationCode.Conv_Ovf_I_Un:
          result.TypeAfterConversion = this.platformType.SystemIntPtr; break;
        case OperationCode.Conv_I1:
        case OperationCode.Conv_Ovf_I1:
        case OperationCode.Conv_Ovf_I1_Un:
          result.TypeAfterConversion = this.platformType.SystemInt8; break;
        case OperationCode.Conv_I2:
        case OperationCode.Conv_Ovf_I2:
        case OperationCode.Conv_Ovf_I2_Un:
          result.TypeAfterConversion = this.platformType.SystemInt16; break;
        case OperationCode.Conv_I4:
        case OperationCode.Conv_Ovf_I4:
        case OperationCode.Conv_Ovf_I4_Un:
          result.TypeAfterConversion = this.platformType.SystemInt32; break;
        case OperationCode.Conv_I8:
        case OperationCode.Conv_Ovf_I8:
        case OperationCode.Conv_Ovf_I8_Un:
          result.TypeAfterConversion = this.platformType.SystemInt64; break;
        case OperationCode.Conv_U:
        case OperationCode.Conv_Ovf_U:
        case OperationCode.Conv_Ovf_U_Un:
          result.TypeAfterConversion = this.platformType.SystemUIntPtr; break;
        case OperationCode.Conv_U1:
        case OperationCode.Conv_Ovf_U1:
        case OperationCode.Conv_Ovf_U1_Un:
          result.TypeAfterConversion = this.platformType.SystemUInt8; break;
        case OperationCode.Conv_U2:
        case OperationCode.Conv_Ovf_U2:
        case OperationCode.Conv_Ovf_U2_Un:
          result.TypeAfterConversion = this.platformType.SystemUInt16; break;
        case OperationCode.Conv_U4:
        case OperationCode.Conv_Ovf_U4:
        case OperationCode.Conv_Ovf_U4_Un:
          result.TypeAfterConversion = this.platformType.SystemUInt32; break;
        case OperationCode.Conv_U8:
        case OperationCode.Conv_Ovf_U8:
        case OperationCode.Conv_Ovf_U8_Un:
          result.TypeAfterConversion = this.platformType.SystemUInt64; break;
        case OperationCode.Conv_R_Un:
          result.TypeAfterConversion = this.platformType.SystemFloat64; break; //TODO: need a type for Float80+
        case OperationCode.Conv_R4:
          result.TypeAfterConversion = this.platformType.SystemFloat32; break;
        case OperationCode.Conv_R8:
          result.TypeAfterConversion = this.platformType.SystemFloat64; break;
        case OperationCode.Unbox:
          result.TypeAfterConversion = Immutable.ManagedPointerType.GetManagedPointerType((ITypeReference)currentOperation.Value, this.host.InternFactory); break;
        case OperationCode.Unbox_Any:
          result.TypeAfterConversion = (ITypeReference)currentOperation.Value; break;
      }
      return result;
    }

    private object ConvertBoxedValue(object ob, ITypeReference typeReference) {
      Contract.Requires(typeReference != null);
      switch (typeReference.TypeCode) {
        case PrimitiveTypeCode.Boolean: { Contract.Assume(ob is int); return ((int)ob) == 1; }
        case PrimitiveTypeCode.Char: { Contract.Assume(ob is int); return (char)((int)ob); }
      }
      return ob;
    }

    private Expression ParseCopyObject() {
      AddressDereference source = new AddressDereference();
      source.Address = this.PopOperandStack();
      AddressDereference addressDeref = new AddressDereference();
      addressDeref.Address = this.PopOperandStack();
      TargetExpression target = new TargetExpression();
      target.Definition = addressDeref;
      Assignment result = new Assignment();
      result.Source = source;
      result.Target = target;
      result.Type = source.Type;
      return result;
    }

    private Expression ParseCreateObjectInstance(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      CreateObjectInstance result = new CreateObjectInstance();
      Contract.Assume(currentOperation.Value is IMethodReference);
      result.MethodToCall = (IMethodReference)currentOperation.Value;
      result.Type = result.MethodToCall.ContainingType;
      foreach (var par in result.MethodToCall.Parameters)
        result.Arguments.Add(this.PopOperandStack());
      result.Arguments.Reverse();
      //Convert ints to bools and enums
      var i = 0;
      foreach (var par in result.MethodToCall.Parameters) {
        Contract.Assume(par != null);
        Contract.Assume(i < result.Arguments.Count);
        Contract.Assume(result.Arguments[i] != null);
        result.Arguments[i] = TypeInferencer.Convert(result.Arguments[i++], par.Type);
      }
      return result;
    }

    private DupValue ParseDup(ITypeReference type) {
      Contract.Requires(type != null);
      var result = new DupValue() { Type = type };
      return result;
    }

    private Statement ParseEndfilter() {
      EndFilter result = new EndFilter();
      result.FilterResult = this.PopOperandStack();
      return result;
    }

    private Expression ParseGetTypeOfTypedReference() {
      GetTypeOfTypedReference result = new GetTypeOfTypedReference();
      result.TypedReference = this.PopOperandStack();
      return result;
    }

    private Expression ParseGetValueOfTypedReference(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      GetValueOfTypedReference result = new GetValueOfTypedReference();
      Contract.Assume(currentOperation.Value is ITypeReference);
      result.TargetType = (ITypeReference)currentOperation.Value;
      result.TypedReference = this.PopOperandStack();
      return result;
    }

    private Statement ParseInitObject(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);

      Contract.Assume(currentOperation.Value is ITypeReference);
      var objectType = (ITypeReference)currentOperation.Value;
      Assignment assignment = new Assignment();
      var addressDeref = new AddressDereference() { Address = this.PopOperandStack(), Type = objectType };
      assignment.Target = new TargetExpression() { Definition = addressDeref, Type = objectType };
      assignment.Source = new DefaultValue() { DefaultValueType = (ITypeReference)currentOperation.Value, Type = objectType };
      assignment.Type = objectType;
      return new ExpressionStatement() { Expression = assignment };
    }

    private Expression ParseMakeTypedReference(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      MakeTypedReference result = new MakeTypedReference();
      Expression operand = this.PopOperandStack();
      Contract.Assume(currentOperation.Value is ITypeReference);
      var type = (ITypeReference)currentOperation.Value;
      operand.Type = Immutable.ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory); ;
      result.Operand = operand;
      return result;
    }

    private BinaryOperation ParseMultiplication(OperationCode currentOpcode) {
      Multiplication multiplication = new Multiplication();
      multiplication.CheckOverflow = currentOpcode != OperationCode.Mul;
      if (currentOpcode == OperationCode.Mul_Ovf_Un) {
        multiplication.TreatOperandsAsUnsignedIntegers = true;
        return this.ParseUnsignedBinaryOperation(multiplication);
      } else
        return this.ParseBinaryOperation(multiplication);
    }

    private Expression ParsePointerCall(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      Contract.Assume(currentOperation.Value is IFunctionPointerTypeReference);
      IFunctionPointerTypeReference funcPointerRef = (IFunctionPointerTypeReference)currentOperation.Value;
      PointerCall result = new PointerCall();
      result.IsTailCall = this.sawTailCall;
      Expression pointer = this.PopOperandStack();
      pointer.Type = funcPointerRef;
      foreach (var par in funcPointerRef.Parameters)
        result.Arguments.Add(this.PopOperandStack());
      if (!funcPointerRef.IsStatic)
        result.Arguments.Add(this.PopOperandStack());
      result.Arguments.Reverse();
      result.Pointer = pointer;
      this.sawTailCall = false;
      return result;
    }

    private Statement ParsePop() {
      ExpressionStatement result = new ExpressionStatement();
      result.Expression = this.PopOperandStack();
      return result;
    }

    private Statement ParseReturn() {
      ReturnStatement result = new ReturnStatement();
      if (this.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void) {
        result.Expression = TypeInferencer.Convert(this.PopOperandStack(), this.MethodDefinition.Type);
      }
      return result;
    }

    private static Expression ParseSizeOf(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      SizeOf result = new SizeOf();
      result.TypeToSize = (ITypeReference)currentOperation.Value;
      return result;
    }

    private Expression ParseStackArrayCreate() {
      StackArrayCreate result = new StackArrayCreate();
      result.Size = this.PopOperandStack();
      result.ElementType = this.host.PlatformType.SystemUInt8;
      return result;
    }

    private BinaryOperation ParseSubtraction(OperationCode currentOpcode) {
      Subtraction subtraction = new Subtraction();
      subtraction.CheckOverflow = currentOpcode != OperationCode.Sub;
      if (currentOpcode == OperationCode.Sub_Ovf_Un) {
        subtraction.TreatOperandsAsUnsignedIntegers = true;
        return this.ParseUnsignedBinaryOperation(subtraction);
      } else
        return this.ParseBinaryOperation(subtraction);
    }

    private Statement ParseSwitchInstruction(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      SwitchInstruction result = new SwitchInstruction();
      result.switchExpression = this.PopOperandStack();
      Contract.Assume(currentOperation.Value is uint[]);
      uint[] branches = (uint[])currentOperation.Value;
      foreach (uint targetAddress in branches) {
        var target = this.GetTargetStatement(targetAddress);
        var gotoBB = new GotoStatement() { TargetStatement = target };
        var key = (uint)gotoBB.TargetStatement.Label.UniqueKey;
        List<IGotoStatement> gotos = this.gotosThatTarget[key];
        if (gotos == null) this.gotosThatTarget[key] = gotos = new List<IGotoStatement>();
        gotos.Add(gotoBB);
        result.SwitchCases.Add(gotoBB);
      }
      return result;
    }

    private Statement ParseThrow() {
      ThrowStatement result = new ThrowStatement();
      result.Exception = this.PopOperandStack();
      return result;
    }

    private static Expression ParseToken(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      TokenOf result = new TokenOf();
      result.Definition = currentOperation.Value;
      return result;
    }

    private Statement ParseUnaryConditionalBranch(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      Expression condition = this.PopOperandStack();
      var castIfPossible = condition as CastIfPossible;
      if (castIfPossible != null) {
        condition = new CheckIfInstance() {
          Locations = castIfPossible.Locations,
          Operand = castIfPossible.ValueToCast,
          TypeToCheck = castIfPossible.TargetType,
          Type = this.host.PlatformType.SystemBoolean,
        };
      } else if (!(condition.Type is Dummy) && condition.Type.TypeCode != PrimitiveTypeCode.Boolean) {
        var defaultValue = new DefaultValue() { DefaultValueType = condition.Type, Type = condition.Type };
        condition = new NotEquality() { LeftOperand = condition, RightOperand = defaultValue, Type = this.host.PlatformType.SystemBoolean };
      }
      GotoStatement gotoStatement = this.MakeGoto(currentOperation);
      ConditionalStatement ifStatement = new ConditionalStatement();
      ifStatement.Condition = condition;
      switch (currentOperation.OperationCode) {
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
          ifStatement.TrueBranch = new EmptyStatement();
          ifStatement.FalseBranch = gotoStatement;
          break;
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
        default:
          ifStatement.TrueBranch = gotoStatement;
          ifStatement.FalseBranch = new EmptyStatement();
          break;
      }
      return ifStatement;
    }

    private Statement ParseUnconditionalBranch(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      GotoStatement gotoStatement = this.MakeGoto(currentOperation);
      return gotoStatement;
    }

    private BinaryOperation ParseUnsignedBinaryOperation(BinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      binaryOperation.RightOperand = this.PopOperandStackAsUnsigned();
      binaryOperation.LeftOperand = this.PopOperandStackAsUnsigned();
      return binaryOperation;
    }

    private Expression ParseUnaryOperation(UnaryOperation unaryOperation) {
      Contract.Requires(unaryOperation != null);
      unaryOperation.Operand = this.PopOperandStack();
      return unaryOperation;
    }

    private Expression ParseVectorLength() {
      VectorLength result = new VectorLength();
      result.Vector = this.PopOperandStack();
      return result;
    }

    private Expression PopOperandStack() {
      Contract.Ensures(Contract.Result<Expression>() != null);
      if (this.operandStack.Count == 0) {
        return new PopValue();
      } else {
        var result = this.operandStack.Pop();
        Contract.Assume(result != null);
        return result;
      }
    }

    private Expression PopOperandStackAsUnsigned() {
      Contract.Ensures(Contract.Result<Expression>() != null);
      if (this.operandStack.Count == 0) {
        Contract.Assume(false);
        return new PopValue();
      } else {
        var result = this.operandStack.Pop();
        Contract.Assume(result != null);
        return this.ConvertToUnsigned(result);
      }
    }

    private GotoStatement MakeGoto(IOperation currentOperation) {
      Contract.Requires(currentOperation != null);
      GotoStatement gotoStatement = new GotoStatement();
      Contract.Assume(currentOperation.Value is uint);
      gotoStatement.TargetStatement = this.GetTargetStatement((uint)currentOperation.Value);
      var key = (uint)gotoStatement.TargetStatement.Label.UniqueKey;
      List<IGotoStatement> gotos = this.gotosThatTarget[key];
      if (gotos == null) this.gotosThatTarget[key] = gotos = new List<IGotoStatement>();
      gotos.Add(gotoStatement);
      return gotoStatement;
    }

    private ILocalDefinition GetLocalWithSourceName(ILocalDefinition localDef) {
      Contract.Requires(localDef != null);
      Contract.Ensures(Contract.Result<ILocalDefinition>() != null);
      return this.sourceMethodBody.GetLocalWithSourceName(localDef);
    }

    private ILabeledStatement GetTargetStatement(uint offset) {
      LabeledStatement result = this.targetStatementFor[offset];
      if (result != null) return result;
      var labeledStatement = new LabeledStatement();
      labeledStatement.Label = this.nameTable.GetNameFor("IL_" + offset.ToString("x4"));
      labeledStatement.Statement = new EmptyStatement();
      this.targetStatementFor.Add(offset, labeledStatement);
      return labeledStatement;
    }

    private static int ConvertToInt(Expression expression) {
      Contract.Requires(expression != null);
      CompileTimeConstant/*?*/ cc = expression as CompileTimeConstant;
      if (cc == null) return 0; //TODO: error
      IConvertible/*?*/ ic = cc.Value as IConvertible;
      if (ic == null) return 0; //TODO: error
      switch (ic.GetTypeCode()) {
        case TypeCode.SByte:
        case TypeCode.Byte:
        case TypeCode.Int16:
        case TypeCode.UInt16:
        case TypeCode.Int32:
          return ic.ToInt32(null);
        case TypeCode.Int64:
          return (int)ic.ToInt64(null); //TODO: error
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          return (int)ic.ToUInt64(null); //TODO: error
      }
      return 0; //TODO: error
    }

    private Expression ConvertToUnsigned(Expression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<Expression>() != null);

      CompileTimeConstant/*?*/ cc = expression as CompileTimeConstant;
      if (cc == null) return ConvertToUnsigned2(expression);
      IConvertible/*?*/ ic = cc.Value as IConvertible;
      if (ic == null) {
        if (cc.Value is System.IntPtr) {
          cc.Value = (System.UIntPtr)(ulong)(System.IntPtr)cc.Value;
          cc.Type = this.platformType.SystemUIntPtr;
          return cc;
        }
        return ConvertToUnsigned2(expression);
      }
      switch (ic.GetTypeCode()) {
        case TypeCode.SByte:
          cc.Value = (byte)ic.ToSByte(null); cc.Type = this.platformType.SystemUInt8; break;
        case TypeCode.Int16:
          cc.Value = (ushort)ic.ToInt16(null); cc.Type = this.platformType.SystemUInt16; break;
        case TypeCode.Int32:
          cc.Value = (uint)ic.ToInt32(null); cc.Type = this.platformType.SystemUInt32; break;
        case TypeCode.Int64:
          cc.Value = (ulong)ic.ToInt64(null); cc.Type = this.platformType.SystemUInt64; break;
      }
      return expression;
    }

    private static Expression ConvertToUnsigned2(Expression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<Expression>() != null);

      var resultType = TypeHelper.UnsignedEquivalent(expression.Type);
      if (resultType != expression.Type)
        return new Conversion() { ValueToConvert = expression, TypeAfterConversion = resultType };
      return expression;
    }

  }

}