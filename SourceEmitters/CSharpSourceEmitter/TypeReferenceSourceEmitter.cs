// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
    public virtual void PrintTypeReference(ITypeReference typeReference) {
      Contract.Requires(typeReference != null);

      PrintTypeReferenceName(typeReference);
    }

    public virtual void PrintTypeReferenceName(ITypeReference typeReference) {
      Contract.Requires(typeReference != null);

      var typeName = TypeHelper.GetTypeName(typeReference, 
        NameFormattingOptions.ContractNullable|NameFormattingOptions.UseTypeKeywords|
        NameFormattingOptions.TypeParameters|NameFormattingOptions.EmptyTypeParameterList|
        NameFormattingOptions.OmitCustomModifiers);
      sourceEmitterOutput.Write(typeName);
    }
  }
}
