// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
    public new void Traverse(IEnumerable<IGenericTypeParameter> genericParameters) {
      Contract.Requires(genericParameters != null);

      PrintToken(CSharpToken.LeftAngleBracket);

      bool fFirstParameter = true;
      foreach (IGenericTypeParameter genericTypeParameter in genericParameters) {
        if (!fFirstParameter)
          PrintGenericTypeParametersListDelimiter();

        this.Traverse(genericTypeParameter);

        fFirstParameter = false;
      }

      PrintToken(CSharpToken.RightAngleBracket);
    }

    public new void Traverse(IEnumerable<IGenericMethodParameter> genericParameters) {
      Contract.Requires(genericParameters != null);

      PrintToken(CSharpToken.LeftAngleBracket);

      bool fFirstParameter = true;
      foreach (IGenericMethodParameter genericMethodParameter in genericParameters) {
        if (!fFirstParameter)
          PrintGenericTypeParametersListDelimiter();

        this.Traverse(genericMethodParameter);

        fFirstParameter = false;
      }

      PrintToken(CSharpToken.RightAngleBracket);
    }

    public virtual void PrintGenericTypeParametersListDelimiter() {
      PrintToken(CSharpToken.Comma);
      PrintToken(CSharpToken.Space);
    }

  }
}
