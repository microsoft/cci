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
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;
using System.Diagnostics.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A class that traverses a mutable code and metadata model in depth first, left to right order,
  /// rewriting each mutable node it visits by updating the node's children with recursivly rewritten nodes.
  /// </summary>
  public class CodeRewriter : MetadataRewriter {

    /// <summary>
    /// A class that traverses a mutable code and metadata model in depth first, left to right order,
    /// rewriting each mutable node it visits by updating the node's children with recursivly rewritten nodes.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this rewriter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyAndRewriteImmutableReferences">
    /// If true, the rewriter replaces frozen or immutable references with shallow copies.
    /// Mutable method definitions that are being used as method references are considered to be "frozen" and so also
    /// get copied.
    /// </param>
    public CodeRewriter(IMetadataHost host, bool copyAndRewriteImmutableReferences = false)
      : base(host, copyAndRewriteImmutableReferences) {
      this.dispatchingVisitor = new Dispatcher() { rewriter = this };
    }

    Dispatcher dispatchingVisitor;
    class Dispatcher : MetadataVisitor, ICodeVisitor {

      internal CodeRewriter rewriter;
      internal object result;

      public void Visit(IAddition addition) {
        this.result = this.rewriter.Rewrite(addition);
      }

      public void Visit(IAddressableExpression addressableExpression) {
        this.result = this.rewriter.Rewrite(addressableExpression);
      }

      public void Visit(IAddressDereference addressDereference) {
        this.result = this.rewriter.Rewrite(addressDereference);
      }

      public void Visit(IAddressOf addressOf) {
        this.result = this.rewriter.Rewrite(addressOf);
      }

      public void Visit(IAnonymousDelegate anonymousDelegate) {
        this.result = this.rewriter.Rewrite(anonymousDelegate);
      }

      public void Visit(IArrayIndexer arrayIndexer) {
        this.result = this.rewriter.Rewrite(arrayIndexer);
      }

      public void Visit(IAssertStatement assertStatement) {
        this.result = this.rewriter.Rewrite(assertStatement);
      }

      public void Visit(IAssignment assignment) {
        this.result = this.rewriter.Rewrite(assignment);
      }

      public void Visit(IAssumeStatement assumeStatement) {
        this.result = this.rewriter.Rewrite(assumeStatement);
      }

      public void Visit(IBitwiseAnd bitwiseAnd) {
        this.result = this.rewriter.Rewrite(bitwiseAnd);
      }

      public void Visit(IBitwiseOr bitwiseOr) {
        this.result = this.rewriter.Rewrite(bitwiseOr);
      }

      public void Visit(IBlockExpression blockExpression) {
        this.result = this.rewriter.Rewrite(blockExpression);
      }

      public void Visit(IBlockStatement block) {
        this.result = this.rewriter.Rewrite(block);
      }

      public void Visit(IBreakStatement breakStatement) {
        this.result = this.rewriter.Rewrite(breakStatement);
      }

      public void Visit(IBoundExpression boundExpression) {
        this.result = this.rewriter.Rewrite(boundExpression);
      }

      public void Visit(ICastIfPossible castIfPossible) {
        this.result = this.rewriter.Rewrite(castIfPossible);
      }

      public void Visit(ICatchClause catchClause) {
        this.result = this.rewriter.Rewrite(catchClause);
      }

      public void Visit(ICheckIfInstance checkIfInstance) {
        this.result = this.rewriter.Rewrite(checkIfInstance);
      }

      public void Visit(ICompileTimeConstant constant) {
        this.result = this.rewriter.Rewrite(constant);
      }

      public void Visit(IConversion conversion) {
        this.result = this.rewriter.Rewrite(conversion);
      }

      public void Visit(IConditional conditional) {
        this.result = this.rewriter.Rewrite(conditional);
      }

      public void Visit(IConditionalStatement conditionalStatement) {
        this.result = this.rewriter.Rewrite(conditionalStatement);
      }

      public void Visit(IContinueStatement continueStatement) {
        this.result = this.rewriter.Rewrite(continueStatement);
      }

      public void Visit(ICopyMemoryStatement copyMemoryBlock) {
        this.result = this.rewriter.Rewrite(copyMemoryBlock);
      }

      public void Visit(ICreateArray createArray) {
        this.result = this.rewriter.Rewrite(createArray);
      }

      public void Visit(ICreateDelegateInstance createDelegateInstance) {
        this.result = this.rewriter.Rewrite(createDelegateInstance);
      }

      public void Visit(ICreateObjectInstance createObjectInstance) {
        this.result = this.rewriter.Rewrite(createObjectInstance);
      }

      public void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        this.result = this.rewriter.Rewrite(debuggerBreakStatement);
      }

      public void Visit(IDefaultValue defaultValue) {
        this.result = this.rewriter.Rewrite(defaultValue);
      }

      public void Visit(IDivision division) {
        this.result = this.rewriter.Rewrite(division);
      }

      public void Visit(IDoUntilStatement doUntilStatement) {
        this.result = this.rewriter.Rewrite(doUntilStatement);
      }

      public void Visit(IDupValue dupValue) {
        this.result = this.rewriter.Rewrite((DupValue)dupValue);
      }

      public void Visit(IEmptyStatement emptyStatement) {
        this.result = this.rewriter.Rewrite((EmptyStatement)emptyStatement);
      }

      public void Visit(IEquality equality) {
        this.result = this.rewriter.Rewrite(equality);
      }

      public void Visit(IExclusiveOr exclusiveOr) {
        this.result = this.rewriter.Rewrite(exclusiveOr);
      }

      public void Visit(IExpressionStatement expressionStatement) {
        this.result = this.rewriter.Rewrite(expressionStatement);
      }

      public void Visit(IFillMemoryStatement fillMemoryStatement) {
        this.result = this.rewriter.Rewrite(fillMemoryStatement);
      }

      public void Visit(IForEachStatement forEachStatement) {
        this.result = this.rewriter.Rewrite((ForEachStatement)forEachStatement);
      }

      public void Visit(IForStatement forStatement) {
        this.result = this.rewriter.Rewrite(forStatement);
      }

      public void Visit(IGotoStatement gotoStatement) {
        this.result = this.rewriter.Rewrite(gotoStatement);
      }

      public void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        this.result = this.rewriter.Rewrite(gotoSwitchCaseStatement);
      }

      public void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        this.result = this.rewriter.Rewrite(getTypeOfTypedReference);
      }

      public void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        this.result = this.rewriter.Rewrite(getValueOfTypedReference);
      }

      public void Visit(IGreaterThan greaterThan) {
        this.result = this.rewriter.Rewrite(greaterThan);
      }

      public void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        this.result = this.rewriter.Rewrite(greaterThanOrEqual);
      }

      public void Visit(ILabeledStatement labeledStatement) {
        this.result = this.rewriter.Rewrite((LabeledStatement)labeledStatement);
      }

      public void Visit(ILeftShift leftShift) {
        this.result = this.rewriter.Rewrite(leftShift);
      }

      public void Visit(ILessThan lessThan) {
        this.result = this.rewriter.Rewrite(lessThan);
      }

      public void Visit(ILessThanOrEqual lessThanOrEqual) {
        this.result = this.rewriter.Rewrite(lessThanOrEqual);
      }

      public void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        this.result = this.rewriter.Rewrite(localDeclarationStatement);
      }

      public void Visit(ILockStatement lockStatement) {
        this.result = this.rewriter.Rewrite(lockStatement);
      }

      public void Visit(ILogicalNot logicalNot) {
        this.result = this.rewriter.Rewrite(logicalNot);
      }

      public void Visit(IMakeTypedReference makeTypedReference) {
        this.result = this.rewriter.Rewrite(makeTypedReference);
      }

      public void Visit(IMethodCall methodCall) {
        this.result = this.rewriter.Rewrite(methodCall);
      }

      public void Visit(IModulus modulus) {
        this.result = this.rewriter.Rewrite(modulus);
      }

      public void Visit(IMultiplication multiplication) {
        this.result = this.rewriter.Rewrite(multiplication);
      }

      public void Visit(INamedArgument namedArgument) {
        this.result = this.rewriter.Rewrite(namedArgument);
      }

      public void Visit(INotEquality notEquality) {
        this.result = this.rewriter.Rewrite((NotEquality)notEquality);
      }

      public void Visit(IOldValue oldValue) {
        this.result = this.rewriter.Rewrite(oldValue);
      }

      public void Visit(IOnesComplement onesComplement) {
        this.result = this.rewriter.Rewrite(onesComplement);
      }

      public void Visit(IOutArgument outArgument) {
        this.result = this.rewriter.Rewrite(outArgument);
      }

      public void Visit(IPointerCall pointerCall) {
        this.result = this.rewriter.Rewrite(pointerCall);
      }

      public void Visit(IPopValue popValue) {
        this.result = this.rewriter.Rewrite(popValue);
      }

      public void Visit(IPushStatement pushStatement) {
        this.result = this.rewriter.Rewrite(pushStatement);
      }

      public void Visit(IRefArgument refArgument) {
        this.result = this.rewriter.Rewrite(refArgument);
      }

      public void Visit(IResourceUseStatement resourceUseStatement) {
        this.result = this.rewriter.Rewrite(resourceUseStatement);
      }

      public void Visit(IReturnValue returnValue) {
        this.result = this.rewriter.Rewrite(returnValue);
      }

      public void Visit(IRethrowStatement rethrowStatement) {
        this.result = this.rewriter.Rewrite(rethrowStatement);
      }

      public void Visit(IReturnStatement returnStatement) {
        this.result = this.rewriter.Rewrite(returnStatement);
      }

      public void Visit(IRightShift rightShift) {
        this.result = this.rewriter.Rewrite(rightShift);
      }

      public void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        this.result = this.rewriter.Rewrite(runtimeArgumentHandleExpression);
      }

      public void Visit(ISizeOf sizeOf) {
        this.result = this.rewriter.Rewrite(sizeOf);
      }

      public void Visit(IStackArrayCreate stackArrayCreate) {
        this.result = this.rewriter.Rewrite(stackArrayCreate);
      }

      public void Visit(ISubtraction subtraction) {
        this.result = this.rewriter.Rewrite(subtraction);
      }

      public void Visit(ISwitchCase switchCase) {
        this.result = this.rewriter.Rewrite(switchCase);
      }

      public void Visit(ISwitchStatement switchStatement) {
        this.result = this.rewriter.Rewrite(switchStatement);
      }

      public void Visit(ITargetExpression targetExpression) {
        this.result = this.rewriter.Rewrite(targetExpression);
      }

      public void Visit(IThisReference thisReference) {
        this.result = this.rewriter.Rewrite(thisReference);
      }

      public void Visit(IThrowStatement throwStatement) {
        this.result = this.rewriter.Rewrite(throwStatement);
      }

      public void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        this.result = this.rewriter.Rewrite(tryCatchFilterFinallyStatement);
      }

      public void Visit(ITokenOf tokenOf) {
        this.result = this.rewriter.Rewrite(tokenOf);
      }

      public void Visit(ITypeOf typeOf) {
        this.result = this.rewriter.Rewrite(typeOf);
      }

      public void Visit(IUnaryNegation unaryNegation) {
        this.result = this.rewriter.Rewrite(unaryNegation);
      }

      public void Visit(IUnaryPlus unaryPlus) {
        this.result = this.rewriter.Rewrite(unaryPlus);
      }

      public void Visit(IVectorLength vectorLength) {
        this.result = this.rewriter.Rewrite(vectorLength);
      }

      public void Visit(IWhileDoStatement whileDoStatement) {
        this.result = this.rewriter.Rewrite(whileDoStatement);
      }

      public void Visit(IYieldBreakStatement yieldBreakStatement) {
        this.result = this.rewriter.Rewrite(yieldBreakStatement);
      }

      public void Visit(IYieldReturnStatement yieldReturnStatement) {
        this.result = this.rewriter.Rewrite(yieldReturnStatement);
      }

    }

    /// <summary>
    /// Rewrites the given addition.
    /// </summary>
    /// <param name="addition"></param>
    public virtual IExpression Rewrite(IAddition addition) {
      Contract.Requires(addition != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableAddition = addition as Addition;
      if (mutableAddition == null) return addition;
      this.RewriteChildren(mutableAddition);
      return mutableAddition;
    }

    /// <summary>
    /// Rewrites the given addressable expression.
    /// </summary>
    /// <param name="addressableExpression"></param>
    public virtual IAddressableExpression Rewrite(IAddressableExpression addressableExpression) {
      Contract.Requires(addressableExpression != null);
      Contract.Ensures(Contract.Result<IAddressableExpression>() != null);

      var mutableAddressableExpression = addressableExpression as AddressableExpression;
      if (mutableAddressableExpression == null) return addressableExpression;
      this.RewriteChildren(mutableAddressableExpression);
      return mutableAddressableExpression;
    }

    /// <summary>
    /// Rewrites the given address dereference expression.
    /// </summary>
    /// <param name="addressDereference"></param>
    public virtual IExpression Rewrite(IAddressDereference addressDereference) {
      Contract.Requires(addressDereference != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableAddressDereference = addressDereference as AddressDereference;
      if (mutableAddressDereference == null) return addressDereference;
      this.RewriteChildren(mutableAddressDereference);
      return mutableAddressDereference;
    }

    /// <summary>
    /// Rewrites the given AddressOf expression.
    /// </summary>
    /// <param name="addressOf"></param>
    public virtual IExpression Rewrite(IAddressOf addressOf) {
      Contract.Requires(addressOf != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableAddressOf = addressOf as AddressOf;
      if (mutableAddressOf == null) return addressOf;
      this.RewriteChildren(mutableAddressOf);
      return mutableAddressOf;
    }

    /// <summary>
    /// Rewrites the given anonymous delegate expression.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
    public virtual IExpression Rewrite(IAnonymousDelegate anonymousDelegate) {
      Contract.Requires(anonymousDelegate != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableAnonymousDelegate = anonymousDelegate as AnonymousDelegate;
      if (mutableAnonymousDelegate == null) return anonymousDelegate;
      this.RewriteChildren(mutableAnonymousDelegate);
      return mutableAnonymousDelegate;
    }

    /// <summary>
    /// Rewrites the given array indexer expression.
    /// </summary>
    /// <param name="arrayIndexer"></param>
    public virtual IExpression Rewrite(IArrayIndexer arrayIndexer) {
      Contract.Requires(arrayIndexer != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableArrayIndexer = arrayIndexer as ArrayIndexer;
      if (mutableArrayIndexer == null) return arrayIndexer;
      this.RewriteChildren(mutableArrayIndexer);
      return mutableArrayIndexer;
    }

    /// <summary>
    /// Rewrites the given assert statement.
    /// </summary>
    /// <param name="assertStatement"></param>
    public virtual IStatement Rewrite(IAssertStatement assertStatement) {
      Contract.Requires(assertStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableAssertStatement = assertStatement as AssertStatement;
      if (mutableAssertStatement == null) return assertStatement;
      this.RewriteChildren(mutableAssertStatement);
      return mutableAssertStatement;
    }

    /// <summary>
    /// Rewrites the given assignment expression.
    /// </summary>
    /// <param name="assignment"></param>
    public virtual IExpression Rewrite(IAssignment assignment) {
      Contract.Requires(assignment != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableAssignment = assignment as Assignment;
      if (mutableAssignment == null) return assignment;
      this.RewriteChildren(mutableAssignment);
      return mutableAssignment;
    }

    /// <summary>
    /// Rewrites the given assume statement.
    /// </summary>
    /// <param name="assumeStatement"></param>
    public virtual IStatement Rewrite(IAssumeStatement assumeStatement) {
      Contract.Requires(assumeStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableAssumeStatement = assumeStatement as AssumeStatement;
      if (mutableAssumeStatement == null) return assumeStatement;
      this.RewriteChildren(mutableAssumeStatement);
      return mutableAssumeStatement;
    }

    /// <summary>
    /// Rewrites the given bitwise and expression.
    /// </summary>
    /// <param name="binaryOperation"></param>
    public virtual IExpression Rewrite(IBinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      binaryOperation.Dispatch(this.dispatchingVisitor);
      return (IBinaryOperation)this.dispatchingVisitor.result;
    }

    /// <summary>
    /// Rewrites the given bitwise and expression.
    /// </summary>
    /// <param name="bitwiseAnd"></param>
    public virtual IExpression Rewrite(IBitwiseAnd bitwiseAnd) {
      Contract.Requires(bitwiseAnd != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableBitwiseAnd = bitwiseAnd as BitwiseAnd;
      if (mutableBitwiseAnd == null) return bitwiseAnd;
      this.RewriteChildren(mutableBitwiseAnd);
      return mutableBitwiseAnd;
    }

    /// <summary>
    /// Rewrites the given bitwise or expression.
    /// </summary>
    /// <param name="bitwiseOr"></param>
    public virtual IExpression Rewrite(IBitwiseOr bitwiseOr) {
      Contract.Requires(bitwiseOr != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableBitwiseOr = bitwiseOr as BitwiseOr;
      if (mutableBitwiseOr == null) return bitwiseOr;
      this.RewriteChildren(mutableBitwiseOr);
      return mutableBitwiseOr;
    }

    /// <summary>
    /// Rewrites the given block expression.
    /// </summary>
    /// <param name="blockExpression"></param>
    public virtual IExpression Rewrite(IBlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableBlockExpression = blockExpression as BlockExpression;
      if (mutableBlockExpression == null) return blockExpression;
      this.RewriteChildren(mutableBlockExpression);
      return mutableBlockExpression;
    }

    /// <summary>
    /// Rewrites the given statement block.
    /// </summary>
    /// <param name="block"></param>
    public virtual IBlockStatement Rewrite(IBlockStatement block) {
      Contract.Requires(block != null);
      Contract.Ensures(Contract.Result<IBlockStatement>() != null);

      var mutableBlockStatement = block as BlockStatement;
      if (mutableBlockStatement == null) return block;
      this.RewriteChildren(mutableBlockStatement);
      return mutableBlockStatement;
    }

    /// <summary>
    /// Rewrites the given bound expression.
    /// </summary>
    /// <param name="boundExpression"></param>
    public virtual IExpression Rewrite(IBoundExpression boundExpression) {
      Contract.Requires(boundExpression != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableBoundExpression = boundExpression as BoundExpression;
      if (mutableBoundExpression == null) return boundExpression;
      this.RewriteChildren(mutableBoundExpression);
      return mutableBoundExpression;
    }

    /// <summary>
    /// Rewrites the given break statement.
    /// </summary>
    /// <param name="breakStatement"></param>
    public virtual IStatement Rewrite(IBreakStatement breakStatement) {
      Contract.Requires(breakStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableBreakStatement = breakStatement as BreakStatement;
      if (mutableBreakStatement == null) return breakStatement;
      this.RewriteChildren(mutableBreakStatement);
      return mutableBreakStatement;
    }

    /// <summary>
    /// Rewrites the cast-if-possible expression.
    /// </summary>
    /// <param name="castIfPossible"></param>
    public virtual IExpression Rewrite(ICastIfPossible castIfPossible) {
      Contract.Requires(castIfPossible != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableCastIfPossible = castIfPossible as CastIfPossible;
      if (mutableCastIfPossible == null) return castIfPossible;
      this.RewriteChildren(mutableCastIfPossible);
      return mutableCastIfPossible;
    }

    /// <summary>
    /// Rewrites the given catch clause.
    /// </summary>
    /// <param name="catchClause"></param>
    public virtual ICatchClause Rewrite(ICatchClause catchClause) {
      Contract.Requires(catchClause != null);
      Contract.Ensures(Contract.Result<ICatchClause>() != null);

      var mutableCatchClause = catchClause as CatchClause;
      if (mutableCatchClause == null) return catchClause;
      this.RewriteChildren(mutableCatchClause);
      return mutableCatchClause;
    }

    /// <summary>
    /// Rewrites the given check-if-instance expression.
    /// </summary>
    /// <param name="checkIfInstance"></param>
    public virtual IExpression Rewrite(ICheckIfInstance checkIfInstance) {
      Contract.Requires(checkIfInstance != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableCheckIfInstance = checkIfInstance as CheckIfInstance;
      if (mutableCheckIfInstance == null) return checkIfInstance;
      this.RewriteChildren(mutableCheckIfInstance);
      return mutableCheckIfInstance;
    }

    /// <summary>
    /// Rewrites the given compile time constant.
    /// </summary>
    /// <param name="constant"></param>
    public virtual ICompileTimeConstant Rewrite(ICompileTimeConstant constant) {
      Contract.Requires(constant != null);
      Contract.Ensures(Contract.Result<ICompileTimeConstant>() != null);

      var mutableCompileTimeConstant = constant as CompileTimeConstant;
      if (mutableCompileTimeConstant == null) return constant;
      this.RewriteChildren(mutableCompileTimeConstant);
      return mutableCompileTimeConstant;
    }

    /// <summary>
    /// Rewrites the given conditional expression.
    /// </summary>
    /// <param name="conditional"></param>
    public virtual IExpression Rewrite(IConditional conditional) {
      Contract.Requires(conditional != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableConditional = conditional as Conditional;
      if (mutableConditional == null) return conditional;
      this.RewriteChildren(mutableConditional);
      return mutableConditional;
    }

    /// <summary>
    /// Rewrites the given conditional statement.
    /// </summary>
    /// <param name="conditionalStatement"></param>
    public virtual IStatement Rewrite(IConditionalStatement conditionalStatement) {
      Contract.Requires(conditionalStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableConditionalStatement = conditionalStatement as ConditionalStatement;
      if (mutableConditionalStatement == null) return conditionalStatement;
      this.RewriteChildren(mutableConditionalStatement);
      return mutableConditionalStatement;
    }

    /// <summary>
    /// Rewrites the given continue statement.
    /// </summary>
    /// <param name="continueStatement"></param>
    public virtual IStatement Rewrite(IContinueStatement continueStatement) {
      Contract.Requires(continueStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableContinueStatement = continueStatement as ContinueStatement;
      if (mutableContinueStatement == null) return continueStatement;
      this.RewriteChildren(mutableContinueStatement);
      return mutableContinueStatement;
    }

    /// <summary>
    /// Rewrites the given conversion expression.
    /// </summary>
    /// <param name="conversion"></param>
    public virtual IExpression Rewrite(IConversion conversion) {
      Contract.Requires(conversion != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableConversion = conversion as Conversion;
      if (mutableConversion == null) return conversion;
      this.RewriteChildren(mutableConversion);
      return mutableConversion;
    }

    /// <summary>
    /// Rewrites the given copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public virtual IStatement Rewrite(ICopyMemoryStatement copyMemoryStatement) {
      Contract.Requires(copyMemoryStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableCopyMemoryStatement = copyMemoryStatement as CopyMemoryStatement;
      if (mutableCopyMemoryStatement == null) return copyMemoryStatement;
      this.RewriteChildren(mutableCopyMemoryStatement);
      return mutableCopyMemoryStatement;
    }

    /// <summary>
    /// Rewrites the given array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
    public virtual IExpression Rewrite(ICreateArray createArray) {
      Contract.Requires(createArray != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableCreateArray = createArray as CreateArray;
      if (mutableCreateArray == null) return createArray;
      this.RewriteChildren(mutableCreateArray);
      return mutableCreateArray;
    }

    /// <summary>
    /// Rewrites the anonymous object creation expression.
    /// </summary>
    /// <param name="createDelegateInstance"></param>
    public virtual IExpression Rewrite(ICreateDelegateInstance createDelegateInstance) {
      Contract.Requires(createDelegateInstance != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableCreateDelegateInstance = createDelegateInstance as CreateDelegateInstance;
      if (mutableCreateDelegateInstance == null) return createDelegateInstance;
      this.RewriteChildren(mutableCreateDelegateInstance);
      return mutableCreateDelegateInstance;
    }

    /// <summary>
    /// Rewrites the given constructor call expression.
    /// </summary>
    /// <param name="createObjectInstance"></param>
    public virtual IExpression Rewrite(ICreateObjectInstance createObjectInstance) {
      Contract.Requires(createObjectInstance != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableCreateObjectInstance = createObjectInstance as CreateObjectInstance;
      if (mutableCreateObjectInstance == null) return createObjectInstance;
      this.RewriteChildren(mutableCreateObjectInstance);
      return mutableCreateObjectInstance;
    }

    /// <summary>
    /// Rewrites the given debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement"></param>
    public virtual IStatement Rewrite(IDebuggerBreakStatement debuggerBreakStatement) {
      Contract.Requires(debuggerBreakStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableDebuggerBreakStatement = debuggerBreakStatement as DebuggerBreakStatement;
      if (mutableDebuggerBreakStatement == null) return debuggerBreakStatement;
      this.RewriteChildren(mutableDebuggerBreakStatement);
      return mutableDebuggerBreakStatement;
    }

    /// <summary>
    /// Rewrites the given defalut value expression.
    /// </summary>
    /// <param name="defaultValue"></param>
    public virtual IExpression Rewrite(IDefaultValue defaultValue) {
      Contract.Requires(defaultValue != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableDefaultValue = defaultValue as DefaultValue;
      if (mutableDefaultValue == null) return defaultValue;
      this.RewriteChildren(mutableDefaultValue);
      return mutableDefaultValue;
    }

    /// <summary>
    /// Rewrites the given division expression.
    /// </summary>
    /// <param name="division"></param>
    public virtual IExpression Rewrite(IDivision division) {
      Contract.Requires(division != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableDivision = division as Division;
      if (mutableDivision == null) return division;
      this.RewriteChildren(mutableDivision);
      return mutableDivision;
    }

    /// <summary>
    /// Rewrites the given do until statement.
    /// </summary>
    /// <param name="doUntilStatement"></param>
    public virtual IStatement Rewrite(IDoUntilStatement doUntilStatement) {
      Contract.Requires(doUntilStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableDoUntilStatement = doUntilStatement as DoUntilStatement;
      if (mutableDoUntilStatement == null) return doUntilStatement;
      this.RewriteChildren(mutableDoUntilStatement);
      return mutableDoUntilStatement;
    }

    /// <summary>
    /// Rewrites the given dup value expression.
    /// </summary>
    /// <param name="dupValue"></param>
    public virtual IExpression Rewrite(IDupValue dupValue) {
      Contract.Requires(dupValue != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableDupValue = dupValue as DupValue;
      if (mutableDupValue == null) return dupValue;
      this.RewriteChildren(mutableDupValue);
      return mutableDupValue;
    }

    /// <summary>
    /// Rewrites the given empty statement.
    /// </summary>
    /// <param name="emptyStatement"></param>
    public virtual IStatement Rewrite(IEmptyStatement emptyStatement) {
      Contract.Requires(emptyStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableEmptyStatement = emptyStatement as EmptyStatement;
      if (mutableEmptyStatement == null) return emptyStatement;
      this.RewriteChildren(mutableEmptyStatement);
      return mutableEmptyStatement;
    }

    /// <summary>
    /// Rewrites the given equality expression.
    /// </summary>
    /// <param name="equality"></param>
    public virtual IExpression Rewrite(IEquality equality) {
      Contract.Requires(equality != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableEquality = equality as Equality;
      if (mutableEquality == null) return equality;
      this.RewriteChildren(mutableEquality);
      return mutableEquality;
    }

    /// <summary>
    /// Rewrites the given exclusive or expression.
    /// </summary>
    /// <param name="exclusiveOr"></param>
    public virtual IExpression Rewrite(IExclusiveOr exclusiveOr) {
      Contract.Requires(exclusiveOr != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableExclusiveOr = exclusiveOr as ExclusiveOr;
      if (mutableExclusiveOr == null) return exclusiveOr;
      this.RewriteChildren(mutableExclusiveOr);
      return mutableExclusiveOr;
    }

    /// <summary>
    /// Rewrites the given expression.
    /// </summary>
    /// <param name="expression"></param>
    public virtual IExpression Rewrite(IExpression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      expression.Dispatch(this.dispatchingVisitor);
      return (IExpression)this.dispatchingVisitor.result;
    }

    /// <summary>
    /// Rewrites the given expression statement.
    /// </summary>
    /// <param name="expressionStatement"></param>
    public virtual IStatement Rewrite(IExpressionStatement expressionStatement) {
      Contract.Requires(expressionStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableExpressionStatement = expressionStatement as ExpressionStatement;
      if (mutableExpressionStatement == null) return expressionStatement;
      this.RewriteChildren(mutableExpressionStatement);
      return mutableExpressionStatement;
    }

    /// <summary>
    /// Rewrites the given fill memory statement.
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    public virtual IStatement Rewrite(IFillMemoryStatement fillMemoryStatement) {
      Contract.Requires(fillMemoryStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableFillMemoryStatement = fillMemoryStatement as FillMemoryStatement;
      if (mutableFillMemoryStatement == null) return fillMemoryStatement;
      this.RewriteChildren(mutableFillMemoryStatement);
      return mutableFillMemoryStatement;
    }

    /// <summary>
    /// Rewrites the given foreach statement.
    /// </summary>
    /// <param name="forEachStatement"></param>
    public virtual IStatement Rewrite(IForEachStatement forEachStatement) {
      Contract.Requires(forEachStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableForEachStatement = forEachStatement as ForEachStatement;
      if (mutableForEachStatement == null) return forEachStatement;
      this.RewriteChildren(mutableForEachStatement);
      return mutableForEachStatement;
    }

    /// <summary>
    /// Rewrites the given for statement.
    /// </summary>
    /// <param name="forStatement"></param>
    public virtual IStatement Rewrite(IForStatement forStatement) {
      Contract.Requires(forStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableForStatement = forStatement as ForStatement;
      if (mutableForStatement == null) return forStatement;
      this.RewriteChildren(mutableForStatement);
      return mutableForStatement;
    }

    /// <summary>
    /// Rewrites the given get type of typed reference expression.
    /// </summary>
    /// <param name="getTypeOfTypedReference"></param>
    public virtual IExpression Rewrite(IGetTypeOfTypedReference getTypeOfTypedReference) {
      Contract.Requires(getTypeOfTypedReference != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableGetTypeOfTypedReference = getTypeOfTypedReference as GetTypeOfTypedReference;
      if (mutableGetTypeOfTypedReference == null) return getTypeOfTypedReference;
      this.RewriteChildren(mutableGetTypeOfTypedReference);
      return mutableGetTypeOfTypedReference;
    }

    /// <summary>
    /// Rewrites the given get value of typed reference expression.
    /// </summary>
    /// <param name="getValueOfTypedReference"></param>
    public virtual IExpression Rewrite(IGetValueOfTypedReference getValueOfTypedReference) {
      Contract.Requires(getValueOfTypedReference != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableGetValueOfTypedReference = getValueOfTypedReference as GetValueOfTypedReference;
      if (mutableGetValueOfTypedReference == null) return getValueOfTypedReference;
      this.RewriteChildren(mutableGetValueOfTypedReference);
      return mutableGetValueOfTypedReference;
    }

    /// <summary>
    /// Rewrites the given goto statement.
    /// </summary>
    /// <param name="gotoStatement"></param>
    public virtual IStatement Rewrite(IGotoStatement gotoStatement) {
      Contract.Requires(gotoStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableGotoStatement = gotoStatement as GotoStatement;
      if (mutableGotoStatement == null) return gotoStatement;
      this.RewriteChildren(mutableGotoStatement);
      return mutableGotoStatement;
    }

    /// <summary>
    /// Rewrites the given goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public virtual IStatement Rewrite(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      Contract.Requires(gotoSwitchCaseStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableGotoSwitchCaseStatement = gotoSwitchCaseStatement as GotoSwitchCaseStatement;
      if (mutableGotoSwitchCaseStatement == null) return gotoSwitchCaseStatement;
      this.RewriteChildren(mutableGotoSwitchCaseStatement);
      return mutableGotoSwitchCaseStatement;
    }

    /// <summary>
    /// Rewrites the given greater-than expression.
    /// </summary>
    /// <param name="greaterThan"></param>
    public virtual IExpression Rewrite(IGreaterThan greaterThan) {
      Contract.Requires(greaterThan != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableGreaterThan = greaterThan as GreaterThan;
      if (mutableGreaterThan == null) return greaterThan;
      this.RewriteChildren(mutableGreaterThan);
      return mutableGreaterThan;
    }

    /// <summary>
    /// Rewrites the given greater-than-or-equal expression.
    /// </summary>
    /// <param name="greaterThanOrEqual"></param>
    public virtual IExpression Rewrite(IGreaterThanOrEqual greaterThanOrEqual) {
      Contract.Requires(greaterThanOrEqual != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableGreaterThanOrEqual = greaterThanOrEqual as GreaterThanOrEqual;
      if (mutableGreaterThanOrEqual == null) return greaterThanOrEqual;
      this.RewriteChildren(mutableGreaterThanOrEqual);
      return mutableGreaterThanOrEqual;
    }

    /// <summary>
    /// Rewrites the given labeled statement.
    /// </summary>
    /// <param name="labeledStatement"></param>
    public virtual IStatement Rewrite(ILabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableLabeledStatement = labeledStatement as LabeledStatement;
      if (mutableLabeledStatement == null) return labeledStatement;
      this.RewriteChildren(mutableLabeledStatement);
      return mutableLabeledStatement;
    }

    /// <summary>
    /// Rewrites the given left shift expression.
    /// </summary>
    /// <param name="leftShift"></param>
    public virtual IExpression Rewrite(ILeftShift leftShift) {
      Contract.Requires(leftShift != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableLeftShift = leftShift as LeftShift;
      if (mutableLeftShift == null) return leftShift;
      this.RewriteChildren(mutableLeftShift);
      return mutableLeftShift;
    }

    /// <summary>
    /// Rewrites the given less-than expression.
    /// </summary>
    /// <param name="lessThan"></param>
    public virtual IExpression Rewrite(ILessThan lessThan) {
      Contract.Requires(lessThan != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableLessThan = lessThan as LessThan;
      if (mutableLessThan == null) return lessThan;
      this.RewriteChildren(mutableLessThan);
      return mutableLessThan;
    }

    /// <summary>
    /// Rewrites the given less-than-or-equal expression.
    /// </summary>
    /// <param name="lessThanOrEqual"></param>
    public virtual IExpression Rewrite(ILessThanOrEqual lessThanOrEqual) {
      Contract.Requires(lessThanOrEqual != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableLessThanOrEqual = lessThanOrEqual as LessThanOrEqual;
      if (mutableLessThanOrEqual == null) return lessThanOrEqual;
      this.RewriteChildren(mutableLessThanOrEqual);
      return mutableLessThanOrEqual;
    }

    /// <summary>
    /// Rewrites the given local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement"></param>
    public virtual IStatement Rewrite(ILocalDeclarationStatement localDeclarationStatement) {
      Contract.Requires(localDeclarationStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableLocalDeclarationStatement = localDeclarationStatement as LocalDeclarationStatement;
      if (mutableLocalDeclarationStatement == null) return localDeclarationStatement;
      this.RewriteChildren(mutableLocalDeclarationStatement);
      return mutableLocalDeclarationStatement;
    }

    /// <summary>
    /// Rewrites the given lock statement.
    /// </summary>
    /// <param name="lockStatement"></param>
    public virtual IStatement Rewrite(ILockStatement lockStatement) {
      Contract.Requires(lockStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableLockStatement = lockStatement as LockStatement;
      if (mutableLockStatement == null) return lockStatement;
      this.RewriteChildren(mutableLockStatement);
      return mutableLockStatement;
    }

    /// <summary>
    /// Rewrites the given logical not expression.
    /// </summary>
    /// <param name="logicalNot"></param>
    public virtual IExpression Rewrite(ILogicalNot logicalNot) {
      Contract.Requires(logicalNot != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableLogicalNot = logicalNot as LogicalNot;
      if (mutableLogicalNot == null) return logicalNot;
      this.RewriteChildren(mutableLogicalNot);
      return mutableLogicalNot;
    }

    /// <summary>
    /// Rewrites the given make typed reference expression.
    /// </summary>
    /// <param name="makeTypedReference"></param>
    public virtual IExpression Rewrite(IMakeTypedReference makeTypedReference) {
      Contract.Requires(makeTypedReference != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableMakeTypedReference = makeTypedReference as MakeTypedReference;
      if (mutableMakeTypedReference == null) return makeTypedReference;
      this.RewriteChildren(mutableMakeTypedReference);
      return mutableMakeTypedReference;
    }

    /// <summary>
    /// Rewrites the the given method body.
    /// </summary>
    /// <param name="methodBody"></param>
    public override IMethodBody Rewrite(IMethodBody methodBody) {
      Contract.Ensures(Contract.Result<IMethodBody>() != null);
      var sourceBody = methodBody as ISourceMethodBody;
      if (sourceBody != null) return this.Rewrite(sourceBody);
      return base.Rewrite(methodBody);
    }

    /// <summary>
    /// Rewrites the given method call.
    /// </summary>
    /// <param name="methodCall"></param>
    public virtual IExpression Rewrite(IMethodCall methodCall) {
      Contract.Requires(methodCall != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableMethodCall = methodCall as MethodCall;
      if (mutableMethodCall == null) return methodCall;
      this.RewriteChildren(mutableMethodCall);
      return mutableMethodCall;
    }

    /// <summary>
    /// Rewrites the given modulus expression.
    /// </summary>
    /// <param name="modulus"></param>
    public virtual IExpression Rewrite(IModulus modulus) {
      Contract.Requires(modulus != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableModulus = modulus as Modulus;
      if (mutableModulus == null) return modulus;
      this.RewriteChildren(mutableModulus);
      return mutableModulus;
    }

    /// <summary>
    /// Rewrites the given multiplication expression.
    /// </summary>
    /// <param name="multiplication"></param>
    public virtual IExpression Rewrite(IMultiplication multiplication) {
      Contract.Requires(multiplication != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableMultiplication = multiplication as Multiplication;
      if (mutableMultiplication == null) return multiplication;
      this.RewriteChildren(mutableMultiplication);
      return mutableMultiplication;
    }

    /// <summary>
    /// Rewrites the given named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
    public virtual IExpression Rewrite(INamedArgument namedArgument) {
      Contract.Requires(namedArgument != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableNamedArgument = namedArgument as NamedArgument;
      if (mutableNamedArgument == null) return namedArgument;
      this.RewriteChildren(mutableNamedArgument);
      return mutableNamedArgument;
    }

    /// <summary>
    /// Rewrites the given not equality expression.
    /// </summary>
    /// <param name="notEquality"></param>
    public virtual IExpression Rewrite(INotEquality notEquality) {
      Contract.Requires(notEquality != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableNotEquality = notEquality as NotEquality;
      if (mutableNotEquality == null) return notEquality;
      this.RewriteChildren(mutableNotEquality);
      return mutableNotEquality;
    }

    /// <summary>
    /// Rewrites the given old value expression.
    /// </summary>
    /// <param name="oldValue"></param>
    public virtual IExpression Rewrite(IOldValue oldValue) {
      Contract.Requires(oldValue != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableOldValue = oldValue as OldValue;
      if (mutableOldValue == null) return oldValue;
      this.RewriteChildren(mutableOldValue);
      return mutableOldValue;
    }

    /// <summary>
    /// Rewrites the given one's complement expression.
    /// </summary>
    /// <param name="onesComplement"></param>
    public virtual IExpression Rewrite(IOnesComplement onesComplement) {
      Contract.Requires(onesComplement != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableOnesComplement = onesComplement as OnesComplement;
      if (mutableOnesComplement == null) return onesComplement;
      this.RewriteChildren(mutableOnesComplement);
      return mutableOnesComplement;
    }

    /// <summary>
    /// Rewrites the given out argument expression.
    /// </summary>
    /// <param name="outArgument"></param>
    public virtual IExpression Rewrite(IOutArgument outArgument) {
      Contract.Requires(outArgument != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableOutArgument = outArgument as OutArgument;
      if (mutableOutArgument == null) return outArgument;
      this.RewriteChildren(mutableOutArgument);
      return mutableOutArgument;
    }

    /// <summary>
    /// Rewrites the given pointer call.
    /// </summary>
    /// <param name="pointerCall"></param>
    public virtual IExpression Rewrite(IPointerCall pointerCall) {
      Contract.Requires(pointerCall != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutablePointerCall = pointerCall as PointerCall;
      if (mutablePointerCall == null) return pointerCall;
      this.RewriteChildren(mutablePointerCall);
      return mutablePointerCall;
    }

    /// <summary>
    /// Rewrites the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public virtual IExpression Rewrite(IPopValue popValue) {
      Contract.Requires(popValue != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutablePopValue = popValue as PopValue;
      if (mutablePopValue == null) return popValue;
      this.RewriteChildren(mutablePopValue);
      return mutablePopValue;
    }

    /// <summary>
    /// Rewrites the given push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    public virtual IStatement Rewrite(IPushStatement pushStatement) {
      Contract.Requires(pushStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutablePushStatement = pushStatement as PushStatement;
      if (mutablePushStatement == null) return pushStatement;
      this.RewriteChildren(mutablePushStatement);
      return mutablePushStatement;
    }

    /// <summary>
    /// Rewrites the given ref argument expression.
    /// </summary>
    /// <param name="refArgument"></param>
    public virtual IExpression Rewrite(IRefArgument refArgument) {
      Contract.Requires(refArgument != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableRefArgument = refArgument as RefArgument;
      if (mutableRefArgument == null) return refArgument;
      this.RewriteChildren(mutableRefArgument);
      return mutableRefArgument;
    }

    /// <summary>
    /// Rewrites the given resource usage statement.
    /// </summary>
    /// <param name="resourceUseStatement"></param>
    public virtual IStatement Rewrite(IResourceUseStatement resourceUseStatement) {
      Contract.Requires(resourceUseStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableResourceUseStatement = resourceUseStatement as ResourceUseStatement;
      if (mutableResourceUseStatement == null) return resourceUseStatement;
      this.RewriteChildren(mutableResourceUseStatement);
      return mutableResourceUseStatement;
    }

    /// <summary>
    /// Rewrites the rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement"></param>
    public virtual IStatement Rewrite(IRethrowStatement rethrowStatement) {
      Contract.Requires(rethrowStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableRethrowStatement = rethrowStatement as RethrowStatement;
      if (mutableRethrowStatement == null) return rethrowStatement;
      this.RewriteChildren(mutableRethrowStatement);
      return mutableRethrowStatement;
    }

    /// <summary>
    /// Rewrites the return statement.
    /// </summary>
    /// <param name="returnStatement"></param>
    public virtual IStatement Rewrite(IReturnStatement returnStatement) {
      Contract.Requires(returnStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableReturnStatement = returnStatement as ReturnStatement;
      if (mutableReturnStatement == null) return returnStatement;
      this.RewriteChildren(mutableReturnStatement);
      return mutableReturnStatement;
    }

    /// <summary>
    /// Rewrites the given return value expression.
    /// </summary>
    /// <param name="returnValue"></param>
    public virtual IExpression Rewrite(IReturnValue returnValue) {
      Contract.Requires(returnValue != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableReturnValue = returnValue as ReturnValue;
      if (mutableReturnValue == null) return returnValue;
      this.RewriteChildren(mutableReturnValue);
      return mutableReturnValue;
    }

    /// <summary>
    /// Rewrites the given right shift expression.
    /// </summary>
    /// <param name="rightShift"></param>
    public virtual IExpression Rewrite(IRightShift rightShift) {
      Contract.Requires(rightShift != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableRightShift = rightShift as RightShift;
      if (mutableRightShift == null) return rightShift;
      this.RewriteChildren(mutableRightShift);
      return mutableRightShift;
    }

    /// <summary>
    /// Rewrites the given runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression"></param>
    public virtual IExpression Rewrite(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      Contract.Requires(runtimeArgumentHandleExpression != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableRuntimeArgumentHandleExpression = runtimeArgumentHandleExpression as RuntimeArgumentHandleExpression;
      if (mutableRuntimeArgumentHandleExpression == null) return runtimeArgumentHandleExpression;
      this.RewriteChildren(mutableRuntimeArgumentHandleExpression);
      return mutableRuntimeArgumentHandleExpression;
    }

    /// <summary>
    /// Rewrites the given sizeof() expression.
    /// </summary>
    /// <param name="sizeOf"></param>
    public virtual IExpression Rewrite(ISizeOf sizeOf) {
      Contract.Requires(sizeOf != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableSizeOf = sizeOf as SizeOf;
      if (mutableSizeOf == null) return sizeOf;
      this.RewriteChildren(mutableSizeOf);
      return mutableSizeOf;
    }

    /// <summary>
    /// Rewrites the given stack array create expression.
    /// </summary>
    /// <param name="stackArrayCreate"></param>
    public virtual IExpression Rewrite(IStackArrayCreate stackArrayCreate) {
      Contract.Requires(stackArrayCreate != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableStackArrayCreate = stackArrayCreate as StackArrayCreate;
      if (mutableStackArrayCreate == null) return stackArrayCreate;
      this.RewriteChildren(mutableStackArrayCreate);
      return mutableStackArrayCreate;
    }

    /// <summary>
    /// Rewrites the the given source method body.
    /// </summary>
    /// <param name="sourceMethodBody"></param>
    public virtual ISourceMethodBody Rewrite(ISourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      Contract.Ensures(Contract.Result<ISourceMethodBody>() != null);

      var mutableSourceMethodBody = sourceMethodBody as SourceMethodBody;
      if (mutableSourceMethodBody == null) return sourceMethodBody;
      this.RewriteChildren(mutableSourceMethodBody);
      return mutableSourceMethodBody;
    }

    /// <summary>
    /// Rewrites the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public virtual IStatement Rewrite(IStatement statement) {
      Contract.Requires(statement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      statement.Dispatch(this.dispatchingVisitor);
      return (IStatement)this.dispatchingVisitor.result;
    }

    /// <summary>
    /// Rewrites the given subtraction expression.
    /// </summary>
    /// <param name="subtraction"></param>
    public virtual IExpression Rewrite(ISubtraction subtraction) {
      Contract.Requires(subtraction != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableSubtraction = subtraction as Subtraction;
      if (mutableSubtraction == null) return subtraction;
      this.RewriteChildren(mutableSubtraction);
      return mutableSubtraction;
    }

    /// <summary>
    /// Rewrites the given switch case.
    /// </summary>
    /// <param name="switchCase"></param>
    public virtual ISwitchCase Rewrite(ISwitchCase switchCase) {
      Contract.Requires(switchCase != null);
      Contract.Ensures(Contract.Result<ISwitchCase>() != null);

      var mutableSwitchCase = switchCase as SwitchCase;
      if (mutableSwitchCase == null) return switchCase;
      this.RewriteChildren(mutableSwitchCase);
      return mutableSwitchCase;
    }

    /// <summary>
    /// Rewrites the given switch statement.
    /// </summary>
    /// <param name="switchStatement"></param>
    public virtual IStatement Rewrite(ISwitchStatement switchStatement) {
      Contract.Requires(switchStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableSwitchStatement = switchStatement as SwitchStatement;
      if (mutableSwitchStatement == null) return switchStatement;
      this.RewriteChildren(mutableSwitchStatement);
      return mutableSwitchStatement;
    }

    /// <summary>
    /// Rewrites the given target expression.
    /// </summary>
    /// <param name="targetExpression"></param>
    public virtual ITargetExpression Rewrite(ITargetExpression targetExpression) {
      Contract.Requires(targetExpression != null);
      Contract.Ensures(Contract.Result<ITargetExpression>() != null);

      var mutableTargetExpression = targetExpression as TargetExpression;
      if (mutableTargetExpression == null) return targetExpression;
      this.RewriteChildren(mutableTargetExpression);
      return mutableTargetExpression;
    }

    /// <summary>
    /// Rewrites the given this reference expression.
    /// </summary>
    /// <param name="thisReference"></param>
    public virtual IExpression Rewrite(IThisReference thisReference) {
      Contract.Requires(thisReference != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableThisReference = thisReference as ThisReference;
      if (mutableThisReference == null) return thisReference;
      this.RewriteChildren(mutableThisReference);
      return mutableThisReference;
    }

    /// <summary>
    /// Rewrites the throw statement.
    /// </summary>
    /// <param name="throwStatement"></param>
    public virtual IStatement Rewrite(IThrowStatement throwStatement) {
      Contract.Requires(throwStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableThrowStatement = throwStatement as ThrowStatement;
      if (mutableThrowStatement == null) return throwStatement;
      this.RewriteChildren(mutableThrowStatement);
      return mutableThrowStatement;
    }

    /// <summary>
    /// Rewrites the given tokenof() expression.
    /// </summary>
    /// <param name="tokenOf"></param>
    public virtual IExpression Rewrite(ITokenOf tokenOf) {
      Contract.Requires(tokenOf != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableTokenOf = tokenOf as TokenOf;
      if (mutableTokenOf == null) return tokenOf;
      this.RewriteChildren(mutableTokenOf);
      return mutableTokenOf;
    }

    /// <summary>
    /// Rewrites the try-catch-filter-finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement"></param>
    public virtual IStatement Rewrite(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      Contract.Requires(tryCatchFilterFinallyStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableTryCatchFinallyStatement = tryCatchFilterFinallyStatement as TryCatchFinallyStatement;
      if (mutableTryCatchFinallyStatement == null) return tryCatchFilterFinallyStatement;
      this.RewriteChildren(mutableTryCatchFinallyStatement);
      return mutableTryCatchFinallyStatement;
    }

    /// <summary>
    /// Rewrites the given typeof() expression.
    /// </summary>
    /// <param name="typeOf"></param>
    public virtual IExpression Rewrite(ITypeOf typeOf) {
      Contract.Requires(typeOf != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableTypeOf = typeOf as TypeOf;
      if (mutableTypeOf == null) return typeOf;
      this.RewriteChildren(mutableTypeOf);
      return mutableTypeOf;
    }

    /// <summary>
    /// Rewrites the given unary negation expression.
    /// </summary>
    /// <param name="unaryNegation"></param>
    public virtual IExpression Rewrite(IUnaryNegation unaryNegation) {
      Contract.Requires(unaryNegation != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableUnaryNegation = unaryNegation as UnaryNegation;
      if (mutableUnaryNegation == null) return unaryNegation;
      this.RewriteChildren(mutableUnaryNegation);
      return mutableUnaryNegation;
    }

    /// <summary>
    /// Rewrites the given unary plus expression.
    /// </summary>
    /// <param name="unaryPlus"></param>
    public virtual IExpression Rewrite(IUnaryPlus unaryPlus) {
      Contract.Requires(unaryPlus != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableUnaryPlus = unaryPlus as UnaryPlus;
      if (mutableUnaryPlus == null) return unaryPlus;
      this.RewriteChildren(mutableUnaryPlus);
      return mutableUnaryPlus;
    }

    /// <summary>
    /// Rewrites the given vector length expression.
    /// </summary>
    /// <param name="vectorLength"></param>
    public virtual IExpression Rewrite(IVectorLength vectorLength) {
      Contract.Requires(vectorLength != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var mutableVectorLength = vectorLength as VectorLength;
      if (mutableVectorLength == null) return vectorLength;
      this.RewriteChildren(mutableVectorLength);
      return mutableVectorLength;
    }

    /// <summary>
    /// Rewrites the given while do statement.
    /// </summary>
    /// <param name="whileDoStatement"></param>
    public virtual IStatement Rewrite(IWhileDoStatement whileDoStatement) {
      Contract.Requires(whileDoStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableWhileDoStatement = whileDoStatement as WhileDoStatement;
      if (mutableWhileDoStatement == null) return whileDoStatement;
      this.RewriteChildren(mutableWhileDoStatement);
      return mutableWhileDoStatement;
    }

    /// <summary>
    /// Rewrites the given yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public virtual IStatement Rewrite(IYieldBreakStatement yieldBreakStatement) {
      Contract.Requires(yieldBreakStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableYieldBreakStatement = yieldBreakStatement as YieldBreakStatement;
      if (mutableYieldBreakStatement == null) return yieldBreakStatement;
      this.RewriteChildren(mutableYieldBreakStatement);
      return mutableYieldBreakStatement;
    }

    /// <summary>
    /// Rewrites the given yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement"></param>
    public virtual IStatement Rewrite(IYieldReturnStatement yieldReturnStatement) {
      Contract.Requires(yieldReturnStatement != null);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var mutableYieldReturnStatement = yieldReturnStatement as YieldReturnStatement;
      if (mutableYieldReturnStatement == null) return yieldReturnStatement;
      this.RewriteChildren(mutableYieldReturnStatement);
      return mutableYieldReturnStatement;
    }

    /// <summary>
    /// Rewrites the given list of catch clauses.
    /// </summary>
    /// <param name="catchClauses"></param>
    public virtual List<ICatchClause> Rewrite(List<ICatchClause> catchClauses) {
      Contract.Requires(catchClauses != null);
      Contract.Ensures(Contract.Result<List<ICatchClause>>() != null);

      for (int i = 0, n = catchClauses.Count; i < n; i++)
        catchClauses[i] = this.Rewrite((CatchClause)catchClauses[i]);
      return catchClauses;
    }

    /// <summary>
    /// Rewrites the given list of expressions.
    /// </summary>
    /// <param name="expressions"></param>
    public virtual List<IExpression> Rewrite(List<IExpression> expressions) {
      Contract.Requires(expressions != null);
      Contract.Ensures(Contract.Result<List<IExpression>>() != null);

      for (int i = 0, n = expressions.Count; i < n; i++)
        expressions[i] = this.Rewrite(expressions[i]);
      return expressions;
    }

    /// <summary>
    /// Rewrites the given list of switch cases.
    /// </summary>
    /// <param name="switchCases"></param>
    public virtual List<ISwitchCase> Rewrite(List<ISwitchCase> switchCases) {
      Contract.Requires(switchCases != null);
      Contract.Ensures(Contract.Result<List<ISwitchCase>>() != null);

      for (int i = 0, n = switchCases.Count; i < n; i++)
        switchCases[i] = this.Rewrite((SwitchCase)switchCases[i]);
      return switchCases;
    }

    /// <summary>
    /// Rewrites the given list of statements.
    /// </summary>
    /// <param name="statements"></param>
    public virtual List<IStatement> Rewrite(List<IStatement> statements) {
      Contract.Requires(statements != null);
      Contract.Ensures(Contract.Result<List<IStatement>>() != null);

      var n = statements.Count;
      var j = 0;
      for (int i = 0; i < n; i++) {
        Contract.Assume(statements[i] != null);
        var s = this.Rewrite(statements[i]);
        if (s == CodeDummy.Block) continue;
        Contract.Assume(j <= i);
        statements[j++] = s;
      }
      if (j < n) statements.RemoveRange(j, n-j);
      return statements;
    }

    /// <summary>
    /// Rewrites the children of the given addition.
    /// </summary>
    /// <param name="addition"></param>
    public virtual void RewriteChildren(Addition addition) {
      Contract.Requires(addition != null);

      this.RewriteChildren((BinaryOperation)addition);
    }

    /// <summary>
    /// Rewrites the children of the given addressable expression.
    /// </summary>
    /// <param name="addressableExpression"></param>
    public virtual void RewriteChildren(AddressableExpression addressableExpression) {
      Contract.Requires(addressableExpression != null);

      this.RewriteChildren((Expression)addressableExpression);
      var local = addressableExpression.Definition as ILocalDefinition;
      if (local != null)
        addressableExpression.Definition = this.RewriteReference(local);
      else {
        var parameter = addressableExpression.Definition as IParameterDefinition;
        if (parameter != null)
          addressableExpression.Definition = this.RewriteReference(parameter);
        else {
          var fieldReference = addressableExpression.Definition as IFieldReference;
          if (fieldReference != null)
            addressableExpression.Definition = this.Rewrite(fieldReference);
          else {
            var arrayIndexer = addressableExpression.Definition as IArrayIndexer;
            if (arrayIndexer != null) {
              addressableExpression.Definition = this.Rewrite(arrayIndexer);
              return; //do not rewrite Instance again
            } else {
              var methodReference = addressableExpression.Definition as IMethodReference;
              if (methodReference != null)
                addressableExpression.Definition = this.Rewrite(methodReference);
              else {
                var expression = (IExpression)addressableExpression.Definition;
                addressableExpression.Definition = this.Rewrite(expression);
              }
            }
          }
        }
      }
      if (addressableExpression.Instance != null)
        addressableExpression.Instance = this.Rewrite(addressableExpression.Instance);
    }

    /// <summary>
    /// Rewrites the children of the given address dereference expression.
    /// </summary>
    /// <param name="addressDereference"></param>
    public virtual void RewriteChildren(AddressDereference addressDereference) {
      Contract.Requires(addressDereference != null);

      this.RewriteChildren((Expression)addressDereference);
      addressDereference.Address = this.Rewrite(addressDereference.Address);
    }

    /// <summary>
    /// Rewrites the children of the given AddressOf expression.
    /// </summary>
    /// <param name="addressOf"></param>
    public virtual void RewriteChildren(AddressOf addressOf) {
      Contract.Requires(addressOf != null);

      this.RewriteChildren((Expression)addressOf);
      addressOf.Expression = this.Rewrite((AddressableExpression)addressOf.Expression);
    }

    /// <summary>
    /// Rewrites the children of the given anonymous delegate expression.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
    public virtual void RewriteChildren(AnonymousDelegate anonymousDelegate) {
      Contract.Requires(anonymousDelegate != null);

      this.RewriteChildren((Expression)anonymousDelegate);
      anonymousDelegate.Parameters = this.Rewrite(anonymousDelegate.Parameters);
      anonymousDelegate.Body = this.Rewrite((BlockStatement)anonymousDelegate.Body);
      anonymousDelegate.ReturnType = this.Rewrite(anonymousDelegate.ReturnType);
      if (anonymousDelegate.ReturnValueIsModified)
        anonymousDelegate.ReturnValueCustomModifiers =this.Rewrite(anonymousDelegate.ReturnValueCustomModifiers);
    }

    /// <summary>
    /// Rewrites the children of the given array indexer expression.
    /// </summary>
    /// <param name="arrayIndexer"></param>
    public virtual void RewriteChildren(ArrayIndexer arrayIndexer) {
      Contract.Requires(arrayIndexer != null);

      this.RewriteChildren((Expression)arrayIndexer);
      arrayIndexer.IndexedObject = this.Rewrite(arrayIndexer.IndexedObject);
      arrayIndexer.Indices = this.Rewrite(arrayIndexer.Indices);
    }

    /// <summary>
    /// Rewrites the children of the given assert statement.
    /// </summary>
    /// <param name="assertStatement"></param>
    public virtual void RewriteChildren(AssertStatement assertStatement) {
      Contract.Requires(assertStatement != null);

      this.RewriteChildren((Statement)assertStatement);
      assertStatement.Condition = this.Rewrite(assertStatement.Condition);
      if (assertStatement.Description != null)
        assertStatement.Description = this.Rewrite(assertStatement.Description);
    }

    /// <summary>
    /// Rewrites the children of the given assignment expression.
    /// </summary>
    /// <param name="assignment"></param>
    public virtual void RewriteChildren(Assignment assignment) {
      Contract.Requires(assignment != null);

      this.RewriteChildren((Expression)assignment);
      assignment.Target = this.Rewrite(assignment.Target);
      assignment.Source = this.Rewrite(assignment.Source);
    }

    /// <summary>
    /// Rewrites the children of the given assume statement.
    /// </summary>
    /// <param name="assumeStatement"></param>
    public virtual void RewriteChildren(AssumeStatement assumeStatement) {
      Contract.Requires(assumeStatement != null);

      this.RewriteChildren((Statement)assumeStatement);
      assumeStatement.Condition = this.Rewrite(assumeStatement.Condition);
      if (assumeStatement.Description != null)
        assumeStatement.Description = this.Rewrite(assumeStatement.Description);
    }

    /// <summary>
    /// Called from the type specific rewrite method to rewrite the common part of all binary operation expressions.
    /// </summary>
    public virtual void RewriteChildren(BinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);

      this.RewriteChildren((Expression)binaryOperation);
      binaryOperation.LeftOperand = this.Rewrite(binaryOperation.LeftOperand);
      binaryOperation.RightOperand = this.Rewrite(binaryOperation.RightOperand);
    }

    /// <summary>
    /// Rewrites the children of the given bitwise and expression.
    /// </summary>
    public virtual void RewriteChildren(BitwiseAnd bitwiseAnd) {
      Contract.Requires(bitwiseAnd != null);

      this.RewriteChildren((BinaryOperation)bitwiseAnd);
    }

    /// <summary>
    /// Rewrites the children of the given bitwise or expression.
    /// </summary>
    public virtual void RewriteChildren(BitwiseOr bitwiseOr) {
      Contract.Requires(bitwiseOr != null);

      this.RewriteChildren((BinaryOperation)bitwiseOr);
    }

    /// <summary>
    /// Rewrites the children of the given block expression.
    /// </summary>
    public virtual void RewriteChildren(BlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);

      this.RewriteChildren((Expression)blockExpression);
      blockExpression.BlockStatement = this.Rewrite((BlockStatement)blockExpression.BlockStatement);
      blockExpression.Expression = this.Rewrite(blockExpression.Expression);
    }

    /// <summary>
    /// Rewrites the children of the given statement block.
    /// </summary>
    public virtual void RewriteChildren(BlockStatement block) {
      Contract.Requires(block != null);

      block.Statements = this.Rewrite(block.Statements);
    }

    /// <summary>
    /// Rewrites the children of the given bound expression.
    /// </summary>
    public virtual void RewriteChildren(BoundExpression boundExpression) {
      Contract.Requires(boundExpression != null);

      this.RewriteChildren((Expression)boundExpression);
      if (boundExpression.Instance != null)
        boundExpression.Instance = this.Rewrite(boundExpression.Instance);
      var local = boundExpression.Definition as ILocalDefinition;
      if (local != null)
        boundExpression.Definition = this.RewriteReference(local);
      else {
        var parameter = boundExpression.Definition as IParameterDefinition;
        if (parameter != null)
          boundExpression.Definition = this.RewriteReference(parameter);
        else {
          var fieldReference = (IFieldReference)boundExpression.Definition;
          boundExpression.Definition = this.Rewrite(fieldReference);
        }
      }
    }

    /// <summary>
    /// Rewrites the children of the given break statement.
    /// </summary>
    public virtual void RewriteChildren(BreakStatement breakStatement) {
      Contract.Requires(breakStatement != null);

      this.RewriteChildren((Statement)breakStatement);
    }

    /// <summary>
    /// Rewrites the children of the cast-if-possible expression.
    /// </summary>
    public virtual void RewriteChildren(CastIfPossible castIfPossible) {
      Contract.Requires(castIfPossible != null);

      this.RewriteChildren((Expression)castIfPossible);
      castIfPossible.ValueToCast = this.Rewrite(castIfPossible.ValueToCast);
      castIfPossible.TargetType = this.Rewrite(castIfPossible.TargetType);
    }

    /// <summary>
    /// Rewrites the children of the given catch clause.
    /// </summary>
    public virtual void RewriteChildren(CatchClause catchClause) {
      Contract.Requires(catchClause != null);

      catchClause.ExceptionType = this.Rewrite(catchClause.ExceptionType);
      if (!(catchClause.ExceptionContainer is Dummy))
        catchClause.ExceptionContainer = this.Rewrite(catchClause.ExceptionContainer);
      if (catchClause.FilterCondition != null)
        catchClause.FilterCondition = this.Rewrite(catchClause.FilterCondition);
      catchClause.Body = this.Rewrite((BlockStatement)catchClause.Body);
    }

    /// <summary>
    /// Rewrites the children of the given check-if-instance expression.
    /// </summary>
    public virtual void RewriteChildren(CheckIfInstance checkIfInstance) {
      Contract.Requires(checkIfInstance != null);

      this.RewriteChildren((Expression)checkIfInstance);
      checkIfInstance.Operand = this.Rewrite(checkIfInstance.Operand);
      checkIfInstance.TypeToCheck = this.Rewrite(checkIfInstance.TypeToCheck);
    }

    /// <summary>
    /// Rewrites the children of the given compile time constant.
    /// </summary>
    public virtual void RewriteChildren(CompileTimeConstant constant) {
      Contract.Requires(constant != null);

      this.RewriteChildren((Expression)constant);
    }

    /// <summary>
    /// Called from the type specific rewrite method to rewrite the common part of constructors and method calls.
    /// </summary>
    /// <param name="constructorOrMethodCall"></param>
    public virtual void RewriteChildren(ConstructorOrMethodCall constructorOrMethodCall) {
      Contract.Requires(constructorOrMethodCall != null);

      this.RewriteChildren((Expression)constructorOrMethodCall);
      constructorOrMethodCall.Arguments = this.Rewrite(constructorOrMethodCall.Arguments);
      constructorOrMethodCall.MethodToCall = this.Rewrite(constructorOrMethodCall.MethodToCall);
    }

    /// <summary>
    /// Rewrites the children of the given conditional expression.
    /// </summary>
    public virtual void RewriteChildren(Conditional conditional) {
      Contract.Requires(conditional != null);

      this.RewriteChildren((Expression)conditional);
      conditional.Condition = this.Rewrite(conditional.Condition);
      conditional.ResultIfTrue = this.Rewrite(conditional.ResultIfTrue);
      conditional.ResultIfFalse = this.Rewrite(conditional.ResultIfFalse);
    }

    /// <summary>
    /// Rewrites the children of the given conditional statement.
    /// </summary>
    public virtual void RewriteChildren(ConditionalStatement conditionalStatement) {
      Contract.Requires(conditionalStatement != null);

      this.RewriteChildren((Statement)conditionalStatement);
      conditionalStatement.Condition = this.Rewrite(conditionalStatement.Condition);
      conditionalStatement.TrueBranch = this.Rewrite(conditionalStatement.TrueBranch);
      conditionalStatement.FalseBranch = this.Rewrite(conditionalStatement.FalseBranch);
    }

    /// <summary>
    /// Rewrites the children of the given continue statement.
    /// </summary>
    public virtual void RewriteChildren(ContinueStatement continueStatement) {
      Contract.Requires(continueStatement != null);

      this.RewriteChildren((Statement)continueStatement);
    }

    /// <summary>
    /// Rewrites the children of the given conversion expression.
    /// </summary>
    public virtual void RewriteChildren(Conversion conversion) {
      Contract.Requires(conversion != null);

      this.RewriteChildren((Expression)conversion);
      conversion.ValueToConvert = this.Rewrite(conversion.ValueToConvert);
      conversion.TypeAfterConversion = this.Rewrite(conversion.TypeAfterConversion);
    }

    /// <summary>
    /// Rewrites the children of the given copy memory statement.
    /// </summary>
    public virtual void RewriteChildren(CopyMemoryStatement copyMemoryStatement) {
      Contract.Requires(copyMemoryStatement != null);

      this.RewriteChildren((Statement)copyMemoryStatement);
      copyMemoryStatement.TargetAddress = this.Rewrite(copyMemoryStatement.TargetAddress);
      copyMemoryStatement.SourceAddress = this.Rewrite(copyMemoryStatement.SourceAddress);
      copyMemoryStatement.NumberOfBytesToCopy = this.Rewrite(copyMemoryStatement.NumberOfBytesToCopy);
    }

    /// <summary>
    /// Rewrites the children of the given array creation expression.
    /// </summary>
    public virtual void RewriteChildren(CreateArray createArray) {
      Contract.Requires(createArray != null);

      this.RewriteChildren((Expression)createArray);
      createArray.ElementType = this.Rewrite(createArray.ElementType);
      createArray.Sizes = this.Rewrite(createArray.Sizes);
      createArray.Initializers = this.Rewrite(createArray.Initializers);
    }

    /// <summary>
    /// Rewrites the children of the anonymous object creation expression.
    /// </summary>
    public virtual void RewriteChildren(CreateDelegateInstance createDelegateInstance) {
      Contract.Requires(createDelegateInstance != null);

      this.RewriteChildren((Expression)createDelegateInstance);
      if (createDelegateInstance.Instance != null)
        createDelegateInstance.Instance = this.Rewrite(createDelegateInstance.Instance);
      createDelegateInstance.MethodToCallViaDelegate = this.Rewrite(createDelegateInstance.MethodToCallViaDelegate);
    }

    /// <summary>
    /// Rewrites the children of the given constructor call expression.
    /// </summary>
    public virtual void RewriteChildren(CreateObjectInstance createObjectInstance) {
      Contract.Requires(createObjectInstance != null);

      this.RewriteChildren((ConstructorOrMethodCall)createObjectInstance);
    }

    /// <summary>
    /// Rewrites the children of the given debugger break statement.
    /// </summary>
    public virtual void RewriteChildren(DebuggerBreakStatement debuggerBreakStatement) {
      Contract.Requires(debuggerBreakStatement != null);

      this.RewriteChildren((Statement)debuggerBreakStatement);
    }

    /// <summary>
    /// Rewrites the children of the given defalut value expression.
    /// </summary>
    public virtual void RewriteChildren(DefaultValue defaultValue) {
      Contract.Requires(defaultValue != null);

      this.RewriteChildren((Expression)defaultValue);
      defaultValue.DefaultValueType = this.Rewrite(defaultValue.DefaultValueType);
    }

    /// <summary>
    /// Rewrites the children of the given division expression.
    /// </summary>
    public virtual void RewriteChildren(Division division) {
      Contract.Requires(division != null);

      this.RewriteChildren((BinaryOperation)division);
    }

    /// <summary>
    /// Rewrites the children of the given do until statement.
    /// </summary>
    public virtual void RewriteChildren(DoUntilStatement doUntilStatement) {
      Contract.Requires(doUntilStatement != null);

      this.RewriteChildren((Statement)doUntilStatement);
      doUntilStatement.Body = this.Rewrite(doUntilStatement.Body);
      doUntilStatement.Condition = this.Rewrite(doUntilStatement.Condition);
    }

    /// <summary>
    /// Rewrites the children of the given dup value expression.
    /// </summary>
    public virtual void RewriteChildren(DupValue dupValue) {
      Contract.Requires(dupValue != null);

      this.RewriteChildren((Expression)dupValue);
    }

    /// <summary>
    /// Rewrites the children of the given empty statement.
    /// </summary>
    public virtual void RewriteChildren(EmptyStatement emptyStatement) {
      Contract.Requires(emptyStatement != null);

      this.RewriteChildren((Statement)emptyStatement);
    }

    /// <summary>
    /// Rewrites the children of the given equality expression.
    /// </summary>
    public virtual void RewriteChildren(Equality equality) {
      Contract.Requires(equality != null);

      this.RewriteChildren((BinaryOperation)equality);
    }

    /// <summary>
    /// Rewrites the children of the given exclusive or expression.
    /// </summary>
    public virtual void RewriteChildren(ExclusiveOr exclusiveOr) {
      Contract.Requires(exclusiveOr != null);

      this.RewriteChildren((BinaryOperation)exclusiveOr);
    }

    /// <summary>
    /// Called from the type specific rewrite method to rewrite the common part of all expressions.
    /// </summary>
    public virtual void RewriteChildren(Expression expression) {
      Contract.Requires(expression != null);

      expression.Type = this.Rewrite(expression.Type);
    }

    /// <summary>
    /// Rewrites the children of the given expression statement.
    /// </summary>
    public virtual void RewriteChildren(ExpressionStatement expressionStatement) {
      Contract.Requires(expressionStatement != null);

      this.RewriteChildren((Statement)expressionStatement);
      expressionStatement.Expression = this.Rewrite(expressionStatement.Expression);
    }

    /// <summary>
    /// Rewrites the children of the given fill memory statement.
    /// </summary>
    public virtual void RewriteChildren(FillMemoryStatement fillMemoryStatement) {
      Contract.Requires(fillMemoryStatement != null);

      this.RewriteChildren((Statement)fillMemoryStatement);
      fillMemoryStatement.TargetAddress = this.Rewrite(fillMemoryStatement.TargetAddress);
      fillMemoryStatement.FillValue = this.Rewrite(fillMemoryStatement.FillValue);
      fillMemoryStatement.NumberOfBytesToFill = this.Rewrite(fillMemoryStatement.NumberOfBytesToFill);
    }

    /// <summary>
    /// Rewrites the children of the given foreach statement.
    /// </summary>
    public virtual void RewriteChildren(ForEachStatement forEachStatement) {
      Contract.Requires(forEachStatement != null);

      this.RewriteChildren((Statement)forEachStatement);
      forEachStatement.Variable = this.Rewrite((LocalDefinition)forEachStatement.Variable);
      forEachStatement.Collection = this.Rewrite(forEachStatement.Collection);
      forEachStatement.Body = this.Rewrite(forEachStatement.Body);
    }

    /// <summary>
    /// Rewrites the children of the given for statement.
    /// </summary>
    public virtual void RewriteChildren(ForStatement forStatement) {
      Contract.Requires(forStatement != null);

      this.RewriteChildren((Statement)forStatement);
      forStatement.InitStatements = this.Rewrite(forStatement.InitStatements);
      forStatement.Condition = this.Rewrite(forStatement.Condition);
      forStatement.IncrementStatements = this.Rewrite(forStatement.IncrementStatements);
      forStatement.Body = this.Rewrite(forStatement.Body);
    }

    /// <summary>
    /// Rewrites the children of the given get type of typed reference expression.
    /// </summary>
    public virtual void RewriteChildren(GetTypeOfTypedReference getTypeOfTypedReference) {
      Contract.Requires(getTypeOfTypedReference != null);

      this.RewriteChildren((Expression)getTypeOfTypedReference);
      getTypeOfTypedReference.TypedReference = this.Rewrite(getTypeOfTypedReference.TypedReference);
    }

    /// <summary>
    /// Rewrites the children of the given get value of typed reference expression.
    /// </summary>
    public virtual void RewriteChildren(GetValueOfTypedReference getValueOfTypedReference) {
      Contract.Requires(getValueOfTypedReference != null);

      this.RewriteChildren((Expression)getValueOfTypedReference);
      getValueOfTypedReference.TypedReference = this.Rewrite(getValueOfTypedReference.TypedReference);
      getValueOfTypedReference.TargetType = this.Rewrite(getValueOfTypedReference.TargetType);
    }

    /// <summary>
    /// Rewrites the children of the given goto statement.
    /// </summary>
    public virtual void RewriteChildren(GotoStatement gotoStatement) {
      Contract.Requires(gotoStatement != null);

      this.RewriteChildren((Statement)gotoStatement);
    }

    /// <summary>
    /// Rewrites the children of the given goto switch case statement.
    /// </summary>
    public virtual void RewriteChildren(GotoSwitchCaseStatement gotoSwitchCaseStatement) {
      Contract.Requires(gotoSwitchCaseStatement != null);

      this.RewriteChildren((Statement)gotoSwitchCaseStatement);
    }

    /// <summary>
    /// Rewrites the children of the given greater-than expression.
    /// </summary>
    public virtual void RewriteChildren(GreaterThan greaterThan) {
      Contract.Requires(greaterThan != null);

      this.RewriteChildren((BinaryOperation)greaterThan);
    }

    /// <summary>
    /// Rewrites the children of the given greater-than-or-equal expression.
    /// </summary>
    public virtual void RewriteChildren(GreaterThanOrEqual greaterThanOrEqual) {
      Contract.Requires(greaterThanOrEqual != null);

      this.RewriteChildren((BinaryOperation)greaterThanOrEqual);
    }

    /// <summary>
    /// Rewrites the children of the given labeled statement.
    /// </summary>
    public virtual void RewriteChildren(LabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);

      this.RewriteChildren((Statement)labeledStatement);
      labeledStatement.Statement = this.Rewrite(labeledStatement.Statement);
    }

    /// <summary>
    /// Rewrites the children of the given left shift expression.
    /// </summary>
    public virtual void RewriteChildren(LeftShift leftShift) {
      Contract.Requires(leftShift != null);

      this.RewriteChildren((BinaryOperation)leftShift);
    }

    /// <summary>
    /// Rewrites the children of the given less-than expression.
    /// </summary>
    public virtual void RewriteChildren(LessThan lessThan) {
      Contract.Requires(lessThan != null);

      this.RewriteChildren((BinaryOperation)lessThan);
    }

    /// <summary>
    /// Rewrites the children of the given less-than-or-equal expression.
    /// </summary>
    public virtual void RewriteChildren(LessThanOrEqual lessThanOrEqual) {
      Contract.Requires(lessThanOrEqual != null);

      this.RewriteChildren((BinaryOperation)lessThanOrEqual);
    }

    /// <summary>
    /// Rewrites the children of the given local declaration statement.
    /// </summary>
    public virtual void RewriteChildren(LocalDeclarationStatement localDeclarationStatement) {
      Contract.Requires(localDeclarationStatement != null);

      this.RewriteChildren((Statement)localDeclarationStatement);
      localDeclarationStatement.LocalVariable = this.Rewrite(localDeclarationStatement.LocalVariable);
      if (localDeclarationStatement.InitialValue != null)
        localDeclarationStatement.InitialValue = this.Rewrite(localDeclarationStatement.InitialValue);
    }

    /// <summary>
    /// Rewrites the children of the given lock statement.
    /// </summary>
    public virtual void RewriteChildren(LockStatement lockStatement) {
      Contract.Requires(lockStatement != null);

      this.RewriteChildren((Statement)lockStatement);
      lockStatement.Guard = this.Rewrite(lockStatement.Guard);
      lockStatement.Body = this.Rewrite(lockStatement.Body);
    }

    /// <summary>
    /// Rewrites the children of the given logical not expression.
    /// </summary>
    public virtual void RewriteChildren(LogicalNot logicalNot) {
      Contract.Requires(logicalNot != null);

      this.RewriteChildren((UnaryOperation)logicalNot);
    }

    /// <summary>
    /// Rewrites the children of the given make typed reference expression.
    /// </summary>
    public virtual void RewriteChildren(MakeTypedReference makeTypedReference) {
      Contract.Requires(makeTypedReference != null);

      this.RewriteChildren((Expression)makeTypedReference);
      makeTypedReference.Operand = this.Rewrite(makeTypedReference.Operand);
    }

    /// <summary>
    /// Rewrites the children of the given method call.
    /// </summary>
    public virtual void RewriteChildren(MethodCall methodCall) {
      Contract.Requires(methodCall != null);

      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall)
        methodCall.ThisArgument = this.Rewrite(methodCall.ThisArgument);
      this.RewriteChildren((ConstructorOrMethodCall)methodCall);
    }

    /// <summary>
    /// Rewrites the children of the given modulus expression.
    /// </summary>
    public virtual void RewriteChildren(Modulus modulus) {
      Contract.Requires(modulus != null);

      this.RewriteChildren((BinaryOperation)modulus);
    }

    /// <summary>
    /// Rewrites the children of the given multiplication expression.
    /// </summary>
    public virtual void RewriteChildren(Multiplication multiplication) {
      Contract.Requires(multiplication != null);

      this.RewriteChildren((BinaryOperation)multiplication);
    }

    /// <summary>
    /// Rewrites the children of the given named argument expression.
    /// </summary>
    public virtual void RewriteChildren(NamedArgument namedArgument) {
      Contract.Requires(namedArgument != null);

      this.RewriteChildren((Expression)namedArgument);
      namedArgument.ArgumentValue = this.Rewrite(namedArgument.ArgumentValue);
    }

    /// <summary>
    /// Rewrites the children of the given not equality expression.
    /// </summary>
    public virtual void RewriteChildren(NotEquality notEquality) {
      Contract.Requires(notEquality != null);

      this.RewriteChildren((BinaryOperation)notEquality);
    }

    /// <summary>
    /// Rewrites the children of the given old value expression.
    /// </summary>
    public virtual void RewriteChildren(OldValue oldValue) {
      Contract.Requires(oldValue != null);

      this.RewriteChildren((Expression)oldValue);
      oldValue.Expression = this.Rewrite(oldValue.Expression);
    }

    /// <summary>
    /// Rewrites the children of the given one's complement expression.
    /// </summary>
    public virtual void RewriteChildren(OnesComplement onesComplement) {
      Contract.Requires(onesComplement != null);

      this.RewriteChildren((UnaryOperation)onesComplement);
    }

    /// <summary>
    /// Rewrites the children of the given out argument expression.
    /// </summary>
    public virtual void RewriteChildren(OutArgument outArgument) {
      Contract.Requires(outArgument != null);

      this.RewriteChildren((Expression)outArgument);
      outArgument.Expression = (ITargetExpression)this.Rewrite((TargetExpression)outArgument.Expression);
    }

    /// <summary>
    /// Rewrites the children of the given pointer call.
    /// </summary>
    public virtual void RewriteChildren(PointerCall pointerCall) {
      Contract.Requires(pointerCall != null);

      this.RewriteChildren((Expression)pointerCall);
      pointerCall.Pointer = this.Rewrite(pointerCall.Pointer);
      pointerCall.Arguments = this.Rewrite(pointerCall.Arguments);
    }

    /// <summary>
    /// Rewrites the children of the given pop value expression.
    /// </summary>
    public virtual void RewriteChildren(PopValue popValue) {
      Contract.Requires(popValue != null);

      this.RewriteChildren((Expression)popValue);
    }

    /// <summary>
    /// Rewrites the children of the given push statement.
    /// </summary>
    public virtual void RewriteChildren(PushStatement pushStatement) {
      Contract.Requires(pushStatement != null);

      this.RewriteChildren((Statement)pushStatement);
      pushStatement.ValueToPush = this.Rewrite(pushStatement.ValueToPush);
    }

    /// <summary>
    /// Rewrites the children of the given ref argument expression.
    /// </summary>
    public virtual void RewriteChildren(RefArgument refArgument) {
      Contract.Requires(refArgument != null);

      this.RewriteChildren((Expression)refArgument);
      refArgument.Expression = this.Rewrite((AddressableExpression)refArgument.Expression);
    }

    /// <summary>
    /// Rewrites the children of the given resource usage statement.
    /// </summary>
    public virtual void RewriteChildren(ResourceUseStatement resourceUseStatement) {
      Contract.Requires(resourceUseStatement != null);

      this.RewriteChildren((Statement)resourceUseStatement);
      resourceUseStatement.ResourceAcquisitions = this.Rewrite(resourceUseStatement.ResourceAcquisitions);
      resourceUseStatement.Body = this.Rewrite(resourceUseStatement.Body);
    }

    /// <summary>
    /// Rewrites the children of the rethrow statement.
    /// </summary>
    public virtual void RewriteChildren(RethrowStatement rethrowStatement) {
      Contract.Requires(rethrowStatement != null);

      this.RewriteChildren((Statement)rethrowStatement);
    }

    /// <summary>
    /// Rewrites the children of the return statement.
    /// </summary>
    public virtual void RewriteChildren(ReturnStatement returnStatement) {
      Contract.Requires(returnStatement != null);

      this.RewriteChildren((Statement)returnStatement);
      if (returnStatement.Expression != null)
        returnStatement.Expression = this.Rewrite(returnStatement.Expression);
    }

    /// <summary>
    /// Rewrites the children of the given return value expression.
    /// </summary>
    public virtual void RewriteChildren(ReturnValue returnValue) {
      Contract.Requires(returnValue != null);

      this.RewriteChildren((Expression)returnValue);
    }

    /// <summary>
    /// Rewrites the children of the given right shift expression.
    /// </summary>
    public virtual void RewriteChildren(RightShift rightShift) {
      Contract.Requires(rightShift != null);

      this.RewriteChildren((BinaryOperation)rightShift);
    }

    /// <summary>
    /// Rewrites the children of the given runtime argument handle expression.
    /// </summary>
    public virtual void RewriteChildren(RuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      Contract.Requires(runtimeArgumentHandleExpression != null);

      this.RewriteChildren((Expression)runtimeArgumentHandleExpression);
    }

    /// <summary>
    /// Rewrites the children of the given sizeof() expression.
    /// </summary>
    public virtual void RewriteChildren(SizeOf sizeOf) {
      Contract.Requires(sizeOf != null);

      this.RewriteChildren((Expression)sizeOf);
      sizeOf.TypeToSize = this.Rewrite(sizeOf.TypeToSize);
    }

    /// <summary>
    /// Rewrites the children of the the given source method body.
    /// </summary>
    public virtual void RewriteChildren(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);

      sourceMethodBody.Block = this.Rewrite((BlockStatement)sourceMethodBody.Block);
    }

    /// <summary>
    /// Rewrites the children of the given stack array create expression.
    /// </summary>
    public virtual void RewriteChildren(StackArrayCreate stackArrayCreate) {
      Contract.Requires(stackArrayCreate != null);

      this.RewriteChildren((Expression)stackArrayCreate);
      stackArrayCreate.ElementType = this.Rewrite(stackArrayCreate.ElementType);
      stackArrayCreate.Size = this.Rewrite(stackArrayCreate.Size);
    }

    /// <summary>
    /// Called from the type specific rewrite method to rewrite the common part of all statements.
    /// </summary>
    public virtual void RewriteChildren(Statement statement) {
      //This is just an extension hook
    }

    /// <summary>
    /// Rewrites the children of the given subtraction expression.
    /// </summary>
    public virtual void RewriteChildren(Subtraction subtraction) {
      Contract.Requires(subtraction != null);

      this.RewriteChildren((BinaryOperation)subtraction);
    }

    /// <summary>
    /// Rewrites the children of the given switch case.
    /// </summary>
    public virtual void RewriteChildren(SwitchCase switchCase) {
      Contract.Requires(switchCase != null);

      if (!switchCase.IsDefault)
        switchCase.Expression = this.Rewrite((CompileTimeConstant)switchCase.Expression);
      switchCase.Body = this.Rewrite(switchCase.Body);
    }

    /// <summary>
    /// Rewrites the children of the given switch statement.
    /// </summary>
    public virtual void RewriteChildren(SwitchStatement switchStatement) {
      Contract.Requires(switchStatement != null);

      this.RewriteChildren((Statement)switchStatement);
      switchStatement.Expression = this.Rewrite(switchStatement.Expression);
      switchStatement.Cases = this.Rewrite(switchStatement.Cases);
    }

    /// <summary>
    /// Rewrites the children of the given target expression.
    /// </summary>
    public virtual void RewriteChildren(TargetExpression targetExpression) {
      Contract.Requires(targetExpression != null);

      this.RewriteChildren((Expression)targetExpression);
      var local = targetExpression.Definition as ILocalDefinition;
      if (local != null)
        targetExpression.Definition = this.RewriteReference(local);
      else {
        var parameter = targetExpression.Definition as IParameterDefinition;
        if (parameter != null)
          targetExpression.Definition = this.RewriteReference(parameter);
        else {
          var fieldReference = targetExpression.Definition as IFieldReference;
          if (fieldReference != null)
            targetExpression.Definition = this.Rewrite(fieldReference);
          else {
            var arrayIndexer = targetExpression.Definition as IArrayIndexer;
            if (arrayIndexer != null) {
              targetExpression.Definition = this.Rewrite(arrayIndexer);
              arrayIndexer = targetExpression.Definition as IArrayIndexer;
              if (arrayIndexer != null) {
                targetExpression.Instance = arrayIndexer.IndexedObject;
                return;
              }
            } else {
              var addressDereference = targetExpression.Definition as IAddressDereference;
              if (addressDereference != null)
                targetExpression.Definition = this.Rewrite(addressDereference);
              else {
                var propertyDefinition = targetExpression.Definition as IPropertyDefinition;
                if (propertyDefinition != null)
                  targetExpression.Definition = this.Rewrite(propertyDefinition);
                else
                  targetExpression.Definition = this.Rewrite((IThisReference)targetExpression.Definition);
              }
            }
          }
        }
      }
      if (targetExpression.Instance != null) {
        targetExpression.Instance = this.Rewrite(targetExpression.Instance);
      }
    }

    /// <summary>
    /// Rewrites the children of the given this reference expression.
    /// </summary>
    public virtual void RewriteChildren(ThisReference thisReference) {
      Contract.Requires(thisReference != null);

      this.RewriteChildren((Expression)thisReference);
    }

    /// <summary>
    /// Rewrites the children of the throw statement.
    /// </summary>
    public virtual void RewriteChildren(ThrowStatement throwStatement) {
      Contract.Requires(throwStatement != null);

      this.RewriteChildren((Statement)throwStatement);
      throwStatement.Exception = this.Rewrite(throwStatement.Exception);
    }

    /// <summary>
    /// Rewrites the children of the given tokenof() expression.
    /// </summary>
    public virtual void RewriteChildren(TokenOf tokenOf) {
      Contract.Requires(tokenOf != null);

      this.RewriteChildren((Expression)tokenOf);
      var fieldReference = tokenOf.Definition as IFieldReference;
      if (fieldReference != null)
        tokenOf.Definition = this.Rewrite(fieldReference);
      else {
        var methodReference = tokenOf.Definition as IMethodReference;
        if (methodReference != null)
          tokenOf.Definition = this.Rewrite(methodReference);
        else {
          var typeReference = (ITypeReference)tokenOf.Definition;
          tokenOf.Definition = this.Rewrite(typeReference);
        }
      }
    }

    /// <summary>
    /// Rewrites the children of the try-catch-filter-finally statement.
    /// </summary>
    public virtual void RewriteChildren(TryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      Contract.Requires(tryCatchFilterFinallyStatement != null);

      this.RewriteChildren((Statement)tryCatchFilterFinallyStatement);
      tryCatchFilterFinallyStatement.TryBody = this.Rewrite((BlockStatement)tryCatchFilterFinallyStatement.TryBody);
      tryCatchFilterFinallyStatement.CatchClauses = this.Rewrite(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FaultBody != null)
        tryCatchFilterFinallyStatement.FaultBody = this.Rewrite((BlockStatement)tryCatchFilterFinallyStatement.FaultBody);
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        tryCatchFilterFinallyStatement.FinallyBody = this.Rewrite((BlockStatement)tryCatchFilterFinallyStatement.FinallyBody);
    }

    /// <summary>
    /// Rewrites the children of the given typeof() expression.
    /// </summary>
    public virtual void RewriteChildren(TypeOf typeOf) {
      Contract.Requires(typeOf != null);

      this.RewriteChildren((Expression)typeOf);
      typeOf.TypeToGet = this.Rewrite(typeOf.TypeToGet);
    }

    /// <summary>
    /// Rewrites the children of the given unary negation expression.
    /// </summary>
    public virtual void RewriteChildren(UnaryNegation unaryNegation) {
      Contract.Requires(unaryNegation != null);

      this.RewriteChildren((UnaryOperation)unaryNegation);
    }

    /// <summary>
    /// Called from the type specific rewrite method to rewrite the common part of all unary operation expressions.
    /// </summary>
    public virtual void RewriteChildren(UnaryOperation unaryOperation) {
      Contract.Requires(unaryOperation != null);

      this.RewriteChildren((Expression)unaryOperation);
      unaryOperation.Operand = this.Rewrite(unaryOperation.Operand);
    }

    /// <summary>
    /// Rewrites the children of the given unary plus expression.
    /// </summary>
    public virtual void RewriteChildren(UnaryPlus unaryPlus) {
      Contract.Requires(unaryPlus != null);

      this.RewriteChildren((UnaryOperation)unaryPlus);
    }

    /// <summary>
    /// Rewrites the children of the given vector length expression.
    /// </summary>
    public virtual void RewriteChildren(VectorLength vectorLength) {
      Contract.Requires(vectorLength != null);

      this.RewriteChildren((Expression)vectorLength);
      vectorLength.Vector = this.Rewrite(vectorLength.Vector);
    }

    /// <summary>
    /// Rewrites the children of the given while do statement.
    /// </summary>
    public virtual void RewriteChildren(WhileDoStatement whileDoStatement) {
      Contract.Requires(whileDoStatement != null);

      this.RewriteChildren((Statement)whileDoStatement);
      whileDoStatement.Condition = this.Rewrite(whileDoStatement.Condition);
      whileDoStatement.Body = this.Rewrite(whileDoStatement.Body);
    }

    /// <summary>
    /// Rewrites the children of the given yield break statement.
    /// </summary>
    public virtual void RewriteChildren(YieldBreakStatement yieldBreakStatement) {
      Contract.Requires(yieldBreakStatement != null);

      this.RewriteChildren((Statement)yieldBreakStatement);
    }

    /// <summary>
    /// Rewrites the children of the given yield return statement.
    /// </summary>
    public virtual void RewriteChildren(YieldReturnStatement yieldReturnStatement) {
      Contract.Requires(yieldReturnStatement != null);

      this.RewriteChildren((Statement)yieldReturnStatement);
      yieldReturnStatement.Expression = this.Rewrite(yieldReturnStatement.Expression);
    }

  }

  /// <summary>
  /// Uses the inherited methods from MetadataMutator to walk everything down to the method body level,
  /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
  /// </summary>
  /// <remarks>While the model is being copied, the resulting model is incomplete and or inconsistent. It should not be traversed
  /// independently nor should any of its computed properties, such as ResolvedType be evaluated. Scenarios that need such functionality
  /// should be implemented by first making a mutable copy of the entire assembly and then running a second pass over the mutable result.
  /// The new classes CodeDeepCopier and CodeRewriter are meant to facilitate such scenarios.
  /// </remarks>
  [Obsolete("This class has been superceded by CodeDeepCopier and CodeRewriter, used in combination. It will go away in the future.")]
  public class CodeMutator : MetadataMutator {

    private CreateMutableType createMutableType;

    /// <summary>
    /// An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.
    /// </summary>
    protected readonly ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutator to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public CodeMutator(IMetadataHost host)
      : base(host) {
      createMutableType = new CreateMutableType(this, true);
    }

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutator to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable">True if the mutator should try and perform mutations in place, rather than mutating new copies.</param>
    public CodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host, copyOnlyIfNotAlreadyMutable) {
      createMutableType = new CreateMutableType(this, !copyOnlyIfNotAlreadyMutable);
    }

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutator to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public CodeMutator(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host) {
      this.sourceLocationProvider = sourceLocationProvider;
      createMutableType = new CreateMutableType(this, true);
    }

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutator to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable"></param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public CodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, copyOnlyIfNotAlreadyMutable) {
      this.sourceLocationProvider = sourceLocationProvider;
      createMutableType = new CreateMutableType(this, !copyOnlyIfNotAlreadyMutable);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDefinitions"></param>
    public override void VisitPrivateHelperMembers(List<INamedTypeDefinition> typeDefinitions) {
      return;
    }

    #region Virtual methods for subtypes to override, one per type in MutableCodeModel

    /// <summary>
    /// Visits the specified addition.
    /// </summary>
    /// <param name="addition">The addition.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Addition addition) {
      return this.Visit((BinaryOperation)addition);
    }

    /// <summary>
    /// Visits the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    /// <returns></returns>
    public virtual IAddressableExpression Visit(AddressableExpression addressableExpression) {
      object def = addressableExpression.Definition;
      ILocalDefinition/*?*/ loc = def as ILocalDefinition;
      if (loc != null)
        addressableExpression.Definition = this.VisitReferenceTo(loc);
      else {
        IParameterDefinition/*?*/ par = def as IParameterDefinition;
        if (par != null)
          addressableExpression.Definition = this.VisitReferenceTo(par);
        else {
          IFieldReference/*?*/ field = def as IFieldReference;
          if (field != null)
            addressableExpression.Definition = this.Visit(field);
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
                  IThisReference thisRef = (IThisReference)def;
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

    /// <summary>
    /// Visits the specified address dereference.
    /// </summary>
    /// <param name="addressDereference">The address dereference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(AddressDereference addressDereference) {
      addressDereference.Address = this.Visit(addressDereference.Address);
      addressDereference.Type = this.Visit(addressDereference.Type);
      return addressDereference;
    }

    /// <summary>
    /// Visits the specified address of.
    /// </summary>
    /// <param name="addressOf">The address of.</param>
    /// <returns></returns>
    public virtual IExpression Visit(AddressOf addressOf) {
      addressOf.Expression = this.Visit(addressOf.Expression);
      addressOf.Type = this.Visit(addressOf.Type);
      return addressOf;
    }

    /// <summary>
    /// Visits the specified anonymous delegate.
    /// </summary>
    /// <param name="anonymousDelegate">The anonymous delegate.</param>
    /// <returns></returns>
    public virtual IExpression Visit(AnonymousDelegate anonymousDelegate) {
      this.path.Push(anonymousDelegate);
      anonymousDelegate.Parameters = this.Visit(anonymousDelegate.Parameters);
      anonymousDelegate.Body = this.Visit(anonymousDelegate.Body);
      anonymousDelegate.ReturnType = this.Visit(anonymousDelegate.ReturnType);
      anonymousDelegate.Type = this.Visit(anonymousDelegate.Type);
      this.path.Pop();
      return anonymousDelegate;
    }

    /// <summary>
    /// Visits the specified array indexer.
    /// </summary>
    /// <param name="arrayIndexer">The array indexer.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ArrayIndexer arrayIndexer) {
      arrayIndexer.IndexedObject = this.Visit(arrayIndexer.IndexedObject);
      arrayIndexer.Indices = this.Visit(arrayIndexer.Indices);
      arrayIndexer.Type = this.Visit(arrayIndexer.Type);
      return arrayIndexer;
    }

    /// <summary>
    /// Visits the specified assert statement.
    /// </summary>
    /// <param name="assertStatement">The assert statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(AssertStatement assertStatement) {
      assertStatement.Condition = this.Visit(assertStatement.Condition);
      return assertStatement;
    }

    /// <summary>
    /// Visits the specified assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Assignment assignment) {
      assignment.Target = this.Visit(assignment.Target);
      assignment.Source = this.Visit(assignment.Source);
      assignment.Type = this.Visit(assignment.Type);
      return assignment;
    }

    /// <summary>
    /// Visits the specified assume statement.
    /// </summary>
    /// <param name="assumeStatement">The assume statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(AssumeStatement assumeStatement) {
      assumeStatement.Condition = this.Visit(assumeStatement.Condition);
      return assumeStatement;
    }

    /// <summary>
    /// Visits the specified bitwise and.
    /// </summary>
    /// <param name="bitwiseAnd">The bitwise and.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BitwiseAnd bitwiseAnd) {
      return this.Visit((BinaryOperation)bitwiseAnd);
    }

    /// <summary>
    /// Visits the specified bitwise or.
    /// </summary>
    /// <param name="bitwiseOr">The bitwise or.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BitwiseOr bitwiseOr) {
      return this.Visit((BinaryOperation)bitwiseOr);
    }

    /// <summary>
    /// Visits the specified binary operation.
    /// </summary>
    /// <param name="binaryOperation">The binary operation.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BinaryOperation binaryOperation) {
      binaryOperation.LeftOperand = this.Visit(binaryOperation.LeftOperand);
      binaryOperation.RightOperand = this.Visit(binaryOperation.RightOperand);
      binaryOperation.Type = this.Visit(binaryOperation.Type);
      return binaryOperation;
    }

    /// <summary>
    /// Visits the specified block expression.
    /// </summary>
    /// <param name="blockExpression">The block expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BlockExpression blockExpression) {
      blockExpression.BlockStatement = this.Visit(blockExpression.BlockStatement);
      blockExpression.Expression = Visit(blockExpression.Expression);
      blockExpression.Type = this.Visit(blockExpression.Type);
      return blockExpression;
    }

    /// <summary>
    /// Visits the specified block statement.
    /// </summary>
    /// <param name="blockStatement">The block statement.</param>
    /// <returns></returns>
    public virtual IBlockStatement Visit(BlockStatement blockStatement) {
      blockStatement.Statements = Visit(blockStatement.Statements);
      return blockStatement;
    }

    /// <summary>
    /// Visits the specified bound expression.
    /// </summary>
    /// <param name="boundExpression">The bound expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BoundExpression boundExpression) {
      if (boundExpression.Instance != null)
        boundExpression.Instance = Visit(boundExpression.Instance);
      ILocalDefinition/*?*/ loc = boundExpression.Definition as ILocalDefinition;
      if (loc != null)
        boundExpression.Definition = this.VisitReferenceTo(loc);
      else {
        IParameterDefinition/*?*/ par = boundExpression.Definition as IParameterDefinition;
        if (par != null)
          boundExpression.Definition = this.VisitReferenceTo(par);
        else {
          IFieldReference/*?*/ field = boundExpression.Definition as IFieldReference;
          boundExpression.Definition = this.Visit(field);
        }
      }
      boundExpression.Type = this.Visit(boundExpression.Type);
      return boundExpression;
    }

    /// <summary>
    /// Visits the specified break statement.
    /// </summary>
    /// <param name="breakStatement">The break statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(BreakStatement breakStatement) {
      return breakStatement;
    }

    /// <summary>
    /// Visits the specified cast if possible.
    /// </summary>
    /// <param name="castIfPossible">The cast if possible.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CastIfPossible castIfPossible) {
      castIfPossible.TargetType = Visit(castIfPossible.TargetType);
      castIfPossible.ValueToCast = Visit(castIfPossible.ValueToCast);
      castIfPossible.Type = this.Visit(castIfPossible.Type);
      return castIfPossible;
    }

    /// <summary>
    /// Visits the specified catch clauses.
    /// </summary>
    /// <param name="catchClauses">The catch clauses.</param>
    /// <returns></returns>
    public virtual List<ICatchClause> Visit(List<CatchClause> catchClauses) {
      List<ICatchClause> newList = new List<ICatchClause>();
      foreach (var catchClause in catchClauses) {
        newList.Add(Visit(catchClause));
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified catch clause.
    /// </summary>
    /// <param name="catchClause">The catch clause.</param>
    /// <returns></returns>
    public virtual ICatchClause Visit(CatchClause catchClause) {
      if (catchClause.FilterCondition != null)
        catchClause.FilterCondition = Visit(catchClause.FilterCondition);
      catchClause.Body = Visit(catchClause.Body);
      return catchClause;
    }

    /// <summary>
    /// Visits the specified check if instance.
    /// </summary>
    /// <param name="checkIfInstance">The check if instance.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CheckIfInstance checkIfInstance) {
      checkIfInstance.Operand = Visit(checkIfInstance.Operand);
      checkIfInstance.TypeToCheck = Visit(checkIfInstance.TypeToCheck);
      checkIfInstance.Type = this.Visit(checkIfInstance.Type);
      return checkIfInstance;
    }

    /// <summary>
    /// Visits the specified constant.
    /// </summary>
    /// <param name="constant">The constant.</param>
    /// <returns></returns>
    public virtual ICompileTimeConstant Visit(CompileTimeConstant constant) {
      constant.Type = this.Visit(constant.Type);
      return constant;
    }

    /// <summary>
    /// Visits the specified conversion.
    /// </summary>
    /// <param name="conversion">The conversion.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Conversion conversion) {
      conversion.ValueToConvert = Visit(conversion.ValueToConvert);
      conversion.Type = this.Visit(conversion.Type);
      return conversion;
    }

    /// <summary>
    /// Visits the specified conditional.
    /// </summary>
    /// <param name="conditional">The conditional.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Conditional conditional) {
      conditional.Condition = Visit(conditional.Condition);
      conditional.ResultIfTrue = Visit(conditional.ResultIfTrue);
      conditional.ResultIfFalse = Visit(conditional.ResultIfFalse);
      conditional.Type = this.Visit(conditional.Type);
      return conditional;
    }

    /// <summary>
    /// Visits the specified conditional statement.
    /// </summary>
    /// <param name="conditionalStatement">The conditional statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ConditionalStatement conditionalStatement) {
      conditionalStatement.Condition = Visit(conditionalStatement.Condition);
      conditionalStatement.TrueBranch = Visit(conditionalStatement.TrueBranch);
      conditionalStatement.FalseBranch = Visit(conditionalStatement.FalseBranch);
      return conditionalStatement;
    }

    /// <summary>
    /// Visits the specified continue statement.
    /// </summary>
    /// <param name="continueStatement">The continue statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ContinueStatement continueStatement) {
      return continueStatement;
    }

    /// <summary>
    /// Visits the specified create array.
    /// </summary>
    /// <param name="createArray">The create array.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CreateArray createArray) {
      createArray.ElementType = this.Visit(createArray.ElementType);
      createArray.Sizes = this.Visit(createArray.Sizes);
      createArray.Initializers = this.Visit(createArray.Initializers);
      createArray.Type = this.Visit(createArray.Type);
      return createArray;
    }

    /// <summary>
    /// Visits the specified create object instance.
    /// </summary>
    /// <param name="createObjectInstance">The create object instance.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CreateObjectInstance createObjectInstance) {
      createObjectInstance.MethodToCall = this.Visit(createObjectInstance.MethodToCall);
      createObjectInstance.Arguments = Visit(createObjectInstance.Arguments);
      createObjectInstance.Type = this.Visit(createObjectInstance.Type);
      return createObjectInstance;
    }

    /// <summary>
    /// Visits the specified create delegate instance.
    /// </summary>
    /// <param name="createDelegateInstance">The create delegate instance.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CreateDelegateInstance createDelegateInstance) {
      createDelegateInstance.MethodToCallViaDelegate = this.Visit(createDelegateInstance.MethodToCallViaDelegate);
      if (createDelegateInstance.Instance != null)
        createDelegateInstance.Instance = Visit(createDelegateInstance.Instance);
      createDelegateInstance.Type = this.Visit(createDelegateInstance.Type);
      return createDelegateInstance;
    }

    /// <summary>
    /// Visits the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(DefaultValue defaultValue) {
      defaultValue.DefaultValueType = Visit(defaultValue.DefaultValueType);
      defaultValue.Type = this.Visit(defaultValue.Type);
      return defaultValue;
    }

    /// <summary>
    /// Visits the specified debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement">The debugger break statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(DebuggerBreakStatement debuggerBreakStatement) {
      return debuggerBreakStatement;
    }

    /// <summary>
    /// Visits the specified division.
    /// </summary>
    /// <param name="division">The division.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Division division) {
      return this.Visit((BinaryOperation)division);
    }

    /// <summary>
    /// Visits the specified do until statement.
    /// </summary>
    /// <param name="doUntilStatement">The do until statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(DoUntilStatement doUntilStatement) {
      doUntilStatement.Body = Visit(doUntilStatement.Body);
      doUntilStatement.Condition = Visit(doUntilStatement.Condition);
      return doUntilStatement;
    }

    /// <summary>
    /// Visits the specified dup value.
    /// </summary>
    /// <param name="dupValue">The dup value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(DupValue dupValue) {
      dupValue.Type = this.Visit(dupValue.Type);
      return dupValue;
    }

    /// <summary>
    /// Visits the specified empty statement.
    /// </summary>
    /// <param name="emptyStatement">The empty statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(EmptyStatement emptyStatement) {
      return emptyStatement;
    }

    /// <summary>
    /// Visits the specified equality.
    /// </summary>
    /// <param name="equality">The equality.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Equality equality) {
      return this.Visit((BinaryOperation)equality);
    }

    /// <summary>
    /// Visits the specified exclusive or.
    /// </summary>
    /// <param name="exclusiveOr">The exclusive or.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ExclusiveOr exclusiveOr) {
      return this.Visit((BinaryOperation)exclusiveOr);
    }

    /// <summary>
    /// Visits the specified expressions.
    /// </summary>
    /// <param name="expressions">The expressions.</param>
    /// <returns></returns>
    public virtual List<IExpression> Visit(List<IExpression> expressions) {
      List<IExpression> newList = new List<IExpression>();
      foreach (var expression in expressions)
        newList.Add(this.Visit(expression));
      return newList;
    }

    /// <summary>
    /// Visits the specified expression statement.
    /// </summary>
    /// <param name="expressionStatement">The expression statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ExpressionStatement expressionStatement) {
      expressionStatement.Expression = Visit(expressionStatement.Expression);
      return expressionStatement;
    }

    /// <summary>
    /// Visits the specified for each statement.
    /// </summary>
    /// <param name="forEachStatement">For each statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ForEachStatement forEachStatement) {
      forEachStatement.Collection = Visit(forEachStatement.Collection);
      forEachStatement.Body = Visit(forEachStatement.Body);
      return forEachStatement;
    }

    /// <summary>
    /// Visits the specified for statement.
    /// </summary>
    /// <param name="forStatement">For statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ForStatement forStatement) {
      forStatement.InitStatements = Visit(forStatement.InitStatements);
      forStatement.Condition = Visit(forStatement.Condition);
      forStatement.IncrementStatements = Visit(forStatement.IncrementStatements);
      forStatement.Body = Visit(forStatement.Body);
      return forStatement;
    }

    /// <summary>
    /// Visits the specified get type of typed reference.
    /// </summary>
    /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GetTypeOfTypedReference getTypeOfTypedReference) {
      getTypeOfTypedReference.TypedReference = Visit(getTypeOfTypedReference.TypedReference);
      getTypeOfTypedReference.Type = this.Visit(getTypeOfTypedReference.Type);
      return getTypeOfTypedReference;
    }

    /// <summary>
    /// Visits the specified get value of typed reference.
    /// </summary>
    /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GetValueOfTypedReference getValueOfTypedReference) {
      getValueOfTypedReference.TypedReference = Visit(getValueOfTypedReference.TypedReference);
      getValueOfTypedReference.TargetType = Visit(getValueOfTypedReference.TargetType);
      getValueOfTypedReference.Type = this.Visit(getValueOfTypedReference.Type);
      return getValueOfTypedReference;
    }

    /// <summary>
    /// Visits the specified goto statement.
    /// </summary>
    /// <param name="gotoStatement">The goto statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(GotoStatement gotoStatement) {
      return gotoStatement;
    }

    /// <summary>
    /// Visits the specified goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(GotoSwitchCaseStatement gotoSwitchCaseStatement) {
      return gotoSwitchCaseStatement;
    }

    /// <summary>
    /// Visits the specified greater than.
    /// </summary>
    /// <param name="greaterThan">The greater than.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GreaterThan greaterThan) {
      return this.Visit((BinaryOperation)greaterThan);
    }

    /// <summary>
    /// Visits the specified greater than or equal.
    /// </summary>
    /// <param name="greaterThanOrEqual">The greater than or equal.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GreaterThanOrEqual greaterThanOrEqual) {
      return this.Visit((BinaryOperation)greaterThanOrEqual);
    }

    /// <summary>
    /// Visits the specified labeled statement.
    /// </summary>
    /// <param name="labeledStatement">The labeled statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(LabeledStatement labeledStatement) {
      labeledStatement.Statement = Visit(labeledStatement.Statement);
      return labeledStatement;
    }

    /// <summary>
    /// Visits the specified left shift.
    /// </summary>
    /// <param name="leftShift">The left shift.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LeftShift leftShift) {
      return this.Visit((BinaryOperation)leftShift);
    }

    /// <summary>
    /// Visits the specified less than.
    /// </summary>
    /// <param name="lessThan">The less than.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LessThan lessThan) {
      return this.Visit((BinaryOperation)lessThan);
    }

    /// <summary>
    /// Visits the specified less than or equal.
    /// </summary>
    /// <param name="lessThanOrEqual">The less than or equal.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LessThanOrEqual lessThanOrEqual) {
      return this.Visit((BinaryOperation)lessThanOrEqual);
    }

    /// <summary>
    /// Visits the specified local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      localDeclarationStatement.LocalVariable = this.VisitReferenceTo(localDeclarationStatement.LocalVariable);
      if (localDeclarationStatement.InitialValue != null)
        localDeclarationStatement.InitialValue = Visit(localDeclarationStatement.InitialValue);
      return localDeclarationStatement;
    }

    /// <summary>
    /// Visits the specified lock statement.
    /// </summary>
    /// <param name="lockStatement">The lock statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(LockStatement lockStatement) {
      lockStatement.Guard = Visit(lockStatement.Guard);
      lockStatement.Body = Visit(lockStatement.Body);
      return lockStatement;
    }

    /// <summary>
    /// Visits the specified logical not.
    /// </summary>
    /// <param name="logicalNot">The logical not.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LogicalNot logicalNot) {
      return this.Visit((UnaryOperation)logicalNot);
    }

    /// <summary>
    /// Visits the specified make typed reference.
    /// </summary>
    /// <param name="makeTypedReference">The make typed reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(MakeTypedReference makeTypedReference) {
      makeTypedReference.Operand = Visit(makeTypedReference.Operand);
      makeTypedReference.Type = this.Visit(makeTypedReference.Type);
      return makeTypedReference;
    }

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    public override IMethodBody Visit(IMethodBody methodBody) {
      ISourceMethodBody sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null) {
        SourceMethodBody mutableSourceMethodBody = null;
        if (this.copyOnlyIfNotAlreadyMutable)
          mutableSourceMethodBody = sourceMethodBody as SourceMethodBody;
        if (mutableSourceMethodBody == null)
          mutableSourceMethodBody = new SourceMethodBody(this.host, this.sourceLocationProvider);
        mutableSourceMethodBody.Block = this.Visit(sourceMethodBody.Block);
        mutableSourceMethodBody.LocalsAreZeroed = methodBody.LocalsAreZeroed;
        mutableSourceMethodBody.MethodDefinition = this.GetCurrentMethod();
        return mutableSourceMethodBody;
      }
      return base.Visit(methodBody);
    }

    /// <summary>
    /// Visits the specified method call.
    /// </summary>
    /// <param name="methodCall">The method call.</param>
    /// <returns></returns>
    public virtual IExpression Visit(MethodCall methodCall) {
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall)
        methodCall.ThisArgument = this.Visit(methodCall.ThisArgument);
      methodCall.Arguments = this.Visit(methodCall.Arguments);
      methodCall.MethodToCall = this.Visit(methodCall.MethodToCall);
      methodCall.Type = this.Visit(methodCall.Type);
      return methodCall;
    }

    /// <summary>
    /// Visits the specified modulus.
    /// </summary>
    /// <param name="modulus">The modulus.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Modulus modulus) {
      return this.Visit((BinaryOperation)modulus);
    }

    /// <summary>
    /// Visits the specified multiplication.
    /// </summary>
    /// <param name="multiplication">The multiplication.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Multiplication multiplication) {
      return this.Visit((BinaryOperation)multiplication);
    }

    /// <summary>
    /// Visits the specified named argument.
    /// </summary>
    /// <param name="namedArgument">The named argument.</param>
    /// <returns></returns>
    public virtual IExpression Visit(NamedArgument namedArgument) {
      namedArgument.ArgumentValue = namedArgument.ArgumentValue;
      namedArgument.Type = this.Visit(namedArgument.Type);
      return namedArgument;
    }

    /// <summary>
    /// Visits the specified not equality.
    /// </summary>
    /// <param name="notEquality">The not equality.</param>
    /// <returns></returns>
    public virtual IExpression Visit(NotEquality notEquality) {
      return this.Visit((BinaryOperation)notEquality);
    }

    /// <summary>
    /// Visits the specified old value.
    /// </summary>
    /// <param name="oldValue">The old value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(OldValue oldValue) {
      oldValue.Expression = Visit(oldValue.Expression);
      oldValue.Type = this.Visit(oldValue.Type);
      return oldValue;
    }

    /// <summary>
    /// Visits the specified ones complement.
    /// </summary>
    /// <param name="onesComplement">The ones complement.</param>
    /// <returns></returns>
    public virtual IExpression Visit(OnesComplement onesComplement) {
      return this.Visit((UnaryOperation)onesComplement);
    }

    /// <summary>
    /// Visits the specified unary operation.
    /// </summary>
    /// <param name="unaryOperation">The unary operation.</param>
    /// <returns></returns>
    public virtual IExpression Visit(UnaryOperation unaryOperation) {
      unaryOperation.Operand = Visit(unaryOperation.Operand);
      unaryOperation.Type = this.Visit(unaryOperation.Type);
      return unaryOperation;
    }

    /// <summary>
    /// Visits the specified out argument.
    /// </summary>
    /// <param name="outArgument">The out argument.</param>
    /// <returns></returns>
    public virtual IExpression Visit(OutArgument outArgument) {
      outArgument.Expression = Visit(outArgument.Expression);
      outArgument.Type = this.Visit(outArgument.Type);
      return outArgument;
    }

    /// <summary>
    /// Visits the specified pointer call.
    /// </summary>
    /// <param name="pointerCall">The pointer call.</param>
    /// <returns></returns>
    public virtual IExpression Visit(PointerCall pointerCall) {
      pointerCall.Pointer = this.Visit(pointerCall.Pointer);
      pointerCall.Arguments = Visit(pointerCall.Arguments);
      pointerCall.Type = this.Visit(pointerCall.Type);
      return pointerCall;
    }

    /// <summary>
    /// Visits the specified pop value.
    /// </summary>
    /// <param name="popValue">The pop value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(PopValue popValue) {
      popValue.Type = this.Visit(popValue.Type);
      return popValue;
    }

    /// <summary>
    /// Visits the specified push statement.
    /// </summary>
    /// <param name="pushStatement">The push statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(PushStatement pushStatement) {
      pushStatement.ValueToPush = this.Visit(pushStatement.ValueToPush);
      return pushStatement;
    }

    /// <summary>
    /// Visits the specified ref argument.
    /// </summary>
    /// <param name="refArgument">The ref argument.</param>
    /// <returns></returns>
    public virtual IExpression Visit(RefArgument refArgument) {
      refArgument.Expression = Visit(refArgument.Expression);
      refArgument.Type = this.Visit(refArgument.Type);
      return refArgument;
    }

    /// <summary>
    /// Visits the specified resource use statement.
    /// </summary>
    /// <param name="resourceUseStatement">The resource use statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ResourceUseStatement resourceUseStatement) {
      resourceUseStatement.ResourceAcquisitions = Visit(resourceUseStatement.ResourceAcquisitions);
      resourceUseStatement.Body = Visit(resourceUseStatement.Body);
      return resourceUseStatement;
    }

    /// <summary>
    /// Visits the specified rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement">The rethrow statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(RethrowStatement rethrowStatement) {
      return rethrowStatement;
    }

    /// <summary>
    /// Visits the specified return statement.
    /// </summary>
    /// <param name="returnStatement">The return statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ReturnStatement returnStatement) {
      if (returnStatement.Expression != null)
        returnStatement.Expression = Visit(returnStatement.Expression);
      return returnStatement;
    }

    /// <summary>
    /// Visits the specified return value.
    /// </summary>
    /// <param name="returnValue">The return value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ReturnValue returnValue) {
      returnValue.Type = this.Visit(returnValue.Type);
      return returnValue;
    }

    /// <summary>
    /// Visits the specified right shift.
    /// </summary>
    /// <param name="rightShift">The right shift.</param>
    /// <returns></returns>
    public virtual IExpression Visit(RightShift rightShift) {
      return this.Visit((BinaryOperation)rightShift);
    }

    /// <summary>
    /// Visits the specified runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(RuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      runtimeArgumentHandleExpression.Type = this.Visit(runtimeArgumentHandleExpression.Type);
      return runtimeArgumentHandleExpression;
    }

    /// <summary>
    /// Visits the specified size of.
    /// </summary>
    /// <param name="sizeOf">The size of.</param>
    /// <returns></returns>
    public virtual IExpression Visit(SizeOf sizeOf) {
      sizeOf.TypeToSize = Visit(sizeOf.TypeToSize);
      sizeOf.Type = this.Visit(sizeOf.Type);
      return sizeOf;
    }

    /// <summary>
    /// Visits the specified stack array create.
    /// </summary>
    /// <param name="stackArrayCreate">The stack array create.</param>
    /// <returns></returns>
    public virtual IExpression Visit(StackArrayCreate stackArrayCreate) {
      stackArrayCreate.ElementType = Visit(stackArrayCreate.ElementType);
      stackArrayCreate.Size = Visit(stackArrayCreate.Size);
      stackArrayCreate.Type = this.Visit(stackArrayCreate.Type);
      return stackArrayCreate;
    }

    /// <summary>
    /// Visits the specified subtraction.
    /// </summary>
    /// <param name="subtraction">The subtraction.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Subtraction subtraction) {
      return this.Visit((BinaryOperation)subtraction);
    }

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
    /// <returns></returns>
    public virtual List<ISwitchCase> Visit(List<SwitchCase> switchCases) {
      List<ISwitchCase> newList = new List<ISwitchCase>();
      foreach (var switchCase in switchCases)
        newList.Add(Visit(switchCase));
      return newList;
    }

    /// <summary>
    /// Visits the specified switch case.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    /// <returns></returns>
    public virtual ISwitchCase Visit(SwitchCase switchCase) {
      if (!switchCase.IsDefault)
        switchCase.Expression = Visit(switchCase.Expression);
      switchCase.Body = Visit(switchCase.Body);
      return switchCase;
    }

    /// <summary>
    /// Visits the specified switch statement.
    /// </summary>
    /// <param name="switchStatement">The switch statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(SwitchStatement switchStatement) {
      switchStatement.Expression = Visit(switchStatement.Expression);
      switchStatement.Cases = Visit(switchStatement.Cases);
      return switchStatement;
    }

    /// <summary>
    /// Visits the specified target expression.
    /// </summary>
    /// <param name="targetExpression">The target expression.</param>
    /// <returns></returns>
    public virtual ITargetExpression Visit(TargetExpression targetExpression) {
      object def = targetExpression.Definition;
      ILocalDefinition/*?*/ loc = def as ILocalDefinition;
      if (loc != null)
        targetExpression.Definition = this.VisitReferenceTo(loc);
      else {
        IParameterDefinition/*?*/ par = targetExpression.Definition as IParameterDefinition;
        if (par != null)
          targetExpression.Definition = this.VisitReferenceTo(par);
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

    /// <summary>
    /// Visits the specified this reference.
    /// </summary>
    /// <param name="thisReference">The this reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ThisReference thisReference) {
      thisReference.Type = this.Visit(thisReference.Type);
      return thisReference;
    }

    /// <summary>
    /// Visits the specified throw statement.
    /// </summary>
    /// <param name="throwStatement">The throw statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ThrowStatement throwStatement) {
      if (throwStatement.Exception != null)
        throwStatement.Exception = Visit(throwStatement.Exception);
      return throwStatement;
    }

    /// <summary>
    /// Visits the specified try catch filter finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(TryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      tryCatchFilterFinallyStatement.TryBody = Visit(tryCatchFilterFinallyStatement.TryBody);
      tryCatchFilterFinallyStatement.CatchClauses = Visit(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        tryCatchFilterFinallyStatement.FinallyBody = Visit(tryCatchFilterFinallyStatement.FinallyBody);
      if (tryCatchFilterFinallyStatement.FaultBody != null)
        tryCatchFilterFinallyStatement.FaultBody = Visit(tryCatchFilterFinallyStatement.FaultBody);
      return tryCatchFilterFinallyStatement;
    }

    /// <summary>
    /// Visits the specified token of.
    /// </summary>
    /// <param name="tokenOf">The token of.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified type of.
    /// </summary>
    /// <param name="typeOf">The type of.</param>
    /// <returns></returns>
    public virtual IExpression Visit(TypeOf typeOf) {
      typeOf.TypeToGet = Visit(typeOf.TypeToGet);
      typeOf.Type = this.Visit(typeOf.Type);
      return typeOf;
    }

    /// <summary>
    /// Visits the specified unary negation.
    /// </summary>
    /// <param name="unaryNegation">The unary negation.</param>
    /// <returns></returns>
    public virtual IExpression Visit(UnaryNegation unaryNegation) {
      return this.Visit((UnaryOperation)unaryNegation);
    }

    /// <summary>
    /// Visits the specified unary plus.
    /// </summary>
    /// <param name="unaryPlus">The unary plus.</param>
    /// <returns></returns>
    public virtual IExpression Visit(UnaryPlus unaryPlus) {
      return this.Visit((UnaryOperation)unaryPlus);
    }

    /// <summary>
    /// Visits the specified vector length.
    /// </summary>
    /// <param name="vectorLength">Length of the vector.</param>
    /// <returns></returns>
    public virtual IExpression Visit(VectorLength vectorLength) {
      vectorLength.Vector = Visit(vectorLength.Vector);
      vectorLength.Type = this.Visit(vectorLength.Type);
      return vectorLength;
    }

    /// <summary>
    /// Visits the specified while do statement.
    /// </summary>
    /// <param name="whileDoStatement">The while do statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(WhileDoStatement whileDoStatement) {
      whileDoStatement.Condition = Visit(whileDoStatement.Condition);
      whileDoStatement.Body = Visit(whileDoStatement.Body);
      return whileDoStatement;
    }

    /// <summary>
    /// Visits the specified yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement">The yield break statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(YieldBreakStatement yieldBreakStatement) {
      return yieldBreakStatement;
    }

    /// <summary>
    /// Visits the specified yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement">The yield return statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(YieldReturnStatement yieldReturnStatement) {
      yieldReturnStatement.Expression = Visit(yieldReturnStatement.Expression);
      return yieldReturnStatement;
    }

    #endregion Virtual methods for subtypes to override, one per type in MutableCodeModel

    #region Methods that take an immutable type and return a type-specific mutable object, either by using the internal visitor, or else directly

    /// <summary>
    /// Visits the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    /// <returns></returns>
    public virtual IAddressableExpression Visit(IAddressableExpression addressableExpression) {
      AddressableExpression mutableAddressableExpression = addressableExpression as AddressableExpression;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableAddressableExpression == null)
        mutableAddressableExpression = new AddressableExpression(addressableExpression);
      return Visit(mutableAddressableExpression);
    }

    /// <summary>
    /// Visits the specified block statement.
    /// </summary>
    /// <param name="blockStatement">The block statement.</param>
    /// <returns></returns>
    public virtual IBlockStatement Visit(IBlockStatement blockStatement) {
      BlockStatement mutableBlockStatement = blockStatement as BlockStatement;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableBlockStatement == null)
        mutableBlockStatement = new BlockStatement(blockStatement);
      return Visit(mutableBlockStatement);
    }

    /// <summary>
    /// Visits the specified catch clause.
    /// </summary>
    /// <param name="catchClause">The catch clause.</param>
    /// <returns></returns>
    public virtual ICatchClause Visit(ICatchClause catchClause) {
      CatchClause mutableCatchClause = catchClause as CatchClause;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableCatchClause == null)
        mutableCatchClause = new CatchClause(catchClause);
      return Visit(mutableCatchClause);
    }

    /// <summary>
    /// Visits the specified catch clauses.
    /// </summary>
    /// <param name="catchClauses">The catch clauses.</param>
    /// <returns></returns>
    public virtual List<ICatchClause> Visit(List<ICatchClause> catchClauses) {
      List<ICatchClause> newList = new List<ICatchClause>();
      foreach (var catchClause in catchClauses) {
        ICatchClause mcc = this.Visit(catchClause);
        newList.Add(mcc);
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified compile time constant.
    /// </summary>
    /// <param name="compileTimeConstant">The compile time constant.</param>
    /// <returns></returns>
    public virtual ICompileTimeConstant Visit(ICompileTimeConstant compileTimeConstant) {
      CompileTimeConstant mutableCompileTimeConstant = compileTimeConstant as CompileTimeConstant;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableCompileTimeConstant == null)
        mutableCompileTimeConstant = new CompileTimeConstant(compileTimeConstant);
      return this.Visit(mutableCompileTimeConstant);
    }

    /// <summary>
    /// Visits the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(IExpression expression) {
      expression.Dispatch(this.createMutableType);
      return this.createMutableType.resultExpression;
    }

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(IStatement statement) {
      statement.Dispatch(this.createMutableType);
      return this.createMutableType.resultStatement;
    }

    /// <summary>
    /// Visits the specified statements.
    /// </summary>
    /// <param name="statements">The statements.</param>
    /// <returns></returns>
    public virtual List<IStatement> Visit(List<IStatement> statements) {
      List<IStatement> newList = new List<IStatement>();
      foreach (var statement in statements) {
        IStatement newStatement = this.Visit(statement);
        if (newStatement != CodeDummy.Block)
          newList.Add(newStatement);
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified switch case.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    /// <returns></returns>
    public virtual ISwitchCase Visit(ISwitchCase switchCase) {
      SwitchCase mutableSwitchCase = switchCase as SwitchCase;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableSwitchCase == null)
        mutableSwitchCase = new SwitchCase(switchCase);
      return Visit(mutableSwitchCase);
    }

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
    /// <returns></returns>
    public virtual List<ISwitchCase> Visit(List<ISwitchCase> switchCases) {
      List<ISwitchCase> newList = new List<ISwitchCase>();
      foreach (var switchCase in switchCases) {
        ISwitchCase swc = this.Visit(switchCase);
        if (swc != CodeDummy.SwitchCase)
          newList.Add(swc);
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified target expression.
    /// </summary>
    /// <param name="targetExpression">The target expression.</param>
    /// <returns></returns>
    public virtual ITargetExpression Visit(ITargetExpression targetExpression) {
      TargetExpression mutableTargetExpression = targetExpression as TargetExpression;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableTargetExpression == null)
        mutableTargetExpression = new TargetExpression(targetExpression);
      return Visit(mutableTargetExpression);
    }

    #endregion Methods that take an immutable type and return a type-specific mutable object, either by using the internal visitor, or else directly

#pragma warning disable 618
    private class CreateMutableType : BaseCodeVisitor, ICodeVisitor {
#pragma warning restore 618

      internal CodeMutator myCodeMutator;

      internal IExpression resultExpression = CodeDummy.Expression;
      internal IStatement resultStatement = CodeDummy.Block;

      bool alwaysMakeACopy;

      internal CreateMutableType(CodeMutator codeMutator, bool alwaysMakeACopy) {
        this.myCodeMutator = codeMutator;
        this.alwaysMakeACopy = alwaysMakeACopy;
      }

      #region overriding implementations of ICodeVisitor Members

      /// <summary>
      /// Visits the specified addition.
      /// </summary>
      /// <param name="addition">The addition.</param>
      public override void Visit(IAddition addition) {
        Addition/*?*/ mutableAddition = addition as Addition;
        if (alwaysMakeACopy || mutableAddition == null) mutableAddition = new Addition(addition);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddition);
      }

      /// <summary>
      /// Visits the specified addressable expression.
      /// </summary>
      /// <param name="addressableExpression">The addressable expression.</param>
      public override void Visit(IAddressableExpression addressableExpression) {
        AddressableExpression mutableAddressableExpression = addressableExpression as AddressableExpression;
        if (alwaysMakeACopy || mutableAddressableExpression == null) mutableAddressableExpression = new AddressableExpression(addressableExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressableExpression);
      }

      /// <summary>
      /// Visits the specified address dereference.
      /// </summary>
      /// <param name="addressDereference">The address dereference.</param>
      public override void Visit(IAddressDereference addressDereference) {
        AddressDereference mutableAddressDereference = addressDereference as AddressDereference;
        if (alwaysMakeACopy || mutableAddressDereference == null) mutableAddressDereference = new AddressDereference(addressDereference);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressDereference);
      }

      /// <summary>
      /// Visits the specified address of.
      /// </summary>
      /// <param name="addressOf">The address of.</param>
      public override void Visit(IAddressOf addressOf) {
        AddressOf mutableAddressOf = addressOf as AddressOf;
        if (alwaysMakeACopy || mutableAddressOf == null) mutableAddressOf = new AddressOf(addressOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressOf);
      }

      /// <summary>
      /// Visits the specified anonymous method.
      /// </summary>
      /// <param name="anonymousMethod">The anonymous method.</param>
      public override void Visit(IAnonymousDelegate anonymousMethod) {
        AnonymousDelegate mutableAnonymousDelegate = anonymousMethod as AnonymousDelegate;
        if (alwaysMakeACopy || mutableAnonymousDelegate == null) mutableAnonymousDelegate = new AnonymousDelegate(anonymousMethod);
        this.resultExpression = this.myCodeMutator.Visit(mutableAnonymousDelegate);
      }

      /// <summary>
      /// Visits the specified array indexer.
      /// </summary>
      /// <param name="arrayIndexer">The array indexer.</param>
      public override void Visit(IArrayIndexer arrayIndexer) {
        ArrayIndexer mutableArrayIndexer = arrayIndexer as ArrayIndexer;
        if (alwaysMakeACopy || mutableArrayIndexer == null) mutableArrayIndexer = new ArrayIndexer(arrayIndexer);
        this.resultExpression = this.myCodeMutator.Visit(mutableArrayIndexer);
      }

      /// <summary>
      /// Visits the specified assert statement.
      /// </summary>
      /// <param name="assertStatement">The assert statement.</param>
      public override void Visit(IAssertStatement assertStatement) {
        AssertStatement mutableAssertStatement = assertStatement as AssertStatement;
        if (alwaysMakeACopy || mutableAssertStatement == null) mutableAssertStatement = new AssertStatement(assertStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableAssertStatement);
      }

      /// <summary>
      /// Visits the specified assignment.
      /// </summary>
      /// <param name="assignment">The assignment.</param>
      public override void Visit(IAssignment assignment) {
        Assignment mutableAssignment = assignment as Assignment;
        if (alwaysMakeACopy || mutableAssignment == null) mutableAssignment = new Assignment(assignment);
        this.resultExpression = this.myCodeMutator.Visit(mutableAssignment);
      }

      /// <summary>
      /// Visits the specified assume statement.
      /// </summary>
      /// <param name="assumeStatement">The assume statement.</param>
      public override void Visit(IAssumeStatement assumeStatement) {
        AssumeStatement mutableAssumeStatement = assumeStatement as AssumeStatement;
        if (alwaysMakeACopy || mutableAssumeStatement == null) mutableAssumeStatement = new AssumeStatement(assumeStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableAssumeStatement);
      }

      /// <summary>
      /// Visits the specified bitwise and.
      /// </summary>
      /// <param name="bitwiseAnd">The bitwise and.</param>
      public override void Visit(IBitwiseAnd bitwiseAnd) {
        BitwiseAnd mutableBitwiseAnd = bitwiseAnd as BitwiseAnd;
        if (alwaysMakeACopy || mutableBitwiseAnd == null) mutableBitwiseAnd = new BitwiseAnd(bitwiseAnd);
        this.resultExpression = this.myCodeMutator.Visit(mutableBitwiseAnd);
      }

      /// <summary>
      /// Visits the specified bitwise or.
      /// </summary>
      /// <param name="bitwiseOr">The bitwise or.</param>
      public override void Visit(IBitwiseOr bitwiseOr) {
        BitwiseOr mutableBitwiseOr = bitwiseOr as BitwiseOr;
        if (alwaysMakeACopy || mutableBitwiseOr == null) mutableBitwiseOr = new BitwiseOr(bitwiseOr);
        this.resultExpression = this.myCodeMutator.Visit(mutableBitwiseOr);
      }

      /// <summary>
      /// Visits the specified block expression.
      /// </summary>
      /// <param name="blockExpression">The block expression.</param>
      public override void Visit(IBlockExpression blockExpression) {
        BlockExpression mutableBlockExpression = blockExpression as BlockExpression;
        if (alwaysMakeACopy || mutableBlockExpression == null) mutableBlockExpression = new BlockExpression(blockExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableBlockExpression);
      }

      /// <summary>
      /// Visits the specified block.
      /// </summary>
      /// <param name="block">The block.</param>
      public override void Visit(IBlockStatement block) {
        BlockStatement mutableBlockStatement = block as BlockStatement;
        if (alwaysMakeACopy || mutableBlockStatement == null) mutableBlockStatement = new BlockStatement(block);
        this.resultStatement = this.myCodeMutator.Visit(mutableBlockStatement);
      }

      /// <summary>
      /// Visits the specified break statement.
      /// </summary>
      /// <param name="breakStatement">The break statement.</param>
      public override void Visit(IBreakStatement breakStatement) {
        BreakStatement mutableBreakStatement = breakStatement as BreakStatement;
        if (alwaysMakeACopy || mutableBreakStatement == null) mutableBreakStatement = new BreakStatement(breakStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableBreakStatement);
      }

      /// <summary>
      /// Visits the specified bound expression.
      /// </summary>
      /// <param name="boundExpression">The bound expression.</param>
      public override void Visit(IBoundExpression boundExpression) {
        BoundExpression mutableBoundExpression = boundExpression as BoundExpression;
        if (alwaysMakeACopy || mutableBoundExpression == null) mutableBoundExpression = new BoundExpression(boundExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableBoundExpression);
      }

      /// <summary>
      /// Visits the specified cast if possible.
      /// </summary>
      /// <param name="castIfPossible">The cast if possible.</param>
      public override void Visit(ICastIfPossible castIfPossible) {
        CastIfPossible mutableCastIfPossible = castIfPossible as CastIfPossible;
        if (alwaysMakeACopy || mutableCastIfPossible == null) mutableCastIfPossible = new CastIfPossible(castIfPossible);
        this.resultExpression = this.myCodeMutator.Visit(mutableCastIfPossible);
      }

      /// <summary>
      /// Visits the specified check if instance.
      /// </summary>
      /// <param name="checkIfInstance">The check if instance.</param>
      public override void Visit(ICheckIfInstance checkIfInstance) {
        CheckIfInstance mutableCheckIfInstance = checkIfInstance as CheckIfInstance;
        if (alwaysMakeACopy || mutableCheckIfInstance == null) mutableCheckIfInstance = new CheckIfInstance(checkIfInstance);
        this.resultExpression = this.myCodeMutator.Visit(mutableCheckIfInstance);
      }

      /// <summary>
      /// Visits the specified constant.
      /// </summary>
      /// <param name="constant">The constant.</param>
      public override void Visit(ICompileTimeConstant constant) {
        CompileTimeConstant mutableCompileTimeConstant = constant as CompileTimeConstant;
        if (alwaysMakeACopy || mutableCompileTimeConstant == null) mutableCompileTimeConstant = new CompileTimeConstant(constant);
        this.resultExpression = this.myCodeMutator.Visit(mutableCompileTimeConstant);
      }

      /// <summary>
      /// Visits the specified conversion.
      /// </summary>
      /// <param name="conversion">The conversion.</param>
      public override void Visit(IConversion conversion) {
        Conversion mutableConversion = conversion as Conversion;
        if (alwaysMakeACopy || mutableConversion == null) mutableConversion = new Conversion(conversion);
        this.resultExpression = this.myCodeMutator.Visit(mutableConversion);
      }

      /// <summary>
      /// Visits the specified conditional.
      /// </summary>
      /// <param name="conditional">The conditional.</param>
      public override void Visit(IConditional conditional) {
        Conditional mutableConditional = conditional as Conditional;
        if (alwaysMakeACopy || mutableConditional == null) mutableConditional = new Conditional(conditional);
        this.resultExpression = this.myCodeMutator.Visit(mutableConditional);
      }

      /// <summary>
      /// Visits the specified conditional statement.
      /// </summary>
      /// <param name="conditionalStatement">The conditional statement.</param>
      public override void Visit(IConditionalStatement conditionalStatement) {
        ConditionalStatement mutableConditionalStatement = conditionalStatement as ConditionalStatement;
        if (alwaysMakeACopy || mutableConditionalStatement == null) mutableConditionalStatement = new ConditionalStatement(conditionalStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableConditionalStatement);
      }

      /// <summary>
      /// Visits the specified continue statement.
      /// </summary>
      /// <param name="continueStatement">The continue statement.</param>
      public override void Visit(IContinueStatement continueStatement) {
        ContinueStatement mutableContinueStatement = continueStatement as ContinueStatement;
        if (alwaysMakeACopy || mutableContinueStatement == null) mutableContinueStatement = new ContinueStatement(continueStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableContinueStatement);
      }

      /// <summary>
      /// Visits the specified copy memory statement.
      /// </summary>
      /// <param name="copyMemoryStatement">The copy memory statement.</param>
      public override void Visit(ICopyMemoryStatement copyMemoryStatement) {
        CopyMemoryStatement mutableCopyMemoryStatement = copyMemoryStatement as CopyMemoryStatement;
        if (alwaysMakeACopy || mutableCopyMemoryStatement == null) mutableCopyMemoryStatement = new CopyMemoryStatement(copyMemoryStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableCopyMemoryStatement);
      }

      /// <summary>
      /// Visits the specified create array.
      /// </summary>
      /// <param name="createArray">The create array.</param>
      public override void Visit(ICreateArray createArray) {
        CreateArray mutableCreateArray = createArray as CreateArray;
        if (alwaysMakeACopy || mutableCreateArray == null) mutableCreateArray = new CreateArray(createArray);
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateArray);
      }

      /// <summary>
      /// Visits the specified create delegate instance.
      /// </summary>
      /// <param name="createDelegateInstance">The create delegate instance.</param>
      public override void Visit(ICreateDelegateInstance createDelegateInstance) {
        CreateDelegateInstance mutableCreateDelegateInstance = createDelegateInstance as CreateDelegateInstance;
        if (alwaysMakeACopy || mutableCreateDelegateInstance == null) mutableCreateDelegateInstance = new CreateDelegateInstance(createDelegateInstance);
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateDelegateInstance);
      }

      /// <summary>
      /// Visits the specified create object instance.
      /// </summary>
      /// <param name="createObjectInstance">The create object instance.</param>
      public override void Visit(ICreateObjectInstance createObjectInstance) {
        CreateObjectInstance mutableCreateObjectInstance = createObjectInstance as CreateObjectInstance;
        if (alwaysMakeACopy || mutableCreateObjectInstance == null) mutableCreateObjectInstance = new CreateObjectInstance(createObjectInstance);
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateObjectInstance);
      }

      /// <summary>
      /// Visits the specified debugger break statement.
      /// </summary>
      /// <param name="debuggerBreakStatement">The debugger break statement.</param>
      public override void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        DebuggerBreakStatement mutableDebuggerBreakStatement = debuggerBreakStatement as DebuggerBreakStatement;
        if (alwaysMakeACopy || mutableDebuggerBreakStatement == null) mutableDebuggerBreakStatement = new DebuggerBreakStatement(debuggerBreakStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableDebuggerBreakStatement);
      }

      /// <summary>
      /// Visits the specified default value.
      /// </summary>
      /// <param name="defaultValue">The default value.</param>
      public override void Visit(IDefaultValue defaultValue) {
        DefaultValue mutableDefaultValue = defaultValue as DefaultValue;
        if (alwaysMakeACopy || mutableDefaultValue == null) mutableDefaultValue = new DefaultValue(defaultValue);
        this.resultExpression = this.myCodeMutator.Visit(mutableDefaultValue);
      }

      /// <summary>
      /// Visits the specified division.
      /// </summary>
      /// <param name="division">The division.</param>
      public override void Visit(IDivision division) {
        Division mutableDivision = division as Division;
        if (alwaysMakeACopy || mutableDivision == null) mutableDivision = new Division(division);
        this.resultExpression = this.myCodeMutator.Visit(mutableDivision);
      }

      /// <summary>
      /// Visits the specified do until statement.
      /// </summary>
      /// <param name="doUntilStatement">The do until statement.</param>
      public override void Visit(IDoUntilStatement doUntilStatement) {
        DoUntilStatement mutableDoUntilStatement = doUntilStatement as DoUntilStatement;
        if (alwaysMakeACopy || mutableDoUntilStatement == null) mutableDoUntilStatement = new DoUntilStatement(doUntilStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableDoUntilStatement);
      }

      /// <summary>
      /// Performs some computation with the given dup value expression.
      /// </summary>
      /// <param name="dupValue"></param>
      public override void Visit(IDupValue dupValue) {
        DupValue mutableDupValue = dupValue as DupValue;
        if (alwaysMakeACopy || mutableDupValue == null) mutableDupValue = new DupValue(dupValue);
        this.resultExpression = this.myCodeMutator.Visit(mutableDupValue);
      }

      /// <summary>
      /// Visits the specified empty statement.
      /// </summary>
      /// <param name="emptyStatement">The empty statement.</param>
      public override void Visit(IEmptyStatement emptyStatement) {
        EmptyStatement mutableEmptyStatement = emptyStatement as EmptyStatement;
        if (alwaysMakeACopy || mutableEmptyStatement == null) mutableEmptyStatement = new EmptyStatement(emptyStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableEmptyStatement);
      }

      /// <summary>
      /// Visits the specified equality.
      /// </summary>
      /// <param name="equality">The equality.</param>
      public override void Visit(IEquality equality) {
        Equality mutableEquality = equality as Equality;
        if (alwaysMakeACopy || mutableEquality == null) mutableEquality = new Equality(equality);
        this.resultExpression = this.myCodeMutator.Visit(mutableEquality);
      }

      /// <summary>
      /// Visits the specified exclusive or.
      /// </summary>
      /// <param name="exclusiveOr">The exclusive or.</param>
      public override void Visit(IExclusiveOr exclusiveOr) {
        ExclusiveOr mutableExclusiveOr = exclusiveOr as ExclusiveOr;
        if (alwaysMakeACopy || mutableExclusiveOr == null) mutableExclusiveOr = new ExclusiveOr(exclusiveOr);
        this.resultExpression = this.myCodeMutator.Visit(mutableExclusiveOr);
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
        ExpressionStatement mutableExpressionStatement = expressionStatement as ExpressionStatement;
        if (alwaysMakeACopy || mutableExpressionStatement == null) mutableExpressionStatement = new ExpressionStatement(expressionStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableExpressionStatement);
      }

      /// <summary>
      /// Visits the specified fill memory statement.
      /// </summary>
      /// <param name="fillMemoryStatement">The fill memory statement.</param>
      public override void Visit(IFillMemoryStatement fillMemoryStatement) {
        FillMemoryStatement mutableFillMemoryStatement = fillMemoryStatement as FillMemoryStatement;
        if (alwaysMakeACopy || mutableFillMemoryStatement == null) mutableFillMemoryStatement = new FillMemoryStatement(fillMemoryStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableFillMemoryStatement);
      }

      /// <summary>
      /// Visits the specified for each statement.
      /// </summary>
      /// <param name="forEachStatement">For each statement.</param>
      public override void Visit(IForEachStatement forEachStatement) {
        ForEachStatement mutableForEachStatement = forEachStatement as ForEachStatement;
        if (alwaysMakeACopy || mutableForEachStatement == null) mutableForEachStatement = new ForEachStatement(forEachStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableForEachStatement);
      }

      /// <summary>
      /// Visits the specified for statement.
      /// </summary>
      /// <param name="forStatement">For statement.</param>
      public override void Visit(IForStatement forStatement) {
        ForStatement mutableForStatement = forStatement as ForStatement;
        if (alwaysMakeACopy || mutableForStatement == null) mutableForStatement = new ForStatement(forStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableForStatement);
      }

      /// <summary>
      /// Visits the specified goto statement.
      /// </summary>
      /// <param name="gotoStatement">The goto statement.</param>
      public override void Visit(IGotoStatement gotoStatement) {
        GotoStatement mutableGotoStatement = gotoStatement as GotoStatement;
        if (alwaysMakeACopy || mutableGotoStatement == null) mutableGotoStatement = new GotoStatement(gotoStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableGotoStatement);
      }

      /// <summary>
      /// Visits the specified goto switch case statement.
      /// </summary>
      /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
      public override void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        GotoSwitchCaseStatement mutableGotoSwitchCaseStatement = gotoSwitchCaseStatement as GotoSwitchCaseStatement;
        if (alwaysMakeACopy || mutableGotoSwitchCaseStatement == null) mutableGotoSwitchCaseStatement = new GotoSwitchCaseStatement(gotoSwitchCaseStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableGotoSwitchCaseStatement);
      }

      /// <summary>
      /// Visits the specified get type of typed reference.
      /// </summary>
      /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
      public override void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        GetTypeOfTypedReference mutableGetTypeOfTypedReference = getTypeOfTypedReference as GetTypeOfTypedReference;
        if (alwaysMakeACopy || mutableGetTypeOfTypedReference == null) mutableGetTypeOfTypedReference = new GetTypeOfTypedReference(getTypeOfTypedReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableGetTypeOfTypedReference);
      }

      /// <summary>
      /// Visits the specified get value of typed reference.
      /// </summary>
      /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
      public override void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        GetValueOfTypedReference mutableGetValueOfTypedReference = getValueOfTypedReference as GetValueOfTypedReference;
        if (alwaysMakeACopy || mutableGetValueOfTypedReference == null) mutableGetValueOfTypedReference = new GetValueOfTypedReference(getValueOfTypedReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableGetValueOfTypedReference);
      }

      /// <summary>
      /// Visits the specified greater than.
      /// </summary>
      /// <param name="greaterThan">The greater than.</param>
      public override void Visit(IGreaterThan greaterThan) {
        GreaterThan mutableGreaterThan = greaterThan as GreaterThan;
        if (alwaysMakeACopy || mutableGreaterThan == null) mutableGreaterThan = new GreaterThan(greaterThan);
        this.resultExpression = this.myCodeMutator.Visit(mutableGreaterThan);
      }

      /// <summary>
      /// Visits the specified greater than or equal.
      /// </summary>
      /// <param name="greaterThanOrEqual">The greater than or equal.</param>
      public override void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        GreaterThanOrEqual mutableGreaterThanOrEqual = greaterThanOrEqual as GreaterThanOrEqual;
        if (alwaysMakeACopy || mutableGreaterThanOrEqual == null) mutableGreaterThanOrEqual = new GreaterThanOrEqual(greaterThanOrEqual);
        this.resultExpression = this.myCodeMutator.Visit(mutableGreaterThanOrEqual);
      }

      /// <summary>
      /// Visits the specified labeled statement.
      /// </summary>
      /// <param name="labeledStatement">The labeled statement.</param>
      public override void Visit(ILabeledStatement labeledStatement) {
        LabeledStatement mutableLabeledStatement = labeledStatement as LabeledStatement;
        if (alwaysMakeACopy || mutableLabeledStatement == null) mutableLabeledStatement = new LabeledStatement(labeledStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableLabeledStatement);
      }

      /// <summary>
      /// Visits the specified left shift.
      /// </summary>
      /// <param name="leftShift">The left shift.</param>
      public override void Visit(ILeftShift leftShift) {
        LeftShift mutableLeftShift = leftShift as LeftShift;
        if (alwaysMakeACopy || mutableLeftShift == null) mutableLeftShift = new LeftShift(leftShift);
        this.resultExpression = this.myCodeMutator.Visit(mutableLeftShift);
      }

      /// <summary>
      /// Visits the specified less than.
      /// </summary>
      /// <param name="lessThan">The less than.</param>
      public override void Visit(ILessThan lessThan) {
        LessThan mutableLessThan = lessThan as LessThan;
        if (alwaysMakeACopy || mutableLessThan == null) mutableLessThan = new LessThan(lessThan);
        this.resultExpression = this.myCodeMutator.Visit(mutableLessThan);
      }

      /// <summary>
      /// Visits the specified less than or equal.
      /// </summary>
      /// <param name="lessThanOrEqual">The less than or equal.</param>
      public override void Visit(ILessThanOrEqual lessThanOrEqual) {
        LessThanOrEqual mutableLessThanOrEqual = lessThanOrEqual as LessThanOrEqual;
        if (alwaysMakeACopy || mutableLessThanOrEqual == null) mutableLessThanOrEqual = new LessThanOrEqual(lessThanOrEqual);
        this.resultExpression = this.myCodeMutator.Visit(mutableLessThanOrEqual);
      }

      /// <summary>
      /// Visits the specified local declaration statement.
      /// </summary>
      /// <param name="localDeclarationStatement">The local declaration statement.</param>
      public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        LocalDeclarationStatement mutableLocalDeclarationStatement = localDeclarationStatement as LocalDeclarationStatement;
        if (alwaysMakeACopy || mutableLocalDeclarationStatement == null) mutableLocalDeclarationStatement = new LocalDeclarationStatement(localDeclarationStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableLocalDeclarationStatement);
      }

      /// <summary>
      /// Visits the specified lock statement.
      /// </summary>
      /// <param name="lockStatement">The lock statement.</param>
      public override void Visit(ILockStatement lockStatement) {
        LockStatement mutableLockStatement = lockStatement as LockStatement;
        if (alwaysMakeACopy || mutableLockStatement == null) mutableLockStatement = new LockStatement(lockStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableLockStatement);
      }

      /// <summary>
      /// Visits the specified logical not.
      /// </summary>
      /// <param name="logicalNot">The logical not.</param>
      public override void Visit(ILogicalNot logicalNot) {
        LogicalNot mutableLogicalNot = logicalNot as LogicalNot;
        if (alwaysMakeACopy || mutableLogicalNot == null) mutableLogicalNot = new LogicalNot(logicalNot);
        this.resultExpression = this.myCodeMutator.Visit(mutableLogicalNot);
      }

      /// <summary>
      /// Visits the specified make typed reference.
      /// </summary>
      /// <param name="makeTypedReference">The make typed reference.</param>
      public override void Visit(IMakeTypedReference makeTypedReference) {
        MakeTypedReference mutableMakeTypedReference = makeTypedReference as MakeTypedReference;
        if (alwaysMakeACopy || mutableMakeTypedReference == null) mutableMakeTypedReference = new MakeTypedReference(makeTypedReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableMakeTypedReference);
      }

      /// <summary>
      /// Visits the specified method call.
      /// </summary>
      /// <param name="methodCall">The method call.</param>
      public override void Visit(IMethodCall methodCall) {
        MethodCall mutableMethodCall = methodCall as MethodCall;
        if (alwaysMakeACopy || mutableMethodCall == null) mutableMethodCall = new MethodCall(methodCall);
        this.resultExpression = this.myCodeMutator.Visit(mutableMethodCall);
      }

      /// <summary>
      /// Visits the specified modulus.
      /// </summary>
      /// <param name="modulus">The modulus.</param>
      public override void Visit(IModulus modulus) {
        Modulus mutableModulus = modulus as Modulus;
        if (alwaysMakeACopy || mutableModulus == null) mutableModulus = new Modulus(modulus);
        this.resultExpression = this.myCodeMutator.Visit(mutableModulus);
      }

      /// <summary>
      /// Visits the specified multiplication.
      /// </summary>
      /// <param name="multiplication">The multiplication.</param>
      public override void Visit(IMultiplication multiplication) {
        Multiplication mutableMultiplication = multiplication as Multiplication;
        if (alwaysMakeACopy || mutableMultiplication == null) mutableMultiplication = new Multiplication(multiplication);
        this.resultExpression = this.myCodeMutator.Visit(mutableMultiplication);
      }

      /// <summary>
      /// Visits the specified named argument.
      /// </summary>
      /// <param name="namedArgument">The named argument.</param>
      public override void Visit(INamedArgument namedArgument) {
        NamedArgument mutableNamedArgument = namedArgument as NamedArgument;
        if (alwaysMakeACopy || mutableNamedArgument == null) mutableNamedArgument = new NamedArgument(namedArgument);
        this.resultExpression = this.myCodeMutator.Visit(mutableNamedArgument);
      }

      /// <summary>
      /// Visits the specified not equality.
      /// </summary>
      /// <param name="notEquality">The not equality.</param>
      public override void Visit(INotEquality notEquality) {
        NotEquality mutableNotEquality = notEquality as NotEquality;
        if (alwaysMakeACopy || mutableNotEquality == null) mutableNotEquality = new NotEquality(notEquality);
        this.resultExpression = this.myCodeMutator.Visit(mutableNotEquality);
      }

      /// <summary>
      /// Visits the specified old value.
      /// </summary>
      /// <param name="oldValue">The old value.</param>
      public override void Visit(IOldValue oldValue) {
        OldValue mutableOldValue = oldValue as OldValue;
        if (alwaysMakeACopy || mutableOldValue == null) mutableOldValue = new OldValue(oldValue);
        this.resultExpression = this.myCodeMutator.Visit(mutableOldValue);
      }

      /// <summary>
      /// Visits the specified ones complement.
      /// </summary>
      /// <param name="onesComplement">The ones complement.</param>
      public override void Visit(IOnesComplement onesComplement) {
        OnesComplement mutableOnesComplement = onesComplement as OnesComplement;
        if (alwaysMakeACopy || mutableOnesComplement == null) mutableOnesComplement = new OnesComplement(onesComplement);
        this.resultExpression = this.myCodeMutator.Visit(mutableOnesComplement);
      }

      /// <summary>
      /// Visits the specified out argument.
      /// </summary>
      /// <param name="outArgument">The out argument.</param>
      public override void Visit(IOutArgument outArgument) {
        OutArgument mutableOutArgument = outArgument as OutArgument;
        if (alwaysMakeACopy || mutableOutArgument == null) mutableOutArgument = new OutArgument(outArgument);
        this.resultExpression = this.myCodeMutator.Visit(mutableOutArgument);
      }

      /// <summary>
      /// Visits the specified pointer call.
      /// </summary>
      /// <param name="pointerCall">The pointer call.</param>
      public override void Visit(IPointerCall pointerCall) {
        PointerCall mutablePointerCall = pointerCall as PointerCall;
        if (alwaysMakeACopy || mutablePointerCall == null) mutablePointerCall = new PointerCall(pointerCall);
        this.resultExpression = this.myCodeMutator.Visit(mutablePointerCall);
      }

      /// <summary>
      /// Performs some computation with the given pop value expression.
      /// </summary>
      /// <param name="popValue"></param>
      public override void Visit(IPopValue popValue) {
        PopValue mutablePopValue = popValue as PopValue;
        if (alwaysMakeACopy || mutablePopValue == null) mutablePopValue = new PopValue(popValue);
        this.resultExpression = this.myCodeMutator.Visit(mutablePopValue);
      }

      /// <summary>
      /// Performs some computation with the given push statement.
      /// </summary>
      /// <param name="pushStatement"></param>
      public override void Visit(IPushStatement pushStatement) {
        PushStatement mutablePushStatement = pushStatement as PushStatement;
        if (alwaysMakeACopy || mutablePushStatement == null) mutablePushStatement = new PushStatement(pushStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutablePushStatement);
      }

      /// <summary>
      /// Visits the specified ref argument.
      /// </summary>
      /// <param name="refArgument">The ref argument.</param>
      public override void Visit(IRefArgument refArgument) {
        RefArgument mutableRefArgument = refArgument as RefArgument;
        if (alwaysMakeACopy || mutableRefArgument == null) mutableRefArgument = new RefArgument(refArgument);
        this.resultExpression = this.myCodeMutator.Visit(mutableRefArgument);
      }

      /// <summary>
      /// Visits the specified resource use statement.
      /// </summary>
      /// <param name="resourceUseStatement">The resource use statement.</param>
      public override void Visit(IResourceUseStatement resourceUseStatement) {
        ResourceUseStatement mutableResourceUseStatement = resourceUseStatement as ResourceUseStatement;
        if (alwaysMakeACopy || mutableResourceUseStatement == null) mutableResourceUseStatement = new ResourceUseStatement(resourceUseStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableResourceUseStatement);
      }

      /// <summary>
      /// Visits the specified return value.
      /// </summary>
      /// <param name="returnValue">The return value.</param>
      public override void Visit(IReturnValue returnValue) {
        ReturnValue mutableReturnValue = returnValue as ReturnValue;
        if (alwaysMakeACopy || mutableReturnValue == null) mutableReturnValue = new ReturnValue(returnValue);
        this.resultExpression = this.myCodeMutator.Visit(mutableReturnValue);
      }

      /// <summary>
      /// Visits the specified rethrow statement.
      /// </summary>
      /// <param name="rethrowStatement">The rethrow statement.</param>
      public override void Visit(IRethrowStatement rethrowStatement) {
        RethrowStatement mutableRethrowStatement = rethrowStatement as RethrowStatement;
        if (alwaysMakeACopy || mutableRethrowStatement == null) mutableRethrowStatement = new RethrowStatement(rethrowStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableRethrowStatement);
      }

      /// <summary>
      /// Visits the specified return statement.
      /// </summary>
      /// <param name="returnStatement">The return statement.</param>
      public override void Visit(IReturnStatement returnStatement) {
        ReturnStatement mutableReturnStatement = returnStatement as ReturnStatement;
        if (alwaysMakeACopy || mutableReturnStatement == null) mutableReturnStatement = new ReturnStatement(returnStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableReturnStatement);
      }

      /// <summary>
      /// Visits the specified right shift.
      /// </summary>
      /// <param name="rightShift">The right shift.</param>
      public override void Visit(IRightShift rightShift) {
        RightShift mutableRightShift = rightShift as RightShift;
        if (alwaysMakeACopy || mutableRightShift == null) mutableRightShift = new RightShift(rightShift);
        this.resultExpression = this.myCodeMutator.Visit(mutableRightShift);
      }

      /// <summary>
      /// Visits the specified runtime argument handle expression.
      /// </summary>
      /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
      public override void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        RuntimeArgumentHandleExpression mutableRuntimeArgumentHandleExpression = runtimeArgumentHandleExpression as RuntimeArgumentHandleExpression;
        if (alwaysMakeACopy || mutableRuntimeArgumentHandleExpression == null) mutableRuntimeArgumentHandleExpression = new RuntimeArgumentHandleExpression(runtimeArgumentHandleExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableRuntimeArgumentHandleExpression);
      }

      /// <summary>
      /// Visits the specified size of.
      /// </summary>
      /// <param name="sizeOf">The size of.</param>
      public override void Visit(ISizeOf sizeOf) {
        SizeOf mutableSizeOf = sizeOf as SizeOf;
        if (alwaysMakeACopy || mutableSizeOf == null) mutableSizeOf = new SizeOf(sizeOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableSizeOf);
      }

      /// <summary>
      /// Visits the specified stack array create.
      /// </summary>
      /// <param name="stackArrayCreate">The stack array create.</param>
      public override void Visit(IStackArrayCreate stackArrayCreate) {
        StackArrayCreate mutableStackArrayCreate = stackArrayCreate as StackArrayCreate;
        if (alwaysMakeACopy || mutableStackArrayCreate == null) mutableStackArrayCreate = new StackArrayCreate(stackArrayCreate);
        this.resultExpression = this.myCodeMutator.Visit(mutableStackArrayCreate);
      }

      /// <summary>
      /// Visits the specified subtraction.
      /// </summary>
      /// <param name="subtraction">The subtraction.</param>
      public override void Visit(ISubtraction subtraction) {
        Subtraction mutableSubtraction = subtraction as Subtraction;
        if (alwaysMakeACopy || mutableSubtraction == null) mutableSubtraction = new Subtraction(subtraction);
        this.resultExpression = this.myCodeMutator.Visit(mutableSubtraction);
      }

      /// <summary>
      /// Visits the specified switch statement.
      /// </summary>
      /// <param name="switchStatement">The switch statement.</param>
      public override void Visit(ISwitchStatement switchStatement) {
        SwitchStatement mutableSwitchStatement = switchStatement as SwitchStatement;
        if (alwaysMakeACopy || mutableSwitchStatement == null) mutableSwitchStatement = new SwitchStatement(switchStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableSwitchStatement);
      }

      /// <summary>
      /// Visits the specified target expression.
      /// </summary>
      /// <param name="targetExpression">The target expression.</param>
      public override void Visit(ITargetExpression targetExpression) {
        TargetExpression mutableTargetExpression = targetExpression as TargetExpression;
        if (alwaysMakeACopy || mutableTargetExpression == null) mutableTargetExpression = new TargetExpression(targetExpression);
        this.resultExpression = this.myCodeMutator.Visit(mutableTargetExpression);
      }

      /// <summary>
      /// Visits the specified this reference.
      /// </summary>
      /// <param name="thisReference">The this reference.</param>
      public override void Visit(IThisReference thisReference) {
        ThisReference mutableThisReference = thisReference as ThisReference;
        if (alwaysMakeACopy || mutableThisReference == null) mutableThisReference = new ThisReference(thisReference);
        this.resultExpression = this.myCodeMutator.Visit(mutableThisReference);
      }

      /// <summary>
      /// Visits the specified throw statement.
      /// </summary>
      /// <param name="throwStatement">The throw statement.</param>
      public override void Visit(IThrowStatement throwStatement) {
        ThrowStatement mutableThrowStatement = throwStatement as ThrowStatement;
        if (alwaysMakeACopy || mutableThrowStatement == null) mutableThrowStatement = new ThrowStatement(throwStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableThrowStatement);
      }

      /// <summary>
      /// Visits the specified try catch filter finally statement.
      /// </summary>
      /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
      public override void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        TryCatchFinallyStatement mutableTryCatchFinallyStatement = tryCatchFilterFinallyStatement as TryCatchFinallyStatement;
        if (alwaysMakeACopy || mutableTryCatchFinallyStatement == null) mutableTryCatchFinallyStatement = new TryCatchFinallyStatement(tryCatchFilterFinallyStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableTryCatchFinallyStatement);
      }

      /// <summary>
      /// Visits the specified token of.
      /// </summary>
      /// <param name="tokenOf">The token of.</param>
      public override void Visit(ITokenOf tokenOf) {
        TokenOf mutableTokenOf = tokenOf as TokenOf;
        if (alwaysMakeACopy || mutableTokenOf == null) mutableTokenOf = new TokenOf(tokenOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableTokenOf);
      }

      /// <summary>
      /// Visits the specified type of.
      /// </summary>
      /// <param name="typeOf">The type of.</param>
      public override void Visit(ITypeOf typeOf) {
        TypeOf mutableTypeOf = typeOf as TypeOf;
        if (alwaysMakeACopy || mutableTypeOf == null) mutableTypeOf = new TypeOf(typeOf);
        this.resultExpression = this.myCodeMutator.Visit(mutableTypeOf);
      }

      /// <summary>
      /// Visits the specified unary negation.
      /// </summary>
      /// <param name="unaryNegation">The unary negation.</param>
      public override void Visit(IUnaryNegation unaryNegation) {
        UnaryNegation mutableUnaryNegation = unaryNegation as UnaryNegation;
        if (alwaysMakeACopy || mutableUnaryNegation == null) mutableUnaryNegation = new UnaryNegation(unaryNegation);
        this.resultExpression = this.myCodeMutator.Visit(mutableUnaryNegation);
      }

      /// <summary>
      /// Visits the specified unary plus.
      /// </summary>
      /// <param name="unaryPlus">The unary plus.</param>
      public override void Visit(IUnaryPlus unaryPlus) {
        UnaryPlus mutableUnaryPlus = unaryPlus as UnaryPlus;
        if (alwaysMakeACopy || mutableUnaryPlus == null) mutableUnaryPlus = new UnaryPlus(unaryPlus);
        this.resultExpression = this.myCodeMutator.Visit(mutableUnaryPlus);
      }

      /// <summary>
      /// Visits the specified vector length.
      /// </summary>
      /// <param name="vectorLength">Length of the vector.</param>
      public override void Visit(IVectorLength vectorLength) {
        VectorLength mutableVectorLength = vectorLength as VectorLength;
        if (alwaysMakeACopy || mutableVectorLength == null) mutableVectorLength = new VectorLength(vectorLength);
        this.resultExpression = this.myCodeMutator.Visit(mutableVectorLength);
      }

      /// <summary>
      /// Visits the specified while do statement.
      /// </summary>
      /// <param name="whileDoStatement">The while do statement.</param>
      public override void Visit(IWhileDoStatement whileDoStatement) {
        WhileDoStatement mutableWhileDoStatement = whileDoStatement as WhileDoStatement;
        if (alwaysMakeACopy || mutableWhileDoStatement == null) mutableWhileDoStatement = new WhileDoStatement(whileDoStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableWhileDoStatement);
      }

      /// <summary>
      /// Visits the specified yield break statement.
      /// </summary>
      /// <param name="yieldBreakStatement">The yield break statement.</param>
      public override void Visit(IYieldBreakStatement yieldBreakStatement) {
        YieldBreakStatement mutableYieldBreakStatement = yieldBreakStatement as YieldBreakStatement;
        if (alwaysMakeACopy || mutableYieldBreakStatement == null) mutableYieldBreakStatement = new YieldBreakStatement(yieldBreakStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableYieldBreakStatement);
      }

      /// <summary>
      /// Visits the specified yield return statement.
      /// </summary>
      /// <param name="yieldReturnStatement">The yield return statement.</param>
      public override void Visit(IYieldReturnStatement yieldReturnStatement) {
        YieldReturnStatement mutableYieldReturnStatement = yieldReturnStatement as YieldReturnStatement;
        if (alwaysMakeACopy || mutableYieldReturnStatement == null) mutableYieldReturnStatement = new YieldReturnStatement(yieldReturnStatement);
        this.resultStatement = this.myCodeMutator.Visit(mutableYieldReturnStatement);
      }

      #endregion overriding implementations of ICodeVisitor Members
    }
  }

  /// <summary>
  /// Use this as a base class when you define a code mutator that mutates ONLY method bodies (in other words
  /// all metadata definitions, including parameter definitions and local definition remain unchanged).
  /// This class has overrides for Visit(IFieldReference), Visit(IMethodReference), 
  /// Visit(ITypeReference), VisitReferenceTo(ILocalDefinition) and VisitReferenceTo(IParameterDefinition) that make sure to not modify the references.
  /// </summary>
  [Obsolete("Please use CodeRewriter")]
  public class MethodBodyCodeMutator : CodeMutatingVisitor {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public MethodBodyCodeMutator(IMetadataHost host)
      : base(host) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable"></param>
    public MethodBodyCodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public MethodBodyCodeMutator(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, sourceLocationProvider) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable"></param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public MethodBodyCodeMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host, sourceLocationProvider) { }

    #region All code mutators that are not mutating an entire assembly need to *not* modify certain references

    /// <summary>
    /// Visits the specified field reference.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    /// <returns></returns>
    public override IFieldReference Visit(IFieldReference fieldReference) {
      return fieldReference;
    }

    /// <summary>
    /// Visits a reference to the specified local definition.
    /// </summary>
    /// <param name="localDefinition">The referenced local definition to visit.</param>
    /// <returns></returns>
    public override ILocalDefinition VisitReferenceTo(ILocalDefinition localDefinition) {
      return localDefinition;
    }

    /// <summary>
    /// Visits the specified method reference.
    /// </summary>
    public override IMethodReference Visit(IMethodReference methodReference) {
      return methodReference;
    }

    /// <summary>
    /// Visits a parameter definition that is being referenced.
    /// </summary>
    /// <param name="parameterDefinition">The referenced parameter definition.</param>
    public override IParameterDefinition VisitReferenceTo(IParameterDefinition parameterDefinition) {
      return parameterDefinition;
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    public override ITypeReference Visit(ITypeReference typeReference) {
      return typeReference;
    }

    #endregion All code mutators that are not mutating an entire assembly need to *not* modify certain references
  }

  /// <summary>
  /// Uses the inherited methods from MetadataMutatingVisitor to walk everything down to the method body level,
  /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
  /// </summary>
  [Obsolete("Please use CodeRewriter")]
  public class CodeMutatingVisitor : MutatingVisitor {

    /// <summary>
    /// A helper ICodeVisitor for IExpression and IStatement dispatch.
    /// </summary>
    private CreateMutableType createMutableType;

    /// <summary>
    /// An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.
    /// </summary>
    protected readonly ISourceLocationProvider/*?*/ sourceLocationProvider;

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutatingVisitor to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public CodeMutatingVisitor(IMetadataHost host)
      : base(host) {
      createMutableType = new CreateMutableType(this);
    }

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutatingVisitor to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    public CodeMutatingVisitor(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
      : base(host) {
      this.sourceLocationProvider = sourceLocationProvider;
      createMutableType = new CreateMutableType(this);
    }

    #region Virtual methods for subtypes to override, one per type in MutableCodeModel

    /// <summary>
    /// Visits the specified addition.
    /// </summary>
    /// <param name="addition">The addition.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Addition addition) {
      return this.Visit((BinaryOperation)addition);
    }

    /// <summary>
    /// Visits the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    /// <returns></returns>
    public virtual IAddressableExpression Visit(AddressableExpression addressableExpression) {
      object def = addressableExpression.Definition;
      ILocalDefinition/*?*/ loc = def as ILocalDefinition;
      if (loc != null)
        addressableExpression.Definition = this.VisitReferenceTo(loc);
      else {
        IParameterDefinition/*?*/ par = def as IParameterDefinition;
        if (par != null)
          addressableExpression.Definition = this.VisitReferenceTo(par);
        else {
          IFieldReference/*?*/ field = def as IFieldReference;
          if (field != null)
            addressableExpression.Definition = this.Visit(field);
          else {
            IArrayIndexer/*?*/ indexer = def as IArrayIndexer;
            if (indexer != null) {
              addressableExpression.Definition = this.Visit(indexer);
              indexer = addressableExpression.Definition as IArrayIndexer;
              if (indexer != null) {
                addressableExpression.Instance = indexer.IndexedObject;
                addressableExpression.Type = indexer.Type;
                return addressableExpression;
              }
            } else {
              IAddressDereference/*?*/ adr = def as IAddressDereference;
              if (adr != null)
                addressableExpression.Definition = this.Visit(adr);
              else {
                IMethodReference/*?*/ meth = def as IMethodReference;
                if (meth != null)
                  addressableExpression.Definition = this.Visit(meth);
                else {
                  IThisReference thisRef = (IThisReference)def;
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

    /// <summary>
    /// Visits the specified address dereference.
    /// </summary>
    /// <param name="addressDereference">The address dereference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(AddressDereference addressDereference) {
      addressDereference.Address = this.Visit(addressDereference.Address);
      addressDereference.Type = this.Visit(addressDereference.Type);
      return addressDereference;
    }

    /// <summary>
    /// Visits the specified address of.
    /// </summary>
    /// <param name="addressOf">The address of.</param>
    /// <returns></returns>
    public virtual IExpression Visit(AddressOf addressOf) {
      addressOf.Expression = this.Visit(addressOf.Expression);
      addressOf.Type = this.Visit(addressOf.Type);
      return addressOf;
    }

    /// <summary>
    /// Visits the specified anonymous delegate.
    /// </summary>
    /// <param name="anonymousDelegate">The anonymous delegate.</param>
    /// <returns></returns>
    public virtual IExpression Visit(AnonymousDelegate anonymousDelegate) {
      this.path.Push(anonymousDelegate);
      for (int i = 0, n = anonymousDelegate.Parameters.Count; i < n; i++)
        anonymousDelegate.Parameters[i] = this.Visit(anonymousDelegate.Parameters[i]);
      anonymousDelegate.Body = this.Visit(anonymousDelegate.Body);
      anonymousDelegate.ReturnType = this.Visit(anonymousDelegate.ReturnType);
      anonymousDelegate.Type = this.Visit(anonymousDelegate.Type);
      this.path.Pop();
      return anonymousDelegate;
    }

    /// <summary>
    /// Visits the specified array indexer.
    /// </summary>
    /// <param name="arrayIndexer">The array indexer.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ArrayIndexer arrayIndexer) {
      arrayIndexer.IndexedObject = this.Visit(arrayIndexer.IndexedObject);
      arrayIndexer.Indices = this.Visit(arrayIndexer.Indices);
      arrayIndexer.Type = this.Visit(arrayIndexer.Type);
      return arrayIndexer;
    }

    /// <summary>
    /// Visits the specified assert statement.
    /// </summary>
    /// <param name="assertStatement">The assert statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(AssertStatement assertStatement) {
      assertStatement.Condition = this.Visit(assertStatement.Condition);
      return assertStatement;
    }

    /// <summary>
    /// Visits the specified assignment.
    /// </summary>
    /// <param name="assignment">The assignment.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Assignment assignment) {
      assignment.Target = this.Visit(assignment.Target);
      assignment.Source = this.Visit(assignment.Source);
      assignment.Type = this.Visit(assignment.Type);
      return assignment;
    }

    /// <summary>
    /// Visits the specified assume statement.
    /// </summary>
    /// <param name="assumeStatement">The assume statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(AssumeStatement assumeStatement) {
      assumeStatement.Condition = this.Visit(assumeStatement.Condition);
      return assumeStatement;
    }

    /// <summary>
    /// Visits the specified bitwise and.
    /// </summary>
    /// <param name="bitwiseAnd">The bitwise and.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BitwiseAnd bitwiseAnd) {
      return this.Visit((BinaryOperation)bitwiseAnd);
    }

    /// <summary>
    /// Visits the specified bitwise or.
    /// </summary>
    /// <param name="bitwiseOr">The bitwise or.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BitwiseOr bitwiseOr) {
      return this.Visit((BinaryOperation)bitwiseOr);
    }

    /// <summary>
    /// Visits the specified binary operation.
    /// </summary>
    /// <param name="binaryOperation">The binary operation.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BinaryOperation binaryOperation) {
      binaryOperation.LeftOperand = this.Visit(binaryOperation.LeftOperand);
      binaryOperation.RightOperand = this.Visit(binaryOperation.RightOperand);
      binaryOperation.Type = this.Visit(binaryOperation.Type);
      return binaryOperation;
    }

    /// <summary>
    /// Visits the specified block expression.
    /// </summary>
    /// <param name="blockExpression">The block expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BlockExpression blockExpression) {
      blockExpression.BlockStatement = this.Visit(blockExpression.BlockStatement);
      blockExpression.Expression = Visit(blockExpression.Expression);
      blockExpression.Type = this.Visit(blockExpression.Type);
      return blockExpression;
    }

    /// <summary>
    /// Visits the specified block statement.
    /// </summary>
    /// <param name="blockStatement">The block statement.</param>
    /// <returns></returns>
    public virtual IBlockStatement Visit(BlockStatement blockStatement) {
      blockStatement.Statements = Visit(blockStatement.Statements);
      return blockStatement;
    }

    /// <summary>
    /// Visits the specified bound expression.
    /// </summary>
    /// <param name="boundExpression">The bound expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(BoundExpression boundExpression) {
      if (boundExpression.Instance != null)
        boundExpression.Instance = Visit(boundExpression.Instance);
      ILocalDefinition/*?*/ loc = boundExpression.Definition as ILocalDefinition;
      if (loc != null)
        boundExpression.Definition = this.VisitReferenceTo(loc);
      else {
        IParameterDefinition/*?*/ par = boundExpression.Definition as IParameterDefinition;
        if (par != null)
          boundExpression.Definition = this.VisitReferenceTo(par);
        else {
          IFieldReference/*?*/ field = boundExpression.Definition as IFieldReference;
          boundExpression.Definition = this.Visit(field);
        }
      }
      boundExpression.Type = this.Visit(boundExpression.Type);
      return boundExpression;
    }

    /// <summary>
    /// Visits the specified break statement.
    /// </summary>
    /// <param name="breakStatement">The break statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(BreakStatement breakStatement) {
      return breakStatement;
    }

    /// <summary>
    /// Visits the specified cast if possible.
    /// </summary>
    /// <param name="castIfPossible">The cast if possible.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CastIfPossible castIfPossible) {
      castIfPossible.TargetType = Visit(castIfPossible.TargetType);
      castIfPossible.ValueToCast = Visit(castIfPossible.ValueToCast);
      castIfPossible.Type = this.Visit(castIfPossible.Type);
      return castIfPossible;
    }

    /// <summary>
    /// Visits the specified catch clauses.
    /// </summary>
    /// <param name="catchClauses">The catch clauses.</param>
    /// <returns></returns>
    public virtual List<ICatchClause> Visit(List<CatchClause> catchClauses) {
      List<ICatchClause> newList = new List<ICatchClause>();
      foreach (var catchClause in catchClauses) {
        newList.Add(Visit(catchClause));
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified catch clause.
    /// </summary>
    /// <param name="catchClause">The catch clause.</param>
    /// <returns></returns>
    public virtual ICatchClause Visit(CatchClause catchClause) {
      if (catchClause.FilterCondition != null)
        catchClause.FilterCondition = Visit(catchClause.FilterCondition);
      catchClause.Body = Visit(catchClause.Body);
      return catchClause;
    }

    /// <summary>
    /// Visits the specified check if instance.
    /// </summary>
    /// <param name="checkIfInstance">The check if instance.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CheckIfInstance checkIfInstance) {
      checkIfInstance.Operand = Visit(checkIfInstance.Operand);
      checkIfInstance.TypeToCheck = Visit(checkIfInstance.TypeToCheck);
      checkIfInstance.Type = this.Visit(checkIfInstance.Type);
      return checkIfInstance;
    }

    /// <summary>
    /// Visits the specified constant.
    /// </summary>
    /// <param name="constant">The constant.</param>
    /// <returns></returns>
    public virtual ICompileTimeConstant Visit(CompileTimeConstant constant) {
      constant.Type = this.Visit(constant.Type);
      return constant;
    }

    /// <summary>
    /// Visits the specified conversion.
    /// </summary>
    /// <param name="conversion">The conversion.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Conversion conversion) {
      conversion.ValueToConvert = Visit(conversion.ValueToConvert);
      conversion.TypeAfterConversion = this.Visit(conversion.TypeAfterConversion);
      return conversion;
    }

    /// <summary>
    /// Visits the specified conditional.
    /// </summary>
    /// <param name="conditional">The conditional.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Conditional conditional) {
      conditional.Condition = Visit(conditional.Condition);
      conditional.ResultIfTrue = Visit(conditional.ResultIfTrue);
      conditional.ResultIfFalse = Visit(conditional.ResultIfFalse);
      conditional.Type = this.Visit(conditional.Type);
      return conditional;
    }

    /// <summary>
    /// Visits the specified conditional statement.
    /// </summary>
    /// <param name="conditionalStatement">The conditional statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ConditionalStatement conditionalStatement) {
      conditionalStatement.Condition = Visit(conditionalStatement.Condition);
      conditionalStatement.TrueBranch = Visit(conditionalStatement.TrueBranch);
      conditionalStatement.FalseBranch = Visit(conditionalStatement.FalseBranch);
      return conditionalStatement;
    }

    /// <summary>
    /// Visits the specified continue statement.
    /// </summary>
    /// <param name="continueStatement">The continue statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ContinueStatement continueStatement) {
      return continueStatement;
    }

    /// <summary>
    /// Visits the specified copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement">The copy memory statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(CopyMemoryStatement copyMemoryStatement) {
      copyMemoryStatement.TargetAddress = Visit(copyMemoryStatement.TargetAddress);
      copyMemoryStatement.SourceAddress = Visit(copyMemoryStatement.SourceAddress);
      copyMemoryStatement.NumberOfBytesToCopy = Visit(copyMemoryStatement.NumberOfBytesToCopy);
      return copyMemoryStatement;
    }

    /// <summary>
    /// Visits the specified create array.
    /// </summary>
    /// <param name="createArray">The create array.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CreateArray createArray) {
      createArray.ElementType = this.Visit(createArray.ElementType);
      createArray.Sizes = this.Visit(createArray.Sizes);
      createArray.Initializers = this.Visit(createArray.Initializers);
      createArray.Type = this.Visit(createArray.Type);
      return createArray;
    }

    /// <summary>
    /// Visits the specified create object instance.
    /// </summary>
    /// <param name="createObjectInstance">The create object instance.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CreateObjectInstance createObjectInstance) {
      createObjectInstance.MethodToCall = this.Visit(createObjectInstance.MethodToCall);
      createObjectInstance.Arguments = Visit(createObjectInstance.Arguments);
      createObjectInstance.Type = this.Visit(createObjectInstance.Type);
      return createObjectInstance;
    }

    /// <summary>
    /// Visits the specified create delegate instance.
    /// </summary>
    /// <param name="createDelegateInstance">The create delegate instance.</param>
    /// <returns></returns>
    public virtual IExpression Visit(CreateDelegateInstance createDelegateInstance) {
      createDelegateInstance.MethodToCallViaDelegate = this.Visit(createDelegateInstance.MethodToCallViaDelegate);
      if (createDelegateInstance.Instance != null)
        createDelegateInstance.Instance = Visit(createDelegateInstance.Instance);
      createDelegateInstance.Type = this.Visit(createDelegateInstance.Type);
      return createDelegateInstance;
    }

    /// <summary>
    /// Visits the specified default value.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(DefaultValue defaultValue) {
      defaultValue.DefaultValueType = Visit(defaultValue.DefaultValueType);
      defaultValue.Type = this.Visit(defaultValue.Type);
      return defaultValue;
    }

    /// <summary>
    /// Visits the specified debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement">The debugger break statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(DebuggerBreakStatement debuggerBreakStatement) {
      return debuggerBreakStatement;
    }

    /// <summary>
    /// Visits the specified division.
    /// </summary>
    /// <param name="division">The division.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Division division) {
      return this.Visit((BinaryOperation)division);
    }

    /// <summary>
    /// Visits the specified do until statement.
    /// </summary>
    /// <param name="doUntilStatement">The do until statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(DoUntilStatement doUntilStatement) {
      doUntilStatement.Body = Visit(doUntilStatement.Body);
      doUntilStatement.Condition = Visit(doUntilStatement.Condition);
      return doUntilStatement;
    }

    /// <summary>
    /// Visits the specified DupValue.
    /// </summary>
    /// <param name="dupValue">The DupValue.</param>
    /// <returns></returns>
    public virtual IExpression Visit(DupValue dupValue) {
      return dupValue;
    }

    /// <summary>
    /// Visits the specified empty statement.
    /// </summary>
    /// <param name="emptyStatement">The empty statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(EmptyStatement emptyStatement) {
      return emptyStatement;
    }

    /// <summary>
    /// Visits the specified equality.
    /// </summary>
    /// <param name="equality">The equality.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Equality equality) {
      return this.Visit((BinaryOperation)equality);
    }

    /// <summary>
    /// Visits the specified exclusive or.
    /// </summary>
    /// <param name="exclusiveOr">The exclusive or.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ExclusiveOr exclusiveOr) {
      return this.Visit((BinaryOperation)exclusiveOr);
    }

    /// <summary>
    /// Visits the specified expressions.
    /// </summary>
    /// <param name="expressions">The expressions.</param>
    /// <returns></returns>
    public virtual List<IExpression> Visit(List<IExpression> expressions) {
      List<IExpression> newList = new List<IExpression>();
      foreach (var expression in expressions)
        newList.Add(this.Visit(expression));
      return newList;
    }

    /// <summary>
    /// Visits the specified expression statement.
    /// </summary>
    /// <param name="expressionStatement">The expression statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ExpressionStatement expressionStatement) {
      expressionStatement.Expression = Visit(expressionStatement.Expression);
      return expressionStatement;
    }

    /// <summary>
    /// Visits the specified fill memory statement.
    /// </summary>
    /// <param name="fillMemoryStatement">The fill memory statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(FillMemoryStatement fillMemoryStatement) {
      fillMemoryStatement.TargetAddress = Visit(fillMemoryStatement.TargetAddress);
      fillMemoryStatement.FillValue = Visit(fillMemoryStatement.FillValue);
      fillMemoryStatement.NumberOfBytesToFill = Visit(fillMemoryStatement.NumberOfBytesToFill);
      return fillMemoryStatement;
    }

    /// <summary>
    /// Visits the specified for each statement.
    /// </summary>
    /// <param name="forEachStatement">For each statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ForEachStatement forEachStatement) {
      forEachStatement.Collection = Visit(forEachStatement.Collection);
      forEachStatement.Body = Visit(forEachStatement.Body);
      return forEachStatement;
    }

    /// <summary>
    /// Visits the specified for statement.
    /// </summary>
    /// <param name="forStatement">For statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ForStatement forStatement) {
      forStatement.InitStatements = Visit(forStatement.InitStatements);
      forStatement.Condition = Visit(forStatement.Condition);
      forStatement.IncrementStatements = Visit(forStatement.IncrementStatements);
      forStatement.Body = Visit(forStatement.Body);
      return forStatement;
    }

    /// <summary>
    /// Visits the specified get type of typed reference.
    /// </summary>
    /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GetTypeOfTypedReference getTypeOfTypedReference) {
      getTypeOfTypedReference.TypedReference = Visit(getTypeOfTypedReference.TypedReference);
      getTypeOfTypedReference.Type = this.Visit(getTypeOfTypedReference.Type);
      return getTypeOfTypedReference;
    }

    /// <summary>
    /// Visits the specified get value of typed reference.
    /// </summary>
    /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GetValueOfTypedReference getValueOfTypedReference) {
      getValueOfTypedReference.TypedReference = Visit(getValueOfTypedReference.TypedReference);
      getValueOfTypedReference.TargetType = Visit(getValueOfTypedReference.TargetType);
      getValueOfTypedReference.Type = this.Visit(getValueOfTypedReference.Type);
      return getValueOfTypedReference;
    }

    /// <summary>
    /// Visits the specified goto statement.
    /// </summary>
    /// <param name="gotoStatement">The goto statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(GotoStatement gotoStatement) {
      return gotoStatement;
    }

    /// <summary>
    /// Visits the specified goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(GotoSwitchCaseStatement gotoSwitchCaseStatement) {
      return gotoSwitchCaseStatement;
    }

    /// <summary>
    /// Visits the specified greater than.
    /// </summary>
    /// <param name="greaterThan">The greater than.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GreaterThan greaterThan) {
      return this.Visit((BinaryOperation)greaterThan);
    }

    /// <summary>
    /// Visits the specified greater than or equal.
    /// </summary>
    /// <param name="greaterThanOrEqual">The greater than or equal.</param>
    /// <returns></returns>
    public virtual IExpression Visit(GreaterThanOrEqual greaterThanOrEqual) {
      return this.Visit((BinaryOperation)greaterThanOrEqual);
    }

    /// <summary>
    /// Visits the specified labeled statement.
    /// </summary>
    /// <param name="labeledStatement">The labeled statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(LabeledStatement labeledStatement) {
      labeledStatement.Statement = Visit(labeledStatement.Statement);
      return labeledStatement;
    }

    /// <summary>
    /// Visits the specified left shift.
    /// </summary>
    /// <param name="leftShift">The left shift.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LeftShift leftShift) {
      return this.Visit((BinaryOperation)leftShift);
    }

    /// <summary>
    /// Visits the specified less than.
    /// </summary>
    /// <param name="lessThan">The less than.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LessThan lessThan) {
      return this.Visit((BinaryOperation)lessThan);
    }

    /// <summary>
    /// Visits the specified less than or equal.
    /// </summary>
    /// <param name="lessThanOrEqual">The less than or equal.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LessThanOrEqual lessThanOrEqual) {
      return this.Visit((BinaryOperation)lessThanOrEqual);
    }

    /// <summary>
    /// Visits the specified local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(LocalDeclarationStatement localDeclarationStatement) {
      localDeclarationStatement.LocalVariable = this.Visit(localDeclarationStatement.LocalVariable);
      if (localDeclarationStatement.InitialValue != null)
        localDeclarationStatement.InitialValue = Visit(localDeclarationStatement.InitialValue);
      return localDeclarationStatement;
    }

    /// <summary>
    /// Visits the specified lock statement.
    /// </summary>
    /// <param name="lockStatement">The lock statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(LockStatement lockStatement) {
      lockStatement.Guard = Visit(lockStatement.Guard);
      lockStatement.Body = Visit(lockStatement.Body);
      return lockStatement;
    }

    /// <summary>
    /// Visits the specified logical not.
    /// </summary>
    /// <param name="logicalNot">The logical not.</param>
    /// <returns></returns>
    public virtual IExpression Visit(LogicalNot logicalNot) {
      return this.Visit((UnaryOperation)logicalNot);
    }

    /// <summary>
    /// Visits the specified make typed reference.
    /// </summary>
    /// <param name="makeTypedReference">The make typed reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(MakeTypedReference makeTypedReference) {
      makeTypedReference.Operand = Visit(makeTypedReference.Operand);
      makeTypedReference.Type = this.Visit(makeTypedReference.Type);
      return makeTypedReference;
    }

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    /// <returns></returns>
    public override IMethodBody Visit(IMethodBody methodBody) {
      var sourceMethodBody = methodBody as SourceMethodBody;
      if (sourceMethodBody != null) {
        sourceMethodBody.Block = this.Visit(sourceMethodBody.Block);
        sourceMethodBody.LocalsAreZeroed = methodBody.LocalsAreZeroed;
        sourceMethodBody.MethodDefinition = this.GetCurrentMethod();
        return sourceMethodBody;
      }
      return base.Visit(methodBody);
    }

    /// <summary>
    /// Visits the specified method call.
    /// </summary>
    /// <param name="methodCall">The method call.</param>
    /// <returns></returns>
    public virtual IExpression Visit(MethodCall methodCall) {
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall)
        methodCall.ThisArgument = this.Visit(methodCall.ThisArgument);
      methodCall.Arguments = this.Visit(methodCall.Arguments);
      methodCall.MethodToCall = this.Visit(methodCall.MethodToCall);
      methodCall.Type = this.Visit(methodCall.Type);
      return methodCall;
    }

    /// <summary>
    /// Visits the specified modulus.
    /// </summary>
    /// <param name="modulus">The modulus.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Modulus modulus) {
      return this.Visit((BinaryOperation)modulus);
    }

    /// <summary>
    /// Visits the specified multiplication.
    /// </summary>
    /// <param name="multiplication">The multiplication.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Multiplication multiplication) {
      return this.Visit((BinaryOperation)multiplication);
    }

    /// <summary>
    /// Visits the specified named argument.
    /// </summary>
    /// <param name="namedArgument">The named argument.</param>
    /// <returns></returns>
    public virtual IExpression Visit(NamedArgument namedArgument) {
      namedArgument.ArgumentValue = namedArgument.ArgumentValue;
      namedArgument.Type = this.Visit(namedArgument.Type);
      return namedArgument;
    }

    /// <summary>
    /// Visits the specified not equality.
    /// </summary>
    /// <param name="notEquality">The not equality.</param>
    /// <returns></returns>
    public virtual IExpression Visit(NotEquality notEquality) {
      return this.Visit((BinaryOperation)notEquality);
    }

    /// <summary>
    /// Visits the specified old value.
    /// </summary>
    /// <param name="oldValue">The old value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(OldValue oldValue) {
      oldValue.Expression = Visit(oldValue.Expression);
      oldValue.Type = this.Visit(oldValue.Type);
      return oldValue;
    }

    /// <summary>
    /// Visits the specified ones complement.
    /// </summary>
    /// <param name="onesComplement">The ones complement.</param>
    /// <returns></returns>
    public virtual IExpression Visit(OnesComplement onesComplement) {
      return this.Visit((UnaryOperation)onesComplement);
    }

    /// <summary>
    /// Visits the specified unary operation.
    /// </summary>
    /// <param name="unaryOperation">The unary operation.</param>
    /// <returns></returns>
    public virtual IExpression Visit(UnaryOperation unaryOperation) {
      unaryOperation.Operand = Visit(unaryOperation.Operand);
      unaryOperation.Type = this.Visit(unaryOperation.Type);
      return unaryOperation;
    }

    /// <summary>
    /// Visits the specified out argument.
    /// </summary>
    /// <param name="outArgument">The out argument.</param>
    /// <returns></returns>
    public virtual IExpression Visit(OutArgument outArgument) {
      outArgument.Expression = Visit(outArgument.Expression);
      outArgument.Type = this.Visit(outArgument.Type);
      return outArgument;
    }

    /// <summary>
    /// Visits the specified pointer call.
    /// </summary>
    /// <param name="pointerCall">The pointer call.</param>
    /// <returns></returns>
    public virtual IExpression Visit(PointerCall pointerCall) {
      pointerCall.Pointer = this.Visit(pointerCall.Pointer);
      pointerCall.Arguments = Visit(pointerCall.Arguments);
      pointerCall.Type = this.Visit(pointerCall.Type);
      return pointerCall;
    }

    /// <summary>
    /// Visits the specified PopValue.
    /// </summary>
    /// <param name="popValue">The PopValue.</param>
    /// <returns></returns>
    public virtual IExpression Visit(PopValue popValue) {
      popValue.Type = this.Visit(popValue.Type);
      return popValue;
    }

    /// <summary>
    /// Visits the specified push statement.
    /// </summary>
    /// <param name="pushStatement">The push statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(PushStatement pushStatement) {
      pushStatement.ValueToPush = Visit(pushStatement.ValueToPush);
      return pushStatement;
    }

    /// <summary>
    /// Visits the specified ref argument.
    /// </summary>
    /// <param name="refArgument">The ref argument.</param>
    /// <returns></returns>
    public virtual IExpression Visit(RefArgument refArgument) {
      refArgument.Expression = Visit(refArgument.Expression);
      refArgument.Type = this.Visit(refArgument.Type);
      return refArgument;
    }

    /// <summary>
    /// Visits the specified resource use statement.
    /// </summary>
    /// <param name="resourceUseStatement">The resource use statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ResourceUseStatement resourceUseStatement) {
      resourceUseStatement.ResourceAcquisitions = Visit(resourceUseStatement.ResourceAcquisitions);
      resourceUseStatement.Body = Visit(resourceUseStatement.Body);
      return resourceUseStatement;
    }

    /// <summary>
    /// Visits the specified rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement">The rethrow statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(RethrowStatement rethrowStatement) {
      return rethrowStatement;
    }

    /// <summary>
    /// Visits the specified return statement.
    /// </summary>
    /// <param name="returnStatement">The return statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ReturnStatement returnStatement) {
      if (returnStatement.Expression != null)
        returnStatement.Expression = Visit(returnStatement.Expression);
      return returnStatement;
    }

    /// <summary>
    /// Visits the specified return value.
    /// </summary>
    /// <param name="returnValue">The return value.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ReturnValue returnValue) {
      returnValue.Type = this.Visit(returnValue.Type);
      return returnValue;
    }

    /// <summary>
    /// Visits the specified right shift.
    /// </summary>
    /// <param name="rightShift">The right shift.</param>
    /// <returns></returns>
    public virtual IExpression Visit(RightShift rightShift) {
      return this.Visit((BinaryOperation)rightShift);
    }

    /// <summary>
    /// Visits the specified runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(RuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      runtimeArgumentHandleExpression.Type = this.Visit(runtimeArgumentHandleExpression.Type);
      return runtimeArgumentHandleExpression;
    }

    /// <summary>
    /// Visits the specified size of.
    /// </summary>
    /// <param name="sizeOf">The size of.</param>
    /// <returns></returns>
    public virtual IExpression Visit(SizeOf sizeOf) {
      sizeOf.TypeToSize = Visit(sizeOf.TypeToSize);
      sizeOf.Type = this.Visit(sizeOf.Type);
      return sizeOf;
    }

    /// <summary>
    /// Visits the specified stack array create.
    /// </summary>
    /// <param name="stackArrayCreate">The stack array create.</param>
    /// <returns></returns>
    public virtual IExpression Visit(StackArrayCreate stackArrayCreate) {
      stackArrayCreate.ElementType = Visit(stackArrayCreate.ElementType);
      stackArrayCreate.Size = Visit(stackArrayCreate.Size);
      stackArrayCreate.Type = this.Visit(stackArrayCreate.Type);
      return stackArrayCreate;
    }

    /// <summary>
    /// Visits the specified subtraction.
    /// </summary>
    /// <param name="subtraction">The subtraction.</param>
    /// <returns></returns>
    public virtual IExpression Visit(Subtraction subtraction) {
      return this.Visit((BinaryOperation)subtraction);
    }

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
    /// <returns></returns>
    public virtual List<ISwitchCase> Visit(List<SwitchCase> switchCases) {
      List<ISwitchCase> newList = new List<ISwitchCase>();
      foreach (var switchCase in switchCases)
        newList.Add(Visit(switchCase));
      return newList;
    }

    /// <summary>
    /// Visits the specified switch case.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    /// <returns></returns>
    public virtual ISwitchCase Visit(SwitchCase switchCase) {
      if (!switchCase.IsDefault)
        switchCase.Expression = Visit(switchCase.Expression);
      switchCase.Body = Visit(switchCase.Body);
      return switchCase;
    }

    /// <summary>
    /// Visits the specified switch statement.
    /// </summary>
    /// <param name="switchStatement">The switch statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(SwitchStatement switchStatement) {
      switchStatement.Expression = Visit(switchStatement.Expression);
      switchStatement.Cases = Visit(switchStatement.Cases);
      return switchStatement;
    }

    /// <summary>
    /// Visits the specified target expression.
    /// </summary>
    /// <param name="targetExpression">The target expression.</param>
    /// <returns></returns>
    public virtual ITargetExpression Visit(TargetExpression targetExpression) {
      object def = targetExpression.Definition;
      ILocalDefinition/*?*/ loc = def as ILocalDefinition;
      if (loc != null)
        targetExpression.Definition = this.VisitReferenceTo(loc);
      else {
        IParameterDefinition/*?*/ par = targetExpression.Definition as IParameterDefinition;
        if (par != null)
          targetExpression.Definition = this.VisitReferenceTo(par);
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
                  targetExpression.Definition = this.Visit(prop);
                }
              }
            }
          }
        }
      }
      targetExpression.Type = this.Visit(targetExpression.Type);
      return targetExpression;
    }

    /// <summary>
    /// Visits the specified this reference.
    /// </summary>
    /// <param name="thisReference">The this reference.</param>
    /// <returns></returns>
    public virtual IExpression Visit(ThisReference thisReference) {
      thisReference.Type = this.Visit(thisReference.Type);
      return thisReference;
    }

    /// <summary>
    /// Visits the specified throw statement.
    /// </summary>
    /// <param name="throwStatement">The throw statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(ThrowStatement throwStatement) {
      if (throwStatement.Exception != null)
        throwStatement.Exception = Visit(throwStatement.Exception);
      return throwStatement;
    }

    /// <summary>
    /// Visits the specified try catch filter finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(TryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      tryCatchFilterFinallyStatement.TryBody = Visit(tryCatchFilterFinallyStatement.TryBody);
      tryCatchFilterFinallyStatement.CatchClauses = Visit(tryCatchFilterFinallyStatement.CatchClauses);
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        tryCatchFilterFinallyStatement.FinallyBody = Visit(tryCatchFilterFinallyStatement.FinallyBody);
      if (tryCatchFilterFinallyStatement.FaultBody != null)
        tryCatchFilterFinallyStatement.FaultBody = Visit(tryCatchFilterFinallyStatement.FaultBody);
      return tryCatchFilterFinallyStatement;
    }

    /// <summary>
    /// Visits the specified token of.
    /// </summary>
    /// <param name="tokenOf">The token of.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified type of.
    /// </summary>
    /// <param name="typeOf">The type of.</param>
    /// <returns></returns>
    public virtual IExpression Visit(TypeOf typeOf) {
      typeOf.TypeToGet = Visit(typeOf.TypeToGet);
      typeOf.Type = this.Visit(typeOf.Type);
      return typeOf;
    }

    /// <summary>
    /// Visits the specified unary negation.
    /// </summary>
    /// <param name="unaryNegation">The unary negation.</param>
    /// <returns></returns>
    public virtual IExpression Visit(UnaryNegation unaryNegation) {
      return this.Visit((UnaryOperation)unaryNegation);
    }

    /// <summary>
    /// Visits the specified unary plus.
    /// </summary>
    /// <param name="unaryPlus">The unary plus.</param>
    /// <returns></returns>
    public virtual IExpression Visit(UnaryPlus unaryPlus) {
      return this.Visit((UnaryOperation)unaryPlus);
    }

    /// <summary>
    /// Visits the specified vector length.
    /// </summary>
    /// <param name="vectorLength">Length of the vector.</param>
    /// <returns></returns>
    public virtual IExpression Visit(VectorLength vectorLength) {
      vectorLength.Vector = Visit(vectorLength.Vector);
      vectorLength.Type = this.Visit(vectorLength.Type);
      return vectorLength;
    }

    /// <summary>
    /// Visits the specified while do statement.
    /// </summary>
    /// <param name="whileDoStatement">The while do statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(WhileDoStatement whileDoStatement) {
      whileDoStatement.Condition = Visit(whileDoStatement.Condition);
      whileDoStatement.Body = Visit(whileDoStatement.Body);
      return whileDoStatement;
    }

    /// <summary>
    /// Visits the specified yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement">The yield break statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(YieldBreakStatement yieldBreakStatement) {
      return yieldBreakStatement;
    }

    /// <summary>
    /// Visits the specified yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement">The yield return statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(YieldReturnStatement yieldReturnStatement) {
      yieldReturnStatement.Expression = Visit(yieldReturnStatement.Expression);
      return yieldReturnStatement;
    }

    #endregion Virtual methods for subtypes to override, one per type in MutableCodeModel

    #region Methods that take an immutable type and return a type-specific mutable object, either by using the internal visitor, or else directly

    /// <summary>
    /// Visits the specified addressable expression.
    /// </summary>
    /// <param name="addressableExpression">The addressable expression.</param>
    /// <returns></returns>
    public virtual IAddressableExpression Visit(IAddressableExpression addressableExpression) {
      AddressableExpression mutableAddressableExpression = addressableExpression as AddressableExpression;
      if (mutableAddressableExpression == null)
        return addressableExpression;
      return Visit(mutableAddressableExpression);
    }

    /// <summary>
    /// Visits the specified block statement.
    /// </summary>
    /// <param name="blockStatement">The block statement.</param>
    /// <returns></returns>
    public virtual IBlockStatement Visit(IBlockStatement blockStatement) {
      BlockStatement mutableBlockStatement = blockStatement as BlockStatement;
      if (mutableBlockStatement == null)
        return blockStatement;
      return Visit(mutableBlockStatement);
    }

    /// <summary>
    /// Visits the specified catch clause.
    /// </summary>
    /// <param name="catchClause">The catch clause.</param>
    /// <returns></returns>
    public virtual ICatchClause Visit(ICatchClause catchClause) {
      CatchClause mutableCatchClause = catchClause as CatchClause;
      if (mutableCatchClause == null)
        return catchClause;
      return Visit(mutableCatchClause);
    }

    /// <summary>
    /// Visits the specified catch clauses.
    /// </summary>
    /// <param name="catchClauses">The catch clauses.</param>
    /// <returns></returns>
    public virtual List<ICatchClause> Visit(List<ICatchClause> catchClauses) {
      List<ICatchClause> newList = new List<ICatchClause>();
      foreach (var catchClause in catchClauses) {
        ICatchClause mcc = this.Visit(catchClause);
        newList.Add(mcc);
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified compile time constant.
    /// </summary>
    /// <param name="compileTimeConstant">The compile time constant.</param>
    /// <returns></returns>
    public virtual ICompileTimeConstant Visit(ICompileTimeConstant compileTimeConstant) {
      CompileTimeConstant mutableCompileTimeConstant = compileTimeConstant as CompileTimeConstant;
      if (mutableCompileTimeConstant == null)
        return compileTimeConstant;
      return this.Visit(mutableCompileTimeConstant);
    }

    /// <summary>
    /// Visits the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns></returns>
    public virtual IExpression Visit(IExpression expression) {
      expression.Dispatch(this.createMutableType);
      return this.createMutableType.resultExpression;
    }

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <returns></returns>
    public virtual IStatement Visit(IStatement statement) {
      statement.Dispatch(this.createMutableType);
      return this.createMutableType.resultStatement;
    }

    /// <summary>
    /// Visits the specified statements.
    /// </summary>
    /// <param name="statements">The statements.</param>
    /// <returns></returns>
    public virtual List<IStatement> Visit(List<IStatement> statements) {
      List<IStatement> newList = new List<IStatement>();
      foreach (var statement in statements) {
        IStatement newStatement = this.Visit(statement);
        if (newStatement != CodeDummy.Block)
          newList.Add(newStatement);
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified switch case.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    /// <returns></returns>
    public virtual ISwitchCase Visit(ISwitchCase switchCase) {
      SwitchCase mutableSwitchCase = switchCase as SwitchCase;
      if (mutableSwitchCase == null)
        return switchCase;
      return Visit(mutableSwitchCase);
    }

    /// <summary>
    /// Visits the specified switch cases.
    /// </summary>
    /// <param name="switchCases">The switch cases.</param>
    /// <returns></returns>
    public virtual List<ISwitchCase> Visit(List<ISwitchCase> switchCases) {
      List<ISwitchCase> newList = new List<ISwitchCase>();
      foreach (var switchCase in switchCases) {
        ISwitchCase swc = this.Visit(switchCase);
        if (swc != CodeDummy.SwitchCase)
          newList.Add(swc);
      }
      return newList;
    }

    /// <summary>
    /// Visits the specified target expression.
    /// </summary>
    /// <param name="targetExpression">The target expression.</param>
    /// <returns></returns>
    public virtual ITargetExpression Visit(ITargetExpression targetExpression) {
      TargetExpression mutableTargetExpression = targetExpression as TargetExpression;
      if (mutableTargetExpression == null)
        return targetExpression;
      return Visit(mutableTargetExpression);
    }

    #endregion Methods that take an immutable type and return a type-specific mutable object, either by using the internal visitor, or else directly

    /// <summary>
    /// This type implements the ICodeVisitor interface so that an IExpression or an IStatement
    /// can dispatch the visits automatically. The result of a visit is either an IExpression or
    /// an IStatement stored in the resultExpression or resultStatement fields. 
    /// </summary>
#pragma warning disable 618
    private class CreateMutableType : BaseCodeVisitor, ICodeVisitor {
#pragma warning restore 618

      /// <summary>
      /// Code mutator to be called back. 
      /// </summary>
      internal CodeMutatingVisitor myCodeMutator;

      /// <summary>
      /// Results of visits.
      /// </summary>
      internal IExpression resultExpression = CodeDummy.Expression;
      internal IStatement resultStatement = CodeDummy.Block;

      /// <summary>
      /// This type implements the ICodeVisitor interface so that an IExpression or an IStatement
      /// can dispatch the visits automatically. 
      /// </summary>
      internal CreateMutableType(CodeMutatingVisitor codeMutator) {
        this.myCodeMutator = codeMutator;
      }

      #region overriding implementations of ICodeVisitor Members

      /// <summary>
      /// Visits the specified addition.
      /// </summary>
      /// <param name="addition">The addition.</param>
      public override void Visit(IAddition addition) {
        Addition/*?*/ mutableAddition = addition as Addition;
        if (mutableAddition != null)
          this.resultExpression = this.myCodeMutator.Visit(mutableAddition);
        else
          this.resultExpression = addition;
      }

      /// <summary>
      /// Visits the specified addressable expression.
      /// </summary>
      /// <param name="addressableExpression">The addressable expression.</param>
      public override void Visit(IAddressableExpression addressableExpression) {
        AddressableExpression mutableAddressableExpression = addressableExpression as AddressableExpression;
        if (mutableAddressableExpression == null) {
          this.resultExpression = addressableExpression;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressableExpression);
      }

      /// <summary>
      /// Visits the specified address dereference.
      /// </summary>
      /// <param name="addressDereference">The address dereference.</param>
      public override void Visit(IAddressDereference addressDereference) {
        AddressDereference mutableAddressDereference = addressDereference as AddressDereference;
        if (mutableAddressDereference == null) {
          this.resultExpression = addressDereference;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressDereference);
      }

      /// <summary>
      /// Visits the specified address of.
      /// </summary>
      /// <param name="addressOf">The address of.</param>
      public override void Visit(IAddressOf addressOf) {
        AddressOf mutableAddressOf = addressOf as AddressOf;
        if (mutableAddressOf == null) {
          this.resultExpression = addressOf;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableAddressOf);
      }

      /// <summary>
      /// Visits the specified anonymous method.
      /// </summary>
      /// <param name="anonymousMethod">The anonymous method.</param>
      public override void Visit(IAnonymousDelegate anonymousMethod) {
        AnonymousDelegate mutableAnonymousDelegate = anonymousMethod as AnonymousDelegate;
        if (mutableAnonymousDelegate == null) {
          this.resultExpression = anonymousMethod;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableAnonymousDelegate);
      }

      /// <summary>
      /// Visits the specified array indexer.
      /// </summary>
      /// <param name="arrayIndexer">The array indexer.</param>
      public override void Visit(IArrayIndexer arrayIndexer) {
        ArrayIndexer mutableArrayIndexer = arrayIndexer as ArrayIndexer;
        if (mutableArrayIndexer == null) return;
        this.resultExpression = this.myCodeMutator.Visit(mutableArrayIndexer);
      }

      /// <summary>
      /// Visits the specified assert statement.
      /// </summary>
      /// <param name="assertStatement">The assert statement.</param>
      public override void Visit(IAssertStatement assertStatement) {
        AssertStatement mutableAssertStatement = assertStatement as AssertStatement;
        if (mutableAssertStatement == null) {
          this.resultStatement = assertStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableAssertStatement);
      }

      /// <summary>
      /// Visits the specified assignment.
      /// </summary>
      /// <param name="assignment">The assignment.</param>
      public override void Visit(IAssignment assignment) {
        Assignment mutableAssignment = assignment as Assignment;
        if (mutableAssignment == null) {
          this.resultExpression = assignment;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableAssignment);
      }

      /// <summary>
      /// Visits the specified assume statement.
      /// </summary>
      /// <param name="assumeStatement">The assume statement.</param>
      public override void Visit(IAssumeStatement assumeStatement) {
        AssumeStatement mutableAssumeStatement = assumeStatement as AssumeStatement;
        if (mutableAssumeStatement == null) {
          this.resultStatement = assumeStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableAssumeStatement);
      }

      /// <summary>
      /// Visits the specified bitwise and.
      /// </summary>
      /// <param name="bitwiseAnd">The bitwise and.</param>
      public override void Visit(IBitwiseAnd bitwiseAnd) {
        BitwiseAnd mutableBitwiseAnd = bitwiseAnd as BitwiseAnd;
        if (mutableBitwiseAnd == null) {
          this.resultExpression = bitwiseAnd;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableBitwiseAnd);
      }

      /// <summary>
      /// Visits the specified bitwise or.
      /// </summary>
      /// <param name="bitwiseOr">The bitwise or.</param>
      public override void Visit(IBitwiseOr bitwiseOr) {
        BitwiseOr mutableBitwiseOr = bitwiseOr as BitwiseOr;
        if (mutableBitwiseOr == null) {
          this.resultExpression = bitwiseOr;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableBitwiseOr);
      }

      /// <summary>
      /// Visits the specified block expression.
      /// </summary>
      /// <param name="blockExpression">The block expression.</param>
      public override void Visit(IBlockExpression blockExpression) {
        BlockExpression mutableBlockExpression = blockExpression as BlockExpression;
        if (mutableBlockExpression == null) {
          this.resultExpression = blockExpression;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableBlockExpression);
      }

      /// <summary>
      /// Visits the specified block.
      /// </summary>
      /// <param name="block">The block.</param>
      public override void Visit(IBlockStatement block) {
        BlockStatement mutableBlockStatement = block as BlockStatement;
        if (mutableBlockStatement == null) {
          this.resultStatement = block;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableBlockStatement);
      }

      /// <summary>
      /// Visits the specified break statement.
      /// </summary>
      /// <param name="breakStatement">The break statement.</param>
      public override void Visit(IBreakStatement breakStatement) {
        BreakStatement mutableBreakStatement = breakStatement as BreakStatement;
        if (mutableBreakStatement == null) {
          this.resultStatement = breakStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableBreakStatement);
      }

      /// <summary>
      /// Visits the specified bound expression.
      /// </summary>
      /// <param name="boundExpression">The bound expression.</param>
      public override void Visit(IBoundExpression boundExpression) {
        BoundExpression mutableBoundExpression = boundExpression as BoundExpression;
        if (mutableBoundExpression == null) {
          this.resultExpression = boundExpression;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableBoundExpression);
      }

      /// <summary>
      /// Visits the specified cast if possible.
      /// </summary>
      /// <param name="castIfPossible">The cast if possible.</param>
      public override void Visit(ICastIfPossible castIfPossible) {
        CastIfPossible mutableCastIfPossible = castIfPossible as CastIfPossible;
        if (mutableCastIfPossible == null) {
          this.resultExpression = castIfPossible;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableCastIfPossible);
      }

      /// <summary>
      /// Visits the specified check if instance.
      /// </summary>
      /// <param name="checkIfInstance">The check if instance.</param>
      public override void Visit(ICheckIfInstance checkIfInstance) {
        CheckIfInstance mutableCheckIfInstance = checkIfInstance as CheckIfInstance;
        if (mutableCheckIfInstance == null) {
          this.resultExpression = checkIfInstance;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableCheckIfInstance);
      }

      /// <summary>
      /// Visits the specified constant.
      /// </summary>
      /// <param name="constant">The constant.</param>
      public override void Visit(ICompileTimeConstant constant) {
        CompileTimeConstant mutableCompileTimeConstant = constant as CompileTimeConstant;
        if (mutableCompileTimeConstant == null) {
          this.resultExpression = constant;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableCompileTimeConstant);
      }

      /// <summary>
      /// Visits the specified conversion.
      /// </summary>
      /// <param name="conversion">The conversion.</param>
      public override void Visit(IConversion conversion) {
        Conversion mutableConversion = conversion as Conversion;
        if (mutableConversion == null) {
          this.resultExpression = conversion;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableConversion);
      }

      /// <summary>
      /// Visits the specified conditional.
      /// </summary>
      /// <param name="conditional">The conditional.</param>
      public override void Visit(IConditional conditional) {
        Conditional mutableConditional = conditional as Conditional;
        if (mutableConditional == null) {
          this.resultExpression = conditional;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableConditional);
      }

      /// <summary>
      /// Visits the specified conditional statement.
      /// </summary>
      /// <param name="conditionalStatement">The conditional statement.</param>
      public override void Visit(IConditionalStatement conditionalStatement) {
        ConditionalStatement mutableConditionalStatement = conditionalStatement as ConditionalStatement;
        if (mutableConditionalStatement == null) {
          this.resultStatement = conditionalStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableConditionalStatement);
      }

      /// <summary>
      /// Visits the specified continue statement.
      /// </summary>
      /// <param name="continueStatement">The continue statement.</param>
      public override void Visit(IContinueStatement continueStatement) {
        ContinueStatement mutableContinueStatement = continueStatement as ContinueStatement;
        if (mutableContinueStatement == null) {
          this.resultStatement = continueStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableContinueStatement);
      }

      /// <summary>
      /// Performs some computation with the given copy memory statement.
      /// </summary>
      /// <param name="copyMemoryStatement"></param>
      public override void Visit(ICopyMemoryStatement copyMemoryStatement) {
        CopyMemoryStatement mutableCopyMemoryStatement = copyMemoryStatement as CopyMemoryStatement;
        if (mutableCopyMemoryStatement == null) {
          this.resultStatement = copyMemoryStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableCopyMemoryStatement);
      }

      /// <summary>
      /// Visits the specified create array.
      /// </summary>
      /// <param name="createArray">The create array.</param>
      public override void Visit(ICreateArray createArray) {
        CreateArray mutableCreateArray = createArray as CreateArray;
        if (mutableCreateArray == null) {
          this.resultExpression = createArray;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateArray);
      }

      /// <summary>
      /// Visits the specified create delegate instance.
      /// </summary>
      /// <param name="createDelegateInstance">The create delegate instance.</param>
      public override void Visit(ICreateDelegateInstance createDelegateInstance) {
        CreateDelegateInstance mutableCreateDelegateInstance = createDelegateInstance as CreateDelegateInstance;
        if (mutableCreateDelegateInstance == null) {
          this.resultExpression = createDelegateInstance;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateDelegateInstance);
      }

      /// <summary>
      /// Visits the specified create object instance.
      /// </summary>
      /// <param name="createObjectInstance">The create object instance.</param>
      public override void Visit(ICreateObjectInstance createObjectInstance) {
        CreateObjectInstance mutableCreateObjectInstance = createObjectInstance as CreateObjectInstance;
        if (mutableCreateObjectInstance == null) {
          this.resultExpression = createObjectInstance;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableCreateObjectInstance);
      }

      /// <summary>
      /// Visits the specified debugger break statement.
      /// </summary>
      /// <param name="debuggerBreakStatement">The debugger break statement.</param>
      public override void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        DebuggerBreakStatement mutableDebuggerBreakStatement = debuggerBreakStatement as DebuggerBreakStatement;
        if (mutableDebuggerBreakStatement == null) {
          this.resultStatement = debuggerBreakStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableDebuggerBreakStatement);
      }

      /// <summary>
      /// Visits the specified default value.
      /// </summary>
      /// <param name="defaultValue">The default value.</param>
      public override void Visit(IDefaultValue defaultValue) {
        DefaultValue mutableDefaultValue = defaultValue as DefaultValue;
        if (mutableDefaultValue == null) {
          this.resultExpression = defaultValue;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableDefaultValue);
      }

      /// <summary>
      /// Visits the specified division.
      /// </summary>
      /// <param name="division">The division.</param>
      public override void Visit(IDivision division) {
        Division mutableDivision = division as Division;
        if (mutableDivision == null) {
          this.resultExpression = division;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableDivision);
      }

      /// <summary>
      /// Visits the specified do until statement.
      /// </summary>
      /// <param name="doUntilStatement">The do until statement.</param>
      public override void Visit(IDoUntilStatement doUntilStatement) {
        DoUntilStatement mutableDoUntilStatement = doUntilStatement as DoUntilStatement;
        if (mutableDoUntilStatement == null) {
          this.resultStatement = doUntilStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableDoUntilStatement);
      }

      /// <summary>
      /// Visits the specified DupValue.
      /// </summary>
      /// <param name="dupValue">The DupValue.</param>
      public override void Visit(IDupValue dupValue) {
        DupValue mutableDupValue = dupValue as DupValue;
        if (mutableDupValue == null) {
          this.resultExpression = dupValue;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableDupValue);
      }

      /// <summary>
      /// Visits the specified empty statement.
      /// </summary>
      /// <param name="emptyStatement">The empty statement.</param>
      public override void Visit(IEmptyStatement emptyStatement) {
        EmptyStatement mutableEmptyStatement = emptyStatement as EmptyStatement;
        if (mutableEmptyStatement == null) {
          this.resultStatement = emptyStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableEmptyStatement);
      }

      /// <summary>
      /// Visits the specified equality.
      /// </summary>
      /// <param name="equality">The equality.</param>
      public override void Visit(IEquality equality) {
        Equality mutableEquality = equality as Equality;
        if (mutableEquality == null) {
          this.resultExpression = equality;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableEquality);
      }

      /// <summary>
      /// Visits the specified exclusive or.
      /// </summary>
      /// <param name="exclusiveOr">The exclusive or.</param>
      public override void Visit(IExclusiveOr exclusiveOr) {
        ExclusiveOr mutableExclusiveOr = exclusiveOr as ExclusiveOr;
        if (mutableExclusiveOr == null) {
          this.resultExpression = exclusiveOr;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableExclusiveOr);
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
        ExpressionStatement mutableExpressionStatement = expressionStatement as ExpressionStatement;
        if (mutableExpressionStatement == null) {
          this.resultStatement = expressionStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableExpressionStatement);
      }

      /// <summary>
      /// Performs some computation with the given fill memory statement.
      /// </summary>
      /// <param name="fillMemoryStatement"></param>
      public override void Visit(IFillMemoryStatement fillMemoryStatement) {
        FillMemoryStatement mutableFillMemoryStatement = fillMemoryStatement as FillMemoryStatement;
        if (mutableFillMemoryStatement == null) {
          this.resultStatement = fillMemoryStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableFillMemoryStatement);
      }

      /// <summary>
      /// Visits the specified for each statement.
      /// </summary>
      /// <param name="forEachStatement">For each statement.</param>
      public override void Visit(IForEachStatement forEachStatement) {
        ForEachStatement mutableForEachStatement = forEachStatement as ForEachStatement;
        if (mutableForEachStatement == null) {
          this.resultStatement = forEachStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableForEachStatement);
      }

      /// <summary>
      /// Visits the specified for statement.
      /// </summary>
      /// <param name="forStatement">For statement.</param>
      public override void Visit(IForStatement forStatement) {
        ForStatement mutableForStatement = forStatement as ForStatement;
        if (mutableForStatement == null) {
          this.resultStatement = forStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableForStatement);
      }

      /// <summary>
      /// Visits the specified goto statement.
      /// </summary>
      /// <param name="gotoStatement">The goto statement.</param>
      public override void Visit(IGotoStatement gotoStatement) {
        GotoStatement mutableGotoStatement = gotoStatement as GotoStatement;
        if (mutableGotoStatement == null) {
          this.resultStatement = gotoStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableGotoStatement);
      }

      /// <summary>
      /// Visits the specified goto switch case statement.
      /// </summary>
      /// <param name="gotoSwitchCaseStatement">The goto switch case statement.</param>
      public override void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        GotoSwitchCaseStatement mutableGotoSwitchCaseStatement = gotoSwitchCaseStatement as GotoSwitchCaseStatement;
        if (mutableGotoSwitchCaseStatement == null) {
          this.resultStatement = gotoSwitchCaseStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableGotoSwitchCaseStatement);
      }

      /// <summary>
      /// Visits the specified get type of typed reference.
      /// </summary>
      /// <param name="getTypeOfTypedReference">The get type of typed reference.</param>
      public override void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        GetTypeOfTypedReference mutableGetTypeOfTypedReference = getTypeOfTypedReference as GetTypeOfTypedReference;
        if (mutableGetTypeOfTypedReference == null) {
          this.resultExpression = getTypeOfTypedReference;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableGetTypeOfTypedReference);
      }

      /// <summary>
      /// Visits the specified get value of typed reference.
      /// </summary>
      /// <param name="getValueOfTypedReference">The get value of typed reference.</param>
      public override void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        GetValueOfTypedReference mutableGetValueOfTypedReference = getValueOfTypedReference as GetValueOfTypedReference;
        if (mutableGetValueOfTypedReference == null) {
          this.resultExpression = getValueOfTypedReference;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableGetValueOfTypedReference);
      }

      /// <summary>
      /// Visits the specified greater than.
      /// </summary>
      /// <param name="greaterThan">The greater than.</param>
      public override void Visit(IGreaterThan greaterThan) {
        GreaterThan mutableGreaterThan = greaterThan as GreaterThan;
        if (mutableGreaterThan == null) {
          this.resultExpression = greaterThan;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableGreaterThan);
      }

      /// <summary>
      /// Visits the specified greater than or equal.
      /// </summary>
      /// <param name="greaterThanOrEqual">The greater than or equal.</param>
      public override void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        GreaterThanOrEqual mutableGreaterThanOrEqual = greaterThanOrEqual as GreaterThanOrEqual;
        if (mutableGreaterThanOrEqual == null) {
          this.resultExpression = greaterThanOrEqual;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableGreaterThanOrEqual);
      }

      /// <summary>
      /// Visits the specified labeled statement.
      /// </summary>
      /// <param name="labeledStatement">The labeled statement.</param>
      public override void Visit(ILabeledStatement labeledStatement) {
        LabeledStatement mutableLabeledStatement = labeledStatement as LabeledStatement;
        if (mutableLabeledStatement == null) {
          this.resultStatement = labeledStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableLabeledStatement);
      }

      /// <summary>
      /// Visits the specified left shift.
      /// </summary>
      /// <param name="leftShift">The left shift.</param>
      public override void Visit(ILeftShift leftShift) {
        LeftShift mutableLeftShift = leftShift as LeftShift;
        if (mutableLeftShift == null) {
          this.resultExpression = leftShift;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableLeftShift);
      }

      /// <summary>
      /// Visits the specified less than.
      /// </summary>
      /// <param name="lessThan">The less than.</param>
      public override void Visit(ILessThan lessThan) {
        LessThan mutableLessThan = lessThan as LessThan;
        if (mutableLessThan == null) {
          this.resultExpression = lessThan;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableLessThan);
      }

      /// <summary>
      /// Visits the specified less than or equal.
      /// </summary>
      /// <param name="lessThanOrEqual">The less than or equal.</param>
      public override void Visit(ILessThanOrEqual lessThanOrEqual) {
        LessThanOrEqual mutableLessThanOrEqual = lessThanOrEqual as LessThanOrEqual;
        if (mutableLessThanOrEqual == null) {
          this.resultExpression = lessThanOrEqual;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableLessThanOrEqual);
      }

      /// <summary>
      /// Visits the specified local declaration statement.
      /// </summary>
      /// <param name="localDeclarationStatement">The local declaration statement.</param>
      public override void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        LocalDeclarationStatement mutableLocalDeclarationStatement = localDeclarationStatement as LocalDeclarationStatement;
        if (mutableLocalDeclarationStatement == null) {
          this.resultStatement = localDeclarationStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableLocalDeclarationStatement);
      }

      /// <summary>
      /// Visits the specified lock statement.
      /// </summary>
      /// <param name="lockStatement">The lock statement.</param>
      public override void Visit(ILockStatement lockStatement) {
        LockStatement mutableLockStatement = lockStatement as LockStatement;
        if (mutableLockStatement == null) {
          this.resultStatement = lockStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableLockStatement);
      }

      /// <summary>
      /// Visits the specified logical not.
      /// </summary>
      /// <param name="logicalNot">The logical not.</param>
      public override void Visit(ILogicalNot logicalNot) {
        LogicalNot mutableLogicalNot = logicalNot as LogicalNot;
        if (mutableLogicalNot == null) {
          this.resultExpression = logicalNot;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableLogicalNot);
      }

      /// <summary>
      /// Visits the specified make typed reference.
      /// </summary>
      /// <param name="makeTypedReference">The make typed reference.</param>
      public override void Visit(IMakeTypedReference makeTypedReference) {
        MakeTypedReference mutableMakeTypedReference = makeTypedReference as MakeTypedReference;
        if (mutableMakeTypedReference == null) {
          this.resultExpression = makeTypedReference;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableMakeTypedReference);
      }

      /// <summary>
      /// Visits the specified method call.
      /// </summary>
      /// <param name="methodCall">The method call.</param>
      public override void Visit(IMethodCall methodCall) {
        MethodCall mutableMethodCall = methodCall as MethodCall;
        if (mutableMethodCall == null) {
          this.resultExpression = methodCall; return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableMethodCall);
      }

      /// <summary>
      /// Visits the specified modulus.
      /// </summary>
      /// <param name="modulus">The modulus.</param>
      public override void Visit(IModulus modulus) {
        Modulus mutableModulus = modulus as Modulus;
        if (mutableModulus == null) { this.resultExpression = modulus; return; }
        this.resultExpression = this.myCodeMutator.Visit(mutableModulus);
      }

      /// <summary>
      /// Visits the specified multiplication.
      /// </summary>
      /// <param name="multiplication">The multiplication.</param>
      public override void Visit(IMultiplication multiplication) {
        Multiplication mutableMultiplication = multiplication as Multiplication;
        if (mutableMultiplication == null) {
          this.resultExpression = multiplication;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableMultiplication);
      }

      /// <summary>
      /// Visits the specified named argument.
      /// </summary>
      /// <param name="namedArgument">The named argument.</param>
      public override void Visit(INamedArgument namedArgument) {
        NamedArgument mutableNamedArgument = namedArgument as NamedArgument;
        if (mutableNamedArgument == null) {
          this.resultExpression = namedArgument;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableNamedArgument);
      }

      /// <summary>
      /// Visits the specified not equality.
      /// </summary>
      /// <param name="notEquality">The not equality.</param>
      public override void Visit(INotEquality notEquality) {
        NotEquality mutableNotEquality = notEquality as NotEquality;
        if (mutableNotEquality == null) {
          this.resultExpression = notEquality;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableNotEquality);
      }

      /// <summary>
      /// Visits the specified old value.
      /// </summary>
      /// <param name="oldValue">The old value.</param>
      public override void Visit(IOldValue oldValue) {
        OldValue mutableOldValue = oldValue as OldValue;
        if (mutableOldValue == null) {
          this.resultExpression = oldValue;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableOldValue);
      }

      /// <summary>
      /// Visits the specified ones complement.
      /// </summary>
      /// <param name="onesComplement">The ones complement.</param>
      public override void Visit(IOnesComplement onesComplement) {
        OnesComplement mutableOnesComplement = onesComplement as OnesComplement;
        if (mutableOnesComplement == null) {
          this.resultExpression = onesComplement;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableOnesComplement);
      }

      /// <summary>
      /// Visits the specified out argument.
      /// </summary>
      /// <param name="outArgument">The out argument.</param>
      public override void Visit(IOutArgument outArgument) {
        OutArgument mutableOutArgument = outArgument as OutArgument;
        if (mutableOutArgument == null) mutableOutArgument = new OutArgument(outArgument);
        this.resultExpression = this.myCodeMutator.Visit(mutableOutArgument);
      }

      /// <summary>
      /// Visits the specified pointer call.
      /// </summary>
      /// <param name="pointerCall">The pointer call.</param>
      public override void Visit(IPointerCall pointerCall) {
        PointerCall mutablePointerCall = pointerCall as PointerCall;
        if (mutablePointerCall == null) mutablePointerCall = new PointerCall(pointerCall);
        this.resultExpression = this.myCodeMutator.Visit(mutablePointerCall);
      }

      /// <summary>
      /// Visits the specified PopValue.
      /// </summary>
      /// <param name="popValue">The PopValue.</param>
      public override void Visit(IPopValue popValue) {
        PopValue mutablePopValue = popValue as PopValue;
        if (mutablePopValue == null) {
          this.resultExpression = popValue;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutablePopValue);
      }

      /// <summary>
      /// Visits the specified push statement.
      /// </summary>
      /// <param name="pushStatement">The push statement.</param>
      public override void Visit(IPushStatement pushStatement) {
        PushStatement mutablePushStatement = pushStatement as PushStatement;
        if (mutablePushStatement == null) {
          this.resultStatement = pushStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutablePushStatement);
      }

      /// <summary>
      /// Visits the specified ref argument.
      /// </summary>
      /// <param name="refArgument">The ref argument.</param>
      public override void Visit(IRefArgument refArgument) {
        RefArgument mutableRefArgument = refArgument as RefArgument;
        if (mutableRefArgument == null) {
          this.resultExpression = refArgument;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableRefArgument);
      }

      /// <summary>
      /// Visits the specified resource use statement.
      /// </summary>
      /// <param name="resourceUseStatement">The resource use statement.</param>
      public override void Visit(IResourceUseStatement resourceUseStatement) {
        ResourceUseStatement mutableResourceUseStatement = resourceUseStatement as ResourceUseStatement;
        if (mutableResourceUseStatement == null) {
          this.resultStatement = resourceUseStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableResourceUseStatement);
      }

      /// <summary>
      /// Visits the specified return value.
      /// </summary>
      /// <param name="returnValue">The return value.</param>
      public override void Visit(IReturnValue returnValue) {
        ReturnValue mutableReturnValue = returnValue as ReturnValue;
        if (mutableReturnValue == null) {
          this.resultExpression = returnValue;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableReturnValue);
      }

      /// <summary>
      /// Visits the specified rethrow statement.
      /// </summary>
      /// <param name="rethrowStatement">The rethrow statement.</param>
      public override void Visit(IRethrowStatement rethrowStatement) {
        RethrowStatement mutableRethrowStatement = rethrowStatement as RethrowStatement;
        if (mutableRethrowStatement == null) {
          this.resultStatement = rethrowStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableRethrowStatement);
      }

      /// <summary>
      /// Visits the specified return statement.
      /// </summary>
      /// <param name="returnStatement">The return statement.</param>
      public override void Visit(IReturnStatement returnStatement) {
        ReturnStatement mutableReturnStatement = returnStatement as ReturnStatement;
        if (mutableReturnStatement == null) {
          this.resultStatement = returnStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableReturnStatement);
      }

      /// <summary>
      /// Visits the specified right shift.
      /// </summary>
      /// <param name="rightShift">The right shift.</param>
      public override void Visit(IRightShift rightShift) {
        RightShift mutableRightShift = rightShift as RightShift;
        if (mutableRightShift == null) {
          this.resultExpression = rightShift;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableRightShift);
      }

      /// <summary>
      /// Visits the specified runtime argument handle expression.
      /// </summary>
      /// <param name="runtimeArgumentHandleExpression">The runtime argument handle expression.</param>
      public override void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        RuntimeArgumentHandleExpression mutableRuntimeArgumentHandleExpression = runtimeArgumentHandleExpression as RuntimeArgumentHandleExpression;
        if (mutableRuntimeArgumentHandleExpression == null) {
          this.resultExpression = runtimeArgumentHandleExpression;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableRuntimeArgumentHandleExpression);
      }

      /// <summary>
      /// Visits the specified size of.
      /// </summary>
      /// <param name="sizeOf">The size of.</param>
      public override void Visit(ISizeOf sizeOf) {
        SizeOf mutableSizeOf = sizeOf as SizeOf;
        if (mutableSizeOf == null) {
          this.resultExpression = sizeOf;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableSizeOf);
      }

      /// <summary>
      /// Visits the specified stack array create.
      /// </summary>
      /// <param name="stackArrayCreate">The stack array create.</param>
      public override void Visit(IStackArrayCreate stackArrayCreate) {
        StackArrayCreate mutableStackArrayCreate = stackArrayCreate as StackArrayCreate;
        if (mutableStackArrayCreate == null) {
          this.resultExpression = stackArrayCreate;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableStackArrayCreate);
      }

      /// <summary>
      /// Visits the specified subtraction.
      /// </summary>
      /// <param name="subtraction">The subtraction.</param>
      public override void Visit(ISubtraction subtraction) {
        Subtraction mutableSubtraction = subtraction as Subtraction;
        if (mutableSubtraction == null) {
          this.resultExpression = subtraction;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableSubtraction);
      }

      /// <summary>
      /// Visits the specified switch statement.
      /// </summary>
      /// <param name="switchStatement">The switch statement.</param>
      public override void Visit(ISwitchStatement switchStatement) {
        SwitchStatement mutableSwitchStatement = switchStatement as SwitchStatement;
        if (mutableSwitchStatement == null) {
          this.resultStatement = switchStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableSwitchStatement);
      }

      /// <summary>
      /// Visits the specified target expression.
      /// </summary>
      /// <param name="targetExpression">The target expression.</param>
      public override void Visit(ITargetExpression targetExpression) {
        TargetExpression mutableTargetExpression = targetExpression as TargetExpression;
        if (mutableTargetExpression == null) {
          this.resultExpression = targetExpression;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableTargetExpression);
      }

      /// <summary>
      /// Visits the specified this reference.
      /// </summary>
      /// <param name="thisReference">The this reference.</param>
      public override void Visit(IThisReference thisReference) {
        ThisReference mutableThisReference = thisReference as ThisReference;
        if (mutableThisReference == null) {
          this.resultExpression = thisReference;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableThisReference);
      }

      /// <summary>
      /// Visits the specified throw statement.
      /// </summary>
      /// <param name="throwStatement">The throw statement.</param>
      public override void Visit(IThrowStatement throwStatement) {
        ThrowStatement mutableThrowStatement = throwStatement as ThrowStatement;
        if (mutableThrowStatement == null) {
          this.resultStatement = throwStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableThrowStatement);
      }

      /// <summary>
      /// Visits the specified try catch filter finally statement.
      /// </summary>
      /// <param name="tryCatchFilterFinallyStatement">The try catch filter finally statement.</param>
      public override void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        TryCatchFinallyStatement mutableTryCatchFinallyStatement = tryCatchFilterFinallyStatement as TryCatchFinallyStatement;
        if (mutableTryCatchFinallyStatement == null) {
          this.resultStatement = tryCatchFilterFinallyStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableTryCatchFinallyStatement);
      }

      /// <summary>
      /// Visits the specified token of.
      /// </summary>
      /// <param name="tokenOf">The token of.</param>
      public override void Visit(ITokenOf tokenOf) {
        TokenOf mutableTokenOf = tokenOf as TokenOf;
        if (mutableTokenOf == null) {
          this.resultExpression = tokenOf;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableTokenOf);
      }

      /// <summary>
      /// Visits the specified type of.
      /// </summary>
      /// <param name="typeOf">The type of.</param>
      public override void Visit(ITypeOf typeOf) {
        TypeOf mutableTypeOf = typeOf as TypeOf;
        if (mutableTypeOf == null) {
          this.resultExpression = typeOf;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableTypeOf);
      }

      /// <summary>
      /// Visits the specified unary negation.
      /// </summary>
      /// <param name="unaryNegation">The unary negation.</param>
      public override void Visit(IUnaryNegation unaryNegation) {
        UnaryNegation mutableUnaryNegation = unaryNegation as UnaryNegation;
        if (mutableUnaryNegation == null) {
          this.resultExpression = unaryNegation;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableUnaryNegation);
      }

      /// <summary>
      /// Visits the specified unary plus.
      /// </summary>
      /// <param name="unaryPlus">The unary plus.</param>
      public override void Visit(IUnaryPlus unaryPlus) {
        UnaryPlus mutableUnaryPlus = unaryPlus as UnaryPlus;
        if (mutableUnaryPlus == null) {
          this.resultExpression = unaryPlus;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableUnaryPlus);
      }

      /// <summary>
      /// Visits the specified vector length.
      /// </summary>
      /// <param name="vectorLength">Length of the vector.</param>
      public override void Visit(IVectorLength vectorLength) {
        VectorLength mutableVectorLength = vectorLength as VectorLength;
        if (mutableVectorLength == null) {
          this.resultExpression = vectorLength;
          return;
        }
        this.resultExpression = this.myCodeMutator.Visit(mutableVectorLength);
      }

      /// <summary>
      /// Visits the specified while do statement.
      /// </summary>
      /// <param name="whileDoStatement">The while do statement.</param>
      public override void Visit(IWhileDoStatement whileDoStatement) {
        WhileDoStatement mutableWhileDoStatement = whileDoStatement as WhileDoStatement;
        if (mutableWhileDoStatement == null) {
          this.resultStatement = whileDoStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableWhileDoStatement);
      }

      /// <summary>
      /// Visits the specified yield break statement.
      /// </summary>
      /// <param name="yieldBreakStatement">The yield break statement.</param>
      public override void Visit(IYieldBreakStatement yieldBreakStatement) {
        YieldBreakStatement mutableYieldBreakStatement = yieldBreakStatement as YieldBreakStatement;
        if (mutableYieldBreakStatement == null) {
          this.resultStatement = yieldBreakStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableYieldBreakStatement);
      }

      /// <summary>
      /// Visits the specified yield return statement.
      /// </summary>
      /// <param name="yieldReturnStatement">The yield return statement.</param>
      public override void Visit(IYieldReturnStatement yieldReturnStatement) {
        YieldReturnStatement mutableYieldReturnStatement = yieldReturnStatement as YieldReturnStatement;
        if (mutableYieldReturnStatement == null) {
          this.resultStatement = yieldReturnStatement;
          return;
        }
        this.resultStatement = this.myCodeMutator.Visit(mutableYieldReturnStatement);
      }

      #endregion overriding implementations of ICodeVisitor Members
    }
  }
}

