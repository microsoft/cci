//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Cci.MutableCodeModel;
using System;

namespace Microsoft.Cci.ILToCodeModel {

  internal class PatternDecompiler : BaseCodeTraverser {

    INameTable nameTable;
    SourceMethodBody sourceMethodBody;
    List<ILocalDefinition> blockLocalVariables;
    Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors;

    internal PatternDecompiler(SourceMethodBody sourceMethodBody, Dictionary<ILabeledStatement, List<IGotoStatement>> predecessors) {
      this.nameTable = sourceMethodBody.nameTable;
      this.sourceMethodBody = sourceMethodBody;
      this.predecessors = predecessors;
    }

    public override void Visit(IBlockStatement block) {
      this.Visit(block.Statements);
      BasicBlock b = (BasicBlock)block;
      this.blockLocalVariables = b.LocalVariables;
      for (int i = 0; i < b.Statements.Count; i++) {
        DeleteGotoNextStatement(b.Statements, i);
        this.ReplaceLocalArrayInitializerPattern(b.Statements, i);
        this.ReplaceShortCircuitPattern(b.Statements, i);
        this.ReplaceShortCircuitPattern2(b.Statements, i);
        this.ReplacePushPopPattern(b.Statements, i);
        if (this.ReplacedPushDupLocPopPattern(b.Statements, i) && i > 0) {
          i--;
          if (this.ReplacedOpAssignPattern(b.Statements, i) && i > 0) {
            i--;
            this.ReplaceOpAssignExpressionPattern(b.Statements, i);
          }
        }
      }
      if (this.blockLocalVariables != b.LocalVariables)
        b.LocalVariables = this.blockLocalVariables;
    }

    private void ReplaceOpAssignExpressionPattern(List<IStatement> statements, int i) {
      if (i > statements.Count - 5) return;
      Push/*?*/ push = statements[i] as Push;
      if (push == null) return;
      ExpressionStatement/*?*/ expressionStatement = statements[i + 1] as ExpressionStatement;
      if (expressionStatement == null) return;
      Assignment/*?*/ assignment = expressionStatement.Expression as Assignment;
      if (assignment == null) return;
      ILocalDefinition/*?*/ local = assignment.Target.Definition as ILocalDefinition;
      if (local == null) return;
      ExpressionStatement/*?*/ expressionStatement2 = statements[i + 2] as ExpressionStatement;
      if (expressionStatement2 == null) return;
      Assignment/*?*/ assignment2 = expressionStatement2.Expression as Assignment;
      if (assignment2 == null) return;
      ILocalDefinition/*?*/ local2 = assignment2.Target.Definition as ILocalDefinition;
      if (local2 == null) return;
      ExpressionStatement/*?*/ expressionStatement3 = statements[i + 3] as ExpressionStatement;
      if (expressionStatement3 == null) return;
      Assignment/*?*/ assignment3 = expressionStatement3.Expression as Assignment;
      if (assignment3 == null) return;
      BoundExpression/*?*/ boundExpression = assignment3.Source as BoundExpression;
      if (boundExpression == null || boundExpression.Definition != local2) return;
      ExpressionStatement/*?*/ expressionStatement4 = statements[i + 4] as ExpressionStatement;
      if (expressionStatement4 == null) return;
      Assignment/*?*/ assignment4 = expressionStatement4.Expression as Assignment;
      if (assignment4 == null) return;
      BoundExpression/*?*/ boundExpression2 = assignment4.Source as BoundExpression;
      if (boundExpression2 == null || boundExpression2.Definition != local2) return;
      TargetExpression/*?*/ target = assignment4.Target as TargetExpression;
      if (!(target.Instance is Pop)) return;
      ILocalDefinition temp = new TempVariable() { Name = this.nameTable.GetNameFor("__temp_"+this.sourceMethodBody.localVariables.Count) };
      this.sourceMethodBody.localVariables.Add(temp);
      if (this.blockLocalVariables == null) this.blockLocalVariables = new List<ILocalDefinition>();
      this.blockLocalVariables.Add(temp);
      this.sourceMethodBody.numberOfReferences.Add(temp, 1);
      statements[i] = new ExpressionStatement() { Expression = new Assignment() { Target = new TargetExpression() { Definition = temp }, Source = push.ValueToPush } };
      target.Instance = new BoundExpression() { Definition = temp };
    }

