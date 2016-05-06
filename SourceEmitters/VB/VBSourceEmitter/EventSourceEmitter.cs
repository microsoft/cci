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

namespace VBSourceEmitter
{
  public partial class SourceEmitter : CodeTraverser, IVBSourceEmitter
  {
    public override void TraverseChildren(IEventDefinition eventDefinition) {

      PrintAttributes(eventDefinition);
      PrintToken(VBToken.Indent);
      IMethodDefinition eventMeth = eventDefinition.Adder == null ?
        eventDefinition.Remover.ResolvedMethod :
        eventDefinition.Adder.ResolvedMethod;
      if (!eventDefinition.ContainingTypeDefinition.IsInterface && 
        IteratorHelper.EnumerableIsEmpty(MemberHelper.GetExplicitlyOverriddenMethods(eventMeth)))
          PrintEventDefinitionVisibility(eventDefinition);
      PrintMethodDefinitionModifiers(eventMeth);
      PrintToken(VBToken.Event);
      PrintEventDefinitionDelegateType(eventDefinition);
      PrintToken(VBToken.Space);
      PrintEventDefinitionName(eventDefinition);
      PrintToken(VBToken.Semicolon);
    }

    public virtual void PrintEventDefinitionVisibility(IEventDefinition eventDefinition) {
      PrintTypeMemberVisibility(eventDefinition.Visibility);
    }

    public virtual void PrintEventDefinitionDelegateType(IEventDefinition eventDefinition) {
      PrintTypeReference(eventDefinition.Type);
    }

    public virtual void PrintEventDefinitionName(IEventDefinition eventDefinition) {
      PrintIdentifier(eventDefinition.Name);
    }

  }
}
