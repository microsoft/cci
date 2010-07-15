//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;

namespace CciSharp.Mutators
{
    /// <summary>
    /// Make property getters lazy
    /// </summary>
    /// <remarks>
    /// The [Lazy] attribute namespace and assembly does not matter.
    /// </remarks>
    public sealed class LazyPropertyMutator
        : CcsMutatorBase
    {
        readonly INamespaceTypeReference nonSerializedAttribute;
        public LazyPropertyMutator(ICcsHost host)
            : base(host, "Lazy Property", 1, typeof(LazyPropertyResources))
        {
            var types = new NetTypes(host);
            this.nonSerializedAttribute = types.SystemNonSerializedAttribute;
        }

        class NetTypes : PlatformType
        {
            public NetTypes(IMetadataHost host)
                : base(host) { }
            public INamespaceTypeReference SystemNonSerializedAttribute
            {
                get
                {
                    return this.CreateReference(
                        this.CoreAssemblyRef,
                        "System", "NonSerializedAttribute");
                }
            }
        }

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

        private void Error(PropertyDefinition propertyDefinition, string message)
        {
            this.Host.Event(CcsEventLevel.Error, "{0} {1}", propertyDefinition, message);
        }

        class Mutator
            : CcsCodeMutatorBase<LazyPropertyMutator>
        {
            readonly INamespaceTypeReference booleanType;
            public Mutator(
                LazyPropertyMutator owner, 
                ISourceLocationProvider _pdbReader, 
                ContractProvider contracts)
                : base(owner, _pdbReader, contracts)
            {
                this.booleanType = this.Host.PlatformType.SystemBoolean; 
            }

            public int MutationCount { get; private set; }

            public override PropertyDefinition Visit(PropertyDefinition propertyDefinition)
            {
                var getter = propertyDefinition.Getter as MethodDefinition;
                var setter = propertyDefinition.Setter as MethodDefinition;
                ICustomAttribute lazyAttribute;
                if (!CcsHelper.TryGetAttributeByName(propertyDefinition.Attributes, "LazyAttribute", out lazyAttribute)) // [ReadOnly]
                    return propertyDefinition;

                if (setter != null)
                {
                    this.Owner.Error(propertyDefinition, "cannot have a setter to be lazy");
                    return propertyDefinition;
                }
                if (getter == null)
                {
                    this.Owner.Error(propertyDefinition, "must have a getter to be lazy");
                    return propertyDefinition;
                }
                if (getter.IsStatic)
                {
                    this.Owner.Error(propertyDefinition, "must be an instance property to be lazy");
                    return propertyDefinition;
                }
                if (getter.IsVirtual)
                {
                    this.Owner.Error(propertyDefinition, "cannot be virtual to be lazy");
                    return propertyDefinition;
                }
                if (getter.ParameterCount > 0)
                {
                    this.Owner.Error(propertyDefinition, "must not be an indexer to be lazy");
                    return propertyDefinition;
                }

                // ok we're good,
                // 1# add a field for the result and to check if the value was set
                var declaringType = (TypeDefinition)propertyDefinition.ContainingTypeDefinition;
                FieldDefinition resultFieldDefinition;
                FieldDefinition resultInitializedFieldDefinition;
                this.DefineFields(declaringType, propertyDefinition, out resultFieldDefinition, out resultInitializedFieldDefinition);

                // 2# generate a new method for the getter and move the old getter
                // somewhere else
                this.DefineUncachedGetter(declaringType, propertyDefinition, getter, resultFieldDefinition, resultInitializedFieldDefinition);

                // 3# remove the lazy attribute
                propertyDefinition.Attributes.Remove(lazyAttribute);

                this.Owner.Host.Event(CcsEventLevel.Message,"lazy: {0}", propertyDefinition);
                this.MutationCount++;
                return propertyDefinition;
            }

            private void DefineUncachedGetter(
                TypeDefinition declaringType,
                PropertyDefinition propertyDefinition, 
                MethodDefinition getter, 
                FieldDefinition resultFieldDefinition, 
                FieldDefinition resultInitializedFieldDefinition)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(propertyDefinition != null);
                Contract.Requires(getter != null);
                Contract.Requires(resultFieldDefinition != null);
                Contract.Requires(resultInitializedFieldDefinition != null);

                // create new method that holds the uncache method implementation
                var uncachedGetter = new MethodDefinition();
                uncachedGetter.Copy(getter, this.Host.InternFactory);
                var name = getter.Name.Value;
                if (name.StartsWith("get_")) name = name.Substring("get_".Length);
                uncachedGetter.Name = this.Host.NameTable.GetNameFor("Get" + name + "Uncached");
                uncachedGetter.Locations.AddRange(getter.Locations);
                uncachedGetter.Visibility = TypeMemberVisibility.Private;
                SourceMethodBody smb = (SourceMethodBody) uncachedGetter.Body;
                smb.MethodDefinition = uncachedGetter;
                // clone contracts as well
                var getterContracts = this.contractProvider.GetMethodContractFor(getter);
                if(getterContracts != null)
                    this.contractProvider.AssociateMethodWithContract(uncachedGetter, new MethodContract(getterContracts));

                // replace getter body
                var body = new SourceMethodBody(this.Host, this.sourceLocationProvider)
                {
                    MethodDefinition = getter
                };
                getter.Attributes.Add(this.CompilerGeneratedAttribute);
                getter.Body = body;
                getter.Locations.Clear();
                var bodyBlock = new BlockStatement();
                body.Block = bodyBlock;
                // if (!this.value$Init)
                // { 
                //      this.value$Value = this.get_Value();
                //      this.value$Init = true;
                // }
                // return this.value$Value;
                var block = new BlockStatement();
                //      this.value$Value = this.get_Value();
                block.Statements.Add(
                    new ExpressionStatement
                    {
                        Expression = new Assignment
                        {
                            Type = propertyDefinition.Type,
                            Source = new MethodCall
                            {
                                MethodToCall = uncachedGetter,
                                ThisArgument = new ThisReference(),
                                Type = propertyDefinition.Type
                            },
                            Target = new TargetExpression
                            {
                                Type = resultFieldDefinition.Type,
                                Instance = new ThisReference(),
                                Definition = resultFieldDefinition
                            }
                        }
                    }
                    );
                //      this.value$Init = true;
                block.Statements.Add(
                    new ExpressionStatement
                    {
                        Expression = new Assignment
                        {
                            Type = booleanType,
                            Source = new CompileTimeConstant { Type = booleanType, Value = true },
                            Target = new TargetExpression
                            {
                                Type = booleanType,
                                Instance = new ThisReference(),
                                Definition = resultInitializedFieldDefinition
                            }
                        }
                    }
                    );
                // if (!this.value$Init) {...}
                var ifStatement = new ConditionalStatement
                {
                    Condition = new LogicalNot
                    {
                        Type = booleanType,
                        Operand = new BoundExpression
                        {
                            Type = booleanType,
                            Instance = new ThisReference(),
                            Definition = resultInitializedFieldDefinition
                        }
                    },
                    TrueBranch = block,
                    FalseBranch = new EmptyStatement()
                };

                // return this.value$Value;
                bodyBlock.Statements.Add(ifStatement);
                bodyBlock.Statements.Add(
                    new ReturnStatement
                    {
                        Expression = new BoundExpression
                        {
                            Type = resultFieldDefinition.Type,
                            Instance = new ThisReference(),
                            Definition = resultFieldDefinition
                        }
                    });
                declaringType.Methods.Add(uncachedGetter);
            }

            private void DefineFields(
                TypeDefinition declaringType,
                PropertyDefinition propertyDefinition, 
                out FieldDefinition resultFieldDefinition, 
                out FieldDefinition resultInitializedFieldDefinition)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(propertyDefinition != null);
                Contract.Ensures(Contract.ValueAtReturn(out resultFieldDefinition) != null);
                Contract.Ensures(Contract.ValueAtReturn(out resultInitializedFieldDefinition) != null);
                Contract.Ensures(resultFieldDefinition.Type == propertyDefinition.Type);
                Contract.Ensures(resultInitializedFieldDefinition.Type == this.booleanType);

                resultFieldDefinition = new FieldDefinition
                {
                    Type = propertyDefinition.Type,
                    Visibility = TypeMemberVisibility.Private,
                    Name = this.Host.NameTable.GetNameFor(propertyDefinition.Name + "$Value")
                };
                resultFieldDefinition.Attributes.Add(this.CompilerGeneratedAttribute);
                resultFieldDefinition.Attributes.Add(this.NonSerializedAttribute);
                declaringType.Fields.Add(resultFieldDefinition);
                resultInitializedFieldDefinition = new FieldDefinition
                {
                    Type = booleanType,
                    Visibility = TypeMemberVisibility.Private,
                    Name = this.Host.NameTable.GetNameFor(propertyDefinition.Name + "$Init")
                };
                resultInitializedFieldDefinition.Attributes.Add(this.CompilerGeneratedAttribute);
                resultInitializedFieldDefinition.Attributes.Add(this.NonSerializedAttribute);
                declaringType.Fields.Add(resultInitializedFieldDefinition);
            }

            public CustomAttribute CompilerGeneratedAttribute
            {
                get
                {
                    if (this.compilerGeneratedCtor == null)
                        this.compilerGeneratedCtor = new Microsoft.Cci.MethodReference(this.Host,
                          this.Host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
                           CallingConvention.HasThis, this.Host.PlatformType.SystemVoid, this.Host.NameTable.Ctor, 0);
                    return new CustomAttribute { Constructor = this.compilerGeneratedCtor };
                }
            }
            private IMethodReference/*?*/ compilerGeneratedCtor;

            public CustomAttribute NonSerializedAttribute
            {
                get
                {
                    if (this.nonSerializedAttributeCtor == null)
                        this.nonSerializedAttributeCtor = new Microsoft.Cci.MethodReference(this.Host,
                          this.Owner.nonSerializedAttribute,
                           CallingConvention.HasThis, this.Host.PlatformType.SystemVoid, this.Host.NameTable.Ctor, 0);
                    return new CustomAttribute { Constructor = this.nonSerializedAttributeCtor };
                }
            }
            private IMethodReference/*?*/ nonSerializedAttributeCtor;
        }
    }
}
