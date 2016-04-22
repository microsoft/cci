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
    public override void TraverseChildren(INamespaceTypeDefinition namespaceTypeDefinition) {
      PrintTypeDefinition(namespaceTypeDefinition as ITypeDefinition);
    }

    public override void TraverseChildren(INestedTypeDefinition nestedTypeDefinition) {
      if (!this.printCompilerGeneratedMembers && AttributeHelper.Contains(nestedTypeDefinition.Attributes, nestedTypeDefinition.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return;
      PrintTypeDefinition(nestedTypeDefinition as ITypeDefinition);
    }

  }
}
