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

namespace Microsoft.Cci.ILToCodeModel {

  internal class BlockRemover : CodeTraverser {

    public override void TraverseChildren(IBlockStatement block) {
      BasicBlock blockStatement = (BasicBlock)block;
      List<IStatement> flatListOfStatements = new List<IStatement>();
      this.Flatten(blockStatement, flatListOfStatements);
      blockStatement.Statements = flatListOfStatements;
    }

    private void Flatten(BasicBlock blockStatement, List<IStatement> flatListOfStatements) {
      foreach (IStatement statement in blockStatement.Statements){
        BasicBlock/*?*/ nestedBlock = statement as BasicBlock;
        if (nestedBlock != null) {
          if (nestedBlock.LocalVariables == null || nestedBlock.LocalVariables.Count == 0 || nestedBlock.Statements.Count == 0 || (nestedBlock.Statements.Count == 1 && nestedBlock.Statements[0] is BasicBlock))
            this.Flatten(nestedBlock, flatListOfStatements);
          else {
            this.Traverse(nestedBlock);
            flatListOfStatements.Add(nestedBlock);
          }
        } else {
          this.Traverse(statement);
          flatListOfStatements.Add(statement);
        }
      }
    }

  }
}