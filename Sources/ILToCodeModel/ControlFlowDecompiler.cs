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
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Cci.UtilityDataStructures;

namespace Microsoft.Cci.ILToCodeModel {

  internal abstract class ControlFlowDecompiler : CodeTraverser {

    protected IPlatformType platformType;
    protected Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors;

    internal ControlFlowDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors) {
      this.platformType = platformType;
      this.predecessors = predecessors;
    }

    public override void TraverseChildren(IBlockStatement block) {
      this.Traverse(block.Statements);
      BasicBlock b = (BasicBlock)block;
      this.Traverse(b);
    }

    protected abstract void Traverse(BasicBlock b);

    protected static BasicBlock GetBasicBlockUpto(BasicBlock b, uint endOffset) {
      BasicBlock result = new BasicBlock(b.StartOffset);
      if (b.LocalVariables != null)
        result.LocalVariables = new List<ILocalDefinition>(b.LocalVariables);
      result.EndOffset = endOffset;
      int n = b.Statements.Count;
      for (int i = 0; i < n-1; i++) {
        var s = b.Statements[i];
        MoveTempIfNecessary(b, result, s);
        result.Statements.Add(b.Statements[i]);
      }
      if (n > 0) {
        b.Statements.RemoveRange(0, n-1);
        BasicBlock bb = (BasicBlock)b.Statements[0];
        if (bb.StartOffset < endOffset)
          result.Statements.Add(GetBasicBlockUpto(bb, endOffset));
      }
      return result;
    }

    protected static BasicBlock GetBasicBlockStartingAt(BasicBlock bb, uint offset) {
      while (bb.StartOffset < offset && bb.Statements.Count > 0) {
        BasicBlock bbb = bb.Statements[bb.Statements.Count-1] as BasicBlock;
        if (bbb == null) return new BasicBlock(offset);
        if (bbb.StartOffset == offset) {
          bb.Statements.RemoveAt(bb.Statements.Count-1);
          return bbb;
        }
        bb = bbb;
      }
      return bb;
    }

    protected IStatement UnwrapSingletonBlock(IStatement statement) {
      var bb = statement as BasicBlock;
      if (bb != null && bb.Statements.Count == 1) return bb.Statements[0];
      return statement;
    }

    protected LabeledStatement/*?*/ FindLabeledStatement(List<IStatement> statements, int i, IName name) {
      while (i < statements.Count) {
        BasicBlock/*?*/ bb = statements[i] as BasicBlock;
        if (bb != null) return this.FindLabeledStatement(bb.Statements, 0, name);
        LabeledStatement/*?*/ result = statements[i] as LabeledStatement;
        if (result != null && result.Label == name) return result;
        i++;
      }
      return null;
    }

    protected GotoStatement/*?*/ RemoveAndReturnLastGotoStatement(BasicBlock/*?*/ block) {
      while (block != null) {
        List<IStatement> statements = block.Statements;
        var n = statements.Count;
        if (n == 0) return null;
        var lastStatementInBlock = statements[n-1];
        var gotoStatement = lastStatementInBlock as GotoStatement;
        if (gotoStatement != null) {
          statements.RemoveAt(n-1);
          return gotoStatement;
        }
        block = lastStatementInBlock as BasicBlock;
      }
      return null;
    }

    protected static BasicBlock ExtractAsBasicBlock(BasicBlock b, int i, int j) {
      List<IStatement> statements = b.Statements;
      BasicBlock result = new BasicBlock(0);
      if (b.LocalVariables != null)
        result.LocalVariables = new List<ILocalDefinition>(b.LocalVariables);
      while (i < j) {
        var s = statements[i++];
        MoveTempIfNecessary(b, result, s);
        result.Statements.Add(s);
      }
      return result;
    }

    private static void MoveTempIfNecessary(BasicBlock b, BasicBlock result, IStatement s) {
      var exprS = s as IExpressionStatement;
      if (exprS != null) {
        var assignment = exprS.Expression as IAssignment;
        if (assignment != null) {
          var tempVar = assignment.Target.Definition as TempVariable;
          if (tempVar != null && b.LocalVariables != null && b.LocalVariables.Contains(tempVar)) {
            b.LocalVariables.Remove(tempVar);
            if (result.LocalVariables == null) result.LocalVariables = new List<ILocalDefinition>();
            result.LocalVariables.Add(tempVar);
          }
        }
      }
    }