namespace Microsoft.Cci.MutableCodeModel.Contracts {

  /// <summary>
  /// A class that traverses a mutable contract, code and metadata model in depth first, left to right order,
  /// rewriting each mutable node it visits by updating the node's children with recursivly rewritten nodes.
  /// </summary>
  public class CodeAndContractRewriter : CodeRewriter {

    /// <summary>
    /// A class that traverses a mutable contract, code and metadata model in depth first, left to right order,
    /// rewriting each mutable node it visits by updating the node's children with recursivly rewritten nodes.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this rewriter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyAndRewriteImmutableReferences">If true, the rewriter replaces frozen or immutable references with shallow copies.</param>
    public CodeAndContractRewriter(IMetadataHost host, bool copyAndRewriteImmutableReferences = false)
      : base(host, copyAndRewriteImmutableReferences) {
    }

    /// <summary>
    /// A class that traverses a mutable contract, code and metadata model in depth first, left to right order,
    /// rewriting each node it visits by updating the node's children with recursivly rewritten nodes.
    /// Important: ALL nodes in the model to rewrite must come from the mutable code and metadata model.
    /// The rewritten model, however, may incorporate other kinds of nodes.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this rewriter. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    /// <param name="copyAndRewriteImmutableReferences">If true, the rewriter replaces frozen or immutable references with shallow copies.</param>
    public CodeAndContractRewriter(IMetadataHost host, ContractProvider/*?*/ contractProvider, bool copyAndRewriteImmutableReferences = false)
      : base(host, copyAndRewriteImmutableReferences) {
      this.contractProvider = contractProvider;
    }

