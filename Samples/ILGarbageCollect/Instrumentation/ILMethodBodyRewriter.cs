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
using Microsoft.Cci.Immutable;
using System.Diagnostics.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;

namespace ILGarbageCollect.Instrumentation {
  /// <summary>
  /// Generates Microsoft intermediate language (MSIL) instructions in a visitor like pattern.  This class is meant to be overridden
  /// and requires clients to implement those virtual functions (e.g. RewriteBranch) where they would like to mutate the IL being copied.
  /// </summary>
  public class ILMethodBodyRewriter {

    protected readonly IMetadataHost host;
    protected readonly MethodBody methodBody;
    protected readonly IDictionary<uint, ILGeneratorLabel> offset2Label;
    protected readonly IDictionary<uint, bool> offsetsUsedInExceptionInformation;
    protected readonly ILGenerator generator;

    /// <summary>
    /// Allocates an object that helps rewrite and mutate Microsoft intermediate language (MSIL) instructions of a method body.
    /// </summary>
    /// <param name="host">Provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.</param>
    public ILMethodBodyRewriter(IMetadataHost host, MethodBody methodBody)
      : this(host, methodBody, new ILGenerator(host, null)) {
    }

    protected ILMethodBodyRewriter(IMetadataHost host, MethodBody methodBody, ILGenerator generator) {
      Contract.Requires(host != null);
      Contract.Requires(methodBody != null);
      Contract.Requires(generator != null);      
      Contract.Requires(methodBody != Dummy.MethodBody);

      this.host = host;
      this.methodBody = methodBody;
      this.generator = generator;

      // Make a label for each branch target
      this.offset2Label = ILMethodBodyRewriter.RecordBranchTargets(this.methodBody.Operations);
      //Record all offsets that appear as part of an exception handler
      this.offsetsUsedInExceptionInformation = ILMethodBodyRewriter.RecordExceptionHandlerOffsets(this.methodBody.OperationExceptionInformation);
    }

    /// <summary>
    /// This method is called for all opcodes related to branches:
    ///    OperationCode.Beq:
    ///    OperationCode.Bge:
    ///    OperationCode.Bge_Un:
    ///    OperationCode.Bgt:
    ///    OperationCode.Bgt_Un:
    ///    OperationCode.Ble:
    ///    OperationCode.Ble_Un:
    ///    OperationCode.Blt:
    ///    OperationCode.Blt_Un:
    ///    OperationCode.Bne_Un:
    ///    OperationCode.Br:
    ///    OperationCode.Brfalse:
    ///    OperationCode.Brtrue:
    ///    OperationCode.Leave:
    ///    OperationCode.Beq_S:
    ///    OperationCode.Bge_S:
    ///    OperationCode.Bge_Un_S:
    ///    OperationCode.Bgt_S:
    ///    OperationCode.Bgt_Un_S:
    ///    OperationCode.Ble_S:
    ///    OperationCode.Ble_Un_S:
    ///    OperationCode.Blt_S:
    ///    OperationCode.Blt_Un_S:
    ///    OperationCode.Bne_Un_S:
    ///    OperationCode.Br_S:
    ///    OperationCode.Brfalse_S:
    ///    OperationCode.Brtrue_S:
    ///    OperationCode.Leave_S:
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>
    protected virtual void RewriteBranch(IOperation op) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);