    private bool ReplacedOpAssignPattern(List<IStatement> statements, int i) {
      if (i > statements.Count - 3) return false;
      Push/*?*/ push = statements[i] as Push;
      if (push == null) return false;
      ExpressionStatement/*?*/ expressionStatement = statements[i + 1] as ExpressionStatement;
      if (expressionStatement == null) return false;
      Assignment/*?*/ assignment = expressionStatement.Expression as Assignment;
      if (assignment == null) return false;
      ILocalDefinition/*?*/ local = assignment.Target.Definition as ILocalDefinition;
      if (local == null) return false;
      BinaryOperation/*?*/ binop = assignment.Source as BinaryOperation;
      if (binop == null) return false;
      BoundExpression/*?*/ boundExpression = binop.LeftOperand as BoundExpression;
      if (boundExpression == null) return false;
      if (!(boundExpression.Instance is Dup)) return false;
      CompileTimeConstant/*?*/ cconst = binop.RightOperand as CompileTimeConstant;
      if (cconst == null || !(cconst.Value is int) || ((int)cconst.Value) != 1) return false;
      ExpressionStatement/*?*/ expressionStatement2 = statements[i + 2] as ExpressionStatement;
      if (expressionStatement == null) return false;
      Assignment/*?*/ assignment2 = expressionStatement2.Expression as Assignment;
      if (assignment2 == null) return false;
      TargetExpression/*?*/ target2 = assignment2.Target as TargetExpression;
      if (target2 == null) return false;
      if (!(target2.Instance is Pop)) return false;
      if (target2.Definition != boundExpression.Definition) return false;
      BoundExpression/*?*/ boundExpression2 = assignment2.Source as BoundExpression;
      if (boundExpression2 == null) return false;
      if (boundExpression2.Definition != local) return false;
      ILocalDefinition temp = new TempVariable() { Name = this.nameTable.GetNameFor("__temp_"+this.sourceMethodBody.localVariables.Count) };
      this.sourceMethodBody.localVariables.Add(temp);
      if (this.blockLocalVariables == null) this.blockLocalVariables = new List<ILocalDefinition>();
      this.blockLocalVariables.Add(temp);
      this.sourceMethodBody.numberOfReferences.Add(temp, 2);
      statements[i] = new ExpressionStatement() { Expression = new Assignment() { Target = new TargetExpression() { Definition = temp }, Source = push.ValueToPush } };
      boundExpression.Instance = new BoundExpression() { Definition = temp };
      target2.Instance = boundExpression.Instance;
      return true;
    }

    private bool ReplacedPushDupLocPopPattern(List<IStatement> statements, int i) {
      if (i > statements.Count - 3) return false;
      Push/*?*/ push = statements[i] as Push;
      if (push == null) return false;
      ExpressionStatement/*?*/ expressionStatement = statements[i + 1] as ExpressionStatement;
      if (expressionStatement == null) return false;
      Assignment/*?*/ assignment = expressionStatement.Expression as Assignment;
      if (assignment == null) return false;
      Dup/*?*/ dup = assignment.Source as Dup;
      if (dup == null) return false;
      ILocalDefinition/*?*/ local = assignment.Target.Definition as ILocalDefinition;
      if (local == null) return false;
      ExpressionStatement/*?*/ expressionStatement2 = statements[i + 2] as ExpressionStatement;
      if (expressionStatement2 == null) return false;
      Assignment/*?*/ assignment2 = expressionStatement2.Expression as Assignment;
      if (assignment2 == null) return false;
      Pop/*?*/ pop = assignment2.Source as Pop;
      if (pop == null) return false;
      statements.RemoveAt(i);
      assignment.Source = push.ValueToPush;
      assignment2.Source = new BoundExpression() { Definition = local };
      this.sourceMethodBody.numberOfReferences[local]++;
      return true;
    }

