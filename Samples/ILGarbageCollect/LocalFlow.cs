using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Diagnostics.Contracts;

using Microsoft.Cci;
using Microsoft.Cci.Analysis;

namespace ILGarbageCollect.LocalFlow {

  #region Key Interfaces

  // C: The underlying carrier type.
  internal interface IAbstraction<C> {

    bool LessThanOrEqual(C lhs, C rhs);

    C Join(C lhs, C rhs);

    C Bottom { get; }

    C Top { get; }

  }

  // C: The underlying carrier type.
  internal interface IValueAbstraction<C> : IAbstraction<C> {
    C GetAbstractValueForType(ITypeDefinition type);

    // Could have GetAbstractValueForNull(), GetAbstractValueForInteger(int i), etc.
    // but for now we'll stick with types
  }

  // A: a state abstraction
  // S: an abstract state
  internal abstract class StateInterpreter<A, S> where A : IAbstraction<S> {

    internal StateInterpreter(A stateAbstraction) {
      StateAbstraction = stateAbstraction;
    }

    public abstract S IntepretBlockInState(BasicBlock<Instruction> block, S preState, IMethodDefinition methodDefinition);

    public abstract S EntryPreState(IMethodDefinition method);

    internal A StateAbstraction { get; private set; }

  }

#endregion


  #region Locals and Operands Machine State
  // V an abstract value
  internal class LocalsAndOperands<V> {

    public List<V> Arguments { get; private set; }

    public List<V> Locals { get; private set; }

    public List<V> OperandStack { get; private set; }

    public LocalsAndOperands() {

      Arguments = new List<V>();
      Locals = new List<V>();
      OperandStack = new List<V>();
    }

    public V Pop() {
      Contract.Requires(OperandStack.Count() > 0);

      V poppedValue = OperandStack.Last();
      OperandStack.RemoveAt(OperandStack.Count() - 1);

      return poppedValue;
    }

    public void Push(V v) {
      OperandStack.Add(v);
    }

    public override string ToString() {
      return "Locals: " + Locals.Count() + " Operands: " + OperandStack.Count();
    }

    public LocalsAndOperands<V> Copy() {
      // We don't make copies of the values themselves;
      // just the lists.
      LocalsAndOperands<V> copy = new LocalsAndOperands<V>();

      copy.Arguments = new List<V>(Arguments);
      copy.Locals = new List<V>(Locals);
      copy.OperandStack = new List<V>(OperandStack);

      return copy;
    }
  }

  // A a value abstraction
  // V an abstract value
  internal class LocalsAndOperandsStateAbstraction<A, V> : IAbstraction<LocalsAndOperands<V>> where A : IAbstraction<V> {

    internal A ValueAbstraction { get; private set; }

    private readonly LocalsAndOperands<V> top;

    public LocalsAndOperandsStateAbstraction(A ValueAbstraction) {
      this.ValueAbstraction = ValueAbstraction;
      top = new LocalsAndOperands<V>();  // We use an arbitrary allocated value to be top
    }

    public bool LessThanOrEqual(LocalsAndOperands<V> lhs, LocalsAndOperands<V> rhs) {

      if (lhs == Bottom) {
        return true;
      }
      else if (rhs == Bottom) {
        // if we've gotten here lhs is NOT bottom
        // so there is no way lhs <= rhs
        return false;
      }

      if (rhs == Top) {
        return true;
      }
      else if (lhs == Top) {
        return false;
      }

      Contract.Assert(lhs.Arguments.Count() == rhs.Arguments.Count());
      Contract.Assert(lhs.Locals.Count() == rhs.Locals.Count());
      Contract.Assert(lhs.OperandStack.Count() == rhs.OperandStack.Count());

      return ValueListLessThanOrEqual(lhs.Locals, rhs.Locals) && ValueListLessThanOrEqual(lhs.OperandStack, lhs.OperandStack);
    }

    private bool ValueListLessThanOrEqual(IList<V> lhs, IList<V> rhs) {
      Contract.Requires(lhs != null);
      Contract.Requires(rhs != null);

      Contract.Requires(lhs.Count() == rhs.Count());

      for (int i = 0; i < lhs.Count(); i++) {
        V lhsValue = lhs.ElementAt(i);
        V rhsValue = rhs.ElementAt(i);

        if (!ValueAbstraction.LessThanOrEqual(lhsValue, rhsValue)) {
          return false;
        }
      }

      return true;
    }

