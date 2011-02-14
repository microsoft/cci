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

namespace Microsoft.Cci.ILToCodeModel {

  internal abstract class ControlFlowDecompiler : BaseCodeTraverser {

    protected IPlatformType platformType;
    protected Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors;

    internal ControlFlowDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors) {
      this.platformType = platformType;
      this.predecessors = predecessors;
    }

    public override void Visit(IBlockStatement block) {
      this.Visit(block.Statements);
      BasicBlock b = (BasicBlock)block;
      this.Visit(b);
    }

    protected abstract void Visit(BasicBlock b);

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
        if (bbb == null) break;
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

    protected BasicBlock ExtractBasicBlockUpto(BasicBlock b, int i, ILabeledStatement label) {
      List<IStatement> statements = b.Statements;
      int n = statements.Count;
      if (i == n-1) {
        var bb = statements[i] as BasicBlock;
        if (bb != null) return this.ExtractBasicBlockUpto(bb, 0, label);
      }
      BasicBlock result = new BasicBlock(0);
      if (b.LocalVariables != null)
        result.LocalVariables = new List<ILocalDefinition>(b.LocalVariables);
      for (int j = i; j < n; j++) {
        IStatement s = statements[j];
        if (s == label) {
          statements.RemoveRange(i, j-i);
          return result;
        }
        BasicBlock/*?*/ bb = s as BasicBlock;
        if (bb == null) {
          MoveTempIfNecessary(b, result, s);
          result.Statements.Add(s);
          continue;
        }
        BasicBlock bb2 = this.ExtractBasicBlockUpto(bb, 0, label);
        if (bb2.Statements.Count > 0)
          result.Statements.Add(bb2);
        statements.RemoveRange(i, j-i);
        return result;
      }
      return result;
    }

  }

  internal class IfThenElseDecompiler : ControlFlowDecompiler {

    internal IfThenElseDecompiler(IPlatformType platformType, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors)
      : base(platformType, predecessors) {
    }

    protected override void Visit(BasicBlock b) {
      for (int i = 0; i < b.Statements.Count; i++) {
        this.DecompileIfThenElseStatement(b, i);
      }
    }

    private void DecompileIfThenElseStatement(BasicBlock b, int i) {
      List<IStatement> statements = b.Statements;
      if (i >= statements.Count) return;
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
      this.Visit(ifBlock);
      BasicBlock elseBlock = null;
      if (gotoEndif != null) {
        var endif = this.FindLabeledStatement(statements, i, gotoEndif.TargetStatement.Label);
        if (endif != null) {
          if (this.predecessors.TryGetValue(gotoEndif.TargetStatement, out branchesToThisLabel))
            branchesToThisLabel.Remove(gotoEndif);
          elseBlock = this.ExtractBasicBlockUpto(b, i, gotoEndif.TargetStatement);
          elseBlock.Statements.Add(gotoEndif.TargetStatement);
          this.Visit(elseBlock);
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

    protected override void Visit(BasicBlock b) {
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

    protected override void Visit(BasicBlock b) {
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

}
