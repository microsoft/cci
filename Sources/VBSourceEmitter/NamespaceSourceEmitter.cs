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
    public override void TraverseChildren(INestedUnitNamespace nestedUnitNamespace) {
      PrintNamespaceDefinition((INamespaceDefinition)nestedUnitNamespace);
    }

    public virtual void PrintNamespaceDefinition(INamespaceDefinition namespaceDefinition) {
      PrintNamespaceDefinitionAttributes(namespaceDefinition);
      PrintToken(VBToken.Indent);
      PrintKeywordNamespace();
      PrintNamespaceDefinitionName(namespaceDefinition);
      //PrintNamespaceDefinitionLeftCurly(namespaceDefinition);
      this.Traverse(namespaceDefinition.Members);
      //PrintNamespaceDefinitionRightCurly(namespaceDefinition);
    }

    public virtual void PrintNamespaceDefinitionAttributes(INamespaceDefinition namespaceDefinition) {
      foreach (var attribute in namespaceDefinition.Attributes) {
        PrintAttribute(namespaceDefinition, attribute, true, "assembly");
      }
    }

    public virtual void PrintNamespaceDefinitionName(INamespaceDefinition namespaceDefinition) {
      PrintIdentifier(namespaceDefinition.Name);
    }

    //public virtual void PrintNamespaceDefinitionLeftCurly(INamespaceDefinition namespaceDefinition) {
    //  PrintToken(CSharpToken.LeftCurly);
    //}

    //public virtual void PrintNamespaceDefinitionRightCurly(INamespaceDefinition namespaceDefinition) {
    //  PrintToken(CSharpToken.RightCurly);
    //}

    public new void Traverse(IEnumerable<INamespaceMember> namespaceMembers) {
      // Try to sort in a way that preserves the original compiler order (captured in token value).
      // Put things with no tokens (eg. nested namespaces) after those with tokens, sorted by name
      var members = new List<INamespaceMember>(namespaceMembers);
      members.Sort(new Comparison<INamespaceMember>((m1, m2) => 
        {
          var t1 = m1 as IMetadataObjectWithToken;
          var t2 = m2 as IMetadataObjectWithToken;

          var tc = (t1 == null).CompareTo(t2 == null);
          if (tc != 0) 
            return tc;

          if (t1 != null)
            return t1.TokenValue.CompareTo(t2.TokenValue);

          return m1.Name.Value.CompareTo(m2.Name.Value);
        }));

      // Print the members with a blank line between them
      VisitWithInterveningBlankLines(members, m => Traverse(m));
    }

    public virtual void VisitWithInterveningBlankLines<T>(IEnumerable<T> members, Action<T> onVisit) {
      // Print the members with a blank line between any non-empty ones 
      // We go to some effort here to avoid blank lines at the start or end of a block,
      // and double blank lines.
      bool blankLinePending = false;
      bool wroteSomething = false;
      var onLineStart = new Action<ISourceEmitterOutput>((o) => {
        wroteSomething = true;
        if (blankLinePending) {
          this.sourceEmitterOutput.WriteLine("");
          blankLinePending = false;
        }
      });
      this.sourceEmitterOutput.LineStart += onLineStart;
      foreach (var member in members) {
        wroteSomething = false;
        onVisit(member);
        // Only output a blank line when we've previously written something, and we're
        // about to start writing a new line
        if (wroteSomething)
          blankLinePending = true;
      }
      this.sourceEmitterOutput.LineStart -= onLineStart;
    }
  }

}
