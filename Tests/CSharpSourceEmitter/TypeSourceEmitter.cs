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

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : BaseCodeTraverser, ICSharpSourceEmitter {
    public override void Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      PrintTypeDefinition(namespaceTypeDefinition as ITypeDefinition);
    }

    public override void Visit(INestedTypeDefinition nestedTypeDefinition) {
      if (!this.printCompilerGeneratedMembers && AttributeHelper.Contains(nestedTypeDefinition.Attributes, nestedTypeDefinition.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return;
      PrintTypeDefinition(nestedTypeDefinition as ITypeDefinition);
    }

    public override void Visit(ITypeDefinition typeDefinition) {
      typeDefinition.Dispatch(this);
    }
  }
}
