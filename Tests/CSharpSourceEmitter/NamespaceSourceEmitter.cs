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
    public override void Visit(INamespaceDefinition namespaceDefinition) {
      namespaceDefinition.Dispatch(this);
    }

    public override void Visit(INestedUnitNamespace nestedUnitNamespace) {
      INamespaceDefinition namespaceDefinition = nestedUnitNamespace as INamespaceDefinition;

      PrintNamespaceDefinitionAttributes(namespaceDefinition);
      PrintToken(CSharpToken.Indent);
      PrintKeywordNamespace();
      PrintNamespaceDefinitionName(namespaceDefinition);
      PrintToken(CSharpToken.NewLine);
      PrintNamespaceDefinitionLeftCurly(namespaceDefinition);
      this.Visit(nestedUnitNamespace.Members);
      PrintNamespaceDefinitionRightCurly(namespaceDefinition);
    }

    public override void Visit(INestedUnitSetNamespace nestedUnitSetNamespace) {
      INamespaceDefinition namespaceDefinition = nestedUnitSetNamespace as INamespaceDefinition;

      PrintNamespaceDefinitionAttributes(namespaceDefinition);
      PrintToken(CSharpToken.Indent);
      PrintKeywordNamespace();
      PrintNamespaceDefinitionName(namespaceDefinition);
      PrintNamespaceDefinitionLeftCurly(namespaceDefinition);
      this.Visit(nestedUnitSetNamespace.Members);
      PrintNamespaceDefinitionRightCurly(namespaceDefinition);
    }

    public override void Visit(IRootUnitNamespace rootUnitNamespace) {
      this.Visit(rootUnitNamespace.Members);
    }

    public override void Visit(IRootUnitSetNamespace rootUnitSetNamespace) {
      this.Visit(rootUnitSetNamespace.Members);
    }

    public override void Visit(INamespaceMember namespaceMember) {
      if (this.stopTraversal) return;
      INamespaceDefinition/*?*/ nestedNamespace = namespaceMember as INamespaceDefinition;
      if (nestedNamespace != null)
        this.Visit(nestedNamespace);
      else {
        ITypeDefinition/*?*/ namespaceType = namespaceMember as ITypeDefinition;
        if (namespaceType != null)
          this.Visit(namespaceType);
        else {
          ITypeDefinitionMember/*?*/ globalFieldOrMethod = namespaceMember as ITypeDefinitionMember;
          if (globalFieldOrMethod != null) {
          } else {
            INamespaceAliasForType/*?*/ namespaceAlias = namespaceMember as INamespaceAliasForType;
            if (namespaceAlias != null)
              this.Visit(namespaceAlias);
            else {
              //TODO: error
              namespaceMember.Dispatch(this);
            }
          }
        }
      }
    }

    public virtual void PrintNamespaceDefinitionAttributes(INamespaceDefinition namespaceDefinition) {
      foreach (var attribute in namespaceDefinition.Attributes) {
        PrintAttribute(attribute, true, "assembly");
      }
    }

    public virtual void PrintNamespaceDefinitionName(INamespaceDefinition namespaceDefinition) {
      PrintIdentifier(namespaceDefinition.Name);
    }

    public virtual void PrintNamespaceDefinitionLeftCurly(INamespaceDefinition namespaceDefinition) {
      PrintToken(CSharpToken.LeftCurly);
    }

    public virtual void PrintNamespaceDefinitionRightCurly(INamespaceDefinition namespaceDefinition) {
      PrintToken(CSharpToken.RightCurly);
      PrintToken(CSharpToken.NewLine);
    }

  }

}
