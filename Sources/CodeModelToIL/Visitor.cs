//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.Cci.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// An object with a method that converts a given block of statements to a list of IL operations, exception information and possibly some private
  /// helper types.
  /// </summary>
  public class CodeModelToILConverter : BaseCodeAndContractTraverser, ISourceToILConverter {

    /// <summary>
    /// Initializes an object with a method that converts a given block of statements to a list of IL operations, exception information and possibly some private 
    /// helper types.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in the block of statements to IPrimarySourceLocation objects.  May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public CodeModelToILConverter(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider)
      : base(contractProvider) {
      this.generator = new ILGenerator(host);
      this.host = host;
      this.sourceLocationProvider = sourceLocationProvider;
      this.minizeCodeSize = true;
    }

    ILGeneratorLabel currentBreakTarget = new ILGeneratorLabel();
    ILGeneratorLabel currentContinueTarget = new ILGeneratorLabel();
    ILGeneratorLabel/*?*/ currentTryCatchFinallyEnd;
    ITryCatchFinallyStatement/*?*/ currentTryCatch;
    bool minizeCodeSize;
    ILGeneratorLabel endOfMethod = new ILGeneratorLabel();
    ILGenerator generator;
    /// <summary>
    /// An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.
    /// </summary>
    protected IMetadataHost host;
    Dictionary<int, ILGeneratorLabel> labelFor = new Dictionary<int, ILGeneratorLabel>();
    bool lastStatementWasUnconditionalTransfer;
    Dictionary<ILocalDefinition, ushort> localIndex = new Dictionary<ILocalDefinition, ushort>();
    IMethodDefinition method = Dummy.Method;
    Dictionary<object, ITryCatchFinallyStatement> mostNestedTryCatchFor = new Dictionary<object, ITryCatchFinallyStatement>();
    ILocalDefinition/*?*/ returnLocal;
    /// <summary>
    /// An object that can map the ILocation objects found in the block of statements to IPrimarySourceLocation objects.
    /// </summary>
    protected ISourceLocationProvider/*?*/ sourceLocationProvider;
    List<ILocalDefinition> temporaries = new List<ILocalDefinition>();

    private static ushort GetParameterIndex(IParameterDefinition parameterDefinition) {
      ushort parameterIndex = parameterDefinition.Index;
      if ((parameterDefinition.ContainingSignature.CallingConvention & CallingConvention.HasThis) != 0)
        parameterIndex++;
      return parameterIndex;
    }

    private void LoadAddressOf(object container, IExpression/*?*/ instance) {
      this.LoadAddressOf(container, instance, false);
    }

    private void LoadAddressOf(object container, IExpression/*?*/ instance, bool emitReadonlyPrefix) {
      this.StackSize++;
      ILocalDefinition/*?*/ local = container as ILocalDefinition;
      if (local != null) {
        ushort localIndex = this.GetLocalIndex(local);
        if (localIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Ldloca_S, local);
        else this.generator.Emit(OperationCode.Ldloca, local);
        return;
      }
      IParameterDefinition/*?*/ parameter = container as IParameterDefinition;
      if (parameter != null) {
        ushort parIndex = GetParameterIndex(parameter);
        if (parIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Ldarga_S, parameter);
        else this.generator.Emit(OperationCode.Ldarga, parameter);
        return;
      }
      IFieldReference/*?*/ field = container as IFieldReference;
      if (field != null) {
        if (instance == null)
          this.generator.Emit(OperationCode.Ldsflda, field);
        else {
          this.Visit(instance);
          this.generator.Emit(OperationCode.Ldflda, field);
        }
        return;
      }
      IArrayIndexer/*?*/ arrayIndexer = container as IArrayIndexer;
      if (arrayIndexer != null) {
        this.Visit(arrayIndexer.IndexedObject);
        this.Visit(arrayIndexer.Indices);
        if (emitReadonlyPrefix)
          this.generator.Emit(OperationCode.Readonly_);
        IArrayTypeReference arrayType = (IArrayTypeReference)arrayIndexer.IndexedObject.Type;
        if (arrayType.IsVector)
          this.generator.Emit(OperationCode.Ldelema, arrayType.ElementType);
        else
          this.generator.Emit(OperationCode.Array_Addr, arrayType);
        return;
      }
      IAddressDereference/*?*/ addressDereference = container as IAddressDereference;
      if (addressDereference != null) {
        this.Visit(addressDereference.Address);
        return;
      }
      IMethodReference/*?*/ method = container as IMethodReference;
      if (method != null) {
        if (instance != null)
          this.Visit(instance);
        if (method.ResolvedMethod.IsVirtual) //TODO: need a way to do this without resolving the method
          this.generator.Emit(OperationCode.Ldvirtftn, method);
        else
          this.generator.Emit(OperationCode.Ldftn, method);
        return;
      }
      IThisReference/*?*/ thisParameter = container as IThisReference;
      if (thisParameter != null) {
        this.generator.Emit(OperationCode.Ldarg_0);
        return;
      }
      IAddressableExpression/*?*/ addressableExpression = container as IAddressableExpression;
      if (addressableExpression != null) {
        this.LoadAddressOf(addressableExpression.Definition, addressableExpression.Instance);
        return;
      }
      ITargetExpression/*?*/ targetExpression = container as ITargetExpression;
      if (targetExpression != null) {
        this.LoadAddressOf(targetExpression.Definition, targetExpression.Instance);
        return;
      }
      IExpression/*?*/ expression = container as IExpression;
      if (expression != null) {
        TemporaryVariable temp = new TemporaryVariable(expression.Type);
        this.Visit(expression);
        this.VisitAssignmentTo(temp);
        this.LoadAddressOf(temp, null);
        return;
      }
      Debug.Assert(false);
    }

    private void LoadField(byte alignment, bool isVolatile, IExpression/*?*/ instance, IFieldReference field) {
      if (alignment != 0)
        this.generator.Emit(OperationCode.Unaligned_, alignment);
      if (isVolatile)
        this.generator.Emit(OperationCode.Volatile_);
      if (instance == null) {
        this.generator.Emit(OperationCode.Ldsfld, field);
        this.StackSize++;
      } else {
        if (instance == this.expressionOnTopOfStack) {
          this.generator.Emit(OperationCode.Dup);
          this.expressionOnTopOfStack = null;
        } else
          this.Visit(instance);
        this.generator.Emit(OperationCode.Ldfld, field);
      }
    }

    IExpression/*?*/ expressionOnTopOfStack;

    private void LoadLocal(ILocalDefinition local) {
      ushort localIndex = this.GetLocalIndex(local);
      if (localIndex == 0) this.generator.Emit(OperationCode.Ldloc_0, local);
      else if (localIndex == 1) this.generator.Emit(OperationCode.Ldloc_1, local);
      else if (localIndex == 2) this.generator.Emit(OperationCode.Ldloc_2, local);
      else if (localIndex == 3) this.generator.Emit(OperationCode.Ldloc_3, local);
      else if (localIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Ldloc_S, local);
      else this.generator.Emit(OperationCode.Ldloc, local);
      this.StackSize++;
    }

    private void LoadParameter(IParameterDefinition parameter) {
      ushort parIndex = GetParameterIndex(parameter);
      if (parIndex == 0) this.generator.Emit(OperationCode.Ldarg_0, parameter);
      else if (parIndex == 1) this.generator.Emit(OperationCode.Ldarg_1, parameter);
      else if (parIndex == 2) this.generator.Emit(OperationCode.Ldarg_2, parameter);
      else if (parIndex == 3) this.generator.Emit(OperationCode.Ldarg_3, parameter);
      else if (parIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Ldarg_S, parameter);
      else this.generator.Emit(OperationCode.Ldarg, parameter);
      this.StackSize++;
    }

    /// <summary>
    /// The maximum number of stack slots that will be needed by an interpreter of the IL produced by this converter.
    /// </summary>
    public ushort MaximumStackSizeNeeded {
      get { return this.maximumStackSizeNeeded; }
    }
    ushort maximumStackSizeNeeded;

    ushort StackSize {
      get { return this._stackSize; }
      set {
        this._stackSize = value; if (value > this.maximumStackSizeNeeded) maximumStackSizeNeeded = value;
        if (value == ushort.MaxValue) { }
      }
    }
    ushort _stackSize;

    /// <summary>
    /// Visits the specified addition.
    /// </summary>
    /// <param name="addition">The addition.</param>
    public override void Visit(IAddition addition) {
      this.Visit(addition.LeftOperand);
      this.Visit(addition.RightOperand);
      OperationCode operationCode = OperationCode.Add;
      if (addition.CheckOverflow) {
        if (TypeHelper.IsSignedPrimitiveInteger(addition.Type))
          operationCode = OperationCode.Add_Ovf;
        else if (TypeHelper.IsUnsignedPrimitiveInteger(addition.Type))
          operationCode = OperationCode.Add_Ovf_Un;
      }
      this.generator.Emit(operationCode);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    public override void Visit(IAddressableExpression addressableExpression) {
      Debug.Assert(false); //The expression containing this as a subexpression should never allow a call to this routine.
    }

    /// <summary>
    /// Visits the specified address dereference.
    /// </summary>
    /// <param name="addressDereference">The address dereference.</param>
    public override void Visit(IAddressDereference addressDereference) {
      this.Visit(addressDereference.Address);
      if (addressDereference.IsUnaligned)
        this.generator.Emit(OperationCode.Unaligned_, addressDereference.Alignment);
      if (addressDereference.IsVolatile)
        this.generator.Emit(OperationCode.Volatile_);
      OperationCode opcode;
      switch (addressDereference.Type.TypeCode) {
        case PrimitiveTypeCode.Boolean: opcode = OperationCode.Ldind_U1; break;
        case PrimitiveTypeCode.Char: opcode = OperationCode.Ldind_U2; break;
        case PrimitiveTypeCode.Float32: opcode = OperationCode.Ldind_R4; break;
        case PrimitiveTypeCode.Float64: opcode = OperationCode.Ldind_R8; break;
        case PrimitiveTypeCode.Int16: opcode = OperationCode.Ldind_I2; break;
        case PrimitiveTypeCode.Int32: opcode = OperationCode.Ldind_I4; break;
        case PrimitiveTypeCode.Int64: opcode = OperationCode.Ldind_I8; break;
        case PrimitiveTypeCode.Int8: opcode = OperationCode.Ldind_I1; break;
        case PrimitiveTypeCode.IntPtr: opcode = OperationCode.Ldind_I; break;
        case PrimitiveTypeCode.Pointer: opcode = OperationCode.Ldind_I; break;
        case PrimitiveTypeCode.UInt16: opcode = OperationCode.Ldind_U2; break;
        case PrimitiveTypeCode.UInt32: opcode = OperationCode.Ldind_U4; break;
        case PrimitiveTypeCode.UInt64: opcode = OperationCode.Ldind_I8; break;
        case PrimitiveTypeCode.UInt8: opcode = OperationCode.Ldind_U1; break;
        case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Ldind_I8; break;
        default:
          if (!TypeHelper.TypesAreEquivalent(addressDereference.Type, addressDereference.Type.PlatformType.SystemObject)) {
            this.generator.Emit(OperationCode.Ldobj, addressDereference.Type);
            return;
          }
          opcode = OperationCode.Ldind_Ref; break;
      }
      this.generator.Emit(opcode);
    }

    /// <summary>
    /// Visits the specified address of.
    /// </summary>
    /// <param name="addressOf">The address of.</param>
    public override void Visit(IAddressOf addressOf) {
      object/*?*/ container = addressOf.Expression.Definition;
      IExpression/*?*/ instance = addressOf.Expression.Instance;
      this.LoadAddressOf(container, instance, addressOf.ObjectControlsMutability);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified alias for type.
    /// </summary>
    /// <param name="aliasForType">Type of the alias for.</param>
    public override void Visit(IAliasForType aliasForType) {
      Debug.Assert(false);
    }

    /// <summary>
    /// Visits the specified anonymous delegate.
    /// </summary>
    /// <param name="anonymousDelegate">The anonymous delegate.</param>
    public override void Visit(IAnonymousDelegate anonymousDelegate) {
      Debug.Assert(false);
    }

    /// <summary>
    /// Visits the specified array indexer.
    /// </summary>
    /// <param name="arrayIndexer">The array indexer.</param>
    public override void Visit(IArrayIndexer arrayIndexer) {
      this.Visit(arrayIndexer.IndexedObject);
      this.Visit(arrayIndexer.Indices);
      IArrayTypeReference arrayType = (IArrayTypeReference)arrayIndexer.IndexedObject.Type;
      if (arrayType.IsVector)
        this.LoadVectorElement(arrayType.ElementType);
      else
        this.generator.Emit(OperationCode.Array_Get, arrayIndexer.IndexedObject.Type);
      this.StackSize -= (ushort)IteratorHelper.EnumerableCount(arrayIndexer.Indices);
    }

    private void LoadVectorElement(ITypeReference typeReference) {
      OperationCode opcode;
      switch (typeReference.TypeCode) {
        case PrimitiveTypeCode.Boolean: opcode = OperationCode.Ldelem_I1; break;
        case PrimitiveTypeCode.Char: opcode = OperationCode.Ldelem_I2; break;
        case PrimitiveTypeCode.Float32: opcode = OperationCode.Ldelem_R4; break;
        case PrimitiveTypeCode.Float64: opcode = OperationCode.Ldelem_R8; break;
        case PrimitiveTypeCode.Int16: opcode = OperationCode.Ldelem_I2; break;
        case PrimitiveTypeCode.Int32: opcode = OperationCode.Ldelem_I4; break;
        case PrimitiveTypeCode.Int64: opcode = OperationCode.Ldelem_I8; break;
        case PrimitiveTypeCode.Int8: opcode = OperationCode.Ldelem_I1; break;
        case PrimitiveTypeCode.IntPtr: opcode = OperationCode.Ldelem_I; break;
        case PrimitiveTypeCode.Pointer: opcode = OperationCode.Ldelem_I; break;
        case PrimitiveTypeCode.UInt16: opcode = OperationCode.Ldelem_I2; break;
        case PrimitiveTypeCode.UInt32: opcode = OperationCode.Ldelem_I4; break;
        case PrimitiveTypeCode.UInt64: opcode = OperationCode.Ldelem_I8; break;
        case PrimitiveTypeCode.UInt8: opcode = OperationCode.Ldelem_I1; break;
        case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Ldelem_I; break;
        default:
          if (typeReference.IsValueType || typeReference is IGenericTypeParameterReference) {
            this.generator.Emit(OperationCode.Ldelem, typeReference);
            return;
          }
          opcode = OperationCode.Ldelem_Ref; break;
      }
      this.generator.Emit(opcode);
    }

    /// <summary>
    /// Visits the specified array type reference.
    /// </summary>
    /// <param name="arrayTypeReference">The array type reference.</param>
    public override void Visit(IArrayTypeReference arrayTypeReference) {
      Debug.Assert(false);
    }

    /// <summary>
    /// Visits the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public override void Visit(IAssembly assembly) {
      base.Visit(assembly);
    }

    /// <summary>
    /// Visits the specified assembly reference.
    /// </summary>
    /// <param name="assemblyReference">The assembly reference.</param>
    public override void Visit(IAssemblyReference assemblyReference) {
      base.Visit(assemblyReference);
    }

    /// <summary>
    /// Visits the specified assert statement.
    /// </summary>
    /// <param name="assertStatement">The assert statement.</param>
    public override void Visit(IAssertStatement assertStatement) {
      if (this.contractProvider == null) return;
      this.Visit(assertStatement.Condition);
      this.generator.Emit(OperationCode.Call, this.contractProvider.ContractMethods.Assert);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    public override void Visit(IAssignment assignment) {
      this.VisitAssignment(assignment, false);
    }

    /// <summary>
    /// Visits the assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    /// <param name="treatAsStatement">if set to <c>true</c> [treat as statement].</param>
    public virtual void VisitAssignment(IAssignment assignment, bool treatAsStatement) {
      object/*?*/ container = assignment.Target.Definition;
      ILocalDefinition/*?*/ local = container as ILocalDefinition;
      if (local != null) {
        if (assignment.Source is IDefaultValue && !local.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(local, null);
          this.generator.Emit(OperationCode.Initobj, local.Type);
        } else {
          this.Visit(assignment.Source);
          this.VisitAssignmentTo(local);
        }
        if (!treatAsStatement) this.LoadLocal(local);
        return;
      }
      IParameterDefinition/*?*/ parameter = container as IParameterDefinition;
      if (parameter != null) {
        if (assignment.Source is IDefaultValue && !parameter.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(parameter, null);
          this.generator.Emit(OperationCode.Initobj, parameter.Type);
        } else {
          this.Visit(assignment.Source);
          ushort parIndex = GetParameterIndex(parameter);
          if (parIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Starg_S, parameter);
          else this.generator.Emit(OperationCode.Starg, parameter);
          this.StackSize--;
        }
        if (!treatAsStatement) this.LoadParameter(parameter);
        return;
      }
      IFieldReference/*?*/ field = container as IFieldReference;
      if (field != null) {
        if (assignment.Source is IDefaultValue && !field.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(field, assignment.Target.Instance);
          if (!treatAsStatement) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          this.generator.Emit(OperationCode.Initobj, field.Type);
          if (!treatAsStatement)
            this.generator.Emit(OperationCode.Ldobj, field.Type);
          else
            this.StackSize--;
        } else {
          if (assignment.Target.Instance != null) {
            this.Visit(assignment.Target.Instance);
            if (!treatAsStatement) {
              this.generator.Emit(OperationCode.Dup);
              this.StackSize++;
            }
            IBinaryOperation binop = assignment.Source as IBinaryOperation;
            if (binop != null) {
              IBoundExpression boundExpr = binop.LeftOperand as IBoundExpression;
              if (boundExpr != null && boundExpr.Instance == assignment.Target.Instance)
                this.expressionOnTopOfStack = boundExpr.Instance;
            }
          }
          this.Visit(assignment.Source);
          if (assignment.Target.IsUnaligned)
            this.generator.Emit(OperationCode.Unaligned_, assignment.Target.Alignment);
          if (assignment.Target.IsVolatile)
            this.generator.Emit(OperationCode.Volatile_);
          if (assignment.Target.Instance == null) {
            this.generator.Emit(OperationCode.Stsfld, field);
            this.StackSize--;
          } else {
            this.generator.Emit(OperationCode.Stfld, field);
            this.StackSize-=2;
          }
          if (!treatAsStatement)
            this.LoadField(assignment.Target.IsUnaligned ? assignment.Target.Alignment : (byte)0, assignment.Target.IsVolatile, null, field);
        }
        return;
      }
      IArrayIndexer/*?*/ arrayIndexer = container as IArrayIndexer;
      if (arrayIndexer != null) {
        if (assignment.Source is IDefaultValue && !arrayIndexer.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(arrayIndexer, assignment.Target.Instance);
          if (!treatAsStatement) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          this.generator.Emit(OperationCode.Initobj, arrayIndexer.Type);
          if (!treatAsStatement)
            this.generator.Emit(OperationCode.Ldobj, arrayIndexer.Type);
          else
            this.StackSize--;
        } else {
          this.Visit(assignment.Target.Instance);
          this.Visit(arrayIndexer.Indices);
          this.Visit(assignment.Source);
          ILocalDefinition/*?*/ temp = null;
          if (!treatAsStatement) {
            temp = new TemporaryVariable(assignment.Source.Type);
            this.VisitAssignmentTo(temp);
            this.LoadLocal(temp);
          }
          IArrayTypeReference arrayType = (IArrayTypeReference)assignment.Target.Instance.Type;
          if (arrayType.IsVector)
            this.StoreVectorElement(arrayType.ElementType);
          else
            this.generator.Emit(OperationCode.Array_Set, arrayType);
          this.StackSize-=(ushort)(IteratorHelper.EnumerableCount(arrayIndexer.Indices)+2);
          if (temp != null) this.LoadLocal(temp);
        }
        return;
      }
      IAddressDereference/*?*/ addressDereference = container as IAddressDereference;
      if (addressDereference != null) {
        this.Visit(addressDereference.Address);
        if (assignment.Source is IDefaultValue && !addressDereference.Type.ResolvedType.IsReferenceType) {
          if (!treatAsStatement) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          this.generator.Emit(OperationCode.Initobj, addressDereference.Type);
          if (!treatAsStatement)
            this.generator.Emit(OperationCode.Ldobj, addressDereference.Type);
          else
            this.StackSize--;
        } else if (assignment.Source is IAddressDereference) {
          if (!treatAsStatement) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          this.Visit(((IAddressDereference)assignment.Source).Address);
          this.generator.Emit(OperationCode.Cpobj, addressDereference.Type);
          this.StackSize-=2;
          if (!treatAsStatement)
            this.generator.Emit(OperationCode.Ldobj, addressDereference.Type);
        } else {
          this.Visit(assignment.Source);
          ILocalDefinition/*?*/ temp = null;
          if (!treatAsStatement) {
            temp = new TemporaryVariable(assignment.Source.Type);
            this.VisitAssignmentTo(temp);
            this.LoadLocal(temp);
          }
          this.VisitAssignmentTo(addressDereference);
          if (temp != null) this.LoadLocal(temp);
        }
        return;
      }
      Debug.Assert(false);
    }

    private void StoreVectorElement(ITypeReference elementTypeReference) {
      OperationCode opcode;
      switch (elementTypeReference.TypeCode) {
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
        case PrimitiveTypeCode.UInt16: opcode = OperationCode.Stelem_I2; break;
        case PrimitiveTypeCode.UInt32: opcode = OperationCode.Stelem_I4; break;
        case PrimitiveTypeCode.UInt64: opcode = OperationCode.Stelem_I8; break;
        case PrimitiveTypeCode.UInt8: opcode = OperationCode.Stelem_I1; break;
        case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Stelem_I; break;
        default:
          if (elementTypeReference.IsValueType || elementTypeReference is IGenericTypeParameterReference) {
            this.generator.Emit(OperationCode.Stelem, elementTypeReference);
            return;
          }
          opcode = OperationCode.Stelem_Ref; break;
      }
      this.generator.Emit(opcode);
    }

    private void VisitAssignmentTo(IAddressDereference addressDereference) {
      if (addressDereference.IsUnaligned)
        this.generator.Emit(OperationCode.Unaligned_, addressDereference.Alignment);
      if (addressDereference.IsVolatile)
        this.generator.Emit(OperationCode.Volatile_);
      OperationCode opcode;
      switch (addressDereference.Type.TypeCode) {
        case PrimitiveTypeCode.Boolean: opcode = OperationCode.Stind_I1; break;
        case PrimitiveTypeCode.Char: opcode = OperationCode.Stind_I2; break;
        case PrimitiveTypeCode.Float32: opcode = OperationCode.Stind_R4; break;
        case PrimitiveTypeCode.Float64: opcode = OperationCode.Stind_R8; break;
        case PrimitiveTypeCode.Int16: opcode = OperationCode.Stind_I2; break;
        case PrimitiveTypeCode.Int32: opcode = OperationCode.Stind_I4; break;
        case PrimitiveTypeCode.Int64: opcode = OperationCode.Stind_I8; break;
        case PrimitiveTypeCode.Int8: opcode = OperationCode.Stind_I1; break;
        case PrimitiveTypeCode.IntPtr: opcode = OperationCode.Stind_I; break;
        case PrimitiveTypeCode.Pointer: opcode = OperationCode.Stind_I; break;
        case PrimitiveTypeCode.UInt16: opcode = OperationCode.Stind_I2; break;
        case PrimitiveTypeCode.UInt32: opcode = OperationCode.Stind_I4; break;
        case PrimitiveTypeCode.UInt64: opcode = OperationCode.Stind_I8; break;
        case PrimitiveTypeCode.UInt8: opcode = OperationCode.Stind_I1; break;
        case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Stind_I; break;
        default:
          if (addressDereference.Type.IsValueType || addressDereference.Type is IGenericTypeParameterReference) {
            this.generator.Emit(OperationCode.Stobj, addressDereference.Type);
            this.StackSize-=2;
            return;
          }
          opcode = OperationCode.Stind_Ref; break;
      }
      this.generator.Emit(opcode);
      this.StackSize-=2;
    }

    private void VisitAssignmentTo(ILocalDefinition local) {
      ushort localIndex = this.GetLocalIndex(local);
      if (localIndex == 0) this.generator.Emit(OperationCode.Stloc_0, local);
      else if (localIndex == 1) this.generator.Emit(OperationCode.Stloc_1, local);
      else if (localIndex == 2) this.generator.Emit(OperationCode.Stloc_2, local);
      else if (localIndex == 3) this.generator.Emit(OperationCode.Stloc_3, local);
      else if (localIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Stloc_S, local);
      else this.generator.Emit(OperationCode.Stloc, local);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified assume statement.
    /// </summary>
    /// <param name="assumeStatement">The assume statement.</param>
    public override void Visit(IAssumeStatement assumeStatement) {
      if (this.contractProvider == null) return;
      this.Visit(assumeStatement.Condition);
      this.generator.Emit(OperationCode.Call, this.contractProvider.ContractMethods.Assume);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified base class reference.
    /// </summary>
    /// <param name="baseClassReference">The base class reference.</param>
    public override void Visit(IBaseClassReference baseClassReference) {
      this.generator.Emit(OperationCode.Ldarg_0);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified bitwise and.
    /// </summary>
    /// <param name="bitwiseAnd">The bitwise and.</param>
    public override void Visit(IBitwiseAnd bitwiseAnd) {
      this.Visit(bitwiseAnd.LeftOperand);
      this.Visit(bitwiseAnd.RightOperand);
      this.generator.Emit(OperationCode.And);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified bitwise or.
    /// </summary>
    /// <param name="bitwiseOr">The bitwise or.</param>
    public override void Visit(IBitwiseOr bitwiseOr) {
      this.Visit(bitwiseOr.LeftOperand);
      this.Visit(bitwiseOr.RightOperand);
      this.generator.Emit(OperationCode.Or);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified block expression.
    /// </summary>
    /// <param name="blockExpression">The block expression.</param>
    public override void Visit(IBlockExpression blockExpression) {
      this.Visit(blockExpression.BlockStatement);
      this.Visit(blockExpression.Expression);
    }

    /// <summary>
    /// Visits the specified block.
    /// </summary>
    /// <param name="block">The block.</param>
    public override void Visit(IBlockStatement block) {
      this.generator.BeginScope();
      this.Visit(block.Statements);
      this.generator.EndScope();
    }

    /// <summary>
    /// Performs some computation with the given bound expression.
    /// </summary>
    /// <param name="boundExpression"></param>
    public override void Visit(IBoundExpression boundExpression) {
      object/*?*/ container = boundExpression.Definition;
      ILocalDefinition/*?*/ local = container as ILocalDefinition;
      if (local != null) {
        this.LoadLocal(local);
        return;
      }
      IParameterDefinition/*?*/ parameter = container as IParameterDefinition;
      if (parameter != null) {
        this.LoadParameter(parameter);
        return;
      }
      IFieldReference/*?*/ field = container as IFieldReference;
      if (field != null) {
        this.LoadField(boundExpression.IsUnaligned ? boundExpression.Alignment : (byte)0, boundExpression.IsVolatile, boundExpression.Instance, field);
        return;
      }
      Debug.Assert(false);
    }

    /// <summary>
    /// Visits the specified break statement.
    /// </summary>
    /// <param name="breakStatement">The break statement.</param>
    public override void Visit(IBreakStatement breakStatement) {
      if (this.LabelIsOutsideCurrentExceptionBlock(this.currentBreakTarget))
        this.generator.Emit(OperationCode.Leave, this.currentBreakTarget);
      else
        this.generator.Emit(OperationCode.Br, this.currentBreakTarget);
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Visits the specified cast if possible.
    /// </summary>
    /// <param name="castIfPossible">The cast if possible.</param>
    public override void Visit(ICastIfPossible castIfPossible) {
      this.Visit(castIfPossible.ValueToCast);
      this.generator.Emit(OperationCode.Isinst, castIfPossible.TargetType);
    }

    /// <summary>
    /// Visits the specified catch clause.
    /// </summary>
    /// <param name="catchClause">The catch clause.</param>
    public override void Visit(ICatchClause catchClause) {
      this.lastStatementWasUnconditionalTransfer = false;
      if (catchClause.FilterCondition != null) {
        this.generator.BeginFilterBlock();
        this.Visit(catchClause.FilterCondition);
        this.generator.BeginFilterBody();
      } else {
        this.generator.BeginCatchBlock(catchClause.ExceptionType);
      }
      this.StackSize++;
      this.VisitAssignmentTo(catchClause.ExceptionContainer);
      base.Visit(catchClause);
      if (!this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Leave, this.currentTryCatchFinallyEnd);
    }

    /// <summary>
    /// Visits the specified check if instance.
    /// </summary>
    /// <param name="checkIfInstance">The check if instance.</param>
    public override void Visit(ICheckIfInstance checkIfInstance) {
      ILGeneratorLabel falseCase = new ILGeneratorLabel();
      ILGeneratorLabel endif = new ILGeneratorLabel();
      this.Visit(checkIfInstance.Operand);
      this.generator.Emit(OperationCode.Isinst, checkIfInstance.TypeToCheck);
      this.generator.Emit(OperationCode.Brfalse_S, falseCase);
      this.generator.Emit(OperationCode.Ldc_I4_1);
      this.generator.Emit(OperationCode.Br_S, endif);
      this.generator.MarkLabel(falseCase);
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.generator.MarkLabel(endif);
    }

    /// <summary>
    /// Visits the specified constant.
    /// </summary>
    /// <param name="constant">The constant.</param>
    public override void Visit(ICompileTimeConstant constant) {
      this.EmitConstant(constant.Value as IConvertible);
    }

    /// <summary>
    /// Visits the specified conditional.
    /// </summary>
    /// <param name="conditional">The conditional.</param>
    public override void Visit(IConditional conditional) {
      ILGeneratorLabel falseCase = new ILGeneratorLabel();
      ILGeneratorLabel endif = new ILGeneratorLabel();
      this.VisitBranchIfFalse(conditional.Condition, falseCase);
      this.Visit(conditional.ResultIfTrue);
      this.generator.Emit(OperationCode.Br, endif);
      this.generator.MarkLabel(falseCase);
      this.StackSize--;
      this.Visit(conditional.ResultIfFalse);
      this.generator.MarkLabel(endif);
    }

    /// <summary>
    /// Visits the specified conditional statement.
    /// </summary>
    /// <param name="conditionalStatement">The conditional statement.</param>
    public override void Visit(IConditionalStatement conditionalStatement) {
      ILGeneratorLabel/*?*/ endif = null;
      if (conditionalStatement.TrueBranch is IBreakStatement && !this.LabelIsOutsideCurrentExceptionBlock(this.currentBreakTarget))
        this.VisitBranchIfTrue(conditionalStatement.Condition, this.currentBreakTarget);
      else if (conditionalStatement.TrueBranch is IContinueStatement &&  !this.LabelIsOutsideCurrentExceptionBlock(this.currentContinueTarget))
        this.VisitBranchIfTrue(conditionalStatement.Condition, this.currentContinueTarget);
      else {
        ILGeneratorLabel falseCase = new ILGeneratorLabel();
        this.VisitBranchIfFalse(conditionalStatement.Condition, falseCase);
        this.Visit(conditionalStatement.TrueBranch);
        if (!this.lastStatementWasUnconditionalTransfer) {
          endif = new ILGeneratorLabel();
          this.generator.Emit(OperationCode.Br, endif);
        } else {
        }
        this.generator.MarkLabel(falseCase);
      }
      this.Visit(conditionalStatement.FalseBranch);
      if (endif != null)
        this.generator.MarkLabel(endif);
    }

    internal bool LabelIsOutsideCurrentExceptionBlock(ILGeneratorLabel label) {
      ITryCatchFinallyStatement tryCatchContainingTarget = null;
      this.mostNestedTryCatchFor.TryGetValue(label, out tryCatchContainingTarget);
      return this.currentTryCatch != tryCatchContainingTarget;
    }

    /// <summary>
    /// Visits the specified continue statement.
    /// </summary>
    /// <param name="continueStatement">The continue statement.</param>
    public override void Visit(IContinueStatement continueStatement) {
      if (this.LabelIsOutsideCurrentExceptionBlock(this.currentContinueTarget))
        this.generator.Emit(OperationCode.Leave, this.currentContinueTarget);
      else
        this.generator.Emit(OperationCode.Br, this.currentContinueTarget);
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Visits the specified conversion.
    /// </summary>
    /// <param name="conversion">The conversion.</param>
    public override void Visit(IConversion conversion) {
      this.Visit(conversion.ValueToConvert);
      //TODO: change IConversion to make it illegal to convert to or from enum types.
      ITypeReference sourceType = conversion.ValueToConvert.Type;
      if (sourceType.ResolvedType.IsEnum) sourceType = sourceType.ResolvedType.UnderlyingType;
      ITypeReference targetType = conversion.Type;
      if (targetType.ResolvedType.IsEnum) targetType = targetType.ResolvedType.UnderlyingType;
      if (conversion.CheckNumericRange)
        this.VisitCheckedConversion(sourceType, targetType);
      else
        this.VisitUncheckedConversion(sourceType, targetType);
    }

    /// <summary>
    /// Visits the specified create array.
    /// </summary>
    /// <param name="createArray">The create array.</param>
    public override void Visit(ICreateArray createArray) {
      foreach (IExpression size in createArray.Sizes) {
        this.Visit(size);
        if (size.Type.TypeCode == PrimitiveTypeCode.Int64 || size.Type.TypeCode == PrimitiveTypeCode.UInt64)
          this.generator.Emit(OperationCode.Conv_Ovf_U);
      }
      uint numLowerBounds = 0;
      foreach (int lowerBound in createArray.LowerBounds) {
        this.EmitConstant(lowerBound);
        numLowerBounds++;
      }
      if (numLowerBounds > 0) {
        while (numLowerBounds < createArray.Rank) {
          this.generator.Emit(OperationCode.Ldc_I4_0);
          numLowerBounds++;
        }
      }
      IArrayTypeReference arrayType;
      OperationCode create;
      if (numLowerBounds > 0) {
        create = OperationCode.Array_Create_WithLowerBound;
        arrayType = Matrix.GetMatrix(createArray.ElementType, createArray.Rank, createArray.LowerBounds, ((IMetadataCreateArray)createArray).Sizes, this.host.InternFactory);
      } else if (createArray.Rank > 1) {
        create = OperationCode.Array_Create;
        arrayType = Matrix.GetMatrix(createArray.ElementType, createArray.Rank, this.host.InternFactory);
      } else {
        create = OperationCode.Newarr;
        arrayType = Vector.GetVector(createArray.ElementType, this.host.InternFactory);
      }
      this.generator.Emit(create, arrayType);
      this.StackSize -= (ushort)(createArray.Rank+numLowerBounds-1);
      int i = 0;
      foreach (IExpression elemValue in createArray.Initializers) {
        this.generator.Emit(OperationCode.Dup);
        this.StackSize++;
        this.EmitConstant(i++);
        this.Visit(elemValue);
        this.StoreVectorElement(createArray.ElementType);
      }
      //TODO: initialization of non vectors and initialization from compile time constant
    }

    /// <summary>
    /// Visits the specified create delegate instance.
    /// </summary>
    /// <param name="createDelegateInstance">The create delegate instance.</param>
    public override void Visit(ICreateDelegateInstance createDelegateInstance) {
      IPlatformType platformType = createDelegateInstance.Type.PlatformType;
      MethodReference constructor = new MethodReference(this.host, createDelegateInstance.Type, CallingConvention.Default|CallingConvention.HasThis,
        platformType.SystemVoid, this.host.NameTable.Ctor, 0, platformType.SystemObject, platformType.SystemIntPtr);
      if (createDelegateInstance.Instance != null) {
        this.Visit(createDelegateInstance.Instance);
        if (createDelegateInstance.MethodToCallViaDelegate.ResolvedMethod.IsVirtual) {
          this.generator.Emit(OperationCode.Dup);
          this.StackSize++;
          this.generator.Emit(OperationCode.Ldvirtftn, createDelegateInstance.MethodToCallViaDelegate);
          this.StackSize--;
        } else
          this.generator.Emit(OperationCode.Ldftn, createDelegateInstance.MethodToCallViaDelegate);
        this.StackSize++;
      } else {
        this.generator.Emit(OperationCode.Ldnull);
        this.generator.Emit(OperationCode.Ldftn, createDelegateInstance.MethodToCallViaDelegate);
        this.StackSize+=2;
      }
      this.generator.Emit(OperationCode.Newobj, constructor);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified create object instance.
    /// </summary>
    /// <param name="createObjectInstance">The create object instance.</param>
    public override void Visit(ICreateObjectInstance createObjectInstance) {
      this.Visit(createObjectInstance.Arguments);
      this.generator.Emit(OperationCode.Newobj, createObjectInstance.MethodToCall);
      this.StackSize -= (ushort)IteratorHelper.EnumerableCount(createObjectInstance.Arguments);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified custom attribute.
    /// </summary>
    /// <param name="customAttribute">The custom attribute.</param>
    public override void Visit(ICustomAttribute customAttribute) {
      base.Visit(customAttribute);
    }

    /// <summary>
    /// Visits the specified custom modifier.
    /// </summary>
    /// <param name="customModifier">The custom modifier.</param>
    public override void Visit(ICustomModifier customModifier) {
      base.Visit(customModifier);
    }

    /// <summary>
    /// Visits the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    public override void Visit(IDefaultValue defaultValue) {
      ILocalDefinition temp = new TemporaryVariable(defaultValue.Type);
      this.generator.AddLocalToCurrentScope(temp);
      this.LoadAddressOf(temp, null);
      this.generator.Emit(OperationCode.Initobj, defaultValue.Type);
      this.StackSize--;
      this.LoadLocal(temp);
    }

    /// <summary>
    /// Visits the specified debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement">The debugger break statement.</param>
    public override void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
      this.generator.Emit(OperationCode.Break);
    }

    /// <summary>
    /// Visits the specified division.
    /// </summary>
    /// <param name="division">The division.</param>
    public override void Visit(IDivision division) {
      this.Visit(division.LeftOperand);
      this.Visit(division.RightOperand);
      if (TypeHelper.IsUnsignedPrimitiveInteger(division.Type))
        this.generator.Emit(OperationCode.Div_Un);
      else
        this.generator.Emit(OperationCode.Div);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified do until statement.
    /// </summary>
    /// <param name="doUntilStatement">The do until statement.</param>
    public override void Visit(IDoUntilStatement doUntilStatement) {
      ILGeneratorLabel savedCurrentBreakTarget = this.currentBreakTarget;
      ILGeneratorLabel savedCurrentContinueTarget = this.currentContinueTarget;
      this.currentBreakTarget = new ILGeneratorLabel();
      this.currentContinueTarget = new ILGeneratorLabel();
      if (this.currentTryCatch != null) {
        this.mostNestedTryCatchFor.Add(this.currentBreakTarget, this.currentTryCatch);
        this.mostNestedTryCatchFor.Add(this.currentContinueTarget, this.currentTryCatch);
      }

      this.generator.MarkLabel(this.currentContinueTarget);
      this.Visit(doUntilStatement.Body);
      this.VisitBranchIfFalse(doUntilStatement.Condition, this.currentContinueTarget);
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
    }

    /// <summary>
    /// Visits the specified empty statement.
    /// </summary>
    /// <param name="emptyStatement">The empty statement.</param>
    public override void Visit(IEmptyStatement emptyStatement) {
      if (!this.minizeCodeSize)
        this.generator.Emit(OperationCode.Nop);
    }

    /// <summary>
    /// Visits the specified equality.
    /// </summary>
    /// <param name="equality">The equality.</param>
    public override void Visit(IEquality equality) {
      this.Visit(equality.LeftOperand);
      this.Visit(equality.RightOperand);
      this.generator.Emit(OperationCode.Ceq);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified exclusive or.
    /// </summary>
    /// <param name="exclusiveOr">The exclusive or.</param>
    public override void Visit(IExclusiveOr exclusiveOr) {
      this.Visit(exclusiveOr.LeftOperand);
      this.Visit(exclusiveOr.RightOperand);
      this.generator.Emit(OperationCode.Xor);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified expression statement.
    /// </summary>
    /// <param name="expressionStatement">The expression statement.</param>
    public override void Visit(IExpressionStatement expressionStatement) {
      if (expressionStatement.Expression == CodeDummy.Expression) return;
      IAssignment/*?*/ assigment = expressionStatement.Expression as IAssignment;
      if (assigment != null) {
        this.VisitAssignment(assigment, true);
        return;
      }
      this.Visit(expressionStatement.Expression);
      if (expressionStatement.Expression.Type.TypeCode != PrimitiveTypeCode.Void) {
        this.generator.Emit(OperationCode.Pop);
        this.StackSize--;
      }
    }

    /// <summary>
    /// Visits the specified for each statement.
    /// </summary>
    /// <param name="forEachStatement">For each statement.</param>
    public override void Visit(IForEachStatement forEachStatement) {
      var arrayType = forEachStatement.Collection.Type as IArrayTypeReference;
      if (arrayType != null && arrayType.IsVector) {
        this.VisitForeachArrayElement(forEachStatement, arrayType);
        return;
      }
      //TODO: special case for enumerator that is sealed and does not implement IDisposable
      base.Visit(forEachStatement);
    }

    /// <summary>
    /// Generates IL code for the given for each statement for the special case where the collection is known
    /// to be vector type.
    /// </summary>
    /// <param name="forEachStatement">The foreach statement to visit.</param>
    /// <param name="arrayType">The vector type of the collection.</param>
    public virtual void VisitForeachArrayElement(IForEachStatement forEachStatement, IArrayTypeReference arrayType)
      //^ requires arrayType.IsVector;
    {
      ILGeneratorLabel savedCurrentBreakTarget = this.currentBreakTarget;
      ILGeneratorLabel savedCurrentContinueTarget = this.currentContinueTarget;
      this.currentBreakTarget = new ILGeneratorLabel();
      this.currentContinueTarget = new ILGeneratorLabel();
      if (this.currentTryCatch != null) {
        this.mostNestedTryCatchFor.Add(this.currentBreakTarget, this.currentTryCatch);
        this.mostNestedTryCatchFor.Add(this.currentContinueTarget, this.currentTryCatch);
      }
      ILGeneratorLabel conditionCheck = new ILGeneratorLabel();
      ILGeneratorLabel loopStart = new ILGeneratorLabel();

      this.EmitSequencePoint(forEachStatement.Variable.Locations);
      this.Visit(forEachStatement.Collection);
      this.generator.Emit(OperationCode.Dup);
      var array = new TemporaryVariable(arrayType);
      this.VisitAssignmentTo(array);
      var length = new TemporaryVariable(this.host.PlatformType.SystemInt32);
      this.generator.Emit(OperationCode.Ldlen);
      this.generator.Emit(OperationCode.Conv_I4);
      this.VisitAssignmentTo(length);
      var counter = new TemporaryVariable(this.host.PlatformType.SystemInt32);
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.VisitAssignmentTo(counter);
      this.generator.Emit(OperationCode.Br, conditionCheck);
      this.generator.MarkLabel(loopStart);
      this.LoadLocal(array);
      this.LoadLocal(counter);
      this.LoadVectorElement(arrayType.ElementType);
      this.VisitAssignmentTo(forEachStatement.Variable);
      this.Visit(forEachStatement.Body);
      this.generator.MarkLabel(this.currentContinueTarget);
      this.LoadLocal(counter);
      this.generator.Emit(OperationCode.Ldc_I4_1);
      this.generator.Emit(OperationCode.Add);
      this.VisitAssignmentTo(counter);
      this.generator.MarkLabel(conditionCheck);
      this.EmitSequencePoint(forEachStatement.Collection.Locations);
      this.LoadLocal(counter);
      this.LoadLocal(length);
      this.generator.Emit(OperationCode.Blt, loopStart);
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
    }

    /// <summary>
    /// Visits the specified for statement.
    /// </summary>
    /// <param name="forStatement">For statement.</param>
    public override void Visit(IForStatement forStatement) {
      ILGeneratorLabel savedCurrentBreakTarget = this.currentBreakTarget;
      ILGeneratorLabel savedCurrentContinueTarget = this.currentContinueTarget;
      this.currentBreakTarget = new ILGeneratorLabel();
      this.currentContinueTarget = new ILGeneratorLabel();
      if (this.currentTryCatch != null) {
        this.mostNestedTryCatchFor.Add(this.currentBreakTarget, this.currentTryCatch);
        this.mostNestedTryCatchFor.Add(this.currentContinueTarget, this.currentTryCatch);
      }
      ILGeneratorLabel conditionCheck = new ILGeneratorLabel();
      ILGeneratorLabel loopStart = new ILGeneratorLabel();

      this.Visit(forStatement.InitStatements);
      this.generator.Emit(OperationCode.Br, conditionCheck);
      this.generator.MarkLabel(loopStart);
      this.Visit(forStatement.Body);
      this.generator.MarkLabel(this.currentContinueTarget);
      this.Visit(forStatement.IncrementStatements);
      this.generator.MarkLabel(conditionCheck);
      this.EmitSequencePoint(forStatement.Condition.Locations);
      this.VisitBranchIfTrue(forStatement.Condition, loopStart);
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
    }

    /// <summary>
    /// Visits the specified get type of typed reference.
    /// </summary>
    /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
    public override void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
      this.Visit(getTypeOfTypedReference.TypedReference);
      this.generator.Emit(OperationCode.Refanytype);
    }

    /// <summary>
    /// Visits the specified get value of typed reference.
    /// </summary>
    /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
    public override void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
      this.Visit(getValueOfTypedReference.TypedReference);
      this.generator.Emit(OperationCode.Refanyval, getValueOfTypedReference.TargetType);
    }

    /// <summary>
    /// Visits the specified goto statement.
    /// </summary>
    /// <param name="gotoStatement">The goto statement.</param>
    public override void Visit(IGotoStatement gotoStatement) {
      ILGeneratorLabel targetLabel;
      if (!this.labelFor.TryGetValue(gotoStatement.TargetStatement.Label.UniqueKey, out targetLabel)) {
        targetLabel = new ILGeneratorLabel();
        this.labelFor.Add(gotoStatement.TargetStatement.Label.UniqueKey, targetLabel);
      }
      if (this.LabelIsOutsideCurrentExceptionBlock(targetLabel))
        this.generator.Emit(OperationCode.Leave, targetLabel);
      else
        this.generator.Emit(OperationCode.Br, targetLabel);
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Visits the specified goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
    public override void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      base.Visit(gotoSwitchCaseStatement);
    }

    /// <summary>
    /// Visits the specified greater than.
    /// </summary>
    /// <param name="greaterThan">The greater than.</param>
    public override void Visit(IGreaterThan greaterThan) {
      this.Visit(greaterThan.LeftOperand);
      this.Visit(greaterThan.RightOperand);
      if (TypeHelper.IsUnsignedPrimitiveInteger(greaterThan.LeftOperand.Type))
        this.generator.Emit(OperationCode.Cgt_Un); //unsigned
      else
        this.generator.Emit(OperationCode.Cgt);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified greater than or equal.
    /// </summary>
    /// <param name="greaterThanOrEqual">The greater than or equal.</param>
    public override void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
      this.Visit(greaterThanOrEqual.LeftOperand);
      this.Visit(greaterThanOrEqual.RightOperand);
      if (TypeHelper.IsSignedPrimitiveInteger(greaterThanOrEqual.LeftOperand.Type))
        this.generator.Emit(OperationCode.Clt);
      else
        this.generator.Emit(OperationCode.Clt_Un); //unsigned or unordered
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.generator.Emit(OperationCode.Ceq);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified labeled statement.
    /// </summary>
    /// <param name="labeledStatement">The labeled statement.</param>
    public override void Visit(ILabeledStatement labeledStatement) {
      ILGeneratorLabel targetLabel;
      if (!this.labelFor.TryGetValue(labeledStatement.Label.UniqueKey, out targetLabel)) {
        targetLabel = new ILGeneratorLabel();
        this.labelFor.Add(labeledStatement.Label.UniqueKey, targetLabel);
      }
      this.generator.MarkLabel(targetLabel);
      this.Visit(labeledStatement.Statement);
    }

    /// <summary>
    /// Visits the specified left shift.
    /// </summary>
    /// <param name="leftShift">The left shift.</param>
    public override void Visit(ILeftShift leftShift) {
      this.Visit(leftShift.LeftOperand);
      this.Visit(leftShift.RightOperand);
      this.generator.Emit(OperationCode.Shl);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified less than.
    /// </summary>
    /// <param name="lessThan">The less than.</param>
    public override void Visit(ILessThan lessThan) {
      this.Visit(lessThan.LeftOperand);
      this.Visit(lessThan.RightOperand);
      if (TypeHelper.IsUnsignedPrimitiveInteger(lessThan.LeftOperand.Type))
        this.generator.Emit(OperationCode.Clt_Un); //unsigned
      else
        this.generator.Emit(OperationCode.Clt);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified less than or equal.
    /// </summary>
    /// <param name="lessThanOrEqual">The less than or equal.</param>
    public override void Visit(ILessThanOrEqual lessThanOrEqual) {
      this.Visit(lessThanOrEqual.LeftOperand);
      this.Visit(lessThanOrEqual.RightOperand);
      if (TypeHelper.IsSignedPrimitiveInteger(lessThanOrEqual.LeftOperand.Type))
        this.generator.Emit(OperationCode.Cgt);
      else
        this.generator.Emit(OperationCode.Cgt_Un); //unsigned or unordered
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.generator.Emit(OperationCode.Ceq);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      if (localDeclarationStatement.InitialValue != null) {
        this.Visit(localDeclarationStatement.InitialValue);
        this.VisitAssignmentTo(localDeclarationStatement.LocalVariable);
      }
    }

    /// <summary>
    /// Visits the specified lock statement.
    /// </summary>
    /// <param name="lockStatement">The lock statement.</param>
    public override void Visit(ILockStatement lockStatement) {
      base.Visit(lockStatement);
    }

    /// <summary>
    /// Visits the specified logical not.
    /// </summary>
    /// <param name="logicalNot">The logical not.</param>
    public override void Visit(ILogicalNot logicalNot) {
      if (TypeHelper.IsPrimitiveInteger(logicalNot.Operand.Type)) {
        this.Visit(logicalNot.Operand);
        this.generator.Emit(OperationCode.Ldc_I4_0);
        this.generator.Emit(OperationCode.Ceq);
      } else {
        //pointer non null test
        this.Visit(logicalNot.Operand);
        this.generator.Emit(OperationCode.Ldnull);
        this.generator.Emit(OperationCode.Ceq);
      }
    }

    /// <summary>
    /// Visits the specified make typed reference.
    /// </summary>
    /// <param name="makeTypedReference">The make typed reference.</param>
    public override void Visit(IMakeTypedReference makeTypedReference) {
      this.LoadAddressOf(makeTypedReference.Operand, null);
      this.generator.Emit(OperationCode.Mkrefany, makeTypedReference.Operand.Type);
    }

    /// <summary>
    /// Visits the specified method call.
    /// </summary>
    /// <param name="methodCall">The method call.</param>
    public override void Visit(IMethodCall methodCall) {
      if (methodCall.MethodToCall == Dummy.MethodReference) return;
      if (this.contractProvider != null && methodCall.MethodToCall.InternedKey == this.contractProvider.ContractMethods.StartContract.InternedKey) {
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(this.method);
        if (methodContract != null) this.Visit(methodContract);
        return;
      }
      if (!methodCall.IsStaticCall)
        this.Visit(methodCall.ThisArgument);
      this.Visit(methodCall.Arguments);
      OperationCode call = OperationCode.Call;
      if (methodCall.IsVirtualCall) {
        call = OperationCode.Callvirt;
        IManagedPointerTypeReference mpt = methodCall.ThisArgument.Type as IManagedPointerTypeReference;
        if (mpt != null)
          this.generator.Emit(OperationCode.Constrained_, mpt.TargetType);
      }
      this.generator.Emit(call, methodCall.MethodToCall);
      this.StackSize -= (ushort)IteratorHelper.EnumerableCount(methodCall.Arguments);
      if (!methodCall.IsStaticCall) this.StackSize--;
      if (methodCall.Type.TypeCode != PrimitiveTypeCode.Void)
        this.StackSize++;
    }

    /// <summary>
    /// Traverses the given method contract.
    /// </summary>
    /// <param name="methodContract"></param>
    public override void Visit(IMethodContract methodContract) {
      this.Visit(methodContract.Postconditions);
      this.Visit(methodContract.Preconditions);
      this.generator.Emit(OperationCode.Call, this.contractProvider.ContractMethods.EndContract);
    }

    /// <summary>
    /// Visits the specified modulus.
    /// </summary>
    /// <param name="modulus">The modulus.</param>
    public override void Visit(IModulus modulus) {
      this.Visit(modulus.LeftOperand);
      this.Visit(modulus.RightOperand);
      if (TypeHelper.IsUnsignedPrimitiveInteger(modulus.Type))
        this.generator.Emit(OperationCode.Rem_Un);
      else
        this.generator.Emit(OperationCode.Rem);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified multiplication.
    /// </summary>
    /// <param name="multiplication">The multiplication.</param>
    public override void Visit(IMultiplication multiplication) {
      this.Visit(multiplication.LeftOperand);
      this.Visit(multiplication.RightOperand);
      OperationCode operationCode = OperationCode.Mul;
      if (multiplication.CheckOverflow) {
        if (TypeHelper.IsSignedPrimitiveInteger(multiplication.Type))
          operationCode = OperationCode.Mul_Ovf;
        else if (TypeHelper.IsUnsignedPrimitiveInteger(multiplication.Type))
          operationCode = OperationCode.Mul_Ovf_Un;
      }
      this.generator.Emit(operationCode);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified not equality.
    /// </summary>
    /// <param name="notEquality">The not equality.</param>
    public override void Visit(INotEquality notEquality) {
      this.Visit(notEquality.LeftOperand);
      this.Visit(notEquality.RightOperand);
      this.generator.Emit(OperationCode.Ceq);
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.generator.Emit(OperationCode.Ceq);
    }

    /// <summary>
    /// Visits the specified old value.
    /// </summary>
    /// <param name="oldValue">The old value.</param>
    public override void Visit(IOldValue oldValue) {
      if (this.contractProvider == null) return;
      this.Visit(oldValue.Expression);
      IEnumerable<ITypeReference> genArgs = IteratorHelper.GetSingletonEnumerable<ITypeReference>(oldValue.Type);
      GenericMethodInstanceReference oldInst = new GenericMethodInstanceReference(this.contractProvider.ContractMethods.Old, genArgs, this.host.InternFactory);
      this.generator.Emit(OperationCode.Call, oldInst);
    }

    /// <summary>
    /// Visits the specified ones complement.
    /// </summary>
    /// <param name="onesComplement">The ones complement.</param>
    public override void Visit(IOnesComplement onesComplement) {
      this.Visit(onesComplement.Operand);
      this.generator.Emit(OperationCode.Not);
    }

    /// <summary>
    /// Visits the specified out argument.
    /// </summary>
    /// <param name="outArgument">The out argument.</param>
    public override void Visit(IOutArgument outArgument) {
      this.LoadAddressOf(outArgument.Expression, null);
    }

    /// <summary>
    /// Visits the specified pointer call.
    /// </summary>
    /// <param name="pointerCall">The pointer call.</param>
    public override void Visit(IPointerCall pointerCall) {
      this.Visit(pointerCall.Arguments);
      this.Visit(pointerCall.Pointer);
      this.generator.Emit(OperationCode.Calli, pointerCall.Pointer.Type);
      this.StackSize -= (ushort)IteratorHelper.EnumerableCount(pointerCall.Arguments);
      if (pointerCall.Type.TypeCode == PrimitiveTypeCode.Void)
        this.StackSize--;
    }

    /// <summary>
    /// Traverses the given postCondition.
    /// </summary>
    /// <param name="postCondition"></param>
    public override void Visit(IPostcondition postCondition) {
      if (this.contractProvider == null) return;
      this.Visit(postCondition.Condition);
      this.generator.Emit(OperationCode.Call, this.contractProvider.ContractMethods.Ensures);
      this.StackSize--;
    }

    /// <summary>
    /// Traverses the given pre condition.
    /// </summary>
    /// <param name="precondition"></param>
    public override void Visit(IPrecondition precondition) {
      if (this.contractProvider == null) return;
      this.Visit(precondition.Condition);
      this.generator.Emit(OperationCode.Call, this.contractProvider.ContractMethods.Requires);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified ref argument.
    /// </summary>
    /// <param name="refArgument">The ref argument.</param>
    public override void Visit(IRefArgument refArgument) {
      this.LoadAddressOf(refArgument.Expression, null);
    }

    /// <summary>
    /// Visits the specified resource use statement.
    /// </summary>
    /// <param name="resourceUseStatement">The resource use statement.</param>
    public override void Visit(IResourceUseStatement resourceUseStatement) {
      base.Visit(resourceUseStatement);
    }

    /// <summary>
    /// Visits the specified rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement">The rethrow statement.</param>
    public override void Visit(IRethrowStatement rethrowStatement) {
      this.generator.Emit(OperationCode.Rethrow);
    }

    /// <summary>
    /// Visits the specified return statement.
    /// </summary>
    /// <param name="returnStatement">The return statement.</param>
    public override void Visit(IReturnStatement returnStatement) {
      if (returnStatement.Expression != null) {
        this.Visit(returnStatement.Expression);
        if (!this.minizeCodeSize || this.currentTryCatch != null) {
          if (this.returnLocal == null)
            this.returnLocal = new TemporaryVariable(this.method.Type);
          this.VisitAssignmentTo(this.returnLocal);
        } else
          this.StackSize--;
      }
      if (this.currentTryCatch != null)
        this.generator.Emit(OperationCode.Leave, this.endOfMethod);
      else if (!this.minizeCodeSize)
        this.generator.Emit(OperationCode.Br, this.endOfMethod);
      else
        this.generator.Emit(OperationCode.Ret);
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Performs some computation with the given return value expression.
    /// </summary>
    /// <param name="returnValue"></param>
    public override void Visit(IReturnValue returnValue) {
      if (this.contractProvider == null) return;
      IEnumerable<ITypeReference> genArgs = IteratorHelper.GetSingletonEnumerable<ITypeReference>(returnValue.Type);
      GenericMethodInstanceReference resultInst = new GenericMethodInstanceReference(this.contractProvider.ContractMethods.Result, genArgs, this.host.InternFactory);
      this.generator.Emit(OperationCode.Call, resultInst);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified right shift.
    /// </summary>
    /// <param name="rightShift">The right shift.</param>
    public override void Visit(IRightShift rightShift) {
      this.Visit(rightShift.LeftOperand);
      this.Visit(rightShift.RightOperand);
      if (TypeHelper.IsUnsignedPrimitiveInteger(rightShift.LeftOperand.Type))
        this.generator.Emit(OperationCode.Shr_Un);
      else
        this.generator.Emit(OperationCode.Shr);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
    public override void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      this.generator.Emit(OperationCode.Arglist);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified size of.
    /// </summary>
    /// <param name="sizeOf">The size of.</param>
    public override void Visit(ISizeOf sizeOf) {
      this.generator.Emit(OperationCode.Sizeof, sizeOf.TypeToSize);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    public override void Visit(ISourceMethodBody methodBody) {
      base.Visit(methodBody);
    }

    /// <summary>
    /// Visits the specified stack array create.
    /// </summary>
    /// <param name="stackArrayCreate">The stack array create.</param>
    public override void Visit(IStackArrayCreate stackArrayCreate) {
      this.Visit(stackArrayCreate.Size);
      this.generator.Emit(OperationCode.Localloc);
    }

    /// <summary>
    /// Traverses the given statement.
    /// </summary>
    /// <param name="statement"></param>
    public override void Visit(IStatement statement) {
      this.lastStatementWasUnconditionalTransfer = false;
      if (!(statement is IBlockStatement))
        this.EmitSequencePoint(statement.Locations);
      base.Visit(statement);
    }

    /// <summary>
    /// Visits the specified subtraction.
    /// </summary>
    /// <param name="subtraction">The subtraction.</param>
    public override void Visit(ISubtraction subtraction) {
      this.Visit(subtraction.LeftOperand);
      this.Visit(subtraction.RightOperand);
      OperationCode operationCode = OperationCode.Sub;
      if (subtraction.CheckOverflow) {
        if (TypeHelper.IsSignedPrimitiveInteger(subtraction.Type))
          operationCode = OperationCode.Sub_Ovf;
        else if (TypeHelper.IsUnsignedPrimitiveInteger(subtraction.Type))
          operationCode = OperationCode.Sub_Ovf_Un;
      }
      this.generator.Emit(operationCode);
      this.StackSize--;
    }

    /// <summary>
    /// Visits the specified switch case.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    public override void Visit(ISwitchCase switchCase) {
      this.Visit(switchCase.Body);
    }

    /// <summary>
    /// Visits the specified switch statement.
    /// </summary>
    /// <param name="switchStatement">The switch statement.</param>
    public override void Visit(ISwitchStatement switchStatement) {
      this.Visit(switchStatement.Expression);
      uint numberOfCases;
      uint maxValue = GetMaxCaseExpressionValueAsUInt(switchStatement.Cases, out numberOfCases);
      if (numberOfCases == 0) { this.generator.Emit(OperationCode.Pop); return; }
      if (maxValue < uint.MaxValue && maxValue/2 < numberOfCases)
        this.GenerateSwitchInstruction(switchStatement.Cases, maxValue);
      //TODO: generate binary search
      //TemporaryVariable switchVar = new TemporaryVariable(switchStatement.Expression.Type);
      //this.VisitAssignmentTo(switchVar);
      //List<ISwitchCase> switchCases = this.GetSortedListOfSwitchCases(switchStatement.Cases);
      //TODO: special handling for switch over strings
    }

    private void GenerateSwitchInstruction(IEnumerable<ISwitchCase> switchCases, uint maxValue) {
      ILGeneratorLabel savedCurrentBreakTarget = this.currentBreakTarget;
      this.currentBreakTarget = new ILGeneratorLabel();
      if (this.currentTryCatch != null) {
        this.mostNestedTryCatchFor.Add(this.currentBreakTarget, this.currentTryCatch);
      }
      ILGeneratorLabel[] labels = new ILGeneratorLabel[maxValue+1];
      ILGeneratorLabel defaultLabel = new ILGeneratorLabel();
      for (uint i = 0; i <= maxValue; i++) labels[i] = defaultLabel;
      Dictionary<ISwitchCase, ILGeneratorLabel> labelFor = new Dictionary<ISwitchCase, ILGeneratorLabel>();
      bool foundDefault = false;
      foreach (ISwitchCase switchCase in switchCases) {
        if (switchCase.IsDefault) {
          labelFor.Add(switchCase, defaultLabel);
          foundDefault = true;
        } else {
          ILGeneratorLabel caseLabel = new ILGeneratorLabel();
          uint i = (uint)((IConvertible)switchCase.Expression.Value).ToUInt64(null);
          labels[i] = caseLabel;
          labelFor.Add(switchCase, caseLabel);
        }
      }
      this.generator.Emit(OperationCode.Switch, labels);
      this.generator.Emit(OperationCode.Br, defaultLabel);
      if (!foundDefault)
        this.currentBreakTarget = defaultLabel;
      else
        this.currentBreakTarget = new ILGeneratorLabel();
      foreach (ISwitchCase switchCase in switchCases) {
        this.generator.MarkLabel(labelFor[switchCase]);
        this.Visit(switchCase);
      }
      this.generator.MarkLabel(this.currentBreakTarget);
      this.currentBreakTarget = savedCurrentBreakTarget;
    }

    private static uint GetMaxCaseExpressionValueAsUInt(IEnumerable<ISwitchCase> switchCases, out uint numberOfCases) {
      numberOfCases = 0;
      uint result = 0;
      foreach (ISwitchCase switchCase in switchCases) {
        numberOfCases++;
        if (switchCase.IsDefault) continue;
        object/*?*/ value = switchCase.Expression.Value;
        switch (System.Convert.GetTypeCode(value)) {
          case TypeCode.Int32:
            int i = (int)value;
            if (i < 0) result = uint.MaxValue;
            else if (i > result) result = (uint)i;
            break;
          case TypeCode.UInt32:
            uint ui = (uint)value;
            if (ui > result) result = ui;
            break;
          case TypeCode.Int64:
            long l = (long)value;
            if (l < 0) result = uint.MaxValue;
            else if (l >= uint.MaxValue) result = uint.MaxValue;
            else if (l > result) result = (uint)l;
            break;
          case TypeCode.UInt64:
            ulong ul = (ulong)value;
            if (ul >= uint.MaxValue) result = uint.MaxValue;
            else if (ul > result) result = (uint)ul;
            break;
          default:
            result = uint.MaxValue;
            break;
        }
      }
      return result;
    }

    //private static List<ISwitchCase> GetSortedListOfSwitchCases(IEnumerable<ISwitchCase> cases) {
    //  List<ISwitchCase> caseList = new List<ISwitchCase>(cases);
    //  caseList.Sort(
    //    delegate(ISwitchCase x, ISwitchCase y) {
    //      if (x == y) return 0;
    //      if (x.IsDefault) return -1;
    //      if (y.IsDefault) return 1;
    //      if (x.Expression == CodeDummy.Constant) return -1;
    //      if (y.Expression == CodeDummy.Constant) return 1;
    //      object/*?*/ xvalue = x.Expression.Value;
    //      object/*?*/ yvalue = y.Expression.Value;
    //      switch (System.Convert.GetTypeCode(xvalue)) {
    //        case TypeCode.Int32:
    //          if (!(yvalue is int)) return 1;
    //          return (int)xvalue - (int)yvalue;
    //        case TypeCode.UInt32:
    //          if (!(yvalue is uint)) return 1;
    //          return (int)((uint)xvalue - (uint)yvalue);
    //        case TypeCode.Int64:
    //          if (!(yvalue is long)) return 1;
    //          return (int)((long)xvalue - (long)yvalue);
    //        case TypeCode.UInt64:
    //          if (!(yvalue is ulong)) return 1;
    //          return (int)((ulong)xvalue - (ulong)yvalue);
    //        case TypeCode.String:
    //          string ystr = yvalue as string;
    //          if (ystr == null) return 1;
    //          return string.CompareOrdinal(ystr, (string)yvalue);
    //        default:
    //          //^ assume false;
    //          if (xvalue == null) return -1;
    //          if (yvalue == null) return 1;
    //          return xvalue.GetHashCode() - yvalue.GetHashCode();
    //      }
    //    }
    //  );
    //  return caseList;
    //}

    /// <summary>
    /// Visits the specified this reference.
    /// </summary>
    /// <param name="thisReference">The this reference.</param>
    public override void Visit(IThisReference thisReference) {
      this.generator.Emit(OperationCode.Ldarg_0);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified throw statement.
    /// </summary>
    /// <param name="throwStatement">The throw statement.</param>
    public override void Visit(IThrowStatement throwStatement) {
      this.Visit(throwStatement.Exception);
      this.generator.Emit(OperationCode.Throw);
      this.StackSize = 0;
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Visits the specified try catch filter finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
    public override void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      ITryCatchFinallyStatement/*?*/ savedCurrentTryCatch = this.currentTryCatch;
      this.currentTryCatch = tryCatchFilterFinallyStatement;
      ILGeneratorLabel/*?*/ savedCurrentTryCatchFinallyEnd = this.currentTryCatchFinallyEnd;
      this.currentTryCatchFinallyEnd = new ILGeneratorLabel();
      this.generator.BeginTryBody();
      this.Visit(tryCatchFilterFinallyStatement.TryBody);
      if (!this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Leave, this.currentTryCatchFinallyEnd);
      this.Visit(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null) {
        this.generator.BeginFinallyBlock();
        this.Visit(tryCatchFilterFinallyStatement.FinallyBody);
        this.generator.Emit(OperationCode.Endfinally);
      }
      this.generator.EndTryBody();
      this.generator.MarkLabel(this.currentTryCatchFinallyEnd);
      this.currentTryCatchFinallyEnd = savedCurrentTryCatchFinallyEnd;
      this.currentTryCatch = savedCurrentTryCatch;
    }

    /// <summary>
    /// Visits the specified token of.
    /// </summary>
    /// <param name="tokenOf">The token of.</param>
    public override void Visit(ITokenOf tokenOf) {
      IFieldReference/*?*/ fieldReference = tokenOf.Definition as IFieldReference;
      if (fieldReference != null)
        this.generator.Emit(OperationCode.Ldtoken, fieldReference);
      else {
        IMethodReference/*?*/ methodReference = tokenOf.Definition as IMethodReference;
        if (methodReference != null)
          this.generator.Emit(OperationCode.Ldtoken, methodReference);
        else
          this.generator.Emit(OperationCode.Ldtoken, (ITypeReference)tokenOf.Definition);
      }
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified type of.
    /// </summary>
    /// <param name="typeOf">The type of.</param>
    public override void Visit(ITypeOf typeOf) {
      this.generator.Emit(OperationCode.Ldtoken, typeOf.TypeToGet);
      this.generator.Emit(OperationCode.Call, this.GetTypeFromHandle);
      this.StackSize++;
    }

    /// <summary>
    /// Visits the specified unary negation.
    /// </summary>
    /// <param name="unaryNegation">The unary negation.</param>
    public override void Visit(IUnaryNegation unaryNegation) {
      if (unaryNegation.CheckOverflow && TypeHelper.IsSignedPrimitiveInteger(unaryNegation.Type)) {
        this.generator.Emit(OperationCode.Ldc_I4_0);
        this.Visit(unaryNegation.Operand);
        this.generator.Emit(OperationCode.Sub_Ovf);
        return;
      }
      this.Visit(unaryNegation.Operand);
      this.generator.Emit(OperationCode.Neg);
    }

    /// <summary>
    /// Visits the specified unary plus.
    /// </summary>
    /// <param name="unaryPlus">The unary plus.</param>
    public override void Visit(IUnaryPlus unaryPlus) {
      this.Visit(unaryPlus.Operand);
    }

    /// <summary>
    /// Visits the specified vector length.
    /// </summary>
    /// <param name="vectorLength">Length of the vector.</param>
    public override void Visit(IVectorLength vectorLength) {
      this.Visit(vectorLength.Vector);
      this.generator.Emit(OperationCode.Ldlen);
    }

    /// <summary>
    /// Visits the specified while do statement.
    /// </summary>
    /// <param name="whileDoStatement">The while do statement.</param>
    public override void Visit(IWhileDoStatement whileDoStatement) {
      ILGeneratorLabel savedCurrentBreakTarget = this.currentBreakTarget;
      ILGeneratorLabel savedCurrentContinueTarget = this.currentContinueTarget;
      this.currentBreakTarget = new ILGeneratorLabel();
      this.currentContinueTarget = new ILGeneratorLabel();
      if (this.currentTryCatch != null) {
        this.mostNestedTryCatchFor.Add(this.currentBreakTarget, this.currentTryCatch);
        this.mostNestedTryCatchFor.Add(this.currentContinueTarget, this.currentTryCatch);
      }
      ILGeneratorLabel loopStart = new ILGeneratorLabel();

      this.generator.Emit(OperationCode.Br, this.currentContinueTarget);
      this.generator.MarkLabel(loopStart);
      this.Visit(whileDoStatement.Body);
      this.generator.MarkLabel(this.currentContinueTarget);
      this.VisitBranchIfTrue(whileDoStatement.Condition, loopStart);
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
    }

    /// <summary>
    /// Visits the specified yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement">The yield break statement.</param>
    public override void Visit(IYieldBreakStatement yieldBreakStatement) {
      base.Visit(yieldBreakStatement);
    }

    /// <summary>
    /// Visits the specified yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement">The yield return statement.</param>
    public override void Visit(IYieldReturnStatement yieldReturnStatement) {
      base.Visit(yieldReturnStatement);
    }

    private void VisitCheckedConversion(ITypeReference sourceType, ITypeReference targetType) {
      switch (sourceType.TypeCode) {
        case PrimitiveTypeCode.Boolean:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
              break;

            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int8:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1);
              break;

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_Ovf_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_Ovf_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.UInt8:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1_Un);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2_Un);
              break;

            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              break;

            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int16:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2);
              break;

            case PrimitiveTypeCode.Int32:
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_Ovf_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_Ovf_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.UInt16:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1_Un);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1_Un);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2_Un);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int32:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2);
              break;

            case PrimitiveTypeCode.Int32:
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_Ovf_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.UInt32:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1_Un);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1_Un);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2_Un);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2_Un);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_Ovf_I4_Un);
              break;

            case PrimitiveTypeCode.UInt32:
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int64:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_I8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_Ovf_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4);
              break;

            case PrimitiveTypeCode.Int64:
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_Ovf_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_Ovf_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_Ovf_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.UInt64:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1_Un);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1_Un);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2_Un);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2_Un);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_Ovf_I4_Un);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4_Un);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_Ovf_I8_Un);
              break;

            case PrimitiveTypeCode.UInt64:
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_Ovf_I_Un);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_Ovf_U_Un);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Float32:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_R4);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_Ovf_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_Ovf_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_Ovf_U8);
              break;

            case PrimitiveTypeCode.Float32:
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_Ovf_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_Ovf_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Float64:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_R8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_Ovf_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_Ovf_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_Ovf_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_Ovf_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_Ovf_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.IntPtr:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_I);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_Ovf_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_Ovf_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_Ovf_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Pointer:
        case PrimitiveTypeCode.Reference:
        case PrimitiveTypeCode.UIntPtr:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_Ovf_I1_Un);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_Ovf_U1_Un);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_Ovf_I2_Un);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_Ovf_U2_Un);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_Ovf_I4_Un);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_Ovf_U4_Un);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_Ovf_I8_Un);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_Ovf_I_Un);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        default:
          //TODO: conversion from &func to function pointer
          //Debug.Assert(false); //Only numeric conversions should be checked
          break;
      }
    }

    private void VisitUncheckedConversion(ITypeReference sourceType, ITypeReference targetType) {
      switch (sourceType.TypeCode) {
        case PrimitiveTypeCode.Boolean:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.UInt32:
            case PrimitiveTypeCode.UInt8:
              break;

            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int8:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.UInt8:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
            case PrimitiveTypeCode.UInt8:
            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              break;

            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int16:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.UInt16:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int32:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.UInt32:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
            case PrimitiveTypeCode.UInt32:
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Int64:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_I8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.UInt64:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
            case PrimitiveTypeCode.UInt64:
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Float32:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_R4);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Float64:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_R8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.IntPtr:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_I);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              this.generator.Emit(OperationCode.Conv_U);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        case PrimitiveTypeCode.Pointer:
        case PrimitiveTypeCode.Reference:
        case PrimitiveTypeCode.UIntPtr:
          switch (targetType.TypeCode) {
            case PrimitiveTypeCode.Boolean:
              this.generator.Emit(OperationCode.Ldc_I4_1);
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              break;

            case PrimitiveTypeCode.Int8:
              this.generator.Emit(OperationCode.Conv_I1);
              break;

            case PrimitiveTypeCode.UInt8:
              this.generator.Emit(OperationCode.Conv_U1);
              break;

            case PrimitiveTypeCode.Int16:
              this.generator.Emit(OperationCode.Conv_I2);
              break;

            case PrimitiveTypeCode.Char:
            case PrimitiveTypeCode.UInt16:
              this.generator.Emit(OperationCode.Conv_U2);
              break;

            case PrimitiveTypeCode.Int32:
              this.generator.Emit(OperationCode.Conv_I4);
              break;

            case PrimitiveTypeCode.UInt32:
              this.generator.Emit(OperationCode.Conv_U4);
              break;

            case PrimitiveTypeCode.Int64:
              this.generator.Emit(OperationCode.Conv_I8);
              break;

            case PrimitiveTypeCode.UInt64:
              this.generator.Emit(OperationCode.Conv_U8);
              break;

            case PrimitiveTypeCode.Float32:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R4);
              break;

            case PrimitiveTypeCode.Float64:
              this.generator.Emit(OperationCode.Conv_R_Un);
              this.generator.Emit(OperationCode.Conv_R8);
              break;

            case PrimitiveTypeCode.IntPtr:
              this.generator.Emit(OperationCode.Conv_I);
              break;

            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        default:
          if (sourceType.IsValueType) {
            this.generator.Emit(OperationCode.Box, sourceType);
            break;
          }
          //TODO: conversion from method to (function) pointer
          if (!sourceType.IsValueType && targetType.TypeCode == PrimitiveTypeCode.IntPtr)
            this.generator.Emit(OperationCode.Conv_I);
          else
            this.generator.Emit(OperationCode.Unbox_Any, targetType);
          break;
      }
    }

    private void VisitBranchIfFalse(IExpression expression, ILGeneratorLabel targetLabel) {
      OperationCode branchOp = OperationCode.Brfalse;
      IBinaryOperation/*?*/ binaryOperation = expression as IBinaryOperation;
      bool signedInteger = binaryOperation != null && TypeHelper.IsSignedPrimitiveInteger(binaryOperation.LeftOperand.Type);
      if (binaryOperation is IEquality) {
        branchOp = OperationCode.Bne_Un;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) || binaryOperation.LeftOperand is IDefaultValue) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || binaryOperation.LeftOperand is IDefaultValue) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is INotEquality) {
        branchOp = OperationCode.Beq;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) || binaryOperation.LeftOperand is IDefaultValue) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || binaryOperation.RightOperand is IDefaultValue) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is ILessThan) branchOp = signedInteger ? OperationCode.Bge : OperationCode.Bge_Un;
      else if (binaryOperation is ILessThanOrEqual) branchOp = signedInteger ? OperationCode.Bgt : OperationCode.Bgt_Un;
      else if (binaryOperation is IGreaterThan) branchOp = signedInteger ? OperationCode.Ble : OperationCode.Ble_Un;
      else if (binaryOperation is IGreaterThanOrEqual) branchOp = signedInteger ? OperationCode.Blt : OperationCode.Blt_Un;
      else {
        IConditional/*?*/ conditional = expression as IConditional;
        if (conditional != null) {
          ICompileTimeConstant/*?*/ resultIfFalse = conditional.ResultIfFalse as ICompileTimeConstant;
          if (resultIfFalse != null && resultIfFalse.Value is bool && !((bool)resultIfFalse.Value)) {
            //conditional.Condition && conditional.ResultIfTrue
            this.VisitBranchIfFalse(conditional.Condition, targetLabel);
            this.VisitBranchIfFalse(conditional.ResultIfTrue, targetLabel);
            return;
          }
          ICompileTimeConstant/*?*/ resultIfTrue = conditional.ResultIfTrue as ICompileTimeConstant;
          if (resultIfTrue != null && resultIfTrue.Value is bool && (bool)resultIfTrue.Value) {
            //conditional.Condition || conditional.ResultIfFalse
            ILGeneratorLabel fallThrough = new ILGeneratorLabel();
            this.VisitBranchIfTrue(conditional.Condition, fallThrough);
            this.VisitBranchIfFalse(conditional.ResultIfFalse, targetLabel);
            this.generator.MarkLabel(fallThrough);
            return;
          }
        }
        IMethodCall/*?*/ methodCall = expression as IMethodCall;
        if (methodCall != null) {
          int mkey = methodCall.MethodToCall.Name.UniqueKey;
          if ((mkey == this.host.NameTable.OpEquality.UniqueKey || mkey == this.host.NameTable.OpInequality.UniqueKey) &&
            TypeHelper.TypesAreEquivalent(methodCall.MethodToCall.ContainingType, methodCall.Type.PlatformType.SystemMulticastDelegate)) {
            List<IExpression> operands = new List<IExpression>(methodCall.Arguments);
            if (operands.Count == 2) {
              if (ExpressionHelper.IsNullLiteral(operands[0]))
                expression = operands[1];
              else if (ExpressionHelper.IsNullLiteral(operands[1]))
                expression = operands[0];
              if (expression != methodCall)
                branchOp = methodCall.MethodToCall.Name.UniqueKey == this.host.NameTable.OpEquality.UniqueKey ? OperationCode.Brtrue : OperationCode.Brfalse;
            }
          }
        } else {
          ILogicalNot/*?*/ logicalNot = expression as ILogicalNot;
          if (logicalNot != null) {
            expression = logicalNot.Operand;
            branchOp = OperationCode.Brtrue;
          }
        }
      }
      if (branchOp == OperationCode.Brfalse || branchOp == OperationCode.Brtrue) {
        this.Visit(expression);
        this.StackSize--;
      } else {
        this.Visit(binaryOperation.LeftOperand);
        this.Visit(binaryOperation.RightOperand);
        this.StackSize-=2;
      }
      this.generator.Emit(branchOp, targetLabel);
    }

    private void VisitBranchIfTrue(IExpression expression, ILGeneratorLabel targetLabel) {
      OperationCode branchOp = OperationCode.Brtrue;
      IBinaryOperation/*?*/ binaryOperation = expression as IBinaryOperation;
      bool signedInteger = binaryOperation != null && TypeHelper.IsSignedPrimitiveInteger(binaryOperation.LeftOperand.Type);
      if (binaryOperation is IEquality) {
        branchOp = OperationCode.Beq;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) || binaryOperation.LeftOperand is IDefaultValue) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || binaryOperation.RightOperand is IDefaultValue) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is INotEquality) {
        branchOp = OperationCode.Bne_Un;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) || binaryOperation.LeftOperand is IDefaultValue) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || binaryOperation.RightOperand is IDefaultValue) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is ILessThan) branchOp = signedInteger ? OperationCode.Blt : OperationCode.Blt_Un;
      else if (binaryOperation is ILessThanOrEqual) branchOp = signedInteger ? OperationCode.Ble : OperationCode.Ble_Un;
      else if (binaryOperation is IGreaterThan) branchOp = signedInteger ? OperationCode.Bgt : OperationCode.Bgt_Un;
      else if (binaryOperation is IGreaterThanOrEqual) branchOp = signedInteger ? OperationCode.Bge : OperationCode.Bge_Un;
      else {
        IConditional/*?*/ conditional = expression as IConditional;
        if (conditional != null) {
          ICompileTimeConstant/*?*/ resultIfFalse = conditional.ResultIfFalse as ICompileTimeConstant;
          if (resultIfFalse != null && resultIfFalse.Value is bool && !((bool)resultIfFalse.Value)) {
            //conditional.Condition && conditional.ResultIfTrue
            ILGeneratorLabel fallThrough = new ILGeneratorLabel();
            this.VisitBranchIfFalse(conditional.Condition, fallThrough);
            this.VisitBranchIfTrue(conditional.ResultIfTrue, targetLabel);
            this.generator.MarkLabel(fallThrough);
            return;
          }
          ICompileTimeConstant/*?*/ resultIfTrue = conditional.ResultIfTrue as ICompileTimeConstant;
          if (resultIfTrue != null && resultIfTrue.Value is bool && (bool)resultIfTrue.Value) {
            //conditional.Condition || conditional.ResultIfFalse
            this.VisitBranchIfTrue(conditional.Condition, targetLabel);
            this.VisitBranchIfTrue(conditional.ResultIfFalse, targetLabel);
            return;
          }
        }
        IMethodCall/*?*/ methodCall = expression as IMethodCall;
        if (methodCall != null) {
          int mkey = methodCall.MethodToCall.Name.UniqueKey;
          if ((mkey == this.host.NameTable.OpEquality.UniqueKey || mkey == this.host.NameTable.OpInequality.UniqueKey) &&
            TypeHelper.TypesAreEquivalent(methodCall.MethodToCall.ContainingType, methodCall.Type.PlatformType.SystemMulticastDelegate)) {
            List<IExpression> operands = new List<IExpression>(methodCall.Arguments);
            if (operands.Count == 2) {
              if (ExpressionHelper.IsNullLiteral(operands[0]))
                expression = operands[1];
              else if (ExpressionHelper.IsNullLiteral(operands[1]))
                expression = operands[0];
              if (expression != methodCall)
                branchOp = methodCall.MethodToCall.Name.UniqueKey == this.host.NameTable.OpEquality.UniqueKey ? OperationCode.Brfalse : OperationCode.Brtrue;
            }
          }
        }
      }
      if (branchOp == OperationCode.Brfalse || branchOp == OperationCode.Brtrue) {
        this.Visit(expression);
        this.StackSize--;
      } else {
        this.Visit(binaryOperation.LeftOperand);
        this.Visit(binaryOperation.RightOperand);
        this.StackSize-=2;
      }
      this.generator.Emit(branchOp, targetLabel);
    }

    private MethodReference DecimalConstructor {
      get {
        if (this.decimalConstructor == null)
          this.decimalConstructor = new MethodReference(this.host, this.host.PlatformType.SystemDecimal,
             CallingConvention.HasThis, this.host.PlatformType.SystemVoid, this.host.NameTable.Ctor, 0,
             this.host.PlatformType.SystemInt32, this.host.PlatformType.SystemInt32, this.host.PlatformType.SystemInt32,
             this.host.PlatformType.SystemBoolean, this.host.PlatformType.SystemUInt8);
        return this.decimalConstructor;
      }
    }
    private MethodReference/*?*/ decimalConstructor;

    private void EmitConstant(IConvertible ic) {
      this.StackSize++;
      if (ic == null)
        this.generator.Emit(OperationCode.Ldnull);
      else {
        TypeCode tc = ic.GetTypeCode();
        switch (tc) {
          case TypeCode.Boolean:
          case TypeCode.SByte:
          case TypeCode.Byte:
          case TypeCode.Char:
          case TypeCode.Int16:
          case TypeCode.UInt16:
          case TypeCode.Int32:
          case TypeCode.UInt32:
          case TypeCode.Int64:
            long n = ic.ToInt64(null);
            switch (n) {
              case -1: this.generator.Emit(OperationCode.Ldc_I4_M1); break;
              case 0: this.generator.Emit(OperationCode.Ldc_I4_0); break;
              case 1: this.generator.Emit(OperationCode.Ldc_I4_1); break;
              case 2: this.generator.Emit(OperationCode.Ldc_I4_2); break;
              case 3: this.generator.Emit(OperationCode.Ldc_I4_3); break;
              case 4: this.generator.Emit(OperationCode.Ldc_I4_4); break;
              case 5: this.generator.Emit(OperationCode.Ldc_I4_5); break;
              case 6: this.generator.Emit(OperationCode.Ldc_I4_6); break;
              case 7: this.generator.Emit(OperationCode.Ldc_I4_7); break;
              case 8: this.generator.Emit(OperationCode.Ldc_I4_8); break;
              default:
                if (sbyte.MinValue <= n && n <= sbyte.MaxValue) {
                  this.generator.Emit(OperationCode.Ldc_I4_S, (sbyte)n);
                } else if (int.MinValue <= n && n <= int.MaxValue ||
                n <= uint.MaxValue && (tc == TypeCode.Char || tc == TypeCode.UInt16 || tc == TypeCode.UInt32)) {
                  if (n == uint.MaxValue)
                    this.generator.Emit(OperationCode.Ldc_I4_M1);
                  else
                    this.generator.Emit(OperationCode.Ldc_I4, (int)n);
                } else {
                  this.generator.Emit(OperationCode.Ldc_I8, n);
                  tc = TypeCode.Empty; //Suppress conversion to long
                }
                break;
            }
            if (tc == TypeCode.Int64)
              this.generator.Emit(OperationCode.Conv_I8);
            return;

          case TypeCode.UInt64:
            this.generator.Emit(OperationCode.Ldc_I8, (long)ic.ToUInt64(null));
            return;

          case TypeCode.Single:
            this.generator.Emit(OperationCode.Ldc_R4, ic.ToSingle(null));
            return;

          case TypeCode.Double:
            this.generator.Emit(OperationCode.Ldc_R8, ic.ToDouble(null));
            return;

          case TypeCode.String:
            this.generator.Emit(OperationCode.Ldstr, ic.ToString(null));
            return;

          case TypeCode.Decimal:
            var bits = Decimal.GetBits(ic.ToDecimal(null));
            this.generator.Emit(OperationCode.Ldc_I4, bits[0]);
            this.generator.Emit(OperationCode.Ldc_I4, bits[1]); this.StackSize++;
            this.generator.Emit(OperationCode.Ldc_I4, bits[2]); this.StackSize++;
            if (bits[3] >= 0)
              this.generator.Emit(OperationCode.Ldc_I4_0);
            else
              this.generator.Emit(OperationCode.Ldc_I4_1);
            this.StackSize++;
            int scale = (bits[3]&0x7FFFFF)>>16;
            if (scale > 28) scale = 28;
            this.generator.Emit(OperationCode.Ldc_I4_S, scale); this.StackSize++;
            this.generator.Emit(OperationCode.Newobj, this.DecimalConstructor);
            this.StackSize -= 4;
            return;
        }
      }
    }

    private void EmitSequencePoint(IEnumerable<ILocation> locations) {
      if (this.sourceLocationProvider == null) return;
      foreach (IPrimarySourceLocation sloc in this.sourceLocationProvider.GetPrimarySourceLocationsFor(locations)) {
        this.generator.MarkSequencePoint(sloc);
        if (!this.minizeCodeSize)
          this.generator.Emit(OperationCode.Nop);
        return;
      }
    }

    private ushort GetLocalIndex(ILocalDefinition local) {
      ushort localIndex;
      if (this.localIndex.TryGetValue(local, out localIndex)) return localIndex;
      localIndex = (ushort)this.localIndex.Count;
      this.localIndex.Add(local, localIndex);
      this.temporaries.Add(local);
      return localIndex;
    }

    /// <summary>
    /// Returns all of the local variables (including compiler generated temporary variables) that are local to the block
    /// of statements translated by this converter.
    /// </summary>
    public IEnumerable<ILocalDefinition> GetLocalVariables() {
      return this.temporaries.AsReadOnly();
    }

    /// <summary>
    /// Returns the IL operations that correspond to the statements that have been converted to IL by this converter.
    /// </summary>
    public IEnumerable<IOperation> GetOperations() {
      return this.generator.GetOperations();
    }

    /// <summary>
    /// Returns zero or more exception exception information blocks (information about handlers, filters and finally blocks)
    /// that correspond to try-catch-finally constructs that appear in the statements that have been converted to IL by this converter.
    /// </summary>
    public IEnumerable<IOperationExceptionInformation> GetOperationExceptionInformation() {
      return this.generator.GetOperationExceptionInformation();
    }

    /// <summary>
    /// Returns zero or more types that are used to keep track of information needed to implement
    /// the statements that have been converted to IL by this converter. For example, any closure classes
    /// needed to compile anonymous delegate expressions (lambdas) will be returned by this method.
    /// </summary>
    public virtual IEnumerable<ITypeDefinition> GetPrivateHelperTypes() {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinition>();
    }

    /// <summary>
    /// A reference to System.Type.GetTypeFromHandle(System.Runtime.TypeHandle).
    /// </summary>
    IMethodReference GetTypeFromHandle {
      get {
        if (this.getTypeFromHandle == null) {
          this.getTypeFromHandle = new MethodReference(this.host, this.host.PlatformType.SystemType, CallingConvention.Default, this.host.PlatformType.SystemType,
          this.host.NameTable.GetNameFor("GetTypeFromHandle"), 0, this.host.PlatformType.SystemRuntimeTypeHandle);
        }
        return this.getTypeFromHandle;
      }
    }
    IMethodReference/*?*/ getTypeFromHandle;

    /// <summary>
    /// Traverses the given block of statements in the context of the given method to produce a list of
    /// IL operations, exception information blocks (the locations of handlers, filters and finallies) and any private helper
    /// types (for example closure classes) that represent the semantics of the given block of statements.
    /// The results of the traversal can be retrieved via the GetOperations, GetOperationExceptionInformation
    /// and GetPrivateHelperTypes methods.
    /// </summary>
    /// <param name="method">A method that provides the context for a block of statments that are to be converted to IL.</param>
    /// <param name="body">A block of statements that are to be converted to IL.</param>
    public virtual void ConvertToIL(IMethodDefinition method, IBlockStatement body) {
      this.method = method;
      ITypeReference returnType = method.Type;
      new LabelAndTryBlockAssociater(this.mostNestedTryCatchFor).Visit(body);
      this.Visit(body);
      this.generator.MarkLabel(this.endOfMethod);
      if (this.returnLocal != null) {
        this.LoadLocal(this.returnLocal);
        this.generator.Emit(OperationCode.Ret);
      } else if (returnType.TypeCode == PrimitiveTypeCode.Void && !this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Ret);
      this.generator.AdjustBranchSizesToBestFit();
    }
  }

  internal class LabelAndTryBlockAssociater : BaseCodeTraverser {

    Dictionary<object, ITryCatchFinallyStatement> mostNestedTryCatchFor;

    ITryCatchFinallyStatement/*?*/ currentTryCatch;

    internal LabelAndTryBlockAssociater(Dictionary<object, ITryCatchFinallyStatement> mostNestedTryCatchFor) {
      this.mostNestedTryCatchFor = mostNestedTryCatchFor;
    }

    public override void Visit(ILabeledStatement labeledStatement) {
      if (this.currentTryCatch != null)
        this.mostNestedTryCatchFor.Add(labeledStatement, this.currentTryCatch);
      base.Visit(labeledStatement);
    }

    public override void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      ITryCatchFinallyStatement/*?*/ savedCurrentTryCatch = this.currentTryCatch;
      this.currentTryCatch = tryCatchFilterFinallyStatement;
      base.Visit(tryCatchFilterFinallyStatement);
      this.currentTryCatch = savedCurrentTryCatch;
    }

  }

}
