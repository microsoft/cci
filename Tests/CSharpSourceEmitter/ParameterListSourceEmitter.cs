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
