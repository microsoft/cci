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
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.Analysis;
using System.Collections.Generic;
using Microsoft.Cci.UtilityDataStructures;

namespace ILtoC {
  partial class Translator {

    [ContractVerification(false)] //timeout
    private void EmitMethodSignatures(Hashtable<ITypeReference>/*?*/ structuralTypes, Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances) {
      var allTypes = this.module.GetAllTypes();

      //Emit a signature for the module's type loader. This will be called during application startup before invoking user code.
      this.sourceEmitter.EmitString("void ");
      this.sourceEmitter.EmitString(this.TypeLoaderName);
      this.sourceEmitter.EmitString("()");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      //Emit a typedef for a pointer to a default constructor. We always need such a type.
      this.sourceEmitter.EmitString("#ifndef DEFAULT_CONSTRUCTOR_TYPEDEF");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#define DEFAULT_CONSTRUCTOR_TYPEDEF");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("typedef uintptr_t (*ctor_ptr)(uintptr_t  _this);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#endif");
      this.sourceEmitter.EmitNewLine();

      //Emit signatures for all of the methods in this module, so that they can be called directly from other modules.
      //All methods are given signatures, in case there has been inlining that results in other modules directly 
      //invoking private methods from this module.
      foreach (var type in allTypes) {
        Contract.Assume(type != null);
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue;
        this.EmitMethodSignaturesForType(type, isTypeDef: false);
      }

      //Emit typedefs for pointers to all of the methods that might be called indirectly.
      foreach (var type in allTypes) {
        Contract.Assume(type != null);
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue;
        this.EmitMethodSignaturesForType(type, isTypeDef: true);
      }

      //Emit signatures for the helper methods that check if static constructors need to run
      foreach (var type in allTypes) {
        Contract.Assume(type != null);
        if (!this.HasStaticConstructor(type)) continue;
        this.sourceEmitter.EmitString("void CheckIfStaticConstructorNeedsToRunFor");
        this.sourceEmitter.EmitString(this.GetMangledTypeName(type));
        this.sourceEmitter.EmitString("();");
      }

      if (structuralTypes != null) {
        foreach (var type in structuralTypes.Values) {
          Contract.Assume(type != null);
          this.EmitMethodSignaturesForType(type.ResolvedType, isTypeDef: false);
        }
        foreach (var type in structuralTypes.Values) {
          Contract.Assume(type != null);
          this.EmitMethodSignaturesForType(type.ResolvedType, isTypeDef: true);
        }
        foreach (var type in structuralTypes.Values) {
          Contract.Assume(type != null);
          if (!this.HasStaticConstructor(type)) continue;
          this.sourceEmitter.EmitString("void CheckIfStaticConstructorNeedsToRunFor");
          this.sourceEmitter.EmitString(this.GetMangledTypeName(type));
          this.sourceEmitter.EmitString("();");
        }
      }
      if (closedGenericMethodInstances != null) {
        foreach (var genMethInst in closedGenericMethodInstances.Values) {
          Contract.Assume(genMethInst != null);
          Contract.Assume(!genMethInst.ResolvedMethod.IsGeneric);
          this.EmitMethodSignature(genMethInst.ResolvedMethod, isTypeDef: false);
          this.sourceEmitter.EmitString(";");
          this.sourceEmitter.EmitNewLine();
        }
      }
    }

    private void EmitMethodSignaturesForType(ITypeDefinition type, bool isTypeDef) {
      Contract.Requires(type != null);
      bool firstTime = true;
      foreach (var method in type.Methods) {
        Contract.Assume(method != null);
        if (AttributeHelper.Contains(method.Attributes, this.cRuntimeAttribute)) return;
        if (method.IsGeneric) continue;
        if (isTypeDef && !method.IsVirtual) continue;
        if (firstTime) {
          this.sourceEmitter.EmitNewLine();
          firstTime = false;
        }
        bool isDelegateConstructor = false;
        if (type.IsDelegate && method.Name == this.host.NameTable.Ctor)
          isDelegateConstructor = true;
        this.EmitMethodSignature(method, isTypeDef: isTypeDef, isDelegateConstructor:isDelegateConstructor);
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
      }
    }

    private void EmitMethods(Hashtable<ITypeReference>/*?*/ structuralTypes, Hashtable<IGenericMethodInstanceReference>/*?*/ closedGenericMethodInstances) {
      foreach (var type in this.module.GetAllTypes()) {
        Contract.Assume(type != null);
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue;
        if (!TypeHelper.GetTypeName(type).StartsWith("System.String")) continue;
        this.EmitMethodsForType(type);
      }
      foreach (var type in this.module.GetAllTypes()) {
        Contract.Assume(type != null);
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue;
        if (TypeHelper.GetTypeName(type).StartsWith("System.String")) continue;
        this.EmitMethodsForType(type);
      }
      if (structuralTypes != null) {
        foreach (var type in structuralTypes.Values) {
          Contract.Assume(type != null);
          this.EmitMethodsForType(type.ResolvedType);
        }
      }
      if (closedGenericMethodInstances != null) {
        foreach (var genMethInst in closedGenericMethodInstances.Values) {
          Contract.Assume(genMethInst != null);
          this.EmitMethod(genMethInst.ResolvedMethod);
        }
      }
    }

