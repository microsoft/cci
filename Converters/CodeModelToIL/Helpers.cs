// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Microsoft.Cci.CodeModelToIL {

  internal sealed class ExpressionSourceLocation : IExpressionSourceLocation {

    internal ExpressionSourceLocation(IPrimarySourceLocation primarySourceLocation) {
      Contract.Requires(primarySourceLocation != null);
      this.primarySourceLocation = primarySourceLocation;
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.primarySourceLocation != null);
    }

    public IPrimarySourceLocation PrimarySourceLocation {
      get { return this.primarySourceLocation; }
    }
    IPrimarySourceLocation primarySourceLocation;

    public IDocument Document {
      get { return this.PrimarySourceLocation.Document; }
    }
  }

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