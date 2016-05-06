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
using Microsoft.Cci.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableContracts {

  /// <summary>
  /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
  /// </summary>
  public class ContractProvider : IContractProvider {

    /// <summary>
    /// Allocates an object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
    /// If the object is already associated with a contract, that association will be lost as a result of this call.
    /// </summary>
    /// <param name="contractMethods">A collection of methods that can be called in a way that provides tools with information about contracts.</param>
    /// <param name="unit">The unit that this is a contract provider for.</param>
    public ContractProvider(IContractMethods contractMethods, IUnit unit) {
      this.contractMethods = contractMethods;
      this.unit = unit;
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
    /// Removes the association of the given object with the loop contract.
    /// </summary>
    /// <param name="loop">The object to remove the association with the loop contract. This can be any kind of object.</param>
    public void UnassociateLoopWithContract(object loop) {
      lock (this.methodContractFor) {
        this.methodContractFor.Remove(loop);
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
    /// Removes the association of the given object with the method contract.
    /// </summary>
    /// <param name="method">The object to remove the association with the method contract. This can be any kind of object.</param>
    public void UnassociateMethodWithContract(object method) {
      lock (this.methodContractFor) {
        this.methodContractFor.Remove(method);
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
    /// Removes the association of the given object with the list of triggers.
    /// </summary>
    /// <param name="quantifier">The object to remove the association with the list of triggers. This can be any kind of object.</param>
    public void UnassociateTriggersWithQuantifier(object quantifier) {
      lock (this.triggersFor) {
        this.triggersFor.Remove(quantifier);
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
    /// Removes the association of the given object with the type contract.
    /// </summary>
    /// <param name="type">The object to remove the association with the type contract. This can be any kind of object.</param>
    public void UnassociateTypeWithContract(object type) {
      lock (this.typeContractFor) {
        this.typeContractFor.Remove(type);
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

    /// <summary>
    /// The unit that this is a contract provider for. Intentional design:
    /// no provider works on more than one unit.
    /// </summary>
    public IUnit/*?*/ Unit {
      get { return this.unit; }
    }
    IUnit/*?*/ unit;

  }

  /// <summary>
  /// A collection of collections of objects that describe a loop.
  /// </summary>
  public sealed class LoopContract : ILoopContract {

    /// <summary>
    /// 
    /// </summary>
    public LoopContract() {
      this.invariants = new List<ILoopInvariant>();
      this.locations = new List<ILocation>(1);
      this.variants = new List<IExpression>();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="loopContract"></param>
    public LoopContract(ILoopContract loopContract) {
      this.invariants = new List<ILoopInvariant>(loopContract.Invariants);
      this.locations = new List<ILocation>(loopContract.Locations);
      if (loopContract.Writes != null)
        this.writes = new List<IExpression>(loopContract.Writes);
      this.variants = new List<IExpression>(loopContract.Variants);
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
    /// A possibly empty list of loop variants.
    /// </summary>
    public List<IExpression> Variants {
      get { return this.variants; }
      set { this.variants = value; }
    }
    List<IExpression> variants;

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

    IEnumerable<IExpression> ILoopContract.Variants {
      get { return this.Variants.AsReadOnly(); }
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
  public sealed class LoopInvariant : ContractElement, ILoopInvariant {

    /// <summary>
    /// Creates a fresh loop invariant.
    /// </summary>
    public LoopInvariant() {
    }

    /// <summary>
    /// Creates a loop invariant that shares all of the information in <paramref name="loopInvariant"/>.
    /// </summary>
    /// <param name="loopInvariant"></param>
    public LoopInvariant(ILoopInvariant loopInvariant)
      : base(loopInvariant) {
    }

    /// <summary>
    /// Calls visitor.Visit(ILoopInvariant).
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// A collection of collections of objects that augment the type signature of a method with additional information
  /// that describe the contract between calling method and called method.
  /// </summary>
  public sealed class MethodContract : IMethodContract {

    /// <summary>
    /// 
    /// </summary>
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
      this.variants = new List<IExpression>();
      this.isPure = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodContract"></param>
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
      this.variants = new List<IExpression>(methodContract.Variants);
      this.writes = new List<IExpression>(methodContract.Writes);
      this.isPure = methodContract.IsPure;
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
    public List<IExpression> Variants {
      get { return this.variants; }
      set { this.variants = value; }
    }
    List<IExpression> variants;

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the called method.
    /// </summary>
    public List<IExpression> Writes {
      get { return this.writes; }
      set { this.writes = value; }
    }
    List<IExpression> writes;

    /// <summary>
    /// True if the method has no observable side-effect on program state and hence this method is safe to use in a contract,
    /// which may or may not be executed, depending on how the program has been compiled.
    /// </summary>
    public bool IsPure {
      get { return this.isPure; }
      set { this.isPure = value; }
    }
    bool isPure;

    #region IMethodContract Members

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

    IEnumerable<IExpression> IMethodContract.Variants {
      get { return this.Variants.AsReadOnly(); }
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
  public sealed class Precondition : ContractElement, IPrecondition {

    /// <summary>
    /// Creates a fresh precondition.
    /// </summary>
    public Precondition()
      : base() {
      this.alwaysCheckedAtRuntime = false;
      this.exceptionToThrow = null;
    }

    /// <summary>
    /// Creates a precondition that shares all of the information in <paramref name="precondition"/>.
    /// </summary>
    /// <param name="precondition"></param>
    public Precondition(IPrecondition precondition)
      : base(precondition) {
      this.alwaysCheckedAtRuntime = precondition.AlwaysCheckedAtRuntime;
      this.exceptionToThrow = precondition.ExceptionToThrow;
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
    /// Calls visitor.Visit(IPrecondition).
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// An exeption that will be thrown if Condition is not true at the start of the method that is associated with this instance.
    /// May be null. If null, the runtime behavior of the associated method is undefined when Condition is not true.
    /// </summary>
    public IExpression/*?*/ ExceptionToThrow {
      get { return this.exceptionToThrow; }
      set { this.exceptionToThrow = value; }
    }
    IExpression/*?*/ exceptionToThrow;

  }

  /// <summary>
  /// A condition that must be true at the end of a method.
  /// </summary>
  public sealed class Postcondition : ContractElement, IPostcondition {

    /// <summary>
    /// Creates a fresh postcondition.
    /// </summary>
    public Postcondition()
      : base() {
    }

    /// <summary>
    /// Creates a postcondition that shares all of the information in <paramref name="postcondition"/>
    /// </summary>
    /// <param name="postcondition"></param>
    public Postcondition(IPostcondition postcondition)
      : base(postcondition) {
    }

    /// <summary>
    /// Calls visitor.Visit(IPostCondition).
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

  }

  /// <summary>
  /// An exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
  /// </summary>
  public sealed class ThrownException : IThrownException {

    /// <summary>
    /// 
    /// </summary>
    public ThrownException() {
      this.exceptionType = Dummy.TypeReference;
      this.postcondition = ContractDummy.Postcondition;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="thrownException"></param>
    public ThrownException(IThrownException thrownException) {
      this.exceptionType = thrownException.ExceptionType;
      this.postcondition = thrownException.Postcondition;
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
    /// The postcondition that holds if the associated method throws this exception.
    /// </summary>
    public IPostcondition Postcondition {
      get { return this.postcondition; }
      set { this.postcondition = value; }
    }
    IPostcondition postcondition;

    #region IObjectWithLocations Members

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion

  }

  /// <summary>
  /// A collection of collections of objects that augment the signature of a type with additional information
  /// that describe invariants, model variables and functions, as well as axioms.
  /// </summary>
  public sealed class TypeContract : ITypeContract {

    /// <summary>
    /// 
    /// </summary>
    public TypeContract() {
      this.contractFields = new List<IFieldDefinition>();
      this.contractMethods = new List<IMethodDefinition>();
      this.invariants = new List<ITypeInvariant>();
      this.locations = new List<ILocation>(1);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeContract"></param>
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
  public sealed class TypeInvariant : ContractElement, ITypeInvariant {

    /// <summary>
    /// Creates a fresh type invariant.
    /// </summary>
    public TypeInvariant()
      : base() {
      this.isAxiom = false;
      this.name = null;
    }

    /// <summary>
    /// Creates a type invariant that shares all of the information in <paramref name="typeInvariant"/>.
    /// </summary>
    /// <param name="typeInvariant"></param>
    public TypeInvariant(ITypeInvariant typeInvariant)
      : base(typeInvariant) {
      this.isAxiom = typeInvariant.IsAxiom;
      this.name = typeInvariant.Name;
    }

    /// <summary>
    /// An axiom is a type invariant whose truth is assumed rather than derived. Commonly used to make statements about the meaning of contract methods.
    /// </summary>
    public bool IsAxiom {
      get { return this.isAxiom; }
      set { this.isAxiom = value; }
    }
    bool isAxiom;

    /// <summary>
    /// Calls visitor.Visit(ITypeInvariant).
    /// </summary>
    /// <param name="visitor"></param>
    public override void Dispatch(ICodeAndContractVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The name of the invariant. Used in error diagnostics. May be null.
    /// </summary>
    public IName/*?*/ Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName/*?*/ name;

  }

  /// <summary>
  /// A common supertype providing a shared implementation used by the classes
  /// LoopInvariant, Precondition, PostCondition, and TypeInvariant.
  /// </summary>
  public abstract class ContractElement : IContractElement {

    /// <summary>
    /// Creates a fresh contract element with no information in it.
    /// </summary>
    protected ContractElement() {
      this.condition = CodeDummy.Expression;
      this.description = null;
      this.conditionAsText = null;
      this.isModel = false;
      this.locations = new List<ILocation>(1);
    }

    /// <summary>
    /// Creates a contract element that shares all of the information in <paramref name="element"/>.
    /// </summary>
    /// <param name="element"></param>
    protected ContractElement(IContractElement element) {
      this.condition = element.Condition;
      this.description = element.Description;
      this.conditionAsText = element.OriginalSource;
      this.isModel = element.IsModel;
      this.locations = new List<ILocation>(element.Locations);
    }

    #region IContractElement Members

    /// <summary>
    /// The condition associated with this particular contract element.
    /// 
    /// For  a loop invariant, the condition that must be true at the start of every iteration of a loop.
    /// For a precondition, it is the condition that must be true for a caller to call the associated
    /// method.
    /// For a postcondition, it is the condition that must be true at the end of the method that is
    /// associated with this instance.
    /// For an object invariant, well, it is complicated. In general it is a condition that must be
    /// true at the end of a public constructor and is both a pre- and postcondition for public methods
    /// on the type with which the object invariant is associated.
    /// 
    /// The meaning of the condition is dependent on the type of contract. For instance, mostly this
    /// will be a boolean-valued expression. But it could also be used for loop variant functions in
    /// which case this would be an expression which represents a natural number.
    /// </summary>
    public virtual IExpression Condition {
      get { return this.condition; }
      set { this.condition = value; }
    }
    IExpression condition;

    /// <summary>
    /// An optional expression that is associated with this particular contract element. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    public virtual IExpression/*?*/ Description {
      get { return this.description; }
      set { this.description = value; }
    }
    IExpression description;

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IContractElement.
    /// </summary>
    /// <param name="visitor"></param>
    public abstract void Dispatch(ICodeAndContractVisitor visitor);

    /// <summary>
    /// As an option, tools that provide contracts may want to have a "string-ified" version
    /// of the condition.
    /// </summary>
    public virtual string/*?*/ OriginalSource {
      get { return this.conditionAsText; }
      set { this.conditionAsText = value; }
    }
    string conditionAsText;

    /// <summary>
    /// True iff any member mentioned in the Condition is a "model member", i.e.,
    /// its definition has the [ContractModel] attribute on it.
    /// </summary>
    public bool IsModel {
      get { return this.isModel; }
      set { this.isModel = value; }
    }
    bool isModel;

    #endregion

    #region IObjectWithLocations Members

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public virtual List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.Locations.AsReadOnly(); }
    }

    #endregion
  }


}