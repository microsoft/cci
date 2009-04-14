//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;

namespace Microsoft.Cci.ILToCodeModel {
  internal class EmptyStatementRemover : BaseCodeTraverser {
    public override void Visit(IBlockStatement block) {
      base.Visit(block);
      BasicBlock bb = block as BasicBlock;
      if (bb != null)
        FindPattern(bb.Statements);
      return;
    }
    private static void FindPattern(List<IStatement> statements) {
      int n = statements.Count;
      for (int i = n - 1; 0 <= i; i--) {
        if (statements[i] is IEmptyStatement)
          statements.RemoveAt(i);
      }
      return;
    }
  }
}
