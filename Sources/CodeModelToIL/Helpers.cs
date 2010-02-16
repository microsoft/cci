//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
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

    internal TemporaryVariable(ITypeReference type) {
      this.type = type;
    }

    public IMetadataConstant CompileTimeValue {
      get { return Dummy.Constant; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
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
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public ITypeReference Type {
      get { return this.type; }
    }
    ITypeReference type;


  }

}