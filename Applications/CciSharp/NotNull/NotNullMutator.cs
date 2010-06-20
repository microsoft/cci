//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;

namespace CciSharp.Mutators
{
    /// <summary>
    /// A mutator that injects null check preconditions and post-conditions.
    /// This mutator must be executed *before* the runtime rewritter.
    /// </summary>
    public sealed class NotNullMutator
        : CcsMutatorBase
    {
        public NotNullMutator(ICcsHost host)
            : base(host, "Not Null", 10, typeof(NotNullResources))
        { }

        public override bool Visit()
        {
            var assembly = this.Host.MutatedAssembly;
            PdbReader _pdbReader;
            if (!this.Host.TryGetMutatedPdbReader(out _pdbReader))
                _pdbReader = null;
            var contracts = this.Host.MutatedContracts;

            var mutator = new Mutator(this, _pdbReader, contracts);
            mutator.Visit(assembly);
            return mutator.MutationCount > 0;
        }

        class Mutator
            : CcsCodeMutatorBase<NotNullMutator>
        {
            readonly Stack<bool> notNullsContext = new Stack<bool>();
            readonly INamespaceTypeReference booleanType;
            readonly INamespaceTypeReference voidType;
            readonly IMethodReference contractRequiresMethod;

            public Mutator(
                NotNullMutator owner, 
                ISourceLocationProvider reader, 
                ContractProvider contracts)
                : base(owner, reader, contracts)
            {
                this.notNullsContext.Push(false);
                this.booleanType = this.Host.PlatformType.SystemBoolean;
                this.voidType = this.Host.PlatformType.SystemBoolean;
                this.contractRequiresMethod =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.host.PlatformType.SystemDiagnosticsContractsContract,
                        CallingConvention.Default, 
                        this.voidType,
                        this.host.NameTable.GetNameFor("Requires"),
                        0,
                        this.booleanType);
            }

            public int MutationCount { get; private set; }


            public override Assembly Visit(Assembly assembly)
            {
                ICustomAttribute attribute;
                bool hasAttribute = CcsHelper.TryGetAttributeByName(assembly.Attributes, "NotNullAttribute", out attribute);
                this.notNullsContext.Push(hasAttribute || this.notNullsContext.Peek());                
                var result = base.Visit(assembly);
                this.notNullsContext.Pop();
                if (hasAttribute)
                    assembly.Attributes.Remove(attribute);
                return result;
            }

            public override Module Visit(Module module)
            {
                ICustomAttribute attribute;
                bool hasAttribute = CcsHelper.TryGetAttributeByName(module.Attributes, "NotNullAttribute", out attribute);
                this.notNullsContext.Push(hasAttribute || this.notNullsContext.Peek());
                var result = base.Visit(module);
                this.notNullsContext.Pop();
                if (hasAttribute)
                    module.Attributes.Remove(attribute);
                return result;
            }

            protected override void Visit(TypeDefinition typeDefinition)
            {
                ICustomAttribute attribute;
                bool hasAttribute = CcsHelper.TryGetAttributeByName(typeDefinition.Attributes, "NotNullAttribute", out attribute);
                this.notNullsContext.Push(hasAttribute || this.notNullsContext.Peek());
                base.Visit(typeDefinition);
                this.notNullsContext.Pop();
                if (hasAttribute)
                    typeDefinition.Attributes.Remove(attribute);
            }

            public override MethodDefinition Visit(MethodDefinition methodDefinition)
            {
                if (methodDefinition.IsAbstract)
                {
                    // not supported yet
                    return methodDefinition;
                }

                if (methodDefinition.IsVirtual &&
                    (MemberHelper.GetImplicitlyOverriddenBaseClassMethod(methodDefinition) != Dummy.Method ||
                     MemberHelper.GetImplicitlyOverriddenBaseClassMethod(methodDefinition) != Dummy.Method))
                {
                    // do not add requires to overloaded methods
                    return methodDefinition;
                }

                var body = methodDefinition.Body as SourceMethodBody;
                if (body != null)
                {
                    var block = (BlockStatement)body.Block;
                    var parameters = methodDefinition.Parameters;
                    for (int i = parameters.Count - 1; i >= 0; i--)
                        this.VisitParameter(methodDefinition, block, (ParameterDefinition)parameters[i]);
                }

                return methodDefinition;
            }

            private void VisitParameter(MethodDefinition methodDefinition, BlockStatement block, ParameterDefinition parameter)
            {
                Contract.Requires(methodDefinition != null);
                Contract.Requires(block != null);
                Contract.Requires(parameter != null);

                if (!methodDefinition.IsStatic && parameter.Index == 0) return; // this parameter

                var type = parameter.Type;
                ICustomAttribute attribute;
                bool hasAttribute = CcsHelper.TryGetAttributeByName(parameter.Attributes, "NotNullAttribute", out attribute);
                // validate
                if (hasAttribute)
                {
                    if (type.IsValueType)
                    {
                        this.Host.Event(CcsEventLevel.Error, "NotNull may only be applied to reference types");
                        return;
                    }
                }
                if (hasAttribute || this.notNullsContext.Peek())
                {
                    // skip
                    if (type.IsValueType)
                        return;
 
                    // add contract requires
                    var requires = new MethodCall
                    {
                        MethodToCall = this.contractRequiresMethod,
                        IsStaticCall = true,
                        Type = this.voidType
                    };
                    requires.Arguments.Add(new LogicalNot
                    {
                        Type = this.booleanType,
                        Operand = new Equality
                        {
                            LeftOperand = new BoundExpression { Definition = parameter },
                            RightOperand = new CompileTimeConstant { Value = null, Type = parameter.Type },
                            Type = this.booleanType
                        }
                    });

                    block.Statements.Insert(0, new ExpressionStatement { Expression = requires });
                }

                if (hasAttribute)
                    parameter.Attributes.Remove(attribute);
            }
        }
    }
}
