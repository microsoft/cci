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
    public override void Visit(IEnumerable<IGenericTypeParameter> genericParameters) {
      PrintToken(CSharpToken.LeftAngleBracket);

      bool fFirstParameter = true;
      foreach (IGenericTypeParameter genericTypeParameter in genericParameters) {
        if (!fFirstParameter)
          PrintGenericTypeParametersListDelimiter();

        this.Visit(genericTypeParameter);

        fFirstParameter = false;
      }

      PrintToken(CSharpToken.RightAngleBracket);
    }

    public override void Visit(IEnumerable<IGenericMethodParameter> genericParameters) {
      PrintToken(CSharpToken.LeftAngleBracket);

      bool fFirstParameter = true;
      foreach (IGenericMethodParameter genericMethodParameter in genericParameters) {
        if (!fFirstParameter)
          PrintGenericTypeParametersListDelimiter();

        this.Visit(genericMethodParameter);

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
