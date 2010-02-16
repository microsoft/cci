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
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {
  internal class UnreferencedLabelRemover : BaseCodeTraverser {

    Dictionary<int, uint> referencedLabels;
    bool secondPass;

    internal void Visit(BasicBlock rootBlock) {
      this.secondPass = false;
      this.Visit((IBlockStatement)rootBlock);
      this.secondPass = true;
      this.Visit((IBlockStatement)rootBlock);
    }

    public override void Visit(IBlockStatement block) {
      if (this.secondPass) {
        var bb = (BasicBlock)block;
        var statements = bb.Statements;
        for (int i = 0; i < statements.Count; i++) {
          var labeledStatement = statements[i] as ILabeledStatement;
          if (labeledStatement == null) continue;
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
        base.Visit(block);
    }

    public override void Visit(IGotoStatement gotoStatement) {
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
