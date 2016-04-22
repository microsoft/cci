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
using Microsoft.Cci.MutableCodeModel;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;
using System.Diagnostics.Contracts;
using System;

namespace Microsoft.Cci.MutableCodeModel
{

    /// <summary>
    /// A rewriter for CodeModel method bodies, which changes any foreach loops found in the body into
    /// var loc := coll.GetEnumerator();
    /// try { while (loc.MoveNext()) { foreach_var := loc.Current; body } }
    /// finally { loc.Dispose(); }
    /// NOTE: It does not modify any foreach loops over array types, which are handled in CodeModelToIL.
    /// </summary>
    public class ForEachRemover : CodeRewriter
    {

        /// <summary>
        /// An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.
        /// </summary>
        ISourceLocationProvider/*?*/ sourceLocationProvider;

        /// <summary>
        /// So foreach loops over the same type of collection can share the same local within a method body
        /// </summary>
        Dictionary<uint, ILocalDefinition> foreachLocals = new Dictionary<uint, ILocalDefinition>();

        private IMethodReference moveNext;
        private IMethodReference disposeMethod;

        /// <summary>
        /// A rewriter for CodeModel method bodies, which changes any foreach loops found in the body into lower level structures.
        /// </summary>
        /// <param name="host">An object representing the application that is hosting the converter. It is used to obtain access to some global
        /// objects and services such as the shared name table and the table for interning references.</param>
        /// <param name="sourceLocationProvider">An object that can map the ILocation objects found in a block of statements to IPrimarySourceLocation objects. May be null.</param>
        public ForEachRemover(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider)
            : base(host)
        {
            this.sourceLocationProvider = sourceLocationProvider;


            this.moveNext = new MethodReference()
            {
                CallingConvention = CallingConvention.HasThis,
                ContainingType = host.PlatformType.SystemCollectionsIEnumerator,
                InternFactory = host.InternFactory,
                Name = host.NameTable.GetNameFor("MoveNext"),
                Parameters = new List<IParameterTypeInformation>(),
                Type = host.PlatformType.SystemBoolean,
            };

            var assemblyReference = new Immutable.AssemblyReference(this.host, this.host.CoreAssemblySymbolicIdentity);
            IUnitNamespaceReference ns = new Immutable.RootUnitNamespaceReference(assemblyReference);
            ns = new Immutable.NestedUnitNamespaceReference(ns, this.host.NameTable.GetNameFor("System"));
            var iDisposable = new Immutable.NamespaceTypeReference(this.host, ns, this.host.NameTable.GetNameFor("IDisposable"), 0, false, false, true, PrimitiveTypeCode.Reference);
            this.disposeMethod = new MethodReference()
            {
                CallingConvention = CallingConvention.HasThis,
                ContainingType = iDisposable,
                InternFactory = host.InternFactory,
                Name = this.host.NameTable.GetNameFor("Dispose"),
                Parameters = new List<IParameterTypeInformation>(),
                Type = this.host.PlatformType.SystemVoid,
            };
        }

        /// <summary>
        /// When given a method definition and a block of statements that represents the Block property of the body of the method
        /// this method returns a semantically equivalent SourceMethod with a body that no longer has any foreach statements that
        /// iterate over non-arrays. Foreach statements that iterate over arrays are handled in CodeModelToIL.
        /// The given block of statements is mutated in place.
        /// </summary>
        /// <param name="method">The method containing the block that is to be rewritten.</param>
        /// <param name="body">The block to be rewritten. 
        /// The entire tree rooted at the block must be mutable and the nodes must not be shared with anything else.</param>
        public void RemoveForEachStatements(IMethodDefinition method, BlockStatement body) {
          this.RewriteChildren(body);
        }

        public override IStatement Rewrite(IForEachStatement forEachStatement)
        {
            ILocalDefinition foreachLocal;
            var key = forEachStatement.Collection.Type.InternedKey;

            ITypeReference enumeratorType;
            IMethodReference getEnumerator;
            IMethodReference getCurrent;

            var gtir = forEachStatement.Collection.Type as IGenericTypeInstanceReference;
            if (gtir != null)
            {
                var typeArguments = gtir.GenericArguments;
                ITypeReference genericEnumeratorType = new Immutable.GenericTypeInstanceReference(this.host.PlatformType.SystemCollectionsGenericIEnumerator, typeArguments, this.host.InternFactory);
                ITypeReference genericEnumerableType = new Immutable.GenericTypeInstanceReference(this.host.PlatformType.SystemCollectionsGenericIEnumerable, typeArguments, this.host.InternFactory);
                enumeratorType = genericEnumeratorType;
                getEnumerator = new SpecializedMethodReference()
                {
                    CallingConvention = CallingConvention.HasThis,
                    ContainingType = genericEnumerableType,
                    InternFactory = this.host.InternFactory,
                    Name = this.host.NameTable.GetNameFor("GetEnumerator"),
                    Parameters = new List<IParameterTypeInformation>(),
                    Type = genericEnumeratorType,
                    UnspecializedVersion = new MethodReference()
                    {
                        CallingConvention = CallingConvention.HasThis,
                        ContainingType = this.host.PlatformType.SystemCollectionsGenericIEnumerable,
                        InternFactory = this.host.InternFactory,
                        Name = this.host.NameTable.GetNameFor("GetEnumerator"),
                        Parameters = new List<IParameterTypeInformation>(),
                        Type = this.host.PlatformType.SystemCollectionsGenericIEnumerator,
                    },
                };
                var getEnumerator2 = (IMethodReference) 
                    IteratorHelper.First(genericEnumerableType.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("GetEnumerator"), false));
                getEnumerator = getEnumerator2;
                getCurrent = (IMethodReference) IteratorHelper.First(genericEnumeratorType.ResolvedType.GetMembersNamed(this.host.NameTable.GetNameFor("get_Current"), false));
            }
            else
            {
                enumeratorType = this.host.PlatformType.SystemCollectionsIEnumerator;
                getEnumerator = new MethodReference()
                {
                    CallingConvention = CallingConvention.HasThis,
                    ContainingType = enumeratorType,
                    InternFactory = this.host.InternFactory,
                    Name = this.host.NameTable.GetNameFor("GetEnumerator"),
                    Parameters = new List<IParameterTypeInformation>(),
                    Type = this.host.PlatformType.SystemCollectionsIEnumerable,
                };
                getCurrent = new MethodReference()
                {
                    CallingConvention = CallingConvention.HasThis,
                    ContainingType = enumeratorType,
                    InternFactory = this.host.InternFactory,
                    Name = this.host.NameTable.GetNameFor("get_Current"),
                    Parameters = new List<IParameterTypeInformation>(),
                    Type = this.host.PlatformType.SystemObject,
                };
            }