    private void ReplacePushPopPattern(List<IStatement> statements, int i) {
      if (i > statements.Count - 2) return;
      int count = 0;
      while (i+count < statements.Count-1 && statements[i+count] is Push) count++;
      while (i > 1 && statements[i-1] is Push) { i--; count++; }
      if (count == 0) return;
      ExpressionStatement/*?*/ expressionStatement = statements[i + count] as ExpressionStatement;
      if (expressionStatement == null) return;
      PopCounter pc = new PopCounter();
      pc.Visit(expressionStatement.Expression);
      if (pc.count > count || pc.count == 0) return;
      i += count-pc.count;
      PopReplacer pr = new PopReplacer(this.sourceMethodBody.host, statements, i);
      expressionStatement.Expression = pr.Visit(expressionStatement.Expression);
      statements.RemoveRange(i, count);
    }

    private static void DeleteGotoNextStatement(List<IStatement> statements, int i) {
      if (i > statements.Count-2) return;
      GotoStatement/*?*/ gotoStatement = statements[i] as GotoStatement;
      if (gotoStatement == null) return;
      BasicBlock/*?*/ basicBlock = statements[i+1] as BasicBlock;
      if (basicBlock == null) return;
      if (basicBlock.Statements.Count < 1) return;
      if (gotoStatement.TargetStatement != basicBlock.Statements[0]) return;
      statements.RemoveAt(i);
    }

    /// <summary>
    /// Finds the following pattern:
    /// i   :  c ? A : B; // either A or B must be an empty statement and the other is "goto L1;"
    /// i+1 :  push x;
    /// i+2 :  goto L2;
    /// i+3 :  Block1
    ///        0  : L1;
    ///        1  : push y;
    ///        2  : Block2
    ///             0 : L2;
    ///             1 : (rest of statements in Block2)
    ///             
    /// Transforms it into:
    /// i   : push d ? X : Y;
    /// i+1 : (rest of statements in Block2)
    /// 
    /// Where if A is the empty statement, then
    ///   d == c, X == x, Y == y
    /// If B is the empty statement, then if y is zero,
    ///   d == !c, X == x, Y == y
    /// If B is the empty statement, then if y is not zero,
    ///   d == c, X == y, Y == x
    /// </summary>
    private bool ReplaceShortCircuitPattern(List<IStatement> statements, int i) {
      if (i > statements.Count - 4) return false;
      ConditionalStatement/*?*/ conditionalStatement = statements[i] as ConditionalStatement;
      if (conditionalStatement == null) return false;
      if (statements[i+1] is ConditionalStatement)
        return this.ReplaceChainedShortCircuitBooleanPattern(statements, i);
      Push/*?*/ push = statements[i+1] as Push;
      if (push == null) return false;
      GotoStatement/*?*/ Goto = statements[i+2] as GotoStatement;
      if (Goto == null) return false;
      BasicBlock/*?*/ block = statements[i+3] as BasicBlock;
      if (block == null) return false;
      if (block.Statements.Count < 3) return false;
      LabeledStatement/*?*/ label = block.Statements[0] as LabeledStatement;
      if (label == null) return false;
      List<IGotoStatement> branchesToThisLabel;
      if (this.predecessors.TryGetValue(label, out branchesToThisLabel)) {
        if (1 < branchesToThisLabel.Count) return false;
      }
      Push/*?*/ push2 = block.Statements[1] as Push;
      if (push2 == null) return false;
      BasicBlock/*?*/ block2 = block.Statements[2] as BasicBlock;
      if (block2 == null || block2.Statements.Count < 1 || block2.Statements[0] != Goto.TargetStatement) return false;
      if (conditionalStatement.TrueBranch is EmptyStatement) {
        Conditional conditional = new Conditional();
        conditional.Condition = conditionalStatement.Condition;
        conditional.ResultIfTrue = push.ValueToPush;
        conditional.ResultIfFalse = push2.ValueToPush;
        push.ValueToPush = conditional;
        statements[i] = push;
        statements.RemoveRange(i+1, 3);
        block2.Statements.RemoveAt(0);
        statements.InsertRange(i+1, block2.Statements);

        return true;
      }
      if (conditionalStatement.FalseBranch is EmptyStatement) {
        Conditional conditional = new Conditional();
        if (ExpressionHelper.IsIntegralZero(push2.ValueToPush)) {
          conditional.Condition = InvertCondition(conditionalStatement.Condition);
          conditional.ResultIfTrue = push.ValueToPush;
          conditional.ResultIfFalse = push2.ValueToPush;
        } else {
          conditional.Condition = conditionalStatement.Condition;
          conditional.ResultIfTrue = push2.ValueToPush;
          conditional.ResultIfFalse = push.ValueToPush;
        }
        push.ValueToPush = conditional;
        statements[i] = push;
        statements.RemoveRange(i+1, 3);
        block2.Statements.RemoveAt(0);
        statements.InsertRange(i+1, block2.Statements);

        return true;
      }
      return false;
    }

