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
    /// The [WeakLazy] attribute namespace and assembly does not matter.
    /// </remarks>
    public sealed class WeakLazyPropertyMutator
        : CcsMutatorBase
    {
        readonly INamespaceTypeReference nonSerializedAttribute;
        readonly INamedTypeReference weakReferenceType;
        public WeakLazyPropertyMutator(ICcsHost host)
            : base(host, "Weak Lazy Property", 1, typeof(WeakLazyPropertyResources))
        {
            var types = new NetTypes(host);
            this.nonSerializedAttribute = types.SystemNonSerializedAttribute;
            this.weakReferenceType = types.SystemWeakReference;
        }

        class NetTypes : Microsoft.Cci.Immutable.PlatformType
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

            public INamespaceTypeReference SystemWeakReference
            {
                get
                {
                    return this.CreateReference(
                        this.CoreAssemblyRef,
                        "System", "WeakReference");
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
            mutator.RewriteChildren(assembly);
            return mutator.MutationCount > 0;
        }

        private void Error(PropertyDefinition propertyDefinition, string message)
        {
            this.Host.Event(CcsEventLevel.Error, "{0} {1}", propertyDefinition, message);
        }

        class Mutator
            : CcsCodeMutatorBase<WeakLazyPropertyMutator>
        {
            readonly INamespaceTypeReference objectType;
            readonly INamespaceTypeReference booleanType;
            public Mutator(WeakLazyPropertyMutator owner, ISourceLocationProvider _pdbReader,
                ContractProvider contracts)
                : base(owner, _pdbReader, contracts)
            {
                this.objectType = this.Host.PlatformType.SystemObject;
                this.booleanType = this.Host.PlatformType.SystemBoolean; 
            }

            public int MutationCount { get; private set; }

            public override void RewriteChildren(PropertyDefinition propertyDefinition)
            {
                var getter = propertyDefinition.Getter as MethodDefinition;
                var setter = propertyDefinition.Setter as MethodDefinition;
                ICustomAttribute lazyAttribute;
                if (!CcsHelper.TryGetAttributeByName(propertyDefinition.Attributes, "WeakLazyAttribute", out lazyAttribute)) // [WeakLazy]
                    return;

                if (setter != null)
                {
                    this.Owner.Error(propertyDefinition, "cannot have a setter to be weak lazy");
                    return;
                }
                if (getter == null)
                {
                    this.Owner.Error(propertyDefinition, "must have a getter to be weak lazy");
                    return;
                }
                if (getter.IsStatic)
                {
                    this.Owner.Error(propertyDefinition, "must be an instance property to be weak lazy");
                    return;
                }
                if (getter.IsVirtual)
                {
                    this.Owner.Error(propertyDefinition, "cannot be virtual to be weak lazy");
                    return;
                }
                if (getter.ParameterCount > 0)
                {
                    this.Owner.Error(propertyDefinition, "must not be an indexer to be weak lazy");
                    return;
                }
                if (propertyDefinition.Type.IsValueType)
                {
                    this.Owner.Error(propertyDefinition, "must a reference type to be weak lazy");
                    return;
                }

                // ok we're good,
                // 1# add a field for the result and to check if the value was set
                var declaringType = (NamedTypeDefinition)propertyDefinition.ContainingTypeDefinition;

                var resultFieldDefinition =
                    this.DefineField(declaringType, propertyDefinition);

                // 2# generate a new method for the getter and move the old getter
                // somewhere else
                this.DefineUncachedGetter(declaringType, propertyDefinition, getter, resultFieldDefinition);

                // 3# remove the lazy attribute
                propertyDefinition.Attributes.Remove(lazyAttribute);

                this.MutationCount++;
                return;
            }

            private void DefineUncachedGetter(
                NamedTypeDefinition declaringType,
                PropertyDefinition propertyDefinition, 
                MethodDefinition getter, 
                FieldDefinition resultFieldDefinition)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(propertyDefinition != null);
                Contract.Requires(getter != null);
                Contract.Requires(resultFieldDefinition != null);

                var uncachedGetter = new MethodDefinition();
                uncachedGetter.Copy(getter, this.Host.InternFactory);
                var name = getter.Name.Value;
                if (name.StartsWith("get_")) name = name.Substring("get_".Length);
                uncachedGetter.Name = this.Host.NameTable.GetNameFor("Get" + name + "Uncached");
                uncachedGetter.Locations.AddRange(getter.Locations);
                uncachedGetter.Visibility = TypeMemberVisibility.Private;

                // replace getter body
                var body = new SourceMethodBody(this.Host, this.sourceLocationProvider)
                {
                    MethodDefinition = uncachedGetter
                };
                if (getter.Attributes == null)
                    getter.Attributes = new List<ICustomAttribute>();
                getter.Attributes.Add(this.CompilerGeneratedAttribute);
                getter.Body = body;
                getter.Locations.Clear();
                var bodyBlock = new BlockStatement();
                body.Block = bodyBlock;
                // var value = this.weakValue != null ? this.weakValue.Value as T : null;
                // if (value == null)
                //      this.weakValue = new WeakReference(value = this.get_Value());
                // return value;
                var valueLocal = new LocalDefinition
                {
                     Type = propertyDefinition.Type,
                     Name = this.Host.NameTable.GetNameFor("value")
                };
                // var value = this.weakValue != null ? this.weakValue.Value as T : null;
                var valueLocalDefinition = new LocalDeclarationStatement
                {
                    LocalVariable = valueLocal,
                    InitialValue = new Conditional
                    {
                        Type = valueLocal.Type,
                        Condition = new Equality
                        {
                            Type = booleanType,
                            LeftOperand = new BoundExpression
                            {
                                Type = resultFieldDefinition.Type,
                                Instance = new ThisReference(),
                                Definition = resultFieldDefinition
                            },
                            RightOperand = new CompileTimeConstant
                            {
                                Type = resultFieldDefinition.Type,
                                Value = null
                            }
                        },
                        ResultIfTrue = new CompileTimeConstant
                        {
                            Type = valueLocal.Type,
                            Value = null
                        },
                        ResultIfFalse = new CastIfPossible {
                             TargetType = valueLocal.Type,
                             Type = valueLocal.Type,
                             ValueToCast = new MethodCall
                            {
                                Type = this.objectType, 
                                MethodToCall = this.WeakReferenceTargetGet,
                                IsStaticCall = false,
                                ThisArgument = new BoundExpression {
                                     Instance = new ThisReference(),
                                     Definition = resultFieldDefinition,
                                     Type = this.Owner.weakReferenceType
                                }
                            }
                        }
                    }
                };

                //    value =  this.get_Value();
                var getValueCall = new Assignment
                {
                    Type = valueLocal.Type,
                    Source = new MethodCall
                    {
                        MethodToCall = uncachedGetter,
                        ThisArgument = new ThisReference(),
                        Type = propertyDefinition.Type
                    },
                    Target = new TargetExpression
                    {
                        Type = valueLocal.Type,
                        Definition = valueLocal
                    }
                };
                //      new WeakReference(this.get_Value());
                var newWeakReference = new CreateObjectInstance
                {
                    MethodToCall = this.WeakReferenceCtor,
                    Type = this.Owner.weakReferenceType
                };
                newWeakReference.Arguments.Add(getValueCall);
                var ifStatement = new ConditionalStatement
                {
                    // if (value == null) {...}
                    Condition = new LogicalNot
                    {
                        Type = booleanType,
                        Operand = new BoundExpression
                        {
                            Type = booleanType,
                            Definition = valueLocal
                        }
                    },
                    //      this.weakValue = new WeakReference(value = this.get_Value());
                    TrueBranch = new ExpressionStatement
                    {
                        Expression = new Assignment
                        {
                            Type = resultFieldDefinition.Type,
                            Source = newWeakReference,
                            Target = new TargetExpression
                            {
                                Type = resultFieldDefinition.Type,
                                Instance = new ThisReference(),
                                Definition = resultFieldDefinition
                            }
                        }
                    },
                    FalseBranch = new EmptyStatement()
                };

                // return this.value$Value;
                bodyBlock.Statements.Add(valueLocalDefinition);
                bodyBlock.Statements.Add(ifStatement);
                bodyBlock.Statements.Add(
                    new ReturnStatement
                    {
                        Expression = new BoundExpression
                        {
                            Type = valueLocal.Type,
                            Definition = valueLocal
                        }
                    });
                declaringType.Methods.Add(uncachedGetter);
            }

            private FieldDefinition DefineField(
                NamedTypeDefinition declaringType,
                IPropertyDefinition propertyDefinition)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(propertyDefinition != null);
                Contract.Ensures(Contract.Result<FieldDefinition>() != null);

                var resultFieldDefinition = new FieldDefinition
                {
                    Type = this.Owner.weakReferenceType,
                    Visibility = TypeMemberVisibility.Private,
                    Name = this.Host.NameTable.GetNameFor("_" + propertyDefinition.Name + "$Weak"),
                    InternFactory = this.Host.InternFactory,
                    Attributes = new List<ICustomAttribute> { this.CompilerGeneratedAttribute, this.NonSerializedAttribute },
                };
                if (declaringType.Fields == null)
                    declaringType.Fields = new List<IFieldDefinition>();
                declaringType.Fields.Add(resultFieldDefinition);

                return resultFieldDefinition;
            }

            public IMethodReference WeakReferenceCtor
            {
                get
                {
                    if (this.weakReferenceCtor == null)
                        this.weakReferenceCtor = new Microsoft.Cci.MethodReference(this.Host,
                            this.Owner.weakReferenceType,
                           CallingConvention.HasThis, this.Host.PlatformType.SystemVoid, this.Host.NameTable.Ctor, 0, 
                           this.objectType);
                    return this.weakReferenceCtor;
                }
            }
            private IMethodReference/*?*/ weakReferenceCtor;

            public IMethodReference WeakReferenceTargetGet
            {
                get
                {
                    if (this._weakReferenceTargetGet == null)
                        this._weakReferenceTargetGet = new Microsoft.Cci.MethodReference(this.Host,
                            this.Owner.weakReferenceType,
                           CallingConvention.HasThis, this.objectType, 
                           this.Host.NameTable.GetNameFor("get_Target"), 0);
                    return this._weakReferenceTargetGet;
                }
            }
            private IMethodReference/*?*/ _weakReferenceTargetGet;

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