    public LocalsAndOperands<V> Join(LocalsAndOperands<V> lhs, LocalsAndOperands<V> rhs) {

      if (lhs == Bottom) {
        return rhs;
      }
      else if (rhs == Bottom) {
        return lhs;
      }

      if (lhs == Top || rhs == Top) {
        return Top;
      }

      LocalsAndOperands<V> joinedState = new LocalsAndOperands<V>();

      JoinListsIntoTarget(lhs.Arguments, rhs.Arguments, joinedState.Arguments);
      JoinListsIntoTarget(lhs.Locals, rhs.Locals, joinedState.Locals);
      JoinListsIntoTarget(lhs.OperandStack, rhs.OperandStack, joinedState.OperandStack);

      return joinedState;
    }

    private void JoinListsIntoTarget(IList<V> lhs, IList<V> rhs, IList<V> target) {
      Contract.Requires(lhs != null);
      Contract.Requires(rhs != null);

      Contract.Requires(lhs.Count() == rhs.Count());
      Contract.Requires(target.Count() == 0);

      Contract.Ensures(lhs.Count() == target.Count());

      for (int i = 0; i < lhs.Count(); i++) {
        V lhsValue = lhs.ElementAt(i);
        V rhsValue = rhs.ElementAt(i);

        V joinedLocal = ValueAbstraction.Join(lhsValue, rhsValue);

        target.Add(joinedLocal);
      }
    }

    public LocalsAndOperands<V> Bottom {
      get {
        return null;
      }
    }

    public LocalsAndOperands<V> Top {
      get {
        return top;
      }
    }
  }

  // A a value abstraction
  // V an abstract value
  internal class LocalsAndOperandStackStateInterpreter<A, V> : StateInterpreter<LocalsAndOperandsStateAbstraction<A, V>, LocalsAndOperands<V>> where A : IValueAbstraction<V> {

    internal A ValueAbstraction { get { return this.StateAbstraction.ValueAbstraction; } }

    public LocalsAndOperandStackStateInterpreter(A valueAbstraction)
      : base(new LocalsAndOperandsStateAbstraction<A, V>(valueAbstraction)) {
    }

    public override LocalsAndOperands<V> EntryPreState(IMethodDefinition method) {
      // entry pre-state is empty operand stack
      // with locals initialized
      // 
      LocalsAndOperands<V> state = new LocalsAndOperands<V>();


      // If method has a this, parameter, it is the first argument

      if (!method.IsStatic && !method.HasExplicitThisParameter) {
        ITypeDefinition instantiatedThis = GarbageCollectHelper.InstantiatedTypeIfPossible(method.ContainingTypeDefinition);

        state.Arguments.Add(ValueAbstraction.GetAbstractValueForType(instantiatedThis));
      }

      foreach (IParameterDefinition parameter in method.Parameters) {
        state.Arguments.Add(ValueAbstraction.GetAbstractValueForType(parameter.Type.ResolvedType));
      }

      foreach (ILocalDefinition localVariable in method.Body.LocalVariables) {
        ITypeDefinition typeOfLocal = localVariable.Type.ResolvedType;

        state.Locals.Add(ValueAbstraction.GetAbstractValueForType(typeOfLocal));
      }

      return state;
    }

    public override LocalsAndOperands<V> IntepretBlockInState(BasicBlock<Instruction> block, LocalsAndOperands<V> preState, IMethodDefinition method) {
    
      

      LocalsAndOperands<V> currentState = preState;

      foreach (Instruction instruction in block.Instructions) {
        currentState = InterpretInstructionInState(instruction, currentState, method);
      }

      return currentState;
    }

