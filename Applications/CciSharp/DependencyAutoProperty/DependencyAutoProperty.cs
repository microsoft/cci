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
            readonly ITypeReference objectType;
            readonly ITypeReference typeType;
            readonly INamespaceTypeReference dependencyObjectType;
            readonly INamespaceTypeReference dependencyPropertyType;
            readonly IMethodReference dependencyPropertyRegisterStringTypeTypeMethod;
            readonly IMethodReference dependencyObjectGetValueMethod;
            readonly IMethodReference dependencyObjectSetValueMethod;
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
                this.objectType = host.PlatformType.SystemObject;
                this.typeType = host.PlatformType.SystemType;

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

                this.dependencyObjectGetValueMethod =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.dependencyObjectType,
                        CallingConvention.HasThis,
                        this.objectType,
                        this.Host.NameTable.GetNameFor("GetValue"),
                        0,
                        this.dependencyPropertyType);
                this.dependencyObjectSetValueMethod =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.dependencyObjectType,
                        CallingConvention.HasThis,
                        this.voidType,
                        this.Host.NameTable.GetNameFor("SetValue"),
                        0,
                        this.dependencyPropertyType,
                        this.objectType
                        );

            }

            public int MutationCount { get; private set; }

            BlockStatement _cctorBody;
            protected override void Visit(TypeDefinition typeDefinition)
            {
                // collect candidates
                this._cctorBody = null;
                base.Visit(typeDefinition);
                if (this._cctorBody != null)
                    this._cctorBody = null;
            }

            public override PropertyDefinition Visit(PropertyDefinition propertyDefinition)
            {
                var getter = propertyDefinition.Getter as MethodDefinition;
                var setter = propertyDefinition.Setter as MethodDefinition;
                ICustomAttribute attribute;
                if (!CcsHelper.TryGetAttributeByName(propertyDefinition.Attributes, "DependencyPropertyAttribute", out attribute))
                    return propertyDefinition;

                if (setter == null)
                {
                    this.Owner.Error(propertyDefinition, "must have a setter to implement DependencyProperty");
                    return propertyDefinition;
                }
                if (getter == null)
                {
                    this.Owner.Error(propertyDefinition, "must have a getter to implement DependencyProperty");
                    return propertyDefinition;
                }
                if (!AttributeHelper.Contains(getter.Attributes, this.compilerGeneratedAttribute) ||
                    !AttributeHelper.Contains(setter.Attributes, this.compilerGeneratedAttribute))
                // compiler generated
                {
                    this.Owner.Error(propertyDefinition, "must be an auto-property to be readonly");
                    return propertyDefinition;
                }
                if (getter.IsStatic || setter.IsStatic)
                {
                    this.Owner.Error(propertyDefinition, "must be an instance to implement DependencyProperty");
                    return propertyDefinition;
                }
                if (getter.IsVirtual || setter.IsVirtual)
                {
                    this.Owner.Error(propertyDefinition, "cannot be virtual to implement DependencyProperty");
                    return propertyDefinition;
                }
                if (getter.ParameterCount > 0)
                {
                    this.Owner.Error(propertyDefinition, "must not be an indexer to implement DependencyProperty");
                    return propertyDefinition;
                }

                // we're good, we can start implement the property
                var declaringType = (TypeDefinition)propertyDefinition.ContainingType;
                if (!TypeHelper.Type1DerivesFromType2(declaringType, this.dependencyObjectType))
                {
                    this.Owner.Error(propertyDefinition, "must be declared in a type inheriting from System.Windows.DependencyObject");
                    return propertyDefinition;
                }

                // create the new field
                var propertyField = new FieldDefinition
                {
                    Type = this.dependencyPropertyType,
                    Name = this.Host.NameTable.GetNameFor(propertyDefinition.Name.Value + "Property"),
                    IsStatic = true,
                    Visibility = TypeMemberVisibility.Public,
                    IsReadOnly = true,
                    ContainingType = declaringType
                };
                declaringType.Fields.Add(propertyField);

                this.RegisterProperty(declaringType, propertyDefinition, propertyField);

                // replace method bodies with SetValue, GetValue
                this.ReplaceGetter(declaringType, getter, propertyField);
                this.ReplaceSetter(declaringType, setter, propertyField);

                // clean up attribute
                propertyDefinition.Attributes.Remove(attribute);

                this.MutationCount++;
                return propertyDefinition;
            }

            private void ReplaceGetter(TypeDefinition declaringType, MethodDefinition getter, FieldDefinition propertyField)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(getter != null);
                Contract.Requires(propertyField != null);

                var getterBody = (SourceMethodBody)getter.Body;
                IFieldReference backingField;
                if (CcsHelper.TryGetFirstFieldReference(getterBody, out backingField))
                    declaringType.Fields.Remove((IFieldDefinition)backingField);

                getterBody = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider)
                {
                    MethodDefinition = getter,
                    LocalsAreZeroed = getter.Body.LocalsAreZeroed
                };
                getter.Body = getterBody;
                var getterBlock = new BlockStatement();
                getterBody.Block = getterBlock;
                var getValueCall = new MethodCall
                {
                    ThisArgument = new ThisReference(),
                    MethodToCall = this.dependencyObjectGetValueMethod,
                    Type = this.objectType, 
                    IsStaticCall = false, 
                    IsVirtualCall = false
                };
                getValueCall.Arguments.Add(new BoundExpression { Definition = propertyField });
                getterBlock.Statements.Add(
                    new ReturnStatement
                    {
                        Expression = new Conversion
                        {
                            Type = getter.Type,
                            ValueToConvert = getValueCall,
                            TypeAfterConversion = getter.Type
                        }
                    });
            }

            private void ReplaceSetter(TypeDefinition declaringType, MethodDefinition setter, FieldDefinition propertyField)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(setter != null);
                Contract.Requires(propertyField != null);

                var setterBody = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider)
                {
                    MethodDefinition = setter,
                    LocalsAreZeroed = setter.Body.LocalsAreZeroed
                };
                setter.Body = setterBody;
                var setterBlock = new BlockStatement();
                setterBody.Block = setterBlock;
                var setValueCall = new MethodCall
                {
                    ThisArgument = new ThisReference(),
                    MethodToCall = this.dependencyObjectSetValueMethod,
                    Type = this.voidType,
                    IsStaticCall = false,
                    IsVirtualCall = false
                };
                setValueCall.Arguments.Add(new BoundExpression { Definition = propertyField });
                setValueCall.Arguments.Add(new BoundExpression { Definition = setter.Parameters[0] });
                setterBlock.Statements.Add(
                    new ExpressionStatement
                    {
                        Expression = setValueCall
                    });
            }

            private void RegisterProperty(
                TypeDefinition declaringType,
                PropertyDefinition propertyDefinition, 
                FieldDefinition propertyField)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(propertyDefinition != null);
                Contract.Requires(propertyField != null);

                // initialize with Register
                var cctorBody = GetOrCreateStaticCtorBody(declaringType);
                var register = new MethodCall
                {
                    MethodToCall = this.dependencyPropertyRegisterStringTypeTypeMethod,
                    IsStaticCall = true,
                    IsVirtualCall = false,
                    Type = this.dependencyPropertyType
                };
                register.Arguments.Add(new CompileTimeConstant { Value = propertyDefinition.Name.Value, Type = this.stringType });
                register.Arguments.Add(new TypeOf { Type = this.typeType, TypeToGet = propertyDefinition.Type });
                register.Arguments.Add(new TypeOf { Type = this.typeType, TypeToGet = declaringType });
                cctorBody.Statements.Insert(0,
                    new ExpressionStatement
                    {
                        Expression = new Assignment
                        {
                            Type = propertyField.Type,
                            Source = register,
                            Target = new TargetExpression
                            {
                                Definition = propertyField,
                                Type = propertyField.Type
                            }
                        }
                    });

            }

            private BlockStatement GetOrCreateStaticCtorBody(TypeDefinition typeDefinition)
            {
                if (this._cctorBody == null)
                {
                    var cctor = this.GetOrCreateStaticCtor(typeDefinition);
                    SourceMethodBody body;
                    if (cctor.Body == Dummy.MethodBody)
                    {
                        cctor.Body = body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
                        body.MethodDefinition = cctor;
                    }
                    body = (SourceMethodBody)cctor.Body;
                    var block = body.Block as BlockStatement;
                    if (block == null)
                        body.Block = block = new BlockStatement();
                    this._cctorBody = block;
                }
                return this._cctorBody;
            }

            private MethodDefinition GetOrCreateStaticCtor(TypeDefinition typeDefinition)
            {
                Contract.Requires(typeDefinition != null);

                foreach (MethodDefinition method in typeDefinition.Methods)
                    if (method.IsStaticConstructor)
                    {
                        method.IsExternal = false;
                        return method;
                    }

                var body = new SourceMethodBody(this.host, this.sourceLocationProvider, this.contractProvider);
                var cctor = new Microsoft.Cci.MutableCodeModel.MethodDefinition
                {
                    IsExternal = false,
                    IsAbstract = false,
                    IsStatic = true,
                    IsSpecialName = true,
                    IsRuntimeSpecial = true,
                    Name = this.Host.NameTable.Cctor,
                    Body = body,
                    ContainingType = typeDefinition
                };
                body.MethodDefinition = cctor;
                typeDefinition.Methods.Add(cctor);
                return cctor;
            }
        }
    }
}
