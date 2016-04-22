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

namespace VBSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, IVBSourceEmitter {
    public new void Traverse(IEnumerable<IParameterDefinition> parameters) {
      PrintToken(VBToken.LeftParenthesis);

      bool fFirstParameter = true;
      foreach (IParameterDefinition parameterDefinition in parameters) {
        if (!fFirstParameter)
          PrintParameterListDelimiter();

        this.Traverse(parameterDefinition);
        fFirstParameter = false;
      }

      PrintToken(VBToken.RightParenthesis);
    }

    public virtual void PrintParameterListDelimiter() {
      PrintToken(VBToken.Comma);
      PrintToken(VBToken.Space);
    }

  }
}
