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

namespace Microsoft.Cci.ILToCodeModel {

  internal class Unstacker : MethodBodyMutator {
    BasicBlock block;
    SourceMethodBody body;
    struct CodePoint { internal List<IStatement> statements; internal int index; internal StackOfLocals operandStack; }
    Queue<CodePoint> codePointsToAnalyze = new Queue<CodePoint>();
    Dictionary<ILabeledStatement, StackOfLocals> stackFor = new Dictionary<ILabeledStatement, StackOfLocals>();
    bool visitedUnconditionalBranch;
    StackOfLocals operandStack;
    // Handling of the catch clause in a try-catch statement. Currently, we identify the first statement in
    // a catch clause, if there is a pop, we do not try to unstack it. Later, the control flow decompiler
    // will turn that pop into an approppriate local definition. 
    // This, however, breaks an invariant that the unstacker will remove all the pushes and pops. A fix is planned. 
    List<BasicBlock>/*!*/ catchBlocks = new List<BasicBlock>();
    bool IsVisitingCatchBlock = false;
    bool IsFirstInCatchBlock = false;

    internal Unstacker(SourceMethodBody body)
      : base(body.host, true) {
      this.body = body;
    }

    public override IExpression Visit(ArrayIndexer arrayIndexer) {
      arrayIndexer.Indices = this.Visit(arrayIndexer.Indices);
      arrayIndexer.IndexedObject = this.Visit(arrayIndexer.IndexedObject);
      arrayIndexer.Type = this.Visit(arrayIndexer.Type);
      return arrayIndexer;
    }

    public override IExpression Visit(Assignment assignment) {
      assignment.Source = this.Visit(assignment.Source);
      assignment.Target = this.Visit(assignment.Target);
      assignment.Type = this.Visit(assignment.Type);
      return assignment;
    }

    public override IExpression Visit(BinaryOperation binaryOperation) {
      binaryOperation.RightOperand = this.Visit(binaryOperation.RightOperand);
      binaryOperation.LeftOperand = this.Visit(binaryOperation.LeftOperand);
      binaryOperation.Type = this.Visit(binaryOperation.Type);
      return binaryOperation;
    }

    internal void Visit(BasicBlock block) {
      if (this.block == null) this.block = block;
      this.operandStack = new StackOfLocals(this.body);
      int numberOfCatchBlocks = block.NumberOfTryBlocksStartingHere;
      BasicBlock bbb = block;
      while (numberOfCatchBlocks-- > 0) {
        bbb = bbb.Statements[bbb.Statements.Count - 1] as BasicBlock;
        if (!this.catchBlocks.Contains(bbb)) this.catchBlocks.Add(bbb);
      }
      this.codePointsToAnalyze.Enqueue(new CodePoint() { statements = block.Statements, index = 0, operandStack = this.operandStack });
      while (this.codePointsToAnalyze.Count > 0) {
        CodePoint codePoint = this.codePointsToAnalyze.Dequeue();
        if (codePoint.operandStack == null) {
          //Can only get here for a code point with a label and labels are only created if there a branches to them. 
          //Sooner or later the branch must be encountered and then this code point will get an operand stack.
          this.codePointsToAnalyze.Enqueue(codePoint);
          continue;
        }
        this.operandStack = codePoint.operandStack.Clone(this.block);
        List<IStatement> statements = codePoint.statements;
        for (int i = codePoint.index; i < statements.Count; i++) {
          statements[i] = this.Visit(statements[i]);
          if (this.visitedUnconditionalBranch) {
            this.visitedUnconditionalBranch = false;
            if (i+1 < statements.Count) {
              ILabeledStatement/*?*/ label = statements[i+1] as ILabeledStatement;
              if (label == null) continue; //Unreachable code. Need to carry on though, since it may contain the only branch to a label.
              this.codePointsToAnalyze.Enqueue(new CodePoint() { statements = statements, index = i+1 });
            }
            break;
          }
        }
      }
    }

    /// <summary>
    /// Visits the specified block statement.
    /// </summary>
    /// <param name="blockStatement">The block statement.</param>
    /// <returns></returns>
    public override IBlockStatement Visit(BlockStatement blockStatement) {
      var savedBlock = this.block;
      BasicBlock bb = blockStatement as BasicBlock;
      if (bb != null) this.block = bb;
      blockStatement.Statements = Visit(blockStatement.Statements);
      this.block = savedBlock;
      return blockStatement;
    }

