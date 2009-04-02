//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Cci.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// Use the inherited methods from MetadataMutator to walk everything down to the method body level,
  /// then take over and define Visit methods for all of the structures in the
  /// code model that pertain to method bodies.
  /// </summary>
  public class CodeMutator : MetadataMutator {

    private CreateMutableType createMutableType;

    protected readonly SourceMethodBodyProvider/*?*/ ilToSourceProvider;
    protected readonly SourceToILConverterProvider/*?*/ sourceToILProvider;
    protected readonly ISourceLocationProvider/*?*/ sourceLocationProvider;

    public CodeMutator(IMetadataHost host)
      : base(host) {
      createMutableType = new CreateMutableType(this, true);
    }

    public CodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host, copyOnlyIfNotAlreadyMutable) {
      createMutableType = new CreateMutableType(this, !copyOnlyIfNotAlreadyMutable);
    }

    public CodeMutator(IMetadataHost host, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host) {
      this.ilToSourceProvider = ilToSourceProvider;
      this.sourceToILProvider = sourceToILProvider;
      this.sourceLocationProvider = sourceLocationProvider;
      createMutableType = new CreateMutableType(this, true);
    }

    public CodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, copyOnlyIfNotAlreadyMutable) {
      this.ilToSourceProvider = ilToSourceProvider;
      this.sourceToILProvider = sourceToILProvider;
      this.sourceLocationProvider = sourceLocationProvider;
      createMutableType = new CreateMutableType(this, !copyOnlyIfNotAlreadyMutable);
    }

    #region Virtual methods for subtypes to override, one per type in MutableCodeModel

    public virtual IExpression Visit(Addition addition) {
      return this.Visit((BinaryOperation)addition);
    }

    public virtual IAddressableExpression Visit(AddressableExpression addressableExpression) {
      object/*?*/ def = addressableExpression.Definition;
      ILocalDefinition/*?*/ loc = def as ILocalDefinition;
      if (loc != null)
        addressableExpression.Definition = this.GetMutableCopyIfItExists(loc);
      else {
        IParameterDefinition/*?*/ par = def as IParameterDefinition;
        if (par != null)
          addressableExpression.Definition = this.GetMutableCopyIfItExists(par);
        else {
          IFieldReference/*?*/ field = def as IFieldReference;
          if (field != null)
            addressableExpression.Definition = this.Visit(this.GetMutableCopy(field));
          else {
            IArrayIndexer/*?*/ indexer = def as IArrayIndexer;
            if (indexer != null)
              addressableExpression.Definition = this.Visit(indexer);
            else {
              IAddressDereference/*?*/ adr = def as IAddressDereference;
              if (adr != null)
                addressableExpression.Definition = this.Visit(adr);
              else {
                IMethodReference/*?*/ meth = def as IMethodReference;
                if (meth != null)
                  addressableExpression.Definition = this.Visit(meth);
                else {
                  IThisReference/*?*/ thisRef = def as IThisReference;
                  if (thisRef != null)
                    addressableExpression.Definition = this.Visit(thisRef);
                }
              }
            }
          }
        }
      }
      if (addressableExpression.Instance != null)
        addressableExpression.Instance = this.Visit(addressableExpression.Instance);
      addressableExpression.Type = this.Visit(addressableExpression.Type);
      return addressableExpression;
    }

    public virtual IExpression Visit(AddressDereference addressDereference) {
      addressDereference.Address = this.Visit(addressDereference.Address);
      addressDereference.Type = this.Visit(addressDereference.Type);
      return addressDereference;
    }

    public virtual IExpression Visit(AddressOf addressOf) {
      addressOf.Expression = this.Visit(addressOf.Expression);
      addressOf.Type = this.Visit(addressOf.Type);
      return addressOf;
    }

    public virtual IExpression Visit(AnonymousDelegate anonymousDelegate) {
      this.path.Push(anonymousDelegate);
      anonymousDelegate.Parameters = this.Visit(anonymousDelegate.Parameters);
      anonymousDelegate.Body = this.Visit(anonymousDelegate.Body);
      this.path.Pop();
      return anonymousDelegate;
    }

    public virtual IExpression Visit(ArrayIndexer arrayIndexer) {
      arrayIndexer.IndexedObject = this.Visit(arrayIndexer.IndexedObject);
      arrayIndexer.Indices = this.Visit(arrayIndexer.Indices);
      arrayIndexer.Type = this.Visit(arrayIndexer.Type);
      return arrayIndexer;
    }

    public virtual IStatement Visit(AssertStatement assertStatement) {
      assertStatement.Condition = this.Visit(assertStatement.Condition);
      return assertStatement;
    }

    public virtual IExpression Visit(Assignment assignment) {
      assignment.Target = this.Visit(assignment.Target);
      assignment.Source = this.Visit(assignment.Source);
      assignment.Type = this.Visit(assignment.Type);
      return assignment;
    }

    public virtual IStatement Visit(AssumeStatement assumeStatement) {
      assumeStatement.Condition = this.Visit(assumeStatement.Condition);
      return assumeStatement;
    }

    public virtual IExpression Visit(BaseClassReference baseClassReference) {
      baseClassReference.Type = this.Visit(baseClassReference.Type);
      return baseClassReference;
    }

    public virtual IExpression Visit(BitwiseAnd bitwiseAnd) {
      return this.Visit((BinaryOperation)bitwiseAnd);
    }

    public virtual IExpression Visit(BitwiseOr bitwiseOr) {
      return this.Visit((BinaryOperation)bitwiseOr);
    }

    public virtual IExpression Visit(BinaryOperation binaryOperation) {
      binaryOperation.LeftOperand = this.Visit(binaryOperation.LeftOperand);
      binaryOperation.RightOperand = this.Visit(binaryOperation.RightOperand);
      binaryOperation.Type = this.Visit(binaryOperation.Type);
      return binaryOperation;
    }

    public virtual IExpression Visit(BlockExpression blockExpression) {
      blockExpression.BlockStatement = this.Visit(blockExpression.BlockStatement);
      blockExpression.Expression = Visit(blockExpression.Expression);
      blockExpression.Type = this.Visit(blockExpression.Type);
      return blockExpression;
    }

    public virtual IBlockStatement Visit(BlockStatement blockStatement) {
      blockStatement.Statements = Visit(blockStatement.Statements);
      return blockStatement;
    }

    public virtual IExpression Visit(BoundExpression boundExpression) {
      if (boundExpression.Instance != null)
        boundExpression.Instance = Visit(boundExpression.Instance);
      ILocalDefinition/*?*/ loc = boundExpression.Definition as ILocalDefinition;
      if (loc != null)
        boundExpression.Definition = this.GetMutableCopyIfItExists(loc);
      else {
        IParameterDefinition/*?*/ par = boundExpression.Definition as IParameterDefinition;
        if (par != null)
          boundExpression.Definition = this.GetMutableCopyIfItExists(par);
        else {
          IFieldReference/*?*/ field = boundExpression.Definition as IFieldReference;
          boundExpression.Definition = this.Visit(field);
        }
      }
      boundExpression.Type = this.Visit(boundExpression.Type);
      return boundExpression;
    }

    public virtual IStatement Visit(BreakStatement breakStatement) {
      return breakStatement;
    }

    public virtual IExpression Visit(CastIfPossible castIfPossible) {
      castIfPossible.TargetType = Visit(castIfPossible.TargetType);
      castIfPossible.ValueToCast = Visit(castIfPossible.ValueToCast);
      castIfPossible.Type = this.Visit(castIfPossible.Type);
      return castIfPossible;
    }

    public virtual List<ICatchClause> Visit(List<CatchClause> catchClauses) {
      List<ICatchClause> newList = new List<ICatchClause>();
      foreach (var catchClause in catchClauses) {
        newList.Add(Visit(catchClause));
      }
      return newList;
    }

    public virtual ICatchClause Visit(CatchClause catchClause) {
      if (catchClause.FilterCondition != null)
        catchClause.FilterCondition = Visit(catchClause.FilterCondition);
      catchClause.Body = Visit(catchClause.Body);
      return catchClause;
    }

    public virtual IExpression Visit(CheckIfInstance checkIfInstance) {
      checkIfInstance.Operand = Visit(checkIfInstance.Operand);
      checkIfInstance.TypeToCheck = Visit(checkIfInstance.TypeToCheck);
      checkIfInstance.Type = this.Visit(checkIfInstance.Type);
      return checkIfInstance;
    }

    public virtual ICompileTimeConstant Visit(CompileTimeConstant constant) {
      constant.Type = this.Visit(constant.Type);
      return constant;
    }

    public virtual IExpression Visit(Conversion conversion) {
      conversion.ValueToConvert = Visit(conversion.ValueToConvert);
      conversion.Type = this.Visit(conversion.Type);
      return conversion;
    }

    public virtual IExpression Visit(Conditional conditional) {
      conditional.Condition = Visit(conditional.Condition);
      conditional.ResultIfTrue = Visit(conditional.ResultIfTrue);
      conditional.ResultIfFalse = Visit(conditional.ResultIfFalse);
      conditional.Type = this.Visit(conditional.Type);
      return conditional;
    }

    public virtual IStatement Visit(ConditionalStatement conditionalStatement) {
      conditionalStatement.Condition = Visit(conditionalStatement.Condition);
      conditionalStatement.TrueBranch = Visit(conditionalStatement.TrueBranch);
      conditionalStatement.FalseBranch = Visit(conditionalStatement.FalseBranch);
      return conditionalStatement;
    }

    public virtual IStatement Visit(ContinueStatement continueStatement) {
      return continueStatement;
    }

    public virtual IExpression Visit(CreateArray createArray) {
      createArray.ElementType = this.Visit(createArray.ElementType);
      createArray.Sizes = this.Visit(createArray.Sizes);
      createArray.Initializers = this.Visit(createArray.Initializers);
      createArray.Type = this.Visit(createArray.Type);
      return createArray;
    }

    public virtual IExpression Visit(CreateObjectInstance createObjectInstance) {
      createObjectInstance.MethodToCall = this.Visit(createObjectInstance.MethodToCall);
      createObjectInstance.Arguments = Visit(createObjectInstance.Arguments);
      createObjectInstance.Type = this.Visit(createObjectInstance.Type);
      return createObjectInstance;
    }

    public virtual IExpression Visit(CreateDelegateInstance createDelegateInstance) {
      createDelegateInstance.MethodToCallViaDelegate = this.Visit(createDelegateInstance.MethodToCallViaDelegate);
      if (createDelegateInstance.Instance != null)
        createDelegateInstance.Instance = Visit(createDelegateInstance.Instance);
      createDelegateInstance.Type = this.Visit(createDelegateInstance.Type);
      return createDelegateInstance;
    }

    public virtual IExpression Visit(DefaultValue defaultValue) {
      defaultValue.DefaultValueType = Visit(defaultValue.DefaultValueType);
      defaultValue.Type = this.Visit(defaultValue.Type);
      return defaultValue;
    }

    public virtual IStatement Visit(DebuggerBreakStatement debuggerBreakStatement) {
      return debuggerBreakStatement;
    }

    public virtual IExpression Visit(Division division) {
      return this.Visit((BinaryOperation)division);
    }

    public virtual IStatement Visit(DoUntilStatement doUntilStatement) {
      doUntilStatement.Body = Visit(doUntilStatement.Body);
      doUntilStatement.Condition = Visit(doUntilStatement.Condition);
      return doUntilStatement;
    }

    public virtual IStatement Visit(EmptyStatement emptyStatement) {
      return emptyStatement;
    }

    public virtual IExpression Visit(Equality equality) {
      return this.Visit((BinaryOperation)equality);
    }

    public virtual IExpression Visit(ExclusiveOr exclusiveOr) {
      return this.Visit((BinaryOperation)exclusiveOr);
    }

    public virtual List<IExpression> Visit(List<IExpression> expressions) {
      List<IExpression> newList = new List<IExpression>();
      foreach (var expression in expressions)
        newList.Add(this.Visit(expression));
      return newList;
    }

    public virtual IStatement Visit(ExpressionStatement expressionStatement) {
      expressionStatement.Expression = Visit(expressionStatement.Expression);
      return expressionStatement;
    }

    public virtual IStatement Visit(ForEachStatement forEachStatement) {
      forEachStatement.Collection = Visit(forEachStatement.Collection);
      forEachStatement.Body = Visit(forEachStatement.Body);
      return forEachStatement;
    }

    public virtual IStatement Visit(ForStatement forStatement) {
      forStatement.InitStatements = Visit(forStatement.InitStatements);
      forStatement.Condition = Visit(forStatement.Condition);
      forStatement.IncrementStatements = Visit(forStatement.IncrementStatements);
      forStatement.Body = Visit(forStatement.Body);
      return forStatement;
    }

    public virtual IExpression Visit(GetTypeOfTypedReference getTypeOfTypedReference) {
      getTypeOfTypedReference.TypedReference = Visit(getTypeOfTypedReference.TypedReference);
      getTypeOfTypedReference.Type = this.Visit(getTypeOfTypedReference.Type);
      return getTypeOfTypedReference;
    }

    public virtual IExpression Visit(GetValueOfTypedReference getValueOfTypedReference) {
      getValueOfTypedReference.TypedReference = Visit(getValueOfTypedReference.TypedReference);
      getValueOfTypedReference.TargetType = Visit(getValueOfTypedReference.TargetType);
      getValueOfTypedReference.Type = this.Visit(getValueOfTypedReference.Type);
      return getValueOfTypedReference;
    }

    public virtual IStatement Visit(GotoStatement gotoStatement) {
      return gotoStatement;
    }

    public virtual IStatement Visit(GotoSwitchCaseStatement gotoSwitchCaseStatement) {
      return gotoSwitchCaseStatement;
    }

    public virtual IExpression Visit(GreaterThan greaterThan) {
      return this.Visit((BinaryOperation)greaterThan);
    }

    public virtual IExpression Visit(GreaterThanOrEqual greaterThanOrEqual) {
      return this.Visit((BinaryOperation)greaterThanOrEqual);
    }

    public virtual IStatement Visit(LabeledStatement labeledStatement) {
      labeledStatement.Statement = Visit(labeledStatement.Statement);
      return labeledStatement;
    }

    public virtual IExpression Visit(LeftShift leftShift) {
      return this.Visit((BinaryOperation)leftShift);
    }

    public virtual IExpression Visit(LessThan lessThan) {
      return this.Visit((BinaryOperation)lessThan);
    }

    public virtual IExpression Visit(LessThanOrEqual lessThanOrEqual) {
      return this.Visit((BinaryOperation)lessThanOrEqual);
    }

    public virtual IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      localDeclarationStatement.LocalVariable = this.Visit(this.GetMutableCopy(localDeclarationStatement.LocalVariable));
      if (localDeclarationStatement.InitialValue != null)
        localDeclarationStatement.InitialValue = Visit(localDeclarationStatement.InitialValue);
      return localDeclarationStatement;
    }

    public virtual IStatement Visit(LockStatement lockStatement) {
      lockStatement.Guard = Visit(lockStatement.Guard);
      lockStatement.Body = Visit(lockStatement.Body);
      return lockStatement;
    }

    public virtual IExpression Visit(LogicalNot logicalNot) {
      return this.Visit((UnaryOperation)logicalNot);
    }

    public virtual IExpression Visit(MakeTypedReference makeTypedReference) {
      makeTypedReference.Operand = Visit(makeTypedReference.Operand);
      makeTypedReference.Type = this.Visit(makeTypedReference.Type);
      return makeTypedReference;
    }

    public override MethodBody Visit(MethodBody methodBody) {
      methodBody.MethodDefinition = this.GetCurrentMethod();
      return methodBody;
    }

    public override IMethodBody Visit(IMethodBody methodBody) {
      ISourceMethodBody sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody == null && this.ilToSourceProvider != null)
        sourceMethodBody = this.ilToSourceProvider(methodBody);
      if (sourceMethodBody != null) {
        SourceMethodBody mutableSourceMethodBody = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, null);
        mutableSourceMethodBody.Block = this.Visit(sourceMethodBody.Block);
        mutableSourceMethodBody.MethodDefinition = this.GetCurrentMethod();
        mutableSourceMethodBody.LocalsAreZeroed = methodBody.LocalsAreZeroed;
        return mutableSourceMethodBody;
      } 
      return base.Visit(methodBody);
    }

    public virtual IExpression Visit(MethodCall methodCall) {
      if (!methodCall.IsStaticCall)
        methodCall.ThisArgument = this.Visit(methodCall.ThisArgument);
      methodCall.Arguments = this.Visit(methodCall.Arguments);
      methodCall.MethodToCall = this.Visit(methodCall.MethodToCall);
      methodCall.Type = this.Visit(methodCall.Type);
      return methodCall;
    }

    public virtual IExpression Visit(Modulus modulus) {
      return this.Visit((BinaryOperation)modulus);
    }

    public virtual IExpression Visit(Multiplication multiplication) {
      return this.Visit((BinaryOperation)multiplication);
    }

    public virtual IExpression Visit(NamedArgument namedArgument) {
      namedArgument.ArgumentValue = namedArgument.ArgumentValue;
      namedArgument.Type = this.Visit(namedArgument.Type);
      return namedArgument;
    }

    public virtual IExpression Visit(NotEquality notEquality) {
      return this.Visit((BinaryOperation)notEquality);
    }

    public virtual IExpression Visit(OldValue oldValue) {
      oldValue.Expression = Visit(oldValue.Expression);
      oldValue.Type = this.Visit(oldValue.Type);
      return oldValue;
    }

    public virtual IExpression Visit(OnesComplement onesComplement) {
      return this.Visit((UnaryOperation)onesComplement);
    }

    public virtual IExpression Visit(UnaryOperation unaryOperation) {
      unaryOperation.Operand = Visit(unaryOperation.Operand);
      unaryOperation.Type = this.Visit(unaryOperation.Type);
      return unaryOperation;
    }

    public virtual IExpression Visit(OutArgument outArgument) {
      outArgument.Expression = Visit(outArgument.Expression);
      outArgument.Type = this.Visit(outArgument.Type);
      return outArgument;
    }

    public virtual IExpression Visit(PointerCall pointerCall) {
      pointerCall.Pointer = this.Visit(pointerCall.Pointer);
      pointerCall.Arguments = Visit(pointerCall.Arguments);
      pointerCall.Type = this.Visit(pointerCall.Type);
      return pointerCall;
    }

    public virtual IExpression Visit(RefArgument refArgument) {
      refArgument.Expression = Visit(refArgument.Expression);
      refArgument.Type = this.Visit(refArgument.Type);
      return refArgument;
    }

    public virtual IStatement Visit(ResourceUseStatement resourceUseStatement) {
      resourceUseStatement.ResourceAcquisitions = Visit(resourceUseStatement.ResourceAcquisitions);
      resourceUseStatement.Body = Visit(resourceUseStatement.Body);
      return resourceUseStatement;
    }

    public virtual IStatement Visit(RethrowStatement rethrowStatement) {
      return rethrowStatement;
    }

    public virtual IStatement Visit(ReturnStatement returnStatement) {
      if (returnStatement.Expression != null)
        returnStatement.Expression = Visit(returnStatement.Expression);
      return returnStatement;
    }

    public virtual IExpression Visit(ReturnValue returnValue) {
      returnValue.Type = this.Visit(returnValue.Type);
      return returnValue;
    }

    public virtual IExpression Visit(RightShift rightShift) {
      return this.Visit((BinaryOperation)rightShift);
    }

    public virtual IExpression Visit(RuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      runtimeArgumentHandleExpression.Type = this.Visit(runtimeArgumentHandleExpression.Type);
      return runtimeArgumentHandleExpression;
    }

    public virtual IExpression Visit(SizeOf sizeOf) {
      sizeOf.TypeToSize = Visit(sizeOf.TypeToSize);
      sizeOf.Type = this.Visit(sizeOf.Type);
      return sizeOf;
    }

    public virtual IExpression Visit(StackArrayCreate stackArrayCreate) {
      stackArrayCreate.ElementType = Visit(stackArrayCreate.ElementType);
      stackArrayCreate.Size = Visit(stackArrayCreate.Size);
      stackArrayCreate.Type = this.Visit(stackArrayCreate.Type);
      return stackArrayCreate;
    }

    public virtual IExpression Visit(Subtraction subtraction) {
      return this.Visit((BinaryOperation)subtraction);
    }

    public virtual List<ISwitchCase> Visit(List<SwitchCase> switchCases) {
      List<ISwitchCase> newList = new List<ISwitchCase>();
      foreach (var switchCase in switchCases)
        newList.Add(Visit(switchCase));
      return newList;
    }

    public virtual ISwitchCase Visit(SwitchCase switchCase) {
      if (!switchCase.IsDefault)
        switchCase.Expression = Visit(switchCase.Expression);
      switchCase.Body = Visit(switchCase.Body);
      return switchCase;
    }

    public virtual IStatement Visit(SwitchStatement switchStatement) {
      switchStatement.Expression = Visit(switchStatement.Expression);
      switchStatement.Cases = Visit(switchStatement.Cases);
      return switchStatement;
    }

    public virtual ITargetExpression Visit(TargetExpression targetExpression) {
      object/*?*/ def = targetExpression.Definition;
      ILocalDefinition/*?*/ loc = def as ILocalDefinition;
      if (loc != null)
        targetExpression.Definition = this.GetMutableCopyIfItExists(loc);
      else {
        IParameterDefinition/*?*/ par = targetExpression.Definition as IParameterDefinition;
        if (par != null)
          targetExpression.Definition = this.GetMutableCopyIfItExists(par);
        else {
          IFieldReference/*?*/ field = targetExpression.Definition as IFieldReference;
          if (field != null) {
            if (targetExpression.Instance != null)
              targetExpression.Instance = this.Visit(targetExpression.Instance);
            targetExpression.Definition = this.Visit(field);
          } else {
            IArrayIndexer/*?*/ indexer = def as IArrayIndexer;
            if (indexer != null) {
              targetExpression.Definition = this.Visit(indexer);
              indexer = targetExpression.Definition as IArrayIndexer;
              if (indexer != null) {
                targetExpression.Instance = indexer.IndexedObject;
                targetExpression.Type = indexer.Type;
                return targetExpression;
              }
            } else {
              IAddressDereference/*?*/ adr = def as IAddressDereference;
              if (adr != null)
                targetExpression.Definition = this.Visit(adr);
              else {
                IPropertyDefinition/*?*/ prop = def as IPropertyDefinition;
                if (prop != null) {
                  if (targetExpression.Instance != null)
                    targetExpression.Instance = this.Visit(targetExpression.Instance);
                  targetExpression.Definition = this.Visit(this.GetMutableCopy(prop));
                }
              }
            }
          }
        }
      }
      targetExpression.Type = this.Visit(targetExpression.Type);
      return targetExpression;
    }

    public virtual IExpression Visit(ThisReference thisReference) {
      thisReference.Type = this.Visit(thisReference.Type);
      return thisReference;
    }

    public virtual IStatement Visit(ThrowStatement throwStatement) {
      if (throwStatement.Exception != null)
        throwStatement.Exception = Visit(throwStatement.Exception);
      return throwStatement;
    }

    public virtual IStatement Visit(TryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      tryCatchFilterFinallyStatement.TryBody = Visit(tryCatchFilterFinallyStatement.TryBody);
      tryCatchFilterFinallyStatement.CatchClauses = Visit(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        tryCatchFilterFinallyStatement.FinallyBody = Visit(tryCatchFilterFinallyStatement.FinallyBody);
      return tryCatchFilterFinallyStatement;
    }

    public virtual IExpression Visit(TokenOf tokenOf) {
      IFieldReference/*?*/ fieldReference = tokenOf.Definition as IFieldReference;
      if (fieldReference != null)
        tokenOf.Definition = this.Visit(fieldReference);
      else {
        IMethodReference/*?*/ methodReference = tokenOf.Definition as IMethodReference;
        if (methodReference != null)
          tokenOf.Definition = this.Visit(methodReference);
        else
          tokenOf.Definition = this.Visit((ITypeReference)tokenOf.Definition);
      }
      tokenOf.Type = this.Visit(tokenOf.Type);
      return tokenOf;
    }

    public virtual IExpression Visit(TypeOf typeOf) {
      typeOf.TypeToGet = Visit(typeOf.TypeToGet);
      typeOf.Type = this.Visit(typeOf.Type);
      return typeOf;
    }

    public virtual IExpression Visit(UnaryNegation unaryNegation) {
      return this.Visit((UnaryOperation)unaryNegation);
    }

    public virtual IExpression Visit(UnaryPlus unaryPlus) {
      return this.Visit((UnaryOperation)unaryPlus);
    }

    public virtual IExpression Visit(VectorLength vectorLength) {
      vectorLength.Vector = Visit(vectorLength.Vector);
      vectorLength.Type = this.Visit(vectorLength.Type);
      return vectorLength;
    }

    public virtual IStatement Visit(WhileDoStatement whileDoStatement) {
      whileDoStatement.Condition = Visit(whileDoStatement.Condition);
      whileDoStatement.Body = Visit(whileDoStatement.Body);
      return whileDoStatement;
    }

    public virtual IStatement Visit(YieldBreakStatement yieldBreakStatement) {
      return yieldBreakStatement;
    }

    public virtual IStatement Visit(YieldReturnStatement yieldReturnStatement) {
      yieldReturnStatement.Expression = Visit(yieldReturnStatement.Expression);
      return yieldReturnStatement;
    }

    #endregion Virtual methods for subtypes to override, one per type in MutableCodeModel

    #region Methods that take an immutable type and return a type-specific mutable object, either by using the internal visitor, or else directly

    public virtual IAddressableExpression Visit(IAddressableExpression addressableExpression) {
      AddressableExpression mutableAddressableExpression = addressableExpression as AddressableExpression;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableAddressableExpression == null)
        mutableAddressableExpression = new AddressableExpression(addressableExpression);
      return Visit(mutableAddressableExpression);
    }

    public virtual IBlockStatement Visit(IBlockStatement blockStatement) {
      BlockStatement mutableBlockStatement = blockStatement as BlockStatement;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableBlockStatement == null)
        mutableBlockStatement = new BlockStatement(blockStatement);
      return Visit(mutableBlockStatement);
    }

    public virtual ICatchClause Visit(ICatchClause catchClause) {
      CatchClause mutableCatchClause = catchClause as CatchClause;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableCatchClause == null)
        mutableCatchClause = new CatchClause(catchClause);
      return Visit(mutableCatchClause);
    }

    public virtual List<ICatchClause> Visit(List<ICatchClause> catchClauses) {
      List<ICatchClause> newList = new List<ICatchClause>();
      foreach (var catchClause in catchClauses) {
        ICatchClause mcc = this.Visit(catchClause);
        newList.Add(mcc);
      }
      return newList;
    }

    public virtual ICompileTimeConstant Visit(ICompileTimeConstant compileTimeConstant) {
      CompileTimeConstant mutableCompileTimeConstant = compileTimeConstant as CompileTimeConstant;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableCompileTimeConstant == null)
        mutableCompileTimeConstant = new CompileTimeConstant(compileTimeConstant);
      return this.Visit(mutableCompileTimeConstant);
    }

    public virtual IExpression Visit(IExpression expression) {
      expression.Dispatch(this.createMutableType);
      return this.createMutableType.resultExpression;
    }

    public virtual IStatement Visit(IStatement statement) {
      statement.Dispatch(this.createMutableType);
      return this.createMutableType.resultStatement;
    }

    public virtual List<IStatement> Visit(List<IStatement> statements) {
      List<IStatement> newList = new List<IStatement>();
      foreach (var statement in statements) {
        IStatement newStatement = this.Visit(statement);
        if (newStatement != CodeDummy.Block)
          newList.Add(newStatement);
      }
      return newList;
    }

    public virtual ISwitchCase Visit(ISwitchCase switchCase) {
      SwitchCase mutableSwitchCase = switchCase as SwitchCase;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableSwitchCase == null)
        mutableSwitchCase = new SwitchCase(switchCase);
      return Visit(mutableSwitchCase);
    }

    public virtual List<ISwitchCase> Visit(List<ISwitchCase> switchCases) {
      List<ISwitchCase> newList = new List<ISwitchCase>();
      foreach (var switchCase in switchCases) {
        ISwitchCase swc = this.Visit(switchCase);
        if (swc != CodeDummy.SwitchCase)
          newList.Add(swc);
      }
      return newList;
    }

    public virtual ITargetExpression Visit(ITargetExpression targetExpression) {
      TargetExpression mutableTargetExpression = targetExpression as TargetExpression;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableTargetExpression == null)
        mutableTargetExpression = new TargetExpression(targetExpression);
      return Visit(mutableTargetExpression);
    }

    #endregion Methods that take an immutable type and return a type-specific mutable object, either by using the internal visitor, or else directly

    private class CreateMutableType : BaseCodeVisitor, ICodeVisitor {

      internal CodeMutator myCodeMutator;

      internal IExpression resultExpression = CodeDummy.Expression;
      internal IStatement resultStatement = CodeDummy.Block;

      bool alwaysMakeACopy;

      internal CreateMutableType(CodeMutator codeMutator, bool alwaysMakeACopy) {
        this.myCodeMutator = codeMutator;
        this.alwaysMakeACopy = alwaysMakeACopy;
      }

      #region overriding implementations of ICodeVisitor Members

      public override void Visit(IAddition addition) {
        Addition/*?*/ mutableAddition = addition as Addition;
        if (alwaysMakeACopy || mutableAddition == null) mutableAddition = new Addition(addition);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddition);
      }

      public override void Visit(IAddressableExpression addressableExpression) {
        AddressableExpression mutableAddressableExpression = addressableExpression as AddressableExpression;
        if (alwaysMakeACopy || mutableAddressableExpression == null) mutableAddressableExpression = new AddressableExpression(addressableExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressableExpression);
      }

      public override void Visit(IAddressDereference addressDereference) {
        AddressDereference mutableAddressDereference = addressDereference as AddressDereference;
        if (alwaysMakeACopy || mutableAddressDereference == null) mutableAddressDereference = new AddressDereference(addressDereference);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressDereference);
      }

      public override void Visit(IAddressOf addressOf) {
        AddressOf mutableAddressOf = addressOf as AddressOf;
        if (alwaysMakeACopy || mutableAddressOf == null) mutableAddressOf = new AddressOf(addressOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressOf);
      }

      public override void Visit(IAnonymousDelegate anonymousMethod) {
        AnonymousDelegate mutableAnonymousDelegate = anonymousMethod as AnonymousDelegate;
        if (alwaysMakeACopy || mutableAnonymousDelegate == null) mutableAnonymousDelegate = new AnonymousDelegate(anonymousMethod);
        this.resultExpression = this.myCodeMutator.Visit(mutableAnonymousDelegate);
      }

      public override void Visit(IArrayIndexer arrayIndexer) {
        ArrayIndexer mutableArrayIndexer = arrayIndexer as ArrayIndexer;
        if (alwaysMakeACopy || mutableArrayIndexer == null) mutableArrayIndexer = new ArrayIndexer(arrayIndexer);
        this.resultExpression = this.myCodeMutator.Visit(mutableArrayIndexer);
      }

      public override void Visit(IAssertStatement assertStatement) {
        AssertStatement mutableAssertStatement = assertStatement as AssertStatement;
        if (alwaysMakeACopy || mutableAssertStatement == null) mutableAssertStatement = new AssertStatement(assertStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableAssertStatement);
      }

      public override void Visit(IAssignment assignment) {
        Assignment mutableAssignment = assignment as Assignment;
        if (alwaysMakeACopy || mutableAssignment == null) mutableAssignment = new Assignment(assignment);
        this.resultExpression = this.myCodeMutator.Visit(mutableAssignment);
      }

      public override void Visit(IAssumeStatement assumeStatement) {
        AssumeStatement mutableAssumeStatement = assumeStatement as AssumeStatement;
        if (alwaysMakeACopy || mutableAssumeStatement == null) mutableAssumeStatement = new AssumeStatement(assumeStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableAssumeStatement);
      }

      public override void Visit(IBaseClassReference baseClassReference) {
        BaseClassReference mutableBaseClassReference = baseClassReference as BaseClassReference;
        if (alwaysMakeACopy || mutableBaseClassReference == null) mutableBaseClassReference = new BaseClassReference(baseClassReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableBaseClassReference);
      }

      public override void Visit(IBitwiseAnd bitwiseAnd) {
        BitwiseAnd mutableBitwiseAnd = bitwiseAnd as BitwiseAnd;
        if (alwaysMakeACopy || mutableBitwiseAnd == null) mutableBitwiseAnd = new BitwiseAnd(bitwiseAnd);
        this.resultExpression = this.myCodeMutator.Visit(mutableBitwiseAnd);
      }

      public override void Visit(IBitwiseOr bitwiseOr) {
        BitwiseOr mutableBitwiseOr = bitwiseOr as BitwiseOr;
        if (alwaysMakeACopy || mutableBitwiseOr == null) mutableBitwiseOr = new BitwiseOr(bitwiseOr);
        this.resultExpression = this.myCodeMutator.Visit(mutableBitwiseOr);
      }

      public override void Visit(IBlockExpression blockExpression) {
        BlockExpression mutableBlockExpression = blockExpression as BlockExpression;
        if (alwaysMakeACopy || mutableBlockExpression == null) mutableBlockExpression = new BlockExpression(blockExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableBlockExpression);
      }

      public override void Visit(IBlockStatement block) {
        BlockStatement mutableBlockStatement = block as BlockStatement;
        if (alwaysMakeACopy || mutableBlockStatement == null) mutableBlockStatement = new BlockStatement(block);
        this.resultStatement = this.myCodeMutator.Visit(mutableBlockStatement);
      }

      public override void Visit(IBreakStatement breakStatement) {
        BreakStatement mutableBreakStatement = breakStatement as BreakStatement;
        if (alwaysMakeACopy || mutableBreakStatement == null) mutableBreakStatement = new BreakStatement(breakStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableBreakStatement);
      }

      public override void Visit(IBoundExpression boundExpression) {
        BoundExpression mutableBoundExpression = boundExpression as BoundExpression;
        if (alwaysMakeACopy || mutableBoundExpression == null) mutableBoundExpression = new BoundExpression(boundExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableBoundExpression);
      }

      public override void Visit(ICastIfPossible castIfPossible) {
        CastIfPossible mutableCastIfPossible = castIfPossible as CastIfPossible;
        if (alwaysMakeACopy || mutableCastIfPossible == null) mutableCastIfPossible = new CastIfPossible(castIfPossible);
        this.resultExpression = this.myCodeMutator.Visit(mutableCastIfPossible);
      }

      public override void Visit(ICheckIfInstance checkIfInstance) {
        CheckIfInstance mutableCheckIfInstance = checkIfInstance as CheckIfInstance;
        if (alwaysMakeACopy || mutableCheckIfInstance == null) mutableCheckIfInstance = new CheckIfInstance(checkIfInstance);
        this.resultExpression = this.myCodeMutator.Visit(mutableCheckIfInstance);
      }

      public override void Visit(ICompileTimeConstant constant) {
        CompileTimeConstant mutableCompileTimeConstant = constant as CompileTimeConstant;
        if (alwaysMakeACopy || mutableCompileTimeConstant == null) mutableCompileTimeConstant = new CompileTimeConstant(constant);
        this.resultExpression = this.myCodeMutator.Visit(mutableCompileTimeConstant);
      }

      public override void Visit(IConversion conversion) {
        Conversion mutableConversion = conversion as Conversion;
        if (alwaysMakeACopy || mutableConversion == null) mutableConversion = new Conversion(conversion);
        this.resultExpression = this.myCodeMutator.Visit(mutableConversion);
      }

      public override void Visit(IConditional conditional) {
        Conditional mutableConditional = conditional as Conditional;
        if (alwaysMakeACopy || mutableConditional == null) mutableConditional = new Conditional(conditional);
        this.resultExpression = this.myCodeMutator.Visit(mutableConditional);
      }

      public override void Visit(IConditionalStatement conditionalStatement) {
        ConditionalStatement mutableConditionalStatement = conditionalStatement as ConditionalStatement;
        if (alwaysMakeACopy || mutableConditionalStatement == null) mutableConditionalStatement = new ConditionalStatement(conditionalStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableConditionalStatement);
      }

      public override void Visit(IContinueStatement continueStatement) {
        ContinueStatement mutableContinueStatement = continueStatement as ContinueStatement;
        if (alwaysMakeACopy || mutableContinueStatement == null) mutableContinueStatement = new ContinueStatement(continueStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableContinueStatement);
      }

      public override void Visit(ICreateArray createArray) {
        CreateArray mutableCreateArray = createArray as CreateArray;
        if (alwaysMakeACopy || mutableCreateArray == null) mutableCreateArray = new CreateArray(createArray);
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateArray);
      }

      public override void Visit(ICreateDelegateInstance createDelegateInstance) {
        CreateDelegateInstance mutableCreateDelegateInstance = createDelegateInstance as CreateDelegateInstance;
        if (alwaysMakeACopy || mutableCreateDelegateInstance == null) mutableCreateDelegateInstance = new CreateDelegateInstance(createDelegateInstance);
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateDelegateInstance);
      }

      public override void Visit(ICreateObjectInstance createObjectInstance) {
        CreateObjectInstance mutableCreateObjectInstance = createObjectInstance as CreateObjectInstance;
        if (alwaysMakeACopy || mutableCreateObjectInstance == null) mutableCreateObjectInstance = new CreateObjectInstance(createObjectInstance);
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateObjectInstance);
      }

      public override void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        DebuggerBreakStatement mutableDebuggerBreakStatement = debuggerBreakStatement as DebuggerBreakStatement;
        if (alwaysMakeACopy || mutableDebuggerBreakStatement == null) mutableDebuggerBreakStatement = new DebuggerBreakStatement(debuggerBreakStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableDebuggerBreakStatement);
      }

      public override void Visit(IDefaultValue defaultValue) {
        DefaultValue mutableDefaultValue = defaultValue as DefaultValue;
        if (alwaysMakeACopy || mutableDefaultValue == null) mutableDefaultValue = new DefaultValue(defaultValue);
        this.resultExpression = this.myCodeMutator.Visit(mutableDefaultValue);
      }

      public override void Visit(IDivision division) {
        Division mutableDivision = division as Division;
        if (alwaysMakeACopy || mutableDivision == null) mutableDivision = new Division(division);
        this.resultExpression = this.myCodeMutator.Visit(mutableDivision);
      }

      public override void Visit(IDoUntilStatement doUntilStatement) {
        DoUntilStatement mutableDoUntilStatement = doUntilStatement as DoUntilStatement;
        if (alwaysMakeACopy || mutableDoUntilStatement == null) mutableDoUntilStatement = new DoUntilStatement(doUntilStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableDoUntilStatement);
      }

      public override void Visit(IEmptyStatement emptyStatement) {
        EmptyStatement mutableEmptyStatement = emptyStatement as EmptyStatement;
        if (alwaysMakeACopy || mutableEmptyStatement == null) mutableEmptyStatement = new EmptyStatement(emptyStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableEmptyStatement);
      }

      public override void Visit(IEquality equality) {
        Equality mutableEquality = equality as Equality;
        if (alwaysMakeACopy || mutableEquality == null) mutableEquality = new Equality(equality);
        this.resultExpression = this.myCodeMutator.Visit(mutableEquality);
      }

      public override void Visit(IExclusiveOr exclusiveOr) {
        ExclusiveOr mutableExclusiveOr = exclusiveOr as ExclusiveOr;
        if (alwaysMakeACopy || mutableExclusiveOr == null) mutableExclusiveOr = new ExclusiveOr(exclusiveOr);
        this.resultExpression = this.myCodeMutator.Visit(mutableExclusiveOr);
      }

      public override void Visit(IExpression expression) {
        Debug.Assert(false); //Should never get here
      }

      public override void Visit(IExpressionStatement expressionStatement) {
        ExpressionStatement mutableExpressionStatement = expressionStatement as ExpressionStatement;
        if (alwaysMakeACopy || mutableExpressionStatement == null) mutableExpressionStatement = new ExpressionStatement(expressionStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableExpressionStatement);
      }

      public override void Visit(IForEachStatement forEachStatement) {
        ForEachStatement mutableForEachStatement = forEachStatement as ForEachStatement;
        if (alwaysMakeACopy || mutableForEachStatement == null) mutableForEachStatement = new ForEachStatement(forEachStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableForEachStatement);
      }

      public override void Visit(IForStatement forStatement) {
        ForStatement mutableForStatement = forStatement as ForStatement;
        if (alwaysMakeACopy || mutableForStatement == null) mutableForStatement = new ForStatement(forStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableForStatement);
      }

      public override void Visit(IGotoStatement gotoStatement) {
        GotoStatement mutableGotoStatement = gotoStatement as GotoStatement;
        if (alwaysMakeACopy || mutableGotoStatement == null) mutableGotoStatement = new GotoStatement(gotoStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableGotoStatement);
      }

      public override void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        GotoSwitchCaseStatement mutableGotoSwitchCaseStatement = gotoSwitchCaseStatement as GotoSwitchCaseStatement;
        if (alwaysMakeACopy || mutableGotoSwitchCaseStatement == null) mutableGotoSwitchCaseStatement = new GotoSwitchCaseStatement(gotoSwitchCaseStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableGotoSwitchCaseStatement);
      }

      public override void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        GetTypeOfTypedReference mutableGetTypeOfTypedReference = getTypeOfTypedReference as GetTypeOfTypedReference;
        if (alwaysMakeACopy || mutableGetTypeOfTypedReference == null) mutableGetTypeOfTypedReference = new GetTypeOfTypedReference(getTypeOfTypedReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableGetTypeOfTypedReference);
      }

      public override void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        GetValueOfTypedReference mutableGetValueOfTypedReference = getValueOfTypedReference as GetValueOfTypedReference;
        if (alwaysMakeACopy || mutableGetValueOfTypedReference == null) mutableGetValueOfTypedReference = new GetValueOfTypedReference(getValueOfTypedReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableGetValueOfTypedReference);
      }

      public override void Visit(IGreaterThan greaterThan) {
        GreaterThan mutableGreaterThan = greaterThan as GreaterThan;
        if (alwaysMakeACopy || mutableGreaterThan == null) mutableGreaterThan = new GreaterThan(greaterThan);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableGreaterThan);
      }

      public override void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        GreaterThanOrEqual mutableGreaterThanOrEqual = greaterThanOrEqual as GreaterThanOrEqual;
        if (alwaysMakeACopy || mutableGreaterThanOrEqual == null) mutableGreaterThanOrEqual = new GreaterThanOrEqual(greaterThanOrEqual);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableGreaterThanOrEqual);
      }

      public override void Visit(ILabeledStatement labeledStatement) {
        LabeledStatement mutableLabeledStatement = labeledStatement as LabeledStatement;
        if (alwaysMakeACopy || mutableLabeledStatement == null) mutableLabeledStatement = new LabeledStatement(labeledStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableLabeledStatement);
      }

      public override void Visit(ILeftShift leftShift) {
        LeftShift mutableLeftShift = leftShift as LeftShift;
        if (alwaysMakeACopy || mutableLeftShift == null) mutableLeftShift = new LeftShift(leftShift);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableLeftShift);
      }

      public override void Visit(ILessThan lessThan) {
        LessThan mutableLessThan = lessThan as LessThan;
        if (alwaysMakeACopy || mutableLessThan == null) mutableLessThan = new LessThan(lessThan);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableLessThan);
      }

      public override void Visit(ILessThanOrEqual lessThanOrEqual) {
        LessThanOrEqual mutableLessThanOrEqual = lessThanOrEqual as LessThanOrEqual;
        if (alwaysMakeACopy || mutableLessThanOrEqual == null) mutableLessThanOrEqual = new LessThanOrEqual(lessThanOrEqual);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableLessThanOrEqual);
      }

      public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        LocalDeclarationStatement mutableLocalDeclarationStatement = localDeclarationStatement as LocalDeclarationStatement;
        if (alwaysMakeACopy || mutableLocalDeclarationStatement == null) mutableLocalDeclarationStatement = new LocalDeclarationStatement(localDeclarationStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableLocalDeclarationStatement);
      }

      public override void Visit(ILockStatement lockStatement) {
        LockStatement mutableLockStatement = lockStatement as LockStatement;
        if (alwaysMakeACopy || mutableLockStatement == null) mutableLockStatement = new LockStatement(lockStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableLockStatement);
      }

      public override void Visit(ILogicalNot logicalNot) {
        LogicalNot mutableLogicalNot = logicalNot as LogicalNot;
        if (alwaysMakeACopy || mutableLogicalNot == null) mutableLogicalNot = new LogicalNot(logicalNot);
        this.resultExpression = this.myCodeMutator.Visit(mutableLogicalNot);
      }

      public override void Visit(IMakeTypedReference makeTypedReference) {
        MakeTypedReference mutableMakeTypedReference = makeTypedReference as MakeTypedReference;
        if (alwaysMakeACopy || mutableMakeTypedReference == null) mutableMakeTypedReference = new MakeTypedReference(makeTypedReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableMakeTypedReference);
      }

      public override void Visit(IMethodCall methodCall) {
        MethodCall mutableMethodCall = methodCall as MethodCall;
        if (alwaysMakeACopy || mutableMethodCall == null) mutableMethodCall = new MethodCall(methodCall);
        this.resultExpression = this.myCodeMutator.Visit(mutableMethodCall);
      }

      public override void Visit(IModulus modulus) {
        Modulus mutableModulus = modulus as Modulus;
        if (alwaysMakeACopy || mutableModulus == null) mutableModulus = new Modulus(modulus);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableModulus);
      }

      public override void Visit(IMultiplication multiplication) {
        Multiplication mutableMultiplication = multiplication as Multiplication;
        if (alwaysMakeACopy || mutableMultiplication == null) mutableMultiplication = new Multiplication(multiplication);
        this.resultExpression = this.myCodeMutator.Visit(mutableMultiplication);
      }

      public override void Visit(INamedArgument namedArgument) {
        NamedArgument mutableNamedArgument = namedArgument as NamedArgument;
        if (alwaysMakeACopy || mutableNamedArgument == null) mutableNamedArgument = new NamedArgument(namedArgument);
        this.resultExpression = this.myCodeMutator.Visit(mutableNamedArgument);
      }

      public override void Visit(INotEquality notEquality) {
        NotEquality mutableNotEquality = notEquality as NotEquality;
        if (alwaysMakeACopy || mutableNotEquality == null) mutableNotEquality = new NotEquality(notEquality);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableNotEquality);
      }

      public override void Visit(IOldValue oldValue) {
        OldValue mutableOldValue = oldValue as OldValue;
        if (alwaysMakeACopy || mutableOldValue == null) mutableOldValue = new OldValue(oldValue);
        this.resultExpression = this.myCodeMutator.Visit(mutableOldValue);
      }

      public override void Visit(IOnesComplement onesComplement) {
        OnesComplement mutableOnesComplement = onesComplement as OnesComplement;
        if (alwaysMakeACopy || mutableOnesComplement == null) mutableOnesComplement = new OnesComplement(onesComplement);
        this.resultExpression = this.myCodeMutator.Visit(mutableOnesComplement);
      }

      public override void Visit(IOutArgument outArgument) {
        OutArgument mutableOutArgument = outArgument as OutArgument;
        if (alwaysMakeACopy || mutableOutArgument == null) mutableOutArgument = new OutArgument(outArgument);
        this.resultExpression = this.myCodeMutator.Visit(mutableOutArgument);
      }

      public override void Visit(IPointerCall pointerCall) {
        PointerCall mutablePointerCall = pointerCall as PointerCall;
        if (alwaysMakeACopy || mutablePointerCall == null) mutablePointerCall = new PointerCall(pointerCall);
        this.resultExpression = this.myCodeMutator.Visit(mutablePointerCall);
      }

      public override void Visit(IRefArgument refArgument) {
        RefArgument mutableRefArgument = refArgument as RefArgument;
        if (alwaysMakeACopy || mutableRefArgument == null) mutableRefArgument = new RefArgument(refArgument);
        this.resultExpression = this.myCodeMutator.Visit(mutableRefArgument);
      }

      public override void Visit(IResourceUseStatement resourceUseStatement) {
        ResourceUseStatement mutableResourceUseStatement = resourceUseStatement as ResourceUseStatement;
        if (alwaysMakeACopy || mutableResourceUseStatement == null) mutableResourceUseStatement = new ResourceUseStatement(resourceUseStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableResourceUseStatement);
      }

      public override void Visit(IReturnValue returnValue) {
        ReturnValue mutableReturnValue = returnValue as ReturnValue;
        if (alwaysMakeACopy || mutableReturnValue == null) mutableReturnValue = new ReturnValue(returnValue);
        this.resultExpression = this.myCodeMutator.Visit(mutableReturnValue);
      }

      public override void Visit(IRethrowStatement rethrowStatement) {
        RethrowStatement mutableRethrowStatement = rethrowStatement as RethrowStatement;
        if (alwaysMakeACopy || mutableRethrowStatement == null) mutableRethrowStatement = new RethrowStatement(rethrowStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableRethrowStatement);
      }

      public override void Visit(IReturnStatement returnStatement) {
        ReturnStatement mutableReturnStatement = returnStatement as ReturnStatement;
        if (alwaysMakeACopy || mutableReturnStatement == null) mutableReturnStatement = new ReturnStatement(returnStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableReturnStatement);
      }

      public override void Visit(IRightShift rightShift) {
        RightShift mutableRightShift = rightShift as RightShift;
        if (alwaysMakeACopy || mutableRightShift == null) mutableRightShift = new RightShift(rightShift);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableRightShift);
      }

      public override void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        RuntimeArgumentHandleExpression mutableRuntimeArgumentHandleExpression = runtimeArgumentHandleExpression as RuntimeArgumentHandleExpression;
        if (alwaysMakeACopy || mutableRuntimeArgumentHandleExpression == null) mutableRuntimeArgumentHandleExpression = new RuntimeArgumentHandleExpression(runtimeArgumentHandleExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableRuntimeArgumentHandleExpression);
      }

      public override void Visit(ISizeOf sizeOf) {
        SizeOf mutableSizeOf = sizeOf as SizeOf;
        if (alwaysMakeACopy || mutableSizeOf == null) mutableSizeOf = new SizeOf(sizeOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableSizeOf);
      }

      public override void Visit(IStackArrayCreate stackArrayCreate) {
        StackArrayCreate mutableStackArrayCreate = stackArrayCreate as StackArrayCreate;
        if (alwaysMakeACopy || mutableStackArrayCreate == null) mutableStackArrayCreate = new StackArrayCreate(stackArrayCreate);
        this.resultExpression = this.myCodeMutator.Visit(mutableStackArrayCreate);
      }

      public override void Visit(ISubtraction subtraction) {
        Subtraction mutableSubtraction = subtraction as Subtraction;
        if (alwaysMakeACopy || mutableSubtraction == null) mutableSubtraction = new Subtraction(subtraction);
        this.resultExpression = this.myCodeMutator.Visit((BinaryOperation)mutableSubtraction);
      }

      public override void Visit(ISwitchStatement switchStatement) {
        SwitchStatement mutableSwitchStatement = switchStatement as SwitchStatement;
        if (alwaysMakeACopy || mutableSwitchStatement == null) mutableSwitchStatement = new SwitchStatement(switchStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableSwitchStatement);
      }

      public override void Visit(ITargetExpression targetExpression) {
        TargetExpression mutableTargetExpression = targetExpression as TargetExpression;
        if (alwaysMakeACopy || mutableTargetExpression == null) mutableTargetExpression = new TargetExpression(targetExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableTargetExpression);
      }

      public override void Visit(IThisReference thisReference) {
        ThisReference mutableThisReference = thisReference as ThisReference;
        if (alwaysMakeACopy || mutableThisReference == null) mutableThisReference = new ThisReference(thisReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableThisReference);
      }

      public override void Visit(IThrowStatement throwStatement) {
        ThrowStatement mutableThrowStatement = throwStatement as ThrowStatement;
        if (alwaysMakeACopy || mutableThrowStatement == null) mutableThrowStatement = new ThrowStatement(throwStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableThrowStatement);
      }

      public override void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        TryCatchFinallyStatement mutableTryCatchFinallyStatement = tryCatchFilterFinallyStatement as TryCatchFinallyStatement;
        if (alwaysMakeACopy || mutableTryCatchFinallyStatement == null) mutableTryCatchFinallyStatement = new TryCatchFinallyStatement(tryCatchFilterFinallyStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableTryCatchFinallyStatement);
      }

      public override void Visit(ITokenOf tokenOf) {
        TokenOf mutableTokenOf = tokenOf as TokenOf;
        if (alwaysMakeACopy || mutableTokenOf == null) mutableTokenOf = new TokenOf(tokenOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableTokenOf);
      }

      public override void Visit(ITypeOf typeOf) {
        TypeOf mutableTypeOf = typeOf as TypeOf;
        if (alwaysMakeACopy || mutableTypeOf == null) mutableTypeOf = new TypeOf(typeOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableTypeOf);
      }

      public override void Visit(IUnaryNegation unaryNegation) {
        UnaryNegation mutableUnaryNegation = unaryNegation as UnaryNegation;
        if (alwaysMakeACopy || mutableUnaryNegation == null) mutableUnaryNegation = new UnaryNegation(unaryNegation);
        this.resultExpression = this.myCodeMutator.Visit(mutableUnaryNegation);
      }

      public override void Visit(IUnaryPlus unaryPlus) {
        UnaryPlus mutableUnaryPlus = unaryPlus as UnaryPlus;
        if (alwaysMakeACopy || mutableUnaryPlus == null) mutableUnaryPlus = new UnaryPlus(unaryPlus);
        this.resultExpression = this.myCodeMutator.Visit(mutableUnaryPlus);
      }

      public override void Visit(IVectorLength vectorLength) {
        VectorLength mutableVectorLength = vectorLength as VectorLength;
        if (alwaysMakeACopy || mutableVectorLength == null) mutableVectorLength = new VectorLength(vectorLength);
        this.resultExpression = this.myCodeMutator.Visit(mutableVectorLength);
      }

      public override void Visit(IWhileDoStatement whileDoStatement) {
        WhileDoStatement mutableWhileDoStatement = whileDoStatement as WhileDoStatement;
        if (alwaysMakeACopy || mutableWhileDoStatement == null) mutableWhileDoStatement = new WhileDoStatement(whileDoStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableWhileDoStatement);
      }

      public override void Visit(IYieldBreakStatement yieldBreakStatement) {
        YieldBreakStatement mutableYieldBreakStatement = yieldBreakStatement as YieldBreakStatement;
        if (alwaysMakeACopy || mutableYieldBreakStatement == null) mutableYieldBreakStatement = new YieldBreakStatement(yieldBreakStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableYieldBreakStatement);
      }

      public override void Visit(IYieldReturnStatement yieldReturnStatement) {
        YieldReturnStatement mutableYieldReturnStatement = yieldReturnStatement as YieldReturnStatement;
        if (alwaysMakeACopy || mutableYieldReturnStatement == null) mutableYieldReturnStatement = new YieldReturnStatement(yieldReturnStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableYieldReturnStatement);
      }

      #endregion overriding implementations of ICodeVisitor Members

    }

  }

  public class CodeAndContractMutator : CodeMutator {

    protected readonly ContractProvider/*?*/ contractProvider;

    public CodeAndContractMutator(IMetadataHost host)
      : base(host) { }

    public CodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host, copyOnlyIfNotAlreadyMutable) {
    }

    public CodeAndContractMutator(IMetadataHost host, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, ilToSourceProvider, sourceToILProvider, sourceLocationProvider) {
      this.contractProvider = contractProvider;
    }

    public CodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider, ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, copyOnlyIfNotAlreadyMutable, ilToSourceProvider, sourceToILProvider, sourceLocationProvider) {
      this.contractProvider = contractProvider;
    }

    protected CodeAndContractMutator(CodeAndContractMutator template)
      : base(template.host, template.ilToSourceProvider, template.sourceToILProvider, template.sourceLocationProvider) {
      this.contractProvider = template.contractProvider;
    }

    public virtual List<IAddressableExpression> Visit(List<IAddressableExpression> addressableExpressions) {
      List<IAddressableExpression> newList = new List<IAddressableExpression>();
      foreach (var addressableExpression in addressableExpressions)
        newList.Add(this.Visit(addressableExpression));
      return newList;
    }

    public virtual IEnumerable<IEnumerable<IExpression>> Visit(IEnumerable<IEnumerable<IExpression>> triggers) {
      List<IEnumerable<IExpression>> newTriggers = new List<IEnumerable<IExpression>>(triggers);
      for (int i = 0, n = newTriggers.Count; i < n; i++)
        newTriggers[i] = this.Visit(new List<IExpression>(newTriggers[i])).AsReadOnly();
      return newTriggers.AsReadOnly();
    }

    public override IExpression Visit(IExpression expression) {
      IExpression result = base.Visit(expression);
      if (this.contractProvider != null && expression is IMethodCall) {
        IEnumerable<IEnumerable<IExpression>>/*?*/ triggers = this.contractProvider.GetTriggersFor(expression);
        if (triggers != null)
          this.contractProvider.AssociateTriggersWithQuantifier(result, this.Visit(triggers));
      }
      return result;
    }

    public virtual ILoopContract Visit(ILoopContract loopContract) {
      LoopContract mutableLoopContract = new LoopContract(loopContract);
      mutableLoopContract.Invariants = this.Visit(mutableLoopContract.Invariants);
      mutableLoopContract.Writes = this.Visit(mutableLoopContract.Writes);
      return mutableLoopContract;
    }

    public virtual List<ILoopInvariant> Visit(List<ILoopInvariant> loopInvariants) {
      List<ILoopInvariant> newList = new List<ILoopInvariant>();
      foreach (var loopInvariant in loopInvariants)
        newList.Add(this.Visit(loopInvariant));
      return newList;
    }

    public virtual ILoopInvariant Visit(ILoopInvariant loopInvariant) {
      LoopInvariant mutableLoopInvariant = new LoopInvariant(loopInvariant);
      mutableLoopInvariant.Condition = this.Visit(mutableLoopInvariant.Condition);
      return mutableLoopInvariant;
    }

    public override IMethodDefinition Visit(IMethodDefinition methodDefinition) {
      var result = this.GetMutableCopy(methodDefinition);
      if (this.contractProvider != null) {
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(methodDefinition);
        if (methodContract != null)
          this.contractProvider.AssociateMethodWithContract(result, methodContract);
      }
      return this.Visit(result);
    }

    public override IGlobalMethodDefinition Visit(IGlobalMethodDefinition globalMethodDefinition) {
      var result = this.GetMutableCopy(globalMethodDefinition);
      if (this.contractProvider != null) {
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(globalMethodDefinition);
        if (methodContract != null)
          this.contractProvider.AssociateMethodWithContract(result, methodContract);
      }
      return this.Visit(result);
    }

    public override MethodDefinition Visit(MethodDefinition methodDefinition) {
      if (this.stopTraversal) return methodDefinition;
      if (methodDefinition == Dummy.Method) return methodDefinition;
      this.Visit((TypeDefinitionMember)methodDefinition);
      this.path.Push(methodDefinition);
      if (methodDefinition.IsGeneric)
        methodDefinition.GenericParameters = this.Visit(methodDefinition.GenericParameters, methodDefinition);
      methodDefinition.Parameters = this.Visit(methodDefinition.Parameters);
      if (methodDefinition.IsPlatformInvoke)
        methodDefinition.PlatformInvokeData = this.Visit(this.GetMutableCopy(methodDefinition.PlatformInvokeData));
      methodDefinition.ReturnValueAttributes = this.VisitMethodReturnValueAttributes(methodDefinition.ReturnValueAttributes);
      if (methodDefinition.ReturnValueIsModified)
        methodDefinition.ReturnValueCustomModifiers = this.VisitMethodReturnValueCustomModifiers(methodDefinition.ReturnValueCustomModifiers);
      if (methodDefinition.ReturnValueIsMarshalledExplicitly)
        methodDefinition.ReturnValueMarshallingInformation = this.VisitMethodReturnValueMarshallingInformation(this.GetMutableCopy(methodDefinition.ReturnValueMarshallingInformation));
      if (methodDefinition.HasDeclarativeSecurity)
        methodDefinition.SecurityAttributes = this.Visit(methodDefinition.SecurityAttributes);
      methodDefinition.Type = this.Visit(methodDefinition.Type);
      if (this.contractProvider != null) {
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(methodDefinition);
        if (methodContract != null)
          this.contractProvider.AssociateMethodWithContract(methodDefinition, this.Visit(methodContract));
      }
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal)
        methodDefinition.Body = this.Visit(methodDefinition.Body);
      this.path.Pop();
      return methodDefinition;
    }

    public override IMethodBody Visit(IMethodBody methodBody) {
      ISourceMethodBody sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody == null && this.ilToSourceProvider != null)
        sourceMethodBody = this.ilToSourceProvider(methodBody);
      if (sourceMethodBody != null) {
        SourceMethodBody mutableSourceMethodBody = new SourceMethodBody(this.sourceToILProvider, this.host, this.sourceLocationProvider, this.contractProvider);
        mutableSourceMethodBody.Block = this.Visit(sourceMethodBody.Block);
        var currentMethod = this.GetCurrentMethod();
        // Visiting the block extracts the contract, but it gets associated with the immutable method
        if (this.contractProvider != null) {
          var methodDef = methodBody.MethodDefinition;
          IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(methodDef);
          if (methodContract != null)
            this.contractProvider.AssociateMethodWithContract(currentMethod, this.Visit(methodContract));
        }
        mutableSourceMethodBody.MethodDefinition = currentMethod;
        mutableSourceMethodBody.LocalsAreZeroed = methodBody.LocalsAreZeroed;
        return mutableSourceMethodBody;
      }
      return base.Visit(methodBody);
    }

    public virtual IMethodContract Visit(IMethodContract methodContract) {
      MethodContract mutableMethodContract = new MethodContract(methodContract);
      mutableMethodContract.Allocates = this.Visit(mutableMethodContract.Allocates);
      mutableMethodContract.Frees = this.Visit(mutableMethodContract.Frees);
      mutableMethodContract.ModifiedVariables = this.Visit(mutableMethodContract.ModifiedVariables);
      mutableMethodContract.Postconditions = this.Visit(mutableMethodContract.Postconditions);
      mutableMethodContract.Preconditions = this.Visit(mutableMethodContract.Preconditions);
      mutableMethodContract.Reads = this.Visit(mutableMethodContract.Reads);
      mutableMethodContract.ThrownExceptions = this.Visit(mutableMethodContract.ThrownExceptions);
      mutableMethodContract.Writes = this.Visit(mutableMethodContract.Writes);
      return mutableMethodContract;
    }

    public virtual List<IPostcondition> Visit(List<IPostcondition> postConditions) {
      List<IPostcondition> newList = new List<IPostcondition>();
      foreach (var postCondition in postConditions)
        newList.Add(this.Visit(postCondition));
      return newList;
    }

    public virtual IPostcondition Visit(IPostcondition postCondition) {
      PostCondition mutablePostCondition = new PostCondition(postCondition);
      mutablePostCondition.Condition = this.Visit(mutablePostCondition.Condition);
      return mutablePostCondition;
    }

    public virtual List<IPrecondition> Visit(List<IPrecondition> preconditions) {
      List<IPrecondition> newList = new List<IPrecondition>();
      foreach (var precondition in preconditions)
        newList.Add(this.Visit(precondition));
      return newList;
    }

    public virtual IPrecondition Visit(IPrecondition preCondition) {
      Precondition mutablePrecondition = new Precondition(preCondition);
      mutablePrecondition.Condition = this.Visit(mutablePrecondition.Condition);
      if (mutablePrecondition.ExceptionToThrow != null)
        mutablePrecondition.ExceptionToThrow = this.Visit(mutablePrecondition.ExceptionToThrow);
      return mutablePrecondition;
    }

    public override IStatement Visit(IStatement statement) {
      IStatement result = base.Visit(statement);
      if (this.contractProvider != null) {
        ILoopContract/*?*/ loopContract = this.contractProvider.GetLoopContractFor(statement);
        if (loopContract != null)
          this.contractProvider.AssociateLoopWithContract(result, this.Visit(loopContract));
      }
      return result;
    }

    public virtual List<IThrownException> Visit(List<IThrownException> thrownExceptions) {
      List<IThrownException> newList = new List<IThrownException>();
      foreach (var thrownException in thrownExceptions)
        newList.Add(this.Visit(thrownException));
      return newList;
    }

    public virtual IThrownException Visit(IThrownException thrownException) {
      ThrownException mutableThrownException = new ThrownException(thrownException);
      mutableThrownException.ExceptionType = this.Visit(mutableThrownException.ExceptionType);
      mutableThrownException.Postconditions = this.Visit(mutableThrownException.Postconditions);
      return mutableThrownException;
    }

    public virtual ITypeContract Visit(ITypeContract typeContract) {
      TypeContract mutableTypeContract = new TypeContract(typeContract);
      mutableTypeContract.ContractFields = this.Visit(mutableTypeContract.ContractFields);
      mutableTypeContract.ContractMethods = this.Visit(mutableTypeContract.ContractMethods);
      mutableTypeContract.Invariants = this.Visit(mutableTypeContract.Invariants);
      return mutableTypeContract;
    }

    public virtual List<ITypeInvariant> Visit(List<ITypeInvariant> typeInvariants) {
      List<ITypeInvariant> newList = new List<ITypeInvariant>();
      foreach (var typeInvariant in typeInvariants)
        newList.Add(this.Visit(typeInvariant));
      return newList;
    }

    public virtual ITypeInvariant Visit(ITypeInvariant typeInvariant) {
      TypeInvariant mutableTypeInvariant = new TypeInvariant(typeInvariant);
      mutableTypeInvariant.Condition = this.Visit(mutableTypeInvariant.Condition);
      return mutableTypeInvariant;
    }

    public override INamespaceTypeDefinition Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      var result = base.Visit(namespaceTypeDefinition);
      if (this.contractProvider != null) {
        ITypeContract/*?*/ typeContract = this.contractProvider.GetTypeContractFor(namespaceTypeDefinition);
        if (typeContract != null)
          this.contractProvider.AssociateTypeWithContract(result, this.Visit(typeContract));
      }
      return result;
    }

    public override INestedTypeDefinition Visit(INestedTypeDefinition nestedTypeDefinition) {
      var result = base.Visit(nestedTypeDefinition);
      if (this.contractProvider != null) {
        ITypeContract/*?*/ typeContract = this.contractProvider.GetTypeContractFor(nestedTypeDefinition);
        if (typeContract != null)
          this.contractProvider.AssociateTypeWithContract(result, this.Visit(typeContract));
      }
      return result;
    }

  }

  /// <summary>
  /// Use this as a base class when you define a code mutator that mutates ONLY method bodies.
  /// This class has overrides for Visit(IFieldReference), Visit(IMethodReference), and
  /// Visit(ITypeReference) that make sure to not modify the references.
  /// </summary>
  public class MethodBodyCodeMutator : CodeMutator {
    public MethodBodyCodeMutator(IMetadataHost host)
      : base(host) { }

    public MethodBodyCodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host, copyOnlyIfNotAlreadyMutable) { }

    public MethodBodyCodeMutator(IMetadataHost host, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, ilToSourceProvider, sourceToILProvider, sourceLocationProvider) { }

    public MethodBodyCodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, copyOnlyIfNotAlreadyMutable, ilToSourceProvider, sourceToILProvider, sourceLocationProvider) { }

    #region All code mutators that are not mutating an entire assembly need to *not* modify certain references
    public override IFieldReference Visit(IFieldReference fieldReference) {
      return fieldReference;
    }

    public override IMethodReference Visit(IMethodReference methodReference) {
      return methodReference;
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      return typeReference;
    }
    #endregion All code mutators that are not mutating an entire assembly need to *not* modify certain references
  }

  /// <summary>
  /// Use this as a base class when you define a code and contract mutator that mutates ONLY
  /// method bodies and their contracts.
  /// This class has overrides for Visit(IFieldReference), Visit(IMethodReference), and
  /// Visit(ITypeReference) that make sure to not modify the references.
  /// </summary>
  public class MethodBodyCodeAndContractMutator : CodeAndContractMutator {
      public MethodBodyCodeAndContractMutator(IMetadataHost host)
      : base(host) { }

    public MethodBodyCodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host, copyOnlyIfNotAlreadyMutable) { }

    public MethodBodyCodeAndContractMutator(IMetadataHost host, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, ilToSourceProvider, sourceToILProvider, sourceLocationProvider, contractProvider) { }

    public MethodBodyCodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, SourceMethodBodyProvider ilToSourceProvider, SourceToILConverterProvider sourceToILProvider, ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, copyOnlyIfNotAlreadyMutable, ilToSourceProvider, sourceToILProvider, sourceLocationProvider, contractProvider) { }

    #region All code mutators that are not mutating an entire assembly need to *not* modify certain references
    public override IFieldReference Visit(IFieldReference fieldReference) {
      return fieldReference;
    }

    public override IMethodReference Visit(IMethodReference methodReference) {
      return methodReference;
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      return typeReference;
    }
    #endregion All code mutators that are not mutating an entire assembly need to *not* modify certain references

  }

}
