//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : BaseCodeTraverser, ICSharpSourceEmitter {
    public override void Visit(IPropertyDefinition propertyDefinition) {

      PrintAttributes(propertyDefinition.Attributes);
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

      if (IteratorHelper.EnumerableIsNotEmpty(propertyDefinition.Parameters)) {
        // We have an indexer.  Note that this could still be an explicit interface implementation
        // Replace the name "Item" with "this".  We could check for a DefaultMemberAttribute to confirm
        // the name is "Item", but the attribute doesn't always exist (eg. if it's an explicit interaface impl)
        string id = propertyDefinition.Name.Value;
        string item = "Item";
        if (id.EndsWith(item))
          id = id.Substring(0, id.Length - item.Length) + "this";
        sourceEmitterOutput.Write(id);
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
          this.Visit(parameterDefinition);
          fFirstParameter = false;
        }
        PrintToken(CSharpToken.RightSquareBracket);
      } else {
        PrintPropertyDefinitionName(propertyDefinition);
      }
      PrintToken(CSharpToken.NewLine);
      PrintToken(CSharpToken.LeftCurly);
      if (propertyDefinition.Getter != null) {
        PrintToken(CSharpToken.Indent);
        PrintToken(CSharpToken.Get);
        var getMeth = propertyDefinition.Getter.ResolvedMethod;
        if (getMeth.IsAbstract)
          PrintToken(CSharpToken.Semicolon);
        else
          Visit(getMeth.Body);
      }
      if (propertyDefinition.Setter != null) {
        PrintToken(CSharpToken.Indent);
        PrintToken(CSharpToken.Set);
        var setMeth = propertyDefinition.Setter.ResolvedMethod;
        if (setMeth.IsAbstract)
          PrintToken(CSharpToken.Semicolon);
        else
          Visit(setMeth.Body);
      }
      PrintToken(CSharpToken.RightCurly);
      PrintToken(CSharpToken.NewLine);
    }

    public virtual void PrintPropertyDefinitionVisibility(IPropertyDefinition propertyDefinition) {
      PrintTypeMemberVisibility(propertyDefinition.Visibility);
    }

    public virtual void PrintPropertyDefinitionReturnType(IPropertyDefinition propertyDefinition) {
      PrintTypeReference(propertyDefinition.Type);
    }

    public virtual void PrintPropertyDefinitionName(IPropertyDefinition propertyDefinition) {
      PrintIdentifier(propertyDefinition.Name);
    }

    public virtual void PrintPropertyDefinitionModifiers(IPropertyDefinition propertyDefinition) {
      PrintMethodDefinitionModifiers(propertyDefinition.Getter == null ?
        propertyDefinition.Setter.ResolvedMethod :
        propertyDefinition.Getter.ResolvedMethod);
    }
  }
}
