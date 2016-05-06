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

  internal class SwitchReplacer : CodeTraverser {

    IMetadataHost host;
    Hashtable<List<IGotoStatement>> gotosThatTarget;

    internal SwitchReplacer(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      this.host = sourceMethodBody.host; Contract.Assume(sourceMethodBody.host != null);
      this.gotosThatTarget = sourceMethodBody.gotosThatTarget; Contract.Assume(this.gotosThatTarget != null);
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.gotosThatTarget != null);
    }

    public override void TraverseChildren(IBlockStatement block) {
      base.TraverseChildren(block);
      Contract.Assume(block is BlockStatement);
      var decompiledBlock = (BlockStatement)block;
      var statements = decompiledBlock.Statements;
      for (int i = 0; i < statements.Count-1; i++) {
        var switchInstruction = statements[i] as SwitchInstruction;
        if (switchInstruction == null) continue;
        SwitchStatement result = new SwitchStatement();
        result.Expression = switchInstruction.switchExpression;
        statements[i] = result;
        for (int j = 0, n = switchInstruction.SwitchCases.Count; j < n; j++) {
          CompileTimeConstant caseLabel = new CompileTimeConstant() { Value = j, Type = this.host.PlatformType.SystemInt32 };
          var gotoCaseBody = switchInstruction.SwitchCases[j];
          Contract.Assume(gotoCaseBody != null);
          SwitchCase currentCase = new SwitchCase() { Expression = caseLabel };
          result.Cases.Add(currentCase);
          if (j < n-1) {
            Contract.Assume(switchInstruction.SwitchCases[j+1] != null);
            if (gotoCaseBody.TargetStatement == switchInstruction.SwitchCases[j+1].TargetStatement) continue;
          }
          currentCase.Body.Add(gotoCaseBody);
        }
        if (i == statements.Count-1) return;
        Contract.Assert(i+1 <= statements.Count);
        var gotoStatement = statements[i+1] as IGotoStatement;
        if (gotoStatement != null) {
          SwitchCase defaultCase = new SwitchCase() { }; // Default case is represented by a dummy Expression.
          defaultCase.Body.Add(statements[i + 1]);
          statements.RemoveAt(i + 1);
          result.Cases.Add(defaultCase);
        }
      }
    }
  }
}