    /// <summary>
    /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.
    /// </summary>
    protected readonly ContractProvider/*?*/ contractProvider;

    /// <summary>
    /// Rewrites the given list of trigger expressions.
    /// </summary>
    public virtual IEnumerable<IEnumerable<IExpression>>/*?*/ Rewrite(IEnumerable<IEnumerable<IExpression>>/*?*/ triggers) {
      var result = new List<IEnumerable<IExpression>>(triggers);
      for (int i = 0, n = result.Count; i < n; i++) {
        var list = new List<IExpression>(result[i]);
        result[i] = this.Rewrite(list).AsReadOnly();
      }
      return result.AsReadOnly();
    }

    /// <summary>
    /// Rewrites the given loop contract.
    /// </summary>
    public virtual ILoopContract Rewrite(ILoopContract loopContract) {
      Contract.Requires(loopContract != null);
      Contract.Ensures(Contract.Result<ILoopContract>() != null);

      var mutableLoopContract = loopContract as LoopContract;
      if (mutableLoopContract == null) return loopContract;
      this.RewriteChildren(mutableLoopContract);
      return mutableLoopContract;
    }

    /// <summary>
    /// Rewrites the given loop invariant.
    /// </summary>
    public virtual ILoopInvariant Rewrite(ILoopInvariant loopInvariant) {
      Contract.Requires(loopInvariant != null);
      Contract.Ensures(Contract.Result<ILoopInvariant>() != null);

      var mutableLoopInvariant = loopInvariant as LoopInvariant;
      if (mutableLoopInvariant == null) return loopInvariant;
      this.RewriteChildren(mutableLoopInvariant);
      return mutableLoopInvariant;
    }

