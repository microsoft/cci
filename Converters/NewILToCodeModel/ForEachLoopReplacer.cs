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
	internal class ForEachLoopReplacer : CodeTraverser
	{
		private IMetadataHost host;

		internal ForEachLoopReplacer(SourceMethodBody sourceMethodBody)
		{
			Contract.Requires(sourceMethodBody != null);
			this.host = sourceMethodBody.host; Contract.Assume(sourceMethodBody.host != null);
		}

		[ContractInvariantMethod]
		private void ObjectInvariant()
		{
			Contract.Invariant(this.host != null);
		}

        private bool SupportsIEnumerator(ITypeDefinition typeDefinition)
        {
            if (TypeHelper.Type1ImplementsType2(typeDefinition, this.host.PlatformType.SystemCollectionsGenericIEnumerator))
                return true;
            if (TypeHelper.Type1ImplementsType2(typeDefinition, this.host.PlatformType.SystemCollectionsIEnumerator))
                return true;
            if (TypeHelper.TypesAreEquivalent(this.host.PlatformType.SystemCollectionsGenericIEnumerator, typeDefinition))
                return true;
            if (TypeHelper.TypesAreEquivalent(this.host.PlatformType.SystemCollectionsIEnumerator, typeDefinition))
                return true;
            return false;
        }

		public override void TraverseChildren(IBlockStatement block)
		{
			Contract.Assume(block is BlockStatement);
			var decompiledBlock = (BlockStatement)block;
			var statements = decompiledBlock.Statements;

			for (int i = 0; i < statements.Count; ++i)
			{
				var resourceUse = statements[i] as ResourceUseStatement;
				if (resourceUse == null) continue;

				var resourceUseBody = resourceUse.Body as BlockStatement;
				if (resourceUseBody == null) continue;

				var resourceUseStatements = resourceUseBody.Statements;
				if (resourceUseStatements.Count != 1) continue;

                ILocalDefinition resourceVar;
                IExpression initialValue;
				var enumeratorDeclaration = resourceUse.ResourceAcquisitions as ILocalDeclarationStatement;
                if (enumeratorDeclaration == null)
                {
                    var es = resourceUse.ResourceAcquisitions as IExpressionStatement;
                    if (es == null) continue;
                    var assign = es.Expression as IAssignment;
                    if (assign == null) continue;
                    if (assign.Target.Instance != null) continue;
                    resourceVar = assign.Target.Definition as ILocalDefinition;
                    if (resourceVar == null) continue;
                    initialValue = assign.Source;
                }
                else
                {
                    resourceVar = enumeratorDeclaration.LocalVariable;
                    initialValue = enumeratorDeclaration.InitialValue;
                }

				var collection = this.MatchMethodCall(initialValue, "GetEnumerator");
				if (collection == null) continue;

                var isEnumerator = TypeHelper.TypesAreEquivalent(resourceVar.Type, this.host.PlatformType.SystemCollectionsIEnumerator) || this.MatchInterface(resourceVar, "System.Collections.IEnumerator");
				if (!isEnumerator) continue;

				var loop = resourceUseStatements.Last() as IWhileDoStatement;
				if (loop == null) continue;

                var methodCall = loop.Condition as IMethodCall;
                if (methodCall == null) continue;
                var be = methodCall.ThisArgument as IBoundExpression;
                if (be != null)
                {
                    if (be.Instance != null) continue;
                    if (be.Definition != resourceVar) continue;
                }
                else
                {
                    var addrOf = methodCall.ThisArgument as IAddressOf;
                    if (addrOf == null) continue;
                    var ae = addrOf.Expression as IAddressableExpression;
                    if (ae == null) continue;
                    if (ae.Instance != null) continue;
                    if (ae.Definition != resourceVar) continue;
                }
                var ct = methodCall.MethodToCall.ContainingType.ResolvedType;
                if (!SupportsIEnumerator(ct)) continue;
                if (methodCall.MethodToCall.Name.Value != "MoveNext") continue;

				var loopBody = loop.Body as BlockStatement;
				if (loopBody == null || loopBody.Statements.Count < 2) continue;

                var c = loopBody.Statements.Count;
                var j = 0;
                while (j < c && !(loopBody.Statements[j] is ILocalDeclarationStatement)) j++;
                if (j == c) continue;
                var lds = loopBody.Statements[j] as ILocalDeclarationStatement;
                methodCall = lds.InitialValue as IMethodCall;
                if (methodCall == null)
                {
                    // it might be "LDS x, null; x := get_Current();"
                    if (lds.InitialValue != null) continue;
                    var k = j+1;
                    if (loopBody.Statements[k] is ILabeledStatement) k++;
                    var es = loopBody.Statements[k] as IExpressionStatement;
                    if (es == null) continue;
                    var assign = es.Expression as IAssignment;
                    if (assign == null) continue;
                    if (assign.Target.Instance != null) continue;
                    var loc = assign.Target.Definition as ILocalDefinition;
                    if (loc == null) continue;
                    methodCall = assign.Source as IMethodCall;
                    if (methodCall == null) continue;
                    loopBody.Statements.RemoveAt(k);
                }
                be = methodCall.ThisArgument as IBoundExpression;
                if (be != null)
                {
                    if (be.Instance != null) continue;
                    if (be.Definition != resourceVar) continue;
                }
                else
                {
                    var addrOf = methodCall.ThisArgument as IAddressOf;
                    if (addrOf == null) continue;
                    var ae = addrOf.Expression as IAddressableExpression;
                    if (ae == null) continue;
                    if (ae.Instance != null) continue;
                    if (ae.Definition != resourceVar) continue;
                }
                var enumerator = lds.LocalVariable;
                ct = TypeHelper.UninstantiateAndUnspecialize(methodCall.MethodToCall.ContainingType).ResolvedType;
                if (!SupportsIEnumerator(ct)) continue;
                if (methodCall.MethodToCall.Name.Value != "get_Current") continue;

				var body = loopBody;
				body.Statements.RemoveAt(j);

				var forEachStatement = new ForEachStatement()
				{
					Locations = resourceUse.Locations,
                    Variable = enumerator,
					Collection = collection,
					Body = body
				};

				statements[i] = forEachStatement;
			}

			base.TraverseChildren(block);
		}

		private bool MatchInterface(ILocalDefinition variable, string interfaceName)
		{
			var resolvedType = variable.Type.ResolvedType;
			var implementInterface = resolvedType.Interfaces.Any(x => x.ToString() == interfaceName);
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

		private IExpression MatchMethodCall(IExpression expression, string methodName)
		{
			var methodCall = expression as IMethodCall;
			if (methodCall == null || methodCall.MethodToCall.Name.Value != methodName) return null;
			return methodCall.ThisArgument;
		}
	}
}
