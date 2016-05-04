// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Cci;

namespace CSharpSourceEmitter {
  public partial class SourceEmitter : CodeTraverser, ICSharpSourceEmitter {
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