    /// <summary>
    /// Rewrites the given method contract.
    /// </summary>
    public virtual IMethodContract Rewrite(IMethodContract methodContract) {
      Contract.Requires(methodContract != null);
      Contract.Ensures(Contract.Result<IMethodContract>() != null);

      var mutableMethodContract = methodContract as MethodContract;
      if (mutableMethodContract == null) return methodContract;
      this.RewriteChildren(mutableMethodContract);
      return mutableMethodContract;
    }

    /// <summary>
    /// Rewrites the given postCondition.
    /// </summary>
    public virtual IPostcondition Rewrite(IPostcondition postCondition) {
      Contract.Requires(postCondition != null);
      Contract.Ensures(Contract.Result<IPostcondition>() != null);

      var mutablePostcondition = postCondition as Postcondition;
      if (mutablePostcondition == null) return postCondition;
      this.RewriteChildren(mutablePostcondition);
      return mutablePostcondition;
    }

    /// <summary>
    /// Rewrites the given pre condition.
    /// </summary>
    public virtual IPrecondition Rewrite(IPrecondition precondition) {
      Contract.Requires(precondition != null);
      Contract.Ensures(Contract.Result<IPrecondition>() != null);

      var mutablePrecondition = precondition as Precondition;
      if (mutablePrecondition == null) return precondition;
      this.RewriteChildren(mutablePrecondition);
      return mutablePrecondition;
    }

