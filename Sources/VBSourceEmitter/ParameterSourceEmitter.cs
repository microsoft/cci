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

namespace VBSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, IVBSourceEmitter {
    public override void TraverseChildren(IParameterDefinition parameterDefinition) {
      PrintParameterDefinitionModifiers(parameterDefinition);
      PrintParameterDefinitionName(parameterDefinition);
      PrintToken(VBToken.Space);
      PrintToken(VBToken.As);
      PrintToken(VBToken.Space);
      PrintParameterDefinitionType(parameterDefinition);
    }

    public virtual void PrintParameterDefinitionModifiers(IParameterDefinition parameterDefinition) {

      if (parameterDefinition.Index == 0) {
        var meth = parameterDefinition.ContainingSignature as IMethodDefinition;
        if (meth != null ) {
          if (Utils.FindAttribute(meth.Attributes, SpecialAttribute.Extension) != null) {
            PrintToken(VBToken.This);
            PrintToken(VBToken.Space);
          }
        }
      }

      foreach (var attribute in parameterDefinition.Attributes) {
        if (Utils.GetAttributeType(attribute) == SpecialAttribute.ParamArray)
          sourceEmitterOutput.Write("params");
        else
          this.PrintAttribute(parameterDefinition, attribute, false, null);

        PrintToken(VBToken.Space);
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
      PrintTypeReference(parameterDefinition.Type);
    }

    public virtual void PrintParameterDefinitionName(IParameterDefinition parameterDefinition) {
      PrintIdentifier(parameterDefinition.Name);
    }

  }
}
