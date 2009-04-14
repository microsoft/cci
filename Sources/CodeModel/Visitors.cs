//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// Implemented by classes that visit nodes of object graphs via a double dispatch mechanism, usually performing some computation of a subset of the nodes in the graph.
  /// Contains a specialized Visit routine for each standard type of object defined in the code model. 
  /// </summary>
  public interface ICodeVisitor : IMetadataVisitor {

    /// <summary>
    /// Performs some computation with the given addition.
    /// </summary>
    void Visit(IAddition addition);
    /// <summary>
    /// Performs some computation with the given addressable expression.
    /// </summary>
    void Visit(IAddressableExpression addressableExpression);
    /// <summary>
    /// Performs some computation with the given address dereference expression.
    /// </summary>
    void Visit(IAddressDereference addressDereference);
    /// <summary>
    /// Performs some computation with the given AddressOf expression.
    /// </summary>
    void Visit(IAddressOf addressOf);
    /// <summary>
    /// Performs some computation with the given anonymous delegate expression.
    /// </summary>
    void Visit(IAnonymousDelegate anonymousDelegate);
    /// <summary>
    /// Performs some computation with the given array indexer expression.
    /// </summary>
    void Visit(IArrayIndexer arrayIndexer);
    /// <summary>
    /// Performs some computation with the given assert statement.
    /// </summary>
    void Visit(IAssertStatement assertStatement);
    /// <summary>
    /// Performs some computation with the given assignment expression.
    /// </summary>
    void Visit(IAssignment assignment);
    /// <summary>
    /// Performs some computation with the given assume statement.
    /// </summary>
    void Visit(IAssumeStatement assumeStatement);
    /// <summary>
    /// Performs some computation with the given base class reference expression.
    /// </summary>
    void Visit(IBaseClassReference baseClassReference);
    /// <summary>
    /// Performs some computation with the given bitwise and expression.
    /// </summary>
    void Visit(IBitwiseAnd bitwiseAnd);
    /// <summary>
    /// Performs some computation with the given bitwise or expression.
    /// </summary>
    void Visit(IBitwiseOr bitwiseOr);
    /// <summary>
    /// Performs some computation with the given block expression.
    /// </summary>
    void Visit(IBlockExpression blockExpression);
    /// <summary>
    /// Performs some computation with the given statement block.
    /// </summary>
    void Visit(IBlockStatement block);
    /// <summary>
    /// Performs some computation with the given break statement.
    /// </summary>
    void Visit(IBreakStatement breakStatement);
    /// <summary>
    /// Performs some computation with the given bound expression.
    /// </summary>
    void Visit(IBoundExpression boundExpression);
    /// <summary>
    /// Performs some computation with the cast-if-possible expression.
    /// </summary>
    void Visit(ICastIfPossible castIfPossible);
    /// <summary>
    /// Performs some computation with the given catch clause.
    /// </summary>
    void Visit(ICatchClause catchClause);
    /// <summary>
    /// Performs some computation with the given check-if-instance expression.
    /// </summary>
    void Visit(ICheckIfInstance checkIfInstance);
    /// <summary>
    /// Performs some computation with the given compile time constant.
    /// </summary>
    void Visit(ICompileTimeConstant constant);
    /// <summary>
    /// Performs some computation with the given conversion expression.
    /// </summary>
    void Visit(IConversion conversion);
    /// <summary>
    /// Performs some computation with the given conditional expression.
    /// </summary>
    void Visit(IConditional conditional);
    /// <summary>
    /// Performs some computation with the given conditional statement.
    /// </summary>
    void Visit(IConditionalStatement conditionalStatement);
    /// <summary>
    /// Performs some computation with the given continue statement.
    /// </summary>
    void Visit(IContinueStatement continueStatement);
    /// <summary>
    /// Performs some computation with the given array creation expression.
    /// </summary>
    void Visit(ICreateArray createArray);
    /// <summary>
    /// Performs some computation with the anonymous object creation expression.
    /// </summary>
    void Visit(ICreateDelegateInstance createDelegateInstance);
    /// <summary>
    /// Performs some computation with the given constructor call expression.
    /// </summary>
    void Visit(ICreateObjectInstance createObjectInstance);
    /// <summary>
    /// Performs some computation with the given debugger break statement.
    /// </summary>
    void Visit(IDebuggerBreakStatement debuggerBreakStatement);
    /// <summary>
    /// Performs some computation with the given defalut value expression.
    /// </summary>
    void Visit(IDefaultValue defaultValue);
    /// <summary>
    /// Performs some computation with the given division expression.
    /// </summary>
    void Visit(IDivision division);
    /// <summary>
    /// Performs some computation with the given do until statement.
    /// </summary>
    void Visit(IDoUntilStatement doUntilStatement);
    /// <summary>
    /// Performs some computation with the given empty statement.
    /// </summary>
    void Visit(IEmptyStatement emptyStatement);
    /// <summary>
    /// Performs some computation with the given equality expression.
    /// </summary>
    void Visit(IEquality equality);
    /// <summary>
    /// Performs some computation with the given exclusive or expression.
    /// </summary>
    void Visit(IExclusiveOr exclusiveOr);
    /// <summary>
    /// Performs some computation with the given expression.
    /// </summary>
    void Visit(IExpression expression);
    /// <summary>
    /// Performs some computation with the given expression statement.
    /// </summary>
    void Visit(IExpressionStatement expressionStatement);
    /// <summary>
    /// Performs some computation with the given foreach statement.
    /// </summary>
    void Visit(IForEachStatement forEachStatement);
    /// <summary>
    /// Performs some computation with the given for statement.
    /// </summary>
    void Visit(IForStatement forStatement);
    /// <summary>
    /// Performs some computation with the given goto statement.
    /// </summary>
    void Visit(IGotoStatement gotoStatement);
    /// <summary>
    /// Performs some computation with the given goto switch case statement.
    /// </summary>
    void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement);
    /// <summary>
    /// Performs some computation with the given get type of typed reference expression.
    /// </summary>
    void Visit(IGetTypeOfTypedReference getTypeOfTypedReference);
    /// <summary>
    /// Performs some computation with the given get value of typed reference expression.
    /// </summary>
    void Visit(IGetValueOfTypedReference getValueOfTypedReference);
    /// <summary>
    /// Performs some computation with the given greater-than expression.
    /// </summary>
    void Visit(IGreaterThan greaterThan);
    /// <summary>
    /// Performs some computation with the given greater-than-or-equal expression.
    /// </summary>
    void Visit(IGreaterThanOrEqual greaterThanOrEqual);
    /// <summary>
    /// Performs some computation with the given labeled statement.
    /// </summary>
    void Visit(ILabeledStatement labeledStatement);
    /// <summary>
    /// Performs some computation with the given left shift expression.
    /// </summary>
    void Visit(ILeftShift leftShift);
    /// <summary>
    /// Performs some computation with the given less-than expression.
    /// </summary>
    void Visit(ILessThan lessThan);
    /// <summary>
    /// Performs some computation with the given less-than-or-equal expression.
    /// </summary>
    void Visit(ILessThanOrEqual lessThanOrEqual);
    /// <summary>
    /// Performs some computation with the given local declaration statement.
    /// </summary>
    void Visit(ILocalDeclarationStatement localDeclarationStatement);
    /// <summary>
    /// Performs some computation with the given lock statement.
    /// </summary>
    void Visit(ILockStatement lockStatement);
    /// <summary>
    /// Performs some computation with the given logical not expression.
    /// </summary>
    void Visit(ILogicalNot logicalNot);
    /// <summary>
    /// Performs some computation with the given make typed reference expression.
    /// </summary>
    void Visit(IMakeTypedReference makeTypedReference);
    /// <summary>
    /// Performs some computation with the given method call.
    /// </summary>
    void Visit(IMethodCall methodCall);
    /// <summary>
    /// Performs some computation with the given modulus expression.
    /// </summary>
    void Visit(IModulus modulus);
    /// <summary>
    /// Performs some computation with the given multiplication expression.
    /// </summary>
    void Visit(IMultiplication multiplication);
    /// <summary>
    /// Performs some computation with the given named argument expression.
    /// </summary>
    void Visit(INamedArgument namedArgument);
    /// <summary>
    /// Performs some computation with the given not equality expression.
    /// </summary>
    void Visit(INotEquality notEquality);
    /// <summary>
    /// Performs some computation with the given old value expression.
    /// </summary>
    void Visit(IOldValue oldValue);
    /// <summary>
    /// Performs some computation with the given one's complement expression.
    /// </summary>
    void Visit(IOnesComplement onesComplement);
    /// <summary>
    /// Performs some computation with the given out argument expression.
    /// </summary>
    void Visit(IOutArgument outArgument);
    /// <summary>
    /// Performs some computation with the given pointer call.
    /// </summary>
    void Visit(IPointerCall pointerCall);
    /// <summary>
    /// Performs some computation with the given ref argument expression.
    /// </summary>
    void Visit(IRefArgument refArgument);
    /// <summary>
    /// Performs some computation with the given resource usage statement.
    /// </summary>
    void Visit(IResourceUseStatement resourceUseStatement);
    /// <summary>
    /// Performs some computation with the given return value expression.
    /// </summary>
    void Visit(IReturnValue returnValue);
    /// <summary>
    /// Performs some computation with the rethrow statement.
    /// </summary>
    void Visit(IRethrowStatement rethrowStatement);
    /// <summary>
    /// Performs some computation with the return statement.
    /// </summary>
    void Visit(IReturnStatement returnStatement);
    /// <summary>
    /// Performs some computation with the given right shift expression.
    /// </summary>
    void Visit(IRightShift rightShift);
    /// <summary>
    /// Performs some computation with the given runtime argument handle expression.
    /// </summary>
    void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression);
    /// <summary>
    /// Performs some computation with the given sizeof() expression.
    /// </summary>
    void Visit(ISizeOf sizeOf);
    /// <summary>
    /// Performs some computation with the given stack array create expression.
    /// </summary>
    void Visit(IStackArrayCreate stackArrayCreate);
    /// <summary>
    /// Performs some computation with the given subtraction expression.
    /// </summary>
    void Visit(ISubtraction subtraction);
    /// <summary>
    /// Performs some computation with the given switch case.
    /// </summary>
    void Visit(ISwitchCase switchCase);
    /// <summary>
    /// Performs some computation with the given switch statement.
    /// </summary>
    void Visit(ISwitchStatement switchStatement);
    /// <summary>
    /// Performs some computation with the given target expression.
    /// </summary>
    void Visit(ITargetExpression targetExpression);
    /// <summary>
    /// Performs some computation with the given this reference expression.
    /// </summary>
    void Visit(IThisReference thisReference);
    /// <summary>
    /// Performs some computation with the throw statement.
    /// </summary>
    void Visit(IThrowStatement throwStatement);
    /// <summary>
    /// Performs some computation with the try-catch-filter-finally statement.
    /// </summary>
    void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement);
    /// <summary>
    /// Performs some computation with the given tokenof() expression.
    /// </summary>
    void Visit(ITokenOf tokenOf);
    /// <summary>
    /// Performs some computation with the given typeof() expression.
    /// </summary>
    void Visit(ITypeOf typeOf);
    /// <summary>
    /// Performs some computation with the given unary negation expression.
    /// </summary>
    void Visit(IUnaryNegation unaryNegation);
    /// <summary>
    /// Performs some computation with the given unary plus expression.
    /// </summary>
    void Visit(IUnaryPlus unaryPlus);
    /// <summary>
    /// Performs some computation with the given vector length expression.
    /// </summary>
    void Visit(IVectorLength vectorLength);
    /// <summary>
    /// Performs some computation with the given while do statement.
    /// </summary>
    void Visit(IWhileDoStatement whileDoStatement);
    /// <summary>
    /// Performs some computation with the given yield break statement.
    /// </summary>
    void Visit(IYieldBreakStatement yieldBreakStatement);
    /// <summary>
    /// Performs some computation with the given yield return statement.
    /// </summary>
    void Visit(IYieldReturnStatement yieldReturnStatement);
  }

