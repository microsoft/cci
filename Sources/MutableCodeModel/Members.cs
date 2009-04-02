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

    public SourceMethodBody(SourceToILConverterProvider/*?*/ sourceToILProvider, IMetadataHost host, 
      ISourceLocationProvider/*?*/ sourceLocationProvider, ContractProvider/*?*/ contractProvider) {
      this.Block = CodeDummy.Block;
      this.contractProvider = contractProvider;
      this.sourceToILProvider = sourceToILProvider;
      this.host = host;
      this.sourceLocationProvider = sourceLocationProvider;
    }

    public IBlockStatement Block {
      get { return this.block; }
      set { this.block = value; }
    }
    IBlockStatement block;

    ContractProvider/*?*/ contractProvider;
    IMetadataHost host;
    SourceToILConverterProvider/*?*/ sourceToILProvider;
    ISourceLocationProvider/*?*/ sourceLocationProvider;

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

    public bool LocalsAreZeroed {
      get { return this.localsAreZeroed; }
      set { this.localsAreZeroed = value; }
    }
    bool localsAreZeroed;

    public IEnumerable<ILocalDefinition> LocalVariables {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.localVariables;
      }
    }
    IEnumerable<ILocalDefinition>/*?*/ localVariables;

    public ushort MaxStack {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.maxStack;
      }
    }
    ushort maxStack;

    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
      set { this.methodDefinition = value; }
    }
    IMethodDefinition methodDefinition;

    public IEnumerable<IOperation> Operations {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operations;
      }
    }
    IEnumerable<IOperation>/*?*/ operations;

    public IEnumerable<IOperationExceptionInformation> OperationExceptionInformation {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operationExceptionInformation;
      }
    }
    IEnumerable<IOperationExceptionInformation>/*?*/ operationExceptionInformation;

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