    /// <summary>
    /// Rewrites the specified statement.
    /// </summary>
    public override IStatement Rewrite(IStatement statement) {
      Contract.Ensures(Contract.Result<IStatement>() != null);
      var rewrittenStatement = base.Rewrite(statement);
      if (this.contractProvider != null) {
        var loopContract = this.contractProvider.GetLoopContractFor(statement);
        if (loopContract != null) {
          var rewrittenLoopContract = this.Rewrite(loopContract);
          this.contractProvider.AssociateLoopWithContract(rewrittenStatement, rewrittenLoopContract);
        }
      }
      return rewrittenStatement;
    }

    /// <summary>
    /// Rewrites the given thrown exception.
    /// </summary>
    public virtual IThrownException Rewrite(IThrownException thrownException) {
      Contract.Requires(thrownException != null);
      Contract.Ensures(Contract.Result<IThrownException>() != null);

      var mutableThrownException = thrownException as ThrownException;
      if (mutableThrownException == null) return thrownException;
      this.RewriteChildren(mutableThrownException);
      return mutableThrownException;
    }

    /// <summary>
    /// Rewrites the given type contract.
    /// </summary>
    public virtual ITypeContract Rewrite(ITypeContract typeContract) {
      Contract.Requires(typeContract != null);
      Contract.Ensures(Contract.Result<ITypeContract>() != null);

      var mutableTypeContract = typeContract as TypeContract;
      if (mutableTypeContract == null) return typeContract;
      this.RewriteChildren(mutableTypeContract);
      return mutableTypeContract;
    }

