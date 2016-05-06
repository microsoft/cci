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

  internal class TryCatchReplacer : CodeTraverser {

    IMetadataHost host;
    DoubleHashtable<TryCatchFinallyStatement> tryCatchFinallyMap = new DoubleHashtable<TryCatchFinallyStatement>();
    DoubleHashtable<IOperationExceptionInformation> handlerMap = new DoubleHashtable<IOperationExceptionInformation>();
    Hashtable<List<IGotoStatement>> gotosThatTarget;
    SetOfObjects trystartOutsideLabels = new SetOfObjects();
    Hashtable<LabeledStatement> insideLabelFor = new Hashtable<LabeledStatement>();

    internal TryCatchReplacer(SourceMethodBody sourceMethodBody, DecompiledBlock block) {
      Contract.Requires(sourceMethodBody != null);
      Contract.Requires(block != null);
      this.host = sourceMethodBody.host; Contract.Assume(sourceMethodBody.host != null);
      this.gotosThatTarget = sourceMethodBody.gotosThatTarget; Contract.Assume(this.gotosThatTarget != null);
      this.unconditionalBranchRemover.gotosThatTarget = sourceMethodBody.gotosThatTarget;

      Contract.Assume(sourceMethodBody.ilMethodBody != null);
      foreach (var exInfo in sourceMethodBody.ilMethodBody.OperationExceptionInformation) {
        var tryCatchF = this.tryCatchFinallyMap.Find(exInfo.TryStartOffset, exInfo.TryEndOffset);
        if (tryCatchF == null) {
          tryCatchF = new TryCatchFinallyStatement();
          this.tryCatchFinallyMap.Add(exInfo.TryStartOffset, exInfo.TryEndOffset, tryCatchF);
        }
        if (exInfo.HandlerKind == HandlerKind.Filter) {
          this.tryCatchFinallyMap.Add(exInfo.FilterDecisionStartOffset, exInfo.HandlerEndOffset, tryCatchF);
          this.handlerMap.Add(exInfo.FilterDecisionStartOffset, exInfo.HandlerEndOffset, exInfo);
        } else {
          this.tryCatchFinallyMap.Add(exInfo.HandlerStartOffset, exInfo.HandlerEndOffset, tryCatchF);
          this.handlerMap.Add(exInfo.HandlerStartOffset, exInfo.HandlerEndOffset, exInfo);
        }
      }
    }

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.tryCatchFinallyMap != null);
      Contract.Invariant(this.handlerMap != null);
      Contract.Invariant(this.gotosThatTarget != null);
      Contract.Invariant(this.trystartOutsideLabels != null);
      Contract.Invariant(this.insideLabelFor != null);
      Contract.Invariant(this.localDeclarationRemover != null);
      Contract.Invariant(this.unconditionalBranchRemover != null);
    }

    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var decompiledBlock = (BlockStatement)block;
      var statements = decompiledBlock.Statements;
      List<IStatement> newStatements = null;
      for (int i = 0, n = statements.Count; i < n; i++) {
        LabeledStatement outerLabel = null;
        Contract.Assume(i < statements.Count);
        var statement = statements[i];
        Contract.Assume(statement != null);
        var nestedBlock = statement as DecompiledBlock;
        if (nestedBlock != null) {
          var trycf = this.tryCatchFinallyMap.Find(nestedBlock.StartOffset, nestedBlock.EndOffset);
          if (trycf != null) {
            statements[i] = trycf;
            if (newStatements == null) newStatements = CopyStatements(statements, i);
            IOperationExceptionInformation handlerInfo = this.handlerMap.Find(nestedBlock.StartOffset, nestedBlock.EndOffset);
            if (handlerInfo == null) {
              outerLabel = nestedBlock.ReturnInitialLabel();
              if (outerLabel != null) {
                newStatements.Add(outerLabel);
                var innerLabel = new LabeledStatement(outerLabel);
                innerLabel.Label = this.host.NameTable.GetNameFor(outerLabel.Label.Value+"#inner");
                nestedBlock.ReplaceInitialLabel(innerLabel);
                this.trystartOutsideLabels.Add(outerLabel);
                this.insideLabelFor[(uint)outerLabel.Label.UniqueKey] = innerLabel;
              }
              trycf.TryBody = nestedBlock;
              statement = trycf;
            } else {
              switch (handlerInfo.HandlerKind) {
                case HandlerKind.Catch:
                  ILocalDefinition exceptionContainer = this.ExtractExceptionContainer(nestedBlock, handlerInfo.ExceptionType);
                  if (!(exceptionContainer is Dummy))
                    this.RemoveLocalDeclarationOf(exceptionContainer, nestedBlock);
                  trycf.CatchClauses.Add(new CatchClause() { Body = nestedBlock, ExceptionType = handlerInfo.ExceptionType, ExceptionContainer = exceptionContainer });
                  break;
                case HandlerKind.Fault:
                  trycf.FaultBody = nestedBlock;
                  break;
                case HandlerKind.Filter:
                  var filterCondition = this.GetFilterCondition(nestedBlock);
                  if (filterCondition != null) this.RemovedFilterCondition(nestedBlock);
                  trycf.CatchClauses.Add(new CatchClause() { Body = nestedBlock, ExceptionType = this.host.PlatformType.SystemObject, FilterCondition = filterCondition });
                  break;
                case HandlerKind.Finally:
                  this.RemoveEndFinallyFrom(nestedBlock);
                  trycf.FinallyBody = nestedBlock;
                  break;
              }
              this.Traverse(nestedBlock);
              if (outerLabel != null) this.trystartOutsideLabels.Remove(outerLabel);
              continue;
            }
          }
        }
        if (newStatements != null) newStatements.Add(statement);
        this.Traverse(statement);
      }
      if (newStatements != null) {
        decompiledBlock.Statements = newStatements;
        for (int i = 0, n = newStatements.Count-1; i < n; i++) {
          var trycf = newStatements[i] as TryCatchFinallyStatement;
          if (trycf == null) continue;
          var followingBlock = newStatements[i+1] as DecompiledBlock;
          if (followingBlock != null) this.RemoveUnconditionalBranchesToLabelImmediatelyFollowing(trycf, followingBlock);
          this.ConsolidateScopes(trycf);
        }
      }
    }

    public override void TraverseChildren(IGotoStatement gotoStatement) {
      var mutableGoto = gotoStatement as GotoStatement;
      if (mutableGoto == null) return;
      var target = gotoStatement.TargetStatement as LabeledStatement;
      if (target != null && this.trystartOutsideLabels.Contains(target)) {
        var key = (uint)target.Label.UniqueKey;
        var gotos = this.gotosThatTarget[key];
        if (gotos != null) gotos.Remove(gotoStatement);
        var newTarget = this.insideLabelFor[key];
        mutableGoto.TargetStatement = newTarget;
        Contract.Assume(newTarget != null);
        key = (uint)newTarget.Label.UniqueKey;
        gotos = this.gotosThatTarget[key];
        if (gotos == null) this.gotosThatTarget[key] = gotos = new List<IGotoStatement>();
        gotos.Add(gotoStatement);
      }
    }

    private void ConsolidateScopes(TryCatchFinallyStatement trycf) {
      Contract.Requires(trycf != null);
      ConsolidateScopes((DecompiledBlock)trycf.TryBody);
      if (trycf.FaultBody != null) ConsolidateScopes((DecompiledBlock)trycf.FaultBody);
      if (trycf.FinallyBody != null) ConsolidateScopes((DecompiledBlock)trycf.FinallyBody);
      foreach (var catchClause in trycf.CatchClauses) {
        Contract.Assume(catchClause != null);
        var cb = (DecompiledBlock)catchClause.Body;
        ConsolidateScopes(cb);
      }
    }

    private static void ConsolidateScopes(DecompiledBlock cb) {
      Contract.Requires(cb != null);
      for (int i = 0; i < cb.Statements.Count; i++) {
        var nb = cb.Statements[i] as DecompiledBlock;
        if (nb == null) break;
        if (nb.Statements.Count == 0) cb.Statements.RemoveAt(i--);
      }
      if (cb.Statements.Count == 1) {
        var nb = cb.Statements[0] as DecompiledBlock;
        if (nb != null) cb.Statements = nb.Statements;
      }
    }

    private bool RemoveEndFinallyFrom(DecompiledBlock block) {
      Contract.Requires(block != null);
      Contract.Assume(block.Statements.Count > 0); //There must be an endfinally
      if (block.Statements[block.Statements.Count-1] is EndFinally) {
        block.Statements.RemoveAt(block.Statements.Count-1);
        return true;
      }
      var lastBlock = block.Statements[block.Statements.Count-1] as DecompiledBlock;
      if (lastBlock != null && this.RemoveEndFinallyFrom(lastBlock)) return true;
      LabeledStatement endFinally = new LabeledStatement() { Label = this.host.NameTable.GetNameFor("__endfinally#"+this.endFinallyCounter++), Statement = new EmptyStatement() };
      block.Statements.Add(endFinally);
      return this.RemoveEndFinallyFrom(block, endFinally);
    }
    int endFinallyCounter;

    private bool RemoveEndFinallyFrom(DecompiledBlock block, LabeledStatement labelToGoto) {
      Contract.Requires(block != null);
      Contract.Requires(labelToGoto != null);

      for (int i = 0; i < block.Statements.Count; i++) {
        if (block.Statements[i] is EndFinally) {
          var gotoFinally = new GotoStatement() { TargetStatement = labelToGoto };
          block.Statements[i] = gotoFinally;
          var gotos = new List<IGotoStatement>(1);
          gotos.Add(gotoFinally);
          this.gotosThatTarget[(uint)labelToGoto.Label.UniqueKey] = gotos;
          return true;
        }
        var nestedBlock = block.Statements[i] as DecompiledBlock;
        if (nestedBlock != null && this.RemoveEndFinallyFrom(nestedBlock, labelToGoto)) return true;
      }
      return false;
    }

    private void RemoveUnconditionalBranchesToLabelImmediatelyFollowing(TryCatchFinallyStatement trycf, DecompiledBlock followingBlock) {
      Contract.Requires(trycf != null);
      while (followingBlock != null) {
        if (followingBlock.Statements.Count == 0) return;
        var label = followingBlock.Statements[0] as LabeledStatement;
        if (label != null) {
          this.unconditionalBranchRemover.StopTraversal = false;
          this.unconditionalBranchRemover.targetLabel = label;
          this.unconditionalBranchRemover.Traverse(trycf);
          var gotos = this.gotosThatTarget.Find((uint)label.Label.UniqueKey);
          if (gotos == null || gotos.Count == 0) {
            followingBlock.Statements.RemoveAt(0);
          }
          return;
        }
        followingBlock = followingBlock.Statements[0] as DecompiledBlock;
      }
    }

    UnconditionalBranchRemover unconditionalBranchRemover = new UnconditionalBranchRemover();

    class UnconditionalBranchRemover : CodeTraverser {
      internal Hashtable<List<IGotoStatement>> gotosThatTarget;
      internal LabeledStatement targetLabel;

      public override void TraverseChildren(IBlockStatement block) {
        Contract.Assume(block is BlockStatement);
        Contract.Assume(this.gotosThatTarget != null);
        var b = (BlockStatement)block;
        for (int i = 0; i < b.Statements.Count; i++) {
          var label = b.Statements[i] as LabeledStatement;
          if (label != null) {
            var gotos = this.gotosThatTarget.Find((uint)label.Label.UniqueKey);
            if (gotos == null || gotos.Count == 0) b.Statements.RemoveAt(i--);
          } else {
            var gotoStatement = b.Statements[i] as GotoStatement;
            if (gotoStatement == null || gotoStatement.TargetStatement != this.targetLabel) continue;
            var gotos = this.gotosThatTarget.Find((uint)gotoStatement.TargetStatement.Label.UniqueKey);
            if (gotos != null) gotos.Remove(gotoStatement);
            b.Statements.RemoveAt(i--);
          }
        }
        this.Traverse(b.Statements);
      }
    }

    private void RemoveLocalDeclarationOf(ILocalDefinition exceptionContainer, DecompiledBlock block) {
      Contract.Requires(exceptionContainer != null);
      Contract.Requires(block != null);
      this.localDeclarationRemover.localVariableToRemove = exceptionContainer;
      this.localDeclarationRemover.StopTraversal = false;
      this.localDeclarationRemover.Traverse(block);
    }

    LocalDeclarationRemover localDeclarationRemover = new LocalDeclarationRemover();

    class LocalDeclarationRemover : CodeTraverser {

      internal ILocalDefinition localVariableToRemove;

      public override void TraverseChildren(IBlockStatement block) {
        Contract.Assume(block is BlockStatement);
        var b = (BlockStatement)block;
        for (int i = 0, n = b.Statements.Count; i < n; i++) {
          var localDecl = b.Statements[i] as LocalDeclarationStatement;
          if (localDecl == null || localDecl.LocalVariable != this.localVariableToRemove) continue;
          b.Statements.RemoveAt(i);
          this.StopTraversal = true;
          return;
        }
        this.Traverse(b.Statements);
      }
    }

    private ILocalDefinition ExtractExceptionContainer(DecompiledBlock nestedBlock, ITypeReference exceptionType) {
      Contract.Requires(nestedBlock != null);
      Contract.Requires(exceptionType != null);
      Contract.Ensures(Contract.Result<ILocalDefinition>() != null);

      Contract.Assume(nestedBlock.Statements.Count > 0);
      int i = 0;
      while (nestedBlock.Statements[i] is LocalDeclarationStatement) { i++; Contract.Assume(i < nestedBlock.Statements.Count); };
      var firstStatement = nestedBlock.Statements[i++];
      var firstBlock = firstStatement as DecompiledBlock;
      while (firstBlock != null) {
        Contract.Assume(firstBlock.Statements.Count > 0);
        i = 0;
        while (firstBlock.Statements[i] is LocalDeclarationStatement) { i++; Contract.Assume(i < firstBlock.Statements.Count); };
        firstStatement = firstBlock.Statements[i++];
        nestedBlock = firstBlock;
        firstBlock = firstStatement as DecompiledBlock;
      }
      //Ignoring any local declarations inserted for lexical scopes, any decompiled block that does not start with a nested block, starts with a label.
      Contract.Assume(firstStatement is LabeledStatement);
      if (nestedBlock.Statements.Count > i) {
        var exprStatement = nestedBlock.Statements[i] as ExpressionStatement;
        if (exprStatement != null) {
          nestedBlock.Statements.RemoveRange(i-1, 2);
          if (exprStatement.Expression is PopValue) return Dummy.LocalVariable;
          var assignment = exprStatement.Expression as Assignment;
          if (assignment != null && assignment.Source is PopValue) {
            var local = assignment.Target.Definition as ILocalDefinition;
            if (local != null) return local; //if not, this is not a recognized code pattern.
          }
        }
        // can't find the local, so just introduce one and leave its value on the stack
        var ld = new LocalDefinition() {
          Type = exceptionType,
        };
        var pushStatement = new PushStatement() {
          ValueToPush = new BoundExpression() {
            Definition = ld,
            Type = exceptionType,
          },
        };
        nestedBlock.Statements.Insert(0, pushStatement);
        return ld;
      } else {
        //Valid IL should always have at least one instruction to consume the exception value as well as a branch out of the handler block.
        Contract.Assume(false);
        return Dummy.LocalVariable;
      }
    }

    private IExpression/*?*/ GetFilterCondition(DecompiledBlock block) {
      Contract.Requires(block != null);
      BlockExpression result = null;
      List<IStatement> statements = null;
      foreach (var statement in block.Statements) {
        var nestedBlock = statement as DecompiledBlock;
        if (nestedBlock != null) {
          var nestedResult = this.GetFilterCondition(nestedBlock);
          if (nestedResult != null) {
            if (result == null) return nestedResult;
            result.Expression = nestedResult;
            return result;
          }
        } else {
          var endFilter = statement as EndFilter;
          if (endFilter != null) {
            if (result == null) return endFilter.FilterResult;
            result.Expression = endFilter.FilterResult;
            return result;
          }
        }
        if (statements == null) {
          statements = new List<IStatement>();
          result = new BlockExpression() { BlockStatement = new BlockStatement() { Statements = statements } };
        }
        statements.Add(statement);
      }
      if (result != null && result.Expression == CodeDummy.Expression) return null;
      return result;
    }

    private bool RemovedFilterCondition(DecompiledBlock block) {
      Contract.Requires(block != null);
      for (int i = 0; i < block.Statements.Count; i++) {
        var statement = block.Statements[i];
        var nestedBlock = statement as DecompiledBlock;
        if (nestedBlock != null) {
          if (this.RemovedFilterCondition(nestedBlock)) return true;
        }
        block.Statements.RemoveAt(i--);
        if (statement is EndFilter) return true;
      }
      return false;
    }

    private static List<IStatement> CopyStatements(List<IStatement> statements, int n) {
      Contract.Requires(statements != null);
      Contract.Requires(0 <= n && n < statements.Count);
      Contract.Ensures(Contract.Result<List<IStatement>>() != null);

 	    var result = new List<IStatement>(statements.Count);
      for (int i = 0; i < n; i++) {
        Contract.Assume(i < statements.Count);
        result.Add(statements[i]);
      }
      return result;
    }

  }

}

