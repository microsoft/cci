// ==++==
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using Microsoft.Cci.UtilityDataStructures;
using System.Text;

namespace ILtoC {
  partial class Translator {

    private void EmitInstructions(ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction> cdfg) {
      Contract.Requires(cdfg != null);

      foreach (var block in cdfg.AllBlocks) {
        Contract.Assume(block != null);
        this.sourceEmitter.EmitLabel("l" + block.Offset.ToString("x4") + ":");
        this.sourceEmitter.EmitNewLine();

        bool first = true;
        this.previousInstruction = null;

        foreach (var instruction in block.Instructions) {
          Contract.Assume(instruction != null);
          if (first && this.catchHandlerOffsets.Contains(instruction.Operation.Offset+1)) {
            this.sourceEmitter.EmitString("originalException = exception;");
            this.sourceEmitter.EmitNewLine();
          }
          first = false;
          if (!this.EmitInstruction(instruction)) continue;
          if (instruction.Operation.OperationCode != OperationCode.Unaligned_)
            this.previousInstruction = instruction;
          this.sourceEmitter.EmitString(";");
          this.sourceEmitter.EmitNewLine();
        }
      }
    }

    private bool EmitInstruction(Instruction instruction) {
      Contract.Requires(instruction != null);

      if (this.pdbReader != null) {
        foreach (var sloc in this.pdbReader.GetPrimarySourceLocationsFor(instruction.Operation.Location)) {
          Contract.Assume(sloc != null);
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("// ");
          this.sourceEmitter.EmitString(sloc.SourceDocument.Name.Value);
          this.sourceEmitter.EmitString("(");
          this.sourceEmitter.EmitString(sloc.StartLine+"): ");
          this.sourceEmitter.EmitString(sloc.Source.Replace("\r\n", "\\n").Replace("\n", "\\n"));
          this.sourceEmitter.EmitNewLine();
        }
      }

      switch (instruction.Operation.OperationCode) {
        case OperationCode.Add:
        case OperationCode.And:
        case OperationCode.Div:
        case OperationCode.Mul:
        case OperationCode.Or:
        case OperationCode.Rem:
        case OperationCode.Shl:
        case OperationCode.Shr:
        case OperationCode.Sub:
        case OperationCode.Xor:
          this.EmitSimpleBinary(instruction);
          break;

        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
        case OperationCode.Mul_Ovf:
        case OperationCode.Mul_Ovf_Un:
        case OperationCode.Sub_Ovf:
        case OperationCode.Sub_Ovf_Un: 
          this.EmitOverflowCheckedInstruction(instruction);
          break;

        case OperationCode.Arglist:
          this.EmitArglist(instruction);
          break;

        case OperationCode.Array_Addr:
        case OperationCode.Array_Create:
        case OperationCode.Array_Create_WithLowerBound:
        case OperationCode.Array_Get:
        case OperationCode.Array_Set:
          goto default;

        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
          this.EmitBinaryBranchCondition(instruction);
          goto case OperationCode.Br;

        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          this.EmitBinaryBranchCondition(instruction);
          goto case OperationCode.Br;

        case OperationCode.Box:
          this.EmitBox(instruction);
          break;

        case OperationCode.Br:
        case OperationCode.Br_S:
          Contract.Assume(instruction.Operation.Value is uint);
          this.sourceEmitter.EmitString("goto l" + ((uint)instruction.Operation.Value).ToString("x4"));
          break;

        case OperationCode.Break:
          this.sourceEmitter.EmitString("__debugbreak()"); //TODO: need to use a macro that can be made multi-platform
          break;

        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          this.EmitUnaryBranchCondition(instruction);
          goto case OperationCode.Br;

        case OperationCode.Call:
          this.EmitCall(instruction, isVirtual: false, targetMethod: null);
          break;

        case OperationCode.Calli:
          this.EmitCalli(instruction);
          break;

        case OperationCode.Callvirt:
          this.EmitVirtualCall(instruction);
          break;

        case OperationCode.Castclass:
          this.EmitCastClass(instruction);
          break;

        case OperationCode.Ceq:
        case OperationCode.Cgt:
        case OperationCode.Clt:
          this.EmitSignedComparison(instruction, leaveResultInTemp:true);
          break;

        case OperationCode.Cgt_Un:
        case OperationCode.Clt_Un:
          this.EmitUnsignedOrUnorderComparison(instruction, leaveResultInTemp:true);
          break;

        case OperationCode.Ckfinite:
          this.EmitCkfinite(instruction);
          break;

        case OperationCode.Constrained_:
        case OperationCode.No_:
        case OperationCode.Readonly_:
        case OperationCode.Tail_:
        case OperationCode.Unaligned_:
        case OperationCode.Volatile_:
          return false;

        case OperationCode.Conv_I:
        case OperationCode.Conv_I1:
        case OperationCode.Conv_I2:
        case OperationCode.Conv_I4:
        case OperationCode.Conv_I8:
        case OperationCode.Conv_R4:
        case OperationCode.Conv_R8:
        case OperationCode.Conv_U:
        case OperationCode.Conv_U1:
        case OperationCode.Conv_U2:
        case OperationCode.Conv_U4:
        case OperationCode.Conv_U8:
          this.EmitSimpleConversion(instruction);
          break;

        case OperationCode.Conv_R_Un:
          this.EmitUnsignedToRealConversion(instruction);
          break;

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
          this.EmitCheckedConversion(instruction);
          break;

        case OperationCode.Conv_Ovf_I1_Un:
        case OperationCode.Conv_Ovf_I2_Un:
        case OperationCode.Conv_Ovf_I_Un:
        case OperationCode.Conv_Ovf_I4_Un:
        case OperationCode.Conv_Ovf_I8_Un:
        case OperationCode.Conv_Ovf_U_Un:
        case OperationCode.Conv_Ovf_U1_Un:
        case OperationCode.Conv_Ovf_U2_Un:
        case OperationCode.Conv_Ovf_U4_Un:
        case OperationCode.Conv_Ovf_U8_Un:
          this.EmitCheckedUnsignedConversion(instruction);
          break;

        case OperationCode.Cpblk:
          this.EmitCopyBlock(instruction);
          break;

        case OperationCode.Cpobj:
          this.EmitCopyObject(instruction);
          break;

        case OperationCode.Div_Un:
        case OperationCode.Rem_Un:
        case OperationCode.Shr_Un:
          this.EmitUnsignedBinaryOperation(instruction, leaveResultInTemp:true);
          break;

        case OperationCode.Dup:
        case OperationCode.Nop:
        case OperationCode.Pop:
          return false;

        case OperationCode.Endfilter:
          goto default;

        case OperationCode.Endfinally:
          this.EmitEndfinally(instruction);
          goto default;

        case OperationCode.Initblk:
          // TODO System.NullReferenceException can be thrown if an invalid address is detected.
          goto default;

        case OperationCode.Initobj:
          this.EmitInitObj(instruction);
          break;

        case OperationCode.Isinst:
          this.EmitIsinst(instruction);
          break;

        case OperationCode.Jmp:
          goto default;

        case OperationCode.Ldarg:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S:
          this.EmitLoadArgument(instruction);
          break;

        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
          this.EmitLoadArgumentAddress(instruction);
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
          this.EmitConstantReference(instruction);
          break;

        case OperationCode.Ldc_R4:
          this.EmitSingleConstantReference(instruction);
          break;

        case OperationCode.Ldc_R8:
          this.EmitDoubleConstantReference(instruction);
          break;

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
          this.EmitLoadElem(instruction);
          break;

        case OperationCode.Ldelema:
          this.EmitLoadElema(instruction);
          break;

        case OperationCode.Ldfld:
        case OperationCode.Ldsfld:
          this.EmitLoadField(instruction, loadAddress:false);
          break;

        case OperationCode.Ldflda:
        case OperationCode.Ldsflda:
          this.EmitLoadField(instruction, loadAddress: true);
          break;

        case OperationCode.Ldftn:
          this.EmitFunctionPointerExpression(instruction);
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
          this.EmitLoadIndirect(instruction);
          break;

        case OperationCode.Ldlen:
          this.EmitLoadLen(instruction);
          break;

        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S:
          this.EmitLoadLocal(instruction, loadAddress: false);
          break;

        case OperationCode.Ldloca:
        case OperationCode.Ldloca_S:
          this.EmitLoadLocal(instruction, loadAddress: true);
          break;

        case OperationCode.Ldnull:
          this.EmitNull(instruction);
          break;

        case OperationCode.Ldobj:
          this.EmitLoadObject(instruction);
          break;

        case OperationCode.Ldstr:
          this.EmitLoadString(instruction);
          break;

        case OperationCode.Ldtoken:
          this.EmitLoadToken(instruction);
          break;

        case OperationCode.Ldvirtftn:
          this.EmitVirtualFunctionPointerExpression(instruction);
          break;

        case OperationCode.Leave:
        case OperationCode.Leave_S:
          Contract.Assume(instruction.Operation.Value != null);
          this.EmitLeave((uint)instruction.Operation.Offset, (uint)instruction.Operation.Value, isLeavingMethod: false);
          goto case OperationCode.Br;

        case OperationCode.Localloc:
          this.EmitLocalloc(instruction);
          break;

        case OperationCode.Mkrefany:
          this.EmitMakeRefAnyType(instruction);
          break;

        case OperationCode.Neg:
        case OperationCode.Not:
          this.EmitUnaryOperation(instruction);
          break;

        case OperationCode.Newarr:
          this.EmitNewArray(instruction);
          break;

        case OperationCode.Newobj:
          this.EmitNewObject(instruction);
          break;

        case OperationCode.Refanytype:
          this.EmitRefAnyType(instruction);
          break;

        case OperationCode.Refanyval:
          this.EmitRefAnyValue(instruction);
          break;

        case OperationCode.Rethrow:
          this.EmitRethrow(instruction);
          break;

        case OperationCode.Ret:
          this.EmitReturn(instruction);
          break;

        case OperationCode.Sizeof:
          this.EmitSizeof(instruction);
          break;

        case OperationCode.Starg:
        case OperationCode.Starg_S:
          this.EmitStoreArgument(instruction);
          break;

        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
          this.EmitStoreLocal(instruction);
          break;

        case OperationCode.Stelem:
        case OperationCode.Stelem_I:
        case OperationCode.Stelem_I1:
        case OperationCode.Stelem_I2:
        case OperationCode.Stelem_I4:
        case OperationCode.Stelem_I8:
        case OperationCode.Stelem_R4:
        case OperationCode.Stelem_R8:
        case OperationCode.Stelem_Ref:
          this.EmitStoreElement(instruction);
          break;

        case OperationCode.Stfld:
        case OperationCode.Stsfld:
          this.EmitStoreField(instruction);
          break;

        case OperationCode.Stind_I:
        case OperationCode.Stind_I1:
        case OperationCode.Stind_I2:
        case OperationCode.Stind_I4:
        case OperationCode.Stind_I8:
        case OperationCode.Stind_R4:
        case OperationCode.Stind_R8:
        case OperationCode.Stind_Ref:
          this.EmitStoreIndirect(instruction);
          break;

        case OperationCode.Stobj:
          this.EmitStoreObject(instruction);
          break;

        case OperationCode.Switch:
          this.EmitSwitch(instruction);
          break;

        case OperationCode.Throw:
          this.EmitThrow(instruction);
          break;

        case OperationCode.Unbox:
          this.EmitUnbox(instruction);
          break;

        case OperationCode.Unbox_Any:
          this.EmitUnboxAny(instruction);
          break;

        default:
          this.sourceEmitter.EmitString("//");
          this.sourceEmitter.EmitString(instruction.Operation.OperationCode.ToString());
          if (instruction.Operand1 != null) {
            this.sourceEmitter.EmitString(" ");
            var opndLocal = instruction.Operand1.result;
            if (opndLocal != null)
              this.sourceEmitter.EmitString(opndLocal.name ?? "");
            else
              this.sourceEmitter.EmitString((instruction.Operand1.Operation.Value ?? "").ToString());
          }
          break;
      }
      return true;
    }

    private bool CheckForNoPrefixWithFlag(OperationCheckFlags flag) {
      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.No_ && flag.CompareTo(previousInstruction.Operation.Value) == 0)
        return true;
      return false;
    }