    /// <summary>
    /// Rewrites the given type invariant.
    /// </summary>
    public virtual ITypeInvariant Rewrite(ITypeInvariant typeInvariant) {
      Contract.Requires(typeInvariant != null);
      Contract.Ensures(Contract.Result<ITypeInvariant>() != null);

      var mutableTypeInvariant = typeInvariant as TypeInvariant;
      if (mutableTypeInvariant == null) return typeInvariant;
      this.RewriteChildren(mutableTypeInvariant);
      return mutableTypeInvariant;
    }

    /// <summary>
    /// Rewrites the given method call.
    /// </summary>
    public override IExpression Rewrite(IMethodCall methodCall) {
      var rewrittenMethodCall = base.Rewrite(methodCall);
      if (this.contractProvider != null) {
        var triggers = this.contractProvider.GetTriggersFor(methodCall);
        if (triggers != null) {
          var rewrittenTriggers = this.Rewrite(triggers);
          this.contractProvider.AssociateTriggersWithQuantifier(rewrittenMethodCall, rewrittenTriggers);
        }
      }
      return rewrittenMethodCall;
    }

    /// <summary>
    /// Rewrites the given method definition.
    /// </summary>
    public override IMethodDefinition Rewrite(IMethodDefinition method) {
      var rewrittenMethod = base.Rewrite(method);
      if (this.contractProvider != null) {
        var methodContract = this.contractProvider.GetMethodContractFor(method);
        if (methodContract != null) {
          var rewrittenContract = this.Rewrite(methodContract);
          this.contractProvider.AssociateMethodWithContract(rewrittenMethod, rewrittenContract);
        }
      }
      return rewrittenMethod;
    }