    // Interpret the instruction in the given abstract state and return the resulting abstract state
    private LocalsAndOperands<V> InterpretInstructionInState(Instruction instruction, LocalsAndOperands<V> preState, IMethodDefinition method) {

      LocalsAndOperands<V> currentState = preState.Copy();

      // Switch cases copied directly from Cci's DataFlowInferencer

      NotePreStateBeforeInterpretingInstruction(currentState, instruction);

      switch (instruction.Operation.OperationCode) {
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
        case OperationCode.Ldelema:
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
          //instruction.Operand2 = stack.Pop();
          //instruction.Operand1 = stack.Pop();
          //stack.Push(instruction);
          currentState.Pop();
          currentState.Pop();
          //currentState.Push(ValueAbstraction.Top); // Not right
          currentState.Push(ValueAbstraction.GetAbstractValueForType(FixupTypeForFlow(instruction.Type.ResolvedType))); // still not right
          break;


        case OperationCode.Ldarg:
        case OperationCode.Ldarg_0:
        case OperationCode.Ldarg_1:
        case OperationCode.Ldarg_2:
        case OperationCode.Ldarg_3:
        case OperationCode.Ldarg_S: 
          {
            int argumentIndex = GetArgumentIndexFromParameterOperation(instruction.Operation, method);
            V argumentValue = currentState.Arguments.ElementAt(argumentIndex);

            currentState.Push(argumentValue); 
          }       
          break;

        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S: {
            Contract.Assume(instruction.Operation.Value is ILocalDefinition);

            ILocalDefinition local = instruction.Operation.Value as ILocalDefinition;

            int localIndex = GetLocalIndexFromLocalOperation(instruction.Operation, method);

            V localValue = currentState.Locals.ElementAt(localIndex);

            currentState.Push(localValue);
          }

          break;

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
        case OperationCode.Arglist:
          //stack.Push(instruction);
          //currentState.Push(ValueAbstraction.Top); // not right
          currentState.Push(ValueAbstraction.GetAbstractValueForType(FixupTypeForFlow(instruction.Type.ResolvedType))); // Still not right
          break;

        case OperationCode.Array_Addr:
        case OperationCode.Array_Get:
          Contract.Assume(instruction.Operation.Value is IArrayTypeReference); //This is an informally specified property of the Metadata model.
          //InitializeArrayIndexerInstruction(instruction, stack, (IArrayTypeReference)instruction.Operation.Value);
          InitializeArrayIndexerInstruction(instruction, currentState, (IArrayTypeReference)instruction.Operation.Value);
          break;

        case OperationCode.Array_Create:
        case OperationCode.Array_Create_WithLowerBound:
        case OperationCode.Newarr:
          InitializeArrayCreateInstruction(instruction, currentState, instruction.Operation);
          break;

        case OperationCode.Array_Set:
          /*stack.Pop();
          Contract.Assume(instruction.Operation.Value is IArrayTypeReference); //This is an informally specified property of the Metadata model.
          InitializeArrayIndexerInstruction(instruction, stack, (IArrayTypeReference)instruction.Operation.Value);
          */
          currentState.Pop();
          Contract.Assume(instruction.Operation.Value is IArrayTypeReference); //This is an informally specified property of the Metadata model.
          InitializeArrayIndexerInstruction(instruction, currentState, (IArrayTypeReference)instruction.Operation.Value);

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
          /*instruction.Operand2 = stack.Pop();
          instruction.Operand1 = stack.Pop();
           */
          currentState.Pop();
          currentState.Pop();
          break;

        case OperationCode.Box:
        case OperationCode.Castclass:
        case OperationCode.Ckfinite:
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
        case OperationCode.Isinst:
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
        case OperationCode.Ldvirtftn:
        case OperationCode.Localloc:
        case OperationCode.Mkrefany:
        case OperationCode.Neg:
        case OperationCode.Not:
        case OperationCode.Refanytype:
        case OperationCode.Refanyval:
        case OperationCode.Unbox:
        case OperationCode.Unbox_Any:
          /*instruction.Operand1 = stack.Pop();
          stack.Push(instruction);
           */
          currentState.Pop();
          //currentState.Push(ValueAbstraction.Top); // Could be smarter than Top
          currentState.Push(ValueAbstraction.GetAbstractValueForType(FixupTypeForFlow(instruction.Type.ResolvedType))); // Still not right
          break;

        case OperationCode.Brfalse:
        case OperationCode.Brfalse_S:
        case OperationCode.Brtrue:
        case OperationCode.Brtrue_S:
          //instruction.Operand1 = stack.Pop();
          currentState.Pop();
          break;

        case OperationCode.Call:
          /*var signature = instruction.Operation.Value as ISignature;
          Contract.Assume(signature != null); //This is an informally specified property of the Metadata model.
          if (!signature.IsStatic) instruction.Operand1 = stack.Pop();
          InitializeArgumentsAndPushReturnResult(instruction, stack, signature);
          */
          var signature = instruction.Operation.Value as ISignature;
          Contract.Assume(signature != null); //This is an informally specified property of the Metadata model.

          InterpretCallWithSignature(currentState, signature, !signature.IsStatic);
          break;

        case OperationCode.Callvirt:
          /*
          instruction.Operand1 = stack.Pop();
          Contract.Assume(instruction.Operation.Value is ISignature); //This is an informally specified property of the Metadata model.
          InitializeArgumentsAndPushReturnResult(instruction, stack, (ISignature)instruction.Operation.Value);
          */

          
          Contract.Assume(instruction.Operation.Value is ISignature); //This is an informally specified property of the Metadata model.
          InterpretCallWithSignature(currentState, (ISignature)instruction.Operation.Value, true);
          break;

        case OperationCode.Calli:
          /*
          Contract.Assume(instruction.Operation.Value is ISignature); //This is an informally specified property of the Metadata model.
          InitializeArgumentsAndPushReturnResult(instruction, stack, (ISignature)instruction.Operation.Value);
          instruction.Operand1 = stack.Pop();
          */
         

          currentState.Pop(); // the method pointer

          signature = instruction.Operation.Value as ISignature;
          Contract.Assume(signature != null); //This is an informally specified property of the Metadata model.
          InterpretCallWithSignature(currentState, (ISignature)instruction.Operation.Value, !signature.IsStatic);

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
          /*
          instruction.Operand2 = stack.Pop();
          instruction.Operand1 = stack.Pop();
          */
          currentState.Pop();
          currentState.Pop();

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
          /*var indexAndValue = new Instruction[2];
          indexAndValue[1] = stack.Pop();
          indexAndValue[0] = stack.Pop();
          instruction.Operand2 = indexAndValue;
          instruction.Operand1 = stack.Pop();
          */

          
          currentState.Pop();
          currentState.Pop();
          currentState.Pop();
          break;

        case OperationCode.Dup:
          /*
          var dupop = stack.Pop();
          instruction.Operand1 = dupop;
          stack.Push(dupop);
          stack.Push(instruction);
          */
          // Pop the top and push it twice
          var dupValue = currentState.Pop();
          currentState.Push(dupValue);
          currentState.Push(dupValue);
          break;


        
        case OperationCode.Starg:
        case OperationCode.Starg_S: 
          {
            int argumentIndex = GetArgumentIndexFromParameterOperation(instruction.Operation, method);

            V newArgumentValue = currentState.Pop();

            currentState.Arguments[argumentIndex] = newArgumentValue;
          }
          break;

        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S: {
            Contract.Assume(instruction.Operation.Value is ILocalDefinition);

            ILocalDefinition local = instruction.Operation.Value as ILocalDefinition;

            int localIndex = GetLocalIndexFromLocalOperation(instruction.Operation, method);

            V newLocalValue = currentState.Pop();

            currentState.Locals[localIndex] = newLocalValue;
          }
          break;


        case OperationCode.Endfilter:
        case OperationCode.Initobj:
        case OperationCode.Pop:

        case OperationCode.Stsfld:
        case OperationCode.Throw:
        case OperationCode.Switch:
          //instruction.Operand1 = stack.Pop();

          currentState.Pop();
          break;

        case OperationCode.Newobj:
          /*
          Contract.Assume(instruction.Operation.Value is ISignature); //This is an informally specified property of the Metadata model.
          InitializeArgumentsAndPushReturnResult(instruction, stack, (ISignature)instruction.Operation.Value); //won't push anything
          stack.Push(instruction);
          */

          Contract.Assume(instruction.Operation.Value is IMethodReference); //This is an informally specified property of the Metadata model.
          InterpretCallWithSignature(currentState, (ISignature)instruction.Operation.Value, false); //won't push anything, since return type is void


          currentState.Push(ValueAbstraction.GetAbstractValueForType(FixupTypeForFlow(((IMethodReference)instruction.Operation.Value).ResolvedMethod.ContainingTypeDefinition)));
           
          break;

        case OperationCode.Ret:
          /*if (this.cdfg.MethodBody.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void)
            instruction.Operand1 = stack.Pop();
           */
          if (method.Type.TypeCode != PrimitiveTypeCode.Void) {
            currentState.Pop();
          }
          break;
      }
      
      return currentState;
    }

