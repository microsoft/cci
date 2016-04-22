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
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;
using System.Diagnostics;

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
    /// <param name="unit">The unit for which this is a contract provider.</param>
    public SourceContractProvider(IContractMethods contractMethods, IUnit unit)
      : base(contractMethods, unit) {
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
    public bool HasErrors {
      get {
        if (this.hasErrors == null)
          this.hasErrors = this.CheckForErrorsAndReturnTrueIfAnyAreFound();
        return this.hasErrors.Value;
      }
    }
    bool? hasErrors;

    /// <summary>
    /// Checks for errors and return true if any are found.
    /// </summary>
    protected abstract bool CheckForErrorsAndReturnTrueIfAnyAreFound();
  }

  /// <summary>
  /// A condition that must be maintained or changed during the execution of a program.
  /// </summary>
  public abstract class Ariant : CheckableSourceItem {

    /// <summary>
    /// Make a copy of template, changing the containing block
    /// </summary>
    /// <param name="containingBlock"> the new containing block</param>
    /// <param name="template">the Ariant to be copied</param>
    protected Ariant(BlockStatement containingBlock, Ariant template)
      : base(template.SourceLocation) {
      this.condition = template.condition.MakeCopyFor(containingBlock);
      this.isModel = template.IsModel;
    }

    /// <summary>
    /// Construct an ariant with the given condition and source location.
    /// </summary>
    /// <param name="condition"></param>
    /// <param name="sourceLocation"></param>
    protected Ariant(Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.condition = condition;
    }

    /// <summary>
    /// The condition that must be maintained or changed.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// An optional expression that is associated with this particular contract element. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    public IExpression/*?*/ Description {
      get { return null; }
    }

    /// <summary>
    /// An optional string that is the "string-ified" version of the condition.
    /// </summary>
    public string/*?*/ OriginalSource {
      get {
        // TODO: Store text from sourceLocation so it can be returned from here
        return null;
      }
    }

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
    //^ [Once]
    Expression/*?*/ convertedCondition;

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the condition.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.Condition.HasErrors;
      if (this.Condition.ContainingBlock.Helper.ImplicitConversionInAssignmentContext(this.Condition, this.Condition.PlatformType.SystemBoolean.ResolvedType) is DummyExpression) {
        this.Condition.ContainingBlock.Helper.ReportFailedImplicitConversion(this.Condition, this.Condition.PlatformType.SystemBoolean.ResolvedType);
        result = true;
      }
      result |= this.Condition.HasSideEffect(true);
      return result;
    }

    /// <summary>
    /// True iff any member mentioned in the Condition is a "model member", i.e.,
    /// its definition has the [ContractModel] attribute on it.
    /// </summary>
    public bool IsModel {
      get {
        return this.isModel;
      }
    }
    bool isModel;

  }

  /// <summary>
  /// A condition that must be maintained during the execution of a program.
  /// </summary>
  public abstract class Invariant : Ariant 
  {
      /// <summary>
      /// Make a copy of an Invariant, changing the containing block.
      /// </summary>
      /// <param name="containingBlock">The new containing block.</param>
      /// <param name="template">The Invariant to be copied.</param>
      protected Invariant(BlockStatement containingBlock, Invariant template) : base(containingBlock, template) 
      { 
      }

      /// <summary>
      /// Construct an invariant with the given condition and source location.
      /// </summary>
      /// <param name="condition"></param>
      /// <param name="sourceLocation"></param>
      protected Invariant(Expression condition, ISourceLocation sourceLocation) : base(condition,sourceLocation) 
      {
      }
  }

  /// <summary>
  /// A measure that must be changed during the execution of a program.
  /// </summary>
  public abstract class Variant : Ariant 
  { 
      /// <summary>
      /// Copy a Variant, changing its containing block.
      /// </summary>
      /// <param name="containingBlock">The new containing block.</param>
      /// <param name="template">The Variant to be copied.</param>
      protected Variant(BlockStatement containingBlock, Variant template) : base(containingBlock, template)
      { 
      }

      /// <summary>
      /// Construct an variant with the given measure and source location.
      /// </summary>
      /// <param name="measure"></param>
      /// <param name="sourceLocation"></param>
      protected Variant(Expression measure, ISourceLocation sourceLocation) : base(measure,sourceLocation) 
      {
      }
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
    public LoopContract(IEnumerable<LoopInvariant>/*?*/ invariants, IEnumerable<Expression>/*?*/ writes) : this(invariants, writes, null)
    {     
    }

    /// <summary>
    /// Allocates a collection of collections of objects that describe a loop.
    /// </summary>
    /// <param name="invariants">A possibly empty or null list of loop invariants.</param>
    /// <param name="writes">A possibly empty list of expressions that each represents a set of memory locations that may be written to by the body of the loop.</param>
    /// <param name="variants">A possibly empty list or null list of loop variants.</param>
    public LoopContract(IEnumerable<LoopInvariant>/*?*/ invariants, IEnumerable<Expression>/*?*/ writes, IEnumerable<Expression>/*?*/ variants)
    {
        this.invariants = invariants == null ? EmptyListOfInvariants : invariants;
        this.writes = writes;
        this.variants = variants == null ? EmptyListOfVariants : variants;
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
      if (template.writes != null)
        this.writes = new List<Expression>(template.writes);
      if (template.variants != EmptyListOfVariants)
        this.variants = new List<Expression>(template.variants);
      else
        this.variants = template.variants;
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

    /// <summary>
    /// Checks for errors and return true if any are found.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (LoopInvariant invariant in this.Invariants)
        result |= invariant.HasErrors;
      foreach (var variant in this.variants)
        result |= variant.HasErrors;
      foreach (var writes in this.writes)
        result |= writes.HasErrors;
      return result;
    }

    /// <summary>
    /// Calls visitor.Visit(ILoopInvariant).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    private static readonly IEnumerable<LoopInvariant> EmptyListOfInvariants = Enumerable<LoopInvariant>.Empty;
    private static readonly IEnumerable<Expression> EmptyListOfVariants = Enumerable<Expression>.Empty;

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
      foreach (var variant in this.variants)
        variant.SetContainingExpression(containingExpression);
    }

    private IEnumerable<IExpression> GetWrites() {
      foreach (Expression e in this.writes)
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
    readonly IEnumerable<Expression>/*?*/ writes;

      /// <summary>
    /// A possibly empty list of loop variants.
    /// </summary>
    public IEnumerable<IExpression> Variants {
      get {
        foreach (Expression e in variants)
          yield return e.ProjectAsIExpression();
      }
    }
    readonly IEnumerable<Expression> variants;

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get {
        foreach (var inv in this.invariants)
          foreach (var loc in inv.Locations) yield return loc;
        if (this.writes != null) {
          foreach (var write in this.writes)
            foreach (var loc in write.Locations) yield return loc;
        }
        foreach (var inv in this.variants)
            foreach (var loc in inv.Locations) yield return loc;
      }
    }

    #endregion
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
      : base(condition, sourceLocation) {
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

    IExpression IContractElement.Condition {
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
    /// that describes the contract between calling method and called method. This constructor omits termination checking, for backwards compatability,
    /// but should probably be depricated.
    /// </summary>
    /// <param name="allocates">A possibly empty list of expressions that each represents a set of memory locations that are newly allocated by a call to the method.</param>
    /// <param name="frees">A possibly empty list of expressions that each represents a set of memory locations that are freed by a call to the method.</param>
    /// <param name="modifiedVariables">A possibly empty or null list of target expressions (variables) that are modified by the called method.</param>
    /// <param name="postconditions">A possibly empty or null list of postconditions that are established by the called method.</param>
    /// <param name="preconditions">A possibly empty list of preconditions that must be established by the calling method.</param>
    /// <param name="reads">A possibly empty list of expressions that each represents a set of memory locations that may be read by the called method.</param>
    /// <param name="thrownExceptions">A possibly empty or null list of exceptions that may be thrown (or passed on) by the called method.</param>
    /// <param name="writes">A possibly empty list of expressions that each represents a set of memory locations that may be written to by the called method.</param>
    /// <param name="isPure">True if the method has no observable side-effect on program state and hence this method is safe to use in a contract, which may or may not be executed, depending on how the program has been compiled.</param>
    public MethodContract(IEnumerable<Expression>/*?*/ allocates, IEnumerable<Expression>/*?*/ frees, IEnumerable<AddressableExpression>/*?*/ modifiedVariables,
      IEnumerable<Postcondition>/*?*/ postconditions, IEnumerable<Precondition>/*?*/ preconditions, IEnumerable<Expression>/*?*/ reads,
      IEnumerable<ThrownException>/*?*/ thrownExceptions, IEnumerable<Expression>/*?*/ writes, bool isPure) {
      this.allocates = allocates==null? EmptyListOfExpressions:allocates;
      this.frees = frees==null? EmptyListOfExpressions:frees;
      this.modifiedVariables = modifiedVariables==null ? EmptyListOfTargetExpressions:modifiedVariables;
      this.mustInline = false;
      this.postconditions = postconditions==null ? EmptyListOfPostconditions:postconditions;
      this.preconditions = preconditions==null ? EmptyListOfPreconditions:preconditions;
      this.reads = reads==null ? EmptyListOfExpressions:reads;
      this.thrownExceptions = thrownExceptions==null ? EmptyListOfThrownExceptions:thrownExceptions;
      this.variants = EmptyListOfExpressions;
      this.writes = writes==null ? EmptyListOfExpressions:writes;
      this.isPure = isPure;
    }

    /// <summary>
    /// Allocates a collection of collections of objects that augment the type signature of a method with additional information
    /// that describes the contract between calling method and called method. This constructor omits termination checking, for backwards compatability,
    /// but should probably be depricated.
    /// </summary>
    /// <param name="allocates">A possibly empty list of expressions that each represents a set of memory locations that are newly allocated by a call to the method.</param>
    /// <param name="frees">A possibly empty list of expressions that each represents a set of memory locations that are freed by a call to the method.</param>
    /// <param name="modifiedVariables">A possibly empty or null list of target expressions (variables) that are modified by the called method.</param>
    /// <param name="postconditions">A possibly empty or null list of postconditions that are established by the called method.</param>
    /// <param name="preconditions">A possibly empty list of preconditions that must be established by the calling method.</param>
    /// <param name="reads">A possibly empty list of expressions that each represents a set of memory locations that may be read by the called method.</param>
    /// <param name="thrownExceptions">A possibly empty or null list of exceptions that may be thrown (or passed on) by the called method.</param>
    /// <param name="writes">A possibly empty list of expressions that each represents a set of memory locations that may be written to by the called method.</param>
    /// <param name="isPure">True if the method has no observable side-effect on program state and hence this method is safe to use in a contract, which may or may not be executed, depending on how the program has been compiled.</param>
    /// <param name="variants">A possibly empty list of variants, each of which is decreased in each call from the method.</param>
    public MethodContract(IEnumerable<Expression>/*?*/ allocates, IEnumerable<Expression>/*?*/ frees, IEnumerable<AddressableExpression>/*?*/ modifiedVariables,
      IEnumerable<Postcondition>/*?*/ postconditions, IEnumerable<Precondition>/*?*/ preconditions, IEnumerable<Expression>/*?*/ reads,
      IEnumerable<ThrownException>/*?*/ thrownExceptions, IEnumerable<Expression>/*?*/ writes, bool isPure, IEnumerable<Expression>/*?*/ variants)
    {
        this.allocates = allocates == null ? EmptyListOfExpressions : allocates;
        this.frees = frees == null ? EmptyListOfExpressions : frees;
        this.modifiedVariables = modifiedVariables == null ? EmptyListOfTargetExpressions : modifiedVariables;
        this.mustInline = false;
        this.postconditions = postconditions == null ? EmptyListOfPostconditions : postconditions;
        this.preconditions = preconditions == null ? EmptyListOfPreconditions : preconditions;
        this.reads = reads == null ? EmptyListOfExpressions : reads;
        this.thrownExceptions = thrownExceptions == null ? EmptyListOfThrownExceptions : thrownExceptions;
        this.variants = variants == null ? EmptyListOfExpressions : variants;
        this.writes = writes == null ? EmptyListOfExpressions : writes;
        this.isPure = isPure;
    }
      
    /// <summary>
    /// Allocates an empty method contract that indicates that the method body must be inlined.
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
      this.variants = EmptyListOfExpressions;
      this.isPure = false;
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
      this.mustInline = template.mustInline;
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
      if (template.variants != EmptyListOfExpressions)
        this.variants = new List<Expression>(template.variants);
      else
          this.variants = template.variants;
      this.isPure = template.isPure;
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

    /// <summary>
    /// The signature declaration (such as a lambda, method, property or anonymous method) that defines this contract.
    /// </summary>
    public ISignatureDeclaration/*?*/ ContainingSignatureDeclaration {
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
    /// A possibly empty list of postconditions that are established by the called method.
    /// </summary>
    public IEnumerable<IExpression> Variants
    {
      get {
        foreach (Expression variant in this.variants)
          yield return variant.ProjectAsIExpression();
      }
    }
    readonly IEnumerable<Expression> variants;
    
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

    /// <summary>
    /// True if the method has no observable side-effect on program state and hence this method is safe to use in a contract,
    /// which may or may not be executed, depending on how the program has been compiled.
    /// </summary>
    public bool IsPure {
      get { return this.isPure; }
    }

    readonly bool isPure;

    /// <summary>
    /// Checks for errors and return true if any are found.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (Precondition preCond in this.Preconditions)
        result |= preCond.HasErrors || ErrorForOutParameterReporter.CheckAndReturnTrueIfFound(preCond.Condition, this.containingBlock.CompilationPart.Helper);
      foreach (Postcondition postCond in this.Postconditions)
        result |= postCond.HasErrors;
      foreach (Expression read in this.reads) {
        result |= read.HasErrors || ErrorForOutParameterReporter.CheckAndReturnTrueIfFound(read, this.containingBlock.CompilationPart.Helper);
        if (read.ProjectAsIExpression() is ICompileTimeConstant)
          this.containingBlock.CompilationPart.Helper.ReportError(new AstErrorMessage(read, Error.ConstInReadsOrWritesClause, read.SourceLocation.Source));
      }
      foreach (Expression write in this.writes) {
        result |= write.HasErrors || ErrorForOutParameterReporter.CheckAndReturnTrueIfFound(write, this.containingBlock.CompilationPart.Helper);
        if (write.ProjectAsIExpression() is ICompileTimeConstant)
          this.containingBlock.CompilationPart.Helper.ReportError(new AstErrorMessage(write, Error.ConstInReadsOrWritesClause, write.SourceLocation.Source));
      }
      foreach(Expression variant in this.variants) {
        result |= variant.HasErrors || ErrorForOutParameterReporter.CheckAndReturnTrueIfFound(variant, this.containingBlock.CompilationPart.Helper);
      }
      foreach (Expression alloc in this.allocates)
        result |= alloc.HasErrors;
      return result;
    }

    private static readonly IEnumerable<Expression> EmptyListOfExpressions = Enumerable<Expression>.Empty;
    private static readonly IEnumerable<Precondition> EmptyListOfPreconditions = Enumerable<Precondition>.Empty;
    private static readonly IEnumerable<Postcondition> EmptyListOfPostconditions = Enumerable<Postcondition>.Empty;
    private static readonly IEnumerable<AddressableExpression> EmptyListOfTargetExpressions = Enumerable<AddressableExpression>.Empty;
    private static readonly IEnumerable<ThrownException> EmptyListOfThrownExceptions = Enumerable<ThrownException>.Empty;

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
      foreach (Expression variant in this.variants)
        variant.SetContainingExpression(containingExpression);
    }

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get {
        foreach (var a in this.allocates)
          foreach (var loc in a.Locations) yield return loc;
        foreach (var f in this.frees)
          foreach (var loc in f.Locations) yield return loc;
        foreach (var m in this.modifiedVariables)
          foreach (var loc in m.Locations) yield return loc;
        foreach (var p in this.postconditions)
          foreach (var loc in p.Locations) yield return loc;
        foreach (var p in this.preconditions)
          foreach (var loc in p.Locations) yield return loc;
        foreach (var r in this.reads)
          foreach (var loc in r.Locations) yield return loc;
        foreach (var t in this.thrownExceptions)
          foreach (var loc in t.Locations) yield return loc;
        foreach (var w in this.writes)
          foreach (var loc in w.Locations) yield return loc;
      }
    }

    #endregion

    private class ErrorForOutParameterReporter : CodeTraverser {
      private bool OutParameterFound = false;
      private readonly LanguageSpecificCompilationHelper helper;
      private readonly ISourceItem defaultSourceItemForErrorReporting;

      private ErrorForOutParameterReporter(LanguageSpecificCompilationHelper helper, ISourceItem defaultSourceItemForErrorReporting) {
        this.helper = helper;
        this.defaultSourceItemForErrorReporting = defaultSourceItemForErrorReporting;
      }

      public override void TraverseChildren(IBoundExpression boundExpression) {
        IParameterDefinition par = boundExpression.Definition as IParameterDefinition;
        if (par != null && par.IsOut) {
          ISourceItem sourceItem = boundExpression as ISourceItem;
          if (sourceItem == null) sourceItem = this.defaultSourceItemForErrorReporting;
          this.helper.ReportError(new AstErrorMessage(sourceItem, Error.OutParameterReferenceNotAllowedHere, par.Name.Value));
          OutParameterFound = true;
        }
        base.TraverseChildren(boundExpression);
      }

      internal static bool CheckAndReturnTrueIfFound(Expression expression, LanguageSpecificCompilationHelper helper) {
        ErrorForOutParameterReporter er = new ErrorForOutParameterReporter(helper, expression);
        er.Traverse((IExpression)expression);
        return er.OutParameterFound;
      }
    }
  }

  /// <summary>
  /// A condition that must be true at the start or end of a method
  /// </summary>
  public abstract class MethodContractItem : CheckableSourceItem {

    /// <summary>
    /// Allocates a condition that must be true at the start or end of a method
    /// </summary>
    /// <param name="condition">The condition that must be true at the start or end of the method that is associated with this MethodContractItem instance.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    protected MethodContractItem(Expression condition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.condition = condition;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied condition. This should be different from the containing block of the template precondition.</param>
    /// <param name="template">The statement to copy.</param>
    protected MethodContractItem(BlockStatement containingBlock, MethodContractItem template)
      : base(template.SourceLocation) {
      this.condition = template.Condition.MakeCopyFor(containingBlock);
      this.isModel = template.IsModel;
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      return this.ConvertedCondition.HasErrors || this.ConvertedCondition.HasSideEffect(true);
    }

    /// <summary>
    /// The condition that must be true at the start or end of the method that is associated with this MethodContractItem instance.
    /// </summary>
    public Expression Condition {
      get { return this.condition; }
    }
    readonly Expression condition;

    /// <summary>
    /// An optional expression that is associated with this particular contract element. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    public IExpression/*?*/ Description {
      get { return null; }
    }

    /// <summary>
    /// An optional string that is the "string-ified" version of the condition.
    /// </summary>
    public string/*?*/ OriginalSource {
      get {
        // TODO: Store text from sourceLocation so it can be returned from here
        return null;
      }
    }

    /// <summary>
    /// The condition that must be true at the start or end of the method that is associated with this MethodContractItem instance.
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
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingExpression(Expression containingExpression) {
      this.Condition.SetContainingExpression(containingExpression);
    }

    /// <summary>
    /// True iff any member mentioned in the Condition is a "model member", i.e.,
    /// its definition has the [ContractModel] attribute on it.
    /// </summary>
    public bool IsModel {
      get {
        return this.isModel;
      }
    }
    bool isModel;
  }

  /// <summary>
  /// A condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
  /// </summary>
  public class Precondition : MethodContractItem, IPrecondition {

    /// <summary>
    /// Allocates a condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
    /// </summary>
    /// <param name="condition">The condition that must be true at the start of the method that is associated with this Precondition instance.</param>
    /// <param name="exceptionToThrow">An exeption that will be thrown if Condition is not true at the start of the method that is associated with this Precondition instance.
    /// May be null. If null, the runtime behavior of the associated method is undefined when Condition is not true.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public Precondition(Expression condition, Expression/*?*/ exceptionToThrow, ISourceLocation sourceLocation)
      : base(condition, sourceLocation) {
      this.exceptionToThrow = exceptionToThrow;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied precondition. This should be different from the containing block of the template precondition.</param>
    /// <param name="template">The statement to copy.</param>
    private Precondition(BlockStatement containingBlock, Precondition template)
      : base(containingBlock, template) {
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
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = base.CheckForErrorsAndReturnTrueIfAnyAreFound();
      result |= this.ExceptionToThrow != null && this.ExceptionToThrow.HasErrors;
      return result;
    }

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
    public override void SetContainingExpression(Expression containingExpression) {
      base.SetContainingExpression(containingExpression);
      if (this.ExceptionToThrow != null)
        this.ExceptionToThrow.SetContainingExpression(containingExpression);
    }

    #region IPrecondition Members

    IExpression IContractElement.Condition {
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
  public sealed class Postcondition : MethodContractItem, IPostcondition {

    /// <summary>
    /// Allocates a condition that must be true at the end of a method.
    /// </summary>
    /// <param name="condition">The condition that must be true at the end of the method that is associated with this Postcondition instance.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public Postcondition(Expression condition, ISourceLocation sourceLocation)
      : base(condition, sourceLocation) {
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied postcondition. This should be different from the containing block of the template postcondition.</param>
    /// <param name="template">The statement to copy.</param>
    private Postcondition(BlockStatement containingBlock, Postcondition template)
      : base(containingBlock, template) {
    }

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
    /// Makes a copy of this post condition, changing the containing block to the given block.
    /// </summary>
    public Postcondition MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.Condition.ContainingBlock == containingBlock) return this;
      return new Postcondition(containingBlock, this);
    }

    #region IPostcondition Members

    IExpression IContractElement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    #endregion

  }

  /// <summary>
  /// An exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
  /// </summary>
  public class ThrownException : CheckableSourceItem, IThrownException {

    /// <summary>
    /// Allocates an exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
    /// </summary>
    /// <param name="exceptionType">The exception that can be thrown by the associated method.</param>
    /// <param name="postcondition">The postcondition that holds if the associated method throws this exception.</param>
    /// <param name="sourceLocation">The source location corresponding to the newly allocated source item.</param>
    public ThrownException(TypeExpression exceptionType, Postcondition postcondition, ISourceLocation sourceLocation)
      : base(sourceLocation) {
      this.exceptionType = exceptionType;
      this.postcondition = postcondition;
    }

    /// <summary>
    /// A copy constructor that allocates an instance that is the same as the given template, except for its containing block.
    /// </summary>
    /// <param name="containingBlock">The containing block of the copied postcondition. This should be different from the containing block of the template postcondition.</param>
    /// <param name="template">The statement to copy.</param>
    private ThrownException(BlockStatement containingBlock, ThrownException template)
      : base(template.SourceLocation) {
      this.exceptionType = (TypeExpression)template.ExceptionType.MakeCopyFor(containingBlock);
      this.postcondition = template.Postcondition.MakeCopyFor(containingBlock);
    }

    /// <summary>
    /// Performs any error checks still needed and returns true if any errors were found in the statement or a constituent part of the statement.
    /// </summary>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = this.ExceptionType.HasErrors;
      //TODO: check that ExceptionType really is an exception.
      result |= this.Postcondition.HasErrors;
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
    /// Makes a copy of this thrown exception, changing the containing block to the given block.
    /// </summary>
    public ThrownException MakeCopyFor(BlockStatement containingBlock)
      //^^ ensures result.GetType() == this.GetType();
    {
      if (this.ExceptionType.ContainingBlock == containingBlock) return this;
      return new ThrownException(containingBlock, this);
    }

    /// <summary>
    /// The postcondition that holds if the associated method throws this exception.
    /// </summary>
    public Postcondition Postcondition {
      get { return this.postcondition; }
    }
    Postcondition postcondition;

    /// <summary>
    /// Completes the two stage construction of this object. This allows bottom up parsers to construct an Expression before constructing the containing Expression.
    /// This method should be called once only and must be called before this object is made available to client code. The construction code itself should also take
    /// care not to call any other methods or property/event accessors on the object until after this method has been called.
    /// </summary>
    public virtual void SetContainingExpression(Expression containingExpression) {
      this.ExceptionType.SetContainingExpression(containingExpression);
      this.Postcondition.SetContainingExpression(containingExpression);
    }

    #region IThrownException Members

    ITypeReference IThrownException.ExceptionType {
      get { return this.ExceptionType.ResolvedType; }
    }

    IPostcondition IThrownException.Postcondition {
      get { return this.Postcondition; }
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

    /// <summary>
    /// Checks for errors and return true if any are found.
    /// </summary>
    /// <returns></returns>
    protected override bool CheckForErrorsAndReturnTrueIfAnyAreFound() {
      bool result = false;
      foreach (TypeInvariant invariant in this.invariants)
        result |= invariant.HasErrors;
      return result;
    }

    /// <summary>
    /// Calls visitor.Visit(ITypeContract).
    /// </summary>
    public virtual void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    private static readonly IEnumerable<FieldDeclaration> EmptyListOfFields = Enumerable<FieldDeclaration>.Empty;
    private static readonly IEnumerable<MethodDeclaration> EmptyListOfMethods = Enumerable<MethodDeclaration>.Empty;
    private static readonly IEnumerable<TypeInvariant> EmptyListOfInvariants = Enumerable<TypeInvariant>.Empty;

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

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get {
        foreach (var f in this.contractFields)
          foreach (var loc in f.Locations) yield return loc;
        foreach (var m in this.contractMethods)
          foreach (var loc in m.Locations) yield return loc;
        foreach (var i in this.invariants)
          foreach (var loc in i.Locations) yield return loc;
      }
    }

    #endregion
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
      : base(condition, sourceLocation) {
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

    IExpression IContractElement.Condition {
      get { return this.ConvertedCondition.ProjectAsIExpression(); }
    }

    IName/*?*/ ITypeInvariant.Name {
      get { return this.Name; }
    }

    #endregion

  }
}