    private bool ReplaceShortCircuitPattern2(List<IStatement> statements, int i) {
      if (i > statements.Count - 3) return false;
      ConditionalStatement/*?*/ conditionalStatement = statements[i] as ConditionalStatement;
      if (conditionalStatement == null) return false;
      ConditionalStatement/*?*/ conditionalStatement2 = statements[i+1] as ConditionalStatement;
      if (conditionalStatement2 == null) return false;
      if (statements[i+2] is ConditionalStatement) {
        if (!ReplaceShortCircuitPattern2(statements, i+1)) return false;
        if (i > statements.Count - 3) return false;
        conditionalStatement2 = statements[i+1] as ConditionalStatement;
        if (conditionalStatement2 == null) return false;
      }
      BasicBlock/*?*/ block = statements[i+2] as BasicBlock;
      if (block == null) {
        return this.ReplaceShortCircuitPattern3(statements, i);
      }
      if (block.Statements.Count < 1) return false;
      GotoStatement/*?*/ gotoStatement = conditionalStatement.TrueBranch as GotoStatement;
      if (gotoStatement == null) {
        return this.ReplaceShortCircuitPattern3(statements, i);
      }
      if (!(conditionalStatement.FalseBranch is EmptyStatement)) return false;
      if (gotoStatement.TargetStatement != block.Statements[0]) return false;
      if (!(conditionalStatement2.TrueBranch is EmptyStatement)) {
        if (!(conditionalStatement2.TrueBranch is GotoStatement)) return false;
        if (!(conditionalStatement2.FalseBranch is EmptyStatement)) return false;
        conditionalStatement2.Condition = InvertCondition(conditionalStatement2.Condition);
        IStatement temp = conditionalStatement2.TrueBranch;
        conditionalStatement2.TrueBranch = conditionalStatement2.FalseBranch;
        conditionalStatement2.FalseBranch = temp;
      } else {
        if (!(conditionalStatement2.FalseBranch is GotoStatement)) return false;
      }
      Conditional conditional = new Conditional();
      conditional.Condition = conditionalStatement.Condition;
      conditional.ResultIfTrue = new CompileTimeConstant() { Value = 1, Type = this.sourceMethodBody.MethodDefinition.Type.PlatformType.SystemInt32 };
      conditional.ResultIfFalse = conditionalStatement2.Condition;
      conditionalStatement2.Condition = conditional;
      statements.RemoveAt(i);
      return true;
    }

