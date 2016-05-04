// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//^ using Microsoft.Contracts;

//TODO: get rid of these interfaces

namespace Microsoft.Cci.Ast {
  /// <summary>
  /// Corresponds to a source construct that declares a class.
  /// </summary>
  public interface IClassDeclaration {
    /// <summary>
    /// A static class can not have instance members. A static class is sealed.
    /// </summary>
    bool IsStatic { get; }
  }

  /// <summary>
  /// Corresponds to a source construct that declares a delegate.
  /// </summary>
  public interface IDelegateDeclaration {
    /// <summary>
    /// The signature of the Invoke method.
    /// </summary>
    ISignatureDeclaration Signature { get; }

  }

  /// <summary>
  /// Corresponds to a source construct that declares an enumerated scalar type.
  /// </summary>
  public interface IEnumDeclaration {

    /// <summary>
    /// The primitive integral type that will be used to represent the values of enumeration. May be null.
    /// </summary>
    TypeExpression/*?*/ UnderlyingType { get; }
  }

  /// <summary>
  /// Corresponds to a source construct that declares an interface.
  /// </summary>
  public interface IInterfaceDeclaration {
  }

  /// <summary>
  /// Corresponds to a source construct that declares a value type (struct).
  /// </summary>
  public interface IStructDeclaration  {
  }

}