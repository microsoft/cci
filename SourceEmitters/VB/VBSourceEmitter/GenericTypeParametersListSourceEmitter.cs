// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace VBSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, IVBSourceEmitter {
    public new void Traverse(IEnumerable<IGenericTypeParameter> genericParameters) {
      PrintToken(VBToken.LeftAngleBracket);

      bool fFirstParameter = true;
      foreach (IGenericTypeParameter genericTypeParameter in genericParameters) {
        if (!fFirstParameter)
          PrintGenericTypeParametersListDelimiter();

        this.Traverse(genericTypeParameter);

        fFirstParameter = false;
      }

      PrintToken(VBToken.RightAngleBracket);
    }

    public new void Traverse(IEnumerable<IGenericMethodParameter> genericParameters) {
      PrintToken(VBToken.LeftAngleBracket);

      bool fFirstParameter = true;
      foreach (IGenericMethodParameter genericMethodParameter in genericParameters) {
        if (!fFirstParameter)
          PrintGenericTypeParametersListDelimiter();

        this.Traverse(genericMethodParameter);

        fFirstParameter = false;
      }

      PrintToken(VBToken.RightAngleBracket);
    }

    public virtual void PrintGenericTypeParametersListDelimiter() {
      PrintToken(VBToken.Comma);
      PrintToken(VBToken.Space);
    }

  }
}