    private bool ReplaceShortCircuitPattern3(List<IStatement> statements, int i) {
      if (i > statements.Count - 3) return false;
      ConditionalStatement/*?*/ conditionalStatement = statements[i] as ConditionalStatement;
      if (conditionalStatement == null) return false;
      ConditionalStatement/*?*/ conditionalStatement2 = statements[i+1] as ConditionalStatement;
      if (conditionalStatement2 == null) return false;
      if (statements[i+2] is ConditionalStatement) {
        if (!this.ReplaceShortCircuitPattern2(statements, i+1)) return false;
        if (i > statements.Count - 3) return false;
        conditionalStatement2 = statements[i+1] as ConditionalStatement;
        if (conditionalStatement2 == null) return false;
      }
      GotoStatement/*?*/ gotoStatement = conditionalStatement.FalseBranch as GotoStatement;
      if (gotoStatement == null) return false;
      if (!(conditionalStatement.TrueBranch is EmptyStatement)) return false;
      GotoStatement/*?*/ gotoStatement2 = conditionalStatement2.FalseBranch as GotoStatement;
      if (gotoStatement2 == null) {
        gotoStatement2 = conditionalStatement2.TrueBranch as GotoStatement;
        if (gotoStatement2 == null) return false;
        if (!(conditionalStatement2.FalseBranch is EmptyStatement)) return false;
        //brfalse, brtrue, ... could be A && B || C
        BasicBlock/*?*/ bb = statements[i+2] as BasicBlock;
        if (bb == null) return false;
        if (bb.Statements.Count < 1 || !(bb.Statements[0] == gotoStatement.TargetStatement)) return false;
        statements.RemoveAt(i+2);
        statements.InsertRange(i+2, bb.Statements);
        statements.RemoveAt(i+2);
        Conditional conditional = new Conditional();
        conditional.Condition = conditionalStatement.Condition;
        conditional.ResultIfTrue = conditionalStatement2.Condition;
        conditional.ResultIfFalse = new CompileTimeConstant() { Value = 0, Type = this.sourceMethodBody.MethodDefinition.Type.PlatformType.SystemInt32 };
        conditionalStatement2.Condition = conditional;
        statements.RemoveAt(i);
        return this.ReplaceShortCircuitPattern2(statements, i);
      }
      if (!(conditionalStatement2.TrueBranch is EmptyStatement)) return false;
      if (gotoStatement.TargetStatement == gotoStatement2.TargetStatement) {
        Conditional conditional = new Conditional();
        conditional.Condition = conditionalStatement.Condition;
        conditional.ResultIfTrue = conditionalStatement2.Condition;
        conditional.ResultIfFalse = new CompileTimeConstant() { Value = 0, Type = this.sourceMethodBody.MethodDefinition.Type.PlatformType.SystemInt32 };
        conditionalStatement2.Condition = conditional;
        statements.RemoveAt(i);
        return true;
      }
      return false;
    }

    private static IExpression InvertCondition(IExpression expression) {
      IBinaryOperation/*?*/ binOp = expression as IBinaryOperation;
      if (binOp != null) return InvertBinaryOperation(binOp);
      LogicalNot logicalNot = new LogicalNot();
      logicalNot.Operand = expression;
      return logicalNot;
    }

    private static IExpression InvertBinaryOperation(IBinaryOperation binOp) {
      BinaryOperation/*?*/ result = null;
      if (binOp is IEquality)
        result = new NotEquality();
      else if (binOp is INotEquality)
        result = new Equality();
      else if (binOp is ILessThan)
        result = new GreaterThanOrEqual();
      else if (binOp is ILessThanOrEqual)
        result = new GreaterThan();
      else if (binOp is IGreaterThan)
        result = new LessThanOrEqual();
      else if (binOp is IGreaterThanOrEqual)
        result = new LessThan();
      if (result != null) {
        result.LeftOperand = binOp.LeftOperand;
        result.RightOperand = binOp.RightOperand;
        return result;
      }
      LogicalNot logicalNot = new LogicalNot();
      logicalNot.Operand = binOp;
      return logicalNot;
    }

