// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace VBSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, IVBSourceEmitter {
    public virtual void PrintTypeReference(ITypeReference typeReference) {
      PrintTypeReferenceName(typeReference);
    }

    public virtual void PrintTypeReferenceName(ITypeReference typeReference) {
      if (typeReference.TypeCode != PrimitiveTypeCode.NotPrimitive) {
        PrintPrimitive(typeReference.TypeCode);
      } else {
        this.sourceEmitterOutput.Write(this.helper.GetTypeName(typeReference.ResolvedType));
      }
    }
  }
}
