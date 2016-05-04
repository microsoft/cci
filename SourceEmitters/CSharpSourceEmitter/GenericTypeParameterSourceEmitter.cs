// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
    public override void TraverseChildren(IGenericTypeParameter genericTypeParameter) {
      sourceEmitterOutput.Write(genericTypeParameter.Name.Value);
    }

    public override void TraverseChildren(IGenericMethodParameter genericMethodParameter) {
      sourceEmitterOutput.Write(genericMethodParameter.Name.Value);
    }

  }
}