    /// <summary>
    /// Rewrites the given type definition.
    /// </summary>
    public override ITypeDefinition Rewrite(ITypeDefinition typeDefinition) {
      var result = base.Rewrite(typeDefinition);
      if (this.contractProvider == null) return result;
      var typeContract = this.contractProvider.GetTypeContractFor(typeDefinition) as TypeContract;
      if (typeContract != null) {
        var newContract = this.Rewrite(typeContract);
        this.contractProvider.AssociateTypeWithContract(result, newContract);
      }
      return result;
    }

    /// <summary>
    /// Rewrites the given list of addressable expressions.
    /// </summary>
    public virtual List<IAddressableExpression>/*?*/ Rewrite(List<IAddressableExpression>/*?*/ addressableExpressions) {
      if (addressableExpressions == null) return null;
      for (int i = 0, n = addressableExpressions.Count; i < n; i++)
        addressableExpressions[i] = this.Rewrite(addressableExpressions[i]);
      return addressableExpressions;
    }

    /// <summary>
    /// Rewrites the given list of loop invariants.
    /// </summary>
    public virtual List<ILoopInvariant>/*?*/ Rewrite(List<ILoopInvariant>/*?*/ loopInvariants) {
      if (loopInvariants == null) return null;
      for (int i = 0, n = loopInvariants.Count; i < n; i++)
        loopInvariants[i] = this.Rewrite(loopInvariants[i]);
      return loopInvariants;
    }

