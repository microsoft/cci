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
