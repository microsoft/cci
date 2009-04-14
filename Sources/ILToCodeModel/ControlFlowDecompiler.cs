//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using Microsoft.Cci.MutableCodeModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.Cci.ILToCodeModel {

  internal class ControlFlowDecompiler : BaseCodeTraverser {

    IPlatformType platformType;

    internal ControlFlowDecompiler(IPlatformType platformType) {
      this.platformType = platformType;
    }

    public override void Visit(IBlockStatement block) {
      this.Visit(block.Statements);
      BasicBlock b = (BasicBlock)block;
      this.Visit(b);
    }

    private void Visit(BasicBlock b){
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
                DecompileHandler(bb, bbb, tryStatement);
                break;
              }
              if (bbb.Statements.Count == 0) break;
              bb = bbb;
              bbb = bbb.Statements[bbb.Statements.Count-1] as BasicBlock;
            }
          }
        }
        tryStatement.TryBody = this.GetBasicBlockUpto(b, firstHandler.StartOffset);
        b.Statements.Insert(0, tryStatement);
      }
      for (int i = 0; i < b.Statements.Count; i++) {
        if (DecompileIfThenElseStatement(b.Statements, i)) continue;
        this.DecompileIfThenStatement(b.Statements, i);
        this.DecompileIfThenStatement2(b.Statements, i);
        this.DecompileSwitch(b.Statements, i);
      }
    }

    private BasicBlock GetBasicBlockUpto(BasicBlock b, uint endOffset) {
      BasicBlock result = new BasicBlock(b.StartOffset);
      result.EndOffset = endOffset;
      int n = b.Statements.Count;
      for (int i = 0; i < n-1; i++)
        result.Statements.Add(b.Statements[i]);
      if (n > 0) {
        b.Statements.RemoveRange(0, n-1);
        BasicBlock bb = (BasicBlock)b.Statements[0];
        if (bb.StartOffset < endOffset)
          result.Statements.Add(this.GetBasicBlockUpto(bb, endOffset));
      }
      return result;
    }

    private static void DecompileHandler(BasicBlock containingBlock, BasicBlock handlerBlock, TryCatchFinallyStatement tryStatement) {
      if (handlerBlock.ExceptionInformation.HandlerKind == HandlerKind.Finally) {
        tryStatement.FinallyBody = handlerBlock;
      } else {
        CatchClause catchClause = new CatchClause();
        catchClause.Body = handlerBlock;
        catchClause.ExceptionType = handlerBlock.ExceptionInformation.ExceptionType;
        if (handlerBlock.ExceptionInformation.HandlerKind == HandlerKind.Catch) {
          catchClause.ExceptionContainer = ExtractExceptionContainer(handlerBlock);
        }
        tryStatement.CatchClauses.Add(catchClause);
      }
      //Remove handler from statements in containing block 
      containingBlock.Statements[containingBlock.Statements.Count-1] = GetBasicBlockStartingAt(handlerBlock, handlerBlock.ExceptionInformation.HandlerEndOffset);
      RemoveEndFinally(handlerBlock);
    }

    private static void RemoveEndFinally(BasicBlock handlerBlock) {
      int i = handlerBlock.Statements.Count-1;
      if (i >= 0 && handlerBlock.Statements[i] is EndFinally)
        handlerBlock.Statements.RemoveAt(i);
    }

    private static ILocalDefinition ExtractExceptionContainer(BasicBlock bb) {
      ILocalDefinition result = Dummy.LocalVariable;
      if (bb.Statements.Count < 1) return result;
      ExpressionStatement es = bb.Statements[0] as ExpressionStatement;
      if (es == null) return result;
      Assignment assign = es.Expression as Assignment;
      if (assign == null) return result;
      if (!(assign.Source is Pop)) return result;
      if (!(assign.Target.Definition is ILocalDefinition)) return result;
      result = (ILocalDefinition)assign.Target.Definition;
      bb.Statements.RemoveAt(0);
      if (bb.Statements.Count > 0) {
        BasicBlock nbb = bb.Statements[0] as BasicBlock;
        if (nbb != null && nbb.LocalVariables != null)
          nbb.LocalVariables.Remove(result);
      }
      return result;
    }

    private static BasicBlock GetBasicBlockStartingAt(BasicBlock bb, uint offset) {
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

    private static bool DecompileIfThenElseStatement(List<IStatement> statements, int i) {
      if (i >= statements.Count) return false;
      ConditionalStatement/*?*/ conditionalStatement = statements[i++] as ConditionalStatement;
      if (conditionalStatement == null) return false;
      GotoStatement/*?*/ gotoElse = conditionalStatement.TrueBranch as GotoStatement;
      if (gotoElse == null) return false;
      if (!(conditionalStatement.FalseBranch is EmptyStatement)) return false;
      GotoStatement/*?*/ gotoEndif = null;
      int j = i;
      while (j < statements.Count) {
        gotoEndif = statements[j++] as GotoStatement;
        if (gotoEndif != null) break;
      }
      if (gotoEndif == null || j >= statements.Count) return false;
      BasicBlock/*?*/ falseBlock = statements[j] as BasicBlock;
      if (falseBlock == null || falseBlock.Statements.Count < 1) return false;
      LabeledStatement/*?*/ elseLabel = falseBlock.Statements[0] as LabeledStatement;
      if (elseLabel == null || gotoElse.TargetStatement != elseLabel) return false;
      BasicBlock/*?*/ blockAfterIf = null;
      int k = 1;
      while (k < falseBlock.Statements.Count) {
        blockAfterIf = falseBlock.Statements[k] as BasicBlock;
        if (blockAfterIf != null && blockAfterIf.Statements.Count > 0 && blockAfterIf.Statements[0] == gotoEndif.TargetStatement)
          break;
        k++;
      }
      if (blockAfterIf == null || k >= falseBlock.Statements.Count) return false;
      BasicBlock ifBlock = ExtractAsBasicBlock(statements, i, j-1);
      BasicBlock elseBlock = ExtractAsBasicBlock(falseBlock.Statements, 1, k);
      LogicalNot not = new LogicalNot();
      not.Operand = conditionalStatement.Condition;
      conditionalStatement.Condition = not;
      conditionalStatement.TrueBranch = ifBlock;
      conditionalStatement.FalseBranch = elseBlock;
      statements.RemoveRange(i, j-i);
      falseBlock.Statements.RemoveRange(0, k);
      blockAfterIf.Statements.RemoveAt(0);
      return true;
    }

    private bool DecompileIfThenStatement(List<IStatement> statements, int i) {
      if (i >= statements.Count) return false;
      ConditionalStatement/*?*/ conditionalStatement = statements[i++] as ConditionalStatement;
      if (conditionalStatement == null) return false;
      GotoStatement/*?*/ gotoAfterThen = conditionalStatement.TrueBranch as GotoStatement;
      if (gotoAfterThen == null) return false;
      if (!(conditionalStatement.FalseBranch is EmptyStatement)) return false;
      IName afterThenLabelName = gotoAfterThen.TargetStatement.Label;
      LabeledStatement/*?*/ afterThenLabel = this.FindLabeledStatement(statements, i, afterThenLabelName);
      if (afterThenLabel == null) return false;
      // TODO? Check the putative ifBlock to make sure it is self-contained: i.e., it has no branches outside of itself
      BasicBlock ifBlock = this.ExtractBasicBlockUpto(statements, i, afterThenLabel);
      this.Visit(ifBlock);
      LogicalNot not = new LogicalNot();
      not.Operand = conditionalStatement.Condition;
      conditionalStatement.Condition = not;
      conditionalStatement.TrueBranch = ifBlock;
      conditionalStatement.FalseBranch = new EmptyStatement();
      return true;
    }

    private LabeledStatement/*?*/ FindLabeledStatement(List<IStatement> statements, int i, IName name) {
      while (i < statements.Count) {
        BasicBlock/*?*/ bb = statements[i] as BasicBlock;
        if (bb != null) return this.FindLabeledStatement(bb.Statements, 0, name);
        LabeledStatement/*?*/ result = statements[i] as LabeledStatement;
        if (result != null && result.Label == name) return result;
        i++;
      }
      return null;
    }

    private bool DecompileIfThenStatement2(List<IStatement> statements, int i) {
      if (i >= statements.Count) return false;
      ConditionalStatement/*?*/ conditionalStatement = statements[i++] as ConditionalStatement;
      if (conditionalStatement == null) return false;
      if (!(conditionalStatement.TrueBranch is EmptyStatement)) return false;
      GotoStatement/*?*/ gotoAfterElse = conditionalStatement.FalseBranch as GotoStatement;
      if (gotoAfterElse == null) return false;
      BasicBlock afterThen = this.ExtractBasicBlockUpto(statements, i, gotoAfterElse.TargetStatement);
      conditionalStatement.FalseBranch = conditionalStatement.TrueBranch; //empty statement
      conditionalStatement.TrueBranch = afterThen;
      return true;
    }

    private void DecompileSwitch(List<IStatement> statements, int i) {
      if (i >= statements.Count-2) return;
      SwitchInstruction/*?*/ switchInstruction = statements[i] as SwitchInstruction;
      if (switchInstruction == null) return;
      SwitchStatement result = new SwitchStatement();
      result.Expression = switchInstruction.switchExpression;
      statements[i] = result;
      statements.RemoveAt(i+1);
      for (int j = 0, n = switchInstruction.switchCases.Count; j < n; j++) {
        CompileTimeConstant caseLabel = new CompileTimeConstant() { Value = j, Type = this.platformType.SystemInt32 };
        BasicBlock currentCaseBody = switchInstruction.switchCases[j];
        SwitchCase currentCase = new SwitchCase() { Expression = caseLabel };
        result.Cases.Add(currentCase);
        if (j < n-1 && currentCaseBody == switchInstruction.switchCases[j+1]) continue;
        ExtractCaseBody(currentCaseBody, currentCase.Body);
      }
    }

    private static void ExtractCaseBody(BasicBlock caseBody, List<IStatement> caseStatements) {
      List<IStatement> body = caseBody.Statements;
      if (body.Count == 0) return;
      ILabeledStatement labeledStatement = body[0] as ILabeledStatement;
      if (labeledStatement != null) {
        caseStatements.Add(new GotoStatement() { TargetStatement = labeledStatement });
        return;
      }
    }

    private static BasicBlock ExtractAsBasicBlock(List<IStatement> statements, int i, int j) {
      BasicBlock result = new BasicBlock(0);
      while (i < j) result.Statements.Add(statements[i++]);
      return result;
    }

    private BasicBlock ExtractBasicBlockUpto(List<IStatement> statements, int i, ILabeledStatement label) {
      BasicBlock result = new BasicBlock(0);
      for (int j = i, n = statements.Count; j < n; j++) {
        IStatement s = statements[j];
        if (s == label){
          statements.RemoveRange(i, j-i);
          return result;
        }
        BasicBlock/*?*/ bb = s as BasicBlock;
        if (bb == null) {
          result.Statements.Add(s);
          continue;
        }
        BasicBlock bb2 = ExtractBasicBlockUpto(bb.Statements, 0, label);
        result.Statements.Add(bb2);
        statements.RemoveRange(i, j-i);
        return result;
      }
      return result;
    }

  }
}
