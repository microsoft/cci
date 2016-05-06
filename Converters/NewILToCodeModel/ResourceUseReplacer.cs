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

using System.Linq;

namespace Microsoft.Cci.ILToCodeModel
{
	internal class ResourceUseReplacer : CodeTraverser
	{
		private IMetadataHost host;
        private readonly ITypeReference IDisposable;

		internal ResourceUseReplacer(SourceMethodBody sourceMethodBody)
		{
			Contract.Requires(sourceMethodBody != null);
			this.host = sourceMethodBody.host;
            Contract.Assume(sourceMethodBody.host != null);
            this.IDisposable = CreateTypeReference(
                host,
                new Microsoft.Cci.MutableCodeModel.AssemblyReference() { AssemblyIdentity = this.host.CoreAssemblySymbolicIdentity, }, "System.IDisposable");
        }

		[ContractInvariantMethod]
		private void ObjectInvariant()
		{
			Contract.Invariant(this.host != null);
            Contract.Invariant(this.IDisposable != null);
		}

        /// <summary>
        /// Creates a type reference anchored in the given assembly reference and whose names are relative to the given host.
        /// When the type name has periods in it, a structured reference with nested namespaces is created.
        /// </summary>
        private static INamespaceTypeReference CreateTypeReference(IMetadataHost host, IAssemblyReference assemblyReference, string typeName)
        {
            IUnitNamespaceReference ns = new Immutable.RootUnitNamespaceReference(assemblyReference);
            string[] names = typeName.Split('.');
            for (int i = 0, n = names.Length - 1; i < n; i++)
                ns = new Immutable.NestedUnitNamespaceReference(ns, host.NameTable.GetNameFor(names[i]));
            return new Immutable.NamespaceTypeReference(host, ns, host.NameTable.GetNameFor(names[names.Length - 1]), 0, false, false, true, PrimitiveTypeCode.NotPrimitive);
        }


        /// <summary>
        /// Looking for the pattern:
        /// IDisposable&lt;T&gt; variable = e;
        /// try {
        ///   using_body
        /// } finally {
        ///   if (variable != null) {
        ///     variable.Dispose();
        ///   }
        /// }
        /// The type of the variable is *not* actually IDisposable, but some
        /// type that implements IDisposable.
        /// The assignment might be a local-declaration statement or else it
        /// might be an assignment statement.
        /// </summary>
		public override void TraverseChildren(IBlockStatement block)
		{
			Contract.Assume(block is BlockStatement);
			var decompiledBlock = (BlockStatement)block;
			var statements = decompiledBlock.Statements;

			for (int i = 2; i < statements.Count; ++i)
			{
				var tryFinally = statements[i] as TryCatchFinallyStatement;
				if (tryFinally == null) continue;

                var finallyBody = tryFinally.FinallyBody;
                if (finallyBody == null) continue;
                var finallyStatements = finallyBody.Statements;
                if (tryFinally.CatchClauses.Any()) continue;

                ILocalDefinition variable;
                IStatement resourceAcquisition;
				var variableDeclaration = statements[i - 2] as ILocalDeclarationStatement;
                if (variableDeclaration == null) {
                    var es = statements[i - 2] as IExpressionStatement;
                    if (es == null) continue;
                    var assign = es.Expression as IAssignment;
                    if (assign == null) continue;
                    if (assign.Target.Instance != null) continue;
                    var loc = assign.Target.Definition as ILocalDefinition;
                    if (loc == null) continue;
                    variable = loc;
                    resourceAcquisition = es;
                } else {
                    variable = variableDeclaration.LocalVariable;
                    resourceAcquisition = variableDeclaration;
                }

                // finally block either looks like:
                //    variable.Dispose();
                // or
                //    if (variable != null) variable.Dispose();
                // or
                //    iDisposableLocalVar := variable as IDisposable;
                //    if (iDisposableLocalVar != null) iDisposableLocalVar.Dispose();
                var c = finallyStatements.Count();
                if (c == 1)
                {
                    var expression = finallyStatements.Single() as IExpressionStatement;
                    var isDispose = this.MatchMethodCall(expression.Expression, variable, "Dispose");
                    if (!isDispose) continue;
                } else if (c == 3 || c == 4)
                {
                    IBoundExpression be;
                    ILocalDefinition iDisposableVariable = variable;
                    var index = 0;
                    if (c == 4)
                    {
                        var es = finallyStatements.ElementAt(index++) as IExpressionStatement;
                        if (es == null) continue;
                        var assignment = es.Expression as IAssignment;
                        if (assignment == null) continue;
                        var castIfPossible = assignment.Source as ICastIfPossible;
                        if (castIfPossible == null) continue;
                        if (!TypeHelper.TypesAreEquivalent(castIfPossible.TargetType, this.IDisposable)) continue;
                        be = castIfPossible.ValueToCast as IBoundExpression;
                        if (be == null) continue;
                        if (be.Instance != null) continue;
                        if (be.Definition != variable) continue;
                        if (assignment.Target.Instance != null) continue;
                        iDisposableVariable = assignment.Target.Definition as ILocalDefinition;
                        if (iDisposableVariable == null) continue;
                    }
                    var conditional = finallyStatements.ElementAt(index++) as IConditionalStatement;
                    if (conditional == null) continue;
                    var expressionStatement = finallyStatements.ElementAt(index++) as IExpressionStatement;
                    if (expressionStatement == null) continue;
                    var lableledStatement = finallyStatements.ElementAt(index++) as ILabeledStatement;
                    if (lableledStatement == null) continue;

                    var equality = conditional.Condition as IEquality;
                    if (equality == null) continue;
                    be = equality.LeftOperand as IBoundExpression;
                    if (be == null) continue;
                    if (be.Instance != null) continue;
                    if (be.Definition != iDisposableVariable) continue;
                    if (!(conditional.FalseBranch is IEmptyStatement)) continue;
                    var gotoStatement = conditional.TrueBranch as IGotoStatement;
                    if (gotoStatement == null) continue;
                    if (gotoStatement.TargetStatement != lableledStatement) continue;
                    var methodCall = expressionStatement.Expression as IMethodCall;
                    if (methodCall == null) continue;
                    var ct = methodCall.MethodToCall.ContainingType;
                    if (!TypeHelper.TypesAreEquivalent(ct, this.IDisposable)) continue;
                }
                else
                {
                    continue;
                }

				var resourceUse = new ResourceUseStatement()
				{
					Locations = tryFinally.Locations,
                    ResourceAcquisitions = resourceAcquisition,
					Body = tryFinally.TryBody
				};

				statements[i] = resourceUse;
				statements.RemoveAt(i - 2);
				i--;
			}

			base.TraverseChildren(block);
		}

		private bool ImplementsIDisposable(ILocalDefinition variable, string interfaceName)
		{
            var resolvedType = variable.Type.ResolvedType;
            var implementInterface = resolvedType.Interfaces.Any(x => TypeHelper.TypesAreEquivalent(x, this.IDisposable));
            return implementInterface;
		}

		private bool MatchMethodCall(IExpression expression, ILocalDefinition variable, string methodName)
		{
			var methodCall = expression as IMethodCall;
			if (methodCall == null || methodCall.MethodToCall.Name.Value != methodName) return false;

			var addressOf = methodCall.ThisArgument as IAddressOf;
			if (addressOf == null || addressOf.Expression.Definition != variable) return false;

			return true;
		}
	}
}
