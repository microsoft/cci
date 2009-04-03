//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.Ast;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.SpecSharp {
  internal sealed class SpecSharpGenericTypeParameterDeclaration : GenericTypeParameterDeclaration {

    public SpecSharpGenericTypeParameterDeclaration(List<SourceCustomAttribute>/*?*/ sourceAttributes, NameDeclaration name, ushort index)
      : base(sourceAttributes, name, index, new List<TypeExpression>(), TypeParameterVariance.NonVariant, false, false, false, name.SourceLocation) {
    }

    new internal bool MustBeReferenceType {
      set 
        //^ requires value;
      {
        base.MustBeValueType = false;
        base.MustBeReferenceType = value; 
      }
    }

    new internal bool MustBeValueType {
      set
        //^ requires value;
      {
        base.MustBeReferenceType = false;
        base.MustBeValueType = value;
      }
    }
  
    new internal bool MustHaveDefaultConstructor {
      set
        //^ requires value;
      {
        base.MustHaveDefaultConstructor = value;
      }
    }
  
    new internal void AddConstraint(TypeExpression typeExpression)
    {
      base.AddConstraint(typeExpression);
    }

  }
}