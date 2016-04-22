//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {
  /// <summary>
  /// A statement that asserts that a condition must always be true when execution reaches it. For example the assert statement of Spec#.
  /// </summary>
  public class AssertStatement : Statement, IAssertStatement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="sourceLocation"></param>
    public AssertStatement(Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.condition = condition;
      this.description = null;
      this.conditionAsText = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingBlock"></param>
    /// <param name="template"></param>
    public AssertStatement(BlockStatement containingBlock, AssertStatement template)
      : base(containingBlock, template) {
      this.condition = template.Condition.MakeCopyFor(containingBlock);
      this.description = (template.Description != null) ? template.Description.MakeCopyFor(containingBlock) : null;
      this.conditionAsText = template.OriginalSource;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Condition.HasErrors || this.Condition.HasSideEffect(true);
    }

    /// <summary>
    /// A condition that must be true when execution reaches this statement.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    Expression condition;

    /// <summary>
    /// An optional expression that is associated with the assertion. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    public Expression/*?*/ Description {
      get { return this.description; }
      set { this.description = value; }
    }
    Expression/*?*/ description;

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

    /// <summary>
    /// The condition that must be true at the start of the method that is associated with this Precondition instance.
    /// </summary>
    public Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    //^ [Once]
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Calls the visitor.Visit(IAssertStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(AssertStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if a static verification tool has determined that the condition will always be true when execution reaches this statement.
    /// </summary>
    public bool HasBeenVerified {
      get { return false; }
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new AssertStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.condition.SetContainingExpression(containingExpression);
      if (this.description != null) this.description.SetContainingExpression(containingExpression);
    }

    #region IAssertStatement Members

    IExpression IAssertStatement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IExpression/*?*/ IAssertStatement.Description {
      get { return (this.Description == null) ? null : this.Description.ProjectAsIExpression(); }
    }

    #endregion
  }

  /// <summary>
  /// A statement that asserts that a condition will always be true when execution reaches it. For example the assume statement of Spec#
  /// or a call to System.Diagnostics.Assert in C#.
  /// </summary>
  public sealed class AssumeStatement : Statement, IAssumeStatement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="sourceLocation"></param>
    public AssumeStatement(Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.condition = condition;
      this.description = null;
      this.conditionAsText = null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingBlock"></param>
    /// <param name="template"></param>
    public AssumeStatement(BlockStatement containingBlock, AssumeStatement template)
      : base(containingBlock, template) {
      this.condition = template.Condition.MakeCopyFor(containingBlock);
      this.description = (template.Description != null) ? template.Description.MakeCopyFor(containingBlock) : null;
      this.conditionAsText = template.OriginalSource;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Condition.HasErrors || this.Condition.HasSideEffect(true);
    }

    /// <summary>
    /// A condition that must be true when execution reaches this statement.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// An optional expression that is associated with the assumption. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    public Expression/*?*/ Description {
      get { return this.description; }
      set { this.description = value; }
    }
    Expression/*?*/ description;

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

    /// <summary>
    /// The condition that must be true at the start of the method that is associated with this Precondition instance.
    /// </summary>
    public Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    //^ [Once]
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Calls the visitor.Visit(IAssumeStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(AssumeStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new AssumeStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.condition.SetContainingExpression(containingExpression);
      if (this.description != null) this.description.SetContainingExpression(containingExpression);
    }

    #region IAssumeStatement Members

    IExpression IAssumeStatement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IExpression/*?*/ IAssumeStatement.Description {
      get { return (this.Description == null) ? null : this.Description.ProjectAsIExpression(); }
    }

    #endregion
  }
  /// <summary>
  /// Represents a statement that attaches an event-handler delegate to an event.
  /// </summary>
  public class AttachEventHandlerStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="handler"></param>
    /// <param name="sourceLocation"></param>
    public AttachEventHandlerStatement(Expression @event, Expression handler, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.@event = @event;
      this.handler = handler;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(AttachEventHandlerStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The event to attach an event-handler delegate to.
    /// </summary>
    public Expression Event {
      get { return this.@event; }
    }
    readonly Expression @event;

    /// <summary>
    /// The event-handler delegate to attach to the event
    /// </summary>
    public Expression Handler {
      get { return this.handler; }
    }
    readonly Expression handler;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.@event.SetContainingExpression(containingExpression);
      this.handler.SetContainingExpression(containingExpression);
    }

  }

  /// <summary>
  /// A delimited collection of statements to execute in a new (nested) scope.
  /// </summary>
  public class BlockStatement : Statement, IBlockStatement { //TODO: need to support a subclass that can reparse the block body on demand

    /// <summary>
    /// Values that indicate if a block is checked/unchecked and safe/unsafe or whether it inherits these settings from its containing block or from a compilation option.
    /// </summary>
    public enum Options {
      /// <summary>
      /// The block inherits its checked/unchecked and safe/unsafe setting from its parent. If there is no parent, the checked/unchecked setting is determined by a compilation option.
      /// </summary>
      Default,
      /// <summary>
      /// Code inside the block can contain unsafe constructs.
      /// </summary>
      AllowUnsafeCode,
      /// <summary>
      /// Arithmetic operations inside this block throw an exception if overflow occurs.
      /// </summary>
      UseCheckedArithmetic,
      /// <summary>
      /// Arithmetic operations inside this block silently overflow.
      /// </summary>
      UseUncheckedArithmetic
    }

    /// <summary>
    /// Allocates a delimited collection of statements to execute in a new (nested) scope.
    /// </summary>
    /// <param name="statements">The statements making up the block.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public BlockStatement(List<Statement> statements, ISourceLocation sourceLocation)
      : this(statements, Options.Default, sourceLocation) {
    }

    /// <summary>
    /// Allocates a delimited collection of statements to execute in a new (nested) scope.
    /// </summary>
    /// <param name="statements">The statements making up the block.</param>
    /// <param name="options">A Value that indicate if the block is checked/unchecked and safe/unsafe or whether it inherits these settings from its containing block or from a compilation option.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public BlockStatement(List<Statement> statements, Options options, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires options == Options.Default || options == Options.AllowUnsafeCode || options == Options.UseCheckedArithmetic || options == Options.UseUncheckedArithmetic;
    {
      this.statements = statements;
      int flags = 0x12; //inherit settings of AllowUnsafe and UseCheckedArithmetic 010010, not yet initialized
      if (options == Options.AllowUnsafeCode)
        flags = 0x15; //set AllowUnsafe == true and inherit UseCheckedArithmetic 010101
      else if (options == Options.UseCheckedArithmetic)
        flags = 0x2a; //set UseCheckedArithmetic == true and inherit AllowUnsafe 101010
      else if (options == Options.UseUncheckedArithmetic)
        flags = 0x0a; //set UseCheckedArithmetic == false and inherit AllowUnsafe 001010
      this.flags = flags;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected BlockStatement(BlockStatement containingBlock, BlockStatement template)
      : base(containingBlock, template) {
      this.statements = new List<Statement>(template.Statements);
      int flags = template.flags;
      if ((flags & 2) != 0) flags &= ~1;
      if ((flags & 16) != 0) flags &= ~8;
      this.flags = flags;
      this.containingSignatureDeclaration = containingBlock.ContainingSignatureDeclaration;
      this.containingNamespaceDeclaration = containingBlock.ContainingNamespaceDeclaration;
      this.containingTypeDeclaration = containingBlock.ContainingTypeDeclaration;
      this.compilationPart = containingBlock.CompilationPart;
    }

    /// <summary>
    /// True if unsafe constructs are allowed inside this block.
    /// </summary>
    public bool AllowUnsafe {
      get {
        if ((this.flags & 1) == 0) { //not yet initialized
          this.flags |= 1;
          if ((this.flags & 2) != 0 && this.ContainingBlock.AllowUnsafe) //inherit 
            this.flags |= 4;
        }
        return (this.flags & 4) != 0;
      }
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (Statement statement in this.Statements)
        result |= statement.HasErrors;
      return result;
    }


    /// <summary>
    /// The compilation part that contains this statement.
    /// </summary>
    new public CompilationPart CompilationPart {
      get {
        //^ assume this.compilationPart != null;
        return this.compilationPart;
      }
    }
    CompilationPart/*?*/ compilationPart;

    /// <summary>
    /// The method definition, if any, that contains this statement block. This can be null if this statement block is part of an anonymous delegate or lambda expression that
    /// is used to initialize a field.
    /// </summary>
    public IMethodDefinition/*?*/ ContainingMethodDefinition {
      get {
        MethodDeclaration/*?*/ containingMethodDeclaration = this.ContainingSignatureDeclaration as MethodDeclaration;
        if (containingMethodDeclaration == null)
          return null;
        else
          return containingMethodDeclaration.MethodDefinition;
      }
    }

    /// <summary>
    /// The namespace declaration that contains this statement block.
    /// </summary>
    public NamespaceDeclaration ContainingNamespaceDeclaration {
      get {
        //^ assume this.containingNamespaceDeclaration != null;
        return this.containingNamespaceDeclaration;
      }
    }
    NamespaceDeclaration/*?*/ containingNamespaceDeclaration;


    /// <summary>
    /// The signature declaration (such as a lambda, method, property or anonymous method) that contains this statement block.
    /// This can be null if this is a dummy block associated with a type declaration or namespace declaration.
    /// </summary>
    public ISignatureDeclaration/*?*/ ContainingSignatureDeclaration {
      get {
        return this.containingSignatureDeclaration;
      }
    }
    ISignatureDeclaration/*?*/ containingSignatureDeclaration;

    /// <summary>
    /// The type declaration that contains this statement block.
    /// </summary>
    public TypeDeclaration/*?*/ ContainingTypeDeclaration {
      get {
        return this.containingTypeDeclaration;
      }
    }
    TypeDeclaration/*?*/ containingTypeDeclaration;

    /// <summary>
    /// Creates a statement scope for this block. Do not call this directly, but use this.Scope to get a cached instance.
    /// </summary>
    protected virtual StatementScope CreateStatementScope() {
      return new StatementScope(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(IBlockStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(BlockStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Bit array of boolean values: 1 == Unsafe Initialized, 2 == Inherit Unsafe, 4 = Allow Unsafe, 8 = Checked Initialized, 16 = Inherit Checked, 32 = Use Checked
    /// </summary>
    protected int flags;

    /// <summary>
    /// Recursively visits all statements of the block, appending any local variable definitions to the given list.
    /// </summary>
    /// <param name="localVariables"></param>
    public void PopulateLocalVariableList(ICollection<ILocalDefinition> localVariables) {
      foreach (Statement s in this.Statements) {
        BlockStatement/*?*/ blk = s as BlockStatement;
        if (blk != null) {
          blk.PopulateLocalVariableList(localVariables);
        } else {
          LocalDeclarationsStatement/*?*/ locDecls = s as LocalDeclarationsStatement;
          if (locDecls != null) {
            foreach (LocalDeclaration locDecl in locDecls.Declarations) {
              if (!localVariables.Contains(locDecl.LocalVariable)) {
                localVariables.Add(locDecl.LocalVariable);
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new BlockStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainers(BlockStatement containingBlock, NamespaceDeclaration containingNamespaceDeclaration) {
      base.SetContainingBlock(containingBlock);
      this.containingNamespaceDeclaration = containingNamespaceDeclaration;
      this.compilationPart = containingNamespaceDeclaration.CompilationPart;
      foreach (Statement statement in this.statements) statement.SetContainingBlock(this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainers(BlockStatement containingBlock, ISignatureDeclaration containingSignatureDeclaration) {
      base.SetContainingBlock(containingBlock);
      this.containingSignatureDeclaration = containingSignatureDeclaration;
      this.containingNamespaceDeclaration = containingBlock.ContainingNamespaceDeclaration;
      this.containingTypeDeclaration = containingBlock.ContainingTypeDeclaration;
      this.compilationPart = containingBlock.CompilationPart;
      foreach (Statement statement in this.statements) statement.SetContainingBlock(this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainers(BlockStatement containingBlock, TypeDeclaration containingTypeDeclaration) {
      this.containingTypeDeclaration = containingTypeDeclaration;
      NamespaceTypeDeclaration/*?*/ containingNamespaceTypeDeclaration = containingTypeDeclaration as NamespaceTypeDeclaration;
      if (containingNamespaceTypeDeclaration != null)
        this.SetContainers(containingBlock, containingNamespaceTypeDeclaration.ContainingNamespaceDeclaration);
      else
        this.SetContainers(containingBlock, containingBlock.ContainingNamespaceDeclaration);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.containingSignatureDeclaration = containingBlock.ContainingSignatureDeclaration;
      this.containingNamespaceDeclaration = containingBlock.ContainingNamespaceDeclaration;
      this.containingTypeDeclaration = containingBlock.ContainingTypeDeclaration;
      this.compilationPart = containingBlock.CompilationPart;
      foreach (Statement statement in this.statements) statement.SetContainingBlock(this);
    }

    /// <summary>
    /// The scope defined by this block.
    /// </summary>
    public StatementScope Scope {
      get {
        if (this.scope == null)
          this.scope = this.CreateStatementScope();
        return this.scope;
      }
    }
    StatementScope/*?*/ scope;

    /// <summary>
    /// If the block contains a single executable statement, that statement is returned. Otherwise null.
    /// </summary>
    public IStatement/*?*/ SingletonStatement {
      get {
        if (this.statements.Count == 1 && !(this.statements[0] is LocalDeclarationsStatement))
          return this.statements[0] = this.statements[0].MakeCopyFor(this);
        return null;
      }
    }

    /// <summary>
    /// The statements making up the block.
    /// </summary>
    public IEnumerable<Statement> Statements {
      get {
        for (int i = 0, n = this.statements.Count; i < n; i++) {
          yield return this.statements[i] = this.statements[i].MakeCopyFor(this);
        }
      }
    }
    readonly List<Statement> statements;

    /// <summary>
    /// True if, by default, all arithmetic expressions in the block must be checked for overflow. This setting is inherited by nested blocks and
    /// can be overridden by nested blocks and expressions.
    /// </summary>
    public bool UseCheckedArithmetic {
      get {
        if ((this.flags & 8) == 0) { //not yet initialized
          this.flags |= 8;
          if ((this.flags & 16) != 0 && this.ContainingBlock.UseCheckedArithmetic) //inherit 
            this.flags |= 32;
        }
        return (this.flags & 32) != 0;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public bool IsEmpty {
      get { return this.statements.Count == 0; }
    }

    private static readonly List<Statement> NoStatements = new List<Statement>(0);

    /// <summary>
    /// Creates an empty BlockStatement for the given source location with default options.
    /// </summary>
    public static BlockStatement CreateDummyFor(ISourceLocation sourceLocation) {
      return BlockStatement.CreateDummyFor(Options.Default, sourceLocation);
    }

    /// <summary>
    /// Creates an empty BlockStatement for the given options and source location.
    /// </summary>
    public static BlockStatement CreateDummyFor(Options options, ISourceLocation sourceLocation) {
      return new BlockStatement(NoStatements, options, sourceLocation);
    }

    #region IBlock Members

    IEnumerable<IStatement> IBlockStatement.Statements {
      get {
        foreach (Statement statement in this.Statements) {
          LocalDeclarationsStatement/*?*/ locDeclStatement = statement as LocalDeclarationsStatement;
          if (locDeclStatement == null) {
            FieldInitializerStatement/*?*/ fieldInitializer = statement as FieldInitializerStatement;
            if (fieldInitializer == null)
              yield return statement;
            else {
              foreach (Statement fieldAssignment in fieldInitializer.FieldInitializers)
                yield return fieldAssignment;
            }
          } else {
            foreach (LocalDeclaration declaration in locDeclStatement.Declarations)
              yield return declaration;
          }
        }
      }
    }

    #endregion

  }

  /// <summary>
  /// Breaks (exits) out of the innner most containing loop or switch case.
  /// </summary>
  public class BreakStatement : Statement, IBreakStatement {

    /// <summary>
    /// Terminates execution of the innermost loop statement or switch case containing this statement directly or indirectly.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public BreakStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected BreakStatement(BlockStatement containingBlock, BreakStatement template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //TODO: check if there is a finally clause in between the loop/switch case and the exit.
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(BreakStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(IBreakStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new BreakStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
    }

  }

  /// <summary>
  /// Represents a catch clause of a try-catch statement or a try-catch-finally statement. 
  /// </summary>
  public class CatchClause : SourceItem, ICatchClause {

    /// <summary>
    /// Represents a catch clause of a try-catch statement or a try-catch-finally statement. 
    /// </summary>
    /// <param name="exceptionType">The type of the exception to handle.</param>
    /// <param name="filterCondition">A condition that must evaluate to true if the catch clause is to be executed. 
    /// May be null, in which case any exception of ExceptionType will cause the handler to execute.</param>
    /// <param name="name">The name of the local definition that contains the exception value during the execution of the catch clause body. May be null.</param>
    /// <param name="body">The statements within the catch clause.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public CatchClause(TypeExpression exceptionType, Expression/*?*/ filterCondition, NameDeclaration/*?*/ name, BlockStatement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.exceptionType = exceptionType;
      this.filterCondition = filterCondition;
      this.name = name;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected CatchClause(BlockStatement containingBlock, CatchClause template)
      : base(template.SourceLocation) {
      this.body = (BlockStatement)template.Body.MakeCopyFor(containingBlock);
      this.exceptionType = (TypeExpression)template.ExceptionType.MakeCopyFor(containingBlock);
      if (template.FilterCondition != null)
        this.filterCondition = template.FilterCondition.MakeCopyFor(containingBlock);
      if (template.Name != null)
        this.name = new NameDeclaration(template.Name.Name, template.Name.SourceLocation);
    }

    /// <summary>
    /// The statements within the catch clause.
    /// </summary>
    public BlockStatement Body {
      get { return this.body; }
    }
    readonly BlockStatement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    public virtual bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ExceptionType.HasErrors;
      result |= this.Body.HasErrors;
      if (this.FilterCondition != null)
        result |= this.FilterCondition.HasErrors;
      return result;
    }

    /// <summary>
    /// Calls the visitor.Visit(ICatchClause) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(CatchClause) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The local that contains the exception instance when executing the catch clause body.
    /// </summary>
    public ILocalDefinition ExceptionContainer {
      get {
        if (this.exceptionContainer == null) {
          LocalDefinition exceptionContainer = this.GetNewExceptionContainer();
          lock (this) {
            if (this.exceptionContainer == null) this.exceptionContainer = exceptionContainer;
          }
        }
        return this.exceptionContainer;
      }
    }
    ILocalDefinition/*?*/ exceptionContainer;

    /// <summary>
    /// The type of the exception to handle.
    /// </summary>
    public TypeExpression ExceptionType {
      get { return this.exceptionType; }
    }
    readonly TypeExpression exceptionType;

    /// <summary>
    /// A condition that must evaluate to true if the catch clause is to be executed. 
    /// May be null, in which case any exception of ExceptionType will cause the handler to execute.
    /// </summary>
    public Expression/*?*/ FilterCondition {
      get { return this.filterCondition; }
    }
    readonly Expression/*?*/ filterCondition;

    /// <summary>
    /// Allocates the local that contains the exception instance when executing the catch clause body.
    /// </summary>
    private LocalDefinition GetNewExceptionContainer() {
      List<LocalDeclaration> declarations = new List<LocalDeclaration>(1);
      LocalDeclarationsStatement localDeclarationsStatement = new LocalDeclarationsStatement(false, true, false, this.ExceptionType, declarations, this.Name.SourceLocation);
      LocalDeclaration localDeclaration = new LocalDeclaration(false, false, this.Name, null, this.Name.SourceLocation);
      declarations.Add(localDeclaration);
      localDeclarationsStatement.SetContainingBlock(this.Body.ContainingBlock); //TODO: perhaps a new block?
      return localDeclaration.LocalVariable;
    }

    /// <summary>
    /// The name of the local definition that contains the exception value during the execution of the catch clause body.
    /// </summary>
    public NameDeclaration Name {
      get {
        if (this.name == null)
          this.name = new NameDeclaration(Dummy.Name, this.ExceptionType.SourceLocation);
        return this.name;
      }
    }
    NameDeclaration/*?*/ name;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public virtual CatchClause MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Body.ContainingBlock == containingBlock) return this;
      return new CatchClause(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingBlock(BlockStatement containingBlock) {
      this.Body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.ExceptionType.SetContainingExpression(containingExpression);
    }

    #region ICatchClause Members

    IBlockStatement ICatchClause.Body {
      get { return this.Body; }
    }

    ITypeReference ICatchClause.ExceptionType {
      get { return this.ExceptionType.ResolvedType; }
    }

    IExpression/*?*/ ICatchClause.FilterCondition {
      get {
        if (this.FilterCondition == null) return null;
        return this.FilterCondition.ProjectAsIExpression();
      }
    }

    #endregion
  }

  /// <summary>
  /// Nulls out the Err variable that records the last exeption that was caught in the method containing this statement.
  /// This statement corresponds to the "On Error Goto -1" statement in VB.
  /// </summary>
  public class ClearLastErrorStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public ClearLastErrorStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ClearLastErrorStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// An object that represents a statement consisting of two sub statements and a condition that governs which one of the two gets executed. Most languages refer to this as an "if statement".
  /// </summary>
  public class ConditionalStatement : Statement, IConditionalStatement {

    /// <summary>
    /// Allocates an object that represents a statement consisting of two sub statements and a condition that governs which one of the two gets executed. Most languages refer to this as an "if statement".
    /// </summary>
    /// <param name="condition">The expression to evaluate as true or false.</param>
    /// <param name="trueBranch">Statement to execute if the conditional expression evaluates to true. </param>
    /// <param name="falseBranch">Statement to execute if the conditional expression evaluates to false. </param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public ConditionalStatement(Expression condition, Statement trueBranch, Statement falseBranch, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.condition = condition;
      this.trueBranch = trueBranch;
      this.falseBranch = falseBranch;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ConditionalStatement(BlockStatement containingBlock, ConditionalStatement template)
      : base(containingBlock, template) {
      this.condition = template.Condition.MakeCopyFor(containingBlock);
      this.trueBranch = template.TrueBranch.MakeCopyFor(containingBlock);
      this.falseBranch = template.FalseBranch.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Condition.HasErrors || this.ConvertedCondition.HasErrors || this.TrueBranch.HasErrors || this.FalseBranch.HasErrors;
    }

    /// <summary>
    /// The expression to evaluate as true or false.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Calls the visitor.Visit(IConditionalStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ConditionalStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Statement to execute if the conditional expression evaluates to false. 
    /// </summary>
    public Statement FalseBranch {
      get { return this.falseBranch; }
    }
    readonly Statement falseBranch;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ConditionalStatement(containingBlock, this);
    }

    /// <summary>
    /// Statement to execute if the conditional expression evaluates to true. 
    /// </summary>
    public Statement TrueBranch {
      get { return this.trueBranch; }
    }
    readonly Statement trueBranch;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.condition.SetContainingExpression(containingExpression);
      this.falseBranch.SetContainingBlock(containingBlock);
      this.trueBranch.SetContainingBlock(containingBlock);
    }

    #region IConditionStatement Members

    IExpression IConditionalStatement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IStatement IConditionalStatement.TrueBranch {
      get {
        BlockStatement/*?*/ blockStatement = this.TrueBranch as BlockStatement;
        if (blockStatement != null) {
          IStatement/*?*/ s = blockStatement.SingletonStatement;
          if (s != null) return s;
        }
        return this.TrueBranch;
      }
    }

    IStatement IConditionalStatement.FalseBranch {
      get { return this.FalseBranch; }
    }

    #endregion

  }

  /// <summary>
  /// Terminates execution of the loop body containing this statement directly or indirectly and continues on to the loop exit condition test.
  /// </summary>
  public class ContinueStatement : Statement, IContinueStatement {

    /// <summary>
    /// Terminates execution of the loop body containing this statement directly or indirectly and continues on to the loop exit condition test.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public ContinueStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ContinueStatement(BlockStatement containingBlock, ContinueStatement template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //TODO: check if there is a finally clause in between the loop/switch case and the exit.
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(IContinueStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ContinueStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ContinueStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
    }

  }

  /// <summary>
  /// Disables the error handler set up by the last "On Error ..." statement executed in the method containing this statement.
  /// Should an exception occur after executing this statement and before executing another "On Error ..." statement, exceptions that would have
  /// been handled by the disabled error handler will now cause the method to be terminated and the exception to be passed on to the caller.
  /// </summary>
  public class DisableOnErrorHandler : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public DisableOnErrorHandler(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(DisableOnErrorHandler) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// Do statements while condition. Tests the condition after the body. Exits when the condition is false.
  /// </summary>
  public class DoWhileStatement : Statement {

    /// <summary>
    /// Do statements while condition. Tests the condition after the body. Exits when the condition is false.
    /// </summary>
    public DoWhileStatement(Statement body, Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.body = body;
      this.condition = condition;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected DoWhileStatement(BlockStatement containingBlock, DoWhileStatement template)
      : base(containingBlock, template) {
      this.body = template.Body.MakeCopyFor(containingBlock);
      this.condition = template.Condition.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ConvertedCondition.HasErrors;
      result |= Body.HasErrors;
      return result;
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// The condition to evaluate as false or true.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Calls the visitor.Visit(IDoUntil) method on a projection of this object onto a DoUntilStatement object.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      Expression invertedCondition = new LogicalNot(this.ConvertedCondition, this.Condition.SourceLocation);
      DoUntilStatement doUntil = new DoUntilStatement(this.Body, invertedCondition, this.SourceLocation);
      doUntil.SetContainingBlock(this.ContainingBlock);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) this.Compilation.ContractProvider.AssociateLoopWithContract(doUntil, contract);
      doUntil.Dispatch(visitor);
    }

    /// <summary>
    /// Calls the visitor.Visit(DoWhileStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new DoWhileStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.condition.SetContainingExpression(containingExpression);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(containingBlock);
    }

  }

  /// <summary>
  /// Do statements until condition. Tests the condition after the body. Exits when the condition is true.
  /// </summary>
  public class DoUntilStatement : Statement, IDoUntilStatement {

    /// <summary>
    /// Do statements until condition. Tests the condition after the body. Exits when the condition is true.
    /// </summary>
    /// <param name="body">The body of the loop.</param>
    /// <param name="condition">The condition to evaluate as false or true.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public DoUntilStatement(Statement body, Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.body = body;
      this.condition = condition;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected DoUntilStatement(BlockStatement containingBlock, DoUntilStatement template)
      : base(containingBlock, template) {
      this.body = template.Body.MakeCopyFor(containingBlock);
      this.condition = template.Condition.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ConvertedCondition.HasErrors;
      result |= this.Body.HasErrors;
      return result;
    }

    /// <summary>
    /// The condition to evaluate as false or true.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Calls the visitor.Visit(IDoUntilStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(DoUntilStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new DoUntilStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.condition.SetContainingExpression(containingExpression);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(containingBlock);
    }

    #region IDoUntilStatement Members

    IStatement IDoUntilStatement.Body {
      get { return this.Body; }
    }

    IExpression IDoUntilStatement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    #endregion

  }

  /// <summary>
  /// An empty statement.
  /// </summary>
  public class EmptyStatement : Statement, IEmptyStatement {

    /// <summary>
    /// Allocates an empty statement.
    /// </summary>
    /// <param name="isSentinel">True if this statement is a sentinel that should never be reachable.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public EmptyStatement(bool isSentinel, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.isSentinel = isSentinel;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected EmptyStatement(BlockStatement containingBlock, EmptyStatement template)
      : base(containingBlock, template) {
      this.isSentinel = template.IsSentinel;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(IEmptyStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(EmptyStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// True if this statement is a sentinel that should never be reachable.
    /// </summary>
    public bool IsSentinel {
      get { return this.isSentinel; }
    }
    readonly bool isSentinel;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new EmptyStatement(containingBlock, this);
    }

  }

  /// <summary>
  /// Terminates the current execution.
  /// </summary>
  public class EndStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public EndStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(EndStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// A statement that assigns null to each of its target expressions. This models the VB Erase statement.
  /// </summary>
  public class EraseStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targets"></param>
    /// <param name="sourceLocation"></param>
    public EraseStatement(IEnumerable<AddressableExpression> targets, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.targets = targets;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(EraseStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The target locations to be set to null.
    /// </summary>
    public IEnumerable<AddressableExpression> Targets {
      get { return this.targets; }
    }
    readonly IEnumerable<AddressableExpression> targets;

  }

  /// <summary>
  /// A statement that throws an instance of System.Exeception that wraps the error number value specified by ErrorNumber.
  /// This corresponds to the VB Error statement.
  /// </summary>
  public class ErrorStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="errorNumber"></param>
    /// <param name="sourceLocation"></param>
    public ErrorStatement(Expression errorNumber, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.errorNumber = errorNumber;
    }


    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ErrorStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Results in an error number that is wrapped with a System.Exception object that is thrown when the containing error statement is executed.
    /// </summary>
    public Expression ErrorNumber {
      get { return this.errorNumber; }
    }
    readonly Expression errorNumber;

  }

  /// <summary>
  /// An object that represents a statement that consists of a single expression.
  /// </summary>
  public class ExpressionStatement : Statement, IExpressionStatement {

    /// <summary>
    /// Allocates an object that represents a statement that consists of a single expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    public ExpressionStatement(Expression expression)
      : base(expression.SourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// Allocates an object that represents a statement that consists of a single expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public ExpressionStatement(Expression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ExpressionStatement(BlockStatement containingBlock, ExpressionStatement template)
      : base(containingBlock, template) {
      this.expression = template.Expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      Cast/*?*/ cast = this.Expression as Cast;
      if (cast != null) {
        //Casting to void is normally an error, but is OK if the cast is part of an expression statement.
        if (TypeHelper.TypesAreEquivalent(cast.TargetType.ResolvedType, this.PlatformType.SystemVoid))
          return cast.ValueToCast.HasErrors;
      }
      if (!(this.Expression is IMethodCall) && !(this.Expression is Parenthesis) && !this.Expression.HasSideEffect(false))
        this.Helper.ReportError(new AstErrorMessage(this.Expression, Error.ExpressionStatementHasNoSideEffect, this.Expression.SourceLocation.Source));
      return this.Expression.HasErrors;
    }

    /// <summary>
    /// Calls the visitor.Visit(IExpressionStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      PostfixUnaryOperationAssignment/*?*/ postFix = this.Expression as PostfixUnaryOperationAssignment;
      if (postFix != null)
        postFix.VisitAsUnaryOperationAssignment(visitor);
      else {
        BaseClassConstructorCall/*?*/ baseClassConstructorCall = this.Expression as BaseClassConstructorCall;
        if (baseClassConstructorCall != null && !(this.ContainingBlock.ContainingTypeDeclaration is IClassDeclaration))
          return; //Value types do not have a callable base class contructor.
        visitor.Visit(this);
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(ExpressionStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The expression.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    readonly Expression expression;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ExpressionStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.expression.SetContainingExpression(containingExpression);
    }

    #region IExpressionStatement Members

    IExpression IExpressionStatement.Expression {
      get {
        Cast/*?*/ cast = this.Expression as Cast;
        if (cast != null) {
          if (TypeHelper.TypesAreEquivalent(cast.TargetType.ResolvedType, this.PlatformType.SystemVoid))
            return cast.ValueToCast.ProjectAsIExpression();
        }
        return this.Expression.ProjectAsIExpression();
      }
    }

    #endregion
  }

  /// <summary>
  /// A statement that has no source equivalent and that serves as a marker for the place where a constructor
  /// should have assignment statements to initialize instance fields that have explicit initial value expressions.
  /// In C#, this initialization happens before the base class constructor is called, which happens before any
  /// of the explicit code in the constructor is executed.
  /// </summary>
  public class FieldInitializerStatement : Statement {

    /// <summary>
    /// Allocates a statement that has no source equivalent and that serves as a marker for the place where a constructor
    /// should have assignment statements to initialize instance fields that have explicit initial value expressions.
    /// In C#, this initialization happens before the base class constructor is called, which happens before any
    /// of the explicit code in the constructor is executed.
    /// </summary>
    public FieldInitializerStatement()
      : base(SourceDummy.SourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected FieldInitializerStatement(BlockStatement containingBlock, FieldInitializerStatement template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //Since this statement has no source location, it also should not report any errors.
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit on zero or more assigment statements that initialize instance fields of the containing type.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      Debug.Assert(false, "A FieldInitializerStatement should never be proffered as part of the Code Model.");
    }

    /// <summary>
    /// Does nothing. This statement is a parser artifact and has no source location.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
    }

    /// <summary>
    /// Returns a list of statements that will initialize any instance fields that have explicit initial values.
    /// </summary>
    public IEnumerable<Statement> FieldInitializers {
      get {
        if (this.fieldInitializers == null)
          this.fieldInitializers = this.GetFieldInitializers();
        return this.fieldInitializers;
      }
    }
    IEnumerable<Statement>/*?*/ fieldInitializers;

    private IEnumerable<Statement> GetFieldInitializers() {
      List<Statement> result = new List<Statement>();
      List<FieldDeclaration> fieldsToIntialize = new List<FieldDeclaration>();
      TypeDeclaration/*?*/ containingTypeDeclaration = this.ContainingBlock.ContainingTypeDeclaration;
      Debug.Assert(containingTypeDeclaration != null);
      foreach (TypeDeclaration typeDeclaration in containingTypeDeclaration.TypeDefinition.TypeDeclarations) {
        foreach (ITypeDeclarationMember member in typeDeclaration.TypeDeclarationMembers) {
          FieldDeclaration/*?*/ fieldDecl = member as FieldDeclaration;
          if (fieldDecl != null && !fieldDecl.IsStatic && fieldDecl.Initializer != null && !fieldDecl.Initializer.HasErrors) {
            if (TypeHelper.IsPrimitiveInteger(fieldDecl.Type.ResolvedType) && ExpressionHelper.IsIntegralZero(fieldDecl.Initializer.ProjectAsIExpression()))
              continue;
            fieldsToIntialize.Add(fieldDecl);
          }
        }
      }
      ThisReference thisArg = new ThisReference(SourceDummy.SourceLocation);
      foreach (FieldDeclaration field in fieldsToIntialize) {
        QualifiedName fieldRef = new QualifiedName(thisArg, new SimpleName(field.Name, SourceDummy.SourceLocation, false), SourceDummy.SourceLocation);
        TargetExpression target = new TargetExpression(new BoundExpression(fieldRef, field.FieldDefinition));
        //^ assume field.Initializer != null; //field wont be in the list unless the assumption holds
        Assignment initializeField = new Assignment(target, field.Initializer, field.Initializer.SourceLocation);
        ExpressionStatement statement = new ExpressionStatement(initializeField);
        statement.SetContainingBlock(this.ContainingBlock);
        result.Add(statement);
      }
      return result.AsReadOnly();
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new FieldInitializerStatement(containingBlock, this);
    }

  }

  /// <summary>
  /// Models the fixed statement in C#.
  /// </summary>
  public class FixedStatement : Statement {

    /// <summary>
    /// Models the fixed statement in C#.
    /// </summary>
    /// <param name="fixedPointerDeclarators">A local declarations statement that declares one or more pointer typed variables to hold the fixed pointers.</param>
    /// <param name="body">The body of the fixed statement.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public FixedStatement(LocalDeclarationsStatement fixedPointerDeclarators, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.fixedPointerDeclarators = fixedPointerDeclarators;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected FixedStatement(BlockStatement containingBlock, FixedStatement template)
      : base(containingBlock, template) {
      this.fixedPointerDeclarators = (LocalDeclarationsStatement)template.FixedPointerDeclarators.MakeCopyFor(containingBlock);
      this.body = template.Body.MakeCopyFor(containingBlock);
      this.DummyBlock.SetContainingBlock(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.FixedPointerDeclarators.HasErrors;
      result |= this.Body.HasErrors;
      return result;
    }

    /// <summary>
    /// Creates a new dummy block that provides a scope for the loop variable.
    /// </summary>
    protected BlockStatement CreateDummyBlock() {
      SourceLocationBuilder sloc = new SourceLocationBuilder(this.FixedPointerDeclarators.SourceLocation);
      sloc.UpdateToSpan(this.Body.SourceLocation);
      List<Statement> statements = new List<Statement>(2);
      statements.Add(this.FixedPointerDeclarators);
      statements.Add(this.Body);
      return new BlockStatement(statements, sloc);
    }

    /// <summary>
    /// Calls this.DummyBlock.Dispatch(visitor).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      this.DummyBlock.Dispatch(visitor);
    }

    /// <summary>
    /// Calls the visitor.Visit(FixedStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The body of the fixed statement.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// A dummy block that provides a scope for the loop variable.
    /// </summary>
    protected BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement block = this.CreateDummyBlock();
          lock (this) {
            if (this.dummyBlock == null)
              this.dummyBlock = block;
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    /// <summary>
    /// A local declarations statement that declares one or more pointer typed variables to hold the fixed pointers.
    /// </summary>
    public LocalDeclarationsStatement FixedPointerDeclarators {
      get { return this.fixedPointerDeclarators; }
    }
    readonly LocalDeclarationsStatement fixedPointerDeclarators;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new FixedStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      this.DummyBlock.SetContainingBlock(containingBlock);
      containingBlock = this.DummyBlock;
      base.SetContainingBlock(containingBlock);
      this.Body.SetContainingBlock(containingBlock);
      this.FixedPointerDeclarators.SetContainingBlock(containingBlock);
    }

  }

  /// <summary>
  /// Represents a foreach statement. Executes the loop body for each element of a collection.
  /// </summary>
  public class ForEachStatement : Statement, IForEachStatement {

    /// <summary>
    /// Represents a foreach statement. Executes the loop body for each element of a collection.
    /// </summary>
    /// <param name="variableType">The type of the foreach loop variable that holds the current element from the collection.</param>
    /// <param name="variableName">The name of the foreach loop variable that holds the current element from the collection.</param>
    /// <param name="collection">An epxression resulting in an enumerable collection of values (an object implementing System.Collections.IEnumerable).</param>
    /// <param name="body">The body of the loop.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public ForEachStatement(TypeExpression variableType, NameDeclaration variableName, Expression collection, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.variableType = variableType;
      this.variableName = variableName;
      this.collection = collection;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ForEachStatement(BlockStatement containingBlock, ForEachStatement template)
      : base(containingBlock, template) {
      this.variableType = (TypeExpression)template.VariableType.MakeCopyFor(containingBlock);
      this.variableName = template.VariableName.MakeCopyFor(containingBlock.Compilation);
      this.collection = template.Collection.MakeCopyFor(containingBlock);
      this.body = template.Body.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.VariableType.HasErrors;
      result |= this.Collection.HasErrors;
      result |= this.Body.HasErrors;
      return result;
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// An epxression resulting in an enumerable collection of values (an object implementing System.Collections.IEnumerable).
    /// </summary>
    public Expression Collection {
      get { return this.collection; }
    }
    readonly Expression collection;

    /// <summary>
    /// Creates a new dummy block that provides a scope for the loop variable.
    /// </summary>
    /// <param name="loopVar">An out parameter providing the loop variable definition from the newly created block.</param>
    /// <returns></returns>
    protected BlockStatement CreateDummyBlock(out LocalDefinition loopVar) {
      SourceLocationBuilder sloc = new SourceLocationBuilder(this.VariableType.SourceLocation);
      sloc.UpdateToSpan(this.VariableName.SourceLocation);
      LocalDeclaration localDeclaration = new LocalDeclaration(false, false, this.VariableName, null, this.VariableName.SourceLocation);
      loopVar = localDeclaration.LocalVariable;
      List<LocalDeclaration> decls = new List<LocalDeclaration>(1);
      decls.Add(localDeclaration);
      LocalDeclarationsStatement localDeclarationsStatement = new LocalDeclarationsStatement(false, false, false, this.VariableType, decls, sloc);
      List<Statement> statements = new List<Statement>(1);
      statements.Add(localDeclarationsStatement);
      return new BlockStatement(statements, localDeclarationsStatement.SourceLocation);
    }

    /// <summary>
    /// Calls the visitor.Visit(IForEachStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ForEachStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A dummy block that provides a scope for the loop variable.
    /// </summary>
    protected BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          LocalDefinition loopVar;
          BlockStatement block = this.CreateDummyBlock(out loopVar);
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = block;
              this.variable = loopVar;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ForEachStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.DummyBlock.SetContainingBlock(containingBlock);
      containingBlock = this.DummyBlock;
      this.Body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.Collection.SetContainingExpression(containingExpression);
      this.VariableType.SetContainingExpression(containingExpression);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(containingBlock);
    }

    /// <summary>
    /// The name of the foreach loop variable that holds the current element from the collection.
    /// </summary>
    public NameDeclaration VariableName {
      get { return this.variableName; }
    }
    readonly NameDeclaration variableName;

    /// <summary>
    /// The type of the foreach loop variable that holds the current element from the collection.
    /// </summary>
    public TypeExpression VariableType {
      get { return this.variableType; }
    }
    readonly TypeExpression variableType;

    /// <summary>
    /// The foreach loop variable that holds the current element from the collection.
    /// </summary>
    public ILocalDefinition Variable {
      get {
        if (this.variable == null) {
          if (this.DummyBlock == CodeDummy.Block) {
            this.variable = Dummy.LocalVariable;
          }
          //^ assume this.variable != null; //It is initialized by this.DummyBlock.get
        }
        return this.variable;
      }
    }
    ILocalDefinition/*?*/ variable;

    #region IForEachStatement Members

    IStatement IForEachStatement.Body {
      get { return this.Body; }
    }

    IExpression IForEachStatement.Collection {
      get { return this.Collection.ProjectAsIExpression(); }
    }

    #endregion
  }

  /// <summary>
  /// Represents a "for i = expr1 to expr2 step expr3 next i" statement in VB.
  /// </summary>
  public class ForRangeStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="variableTypeExpression"></param>
    /// <param name="variableName"></param>
    /// <param name="range"></param>
    /// <param name="step"></param>
    /// <param name="body"></param>
    /// <param name="sourceLocation"></param>
    public ForRangeStatement(TypeExpression/*?*/ variableTypeExpression, SimpleName variableName, Range range, Expression/*?*/ step, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.variableTypeExpression = variableTypeExpression;
      this.variableName = variableName;
      this.range = range;
      this.step = step;
      this.body = body;
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// An expression that returns true if the loop variable falls inside the range.
    /// </summary>
    public Expression Condition {
      get {
        Expression le = new LessThanOrEqual(this.VariableName, this.Range.EndValue, this.Range.EndValue.SourceLocation);
        le.SetContainingExpression(new DummyExpression(this.ContainingBlock, this.SourceLocation));
        return le;
      }
    }

    /// <summary>
    /// Projects the statement onto a ForStatement and calls visitor.Visit(IForStatement).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      List<Statement> initStatements = new List<Statement>(1);
      initStatements.Add(this.Initializer);
      List<Statement> incrementStatements = new List<Statement>(1);
      incrementStatements.Add(this.Incrementer);
      ForStatement forStatement = new ForStatement(initStatements, this.Condition, incrementStatements, this.Body, this.SourceLocation);
      forStatement.SetContainingBlock(this.ContainingBlock);
      visitor.Visit(forStatement);
    }

    /// <summary>
    /// Calls the visitor.Visit(ForRangeStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A statement that increments the loop variable (by Step, if specified, otherwise by 1).
    /// </summary>
    public Statement Incrementer {
      get {
        ExpressionStatement result;
        if (this.Step != null)
          result = new ExpressionStatement(new AdditionAssignment(new TargetExpression(this.VariableName), this.Step, this.Range.StartValue.SourceLocation));
        else
          result = new ExpressionStatement(new PrefixIncrement(new TargetExpression(this.VariableName), this.VariableName.SourceLocation));
        result.SetContainingBlock(this.ContainingBlock);
        return result;
      }
    }

    /// <summary>
    /// A statement that initializes the loop variable.
    /// </summary>
    public Statement Initializer {
      get {
        ExpressionStatement result = new ExpressionStatement(new Assignment(new TargetExpression(this.VariableName), this.Range.StartValue, this.Range.StartValue.SourceLocation));
        result.SetContainingBlock(this.ContainingBlock);
        return result;
      }
    }

    /// <summary>
    /// An expression resulting in an enumerable range of numeric values.
    /// </summary>
    public Range Range {
      get { return this.range; }
    }
    readonly Range range;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.range.SetContainingExpression(containingExpression);
      if (this.step != null) this.step.SetContainingExpression(containingExpression);
      if (this.variableName != null) this.variableName.SetContainingExpression(containingExpression);
      if (this.variableTypeExpression != null) this.variableTypeExpression.SetContainingExpression(containingExpression);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(containingBlock);
    }

    /// <summary>
    /// An expression resulting in a number that is repeatedly added to the starting value of Range 
    /// until the result is greater than or equal to the ending value of the Range. May be null.
    /// </summary>
    public Expression/*?*/ Step {
      get { return this.step; }
    }
    readonly Expression/*?*/ step;

    /// <summary>
    /// The for range loop variable that holds the number from the range.
    /// </summary>
    public SimpleName VariableName {
      get { return this.variableName; }
    }
    readonly SimpleName variableName;

    /// <summary>
    /// 
    /// </summary>
    public TypeExpression/*?*/ VariableTypeExpression {
      get { return this.variableTypeExpression; }
    }
    readonly TypeExpression/*?*/ variableTypeExpression;

    /// <summary>
    /// 
    /// </summary>
    public ITypeDefinition VariableType {
      get {
        if (this.VariableTypeExpression != null)
          return this.VariableTypeExpression.ResolvedType;
        return this.Range.StartValue.Type; //TODO: unify types of start val, end val and step val.
      }
    }

  }

  /// <summary>
  /// Represents a for statement, or a loop through a block of statements, using a test expression as a condition for continuing to loop.
  /// </summary>
  public class ForStatement : Statement, IForStatement {

    /// <summary>
    /// Represents a for statement, or a loop through a block of statements, using a test expression as a condition for continuing to loop.
    /// </summary>
    /// <param name="initStatements">The loop initialization statements.</param>
    /// <param name="condition">The expression to evaluate as true or false, which determines if the loop is to continue.</param>
    /// <param name="incrementStatements">Statements that are called after each loop cycle, typically to increment a counter.</param>
    /// <param name="body">The statements making up the body of the loop.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public ForStatement(List<Statement> initStatements, Expression condition, List<Statement> incrementStatements, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.initStatements = initStatements;
      this.condition = condition;
      this.incrementStatements = incrementStatements;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ForStatement(BlockStatement containingBlock, ForStatement template)
      : base(containingBlock, template) {
      this.body = template.Body.MakeCopyFor(containingBlock);
      this.condition = template.Condition.MakeCopyFor(containingBlock);
      this.incrementStatements = new List<Statement>(template.IncrementStatements);
      this.initStatements = new List<Statement>(template.InitStatements);
    }

    /// <summary>
    /// The statements making up the body of the loop.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ConvertedCondition.HasErrors;
      foreach (Statement statement in this.InitStatements)
        result |= statement.HasErrors;
      foreach (Statement statement in this.IncrementStatements)
        result |= statement.HasErrors;
      result |= this.Body.HasErrors;
      return result;
    }

    /// <summary>
    /// The expression to evaluate as true or false, which determines if the loop is to continue.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition {
      get {
        Expression/*?*/ result = this.convertedCondition;
        if (result == null)
          this.convertedCondition = result = new IsTrue(this.Condition);
        return result;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Calls the visitor.Visit(IForStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ForStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A block that encloses the initializer statements, condition, increment statements and the for body.
    /// </summary>
    public BlockStatement ForBlock {
      get {
        if (this.forBlock == null) {
          BlockStatement forBlock = this.GetNewForBlock();
          lock (this) {
            if (this.forBlock == null) this.forBlock = forBlock;
          }
        }
        return this.forBlock;
      }
    }
    BlockStatement/*?*/ forBlock;

    /// <summary>
    /// Allocates block that encloses the initializer statements, condition, increment statements and the for body.
    /// </summary>
    private BlockStatement GetNewForBlock() {
      List<Statement> statements = new List<Statement>(this.initStatements.Count + this.incrementStatements.Count);
      BlockStatement forBlock = new BlockStatement(statements, this.SourceLocation);
      statements.AddRange(this.initStatements);
      statements.AddRange(this.incrementStatements);
      forBlock.SetContainingBlock(this.ContainingBlock);
      return forBlock;
    }

    /// <summary>
    /// Statements that are called after each loop cycle, typically to increment a counter.
    /// </summary>
    public IEnumerable<Statement> IncrementStatements {
      get {
        for (int i = 0, n = this.incrementStatements.Count; i < n; i++)
          yield return this.incrementStatements[i] = this.incrementStatements[i].MakeCopyFor(this.ForBlock);
      }
    }
    readonly List<Statement> incrementStatements;

    /// <summary>
    /// The loop initialization statements.
    /// </summary>
    public IEnumerable<Statement> InitStatements {
      get {
        for (int i = 0, n = this.initStatements.Count; i < n; i++)
          yield return this.initStatements[i] = this.initStatements[i].MakeCopyFor(this.ForBlock);
      }
    }
    readonly List<Statement> initStatements;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ForStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.ForBlock.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(this.ForBlock, SourceDummy.SourceLocation);
      this.Condition.SetContainingExpression(containingExpression);
      this.Body.SetContainingBlock(this.ForBlock);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(this.ForBlock);
    }

    #region IForStatement Members

    IStatement IForStatement.Body {
      get { return this.Body; }
    }

    IExpression IForStatement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IEnumerable<IStatement> IForStatement.IncrementStatements {
      get {
        return IteratorHelper.GetConversionEnumerable<Statement, IStatement>(this.IncrementStatements);
      }
    }

    IEnumerable<IStatement> IForStatement.InitStatements {
      get {
        foreach (Statement statement in this.InitStatements) {
          LocalDeclarationsStatement/*?*/ locDeclStatement = statement as LocalDeclarationsStatement;
          if (locDeclStatement == null)
            yield return statement;
          else {
            foreach (LocalDeclaration declaration in locDeclStatement.Declarations)
              yield return declaration;
          }
        }
      }
    }


    #endregion

  }

  /// <summary>
  /// Represents a goto statement.
  /// </summary>
  public class GotoStatement : Statement, IGotoStatement {

    /// <summary>
    /// Represents a goto statement.
    /// </summary>
    /// <param name="targetLabel">The label of the statement at which the program execution is to continue.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public GotoStatement(SimpleName targetLabel, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.targetLabel = targetLabel;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected GotoStatement(BlockStatement containingBlock, GotoStatement template)
      : base(containingBlock, template) {
      this.targetLabel = (SimpleName)template.TargetLabel.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      if (this.TargetStatement == CodeDummy.LabeledStatement) {
        this.Helper.ReportError(new AstErrorMessage(this.TargetLabel, Error.LabelNotFound, this.TargetLabel.Name.Value));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(IGotoStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(GotoStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new GotoStatement(containingBlock, this);
    }

    /// <summary>
    /// The label of the statement at which the program execution is to continue.
    /// </summary>
    public SimpleName TargetLabel {
      get { return this.targetLabel; }
    }
    readonly SimpleName targetLabel;

    /// <summary>
    /// The statement at which the program execution is to continue.
    /// </summary>
    public ILabeledStatement TargetStatement {
      get {
        if (this.targetStatement == null)
          this.targetStatement = this.TargetLabel.ResolveAsTargetStatement();
        return this.targetStatement;
      }
    }
    ILabeledStatement/*?*/ targetStatement;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.targetLabel.SetContainingExpression(containingExpression);
    }

  }

  /// <summary>
  /// Represents a "goto case x;" or "goto default" statement in C#.
  /// </summary>
  public class GotoSwitchCaseStatement : Statement, IGotoSwitchCaseStatement {

    /// <summary>
    /// Represents a "goto case x;" or "goto default" statement in C#.
    /// </summary>
    /// <param name="targetCaseLabel">The case label (constant) of the switch statement case clause to which this statement transfers control to. May be null (for the default case).</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public GotoSwitchCaseStatement(Expression/*?*/ targetCaseLabel, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.targetCaseLabel = targetCaseLabel;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected GotoSwitchCaseStatement(BlockStatement containingBlock, GotoSwitchCaseStatement template)
      : base(containingBlock, template) {
      if (template.TargetCaseLabel != null)
        this.targetCaseLabel = template.TargetCaseLabel.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //TODO: check that the case label is a constant
      return this.TargetCase == CodeDummy.SwitchCase;
    }

    /// <summary>
    /// Calls the visitor.Visit(IGotoSwitchCaseStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(GotoSwitchCaseStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The case label (constant) of the switch statement case clause to which this statement transfers control to. May be null (for the default case).
    /// </summary>
    public Expression/*?*/ TargetCaseLabel {
      get { return this.targetCaseLabel; }
    }
    readonly Expression/*?*/ targetCaseLabel;

    /// <summary>
    /// The switch statement case clause to which this statement transfers control to.
    /// </summary>
    public ISwitchCase TargetCase {
      get {
        //TODO: find the most nested enclosing switch statement and search it for a matching case.
        return CodeDummy.SwitchCase;
      }
    }

  }

  /// <summary>
  /// An object that represents a labeled statement or a stand-alone label.
  /// </summary>
  public class LabeledStatement : Statement, ILabeledStatement {

    /// <summary>
    /// Allocates an object that represents a labeled statement or a stand-alone label.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="statement">The associated statement. Contains an empty statement if this is a stand-alone label.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public LabeledStatement(NameDeclaration label, Statement statement, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.label = label;
      this.statement = statement;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected LabeledStatement(BlockStatement containingBlock, LabeledStatement template)
      : base(containingBlock, template) {
      this.label = template.Label.MakeCopyFor(containingBlock.Compilation);
      this.statement = template.Statement.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
      //TODO: check for duplicate label, etc.
    }

    /// <summary>
    /// Calls the visitor.Visit(ILabeledStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(LabeledStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The label.
    /// </summary>
    public NameDeclaration Label {
      get { return this.label; }
    }
    readonly NameDeclaration label;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new LabeledStatement(containingBlock, this);
    }

    /// <summary>
    /// The associated statement. Contains an empty statement if this is a stand-alone label.
    /// </summary>
    public Statement Statement {
      get { return this.statement; }
    }
    readonly Statement statement;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.statement.SetContainingBlock(containingBlock);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(containingBlock);
    }

    #region ILabeledStatement Members

    IName ILabeledStatement.Label {
      get { return this.Label; }
    }

    IStatement ILabeledStatement.Statement {
      get { return this.statement; }
    }

    #endregion
  }

  /// <summary>
  /// A type and name pair to be used as a bound variable in a quantifier expression.
  /// </summary>
  public class QuantifierVariable : CheckableSourceItem {
    /// <summary>
    /// Allocates local declaration that appears as part of a statement containing a collection of local declarations, all with the same type.
    /// </summary>
    /// <param name="type">The type of the variable.</param>
    /// <param name="name">The name of the variable.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public QuantifierVariable(TypeExpression type, NameDeclaration name, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.type = type;
      this.name = name;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block. This should be different from the containing block of the template declaration.</param>
    /// <param name="template">The statement to copy.</param>
    protected QuantifierVariable(BlockStatement containingBlock, QuantifierVariable template)
      : base(template.SourceLocation) {
      this.containingBlock = containingBlock;
      this.type = (TypeExpression)template.Type.MakeCopyFor(containingBlock);
      this.name = template.Name.MakeCopyFor(containingBlock.Compilation);
    }

    /// <summary>
    /// The block that contains the bound variable.
    /// </summary>
    public BlockStatement ContainingBlock {
      get {
        //^ assume this.containingBlock != null;
        return this.containingBlock;
      }
    }
    BlockStatement/*?*/ containingBlock;

    /// <summary>
    /// Calls the visitor.Visit(BoundVariable) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the item or a constituent part of the item.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Type.HasErrors;
    }

    /// <summary>
    /// Makes a copy of this local declaration, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public virtual QuantifierVariable MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new QuantifierVariable(containingBlock, this);
    }

    /// <summary>
    /// The name of the variable.
    /// </summary>
    public NameDeclaration Name {
      get { return this.name; }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingBlock(BlockStatement containingBlock)
      //^ ensures this.ContainingBlock == containingBlock;
    {
      this.containingBlock = containingBlock;
    }

    /// <summary>
    /// The type of the variable.
    /// </summary>
    public TypeExpression Type {
      get { return this.type; }
    }
    readonly TypeExpression type;

  }

  /// <summary>
  /// A local declaration that holds a pointer to a fixed object.
  /// </summary>
  public class FixedPointerDeclaration : LocalDeclaration {
    /// <summary>
    /// Allocates local declaration that holds a pointer to a fixed object.
    /// </summary>
    /// <param name="name">The name of the local.</param>
    /// <param name="initialValue">The value, if any, to assign to the local as its initial value. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public FixedPointerDeclaration(NameDeclaration name, Expression initialValue, ISourceLocation sourceLocation)
      : base(true, true, name, initialValue, sourceLocation) {
    }
    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingLocalDeclarationsStatement">The containing statement. This should be different from the containing statement of the template declaration.</param>
    /// <param name="template">The statement to copy.</param>
    protected FixedPointerDeclaration(LocalDeclarationsStatement containingLocalDeclarationsStatement, FixedPointerDeclaration template)
      : base(containingLocalDeclarationsStatement, template) {
    }

    /// <summary>
    /// Returns an expression that resulting in an implicit conversion of this.InitialValue to this.Type. 
    /// If this.InitialValue is null, a dummy expression is returned.
    /// Call this method via this.ConvertedValue, so that its result can get cached.
    /// </summary>
    protected override Expression ConvertInitialValue() {
      //TODO: if expression is an array, load the address of element 0
      //TODO: convert pointer
      return this.InitialValue;
    }

    /// <summary>
    /// Calls the visitor.Visit(IExpressionStatement) method on an assignment statement that initializes the local. 
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //TODO: if the initial value is a string, introduce a second local and assign the sum of the pinned pointer plus the string offset to the second local
      //Expression dummyExpression = new DummyExpression(this.Name.SourceLocation);
      //TargetExpression targetExpression = new TargetExpression(new BoundExpression(dummyExpression, this.LocalVariable), this.Name.SourceLocation);
      //Assignment assignment = new Assignment(targetExpression, this.ConvertedInitialValue, this.SourceLocation);
      //ExpressionStatement aStat = new ExpressionStatement(assignment);
      //aStat.SetContainingBlock(this.ContainingLocalDeclarationsStatement.ContainingBlock);
      //visitor.Visit(aStat);
    }

    /// <summary>
    /// Calls the visitor.Visit(LocalDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }


    /// <summary>
    /// Makes a copy of this local declaration, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override LocalDeclaration MakeCopyFor(LocalDeclarationsStatement containingLocalDeclarationsStatement)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingLocalDeclarationsStatement == containingLocalDeclarationsStatement) return this;
      return new FixedPointerDeclaration(containingLocalDeclarationsStatement, this);
    }

  }


  /// <summary>
  /// A local declaration that appears as part of a statement containing a collection of local declarations, all with the same type.
  /// </summary>
  public class LocalDeclaration : CheckableSourceItem, IScopeMember<IScope<LocalDeclaration>>, ILocalDeclarationStatement {

    /// <summary>
    /// Allocates local declaration that appears as part of a statement containing a collection of local declarations, all with the same type.
    /// </summary>
    /// <param name="isReference">True if the local contains a managed pointer (for example a reference to an object or a reference to a field of an object).</param>
    /// <param name="isPinned">True if the value referenced by the local must not be moved by the actions of the garbage collector.</param>
    /// <param name="name">The name of the local.</param>
    /// <param name="initialValue">The value, if any, to assign to the local as its initial value. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public LocalDeclaration(bool isReference, bool isPinned, NameDeclaration name, Expression/*?*/ initialValue, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires isPinned ==> isReference;
    {
      this.name = name;
      this.initialValue = initialValue;
      int flags = 0;
      if (isReference) flags |= 1;
      if (isPinned) flags |= 2;
      this.flags = flags;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingLocalDeclarationsStatement">The containing statement. This should be different from the containing statement of the template declaration.</param>
    /// <param name="template">The statement to copy.</param>
    protected LocalDeclaration(LocalDeclarationsStatement containingLocalDeclarationsStatement, LocalDeclaration template)
      : base(template.SourceLocation) {
      this.containingLocalDeclarationsStatement = containingLocalDeclarationsStatement;
      this.name = template.Name.MakeCopyFor(containingLocalDeclarationsStatement.ContainingBlock.Compilation);
      Expression/*?*/ initialValue = template.InitialValue;
      if (initialValue != null)
        this.initialValue = initialValue.MakeCopyFor(containingLocalDeclarationsStatement.ContainingBlock);
      this.flags = template.flags;
    }

    /// <summary>
    /// The compile time value of the declaration, if it is a local constant.
    /// </summary>
    public CompileTimeConstant CompileTimeValue {
      get {
        if (this.compileTimeValue == null)
          this.compileTimeValue = this.GetCompileTimeValue();
        return this.compileTimeValue;
      }
    }
    //^ [Once]
    CompileTimeConstant/*?*/ compileTimeValue;

    /// <summary>
    /// The statement that contains the local declaration.
    /// </summary>
    public LocalDeclarationsStatement ContainingLocalDeclarationsStatement {
      get {
        //^ assume this.containingLocalDeclarationsStatement != null;
        return this.containingLocalDeclarationsStatement;
      }
    }
    LocalDeclarationsStatement/*?*/ containingLocalDeclarationsStatement;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.DeclarationNameIsInUse() || (this.InitialValue != null && this.ConvertedInitialValue.HasErrors);
    }

    /// <summary>
    /// Returns an expression that resulting in an implicit conversion of this.InitialValue to this.Type. 
    /// If this.InitialValue is null, a dummy expression is returned.
    /// </summary>
    public Expression ConvertedInitialValue {
      get {
        if (this.convertedInitialValue == null)
          this.convertedInitialValue = this.ConvertInitialValue();
        return this.convertedInitialValue;
      }
    }
    Expression/*?*/ convertedInitialValue;

    /// <summary>
    /// Returns an expression that resulting in an implicit conversion of this.InitialValue to this.Type. 
    /// If this.InitialValue is null, a dummy expression is returned.
    /// Call this method via this.ConvertedValue, so that its result can get cached.
    /// </summary>
    protected virtual Expression ConvertInitialValue() {
      if (this.InitialValue == null) return new DummyExpression(SourceDummy.SourceLocation);
      Expression result = this.ContainingLocalDeclarationsStatement.Helper.ImplicitConversionInAssignmentContext(this.InitialValue, this.Type);
      if (result is DummyExpression && this.InitialValue != null && !this.InitialValue.HasErrors) {
        if (this.InitialValue.Type is Dummy && this.Type.IsDelegate)
          this.ContainingLocalDeclarationsStatement.Helper.ReportFailedMethodGroupToDelegateConversion(this.InitialValue, this.Type);
        else
          this.ContainingLocalDeclarationsStatement.Helper.ReportFailedImplicitConversion(this.InitialValue, this.Type);
      }
      return result;
    }

    /// <summary>
    /// Returns true if the name of this declaration has already been used in the current scope or an outer scope.
    /// </summary>
    /// <returns></returns>
    protected virtual bool DeclarationNameIsInUse() {
      return false;
      //TODO: run up scope chain and do the C# thing
    }

    /// <summary>
    /// Calls the visitor.Visit((ILocalDeclarationStatement)this).
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(LocalDeclaration) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// 
    /// </summary>
    protected int flags;

    /// <summary>
    /// Returns the value of the intializer expression, provided that the expression
    /// has been provided, can be converted to the type of the declaration and has a value at compile time.
    /// Otherwise returns an instance of DummyConstant.
    /// </summary>
    protected virtual CompileTimeConstant GetCompileTimeValue() {
      CompileTimeConstant/*?*/ result = null;
      Expression/*?*/ convertedInitializer = this.ConvertedInitialValue;
      if (convertedInitializer != null) {
        result = convertedInitializer as CompileTimeConstant;
        if (result == null) {
          object/*?*/ value = convertedInitializer.Value;
          if (value != null) {
            CompileTimeConstant ctc = new CompileTimeConstant(value, convertedInitializer.SourceLocation);
            ctc.UnfoldedExpression = convertedInitializer;
            ctc.SetContainingExpression(convertedInitializer);
            result = ctc;
          }
        }
      }
      if (result == null) {
        ISourceLocation sourceLocation = SourceDummy.SourceLocation;
        if (this.InitialValue != null) sourceLocation = this.InitialValue.SourceLocation;
        result = new DummyConstant(sourceLocation);
      }
      return result;
    }

    /// <summary>
    /// The value, if any, to assign to the local as its initial value. May be null.
    /// </summary>
    public virtual Expression/*?*/ InitialValue {
      get { return this.initialValue; }
    }
    readonly Expression/*?*/ initialValue;

    /// <summary>
    /// True if this local declaration is readonly and initialized with a compile time constant value.
    /// </summary>
    public bool IsConstant {
      get { return this.ContainingLocalDeclarationsStatement.IsConstant; }
    }

    /// <summary>
    /// True if the value referenced by the local must not be moved by the actions of the garbage collector.
    /// </summary>
    public bool IsPinned {
      get { return (this.flags & 1) != 0; }
    }

    /// <summary>
    /// True if the local contains a managed pointer (for example a reference to an object or a reference to a field of an object).
    /// </summary>
    public bool IsReference {
      get { return (this.flags & 2) != 0; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    protected virtual LocalDefinition CreateLocalDefinition() {
      return new LocalDefinition(this);
    }

    /// <summary>
    /// The local variable that corresponds to this declaration. It is not valid to evaluate this property if the declaration is for a constant.
    /// </summary>
    public LocalDefinition LocalVariable {
      get {
        if (this.localVariable == null) {
          LocalDefinition def = CreateLocalDefinition();
          lock (this) {
            if (this.localVariable == null)
              this.localVariable = def;
          }
        }
        return this.localVariable;
      }
    }
    private LocalDefinition/*?*/ localVariable;

    /// <summary>
    /// Makes a copy of this local declaration, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public virtual LocalDeclaration MakeCopyFor(LocalDeclarationsStatement containingLocalDeclarationsStatement)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingLocalDeclarationsStatement == containingLocalDeclarationsStatement) return this;
      return new LocalDeclaration(containingLocalDeclarationsStatement, this);
    }

    /// <summary>
    /// The name of the local.
    /// </summary>
    public NameDeclaration Name {
      get { return this.name; }
    }
    readonly NameDeclaration name;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingLocalDeclarationsStatement(LocalDeclarationsStatement containingLocalDeclarationsStatement)
      //^ ensures this.ContainingLocalDeclarationsStatement == containingLocalDeclarationsStatement;
    {
      this.containingLocalDeclarationsStatement = containingLocalDeclarationsStatement;
      DummyExpression containingExpression = new DummyExpression(containingLocalDeclarationsStatement.ContainingBlock, SourceDummy.SourceLocation);
      if (this.initialValue != null) this.initialValue.SetContainingExpression(containingExpression);
      //^ assume this.ContainingLocalDeclarationsStatement == containingLocalDeclarationsStatement;
    }

    /// <summary>
    /// The type of the local.
    /// </summary>
    public virtual ITypeDefinition Type {
      get {
        return this.ContainingLocalDeclarationsStatement.Type;
      }
    }

    #region ILocalDeclarationStatement Members

    IExpression/*?*/ ILocalDeclarationStatement.InitialValue {
      get {
        if (this.InitialValue == null) return null;
        return this.ConvertedInitialValue.ProjectAsIExpression();
      }
    }

    ILocalDefinition ILocalDeclarationStatement.LocalVariable {
      get { return this.LocalVariable; }
    }

    #endregion

    #region IScopeMember<IScope<ILocalDeclaration>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<LocalDeclaration> ContainingScope {
      get { return this.ContainingLocalDeclarationsStatement.ContainingBlock.Scope; }
    }

    #endregion

    #region INamedEntity Members

    IName INamedEntity.Name {
      get { return this.Name; }
    }

    #endregion
  }

  /// <summary>
  /// A single statement declaring one or more locals of the same type.
  /// </summary>
  public class LocalDeclarationsStatement : Statement {

    /// <summary>
    /// A single statement declaring one or more locals of the same type.
    /// </summary>
    /// <param name="isConstant">True if the local declarations are readonly and initialized with compile time constant values.</param>
    /// <param name="isReadOnly">True if it is an error to assign to the local declarations after they have been initialized.</param>
    /// <param name="mayInferType">True if it is not an error if the value of type expression does not resolve, but an indication that the type of 
    /// declarations must be inferred from the types of their initial values.</param>
    /// <param name="typeExpression">All of the locals declared by this statement are of the type to which this expression resolves.</param>
    /// <param name="declarations">The individual local declarations making up the statement. The collection has at least one element.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public LocalDeclarationsStatement(bool isConstant, bool isReadOnly, bool mayInferType, TypeExpression/*?*/ typeExpression, List<LocalDeclaration> declarations, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires typeExpression == null ==> mayInferType; //TODO: get rid of the flag and use nullness of typeExpression instead.
    {
      int flags = 0;
      if (isConstant) flags = 1;
      if (isReadOnly) flags = 2;
      if (mayInferType) flags |= 4;
      this.flags = flags;
      this.typeExpression = typeExpression;
      this.declarations = declarations;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected LocalDeclarationsStatement(BlockStatement containingBlock, LocalDeclarationsStatement template)
      : base(containingBlock, template) {
      this.flags = template.flags;
      if (template.TypeExpression != null)
        this.typeExpression = (TypeExpression)template.TypeExpression.MakeCopyFor(containingBlock);
      this.declarations = new List<LocalDeclaration>(template.Declarations);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      if (this.TypeExpression != null) result = this.TypeExpression.HasErrors;
      foreach (LocalDeclaration localDeclaration in this.Declarations) {
        result = result || localDeclaration.HasErrors;
        if (!result && this.TypeExpression.ResolvedType.TypeCode == PrimitiveTypeCode.Void) {
          this.Helper.ReportError(new AstErrorMessage(this, Error.IllegalUseOfType, localDeclaration.Name.Value, this.Helper.GetTypeName(this.PlatformType.SystemVoid.ResolvedType)));
          result = true;
        }
      }
      return result;
    }

    /// <summary>
    /// The individual local declarations making up the statement. The collection has at least one element.
    /// </summary>
    public IEnumerable<LocalDeclaration> Declarations {
      get {
        for (int i = 0, n = this.declarations.Count; i < n; i++)
          yield return this.declarations[i] = this.declarations[i].MakeCopyFor(this);
      }
    }
    readonly List<LocalDeclaration> declarations;

    /// <summary>
    /// This should never get called.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      Debug.Assert(false, "A LocalDeclarationStatement should never be proffered as part of the Code Model.");
    }

    /// <summary>
    /// Calls the visitor.Visit(LocalDeclarationsStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// 1 == IsConstant, 2 == IsReadOnly, 4 = MayInferType
    /// </summary>
    protected int flags;

    /// <summary>
    /// Tries to resolve this.TypeExpression. If it does not resolve and this.MayInferType is true and the type expression is the simple name "var",
    /// then infers a common base type for each of the local declaration initial values. If resolution and inference fail, returns Dummy.Type.
    /// </summary>
    protected virtual ITypeDefinition InferType() {
      ITypeDefinition/*?*/ result = null;
      if (this.TypeExpression != null) return this.TypeExpression.ResolvedType;
      for (int i = 0, n = this.declarations.Count; i < n; i++) {
        LocalDeclaration localDeclaration = this.declarations[i];
        Expression/*?*/ initialValue = localDeclaration.InitialValue;
        if (initialValue == null) {
          //TODO: error message about decl not having an initializer
          continue;
        }
        ITypeDefinition dtype = initialValue.Type;
        if (dtype is Dummy) continue;
        if (result == null)
          result = dtype;
        else {
          if (!TypeHelper.TypesAreEquivalent(result, dtype)) {
            //TODO: error about types not being the same
          }
        }
      }
      if (result != null) return result;
      return Dummy.Type;
    }

    /// <summary>
    /// True if the local declarations are readonly and initialized with compile time constant values.
    /// </summary>
    public bool IsConstant {
      get { return (this.flags & 1) != 0; }
    }

    /// <summary>
    /// True if it is an error to assign to the local declarations after they have been initialized.
    /// </summary>
    public bool IsReadOnly {
      get { return (this.flags & 2) != 0; }
    }

    /// <summary>
    /// True if it is not an error if the value of type expression does not resolve, but an indication that the type of 
    /// declarations must be inferred from the types of their initial values.
    /// </summary>
    public bool MayInferType {
      get { return (this.flags & 4) != 0; }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      if (this.typeExpression != null)
        this.typeExpression.SetContainingExpression(containingExpression);
      foreach (LocalDeclaration declaration in this.declarations) declaration.SetContainingLocalDeclarationsStatement(this);
    }

    /// <summary>
    /// All of the locals declared by this statement are of this type.
    /// </summary>
    public ITypeDefinition Type {
      get {
        if (this.type == null)
          this.type = this.InferType();
        return this.type;
      }
    }
    ITypeDefinition/*?*/ type;

    /// <summary>
    /// All of the locals declared by this statement are of the type to which this expression resolves.
    /// </summary>
    public TypeExpression/*?*/ TypeExpression {
      get { return this.typeExpression; }
    }
    readonly TypeExpression/*?*/ typeExpression;

  }

  /// <summary>
  /// An object that represents a local variable or constant.
  /// </summary>
  public class LocalDefinition : ILocalDefinition {

    /// <summary>
    /// Allocates an object that represents a local variable or constant.
    /// </summary>
    /// <param name="localDeclaration">The local declaration that is projected onto this definition.</param>
    public LocalDefinition(LocalDeclaration localDeclaration) {
      this.localDeclaration = localDeclaration;
    }

    /// <summary>
    /// The compile time value of the definition, if it is a local constant.
    /// </summary>
    public CompileTimeConstant CompileTimeValue {
      get
        //^ requires this.IsConstant;
      {
        return this.LocalDeclaration.CompileTimeValue;
      }
    }

    /// <summary>
    /// The block that contains (defines) this local variable or constant.
    /// </summary>
    public BlockStatement ContainingBlock {
      get { return this.LocalDeclaration.ContainingLocalDeclarationsStatement.ContainingBlock; }
    }

    /// <summary>
    /// Custom modifiers associated with local definition.
    /// </summary>
    public virtual IEnumerable<ICustomModifier> CustomModifiers {
      get
        //^ requires this.IsModified;
      {
        return Enumerable<ICustomModifier>.Empty;
      }
    }

    /// <summary>
    /// True if this local definition is readonly and initialized with a compile time constant value.
    /// </summary>
    public bool IsConstant {
      get { return this.LocalDeclaration.IsConstant; }
    }

    /// <summary>
    /// The local definition has custom modifiers.
    /// </summary>
    public virtual bool IsModified {
      get { return false; }
    }

    /// <summary>
    /// True if the value referenced by the local must not be moved by the actions of the garbage collector.
    /// </summary>
    public bool IsPinned {
      get { return this.LocalDeclaration.IsPinned; }
    }

    /// <summary>
    /// True if the local contains a managed pointer (for example a reference to an object or reference to a field of an object).
    /// </summary>
    public bool IsReference {
      get { return this.LocalDeclaration.IsReference; }
    }

    /// <summary>
    /// The local declaration that is projected onto this definition.
    /// </summary>
    public LocalDeclaration LocalDeclaration {
      get { return this.localDeclaration; }
    }
    readonly LocalDeclaration localDeclaration;

    /// <summary>
    /// The definition of the method in which this local is defined.
    /// </summary>
    public IMethodDefinition MethodDefinition {
      get { return this.LocalDeclaration.LocalVariable.MethodDefinition; }
    }

    /// <summary>
    /// The name of the local.
    /// </summary>
    public IName Name {
      get { return this.LocalDeclaration.Name; }
    }

    /// <summary>
    /// The type of the local.
    /// </summary>
    public ITypeReference Type {
      get { return this.LocalDeclaration.Type; }
    }

    #region ILocalDefinition Members

    IMetadataConstant ILocalDefinition.CompileTimeValue {
      get {
        return this.CompileTimeValue;
      }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return IteratorHelper.GetSingletonEnumerable<ILocation>(this.LocalDeclaration.SourceLocation); }
    }

    #endregion
  }

  /// <summary>
  /// Represents matched monitor enter and exit calls, together with a try-finally to ensure that the exit call always happens.
  /// </summary>
  public class LockStatement : Statement, ILockStatement {

    /// <summary>
    /// Represents matched monitor enter and exit calls, together with a try-finally to ensure that the exit call always happens.
    /// </summary>
    /// <param name="guard">The monitor object (which gets locked when the monitor is entered and unlocked in the finally clause).</param>
    /// <param name="body">The statement to execute inside the try body after the monitor has been entered.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public LockStatement(Expression guard, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.guard = guard;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected LockStatement(BlockStatement containingBlock, LockStatement template)
      : base(template.SourceLocation) {
      this.guard = template.Guard.MakeCopyFor(containingBlock);
      this.body = template.Body.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The statement to execute inside the try body after the monitor has been entered.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(ILockStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(LockStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The monitor object (which gets locked when the monitor is entered and unlocked in the finally clause).
    /// </summary>
    public Expression Guard {
      get { return this.guard; }
    }
    readonly Expression guard;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new LockStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.guard.SetContainingExpression(containingExpression);
    }


    #region ILockStatement Members

    IStatement ILockStatement.Body {
      get { return this.Body; }
    }

    IExpression ILockStatement.Guard {
      get { return this.Guard.ProjectAsIExpression(); }
    }

    #endregion
  }

  /// <summary>
  /// A statement that arranges for a transfer of control that occurs only when an exception is encountered.
  /// </summary>
  public class OnErrorGotoStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="goto"></param>
    /// <param name="sourceLocation"></param>
    public OnErrorGotoStatement(GotoStatement @goto, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.@goto = @goto;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(OnErrorGotoStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A goto statement that is invoked if an exception occurs.
    /// </summary>
    public GotoStatement Goto {
      get { return this.@goto; }
    }
    readonly GotoStatement @goto;

  }

  /// <summary>
  /// If an exception occurs, make a note of it and resume execution at the statement following the statement that caused the exception.
  /// </summary>
  public class OnErrorResumeNextStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public OnErrorResumeNextStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(OnErrorResumeNextStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// Represents a call to a delegate bound to an event. If no delegate is present, no action is taken.
  /// </summary>
  public class RaiseEventStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventToRaise"></param>
    /// <param name="arguments"></param>
    /// <param name="sourceLocation"></param>
    public RaiseEventStatement(SimpleName eventToRaise, IEnumerable<Expression> arguments, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.eventToRaise = eventToRaise;
      this.arguments = arguments;
    }

    /// <summary>
    /// The arguments to pass to the event delegate.
    /// </summary>
    public IEnumerable<Expression> Arguments {
      get { return this.arguments; }
    }
    readonly IEnumerable<Expression> arguments;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RaiseEventStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The event to raise.
    /// </summary>
    public SimpleName EventToRaise {
      get { return this.eventToRaise; }
    }
    readonly SimpleName eventToRaise;

    /// <summary>
    /// The resulting call expression. This is derived from EventToRaise and Arguments.
    /// </summary>
    public IMethodCall MethodCall {
      get {
        //TODO: implement this
        return CodeDummy.MethodCall;
      }
    }

  }

  /// <summary>
  /// The location and new dimensions of an arrays to reallocate. Forms part of a VB ReDim statement.
  /// </summary>
  public class RedimensionClause : SourceItem {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="value"></param>
    /// <param name="sourceLocation"></param>
    public RedimensionClause(AddressableExpression target, CreateArray value, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.target = target;
      this.value = value;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RedimensionClause) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Target location where the newly allocated array will be stored (and whose existing element values may optionally be copied to the newly allocated array).
    /// </summary>
    public AddressableExpression Target {
      get { return this.target; }
    }
    readonly AddressableExpression target;

    /// <summary>
    /// An expression providing the new dimensions of the array to be reallocated.
    /// </summary>
    public CreateArray Value {
      get { return this.value; }
    }
    readonly CreateArray value;

  }

  /// <summary>
  /// Allocates new arrays and optionally copy the elements from the previous values of the target expressions.
  /// This models a VB ReDim statement.
  /// </summary>
  public class RedimensionStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="preserveExistingElementValues"></param>
    /// <param name="arrays"></param>
    /// <param name="sourceLocation"></param>
    public RedimensionStatement(bool preserveExistingElementValues, IEnumerable<RedimensionClause> arrays, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.preserveExistingElementValues = preserveExistingElementValues;
      this.arrays = arrays;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RedimensionStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The locations and new dimensions of the arrays to reallocate.
    /// </summary>
    public IEnumerable<RedimensionClause> Arrays {
      get { return this.arrays; }
    }
    readonly IEnumerable<RedimensionClause> arrays;

    /// <summary>
    /// If true, the element values must be copied from the existing arrays at the target expressions to the newly allocated arrays.
    /// </summary>
    public bool PreserveExistingElementValues {
      get { return this.preserveExistingElementValues; }
    }
    readonly bool preserveExistingElementValues;

  }

  /// <summary>
  /// Represents a statement that attaches an event-handler delegate to an event.
  /// </summary>
  public class RemoveEventHandlerStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="event"></param>
    /// <param name="handler"></param>
    /// <param name="sourceLocation"></param>
    public RemoveEventHandlerStatement(Expression @event, Expression handler, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.@event = @event;
      this.handler = handler;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RemoveEventHandlerStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The event from which to remove the event-handler delegate.
    /// </summary>
    public Expression Event {
      get { return this.@event; }
    }
    readonly Expression @event;

    /// <summary>
    /// The event-handler delegate to remove from the event
    /// </summary>
    public Expression Handler {
      get { return this.handler; }
    }
    readonly Expression handler;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.@event.SetContainingExpression(containingExpression);
      this.handler.SetContainingExpression(containingExpression);
    }

  }

  /// <summary>
  /// Represents a using statement block (of one or more IDisposable resources).
  /// </summary>
  public class ResourceUseStatement : Statement, IResourceUseStatement {

    /// <summary>
    /// Represents a using statement block (of one or more IDisposable resources).
    /// </summary>
    /// <param name="resourceAcquisitions">An expression that results in a used resource, or a local declaration that is intialized with one or more used resources.</param>
    /// <param name="body">The body of the resource use statement.</param>
    /// <param name="sourceLocation"></param>
    public ResourceUseStatement(Statement resourceAcquisitions, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation)
      //^ requires resourceAcquisitions is LocalDeclarationsStatement || resourceAcquisitions is ExpressionStatement;
    {
      this.resourceAcquisitions = resourceAcquisitions;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ResourceUseStatement(BlockStatement containingBlock, ResourceUseStatement template)
      : base(containingBlock, template) {
      this.resourceAcquisitions = template.ResourceAcquisitions.MakeCopyFor(containingBlock);
      this.body = template.Body.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The body of the resource use statement.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Creates a new dummy block that provides a scope for the acquired resources.
    /// </summary>
    /// <returns></returns>
    protected BlockStatement CreateDummyBlock() {
      List<Statement> statements = new List<Statement>(1);
      statements.Add(this.resourceAcquisitions);
      return new BlockStatement(statements, this.resourceAcquisitions.SourceLocation);
    }

    /// <summary>
    /// Calls the visitor.Visit(IResourceUseStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ResourceUseStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A dummy block that provides a scope for the acquired resources.
    /// </summary>
    protected BlockStatement DummyBlock {
      get {
        if (this.dummyBlock == null) {
          BlockStatement block = this.CreateDummyBlock();
          lock (this) {
            if (this.dummyBlock == null) {
              this.dummyBlock = block;
            }
          }
        }
        return this.dummyBlock;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ dummyBlock;

    /// <summary>
    /// An expression that results in a used resource, or a local declaration that is intialized with one or more used resources.
    /// </summary>
    public Statement ResourceAcquisitions {
      get
        //^ ensures result is LocalDeclarationsStatement || result is ExpressionStatement;
      {
        return this.resourceAcquisitions;
      }
    }
    readonly Statement resourceAcquisitions;
    //^ invariant resourceAcquisitions is LocalDeclarationsStatement || resourceAcquisitions is ExpressionStatement;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.DummyBlock.SetContainingBlock(containingBlock);
      this.body.SetContainingBlock(this.DummyBlock);
    }

    #region IResourceUseStatement Members

    IStatement IResourceUseStatement.Body {
      get { return this.Body; }
    }

    IStatement IResourceUseStatement.ResourceAcquisitions {
      get { return this.ResourceAcquisitions; }
    }

    #endregion
  }

  /// <summary>
  /// Transfers control to the statement identified by the target label.
  /// Also clears the location that records the most recent exception, thus reverting to "normal" program execution.
  /// </summary>
  public class ResumeLabeledStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetLabel"></param>
    /// <param name="sourceLocation"></param>
    public ResumeLabeledStatement(SimpleName targetLabel, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.targetLabel = targetLabel;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ResumeLabledStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The label of the statement at which to resume normal program execution.
    /// </summary>
    public SimpleName TargetLabel {
      get { return this.targetLabel; }
    }
    readonly SimpleName targetLabel;

  }

  /// <summary>
  /// Transfers control to the statement following the statement that caused the last exception.
  /// Also clears the location that records the most recent exception, thus reverting to "normal" program execution.
  /// </summary>
  public class ResumeNextStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public ResumeNextStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ResumeNextStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// Transfers control to the statement that caused the last exception.
  /// Also clears the location that records the most recent exception, thus reverting to "normal" program execution.
  /// </summary>
  public class ResumeStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public ResumeStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ResumeStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// Represents a statement that can only appear inside a catch clause or a filter clause and which rethrows the exception that caused the clause to be invoked.
  /// </summary>
  public class RethrowStatement : Statement, IRethrowStatement {

    /// <summary>
    /// Represents a statement that can only appear inside a catch clause or a filter clause and which rethrows the exception that caused the clause to be invoked.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public RethrowStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected RethrowStatement(BlockStatement containingBlock, RethrowStatement template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //TODO: error if not in a catch clause
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(IRethrowStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(RethrowStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new RethrowStatement(containingBlock, this);
    }

  }

  /// <summary>
  /// Represents a return statement.
  /// </summary>
  public class ReturnStatement : Statement, IReturnStatement {

    /// <summary>
    /// Represents a return statement.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public ReturnStatement(Expression/*?*/ expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ReturnStatement(BlockStatement containingBlock, ReturnStatement template)
      : base(containingBlock, template) {
      if (template.Expression != null)
        this.expression = template.Expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      if (this.Expression != null && this.Expression.HasErrors) return true;
      if (this.ConvertedExpression != null && this.ConvertedExpression.HasErrors) return true;
      //TODO: special error if return is in void method or in property setter.
      //TODO: check if return is nested inside a finally block
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public Expression/*?*/ ConvertedExpression {
      get {
        if (this.expression == null) return null;
        Expression/*?*/ result = this.convertedExpression;
        if (result == null) {
          ISignatureDeclaration/*?*/ containingSignature = this.ContainingBlock.ContainingSignatureDeclaration;
          if (containingSignature != null && this.Expression != null) {
            result = this.Helper.ImplicitConversionInAssignmentContext(this.Expression, containingSignature.Type.ResolvedType);
            if (result is DummyExpression)
              this.Helper.ReportFailedImplicitConversion(this.Expression, containingSignature.Type.ResolvedType);
          } else
            result = new DummyExpression(SourceDummy.SourceLocation);
          this.convertedExpression = result;
        }
        if (result is DummyExpression) return null;
        return result;
      }
    }
    Expression/*?*/ convertedExpression;

    /// <summary>
    /// Calls the visitor.Visit(IReturnStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ReturnStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The return value, if any.
    /// </summary>
    public Expression/*?*/ Expression {
      get { return this.expression; }
    }
    readonly Expression/*?*/ expression;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ReturnStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      if (this.Expression != null) {
        DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
        this.Expression.SetContainingExpression(containingExpression);
      }
    }

    #region IReturnStatement Members

    IExpression/*?*/ IReturnStatement.Expression {
      get {
        if (this.ConvertedExpression == null) return null;
        return this.ConvertedExpression.ProjectAsIExpression();
      }
    }

    #endregion
  }

  /// <summary>
  /// An executable statement.
  /// </summary>
  public abstract class Statement : CheckableSourceItem, IStatement {

    /// <summary>
    /// Initializes an executable statement.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    protected Statement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected Statement(BlockStatement containingBlock, Statement template)
      : base(template.SourceLocation) {
      this.containingBlock = containingBlock;
    }

    /// <summary>
    /// The compilation that contains this statement.
    /// </summary>
    public Compilation Compilation {
      get {
        return this.CompilationPart.Compilation;
      }
    }

    /// <summary>
    /// The compilation part that contains this statement.
    /// </summary>
    public CompilationPart CompilationPart {
      get {
        return this.ContainingBlock.CompilationPart;
      }
    }

    /// <summary>
    /// The block in which this statement is nested. If the statement is the outer most block of a method, then the containing block is itself.
    /// </summary>
    public BlockStatement ContainingBlock {
      get {
        //^ assume this.containingBlock != null;
        return this.containingBlock;
      }
    }
    //^ [SpecPublic]
    BlockStatement/*?*/ containingBlock;

    /// <summary>
    /// An instance of a language specific class containing methods that are of general utility. 
    /// </summary>
    public LanguageSpecificCompilationHelper Helper {
      get { return this.ContainingBlock.CompilationPart.Helper; }
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    public virtual Statement MakeCopyFor(BlockStatement containingBlock)
      //^ ensures result.GetType() == this.GetType();
    {
      return this; //TODO: make this abstract
    }

    /// <summary>
    /// A table used to intern strings used as names. This table is obtained from the host environment.
    /// It is mutuable, in as much as it is possible to add new names to the table.
    /// </summary>
    public INameTable NameTable {
      get { return this.Compilation.NameTable; }
    }

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    public IPlatformType PlatformType {
      get { return this.Compilation.PlatformType; }
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingBlock(BlockStatement containingBlock) {
      this.containingBlock = containingBlock;
    }

  }

  /// <summary>
  /// Causes a debugger exception to occur.
  /// </summary>
  public class StopStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceLocation"></param>
    public StopStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(StopStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// An object representing a switch case.
  /// </summary>
  public class SwitchCase : CheckableSourceItem, ISwitchCase {

    /// <summary>
    /// Allocates an object representing a switch case.
    /// </summary>
    /// <param name="expression">An expression that is expected to have a compile time constant value of the same type as the switch expression.</param>
    /// <param name="body">The statements representing this switch case.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated expression.</param>
    public SwitchCase(Expression/*?*/ expression, IEnumerable<Statement> body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
      this.body = body;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingSwitchStatement">The containing switch statement of the copied switch case. This should be different from the containing switch statement of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected SwitchCase(SwitchStatement containingSwitchStatement, SwitchCase template)
      : base(template.sourceLocation) {
      this.containingSwitchStatement = containingSwitchStatement;
      this.body = new List<Statement>(template.Body);
      if (!template.IsDefault)
        this.expression = template.Expression.MakeCopyFor(containingSwitchStatement.ContainingBlock);
    }

    /// <summary>
    /// An instance of a language specific class containing methods that are of general utility. 
    /// </summary>
    public LanguageSpecificCompilationHelper Helper {
      get { return this.ContainingSwitchStatement.Helper; }
    }


    /// <summary>
    /// The statements representing this switch case.
    /// </summary>
    public IEnumerable<Statement> Body {
      get { return this.body; }
    }
    readonly IEnumerable<Statement> body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //TODO: check that expression is a constant
      bool result = false;
      if (!this.IsDefault) {
        result |= this.Expression == null || this.Expression.HasErrors;
        result |= this.ConvertedExpression == null || this.ConvertedExpression.HasErrors;
      }
      foreach (Statement statement in this.Body)
        result |= statement.HasErrors;
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    public Expression/*?*/ ConvertedExpression {
      get {
        if (this.expression == null) return null;
        Expression/*?*/ result = this.convertedExpression;
        if (result == null) {
          if (this.containingSwitchStatement != null && this.Expression != null) {
            result = this.Helper.ImplicitConversionInAssignmentContext(this.Expression, this.containingSwitchStatement.Expression.Type.ResolvedType);
            if (result is DummyExpression)
              this.Helper.ReportFailedImplicitConversion(this.Expression, this.containingSwitchStatement.Expression.Type.ResolvedType);
          } else
            result = new DummyExpression(SourceDummy.SourceLocation);
          this.convertedExpression = result;
        }
        if (result is DummyExpression) return null;
        return result;
      }
    }
    Expression/*?*/ convertedExpression;


    /// <summary>
    /// The switch statement that branches to this switch case.
    /// </summary>
    public SwitchStatement ContainingSwitchStatement {
      get {
        //^ assume this.containingSwitchStatement != null;
        return this.containingSwitchStatement;
      }
    }
    SwitchStatement/*?*/ containingSwitchStatement;

    /// <summary>
    /// Calls the visitor.Visit(ISwitchCase) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(SwitchCase) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An expression that is expected to have a compile time constant value of the same type as the switch expression.
    /// </summary>
    public Expression Expression {
      get
        //^ requires !this.IsDefault;
      {
        //^ assume this.expression != null; //The precondition ensures this.
        return this.expression;
      }
    }
    readonly Expression/*?*/ expression;

    /// <summary>
    /// True if this case will be branched to for all values where no other case is applicable. Only of of these is legal per switch statement.
    /// </summary>
    public bool IsDefault {
      get { return this.expression == null; }
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block but leaving other properties alone until they are actually read.
    /// </summary>
    //^ [MustOverride]
    public virtual SwitchCase MakeShallowCopyFor(SwitchStatement containingSwitchStatement)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingSwitchStatement == containingSwitchStatement) return this;
      return new SwitchCase(containingSwitchStatement, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a switch case before constructing the switch statement.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingSwitchStatement(SwitchStatement containingSwitchStatement) {
      this.containingSwitchStatement = containingSwitchStatement;
      Expression/*?*/ expression = this.expression;
      if (expression != null) {
        DummyExpression containingExpression = new DummyExpression(containingSwitchStatement.ContainingBlock, SourceDummy.SourceLocation);
        expression.SetContainingExpression(containingExpression);
      }
      foreach (Statement statement in this.body) statement.SetContainingBlock(containingSwitchStatement.Block);
    }

    #region ISwitchCase Members

    IEnumerable<IStatement> ISwitchCase.Body {
      get {
        foreach (Statement statement in this.Body) {
          LocalDeclarationsStatement/*?*/ locDeclStatement = statement as LocalDeclarationsStatement;
          if (locDeclStatement == null)
            yield return statement;
          else {
            foreach (LocalDeclaration declaration in locDeclStatement.Declarations)
              yield return declaration;
          }
        }
      }
    }

    ICompileTimeConstant ISwitchCase.Expression {
      get
        //^^ requires !this.IsDefault;
      {
        Expression/*?*/ convertedExpression = this.ConvertedExpression;
        if (convertedExpression == null) return CodeDummy.Constant;
        ICompileTimeConstant/*?*/ result = convertedExpression.ProjectAsIExpression() as ICompileTimeConstant;
        if (result == null) return CodeDummy.Constant;
        return result;
      }
    }

    bool ISwitchCase.IsDefault {
      get { return this.IsDefault; }
    }

    #endregion

  }

  /// <summary>
  /// An object that represents a switch statement. Branches to one of a list of cases based on the value of a single expression.
  /// </summary>
  public class SwitchStatement : Statement, ISwitchStatement {

    /// <summary>
    /// Allocates an object representing a switch case.
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="cases">The switch cases.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public SwitchStatement(Expression expression, List<SwitchCase> cases, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
      this.cases = cases;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected SwitchStatement(BlockStatement containingBlock, SwitchStatement template)
      : base(containingBlock, template) {
      this.expression = template.Expression.MakeCopyFor(containingBlock);
      this.cases = new List<SwitchCase>(template.Cases);
    }

    /// <summary>
    /// 
    /// </summary>
    public BlockStatement Block {
      get {
        if (this.block == null) {
          List<Statement> statements = new List<Statement>(1);
          BlockStatement block = new BlockStatement(statements, this.SourceLocation);
          block.SetContainingBlock(this.ContainingBlock);
          statements.Add(this);
          this.block = block;
        }
        return this.block;
      }
    }
    //^ [Once]
    private BlockStatement/*?*/ block;

    /// <summary>
    /// The switch cases.
    /// </summary>
    public IEnumerable<SwitchCase> Cases {
      get {
        for (int i = 0, n = cases.Count; i < n; i++)
          yield return cases[i] = cases[i].MakeShallowCopyFor(this);
      }
    }
    readonly List<SwitchCase> cases;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.Expression.HasErrors;
      foreach (SwitchCase switchCase in this.Cases)
        result |= switchCase.HasErrors;
      return false;
    }

    /// <summary>
    /// The expression to evaluate in order to determine with switch case to branch to, after conversion to a switchable type.
    /// </summary>
    public Expression ConvertedExpression {
      get {
        if (this.Expression.HasErrors) return this.Expression;
        ITypeDefinition switchableType = this.GetSwitchableType(this.Expression.Type);
        if (switchableType is Dummy) return this.Expression;
        return this.Helper.ImplicitConversion(this.Expression, switchableType);
      }
    }

    /// <summary>
    /// Returns this.Expression.Type if it is a switchable type. Otherwise, if this.Expression.Type has
    /// a single implicit coercion that results in a switchable type, it returns the result type of the coercion.
    /// Otherwise Dummy.Type is returned.
    /// </summary>
    protected virtual ITypeDefinition GetSwitchableType(ITypeDefinition type) {
      if (this.Helper.IsSwitchableType(type)) return type;
      ITypeDefinition result = Dummy.Type;
      foreach (ITypeDefinitionMember member in type.GetMembersNamed(this.NameTable.OpImplicit, false)) {
        IMethodDefinition/*?*/ conversion = member as IMethodDefinition;
        if (conversion == null) continue;
        if (this.Helper.IsSwitchableType(conversion.Type.ResolvedType)) {
          if (!(result is Dummy)) return Dummy.Type;
          result = conversion.Type.ResolvedType;
        }
      }
      if (!(result is Dummy)) return result;
      foreach (ITypeReference baseTypeReference in type.BaseClasses) {
        ITypeDefinition r = this.GetSwitchableType(baseTypeReference.ResolvedType);
        if (r is Dummy) continue;
        if (!(result is Dummy)) return Dummy.Type;
        result = r;
      }
      return result;
    }

    /// <summary>
    /// Calls the visitor.Visit(ISwitchStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(SwitchStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The expression to evaluate in order to determine with switch case to branch to.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    readonly Expression expression;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new SwitchStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.expression.SetContainingExpression(containingExpression);
      foreach (SwitchCase switchCase in this.cases) switchCase.SetContainingSwitchStatement(this);
    }

    #region ISwitchStatement Members

    IEnumerable<ISwitchCase> ISwitchStatement.Cases {
      get { return IteratorHelper.GetConversionEnumerable<SwitchCase, ISwitchCase>(this.Cases); }
    }

    IExpression ISwitchStatement.Expression {
      get { return this.Expression.ProjectAsIExpression(); }
    }

    #endregion
  }

  /// <summary>
  /// Represents a statement that throws an exception.
  /// </summary>
  public class ThrowStatement : Statement, IThrowStatement {

    /// <summary>
    /// Represents a statement that throws an exception.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public ThrowStatement(Expression exception, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.exception = exception;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected ThrowStatement(BlockStatement containingBlock, ThrowStatement template)
      : base(containingBlock, template) {
      this.exception = template.Exception.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.Exception.HasErrors;
    }

    /// <summary>
    /// Calls the visitor.Visit(IThrowStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(ThrowStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The exception to throw.
    /// </summary>
    public Expression Exception {
      get { return this.exception; }
    }
    readonly Expression exception;

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new ThrowStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.exception.SetContainingExpression(containingExpression);
    }

    #region IThrowStatement Members

    IExpression IThrowStatement.Exception {
      get { return this.Exception.ProjectAsIExpression(); }
    }

    #endregion
  }

  /// <summary>
  /// Represents a try block with any number of catch clauses, any number of filter clauses and, optionally, a finally block.
  /// </summary>
  public class TryCatchFinallyStatement : Statement, ITryCatchFinallyStatement {

    /// <summary>
    /// Represents a try block with any number of catch clauses, any number of filter clauses and, optionally, a finally block.
    /// </summary>
    /// <param name="tryBody">The body of the try clause.</param>
    /// <param name="catchClauses">The catch clauses.</param>
    /// <param name="finallyBody">The body of the finally clause, if any. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public TryCatchFinallyStatement(BlockStatement tryBody, List<CatchClause> catchClauses, BlockStatement/*?*/ finallyBody, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.tryBody = tryBody;
      this.catchClauses = catchClauses;
      this.finallyBody = finallyBody;
    }

    /// <summary>
    /// Represents a try block with any number of catch clauses, any number of filter clauses and, optionally, a finally block and or a fault block.
    /// </summary>
    /// <param name="tryBody">The body of the try clause.</param>
    /// <param name="catchClauses">The catch clauses.</param>
    /// <param name="finallyBody">The body of the finally clause, if any. May be null.</param>
    /// <param name="faultBody">The body of the fault clause, if any. May be null.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public TryCatchFinallyStatement(BlockStatement tryBody, List<CatchClause> catchClauses, BlockStatement/*?*/ finallyBody, BlockStatement/*?*/ faultBody, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.tryBody = tryBody;
      this.catchClauses = catchClauses;
      this.finallyBody = finallyBody;
      this.faultBody = faultBody;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected TryCatchFinallyStatement(BlockStatement containingBlock, TryCatchFinallyStatement template)
      : base(containingBlock, template) {
      this.tryBody = (BlockStatement)template.TryBody.MakeCopyFor(containingBlock);
      this.catchClauses = new List<CatchClause>(template.CatchClauses);
      BlockStatement/*?*/ finallyBody = template.FinallyBody;
      if (finallyBody != null)
        this.finallyBody = (BlockStatement)finallyBody.MakeCopyFor(containingBlock);
      BlockStatement/*?*/ faultBody = template.FaultBody;
      if (faultBody != null)
        this.faultBody = (BlockStatement)faultBody.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// The catch clauses.
    /// </summary>
    public IEnumerable<CatchClause> CatchClauses {
      get {
        for (int i = 0, n = this.catchClauses.Count; i < n; i++)
          yield return this.catchClauses[i] = this.catchClauses[i].MakeCopyFor(this.ContainingBlock);
      }
    }
    readonly List<CatchClause> catchClauses;

    /// <summary>
    /// Calls the visitor.Visit(ITryCatchFinallyStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(TryCatchFinallyStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The body of the finally clause, if any. May be null.
    /// </summary>
    public BlockStatement/*?*/ FinallyBody {
      get { return this.finallyBody; }
    }
    readonly BlockStatement/*?*/ finallyBody;

    /// <summary>
    /// The body of the fault clause, if any. May be null.
    /// There is no C# equivalent of a fault clause. It is just like a finally clause, but is only invoked if an exception occurred.
    /// </summary>
    public BlockStatement/*?*/ FaultBody {
      get { return this.faultBody; }
    }
    readonly BlockStatement/*?*/ faultBody;

    /// <summary>
    /// The body of the try clause.
    /// </summary>
    public BlockStatement TryBody {
      get { return this.tryBody; }
    }
    readonly BlockStatement tryBody;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      foreach (CatchClause catchClause in this.catchClauses) catchClause.SetContainingBlock(containingBlock);
      if (this.finallyBody != null) this.finallyBody.SetContainingBlock(containingBlock);
      this.tryBody.SetContainingBlock(containingBlock);
    }

    #region ITryCatchFinallyStatement Members

    IEnumerable<ICatchClause> ITryCatchFinallyStatement.CatchClauses {
      get { return IteratorHelper.GetConversionEnumerable<CatchClause, ICatchClause>(this.CatchClauses); }
    }

    IBlockStatement/*?*/ ITryCatchFinallyStatement.FinallyBody {
      get { return this.FinallyBody; }
    }

    IBlockStatement/*?*/ ITryCatchFinallyStatement.FaultBody {
      get { return this.FaultBody; }
    }

    IBlockStatement ITryCatchFinallyStatement.TryBody {
      get { return this.TryBody; }
    }

    #endregion
  }

  /// <summary>
  /// until condition do statements. Tests the condition before the body. Exits when the condition is false.
  /// </summary>
  public class UntilDoStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="body"></param>
    /// <param name="sourceLocation"></param>
    public UntilDoStatement(Expression condition, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.body = body;
      this.condition = condition;
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ConvertedCondition.HasErrors;
      result |= this.Body.HasErrors;
      return result;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(UntilDoStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The condition to evaluate as false or true;
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition {
      get {
        Expression/*?*/ convertedCondition = this.convertedCondition;
        if (convertedCondition == null)
          this.convertedCondition = convertedCondition = new IsTrue(this.Condition);
        return convertedCondition;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.condition.SetContainingExpression(containingExpression);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(containingBlock);
    }

  }

  /// <summary>
  /// While condition do statements. Tests the condition before the body. Exits when the condition is true.
  /// </summary>
  public class WhileDoStatement : Statement, IWhileDoStatement {

    /// <summary>
    /// While condition do statements. Tests the condition before the body. Exits when the condition is true.
    /// </summary>
    /// <param name="condition">The condition to evaluate as false or true.</param>
    /// <param name="body">The body of the loop.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public WhileDoStatement(Expression condition, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.body = body;
      this.condition = condition;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected WhileDoStatement(BlockStatement containingBlock, WhileDoStatement template)
      : base(containingBlock, template) {
      this.body = template.Body.MakeCopyFor(containingBlock);
      this.condition = template.Condition.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The body of the loop.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ConvertedCondition.HasErrors;
      result |= this.Body.HasErrors;
      return result;
    }

    /// <summary>
    /// The condition to evaluate as false or true.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition {
      get {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Calls the visitor.Visit(IWhileDoStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(WhileDoStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new WhileDoStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      this.body.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.condition.SetContainingExpression(containingExpression);
      LoopContract/*?*/ contract = this.Compilation.ContractProvider.GetLoopContractFor(this) as LoopContract;
      if (contract != null) contract.SetContainingBlock(containingBlock);
    }

    #region IWhileDoStatement Members

    IStatement IWhileDoStatement.Body {
      get { return this.Body; }
    }

    IExpression IWhileDoStatement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    #endregion

  }

  /// <summary>
  /// Models the VB with statement.
  /// </summary>
  public class WithStatement : Statement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="implicitQualifier"></param>
    /// <param name="body"></param>
    /// <param name="sourceLocation"></param>
    public WithStatement(Expression implicitQualifier, Statement body, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.body = body;
      this.implicitQualifier = implicitQualifier;
    }

    /// <summary>
    /// The body of the with.
    /// </summary>
    public Statement Body {
      get { return this.body; }
    }
    readonly Statement body;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      //visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(WithStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The expression that supplies the implicit qualifier.
    /// </summary>
    public Expression ImplicitQualifier {
      get { return this.implicitQualifier; }
    }
    readonly Expression implicitQualifier;

  }

  /// <summary>
  /// Terminates the iteration of values produced by the iterator method containing this statement.
  /// </summary>
  public class YieldBreakStatement : Statement, IYieldBreakStatement {

    /// <summary>
    /// Terminates the iteration of values produced by the iterator method containing this statement.
    /// </summary>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated statement.</param>
    public YieldBreakStatement(ISourceLocation sourceLocation)
      : base(sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected YieldBreakStatement(BlockStatement containingBlock, YieldBreakStatement template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //TODO: check if method can be iterator
      return false;
    }

    /// <summary>
    /// Calls the visitor.Visit(IYieldBreakStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(YieldBreakStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new YieldBreakStatement(containingBlock, this);
    }

  }

  /// <summary>
  /// Yields the next value in the stream produced by the iterator method containing this statement.
  /// </summary>
  public class YieldReturnStatement : Statement, IYieldReturnStatement {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="sourceLocation"></param>
    public YieldReturnStatement(Expression expression, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.expression = expression;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied statement. This should be different from the containing block of the template statement.</param>
    /// <param name="template">The statement to copy.</param>
    protected YieldReturnStatement(BlockStatement containingBlock, YieldReturnStatement template)
      : base(containingBlock, template) {
      this.expression = template.Expression.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// Do not call this method directly, but call the HasErrors method. The latter will cache the return value.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      //TODO: check that expression return type is compatible with method return type
      //check that yield is not inside a finally block or inside a try block with catch clauses
      return this.Expression.HasErrors;
    }

    /// <summary>
    /// Calls the visitor.Visit(IYieldReturnStatement) method.
    /// </summary>
    public override void Dispatch(ICodeVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(YieldReturnStatement) method.
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Returns this.Expression after implicit conversion to this.GetIteratorElementType().
    /// </summary>
    public Expression ConvertedExpression {
      get {
        if (this.convertedExpression == null) {
          ITypeDefinition iteratorElementType = this.GetIteratorElementType();
          this.convertedExpression = this.Helper.ImplicitConversion(this.Expression, iteratorElementType);
        }
        return this.convertedExpression;
      }
    }
    Expression/*?*/ convertedExpression;

    /// <summary>
    /// The value to yield.
    /// </summary>
    public Expression Expression {
      get { return this.expression; }
    }
    readonly Expression expression;

    /// <summary>
    /// If the return type of the declaring method is IEnumerable or IEnumerator, return System.Object.
    /// If the return type is IEnumerable&lt;T&gt; or IEnumerator&lt;T&gt;, return T.
    /// Otherwise return Dummy.Type. (Note that there may not be a declaring method.)
    /// </summary>
    protected virtual ITypeDefinition GetIteratorElementType() {
      IMethodDefinition/*?*/ declaringMethod = this.ContainingBlock.ContainingMethodDefinition;
      if (declaringMethod == null) return Dummy.Type;
      ITypeDefinition returnType = declaringMethod.Type.ResolvedType;
      if (TypeHelper.TypesAreEquivalent(returnType, this.PlatformType.SystemCollectionsIEnumerable) || TypeHelper.TypesAreEquivalent(returnType, this.PlatformType.SystemCollectionsIEnumerator))
        return this.PlatformType.SystemObject.ResolvedType;
      IGenericTypeInstanceReference/*?*/ genInst = declaringMethod.Type as IGenericTypeInstanceReference;
      if (genInst == null) return Dummy.Type;
      if (TypeHelper.TypesAreEquivalent(genInst, this.PlatformType.SystemCollectionsGenericIEnumerable) || TypeHelper.TypesAreEquivalent(genInst, this.PlatformType.SystemCollectionsGenericIEnumerator)) {
        IEnumerator<ITypeReference> genArgs = genInst.GenericArguments.GetEnumerator();
        if (!genArgs.MoveNext()) return Dummy.Type;
        ITypeDefinition result = genArgs.Current.ResolvedType;
        if (genArgs.MoveNext()) return Dummy.Type;
        return result;
      }
      return Dummy.Type;
    }

    /// <summary>
    /// Makes a copy of this statement, changing the ContainingBlock to the given block.
    /// </summary>
    //^ [MustOverride]
    public override Statement MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ContainingBlock == containingBlock) return this;
      return new YieldReturnStatement(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct a Statement before constructing the containing Block.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public override void SetContainingBlock(BlockStatement containingBlock) {
      base.SetContainingBlock(containingBlock);
      DummyExpression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      this.Expression.SetContainingExpression(containingExpression);
    }

    #region IYieldReturnStatement Members

    IExpression IYieldReturnStatement.Expression {
      get { return this.ConvertedExpression.ProjectAsIExpression(); }
    }

    #endregion
  }

}
