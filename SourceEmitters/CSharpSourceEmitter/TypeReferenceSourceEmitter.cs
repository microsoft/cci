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
