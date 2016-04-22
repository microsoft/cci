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
using System.Collections.Generic;
using Microsoft.Cci.Immutable;
using System.Diagnostics.Contracts;

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
    /// A reference to a method that is called to indicate that the condition supplied as its argument is an
    /// object invariant for the type in which the method call is found. The exact meaning of when an object
    /// invariant must hold is left up to the individual tools.
    /// </summary>
    IMethodReference Invariant { get; }

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

    /// <summary>
    /// The unit that this is a contract provider for. Intentional design:
    /// no provider works on more than one unit.
    /// </summary>
    IUnit/*?*/ Unit { get; }

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
          IGenericTypeInstanceReference predT = new GenericTypeInstanceReference(this.PredicateType, tArg, this.host.InternFactory);
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
          IGenericTypeInstanceReference predT = new GenericTypeInstanceReference(this.PredicateType, tArg, this.host.InternFactory);
          MethodReference mr = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Generic,
            this.host.PlatformType.SystemBoolean, this.host.NameTable.GetNameFor("Forall"), 1, predT);
          t.DefiningMethod = mr;
          this.forallRef = mr;
        }
        return this.forallRef;
      }
    }
    IMethodReference/*?*/ forallRef;

    private INamedTypeReference PredicateType {
      get {
        if (this.predicateType == null)
          this.predicateType = new NamespaceTypeReference(this.host, this.host.PlatformType.SystemObject.ContainingUnitNamespace,
            this.host.NameTable.GetNameFor("Predicate"), 1, false, false, true, PrimitiveTypeCode.NotPrimitive);
        return this.predicateType;
      }
    }
    INamedTypeReference/*?*/ predicateType;

    /// <summary>
    /// A reference to a method that is called to indicate that the condition supplied as its argument is an
    /// object invariant for the type in which the method call is found. The exact meaning of when an object
    /// invariant must hold is left up to the individual tools.
    /// </summary>
    public IMethodReference Invariant {
      get {
        if (this.invariantRef == null)
          this.invariantRef = new MethodReference(this.host, this.host.PlatformType.SystemDiagnosticsContractsContract, CallingConvention.Default,
            this.host.PlatformType.SystemVoid, this.host.NameTable.GetNameFor("Invariant"), 0, this.host.PlatformType.SystemBoolean);
        return this.invariantRef;
      }
    }
    IMethodReference/*?*/ invariantRef;

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
  /// A common supertype for contracts like preconditions, postconditions,
  /// object invariants, and loop invariants.
  /// </summary>
  public interface IContractElement : IObjectWithLocations {
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
    IExpression Condition { get; }

    /// <summary>
    /// An optional expression that is associated with this particular contract element. Generally, it would
    /// be a message that was written at the same time as the contract and is meant to be used as a description
    /// when the contract fails.
    /// </summary>
    IExpression/*?*/ Description { get; }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IContractElement.
    /// </summary>
    void Dispatch(ICodeAndContractVisitor visitor);

    /// <summary>
    /// The original source representation of the contract element.
    /// </summary>
    /// <remarks>
    /// Normally this would be extracted directly from the source file.
    /// The expectation is that one would translate the Condition into
    /// a source string in a source language appropriate for
    /// the particular tool environment, e.g., when doing static analysis,
    /// in the language the client code uses, not the language the contract
    /// was written.
    /// </remarks>
    string/*?*/ OriginalSource { get; }

    /// <summary>
    /// True iff any member mentioned in the Condition is a "model member", i.e.,
    /// its definition has the [ContractModel] attribute on it.
    /// </summary>
    bool IsModel { get; }
  }

  /// <summary>
  /// A collection of collections of objects that describe a loop.
  /// </summary>
  public interface ILoopContract : IObjectWithLocations {
    /// <summary>
    /// A possibly empty list of loop invariants.
    /// </summary>
    IEnumerable<ILoopInvariant> Invariants { get; }

    /// <summary>
    /// A possibly empty list of expressions that each represents a set of memory locations that may be written to by the loop.
    /// Is null when no writes clause was specified.
    /// </summary>
    IEnumerable<IExpression>/*?*/ Writes { get; }

    /// <summary>
    /// A possibly empty list of loop variants.
    /// </summary>
    IEnumerable<IExpression> Variants { get; }

  }

  /// <summary>
  /// A condition that must be true at the start of every iteration of a loop.
  /// </summary>
  public interface ILoopInvariant : IContractElement {
  }

  /// <summary>
  /// A collection of collections of objects that augment the type signature of a method with additional information
  /// that describe the contract between calling method and called method.
  /// </summary>
  public partial interface IMethodContract : IObjectWithLocations {

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

    /// <summary>
    /// A possibly empty list of expressions that each represents a measure that goes down on every method call invoked by the called method.
    /// </summary>
    IEnumerable<IExpression> Variants { get; }

    /// <summary>
    /// True if the method has no observable side-effect on program state and hence this method is safe to use in a contract,
    /// which may or may not be executed, depending on how the program has been compiled.
    /// </summary>
    bool IsPure { get; }
  }

  #region IMethodContract contract binding
  [ContractClass(typeof(IMethodContractContract))]
  public partial interface IMethodContract { }

  [ContractClassFor(typeof(IMethodContract))]
  abstract class IMethodContractContract : IMethodContract
  {
    public IEnumerable<IExpression> Allocates
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new System.NotImplementedException(); 
      }
    }

    public IEnumerable<IExpression> Frees
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public IEnumerable<IAddressableExpression> ModifiedVariables
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IAddressableExpression>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public bool MustInline
    {
      get { throw new System.NotImplementedException(); }
    }

    public IEnumerable<IPostcondition> Postconditions
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IPostcondition>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public IEnumerable<IPrecondition> Preconditions
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IPrecondition>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public IEnumerable<IExpression> Reads
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public IEnumerable<IThrownException> ThrownExceptions
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IThrownException>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public IEnumerable<IExpression> Writes
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public IEnumerable<IExpression> Variants
    {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<IExpression>>() != null);
        throw new System.NotImplementedException();
      }
    }

    public bool IsPure
    {
      get { throw new System.NotImplementedException(); }
    }

    #region Inherited
    public IEnumerable<ILocation> Locations
    {
      get { throw new System.NotImplementedException(); }
    }
    #endregion
  }
  #endregion

  /// <summary>
  /// A condition that must be true at the start of a method, possibly bundled with an exception that will be thrown if the condition does not hold.
  /// </summary>
  public interface IPrecondition : IContractElement {

    /// <summary>
    /// The precondition is always checked at runtime, even in release builds.
    /// </summary>
    bool AlwaysCheckedAtRuntime { get; }

    /// <summary>
    /// One of three things:
    /// 1. Null. If null, the runtime behavior of the associated method is undefined when Condition is not true.
    /// 2. An exception: the value of type Exception that will be thrown if Condition is not true at the start of the method that is associated with this instance.
    /// 3. A method call to a void method. The method is called if Condition is not true at the start of the method that is associated with this instance.
    ///    The method is assumed to throw and never terminate normally, but there is no check for this.
    /// </summary>
    IExpression/*?*/ ExceptionToThrow { get; }

  }

  /// <summary>
  /// A condition that must be true at the end of a method.
  /// </summary>
  public interface IPostcondition : IContractElement {
  }

  /// <summary>
  /// An exception that can be thrown by the associated method, along with a possibly empty list of postconditions that are true when that happens.
  /// </summary>
  public partial interface IThrownException : IObjectWithLocations {

    /// <summary>
    /// The exception that can be thrown by the associated method.
    /// </summary>
    ITypeReference ExceptionType { get; }

    /// <summary>
    /// The postcondition that holds if the associated method throws this exception.
    /// </summary>
    IPostcondition Postcondition { get; }
  }

  #region IThrownException contract binding
  [ContractClass(typeof(IThrownExceptionContract))]
  public partial interface IThrownException {}

  [ContractClassFor(typeof(IThrownException))]
  abstract class IThrownExceptionContract : IThrownException
  {
    public ITypeReference ExceptionType
    {
      get {
        Contract.Ensures(Contract.Result<ITypeReference>() != null);
        throw new System.NotImplementedException(); 
      }
    }

    public IPostcondition Postcondition
    {
      get {
        Contract.Ensures(Contract.Result<IPostcondition>() != null); 
        throw new System.NotImplementedException();
      }
    }

    #region Inherited
    public IEnumerable<ILocation> Locations
    {
      get {
        throw new System.NotImplementedException();
      }
    }
    #endregion
  }
  #endregion

  /// <summary>
  /// A collection of collections of objects that augment the signature of a type with additional information
  /// that describe invariants, model variables and functions, as well as axioms.
  /// </summary>
  public interface ITypeContract : IObjectWithLocations {

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
  public interface ITypeInvariant : IContractElement {

    /// <summary>
    /// An axiom is a type invariant whose truth is assumed rather than derived. Commonly used to make statements about the meaning of contract methods.
    /// </summary>
    bool IsAxiom { get; }

    /// <summary>
    /// The name of the invariant. Used in error diagnostics. May be null.
    /// </summary>
    IName/*?*/ Name { get; }

  }

  /// <summary>
  /// An object implements this interface so that it can be notified
  /// by a contract extractor when a contract has been extracted. The
  /// notification consists of a method's residual body (which includes
  /// its operations) after the portion representing the contracts has
  /// been extracted.
  /// </summary>
  public interface IContractProviderCallback {

    /// <summary>
    /// When a contract is extracted, the extractor calls this method
    /// (if a callback object has been registered) with the part of the
    /// method body that is left over after the contract has been removed.
    /// </summary>
    void ProvideResidualMethodBody(IMethodDefinition methodDefinition, IBlockStatement/*?*/ blockStatement);

  }

  /// <summary>
  /// Since this project isn't using the 3.5 platform, just needed a quick
  /// 2-tuple.
  /// </summary>
  public struct MethodContractAndMethodBody {
    IMethodContract/*?*/ methodContract;
    IBlockStatement/*?*/ blockStatement;
    /// <summary>
    /// Constructs a pair from the arguments.
    /// </summary>
    public MethodContractAndMethodBody(IMethodContract/*?*/ methodContract, IBlockStatement/*?*/ blockStatement) {
      this.methodContract = methodContract;
      this.blockStatement = blockStatement;
    }
    /// <summary>
    /// Returns the method contract of the pair.
    /// </summary>
    public IMethodContract/*?*/  MethodContract { get { return this.methodContract; } set { this.methodContract = value; } }
    /// <summary>
    /// Returns the method body of the pair.
    /// </summary>
    public IBlockStatement/*?*/ BlockStatement { get { return this.blockStatement; } set { this.blockStatement = value; } }
  }

  /// <summary>
  /// A contract provider that, when asked for a method contract, extracts
  /// it from the method and uses a callback to notify clients with
  /// the remaining method body.
  /// </summary>
  public interface IContractExtractor : IContractProvider {

    /// <summary>
    /// When a contract is extracted from a method, all registered callbacks will be notified.
    /// </summary>
    void RegisterContractProviderCallback(IContractProviderCallback contractProviderCallback);

    /// <summary>
    /// For a client (e.g., a decompiler or a binary rewriter) that has a source method body and wants to have its
    /// contract extracted. In addition to being returned, the contract is added to the contract provider.
    /// The residual method body is returned in the second element of the pair and is *not* retained
    /// by the contract provider.
    /// REVIEW: When this method is called, should the callback be called? Should it take an extra argument
    /// that identifies the caller and the event won't be triggered for that client?
    /// </summary>
    MethodContractAndMethodBody SplitMethodBodyIntoContractAndCode(ISourceMethodBody sourceMethodBody);

  }

  /// <summary>
  /// A host that automatically attaches a contract extractor to each unit that it loads.
  /// </summary>
  public interface IContractAwareHost : IMetadataHost {

    /// <summary>
    /// If the unit with the specified identity has been loaded with this host,
    /// then it will have attached a contract extractor to that unit.
    /// This method returns that contract extractor.
    /// If the unit has not been loaded by this host, then null is returned.
    /// </summary>
    IContractExtractor/*?*/ GetContractExtractor(UnitIdentity unitIdentity);

  }

#pragma warning disable 1591

  public static class ContractDummy {
    public static IMethodContract MethodContract {
      get {
        if (ContractDummy.methodContract == null)
          ContractDummy.methodContract = new DummyMethodContract();
        return ContractDummy.methodContract;
      }
    }
    private static IMethodContract/*?*/ methodContract;
    public static ITypeContract TypeContract {
      get {
        if (ContractDummy.typeContract == null)
          ContractDummy.typeContract = new DummyTypeContract();
        return ContractDummy.typeContract;
      }
    }
    private static ITypeContract/*?*/ typeContract;
    public static IPostcondition Postcondition {
      get {
        if (ContractDummy.postcondition == null)
          ContractDummy.postcondition = new DummyPostcondition();
        return ContractDummy.postcondition;
      }
    }
    private static IPostcondition/*?*/ postcondition;
  }
  internal sealed class DummyMethodContract : IMethodContract {
    #region IMethodContract Members

    public IEnumerable<IExpression> Allocates {
      get { return Enumerable<IExpression>.Empty; }
    }

    public IEnumerable<IExpression> Frees {
      get { return Enumerable<IExpression>.Empty; }
    }

    public IEnumerable<IAddressableExpression> ModifiedVariables {
      get { return Enumerable<IAddressableExpression>.Empty; }
    }

    public bool MustInline {
      get { return false; }
    }

    public IEnumerable<IPostcondition> Postconditions {
      get { return Enumerable<IPostcondition>.Empty; }
    }

    public IEnumerable<IPrecondition> Preconditions {
      get { return Enumerable<IPrecondition>.Empty; }
    }

    public IEnumerable<IExpression> Reads {
      get { return Enumerable<IExpression>.Empty; }
    }

    public IEnumerable<IThrownException> ThrownExceptions {
      get { return Enumerable<IThrownException>.Empty; }
    }

    public IEnumerable<IExpression> Writes {
      get { return Enumerable<IExpression>.Empty; }
    }

    public IEnumerable<IExpression> Variants {
      get { return Enumerable<IExpression>.Empty; }
    }

    public bool IsPure {
      get { return false; }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }
  internal sealed class DummyTypeContract : ITypeContract {
    #region ITypeContract Members

    public IEnumerable<IFieldDefinition> ContractFields {
      get { return Enumerable<IFieldDefinition>.Empty; }
    }

    public IEnumerable<IMethodDefinition> ContractMethods {
      get { return Enumerable<IMethodDefinition>.Empty; }
    }

    public IEnumerable<ITypeInvariant> Invariants {
      get { return Enumerable<ITypeInvariant>.Empty; }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }
  internal sealed class DummyPostcondition : IPostcondition {
    #region IContractElement Members

    public IExpression Condition {
      get { return CodeDummy.Expression; }
    }

    public IExpression Description {
      get { return CodeDummy.Expression; }
    }

    public void Dispatch(ICodeAndContractVisitor visitor) {
    }

    public string OriginalSource {
      get { return null; }
    }

    public bool IsModel { get { return false; } }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

#pragma warning restore 1591

}
