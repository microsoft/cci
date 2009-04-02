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
    public override void Visit(IEnumerable<IParameterDefinition> parameters) {
      PrintToken(CSharpToken.LeftParenthesis);

      bool fFirstParameter = true;
      foreach (IParameterDefinition parameterDefinition in parameters) {
        if (!fFirstParameter)
          PrintParameterListDelimiter();

        this.Visit(parameterDefinition);
        fFirstParameter = false;
      }

      PrintToken(CSharpToken.RightParenthesis);
    }

    public virtual void PrintParameterListDelimiter() {
      PrintToken(CSharpToken.Comma);
      PrintToken(CSharpToken.Space);
    }

  }
}
