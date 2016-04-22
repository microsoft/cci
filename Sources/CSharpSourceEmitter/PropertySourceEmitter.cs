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
    public override void TraverseChildren(IPropertyDefinition propertyDefinition) {

      PrintAttributes(propertyDefinition);

      bool isIndexer = false;
      if (IteratorHelper.EnumerableIsNotEmpty(propertyDefinition.Parameters)) {
        // We have an indexer.  Note that this could still be an explicit interface implementation.
        // If it's got a name other than 'Item', we need an attribute to rename it.
        // Note that there will usually be a DefaultMemberAttribute on the class with the name
        // but the attribute doesn't always exist (eg. if it's an explicit interaface impl)
        isIndexer = true;
        string id = propertyDefinition.Name.Value;
        string simpleId = id.Substring(id.LastIndexOf('.') + 1);  // excludes any interface type
        if (simpleId != "Item") {
          PrintPseudoCustomAttribute(propertyDefinition, "System.Runtime.CompilerServices.IndexerName", QuoteString(simpleId), true, null);
        }
      }

      PrintToken(CSharpToken.Indent);

      IMethodDefinition propMeth = propertyDefinition.Getter == null ?
        propertyDefinition.Setter.ResolvedMethod :
        propertyDefinition.Getter.ResolvedMethod;
      if (!propertyDefinition.ContainingTypeDefinition.IsInterface && 
        IteratorHelper.EnumerableIsEmpty(MemberHelper.GetExplicitlyOverriddenMethods(propMeth)))
          PrintPropertyDefinitionVisibility(propertyDefinition);
      PrintPropertyDefinitionModifiers(propertyDefinition);
      PrintPropertyDefinitionReturnType(propertyDefinition);
      PrintToken(CSharpToken.Space);

      if (isIndexer) {
        // Indexers are always identified with a 'this' keyword, but might have an interface prefix
        string id = propertyDefinition.Name.Value;
        int lastDot = id.LastIndexOf('.');
        if (lastDot != -1)
          sourceEmitterOutput.Write(id.Substring(0, lastDot +1));
        PrintToken(CSharpToken.This);
        PrintToken(CSharpToken.LeftSquareBracket);
        bool fFirstParameter = true;
        var parms = propertyDefinition.Parameters;
        if (propertyDefinition.Getter != null)
          parms = propertyDefinition.Getter.ResolvedMethod.Parameters;  // more likely to have names
        else if (propertyDefinition.Setter != null) {
          // Use the setter's names except for the final 'value' parameter
          var l = new List<IParameterDefinition>(propertyDefinition.Setter.ResolvedMethod.Parameters);
          l.RemoveAt(l.Count - 1);
          parms = l;
        }
        foreach (IParameterDefinition parameterDefinition in parms) {
          if (!fFirstParameter)
            PrintParameterListDelimiter();
          this.Traverse(parameterDefinition);
          fFirstParameter = false;
        }
        PrintToken(CSharpToken.RightSquareBracket);
      } else {
        PrintPropertyDefinitionName(propertyDefinition);
      }
      PrintToken(CSharpToken.LeftCurly);
      if (propertyDefinition.Getter != null) {
        PrintToken(CSharpToken.Indent);
        var getMeth = propertyDefinition.Getter.ResolvedMethod;
        if (getMeth.Visibility != propertyDefinition.Visibility)
          PrintTypeMemberVisibility(getMeth.Visibility);
        PrintToken(CSharpToken.Get);
        if (getMeth.IsAbstract || getMeth.IsExternal)
          PrintToken(CSharpToken.Semicolon);
        else
          Traverse(getMeth.Body);
      }
      if (propertyDefinition.Setter != null) {
        PrintToken(CSharpToken.Indent);
        var setMeth = propertyDefinition.Setter.ResolvedMethod;
        if (setMeth.Visibility != propertyDefinition.Visibility)
          PrintTypeMemberVisibility(setMeth.Visibility);
        PrintToken(CSharpToken.Set);
        if (setMeth.IsAbstract || setMeth.IsExternal)
          PrintToken(CSharpToken.Semicolon);
        else
          Traverse(setMeth.Body);
      }
      PrintToken(CSharpToken.RightCurly);
    }

    public virtual void PrintPropertyDefinitionVisibility(IPropertyDefinition propertyDefinition) {
      Contract.Requires(propertyDefinition != null);

      PrintTypeMemberVisibility(propertyDefinition.Visibility);
    }

    public virtual void PrintPropertyDefinitionReturnType(IPropertyDefinition propertyDefinition) {
      Contract.Requires(propertyDefinition != null);

      PrintTypeReference(propertyDefinition.Type);
    }

    public virtual void PrintPropertyDefinitionName(IPropertyDefinition propertyDefinition) {
      Contract.Requires(propertyDefinition != null);

      PrintIdentifier(propertyDefinition.Name);
    }

    public virtual void PrintPropertyDefinitionModifiers(IPropertyDefinition propertyDefinition) {
      Contract.Requires(propertyDefinition != null);

      PrintMethodDefinitionModifiers(propertyDefinition.Getter == null ?
        propertyDefinition.Setter.ResolvedMethod :
        propertyDefinition.Getter.ResolvedMethod);
    }
  }
}
