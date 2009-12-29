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

namespace CciSharp.Mutators
{
    /// <summary>
    /// Uses System.Lazy to turn make properties lazy.
    /// </summary>
    /// <remarks>
    /// The [Lazy] attribute namespace and assembly does not matter.
    /// </remarks>
    public sealed class LazyAutoPropertyMutator
        : CcsMutatorBase
    {
        public LazyAutoPropertyMutator(ICcsHost host)
            : base(host, "Lazy Auto Property", 1, typeof(LazyAutoPropertyResources))
        {
        }

        public override bool Visit(Module module, PdbReader pdbReader)
        {
            var mutator = new Mutator(this, pdbReader);
            mutator.Visit(module);
            return mutator.MutationCount > 0;
        }

        private void Error(PropertyDefinition propertyDefinition, string message)
        {
            this.Host.Event(CcsEventLevel.Error, "{0} {1}", propertyDefinition, message);
        }

        class Mutator
            : CcsCodeMutatorBase<LazyAutoPropertyMutator>
        {
            public Mutator(LazyAutoPropertyMutator owner, ISourceLocationProvider _pdbReader)
                : base(owner, _pdbReader)
            { }

            public int MutationCount { get; private set; }

            public override PropertyDefinition Visit(PropertyDefinition propertyDefinition)
            {
                var getter = propertyDefinition.Getter as MethodDefinition;
                var setter = propertyDefinition.Setter as MethodDefinition;
                if (!ContainsLazy(propertyDefinition.Attributes)) // [ReadOnly]
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

                var booleanType = this.Host.PlatformType.SystemBoolean;
                // ok we're good,
                // 1# add a field for the result and to check if the value was set
                var declaringType = (TypeDefinition)propertyDefinition.ContainingTypeDefinition;
                var resultFieldDefinition = new FieldDefinition
                {
                    Type = propertyDefinition.Type,
                    Visibility = TypeMemberVisibility.Private,
                    Name = this.Host.NameTable.GetNameFor(propertyDefinition.Name + "$Value")
                };
                resultFieldDefinition.Attributes.Add(this.CompilerGeneratedAttribute);
                declaringType.Fields.Add(resultFieldDefinition);
                var resultInitializedFieldDefinition = new FieldDefinition
                {
                    Type = booleanType,
                    Visibility = TypeMemberVisibility.Private,
                    Name = this.Host.NameTable.GetNameFor(propertyDefinition.Name + "$Init")
                };
                resultInitializedFieldDefinition.Attributes.Add(this.CompilerGeneratedAttribute);
                declaringType.Fields.Add(resultInitializedFieldDefinition);

                // 2# generate a new method for the getter and move the old getter
                // somewhere else
                var uncachedGetter = new MethodDefinition();
                uncachedGetter.Copy(getter, this.Host.InternFactory);
                uncachedGetter.Name = this.Host.NameTable.GetNameFor("Uncached" + getter.Name);
                uncachedGetter.Locations.AddRange(getter.Locations);
                uncachedGetter.Visibility = TypeMemberVisibility.Private;

                // replace getter body
                var body = new SourceMethodBody(this.Host, this.sourceLocationProvider, this.contractProvider);
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

                return propertyDefinition;
            }

            /// <summary>
            /// A reference to the default constructor of the System.Runtime.CompilerServices.CompilerGenerated attribute.
            /// </summary>
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

            private static bool ContainsLazy(IEnumerable<ICustomAttribute> attributes)
            {
                Contract.Requires(attributes != null);
                foreach (var attribute in attributes)
                {
                    var type = attribute.Type as INamedEntity;
                    if (type != null &&
                        type.Name.Value == "LazyAttribute")
                        return true;
                }
                return false;
            }

        }
    }
}