    private bool ReplaceChainedShortCircuitBooleanPattern(List<IStatement> statements, int i) {
      ConditionalStatement conditionalStatement = (ConditionalStatement)statements[i];
      if (!this.ReplaceShortCircuitPattern(statements, i + 1)) {
        if (!this.ReplaceShortCircuitPatternCreatedByCCI2(statements, i + 1)) return false;
      }
      //if (!this.ReplaceShortCircuitPattern(statements, i+1)) return false;
      Push/*?*/ push = statements[i+1] as Push;
      if (push == null) return false;
      Conditional/*?*/ chainedConditional = push.ValueToPush as Conditional;
      if (chainedConditional == null) return false;

      return this.ReplaceShortCircuitPattern(statements, i);
      // The code below seems wrong to me, but I don't have the repro anymore.

      //if (conditionalStatement.TrueBranch is EmptyStatement) {
      //  Conditional conditional = new Conditional();
      //  conditional.Condition = conditionalStatement.Condition;
      //  conditional.ResultIfTrue = chainedConditional;
      //  conditional.ResultIfFalse = new CompileTimeConstant() { Value = 0, Type = this.sourceMethodBody.MethodDefinition.Type.PlatformType.SystemInt32 };
      //  push.ValueToPush = conditional;
      //  statements[i] = push;
      //  statements.RemoveRange(i+1, 1);
      //  return true;
      //}
      //if (conditionalStatement.FalseBranch is EmptyStatement) {
      //  Conditional conditional = new Conditional();
      //  conditional.Condition = conditionalStatement.Condition;
      //  conditional.ResultIfTrue = chainedConditional.ResultIfTrue;
      //  conditional.ResultIfFalse = chainedConditional;
      //  push.ValueToPush = conditional;
      //  statements[i] = push;
      //  statements.RemoveRange(i+1, 1);
      //  return true;
      //}
      //return false;
    }

