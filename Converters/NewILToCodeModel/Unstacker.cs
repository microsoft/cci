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

namespace Microsoft.Cci.ILToCodeModel
{
    internal class Unstacker : CodeRewriter
    {
        private ISourceLocationProvider sourceLocationProvider;

        private Stack<ILocalDefinition> locals;
        private readonly IMethodDefinition methodDefinition;
        private readonly Dictionary<Tuple<int, uint>, ILocalDefinition> createdLocals;
        private Dictionary<int, Assignment> thenBranchPushes;
        private bool inThenBranch;
        private bool inElseBranch;
        private readonly ITypeReference systemBool;
        private readonly ITypeReference systemInt32;

        private Unstacker(SourceMethodBody sourceMethodBody)
            : base(sourceMethodBody.host)
        {
            Contract.Requires(sourceMethodBody != null);
            Contract.Assume(sourceMethodBody.host != null);
            this.sourceLocationProvider = sourceMethodBody.sourceLocationProvider;

            this.locals = new Stack<ILocalDefinition>();
            this.methodDefinition = sourceMethodBody.MethodDefinition;
            this.createdLocals = new Dictionary<Tuple<int, uint>, ILocalDefinition>();

            this.systemBool = this.host.PlatformType.SystemBoolean;
            this.systemInt32 = this.host.PlatformType.SystemInt32;
        }

        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
        private void ObjectInvariant()
        {
            Contract.Invariant(this.locals != null);
            Contract.Invariant(this.createdLocals != null);
            Contract.Invariant(this.systemBool != null);
            Contract.Invariant(this.systemInt32 != null);
        }

        public static IBlockStatement GetRidOfStack(SourceMethodBody methodBody, IBlockStatement block)
        {
            var me = new Unstacker(methodBody);
            var result = me.Rewrite(block);
            var stmts = new List<IStatement>();
            foreach (var loc in me.createdLocals.Values)
            {
                var decl = new LocalDeclarationStatement()
                {
                    InitialValue = null,
                    LocalVariable = loc,
                };
                stmts.Add(decl);
            }
            stmts.AddRange(result.Statements);
            var newBlock = new BlockStatement()
            {
                Statements = stmts,
                Locations = new List<ILocation>(result.Locations),
            };
            return newBlock;
        }

