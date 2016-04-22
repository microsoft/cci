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
  /// An object that can visit a source method body Block and produce the corresponding IL.
  /// </summary>
  public interface ISourceToILConverter {

    /// <summary>
    /// Traverses the given block of statements in the context of the given method to produce a list of
    /// IL operations, exception information blocks (the locations of handlers, filters and finallies) and any private helper
    /// types (for example closure classes) that represent the semantics of the given block of statements.
    /// The results of the traversal can be retrieved via the GetOperations, GetOperationExceptionInformation
    /// and GetPrivateHelperTypes methods.
    /// It is assumed that any implementation of this interface will already have a reference to the method the body is
    /// contained in before this method is called.
    /// </summary>
    /// <param name="body">A block of statements that are to be converted to IL.</param>
    void ConvertToIL(IBlockStatement body);

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

}