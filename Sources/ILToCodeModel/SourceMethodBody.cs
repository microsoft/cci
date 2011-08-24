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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {
  /// <summary>
  /// A metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
  /// </summary>
  public class SourceMethodBody : Microsoft.Cci.MutableCodeModel.SourceMethodBody {

    internal readonly IMetadataHost host;

    internal readonly IMethodBody ilMethodBody;
    internal readonly INameTable nameTable;
    internal readonly ISourceLocationProvider/*?*/ sourceLocationProvider;
    internal readonly ILocalScopeProvider/*?*/ localScopeProvider;
    internal readonly DecompilerOptions options;
    private readonly PdbReader/*?*/ pdbReader;
    internal readonly IPlatformType platformType;
    internal List<ILocalDefinition> localVariablesAndTemporaries;
    internal List<ITypeDefinition> privateHelperTypesToRemove;
    internal Dictionary<uint, IMethodDefinition> privateHelperMethodsToRemove;
    internal Dictionary<IFieldDefinition, IFieldDefinition> privateHelperFieldsToRemove;
    ISourceLocation/*?*/ lastSourceLocation;
    ILocation/*?*/ lastLocation;
    bool sawReadonly;
    bool sawTailCall;
    bool sawVolatile;
    byte alignment;

    /// <summary>
    /// Allocates a metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
    /// </summary>
    /// <param name="ilMethodBody">A method body whose IL operations should be decompiled into a block of statements that will be the
    /// result of the Block property of the resulting source method body.</param>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="localScopeProvider">An object that can provide information about the local scopes of a method.</param>
    /// <param name="options">Set of options that control decompilation.</param>
    public SourceMethodBody(IMethodBody ilMethodBody, IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider,
      ILocalScopeProvider/*?*/ localScopeProvider, DecompilerOptions options = DecompilerOptions.None)
      : base(host, sourceLocationProvider) {
      this.ilMethodBody = ilMethodBody;
      this.host = host;
      this.nameTable = host.NameTable;
      this.sourceLocationProvider = sourceLocationProvider;
      this.pdbReader = sourceLocationProvider as PdbReader;
      this.localScopeProvider = localScopeProvider;
      this.options = options;
      this.platformType = ilMethodBody.MethodDefinition.ContainingTypeDefinition.PlatformType;
      this.operationEnumerator = ilMethodBody.Operations.GetEnumerator();
      if (IteratorHelper.EnumerableIsNotEmpty(ilMethodBody.LocalVariables))
        this.LocalsAreZeroed = ilMethodBody.LocalsAreZeroed;
      else
        this.LocalsAreZeroed = true;
      this.MethodDefinition = ilMethodBody.MethodDefinition;
    }

    [ContractInvariantMethod]
    void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.nameTable != null);
      Contract.Invariant(this.platformType != null);
      Contract.Invariant(this.localVariablesAndTemporaries != null);
      Contract.Invariant(this.privateHelperMethodsToRemove != null);
      Contract.Invariant(this.privateHelperFieldsToRemove != null);
      Contract.Invariant(this.blockFor != null);
      Contract.Invariant(this.operandStack != null);
      Contract.Invariant(this.numberOfReferences != null);
      Contract.Invariant(this.targetStatementFor != null);
      Contract.Invariant(this.predecessors != null);
    }

    private Dictionary<uint, BasicBlock> blockFor = new Dictionary<uint, BasicBlock>();

    private static int ConvertToInt(Expression expression) {
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
      CompileTimeConstant/*?*/ cc = expression as CompileTimeConstant;
      if (cc == null) return new ConvertToUnsigned(expression);
      IConvertible/*?*/ ic = cc.Value as IConvertible;
      if (ic == null) {
        if (cc.Value is System.IntPtr) {
          cc.Value = (System.UIntPtr)(ulong)(System.IntPtr)cc.Value;
          cc.Type = this.platformType.SystemUIntPtr;
          return cc;
        }
        return new ConvertToUnsigned(expression);
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

    private void CreateBlocksForBranchTargets() {
      foreach (IOperation ilOperation in this.ilMethodBody.Operations) {
        switch (ilOperation.OperationCode) {
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
          case OperationCode.Br:
          case OperationCode.Br_S:
          case OperationCode.Brfalse:
          case OperationCode.Brfalse_S:
          case OperationCode.Brtrue:
          case OperationCode.Brtrue_S:
          case OperationCode.Leave:
          case OperationCode.Leave_S:
            this.GetOrCreateBlock((uint)ilOperation.Value, true);
            break;
          case OperationCode.Switch: {
              uint[] branches = (uint[])ilOperation.Value;
              foreach (uint targetAddress in branches)
                this.GetOrCreateBlock(targetAddress, true);
            }
            break;
        }
      }
    }

    private void CreateBlocksForExceptionHandlers() {
      foreach (IOperationExceptionInformation exinfo in this.ilMethodBody.OperationExceptionInformation) {
        BasicBlock bb = this.GetOrCreateBlock(exinfo.TryStartOffset, false);
        bb.NumberOfTryBlocksStartingHere++;
        if (exinfo.HandlerKind == HandlerKind.Filter) {
          bb = this.GetOrCreateBlock(exinfo.FilterDecisionStartOffset, false);
          this.GetOrCreateBlock(exinfo.HandlerStartOffset, false);
          bb.Statements.Add(new PushStatement() { ValueToPush = new Pop() { Type = exinfo.ExceptionType } });
        } else if (exinfo.HandlerKind == HandlerKind.Catch) {
          bb = this.GetOrCreateBlock(exinfo.HandlerStartOffset, false);
          bb.Statements.Add(new PushStatement() { ValueToPush = new Pop() { Type = exinfo.ExceptionType } });
        } else {
          bb = this.GetOrCreateBlock(exinfo.HandlerStartOffset, false);
        }
        bb.ExceptionInformation = exinfo;
        bb = this.GetOrCreateBlock(exinfo.HandlerEndOffset, false);
      }
    }

    private void CreateBlocksForLexicalScopes() {
      if (this.pdbReader == null) return;
      foreach (ILocalScope localScope in this.pdbReader.GetLocalScopes(this.ilMethodBody)) {
        BasicBlock block = this.GetOrCreateBlock(localScope.Offset, false);
        block.LocalVariables = new List<ILocalDefinition>(this.pdbReader.GetVariablesInScope(localScope));
        for (int i = 0, n = block.LocalVariables.Count; i < n; i++) {
          block.LocalVariables[i] = this.GetLocalWithSourceName(block.LocalVariables[i]);
        }
        block.EndOffset = localScope.Offset + localScope.Length;
        this.GetOrCreateBlock(block.EndOffset, false);
      }
    }

    /// <summary>
    /// Decompile the IL operations of this method body into a block of statements.
    /// </summary>
    protected override IBlockStatement GetBlock() {
      this.CreateBlocksForLexicalScopes();
      this.CreateBlocksForBranchTargets();
      this.CreateBlocksForExceptionHandlers();
      BasicBlock result = this.GetOrCreateBlock(0, false);
      BasicBlock currentBlock = result;
      while (this.operationEnumerator.MoveNext()) {
        IOperation currentOperation = this.operationEnumerator.Current;
        if (currentOperation.Offset > 0 && this.blockFor.ContainsKey(currentOperation.Offset)) {
          this.TurnOperandStackIntoPushStatements(currentBlock);
          BasicBlock newBlock = this.GetOrCreateBlock(currentOperation.Offset, false);
          currentBlock.Statements.Add(newBlock);
          currentBlock = newBlock;
        }
        this.ParseInstruction(currentBlock);
      }
      this.TurnOperandStackIntoPushStatements(currentBlock);
      this.localVariablesAndTemporaries = new List<ILocalDefinition>(this.ilMethodBody.LocalVariables);
      for (int i = 0, n = this.localVariablesAndTemporaries.Count; i < n; i++)
        this.localVariablesAndTemporaries[i] = this.GetLocalWithSourceName(this.localVariablesAndTemporaries[i]);
      return this.Transform(result);
    }

    ICreateObjectInstance/*?*/ GetICreateObjectInstance(IStatement statement) {
      IExpressionStatement expressionStatement = statement as IExpressionStatement;
      if (expressionStatement != null) {
        IAssignment assignment = expressionStatement.Expression as IAssignment;
        if (assignment == null) return null;
        ICreateObjectInstance createObjectInstance = assignment.Source as ICreateObjectInstance;
        return createObjectInstance;
      }
      ILocalDeclarationStatement localDeclaration = statement as ILocalDeclarationStatement;
      if (localDeclaration != null) {
        ICreateObjectInstance createObjectInstance = localDeclaration.InitialValue as ICreateObjectInstance;
        return createObjectInstance;
      }
      return null;
    }

    Hashtable<ILocalDefinition, LocalDefinition> localMap = new Hashtable<ILocalDefinition, LocalDefinition>();

    private ILocalDefinition GetLocalWithSourceName(ILocalDefinition localDef) {
      if (this.sourceLocationProvider == null) return localDef;
      var mutableLocal = this.localMap[localDef];
      if (mutableLocal != null) return mutableLocal;
      mutableLocal = localDef as LocalDefinition;
      if (mutableLocal == null) {
        mutableLocal = new LocalDefinition();
        mutableLocal.Copy(localDef, this.host.InternFactory);
      }
      this.localMap.Add(localDef, mutableLocal);
      bool isCompilerGenerated;
      var sourceName = this.sourceLocationProvider.GetSourceNameFor(localDef, out isCompilerGenerated);
      if (sourceName != localDef.Name.Value) {
        mutableLocal.Name = this.host.NameTable.GetNameFor(sourceName);
      }
      return mutableLocal;
    }

    /// <summary>
    /// For an iterator method, find the closure class' MoveNext method and return its body.
    /// </summary>
    /// <param name="iteratorIL">The body of the iterator method, decompiled from the ILs of the iterator body.</param>
    /// <returns>Dummy.MethodBody if <paramref name="iteratorIL"/> does not fit into the code pattern of an iterator method, 
    /// or the body of the MoveNext method of the corresponding closure class if it does.
    /// </returns>
    IMethodBody FindClosureMoveNext(IBlockStatement/*!*/ iteratorIL) {
      foreach (var statement in iteratorIL.Statements) {
        ICreateObjectInstance createObjectInstance = GetICreateObjectInstance(statement);
        if (createObjectInstance == null) {
          // If the first statement in the method body is not the creation of iterator closure, return a dummy.
          // Possible corner case not handled: a local is used to hold the constant value for the initial state of the closure.
          return Dummy.MethodBody;
        }
        ITypeReference closureType/*?*/ = createObjectInstance.MethodToCall.ContainingType;
        ITypeReference unspecializedClosureType = GetUnspecializedType(closureType);
        if (!AttributeHelper.Contains(unspecializedClosureType.Attributes, closureType.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
          return Dummy.MethodBody;
        INestedTypeReference closureTypeAsNestedTypeReference = unspecializedClosureType as INestedTypeReference;
        if (closureTypeAsNestedTypeReference == null) return Dummy.MethodBody;
        ITypeReference unspecializedClosureContainingType = GetUnspecializedType(closureTypeAsNestedTypeReference.ContainingType);
        if (closureType != null && TypeHelper.TypesAreEquivalent(this.ilMethodBody.MethodDefinition.ContainingTypeDefinition, unspecializedClosureContainingType)) {
          IName MoveNextName = this.nameTable.GetNameFor("MoveNext");
          foreach (ITypeDefinitionMember member in closureType.ResolvedType.GetMembersNamed(MoveNextName, false)) {
            IMethodDefinition moveNext = member as IMethodDefinition;
            if (moveNext != null) {
              ISpecializedMethodDefinition moveNextGeneric = moveNext as ISpecializedMethodDefinition;
              if (moveNextGeneric != null)
                moveNext = moveNextGeneric.UnspecializedVersion.ResolvedMethod;
              var body = moveNext.Body;
              var sourceBody = body as SourceMethodBody; //it may already have been decompiled.
              if (sourceBody != null) body = sourceBody.ilMethodBody; //TODO: it would be nice to avoid decompiling it again
              return body;
            }
          }
        }
        return Dummy.MethodBody;
      }
      return Dummy.MethodBody;
    }

    private ITypeReference GetUnspecializedType(ITypeReference typeReference) {
      ISpecializedNestedTypeReference specializedNested = typeReference as ISpecializedNestedTypeReference;
      if (specializedNested != null)
        return specializedNested.UnspecializedVersion;
      IGenericTypeInstanceReference instanceTypeReference = typeReference as IGenericTypeInstanceReference;
      if (instanceTypeReference != null)
        return GetUnspecializedType(instanceTypeReference.GenericType);
      return typeReference;
    }

    /// <summary>
    /// Perform different phases approppriate for normal, MoveNext, or iterator source method bodies.
    /// </summary>
    /// <param name="rootBlock"></param>
    /// <returns></returns>
    protected IBlockStatement Transform(BasicBlock rootBlock) {
      new TypeInferencer((INamedTypeDefinition)this.ilMethodBody.MethodDefinition.ContainingType, this.host).Traverse(rootBlock);
      new PatternDecompiler(this, this.predecessors).Traverse(rootBlock);
      new RemoveBranchConditionLocals(this).Traverse(rootBlock);
      new Unstacker(this).Visit(rootBlock);
      new TypeInferencer((INamedTypeDefinition)this.ilMethodBody.MethodDefinition.ContainingType, this.host).Traverse(rootBlock);
      new TryCatchDecompiler(this.host.PlatformType, this.predecessors).Traverse(rootBlock);
      new IfThenElseDecompiler(this.host.PlatformType, this.predecessors).Traverse(rootBlock);
      new SwitchDecompiler(this.host.PlatformType, this.predecessors).Traverse(rootBlock);
      if ((this.options & DecompilerOptions.Loops) != 0) {
        new WhileLoopDecompiler(this.host.PlatformType, this.predecessors).Traverse(rootBlock);
        new ForLoopDecompiler(this.host.PlatformType, this.predecessors).Traverse(rootBlock);
      }
      new BlockRemover().Traverse(rootBlock);
      new DeclarationAdder().Traverse(this, rootBlock);
      new EmptyStatementRemover().Traverse(rootBlock);
      IBlockStatement result = rootBlock;
      if ((this.options & DecompilerOptions.Iterators) != 0) {
        IMethodBody moveNextILBody = this.FindClosureMoveNext(rootBlock);
        if (moveNextILBody != Dummy.MethodBody) {
          if (this.privateHelperTypesToRemove == null) this.privateHelperTypesToRemove = new List<ITypeDefinition>(1);
          this.privateHelperTypesToRemove.Add(moveNextILBody.MethodDefinition.ContainingTypeDefinition);
          var moveNextBody = new MoveNextSourceMethodBody(this.ilMethodBody, moveNextILBody, this.host, this.sourceLocationProvider, this.localScopeProvider, this.options);
          result = moveNextBody.TransformedBlock;
        }
      }
      result = new CompilationArtifactRemover(this, (this.options & DecompilerOptions.AnonymousDelegates) != 0).RemoveCompilationArtifacts((BlockStatement)result);
      var bb = result as BasicBlock;
      if (bb != null) {
        new UnreferencedLabelRemover(this).Traverse(bb);
        new TypeInferencer((INamedTypeDefinition)this.ilMethodBody.MethodDefinition.ContainingType, this.host).Traverse(bb);
      }
      return result;
    }

    private void TurnOperandStackIntoPushStatements(BasicBlock currentBlock) {
      int insertPoint = currentBlock.Statements.Count;
      while (this.operandStack.Count > 0) {
        Expression operand = this.PopOperandStack();
        MethodCall/*?*/ call = operand as MethodCall;
        if (call != null && call.MethodToCall.Type.TypeCode == PrimitiveTypeCode.Void) {
          ExpressionStatement expressionStatement = new ExpressionStatement();
          expressionStatement.Expression = operand;
          currentBlock.Statements.Insert(insertPoint, expressionStatement);
        } else {
          PushStatement push = new PushStatement();
          push.ValueToPush = operand;
          currentBlock.Statements.Insert(insertPoint, push);
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="addLabel"></param>
    /// <returns></returns>
    protected BasicBlock GetOrCreateBlock(uint offset, bool addLabel) {
      BasicBlock result;
      if (!this.blockFor.TryGetValue(offset, out result)) {
        result = new BasicBlock(offset);
        this.blockFor.Add(offset, result);
      }
      if (addLabel && result.Statements.Count == 0) {
        LabeledStatement label = new LabeledStatement();
        label.Label = this.nameTable.GetNameFor("IL_" + offset.ToString("x4"));
        label.Statement = new EmptyStatement();
        result.Statements.Add(label);
        this.targetStatementFor.Add(offset, label);
      }
      return result;
    }

    private ILabeledStatement GetTargetStatement(object label) {
      LabeledStatement result;
      if (label is uint && this.targetStatementFor.TryGetValue((uint)label, out result)) return result;
      return CodeDummy.LabeledStatement;
    }

    private GotoStatement MakeGoto(IOperation currentOperation) {
      GotoStatement gotoStatement = new GotoStatement();
      gotoStatement.TargetStatement = this.GetTargetStatement(currentOperation.Value);
      List<IGotoStatement> values;
      var found = this.predecessors.TryGetValue(gotoStatement.TargetStatement, out values);
      if (!found) {
        var l = new List<IGotoStatement>();
        l.Add(gotoStatement);
        this.predecessors.Add(gotoStatement.TargetStatement, l);
      } else {
        values.Add(gotoStatement);
      }
      return gotoStatement;
    }

    private Expression ParseAddition(OperationCode currentOpcode) {
      Addition addition = new Addition();
      addition.CheckOverflow = currentOpcode != OperationCode.Add;
      if (currentOpcode == OperationCode.Add_Ovf_Un) {
        addition.TreatOperandsAsUnsignedIntegers = true; //force use of unsigned addition, even for cases where the operands are expressions that result in signed values
        return this.ParseUnsignedBinaryOperation(addition);
      } else
        return this.ParseBinaryOperation(addition);
    }

    private Expression ParseAddressOf(IOperation currentOperation) {
      AddressableExpression addressableExpression = new AddressableExpression();
      if (currentOperation.Value == null) {
        Debug.Assert(currentOperation.OperationCode == OperationCode.Ldarg || currentOperation.OperationCode == OperationCode.Ldarga_S);
        addressableExpression.Definition = new ThisReference();
      } else
        addressableExpression.Definition = currentOperation.Value;
      if (currentOperation.OperationCode == OperationCode.Ldflda || currentOperation.OperationCode == OperationCode.Ldvirtftn)
        addressableExpression.Instance = this.PopOperandStack();
      if (currentOperation.OperationCode == OperationCode.Ldloca || currentOperation.OperationCode == OperationCode.Ldloca_S) {
        var local = this.GetLocalWithSourceName((ILocalDefinition)currentOperation.Value);
        addressableExpression.Definition = local;
        this.numberOfReferences[local] = this.numberOfReferences.ContainsKey(local) ? this.numberOfReferences[local] + 1 : 1;
        //Treat this as an assignment as well, so that the local does not get deleted because it contains a constant and has only one assignment to it.
        this.numberOfAssignments[local] = this.numberOfAssignments.ContainsKey(local) ? this.numberOfAssignments[local] + 1 : 1;
      }
      return new AddressOf() { Expression = addressableExpression };
    }

    private Expression ParseAddressDereference(IOperation currentOperation) {
      ITypeReference elementType = null;
      switch (currentOperation.OperationCode) {
        case OperationCode.Ldind_I: elementType = this.platformType.SystemIntPtr; break;
        case OperationCode.Ldind_I1: elementType = this.platformType.SystemInt8; break;
        case OperationCode.Ldind_I2: elementType = this.platformType.SystemInt16; break;
        case OperationCode.Ldind_I4: elementType = this.platformType.SystemInt32; break;
        case OperationCode.Ldind_I8: elementType = this.platformType.SystemInt64; break;
        case OperationCode.Ldind_R4: elementType = this.platformType.SystemFloat32; break;
        case OperationCode.Ldind_R8: elementType = this.platformType.SystemFloat64; break;
        case OperationCode.Ldind_U1: elementType = this.platformType.SystemUInt8; break;
        case OperationCode.Ldind_U2: elementType = this.platformType.SystemUInt16; break;
        case OperationCode.Ldind_U4: elementType = this.platformType.SystemUInt32; break;
      }
      AddressDereference result = new AddressDereference();
      result.Address = this.PopOperandStack();
      result.Alignment = this.alignment;
      result.IsVolatile = this.sawVolatile;
      //Capture the element type. The pointer might be untyped, in which case the instruction is the only point where the element type is known.
      if (elementType != null) result.Type = elementType; //else: The type inferencer will fill in the type once the pointer type is known.
      this.alignment = 0;
      this.sawVolatile = false;
      return result;
    }

    private Expression ParseArrayCreate(IOperation currentOperation) {
      IArrayTypeReference arrayType = (IArrayTypeReference)currentOperation.Value;
      CreateArray result = new CreateArray();
      result.ElementType = arrayType.ElementType;
      result.Rank = arrayType.Rank;
      result.Type = arrayType;
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

    private Expression ParseArrayElementAddres(IOperation currentOperation) {
      AddressOf result = new AddressOf();
      result.ObjectControlsMutability = this.sawReadonly;
      AddressableExpression addressableExpression = new AddressableExpression();
      result.Expression = addressableExpression;
      ArrayIndexer indexer = this.ParseArrayIndexer(currentOperation);
      addressableExpression.Definition = indexer;
      addressableExpression.Instance = indexer.IndexedObject;
      this.sawReadonly = false;
      return result;
    }

    private ArrayIndexer ParseArrayIndexer(IOperation currentOperation) {
      uint rank = 1;
      IArrayTypeReference/*?*/ arrayType = currentOperation.Value as IArrayTypeReference;
      if (arrayType != null) rank = arrayType.Rank;
      ArrayIndexer result = new ArrayIndexer();
      for (uint i = 0; i < rank; i++)
        result.Indices.Add(this.PopOperandStack());
      result.Indices.Reverse();
      result.IndexedObject = this.PopOperandStack();
      return result;
    }

    private Statement ParseArraySet(IOperation currentOperation) {
      ExpressionStatement result = new ExpressionStatement();
      Assignment assignment = new Assignment();
      result.Expression = assignment;
      assignment.Source = this.PopOperandStack();
      TargetExpression targetExpression = new TargetExpression();
      assignment.Target = targetExpression;
      ArrayIndexer indexer = this.ParseArrayIndexer(currentOperation);
      targetExpression.Definition = indexer;
      targetExpression.Instance = indexer.IndexedObject;
      return result;
    }

    private Statement ParseAssignment(IOperation currentOperation) {
      TargetExpression target = new TargetExpression();
      ITypeReference/*?*/ elementType = null;
      target.Alignment = this.alignment;
      target.Definition = currentOperation.Value;
      target.IsVolatile = this.sawVolatile;
      Assignment assignment = new Assignment();
      assignment.Target = target;
      assignment.Source = this.PopOperandStack();
      ExpressionStatement result = new ExpressionStatement();
      result.Expression = assignment;
      switch (currentOperation.OperationCode) {
        case OperationCode.Stfld:
          target.Instance = this.PopOperandStack();
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
          ArrayIndexer indexer = this.ParseArrayIndexer(currentOperation);
          target.Definition = indexer;
          target.Instance = indexer.IndexedObject;
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
        case OperationCode.Stind_Ref:
        case OperationCode.Stobj:
          AddressDereference addressDereference = new AddressDereference();
          addressDereference.Address = this.PopOperandStack();
          addressDereference.Alignment = this.alignment;
          addressDereference.IsVolatile = this.sawVolatile;
          //capture the element type. The pointer might be untyped, in which case the instruction is the only point where the element type is known.
          if (elementType != null) addressDereference.Type = elementType; //else: The type inferencer will fill in the type once the pointer type is known.
          target.Definition = addressDereference;
          break;
        case OperationCode.Stloc:
        case OperationCode.Stloc_0:
        case OperationCode.Stloc_1:
        case OperationCode.Stloc_2:
        case OperationCode.Stloc_3:
        case OperationCode.Stloc_S:
          var local = this.GetLocalWithSourceName((ILocalDefinition)target.Definition);
          target.Definition = local;
          this.numberOfAssignments[local] =
            this.numberOfAssignments.ContainsKey(local) ?
            this.numberOfAssignments[local] + 1 :
            1;
          break;
      }
      this.alignment = 0;
      this.sawVolatile = false;
      return result;
    }

    private static bool TypesAreClrCompatible(ITypeReference sourceType, ITypeReference targetType) {
      if (sourceType == targetType) return true;
      if (sourceType.InternedKey == targetType.InternedKey) return true;
      if (sourceType.TypeCode == PrimitiveTypeCode.Boolean && TypeHelper.IsPrimitiveInteger(targetType)) return true;
      if (TypeHelper.IsPrimitiveInteger(sourceType) && targetType.TypeCode == PrimitiveTypeCode.Boolean) return true;
      return false;
    }

    internal readonly Dictionary<ILocalDefinition, int> numberOfAssignments = new Dictionary<ILocalDefinition, int>();

    private BinaryOperation ParseBinaryOperation(BinaryOperation binaryOperation) {
      binaryOperation.RightOperand = this.PopOperandStack();
      binaryOperation.LeftOperand = this.PopOperandStack();
      return binaryOperation;
    }

    private Expression ParseBinaryOperation(OperationCode currentOpcode) {
      switch (currentOpcode) {
        default:
          Debug.Assert(false);
          goto case OperationCode.Xor;
        case OperationCode.Add:
        case OperationCode.Add_Ovf:
        case OperationCode.Add_Ovf_Un:
          return this.ParseAddition(currentOpcode);
        case OperationCode.And:
          return this.ParseBinaryOperation(new BitwiseAnd());
        case OperationCode.Ceq:
          return this.ParseBinaryOperation(new Equality());
        case OperationCode.Cgt:
          return this.ParseBinaryOperation(new GreaterThan());
        case OperationCode.Cgt_Un:
          return this.ParseBinaryOperation(new GreaterThan() { IsUnsignedOrUnordered = true });
        case OperationCode.Clt:
          return this.ParseBinaryOperation(new LessThan());
        case OperationCode.Clt_Un:
          return this.ParseBinaryOperation(new LessThan() { IsUnsignedOrUnordered = true });
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

    private Statement ParseBinaryConditionalBranch(IOperation currentOperation) {
      BinaryOperation condition;
      switch (currentOperation.OperationCode) {
        case OperationCode.Beq:
        case OperationCode.Beq_S:
          condition = this.ParseBinaryOperation(new Equality());
          break;
        case OperationCode.Bge:
        case OperationCode.Bge_S:
          condition = this.ParseBinaryOperation(new GreaterThanOrEqual());
          break;
        case OperationCode.Bge_Un:
        case OperationCode.Bge_Un_S:
          condition = this.ParseUnsignedBinaryOperation(new GreaterThanOrEqual());
          break;
        case OperationCode.Bgt:
        case OperationCode.Bgt_S:
          condition = this.ParseBinaryOperation(new GreaterThan());
          break;
        case OperationCode.Bgt_Un:
        case OperationCode.Bgt_Un_S:
          condition = this.ParseUnsignedBinaryOperation(new GreaterThan());
          break;
        case OperationCode.Ble:
        case OperationCode.Ble_S:
          condition = this.ParseBinaryOperation(new LessThanOrEqual());
          break;
        case OperationCode.Ble_Un:
        case OperationCode.Ble_Un_S:
          condition = this.ParseUnsignedBinaryOperation(new LessThanOrEqual());
          break;
        case OperationCode.Blt:
        case OperationCode.Blt_S:
          condition = this.ParseBinaryOperation(new LessThan());
          break;
        case OperationCode.Blt_Un:
        case OperationCode.Blt_Un_S:
          condition = this.ParseUnsignedBinaryOperation(new LessThan());
          break;
        case OperationCode.Bne_Un:
        case OperationCode.Bne_Un_S:
        default:
          condition = this.ParseBinaryOperation(new NotEquality());
          break;
      }
      condition.Type = this.platformType.SystemBoolean;
      if (this.host.PreserveILLocations) {
        condition.Locations.Add(currentOperation.Location);
      }
      GotoStatement gotoStatement = MakeGoto(currentOperation);
      ConditionalStatement ifStatement = new ConditionalStatement();
      ifStatement.Condition = condition;
      ifStatement.TrueBranch = gotoStatement;
      ifStatement.FalseBranch = new EmptyStatement();
      return ifStatement;
    }

    private Expression ParseBoundExpression(IOperation currentOperation) {
      if (currentOperation.Value == null)
        return new ThisReference();
      BoundExpression result = new BoundExpression();
      result.Alignment = this.alignment;
      result.Definition = currentOperation.Value;
      result.IsVolatile = this.sawVolatile;
      var parameter = result.Definition as IParameterDefinition;
      if (parameter != null) {
        result.Type = parameter.Type;
        if (parameter.IsByReference) result.Type = Immutable.ManagedPointerType.GetManagedPointerType(result.Type, this.host.InternFactory);
      } else {
        var local = result.Definition as ILocalDefinition;
        if (local != null) {
          result.Definition = this.GetLocalWithSourceName(local);
          result.Type = local.Type;
          if (local.IsReference) result.Type = Immutable.ManagedPointerType.GetManagedPointerType(result.Type, this.host.InternFactory);
        } else {
          var field = (IFieldReference)result.Definition;
          result.Type = field.Type;
        }
      }
      switch (currentOperation.OperationCode) {
        case OperationCode.Ldfld:
          result.Instance = this.PopOperandStack();
          break;
        case OperationCode.Ldloc:
        case OperationCode.Ldloc_0:
        case OperationCode.Ldloc_1:
        case OperationCode.Ldloc_2:
        case OperationCode.Ldloc_3:
        case OperationCode.Ldloc_S:
          this.numberOfReferences[result.Definition] =
            this.numberOfReferences.ContainsKey(result.Definition) ?
            this.numberOfReferences[result.Definition] + 1 :
            1;
          break;
      }
      this.alignment = 0;
      this.sawVolatile = false;
      return result;
    }

    internal readonly Dictionary<object, int> numberOfReferences = new Dictionary<object, int>();

    private MethodCall ParseCall(IOperation currentOperation) {
      IMethodReference methodRef = (IMethodReference)currentOperation.Value;
      MethodCall result = new MethodCall();
      result.IsTailCall = this.sawTailCall;
      foreach (var par in methodRef.Parameters)
        result.Arguments.Add(this.PopOperandStack());
      foreach (var par in methodRef.ExtraParameters)
        result.Arguments.Add(this.PopOperandStack());
      result.Arguments.Reverse();
      result.IsVirtualCall = currentOperation.OperationCode == OperationCode.Callvirt;
      result.MethodToCall = methodRef;
      if (!methodRef.IsStatic)
        result.ThisArgument = this.PopOperandStack();
      else
        result.IsStaticCall = true;
      result.Type = methodRef.Type;
      this.sawTailCall = false;
      return result;
    }

    private Expression ParseCastIfPossible(IOperation currentOperation) {
      CastIfPossible result = new CastIfPossible();
      result.ValueToCast = this.PopOperandStack();
      result.TargetType = (ITypeReference)currentOperation.Value;
      return result;
    }

    private Expression ParseCompileTimeConstant(IOperation currentOperation) {
      CompileTimeConstant result = new CompileTimeConstant();
      result.Value = currentOperation.Value;
      result.Type = this.platformType.SystemInt32;
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
        case OperationCode.Ldc_I8: result.Type = this.platformType.SystemInt64; break;
        case OperationCode.Ldc_R4: result.Type = this.platformType.SystemFloat32; break;
        case OperationCode.Ldc_R8: result.Type = this.platformType.SystemFloat64; break;
        case OperationCode.Ldnull: result.Type = this.platformType.SystemObject; break;
        case OperationCode.Ldstr: result.Type = this.platformType.SystemString; break;
      }
      return result;
    }

    private Expression ParseConversion(IOperation currentOperation) {
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
          ((Expression)result.ValueToConvert).Type = (ITypeReference)currentOperation.Value;
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
      switch (typeReference.TypeCode) {
        case PrimitiveTypeCode.Boolean: return ((int)ob) == 1;
        case PrimitiveTypeCode.Char: return (char)((int)ob);
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
      return result;
    }

    private Expression ParseCreateObjectInstance(IOperation currentOperation) {
      CreateObjectInstance result = new CreateObjectInstance();
      result.MethodToCall = (IMethodReference)currentOperation.Value;
      foreach (var par in result.MethodToCall.Parameters)
        result.Arguments.Add(this.PopOperandStack());
      result.Arguments.Reverse();
      return result;
    }

    private Statement ParseDup() {
      PushStatement result = new PushStatement();
      result.ValueToPush = new Dup();
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
      GetValueOfTypedReference result = new GetValueOfTypedReference();
      result.TargetType = (ITypeReference)currentOperation.Value;
      result.TypedReference = this.PopOperandStack();
      return result;
    }

    private Statement ParseInitObject(IOperation currentOperation) {
      Assignment assignment = new Assignment();
      assignment.Target = new TargetExpression() { Definition = new AddressDereference() { Address = this.PopOperandStack() } };
      assignment.Source = new DefaultValue() { DefaultValueType = (ITypeReference)currentOperation.Value };
      return new ExpressionStatement() { Expression = assignment };
    }

    private Expression ParseMakeTypedReference(IOperation currentOperation) {
      MakeTypedReference result = new MakeTypedReference();
      Expression operand = this.PopOperandStack();
      var type = (ITypeReference)currentOperation.Value;
      if (type.IsValueType)
        type = Immutable.ManagedPointerType.GetManagedPointerType(type, this.host.InternFactory);
      operand.Type = type;
      result.Operand = operand;
      return result;
    }

    private Expression ParseMultiplication(OperationCode currentOpcode) {
      Multiplication multiplication = new Multiplication();
      multiplication.CheckOverflow = currentOpcode != OperationCode.Mul;
      if (currentOpcode == OperationCode.Mul_Ovf_Un) {
        multiplication.TreatOperandsAsUnsignedIntegers = true;
        return this.ParseUnsignedBinaryOperation(multiplication);
      } else
        return this.ParseBinaryOperation(multiplication);
    }

    private Expression ParsePointerCall(IOperation currentOperation) {
      IFunctionPointerTypeReference funcPointerRef = (IFunctionPointerTypeReference)currentOperation.Value;
      PointerCall result = new PointerCall();
      result.IsTailCall = this.sawTailCall;
      Expression pointer = this.PopOperandStack();
      pointer.Type = funcPointerRef;
      foreach (var par in funcPointerRef.Parameters)
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

    /// <summary>
    /// Parse instructions and put them into an expression tree until an assignment, void call, branch target, or branch is encountered.
    /// Returns true if the parsed statement is last of the current basic block. This happens when the next statement is a branch
    /// target, or if the parsed statement could transfers control to anything but the following statement.
    /// </summary>
    private void ParseInstruction(BasicBlock currentBlock) {
      Statement/*?*/ statement = null;
      Expression/*?*/ expression = null;
      IOperation currentOperation = this.operationEnumerator.Current;
      OperationCode currentOpcode = currentOperation.OperationCode;
      if (this.host.PreserveILLocations) {
        if (this.lastLocation == null)
          this.lastLocation = currentOperation.Location;
      } else {
        if (this.sourceLocationProvider != null && this.lastSourceLocation == null) {
          foreach (var sourceLocation in this.sourceLocationProvider.GetPrimarySourceLocationsFor(currentOperation.Location)) {
            if (sourceLocation.StartLine != 0x00feefee) {
              this.lastSourceLocation = sourceLocation;
              break;
            }
          }
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
        case OperationCode.Ldelema:
          expression = this.ParseArrayElementAddres(currentOperation);
          break;

        case OperationCode.Array_Create:
        case OperationCode.Array_Create_WithLowerBound:
        case OperationCode.Newarr:
          expression = this.ParseArrayCreate(currentOperation);
          break;

        case OperationCode.Array_Get:
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
          expression = this.ParseArrayIndexer(currentOperation);
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
          this.PopOperandStack();
          Debug.Assert(false); //if code out there actually uses this, I need to know sooner rather than later.
          //TODO: need a code model statement for this instruction.
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
          statement = this.ParseDup();
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
          Debug.Assert(false); //if code out there actually uses this, I need to know sooner rather than later.
          //TODO: need a code model statement for this instruction.
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
          expression = this.ParseBoundExpression(currentOperation);
          break;

        case OperationCode.Ldarga:
        case OperationCode.Ldarga_S:
        case OperationCode.Ldflda:
        case OperationCode.Ldsflda:
        case OperationCode.Ldloca:
        case OperationCode.Ldloca_S:
        case OperationCode.Ldftn:
        case OperationCode.Ldvirtftn:
          expression = this.ParseAddressOf(currentOperation);
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
          Debug.Assert(false); //if code out there actually uses this, I need to know sooner rather than later.
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
          this.alignment = (byte)currentOperation.Value;
          break;

        case OperationCode.Volatile_:
          this.sawVolatile = true;
          break;

      }
      if (expression != null) {
        if (this.host.PreserveILLocations) {
          expression.Locations.Add(currentOperation.Location);
        }
        this.operandStack.Push(expression);
      } else if (statement != null) {
        this.TurnOperandStackIntoPushStatements(currentBlock);
        currentBlock.Statements.Add(statement);
        if (this.host.PreserveILLocations) {
          if (this.lastLocation != null) {
            statement.Locations.Add(this.lastLocation);
            this.lastLocation = null;
          }
        } else if (this.lastSourceLocation != null) {
          statement.Locations.Add(this.lastSourceLocation);
          this.lastSourceLocation = null;
        }
      }
    }

    private Statement ParseReturn() {
      ReturnStatement result = new ReturnStatement();
      if (this.MethodDefinition.Type.TypeCode != PrimitiveTypeCode.Void)
        result.Expression = this.PopOperandStack();
      return result;
    }

    private static Expression ParseSizeOf(IOperation currentOperation) {
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

    private Expression ParseSubtraction(OperationCode currentOpcode) {
      Subtraction subtraction = new Subtraction();
      subtraction.CheckOverflow = currentOpcode != OperationCode.Sub;
      if (currentOpcode == OperationCode.Sub_Ovf_Un) {
        subtraction.TreatOperandsAsUnsignedIntegers = true;
        return this.ParseUnsignedBinaryOperation(subtraction);
      } else
        return this.ParseBinaryOperation(subtraction);
    }

    private Statement ParseSwitchInstruction(IOperation operation) {
      SwitchInstruction result = new SwitchInstruction();
      result.switchExpression = this.PopOperandStack();
      uint[] branches = (uint[])operation.Value;
      foreach (uint targetAddress in branches) {
        BasicBlock bb = this.GetOrCreateBlock(targetAddress, true);
        var gotoBB = new GotoStatement() { TargetStatement = (LabeledStatement)bb.Statements[0] };
        result.switchCases.Add(gotoBB);
      }
      return result;
    }

    private Statement ParseThrow() {
      ThrowStatement result = new ThrowStatement();
      result.Exception = this.PopOperandStack();
      return result;
    }

    private static Expression ParseToken(IOperation currentOperation) {
      TokenOf result = new TokenOf();
      result.Definition = currentOperation.Value;
      return result;
    }

    private Statement ParseUnaryConditionalBranch(IOperation currentOperation) {
      Expression condition = this.PopOperandStack();
      var castIfPossible = condition as CastIfPossible;
      if (castIfPossible != null) {
        condition = new CheckIfInstance() {
          Locations = castIfPossible.Locations,
          Operand = castIfPossible.ValueToCast,
          TypeToCheck = castIfPossible.TargetType,
        };
      } else if (condition.Type != Dummy.TypeReference && condition.Type.TypeCode != PrimitiveTypeCode.Boolean) {
        var defaultValue = new DefaultValue() { DefaultValueType = condition.Type, Type = condition.Type };
        condition = new NotEquality() { LeftOperand = condition, RightOperand = defaultValue };
      }
      condition.Type = this.platformType.SystemBoolean;
      GotoStatement gotoStatement = MakeGoto(currentOperation);
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
      GotoStatement gotoStatement = this.MakeGoto(currentOperation);
      return gotoStatement;
    }

    private BinaryOperation ParseUnsignedBinaryOperation(BinaryOperation binaryOperation) {
      binaryOperation.RightOperand = this.PopOperandStackAsUnsigned();
      binaryOperation.LeftOperand = this.PopOperandStackAsUnsigned();
      return binaryOperation;
    }

    private Expression ParseUnaryOperation(UnaryOperation unaryOperation) {
      unaryOperation.Operand = this.PopOperandStack();
      return unaryOperation;
    }

    private Expression ParseVectorLength() {
      VectorLength result = new VectorLength();
      result.Vector = this.PopOperandStack();
      return result;
    }

    private Stack<Expression> operandStack = new Stack<Expression>();

    private IEnumerator<IOperation> operationEnumerator;

    private Expression PopOperandStack() {
      if (this.operandStack.Count == 0)
        return new Pop();
      else
        return this.operandStack.Pop();
    }

    private Expression PopOperandStackAsUnsigned() {
      Expression result;
      if (this.operandStack.Count == 0)
        return new PopAsUnsigned();
      else
        result = this.operandStack.Pop();
      return new ConvertToUnsigned(result);
    }

    private Dictionary<uint, LabeledStatement> targetStatementFor = new Dictionary<uint, LabeledStatement>();
    /// <summary>
    /// Predecessors of labeled statements.
    /// </summary>
    protected Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors = new Dictionary<ILabeledStatement, List<IGotoStatement>>();
  }

  /// <summary>
  /// A metadata (IL) representation along with a source level representation of the body of an iterator method/property.
  /// </summary>
  internal class MoveNextSourceMethodBody : SourceMethodBody {

    /// <summary>
    /// The method body of the original iterator method. 
    /// </summary>
    internal IMethodBody iteratorMethodBody;
    /// <summary>
    /// Allocates a metadata (IL) representation along with a source level representation of the body of an iterator method/property/event accessor.
    /// </summary>
    /// <param name="iteratorMethodBody"> The method body of the iterator method, to which this MoveNextSourceMethodBody corresponds.</param>
    /// <param name="ilMethodBody">The method body of MoveNext whose IL operations should be decompiled into a block of statements that will be the
    /// result of the Block property of the resulting source method body. More importantly, the decompiled body for the original iterator method 
    /// is accessed by the TransformedBlock property.</param>
    /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map some kinds of ILocation objects to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="localScopeProvider">An object that can provide information about the local scopes of a method.</param>
    /// <param name="options">Set of options that control decompilation.</param>
    public MoveNextSourceMethodBody(IMethodBody iteratorMethodBody, IMethodBody ilMethodBody, IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, ILocalScopeProvider/*?*/ localScopeProvider, DecompilerOptions options = DecompilerOptions.None)
      : base(ilMethodBody, host, sourceLocationProvider, localScopeProvider, options) {
      this.iteratorMethodBody = iteratorMethodBody;
    }

    /// <summary>
    /// Computes the method body of the iterator method of which the defining class of this MoveNext method is a closure class.
    /// </summary>
    internal IBlockStatement TransformedBlock {
      // TODO: check for the conditions that we have assumed, such as there must be a switch statement, and return a dummy
      // if such conditions are not satisfied. 
      get {
        IBlockStatement block = this.Block;
        block = DecompileMoveNext(block);
        BasicBlock rootBlock = GetOrCreateBlock(0, false);
        block = DuplicateMoveNextForIteratorMethod(rootBlock);

        block = this.AddLocalDeclarationIfNecessary(block);
        new TypeInferencer((INamedTypeDefinition)this.iteratorMethodBody.MethodDefinition.ContainingType, this.host).Traverse(block);
        return block;
      }
    }

    private IBlockStatement AddLocalDeclarationIfNecessary(IBlockStatement block) {
      return new ClosureLocalVariableDeclarationAdder(this).Visit(block);
    }

    private IBlockStatement DecompileMoveNext(IBlockStatement block) {
      return new MoveNextDecompiler(this.ilMethodBody.MethodDefinition.ContainingTypeDefinition, this.host).Decompile((BlockStatement)block);
    }
    private IBlockStatement DuplicateMoveNextForIteratorMethod(BlockStatement rootBlock) {
      return MoveNextToIteratorBlockTransformer.Transform(iteratorMethodBody.MethodDefinition, this.ilMethodBody.MethodDefinition, rootBlock, this.host);
    }
  }

}