    /// <summary>
    /// Finds the following pattern:
    /// i   :  c ? A : B; // either A or B must be an empty statement and the other is "goto L1;"
    /// i+1 :  push x;
    /// i+2 :  goto L2;
    /// i+3 :  Block1
    ///        0  : L1;
    ///        1  : push y;
    ///        2  : goto L2;
    ///        3  : Block2
    ///             0 : whatever (but presumably it is the label L2)
    ///             
    /// Transforms it into:
    /// i   : push d ? X : Y;
    /// i+1 : goto L1;
    /// i+2 : Block 2
    /// 
    /// Where if A is the empty statement,
    ///   d == c, X == x, Y == y
    /// If B is the empty statement and y is zero
    ///   d == !c, X == x, Y == y
    /// If B is the empty statement and y is not zero
    ///   d == c, X == y, Y == x
    /// And Block1 is deleted from the list.
    /// </summary>
    private bool ReplaceShortCircuitPatternCreatedByCCI2(List<IStatement> statements, int i) {
      if (i > statements.Count - 4) return false;
      ConditionalStatement/*?*/ conditionalStatement = statements[i] as ConditionalStatement;
      if (conditionalStatement == null) return false;
      Push/*?*/ push1 = statements[i + 1] as Push;
      if (push1 == null) return false;
      GotoStatement/*?*/ Goto = statements[i + 2] as GotoStatement;
      if (Goto == null) return false;
      BasicBlock/*?*/ block1 = statements[i + 3] as BasicBlock;
      if (block1 == null) return false;
      if (block1.Statements.Count < 4) return false;
      LabeledStatement/*?*/ label = block1.Statements[0] as LabeledStatement;
      if (label == null) return false;
      List<IGotoStatement> branchesToThisLabel;
      if (this.predecessors.TryGetValue(label, out branchesToThisLabel)) {
        //Console.WriteLine("there are " + branchesToThisLabel.Count + " branches to " + label.Label.Value);
        if (1 < branchesToThisLabel.Count) return false;
      }
      // TODO? Should we make sure that one of the branches in the conditionalStatement is
      // to label?
      Push/*?*/ push2 = block1.Statements[1] as Push;
      if (push2 == null) return false;
      GotoStatement/*?*/ Goto2 = block1.Statements[2] as GotoStatement;
      if (Goto2 == null) return false;
      if (Goto.TargetStatement != Goto2.TargetStatement) return false;
      BasicBlock/*?*/ block2 = block1.Statements[3] as BasicBlock;
      if (block2 == null) return false;
      if (conditionalStatement.TrueBranch is EmptyStatement) {
        Conditional conditional = new Conditional();
        conditional.Condition = conditionalStatement.Condition;
        conditional.ResultIfTrue = push1.ValueToPush;
        conditional.ResultIfFalse = push2.ValueToPush;
        push1.ValueToPush = conditional;
        statements[i] = push1;
        statements[i + 1] = statements[i + 2]; // move the goto up
        statements[i + 2] = block2;
        statements.RemoveRange(i + 3, 1);
        return true;
      }
      if (conditionalStatement.FalseBranch is EmptyStatement) {
        Conditional conditional = new Conditional();
        if (ExpressionHelper.IsIntegralZero(push2.ValueToPush)) {
          conditional.Condition = InvertCondition(conditionalStatement.Condition);
          conditional.ResultIfTrue = push1.ValueToPush;
          conditional.ResultIfFalse = push2.ValueToPush;
        } else {
          conditional.Condition = conditionalStatement.Condition;
          conditional.ResultIfTrue = push2.ValueToPush;
          conditional.ResultIfFalse = push1.ValueToPush;
        }
        push1.ValueToPush = conditional;
        statements[i] = push1;
        statements[i + 1] = statements[i + 2]; // move the goto up
        statements[i + 2] = block2;
        statements.RemoveRange(i + 3, 1);
        return true;
      }
      return false;
    }

    private void ReplaceLocalArrayInitializerPattern(List<IStatement> statements, int i) {
      if (i > statements.Count - 3) return;
      Push/*?*/ push = statements[i] as Push;
      if (push == null) return;
      CreateArray/*?*/ createArray = push.ValueToPush as CreateArray;
      if (createArray == null) return;
      ExpressionStatement/*?*/ expressionStatement = statements[i+1] as ExpressionStatement;
      if (expressionStatement == null) return;
      MethodCall/*?*/ methodCall = expressionStatement.Expression as MethodCall;
      if (methodCall == null || !methodCall.IsStaticCall || methodCall.Arguments.Count != 2) return;
      Dup/*?*/ dup = methodCall.Arguments[0] as Dup;
      if (dup == null) return;
      TokenOf/*?*/ tokenOf = methodCall.Arguments[1] as TokenOf;
      if (tokenOf == null) return;
      IFieldDefinition/*?*/ initialValueField = tokenOf.Definition as IFieldDefinition;
      if (initialValueField == null || !initialValueField.IsMapped) return;
      if (methodCall.MethodToCall.Name.UniqueKey != this.InitializeArray.UniqueKey) return;
      expressionStatement = statements[i+2] as ExpressionStatement;
      if (expressionStatement == null) return;
      Assignment/*?*/ assignment = expressionStatement.Expression as Assignment;
      if (assignment == null) return;
      Pop/*?*/ pop = assignment.Source as Pop;
      if (pop == null) return;
      List<ulong> sizes = new List<ulong>();
      foreach (IExpression expr in createArray.Sizes) {
        IMetadataConstant mdc = expr as IMetadataConstant;
        if (mdc == null) return;
        sizes.Add(ConvertToUlong(mdc));
      }
      AddArrayInitializers(createArray, initialValueField, sizes.ToArray());
      assignment.Source = createArray;
      statements[i] = expressionStatement;
      statements.RemoveRange(i+1, 2);
    }