    private int GetArgumentIndexFromParameterOperation(IOperation operation, IMethodDefinition method) {
      // Should add precondition about operation being a parameter operation

      var parameter = operation.Value as IParameterDefinition;

      int argumentIndex;


      if (method.IsStatic) {
        Contract.Assume(parameter != null);

        argumentIndex = parameter.Index;
      }
      else {
        // we're an instance method, so argument 0 is 'this'
        if (parameter != null) {
          // The argument is a true parameter
          argumentIndex = parameter.Index + 1;
        }
        else {
          // the argument is 'this'
          argumentIndex = 0;
        }
      }

      return argumentIndex;
    }

    private int GetLocalIndexFromLocalOperation(IOperation operation, IMethodDefinition method) {
      Contract.Assert(operation.Value is ILocalDefinition);

      ILocalDefinition localDefinition = operation.Value as ILocalDefinition;

      // Note the most efficient; should perhaps stash in a map somewhere
      return method.Body.LocalVariables.ToList().IndexOf(localDefinition);
    }

    private void InterpretCallWithSignature(LocalsAndOperands<V> state, ISignature signature, bool hasReceiver) {
      /*Contract.Requires(instruction != null);
      Contract.Requires(stack != null);
      Contract.Requires(signature != null);
      var numArguments = IteratorHelper.EnumerableCount(signature.Parameters);
      var arguments = new Instruction[numArguments];
      instruction.Operand2 = arguments;
      for (var i = numArguments; i > 0; i--)
        arguments[i - 1] = stack.Pop();
      if (signature.Type.TypeCode != PrimitiveTypeCode.Void)
        stack.Push(instruction);
      */

      var numArguments = IteratorHelper.EnumerableCount(signature.Parameters);
      for (var i = numArguments; i > 0; i--) {
        state.Pop();
      }

      if (hasReceiver) {
        state.Pop();
      }

      if (signature.Type.TypeCode != PrimitiveTypeCode.Void) {
        state.Push(ValueAbstraction.GetAbstractValueForType(FixupTypeForFlow(signature.Type.ResolvedType))); // Should be smarter than just Top
      }
    }