    protected BasicBlock ExtractBasicBlockUpto(BasicBlock b, int i, IStatement statement) {
      List<IStatement> statements = b.Statements;
      int n = statements.Count;
      if (i == n-1) {
        var bb = statements[i] as BasicBlock;
        if (bb != null) return this.ExtractBasicBlockUpto(bb, 0, statement);
      }
      BasicBlock result = new BasicBlock(0);
      if (b.LocalVariables != null)
        result.LocalVariables = new List<ILocalDefinition>(b.LocalVariables);
      for (int j = i; j < n; j++) {
        IStatement s = statements[j];
        if (s == statement) {
          statements.RemoveRange(i, j-i);
          return result;
        }
        BasicBlock/*?*/ bb = s as BasicBlock;
        if (bb == null) {
          MoveTempIfNecessary(b, result, s);
          result.Statements.Add(s);
          continue;
        }
        BasicBlock bb2 = this.ExtractBasicBlockUpto(bb, 0, statement);
        if (bb2.Statements.Count > 0)
          result.Statements.Add(bb2);
        statements.RemoveRange(i, j-i);
        return result;
      }
      return result;
    }

    protected static void RemoveStatement(BasicBlock block, IStatement statement) {
      while (block != null) {
        for (int i = 0, n = block.Statements.Count; i < n; i++) {
          if (block.Statements[i] == statement) {
            block.Statements.RemoveAt(i);
            return;
          } else if (i == n-1) {
            block = block.Statements[i] as BasicBlock;
          }
        }
      }
    }

  }

  internal class IfThenElseDecompiler : ControlFlowDecompiler {

    internal IfThenElseDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors)
      : base(platformType, predecessors){
    }

    protected override void Traverse(BasicBlock b) {
      for (int i = 0; i < b.Statements.Count; i++) {
        this.DecompileIfThenElseStatement(b, i);
      }
    }

    private void DecompileIfThenElseStatement(BasicBlock b, int i) {
      List<IStatement> statements = b.Statements;
      ConditionalStatement/*?*/ conditionalStatement = statements[i++] as ConditionalStatement;
      if (conditionalStatement == null) return;
      IExpression condition;
      var trueBranch = UnwrapSingletonBlock(conditionalStatement.TrueBranch);
      GotoStatement/*?*/ gotoAfterThen = trueBranch as GotoStatement;
      if (gotoAfterThen == null) {
        gotoAfterThen = UnwrapSingletonBlock(conditionalStatement.FalseBranch) as GotoStatement;
        if (gotoAfterThen == null || !(trueBranch is EmptyStatement)) return;
        condition = conditionalStatement.Condition;
      } else {
        if (!(conditionalStatement.FalseBranch is EmptyStatement)) return;
        LogicalNot not = new LogicalNot();
        not.Operand = conditionalStatement.Condition;
        condition = not;
      }
      //At this point we have:
      //if (!condition) goto afterThen;
      var afterThen = this.FindLabeledStatement(statements, i, gotoAfterThen.TargetStatement.Label);
      if (afterThen == null) return;
      List<IGotoStatement> branchesToThisLabel;
      if (this.predecessors.TryGetValue(afterThen, out branchesToThisLabel))
        branchesToThisLabel.Remove(gotoAfterThen);
      BasicBlock ifBlock = this.ExtractBasicBlockUpto(b, i, afterThen);
      GotoStatement/*?*/ gotoEndif = this.RemoveAndReturnLastGotoStatement(ifBlock);
      this.Traverse(ifBlock);
      BasicBlock elseBlock = null;
      if (gotoEndif != null) {
        var endif = this.FindLabeledStatement(statements, i, gotoEndif.TargetStatement.Label);
        if (endif != null) {
          if (this.predecessors.TryGetValue(gotoEndif.TargetStatement, out branchesToThisLabel))
            branchesToThisLabel.Remove(gotoEndif);
          elseBlock = this.ExtractBasicBlockUpto(b, i, gotoEndif.TargetStatement);
          elseBlock.Statements.Add(gotoEndif.TargetStatement);
          this.Traverse(elseBlock);
          elseBlock.Statements.Remove(gotoEndif.TargetStatement);
        } else {
          ifBlock.Statements.Add(gotoEndif);
        }
      }
      conditionalStatement.Condition = condition;
      conditionalStatement.TrueBranch = ifBlock;
      if (elseBlock != null)
        conditionalStatement.FalseBranch = elseBlock;
      else
        conditionalStatement.FalseBranch = new EmptyStatement();
      return;
    }

  }

  internal class SwitchDecompiler : ControlFlowDecompiler {

    internal SwitchDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors)
      : base(platformType, predecessors) {
    }

    protected override void Traverse(BasicBlock b) {
      for (int i = 0; i < b.Statements.Count; i++) {
        this.DecompileSwitch(b.Statements, i);
      }
    }

    private void DecompileSwitch(List<IStatement> statements, int i) {
      if (i >= statements.Count-1) return;
      SwitchInstruction/*?*/ switchInstruction = statements[i] as SwitchInstruction;
      if (switchInstruction == null) return;
      SwitchStatement result = new SwitchStatement();
      result.Expression = switchInstruction.switchExpression;
      statements[i] = result;
      for (int j = 0, n = switchInstruction.switchCases.Count; j < n; j++) {
        CompileTimeConstant caseLabel = new CompileTimeConstant() { Value = j, Type = this.platformType.SystemInt32 };
        var gotoCaseBody = switchInstruction.switchCases[j];
        SwitchCase currentCase = new SwitchCase() { Expression = caseLabel };
        result.Cases.Add(currentCase);
        if (j < n-1 && gotoCaseBody.TargetStatement == switchInstruction.switchCases[j+1].TargetStatement) continue;
        currentCase.Body.Add(gotoCaseBody);
      }
      if (i == statements.Count-1) return;
      var gotoStatement = statements[i+1] as IGotoStatement;
      if (gotoStatement != null) {
        SwitchCase defaultCase = new SwitchCase() { }; // Default case is represented by a dummy Expression.
        defaultCase.Body.Add(statements[i + 1]);
        statements.RemoveAt(i + 1);
        result.Cases.Add(defaultCase);
      }
    }


  }

  internal class TryCatchDecompiler : ControlFlowDecompiler {

    internal TryCatchDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors)
      : base(platformType, predecessors) {
    }

    protected override void Traverse(BasicBlock b) {
      if (b.NumberOfTryBlocksStartingHere > 0) {
        BasicBlock firstHandler = null;
        TryCatchFinallyStatement/*?*/ tryStatement = new TryCatchFinallyStatement();
        BasicBlock bb = b;
        while (b.NumberOfTryBlocksStartingHere-- > 0) {
          if (bb.Statements.Count > 0) {
            BasicBlock bbb = bb.Statements[bb.Statements.Count-1] as BasicBlock;
            while (bbb != null) {
              if (bbb.ExceptionInformation != null) {
                if (firstHandler == null) firstHandler = bbb;
                if (firstHandler.ExceptionInformation.TryEndOffset < bbb.ExceptionInformation.TryEndOffset) {
                  DecompileTryBody(b, firstHandler, tryStatement);
                  tryStatement = new TryCatchFinallyStatement();
                  firstHandler = bbb;
                }
                DecompileHandler(bb, bbb, tryStatement);
                break;
              }
              if (bbb.Statements.Count == 0) break;
              bb = bbb;
              bbb = bbb.Statements[bbb.Statements.Count-1] as BasicBlock;
            }
          }
        }
        DecompileTryBody(b, firstHandler, tryStatement);
      }
    }

    private void DecompileTryBody(BasicBlock b, BasicBlock firstHandler, TryCatchFinallyStatement tryStatement) {
      tryStatement.TryBody = GetBasicBlockUpto(b, firstHandler.StartOffset);
      BasicBlock tryBody = tryStatement.TryBody as BasicBlock;
      int startPoint = 0;
      if (tryBody != null && tryBody.Statements.Count > 0) {
        ILabeledStatement labeledStatement = tryBody.Statements[0] as ILabeledStatement;
        if (labeledStatement != null) {
          tryBody.Statements.RemoveAt(0);
          b.Statements.Insert(startPoint, labeledStatement);
          startPoint++;
        }
      }
      b.Statements.Insert(startPoint, tryStatement);
    }

    private static void DecompileHandler(BasicBlock containingBlock, BasicBlock handlerBlock, TryCatchFinallyStatement tryStatement) {
      if (handlerBlock.ExceptionInformation.HandlerKind == HandlerKind.Finally) {
        tryStatement.FinallyBody = handlerBlock;
      } else if (handlerBlock.ExceptionInformation.HandlerKind == HandlerKind.Fault) {
        tryStatement.FaultBody = handlerBlock;
      } else {
        CatchClause catchClause = new CatchClause();
        catchClause.Body = handlerBlock;
        catchClause.ExceptionType = handlerBlock.ExceptionInformation.ExceptionType;
        if (handlerBlock.ExceptionInformation.HandlerKind == HandlerKind.Catch) {
          catchClause.ExceptionContainer = ExtractExceptionContainer(handlerBlock);
        } else if (handlerBlock.ExceptionInformation.HandlerKind == HandlerKind.Filter) {
          catchClause.FilterCondition = ExtractFilterCondition(handlerBlock);
        }
        tryStatement.CatchClauses.Add(catchClause);
      }
      //Remove handler from statements in containing block 
      containingBlock.Statements[containingBlock.Statements.Count-1] = GetBasicBlockStartingAt(handlerBlock, handlerBlock.ExceptionInformation.HandlerEndOffset);
      RemoveEndFinally(handlerBlock);
    }

    private static void RemoveEndFinally(BasicBlock handlerBlock) {
      while (handlerBlock != null) {
        int i = handlerBlock.Statements.Count-1;
        if (i < 0) return;
        if (handlerBlock.Statements[i] is EndFinally) {
          handlerBlock.Statements.RemoveAt(i);
          return;
        } else {
          handlerBlock = handlerBlock.Statements[i] as BasicBlock;
        }
      }
    }

    private static IExpression ExtractFilterCondition(BasicBlock handlerBlock) {
      int endFilterIndex = -1;
      IExpression result = CodeDummy.Expression;
      for (int i = 0; i < handlerBlock.Statements.Count; i++) {
        var endFilter = handlerBlock.Statements[i] as EndFilter;
        if (endFilter != null) {
          result = endFilter.FilterResult;
          endFilterIndex = i;
          break;
        }
      }
      if (endFilterIndex < 0) return result;
      if (endFilterIndex == 0) {
        handlerBlock.Statements.RemoveAt(0);
        return result;
      }
      var blockExpression = new BlockExpression();
      blockExpression.BlockStatement = ExtractAsBasicBlock(handlerBlock, 0, endFilterIndex);
      blockExpression.Expression = result;
      blockExpression.Type = result.Type;
      handlerBlock.Statements.RemoveRange(0, endFilterIndex+1);
      return blockExpression;
    }

    private static ILocalDefinition ExtractExceptionContainer(BasicBlock bb) {
      ILocalDefinition result = Dummy.LocalVariable;
      if (bb.Statements.Count < 1) return result;
      ExpressionStatement es = bb.Statements[0] as ExpressionStatement;
      if (es == null) return result;
      if (es.Expression is Pop) {
        bb.Statements.RemoveAt(0);
        return result;
      }
      Assignment assign = es.Expression as Assignment;
      if (assign == null) return result;
      if (!(assign.Source is Pop)) return result;
      if (!(assign.Target.Definition is ILocalDefinition)) return result;
      result = (ILocalDefinition)assign.Target.Definition;
      bb.Statements.RemoveAt(0);
      if (bb.LocalVariables != null && bb.LocalVariables.Remove(result))
        return result;
      if (bb.Statements.Count > 0) {
        BasicBlock nbb = bb.Statements[0] as BasicBlock;
        if (nbb != null && nbb.LocalVariables != null)
          nbb.LocalVariables.Remove(result);
      }
      return result;
    }

  }

  internal class WhileLoopDecompiler : ControlFlowDecompiler {

    internal WhileLoopDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors)
      : base(platformType, predecessors){
    }

    SetOfObjects gotosAlreadyTraversed = new SetOfObjects();
    BasicBlock currentBlock;
    IConditionalStatement currentIf;
    BasicBlock blockContainingCurrentIf;
    BasicBlock blockContainingLoopHeader;
    ILabeledStatement loopHeader;
    IGotoStatement backwardsBranch;
    IConditionalStatement ifContainingBackwardsBranch;
    BasicBlock blockContainingIfContainingBackwardsBranch;

    public override void TraverseChildren(IBlockStatement block) {
      var savedCurrentBlock = this.currentBlock;
      var bb = (BasicBlock)block;
      this.currentBlock = bb;
      while (true) {
        var n = bb.Statements.Count;
        if (n == 0) break;
        for (int i = 0; i < n-1; i++)
          this.Traverse(bb.Statements[i]);
        var bbb = bb.Statements[n-1] as BasicBlock;
        if (bbb == null || this.loopHeader == null) {
          this.Traverse(bb.Statements[n-1]);
          break;
        }
        bb = bbb;
      }
      this.Traverse(this.currentBlock);
      this.currentBlock = savedCurrentBlock;
    }

    protected override void Traverse(BasicBlock b) {
      if (b == this.blockContainingLoopHeader && this.ifContainingBackwardsBranch != null && this.blockContainingIfContainingBackwardsBranch == b) {
        int i = 0;
        while (b.Statements[i] != this.loopHeader) i++;
        var loopBody = this.ExtractBasicBlockUpto(b, i+1, this.ifContainingBackwardsBranch);
        b.Statements[i] = new WhileDoStatement() { Body = loopBody, Condition = this.ifContainingBackwardsBranch.Condition};
        RemoveStatement(b, this.ifContainingBackwardsBranch);
        new WhileLoopDecompiler(this.platformType, this.predecessors).Traverse(loopBody);
      }
    }

    public override void TraverseChildren(IConditionalStatement conditionalStatement) {
      this.currentIf = conditionalStatement;
      this.blockContainingCurrentIf = this.currentBlock;
      base.TraverseChildren(conditionalStatement);
    }

    public override void TraverseChildren(IGotoStatement gotoStatement) {
      if (gotoStatement == this.backwardsBranch) {
        if (this.currentIf != null && this.currentIf.TrueBranch == this.currentBlock && this.currentIf.FalseBranch is EmptyStatement) {
          this.ifContainingBackwardsBranch = this.currentIf;
          this.blockContainingIfContainingBackwardsBranch = this.blockContainingCurrentIf;
        }
      }
      this.gotosAlreadyTraversed.Add(gotoStatement);
      base.TraverseChildren(gotoStatement);
    }

    public override void TraverseChildren(ILabeledStatement labeledStatement) {
      if (this.loopHeader == null) {
        List<IGotoStatement> predecessors;
        if (this.predecessors.TryGetValue(labeledStatement, out predecessors)) {
          if (predecessors.Count == 1 && !this.gotosAlreadyTraversed.Contains(predecessors[0])) {
            this.blockContainingLoopHeader = this.currentBlock;
            this.loopHeader = labeledStatement;
            this.backwardsBranch = predecessors[0];
            this.ifContainingBackwardsBranch = null;
          }
        }
      }
      base.TraverseChildren(labeledStatement);
    }

  }

  internal class ForLoopDecompiler : ControlFlowDecompiler {

    internal ForLoopDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors)
      : base(platformType, predecessors){
    }

    protected override void Traverse(BasicBlock b) {
      for (int i = 0; i < b.Statements.Count; i++) {
        this.DecompileForStatement(b, i);
      }
    }

    private void DecompileForStatement(BasicBlock b, int i) {
      List<IStatement> statements = b.Statements;
      if (i > b.Statements.Count-2) return;
      var gotoStatement = statements[i++] as GotoStatement;
      if (gotoStatement == null) return;
      var blockStatement = statements[i++] as BlockStatement;
      if (blockStatement == null) return;
      var n = blockStatement.Statements.Count;
      if (n == 0) return;
      var whileLoop = blockStatement.Statements[0] as WhileDoStatement;
      if (whileLoop == null) return;
      if (whileLoop.Condition is IMethodCall) return;
      var loopBody = (BasicBlock)whileLoop.Body;
      if (RemoveLastStatement(loopBody, gotoStatement.TargetStatement)) {
        var forLoop = new ForStatement() { Condition = whileLoop.Condition, Body = loopBody };
        if (i > 2) {
          var initializer = statements[i-3];
          forLoop.InitStatements.Add(initializer);
          statements.RemoveAt(i-3);
          i--;
        }
        var incrementer = FindLastStatement(loopBody) as ExpressionStatement;
        if (incrementer != null) {
          var assign = incrementer.Expression as Assignment;
          if (assign != null && (assign.Source is Addition || assign.Source is Subtraction)) {
            RemoveStatement(loopBody, incrementer);
            forLoop.IncrementStatements.Add(incrementer);
          }
        }
        statements[i-2] = forLoop;
        blockStatement.Statements.RemoveAt(0);
        new ForLoopDecompiler(this.platformType, this.predecessors).Traverse(loopBody);
      }
    }

    static bool RemoveLastStatement(BasicBlock block, IStatement statement) {
      while (block != null) {
        var i = block.Statements.Count-1;
        if (i < 0) return false;
        if ( block.Statements[i] == statement) {
          block.Statements.RemoveAt(i);
          return true;
        }
        block = block.Statements[i] as BasicBlock;
      }
      return false;
    }

    static IStatement FindLastStatement(BasicBlock block) {
      IStatement result = null;
      while (block != null) {
        var i = block.Statements.Count-1;
        if (i < 0) return result;
        var nextBlock = block.Statements[i] as BasicBlock;
        if (nextBlock == null) return block.Statements[i];
        if (i > 0) result = block.Statements[i-1];
        block = nextBlock;
      }
      return null;
    }


  }

}
