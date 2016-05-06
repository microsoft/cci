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
using System.Diagnostics.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// Implemented by classes that visit nodes of object graphs via a double dispatch mechanism, usually performing some computation of a subset of the nodes in the graph.
  /// Contains a specialized Visit routine for each standard type of object defined in the code model. 
  /// </summary>
  [ContractClass(typeof(ICodeVisitorContract))]
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
    /// Performs some computation with the given copy memory statement.
    /// </summary>
    void Visit(ICopyMemoryStatement copyMemoryStatement);
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
    /// Performs some computation with the given expression statement.
    /// </summary>
    void Visit(IExpressionStatement expressionStatement);
    /// <summary>
    /// Performs some computation with the given fill memory statement.
    /// </summary>
    void Visit(IFillMemoryStatement fillMemoryStatement);
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

  #region ICodeVisitor contract binding

  [ContractClassFor(typeof(ICodeVisitor))]
  abstract class ICodeVisitorContract : ICodeVisitor {
    #region ICodeVisitor Members

    public void Visit(IAddition addition) {
      Contract.Requires(addition != null);
      throw new NotImplementedException();
    }

    public void Visit(IAddressableExpression addressableExpression) {
      Contract.Requires(addressableExpression != null);
      throw new NotImplementedException();
    }

    public void Visit(IAddressDereference addressDereference) {
      Contract.Requires(addressDereference != null);
      throw new NotImplementedException();
    }

    public void Visit(IAddressOf addressOf) {
      Contract.Requires(addressOf != null);
      throw new NotImplementedException();
    }

    public void Visit(IAnonymousDelegate anonymousDelegate) {
      Contract.Requires(anonymousDelegate != null);
      throw new NotImplementedException();
    }

    public void Visit(IArrayIndexer arrayIndexer) {
      Contract.Requires(arrayIndexer != null);
      throw new NotImplementedException();
    }

    public void Visit(IAssertStatement assertStatement) {
      Contract.Requires(assertStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IAssignment assignment) {
      Contract.Requires(assignment != null);
      throw new NotImplementedException();
    }

    public void Visit(IAssumeStatement assumeStatement) {
      Contract.Requires(assumeStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IBitwiseAnd bitwiseAnd) {
      Contract.Requires(bitwiseAnd != null);
      throw new NotImplementedException();
    }

    public void Visit(IBitwiseOr bitwiseOr) {
      Contract.Requires(bitwiseOr != null);
      throw new NotImplementedException();
    }

    public void Visit(IBlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);
      throw new NotImplementedException();
    }

    public void Visit(IBlockStatement block) {
      Contract.Requires(block != null);
      throw new NotImplementedException();
    }

    public void Visit(IBreakStatement breakStatement) {
      Contract.Requires(breakStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IBoundExpression boundExpression) {
      Contract.Requires(boundExpression != null);
      throw new NotImplementedException();
    }

    public void Visit(ICastIfPossible castIfPossible) {
      Contract.Requires(castIfPossible != null);
      throw new NotImplementedException();
    }

    public void Visit(ICatchClause catchClause) {
      Contract.Requires(catchClause != null);
      throw new NotImplementedException();
    }

    public void Visit(ICheckIfInstance checkIfInstance) {
      Contract.Requires(checkIfInstance != null);
      throw new NotImplementedException();
    }

    public void Visit(ICompileTimeConstant constant) {
      Contract.Requires(constant != null);
      throw new NotImplementedException();
    }

    public void Visit(IConversion conversion) {
      Contract.Requires(conversion != null);
      throw new NotImplementedException();
    }

    public void Visit(IConditional conditional) {
      Contract.Requires(conditional != null);
      throw new NotImplementedException();
    }

    public void Visit(IConditionalStatement conditionalStatement) {
      Contract.Requires(conditionalStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IContinueStatement continueStatement) {
      Contract.Requires(continueStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ICopyMemoryStatement copyMemoryStatement) {
      Contract.Requires(copyMemoryStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ICreateArray createArray) {
      Contract.Requires(createArray != null);
      throw new NotImplementedException();
    }

    public void Visit(ICreateDelegateInstance createDelegateInstance) {
      Contract.Requires(createDelegateInstance != null);
      throw new NotImplementedException();
    }

    public void Visit(ICreateObjectInstance createObjectInstance) {
      Contract.Requires(createObjectInstance != null);
      throw new NotImplementedException();
    }

    public void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
      Contract.Requires(debuggerBreakStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IDefaultValue defaultValue) {
      Contract.Requires(defaultValue != null);
      throw new NotImplementedException();
    }

    public void Visit(IDivision division) {
      Contract.Requires(division != null);
      throw new NotImplementedException();
    }

    public void Visit(IDoUntilStatement doUntilStatement) {
      Contract.Requires(doUntilStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IDupValue dupValue) {
      Contract.Requires(dupValue != null);
      throw new NotImplementedException();
    }

    public void Visit(IEmptyStatement emptyStatement) {
      Contract.Requires(emptyStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IEquality equality) {
      Contract.Requires(equality != null);
      throw new NotImplementedException();
    }

    public void Visit(IExclusiveOr exclusiveOr) {
      Contract.Requires(exclusiveOr != null);
      throw new NotImplementedException();
    }

    public void Visit(IExpressionStatement expressionStatement) {
      Contract.Requires(expressionStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IFillMemoryStatement fillMemoryStatement) {
      Contract.Requires(fillMemoryStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IForEachStatement forEachStatement) {
      Contract.Requires(forEachStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IForStatement forStatement) {
      Contract.Requires(forStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IGotoStatement gotoStatement) {
      Contract.Requires(gotoStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      Contract.Requires(gotoSwitchCaseStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
      Contract.Requires(getTypeOfTypedReference != null);
      throw new NotImplementedException();
    }

    public void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
      Contract.Requires(getValueOfTypedReference != null);
      throw new NotImplementedException();
    }

    public void Visit(IGreaterThan greaterThan) {
      Contract.Requires(greaterThan != null);
      throw new NotImplementedException();
    }

    public void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
      Contract.Requires(greaterThanOrEqual != null);
      throw new NotImplementedException();
    }

    public void Visit(ILabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ILeftShift leftShift) {
      Contract.Requires(leftShift != null);
      throw new NotImplementedException();
    }

    public void Visit(ILessThan lessThan) {
      Contract.Requires(lessThan != null);
      throw new NotImplementedException();
    }

    public void Visit(ILessThanOrEqual lessThanOrEqual) {
      Contract.Requires(lessThanOrEqual != null);
      throw new NotImplementedException();
    }

    public void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      Contract.Requires(localDeclarationStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ILockStatement lockStatement) {
      Contract.Requires(lockStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ILogicalNot logicalNot) {
      Contract.Requires(logicalNot != null);
      throw new NotImplementedException();
    }

    public void Visit(IMakeTypedReference makeTypedReference) {
      Contract.Requires(makeTypedReference != null);
      throw new NotImplementedException();
    }

    public void Visit(IMethodCall methodCall) {
      Contract.Requires(methodCall != null);
      throw new NotImplementedException();
    }

    public void Visit(IModulus modulus) {
      Contract.Requires(modulus != null);
      throw new NotImplementedException();
    }

    public void Visit(IMultiplication multiplication) {
      Contract.Requires(multiplication != null);
      throw new NotImplementedException();
    }

    public void Visit(INamedArgument namedArgument) {
      Contract.Requires(namedArgument != null);
      throw new NotImplementedException();
    }

    public void Visit(INotEquality notEquality) {
      Contract.Requires(notEquality != null);
      throw new NotImplementedException();
    }

    public void Visit(IOldValue oldValue) {
      Contract.Requires(oldValue != null);
      throw new NotImplementedException();
    }

    public void Visit(IOnesComplement onesComplement) {
      Contract.Requires(onesComplement != null);
      throw new NotImplementedException();
    }

    public void Visit(IOutArgument outArgument) {
      Contract.Requires(outArgument != null);
      throw new NotImplementedException();
    }

    public void Visit(IPointerCall pointerCall) {
      Contract.Requires(pointerCall != null);
      throw new NotImplementedException();
    }

    public void Visit(IPopValue popValue) {
      Contract.Requires(popValue != null);
      throw new NotImplementedException();
    }

    public void Visit(IPushStatement pushStatement) {
      Contract.Requires(pushStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IRefArgument refArgument) {
      Contract.Requires(refArgument != null);
      throw new NotImplementedException();
    }

    public void Visit(IResourceUseStatement resourceUseStatement) {
      Contract.Requires(resourceUseStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IReturnValue returnValue) {
      Contract.Requires(returnValue != null);
      throw new NotImplementedException();
    }

    public void Visit(IRethrowStatement rethrowStatement) {
      Contract.Requires(rethrowStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IReturnStatement returnStatement) {
      Contract.Requires(returnStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IRightShift rightShift) {
      Contract.Requires(rightShift != null);
      throw new NotImplementedException();
    }

    public void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      Contract.Requires(runtimeArgumentHandleExpression != null);
      throw new NotImplementedException();
    }

    public void Visit(ISizeOf sizeOf) {
      Contract.Requires(sizeOf != null);
      throw new NotImplementedException();
    }

    public void Visit(IStackArrayCreate stackArrayCreate) {
      Contract.Requires(stackArrayCreate != null);
      throw new NotImplementedException();
    }

    public void Visit(ISubtraction subtraction) {
      Contract.Requires(subtraction != null);
      throw new NotImplementedException();
    }

    public void Visit(ISwitchCase switchCase) {
      Contract.Requires(switchCase != null);
      throw new NotImplementedException();
    }

    public void Visit(ISwitchStatement switchStatement) {
      Contract.Requires(switchStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ITargetExpression targetExpression) {
      Contract.Requires(targetExpression != null);
      throw new NotImplementedException();
    }

    public void Visit(IThisReference thisReference) {
      Contract.Requires(thisReference != null);
      throw new NotImplementedException();
    }

    public void Visit(IThrowStatement throwStatement) {
      Contract.Requires(throwStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      Contract.Requires(tryCatchFilterFinallyStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(ITokenOf tokenOf) {
      Contract.Requires(tokenOf != null);
      throw new NotImplementedException();
    }

    public void Visit(ITypeOf typeOf) {
      Contract.Requires(typeOf != null);
      throw new NotImplementedException();
    }

    public void Visit(IUnaryNegation unaryNegation) {
      Contract.Requires(unaryNegation != null);
      throw new NotImplementedException();
    }

    public void Visit(IUnaryPlus unaryPlus) {
      Contract.Requires(unaryPlus != null);
      throw new NotImplementedException();
    }

    public void Visit(IVectorLength vectorLength) {
      Contract.Requires(vectorLength != null);
      throw new NotImplementedException();
    }

    public void Visit(IWhileDoStatement whileDoStatement) {
      Contract.Requires(whileDoStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IYieldBreakStatement yieldBreakStatement) {
      Contract.Requires(yieldBreakStatement != null);
      throw new NotImplementedException();
    }

    public void Visit(IYieldReturnStatement yieldReturnStatement) {
      Contract.Requires(yieldReturnStatement != null);
      throw new NotImplementedException();
    }

    #endregion

    #region IMetadataVisitor Members

    public void Visit(IArrayTypeReference arrayTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IAssembly assembly) {
      throw new NotImplementedException();
    }

    public void Visit(IAssemblyReference assemblyReference) {
      throw new NotImplementedException();
    }

    public void Visit(ICustomAttribute customAttribute) {
      throw new NotImplementedException();
    }

    public void Visit(ICustomModifier customModifier) {
      throw new NotImplementedException();
    }

    public void Visit(IEventDefinition eventDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IFieldDefinition fieldDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IFieldReference fieldReference) {
      throw new NotImplementedException();
    }

    public void Visit(IFileReference fileReference) {
      throw new NotImplementedException();
    }

    public void Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericMethodInstanceReference genericMethodInstanceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericMethodParameter genericMethodParameter) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGlobalFieldDefinition globalFieldDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IGlobalMethodDefinition globalMethodDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericTypeParameter genericTypeParameter) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      throw new NotImplementedException();
    }

    public void Visit(ILocalDefinition localDefinition) {
      throw new NotImplementedException();
    }

    public void VisitReference(ILocalDefinition localDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IManagedPointerTypeReference managedPointerTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IMarshallingInformation marshallingInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataConstant constant) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataCreateArray createArray) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataExpression expression) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataNamedArgument namedArgument) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataTypeOf typeOf) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodBody methodBody) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodDefinition method) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodImplementation methodImplementation) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodReference methodReference) {
      throw new NotImplementedException();
    }

    public void Visit(IModifiedTypeReference modifiedTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IModule module) {
      throw new NotImplementedException();
    }

    public void Visit(IModuleReference moduleReference) {
      throw new NotImplementedException();
    }

    public void Visit(INamespaceAliasForType namespaceAliasForType) {
      throw new NotImplementedException();
    }

    public void Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(INamespaceTypeReference namespaceTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(INestedAliasForType nestedAliasForType) {
      throw new NotImplementedException();
    }

    public void Visit(INestedTypeDefinition nestedTypeDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(INestedTypeReference nestedTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(INestedUnitNamespace nestedUnitNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      throw new NotImplementedException();
    }

    public void Visit(INestedUnitSetNamespace nestedUnitSetNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(IOperation operation) {
      throw new NotImplementedException();
    }

    public void Visit(IOperationExceptionInformation operationExceptionInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IParameterDefinition parameterDefinition) {
      throw new NotImplementedException();
    }

    public void VisitReference(IParameterDefinition parameterDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IParameterTypeInformation parameterTypeInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IPESection peSection) {
      throw new NotImplementedException();
    }

    public void Visit(IPlatformInvokeInformation platformInvokeInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IPointerTypeReference pointerTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IPropertyDefinition propertyDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IResourceReference resourceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IRootUnitNamespace rootUnitNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IRootUnitSetNamespace rootUnitSetNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(ISecurityAttribute securityAttribute) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedEventDefinition specializedEventDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedFieldDefinition specializedFieldDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedFieldReference specializedFieldReference) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedMethodDefinition specializedMethodDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedMethodReference specializedMethodReference) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedPropertyDefinition specializedPropertyDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedNestedTypeDefinition specializedNestedTypeDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedNestedTypeReference specializedNestedTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IUnitSet unitSet) {
      throw new NotImplementedException();
    }

    public void Visit(IWin32Resource win32Resource) {
      throw new NotImplementedException();
    }

    #endregion
  }
  #endregion

  /// <summary>
  /// Contains a specialized Visit routine for each standard type of object defined in the code and metadata model. 
  /// </summary>
  public class CodeVisitor : MetadataVisitor, ICodeVisitor {

    /// <summary>
    /// Contains a specialized Visit routine for each standard type of object defined in the code and metadata model. 
    /// </summary>
    public CodeVisitor() {
    }

    /// <summary>
    /// Performs some computation with the given addition.
    /// </summary>
    /// <param name="addition"></param>
    public virtual void Visit(IAddition addition) {
      this.Visit((IBinaryOperation)addition);
    }

    /// <summary>
    /// Performs some computation with the given addressable expression.
    /// </summary>
    /// <param name="addressableExpression"></param>
    public virtual void Visit(IAddressableExpression addressableExpression) {
      this.Visit((IExpression)addressableExpression);
    }

    /// <summary>
    /// Performs some computation with the given address dereference expression.
    /// </summary>
    /// <param name="addressDereference"></param>
    public virtual void Visit(IAddressDereference addressDereference) {
      this.Visit((IExpression)addressDereference);
    }

    /// <summary>
    /// Performs some computation with the given AddressOf expression.
    /// </summary>
    /// <param name="addressOf"></param>
    public virtual void Visit(IAddressOf addressOf) {
      this.Visit((IExpression)addressOf);
    }

    /// <summary>
    /// Performs some computation with the given anonymous delegate expression.
    /// </summary>
    /// <param name="anonymousDelegate"></param>
    public virtual void Visit(IAnonymousDelegate anonymousDelegate) {
      this.Visit((IExpression)anonymousDelegate);
    }

    /// <summary>
    /// Performs some computation with the given array indexer expression.
    /// </summary>
    /// <param name="arrayIndexer"></param>
    public virtual void Visit(IArrayIndexer arrayIndexer) {
      this.Visit((IExpression)arrayIndexer);
    }

    /// <summary>
    /// Performs some computation with the given assert statement.
    /// </summary>
    /// <param name="assertStatement"></param>
    public virtual void Visit(IAssertStatement assertStatement) {
      this.Visit((IStatement)assertStatement);
    }

    /// <summary>
    /// Performs some computation with the given assignment expression.
    /// </summary>
    /// <param name="assignment"></param>
    public virtual void Visit(IAssignment assignment) {
      this.Visit((IExpression)assignment);
    }

    /// <summary>
    /// Performs some computation with the given assume statement.
    /// </summary>
    /// <param name="assumeStatement"></param>
    public virtual void Visit(IAssumeStatement assumeStatement) {
      this.Visit((IStatement)assumeStatement);
    }

    /// <summary>
    /// Performs some computation with the given bitwise and expression.
    /// </summary>
    /// <param name="bitwiseAnd"></param>
    public virtual void Visit(IBitwiseAnd bitwiseAnd) {
      this.Visit((IBinaryOperation)bitwiseAnd);
    }

    /// <summary>
    /// Performs some computation with the given bitwise and expression.
    /// </summary>
    /// <param name="binaryOperation"></param>
    public virtual void Visit(IBinaryOperation binaryOperation) {
      this.Visit((IExpression)binaryOperation);
    }

    /// <summary>
    /// Performs some computation with the given bitwise or expression.
    /// </summary>
    /// <param name="bitwiseOr"></param>
    public virtual void Visit(IBitwiseOr bitwiseOr) {
      this.Visit((IBinaryOperation)bitwiseOr);
    }

    /// <summary>
    /// Performs some computation with the given block expression.
    /// </summary>
    /// <param name="blockExpression"></param>
    public virtual void Visit(IBlockExpression blockExpression) {
      this.Visit((IExpression)blockExpression);
    }

    /// <summary>
    /// Performs some computation with the given statement block.
    /// </summary>
    /// <param name="block"></param>
    public virtual void Visit(IBlockStatement block) {
      this.Visit((IStatement)block);
    }

    /// <summary>
    /// Performs some computation with the given break statement.
    /// </summary>
    /// <param name="breakStatement"></param>
    public virtual void Visit(IBreakStatement breakStatement) {
      this.Visit((IStatement)breakStatement);
    }

    /// <summary>
    /// Performs some computation with the cast-if-possible expression.
    /// </summary>
    /// <param name="castIfPossible"></param>
    public virtual void Visit(ICastIfPossible castIfPossible) {
      this.Visit((IExpression)castIfPossible);
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
      this.Visit((IExpression)checkIfInstance);
    }

    /// <summary>
    /// Performs some computation with the given compile time constant.
    /// </summary>
    /// <param name="constant"></param>
    public virtual void Visit(ICompileTimeConstant constant) {
      this.Visit((IExpression)constant);
    }

    /// <summary>
    /// Performs some computation with the given conversion expression.
    /// </summary>
    /// <param name="conversion"></param>
    public virtual void Visit(IConversion conversion) {
      this.Visit((IExpression)conversion);
    }

    /// <summary>
    /// Performs some computation with the given conditional expression.
    /// </summary>
    /// <param name="conditional"></param>
    public virtual void Visit(IConditional conditional) {
      this.Visit((IExpression)conditional);
    }

    /// <summary>
    /// Performs some computation with the given conditional statement.
    /// </summary>
    /// <param name="conditionalStatement"></param>
    public virtual void Visit(IConditionalStatement conditionalStatement) {
      this.Visit((IStatement)conditionalStatement);
    }

    /// <summary>
    /// Performs some computation with the given continue statement.
    /// </summary>
    /// <param name="continueStatement"></param>
    public virtual void Visit(IContinueStatement continueStatement) {
      this.Visit((IStatement)continueStatement);
    }

    /// <summary>
    /// Performs some computation with the given copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public virtual void Visit(ICopyMemoryStatement copyMemoryStatement) {
      this.Visit((IStatement)copyMemoryStatement);
    }

    /// <summary>
    /// Performs some computation with the given array creation expression.
    /// </summary>
    /// <param name="createArray"></param>
    public virtual void Visit(ICreateArray createArray) {
      this.Visit((IExpression)createArray);
    }

    /// <summary>
    /// Performs some computation with the given constructor call expression.
    /// </summary>
    /// <param name="createObjectInstance"></param>
    public virtual void Visit(ICreateObjectInstance createObjectInstance) {
      this.Visit((IExpression)createObjectInstance);
    }

    /// <summary>
    /// Performs some computation with the anonymous object creation expression.
    /// </summary>
    /// <param name="createDelegateInstance"></param>
    public virtual void Visit(ICreateDelegateInstance createDelegateInstance) {
      this.Visit((IExpression)createDelegateInstance);
    }

    /// <summary>
    /// Performs some computation with the given defalut value expression.
    /// </summary>
    /// <param name="defaultValue"></param>
    public virtual void Visit(IDefaultValue defaultValue) {
      this.Visit((IExpression)defaultValue);
    }

    /// <summary>
    /// Performs some computation with the given division expression.
    /// </summary>
    /// <param name="division"></param>
    public virtual void Visit(IDivision division) {
      this.Visit((IBinaryOperation)division);
    }

    /// <summary>
    /// Performs some computation with the given do until statement.
    /// </summary>
    /// <param name="doUntilStatement"></param>
    public virtual void Visit(IDoUntilStatement doUntilStatement) {
      this.Visit((IStatement)doUntilStatement);
    }

    /// <summary>
    /// Performs some computation with the given dup value expression.
    /// </summary>
    /// <param name="dupValue"></param>
    public virtual void Visit(IDupValue dupValue) {
      this.Visit((IExpression)dupValue);
    }

    /// <summary>
    /// Performs some computation with the given empty statement.
    /// </summary>
    /// <param name="emptyStatement"></param>
    public virtual void Visit(IEmptyStatement emptyStatement) {
      this.Visit((IStatement)emptyStatement);
    }

    /// <summary>
    /// Performs some computation with the given equality expression.
    /// </summary>
    /// <param name="equality"></param>
    public virtual void Visit(IEquality equality) {
      this.Visit((IBinaryOperation)equality);
    }

    /// <summary>
    /// Performs some computation with the given exclusive or expression.
    /// </summary>
    /// <param name="exclusiveOr"></param>
    public virtual void Visit(IExclusiveOr exclusiveOr) {
      this.Visit((IBinaryOperation)exclusiveOr);
    }

    /// <summary>
    /// Performs some computation with the given bound expression.
    /// </summary>
    /// <param name="boundExpression"></param>
    public virtual void Visit(IBoundExpression boundExpression) {
      this.Visit((IExpression)boundExpression);
    }

    /// <summary>
    /// Performs some computation with the given debugger break statement.
    /// </summary>
    /// <param name="debuggerBreakStatement"></param>
    public virtual void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
      this.Visit((IStatement)debuggerBreakStatement);
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
      this.Visit((IStatement)expressionStatement);
    }

    /// <summary>
    /// Performs some computation with the given fill memory statement.
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    public virtual void Visit(IFillMemoryStatement fillMemoryStatement) {
      this.Visit((IStatement)fillMemoryStatement);
    }

    /// <summary>
    /// Performs some computation with the given foreach statement.
    /// </summary>
    /// <param name="forEachStatement"></param>
    public virtual void Visit(IForEachStatement forEachStatement) {
      this.Visit((IStatement)forEachStatement);
    }

    /// <summary>
    /// Performs some computation with the given for statement.
    /// </summary>
    /// <param name="forStatement"></param>
    public virtual void Visit(IForStatement forStatement) {
      this.Visit((IStatement)forStatement);
    }

    /// <summary>
    /// Performs some computation with the given get type of typed reference expression.
    /// </summary>
    /// <param name="getTypeOfTypedReference"></param>
    public virtual void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
      this.Visit((IExpression)getTypeOfTypedReference);
    }

    /// <summary>
    /// Performs some computation with the given get value of typed reference expression.
    /// </summary>
    /// <param name="getValueOfTypedReference"></param>
    public virtual void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
      this.Visit((IExpression)getValueOfTypedReference);
    }

    /// <summary>
    /// Performs some computation with the given goto statement.
    /// </summary>
    /// <param name="gotoStatement"></param>
    public virtual void Visit(IGotoStatement gotoStatement) {
      this.Visit((IStatement)gotoStatement);
    }

    /// <summary>
    /// Performs some computation with the given goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public virtual void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      this.Visit((IStatement)gotoSwitchCaseStatement);
    }

    /// <summary>
    /// Performs some computation with the given greater-than expression.
    /// </summary>
    /// <param name="greaterThan"></param>
    public virtual void Visit(IGreaterThan greaterThan) {
      this.Visit((IBinaryOperation)greaterThan);
    }

    /// <summary>
    /// Performs some computation with the given greater-than-or-equal expression.
    /// </summary>
    /// <param name="greaterThanOrEqual"></param>
    public virtual void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
      this.Visit((IBinaryOperation)greaterThanOrEqual);
    }

    /// <summary>
    /// Performs some computation with the given labeled statement.
    /// </summary>
    /// <param name="labeledStatement"></param>
    public virtual void Visit(ILabeledStatement labeledStatement) {
      this.Visit((IStatement)labeledStatement);
    }

    /// <summary>
    /// Performs some computation with the given left shift expression.
    /// </summary>
    /// <param name="leftShift"></param>
    public virtual void Visit(ILeftShift leftShift) {
      this.Visit((IBinaryOperation)leftShift);
    }

    /// <summary>
    /// Performs some computation with the given less-than expression.
    /// </summary>
    /// <param name="lessThan"></param>
    public virtual void Visit(ILessThan lessThan) {
      this.Visit((IBinaryOperation)lessThan);
    }

    /// <summary>
    /// Performs some computation with the given less-than-or-equal expression.
    /// </summary>
    /// <param name="lessThanOrEqual"></param>
    public virtual void Visit(ILessThanOrEqual lessThanOrEqual) {
      this.Visit((IBinaryOperation)lessThanOrEqual);
    }

    /// <summary>
    /// Performs some computation with the given local declaration statement.
    /// </summary>
    /// <param name="localDeclarationStatement"></param>
    public virtual void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      this.Visit((IStatement)localDeclarationStatement);
    }

    /// <summary>
    /// Performs some computation with the given lock statement.
    /// </summary>
    /// <param name="lockStatement"></param>
    public virtual void Visit(ILockStatement lockStatement) {
      this.Visit((IStatement)lockStatement);
    }

    /// <summary>
    /// Performs some computation with the given logical not expression.
    /// </summary>
    /// <param name="logicalNot"></param>
    public virtual void Visit(ILogicalNot logicalNot) {
      this.Visit((IUnaryOperation)logicalNot);
    }

    /// <summary>
    /// Performs some computation with the given make typed reference expression.
    /// </summary>
    /// <param name="makeTypedReference"></param>
    public virtual void Visit(IMakeTypedReference makeTypedReference) {
      this.Visit((IExpression)makeTypedReference);
    }

    /// <summary>
    /// Performs some computation with the given method call.
    /// </summary>
    /// <param name="methodCall"></param>
    public virtual void Visit(IMethodCall methodCall) {
      this.Visit((IExpression)methodCall);
    }

    /// <summary>
    /// Performs some computation with the given modulus expression.
    /// </summary>
    /// <param name="modulus"></param>
    public virtual void Visit(IModulus modulus) {
      this.Visit((IBinaryOperation)modulus);
    }

    /// <summary>
    /// Performs some computation with the given multiplication expression.
    /// </summary>
    /// <param name="multiplication"></param>
    public virtual void Visit(IMultiplication multiplication) {
      this.Visit((IBinaryOperation)multiplication);
    }

    /// <summary>
    /// Performs some computation with the given named argument expression.
    /// </summary>
    /// <param name="namedArgument"></param>
    public virtual void Visit(INamedArgument namedArgument) {
      this.Visit((IExpression)namedArgument);
    }

    /// <summary>
    /// Performs some computation with the given not equality expression.
    /// </summary>
    /// <param name="notEquality"></param>
    public virtual void Visit(INotEquality notEquality) {
      this.Visit((IBinaryOperation)notEquality);
    }

    /// <summary>
    /// Performs some computation with the given old value expression.
    /// </summary>
    /// <param name="oldValue"></param>
    public virtual void Visit(IOldValue oldValue) {
      this.Visit((IExpression)oldValue);
    }

    /// <summary>
    /// Performs some computation with the given one's complement expression.
    /// </summary>
    /// <param name="onesComplement"></param>
    public virtual void Visit(IOnesComplement onesComplement) {
      this.Visit((IUnaryOperation)onesComplement);
    }

    /// <summary>
    /// Performs some computation with the given out argument expression.
    /// </summary>
    /// <param name="outArgument"></param>
    public virtual void Visit(IOutArgument outArgument) {
      this.Visit((IExpression)outArgument);
    }

    /// <summary>
    /// Performs some computation with the given pointer call.
    /// </summary>
    /// <param name="pointerCall"></param>
    public virtual void Visit(IPointerCall pointerCall) {
      this.Visit((IExpression)pointerCall);
    }

    /// <summary>
    /// Performs some computation with the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public virtual void Visit(IPopValue popValue) {
      this.Visit((IExpression)popValue);
    }

    /// <summary>
    /// Performs some computation with the given push statement.
    /// </summary>
    /// <param name="pushStatement"></param>
    public virtual void Visit(IPushStatement pushStatement) {
      this.Visit((IStatement)pushStatement);
    }

    /// <summary>
    /// Performs some computation with the given ref argument expression.
    /// </summary>
    /// <param name="refArgument"></param>
    public virtual void Visit(IRefArgument refArgument) {
      this.Visit((IExpression)refArgument);
    }

    /// <summary>
    /// Performs some computation with the given resource usage statement.
    /// </summary>
    /// <param name="resourceUseStatement"></param>
    public virtual void Visit(IResourceUseStatement resourceUseStatement) {
      this.Visit((IStatement)resourceUseStatement);
    }

    /// <summary>
    /// Performs some computation with the rethrow statement.
    /// </summary>
    /// <param name="rethrowStatement"></param>
    public virtual void Visit(IRethrowStatement rethrowStatement) {
      this.Visit((IStatement)rethrowStatement);
    }

    /// <summary>
    /// Performs some computation with the return statement.
    /// </summary>
    /// <param name="returnStatement"></param>
    public virtual void Visit(IReturnStatement returnStatement) {
      this.Visit((IStatement)returnStatement);
    }

    /// <summary>
    /// Performs some computation with the given return value expression.
    /// </summary>
    /// <param name="returnValue"></param>
    public virtual void Visit(IReturnValue returnValue) {
      this.Visit((IExpression)returnValue);
    }

    /// <summary>
    /// Performs some computation with the given right shift expression.
    /// </summary>
    /// <param name="rightShift"></param>
    public virtual void Visit(IRightShift rightShift) {
      this.Visit((IBinaryOperation)rightShift);
    }

    /// <summary>
    /// Performs some computation with the given stack array create expression.
    /// </summary>
    /// <param name="stackArrayCreate"></param>
    public virtual void Visit(IStackArrayCreate stackArrayCreate) {
      this.Visit((IExpression)stackArrayCreate);
    }

    /// <summary>
    /// Performs some computation with the given runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression"></param>
    public virtual void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      this.Visit((IExpression)runtimeArgumentHandleExpression);
    }

    /// <summary>
    /// Performs some computation with the given sizeof() expression.
    /// </summary>
    /// <param name="sizeOf"></param>
    public virtual void Visit(ISizeOf sizeOf) {
      this.Visit((IExpression)sizeOf);
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
      this.Visit((IBinaryOperation)subtraction);
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
      this.Visit((IStatement)switchStatement);
    }

    /// <summary>
    /// Performs some computation with the given target expression.
    /// </summary>
    /// <param name="targetExpression"></param>
    public virtual void Visit(ITargetExpression targetExpression) {
      this.Visit((IExpression)targetExpression);
    }

    /// <summary>
    /// Performs some computation with the given this reference expression.
    /// </summary>
    /// <param name="thisReference"></param>
    public virtual void Visit(IThisReference thisReference) {
      this.Visit((IExpression)thisReference);
    }

    /// <summary>
    /// Performs some computation with the throw statement.
    /// </summary>
    /// <param name="throwStatement"></param>
    public virtual void Visit(IThrowStatement throwStatement) {
      this.Visit((IStatement)throwStatement);
    }

    /// <summary>
    /// Performs some computation with the try-catch-filter-finally statement.
    /// </summary>
    /// <param name="tryCatchFilterFinallyStatement"></param>
    public virtual void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      this.Visit((IStatement)tryCatchFilterFinallyStatement);
    }

    /// <summary>
    /// Performs some computation with the given tokenof() expression.
    /// </summary>
    /// <param name="tokenOf"></param>
    public virtual void Visit(ITokenOf tokenOf) {
      this.Visit((IExpression)tokenOf);
    }

    /// <summary>
    /// Performs some computation with the given typeof() expression.
    /// </summary>
    /// <param name="typeOf"></param>
    public virtual void Visit(ITypeOf typeOf) {
      this.Visit((IExpression)typeOf);
    }

    /// <summary>
    /// Performs some computation with the given unary negation expression.
    /// </summary>
    /// <param name="unaryNegation"></param>
    public virtual void Visit(IUnaryNegation unaryNegation) {
      this.Visit((IUnaryOperation)unaryNegation);
    }

    /// <summary>
    /// Performs some computation with the given unary operation expression.
    /// </summary>
    /// <param name="unaryOperation"></param>
    public virtual void Visit(IUnaryOperation unaryOperation) {
      this.Visit((IExpression)unaryOperation);
    }

    /// <summary>
    /// Performs some computation with the given unary plus expression.
    /// </summary>
    /// <param name="unaryPlus"></param>
    public virtual void Visit(IUnaryPlus unaryPlus) {
      this.Visit((IUnaryOperation)unaryPlus);
    }

    /// <summary>
    /// Performs some computation with the given vector length expression.
    /// </summary>
    /// <param name="vectorLength"></param>
    public virtual void Visit(IVectorLength vectorLength) {
      this.Visit((IExpression)vectorLength);
    }

    /// <summary>
    /// Performs some computation with the given while do statement.
    /// </summary>
    /// <param name="whileDoStatement"></param>
    public virtual void Visit(IWhileDoStatement whileDoStatement) {
      this.Visit((IStatement)whileDoStatement);
    }

    /// <summary>
    /// Performs some computation with the given yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public virtual void Visit(IYieldBreakStatement yieldBreakStatement) {
      this.Visit((IStatement)yieldBreakStatement);
    }

    /// <summary>
    /// Performs some computation with the given yield return statement.
    /// </summary>
    /// <param name="yieldReturnStatement"></param>
    public virtual void Visit(IYieldReturnStatement yieldReturnStatement) {
      this.Visit((IStatement)yieldReturnStatement);
    }

  }

  /// <summary>
  /// A class that traverses the code and metadata model in depth first, left to right order,
  /// calling visitors on each model instance in pre-order as well as post-order.
  /// </summary>
  public class CodeTraverser : MetadataTraverser {

    /// <summary>
    /// A class that traverses the code and metadata model in depth first, left to right order,
    /// calling visitors on each model instance in pre-order as well as post-order.
    /// </summary>
    public CodeTraverser() {
      this.dispatchingVisitor = new Dispatcher() { traverser = this };
    }

    ICodeVisitor/*?*/ preorderVisitor;
    ICodeVisitor/*?*/ postorderVisitor;

    /// <summary>
    /// A visitor that should be called on each object being traversed, before any of its children are traversed. May be null.
    /// </summary>
    public new ICodeVisitor/*?*/ PreorderVisitor {
      get { return this.preorderVisitor; }
      set {
        this.preorderVisitor = value;
        base.PreorderVisitor = value;
      }
    }

    /// <summary>
    /// A visitor that should be called on each object being traversed, after all of its children are traversed. May be null. 
    /// </summary>
    public new ICodeVisitor/*?*/ PostorderVisitor {
      get { return this.postorderVisitor; }
      set {
        this.postorderVisitor = value;
        base.PostorderVisitor = value;
      }
    }

    Dispatcher dispatchingVisitor;
    class Dispatcher : MetadataVisitor, ICodeVisitor {

      internal CodeTraverser traverser;

      public void Visit(IAddition addition) {
        this.traverser.Traverse(addition);
      }

      public void Visit(IAddressableExpression addressableExpression) {
        this.traverser.Traverse(addressableExpression);
      }

      public void Visit(IAddressDereference addressDereference) {
        this.traverser.Traverse(addressDereference);
      }

      public void Visit(IAddressOf addressOf) {
        this.traverser.Traverse(addressOf);
      }

      public void Visit(IAnonymousDelegate anonymousDelegate) {
        this.traverser.Traverse(anonymousDelegate);
      }

      public void Visit(IArrayIndexer arrayIndexer) {
        this.traverser.Traverse(arrayIndexer);
      }

      public void Visit(IAssertStatement assertStatement) {
        this.traverser.Traverse(assertStatement);
      }

      public void Visit(IAssignment assignment) {
        this.traverser.Traverse(assignment);
      }

      public void Visit(IAssumeStatement assumeStatement) {
        this.traverser.Traverse(assumeStatement);
      }

      public void Visit(IBitwiseAnd bitwiseAnd) {
        this.traverser.Traverse(bitwiseAnd);
      }

      public void Visit(IBitwiseOr bitwiseOr) {
        this.traverser.Traverse(bitwiseOr);
      }

      public void Visit(IBlockExpression blockExpression) {
        this.traverser.Traverse(blockExpression);
      }

      public void Visit(IBlockStatement block) {
        this.traverser.Traverse(block);
      }

      public void Visit(IBreakStatement breakStatement) {
        this.traverser.Traverse(breakStatement);
      }

      public void Visit(IBoundExpression boundExpression) {
        this.traverser.Traverse(boundExpression);
      }

      public void Visit(ICastIfPossible castIfPossible) {
        this.traverser.Traverse(castIfPossible);
      }

      public void Visit(ICatchClause catchClause) {
        this.traverser.Traverse(catchClause);
      }

      public void Visit(ICheckIfInstance checkIfInstance) {
        this.traverser.Traverse(checkIfInstance);
      }

      public void Visit(ICompileTimeConstant constant) {
        this.traverser.Traverse(constant);
      }

      public void Visit(IConversion conversion) {
        this.traverser.Traverse(conversion);
      }

      public void Visit(IConditional conditional) {
        this.traverser.Traverse(conditional);
      }

      public void Visit(IConditionalStatement conditionalStatement) {
        this.traverser.Traverse(conditionalStatement);
      }

      public void Visit(IContinueStatement continueStatement) {
        this.traverser.Traverse(continueStatement);
      }

      public void Visit(ICopyMemoryStatement copyMemoryStatement) {
        this.traverser.Traverse(copyMemoryStatement);
      }

      public void Visit(ICreateArray createArray) {
        this.traverser.Traverse(createArray);
      }

      public void Visit(ICreateDelegateInstance createDelegateInstance) {
        this.traverser.Traverse(createDelegateInstance);
      }

      public void Visit(ICreateObjectInstance createObjectInstance) {
        this.traverser.Traverse(createObjectInstance);
      }

      public void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
        this.traverser.Traverse(debuggerBreakStatement);
      }

      public void Visit(IDefaultValue defaultValue) {
        this.traverser.Traverse(defaultValue);
      }

      public void Visit(IDivision division) {
        this.traverser.Traverse(division);
      }

      public void Visit(IDoUntilStatement doUntilStatement) {
        this.traverser.Traverse(doUntilStatement);
      }

      public void Visit(IDupValue dupValue) {
        this.traverser.Traverse(dupValue);
      }

      public void Visit(IEmptyStatement emptyStatement) {
        this.traverser.Traverse(emptyStatement);
      }

      public void Visit(IEquality equality) {
        this.traverser.Traverse(equality);
      }

      public void Visit(IExclusiveOr exclusiveOr) {
        this.traverser.Traverse(exclusiveOr);
      }

      public void Visit(IExpressionStatement expressionStatement) {
        this.traverser.Traverse(expressionStatement);
      }

      public void Visit(IFillMemoryStatement fillMemoryStatement) {
        this.traverser.Traverse(fillMemoryStatement);
      }

      public void Visit(IForEachStatement forEachStatement) {
        this.traverser.Traverse(forEachStatement);
      }

      public void Visit(IForStatement forStatement) {
        this.traverser.Traverse(forStatement);
      }

      public void Visit(IGotoStatement gotoStatement) {
        this.traverser.Traverse(gotoStatement);
      }

      public void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
        this.traverser.Traverse(gotoSwitchCaseStatement);
      }

      public void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
        this.traverser.Traverse(getTypeOfTypedReference);
      }

      public void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
        this.traverser.Traverse(getValueOfTypedReference);
      }

      public void Visit(IGreaterThan greaterThan) {
        this.traverser.Traverse(greaterThan);
      }

      public void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
        this.traverser.Traverse(greaterThanOrEqual);
      }

      public void Visit(ILabeledStatement labeledStatement) {
        this.traverser.Traverse(labeledStatement);
      }

      public void Visit(ILeftShift leftShift) {
        this.traverser.Traverse(leftShift);
      }

      public void Visit(ILessThan lessThan) {
        this.traverser.Traverse(lessThan);
      }

      public void Visit(ILessThanOrEqual lessThanOrEqual) {
        this.traverser.Traverse(lessThanOrEqual);
      }

      public void Visit(ILocalDeclarationStatement localDeclarationStatement) {
        this.traverser.Traverse(localDeclarationStatement);
      }

      public void Visit(ILockStatement lockStatement) {
        this.traverser.Traverse(lockStatement);
      }

      public void Visit(ILogicalNot logicalNot) {
        this.traverser.Traverse(logicalNot);
      }

      public void Visit(IMakeTypedReference makeTypedReference) {
        this.traverser.Traverse(makeTypedReference);
      }

      public void Visit(IMethodCall methodCall) {
        this.traverser.Traverse(methodCall);
      }

      public void Visit(IModulus modulus) {
        this.traverser.Traverse(modulus);
      }

      public void Visit(IMultiplication multiplication) {
        this.traverser.Traverse(multiplication);
      }

      public void Visit(INamedArgument namedArgument) {
        this.traverser.Traverse(namedArgument);
      }

      public void Visit(INotEquality notEquality) {
        this.traverser.Traverse(notEquality);
      }

      public void Visit(IOldValue oldValue) {
        this.traverser.Traverse(oldValue);
      }

      public void Visit(IOnesComplement onesComplement) {
        this.traverser.Traverse(onesComplement);
      }

      public void Visit(IOutArgument outArgument) {
        this.traverser.Traverse(outArgument);
      }

      public void Visit(IPointerCall pointerCall) {
        this.traverser.Traverse(pointerCall);
      }

      public void Visit(IPopValue popValue) {
        this.traverser.Traverse(popValue);
      }

      public void Visit(IPushStatement pushStatement) {
        this.traverser.Traverse(pushStatement);
      }

      public void Visit(IRefArgument refArgument) {
        this.traverser.Traverse(refArgument);
      }

      public void Visit(IResourceUseStatement resourceUseStatement) {
        this.traverser.Traverse(resourceUseStatement);
      }

      public void Visit(IReturnValue returnValue) {
        this.traverser.Traverse(returnValue);
      }

      public void Visit(IRethrowStatement rethrowStatement) {
        this.traverser.Traverse(rethrowStatement);
      }

      public void Visit(IReturnStatement returnStatement) {
        this.traverser.Traverse(returnStatement);
      }

      public void Visit(IRightShift rightShift) {
        this.traverser.Traverse(rightShift);
      }

      public void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
        this.traverser.Traverse(runtimeArgumentHandleExpression);
      }

      public void Visit(ISizeOf sizeOf) {
        this.traverser.Traverse(sizeOf);
      }

      public void Visit(IStackArrayCreate stackArrayCreate) {
        this.traverser.Traverse(stackArrayCreate);
      }

      public void Visit(ISubtraction subtraction) {
        this.traverser.Traverse(subtraction);
      }

      public void Visit(ISwitchCase switchCase) {
        this.traverser.Traverse(switchCase);
      }

      public void Visit(ISwitchStatement switchStatement) {
        this.traverser.Traverse(switchStatement);
      }

      public void Visit(ITargetExpression targetExpression) {
        this.traverser.Traverse(targetExpression);
      }

      public void Visit(IThisReference thisReference) {
        this.traverser.Traverse(thisReference);
      }

      public void Visit(IThrowStatement throwStatement) {
        this.traverser.Traverse(throwStatement);
      }

      public void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
        this.traverser.Traverse(tryCatchFilterFinallyStatement);
      }

      public void Visit(ITokenOf tokenOf) {
        this.traverser.Traverse(tokenOf);
      }

      public void Visit(ITypeOf typeOf) {
        this.traverser.Traverse(typeOf);
      }

      public void Visit(IUnaryNegation unaryNegation) {
        this.traverser.Traverse(unaryNegation);
      }

      public void Visit(IUnaryPlus unaryPlus) {
        this.traverser.Traverse(unaryPlus);
      }

      public void Visit(IVectorLength vectorLength) {
        this.traverser.Traverse(vectorLength);
      }

      public void Visit(IWhileDoStatement whileDoStatement) {
        this.traverser.Traverse(whileDoStatement);
      }

      public void Visit(IYieldBreakStatement yieldBreakStatement) {
        this.traverser.Traverse(yieldBreakStatement);
      }

      public void Visit(IYieldReturnStatement yieldReturnStatement) {
        this.traverser.Traverse(yieldReturnStatement);
      }

    }

    /// <summary>
    /// Traverses the addition.
    /// </summary>
    public void Traverse(IAddition addition) {
      Contract.Requires(addition != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(addition);
      if (this.StopTraversal) return;
      this.TraverseChildren(addition);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(addition);
    }

    /// <summary>
    /// Traverses the addressable expression.
    /// </summary>
    public void Traverse(IAddressableExpression addressableExpression) {
      Contract.Requires(addressableExpression != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(addressableExpression);
      if (this.StopTraversal) return;
      this.TraverseChildren(addressableExpression);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(addressableExpression);
    }

    /// <summary>
    /// Traverses the address dereference expression.
    /// </summary>
    public void Traverse(IAddressDereference addressDereference) {
      Contract.Requires(addressDereference != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(addressDereference);
      if (this.StopTraversal) return;
      this.TraverseChildren(addressDereference);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(addressDereference);
    }

    /// <summary>
    /// Traverses the AddressOf expression.
    /// </summary>
    public void Traverse(IAddressOf addressOf) {
      Contract.Requires(addressOf != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(addressOf);
      if (this.StopTraversal) return;
      this.TraverseChildren(addressOf);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(addressOf);
    }

    /// <summary>
    /// Traverses the anonymous delegate expression.
    /// </summary>
    public void Traverse(IAnonymousDelegate anonymousDelegate) {
      Contract.Requires(anonymousDelegate != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(anonymousDelegate);
      if (this.StopTraversal) return;
      this.TraverseChildren(anonymousDelegate);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(anonymousDelegate);
    }

    /// <summary>
    /// Traverses the array indexer expression.
    /// </summary>
    public void Traverse(IArrayIndexer arrayIndexer) {
      Contract.Requires(arrayIndexer != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(arrayIndexer);
      if (this.StopTraversal) return;
      this.TraverseChildren(arrayIndexer);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(arrayIndexer);
    }

    /// <summary>
    /// Traverses the assert statement.
    /// </summary>
    public void Traverse(IAssertStatement assertStatement) {
      Contract.Requires(assertStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(assertStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(assertStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(assertStatement);
    }

    /// <summary>
    /// Traverses the assignment expression.
    /// </summary>
    public void Traverse(IAssignment assignment) {
      Contract.Requires(assignment != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(assignment);
      if (this.StopTraversal) return;
      this.TraverseChildren(assignment);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(assignment);
    }

    /// <summary>
    /// Traverses the assume statement.
    /// </summary>
    public void Traverse(IAssumeStatement assumeStatement) {
      Contract.Requires(assumeStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(assumeStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(assumeStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(assumeStatement);
    }

    /// <summary>
    /// Traverses the bitwise and expression.
    /// </summary>
    /// <param name="binaryOperation"></param>
    public void Traverse(IBinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      binaryOperation.Dispatch(this.dispatchingVisitor);
    }

    /// <summary>
    /// Traverses the bitwise and expression.
    /// </summary>
    public void Traverse(IBitwiseAnd bitwiseAnd) {
      Contract.Requires(bitwiseAnd != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(bitwiseAnd);
      if (this.StopTraversal) return;
      this.TraverseChildren(bitwiseAnd);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(bitwiseAnd);
    }

    /// <summary>
    /// Traverses the bitwise or expression.
    /// </summary>
    public void Traverse(IBitwiseOr bitwiseOr) {
      Contract.Requires(bitwiseOr != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(bitwiseOr);
      if (this.StopTraversal) return;
      this.TraverseChildren(bitwiseOr);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(bitwiseOr);
    }

    /// <summary>
    /// Traverses the block expression.
    /// </summary>
    public void Traverse(IBlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(blockExpression);
      if (this.StopTraversal) return;
      this.TraverseChildren(blockExpression);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(blockExpression);
    }

    /// <summary>
    /// Traverses the statement block.
    /// </summary>
    public void Traverse(IBlockStatement block) {
      Contract.Requires(block != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(block);
      if (this.StopTraversal) return;
      this.TraverseChildren(block);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(block);
    }

    /// <summary>
    /// Traverses the bound expression.
    /// </summary>
    public void Traverse(IBoundExpression boundExpression) {
      Contract.Requires(boundExpression != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(boundExpression);
      if (this.StopTraversal) return;
      this.TraverseChildren(boundExpression);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(boundExpression);
    }

    /// <summary>
    /// Traverses the break statement.
    /// </summary>
    public void Traverse(IBreakStatement breakStatement) {
      Contract.Requires(breakStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(breakStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(breakStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(breakStatement);
    }

    /// <summary>
    /// Traverses the cast-if-possible expression.
    /// </summary>
    public void Traverse(ICastIfPossible castIfPossible) {
      Contract.Requires(castIfPossible != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(castIfPossible);
      if (this.StopTraversal) return;
      this.TraverseChildren(castIfPossible);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(castIfPossible);
    }

    /// <summary>
    /// Traverses the catch clause.
    /// </summary>
    public void Traverse(ICatchClause catchClause) {
      Contract.Requires(catchClause != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(catchClause);
      if (this.StopTraversal) return;
      this.TraverseChildren(catchClause);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(catchClause);
    }

    /// <summary>
    /// Traverses the check-if-instance expression.
    /// </summary>
    public void Traverse(ICheckIfInstance checkIfInstance) {
      Contract.Requires(checkIfInstance != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(checkIfInstance);
      if (this.StopTraversal) return;
      this.TraverseChildren(checkIfInstance);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(checkIfInstance);
    }

    /// <summary>
    /// Traverses the compile time constant.
    /// </summary>
    public void Traverse(ICompileTimeConstant constant) {
      Contract.Requires(constant != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(constant);
      if (this.StopTraversal) return;
      this.TraverseChildren(constant);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(constant);
    }

    /// <summary>
    /// Traverses the conditional expression.
    /// </summary>
    public void Traverse(IConditional conditional) {
      Contract.Requires(conditional != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(conditional);
      if (this.StopTraversal) return;
      this.TraverseChildren(conditional);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(conditional);
    }

    /// <summary>
    /// Traverses the conditional statement.
    /// </summary>
    public void Traverse(IConditionalStatement conditionalStatement) {
      Contract.Requires(conditionalStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(conditionalStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(conditionalStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(conditionalStatement);
    }

    /// <summary>
    /// Traverses the continue statement.
    /// </summary>
    public void Traverse(IContinueStatement continueStatement) {
      Contract.Requires(continueStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(continueStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(continueStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(continueStatement);
    }

    /// <summary>
    /// Traverses the conversion expression.
    /// </summary>
    public void Traverse(IConversion conversion) {
      Contract.Requires(conversion != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(conversion);
      if (this.StopTraversal) return;
      this.TraverseChildren(conversion);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(conversion);
    }

    /// <summary>
    /// Traverses the copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public void Traverse(ICopyMemoryStatement copyMemoryStatement) {
      Contract.Requires(copyMemoryStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(copyMemoryStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(copyMemoryStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(copyMemoryStatement);
    }

    /// <summary>
    /// Traverses the array creation expression.
    /// </summary>
    public void Traverse(ICreateArray createArray) {
      Contract.Requires(createArray != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(createArray);
      if (this.StopTraversal) return;
      this.TraverseChildren(createArray);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(createArray);
    }

    /// <summary>
    /// Traverses the delegate creation expression.
    /// </summary>
    public void Traverse(ICreateDelegateInstance createDelegateInstance) {
      Contract.Requires(createDelegateInstance != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(createDelegateInstance);
      if (this.StopTraversal) return;
      this.TraverseChildren(createDelegateInstance);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(createDelegateInstance);
    }

    /// <summary>
    /// Traverses the create object instance expression.
    /// </summary>
    public void Traverse(ICreateObjectInstance createObjectInstance) {
      Contract.Requires(createObjectInstance != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(createObjectInstance);
      if (this.StopTraversal) return;
      this.TraverseChildren(createObjectInstance);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(createObjectInstance);
    }

    /// <summary>
    /// Traverses the debugger break statement.
    /// </summary>
    public void Traverse(IDebuggerBreakStatement debuggerBreakStatement) {
      Contract.Requires(debuggerBreakStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(debuggerBreakStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(debuggerBreakStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(debuggerBreakStatement);
    }

    /// <summary>
    /// Traverses the defalut value expression.
    /// </summary>
    public void Traverse(IDefaultValue defaultValue) {
      Contract.Requires(defaultValue != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(defaultValue);
      if (this.StopTraversal) return;
      this.TraverseChildren(defaultValue);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(defaultValue);
    }

    /// <summary>
    /// Traverses the division expression.
    /// </summary>
    public void Traverse(IDivision division) {
      Contract.Requires(division != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(division);
      if (this.StopTraversal) return;
      this.TraverseChildren(division);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(division);
    }

    /// <summary>
    /// Traverses the do until statement.
    /// </summary>
    public void Traverse(IDoUntilStatement doUntilStatement) {
      Contract.Requires(doUntilStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(doUntilStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(doUntilStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(doUntilStatement);
    }

    /// <summary>
    /// Traverses the dup value expression.
    /// </summary>
    public void Traverse(IDupValue dupValue) {
      Contract.Requires(dupValue != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(dupValue);
      if (this.StopTraversal) return;
      this.TraverseChildren(dupValue);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(dupValue);
    }

    /// <summary>
    /// Traverses the empty statement.
    /// </summary>
    public void Traverse(IEmptyStatement emptyStatement) {
      Contract.Requires(emptyStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(emptyStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(emptyStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(emptyStatement);
    }

    /// <summary>
    /// Traverses the equality expression.
    /// </summary>
    public void Traverse(IEquality equality) {
      Contract.Requires(equality != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(equality);
      if (this.StopTraversal) return;
      this.TraverseChildren(equality);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(equality);
    }

    /// <summary>
    /// Traverses the exclusive or expression.
    /// </summary>
    public void Traverse(IExclusiveOr exclusiveOr) {
      Contract.Requires(exclusiveOr != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(exclusiveOr);
      if (this.StopTraversal) return;
      this.TraverseChildren(exclusiveOr);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(exclusiveOr);
    }

    /// <summary>
    /// Traverses the expression.
    /// </summary>
    public void Traverse(IExpression expression) {
      Contract.Requires(expression != null);
      expression.Dispatch(this.dispatchingVisitor);
    }

    /// <summary>
    /// Traverses the expression statement.
    /// </summary>
    public void Traverse(IExpressionStatement expressionStatement) {
      Contract.Requires(expressionStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(expressionStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(expressionStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(expressionStatement);
    }

    /// <summary>
    /// Traverses the fill memory statement.
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    public void Traverse(IFillMemoryStatement fillMemoryStatement) {
      Contract.Requires(fillMemoryStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(fillMemoryStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(fillMemoryStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(fillMemoryStatement);
    }

    /// <summary>
    /// Traverses the foreach statement.
    /// </summary>
    public void Traverse(IForEachStatement forEachStatement) {
      Contract.Requires(forEachStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(forEachStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(forEachStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(forEachStatement);
    }

    /// <summary>
    /// Traverses the for statement.
    /// </summary>
    public void Traverse(IForStatement forStatement) {
      Contract.Requires(forStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(forStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(forStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(forStatement);
    }

    /// <summary>
    /// Traverses the get type of typed reference expression.
    /// </summary>
    public void Traverse(IGetTypeOfTypedReference getTypeOfTypedReference) {
      Contract.Requires(getTypeOfTypedReference != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(getTypeOfTypedReference);
      if (this.StopTraversal) return;
      this.TraverseChildren(getTypeOfTypedReference);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(getTypeOfTypedReference);
    }

    /// <summary>
    /// Traverses the get value of typed reference expression.
    /// </summary>
    public void Traverse(IGetValueOfTypedReference getValueOfTypedReference) {
      Contract.Requires(getValueOfTypedReference != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(getValueOfTypedReference);
      if (this.StopTraversal) return;
      this.TraverseChildren(getValueOfTypedReference);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(getValueOfTypedReference);
    }

    /// <summary>
    /// Traverses the goto statement.
    /// </summary>
    public void Traverse(IGotoStatement gotoStatement) {
      Contract.Requires(gotoStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(gotoStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(gotoStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(gotoStatement);
    }

    /// <summary>
    /// Traverses the goto switch case statement.
    /// </summary>
    public void Traverse(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      Contract.Requires(gotoSwitchCaseStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(gotoSwitchCaseStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(gotoSwitchCaseStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(gotoSwitchCaseStatement);
    }

    /// <summary>
    /// Traverses the greater-than expression.
    /// </summary>
    public void Traverse(IGreaterThan greaterThan) {
      Contract.Requires(greaterThan != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(greaterThan);
      if (this.StopTraversal) return;
      this.TraverseChildren(greaterThan);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(greaterThan);
    }

    /// <summary>
    /// Traverses the greater-than-or-equal expression.
    /// </summary>
    public void Traverse(IGreaterThanOrEqual greaterThanOrEqual) {
      Contract.Requires(greaterThanOrEqual != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(greaterThanOrEqual);
      if (this.StopTraversal) return;
      this.TraverseChildren(greaterThanOrEqual);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(greaterThanOrEqual);
    }

    /// <summary>
    /// Traverses the labeled statement.
    /// </summary>
    public void Traverse(ILabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(labeledStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(labeledStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(labeledStatement);
    }

    /// <summary>
    /// Traverses the left shift expression.
    /// </summary>
    public void Traverse(ILeftShift leftShift) {
      Contract.Requires(leftShift != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(leftShift);
      if (this.StopTraversal) return;
      this.TraverseChildren(leftShift);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(leftShift);
    }

    /// <summary>
    /// Traverses the less-than expression.
    /// </summary>
    public void Traverse(ILessThan lessThan) {
      Contract.Requires(lessThan != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(lessThan);
      if (this.StopTraversal) return;
      this.TraverseChildren(lessThan);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(lessThan);
    }

    /// <summary>
    /// Traverses the less-than-or-equal expression.
    /// </summary>
    public void Traverse(ILessThanOrEqual lessThanOrEqual) {
      Contract.Requires(lessThanOrEqual != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(lessThanOrEqual);
      if (this.StopTraversal) return;
      this.TraverseChildren(lessThanOrEqual);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(lessThanOrEqual);
    }

    /// <summary>
    /// Traverses the local declaration statement.
    /// </summary>
    public void Traverse(ILocalDeclarationStatement localDeclarationStatement) {
      Contract.Requires(localDeclarationStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(localDeclarationStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(localDeclarationStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(localDeclarationStatement);
    }

    /// <summary>
    /// Traverses the lock statement.
    /// </summary>
    public void Traverse(ILockStatement lockStatement) {
      Contract.Requires(lockStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(lockStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(lockStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(lockStatement);
    }

    /// <summary>
    /// Traverses the logical not expression.
    /// </summary>
    public void Traverse(ILogicalNot logicalNot) {
      Contract.Requires(logicalNot != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(logicalNot);
      if (this.StopTraversal) return;
      this.TraverseChildren(logicalNot);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(logicalNot);
    }

    /// <summary>
    /// Traverses the make typed reference expression.
    /// </summary>
    public void Traverse(IMakeTypedReference makeTypedReference) {
      Contract.Requires(makeTypedReference != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(makeTypedReference);
      if (this.StopTraversal) return;
      this.TraverseChildren(makeTypedReference);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(makeTypedReference);
    }

    /// <summary>
    /// Traverses the the given method body.
    /// </summary>
    public override void Traverse(IMethodBody methodBody) {
      var sourceMethodBody = methodBody as ISourceMethodBody;
      if (sourceMethodBody != null)
        this.Traverse(sourceMethodBody);
      else
        base.Traverse(methodBody);
    }

    /// <summary>
    /// Traverses the method call.
    /// </summary>
    public void Traverse(IMethodCall methodCall) {
      Contract.Requires(methodCall != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(methodCall);
      if (this.StopTraversal) return;
      this.TraverseChildren(methodCall);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(methodCall);
    }

    /// <summary>
    /// Traverses the modulus expression.
    /// </summary>
    public void Traverse(IModulus modulus) {
      Contract.Requires(modulus != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(modulus);
      if (this.StopTraversal) return;
      this.TraverseChildren(modulus);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(modulus);
    }

    /// <summary>
    /// Traverses the multiplication expression.
    /// </summary>
    public void Traverse(IMultiplication multiplication) {
      Contract.Requires(multiplication != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(multiplication);
      if (this.StopTraversal) return;
      this.TraverseChildren(multiplication);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(multiplication);
    }

    /// <summary>
    /// Traverses the named argument expression.
    /// </summary>
    public void Traverse(INamedArgument namedArgument) {
      Contract.Requires(namedArgument != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(namedArgument);
      if (this.StopTraversal) return;
      this.TraverseChildren(namedArgument);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(namedArgument);
    }

    /// <summary>
    /// Traverses the not equality expression.
    /// </summary>
    public void Traverse(INotEquality notEquality) {
      Contract.Requires(notEquality != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(notEquality);
      if (this.StopTraversal) return;
      this.TraverseChildren(notEquality);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(notEquality);
    }

    /// <summary>
    /// Traverses the old value expression.
    /// </summary>
    public void Traverse(IOldValue oldValue) {
      Contract.Requires(oldValue != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(oldValue);
      if (this.StopTraversal) return;
      this.TraverseChildren(oldValue);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(oldValue);
    }

    /// <summary>
    /// Traverses the one's complement expression.
    /// </summary>
    public void Traverse(IOnesComplement onesComplement) {
      Contract.Requires(onesComplement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(onesComplement);
      if (this.StopTraversal) return;
      this.TraverseChildren(onesComplement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(onesComplement);
    }

    /// <summary>
    /// Traverses the out argument expression.
    /// </summary>
    public void Traverse(IOutArgument outArgument) {
      Contract.Requires(outArgument != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(outArgument);
      if (this.StopTraversal) return;
      this.TraverseChildren(outArgument);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(outArgument);
    }

    /// <summary>
    /// Traverses the pointer call.
    /// </summary>
    public void Traverse(IPointerCall pointerCall) {
      Contract.Requires(pointerCall != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(pointerCall);
      if (this.StopTraversal) return;
      this.TraverseChildren(pointerCall);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(pointerCall);
    }

    /// <summary>
    /// Traverses the pop value expression.
    /// </summary>
    public void Traverse(IPopValue popValue) {
      Contract.Requires(popValue != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(popValue);
      if (this.StopTraversal) return;
      this.TraverseChildren(popValue);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(popValue);
    }

    /// <summary>
    /// Traverses the push statement.
    /// </summary>
    public void Traverse(IPushStatement pushStatement) {
      Contract.Requires(pushStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(pushStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(pushStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(pushStatement);
    }

    /// <summary>
    /// Traverses the ref argument expression.
    /// </summary>
    public void Traverse(IRefArgument refArgument) {
      Contract.Requires(refArgument != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(refArgument);
      if (this.StopTraversal) return;
      this.TraverseChildren(refArgument);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(refArgument);
    }

    /// <summary>
    /// Traverses the resource usage statement.
    /// </summary>
    public void Traverse(IResourceUseStatement resourceUseStatement) {
      Contract.Requires(resourceUseStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(resourceUseStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(resourceUseStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(resourceUseStatement);
    }

    /// <summary>
    /// Traverses the rethrow statement.
    /// </summary>
    public void Traverse(IRethrowStatement rethrowStatement) {
      Contract.Requires(rethrowStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(rethrowStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(rethrowStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(rethrowStatement);
    }

    /// <summary>
    /// Traverses the return statement.
    /// </summary>
    public void Traverse(IReturnStatement returnStatement) {
      Contract.Requires(returnStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(returnStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(returnStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(returnStatement);
    }

    /// <summary>
    /// Traverses the return value expression.
    /// </summary>
    public void Traverse(IReturnValue returnValue) {
      Contract.Requires(returnValue != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(returnValue);
      if (this.StopTraversal) return;
      this.TraverseChildren(returnValue);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(returnValue);
    }

    /// <summary>
    /// Traverses the right shift expression.
    /// </summary>
    public void Traverse(IRightShift rightShift) {
      Contract.Requires(rightShift != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(rightShift);
      if (this.StopTraversal) return;
      this.TraverseChildren(rightShift);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(rightShift);
    }

    /// <summary>
    /// Traverses the runtime argument handle expression.
    /// </summary>
    public void Traverse(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      Contract.Requires(runtimeArgumentHandleExpression != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(runtimeArgumentHandleExpression);
      if (this.StopTraversal) return;
      this.TraverseChildren(runtimeArgumentHandleExpression);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(runtimeArgumentHandleExpression);
    }

    /// <summary>
    /// Traverses the sizeof() expression.
    /// </summary>
    public void Traverse(ISizeOf sizeOf) {
      Contract.Requires(sizeOf != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(sizeOf);
      if (this.StopTraversal) return;
      this.TraverseChildren(sizeOf);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(sizeOf);
    }

    /// <summary>
    /// Traverses the the given source method body.
    /// </summary>
    public void Traverse(ISourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(sourceMethodBody);
      if (this.StopTraversal) return;
      this.TraverseChildren(sourceMethodBody);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(sourceMethodBody);
    }

    /// <summary>
    /// Traverses the stack array create expression.
    /// </summary>
    public void Traverse(IStackArrayCreate stackArrayCreate) {
      Contract.Requires(stackArrayCreate != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(stackArrayCreate);
      if (this.StopTraversal) return;
      this.TraverseChildren(stackArrayCreate);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(stackArrayCreate);
    }

    /// <summary>
    /// Traverses the specified statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    public void Traverse(IStatement statement) {
      Contract.Requires(statement != null);
      statement.Dispatch(this.dispatchingVisitor);
    }

    /// <summary>
    /// Traverses the subtraction expression.
    /// </summary>
    public void Traverse(ISubtraction subtraction) {
      Contract.Requires(subtraction != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(subtraction);
      if (this.StopTraversal) return;
      this.TraverseChildren(subtraction);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(subtraction);
    }

    /// <summary>
    /// Traverses the switch case.
    /// </summary>
    public void Traverse(ISwitchCase switchCase) {
      Contract.Requires(switchCase != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(switchCase);
      if (this.StopTraversal) return;
      this.TraverseChildren(switchCase);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(switchCase);
    }

    /// <summary>
    /// Traverses the switch statement.
    /// </summary>
    public void Traverse(ISwitchStatement switchStatement) {
      Contract.Requires(switchStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(switchStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(switchStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(switchStatement);
    }

    /// <summary>
    /// Traverses the target expression.
    /// </summary>
    public void Traverse(ITargetExpression targetExpression) {
      Contract.Requires(targetExpression != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(targetExpression);
      if (this.StopTraversal) return;
      this.TraverseChildren(targetExpression);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(targetExpression);
    }

    /// <summary>
    /// Traverses the this reference expression.
    /// </summary>
    public void Traverse(IThisReference thisReference) {
      Contract.Requires(thisReference != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(thisReference);
      if (this.StopTraversal) return;
      this.TraverseChildren(thisReference);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(thisReference);
    }

    /// <summary>
    /// Traverses the throw statement.
    /// </summary>
    public void Traverse(IThrowStatement throwStatement) {
      Contract.Requires(throwStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(throwStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(throwStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(throwStatement);
    }

    /// <summary>
    /// Traverses the try-catch-filter-finally statement.
    /// </summary>
    public void Traverse(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      Contract.Requires(tryCatchFilterFinallyStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(tryCatchFilterFinallyStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(tryCatchFilterFinallyStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(tryCatchFilterFinallyStatement);
    }

    /// <summary>
    /// Traverses the tokenof() expression.
    /// </summary>
    public void Traverse(ITokenOf tokenOf) {
      Contract.Requires(tokenOf != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(tokenOf);
      if (this.StopTraversal) return;
      this.TraverseChildren(tokenOf);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(tokenOf);
    }

    /// <summary>
    /// Traverses the typeof() expression.
    /// </summary>
    public void Traverse(ITypeOf typeOf) {
      Contract.Requires(typeOf != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(typeOf);
      if (this.StopTraversal) return;
      this.TraverseChildren(typeOf);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(typeOf);
    }

    /// <summary>
    /// Traverses the unary negation expression.
    /// </summary>
    public void Traverse(IUnaryNegation unaryNegation) {
      Contract.Requires(unaryNegation != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(unaryNegation);
      if (this.StopTraversal) return;
      this.TraverseChildren(unaryNegation);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(unaryNegation);
    }

    /// <summary>
    /// Traverses the unary operation expression.
    /// </summary>
    /// <param name="unaryOperation"></param>
    public void Traverse(IUnaryOperation unaryOperation) {
      Contract.Requires(unaryOperation != null);
      unaryOperation.Dispatch(this.dispatchingVisitor);
    }

    /// <summary>
    /// Traverses the unary plus expression.
    /// </summary>
    public void Traverse(IUnaryPlus unaryPlus) {
      Contract.Requires(unaryPlus != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(unaryPlus);
      if (this.StopTraversal) return;
      this.TraverseChildren(unaryPlus);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(unaryPlus);
    }

    /// <summary>
    /// Traverses the vector length expression.
    /// </summary>
    public void Traverse(IVectorLength vectorLength) {
      Contract.Requires(vectorLength != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(vectorLength);
      if (this.StopTraversal) return;
      this.TraverseChildren(vectorLength);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(vectorLength);
    }

    /// <summary>
    /// Traverses the while do statement.
    /// </summary>
    public void Traverse(IWhileDoStatement whileDoStatement) {
      Contract.Requires(whileDoStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(whileDoStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(whileDoStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(whileDoStatement);
    }

    /// <summary>
    /// Traverses the yield break statement.
    /// </summary>
    public void Traverse(IYieldBreakStatement yieldBreakStatement) {
      Contract.Requires(yieldBreakStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(yieldBreakStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(yieldBreakStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(yieldBreakStatement);
    }

    /// <summary>
    /// Traverses the yield return statement.
    /// </summary>
    public void Traverse(IYieldReturnStatement yieldReturnStatement) {
      Contract.Requires(yieldReturnStatement != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(yieldReturnStatement);
      if (this.StopTraversal) return;
      this.TraverseChildren(yieldReturnStatement);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(yieldReturnStatement);
    }

    /// <summary>
    /// Traverses the enumeration of catch clauses.
    /// </summary>
    public void Traverse(IEnumerable<ICatchClause> catchClauses) {
      Contract.Requires(catchClauses != null);
      foreach (var catchClause in catchClauses) {
        this.Traverse(catchClause);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of expressions.
    /// </summary>
    public void Traverse(IEnumerable<IExpression> expressions) {
      Contract.Requires(expressions != null);
      foreach (var expression in expressions) {
        this.Traverse(expression);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of switch cases.
    /// </summary>
    public void Traverse(IEnumerable<ISwitchCase> switchCases) {
      foreach (var switchCase in switchCases) {
        this.Traverse(switchCase);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of statements.
    /// </summary>
    public void Traverse(IEnumerable<IStatement> statements) {
      foreach (var statement in statements) {
        this.Traverse(statement);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the children of the addition.
    /// </summary>
    public virtual void TraverseChildren(IAddition addition) {
      Contract.Requires(addition != null);
      this.TraverseChildren((IBinaryOperation)addition);
    }

    /// <summary>
    /// Traverses the children of the addressable expression.
    /// </summary>
    public virtual void TraverseChildren(IAddressableExpression addressableExpression) {
      Contract.Requires(addressableExpression != null);
      this.TraverseChildren((IExpression)addressableExpression);
      if (this.StopTraversal) return;
      var local = addressableExpression.Definition as ILocalDefinition;
      if (local != null)
        this.Traverse(local);
      else {
        var parameter = addressableExpression.Definition as IParameterDefinition;
        if (parameter != null)
          this.Traverse(parameter);
        else {
          var fieldReference = addressableExpression.Definition as IFieldReference;
          if (fieldReference != null)
            this.Traverse(fieldReference);
          else {
            var arrayIndexer = addressableExpression.Definition as IArrayIndexer;
            if (arrayIndexer != null) {
              this.Traverse(arrayIndexer);
              return; //do not traverse Instance again
            } else {
              var methodReference = addressableExpression.Definition as IMethodReference;
              if (methodReference != null)
                this.Traverse(methodReference);
              else {
                var expression = (IExpression)addressableExpression.Definition;
                this.Traverse(expression);
              }
            }
          }
        }
      }
      if (this.StopTraversal) return;
      if (addressableExpression.Instance != null) {
        this.Traverse(addressableExpression.Instance);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the children of the address dereference expression.
    /// </summary>
    public virtual void TraverseChildren(IAddressDereference addressDereference) {
      Contract.Requires(addressDereference != null);
      this.TraverseChildren((IExpression)addressDereference);
      if (this.StopTraversal) return;
      this.Traverse(addressDereference.Address);
    }

    /// <summary>
    /// Traverses the children of the AddressOf expression.
    /// </summary>
    public virtual void TraverseChildren(IAddressOf addressOf) {
      Contract.Requires(addressOf != null);
      this.TraverseChildren((IExpression)addressOf);
      if (this.StopTraversal) return;
      this.Traverse(addressOf.Expression);
    }

    /// <summary>
    /// Traverses the children of the anonymous delegate expression.
    /// </summary>
    public virtual void TraverseChildren(IAnonymousDelegate anonymousDelegate) {
      Contract.Requires(anonymousDelegate != null);
      this.TraverseChildren((IExpression)anonymousDelegate);
      if (this.StopTraversal) return;
      this.Traverse(anonymousDelegate.Parameters);
      if (this.StopTraversal) return;
      this.Traverse(anonymousDelegate.Body);
      if (this.StopTraversal) return;
      this.Traverse(anonymousDelegate.ReturnType);
      if (this.StopTraversal) return;
      if (anonymousDelegate.ReturnValueIsModified)
        this.Traverse(anonymousDelegate.ReturnValueCustomModifiers);
    }

    /// <summary>
    /// Traverses the children of the array indexer expression.
    /// </summary>
    public virtual void TraverseChildren(IArrayIndexer arrayIndexer) {
      Contract.Requires(arrayIndexer != null);
      this.TraverseChildren((IExpression)arrayIndexer);
      if (this.StopTraversal) return;
      this.Traverse(arrayIndexer.IndexedObject);
      if (this.StopTraversal) return;
      this.Traverse(arrayIndexer.Indices);
    }

    /// <summary>
    /// Traverses the children of the assert statement.
    /// </summary>
    public virtual void TraverseChildren(IAssertStatement assertStatement) {
      Contract.Requires(assertStatement != null);
      this.TraverseChildren((IStatement)assertStatement);
      if (this.StopTraversal) return;
      this.Traverse(assertStatement.Condition);
      if (this.StopTraversal) return;
      if (assertStatement.Description != null)
        this.Traverse(assertStatement.Description);
    }

    /// <summary>
    /// Traverses the children of the assignment expression.
    /// </summary>
    public virtual void TraverseChildren(IAssignment assignment) {
      Contract.Requires(assignment != null);
      this.TraverseChildren((IExpression)assignment);
      if (this.StopTraversal) return;
      this.Traverse(assignment.Target);
      if (this.StopTraversal) return;
      this.Traverse(assignment.Source);
    }

    /// <summary>
    /// Traverses the children of the assume statement.
    /// </summary>
    public virtual void TraverseChildren(IAssumeStatement assumeStatement) {
      Contract.Requires(assumeStatement != null);
      this.TraverseChildren((IStatement)assumeStatement);
      if (this.StopTraversal) return;
      this.Traverse(assumeStatement.Condition);
      if (this.StopTraversal) return;
      if (assumeStatement.Description != null)
        this.Traverse(assumeStatement.Description);
    }

    /// <summary>
    /// Called whenever a binary operation expression is about to be traversed by a type specific routine.
    /// This gives the traverser the opportunity to take some uniform action for all binary operation expressions,
    /// regardless of how the traversal gets to them.
    /// </summary>
    public virtual void TraverseChildren(IBinaryOperation binaryOperation) {
      Contract.Requires(binaryOperation != null);
      this.TraverseChildren((IExpression)binaryOperation);
      this.Traverse(binaryOperation.LeftOperand);
      this.Traverse(binaryOperation.RightOperand);
    }

    /// <summary>
    /// Traverses the children of the bitwise and expression.
    /// </summary>
    public virtual void TraverseChildren(IBitwiseAnd bitwiseAnd) {
      Contract.Requires(bitwiseAnd != null);
      this.TraverseChildren((IBinaryOperation)bitwiseAnd);
    }

    /// <summary>
    /// Traverses the children of the bitwise or expression.
    /// </summary>
    public virtual void TraverseChildren(IBitwiseOr bitwiseOr) {
      Contract.Requires(bitwiseOr != null);
      this.TraverseChildren((IBinaryOperation)bitwiseOr);
    }

    /// <summary>
    /// Traverses the children of the block expression.
    /// </summary>
    public virtual void TraverseChildren(IBlockExpression blockExpression) {
      Contract.Requires(blockExpression != null);
      this.TraverseChildren((IExpression)blockExpression);
      if (this.StopTraversal) return;
      this.Traverse(blockExpression.BlockStatement);
      if (this.StopTraversal) return;
      this.Traverse(blockExpression.Expression);
    }

    /// <summary>
    /// Traverses the children of the statement block.
    /// </summary>
    public virtual void TraverseChildren(IBlockStatement block) {
      Contract.Requires(block != null);
      this.TraverseChildren((IStatement)block);
      if (this.StopTraversal) return;
      this.Traverse(block.Statements);
    }

    /// <summary>
    /// Traverses the children of the bound expression.
    /// </summary>
    public virtual void TraverseChildren(IBoundExpression boundExpression) {
      Contract.Requires(boundExpression != null);
      this.TraverseChildren((IExpression)boundExpression);
      if (this.StopTraversal) return;
      if (boundExpression.Instance != null) {
        this.Traverse(boundExpression.Instance);
        if (this.StopTraversal) return;
      }
      var local = boundExpression.Definition as ILocalDefinition;
      if (local != null)
        this.Traverse(local);
      else {
        var parameter = boundExpression.Definition as IParameterDefinition;
        if (parameter != null)
          this.Traverse(parameter);
        else {
          var fieldReference = (IFieldReference)boundExpression.Definition;
          this.Traverse(fieldReference);
        }
      }
    }

    /// <summary>
    /// Traverses the children of the break statement.
    /// </summary>
    public virtual void TraverseChildren(IBreakStatement breakStatement) {
      Contract.Requires(breakStatement != null);
      this.TraverseChildren((IStatement)breakStatement);
    }

    /// <summary>
    /// Traverses the cast-if-possible expression.
    /// </summary>
    public virtual void TraverseChildren(ICastIfPossible castIfPossible) {
      Contract.Requires(castIfPossible != null);
      this.TraverseChildren((IExpression)castIfPossible);
      if (this.StopTraversal) return;
      this.Traverse(castIfPossible.ValueToCast);
      if (this.StopTraversal) return;
      this.Traverse(castIfPossible.TargetType);
    }

    /// <summary>
    /// Traverses the children of the catch clause.
    /// </summary>
    public virtual void TraverseChildren(ICatchClause catchClause) {
      Contract.Requires(catchClause != null);
      this.Traverse(catchClause.ExceptionType);
      if (this.StopTraversal) return;
      if (!(catchClause.ExceptionContainer is Dummy)) {
        this.Traverse(catchClause.ExceptionContainer);
        if (this.StopTraversal) return;
      }
      if (catchClause.FilterCondition != null) {
        this.Traverse(catchClause.FilterCondition);
        if (this.StopTraversal) return;
      }
      this.Traverse(catchClause.Body);
    }

    /// <summary>
    /// Traverses the children of the check-if-instance expression.
    /// </summary>
    public virtual void TraverseChildren(ICheckIfInstance checkIfInstance) {
      Contract.Requires(checkIfInstance != null);
      this.TraverseChildren((IExpression)checkIfInstance);
      if (this.StopTraversal) return;
      this.Traverse(checkIfInstance.Operand);
      if (this.StopTraversal) return;
      this.Traverse(checkIfInstance.TypeToCheck);
    }

    /// <summary>
    /// Traverses the children of the compile time constant.
    /// </summary>
    public virtual void TraverseChildren(ICompileTimeConstant constant) {
      Contract.Requires(constant != null);
      this.TraverseChildren((IExpression)constant);
    }

    /// <summary>
    /// Traverses the children of the conditional expression.
    /// </summary>
    public virtual void TraverseChildren(IConditional conditional) {
      Contract.Requires(conditional != null);
      this.TraverseChildren((IExpression)conditional);
      if (this.StopTraversal) return;
      this.Traverse(conditional.Condition);
      if (this.StopTraversal) return;
      this.Traverse(conditional.ResultIfTrue);
      if (this.StopTraversal) return;
      this.Traverse(conditional.ResultIfFalse);
    }

    /// <summary>
    /// Traverses the children of the conditional statement.
    /// </summary>
    public virtual void TraverseChildren(IConditionalStatement conditionalStatement) {
      Contract.Requires(conditionalStatement != null);
      this.TraverseChildren((IStatement)conditionalStatement);
      if (this.StopTraversal) return;
      this.Traverse(conditionalStatement.Condition);
      if (this.StopTraversal) return;
      this.Traverse(conditionalStatement.TrueBranch);
      if (this.StopTraversal) return;
      this.Traverse(conditionalStatement.FalseBranch);
    }

    /// <summary>
    /// Traverses the children of the continue statement.
    /// </summary>
    public virtual void TraverseChildren(IContinueStatement continueStatement) {
      Contract.Requires(continueStatement != null);
      this.TraverseChildren((IStatement)continueStatement);
    }

    /// <summary>
    /// Traverses the children of the copy memory statement.
    /// </summary>
    public virtual void TraverseChildren(ICopyMemoryStatement copyMemoryStatement) {
      Contract.Requires(copyMemoryStatement != null);
      this.TraverseChildren((IStatement)copyMemoryStatement);
      if (this.StopTraversal) return;
      this.Traverse(copyMemoryStatement.TargetAddress);
      if (this.StopTraversal) return;
      this.Traverse(copyMemoryStatement.SourceAddress);
      if (this.StopTraversal) return;
      this.Traverse(copyMemoryStatement.NumberOfBytesToCopy);
    }

    /// <summary>
    /// Traverses the children of the conversion expression.
    /// </summary>
    public virtual void TraverseChildren(IConversion conversion) {
      Contract.Requires(conversion != null);
      this.TraverseChildren((IExpression)conversion);
      if (this.StopTraversal) return;
      this.Traverse(conversion.ValueToConvert);
      if (this.StopTraversal) return;
      this.Traverse(conversion.TypeAfterConversion);
    }

    /// <summary>
    /// Traverses the children of the array creation expression.
    /// </summary>
    public virtual void TraverseChildren(ICreateArray createArray) {
      Contract.Requires(createArray != null);
      this.TraverseChildren((IExpression)createArray);
      if (this.StopTraversal) return;
      this.Traverse(createArray.ElementType);
      if (this.StopTraversal) return;
      this.Traverse(createArray.Sizes);
      if (this.StopTraversal) return;
      this.Traverse(createArray.Initializers);
    }

    /// <summary>
    /// Traverses the children the delegate instance creation expression.
    /// </summary>
    public virtual void TraverseChildren(ICreateDelegateInstance createDelegateInstance) {
      Contract.Requires(createDelegateInstance != null);
      this.TraverseChildren((IExpression)createDelegateInstance);
      if (this.StopTraversal) return;
      if (createDelegateInstance.Instance != null)
        this.Traverse(createDelegateInstance.Instance);
      if (this.StopTraversal) return;
      this.Traverse(createDelegateInstance.MethodToCallViaDelegate);
    }

    /// <summary>
    /// Traverses the children of the create object instance expression.
    /// </summary>
    public virtual void TraverseChildren(ICreateObjectInstance createObjectInstance) {
      Contract.Requires(createObjectInstance != null);
      this.TraverseChildren((IExpression)createObjectInstance);
      if (this.StopTraversal) return;
      this.Traverse(createObjectInstance.MethodToCall);
      if (this.StopTraversal) return;
      this.Traverse(createObjectInstance.Arguments);
    }

    /// <summary>
    /// Traverses the children of the debugger break statement.
    /// </summary>
    public virtual void TraverseChildren(IDebuggerBreakStatement debuggerBreakStatement) {
      Contract.Requires(debuggerBreakStatement != null);
      this.TraverseChildren((IStatement)debuggerBreakStatement);
    }

    /// <summary>
    /// Traverses the children of the defalut value expression.
    /// </summary>
    public virtual void TraverseChildren(IDefaultValue defaultValue) {
      Contract.Requires(defaultValue != null);
      this.TraverseChildren((IExpression)defaultValue);
      if (this.StopTraversal) return;
      this.Traverse(defaultValue.DefaultValueType);
    }

    /// <summary>
    /// Traverses the children of the division expression.
    /// </summary>
    public virtual void TraverseChildren(IDivision division) {
      Contract.Requires(division != null);
      this.TraverseChildren((IBinaryOperation)division);
    }

    /// <summary>
    /// Traverses the children of the do until statement.
    /// </summary>
    public virtual void TraverseChildren(IDoUntilStatement doUntilStatement) {
      Contract.Requires(doUntilStatement != null);
      this.TraverseChildren((IStatement)doUntilStatement);
      if (this.StopTraversal) return;
      this.Traverse(doUntilStatement.Body);
      if (this.StopTraversal) return;
      this.Traverse(doUntilStatement.Condition);
    }

    /// <summary>
    /// Traverses the children of the dup value expression.
    /// </summary>
    public virtual void TraverseChildren(IDupValue dupValue) {
      Contract.Requires(dupValue != null);
      this.TraverseChildren((IExpression)dupValue);
    }

    /// <summary>
    /// Traverses the children of the empty statement.
    /// </summary>
    public virtual void TraverseChildren(IEmptyStatement emptyStatement) {
      Contract.Requires(emptyStatement != null);
      this.TraverseChildren((IStatement)emptyStatement);
    }

    /// <summary>
    /// Traverses the children of the equality expression.
    /// </summary>
    public virtual void TraverseChildren(IEquality equality) {
      Contract.Requires(equality != null);
      this.TraverseChildren((IBinaryOperation)equality);
    }

    /// <summary>
    /// Traverses the children of the exclusive or expression.
    /// </summary>
    public virtual void TraverseChildren(IExclusiveOr exclusiveOr) {
      Contract.Requires(exclusiveOr != null);
      this.TraverseChildren((IBinaryOperation)exclusiveOr);
    }

    /// <summary>
    /// Called whenever an expression is about to be traversed by a type specific routine.
    /// This gives the traverser the opportunity to take some uniform action for all expressions,
    /// regardless of how the traversal gets to them.
    /// </summary>
    public virtual void TraverseChildren(IExpression expression) {
      Contract.Requires(expression != null);
      this.Traverse(expression.Type);
    }

    /// <summary>
    /// Traverses the children of the expression statement.
    /// </summary>
    public virtual void TraverseChildren(IExpressionStatement expressionStatement) {
      Contract.Requires(expressionStatement != null);
      this.TraverseChildren((IStatement)expressionStatement);
      if (this.StopTraversal) return;
      this.Traverse(expressionStatement.Expression);
    }

    /// <summary>
    /// Traverses the children of the fill memory statement.
    /// </summary>
    public virtual void TraverseChildren(IFillMemoryStatement fillMemoryStatement) {
      Contract.Requires(fillMemoryStatement != null);
      this.TraverseChildren((IStatement)fillMemoryStatement);
      if (this.StopTraversal) return;
      this.Traverse(fillMemoryStatement.TargetAddress);
      if (this.StopTraversal) return;
      this.Traverse(fillMemoryStatement.FillValue);
      if (this.StopTraversal) return;
      this.Traverse(fillMemoryStatement.NumberOfBytesToFill);
    }

    /// <summary>
    /// Traverses the children of the foreach statement.
    /// </summary>
    public virtual void TraverseChildren(IForEachStatement forEachStatement) {
      Contract.Requires(forEachStatement != null);
      this.TraverseChildren((IStatement)forEachStatement);
      if (this.StopTraversal) return;
      this.Traverse(forEachStatement.Variable);
      if (this.StopTraversal) return;
      this.Traverse(forEachStatement.Collection);
      if (this.StopTraversal) return;
      this.Traverse(forEachStatement.Body);
    }

    /// <summary>
    /// Traverses the children of the for statement.
    /// </summary>
    public virtual void TraverseChildren(IForStatement forStatement) {
      Contract.Requires(forStatement != null);
      this.TraverseChildren((IStatement)forStatement);
      if (this.StopTraversal) return;
      this.Traverse(forStatement.InitStatements);
      if (this.StopTraversal) return;
      this.Traverse(forStatement.Condition);
      if (this.StopTraversal) return;
      this.Traverse(forStatement.IncrementStatements);
      if (this.StopTraversal) return;
      this.Traverse(forStatement.Body);
    }

    /// <summary>
    /// Traverses the children of the get type of typed reference expression.
    /// </summary>
    public virtual void TraverseChildren(IGetTypeOfTypedReference getTypeOfTypedReference) {
      Contract.Requires(getTypeOfTypedReference != null);
      this.TraverseChildren((IExpression)getTypeOfTypedReference);
      if (this.StopTraversal) return;
      this.Traverse(getTypeOfTypedReference.TypedReference);
    }

    /// <summary>
    /// Traverses the children of the get value of typed reference expression.
    /// </summary>
    public virtual void TraverseChildren(IGetValueOfTypedReference getValueOfTypedReference) {
      Contract.Requires(getValueOfTypedReference != null);
      this.TraverseChildren((IExpression)getValueOfTypedReference);
      if (this.StopTraversal) return;
      this.Traverse(getValueOfTypedReference.TypedReference);
      if (this.StopTraversal) return;
      this.Traverse(getValueOfTypedReference.TargetType);
    }

    /// <summary>
    /// Traverses the children of the goto statement.
    /// </summary>
    public virtual void TraverseChildren(IGotoStatement gotoStatement) {
      Contract.Requires(gotoStatement != null);
      this.TraverseChildren((IStatement)gotoStatement);
    }

    /// <summary>
    /// Traverses the children of the goto switch case statement.
    /// </summary>
    public virtual void TraverseChildren(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      Contract.Requires(gotoSwitchCaseStatement != null);
      this.TraverseChildren((IStatement)gotoSwitchCaseStatement);
    }

    /// <summary>
    /// Traverses the children of the greater-than expression.
    /// </summary>
    public virtual void TraverseChildren(IGreaterThan greaterThan) {
      Contract.Requires(greaterThan != null);
      this.TraverseChildren((IBinaryOperation)greaterThan);
    }

    /// <summary>
    /// Traverses the children of the greater-than-or-equal expression.
    /// </summary>
    public virtual void TraverseChildren(IGreaterThanOrEqual greaterThanOrEqual) {
      Contract.Requires(greaterThanOrEqual != null);
      this.TraverseChildren((IBinaryOperation)greaterThanOrEqual);
    }

    /// <summary>
    /// Traverses the children of the labeled statement.
    /// </summary>
    public virtual void TraverseChildren(ILabeledStatement labeledStatement) {
      Contract.Requires(labeledStatement != null);
      this.TraverseChildren((IStatement)labeledStatement);
      if (this.StopTraversal) return;
      this.Traverse(labeledStatement.Statement);
    }

    /// <summary>
    /// Traverses the children of the left shift expression.
    /// </summary>
    public virtual void TraverseChildren(ILeftShift leftShift) {
      Contract.Requires(leftShift != null);
      this.TraverseChildren((IBinaryOperation)leftShift);
    }

    /// <summary>
    /// Traverses the children of the less-than expression.
    /// </summary>
    public virtual void TraverseChildren(ILessThan lessThan) {
      Contract.Requires(lessThan != null);
      this.TraverseChildren((IBinaryOperation)lessThan);
    }

    /// <summary>
    /// Traverses the children of the less-than-or-equal expression.
    /// </summary>
    public virtual void TraverseChildren(ILessThanOrEqual lessThanOrEqual) {
      Contract.Requires(lessThanOrEqual != null);
      this.TraverseChildren((IBinaryOperation)lessThanOrEqual);
    }

    /// <summary>
    /// Traverses the children of the local declaration statement.
    /// </summary>
    public virtual void TraverseChildren(ILocalDeclarationStatement localDeclarationStatement) {
      Contract.Requires(localDeclarationStatement != null);
      this.TraverseChildren((IStatement)localDeclarationStatement);
      if (this.StopTraversal) return;
      this.Traverse(localDeclarationStatement.LocalVariable);
      if (this.StopTraversal) return;
      if (localDeclarationStatement.InitialValue != null)
        this.Traverse(localDeclarationStatement.InitialValue);
    }

    /// <summary>
    /// Traverses the children of the lock statement.
    /// </summary>
    public virtual void TraverseChildren(ILockStatement lockStatement) {
      Contract.Requires(lockStatement != null);
      this.TraverseChildren((IStatement)lockStatement);
      if (this.StopTraversal) return;
      this.Traverse(lockStatement.Guard);
      if (this.StopTraversal) return;
      this.Traverse(lockStatement.Body);
    }

    /// <summary>
    /// Traverses the children of the logical not expression.
    /// </summary>
    public virtual void TraverseChildren(ILogicalNot logicalNot) {
      Contract.Requires(logicalNot != null);
      this.TraverseChildren((IUnaryOperation)logicalNot);
    }

    /// <summary>
    /// Traverses the children of the make typed reference expression.
    /// </summary>
    public virtual void TraverseChildren(IMakeTypedReference makeTypedReference) {
      Contract.Requires(makeTypedReference != null);
      this.TraverseChildren((IExpression)makeTypedReference);
      if (this.StopTraversal) return;
      this.Traverse(makeTypedReference.Operand);
    }

    /// <summary>
    /// Traverses the children of the method call.
    /// </summary>
    public virtual void TraverseChildren(IMethodCall methodCall) {
      Contract.Requires(methodCall != null);
      this.TraverseChildren((IExpression)methodCall);
      if (this.StopTraversal) return;
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall) {
        this.Traverse(methodCall.ThisArgument);
        if (this.StopTraversal) return;
      }
      this.Traverse(methodCall.MethodToCall);
      if (this.StopTraversal) return;
      this.Traverse(methodCall.Arguments);
    }

    /// <summary>
    /// Traverses the children of the modulus expression.
    /// </summary>
    public virtual void TraverseChildren(IModulus modulus) {
      Contract.Requires(modulus != null);
      this.TraverseChildren((IBinaryOperation)modulus);
    }

    /// <summary>
    /// Traverses the children of the multiplication expression.
    /// </summary>
    public virtual void TraverseChildren(IMultiplication multiplication) {
      Contract.Requires(multiplication != null);
      this.TraverseChildren((IBinaryOperation)multiplication);
    }

    /// <summary>
    /// Traverses the children of the named argument expression.
    /// </summary>
    public virtual void TraverseChildren(INamedArgument namedArgument) {
      Contract.Requires(namedArgument != null);
      this.TraverseChildren((IExpression)namedArgument);
      if (this.StopTraversal) return;
      this.Traverse(namedArgument.ArgumentValue);
    }

    /// <summary>
    /// Traverses the children of the not equality expression.
    /// </summary>
    public virtual void TraverseChildren(INotEquality notEquality) {
      Contract.Requires(notEquality != null);
      this.TraverseChildren((IBinaryOperation)notEquality);
    }

    /// <summary>
    /// Traverses the children of the old value expression.
    /// </summary>
    public virtual void TraverseChildren(IOldValue oldValue) {
      Contract.Requires(oldValue != null);
      this.TraverseChildren((IExpression)oldValue);
      if (this.StopTraversal) return;
      this.Traverse(oldValue.Expression);
    }

    /// <summary>
    /// Traverses the children of the one's complement expression.
    /// </summary>
    public virtual void TraverseChildren(IOnesComplement onesComplement) {
      Contract.Requires(onesComplement != null);
      this.TraverseChildren((IUnaryOperation)onesComplement);
    }

    /// <summary>
    /// Traverses the children of the out argument expression.
    /// </summary>
    public virtual void TraverseChildren(IOutArgument outArgument) {
      Contract.Requires(outArgument != null);
      this.TraverseChildren((IExpression)outArgument);
      if (this.StopTraversal) return;
      this.Traverse(outArgument.Expression);
    }

    /// <summary>
    /// Traverses the children of the pointer call.
    /// </summary>
    public virtual void TraverseChildren(IPointerCall pointerCall) {
      Contract.Requires(pointerCall != null);
      this.TraverseChildren((IExpression)pointerCall);
      if (this.StopTraversal) return;
      this.Traverse(pointerCall.Pointer);
      if (this.StopTraversal) return;
      this.Traverse(pointerCall.Arguments);
    }

    /// <summary>
    /// Traverses the children of the pop value expression.
    /// </summary>
    public virtual void TraverseChildren(IPopValue popValue) {
      Contract.Requires(popValue != null);
      this.TraverseChildren((IExpression)popValue);
    }

    /// <summary>
    /// Traverses the children of the push statement.
    /// </summary>
    public virtual void TraverseChildren(IPushStatement pushStatement) {
      Contract.Requires(pushStatement != null);
      this.TraverseChildren((IStatement)pushStatement);
      if (this.StopTraversal) return;
      this.Traverse(pushStatement.ValueToPush);
    }

    /// <summary>
    /// Traverses the children of the ref argument expression.
    /// </summary>
    public virtual void TraverseChildren(IRefArgument refArgument) {
      Contract.Requires(refArgument != null);
      this.TraverseChildren((IExpression)refArgument);
      if (this.StopTraversal) return;
      this.Traverse(refArgument.Expression);
    }

    /// <summary>
    /// Traverses the children of the resource usage statement.
    /// </summary>
    public virtual void TraverseChildren(IResourceUseStatement resourceUseStatement) {
      Contract.Requires(resourceUseStatement != null);
      this.TraverseChildren((IStatement)resourceUseStatement);
      if (this.StopTraversal) return;
      this.Traverse(resourceUseStatement.ResourceAcquisitions);
      if (this.StopTraversal) return;
      this.Traverse(resourceUseStatement.Body);
    }

    /// <summary>
    /// Traverses the rethrow statement.
    /// </summary>
    public virtual void TraverseChildren(IRethrowStatement rethrowStatement) {
      Contract.Requires(rethrowStatement != null);
      this.TraverseChildren((IStatement)rethrowStatement);
    }

    /// <summary>
    /// Traverses the return statement.
    /// </summary>
    public virtual void TraverseChildren(IReturnStatement returnStatement) {
      Contract.Requires(returnStatement != null);
      this.TraverseChildren((IStatement)returnStatement);
      if (this.StopTraversal) return;
      if (returnStatement.Expression != null)
        this.Traverse(returnStatement.Expression);
    }

    /// <summary>
    /// Traverses the children of the return value expression.
    /// </summary>
    public virtual void TraverseChildren(IReturnValue returnValue) {
      Contract.Requires(returnValue != null);
      this.TraverseChildren((IExpression)returnValue);
    }

    /// <summary>
    /// Traverses the children of the right shift expression.
    /// </summary>
    public virtual void TraverseChildren(IRightShift rightShift) {
      Contract.Requires(rightShift != null);
      this.TraverseChildren((IBinaryOperation)rightShift);
    }

    /// <summary>
    /// Traverses the children of the runtime argument handle expression.
    /// </summary>
    public virtual void TraverseChildren(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      Contract.Requires(runtimeArgumentHandleExpression != null);
      this.TraverseChildren((IExpression)runtimeArgumentHandleExpression);
    }

    /// <summary>
    /// Traverses the children of the sizeof() expression.
    /// </summary>
    public virtual void TraverseChildren(ISizeOf sizeOf) {
      Contract.Requires(sizeOf != null);
      this.TraverseChildren((IExpression)sizeOf);
      if (this.StopTraversal) return;
      this.Traverse(sizeOf.TypeToSize);
    }

    /// <summary>
    /// Traverses the the given source method body.
    /// </summary>
    public virtual void TraverseChildren(ISourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      //do not traverse the IL via IMethodBody
      this.Traverse(sourceMethodBody.Block);
    }

    /// <summary>
    /// Traverses the children of the stack array create expression.
    /// </summary>
    public virtual void TraverseChildren(IStackArrayCreate stackArrayCreate) {
      Contract.Requires(stackArrayCreate != null);
      this.TraverseChildren((IExpression)stackArrayCreate);
      if (this.StopTraversal) return;
      this.Traverse(stackArrayCreate.ElementType);
      if (this.StopTraversal) return;
      this.Traverse(stackArrayCreate.Size);
    }

    /// <summary>
    /// Called whenever a statement is about to be traversed by a type specific routine.
    /// This gives the traverser the opportunity to take some uniform action for all statements,
    /// regardless of how the traversal gets to them.
    /// </summary>
    public virtual void TraverseChildren(IStatement statement) {
      Contract.Requires(statement != null);
      //this is just an extension hook
    }

    /// <summary>
    /// Traverses the children of the subtraction expression.
    /// </summary>
    public virtual void TraverseChildren(ISubtraction subtraction) {
      Contract.Requires(subtraction != null);
      this.TraverseChildren((IBinaryOperation)subtraction);
    }

    /// <summary>
    /// Traverses the children of the switch case.
    /// </summary>
    public virtual void TraverseChildren(ISwitchCase switchCase) {
      Contract.Requires(switchCase != null);
      if (!switchCase.IsDefault) {
        this.Traverse(switchCase.Expression);
        if (this.StopTraversal) return;
      }
      this.Traverse(switchCase.Body);
    }

    /// <summary>
    /// Traverses the children of the switch statement.
    /// </summary>
    public virtual void TraverseChildren(ISwitchStatement switchStatement) {
      Contract.Requires(switchStatement != null);
      this.TraverseChildren((IStatement)switchStatement);
      if (this.StopTraversal) return;
      this.Traverse(switchStatement.Expression);
      if (this.StopTraversal) return;
      this.Traverse(switchStatement.Cases);
    }

    /// <summary>
    /// Traverses the children of the target expression.
    /// </summary>
    public virtual void TraverseChildren(ITargetExpression targetExpression) {
      Contract.Requires(targetExpression != null);
      this.TraverseChildren((IExpression)targetExpression);
      if (this.StopTraversal) return;
      var local = targetExpression.Definition as ILocalDefinition;
      if (local != null)
        this.Traverse(local);
      else {
        var parameter = targetExpression.Definition as IParameterDefinition;
        if (parameter != null)
          this.Traverse(parameter);
        else {
          var fieldReference = targetExpression.Definition as IFieldReference;
          if (fieldReference != null)
            this.Traverse(fieldReference);
          else {
            var arrayIndexer = targetExpression.Definition as IArrayIndexer;
            if (arrayIndexer != null) {
              this.Traverse(arrayIndexer);
              return; //do not visit the instance again
            } else {
              var addressDereference = targetExpression.Definition as IAddressDereference;
              if (addressDereference != null)
                this.Traverse(addressDereference);
              else {
                var propertyDefinition = targetExpression.Definition as IPropertyDefinition;
                if (propertyDefinition != null)
                  this.Traverse(propertyDefinition);
                else
                  this.Traverse((IThisReference)targetExpression.Definition);
              }
            }
          }
        }
      }
      if (this.StopTraversal) return;
      if (targetExpression.Instance != null)
        this.Traverse(targetExpression.Instance);
    }

    /// <summary>
    /// Traverses the children of the this reference expression.
    /// </summary>
    public virtual void TraverseChildren(IThisReference thisReference) {
      Contract.Requires(thisReference != null);
      this.TraverseChildren((IExpression)thisReference);
    }

    /// <summary>
    /// Traverses the throw statement.
    /// </summary>
    public virtual void TraverseChildren(IThrowStatement throwStatement) {
      Contract.Requires(throwStatement != null);
      this.TraverseChildren((IStatement)throwStatement);
      if (this.StopTraversal) return;
      this.Traverse(throwStatement.Exception);
    }

    /// <summary>
    /// Traverses the try-catch-filter-finally statement.
    /// </summary>
    public virtual void TraverseChildren(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      Contract.Requires(tryCatchFilterFinallyStatement != null);
      this.TraverseChildren((IStatement)tryCatchFilterFinallyStatement);
      if (this.StopTraversal) return;
      this.Traverse(tryCatchFilterFinallyStatement.TryBody);
      if (this.StopTraversal) return;
      this.Traverse(tryCatchFilterFinallyStatement.CatchClauses);
      if (this.StopTraversal) return;
      if (tryCatchFilterFinallyStatement.FaultBody != null) {
        this.Traverse(tryCatchFilterFinallyStatement.FaultBody);
        if (this.StopTraversal) return;
      }
      if (tryCatchFilterFinallyStatement.FinallyBody != null)
        this.Traverse(tryCatchFilterFinallyStatement.FinallyBody);
    }

    /// <summary>
    /// Traverses the children of the tokenof() expression.
    /// </summary>
    public virtual void TraverseChildren(ITokenOf tokenOf) {
      Contract.Requires(tokenOf != null);
      this.TraverseChildren((IExpression)tokenOf);
      if (this.StopTraversal) return;
      var fieldReference = tokenOf.Definition as IFieldReference;
      if (fieldReference != null)
        this.Traverse(fieldReference);
      else {
        var methodReference = tokenOf.Definition as IMethodReference;
        if (methodReference != null)
          this.Traverse(methodReference);
        else {
          var typeReference = (ITypeReference)tokenOf.Definition;
          this.Traverse(typeReference);
        }
      }
    }

    /// <summary>
    /// Traverses the children of the typeof() expression.
    /// </summary>
    public virtual void TraverseChildren(ITypeOf typeOf) {
      Contract.Requires(typeOf != null);
      this.TraverseChildren((IExpression)typeOf);
      if (this.StopTraversal) return;
      this.Traverse(typeOf.TypeToGet);
    }

    /// <summary>
    /// Traverses the children of the unary negation expression.
    /// </summary>
    public virtual void TraverseChildren(IUnaryNegation unaryNegation) {
      Contract.Requires(unaryNegation != null);
      this.TraverseChildren((IUnaryOperation)unaryNegation);
    }

    /// <summary>
    /// Called whenever a unary operation expression is about to be traversed by a type specific routine.
    /// This gives the traverser the opportunity to take some uniform action for all unary operation expressions,
    /// regardless of how the traversal gets to them.
    /// </summary>
    public virtual void TraverseChildren(IUnaryOperation unaryOperation) {
      Contract.Requires(unaryOperation != null);
      this.TraverseChildren((IExpression)unaryOperation);
      this.Traverse(unaryOperation.Operand);
    }

    /// <summary>
    /// Traverses the children of the unary plus expression.
    /// </summary>
    public virtual void TraverseChildren(IUnaryPlus unaryPlus) {
      Contract.Requires(unaryPlus != null);
      this.TraverseChildren((IUnaryOperation)unaryPlus);
    }

    /// <summary>
    /// Traverses the children of the vector length expression.
    /// </summary>
    public virtual void TraverseChildren(IVectorLength vectorLength) {
      Contract.Requires(vectorLength != null);
      this.TraverseChildren((IExpression)vectorLength);
      if (this.StopTraversal) return;
      this.Traverse(vectorLength.Vector);
    }

    /// <summary>
    /// Traverses the children of the while do statement.
    /// </summary>
    public virtual void TraverseChildren(IWhileDoStatement whileDoStatement) {
      Contract.Requires(whileDoStatement != null);
      this.TraverseChildren((IStatement)whileDoStatement);
      if (this.StopTraversal) return;
      this.Traverse(whileDoStatement.Condition);
      if (this.StopTraversal) return;
      this.Traverse(whileDoStatement.Body);
    }

    /// <summary>
    /// Traverses the children of the yield break statement.
    /// </summary>
    public virtual void TraverseChildren(IYieldBreakStatement yieldBreakStatement) {
      Contract.Requires(yieldBreakStatement != null);
      this.TraverseChildren((IStatement)yieldBreakStatement);
    }

    /// <summary>
    /// Traverses the children of the yield return statement.
    /// </summary>
    public virtual void TraverseChildren(IYieldReturnStatement yieldReturnStatement) {
      Contract.Requires(yieldReturnStatement != null);
      this.TraverseChildren((IStatement)yieldReturnStatement);
      if (this.StopTraversal) return;
      this.Traverse(yieldReturnStatement.Expression);
    }

  }

  /// <summary>
  /// A visitor base class that traverses the code model in depth first, left to right order.
  /// </summary>
  [Obsolete("Please use CodeTraverser")]
  public class BaseCodeTraverser : BaseMetadataTraverser, ICodeVisitor {

    /// <summary>
    /// Allocates a visitor instance that traverses the code model in depth first, left to right order.
    /// </summary>
    public BaseCodeTraverser() {
    }

    #region ICodeVisitor Members

    /// <summary>
    /// Traverses the given addition.
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
    /// Traverses the given addressable expression.
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
            if (indexer != null) {
              this.Visit(indexer);
              return; //do not visit Instance again
            } else {
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
    /// Traverses the given address dereference expression.
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
    /// Traverses the given AddressOf expression.
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
    /// Traverses the given anonymous delegate expression.
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
    /// Traverses the given assert statement.
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
    /// Traverses the given assignment expression.
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
    /// Traverses the given assume statement.
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
    /// Traverses the given bitwise and expression.
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
    /// Traverses the given bitwise or expression.
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
    /// Traverses the given block expression.
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
    /// Traverses the given statement block.
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
    /// Traverses the cast-if-possible expression.
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
    /// Traverses the given catch clause.
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
    /// Traverses the given check-if-instance expression.
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
    /// Traverses the given compile time constant.
    /// </summary>
    /// <param name="constant"></param>
    public virtual void Visit(ICompileTimeConstant constant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Traverses the given conversion expression.
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
    /// Traverses the given conditional expression.
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
    /// Traverses the given conditional statement.
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
    /// Traverses the given continue statement.
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
    /// Traverses the given copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public virtual void Visit(ICopyMemoryStatement copyMemoryStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(copyMemoryStatement);
      this.Visit(copyMemoryStatement.TargetAddress);
      this.Visit(copyMemoryStatement.SourceAddress);
      this.Visit(copyMemoryStatement.NumberOfBytesToCopy);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given array creation expression.
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
    /// Traverses the given constructor call expression.
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
    /// Traverses the anonymous object creation expression.
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
    /// Traverses the given array indexer expression.
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
    /// Traverses the given bound expression.
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
    /// Traverses the given custom attribute.
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
    /// Traverses the given defalut value expression.
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
    /// Traverses the given debugger break statement.
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
    /// Traverses the given division expression.
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
    /// Traverses the given do until statement.
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
    /// Traverses the given dup value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public virtual void Visit(IDupValue popValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
    }

    /// <summary>
    /// Traverses the given empty statement.
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
    /// Traverses the given equality expression.
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
    /// Traverses the given exclusive or expression.
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
    /// Traverses the given expression.
    /// </summary>
    /// <param name="expression"></param>
    public virtual void Visit(IExpression expression) {
      if (this.stopTraversal) return;
      expression.Dispatch(this);
    }

    /// <summary>
    /// Traverses the given expression statement.
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
    /// Traverses the given fill memory statement.
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    public virtual void Visit(IFillMemoryStatement fillMemoryStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(fillMemoryStatement);
      this.Visit(fillMemoryStatement.TargetAddress);
      this.Visit(fillMemoryStatement.FillValue);
      this.Visit(fillMemoryStatement.NumberOfBytesToFill);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given foreach statement.
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
    /// Traverses the given for statement.
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
    /// Traverses the given get type of typed reference expression.
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
    /// Traverses the given get value of typed reference expression.
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
    /// Traverses the given goto statement.
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
    /// Traverses the given goto switch case statement.
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public virtual void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Traverses the given greater-than expression.
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
    /// Traverses the given greater-than-or-equal expression.
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
    /// Traverses the given labeled statement.
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
    /// Traverses the given left shift expression.
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
    /// Traverses the given less-than expression.
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
    /// Traverses the given less-than-or-equal expression.
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
    /// Traverses the given local declaration statement.
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
    /// Traverses the given lock statement.
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
    /// Traverses the given logical not expression.
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
    /// Traverses the given break statement.
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
    /// Traverses the given make typed reference expression.
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
    /// Traverses the given method definition.
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
    /// Traverses the given method body.
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
    /// Traverses the given method call.
    /// </summary>
    /// <param name="methodCall"></param>
    public virtual void Visit(IMethodCall methodCall)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodCall);
      this.Visit(methodCall.MethodToCall);
      if (!methodCall.IsStaticCall && !methodCall.IsJumpCall)
        this.Visit(methodCall.ThisArgument);
      this.Visit(methodCall.Arguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    /// <summary>
    /// Traverses the given modulus expression.
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
    /// Traverses the given multiplication expression.
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
    /// Traverses the given named argument expression.
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
    /// Traverses the given not equality expression.
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
    /// Traverses the given old value expression.
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
    /// Traverses the given one's complement expression.
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
    /// Traverses the given out argument expression.
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
    /// Traverses the given pointer call.
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
    /// Traverses the given pop value expression.
    /// </summary>
    /// <param name="popValue"></param>
    public virtual void Visit(IPopValue popValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
    }

    /// <summary>
    /// Traverses the given push statement.
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
    /// Traverses the given ref argument expression.
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
    /// Traverses the given resource usage statement.
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
    /// Traverses the rethrow statement.
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
    /// Traverses the return statement.
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
    /// Traverses the given return value expression.
    /// </summary>
    public virtual void Visit(IReturnValue returnValue)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Traverses the given right shift expression.
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
    /// Traverses the given stack array create expression.
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
    /// Traverses the given runtime argument handle expression.
    /// </summary>
    /// <param name="runtimeArgumentHandleExpression"></param>
    public virtual void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Traverses the given sizeof() expression.
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
    /// Traverses the given subtraction expression.
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
    /// Traverses the given switch case.
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
    /// Traverses the given switch statement.
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
    /// Traverses the given target expression.
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
    /// Traverses the given this reference expression.
    /// </summary>
    /// <param name="thisReference"></param>
    public virtual void Visit(IThisReference thisReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Traverses the throw statement.
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
    /// Traverses the try-catch-filter-finally statement.
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
    /// Traverses the given tokenof() expression.
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
    /// Traverses the given typeof() expression.
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
    /// Traverses the given unary negation expression.
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
    /// Traverses the given unary plus expression.
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
    /// Traverses the given vector length expression.
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
    /// Traverses the given while do statement.
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
    /// Traverses the given yield break statement.
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public virtual void Visit(IYieldBreakStatement yieldBreakStatement)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    /// <summary>
    /// Traverses the given yield return statement.
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
    /// Performs some computation on the reference to the given property definition.
    /// </summary>
    /// <param name="property">The property definition being referenced.</param>
    public virtual void VisitReference(IPropertyDefinition property) {
    }

  }

  /// <summary>
  /// A visitor base class that provides a dummy body for each method of ICodeVisitor.
  /// </summary>
  [Obsolete("Please use CodeVisitor")]
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
    /// Performs some computation with the given copy memory statement.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public virtual void Visit(ICopyMemoryStatement copyMemoryStatement) {
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
    /// Performs some computation with the given fill memory block statement.
    /// </summary>
    /// <param name="fillMemoryStatement"></param>
    public virtual void Visit(IFillMemoryStatement fillMemoryStatement) {
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
  [ContractClass(typeof(ICodeAndContractVisitorContract))]
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

  #region ICodeAndContractVisitor contract binding
  [ContractClassFor(typeof(ICodeAndContractVisitor))]
  abstract class ICodeAndContractVisitorContract : ICodeAndContractVisitor {
    #region ICodeAndContractVisitor Members

    public void Visit(ILoopContract loopContract) {
      Contract.Requires(loopContract != null);
      throw new NotImplementedException();
    }

    public void Visit(ILoopInvariant loopInvariant) {
      Contract.Requires(loopInvariant != null);
      throw new NotImplementedException();
    }

    public void Visit(IMethodContract methodContract) {
      Contract.Requires(methodContract != null);
      throw new NotImplementedException();
    }

    public void Visit(IPostcondition postCondition) {
      Contract.Requires(postCondition != null);
      throw new NotImplementedException();
    }

    public void Visit(IPrecondition precondition) {
      Contract.Requires(precondition != null);
      throw new NotImplementedException();
    }

    public void Visit(IThrownException thrownException) {
      Contract.Requires(thrownException != null);
      throw new NotImplementedException();
    }

    public void Visit(ITypeContract typeContract) {
      Contract.Requires(typeContract != null);
      throw new NotImplementedException();
    }

    public void Visit(ITypeInvariant typeInvariant) {
      Contract.Requires(typeInvariant != null);
      throw new NotImplementedException();
    }

    #endregion

    #region ICodeVisitor Members

    public void Visit(IAddition addition) {
      throw new NotImplementedException();
    }

    public void Visit(IAddressableExpression addressableExpression) {
      throw new NotImplementedException();
    }

    public void Visit(IAddressDereference addressDereference) {
      throw new NotImplementedException();
    }

    public void Visit(IAddressOf addressOf) {
      throw new NotImplementedException();
    }

    public void Visit(IAnonymousDelegate anonymousDelegate) {
      throw new NotImplementedException();
    }

    public void Visit(IArrayIndexer arrayIndexer) {
      throw new NotImplementedException();
    }

    public void Visit(IAssertStatement assertStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IAssignment assignment) {
      throw new NotImplementedException();
    }

    public void Visit(IAssumeStatement assumeStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IBitwiseAnd bitwiseAnd) {
      throw new NotImplementedException();
    }

    public void Visit(IBitwiseOr bitwiseOr) {
      throw new NotImplementedException();
    }

    public void Visit(IBlockExpression blockExpression) {
      throw new NotImplementedException();
    }

    public void Visit(IBlockStatement block) {
      throw new NotImplementedException();
    }

    public void Visit(IBreakStatement breakStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IBoundExpression boundExpression) {
      throw new NotImplementedException();
    }

    public void Visit(ICastIfPossible castIfPossible) {
      throw new NotImplementedException();
    }

    public void Visit(ICatchClause catchClause) {
      throw new NotImplementedException();
    }

    public void Visit(ICheckIfInstance checkIfInstance) {
      throw new NotImplementedException();
    }

    public void Visit(ICompileTimeConstant constant) {
      throw new NotImplementedException();
    }

    public void Visit(IConversion conversion) {
      throw new NotImplementedException();
    }

    public void Visit(IConditional conditional) {
      throw new NotImplementedException();
    }

    public void Visit(IConditionalStatement conditionalStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IContinueStatement continueStatement) {
      throw new NotImplementedException();
    }

    public void Visit(ICopyMemoryStatement copyMemoryBlock) {
      throw new NotImplementedException();
    }

    public void Visit(ICreateArray createArray) {
      throw new NotImplementedException();
    }

    public void Visit(ICreateDelegateInstance createDelegateInstance) {
      throw new NotImplementedException();
    }

    public void Visit(ICreateObjectInstance createObjectInstance) {
      throw new NotImplementedException();
    }

    public void Visit(IDebuggerBreakStatement debuggerBreakStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IDefaultValue defaultValue) {
      throw new NotImplementedException();
    }

    public void Visit(IDivision division) {
      throw new NotImplementedException();
    }

    public void Visit(IDoUntilStatement doUntilStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IDupValue dupValue) {
      throw new NotImplementedException();
    }

    public void Visit(IEmptyStatement emptyStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IEquality equality) {
      throw new NotImplementedException();
    }

    public void Visit(IExclusiveOr exclusiveOr) {
      throw new NotImplementedException();
    }

    public void Visit(IExpressionStatement expressionStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IFillMemoryStatement fillMemoryStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IForEachStatement forEachStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IForStatement forStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IGotoStatement gotoStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IGotoSwitchCaseStatement gotoSwitchCaseStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IGetTypeOfTypedReference getTypeOfTypedReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGetValueOfTypedReference getValueOfTypedReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGreaterThan greaterThan) {
      throw new NotImplementedException();
    }

    public void Visit(IGreaterThanOrEqual greaterThanOrEqual) {
      throw new NotImplementedException();
    }

    public void Visit(ILabeledStatement labeledStatement) {
      throw new NotImplementedException();
    }

    public void Visit(ILeftShift leftShift) {
      throw new NotImplementedException();
    }

    public void Visit(ILessThan lessThan) {
      throw new NotImplementedException();
    }

    public void Visit(ILessThanOrEqual lessThanOrEqual) {
      throw new NotImplementedException();
    }

    public void Visit(ILocalDeclarationStatement localDeclarationStatement) {
      throw new NotImplementedException();
    }

    public void Visit(ILockStatement lockStatement) {
      throw new NotImplementedException();
    }

    public void Visit(ILogicalNot logicalNot) {
      throw new NotImplementedException();
    }

    public void Visit(IMakeTypedReference makeTypedReference) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodCall methodCall) {
      throw new NotImplementedException();
    }

    public void Visit(IModulus modulus) {
      throw new NotImplementedException();
    }

    public void Visit(IMultiplication multiplication) {
      throw new NotImplementedException();
    }

    public void Visit(INamedArgument namedArgument) {
      throw new NotImplementedException();
    }

    public void Visit(INotEquality notEquality) {
      throw new NotImplementedException();
    }

    public void Visit(IOldValue oldValue) {
      throw new NotImplementedException();
    }

    public void Visit(IOnesComplement onesComplement) {
      throw new NotImplementedException();
    }

    public void Visit(IOutArgument outArgument) {
      throw new NotImplementedException();
    }

    public void Visit(IPointerCall pointerCall) {
      throw new NotImplementedException();
    }

    public void Visit(IPopValue popValue) {
      throw new NotImplementedException();
    }

    public void Visit(IPushStatement pushStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IRefArgument refArgument) {
      throw new NotImplementedException();
    }

    public void Visit(IResourceUseStatement resourceUseStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IReturnValue returnValue) {
      throw new NotImplementedException();
    }

    public void Visit(IRethrowStatement rethrowStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IReturnStatement returnStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IRightShift rightShift) {
      throw new NotImplementedException();
    }

    public void Visit(IRuntimeArgumentHandleExpression runtimeArgumentHandleExpression) {
      throw new NotImplementedException();
    }

    public void Visit(ISizeOf sizeOf) {
      throw new NotImplementedException();
    }

    public void Visit(IStackArrayCreate stackArrayCreate) {
      throw new NotImplementedException();
    }

    public void Visit(ISubtraction subtraction) {
      throw new NotImplementedException();
    }

    public void Visit(ISwitchCase switchCase) {
      throw new NotImplementedException();
    }

    public void Visit(ISwitchStatement switchStatement) {
      throw new NotImplementedException();
    }

    public void Visit(ITargetExpression targetExpression) {
      throw new NotImplementedException();
    }

    public void Visit(IThisReference thisReference) {
      throw new NotImplementedException();
    }

    public void Visit(IThrowStatement throwStatement) {
      throw new NotImplementedException();
    }

    public void Visit(ITryCatchFinallyStatement tryCatchFilterFinallyStatement) {
      throw new NotImplementedException();
    }

    public void Visit(ITokenOf tokenOf) {
      throw new NotImplementedException();
    }

    public void Visit(ITypeOf typeOf) {
      throw new NotImplementedException();
    }

    public void Visit(IUnaryNegation unaryNegation) {
      throw new NotImplementedException();
    }

    public void Visit(IUnaryPlus unaryPlus) {
      throw new NotImplementedException();
    }

    public void Visit(IVectorLength vectorLength) {
      throw new NotImplementedException();
    }

    public void Visit(IWhileDoStatement whileDoStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IYieldBreakStatement yieldBreakStatement) {
      throw new NotImplementedException();
    }

    public void Visit(IYieldReturnStatement yieldReturnStatement) {
      throw new NotImplementedException();
    }

    #endregion

    #region IMetadataVisitor Members

    public void Visit(IArrayTypeReference arrayTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IAssembly assembly) {
      throw new NotImplementedException();
    }

    public void Visit(IAssemblyReference assemblyReference) {
      throw new NotImplementedException();
    }

    public void Visit(ICustomAttribute customAttribute) {
      throw new NotImplementedException();
    }

    public void Visit(ICustomModifier customModifier) {
      throw new NotImplementedException();
    }

    public void Visit(IEventDefinition eventDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IFieldDefinition fieldDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IFieldReference fieldReference) {
      throw new NotImplementedException();
    }

    public void Visit(IFileReference fileReference) {
      throw new NotImplementedException();
    }

    public void Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericMethodInstanceReference genericMethodInstanceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericMethodParameter genericMethodParameter) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGlobalFieldDefinition globalFieldDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IGlobalMethodDefinition globalMethodDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericTypeParameter genericTypeParameter) {
      throw new NotImplementedException();
    }

    public void Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      throw new NotImplementedException();
    }

    public void Visit(ILocalDefinition localDefinition) {
      throw new NotImplementedException();
    }

    public void VisitReference(ILocalDefinition localDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IManagedPointerTypeReference managedPointerTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IMarshallingInformation marshallingInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataConstant constant) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataCreateArray createArray) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataExpression expression) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataNamedArgument namedArgument) {
      throw new NotImplementedException();
    }

    public void Visit(IMetadataTypeOf typeOf) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodBody methodBody) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodDefinition method) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodImplementation methodImplementation) {
      throw new NotImplementedException();
    }

    public void Visit(IMethodReference methodReference) {
      throw new NotImplementedException();
    }

    public void Visit(IModifiedTypeReference modifiedTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IModule module) {
      throw new NotImplementedException();
    }

    public void Visit(IModuleReference moduleReference) {
      throw new NotImplementedException();
    }

    public void Visit(INamespaceAliasForType namespaceAliasForType) {
      throw new NotImplementedException();
    }

    public void Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(INamespaceTypeReference namespaceTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(INestedAliasForType nestedAliasForType) {
      throw new NotImplementedException();
    }

    public void Visit(INestedTypeDefinition nestedTypeDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(INestedTypeReference nestedTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(INestedUnitNamespace nestedUnitNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      throw new NotImplementedException();
    }

    public void Visit(INestedUnitSetNamespace nestedUnitSetNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(IOperation operation) {
      throw new NotImplementedException();
    }

    public void Visit(IOperationExceptionInformation operationExceptionInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IParameterDefinition parameterDefinition) {
      throw new NotImplementedException();
    }

    public void VisitReference(IParameterDefinition parameterDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IParameterTypeInformation parameterTypeInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IPESection peSection) {
      throw new NotImplementedException();
    }

    public void Visit(IPlatformInvokeInformation platformInvokeInformation) {
      throw new NotImplementedException();
    }

    public void Visit(IPointerTypeReference pointerTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IPropertyDefinition propertyDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(IResourceReference resourceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IRootUnitNamespace rootUnitNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      throw new NotImplementedException();
    }

    public void Visit(IRootUnitSetNamespace rootUnitSetNamespace) {
      throw new NotImplementedException();
    }

    public void Visit(ISecurityAttribute securityAttribute) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedEventDefinition specializedEventDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedFieldDefinition specializedFieldDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedFieldReference specializedFieldReference) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedMethodDefinition specializedMethodDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedMethodReference specializedMethodReference) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedPropertyDefinition specializedPropertyDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedNestedTypeDefinition specializedNestedTypeDefinition) {
      throw new NotImplementedException();
    }

    public void Visit(ISpecializedNestedTypeReference specializedNestedTypeReference) {
      throw new NotImplementedException();
    }

    public void Visit(IUnitSet unitSet) {
      throw new NotImplementedException();
    }

    public void Visit(IWin32Resource win32Resource) {
      throw new NotImplementedException();
    }

    #endregion

  }
  #endregion


  /// <summary>
  /// Contains a specialized Visit routine for each standard type of object defined in the contract, code and metadata model. 
  /// </summary>
  public class CodeAndContractVisitor : CodeVisitor, ICodeAndContractVisitor {

    /// <summary>
    /// A map from code model objects to contract objects.
    /// </summary>
    protected readonly IContractProvider/*?*/ contractProvider;

    /// <summary>
    /// Contains a specialized Visit routine for each standard type of object defined in the contract, code and metadata model. 
    /// </summary>
    public CodeAndContractVisitor(IContractProvider/*?*/ contractProvider) {
      this.contractProvider = contractProvider;
    }

    /// <summary>
    /// Visits the given contract element.
    /// </summary>
    /// <param name="contractElement"></param>
    public virtual void Visit(IContractElement contractElement) {
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
      this.Visit((IContractElement)loopInvariant);
    }

    /// <summary>
    /// Visits the given method contract.
    /// </summary>
    public virtual void Visit(IMethodContract methodContract) {
    }

    /// <summary>
    /// Visits the given postCondition.
    /// </summary>
    public virtual void Visit(IPostcondition postCondition) {
      this.Visit((IContractElement)postCondition);
    }

    /// <summary>
    /// Visits the given precondition.
    /// </summary>
    public virtual void Visit(IPrecondition precondition) {
      this.Visit((IContractElement)precondition);
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
      this.Visit((IContractElement)typeInvariant);
    }

  }

  /// <summary>
  /// A class that traverses the contract, code and metadata models in depth first, left to right order.
  /// </summary>
  public class CodeAndContractTraverser : CodeTraverser {

    /// <summary>
    /// A class that traverses the contract, code and metadata models in depth first, left to right order.
    /// </summary>
    public CodeAndContractTraverser(IContractProvider/*?*/ contractProvider) {
      this.contractProvider = contractProvider;
      this.dispatchingVisitor = new ContractElementDispatcher() { traverser = this };
    }

    /// <summary>
    /// A map from code model objects to contract objects.
    /// </summary>
    protected readonly IContractProvider/*?*/ contractProvider;

    ICodeAndContractVisitor/*?*/ preorderVisitor;
    ICodeAndContractVisitor/*?*/ postorderVisitor;

    /// <summary>
    /// A visitor that should be called on each object being traversed, before any of its children are traversed. May be null.
    /// </summary>
    public new ICodeAndContractVisitor/*?*/ PreorderVisitor {
      get { return this.preorderVisitor; }
      set {
        this.preorderVisitor = value;
        base.PreorderVisitor = value;
      }
    }

    /// <summary>
    /// A visitor that should be called on each object being traversed, after all of its children are traversed. May be null. 
    /// </summary>
    public new ICodeAndContractVisitor/*?*/ PostorderVisitor {
      get { return this.postorderVisitor; }
      set {
        this.postorderVisitor = value;
        base.PostorderVisitor = value;
      }
    }

    ContractElementDispatcher dispatchingVisitor;
    class ContractElementDispatcher : CodeVisitor, ICodeAndContractVisitor {

      internal CodeAndContractTraverser traverser;

      public void Visit(ILoopInvariant loopInvariant) {
        this.traverser.Traverse(loopInvariant);
      }

      public void Visit(IPostcondition postCondition) {
        this.traverser.Traverse(postCondition);
      }

      public void Visit(IPrecondition precondition) {
        this.traverser.Traverse(precondition);
      }

      public void Visit(ITypeInvariant typeInvariant) {
        this.traverser.Traverse(typeInvariant);
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
    /// Traverses the contract element.
    /// </summary>
    public void Traverse(IContractElement contractElement) {
      Contract.Requires(contractElement != null);
      contractElement.Dispatch(this.dispatchingVisitor);
    }

    /// <summary>
    /// Traverses the enumeration of addressable expressions.
    /// </summary>
    public virtual void Traverse(IEnumerable<IAddressableExpression> addressableExpressions) {
      Contract.Requires(addressableExpressions != null);
      foreach (var addressableExpression in addressableExpressions) {
        this.Traverse(addressableExpression);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of trigger expressions.
    /// </summary>
    public virtual void Traverse(IEnumerable<IEnumerable<IExpression>> triggers) {
      Contract.Requires(triggers != null);
      foreach (var trigs in triggers) {
        this.Traverse(trigs);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of loop invariants.
    /// </summary>
    public virtual void Traverse(IEnumerable<ILoopInvariant> loopInvariants) {
      Contract.Requires(loopInvariants != null);
      foreach (var loopInvariant in loopInvariants) {
        this.Traverse(loopInvariant);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of post conditions.
    /// </summary>
    public virtual void Traverse(IEnumerable<IPostcondition> postConditions) {
      Contract.Requires(postConditions != null);
      foreach (var postCondition in postConditions) {
        this.Traverse(postCondition);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of pre conditions.
    /// </summary>
    public virtual void Traverse(IEnumerable<IPrecondition> preconditions) {
      Contract.Requires(preconditions != null);
      foreach (var precondition in preconditions) {
        this.Traverse(precondition);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of thrown exceptions.
    /// </summary>
    public virtual void Traverse(IEnumerable<IThrownException> thrownExceptions) {
      Contract.Requires(thrownExceptions != null);
      foreach (var thrownException in thrownExceptions) {
        this.Traverse(thrownException);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the enumeration of addressable expressions.
    /// </summary>
    public virtual void Traverse(IEnumerable<ITypeInvariant> typeInvariants) {
      Contract.Requires(typeInvariants != null);
      foreach (var typeInvariant in typeInvariants) {
        this.Traverse(typeInvariant);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the loop contract.
    /// </summary>
    public void Traverse(ILoopContract loopContract) {
      Contract.Requires(loopContract != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(loopContract);
      if (this.StopTraversal) return;
      this.TraverseChildren(loopContract);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(loopContract);
    }

    /// <summary>
    /// Traverses the loop invariant.
    /// </summary>
    public void Traverse(ILoopInvariant loopInvariant) {
      Contract.Requires(loopInvariant != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(loopInvariant);
      if (this.StopTraversal) return;
      this.TraverseChildren(loopInvariant);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(loopInvariant);
    }

    /// <summary>
    /// Traverses the method contract.
    /// </summary>
    public void Traverse(IMethodContract methodContract) {
      Contract.Requires(methodContract != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(methodContract);
      if (this.StopTraversal) return;
      this.TraverseChildren(methodContract);
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(methodContract);
      if (this.StopTraversal) return;
    }

    /// <summary>
    /// Traverses the postCondition.
    /// </summary>
    public void Traverse(IPostcondition postCondition) {
      Contract.Requires(postCondition != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(postCondition);
      if (this.StopTraversal) return;
      this.TraverseChildren(postCondition);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(postCondition);
    }

    /// <summary>
    /// Traverses the pre condition.
    /// </summary>
    public void Traverse(IPrecondition precondition) {
      Contract.Requires(precondition != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(precondition);
      if (this.StopTraversal) return;
      this.TraverseChildren(precondition);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(precondition);
    }

    /// <summary>
    /// Traverses the thrown exception.
    /// </summary>
    public void Traverse(IThrownException thrownException) {
      Contract.Requires(thrownException != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(thrownException);
      if (this.StopTraversal) return;
      this.TraverseChildren(thrownException);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(thrownException);
    }

    /// <summary>
    /// Traverses the type contract.
    /// </summary>
    public void Traverse(ITypeContract typeContract) {
      Contract.Requires(typeContract != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(typeContract);
      if (this.StopTraversal) return;
      this.TraverseChildren(typeContract);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(typeContract);
    }

    /// <summary>
    /// Traverses the type invariant.
    /// </summary>
    public void Traverse(ITypeInvariant typeInvariant) {
      Contract.Requires(typeInvariant != null);
      if (this.preorderVisitor != null) this.preorderVisitor.Visit(typeInvariant);
      if (this.StopTraversal) return;
      this.TraverseChildren(typeInvariant);
      if (this.StopTraversal) return;
      if (this.postorderVisitor != null) this.postorderVisitor.Visit(typeInvariant);
    }

    /// <summary>
    /// Called whenever a contract element is about to be traversed by a type specific routine.
    /// This gives the traverser the opportunity to take some uniform action for all contract elements,
    /// regardless of how the traversal gets to them.
    /// </summary>
    public virtual void TraverseChildren(IContractElement contractElement) {
      Contract.Requires(contractElement != null);
      this.Traverse(contractElement.Condition);
      if (this.StopTraversal) return;
      if (contractElement.Description != null) {
        this.Traverse(contractElement.Description);
        if (this.StopTraversal) return;
      }
    }

    /// <summary>
    /// Traverses the children of the loop contract.
    /// </summary>
    public virtual void TraverseChildren(ILoopContract loopContract) {
      Contract.Requires(loopContract != null);
      this.Traverse(loopContract.Invariants);
      if (this.StopTraversal) return;
      this.Traverse(loopContract.Variants);
      if (this.StopTraversal) return;
      this.Traverse(loopContract.Writes);
    }

    /// <summary>
    /// Traverses the children of the loop invariant.
    /// </summary>
    public virtual void TraverseChildren(ILoopInvariant loopInvariant) {
      Contract.Requires(loopInvariant != null);
      this.TraverseChildren((IContractElement)loopInvariant);
    }

    /// <summary>
    /// Traverses the children of the method call.
    /// </summary>
    public override void TraverseChildren(IMethodCall methodCall) {
      base.TraverseChildren(methodCall);
      if (this.StopTraversal) return;
      if (this.contractProvider == null) return;
      IEnumerable<IEnumerable<IExpression>>/*?*/ triggers = this.contractProvider.GetTriggersFor(methodCall);
      if (triggers != null)
        this.Traverse(triggers);
    }

    /// <summary>
    /// Traverses the children of the method contract.
    /// </summary>
    public virtual void TraverseChildren(IMethodContract methodContract) {
      Contract.Requires(methodContract != null);
      this.Traverse(methodContract.Allocates);
      if (this.StopTraversal) return;
      this.Traverse(methodContract.Frees);
      if (this.StopTraversal) return;
      this.Traverse(methodContract.ModifiedVariables);
      if (this.StopTraversal) return;
      this.Traverse(methodContract.Postconditions);
      if (this.StopTraversal) return;
      this.Traverse(methodContract.Preconditions);
      if (this.StopTraversal) return;
      this.Traverse(methodContract.Reads);
      if (this.StopTraversal) return;
      this.Traverse(methodContract.ThrownExceptions);
      if (this.StopTraversal) return;
      this.Traverse(methodContract.Writes);
    }

    /// <summary>
    /// Traverses the children of the method definition.
    /// </summary>
    public override void TraverseChildren(IMethodDefinition method) {
      if (this.contractProvider != null) {
        IMethodContract/*?*/ methodContract = this.contractProvider.GetMethodContractFor(method);
        if (methodContract != null) {
          this.Traverse(methodContract);
          if (this.StopTraversal) return;
        }
      }
      base.TraverseChildren(method);
    }

    /// <summary>
    /// Traverses the children of the postCondition.
    /// </summary>
    public virtual void TraverseChildren(IPostcondition postCondition) {
      Contract.Requires(postCondition != null);
      this.TraverseChildren((IContractElement)postCondition);
    }

    /// <summary>
    /// Traverses the children of the pre condition.
    /// </summary>
    public virtual void TraverseChildren(IPrecondition precondition) {
      Contract.Requires(precondition != null);
      this.TraverseChildren((IContractElement)precondition);
      if (this.StopTraversal) return;
      if (precondition.ExceptionToThrow != null)
        this.Traverse(precondition.ExceptionToThrow);
    }

    /// <summary>
    /// Traverses the children of the statement.
    /// </summary>
    public override void TraverseChildren(IStatement statement) {
      base.TraverseChildren(statement);
      if (this.contractProvider == null) return;
      ILoopContract/*?*/ loopContract = this.contractProvider.GetLoopContractFor(statement);
      if (loopContract != null)
        this.Traverse(loopContract);
    }

    /// <summary>
    /// Traverses the children of the thrown exception.
    /// </summary>
    public virtual void TraverseChildren(IThrownException thrownException) {
      Contract.Requires(thrownException != null);
      this.Traverse(thrownException.ExceptionType);
      if (this.StopTraversal) return;
      this.Traverse(thrownException.Postcondition);
    }

    /// <summary>
    /// Traverses the children of the type contract.
    /// </summary>
    public virtual void TraverseChildren(ITypeContract typeContract) {
      Contract.Requires(typeContract != null);
      this.Traverse(typeContract.ContractFields);
      if (this.StopTraversal) return;
      this.Traverse(typeContract.ContractMethods);
      if (this.StopTraversal) return;
      this.Traverse(typeContract.Invariants);
    }

    /// <summary>
    /// Traverses the children of the type definition.
    /// </summary>
    public override void TraverseChildren(ITypeDefinition typeDefinition) {
      base.TraverseChildren(typeDefinition);
      if (this.StopTraversal) return;
      if (this.contractProvider == null) return;
      ITypeContract/*?*/ typeContract = this.contractProvider.GetTypeContractFor(typeDefinition);
      if (typeContract != null)
        this.Traverse(typeContract);
    }

    /// <summary>
    /// Traverses the children of the type invariant.
    /// </summary>
    public virtual void TraverseChildren(ITypeInvariant typeInvariant) {
      Contract.Requires(typeInvariant != null);
      this.TraverseChildren((IContractElement)typeInvariant);
    }

  }

  /// <summary>
  /// A visitor base class that traverses a code model in depth first, left to right order.
  /// </summary>
  [Obsolete("Please use CodeAndContractVisitor")]
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
  [Obsolete("Please use CodeAndContractVisitor")]
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
    /// Visits the given method contract.
    /// </summary>
    public virtual void Visit(IMethodContract methodContract) {
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

