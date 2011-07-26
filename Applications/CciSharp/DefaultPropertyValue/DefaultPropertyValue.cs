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
using System.IO;
using System.Diagnostics.Contracts;
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableContracts;

namespace CciSharp.Mutators
{
    /// <summary>
    /// Assigns the value in [DefaultValue(...)] in all constructors for properties
    /// </summary>
    public sealed class DefaultPropertyValue
        : CcsMutatorBase
    {
        public DefaultPropertyValue(ICcsHost host)
            : base(host, "Default Property Value", 5, typeof(DefaultPropertyValueResources))
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

        class NetTypes : Microsoft.Cci.Immutable.PlatformType
        {
            public readonly INamespaceTypeReference DefaultAttribute;
            public NetTypes(IMetadataHost host)
                : base(host)
            {
                var systemAssembly = new Microsoft.Cci.Immutable.AssemblyReference(host, new AssemblyIdentity(
                    host.NameTable.System,
                    this.CoreAssemblyRef.Culture,
                    this.CoreAssemblyRef.Version,
                    this.CoreAssemblyRef.PublicKeyToken,
                    null));
                this.DefaultAttribute = this.CreateReference(systemAssembly, "System", "ComponentModel", "DefaultAttribute");
            }
        }

        private void Error(IPropertyDefinition propertyDefinition, string message)
        {
            this.Host.Event(CcsEventLevel.Error, "{0} {1}", propertyDefinition, message);
        }

        class Mutator
            : CcsCodeMutatorBase<DefaultPropertyValue>
        {
            readonly ITypeReference defaultAttribute;
            readonly ITypeReference voidType;

            public Mutator(
                DefaultPropertyValue owner, 
                ISourceLocationProvider sourceLocationProvider,
                ContractProvider contracts)
                : base(owner, sourceLocationProvider, contracts)
            {
                var types = new NetTypes(host);
                this.defaultAttribute = types.DefaultAttribute;
                this.voidType = types.SystemVoid;
            }

            public int MutationCount { get; private set; }

            public override NamespaceTypeDefinition Mutate(NamespaceTypeDefinition namespaceTypeDefinition)
            {
                this.AddCodeToSetDefaultValues(namespaceTypeDefinition);
                return base.Mutate(namespaceTypeDefinition);
            }

            public override NestedTypeDefinition Mutate(NestedTypeDefinition nestedTypeDefinition)
            {
                this.AddCodeToSetDefaultValues(nestedTypeDefinition);
                return base.Mutate(nestedTypeDefinition);
            }

            private void AddCodeToSetDefaultValues(NamedTypeDefinition typeDefinition)
            {
                // find the properties that need a default value
                var properties = new List<KeyValuePair<IPropertyDefinition, IMetadataConstant>>();
                foreach (var property in typeDefinition.Properties)
                {
                    IMetadataConstant value;
                    if (this.TryGetDefaultValue(property, out value))
                        properties.Add(new KeyValuePair<IPropertyDefinition, IMetadataConstant>(property, value));
                }

                if (properties.Count > 0)
                {
                    this.MutationCount += properties.Count;
                    foreach (MethodDefinition method in typeDefinition.Methods)
                    {
                        if (method.IsConstructor && !method.IsStatic)
                            this.SetDefaultValue(method, properties);
                    }
                }
            }

            private bool TryGetDefaultValue(IPropertyDefinition propertyDefinition, out IMetadataConstant value)
            {
                Contract.Requires(propertyDefinition != null);

                value = null;
                var getter = propertyDefinition.Getter as IMethodDefinition;
                var setter = propertyDefinition.Setter as IMethodDefinition;
                ICustomAttribute attribute;
                if (CcsHelper.TryGetAttributeByName(propertyDefinition.Attributes, "DefaultValueAttribute", out attribute)) // [DefaultValue(...)]
                {
                    if (getter == null)
                    {
                        this.Owner.Error(propertyDefinition, "must have a getter to be readonly");
                        return false;
                    }
                    if (setter == null)
                    {
                        this.Owner.Error(propertyDefinition, "must have a setter to be readonly");
                        return false;
                    }
                    if (getter.IsStatic || setter.IsStatic)
                    {
                        this.Owner.Error(propertyDefinition, "must be an instance property to be readonly");
                        return false;
                    }

                    if (getter.IsVirtual || setter.IsVirtual)
                    {
                        this.Owner.Error(propertyDefinition, "cannot be virtual to be readonly");
                        return false;
                    }
                    if (getter.ParameterCount > 0)
                    {
                        this.Owner.Error(propertyDefinition, "must not be an indexer");
                        return false;
                    }

                    IMetadataExpression valueExpression = new List<IMetadataExpression>(attribute.Arguments)[0];
                    value = valueExpression as IMetadataConstant;
                    // good candidate
                    return value != null;
                }

                return false;
            }

            private void SetDefaultValue(
                MethodDefinition ctor,
                IEnumerable<KeyValuePair<IPropertyDefinition, IMetadataConstant>> properties)
            {
                Contract.Requires(ctor != null);
                Contract.Requires(properties != null);

                var body = (SourceMethodBody)ctor.Body;
                var block = (BlockStatement)body.Block;
                Contract.Assert(block.Statements.Count > 0);
                // the first satement should be a call to the base constructor, 
                foreach (var kv in properties)
                {
                    var property = kv.Key;
                    var value = kv.Value;
                    var type = property.Type;
                    var setvalue = new MethodCall
                    {
                        Type = this.voidType,
                        ThisArgument = new ThisReference(),
                        IsStaticCall = false,
                        IsVirtualCall = false,
                        MethodToCall = property.Setter
                    };
                    setvalue.Arguments.Add(new CompileTimeConstant { Type = value.Type, Value = value.Value });
                    block.Statements.Insert(1, new ExpressionStatement { Expression = setvalue });
                }
            }
        }

    }
}
