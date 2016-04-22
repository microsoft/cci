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
using Microsoft.Cci.Immutable;

namespace Microsoft.Cci {
  using Microsoft.Cci.CodeModelToIL;

  /// <summary>
  /// An object with a method that converts a given block of statements to a list of IL operations, exception information and possibly some private helper types.
  /// </summary>
  public class CodeModelToILConverter : CodeTraverser, ISourceToILConverter {

    /// <summary>
    /// Initializes an object with a method that converts a given block of statements to a list of IL operations, exception information and possibly some private helper types.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="method">The method that contains the block of statements that will be converted.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in the block of statements to IPrimarySourceLocation objects.  May be null.</param>
    public CodeModelToILConverter(IMetadataHost host, IMethodDefinition method, ISourceLocationProvider/*?*/ sourceLocationProvider) {
      Contract.Requires(host != null);
      Contract.Requires(method != null);
      this.generator = new ILGenerator(host, method);
      this.host = host;
      this.method = method;
      this.sourceLocationProvider = sourceLocationProvider;
      this.minizeCodeSize = true;
    }

    /// <summary>
    /// Initializes an object with a method that converts a given block of statements to a list of IL operations, exception information and possibly some private helper types.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="method">The method that contains the block of statements that will be converted.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in the block of statements to IPrimarySourceLocation objects.  May be null.</param>
    /// <param name="asyncMethod">May be null.</param>
    /// <param name="iteratorLocalCount">A map that indicates how many iterator locals are present in a given block. Only useful for generated MoveNext methods. May be null.</param>
    public CodeModelToILConverter(IMetadataHost host, IMethodDefinition method, ISourceLocationProvider/*?*/ sourceLocationProvider,
      IMethodDefinition/*?*/ asyncMethod, IDictionary<IBlockStatement, uint>/*?*/ iteratorLocalCount) {
      Contract.Requires(host != null);
      Contract.Requires(method != null);
      this.generator = new ILGenerator(host, method, asyncMethod);
      this.host = host;
      this.method = method;
      this.sourceLocationProvider = sourceLocationProvider;
      this.minizeCodeSize = true;
      this.iteratorLocalCount = iteratorLocalCount;
    }

    /// <summary>
    /// A label for the instruction to where a break statement should currently branch to.
    /// </summary>
    ILGeneratorLabel currentBreakTarget = new ILGeneratorLabel();

    /// <summary>
    /// A label for the instruction to where a continue statement should currently branch to.
    /// </summary>
    ILGeneratorLabel currentContinueTarget = new ILGeneratorLabel();

    /// <summary>
    /// True when the binary expression currently being processed is the top level expression of an ExpressionStatement and it has
    /// a target expression as its left operand (i.e. it is an assignment statement of the form tgt op= src).
    /// Be sure to clear this flag before any sub expresions are processed.
    /// </summary>
    bool currentExpressionIsStatement;

    /// <summary>
    /// A label for the first instruction that comes after the current TryCatchFinally statement.
    /// </summary>
    ILGeneratorLabel/*?*/ currentTryCatchFinallyEnd;

    /// <summary>
    /// The TryCatchFinally statement for which IL is currently being generated.
    /// </summary>
    IStatement/*?*/ currentTryCatch;

    /// <summary>
    /// A label for the final (return) instruction in the current method, or the instruction that loads the argument of the final return instruction.
    /// If there is no final return intruction, this is location of the instruction following the final instruction (which of course does not exist).
    /// </summary>
    ILGeneratorLabel endOfMethod = new ILGeneratorLabel();

    /// <summary>
    /// On object into which IL instructions are generated.
    /// </summary>
    ILGenerator generator;

    /// <summary>
    /// An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.
    /// </summary>
    protected IMetadataHost host;

    /// <summary>
    /// A map from source label name unique keys to ILGenerator labels.
    /// </summary>
    Dictionary<int, ILGeneratorLabel> labelFor = new Dictionary<int, ILGeneratorLabel>();

    /// <summary>
    /// True if the last generated statement tranferred control unconditionally, and hence an instruction at the current location can only
    /// be reached via a branch.
    /// </summary>
    bool lastStatementWasUnconditionalTransfer;

    /// <summary>
    /// A map from ILocalDefinition instances to indices that can be used in IL instructions referring to locals.
    /// </summary>
    Dictionary<ILocalDefinition, ushort> localIndex = new Dictionary<ILocalDefinition, ushort>();

    /// <summary>
    /// The method whose CodeModel body is being converted to IL instructions.
    /// </summary>
    IMethodDefinition method;

    /// <summary>
    /// If true, code generation emphasizes small code size over patterns that make debugging better.
    /// </summary>
    bool minizeCodeSize;

    /// <summary>
    /// A map from labels (and labeled statements) to the most nested try catch that contains them. If a branch is
    /// enountered to a label and the current try catch is not the one that contains the label, then the branch leaves
    /// the current try catch block (and thus must become a leave instruction).
    /// </summary>
    Dictionary<object, IStatement> mostNestedTryCatchFor = new Dictionary<object, IStatement>();

    /// <summary>
    /// A local (temporary) that holds the return value until control reaches the end of the method body.
    /// Only used if minimizeCodeSize is false or if the return statement is inside a try catch.
    /// </summary>
    ILocalDefinition/*?*/ returnLocal;

    /// <summary>
    /// An object that can map the ILocation objects found in the block of statements to IPrimarySourceLocation objects.
    /// </summary>
    protected ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// If true, the generated IL keeps track of the source locations of expressions, not just statements.
    /// </summary>
    private bool trackExpressionSourceLocations;

    /// <summary>
    /// A list of all the local variable definitions that were encountered during the translation to IL.
    /// </summary>
    List<ILocalDefinition> localVariables = new List<ILocalDefinition>();

    /// <summary>
    /// A map that indicates how many iterator locals are present in a given block. Only useful for generated MoveNext methods. May be null.
    /// </summary>
    IDictionary<IBlockStatement, uint>/*?*/ iteratorLocalCount;

    /// <summary>
    /// Report the current expression to the IL generator. The source locations of the current expression will be wrapped up
    /// as an IExpressionSourceLocation object and stored in the Location property of the next IL instruction that is created by the IL generator.
    /// </summary>
    private void EmitSourceLocation(IExpression expression) {
      Contract.Requires(expression != null);
      if (this.sourceLocationProvider == null || !this.trackExpressionSourceLocations) return;
      foreach (var location in expression.Locations) {
        Contract.Assume(location != null);
        foreach (IPrimarySourceLocation sloc in this.sourceLocationProvider.GetPrimarySourceLocationsFor(location)) {
          Contract.Assume(sloc != null);
          this.generator.MarkExpressionLocation(new ExpressionSourceLocation(sloc)); //hide the location from the PDB writer.
          return;
        }
      }
    }

    /// <summary>
    /// Translates the parameter list position of the given parameter to an IL parameter index. In other words,
    /// it adds 1 to the parameterDefinition.Index value if the containing method has an implicit this parameter.
    /// </summary>
    private static ushort GetParameterIndex(IParameterDefinition parameterDefinition) {
      ushort parameterIndex = parameterDefinition.Index;
      if (!parameterDefinition.ContainingSignature.IsStatic) parameterIndex++;
      return parameterIndex;
    }

    private void LoadAddressOf(object container, IExpression/*?*/ instance) {
      this.LoadAddressOf(container, instance, false);
    }

    private void LoadAddressOf(object container, IExpression/*?*/ instance, bool emitReadonlyPrefix) {
      ILocalDefinition/*?*/ local = container as ILocalDefinition;
      if (local != null) {
        ushort localIndex = this.GetLocalIndex(local);
        if (localIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Ldloca_S, local);
        else this.generator.Emit(OperationCode.Ldloca, local);
        this.StackSize++;
        return;
      }
      IParameterDefinition/*?*/ parameter = container as IParameterDefinition;
      if (parameter != null) {
        ushort parIndex = GetParameterIndex(parameter);
        if (parIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Ldarga_S, parameter);
        else this.generator.Emit(OperationCode.Ldarga, parameter);
        this.StackSize++;
        return;
      }
      IFieldReference/*?*/ field = container as IFieldReference;
      if (field != null) {
        if (instance == null) {
          this.generator.Emit(OperationCode.Ldsflda, field);
          this.StackSize++;
        } else {
          this.Traverse(instance);
          this.generator.Emit(OperationCode.Ldflda, field);
        }
        return;
      }
      IArrayIndexer/*?*/ arrayIndexer = container as IArrayIndexer;
      if (arrayIndexer != null) {
        this.Traverse(arrayIndexer.IndexedObject);
        this.Traverse(arrayIndexer.Indices);
        if (emitReadonlyPrefix)
          this.generator.Emit(OperationCode.Readonly_);
        IArrayTypeReference arrayType = (IArrayTypeReference)arrayIndexer.IndexedObject.Type;
        if (arrayType.IsVector) {
          this.generator.Emit(OperationCode.Ldelema, arrayType.ElementType);
          this.StackSize--;
        } else {
          this.generator.Emit(OperationCode.Array_Addr, arrayType);
          this.StackSize -= (ushort)IteratorHelper.EnumerableCount(arrayIndexer.Indices);
        }
        return;
      }
      IAddressDereference/*?*/ addressDereference = container as IAddressDereference;
      if (addressDereference != null) {
        this.Traverse(addressDereference.Address);
        this.StackSize++;
        return;
      }
      IMethodReference/*?*/ method = container as IMethodReference;
      if (method != null) {
        if (instance != null) {
          this.Traverse(instance);
          this.generator.Emit(OperationCode.Ldvirtftn, method);
        } else
          this.generator.Emit(OperationCode.Ldftn, method);
        this.StackSize++;
        return;
      }
      IThisReference/*?*/ thisParameter = container as IThisReference;
      if (thisParameter != null) {
        this.generator.Emit(OperationCode.Ldarg_0);
        this.StackSize++;
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
      IAddressOf addressOfExpression = container as IAddressOf;
      if (addressOfExpression != null) {
        this.LoadAddressOf(addressOfExpression.Expression, addressOfExpression);
        return;
      }
      IExpression/*?*/ expression = container as IExpression;
      if (expression != null) {
        TemporaryVariable temp = new TemporaryVariable(expression.Type, this.method);
        this.Traverse(expression);
        this.VisitAssignmentTo(temp);
        this.LoadAddressOf(temp, null);
        return;
      }
      Contract.Assume(false);
    }

    private void LoadField(byte alignment, bool isVolatile, IExpression/*?*/ instance, IFieldReference field, bool fieldIsStatic) {
      if (instance == null) {
        if (alignment != 0)
          this.generator.Emit(OperationCode.Unaligned_, alignment);
        if (isVolatile)
          this.generator.Emit(OperationCode.Volatile_);
        if (fieldIsStatic) {
          this.generator.Emit(OperationCode.Ldsfld, field);
          this.StackSize++;
        } else
          //The caller has already generated code to load the instance on the stack.
          this.generator.Emit(OperationCode.Ldfld, field);
      } else {
        this.Traverse(instance);
        if (alignment != 0)
          this.generator.Emit(OperationCode.Unaligned_, alignment);
        if (isVolatile)
          this.generator.Emit(OperationCode.Volatile_);
        this.generator.Emit(OperationCode.Ldfld, field);
      }
    }

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

    /// <summary>
    /// If true, code generation emphasizes small code size over patterns that make debugging better.
    /// </summary>
    public bool MinimizeCodeSize {
      get { return this.minizeCodeSize; }
      set { this.minizeCodeSize = value; }
    }

    ushort StackSize {
      get { return this._stackSize; }
      set {
        this._stackSize = value; if (value > this.maximumStackSizeNeeded) maximumStackSizeNeeded = value;
        Contract.Assume(value <= ushort.MaxValue-20, "Probable stack underflow");
      }
    }
    ushort _stackSize;

    /// <summary>
    /// If true, the generated IL keeps track of the source locations of expressions, not just statements.
    /// </summary>
    public bool TrackExpressionSourceLocations {
      get { return this.trackExpressionSourceLocations; }
      set { this.trackExpressionSourceLocations = value; }
    }

    /// <summary>
    /// Generates IL for the specified addition.
    /// </summary>
    /// <param name="addition">The addition.</param>
    public override void TraverseChildren(IAddition addition) {
      var targetExpression = addition.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, addition, (IExpression e) => this.TraverseAdditionRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: addition.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(addition.LeftOperand);
        this.TraverseAdditionRightOperandAndDoOperation(addition);
      }
    }

    private void TraverseAdditionRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IAddition);
      var addition = (IAddition)expression;
      this.Traverse(addition.RightOperand);
      OperationCode operationCode = OperationCode.Add;
      if (addition.CheckOverflow) {
        if (TypeHelper.IsSignedPrimitive(addition.Type))
          operationCode = OperationCode.Add_Ovf;
        else if (TypeHelper.IsUnsignedPrimitive(addition.Type))
          operationCode = OperationCode.Add_Ovf_Un;
        else if (addition.Type.TypeCode == PrimitiveTypeCode.Pointer
          || addition.Type.TypeCode == PrimitiveTypeCode.Reference) {
          if (TypeHelper.IsSignedPrimitive(addition.LeftOperand.Type) ||
            TypeHelper.IsSignedPrimitive(addition.RightOperand.Type))
            operationCode = OperationCode.Add_Ovf;
          else
            operationCode = OperationCode.Add_Ovf_Un;
        }
      }
      this.EmitSourceLocation(addition);
      this.generator.Emit(operationCode);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    public override void TraverseChildren(IAddressableExpression addressableExpression) {
      Contract.Assume(false, "The expression containing this as a subexpression should never allow a call to this routine.");
    }