    private void InitializeArrayCreateInstruction(Instruction instruction, LocalsAndOperands<V> state, IOperation currentOperation) {
     /* Contract.Requires(instruction != null);
      Contract.Requires(stack != null);
      Contract.Requires(currentOperation != null);
      IArrayTypeReference arrayType = (IArrayTypeReference)currentOperation.Value;
      Contract.Assume(arrayType != null); //This is an informally specified property of the Metadata model.
      var rank = arrayType.Rank;
      if (currentOperation.OperationCode == OperationCode.Array_Create_WithLowerBound) rank *= 2;
      var indices = new Instruction[rank];
      instruction.Operand2 = indices;
      for (var i = rank; i > 0; i--)
        indices[i - 1] = stack.Pop();
      stack.Push(instruction);
     */
     
      IArrayTypeReference arrayType = (IArrayTypeReference)currentOperation.Value;
      Contract.Assume(arrayType != null); //This is an informally specified property of the Metadata model.
      var rank = arrayType.Rank;
      if (currentOperation.OperationCode == OperationCode.Array_Create_WithLowerBound) rank *= 2;
      for (var i = rank; i > 0; i--) {
        state.Pop();
      }
      state.Push(ValueAbstraction.Top); // can be smarter than Top
    }

    private void InitializeArrayIndexerInstruction(Instruction instruction, LocalsAndOperands<V> state, IArrayTypeReference arrayType) {
      // Copied from Cci's DataFlowInferencer.cs
      /* Contract.Requires(instruction != null);
      Contract.Requires(stack != null);
      Contract.Requires(arrayType != null);
      var rank = arrayType.Rank;
      var indices = new Instruction[rank];
      instruction.Operand2 = indices;
      for (var i = rank; i > 0; i--)
        indices[i - 1] = stack.Pop();
      instruction.Operand1 = stack.Pop();
      stack.Push(instruction);
      */

      var rank = arrayType.Rank;

      for (var i = rank; i > 0; i--) {
         state.Pop();
      }

      state.Pop();

      state.Push(ValueAbstraction.Top); // need to be smarter about top
    }

    private ITypeDefinition FixupTypeForFlow(ITypeDefinition type) {

      // If the type is generic but we're not instantiated, we try to instantiate it.
      // This is required because the CFG is over the completely unspecialized uninstantiated
      // bytecode while but some instruction *types* (e.g. calls) require instantiate types.

      // This has got to be slow.

      // Another alternative would have been to constrcut the CFG over the instantiated type
      // but this appears to be buggy at the moment.


      return GarbageCollectHelper.InstantiatedTypeIfPossible(type);
    }


