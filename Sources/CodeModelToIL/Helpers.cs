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
using System.Diagnostics;
using Microsoft.Cci;

namespace Microsoft.Cci {
  internal sealed class TemporaryVariable : ILocalDefinition {

    internal TemporaryVariable(ITypeReference type, IMethodDefinition containingMethod) {
      this.type = type;
      this.methodDefinition = containingMethod;
    }

    public IMetadataConstant CompileTimeValue {
      get { return Dummy.Constant; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    public bool IsConstant {
      get { return false; }
    }

    public bool IsModified {
      get { return false; }
    }

    public bool IsPinned {
      get { return false; }
    }

    public bool IsReference {
      get { return false; }
    }

    public IName Name {
      get { return Dummy.Name; }
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
    }
    IMethodDefinition methodDefinition;


    public ITypeReference Type {
      get { return this.type; }
    }
    ITypeReference type;


  }

}