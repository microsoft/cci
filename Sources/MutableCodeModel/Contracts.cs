//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Contracts {

  /// <summary>
  /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
  /// </summary>
  public class ContractProvider : IContractProvider {

    /// <summary>
    /// Allocates an object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
    /// If the object is already associated with a contract, that association will be lost as a result of this call.
    /// </summary>
    /// <param name="contractMethods">A collection of methods that can be called in a way that provides tools with information about contracts.</param>
    public ContractProvider(IContractMethods contractMethods) {
      this.contractMethods = contractMethods;
    }

    /// <summary>
    /// Associates the given object with the given loop contract.
    /// If the object is already associated with a loop contract, that association will be lost as a result of this call.
    /// </summary>
    /// <param name="contract">The contract to associate with loop.</param>
    /// <param name="loop">An object to associate with the loop contract. This can be any kind of object.</param>
    public void AssociateLoopWithContract(object loop, ILoopContract contract) {
      lock (this.methodContractFor) {
        this.loopContractFor[loop] = contract;
      }
    }

    /// <summary>
    /// Associates the given object with the given method contract.
    /// If the object is already associated with a method contract, that association will be lost as a result of this call.
    /// </summary>
    /// <param name="contract">The contract to associate with method.</param>
    /// <param name="method">An object to associate with the method contract. This can be any kind of object.</param>
    public void AssociateMethodWithContract(object method, IMethodContract contract) {
      lock (this.methodContractFor) {
        this.methodContractFor[method] = contract;
      }
    }

    /// <summary>
    /// Associates the given object with the given list of triggers.
    /// If the object is already associated with a list of triggers, that association will be lost as a result of this call.
    /// </summary>
    /// <param name="triggers">One or more groups of expressions that trigger the instantiation of a quantifier by the theorem prover.</param>
    /// <param name="quantifier">An object to associate with the triggers. This can be any kind of object.</param>
    public void AssociateTriggersWithQuantifier(object quantifier, IEnumerable<IEnumerable<IExpression>> triggers) {
      lock (this.triggersFor) {
        this.triggersFor[quantifier] = triggers;
      }
    }

    /// <summary>
    /// Associates the given object with the given type contract.
    /// If the object is already associated with a type contract, that association will be lost as a result of this call.
    /// </summary>
    /// <param name="contract">The contract to associate with type.</param>
    /// <param name="type">An object to associate with the type contract. This can be any kind of object.</param>
    public void AssociateTypeWithContract(object type, ITypeContract contract) {
      lock (this.typeContractFor) {
        this.typeContractFor[type] = contract;
      }
    }

    /// <summary>
    /// A collection of methods that can be called in a way that provides tools with information about contracts.
    /// </summary>
    public IContractMethods ContractMethods {
      get { return this.contractMethods; }
    }
    IContractMethods contractMethods;

    /// <summary>
    /// Returns the loop contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="loop">An object that might have been associated with a loop contract. This can be any kind of object.</param>
    public ILoopContract/*?*/ GetLoopContractFor(object loop) {
      lock (this.loopContractFor) {
        ILoopContract/*?*/ result;
        if (this.loopContractFor.TryGetValue(loop, out result)) {
          return result;
        }
      }
      return null;
    }
    private readonly Dictionary<object, ILoopContract> loopContractFor = new Dictionary<object, ILoopContract>();

    /// <summary>
    /// Returns the method contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="method">An object that might have been associated with a method contract. This can be any kind of object.</param>
    public IMethodContract/*?*/ GetMethodContractFor(object method) {
      lock (this.methodContractFor) {
        IMethodContract/*?*/ result;
        if (this.methodContractFor.TryGetValue(method, out result)) {
          return result;
        }
      }
      return null;
    }
    private readonly Dictionary<object, IMethodContract> methodContractFor = new Dictionary<object, IMethodContract>();

    /// <summary>
    /// Returns the triggers, if any, that have been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="quantifier">An object that might have been associated with triggers. This can be any kind of object.</param>
    public IEnumerable<IEnumerable<IExpression>>/*?*/ GetTriggersFor(object quantifier) {
      lock (this.triggersFor) {
        IEnumerable<IEnumerable<IExpression>>/*?*/ result;
        if (this.triggersFor.TryGetValue(quantifier, out result)) {
          return result;
        }
      }
      return null;
    }
    private readonly Dictionary<object, IEnumerable<IEnumerable<IExpression>>> triggersFor = new Dictionary<object, IEnumerable<IEnumerable<IExpression>>>();

    /// <summary>
    /// Returns the type contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="type">An object that might have been associated with a type contract. This can be any kind of object.</param>
    public ITypeContract/*?*/ GetTypeContractFor(object type) {
      lock (this.typeContractFor) {
        ITypeContract/*?*/ result;
        if (this.typeContractFor.TryGetValue(type, out result)) {
          return result;
        }
      }
      return null;
    }
    private readonly Dictionary<object, ITypeContract> typeContractFor = new Dictionary<object, ITypeContract>();


  }

  /// <summary>
  /// A collection of collections of objects that describe a loop.
  /// </summary>
  public sealed class LoopContract : ILoopContract {

    public LoopContract() {
      this.invariants = new List<ILoopInvariant>();
      this.locations = new List<ILocation>(1);
    }

    public LoopContract(ILoopContract loopContract) {
      this.invariants = new List<ILoopInvariant>(loopContract.Invariants);
      this.locations = new List<ILocation>(loopContract.Locations);
      if (loopContract.Writes != null)
        this.writes = new List<IExpression>(loopContract.Writes);
    }

    /// <summary>
    /// A possibly empty list of loop invariants.
    /// </summary>
    public List<ILoopInvariant> Invariants {
      get { return this.invariants; }
      set { this.invariants = value; }
    }
    List<ILoopInvariant> invariants;


    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the loop.
    /// Is null when no writes clause was specified.
    /// </summary>
    public List<IExpression>/*?*/ Writes {
      get { return this.writes; }
      set { this.writes = value; }
    }
    List<IExpression>/*?*/ writes;

    #region ILoopContract Members

    IEnumerable<ILoopInvariant> ILoopContract.Invariants {
      get { return this.Invariants.AsReadOnly(); }
    }

    IEnumerable<IExpression>/*?*/ ILoopContract.Writes {
      get { return this.Writes == null ? null : this.Writes.AsReadOnly(); }
    }

    public bool HasErrors() {
      return false;
    }

    #endregion

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// A condition that must be true at the start of every iteration of a loop.
  /// </summary>
  public sealed class LoopInvariant : ILoopInvariant {

    public LoopInvariant() {
      this.condition = CodeDummy.Expression;
      this.locations = new List<ILocation>(1);
    }

    public LoopInvariant(ILoopInvariant loopInvariant) {
      this.condition = loopInvariant.Condition;
      this.locations = new List<ILocation>(loopInvariant.Locations);
    }


    /// <summary>
    /// The condition that must be true at the start of every iteration of a loop.
    /// </summary>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    #region ILoopInvariant Members

    public bool HasErrors() {
      return false;
    }

    #endregion

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// A collection of collections of objects that augment the type signature of a method with additional information
  /// that describe the contract between calling method and called method.
  /// </summary>
  public sealed class MethodContract : IMethodContract {

    public MethodContract() {
      this.allocates = new List<IExpression>();
      this.frees = new List<IExpression>();
      this.locations = new List<ILocation>(1);
      this.modifiedVariables = new List<IAddressableExpression>();
      this.mustInline = false;
      this.postconditions = new List<IPostcondition>();
      this.preconditions = new List<IPrecondition>();
      this.reads = new List<IExpression>();
      this.thrownExceptions = new List<IThrownException>();
      this.writes = new List<IExpression>();
    }

    public MethodContract(IMethodContract methodContract) {
      this.allocates = new List<IExpression>(methodContract.Allocates);
      this.frees = new List<IExpression>(methodContract.Frees);
      this.locations = new List<ILocation>(methodContract.Locations);
      this.modifiedVariables = new List<IAddressableExpression>(methodContract.ModifiedVariables);
      this.mustInline = methodContract.MustInline;
      this.postconditions = new List<IPostcondition>(methodContract.Postconditions);
      this.preconditions = new List<IPrecondition>(methodContract.Preconditions);
      this.reads = new List<IExpression>(methodContract.Reads);
      this.thrownExceptions = new List<IThrownException>(methodContract.ThrownExceptions);
      this.writes = new List<IExpression>(methodContract.Writes);
    }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that are newly allocated by a call to the method.
    /// </summary>
    public List<IExpression> Allocates {
      get { return this.allocates; }
      set { this.allocates = value; }
    }
    List<IExpression> allocates;

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that are freed by a call to the method.
    /// </summary>
    public List<IExpression> Frees {
      get { return this.frees; }
      set { this.frees = value; }
    }
    List<IExpression> frees;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// A possibly empty list of addressable expressions (variables) that are modified by the called method.
    /// </summary>
    public List<IAddressableExpression> ModifiedVariables {
      get { return this.modifiedVariables; }
      set { this.modifiedVariables = value; }
    }
    List<IAddressableExpression> modifiedVariables;

    /// <summary>
    /// The method body constitutes its contract. Callers must substitute the body in line with the call site.
    /// </summary>
    public bool MustInline {
      get { return this.mustInline; }
      set { this.mustInline = value; }
    }
    bool mustInline;

    /// <summary>
    /// A possibly empty list of postconditions that are established by the called method.
    /// </summary>
    public List<IPostcondition> Postconditions {
      get { return this.postconditions; }
      set { this.postconditions = value; }
    }
    List<IPostcondition> postconditions;

    /// <summary>
    /// A possibly empty list of preconditions that must be established by the calling method.
    /// </summary>
    public List<IPrecondition> Preconditions {
      get { return this.preconditions; }
      set { this.preconditions = value; }
    }
    List<IPrecondition> preconditions;

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be read by the called method.
    /// </summary>
    public List<IExpression> Reads {
      get { return this.reads; }
      set { this.reads = value; }
    }
    List<IExpression> reads;

    /// <summary>
    /// A possibly empty list of exceptions that may be thrown (or passed on) by the called method.
    /// </summary>
    public List<IThrownException> ThrownExceptions {
      get { return this.thrownExceptions; }
      set { this.thrownExceptions = value; }
    }
    List<IThrownException> thrownExceptions;

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the called method.
    /// </summary>
    public List<IExpression> Writes {
      get { return this.writes; }
      set { this.writes = value; }
    }
    List<IExpression> writes;

    #region IMethodContract Members

    public bool HasErrors() {
      return false;
    }

    IEnumerable<IExpression> IMethodContract.Allocates {
      get { return this.Allocates.AsReadOnly(); }
    }

    IEnumerable<IExpression> IMethodContract.Frees {
      get { return this.Frees.AsReadOnly(); }
    }

    IEnumerable<IAddressableExpression> IMethodContract.ModifiedVariables {
      get { return this.ModifiedVariables.AsReadOnly(); }
    }

    IEnumerable<IPostcondition> IMethodContract.Postconditions {
      get { return this.Postconditions.AsReadOnly(); }
    }

    IEnumerable<IPrecondition> IMethodContract.Preconditions {
      get { return this.Preconditions.AsReadOnly(); }
    }

    IEnumerable<IExpression> IMethodContract.Reads {
      get { return this.Reads.AsReadOnly(); }
    }

    IEnumerable<IThrownException> IMethodContract.ThrownExceptions {
      get { return this.ThrownExceptions.AsReadOnly(); }
    }

    IEnumerable<IExpression> IMethodContract.Writes {
      get { return this.Writes.AsReadOnly(); }
    }

    #endregion

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// A condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
  /// </summary>
  public sealed class Precondition : IPrecondition {

    public Precondition() {
      this.alwaysCheckedAtRuntime = false;
      this.condition = CodeDummy.Expression;
      this.exceptionToThrow = null;
      this.locations = new List<ILocation>(1);
    }

    public Precondition(IPrecondition precondition) {
      this.alwaysCheckedAtRuntime = precondition.AlwaysCheckedAtRuntime;
      this.condition = precondition.Condition;
      this.exceptionToThrow = precondition.ExceptionToThrow;
      this.locations = new List<ILocation>(precondition.Locations);
    }

    /// <summary>
    /// The precondition is always checked at runtime, even in release builds.
    /// </summary>
    public bool AlwaysCheckedAtRuntime {
      get { return this.alwaysCheckedAtRuntime; }
      set { this.alwaysCheckedAtRuntime = value; }
    }
    bool alwaysCheckedAtRuntime;

    /// <summary>
    /// The condition that must be true at the start of the method that is associated with this instance.
    /// </summary>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// An exeption that will be thrown if Condition is not true at the start of the method that is associated with this instance.
    /// May be null. If null, the runtime behavior of the associated method is undefined when Condition is not true.
    /// </summary>
    public IExpression/*?*/ ExceptionToThrow {
      get { return this.exceptionToThrow; }
      set { this.exceptionToThrow = value; }
    }
    IExpression/*?*/ exceptionToThrow;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// Checks the expression for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() {
      return false;
    }

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// A condition that must be true at the end of a method.
  /// </summary>
  public sealed class PostCondition : IPostcondition {

    public PostCondition() {
      this.condition = CodeDummy.Expression;
      this.locations = new List<ILocation>(1);
    }

    public PostCondition(IPostcondition postcondition) {
      this.condition = postcondition.Condition;
      this.locations = new List<ILocation>(postcondition.Locations);
    }

    /// <summary>
    /// The condition that must be true at the end of the method that is associated with this instance.
    /// </summary>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// Checks the postcondition for errors and returns true if any were found.
    /// </summary>
    public bool HasErrors() { return false; }

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// An exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
  /// </summary>
  public sealed class ThrownException : IThrownException {

    public ThrownException() {
      this.exceptionType = Dummy.TypeReference;
      this.postconditions = new List<IPostcondition>();
    }

    public ThrownException(IThrownException thrownException) {
      this.exceptionType = thrownException.ExceptionType;
      this.postconditions = new List<IPostcondition>(thrownException.Postconditions);
    }

    /// <summary>
    /// The exception that can be thrown by the associated method.
    /// </summary>
    public ITypeReference ExceptionType {
      get { return this.exceptionType; }
      set { this.exceptionType = value; }
    }
    ITypeReference exceptionType;

    /// <summary>
    /// The postconditions that hold if the associated method throws this exception. Can be empty (but not null).
    /// </summary>
    public List<IPostcondition> Postconditions {
      get { return this.postconditions; }
      set { this.postconditions = value; }
    }
    List<IPostcondition> postconditions;

    #region IThrownException Members


    IEnumerable<IPostcondition> IThrownException.Postconditions {
      get { return this.Postconditions.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// A collection of collections of objects that augment the signature of a type with additional information
  /// that describe invariants, model variables and functions, as well as axioms.
  /// </summary>
  public sealed class TypeContract : ITypeContract {

    public TypeContract() {
      this.contractFields = new List<IFieldDefinition>();
      this.contractMethods = new List<IMethodDefinition>();
      this.invariants = new List<ITypeInvariant>();
      this.locations = new List<ILocation>(1);
    }

    public TypeContract(ITypeContract typeContract) {
      this.contractFields = new List<IFieldDefinition>(typeContract.ContractFields);
      this.contractMethods = new List<IMethodDefinition>(typeContract.ContractMethods);
      this.invariants = new List<ITypeInvariant>(typeContract.Invariants);
      this.locations = new List<ILocation>(typeContract.Locations);
    }

    /// <summary>
    /// A possibly empty list of contract fields. Contract fields can only be used inside contracts and are not available at runtime.
    /// </summary>
    public List<IFieldDefinition> ContractFields {
      get { return this.contractFields; }
      set { this.contractFields = value; }
    }
    List<IFieldDefinition> contractFields;

    /// <summary>
    /// A possibly empty list of contract methods. Contract methods have no bodies and can only be used inside contracts. The meaning of a contract
    /// method is specified by the axioms of the associated type. Contract methods are not available at runtime.
    /// </summary>
    public List<IMethodDefinition> ContractMethods {
      get { return this.contractMethods; }
      set { this.contractMethods = value; }
    }
    List<IMethodDefinition> contractMethods;

    /// <summary>
    /// A possibly empty list of type invariants.
    /// </summary>
    public List<ITypeInvariant> Invariants {
      get { return this.invariants; }
      set { this.invariants = value; }
    }
    List<ITypeInvariant> invariants;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    #region ITypeContract Members

    public bool HasErrors() {
      return false;
    }

    IEnumerable<IFieldDefinition> ITypeContract.ContractFields {
      get { return this.ContractFields.AsReadOnly(); }
    }

    IEnumerable<IMethodDefinition> ITypeContract.ContractMethods {
      get { return this.ContractMethods.AsReadOnly(); }
    }

    IEnumerable<ITypeInvariant> ITypeContract.Invariants {
      get { return this.Invariants.AsReadOnly(); }
    }

    #endregion

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }

  /// <summary>
  /// A condition that must be true after an object has been constructed and that is by default a part of the precondition and postcondition of every public method of the associated type.
  /// </summary>
  public sealed class TypeInvariant : ITypeInvariant {

    public TypeInvariant() {
      this.condition = CodeDummy.Expression;
      this.isAxiom = false;
      this.locations = new List<ILocation>(1);
      this.name = null;
    }

    public TypeInvariant(ITypeInvariant typeInvariant) {
      this.condition = typeInvariant.Condition;
      this.isAxiom = typeInvariant.IsAxiom;
      this.locations = new List<ILocation>(typeInvariant.Locations);
      this.name = typeInvariant.Name;
    }

    /// <summary>
    /// The condition that must be true after an object of the type associated with this invariant has been constructed.
    /// </summary>
    public IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// An axiom is a type invariant whose truth is assumed rather than derived. Commonly used to make statements about the meaning of contract methods.
    /// </summary>
    public bool IsAxiom {
      get { return this.isAxiom; }
      set { this.isAxiom = value; }
    }
    bool isAxiom;

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    /// <summary>
    /// The name of the invariant. Used in error diagnostics. May be null.
    /// </summary>
    public IName/*?*/ Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName/*?*/ name;


    #region ITypeInvariant Members

    public bool HasErrors() {
      return false;
    }

    #endregion

    #region IObjectWithLocations Members

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }


}