//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {
  /// <summary>
  /// The parameters and return type that makes up a method or property signature.
  /// This interface models the source representation of a signature.
  /// </summary>
  public interface ISignatureDeclaration : ISourceItem {
    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    IEnumerable<ParameterDeclaration> Parameters { get; }

    /// <summary>
    /// An expression that denotes the return type of the method or type of the property.
    /// </summary>
    TypeExpression Type { get; }

    /// <summary>
    /// The symbol table object that represents the metadata for this signature.
    /// </summary>
    ISignature SignatureDefinition { get; }
  }

  /// <summary>
  /// A member of a type declaration, such as a field or a method.
  /// This interface models the source representation of a type member.
  /// </summary>
  public interface ITypeDeclarationMember : IContainerMember<TypeDeclaration>, IDeclaration, IErrorCheckable {

    /// <summary>
    /// The type declaration that contains this member.
    /// </summary>
    TypeDeclaration ContainingTypeDeclaration { get;  }

    ///// <summary>
    ///// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    ///// of the object implementing ITypeDeclarationMember. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    ///// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    ///// </summary>
    //void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// Returns the visibility that applies by default to this member if no visibility was supplied in the source code.
    /// </summary>
    TypeMemberVisibility GetDefaultVisibility();

    /// <summary>
    /// Indicates that this member is intended to hide the name of an inherited member.
    /// </summary>
    bool IsNew { get;  }

    /// <summary>
    /// True if the member exposes an unsafe type, such as a pointer.
    /// </summary>
    bool IsUnsafe { get; }

    /// <summary>
    /// The name of the member. 
    /// </summary>
    new NameDeclaration Name {
      get;
    }

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target type declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target type declaration it
    /// returns itself.
    /// </summary>
    ITypeDeclarationMember MakeShallowCopyFor(TypeDeclaration targetTypeDeclaration);
    //^ requires targetTypeDeclaration.GetType() == this.ContainingTypeDeclaration.GetType();
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingTypeDeclaration == targetTypeDeclaration;

    /// <summary>
    /// The symbol table object that represents the metadata for this member. May be null if this member does not generate metadata.
    /// </summary>
    ITypeDefinitionMember/*?*/ TypeDefinitionMember { get; }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    TypeMemberVisibility Visibility { get; }
  }

}