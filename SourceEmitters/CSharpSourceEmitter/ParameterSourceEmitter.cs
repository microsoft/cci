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
    public override void TraverseChildren(IParameterDefinition parameterDefinition) {
      PrintParameterDefinitionModifiers(parameterDefinition);
      PrintParameterDefinitionType(parameterDefinition);
      PrintToken(CSharpToken.Space);
      PrintParameterDefinitionName(parameterDefinition);
    }

    public virtual void PrintParameterDefinitionModifiers(IParameterDefinition parameterDefinition) {
      Contract.Requires(parameterDefinition != null);

      if (parameterDefinition.Index == 0) {
        var meth = parameterDefinition.ContainingSignature as IMethodDefinition;
        if (meth != null ) {
          if (Utils.FindAttribute(meth.Attributes, SpecialAttribute.Extension) != null) {
            PrintToken(CSharpToken.This);
            PrintToken(CSharpToken.Space);
          }
        }
      }

      foreach (var attribute in SortAttributes(parameterDefinition.Attributes)) {
        if (Utils.GetAttributeType(attribute) == SpecialAttribute.ParamArray)
          sourceEmitterOutput.Write("params");
        else
          this.PrintAttribute(parameterDefinition, attribute, false, null);

        PrintToken(CSharpToken.Space);
      }
      if (parameterDefinition.IsOut && !parameterDefinition.IsIn && parameterDefinition.IsByReference) {
        // C# out keyword means [Out] ref (with no [In] allowed)
        PrintKeywordOut();
      } else {
        if (parameterDefinition.IsIn)
          PrintPseudoCustomAttribute(parameterDefinition, "System.Runtime.InteropServices.In", null, false, null);
        if (parameterDefinition.IsOut)
          PrintPseudoCustomAttribute(parameterDefinition, "System.Runtime.InteropServices.Out", null, false, null);
        if (parameterDefinition.IsByReference)
          PrintKeywordRef();
      }
    }

    public virtual void PrintParameterDefinitionType(IParameterDefinition parameterDefinition) {
      Contract.Requires(parameterDefinition != null);

      PrintTypeReference(parameterDefinition.Type);
    }

    public virtual void PrintParameterDefinitionName(IParameterDefinition parameterDefinition) {
      Contract.Requires(parameterDefinition != null);

      PrintIdentifier(parameterDefinition.Name);
    }

  }
}
