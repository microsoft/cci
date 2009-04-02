//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
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
      if (AttributeHelper.Contains(nestedTypeDefinition.Attributes, nestedTypeDefinition.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute))
        return;
      PrintTypeDefinition(nestedTypeDefinition as ITypeDefinition);
    }

    public override void Visit(ITypeDefinition typeDefinition) {
      typeDefinition.Dispatch(this);
    }
  }
}
