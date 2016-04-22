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
using System.Diagnostics.Contracts;
using Microsoft.Cci.UtilityDataStructures;
using System;

namespace Microsoft.Cci.ILToCodeModel {

  internal class WhileLoopReplacer : CodeTraverser {

    internal WhileLoopReplacer(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      this.host = sourceMethodBody.host; Contract.Assume(sourceMethodBody.host != null);
      this.gotosThatTarget = sourceMethodBody.gotosThatTarget; Contract.Assume(this.gotosThatTarget != null);
    }

    IMetadataHost host;
    Hashtable<List<IGotoStatement>> gotosThatTarget;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.gotosThatTarget != null);
    }

    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var decompiledBlock = (BlockStatement)block;
      var statements = decompiledBlock.Statements;
      for (int i = 0; i < statements.Count-3; i++) {
        var gotoCondition = statements[i] as GotoStatement;
        if (gotoCondition == null) continue;
        var gotosThatTarget = this.gotosThatTarget[(uint)gotoCondition.TargetStatement.Label.UniqueKey];
        Contract.Assume(gotosThatTarget != null && gotosThatTarget.Count >= 1);
        if (gotosThatTarget.Count != 1) continue;
        var conditionalGotoBody = LookForCondition(statements, i+1, gotoCondition.TargetStatement);
        if (conditionalGotoBody == null || !(conditionalGotoBody.FalseBranch is EmptyStatement)) continue;
        var gotoBody = conditionalGotoBody.TrueBranch as GotoStatement;
        if (gotoBody == null) continue;
        Contract.Assume(i < statements.Count-3);
        if (!IsOrContainsAsFirstStatement(statements[i+1], gotoBody.TargetStatement)) continue;
        gotosThatTarget.Remove(gotoCondition);
        gotosThatTarget = this.gotosThatTarget[(uint)gotoBody.TargetStatement.Label.UniqueKey];
        Contract.Assume(gotosThatTarget != null && gotosThatTarget.Count >= 1);
        gotosThatTarget.Remove(gotoBody);
        var loopBody = ExtractBlock(statements, i+1, gotoCondition.TargetStatement);
        var whileLoop = new WhileDoStatement() { Body = loopBody, Condition = conditionalGotoBody.Condition };
        Contract.Assume(i < statements.Count);
        statements[i] = whileLoop;
      }
      base.TraverseChildren(block);
    }

    private static bool IsOrContainsAsFirstStatement(IStatement statement, ILabeledStatement labeledStatement) {
      if (statement == labeledStatement) return true;
      var block = statement as BlockStatement;
      while (block != null) {
        var statements = block.Statements;
        var n = statements.Count;
        if (n == 0) return false;
        for (int i = 0; i < n; i++) {
          var s = statements[i];
          var locDecl = s as LocalDeclarationStatement;
          if (locDecl != null) {
            if (locDecl.InitialValue == null) continue;
            return false;
          }
          if (s == labeledStatement) return true;
          block = s as BlockStatement;
        }
      }
      return false;
    }

    private ConditionalStatement/*?*/ LookForCondition(List<IStatement> statements, int i, ILabeledStatement potentialLabel) {
      Contract.Requires(statements != null);
      Contract.Requires(i >= 0);
      Contract.Requires(potentialLabel != null);

      for (; i < statements.Count-1; i++) {
        if (statements[i] != potentialLabel) continue;
        return statements[i+1] as ConditionalStatement;
      }
      return null;
    }

    private static IStatement ExtractBlock(List<IStatement> statements, int first, ILabeledStatement labelOfSubsequentCode) {
      Contract.Requires(statements != null);
      Contract.Requires(first > 0);
      Contract.Ensures(Contract.Result<IStatement>() != null);

      var last = first;
      var n = statements.Count;
      while (last < n) {
        var statement = statements[last];
        if (statement == labelOfSubsequentCode) {
          statements.RemoveRange(last, 2);
          break;
        }
        last++;
      }
      if (last == n) return new EmptyStatement();
      Contract.Assume(last <= statements.Count);
      if (first == last) return new EmptyStatement();
      if (first == last-1) {
        var firstBlock = statements[first] as BlockStatement;
        if (firstBlock != null) {
          statements.RemoveAt(first);
          return firstBlock;
        }
      }
      var newStatements = statements.GetRange(first, last-first);
      statements.RemoveRange(first, last-first);
      return new BlockStatement() { Statements = newStatements };
    }

  }

}
