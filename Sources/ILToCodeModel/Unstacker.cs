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
using System.Diagnostics.Contracts;

namespace Microsoft.Cci.ILToCodeModel {

  internal class Unstacker : MethodBodyCodeMutator {
    BasicBlock block;
    SourceMethodBody body;
    struct CodePoint { internal List<IStatement> statements; internal int index; internal StackOfLocals operandStack; }
    Queue<CodePoint> codePointsToAnalyze = new Queue<CodePoint>();
    Dictionary<ILabeledStatement, StackOfLocals> stackFor = new Dictionary<ILabeledStatement, StackOfLocals>();
    bool visitedUnconditionalBranch;
    StackOfLocals operandStack;
    int tempCounter;

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
      this.block = block;
      this.operandStack = new StackOfLocals(this.body);
      this.codePointsToAnalyze.Enqueue(new CodePoint() { statements = block.Statements, index = 0, operandStack = this.operandStack });

      while (this.codePointsToAnalyze.Count > 0) {
        CodePoint codePoint = this.codePointsToAnalyze.Dequeue();
        if (codePoint.operandStack == null) {
          //Can only get here for a code point with a label and labels are only created if there a branches to them. 
          //Sooner or later the branch must be encountered and then this code point will get an operand stack from the branch.
          if (this.codePointsToAnalyze.Count == 0) {
            //But if we get here, there are NO other code blocks, so we'll loop forever if we just put codePoint back on the queue with an empty stack.
            //Start with an empty stack and just carry on.
            codePoint.operandStack = new StackOfLocals(this.body);
          } else {
            this.codePointsToAnalyze.Enqueue(codePoint); //keep looking for the branch.
            continue;
          }
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
      BasicBlock/*?*/ b = blockStatement as BasicBlock;
      for (; ; ) {
        if (b != null) {
          this.block = b;
          int statementCount = b.Statements.Count;
          BasicBlock/*?*/ bnext = null;
          if (statementCount > 1 && b.Statements[statementCount-2] is IGotoStatement) {
            bnext = b.Statements[--statementCount] as BasicBlock;
            if (bnext != null) b.Statements.RemoveAt(statementCount);
          }
          b.Statements = this.Visit(b.Statements);
          if (bnext == null || bnext.Statements.Count == 0) break;
          ILabeledStatement labeledStatement = bnext.Statements[0] as ILabeledStatement;
          if (labeledStatement != null) {
            StackOfLocals newStack = null;
            if (this.stackFor.TryGetValue(labeledStatement, out newStack)) {
              this.operandStack.TransferTo(newStack, b.Statements);
              this.operandStack = newStack;
              this.stackFor.Remove(labeledStatement);
            }
          }
          b.Statements.Add(bnext);
          b = bnext;
        } else {
          blockStatement.Statements = Visit(blockStatement.Statements);
          break;
        }
      }
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
          if (pop.Type.TypeCode == PrimitiveTypeCode.Boolean && local.Type != Dummy.TypeReference) {
            return new NotEquality() {
              LeftOperand = new BoundExpression() { Definition = local },
              RightOperand = new DefaultValue() { DefaultValueType = local.Type },
              Type = pop.Type
            };
          }
          if (pop.Type != Dummy.TypeReference)
            local.Type = pop.Type;
          this.body.numberOfReferences[local]++;
          IExpression result;
          if (local.turnIntoPopValueExpression)
            result = new PopValue() { Type = local.Type };
          else
            result = new BoundExpression() { Definition = local, Type = local.Type };
          if (pop is PopAsUnsigned)
            result = new ConvertToUnsigned(result);
          return result;
        } else {
          // popping the unnamed exception in a catch block.
          return pop;
        }
      }
      Dup/*?*/ dup = expression as Dup;
      if (dup != null) {
        if (this.operandStack.Count > 0) {
          var local = this.operandStack.Peek();
          this.body.numberOfReferences[local]++;
          IExpression result;
          if (local.turnIntoPopValueExpression)
            result = new DupValue() { Type = local.Type };
          else
            result = new BoundExpression() { Definition = local, Type = local.Type };
          //TODO: what about unsigned dup?
          return result;
        }
        return CodeDummy.Expression;
      }
      return base.Visit(expression);
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
      PushStatement/*?*/ push = statement as PushStatement;
      if (push != null) {
        push.ValueToPush = this.Visit(push.ValueToPush);
        this.tempCounter = this.body.localVariablesAndTemporaries.Count;
        var temp = new TempVariable() { Name = this.body.host.NameTable.GetNameFor("__temp_"+this.tempCounter++) };
        temp.Type = push.ValueToPush.Type;
        this.operandStack.Push(temp);
        this.body.numberOfReferences.Add(temp, 0);
        var ctc = push.ValueToPush as ICompileTimeConstant;
        // "push null" doesn't tell us enough to know what type it really is
        if (ctc != null && ctc.Value == null) { // REVIEW: need to make sure the type is a reference type?
          temp.isPolymorphic = true;
        }
        var be = push.ValueToPush as IBoundExpression;
        if (be != null) {
          var sourceLocal = be.Definition as TempVariable;
          if (sourceLocal != null && sourceLocal.isPolymorphic) {
            temp.isPolymorphic = true;
          }
        }
        if (push.ValueToPush.Type is IManagedPointerTypeReference) {
          temp.turnIntoPopValueExpression = true;
          return statement;
        }
        this.body.localVariablesAndTemporaries.Add(temp);
        if (this.block.LocalVariables == null) this.block.LocalVariables = new List<ILocalDefinition>();
        this.block.LocalVariables.Add(temp);
        this.body.numberOfAssignments.Add(temp, 1);
        return new ExpressionStatement() {
          Expression = new Assignment() { Target = new TargetExpression() { Definition = temp }, Source = push.ValueToPush },
          Locations = push.Locations
        };
      }
      if (statement is EndFilter || statement is EndFinally) {
        this.visitedUnconditionalBranch = true;
        return statement;
      }
      if (statement is SwitchInstruction) {
        return statement;
      }
      return base.Visit(statement);
    }

