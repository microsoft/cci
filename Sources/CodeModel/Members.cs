//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Contracts;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci {

  /// <summary>
  /// A metadata (IL) representation along with a source level representation of the body of a method or of a property/event accessor.
  /// </summary>
  public interface ISourceMethodBody : IMethodBody {

    /// <summary>
    /// The collection of statements making up the body.
    /// This is produced by either language parser or through decompilation of the Instructions.
    /// </summary>
    IBlockStatement Block {
      get;
      //^ requires !this.MethodDefinition.IsAbstract;
    }
  }

  /// <summary>
  /// A way to have a dynamically registered component (e.g., the decompiler) be able to provide a source-level method body
  /// </summary>
  public delegate ISourceMethodBody SourceMethodBodyProvider(IMethodBody methodBody);

  /// <summary>
  /// An object that can visit a source method body Block and produce the corresponding IL.
  /// </summary>
  public interface ISourceToILConverter {

    /// <summary>
    /// Traverses the given block of statements in the context of the given method to produce a list of
    /// IL operations, exception information blocks (the locations of handlers, filters and finallies) and any private helper
    /// types (for example closure classes) that represent the semantics of the given block of statements.
    /// The results of the traversal can be retrieved via the GetOperations, GetOperationExceptionInformation
    /// and GetPrivateHelperTypes methods.
    /// </summary>
    /// <param name="method">A method that provides the context for a block of statments that are to be converted to IL.</param>
    /// <param name="body">A block of statements that are to be converted to IL.</param>
    void ConvertToIL(IMethodDefinition method, IBlockStatement body);

    /// <summary>
    /// Returns all of the local variables (including compiler generated temporary variables) that are local to the block
    /// of statements translated by this converter.
    /// </summary>
    IEnumerable<ILocalDefinition> GetLocalVariables();

    /// <summary>
    /// Returns the IL operations that correspond to the statements that have been converted to IL by this converter.
    /// </summary>
    IEnumerable<IOperation> GetOperations();

    /// <summary>
    /// Returns zero or more exception exception information blocks (information about handlers, filters and finally blocks)
    /// that correspond to try-catch-finally constructs that appear in the statements that have been converted to IL by this converter.
    /// </summary>
    IEnumerable<IOperationExceptionInformation> GetOperationExceptionInformation();

    /// <summary>
    /// Returns zero or more types that are used to keep track of information needed to implement
    /// the statements that have been converted to IL by this converter. For example, any closure classes
    /// needed to compile anonymous delegate expressions (lambdas) will be returned by this method.
    /// </summary>
    IEnumerable<ITypeDefinition> GetPrivateHelperTypes();

    /// <summary>
    /// The maximum number of stack slots that will be needed by an interpreter of the IL produced by this converter.
    /// </summary>
    ushort MaximumStackSizeNeeded { get; }

  }

  /// <summary>
  /// A delegate that returns an ISourceToILConverter object initialized with the given host, source location provider and contract provider.
  /// The returned object is in turn used to convert blocks of statements into lists of IL operations.
  /// </summary>
  /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
  /// objects and services such as the shared name table and the table for interning references.</param>
  /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
  /// <param name="contractProvider">An object that associates contracts, such as preconditions and postconditions, with methods, types and loops.
  /// IL to check this contracts will be generated along with IL to evaluate the block of statements. May be null.</param>
  public delegate ISourceToILConverter SourceToILConverterProvider(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider);

}