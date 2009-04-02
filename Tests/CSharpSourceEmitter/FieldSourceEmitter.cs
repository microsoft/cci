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
    public override void Visit(IFieldDefinition fieldDefinition) {
      PrintToken(CSharpToken.Indent);
      PrintFieldDefinitionVisibility(fieldDefinition);
      PrintFieldDefinitionModifiers(fieldDefinition);
      PrintFieldDefinitionType(fieldDefinition);
      PrintToken(CSharpToken.Space);
      PrintFieldDefinitionName(fieldDefinition);
      if (fieldDefinition.IsCompileTimeConstant) {
        sourceEmitterOutput.Write(" = ");
        this.Visit(fieldDefinition.CompileTimeValue);
      }
      PrintToken(CSharpToken.Semicolon);
    }

    public virtual void PrintFieldDefinitionVisibility(IFieldDefinition fieldDefinition) {
      PrintTypeMemberVisibility(fieldDefinition.Visibility);
    }

    public virtual void PrintFieldDefinitionModifiers(IFieldDefinition fieldDefinition) {
      if (fieldDefinition.IsCompileTimeConstant) {
        sourceEmitterOutput.Write("const ");
        return;
      }

      if (fieldDefinition.IsStatic)
        PrintKeywordStatic();

      if (fieldDefinition.IsReadOnly)
        PrintKeywordReadOnly();

      if (MemberHelper.IsVolatile(fieldDefinition))
        sourceEmitterOutput.Write("volatile ");
    }

    public virtual void PrintFieldDefinitionType(IFieldDefinition fieldDefinition) {
      PrintTypeReference(fieldDefinition.Type);
    }

    public virtual void PrintFieldDefinitionName(IFieldDefinition fieldDefinition) {
      sourceEmitterOutput.Write(fieldDefinition.Name.Value);
    }

  }
}
