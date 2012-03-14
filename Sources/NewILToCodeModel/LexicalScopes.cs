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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {

  internal class RemoveNonLexicalBlocks : CodeTraverser {

    public override void TraverseChildren(IBlockStatement blockStatement) {
      base.TraverseChildren(blockStatement);
      Contract.Assume(blockStatement is BlockStatement);
      var block = (BlockStatement)blockStatement;
      int numberOfNonLexicalBlocks = 0;
      int numberOfNonLexicalStatements = 0;
      foreach (var statement in block.Statements) {
        var nestedBlock = statement as DecompiledBlock;
        if (nestedBlock == null) continue;
        if (nestedBlock.IsLexicalScope) continue;
        numberOfNonLexicalBlocks++;
        numberOfNonLexicalStatements += nestedBlock.Statements.Count;
      }
      if (numberOfNonLexicalBlocks == 0) return;
      var flattenedList = new List<IStatement>(block.Statements.Count-numberOfNonLexicalBlocks+numberOfNonLexicalStatements);
      foreach (var statement in block.Statements) {
        var nestedBlock = statement as DecompiledBlock;
        if (nestedBlock == null || nestedBlock.IsLexicalScope)
          flattenedList.Add(statement);
        else
          flattenedList.AddRange(nestedBlock.Statements);
      }
      block.Statements = flattenedList;
    }

  }

  internal class BlockFlattener : CodeTraverser {

    public override void TraverseChildren(IBlockStatement blockStatement) {
      base.TraverseChildren(blockStatement);
      Contract.Assume(blockStatement is BlockStatement);
      var block = (BlockStatement)blockStatement;
      var statements = block.Statements;
      var n = statements.Count;
      for (int i = 0; i < n; i++) {
        var nestedBlock = statements[i] as BlockStatement;
        if (nestedBlock != null) {
          if (nestedBlock.Statements.Count == 1) {
            var decompiledBlock = nestedBlock as DecompiledBlock;
            if (decompiledBlock != null && decompiledBlock.IsLexicalScope)
              continue;
            if (nestedBlock.Statements[0] is ILocalDeclarationStatement) continue;
            statements[i] = nestedBlock.Statements[0];
          } else if (n == 1) {
            block.Statements = nestedBlock.Statements;
            return;
          }
        }
      }
    }

  }

  internal class DeclarationUnifier : CodeTraverser {

    internal DeclarationUnifier(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);

      this.numberOfReferencesToLocal = sourceMethodBody.numberOfReferencesToLocal; Contract.Assume(this.numberOfReferencesToLocal != null);
      this.numberOfAssignmentsToLocal = sourceMethodBody.numberOfAssignmentsToLocal; Contract.Assume(this.numberOfAssignmentsToLocal != null);
    }

    HashtableForUintValues<object> numberOfReferencesToLocal;
    HashtableForUintValues<object> numberOfAssignmentsToLocal;
    Hashtable<object, LocalDeclarationStatement> declarationFor = new Hashtable<object, LocalDeclarationStatement>();
    SetOfObjects unifiedDeclarations = new SetOfObjects();
    BlockStatement currentBlock;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.numberOfReferencesToLocal != null);
      Contract.Invariant(this.numberOfAssignmentsToLocal != null);
      Contract.Invariant(this.declarationFor != null);
      Contract.Invariant(this.unifiedDeclarations != null);
    }

    public override void TraverseChildren(IBlockStatement blockStatement) {
      base.TraverseChildren(blockStatement);
      Contract.Assume(blockStatement is BlockStatement);
      var block = this.currentBlock = (BlockStatement)blockStatement;
      var n = block.Statements.Count;
      int i;
      for (i = 0; i < n; i++) {
        var statement = block.Statements[i];
        Contract.Assume(statement != null);
        if (statement is EmptyStatement) continue;
        var decl = statement as LocalDeclarationStatement;
        if (decl == null) break;
        this.declarationFor.Add(decl.LocalVariable, decl);
      }
      int unifications = 0;
      for (; i < n; i++) {
        var statement = block.Statements[i];
        Contract.Assume(statement != null);
        var es = statement as ExpressionStatement;
        if (es == null) {
          if (statement is EmptyStatement || statement is LabeledStatement) continue;
          break;
        }
        var assignment = es.Expression as Assignment;
        if (assignment == null) continue;
        var local = assignment.Target.Definition as ILocalDefinition;
        if (local == null) {
          var addressDeref = assignment.Target.Definition as IAddressDereference;
          if (addressDeref == null) continue;
          var addressOf = addressDeref.Address as IAddressOf;
          if (addressOf == null) continue;
          var addressableExpression = addressOf.Expression as IAddressableExpression;
          if (addressableExpression == null) continue;
          local = addressableExpression.Definition as ILocalDefinition;
          if (local == null) continue;
        }
        LocalDeclarationStatement decl;
        if (!this.declarationFor.TryGetValue(local, out decl)) continue;
        Contract.Assume(decl != null);
        this.unifiedDeclarations.Add(decl);
        this.declarationFor.Remove(local);
        decl.InitialValue = assignment.Source;
        block.Statements[i] = decl;
        unifications++;
      }
      if (unifications == 0) return;
      var newStatements = new List<IStatement>(block.Statements.Count-unifications);
      for (i = 0; i < n; i++) {
        var statement = block.Statements[i];
        var decl = statement as LocalDeclarationStatement;
        if (decl != null) {
          if (this.unifiedDeclarations.Contains(decl)) {
            this.unifiedDeclarations.Remove(decl);
            continue;
          }
          if (this.numberOfReferencesToLocal[decl.LocalVariable] == 0 && this.numberOfAssignmentsToLocal[decl.LocalVariable] == 0)
            continue;
        }
        newStatements.Add(statement);
      }
      block.Statements = newStatements;
    }
  }

}