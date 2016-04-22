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

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A statement that asserts that a condition must always be true when execution reaches it. For example the assert statement of Spec#.
  /// </summary>
  public class AssertStatement : Statement, IAssertStatement {

    /// <summary>
    /// Allocates a statement that asserts that a condition must always be true when execution reaches it. For example the assert statement of Spec#.
    /// </summary>
    public AssertStatement() {
      this.condition = CodeDummy.Expression;
      this.description = null;
      this.conditionAsText = null;
    }

    /// <summary>
    /// Allocates a copy of a statement that asserts that a condition must always be true when execution reaches it. For example the assert statement of Spec#.
    /// </summary>
    /// <param name="assertStatement"></param>
    public AssertStatement(IAssertStatement assertStatement)
      : base(assertStatement) {
      this.condition = assertStatement.Condition;
      this.description = assertStatement.Description;
      this.conditionAsText = assertStatement.OriginalSource;
      this.hasBeenVerified = assertStatement.HasBeenVerified;
    }

    /// <summary>
    /// A condition that must be true when execution reaches this statement.
    /// </summary>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// Calls the visitor.Visit(IAssertStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if a static verification tool has determined that the condition will always be true when execution reaches this statement.
    /// </summary>
    public bool HasBeenVerified {
      get { return this.hasBeenVerified; }
      set { this.hasBeenVerified = value; }
    }
    bool hasBeenVerified;


    /// <summary>
    /// An optional expression that is associated with the assertion. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    public IExpression/*?*/ Description {
      get { return this.description; }
      set { this.description = value; }
    }
    IExpression description;

    /// <summary>
    /// The original source representation of the assertion.
    /// </summary>
    /// <remarks>
    /// Normally this would be extracted directly from the source file.
    /// The expectation is that one would translate the Condition into
    /// a source string in a source language appropriate for
    /// the particular tool environment, e.g., when doing static analysis,
    /// in the language the client code uses, not the language the contract
    /// was written.
    /// </remarks>
    public string/*?*/ OriginalSource {
      get { return this.conditionAsText; }
      set { this.conditionAsText = value; }
    }
    string conditionAsText;

  }

  /// <summary>
  /// A statement that asserts that a condition will always be true when execution reaches it. For example the assume statement of Spec#
  /// or a call to System.Diagnostics.Assert in C#.
  /// </summary>
  public class AssumeStatement : Statement, IAssumeStatement {

    /// <summary>
    /// 
    /// </summary>
    public AssumeStatement() {
      this.condition = CodeDummy.Expression;
      this.description = null;
      this.conditionAsText = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assumeStatement"></param>
    public AssumeStatement(IAssumeStatement assumeStatement)
      : base(assumeStatement) {
      this.condition = assumeStatement.Condition;
      this.description = assumeStatement.Description;
      this.conditionAsText = assumeStatement.OriginalSource;
    }

    /// <summary>
    /// A condition that must be true when execution reaches this statement.
    /// </summary>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// Calls visitor.Visit(IAssumeStatement).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An optional expression that is associated with the assumption. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    public IExpression/*?*/ Description {
      get { return this.description; }
      set { this.description = value; }
    }
    IExpression description;

    /// <summary>
    /// The original source representation of the assumption.
    /// </summary>
    /// <remarks>
    /// Normally this would be extracted directly from the source file.
    /// The expectation is that one would translate the Condition into
    /// a source string in a source language appropriate for
    /// the particular tool environment, e.g., when doing static analysis,
    /// in the language the client code uses, not the language the contract
    /// was written.
    /// </remarks>
    public string/*?*/ OriginalSource {
      get { return this.conditionAsText; }
      set { this.conditionAsText = value; }
    }
    string conditionAsText;

  }

  /// <summary>
  /// 
  /// </summary>
  public class BlockStatement : Statement, IBlockStatement {

    /// <summary>
    /// 
    /// </summary>
    public BlockStatement() {
      this.statements = new List<IStatement>();
      this.useCheckedArithmetic = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="blockStatement"></param>
    public BlockStatement(IBlockStatement blockStatement)
      : base(blockStatement) {
      this.statements = new List<IStatement>(blockStatement.Statements);
      this.useCheckedArithmetic = blockStatement.UseCheckedArithmetic;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.statements != null);
    }


    /// <summary>
    /// Calls visitor.Visit(IBlockStatement).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The statements making up the block.
    /// </summary>
    /// <value></value>
    public List<IStatement> Statements {
      get {
        Contract.Ensures(Contract.Result<List<IStatement>>() != null);
        return this.statements;
      }
      set {
        Contract.Requires(value != null);
        this.statements = value;
      }
    }
    List<IStatement> statements;

    /// <summary>
    /// True if, by default, all arithmetic expressions in the block must be checked for overflow. This setting is inherited by nested blocks and
    /// can be overridden by nested blocks and expressions.
    /// </summary>
    /// <value></value>
    public bool UseCheckedArithmetic {
      get { return this.useCheckedArithmetic; }
      set { this.useCheckedArithmetic = value; }
    }
    bool useCheckedArithmetic;

    #region IBlock Members

    IEnumerable<IStatement> IBlockStatement.Statements {
      get { return this.statements.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class BreakStatement : Statement, IBreakStatement {

    /// <summary>
    /// 
    /// </summary>
    public BreakStatement() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="breakStatement"></param>
    public BreakStatement(IBreakStatement breakStatement)
      : base(breakStatement) {
    }

    /// <summary>
    /// Calls visitor.Visit(IBreakStatement).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class CatchClause : ICatchClause {

    /// <summary>
    /// 
    /// </summary>
    public CatchClause() {
      this.body = CodeDummy.Block;
      this.exceptionContainer = Dummy.LocalVariable;
      this.exceptionType = Dummy.TypeReference;
      this.filterCondition = null;
      this.locations = new List<ILocation>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="catchClause"></param>
    public CatchClause(ICatchClause catchClause) {
      this.body = catchClause.Body;
      this.exceptionContainer = catchClause.ExceptionContainer;
      this.exceptionType = catchClause.ExceptionType;
      this.filterCondition = catchClause.FilterCondition;
      this.locations = new List<ILocation>(catchClause.Locations);
    }

    /// <summary>
    /// The statements within the catch clause.
    /// </summary>
    /// <value></value>
    public IBlockStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IBlockStatement body;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The local that contains the exception instance when executing the catch clause body. 
    /// If there is no such local, Dummy.LocalVariable is returned.
    /// </summary>
    /// <value></value>
    public ILocalDefinition ExceptionContainer {
      get { return this.exceptionContainer; }
      set { this.exceptionContainer = value; }
    }
    ILocalDefinition exceptionContainer;

    /// <summary>
    /// The type of the exception to handle.
    /// </summary>
    /// <value></value>
    public ITypeReference ExceptionType {
      get { return this.exceptionType; }
      set { this.exceptionType = value; }
    }
    ITypeReference exceptionType;

    /// <summary>
    /// A condition that must evaluate to true if the catch clause is to be executed.
    /// May be null, in which case any exception of ExceptionType will cause the handler to execute.
    /// </summary>
    /// <value></value>
    public IExpression/*?*/ FilterCondition {
      get { return this.filterCondition; }
      set { this.filterCondition = value; }
    }
    IExpression/*?*/ filterCondition;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IList<ILocation> Locations {
      get { return this.locations; }
    }
    List<ILocation> locations;

    #region ICatchClause Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class ConditionalStatement : Statement, IConditionalStatement {

    /// <summary>
    /// 
    /// </summary>
    public ConditionalStatement() {
      this.condition = CodeDummy.Expression;
      this.falseBranch = CodeDummy.Block;
      this.trueBranch = CodeDummy.Block;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="conditionalStatement"></param>
    public ConditionalStatement(IConditionalStatement conditionalStatement)
      : base(conditionalStatement) {
      this.condition = conditionalStatement.Condition;
      this.falseBranch = conditionalStatement.FalseBranch;
      this.trueBranch = conditionalStatement.TrueBranch;
    }

    /// <summary>
    /// Returns a shallow copy of this ConditionalStatement.
    /// </summary>
    public virtual ConditionalStatement Clone() {
      return new ConditionalStatement(this);
    }

    /// <summary>
    /// The expression to evaluate as true or false.
    /// </summary>
    /// <value></value>
    public IExpression Condition {
      get { return this.condition; }
      set {
        Contract.Requires(value != null);
        //Contract.Requires(value.Type.TypeCode == PrimitiveTypeCode.Boolean);
        this.condition = value; 
      }
    }
    IExpression condition;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Statement to execute if the conditional expression evaluates to false.
    /// </summary>
    /// <value></value>
    public IStatement FalseBranch {
      get { return this.falseBranch; }
      set {
        Contract.Requires(value != null);
        this.falseBranch = value; 
      }
    }
    IStatement falseBranch;

    /// <summary>
    /// Statement to execute if the conditional expression evaluates to true.
    /// </summary>
    /// <value></value>
    public IStatement TrueBranch {
      get { return this.trueBranch; }
      set {
        Contract.Requires(value != null);
        this.trueBranch = value; 
      }
    }
    IStatement trueBranch;

  }

  /// <summary>
  /// 
  /// </summary>
  public class ContinueStatement : Statement, IContinueStatement {

    /// <summary>
    /// 
    /// </summary>
    public ContinueStatement() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="continueStatement"></param>
    public ContinueStatement(IContinueStatement continueStatement)
      : base(continueStatement) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// Represents the cpblk IL instruction, which copies a block of memory from one address to another.
  /// The behavior of this instruction is undefined if the source block overlaps the target block.
  /// </summary>
  public class CopyMemoryStatement : Statement, ICopyMemoryStatement {

    /// <summary>
    /// Represents the cpblk IL instruction, which copies a block of memory from one address to another.
    /// The behavior of this instruction is undefined if the source block overlaps the target block.
    /// </summary>
    public CopyMemoryStatement() {
      this.targetAddress = CodeDummy.Expression;
      this.sourceAddress = CodeDummy.Expression;
      this.numberOfBytesToCopy = CodeDummy.Expression;
    }

    /// <summary>
    /// Makes a shallow copy of a statement that represents cpblk IL instruction, which copies a block of memory from one address to another.
    /// </summary>
    /// <param name="copyMemoryStatement"></param>
    public CopyMemoryStatement(ICopyMemoryStatement copyMemoryStatement)
      : base(copyMemoryStatement) {
      this.targetAddress = copyMemoryStatement.TargetAddress;
      this.sourceAddress = copyMemoryStatement.SourceAddress;
      this.numberOfBytesToCopy = copyMemoryStatement.NumberOfBytesToCopy;
    }

    /// <summary>
    /// Calls the visitor.Visit(ICopyMemoryStatement).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    #region ICopyMemoryStatement Members

    /// <summary>
    /// A pointer to the block of memory that is overwritten with the contents of block at SourceAddress.
    /// </summary>
    public IExpression TargetAddress {
      get { return this.targetAddress; }
      set { this.targetAddress = value; }
    }
    IExpression targetAddress;

    /// <summary>
    /// A pointer to the block of memory whose contents is to be copied to the block of memory at TargetAddress.
    /// </summary>
    public IExpression SourceAddress {
      get { return this.sourceAddress; }
      set { this.sourceAddress = value; }
    }
    IExpression sourceAddress;

    /// <summary>
    /// The number of bytes to copy from SourceAddress to TargetAddress.
    /// </summary>
    public IExpression NumberOfBytesToCopy {
      get { return this.numberOfBytesToCopy; }
      set { this.numberOfBytesToCopy = value; }
    }
    IExpression numberOfBytesToCopy;

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class DebuggerBreakStatement : Statement, IDebuggerBreakStatement {

    /// <summary>
    /// 
    /// </summary>
    public DebuggerBreakStatement() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="debuggerBreakStatement"></param>
    public DebuggerBreakStatement(IDebuggerBreakStatement debuggerBreakStatement)
      : base(debuggerBreakStatement) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class DoUntilStatement : Statement, IDoUntilStatement {

    /// <summary>
    /// 
    /// </summary>
    public DoUntilStatement() {
      this.body = CodeDummy.Block;
      this.condition = CodeDummy.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="doUntilStatement"></param>
    public DoUntilStatement(IDoUntilStatement doUntilStatement)
      : base(doUntilStatement) {
      this.body = doUntilStatement.Body;
      this.condition = doUntilStatement.Condition;
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    /// <value></value>
    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    /// <summary>
    /// The condition to evaluate as false or true.
    /// </summary>
    /// <value></value>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class EmptyStatement : Statement, IEmptyStatement {

    /// <summary>
    /// 
    /// </summary>
    public EmptyStatement() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="emptyStatement"></param>
    public EmptyStatement(IEmptyStatement emptyStatement)
      : base(emptyStatement) {
      this.isSentinel = emptyStatement.IsSentinel;
    }

    /// <summary>
    /// Returns a shallow copy of this EmptyStatement.
    /// </summary>
    public virtual EmptyStatement Clone() {
      return new EmptyStatement(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if this statement is a sentinel that should never be reachable.
    /// </summary>
    /// <value></value>
    public bool IsSentinel {
      get { return this.isSentinel; }
      set { this.IsSentinel = value; }
    }
    bool isSentinel;

  }

  /// <summary>
  /// 
  /// </summary>
  public class ExpressionStatement : Statement, IExpressionStatement {

    /// <summary>
    /// 
    /// </summary>
    public ExpressionStatement() {
      this.expression = CodeDummy.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expressionStatement"></param>
    public ExpressionStatement(IExpressionStatement expressionStatement)
      : base(expressionStatement) {
      this.expression = expressionStatement.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The expression.
    /// </summary>
    /// <value></value>
    public IExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression expression;

  }

  /// <summary>
  /// Represents the initblk IL instruction, which fills a block of memory with repeated copies of a given fill value.
  /// </summary>
  public class FillMemoryStatement : Statement, IFillMemoryStatement {

    /// <summary>
    /// Represents the initblk IL instruction, which fills a block of memory with repeated copies of a given fill value.
    /// </summary>
    public FillMemoryStatement() {
      this.targetAddress = CodeDummy.Expression;
      this.fillValue = CodeDummy.Expression;
      this.numberOfBytesToFill = CodeDummy.Expression;
    }

    /// <summary>
    /// Makes a shallow copy of a statement that represents initblk IL instruction, which fills a block of memory with repeated copies of a given fill value.
    /// </summary>
    /// <param name="fillMemoryStatement">The IFillMemoryStatement instance to copy.</param>
    public FillMemoryStatement(IFillMemoryStatement fillMemoryStatement)
      : base(fillMemoryStatement) {
      this.targetAddress = fillMemoryStatement.TargetAddress;
      this.fillValue = fillMemoryStatement.FillValue;
      this.numberOfBytesToFill = fillMemoryStatement.NumberOfBytesToFill;
    }

    /// <summary>
    /// Calls the visitor.Visit(IFillMemoryStatement).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    #region IFillMemoryStatement Members

    /// <summary>
    /// An expression resulting in an unsigned 8-bite value that will be used to fill the block at TargetAddress.
    /// </summary>
    public IExpression FillValue {
      get { return this.fillValue; }
      set { this.fillValue = value; }
    }
    IExpression fillValue;

    /// <summary>
    /// A pointer to the block of memory that is overwritten with the repeated value of FillValue.
    /// </summary>
    public IExpression TargetAddress {
      get { return this.targetAddress; }
      set { this.targetAddress = value; }
    }
    IExpression targetAddress;

    /// <summary>
    /// The number of bytes to fill (initialize) with FillValue.
    /// </summary>
    public IExpression NumberOfBytesToFill {
      get { return this.numberOfBytesToFill; }
      set { this.numberOfBytesToFill = value; }
    }
    IExpression numberOfBytesToFill;

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class ForEachStatement : Statement, IForEachStatement {

    /// <summary>
    /// 
    /// </summary>
    public ForEachStatement() {
      this.body = CodeDummy.Block;
      this.collection = CodeDummy.Expression;
      this.variable = Dummy.LocalVariable;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="forEachStatement"></param>
    public ForEachStatement(IForEachStatement forEachStatement)
      : base(forEachStatement) {
      this.body = forEachStatement.Body;
      this.collection = forEachStatement.Collection;
      this.variable = forEachStatement.Variable;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    /// <value></value>
    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    /// <summary>
    /// An epxression resulting in an enumerable collection of values (an object implementing System.Collections.IEnumerable).
    /// </summary>
    /// <value></value>
    public IExpression Collection {
      get { return this.collection; }
      set { this.collection = value; }
    }
    IExpression collection;

    /// <summary>
    /// The foreach loop variable that holds the current element from the collection.
    /// </summary>
    /// <value></value>
    public ILocalDefinition Variable {
      get { return this.variable; }
      set { this.variable = value; }
    }
    ILocalDefinition variable;

  }

  /// <summary>
  /// 
  /// </summary>
  public class ForStatement : Statement, IForStatement {

    /// <summary>
    /// 
    /// </summary>
    public ForStatement() {
      this.body = CodeDummy.Block;
      this.condition = CodeDummy.Expression;
      this.incrementStatements = new List<IStatement>();
      this.initStatements = new List<IStatement>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="forStatement"></param>
    public ForStatement(IForStatement forStatement)
      : base(forStatement) {
      this.body = forStatement.Body;
      this.condition = forStatement.Condition;
      this.incrementStatements = new List<IStatement>(forStatement.IncrementStatements);
      this.initStatements = new List<IStatement>(forStatement.InitStatements);
    }

    [ContractInvariantMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
    private void ObjectInvariant() {
      Contract.Invariant(this.body != null);
      Contract.Invariant(this.condition != null);
      Contract.Invariant(this.incrementStatements != null);
      Contract.Invariant(this.initStatements != null);
    }


    /// <summary>
    /// The statements making up the body of the loop.
    /// </summary>
    /// <value></value>
    public IStatement Body {
      get { return this.body; }
      set {
        Contract.Requires(value != null);
        this.body = value;
      }
    }
    IStatement body;

    /// <summary>
    /// Returns a shallow copy of this ForStatement.
    /// </summary>
    public virtual ForStatement Clone() {
      return new ForStatement(this);
    }

    /// <summary>
    /// The expression to evaluate as true or false, which determines if the loop is to continue.
    /// </summary>
    /// <value></value>
    public IExpression Condition {
      get { return this.condition; }
      set { 
        Contract.Requires(value != null);
        this.condition = value; 
      }
    }
    IExpression condition;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Statements that are called after each loop cycle, typically to increment a counter.
    /// </summary>
    /// <value></value>
    public List<IStatement> IncrementStatements {
      get {
        Contract.Ensures(Contract.Result<List<IStatement>>() != null);
        return this.incrementStatements; 
      }
      set {
        Contract.Requires(value != null);
        this.incrementStatements = value; 
      }
    }
    List<IStatement> incrementStatements;

    /// <summary>
    /// The loop initialization statements.
    /// </summary>
    /// <value></value>
    public List<IStatement> InitStatements {
      get {
        Contract.Ensures(Contract.Result<List<IStatement>>() != null);
        return this.initStatements; 
      }
      set {
        Contract.Requires(value != null);
        this.initStatements = value; 
      }
    }
    List<IStatement> initStatements;

    #region IForStatement Members

    IEnumerable<IStatement> IForStatement.IncrementStatements {
      get { return this.incrementStatements.AsReadOnly(); }
    }

    IEnumerable<IStatement> IForStatement.InitStatements {
      get { return this.initStatements.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class GotoStatement : Statement, IGotoStatement {

    /// <summary>
    /// 
    /// </summary>
    public GotoStatement() {
      this.targetStatement = CodeDummy.LabeledStatement;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gotoStatement"></param>
    public GotoStatement(IGotoStatement gotoStatement)
      : base(gotoStatement) {
      this.targetStatement = gotoStatement.TargetStatement;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The statement at which the program execution is to continue.
    /// </summary>
    /// <value></value>
    public ILabeledStatement TargetStatement {
      get { return this.targetStatement; }
      set { this.targetStatement = value; }
    }
    ILabeledStatement targetStatement;

  }

  /// <summary>
  /// 
  /// </summary>
  public class GotoSwitchCaseStatement : Statement, IGotoSwitchCaseStatement {

    /// <summary>
    /// 
    /// </summary>
    public GotoSwitchCaseStatement() {
      this.targetCase = CodeDummy.SwitchCase;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gotoSwitchCaseStatement"></param>
    public GotoSwitchCaseStatement(IGotoSwitchCaseStatement gotoSwitchCaseStatement)
      : base(gotoSwitchCaseStatement) {
      this.targetCase = gotoSwitchCaseStatement.TargetCase;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The switch statement case clause to which this statement transfers control to.
    /// </summary>
    /// <value></value>
    public ISwitchCase TargetCase {
      get { return this.targetCase; }
      set { this.targetCase = value; }
    }
    ISwitchCase targetCase;

  }

  /// <summary>
  /// 
  /// </summary>
  public class LabeledStatement : Statement, ILabeledStatement {

    /// <summary>
    /// 
    /// </summary>
    public LabeledStatement() {
      this.labelName = Dummy.Name;
      this.statement = CodeDummy.Block;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="labeledStatement"></param>
    public LabeledStatement(ILabeledStatement labeledStatement)
      : base(labeledStatement) {
      this.labelName = labeledStatement.Label;
      this.statement = labeledStatement.Statement;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The label.
    /// </summary>
    /// <value></value>
    public IName Label {
      get { return this.labelName; }
      set { this.labelName = value; }
    }
    IName labelName;

    /// <summary>
    /// The associated statement. Contains an empty statement if this is a stand-alone label.
    /// </summary>
    /// <value></value>
    public IStatement Statement {
      get { return this.statement; }
      set { this.statement = value; }
    }
    IStatement statement;

  }

  /// <summary>
  /// An object that represents the declaration of a local variable, with optional initializer.
  /// </summary>
  public class LocalDeclarationStatement : Statement, ILocalDeclarationStatement {

    /// <summary>
    /// Initializes an object that represents the declaration of a local variable, with optional initializer.
    /// </summary>
    public LocalDeclarationStatement() {
      this.initialValue = null;
      this.localVariable = Dummy.LocalVariable;
    }

    /// <summary>
    /// Initializes an object that represents the declaration of a local variable, with optional initializer.
    /// </summary>
    /// <param name="localDeclarationStatement">The local declaration statement.</param>
    public LocalDeclarationStatement(ILocalDeclarationStatement localDeclarationStatement)
      : base(localDeclarationStatement) {
      this.initialValue = localDeclarationStatement.InitialValue;
      this.localVariable = localDeclarationStatement.LocalVariable;
    }

    /// <summary>
    /// Calls visitor.Visit(ILocalDeclarationsStatement).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The initial value of the local variable. This may be null.
    /// </summary>
    /// <value></value>
    public IExpression/*?*/ InitialValue {
      get { return this.initialValue; }
      set { this.initialValue = value; }
    }
    IExpression/*?*/ initialValue;

    /// <summary>
    /// The local variable declared by this statement.
    /// </summary>
    /// <value></value>
    public ILocalDefinition LocalVariable {
      get { return this.localVariable; }
      set { this.localVariable = value; }
    }
    ILocalDefinition localVariable;

  }

  /// <summary>
  /// 
  /// </summary>
  public class LockStatement : Statement, ILockStatement {

    /// <summary>
    /// 
    /// </summary>
    public LockStatement() {
      this.body = CodeDummy.Block;
      this.guard = CodeDummy.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lockStatement"></param>
    public LockStatement(ILockStatement lockStatement)
      : base(lockStatement) {
      this.body = lockStatement.Body;
      this.guard = lockStatement.Guard;
    }

    /// <summary>
    /// The statement to execute inside the try body after the monitor has been entered.
    /// </summary>
    /// <value></value>
    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The monitor object (which gets locked when the monitor is entered and unlocked in the finally clause).
    /// </summary>
    /// <value></value>
    public IExpression Guard {
      get { return this.guard; }
      set { this.guard = value; }
    }
    IExpression guard;
  }

  /// <summary>
  /// Pushes a value onto an implicit operand stack.
  /// </summary>
  public class PushStatement : Statement, IPushStatement {

    /// <summary>
    /// Allocates a statement that pushes a value onto an implicit operand stack.
    /// </summary>
    public PushStatement() {
      this.valueToPush = CodeDummy.Expression;
    }

    /// <summary>
    /// Allocates a shallow copy of a statement that pushes a value onto an implicit operand stack.
    /// </summary>
    public PushStatement(IPushStatement pushStatement)
      : base(pushStatement) {
      this.valueToPush = pushStatement.ValueToPush;
    }

    /// <summary>
    /// Calls visitor.Visit((IPushStatement)this).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A value that is to be pushed onto the implicit operand stack.
    /// </summary>
    public IExpression ValueToPush {
      get { return this.valueToPush; }
      set { this.valueToPush = value; }
    }
    IExpression valueToPush;

  }

  /// <summary>
  /// 
  /// </summary>
  public class ResourceUseStatement : Statement, IResourceUseStatement {

    /// <summary>
    /// 
    /// </summary>
    public ResourceUseStatement() {
      this.body = CodeDummy.Block;
      this.resourceAcquisitions = CodeDummy.Block;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceUseStatement"></param>
    public ResourceUseStatement(IResourceUseStatement resourceUseStatement)
      : base(resourceUseStatement) {
      this.body = resourceUseStatement.Body;
      this.resourceAcquisitions = resourceUseStatement.ResourceAcquisitions;
    }

    /// <summary>
    /// The body of the resource use statement.
    /// </summary>
    /// <value></value>
    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Statements to initialize local definitions with the resources to use.
    /// </summary>
    /// <value></value>
    public IStatement ResourceAcquisitions {
      get { return this.resourceAcquisitions; }
      set { this.resourceAcquisitions = value; }
    }
    IStatement resourceAcquisitions;
  }

  /// <summary>
  /// 
  /// </summary>
  public class RethrowStatement : Statement, IRethrowStatement {

    /// <summary>
    /// 
    /// </summary>
    public RethrowStatement() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rethrowStatement"></param>
    public RethrowStatement(IRethrowStatement rethrowStatement)
      : base(rethrowStatement) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class ReturnStatement : Statement, IReturnStatement {

    /// <summary>
    /// 
    /// </summary>
    public ReturnStatement() {
      this.expression = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="returnStatement"></param>
    public ReturnStatement(IReturnStatement returnStatement)
      : base(returnStatement) {
      this.expression = returnStatement.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The return value, if any.
    /// </summary>
    /// <value></value>
    public IExpression/*?*/ Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression/*?*/ expression;

  }

  /// <summary>
  /// An executable statement.
  /// </summary>
  public abstract class Statement : IStatement {

    /// <summary>
    /// 
    /// </summary>
    protected Statement() {
      this.locations = new List<ILocation>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="statement"></param>
    protected Statement(IStatement statement) {
      Contract.Requires(statement != null);
      this.locations = new List<ILocation>(statement.Locations);
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.locations != null);
    }


    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(ICodeVisitor visitor);

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public List<ILocation> Locations {
      get {
        Contract.Ensures(Contract.Result<List<ILocation>>() != null);
        return this.locations;
      }
      set {
        Contract.Requires(value != null);
        this.locations = value;
      }
    }
    List<ILocation> locations;

    #region IStatement Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class SwitchCase : ISwitchCase {

    /// <summary>
    /// 
    /// </summary>
    public SwitchCase() {
      this.body = new List<IStatement>();
      this.expression = CodeDummy.Constant;
      this.locations = new List<ILocation>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchCase"/> class.
    /// </summary>
    /// <param name="switchCase">The switch case.</param>
    public SwitchCase(ISwitchCase switchCase) {
      this.body = new List<IStatement>(switchCase.Body);
      if (!switchCase.IsDefault)
        this.expression = switchCase.Expression;
      else
        this.expression = CodeDummy.Constant;
      this.locations = new List<ILocation>(switchCase.Locations);
    }

    /// <summary>
    /// The statements representing this switch case.
    /// </summary>
    /// <value></value>
    public List<IStatement> Body {
      get {
        Contract.Ensures(Contract.Result<List<IStatement>>() != null);
        return this.body; 
      }
      set {
        Contract.Requires(value != null);
        this.body = value; 
      }
    }
    List<IStatement> body;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A compile time constant of the same type as the switch expression.
    /// </summary>
    /// <value></value>
    public ICompileTimeConstant Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    ICompileTimeConstant expression;

    /// <summary>
    /// True if this case will be branched to for all values where no other case is applicable. Only one of of these is legal per switch statement.
    /// </summary>
    /// <value></value>
    public bool IsDefault {
      get { return this.expression == CodeDummy.Constant; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IList<ILocation> Locations {
      get { return this.locations; }
    }
    List<ILocation> locations;

    #region ISwitchCase Members

    IEnumerable<IStatement> ISwitchCase.Body {
      get { return this.body.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class SwitchStatement : Statement, ISwitchStatement {

    /// <summary>
    /// 
    /// </summary>
    public SwitchStatement() {
      this.cases = new List<ISwitchCase>();
      this.expression = CodeDummy.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="switchStatement"></param>
    public SwitchStatement(ISwitchStatement switchStatement)
      : base(switchStatement) {
      this.cases = new List<ISwitchCase>(switchStatement.Cases);
      this.expression = switchStatement.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The switch cases.
    /// </summary>
    /// <value></value>
    public List<ISwitchCase> Cases {
      get {
        Contract.Ensures(Contract.Result<List<ISwitchCase>>() != null);
        return this.cases; 
      }
      set {
        Contract.Requires(value != null);
        this.cases = value; 
      }
    }
    List<ISwitchCase> cases;

    /// <summary>
    /// The expression to evaluate in order to determine with switch case to branch to.
    /// </summary>
    /// <value></value>
    public IExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression expression;

    #region ISwitchStatement Members

    IEnumerable<ISwitchCase> ISwitchStatement.Cases {
      get { return this.cases.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class ThrowStatement : Statement, IThrowStatement {

    /// <summary>
    /// 
    /// </summary>
    public ThrowStatement() {
      this.exception = CodeDummy.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="throwStatement"></param>
    public ThrowStatement(IThrowStatement throwStatement)
      : base(throwStatement) {
      this.exception = throwStatement.Exception;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The exception to throw.
    /// </summary>
    /// <value></value>
    public IExpression Exception {
      get { return this.exception; }
      set { this.exception = value; }
    }
    IExpression exception;

  }

  /// <summary>
  /// Represents a try block with any number of catch clauses, any number of filter clauses and, optionally, a finally or fault block.
  /// </summary>
  public class TryCatchFinallyStatement : Statement, ITryCatchFinallyStatement {

    /// <summary>
    /// Allocates and object that represents a try block with any number of catch clauses, any number of filter clauses and, optionally, a finally or fault block.
    /// </summary>
    public TryCatchFinallyStatement() {
      this.catchClauses = new List<ICatchClause>();
      this.finallyBody = null;
      this.faultBody = null;
      this.tryBody = CodeDummy.Block;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tryCatchFinallyStatement"></param>
    public TryCatchFinallyStatement(ITryCatchFinallyStatement tryCatchFinallyStatement)
      : base(tryCatchFinallyStatement) {
      this.catchClauses = new List<ICatchClause>(tryCatchFinallyStatement.CatchClauses);
      this.finallyBody = tryCatchFinallyStatement.FinallyBody;
      this.faultBody = tryCatchFinallyStatement.FaultBody;
      this.tryBody = tryCatchFinallyStatement.TryBody;
    }

    [ContractInvariantMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
    private void ObjectInvariant() {
      Contract.Invariant(this.catchClauses != null);
      Contract.Invariant(this.tryBody != null);
    }


    /// <summary>
    /// The catch clauses.
    /// </summary>
    /// <value></value>
    public List<ICatchClause> CatchClauses {
      get {
        Contract.Ensures(Contract.Result<List<ICatchClause>>() != null);
        return this.catchClauses;
      }
      set {
        Contract.Requires(value != null);
        this.catchClauses = value;
      }
    }
    List<ICatchClause> catchClauses;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The body of the finally clause, if any. May be null.
    /// </summary>
    /// <value></value>
    public IBlockStatement/*?*/ FinallyBody {
      get { return this.finallyBody; }
      set { this.finallyBody = value; }
    }
    IBlockStatement/*?*/ finallyBody;

    /// <summary>
    /// The body of the fault clause, if any. May be null.
    /// There is no C# equivalent of a fault clause. It is just like a finally clause, but is only invoked if an exception occurred.
    /// </summary>
    public IBlockStatement/*?*/ FaultBody {
      get { return this.faultBody; }
      set { this.faultBody = value; }
    }
    IBlockStatement/*?*/ faultBody;

    /// <summary>
    /// The body of the try clause.
    /// </summary>
    /// <value></value>
    public IBlockStatement TryBody {
      get { return this.tryBody; }
      set { this.tryBody = value; }
    }
    IBlockStatement tryBody;

    #region ITryCatchFinallyStatement Members

    IEnumerable<ICatchClause> ITryCatchFinallyStatement.CatchClauses {
      get { return this.catchClauses.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class WhileDoStatement : Statement, IWhileDoStatement {

    /// <summary>
    /// 
    /// </summary>
    public WhileDoStatement() {
      this.body = CodeDummy.Block;
      this.condition = CodeDummy.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="whileDoStatement"></param>
    public WhileDoStatement(IWhileDoStatement whileDoStatement)
      : base(whileDoStatement) {
      Contract.Requires(whileDoStatement != null);
      this.body = whileDoStatement.Body;
      this.condition = whileDoStatement.Condition;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.body != null);
      Contract.Invariant(this.condition != null);
    }


    /// <summary>
    /// The body of the loop.
    /// </summary>
    /// <value></value>
    public IStatement Body {
      get { return this.body; }
      set {
        Contract.Requires(value != null);
        this.body = value; 
      }
    }
    IStatement body;

    /// <summary>
    /// The condition to evaluate as false or true.
    /// </summary>
    /// <value></value>
    public IExpression Condition {
      get { return this.condition; }
      set {
        Contract.Requires(value != null);
        this.condition = value; 
      }
    }
    IExpression condition;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class YieldBreakStatement : Statement, IYieldBreakStatement {

    /// <summary>
    /// 
    /// </summary>
    public YieldBreakStatement() {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="yieldBreakStatement"></param>
    public YieldBreakStatement(IYieldBreakStatement yieldBreakStatement)
      : base(yieldBreakStatement) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class YieldReturnStatement : Statement, IYieldReturnStatement {

    /// <summary>
    /// 
    /// </summary>
    public YieldReturnStatement() {
      this.expression = CodeDummy.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="yieldReturnStatement"></param>
    public YieldReturnStatement(IYieldReturnStatement yieldReturnStatement)
      : base(yieldReturnStatement) {
      this.expression = yieldReturnStatement.Expression;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The value to yield.
    /// </summary>
    /// <value></value>
    public IExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression expression;

  }

}
