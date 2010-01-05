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

namespace CciSharp.Mutators
{
    /// <summary>
    /// Turns an auto property annotated with a [ReadOnly] attribute
    /// and whose setter is private, into a property with a getter
    /// and a readonly backing field.
    /// </summary>
    /// <remarks>
    /// The [ReadOnly] attribute namespace and assembly does not matter.
    /// </remarks>
    public sealed class ReadOnlyAutoPropertyMutator
        : CcsMutatorBase
    {
        public ReadOnlyAutoPropertyMutator(ICcsHost host)
            : base(host, "ReadOnly Auto Property", 10, typeof(ReadOnlyAutoPropertyResources))
        { }

        public override bool Visit()
        {
            var assembly = this.Host.MutatedAssembly;
            PdbReader _pdbReader;
            if (!this.Host.TryGetMutatedPdbReader(out _pdbReader))
                _pdbReader = null; 
            var contracts = this.Host.MutatedContracts;

            // pass1: collect properties to mutate and field references,
            var collector = new PropertyCollector(this, _pdbReader, contracts);
            collector.Visit(assembly);
            var properties = collector.Properties;
            // nothing to do...
            if (properties.Count == 0)
                return false;

            // pass2: mutate properties and update field references
            var mutator = new SetterReplacer(this, _pdbReader, contracts, properties);
            mutator.Visit(assembly);
            return true;
        }

        private void Error(PropertyDefinition propertyDefinition, string message)
        {
            this.Host.Event(CcsEventLevel.Error, "{0} {1}", propertyDefinition, message);
        }

        class PropertyCollector
            : CcsCodeMutatorBase<ReadOnlyAutoPropertyMutator>
        {
            readonly ITypeReference compilerGeneratedAttribute;

            public PropertyCollector(
                ReadOnlyAutoPropertyMutator owner, 
                ISourceLocationProvider sourceLocationProvider,
                ContractProvider contracts)
                : base(owner, sourceLocationProvider, contracts)
            {
                this.compilerGeneratedAttribute = host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute;
            }
            public readonly Dictionary<uint, Setter> Properties 
                = new Dictionary<uint, Setter>();

            public override PropertyDefinition Visit(PropertyDefinition propertyDefinition)
            {
                var getter = propertyDefinition.Getter as IMethodDefinition;
                var setter = propertyDefinition.Setter as IMethodDefinition;
                if (ContainsReadOnly(propertyDefinition.Attributes)) // [ReadOnly]
                {
                    if (getter == null)
                    {
                        this.Owner.Error(propertyDefinition, "must have a getter to be readonly");
                        return propertyDefinition;
                    }
                    if (setter == null)
                    {
                        this.Owner.Error(propertyDefinition, "must have a setter to be readonly");
                        return propertyDefinition;
                    }
                    if (!AttributeHelper.Contains(getter.Attributes, this.compilerGeneratedAttribute) ||
                        !AttributeHelper.Contains(setter.Attributes, this.compilerGeneratedAttribute)) // compiler generated
                    {
                        this.Owner.Error(propertyDefinition, "must be an auto-property to be readonly");
                        return propertyDefinition;
                    }
                    if (getter.IsStatic || setter.IsStatic)
                    {
                        this.Owner.Error(propertyDefinition, "must be an instance property to be readonly");
                        return propertyDefinition;
                    }

                    if (getter.IsVirtual || setter.IsVirtual)
                    {
                        this.Owner.Error(propertyDefinition, "cannot be virtual to be readonly");
                        return propertyDefinition;
                    }
                    if (setter.Visibility != TypeMemberVisibility.Private) // setter is private
                    {
                        this.Owner.Error(propertyDefinition, "must have a private setter to be readonly");
                        return propertyDefinition;
                    }
                    if (getter.ParameterCount > 0)
                    {
                        this.Owner.Error(propertyDefinition, "must not be an indexer");
                        return propertyDefinition;
                    }

                    // decompile setter body and get the backing field.
                    IFieldReference field;
                    if (!CcsHelper.TryGetFirstFieldReference(setter.Body, out field))
                    {
                        this.Owner.Error(propertyDefinition, "has no backing field");
                        return propertyDefinition;
                    }

                    // remove setter, make field readonly
                    var fieldDefinition = (FieldDefinition)field.ResolvedField;
                    fieldDefinition.IsReadOnly = true;
                    propertyDefinition.Setter = null;
                    propertyDefinition.Accessors.Remove(setter);

                    // store field to update
                    this.Properties[setter.InternedKey] = new Setter(propertyDefinition, field);
                    this.Host.Event(CcsEventLevel.Message, "readonly property: {0}, field {1}", propertyDefinition, field);
                }

                return propertyDefinition;
            }

            private static bool ContainsReadOnly(IEnumerable<ICustomAttribute> attributes)
            {
                Contract.Requires(attributes != null);
                foreach (var attribute in attributes)
                {
                    var type = attribute.Type as INamedEntity;
                    if (type != null &&
                        type.Name.Value == "ReadOnlyAttribute")
                        return true;
                }
                return false;
            }
        }

        struct Setter
        {
            public readonly PropertyDefinition Property;
            public readonly IFieldReference Field;
            public Setter(PropertyDefinition property, IFieldReference field)
            {
                Contract.Requires(property != null);
                Contract.Requires(field != null);
                this.Property = property;
                this.Field = field;
            }
        }

        class SetterReplacer
            : CcsCodeMutatorBase<ReadOnlyAutoPropertyMutator>
        {
            readonly Dictionary<uint, Setter> fields;
            public SetterReplacer(
                ReadOnlyAutoPropertyMutator owner, 
                ISourceLocationProvider _pdbReader,
                ContractProvider contracts,
                Dictionary<uint, Setter> fields)
                : base(owner, _pdbReader, contracts)
            {
                Contract.Requires(fields != null);
                this.fields = fields;
            }

            MethodDefinition currentMethod = null;
            public override MethodDefinition Visit(MethodDefinition methodDefinition)
            {
                currentMethod = methodDefinition;
                var result = base.Visit(methodDefinition);
                currentMethod = null;
                return result;
            }

            public override IExpression Visit(MethodCall methodCall)
            {
                Setter setter;
                var methodToCall = methodCall.MethodToCall;
                if (this.fields.TryGetValue(methodToCall.InternedKey, out setter))
                {
                    var field = setter.Field;
                    var property = setter.Property;
                    // are we in a .ctor?
                    if (!currentMethod.IsConstructor)
                    {
                        this.Owner.Error(property, "can only be assigned in a constructor");
                        return methodCall;
                    }

                    var storeField = new Assignment
                    {
                        Source = methodCall.Arguments[0],
                        Target = new TargetExpression
                        {
                             Instance = methodCall.ThisArgument,
                             Definition = field,
                             Locations = methodCall.Locations
                        },
                        Locations = methodCall.Locations
                    };
                    return storeField;
                }
                return methodCall;
            }

            protected override void Visit(TypeDefinition typeDefinition)
            {
                var methods = new List<IMethodDefinition>();
                foreach (var method in typeDefinition.Methods)
                {
                    if (this.fields.ContainsKey(method.InternedKey))
                        continue;
                    methods.Add(method);
                }
                typeDefinition.Methods = methods;
                base.Visit(typeDefinition);
            }
        }
    }
}