#pragma warning disable 1591
  /// <summary>
  /// A visitor base class that traverses the code model in depth first, left to right order.
  /// </summary>
  public class BaseCodeTraverser : BaseMetadataTraverser, ICodeVisitor {

    public BaseCodeTraverser() {
    }

    #region ICodeVisitor Members

    public virtual void Visit(IAddition addition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addition);
      this.Visit(addition.LeftOperand);
      this.Visit(addition.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IAddressableExpression addressableExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      object/*?*/ def = addressableExpression.Definition;
      IAddressDereference/*?*/ adr = def as IAddressDereference;
      if (adr != null)
        this.Visit(adr);
      else {
        IArrayIndexer/*?*/ indexer = def as IArrayIndexer;
        if (indexer != null) { this.Visit(indexer); return; }
      }
      if (addressableExpression.Instance != null)
        this.Visit(addressableExpression.Instance);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IAddressDereference addressDereference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addressDereference);
      this.Visit(addressDereference.Address);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IAddressOf addressOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addressOf);
      this.Visit(addressOf.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IAnonymousDelegate anonymousDelegate)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(anonymousDelegate);
      this.Visit(anonymousDelegate.Parameters);
      this.Visit(anonymousDelegate.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IAssertStatement assertStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assertStatement);
      this.Visit(assertStatement.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IAssignment> assignments)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IAssignment assignment in assignments) {
        this.Visit(assignment);
        if (this.stopTraversal) return;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    }

    public virtual void Visit(IAssignment assignment)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assignment);
      this.Visit(assignment.Target);
      this.Visit(assignment.Source);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IAssumeStatement assumeStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assumeStatement);
      this.Visit(assumeStatement.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IBaseClassReference baseClassReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IBitwiseAnd bitwiseAnd)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(bitwiseAnd);
      this.Visit(bitwiseAnd.LeftOperand);
      this.Visit(bitwiseAnd.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IBitwiseOr bitwiseOr)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(bitwiseOr);
      this.Visit(bitwiseOr.LeftOperand);
      this.Visit(bitwiseOr.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IBlockExpression blockExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(blockExpression);
      this.Visit(blockExpression.BlockStatement);
      this.Visit(blockExpression.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IBlockStatement block)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(block);
      this.Visit(block.Statements);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ICastIfPossible castIfPossible)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(castIfPossible);
      this.Visit(castIfPossible.TargetType);
      this.Visit(castIfPossible.ValueToCast);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<ICatchClause> catchClauses)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ICatchClause catchClause in catchClauses) {
        this.Visit(catchClause);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(ICatchClause catchClause)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(catchClause);
      if (catchClause.FilterCondition != null)
        this.Visit(catchClause.FilterCondition);
      this.Visit(catchClause.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ICheckIfInstance checkIfInstance)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(checkIfInstance);
      this.Visit(checkIfInstance.Operand);
      this.Visit(checkIfInstance.TypeToCheck);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ICompileTimeConstant constant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IConversion conversion)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(conversion);
      this.Visit(conversion.ValueToConvert);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IConditional conditional)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(conditional);
      this.Visit(conditional.Condition);
      this.Visit(conditional.ResultIfTrue);
      this.Visit(conditional.ResultIfFalse);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IConditionalStatement conditionalStatement)
      //^ ensures this.path.Count == old(this.path.Count);
   {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(conditionalStatement);
      this.Visit(conditionalStatement.Condition);
      this.Visit(conditionalStatement.TrueBranch);
      this.Visit(conditionalStatement.FalseBranch);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IContinueStatement continueStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(continueStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ICreateArray createArray)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createArray);
      this.Visit(createArray.Sizes);
      this.Visit(createArray.Initializers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ICreateObjectInstance createObjectInstance)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createObjectInstance);
      this.Visit(createObjectInstance.Arguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ICreateDelegateInstance createDelegateInstance)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createDelegateInstance);
      if (createDelegateInstance.Instance != null)
        this.Visit(createDelegateInstance.Instance);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IArrayIndexer arrayIndexer)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(arrayIndexer);
      this.Visit(arrayIndexer.IndexedObject);
      this.Visit(arrayIndexer.Indices);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given bound expression.
    /// </summary>
    public virtual void Visit(IBoundExpression boundExpression) {
      if (this.stopTraversal) return;
      this.path.Push(boundExpression);
      if (boundExpression.Instance != null)
        this.Visit(boundExpression.Instance);
      this.path.Pop();
    }

    public override void Visit(ICustomAttribute customAttribute)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(customAttribute);
      this.Visit(customAttribute.Arguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IDefaultValue defaultValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(defaultValue);
      this.Visit(defaultValue.DefaultValueType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IDebuggerBreakStatement debuggerBreakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(debuggerBreakStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IDivision division)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(division);
      this.Visit(division.LeftOperand);
      this.Visit(division.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IDoUntilStatement doUntilStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(doUntilStatement);
      this.Visit(doUntilStatement.Body);
      this.Visit(doUntilStatement.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEmptyStatement emptyStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(emptyStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEquality equality)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(equality);
      this.Visit(equality.LeftOperand);
      this.Visit(equality.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IExclusiveOr exclusiveOr)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(exclusiveOr);
      this.Visit(exclusiveOr.LeftOperand);
      this.Visit(exclusiveOr.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IExpression> expressions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IExpression expression in expressions) {
        this.Visit(expression);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IExpression expression) {
      if (this.stopTraversal) return;
      expression.Dispatch(this);
    }

    public virtual void Visit(IExpressionStatement expressionStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(expressionStatement);
      this.Visit(expressionStatement.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public override void Visit(IFieldDefinition fieldDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(fieldDefinition);
      this.Visit(fieldDefinition.Attributes);
      if (fieldDefinition.IsCompileTimeConstant)
        this.Visit((IMetadataExpression)fieldDefinition.CompileTimeValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IForEachStatement forEachStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(forEachStatement);
      this.Visit(forEachStatement.Collection);
      this.Visit(forEachStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IForStatement forStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(forStatement);
      this.Visit(forStatement.InitStatements);
      this.Visit(forStatement.Condition);
      this.Visit(forStatement.IncrementStatements);
      this.Visit(forStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IGetTypeOfTypedReference getTypeOfTypedReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(getTypeOfTypedReference);
      this.Visit(getTypeOfTypedReference.TypedReference);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IGetValueOfTypedReference getValueOfTypedReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(getValueOfTypedReference);
      this.Visit(getValueOfTypedReference.TypedReference);
      this.Visit(getValueOfTypedReference.TargetType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IGotoStatement gotoStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(gotoStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IGreaterThan greaterThan)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(greaterThan);
      this.Visit(greaterThan.LeftOperand);
      this.Visit(greaterThan.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IGreaterThanOrEqual greaterThanOrEqual)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(greaterThanOrEqual);
      this.Visit(greaterThanOrEqual.LeftOperand);
      this.Visit(greaterThanOrEqual.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ILabeledStatement labeledStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(labeledStatement);
      this.Visit(labeledStatement.Statement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ILeftShift leftShift)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(leftShift);
      this.Visit(leftShift.LeftOperand);
      this.Visit(leftShift.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ILessThan lessThan)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lessThan);
      this.Visit(lessThan.LeftOperand);
      this.Visit(lessThan.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ILessThanOrEqual lessThanOrEqual)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lessThanOrEqual);
      this.Visit(lessThanOrEqual.LeftOperand);
      this.Visit(lessThanOrEqual.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ILocalDeclarationStatement localDeclarationStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(localDeclarationStatement);
      if (localDeclarationStatement.InitialValue != null)
        this.Visit(localDeclarationStatement.InitialValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ILockStatement lockStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(lockStatement);
      this.Visit(lockStatement.Guard);
      this.Visit(lockStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ILogicalNot logicalNot)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(logicalNot);
      this.Visit(logicalNot.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.hod or operation is not implemented.");
      this.path.Pop();
    }

    public virtual void Visit(IBreakStatement breakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(breakStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IMakeTypedReference makeTypedReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(makeTypedReference);
      this.Visit(makeTypedReference.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.or operation is not implemented.");
      this.path.Pop();
    }

    public virtual void Visit(ISourceMethodBody methodBody)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodBody);
      if (!methodBody.MethodDefinition.IsAbstract)
        this.Visit(methodBody.Block);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public override void Visit(IMethodBody methodBody)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      ISourceMethodBody /*?*/ sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null) {
        this.Visit(sourceMethodBody);
        return;
      }
      //^ int oldCount = this.path.Count;
      this.path.Push(methodBody);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IMethodCall methodCall)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodCall);
      if (!methodCall.IsStaticCall)
        this.Visit(methodCall.ThisArgument);
      this.Visit(methodCall.Arguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IModulus modulus)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(modulus);
      this.Visit(modulus.LeftOperand);
      this.Visit(modulus.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IMultiplication multiplication)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(multiplication);
      this.Visit(multiplication.LeftOperand);
      this.Visit(multiplication.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.or operation is not implemented.");
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<INamedArgument> namedArguments)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (INamedArgument namedArgument in namedArguments) {
        this.Visit(namedArgument);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(INamedArgument namedArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namedArgument);
      this.Visit(namedArgument.ArgumentValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(INotEquality notEquality)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(notEquality);
      this.Visit(notEquality.LeftOperand);
      this.Visit(notEquality.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IOldValue oldValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(oldValue);
      this.Visit(oldValue.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IOnesComplement onesComplement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(onesComplement);
      this.Visit(onesComplement.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IOutArgument outArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(outArgument);
      this.Visit(outArgument.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public override void Visit(IParameterDefinition parameterDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(parameterDefinition);
      this.Visit(parameterDefinition.Attributes);
      if (parameterDefinition.HasDefaultValue)
        this.Visit((IMetadataExpression)parameterDefinition.DefaultValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IPointerCall pointerCall)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(pointerCall);
      this.Visit(pointerCall.Arguments);
      this.Visit(pointerCall.Pointer);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public override void Visit(IPropertyDefinition propertyDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(propertyDefinition);
      this.Visit(propertyDefinition.Attributes);
      this.Visit(propertyDefinition.Parameters);
      if (propertyDefinition.HasDefaultValue)
        this.Visit((IMetadataExpression)propertyDefinition.DefaultValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IRefArgument refArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(refArgument);
      this.Visit(refArgument.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IResourceUseStatement resourceUseStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(resourceUseStatement);
      this.Visit(resourceUseStatement.ResourceAcquisitions);
      this.Visit(resourceUseStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IRethrowStatement rethrowStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(rethrowStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IReturnStatement returnStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(returnStatement);
      if (returnStatement.Expression != null)
        this.Visit(returnStatement.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given return value expression.
    /// </summary>
    public virtual void Visit(IReturnValue returnValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IRightShift rightShift)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(rightShift);
      this.Visit(rightShift.LeftOperand);
      this.Visit(rightShift.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IStackArrayCreate stackArrayCreate)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(stackArrayCreate);
      this.Visit(stackArrayCreate.ElementType);
      this.Visit(stackArrayCreate.Size);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(ISizeOf sizeOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(sizeOf);
      this.Visit(sizeOf.TypeToSize);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IStatement> statements)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IStatement statement in statements) {
        this.Visit(statement);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IStatement statement) {
      if (this.stopTraversal) return;
      statement.Dispatch(this);
    }

    public virtual void Visit(ISubtraction subtraction)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(subtraction);
      this.Visit(subtraction.LeftOperand);
      this.Visit(subtraction.RightOperand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<ISwitchCase> switchCases)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ISwitchCase switchCase in switchCases) {
        this.Visit(switchCase);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(ISwitchCase switchCase)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(switchCase);
      if (!switchCase.IsDefault)
        this.Visit(switchCase.Expression);
      this.Visit(switchCase.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ISwitchStatement switchStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(switchStatement);
      this.Visit(switchStatement.Expression);
      this.Visit(switchStatement.Cases);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ITargetExpression targetExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      object/*?*/ def = targetExpression.Definition;
      IAddressDereference/*?*/ adr = def as IAddressDereference;
      if (adr != null)
        this.Visit(adr);
      else {
        IArrayIndexer/*?*/ indexer = def as IArrayIndexer;
        if (indexer != null) {
          this.Visit(indexer);
          return;
        }
      }
      if (targetExpression.Instance != null)
        this.Visit(targetExpression.Instance);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IThisReference thisReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IThrowStatement throwStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(throwStatement);
      if (throwStatement.Exception != null)
        this.Visit(throwStatement.Exception);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(tryCatchFilterFinallyStatement);
      this.Visit(tryCatchFilterFinallyStatement.TryBody);
      this.Visit(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        this.Visit(tryCatchFilterFinallyStatement.FinallyBody);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ITokenOf tokenOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(tokenOf);
      IFieldReference/*?*/ fieldReference = tokenOf.Definition as IFieldReference;
      if (fieldReference != null)
        this.Visit(fieldReference);
      else {
        IMethodReference/*?*/ methodReference = tokenOf.Definition as IMethodReference;
        if (methodReference != null)
          this.Visit(methodReference);
        else
          this.Visit((ITypeReference)tokenOf.Definition);
      }
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(ITypeOf typeOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeOf);
      this.Visit(typeOf.TypeToGet);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IUnaryNegation unaryNegation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unaryNegation);
      this.Visit(unaryNegation.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IUnaryPlus unaryPlus)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unaryPlus);
      this.Visit(unaryPlus.Operand);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IVectorLength vectorLength)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(vectorLength);
      this.Visit(vectorLength.Vector);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IWhileDoStatement whileDoStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(whileDoStatement);
      this.Visit(whileDoStatement.Condition);
      this.Visit(whileDoStatement.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IYieldBreakStatement yieldBreakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IYieldReturnStatement yieldReturnStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(yieldReturnStatement);
      this.Visit(yieldReturnStatement.Expression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    #endregion
  }

  /// <summary>
  /// A visitor base class that provides a dummy body for each method of ICodeVisitor.
  /// </summary>
  public class BaseCodeVisitor : BaseMetadataVisitor, ICodeVisitor {

    public BaseCodeVisitor() {
    }

    #region IMetadataVisitor Members

    public virtual void Visit(IAddition addition) {
    }

    public virtual void Visit(IAddressableExpression addressableExpression) {
    }

    public virtual void Visit(IAddressDereference addressDereference) {
    }

    public virtual void Visit(IAddressOf addressOf) {
    }

    public virtual void Visit(IAnonymousDelegate anonymousDelegate) {
    }

    public virtual void Visit(IArrayIndexer arrayIndexer) {
    }

    public virtual void Visit(IAssertStatement assertStatement) {
    }

    public virtual void Visit(IEnumerable<IAssignment> assignments) {
    }

    public virtual void Visit(IAssignment assignment) {
    }

    public virtual void Visit(IAssumeStatement assumeStatement) {
    }

    public virtual void Visit(IBaseClassReference baseClassReference) {
    }

    public virtual void Visit(IBitwiseAnd bitwiseAnd) {
    }

    public virtual void Visit(IBitwiseOr bitwiseOr) {
    }

    public virtual void Visit(IBlockExpression blockExpression) {
    }

    public virtual void Visit(IBlockStatement block) {
    }

    public virtual void Visit(IBreakStatement breakStatement) {
    }

    public virtual void Visit(ICastIfPossible castIfPossible) {
    }

    public virtual void Visit(IEnumerable<ICatchClause> catchClauses) {
    }

    public virtual void Visit(ICatchClause catchClause) {
    }

    public virtual void Visit(ICheckIfInstance checkIfInstance) {
    }

    public virtual void Visit(ICompileTimeConstant constant) {
    }

    public virtual void Visit(IConversion conversion) {
    }

    public virtual void Visit(IConditional conditional) {
    }

    public virtual void Visit(IConditionalStatement conditionalStatement) {
    }

    public virtual void Visit(IContinueStatement continueStatement) {
    }

    public virtual void Visit(ICreateArray createArray) {
    }

    public virtual void Visit(ICreateObjectInstance createObjectInstance) {
    }

    public virtual void Visit(ICreateDelegateInstance createDelegateInstance) {
    }

    public virtual void Visit(IDefaultValue defaultValue) {
    }

    public virtual void Visit(IDivision division) {
    }

    public virtual void Visit(IDoUntilStatement doUntilStatement) {
    }

    public virtual void Visit(IEmptyStatement emptyStatement) {
    }

    public virtual void Visit(IEquality equality) {
    }

    public virtual void Visit(IExclusiveOr exclusiveOr) {
    }

    public virtual void Visit(IBoundExpression boundExpression) {
    }

    public virtual void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
    }

    public virtual void Visit(IExpression expression) {
    }

    public virtual void Visit(IExpressionStatement expressionStatement) {
    }

    public virtual void Visit(IForEachStatement forEachStatement) {
    }

    public virtual void Visit(IForStatement forStatement) {
    }

    public virtual void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
    }

    public virtual void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
    }

    public virtual void Visit(IGotoStatement gotoStatement) {
    }

    public virtual void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
    }

    public virtual void Visit(IGreaterThan greaterThan) {
    }

    public virtual void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
    }

    public virtual void Visit(ILabeledStatement labeledStatement) {
    }

    public virtual void Visit(ILeftShift leftShift) {
    }

    public virtual void Visit(ILessThan lessThan) {
    }

    public virtual void Visit(ILessThanOrEqual lessThanOrEqual) {
    }

    public virtual void Visit(ILocalDeclarationStatement localDeclarationStatement) {
    }

    public virtual void Visit(ILockStatement lockStatement) {
    }

    public virtual void Visit(ILogicalNot logicalNot) {
    }

    public virtual void Visit(IMakeTypedReference makeTypedReference) {
    }

    public virtual void Visit(IMethodCall methodCall) {
    }

    public virtual void Visit(IModulus modulus) {
    }

    public virtual void Visit(IMultiplication multiplication) {
    }

    public virtual void Visit(IEnumerable<INamedArgument> namedArguments) {
    }

    public virtual void Visit(INamedArgument namedArgument) {
    }

    public virtual void Visit(INotEquality notEquality) {
    }

    public virtual void Visit(IOldValue oldValue) {
    }

    public virtual void Visit(IOnesComplement onesComplement) {
    }

    public virtual void Visit(IOutArgument outArgument) {
    }

    public virtual void Visit(IPointerCall pointerCall) {
    }

    public virtual void Visit(IRefArgument refArgument) {
    }

    public virtual void Visit(IResourceUseStatement resourceUseStatement) {
    }

    public virtual void Visit(IRethrowStatement rethrowStatement) {
    }

    public virtual void Visit(IReturnStatement returnStatement) {
    }

    public virtual void Visit(IReturnValue returnValue) {
    }

    public virtual void Visit(IRightShift rightShift) {
    }

    public virtual void Visit(IStackArrayCreate stackArrayCreate) {
    }

    public virtual void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
    }

    public virtual void Visit(ISizeOf sizeOf) {
    }

    public virtual void Visit(IEnumerable<IStatement> statements) {
    }

    public virtual void Visit(IStatement statement) {
    }

    public virtual void Visit(ISubtraction subtraction) {
    }

    public virtual void Visit(IEnumerable<ISwitchCase> switchCases) {
    }

    public virtual void Visit(ISwitchCase switchCase) {
    }

    public virtual void Visit(ISwitchStatement switchStatement) {
    }

    public virtual void Visit(ITargetExpression targetExpression) {
    }

    public virtual void Visit(IThisReference thisReference) {
    }

    public virtual void Visit(IThrowStatement throwStatement) {
    }

    public virtual void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
    }

    public virtual void Visit(ITokenOf tokenOf) {
    }

    public virtual void Visit(ITypeOf typeOf) {
    }

    public virtual void Visit(IUnaryNegation unaryNegation) {
    }

    public virtual void Visit(IUnaryPlus unaryPlus) {
    }

    public virtual void Visit(IVectorLength vectorLength) {
    }

    public virtual void Visit(IWhileDoStatement whileDoStatement) {
    }

    public virtual void Visit(IYieldBreakStatement yieldBreakStatement) {
    }

    public virtual void Visit(IYieldReturnStatement yieldReturnStatement) {
    }

    #endregion
  }

#pragma warning restore 1591
}

namespace Microsoft.Cci.Contracts {

  /// <summary>
  /// Implemented by classes that visit nodes of object graphs via a double dispatch mechanism, usually performing some computation of a subset of the nodes in the graph.
  /// Contains a specialized Visit routine for each standard type of object defined in the contract object model. 
  /// </summary>
  public interface ICodeAndContractVisitor : ICodeVisitor {
    /// <summary>
    /// Performs some computation with the given loop contract.
    /// </summary>
    void Visit(ILoopContract loopContract);

    /// <summary>
    /// Performs some computation with the given loop invariant.
    /// </summary>
    void Visit(ILoopInvariant loopInvariant);

    /// <summary>
    /// Performs some computation with the given method contract.
    /// </summary>
    void Visit(IMethodContract methodContract);

    /// <summary>
    /// Performs some computation with the given postCondition.
    /// </summary>
    void Visit(IPostcondition postCondition);

    /// <summary>
    /// Performs some computation with the given pre condition.
    /// </summary>
    void Visit(IPrecondition precondition);

    /// <summary>
    /// Performs some computation with the given thrown exception.
    /// </summary>
    void Visit(IThrownException thrownException);

    /// <summary>
    /// Performs some computation with the given type contract.
    /// </summary>
    void Visit(ITypeContract typeContract);

    /// <summary>
    /// Performs some computation with the given type invariant.
    /// </summary>
    void Visit(ITypeInvariant typeInvariant);
  }

  /// <summary>
  /// A visitor base class that traverses a code model in depth first, left to right order.
  /// </summary>
  public class BaseCodeAndContractTraverser : BaseCodeTraverser, ICodeAndContractVisitor {

    /// <summary>
    /// A map from code model objects to contract objects.
    /// </summary>
    protected readonly IContractProvider/*?*/ contractProvider;

    /// <summary>
    /// Allocates a visitor that traverses a code model model in depth first, left to right order.
    /// </summary>
    public BaseCodeAndContractTraverser(IContractProvider/*?*/ contractProvider) {
      this.contractProvider = contractProvider;
    }

    /// <summary>
    /// Traverses the given list of addressable expressions.
    /// </summary>
    public virtual void Visit(IEnumerable<IAddressableExpression> addressableExpressions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(addressableExpressions);
      foreach (var addressableExpression in addressableExpressions)
        this.Visit(addressableExpression);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of trigger expressions.
    /// </summary>
    public virtual void Visit(IEnumerable<IEnumerable<IExpression>> triggers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(triggers);
      foreach (var trigs in triggers)
        this.Visit(trigs);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given expression and any triggers that hang of it.
    /// </summary>
    public override void Visit(IExpression expression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      base.Visit(expression);
      if (this.contractProvider == null) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(expression);
      if (expression is IMethodCall) {
        IEnumerable<IEnumerable<IExpression>>/*?*/ triggers = this.contractProvider.GetTriggersFor(expression);
        if (triggers != null)
          this.Visit(triggers);
      }
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given loop contract.
    /// </summary>
    public virtual void Visit(ILoopContract loopContract)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(loopContract);
      this.Visit(loopContract.Invariants);
      this.Visit(loopContract.Writes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of loop invariants.
    /// </summary>
    public virtual void Visit(IEnumerable<ILoopInvariant> loopInvariants)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(loopInvariants);
      foreach (var loopInvariant in loopInvariants)
        this.Visit(loopInvariant);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given loop invariant.
    /// </summary>
    public virtual void Visit(ILoopInvariant loopInvariant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(loopInvariant);
      this.Visit(loopInvariant.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given method contract.
    /// </summary>
    public virtual void Visit(IMethodContract methodContract)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodContract);
      this.Visit(methodContract.Allocates);
      this.Visit(methodContract.Frees);
      this.Visit(methodContract.ModifiedVariables);
      this.Visit(methodContract.Postconditions);
      this.Visit(methodContract.Preconditions);
      this.Visit(methodContract.Reads);
      this.Visit(methodContract.ThrownExceptions);
      this.Visit(methodContract.Writes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of post conditions.
    /// </summary>
    public virtual void Visit(IEnumerable<IPostcondition> postConditions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(postConditions);
      foreach (var postCondition in postConditions)
        this.Visit(postCondition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given postCondition.
    /// </summary>
    public virtual void Visit(IPostcondition postCondition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(postCondition);
      this.Visit(postCondition.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of pre conditions.
    /// </summary>
    public virtual void Visit(IEnumerable<IPrecondition> preconditions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(preconditions);
      foreach (var precondition in preconditions)
        this.Visit(precondition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given pre condition.
    /// </summary>
    public virtual void Visit(IPrecondition precondition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(precondition);
      this.Visit(precondition.Condition);
      if (precondition.ExceptionToThrow != null)
        this.Visit(precondition.ExceptionToThrow);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given statement.
    /// </summary>
    public override void Visit(IStatement statement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      base.Visit(statement);
      if (this.contractProvider == null) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(statement);
      ILoopContract/*?*/ loopContract = this.contractProvider.GetLoopContractFor(statement);
      if (loopContract != null)
        this.Visit(loopContract);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of thrown exceptions.
    /// </summary>
    public virtual void Visit(IEnumerable<IThrownException> thrownExceptions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(thrownExceptions);
      foreach (var thrownException in thrownExceptions)
        this.Visit(thrownException);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given thrown exception.
    /// </summary>
    public virtual void Visit(IThrownException thrownException)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(thrownException);
      this.Visit(thrownException.ExceptionType);
      this.Visit(thrownException.Postconditions);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given type contract.
    /// </summary>
    public virtual void Visit(ITypeContract typeContract)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeContract);
      this.Visit(typeContract.ContractFields);
      this.Visit(typeContract.ContractMethods);
      this.Visit(typeContract.Invariants);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of addressable expressions.
    /// </summary>
    public virtual void Visit(IEnumerable<ITypeInvariant> typeInvariants)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeInvariants);
      foreach (var typeInvariant in typeInvariants)
        this.Visit(typeInvariant);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given type invariant.
    /// </summary>
    public virtual void Visit(ITypeInvariant typeInvariant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeInvariant);
      this.Visit(typeInvariant.Condition);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given method definition.
    /// </summary>
    public override void Visit(IMethodDefinition method)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      base.Visit(method);
      if (this.contractProvider == null) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(method);
      IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(method);
      if (methodContract != null)
        this.Visit(methodContract);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given type definition.
    /// </summary>
    public override void Visit(ITypeDefinition typeDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      base.Visit(typeDefinition);
      if (this.contractProvider == null) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeDefinition);
      ITypeContract/*?*/ typeContract = this.contractProvider.GetTypeContractFor(typeDefinition);
      if (typeContract != null)
        this.Visit(typeContract);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

  }

  /// <summary>
  /// A visitor base class that provides a dummy body for each method of ICodeAndContractVisitor.
  /// </summary>
  public class BaseCodeAndContractVisitor : BaseCodeVisitor, ICodeAndContractVisitor {

    /// <summary>
    /// A map from code model objects to contract objects.
    /// </summary>
    protected readonly IContractProvider/*?*/ contractProvider;

    /// <summary>
    /// Allocates a visitor that traverses a code model model in depth first, left to right order.
    /// </summary>
    public BaseCodeAndContractVisitor(IContractProvider/*?*/ contractProvider) {
      this.contractProvider = contractProvider;
    }

    /// <summary>
    /// Visits the given loop contract.
    /// </summary>
    public virtual void Visit(ILoopContract loopContract)
    {
    }

    /// <summary>
    /// Visits the given loop invariant.
    /// </summary>
    public virtual void Visit(ILoopInvariant loopInvariant)
    {
    }

    /// <summary>
    /// Visits the given method contract.
    /// </summary>
    public virtual void Visit(IMethodContract methodContract)
    {
    }

    /// <summary>
    /// Visits the given postCondition.
    /// </summary>
    public virtual void Visit(IPostcondition postCondition)
    {
    }

    /// <summary>
    /// Visits the given pre condition.
    /// </summary>
    public virtual void Visit(IPrecondition precondition)
    {
    }

    /// <summary>
    /// Visits the given thrown exception.
    /// </summary>
    public virtual void Visit(IThrownException thrownException)
    {
    }

    /// <summary>
    /// Visits the given type contract.
    /// </summary>
    public virtual void Visit(ITypeContract typeContract)
    {
    }

    /// <summary>
    /// Visits the given type invariant.
    /// </summary>
    public virtual void Visit(ITypeInvariant typeInvariant)
    {
    }

  }

}

