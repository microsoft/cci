//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {


  /// <summary>
  /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
  /// </summary>
  public class SourceContractProvider : ContractProvider {

    /// <summary>
    /// Allocates an object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
    /// </summary>
    /// <param name="contractMethods">A collection of methods that can be called in a way that provides tools with information about contracts.</param>
    public SourceContractProvider(IContractMethods contractMethods)
      : base(contractMethods) {
    }

    /// <summary>
    /// Associates the given object with the given list of triggers.
    /// If the object is already associated with a list of triggers, that association will be lost as a result of this call.
    /// </summary>
    /// <param name="triggers">One or more groups of expressions that trigger the instantiation of a quantifier by the theorem prover.</param>
    /// <param name="quantifier">An object to associate with the triggers. This can be any kind of object.</param>
    public void AssociateTriggersWithQuantifier(object quantifier, IEnumerable<IEnumerable<Expression>> triggers) {
      lock (this.triggersFor) {
        this.triggersFor[quantifier] = triggers;
      }
    }

    /// <summary>
    /// Returns the triggers, if any, that have been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="quantifier">An object that might have been associated with triggers. This can be any kind of object.</param>
    public new IEnumerable<IEnumerable<Expression>>/*?*/ GetTriggersFor(object quantifier) {
      lock (this.triggersFor) {
        IEnumerable<IEnumerable<Expression>>/*?*/ result;
        if (this.triggersFor.TryGetValue(quantifier, out result)) {
          return result;
        }
      }
      return null;
    }
    private readonly Dictionary<object, IEnumerable<IEnumerable<Expression>>> triggersFor = new Dictionary<object, IEnumerable<IEnumerable<Expression>>>();

    #region IContractProvider Members

    private static IEnumerable<IExpression> ProjectedExpressionList(IEnumerable<Expression> triggers) {
      foreach (Expression trigger in triggers) yield return trigger.ProjectAsIExpression();
    }

    #endregion

  }

  /// <summary>
  /// A collection of collections of objects that describe a contract for an code entity such as a loop, method or type.
  /// </summary>
  public abstract class Contract : IErrorCheckable {

    /// <summary>
    /// Checks the precondition for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors()
    {
      if (this.hasErrors == null)
        this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
      return this.hasErrors.Value;
    }
    bool? hasErrors;

    protected abstract bool CheckForErrorsAndReturnTrueIfAnyAreFound();
  }

  /// <summary>
  /// A condition that must be maintained during the execution of a program
  /// </summary>
  public abstract class Invariant : SourceItem, IErrorCheckable {

    protected Invariant(Expression condition, ISourceLocation sourceLocation) : base(sourceLocation)
    {
      this.condition = condition;
    }

    protected Invariant(BlockStatement containingBlock, Invariant template)
      :base(template.SourceLocation)
    {
      this.condition = template.condition.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The condition that must be maintained.
    /// </summary>
    public Expression Condition
    {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// IsTrue(this.Condition)
    /// </summary>
    public Expression ConvertedCondition
    {
      get
      {
        if (this.convertedCondition == null)
          this.convertedCondition = new IsTrue(this.Condition);
        return this.convertedCondition;
      }
    }
    //^ [Once]
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the condition.
    /// </summary>
    public virtual bool CheckForErrorsAndReturnTrueIfAnyAreFound()
    {
      bool result = this.Condition.HasErrors();
      if (this.Condition.ContainingBlock.Helper.ImplicitConversionInAssignmentContext(this.Condition, this.Condition.PlatformType.SystemBoolean.ResolvedType) is DummyExpression) {
        this.Condition.ContainingBlock.Helper.ReportFailedImplicitConversion(this.Condition, this.Condition.PlatformType.SystemBoolean.ResolvedType);
        result = true;
      }
      result |= this.Condition.HasSideEffect(true);
      return result;
    }

    /// <summary>
    /// Checks the precondition for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors()
    {
      if (this.hasErrors == null)
        this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
      return this.hasErrors.Value;
    }
    bool? hasErrors;
  }

  /// <summary>
  /// A collection of collections of objects that describe a loop.
  /// </summary>
  public class LoopContract : Contract, ILoopContract {
    /// <summary>
    /// Allocates a collection of collections of objects that describe a loop.
    /// </summary>
    /// <param name="invariants">A possibly empty or null list of loop invariants.</param>
    /// <param name="writes">A possibly empty list of expressions that each represents a set of memory locations that may be written to by the body of the loop.</param>
    public LoopContract(IEnumerable<LoopInvariant>/*?*/ invariants, IEnumerable<Expression>/*?*/ writes) {
      this.invariants = invariants == null ? EmptyListOfInvariants : invariants;
      this.writes = writes;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template.
    /// </summary>
    /// <param name="template">The template to copy.</param>
    private LoopContract(LoopContract template) {
      if (template.invariants != EmptyListOfInvariants)
        this.invariants = new List<LoopInvariant>(template.invariants);
      else
        this.invariants = template.invariants;
    }

    /// <summary>
    /// The block statement that contains the method contract. Usually this is the dummy block of a method.
    /// </summary>
    BlockStatement/*?*/ containingBlock;

    /// <summary>
    /// A possibly empty list of loop invariants.
    /// </summary>
    public IEnumerable<ILoopInvariant> Invariants {
      get { return IteratorHelper.GetConversionEnumerable<LoopInvariant, ILoopInvariant>(this.invariants); }
    }
    readonly IEnumerable<LoopInvariant> invariants;

    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound()
    {
      bool result = false;
      foreach (LoopInvariant invariant in this.Invariants)
        result |= invariant.HasErrors();
      return result;
    }

    /// <summary>
    /// Calls visitor.Visit(ILoopInvariant).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    private static readonly IEnumerable<LoopInvariant> EmptyListOfInvariants = IteratorHelper.GetEmptyEnumerable<LoopInvariant>();

    /// <summary>
    /// Makes a copy of this contract, changing the containing block to the given block.
    /// </summary>
    public LoopContract MakeCopyFor(BlockStatement containingBlock) {
      if (this.containingBlock == containingBlock) return this;
      LoopContract result = new LoopContract(this);
      result.SetContainingBlock(containingBlock);
      return result;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingBlock(BlockStatement containingBlock) {
      this.containingBlock = containingBlock;
      Expression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      foreach (LoopInvariant loopInvariant in this.invariants)
        loopInvariant.SetContainingExpression(containingExpression);
      foreach (Expression writes in this.writes)
        writes.SetContainingExpression(containingExpression);
    }

    private IEnumerable<IExpression> GetWrites() {
      foreach (Expression e in writes)
        yield return e.ProjectAsIExpression();
    }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the body of the loop.
    /// </summary>
    public IEnumerable<IExpression>/*?*/ Writes {
      get {
        if (writes == null) return null;
        else return this.GetWrites();
      }
    }
    readonly IEnumerable<Expression> writes;
  }

  /// <summary>
  /// A condition that must be true at the start of every iteration of a loop.
  /// </summary>
  public class LoopInvariant : Invariant, ILoopInvariant {
    /// <summary>
    /// Allocates a condition that must be true at the start of every iteration of a loop.
    /// </summary>
    /// <param name="condition">The condition that must be true at the start of every iteration of a loop.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public LoopInvariant(Expression condition, ISourceLocation sourceLocation) 
      : base(condition, sourceLocation)
    {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied precondition. This should be different from the containing block of the template precondition.</param>
    /// <param name="template">The statement to copy.</param>
    private LoopInvariant(BlockStatement containingBlock, LoopInvariant template)
      : base(containingBlock, template) {
    }

    /// <summary>
    /// Calls visitor.Visit(ILoopInvariant).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(LoopInvariant).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Makes a copy of this invariant, changing the Condition.ContainingBlock to the given block.
    /// </summary>
    public LoopInvariant MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Condition.ContainingBlock == containingBlock) return this;
      return new LoopInvariant(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public void SetContainingExpression(Expression containingExpression) {
      this.Condition.SetContainingExpression(containingExpression);
    }

    #region ILoopInvariant Members

    IExpression ILoopInvariant.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    #endregion

  }

  /// <summary>
  /// A collection of collections of objects that augment the type signature of a method with additional information
  /// that describes the contract between calling method and called method.
  /// </summary>
  public class MethodContract : Contract, IMethodContract {

    /// <summary>
    /// Allocates a collection of collections of objects that augment the type signature of a method with additional information
    /// that describes the contract between calling method and called method.
    /// </summary>
    /// <param name="allocates">A possibly empty list of expressions that each represents a set of memory locations that are newly allocated by a call to the method.</param>
    /// <param name="frees">A possibly empty list of expressions that each represents a set of memory locations that are freed by a call to the method.</param>
    /// <param name="modifiedVariables">A possibly empty or null list of target expressions (variables) that are modified by the called method.</param>
    /// <param name="postconditions">A possibly empty or null list of postconditions that are established by the called method.</param>
    /// <param name="preconditions">A possibly empty list of preconditions that must be established by the calling method.</param>
    /// <param name="reads">A possibly empty list of expressions that each represents a set of memory locations that may be read by the called method.</param>
    /// <param name="thrownExceptions">A possibly empty or null list of exceptions that may be thrown (or passed on) by the called method.</param>
    /// <param name="writes">A possibly empty list of expressions that each represents a set of memory locations that may be written to by the called method.</param>
    public MethodContract(IEnumerable<Expression>/*?*/ allocates, IEnumerable<Expression>/*?*/ frees, IEnumerable<AddressableExpression>/*?*/ modifiedVariables,
      IEnumerable<Postcondition>/*?*/ postconditions, IEnumerable<Precondition>/*?*/ preconditions, IEnumerable<Expression>/*?*/ reads,
      IEnumerable<ThrownException>/*?*/ thrownExceptions, IEnumerable<Expression>/*?*/ writes) {
      this.allocates = allocates==null? EmptyListOfExpressions:allocates;
      this.frees = frees==null? EmptyListOfExpressions:frees;
      this.modifiedVariables = modifiedVariables==null ? EmptyListOfTargetExpressions:modifiedVariables;
      this.mustInline = false;
      this.postconditions = postconditions==null ? EmptyListOfPostconditions:postconditions;
      this.preconditions = preconditions==null ? EmptyListOfPreconditions:preconditions;
      this.reads = reads==null ? EmptyListOfExpressions:reads;
      this.thrownExceptions = thrownExceptions==null ? EmptyListOfThrownExceptions:thrownExceptions;
      this.writes = writes==null ? EmptyListOfExpressions:writes;
    }

    /// <summary>
    /// Allocates a method contract that indicates that the method body must be inlined.
    /// </summary>
    public MethodContract() {
      this.allocates = EmptyListOfExpressions;
      this.frees = EmptyListOfExpressions;
      this.modifiedVariables = EmptyListOfTargetExpressions;
      this.mustInline = true;
      this.postconditions = EmptyListOfPostconditions;
      this.preconditions = EmptyListOfPreconditions;
      this.reads = EmptyListOfExpressions;
      this.thrownExceptions = EmptyListOfThrownExceptions;
      this.writes = EmptyListOfExpressions;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template.
    /// </summary>
    /// <param name="template">The template to copy.</param>
    protected MethodContract(MethodContract template) {
      if (template.allocates != EmptyListOfExpressions)
        this.allocates = new List<Expression>(template.allocates);
      else
        this.allocates = template.allocates;
      if (template.frees != EmptyListOfExpressions)
        this.frees = new List<Expression>(template.frees);
      else
        this.frees = template.frees;
      if (template.modifiedVariables != EmptyListOfTargetExpressions)
        this.modifiedVariables = new List<AddressableExpression>(template.modifiedVariables);
      else
        this.modifiedVariables = template.modifiedVariables;
      if (template.postconditions != EmptyListOfPostconditions)
        this.postconditions = new List<Postcondition>(template.postconditions);
      else
        this.postconditions = template.postconditions;
      if (template.preconditions != EmptyListOfPreconditions)
        this.preconditions = new List<Precondition>(template.preconditions);
      else
        this.preconditions = template.preconditions;
      if (template.reads != EmptyListOfExpressions)
        this.reads = new List<Expression>(template.reads);
      else
        this.reads = template.reads;
      if (template.thrownExceptions != EmptyListOfThrownExceptions)
        this.thrownExceptions = new List<ThrownException>(template.thrownExceptions);
      else
        this.thrownExceptions = template.thrownExceptions;
      if (template.writes != EmptyListOfExpressions)
        this.writes = new List<Expression>(template.writes);
      else
        this.writes = template.writes;
    }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that are newly allocated by a call to the method.
    /// </summary>
    public IEnumerable<IExpression> Allocates {
      get {
        foreach (Expression expr in this.allocates) 
          yield return expr.ProjectAsIExpression();
      }
    }
    readonly IEnumerable<Expression> allocates;

    public ISignatureDeclaration/*?*/ ContainingSignatureDeclaration
    {
      get { 
        return this.containingBlock != null ? this.containingBlock.ContainingSignatureDeclaration : null;
      }
    }

    /// <summary>
    /// The block statement that contains the method contract. Usually this is the dummy block of a method.
    /// </summary>
    BlockStatement/*?*/ containingBlock;

    /// <summary>
    /// Calls visitor.Visit(IMethodContract).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that are freed by a call to the method.
    /// </summary>
    public IEnumerable<IExpression> Frees {
      get {
        foreach (Expression expr in this.frees) 
          yield return expr.ProjectAsIExpression();
      }
    }
    readonly IEnumerable<Expression> frees;

    /// <summary>
    /// A possibly empty list of addressable expressions (variables) that are modified by the called method.
    /// </summary>
    public IEnumerable<IAddressableExpression> ModifiedVariables {
      get {
        foreach (AddressableExpression expr in this.modifiedVariables)
          yield return expr;
      }
    }
    readonly IEnumerable<AddressableExpression> modifiedVariables;

    /// <summary>
    /// The method body constitutes its contract. Callers must substitute the body in line with the call site.
    /// </summary>
    public bool MustInline {
      get { return this.mustInline; }
    }
    readonly bool mustInline;

    /// <summary>
    /// A possibly empty list of postconditions that are established by the called method.
    /// </summary>
    public IEnumerable<IPostcondition> Postconditions {
      get {
        foreach (Postcondition postcondition in this.postconditions)
          yield return postcondition;
      }
    }
    readonly IEnumerable<Postcondition> postconditions;

    /// <summary>
    /// A possibly empty list of preconditions that must be established by the calling method.
    /// </summary>
    public IEnumerable<IPrecondition> Preconditions {
      get {
        foreach (Precondition precondition in this.preconditions)
          yield return precondition;
      }
    }
    readonly IEnumerable<Precondition> preconditions;

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be read by the called method.
    /// </summary>
    public IEnumerable<IExpression> Reads {
      get {
        foreach (Expression expr in this.reads) 
          yield return expr.ProjectAsIExpression();
      }
    }
    readonly IEnumerable<Expression> reads;

    /// <summary>
    /// A possibly empty list of exceptions that may be thrown (or passed on) by the called method.
    /// </summary>
    public IEnumerable<IThrownException> ThrownExceptions {
      get {
        foreach (ThrownException thrownException in this.thrownExceptions)
          yield return thrownException;
      } 
    }
    readonly IEnumerable<ThrownException> thrownExceptions;

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the called method.
    /// </summary>
    public IEnumerable<IExpression> Writes {
      get {
        foreach (Expression expr in this.writes) 
          yield return expr.ProjectAsIExpression();
      }
    }
    readonly IEnumerable<Expression> writes;


    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound()
    {
      bool result = false;
      foreach (Precondition preCond in this.Preconditions)
        result |= preCond.HasErrors();
      foreach (Postcondition postCond in this.Postconditions)
        result |= postCond.HasErrors();
      foreach (Expression read in this.reads) {
        result |= read.HasErrors();
        if (read.ProjectAsIExpression() is ICompileTimeConstant)
          this.containingBlock.CompilationPart.Helper.ReportError(new AstErrorMessage(read, Error.ConstInReadsOrWritesClause, read.SourceLocation.Source));
      }
      foreach (Expression write in this.writes) {
        result |= write.HasErrors();
        if (write.ProjectAsIExpression() is ICompileTimeConstant)
          this.containingBlock.CompilationPart.Helper.ReportError(new AstErrorMessage(write, Error.ConstInReadsOrWritesClause, write.SourceLocation.Source));
      }
      foreach (Expression alloc in this.allocates)
        result |= alloc.HasErrors();
      return result;
    }

    private static readonly IEnumerable<Expression> EmptyListOfExpressions = IteratorHelper.GetEmptyEnumerable<Expression>();
    private static readonly IEnumerable<Precondition> EmptyListOfPreconditions = IteratorHelper.GetEmptyEnumerable<Precondition>();
    private static readonly IEnumerable<Postcondition> EmptyListOfPostconditions = IteratorHelper.GetEmptyEnumerable<Postcondition>();
    private static readonly IEnumerable<AddressableExpression> EmptyListOfTargetExpressions = IteratorHelper.GetEmptyEnumerable<AddressableExpression>();
    private static readonly IEnumerable<ThrownException> EmptyListOfThrownExceptions = IteratorHelper.GetEmptyEnumerable<ThrownException>();

    /// <summary>
    /// Makes a copy of this contract, changing the containing block to the given block.
    /// </summary>
    //^ [MustOverride]
    public virtual MethodContract MakeCopyFor(BlockStatement containingBlock) {
      if (this.containingBlock == containingBlock) return this;
      MethodContract result = new MethodContract(this);
      result.SetContainingBlock(containingBlock);
      return result;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingBlock(BlockStatement containingBlock) {
      this.containingBlock = containingBlock;
      Expression containingExpression = new DummyExpression(containingBlock, SourceDummy.SourceLocation);
      foreach (Expression allocation in this.allocates)
        allocation.SetContainingExpression(containingExpression);
      foreach (Expression free in this.frees)
        free.SetContainingExpression(containingExpression);
      foreach (AddressableExpression tgtExpr in this.modifiedVariables)
        tgtExpr.SetContainingExpression(containingExpression);
      foreach (Postcondition postCond in this.postconditions)
        postCond.SetContainingExpression(containingExpression);
      foreach (Precondition preCond in this.preconditions)
        preCond.SetContainingExpression(containingExpression);
      foreach (Expression read in this.reads)
        read.SetContainingExpression(containingExpression);
      foreach (ThrownException except in this.thrownExceptions)
        except.SetContainingExpression(containingExpression);
      foreach (Expression write in this.writes)
        write.SetContainingExpression(containingExpression);
    }

  }

  /// <summary>
  /// A condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
  /// </summary>
  public class Precondition : SourceItem, IPrecondition {

    /// <summary>
    /// Allocates a condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
    /// </summary>
    /// <param name="condition">The condition that must be true at the start of the method that is associated with this Precondition instance.</param>
    /// <param name="exceptionToThrow">An exeption that will be thrown if Condition is not true at the start of the method that is associated with this Precondition instance.
    /// May be null. If null, the runtime behavior of the associated method is undefined when Condition is not true.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public Precondition(Expression condition, Expression/*?*/ exceptionToThrow, ISourceLocation sourceLocation) 
      : base(sourceLocation)
    {
      this.condition = condition;
      this.exceptionToThrow = exceptionToThrow;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied precondition. This should be different from the containing block of the template precondition.</param>
    /// <param name="template">The statement to copy.</param>
    private Precondition(BlockStatement containingBlock, Precondition template)
      : base(template.SourceLocation) {
      this.condition = template.Condition.MakeCopyFor(containingBlock);
      if (template.ExceptionToThrow != null)
        this.exceptionToThrow = template.ExceptionToThrow.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// The precondition is always checked at runtime, even in release builds.
    /// </summary>
    public virtual bool AlwaysCheckedAtRuntime {
      get { return false; }
    }
    
    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    private bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.ConvertedCondition.HasErrors() || this.ConvertedCondition.HasSideEffect(true) || this.ExceptionToThrow != null && this.ExceptionToThrow.HasErrors();
    }

    /// <summary>
    /// The condition that must be true at the start of the method that is associated with this Precondition instance.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

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
    /// Calls visitor.Visit(IPrecondition).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(Precondition).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An exeption that will be thrown if Condition is not true at the start of the method that is associated with this Precondition instance.
    /// May be null. If null, the runtime behavior of the associated method is undefined when Condition is not true.
    /// </summary>
    public Expression/*?*/ ExceptionToThrow {
      get { return this.exceptionToThrow; }
    }
    readonly Expression/*?*/ exceptionToThrow;

    /// <summary>
    /// Checks the precondition for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() {
      if (this.hasErrors == null)
        this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound(); 
      return this.hasErrors.Value;
    }  
    bool? hasErrors;

    /// <summary>
    /// Makes a copy of this precondition, changing the containing block to the given block.
    /// </summary>
    public Precondition MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Condition.ContainingBlock == containingBlock) return this;
      return new Precondition(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public void SetContainingExpression(Expression containingExpression) {
      this.Condition.SetContainingExpression(containingExpression);
      if (this.ExceptionToThrow != null)
        this.ExceptionToThrow.SetContainingExpression(containingExpression);
    }

    #region IPrecondition Members

    IExpression IPrecondition.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IExpression/*?*/ IPrecondition.ExceptionToThrow {
      get {
        if (this.ExceptionToThrow == null) return null;
        return this.ExceptionToThrow.ProjectAsIExpression();
      }
    }

    #endregion

  }

  /// <summary>
  /// A condition that must be true at the end of a method.
  /// </summary>
  public sealed class Postcondition : SourceItem, IPostcondition {

    /// <summary>
    /// Allocates a condition that must be true at the end of a method.
    /// </summary>
    /// <param name="condition">The condition that must be true at the end of the method that is associated with this Postcondition instance.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public Postcondition(Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) 
    {
      this.condition = condition;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied postcondition. This should be different from the containing block of the template postcondition.</param>
    /// <param name="template">The statement to copy.</param>
    private Postcondition(BlockStatement containingBlock, Postcondition template)
      : base(template.SourceLocation) {
      this.condition = template.Condition.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    private bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.ConvertedCondition.HasErrors() || this.ConvertedCondition.HasSideEffect(true);
    }

    /// <summary>
    /// The condition that must be true at the end of the method that is associated with this Postcondition instance.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

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
    /// Calls visitor.Visit(IPostcondition).
    /// </summary>
    public void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(Postcondition).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Checks the postcondition for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() {
      if (this.hasErrors == null)
        this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
      return this.hasErrors.Value;
    }
    bool? hasErrors;

    /// <summary>
    /// Makes a copy of this post condition, changing the containing block to the given block.
    /// </summary>
    public Postcondition MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Condition.ContainingBlock == containingBlock) return this;
      return new Postcondition(containingBlock, this);
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public void SetContainingExpression(Expression containingExpression) {
      this.Condition.SetContainingExpression(containingExpression);
    }

    #region IPostcondition Members

    IExpression IPostcondition.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    #endregion

  }

  /// <summary>
  /// An exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
  /// </summary>
  public class ThrownException : SourceItem, IThrownException {

    /// <summary>
    /// Allocates an exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
    /// </summary>
    /// <param name="exceptionType">The exception that can be thrown by the associated method.</param>
    /// <param name="postconditions">The postconditions that hold if the associated method throws this exception.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public ThrownException(TypeExpression exceptionType, IList<Postcondition> postconditions, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.exceptionType = exceptionType;
      this.postconditions = postconditions;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied postcondition. This should be different from the containing block of the template postcondition.</param>
    /// <param name="template">The statement to copy.</param>
    private ThrownException(BlockStatement containingBlock, ThrownException template)
      : base(template.SourceLocation) {
      this.exceptionType = (TypeExpression)template.ExceptionType.MakeCopyFor(containingBlock);
      this.postconditions = new List<Postcondition>(template.Postconditions);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected virtual bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ExceptionType.HasErrors();
      //TODO: check that ExceptionType really is an exception.
      foreach (Postcondition postcondition in this.Postconditions)
        result |= postcondition.HasErrors();
      return result;
    }

    /// <summary>
    /// Calls visitor.Visit(IThrownException).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(ThrownException).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The exception that can be thrown by the associated method.
    /// </summary>
    public TypeExpression ExceptionType {
      get { return this.exceptionType; }
    }
    readonly TypeExpression exceptionType;

    /// <summary>
    /// Checks the precondition for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() {
      if (this.hasErrors == null)
        this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
      return this.hasErrors.Value;
    }
    bool? hasErrors;

    /// <summary>
    /// Makes a copy of this thrown exception, changing the containing block to the given block.
    /// </summary>
    public ThrownException MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ExceptionType.ContainingBlock == containingBlock) return this;
      return new ThrownException(containingBlock, this);
    }

    /// <summary>
    /// The postconditions that hold if the associated method throws this exception.
    /// </summary>
    public IEnumerable<Postcondition> Postconditions {
      get {
        for (int i = 0, n = this.postconditions.Count; i < n; i++)
          yield return this.postconditions[i] = this.postconditions[i].MakeCopyFor(this.ExceptionType.ContainingBlock);
      }
    }
    readonly IList<Postcondition> postconditions;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingExpression(Expression containingExpression) {
      this.ExceptionType.SetContainingExpression(containingExpression);
      foreach (Postcondition postCond in this.Postconditions)
        postCond.SetContainingExpression(containingExpression);
    }

    #region IThrownException Members

    ITypeReference IThrownException.ExceptionType {
      get { return this.ExceptionType.ResolvedType; }
    }

    IEnumerable<IPostcondition> IThrownException.Postconditions {
      get { return IteratorHelper.GetConversionEnumerable<Postcondition, IPostcondition>(this.Postconditions); }
    }

    #endregion

  }

  /// <summary>
  /// A collection of collections of objects that augment the signature of a type with additional information
  /// that describe invariants, model variables and functions, as well as axioms.
  /// </summary>
  public class TypeContract : Contract, ITypeContract {

    /// <summary>
    /// Allocates a collection of collections of objects that augment the signature of a type with additional information
    /// that describe invariants, model variables and functions, as well as axioms.
    /// </summary>
    /// <param name="contractFields">A possibly empty list of contract fields. Contract fields can only be used inside contracts and are not available at runtime.</param>
    /// <param name="contractMethods">A possibly empty list of contract methods. Contract methods have no bodies and can only be used inside contracts. The meaning of a contract
    /// method is specified by the axioms (assumed invariants) of the associated type. Contract methods are not available at runtime.</param>
    /// <param name="invariants">A possibly empty list of type invariants. Axioms are a special type of invariant.</param>
    public TypeContract(IEnumerable<FieldDeclaration>/*?*/ contractFields, IEnumerable<MethodDeclaration>/*?*/ contractMethods, IEnumerable<TypeInvariant>/*?*/ invariants) {
      this.contractFields = contractFields==null ? EmptyListOfFields:contractFields;
      this.contractMethods = contractMethods==null ? EmptyListOfMethods:contractMethods;
      this.invariants = invariants==null ? EmptyListOfInvariants:invariants;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template.
    /// </summary>
    /// <param name="template">The template to copy.</param>
    private TypeContract(TypeContract template) {
      if (template.contractFields != EmptyListOfFields)
        this.contractFields = new List<FieldDeclaration>(template.contractFields);
      else
        this.contractFields = template.contractFields;
      if (template.contractMethods != EmptyListOfMethods)
        this.contractMethods = new List<MethodDeclaration>(template.contractMethods);
      else
        this.contractMethods = template.contractMethods;
      if (template.invariants != EmptyListOfInvariants)
        this.invariants = new List<TypeInvariant>(template.invariants);
      else
        this.invariants = template.invariants;
    }

    /// <summary>
    /// The type declaration that contains the type contract.
    /// </summary>
    TypeDeclaration/*?*/ containingType;

    /// <summary>
    /// A possibly empty list of contract fields. Contract fields can only be used inside contracts and are not available at runtime.
    /// </summary>
    public IEnumerable<IFieldDefinition> ContractFields {
      get {
        foreach (FieldDeclaration fieldDecl in this.contractFields) yield return fieldDecl.FieldDefinition;
      }
    }
    readonly IEnumerable<FieldDeclaration> contractFields;

    /// <summary>
    /// A possibly empty list of contract methods. Contract methods have no bodies and can only be used inside contracts. The meaning of a contract
    /// method is specified by the axioms (assumed invariants) of the associated type. Contract methods are not available at runtime.
    /// </summary>
    public IEnumerable<IMethodDefinition> ContractMethods {
      get {
        foreach (MethodDeclaration methDecl in this.contractMethods) 
          yield return methDecl.MethodDefinition;
      }
    }
    readonly IEnumerable<MethodDeclaration> contractMethods;

    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound()
    {
      bool result = false;
      foreach (TypeInvariant invariant in this.invariants)
        result |= invariant.HasErrors();
      return result;
    }

    /// <summary>
    /// Calls visitor.Visit(ITypeContract).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    private static readonly IEnumerable<FieldDeclaration> EmptyListOfFields = IteratorHelper.GetEmptyEnumerable<FieldDeclaration>();
    private static readonly IEnumerable<MethodDeclaration> EmptyListOfMethods = IteratorHelper.GetEmptyEnumerable<MethodDeclaration>();
    private static readonly IEnumerable<TypeInvariant> EmptyListOfInvariants = IteratorHelper.GetEmptyEnumerable<TypeInvariant>();

    /// <summary>
    /// A possibly empty list of type invariants. Axioms are a special type of invariant.
    /// </summary>
    public IEnumerable<ITypeInvariant> Invariants {
      get { return IteratorHelper.GetConversionEnumerable<TypeInvariant, ITypeInvariant>(this.invariants); }
    }
    readonly IEnumerable<TypeInvariant> invariants;

    /// <summary>
    /// Makes a copy of this contract, changing the containing block to the given block.
    /// </summary>
    public TypeContract MakeCopyFor(TypeDeclaration containingType) {
      if (this.containingType == containingType) return this;
      TypeContract result = new TypeContract(this);
      result.SetContainingType(containingType);
      return result;
    }

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingType(TypeDeclaration containingType) {
      this.containingType = containingType;
      Expression containingExpression = new DummyExpression(containingType.DummyBlock, SourceDummy.SourceLocation);
      foreach (FieldDeclaration contractField in this.contractFields)
        contractField.SetContainingTypeDeclaration(containingType, true);
      foreach (MethodDeclaration contractMethod in this.contractMethods)
        contractMethod.SetContainingTypeDeclaration(containingType, true);
      foreach (TypeInvariant typeInvariant in this.invariants)
        typeInvariant.SetContainingExpression(containingExpression);
    }

  }

  /// <summary>
  /// A condition that must be true after an object has been constructed and that is by default a part of the precondition and postcondition of every public method of the associated type.
  /// </summary>
  public class TypeInvariant : Invariant, ITypeInvariant {

    /// <summary>
    /// Allocates a condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
    /// </summary>
    /// <param name="name">The name of the axiom. Used in error diagnostics. May be null.</param>
    /// <param name="condition">The condition that must be true at the start of the method that is associated with this Precondition instance.</param>
    /// <param name="isAxiom">An axiom is a type invariant whose truth is assumed rather than derived. Commonly used to make statements about the meaning of contract methods.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public TypeInvariant(NameDeclaration/*?*/ name, Expression condition, bool isAxiom, ISourceLocation sourceLocation) 
      : base(condition, sourceLocation)
    {
      this.name = name;
      this.isAxiom = isAxiom;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied type invariant. This should be different from the containing block of the template.</param>
    /// <param name="template">The statement to copy.</param>
    protected TypeInvariant(BlockStatement containingBlock, TypeInvariant template)
      : base(containingBlock, template) {
      if (template.Name != null)
        this.name = template.Name.MakeCopyFor(containingBlock.Compilation);
      this.isAxiom = template.isAxiom;
    }

    /// <summary>
    /// Calls visitor.Visit(ITypeInvariant).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.Visit(TypeInvariant).
    /// </summary>
    public override void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An axiom is a type invariant whose truth is assumed rather than derived. Commonly used to make statements about the meaning of contract methods.
    /// </summary>
    public bool IsAxiom {
      get { return this.isAxiom; } 
    }
    readonly bool isAxiom;

    /// <summary>
    /// Makes a copy of this type invariant, changing the containing block to the given block.
    /// </summary>
    //^ [MustOverride]
    public virtual TypeInvariant MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Condition.ContainingBlock == containingBlock) return this;
      return new TypeInvariant(containingBlock, this);
    }

    /// <summary>
    /// The name of the axiom. Used in error diagnostics. May be null.
    /// </summary>
    public NameDeclaration/*?*/ Name {
      get { return this.name; }
    }
    NameDeclaration/*?*/ name;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public void SetContainingExpression(Expression containingExpression) {
      this.Condition.SetContainingExpression(containingExpression);
    }

    #region ITypeInvariant Members

    IExpression ITypeInvariant.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IName/*?*/ ITypeInvariant.Name {
      get { return this.Name; }
    }

    #endregion

  }
}