    private static void AddArrayInitializers(CreateArray createArray, IFieldDefinition initialValueField, ulong[] sizes) {
      ITypeReference elemType = createArray.ElementType;
      MemoryStream memoryStream = new MemoryStream(new List<byte>(initialValueField.FieldMapping.Data).ToArray());
      BinaryReader reader = new BinaryReader(memoryStream, Encoding.Unicode);
      ulong flatSize = 1;
      foreach (ulong dimensionSize in sizes) flatSize *= dimensionSize;
      while (flatSize-- > 0) {
        CompileTimeConstant cc = new CompileTimeConstant();
        cc.Value = ReadValue(elemType.TypeCode, reader);
        cc.Type = elemType;
        createArray.Initializers.Add(cc);
      }
    }

    private static ulong ConvertToUlong(IMetadataConstant c) {
      IConvertible/*?*/ ic = c.Value as IConvertible;
      if (ic == null) return 0; //TODO: error
      switch (ic.GetTypeCode()) {
        case TypeCode.SByte:
        case TypeCode.Int16:
        case TypeCode.Int32:
        case TypeCode.Int64:
          return (ulong)ic.ToInt64(null); //TODO: error if < 0
        case TypeCode.Byte:
        case TypeCode.UInt16:
        case TypeCode.UInt32:
        case TypeCode.UInt64:
          return ic.ToUInt64(null);
      }
      return 0; //TODO: error
    }

    private static object ReadValue(PrimitiveTypeCode primitiveTypeCode, BinaryReader reader) {
      switch (primitiveTypeCode) {
        case PrimitiveTypeCode.Boolean: return reader.ReadBoolean();
        case PrimitiveTypeCode.Char: return reader.ReadChar();
        case PrimitiveTypeCode.Float32: return reader.ReadSingle();
        case PrimitiveTypeCode.Float64: return reader.ReadDouble();
        case PrimitiveTypeCode.Int16: return reader.ReadInt16();
        case PrimitiveTypeCode.Int32: return reader.ReadInt32();
        case PrimitiveTypeCode.Int64: return reader.ReadInt64();
        case PrimitiveTypeCode.Int8: return reader.ReadSByte();
        case PrimitiveTypeCode.UInt16: return reader.ReadUInt16();
        case PrimitiveTypeCode.UInt32: return reader.ReadUInt32();
        case PrimitiveTypeCode.UInt64: return reader.ReadUInt64();
        case PrimitiveTypeCode.UInt8: return reader.ReadByte();
        default:
          Debug.Assert(false);
          break;
      }
      return null;
    }

    IName InitializeArray {
      get {
        if (this.initializeArray == null)
          this.initializeArray = this.nameTable.GetNameFor("InitializeArray");
        return this.initializeArray;
      }
    }
    IName/*?*/ initializeArray;
  }

  internal class PopCounter : BaseCodeTraverser {
    internal int count;

    public override void Visit(IExpression expression) {
      if (expression is Pop) this.count++;
      base.Visit(expression);
    }

  }

  internal class PopReplacer : MethodBodyCodeMutator {
    List<IStatement> statements;
    int i;

    internal PopReplacer(IMetadataHost host, List<IStatement> statements, int i)
      : base(host) {
      this.statements = statements;
      this.i = i;
    }

    public override IExpression Visit(IExpression expression) {
      Pop pop = expression as Pop;
      if (pop != null) {
        Push push = (Push)this.statements[this.i++];
        return push.ValueToPush;
      }
      return base.Visit(expression);
    }

  }

  internal class TempVariable : LocalDefinition {
    public TempVariable() {
    }

  }

}

