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
    public override void Visit(IGenericTypeParameter genericTypeParameter) {
      sourceEmitterOutput.Write(genericTypeParameter.Name.Value);
    }

    public override void Visit(IGenericMethodParameter genericMethodParameter) {
      sourceEmitterOutput.Write(genericMethodParameter.Name.Value);
    }

  }
}
