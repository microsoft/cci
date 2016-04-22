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

  internal class ForLoopReplacer : CodeTraverser {

    internal ForLoopReplacer(SourceMethodBody sourceMethodBody) {
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
      for (int i = 0; i < statements.Count-1; i++) {
        IStatement initializer = null;
        var initializerAssignStat = statements[i] as IExpressionStatement;
        if (initializerAssignStat != null) {
          if (!(initializerAssignStat.Expression is IAssignment)) continue;
          initializer = initializerAssignStat;
        } else {
          var initialDecl = statements[i] as ILocalDeclarationStatement;
          if (initialDecl == null || initialDecl.InitialValue == null) continue;
          initializer = initialDecl;
        }
        var whileLoop = statements[i+1] as IWhileDoStatement;
        if (whileLoop == null) continue;
        var loopBody = whileLoop.Body as BlockStatement;
        if (loopBody == null) continue;
        var incrementer = FindLastStatement(loopBody) as IExpressionStatement;
        if (incrementer == null) continue;
        var incrementAssignment = incrementer.Expression as IAssignment;
        if (incrementAssignment == null || !(incrementAssignment.Source is IAddition || incrementAssignment.Source is ISubtraction)) continue;
        var forLoop = new ForStatement() { Condition = whileLoop.Condition, Body = loopBody };
        if (initializer != null) {
          statements.RemoveAt(i--);
          forLoop.InitStatements.Add(initializer);
        }
        RemoveLastStatement(loopBody, incrementer);
        forLoop.IncrementStatements.Add(incrementer);
        statements[i + 1] = forLoop;
      }
      base.TraverseChildren(block);
    }

    static bool RemoveLastStatement(BlockStatement block, IStatement statement) {
      while (block != null) {
        var i = block.Statements.Count-1;
        if (i < 0) return false;
        if (block.Statements[i] == statement) {
          block.Statements.RemoveAt(i);
          return true;
        }
        block = block.Statements[i] as BlockStatement;
      }
      return false;
    }

    static IStatement FindLastStatement(BlockStatement block) {
      IStatement result = null;
      while (block != null) {
        var i = block.Statements.Count-1;
        if (i < 0) return result;
        var nextBlock = block.Statements[i] as BlockStatement;
        if (nextBlock == null) return block.Statements[i];
        if (i > 0) result = block.Statements[i-1];
        block = nextBlock;
      }
      return null;
    }

  }


}
