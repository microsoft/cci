// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CSharpSourceEmitter
{
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter
  {
    public override void TraverseChildren(IEventDefinition eventDefinition) {

      PrintAttributes(eventDefinition);
      PrintToken(CSharpToken.Indent);
      IMethodDefinition eventMeth = eventDefinition.Adder == null ?
        eventDefinition.Remover.ResolvedMethod :
        eventDefinition.Adder.ResolvedMethod;
      if (!eventDefinition.ContainingTypeDefinition.IsInterface && 
        IteratorHelper.EnumerableIsEmpty(MemberHelper.GetExplicitlyOverriddenMethods(eventMeth)))
          PrintEventDefinitionVisibility(eventDefinition);
      PrintMethodDefinitionModifiers(eventMeth);
      PrintToken(CSharpToken.Event);
      PrintEventDefinitionDelegateType(eventDefinition);
      PrintToken(CSharpToken.Space);
      PrintEventDefinitionName(eventDefinition);
      PrintToken(CSharpToken.Semicolon);
    }

    public virtual void PrintEventDefinitionVisibility(IEventDefinition eventDefinition) {
      Contract.Requires(eventDefinition != null);

      PrintTypeMemberVisibility(eventDefinition.Visibility);
    }

    public virtual void PrintEventDefinitionDelegateType(IEventDefinition eventDefinition) {
      Contract.Requires(eventDefinition != null);

      PrintTypeReference(eventDefinition.Type);
    }

    public virtual void PrintEventDefinitionName(IEventDefinition eventDefinition) {
      Contract.Requires(eventDefinition != null);

      PrintIdentifier(eventDefinition.Name);
    }

  }
}
