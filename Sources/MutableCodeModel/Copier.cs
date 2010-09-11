//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci;
using System.Diagnostics;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {
  /// <summary>
  /// Provides copy of a method body, a statement, or an expression, in which the references to the nodes
  /// inside a cone is replaced. The cone is defined using the parent class. 
  /// </summary>
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
      : base(host, rootOfCone, out newTypes)
    {
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
        SourceMethodBody mutableSourceMethodBody = new SourceMethodBody(this.host, this.sourceLocationProvider, null);
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
    /// Visits the specified base class reference.
    /// </summary>
    /// <param name="baseClassReference">The base class reference.</param>
    /// <returns></returns>
    protected virtual IExpression DeepCopy(BaseClassReference baseClassReference) {
      baseClassReference.Type = this.Substitute(baseClassReference.Type);
      return baseClassReference;
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
      if (!methodCall.IsStaticCall)
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
      outArgument.Expression = (ITargetExpression) Substitute(outArgument.Expression);
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
        switchCase.Expression = (ICompileTimeConstant) Substitute(switchCase.Expression);
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
    private class CreateMutableType : BaseCodeVisitor, ICodeVisitor {
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
      /// Visits the specified base class reference.
      /// </summary>
      /// <param name="baseClassReference">The base class reference.</param>
      public override void Visit(IBaseClassReference baseClassReference) {
        BaseClassReference mutableBaseClassReference = new BaseClassReference(baseClassReference);
        this.resultExpression = this.myCodeCopier.DeepCopy(mutableBaseClassReference);
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