    /// <summary>
    /// Hook for subclasses to use to get their hands the analysis prestate before a given instruction.
    /// 
    /// This is called every time the instruction is interpreted (i.e. it may be called multiple times if the
    /// instruction is in a loop. Overrides should only use the final version of the prestate passed.
    /// 
    /// Note: it is the overrider's responsibility to COPY the preState if it stashes it away. This state is MUTABLE
    /// and will change as further instructions in the basic block are interpreted.
    /// You've been warned.
    /// </summary>
    /// <param name="preState"></param>
    /// <param name="instruction"></param>
    virtual protected void NotePreStateBeforeInterpretingInstruction(LocalsAndOperands<V> preState, Instruction instruction) {
      // default is to do nothing
    }
  }

  #endregion

  #region Worklist


  // I: a state interpreter
  // A: a state abstraction
  // S: an abstract state
  internal sealed class WorkListAlgorithm<I, A, S>
    where I : StateInterpreter<A, S>
    where A : IAbstraction<S> {

    public ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction> ControlFlowGraph { get; private set; }

    BasicBlock<Instruction> entryBlock;

    IDictionary<BasicBlock<Instruction>, S> postStatesByBlock = new Dictionary<BasicBlock<Instruction>, S>();

    ISet<BasicBlock<Instruction>> workList = new HashSet<BasicBlock<Instruction>>();

    public I StateInterpreter { get; private set; }

    A stateAbstraction;

    IDictionary<BasicBlock<Instruction>, ISet<BasicBlock<Instruction>>> predecessorsByBlock = new Dictionary<BasicBlock<Instruction>, ISet<BasicBlock<Instruction>>>();

   
    public WorkListAlgorithm(I stateInterpreter) {
      this.StateInterpreter = stateInterpreter;
      this.stateAbstraction = stateInterpreter.StateAbstraction;
    }

    public void RunOnMethod(IMethodDefinition methodDefinition, IMetadataHost host) {
      ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction> cfg = 
          ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction>.GetControlAndDataFlowGraphFor(host, methodDefinition.Body);

      // Note: we assume the first root block is the entrypoint of the function (and not, say, an exception handler
      // This may not be warranted; should perhaps check IL offset?

      BasicBlock<Instruction> entryBlock = cfg.RootBlocks.First();

      RunOnControlFlowGraph(cfg, entryBlock);

    }

    public void RunOnControlFlowGraph(ControlAndDataFlowGraph<BasicBlock<Instruction>, Instruction> cfg, BasicBlock<Instruction> entryBlock) {



      stateAbstraction = StateInterpreter.StateAbstraction;

      ControlFlowGraph = cfg;

      // factor this out into a static method
      // CFG only gives us successors, but we want predecessors too. So we stash those in a dictionary
      foreach (BasicBlock<Instruction> block in cfg.AllBlocks) {
        foreach (BasicBlock<Instruction> successor in cfg.SuccessorsFor(block)) {
          ISet<BasicBlock<Instruction>> predecessorsOfSuccessor;

          if (!predecessorsByBlock.TryGetValue(successor, out predecessorsOfSuccessor)) {
            predecessorsOfSuccessor = new HashSet<BasicBlock<Instruction>>();
            predecessorsByBlock[successor] = predecessorsOfSuccessor;
          }

          predecessorsOfSuccessor.Add(block);
        }
      }

      this.entryBlock = entryBlock;

      // This needn't actually be true.
      //Contract.Assert(PredecessorsOfBlock(entryBlock).Count() == 0);
      // We still might want to assert something like: "the entry block dominates all blocks reachable from it"?

      Run();
    }



    public void Run() {
      workList.Add(entryBlock);

      while (workList.Count() > 0) {
        BasicBlock<Instruction> block = workList.First();
        workList.Remove(block);

        ProcessBlock(block);
      }
    }

    private IMethodDefinition AnalyzedMethod() {
      return ControlFlowGraph.MethodBody.MethodDefinition;
    }

    private void PrintBlockState(BasicBlock<Instruction> block) {
      Console.WriteLine("Block {0} has postState {1}", block.GetHashCode(), GetBlockPostState(block));

      if (block == entryBlock) {
        Console.WriteLine("\tentry block");
      }

      foreach (BasicBlock<Instruction> pred in PredecessorsOfBlock(block)) {
        Console.WriteLine("\tpred: {0} has state {1}", pred.GetHashCode(), GetBlockPostState(pred));
      }
    }

    private void ProcessBlock(BasicBlock<Instruction> block) {

      // PrintBlockState(block);

      if (ControlFlowGraph.MethodBody.MethodDefinition.ToString().Contains("Microsoft.Cci.IModule Microsoft.Cci.MutableCodeModel.MetadataRewriter.Rewrite(Microsoft.Cci.IModule)")) {

      }

      S preState = CalculatePreStateForBlock(block);

      S newPostState = StateInterpreter.IntepretBlockInState(block, preState, AnalyzedMethod());

      S oldPostState = GetBlockPostState(block);

      //Console.WriteLine("Processing block {0} preState is {1} new post state {2} old post state {3}", block.GetHashCode(), preState, newPostState, oldPostState);

      // Iteration should only go up the lattice
      Contract.Assert(stateAbstraction.LessThanOrEqual(oldPostState, newPostState));

      if (!stateAbstraction.LessThanOrEqual(newPostState, oldPostState)) {
        // new state > old state

        SetBlockPostState(block, newPostState);

        // We haven't stablized yet, so add our successors to the work list

        foreach (BasicBlock<Instruction> successor in ControlFlowGraph.SuccessorsFor(block)) {
          workList.Add(successor);
        }
      }

    }

    private S CalculatePreStateForBlock(BasicBlock<Instruction> block) {

      if (block != entryBlock) {
        // Join the post states of all the predecessors

        S joinedState = stateAbstraction.Bottom;

        

        foreach (BasicBlock<Instruction> predecessor in PredecessorsOfBlock(block)) {
          S predecessorPostState = GetBlockPostState(predecessor);

          joinedState = stateAbstraction.Join(joinedState, predecessorPostState);

        }

        return joinedState;
      }
      else {
        // entry block gets entry pre state
        return StateInterpreter.EntryPreState(AnalyzedMethod());
      }

    }

    private ISet<BasicBlock<Instruction>> PredecessorsOfBlock(BasicBlock<Instruction> block) {
      ISet<BasicBlock<Instruction>> predecessors;

      if (predecessorsByBlock.TryGetValue(block, out predecessors)) {
        return predecessors;
      }
      else {
        return new HashSet<BasicBlock<Instruction>>();
      }
    }

    public S GetBlockPostState(BasicBlock<Instruction> block) {
      S postState;

      if (postStatesByBlock.TryGetValue(block, out postState)) {
        return postState;
      }
      else {
        return stateAbstraction.Bottom;
      }
    }

    private void SetBlockPostState(BasicBlock<Instruction> block, S postState) {

      //Console.WriteLine("Setting postState for {0} to {1}", block.GetHashCode(), postState);

      postStatesByBlock[block] = postState;
    }

    public S GetBlockPreState(BasicBlock<Instruction> block) {
      return CalculatePreStateForBlock(block);
    }

  }
#endregion


