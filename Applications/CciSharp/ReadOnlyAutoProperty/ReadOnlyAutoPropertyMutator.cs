using System;
using System.Collections.Generic;
using System.Text;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CciSharp.ReadOnlyAutoProperty
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
            : base(host, "ReadOnly Auto Property", 1)
        { }

        public override bool Visit(Module module)
        {
            // pass1: collect properties to mutate and field references,
            var collector = new PropertyCollector(this.Host);
            collector.Visit(module);
            var properties = collector.Properties;

            // pass2: mutate properties and update field references

            return false;
        }

        class PropertyCollector
            : BaseMetadataTraverser
        {
            ICcsHost host;
            readonly ITypeReference compilerGeneratedAttribute;

            public PropertyCollector(ICcsHost host)
            {
                Contract.Requires(host != null);
                this.host = host;
                this.compilerGeneratedAttribute = host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute;
            }
            public readonly List<IPropertyDefinition> Properties = new List<IPropertyDefinition>();

            private void Error(IPropertyDefinition propertyDefinition, string message)
            {
                this.host.Event(CcsEventLevel.Error, "{0} {1}", propertyDefinition, message);
            }

            public override void Visit(IPropertyDefinition propertyDefinition)
            {
                var getter = propertyDefinition.Getter as IMethodDefinition;
                var setter = propertyDefinition.Setter as IMethodDefinition;
                if (ContainsReadOnly(propertyDefinition.Attributes)) // [ReadOnly]
                {
                    if (getter == null)
                    {
                        this.Error(propertyDefinition, "must have a getter to be readonly");
                        return;
                    }
                    if (setter == null)
                    {
                        this.Error(propertyDefinition, "must have a setter to be readonly");
                        return;
                    }
                    if (!AttributeHelper.Contains(getter.Attributes, this.compilerGeneratedAttribute) ||
                        !AttributeHelper.Contains(setter.Attributes, this.compilerGeneratedAttribute)) // compiler generated
                    {
                        this.Error(propertyDefinition, "must be an auto-property to be readonly");
                        return;
                    }
                    if (getter.IsStatic || setter.IsStatic)
                    {
                        this.Error(propertyDefinition, "must be an instance property to be readonly");
                        return;
                    }

                    if (getter.IsVirtual || setter.IsVirtual)
                    {
                        this.Error(propertyDefinition, "cannot be virtual to be readonly");
                        return;
                    }
                    if (setter.Visibility != TypeMemberVisibility.Private) // setter is private
                    {
                        this.Error(propertyDefinition, "must have a private setter to be readonly");
                        return;
                    }
                    this.Properties.Add(propertyDefinition);
                }
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

            // short cuts
            public override void Visit(IEnumerable<IMethodDefinition> methods)
            {}
            public override void Visit(IEnumerable<IEventDefinition> events)
            {}
            public override void Visit(IEnumerable<IFieldDefinition> fields)
            {}
            public override void Visit(IEnumerable<ICustomAttribute> customAttributes)
            {}
        }

        class Mutator
            : CcsCodeMutatorBase<ReadOnlyAutoPropertyMutator>
        {
            public Mutator(ReadOnlyAutoPropertyMutator owner)
                : base(owner)
            { }

            public override MethodDefinition Visit(MethodDefinition methodDefinition)
            {
                return base.Visit(methodDefinition);
            }
        }
    }
}