            var initializer = new MethodCall()
                    {
                        Arguments = new List<IExpression>(),
                        IsStaticCall = false,
                        IsVirtualCall = true,
                        MethodToCall = getEnumerator,
                        ThisArgument = forEachStatement.Collection,
                        Type = enumeratorType,
                    };
            IStatement initialization;

            if (!this.foreachLocals.TryGetValue(key, out foreachLocal))
            {
                foreachLocal = new LocalDefinition() { Type = enumeratorType, Name = this.host.NameTable.GetNameFor("CS$5$" + this.foreachLocals.Count) };
                this.foreachLocals.Add(key, foreachLocal);
                initialization = new LocalDeclarationStatement()
                {
                    InitialValue = initializer,
                    LocalVariable = foreachLocal,
                };
            }
            else
            {
                initialization = new ExpressionStatement()
                {
                    Expression = new Assignment()
                    {
                        Source = initializer,
                        Target = new TargetExpression()
                        {
                            Definition = foreachLocal,
                            Instance = null,
                            Type = foreachLocal.Type,
                        },
                        Type = foreachLocal.Type,
                    },
                };
            }

            var newStmts = new List<IStatement>();
            newStmts.Add(new ExpressionStatement(){
                                                Expression = new Assignment(){
                                                     Source = new MethodCall(){
                                                          Arguments = new List<IExpression>(),
                                                          IsStaticCall = false,
                                                          IsVirtualCall = true,
                                                          MethodToCall = getCurrent,
                                                          ThisArgument = new BoundExpression(){
                                                               Definition = foreachLocal,
                                                               Instance = null,
                                                          },
                                                          Type = forEachStatement.Variable.Type,
                                                     },
                                                      Target = new TargetExpression(){
                                                           Definition = forEachStatement.Variable,
                                                           Instance = null,
                                                      },
                                                       Type = forEachStatement.Variable.Type,
                                                },
                                           });
            newStmts.Add(forEachStatement.Body);
            var newBody = new BlockStatement(){ Statements = newStmts,}; 
            var result = new BlockStatement()
            {
                Statements = new List<IStatement>(){
                   initialization,
                   new TryCatchFinallyStatement(){
                       TryBody = new BlockStatement() {
                           Statements = new List<IStatement>(){
                               new WhileDoStatement(){
                                   Body = newBody,
                                   Condition = new MethodCall(){
                                       Arguments = new List<IExpression>(),
                                       IsStaticCall = false,
                                       IsVirtualCall = true,
                                       MethodToCall = moveNext,
                                       ThisArgument = new BoundExpression(){ 
                                           Definition = foreachLocal,
                                           Instance = null,
                                       },
                                       Type = this.host.PlatformType.SystemBoolean,
                                   },
                               },
                           },
                       },
                       FinallyBody = new BlockStatement() {
                           Statements = new List<IStatement>(){
                               new ConditionalStatement(){
                                   Condition = new Equality(){
                                       LeftOperand = new BoundExpression(){ Definition = foreachLocal, Instance = null, Type = foreachLocal.Type, },
                                       RightOperand = new CompileTimeConstant(){ Type = foreachLocal.Type, Value = null, },
                                       Type = this.host.PlatformType.SystemBoolean,
                                   },
                                   FalseBranch = new EmptyStatement(),
                                   TrueBranch = new ExpressionStatement(){
                                       Expression = new MethodCall(){
                                           Arguments = new List<IExpression>(),
                                           IsStaticCall = false,
                                           IsVirtualCall = true,
                                           MethodToCall = this.disposeMethod,
                                           ThisArgument = new BoundExpression(){ 
                                               Definition = foreachLocal,
                                               Instance = null,
                                           },
                                           Type = this.host.PlatformType.SystemVoid,
                                       },
                                   },
                               },
                           },
                       },
                   },
                },
            };
            return result;
        }
    }
}