    public override IExpression Visit(Conditional conditional) {
      conditional.ResultIfFalse = Visit(conditional.ResultIfFalse);
      conditional.ResultIfTrue = Visit(conditional.ResultIfTrue);
      conditional.Condition = Visit(conditional.Condition);
      conditional.Type = this.Visit(conditional.Type);
      return conditional;
    }

    public override IExpression Visit(CreateArray createArray) {
      createArray.ElementType = this.Visit(createArray.ElementType);
      createArray.Initializers = this.Visit(createArray.Initializers);
      createArray.Sizes = this.Visit(createArray.Sizes);
      createArray.Type = this.Visit(createArray.Type);
      return createArray;
    }

    public override IExpression Visit(IExpression expression) {
      Pop/*?*/ pop = expression as Pop;
      if (pop != null) {
        if (this.operandStack.Count > 0) {
          var local = this.operandStack.Pop();
          if (pop.Type != Dummy.TypeReference)
            local.Type = pop.Type;
          this.body.numberOfReferences[local]++;
          var result = new BoundExpression() { Definition = local, Type = local.Type };
          //if (pop is PopAsUnsigned)
          //  result = new Conversion(){ ValueToConvert = result, TypeAfterConversion = TypeHelper.UnsignedEquivalent(local.Type) }
          return result;
        } else {
          if (this.IsFirstInCatchBlock) return pop;
        }
        return CodeDummy.Expression;
      }
      Dup/*?*/ dup = expression as Dup;
      if (dup != null) {
        if (this.operandStack.Count > 0) {
          var local = this.operandStack.Peek();
          this.body.numberOfReferences[local]++;
          return new BoundExpression() { Definition = local, Type = local.Type };
          //TODO: what about unsigned dup?
        }
        return CodeDummy.Expression;
      }
      return base.Visit(expression);
    }

    public override IFieldReference Visit(IFieldReference fieldReference) {
      return fieldReference;
    }

    public override IStatement Visit(IStatement statement) {
      IGotoStatement gotoStatement = statement as IGotoStatement;
      if (gotoStatement != null) {
        this.visitedUnconditionalBranch = true;
        foreach (object ob in this.path) { if (ob is IConditionalStatement) this.visitedUnconditionalBranch = false; }
        StackOfLocals newStack = null;
        if (this.stackFor.TryGetValue(gotoStatement.TargetStatement, out newStack))
          return this.TransferStack(gotoStatement, newStack);
        this.stackFor.Add(gotoStatement.TargetStatement, this.operandStack.Clone(this.block));
        return gotoStatement;
      }
      ILabeledStatement labeledStatement = statement as ILabeledStatement;
      if (labeledStatement != null) {
        StackOfLocals newStack = null;
        if (this.stackFor.TryGetValue(labeledStatement, out newStack))
          return this.TransferStack(labeledStatement, newStack);
        this.stackFor.Add(labeledStatement, this.operandStack.Clone(this.block));
        return labeledStatement;
      }
      Push/*?*/ push = statement as Push;
      if (push != null) {
        push.ValueToPush = this.Visit(push.ValueToPush);
        LocalDefinition temp = new TempVariable() { Name = this.body.host.NameTable.GetNameFor("__temp_"+this.body.localVariables.Count) };
        temp.Type = push.ValueToPush.Type;
        this.operandStack.Push(temp);
        this.body.localVariables.Add(temp);
        if (this.block.LocalVariables == null) this.block.LocalVariables = new List<ILocalDefinition>();
        this.block.LocalVariables.Add(temp);
        this.body.numberOfReferences.Add(temp, 0);
        this.body.numberOfAssignments.Add(temp, 1);
        return new ExpressionStatement() { Expression = new Assignment() { Target = new TargetExpression() { Definition = temp }, Source = push.ValueToPush } };
      }
      if (statement is EndFilter || statement is EndFinally) {
        this.visitedUnconditionalBranch = true;
        return statement;
      }
      if (statement is SwitchInstruction) {
        return statement;
      }
      BasicBlock bbb = statement as BasicBlock;
      if (bbb != null) {
        if (this.catchBlocks.Contains(bbb)) this.IsVisitingCatchBlock = true;
        int numberOfCatchBlocks = bbb.NumberOfTryBlocksStartingHere;
        while (numberOfCatchBlocks-- > 0) {
          bbb = bbb.Statements[bbb.Statements.Count - 1] as BasicBlock;
          if (!this.catchBlocks.Contains(bbb)) this.catchBlocks.Add(bbb);
        }
      }
      return base.Visit(statement);
    }

    public override ITypeReference Visit(ITypeReference typeReference) {
      return typeReference;
    }