    public override List<IExpression> Visit(List<IExpression> expressions) {
      for (int i = expressions.Count-1; i >= 0; i--)
        expressions[i] = this.Visit(expressions[i]);
      return expressions;
    }

    public override List<IStatement> Visit(List<IStatement> statements) {
      List<IStatement> newList = new List<IStatement>();
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
        IStatement newStatement;

        newStatement = this.Visit(statement);

        if (newStatement is IBlockStatement && !(statement is IBlockStatement))
          newList.AddRange(((IBlockStatement)newStatement).Statements);
        else
          newList.Add(newStatement);
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
    private TempVariable[]/*?*/ elements;
    private int top = -1;

    internal StackOfLocals(SourceMethodBody body) {
      this.body = body;
    }

    private StackOfLocals(StackOfLocals template) {
      if (template.elements != null)
        this.elements = (TempVariable[])template.elements.Clone();
      this.top = template.top;
      this.body = template.body;
    }

    [ContractInvariantMethod]
    void ObjectInvariant() {
      Contract.Invariant(this.body != null);
      Contract.Invariant(this.top < 0 || this.elements != null);
      Contract.Invariant(this.top < 0 || this.top < this.elements.Length);
      Contract.Invariant(this.top < 0 || Contract.ForAll(0, this.top+1, i => this.elements[i] != null));
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

    internal TempVariable Peek() {
      return this.elements[this.top];
    }

    internal TempVariable Pop() {
      return this.elements[this.top--];
    }

    internal void Push(TempVariable local) {
      if (this.elements == null)
        this.elements = new TempVariable[8];
      else if (this.top >= this.elements.Length-1)
        Array.Resize(ref this.elements, this.elements.Length*2);
      this.elements[++this.top] = local;
    }

    [ContractVerification(false)]
    internal void TransferTo(StackOfLocals targetStack, List<IStatement> list) {
      Contract.Requires(targetStack != null);
      Contract.Requires(list != null);
      Contract.Requires(Contract.ForAll(list, x => x != null));

      for (int i = 0; i <= this.top && i <= targetStack.top; i++) {
        Contract.Assert(this.top >= 0);
        Contract.Assert(this.top < this.elements.Length);
        var sourceLocal = this.elements[i];
        Contract.Assert(targetStack.top >= 0);
        Contract.Assume(targetStack.elements != null);
        Contract.Assume(targetStack.top < targetStack.elements.Length);
        var targetLocal = targetStack.elements[i];
        Contract.Assume(targetLocal != null);
        if (sourceLocal == targetLocal) continue;
        if (targetLocal.turnIntoPopValueExpression) {
          sourceLocal.turnIntoPopValueExpression = true;
        } else if (sourceLocal.turnIntoPopValueExpression) {
          targetLocal.turnIntoPopValueExpression = true;
        } else {
          Contract.Assume(this.body.numberOfReferences != null);
          this.body.numberOfReferences[sourceLocal]++;
          Contract.Assume(this.body.numberOfAssignments != null);
          this.body.numberOfAssignments[targetLocal]++;
          var targetType = targetLocal.Type;
          var sourceType = sourceLocal.Type;
          var mergedType = TypeHelper.MergedType(TypeHelper.StackType(targetType), TypeHelper.StackType(sourceType));
          if (targetType != mergedType && !(targetType.TypeCode == PrimitiveTypeCode.Boolean && mergedType.TypeCode == PrimitiveTypeCode.Int32)) {
            targetLocal.Type = mergedType;
            targetType = mergedType;
            if (targetType is Dummy || sourceType is Dummy) targetLocal.isPolymorphic = true;
          }
          var target = new TargetExpression() { Definition = targetLocal, Type = targetType };
          var source = new BoundExpression() { Definition = sourceLocal, Type = sourceType };
          var assigment = new Assignment() { Target = target, Source = source, Type = targetType };
          list.Add(new ExpressionStatement() { Expression = assigment });
        }
      }
      //Contract.Assume(this.top < 0 || Contract.ForAll(0, this.top+1, i => this.elements[i] != null));
    }
  }

}
