//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A statement that asserts that a condition must always be true when execution reaches it. For example the assert statement of Spec#.
  /// </summary>
  public sealed class AssertStatement : Statement, IAssertStatement {

    public AssertStatement() {
      this.condition = CodeDummy.Expression;
    }

    public AssertStatement(IAssertStatement assertStatement) 
      : base(assertStatement)
    {
      this.condition = assertStatement.Condition;
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

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if a static verification tool has determined that the condition will always be true when execution reaches this statement.
    /// </summary>
    public bool HasBeenVerified {
      get { return this.hasBeenVerified; }
      set { this.hasBeenVerified = true; }
    }
    bool hasBeenVerified;
  }

  /// <summary>
  /// A statement that asserts that a condition will always be true when execution reaches it. For example the assume statement of Spec#
  /// or a call to System.Diagnostics.Assert in C#.
  /// </summary>
  public sealed class AssumeStatement : Statement, IAssumeStatement {

    public AssumeStatement() {
      this.condition = CodeDummy.Expression;
    }

    public AssumeStatement(IAssumeStatement assumeStatement)
      : base(assumeStatement) {
      this.condition = assumeStatement.Condition;
    }

    /// <summary>
    /// A condition that must be true when execution reaches this statement.
    /// </summary>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }
  
  public class BlockStatement : Statement, IBlockStatement {

    public BlockStatement(){
      this.statements = new List<IStatement>();
      this.useCheckedArithmetic = false;
    }

    public BlockStatement(IBlockStatement blockStatement)
      : base(blockStatement) {
      this.statements = new List<IStatement>(blockStatement.Statements);
      this.useCheckedArithmetic = blockStatement.UseCheckedArithmetic;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public List<IStatement> Statements {
      get { return this.statements; }
      set { this.statements = value; }
    }
    List<IStatement> statements;

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

  public sealed class BreakStatement : Statement, IBreakStatement {

    public BreakStatement() {
    }

    public BreakStatement(IBreakStatement breakStatement)
      : base(breakStatement) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class CatchClause : ICatchClause {

    public CatchClause() {
      this.body = CodeDummy.Block;
      this.exceptionContainer = Dummy.LocalVariable;
      this.exceptionType = Dummy.TypeReference;
      this.filterCondition = null;
      this.locations = new List<ILocation>();
    }

    public CatchClause(ICatchClause catchClause) {
      this.body = catchClause.Body;
      this.exceptionContainer = catchClause.ExceptionContainer;
      this.exceptionType = catchClause.ExceptionType;
      this.filterCondition = catchClause.FilterCondition;
      this.locations = new List<ILocation>(catchClause.Locations);
    }

    public IBlockStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IBlockStatement body;

    public void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ILocalDefinition ExceptionContainer {
      get { return this.exceptionContainer; }
      set { this.exceptionContainer = value; }
    }
    ILocalDefinition exceptionContainer;

    public ITypeReference ExceptionType {
      get { return this.exceptionType; }
      set { this.exceptionType = value; }
    }
    ITypeReference exceptionType;

    public IExpression/*?*/ FilterCondition {
      get { return this.filterCondition; }
      set { this.filterCondition = value; }
    }
    IExpression/*?*/ filterCondition;

    public IList<ILocation> Locations {
      get { return this.locations; }
    }
    List<ILocation> locations;

    #region ICatchClause Members

    IEnumerable<ILocation> ICatchClause.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class ConditionalStatement : Statement, IConditionalStatement {

    public ConditionalStatement() {
      this.condition = CodeDummy.Expression;
      this.falseBranch = CodeDummy.Block;
      this.trueBranch = CodeDummy.Block;
    }

    public ConditionalStatement(IConditionalStatement conditionalStatement)
      : base(conditionalStatement) {
      this.condition = conditionalStatement.Condition;
      this.falseBranch = conditionalStatement.FalseBranch;
      this.trueBranch = conditionalStatement.TrueBranch;
    }

    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IStatement FalseBranch {
      get { return this.falseBranch; }
      set { this.falseBranch = value; }
    }
    IStatement falseBranch;

    public IStatement TrueBranch {
      get { return this.trueBranch; }
      set { this.trueBranch = value; }
    }
    IStatement trueBranch;

  }

  public sealed class ContinueStatement : Statement, IContinueStatement {

    public ContinueStatement() {
    }

    public ContinueStatement(IContinueStatement continueStatement)
      : base(continueStatement) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class DebuggerBreakStatement : Statement, IDebuggerBreakStatement {

    public DebuggerBreakStatement() {
    }

    public DebuggerBreakStatement(IDebuggerBreakStatement debuggerBreakStatement)
      : base(debuggerBreakStatement) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class DoUntilStatement : Statement, IDoUntilStatement {

    public DoUntilStatement() {
      this.body = CodeDummy.Block;
      this.condition = CodeDummy.Expression;
    }

    public DoUntilStatement(IDoUntilStatement doUntilStatement)
      : base(doUntilStatement) {
      this.body = doUntilStatement.Body;
      this.condition = doUntilStatement.Condition;
    }

    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class EmptyStatement : Statement, IEmptyStatement {

    public EmptyStatement() {
    }

    public EmptyStatement(IEmptyStatement emptyStatement)
      : base(emptyStatement) {
      this.isSentinel = emptyStatement.IsSentinel;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public bool IsSentinel {
      get { return this.isSentinel; }
      set { this.IsSentinel = value; }
    }
    bool isSentinel;

  }

  public sealed class ExpressionStatement : Statement, IExpressionStatement {

    public ExpressionStatement() {
      this.expression = CodeDummy.Expression;
    }

    public ExpressionStatement(IExpressionStatement expressionStatement)
      : base(expressionStatement) {
      this.expression = expressionStatement.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression expression;

  }

  public sealed class ForEachStatement : Statement, IForEachStatement {

    public ForEachStatement() {
      this.body = CodeDummy.Block;
      this.collection = CodeDummy.Expression;
      this.variable = Dummy.LocalVariable;
    }

    public ForEachStatement(IForEachStatement forEachStatement)
      : base(forEachStatement) {
      this.body = forEachStatement.Body;
      this.collection = forEachStatement.Collection;
      this.variable = forEachStatement.Variable;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    public IExpression Collection {
      get { return this.collection; }
      set { this.collection = value; }
    }
    IExpression collection;

    public ILocalDefinition Variable {
      get { return this.variable; }
      set { this.variable = value; }
    }
    ILocalDefinition variable;

  }

  public sealed class ForStatement : Statement, IForStatement {

    public ForStatement() {
      this.body = CodeDummy.Block;
      this.condition = CodeDummy.Expression;
      this.incrementStatements = new List<IStatement>();
      this.initStatements = new List<IStatement>();
    }

    public ForStatement(IForStatement forStatement)
      : base(forStatement) {
      this.body = forStatement.Body;
      this.condition = forStatement.Condition;
      this.incrementStatements = new List<IStatement>(forStatement.IncrementStatements);
      this.initStatements = new List<IStatement>(forStatement.InitStatements);
    }

    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public List<IStatement> IncrementStatements {
      get { return this.incrementStatements; }
      set { this.incrementStatements = value; }
    }
    List<IStatement> incrementStatements;

    public List<IStatement> InitStatements {
      get { return this.initStatements; }
      set { this.initStatements = value; }
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

  public class GotoStatement : Statement, IGotoStatement {

    public GotoStatement() {
      this.targetStatement = CodeDummy.LabeledStatement;
    }

    public GotoStatement(IGotoStatement gotoStatement)
      : base(gotoStatement) {
      this.targetStatement = gotoStatement.TargetStatement;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ILabeledStatement TargetStatement {
      get { return this.targetStatement; }
      set { this.targetStatement = value; }
    }
    ILabeledStatement targetStatement;

  }

  public sealed class GotoSwitchCaseStatement : Statement, IGotoSwitchCaseStatement {

    public GotoSwitchCaseStatement() {
      this.targetCase = CodeDummy.SwitchCase;
    }

    public GotoSwitchCaseStatement(IGotoSwitchCaseStatement gotoSwitchCaseStatement)
      : base(gotoSwitchCaseStatement) {
      this.targetCase = gotoSwitchCaseStatement.TargetCase;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ISwitchCase TargetCase {
      get { return this.targetCase; }
      set { this.targetCase = value; }
    }
    ISwitchCase targetCase;

  }

  public sealed class LabeledStatement : Statement, ILabeledStatement {

    public LabeledStatement() {
      this.labelName = Dummy.Name;
      this.statement = CodeDummy.Block;
    }

    public LabeledStatement(ILabeledStatement labeledStatement)
      : base(labeledStatement) {
      this.labelName = labeledStatement.Label;
      this.statement = labeledStatement.Statement;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IName Label {
      get { return this.labelName; }
      set { this.labelName = value; }
    }
    IName labelName;

    public IStatement Statement {
      get { return this.statement; }
      set { this.statement = value; }
    }
    IStatement statement;

  }

  public sealed class LocalDeclarationStatement : Statement, ILocalDeclarationStatement {

    public LocalDeclarationStatement() {
      this.initialValue = null;
      this.localVariable = Dummy.LocalVariable;
    }

    public LocalDeclarationStatement(ILocalDeclarationStatement localDeclarationStatement)
      : base(localDeclarationStatement) {
      this.initialValue = localDeclarationStatement.InitialValue;
      this.localVariable = localDeclarationStatement.LocalVariable;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression/*?*/ InitialValue {
      get { return this.initialValue; }
      set { this.initialValue = value; }
    }
    IExpression/*?*/ initialValue;

    public ILocalDefinition LocalVariable {
      get { return this.localVariable; }
      set { this.localVariable = value; }
    }
    ILocalDefinition localVariable;

  }

  public sealed class LockStatement : Statement, ILockStatement {

    public LockStatement() {
      this.body = CodeDummy.Block;
      this.guard = CodeDummy.Expression;
    }

    public LockStatement(ILockStatement lockStatement)
      : base(lockStatement) {
      this.body = lockStatement.Body;
      this.guard = lockStatement.Guard;
    }

    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Guard {
      get { return this.guard; }
      set { this.guard = value; }
    }
    IExpression guard;
  }

  public sealed class ResourceUseStatement : Statement, IResourceUseStatement {

    public ResourceUseStatement() {
      this.body = CodeDummy.Block;
      this.resourceAcquisitions = CodeDummy.Block;
    }

    public ResourceUseStatement(IResourceUseStatement resourceUseStatement)
      : base(resourceUseStatement) {
      this.body = resourceUseStatement.Body;
      this.resourceAcquisitions = resourceUseStatement.ResourceAcquisitions;
    }

    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IStatement ResourceAcquisitions {
      get { return this.resourceAcquisitions; }
      set { this.resourceAcquisitions = value; }
    }
    IStatement resourceAcquisitions;
  }

  public sealed class RethrowStatement : Statement, IRethrowStatement {

    public RethrowStatement() {
    }

    public RethrowStatement(IRethrowStatement rethrowStatement)
      : base(rethrowStatement) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class ReturnStatement : Statement, IReturnStatement {

    public ReturnStatement() {
      this.expression = null;
    }

    public ReturnStatement(IReturnStatement returnStatement)
      : base(returnStatement) {
      this.expression = returnStatement.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

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

    protected Statement() {
      this.locations = new List<ILocation>();
    }

    protected Statement(IStatement statement) {
      this.locations = new List<ILocation>(statement.Locations);
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(ICodeVisitor visitor);

    /// <summary>
    /// Checks the statement for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() {
      return false;
    }

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    #region IStatement Members

    IEnumerable<ILocation> IStatement.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class SwitchCase : ISwitchCase {

    public SwitchCase() {
      this.body = new List<IStatement>();
      this.expression = CodeDummy.Constant;
      this.locations = new List<ILocation>();
    }

    public SwitchCase(ISwitchCase switchCase) {
      this.body = new List<IStatement>(switchCase.Body);
      if (!switchCase.IsDefault)
        this.expression = switchCase.Expression;
      else
        this.expression = CodeDummy.Constant;
      this.locations = new List<ILocation>(switchCase.Locations);
    }

    public List<IStatement> Body {
      get { return this.body; }
      set { this.body = value; }
    }
    List<IStatement> body;

    public void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public ICompileTimeConstant Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    ICompileTimeConstant expression;

    public bool IsDefault {
      get { return this.expression == CodeDummy.Constant; }
    }

    public IList<ILocation> Locations {
      get { return this.locations; }
    }
    List<ILocation> locations;

    #region ISwitchCase Members

    IEnumerable<IStatement> ISwitchCase.Body {
      get { return this.body.AsReadOnly(); }
    }

    IEnumerable<ILocation> ISwitchCase.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion

  }

  public sealed class SwitchStatement : Statement, ISwitchStatement {

    public SwitchStatement() {
      this.cases = new List<ISwitchCase>();
      this.expression = CodeDummy.Expression;
    }

    public SwitchStatement(ISwitchStatement switchStatement)
      : base(switchStatement) {
      this.cases = new List<ISwitchCase>(switchStatement.Cases);
      this.expression = switchStatement.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public List<ISwitchCase> Cases {
      get { return this.cases; }
      set { this.cases = value; }
    }
    List<ISwitchCase> cases;

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

  public sealed class ThrowStatement : Statement, IThrowStatement {

    public ThrowStatement() {
      this.exception = CodeDummy.Expression;
    }

    public ThrowStatement(IThrowStatement throwStatement)
      : base(throwStatement) {
      this.exception = throwStatement.Exception;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Exception {
      get { return this.exception; }
      set { this.exception = value; }
    }
    IExpression exception;

  }

  public sealed class TryCatchFinallyStatement : Statement, ITryCatchFinallyStatement {

    public TryCatchFinallyStatement() {
      this.catchClauses = new List<ICatchClause>();
      this.finallyBody = null;
      this.tryBody = CodeDummy.Block;
    }

    public TryCatchFinallyStatement(ITryCatchFinallyStatement tryCatchFinallyStatement)
      : base(tryCatchFinallyStatement) {
      this.catchClauses = new List<ICatchClause>(tryCatchFinallyStatement.CatchClauses);
      this.finallyBody = tryCatchFinallyStatement.FinallyBody;
      this.tryBody = tryCatchFinallyStatement.TryBody;
    }

    public List<ICatchClause> CatchClauses {
      get { return this.catchClauses; }
      set { this.catchClauses = value; }
    }
    List<ICatchClause> catchClauses;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IBlockStatement/*?*/ FinallyBody {
      get { return this.finallyBody; }
      set { this.finallyBody = value; }
    }
    IBlockStatement/*?*/ finallyBody;

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

  public sealed class WhileDoStatement : Statement, IWhileDoStatement {

    public WhileDoStatement() {
      this.body = CodeDummy.Block;
      this.condition = CodeDummy.Expression;
    }

    public WhileDoStatement(IWhileDoStatement whileDoStatement)
      : base(whileDoStatement) {
      this.body = whileDoStatement.Body;
      this.condition = whileDoStatement.Condition;
    }

    public IStatement Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IStatement body;

    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class YieldBreakStatement : Statement, IYieldBreakStatement {

    public YieldBreakStatement() {
    }

    public YieldBreakStatement(IYieldBreakStatement yieldBreakStatement)
      : base(yieldBreakStatement) {
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

  }

  public sealed class YieldReturnStatement : Statement, IYieldReturnStatement {

    public YieldReturnStatement() {
      this.expression = CodeDummy.Expression;
    }

    public YieldReturnStatement(IYieldReturnStatement yieldReturnStatement)
      : base(yieldReturnStatement) {
      this.expression = yieldReturnStatement.Expression;
    }

    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    public IExpression Expression {
      get { return this.expression; }
      set { this.expression = value; }
    }
    IExpression expression;

  }

}