    /// <summary>
    /// Generates IL for the specified address dereference.
    /// </summary>
    /// <param name="addressDereference">The address dereference.</param>
    public override void TraverseChildren(IAddressDereference addressDereference) {
      this.Traverse(addressDereference.Address);
      if (addressDereference.IsUnaligned)
        this.generator.Emit(OperationCode.Unaligned_, addressDereference.Alignment);
      if (addressDereference.IsVolatile)
        this.generator.Emit(OperationCode.Volatile_);
      this.LoadIndirect(addressDereference.Type);
    }

    private void LoadIndirect(ITypeReference targetType) {
      Contract.Requires(targetType != null);

      OperationCode opcode;
      switch (targetType.TypeCode) {
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
        case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Ldind_I; break;
        default:
          var ptr = targetType as IPointerTypeReference;
          if (ptr != null) {
            opcode = OperationCode.Ldind_I; break;
          } else {
            var mgdPtr = targetType as IManagedPointerTypeReference;
            if (mgdPtr != null) {
              opcode = OperationCode.Ldind_I; break;
            }
          }
          //If type is a reference type, then Ldobj is equivalent to Lind_Ref, but the instruction is larger, so try to avoid it.
          if (targetType.IsValueType || targetType is IGenericParameterReference) {
            this.generator.Emit(OperationCode.Ldobj, targetType);
            return;
          }
          opcode = OperationCode.Ldind_Ref; break;
      }
      this.generator.Emit(opcode);
    }

    /// <summary>
    /// Generates IL for the specified address of.
    /// </summary>
    /// <param name="addressOf">The address of.</param>
    public override void TraverseChildren(IAddressOf addressOf) {
      object container = addressOf.Expression.Definition;
      IExpression/*?*/ instance = addressOf.Expression.Instance;
      this.LoadAddressOf(container, instance, addressOf.ObjectControlsMutability);
    }

