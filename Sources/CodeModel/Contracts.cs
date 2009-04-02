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
  /// A collection of methods that can be called in a way that provides tools with information about contracts.
  /// </summary>
  public interface IContractMethods {

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at the point of call.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    IMethodReference Assert { get; }

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at the point of call.
    /// A static verification tool may assume that this condition will be true for all executions that can reach the call.
    /// </summary>
    IMethodReference Assume { get; }

    /// <summary>
    /// A reference to a method that is called to indicate that any preceding code should be interpreted as part of the method contract.
    /// </summary>
    IMethodReference EndContract { get; }

    /// <summary>
    /// A reference to a generic method that is called to indicate that there exists a value of the type supplied as its generic argument for
    /// which the predicate supplied as its argument would return true if called with this value its argument.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    IMethodReference Exists { get; }

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at method exit.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    IMethodReference Ensures { get; }

    /// <summary>
    /// A reference to a generic method that is called to indicate that the predicate supplied as its argument should return true if called on
    /// any value of the type supplied as the generic argument.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    IMethodReference Forall { get; }

    /// <summary>
    /// A reference to a generic method whose result is the value of its argument expression as it was at the start of the method.
    /// </summary>
    IMethodReference Old { get; }

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at method entry.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    IMethodReference Requires { get; }

    /// <summary>
    /// A reference to a method whose result is the value that is returned from the method whose contract contains a call to Result.
    /// </summary>
    IMethodReference Result { get; }

    /// <summary>
    /// A reference to a method that is called to indicate that any preceding code should be executed before contract checking happens.
    /// </summary>
    IMethodReference StartContract { get; }

  }

  /// <summary>
  /// An object that associates contracts, such as preconditions and postconditions, with methods, types and loops. 
  /// </summary>
  public interface IContractProvider {

    /// <summary>
    /// Returns the loop contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="loop">An object that might have been associated with a loop contract. This can be any kind of object.</param>
    ILoopContract/*?*/ GetLoopContractFor(object loop);

    /// <summary>
    /// Returns the method contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="method">An object that might have been associated with a method contract. This can be any kind of object.</param>
    IMethodContract/*?*/ GetMethodContractFor(object method);

    /// <summary>
    /// Returns the triggers, if any, that have been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="quantifier">An object that might have been associated with triggers. This can be any kind of object.</param>
    IEnumerable<IEnumerable<IExpression>>/*?*/ GetTriggersFor(object quantifier);

    /// <summary>
    /// Returns the type contract, if any, that has been associated with the given object. Returns null if no association exits.
    /// </summary>
    /// <param name="type">An object that might have been associated with a type contract. This can be any kind of object.</param>
    ITypeContract/*?*/ GetTypeContractFor(object type);

    /// <summary>
    /// A collection of methods that can be called in a way that provides tools with information about contracts.
    /// </summary>
    IContractMethods/*?*/ ContractMethods { get; }

  }

  /// <summary>
  /// A collection of methods that can be called in a way that provides tools with information about contracts.
  /// </summary>
  public class ContractMethods : IContractMethods {

    /// <summary>
    /// Allocates a collection of methods that can be called in a way that provides tools with information about contracts.
    /// </summary>
    public ContractMethods(IMetadataHost host) {
      this.host = host;
    }

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at the point of call.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    public IMethodReference Assert {
      get {
        if (this.assertRef == null)
          this.assertRef = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Default, 
            this.host.PlatformType.SystemVoid, this.host.NameTable.GetNameFor("Assert"), 0, this.host.PlatformType.SystemBoolean);
        return this.assertRef;
      }
    }
    IMethodReference/*?*/ assertRef;

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at the point of call.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    public IMethodReference Assume {
      get {
        if (this.assumeRef == null)
          this.assumeRef = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Default,
            this.host.PlatformType.SystemVoid, this.host.NameTable.GetNameFor("Assume"), 0, this.host.PlatformType.SystemBoolean);
        return this.assumeRef;
      }
    }
    IMethodReference/*?*/ assumeRef;

    /// <summary>
    /// A reference to a method that is called to indicate that any preceding code should be interpreted as part of the method contract.
    /// </summary>
    public IMethodReference EndContract {
      get {
        if (this.endContract == null)
          this.endContract = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Default,
            this.host.PlatformType.SystemVoid, this.host.NameTable.GetNameFor("EndContractBlock"), 0);
        return this.endContract;
      }
    }
    IMethodReference endContract;

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at method entry.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    public IMethodReference Ensures {
      get {
        if (this.ensuresRef == null)
          this.ensuresRef = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Default,
            this.host.PlatformType.SystemVoid, this.host.NameTable.GetNameFor("Ensures"), 0, this.host.PlatformType.SystemBoolean);
        return this.ensuresRef;
      }
    }
    IMethodReference/*?*/ ensuresRef;

    /// <summary>
    /// A reference to a generic method that is called to indicate that there exists a value of the type supplied as its generic argument for
    /// which the predicate supplied as its argument would return true if called with this value its argument.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    public IMethodReference Exists {
      get {
        if (this.existsRef == null) {
          IName tname = this.host.NameTable.GetNameFor("T");
          GenericMethodParameterReference t = new GenericMethodParameterReference(tname, 0, this.host);
          IEnumerable<ITypeReference> tArg = IteratorHelper.GetSingletonEnumerable<ITypeReference>(t);
          IGenericTypeInstanceReference predT = GenericTypeInstance.GetGenericTypeInstance(this.PredicateType, tArg, this.host.InternFactory);
          MethodReference mr = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Generic,
            this.host.PlatformType.SystemBoolean, this.host.NameTable.GetNameFor("Exists"), 1, predT);
          t.DefiningMethod = mr;
          this.existsRef = mr;
        }
        return this.existsRef;
      }
    }
    IMethodReference/*?*/ existsRef;


    /// <summary>
    /// A reference to a generic method that is called to indicate that the predicate supplied as its argument should return true if called on
    /// any value of the type supplied as the generic argument.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    public IMethodReference Forall {
      get {
        if (this.forallRef == null) {
          IName tname = this.host.NameTable.GetNameFor("T");
          GenericMethodParameterReference t = new GenericMethodParameterReference(tname, 0, this.host);
          IEnumerable<ITypeReference> tArg = IteratorHelper.GetSingletonEnumerable<ITypeReference>(t);
          IGenericTypeInstanceReference predT = GenericTypeInstance.GetGenericTypeInstance(this.PredicateType, tArg, this.host.InternFactory);
          MethodReference mr = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Generic,
            this.host.PlatformType.SystemBoolean, this.host.NameTable.GetNameFor("Forall"), 1, predT);
          t.DefiningMethod = mr;
          this.forallRef = mr;
        }
        return this.forallRef;
      }
    }
    IMethodReference/*?*/ forallRef;

    private ITypeReference PredicateType {
      get {
        if (this.predicateType == null)
          this.predicateType = new NamespaceTypeReference(this.host, this.host.PlatformType.SystemObject.ContainingUnitNamespace,
            this.host.NameTable.GetNameFor("Predicate"), 1, false, false, PrimitiveTypeCode.NotPrimitive);
        return this.predicateType;
      }
    }
    ITypeReference/*?*/ predicateType;

    /// <summary>
    /// A reference to a generic method whose result is the value of its argument expression as it was at the start of the method.
    /// </summary>
    public IMethodReference Old {
      get {
        if (this.oldRef == null) {
          IName tname = this.host.NameTable.GetNameFor("T");
          GenericMethodParameterReference t = new GenericMethodParameterReference(tname, 0, this.host);
          MethodReference mr = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Generic,
            t, this.host.NameTable.GetNameFor("OldValue"), 1, t);
          t.DefiningMethod = mr;
          this.oldRef = mr;
        }
        return this.oldRef;
      }
    }
    IMethodReference/*?*/ oldRef;


    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument should hold at method entry.
    /// A static verification tool would have to prove that this condition will be true for all executions that can reach the call.
    /// </summary>
    public IMethodReference Requires {
      get {
        if (this.requiresRef == null)
          this.requiresRef = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Default,
            this.host.PlatformType.SystemVoid, this.host.NameTable.GetNameFor("Requires"), 0, this.host.PlatformType.SystemBoolean);
        return this.requiresRef;
      }
    }
    IMethodReference/*?*/ requiresRef;

    /// <summary>
    /// A reference to a method whose result is the value that is returned from the method whose contract contains a call to Result.
    /// </summary>
    public IMethodReference Result {
      get {
        if (this.result == null) {
          IName tname = this.host.NameTable.GetNameFor("T");
          GenericMethodParameterReference t = new GenericMethodParameterReference(tname, 0, this.host);
          MethodReference mr = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Generic,
            t, this.host.NameTable.GetNameFor("Result"), 1);
          t.DefiningMethod = mr;
          this.result = mr;
        }
        return this.result;
      }
    }
    IMethodReference/*?*/ result;

    /// <summary>
    /// A reference to a method that is called to indicate that any preceding code should be executed before contract checking happens.
    /// </summary>
    public IMethodReference StartContract {
      get {
        if (this.startContract == null)
          this.startContract = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Default,
            this.host.PlatformType.SystemVoid, this.host.NameTable.GetNameFor("StartContract"), 0);
        return this.startContract;
      }
    }
    IMethodReference startContract;

    IMetadataHost host;

  }

  /// <summary>
  /// A collection of collections of objects that describe a loop.
  /// </summary>
  public interface ILoopContract : IErrorCheckable {
    /// <summary>
    /// A possibly empty list of loop invariants.
    /// </summary>
    IEnumerable<ILoopInvariant> Invariants { get; }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the loop.
    /// Is null when no writes clause was specified.
    /// </summary>
    IEnumerable<IExpression>/*?*/ Writes { get; }
  }

  /// <summary>
  /// A condition that must be true at the start of every iteration of a loop.
  /// </summary>
  public interface ILoopInvariant : IErrorCheckable {
    /// <summary>
    /// The condition that must be true at the start of every iteration of a loop.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    IEnumerable<ILocation> Locations { get;}
  }

  /// <summary>
  /// A collection of collections of objects that augment the type signature of a method with additional information
  /// that describe the contract between calling method and called method.
  /// </summary>
  public interface IMethodContract : IErrorCheckable {

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that are newly allocated by a call to the method.
    /// </summary>
    IEnumerable<IExpression> Allocates { get; }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that are freed by a call to the method.
    /// </summary>
    IEnumerable<IExpression> Frees { get; }

    /// <summary>
    /// A possibly empty list of addressable expressions (variables) that are modified by the called method.
    /// </summary>
    IEnumerable<IAddressableExpression> ModifiedVariables { get; }

    /// <summary>
    /// The method body constitutes its contract. Callers must substitute the body in line with the call site.
    /// </summary>
    bool MustInline { get; }

    /// <summary>
    /// A possibly empty list of postconditions that are established by the called method.
    /// </summary>
    IEnumerable<IPostcondition> Postconditions { get; }

    /// <summary>
    /// A possibly empty list of preconditions that must be established by the calling method.
    /// </summary>
    IEnumerable<IPrecondition> Preconditions { get; }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be read by the called method.
    /// </summary>
    IEnumerable<IExpression> Reads { get; }

    /// <summary>
    /// A possibly empty list of exceptions that may be thrown (or passed on) by the called method.
    /// </summary>
    IEnumerable<IThrownException> ThrownExceptions { get; }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the called method.
    /// </summary>
    IEnumerable<IExpression> Writes { get; }
 }

  /// <summary>
  /// A condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
  /// </summary>
  public interface IPrecondition : IErrorCheckable {

    /// <summary>
    /// The precondition is always checked at runtime, even in release builds.
    /// </summary>
    bool AlwaysCheckedAtRuntime { get; }

    /// <summary>
    /// The condition that must be true at the start of the method that is associated with this instance.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// One of three things:
    /// 1. Null. If null, the runtime behavior of the associated method is undefined when Condition is not true.
    /// 2. An exception: the value of type Exception that will be thrown if Condition is not true at the start of the method that is associated with this instance.
    /// 3. A method call to a void method. The method is called if Condition is not true at the start of the method that is associated with this instance.
    ///    The method is assumed to throw and never terminate normally, but there is no check for this.
    /// </summary>
    IExpression/*?*/ ExceptionToThrow { get; }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    IEnumerable<ILocation> Locations { get;}
  }

  /// <summary>
  /// A condition that must be true at the end of a method.
  /// </summary>
  public interface IPostcondition : IErrorCheckable {

    /// <summary>
    /// The condition that must be true at the end of the method that is associated with this instance.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    IEnumerable<ILocation> Locations { get;}
  }

  /// <summary>
  /// An exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
  /// </summary>
  public interface IThrownException {

    /// <summary>
    /// The exception that can be thrown by the associated method.
    /// </summary>
    ITypeReference ExceptionType { get; }

    /// <summary>
    /// The postconditions that hold if the associated method throws this exception. Can be empty (but not null).
    /// </summary>
    IEnumerable<IPostcondition> Postconditions { get; }
  }

  /// <summary>
  /// A collection of collections of objects that augment the signature of a type with additional information
  /// that describe invariants, model variables and functions, as well as axioms.
  /// </summary>
  public interface ITypeContract : IErrorCheckable {

    /// <summary>
    /// A possibly empty list of contract fields. Contract fields can only be used inside contracts and are not available at runtime.
    /// </summary>
    IEnumerable<IFieldDefinition> ContractFields { get; }

    /// <summary>
    /// A possibly empty list of contract methods. Contract methods have no bodies and can only be used inside contracts. The meaning of a contract
    /// method is specified by the axioms of the associated type. Contract methods are not available at runtime.
    /// </summary>
    IEnumerable<IMethodDefinition> ContractMethods { get; }

    /// <summary>
    /// A possibly empty list of type invariants.
    /// </summary>
    IEnumerable<ITypeInvariant> Invariants { get; }
  }

  /// <summary>
  /// A condition that must be true after an object has been constructed and that is by default a part of the precondition and postcondition of every public method of the associated type.
  /// </summary>
  public interface ITypeInvariant : IErrorCheckable {
    /// <summary>
    /// The condition that must be true after an object of the type associated with this invariant has been constructed.
    /// </summary>
    IExpression Condition { get; }

    /// <summary>
    /// An axiom is a type invariant whose truth is assumed rather than derived. Commonly used to make statements about the meaning of contract methods.
    /// </summary>
    bool IsAxiom { get; }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    IEnumerable<ILocation> Locations { get;}

    /// <summary>
    /// The name of the invariant. Used in error diagnostics. May be null.
    /// </summary>
    IName/*?*/ Name { get; }

  }


}
