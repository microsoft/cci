using System;
using System.Collections.Generic;
using CciSharp.Framework;
using Microsoft.Cci.MutableCodeModel;
using Microsoft.Cci;
using System.Diagnostics.Contracts;

namespace CciSharp.Mutators
{
    /// <summary>
    /// A mutator that instruments an autoproperty to implement the DependencyProperty pattern.
    /// </summary>
    public sealed class DependencyAutoProperty
        : CcsMutatorBase
    {
        public DependencyAutoProperty(ICcsHost host)
            :base(host, "Dependency Auto Property", 20, typeof(DependencyAutoPropertyResources))
        {}

        class WindowsBaseTypes : PlatformType
        {
            public readonly INamespaceTypeReference DependencyObjectType;
            public readonly INamespaceTypeReference DependencyPropertyType;
            public WindowsBaseTypes(IMetadataHost host, IAssemblyReference windowsBaseAssembly)
                :base(host)
            {
                Contract.Requires(windowsBaseAssembly != null);

                this.DependencyObjectType =
                    this.CreateReference(windowsBaseAssembly, "System", "Windows", "DependencyObject");
                this.DependencyPropertyType =
                    this.CreateReference(windowsBaseAssembly, "System", "Windows", "DependencyProperty");
            }
        }

        public override bool Visit(Assembly assembly, PdbReader pdbReader)
        {
            IAssemblyReference windowsBaseReference;
            if (!this.TryGetWindowsBase(assembly.AssemblyReferences, out windowsBaseReference))
                return false;

            var mutator = new Mutator(this, pdbReader, windowsBaseReference);
            mutator.Visit(assembly);
            return mutator.MutationCount > 0;
        }

        private bool TryGetWindowsBase(IEnumerable<IAssemblyReference> references, out IAssemblyReference reference)
        {
            Contract.Requires(references != null);

            foreach (var r in references)
            {
                if (r.Name.Value == "WindowsBase")
                {
                    reference = r;
                    return true;
                }
            }

            reference = null;
            return false;
        }

        private void Error(PropertyDefinition propertyDefinition, string message)
        {
            this.Host.Event(CcsEventLevel.Error, "{0} {1}", propertyDefinition, message);
        }

        class Mutator
            : CcsCodeMutatorBase<DependencyAutoProperty>
        {
            readonly ITypeReference compilerGeneratedAttribute;
            readonly ITypeReference voidType;
            readonly ITypeReference stringType;
            readonly INamespaceTypeReference dependencyObjectType;
            readonly INamespaceTypeReference dependencyPropertyType;
            readonly IMethodReference dependencyPropertyRegisterStringTypeTypeMethod;
            //readonly IMethodReference dependencyPropertyRegisterStringTypeTypePropertyMetadataMethod;

            public Mutator(DependencyAutoProperty owner, ISourceLocationProvider sourceLocationProvider, IAssemblyReference windowsBaseReference)
                : base(owner, sourceLocationProvider)
            {
                var types = new WindowsBaseTypes(host, windowsBaseReference);
                this.dependencyObjectType = types.DependencyObjectType;
                this.dependencyPropertyType = types.DependencyPropertyType;
                this.compilerGeneratedAttribute = host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute;
                this.voidType = host.PlatformType.SystemVoid;
                this.stringType = host.PlatformType.SystemString;

                var registerName = this.host.NameTable.GetNameFor("Register");
                var stringType = types.SystemString;
                var typeType = types.SystemType;
                this.dependencyPropertyRegisterStringTypeTypeMethod =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.dependencyPropertyType,
                        CallingConvention.Default,
                        this.dependencyPropertyType,
                        registerName,
                        0,
                        stringType, typeType, typeType);
            }

            public int MutationCount { get; private set; }

            BlockStatement _cctorBody;
            protected override void Visit(TypeDefinition typeDefinition)
            {
                this._cctorBody = null;
                // mutate properties
                foreach (PropertyDefinition property in typeDefinition.Properties)
                    this.Mutate(property);
                this._cctorBody = null;

                // then visit
                base.Visit(typeDefinition);
            }

            private void Mutate(PropertyDefinition propertyDefinition)
            {
                var getter = propertyDefinition.Getter as MethodDefinition;
                var setter = propertyDefinition.Setter as MethodDefinition;
                ICustomAttribute attribute;
                if (!CcsHelper.TryGetAttributeByName(propertyDefinition.Attributes, "DependencyPropertyAttribute", out attribute))
                    return;

                if (setter == null)
                {
                    this.Owner.Error(propertyDefinition, "must have a setter to implement DependencyProperty");
                    return;
                }
                if (getter == null)
                {
                    this.Owner.Error(propertyDefinition, "must have a getter to implement DependencyProperty");
                    return;
                }
                if (!AttributeHelper.Contains(getter.Attributes, this.compilerGeneratedAttribute) ||
                    !AttributeHelper.Contains(setter.Attributes, this.compilerGeneratedAttribute)) 
                    // compiler generated
                {
                    this.Owner.Error(propertyDefinition, "must be an auto-property to be readonly");
                    return;
                }
                if (getter.IsStatic || setter.IsStatic)
                {
                    this.Owner.Error(propertyDefinition, "must be an instance to implement DependencyProperty");
                    return;
                }
                if (getter.IsVirtual || setter.IsVirtual)
                {
                    this.Owner.Error(propertyDefinition, "cannot be virtual to implement DependencyProperty");
                    return;
                }
                if (getter.ParameterCount > 0)
                {
                    this.Owner.Error(propertyDefinition, "must not be an indexer to implement DependencyProperty");
                    return;
                }

                // we're good, we can start implement the property
                var declaringType = (TypeDefinition)propertyDefinition.ContainingType;
                if (!TypeHelper.Type1DerivesFromType2(declaringType, this.dependencyObjectType))
                {
                    this.Owner.Error(propertyDefinition, "must be declared in a type inheriting from System.Windows.DependencyObject");
                    return;
                }

                // create the new field
                var propertyField = new FieldDefinition
                {
                    Type = this.dependencyPropertyType,
                    Name = this.Host.NameTable.GetNameFor(propertyDefinition.Name.Value + "Property"),
                    IsStatic = true,
                    Visibility = TypeMemberVisibility.Public, 
                    IsReadOnly = true
                };
                declaringType.Fields.Add(propertyField);

                // initialize with Register
                var cctorBody = GetOrCreateStaticCtorBody(declaringType);
                var register = new MethodCall
                {
                    MethodToCall = this.dependencyPropertyRegisterStringTypeTypeMethod,
                    IsStaticCall = true, 
                    IsVirtualCall = false, 
                    Type = this.dependencyPropertyType
                };
                register.Arguments.Add(new CompileTimeConstant { Value = propertyField.Name.Value, Type = this.stringType });
                register.Arguments.Add(new TypeOf { Type = propertyDefinition.Type });
                register.Arguments.Add(new TypeOf { Type = declaringType });
                cctorBody.Statements.Insert(0,
                    new ExpressionStatement
                    {
                        Expression = new Assignment
                        {
                            Type = voidType,
                            Source = register,
                            Target = new TargetExpression { Definition = propertyField }
                        }
                    });

                // replace method bodies with SetValue, GetValue
            }

            private BlockStatement GetOrCreateStaticCtorBody(TypeDefinition typeDefinition)
            {
                if (this._cctorBody == null)
                {
                    var cctor = this.GetOrCreateStaticCtor(typeDefinition);
                    if (cctor.Body == Dummy.MethodBody)
                        cctor.Body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
                    var body = (SourceMethodBody)cctor.Body;
                    var block = body.Block as BlockStatement;
                    if (block == null)
                    {
                        body.Block = block = new BlockStatement();
                        block.Statements.Add(new ReturnStatement());
                    }
                    this._cctorBody = block;
                }
                return this._cctorBody;
            }

            private MethodDefinition GetOrCreateStaticCtor(TypeDefinition typeDefinition)
            {
                Contract.Requires(typeDefinition != null);

                foreach (MethodDefinition method in typeDefinition.Methods)
                    if (method.IsStaticConstructor)
                        return method;

                var cctor = new Microsoft.Cci.MutableCodeModel.MethodDefinition
                {
                      IsStatic = true,
                      IsSpecialName = true, 
                      IsRuntimeSpecial = true,
                      Name = this.Host.NameTable.Cctor, 
                      Body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider)
                };
                typeDefinition.Methods.Add(cctor);
                return cctor;
            }
        }
    }
}