    public override List<IExpression> Visit(List<IExpression> expressions) {
      for (int i = expressions.Count-1; i >= 0; i--)
        expressions[i] = this.Visit(expressions[i]);
      return expressions;
    }

    public override List<IStatement> Visit(List<IStatement> statements) {
      List<IStatement> newList = new List<IStatement>();
      bool IsFirst = true;
      foreach (var statement in statements) {
        BasicBlock bb = statement as BasicBlock;
        if (bb != null && bb.Statements.Count > 0) {
          ILabeledStatement labeledStatement = bb.Statements[0] as ILabeledStatement;
          if (labeledStatement != null) {
            StackOfLocals newStack = null;
            if (this.stackFor.TryGetValue(labeledStatement, out newStack)) {
              this.operandStack.TransferTo(newStack, newList);
              this.operandStack = newStack;
              this.stackFor.Remove(labeledStatement);
            }
          }
        }
        if (IsVisitingCatchBlock && IsFirst)
          this.IsFirstInCatchBlock = true;
        IStatement newStatement;
        try {
          newStatement = this.Visit(statement);
        } finally {
          this.IsFirstInCatchBlock = false;
        }
        if (newStatement is IBlockStatement && !(statement is IBlockStatement))
          newList.AddRange(((IBlockStatement)newStatement).Statements);
        else
          newList.Add(newStatement);
        if (IsFirst) IsFirst = false;
      }
      return newList;
    }

    public override IExpression Visit(MethodCall methodCall) {
      methodCall.Arguments = this.Visit(methodCall.Arguments);
      if (!methodCall.IsStaticCall)
        methodCall.ThisArgument = this.Visit(methodCall.ThisArgument);
      methodCall.MethodToCall = this.Visit(methodCall.MethodToCall);
      methodCall.Type = this.Visit(methodCall.Type);
      return methodCall;
    }

    public override IMethodReference Visit(IMethodReference methodReference) {
      return methodReference;
    }

    public override IExpression Visit(PointerCall pointerCall) {
      pointerCall.Arguments = Visit(pointerCall.Arguments);
      pointerCall.Pointer = this.Visit(pointerCall.Pointer);
      pointerCall.Type = this.Visit(pointerCall.Type);
      return pointerCall;
    }

    private IStatement TransferStack(IStatement statement, StackOfLocals targetStack) {
      BasicBlock block = new BasicBlock(0);
      this.operandStack.TransferTo(targetStack, block.Statements);
      block.Statements.Add(statement);
      this.operandStack = targetStack.Clone(this.block);
      return block;
    }

  }

  internal class StackOfLocals {
    private SourceMethodBody body;
    private LocalDefinition[]/*?*/ elements;
    private int top = -1;

    internal StackOfLocals(SourceMethodBody body) {
      this.body = body;
    }

    private StackOfLocals(StackOfLocals template) {
      if (template.elements != null)
        this.elements = (LocalDefinition[])template.elements.Clone();
      this.top = template.top;
      this.body = template.body;
    }

    internal StackOfLocals Clone(BasicBlock block) {
      if (block.LocalVariables != null) {
        for (int i = 0; i <= this.top; i++) {
          LocalDefinition local = this.elements[i];
          block.LocalVariables.Remove(local);
        }
      }
      return new StackOfLocals(this);
    }

    internal int Count {
      get { return this.top+1; }
    }

    internal LocalDefinition Peek() {
      return this.elements[this.top];
    }

    internal LocalDefinition Pop() {
      return this.elements[this.top--];
    }

    internal void Push(LocalDefinition local) {
      if (this.elements == null)
        this.elements = new LocalDefinition[8];
      else if (this.top >= this.elements.Length-1)
        Array.Resize(ref this.elements, this.elements.Length*2);
      this.elements[++this.top] = local;
    }

    internal void TransferTo(StackOfLocals targetStack, List<IStatement> list) {
      for (int i = 0; i <= this.top && i <= targetStack.top; i++) {
        LocalDefinition sourceLocal = this.elements[i];
        LocalDefinition targetLocal = targetStack.elements[i];
        if (sourceLocal == targetLocal) continue;
        this.body.numberOfReferences[sourceLocal]++;
        this.body.numberOfAssignments[targetLocal]++;
        var target = new TargetExpression() { Definition = targetLocal, Type = targetLocal.Type };
        var source = new BoundExpression() { Definition = sourceLocal, Type = sourceLocal.Type };
        var assigment = new Assignment() { Target = target, Source = source, Type = target.Type };
        list.Add(new ExpressionStatement() { Expression = assigment });
      }
    }
  }

}