  #region Example Implementations

  #region 1-bit Reachability Examples

  internal class ReachabilityStateAbstraction : IAbstraction<bool> {
    public bool LessThanOrEqual(bool lhs, bool rhs) {
      if (lhs) {
        // true <= rhs?
        if (rhs) {
          // true <= true? YES
          return true;
        }
        else {
          // true <= false? NO
          return false;
        }
      }
      else {
        // false <= rhs? Always
        return true;
      }
    }

    public bool Join(bool lhs, bool rhs) {
      return lhs || rhs;
    }

    public bool Bottom {
      get {
        return false;
      }
    }

    public bool Top {
      get {
        return true;
      }
    }
  }

  internal class ReachabilityStateInterpreter : StateInterpreter<ReachabilityStateAbstraction, bool> {

    public ReachabilityStateInterpreter(ReachabilityStateAbstraction stateAbstraction) : base(stateAbstraction) {

    }

    public override bool IntepretBlockInState(BasicBlock<Instruction> block, bool preState, IMethodDefinition method) {
      return preState;
    }

    public override bool EntryPreState(IMethodDefinition method) {
      // The entry is reachable
      return true;
    }
  }

  #endregion  

  #region Value Abstraction Examples
  // Just a test class; this abstraction maps everything
  // to Top. We use this to to run through the basic block interpreters
  // to make sure they don't violate operand stack invariants.
  class OnlyTopValueAbstraction : IValueAbstraction<string> {
    const string top = "top";

    public string Bottom {
      get {
        return null;
      }
    }