    /// <summary>
    /// Rewrites the given list of post conditions.
    /// </summary>
    public virtual List<IPostcondition>/*?*/ Rewrite(List<IPostcondition>/*?*/ postConditions) {
      if (postConditions == null) return null;
      for (int i = 0, n = postConditions.Count; i < n; i++)
        postConditions[i] = this.Rewrite(postConditions[i]);
      return postConditions;
    }

    /// <summary>
    /// Rewrites the given list of pre conditions.
    /// </summary>
    public virtual List<IPrecondition>/*?*/ Rewrite(List<IPrecondition>/*?*/ preconditions) {
      if (preconditions == null) return null;
      for (int i = 0, n = preconditions.Count; i < n; i++)
        preconditions[i] = this.Rewrite(preconditions[i]);
      return preconditions;
    }

    /// <summary>
    /// Rewrites the given list of thrown exceptions.
    /// </summary>
    public virtual List<IThrownException>/*?*/ Rewrite(List<IThrownException>/*?*/ thrownExceptions) {
      if (thrownExceptions == null) return null;
      for (int i = 0, n = thrownExceptions.Count; i < n; i++)
        thrownExceptions[i] = this.Rewrite(thrownExceptions[i]);
      return thrownExceptions;
    }

    /// <summary>
    /// Rewrites the given list of addressable expressions.
    /// </summary>
    public virtual List<ITypeInvariant>/*?*/ Rewrite(List<ITypeInvariant>/*?*/ typeInvariants) {
      if (typeInvariants == null) return null;
      for (int i = 0, n = typeInvariants.Count; i < n; i++)
        typeInvariants[i] = this.Rewrite(typeInvariants[i]);
      return typeInvariants;
    }

    /// <summary>
    /// Called from the type specific rewrite method to rewrite the common part of all contract elments.
    /// </summary>
    /// <param name="contractElement"></param>
    public virtual void RewriteChildren(ContractElement contractElement) {
      Contract.Requires(contractElement != null);

      contractElement.Condition = this.Rewrite(contractElement.Condition);
      if (contractElement.Description != null)
        contractElement.Description = this.Rewrite(contractElement.Description);
    }

    /// <summary>
    /// Rewrites the children of the given loop contract.
    /// </summary>
    public virtual void RewriteChildren(LoopContract loopContract) {
      Contract.Requires(loopContract != null);

      loopContract.Invariants = this.Rewrite(loopContract.Invariants);
      loopContract.Variants = this.Rewrite(loopContract.Variants);
      loopContract.Writes = this.Rewrite(loopContract.Writes);
    }

    /// <summary>
    /// Rewrites the children of the given loop invariant.
    /// </summary>
    public virtual void RewriteChildren(LoopInvariant loopInvariant) {
      Contract.Requires(loopInvariant != null);

      this.RewriteChildren((ContractElement)loopInvariant);
    }

    /// <summary>
    /// Rewrites the children of the given method contract.
    /// </summary>
    public virtual void RewriteChildren(MethodContract methodContract) {
      Contract.Requires(methodContract != null);

      methodContract.Allocates = this.Rewrite(methodContract.Allocates);
      methodContract.Frees = this.Rewrite(methodContract.Frees);
      methodContract.ModifiedVariables = this.Rewrite(methodContract.ModifiedVariables);
      methodContract.Postconditions = this.Rewrite(methodContract.Postconditions);
      methodContract.Preconditions = this.Rewrite(methodContract.Preconditions);
      methodContract.Reads = this.Rewrite(methodContract.Reads);
      methodContract.ThrownExceptions = this.Rewrite(methodContract.ThrownExceptions);
      methodContract.Writes = this.Rewrite(methodContract.Writes);
    }

    /// <summary>
    /// Rewrites the children of the given pre condition.
    /// </summary>
    public virtual void RewriteChildren(Precondition precondition) {
      Contract.Requires(precondition != null);

      this.RewriteChildren((ContractElement)precondition);
      if (precondition.ExceptionToThrow != null)
        precondition.ExceptionToThrow = this.Rewrite(precondition.ExceptionToThrow);
    }

    /// <summary>
    /// Rewrites the children of the given thrown exception.
    /// </summary>
    public virtual void RewriteChildren(ThrownException thrownException) {
      Contract.Requires(thrownException != null);

      thrownException.ExceptionType = this.Rewrite(thrownException.ExceptionType);
      thrownException.Postcondition = this.Rewrite((Postcondition)thrownException.Postcondition);
    }

    /// <summary>
    /// Rewrites the children of the given type contract.
    /// </summary>
    public virtual void RewriteChildren(TypeContract typeContract) {
      Contract.Requires(typeContract != null);

      typeContract.ContractFields = this.Rewrite(typeContract.ContractFields);
      typeContract.ContractMethods = this.Rewrite(typeContract.ContractMethods);
      typeContract.Invariants = this.Rewrite(typeContract.Invariants);
    }

    /// <summary>
    /// Rewrites the children of the given type invariant.
    /// </summary>
    public virtual void RewriteChildren(TypeInvariant typeInvariant) {
      Contract.Requires(typeInvariant != null);

      this.RewriteChildren((ContractElement)typeInvariant);
    }

  }

  /// <summary>
  /// Uses the inherited methods from MetadataMutator to walk everything down to the method body level,
  /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
  /// Also visits and mutates the associated code contracts and establishes associations with new copies.
  /// </summary>
  /// <remarks>While the model is being copied, the resulting model is incomplete and or inconsistent. It should not be traversed
  /// independently nor should any of its computed properties, such as ResolvedType be evaluated. Scenarios that need such functionality
  /// should be implemented by first making a mutable copy of the entire assembly and then running a second pass over the mutable result.
  /// The new classes CodeAndContractDeepCopier and CodeAndContractRewriter are meant to facilitate such scenarios.
  /// </remarks>
  [Obsolete("This class has been superceded by CodeAndContractDeepCopier and CodeAndContractRewriter, used in combination. It will go away after April 2011")]
  public class CodeAndContractMutator : CodeMutator {

    /// <summary>
    /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.
    /// </summary>
    protected readonly ContractProvider/*?*/ contractProvider;

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutator to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// The mutator also visits and mutates the associated code contracts and establishes associations with new copies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public CodeAndContractMutator(IMetadataHost host)
      : base(host) { }

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutator to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// The mutator also visits and mutates the associated code contracts and establishes associations with new copies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable">True if the mutator should try and perform mutations in place, rather than mutating new copies.</param>
    public CodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host, copyOnlyIfNotAlreadyMutable) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public CodeAndContractMutator(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceLocationProvider) {
      this.contractProvider = contractProvider;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable"></param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public CodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, copyOnlyIfNotAlreadyMutable, sourceLocationProvider) {
      this.contractProvider = contractProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeAndContractMutator"/> class.
    /// </summary>
    /// <param name="template">The template.</param>
    protected CodeAndContractMutator(CodeAndContractMutator template)
      : base(template.host, template.sourceLocationProvider) {
      this.contractProvider = template.contractProvider;
    }

    /// <summary>
    /// Visits the specified addressable expressions.
    /// </summary>
    /// <param name="addressableExpressions">The addressable expressions.</param>
    /// <returns></returns>
    public virtual List<IAddressableExpression> Visit(List<IAddressableExpression> addressableExpressions) {
      List<IAddressableExpression> newList = new List<IAddressableExpression>();
      foreach (var addressableExpression in addressableExpressions)
        newList.Add(this.Visit(addressableExpression));
      return newList;
    }

    /// <summary>
    /// Visits the specified triggers.
    /// </summary>
    /// <param name="triggers">The triggers.</param>
    /// <returns></returns>
    public virtual IEnumerable<IEnumerable<IExpression>> Visit(IEnumerable<IEnumerable<IExpression>> triggers) {
      List<IEnumerable<IExpression>> newTriggers = new List<IEnumerable<IExpression>>(triggers);
      for (int i = 0, n = newTriggers.Count; i < n; i++)
        newTriggers[i] = this.Visit(new List<IExpression>(newTriggers[i])).AsReadOnly();
      return newTriggers.AsReadOnly();
    }

    /// <summary>
    /// Visits the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns></returns>
    public override IExpression Visit(IExpression expression) {
      IExpression result = base.Visit(expression);
      if (this.contractProvider != null && expression is IMethodCall) {
        IEnumerable<IEnumerable<IExpression>>/*?*/ triggers = this.contractProvider.GetTriggersFor(expression);
        if (triggers != null)
          this.contractProvider.AssociateTriggersWithQuantifier(result, this.Visit(triggers));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified loop contract.
    /// </summary>
    /// <param name="loopContract">The loop contract.</param>
    /// <returns></returns>
    public virtual ILoopContract Visit(ILoopContract loopContract) {
      LoopContract mutableLoopContract = loopContract as LoopContract;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableLoopContract == null)
        mutableLoopContract = new LoopContract(loopContract);
      return this.Visit(mutableLoopContract);
    }

    /// <summary>
    /// Visits the specified loop contract.
    /// </summary>
    /// <param name="loopContract">The loop contract.</param>
    /// <returns></returns>
    public virtual ILoopContract Visit(LoopContract loopContract) {
      loopContract.Invariants = this.Visit(loopContract.Invariants);
      loopContract.Writes = this.Visit(loopContract.Writes);
      return loopContract;
    }

    /// <summary>
    /// Visits the specified loop invariants.
    /// </summary>
    /// <param name="loopInvariants">The loop invariants.</param>
    /// <returns></returns>
    public virtual List<ILoopInvariant> Visit(List<ILoopInvariant> loopInvariants) {
      List<ILoopInvariant> newList = new List<ILoopInvariant>();
      foreach (var loopInvariant in loopInvariants)
        newList.Add(this.Visit(loopInvariant));
      return newList;
    }

    /// <summary>
    /// Visits the specified loop invariant.
    /// </summary>
    /// <param name="loopInvariant">The loop invariant.</param>
    /// <returns></returns>
    public virtual ILoopInvariant Visit(ILoopInvariant loopInvariant) {
      LoopInvariant mutableLoopInvariant = loopInvariant as LoopInvariant;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableLoopInvariant == null)
        mutableLoopInvariant = new LoopInvariant(loopInvariant);
      return this.Visit(mutableLoopInvariant);
    }

    /// <summary>
    /// Visits the specified loop invariant.
    /// </summary>
    /// <param name="loopInvariant">The loop invariant.</param>
    /// <returns></returns>
    public virtual ILoopInvariant Visit(LoopInvariant loopInvariant) {
      loopInvariant.Condition = this.Visit(loopInvariant.Condition);
      if (loopInvariant.Description != null)
        loopInvariant.Description = this.Visit(loopInvariant.Description);
      return loopInvariant;
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
    public override IMethodDefinition Visit(IMethodDefinition methodDefinition) {
      var result = this.GetMutableCopy(methodDefinition);
      if (this.contractProvider != null) {
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(methodDefinition);
        if (methodContract != null)
          this.contractProvider.AssociateMethodWithContract(result, methodContract);
      }
      return this.Visit(result);
    }

    /// <summary>
    /// Visits the specified global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition">The global method definition.</param>
    /// <returns></returns>
    public override IGlobalMethodDefinition Visit(IGlobalMethodDefinition globalMethodDefinition) {
      var result = this.GetMutableCopy(globalMethodDefinition);
      if (this.contractProvider != null) {
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(globalMethodDefinition);
        if (methodContract != null)
          this.contractProvider.AssociateMethodWithContract(result, methodContract);
      }
      return this.Visit(result);
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
    public override MethodDefinition Visit(MethodDefinition methodDefinition) {
      if (this.stopTraversal) return methodDefinition;
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

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    /// <returns></returns>
    public override IMethodBody Visit(IMethodBody methodBody) {
      ISourceMethodBody sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null) {
        SourceMethodBody mutableSourceMethodBody = new SourceMethodBody(this.host, this.sourceLocationProvider);
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

    /// <summary>
    /// Visits the specified method contract.
    /// </summary>
    /// <param name="methodContract">The method contract.</param>
    /// <returns></returns>
    public virtual IMethodContract Visit(IMethodContract methodContract) {
      MethodContract mutableMethodContract = methodContract as MethodContract;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableMethodContract == null)
        mutableMethodContract = new MethodContract(methodContract);
      return this.Visit(mutableMethodContract);
    }

    /// <summary>
    /// Visits the specified method contract.
    /// </summary>
    /// <param name="methodContract">The method contract.</param>
    /// <returns></returns>
    public virtual IMethodContract Visit(MethodContract methodContract) {
      methodContract.Allocates = this.Visit(methodContract.Allocates);
      methodContract.Frees = this.Visit(methodContract.Frees);
      methodContract.ModifiedVariables = this.Visit(methodContract.ModifiedVariables);
      methodContract.Postconditions = this.Visit(methodContract.Postconditions);
      methodContract.Preconditions = this.Visit(methodContract.Preconditions);
      methodContract.Reads = this.Visit(methodContract.Reads);
      methodContract.ThrownExceptions = this.Visit(methodContract.ThrownExceptions);
      methodContract.Writes = this.Visit(methodContract.Writes);
      return methodContract;
    }

    /// <summary>
    /// Visits the specified post conditions.
    /// </summary>
    /// <param name="postConditions">The post conditions.</param>
    /// <returns></returns>
    public virtual List<IPostcondition> Visit(List<IPostcondition> postConditions) {
      List<IPostcondition> newList = new List<IPostcondition>();
      foreach (var postCondition in postConditions)
        newList.Add(this.Visit(postCondition));
      return newList;
    }

    /// <summary>
    /// Visits the specified post condition.
    /// </summary>
    /// <param name="postCondition">The post condition.</param>
    public virtual IPostcondition Visit(IPostcondition postCondition) {
      Postcondition mutablePostCondition = postCondition as Postcondition;
      if (!this.copyOnlyIfNotAlreadyMutable || mutablePostCondition == null)
        mutablePostCondition = new Postcondition(postCondition);
      return this.Visit(mutablePostCondition);
    }

    /// <summary>
    /// Visits the specified post condition.
    /// </summary>
    /// <param name="postCondition">The post condition.</param>
    public virtual IPostcondition Visit(Postcondition postCondition) {
      postCondition.Condition = this.Visit(postCondition.Condition);
      if (postCondition.Description != null)
        postCondition.Description = this.Visit(postCondition.Description);
      return postCondition;
    }

    /// <summary>
    /// Visits the specified preconditions.
    /// </summary>
    /// <param name="preconditions">The preconditions.</param>
    public virtual List<IPrecondition> Visit(List<IPrecondition> preconditions) {
      List<IPrecondition> newList = new List<IPrecondition>();
      foreach (var precondition in preconditions)
        newList.Add(this.Visit(precondition));
      return newList;
    }

    /// <summary>
    /// Visits the specified precondition.
    /// </summary>
    /// <param name="precondition">The precondition.</param>
    public virtual IPrecondition Visit(IPrecondition precondition) {
      Precondition mutablePrecondition = precondition as Precondition;
      if (!this.copyOnlyIfNotAlreadyMutable || mutablePrecondition == null)
        mutablePrecondition = new Precondition(precondition);
      return this.Visit(mutablePrecondition);
    }

    /// <summary>
    /// Visits the specified precondition.
    /// </summary>
    /// <param name="precondition">The precondition.</param>
    public virtual IPrecondition Visit(Precondition precondition) {
      precondition.Condition = this.Visit(precondition.Condition);
      if (precondition.Description != null)
        precondition.Description = this.Visit(precondition.Description);
      if (precondition.ExceptionToThrow != null)
        precondition.ExceptionToThrow = this.Visit(precondition.ExceptionToThrow);
      return precondition;
    }

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public override IStatement Visit(IStatement statement) {
      IStatement result = base.Visit(statement);
      if (this.contractProvider != null) {
        ILoopContract/*?*/ loopContract = this.contractProvider.GetLoopContractFor(statement);
        if (loopContract != null)
          this.contractProvider.AssociateLoopWithContract(result, this.Visit(loopContract));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified thrown exceptions.
    /// </summary>
    /// <param name="thrownExceptions">The thrown exceptions.</param>
    public virtual List<IThrownException> Visit(List<IThrownException> thrownExceptions) {
      List<IThrownException> newList = new List<IThrownException>();
      foreach (var thrownException in thrownExceptions)
        newList.Add(this.Visit(thrownException));
      return newList;
    }

    /// <summary>
    /// Visits the specified thrown exception.
    /// </summary>
    /// <param name="thrownException">The thrown exception.</param>
    public virtual IThrownException Visit(IThrownException thrownException) {
      ThrownException mutableThrownException = thrownException as ThrownException;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableThrownException == null)
        mutableThrownException = new ThrownException(thrownException);
      return this.Visit(mutableThrownException);
    }

    /// <summary>
    /// Visits the specified thrown exception.
    /// </summary>
    /// <param name="thrownException">The thrown exception.</param>
    public virtual IThrownException Visit(ThrownException thrownException) {
      thrownException.ExceptionType = this.Visit(thrownException.ExceptionType);
      thrownException.Postcondition = this.Visit(thrownException.Postcondition);
      return thrownException;
    }

    /// <summary>
    /// Visits the specified type contract.
    /// </summary>
    /// <param name="typeContract">The type contract.</param>
    public virtual ITypeContract Visit(ITypeContract typeContract) {
      TypeContract mutableTypeContract = typeContract as TypeContract;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableTypeContract == null)
        mutableTypeContract = new TypeContract(typeContract);
      return this.Visit(mutableTypeContract);
    }

    /// <summary>
    /// Visits the specified type contract.
    /// </summary>
    /// <param name="typeContract">The type contract.</param>
    public virtual ITypeContract Visit(TypeContract typeContract) {
      typeContract.ContractFields = this.Visit(typeContract.ContractFields);
      typeContract.ContractMethods = this.Visit(typeContract.ContractMethods);
      typeContract.Invariants = this.Visit(typeContract.Invariants);
      return typeContract;
    }

    /// <summary>
    /// Visits the specified type invariants.
    /// </summary>
    /// <param name="typeInvariants">The type invariants.</param>
    public virtual List<ITypeInvariant> Visit(List<ITypeInvariant> typeInvariants) {
      List<ITypeInvariant> newList = new List<ITypeInvariant>();
      foreach (var typeInvariant in typeInvariants)
        newList.Add(this.Visit(typeInvariant));
      return newList;
    }

    /// <summary>
    /// Visits the specified type invariant.
    /// </summary>
    /// <param name="typeInvariant">The type invariant.</param>
    public virtual ITypeInvariant Visit(ITypeInvariant typeInvariant) {
      TypeInvariant mutableTypeInvariant = typeInvariant as TypeInvariant;
      if (!this.copyOnlyIfNotAlreadyMutable || mutableTypeInvariant == null)
        mutableTypeInvariant = new TypeInvariant(typeInvariant);
      return this.Visit(mutableTypeInvariant);
    }

    /// <summary>
    /// Visits the specified type invariant.
    /// </summary>
    /// <param name="typeInvariant">The type invariant.</param>
    public virtual ITypeInvariant Visit(TypeInvariant typeInvariant) {
      typeInvariant.Condition = this.Visit(typeInvariant.Condition);
      if (typeInvariant.Description != null)
        typeInvariant.Description = this.Visit(typeInvariant.Description);
      return typeInvariant;
    }

    /// <summary>
    /// Visits the specified namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition">The namespace type definition.</param>
    public override INamespaceTypeDefinition Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      var result = base.Visit(namespaceTypeDefinition);
      if (this.contractProvider != null) {
        ITypeContract/*?*/ typeContract = this.contractProvider.GetTypeContractFor(namespaceTypeDefinition);
        if (typeContract != null)
          this.contractProvider.AssociateTypeWithContract(result, this.Visit(typeContract));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition">The nested type definition.</param>
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
  /// Use this as a base class when you define a code and contract mutator that mutates ONLY
  /// method bodies and their contracts.
  /// This class has overrides for Visit(IFieldReference), Visit(IMethodReference), and
  /// Visit(ITypeReference) that make sure to not modify the references.
  /// </summary>
  [Obsolete("Please use CodeAndContractRewriter")]
  public class MethodBodyCodeAndContractMutator : CodeAndContractMutatingVisitor {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    public MethodBodyCodeAndContractMutator(IMetadataHost host)
      : base(host) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="copyOnlyIfNotAlreadyMutable"></param>
    public MethodBodyCodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable)
      : base(host) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public MethodBodyCodeAndContractMutator(IMetadataHost host,
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceLocationProvider, contractProvider) { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host"></param>
    /// <param name="copyOnlyIfNotAlreadyMutable"></param>
    /// <param name="sourceLocationProvider"></param>
    /// <param name="contractProvider"></param>
    public MethodBodyCodeAndContractMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable, ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceLocationProvider, contractProvider) { }

    #region All code mutators that are not mutating an entire assembly need to *not* modify certain references
    /// <summary>
    /// Visits the specified field reference.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    public override IFieldReference Visit(IFieldReference fieldReference) {
      return fieldReference;
    }

    /// <summary>
    /// Visits a reference to the specified local definition.
    /// </summary>
    /// <param name="localDefinition">The referenced local definition to visit.</param>
    /// <returns></returns>
    public override ILocalDefinition VisitReferenceTo(ILocalDefinition localDefinition) {
      return localDefinition;
    }

    /// <summary>
    /// Visits the specified method reference.
    /// </summary>
    public override IMethodReference Visit(IMethodReference methodReference) {
      return methodReference;
    }

    /// <summary>
    /// Visits a parameter definition that is being referenced.
    /// </summary>
    /// <param name="parameterDefinition">The referenced parameter definition.</param>
    public override IParameterDefinition VisitReferenceTo(IParameterDefinition parameterDefinition) {
      return parameterDefinition;
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    public override ITypeReference Visit(ITypeReference typeReference) {
      return typeReference;
    }

    #endregion All code mutators that are not mutating an entire assembly need to *not* modify certain references
  }

  /// <summary>
  /// 
  /// </summary>
  [Obsolete("Please use CodeAndContractRewriter")]
  public class CodeAndContractMutatingVisitor : CodeMutatingVisitor {
    /// <summary>
    /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.
    /// </summary>
    protected readonly ContractProvider/*?*/ contractProvider;

    /// <summary>
    /// Allocates a mutator that uses the inherited methods from MetadataMutator to walk everything down to the method body level,
    /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
    /// The mutator also visits and mutates the associated code contracts and establishes associations with new copies.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public CodeAndContractMutatingVisitor(IMetadataHost host)
      : base(host) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public CodeAndContractMutatingVisitor(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider)
      : base(host, sourceLocationProvider) {
      this.contractProvider = contractProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeAndContractMutator"/> class.
    /// </summary>
    /// <param name="template">The template.</param>
    protected CodeAndContractMutatingVisitor(CodeAndContractMutatingVisitor template)
      : base(template.host, template.sourceLocationProvider) {
      this.contractProvider = template.contractProvider;
    }

    /// <summary>
    /// Visits the specified addressable expressions.
    /// </summary>
    /// <param name="addressableExpressions">The addressable expressions.</param>
    /// <returns></returns>
    public virtual List<IAddressableExpression> Visit(List<IAddressableExpression> addressableExpressions) {
      if (this.stopTraversal) return addressableExpressions;
      for (int i = 0, n = addressableExpressions.Count; i < n; i++)
        addressableExpressions[i] = this.Visit(addressableExpressions[i]);
      return addressableExpressions;
    }

    /// <summary>
    /// Visits the specified triggers.
    /// </summary>
    /// <param name="triggers">The triggers.</param>
    /// <returns></returns>
    public virtual IEnumerable<IEnumerable<IExpression>> Visit(IEnumerable<IEnumerable<IExpression>> triggers) {
      if (this.stopTraversal) return triggers;
      var newTriggers = new List<IEnumerable<IExpression>>(triggers);
      for (int i = 0, n = newTriggers.Count; i < n; i++)
        newTriggers[i] = this.Visit(new List<IExpression>(newTriggers[i])).AsReadOnly();
      return newTriggers.AsReadOnly();
    }

    /// <summary>
    /// Visits the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns></returns>
    public override IExpression Visit(IExpression expression) {
      if (this.stopTraversal) return expression;
      var result = base.Visit(expression);
      if (this.contractProvider != null && expression is IMethodCall) {
        IEnumerable<IEnumerable<IExpression>>/*?*/ triggers = this.contractProvider.GetTriggersFor(expression);
        if (triggers != null)
          this.contractProvider.AssociateTriggersWithQuantifier(result, this.Visit(triggers));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified loop contract.
    /// </summary>
    /// <param name="loopContract">The loop contract.</param>
    /// <returns></returns>
    public virtual ILoopContract Visit(ILoopContract loopContract) {
      if (this.stopTraversal) return loopContract;
      LoopContract mutableLoopContract = loopContract as LoopContract;
      if (mutableLoopContract == null) return loopContract;
      mutableLoopContract.Invariants = this.Visit(mutableLoopContract.Invariants);
      mutableLoopContract.Writes = this.Visit(mutableLoopContract.Writes);
      return mutableLoopContract;
    }

    /// <summary>
    /// Visits the specified loop invariants.
    /// </summary>
    /// <param name="loopInvariants">The loop invariants.</param>
    /// <returns></returns>
    public virtual List<ILoopInvariant> Visit(List<ILoopInvariant> loopInvariants) {
      if (this.stopTraversal) return loopInvariants;
      for (int i = 0, n = loopInvariants.Count; i < n; i++)
        loopInvariants[i] = this.Visit(loopInvariants[i]);
      return loopInvariants;
    }

    /// <summary>
    /// Visits the specified loop invariant.
    /// </summary>
    /// <param name="loopInvariant">The loop invariant.</param>
    /// <returns></returns>
    public virtual ILoopInvariant Visit(ILoopInvariant loopInvariant) {
      if (this.stopTraversal) return loopInvariant;
      LoopInvariant mutableLoopInvariant = loopInvariant as LoopInvariant;
      if (mutableLoopInvariant == null) return loopInvariant;
      mutableLoopInvariant.Condition = this.Visit(mutableLoopInvariant.Condition);
      if (mutableLoopInvariant.Description != null)
        mutableLoopInvariant.Description = this.Visit(mutableLoopInvariant.Description);
      return mutableLoopInvariant;
    }

    /// <summary>
    /// Visits the method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
    public override IMethodDefinition Visit(IMethodDefinition methodDefinition) {
      if (this.stopTraversal) return methodDefinition;
      var result = base.Visit(methodDefinition);
      if (this.contractProvider != null) {
        //Visit the contract before visiting the method body so that it is all fixed up before
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(methodDefinition);
        if (methodContract != null)
          this.contractProvider.AssociateMethodWithContract(methodDefinition, this.Visit(methodContract));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified method contract.
    /// </summary>
    /// <param name="methodContract">The method contract.</param>
    /// <returns></returns>
    public virtual IMethodContract Visit(IMethodContract methodContract) {
      if (this.stopTraversal) return methodContract;
      MethodContract mutableMethodContract = methodContract as MethodContract;
      if (mutableMethodContract == null) return methodContract;
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

    /// <summary>
    /// Visits the specified post conditions.
    /// </summary>
    /// <param name="postConditions">The post conditions.</param>
    /// <returns></returns>
    public virtual List<IPostcondition> Visit(List<IPostcondition> postConditions) {
      if (this.stopTraversal) return postConditions;
      for (int i = 0, n = postConditions.Count; i < n; i++)
        postConditions[i] = this.Visit(postConditions[i]);
      return postConditions;
    }

    /// <summary>
    /// Visits the specified post condition.
    /// </summary>
    /// <param name="postCondition">The post condition.</param>
    public virtual IPostcondition Visit(IPostcondition postCondition) {
      if (this.stopTraversal) return postCondition;
      Postcondition mutablePostCondition = postCondition as Postcondition;
      if (mutablePostCondition == null) return postCondition;
      mutablePostCondition.Condition = this.Visit(mutablePostCondition.Condition);
      if (mutablePostCondition.Description != null)
        mutablePostCondition.Description = this.Visit(mutablePostCondition.Description);
      return mutablePostCondition;
    }

    /// <summary>
    /// Visits the specified preconditions.
    /// </summary>
    /// <param name="preconditions">The preconditions.</param>
    public virtual List<IPrecondition> Visit(List<IPrecondition> preconditions) {
      if (this.stopTraversal) return preconditions;
      for (int i = 0, n = preconditions.Count; i < n; i++)
        preconditions[i] = this.Visit(preconditions[i]);
      return preconditions;
    }

    /// <summary>
    /// Visits the specified precondition.
    /// </summary>
    /// <param name="precondition">The precondition.</param>
    public virtual IPrecondition Visit(IPrecondition precondition) {
      if (this.stopTraversal) return precondition;
      Precondition mutablePrecondition = precondition as Precondition;
      if (mutablePrecondition == null) return precondition;
      mutablePrecondition.Condition = this.Visit(mutablePrecondition.Condition);
      if (mutablePrecondition.Description != null)
        mutablePrecondition.Description = this.Visit(mutablePrecondition.Description);
      if (mutablePrecondition.ExceptionToThrow != null)
        mutablePrecondition.ExceptionToThrow = this.Visit(mutablePrecondition.ExceptionToThrow);
      return mutablePrecondition;
    }

    /// <summary>
    /// Visits the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public override IStatement Visit(IStatement statement) {
      if (this.stopTraversal) return statement;
      IStatement result = base.Visit(statement);
      if (this.contractProvider != null) {
        ILoopContract/*?*/ loopContract = this.contractProvider.GetLoopContractFor(statement);
        if (loopContract != null)
          this.contractProvider.AssociateLoopWithContract(result, this.Visit(loopContract));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified thrown exceptions.
    /// </summary>
    /// <param name="thrownExceptions">The thrown exceptions.</param>
    public virtual List<IThrownException> Visit(List<IThrownException> thrownExceptions) {
      if (this.stopTraversal) return thrownExceptions;
      for (int i = 0, n = thrownExceptions.Count; i < n; i++)
        thrownExceptions[i] = this.Visit(thrownExceptions[i]);
      return thrownExceptions;
    }

    /// <summary>
    /// Visits the specified thrown exception.
    /// </summary>
    /// <param name="thrownException">The thrown exception.</param>
    public virtual IThrownException Visit(IThrownException thrownException) {
      if (this.stopTraversal) return thrownException;
      ThrownException mutableThrownException = thrownException as ThrownException;
      if (mutableThrownException == null) return thrownException;
      mutableThrownException.ExceptionType = this.Visit(mutableThrownException.ExceptionType);
      mutableThrownException.Postcondition = this.Visit(mutableThrownException.Postcondition);
      return mutableThrownException;
    }

    /// <summary>
    /// Visits the specified type contract.
    /// </summary>
    /// <param name="typeContract">The type contract.</param>
    public virtual ITypeContract Visit(ITypeContract typeContract) {
      if (this.stopTraversal) return typeContract;
      TypeContract mutableTypeContract = typeContract as TypeContract;
      if (mutableTypeContract == null) return typeContract;
      mutableTypeContract.ContractFields = this.Visit(mutableTypeContract.ContractFields);
      mutableTypeContract.ContractMethods = this.Visit(mutableTypeContract.ContractMethods);
      mutableTypeContract.Invariants = this.Visit(mutableTypeContract.Invariants);
      return mutableTypeContract;
    }

    /// <summary>
    /// Visits the specified field invariants.
    /// </summary>
    /// <param name="fieldInvariants">The field invariants.</param>
    public virtual List<IFieldDefinition> Visit(List<IFieldDefinition> fieldInvariants) {
      if (this.stopTraversal) return fieldInvariants;
      for (int i = 0, n = fieldInvariants.Count; i < n; i++)
        fieldInvariants[i] = this.Visit(fieldInvariants[i]);
      return fieldInvariants;
    }

    /// <summary>
    /// Visits the specified contract methods.
    /// </summary>
    /// <param name="contractMethods">The contract methods.</param>
    public virtual List<IMethodDefinition> Visit(List<IMethodDefinition> contractMethods) {
      if (this.stopTraversal) return contractMethods;
      for (int i = 0, n = contractMethods.Count; i < n; i++)
        contractMethods[i] = this.Visit(contractMethods[i]);
      return contractMethods;
    }

    /// <summary>
    /// Visits the specified type invariants.
    /// </summary>
    /// <param name="typeInvariants">The type invariants.</param>
    public virtual List<ITypeInvariant> Visit(List<ITypeInvariant> typeInvariants) {
      if (this.stopTraversal) return typeInvariants;
      for (int i = 0, n = typeInvariants.Count; i < n; i++)
        typeInvariants[i] = this.Visit(typeInvariants[i]);
      return typeInvariants;
    }

    /// <summary>
    /// Visits the specified type invariant.
    /// </summary>
    /// <param name="typeInvariant">The type invariant.</param>
    public virtual ITypeInvariant Visit(ITypeInvariant typeInvariant) {
      if (this.stopTraversal) return typeInvariant;
      TypeInvariant mutableTypeInvariant = typeInvariant as TypeInvariant;
      if (mutableTypeInvariant == null) return typeInvariant;
      mutableTypeInvariant.Condition = this.Visit(mutableTypeInvariant.Condition);
      if (mutableTypeInvariant.Description != null)
        mutableTypeInvariant.Description = this.Visit(mutableTypeInvariant.Description);
      return mutableTypeInvariant;
    }

    /// <summary>
    /// Visits the specified namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition">The namespace type definition.</param>
    public override INamespaceTypeDefinition Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      if (this.stopTraversal) return namespaceTypeDefinition;
      var result = base.Visit(namespaceTypeDefinition);
      if (this.contractProvider != null) {
        ITypeContract/*?*/ typeContract = this.contractProvider.GetTypeContractFor(namespaceTypeDefinition);
        if (typeContract != null)
          this.contractProvider.AssociateTypeWithContract(result, this.Visit(typeContract));
      }
      return result;
    }

    /// <summary>
    /// Visits the specified nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition">The nested type definition.</param>
    public override INestedTypeDefinition Visit(INestedTypeDefinition nestedTypeDefinition) {
      if (this.stopTraversal) return nestedTypeDefinition;
      var result = base.Visit(nestedTypeDefinition);
      if (this.contractProvider != null) {
        ITypeContract/*?*/ typeContract = this.contractProvider.GetTypeContractFor(nestedTypeDefinition);
        if (typeContract != null)
          this.contractProvider.AssociateTypeWithContract(result, this.Visit(typeContract));
      }
      return result;
    }
  }

}
