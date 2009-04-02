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
    public virtual void PrintBaseTypesAndInterfacesList(ITypeDefinition typeDefinition) {
      IEnumerable<ITypeReference> basesList = typeDefinition.BaseClasses;
      IEnumerable<ITypeReference> interfacesList = typeDefinition.Interfaces;

      bool fFirstBase = true;
      foreach (ITypeReference baseTypeReference in basesList) {
        if (fFirstBase && TypeHelper.TypesAreEquivalent(baseTypeReference, typeDefinition.PlatformType.SystemObject))
          continue;

        if (fFirstBase)
          PrintBaseTypesAndInterfacesColon();
        else
          PrintBaseTypesAndInterfacesListDelimiter();

        PrintBaseTypeOrInterface(baseTypeReference);
        fFirstBase = false;
      }

      foreach (ITypeReference interfaceTypeReference in interfacesList) {
        if (fFirstBase)
          PrintBaseTypesAndInterfacesColon();
        else
          PrintBaseTypesAndInterfacesListDelimiter();

        PrintBaseTypeOrInterface(interfaceTypeReference);
        fFirstBase = false;
      }
    }

    public virtual void PrintBaseTypesAndInterfacesColon() {
      PrintToken(CSharpToken.Space);
      PrintToken(CSharpToken.Colon);
      PrintToken(CSharpToken.Space);
    }

    public virtual void PrintBaseTypesAndInterfacesListDelimiter() {
      PrintToken(CSharpToken.Comma);
      PrintToken(CSharpToken.Space);
    }

    public virtual void PrintBaseTypeOrInterface(ITypeReference baseTypeReference) {
      PrintBaseTypeOrInterfaceName(baseTypeReference);
    }

  }
}
