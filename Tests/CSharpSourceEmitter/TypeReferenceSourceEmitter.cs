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
    public virtual void PrintTypeReference(ITypeReference typeReference) {
      PrintTypeReferenceName(typeReference);
    }

    public virtual void PrintTypeReferenceName(ITypeReference typeReference) {
      sourceEmitterOutput.Write(TypeHelper.GetTypeName(typeReference, NameFormattingOptions.ContractNullable|NameFormattingOptions.UseTypeKeywords));
    }
  }
}