    private void EmitAdjustPointerToDataFromHeader(string name) {
      Contract.Requires(name != null);
      this.sourceEmitter.EmitString(name);
      this.sourceEmitter.EmitString(" += ");
      this.sourceEmitter.EmitString("sizeof(struct ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(this.host.PlatformType.SystemObject));
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitAdjustPointerToHeaderFromData() {
      this.sourceEmitter.EmitString(" - sizeof(struct ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(this.host.PlatformType.SystemObject));
      this.sourceEmitter.EmitString(")");
    }

    private void EmitArglist(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("va_start(*((va_list*)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString("), _extraArgumentTypes);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("((intptr_t*)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(")[1] = _extraArgumentTypes");
    }

    private void EmitBox(Instruction instruction) {
      Contract.Requires(instruction != null);

      var targetType = instruction.Operation.Value as ITypeReference;
      Contract.Assume(targetType != null);
      if (!targetType.ResolvedType.IsValueType) {
        //Can happen when translating a specialized method where a type parameter was replaced with a reference type.
        this.EmitInstructionResultName(instruction);
        this.sourceEmitter.EmitString(" = ");
        this.EmitInstructionReference(instruction.Operand1);
        return; 
      }
      this.EmitBox(instruction, targetType);
    }

    private void EmitBox(Instruction instruction, ITypeReference targetType) {
      Contract.Requires(instruction != null);
      Contract.Requires(targetType != null);

      //TODO: need special case handling for Nullable<T>

      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t boxed_object = (uintptr_t)malloc(sizeof(struct ");
      var systemObject = this.GetMangledTypeName(this.host.PlatformType.SystemObject);
      this.sourceEmitter.EmitString(systemObject);
      this.sourceEmitter.EmitString(") + sizeof(struct ");
      var unboxedType = this.GetMangledTypeName(targetType);
      this.sourceEmitter.EmitString(unboxedType);
      this.sourceEmitter.EmitString("));");
      this.sourceEmitter.EmitNewLine();
      this.EmitOutOfMemoryCheck(instruction, "boxed_object");
      this.EmitAdjustPointerToDataFromHeader("boxed_object");

      //The next two calls will initialize all of the memory, so we do not need to call memset.
      this.sourceEmitter.EmitString("SetType(boxed_object, ");
      this.sourceEmitter.EmitString(unboxedType);
      this.sourceEmitter.EmitString("_typeObject);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("memcpy((void*)(boxed_object");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(" + sizeof(struct ");
      this.sourceEmitter.EmitString(systemObject);
      this.sourceEmitter.EmitString(")), &");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", sizeof(struct ");
      this.sourceEmitter.EmitString(unboxedType);
      this.sourceEmitter.EmitString("));");
      this.sourceEmitter.EmitNewLine();
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = boxed_object;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitBinaryBranchCondition(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("if (");
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Bge:
        case OperationCode.Bge_S:
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Ble:
        case OperationCode.Ble_S:
        case OperationCode.Blt:
        case OperationCode.Blt_S:
          this.EmitSignedComparison(instruction, leaveResultInTemp:false);
          break;

        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          this.EmitUnsignedOrUnorderComparison(instruction, leaveResultInTemp:false);
          break;

        default:
          Contract.Assume(false);
          break;
      }
      this.sourceEmitter.EmitString(") ");
    }

    private void EmitCall(Instruction instruction, bool isVirtual, IMethodDefinition targetMethod /*?*/, bool dereferenceVirtualPtr = true, bool dereferencePointer = false, bool needsBoxing = false) {
      Contract.Requires(instruction != null);

      var methodToCall = instruction.Operation.Value as IMethodReference;
      Contract.Assume(methodToCall != null);
      if (!methodToCall.IsStatic && targetMethod == null) {
        Contract.Assume(instruction.Operand1 != null);
        this.EmitNullReferenceCheck(instruction.Operation.Offset+2, instruction.Operand1);
        var mptr = instruction.Operand1.Type as IManagedPointerTypeReference;
        if (needsBoxing || (mptr != null && !(methodToCall.ContainingType.ResolvedType.IsValueType))) {
          // If the target type is a reference type then there is no need to box
          if (mptr != null && mptr.TargetType.ResolvedType.IsValueType) {
            //Need to box the argument before the call.
            this.EmitBox(instruction, mptr.TargetType);
            this.sourceEmitter.EmitString(";");
            this.sourceEmitter.EmitNewLine();
          }
        }
      }
  
      var m = methodToCall.ResolvedMethod;
      if (m.ContainingTypeDefinition.InternedKey == this.host.PlatformType.SystemObject.InternedKey) {
        if (methodToCall.Name == this.callFunctionPointer || methodToCall.Name == this.callFunctionPointer2) {
          this.sourceEmitter.EmitString("exception = ((uintptr_t (*)(uintptr_t))");
          this.EmitInstructionReference(instruction.Operand1);
          this.sourceEmitter.EmitString(")(");
          var args = instruction.Operand2 as Instruction[];
          Contract.Assume(args != null && args.Length >= 1);
          this.EmitInstructionReference(args[0]);
          if (methodToCall.Name == this.callFunctionPointer2) {
            Contract.Assume(args.Length >= 2);
            this.sourceEmitter.EmitString(", ");
            this.EmitInstructionReference(args[1]);
          }
          this.sourceEmitter.EmitString(");");
          this.sourceEmitter.EmitNewLine();
          this.EmitExceptionHandler(instruction);
          return;
        }
        if (m.Name == this.getAs || m.Name == this.getAsPointer) {
          this.EmitInstructionResultName(instruction);
          this.sourceEmitter.EmitString(" = ");
          this.EmitInstructionReference(instruction.Operand1);
          return;
        }
      } else if (m.ContainingTypeDefinition.InternedKey == this.runtimeHelpers.InternedKey) {
        if (m.Name == this.getOffsetToStringData) {
          this.EmitInstructionResultName(instruction);
          this.sourceEmitter.EmitString(" = sizeof(uint32_t)");
          return;
        }
      }
      var cRuntimeMethod = AttributeHelper.Contains(methodToCall.ResolvedMethod.Attributes, this.cRuntimeAttribute);
      if (cRuntimeMethod) {
        this.EmitStackSlotAssignmentPrefix(instruction);
        if (methodToCall.Type.TypeCode != PrimitiveTypeCode.Void && !IsScalarInC(methodToCall.Type.ResolvedType))
          this.sourceEmitter.EmitString("(uintptr_t)");
        this.sourceEmitter.EmitString(methodToCall.Name.Value);
        var genMethInst = methodToCall as IGenericMethodInstanceReference;
        if (genMethInst != null) {
          this.sourceEmitter.EmitString("(");
          this.EmitInstructionReference(instruction.Operand1); 
          this.sourceEmitter.EmitString(", ");
          foreach (var genArg in genMethInst.GenericArguments) {
            Contract.Assume(genArg != null);
            this.EmitTypeReference(genArg);
            break;
          }
          this.sourceEmitter.EmitString(")");
          return;
        }
      } else {
        this.sourceEmitter.EmitString("exception");
        this.sourceEmitter.EmitString(" = ");
        if (targetMethod != null) {
          this.sourceEmitter.EmitString(this.GetMangledMethodName(targetMethod));
        } else {
          if (isVirtual)
            this.sourceEmitter.EmitString("((");
          this.sourceEmitter.EmitString(this.GetMangledMethodName(methodToCall));
          if (isVirtual) {
            this.sourceEmitter.EmitString("_ptr)");
            if (dereferenceVirtualPtr)
              this.sourceEmitter.EmitString("*");
            this.sourceEmitter.EmitString("virtualPtr)");
          }
        }
      }
      this.sourceEmitter.EmitString("(");
      bool first = true;
      if (instruction.Operand1 != null) {
        first = false;
        this.EmitMethodArg(instruction.Operand1, cRuntimeMethod, dereferencePointer);
        this.EmitMethodArgs(instruction.Operand2 as Instruction[], cRuntimeMethod, first: false);
      }
      if (instruction.result != null && !cRuntimeMethod) {
        if (!first) this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString("(uintptr_t)&");
        this.EmitInstructionResultName(instruction);
      }
      this.sourceEmitter.EmitString(")");

      if (!cRuntimeMethod) {
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.EmitExceptionHandler(instruction);
      }
    }

    private void EmitCalli(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operand1 != null);
      this.sourceEmitter.EmitString("exception = ((");
      this.EmitNonPrimitiveTypeReference(instruction.Operand1.Type);
      this.sourceEmitter.EmitString(")(");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString("))(");
      var restOfOperands = instruction.Operand2 as Instruction[];
      Contract.Assume(restOfOperands != null);
      this.EmitMethodArgs(restOfOperands, cRuntimeMethod: false, first: true);
      if (restOfOperands.Length != 0)
        this.sourceEmitter.EmitString(", ");
      if (instruction.result != null) {
        this.sourceEmitter.EmitString("(uintptr_t)&");
        this.EmitInstructionResultName(instruction);
      }
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitExceptionHandler(instruction);
    }

    private unsafe void EmitDoubleConstantReference(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operation.Value is double);
      var d = (double)instruction.Operation.Value;
      var i = *(ulong*)&d;
      var hex = i.ToString("x8");
      this.sourceEmitter.EmitString("{uint64_t doubleAsHex = 0x"+hex+"; ");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = *(double*)&doubleAsHex;} /*");
      this.sourceEmitter.EmitString(d.ToString());
      this.sourceEmitter.EmitString("*/");
    }

    private void EmitMethodArgs(Instruction[]/*?*/ restOfOperands, bool cRuntimeMethod, bool first) {

      if (restOfOperands != null) {
        foreach (var operand in restOfOperands) {
          Contract.Assume(operand != null);
          if (first) first = false; else this.sourceEmitter.EmitString(", ");
          this.EmitMethodArg(operand, cRuntimeMethod);
        }
      }
    }

    private void EmitMethodArg(Instruction instruction, bool cRuntimeMethod, bool dereferencePointer = false) {
      Contract.Requires(instruction != null);

      var operandType = instruction.Type.ResolvedType;
      if (!IsScalarInC(operandType)) {
        if (cRuntimeMethod) {
          this.sourceEmitter.EmitString("(void*)");
          if (operandType.IsValueType) this.sourceEmitter.EmitString("&");
        } else if (operandType.IsValueType)
          this.sourceEmitter.EmitString("(uintptr_t)&");
      } else if (cRuntimeMethod && (operandType.TypeCode == PrimitiveTypeCode.UIntPtr || operandType.TypeCode == PrimitiveTypeCode.IntPtr)) {
        this.sourceEmitter.EmitString("(void*)");
      }
      if (dereferencePointer)
        this.sourceEmitter.EmitString("*(uintptr_t*)");
      this.EmitInstructionReference(instruction);
    }

    private void EmitCastClass(Instruction instruction) {
      Contract.Requires(instruction != null);

      // If there is a no. prefix there is no need to perform any typecheks
      if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoTypeCheck)) {
        this.EmitInstructionResultName(instruction);
        this.sourceEmitter.EmitString(" = ");
        this.EmitInstructionReference(instruction.Operand1);
        return;
      }

      var typeRef = instruction.Operation.Value as ITypeReference;
      Contract.Assume(typeRef != null);
      var arrayTypeRef = typeRef as IArrayTypeReference;
      if (arrayTypeRef != null) {
        this.sourceEmitter.EmitString("exception = CastAsArray(");
        typeRef = arrayTypeRef.ElementType;
      } else {
        var typeDef = typeRef.ResolvedType;
        var ntd = typeDef as INamedTypeDefinition;
        if (ntd != null && ntd.IsClass) {
          //Getting the C compiler to work out that the depth of the type is a compile time constant at this point requires non local reasoning over the heap.
          //Let's help out the C compiler, by inlining and specializing casting, since it is easy for us to figure out the depth and, moreover,
          //we know that the target type of the cast is not an interface or a structural type.
          var depth = this.GetTypeDepth(ntd);
          switch (depth) {
            case 0: this.EmitFastBaseClassCheck(instruction, ntd, depth, this.baseClass0); return;
            case 1: this.EmitFastBaseClassCheck(instruction, ntd, depth, this.baseClass1); return;
            case 2: this.EmitFastBaseClassCheck(instruction, ntd, depth, this.baseClass2); return;
            case 3: this.EmitFastBaseClassCheck(instruction, ntd, depth, this.baseClass3); return;
            case 4: this.EmitFastBaseClassCheck(instruction, ntd, depth, this.baseClass4); return;
            case 5: this.EmitFastBaseClassCheck(instruction, ntd, depth, this.baseClass5); return;
            default: this.EmitFastBaseClassCheck(instruction, ntd, depth, this.baseClasses6andBeyond); return;
          }
        }
        if (typeDef.IsInterface) {
          this.EmitFastInterfaceCheck(instruction, typeDef);
          return;
        }
        //Can get here if typeDef is a pointer or managed reference or generic type instance
        this.sourceEmitter.EmitString("exception = Cast(");
      }
      Contract.Assume(instruction.Operand1 != null);
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.EmitTypeObjectReference(typeRef);
      if (arrayTypeRef != null)
        this.sourceEmitter.EmitString(", " + arrayTypeRef.Rank);
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitExceptionHandler(instruction);
    }

    private void EmitFastBaseClassCheck(Instruction instruction, INamedTypeDefinition derivedType, 
      uint depth, IFieldDefinition baseClassField) {
      Contract.Requires(instruction != null);
      Contract.Requires(derivedType != null);
      Contract.Requires(baseClassField != null);

      //Only check cast if operand is not null
      this.sourceEmitter.EmitString("if (");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(" != 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get the type of the object to cast
      this.sourceEmitter.EmitString("uintptr_t objectType = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemObject);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.EmitInstructionReference(instruction.Operand1);
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //We expect the object type to be the same as the derived type in most cases, so check this using only locals.
      this.sourceEmitter.EmitString("if (objectType != ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(derivedType));
      this.sourceEmitter.EmitString("_typeObject) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get the appropriate baseClass from the type of the object to cast
      this.sourceEmitter.EmitString("uintptr_t baseClassType = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemType);
      this.sourceEmitter.EmitString(")( objectType");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString("))->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(baseClassField));
      if (depth >= 6) this.sourceEmitter.EmitString("["+(depth-6)+"]");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //And now compare and throw if not the same.
      this.sourceEmitter.EmitString("if (baseClassType != ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(derivedType));
      this.sourceEmitter.EmitString("_typeObject) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t invalidCastException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetInvalidCastException((uintptr_t)&invalidCastException);");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, this.invalidCastExceptionType, "invalidCastException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Close the object type check if
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Close the null check if
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //If execution gets to the point in the emitted code, the cast succeeded and we just need to emit code to assign the operand to the instruction result
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = ");
      this.EmitInstructionReference(instruction.Operand1);
    }

    private void EmitFastInterfaceCheck(Instruction instruction, ITypeDefinition interfaceType) {
      Contract.Requires(instruction != null);
      Contract.Requires(interfaceType != null);

      //Only check cast if operand is not null
      this.sourceEmitter.EmitString("if (");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(" != 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get the type of the object to cast
      this.sourceEmitter.EmitString("uintptr_t objectType = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemObject);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.EmitInstructionReference(instruction.Operand1);
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //Get the implemented interface map from the object type
      this.sourceEmitter.EmitString("uint32_t* map = (uint32_t*)((");
      this.EmitTypeReference(this.host.PlatformType.SystemType);
      this.sourceEmitter.EmitString(")(objectType");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString("))->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.implementedInterfaceMapField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //Get the interface index from the target type
      this.sourceEmitter.EmitString("uint32_t ifaceIndex = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemType);
      this.sourceEmitter.EmitString(")(");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(interfaceType));
      this.sourceEmitter.EmitString("_typeObject");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString("))->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.interfaceIndexField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //Only check the interface map if the index is in range
      this.sourceEmitter.EmitString("if (ifaceIndex < 60*4 || ifaceIndex < *map) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get a block of 16 map entries into a local
      this.sourceEmitter.EmitString("uint32_t block = map[(ifaceIndex>>4)+1];");
      this.sourceEmitter.EmitNewLine();

      //Get the actual 2-bit entry into a local
      this.sourceEmitter.EmitString("uint32_t entry = (block >> ((ifaceIndex & 0xF)*2))&3;");
      this.sourceEmitter.EmitNewLine();

      //If the entry is 2 the the cast has succeeded
      this.sourceEmitter.EmitString("if (entry == 2) goto castSucceeded"+instruction.Operation.Offset+";");
      this.sourceEmitter.EmitNewLine();

      //If the entry is 1 then the cast has failed, so throw
      this.sourceEmitter.EmitString("if (entry == 1) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t invalidCastException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetInvalidCastException((uintptr_t)&invalidCastException);");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, this.invalidCastExceptionType, "invalidCastException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Close if if (ifaceIndex < 60*4 || ifaceIndex < *map)
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Otherwise invoke the helper. Either because the interface map has no entry for this interface, or because the entry is 0
      this.sourceEmitter.EmitString("exception = Cast(");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(interfaceType));
      this.sourceEmitter.EmitString("_typeObject");
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("if (exception) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, null, "exception");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Close if (operand1 != null) 
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //If we get here the cast has succeeded and we can assign the operand to the result.
      this.sourceEmitter.EmitLabel("castSucceeded"+instruction.Operation.Offset+":");
      this.sourceEmitter.EmitNewLine();
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = ");
      this.EmitInstructionReference(instruction.Operand1);
    }

    private void EmitCheckedConversion(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);

      string srcType = null;
      Contract.Assume(instruction.Operand1 != null);
      switch (instruction.Operand1.Type.TypeCode) {
        case PrimitiveTypeCode.Int16: srcType = "int16_t"; break;
        case PrimitiveTypeCode.Int32: srcType = "int32_t"; break;
        case PrimitiveTypeCode.Int64: srcType = "int64_t"; break;
        case PrimitiveTypeCode.IntPtr: srcType = "intptr_t"; break;
        case PrimitiveTypeCode.UInt16: srcType = "uint16_t"; break;
        case PrimitiveTypeCode.UInt32: srcType = "uint32_t"; break;
        case PrimitiveTypeCode.UInt64: srcType = "uint64_t"; break;
        case PrimitiveTypeCode.UIntPtr: srcType = "uintptr_t"; break;
      }

      string targetType = "invalid";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Conv_Ovf_I: targetType = "intptr_t"; break;
        case OperationCode.Conv_Ovf_I1: targetType = "int8_t"; break;
        case OperationCode.Conv_Ovf_I2: targetType = "int16_t"; break;
        case OperationCode.Conv_Ovf_I4: targetType = "int32_t"; break;
        case OperationCode.Conv_Ovf_I8: targetType = "int64_t"; break;
      }
      this.EmitConversion(instruction, srcType, targetType);
    }

    private void EmitCheckedUnsignedConversion(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);

      string srcType = null;
      Contract.Assume(instruction.Operand1 != null);
      switch (instruction.Operand1.Type.TypeCode) {
        case PrimitiveTypeCode.UInt16: srcType = "uint16_t"; break;
        case PrimitiveTypeCode.UInt32: srcType = "uint32_t"; break;
        case PrimitiveTypeCode.UInt64: srcType = "uint64_t"; break;
        case PrimitiveTypeCode.UIntPtr: srcType = "uintptr_t"; break;
      }

      string targetType = "invalid";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Conv_Ovf_I_Un: targetType = "intptr_t"; break;
        case OperationCode.Conv_Ovf_I1_Un: targetType = "int8_t"; break;
        case OperationCode.Conv_Ovf_I2_Un: targetType = "int16_t"; break;
        case OperationCode.Conv_Ovf_I4_Un: targetType = "int32_t"; break;
        case OperationCode.Conv_Ovf_I8_Un: targetType = "int64_t"; break;
        case OperationCode.Conv_Ovf_U_Un: targetType = "uintptr_t"; break;
        case OperationCode.Conv_Ovf_U1_Un: targetType = "uint8_t"; break;
        case OperationCode.Conv_Ovf_U2_Un: targetType = "uint16_t"; break;
        case OperationCode.Conv_Ovf_U4_Un: targetType = "uint32_t"; break;
        case OperationCode.Conv_Ovf_U8_Un: targetType = "uint64_t"; break;
      }
      this.EmitConversion(instruction, srcType, targetType);
    }

    private void EmitConversion(Instruction instruction, string /*?*/ srcType, string targetType) {
      Contract.Requires(instruction != null);
      Contract.Requires(targetType != null);

      if (srcType != null) {
        // We need to call helper methods to do the conversion and check for overflow
        this.sourceEmitter.EmitString("Convert_");
        this.sourceEmitter.EmitString(srcType);
        this.sourceEmitter.EmitString("_to_");
        this.sourceEmitter.EmitString(targetType);
        this.sourceEmitter.EmitString("(");
        this.EmitInstructionReference(instruction.Operand1);
        this.sourceEmitter.EmitString(", &overflowFlag" + ");");
        this.sourceEmitter.EmitNewLine();
        this.EmitOverflowCheck(instruction);
      } else {
        // No need to detect overflow, can simply truncate
        this.sourceEmitter.EmitString("((" + targetType + ")");
        this.EmitInstructionReference(instruction.Operand1);
        this.sourceEmitter.EmitString(")");
        this.EmitStackSlotAssignmentPostfix(instruction);
      }
    }

    private void EmitCkfinite(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("if (!isfinite(");
      this.EmitInstructionReference(instruction);
      this.sourceEmitter.EmitString(")) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t arithmeticException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetArithmeticException((uintptr_t)&arithmeticException);");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, this.arithmeticExceptionType, "arithmeticException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitConstantReference(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      Contract.Assume(instruction.Operation.Value != null);
      this.sourceEmitter.EmitString(instruction.Operation.Value.ToString());
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitCopyBlock(Instruction instruction) {
      Contract.Requires(instruction != null);
      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.Volatile_) {
        this.sourceEmitter.EmitString("MemoryBarrier();"); //force all preceding reads to complete
        this.sourceEmitter.EmitNewLine();
      }
      //TODO: check if there is a better way to do this.
      this.sourceEmitter.EmitString("memcpy((void*)");
      Contract.Assume(instruction.Operand1 != null);
      this.EmitInstructionResultName(instruction.Operand1);
      this.sourceEmitter.EmitString(", (void*)");
      Contract.Assume(instruction.Operand2 != null);
      var operands = instruction.Operand2 as Instruction[];
      Contract.Assume(operands != null);
      Contract.Assume(operands.Length == 2);
      Contract.Assume(operands[0] != null);
      this.EmitInstructionResultName(operands[0] as Instruction);
      this.sourceEmitter.EmitString(", ");
      Contract.Assume(operands[1] != null);
      this.EmitInstructionResultName(operands[1] as Instruction);
      this.sourceEmitter.EmitString(")");
    }
    
    private void EmitCopyObject(Instruction instruction) {
      Contract.Requires(instruction != null);
      this.sourceEmitter.EmitString("memcpy((void*)");
      Contract.Assume(instruction.Operand1 != null);
      this.EmitInstructionResultName(instruction.Operand1);
      this.sourceEmitter.EmitString(", (void*)");
      Contract.Assume(instruction.Operand2 is Instruction);
      this.EmitInstructionResultName(instruction.Operand2 as Instruction);
      this.sourceEmitter.EmitString(", ");
      this.sourceEmitter.EmitString("sizeof(");
      var typeRef = instruction.Operation.Value as ITypeReference;
      Contract.Assume(typeRef != null);
      this.EmitTypeReference(typeRef);
      this.sourceEmitter.EmitString("))");
    }
    
    private void EmitEndfinally(Instruction instruction) {
      Contract.Requires(instruction != null);

      foreach (var expInfo in this.currentBody.OperationExceptionInformation) {
        if (expInfo.HandlerKind == HandlerKind.Finally && (instruction.Operation.Offset + 1) == expInfo.HandlerEndOffset) {
          if (this.exceptionSwitchTables.NumberOfEntries(expInfo.TryStartOffset) > 0) {
            this.sourceEmitter.EmitString("goto lexpSwitch" + expInfo.TryStartOffset + ";");
            break;
          }
        }
      }
    }

    private void EmitExceptionHandler(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("if (exception) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, null, "exception");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitFunctionPointerExpression(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      var methodRef = instruction.Operation.Value as IMethodReference;
      Contract.Assume(methodRef != null);
      this.sourceEmitter.EmitString("(uintptr_t)&");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(methodRef));
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitGenericTypeObject(uint index, IFieldDefinition field, bool isStatic) {
      Contract.Requires(field != null);

      this.sourceEmitter.EmitString("(uintptr_t)((void**)((");
      if (isStatic) {
        this.EmitTypeReference(this.host.PlatformType.SystemType);
        this.sourceEmitter.EmitString(")(typeObject");
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(")");
      } else {
        this.EmitTypeReference(this.host.PlatformType.SystemType);
        this.sourceEmitter.EmitString(")(((");
        this.EmitTypeReference(this.host.PlatformType.SystemObject);
        this.sourceEmitter.EmitString(")");
        this.sourceEmitter.EmitString("(");
        this.sourceEmitter.EmitString("_this ");
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(")");
        this.sourceEmitter.EmitString(")->");
        this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(")");
      }
      this.sourceEmitter.EmitString(")->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(field));
      this.sourceEmitter.EmitString(")[" + index + "]");
    }

    private void EmitInitObj(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("memset((void*)");
      Contract.Assume(instruction.Operand1 != null);
      this.sourceEmitter.EmitString("(");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(", 0, sizeof(");
      var typeRef = instruction.Operation.Value as ITypeReference;
      Contract.Assume(typeRef != null);
      this.EmitTypeReference(typeRef, storageLocation: true);
      this.sourceEmitter.EmitString("))");
    }

    private void EmitInstructionReference(Instruction instruction) {
      Contract.Assume(instruction != null);
      var temp = instruction.result;
      Contract.Assume(temp != null);
      Contract.Assume(temp.name != null);
      this.sourceEmitter.EmitString(temp.name);
    }

    private void EmitInstructionResultName(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.result != null);
      Contract.Assume(instruction.result.name != null);
      this.sourceEmitter.EmitString(instruction.result.name);
    }

    private void EmitIsinst(Instruction instruction) {
      Contract.Requires(instruction != null);

      var typeRef = instruction.Operation.Value as ITypeReference;
      Contract.Assume(typeRef != null);
      var arrayTypeRef = typeRef as IArrayTypeReference;
      if (arrayTypeRef != null) {
        this.sourceEmitter.EmitString("TryCastAsArray(");
        typeRef = arrayTypeRef.ElementType;
      } else {
        var typeDef = typeRef.ResolvedType;
        var ntd = typeDef as INamedTypeDefinition;
        if (ntd != null && ntd.IsClass) {
          //Getting the C compiler to work out that the depth of the type is a compile time constant at this point requires non local reasoning over the heap.
          //Let's help out the C compiler, by inlining and specializing casting, since it is easy for us to figure out the depth and, moreover,
          //we know that the target type of the cast is not an interface or a structural type.
          var depth = this.GetTypeDepth(ntd);
          switch (depth) {
            case 0: this.EmitFastInstanceCheck(instruction, ntd, depth, this.baseClass0); return;
            case 1: this.EmitFastInstanceCheck(instruction, ntd, depth, this.baseClass1); return;
            case 2: this.EmitFastInstanceCheck(instruction, ntd, depth, this.baseClass2); return;
            case 3: this.EmitFastInstanceCheck(instruction, ntd, depth, this.baseClass3); return;
            case 4: this.EmitFastInstanceCheck(instruction, ntd, depth, this.baseClass4); return;
            case 5: this.EmitFastInstanceCheck(instruction, ntd, depth, this.baseClass5); return;
            default: this.EmitFastInstanceCheck(instruction, ntd, depth, this.baseClasses6andBeyond); return;
          }
        }
        if (typeDef.IsInterface) {
          this.EmitFastInterfaceInstanceCheck(instruction, typeDef);
          return;
        }
        this.sourceEmitter.EmitString("TryCast(");
      }
      Contract.Assume(instruction.Operand1 != null);
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.EmitTypeObjectReference(typeRef);
      if (arrayTypeRef != null)
        this.sourceEmitter.EmitString(", " + arrayTypeRef.Rank);
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(")");
    }

    private void EmitFastInstanceCheck(Instruction instruction, INamedTypeDefinition targetType, uint depth, IFieldDefinition baseClassField) {

      Contract.Requires(instruction != null);
      Contract.Requires(targetType != null);
      Contract.Requires(baseClassField != null);

      //Just assign the operand to the result.
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = ");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //If the result is not null, we have more work to do.
      this.sourceEmitter.EmitString("if (");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" != 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get the type of the object to cast
      this.sourceEmitter.EmitString("uintptr_t objectType = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemObject);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.EmitInstructionReference(instruction.Operand1);
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //If the types are not the same, we have more work to do.
      this.sourceEmitter.EmitString("if (objectType != ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(targetType));
      this.sourceEmitter.EmitString("_typeObject) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get the appropriate baseClass from the type of the object to cast
      this.sourceEmitter.EmitString("uintptr_t baseClassType = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemType);
      this.sourceEmitter.EmitString(")(objectType");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString("))->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(baseClassField));
      if (depth >= 6) this.sourceEmitter.EmitString("["+(depth-6)+"]");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //And now compare and null out the result if they are not the same.
      this.sourceEmitter.EmitString("if (baseClassType != ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(targetType));
      this.sourceEmitter.EmitString("_typeObject) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = 0;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Close the object type check if
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Close the null check if
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitFastInterfaceInstanceCheck(Instruction instruction, ITypeDefinition interfaceType) {
      Contract.Requires(instruction != null);
      Contract.Requires(interfaceType != null);

      //Just assign the operand to the result.
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = ");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //If the result is not null, we have more work to do.
      this.sourceEmitter.EmitString("if (");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" != 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get the type of the object to cast
      this.sourceEmitter.EmitString("uintptr_t objectType = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemObject);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.EmitInstructionReference(instruction.Operand1);
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //Get the implemented interface map from the object type
      this.sourceEmitter.EmitString("uint32_t* map = (uint32_t*)((");
      this.EmitTypeReference(this.host.PlatformType.SystemType);
      this.sourceEmitter.EmitString(")(objectType");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString("))->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.implementedInterfaceMapField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //Get the interface index from the target type
      this.sourceEmitter.EmitString("uint32_t ifaceIndex = ((");
      this.EmitTypeReference(this.host.PlatformType.SystemType);
      this.sourceEmitter.EmitString(")(");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(interfaceType));
      this.sourceEmitter.EmitString("_typeObject");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString("))->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.interfaceIndexField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //Only do fast check via the interface map if the index is in range
      this.sourceEmitter.EmitString("if (ifaceIndex < 60*4 || ifaceIndex < *map) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      //Get a block of 16 map entries into a local
      this.sourceEmitter.EmitString("uint32_t block = map[(ifaceIndex/16)+1];");
      this.sourceEmitter.EmitNewLine();

      //Get the actual 2-bit entry into a local
      this.sourceEmitter.EmitString("uint32_t entry = (block >> ((ifaceIndex % 16)*2))&3;");
      this.sourceEmitter.EmitNewLine();

      //If the entry is 2 the the cast has succeeded
      this.sourceEmitter.EmitString("if (entry == 2) goto doneWithCast"+instruction.Operation.Offset+";");
      this.sourceEmitter.EmitNewLine();

      //If the entry is 1 then the cast has failed, so null out result
      this.sourceEmitter.EmitString("if (entry == 1) goto castFailed"+instruction.Operation.Offset+";");
      this.sourceEmitter.EmitNewLine();

      //Close if if (ifaceIndex < 60*4 || ifaceIndex < *map)
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //Invoke the helper. Either because the interface map has no entry for this interface, or because the entry is 0
      this.sourceEmitter.EmitString("TryCast(");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(interfaceType));
      this.sourceEmitter.EmitString("_typeObject");
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("goto doneWithCast"+instruction.Operation.Offset+";");
      this.sourceEmitter.EmitNewLine();

      //If we get here the cast has failed and we assign null to the result.
      this.sourceEmitter.EmitLabel("castFailed"+instruction.Operation.Offset+":");
      this.sourceEmitter.EmitNewLine();
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = 0;");
      this.sourceEmitter.EmitNewLine();

      //Close if (operand1 != null) 
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      //If we get here we are done with the cast and result is already initialized.
      this.sourceEmitter.EmitLabel("doneWithCast"+instruction.Operation.Offset+":");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitLoadArgument(Instruction instruction) {
      Contract.Requires(instruction != null);

      var type = instruction.Type.ResolvedType;
      var temp = instruction.result;
      if (temp != null) {
        if ((type.IsValueType && !IsScalarInC(type)) || type == this.vaListType) {
          this.sourceEmitter.EmitString("memcpy((void*)&");
          Contract.Assume(temp.name != null);
          this.sourceEmitter.EmitString(temp.name);
          this.sourceEmitter.EmitString(", (void*)");
        } else {
          Contract.Assume(temp.name != null);
          this.sourceEmitter.EmitString(temp.name);
          this.sourceEmitter.EmitString(" = ");
          temp = null;
        }
      }
      this.EmitParameterReference(instruction.Operation.Value, loadAddress:false);
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitLoadArgumentAddress(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      this.EmitParameterReference(instruction.Operation.Value, loadAddress: true);
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitLoadElem(Instruction instruction) {
      Contract.Requires(instruction != null);

      var elementType = instruction.Type;
      var indexOperand = instruction.Operand2 as Instruction;
      Contract.Assume(indexOperand != null);
      //TODO: if there are no_ prefixes, omit those checks
      this.EmitArrayElementAccessChecks(instruction, indexOperand, null, elementType);
      this.EmitLoadElementAddressInto("element_address", instruction, elementType, indexOperand);
      var opcode = instruction.Operation.OperationCode;

      if (opcode == OperationCode.Ldelem) {
        if (elementType.IsValueType) {
          switch (elementType.TypeCode) {
            case PrimitiveTypeCode.Boolean: opcode = OperationCode.Ldelem_U1; break;
            case PrimitiveTypeCode.Char: opcode = OperationCode.Ldelem_U2; break;
            case PrimitiveTypeCode.Float32: opcode = OperationCode.Ldelem_R4; break;
            case PrimitiveTypeCode.Float64: opcode = OperationCode.Ldelem_R8; break;
            case PrimitiveTypeCode.Int16: opcode = OperationCode.Ldelem_I2; break;
            case PrimitiveTypeCode.Int32: opcode = OperationCode.Ldelem_I4; break;
            case PrimitiveTypeCode.Int64: opcode = OperationCode.Ldelem_I8; break;
            case PrimitiveTypeCode.Int8: opcode = OperationCode.Ldelem_I1; break;
            case PrimitiveTypeCode.IntPtr: opcode = OperationCode.Ldelem_I; break;
            case PrimitiveTypeCode.Pointer: opcode = OperationCode.Ldelem_I; break;
            case PrimitiveTypeCode.Reference: opcode = OperationCode.Ldelem_I; break;
            case PrimitiveTypeCode.String: opcode = OperationCode.Ldelem_I; break;
            case PrimitiveTypeCode.UInt16: opcode = OperationCode.Ldelem_U2; break;
            case PrimitiveTypeCode.UInt32: opcode = OperationCode.Ldelem_U4; break;
            case PrimitiveTypeCode.UInt64: opcode = OperationCode.Ldelem_I8; break;
            case PrimitiveTypeCode.UInt8: opcode = OperationCode.Ldelem_U1; break;
            case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Ldelem_I; break;
            default:
              //TODO: if the size of the value type is known and <= 8, use one of the above opcodes.
              break;
          }
          if (opcode == OperationCode.Ldelem) {
            var temp = instruction.result;
            Contract.Assume(temp != null && temp.name != null);
            this.sourceEmitter.EmitString("memcpy((void*)&"+temp.name+", (void*)&");
            this.sourceEmitter.EmitString("element_address, sizeof(");
            this.EmitTypeReference(elementType, storageLocation: true);
            this.sourceEmitter.EmitString("))");
            return;
          }
        }
      }
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = *(");
      var typeCast = "";
      switch (opcode) {
        case OperationCode.Ldelem:
        case OperationCode.Ldelem_I:
        case OperationCode.Ldelem_Ref: typeCast = "(uintptr_t*)"; break;
        case OperationCode.Ldelem_I1: typeCast = "(int8_t*)"; break;
        case OperationCode.Ldelem_I2: typeCast = "(int16_t*)"; break;
        case OperationCode.Ldelem_I4: typeCast = "(int32_t*)"; break;
        case OperationCode.Ldelem_I8: typeCast = "(int64_t*)"; break;
        case OperationCode.Ldelem_R4: typeCast = "(float*)"; break;
        case OperationCode.Ldelem_R8: typeCast = "(double*)"; break;
        case OperationCode.Ldelem_U1: typeCast = "(uint8_t*)"; break;
        case OperationCode.Ldelem_U2: typeCast = "(uint16_t*)"; break;
        case OperationCode.Ldelem_U4: typeCast = "(uint32_t*)"; break;
      }
      this.sourceEmitter.EmitString(typeCast);
      this.sourceEmitter.EmitString("element_address)");
    }

    private void EmitLoadElema(Instruction instruction) {
      Contract.Requires(instruction != null);

      var elementType = instruction.Operation.Value as ITypeReference;
      Contract.Assume(elementType != null);
      var indexOperand = instruction.Operand2 as Instruction;
      Contract.Assume(indexOperand != null);
      this.EmitArrayElementAccessChecks(instruction, indexOperand, null, elementType);
      Contract.Assume(instruction.result != null && instruction.result.name != null);
      this.EmitLoadElementAddressInto(instruction.result.name, instruction, elementType, indexOperand);
    }

    private void EmitArrayElementAccessChecks(Instruction instruction, Instruction indexOperand, Instruction/*?*/ newElementValue, ITypeReference elementType) {
      Contract.Requires(instruction != null);
      Contract.Requires(elementType != null);

      bool checkGenerated = false;
      Contract.Assume(instruction.Operand1 != null);
      if (newElementValue != null && !elementType.IsValueType) {
        if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoNullCheck)) {
          // Can omit the null check
          if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoRangeCheck)) {
            // Can omit the range check
            if (!CheckForNoPrefixWithFlag(OperationCheckFlags.NoTypeCheck)) {
              // Only need the type check
              checkGenerated = true;
              this.sourceEmitter.EmitString("exception = CheckArrayElementStoreType(");
            }
          } else {
            // Need the range check
            checkGenerated = true;
            if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoTypeCheck)) {
              // Only need the range check 
              this.sourceEmitter.EmitString("exception = CheckArrayElementStoreIndexRange(");
            } else {
              // Need the range check and type check
              this.sourceEmitter.EmitString("exception = CheckArrayElementStoreAndIndexRange(");
            }
          }
        } else {
          // Need the null check
          checkGenerated = true;
          if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoRangeCheck)) {
            // Can omit the range check
            if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoTypeCheck)) {
              // Only need the null check 
              this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);
              this.sourceEmitter.EmitNewLine();
              this.EmitExceptionHandler(instruction);
              this.sourceEmitter.EmitNewLine();
              return;
            } else {
              // Need the null check and type check
              this.sourceEmitter.EmitString("exception = CheckArrayElementStoreAndNullCheck(");
            }
          } else {
            // Need the null check and range check
            if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoTypeCheck)) {
              // Need the null check and range check
              this.sourceEmitter.EmitString("exception = CheckArrayElementStoreNullRefAndIndexRange(");
            } else {
              // Need all 3 checks
              this.sourceEmitter.EmitString("exception = CheckArrayElementStore(");
            }
          }
        }
        
      } else {
        if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoNullCheck)) {
          // Can omit the null check
          if (!CheckForNoPrefixWithFlag(OperationCheckFlags.NoRangeCheck)) {
            // Only need the range check 
            checkGenerated = true;
            this.sourceEmitter.EmitString("exception = CheckArrayElementLoadIndexRange(");
          }
        } else {
          // Need the null check
          checkGenerated = true;
          if (CheckForNoPrefixWithFlag(OperationCheckFlags.NoRangeCheck)) {
            // Only need the null check 
            this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);
            this.sourceEmitter.EmitNewLine();
            this.EmitExceptionHandler(instruction);
            this.sourceEmitter.EmitNewLine();
            return;
          } else {
            // Need null check and range check
            this.sourceEmitter.EmitString("exception = CheckArrayElementLoad(");
          }
        }
      }

      if (!checkGenerated) return;
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.EmitInstructionReference(indexOperand);
      if (newElementValue != null && !elementType.IsValueType) {
        this.sourceEmitter.EmitString(", ");
        this.EmitInstructionReference(newElementValue);
      }
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitExceptionHandler(instruction);
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitLoadElementAddressInto(string tempForAdress, Instruction instruction, ITypeReference elementType, Instruction indexOperand) {
      Contract.Requires(tempForAdress != null);
      Contract.Requires(instruction != null);
      Contract.Requires(elementType != null);
      Contract.Requires(indexOperand != null);

      this.sourceEmitter.EmitString(tempForAdress);
      this.sourceEmitter.EmitString(" = ");
      this.EmitInstructionReference(instruction.Operand1);
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(" + sizeof(struct ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(this.host.PlatformType.SystemArray));
      this.sourceEmitter.EmitString(") + ");
      this.EmitInstructionReference(indexOperand);
      this.sourceEmitter.EmitString(" * sizeof(");
      this.EmitTypeReference(elementType, storageLocation: true);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitLoadLen(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("exception = GetArrayLength(");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitExceptionHandler(instruction);
    }

    private void EmitLoadField(Instruction instruction, bool loadAddress) {
      Contract.Requires(instruction != null);

      var fieldRef = instruction.Operation.Value as IFieldReference;
      Contract.Assume(fieldRef != null);
      var field = fieldRef.ResolvedField;
      var mangledFieldName = this.GetMangledFieldName(field);
      var operand1 = instruction.Operand1;
      if (field.IsStatic) {
        if (field == this.stringEmptyField) {
          this.EmitInstructionResultName(instruction);
          this.sourceEmitter.EmitString(" = ");
          this.sourceEmitter.EmitString(new Mangler().Mangle(""));
          return;
        }
        this.EmitCheckForStaticConstructor(field.ContainingTypeDefinition);
        this.sourceEmitter.EmitString("statics = GetThreadLocalValue(");
        var tlsIndex = "appdomain_static_block_tlsIndex";
        if (AttributeHelper.Contains(field.Attributes, this.threadStaticAttribute)) tlsIndex = "thread_static_block_tlsIndex";
        this.sourceEmitter.EmitString(tlsIndex);
        this.sourceEmitter.EmitString(");");
        this.sourceEmitter.EmitNewLine();
        this.EmitStackSlotAssignmentPrefix(instruction);
        if (loadAddress)
          this.sourceEmitter.EmitString("(uintptr_t)");
        else
          this.sourceEmitter.EmitString("*");
        this.sourceEmitter.EmitString("((");
        this.EmitTypeReference(field.Type, storageLocation: true);
        this.sourceEmitter.EmitString("*");
        this.sourceEmitter.EmitString(")(statics+");
        this.sourceEmitter.EmitString(mangledFieldName);
        this.sourceEmitter.EmitString("))");
      } else {
        Contract.Assume(operand1 != null);
        // If there was a prefix of no. for nullcheck we can omit the nullcheck
        if (!loadAddress && !CheckForNoPrefixWithFlag(OperationCheckFlags.NoNullCheck))
          this.EmitNullReferenceCheck(instruction.Operation.Offset, operand1);
        this.EmitStackSlotAssignmentPrefix(instruction);
        if (loadAddress) this.sourceEmitter.EmitString("(uintptr_t)&");
        var fieldContainer = fieldRef.ContainingType.ResolvedType;
        if (IsScalarInC(fieldContainer)) {
          this.sourceEmitter.EmitString("*((");
          this.EmitTypeReference(fieldContainer);
          this.sourceEmitter.EmitString("*)");
          this.EmitInstructionReference(operand1);
          this.sourceEmitter.EmitString(")");
          return;
        }
        var operand1Type = operand1.Type.ResolvedType;
        if (!operand1Type.IsValueType) {
          this.sourceEmitter.EmitString("((");
          this.EmitTypeReference(fieldContainer);
          if (fieldContainer.IsValueType) this.sourceEmitter.EmitString("*");
          this.sourceEmitter.EmitString(")(");
          this.EmitInstructionReference(operand1);
          this.EmitAdjustPointerToHeaderFromData();
          this.sourceEmitter.EmitString("))");
        } else
          this.EmitInstructionReference(operand1);

        if (operand1Type.IsValueType)
          this.sourceEmitter.EmitString(".");
        else
          this.sourceEmitter.EmitString("->");
        this.sourceEmitter.EmitString(mangledFieldName);
      }
      this.EmitStackSlotAssignmentPostfix(instruction);
      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.Volatile_) {
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("MemoryBarrier()"); //Force this read to complete before any subsequent writes
      }
    }

    private void EmitCheckForStaticConstructor(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);

      if (!this.HasStaticConstructor(typeDefinition)) return;
      this.sourceEmitter.EmitString("CheckIfStaticConstructorNeedsToRunFor");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(typeDefinition));
      this.sourceEmitter.EmitString("();");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitLoadIndirect(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operand1 != null);
      this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);
      this.EmitStackSlotAssignmentPrefix(instruction);
      this.sourceEmitter.EmitString("*((");
      string type = "invalid";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Ldind_I:
          type = "intptr_t"; break;
        case OperationCode.Ldind_I1:
          type = "int8_t"; break;
        case OperationCode.Ldind_I2:
          type = "int16_t"; break;
        case OperationCode.Ldind_I4:
          type = "int32_t"; break;
        case OperationCode.Ldind_I8:
          type = "int64_t"; break;
        case OperationCode.Ldind_R4:
          type = "float"; break;
        case OperationCode.Ldind_R8:
          type = "double"; break;
        case OperationCode.Ldind_Ref:
          type = "uintptr_t"; break;
        case OperationCode.Ldind_U1:
          type = "uint8_t"; break;
        case OperationCode.Ldind_U2:
          type = "uint16_t"; break;
        case OperationCode.Ldind_U4:
          type = "uint32_t"; break;
      }
      this.sourceEmitter.EmitString(type);
      this.sourceEmitter.EmitString("*)");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(")");
      this.EmitStackSlotAssignmentPostfix(instruction);
      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.Volatile_) {
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("MemoryBarrier()"); //Force this read to complete before any subsequent writes
      }
    }

    private void EmitLoadLocal(Instruction instruction, bool loadAddress) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      var local = instruction.Operation.Value as ILocalDefinition;
      Contract.Assume(local != null);
      if (local.IsReference && !loadAddress) 
        this.sourceEmitter.EmitString("(uintptr_t)");
      this.EmitLocalReference(local, loadAddress);
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitLoadString(Instruction instruction) {
      Contract.Requires(instruction != null);

      var str = instruction.Operation.Value as string;
      Contract.Assume(str != null);
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = ");
      this.sourceEmitter.EmitString(new Mangler().Mangle(str));
    }

    private void EmitLoadToken(Instruction instruction) {
      Contract.Requires(instruction != null);

      var typeRef = instruction.Operation.Value as ITypeReference;
      if (typeRef != null) {
        this.sourceEmitter.EmitString("*((uintptr_t*)&");
        this.EmitInstructionResultName(instruction);
        this.sourceEmitter.EmitString(") = ");
        this.EmitTypeObjectReference(typeRef);
      }
    }

    private void EmitLocalloc(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      Contract.Assume(instruction.Operand1 != null);
      this.sourceEmitter.EmitString("(uintptr_t)alloca(");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(")");
      this.EmitStackSlotAssignmentPostfix(instruction);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("if (");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" == 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t stackOverflowException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetStackOverflowException((uintptr_t)&stackOverflowException);");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, this.stackOverflowExceptionType, "stackOverflowException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitLocalReference(object local, bool loadAddress = false) {
      if (loadAddress) this.sourceEmitter.EmitString("(uintptr_t)&");
      Contract.Assume(local is ILocalDefinition);
      var locDef = (ILocalDefinition)local;
      this.sourceEmitter.EmitString(this.GetSanitizedName(locDef.Name));

      if (locDef.IsPinned)
        this.sourceEmitter.EmitString(" /*pinned*/");
      if (this.pdbReader != null) {
        bool isCompilerGenerated;
        var sourceName = this.pdbReader.GetSourceNameFor(locDef, out isCompilerGenerated);
        if (!isCompilerGenerated) {
          this.sourceEmitter.EmitString(" /*");
          this.sourceEmitter.EmitString(TypeHelper.GetTypeName(locDef.Type));
          this.sourceEmitter.EmitString(" ");
          this.sourceEmitter.EmitString(sourceName);
          this.sourceEmitter.EmitString("*/");
        }
      }        
    }

    private void EmitLeave(uint offset, uint value, bool isLeavingMethod) {
      foreach (var expInfo in this.currentBody.OperationExceptionInformation) {
        if (expInfo.HandlerKind == HandlerKind.Finally && offset >= expInfo.TryStartOffset && offset < expInfo.TryEndOffset) {
          if (!isLeavingMethod) {
            if (value < expInfo.TryEndOffset) continue; 
          }
          this.sourceEmitter.EmitString("expSwitchVal" + expInfo.TryStartOffset + " = " + this.exceptionSwitchTables.NumberOfEntries(expInfo.TryStartOffset) + ";");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("goto l" + expInfo.HandlerStartOffset.ToString("x4") + ";");
          this.sourceEmitter.EmitNewLine();
          string label = "l" + value.ToString("x4") + "_" + expInfo.HandlerStartOffset.ToString("x4") + "_" + labelCounter++;
          this.exceptionSwitchTables.Add(expInfo.TryStartOffset, label);
          this.sourceEmitter.EmitLabel(label + ":");
          this.sourceEmitter.EmitNewLine();
        }
      }
    }

    private void EmitLoadObject(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operand1 != null);
      this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);
      Contract.Assume(instruction.result != null);
      Contract.Assume(instruction.result.name != null);

      this.sourceEmitter.EmitString("memcpy((void*)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(", (void*)");
      this.sourceEmitter.EmitString("(");
      if (instruction.Operation.OperationCode == OperationCode.Unbox_Any)
        this.sourceEmitter.EmitString("unboxed");
      else 
        this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(", sizeof(");
      this.EmitTypeReference(instruction.Type, storageLocation: true);
      this.sourceEmitter.EmitString("))");
      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.Volatile_) {
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("MemoryBarrier()"); //Force this read to complete before any subsequent writes
      }
    }

    private void EmitMakeRefAnyType(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("InitializeTypedReference((uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(", ");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.EmitTypeObjectReference((ITypeReference)instruction.Operation.Value);
      this.sourceEmitter.EmitString(")");
    }

    private void EmitNewArray(Instruction instruction) {
      Contract.Requires(instruction != null);

      var arrayType = instruction.Operation.Value as IArrayTypeReference;
      Contract.Assume(arrayType != null);

      // Throw overflowexception if n < 0
      this.sourceEmitter.EmitString("if (");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(" < 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t overflowException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetOverflowException((uintptr_t)&overflowException);");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, this.overflowExceptionType, "overflowException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("} ");
      this.sourceEmitter.EmitElse("else ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t array_to_construct = ");
      var systemArray = this.GetMangledTypeName(this.host.PlatformType.SystemArray);
      this.sourceEmitter.EmitString("(uintptr_t)calloc(1, sizeof(struct ");
      this.sourceEmitter.EmitString(systemArray);
      this.sourceEmitter.EmitString(") + sizeof(");
      this.EmitTypeReference(arrayType.ElementType, storageLocation:true);
      this.sourceEmitter.EmitString(") * ");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitOutOfMemoryCheck(instruction, "array_to_construct");
      this.EmitAdjustPointerToDataFromHeader("array_to_construct");
      this.sourceEmitter.EmitString("InitializeArrayHeader(array_to_construct, ");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.EmitTypeObjectReference(arrayType);
      this.sourceEmitter.EmitString(", ");
      var elemType = arrayType.ElementType;
      this.EmitTypeObjectReference(elemType);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = array_to_construct;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitNewObject(Instruction instruction) {
      Contract.Requires(instruction != null);

      var methodToCall = instruction.Operation.Value as IMethodReference;
      Contract.Assume(methodToCall != null);
      var containingTypeDef = methodToCall.ContainingType.ResolvedType;
      if (containingTypeDef.TypeCode == PrimitiveTypeCode.String) {
        this.EmitNewString(instruction, methodToCall.ResolvedMethod);
        return;
      }
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.EmitTypeReference(containingTypeDef, storageLocation: true);
      this.sourceEmitter.EmitString(" object_to_construct;");
      this.sourceEmitter.EmitNewLine();
      if (!containingTypeDef.IsValueType) {
        this.sourceEmitter.EmitString("object_to_construct = ");
        var containingType = this.GetMangledTypeName(containingTypeDef);
        this.sourceEmitter.EmitString("(uintptr_t)calloc(1, sizeof(struct ");
        this.sourceEmitter.EmitString(containingType);
        this.sourceEmitter.EmitString("));");
        this.sourceEmitter.EmitNewLine();
        this.EmitOutOfMemoryCheck(instruction, "object_to_construct");
        this.EmitAdjustPointerToDataFromHeader("object_to_construct");
        this.sourceEmitter.EmitString("SetType(object_to_construct, ");
        this.EmitTypeObjectReference(containingTypeDef);
        this.sourceEmitter.EmitString(");");
        this.sourceEmitter.EmitNewLine();
      }
      this.sourceEmitter.EmitString("exception = ");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(methodToCall));
      this.sourceEmitter.EmitString("(");
      if (containingTypeDef.IsValueType)
        this.sourceEmitter.EmitString("(uintptr_t)&");
      this.sourceEmitter.EmitString("object_to_construct");
      if (instruction.Operand1 != null) {
        this.sourceEmitter.EmitString(", ");
        var operand1Type = instruction.Operand1.Type.ResolvedType;
        if (operand1Type.IsValueType && !IsScalarInC(operand1Type))
          this.sourceEmitter.EmitString("(uintptr_t)&");
        this.EmitInstructionReference(instruction.Operand1);
        var restOfOperands = instruction.Operand2 as Instruction[];
        if (restOfOperands != null) {
          foreach (var operand in restOfOperands) {
            Contract.Assume(operand != null);
            this.sourceEmitter.EmitString(", ");
            var operandType = operand.Type.ResolvedType;
            if (operandType.IsValueType && !IsScalarInC(operandType))
              this.sourceEmitter.EmitString("(uintptr_t)&");
            this.EmitInstructionReference(operand);
          }
        }
      }
      // When calling the constructor of a delegate we need pass in an extra augument which indicated whether the function pointer we are passing in is Static
      if (methodToCall.ContainingType.ResolvedType.IsDelegate) {
        // TODO currently we mandate that the instruction preceeding a creation of a delegate should load the function pointer on the stack. 
        // At the point of invoking the function pointer we need to know whether it's static or not and this is the only place that we could get that information.
        Contract.Assume(this.previousInstruction != null);
        Contract.Assume(this.previousInstruction.Operation.OperationCode == OperationCode.Ldftn || previousInstruction.Operation.OperationCode == OperationCode.Ldvirtftn);

        var methodRef = this.previousInstruction.Operation.Value as IMethodReference;
        Contract.Assume(methodRef != null);
        if (methodRef.ResolvedMethod.IsStatic)
          this.sourceEmitter.EmitString(", 1");
        else
          this.sourceEmitter.EmitString(", 0");
      }
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitExceptionHandler(instruction);
      this.sourceEmitter.EmitNewLine();
      this.EmitStackSlotAssignmentPrefix(instruction);
      this.sourceEmitter.EmitString("object_to_construct");
      this.EmitStackSlotAssignmentPostfix(instruction);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitNewString(Instruction instruction, IMethodDefinition constructor) {
      Contract.Requires(instruction != null);
      Contract.Requires(constructor != null);

      this.sourceEmitter.EmitString("exception = ");
      var parameters = IteratorHelper.GetAsArray(constructor.Parameters);
      switch (parameters.Length) {
        case 1: {
            var p0 = parameters[0];
            Contract.Assume(p0 != null);
            if (p0.Type is IPointerTypeReference)
              this.sourceEmitter.EmitString("CtorCharPtr(");
            else if (p0.Type is IArrayTypeReference)
              this.sourceEmitter.EmitString("CtorCharArray(");
            else
              Contract.Assume(false);
            this.EmitInstructionReference(instruction.Operand1);
            break;
          }
        case 2: {
            var p0 = parameters[0];
            Contract.Assume(p0 != null);
            var p1 = parameters[1];
            Contract.Assume(p1 != null);
            if (p0.Type.TypeCode == PrimitiveTypeCode.Char && p1.Type.TypeCode == PrimitiveTypeCode.Int32)
              this.sourceEmitter.EmitString("CtorCharCount(");
            else
              Contract.Assume(false);
            this.EmitInstructionReference(instruction.Operand1);
            this.sourceEmitter.EmitString(", ");
            var operands2AndBeyond = instruction.Operand2 as Instruction[];
            Contract.Assume(operands2AndBeyond != null);
            Contract.Assume(operands2AndBeyond.Length == 1);
            this.EmitInstructionReference(operands2AndBeyond[0] as Instruction);
            break;
          }
        case 3: {
            var p0 = parameters[0];
            Contract.Assume(p0 != null);
            var p1 = parameters[1];
            Contract.Assume(p1 != null);
            var p2 = parameters[2];
            Contract.Assume(p2 != null);
            if (p0.Type is IPointerTypeReference && p1.Type.TypeCode == PrimitiveTypeCode.Int32 && p2.Type.TypeCode == PrimitiveTypeCode.Int32)
              this.sourceEmitter.EmitString("CtorCharPtrStartLength(");
            else if (p0.Type is IArrayTypeReference && p1.Type.TypeCode == PrimitiveTypeCode.Int32 && p2.Type.TypeCode == PrimitiveTypeCode.Int32)
              this.sourceEmitter.EmitString("CtorCharArrayStartLength(");
            else
              Contract.Assume(false);
            this.EmitInstructionReference(instruction.Operand1);
            this.sourceEmitter.EmitString(", ");
            var operands2AndBeyond = instruction.Operand2 as Instruction[];
            Contract.Assume(operands2AndBeyond != null);
            Contract.Assume(operands2AndBeyond.Length == 2);
            this.EmitInstructionReference(operands2AndBeyond[0]);
            this.sourceEmitter.EmitString(", ");
            this.EmitInstructionReference(operands2AndBeyond[1]);
            break;
          }
      }
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.EmitExceptionHandler(instruction);
    }

    private void EmitNull(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      this.sourceEmitter.EmitString("0");
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitNullReferenceCheck(uint offset, Instruction operand) {
      Contract.Requires(operand != null);

      if (operand.Operation.OperationCode == OperationCode.Ldarg_0 && !this.currentBody.MethodDefinition.ResolvedMethod.IsStatic) {
        //The this argument will have been null checked at the call site.
        return;
      }
      if (operand.Type is IManagedPointerTypeReference) {
        //The code for constructing the managed pointer will already have done any necessary null checks.
        return;
      }
      if (operand.Type.ResolvedType.IsValueType) {
        //No need for a null check
        return;
      }
      Contract.Assume(operand.result != null);
      Contract.Assume(operand.result.name != null);
      this.sourceEmitter.EmitString("if (");
      this.sourceEmitter.EmitString(operand.result.name);    
      this.sourceEmitter.EmitString(" == 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t nullReferenceException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetNullReferenceException((uintptr_t)&nullReferenceException);");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(offset, this.nullReferenceExceptionType, "nullReferenceException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitOutOfMemoryCheck(Instruction instruction, string mallocResultName) {
      Contract.Requires(instruction != null);
      Contract.Requires(mallocResultName != null);

      this.sourceEmitter.EmitString("if (");
      this.sourceEmitter.EmitString(mallocResultName);
      this.sourceEmitter.EmitString(" == 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("GetOutOfMemoryException((uintptr_t)&exception);"); //Does not fail because it uses a static
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, this.outOfMemoryExceptionType, "exception", emitNullCheck: false);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitOverflowCheckedInstruction(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitInstructionReference(instruction);
      this.sourceEmitter.EmitString(" = ");

      bool unsigned = false;
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Add_Ovf_Un:
          unsigned = true;
          goto case OperationCode.Add_Ovf;
        case OperationCode.Add_Ovf:
          this.sourceEmitter.EmitString("Add_");
          break;
        case OperationCode.Mul_Ovf_Un:
          unsigned = true;
          goto case OperationCode.Mul_Ovf;
        case OperationCode.Mul_Ovf:
          this.sourceEmitter.EmitString("Multiply_");
          break;
        case OperationCode.Sub_Ovf_Un:
          unsigned = true;
          goto case OperationCode.Sub_Ovf;
        case OperationCode.Sub_Ovf:
          this.sourceEmitter.EmitString("Subtract_");
          break;
      }
      Contract.Assume(instruction.Operand1 != null);
      if (unsigned)
        this.EmitTypeReference(TypeHelper.UnsignedEquivalent(instruction.Operand1.Type));
      else
        this.EmitTypeReference(instruction.Operand1.Type);
      this.sourceEmitter.EmitString("_");
      var operand2 = instruction.Operand2 as Instruction;
      Contract.Assume(operand2 != null);
      if (unsigned)
        this.EmitTypeReference(TypeHelper.UnsignedEquivalent(operand2.Type));
      else
        this.EmitTypeReference(operand2.Type);
      this.sourceEmitter.EmitString("(");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", ");
      this.EmitInstructionReference(operand2);
      this.sourceEmitter.EmitString(", &overflowFlag" + ");");
      this.sourceEmitter.EmitNewLine();
      this.EmitOverflowCheck(instruction);
    }

    private void EmitOverflowCheck(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("if (overflowFlag" + " == 1) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("uintptr_t overflowException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetOverflowException((uintptr_t)&overflowException);");
      this.sourceEmitter.EmitNewLine();

      this.EmitThrow(instruction.Operation.Offset, this.overflowExceptionType, "overflowException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }
    
    private void EmitParameterReference(object argument, bool loadAddress = false) {
      var parameter = argument as IParameterDefinition;
      if (parameter == null) {
        Contract.Assume(argument == null);
        if (loadAddress) this.sourceEmitter.EmitString("(uintptr_t)&");
        this.sourceEmitter.EmitString("_this");
      } else {
        var parameterType = parameter.Type.ResolvedType;
        if (loadAddress && (!parameterType.IsValueType || IsScalarInC(parameterType)))
          this.sourceEmitter.EmitString("(uintptr_t)&");
        this.sourceEmitter.EmitString(this.GetSanitizedName(parameter.Name));
      }
    }

    private void EmitRefAnyType(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("GetTypedReferenceType((uintptr_t)&");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(")");
    }

    private void EmitRefAnyValue(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("GetTypedReferenceValue((uintptr_t)&");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(")");
    }

    private void EmitRethrow(Instruction instruction) {
      Contract.Requires(instruction != null);

      // We need to figure out the corresponding catch block in order to get the type of the exception thrown
      foreach (var expInfo in this.currentBody.OperationExceptionInformation) {
        if (expInfo.HandlerKind == HandlerKind.Catch && instruction.Operation.Offset >= expInfo.HandlerStartOffset && instruction.Operation.Offset < expInfo.HandlerEndOffset) {
          this.EmitThrow(instruction.Operation.Offset, expInfo.ExceptionType, "originalException");
          break;
        }
      }
    }

    private void EmitReturn(Instruction instruction) {
      Contract.Requires(instruction != null);

      if (instruction.Operand1 != null) {
        var operandType = TypeHelper.StackType(instruction.Operand1.Type).ResolvedType;
        if ((operandType.IsValueType && !IsScalarInC(operandType)) || operandType == this.vaListType) {
          this.sourceEmitter.EmitString("memcpy((void*)_result, (void*)&");
          this.EmitInstructionReference(instruction.Operand1);
          this.sourceEmitter.EmitString(", sizeof(");
          this.EmitTypeReference(operandType, storageLocation: true);
          this.sourceEmitter.EmitString("))");
        } else {
          this.sourceEmitter.EmitString("*((");
          this.EmitTypeReference(operandType, storageLocation: true);
          this.sourceEmitter.EmitString("*)_result) = ");
          this.EmitInstructionReference(instruction.Operand1);
        }
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
      }
      this.sourceEmitter.EmitString("return 0");
    }

    private void EmitSignedComparison(Instruction instruction, bool leaveResultInTemp) {
      Contract.Requires(instruction != null);

      if (leaveResultInTemp) this.EmitStackSlotAssignmentPrefix(instruction);
      Contract.Assume(instruction.Operand1 != null);
      var operand1 = instruction.Operand1;
      Contract.Assume(instruction.Operand2 is Instruction);
      var operand2 = (Instruction)instruction.Operand2;
      var signedType1 = TypeHelper.SignedEquivalent(operand1.Type);
      if (signedType1 != operand1.Type) {
        this.sourceEmitter.EmitString("((");
        this.EmitTypeReference(signedType1);
        this.sourceEmitter.EmitString(")");
        this.EmitInstructionReference(operand1);
        this.sourceEmitter.EmitString(")");
      } else {
        this.EmitInstructionReference(operand1);
      }

      string comparison = " invalid ";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Beq:
        case OperationCode.Beq_S:
        case OperationCode.Ceq:
          comparison = " == ";
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
          comparison = " >= ";
          break;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
        case OperationCode.Cgt:
          comparison = " > ";
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
          comparison = " <= ";
          break;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
        case OperationCode.Clt:
          comparison = " < ";
          break;
        default:
          Contract.Assume(false);
          break;
      }
      this.sourceEmitter.EmitString(comparison);

      var operand2Type = operand2.Type.ResolvedType;
      var signedType2 = TypeHelper.SignedEquivalent(operand2Type);
      if (signedType2 != operand2Type) {
        this.sourceEmitter.EmitString("((");
        this.EmitTypeReference(signedType2);
        this.sourceEmitter.EmitString(")");
        this.EmitInstructionReference(operand2);
        this.sourceEmitter.EmitString(")");
      } else {
        this.EmitInstructionReference(operand2);
      }
      if (leaveResultInTemp) this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitSimpleBinary(Instruction instruction) {
      Contract.Requires(instruction != null);

      var operand1 = instruction.Operand1;
      Contract.Assume(operand1 != null);
      var operand2 = instruction.Operand2 as Instruction;
      Contract.Assume(operand2 != null);

      bool isDivOrRem = false;
      var opCode = instruction.Operation.OperationCode;
      if (opCode == OperationCode.Div || opCode == OperationCode.Rem) {
        if (IsIntegral(instruction.Type.ResolvedType)) {
          isDivOrRem = true;
          this.EmitDivideByZeroCheck(instruction, operand2);
          this.sourceEmitter.EmitString(" else if (");
          this.EmitInstructionReference(operand2);
          this.sourceEmitter.EmitString(" == -1 && ");
          this.EmitInstructionReference(instruction.Operand1);
          this.sourceEmitter.EmitString(" == ");

          switch (TypeHelper.StackType(instruction.Operand1.Type).TypeCode) {
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              this.sourceEmitter.EmitString(" INT32_MIN ");
              break;
            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
              this.sourceEmitter.EmitString(" INT64_MIN ");
              break;
            case PrimitiveTypeCode.IntPtr:
            case PrimitiveTypeCode.UIntPtr:
              this.sourceEmitter.EmitString(" INTPTR_MIN ");
              break;
            default:
              Contract.Assume(false);
              break;
          }

          this.sourceEmitter.EmitString(")");
          this.sourceEmitter.EmitBlockOpeningDelimiter("{");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("uintptr_t arithmeticException;");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("exception = GetArithmeticException((uintptr_t)&arithmeticException);");
          this.sourceEmitter.EmitNewLine();
          this.EmitThrow(instruction.Operation.Offset, this.arithmeticExceptionType, "arithmeticException");
          this.sourceEmitter.EmitString(";");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitBlockClosingDelimiter("}");
          this.sourceEmitter.EmitNewLine();
        }
      }

      this.EmitStackSlotAssignmentPrefix(instruction);
      if (opCode == OperationCode.Rem && (instruction.Type.TypeCode == PrimitiveTypeCode.Float64 || instruction.Type.TypeCode == PrimitiveTypeCode.Float64)) {
        this.sourceEmitter.EmitString("fmod(");
        this.EmitInstructionReference(operand1);
        this.sourceEmitter.EmitString(", ");
        this.EmitInstructionReference(operand2);
        this.sourceEmitter.EmitString(")");
      } else {
        if (isDivOrRem) {
          var signedType = TypeHelper.SignedEquivalent(operand1.Type);
          if (signedType != operand1.Type) {
            this.sourceEmitter.EmitString("((");
            this.EmitTypeReference(signedType);
            this.sourceEmitter.EmitString(")");
            this.EmitInstructionReference(operand1);
            this.sourceEmitter.EmitString(")");
          } else {
            this.EmitInstructionReference(operand1);
          }
        } else {
          this.EmitInstructionReference(operand1);
        }
        string binaryOp = " invalid ";
        switch (opCode) {
          case OperationCode.Add: binaryOp = " + "; break;
          case OperationCode.And: binaryOp = " & "; break;
          case OperationCode.Ceq: binaryOp = " == "; break;
          case OperationCode.Cgt: binaryOp = " > "; break;
          case OperationCode.Clt: binaryOp = " < "; break;
          case OperationCode.Div: binaryOp = " / "; break;
          case OperationCode.Mul: binaryOp = " * "; break;
          case OperationCode.Or: binaryOp = " | "; break;
          case OperationCode.Rem: binaryOp = " % "; break;
          case OperationCode.Shl: binaryOp = " << "; break;
          case OperationCode.Shr: binaryOp = " >> "; break;
          case OperationCode.Sub: binaryOp = " - "; break;
          case OperationCode.Xor: binaryOp = " ^ "; break;
        }
        this.sourceEmitter.EmitString(binaryOp);
        if (isDivOrRem) {
          var signedType = TypeHelper.SignedEquivalent(operand2.Type);
          if (signedType != operand2.Type) {
            this.sourceEmitter.EmitString("((");
            this.EmitTypeReference(signedType);
            this.sourceEmitter.EmitString(")");
            this.EmitInstructionReference(operand2);
            this.sourceEmitter.EmitString(")");
          } else {
            this.EmitInstructionReference(operand2);
          }
        } else {
          this.EmitInstructionReference(operand2);
        }
      }
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitDivideByZeroCheck(Instruction instruction, Instruction operand2) {
      Contract.Requires(instruction != null);
      Contract.Requires(operand2 != null);

      this.sourceEmitter.EmitString("if (");
      this.EmitInstructionReference(operand2);
      this.sourceEmitter.EmitString(" == 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uintptr_t divideByZeroException;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("exception = GetDivideByZeroException((uintptr_t)&divideByZeroException);");
      this.sourceEmitter.EmitNewLine();
      this.EmitThrow(instruction.Operation.Offset, this.divideByZeroExceptionType, "divideByZeroException");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitSimpleConversion(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      string targetType = "((invalid";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Conv_I: targetType = "((intptr_t)"; break;
        case OperationCode.Conv_I1: targetType = "((int8_t)"; break;
        case OperationCode.Conv_I2: targetType = "((int16_t)"; break;
        case OperationCode.Conv_I4: targetType = "((int32_t)"; break;
        case OperationCode.Conv_I8: targetType = "((int64_t)"; break;
        case OperationCode.Conv_R4: targetType = "((float)"; break;
        case OperationCode.Conv_R8: targetType = "((double)"; break;
        case OperationCode.Conv_U: targetType = "((uintptr_t)"; break;
        case OperationCode.Conv_U1: targetType = "((uint8_t)"; break;
        case OperationCode.Conv_U2: targetType = "((uint16_t)"; break;
        case OperationCode.Conv_U4: targetType = "((uint32_t)"; break;
        case OperationCode.Conv_U8: targetType = "((uint64_t)"; break;
      }
      this.sourceEmitter.EmitString(targetType);
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(")");
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private unsafe void EmitSingleConstantReference(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operation.Value is float);
      var f = (float)instruction.Operation.Value;
      var i = *(uint*)&f;
      var hex = i.ToString("x4");
      this.sourceEmitter.EmitString("{uint32_t floatAsHex = 0x"+hex+"; ");
      this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = *(float*)&floatAsHex;} /*");
      this.sourceEmitter.EmitString(f.ToString());
      this.sourceEmitter.EmitString("*/");
    }

    private void EmitSizeof(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      this.sourceEmitter.EmitString("sizeof(");
      var typeRef = instruction.Operation.Value as ITypeReference;
      Contract.Assume(typeRef != null);
      this.EmitTypeReference(typeRef, storageLocation: true);
      this.sourceEmitter.EmitString(")");
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitStackSlotAssignmentPrefix(Instruction instruction) {
      Contract.Requires(instruction != null);

      var type = instruction.Type.ResolvedType;
      var temp = instruction.result;
      if (temp != null) {
        if ((type.IsValueType && !IsScalarInC(type)) || type == this.vaListType) {
          this.sourceEmitter.EmitString("memcpy((void*)&"+temp.name+", (void*)&");
        } else {
          Contract.Assume(temp.name != null);
          this.sourceEmitter.EmitString(temp.name);
          this.sourceEmitter.EmitString(" = ");
          temp = null;
        }
      }
    }

    private void EmitStackSlotAssignmentPostfix(Instruction instruction) {
      Contract.Requires(instruction != null);

      var type = instruction.Type.ResolvedType;
      var temp = instruction.result;
      if (temp != null) {
        if ((type.IsValueType && !IsScalarInC(type)) || type == this.vaListType) {
          this.sourceEmitter.EmitString(", sizeof(");
          this.EmitTypeReference(type, storageLocation: true);
          this.sourceEmitter.EmitString("))");
        }
      }
    }

    private void EmitStoreArgument(Instruction instruction) {
      Contract.Requires(instruction != null);

      var parameter = instruction.Operation.Value as IParameterDefinition;
      Contract.Assume(parameter != null);
      var parameterType = parameter.Type.ResolvedType;
      if ((parameterType.IsValueType && !IsScalarInC(parameterType)) || parameterType == this.vaListType) {
        this.sourceEmitter.EmitString("memcpy((void*)");
        this.EmitParameterReference(parameter, loadAddress: true);
        this.sourceEmitter.EmitString(", (void*)");
        Contract.Assume(instruction.Operand1 != null);
        if (instruction.Operand1.Type.ResolvedType.IsValueType)
          this.sourceEmitter.EmitString("&");
        this.EmitInstructionReference(instruction.Operand1);
        this.sourceEmitter.EmitString(", sizeof(");
        this.EmitTypeReference(parameterType, storageLocation: true);
        this.sourceEmitter.EmitString("))");
      } else {
        this.EmitParameterReference(parameter);
        this.sourceEmitter.EmitString(" = ");
        this.EmitInstructionReference(instruction.Operand1);
      }
    }

    private void EmitStoreElement(Instruction instruction) {
      Contract.Requires(instruction != null);

      ITypeReference elementType = null;
      var typeCast = "";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Stelem_I: typeCast = "*(intptr_t*)"; elementType = this.host.PlatformType.SystemIntPtr; break;
        case OperationCode.Stelem_I1: typeCast = "*(int8_t*)"; elementType = this.host.PlatformType.SystemInt8; break;
        case OperationCode.Stelem_I2: typeCast = "*(int16_t*)"; elementType = this.host.PlatformType.SystemInt16; break;
        case OperationCode.Stelem_I4: typeCast = "*(int32_t*)"; elementType = this.host.PlatformType.SystemInt32; break;
        case OperationCode.Stelem_I8: typeCast = "*(int64_t*)"; elementType = this.host.PlatformType.SystemInt64; break;
        case OperationCode.Stelem_R4: typeCast = "*(float*)"; elementType = this.host.PlatformType.SystemFloat32; break;
        case OperationCode.Stelem_R8: typeCast = "*(double*)"; elementType = this.host.PlatformType.SystemFloat64; break;
        case OperationCode.Stelem: typeCast = "*(uintptr_t*)"; elementType = instruction.Operation.Value as ITypeReference; break;
        case OperationCode.Stelem_Ref: typeCast = "*(uintptr_t*)"; elementType = this.host.PlatformType.SystemObject; break;
      }
      Contract.Assume(instruction.Operand1 != null);
      var arrayType = instruction.Operand1.Type as IArrayTypeReference;
      if (arrayType != null) elementType = arrayType.ElementType;
      Contract.Assume(elementType != null);
      var indexAndValue = instruction.Operand2 as Instruction[];
      Contract.Assume(indexAndValue != null);
      Contract.Assume(indexAndValue.Length == 2);
      var indexOperand = indexAndValue[0];
      Contract.Assume(indexOperand != null);
      //TODO: if there are no_ prefixes, omit those checks
      this.EmitArrayElementAccessChecks(instruction, indexOperand, indexAndValue[1], elementType);
      this.EmitLoadElementAddressInto("element_address", instruction, elementType, indexOperand);
      this.sourceEmitter.EmitNewLine();
      var opcode = instruction.Operation.OperationCode;
      if (opcode == OperationCode.Stelem) {
        if (elementType.IsValueType) {
          switch (elementType.TypeCode) {
            case PrimitiveTypeCode.Boolean: opcode = OperationCode.Stelem_I1; break;
            case PrimitiveTypeCode.Char: opcode = OperationCode.Stelem_I2; break;
            case PrimitiveTypeCode.Float32: opcode = OperationCode.Stelem_R4; break;
            case PrimitiveTypeCode.Float64: opcode = OperationCode.Stelem_R8; break;
            case PrimitiveTypeCode.Int16: opcode = OperationCode.Stelem_I2; break;
            case PrimitiveTypeCode.Int32: opcode = OperationCode.Stelem_I4; break;
            case PrimitiveTypeCode.Int64: opcode = OperationCode.Stelem_I8; break;
            case PrimitiveTypeCode.Int8: opcode = OperationCode.Stelem_I1; break;
            case PrimitiveTypeCode.IntPtr: opcode = OperationCode.Stelem_I; break;
            case PrimitiveTypeCode.Pointer: opcode = OperationCode.Stelem_I; break;
            case PrimitiveTypeCode.Reference: opcode = OperationCode.Stelem_I; break;
            case PrimitiveTypeCode.String: opcode = OperationCode.Stelem_I; break;
            case PrimitiveTypeCode.UInt16: opcode = OperationCode.Ldelem_U2; break;
            case PrimitiveTypeCode.UInt32: opcode = OperationCode.Stelem_I4; break;
            case PrimitiveTypeCode.UInt64: opcode = OperationCode.Stelem_I8; break;
            case PrimitiveTypeCode.UInt8: opcode = OperationCode.Stelem_I1; break;
            case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Stelem_I; break;
            default:
              //TODO: if the size of the value type is known and <= 8, use one of the above opcodes.
              break;
          }
          if (opcode == OperationCode.Stelem) {
            this.sourceEmitter.EmitString("memcpy((void*)element_address, &");
            this.EmitInstructionReference(indexAndValue[1]);
            this.sourceEmitter.EmitString(", sizeof(");
            this.EmitTypeReference(elementType, storageLocation: true);
            this.sourceEmitter.EmitString("))");
            return;
          }
        }
      }
      this.sourceEmitter.EmitString(typeCast);
      this.sourceEmitter.EmitString("element_address = ");
      this.EmitInstructionReference(indexAndValue[1]);
    }

    private void EmitStoreField(Instruction instruction) {
      Contract.Requires(instruction != null);

      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.Volatile_) {
        this.sourceEmitter.EmitString("MemoryBarrier();"); //force all preceding reads to complete
        this.sourceEmitter.EmitNewLine();
      }
      var valueToStore = instruction.Operand1;
      Contract.Assume(valueToStore != null);
      var fieldRef = instruction.Operation.Value as IFieldReference;
      Contract.Assume(fieldRef != null);
      var field = fieldRef.ResolvedField;
      var mangledFieldName = this.GetMangledFieldName(field);
      var fieldType = field.Type.ResolvedType;
      bool useMemcpy = (fieldType.IsValueType && !IsScalarInC(fieldType)) || fieldType == this.vaListType;

      if (field.IsStatic) {
        this.EmitCheckForStaticConstructor(field.ContainingTypeDefinition);
        this.sourceEmitter.EmitString("statics = GetThreadLocalValue(");
        var tlsIndex = "appdomain_static_block_tlsIndex";
        if (AttributeHelper.Contains(field.Attributes, this.threadStaticAttribute)) tlsIndex = "thread_static_block_tlsIndex";
        this.sourceEmitter.EmitString(tlsIndex);
        this.sourceEmitter.EmitString(");");
        this.sourceEmitter.EmitNewLine();
        if (useMemcpy) {
          this.sourceEmitter.EmitString("memcpy((void*)");
          this.sourceEmitter.EmitString("(statics+");
          this.sourceEmitter.EmitString(mangledFieldName);
          this.sourceEmitter.EmitString("), (void*)&");
          this.EmitInstructionReference(valueToStore);
          this.sourceEmitter.EmitString(", sizeof(");
          this.EmitTypeReference(fieldType, storageLocation: true);
          this.sourceEmitter.EmitString("))");
        } else {
          this.sourceEmitter.EmitString("*((");
          this.EmitTypeReference(field.Type, storageLocation: true);
          this.sourceEmitter.EmitString("*");
          this.sourceEmitter.EmitString(")(statics+");
          this.sourceEmitter.EmitString(mangledFieldName);
          this.sourceEmitter.EmitString(")) = ");
          this.EmitInstructionReference(valueToStore);
        }
      } else {
        if (!CheckForNoPrefixWithFlag(OperationCheckFlags.NoNullCheck))
          // If there was a prefix of no. for nullcheck we can omit the nullcheck
          this.EmitNullReferenceCheck(instruction.Operation.Offset, valueToStore);
        if (useMemcpy) this.sourceEmitter.EmitString("memcpy((void*)&");
        var operand1 = valueToStore;
        valueToStore = instruction.Operand2 as Instruction;
        Contract.Assume(valueToStore != null);
        var operand1Type = operand1.Type.ResolvedType;
        var fieldContainer = fieldRef.ContainingType.ResolvedType;
        if (IsScalarInC(fieldContainer)) {
          this.sourceEmitter.EmitString("*((");
          this.EmitTypeReference(fieldContainer);
          this.sourceEmitter.EmitString("*)");
          this.EmitInstructionReference(operand1);
          this.sourceEmitter.EmitString(") = ");
          this.EmitInstructionReference(valueToStore);
          return;
        }
        if (!operand1Type.IsValueType) {
          this.sourceEmitter.EmitString("((");
          this.EmitTypeReference(fieldContainer);
          if (fieldContainer.IsValueType) this.sourceEmitter.EmitString("*");
          this.sourceEmitter.EmitString(")(");
          this.EmitInstructionReference(operand1);
          this.EmitAdjustPointerToHeaderFromData();
          this.sourceEmitter.EmitString("))");
        } else {
          this.sourceEmitter.EmitString("(");
          this.EmitInstructionReference(operand1);
          this.sourceEmitter.EmitString(")");
        }
        if (operand1Type.IsValueType)
          this.sourceEmitter.EmitString(".");
        else
          this.sourceEmitter.EmitString("->");
        this.sourceEmitter.EmitString(this.GetMangledFieldName(fieldRef));
        if (useMemcpy) {
          this.sourceEmitter.EmitString(", (void*)&");
          this.EmitInstructionReference(valueToStore);
          this.sourceEmitter.EmitString(", sizeof(");
          this.EmitTypeReference(fieldType, storageLocation: true);
          this.sourceEmitter.EmitString("))");
        } else {
          this.sourceEmitter.EmitString(" = ");
          this.EmitInstructionReference(valueToStore);
        }
      }
    }

    private void EmitStoreIndirect(Instruction instruction) {
      Contract.Requires(instruction != null);

      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.Volatile_) {
        this.sourceEmitter.EmitString("MemoryBarrier();"); //force all preceding reads to complete
        this.sourceEmitter.EmitNewLine();
      }
      // TODO System.NullReferenceException is thrown if addr is not naturally aligned for the argument type implied by the instruction suffix.
      Contract.Assume(instruction.Operand1 != null);
      this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);
      this.sourceEmitter.EmitString("*((");
      string type = "invalid";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Stind_I:
          type = "intptr_t"; break;
        case OperationCode.Stind_I1:
          type = "int8_t"; break;
        case OperationCode.Stind_I2:
          type = "int16_t"; break;
        case OperationCode.Stind_I4:
          type = "int32_t"; break;
        case OperationCode.Stind_I8:
          type = "int64_t"; break;
        case OperationCode.Stind_R4:
          type = "float"; break;
        case OperationCode.Stind_R8:
          type = "double"; break;
        case OperationCode.Stind_Ref:
          type = "uintptr_t"; break;
      }
      this.sourceEmitter.EmitString(type);
      this.sourceEmitter.EmitString("*)");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(" = ");
      var operand2 = instruction.Operand2 as Instruction;
      Contract.Assume(operand2 != null);
      this.EmitInstructionReference(operand2);
    }

    private void EmitStoreLocal(Instruction instruction) {
      Contract.Requires(instruction != null);

      var local = instruction.Operation.Value as ILocalDefinition;
      Contract.Assume(local != null);
      var localType = local.Type.ResolvedType;
      Contract.Assume(instruction.Operand1 != null);

      if ((!local.IsReference && localType.IsValueType && !IsScalarInC(localType)) || localType == this.vaListType) {
        this.sourceEmitter.EmitString("memcpy((void*)");
        this.EmitLocalReference(local, loadAddress: true);
        this.sourceEmitter.EmitString(", (void*)&");
        this.EmitInstructionReference(instruction.Operand1);
        this.sourceEmitter.EmitString(", sizeof(");
        this.EmitTypeReference(local.Type, storageLocation: true);
        this.sourceEmitter.EmitString("))");
      } else {
        this.EmitLocalReference(local);
        this.sourceEmitter.EmitString(" = ");
        if (local.IsReference) {
          this.sourceEmitter.EmitString("(");
          this.EmitTypeReference(localType, storageLocation: true);
          this.sourceEmitter.EmitString("*)");
        }
        this.EmitInstructionReference(instruction.Operand1);
      }
    }

    private void EmitStoreObject(Instruction instruction) {
      Contract.Requires(instruction != null);

      if (this.previousInstruction != null && this.previousInstruction.Operation.OperationCode == OperationCode.Volatile_) {
        this.sourceEmitter.EmitString("MemoryBarrier();"); //force all preceding reads to complete
        this.sourceEmitter.EmitNewLine();
      }
      Contract.Assume(instruction.Operation.Value is ITypeReference);
      var type = (ITypeReference)instruction.Operation.Value;
      Contract.Assume(instruction.Operand1 != null);
      this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);
      Contract.Assume(instruction.Operand2 is Instruction);
      this.EmitNullReferenceCheck(instruction.Operation.Offset, (Instruction)instruction.Operand2);
      this.sourceEmitter.EmitString("memcpy((void*)");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(", (void*)");
      var operand2 = instruction.Operand2 as Instruction;
      if (operand2.Type.IsValueType) this.sourceEmitter.EmitString("&");
      this.EmitInstructionReference((Instruction)instruction.Operand2);
      this.sourceEmitter.EmitString(", sizeof(");
      this.EmitTypeReference(type, storageLocation: true);
      this.sourceEmitter.EmitString("))");
    }

    private void EmitSwitch(Instruction instruction) {
      Contract.Requires(instruction != null);

      uint[] branches = instruction.Operation.Value as uint[];
      Contract.Assume(branches != null);
      var n = branches.Length;
      this.sourceEmitter.EmitString("switch(");
      Contract.Assume(instruction.Operand1 != null);
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitControlBlockOpeningDelimiter("{");
      for (int i = 0; i < n; i++) {
        this.sourceEmitter.EmitCaseOpeningDelimiter("case ");
        this.sourceEmitter.EmitString(i.ToString());
        this.sourceEmitter.EmitString(":");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("goto l" + branches[i].ToString("x4") + ";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitCaseClosingDelimiter("");
        this.sourceEmitter.EmitNewLine();
      }
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitThrow(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operand1 != null);
      Contract.Assume(instruction.Operand1.result != null);
      Contract.Assume(instruction.Operand1.result.name != null);
      this.EmitThrow(instruction.Operation.Offset, instruction.Operand1.Type, instruction.Operand1.result.name, emitNullCheck: true);
    }

    private void EmitThrow(uint offset, ITypeReference/*?*/ type, string name, bool emitNullCheck = false) {
      Contract.Requires(name != null);
      this.sourceEmitter.EmitString("#ifdef ENABLE_DEBUG_BREAK");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("__debugbreak();");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#endif");
      this.sourceEmitter.EmitNewLine();
      if (emitNullCheck) {
        this.sourceEmitter.EmitString("if ("+name+" == 0) ");
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uintptr_t nullReferenceException;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("exception = GetNullReferenceException((uintptr_t)&nullReferenceException);");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("if (exception != 0) "); 
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
        this.EmitThrow(offset, this.outOfMemoryExceptionType, "exception");
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("exception = nullReferenceException;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitString(" else ");
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("exception = "+name+";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitNewLine();
      } else if (name != "exception" && name != "originalException") {
        this.sourceEmitter.EmitString("if (exception) ");
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
        this.EmitThrow(offset, this.outOfMemoryExceptionType, "exception");
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("exception = "+name+";");
        this.sourceEmitter.EmitNewLine();
      }


      bool exceptionHandled = false;
      if (IteratorHelper.EnumerableIsNotEmpty(this.currentBody.OperationExceptionInformation)) {
        this.sourceEmitter.EmitString("throwOffset = " + offset + ";");
        this.sourceEmitter.EmitNewLine();
        foreach (var expInfo in this.currentBody.OperationExceptionInformation) {
          Contract.Assume(expInfo != null);
          if (expInfo.HandlerKind == HandlerKind.Catch && offset >= expInfo.TryStartOffset && offset < expInfo.TryEndOffset) {

            if (expInfo.ExceptionType.ResolvedType == this.host.PlatformType.SystemObject.ResolvedType) {
              // If we have a catch block that can catch anything we can simply run it without having to run any filters
              this.sourceEmitter.EmitString("goto lrun_finallies_for_catch_" + expInfo.HandlerStartOffset + ";");
              this.sourceEmitter.EmitNewLine();
              exceptionHandled = true;
              break;
            } else if (type != null) {
              // If there is a handler that matches the thrown object no need running through the filters we can jump into running the finallies for it
              if (TypeHelper.Type1DerivesFromOrIsTheSameAsType2(type.ResolvedType, expInfo.ExceptionType) || TypeHelper.Type1ImplementsType2(type.ResolvedType, expInfo.ExceptionType)) {
                this.sourceEmitter.EmitString("goto lrun_finallies_for_catch_" + expInfo.HandlerStartOffset + ";");
                this.sourceEmitter.EmitNewLine();
                exceptionHandled = true;
                break;
              } else if (TypeHelper.Type1DerivesFromOrIsTheSameAsType2(expInfo.ExceptionType.ResolvedType, type) || TypeHelper.Type1ImplementsType2(expInfo.ExceptionType.ResolvedType, type)) {
                // If we have a class hierarchy  "A <- B <- C" with "throw B", then "catch C" and "catch B" are potential catch filters that we need to check
                // Also we need to check whethet C implements B
                this.EmitCatchFilter(offset, expInfo);
              }
            } else {
              this.EmitCatchFilter(offset, expInfo);
            }
          }
        }
      }
      if (!exceptionHandled) {
        // We are not sure whether the exception was handled, hence we should be prepared to execute the finally blocks needed and exit this method
        this.EmitLeave(offset, offset, isLeavingMethod: true);
        this.sourceEmitter.EmitString("return ");
        this.sourceEmitter.EmitString("exception");
      }     
    }

    private void EmitCatchFilter(uint offset, IOperationExceptionInformation expInfo) {
      Contract.Requires(expInfo != null);

      this.sourceEmitter.EmitString("expSwitchVal" + expInfo.TryStartOffset + " = " + this.exceptionSwitchTables.NumberOfEntries(expInfo.TryStartOffset) + ";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("goto lcatch_filter_" + expInfo.HandlerStartOffset + ";");
      this.sourceEmitter.EmitNewLine();
      string label = "l" + offset.ToString("x4") + "_" + expInfo.HandlerStartOffset.ToString("x4") + "_" + labelCounter++;
      this.exceptionSwitchTables.Add(expInfo.TryStartOffset, label);
      this.sourceEmitter.EmitLabel(label + ":");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("if (canHandleExp > 0) ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("canHandleExp = 0;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("goto lrun_finallies_for_catch_" + expInfo.HandlerStartOffset + ";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    static int labelCounter = 0;

    private void EmitUnaryBranchCondition(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("if (");
      this.EmitUnaryOperation(instruction);
      this.sourceEmitter.EmitString(") ");
    }

    private void EmitUnaryOperation(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      string unaryOp = "invalid";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
          unaryOp = "!";
          break;
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          unaryOp = "";
          break;
        case OperationCode.Neg:
          unaryOp = "-";
          break;
        case OperationCode.Not:
          unaryOp = "~";
          break;
      }
      this.sourceEmitter.EmitString(unaryOp);
      Contract.Assume(instruction.Operand1 != null);
      this.EmitInstructionReference(instruction.Operand1);
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitUnbox(Instruction instruction) {
      Contract.Requires(instruction != null);

      var targetType = instruction.Operation.Value as ITypeReference;
      Contract.Assume(targetType != null);
      Contract.Assume(instruction.Operand1 != null);

      bool needClosingBrace = false;
      // If there is a no. prefix we can skip the type check
      if (targetType.ResolvedType.IsValueType || !CheckForNoPrefixWithFlag(OperationCheckFlags.NoTypeCheck)) {
        this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1); //TODO: special handling for nullable<T>
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        needClosingBrace = true;
        this.sourceEmitter.EmitNewLine();

        //Get the type of the object to unbox
        this.sourceEmitter.EmitString("uintptr_t objectType = ((");
        this.EmitTypeReference(this.host.PlatformType.SystemObject);
        this.sourceEmitter.EmitString(")");
        this.sourceEmitter.EmitString("(");
        this.EmitInstructionReference(instruction.Operand1);
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(")");
        this.sourceEmitter.EmitString(")->");
        this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();

        //If the type is not the boxed version of targetType throw an invalid cast exception.
        this.sourceEmitter.EmitString("if (objectType != ");
        this.EmitTypeObjectReference(targetType);
        this.sourceEmitter.EmitString(") ");
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        //TODO: if the objectType is a boxed value type of a value type that is assignment compatible to targetType then allow the unbox.
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uintptr_t invalidCastException;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("exception = GetInvalidCastException((uintptr_t)&invalidCastException);");
        this.sourceEmitter.EmitNewLine();
        this.EmitThrow(instruction.Operation.Offset, this.invalidCastExceptionType, "invalidCastException");
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        if (needClosingBrace) {
          this.sourceEmitter.EmitBlockClosingDelimiter("}");
          this.sourceEmitter.EmitNewLine();
        }
      }

      if (instruction.Operation.OperationCode == OperationCode.Unbox_Any)
        this.sourceEmitter.EmitString("unboxed");
      else
        this.EmitInstructionResultName(instruction);
      this.sourceEmitter.EmitString(" = ");
      this.EmitInstructionReference(instruction.Operand1);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
    }

    private void EmitUnboxAny(Instruction instruction) {
      Contract.Requires(instruction != null);

      var targetType = instruction.Operation.Value as ITypeReference;
      Contract.Assume(targetType != null);
      if (targetType.ResolvedType.IsValueType) {
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uintptr_t unboxed;");
        this.sourceEmitter.EmitNewLine();
        this.EmitUnbox(instruction);
        this.sourceEmitter.EmitNewLine();
        this.EmitLoadObject(instruction);
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
      } else {
        this.EmitCastClass(instruction);
      }
    }

    private void EmitUnsignedBinaryOperation(Instruction instruction, bool leaveResultInTemp) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operand2 is Instruction);
      var operand2 = (Instruction)instruction.Operand2;
    
      if (OperationCode.Div_Un == instruction.Operation.OperationCode || OperationCode.Rem_Un == instruction.Operation.OperationCode) {
        if (IsIntegral(instruction.Type.ResolvedType)) {
		      this.EmitDivideByZeroCheck(instruction, operand2);
          this.sourceEmitter.EmitNewLine(); 
	      }
      }

      if (leaveResultInTemp) this.EmitStackSlotAssignmentPrefix(instruction);
      Contract.Assume(instruction.Operand1 != null);
      var operand1 = instruction.Operand1;

      var stackType = TypeHelper.StackType(operand1.Type);
      var unsignedType1 = TypeHelper.UnsignedEquivalent(stackType);
      if (unsignedType1 != stackType) {
        this.sourceEmitter.EmitString("((");
        this.EmitTypeReference(unsignedType1);
        this.sourceEmitter.EmitString(")");
        this.EmitInstructionReference(operand1);
        this.sourceEmitter.EmitString(")");
      } else {
        this.EmitInstructionReference(operand1);
      }

      string binaryOperation = " invalid ";
      switch (instruction.Operation.OperationCode) {
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          binaryOperation = " >= ";
          break;
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Cgt_Un:
          binaryOperation = " > ";
          break;
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          binaryOperation = " <= ";
          break;
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
        case OperationCode.Clt_Un:
          binaryOperation = " < ";
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
          binaryOperation = " != ";
          break;
        case OperationCode.Div_Un:
          binaryOperation = " / ";
          break;
        case OperationCode.Rem_Un:
          binaryOperation = " % ";
          break;
        case OperationCode.Shr_Un:
          binaryOperation = " >> ";
          break;
        default:
          Contract.Assume(false);
          break;
      }
      this.sourceEmitter.EmitString(binaryOperation);

      var stackType2 = TypeHelper.StackType(operand2.Type);
      var unsignedType2 = TypeHelper.UnsignedEquivalent(stackType2);
      if (unsignedType2 != stackType2) {
        this.sourceEmitter.EmitString("((");
        this.EmitTypeReference(unsignedType2);
        this.sourceEmitter.EmitString(")");
        this.EmitInstructionReference(operand2);
        this.sourceEmitter.EmitString(")");
      } else {
        this.EmitInstructionReference(operand2);
      }
      if (leaveResultInTemp) this.EmitStackSlotAssignmentPostfix(instruction);
    }

    private void EmitUnsignedOrUnorderComparison(Instruction instruction, bool leaveResultInTemp) {
      Contract.Requires(instruction != null);

      Contract.Assume(instruction.Operand1 != null);
      var operand1 = instruction.Operand1;
      Contract.Assume(instruction.Operand2 is Instruction);
      var operand2 = (Instruction)instruction.Operand2;
      var typeCode = operand1.Type.TypeCode;
      if (typeCode == PrimitiveTypeCode.Float32 || typeCode == PrimitiveTypeCode.Float64) {
        if (leaveResultInTemp) this.EmitStackSlotAssignmentPrefix(instruction);
        this.sourceEmitter.EmitString("!(");
        this.EmitInstructionReference(operand1);
        string comparison = " invalid ";
        switch (instruction.Operation.OperationCode) {
          case OperationCode.Bge_Un:
          case OperationCode.Bge_Un_S:
            comparison = " < ";
            break;
          case OperationCode.Bgt_Un:
          case OperationCode.Bgt_Un_S:
          case OperationCode.Cgt_Un:
            comparison = " <= ";
            break;
          case OperationCode.Ble_Un:
          case OperationCode.Ble_Un_S:
            comparison = " > ";
            break;
          case OperationCode.Blt_Un:
          case OperationCode.Blt_Un_S:
          case OperationCode.Clt_Un:
            comparison = " >= ";
            break;
          case OperationCode.Bne_Un:
          case OperationCode.Bne_Un_S:
            comparison = " == ";
            break;
          default:
            Contract.Assume(false);
            break;
        }
        this.sourceEmitter.EmitString(comparison);
        this.EmitInstructionReference(operand2);
        this.sourceEmitter.EmitString(")");
        if (leaveResultInTemp) this.EmitStackSlotAssignmentPostfix(instruction);
      } else {
        this.EmitUnsignedBinaryOperation(instruction, leaveResultInTemp);
      }
    }

    private void EmitUnsignedToRealConversion(Instruction instruction) {
      Contract.Requires(instruction != null);

      this.EmitStackSlotAssignmentPrefix(instruction);
      var operand1 = instruction.Operand1;
      Contract.Assume(operand1 != null);
      this.sourceEmitter.EmitString("((double)");
      var stackType = TypeHelper.StackType(operand1.Type);
      var unsignedType1 = TypeHelper.UnsignedEquivalent(stackType);
      if (unsignedType1 != stackType) {
        this.sourceEmitter.EmitString("((");
        this.EmitTypeReference(unsignedType1);
        this.sourceEmitter.EmitString(")");
        this.EmitInstructionReference(operand1);
        this.sourceEmitter.EmitString(")");
      } else {
        this.EmitInstructionReference(operand1);
      }
      if (unsignedType1 != operand1.Type)
        this.sourceEmitter.EmitString("))");
      else
        this.sourceEmitter.EmitString(")");
      this.EmitStackSlotAssignmentPostfix(instruction);
    }

    [ContractVerification(false)] //timeout
    private void EmitVirtualCall(Instruction instruction) {
      Contract.Requires(instruction != null);

      var methodToCall = instruction.Operation.Value as IMethodReference;
      Contract.Assume(methodToCall != null);
      // If there was a prefix of no. for nullcheck we can omit the nullcheck
      if (!CheckForNoPrefixWithFlag(OperationCheckFlags.NoNullCheck))
        this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);
      if (methodToCall.ResolvedMethod.IsVirtual) {
        this.EmitVirtualCall(instruction, methodToCall);
      } else {
        this.EmitCall(instruction, isVirtual: false, targetMethod: null);
      }
    }

    [ContractVerification(false)] //timeout
    private void EmitVirtualCall(Instruction instruction, IMethodReference methodToCall, bool makeCall = true) {
      Contract.Requires(instruction != null);
      Contract.Requires(methodToCall != null);

      bool dereferencePointer = false;
      bool needsBoxing = false;
      bool isSealed = false;
      ITypeDefinition targetType;

      Contract.Assume(instruction.Operand1 != null);
      var managedPointerType = instruction.Operand1.Type as IManagedPointerTypeReference;
      if (managedPointerType != null) {
        isSealed = managedPointerType.TargetType.ResolvedType.IsSealed;
        targetType = managedPointerType.TargetType.ResolvedType;
        if (managedPointerType.TargetType.ResolvedType.IsReferenceType ||
            (managedPointerType.TargetType as IGenericTypeParameter) != null)
          dereferencePointer = true;
        if (managedPointerType.TargetType.ResolvedType.IsValueType) {
          var method = TypeHelper.GetMethod(managedPointerType.TargetType.ResolvedType, methodToCall);
          if (method == Dummy.MethodDefinition) needsBoxing = true;
        }
      } else {
        isSealed = instruction.Operand1.Type.ResolvedType.IsSealed;
        targetType = instruction.Operand1.Type.ResolvedType;
      }

      // If the targetType is sealed, then we can devirtualize this method. 
      if (makeCall && isSealed) {
        var targetMethod = TypeHelper.GetMethod(targetType, methodToCall);
        if (targetMethod != Dummy.MethodDefinition) {
          this.EmitCall(instruction, isVirtual: false, targetMethod: targetMethod);
          return;
        }
      }

      if (methodToCall.ContainingType.ResolvedType.IsInterface) {
        this.EmitInitializeVirtualPtr(instruction, dereferencePointer, isInterface: true);

        this.sourceEmitter.EmitString("virtualPtr = (void **)(virtualPtr + (");
        this.sourceEmitter.EmitString(this.GetMangledMethodName(methodToCall));
        this.sourceEmitter.EmitString("_id % IMTSIZE));");
        this.sourceEmitter.EmitNewLine();

        this.sourceEmitter.EmitString("virtualPtr = (void **)*virtualPtr;");
        this.sourceEmitter.EmitNewLine();

        this.sourceEmitter.EmitString("if (virtualPtr == (void **)1 ) ");
        this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("GetFunctionPointerForInterfaceID(");
        this.sourceEmitter.EmitString("(uintptr_t)(*(void**)(((");
        this.EmitTypeReference(this.host.PlatformType.SystemObject);
        this.sourceEmitter.EmitString(")");
        this.sourceEmitter.EmitString("(");
        if (dereferencePointer) {
          this.sourceEmitter.EmitString("(*(uintptr_t*)");
          this.EmitInstructionReference(instruction.Operand1);
          this.sourceEmitter.EmitString(")");
        } else this.EmitInstructionReference(instruction.Operand1);
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(")");
        this.sourceEmitter.EmitString(")->");
        this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(" + sizeof(");
        this.EmitNonPrimitiveTypeReference(this.runtimeType);
        this.sourceEmitter.EmitString("))), ");
        this.sourceEmitter.EmitString(this.GetMangledMethodName(methodToCall));
        this.sourceEmitter.EmitString("_id, (uintptr_t)&virtualPtr);");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitNewLine();
        if (makeCall)
          this.EmitCall(instruction, isVirtual: true, targetMethod: null, dereferenceVirtualPtr: false,
                      dereferencePointer: dereferencePointer, needsBoxing: needsBoxing);
        else {
          this.EmitStackSlotAssignmentPrefix(instruction);
          this.sourceEmitter.EmitString("(uintptr_t) virtualPtr");
        }
      } else {
        this.EmitInitializeVirtualPtr(instruction, dereferencePointer, isInterface: false);
        Hashtable hashtable = this.vmtHashTable[methodToCall.ContainingType.InternedKey];
        if (hashtable == null) hashtable = new Hashtable(); //TODO: getting here is a bug
        uint index = hashtable[methodToCall.InternedKey];
        this.sourceEmitter.EmitString("virtualPtr = (void **)(virtualPtr + ");
        this.sourceEmitter.EmitString(index + ");");
        this.sourceEmitter.EmitNewLine();
        if (makeCall)
          this.EmitCall(instruction, isVirtual: true, targetMethod: null, dereferenceVirtualPtr: true,
                      dereferencePointer: dereferencePointer, needsBoxing: needsBoxing);
        else {
          this.EmitStackSlotAssignmentPrefix(instruction);
          this.sourceEmitter.EmitString("(uintptr_t) (*virtualPtr)");
        }
      }
    }

    [ContractVerification(false)]
    private void EmitVirtualFunctionPointerExpression(Instruction instruction) {
      Contract.Requires(instruction != null);

      var methodToCall = instruction.Operation.Value as IMethodReference;
      Contract.Assume(methodToCall != null);
      Contract.Assume(instruction.Operand1 != null);

      // If there was a prefix of no. for nullcheck we can omit the nullcheck
      if (!CheckForNoPrefixWithFlag(OperationCheckFlags.NoNullCheck))
        this.EmitNullReferenceCheck(instruction.Operation.Offset, instruction.Operand1);

      this.EmitVirtualCall(instruction, methodToCall, makeCall:false);
    }

    private void EmitInitializeVirtualPtr(Instruction instruction, bool dereferencePointer, bool isInterface = false) {
      Contract.Requires(instruction != null);

      this.sourceEmitter.EmitString("virtualPtr = (void **)(((");
      this.EmitTypeReference(this.host.PlatformType.SystemObject);
      this.sourceEmitter.EmitString(")");
      Contract.Assume(instruction.Operand1 != null);
      this.sourceEmitter.EmitString("(");
      if (dereferencePointer) {
        this.sourceEmitter.EmitString("(*(uintptr_t*)");
        this.EmitInstructionReference(instruction.Operand1);
        this.sourceEmitter.EmitString(")");
      } else {
        this.EmitInstructionReference(instruction.Operand1);
      }
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.typeField));
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(" + sizeof(");
      this.EmitNonPrimitiveTypeReference(this.runtimeType);
      this.sourceEmitter.EmitString(") + (");
      if (isInterface)
        this.sourceEmitter.EmitString("1");
      else
        this.sourceEmitter.EmitString("(1 + IMTSIZE)");
      this.sourceEmitter.EmitString("* sizeof(uintptr_t)))");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
    }



    private bool IsScalarInC(ITypeDefinition type) {
      Contract.Requires(type != null);

      switch (type.TypeCode) {
        case PrimitiveTypeCode.Boolean:
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.Int8:
        case PrimitiveTypeCode.IntPtr:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UInt8:
        case PrimitiveTypeCode.UIntPtr:
        case PrimitiveTypeCode.Float32:
        case PrimitiveTypeCode.Float64:
          return true;
        default:
          if (type.IsEnum) return true;
          return type == this.vaListType;
      }
    }

    private static bool IsIntegral(ITypeDefinition type) {
      Contract.Requires(type != null);

      switch (type.TypeCode) {
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.IntPtr:
        case PrimitiveTypeCode.UInt32:
        case PrimitiveTypeCode.UInt64:
        case PrimitiveTypeCode.UIntPtr:
          return true;
        default:
          return false;
      }
    }

  }

}
