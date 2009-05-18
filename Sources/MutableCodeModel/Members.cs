//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  /// <summary>
  /// A metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
  /// </summary>
  public sealed class SourceMethodBody : ISourceMethodBody {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceToILProvider">A delegate that returns an ISourceToILConverter object initialized with the given host, source location provider and contract provider.
    /// The returned object is in turn used to convert blocks of statements into lists of IL operations.</param>
    /// <param name="host">An object representing the application that is hosting this source method body. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in the block of statements to IPrimarySourceLocation objects.  May be null.</param>
    /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
    /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
    public SourceMethodBody(SourceToILConverterProvider/*?*/ sourceToILProvider, IMetadataHost host, 
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider) {
      this.Block = CodeDummy.Block;
      this.contractProvider = contractProvider;
      this.sourceToILProvider = sourceToILProvider;
      this.host = host;
      this.sourceLocationProvider = sourceLocationProvider;
    }

    /// <summary>
    /// The collection of statements making up the body.
    /// This is produced by either language parser or through decompilation of the Instructions.
    /// </summary>
    /// <value></value>
    public IBlockStatement Block {
      get { return this.block; }
      set { this.block = value; }
    }
    IBlockStatement block;

    ContractProvider/*?*/ contractProvider;
    IMetadataHost host;
    /// <summary>
    /// A delegate that returns an ISourceToILConverter object initialized with the given host, source location provider and contract provider.
    /// The returned object is in turn used to convert blocks of statements into lists of IL operations.
    /// </summary>
    SourceToILConverterProvider/*?*/ sourceToILProvider;
    ISourceLocationProvider/*?*/ sourceLocationProvider;

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
      IEnumerable<IOperation> operations;
      IEnumerable<IOperationExceptionInformation> operationExceptionInformation;
      IEnumerable<ITypeDefinition>/*?*/ privateHelperTypes = this.privateHelperTypes;
      if (this.sourceToILProvider == null) {
        localVariables = IteratorHelper.GetEmptyEnumerable<ILocalDefinition>();
        maxStack = 0;
        operations = IteratorHelper.GetEmptyEnumerable<IOperation>();
        operationExceptionInformation = IteratorHelper.GetEmptyEnumerable<IOperationExceptionInformation>();
        if (privateHelperTypes == null)
          privateHelperTypes = IteratorHelper.GetEmptyEnumerable<ITypeDefinition>();
      } else {
        ISourceToILConverter converter = this.sourceToILProvider(this.host, this.sourceLocationProvider, this.contractProvider);
        converter.ConvertToIL(this.MethodDefinition, this.Block);
        localVariables = converter.GetLocalVariables();
        maxStack = converter.MaximumStackSizeNeeded;
        operations = converter.GetOperations();
        operationExceptionInformation = converter.GetOperationExceptionInformation();
        if (privateHelperTypes == null)
          privateHelperTypes = converter.GetPrivateHelperTypes();
      }

      lock (this) {
        if (this.ilWasGenerated) return;
        this.ilWasGenerated = true;
        this.localVariables = localVariables;
        this.maxStack = maxStack;
        this.operations = operations;
        this.operationExceptionInformation = operationExceptionInformation;
        this.privateHelperTypes = privateHelperTypes;
      }
    }

    bool ilWasGenerated;

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
    /// Maximum number of elements on the evaluation stack during the execution of the method.
    /// </summary>
    /// <value></value>
    public ushort MaxStack {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.maxStack;
      }
    }
    ushort maxStack;

    /// <summary>
    /// Definition of method whose body this is.
    /// If this is body for Event/Property this will hold the corresponding adder/remover/setter or getter
    /// </summary>
    /// <value></value>
    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
      set { this.methodDefinition = value; }
    }
    IMethodDefinition methodDefinition;

    /// <summary>
    /// A list CLR IL operations that implement this method body.
    /// </summary>
    /// <value></value>
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
    /// <value></value>
    public IEnumerable<ITypeDefinition> PrivateHelperTypes {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.privateHelperTypes; 
      }
      set { this.privateHelperTypes = value; }
    }
    IEnumerable<ITypeDefinition>/*?*/ privateHelperTypes;

  }

}