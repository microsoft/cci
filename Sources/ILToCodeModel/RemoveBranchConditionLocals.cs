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
  internal class RemoveBranchConditionLocals : CodeTraverser {

    private SourceMethodBody sourceMethodBody;
    private Dictionary<ILocalDefinition, bool> branchConditionLocals = new Dictionary<ILocalDefinition, bool>();

    internal RemoveBranchConditionLocals(SourceMethodBody sourceMethodBody) {
      this.sourceMethodBody = sourceMethodBody;
      var ops = new List<IOperation>(sourceMethodBody.ilMethodBody.Operations);
      foreach (var local in sourceMethodBody.localVariablesAndTemporaries) {
        if (local.Type.TypeCode != PrimitiveTypeCode.Boolean) continue;
        int i = 0;
        int n = ops.Count;
        while (i < n) {
          if (IsLoadLocalOp(ops[i].OperationCode) && 0 < i && ops[i].Value == local) {
            if (!IsStoreLocalOp(ops[i - 1].OperationCode)) break;
          }
          i++;
        }
        if (i == n) this.branchConditionLocals.Add(local, true);
      }
    }

    private static bool IsLoadLocalOp(OperationCode opCode) {
      return opCode == OperationCode.Ldloc || opCode == OperationCode.Ldloc_0 || opCode == OperationCode.Ldloc_1
        || opCode == OperationCode.Ldloc_2 || opCode == OperationCode.Ldloc_3 || opCode == OperationCode.Ldloc_S;
    }

    private static bool IsStoreLocalOp(OperationCode opCode) {
      return opCode == OperationCode.Stloc || opCode == OperationCode.Stloc_0 || opCode == OperationCode.Stloc_1
        || opCode == OperationCode.Stloc_2 || opCode == OperationCode.Stloc_3 || opCode == OperationCode.Stloc_S;
    }

    public override void TraverseChildren(IBlockStatement block) {
      BasicBlock bb = block as BasicBlock;
      if (bb != null) {
        FindPattern(bb.Statements);
        if (bb.Statements.Count > 0) {
          BasicBlock nbb = bb.Statements[bb.Statements.Count-1] as BasicBlock;
          if (nbb != null) this.Traverse(nbb);
        }
      }
    }

    // i   :  loc := e0;
    // i+1 :  if (loc) S0; else S1;
    //
    //  ==>
    //
    //        if (e0) S0; else S1;
    //
    // and delete statement i
    //
    // This is done only if loc is in this.branchConditionLocals
    //
    private void FindPattern(List<IStatement> statements) {
      for (int i = 0; i < statements.Count - 1; i++) {
        IExpressionStatement/*?*/ expressionStatement = statements[i] as IExpressionStatement;
        if (expressionStatement == null) continue;
        IAssignment/*?*/ assignmentStatement = expressionStatement.Expression as IAssignment;
        if (assignmentStatement == null) continue;
        if (assignmentStatement.Source is Pop) continue;
        ILocalDefinition/*?*/ localDefinition = assignmentStatement.Target.Definition as ILocalDefinition;
        if (localDefinition == null) continue;
        if (localDefinition.Type.TypeCode != PrimitiveTypeCode.Boolean) continue; // cheaper test than looking in the table
        if (!this.branchConditionLocals.ContainsKey(localDefinition)) continue;

        IConditionalStatement/*?*/ conditional = statements[i + 1] as IConditionalStatement;
        if (conditional == null) continue;
        BoundExpression/*?*/ boundExpression = conditional.Condition as BoundExpression;
        if (boundExpression == null) continue;
        ILocalDefinition/*?*/ localDefinition2 = boundExpression.Definition as ILocalDefinition;
        if (localDefinition2 == null) continue;
        if (localDefinition != localDefinition2) continue;

        var newLocs = new List<ILocation>(expressionStatement.Locations);
        newLocs.AddRange(conditional.Locations);

        statements[i + 1] = new ConditionalStatement() {
          Condition = assignmentStatement.Source,
          TrueBranch = conditional.TrueBranch,
          FalseBranch = conditional.FalseBranch,
          Locations = newLocs,
        };
        this.sourceMethodBody.numberOfAssignments[localDefinition]--;
        this.sourceMethodBody.numberOfReferences[localDefinition]--;

        statements.RemoveAt(i);
      }
    }
  }
}