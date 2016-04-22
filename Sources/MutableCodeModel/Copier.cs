//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci;
using System.Diagnostics;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;
using System.Diagnostics.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// 
  /// </summary>
  public class CodeShallowCopier : MetadataShallowCopier {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    /// <param name="sourceLocationProvider"></param>
    /// <param name="localScopeProvider"></param>
    public CodeShallowCopier(IMetadataHost targetHost, ISourceLocationProvider sourceLocationProvider = null, ILocalScopeProvider localScopeProvider = null)
      : base(targetHost) {
      this.sourceLocationProvider = sourceLocationProvider;
      this.localScopeProvider = localScopeProvider;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    /// <param name="targetUnit">The unit of metadata into which copies made by this copier will be inserted.</param>
    /// <param name="sourceLocationProvider"></param>
    public CodeShallowCopier(IMetadataHost targetHost, IUnit targetUnit, ISourceLocationProvider sourceLocationProvider = null)
      : base(targetHost, targetUnit) {
      this.sourceLocationProvider = sourceLocationProvider;
    }

    ISourceLocationProvider/*?*/ sourceLocationProvider;
    ILocalScopeProvider/*?*/ localScopeProvider = null;

    CodeDispatcher Dispatcher {
      get {
        if (this.dispatcher == null)
          this.dispatcher = new CodeDispatcher() { copier = this };
        return this.dispatcher;
      }
    }
    CodeDispatcher dispatcher;

    class CodeDispatcher : MetadataDispatcher, ICodeVisitor {

      internal object result;
      internal CodeShallowCopier copier;

      #region ICodeVisitor Members

      public void Visit(IAddition addition) {
        this.result = this.copier.Copy(addition);
      }

      public void Visit(IAddressableExpression addressableExpression) {
        this.result = this.copier.Copy(addressableExpression);
      }

      public void Visit(IAddressDereference addressDereference) {
        this.result = this.copier.Copy(addressDereference);
      }

      public void Visit(IAddressOf addressOf) {
        this.result = this.copier.Copy(addressOf);
      }

      public void Visit(IAnonymousDelegate anonymousDelegate) {
        this.result = this.copier.Copy(anonymousDelegate);
      }

      public void Visit(IArrayIndexer arrayIndexer) {
        this.result = this.copier.Copy(arrayIndexer);
      }

      public void Visit(IAssertStatement assertStatement) {
        this.result = this.copier.Copy(assertStatement);
      }

      public void Visit(IAssignment assignment) {
        this.result = this.copier.Copy(assignment);
      }

      public void Visit(IAssumeStatement assumeStatement) {
        this.result = this.copier.Copy(assumeStatement);
      }

      public void Visit(IBitwiseAnd bitwiseAnd) {
        this.result = this.copier.Copy(bitwiseAnd);
      }

      public void Visit(IBitwiseOr bitwiseOr) {
        this.result = this.copier.Copy(bitwiseOr);
      }

      public void Visit(IBlockExpression blockExpression) {
        this.result = this.copier.Copy(blockExpression);
      }

      public void Visit(IBlockStatement block) {
        this.result = this.copier.Copy(block);
      }

      public void Visit(IBreakStatement breakStatement) {
        this.result = this.copier.Copy(breakStatement);
      }

      public void Visit(IBoundExpression boundExpression) {
        this.result = this.copier.Copy(boundExpression);
      }

      public void Visit(ICastIfPossible castIfPossible) {
        this.result = this.copier.Copy(castIfPossible);
      }

      public void Visit(ICatchClause catchClause) {
        this.result = this.copier.Copy(catchClause);
      }

      public void Visit(ICheckIfInstance checkIfInstance) {
        this.result = this.copier.Copy(checkIfInstance);
      }

      public void Visit(ICompileTimeConstant constant) {
        this.result = this.copier.Copy(constant);
      }

      public void Visit(IConversion conversion) {
        this.result = this.copier.Copy(conversion);
      }

      public void Visit(IConditional conditional) {
        this.result = this.copier.Copy(conditional);
      }

      public void Visit(IConditionalStatement conditionalStatement) {
        this.result = this.copier.Copy(conditionalStatement);
      }

      public void Visit(IContinueStatement continueStatement) {
        this.result = this.copier.Copy(continueStatement);
      }

      public void Visit(ICopyMemoryStatement copyMemoryBlock) {
        this.result = this.copier.Copy(copyMemoryBlock);
      }

      public void Visit(ICreateArray createArray) {
        this.result = this.copier.Copy(createArray);
      }

      public void Visit(ICreateDelegateInstance createDelegateInstance) {
        this.result = this.copier.Copy(createDelegateInstance);
      }

      public void Visit(ICreateObjectInstance createObjectInstance) {
        this.result = this.copier.Copy(createObjectInstance);
      }

      public void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        this.result = this.copier.Copy(debuggerBreakStatement);
      }

      public void Visit(IDefaultValue defaultValue) {
        this.result = this.copier.Copy(defaultValue);
      }

      public void Visit(IDivision division) {
        this.result = this.copier.Copy(division);
      }

      public void Visit(IDoUntilStatement doUntilStatement) {
        this.result = this.copier.Copy(doUntilStatement);
      }

      public void Visit(IDupValue dupValue) {
        this.result = this.copier.Copy(dupValue);
      }

      public void Visit(IEmptyStatement emptyStatement) {
        this.result = this.copier.Copy(emptyStatement);
      }

      public void Visit(IEquality equality) {
        this.result = this.copier.Copy(equality);
      }

      public void Visit(IExclusiveOr exclusiveOr) {
        this.result = this.copier.Copy(exclusiveOr);
      }

      public void Visit(IExpressionStatement expressionStatement) {
        this.result = this.copier.Copy(expressionStatement);
      }

      public void Visit(IFillMemoryStatement fillMemoryStatement) {
        this.result = this.copier.Copy(fillMemoryStatement);
      }

      public void Visit(IForEachStatement forEachStatement) {
        this.result = this.copier.Copy(forEachStatement);
      }

      public void Visit(IForStatement forStatement) {
        this.result = this.copier.Copy(forStatement);
      }

      public void Visit(IGotoStatement gotoStatement) {
        this.result = this.copier.Copy(gotoStatement);
      }

      public void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        this.result = this.copier.Copy(gotoSwitchCaseStatement);
      }

      public void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        this.result = this.copier.Copy(getTypeOfTypedReference);
      }

      public void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        this.result = this.copier.Copy(getValueOfTypedReference);
      }

      public void Visit(IGreaterThan greaterThan) {
        this.result = this.copier.Copy(greaterThan);
      }

      public void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        this.result = this.copier.Copy(greaterThanOrEqual);
      }

      public void Visit(ILabeledStatement labeledStatement) {
        this.result = this.copier.Copy(labeledStatement);
      }

      public void Visit(ILeftShift leftShift) {
        this.result = this.copier.Copy(leftShift);
      }

      public void Visit(ILessThan lessThan) {
        this.result = this.copier.Copy(lessThan);
      }

      public void Visit(ILessThanOrEqual lessThanOrEqual) {
        this.result = this.copier.Copy(lessThanOrEqual);
      }

      public void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        this.result = this.copier.Copy(localDeclarationStatement);
      }

      public void Visit(ILockStatement lockStatement) {
        this.result = this.copier.Copy(lockStatement);
      }

      public void Visit(ILogicalNot logicalNot) {
        this.result = this.copier.Copy(logicalNot);
      }

      public void Visit(IMakeTypedReference makeTypedReference) {
        this.result = this.copier.Copy(makeTypedReference);
      }

      public void Visit(IMethodCall methodCall) {
        this.result = this.copier.Copy(methodCall);
      }

      public void Visit(IModulus modulus) {
        this.result = this.copier.Copy(modulus);
      }

      public void Visit(IMultiplication multiplication) {
        this.result = this.copier.Copy(multiplication);
      }

      public void Visit(INamedArgument namedArgument) {
        this.result = this.copier.Copy(namedArgument);
      }

      public void Visit(INotEquality notEquality) {
        this.result = this.copier.Copy(notEquality);
      }

      public void Visit(IOldValue oldValue) {
        this.result = this.copier.Copy(oldValue);
      }

      public void Visit(IOnesComplement onesComplement) {
        this.result = this.copier.Copy(onesComplement);
      }

      public void Visit(IOutArgument outArgument) {
        this.result = this.copier.Copy(outArgument);
      }

      public void Visit(IPointerCall pointerCall) {
        this.result = this.copier.Copy(pointerCall);
      }

      public void Visit(IPopValue popValue) {
        this.result = this.copier.Copy(popValue);
      }

      public void Visit(IPushStatement pushStatement) {
        this.result = this.copier.Copy(pushStatement);
      }

      public void Visit(IRefArgument refArgument) {
        this.result = this.copier.Copy(refArgument);
      }

      public void Visit(IResourceUseStatement resourceUseStatement) {
        this.result = this.copier.Copy(resourceUseStatement);
      }

      public void Visit(IReturnValue returnValue) {
        this.result = this.copier.Copy(returnValue);
      }

      public void Visit(IRethrowStatement rethrowStatement) {
        this.result = this.copier.Copy(rethrowStatement);
      }

      public void Visit(IReturnStatement returnStatement) {
        this.result = this.copier.Copy(returnStatement);
      }

      public void Visit(IRightShift rightShift) {
        this.result = this.copier.Copy(rightShift);
      }

      public void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        this.result = this.copier.Copy(runtimeArgumentHandleExpression);
      }

      public void Visit(ISizeOf sizeOf) {
        this.result = this.copier.Copy(sizeOf);
      }

      public void Visit(IStackArrayCreate stackArrayCreate) {
        this.result = this.copier.Copy(stackArrayCreate);
      }

      public void Visit(ISubtraction subtraction) {
        this.result = this.copier.Copy(subtraction);
      }

      public void Visit(ISwitchCase switchCase) {
        this.result = this.copier.Copy(switchCase);
      }

      public void Visit(ISwitchStatement switchStatement) {
        this.result = this.copier.Copy(switchStatement);
      }

      public void Visit(ITargetExpression targetExpression) {
        this.result = this.copier.Copy(targetExpression);
      }

      public void Visit(IThisReference thisReference) {
        this.result = this.copier.Copy(thisReference);
      }

      public void Visit(IThrowStatement throwStatement) {
        this.result = this.copier.Copy(throwStatement);
      }

      public void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        this.result = this.copier.Copy(tryCatchFilterFinallyStatement);
      }

      public void Visit(ITokenOf tokenOf) {
        this.result = this.copier.Copy(tokenOf);
      }

      public void Visit(ITypeOf typeOf) {
        this.result = this.copier.Copy(typeOf);
      }

      public void Visit(IUnaryNegation unaryNegation) {
        this.result = this.copier.Copy(unaryNegation);
      }

      public void Visit(IUnaryPlus unaryPlus) {
        this.result = this.copier.Copy(unaryPlus);
      }

      public void Visit(IVectorLength vectorLength) {
        this.result = this.copier.Copy(vectorLength);
      }

      public void Visit(IWhileDoStatement whileDoStatement) {
        this.result = this.copier.Copy(whileDoStatement);
      }

      public void Visit(IYieldBreakStatement yieldBreakStatement) {
        this.result = this.copier.Copy(yieldBreakStatement);
      }

      public void Visit(IYieldReturnStatement yieldReturnStatement) {
        this.result = this.copier.Copy(yieldReturnStatement);
      }

      #endregion
    }

    /// <summary>
    /// Returns a shallow copy of the given addition.
    /// </summary>
    /// <param name="addition"></param>
    public Addition Copy(IAddition addition) {
      Contract.Requires(addition != null);
      Contract.Ensures(Contract.Result<Addition>() != null);

      return new Addition(addition);
    }

    /// <summary>
    /// Returns a shallow copy of the given addressable expression.
    /// </summary>
    /// <param name="addressableExpression"></param>
    public AddressableExpression Copy(IAddressableExpression addressableExpression) {
      Contract.Requires(addressableExpression != null);
      Contract.Ensures(Contract.Result<AddressableExpression>() != null);

      return new AddressableExpression(addressableExpression);
    }

    /// <summary>
    /// Returns a shallow copy of the given address dereference expression.
    /// </summary>
    /// <param name="addressDereference"></param>
    public AddressDereference Copy(IAddressDereference addressDereference) {
      Contract.Requires(addressDereference != null);
      Contract.Ensures(Contract.Result<AddressDereference>() != null);

      return new AddressDereference(addressDereference);
    }

    /// <summary>
    /// Returns a shallow copy of the given AddressOf expression.
    /// </summary>
    /// <param name="addressOf"></param>
    public AddressOf Copy(IAddressOf addressOf) {
      Contract.Requires(addressOf != null);
      Contract.Ensures(Contract.Result<AddressOf>() != null);

      return new AddressOf(addressOf);
    }

    /// <summary>
    /// Returns a shallow copy of the given anonymous delegate expression.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
    public AnonymousDelegate Copy(IAnonymousDelegate anonymousDelegate) {
      Contract.Requires(anonymousDelegate != null);
      Contract.Ensures(Contract.Result<AnonymousDelegate>() != null);

      return new AnonymousDelegate(anonymousDelegate);
    }

    /// <summary>
    /// Returns a shallow copy of the given array indexer expression.
    /// </summary>
    /// <param name="arrayIndexer"></param>
    public ArrayIndexer Copy(IArrayIndexer arrayIndexer) {
      Contract.Requires(arrayIndexer != null);
      Contract.Ensures(Contract.Result<ArrayIndexer>() != null);

      return new ArrayIndexer(arrayIndexer);
    }

    /// <summary>
    /// Returns a shallow copy of the given assert statement.
    /// </summary>
    /// <param name="assertStatement"></param>
    public AssertStatement Copy(IAssertStatement assertStatement) {
      Contract.Requires(assertStatement != null);
      Contract.Ensures(Contract.Result<AssertStatement>() != null);

      return new AssertStatement(assertStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given assignment expression.
    /// </summary>
    /// <param name="assignment"></param>
    public Assignment Copy(IAssignment assignment) {
      Contract.Requires(assignment != null);
      Contract.Ensures(Contract.Result<Assignment>() != null);

      return new Assignment(assignment);
    }

    /// <summary>
    /// Returns a shallow copy of the given assume statement.
    /// </summary>
    /// <param name="assumeStatement"></param>
    public AssumeStatement Copy(IAssumeStatement assumeStatement) {
      Contract.Requires(assumeStatement != null);
      Contract.Ensures(Contract.Result<AssumeStatement>() != null);

      return new AssumeStatement(assumeStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given bitwise and expression.
    /// </summary>
    /// <param name="bitwiseAnd"></param>
    public BitwiseAnd Copy(IBitwiseAnd bitwiseAnd) {
      Contract.Requires(bitwiseAnd != null);
      Contract.Ensures(Contract.Result<BitwiseAnd>() != null);

      return new BitwiseAnd(bitwiseAnd);
    }

    /// <summary>
    /// Returns a shallow copy of the given bitwise or expression.
    /// </summary>
    /// <param name="bitwiseOr"></param>
    public BitwiseOr Copy(IBitwiseOr bitwiseOr) {
      Contract.Requires(bitwiseOr != null);
      Contract.Ensures(Contract.Result<BitwiseOr>() != null);

      return new BitwiseOr(bitwiseOr);
    }

    /// <summary>
    /// Returns a shallow copy of the given block expression.
    /// </summary>
    /// <param name="blockExpression"></param>
    public BlockExpression Copy(IBlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);
      Contract.Ensures(Contract.Result<BlockExpression>() != null);

      return new BlockExpression(blockExpression);
    }

    /// <summary>
    /// Returns a shallow copy of the given statement block.
    /// </summary>
    /// <param name="block"></param>
    public BlockStatement Copy(IBlockStatement block) {
      Contract.Requires(block != null);
      Contract.Ensures(Contract.Result<BlockStatement>() != null);

      return new BlockStatement(block);
    }

    /// <summary>
    /// Returns a shallow copy of the given break statement.
    /// </summary>
    /// <param name="breakStatement"></param>
    public BreakStatement Copy(IBreakStatement breakStatement) {
      Contract.Requires(breakStatement != null);
      Contract.Ensures(Contract.Result<BreakStatement>() != null);

      return new BreakStatement(breakStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the cast-if-possible expression.
    /// </summary>
    /// <param name="castIfPossible"></param>
    public CastIfPossible Copy(ICastIfPossible castIfPossible) {
      Contract.Requires(castIfPossible != null);
      Contract.Ensures(Contract.Result<CastIfPossible>() != null);

      return new CastIfPossible(castIfPossible);
    }

    /// <summary>
    /// Returns a shallow copy of the given catch clause.
    /// </summary>
    /// <param name="catchClause"></param>
    public CatchClause Copy(ICatchClause catchClause) {
      Contract.Requires(catchClause != null);
      Contract.Ensures(Contract.Result<CatchClause>() != null);

      return new CatchClause(catchClause);
    }

    /// <summary>
    /// Returns a shallow copy of the given check-if-instance expression.
    /// </summary>
    /// <param name="checkIfInstance"></param>
    public CheckIfInstance Copy(ICheckIfInstance checkIfInstance) {
      Contract.Requires(checkIfInstance != null);
      Contract.Ensures(Contract.Result<CheckIfInstance>() != null);

      return new CheckIfInstance(checkIfInstance);
    }

    /// <summary>
    /// Returns a shallow copy of the given compile time constant.
    /// </summary>
    /// <param name="constant"></param>
    public CompileTimeConstant Copy(ICompileTimeConstant constant) {
      Contract.Requires(constant != null);
      Contract.Ensures(Contract.Result<CompileTimeConstant>() != null);

      return new CompileTimeConstant(constant);
    }

    /// <summary>
    /// Returns a shallow copy of the given conversion expression.
    /// </summary>
    /// <param name="conversion"></param>
    public Conversion Copy(IConversion conversion) {
      Contract.Requires(conversion != null);
      Contract.Ensures(Contract.Result<Conversion>() != null);

      return new Conversion(conversion);
    }

    /// <summary>
    /// Returns a shallow copy of the given conditional expression.
    /// </summary>
    /// <param name="conditional"></param>
    public Conditional Copy(IConditional conditional) {
      Contract.Requires(conditional != null);
      Contract.Ensures(Contract.Result<Conditional>() != null);

      return new Conditional(conditional);
    }

    /// <summary>
    /// Returns a shallow copy of the given conditional statement.
    /// </summary>
    /// <param name="conditionalStatement"></param>
    public ConditionalStatement Copy(IConditionalStatement conditionalStatement) {
      Contract.Requires(conditionalStatement != null);
      Contract.Ensures(Contract.Result<ConditionalStatement>() != null);

      var mutable = conditionalStatement as ConditionalStatement;
      if (mutable != null) return mutable.Clone();
      return new ConditionalStatement(conditionalStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given continue statement.
    /// </summary>
    /// <param name="continueStatement"></param>
    public ContinueStatement Copy(IContinueStatement continueStatement) {
      Contract.Requires(continueStatement != null);
      Contract.Ensures(Contract.Result<ContinueStatement>() != null);

      return new ContinueStatement(continueStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public CopyMemoryStatement Copy(ICopyMemoryStatement copyMemoryStatement) {
      Contract.Requires(copyMemoryStatement != null);
      Contract.Ensures(Contract.Result<CopyMemoryStatement>() != null);

      return new CopyMemoryStatement(copyMemoryStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
    public CreateArray Copy(ICreateArray createArray) {
      Contract.Requires(createArray != null);
      Contract.Ensures(Contract.Result<CreateArray>() != null);

      return new CreateArray(createArray);
    }

    /// <summary>
    /// Returns a shallow copy of the given constructor call expression.
    /// </summary>
    /// <param name="createObjectInstance"></param>
    public CreateObjectInstance Copy(ICreateObjectInstance createObjectInstance) {
      Contract.Requires(createObjectInstance != null);
      Contract.Ensures(Contract.Result<CreateObjectInstance>() != null);

      return new CreateObjectInstance(createObjectInstance);
    }

    /// <summary>
    /// Returns a shallow copy of the anonymous object creation expression.
    /// </summary>
    /// <param name="createDelegateInstance"></param>
    public CreateDelegateInstance Copy(ICreateDelegateInstance createDelegateInstance) {
      Contract.Requires(createDelegateInstance != null);
      Contract.Ensures(Contract.Result<CreateDelegateInstance>() != null);

      return new CreateDelegateInstance(createDelegateInstance);
    }

    /// <summary>
    /// Returns a shallow copy of the given defalut value expression.
    /// </summary>
    /// <param name="defaultValue"></param>
    public DefaultValue Copy(IDefaultValue defaultValue) {
      Contract.Requires(defaultValue != null);
      Contract.Ensures(Contract.Result<DefaultValue>() != null);

      return new DefaultValue(defaultValue);
    }

    /// <summary>
    /// Returns a shallow copy of the given division expression.
    /// </summary>
    /// <param name="division"></param>
    public Division Copy(IDivision division) {
      Contract.Requires(division != null);
      Contract.Ensures(Contract.Result<Division>() != null);

      return new Division(division);
    }

    /// <summary>
    /// Returns a shallow copy of the given do until statement.
    /// </summary>
    /// <param name="doUntilStatement"></param>
    public DoUntilStatement Copy(IDoUntilStatement doUntilStatement) {
      Contract.Requires(doUntilStatement != null);
      Contract.Ensures(Contract.Result<DoUntilStatement>() != null);

      return new DoUntilStatement(doUntilStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given dup value expression.
    /// </summary>
    /// <param name="dupValue"></param>
    public DupValue Copy(IDupValue dupValue) {
      Contract.Requires(dupValue != null);
      Contract.Ensures(Contract.Result<DupValue>() != null);

      return new DupValue(dupValue);
    }

    /// <summary>
    /// Returns a shallow copy of the given empty statement.
    /// </summary>
    /// <param name="emptyStatement"></param>
    public EmptyStatement Copy(IEmptyStatement emptyStatement) {
      Contract.Requires(emptyStatement != null);
      Contract.Ensures(Contract.Result<EmptyStatement>() != null);

      var mutable = emptyStatement as EmptyStatement;
      if (mutable != null) return mutable.Clone();
      return new EmptyStatement(emptyStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given equality expression.
    /// </summary>
    /// <param name="equality"></param>
    public Equality Copy(IEquality equality) {
      Contract.Requires(equality != null);
      Contract.Ensures(Contract.Result<Equality>() != null);

      return new Equality(equality);
    }

    /// <summary>
    /// Returns a shallow copy of the given exclusive or expression.
    /// </summary>
    /// <param name="exclusiveOr"></param>
    public ExclusiveOr Copy(IExclusiveOr exclusiveOr) {
      Contract.Requires(exclusiveOr != null);
      Contract.Ensures(Contract.Result<ExclusiveOr>() != null);

      return new ExclusiveOr(exclusiveOr);
    }

    /// <summary>
    /// Returns a shallow copy of the given bound expression.
    /// </summary>
    /// <param name="boundExpression"></param>
    public BoundExpression Copy(IBoundExpression boundExpression) {
      Contract.Requires(boundExpression != null);
      Contract.Ensures(Contract.Result<BoundExpression>() != null);

      return new BoundExpression(boundExpression);
    }

    /// <summary>
    /// Returns a shallow copy of the given debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement"></param>
    public DebuggerBreakStatement Copy(IDebuggerBreakStatement debuggerBreakStatement) {
      Contract.Requires(debuggerBreakStatement != null);
      Contract.Ensures(Contract.Result<DebuggerBreakStatement>() != null);

      return new DebuggerBreakStatement(debuggerBreakStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given expression.
    /// </summary>
    /// <param name="expression"></param>
    public Expression Copy(IExpression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<Expression>() != null);

      expression.Dispatch(this.Dispatcher);
      return (Expression)this.Dispatcher.result;
    }

    /// <summary>
    /// Returns a shallow copy of the given expression statement.
    /// </summary>
    /// <param name="expressionStatement"></param>
    public ExpressionStatement Copy(IExpressionStatement expressionStatement) {
      Contract.Requires(expressionStatement != null);
      Contract.Ensures(Contract.Result<ExpressionStatement>() != null);

      return new ExpressionStatement(expressionStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given fill memory block statement.
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    public FillMemoryStatement Copy(IFillMemoryStatement fillMemoryStatement) {
      Contract.Requires(fillMemoryStatement != null);
      Contract.Ensures(Contract.Result<FillMemoryStatement>() != null);

      return new FillMemoryStatement(fillMemoryStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given foreach statement.
    /// </summary>
    /// <param name="forEachStatement"></param>
    public ForEachStatement Copy(IForEachStatement forEachStatement) {
      Contract.Requires(forEachStatement != null);
      Contract.Ensures(Contract.Result<ForEachStatement>() != null);

      return new ForEachStatement(forEachStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given for statement.
    /// </summary>
    /// <param name="forStatement"></param>
    public ForStatement Copy(IForStatement forStatement) {
      Contract.Requires(forStatement != null);
      Contract.Ensures(Contract.Result<ForStatement>() != null);

      var mutable = forStatement as ForStatement;
      if (mutable != null) return mutable.Clone();
      return new ForStatement(forStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given get type of typed reference expression.
    /// </summary>
    /// <param name="getTypeOfTypedReference"></param>
    public GetTypeOfTypedReference Copy(IGetTypeOfTypedReference getTypeOfTypedReference) {
      Contract.Requires(getTypeOfTypedReference != null);
      Contract.Ensures(Contract.Result<GetTypeOfTypedReference>() != null);

      return new GetTypeOfTypedReference(getTypeOfTypedReference);
    }

    /// <summary>
    /// Returns a shallow copy of the given get value of typed reference expression.
    /// </summary>
    /// <param name="getValueOfTypedReference"></param>
    public GetValueOfTypedReference Copy(IGetValueOfTypedReference getValueOfTypedReference) {
      Contract.Requires(getValueOfTypedReference != null);
      Contract.Ensures(Contract.Result<GetValueOfTypedReference>() != null);

      return new GetValueOfTypedReference(getValueOfTypedReference);
    }

    /// <summary>
    /// Returns a shallow copy of the given goto statement.
    /// </summary>
    /// <param name="gotoStatement"></param>
    public GotoStatement Copy(IGotoStatement gotoStatement) {
      Contract.Requires(gotoStatement != null);
      Contract.Ensures(Contract.Result<GotoStatement>() != null);

      return new GotoStatement(gotoStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public GotoSwitchCaseStatement Copy(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      Contract.Requires(gotoSwitchCaseStatement != null);
      Contract.Ensures(Contract.Result<GotoSwitchCaseStatement>() != null);

      return new GotoSwitchCaseStatement(gotoSwitchCaseStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given greater-than expression.
    /// </summary>
    /// <param name="greaterThan"></param>
    public GreaterThan Copy(IGreaterThan greaterThan) {
      Contract.Requires(greaterThan != null);
      Contract.Ensures(Contract.Result<GreaterThan>() != null);

      return new GreaterThan(greaterThan);
    }

    /// <summary>
    /// Returns a shallow copy of the given greater-than-or-equal expression.
    /// </summary>
    /// <param name="greaterThanOrEqual"></param>
    public GreaterThanOrEqual Copy(IGreaterThanOrEqual greaterThanOrEqual) {
      Contract.Requires(greaterThanOrEqual != null);
      Contract.Ensures(Contract.Result<GreaterThanOrEqual>() != null);

      return new GreaterThanOrEqual(greaterThanOrEqual);
    }

    /// <summary>
    /// Returns a shallow copy of the given labeled statement.
    /// </summary>
    /// <param name="labeledStatement"></param>
    public LabeledStatement Copy(ILabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);
      Contract.Ensures(Contract.Result<LabeledStatement>() != null);

      return new LabeledStatement(labeledStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given left shift expression.
    /// </summary>
    /// <param name="leftShift"></param>
    public LeftShift Copy(ILeftShift leftShift) {
      Contract.Requires(leftShift != null);
      Contract.Ensures(Contract.Result<LeftShift>() != null);

      return new LeftShift(leftShift);
    }

    /// <summary>
    /// Returns a shallow copy of the given less-than expression.
    /// </summary>
    /// <param name="lessThan"></param>
    public LessThan Copy(ILessThan lessThan) {
      Contract.Requires(lessThan != null);
      Contract.Ensures(Contract.Result<LessThan>() != null);

      return new LessThan(lessThan);
    }

    /// <summary>
    /// Returns a shallow copy of the given less-than-or-equal expression.
    /// </summary>
    /// <param name="lessThanOrEqual"></param>
    public LessThanOrEqual Copy(ILessThanOrEqual lessThanOrEqual) {
      Contract.Requires(lessThanOrEqual != null);
      Contract.Ensures(Contract.Result<LessThanOrEqual>() != null);

      return new LessThanOrEqual(lessThanOrEqual);
    }

    /// <summary>
    /// Returns a shallow copy of the given local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement"></param>
    public LocalDeclarationStatement Copy(ILocalDeclarationStatement localDeclarationStatement) {
      Contract.Requires(localDeclarationStatement != null);
      Contract.Ensures(Contract.Result<LocalDeclarationStatement>() != null);

      return new LocalDeclarationStatement(localDeclarationStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given lock statement.
    /// </summary>
    /// <param name="lockStatement"></param>
    public LockStatement Copy(ILockStatement lockStatement) {
      Contract.Requires(lockStatement != null);
      Contract.Ensures(Contract.Result<LockStatement>() != null);

      return new LockStatement(lockStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given logical not expression.
    /// </summary>
    /// <param name="logicalNot"></param>
    public LogicalNot Copy(ILogicalNot logicalNot) {
      Contract.Requires(logicalNot != null);
      Contract.Ensures(Contract.Result<LogicalNot>() != null);

      return new LogicalNot(logicalNot);
    }

    /// <summary>
    /// Returns a shallow copy of the given make typed reference expression.
    /// </summary>
    /// <param name="makeTypedReference"></param>
    public MakeTypedReference Copy(IMakeTypedReference makeTypedReference) {
      Contract.Requires(makeTypedReference != null);
      Contract.Ensures(Contract.Result<MakeTypedReference>() != null);

      return new MakeTypedReference(makeTypedReference);
    }

    /// <summary>
    /// Returns a shallow copy of the given method call.
    /// </summary>
    /// <param name="methodCall"></param>
    public MethodCall Copy(IMethodCall methodCall) {
      Contract.Requires(methodCall != null);
      Contract.Ensures(Contract.Result<MethodCall>() != null);

      return new MethodCall(methodCall);
    }

    /// <summary>
    /// Returns a shallow copy of the given modulus expression.
    /// </summary>
    /// <param name="modulus"></param>
    public Modulus Copy(IModulus modulus) {
      Contract.Requires(modulus != null);
      Contract.Ensures(Contract.Result<Modulus>() != null);

      return new Modulus(modulus);
    }

    /// <summary>
    /// Returns a shallow copy of the given multiplication expression.
    /// </summary>
    /// <param name="multiplication"></param>
    public Multiplication Copy(IMultiplication multiplication) {
      Contract.Requires(multiplication != null);
      Contract.Ensures(Contract.Result<Multiplication>() != null);

      return new Multiplication(multiplication);
    }

    /// <summary>
    /// Visits the specified named arguments.
    /// </summary>
    /// <param name="namedArguments">The named arguments.</param>
    public virtual void Visit(IEnumerable<INamedArgument> namedArguments) {
      Contract.Requires(namedArguments != null);
    }

    /// <summary>
    /// Returns a shallow copy of the given named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
    public NamedArgument Copy(INamedArgument namedArgument) {
      Contract.Requires(namedArgument != null);
      Contract.Ensures(Contract.Result<NamedArgument>() != null);

      return new NamedArgument(namedArgument);
    }

    /// <summary>
    /// Returns a shallow copy of the given not equality expression.
    /// </summary>
    /// <param name="notEquality"></param>
    public NotEquality Copy(INotEquality notEquality) {
      Contract.Requires(notEquality != null);
      Contract.Ensures(Contract.Result<NotEquality>() != null);

      return new NotEquality(notEquality);
    }

    /// <summary>
    /// Returns a shallow copy of the given old value expression.
    /// </summary>
    /// <param name="oldValue"></param>
    public OldValue Copy(IOldValue oldValue) {
      Contract.Requires(oldValue != null);
      Contract.Ensures(Contract.Result<OldValue>() != null);

      return new OldValue(oldValue);
    }

    /// <summary>
    /// Returns a shallow copy of the given one's complement expression.
    /// </summary>
    /// <param name="onesComplement"></param>
    public OnesComplement Copy(IOnesComplement onesComplement) {
      Contract.Requires(onesComplement != null);
      Contract.Ensures(Contract.Result<OnesComplement>() != null);

      return new OnesComplement(onesComplement);
    }

    /// <summary>
    /// Returns a shallow copy of the given out argument expression.
    /// </summary>
    /// <param name="outArgument"></param>
    public OutArgument Copy(IOutArgument outArgument) {
      Contract.Requires(outArgument != null);
      Contract.Ensures(Contract.Result<OutArgument>() != null);

      return new OutArgument(outArgument);
    }

    /// <summary>
    /// Returns a shallow copy of the given pointer call.
    /// </summary>
    /// <param name="pointerCall"></param>
    public PointerCall Copy(IPointerCall pointerCall) {
      Contract.Requires(pointerCall != null);
      Contract.Ensures(Contract.Result<PointerCall>() != null);

      return new PointerCall(pointerCall);
    }

    /// <summary>
    /// Returns a shallow copy of the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public PopValue Copy(IPopValue popValue) {
      Contract.Requires(popValue != null);
      Contract.Ensures(Contract.Result<PopValue>() != null);

      return new PopValue(popValue);
    }

    /// <summary>
    /// Returns a shallow copy of the given push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    public PushStatement Copy(IPushStatement pushStatement) {
      Contract.Requires(pushStatement != null);
      Contract.Ensures(Contract.Result<PushStatement>() != null);

      return new PushStatement(pushStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given ref argument expression.
    /// </summary>
    /// <param name="refArgument"></param>
    public RefArgument Copy(IRefArgument refArgument) {
      Contract.Requires(refArgument != null);
      Contract.Ensures(Contract.Result<RefArgument>() != null);

      return new RefArgument(refArgument);
    }

    /// <summary>
    /// Returns a shallow copy of the given resource usage statement.
    /// </summary>
    /// <param name="resourceUseStatement"></param>
    public ResourceUseStatement Copy(IResourceUseStatement resourceUseStatement) {
      Contract.Requires(resourceUseStatement != null);
      Contract.Ensures(Contract.Result<ResourceUseStatement>() != null);

      return new ResourceUseStatement(resourceUseStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement"></param>
    public RethrowStatement Copy(IRethrowStatement rethrowStatement) {
      Contract.Requires(rethrowStatement != null);
      Contract.Ensures(Contract.Result<RethrowStatement>() != null);

      return new RethrowStatement(rethrowStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the return statement.
    /// </summary>
    /// <param name="returnStatement"></param>
    public ReturnStatement Copy(IReturnStatement returnStatement) {
      Contract.Requires(returnStatement != null);
      Contract.Ensures(Contract.Result<ReturnStatement>() != null);

      return new ReturnStatement(returnStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given return value expression.
    /// </summary>
    /// <param name="returnValue"></param>
    public ReturnValue Copy(IReturnValue returnValue) {
      Contract.Requires(returnValue != null);
      Contract.Ensures(Contract.Result<ReturnValue>() != null);

      return new ReturnValue(returnValue);
    }

    /// <summary>
    /// Returns a shallow copy of the given right shift expression.
    /// </summary>
    /// <param name="rightShift"></param>
    public RightShift Copy(IRightShift rightShift) {
      Contract.Requires(rightShift != null);
      Contract.Ensures(Contract.Result<RightShift>() != null);

      return new RightShift(rightShift);
    }

    /// <summary>
    /// Returns a shallow copy of the given stack array create expression.
    /// </summary>
    /// <param name="stackArrayCreate"></param>
    public StackArrayCreate Copy(IStackArrayCreate stackArrayCreate) {
      Contract.Requires(stackArrayCreate != null);
      Contract.Ensures(Contract.Result<StackArrayCreate>() != null);

      return new StackArrayCreate(stackArrayCreate);
    }

    /// <summary>
    /// Returns a shallow copy of the given runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression"></param>
    public RuntimeArgumentHandleExpression Copy(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      Contract.Requires(runtimeArgumentHandleExpression != null);
      Contract.Ensures(Contract.Result<RuntimeArgumentHandleExpression>() != null);

      return new RuntimeArgumentHandleExpression(runtimeArgumentHandleExpression);
    }

    /// <summary>
    /// Returns a shallow copy of the given sizeof() expression.
    /// </summary>
    /// <param name="sizeOf"></param>
    public SizeOf Copy(ISizeOf sizeOf) {
      Contract.Requires(sizeOf != null);
      Contract.Ensures(Contract.Result<SizeOf>() != null);

      return new SizeOf(sizeOf);
    }

    /// <summary>
    /// Returns a shallow copy of the given source method body.
    /// </summary>
    /// <param name="sourceMethodBody"></param>
    public SourceMethodBody Copy(ISourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      Contract.Ensures(Contract.Result<SourceMethodBody>() != null);

      var copy = new SourceMethodBody(this.targetHost, this.sourceLocationProvider, this.localScopeProvider);
      copy.Block = sourceMethodBody.Block;
      copy.LocalsAreZeroed = sourceMethodBody.LocalsAreZeroed;
      copy.MethodDefinition = sourceMethodBody.MethodDefinition;
      return copy;
    }

    /// <summary>
    /// Visits the specified statements.
    /// </summary>
    /// <param name="statements">The statements.</param>
    public virtual void Visit(IEnumerable<IStatement> statements) {
    }

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public Statement Copy(IStatement statement) {
      Contract.Requires(statement != null);
      Contract.Ensures(Contract.Result<Statement>() != null);

      statement.Dispatch(this.Dispatcher);
      return (Statement)this.Dispatcher.result;
    }

    /// <summary>
    /// Returns a shallow copy of the given subtraction expression.
    /// </summary>
    /// <param name="subtraction"></param>
    public Subtraction Copy(ISubtraction subtraction) {
      Contract.Requires(subtraction != null);
      Contract.Ensures(Contract.Result<Subtraction>() != null);

      return new Subtraction(subtraction);
    }

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
    public virtual void Visit(IEnumerable<ISwitchCase> switchCases) {
      Contract.Requires(switchCases != null);
    }

    /// <summary>
    /// Returns a shallow copy of the given switch case.
    /// </summary>
    /// <param name="switchCase"></param>
    public SwitchCase Copy(ISwitchCase switchCase) {
      Contract.Requires(switchCase != null);
      Contract.Ensures(Contract.Result<SwitchCase>() != null);

      return new SwitchCase(switchCase);
    }

    /// <summary>
    /// Returns a shallow copy of the given switch statement.
    /// </summary>
    /// <param name="switchStatement"></param>
    public SwitchStatement Copy(ISwitchStatement switchStatement) {
      Contract.Requires(switchStatement != null);
      Contract.Ensures(Contract.Result<SwitchStatement>() != null);

      return new SwitchStatement(switchStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given target expression.
    /// </summary>
    /// <param name="targetExpression"></param>
    public TargetExpression Copy(ITargetExpression targetExpression) {
      Contract.Requires(targetExpression != null);
      Contract.Ensures(Contract.Result<TargetExpression>() != null);

      return new TargetExpression(targetExpression);
    }

    /// <summary>
    /// Returns a shallow copy of the given this reference expression.
    /// </summary>
    /// <param name="thisReference"></param>
    public ThisReference Copy(IThisReference thisReference) {
      Contract.Requires(thisReference != null);
      Contract.Ensures(Contract.Result<ThisReference>() != null);

      return new ThisReference(thisReference);
    }

    /// <summary>
    /// Returns a shallow copy of the throw statement.
    /// </summary>
    /// <param name="throwStatement"></param>
    public ThrowStatement Copy(IThrowStatement throwStatement) {
      Contract.Requires(throwStatement != null);
      Contract.Ensures(Contract.Result<ThrowStatement>() != null);

      return new ThrowStatement(throwStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the try-catch-filter-finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement"></param>
    public TryCatchFinallyStatement Copy(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      Contract.Requires(tryCatchFilterFinallyStatement != null);
      Contract.Ensures(Contract.Result<TryCatchFinallyStatement>() != null);

      return new TryCatchFinallyStatement(tryCatchFilterFinallyStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given tokenof() expression.
    /// </summary>
    /// <param name="tokenOf"></param>
    public TokenOf Copy(ITokenOf tokenOf) {
      Contract.Requires(tokenOf != null);
      Contract.Ensures(Contract.Result<TokenOf>() != null);

      return new TokenOf(tokenOf);
    }

    /// <summary>
    /// Returns a shallow copy of the given typeof() expression.
    /// </summary>
    /// <param name="typeOf"></param>
    public TypeOf Copy(ITypeOf typeOf) {
      Contract.Requires(typeOf != null);
      Contract.Ensures(Contract.Result<TypeOf>() != null);

      return new TypeOf(typeOf);
    }

    /// <summary>
    /// Returns a shallow copy of the given unary negation expression.
    /// </summary>
    /// <param name="unaryNegation"></param>
    public UnaryNegation Copy(IUnaryNegation unaryNegation) {
      Contract.Requires(unaryNegation != null);
      Contract.Ensures(Contract.Result<UnaryNegation>() != null);

      return new UnaryNegation(unaryNegation);
    }

    /// <summary>
    /// Returns a shallow copy of the given unary plus expression.
    /// </summary>
    /// <param name="unaryPlus"></param>
    public UnaryPlus Copy(IUnaryPlus unaryPlus) {
      Contract.Requires(unaryPlus != null);
      Contract.Ensures(Contract.Result<UnaryPlus>() != null);

      return new UnaryPlus(unaryPlus);
    }

    /// <summary>
    /// Returns a shallow copy of the given vector length expression.
    /// </summary>
    /// <param name="vectorLength"></param>
    public VectorLength Copy(IVectorLength vectorLength) {
      Contract.Requires(vectorLength != null);
      Contract.Ensures(Contract.Result<VectorLength>() != null);

      return new VectorLength(vectorLength);
    }

    /// <summary>
    /// Returns a shallow copy of the given while do statement.
    /// </summary>
    /// <param name="whileDoStatement"></param>
    public WhileDoStatement Copy(IWhileDoStatement whileDoStatement) {
      Contract.Requires(whileDoStatement != null);
      Contract.Ensures(Contract.Result<WhileDoStatement>() != null);

      return new WhileDoStatement(whileDoStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public YieldBreakStatement Copy(IYieldBreakStatement yieldBreakStatement) {
      Contract.Requires(yieldBreakStatement != null);
      Contract.Ensures(Contract.Result<YieldBreakStatement>() != null);

      return new YieldBreakStatement(yieldBreakStatement);
    }

    /// <summary>
    /// Returns a shallow copy of the given yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement"></param>
    public YieldReturnStatement Copy(IYieldReturnStatement yieldReturnStatement) {
      Contract.Requires(yieldReturnStatement != null);
      Contract.Ensures(Contract.Result<YieldReturnStatement>() != null);

      return new YieldReturnStatement(yieldReturnStatement);
    }


  }

  /// <summary>
  /// 
  /// </summary>
  public class CodeDeepCopier : MetadataDeepCopier {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    /// <param name="sourceLocationProvider"></param>
    /// <param name="localScopeProvider"></param>
    public CodeDeepCopier(IMetadataHost targetHost, ISourceLocationProvider sourceLocationProvider = null, ILocalScopeProvider localScopeProvider = null)
      : this(targetHost, new CodeShallowCopier(targetHost, sourceLocationProvider, localScopeProvider)) {
      Contract.Requires(targetHost != null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    /// <param name="targetUnit">The unit of metadata into which copies made by this copier will be inserted.</param>
    /// <param name="sourceLocationProvider"></param>
    public CodeDeepCopier(IMetadataHost targetHost, IUnit targetUnit, ISourceLocationProvider sourceLocationProvider = null)
      : this(targetHost, new CodeShallowCopier(targetHost, targetUnit, sourceLocationProvider)) {
      Contract.Requires(targetHost != null);
      Contract.Requires(targetUnit != null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    /// <param name="shallowCopier"></param>
    protected CodeDeepCopier(IMetadataHost targetHost, CodeShallowCopier shallowCopier)
      : base(targetHost, shallowCopier) {
      Contract.Requires(targetHost != null);
      Contract.Requires(shallowCopier != null);
      this.shallowCopier = shallowCopier;
    }

    CodeShallowCopier shallowCopier;

    private CodeDispatcher Dispatcher {
      get {
        if (this.dispatcher == null)
          this.dispatcher = new CodeDispatcher() { copier = this };
        return dispatcher;
      }
    }
    CodeDispatcher dispatcher;

    class CodeDispatcher : MetadataDispatcher, ICodeVisitor {

      internal object result;
      internal CodeDeepCopier copier;

      #region ICodeVisitor Members

      public void Visit(IAddition addition) {
        this.result = this.copier.Copy(addition);
      }

      public void Visit(IAddressableExpression addressableExpression) {
        this.result = this.copier.Copy(addressableExpression);
      }

      public void Visit(IAddressDereference addressDereference) {
        this.result = this.copier.Copy(addressDereference);
      }

      public void Visit(IAddressOf addressOf) {
        this.result = this.copier.Copy(addressOf);
      }

      public void Visit(IAnonymousDelegate anonymousDelegate) {
        this.result = this.copier.Copy(anonymousDelegate);
      }

      public void Visit(IArrayIndexer arrayIndexer) {
        this.result = this.copier.Copy(arrayIndexer);
      }

      public void Visit(IAssertStatement assertStatement) {
        this.result = this.copier.Copy(assertStatement);
      }

      public void Visit(IAssignment assignment) {
        this.result = this.copier.Copy(assignment);
      }

      public void Visit(IAssumeStatement assumeStatement) {
        this.result = this.copier.Copy(assumeStatement);
      }

      public void Visit(IBitwiseAnd bitwiseAnd) {
        this.result = this.copier.Copy(bitwiseAnd);
      }

      public void Visit(IBitwiseOr bitwiseOr) {
        this.result = this.copier.Copy(bitwiseOr);
      }

      public void Visit(IBlockExpression blockExpression) {
        this.result = this.copier.Copy(blockExpression);
      }

      public void Visit(IBlockStatement block) {
        this.result = this.copier.Copy(block);
      }

      public void Visit(IBreakStatement breakStatement) {
        this.result = this.copier.Copy(breakStatement);
      }

      public void Visit(IBoundExpression boundExpression) {
        this.result = this.copier.Copy(boundExpression);
      }

      public void Visit(ICastIfPossible castIfPossible) {
        this.result = this.copier.Copy(castIfPossible);
      }

      public void Visit(ICatchClause catchClause) {
        this.result = this.copier.Copy(catchClause);
      }

      public void Visit(ICheckIfInstance checkIfInstance) {
        this.result = this.copier.Copy(checkIfInstance);
      }

      public void Visit(ICompileTimeConstant constant) {
        this.result = this.copier.Copy(constant);
      }

      public void Visit(IConversion conversion) {
        this.result = this.copier.Copy(conversion);
      }

      public void Visit(IConditional conditional) {
        this.result = this.copier.Copy(conditional);
      }

      public void Visit(IConditionalStatement conditionalStatement) {
        this.result = this.copier.Copy(conditionalStatement);
      }

      public void Visit(IContinueStatement continueStatement) {
        this.result = this.copier.Copy(continueStatement);
      }

      public void Visit(ICopyMemoryStatement copyMemoryBlock) {
        this.result = this.copier.Copy(copyMemoryBlock);
      }

      public void Visit(ICreateArray createArray) {
        this.result = this.copier.Copy(createArray);
      }

      public void Visit(ICreateDelegateInstance createDelegateInstance) {
        this.result = this.copier.Copy(createDelegateInstance);
      }

      public void Visit(ICreateObjectInstance createObjectInstance) {
        this.result = this.copier.Copy(createObjectInstance);
      }

      public void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        this.result = this.copier.Copy(debuggerBreakStatement);
      }

      public void Visit(IDefaultValue defaultValue) {
        this.result = this.copier.Copy(defaultValue);
      }

      public void Visit(IDivision division) {
        this.result = this.copier.Copy(division);
      }

      public void Visit(IDoUntilStatement doUntilStatement) {
        this.result = this.copier.Copy(doUntilStatement);
      }

      public void Visit(IDupValue dupValue) {
        this.result = this.copier.Copy(dupValue);
      }

      public void Visit(IEmptyStatement emptyStatement) {
        this.result = this.copier.Copy(emptyStatement);
      }

      public void Visit(IEquality equality) {
        this.result = this.copier.Copy(equality);
      }

      public void Visit(IExclusiveOr exclusiveOr) {
        this.result = this.copier.Copy(exclusiveOr);
      }

      public void Visit(IExpressionStatement expressionStatement) {
        this.result = this.copier.Copy(expressionStatement);
      }

      public void Visit(IFillMemoryStatement fillMemoryStatement) {
        this.result = this.copier.Copy(fillMemoryStatement);
      }

      public void Visit(IForEachStatement forEachStatement) {
        this.result = this.copier.Copy(forEachStatement);
      }

      public void Visit(IForStatement forStatement) {
        this.result = this.copier.Copy(forStatement);
      }

      public void Visit(IGotoStatement gotoStatement) {
        this.result = this.copier.Copy(gotoStatement);
      }

      public void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        this.result = this.copier.Copy(gotoSwitchCaseStatement);
      }

      public void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        this.result = this.copier.Copy(getTypeOfTypedReference);
      }

      public void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        this.result = this.copier.Copy(getValueOfTypedReference);
      }

      public void Visit(IGreaterThan greaterThan) {
        this.result = this.copier.Copy(greaterThan);
      }

      public void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        this.result = this.copier.Copy(greaterThanOrEqual);
      }

      public void Visit(ILabeledStatement labeledStatement) {
        this.result = this.copier.Copy(labeledStatement);
      }

      public void Visit(ILeftShift leftShift) {
        this.result = this.copier.Copy(leftShift);
      }

      public void Visit(ILessThan lessThan) {
        this.result = this.copier.Copy(lessThan);
      }

      public void Visit(ILessThanOrEqual lessThanOrEqual) {
        this.result = this.copier.Copy(lessThanOrEqual);
      }

      public void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        this.result = this.copier.Copy(localDeclarationStatement);
      }

      public void Visit(ILockStatement lockStatement) {
        this.result = this.copier.Copy(lockStatement);
      }

      public void Visit(ILogicalNot logicalNot) {
        this.result = this.copier.Copy(logicalNot);
      }

      public void Visit(IMakeTypedReference makeTypedReference) {
        this.result = this.copier.Copy(makeTypedReference);
      }

      public void Visit(IMethodCall methodCall) {
        this.result = this.copier.Copy(methodCall);
      }

      public void Visit(IModulus modulus) {
        this.result = this.copier.Copy(modulus);
      }

      public void Visit(IMultiplication multiplication) {
        this.result = this.copier.Copy(multiplication);
      }

      public void Visit(INamedArgument namedArgument) {
        this.result = this.copier.Copy(namedArgument);
      }

      public void Visit(INotEquality notEquality) {
        this.result = this.copier.Copy(notEquality);
      }

      public void Visit(IOldValue oldValue) {
        this.result = this.copier.Copy(oldValue);
      }

      public void Visit(IOnesComplement onesComplement) {
        this.result = this.copier.Copy(onesComplement);
      }

      public void Visit(IOutArgument outArgument) {
        this.result = this.copier.Copy(outArgument);
      }

      public void Visit(IPointerCall pointerCall) {
        this.result = this.copier.Copy(pointerCall);
      }

      public void Visit(IPopValue popValue) {
        this.result = this.copier.Copy(popValue);
      }

      public void Visit(IPushStatement pushStatement) {
        this.result = this.copier.Copy(pushStatement);
      }

      public void Visit(IRefArgument refArgument) {
        this.result = this.copier.Copy(refArgument);
      }

      public void Visit(IResourceUseStatement resourceUseStatement) {
        this.result = this.copier.Copy(resourceUseStatement);
      }

      public void Visit(IReturnValue returnValue) {
        this.result = this.copier.Copy(returnValue);
      }

      public void Visit(IRethrowStatement rethrowStatement) {
        this.result = this.copier.Copy(rethrowStatement);
      }

      public void Visit(IReturnStatement returnStatement) {
        this.result = this.copier.Copy(returnStatement);
      }

      public void Visit(IRightShift rightShift) {
        this.result = this.copier.Copy(rightShift);
      }

      public void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        this.result = this.copier.Copy(runtimeArgumentHandleExpression);
      }

      public void Visit(ISizeOf sizeOf) {
        this.result = this.copier.Copy(sizeOf);
      }

      public void Visit(IStackArrayCreate stackArrayCreate) {
        this.result = this.copier.Copy(stackArrayCreate);
      }

      public void Visit(ISubtraction subtraction) {
        this.result = this.copier.Copy(subtraction);
      }

      public void Visit(ISwitchCase switchCase) {
        this.result = this.copier.Copy(switchCase);
      }

      public void Visit(ISwitchStatement switchStatement) {
        this.result = this.copier.Copy(switchStatement);
      }

      public void Visit(ITargetExpression targetExpression) {
        this.result = this.copier.Copy(targetExpression);
      }

      public void Visit(IThisReference thisReference) {
        this.result = this.copier.Copy(thisReference);
      }

      public void Visit(IThrowStatement throwStatement) {
        this.result = this.copier.Copy(throwStatement);
      }

      public void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        this.result = this.copier.Copy(tryCatchFilterFinallyStatement);
      }

      public void Visit(ITokenOf tokenOf) {
        this.result = this.copier.Copy(tokenOf);
      }

      public void Visit(ITypeOf typeOf) {
        this.result = this.copier.Copy(typeOf);
      }

      public void Visit(IUnaryNegation unaryNegation) {
        this.result = this.copier.Copy(unaryNegation);
      }

      public void Visit(IUnaryPlus unaryPlus) {
        this.result = this.copier.Copy(unaryPlus);
      }

      public void Visit(IVectorLength vectorLength) {
        this.result = this.copier.Copy(vectorLength);
      }

      public void Visit(IWhileDoStatement whileDoStatement) {
        this.result = this.copier.Copy(whileDoStatement);
      }

      public void Visit(IYieldBreakStatement yieldBreakStatement) {
        this.result = this.copier.Copy(yieldBreakStatement);
      }

      public void Visit(IYieldReturnStatement yieldReturnStatement) {
        this.result = this.copier.Copy(yieldReturnStatement);
      }

      #endregion
    }

    private Dictionary<ILocalDefinition, ILocalDefinition> LocalsInsideCone {
      get {
        if (this.localsInsideCone == null)
          this.localsInsideCone = new Dictionary<ILocalDefinition, ILocalDefinition>();
        return this.localsInsideCone;
      }
    }
    Dictionary<ILocalDefinition, ILocalDefinition> localsInsideCone;

    private new IFieldReference Copy(IFieldReference fieldReference) {
      Contract.Requires(fieldReference != null);
      Contract.Ensures(Contract.Result<IFieldReference>() != null);

      if (fieldReference is Dummy) return fieldReference;
      return base.Copy(fieldReference);
    }

    private new IMethodReference Copy(IMethodReference methodReference) {
      Contract.Requires(methodReference != null);
      Contract.Ensures(Contract.Result<IMethodReference>() != null);

      if (methodReference is Dummy) return methodReference;
      return base.Copy(methodReference);
    }

    private new ITypeReference Copy(ITypeReference typeReference) {
      Contract.Requires(typeReference != null);
      Contract.Ensures(Contract.Result<ITypeReference>() != null);

      if (typeReference is Dummy) return typeReference;
      return base.Copy(typeReference);
    }

    /// <summary>
    /// Returns a deep copy of the given addition.
    /// </summary>
    /// <param name="addition"></param>
    public Addition Copy(IAddition addition) {
      Contract.Requires(addition != null);
      Contract.Ensures(Contract.Result<Addition>() != null);

      var mutableCopy = this.shallowCopier.Copy(addition);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given addressable expression.
    /// </summary>
    /// <param name="addressableExpression"></param>
    public AddressableExpression Copy(IAddressableExpression addressableExpression) {
      Contract.Requires(addressableExpression != null);
      Contract.Ensures(Contract.Result<AddressableExpression>() != null);

      var mutableCopy = this.shallowCopier.Copy(addressableExpression);
      this.CopyChildren((Expression)mutableCopy);
      if (mutableCopy.Instance != null)
        mutableCopy.Instance = this.Copy(mutableCopy.Instance);
      var local = mutableCopy.Definition as ILocalDefinition;
      if (local != null)
        mutableCopy.Definition = this.GetExistingCopyIfInsideCone(local);
      else {
        var parameter = mutableCopy.Definition as IParameterDefinition;
        if (parameter != null)
          mutableCopy.Definition = this.GetExistingCopyIfInsideCone(parameter);
        else {
          var fieldReference = mutableCopy.Definition as IFieldReference;
          if (fieldReference != null)
            mutableCopy.Definition = this.Copy(fieldReference);
          else {
            var arrayIndexer = mutableCopy.Definition as IArrayIndexer;
            if (arrayIndexer != null)
              mutableCopy.Definition = this.Copy(arrayIndexer);
            else {
              var methodReference = addressableExpression.Definition as IMethodReference;
              if (methodReference != null)
                mutableCopy.Definition = this.Copy(methodReference);
              else {
                var expression = (IExpression)mutableCopy.Definition;
                mutableCopy.Definition = this.Copy(expression);
              }
            }
          }
        }
      }
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given address dereference expression.
    /// </summary>
    /// <param name="addressDereference"></param>
    public AddressDereference Copy(IAddressDereference addressDereference) {
      Contract.Requires(addressDereference != null);
      Contract.Ensures(Contract.Result<AddressDereference>() != null);

      var mutableCopy = this.shallowCopier.Copy(addressDereference);
      mutableCopy.Address = this.Copy(mutableCopy.Address);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given AddressOf expression.
    /// </summary>
    /// <param name="addressOf"></param>
    public AddressOf Copy(IAddressOf addressOf) {
      Contract.Requires(addressOf != null);
      Contract.Ensures(Contract.Result<AddressOf>() != null);

      var mutableCopy = this.shallowCopier.Copy(addressOf);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given anonymous delegate expression.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
    public AnonymousDelegate Copy(IAnonymousDelegate anonymousDelegate) {
      Contract.Requires(anonymousDelegate != null);
      Contract.Ensures(Contract.Result<AnonymousDelegate>() != null);

      var mutableCopy = this.shallowCopier.Copy(anonymousDelegate);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Parameters = this.Copy(mutableCopy.Parameters);
      mutableCopy.Body = this.Copy((BlockStatement)mutableCopy.Body);
      mutableCopy.ReturnType = this.Copy(mutableCopy.ReturnType);
      if (mutableCopy.ReturnValueIsModified)
        mutableCopy.ReturnValueCustomModifiers = this.Copy(mutableCopy.ReturnValueCustomModifiers);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given array indexer expression.
    /// </summary>
    /// <param name="arrayIndexer"></param>
    public ArrayIndexer Copy(IArrayIndexer arrayIndexer) {
      Contract.Requires(arrayIndexer != null);
      Contract.Ensures(Contract.Result<ArrayIndexer>() != null);

      var mutableCopy = this.shallowCopier.Copy(arrayIndexer);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.IndexedObject = this.Copy(mutableCopy.IndexedObject);
      mutableCopy.Indices = this.Copy(mutableCopy.Indices);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given assert statement.
    /// </summary>
    /// <param name="assertStatement"></param>
    public AssertStatement Copy(IAssertStatement assertStatement) {
      Contract.Requires(assertStatement != null);
      Contract.Ensures(Contract.Result<AssertStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(assertStatement);
      mutableCopy.Condition = this.Copy(mutableCopy.Condition);
      if (mutableCopy.Description != null)
        mutableCopy.Description = this.Copy(mutableCopy.Description);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given assignment expression.
    /// </summary>
    /// <param name="assignment"></param>
    public Assignment Copy(IAssignment assignment) {
      Contract.Requires(assignment != null);
      Contract.Ensures(Contract.Result<Assignment>() != null);

      var mutableCopy = this.shallowCopier.Copy(assignment);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Target = this.Copy(mutableCopy.Target);
      mutableCopy.Source = this.Copy(mutableCopy.Source);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given assume statement.
    /// </summary>
    /// <param name="assumeStatement"></param>
    public AssumeStatement Copy(IAssumeStatement assumeStatement) {
      Contract.Requires(assumeStatement != null);
      Contract.Ensures(Contract.Result<AssumeStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(assumeStatement);
      mutableCopy.Condition = this.Copy(mutableCopy.Condition);
      if (mutableCopy.Description != null)
        mutableCopy.Description = this.Copy(mutableCopy.Description);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given bitwise and expression.
    /// </summary>
    /// <param name="bitwiseAnd"></param>
    public BitwiseAnd Copy(IBitwiseAnd bitwiseAnd) {
      Contract.Requires(bitwiseAnd != null);
      Contract.Ensures(Contract.Result<BitwiseAnd>() != null);

      var mutableCopy = this.shallowCopier.Copy(bitwiseAnd);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given bitwise or expression.
    /// </summary>
    /// <param name="bitwiseOr"></param>
    public BitwiseOr Copy(IBitwiseOr bitwiseOr) {
      Contract.Requires(bitwiseOr != null);
      Contract.Ensures(Contract.Result<BitwiseOr>() != null);

      var mutableCopy = this.shallowCopier.Copy(bitwiseOr);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given block expression.
    /// </summary>
    /// <param name="blockExpression"></param>
    public BlockExpression Copy(IBlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);
      Contract.Ensures(Contract.Result<BlockExpression>() != null);

      var mutableCopy = this.shallowCopier.Copy(blockExpression);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.BlockStatement = this.Copy((BlockStatement)mutableCopy.BlockStatement);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given statement block.
    /// </summary>
    /// <param name="block"></param>
    public BlockStatement Copy(IBlockStatement block) {
      Contract.Requires(block != null);
      Contract.Ensures(Contract.Result<BlockStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(block);
      mutableCopy.Statements = this.Copy(mutableCopy.Statements);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given break statement.
    /// </summary>
    /// <param name="breakStatement"></param>
    public BreakStatement Copy(IBreakStatement breakStatement) {
      Contract.Requires(breakStatement != null);
      Contract.Ensures(Contract.Result<BreakStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(breakStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given bound expression.
    /// </summary>
    /// <param name="boundExpression"></param>
    public BoundExpression Copy(IBoundExpression boundExpression) {
      Contract.Requires(boundExpression != null);
      Contract.Ensures(Contract.Result<BoundExpression>() != null);

      var mutableCopy = this.shallowCopier.Copy(boundExpression);
      this.CopyChildren((Expression)mutableCopy);
      if (mutableCopy.Instance != null)
        mutableCopy.Instance = this.Copy(mutableCopy.Instance);
      var local = mutableCopy.Definition as ILocalDefinition;
      if (local != null)
        mutableCopy.Definition = this.GetExistingCopyIfInsideCone(local);
      else {
        var parameter = mutableCopy.Definition as IParameterDefinition;
        if (parameter != null)
          mutableCopy.Definition = this.GetExistingCopyIfInsideCone(parameter);
        else {
          var fieldReference = (IFieldReference)mutableCopy.Definition;
          mutableCopy.Definition = this.Copy(fieldReference);
        }
      }
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the cast-if-possible expression.
    /// </summary>
    /// <param name="castIfPossible"></param>
    public CastIfPossible Copy(ICastIfPossible castIfPossible) {
      Contract.Requires(castIfPossible != null);
      Contract.Ensures(Contract.Result<CastIfPossible>() != null);

      var mutableCopy = this.shallowCopier.Copy(castIfPossible);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.ValueToCast = this.Copy(mutableCopy.ValueToCast);
      mutableCopy.TargetType = this.Copy(mutableCopy.TargetType);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given catch clause.
    /// </summary>
    /// <param name="catchClause"></param>
    public CatchClause Copy(ICatchClause catchClause) {
      Contract.Requires(catchClause != null);
      Contract.Ensures(Contract.Result<CatchClause>() != null);

      var mutableCopy = this.shallowCopier.Copy(catchClause);
      mutableCopy.ExceptionType = this.Copy(mutableCopy.ExceptionType);
      if (!(mutableCopy.ExceptionContainer is Dummy)) {
        var copy = this.GetExistingCopyIfInsideCone(mutableCopy.ExceptionContainer); //allow catch clauses to share the same local
        if (copy == mutableCopy.ExceptionContainer) {
          mutableCopy.ExceptionContainer = this.Copy(mutableCopy.ExceptionContainer);
          this.LocalsInsideCone.Add(catchClause.ExceptionContainer, mutableCopy.ExceptionContainer);
        } else {
          mutableCopy.ExceptionContainer = copy;
        }
      }
      if (mutableCopy.FilterCondition != null)
        mutableCopy.FilterCondition = this.Copy(mutableCopy.FilterCondition);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given check-if-instance expression.
    /// </summary>
    /// <param name="checkIfInstance"></param>
    public CheckIfInstance Copy(ICheckIfInstance checkIfInstance) {
      Contract.Requires(checkIfInstance != null);
      Contract.Ensures(Contract.Result<CheckIfInstance>() != null);

      var mutableCopy = this.shallowCopier.Copy(checkIfInstance);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Operand = this.Copy(mutableCopy.Operand);
      mutableCopy.TypeToCheck = this.Copy(mutableCopy.TypeToCheck);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given compile time constant.
    /// </summary>
    /// <param name="constant"></param>
    public CompileTimeConstant Copy(ICompileTimeConstant constant) {
      Contract.Requires(constant != null);
      Contract.Ensures(Contract.Result<CompileTimeConstant>() != null);

      var mutableCopy = this.shallowCopier.Copy(constant);
      this.CopyChildren((Expression)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given conversion expression.
    /// </summary>
    /// <param name="conversion"></param>
    public Conversion Copy(IConversion conversion) {
      Contract.Requires(conversion != null);
      Contract.Ensures(Contract.Result<Conversion>() != null);

      var mutableCopy = this.shallowCopier.Copy(conversion);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.ValueToConvert = this.Copy(mutableCopy.ValueToConvert);
      mutableCopy.TypeAfterConversion = this.Copy(mutableCopy.TypeAfterConversion);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given conditional expression.
    /// </summary>
    /// <param name="conditional"></param>
    public Conditional Copy(IConditional conditional) {
      Contract.Requires(conditional != null);
      Contract.Ensures(Contract.Result<Conditional>() != null);

      var mutableCopy = this.shallowCopier.Copy(conditional);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Condition = this.Copy(mutableCopy.Condition);
      mutableCopy.ResultIfTrue = this.Copy(mutableCopy.ResultIfTrue);
      mutableCopy.ResultIfFalse = this.Copy(mutableCopy.ResultIfFalse);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given conditional statement.
    /// </summary>
    /// <param name="conditionalStatement"></param>
    public ConditionalStatement Copy(IConditionalStatement conditionalStatement) {
      Contract.Requires(conditionalStatement != null);
      Contract.Ensures(Contract.Result<ConditionalStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(conditionalStatement);
      mutableCopy.Condition = this.Copy(mutableCopy.Condition);
      mutableCopy.TrueBranch = this.Copy(mutableCopy.TrueBranch);
      mutableCopy.FalseBranch = this.Copy(mutableCopy.FalseBranch);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given continue statement.
    /// </summary>
    /// <param name="continueStatement"></param>
    public ContinueStatement Copy(IContinueStatement continueStatement) {
      Contract.Requires(continueStatement != null);
      Contract.Ensures(Contract.Result<ContinueStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(continueStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public CopyMemoryStatement Copy(ICopyMemoryStatement copyMemoryStatement) {
      Contract.Requires(copyMemoryStatement != null);
      Contract.Ensures(Contract.Result<CopyMemoryStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(copyMemoryStatement);
      mutableCopy.TargetAddress = this.Copy(mutableCopy.TargetAddress);
      mutableCopy.SourceAddress = this.Copy(mutableCopy.SourceAddress);
      mutableCopy.NumberOfBytesToCopy = this.Copy(mutableCopy.NumberOfBytesToCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
    public CreateArray Copy(ICreateArray createArray) {
      Contract.Requires(createArray != null);
      Contract.Ensures(Contract.Result<CreateArray>() != null);

      var mutableCopy = this.shallowCopier.Copy(createArray);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.ElementType = this.Copy(mutableCopy.ElementType);
      mutableCopy.Sizes = this.Copy(mutableCopy.Sizes);
      mutableCopy.Initializers = this.Copy(mutableCopy.Initializers);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given constructor call expression.
    /// </summary>
    /// <param name="createObjectInstance"></param>
    public CreateObjectInstance Copy(ICreateObjectInstance createObjectInstance) {
      Contract.Requires(createObjectInstance != null);
      Contract.Ensures(Contract.Result<CreateObjectInstance>() != null);

      var mutableCopy = this.shallowCopier.Copy(createObjectInstance);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.MethodToCall = this.Copy(mutableCopy.MethodToCall);
      mutableCopy.Arguments = this.Copy(mutableCopy.Arguments);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the anonymous object creation expression.
    /// </summary>
    /// <param name="createDelegateInstance"></param>
    public CreateDelegateInstance Copy(ICreateDelegateInstance createDelegateInstance) {
      Contract.Requires(createDelegateInstance != null);
      Contract.Ensures(Contract.Result<CreateDelegateInstance>() != null);

      var mutableCopy = this.shallowCopier.Copy(createDelegateInstance);
      this.CopyChildren((Expression)mutableCopy);
      if (mutableCopy.Instance != null)
        mutableCopy.Instance = this.Copy(mutableCopy.Instance);
      mutableCopy.MethodToCallViaDelegate = this.Copy(mutableCopy.MethodToCallViaDelegate);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement"></param>
    public DebuggerBreakStatement Copy(IDebuggerBreakStatement debuggerBreakStatement) {
      Contract.Requires(debuggerBreakStatement != null);
      Contract.Ensures(Contract.Result<DebuggerBreakStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(debuggerBreakStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given defalut value expression.
    /// </summary>
    /// <param name="defaultValue"></param>
    public DefaultValue Copy(IDefaultValue defaultValue) {
      Contract.Requires(defaultValue != null);
      Contract.Ensures(Contract.Result<DefaultValue>() != null);

      var mutableCopy = this.shallowCopier.Copy(defaultValue);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.DefaultValueType = this.Copy(mutableCopy.DefaultValueType);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given division expression.
    /// </summary>
    /// <param name="division"></param>
    public Division Copy(IDivision division) {
      Contract.Requires(division != null);
      Contract.Ensures(Contract.Result<Division>() != null);

      var mutableCopy = this.shallowCopier.Copy(division);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given do until statement.
    /// </summary>
    /// <param name="doUntilStatement"></param>
    public DoUntilStatement Copy(IDoUntilStatement doUntilStatement) {
      Contract.Requires(doUntilStatement != null);
      Contract.Ensures(Contract.Result<DoUntilStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(doUntilStatement);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      mutableCopy.Condition = this.Copy(mutableCopy.Condition);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given dup value expression.
    /// </summary>
    /// <param name="dupValue"></param>
    public DupValue Copy(IDupValue dupValue) {
      Contract.Requires(dupValue != null);
      Contract.Ensures(Contract.Result<DupValue>() != null);

      var mutableCopy = this.shallowCopier.Copy(dupValue);
      this.CopyChildren((Expression)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given empty statement.
    /// </summary>
    /// <param name="emptyStatement"></param>
    public EmptyStatement Copy(IEmptyStatement emptyStatement) {
      Contract.Requires(emptyStatement != null);
      Contract.Ensures(Contract.Result<EmptyStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(emptyStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given equality expression.
    /// </summary>
    /// <param name="equality"></param>
    public Equality Copy(IEquality equality) {
      Contract.Requires(equality != null);
      Contract.Ensures(Contract.Result<Equality>() != null);

      var mutableCopy = this.shallowCopier.Copy(equality);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given exclusive or expression.
    /// </summary>
    /// <param name="exclusiveOr"></param>
    public ExclusiveOr Copy(IExclusiveOr exclusiveOr) {
      Contract.Requires(exclusiveOr != null);
      Contract.Ensures(Contract.Result<ExclusiveOr>() != null);

      var mutableCopy = this.shallowCopier.Copy(exclusiveOr);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given expression.
    /// </summary>
    /// <param name="expression"></param>
    public Expression Copy(IExpression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<Expression>() != null);

      expression.Dispatch(this.Dispatcher);
      return (Expression)this.Dispatcher.result;
    }

    /// <summary>
    /// Returns a deep copy of the given expression statement.
    /// </summary>
    /// <param name="expressionStatement"></param>
    public ExpressionStatement Copy(IExpressionStatement expressionStatement) {
      Contract.Requires(expressionStatement != null);
      Contract.Ensures(Contract.Result<ExpressionStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(expressionStatement);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given fill memory block statement.
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    public FillMemoryStatement Copy(IFillMemoryStatement fillMemoryStatement) {
      Contract.Requires(fillMemoryStatement != null);
      Contract.Ensures(Contract.Result<FillMemoryStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(fillMemoryStatement);
      mutableCopy.TargetAddress = this.Copy(mutableCopy.TargetAddress);
      mutableCopy.FillValue = this.Copy(mutableCopy.FillValue);
      mutableCopy.NumberOfBytesToFill = this.Copy(mutableCopy.NumberOfBytesToFill);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given foreach statement.
    /// </summary>
    /// <param name="forEachStatement"></param>
    public ForEachStatement Copy(IForEachStatement forEachStatement) {
      Contract.Requires(forEachStatement != null);
      Contract.Ensures(Contract.Result<ForEachStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(forEachStatement);
      if (!(mutableCopy.Variable is Dummy)) {
        var copy = this.GetExistingCopyIfInsideCone(mutableCopy.Variable); // allow foreach loops to share the same local
        if (copy == mutableCopy.Variable) {
          mutableCopy.Variable = this.Copy(mutableCopy.Variable);
          this.LocalsInsideCone.Add(forEachStatement.Variable, mutableCopy.Variable);
        } else {
          mutableCopy.Variable = copy;
        }
      }
      mutableCopy.Collection = this.Copy(mutableCopy.Collection);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given for statement.
    /// </summary>
    /// <param name="forStatement"></param>
    public ForStatement Copy(IForStatement forStatement) {
      Contract.Requires(forStatement != null);
      Contract.Ensures(Contract.Result<ForStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(forStatement);
      mutableCopy.InitStatements = this.Copy(mutableCopy.InitStatements);
      mutableCopy.Condition = this.Copy(mutableCopy.Condition);
      mutableCopy.IncrementStatements = this.Copy(mutableCopy.IncrementStatements);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given get type of typed reference expression.
    /// </summary>
    /// <param name="getTypeOfTypedReference"></param>
    public GetTypeOfTypedReference Copy(IGetTypeOfTypedReference getTypeOfTypedReference) {
      Contract.Requires(getTypeOfTypedReference != null);
      Contract.Ensures(Contract.Result<GetTypeOfTypedReference>() != null);

      var mutableCopy = this.shallowCopier.Copy(getTypeOfTypedReference);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.TypedReference = this.Copy(mutableCopy.TypedReference);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given get value of typed reference expression.
    /// </summary>
    /// <param name="getValueOfTypedReference"></param>
    public GetValueOfTypedReference Copy(IGetValueOfTypedReference getValueOfTypedReference) {
      Contract.Requires(getValueOfTypedReference != null);
      Contract.Ensures(Contract.Result<GetValueOfTypedReference>() != null);

      var mutableCopy = this.shallowCopier.Copy(getValueOfTypedReference);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.TypedReference = this.Copy(mutableCopy.TypedReference);
      mutableCopy.TargetType = this.Copy(mutableCopy.TargetType);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given goto statement.
    /// </summary>
    /// <param name="gotoStatement"></param>
    public GotoStatement Copy(IGotoStatement gotoStatement) {
      Contract.Requires(gotoStatement != null);
      Contract.Ensures(Contract.Result<GotoStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(gotoStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public GotoSwitchCaseStatement Copy(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      Contract.Requires(gotoSwitchCaseStatement != null);
      Contract.Ensures(Contract.Result<GotoSwitchCaseStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(gotoSwitchCaseStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given greater-than expression.
    /// </summary>
    /// <param name="greaterThan"></param>
    public GreaterThan Copy(IGreaterThan greaterThan) {
      Contract.Requires(greaterThan != null);
      Contract.Ensures(Contract.Result<GreaterThan>() != null);

      var mutableCopy = this.shallowCopier.Copy(greaterThan);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given greater-than-or-equal expression.
    /// </summary>
    /// <param name="greaterThanOrEqual"></param>
    public GreaterThanOrEqual Copy(IGreaterThanOrEqual greaterThanOrEqual) {
      Contract.Requires(greaterThanOrEqual != null);
      Contract.Ensures(Contract.Result<GreaterThanOrEqual>() != null);

      var mutableCopy = this.shallowCopier.Copy(greaterThanOrEqual);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given labeled statement.
    /// </summary>
    /// <param name="labeledStatement"></param>
    public LabeledStatement Copy(ILabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);
      Contract.Ensures(Contract.Result<LabeledStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(labeledStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given left shift expression.
    /// </summary>
    /// <param name="leftShift"></param>
    public LeftShift Copy(ILeftShift leftShift) {
      Contract.Requires(leftShift != null);
      Contract.Ensures(Contract.Result<LeftShift>() != null);

      var mutableCopy = this.shallowCopier.Copy(leftShift);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given less-than expression.
    /// </summary>
    /// <param name="lessThan"></param>
    public LessThan Copy(ILessThan lessThan) {
      Contract.Requires(lessThan != null);
      Contract.Ensures(Contract.Result<LessThan>() != null);

      var mutableCopy = this.shallowCopier.Copy(lessThan);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given less-than-or-equal expression.
    /// </summary>
    /// <param name="lessThanOrEqual"></param>
    public LessThanOrEqual Copy(ILessThanOrEqual lessThanOrEqual) {
      Contract.Requires(lessThanOrEqual != null);
      Contract.Ensures(Contract.Result<LessThanOrEqual>() != null);

      var mutableCopy = this.shallowCopier.Copy(lessThanOrEqual);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement"></param>
    public LocalDeclarationStatement Copy(ILocalDeclarationStatement localDeclarationStatement) {
      Contract.Requires(localDeclarationStatement != null);
      Contract.Ensures(Contract.Result<LocalDeclarationStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(localDeclarationStatement);
      if (!this.LocalsInsideCone.ContainsKey(mutableCopy.LocalVariable)) { //work around bug in decompiler, for now
        mutableCopy.LocalVariable = this.Copy(mutableCopy.LocalVariable);
        this.LocalsInsideCone.Add(localDeclarationStatement.LocalVariable, mutableCopy.LocalVariable);
      }
      if (mutableCopy.InitialValue != null)
        mutableCopy.InitialValue = this.Copy(mutableCopy.InitialValue);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given lock statement.
    /// </summary>
    /// <param name="lockStatement"></param>
    public LockStatement Copy(ILockStatement lockStatement) {
      Contract.Requires(lockStatement != null);
      Contract.Ensures(Contract.Result<LockStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(lockStatement);
      mutableCopy.Guard = this.Copy(mutableCopy.Guard);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given logical not expression.
    /// </summary>
    /// <param name="logicalNot"></param>
    public LogicalNot Copy(ILogicalNot logicalNot) {
      Contract.Requires(logicalNot != null);
      Contract.Ensures(Contract.Result<LogicalNot>() != null);

      var mutableCopy = this.shallowCopier.Copy(logicalNot);
      this.CopyChildren((UnaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given make typed reference expression.
    /// </summary>
    /// <param name="makeTypedReference"></param>
    public MakeTypedReference Copy(IMakeTypedReference makeTypedReference) {
      Contract.Requires(makeTypedReference != null);
      Contract.Ensures(Contract.Result<MakeTypedReference>() != null);

      var mutableCopy = this.shallowCopier.Copy(makeTypedReference);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Operand = this.Copy(mutableCopy.Operand);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given method call.
    /// </summary>
    /// <param name="methodCall"></param>
    public MethodCall Copy(IMethodCall methodCall) {
      Contract.Requires(methodCall != null);
      Contract.Ensures(Contract.Result<MethodCall>() != null);

      var mutableCopy = this.shallowCopier.Copy(methodCall);
      this.CopyChildren((Expression)mutableCopy);
      if (!mutableCopy.IsStaticCall && !mutableCopy.IsJumpCall)
        mutableCopy.ThisArgument = this.Copy(mutableCopy.ThisArgument);
      mutableCopy.MethodToCall = this.Copy(mutableCopy.MethodToCall);
      mutableCopy.Arguments = this.Copy(mutableCopy.Arguments);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given modulus expression.
    /// </summary>
    /// <param name="modulus"></param>
    public Modulus Copy(IModulus modulus) {
      Contract.Requires(modulus != null);
      Contract.Ensures(Contract.Result<Modulus>() != null);

      var mutableCopy = this.shallowCopier.Copy(modulus);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given multiplication expression.
    /// </summary>
    /// <param name="multiplication"></param>
    public Multiplication Copy(IMultiplication multiplication) {
      Contract.Requires(multiplication != null);
      Contract.Ensures(Contract.Result<Multiplication>() != null);

      var mutableCopy = this.shallowCopier.Copy(multiplication);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
    public NamedArgument Copy(INamedArgument namedArgument) {
      Contract.Requires(namedArgument != null);
      Contract.Ensures(Contract.Result<NamedArgument>() != null);

      var mutableCopy = this.shallowCopier.Copy(namedArgument);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.ArgumentValue = this.Copy(mutableCopy.ArgumentValue);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given not equality expression.
    /// </summary>
    /// <param name="notEquality"></param>
    public NotEquality Copy(INotEquality notEquality) {
      Contract.Requires(notEquality != null);
      Contract.Ensures(Contract.Result<NotEquality>() != null);

      var mutableCopy = this.shallowCopier.Copy(notEquality);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given old value expression.
    /// </summary>
    /// <param name="oldValue"></param>
    public OldValue Copy(IOldValue oldValue) {
      Contract.Requires(oldValue != null);
      Contract.Ensures(Contract.Result<OldValue>() != null);

      var mutableCopy = this.shallowCopier.Copy(oldValue);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given one's complement expression.
    /// </summary>
    /// <param name="onesComplement"></param>
    public OnesComplement Copy(IOnesComplement onesComplement) {
      Contract.Requires(onesComplement != null);
      Contract.Ensures(Contract.Result<OnesComplement>() != null);

      var mutableCopy = this.shallowCopier.Copy(onesComplement);
      this.CopyChildren((UnaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given out argument expression.
    /// </summary>
    /// <param name="outArgument"></param>
    public OutArgument Copy(IOutArgument outArgument) {
      Contract.Requires(outArgument != null);
      Contract.Ensures(Contract.Result<OutArgument>() != null);

      var mutableCopy = this.shallowCopier.Copy(outArgument);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given pointer call.
    /// </summary>
    /// <param name="pointerCall"></param>
    public PointerCall Copy(IPointerCall pointerCall) {
      Contract.Requires(pointerCall != null);
      Contract.Ensures(Contract.Result<PointerCall>() != null);

      var mutableCopy = this.shallowCopier.Copy(pointerCall);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Pointer = this.Copy(mutableCopy.Pointer);
      mutableCopy.Arguments = this.Copy(mutableCopy.Arguments);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public PopValue Copy(IPopValue popValue) {
      Contract.Requires(popValue != null);
      Contract.Ensures(Contract.Result<PopValue>() != null);

      var mutableCopy = this.shallowCopier.Copy(popValue);
      this.CopyChildren((Expression)popValue);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    public PushStatement Copy(IPushStatement pushStatement) {
      Contract.Requires(pushStatement != null);
      Contract.Ensures(Contract.Result<PushStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(pushStatement);
      mutableCopy.ValueToPush = this.Copy(mutableCopy.ValueToPush);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given ref argument expression.
    /// </summary>
    /// <param name="refArgument"></param>
    public RefArgument Copy(IRefArgument refArgument) {
      Contract.Requires(refArgument != null);
      Contract.Ensures(Contract.Result<RefArgument>() != null);

      var mutableCopy = this.shallowCopier.Copy(refArgument);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given resource usage statement.
    /// </summary>
    /// <param name="resourceUseStatement"></param>
    public ResourceUseStatement Copy(IResourceUseStatement resourceUseStatement) {
      Contract.Requires(resourceUseStatement != null);
      Contract.Ensures(Contract.Result<ResourceUseStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(resourceUseStatement);
      mutableCopy.ResourceAcquisitions = this.Copy(mutableCopy.ResourceAcquisitions);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement"></param>
    public RethrowStatement Copy(IRethrowStatement rethrowStatement) {
      Contract.Requires(rethrowStatement != null);
      Contract.Ensures(Contract.Result<RethrowStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(rethrowStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the return statement.
    /// </summary>
    /// <param name="returnStatement"></param>
    public ReturnStatement Copy(IReturnStatement returnStatement) {
      Contract.Requires(returnStatement != null);
      Contract.Ensures(Contract.Result<ReturnStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(returnStatement);
      if (mutableCopy.Expression != null)
        mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given return value expression.
    /// </summary>
    /// <param name="returnValue"></param>
    public ReturnValue Copy(IReturnValue returnValue) {
      Contract.Requires(returnValue != null);
      Contract.Ensures(Contract.Result<ReturnValue>() != null);

      var mutableCopy = this.shallowCopier.Copy(returnValue);
      this.CopyChildren((Expression)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given right shift expression.
    /// </summary>
    /// <param name="rightShift"></param>
    public RightShift Copy(IRightShift rightShift) {
      Contract.Requires(rightShift != null);
      Contract.Ensures(Contract.Result<RightShift>() != null);

      var mutableCopy = this.shallowCopier.Copy(rightShift);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given stack array create expression.
    /// </summary>
    /// <param name="stackArrayCreate"></param>
    public StackArrayCreate Copy(IStackArrayCreate stackArrayCreate) {
      Contract.Requires(stackArrayCreate != null);
      Contract.Ensures(Contract.Result<StackArrayCreate>() != null);

      var mutableCopy = this.shallowCopier.Copy(stackArrayCreate);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.ElementType = this.Copy(mutableCopy.ElementType);
      mutableCopy.Size = this.Copy(mutableCopy.Size);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression"></param>
    public RuntimeArgumentHandleExpression Copy(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      Contract.Requires(runtimeArgumentHandleExpression != null);
      Contract.Ensures(Contract.Result<RuntimeArgumentHandleExpression>() != null);

      var mutableCopy = this.shallowCopier.Copy(runtimeArgumentHandleExpression);
      this.CopyChildren((Expression)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given sizeof() expression.
    /// </summary>
    /// <param name="sizeOf"></param>
    public SizeOf Copy(ISizeOf sizeOf) {
      Contract.Requires(sizeOf != null);
      Contract.Ensures(Contract.Result<SizeOf>() != null);

      var mutableCopy = this.shallowCopier.Copy(sizeOf);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.TypeToSize = this.Copy(mutableCopy.TypeToSize);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given source method body.
    /// </summary>
    /// <param name="sourceMethodBody"></param>
    public SourceMethodBody Copy(ISourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      Contract.Ensures(Contract.Result<SourceMethodBody>() != null);

      return this.Copy(sourceMethodBody, sourceMethodBody.MethodDefinition);
    }

    private SourceMethodBody Copy(ISourceMethodBody sourceMethodBody, IMethodDefinition method) {
      var mutableCopy = this.shallowCopier.Copy(sourceMethodBody);
      mutableCopy.Block = this.Copy(mutableCopy.Block);
      mutableCopy.MethodDefinition = method;
      return mutableCopy;
    }

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public Statement Copy(IStatement statement) {
      Contract.Requires(statement != null);
      Contract.Ensures(Contract.Result<Statement>() != null);

      statement.Dispatch(this.Dispatcher);
      return (Statement)this.Dispatcher.result;
    }

    /// <summary>
    /// Returns a deep copy of the given subtraction expression.
    /// </summary>
    /// <param name="subtraction"></param>
    public Subtraction Copy(ISubtraction subtraction) {
      Contract.Requires(subtraction != null);
      Contract.Ensures(Contract.Result<Subtraction>() != null);

      var mutableCopy = this.shallowCopier.Copy(subtraction);
      this.CopyChildren((BinaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given switch case.
    /// </summary>
    /// <param name="switchCase"></param>
    public SwitchCase Copy(ISwitchCase switchCase) {
      Contract.Requires(switchCase != null);
      Contract.Ensures(Contract.Result<SwitchCase>() != null);

      var mutableCopy = this.shallowCopier.Copy(switchCase);
      if (!mutableCopy.IsDefault)
        mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given switch statement.
    /// </summary>
    /// <param name="switchStatement"></param>
    public SwitchStatement Copy(ISwitchStatement switchStatement) {
      Contract.Requires(switchStatement != null);
      Contract.Ensures(Contract.Result<SwitchStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(switchStatement);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      mutableCopy.Cases = this.Copy(mutableCopy.Cases);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given target expression.
    /// </summary>
    /// <param name="targetExpression"></param>
    public TargetExpression Copy(ITargetExpression targetExpression) {
      Contract.Requires(targetExpression != null);
      Contract.Ensures(Contract.Result<TargetExpression>() != null);

      var mutableCopy = this.shallowCopier.Copy(targetExpression);
      this.CopyChildren((Expression)mutableCopy);
      if (mutableCopy.Instance != null) {
        mutableCopy.Instance = this.Copy(mutableCopy.Instance);
      }
      var local = mutableCopy.Definition as ILocalDefinition;
      if (local != null)
        mutableCopy.Definition = this.GetExistingCopyIfInsideCone(local);
      else {
        var parameter = mutableCopy.Definition as IParameterDefinition;
        if (parameter != null)
          mutableCopy.Definition = this.GetExistingCopyIfInsideCone(parameter);
        else {
          var fieldReference = mutableCopy.Definition as IFieldReference;
          if (fieldReference != null)
            mutableCopy.Definition = this.Copy(fieldReference);
          else {
            var arrayIndexer = mutableCopy.Definition as IArrayIndexer;
            if (arrayIndexer != null)
              mutableCopy.Definition = this.Copy(arrayIndexer);
            else {
              var addressDereference = mutableCopy.Definition as IAddressDereference;
              if (addressDereference != null)
                mutableCopy.Definition = this.Copy(addressDereference);
              else {
                var propertyDefinition = mutableCopy.Definition as IPropertyDefinition;
                if (propertyDefinition != null)
                  mutableCopy.Definition = this.GetExistingCopyIfInsideCone(propertyDefinition);
                else
                  mutableCopy.Definition = this.Copy((IThisReference)mutableCopy.Definition);
              }
            }
          }
        }
      }
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given this reference expression.
    /// </summary>
    /// <param name="thisReference"></param>
    public ThisReference Copy(IThisReference thisReference) {
      Contract.Requires(thisReference != null);
      Contract.Ensures(Contract.Result<ThisReference>() != null);

      var mutableCopy = this.shallowCopier.Copy(thisReference);
      this.CopyChildren((Expression)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the throw statement.
    /// </summary>
    /// <param name="throwStatement"></param>
    public ThrowStatement Copy(IThrowStatement throwStatement) {
      Contract.Requires(throwStatement != null);
      Contract.Ensures(Contract.Result<ThrowStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(throwStatement);
      mutableCopy.Exception = this.Copy(mutableCopy.Exception);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the try-catch-filter-finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement"></param>
    public TryCatchFinallyStatement Copy(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      Contract.Requires(tryCatchFilterFinallyStatement != null);
      Contract.Ensures(Contract.Result<TryCatchFinallyStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(tryCatchFilterFinallyStatement);
      mutableCopy.TryBody = this.Copy(mutableCopy.TryBody);
      mutableCopy.CatchClauses = this.Copy(mutableCopy.CatchClauses);
      if (mutableCopy.FaultBody != null)
        mutableCopy.FaultBody = this.Copy(mutableCopy.FaultBody);
      if (mutableCopy.FinallyBody != null)
        mutableCopy.FinallyBody = this.Copy(mutableCopy.FinallyBody);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given tokenof() expression.
    /// </summary>
    /// <param name="tokenOf"></param>
    public TokenOf Copy(ITokenOf tokenOf) {
      Contract.Requires(tokenOf != null);
      Contract.Ensures(Contract.Result<TokenOf>() != null);

      var mutableCopy = this.shallowCopier.Copy(tokenOf);
      this.CopyChildren((Expression)mutableCopy);
      var fieldReference = mutableCopy.Definition as IFieldReference;
      if (fieldReference != null)
        mutableCopy.Definition = this.Copy(fieldReference);
      else {
        var methodReference = mutableCopy.Definition as IMethodReference;
        if (methodReference != null)
          mutableCopy.Definition = this.Copy(methodReference);
        else {
          var typeReference = (ITypeReference)mutableCopy.Definition;
          mutableCopy.Definition = this.Copy(typeReference);
        }
      }
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given typeof() expression.
    /// </summary>
    /// <param name="typeOf"></param>
    public TypeOf Copy(ITypeOf typeOf) {
      Contract.Requires(typeOf != null);
      Contract.Ensures(Contract.Result<TypeOf>() != null);

      var mutableCopy = this.shallowCopier.Copy(typeOf);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.TypeToGet = this.Copy(mutableCopy.TypeToGet);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given unary negation expression.
    /// </summary>
    /// <param name="unaryNegation"></param>
    public UnaryNegation Copy(IUnaryNegation unaryNegation) {
      Contract.Requires(unaryNegation != null);
      Contract.Ensures(Contract.Result<UnaryNegation>() != null);

      var mutableCopy = this.shallowCopier.Copy(unaryNegation);
      this.CopyChildren((UnaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given unary plus expression.
    /// </summary>
    /// <param name="unaryPlus"></param>
    public UnaryPlus Copy(IUnaryPlus unaryPlus) {
      Contract.Requires(unaryPlus != null);
      Contract.Ensures(Contract.Result<UnaryPlus>() != null);

      var mutableCopy = this.shallowCopier.Copy(unaryPlus);
      this.CopyChildren((UnaryOperation)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given vector length expression.
    /// </summary>
    /// <param name="vectorLength"></param>
    public VectorLength Copy(IVectorLength vectorLength) {
      Contract.Requires(vectorLength != null);
      Contract.Ensures(Contract.Result<VectorLength>() != null);

      var mutableCopy = this.shallowCopier.Copy(vectorLength);
      this.CopyChildren((Expression)mutableCopy);
      mutableCopy.Vector = this.Copy(mutableCopy.Vector);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given while do statement.
    /// </summary>
    /// <param name="whileDoStatement"></param>
    public WhileDoStatement Copy(IWhileDoStatement whileDoStatement) {
      Contract.Requires(whileDoStatement != null);
      Contract.Ensures(Contract.Result<WhileDoStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(whileDoStatement);
      mutableCopy.Condition = this.Copy(mutableCopy.Condition);
      mutableCopy.Body = this.Copy(mutableCopy.Body);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public YieldBreakStatement Copy(IYieldBreakStatement yieldBreakStatement) {
      Contract.Requires(yieldBreakStatement != null);
      Contract.Ensures(Contract.Result<YieldBreakStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(yieldBreakStatement);
      return mutableCopy;
    }

    /// <summary>
    /// Returns a deep copy of the given yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement"></param>
    public YieldReturnStatement Copy(IYieldReturnStatement yieldReturnStatement) {
      Contract.Requires(yieldReturnStatement != null);
      Contract.Ensures(Contract.Result<YieldReturnStatement>() != null);

      var mutableCopy = this.shallowCopier.Copy(yieldReturnStatement);
      mutableCopy.Expression = this.Copy(mutableCopy.Expression);
      return mutableCopy;
    }


    /// <summary>
    /// Returns a deep copy the given list of catch clauses.
    /// </summary>
    /// <param name="catchClauses"></param>
    public virtual List<ICatchClause>/*?*/ Copy(List<ICatchClause>/*?*/ catchClauses) {
      if (catchClauses == null) return null;
      for (int i = 0, n = catchClauses.Count; i < n; i++)
        catchClauses[i] = this.Copy(catchClauses[i]);
      return catchClauses;
    }

    /// <summary>
    /// Returns a deep copy the given list of expressions.
    /// </summary>
    /// <param name="expressions"></param>
    public virtual List<IExpression>/*?*/ Copy(List<IExpression>/*?*/ expressions) {
      if (expressions == null) return null;
      for (int i = 0, n = expressions.Count; i < n; i++)
        expressions[i] = this.Copy(expressions[i]);
      return expressions;
    }

    /// <summary>
    /// Returns a deep copy the given list of switch cases.
    /// </summary>
    /// <param name="switchCases"></param>
    public virtual List<ISwitchCase>/*?*/ Copy(List<ISwitchCase>/*?*/ switchCases) {
      if (switchCases == null) return null;
      for (int i = 0, n = switchCases.Count; i < n; i++)
        switchCases[i] = this.Copy(switchCases[i]);
      return switchCases;
    }

    /// <summary>
    /// Returns a deep copy the given enumeration of statements.
    /// </summary>
    /// <param name="statements"></param>
    public virtual List<IStatement>/*?*/ Copy(List<IStatement>/*?*/ statements) {
      if (statements == null) return null;
      for (int i = 0, n = statements.Count; i < n; i++)
        statements[i] = this.Copy(statements[i]);
      return statements;
    }

    /// <summary>
    /// Returns a deep copy of the given list of parameters.
    /// </summary>
    public virtual List<IParameterDefinition>/*?*/ Copy(List<IParameterDefinition>/*?*/ parameters) {
      if (parameters == null) return null;
      for (int i = 0, n = parameters.Count; i < n; i++)
        parameters[i] = this.Copy(parameters[i]);
      return parameters;
    }

    /// <summary>
    /// Returns a deep copy of the given list of custom modifiers.
    /// </summary>
    public virtual List<ICustomModifier>/*?*/ Copy(List<ICustomModifier>/*?*/ customModifiers) {
      if (customModifiers == null) return null;
      for (int i = 0, n = customModifiers.Count; i < n; i++)
        customModifiers[i] = this.Copy(customModifiers[i]);
      return customModifiers;
    }

    private void CopyChildren(BinaryOperation binaryOperation) {
      this.CopyChildren((Expression)binaryOperation);
      binaryOperation.LeftOperand = this.Copy(binaryOperation.LeftOperand);
      binaryOperation.RightOperand = this.Copy(binaryOperation.RightOperand);
    }

    private void CopyChildren(Expression expression) {
      expression.Type = this.Copy(expression.Type);
      Contract.Assume(expression.Type is Microsoft.Cci.MutableCodeModel.NamedTypeDefinition || expression.Type is Microsoft.Cci.MutableCodeModel.TypeReference);
    }

    private void CopyChildren(UnaryOperation unaryOperation) {
      this.CopyChildren((Expression)unaryOperation);
      unaryOperation.Operand = this.Copy(unaryOperation.Operand);
    }

    /// <summary>
    /// Returns a deep copy of the given method body.
    /// </summary>
    protected override IMethodBody CopyMethodBody(IMethodBody methodBody, IMethodDefinition method) {
      var sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null) return this.Copy(sourceMethodBody, method);
      return base.CopyMethodBody(methodBody, method);
    }

    /// <summary>
    /// If the local is declared inside the subtree being copied by this copier, then return the copy. Otherwise return the local itself. No copies are made by this method.
    /// </summary>
    /// <param name="local">A local that is referenced from an expression.</param>
    /// <returns></returns>
    public ILocalDefinition GetExistingCopyIfInsideCone(ILocalDefinition local) {
      ILocalDefinition copy;
      if (this.LocalsInsideCone.TryGetValue(local, out copy)) return copy;
      return local; //The local is declared in a scope that encloses the cone being copied.
    }

  }

  /// <summary>
  /// Provides copy of a method body, a statement, or an expression, in which the references to the nodes
  /// inside a cone is replaced. The cone is defined using the parent class. 
  /// </summary>
  [Obsolete("Please use CodeDeepCopier or CodeShallowCopier")]
  public class CodeCopier : MetadataCopier {
    /// <summary>
    /// Provides copy of a method body, a statement, or an expression, in which the references to the nodes
    /// inside a cone is replaced. The cone is defined using the parent class. 
    /// </summary>
    public CodeCopier(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host) {
      this.sourceLocationProvider = sourceLocationProvider;
      this.createMutableType = new CreateMutableType(this);
    }
    /// <summary>
    /// Provides copy of a method body, a statement, or an expression, in which the references to the nodes
    /// inside a cone is replaced. The cone is defined using the parent class. 
    /// </summary>
    public CodeCopier(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IDefinition rootOfCone, out List<INamedTypeDefinition> newTypes)
      : base(host, rootOfCone, out newTypes) {
      this.sourceLocationProvider = sourceLocationProvider;
      this.createMutableType = new CreateMutableType(this);
    }

    private CreateMutableType createMutableType;

    /// <summary>
    /// An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.
    /// </summary>
    protected readonly ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// Do not copy private 
    /// </summary>
    /// <param name="typeDefinitions"></param>
    protected override void VisitPrivateHelperMembers(List<INamedTypeDefinition> typeDefinitions) {
      return;
    }

    /// <summary>
    /// A dispatcher method that calls the type-specific Substitute for <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public virtual IExpression Substitute(IExpression expression) {
      expression.Dispatch(this.createMutableType);
      return this.createMutableType.resultExpression;
    }

    /// <summary>
    /// A dispatcher method that calls the type-specific Substitute for <paramref name="statement"/>
    /// </summary>
    /// <param name="statement"></param>
    /// <returns></returns>
    public virtual IStatement Substitute(IStatement statement) {
      statement.Dispatch(this.createMutableType);
      return this.createMutableType.resultStatement;
    }

    /// <summary>
    /// Override the parent Substitute so that when we see a source method body, we will visit its statements.
    /// </summary>
    /// <param name="methodBody"></param>
    /// <returns></returns>
    public override IMethodBody Substitute(IMethodBody methodBody) {
      //^ requires (methodBody is ISourceMethodBody ==> this.cache.ContainsKeymethodBody.MethodDefinition));
      ISourceMethodBody sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null) {
        SourceMethodBody mutableSourceMethodBody = new SourceMethodBody(this.host, this.sourceLocationProvider);
        mutableSourceMethodBody.Block = (IBlockStatement)this.Substitute(sourceMethodBody.Block);
        mutableSourceMethodBody.MethodDefinition = (IMethodDefinition)this.cache[methodBody.MethodDefinition];
        mutableSourceMethodBody.LocalsAreZeroed = methodBody.LocalsAreZeroed;
        return mutableSourceMethodBody;
      }
      return base.Substitute(methodBody);
    }

    /// <summary>
    /// Substitute a list of statements. 
    /// </summary>
    /// <param name="statements"></param>
    /// <returns></returns>
    public virtual List<IStatement> Substitute(List<IStatement> statements) {
      var result = new List<IStatement>(statements);
      return this.DeepCopy(result);
    }

    #region DeepCopy methods

    /// <summary>
    /// Visit a list of statements. 
    /// </summary>
    /// <param name="statements"></param>
    /// <returns></returns>
    protected virtual List<IStatement> DeepCopy(List<IStatement> statements) {
      for (int i = 0, n = statements.Count; i < n; i++) {
        statements[i] = this.Substitute(statements[i]);
      }
      return statements;
    }

    /// <summary>
    /// Visits the specified addition.
    /// </summary>
    /// <param name="addition">The addition.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Addition addition) {
      return this.DeepCopy((BinaryOperation)addition);
    }

    /// <summary>
    /// Visits the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    /// <returns></returns>
    protected virtual IAddressableExpression DeepCopy(AddressableExpression addressableExpression) {
      object def = addressableExpression.Definition;
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
            addressableExpression.Definition = this.Substitute(field);
          else {
            IArrayIndexer/*?*/ indexer = def as IArrayIndexer;
            if (indexer != null)
              addressableExpression.Definition = this.Substitute(indexer);
            else {
              IAddressDereference/*?*/ adr = def as IAddressDereference;
              if (adr != null)
                addressableExpression.Definition = this.Substitute(adr);
              else {
                IMethodReference/*?*/ meth = def as IMethodReference;
                if (meth != null)
                  addressableExpression.Definition = this.Substitute(meth);
                else {
                  IThisReference thisRef = (IThisReference)def;
                  addressableExpression.Definition = this.Substitute(thisRef);
                }
              }
            }
          }
        }
      }
      if (addressableExpression.Instance != null)
        addressableExpression.Instance = this.Substitute(addressableExpression.Instance);
      addressableExpression.Type = this.Substitute(addressableExpression.Type);
      return addressableExpression;
    }

    /// <summary>
    /// Visits the specified address dereference.
    /// </summary>
    /// <param name="addressDereference">The address dereference.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(AddressDereference addressDereference) {
      addressDereference.Address = this.Substitute(addressDereference.Address);
      addressDereference.Type = this.Substitute(addressDereference.Type);
      return addressDereference;
    }

    /// <summary>
    /// Visits the specified address of.
    /// </summary>
    /// <param name="addressOf">The address of.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(AddressOf addressOf) {
      addressOf.Expression = (IAddressableExpression)this.Substitute(addressOf.Expression);
      addressOf.Type = this.Substitute(addressOf.Type);
      return addressOf;
    }

    /// <summary>
    /// Visits the specified anonymous delegate.
    /// </summary>
    /// <param name="anonymousDelegate">The anonymous delegate.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(AnonymousDelegate anonymousDelegate) {
      //^ requires this.cache.ContainsKey(anonymousDelegate);
      var pars = new List<IParameterDefinition>();
      foreach (var p in anonymousDelegate.Parameters) {
        var newp = this.DeepCopy(this.GetMutableCopyParamAnonymDeleg(p));
        pars.Add(newp);
      }
      anonymousDelegate.Parameters = pars;
      anonymousDelegate.Body = (IBlockStatement)this.Substitute(anonymousDelegate.Body);
      anonymousDelegate.ReturnType = this.Substitute(anonymousDelegate.ReturnType);
      anonymousDelegate.Type = this.Substitute(anonymousDelegate.Type);
      return anonymousDelegate;
    }

    /// <summary>
    /// Get mutable copy of a parameter definition of an anonymous delegate. The parameters of anonymous delegate
    /// are not visited until the code of a souce method body is visited.
    /// </summary>
    /// <param name="parameterDefinition"></param>
    /// <returns></returns>
    private ParameterDefinition GetMutableCopyParamAnonymDeleg(IParameterDefinition parameterDefinition) {
      ParameterDefinition/*?*/ result;
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(parameterDefinition, out cachedValue);
      result = cachedValue as ParameterDefinition;
      if (result != null) return result;
      result = new ParameterDefinition();
      this.cache.Add(parameterDefinition, result);
      this.cache.Add(result, result);
      result.Copy(parameterDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// Visits the specified array indexer.
    /// </summary>
    /// <param name="arrayIndexer">The array indexer.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(ArrayIndexer arrayIndexer) {
      arrayIndexer.IndexedObject = this.Substitute(arrayIndexer.IndexedObject);
      arrayIndexer.Indices = this.DeepCopy(arrayIndexer.Indices);
      arrayIndexer.Type = this.Substitute(arrayIndexer.Type);
      return arrayIndexer;
    }

    /// <summary>
    /// Visits the specified assert statement.
    /// </summary>
    /// <param name="assertStatement">The assert statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(AssertStatement assertStatement) {
      assertStatement.Condition = this.Substitute(assertStatement.Condition);
      return assertStatement;
    }

    /// <summary>
    /// Visits the specified assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Assignment assignment) {
      assignment.Target = (ITargetExpression)this.Substitute(assignment.Target);
      assignment.Source = this.Substitute(assignment.Source);
      assignment.Type = this.Substitute(assignment.Type);
      return assignment;
    }

    /// <summary>
    /// Visits the specified assume statement.
    /// </summary>
    /// <param name="assumeStatement">The assume statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(AssumeStatement assumeStatement) {
      assumeStatement.Condition = this.Substitute(assumeStatement.Condition);
      return assumeStatement;
    }

    /// <summary>
    /// Visits the specified bitwise and.
    /// </summary>
    /// <param name="bitwiseAnd">The bitwise and.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(BitwiseAnd bitwiseAnd) {
      return this.DeepCopy((BinaryOperation)bitwiseAnd);
    }

    /// <summary>
    /// Visits the specified bitwise or.
    /// </summary>
    /// <param name="bitwiseOr">The bitwise or.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(BitwiseOr bitwiseOr) {
      return this.DeepCopy((BinaryOperation)bitwiseOr);
    }

    /// <summary>
    /// Visits the specified binary operation.
    /// </summary>
    /// <param name="binaryOperation">The binary operation.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(BinaryOperation binaryOperation) {
      binaryOperation.LeftOperand = this.Substitute(binaryOperation.LeftOperand);
      binaryOperation.RightOperand = this.Substitute(binaryOperation.RightOperand);
      binaryOperation.Type = this.Substitute(binaryOperation.Type);
      return binaryOperation;
    }

    /// <summary>
    /// Visits the specified block expression.
    /// </summary>
    /// <param name="blockExpression">The block expression.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(BlockExpression blockExpression) {
      blockExpression.BlockStatement = (IBlockStatement)this.Substitute(blockExpression.BlockStatement);
      blockExpression.Expression = Substitute(blockExpression.Expression);
      blockExpression.Type = this.Substitute(blockExpression.Type);
      return blockExpression;
    }

    /// <summary>
    /// Visits the specified block statement.
    /// </summary>
    /// <param name="blockStatement">The block statement.</param>
    /// <returns></returns>
    protected virtual IBlockStatement DeepCopy(BlockStatement blockStatement) {
      blockStatement.Statements = Substitute(blockStatement.Statements);
      return blockStatement;
    }

    /// <summary>
    /// Visits the specified bound expression.
    /// </summary>
    /// <param name="boundExpression">The bound expression.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(BoundExpression boundExpression) {
      if (boundExpression.Instance != null)
        boundExpression.Instance = Substitute(boundExpression.Instance);
      ILocalDefinition/*?*/ loc = boundExpression.Definition as ILocalDefinition;
      if (loc != null)
        boundExpression.Definition = this.GetMutableCopyIfItExists(loc);
      else {
        IParameterDefinition/*?*/ par = boundExpression.Definition as IParameterDefinition;
        if (par != null)
          boundExpression.Definition = this.GetMutableCopyIfItExists(par);
        else {
          IFieldReference/*?*/ field = boundExpression.Definition as IFieldReference;
          boundExpression.Definition = this.Substitute(field);
        }
      }
      boundExpression.Type = this.Substitute(boundExpression.Type);
      return boundExpression;
    }

    /// <summary>
    /// Visits the specified break statement.
    /// </summary>
    /// <param name="breakStatement">The break statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(BreakStatement breakStatement) {
      return breakStatement;
    }

    /// <summary>
    /// Visits the specified cast if possible.
    /// </summary>
    /// <param name="castIfPossible">The cast if possible.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(CastIfPossible castIfPossible) {
      castIfPossible.TargetType = Substitute(castIfPossible.TargetType);
      castIfPossible.ValueToCast = Substitute(castIfPossible.ValueToCast);
      castIfPossible.Type = this.Substitute(castIfPossible.Type);
      return castIfPossible;
    }


    /// <summary>
    /// Visits the specified catch clause.
    /// </summary>
    /// <param name="catchClause">The catch clause.</param>
    /// <returns></returns>
    protected virtual ICatchClause DeepCopy(CatchClause catchClause) {
      if (catchClause.FilterCondition != null)
        catchClause.FilterCondition = Substitute(catchClause.FilterCondition);
      catchClause.Body = (IBlockStatement)Substitute(catchClause.Body);
      return catchClause;
    }

    /// <summary>
    /// Visits the specified check if instance.
    /// </summary>
    /// <param name="checkIfInstance">The check if instance.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(CheckIfInstance checkIfInstance) {
      checkIfInstance.Operand = Substitute(checkIfInstance.Operand);
      checkIfInstance.TypeToCheck = Substitute(checkIfInstance.TypeToCheck);
      checkIfInstance.Type = this.Substitute(checkIfInstance.Type);
      return checkIfInstance;
    }

    /// <summary>
    /// Visits the specified constant.
    /// </summary>
    /// <param name="constant">The constant.</param>
    /// <returns></returns>
    protected virtual ICompileTimeConstant DeepCopy(CompileTimeConstant constant) {
      constant.Type = this.Substitute(constant.Type);
      return constant;
    }

    /// <summary>
    /// Visits the specified conversion.
    /// </summary>
    /// <param name="conversion">The conversion.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Conversion conversion) {
      conversion.ValueToConvert = Substitute(conversion.ValueToConvert);
      conversion.Type = this.Substitute(conversion.Type);
      return conversion;
    }

    /// <summary>
    /// Visits the specified conditional.
    /// </summary>
    /// <param name="conditional">The conditional.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Conditional conditional) {
      conditional.Condition = Substitute(conditional.Condition);
      conditional.ResultIfTrue = Substitute(conditional.ResultIfTrue);
      conditional.ResultIfFalse = Substitute(conditional.ResultIfFalse);
      conditional.Type = this.Substitute(conditional.Type);
      return conditional;
    }

    /// <summary>
    /// Visits the specified conditional statement.
    /// </summary>
    /// <param name="conditionalStatement">The conditional statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ConditionalStatement conditionalStatement) {
      conditionalStatement.Condition = Substitute(conditionalStatement.Condition);
      conditionalStatement.TrueBranch = Substitute(conditionalStatement.TrueBranch);
      conditionalStatement.FalseBranch = Substitute(conditionalStatement.FalseBranch);
      return conditionalStatement;
    }

    /// <summary>
    /// Visits the specified continue statement.
    /// </summary>
    /// <param name="continueStatement">The continue statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ContinueStatement continueStatement) {
      return continueStatement;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(CopyMemoryStatement copyMemoryStatement) {
      copyMemoryStatement.TargetAddress = Substitute(copyMemoryStatement.TargetAddress);
      copyMemoryStatement.SourceAddress = Substitute(copyMemoryStatement.SourceAddress);
      copyMemoryStatement.NumberOfBytesToCopy = Substitute(copyMemoryStatement.NumberOfBytesToCopy);
      return copyMemoryStatement;
    }

    /// <summary>
    /// Visits the specified create array.
    /// </summary>
    /// <param name="createArray">The create array.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(CreateArray createArray) {
      createArray.ElementType = this.Substitute(createArray.ElementType);
      createArray.Sizes = this.DeepCopy(createArray.Sizes);
      createArray.Initializers = this.DeepCopy(createArray.Initializers);
      createArray.Type = this.Substitute(createArray.Type);
      return createArray;
    }

    /// <summary>
    /// Visits the specified create object instance.
    /// </summary>
    /// <param name="createObjectInstance">The create object instance.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(CreateObjectInstance createObjectInstance) {
      createObjectInstance.MethodToCall = this.Substitute(createObjectInstance.MethodToCall);
      createObjectInstance.Arguments = this.DeepCopy(createObjectInstance.Arguments);
      createObjectInstance.Type = this.Substitute(createObjectInstance.Type);
      return createObjectInstance;
    }

    /// <summary>
    /// Visits the specified create delegate instance.
    /// </summary>
    /// <param name="createDelegateInstance">The create delegate instance.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(CreateDelegateInstance createDelegateInstance) {
      createDelegateInstance.MethodToCallViaDelegate = this.Substitute(createDelegateInstance.MethodToCallViaDelegate);
      if (createDelegateInstance.Instance != null)
        createDelegateInstance.Instance = Substitute(createDelegateInstance.Instance);
      createDelegateInstance.Type = this.Substitute(createDelegateInstance.Type);
      return createDelegateInstance;
    }

    /// <summary>
    /// Visits the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(DefaultValue defaultValue) {
      defaultValue.DefaultValueType = Substitute(defaultValue.DefaultValueType);
      defaultValue.Type = this.Substitute(defaultValue.Type);
      return defaultValue;
    }

    /// <summary>
    /// Visits the specified debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement">The debugger break statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(DebuggerBreakStatement debuggerBreakStatement) {
      return debuggerBreakStatement;
    }

    /// <summary>
    /// Visits the specified division.
    /// </summary>
    /// <param name="division">The division.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Division division) {
      return this.DeepCopy((BinaryOperation)division);
    }

    /// <summary>
    /// Visits the specified do until statement.
    /// </summary>
    /// <param name="doUntilStatement">The do until statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(DoUntilStatement doUntilStatement) {
      doUntilStatement.Body = Substitute(doUntilStatement.Body);
      doUntilStatement.Condition = Substitute(doUntilStatement.Condition);
      return doUntilStatement;
    }

    /// <summary>
    /// Visits the specified dup value expression.
    /// </summary>
    /// <param name="dupValue">The dup value expression.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(DupValue dupValue) {
      dupValue.Type = this.Substitute(dupValue.Type);
      return dupValue;
    }

    /// <summary>
    /// Visits the specified empty statement.
    /// </summary>
    /// <param name="emptyStatement">The empty statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(EmptyStatement emptyStatement) {
      return emptyStatement;
    }

    /// <summary>
    /// Visits the specified equality.
    /// </summary>
    /// <param name="equality">The equality.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Equality equality) {
      return this.DeepCopy((BinaryOperation)equality);
    }

    /// <summary>
    /// Visits the specified exclusive or.
    /// </summary>
    /// <param name="exclusiveOr">The exclusive or.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(ExclusiveOr exclusiveOr) {
      return this.DeepCopy((BinaryOperation)exclusiveOr);
    }

    /// <summary>
    /// Visits the specified expressions.
    /// </summary>
    /// <param name="expressions">The expressions.</param>
    /// <returns></returns>
    protected virtual List<IExpression> DeepCopy(List<IExpression> expressions) {
      List<IExpression> newList = new List<IExpression>();
      foreach (var expression in expressions)
        newList.Add(this.Substitute(expression));
      return newList;
    }

    /// <summary>
    /// Visits the specified expression statement.
    /// </summary>
    /// <param name="expressionStatement">The expression statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ExpressionStatement expressionStatement) {
      expressionStatement.Expression = Substitute(expressionStatement.Expression);
      return expressionStatement;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(FillMemoryStatement fillMemoryStatement) {
      fillMemoryStatement.TargetAddress = Substitute(fillMemoryStatement.TargetAddress);
      fillMemoryStatement.FillValue = Substitute(fillMemoryStatement.FillValue);
      fillMemoryStatement.NumberOfBytesToFill = Substitute(fillMemoryStatement.NumberOfBytesToFill);
      return fillMemoryStatement;
    }

    /// <summary>
    /// Visits the specified for each statement.
    /// </summary>
    /// <param name="forEachStatement">For each statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ForEachStatement forEachStatement) {
      forEachStatement.Collection = Substitute(forEachStatement.Collection);
      forEachStatement.Body = Substitute(forEachStatement.Body);
      return forEachStatement;
    }

    /// <summary>
    /// Visits the specified for statement.
    /// </summary>
    /// <param name="forStatement">For statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ForStatement forStatement) {
      forStatement.InitStatements = Substitute(forStatement.InitStatements);
      forStatement.Condition = Substitute(forStatement.Condition);
      forStatement.IncrementStatements = Substitute(forStatement.IncrementStatements);
      forStatement.Body = Substitute(forStatement.Body);
      return forStatement;
    }

    /// <summary>
    /// Visits the specified get type of typed reference.
    /// </summary>
    /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(GetTypeOfTypedReference getTypeOfTypedReference) {
      getTypeOfTypedReference.TypedReference = Substitute(getTypeOfTypedReference.TypedReference);
      getTypeOfTypedReference.Type = this.Substitute(getTypeOfTypedReference.Type);
      return getTypeOfTypedReference;
    }

    /// <summary>
    /// Visits the specified get value of typed reference.
    /// </summary>
    /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(GetValueOfTypedReference getValueOfTypedReference) {
      getValueOfTypedReference.TypedReference = Substitute(getValueOfTypedReference.TypedReference);
      getValueOfTypedReference.TargetType = Substitute(getValueOfTypedReference.TargetType);
      getValueOfTypedReference.Type = this.Substitute(getValueOfTypedReference.Type);
      return getValueOfTypedReference;
    }

    /// <summary>
    /// Visits the specified goto statement.
    /// </summary>
    /// <param name="gotoStatement">The goto statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(GotoStatement gotoStatement) {
      return gotoStatement;
    }

    /// <summary>
    /// Visits the specified goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(GotoSwitchCaseStatement gotoSwitchCaseStatement) {
      return gotoSwitchCaseStatement;
    }

    /// <summary>
    /// Visits the specified greater than.
    /// </summary>
    /// <param name="greaterThan">The greater than.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(GreaterThan greaterThan) {
      return this.DeepCopy((BinaryOperation)greaterThan);
    }

    /// <summary>
    /// Visits the specified greater than or equal.
    /// </summary>
    /// <param name="greaterThanOrEqual">The greater than or equal.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(GreaterThanOrEqual greaterThanOrEqual) {
      return this.DeepCopy((BinaryOperation)greaterThanOrEqual);
    }

    /// <summary>
    /// Visits the specified labeled statement.
    /// </summary>
    /// <param name="labeledStatement">The labeled statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(LabeledStatement labeledStatement) {
      labeledStatement.Statement = Substitute(labeledStatement.Statement);
      return labeledStatement;
    }

    /// <summary>
    /// Visits the specified left shift.
    /// </summary>
    /// <param name="leftShift">The left shift.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(LeftShift leftShift) {
      return this.DeepCopy((BinaryOperation)leftShift);
    }

    /// <summary>
    /// Visits the specified less than.
    /// </summary>
    /// <param name="lessThan">The less than.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(LessThan lessThan) {
      return this.DeepCopy((BinaryOperation)lessThan);
    }

    /// <summary>
    /// Visits the specified less than or equal.
    /// </summary>
    /// <param name="lessThanOrEqual">The less than or equal.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(LessThanOrEqual lessThanOrEqual) {
      return this.DeepCopy((BinaryOperation)lessThanOrEqual);
    }

    /// <summary>
    /// Visits the specified local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(LocalDeclarationStatement localDeclarationStatement) {
      localDeclarationStatement.LocalVariable = this.Substitute(localDeclarationStatement.LocalVariable);
      if (localDeclarationStatement.InitialValue != null)
        localDeclarationStatement.InitialValue = Substitute(localDeclarationStatement.InitialValue);
      return localDeclarationStatement;
    }

    /// <summary>
    /// Visit local definition.
    /// </summary>
    /// <param name="localDefinition"></param>
    /// <returns></returns>
    public virtual ILocalDefinition Substitute(ILocalDefinition localDefinition) {
      return this.DeepCopy(this.GetMutableCopy(localDefinition));
    }

    /// <summary>
    /// Deep copy a local definition. 
    /// </summary>
    /// <param name="localDefinition"></param>
    /// <returns></returns>
    protected override LocalDefinition DeepCopy(LocalDefinition localDefinition) {
      localDefinition.Type = this.Substitute(localDefinition.Type);
      localDefinition.CustomModifiers = this.DeepCopy(localDefinition.CustomModifiers);
      return localDefinition;
    }

    /// <summary>
    /// Create a mutable copy of a local definition. 
    /// </summary>
    /// <param name="localDefinition"></param>
    /// <returns></returns>
    protected virtual LocalDefinition GetMutableCopy(ILocalDefinition localDefinition) {
      object cachedValue;
      if (this.cache.TryGetValue(localDefinition, out cachedValue)) {
        return (LocalDefinition)cachedValue;
      }
      var result = new LocalDefinition();
      result.Copy(localDefinition, this.host.InternFactory);
      this.cache.Add(localDefinition, result);
      this.cache.Add(result, result);
      return result;
    }

    /// <summary>
    /// Visits the specified lock statement.
    /// </summary>
    /// <param name="lockStatement">The lock statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(LockStatement lockStatement) {
      lockStatement.Guard = Substitute(lockStatement.Guard);
      lockStatement.Body = Substitute(lockStatement.Body);
      return lockStatement;
    }

    /// <summary>
    /// Visits the specified logical not.
    /// </summary>
    /// <param name="logicalNot">The logical not.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(LogicalNot logicalNot) {
      return this.DeepCopy((UnaryOperation)logicalNot);
    }

    /// <summary>
    /// Visits the specified make typed reference.
    /// </summary>
    /// <param name="makeTypedReference">The make typed reference.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(MakeTypedReference makeTypedReference) {
      makeTypedReference.Operand = Substitute(makeTypedReference.Operand);
      makeTypedReference.Type = this.Substitute(makeTypedReference.Type);
      return makeTypedReference;
    }

    /// <summary>
    /// Visits the specified method call.
    /// </summary>
    /// <param name="methodCall">The method call.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(MethodCall methodCall) {
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall)
        methodCall.ThisArgument = this.Substitute(methodCall.ThisArgument);
      methodCall.Arguments = this.DeepCopy(methodCall.Arguments);
      methodCall.MethodToCall = this.Substitute(methodCall.MethodToCall);
      methodCall.Type = this.Substitute(methodCall.Type);
      return methodCall;
    }

    /// <summary>
    /// Visits the specified modulus.
    /// </summary>
    /// <param name="modulus">The modulus.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Modulus modulus) {
      return this.DeepCopy((BinaryOperation)modulus);
    }

    /// <summary>
    /// Visits the specified multiplication.
    /// </summary>
    /// <param name="multiplication">The multiplication.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Multiplication multiplication) {
      return this.DeepCopy((BinaryOperation)multiplication);
    }

    /// <summary>
    /// Visits the specified named argument.
    /// </summary>
    /// <param name="namedArgument">The named argument.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(NamedArgument namedArgument) {
      namedArgument.ArgumentValue = namedArgument.ArgumentValue;
      namedArgument.Type = this.Substitute(namedArgument.Type);
      return namedArgument;
    }

    /// <summary>
    /// Visits the specified not equality.
    /// </summary>
    /// <param name="notEquality">The not equality.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(NotEquality notEquality) {
      return this.DeepCopy((BinaryOperation)notEquality);
    }

    /// <summary>
    /// Visits the specified old value.
    /// </summary>
    /// <param name="oldValue">The old value.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(OldValue oldValue) {
      oldValue.Expression = Substitute(oldValue.Expression);
      oldValue.Type = this.Substitute(oldValue.Type);
      return oldValue;
    }

    /// <summary>
    /// Visits the specified ones complement.
    /// </summary>
    /// <param name="onesComplement">The ones complement.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(OnesComplement onesComplement) {
      return this.DeepCopy((UnaryOperation)onesComplement);
    }

    /// <summary>
    /// Visits the specified unary operation.
    /// </summary>
    /// <param name="unaryOperation">The unary operation.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(UnaryOperation unaryOperation) {
      unaryOperation.Operand = Substitute(unaryOperation.Operand);
      unaryOperation.Type = this.Substitute(unaryOperation.Type);
      return unaryOperation;
    }

    /// <summary>
    /// Visits the specified out argument.
    /// </summary>
    /// <param name="outArgument">The out argument.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(OutArgument outArgument) {
      outArgument.Expression = (ITargetExpression)Substitute(outArgument.Expression);
      outArgument.Type = this.Substitute(outArgument.Type);
      return outArgument;
    }

    /// <summary>
    /// Visits the specified pointer call.
    /// </summary>
    /// <param name="pointerCall">The pointer call.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(PointerCall pointerCall) {
      pointerCall.Pointer = this.Substitute(pointerCall.Pointer);
      pointerCall.Arguments = this.DeepCopy(pointerCall.Arguments);
      pointerCall.Type = this.Substitute(pointerCall.Type);
      return pointerCall;
    }

    /// <summary>
    /// Visits the specified pop value expression.
    /// </summary>
    /// <param name="popValue">The pop value expression.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(PopValue popValue) {
      popValue.Type = this.Substitute(popValue.Type);
      return popValue;
    }

    /// <summary>
    /// Visits the specified push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(PushStatement pushStatement) {
      pushStatement.ValueToPush = this.Substitute(pushStatement.ValueToPush);
      return pushStatement;
    }

    /// <summary>
    /// Visits the specified ref argument.
    /// </summary>
    /// <param name="refArgument">The ref argument.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(RefArgument refArgument) {
      refArgument.Expression = (IAddressableExpression)this.Substitute(refArgument.Expression);
      refArgument.Type = this.Substitute(refArgument.Type);
      return refArgument;
    }

    /// <summary>
    /// Visits the specified resource use statement.
    /// </summary>
    /// <param name="resourceUseStatement">The resource use statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ResourceUseStatement resourceUseStatement) {
      resourceUseStatement.ResourceAcquisitions = this.Substitute(resourceUseStatement.ResourceAcquisitions);
      resourceUseStatement.Body = this.Substitute(resourceUseStatement.Body);
      return resourceUseStatement;
    }

    /// <summary>
    /// Visits the specified rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement">The rethrow statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(RethrowStatement rethrowStatement) {
      return rethrowStatement;
    }

    /// <summary>
    /// Visits the specified return statement.
    /// </summary>
    /// <param name="returnStatement">The return statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ReturnStatement returnStatement) {
      if (returnStatement.Expression != null)
        returnStatement.Expression = this.Substitute(returnStatement.Expression);
      return returnStatement;
    }

    /// <summary>
    /// Visits the specified return value.
    /// </summary>
    /// <param name="returnValue">The return value.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(ReturnValue returnValue) {
      returnValue.Type = this.Substitute(returnValue.Type);
      return returnValue;
    }

    /// <summary>
    /// Visits the specified right shift.
    /// </summary>
    /// <param name="rightShift">The right shift.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(RightShift rightShift) {
      return this.DeepCopy((BinaryOperation)rightShift);
    }

    /// <summary>
    /// Visits the specified runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(RuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      runtimeArgumentHandleExpression.Type = this.Substitute(runtimeArgumentHandleExpression.Type);
      return runtimeArgumentHandleExpression;
    }

    /// <summary>
    /// Visits the specified size of.
    /// </summary>
    /// <param name="sizeOf">The size of.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(SizeOf sizeOf) {
      sizeOf.TypeToSize = this.Substitute(sizeOf.TypeToSize);
      sizeOf.Type = this.Substitute(sizeOf.Type);
      return sizeOf;
    }

    /// <summary>
    /// Visits the specified stack array create.
    /// </summary>
    /// <param name="stackArrayCreate">The stack array create.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(StackArrayCreate stackArrayCreate) {
      stackArrayCreate.ElementType = this.Substitute(stackArrayCreate.ElementType);
      stackArrayCreate.Size = this.Substitute(stackArrayCreate.Size);
      stackArrayCreate.Type = this.Substitute(stackArrayCreate.Type);
      return stackArrayCreate;
    }

    /// <summary>
    /// Visits the specified subtraction.
    /// </summary>
    /// <param name="subtraction">The subtraction.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(Subtraction subtraction) {
      return this.DeepCopy((BinaryOperation)subtraction);
    }

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
    /// <returns></returns>
    protected virtual List<ISwitchCase> DeepCopy(List<ISwitchCase> switchCases) {
      List<ISwitchCase> newList = new List<ISwitchCase>();
      foreach (var switchCase in switchCases) {
        var newCase = this.DeepCopy(this.GetMutableCopy(switchCase));
        newList.Add(newCase);
      }
      return newList;
    }

    /// <summary>
    /// Get the mutable copy of a switch case. 
    /// </summary>
    /// <param name="swithCase"></param>
    /// <returns></returns>
    public virtual SwitchCase GetMutableCopy(ISwitchCase swithCase) {
      object cachedValue;
      if (this.cache.TryGetValue(swithCase, out cachedValue))
        return (SwitchCase)cachedValue;
      var result = new SwitchCase(swithCase);
      // Probably not necessary, no two switch cases are shared. 
      this.cache.Add(swithCase, result);
      this.cache.Add(result, result);
      return result;
    }

    /// <summary>
    /// Visits the specified switch case.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    /// <returns></returns>
    protected virtual ISwitchCase DeepCopy(SwitchCase switchCase) {
      if (!switchCase.IsDefault)
        switchCase.Expression = (ICompileTimeConstant)Substitute(switchCase.Expression);
      switchCase.Body = this.Substitute(switchCase.Body);
      return switchCase;
    }

    /// <summary>
    /// Visits the specified switch statement.
    /// </summary>
    /// <param name="switchStatement">The switch statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(SwitchStatement switchStatement) {
      switchStatement.Expression = this.Substitute(switchStatement.Expression);
      switchStatement.Cases = this.DeepCopy(switchStatement.Cases);
      return switchStatement;
    }

    /// <summary>
    /// Visits the specified target expression.
    /// </summary>
    /// <param name="targetExpression">The target expression.</param>
    /// <returns></returns>
    protected virtual ITargetExpression DeepCopy(TargetExpression targetExpression) {
      object def = targetExpression.Definition;
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
              targetExpression.Instance = this.Substitute(targetExpression.Instance);
            targetExpression.Definition = this.Substitute(field);
          } else {
            IArrayIndexer/*?*/ indexer = def as IArrayIndexer;
            if (indexer != null) {
              targetExpression.Definition = this.Substitute(indexer);
              indexer = targetExpression.Definition as IArrayIndexer;
              if (indexer != null) {
                targetExpression.Instance = indexer.IndexedObject;
                targetExpression.Type = indexer.Type;
                return targetExpression;
              }
            } else {
              IAddressDereference/*?*/ adr = def as IAddressDereference;
              if (adr != null)
                targetExpression.Definition = this.Substitute(adr);
              else {
                IPropertyDefinition/*?*/ prop = def as IPropertyDefinition;
                if (prop != null) {
                  if (targetExpression.Instance != null)
                    targetExpression.Instance = this.Substitute(targetExpression.Instance);
                  targetExpression.Definition = this.GetMutableCopyIfExists(prop);
                }
              }
            }
          }
        }
      }
      targetExpression.Type = this.Substitute(targetExpression.Type);
      return targetExpression;
    }

    /// <summary>
    /// Get a property definition's copy from the cache if it was copied or otherwise return the property
    /// definition itself. 
    /// </summary>
    /// <param name="propertyDefinition"></param>
    /// <returns></returns>
    private IPropertyDefinition GetMutableCopyIfExists(IPropertyDefinition propertyDefinition) {
      object cachedValue;
      if (this.cache.TryGetValue(propertyDefinition, out cachedValue)) {
        return (IPropertyDefinition)propertyDefinition;
      }
      return propertyDefinition;
    }

    /// <summary>
    /// Visits the specified this reference.
    /// </summary>
    /// <param name="thisReference">The this reference.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(ThisReference thisReference) {
      thisReference.Type = this.Substitute(thisReference.Type);
      return thisReference;
    }

    /// <summary>
    /// Visits the specified throw statement.
    /// </summary>
    /// <param name="throwStatement">The throw statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(ThrowStatement throwStatement) {
      if (throwStatement.Exception != null)
        throwStatement.Exception = Substitute(throwStatement.Exception);
      return throwStatement;
    }

    /// <summary>
    /// Visits the specified try catch filter finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(TryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      tryCatchFilterFinallyStatement.TryBody = (IBlockStatement)Substitute(tryCatchFilterFinallyStatement.TryBody);
      tryCatchFilterFinallyStatement.CatchClauses = this.DeepCopy(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        tryCatchFilterFinallyStatement.FinallyBody = (IBlockStatement)Substitute(tryCatchFilterFinallyStatement.FinallyBody);
      return tryCatchFilterFinallyStatement;
    }

    /// <summary>
    /// Deep copy a list of catch clauses. 
    /// </summary>
    /// <param name="catchClauses"></param>
    /// <returns></returns>
    protected List<ICatchClause> DeepCopy(List<ICatchClause> catchClauses) {
      var result = new List<ICatchClause>();
      foreach (var c in catchClauses) {
        result.Add(this.DeepCopy(this.GetMutableCopy(c)));
      }
      return result;
    }

    /// <summary>
    /// Create a mutable copy of an icatchclause.
    /// </summary>
    /// <param name="catchClause"></param>
    /// <returns></returns>
    protected virtual CatchClause GetMutableCopy(ICatchClause catchClause) {
      var copy = new CatchClause(catchClause);
      return copy;
    }

    /// <summary>
    /// Visits the specified token of.
    /// </summary>
    /// <param name="tokenOf">The token of.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(TokenOf tokenOf) {
      IFieldReference/*?*/ fieldReference = tokenOf.Definition as IFieldReference;
      if (fieldReference != null)
        tokenOf.Definition = this.Substitute(fieldReference);
      else {
        IMethodReference/*?*/ methodReference = tokenOf.Definition as IMethodReference;
        if (methodReference != null)
          tokenOf.Definition = this.Substitute(methodReference);
        else
          tokenOf.Definition = this.Substitute((ITypeReference)tokenOf.Definition);
      }
      tokenOf.Type = this.Substitute(tokenOf.Type);
      return tokenOf;
    }

    /// <summary>
    /// Visits the specified type of.
    /// </summary>
    /// <param name="typeOf">The type of.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(TypeOf typeOf) {
      typeOf.TypeToGet = this.Substitute(typeOf.TypeToGet);
      typeOf.Type = this.Substitute(typeOf.Type);
      return typeOf;
    }

    /// <summary>
    /// Visits the specified unary negation.
    /// </summary>
    /// <param name="unaryNegation">The unary negation.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(UnaryNegation unaryNegation) {
      return this.DeepCopy((UnaryOperation)unaryNegation);
    }

    /// <summary>
    /// Visits the specified unary plus.
    /// </summary>
    /// <param name="unaryPlus">The unary plus.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(UnaryPlus unaryPlus) {
      return this.DeepCopy((UnaryOperation)unaryPlus);
    }

    /// <summary>
    /// Visits the specified vector length.
    /// </summary>
    /// <param name="vectorLength">Length of the vector.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(VectorLength vectorLength) {
      vectorLength.Vector = Substitute(vectorLength.Vector);
      vectorLength.Type = this.Substitute(vectorLength.Type);
      return vectorLength;
    }

    /// <summary>
    /// Visits the specified while do statement.
    /// </summary>
    /// <param name="whileDoStatement">The while do statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(WhileDoStatement whileDoStatement) {
      whileDoStatement.Condition = Substitute(whileDoStatement.Condition);
      whileDoStatement.Body = Substitute(whileDoStatement.Body);
      return whileDoStatement;
    }

    /// <summary>
    /// Visits the specified yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement">The yield break statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(YieldBreakStatement yieldBreakStatement) {
      return yieldBreakStatement;
    }

    /// <summary>
    /// Visits the specified yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement">The yield return statement.</param>
    /// <returns></returns>
    protected virtual IStatement DeepCopy(YieldReturnStatement yieldReturnStatement) {
      yieldReturnStatement.Expression = Substitute(yieldReturnStatement.Expression);
      return yieldReturnStatement;
    }

    #endregion

    /// <summary>
    /// The best way to dispatch a visit method in code copier is to implement an 
    /// ICodeVisitor and call the dispatch method of IExpression or IStatement dynamically.
    /// This class implements the ICodeVisitor interface for the code copier. 
    /// After a node is visited, the result is stored in resultExpression or resultStatement.
    /// </summary>
#pragma warning disable 618
    private class CreateMutableType : BaseCodeVisitor, ICodeVisitor {
#pragma warning restore 618
      /// <summary>
      /// The parent code copier.
      /// </summary>
      internal CodeCopier myCodeCopier;

      /// <summary>
      /// The resultant expression or statement. 
      /// </summary>
      internal IExpression resultExpression = CodeDummy.Expression;
      internal IStatement resultStatement = CodeDummy.Block;

      /// <summary>
      /// The best way to dispatch a visit method in code copier is to implement an 
      /// ICodeVisitor and call the dispatch method of IExpression or IStatement dynamically.
      /// This class implements the ICodeVisitor interface for the code copier. 
      /// After a node is visited, the result is stored in resultExpression or resultStatement.
      /// </summary>
      internal CreateMutableType(CodeCopier codeCopier) {
        this.myCodeCopier = codeCopier;
      }

      #region overriding implementations of ICodeVisitor Members

      /// <summary>
      /// Visits the specified addition.
      /// </summary>
      /// <param name="addition">The addition.</param>
      public override void Visit(IAddition addition) {
        Addition/*?*/ mutableAddition = new Addition(addition);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableAddition);
      }

      /// <summary>
      /// Visits the specified addressable expression.
      /// </summary>
      /// <param name="addressableExpression">The addressable expression.</param>
      public override void Visit(IAddressableExpression addressableExpression) {
        AddressableExpression mutableAddressableExpression = new AddressableExpression(addressableExpression);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableAddressableExpression);
      }

      /// <summary>
      /// Visits the specified address dereference.
      /// </summary>
      /// <param name="addressDereference">The address dereference.</param>
      public override void Visit(IAddressDereference addressDereference) {
        AddressDereference mutableAddressDereference  = new AddressDereference(addressDereference);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableAddressDereference);
      }

      /// <summary>
      /// Visits the specified address of.
      /// </summary>
      /// <param name="addressOf">The address of.</param>
      public override void Visit(IAddressOf addressOf) {
        AddressOf mutableAddressOf = new AddressOf(addressOf);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableAddressOf);
      }

      /// <summary>
      /// Visits the specified anonymous method.
      /// </summary>
      /// <param name="anonymousMethod">The anonymous method.</param>
      public override void Visit(IAnonymousDelegate anonymousMethod) {
        AnonymousDelegate mutableAnonymousDelegate = new AnonymousDelegate(anonymousMethod);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableAnonymousDelegate);
      }

      /// <summary>
      /// Visits the specified array indexer.
      /// </summary>
      /// <param name="arrayIndexer">The array indexer.</param>
      public override void Visit(IArrayIndexer arrayIndexer) {
        ArrayIndexer mutableArrayIndexer = new ArrayIndexer(arrayIndexer);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableArrayIndexer);
      }

      /// <summary>
      /// Visits the specified assert statement.
      /// </summary>
      /// <param name="assertStatement">The assert statement.</param>
      public override void Visit(IAssertStatement assertStatement) {
        AssertStatement mutableAssertStatement = new AssertStatement(assertStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableAssertStatement);
      }

      /// <summary>
      /// Visits the specified assignment.
      /// </summary>
      /// <param name="assignment">The assignment.</param>
      public override void Visit(IAssignment assignment) {
        Assignment mutableAssignment = new Assignment(assignment);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableAssignment);
      }

      /// <summary>
      /// Visits the specified assume statement.
      /// </summary>
      /// <param name="assumeStatement">The assume statement.</param>
      public override void Visit(IAssumeStatement assumeStatement) {
        AssumeStatement mutableAssumeStatement = new AssumeStatement(assumeStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableAssumeStatement);
      }

      /// <summary>
      /// Visits the specified bitwise and.
      /// </summary>
      /// <param name="bitwiseAnd">The bitwise and.</param>
      public override void Visit(IBitwiseAnd bitwiseAnd) {
        BitwiseAnd mutableBitwiseAnd = new BitwiseAnd(bitwiseAnd);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableBitwiseAnd);
      }

      /// <summary>
      /// Visits the specified bitwise or.
      /// </summary>
      /// <param name="bitwiseOr">The bitwise or.</param>
      public override void Visit(IBitwiseOr bitwiseOr) {
        BitwiseOr mutableBitwiseOr = new BitwiseOr(bitwiseOr);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableBitwiseOr);
      }

      /// <summary>
      /// Visits the specified block expression.
      /// </summary>
      /// <param name="blockExpression">The block expression.</param>
      public override void Visit(IBlockExpression blockExpression) {
        BlockExpression mutableBlockExpression = new BlockExpression(blockExpression);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableBlockExpression);
      }

      /// <summary>
      /// Visits the specified block.
      /// </summary>
      /// <param name="block">The block.</param>
      public override void Visit(IBlockStatement block) {
        BlockStatement mutableBlockStatement = new BlockStatement(block);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableBlockStatement);
      }

      /// <summary>
      /// Visits the specified break statement.
      /// </summary>
      /// <param name="breakStatement">The break statement.</param>
      public override void Visit(IBreakStatement breakStatement) {
        BreakStatement mutableBreakStatement = new BreakStatement(breakStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableBreakStatement);
      }

      /// <summary>
      /// Visits the specified bound expression.
      /// </summary>
      /// <param name="boundExpression">The bound expression.</param>
      public override void Visit(IBoundExpression boundExpression) {
        BoundExpression mutableBoundExpression = new BoundExpression(boundExpression);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableBoundExpression);
      }

      /// <summary>
      /// Visits the specified cast if possible.
      /// </summary>
      /// <param name="castIfPossible">The cast if possible.</param>
      public override void Visit(ICastIfPossible castIfPossible) {
        CastIfPossible mutableCastIfPossible = new CastIfPossible(castIfPossible);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableCastIfPossible);
      }

      /// <summary>
      /// Visits the specified check if instance.
      /// </summary>
      /// <param name="checkIfInstance">The check if instance.</param>
      public override void Visit(ICheckIfInstance checkIfInstance) {
        CheckIfInstance mutableCheckIfInstance = new CheckIfInstance(checkIfInstance);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableCheckIfInstance);
      }

      /// <summary>
      /// Visits the specified constant.
      /// </summary>
      /// <param name="constant">The constant.</param>
      public override void Visit(ICompileTimeConstant constant) {
        CompileTimeConstant mutableCompileTimeConstant = new CompileTimeConstant(constant);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableCompileTimeConstant);
      }

      /// <summary>
      /// Visits the specified conversion.
      /// </summary>
      /// <param name="conversion">The conversion.</param>
      public override void Visit(IConversion conversion) {
        Conversion mutableConversion = new Conversion(conversion);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableConversion);
      }

      /// <summary>
      /// Visits the specified conditional.
      /// </summary>
      /// <param name="conditional">The conditional.</param>
      public override void Visit(IConditional conditional) {
        Conditional mutableConditional = new Conditional(conditional);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableConditional);
      }

      /// <summary>
      /// Visits the specified conditional statement.
      /// </summary>
      /// <param name="conditionalStatement">The conditional statement.</param>
      public override void Visit(IConditionalStatement conditionalStatement) {
        ConditionalStatement mutableConditionalStatement = new ConditionalStatement(conditionalStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableConditionalStatement);
      }

      /// <summary>
      /// Visits the specified continue statement.
      /// </summary>
      /// <param name="continueStatement">The continue statement.</param>
      public override void Visit(IContinueStatement continueStatement) {
        ContinueStatement mutableContinueStatement = new ContinueStatement(continueStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableContinueStatement);
      }

      /// <summary>
      /// Performs some computation with the given copy memory statement.
      /// </summary>
      /// <param name="copyMemoryStatement"></param>
      public override void Visit(ICopyMemoryStatement copyMemoryStatement) {
        CopyMemoryStatement mutableCopyMemoryStatement = new CopyMemoryStatement(copyMemoryStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableCopyMemoryStatement);
      }

      /// <summary>
      /// Visits the specified create array.
      /// </summary>
      /// <param name="createArray">The create array.</param>
      public override void Visit(ICreateArray createArray) {
        CreateArray mutableCreateArray = new CreateArray(createArray);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableCreateArray);
      }

      /// <summary>
      /// Visits the specified create delegate instance.
      /// </summary>
      /// <param name="createDelegateInstance">The create delegate instance.</param>
      public override void Visit(ICreateDelegateInstance createDelegateInstance) {
        CreateDelegateInstance mutableCreateDelegateInstance = new CreateDelegateInstance(createDelegateInstance);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableCreateDelegateInstance);
      }

      /// <summary>
      /// Visits the specified create object instance.
      /// </summary>
      /// <param name="createObjectInstance">The create object instance.</param>
      public override void Visit(ICreateObjectInstance createObjectInstance) {
        CreateObjectInstance mutableCreateObjectInstance = new CreateObjectInstance(createObjectInstance);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableCreateObjectInstance);
      }

      /// <summary>
      /// Visits the specified debugger break statement.
      /// </summary>
      /// <param name="debuggerBreakStatement">The debugger break statement.</param>
      public override void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        DebuggerBreakStatement mutableDebuggerBreakStatement = new DebuggerBreakStatement(debuggerBreakStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableDebuggerBreakStatement);
      }

      /// <summary>
      /// Visits the specified default value.
      /// </summary>
      /// <param name="defaultValue">The default value.</param>
      public override void Visit(IDefaultValue defaultValue) {
        DefaultValue mutableDefaultValue = new DefaultValue(defaultValue);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableDefaultValue);
      }

      /// <summary>
      /// Visits the specified division.
      /// </summary>
      /// <param name="division">The division.</param>
      public override void Visit(IDivision division) {
        Division mutableDivision = new Division(division);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableDivision);
      }

      /// <summary>
      /// Visits the specified do until statement.
      /// </summary>
      /// <param name="doUntilStatement">The do until statement.</param>
      public override void Visit(IDoUntilStatement doUntilStatement) {
        DoUntilStatement mutableDoUntilStatement = new DoUntilStatement(doUntilStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableDoUntilStatement);
      }

      /// <summary>
      /// Performs some computation with the given dup value expression.
      /// </summary>
      public override void Visit(IDupValue dupValue) {
        DupValue mutableDupValue = new DupValue(dupValue);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableDupValue);
      }

      /// <summary>
      /// Visits the specified empty statement.
      /// </summary>
      /// <param name="emptyStatement">The empty statement.</param>
      public override void Visit(IEmptyStatement emptyStatement) {
        EmptyStatement mutableEmptyStatement = new EmptyStatement(emptyStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableEmptyStatement);
      }

      /// <summary>
      /// Visits the specified equality.
      /// </summary>
      /// <param name="equality">The equality.</param>
      public override void Visit(IEquality equality) {
        Equality mutableEquality = new Equality(equality);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableEquality);
      }

      /// <summary>
      /// Visits the specified exclusive or.
      /// </summary>
      /// <param name="exclusiveOr">The exclusive or.</param>
      public override void Visit(IExclusiveOr exclusiveOr) {
        ExclusiveOr mutableExclusiveOr = new ExclusiveOr(exclusiveOr);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableExclusiveOr);
      }

      /// <summary>
      /// Visits the specified expression.
      /// </summary>
      /// <param name="expression">The expression.</param>
      public override void Visit(IExpression expression) {
        Debug.Assert(false); //Should never get here
      }

      /// <summary>
      /// Visits the specified expression statement.
      /// </summary>
      /// <param name="expressionStatement">The expression statement.</param>
      public override void Visit(IExpressionStatement expressionStatement) {
        ExpressionStatement mutableExpressionStatement = new ExpressionStatement(expressionStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableExpressionStatement);
      }

      /// <summary>
      /// Performs some computation with the given fill memory block statement.
      /// </summary>
      /// <param name="fillMemoryStatement"></param>
      public override void Visit(IFillMemoryStatement fillMemoryStatement) {
        FillMemoryStatement mutableFillMemoryStatement = new FillMemoryStatement(fillMemoryStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableFillMemoryStatement);
      }

      /// <summary>
      /// Visits the specified for each statement.
      /// </summary>
      /// <param name="forEachStatement">For each statement.</param>
      public override void Visit(IForEachStatement forEachStatement) {
        ForEachStatement mutableForEachStatement = new ForEachStatement(forEachStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableForEachStatement);
      }

      /// <summary>
      /// Visits the specified for statement.
      /// </summary>
      /// <param name="forStatement">For statement.</param>
      public override void Visit(IForStatement forStatement) {
        ForStatement mutableForStatement = new ForStatement(forStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableForStatement);
      }

      /// <summary>
      /// Visits the specified goto statement.
      /// </summary>
      /// <param name="gotoStatement">The goto statement.</param>
      public override void Visit(IGotoStatement gotoStatement) {
        GotoStatement mutableGotoStatement = new GotoStatement(gotoStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableGotoStatement);
      }

      /// <summary>
      /// Visits the specified goto switch case statement.
      /// </summary>
      /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
      public override void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        GotoSwitchCaseStatement mutableGotoSwitchCaseStatement = new GotoSwitchCaseStatement(gotoSwitchCaseStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableGotoSwitchCaseStatement);
      }

      /// <summary>
      /// Visits the specified get type of typed reference.
      /// </summary>
      /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
      public override void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        GetTypeOfTypedReference mutableGetTypeOfTypedReference = new GetTypeOfTypedReference(getTypeOfTypedReference);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableGetTypeOfTypedReference);
      }

      /// <summary>
      /// Visits the specified get value of typed reference.
      /// </summary>
      /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
      public override void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        GetValueOfTypedReference mutableGetValueOfTypedReference = new GetValueOfTypedReference(getValueOfTypedReference);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableGetValueOfTypedReference);
      }

      /// <summary>
      /// Visits the specified greater than.
      /// </summary>
      /// <param name="greaterThan">The greater than.</param>
      public override void Visit(IGreaterThan greaterThan) {
        GreaterThan mutableGreaterThan = new GreaterThan(greaterThan);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableGreaterThan);
      }

      /// <summary>
      /// Visits the specified greater than or equal.
      /// </summary>
      /// <param name="greaterThanOrEqual">The greater than or equal.</param>
      public override void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        GreaterThanOrEqual mutableGreaterThanOrEqual = new GreaterThanOrEqual(greaterThanOrEqual);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableGreaterThanOrEqual);
      }

      /// <summary>
      /// Visits the specified labeled statement.
      /// </summary>
      /// <param name="labeledStatement">The labeled statement.</param>
      public override void Visit(ILabeledStatement labeledStatement) {
        LabeledStatement mutableLabeledStatement = new LabeledStatement(labeledStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableLabeledStatement);
      }

      /// <summary>
      /// Visits the specified left shift.
      /// </summary>
      /// <param name="leftShift">The left shift.</param>
      public override void Visit(ILeftShift leftShift) {
        LeftShift mutableLeftShift = new LeftShift(leftShift);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableLeftShift);
      }

      /// <summary>
      /// Visits the specified less than.
      /// </summary>
      /// <param name="lessThan">The less than.</param>
      public override void Visit(ILessThan lessThan) {
        LessThan mutableLessThan = new LessThan(lessThan);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableLessThan);
      }

      /// <summary>
      /// Visits the specified less than or equal.
      /// </summary>
      /// <param name="lessThanOrEqual">The less than or equal.</param>
      public override void Visit(ILessThanOrEqual lessThanOrEqual) {
        LessThanOrEqual mutableLessThanOrEqual = new LessThanOrEqual(lessThanOrEqual);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableLessThanOrEqual);
      }

      /// <summary>
      /// Visits the specified local declaration statement.
      /// </summary>
      /// <param name="localDeclarationStatement">The local declaration statement.</param>
      public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        LocalDeclarationStatement mutableLocalDeclarationStatement = new LocalDeclarationStatement(localDeclarationStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableLocalDeclarationStatement);
      }

      /// <summary>
      /// Visits the specified lock statement.
      /// </summary>
      /// <param name="lockStatement">The lock statement.</param>
      public override void Visit(ILockStatement lockStatement) {
        LockStatement mutableLockStatement = new LockStatement(lockStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableLockStatement);
      }

      /// <summary>
      /// Visits the specified logical not.
      /// </summary>
      /// <param name="logicalNot">The logical not.</param>
      public override void Visit(ILogicalNot logicalNot) {
        LogicalNot mutableLogicalNot = new LogicalNot(logicalNot);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableLogicalNot);
      }

      /// <summary>
      /// Visits the specified make typed reference.
      /// </summary>
      /// <param name="makeTypedReference">The make typed reference.</param>
      public override void Visit(IMakeTypedReference makeTypedReference) {
        MakeTypedReference mutableMakeTypedReference = new MakeTypedReference(makeTypedReference);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableMakeTypedReference);
      }

      /// <summary>
      /// Visits the specified method call.
      /// </summary>
      /// <param name="methodCall">The method call.</param>
      public override void Visit(IMethodCall methodCall) {
        MethodCall mutableMethodCall = new MethodCall(methodCall);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableMethodCall);
      }

      /// <summary>
      /// Visits the specified modulus.
      /// </summary>
      /// <param name="modulus">The modulus.</param>
      public override void Visit(IModulus modulus) {
        Modulus mutableModulus = new Modulus(modulus);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableModulus);
      }

      /// <summary>
      /// Visits the specified multiplication.
      /// </summary>
      /// <param name="multiplication">The multiplication.</param>
      public override void Visit(IMultiplication multiplication) {
        Multiplication mutableMultiplication = new Multiplication(multiplication);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableMultiplication);
      }

      /// <summary>
      /// Visits the specified named argument.
      /// </summary>
      /// <param name="namedArgument">The named argument.</param>
      public override void Visit(INamedArgument namedArgument) {
        NamedArgument mutableNamedArgument = new NamedArgument(namedArgument);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableNamedArgument);
      }

      /// <summary>
      /// Visits the specified not equality.
      /// </summary>
      /// <param name="notEquality">The not equality.</param>
      public override void Visit(INotEquality notEquality) {
        NotEquality mutableNotEquality = new NotEquality(notEquality);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableNotEquality);
      }

      /// <summary>
      /// Visits the specified old value.
      /// </summary>
      /// <param name="oldValue">The old value.</param>
      public override void Visit(IOldValue oldValue) {
        OldValue mutableOldValue = new OldValue(oldValue);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableOldValue);
      }

      /// <summary>
      /// Visits the specified ones complement.
      /// </summary>
      /// <param name="onesComplement">The ones complement.</param>
      public override void Visit(IOnesComplement onesComplement) {
        OnesComplement mutableOnesComplement = new OnesComplement(onesComplement);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableOnesComplement);
      }

      /// <summary>
      /// Visits the specified out argument.
      /// </summary>
      /// <param name="outArgument">The out argument.</param>
      public override void Visit(IOutArgument outArgument) {
        OutArgument mutableOutArgument = new OutArgument(outArgument);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableOutArgument);
      }

      /// <summary>
      /// Visits the specified pointer call.
      /// </summary>
      /// <param name="pointerCall">The pointer call.</param>
      public override void Visit(IPointerCall pointerCall) {
        PointerCall mutablePointerCall = new PointerCall(pointerCall);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutablePointerCall);
      }

      /// <summary>
      /// Performs some computation with the given pop value expression.
      /// </summary>
      public override void Visit(IPopValue popValue) {
        PopValue mutablePopValue = new PopValue(popValue);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutablePopValue);
      }

      /// <summary>
      /// Performs some computation with the given push statement.
      /// </summary>
      public override void Visit(IPushStatement pushStatement) {
        PushStatement mutablePushStatement = new PushStatement(pushStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutablePushStatement);
      }

      /// <summary>
      /// Visits the specified ref argument.
      /// </summary>
      /// <param name="refArgument">The ref argument.</param>
      public override void Visit(IRefArgument refArgument) {
        RefArgument mutableRefArgument = new RefArgument(refArgument);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableRefArgument);
      }

      /// <summary>
      /// Visits the specified resource use statement.
      /// </summary>
      /// <param name="resourceUseStatement">The resource use statement.</param>
      public override void Visit(IResourceUseStatement resourceUseStatement) {
        ResourceUseStatement mutableResourceUseStatement = new ResourceUseStatement(resourceUseStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableResourceUseStatement);
      }

      /// <summary>
      /// Visits the specified return value.
      /// </summary>
      /// <param name="returnValue">The return value.</param>
      public override void Visit(IReturnValue returnValue) {
        ReturnValue mutableReturnValue = new ReturnValue(returnValue);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableReturnValue);
      }

      /// <summary>
      /// Visits the specified rethrow statement.
      /// </summary>
      /// <param name="rethrowStatement">The rethrow statement.</param>
      public override void Visit(IRethrowStatement rethrowStatement) {
        RethrowStatement mutableRethrowStatement = new RethrowStatement(rethrowStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableRethrowStatement);
      }

      /// <summary>
      /// Visits the specified return statement.
      /// </summary>
      /// <param name="returnStatement">The return statement.</param>
      public override void Visit(IReturnStatement returnStatement) {
        ReturnStatement mutableReturnStatement = new ReturnStatement(returnStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableReturnStatement);
      }

      /// <summary>
      /// Visits the specified right shift.
      /// </summary>
      /// <param name="rightShift">The right shift.</param>
      public override void Visit(IRightShift rightShift) {
        RightShift mutableRightShift = new RightShift(rightShift);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableRightShift);
      }

      /// <summary>
      /// Visits the specified runtime argument handle expression.
      /// </summary>
      /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
      public override void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        RuntimeArgumentHandleExpression mutableRuntimeArgumentHandleExpression = new RuntimeArgumentHandleExpression(runtimeArgumentHandleExpression);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableRuntimeArgumentHandleExpression);
      }

      /// <summary>
      /// Visits the specified size of.
      /// </summary>
      /// <param name="sizeOf">The size of.</param>
      public override void Visit(ISizeOf sizeOf) {
        SizeOf mutableSizeOf = new SizeOf(sizeOf);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableSizeOf);
      }

      /// <summary>
      /// Visits the specified stack array create.
      /// </summary>
      /// <param name="stackArrayCreate">The stack array create.</param>
      public override void Visit(IStackArrayCreate stackArrayCreate) {
        StackArrayCreate mutableStackArrayCreate = new StackArrayCreate(stackArrayCreate);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableStackArrayCreate);
      }

      /// <summary>
      /// Visits the specified subtraction.
      /// </summary>
      /// <param name="subtraction">The subtraction.</param>
      public override void Visit(ISubtraction subtraction) {
        Subtraction mutableSubtraction = new Subtraction(subtraction);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableSubtraction);
      }

      /// <summary>
      /// Visits the specified switch statement.
      /// </summary>
      /// <param name="switchStatement">The switch statement.</param>
      public override void Visit(ISwitchStatement switchStatement) {
        SwitchStatement mutableSwitchStatement = new SwitchStatement(switchStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableSwitchStatement);
      }

      /// <summary>
      /// Visits the specified target expression.
      /// </summary>
      /// <param name="targetExpression">The target expression.</param>
      public override void Visit(ITargetExpression targetExpression) {
        TargetExpression mutableTargetExpression = new TargetExpression(targetExpression);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableTargetExpression);
      }

      /// <summary>
      /// Visits the specified this reference.
      /// </summary>
      /// <param name="thisReference">The this reference.</param>
      public override void Visit(IThisReference thisReference) {
        ThisReference mutableThisReference = new ThisReference(thisReference);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableThisReference);
      }

      /// <summary>
      /// Visits the specified throw statement.
      /// </summary>
      /// <param name="throwStatement">The throw statement.</param>
      public override void Visit(IThrowStatement throwStatement) {
        ThrowStatement mutableThrowStatement = new ThrowStatement(throwStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableThrowStatement);
      }

      /// <summary>
      /// Visits the specified try catch filter finally statement.
      /// </summary>
      /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
      public override void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        TryCatchFinallyStatement mutableTryCatchFinallyStatement = new TryCatchFinallyStatement(tryCatchFilterFinallyStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableTryCatchFinallyStatement);
      }

      /// <summary>
      /// Visits the specified token of.
      /// </summary>
      /// <param name="tokenOf">The token of.</param>
      public override void Visit(ITokenOf tokenOf) {
        TokenOf mutableTokenOf = new TokenOf(tokenOf);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableTokenOf);
      }

      /// <summary>
      /// Visits the specified type of.
      /// </summary>
      /// <param name="typeOf">The type of.</param>
      public override void Visit(ITypeOf typeOf) {
        TypeOf mutableTypeOf = new TypeOf(typeOf);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableTypeOf);
      }

      /// <summary>
      /// Visits the specified unary negation.
      /// </summary>
      /// <param name="unaryNegation">The unary negation.</param>
      public override void Visit(IUnaryNegation unaryNegation) {
        UnaryNegation mutableUnaryNegation = new UnaryNegation(unaryNegation);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableUnaryNegation);
      }

      /// <summary>
      /// Visits the specified unary plus.
      /// </summary>
      /// <param name="unaryPlus">The unary plus.</param>
      public override void Visit(IUnaryPlus unaryPlus) {
        UnaryPlus mutableUnaryPlus = new UnaryPlus(unaryPlus);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableUnaryPlus);
      }

      /// <summary>
      /// Visits the specified vector length.
      /// </summary>
      /// <param name="vectorLength">Length of the vector.</param>
      public override void Visit(IVectorLength vectorLength) {
        VectorLength mutableVectorLength = new VectorLength(vectorLength);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableVectorLength);
      }

      /// <summary>
      /// Visits the specified while do statement.
      /// </summary>
      /// <param name="whileDoStatement">The while do statement.</param>
      public override void Visit(IWhileDoStatement whileDoStatement) {
        WhileDoStatement mutableWhileDoStatement = new WhileDoStatement(whileDoStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableWhileDoStatement);
      }

      /// <summary>
      /// Visits the specified yield break statement.
      /// </summary>
      /// <param name="yieldBreakStatement">The yield break statement.</param>
      public override void Visit(IYieldBreakStatement yieldBreakStatement) {
        YieldBreakStatement mutableYieldBreakStatement = new YieldBreakStatement(yieldBreakStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableYieldBreakStatement);
      }

      /// <summary>
      /// Visits the specified yield return statement.
      /// </summary>
      /// <param name="yieldReturnStatement">The yield return statement.</param>
      public override void Visit(IYieldReturnStatement yieldReturnStatement) {
        YieldReturnStatement mutableYieldReturnStatement = new YieldReturnStatement(yieldReturnStatement);
        this.resultStatement = this.myCodeCopier.DeepCopy(mutableYieldReturnStatement);
      }
      #endregion overriding implementations of ICodeVisitor Members
    }
  }

}

namespace Microsoft.Cci.MutableCodeModel.Contracts {
  /// <summary>
  /// 
  /// </summary>
  public class CodeAndContractShallowCopier : CodeShallowCopier {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    public CodeAndContractShallowCopier(IMetadataHost targetHost)
      : base(targetHost) {
      this.dispatchingVisitor = new ContractElementDispatcher() { copier = this };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    /// <param name="targetUnit">The unit of metadata into which copies made by this copier will be inserted.</param>
    public CodeAndContractShallowCopier(IMetadataHost targetHost, IUnit targetUnit)
      : base(targetHost, targetUnit) {
      this.dispatchingVisitor = new ContractElementDispatcher() { copier = this };
    }

    ContractElementDispatcher dispatchingVisitor;
    class ContractElementDispatcher : CodeVisitor, ICodeAndContractVisitor {

      internal object result;
      internal CodeAndContractShallowCopier copier;

      public void Visit(ILoopInvariant loopInvariant) {
        this.result = this.copier.Copy(loopInvariant);
      }

      public void Visit(IPostcondition postCondition) {
        this.result = this.copier.Copy(postCondition);
      }

      public void Visit(IPrecondition precondition) {
        this.result = this.copier.Copy(precondition);
      }

      public void Visit(ITypeInvariant typeInvariant) {
        this.result = this.copier.Copy(typeInvariant);
      }

      public void Visit(ILoopContract loopContract) {
        Contract.Assume(false);
      }

      public void Visit(IMethodContract methodContract) {
        Contract.Assume(false);
      }

      public void Visit(IThrownException thrownException) {
        Contract.Assume(false);
      }

      public void Visit(ITypeContract typeContract) {
        Contract.Assume(false);
      }

    }

    /// <summary>
    /// Makes a shallow copy of the given contract element.
    /// </summary>
    /// <param name="contractElement"></param>
    public virtual ContractElement Copy(IContractElement contractElement) {
      Contract.Requires(contractElement != null);
      Contract.Ensures(Contract.Result<ContractElement>() != null);

      contractElement.Dispatch(this.dispatchingVisitor);
      return (ContractElement)this.dispatchingVisitor.result;
    }

    /// <summary>
    /// Makes a shallow copy of the given loop contract.
    /// </summary>
    public virtual LoopContract Copy(ILoopContract loopContract) {
      Contract.Requires(loopContract != null);
      Contract.Ensures(Contract.Result<LoopContract>() != null);

      return new LoopContract(loopContract);
    }

    /// <summary>
    /// Makes a shallow copy of the given loop invariant.
    /// </summary>
    public virtual LoopInvariant Copy(ILoopInvariant loopInvariant) {
      Contract.Requires(loopInvariant != null);
      Contract.Ensures(Contract.Result<LoopInvariant>() != null);

      return new LoopInvariant(loopInvariant);
    }

    /// <summary>
    /// Makes a shallow copy of the given method contract.
    /// </summary>
    public virtual MethodContract Copy(IMethodContract methodContract) {
      Contract.Requires(methodContract != null);
      Contract.Ensures(Contract.Result<MethodContract>() != null);

      return new MethodContract(methodContract);
    }

    /// <summary>
    /// Makes a shallow copy of the given postCondition.
    /// </summary>
    public virtual Postcondition Copy(IPostcondition postCondition) {
      Contract.Requires(postCondition != null);
      Contract.Ensures(Contract.Result<Postcondition>() != null);

      return new Postcondition(postCondition);
    }

    /// <summary>
    /// Makes a shallow copy of the given precondition.
    /// </summary>
    public virtual Precondition Copy(IPrecondition precondition) {
      Contract.Requires(precondition != null);
      Contract.Ensures(Contract.Result<Precondition>() != null);

      return new Precondition(precondition);
    }

    /// <summary>
    /// Makes a shallow copy of the given thrown exception.
    /// </summary>
    public virtual ThrownException Copy(IThrownException thrownException) {
      Contract.Requires(thrownException != null);
      Contract.Ensures(Contract.Result<ThrownException>() != null);

      return new ThrownException(thrownException);
    }

    /// <summary>
    /// Makes a shallow copy of the given type contract.
    /// </summary>
    public virtual TypeContract Copy(ITypeContract typeContract) {
      Contract.Requires(typeContract != null);
      Contract.Ensures(Contract.Result<TypeContract>() != null);

      return new TypeContract(typeContract);
    }

    /// <summary>
    /// Makes a shallow copy of the given type invariant.
    /// </summary>
    public virtual TypeInvariant Copy(ITypeInvariant typeInvariant) {
      Contract.Requires(typeInvariant != null);
      Contract.Ensures(Contract.Result<TypeInvariant>() != null);

      return new TypeInvariant(typeInvariant);
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class CodeAndContractDeepCopier : CodeDeepCopier {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    public CodeAndContractDeepCopier(IMetadataHost targetHost)
      : this(targetHost, new CodeAndContractShallowCopier(targetHost)) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetHost">An object representing the application that will host the copies made by this copier.</param>
    /// <param name="targetUnit">The unit of metadata into which copies made by this copier will be inserted.</param>
    public CodeAndContractDeepCopier(IMetadataHost targetHost, IUnit targetUnit)
      : this(targetHost, new CodeAndContractShallowCopier(targetHost, targetUnit)) {
    }

    private CodeAndContractDeepCopier(IMetadataHost targetHost, CodeAndContractShallowCopier shallowCopier)
      : base(targetHost, shallowCopier) {
      this.shallowCopier = shallowCopier;
    }

    ContractElementDispatcher dispatcher;
    private ContractElementDispatcher Dispatcher {
      get {
        if (this.dispatcher == null)
          this.dispatcher = new ContractElementDispatcher() { copier = this };
        return this.dispatcher;
      }
    }
    class ContractElementDispatcher : CodeVisitor, ICodeAndContractVisitor {

      internal object result;
      internal CodeAndContractDeepCopier copier;

      public void Visit(ILoopInvariant loopInvariant) {
        this.result = this.copier.Copy(loopInvariant);
      }

      public void Visit(IPostcondition postCondition) {
        this.result = this.copier.Copy(postCondition);
      }

      public void Visit(IPrecondition precondition) {
        this.result = this.copier.Copy(precondition);
      }

      public void Visit(ITypeInvariant typeInvariant) {
        this.result = this.copier.Copy(typeInvariant);
      }

      public void Visit(ILoopContract loopContract) {
        Contract.Assume(false);
      }

      public void Visit(IMethodContract methodContract) {
        Contract.Assume(false);
      }

      public void Visit(IThrownException thrownException) {
        Contract.Assume(false);
      }

      public void Visit(ITypeContract typeContract) {
        Contract.Assume(false);
      }

    }

    CodeAndContractShallowCopier shallowCopier;

    /// <summary>
    /// Called from the type specific copy method to copy the common part of all contract elments.
    /// </summary>
    /// <param name="contractElement"></param>
    private void Copy(ContractElement contractElement) {
      Contract.Requires(contractElement != null);

      contractElement.Condition = this.Copy(contractElement.Condition);
      if (contractElement.Description != null)
        contractElement.Description = this.Copy(contractElement.Description);
    }

    /// <summary>
    /// Makes a deep copy of the given list of addressable expressions.
    /// </summary>
    private List<IAddressableExpression> Copy(List<IAddressableExpression> addressableExpressions) {
      if (addressableExpressions == null) return null;
      for (int i = 0, n = addressableExpressions.Count; i < n; i++)
        addressableExpressions[i] = this.Copy(addressableExpressions[i]);
      return addressableExpressions;
    }

    /// <summary>
    /// Makes a deep copy of the given list of fields.
    /// </summary>
    private List<IFieldDefinition> Copy(List<IFieldDefinition> fields) {
      if (fields == null) return null;
      for (int i = 0, n = fields.Count; i < n; i++)
        fields[i] = this.Copy(fields[i]);
      return fields;
    }

    /// <summary>
    /// Makes a deep copy of the given list of trigger expressions.
    /// </summary>
    private IEnumerable<IEnumerable<IExpression>> Copy(IEnumerable<IEnumerable<IExpression>> triggers) {
      var result = new List<IEnumerable<IExpression>>(triggers);
      for (int i = 0, n = result.Count; i < n; i++) {
        var list = new List<IExpression>(result[i]);
        result[i] = this.Copy(list).AsReadOnly();
      }
      return result.AsReadOnly();
    }

    /// <summary>
    /// Makes a deep copy of the given list of loop invariants.
    /// </summary>
    private List<ILoopInvariant> Copy(List<ILoopInvariant> loopInvariants) {
      if (loopInvariants == null) return null;
      for (int i = 0, n = loopInvariants.Count; i < n; i++)
        loopInvariants[i] = this.Copy(loopInvariants[i]);
      return loopInvariants;
    }

    /// <summary>
    /// Makes a deep copy of the given list of method definitions.
    /// </summary>
    private List<IMethodDefinition> Copy(List<IMethodDefinition> methods) {
      if (methods == null) return null;
      for (int i = 0, n = methods.Count; i < n; i++)
        methods[i] = this.Copy(methods[i]);
      return methods;
    }

    /// <summary>
    /// Makes a deep copy of the given list of post conditions.
    /// </summary>
    private List<IPostcondition> Copy(List<IPostcondition> postConditions) {
      if (postConditions == null) return null;
      for (int i = 0, n = postConditions.Count; i < n; i++)
        postConditions[i] = this.Copy(postConditions[i]);
      return postConditions;
    }

    /// <summary>
    /// Makes a deep copy of the given list of pre conditions.
    /// </summary>
    private List<IPrecondition> Copy(List<IPrecondition> preconditions) {
      if (preconditions == null) return null;
      for (int i = 0, n = preconditions.Count; i < n; i++)
        preconditions[i] = this.Copy(preconditions[i]);
      return preconditions;
    }

    /// <summary>
    /// Makes a deep copy of the given list of thrown exceptions.
    /// </summary>
    private List<IThrownException> Copy(List<IThrownException> thrownExceptions) {
      if (thrownExceptions == null) return null;
      for (int i = 0, n = thrownExceptions.Count; i < n; i++)
        thrownExceptions[i] = this.Copy(thrownExceptions[i]);
      return thrownExceptions;
    }

    /// <summary>
    /// Makes a deep copy of the given list of addressable expressions.
    /// </summary>
    private List<ITypeInvariant> Copy(List<ITypeInvariant> typeInvariants) {
      if (typeInvariants == null) return null;
      for (int i = 0, n = typeInvariants.Count; i < n; i++)
        typeInvariants[i] = this.Copy(typeInvariants[i]);
      return typeInvariants;
    }

    /// <summary>
    /// Makes a deep copy of the given contract element.
    /// </summary>
    /// <param name="contractElement"></param>
    public ContractElement Copy(IContractElement contractElement) {
      Contract.Requires(contractElement != null);
      Contract.Ensures(Contract.Result<ContractElement>() != null);

      contractElement.Dispatch(this.Dispatcher);
      return (ContractElement)this.Dispatcher.result;
    }

    /// <summary>
    /// Makes a deep copy of the given loop contract.
    /// </summary>
    public LoopContract Copy(ILoopContract loopContract) {
      Contract.Requires(loopContract != null);
      Contract.Ensures(Contract.Result<LoopContract>() != null);

      var mutableCopy = this.shallowCopier.Copy(loopContract);
      mutableCopy.Invariants = this.Copy(mutableCopy.Invariants);
      mutableCopy.Variants = this.Copy(mutableCopy.Variants);
      mutableCopy.Writes = this.Copy(mutableCopy.Writes);
      return mutableCopy;
    }

    /// <summary>
    /// Makes a deep copy of the given loop invariant.
    /// </summary>
    public LoopInvariant Copy(ILoopInvariant loopInvariant) {
      Contract.Requires(loopInvariant != null);
      Contract.Ensures(Contract.Result<LoopInvariant>() != null);

      var mutableCopy = this.shallowCopier.Copy(loopInvariant);
      this.Copy((ContractElement)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Makes a deep copy of the given method contract.
    /// </summary>
    public MethodContract Copy(IMethodContract methodContract) {
      Contract.Requires(methodContract != null);
      Contract.Ensures(Contract.Result<MethodContract>() != null);

      var mutableCopy = this.shallowCopier.Copy(methodContract);
      mutableCopy.Allocates = this.Copy(mutableCopy.Allocates);
      mutableCopy.Frees = this.Copy(mutableCopy.Frees);
      mutableCopy.ModifiedVariables = this.Copy(mutableCopy.ModifiedVariables);
      mutableCopy.Postconditions = this.Copy(mutableCopy.Postconditions);
      mutableCopy.Preconditions = this.Copy(mutableCopy.Preconditions);
      mutableCopy.Reads = this.Copy(mutableCopy.Reads);
      mutableCopy.ThrownExceptions = this.Copy(mutableCopy.ThrownExceptions);
      mutableCopy.Writes = this.Copy(mutableCopy.Writes);
      return mutableCopy;
    }

    /// <summary>
    /// Makes a deep copy of the given postCondition.
    /// </summary>
    public Postcondition Copy(IPostcondition postCondition) {
      Contract.Requires(postCondition != null);
      Contract.Ensures(Contract.Result<Postcondition>() != null);

      var mutableCopy = this.shallowCopier.Copy(postCondition);
      this.Copy((ContractElement)mutableCopy);
      return mutableCopy;
    }

    /// <summary>
    /// Makes a deep copy of the given precondition.
    /// </summary>
    public Precondition Copy(IPrecondition precondition) {
      Contract.Requires(precondition != null);
      Contract.Ensures(Contract.Result<Precondition>() != null);

      var mutableCopy = this.shallowCopier.Copy(precondition);
      this.Copy((ContractElement)mutableCopy);
      if (mutableCopy.ExceptionToThrow != null)
        mutableCopy.ExceptionToThrow = this.Copy(mutableCopy.ExceptionToThrow);
      return mutableCopy;
    }

    //TODO: copy statement contract

    /// <summary>
    /// Makes a deep copy of the given thrown exception.
    /// </summary>
    public ThrownException Copy(IThrownException thrownException) {
      Contract.Requires(thrownException != null);
      Contract.Ensures(Contract.Result<ThrownException>() != null);

      var mutableCopy = this.shallowCopier.Copy(thrownException);
      mutableCopy.ExceptionType = this.Copy(mutableCopy.ExceptionType);
      mutableCopy.Postcondition = this.Copy(mutableCopy.Postcondition);
      return mutableCopy;
    }

    /// <summary>
    /// Makes a deep copy of the given type contract.
    /// </summary>
    public TypeContract Copy(ITypeContract typeContract) {
      Contract.Requires(typeContract != null);
      Contract.Ensures(Contract.Result<TypeContract>() != null);

      var mutableCopy = this.shallowCopier.Copy(typeContract);
      mutableCopy.ContractFields = this.Copy(mutableCopy.ContractFields);
      mutableCopy.ContractMethods = this.Copy(mutableCopy.ContractMethods);
      mutableCopy.Invariants = this.Copy(mutableCopy.Invariants);
      return mutableCopy;
    }

    /// <summary>
    /// Makes a deep copy of the given type invariant.
    /// </summary>
    public TypeInvariant Copy(ITypeInvariant typeInvariant) {
      Contract.Requires(typeInvariant != null);
      Contract.Ensures(Contract.Result<TypeInvariant>() != null);

      var mutableCopy = this.shallowCopier.Copy(typeInvariant);
      this.Copy((ContractElement)mutableCopy);
      return mutableCopy;
    }
  }

  /// <summary>
  /// Provides copy of a method contract, method body, a statement, or an expression, in which the references to the nodes
  /// inside a cone is replaced. The cone is defined using the parent class. 
  /// </summary>
  [Obsolete("Please use CodeAndContractDeepCopier or CodeAndContractShallowCopier")]
  public class CodeAndContractCopier : CodeCopier {
    /// <summary>
    /// Provides copy of a method contract, method body, a statement, or an expression, in which the references to the nodes
    /// inside a cone is replaced. The cone is defined using the parent class. 
    /// </summary>
    public CodeAndContractCopier(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, sourceLocationProvider) {
    }
    /// <summary>
    /// Provides copy of a method contract, method body, a statement, or an expression, in which the references to the nodes
    /// inside a cone is replaced. The cone is defined using the parent class. 
    /// </summary>
    public CodeAndContractCopier(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IDefinition rootOfCone, out List<INamedTypeDefinition> newTypes)
      : base(host, sourceLocationProvider, rootOfCone, out newTypes) {
    }

    #region GetMutableCopy methods

    /// <summary>
    /// Get the mutable copy of a loop invariant.
    /// </summary>
    /// <param name="loopInvariant"></param>
    /// <returns></returns>
    public virtual LoopInvariant GetMutableCopy(ILoopInvariant loopInvariant) {
      object cachedValue;
      if (this.cache.TryGetValue(loopInvariant, out cachedValue))
        return (LoopInvariant)cachedValue;
      var result = new LoopInvariant(loopInvariant);
      // Probably not necessary, no two loop invariants are shared. 
      this.cache.Add(loopInvariant, result);
      this.cache.Add(result, result);
      return result;
    }

    /// <summary>
    /// Get the mutable copy of a postcondition.
    /// </summary>
    /// <param name="postcondition"></param>
    /// <returns></returns>
    public virtual Postcondition GetMutableCopy(IPostcondition postcondition) {
      object cachedValue;
      if (this.cache.TryGetValue(postcondition, out cachedValue))
        return (Postcondition)cachedValue;
      var result = new Postcondition(postcondition);
      // Probably not necessary, no two postconditions are shared. 
      this.cache.Add(postcondition, result);
      this.cache.Add(result, result);
      return result;
    }

    /// <summary>
    /// Get the mutable copy of a precondition.
    /// </summary>
    /// <param name="precondition"></param>
    /// <returns></returns>
    public virtual Precondition GetMutableCopy(IPrecondition precondition) {
      object cachedValue;
      if (this.cache.TryGetValue(precondition, out cachedValue))
        return (Precondition)cachedValue;
      var result = new Precondition(precondition);
      // Probably not necessary, no two postconditions are shared. 
      this.cache.Add(precondition, result);
      this.cache.Add(result, result);
      return result;
    }

    /// <summary>
    /// Get the mutable copy of a thrown exception.
    /// </summary>
    /// <param name="thrownException"></param>
    /// <returns></returns>
    public virtual ThrownException GetMutableCopy(IThrownException thrownException) {
      object cachedValue;
      if (this.cache.TryGetValue(thrownException, out cachedValue))
        return (ThrownException)cachedValue;
      var result = new ThrownException(thrownException);
      // Probably not necessary, no two thrown exceptions are shared. 
      this.cache.Add(thrownException, result);
      this.cache.Add(result, result);
      return result;
    }

    /// <summary>
    /// Get the mutable copy of a type invariant.
    /// </summary>
    /// <param name="typeInvariant"></param>
    /// <returns></returns>
    public virtual TypeInvariant GetMutableCopy(ITypeInvariant typeInvariant) {
      object cachedValue;
      if (this.cache.TryGetValue(typeInvariant, out cachedValue))
        return (TypeInvariant)cachedValue;
      var result = new TypeInvariant(typeInvariant);
      // Probably not necessary, no two thrown exceptions are shared. 
      this.cache.Add(typeInvariant, result);
      this.cache.Add(result, result);
      return result;
    }

    #endregion

    #region DeepCopy methods

    /// <summary>
    /// Returns a deep mutable copy of the given method contract.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references. For the purposes of this call, the
    /// table for interning is what is needed.</param>
    /// <param name="methodContract">The method contract to copy.</param>
    public static MethodContract DeepCopy(IMetadataHost host, IMethodContract methodContract) {
      return (MethodContract)new CodeAndContractCopier(host, null).Substitute(methodContract);
    }

    /// <summary>
    /// Returns a deep mutable copy of the given type contract.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references. For the purposes of this call, the
    /// table for interning is what is needed.</param>
    /// <param name="typeContract">The type contract to copy.</param>
    public static TypeContract DeepCopy(IMetadataHost host, ITypeContract typeContract) {
      return (TypeContract)new CodeAndContractCopier(host, null).Substitute(typeContract);
    }

    /// <summary>
    /// Visits the specified addressable expressions.
    /// </summary>
    /// <param name="addressableExpressions">The addressable expressions.</param>
    /// <returns></returns>
    protected virtual List<IAddressableExpression> DeepCopy(List<IAddressableExpression> addressableExpressions) {
      List<IAddressableExpression> newList = new List<IAddressableExpression>();
      foreach (var addressableExpression in addressableExpressions)
        newList.Add((IAddressableExpression)this.Substitute(addressableExpression));
      return newList;
    }

    /// <summary>
    /// Visits the specified loop contract.
    /// </summary>
    /// <param name="loopContract">The loop contract.</param>
    /// <returns></returns>
    protected virtual ILoopContract DeepCopy(LoopContract loopContract) {
      loopContract.Invariants = this.DeepCopy(loopContract.Invariants);
      loopContract.Writes = this.DeepCopy(loopContract.Writes);
      return loopContract;
    }

    /// <summary>
    /// Visits the specified loop invariants.
    /// </summary>
    /// <param name="loopInvariants">The loop invariants.</param>
    /// <returns></returns>
    protected virtual List<ILoopInvariant> DeepCopy(List<ILoopInvariant> loopInvariants) {
      List<ILoopInvariant> newList = new List<ILoopInvariant>();
      foreach (var loopInvariant in loopInvariants)
        newList.Add(this.DeepCopy(this.GetMutableCopy(loopInvariant)));
      return newList;
    }

    /// <summary>
    /// Visits the specified loop invariant.
    /// </summary>
    /// <param name="loopInvariant">The loop invariant.</param>
    /// <returns></returns>
    protected virtual ILoopInvariant DeepCopy(LoopInvariant loopInvariant) {
      loopInvariant.Condition = this.Substitute(loopInvariant.Condition);
      if (loopInvariant.Description != null)
        loopInvariant.Description = this.Substitute(loopInvariant.Description);
      return loopInvariant;
    }

    /// <summary>
    /// Visits the specified method contract.
    /// </summary>
    /// <param name="methodContract">The method contract.</param>
    /// <returns></returns>
    protected virtual IMethodContract DeepCopy(MethodContract methodContract) {
      methodContract.Allocates = this.DeepCopy(methodContract.Allocates);
      methodContract.Frees = this.DeepCopy(methodContract.Frees);
      methodContract.ModifiedVariables = this.DeepCopy(methodContract.ModifiedVariables);
      methodContract.Postconditions = this.DeepCopy(methodContract.Postconditions);
      methodContract.Preconditions = this.DeepCopy(methodContract.Preconditions);
      methodContract.Reads = this.DeepCopy(methodContract.Reads);
      methodContract.ThrownExceptions = this.DeepCopy(methodContract.ThrownExceptions);
      methodContract.Writes = this.DeepCopy(methodContract.Writes);
      return methodContract;
    }

    /// <summary>
    /// Visits the specified post conditions.
    /// </summary>
    /// <param name="postConditions">The post conditions.</param>
    /// <returns></returns>
    protected virtual List<IPostcondition> DeepCopy(List<IPostcondition> postConditions) {
      List<IPostcondition> newList = new List<IPostcondition>();
      foreach (var postcondition in postConditions)
        newList.Add(this.DeepCopy(this.GetMutableCopy(postcondition)));
      return newList;
    }

    /// <summary>
    /// Visits the specified post condition.
    /// </summary>
    /// <param name="postCondition">The post condition.</param>
    protected virtual IPostcondition DeepCopy(Postcondition postCondition) {
      postCondition.Condition = this.Substitute(postCondition.Condition);
      if (postCondition.Description != null)
        postCondition.Description = this.Substitute(postCondition.Description);
      return postCondition;
    }

    /// <summary>
    /// Visits the specified preconditions.
    /// </summary>
    /// <param name="preconditions">The preconditions.</param>
    protected virtual List<IPrecondition> DeepCopy(List<IPrecondition> preconditions) {
      List<IPrecondition> newList = new List<IPrecondition>();
      foreach (var precondition in preconditions)
        newList.Add(this.DeepCopy(this.GetMutableCopy(precondition)));
      return newList;
    }

    /// <summary>
    /// Visits the specified precondition.
    /// </summary>
    /// <param name="precondition">The precondition.</param>
    protected virtual IPrecondition DeepCopy(Precondition precondition) {
      precondition.Condition = this.Substitute(precondition.Condition);
      if (precondition.Description != null)
        precondition.Description = this.Substitute(precondition.Description);
      if (precondition.ExceptionToThrow != null)
        precondition.ExceptionToThrow = this.Substitute(precondition.ExceptionToThrow);
      return precondition;
    }

    /// <summary>
    /// Visits the specified thrown exceptions.
    /// </summary>
    /// <param name="thrownExceptions">The thrown exceptions.</param>
    protected virtual List<IThrownException> DeepCopy(List<IThrownException> thrownExceptions) {
      List<IThrownException> newList = new List<IThrownException>();
      foreach (var thrownException in thrownExceptions)
        newList.Add(this.DeepCopy(this.GetMutableCopy(thrownException)));
      return newList;
    }

    /// <summary>
    /// Visits the specified thrown exception.
    /// </summary>
    /// <param name="thrownException">The thrown exception.</param>
    protected virtual IThrownException DeepCopy(ThrownException thrownException) {
      thrownException.ExceptionType = this.Substitute(thrownException.ExceptionType);
      thrownException.Postcondition = this.Substitute(thrownException.Postcondition);
      return thrownException;
    }

    /// <summary>
    /// Visits the specified type contract.
    /// </summary>
    /// <param name="typeContract">The type contract.</param>
    protected virtual ITypeContract DeepCopy(TypeContract typeContract) {
      typeContract.ContractFields = this.DeepCopy(typeContract.ContractFields);
      typeContract.ContractMethods = this.DeepCopy(typeContract.ContractMethods);
      typeContract.Invariants = this.DeepCopy(typeContract.Invariants);
      return typeContract;
    }

    /// <summary>
    /// Visits the specified type invariants.
    /// </summary>
    /// <param name="typeInvariants">The type invariants.</param>
    protected virtual List<ITypeInvariant> DeepCopy(List<ITypeInvariant> typeInvariants) {
      List<ITypeInvariant> newList = new List<ITypeInvariant>();
      foreach (var typeInvariant in typeInvariants)
        newList.Add(this.DeepCopy(this.GetMutableCopy(typeInvariant)));
      return newList;
    }

    /// <summary>
    /// Visits the specified type invariant.
    /// </summary>
    /// <param name="typeInvariant">The type invariant.</param>
    protected virtual ITypeInvariant DeepCopy(TypeInvariant typeInvariant) {
      typeInvariant.Condition = this.Substitute(typeInvariant.Condition);
      if (typeInvariant.Description != null)
        typeInvariant.Description = this.Substitute(typeInvariant.Description);
      return typeInvariant;
    }

    #endregion

    #region Substitute methods

    /// <summary>
    /// Returns a deep copy of the <param name="precondition"/>.
    /// </summary>
    public virtual IPrecondition Substitute(IPrecondition precondition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(new Precondition(precondition));
    }

    /// <summary>
    /// Returns a deep copy of the <param name="postcondition"/>.
    /// </summary>
    public virtual IPostcondition Substitute(IPostcondition postcondition) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(new Postcondition(postcondition));
    }

    /// <summary>
    /// Returns a deep copy of the <param name="thrownException"/>.
    /// </summary>
    public virtual IThrownException Substitute(IThrownException thrownException) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(new ThrownException(thrownException));
    }

    /// <summary>
    /// Returns a deep copy of the <param name="methodContract"/>.
    /// </summary>
    public virtual IMethodContract Substitute(IMethodContract methodContract) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(new MethodContract(methodContract));
    }

    /// <summary>
    /// Returns a deep copy of the <param name="typeInvariant"/>.
    /// </summary>
    public virtual ITypeInvariant Substitute(ITypeInvariant typeInvariant) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(new TypeInvariant(typeInvariant));
    }

    /// <summary>
    /// Returns a deep copy of the <param name="typeContract"/>.
    /// </summary>
    public virtual ITypeContract Substitute(ITypeContract typeContract) {
      this.coneAlreadyFixed = true;
      return this.DeepCopy(new TypeContract(typeContract));
    }

    #endregion

  }
}