      // handle branches
      switch (op.OperationCode) {
        // Branches
        case OperationCode.Beq:
        case OperationCode.Bge:
        case OperationCode.Bge_Un:
        case OperationCode.Bgt:
        case OperationCode.Bgt_Un:
        case OperationCode.Ble:
        case OperationCode.Ble_Un:
        case OperationCode.Blt:
        case OperationCode.Blt_Un:
        case OperationCode.Bne_Un:
        case OperationCode.Br:
        case OperationCode.Brfalse:
        case OperationCode.Brtrue:
        case OperationCode.Leave:
        case OperationCode.Beq_S:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un_S:
        case OperationCode.Br_S:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue_S:
        case OperationCode.Leave_S:
          this.generator.Emit(ILGenerator.LongVersionOf(op.OperationCode),
            this.offset2Label[(uint)op.Value]);
          break;
        case OperationCode.Switch:
          uint[] offsets = op.Value as uint[];
          ILGeneratorLabel[] labels = new ILGeneratorLabel[offsets.Length];
          for (int j = 0, n = offsets.Length; j < n; j++) {
            labels[j] = offset2Label[offsets[j]];
          }
          this.generator.Emit(OperationCode.Switch, labels);
          break;
        default:
          throw new Exception("Only branches should be handled here");
      }
    }

    /// <summary>
    /// This method is called for all labels.
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction that is marked as being a label.</param>
    /// <param name="label">A label that can be used to designate locations in MSIL where an instruction may jump.</param>
    protected virtual void RewriteLabel(IOperation op, ILGeneratorLabel label) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);
      Contract.Requires(label != null);

      this.generator.MarkLabel(label);
    }

    /// <summary>
    /// This method is called for all return opcodes.
    ///   OperationCode.Ret
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>
    protected virtual void RewriteReturn(IOperation op) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);
      Contract.Requires(op.OperationCode == OperationCode.Ret);

      this.RewriteOpertation(op);
    }

    /// <summary>
    /// This method is called for all load from local memory opcodes.
    ///    OperationCode.Ldloc:
    ///    OperationCode.Ldloc_0:
    ///    OperationCode.Ldloc_1:
    ///    OperationCode.Ldloc_2:
    ///    OperationCode.Ldloc_3:
    ///    OperationCode.Ldloc_S:
    ///    OperationCode.Ldarg_0:
    ///    OperationCode.Ldarg_1:
    ///    OperationCode.Ldarg_2:
    ///    OperationCode.Ldarg_3:
    ///    OperationCode.Ldarg_S:
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>
    protected virtual void RewriteLoadLocal(IOperation op) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);

      this.RewriteOpertation(op);
    }

    /// <summary>
    /// This method is called for all store to local memory opcodes.
    ///   OperationCode.Stloc:
    ///   OperationCode.Stloc_S:
    ///   OperationCode.Stloc_0:
    ///   OperationCode.Stloc_1:
    ///   OperationCode.Stloc_2:
    ///   OperationCode.Stloc_3:
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>
    protected virtual void RewriteStoreLocal(IOperation op) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);

      this.RewriteOpertation(op);
    }

    /// <summary>
    /// This method is called for all store to field opcodes.
    ///   OperationCode.Stfld:
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>
    protected virtual void RewriteStoreField(IOperation op) {
      Contract.Requires(op != null);
      Contract.Requires(op.OperationCode == OperationCode.Stfld);
      Contract.Requires(op.Value != null);
      Contract.Requires(op.Value is IFieldReference);

      this.RewriteOpertation(op);
    }

    /// <summary>
    /// This method is called for all load from field opcodes.
    ///   OperationCode.Ldfld:
    ///   OperationCode.Ldflda:
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>
    protected virtual void RewriteLoadField(IOperation op) {
      Contract.Requires(op != null);
      Contract.Requires(op.OperationCode == OperationCode.Ldfld || op.OperationCode == OperationCode.Ldflda);
      Contract.Requires(op.Value != null);
      Contract.Requires(op.Value is IFieldReference);

      this.RewriteOpertation(op);
    }


    /// <summary>
    /// This method is called for all opcodes that create new objects or arrays.
    ///   OperationCode.Newarr:
    ///   OperationCode.Newobj:    
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>
    protected virtual void RewriteNewObject(IOperation op) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);

      this.RewriteOpertation(op);
    }

    /// <summary>
    /// This method is called for all call opcodes.
    ///   OperationCode.Call:
    ///   OperationCode.Calli:   
    ///   OperationCode.Callvirt:
    ///   OperationCode.Jmp:   
    /// </summary>
    /// <param name="op">The Microsoft intermediate language (MSIL) instruction to be copied.</param>    
    protected virtual void RewriteCall(IOperation op) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);

      this.RewriteOpertation(op);
    }

    /// <summary>
    /// This method is called just before we visit the first opcode.
    /// </summary>
    protected virtual void Start() {
      Contract.Requires(this.generator != null);
    }


    /// <summary>
    /// Creates a type reference anchored in the given assembly reference and whose names are relative to the given host.
    /// When the type name has periods in it, a structured reference with nested namespaces is created.
    /// </summary>
    protected static INamespaceTypeReference CreateTypeReference(IMetadataHost host, IAssemblyReference assemblyReference, string typeName) {
      Contract.Requires(host != null);
      Contract.Requires(assemblyReference != null);
      Contract.Requires(assemblyReference != Dummy.AssemblyReference);
      Contract.Requires(typeName != null);
      Contract.Requires(typeName != string.Empty);

      Contract.Ensures(Contract.Result<INamespaceTypeReference>() != null);
      Contract.Ensures(Contract.Result<INamespaceTypeReference>() != Dummy.NamespaceTypeReference);

      IUnitNamespaceReference ns = new Microsoft.Cci.Immutable.RootUnitNamespaceReference(assemblyReference);
      string[] names = typeName.Split('.');
      for (int i = 0, n = names.Length - 1; i < n; i++)
        ns = new Microsoft.Cci.Immutable.NestedUnitNamespaceReference(ns, host.NameTable.GetNameFor(names[i]));
      return new Microsoft.Cci.Immutable.NamespaceTypeReference(host, ns, host.NameTable.GetNameFor(names[names.Length - 1]), 0, false, false, true, PrimitiveTypeCode.NotPrimitive);
    }



    /// <summary>
    /// This method is the workhorse that does the rewrite of the IOperation op.
    /// </summary>
    /// <param name="op">The MSIL operation to rewrite.</param>
    protected void RewriteOpertation(IOperation op) {
      Contract.Requires(this.generator != null);
      Contract.Requires(op != null);

      if (op.Value == null) {
        generator.Emit(op.OperationCode);
        return;
      }
      var typeCode = System.Convert.GetTypeCode(op.Value);
      switch (typeCode) {
        case TypeCode.Byte:
          this.generator.Emit(op.OperationCode, (byte)op.Value);
          break;
        case TypeCode.Double:
          this.generator.Emit(op.OperationCode, (double)op.Value);
          break;
        case TypeCode.Int16:
          this.generator.Emit(op.OperationCode, (short)op.Value);
          break;
        case TypeCode.Int32:
          this.generator.Emit(op.OperationCode, (int)op.Value);
          break;
        case TypeCode.Int64:
          this.generator.Emit(op.OperationCode, (long)op.Value);
          break;
        case TypeCode.Object:
          IFieldReference fieldReference = op.Value as IFieldReference;
          if (fieldReference != null) {
            this.generator.Emit(op.OperationCode, fieldReference);
            break;
          }
          ILocalDefinition localDefinition = op.Value as ILocalDefinition;
          if (localDefinition != null) {
            this.generator.Emit(op.OperationCode, localDefinition);
            break;
          }
          IMethodReference methodReference = op.Value as IMethodReference;
          if (methodReference != null) {
            this.generator.Emit(op.OperationCode, methodReference);
            break;
          }
          IParameterDefinition parameterDefinition = op.Value as IParameterDefinition;
          if (parameterDefinition != null) {
            this.generator.Emit(op.OperationCode, parameterDefinition);
            break;
          }
          ISignature signature = op.Value as ISignature;
          if (signature != null) {
            this.generator.Emit(op.OperationCode, signature);
            break;
          }
          ITypeReference typeReference = op.Value as ITypeReference;
          if (typeReference != null) {
            this.generator.Emit(op.OperationCode, typeReference);
            break;
          }
          throw new Exception("Should never get here: no other IOperation argument types should exist");
        case TypeCode.SByte:
          this.generator.Emit(op.OperationCode, (sbyte)op.Value);
          break;
        case TypeCode.Single:
          this.generator.Emit(op.OperationCode, (float)op.Value);
          break;
        case TypeCode.String:
          this.generator.Emit(op.OperationCode, (string)op.Value);
          break;
        default:
          // The other cases are the other enum values that TypeCode has.
          // But no other argument types should be in the Operations. ILGenerator cannot handle anything else,
          // so such IOperations should never exist.
          //case TypeCode.Boolean:
          //case TypeCode.Char:
          //case TypeCode.DateTime:
          //case TypeCode.DBNull:
          //case TypeCode.Decimal:
          //case TypeCode.Empty: // this would be the value for null, but the case when op.Value is null is handled before the switch statement
          //case TypeCode.UInt16:
          //case TypeCode.UInt32:
          //case TypeCode.UInt64:
          throw new Exception("Should never get here: no other IOperation argument types should exist");
      }
    }

    /// <summary>
    /// This method is called after a complete traversal of MSIL.  A client of this class should update the MaxStack property based on how much the
    /// client has changed the maximum stack size of the method. 
    /// </summary>
    /// <param name="maxstack">The maximum size of the execution stack for this method.</param>
    protected virtual void RewriteMaxStack(ushort maxstack) {
      this.methodBody.MaxStack = maxstack;
    }

    /// <summary>
    /// This method is called after a complete traversal of MSIL.  A client should update the LocalsAreZeroed property if they add local variables
    /// that need to be zeroed on initialization.
    /// </summary>
    /// <param name="b">Whether locals are zeroed on initialization for this method.</param>
    protected virtual void RewriteLocalsAreZeroed(bool b) {
      this.methodBody.LocalsAreZeroed = b;
    }


    /// <summary>
    /// This method starts the traversal of opcodes.  It copies the MSIL operations and exception information into the out parameters.
    /// </summary>
    /// <param name="ops">After this method executes, ops will contain a list of MSIL instructions that can be saved into a MethodBody</param>
    /// <param name="exops">After this method executes, exops will contain a list of exception information that can be saved into a MethodBody</param>
    public void Rewrite() {
      // Emit each operation, along with labels
      this.Start();
      foreach (var op in this.methodBody.Operations) {
        if (op.Location is IILLocation) {
          this.generator.MarkSequencePoint(op.Location);
        }

        // Mark operation if it is a label for a branch
        ILGeneratorLabel label;
        if (offset2Label.TryGetValue(op.Offset, out label)) {          
          this.RewriteLabel(op, label);
        }

        // Mark operation if it is pointed to by an exception handler
        this.HandleException(op);

        // Emit operation along with any injection
        this.RewriteAny(op);
      }
      while (generator.InTryBody)
        generator.EndTryBody();

      // fix up branches and labels
      this.generator.AdjustBranchSizesToBestFit();
      
      this.methodBody.Operations = new List<IOperation>(this.generator.GetOperations());
      this.methodBody.OperationExceptionInformation = new List<IOperationExceptionInformation>(this.generator.GetOperationExceptionInformation());

      // last step: let the subclass update the max stack of this method and/or update whether locals are zeroed
      this.RewriteMaxStack(this.methodBody.MaxStack);
      this.RewriteLocalsAreZeroed(this.methodBody.LocalsAreZeroed);
    }

    private void HandleException(IOperation op) {
      bool ignore;
      uint offset = op.Offset;
      if (offsetsUsedInExceptionInformation.TryGetValue(offset, out ignore)) {
        foreach (var exceptionInfo in this.methodBody.OperationExceptionInformation) {
          if (offset == exceptionInfo.TryStartOffset)
            generator.BeginTryBody();

          // Never need to do anthing when offset == exceptionInfo.TryEndOffset because
          // we pick up an EndTryBody from the HandlerEndOffset below
          //  generator.EndTryBody();

          if (offset == exceptionInfo.HandlerStartOffset) {
            switch (exceptionInfo.HandlerKind) {
              case HandlerKind.Catch:
                generator.BeginCatchBlock(exceptionInfo.ExceptionType);
                break;
              case HandlerKind.Fault:
                generator.BeginFaultBlock();
                break;
              case HandlerKind.Filter:
                generator.BeginFilterBody();
                break;
              case HandlerKind.Finally:
                generator.BeginFinallyBlock();
                break;
            }
          }
          if (exceptionInfo.HandlerKind == HandlerKind.Filter && offset == exceptionInfo.FilterDecisionStartOffset) {
            generator.BeginFilterBlock();
          }
          if (offset == exceptionInfo.HandlerEndOffset)
            generator.EndTryBody();
        }
      }
    }

    /// <summary>
    /// This method is called for every operation code. If a client needs to intercept each opcode, this is the method to override. Note that doing so means that 
    /// the other RewriteOpertation methods will not be called.
    /// </summary>
    /// <param name="op">The MSIL operation to rewrite.</param>
    protected virtual void RewriteAny(IOperation op) {
      switch (op.OperationCode) {
        // Branches
        case OperationCode.Beq:
        case OperationCode.Bge:
        case OperationCode.Bge_Un:
        case OperationCode.Bgt:
        case OperationCode.Bgt_Un:
        case OperationCode.Ble:
        case OperationCode.Ble_Un:
        case OperationCode.Blt:
        case OperationCode.Blt_Un:
        case OperationCode.Bne_Un:
        case OperationCode.Br:
        case OperationCode.Brfalse:
        case OperationCode.Brtrue:
        case OperationCode.Leave:
        case OperationCode.Beq_S:
        case OperationCode.Bge_S:
        case OperationCode.Bge_Un_S:
        case OperationCode.Bgt_S:
        case OperationCode.Bgt_Un_S:
        case OperationCode.Ble_S:
        case OperationCode.Ble_Un_S:
        case OperationCode.Blt_S:
        case OperationCode.Blt_Un_S:
        case OperationCode.Bne_Un_S:
        case OperationCode.Br_S:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue_S:
        case OperationCode.Leave_S:
        case OperationCode.Switch:
          this.RewriteBranch(op);
          break;

        // calls
        case OperationCode.Call:
        case OperationCode.Calli:
        case OperationCode.Callvirt:
        case OperationCode.Jmp:
          this.RewriteCall(op);
          break;

        // loads from args/locals
        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S:
          this.RewriteLoadLocal(op);
          break;

        // stores to locals
        case OperationCode.Stloc:
        case OperationCode.Stloc_S:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
          this.RewriteStoreLocal(op);
          break;

        // return opcode: always the last of funtion
        case OperationCode.Ret:
          this.RewriteReturn(op);
          break;

        // new objects
        case OperationCode.Newarr:
        case OperationCode.Newobj:
          this.RewriteNewObject(op);
          break;

        // set field
        case OperationCode.Stfld:
          this.RewriteStoreField(op);
          break;

        // load field
        case OperationCode.Ldflda:
        case OperationCode.Ldfld:
          this.RewriteLoadField(op);
          break;

        // Everything else
        default:
          this.RewriteOpertation(op);
          break;
      }
    }

    private static IDictionary<uint, ILGeneratorLabel> RecordBranchTargets(IEnumerable<IOperation> operations) {
      var offset2Label = new Dictionary<uint, ILGeneratorLabel>();

      // for first block
      offset2Label.Add(0, new ILGeneratorLabel());

      // denotes whether the prior instruction was a branch that fell through
      // to the current instruction
      bool fellThroughBranch = false;

      foreach (var op in operations) {
        // if true here the prior op was a branch to here.
        // we need to add a label if it is not in the 
        // offset2label map yet.
        if (fellThroughBranch) {
          if (!offset2Label.ContainsKey(op.Offset))
            offset2Label.Add(op.Offset, new ILGeneratorLabel());
          fellThroughBranch = false;
        }

        switch (op.OperationCode) {
          case OperationCode.Beq:
          case OperationCode.Bge:
          case OperationCode.Bge_Un:
          case OperationCode.Bgt:
          case OperationCode.Bgt_Un:
          case OperationCode.Ble:
          case OperationCode.Ble_Un:
          case OperationCode.Blt:
          case OperationCode.Blt_Un:
          case OperationCode.Bne_Un:
          case OperationCode.Br:
          case OperationCode.Brfalse:
          case OperationCode.Brtrue:
          case OperationCode.Leave:
          case OperationCode.Beq_S:
          case OperationCode.Bge_S:
          case OperationCode.Bge_Un_S:
          case OperationCode.Bgt_S:
          case OperationCode.Bgt_Un_S:
          case OperationCode.Ble_S:
          case OperationCode.Ble_Un_S:
          case OperationCode.Blt_S:
          case OperationCode.Blt_Un_S:
          case OperationCode.Bne_Un_S:
          case OperationCode.Br_S:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue_S:
          case OperationCode.Leave_S:
            uint x = (uint)op.Value;
            if (!offset2Label.ContainsKey(x)) offset2Label.Add(x, new ILGeneratorLabel());
            // add label if we fell through from lastop
            if (op.OperationCode != OperationCode.Br ||
                op.OperationCode != OperationCode.Br_S ||
                op.OperationCode != OperationCode.Leave ||
                op.OperationCode != OperationCode.Leave_S) {
              // used to tell next opcode that we fell through this branch
              fellThroughBranch = true;
            }
            break;
          case OperationCode.Switch:
            uint[] offsets = op.Value as uint[];
            foreach (var offset in offsets) {
              if (!offset2Label.ContainsKey(offset))
                offset2Label.Add(offset, new ILGeneratorLabel());
            }
            break;
          default:
            break;
        }
      }
      return offset2Label;
    }

    private static IDictionary<uint, bool> RecordExceptionHandlerOffsets(IEnumerable<IOperationExceptionInformation> ops) {
      var offsetsUsedInExceptionInformation = new Dictionary<uint, bool>();
      foreach (var exceptionInfo in ops) {
        uint x = exceptionInfo.TryStartOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        x = exceptionInfo.TryEndOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        x = exceptionInfo.HandlerStartOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        x = exceptionInfo.HandlerEndOffset;
        if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        if (exceptionInfo.HandlerKind == HandlerKind.Filter) {
          x = exceptionInfo.FilterDecisionStartOffset;
          if (!offsetsUsedInExceptionInformation.ContainsKey(x)) offsetsUsedInExceptionInformation.Add(x, true);
        }
      }
      return offsetsUsedInExceptionInformation;
    }
  }
}