    public string Top {
      get {
        return top;
      }
    }

    public string Join(string lhs, string rhs) {
      if (lhs == Bottom) {
        return rhs;
      }
      else if (rhs == Bottom) {
        return lhs;
      }

      //if we've gotten here, neither lhs nor rhs are bottom
      return Top;
    }

    public bool LessThanOrEqual(string lhs, string rhs) {
      if (lhs == Bottom) {
        return true;
      }
      else if (rhs == Bottom) {
        return false;
      }

      return true;
    }

    public string GetAbstractValueForType(ITypeDefinition type) {
      return Top;
    }
  }


  class TypesValueAbstraction : IValueAbstraction<ITypeDefinition> {

    private readonly IMetadataHost host;

    public TypesValueAbstraction(IMetadataHost host) {
      this.host = host;
    }

    public ITypeDefinition Bottom {
      get {
        return null;
      }
    }

    public ITypeDefinition Top {
      get {
        return host.PlatformType.SystemObject.ResolvedType;      
      }
    }

    public bool LessThanOrEqual(ITypeDefinition lhs, ITypeDefinition rhs) {
      if (lhs == Bottom) {
        return true;
      }
      else if (rhs == Bottom) {
        return false;
      }

      return TypeHelper.Type1DerivesFromOrIsTheSameAsType2(lhs, rhs)
        || (rhs.IsInterface && TypeHelper.Type1ImplementsType2(lhs, rhs))
        || (lhs.IsInterface && rhs == host.PlatformType.SystemObject.ResolvedType); /* Interfaces don't explicitly derive from System.Object, but we treat them as if they do */
    }

    public ITypeDefinition Join(ITypeDefinition lhs, ITypeDefinition rhs) {
      if (lhs == Bottom) {
        return rhs;
      }
      else if (rhs == Bottom) {
        return lhs;
      }

      if (lhs == Top) {
        return Top;
      }
      else if (rhs == Top) {
        return Top;
      }

      return TypeHelper.MergedType(lhs, rhs).ResolvedType; // Returns System.Object if can't merge
    }

    public ITypeDefinition GetAbstractValueForType(ITypeDefinition type) {
      if (type.IsReferenceType || type.IsStruct) {
        return type;
      }
      else {
        return Top;
      }
    }
  }

  #endregion

  #region LocalsAndOperandStackStateInterpreter Examples

  /// <summary>
  /// A state interpreter that will record the abstract value of the receiver of all callvirt instructions.
  /// </summary>
  /// <typeparam name="A"></typeparam>
  /// <typeparam name="V"></typeparam>
  internal class VirtualCallRecordingStateInterpreter<A, V> : LocalsAndOperandStackStateInterpreter<A, V> where A : IValueAbstraction<V> {

    private readonly IDictionary<IOperation, LocalsAndOperands<V>> preStatesByOperation = new Dictionary<IOperation, LocalsAndOperands<V>>();

    public IEnumerable<IOperation> VirtualCallOperations { get { return preStatesByOperation.Keys; } }

    public VirtualCallRecordingStateInterpreter(A stateAbstraction)
      : base(stateAbstraction) {

    }

    protected override void NotePreStateBeforeInterpretingInstruction(LocalsAndOperands<V> preState, Instruction instruction) {
      if (instruction.Operation.OperationCode == OperationCode.Callvirt) {

        // This copy is CRUCIAL: the preState is mutable and the analysis
        // WILL change it behind our back.

        LocalsAndOperands<V> preStateCopy = preState.Copy();

        preStatesByOperation[instruction.Operation] = preStateCopy;
      }
    }

    public V ReceiverValueForVirtualCallInstruction(IOperation operation) {
      Contract.Requires(operation.Value is IMethodReference);
      IMethodReference methodCalled = operation.Value as IMethodReference;

      return FindReceiverInPreStateOperandStack(preStatesByOperation[operation], methodCalled);
    }

    private V FindReceiverInPreStateOperandStack(LocalsAndOperands<V> preState, IMethodReference methodCalled) {
      
      int countOfParameters = methodCalled.Parameters.Count();

      //Operand stack in preState is ..., receiver, param1, param2, param3.

      int indexOfReceiverInOperandStack = (preState.OperandStack.Count() - countOfParameters) - 1;

      return preState.OperandStack.ElementAt(indexOfReceiverInOperandStack);
    }
      
    
  }

  #endregion

  #endregion

}
