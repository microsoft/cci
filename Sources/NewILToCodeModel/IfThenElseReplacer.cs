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

  internal class IfThenElseReplacer : CodeTraverser {

    internal IfThenElseReplacer(SourceMethodBody sourceMethodBody) {
      Contract.Requires(sourceMethodBody != null);
      this.host = sourceMethodBody.host; Contract.Assume(sourceMethodBody.host != null);
      this.gotosThatTarget = sourceMethodBody.gotosThatTarget; Contract.Assume(this.gotosThatTarget != null);
    }

    IMetadataHost host;
    Hashtable<List<IGotoStatement>> gotosThatTarget;
    LabeledStatement labelImmediatelyFollowingCurrentBlock;

    [ContractInvariantMethod]
    private void ObjectInvariant() {
      Contract.Invariant(this.host != null);
      Contract.Invariant(this.gotosThatTarget != null);
    }

    public override void TraverseChildren(IBlockStatement block) {
      Contract.Assume(block is BlockStatement);
      var decompiledBlock = (BlockStatement)block;
      var statements = decompiledBlock.Statements;
      for (int i = 0; i < statements.Count; i++) {
        var statement = statements[i];
        var conditionalStatement = statement as ConditionalStatement;
        if (conditionalStatement == null) continue;
        var condition = conditionalStatement.Condition;
        var gotoStatement = conditionalStatement.FalseBranch as GotoStatement;
        if (gotoStatement == null) {
          gotoStatement = conditionalStatement.TrueBranch as GotoStatement;
          if (gotoStatement == null) continue;
          if (!(conditionalStatement.FalseBranch is EmptyStatement)) continue;
          condition = InvertCondition(condition);
        } else {
          if (!(conditionalStatement.TrueBranch is EmptyStatement)) continue;
        }
        var gotosThatTarget = this.gotosThatTarget[(uint)gotoStatement.TargetStatement.Label.UniqueKey];
        Contract.Assume(gotosThatTarget != null && gotosThatTarget.Count > 0);
        var trueBlock = ExtractBlock(statements, i+1, gotoStatement.TargetStatement, gotosThatTarget.Count == 1);
        if (trueBlock == null) continue;
        if (conditionalStatement.TrueBranch is EmptyStatement)
          conditionalStatement.FalseBranch = conditionalStatement.TrueBranch;
        conditionalStatement.TrueBranch = trueBlock;
        conditionalStatement.Condition = condition;
        gotosThatTarget.Remove(gotoStatement);
      }
      for (int i = statements.Count-2; i >= 0; i--) {
        Contract.Assume(i < statements.Count);
        var statement = statements[i];
        var conditionalStatement = statement as ConditionalStatement;
        if (conditionalStatement == null || !(conditionalStatement.FalseBranch is EmptyStatement)) continue;
        var trueBlock = conditionalStatement.TrueBranch as BlockStatement;
        if (trueBlock == null) continue;
        var gotoEndif = ReturnLastGoto(trueBlock);
        if (gotoEndif == null) continue;
        var gotosThatTarget = this.gotosThatTarget[(uint)gotoEndif.TargetStatement.Label.UniqueKey];
        Contract.Assume(gotosThatTarget != null && gotosThatTarget.Count > 0);
        var falseBlock = ExtractBlock(statements, i+1, gotoEndif.TargetStatement, gotosThatTarget.Count == 1);
        if (falseBlock == null) continue;
        conditionalStatement.FalseBranch = falseBlock;
        gotosThatTarget.Remove(gotoEndif);
        RemoveLastGoto(trueBlock);
      }
      var savedLabelImmediatelyFollowingCurrentBlock = this.labelImmediatelyFollowingCurrentBlock;
      for (int i = 0, n = decompiledBlock.Statements.Count; i < n; i++) {
        var statement = decompiledBlock.Statements[i];
        Contract.Assume(statement != null);
        if (statement is BlockStatement || statement is ConditionalStatement) {
          if (i < n-1) {
            this.labelImmediatelyFollowingCurrentBlock = decompiledBlock.Statements[i+1] as LabeledStatement;
            if (this.labelImmediatelyFollowingCurrentBlock == null) {
              var blk = decompiledBlock.Statements[i+1] as BlockStatement;
              if (blk != null && blk.Statements.Count > 0)
                this.labelImmediatelyFollowingCurrentBlock = blk.Statements[0] as LabeledStatement;
            }
          } else {
            this.labelImmediatelyFollowingCurrentBlock = savedLabelImmediatelyFollowingCurrentBlock;
          }
        }
        this.Traverse(statement);
      }
      this.labelImmediatelyFollowingCurrentBlock = savedLabelImmediatelyFollowingCurrentBlock;
    }

    private static GotoStatement/*?*/ ReturnLastGoto(BlockStatement block) {
      Contract.Requires(block != null);
      var n = block.Statements.Count;
      if (n == 0) return null;
      var lastStatement = block.Statements[n-1];
      var lastGoto = lastStatement as GotoStatement;
      if (lastGoto != null) return lastGoto;
      var lastBlock = lastStatement as BlockStatement;
      if (lastBlock == null) return null;
      return ReturnLastGoto(lastBlock);
    }

    private static void RemoveLastGoto(BlockStatement block) {
      Contract.Requires(block != null);
      var n = block.Statements.Count;
      if (n == 0) return;
      var lastStatement = block.Statements[n-1];
      var lastGoto = lastStatement as GotoStatement;
      if (lastGoto != null) {
        block.Statements.RemoveAt(n-1);
        return;
      }
      var lastBlock = lastStatement as BlockStatement;
      if (lastBlock == null) return;
      RemoveLastGoto(lastBlock);
    }

    internal static IExpression InvertCondition(IExpression expression) {
      Contract.Requires(expression != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var binOp = expression as IBinaryOperation;
      if (binOp != null) return InvertBinaryOperation(binOp);
      var conditional = expression as Conditional;
      if (conditional != null) return InvertConditional(conditional);
      var compileTimeConst = expression as CompileTimeConstant;
      if (compileTimeConst != null) return InvertCompileTimeConstant(compileTimeConst);
      var logNot = expression as ILogicalNot;
      if (logNot != null) return logNot.Operand;
      var logicalNot = new LogicalNot();
      logicalNot.Operand = expression;
      logicalNot.Type = expression.Type;
      logicalNot.Locations.AddRange(expression.Locations);
      return logicalNot;
    }

    private static IExpression InvertCompileTimeConstant(CompileTimeConstant compileTimeConst) {
      Contract.Requires(compileTimeConst != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      Contract.Assume(compileTimeConst.Value is bool); //since it is used as a condition, it is assumed to be a boolean
      return new CompileTimeConstant() { Value = !(bool)compileTimeConst.Value, Type = compileTimeConst.Type };
    }

    private static IExpression InvertConditional(Conditional conditional) {
      Contract.Requires(conditional != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      var result = new Conditional() { Type = conditional.Type };
      if (ExpressionHelper.IsIntegralZero(conditional.ResultIfTrue)) {
        result.Condition = conditional.Condition;
        result.ResultIfTrue = InvertCondition(conditional.ResultIfTrue);
        result.ResultIfFalse = InvertCondition(conditional.ResultIfFalse);
      } else {
        result.Condition = InvertCondition(conditional.Condition);
        result.ResultIfTrue = InvertCondition(conditional.ResultIfFalse);
        result.ResultIfFalse = InvertCondition(conditional.ResultIfTrue);
      }
      return result;
    }

    private static IExpression InvertBinaryOperation(IBinaryOperation binOp) {
      Contract.Requires(binOp != null);
      Contract.Ensures(Contract.Result<IExpression>() != null);

      BinaryOperation/*?*/ result = null;
      if (binOp is IEquality && binOp.LeftOperand.Type.TypeCode != PrimitiveTypeCode.Float32 && binOp.LeftOperand.Type.TypeCode != PrimitiveTypeCode.Float64)
        result = new NotEquality();
      else if (binOp is INotEquality && binOp.LeftOperand.Type.TypeCode != PrimitiveTypeCode.Float32 && binOp.LeftOperand.Type.TypeCode != PrimitiveTypeCode.Float64)
        result = new Equality();
      else if (binOp is ILessThan)
        result = new GreaterThanOrEqual() { IsUnsignedOrUnordered = KeepUnsignedButInvertUnordered(((ILessThan)binOp).IsUnsignedOrUnordered, binOp) };
      else if (binOp is ILessThanOrEqual)
        result = new GreaterThan() { IsUnsignedOrUnordered = KeepUnsignedButInvertUnordered(((ILessThanOrEqual)binOp).IsUnsignedOrUnordered, binOp) };
      else if (binOp is IGreaterThan)
        result = new LessThanOrEqual() { IsUnsignedOrUnordered = KeepUnsignedButInvertUnordered(((IGreaterThan)binOp).IsUnsignedOrUnordered, binOp) };
      else if (binOp is IGreaterThanOrEqual)
        result = new LessThan() { IsUnsignedOrUnordered = KeepUnsignedButInvertUnordered(((IGreaterThanOrEqual)binOp).IsUnsignedOrUnordered, binOp) };
      if (result != null) {
        result.LeftOperand = binOp.LeftOperand;
        result.RightOperand = binOp.RightOperand;
        result.Type = binOp.Type;
        result.Locations.AddRange(binOp.Locations);
        return result;
      }
      LogicalNot logicalNot = new LogicalNot();
      logicalNot.Operand = binOp;
      logicalNot.Type = binOp.Type;
      logicalNot.Locations.AddRange(binOp.Locations);
      return logicalNot;
    }

    private static bool KeepUnsignedButInvertUnordered(bool usignedOrUnordered, IBinaryOperation binOp) {
      Contract.Requires(binOp != null);
      var isIntegerOperation = TypeHelper.IsPrimitiveInteger(binOp.LeftOperand.Type);
      if (usignedOrUnordered) return isIntegerOperation;
      return !isIntegerOperation; //i.e. !(x < y) is the same as (x >= y) only if first comparison returns the opposite result than the second for the unordered case.
    }

    private IStatement/*?*/ ExtractBlock(List<IStatement> statements, int first, ILabeledStatement labelOfSubsequentCode, bool removeLabel) {
      Contract.Requires(statements != null);
      Contract.Requires(first > 0);

      var last = first;
      var n = statements.Count;
      if (first == n-1) {
        var lastBlock = statements[first] as BlockStatement;
        if (lastBlock != null && labelOfSubsequentCode == this.labelImmediatelyFollowingCurrentBlock) {
          statements.RemoveAt(first);
          return lastBlock;
        }          
      }
      while (last < n) {
        var statement = statements[last];
        if (statement == labelOfSubsequentCode) {
          if (removeLabel) statements.RemoveAt(last);
          break;
        }
        var block = statement as DecompiledBlock;
        if (block != null && block.FirstExecutableStatementIs(labelOfSubsequentCode)) {
          if (removeLabel) block.RemoveAndReturnInitialLabel();
          break;
        }
        last++;
      }
      if (last == n) {
        if (labelOfSubsequentCode != this.labelImmediatelyFollowingCurrentBlock) return null;
        Contract.Assert(n == statements.Count); //any modification to statements will terminate the while loop before last == n.
      }
      if (first == last) return new EmptyStatement();
      if (first == last-1) {
        var firstBlock = statements[first] as BlockStatement;
        if (firstBlock != null) {
          statements.RemoveAt(first);
          return firstBlock;
        }
      }
      var newStatements = statements.GetRange(first, last-first);
      statements.RemoveRange(first, last-first);
      return new BlockStatement() { Statements = newStatements };
    }

  }
}