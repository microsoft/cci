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
using System.Text;
using Microsoft.Cci.MutableContracts;
using System.Diagnostics.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
  /// </summary>
  public class SourceMethodBody : ISourceMethodBody {

    /// <summary>
    /// Allocates an object that provides a metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this source method body. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in the block of statements to IPrimarySourceLocation objects.  May be null.</param>
    /// <param name="localScopeProvider"></param>
    /// <param name="iteratorLocalCount">A map that indicates how many iterator locals are present in a given block. Only useful for generated MoveNext methods. May be null.</param>
    public SourceMethodBody(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider = null, ILocalScopeProvider/*?*/ localScopeProvider = null, IDictionary<IBlockStatement, uint>/*?*/ iteratorLocalCount = null) {
      this.host = host;
      this.sourceLocationProvider = sourceLocationProvider;
      this.localScopeProvider = localScopeProvider;
      this.iteratorLocalCount = iteratorLocalCount;
    }

    /// <summary>
    /// The collection of statements making up the body.
    /// This is produced by either language parser or through decompilation of the Instructions.
    /// </summary>
    /// <value></value>
    public IBlockStatement Block {
      get {
        Contract.Ensures(Contract.Result<IBlockStatement>() != null);
        if (this.block == null)
          this.block = this.GetBlock();
        return this.block;
      }
      set {
        Contract.Requires(value != null);
        this.block = value; 
        this.ilWasGenerated = false; 
      }
    }
    IBlockStatement/*?*/ block;

    /// <summary>
    /// If no value has been provided for the Block property, make one.
    /// </summary>
    /// <returns></returns>
    protected virtual IBlockStatement GetBlock() {
      return CodeDummy.Block;
    }

    IMetadataHost host;
    ISourceLocationProvider/*?*/ sourceLocationProvider;
    ILocalScopeProvider/*?*/ localScopeProvider;

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    private void GenerateIL() {
      IEnumerable<ILocalDefinition> localVariables;
      ushort maxStack;
      IEnumerable<ILocalScope> iteratorScopes;
      IEnumerable<ILocalScope> localScopes;
      IEnumerable<INamespaceScope> namespaceScopes;
      IEnumerable<IOperation> operations;
      IEnumerable<IOperationExceptionInformation> operationExceptionInformation;
      List<ITypeDefinition>/*?*/ privateHelperTypes = this.privateHelperTypes;
      ISynchronizationInformation/*?*/ synchronizationInformation;
      uint size;

      var isNormalized = this.isNormalized;
      NormalizationChecker checker = null;
      if (!isNormalized) {
        //Assuming that most methods are not iterators and do not contain anonymous delegates and do not contain foreach statements,
        //it is worth our while to check if this is really the case.
        checker = new NormalizationChecker();
        checker.TraverseChildren(this.Block);
        isNormalized = !checker.foundAnonymousDelegate && !checker.foundYield && !checker.foundForEach;
      }

      if (isNormalized) {
        IMethodDefinition/*?*/ asyncMethod = null;
        if (this.localScopeProvider != null) {
          var asyncInfo = this.localScopeProvider.GetSynchronizationInformation(this);
          if (asyncInfo != null) asyncMethod = asyncInfo.AsyncMethod;
        }
        var converter = new CodeModelToILConverter(this.host, this.MethodDefinition, this.sourceLocationProvider, asyncMethod, this.iteratorLocalCount);
        converter.TrackExpressionSourceLocations = this.trackExpressionSourceLocations;
        converter.ConvertToIL(this.Block);
        iteratorScopes = converter.GetIteratorScopes();
        localScopes = converter.GetLocalScopes();
        localVariables = converter.GetLocalVariables();
        maxStack = converter.MaximumStackSizeNeeded;
        size = converter.GetBodySize();
        namespaceScopes = converter.GetNamespaceScopes();
        operations = converter.GetOperations();
        operationExceptionInformation = converter.GetOperationExceptionInformation();
        synchronizationInformation = converter.GetSynchronizationInformation();
      } else {
        //This object might already be immutable and we are just doing delayed initialization, so make a copy of this.Block.
        var mutableBlock = new CodeDeepCopier(this.host, this.sourceLocationProvider).Copy(this.Block);
        if (checker.foundAnonymousDelegate) {
          var remover = new AnonymousDelegateRemover(this.host, this.sourceLocationProvider);
          remover.RemoveAnonymousDelegates(this.MethodDefinition, mutableBlock);
          privateHelperTypes = remover.closureClasses;
        }
        if (checker.foundForEach) {
          var remover = new ForEachRemover(this.host, this.sourceLocationProvider);
          remover.RemoveForEachStatements(this.MethodDefinition, mutableBlock);
        }
        var normalizer = new MethodBodyNormalizer(this.host, this.sourceLocationProvider);
        var normalizedBody = (SourceMethodBody)normalizer.GetNormalizedSourceMethodBodyFor(this.MethodDefinition, mutableBlock);
        normalizedBody.isNormalized = true;
        iteratorScopes = normalizedBody.IteratorScopes;
        localScopes = normalizedBody.LocalScopes;
        localVariables = normalizedBody.LocalVariables;
        maxStack = normalizedBody.MaxStack;
        size = normalizedBody.Size;
        namespaceScopes = normalizedBody.NamespaceScopes;
        operations = normalizedBody.Operations;
        operationExceptionInformation = normalizedBody.OperationExceptionInformation;
        synchronizationInformation = normalizedBody.SynchronizationInformation;
        if (privateHelperTypes == null)
          privateHelperTypes = normalizedBody.PrivateHelperTypes;
        else //this can happen when this source method body has already been partially normalized, for instance by the removal of yield statements.
          privateHelperTypes.AddRange(normalizedBody.PrivateHelperTypes);
      }

      lock (this) {
        if (this.ilWasGenerated) return;
        this.ilWasGenerated = true;
        this.iteratorScopes = iteratorScopes;
        this.localScopes = localScopes;
        this.localVariables = localVariables;
        this.maxStack = maxStack;
        this.namespaceScopes = namespaceScopes;
        this.operations = operations;
        this.operationExceptionInformation = operationExceptionInformation;
        this.synchronizationInformation = synchronizationInformation;
        this.privateHelperTypes = privateHelperTypes;
        this.size = size;
      }
    }

    bool ilWasGenerated;

    /// <summary>
    /// True if the method body does not contain any anonymous delegates or yield statements.
    /// This property is not computed, but is set by the constructor of this body.
    /// </summary>
    public bool IsNormalized {
      get { return this.isNormalized; }
      set { this.isNormalized = value; }
    }
    bool isNormalized;

    IDictionary<IBlockStatement, uint>/*?*/ iteratorLocalCount;

    /// <summary>
    /// True if the method body is an iterator.
    /// </summary>
    public bool IsIterator {
      get {
        return this.iteratorLocalCount != null;
      }
    }

    /// <summary>
    /// Returns zero or more local (block) scopes, each defining an IL range in which an iterator local is defined.
    /// The scopes are returned by the MoveNext method of the object returned by the iterator method.
    /// The index of the scope corresponds to the index of the local. Specifically local scope i corresponds
    /// to the local stored in field &lt;localName&gt;x_i of the class used to store the local values in between
    /// calls to MoveNext.
    /// </summary>
    public IEnumerable<ILocalScope> IteratorScopes {
      get {
        Contract.Ensures(Contract.Result<IEnumerable<ILocalScope>>() != null);
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.iteratorScopes??Enumerable<ILocalScope>.Empty;
      }
    }
    IEnumerable<ILocalScope> iteratorScopes;

    /// <summary>
    /// Returns zero or more local (block) scopes into which the CLR IL operations in the given method body is organized.
    /// </summary>
    public IEnumerable<ILocalScope> LocalScopes {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.localScopes;
      }
    }
    IEnumerable<ILocalScope> localScopes;

    /// <summary>
    /// True if the locals are initialized by zeroeing the stack upon method entry.
    /// </summary>
    /// <value></value>
    public bool LocalsAreZeroed {
      get { return this.localsAreZeroed; }
      set { this.localsAreZeroed = value; }
    }
    bool localsAreZeroed;

    /// <summary>
    /// The local variables of the method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocalDefinition> LocalVariables {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.localVariables;
      }
    }
    IEnumerable<ILocalDefinition>/*?*/ localVariables;

    /// <summary>
    /// The maximum number of elements on the evaluation stack during the execution of the method.
    /// </summary>
    public ushort MaxStack {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.maxStack;
      }
    }
    ushort maxStack;

    /// <summary>
    /// The definition of the method whose body this is.
    /// If this is the body of an event or property accessor, this will hold the corresponding adder/remover/setter or getter method.
    /// </summary>
    /// <remarks>The setter should only be called once, to complete the two phase initialization of this object.</remarks>
    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
      set { this.methodDefinition = value; }
    }
    IMethodDefinition methodDefinition;

    /// <summary>
    /// Returns zero or more namespace scopes into which the namespace type containing the given method body has been nested.
    /// These scopes determine how simple names are looked up inside the method body. There is a separate scope for each dotted
    /// component in the namespace type name. For istance namespace type x.y.z will have two namespace scopes, the first is for the x and the second
    /// is for the y.
    /// </summary>
    public IEnumerable<INamespaceScope> NamespaceScopes {
      get { return this.namespaceScopes; }
    }
    IEnumerable<INamespaceScope> namespaceScopes;

    /// <summary>
    /// A list CLR IL operations that implement this method body.
    /// </summary>
    public IEnumerable<IOperation> Operations {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operations;
      }
    }
    IEnumerable<IOperation>/*?*/ operations;

    /// <summary>
    /// A list exception data within the method body IL.
    /// </summary>
    /// <value></value>
    public IEnumerable<IOperationExceptionInformation> OperationExceptionInformation {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operationExceptionInformation;
      }
    }
    IEnumerable<IOperationExceptionInformation>/*?*/ operationExceptionInformation;

    /// <summary>
    /// Any types that are implicitly defined in order to implement the body semantics.
    /// In case of AST to instructions conversion this lists the types produced.
    /// In case of instructions to AST decompilation this should ideally be list of all types
    /// which are local to method.
    /// </summary>
    public List<ITypeDefinition>/*?*/ PrivateHelperTypes {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.privateHelperTypes;
      }
      set { this.privateHelperTypes = value; }
    }
    List<ITypeDefinition>/*?*/ privateHelperTypes;

    /// <summary>
    /// The ISourceLocationProvider instance that is used when then body is compiled to IL. May be null.
    /// </summary>
    public ISourceLocationProvider/*?*/ SourceLocationProvider {
      get { return this.sourceLocationProvider; }
    }

    /// <summary>
    /// If true, the generated IL keeps track of the source locations of expressions, not just statements.
    /// </summary>
    public bool TrackExpressionSourceLocations {
      get { return this.trackExpressionSourceLocations; }
      set { this.trackExpressionSourceLocations = value; }
    }
    private bool trackExpressionSourceLocations;

    /// <summary>
    /// The size in bytes of the method body when serialized.
    /// </summary>
    public uint Size {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.size; 
      }
    }
    uint size;

    /// <summary>
    /// </summary>
    public ISynchronizationInformation/*?*/ SynchronizationInformation {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.synchronizationInformation;
      }
    }
    ISynchronizationInformation/*?*/ synchronizationInformation;

    #region IMethodBody Members

    IEnumerable<ITypeDefinition> IMethodBody.PrivateHelperTypes {
      get
      {
        return this.PrivateHelperTypesImplementation; 
      }
    }

    /// <summary>
    /// Implementation of IMethodBody.PrivateHelperTypes. Exposed here so that sub types can implement
    /// IMethodBody interface, while calling the base implementation if necessary
    /// </summary>
    protected IEnumerable<ITypeDefinition> PrivateHelperTypesImplementation
    {
      get
      {
        if (this.PrivateHelperTypes == null)
          return Enumerable<ITypeDefinition>.Empty;
        else
          return this.PrivateHelperTypes.AsReadOnly();
      }
    }

    #endregion

  }

}
