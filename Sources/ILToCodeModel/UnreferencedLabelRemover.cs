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
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {
  internal class UnreferencedLabelRemover : CodeTraverser {

    internal UnreferencedLabelRemover(SourceMethodBody methodBody) {
      this.methodBody = methodBody;
    }

    SourceMethodBody methodBody;
    Dictionary<int, uint> referencedLabels;
    bool secondPass;

    internal void Traverse(BasicBlock rootBlock) {
      this.secondPass = false;
      this.Traverse((IBlockStatement)rootBlock);
      this.secondPass = true;
      this.Traverse((IBlockStatement)rootBlock);
    }

    public override void TraverseChildren(IBlockStatement block) {
      if (this.secondPass) {
        var bb = (BasicBlock)block;
        var statements = bb.Statements;
        for (int i = 0; i < statements.Count; i++) {
          var labeledStatement = statements[i] as ILabeledStatement;
          if (labeledStatement == null) {
            if (i == 0) continue;
            var returnStatement = statements[i] as ReturnStatement;
            if (returnStatement != null) {
              var localDeclarationsStatement = statements[i-1] as ILocalDeclarationStatement;
              if (localDeclarationsStatement != null) {
                var boundExpression = returnStatement.Expression as IBoundExpression;
                int numReferences;
                if (boundExpression != null && boundExpression.Definition == localDeclarationsStatement.LocalVariable &&
                  this.methodBody.numberOfReferences.TryGetValue(localDeclarationsStatement.LocalVariable, out numReferences) &&
                  numReferences == 1) {
                    if (this.methodBody.sourceLocationProvider != null) {
                      bool isCompilerGenerated;
                      this.methodBody.sourceLocationProvider.GetSourceNameFor(localDeclarationsStatement.LocalVariable, out isCompilerGenerated);
                      if (!isCompilerGenerated) continue;
                    }
                    statements.RemoveAt(--i);
                    returnStatement.Expression = localDeclarationsStatement.InitialValue;
                }
              }
            } else {
              var expressionStatement = statements[i] as IExpressionStatement;
              if (expressionStatement != null) {
                var assignment = expressionStatement.Expression as IAssignment;
                if (assignment != null) {
                  var localDefinition = assignment.Target.Definition as ILocalDefinition;
                  if (localDefinition != null) {
                    for (int j = i-1; j >= 0; j--) {
                      if (statements[j] is IEmptyStatement) continue;
                      var localDeclarationsStatement = statements[j] as LocalDeclarationStatement;
                      if (localDeclarationsStatement == null) break;
                      if (localDeclarationsStatement.LocalVariable != localDefinition) continue;
                      if (localDeclarationsStatement.InitialValue != null) break;
                      localDeclarationsStatement.InitialValue = assignment.Source;
                      statements[i] = statements[j];
                      statements.RemoveAt(j);
                      i--;
                      break;
                    }
                  }
                }
              }
            }
            continue;
          }
          uint references = 0;
          if (this.referencedLabels != null)
            this.referencedLabels.TryGetValue(labeledStatement.Label.UniqueKey, out references);
          if (references > 1) continue;
          if (references == 1 && i > 0) {
            var gotoStatement = statements[i-1] as GotoStatement;
            if (gotoStatement != null && gotoStatement.TargetStatement.Label.UniqueKey == labeledStatement.Label.UniqueKey) {
              statements.RemoveAt(--i);
              references = 0;
            }
          }
          if (references > 0) continue;
          statements[i] = labeledStatement.Statement;
        }
      } else
        base.TraverseChildren(block);
    }

    public override void TraverseChildren(IGotoStatement gotoStatement) {
      if (this.referencedLabels == null)
        this.referencedLabels = new Dictionary<int, uint>();
      var key = gotoStatement.TargetStatement.Label.UniqueKey;
      if (this.referencedLabels.ContainsKey(key))
        this.referencedLabels[key]++;
      else
        this.referencedLabels.Add(key, 1);
    }

  }
}