    /// <summary>
    /// Generates IL for the specified anonymous delegate.
    /// </summary>
    /// <param name="anonymousDelegate">The anonymous delegate.</param>
    public override void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      Contract.Assume(false, "IAnonymousDelegate nodes must be replaced before trying to convert the Code Model to IL.");
    }

    /// <summary>
    /// Generates IL for the specified array indexer.
    /// </summary>
    /// <param name="arrayIndexer">The array indexer.</param>
    public override void TraverseChildren(IArrayIndexer arrayIndexer) {
      this.Traverse(arrayIndexer.IndexedObject);
      this.Traverse(arrayIndexer.Indices);
      this.EmitSourceLocation(arrayIndexer);
      IArrayTypeReference arrayType = (IArrayTypeReference)arrayIndexer.IndexedObject.Type;
      if (arrayType.IsVector)
        this.LoadVectorElement(arrayType.ElementType);
      else {
        this.generator.Emit(OperationCode.Array_Get, arrayIndexer.IndexedObject.Type);
        this.StackSize -= (ushort)IteratorHelper.EnumerableCount(arrayIndexer.Indices);
      }
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
        case PrimitiveTypeCode.UInt16: opcode = OperationCode.Ldelem_U2; break;
        case PrimitiveTypeCode.UInt32: opcode = OperationCode.Ldelem_U4; break;
        case PrimitiveTypeCode.UInt64: opcode = OperationCode.Ldelem_I8; break;
        case PrimitiveTypeCode.UInt8: opcode = OperationCode.Ldelem_U1; break;
        case PrimitiveTypeCode.UIntPtr: opcode = OperationCode.Ldelem_I; break;
        default:
          if (typeReference.IsValueType || typeReference is IGenericParameterReference) {
            this.generator.Emit(OperationCode.Ldelem, typeReference);
            this.StackSize--;
            return;
          }
          opcode = OperationCode.Ldelem_Ref; break;
      }
      this.generator.Emit(opcode);
      this.StackSize--;
    }

    /// <summary>
    /// Throws an exception when executed: IAssertStatement nodes
    /// must be replaced before converting the Code Model to IL.
    /// </summary>
    public override void TraverseChildren(IAssertStatement assertStatement) {
      Contract.Assume(false, "IAssertStatement nodes must be replaced before trying to convert the Code Model to IL.");
    }

    /// <summary>
    /// Generates IL for the specified assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    public override void TraverseChildren(IAssignment assignment) {
      this.VisitAssignment(assignment, false);
    }

    /// <summary>
    /// Generates IL for the assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    /// <param name="treatAsStatement">if set to <c>true</c> [treat as statement].</param>
    public virtual void VisitAssignment(IAssignment assignment, bool treatAsStatement) {
      var target = assignment.Target;
      this.VisitAssignment(assignment.Target, assignment.Source, (IExpression e) => this.Traverse(e), treatAsStatement, pushTargetRValue: false, resultIsInitialTargetRValue: false);
    }

    internal delegate void SourceTraverser(IExpression source);

    private void VisitAssignment(ITargetExpression target, IExpression source, SourceTraverser sourceTraverser,
      bool treatAsStatement, bool pushTargetRValue, bool resultIsInitialTargetRValue) {
      Contract.Requires(target != null);
      Contract.Requires(source != null);
      Contract.Requires(sourceTraverser != null);
      Contract.Requires(!resultIsInitialTargetRValue || pushTargetRValue);
      Contract.Requires(!pushTargetRValue || source is IBinaryOperation);

      object container = target.Definition;
      ILocalDefinition/*?*/ local = container as ILocalDefinition;
      if (local != null) {
        if (source is IDefaultValue && !local.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(local, null);
          this.generator.Emit(OperationCode.Initobj, local.Type);
          this.StackSize--;
          if (!treatAsStatement) this.LoadLocal(local);
        } else {
          if (pushTargetRValue) {
            this.LoadLocal(local);
            if (!treatAsStatement && resultIsInitialTargetRValue) {
              this.generator.Emit(OperationCode.Dup);
              this.StackSize++;
            }
          }
          sourceTraverser(source);
          if (!treatAsStatement && !resultIsInitialTargetRValue) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          this.VisitAssignmentTo(local);
        }
        return;
      }
      IParameterDefinition/*?*/ parameter = container as IParameterDefinition;
      if (parameter != null) {
        if (source is IDefaultValue && !parameter.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(parameter, null);
          this.generator.Emit(OperationCode.Initobj, parameter.Type);
          this.StackSize--;
          if (!treatAsStatement) this.LoadParameter(parameter);
        } else {
          if (pushTargetRValue) {
            this.LoadParameter(parameter);
            if (!treatAsStatement && resultIsInitialTargetRValue) {
              this.generator.Emit(OperationCode.Dup);
              this.StackSize++;
            }
          }
          sourceTraverser(source);
          if (!treatAsStatement && !resultIsInitialTargetRValue) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          ushort parIndex = GetParameterIndex(parameter);
          if (parIndex <= byte.MaxValue) this.generator.Emit(OperationCode.Starg_S, parameter);
          else this.generator.Emit(OperationCode.Starg, parameter);
          this.StackSize--;
        }
        return;
      }
      IFieldReference/*?*/ field = container as IFieldReference;
      if (field != null) {
        if (source is IDefaultValue && !field.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(field, target.Instance);
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
          ILocalDefinition/*?*/ temp = null;
          if (target.Instance != null) {
            this.Traverse(target.Instance);
            if (pushTargetRValue) {
              this.generator.Emit(OperationCode.Dup);
              this.StackSize++;
            }
          }
          if (pushTargetRValue) {
            if (target.Instance != null)
              this.generator.Emit(OperationCode.Ldfld, field);
            else {
              this.generator.Emit(OperationCode.Ldsfld, field);
              this.StackSize++;
            }
            if (!treatAsStatement && resultIsInitialTargetRValue) {
              this.generator.Emit(OperationCode.Dup);
              this.StackSize++;
              temp = new TemporaryVariable(source.Type, this.method);
              this.VisitAssignmentTo(temp);
            }
          }
          
          sourceTraverser(source);
          if (!treatAsStatement && !resultIsInitialTargetRValue) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
            temp = new TemporaryVariable(source.Type, this.method);
            this.VisitAssignmentTo(temp);
          }
          if (target.IsUnaligned)
            this.generator.Emit(OperationCode.Unaligned_, target.Alignment);
          if (target.IsVolatile)
            this.generator.Emit(OperationCode.Volatile_);
          if (target.Instance == null) {
            this.generator.Emit(OperationCode.Stsfld, field);
            this.StackSize--;
          } else {
            this.generator.Emit(OperationCode.Stfld, field);
            this.StackSize-=2;
          }
          if (temp != null) this.LoadLocal(temp);
        }
        return;
      }
      IArrayIndexer/*?*/ arrayIndexer = container as IArrayIndexer;
      if (arrayIndexer != null) {
        if (source is IDefaultValue && !arrayIndexer.Type.ResolvedType.IsReferenceType) {
          this.LoadAddressOf(arrayIndexer, target.Instance);
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
          ILocalDefinition/*?*/ temp = null;
          IArrayTypeReference arrayType = (IArrayTypeReference)target.Instance.Type;
          this.Traverse(target.Instance);
          this.Traverse(arrayIndexer.Indices);
          if (pushTargetRValue) {
            if (arrayType.IsVector)
              this.generator.Emit(OperationCode.Ldelema);
            else
              this.generator.Emit(OperationCode.Array_Addr, arrayType);
            this.generator.Emit(OperationCode.Dup); this.StackSize++;
            this.LoadIndirect(arrayType.ElementType);
            if (!treatAsStatement && resultIsInitialTargetRValue) {
              this.generator.Emit(OperationCode.Dup);
              this.StackSize++;
              temp = new TemporaryVariable(source.Type, this.method);
              this.VisitAssignmentTo(temp);
            }
          }
          sourceTraverser(source);
          if (!treatAsStatement && !resultIsInitialTargetRValue) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
            temp = new TemporaryVariable(source.Type, this.method);
            this.VisitAssignmentTo(temp);
          }
          if (pushTargetRValue) {
            this.StoreIndirect(arrayType.ElementType);
          } else {
            if (arrayType.IsVector)
              this.StoreVectorElement(arrayType.ElementType);
            else {
              this.generator.Emit(OperationCode.Array_Set, arrayType);
              this.StackSize-=(ushort)(IteratorHelper.EnumerableCount(arrayIndexer.Indices)+2);
            }
          }
          if (temp != null) this.LoadLocal(temp);
        }
        return;
      }
      IAddressDereference/*?*/ addressDereference = container as IAddressDereference;
      if (addressDereference != null) {
        this.Traverse(addressDereference.Address);
        if (source is IDefaultValue && !addressDereference.Type.ResolvedType.IsReferenceType) {
          if (!treatAsStatement) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          this.generator.Emit(OperationCode.Initobj, addressDereference.Type);
          if (!treatAsStatement)
            this.generator.Emit(OperationCode.Ldobj, addressDereference.Type);
          else
            this.StackSize--;
        } else if (source is IAddressDereference) {
          if (!treatAsStatement) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
          }
          this.Traverse(((IAddressDereference)source).Address);
          this.generator.Emit(OperationCode.Cpobj, addressDereference.Type);
          this.StackSize-=2;
          if (!treatAsStatement)
            this.generator.Emit(OperationCode.Ldobj, addressDereference.Type);
        } else {
          ILocalDefinition/*?*/ temp = null;
          if (pushTargetRValue) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
            if (addressDereference.IsUnaligned)
              this.generator.Emit(OperationCode.Unaligned_, addressDereference.Alignment);
            if (addressDereference.IsVolatile)
              this.generator.Emit(OperationCode.Volatile_);
            this.LoadIndirect(addressDereference.Type);
            if (!treatAsStatement && resultIsInitialTargetRValue) {
              this.generator.Emit(OperationCode.Dup);
              this.StackSize++;
              temp = new TemporaryVariable(source.Type, this.method);
              this.VisitAssignmentTo(temp);
            }
          }
          sourceTraverser(source);
          if (!treatAsStatement && !resultIsInitialTargetRValue) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
            temp = new TemporaryVariable(source.Type, this.method);
            this.VisitAssignmentTo(temp);
          }
          this.VisitAssignmentTo(addressDereference);
          if (temp != null) this.LoadLocal(temp);
        }
        return;
      }
      IPropertyDefinition/*?*/ propertyDefinition = container as IPropertyDefinition;
      if (propertyDefinition != null) {
        Contract.Assume(propertyDefinition.Getter != null && propertyDefinition.Setter != null);
        if (!propertyDefinition.IsStatic) {
          this.Traverse(target.Instance);
        }
        ILocalDefinition temp = null;
        if (pushTargetRValue) {
          if (!propertyDefinition.IsStatic) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
            this.generator.Emit(target.GetterIsVirtual ? OperationCode.Callvirt : OperationCode.Call, propertyDefinition.Getter);
          } else {
            this.generator.Emit(OperationCode.Call, propertyDefinition.Getter);
            this.StackSize++;
          }
          if (!treatAsStatement && resultIsInitialTargetRValue) {
            this.generator.Emit(OperationCode.Dup);
            this.StackSize++;
            temp = new TemporaryVariable(source.Type, this.method);
            this.VisitAssignmentTo(temp);
          }
        }
        sourceTraverser(source);
        if (!treatAsStatement && !resultIsInitialTargetRValue) {
          this.generator.Emit(OperationCode.Dup);
          this.StackSize++;
          temp = new TemporaryVariable(propertyDefinition.Type, this.method);
          this.VisitAssignmentTo(temp);
        }
        if (!propertyDefinition.IsStatic) {
          this.generator.Emit(target.SetterIsVirtual ? OperationCode.Callvirt : OperationCode.Call, propertyDefinition.Setter);
          this.StackSize -= 2;
        } else {
          this.generator.Emit(OperationCode.Call, propertyDefinition.Setter);
          this.StackSize--;
        }
        if (temp != null) this.LoadLocal(temp);
        return;
      }
      Contract.Assume(false);
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
          if (elementTypeReference.IsValueType || elementTypeReference is IGenericParameterReference) {
            this.generator.Emit(OperationCode.Stelem, elementTypeReference);
            this.StackSize -= 3;
            return;
          }
          opcode = OperationCode.Stelem_Ref; break;
      }
      this.generator.Emit(opcode);
      this.StackSize -= 3;
    }

    private void VisitAssignmentTo(IAddressDereference addressDereference) {
      if (addressDereference.IsUnaligned)
        this.generator.Emit(OperationCode.Unaligned_, addressDereference.Alignment);
      if (addressDereference.IsVolatile)
        this.generator.Emit(OperationCode.Volatile_);
      this.StoreIndirect(addressDereference.Type);
    }

    private void StoreIndirect(ITypeReference targetType) {
      OperationCode opcode;
      switch (targetType.TypeCode) {
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
          if (targetType.IsValueType || targetType is IGenericParameterReference) {
            this.generator.Emit(OperationCode.Stobj, targetType);
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
    /// Throws an exception when executed: IAssumeStatement nodes
    /// must be replaced before converting the Code Model to IL.
    /// </summary>
    public override void TraverseChildren(IAssumeStatement assumeStatement) {
      Contract.Assume(false, "IAssumeStatement nodes must be replaced before trying to convert the Code Model to IL.");
    }

    /// <summary>
    /// Generates IL for the specified bitwise and.
    /// </summary>
    /// <param name="bitwiseAnd">The bitwise and.</param>
    public override void TraverseChildren(IBitwiseAnd bitwiseAnd) {
      var targetExpression = bitwiseAnd.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, bitwiseAnd, (IExpression e) => this.TraverseBitwiseAndRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: bitwiseAnd.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(bitwiseAnd.LeftOperand);
        this.TraverseBitwiseAndRightOperandAndDoOperation(bitwiseAnd);
      }
    }

    private void TraverseBitwiseAndRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IBitwiseAnd);
      var bitwiseAnd = (IBitwiseAnd)expression;
      this.Traverse(bitwiseAnd.RightOperand);
      this.EmitSourceLocation(bitwiseAnd);
      this.generator.Emit(OperationCode.And);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified bitwise or.
    /// </summary>
    /// <param name="bitwiseOr">The bitwise or.</param>
    public override void TraverseChildren(IBitwiseOr bitwiseOr) {
      var targetExpression = bitwiseOr.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, bitwiseOr, (IExpression e) => this.TraverseBitwiseOrRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: bitwiseOr.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(bitwiseOr.LeftOperand);
        this.TraverseBitwiseOrRightOperandAndDoOperation(bitwiseOr);
      }
    }

    private void TraverseBitwiseOrRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IBitwiseOr);
      var bitwiseOr = (IBitwiseOr)expression;
      this.Traverse(bitwiseOr.RightOperand);
      this.EmitSourceLocation(bitwiseOr);
      this.generator.Emit(OperationCode.Or);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified block expression.
    /// </summary>
    /// <param name="blockExpression">The block expression.</param>
    public override void TraverseChildren(IBlockExpression blockExpression) {
      uint numberOfIteratorLocals = 0;
      if (this.iteratorLocalCount != null)
        this.iteratorLocalCount.TryGetValue(blockExpression.BlockStatement, out numberOfIteratorLocals);
      this.generator.BeginScope(numberOfIteratorLocals);
      this.Traverse(blockExpression.BlockStatement.Statements);
      this.Traverse(blockExpression.Expression);
      this.generator.EndScope();
    }

    /// <summary>
    /// Generates IL for the specified block.
    /// </summary>
    /// <param name="block">The block.</param>
    public override void TraverseChildren(IBlockStatement block) {
      uint numberOfIteratorLocals = 0;
      if (this.iteratorLocalCount != null)
        this.iteratorLocalCount.TryGetValue(block, out numberOfIteratorLocals);
      this.generator.BeginScope(numberOfIteratorLocals);
      this.Traverse(block.Statements);
      this.generator.EndScope();
    }

    /// <summary>
    /// Performs some computation with the given bound expression.
    /// </summary>
    /// <param name="boundExpression"></param>
    public override void TraverseChildren(IBoundExpression boundExpression) {
      //this.EmitSourceLocation(boundExpression);
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
        this.LoadField(boundExpression.IsUnaligned ? boundExpression.Alignment : (byte)0, boundExpression.IsVolatile, boundExpression.Instance, field, boundExpression.Instance == null);
        return;
      }
      Contract.Assert(false);
    }

    /// <summary>
    /// Generates IL for the specified break statement.
    /// </summary>
    /// <param name="breakStatement">The break statement.</param>
    public override void TraverseChildren(IBreakStatement breakStatement) {
      this.EmitSequencePoint(breakStatement.Locations);
      if (this.LabelIsOutsideCurrentExceptionBlock(this.currentBreakTarget))
        this.generator.Emit(OperationCode.Leave, this.currentBreakTarget);
      else
        this.generator.Emit(OperationCode.Br, this.currentBreakTarget);
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Generates IL for the specified cast if possible.
    /// </summary>
    /// <param name="castIfPossible">The cast if possible.</param>
    public override void TraverseChildren(ICastIfPossible castIfPossible) {
      this.Traverse(castIfPossible.ValueToCast);
      this.EmitSourceLocation(castIfPossible);
      this.generator.Emit(OperationCode.Isinst, castIfPossible.TargetType);
    }

    /// <summary>
    /// Generates IL for the specified catch clause.
    /// </summary>
    /// <param name="catchClause">The catch clause.</param>
    public override void TraverseChildren(ICatchClause catchClause) {
      this.generator.BeginScope(0);
      this.StackSize++;
      if (catchClause.FilterCondition != null) {
        this.generator.BeginFilterBlock();
        this.Traverse(catchClause.FilterCondition);
        this.generator.BeginFilterBody();
      } else {
        this.generator.BeginCatchBlock(catchClause.ExceptionType);
      }
      if (!(catchClause.ExceptionContainer is Dummy)) {
        this.generator.AddVariableToCurrentScope(catchClause.ExceptionContainer);
        this.VisitAssignmentTo(catchClause.ExceptionContainer);
      } else if (catchClause.FilterCondition == null) {
        this.generator.Emit(OperationCode.Pop);
        this.StackSize--;
      }
      this.lastStatementWasUnconditionalTransfer = false;
      this.Traverse(catchClause.Body);
      if (!this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Leave, this.currentTryCatchFinallyEnd);
      this.generator.EndScope();
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified check if instance.
    /// </summary>
    /// <param name="checkIfInstance">The check if instance.</param>
    public override void TraverseChildren(ICheckIfInstance checkIfInstance) {
      this.Traverse(checkIfInstance.Operand);
      this.EmitSourceLocation(checkIfInstance);
      this.generator.Emit(OperationCode.Isinst, checkIfInstance.TypeToCheck);
      this.generator.Emit(OperationCode.Ldnull);
      this.StackSize++;
      this.generator.Emit(OperationCode.Cgt_Un);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified constant.
    /// </summary>
    /// <param name="constant">The constant.</param>
    public override void TraverseChildren(ICompileTimeConstant constant) {
      var ic = constant.Value as IConvertible;
      if (ic != null)
        this.EmitConstant(ic);
      else {
        var tc = constant.Type.TypeCode;
        if (tc == PrimitiveTypeCode.IntPtr) {
          this.EmitConstant(((IntPtr)constant.Value).ToInt64());
          this.generator.Emit(OperationCode.Conv_Ovf_I);
        } else if (tc == PrimitiveTypeCode.UIntPtr) {
          this.EmitConstant(((UIntPtr)constant.Value).ToUInt64());
          this.generator.Emit(OperationCode.Conv_Ovf_U);
        } else {
          this.generator.Emit(OperationCode.Ldnull);
          this.StackSize++;
        }
      }
    }

    /// <summary>
    /// Generates IL for the specified conditional.
    /// </summary>
    /// <param name="conditional">The conditional.</param>
    public override void TraverseChildren(IConditional conditional) {
      ILGeneratorLabel falseCase = new ILGeneratorLabel();
      ILGeneratorLabel endif = new ILGeneratorLabel();
      this.VisitBranchIfFalse(conditional.Condition, falseCase);
      this.Traverse(conditional.ResultIfTrue);
      this.generator.Emit(OperationCode.Br, endif);
      this.generator.MarkLabel(falseCase);
      this.StackSize--;
      this.Traverse(conditional.ResultIfFalse);
      this.generator.MarkLabel(endif);
    }

    /// <summary>
    /// Generates IL for the specified conditional statement.
    /// </summary>
    /// <param name="conditionalStatement">The conditional statement.</param>
    public override void TraverseChildren(IConditionalStatement conditionalStatement) {
      if (IteratorHelper.EnumerableIsNotEmpty(conditionalStatement.Condition.Locations))
        this.EmitSequencePoint(conditionalStatement.Condition.Locations);
      else
        this.EmitSequencePoint(conditionalStatement.Locations);
      ILGeneratorLabel/*?*/ endif = null;
      var trueBranchDelta = 0;
      ushort stackSizeAfterCondition = 0;
      if (conditionalStatement.TrueBranch is IBreakStatement && !this.LabelIsOutsideCurrentExceptionBlock(this.currentBreakTarget)) {
        this.VisitBranchIfTrue(conditionalStatement.Condition, this.currentBreakTarget);
        stackSizeAfterCondition = this.StackSize;
      } else if (conditionalStatement.TrueBranch is IContinueStatement && !this.LabelIsOutsideCurrentExceptionBlock(this.currentContinueTarget)) {
        this.VisitBranchIfTrue(conditionalStatement.Condition, this.currentContinueTarget);
        stackSizeAfterCondition = this.StackSize;
      } else {
        ILGeneratorLabel falseCase = new ILGeneratorLabel();
        this.VisitBranchIfFalse(conditionalStatement.Condition, falseCase);
        stackSizeAfterCondition = this.StackSize;
        this.Traverse(conditionalStatement.TrueBranch);
        trueBranchDelta = this.StackSize - stackSizeAfterCondition;
        this.StackSize = stackSizeAfterCondition;
        if (!this.lastStatementWasUnconditionalTransfer) {
          endif = new ILGeneratorLabel();
          this.generator.Emit(OperationCode.Br, endif);
        } else {
        }
        this.generator.MarkLabel(falseCase);
      }
      var beginningOfFalseBranch = this.StackSize;
      this.Traverse(conditionalStatement.FalseBranch);
      var falseBranchDelta = this.StackSize - beginningOfFalseBranch;

      if (trueBranchDelta != falseBranchDelta) {
        //
        // Put a breakpoint here to find (potential) bugs in the decompiler and/or this traverser's
        // tracking of the stack size. However, it cannot be enforced because when structured code
        // is not completely decompiled, the resulting explicit stack instructions cannot be tracked
        // accurately by this traverser. (Unstructured source code can also lead to this situation.)
        //
        // For instance, the following will result in both pushes being counted, but the stack size
        // should increase only by one.
        //
        // if (c) goto L1;
        // push e;
        // goto L2;
        // L1:
        // push f;
        // L2:
        // an expression containing a pop value
      }

      this.StackSize = (ushort)(stackSizeAfterCondition + Math.Max(trueBranchDelta, falseBranchDelta));

      if (endif != null) {
        this.generator.MarkLabel(endif);
        this.lastStatementWasUnconditionalTransfer = false;
      }
    }

    internal bool LabelIsOutsideCurrentExceptionBlock(ILGeneratorLabel label) {
      IStatement tryCatchContainingTarget = null;
      this.mostNestedTryCatchFor.TryGetValue(label, out tryCatchContainingTarget);
      return this.currentTryCatch != tryCatchContainingTarget;
    }

    /// <summary>
    /// Generates IL for the specified continue statement.
    /// </summary>
    /// <param name="continueStatement">The continue statement.</param>
    public override void TraverseChildren(IContinueStatement continueStatement) {
      this.EmitSequencePoint(continueStatement.Locations);
      if (this.LabelIsOutsideCurrentExceptionBlock(this.currentContinueTarget))
        this.generator.Emit(OperationCode.Leave, this.currentContinueTarget);
      else
        this.generator.Emit(OperationCode.Br, this.currentContinueTarget);
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Generates IL for the specified copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement">The copy memory statement.</param>
    public override void TraverseChildren(ICopyMemoryStatement copyMemoryStatement) {
      this.EmitSequencePoint(copyMemoryStatement.Locations);
      this.Traverse(copyMemoryStatement.TargetAddress);
      this.Traverse(copyMemoryStatement.SourceAddress);
      this.Traverse(copyMemoryStatement.NumberOfBytesToCopy);
      this.generator.Emit(OperationCode.Cpblk);
    }

    /// <summary>
    /// Generates IL for the specified conversion.
    /// </summary>
    /// <param name="conversion">The conversion.</param>
    public override void TraverseChildren(IConversion conversion) {
      this.Traverse(conversion.ValueToConvert);
      //TODO: change IConversion to make it illegal to convert to or from enum types.
      ITypeReference sourceType = conversion.ValueToConvert.Type;
      ITypeReference targetType = conversion.Type;
      // The code model represents the implicit conversion of "null" to a reference type
      // with an explicit conversion. But that does not need to be present in the IL.
      // There it can just be implicit.
      var ctc = conversion.ValueToConvert as ICompileTimeConstant;
      if (ctc != null && ctc.Value == null && targetType.TypeCode == PrimitiveTypeCode.NotPrimitive && TypeHelper.TypesAreEquivalent(sourceType, this.host.PlatformType.SystemObject))
        return;
      if (sourceType.ResolvedType.IsEnum && !TypeHelper.TypesAreEquivalent(targetType, this.host.PlatformType.SystemObject))
        sourceType = sourceType.ResolvedType.UnderlyingType;
      if (targetType.ResolvedType.IsEnum) targetType = targetType.ResolvedType.UnderlyingType;
      if (sourceType is Dummy) sourceType = targetType;
      if (targetType is Dummy) targetType = sourceType;
      if (TypeHelper.TypesAreEquivalent(sourceType, targetType)) return;
      if (conversion.CheckNumericRange)
        this.VisitCheckedConversion(sourceType, targetType);
      else
        this.VisitUncheckedConversion(sourceType, targetType);
    }

    /// <summary>
    /// Generates IL for the specified create array.
    /// </summary>
    /// <param name="createArray">The create array instance to visit.</param>
    public override void TraverseChildren(ICreateArray createArray) {
      IEnumerator<int> bounds = createArray.LowerBounds.GetEnumerator();
      bool hasOneOrMoreBounds = bounds.MoveNext();
      bool hasMoreBounds = hasOneOrMoreBounds;
      uint boundsEmitted = 0;
      //
      // For the case of rank > 1 the lower bounds and sizes are interleaved on the stack
      //
      foreach (IExpression size in createArray.Sizes) {
        // First the lower bound, if any
        if (hasOneOrMoreBounds) {
          if (hasMoreBounds) {
            this.EmitConstant(bounds.Current);
            hasMoreBounds = bounds.MoveNext();
          } else {
            this.generator.Emit(OperationCode.Ldc_I4_0);
            this.StackSize++;
          }
          boundsEmitted++;
        }
        this.Traverse(size);
        if (size.Type.TypeCode == PrimitiveTypeCode.Int64 || size.Type.TypeCode == PrimitiveTypeCode.UInt64)
          this.generator.Emit(OperationCode.Conv_Ovf_U);
      }
      IArrayTypeReference arrayType;
      OperationCode create;
      if (hasOneOrMoreBounds) {
        create = OperationCode.Array_Create_WithLowerBound;
        arrayType = Matrix.GetMatrix(createArray.ElementType, createArray.Rank, createArray.LowerBounds, ((IMetadataCreateArray)createArray).Sizes, this.host.InternFactory);
      } else if (createArray.Rank > 1) {
        create = OperationCode.Array_Create;
        arrayType = Matrix.GetMatrix(createArray.ElementType, createArray.Rank, this.host.InternFactory);
      } else {
        create = OperationCode.Newarr;
        arrayType = Vector.GetVector(createArray.ElementType, this.host.InternFactory);
      }
      this.EmitSourceLocation(createArray);
      this.generator.Emit(create, arrayType);
      this.StackSize -= (ushort)(createArray.Rank+boundsEmitted - 1);
      if (createArray.Rank == 1) {
        int i = 0;
        foreach (IExpression elemValue in createArray.Initializers) {
          this.generator.Emit(OperationCode.Dup);
          this.StackSize++;
          this.EmitConstant(i++);
          this.Traverse(elemValue);
          this.StoreVectorElement(createArray.ElementType);
        }
      } else {
        StoreInitializers(createArray, arrayType);
      }
      //TODO: initialization from compile time constant
    }

    private void StoreInitializers(ICreateArray createArray, IArrayTypeReference arrayType) {
      var initializers = new List<IExpression>(createArray.Initializers);
      if (initializers.Count > 0) {
        var sizes = new List<IExpression>(createArray.Sizes);
        // Used to do the "reverse" mapping from offset (linear index into
        // initializer list) to the d-dimensional coordinates, where d is
        // the rank of the array.
        ulong[] dimensionStride = new ulong[sizes.Count];
        dimensionStride[sizes.Count - 1] = 1;
        for (int i = sizes.Count - 2; 0 <= i; i--) {
          var size = ((IConvertible)((ICompileTimeConstant)sizes[i + 1]).Value).ToUInt64(null);
          dimensionStride[i] = size * dimensionStride[i + 1];
        }
        for (int i = 0; i < initializers.Count; i++) {
          this.generator.Emit(OperationCode.Dup);
          this.StackSize++;
          ulong n = (ulong)i; // compute the indices that map to the offset n
          for (uint d = 0; d < createArray.Rank; d++) {
            var divisor = dimensionStride[d];
            var indexInThisDimension = n / divisor;
            n = n % divisor;
            this.EmitConstant((int)indexInThisDimension);
          }
          this.Traverse(initializers[i]);
          this.generator.Emit(OperationCode.Array_Set, arrayType);
          this.StackSize -= (ushort)(createArray.Rank + 2);
        }
      }
    }

    /// <summary>
    /// Generates IL for the specified create delegate instance.
    /// </summary>
    /// <param name="createDelegateInstance">The create delegate instance.</param>
    public override void TraverseChildren(ICreateDelegateInstance createDelegateInstance) {
      IPlatformType platformType = createDelegateInstance.Type.PlatformType;
      MethodReference constructor = new MethodReference(this.host, createDelegateInstance.Type, CallingConvention.Default|CallingConvention.HasThis,
        platformType.SystemVoid, this.host.NameTable.Ctor, 0, platformType.SystemObject, platformType.SystemIntPtr);
      if (createDelegateInstance.Instance != null) {
        this.Traverse(createDelegateInstance.Instance);
        if (createDelegateInstance.IsVirtualDelegate) {
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
    /// Generates IL for the specified create object instance.
    /// </summary>
    /// <param name="createObjectInstance">The create object instance.</param>
    public override void TraverseChildren(ICreateObjectInstance createObjectInstance) {
      this.Traverse(createObjectInstance.Arguments);
      this.EmitSourceLocation(createObjectInstance);
      this.generator.Emit(OperationCode.Newobj, createObjectInstance.MethodToCall);
      this.StackSize -= (ushort)IteratorHelper.EnumerableCount(createObjectInstance.Arguments);
      this.StackSize++;
    }

    /// <summary>
    /// Generates IL for the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    public override void TraverseChildren(IDefaultValue defaultValue) {
      ILocalDefinition temp = new TemporaryVariable(defaultValue.Type, this.method);
      this.LoadAddressOf(temp, null);
      this.generator.Emit(OperationCode.Initobj, defaultValue.Type);
      this.StackSize--;
      this.LoadLocal(temp);
    }

    /// <summary>
    /// Generates IL for the specified debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement">The debugger break statement.</param>
    public override void TraverseChildren(IDebuggerBreakStatement debuggerBreakStatement) {
      this.EmitSequencePoint(debuggerBreakStatement.Locations);
      this.generator.Emit(OperationCode.Break);
    }

    /// <summary>
    /// Generates IL for the specified division.
    /// </summary>
    /// <param name="division">The division.</param>
    public override void TraverseChildren(IDivision division) {
      var targetExpression = division.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, division, (IExpression e) => this.TraverseDivisionRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: division.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(division.LeftOperand);
        this.TraverseDivisionRightOperandAndDoOperation(division);
      }
    }

    private void TraverseDivisionRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IDivision);
      var division = (IDivision)expression;
      this.Traverse(division.RightOperand);
      this.EmitSourceLocation(division);
      if (division.TreatOperandsAsUnsignedIntegers)
        this.generator.Emit(OperationCode.Div_Un);
      else
        this.generator.Emit(OperationCode.Div);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified do until statement.
    /// </summary>
    /// <param name="doUntilStatement">The do until statement.</param>
    public override void TraverseChildren(IDoUntilStatement doUntilStatement) {
      ILGeneratorLabel savedCurrentBreakTarget = this.currentBreakTarget;
      ILGeneratorLabel savedCurrentContinueTarget = this.currentContinueTarget;
      this.currentBreakTarget = new ILGeneratorLabel();
      this.currentContinueTarget = new ILGeneratorLabel();
      if (this.currentTryCatch != null) {
        this.mostNestedTryCatchFor.Add(this.currentBreakTarget, this.currentTryCatch);
        this.mostNestedTryCatchFor.Add(this.currentContinueTarget, this.currentTryCatch);
      }

      this.generator.MarkLabel(this.currentContinueTarget);
      this.Traverse(doUntilStatement.Body);
      this.EmitSequencePoint(doUntilStatement.Condition.Locations);
      this.VisitBranchIfFalse(doUntilStatement.Condition, this.currentContinueTarget);
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Performs some computation with the given dup value expression.
    /// </summary>
    /// <param name="dupValue"></param>
    public override void TraverseChildren(IDupValue dupValue) {
      this.generator.Emit(OperationCode.Dup);
      this.StackSize++;
    }

    /// <summary>
    /// Generates IL for the specified empty statement.
    /// </summary>
    /// <param name="emptyStatement">The empty statement.</param>
    public override void TraverseChildren(IEmptyStatement emptyStatement) {
      if (!this.minizeCodeSize || IteratorHelper.EnumerableIsNotEmpty(emptyStatement.Locations)) {
        this.EmitSequencePoint(emptyStatement.Locations);
        this.generator.Emit(OperationCode.Nop);
      }
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified equality.
    /// </summary>
    /// <param name="equality">The equality.</param>
    public override void TraverseChildren(IEquality equality) {
      this.Traverse(equality.LeftOperand);
      this.Traverse(equality.RightOperand);
      this.EmitSourceLocation(equality);
      this.generator.Emit(OperationCode.Ceq);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified exclusive or.
    /// </summary>
    /// <param name="exclusiveOr">The exclusive or.</param>
    public override void TraverseChildren(IExclusiveOr exclusiveOr) {
      var targetExpression = exclusiveOr.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, exclusiveOr, (IExpression e) => this.TraverseExclusiveOrRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: exclusiveOr.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(exclusiveOr.LeftOperand);
        this.TraverseExclusiveOrRightOperandAndDoOperation(exclusiveOr);
      }
    }

    private void TraverseExclusiveOrRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IExclusiveOr);
      var exclusiveOr = (IExclusiveOr)expression;
      this.Traverse(exclusiveOr.RightOperand);
      this.EmitSourceLocation(exclusiveOr);
      this.generator.Emit(OperationCode.Xor);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified expression statement.
    /// </summary>
    /// <param name="expressionStatement">The expression statement.</param>
    public override void TraverseChildren(IExpressionStatement expressionStatement) {
      this.EmitSequencePoint(expressionStatement.Locations);
      IAssignment/*?*/ assigment = expressionStatement.Expression as IAssignment;
      if (assigment != null) {
        this.VisitAssignment(assigment, true);
      } else {
        IBinaryOperation binOp = expressionStatement.Expression as IBinaryOperation;
        if (binOp != null && binOp.LeftOperand is ITargetExpression) {
          this.currentExpressionIsStatement = true;
          this.Traverse(expressionStatement.Expression);
        } else {
          this.Traverse(expressionStatement.Expression);
          if (expressionStatement.Expression.Type.TypeCode != PrimitiveTypeCode.Void) {
            this.generator.Emit(OperationCode.Pop);
            this.StackSize--;
          }
        }
      }
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified fill memory statement.
    /// </summary>
    /// <param name="fillMemoryStatement">The fill memory statement.</param>
    public override void TraverseChildren(IFillMemoryStatement fillMemoryStatement) {
      this.EmitSequencePoint(fillMemoryStatement.Locations);
      this.Traverse(fillMemoryStatement.TargetAddress);
      this.Traverse(fillMemoryStatement.FillValue);
      this.Traverse(fillMemoryStatement.NumberOfBytesToFill);
      this.generator.Emit(OperationCode.Initblk);
    }

    /// <summary>
    /// Generates IL for the specified for each statement.
    /// </summary>
    /// <param name="forEachStatement">For each statement.</param>
    public override void TraverseChildren(IForEachStatement forEachStatement) {
      var arrayType = forEachStatement.Collection.Type as IArrayTypeReference;
      if (arrayType != null && arrayType.IsVector) {
        this.VisitForeachArrayElement(forEachStatement, arrayType);
        return;
      }
      //TODO: special case for enumerator that is sealed and does not implement IDisposable
      base.TraverseChildren(forEachStatement);
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL code for the given for each statement for the special case where the collection is known
    /// to be vector type.
    /// </summary>
    /// <param name="forEachStatement">The foreach statement to visit.</param>
    /// <param name="arrayType">The vector type of the collection.</param>
    public virtual void VisitForeachArrayElement(IForEachStatement forEachStatement, IArrayTypeReference arrayType) {
      Contract.Requires(arrayType.IsVector);
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
      this.Traverse(forEachStatement.Collection);
      this.generator.Emit(OperationCode.Dup);
      this.StackSize++;
      var array = new TemporaryVariable(arrayType, this.method);
      this.VisitAssignmentTo(array);
      var length = new TemporaryVariable(this.host.PlatformType.SystemInt32, this.method);
      this.generator.Emit(OperationCode.Ldlen);
      this.generator.Emit(OperationCode.Conv_I4);
      this.VisitAssignmentTo(length);
      var counter = new TemporaryVariable(this.host.PlatformType.SystemInt32, this.method);
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.StackSize++;
      this.VisitAssignmentTo(counter);
      this.generator.Emit(OperationCode.Br, conditionCheck);
      this.generator.MarkLabel(loopStart);
      this.LoadLocal(array);
      this.LoadLocal(counter);
      this.LoadVectorElement(arrayType.ElementType);
      this.VisitAssignmentTo(forEachStatement.Variable);
      this.Traverse(forEachStatement.Body);
      this.generator.MarkLabel(this.currentContinueTarget);
      this.LoadLocal(counter);
      this.generator.Emit(OperationCode.Ldc_I4_1);
      this.StackSize++;
      this.generator.Emit(OperationCode.Add);
      this.StackSize--;
      this.VisitAssignmentTo(counter);
      this.generator.MarkLabel(conditionCheck);
      this.EmitSequencePoint(forEachStatement.Collection.Locations);
      this.LoadLocal(counter);
      this.LoadLocal(length);
      this.generator.Emit(OperationCode.Blt, loopStart);
      this.StackSize -= 2;
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
    }

    /// <summary>
    /// Generates IL for the specified for statement.
    /// </summary>
    /// <param name="forStatement">For statement.</param>
    public override void TraverseChildren(IForStatement forStatement) {
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

      this.Traverse(forStatement.InitStatements);
      this.generator.Emit(OperationCode.Br, conditionCheck);
      this.generator.MarkLabel(loopStart);
      this.Traverse(forStatement.Body);
      this.generator.MarkLabel(this.currentContinueTarget);
      this.Traverse(forStatement.IncrementStatements);
      this.generator.MarkLabel(conditionCheck);
      this.EmitSequencePoint(forStatement.Condition.Locations);
      this.VisitBranchIfTrue(forStatement.Condition, loopStart);
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified get type of typed reference.
    /// </summary>
    /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
    public override void TraverseChildren(IGetTypeOfTypedReference getTypeOfTypedReference) {
      this.Traverse(getTypeOfTypedReference.TypedReference);
      this.generator.Emit(OperationCode.Refanytype);
    }

    /// <summary>
    /// Generates IL for the specified get value of typed reference.
    /// </summary>
    /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
    public override void TraverseChildren(IGetValueOfTypedReference getValueOfTypedReference) {
      this.Traverse(getValueOfTypedReference.TypedReference);
      this.generator.Emit(OperationCode.Refanyval, getValueOfTypedReference.TargetType);
    }

    /// <summary>
    /// Generates IL for the specified goto statement.
    /// </summary>
    /// <param name="gotoStatement">The goto statement.</param>
    public override void TraverseChildren(IGotoStatement gotoStatement) {
      this.EmitSequencePoint(gotoStatement.Locations);
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
    /// Generates IL for the specified goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
    public override void TraverseChildren(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      this.EmitSequencePoint(gotoSwitchCaseStatement.Locations);
      base.TraverseChildren(gotoSwitchCaseStatement);
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified greater than.
    /// </summary>
    /// <param name="greaterThan">The greater than.</param>
    public override void TraverseChildren(IGreaterThan greaterThan) {
      this.Traverse(greaterThan.LeftOperand);
      this.Traverse(greaterThan.RightOperand);
      this.EmitSourceLocation(greaterThan);
      if (greaterThan.IsUnsignedOrUnordered)
        this.generator.Emit(OperationCode.Cgt_Un);
      else
        this.generator.Emit(OperationCode.Cgt);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified greater than or equal.
    /// </summary>
    /// <param name="greaterThanOrEqual">The greater than or equal.</param>
    public override void TraverseChildren(IGreaterThanOrEqual greaterThanOrEqual) {
      this.Traverse(greaterThanOrEqual.LeftOperand);
      this.Traverse(greaterThanOrEqual.RightOperand);
      this.EmitSourceLocation(greaterThanOrEqual);
      if (greaterThanOrEqual.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(greaterThanOrEqual.LeftOperand.Type))
        this.generator.Emit(OperationCode.Clt_Un);
      else
        this.generator.Emit(OperationCode.Clt);
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.generator.Emit(OperationCode.Ceq);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified labeled statement.
    /// </summary>
    /// <param name="labeledStatement">The labeled statement.</param>
    public override void TraverseChildren(ILabeledStatement labeledStatement) {
      ILGeneratorLabel targetLabel;
      if (!this.labelFor.TryGetValue(labeledStatement.Label.UniqueKey, out targetLabel)) {
        targetLabel = new ILGeneratorLabel();
        this.labelFor.Add(labeledStatement.Label.UniqueKey, targetLabel);
      }
      this.generator.MarkLabel(targetLabel);
      this.Traverse(labeledStatement.Statement);
    }

    /// <summary>
    /// Generates IL for the specified left shift.
    /// </summary>
    /// <param name="leftShift">The left shift.</param>
    public override void TraverseChildren(ILeftShift leftShift) {
      var targetExpression = leftShift.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, leftShift, (IExpression e) => this.TraverseLeftShiftRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: leftShift.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(leftShift.LeftOperand);
        this.TraverseLeftShiftRightOperandAndDoOperation(leftShift);
      }
    }

    private void TraverseLeftShiftRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is ILeftShift);
      var leftShift = (ILeftShift)expression;
      this.Traverse(leftShift.RightOperand);
      this.EmitSourceLocation(leftShift);
      this.generator.Emit(OperationCode.Shl);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified less than.
    /// </summary>
    /// <param name="lessThan">The less than.</param>
    public override void TraverseChildren(ILessThan lessThan) {
      this.Traverse(lessThan.LeftOperand);
      this.Traverse(lessThan.RightOperand);
      this.EmitSourceLocation(lessThan);
      if (lessThan.IsUnsignedOrUnordered)
        this.generator.Emit(OperationCode.Clt_Un);
      else
        this.generator.Emit(OperationCode.Clt);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified less than or equal.
    /// </summary>
    /// <param name="lessThanOrEqual">The less than or equal.</param>
    public override void TraverseChildren(ILessThanOrEqual lessThanOrEqual) {
      this.Traverse(lessThanOrEqual.LeftOperand);
      this.Traverse(lessThanOrEqual.RightOperand);
      if (lessThanOrEqual.IsUnsignedOrUnordered && !TypeHelper.IsPrimitiveInteger(lessThanOrEqual.LeftOperand.Type))
        this.generator.Emit(OperationCode.Cgt_Un);
      else
        this.generator.Emit(OperationCode.Cgt);
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.generator.Emit(OperationCode.Ceq);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    public override void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      this.EmitSequencePoint(localDeclarationStatement.Locations);
      if (localDeclarationStatement.LocalVariable.IsConstant) {
        this.generator.AddConstantToCurrentScope(localDeclarationStatement.LocalVariable);
        this.generator.Emit(OperationCode.Nop); //Make sure the constant always has a scope
      } else {
        this.GetLocalIndex(localDeclarationStatement.LocalVariable);
        this.generator.AddVariableToCurrentScope(localDeclarationStatement.LocalVariable);
        if (localDeclarationStatement.InitialValue != null) {
          if (localDeclarationStatement.LocalVariable.Type.IsValueType) {
            if (localDeclarationStatement.InitialValue is IDefaultValue) {
              this.LoadAddressOf(localDeclarationStatement.LocalVariable, null);
              this.generator.Emit(OperationCode.Initobj, localDeclarationStatement.LocalVariable.Type);
              this.StackSize--;
              this.lastStatementWasUnconditionalTransfer = false;
              return;
            }
            var createObj = localDeclarationStatement.InitialValue as ICreateObjectInstance;
            if (createObj != null) {
              this.LoadAddressOf(localDeclarationStatement.LocalVariable, null);
              this.Traverse(createObj.Arguments);
              this.generator.Emit(OperationCode.Call, createObj.MethodToCall);
              this.StackSize -= (ushort)(IteratorHelper.EnumerableCount(createObj.Arguments)+1);
              this.lastStatementWasUnconditionalTransfer = false;
              return;
            }
          }
          this.Traverse(localDeclarationStatement.InitialValue);
          this.VisitAssignmentTo(localDeclarationStatement.LocalVariable);
        }
      }
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified lock statement.
    /// </summary>
    /// <param name="lockStatement">The lock statement.</param>
    public override void TraverseChildren(ILockStatement lockStatement) {
      if (this.host.SystemCoreAssemblySymbolicIdentity.Version.Major < 4) {
        this.GenerateDownLevelLockStatement(lockStatement);
        return;
      }
      var systemThreading = new NestedUnitNamespaceReference(this.host.PlatformType.SystemObject.ContainingUnitNamespace,
        this.host.NameTable.GetNameFor("Threading"));
      var systemThreadingMonitor = new NamespaceTypeReference(this.host, systemThreading, this.host.NameTable.GetNameFor("Monitor"), 0,
        isEnum: false, isValueType: false, typeCode: PrimitiveTypeCode.NotPrimitive);
      var parameters = new IParameterTypeInformation[2];
      var monitorEnter = new MethodReference(this.host, systemThreadingMonitor, CallingConvention.Default, this.host.PlatformType.SystemVoid,
        this.host.NameTable.GetNameFor("Enter"), 0, parameters);
      parameters[0] = new SimpleParameterTypeInformation(monitorEnter, 0, this.host.PlatformType.SystemObject);
      parameters[1] = new SimpleParameterTypeInformation(monitorEnter, 1, this.host.PlatformType.SystemBoolean, isByReference: true);
      var monitorExit = new MethodReference(this.host, systemThreadingMonitor, CallingConvention.Default, this.host.PlatformType.SystemVoid,
        this.host.NameTable.GetNameFor("Exit"), 0, this.host.PlatformType.SystemObject);

      this.EmitSequencePoint(lockStatement.Locations);
      var guardObject = new TemporaryVariable(lockStatement.Guard.Type, this.method);
      var lockTaken = new TemporaryVariable(this.host.PlatformType.SystemBoolean, this.method);
      //try
      var savedCurrentTryCatch = this.currentTryCatch;
      this.currentTryCatch = lockStatement;
      var savedCurrentTryCatchFinallyEnd = this.currentTryCatchFinallyEnd;
      this.currentTryCatchFinallyEnd = new ILGeneratorLabel();
      this.generator.BeginTryBody();
      this.Traverse(lockStatement.Guard);
      this.generator.Emit(OperationCode.Dup); this.StackSize++;
      this.VisitAssignmentTo(guardObject);
      this.LoadAddressOf(lockTaken, null);
      this.generator.Emit(OperationCode.Call, monitorEnter);
      this.StackSize-=2;
      this.Traverse(lockStatement.Body);
      if (!this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Leave, this.currentTryCatchFinallyEnd);
      //finally
      this.generator.BeginFinallyBlock();
      //if (status)
      var endIf = new ILGeneratorLabel();
      this.LoadLocal(lockTaken);
      this.generator.Emit(OperationCode.Brfalse_S, endIf);
      this.StackSize--;
      this.LoadLocal(guardObject);
      this.generator.Emit(OperationCode.Call, monitorExit);
      this.StackSize--;
      this.generator.MarkLabel(endIf);
      //monitor exit
      this.generator.Emit(OperationCode.Endfinally);
      this.generator.EndTryBody();
      this.generator.MarkLabel(this.currentTryCatchFinallyEnd);
      this.currentTryCatchFinallyEnd = savedCurrentTryCatchFinallyEnd;
      this.currentTryCatch = savedCurrentTryCatch;
      this.lastStatementWasUnconditionalTransfer = false;
    }

    private void GenerateDownLevelLockStatement(ILockStatement lockStatement) {
      var systemThreading = new NestedUnitNamespaceReference(this.host.PlatformType.SystemObject.ContainingUnitNamespace,
        this.host.NameTable.GetNameFor("Threading"));
      var systemThreadingMonitor = new NamespaceTypeReference(this.host, systemThreading, this.host.NameTable.GetNameFor("Monitor"), 0,
        isEnum: false, isValueType: false, typeCode: PrimitiveTypeCode.NotPrimitive);
      var parameters = new IParameterTypeInformation[2];
      var monitorEnter = new MethodReference(this.host, systemThreadingMonitor, CallingConvention.Default, this.host.PlatformType.SystemVoid,
        this.host.NameTable.GetNameFor("Enter"), 0, this.host.PlatformType.SystemObject);
      var monitorExit = new MethodReference(this.host, systemThreadingMonitor, CallingConvention.Default, this.host.PlatformType.SystemVoid,
        this.host.NameTable.GetNameFor("Exit"), 0, this.host.PlatformType.SystemObject);

      this.EmitSequencePoint(lockStatement.Locations);
      var guardObject = new TemporaryVariable(lockStatement.Guard.Type, this.method);
      this.Traverse(lockStatement.Guard);
      this.generator.Emit(OperationCode.Dup); this.StackSize++;
      this.VisitAssignmentTo(guardObject);
      this.generator.Emit(OperationCode.Call, monitorEnter);
      this.StackSize--;
      //try
      var savedCurrentTryCatch = this.currentTryCatch;
      this.currentTryCatch = lockStatement;
      var savedCurrentTryCatchFinallyEnd = this.currentTryCatchFinallyEnd;
      this.currentTryCatchFinallyEnd = new ILGeneratorLabel();
      this.generator.BeginTryBody();
      this.Traverse(lockStatement.Body);
      if (!this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Leave, this.currentTryCatchFinallyEnd);
      //finally
      this.generator.BeginFinallyBlock();
      //if (status)
      this.LoadLocal(guardObject);
      this.generator.Emit(OperationCode.Call, monitorExit);
      this.StackSize--;
      //monitor exit
      this.generator.Emit(OperationCode.Endfinally);
      this.generator.EndTryBody();
      this.generator.MarkLabel(this.currentTryCatchFinallyEnd);
      this.currentTryCatchFinallyEnd = savedCurrentTryCatchFinallyEnd;
      this.currentTryCatch = savedCurrentTryCatch;
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified logical not.
    /// </summary>
    /// <param name="logicalNot">The logical not.</param>
    public override void TraverseChildren(ILogicalNot logicalNot) {
      if (logicalNot.Operand.Type.IsValueType) {
        //The type should be a primitive integer, a boolean or an enum.
        this.Traverse(logicalNot.Operand);
        this.EmitSourceLocation(logicalNot);
        var opsize = TypeHelper.SizeOfType(logicalNot.Operand.Type);
        if (opsize == 1 || opsize == 2 || opsize == 4) {
          this.generator.Emit(OperationCode.Ldc_I4_0);
          this.StackSize++;
          this.generator.Emit(OperationCode.Ceq);
          this.StackSize--;
        } else if (opsize == 8) {
          this.generator.Emit(OperationCode.Ldc_I4_0);
          this.StackSize++;
          this.generator.Emit(OperationCode.Conv_I8);
          this.generator.Emit(OperationCode.Ceq);
          this.StackSize--;
        } else {
          Contract.Assert(opsize == 0); //If not, the CodeModel is invalid.
          //the type is an unresolved reference, typically an enum, so we just don't know what size it is (at compile time, that is).
          var trueCase = new ILGeneratorLabel();
          var done = new ILGeneratorLabel();
          this.generator.Emit(OperationCode.Brtrue_S, trueCase);
          this.generator.Emit(OperationCode.Ldc_I4_0);
          this.generator.Emit(OperationCode.Br_S, done);
          this.generator.MarkLabel(trueCase);
          this.generator.Emit(OperationCode.Ldc_I4_1);
          this.generator.MarkLabel(done);
        }
      } else {
        //pointer non null test
        this.Traverse(logicalNot.Operand);
        this.generator.Emit(OperationCode.Ldnull);
        this.StackSize++;
        this.generator.Emit(OperationCode.Ceq);
        this.StackSize--;
      }
    }

    /// <summary>
    /// Generates IL for the specified make typed reference.
    /// </summary>
    /// <param name="makeTypedReference">The make typed reference.</param>
    public override void TraverseChildren(IMakeTypedReference makeTypedReference) {
      this.Traverse(makeTypedReference.Operand);
      var type = makeTypedReference.Operand.Type;
      var mptr = type as IManagedPointerTypeReference;
      Contract.Assume(mptr != null);
      this.generator.Emit(OperationCode.Mkrefany, mptr.TargetType);
    }

    /// <summary>
    /// Generates IL for the specified method call.
    /// </summary>
    /// <param name="methodCall">The method call.</param>
    public override void TraverseChildren(IMethodCall methodCall) {
      if (methodCall.MethodToCall is Dummy) return;
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall)
        this.Traverse(methodCall.ThisArgument);
      this.Traverse(methodCall.Arguments);
      if (methodCall.MethodToCall.ContainingType.TypeCode == PrimitiveTypeCode.Float64 && methodCall.MethodToCall.CallingConvention == CallingConvention.FastCall &&
        methodCall.MethodToCall.Name.Value == "__ckfinite__") {
        this.generator.Emit(OperationCode.Ckfinite);
        return;
      }
      OperationCode call = OperationCode.Call;
      if (methodCall.IsVirtualCall) {
        call = OperationCode.Callvirt;
        IManagedPointerTypeReference mpt = methodCall.ThisArgument.Type as IManagedPointerTypeReference;
        if (mpt != null)
          this.generator.Emit(OperationCode.Constrained_, mpt.TargetType);
      } else if (methodCall.IsJumpCall)
        call = OperationCode.Jmp;
      if (methodCall.IsTailCall)
        this.generator.Emit(OperationCode.Tail_);
      this.EmitSourceLocation(methodCall);
      this.generator.Emit(call, methodCall.MethodToCall);
      this.StackSize -= (ushort)IteratorHelper.EnumerableCount(methodCall.Arguments);
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall) this.StackSize--;
      if (methodCall.Type.TypeCode != PrimitiveTypeCode.Void)
        this.StackSize++;
    }

    /// <summary>
    /// Generates IL for the specified modulus.
    /// </summary>
    /// <param name="modulus">The modulus.</param>
    public override void TraverseChildren(IModulus modulus) {
      var targetExpression = modulus.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, modulus, (IExpression e) => this.TraverseModulusRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: modulus.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(modulus.LeftOperand);
        this.TraverseModulusRightOperandAndDoOperation(modulus);
      }
    }

    private void TraverseModulusRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IModulus);
      var modulus = (IModulus)expression;
      this.Traverse(modulus.RightOperand);
      this.EmitSourceLocation(modulus);
      if (modulus.TreatOperandsAsUnsignedIntegers)
        this.generator.Emit(OperationCode.Rem_Un);
      else
        this.generator.Emit(OperationCode.Rem);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified multiplication.
    /// </summary>
    /// <param name="multiplication">The multiplication.</param>
    public override void TraverseChildren(IMultiplication multiplication) {
      var targetExpression = multiplication.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, multiplication, (IExpression e) => this.TraverseMultiplicationRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: multiplication.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(multiplication.LeftOperand);
        this.TraverseMultiplicationRightOperandAndDoOperation(multiplication);
      }
    }

    private void TraverseMultiplicationRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IMultiplication);
      var multiplication = (IMultiplication)expression;
      this.Traverse(multiplication.RightOperand);
      this.EmitSourceLocation(multiplication);
      OperationCode operationCode = OperationCode.Mul;
      if (multiplication.CheckOverflow) {
        if (multiplication.TreatOperandsAsUnsignedIntegers)
          operationCode = OperationCode.Mul_Ovf_Un;
        else
          operationCode = OperationCode.Mul_Ovf;
      }
      this.generator.Emit(operationCode);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified not equality.
    /// </summary>
    /// <param name="notEquality">The not equality.</param>
    public override void TraverseChildren(INotEquality notEquality) {
      this.Traverse(notEquality.LeftOperand);
      this.Traverse(notEquality.RightOperand);
      this.EmitSourceLocation(notEquality);
      var compileTimeConstant = notEquality.LeftOperand as ICompileTimeConstant;
      if (compileTimeConstant != null) {
        if (compileTimeConstant.Value == null) {
          this.generator.Emit(OperationCode.Clt_Un);
          this.StackSize--;
          return;
        }
      }
      compileTimeConstant = notEquality.RightOperand as ICompileTimeConstant;
      if (compileTimeConstant != null) {
        if (compileTimeConstant.Value == null) {
          this.generator.Emit(OperationCode.Cgt_Un);
          this.StackSize--;
          return;
        }
      }
      this.generator.Emit(OperationCode.Ceq);
      this.generator.Emit(OperationCode.Ldc_I4_0);
      this.generator.Emit(OperationCode.Ceq);
      this.StackSize--;
    }

    /// <summary>
    /// Throws an exception when executed: IOldValue nodes
    /// must be replaced before converting the Code Model to IL.
    /// </summary>
    public override void TraverseChildren(IOldValue oldValue) {
      Contract.Assume(false, "IOldValue nodes must be replaced before trying to convert the Code Model to IL.");
    }


    /// <summary>
    /// Generates IL for the specified ones complement.
    /// </summary>
    /// <param name="onesComplement">The ones complement.</param>
    public override void TraverseChildren(IOnesComplement onesComplement) {
      this.Traverse(onesComplement.Operand);
      this.EmitSourceLocation(onesComplement);
      this.generator.Emit(OperationCode.Not);
    }

    /// <summary>
    /// Generates IL for the specified out argument.
    /// </summary>
    /// <param name="outArgument">The out argument.</param>
    public override void TraverseChildren(IOutArgument outArgument) {
      this.LoadAddressOf(outArgument.Expression, null);
    }

    /// <summary>
    /// Generates IL for the specified pointer call.
    /// </summary>
    /// <param name="pointerCall">The pointer call.</param>
    public override void TraverseChildren(IPointerCall pointerCall) {
      this.Traverse(pointerCall.Arguments);
      this.Traverse(pointerCall.Pointer);
      this.generator.Emit(OperationCode.Calli, pointerCall.Pointer.Type);
      this.StackSize -= (ushort)IteratorHelper.EnumerableCount(pointerCall.Arguments);
      if (pointerCall.Type.TypeCode == PrimitiveTypeCode.Void)
        this.StackSize--;
    }

    /// <summary>
    /// Performs some computation with the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public override void TraverseChildren(IPopValue popValue) {
      //Do nothing. The containing expression or statement will consume the value.
    }

    /// <summary>
    /// Performs some computation with the given push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    public override void TraverseChildren(IPushStatement pushStatement) {
      this.Traverse(pushStatement.ValueToPush);
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified ref argument.
    /// </summary>
    /// <param name="refArgument">The ref argument.</param>
    public override void TraverseChildren(IRefArgument refArgument) {
      this.LoadAddressOf(refArgument.Expression, null);
    }

    /// <summary>
    /// Generates IL for the specified resource use statement.
    /// </summary>
    /// <param name="resourceUseStatement">The resource use statement.</param>
    public override void TraverseChildren(IResourceUseStatement resourceUseStatement) {
      this.EmitSequencePoint(resourceUseStatement.Locations);
      var systemIDisposable = new NamespaceTypeReference(this.host, this.host.PlatformType.SystemObject.ContainingUnitNamespace,
        this.host.NameTable.GetNameFor("IDisposable"), 0, isEnum: false, isValueType: false, typeCode: PrimitiveTypeCode.NotPrimitive);
      var dispose = new MethodReference(this.host, systemIDisposable, CallingConvention.HasThis, this.host.PlatformType.SystemVoid,
        this.host.NameTable.GetNameFor("Dispose"), 0, Enumerable<IParameterTypeInformation>.Empty);

      //Get resource into a local
      ILocalDefinition resourceLocal;
      var localDeclaration = resourceUseStatement.ResourceAcquisitions as ILocalDeclarationStatement;
      if (localDeclaration != null) {
        resourceLocal = localDeclaration.LocalVariable;
        this.Traverse(localDeclaration.InitialValue);
      } else {
        var expressionStatement = (IExpressionStatement)resourceUseStatement.ResourceAcquisitions;
        this.Traverse(expressionStatement.Expression);
        resourceLocal = new TemporaryVariable(systemIDisposable, this.method);
      }
      this.VisitAssignmentTo(resourceLocal);

      //try
      var savedCurrentTryCatch = this.currentTryCatch;
      this.currentTryCatch = resourceUseStatement;
      var savedCurrentTryCatchFinallyEnd = this.currentTryCatchFinallyEnd;
      this.currentTryCatchFinallyEnd = new ILGeneratorLabel();
      this.generator.BeginTryBody();
      this.Traverse(resourceUseStatement.Body);
      if (!this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Leave, this.currentTryCatchFinallyEnd);

      //finally
      this.generator.BeginFinallyBlock();
      var endOfFinally = new ILGeneratorLabel();
      if (!resourceLocal.Type.IsValueType) {
        this.generator.Emit(OperationCode.Ldloc, resourceLocal);
        this.generator.Emit(OperationCode.Brfalse_S, endOfFinally);
      }
      this.generator.Emit(OperationCode.Ldloc, resourceLocal);
      this.generator.Emit(OperationCode.Callvirt, dispose);
      this.generator.MarkLabel(endOfFinally);
      this.generator.Emit(OperationCode.Endfinally);
      this.generator.EndTryBody();
      this.generator.MarkLabel(this.currentTryCatchFinallyEnd);
      this.currentTryCatchFinallyEnd = savedCurrentTryCatchFinallyEnd;
      this.currentTryCatch = savedCurrentTryCatch;
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement">The rethrow statement.</param>
    public override void TraverseChildren(IRethrowStatement rethrowStatement) {
      this.EmitSequencePoint(rethrowStatement.Locations);
      this.generator.Emit(OperationCode.Rethrow);
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Generates IL for the specified return statement.
    /// </summary>
    /// <param name="returnStatement">The return statement.</param>
    public override void TraverseChildren(IReturnStatement returnStatement) {
      this.EmitSequencePoint(returnStatement.Locations);
      if (returnStatement.Expression != null) {
        this.Traverse(returnStatement.Expression);
        if (!this.minizeCodeSize || this.currentTryCatch != null) {
          if (this.returnLocal == null)
            this.returnLocal = new TemporaryVariable(this.method.Type, this.method);
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
    /// Throws an exception when executed: IReturnValue nodes
    /// must be replaced before converting the Code Model to IL.
    /// </summary>
    public override void TraverseChildren(IReturnValue returnValue) {
      Contract.Assume(false, "IReturnValue nodes must be replaced before trying to convert the Code Model to IL.");
    }

    /// <summary>
    /// Generates IL for the specified right shift.
    /// </summary>
    /// <param name="rightShift">The right shift.</param>
    public override void TraverseChildren(IRightShift rightShift) {
      var targetExpression = rightShift.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, rightShift, (IExpression e) => this.TraverseRightShiftRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: rightShift.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(rightShift.LeftOperand);
        this.TraverseRightShiftRightOperandAndDoOperation(rightShift);
      }
    }

    private void TraverseRightShiftRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is IRightShift);
      var rightShift = (IRightShift)expression;
      this.Traverse(rightShift.RightOperand);
      this.EmitSourceLocation(rightShift);
      if (TypeHelper.IsUnsignedPrimitiveInteger(rightShift.LeftOperand.Type))
        this.generator.Emit(OperationCode.Shr_Un);
      else
        this.generator.Emit(OperationCode.Shr);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
    public override void TraverseChildren(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      this.generator.Emit(OperationCode.Arglist);
      this.StackSize++;
    }

    /// <summary>
    /// Generates IL for the specified size of.
    /// </summary>
    /// <param name="sizeOf">The size of.</param>
    public override void TraverseChildren(ISizeOf sizeOf) {
      this.EmitSourceLocation(sizeOf);
      this.generator.Emit(OperationCode.Sizeof, sizeOf.TypeToSize);
      this.StackSize++;
    }

    /// <summary>
    /// Generates IL for the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    public override void TraverseChildren(ISourceMethodBody methodBody) {
      base.TraverseChildren(methodBody);
    }

    /// <summary>
    /// Generates IL for the specified stack array create.
    /// </summary>
    /// <param name="stackArrayCreate">The stack array create.</param>
    public override void TraverseChildren(IStackArrayCreate stackArrayCreate) {
      this.Traverse(stackArrayCreate.Size);
      this.generator.Emit(OperationCode.Localloc);
    }

    /// <summary>
    /// Generates IL for the specified subtraction.
    /// </summary>
    /// <param name="subtraction">The subtraction.</param>
    public override void TraverseChildren(ISubtraction subtraction) {
      var targetExpression = subtraction.LeftOperand as ITargetExpression;
      if (targetExpression != null) {
        bool statement = this.currentExpressionIsStatement;
        this.currentExpressionIsStatement = false;
        this.VisitAssignment(targetExpression, subtraction, (IExpression e) => this.TraverseSubtractionRightOperandAndDoOperation(e),
          treatAsStatement: statement, pushTargetRValue: true, resultIsInitialTargetRValue: subtraction.ResultIsUnmodifiedLeftOperand);
      } else {
        this.Traverse(subtraction.LeftOperand);
        this.TraverseSubtractionRightOperandAndDoOperation(subtraction);
      }
    }

    private void TraverseSubtractionRightOperandAndDoOperation(IExpression expression) {
      Contract.Assume(expression is ISubtraction);
      var subtraction = (ISubtraction)expression;
      this.Traverse(subtraction.RightOperand);
      this.EmitSourceLocation(subtraction);
      OperationCode operationCode = OperationCode.Sub;
      if (subtraction.CheckOverflow) {
        if (subtraction.TreatOperandsAsUnsignedIntegers)
          operationCode = OperationCode.Sub_Ovf_Un;
        else
          operationCode = OperationCode.Sub_Ovf;
      }
      this.generator.Emit(operationCode);
      this.StackSize--;
    }

    /// <summary>
    /// Generates IL for the specified switch case.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    public override void TraverseChildren(ISwitchCase switchCase) {
      this.Traverse(switchCase.Body);
    }

    /// <summary>
    /// Generates IL for the specified switch statement.
    /// </summary>
    /// <param name="switchStatement">The switch statement.</param>
    public override void TraverseChildren(ISwitchStatement switchStatement) {
      this.EmitSequencePoint(switchStatement.Locations);
      this.Traverse(switchStatement.Expression);
      uint numberOfCases;
      uint maxValue = GetMaxCaseExpressionValueAsUInt(switchStatement.Cases, out numberOfCases);
      if (numberOfCases == 0) { this.generator.Emit(OperationCode.Pop); return; }
      //if (maxValue < uint.MaxValue && maxValue/2 < numberOfCases)
      this.GenerateSwitchInstruction(switchStatement.Cases, maxValue);
      //TODO: generate binary search
      //TemporaryVariable switchVar = new TemporaryVariable(switchStatement.Expression.Type);
      //this.VisitAssignmentTo(switchVar);
      //List<ISwitchCase> switchCases = this.GetSortedListOfSwitchCases(switchStatement.Cases);
      //TODO: special handling for switch over strings
      this.lastStatementWasUnconditionalTransfer = false;
    }

    private void GenerateSwitchInstruction(IEnumerable<ISwitchCase> switchCases, uint maxValue) {
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
      this.StackSize--;
      this.generator.Emit(OperationCode.Br, defaultLabel);
      ILGeneratorLabel savedCurrentBreakTarget = this.currentBreakTarget;
      if (!foundDefault)
        this.currentBreakTarget = defaultLabel;
      else
        this.currentBreakTarget = new ILGeneratorLabel();
      if (this.currentTryCatch != null)
        this.mostNestedTryCatchFor.Add(this.currentBreakTarget, this.currentTryCatch);
      foreach (ISwitchCase switchCase in switchCases) {
        this.generator.MarkLabel(labelFor[switchCase]);
        this.Traverse(switchCase);
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
    /// Performs some computation with the given target expression.
    /// </summary>
    /// <param name="targetExpression"></param>
    public override void TraverseChildren(ITargetExpression targetExpression) {
      Contract.Assume(false, "The expression containing this as a subexpression should never allow a call to this routine.");
    }

    /// <summary>
    /// Generates IL for the specified this reference.
    /// </summary>
    /// <param name="thisReference">The this reference.</param>
    public override void TraverseChildren(IThisReference thisReference) {
      this.generator.Emit(OperationCode.Ldarg_0);
      this.StackSize++;
    }

    /// <summary>
    /// Generates IL for the specified throw statement.
    /// </summary>
    /// <param name="throwStatement">The throw statement.</param>
    public override void TraverseChildren(IThrowStatement throwStatement) {
      this.EmitSequencePoint(throwStatement.Locations);
      this.Traverse(throwStatement.Exception);
      this.generator.Emit(OperationCode.Throw);
      this.StackSize = 0;
      this.lastStatementWasUnconditionalTransfer = true;
    }

    /// <summary>
    /// Generates IL for the specified try catch filter finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
    public override void TraverseChildren(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      var savedCurrentTryCatch = this.currentTryCatch;
      this.currentTryCatch = tryCatchFilterFinallyStatement;
      ILGeneratorLabel/*?*/ savedCurrentTryCatchFinallyEnd = this.currentTryCatchFinallyEnd;
      this.currentTryCatchFinallyEnd = new ILGeneratorLabel();
      this.generator.BeginTryBody();
      this.Traverse(tryCatchFilterFinallyStatement.TryBody);
      if (!this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Leave, this.currentTryCatchFinallyEnd);
      this.Traverse(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null) {
        this.generator.BeginFinallyBlock();
        this.Traverse(tryCatchFilterFinallyStatement.FinallyBody);
        this.generator.Emit(OperationCode.Endfinally);
      }
      if (tryCatchFilterFinallyStatement.FaultBody != null) {
        this.generator.BeginFaultBlock();
        this.Traverse(tryCatchFilterFinallyStatement.FaultBody);
        this.generator.Emit(OperationCode.Endfinally);
      }
      this.generator.EndTryBody();
      this.generator.MarkLabel(this.currentTryCatchFinallyEnd);
      this.currentTryCatchFinallyEnd = savedCurrentTryCatchFinallyEnd;
      this.currentTryCatch = savedCurrentTryCatch;
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified token of.
    /// </summary>
    /// <param name="tokenOf">The token of.</param>
    public override void TraverseChildren(ITokenOf tokenOf) {
      this.EmitSourceLocation(tokenOf);
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
    /// Generates IL for the specified type of.
    /// </summary>
    /// <param name="typeOf">The type of.</param>
    public override void TraverseChildren(ITypeOf typeOf) {
      this.EmitSourceLocation(typeOf);
      this.generator.Emit(OperationCode.Ldtoken, typeOf.TypeToGet);
      this.generator.Emit(OperationCode.Call, this.GetTypeFromHandle);
      this.StackSize++;
    }

    /// <summary>
    /// Generates IL for the specified unary negation.
    /// </summary>
    /// <param name="unaryNegation">The unary negation.</param>
    public override void TraverseChildren(IUnaryNegation unaryNegation) {
      if (unaryNegation.CheckOverflow && TypeHelper.IsSignedPrimitiveInteger(unaryNegation.Type)) {
        this.generator.Emit(OperationCode.Ldc_I4_0);
        this.StackSize++;
        this.Traverse(unaryNegation.Operand);
        this.generator.Emit(OperationCode.Sub_Ovf);
        this.StackSize--;
        return;
      }
      this.Traverse(unaryNegation.Operand);
      this.EmitSourceLocation(unaryNegation);
      this.generator.Emit(OperationCode.Neg);
    }

    /// <summary>
    /// Generates IL for the specified unary plus.
    /// </summary>
    /// <param name="unaryPlus">The unary plus.</param>
    public override void TraverseChildren(IUnaryPlus unaryPlus) {
      this.Traverse(unaryPlus.Operand);
    }

    /// <summary>
    /// Generates IL for the specified vector length.
    /// </summary>
    /// <param name="vectorLength">Length of the vector.</param>
    public override void TraverseChildren(IVectorLength vectorLength) {
      this.Traverse(vectorLength.Vector);
      this.EmitSourceLocation(vectorLength);
      this.generator.Emit(OperationCode.Ldlen);
    }

    /// <summary>
    /// Generates IL for the specified while do statement.
    /// </summary>
    /// <param name="whileDoStatement">The while do statement.</param>
    public override void TraverseChildren(IWhileDoStatement whileDoStatement) {
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
      this.Traverse(whileDoStatement.Body);
      this.generator.MarkLabel(this.currentContinueTarget);
      this.EmitSequencePoint(whileDoStatement.Condition.Locations);
      this.VisitBranchIfTrue(whileDoStatement.Condition, loopStart);
      this.generator.MarkLabel(this.currentBreakTarget);

      this.currentBreakTarget = savedCurrentBreakTarget;
      this.currentContinueTarget = savedCurrentContinueTarget;
      this.lastStatementWasUnconditionalTransfer = false;
    }

    /// <summary>
    /// Generates IL for the specified yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement">The yield break statement.</param>
    public override void TraverseChildren(IYieldBreakStatement yieldBreakStatement) {
      Contract.Assume(false, "IYieldBreakStatement nodes must be replaced before trying to convert the Code Model to IL.");
    }

    /// <summary>
    /// Generates IL for the specified yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement">The yield return statement.</param>
    public override void TraverseChildren(IYieldReturnStatement yieldReturnStatement) {
      Contract.Assume(false, "IYieldReturnStatement nodes must be replaced before trying to convert the Code Model to IL.");
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_I8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_R4);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_R8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_I);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_I8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_R4);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_R8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_I);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
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
              this.StackSize++;
              this.generator.Emit(OperationCode.Conv_U8);
              this.generator.Emit(OperationCode.Ceq);
              this.StackSize--;
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
            case PrimitiveTypeCode.UIntPtr:
            case PrimitiveTypeCode.Pointer:
            case PrimitiveTypeCode.Reference:
              if (sourceType.TypeCode != targetType.TypeCode)
                this.generator.Emit(targetType.TypeCode == PrimitiveTypeCode.UIntPtr ? OperationCode.Conv_U : OperationCode.Conv_I);
              break;

            default:
              this.generator.Emit(OperationCode.Box, sourceType);
              break;
          }
          break;

        default:
          if (TypeHelper.TypesAreEquivalent(targetType, this.host.PlatformType.SystemObject)) {
            this.generator.Emit(OperationCode.Box, sourceType);
            break;
          }
          //TODO: conversion from method to (function) pointer
          if (!sourceType.IsValueType && !TypeHelper.TypesAreEquivalent(sourceType, this.host.PlatformType.SystemObject) && targetType.TypeCode == PrimitiveTypeCode.IntPtr)
            this.generator.Emit(OperationCode.Conv_I);
          else if (TypeHelper.TypesAreEquivalent(sourceType, this.host.PlatformType.SystemObject)) {
            var mptr = targetType as IManagedPointerTypeReference;
            if (mptr != null)
              this.generator.Emit(OperationCode.Unbox, mptr.TargetType);
            else {
              this.generator.Emit(OperationCode.Unbox_Any, targetType);
            }
          } else if (sourceType.IsValueType && TypeHelper.IsPrimitiveInteger(targetType)) {
            //This can only be legal if sourceType is an unresolved enum type
            switch (targetType.TypeCode) {
              case PrimitiveTypeCode.Int16: this.generator.Emit(OperationCode.Conv_I2); break;
              case PrimitiveTypeCode.Int32: this.generator.Emit(OperationCode.Conv_I4); break;
              case PrimitiveTypeCode.Int64: this.generator.Emit(OperationCode.Conv_I8); break;
              case PrimitiveTypeCode.UInt16: this.generator.Emit(OperationCode.Conv_U2); break;
              case PrimitiveTypeCode.UInt32: this.generator.Emit(OperationCode.Conv_U4); break;
              case PrimitiveTypeCode.UInt64: this.generator.Emit(OperationCode.Conv_U8); break;
              case PrimitiveTypeCode.UInt8: this.generator.Emit(OperationCode.Conv_U1); break;
              default:
                Contract.Assume(false, "Not expected to happen. Please notify hermanv@microsoft.com with a way to reproduce this.");
                break;
            }
          } else if (!sourceType.IsValueType) {
            this.generator.Emit(OperationCode.Unbox_Any, targetType);
          } else {
            Contract.Assume(false, "Not expected to happen. Please notify hermanv@microsoft.com with a way to reproduce this.");
          }
          break;
      }
    }

    private void VisitBranchIfFalse(IExpression expression, ILGeneratorLabel targetLabel) {
      OperationCode branchOp = OperationCode.Brfalse;
      IBinaryOperation/*?*/ binaryOperation = expression as IBinaryOperation;
      bool signedPrimitive = binaryOperation != null && TypeHelper.IsSignedPrimitive(binaryOperation.LeftOperand.Type);
      if (binaryOperation is IEquality) {
        branchOp = OperationCode.Bne_Un;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) || 
          binaryOperation.LeftOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.LeftOperand)) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || 
          binaryOperation.RightOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.RightOperand)) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is INotEquality) {
        branchOp = OperationCode.Beq;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) || 
          binaryOperation.LeftOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.LeftOperand)) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || 
          binaryOperation.RightOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.RightOperand)) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is ILessThan) branchOp = KeepUnsignedButInvertUnordered(((ILessThan)binaryOperation).IsUnsignedOrUnordered, binaryOperation) ? OperationCode.Bge_Un : OperationCode.Bge;
      else if (binaryOperation is ILessThanOrEqual) branchOp = KeepUnsignedButInvertUnordered(((ILessThanOrEqual)binaryOperation).IsUnsignedOrUnordered, binaryOperation) ? OperationCode.Bgt_Un : OperationCode.Bgt;
      else if (binaryOperation is IGreaterThan) branchOp = KeepUnsignedButInvertUnordered(((IGreaterThan)binaryOperation).IsUnsignedOrUnordered, binaryOperation) ? OperationCode.Ble_Un : OperationCode.Ble;
      else if (binaryOperation is IGreaterThanOrEqual) branchOp = KeepUnsignedButInvertUnordered(((IGreaterThanOrEqual)binaryOperation).IsUnsignedOrUnordered, binaryOperation) ? OperationCode.Blt_Un : OperationCode.Blt;
      else {
        IConditional/*?*/ conditional = expression as IConditional;
        if (conditional != null) {
          ICompileTimeConstant/*?*/ resultIfFalse = conditional.ResultIfFalse as ICompileTimeConstant;
          if (resultIfFalse != null && (ExpressionHelper.IsIntegralZero(resultIfFalse) || (resultIfFalse.Value is bool && !((bool)resultIfFalse.Value)))) {
            //conditional.Condition && conditional.ResultIfTrue
            this.VisitBranchIfFalse(conditional.Condition, targetLabel);
            this.VisitBranchIfFalse(conditional.ResultIfTrue, targetLabel);
            return;
          }
          ICompileTimeConstant/*?*/ resultIfTrue = conditional.ResultIfTrue as ICompileTimeConstant;
          if (resultIfTrue != null && (ExpressionHelper.IsIntegralOne(resultIfTrue) || (resultIfTrue.Value is bool && (bool)resultIfTrue.Value))) {
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
            this.VisitBranchIfTrue(logicalNot.Operand, targetLabel);
            return;
          }
        }
      }
      if (branchOp == OperationCode.Brfalse || branchOp == OperationCode.Brtrue) {
        this.Traverse(expression);
        this.StackSize--;
      } else {
        this.Traverse(binaryOperation.LeftOperand);
        this.Traverse(binaryOperation.RightOperand);
        this.StackSize-=2;
      }
      this.EmitSourceLocation(expression);
      this.generator.Emit(branchOp, targetLabel);
    }

    private static bool KeepUnsignedButInvertUnordered(bool usignedOrUnordered, IBinaryOperation binOp) {
      var isIntegerOperation = TypeHelper.IsPrimitiveInteger(binOp.LeftOperand.Type);
      if (usignedOrUnordered) return isIntegerOperation;
      return !isIntegerOperation; //i.e. !(x < y) is the same as (x >= y) only if first comparison returns the opposite result than the second for the unordered case.
    }

    private void VisitBranchIfTrue(IExpression expression, ILGeneratorLabel targetLabel) {
      OperationCode branchOp = OperationCode.Brtrue;
      IBinaryOperation/*?*/ binaryOperation = expression as IBinaryOperation;
      bool signedPrimitive = binaryOperation != null && TypeHelper.IsSignedPrimitive(binaryOperation.LeftOperand.Type);
      if (binaryOperation is IEquality) {
        branchOp = OperationCode.Beq;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) ||
          binaryOperation.LeftOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.LeftOperand)) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || 
          binaryOperation.RightOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.RightOperand)) {
          branchOp = OperationCode.Brfalse;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is INotEquality) {
        branchOp = OperationCode.Bne_Un;
        if (ExpressionHelper.IsIntegralZero(binaryOperation.LeftOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.LeftOperand) || 
          binaryOperation.LeftOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.LeftOperand)) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.RightOperand;
        } else if (ExpressionHelper.IsIntegralZero(binaryOperation.RightOperand) || ExpressionHelper.IsNullLiteral(binaryOperation.RightOperand) || 
          binaryOperation.RightOperand is IDefaultValue || ExpressionHelper.IsZeroIntPtr(binaryOperation.RightOperand)) {
          branchOp = OperationCode.Brtrue;
          expression = binaryOperation.LeftOperand;
        }
      } else if (binaryOperation is ILessThan) branchOp = ((ILessThan)binaryOperation).IsUnsignedOrUnordered ? OperationCode.Blt_Un : OperationCode.Blt;
      else if (binaryOperation is ILessThanOrEqual) branchOp = ((ILessThanOrEqual)binaryOperation).IsUnsignedOrUnordered ? OperationCode.Ble_Un : OperationCode.Ble;
      else if (binaryOperation is IGreaterThan) branchOp = ((IGreaterThan)binaryOperation).IsUnsignedOrUnordered ? OperationCode.Bgt_Un : OperationCode.Bgt;
      else if (binaryOperation is IGreaterThanOrEqual) branchOp = ((IGreaterThanOrEqual)binaryOperation).IsUnsignedOrUnordered ? OperationCode.Bge_Un : OperationCode.Bge;
      else {
        IConditional/*?*/ conditional = expression as IConditional;
        if (conditional != null) {
          ICompileTimeConstant/*?*/ resultIfFalse = conditional.ResultIfFalse as ICompileTimeConstant;
          if (resultIfFalse != null && (ExpressionHelper.IsIntegralZero(resultIfFalse) || (resultIfFalse.Value is bool && !((bool)resultIfFalse.Value)))) {
            //conditional.Condition && conditional.ResultIfTrue
            ILGeneratorLabel fallThrough = new ILGeneratorLabel();
            this.VisitBranchIfFalse(conditional.Condition, fallThrough);
            this.VisitBranchIfTrue(conditional.ResultIfTrue, targetLabel);
            this.generator.MarkLabel(fallThrough);
            return;
          }
          ICompileTimeConstant/*?*/ resultIfTrue = conditional.ResultIfTrue as ICompileTimeConstant;
          if (resultIfTrue != null && (ExpressionHelper.IsIntegralOne(resultIfTrue) || (resultIfTrue.Value is bool && (bool)resultIfTrue.Value))) {
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
        } else {
          ILogicalNot/*?*/ logicalNot = expression as ILogicalNot;
          if (logicalNot != null) {
            this.VisitBranchIfFalse(logicalNot.Operand, targetLabel);
            return;
          }
        }
      }
      if (branchOp == OperationCode.Brfalse || branchOp == OperationCode.Brtrue) {
        this.Traverse(expression);
        this.StackSize--;
      } else {
        this.Traverse(binaryOperation.LeftOperand);
        this.Traverse(binaryOperation.RightOperand);
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

    private void EmitSequencePoint(IEnumerable<ILocation> locations) {
      foreach (var loc in locations) {
        var syncPointLoc = loc as SynchronizationPointLocation;
        if (syncPointLoc != null) {
          var continuationLabel = new ILGeneratorLabel();
          if (syncPointLoc.SynchronizationPoint.ContinuationMethod == null) {
            this.generator.MarkSynchronizationPoint(this.method, continuationLabel);
            this.labelFor[(int)syncPointLoc.SynchronizationPoint.ContinuationOffset] = continuationLabel;
          } else {
            this.generator.MarkSynchronizationPoint(syncPointLoc.SynchronizationPoint.ContinuationMethod, continuationLabel);
            continuationLabel.Offset = syncPointLoc.SynchronizationPoint.ContinuationOffset;
          }
        } else {
          var continuationLoc = loc as ContinuationLocation;
          if (continuationLoc != null) {
            ILGeneratorLabel continuationLabel = this.labelFor[(int)continuationLoc.SynchronizationPointLocation.SynchronizationPoint.ContinuationOffset];
            this.generator.MarkLabel(continuationLabel);
          }
        }
      }
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
      this.localVariables.Add(local);
      return localIndex;
    }

    /// <summary>
    /// Returns the size in bytes of the serialized method body generated by this converter.
    /// </summary>
    /// <returns></returns>
    public uint GetBodySize() {
      return this.generator.CurrentOffset;
    }

    /// <summary>
    /// Returns a local scope for each local variable in the iterator that generated the MoveNext method body that was
    /// converted to IL by this converter. The scopes may be duplicated and occur in the same order as the local variable
    /// declarations occur in the iterator. If this converter did not convert a MoveNext method, the result is null.
    /// </summary>
    public IEnumerable<ILocalScope>/*?*/ GetIteratorScopes() {
      return this.generator.GetIteratorScopes();
    }

    /// <summary>
    /// Returns zero or more local (block) scopes into which the CLR IL operations of this converted Code Model block is organized.
    /// </summary>
    public IEnumerable<ILocalScope> GetLocalScopes() {
      return IteratorHelper.GetConversionEnumerable<ILGeneratorScope, ILocalScope>(this.generator.GetLocalScopes());
    }

    /// <summary>
    /// Returns all of the local variables (including compiler generated temporary variables) that are local to the block
    /// of statements translated by this converter.
    /// </summary>
    public IEnumerable<ILocalDefinition> GetLocalVariables() {
      return this.localVariables.AsReadOnly();
    }

    /// <summary>
    /// Returns zero or more namespace scopes into which the namespace type containing the given method body has been nested.
    /// These scopes determine how simple names are looked up inside the method body. There is a separate scope for each dotted
    /// component in the namespace type name. For istance namespace type x.y.z will have two namespace scopes, the first is for the x and the second
    /// is for the y.
    /// </summary>
    public IEnumerable<INamespaceScope> GetNamespaceScopes() {
      return this.generator.GetNamespaceScopes();
    }

    /// <summary>
    /// Returns the IL operations that correspond to the statements that have been converted to IL by this converter.
    /// </summary>
    public IEnumerable<IOperation> GetOperations() {
      return this.generator.GetOperations();
    }

    /// <summary>
    /// Returns an object that describes where synchronization points occur in the IL operations of the "MoveNext" method of the state class of
    /// an asynchronous method. This returns null unless the generator has been supplied with an non null value for asyncMethodDefinition parameter
    /// during construction.
    /// </summary>
    public ISynchronizationInformation/*?*/ GetSynchronizationInformation() {
      return this.generator.GetSynchronizationInformation();
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
      return Enumerable<ITypeDefinition>.Empty;
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
    /// <param name="body">A block of statements that are to be converted to IL.</param>
    public virtual void ConvertToIL(IBlockStatement body) {
      ITypeReference returnType = this.method.Type;
      new LabelAndTryBlockAssociater(this.mostNestedTryCatchFor).Traverse(body);
      Contract.Assume(this.StackSize == 0);
      this.Traverse(body);
      var ending = this.StackSize;
      if (this.StackSize != 0) {
        //
        // Put a breakpoint here to find (potential) bugs in the decompiler and/or this traverser's
        // tracking of the stack size. However, it cannot be enforced because when structured code
        // is not completely decompiled, the resulting explicit stack instructions cannot be tracked
        // accurately by this traverser. (Unstructured source code can also lead to this situation.)
        //
        // For instance, the following will result in both pushes being counted, but the stack size
        // should increase only by one.
        //
        // if (c) goto L1;
        // push e;
        // goto L2;
        // L1:
        // push f;
        // L2:
        // an expression containing a pop value
      }
      this.generator.MarkLabel(this.endOfMethod);
      if (this.returnLocal != null) {
        this.LoadLocal(this.returnLocal);
        this.generator.Emit(OperationCode.Ret);
      } else if (returnType.TypeCode == PrimitiveTypeCode.Void && !this.lastStatementWasUnconditionalTransfer)
        this.generator.Emit(OperationCode.Ret);
      this.generator.AdjustBranchSizesToBestFit(eliminateBranchesToNext: true);
    }
  }

}

namespace Microsoft.Cci.CodeModelToIL {
  internal class LabelAndTryBlockAssociater : CodeTraverser {

    Dictionary<object, IStatement> mostNestedTryCatchFor;

    ITryCatchFinallyStatement/*?*/ currentTryCatch;

    internal LabelAndTryBlockAssociater(Dictionary<object, IStatement> mostNestedTryCatchFor) {
      this.mostNestedTryCatchFor = mostNestedTryCatchFor;
    }

    public override void TraverseChildren(ILabeledStatement labeledStatement) {
      if (this.currentTryCatch != null)
        this.mostNestedTryCatchFor.Add(labeledStatement, this.currentTryCatch);
      base.TraverseChildren(labeledStatement);
    }

    public override void TraverseChildren(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      ITryCatchFinallyStatement/*?*/ savedCurrentTryCatch = this.currentTryCatch;
      this.currentTryCatch = tryCatchFilterFinallyStatement;
      base.TraverseChildren(tryCatchFilterFinallyStatement);
      this.currentTryCatch = savedCurrentTryCatch;
    }

  }

}
