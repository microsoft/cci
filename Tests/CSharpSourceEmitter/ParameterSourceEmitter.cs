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
    public override void Visit(IParameterDefinition parameterDefinition) {
      PrintParameterDefinitionModifiers(parameterDefinition);
      PrintParameterDefinitionType(parameterDefinition);
      PrintToken(CSharpToken.Space);
      PrintParameterDefinitionName(parameterDefinition);
    }

    public virtual void PrintParameterDefinitionModifiers(IParameterDefinition parameterDefinition) {
      bool first = true;
      foreach (var attribute in parameterDefinition.Attributes) {
        if (first)
          first = false;
        else
          sourceEmitterOutput.Write(" ");
        this.PrintAttribute(attribute, false, null);
      }
      if (parameterDefinition.IsIn) {
        PrintKeywordIn();
      } else if (parameterDefinition.IsOut) {
        PrintKeywordOut();
      } else if (parameterDefinition.IsByReference) {
        PrintKeywordRef();
      }
    }

    public virtual void PrintParameterDefinitionType(IParameterDefinition parameterDefinition) {
      PrintTypeReference(parameterDefinition.Type);
    }

    public virtual void PrintParameterDefinitionName(IParameterDefinition parameterDefinition) {
      sourceEmitterOutput.Write(parameterDefinition.Name.Value);
    }

  }
}
