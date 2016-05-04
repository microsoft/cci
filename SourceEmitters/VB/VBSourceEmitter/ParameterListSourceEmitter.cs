// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
