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
    /// 
    /// </summary>
    /// <param name="method"></param>
    /// <param name="body"></param>
    void ConvertToIL(IMethodDefinition method, IBlockStatement body);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerable<ILocalDefinition> GetLocalVariables();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerable<IOperation> GetOperations();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerable<IOperationExceptionInformation> GetOperationExceptionInformation();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    IEnumerable<ITypeDefinition> GetPrivateHelperTypes();

    /// <summary>
    /// 
    /// </summary>
    ushort MaximumStackSizeNeeded { get; }

  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="host"></param>
  /// <param name="sourceLocationProvider"></param>
  /// <param name="contractProvider"></param>
  /// <returns></returns>
  public delegate ISourceToILConverter SourceToILConverterProvider(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider);

}