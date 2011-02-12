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
    /// Performs some compuation with the given dup value expression.
    /// </summary>
    /// <param name="dupValue"></param>
    void Visit(IDupValue dupValue);
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
    /// Performs some compuation with the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    void Visit(IPopValue popValue);
    /// <summary>
    /// Performs some computation with the given push statement.
    /// </summary>
    void Visit(IPushStatement pushStatement);
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

  /// <summary>
  /// A visitor base class that traverses the code model in depth first, left to right order.
  /// </summary>
  public class BaseCodeTraverser : BaseMetadataTraverser, ICodeVisitor {

    /// <summary>
    /// Allocates a visitor instance that traverses the code model in depth first, left to right order.
    /// </summary>
    public BaseCodeTraverser() {
    }

    #region ICodeVisitor Members

    /// <summary>
    /// Performs some computation with the given addition.
    /// </summary>
    /// <param name="addition"></param>
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

    /// <summary>
    /// Performs some computation with the given addressable expression.
    /// </summary>
    /// <param name="addressableExpression"></param>
    public virtual void Visit(IAddressableExpression addressableExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      object def = addressableExpression.Definition;
      var loc = def as ILocalDefinition;
      if (loc != null)
        this.VisitReference(loc);
      else {
        var par = def as IParameterDefinition;
        if (par != null)
          this.VisitReference(par);
        else {
          var fieldReference = def as IFieldReference;
          if (fieldReference != null)
            this.Visit(fieldReference);
          else {
            var indexer = def as IArrayIndexer;
            if (indexer != null)
              this.Visit(indexer);
            else {
              var adr = def as IAddressDereference;
              if (adr != null)
                this.Visit(adr);
              else {
                var meth = def as IMethodReference;
                if (meth != null)
                  this.Visit(meth);
                else {
                  var thisRef = (IThisReference)def;
                  this.Visit(thisRef);
                }
              }
            }
          }
        }
      }
      if (addressableExpression.Instance != null)
        this.Visit(addressableExpression.Instance);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given address dereference expression.
    /// </summary>
    /// <param name="addressDereference"></param>
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

    /// <summary>
    /// Performs some computation with the given AddressOf expression.
    /// </summary>
    /// <param name="addressOf"></param>
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

    /// <summary>
    /// Performs some computation with the given anonymous delegate expression.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
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

    /// <summary>
    /// Performs some computation with the given assert statement.
    /// </summary>
    /// <param name="assertStatement"></param>
    public virtual void Visit(IAssertStatement assertStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assertStatement);
      this.Visit(assertStatement.Condition);
      if (assertStatement.Description != null)
        this.Visit(assertStatement.Description);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Visits the specified assignments.
    /// </summary>
    /// <param name="assignments">The assignments.</param>
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

    /// <summary>
    /// Performs some computation with the given assignment expression.
    /// </summary>
    /// <param name="assignment"></param>
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

    /// <summary>
    /// Performs some computation with the given assume statement.
    /// </summary>
    /// <param name="assumeStatement"></param>
    public virtual void Visit(IAssumeStatement assumeStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(assumeStatement);
      this.Visit(assumeStatement.Condition);
      if (assumeStatement.Description != null)
        this.Visit(assumeStatement.Description);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given bitwise and expression.
    /// </summary>
    /// <param name="bitwiseAnd"></param>
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

    /// <summary>
    /// Performs some computation with the given bitwise or expression.
    /// </summary>
    /// <param name="bitwiseOr"></param>
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

    /// <summary>
    /// Performs some computation with the given block expression.
    /// </summary>
    /// <param name="blockExpression"></param>
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

    /// <summary>
    /// Performs some computation with the given statement block.
    /// </summary>
    /// <param name="block"></param>
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

    /// <summary>
    /// Performs some computation with the cast-if-possible expression.
    /// </summary>
    /// <param name="castIfPossible"></param>
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

    /// <summary>
    /// Visits the specified catch clauses.
    /// </summary>
    /// <param name="catchClauses">The catch clauses.</param>
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

    /// <summary>
    /// Performs some computation with the given catch clause.
    /// </summary>
    /// <param name="catchClause"></param>
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

    /// <summary>
    /// Performs some computation with the given check-if-instance expression.
    /// </summary>
    /// <param name="checkIfInstance"></param>
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

    /// <summary>
    /// Performs some computation with the given compile time constant.
    /// </summary>
    /// <param name="constant"></param>
    public virtual void Visit(ICompileTimeConstant constant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given conversion expression.
    /// </summary>
    /// <param name="conversion"></param>
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

    /// <summary>
    /// Performs some computation with the given conditional expression.
    /// </summary>
    /// <param name="conditional"></param>
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

    /// <summary>
    /// Performs some computation with the given conditional statement.
    /// </summary>
    /// <param name="conditionalStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given continue statement.
    /// </summary>
    /// <param name="continueStatement"></param>
    public virtual void Visit(IContinueStatement continueStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(continueStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
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

    /// <summary>
    /// Performs some computation with the given constructor call expression.
    /// </summary>
    /// <param name="createObjectInstance"></param>
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

    /// <summary>
    /// Performs some computation with the anonymous object creation expression.
    /// </summary>
    /// <param name="createDelegateInstance"></param>
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

    /// <summary>
    /// Performs some computation with the given array indexer expression.
    /// </summary>
    /// <param name="arrayIndexer"></param>
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
    /// <param name="boundExpression"></param>
    public virtual void Visit(IBoundExpression boundExpression) {
      if (this.stopTraversal) return;
      this.path.Push(boundExpression);
      var definition = boundExpression.Definition;
      var local = definition as ILocalDefinition;
      if (local != null)
        this.VisitReference(local);
      else {
        var parameter = definition as IParameterDefinition;
        if (parameter != null)
          this.VisitReference(parameter);
        else {
          var field = (IFieldReference)definition;
          this.Visit(field);
        }
      }
      if (boundExpression.Instance != null)
        this.Visit(boundExpression.Instance);
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given custom attribute.
    /// </summary>
    /// <param name="customAttribute"></param>
    public override void Visit(ICustomAttribute customAttribute)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(customAttribute);
      this.Visit(customAttribute.Constructor);
      this.Visit(customAttribute.Arguments);
      this.Visit(customAttribute.NamedArguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given defalut value expression.
    /// </summary>
    /// <param name="defaultValue"></param>
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

    /// <summary>
    /// Performs some computation with the given debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement"></param>
    public virtual void Visit(IDebuggerBreakStatement debuggerBreakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(debuggerBreakStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given division expression.
    /// </summary>
    /// <param name="division"></param>
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

    /// <summary>
    /// Performs some computation with the given do until statement.
    /// </summary>
    /// <param name="doUntilStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given dup value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public virtual void Visit(IDupValue popValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
    }

    /// <summary>
    /// Performs some computation with the given empty statement.
    /// </summary>
    /// <param name="emptyStatement"></param>
    public virtual void Visit(IEmptyStatement emptyStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(emptyStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given equality expression.
    /// </summary>
    /// <param name="equality"></param>
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

    /// <summary>
    /// Performs some computation with the given exclusive or expression.
    /// </summary>
    /// <param name="exclusiveOr"></param>
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

    /// <summary>
    /// Visits the specified expressions.
    /// </summary>
    /// <param name="expressions">The expressions.</param>
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

    /// <summary>
    /// Performs some computation with the given expression.
    /// </summary>
    /// <param name="expression"></param>
    public virtual void Visit(IExpression expression) {
      if (this.stopTraversal) return;
      expression.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given expression statement.
    /// </summary>
    /// <param name="expressionStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given foreach statement.
    /// </summary>
    /// <param name="forEachStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given for statement.
    /// </summary>
    /// <param name="forStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given get type of typed reference expression.
    /// </summary>
    /// <param name="getTypeOfTypedReference"></param>
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

    /// <summary>
    /// Performs some computation with the given get value of typed reference expression.
    /// </summary>
    /// <param name="getValueOfTypedReference"></param>
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

    /// <summary>
    /// Performs some computation with the given goto statement.
    /// </summary>
    /// <param name="gotoStatement"></param>
    public virtual void Visit(IGotoStatement gotoStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(gotoStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public virtual void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given greater-than expression.
    /// </summary>
    /// <param name="greaterThan"></param>
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

    /// <summary>
    /// Performs some computation with the given greater-than-or-equal expression.
    /// </summary>
    /// <param name="greaterThanOrEqual"></param>
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

    /// <summary>
    /// Performs some computation with the given labeled statement.
    /// </summary>
    /// <param name="labeledStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given left shift expression.
    /// </summary>
    /// <param name="leftShift"></param>
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

    /// <summary>
    /// Performs some computation with the given less-than expression.
    /// </summary>
    /// <param name="lessThan"></param>
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

    /// <summary>
    /// Performs some computation with the given less-than-or-equal expression.
    /// </summary>
    /// <param name="lessThanOrEqual"></param>
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

    /// <summary>
    /// Performs some computation with the given local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given lock statement.
    /// </summary>
    /// <param name="lockStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given logical not expression.
    /// </summary>
    /// <param name="logicalNot"></param>
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

    /// <summary>
    /// Performs some computation with the given break statement.
    /// </summary>
    /// <param name="breakStatement"></param>
    public virtual void Visit(IBreakStatement breakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(breakStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given make typed reference expression.
    /// </summary>
    /// <param name="makeTypedReference"></param>
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

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
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

    /// <summary>
    /// Performs some computation with the given method definition.
    /// </summary>
    /// <param name="method"></param>
    public override void Visit(IMethodDefinition method)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(method);
      this.VisitMethodReturnAttributes(method.ReturnValueAttributes);
      if (method.ReturnValueIsModified)
        this.Visit(method.ReturnValueCustomModifiers);
      if (method.HasDeclarativeSecurity)
        this.Visit(method.SecurityAttributes);
      if (method.IsGeneric) this.Visit(method.GenericParameters);
      this.Visit(method.Type);
      this.Visit(method.Parameters);
      if (method.IsPlatformInvoke)
        this.Visit(method.PlatformInvokeData);
      if (!method.IsAbstract && !method.IsExternal)
        this.Visit(method.Body);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given method body.
    /// </summary>
    /// <param name="methodBody"></param>
    public override void Visit(IMethodBody methodBody)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      var sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null) {
        this.Visit(sourceMethodBody);
        return;
      }
      //^ int oldCount = this.path.Count;
      this.path.Push(methodBody);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given method call.
    /// </summary>
    /// <param name="methodCall"></param>
    public virtual void Visit(IMethodCall methodCall)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodCall);
      this.Visit(methodCall.MethodToCall);
      if (!methodCall.IsStaticCall)
        this.Visit(methodCall.ThisArgument);
      this.Visit(methodCall.Arguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given modulus expression.
    /// </summary>
    /// <param name="modulus"></param>
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

    /// <summary>
    /// Performs some computation with the given multiplication expression.
    /// </summary>
    /// <param name="multiplication"></param>
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

    /// <summary>
    /// Visits the specified named arguments.
    /// </summary>
    /// <param name="namedArguments">The named arguments.</param>
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

    /// <summary>
    /// Performs some computation with the given named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
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

    /// <summary>
    /// Performs some computation with the given not equality expression.
    /// </summary>
    /// <param name="notEquality"></param>
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

    /// <summary>
    /// Performs some computation with the given old value expression.
    /// </summary>
    /// <param name="oldValue"></param>
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

    /// <summary>
    /// Performs some computation with the given one's complement expression.
    /// </summary>
    /// <param name="onesComplement"></param>
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

    /// <summary>
    /// Performs some computation with the given out argument expression.
    /// </summary>
    /// <param name="outArgument"></param>
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

    /// <summary>
    /// Performs some computation with the given pointer call.
    /// </summary>
    /// <param name="pointerCall"></param>
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

    /// <summary>
    /// Performs some computation with the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public virtual void Visit(IPopValue popValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
    }

    /// <summary>
    /// Performs some computation with the given push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    public virtual void Visit(IPushStatement pushStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(pushStatement);
      this.Visit(pushStatement.ValueToPush);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given ref argument expression.
    /// </summary>
    /// <param name="refArgument"></param>
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

    /// <summary>
    /// Performs some computation with the given resource usage statement.
    /// </summary>
    /// <param name="resourceUseStatement"></param>
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

    /// <summary>
    /// Performs some computation with the rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement"></param>
    public virtual void Visit(IRethrowStatement rethrowStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(rethrowStatement);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the return statement.
    /// </summary>
    /// <param name="returnStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given right shift expression.
    /// </summary>
    /// <param name="rightShift"></param>
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

    /// <summary>
    /// Performs some computation with the given stack array create expression.
    /// </summary>
    /// <param name="stackArrayCreate"></param>
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

    /// <summary>
    /// Performs some computation with the given runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression"></param>
    public virtual void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given sizeof() expression.
    /// </summary>
    /// <param name="sizeOf"></param>
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

    /// <summary>
    /// Visits the specified statements.
    /// </summary>
    /// <param name="statements">The statements.</param>
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

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public virtual void Visit(IStatement statement) {
      if (this.stopTraversal) return;
      statement.Dispatch(this);
    }

    /// <summary>
    /// Performs some computation with the given subtraction expression.
    /// </summary>
    /// <param name="subtraction"></param>
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

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
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

    /// <summary>
    /// Performs some computation with the given switch case.
    /// </summary>
    /// <param name="switchCase"></param>
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

    /// <summary>
    /// Performs some computation with the given switch statement.
    /// </summary>
    /// <param name="switchStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given target expression.
    /// </summary>
    /// <param name="targetExpression"></param>
    public virtual void Visit(ITargetExpression targetExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      object def = targetExpression.Definition;
      var loc = def as ILocalDefinition;
      if (loc != null)
        this.VisitReference(loc);
      else {
        var par = def as IParameterDefinition;
        if (par != null)
          this.VisitReference(par);
        else {
          var fieldReference = def as IFieldReference;
          if (fieldReference != null)
            this.Visit(fieldReference);
          else {
            var indexer = def as IArrayIndexer;
            if (indexer != null) {
              this.Visit(indexer);
              return; //do not visit the instance again
            } else {
              var adr = def as IAddressDereference;
              if (adr != null)
                this.Visit(adr);
              else {
                var meth = def as IMethodReference;
                if (meth != null)
                  this.Visit(meth);
                else {
                  var propertyReference = (IPropertyDefinition)def;
                  this.VisitReference(propertyReference);
                }
              }
            }
          }
        }
      }
      if (targetExpression.Instance != null)
        this.Visit(targetExpression.Instance);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    /// <summary>
    /// Performs some computation with the given this reference expression.
    /// </summary>
    /// <param name="thisReference"></param>
    public virtual void Visit(IThisReference thisReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the throw statement.
    /// </summary>
    /// <param name="throwStatement"></param>
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

    /// <summary>
    /// Performs some computation with the try-catch-filter-finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement"></param>
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
      if (tryCatchFilterFinallyStatement.FaultBody != null)
        this.Visit(tryCatchFilterFinallyStatement.FaultBody);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Performs some computation with the given tokenof() expression.
    /// </summary>
    /// <param name="tokenOf"></param>
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

    /// <summary>
    /// Performs some computation with the given typeof() expression.
    /// </summary>
    /// <param name="typeOf"></param>
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

    /// <summary>
    /// Performs some computation with the given unary negation expression.
    /// </summary>
    /// <param name="unaryNegation"></param>
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

    /// <summary>
    /// Performs some computation with the given unary plus expression.
    /// </summary>
    /// <param name="unaryPlus"></param>
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

    /// <summary>
    /// Performs some computation with the given vector length expression.
    /// </summary>
    /// <param name="vectorLength"></param>
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

    /// <summary>
    /// Performs some computation with the given while do statement.
    /// </summary>
    /// <param name="whileDoStatement"></param>
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

    /// <summary>
    /// Performs some computation with the given yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public virtual void Visit(IYieldBreakStatement yieldBreakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Performs some computation with the given yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement"></param>
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

    /// <summary>
    /// Performs some computation on the reference to the given parameter definition.
    /// </summary>
    /// <param name="parameter">The parameter being referenced.</param>
    public virtual void VisitReference(IParameterDefinition parameter) {
    }

    /// <summary>
    /// Performs some computation on the reference to the given local definition.
    /// </summary>
    /// <param name="local">The local definition being referenced.</param>
    public virtual void VisitReference(ILocalDefinition local) {
    }

    /// <summary>
    /// Performs some computation on the reference to the given property definition.
    /// </summary>
    /// <param name="property">The property definition being referenced.</param>
    public virtual void VisitReference(IPropertyDefinition property) {
    }

  }

  /// <summary>
  /// A visitor base class that provides a dummy body for each method of ICodeVisitor.
  /// </summary>
  public class BaseCodeVisitor : BaseMetadataVisitor, ICodeVisitor {

    /// <summary>
    /// 
    /// </summary>
    public BaseCodeVisitor() {
    }

    #region IMetadataVisitor Members

    /// <summary>
    /// Performs some computation with the given addition.
    /// </summary>
    /// <param name="addition"></param>
    public virtual void Visit(IAddition addition) {
    }

    /// <summary>
    /// Performs some computation with the given addressable expression.
    /// </summary>
    /// <param name="addressableExpression"></param>
    public virtual void Visit(IAddressableExpression addressableExpression) {
    }

    /// <summary>
    /// Performs some computation with the given address dereference expression.
    /// </summary>
    /// <param name="addressDereference"></param>
    public virtual void Visit(IAddressDereference addressDereference) {
    }

    /// <summary>
    /// Performs some computation with the given AddressOf expression.
    /// </summary>
    /// <param name="addressOf"></param>
    public virtual void Visit(IAddressOf addressOf) {
    }

    /// <summary>
    /// Performs some computation with the given anonymous delegate expression.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
    public virtual void Visit(IAnonymousDelegate anonymousDelegate) {
    }

    /// <summary>
    /// Performs some computation with the given array indexer expression.
    /// </summary>
    /// <param name="arrayIndexer"></param>
    public virtual void Visit(IArrayIndexer arrayIndexer) {
    }

    /// <summary>
    /// Performs some computation with the given assert statement.
    /// </summary>
    /// <param name="assertStatement"></param>
    public virtual void Visit(IAssertStatement assertStatement) {
    }

    /// <summary>
    /// Visits the specified assignments.
    /// </summary>
    /// <param name="assignments">The assignments.</param>
    public virtual void Visit(IEnumerable<IAssignment> assignments) {
    }

    /// <summary>
    /// Performs some computation with the given assignment expression.
    /// </summary>
    /// <param name="assignment"></param>
    public virtual void Visit(IAssignment assignment) {
    }

    /// <summary>
    /// Performs some computation with the given assume statement.
    /// </summary>
    /// <param name="assumeStatement"></param>
    public virtual void Visit(IAssumeStatement assumeStatement) {
    }

    /// <summary>
    /// Performs some computation with the given bitwise and expression.
    /// </summary>
    /// <param name="bitwiseAnd"></param>
    public virtual void Visit(IBitwiseAnd bitwiseAnd) {
    }

    /// <summary>
    /// Performs some computation with the given bitwise or expression.
    /// </summary>
    /// <param name="bitwiseOr"></param>
    public virtual void Visit(IBitwiseOr bitwiseOr) {
    }

    /// <summary>
    /// Performs some computation with the given block expression.
    /// </summary>
    /// <param name="blockExpression"></param>
    public virtual void Visit(IBlockExpression blockExpression) {
    }

    /// <summary>
    /// Performs some computation with the given statement block.
    /// </summary>
    /// <param name="block"></param>
    public virtual void Visit(IBlockStatement block) {
    }

    /// <summary>
    /// Performs some computation with the given break statement.
    /// </summary>
    /// <param name="breakStatement"></param>
    public virtual void Visit(IBreakStatement breakStatement) {
    }

    /// <summary>
    /// Performs some computation with the cast-if-possible expression.
    /// </summary>
    /// <param name="castIfPossible"></param>
    public virtual void Visit(ICastIfPossible castIfPossible) {
    }

    /// <summary>
    /// Visits the specified catch clauses.
    /// </summary>
    /// <param name="catchClauses">The catch clauses.</param>
    public virtual void Visit(IEnumerable<ICatchClause> catchClauses) {
    }

    /// <summary>
    /// Performs some computation with the given catch clause.
    /// </summary>
    /// <param name="catchClause"></param>
    public virtual void Visit(ICatchClause catchClause) {
    }

    /// <summary>
    /// Performs some computation with the given check-if-instance expression.
    /// </summary>
    /// <param name="checkIfInstance"></param>
    public virtual void Visit(ICheckIfInstance checkIfInstance) {
    }

    /// <summary>
    /// Performs some computation with the given compile time constant.
    /// </summary>
    /// <param name="constant"></param>
    public virtual void Visit(ICompileTimeConstant constant) {
    }

    /// <summary>
    /// Performs some computation with the given conversion expression.
    /// </summary>
    /// <param name="conversion"></param>
    public virtual void Visit(IConversion conversion) {
    }

    /// <summary>
    /// Performs some computation with the given conditional expression.
    /// </summary>
    /// <param name="conditional"></param>
    public virtual void Visit(IConditional conditional) {
    }

    /// <summary>
    /// Performs some computation with the given conditional statement.
    /// </summary>
    /// <param name="conditionalStatement"></param>
    public virtual void Visit(IConditionalStatement conditionalStatement) {
    }

    /// <summary>
    /// Performs some computation with the given continue statement.
    /// </summary>
    /// <param name="continueStatement"></param>
    public virtual void Visit(IContinueStatement continueStatement) {
    }

    /// <summary>
    /// Performs some computation with the given array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
    public virtual void Visit(ICreateArray createArray) {
    }

    /// <summary>
    /// Performs some computation with the given constructor call expression.
    /// </summary>
    /// <param name="createObjectInstance"></param>
    public virtual void Visit(ICreateObjectInstance createObjectInstance) {
    }

    /// <summary>
    /// Performs some computation with the anonymous object creation expression.
    /// </summary>
    /// <param name="createDelegateInstance"></param>
    public virtual void Visit(ICreateDelegateInstance createDelegateInstance) {
    }

    /// <summary>
    /// Performs some computation with the given defalut value expression.
    /// </summary>
    /// <param name="defaultValue"></param>
    public virtual void Visit(IDefaultValue defaultValue) {
    }

    /// <summary>
    /// Performs some computation with the given division expression.
    /// </summary>
    /// <param name="division"></param>
    public virtual void Visit(IDivision division) {
    }

    /// <summary>
    /// Performs some computation with the given do until statement.
    /// </summary>
    /// <param name="doUntilStatement"></param>
    public virtual void Visit(IDoUntilStatement doUntilStatement) {
    }

    /// <summary>
    /// Performs some computation with the given dup value expression.
    /// </summary>
    /// <param name="dupValue"></param>
    public virtual void Visit(IDupValue dupValue) {
    }

    /// <summary>
    /// Performs some computation with the given empty statement.
    /// </summary>
    /// <param name="emptyStatement"></param>
    public virtual void Visit(IEmptyStatement emptyStatement) {
    }

    /// <summary>
    /// Performs some computation with the given equality expression.
    /// </summary>
    /// <param name="equality"></param>
    public virtual void Visit(IEquality equality) {
    }

    /// <summary>
    /// Performs some computation with the given exclusive or expression.
    /// </summary>
    /// <param name="exclusiveOr"></param>
    public virtual void Visit(IExclusiveOr exclusiveOr) {
    }

    /// <summary>
    /// Performs some computation with the given bound expression.
    /// </summary>
    /// <param name="boundExpression"></param>
    public virtual void Visit(IBoundExpression boundExpression) {
    }

    /// <summary>
    /// Performs some computation with the given debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement"></param>
    public virtual void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
    }

    /// <summary>
    /// Performs some computation with the given expression.
    /// </summary>
    /// <param name="expression"></param>
    public virtual void Visit(IExpression expression) {
    }

    /// <summary>
    /// Performs some computation with the given expression statement.
    /// </summary>
    /// <param name="expressionStatement"></param>
    public virtual void Visit(IExpressionStatement expressionStatement) {
    }

    /// <summary>
    /// Performs some computation with the given foreach statement.
    /// </summary>
    /// <param name="forEachStatement"></param>
    public virtual void Visit(IForEachStatement forEachStatement) {
    }

    /// <summary>
    /// Performs some computation with the given for statement.
    /// </summary>
    /// <param name="forStatement"></param>
    public virtual void Visit(IForStatement forStatement) {
    }

    /// <summary>
    /// Performs some computation with the given get type of typed reference expression.
    /// </summary>
    /// <param name="getTypeOfTypedReference"></param>
    public virtual void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
    }

    /// <summary>
    /// Performs some computation with the given get value of typed reference expression.
    /// </summary>
    /// <param name="getValueOfTypedReference"></param>
    public virtual void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
    }

    /// <summary>
    /// Performs some computation with the given goto statement.
    /// </summary>
    /// <param name="gotoStatement"></param>
    public virtual void Visit(IGotoStatement gotoStatement) {
    }

    /// <summary>
    /// Performs some computation with the given goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public virtual void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
    }

    /// <summary>
    /// Performs some computation with the given greater-than expression.
    /// </summary>
    /// <param name="greaterThan"></param>
    public virtual void Visit(IGreaterThan greaterThan) {
    }

    /// <summary>
    /// Performs some computation with the given greater-than-or-equal expression.
    /// </summary>
    /// <param name="greaterThanOrEqual"></param>
    public virtual void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
    }

    /// <summary>
    /// Performs some computation with the given labeled statement.
    /// </summary>
    /// <param name="labeledStatement"></param>
    public virtual void Visit(ILabeledStatement labeledStatement) {
    }

    /// <summary>
    /// Performs some computation with the given left shift expression.
    /// </summary>
    /// <param name="leftShift"></param>
    public virtual void Visit(ILeftShift leftShift) {
    }

    /// <summary>
    /// Performs some computation with the given less-than expression.
    /// </summary>
    /// <param name="lessThan"></param>
    public virtual void Visit(ILessThan lessThan) {
    }

    /// <summary>
    /// Performs some computation with the given less-than-or-equal expression.
    /// </summary>
    /// <param name="lessThanOrEqual"></param>
    public virtual void Visit(ILessThanOrEqual lessThanOrEqual) {
    }

    /// <summary>
    /// Performs some computation with the given local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement"></param>
    public virtual void Visit(ILocalDeclarationStatement localDeclarationStatement) {
    }

    /// <summary>
    /// Performs some computation with the given lock statement.
    /// </summary>
    /// <param name="lockStatement"></param>
    public virtual void Visit(ILockStatement lockStatement) {
    }

    /// <summary>
    /// Performs some computation with the given logical not expression.
    /// </summary>
    /// <param name="logicalNot"></param>
    public virtual void Visit(ILogicalNot logicalNot) {
    }

    /// <summary>
    /// Performs some computation with the given make typed reference expression.
    /// </summary>
    /// <param name="makeTypedReference"></param>
    public virtual void Visit(IMakeTypedReference makeTypedReference) {
    }

    /// <summary>
    /// Performs some computation with the given method call.
    /// </summary>
    /// <param name="methodCall"></param>
    public virtual void Visit(IMethodCall methodCall) {
    }

    /// <summary>
    /// Performs some computation with the given modulus expression.
    /// </summary>
    /// <param name="modulus"></param>
    public virtual void Visit(IModulus modulus) {
    }

    /// <summary>
    /// Performs some computation with the given multiplication expression.
    /// </summary>
    /// <param name="multiplication"></param>
    public virtual void Visit(IMultiplication multiplication) {
    }

    /// <summary>
    /// Visits the specified named arguments.
    /// </summary>
    /// <param name="namedArguments">The named arguments.</param>
    public virtual void Visit(IEnumerable<INamedArgument> namedArguments) {
    }

    /// <summary>
    /// Performs some computation with the given named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
    public virtual void Visit(INamedArgument namedArgument) {
    }

    /// <summary>
    /// Performs some computation with the given not equality expression.
    /// </summary>
    /// <param name="notEquality"></param>
    public virtual void Visit(INotEquality notEquality) {
    }

    /// <summary>
    /// Performs some computation with the given old value expression.
    /// </summary>
    /// <param name="oldValue"></param>
    public virtual void Visit(IOldValue oldValue) {
    }

    /// <summary>
    /// Performs some computation with the given one's complement expression.
    /// </summary>
    /// <param name="onesComplement"></param>
    public virtual void Visit(IOnesComplement onesComplement) {
    }

    /// <summary>
    /// Performs some computation with the given out argument expression.
    /// </summary>
    /// <param name="outArgument"></param>
    public virtual void Visit(IOutArgument outArgument) {
    }

    /// <summary>
    /// Performs some computation with the given pointer call.
    /// </summary>
    /// <param name="pointerCall"></param>
    public virtual void Visit(IPointerCall pointerCall) {
    }

    /// <summary>
    /// Performs some computation with the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public virtual void Visit(IPopValue popValue) {
    }

    /// <summary>
    /// Performs some computation with the given push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    public virtual void Visit(IPushStatement pushStatement) {
    }

    /// <summary>
    /// Performs some computation with the given ref argument expression.
    /// </summary>
    /// <param name="refArgument"></param>
    public virtual void Visit(IRefArgument refArgument) {
    }

    /// <summary>
    /// Performs some computation with the given resource usage statement.
    /// </summary>
    /// <param name="resourceUseStatement"></param>
    public virtual void Visit(IResourceUseStatement resourceUseStatement) {
    }

    /// <summary>
    /// Performs some computation with the rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement"></param>
    public virtual void Visit(IRethrowStatement rethrowStatement) {
    }

    /// <summary>
    /// Performs some computation with the return statement.
    /// </summary>
    /// <param name="returnStatement"></param>
    public virtual void Visit(IReturnStatement returnStatement) {
    }

    /// <summary>
    /// Performs some computation with the given return value expression.
    /// </summary>
    /// <param name="returnValue"></param>
    public virtual void Visit(IReturnValue returnValue) {
    }

    /// <summary>
    /// Performs some computation with the given right shift expression.
    /// </summary>
    /// <param name="rightShift"></param>
    public virtual void Visit(IRightShift rightShift) {
    }

    /// <summary>
    /// Performs some computation with the given stack array create expression.
    /// </summary>
    /// <param name="stackArrayCreate"></param>
    public virtual void Visit(IStackArrayCreate stackArrayCreate) {
    }

    /// <summary>
    /// Performs some computation with the given runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression"></param>
    public virtual void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
    }

    /// <summary>
    /// Performs some computation with the given sizeof() expression.
    /// </summary>
    /// <param name="sizeOf"></param>
    public virtual void Visit(ISizeOf sizeOf) {
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
    public virtual void Visit(IStatement statement) {
    }

    /// <summary>
    /// Performs some computation with the given subtraction expression.
    /// </summary>
    /// <param name="subtraction"></param>
    public virtual void Visit(ISubtraction subtraction) {
    }

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
    public virtual void Visit(IEnumerable<ISwitchCase> switchCases) {
    }

    /// <summary>
    /// Performs some computation with the given switch case.
    /// </summary>
    /// <param name="switchCase"></param>
    public virtual void Visit(ISwitchCase switchCase) {
    }

    /// <summary>
    /// Performs some computation with the given switch statement.
    /// </summary>
    /// <param name="switchStatement"></param>
    public virtual void Visit(ISwitchStatement switchStatement) {
    }

    /// <summary>
    /// Performs some computation with the given target expression.
    /// </summary>
    /// <param name="targetExpression"></param>
    public virtual void Visit(ITargetExpression targetExpression) {
    }

    /// <summary>
    /// Performs some computation with the given this reference expression.
    /// </summary>
    /// <param name="thisReference"></param>
    public virtual void Visit(IThisReference thisReference) {
    }

    /// <summary>
    /// Performs some computation with the throw statement.
    /// </summary>
    /// <param name="throwStatement"></param>
    public virtual void Visit(IThrowStatement throwStatement) {
    }

    /// <summary>
    /// Performs some computation with the try-catch-filter-finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement"></param>
    public virtual void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
    }

    /// <summary>
    /// Performs some computation with the given tokenof() expression.
    /// </summary>
    /// <param name="tokenOf"></param>
    public virtual void Visit(ITokenOf tokenOf) {
    }

    /// <summary>
    /// Performs some computation with the given typeof() expression.
    /// </summary>
    /// <param name="typeOf"></param>
    public virtual void Visit(ITypeOf typeOf) {
    }

    /// <summary>
    /// Performs some computation with the given unary negation expression.
    /// </summary>
    /// <param name="unaryNegation"></param>
    public virtual void Visit(IUnaryNegation unaryNegation) {
    }

    /// <summary>
    /// Performs some computation with the given unary plus expression.
    /// </summary>
    /// <param name="unaryPlus"></param>
    public virtual void Visit(IUnaryPlus unaryPlus) {
    }

    /// <summary>
    /// Performs some computation with the given vector length expression.
    /// </summary>
    /// <param name="vectorLength"></param>
    public virtual void Visit(IVectorLength vectorLength) {
    }

    /// <summary>
    /// Performs some computation with the given while do statement.
    /// </summary>
    /// <param name="whileDoStatement"></param>
    public virtual void Visit(IWhileDoStatement whileDoStatement) {
    }

    /// <summary>
    /// Performs some computation with the given yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public virtual void Visit(IYieldBreakStatement yieldBreakStatement) {
    }

    /// <summary>
    /// Performs some computation with the given yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement"></param>
    public virtual void Visit(IYieldReturnStatement yieldReturnStatement) {
    }

    #endregion
  }

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
    /// Performs some computation with the given loop invariant.
    /// </summary>
    void Visit(ILoopVariant loopVariant);

    /// <summary>
    /// Performs some computation with the given method contract.
    /// </summary>
    void Visit(IMethodContract methodContract);

    /// <summary>
    /// Performs some computation with the given method contract.
    /// </summary>
    void Visit(IMethodVariant methodVariant);

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
      this.Visit(loopContract.Variants);
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
      if (loopInvariant.Description != null)
        this.Visit(loopInvariant.Description);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of loop variants.
    /// </summary>
    public virtual void Visit(IEnumerable<ILoopVariant> loopVariants)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(loopVariants);
      foreach (var loopVariant in loopVariants)
        this.Visit(loopVariant);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given loop variant.
    /// </summary>
    public virtual void Visit(ILoopVariant loopVariant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(loopVariant);
      this.Visit(loopVariant.Condition);
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
      if (postCondition.Description != null)
        this.Visit(postCondition.Description);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given list of post conditions.
    /// </summary>
    public virtual void Visit(IEnumerable<IMethodVariant> variants)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(variants);
      foreach (var variant in variants)
        this.Visit(variant);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given variant.
    /// </summary>
    public virtual void Visit(IMethodVariant variant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(variant);
      this.Visit(variant.Condition);
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
      if (precondition.Description != null)
        this.Visit(precondition.Description);
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
      this.Visit(thrownException.Postcondition);
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
      if (typeInvariant.Description != null)
        this.Visit(typeInvariant.Description);
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
    public virtual void Visit(ILoopContract loopContract) {
    }

    /// <summary>
    /// Visits the given loop invariant.
    /// </summary>
    public virtual void Visit(ILoopInvariant loopInvariant) {
    }

    /// <summary>
    /// Visits the given loop variant.
    /// </summary>
    public virtual void Visit(ILoopVariant loopVariant) {
    }

    /// <summary>
    /// Visits the given method contract.
    /// </summary>
    public virtual void Visit(IMethodContract methodContract) {
    }

    /// <summary>
    /// Visits the given variant.
    /// </summary>
    public virtual void Visit(IMethodVariant variant) {
    }

    /// <summary>
    /// Visits the given postCondition.
    /// </summary>
    public virtual void Visit(IPostcondition postCondition) {
    }

    /// <summary>
    /// Visits the given precondition.
    /// </summary>
    public virtual void Visit(IPrecondition precondition) {
    }

    /// <summary>
    /// Visits the given thrown exception.
    /// </summary>
    public virtual void Visit(IThrownException thrownException) {
    }

    /// <summary>
    /// Visits the given type contract.
    /// </summary>
    public virtual void Visit(ITypeContract typeContract) {
    }

    /// <summary>
    /// Visits the given type invariant.
    /// </summary>
    public virtual void Visit(ITypeInvariant typeInvariant) {
    }

  }

}

