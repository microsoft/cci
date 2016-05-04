// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
