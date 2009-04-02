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
      PrintToken(CSharpToken.Indent);
      PrintPropertyDefinitionVisibility(propertyDefinition);
      PrintPropertyDefinitionReturnType(propertyDefinition);
      PrintToken(CSharpToken.Space);
      PrintPropertyDefinitionName(propertyDefinition);
      if (IteratorHelper.EnumerableIsNotEmpty(propertyDefinition.Parameters))
        Visit(propertyDefinition.Parameters);
      PrintToken(CSharpToken.NewLine);
      PrintToken(CSharpToken.LeftCurly);
      if (propertyDefinition.Getter != null) {
        PrintToken(CSharpToken.Indent);
        PrintToken(CSharpToken.Get);
        PrintToken(CSharpToken.Semicolon);
      }
      if (propertyDefinition.Setter != null) {
        PrintToken(CSharpToken.Indent);
        PrintToken(CSharpToken.Set);
        PrintToken(CSharpToken.Semicolon);
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
      sourceEmitterOutput.Write(propertyDefinition.Name.Value);
    }

  }
}
