// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
    public new void Traverse(IEnumerable<IParameterDefinition> parameters) {
      Contract.Requires(parameters != null);

      PrintToken(CSharpToken.LeftParenthesis);

      bool fFirstParameter = true;
      foreach (IParameterDefinition parameterDefinition in parameters) {
        if (!fFirstParameter)
          PrintParameterListDelimiter();

        this.Traverse(parameterDefinition);
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
