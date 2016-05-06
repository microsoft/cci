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

//^ using Microsoft.Contracts;

//TODO: get rid of these interfaces

namespace Microsoft.Cci.Ast {

  /// <summary>
  /// Implemented by constructs that can be nested inside namespaces, such as types, nested namespaces, alias declarations, and so on.
  /// </summary>
  public interface INamespaceDeclarationMember : IContainerMember<NamespaceDeclaration>, ISourceItem, IErrorCheckable {

    /// <summary>
    /// The namespace declaration in which this nested namespace declaration is nested.
    /// </summary>
    NamespaceDeclaration ContainingNamespaceDeclaration {
      get;
      // ^ ensures exists{INamespaceDeclarationMember member in result.Members; member == this};
    }

    ///// <summary>
    ///// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    ///// of the object implementing INamespaceDeclarationMember. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    ///// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    ///// </summary>
    //void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// Makes a shallow copy of this member that can be added to the member list of the given target namespace declaration.
    /// The shallow copy may share child objects with this instance, but should never expose such child objects except through
    /// wrappers (or shallow copies made on demand). If this instance is already a member of the target namespace declaration it
    /// returns itself. 
    /// </summary>
    INamespaceDeclarationMember MakeShallowCopyFor(NamespaceDeclaration targetNamespaceDeclaration);
    //^ requires targetNamespaceDeclaration.GetType() == this.ContainingNamespaceDeclaration.GetType();
    //^ ensures result.GetType() == this.GetType();
    //^ ensures result.ContainingNamespaceDeclaration == targetNamespaceDeclaration;

    /// <summary>
    /// The name of the member. For example the alias of an alias declaration, the name of nested namespace and so on. 
    /// Can be the empty name, for example if the construct is a namespace import.
    /// </summary>
    new NameDeclaration Name {
      get;
    }

  }

}

