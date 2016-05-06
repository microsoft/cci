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
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
    public virtual void PrintTypeDefinition(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);

      if (typeDefinition.IsDelegate) {
        PrintDelegateDefinition(typeDefinition);
        return;
      }

      if (((INamedEntity)typeDefinition).Name.Value.Contains("PrivateImplementationDetails")) return;

      PrintTypeDefinitionAttributes(typeDefinition);
      PrintToken(CSharpToken.Indent);
      PrintTypeDefinitionVisibility(typeDefinition);
      PrintTypeDefinitionModifiers(typeDefinition);
      PrintTypeDefinitionKeywordType(typeDefinition);
      PrintTypeDefinitionName(typeDefinition);
      if (typeDefinition.IsGeneric) {
        this.Traverse(typeDefinition.GenericParameters);
      }
      PrintTypeDefinitionBaseTypesAndInterfaces(typeDefinition);

      PrintTypeDefinitionLeftCurly(typeDefinition);

      // Get the members in metadata order for each type
      // Note that it's important to preserve the metadata order here (eg. sequential layout fields,
      // methods in COMImport types, etc.).
      var members = new List<ITypeDefinitionMember>();
      foreach (var m in typeDefinition.Methods) members.Add(m);
      foreach (var m in typeDefinition.Events) members.Add(m);
      foreach (var m in typeDefinition.Properties) members.Add(m);
      foreach (var m in typeDefinition.Fields) members.Add(m);
      foreach (var m in typeDefinition.NestedTypes) members.Add(m);
      Traverse(members);

      PrintTypeDefinitionRightCurly(typeDefinition);
    }

    public virtual void PrintDelegateDefinition(ITypeDefinition delegateDefinition) {
      Contract.Requires(delegateDefinition != null);

      PrintTypeDefinitionAttributes(delegateDefinition);
      PrintToken(CSharpToken.Indent);

      IMethodDefinition invokeMethod = null;
      foreach (var invokeMember in delegateDefinition.GetMatchingMembers(m => m.Name.Value == "Invoke")) {
        IMethodDefinition idef = invokeMember as IMethodDefinition;
        if (idef != null) { invokeMethod = idef; break; }
      }

      PrintTypeDefinitionVisibility(delegateDefinition);
      if (IsMethodUnsafe(invokeMethod))
        PrintKeywordUnsafe();

      PrintKeywordDelegate();
      PrintMethodDefinitionReturnType(invokeMethod);
      PrintToken(CSharpToken.Space);
      PrintTypeDefinitionName(delegateDefinition);
      if (delegateDefinition.GenericParameterCount > 0) {
        this.Traverse(delegateDefinition.GenericParameters);
      }
      if (invokeMethod != null)
        Traverse(invokeMethod.Parameters);

      PrintToken(CSharpToken.Semicolon);
    }

    public virtual void PrintTypeDefinitionAttributes(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);

      foreach (var attribute in SortAttributes(typeDefinition.Attributes)) {
        // Skip DefaultMemberAttribute on a class that has an indexer
        var at = Utils.GetAttributeType(attribute);
        if (at == SpecialAttribute.DefaultMemberAttribute &&
          IteratorHelper.Any(typeDefinition.Properties, p => IteratorHelper.EnumerableIsNotEmpty(p.Parameters)))
          continue;
        // Skip ExtensionAttribute
        if (at == SpecialAttribute.Extension)
          continue;

        PrintAttribute(typeDefinition, attribute, true, null);
      }

      if (typeDefinition.Layout != LayoutKind.Auto) {
        PrintPseudoCustomAttribute(typeDefinition,
          "System.Runtime.InteropServices.StructLayout",
          String.Format("System.Runtime.InteropServices.LayoutKind.{0}", typeDefinition.Layout.ToString()),
          true, null);
      }
    }

    public virtual void PrintTypeDefinitionVisibility(ITypeDefinition typeDefinition) {
      if (typeDefinition is INamespaceTypeDefinition) {
        INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
        if (namespaceTypeDefinition.IsPublic)
          PrintKeywordPublic();
      } else if (typeDefinition is INestedTypeDefinition) {
        INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
        PrintTypeMemberVisibility(nestedTypeDefinition.Visibility);
      }
    }

    public virtual void PrintTypeDefinitionModifiers(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);

      // If it's abstract and sealed and has no ctors, then it's a static class
      if (typeDefinition.IsStatic) {
        PrintKeywordStatic();
      }
      else {
        if (typeDefinition.IsAbstract && !typeDefinition.IsInterface)
          PrintKeywordAbstract();

        if (typeDefinition.IsSealed && !typeDefinition.IsValueType)
          PrintKeywordSealed();
      }
    }

    public virtual void PrintTypeDefinitionKeywordType(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);

      if (typeDefinition.IsInterface)
        PrintKeywordInterface();
      else if (typeDefinition.IsEnum)
        PrintKeywordEnum();
      else if (typeDefinition.IsValueType)
        PrintKeywordStruct();
      else
        PrintKeywordClass();
    }

    public virtual void PrintKeywordClass() {
      PrintToken(CSharpToken.Class);
    }

    public virtual void PrintKeywordInterface() {
      PrintToken(CSharpToken.Interface);
    }

    public virtual void PrintKeywordStruct() {
      PrintToken(CSharpToken.Struct);
    }

    public virtual void PrintKeywordEnum() {
      PrintToken(CSharpToken.Enum);
    }

    public virtual void PrintKeywordDelegate() {
      PrintToken(CSharpToken.Delegate);
    }

    public virtual void PrintTypeDefinitionName(ITypeDefinition typeDefinition) {
      INamespaceTypeDefinition namespaceTypeDefinition = typeDefinition as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) {
        PrintIdentifier(namespaceTypeDefinition.Name);
        return;
      }

      INestedTypeDefinition nestedTypeDefinition = typeDefinition as INestedTypeDefinition;
      if (nestedTypeDefinition != null) {
        PrintIdentifier(nestedTypeDefinition.Name);
        return;
      }

      INamedEntity namedEntity = typeDefinition as INamedEntity;
      if (namedEntity != null) {
        PrintIdentifier(namedEntity.Name);
      } else {
        sourceEmitterOutput.Write(typeDefinition.ToString());
      }
    }

    public virtual void PrintTypeDefinitionBaseTypesAndInterfaces(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);

      PrintBaseTypesAndInterfacesList(typeDefinition);
    }

    public virtual void PrintTypeDefinitionLeftCurly(ITypeDefinition typeDefinition) {
      PrintToken(CSharpToken.LeftCurly);
    }

    public virtual void PrintTypeDefinitionRightCurly(ITypeDefinition typeDefinition) {
      PrintToken(CSharpToken.RightCurly);
    }

    public new void Traverse(IEnumerable<ITypeDefinitionMember> typeMembers) {
      Contract.Requires(typeMembers != null);

      if (IteratorHelper.EnumerableIsNotEmpty(typeMembers) && IteratorHelper.First(typeMembers).ContainingTypeDefinition.IsEnum) {
        // Enums don't get intervening blank lines
        foreach (var member in typeMembers)
          Traverse(member);
      } else {
        // Ensure there's exactly one blank line between each non-empty member
        VisitWithInterveningBlankLines(typeMembers, m => Traverse(m));
      }
    }
  }
}
