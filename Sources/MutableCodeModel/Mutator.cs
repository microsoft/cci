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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// Uses the inherited methods from MetadataMutator to walk everything down to the method body level,
  /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
  /// </summary>
  /// <remarks>While the model is being copied, the resulting model is incomplete and or inconsistent. It should not be traversed
  /// independently nor should any of its computed properties, such as ResolvedType be evaluated. Scenarios that need such functionality
  /// should be implemented by first making a mutable copy of the entire assembly and then running a second pass over the mutable result.
  /// The new classes CodeCopier and CodeMutatingVisitor are meant to facilitate such scenarios.
  /// </remarks>
  [Obsolete("This class has been superceded by CodeCopier and CodeMutatingVisitor, used in combination. It will go away after May 2011")]
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
          mutableSourceMethodBody = new SourceMethodBody(this.host, this.sourceLocationProvider, null);
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
      if (!methodCall.IsStaticCall)
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
  /// Uses the inherited methods from MetadataMutator to walk everything down to the method body level,
  /// then takes over and define Visit methods for all of the structures in the code model that pertain to method bodies.
  /// Also visits and mutates the associated code contracts and establishes associations with new copies.
  /// </summary>
  /// <remarks>While the model is being copied, the resulting model is incomplete and or inconsistent. It should not be traversed
  /// independently nor should any of its computed properties, such as ResolvedType be evaluated. Scenarios that need such functionality
  /// should be implemented by first making a mutable copy of the entire assembly and then running a second pass over the mutable result.
  /// The new classes CodeAndContractCopier and CodeAndContractMutatingVisitor are meant to facilitate such scenarios.
  /// </remarks>
  [Obsolete("This class has been superceded by CodeAndContractCopier and CodeAndContractMutatingVisitor, used in combination. It will go away after April 2011")]
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
      PostCondition mutablePostCondition = postCondition as PostCondition;
      if (!this.copyOnlyIfNotAlreadyMutable || mutablePostCondition == null)
        mutablePostCondition = new PostCondition(postCondition);
      return this.Visit(mutablePostCondition);
    }

    /// <summary>
    /// Visits the specified post condition.
    /// </summary>
    /// <param name="postCondition">The post condition.</param>
    public virtual IPostcondition Visit(PostCondition postCondition) {
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
  /// 
  /// </summary>
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
      PostCondition mutablePostCondition = postCondition as PostCondition;
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

  /// <summary>
  /// Use this as a base class when you define a code mutator that mutates ONLY method bodies (in other words
  /// all metadata definitions, including parameter definitions and local definition remain unchanged).
  /// This class has overrides for Visit(IFieldReference), Visit(IMethodReference), 
  /// Visit(ITypeReference), VisitReferenceTo(ILocalDefinition) and VisitReferenceTo(IParameterDefinition) that make sure to not modify the references.
  /// </summary>
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
    /// <param name="methodReference">The method reference.</param>
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
  /// Use this as a base class when you define a code and contract mutator that mutates ONLY
  /// method bodies and their contracts.
  /// This class has overrides for Visit(IFieldReference), Visit(IMethodReference), and
  /// Visit(ITypeReference) that make sure to not modify the references.
  /// </summary>
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
    /// <param name="methodReference">The method reference.</param>
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
      if (!methodCall.IsStaticCall)
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
    private class CreateMutableType : BaseCodeVisitor, ICodeVisitor {

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