        private Tuple<int, uint> KeyForLocal(int depth, ITypeReference type)
        {
            Contract.Requires(0 <= depth);
            Contract.Requires(type != null);

            var key = Tuple.Create(depth, type.InternedKey);
            return key;
        }
        private string NameForLocal(int depth, ITypeReference type)
        {
            Contract.Requires(0 <= depth);
            Contract.Requires(type != null);

            var str = String.Format("stack_{0}_{1}", depth, TypeHelper.GetTypeName(type));
            return CleanUpIdentifierName(str);
        }
        private static string CleanUpIdentifierName(string s)
        {
            Contract.Requires(s != null);

            return s.Replace('`', '_').Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_').Replace('`', '_');
        }

        private ILocalDefinition GetOrCreateLocal(int depth, ITypeReference type)
        {
            Contract.Requires(0 <= depth);
            Contract.Requires(type != null);

            ILocalDefinition local;
            var key = KeyForLocal(depth, type);
            if (this.createdLocals.TryGetValue(key, out local)) return local;
            local = new LocalDefinition()
            {
                Name = this.host.NameTable.GetNameFor(NameForLocal(depth, type)),
                MethodDefinition = this.methodDefinition,
                Type = type,
            };
            this.createdLocals.Add(key, local);
            return local;
        }

        public override IStatement Rewrite(IPushStatement pushStatement)
        {
            var depth = this.locals.Count;
            var t = pushStatement.ValueToPush.Type;
            var local = this.GetOrCreateLocal(depth, t);
            this.locals.Push(local);
            var assignment = new Assignment()
            {
                Source = pushStatement.ValueToPush,
                Target = new TargetExpression(){ Definition = local, Instance = null, Type = t, },
                Type = t,
            };
            if (this.inThenBranch)
            {
                if (this.thenBranchPushes != null && (t.TypeCode == PrimitiveTypeCode.Int32 || t.TypeCode == PrimitiveTypeCode.Boolean))
                {
                    this.thenBranchPushes.Add(depth, assignment);
                }
            }
            else if (this.inElseBranch)
            {
                if (this.thenBranchPushes != null)
                {
                    if (t.TypeCode == PrimitiveTypeCode.Int32)
                    {
                        Contract.Assume(this.thenBranchPushes.ContainsKey(depth));
                        var a = this.thenBranchPushes[depth];
                        if (a.Type.TypeCode == PrimitiveTypeCode.Boolean)
                        {
                            // then this should be a push of a boolean, not an int
                            Contract.Assume(pushStatement.ValueToPush is ICompileTimeConstant);
                            var ctc = (ICompileTimeConstant) pushStatement.ValueToPush;
                            var boolLocal = a.Target.Definition as ILocalDefinition;
                            assignment.Target = new TargetExpression() { Definition = boolLocal, Instance = null, Type = this.systemBool, };
                            assignment.Source = new CompileTimeConstant() { Type = this.systemBool, Value = ((int)ctc.Value) == 0 ? false : true, };
                            this.locals.Pop();
                            this.locals.Push(boolLocal);
                        }
                    }
                    else if (t.TypeCode == PrimitiveTypeCode.Boolean)
                    {
                        Contract.Assume(this.thenBranchPushes.ContainsKey(depth));
                        var a = this.thenBranchPushes[depth];
                        if (a.Type.TypeCode == PrimitiveTypeCode.Int32)
                        {
                            // then this should have been a push of a boolean, not an int
                            Contract.Assume(a.Source is ICompileTimeConstant);
                            var ctc = (ICompileTimeConstant)a.Source;
                            var boolLocal = a.Target.Definition as ILocalDefinition;
                            Contract.Assume(boolLocal != null);
                            a.Target = new TargetExpression() { Definition = boolLocal, Instance = null, Type = this.systemBool, };
                            a.Source = new CompileTimeConstant() { Type = this.systemBool, Value = ((int)ctc.Value) == 0 ? false : true, };
                        }
                    }
                }
            }
            var expressionStatment = new ExpressionStatement()
            {
                Expression = assignment,
            };
            return expressionStatment;
        }

        public override IExpression Rewrite(IPopValue popValue)
        {
            var t = popValue.Type;
            Contract.Assume(0 < this.locals.Count);
            var local = this.locals.Pop();
            if (this.inThenBranch) {
              var depth = this.locals.Count;
              if (this.thenBranchPushes != null && this.thenBranchPushes.ContainsKey(depth) && local == this.thenBranchPushes[depth].Target.Definition)
                this.thenBranchPushes.Remove(depth);
            }
            var be = new BoundExpression() { Definition = local, Instance = null, Type = local.Type, };
            return be;
        }

        public override IExpression Rewrite(IDupValue dupValue)
        {
            var depth = this.locals.Count;
            var t = dupValue.Type;
            Contract.Assume(0 < this.locals.Count);
            var local = this.locals.Peek();
            this.locals.Push(local);
            var be = new BoundExpression() { Definition = local, Instance = null, Type = t, };
            return be;
        }

        public override void RewriteChildren(ConditionalStatement conditionalStatement)
        {
            // exactly the same code as the base visitor. just need to reset stack
            // depth for each branch.

            this.RewriteChildren((Statement)conditionalStatement);

            conditionalStatement.Condition = this.Rewrite(conditionalStatement.Condition);

            var savedInThenBranch = this.inThenBranch;
            var savedInElseBranch = this.inElseBranch;
            this.inThenBranch = true;
            this.inElseBranch = false;

            var savedThenBranchPushes = this.thenBranchPushes;
            this.thenBranchPushes = new Dictionary<int, Assignment>();

            var savedStack = Copy(this.locals);
            conditionalStatement.TrueBranch = this.Rewrite(conditionalStatement.TrueBranch);
            var stackAfterTrue = Copy(this.locals);

            this.locals = Copy(savedStack);
            this.inThenBranch = false;
            this.inElseBranch = true;
            conditionalStatement.FalseBranch = this.Rewrite(conditionalStatement.FalseBranch);

            Contract.Assume(stackAfterTrue.Count == this.locals.Count);
            // and that the things pushed in both branches are type-compatible
            // (one branch might push a bool and the other an int)

            // continuing on with the stack being the one from the else-branch.
            // is that okay? should it be the one from the then-branch?
            // currently it is important that it is the one from the else-branch
            // because of the fixup code in Rewrite(IPushStatement) that deals
            // with the bool/int confusion

            this.inThenBranch = savedInThenBranch;
            this.inElseBranch = savedInElseBranch;
            this.thenBranchPushes = savedThenBranchPushes;
        }

        private Stack<ILocalDefinition> Copy(Stack<ILocalDefinition> stack)
        {
            var a = stack.ToArray();
            var newStack = new Stack<ILocalDefinition>();
            for (int i = a.Length-1; 0 <= i; i--)
            {
                var l = a[i];
                newStack.Push(l);
            }
            return newStack;
        }

        public override void RewriteChildren(ConstructorOrMethodCall constructorOrMethodCall)
        {
            // Method calls (including calls to ctors) require special handling:
            // any push statements that put arguments on the stack actually need
            // to be popped in the reverse order (reverse to the stack's LIFO semantics).
            // The code generator generates a push for push statements, but the call instruction
            // in IL consumes the stack from last argument to first argument. Thus
            // if the code model had this "push x; push y; M(pop,pop)" keeping the
            // stack as it is created by this rewriter would end up creating "M(y,x)"
            // because the arguments are visited left-to-right.
            var popsFound = 0;
            foreach (var a in constructorOrMethodCall.Arguments)
            {
                if (a is IPopValue) popsFound++;
            }
            if (0 < popsFound)
            {
                ReverseN(popsFound);
            }
            base.RewriteChildren(constructorOrMethodCall);
        }

        private void ReverseN(int n)
        {
            var tmp = new List<ILocalDefinition>();
            for (int i = 0; i < n; i++)
            {
                tmp.Add(this.locals.Pop());
            }
            foreach (var a in tmp)
            {
                this.locals.Push(a);
            }
        }

        public override void RewriteChildren(BinaryOperation binaryOperation)
        {
            var leftPop = binaryOperation.LeftOperand is IPopValue && binaryOperation.LeftOperand.Type.TypeCode != PrimitiveTypeCode.Boolean;
            var rightPop = binaryOperation.RightOperand is IPopValue && binaryOperation.RightOperand.Type.TypeCode != PrimitiveTypeCode.Boolean;
            base.RewriteChildren(binaryOperation);
            if (leftPop && !rightPop && binaryOperation.LeftOperand.Type.TypeCode == PrimitiveTypeCode.Boolean)
            {
                // then left went from int to bool
                binaryOperation.RightOperand = TypeInferencer.Convert(binaryOperation.RightOperand, this.systemBool);
            }
            else if (rightPop && !leftPop && binaryOperation.RightOperand.Type.TypeCode == PrimitiveTypeCode.Boolean)
            {
                // then right went from int to bool
                binaryOperation.LeftOperand = TypeInferencer.Convert(binaryOperation.LeftOperand, this.systemBool);
            }
        }
    }
}