    private void EmitMethodsForType(ITypeDefinition type) {
      Contract.Requires(type != null);

      if (type.IsDelegate) {
        this.EmitMethodsForDelegate(type);
        return;
      }

      foreach (var method in type.Methods) {
        Contract.Assume(method != null);
        if (method.IsGeneric) continue;
        this.EmitMethod(method);
        if (method.IsStaticConstructor)
          this.EmitHelperForRunningConstructor(method);
      }
    }

    // We will currently Emit method bodies for the constructor and the Invoke method. 
    private void EmitMethodsForDelegate(ITypeDefinition type) {
      Contract.Requires(type != null);
      Contract.Requires(type.IsDelegate);

      foreach (var method in type.Methods) {
        Contract.Assume(method != null);
        Contract.Assume(!method.IsGeneric);

        if (method.IsGeneric) continue;
        this.EmitMethodSignature(method, isTypeDef: false, isDelegateConstructor: method.IsConstructor);
        this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();

        // TODO Need to emit bodies for BeginInvoke and EndInvoke
        if (method.IsConstructor)
          this.EmitDelegateConstructor(method);
        else if (method.Name.Value == "Invoke")
          this.EmitDelegateInvoke(method); 

        this.sourceEmitter.EmitString("return 0;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitNewLine();
      }
    }

    private void EmitDelegateConstructor(IMethodDefinition method) {
      Contract.Requires(method != null);
      Contract.Requires(method.IsConstructor);

      var parameters = IteratorHelper.GetAsArray(method.Parameters);
      Contract.Assume(parameters.Length == 2);
      Contract.Assume(parameters[0] != null);
      Contract.Assume(parameters[1] != null);

      this.sourceEmitter.EmitString("((");
      this.EmitTypeReference(method.ContainingType);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.sourceEmitter.EmitString("_this");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.delegateTargetField));
      this.sourceEmitter.EmitString(" = ");
      this.sourceEmitter.EmitString(this.GetSanitizedName(parameters[0].Name));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("((");
      this.EmitTypeReference(method.ContainingType);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.sourceEmitter.EmitString("_this");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.delegateMethodPtrField));
      this.sourceEmitter.EmitString(" = ");
      this.sourceEmitter.EmitString(this.GetSanitizedName(parameters[1].Name));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      
      this.sourceEmitter.EmitString("((");
      this.EmitTypeReference(method.ContainingType);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.sourceEmitter.EmitString("_this");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.delegateIsStaticField));
      this.sourceEmitter.EmitString(" = ");
      this.sourceEmitter.EmitString("isStatic");
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitDelegateInvoke(IMethodDefinition method) {
      Contract.Requires(method != null);

      this.sourceEmitter.EmitString("uintptr_t exception;");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("uintptr_t target = ");
      this.sourceEmitter.EmitString("((");
      this.EmitTypeReference(method.ContainingType);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.sourceEmitter.EmitString("_this");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.delegateTargetField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("intptr_t methodPtr = ");
      this.sourceEmitter.EmitString("((");
      this.EmitTypeReference(method.ContainingType);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.sourceEmitter.EmitString("_this");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.delegateMethodPtrField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("uint32_t isStatic = ");
      this.sourceEmitter.EmitString("((");
      this.EmitTypeReference(method.ContainingType);
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("(");
      this.sourceEmitter.EmitString("_this");
      this.EmitAdjustPointerToHeaderFromData();
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitString("->");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(this.delegateIsStaticField));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("if (isStatic)");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("// Invoking a static method");
      this.sourceEmitter.EmitNewLine();
      this.EmitDelegateInvokeFunctionPointer(method, isStatic:true);
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitString("else ");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("// Invoking an instance method");
      this.sourceEmitter.EmitNewLine();
      this.EmitDelegateInvokeFunctionPointer(method, isStatic: false);
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("if (exception)");
      this.sourceEmitter.EmitBlockOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("return exception;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitDelegateInvokeFunctionPointer(IMethodDefinition method, bool isStatic) {
      Contract.Requires(method != null);
      this.sourceEmitter.EmitString("exception = ((");

      bool first = true;
      // Emit the signature of the function to be invoked
      this.sourceEmitter.EmitString("uintptr_t (*)(");
      if (!isStatic) {
        this.sourceEmitter.EmitString("uintptr_t");
        first = false;
      }
      this.EmitParametersOfFunctionPointer(first, method.Parameters, method.Type.TypeCode);
      
      // Done emiting signature
      this.sourceEmitter.EmitString(")(");
      // Emit the function pointer
      this.sourceEmitter.EmitString("methodPtr");
      this.sourceEmitter.EmitString("))(");
      // Start emiting the arguments to the function
      first = true;
      if (!isStatic) {
        this.sourceEmitter.EmitString("target");
        first = false;
      }

      foreach (var parameter in method.Parameters) {
        Contract.Assume(parameter != null);
        if (first) first = false; else this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString(this.GetSanitizedName(parameter.Name));
      }
      if (method.Type.TypeCode != PrimitiveTypeCode.Void) {
        if (first) first = false; else this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString("_result");
      }
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitHelperForRunningConstructor(IMethodDefinition constructor) {
      Contract.Requires(constructor != null);
      Contract.Requires(constructor.IsStaticConstructor);

      var mangledTypeName = this.GetMangledTypeName(constructor.ContainingTypeDefinition);
      this.sourceEmitter.EmitString("extern uint32_t ");
      this.sourceEmitter.EmitString(mangledTypeName);
      this.sourceEmitter.EmitString("_isInitialized;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("void CheckIfStaticConstructorNeedsToRunFor");
      this.sourceEmitter.EmitString(mangledTypeName);
      this.sourceEmitter.EmitString("(");
      this.sourceEmitter.EmitString(")");
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uint8_t* appDomainStatics = GetThreadLocalValue(appdomain_static_block_tlsIndex);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("if (*((uint8_t*)(appDomainStatics+");
      this.sourceEmitter.EmitString(mangledTypeName);
      this.sourceEmitter.EmitString("_isInitialized))) return;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("*((uint8_t*)(appDomainStatics+");
      this.sourceEmitter.EmitString(mangledTypeName);
      this.sourceEmitter.EmitString("_isInitialized)) = 1;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString(this.GetMangledMethodName(constructor));
      this.sourceEmitter.EmitString("();");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitMethod(IMethodDefinition method) {
      Contract.Requires(method != null);

      if (this.sourceEmitter.LeaveBlankLinesBetweenNamespaceMembers) this.sourceEmitter.EmitNewLine();
      if (method.IsExternal && method.IsStatic) return;
      if (AttributeHelper.Contains(method.Attributes, this.cRuntimeAttribute)) return;
      if (method.IsGeneric) return;
      this.EmitMethodSignature(method, isTypeDef: false);
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      if (method.IsAbstract || method.IsExternal) {
        this.sourceEmitter.EmitString("return 0;");
        this.sourceEmitter.EmitNewLine();
      } else
        this.EmitMethodBody(method);
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitMethodSignature(IMethodDefinition method, bool isTypeDef, bool isDelegateConstructor = false) {
      Contract.Requires(method != null);
      Contract.Requires(!method.IsGeneric);

      if (isTypeDef) 
        this.sourceEmitter.EmitString("typedef ");
      this.sourceEmitter.EmitString("uintptr_t ");
      if (isTypeDef)
        this.sourceEmitter.EmitString("(*");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(method));
      if (isTypeDef)
        this.sourceEmitter.EmitString("_ptr)");
      this.sourceEmitter.EmitString("(");
      bool first = true;
      if (!method.IsStatic) {
        this.sourceEmitter.EmitString("uintptr_t ");
        this.sourceEmitter.EmitString(" _this");
        first = false;
      }
      foreach (var par in method.Parameters) {
        Contract.Assume(par != null);
        var parType = par.Type.ResolvedType;
        if (first) first = false; else this.sourceEmitter.EmitString(", ");
        if (par.IsByReference || (parType.IsValueType && !IsScalarInC(parType)))
          this.sourceEmitter.EmitString("uintptr_t ");
        else
          this.EmitTypeReference(parType, storageLocation: true);
        this.sourceEmitter.EmitString(" ");
        this.sourceEmitter.EmitString(this.GetSanitizedName(par.Name));
      }
      if (isDelegateConstructor) {
        // When creating a delegate instance we also pass in an extra augument which indicated whether the function we are passing in isStatic
        // The call to the constructor is emited by the compiler and hence this is not passed in by IL
        this.sourceEmitter.EmitString(", uint32_t isStatic");
      }
      if (method.Type.TypeCode != PrimitiveTypeCode.Void) {
        if (first) first = false; else this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString("uintptr_t _result");
      }
      if (method.AcceptsExtraArguments) {
        if (first) first = false; else this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString("uintptr_t _extraArgumentTypes, ...");
      }
      this.sourceEmitter.EmitString(")");
    }

    private string GetMangledMethodName(IMethodReference method) {
      Contract.Requires(method != null);
      var result = this.mangledMethodName[method.InternedKey];
      if (result == null) {
        if (AttributeHelper.Contains(method.Attributes, this.doNotMangleAttribute))
          result = method.Name.Value;
        else
          result = new Mangler().Mangle(method);
        this.mangledMethodName[method.InternedKey] = result;
      }
      return result;
    }

    [ContractVerification(false)]
    private void EmitMethodBody(IMethodDefinition method) {
      Contract.Requires(method != null);

      if (method.IsAbstract) return;
      if (method.IsExternal) return; //TODO: may need to emit a prototype.
      this.tempForStackSlot.Clear();
      this.temps.Clear();

      var methodBody = this.currentBody = method.Body;
      var cdfg = ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(this.host, methodBody, this.pdbReader);
      this.CreateTempsForOperandStack(cdfg);
      this.EmitLocalDefinitions(cdfg);
      this.EmitExceptionSwitchTableDefinitions(cdfg.MethodBody.OperationExceptionInformation);
      if (!method.ContainingTypeDefinition.IsBeforeFieldInit)
        this.EmitCheckForStaticConstructor(method.ContainingTypeDefinition);
      this.EmitInstructions(cdfg);
      this.catchHandlerOffsets.Clear();
      this.EmitCatchFilters(cdfg.MethodBody.OperationExceptionInformation);
      this.EmitExceptionSwitchTable(cdfg.MethodBody.OperationExceptionInformation);
      
    }

    [ContractVerification(false)]
    private void CreateTempsForOperandStack(ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction> cdfg) {
      Contract.Requires(cdfg != null);

      foreach (var block in cdfg.AllBlocks) {
        Contract.Assume(block != null);
        this.InitializeOperandStack(block);

        foreach (var instruction in block.Instructions) {
          Contract.Assume(instruction != null);
          switch (instruction.Operation.OperationCode) {
            case OperationCode.Add:
            case OperationCode.And:
            case OperationCode.Ceq:
            case OperationCode.Cgt:
            case OperationCode.Cgt_Un:
            case OperationCode.Clt:
            case OperationCode.Clt_Un:
            case OperationCode.Mul:
            case OperationCode.Or:
            case OperationCode.Shl:
            case OperationCode.Shr:
            case OperationCode.Shr_Un:
            case OperationCode.Sub:
            case OperationCode.Xor:
              this.AdjustStackForBinaryOperation(instruction);
              break;

            case OperationCode.Add_Ovf:
            case OperationCode.Add_Ovf_Un:
            case OperationCode.Mul_Ovf:
            case OperationCode.Mul_Ovf_Un:
            case OperationCode.Sub_Ovf:
            case OperationCode.Sub_Ovf_Un:
              this.AdjustStackForBinaryOperation(instruction);
              this.generateOverflowCheckTemp = true;
              this.mayThrowException = true;
              break;

            case OperationCode.Arglist:
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
            case OperationCode.Ldsfld:
            case OperationCode.Ldarga:
            case OperationCode.Ldarga_S:
            case OperationCode.Ldsflda:
            case OperationCode.Ldloca:
            case OperationCode.Ldloca_S:
            case OperationCode.Ldftn:
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
            case OperationCode.Ldtoken:
            case OperationCode.Sizeof:
              this.PushTempForSlotAndAssociateWithInstruction(instruction);
              break;

            case OperationCode.Array_Addr:
            case OperationCode.Array_Get:
              this.AdjustStackForArrayAddr(instruction);
              break;

            case OperationCode.Array_Create:
            case OperationCode.Array_Create_WithLowerBound:
            case OperationCode.Newarr:
              this.AdjustStackForArrayCreate(instruction);
              this.mayThrowException = true;
              break;

            case OperationCode.Array_Set:
              this.AdjustStackForArraySet(instruction);
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
              this.AdjustStackForBinaryVoidOperation();
              break;

            case OperationCode.Cpobj:
            case OperationCode.Stind_I:
            case OperationCode.Stind_I1:
            case OperationCode.Stind_I2:
            case OperationCode.Stind_I4:
            case OperationCode.Stind_I8:
            case OperationCode.Stind_R4:
            case OperationCode.Stind_R8:
            case OperationCode.Stind_Ref:
            case OperationCode.Stobj:
              this.AdjustStackForBinaryVoidOperation();
              this.mayThrowException = true;
              break;

            case OperationCode.Box:
            case OperationCode.Castclass:
            case OperationCode.Ckfinite:
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
            case OperationCode.Ldflda:
            case OperationCode.Ldfld:
            case OperationCode.Ldlen:
            case OperationCode.Localloc:
            case OperationCode.Refanytype:
            case OperationCode.Refanyval:
            case OperationCode.Unbox:
            case OperationCode.Unbox_Any:
              this.AdjustStackForUnaryOperation(instruction);
              this.mayThrowException = true;
              break;

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
              this.AdjustStackForUnaryOperation(instruction);
              this.generateOverflowCheckTemp = true;
              this.mayThrowException = true;
              break;

            case OperationCode.Brfalse:
            case OperationCode.Brfalse_S:
            case OperationCode.Brtrue:
            case OperationCode.Brtrue_S:
            case OperationCode.Endfilter:
            case OperationCode.Initobj:
            case OperationCode.Pop:
            case OperationCode.Starg:
            case OperationCode.Starg_S:
            case OperationCode.Stloc:
            case OperationCode.Stloc_0:
            case OperationCode.Stloc_1:
            case OperationCode.Stloc_2:
            case OperationCode.Stloc_3:
            case OperationCode.Stloc_S:
            case OperationCode.Stsfld:
            case OperationCode.Switch:
              this.AdjustStackForUnaryVoidOperation();
              break;

            case OperationCode.Call:
              this.AdjustStackForCall(instruction);
              this.mayThrowException = true;
              break;

            case OperationCode.Callvirt:
              this.AdjustStackForCall(instruction);
              this.mayThrowException = true;
              this.hasCallVirt = true;
              break;

            case OperationCode.Calli:
              this.AdjustStackForCalli(instruction);
              this.mayThrowException = true;
              break;

            case OperationCode.Conv_I:
            case OperationCode.Conv_I1:
            case OperationCode.Conv_I2:
            case OperationCode.Conv_I4:
            case OperationCode.Conv_I8:
            case OperationCode.Conv_R_Un:
            case OperationCode.Conv_R4:
            case OperationCode.Conv_R8:
            case OperationCode.Conv_U:
            case OperationCode.Conv_U1:
            case OperationCode.Conv_U2:
            case OperationCode.Conv_U4:
            case OperationCode.Conv_U8:
            case OperationCode.Isinst:
            case OperationCode.Mkrefany:
            case OperationCode.Neg:
            case OperationCode.Not:
              this.AdjustStackForUnaryOperation(instruction);
              break;

            case OperationCode.Cpblk:
            case OperationCode.Initblk:
              this.AdjustStackForTernaryVoidOperation();
              this.mayThrowException = true;
              break;

            case OperationCode.Dup:
              this.AdjustStackForDup(instruction);
              break;

            case OperationCode.Div:
            case OperationCode.Div_Un:
            case OperationCode.Rem:
            case OperationCode.Rem_Un:
            case OperationCode.Ldelema:
              this.AdjustStackForBinaryOperation(instruction);
              this.mayThrowException = true;
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
              this.AdjustStackForBinaryOperation(instruction);
              this.mayThrowException = true;
              this.needsTempForArrayElementAddress = true;
              break;

            case OperationCode.Ldvirtftn:
              this.AdjustStackForUnaryOperation(instruction);
              this.mayThrowException = true;
              this.hasCallVirt = true;
              break;

            case OperationCode.Leave:
            case OperationCode.Leave_S:
              this.AdjustStackForLeave();
              break;

            case OperationCode.Newobj:
              this.AdjustStackForNewObject(instruction);
              this.mayThrowException = true;
              break;

            case OperationCode.Ret:
              this.AdjustStackForRet(cdfg);
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
              this.AdjustStackForTernaryVoidOperation();
              this.mayThrowException = true;
              this.needsTempForArrayElementAddress = true;
              break;

            case OperationCode.Stfld:
              this.AdjustStackForBinaryVoidOperation();
              this.mayThrowException = true;
              break;

            case OperationCode.Throw:
              this.AdjustStackForUnaryVoidOperation();
              this.mayThrowException = true;
              break;
          }
        }
      }

    }

    private void EmitLocalDefinitions(ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction> cdfg) {
      Contract.Requires(cdfg != null);

      bool thereWereLocalsVariables = false;
      foreach (var methodLocal in cdfg.MethodBody.LocalVariables) {
        Contract.Assume(methodLocal != null);
        this.EmitTypeReference(methodLocal.Type, storageLocation: true);
        if (methodLocal.IsReference) this.sourceEmitter.EmitString("*");
        this.sourceEmitter.EmitString(" ");
        this.EmitLocalReference(methodLocal);
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        thereWereLocalsVariables = true;
      }
      if (thereWereLocalsVariables)
        this.sourceEmitter.EmitNewLine();
      if (this.mayThrowException) {
        this.sourceEmitter.EmitString("uintptr_t exception;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uintptr_t originalException;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uint32_t throwOffset;");
        this.sourceEmitter.EmitNewLine();
        this.mayThrowException = false;
      }
      if (this.generateOverflowCheckTemp) {
        this.sourceEmitter.EmitString("int32_t overflowFlag;");
        this.sourceEmitter.EmitNewLine();
        this.generateOverflowCheckTemp = false;
      }
      if (this.needsTempForArrayElementAddress) {
        this.sourceEmitter.EmitString("uintptr_t element_address;");
        this.sourceEmitter.EmitNewLine();
        this.generateOverflowCheckTemp = false;
      }
      if (this.hasCallVirt) {
        this.sourceEmitter.EmitString("void ** virtualPtr;");
        this.sourceEmitter.EmitNewLine();
        this.hasCallVirt = false;
      }
      
      foreach (var temp in this.temps) this.EmitTemp(temp);
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitTemp(Temp temp) {
      Contract.Assume(temp != null);
      Contract.Assume(temp.type != null);
      this.EmitTypeReference(temp.type, storageLocation: true);
      this.sourceEmitter.EmitString(" ");
      Contract.Assume(temp.name != null);
      this.sourceEmitter.EmitString(temp.name);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitString(" // ");
      this.sourceEmitter.EmitString(TypeHelper.GetTypeName(temp.type));
      this.sourceEmitter.EmitNewLine();
    }

    [ContractVerification(false)]
    private void EmitExceptionSwitchTableDefinitions(IEnumerable<IOperationExceptionInformation> operationExceptionInformation) {
      Contract.Requires(operationExceptionInformation != null);

      bool haveCatchClauses = false;
      SetOfUints tryBlockOffsets = new SetOfUints();
      List<IOperationExceptionInformation> finallyHandlers = new List<IOperationExceptionInformation>();
      foreach (var expInfo in operationExceptionInformation) {
        Contract.Assume(expInfo != null);
        if (expInfo.HandlerKind == HandlerKind.Finally) {
          finallyHandlers.Add(expInfo);
        }else if (expInfo.HandlerKind == HandlerKind.Catch) {
          haveCatchClauses = true;
          this.catchHandlerOffsets.Add(expInfo.HandlerStartOffset+1);
          // Keep track of the finally handlers a catch handler MAY have to run in the event of a throw
          foreach (IOperationExceptionInformation finallyHandler in finallyHandlers) {
            Contract.Assume(finallyHandler != null);
            if (finallyHandler.HandlerEndOffset < expInfo.TryEndOffset && finallyHandler.HandlerStartOffset > expInfo.TryStartOffset) {
              this.finallyHandlersForCatch.Add((uint)expInfo.HandlerStartOffset, finallyHandler);
            }
          }
        }
        if (!tryBlockOffsets.Contains(expInfo.TryStartOffset+1)) {
          this.sourceEmitter.EmitString("int32_t expSwitchVal" + expInfo.TryStartOffset + ";");
          this.sourceEmitter.EmitNewLine();
          tryBlockOffsets.Add(expInfo.TryStartOffset+1);          
        }
      }
      if (haveCatchClauses) {
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("uint32_t canHandleExp = 0;");
      }
      this.sourceEmitter.EmitNewLine();
    }

    [ContractVerification(false)]
    private void EmitCatchFilters(IEnumerable<IOperationExceptionInformation> operationExceptionInformation) {
      Contract.Requires(operationExceptionInformation != null);
      int labelCount = 0;
      foreach (var expInfo in operationExceptionInformation) {
        Contract.Assume(expInfo != null);
        if (expInfo.HandlerKind == HandlerKind.Catch) {
          this.sourceEmitter.EmitLabel("lcatch_filter_" + expInfo.HandlerStartOffset + ":");
          this.sourceEmitter.EmitNewLine();

          Temp temp = this.tempForStackSlot.Find(0, this.host.PlatformType.SystemUIntPtr.InternedKey);
          Contract.Assume(temp != null);
          Contract.Assume(temp.name != null);
          this.sourceEmitter.EmitString("TryCast(");
          this.sourceEmitter.EmitString("exception");
          this.sourceEmitter.EmitString(", ");
          this.sourceEmitter.EmitString(this.GetMangledTypeName(expInfo.ExceptionType));
          this.sourceEmitter.EmitString("_typeObject");
          this.sourceEmitter.EmitString(", (uintptr_t)&");
          this.sourceEmitter.EmitString(temp.name);
          this.sourceEmitter.EmitString(");");
          this.sourceEmitter.EmitNewLine();

          this.sourceEmitter.EmitString("if ((void *)");
          this.sourceEmitter.EmitString(temp.name);
          this.sourceEmitter.EmitString(" != NULL)");
          this.sourceEmitter.EmitBlockOpeningDelimiter("{");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("canHandleExp = 1;");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitBlockClosingDelimiter("}");
          this.sourceEmitter.EmitNewLine();

          this.sourceEmitter.EmitString("goto lexpSwitch" + expInfo.TryStartOffset + ";");
          this.sourceEmitter.EmitNewLine(); 

          this.sourceEmitter.EmitLabel("lrun_finallies_for_catch_" + expInfo.HandlerStartOffset + ":");
          this.sourceEmitter.EmitNewLine();
         
          foreach (IOperationExceptionInformation finallyHandler in this.finallyHandlersForCatch.GetValuesFor((uint)expInfo.HandlerStartOffset)) {
            Contract.Assert(finallyHandler != null);
            this.sourceEmitter.EmitString("if (throwOffset >= ");
            this.sourceEmitter.EmitString(finallyHandler.TryStartOffset + " && ");
            this.sourceEmitter.EmitString("throwOffset < ");
            this.sourceEmitter.EmitString(finallyHandler.TryEndOffset + ") ");
            this.sourceEmitter.EmitBlockOpeningDelimiter("{");
            this.sourceEmitter.EmitNewLine();

            // When emiting the switch table the code simply goes throgh what is in the table and emits a case after another using the location of the lable as the case value, 
            // thus we set the switch value to the number of entries currently held in the lable
            this.sourceEmitter.EmitString("expSwitchVal" + finallyHandler.TryStartOffset + " = " + this.exceptionSwitchTables.NumberOfEntries(finallyHandler.TryStartOffset) + ";");
            this.sourceEmitter.EmitNewLine();
            this.sourceEmitter.EmitString("goto l" + finallyHandler.HandlerStartOffset.ToString("x4") + ";");
            this.sourceEmitter.EmitNewLine();
            this.sourceEmitter.EmitBlockClosingDelimiter("}");
            this.sourceEmitter.EmitNewLine();
            string label = "lrun_finallies_for_catch_" + finallyHandler.HandlerStartOffset + "_" + labelCount;
            labelCount++;
            this.exceptionSwitchTables.Add(finallyHandler.TryStartOffset, label);
            this.sourceEmitter.EmitLabel(label + ":");
            this.sourceEmitter.EmitNewLine();           
          }
          this.sourceEmitter.EmitString("// Done running finally handlers. Running the catch handler now");
          this.sourceEmitter.EmitNewLine(); 
          this.sourceEmitter.EmitString("goto l" + expInfo.HandlerStartOffset.ToString("x4") + ";");
          this.sourceEmitter.EmitNewLine();
        }
      }
      this.finallyHandlersForCatch.Clear();
    }

    private void EmitExceptionSwitchTable(IEnumerable<IOperationExceptionInformation> operationExceptionInformation) {
      Contract.Requires(operationExceptionInformation != null);

      SetOfUints tryBlockOffsets = new SetOfUints();
      foreach (var expInfo in operationExceptionInformation) {
        Contract.Assume(expInfo != null);
        if (!tryBlockOffsets.Contains(expInfo.TryStartOffset+1)) {
          this.sourceEmitter.EmitLabel("lexpSwitch" + expInfo.TryStartOffset + ":");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("switch (expSwitchVal" + expInfo.TryStartOffset + ") ");
          this.sourceEmitter.EmitBlockOpeningDelimiter("{");
          this.sourceEmitter.EmitNewLine();
          int val = 0;
          foreach (string label in this.exceptionSwitchTables.GetValuesFor(expInfo.TryStartOffset)) {
            Contract.Assert(label != null);
            this.sourceEmitter.EmitString("case " + val + " :");
            this.sourceEmitter.EmitNewLine();
            this.sourceEmitter.EmitString("goto " + label + ";");
            this.sourceEmitter.EmitNewLine();
            val++;
          }
          this.sourceEmitter.EmitBlockClosingDelimiter("}");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("return 0; //dummy return to suppress warning");
          this.sourceEmitter.EmitNewLine();
          tryBlockOffsets.Add(expInfo.TryStartOffset+1);
        }
      }
      this.exceptionSwitchTables.Clear();
    }

    private void AdjustStackForArrayAddr(Instruction instruction) {
      Contract.Requires(instruction != null);

      var arrayType = instruction.Operation.Value as IArrayTypeReference;
      Contract.Assume(arrayType != null); //This is an informally specified property of the Metadata model.
      var rank = arrayType.Rank+1;
      Contract.Assume(this.operandStack.Count >= rank);
      while (rank-- > 0) this.operandStack.Pop();
      this.PushTempForSlotAndAssociateWithInstruction(instruction);
    }

    private void AdjustStackForArrayCreate(Instruction instruction) {
      Contract.Requires(instruction != null);

      var arrayType = instruction.Operation.Value as IArrayTypeReference;
      Contract.Assume(arrayType != null); //This is an informally specified property of the Metadata model.
      long rank = arrayType.Rank;
      if (instruction.Operation.OperationCode == OperationCode.Array_Create_WithLowerBound) rank *= 2;
      Contract.Assume(this.operandStack.Count >= rank);
      while (rank-- > 0) this.operandStack.Pop();
      this.PushTempForSlotAndAssociateWithInstruction(instruction);
    }

    private void AdjustStackForArraySet(Instruction instruction) {
      Contract.Requires(instruction != null);

      var arrayType = instruction.Operation.Value as IArrayTypeReference;
      Contract.Assume(arrayType != null); //This is an informally specified property of the Metadata model.
      var rank = arrayType.Rank+2;
      Contract.Assume(this.operandStack.Count >= rank);
      while (rank-- > 0) this.operandStack.Pop();
    }

    private void AdjustStackForBinaryOperation(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(this.operandStack.Count >= 2);
      this.operandStack.Pop();
      this.operandStack.Pop();
      this.PushTempForSlotAndAssociateWithInstruction(instruction);
    }

    private void AdjustStackForBinaryVoidOperation() {
      Contract.Assume(this.operandStack.Count >= 2);
      this.operandStack.Pop();
      this.operandStack.Pop();
    }

    private void AdjustStackForCall(Instruction instruction) {
      Contract.Requires(instruction != null);

      var signature = instruction.Operation.Value as ISignature;
      Contract.Assume(signature != null); //This is an informally specified property of the Metadata model.
      var methodRef = signature as IMethodReference;
      long numArguments = IteratorHelper.EnumerableCount(signature.Parameters);
      if (methodRef != null && methodRef.AcceptsExtraArguments) numArguments += IteratorHelper.EnumerableCount(methodRef.ExtraParameters);
      if (!signature.IsStatic) numArguments++;
      Contract.Assume(this.operandStack.Count >= numArguments);
      while (numArguments-- > 0) {
        Contract.Assert(numArguments+1 <= this.operandStack.Count);
        this.operandStack.Pop();
      }
      if (signature.Type.TypeCode != PrimitiveTypeCode.Void)
        this.PushTempForSlotAndAssociateWithInstruction(instruction);
    }

    private void AdjustStackForCalli(Instruction instruction) {
      Contract.Requires(instruction != null);

      var funcPointer = instruction.Operation.Value as IFunctionPointerTypeReference;
      Contract.Assume(funcPointer != null); //This is an informally specified property of the Metadata model.
      var numArguments = 1u; //the function pointer
      numArguments += IteratorHelper.EnumerableCount(funcPointer.Parameters);
      if (!funcPointer.IsStatic) numArguments++;
      Contract.Assume(this.operandStack.Count >= numArguments);
      while (numArguments-- > 0) this.operandStack.Pop();
      if (funcPointer.Type.TypeCode != PrimitiveTypeCode.Void)
        this.PushTempForSlotAndAssociateWithInstruction(instruction);
    }

    private void AdjustStackForDup(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(this.operandStack.Count >= 1);
      var temp = this.operandStack.Pop();
      instruction.result = temp;
      this.operandStack.Push(temp);
      this.operandStack.Push(temp);
    }

    private void AdjustStackForLeave() {
      this.operandStack.Clear();
    }

    private void AdjustStackForNewObject(Instruction instruction) {
      Contract.Requires(instruction != null);

      var signature = (ISignature)instruction.Operation.Value as ISignature;
      Contract.Assume(signature != null); //This is an informally specified property of the Metadata model.
      var numArguments = IteratorHelper.EnumerableCount(signature.Parameters);
      Contract.Assume(this.operandStack.Count >= numArguments);
      while (numArguments-- > 0) this.operandStack.Pop();
      this.PushTempForSlotAndAssociateWithInstruction(instruction);
    }

    private void AdjustStackForRet(ControlAndDataFlowGraph<EnhancedBasicBlock<Instruction>, Instruction> cdfg) {
      Contract.Requires(cdfg != null);

      //If ret is unreachable, the stack may already be empty.
      if (cdfg.MethodBody.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void && this.operandStack.Count > 0)
        this.operandStack.Pop();
    }

    private void AdjustStackForTernaryVoidOperation() {
      Contract.Assume(this.operandStack.Count >= 3);
      this.operandStack.Pop();
      this.operandStack.Pop();
      this.operandStack.Pop();
    }

    private void AdjustStackForUnaryOperation(Instruction instruction) {
      Contract.Requires(instruction != null);

      Contract.Assume(this.operandStack.Count >= 1);
      this.operandStack.Pop();
      this.PushTempForSlotAndAssociateWithInstruction(instruction);
    }

    private void AdjustStackForUnaryVoidOperation() {
      Contract.Assume(this.operandStack.Count >= 1);
      this.operandStack.Pop();
    }

    private void InitializeOperandStack(EnhancedBasicBlock<Instruction> block) {
      Contract.Requires(block != null);

      this.operandStack.Clear();
      foreach (var pseudoInstruction in block.OperandStack) {
        Contract.Assume(pseudoInstruction != null);
        this.PushTempForSlotAndAssociateWithInstruction(pseudoInstruction);
      }
    }

    private void PushTempForSlotAndAssociateWithInstruction(Instruction instruction) {
      Contract.Requires(instruction != null);

      var stackDepth = (uint)this.operandStack.Count;
      var stackType = TypeHelper.StackType(instruction.Type);
      if (!stackType.ResolvedType.IsValueType) stackType = this.host.PlatformType.SystemUIntPtr;
      var tempForStackSlot = this.tempForStackSlot.Find(stackDepth, stackType.InternedKey);
      if (tempForStackSlot == null) {
        tempForStackSlot = new Temp() {
          name = "_slot_"+stackDepth+"_"+stackType.InternedKey,
          type = stackType
        };
        this.tempForStackSlot.Add(stackDepth, stackType.InternedKey, tempForStackSlot);
        this.temps.Add(tempForStackSlot);
      }
      this.operandStack.Push(tempForStackSlot);
      instruction.result = tempForStackSlot;
    }
  }
}
