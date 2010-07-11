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
            public readonly ITypeReference DependencyObjectType;
            public readonly ITypeReference DependencyPropertyType;
            public readonly ITypeReference PropertyMetadataType;
            public readonly ITypeReference ValidateValueCallbackType;
            public WindowsBaseTypes(IMetadataHost host, IAssemblyReference windowsBaseAssembly)
                :base(host)
            {
                Contract.Requires(windowsBaseAssembly != null);

                this.DependencyObjectType =
                    this.CreateReference(windowsBaseAssembly, "System", "Windows", "DependencyObject");
                this.DependencyPropertyType =
                    this.CreateReference(windowsBaseAssembly, "System", "Windows", "DependencyProperty");
                this.PropertyMetadataType =
                    this.CreateReference(windowsBaseAssembly, "System", "Windows", "PropertyMetadata");
                this.ValidateValueCallbackType =
                    this.CreateReference(windowsBaseAssembly, "System", "Windows", "ValidateValueCallback");
                var systemAssembly = new Microsoft.Cci.AssemblyReference(host, new AssemblyIdentity(
                    host.NameTable.System,
                    this.CoreAssemblyRef.Culture,
                    this.CoreAssemblyRef.Version,
                    this.CoreAssemblyRef.PublicKeyToken,
                    null));
            }
        }

        public override bool Visit()
        {
            var assembly = this.Host.MutatedAssembly;
            IAssemblyReference windowsBaseReference;
            if (!this.TryGetWindowsBase(assembly.AssemblyReferences, out windowsBaseReference))
                return false;

            PdbReader _pdbReader;
            if (!this.Host.TryGetMutatedPdbReader(out _pdbReader))
                _pdbReader = null;
            var contracts = this.Host.MutatedContracts;
            var mutator = new Mutator(this, _pdbReader, contracts, windowsBaseReference);
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
            readonly ITypeReference booleanType;
            readonly ITypeReference dependencyObjectType;
            readonly ITypeReference dependencyPropertyType;
            readonly ITypeReference propertyMetadataType;
            readonly ITypeReference validateValueCallbackType;
            readonly IMethodReference compilerGeneratedAttributeCtor;
            readonly IMethodReference propertyMetadataCtorObject;
            readonly IMethodReference dependencyPropertyRegisterStringTypeTypeMethod;
            readonly IMethodReference dependencyPropertyRegisterStringTypeTypePropertyMetadataMethod;
            readonly IMethodReference dependencyPropertyRegisterStringTypeTypePropertyMetadataValidateValueCallbackMethod;
            readonly IMethodReference dependencyObjectGetValueMethod;
            readonly IMethodReference dependencyObjectSetValueMethod;

            public Mutator(
                DependencyAutoProperty owner, 
                ISourceLocationProvider sourceLocationProvider, 
                ContractProvider contracts,
                IAssemblyReference windowsBaseReference)
                : base(owner, sourceLocationProvider, contracts)
            {
                var types = new WindowsBaseTypes(host, windowsBaseReference);
                this.dependencyObjectType = types.DependencyObjectType;
                this.dependencyPropertyType = types.DependencyPropertyType;
                this.propertyMetadataType = types.PropertyMetadataType;
                this.validateValueCallbackType = types.ValidateValueCallbackType;
                this.compilerGeneratedAttribute = host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute;
                this.voidType = host.PlatformType.SystemVoid;
                this.stringType = host.PlatformType.SystemString;
                this.objectType = host.PlatformType.SystemObject;
                this.typeType = host.PlatformType.SystemType;
                this.booleanType = host.PlatformType.SystemBoolean;

                var registerName = this.host.NameTable.GetNameFor("Register");
                var stringType = types.SystemString;
                var typeType = types.SystemType;
                this.propertyMetadataCtorObject =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.propertyMetadataType,
                        CallingConvention.HasThis,
                        this.voidType,
                        this.host.NameTable.Ctor,
                        0,
                        this.objectType);
                this.dependencyPropertyRegisterStringTypeTypeMethod =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.dependencyPropertyType,
                        CallingConvention.Default,
                        this.dependencyPropertyType,
                        registerName,
                        0,
                        stringType, typeType, typeType);
                this.dependencyPropertyRegisterStringTypeTypePropertyMetadataMethod =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.dependencyPropertyType,
                        CallingConvention.Default,
                        this.dependencyPropertyType,
                        registerName,
                        0,
                        stringType, typeType, typeType, this.propertyMetadataType);
                this.dependencyPropertyRegisterStringTypeTypePropertyMetadataValidateValueCallbackMethod =
                    new Microsoft.Cci.MethodReference(
                        this.host,
                        this.dependencyPropertyType,
                        CallingConvention.Default,
                        this.dependencyPropertyType,
                        registerName,
                        0,
                        stringType, typeType, typeType, 
                        this.propertyMetadataType, this.validateValueCallbackType);

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
                this.compilerGeneratedAttributeCtor = new Microsoft.Cci.MethodReference(
                    this.Host,
                    this.Host.PlatformType.SystemRuntimeCompilerServicesCompilerGeneratedAttribute,
                    CallingConvention.HasThis, 
                    this.Host.PlatformType.SystemVoid, 
                    this.Host.NameTable.Ctor, 
                    0);
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
                if (!CcsHelper.TryGetAttributeByName(propertyDefinition.Attributes, "DependencyAutoPropertyAttribute", out attribute))
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
                var declaringType = (TypeDefinition)propertyDefinition.ContainingTypeDefinition;
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
                    ContainingTypeDefinition = declaringType
                };
                declaringType.Fields.Add(propertyField);

                if (!this.TryRegisterProperty(declaringType, propertyDefinition, propertyField, attribute))
                    return propertyDefinition;

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

                getterBody = new SourceMethodBody(this.host, this.sourceLocationProvider)
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

                var setterBody = new SourceMethodBody(this.host, this.sourceLocationProvider)
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

            private bool TryRegisterProperty(
                TypeDefinition declaringType,
                PropertyDefinition propertyDefinition, 
                FieldDefinition propertyField,
                ICustomAttribute attribute)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(propertyDefinition != null);
                Contract.Requires(propertyField != null);
                Contract.Requires(attribute != null);

                // initialize with Register
                var cctorBody = GetOrCreateStaticCtorBody(declaringType);

                var register = new MethodCall
                {
                    IsStaticCall = true,
                    IsVirtualCall = false,
                    Type = this.dependencyPropertyType,                    
                    MethodToCall = this.dependencyPropertyRegisterStringTypeTypeMethod,
                };
                register.Arguments.Add(new CompileTimeConstant { Value = propertyDefinition.Name.Value, Type = this.stringType });
                register.Arguments.Add(new TypeOf { Type = this.typeType, TypeToGet = propertyDefinition.Type });
                register.Arguments.Add(new TypeOf { Type = this.typeType, TypeToGet = declaringType });
                if (attribute.NumberOfNamedArguments > 0)
                {
                    // get the default value.
                    IExpression defaultValue;
                    if (!this.TryGetDefaultValue(propertyDefinition, attribute, out defaultValue))
                        return false;
                    var newPropertyMetadata = new CreateObjectInstance
                    {
                        MethodToCall = this.propertyMetadataCtorObject,
                        Type = this.propertyMetadataType
                    };
                    newPropertyMetadata.Arguments.Add(defaultValue);
                    register.Arguments.Add(newPropertyMetadata);
                    register.MethodToCall = this.dependencyPropertyRegisterStringTypeTypePropertyMetadataMethod;

                    if (this.RequiresValidation(attribute))
                    {
                        // let's look for a method to validate the property value
                        IMethodReference validateCallback;
                        if (!this.TryCreateValidationMethod(declaringType, propertyDefinition, out validateCallback))
                            return false;
                        register.MethodToCall = this.dependencyPropertyRegisterStringTypeTypePropertyMetadataValidateValueCallbackMethod;
                        register.Arguments.Add(new CreateDelegateInstance
                        {
                             Type = this.validateValueCallbackType, 
                             MethodToCallViaDelegate = validateCallback
                        });
                    }
                }

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

                return true;
            }

            private bool TryCreateValidationMethod(
                TypeDefinition declaringType, 
                PropertyDefinition propertyDefinition,
                out IMethodReference validateCallback)
            {
                Contract.Requires(declaringType != null); 
                Contract.Requires(propertyDefinition != null);

                MethodDefinition validationMethod;
                if (this.TryGetValidationMethod(declaringType, propertyDefinition, out validationMethod))
                {
                    // we need to generate a delegate that takes an object
                    var validator = new MethodDefinition
                    {
                        Type = this.booleanType,
                        Name = this.Host.NameTable.GetNameFor("Validate" + propertyDefinition.Name.Value + "$Proxy"),
                        IsStatic = true,
                        Visibility = TypeMemberVisibility.Private,
                        CallingConvention = CallingConvention.Default,
                        ContainingTypeDefinition = declaringType
                    };
                    validator.Parameters.Add(new ParameterDefinition
                    {
                        Index = 0,
                        Type = this.objectType, 
                        Name = this.Host.NameTable.value,
                    });
                    validator.Attributes.Add(new CustomAttribute { Constructor = this.compilerGeneratedAttributeCtor });
                    declaringType.Methods.Add(validator);
                    var block = new BlockStatement();
                    SourceMethodBody body;
                    validator.Body = body = new SourceMethodBody(this.Host, this.sourceLocationProvider)
                    {
                        MethodDefinition = validator,
                        Block = block,
                    };
                    var proxyCall = new MethodCall
                    {
                        MethodToCall = validationMethod,
                        Type = this.booleanType, 
                        IsStaticCall = true
                    };
                    proxyCall.Arguments.Add(new Conversion 
                    { 
                        TypeAfterConversion = propertyDefinition.Type,
                        Type = propertyDefinition.Type,
                        ValueToConvert = new BoundExpression 
                        {
                             Type = this.objectType,
                             Definition = validator.Parameters[0],
                        }
                    });
                    block.Statements.Add(new ReturnStatement
                    {
                        Expression = proxyCall
                    });


                    validateCallback = validator;
                    return true;
                }

                validateCallback = null;
                return false;
            }

            private bool TryGetValidationMethod(
                TypeDefinition declaringType, 
                PropertyDefinition propertyDefinition, 
                out MethodDefinition validationMethod)
            {
                Contract.Requires(declaringType != null);
                Contract.Requires(propertyDefinition != null);

                var name = this.Host.NameTable.GetNameFor("Validate" + propertyDefinition.Name.Value);
                foreach (MethodDefinition method in declaringType.Methods)
                {
                    if (method.IsStatic &&
                        method.Type.InternedKey == this.booleanType.InternedKey &&
                        method.ParameterCount == 1 &&
                        method.Parameters[0].Type.InternedKey == propertyDefinition.Type.InternedKey &&
                        method.Name.UniqueKey == name.UniqueKey)
                    {
                        validationMethod = method;
                        return true;
                    }
                }

                this.Owner.Error(propertyDefinition,
                    String.Format("requires the method static bool {0}({1} value)", name.Value, propertyDefinition.Type)
                    );
                validationMethod = null;
                return false;
            }

            private bool RequiresValidation(ICustomAttribute attribute)
            {
                Contract.Requires(attribute != null);
                CompileTimeConstant value;
                if (CcsHelper.TryGetNamedArgumentValue(attribute, "Validate", out value))
                {
                    if (value.Type.InternedKey != this.booleanType.InternedKey)
                    {
                        this.Host.Event(CcsEventLevel.Error, "DependencyAutoPropertyAttribute.Validate must be a boolean type");
                        return false;
                    }

                    var b = (bool)value.Value;
                    return b;
                }

                return false;
            }

            private bool TryGetDefaultValue(
                PropertyDefinition propertyDefinition,
                ICustomAttribute attribute,
                out IExpression defaultValue)
            {
                Contract.Requires(propertyDefinition != null);
                Contract.Requires(attribute != null);

                CompileTimeConstant constant;
                if (CcsHelper.TryGetNamedArgumentValue(attribute, "DefaultValue", out constant))
                {
                    // ensure type matches

                    if (constant.Type.InternedKey != propertyDefinition.Type.InternedKey)
                    {
                        this.Owner.Error(propertyDefinition, "has a default value that does not match its type");
                        defaultValue = null;
                        return false;
                    }
                    defaultValue = constant;
                }
                else
                {
                    defaultValue = new DefaultValue
                    {
                        DefaultValueType = propertyDefinition.Type,
                        Type = propertyDefinition.Type
                    };
                }
                // boxing
                if (defaultValue.Type.IsValueType)
                {
                    defaultValue = new Conversion
                    {
                        Type = this.objectType,
                        ValueToConvert = defaultValue
                    };
                }
                return true;
            }

            private BlockStatement GetOrCreateStaticCtorBody(TypeDefinition typeDefinition)
            {
                if (this._cctorBody == null)
                {
                    var cctor = this.GetOrCreateStaticCtor(typeDefinition);
                    SourceMethodBody body;
                    if (cctor.Body == Dummy.MethodBody)
                    {
                        cctor.Body = body = new SourceMethodBody(this.host, this.sourceLocationProvider);
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

                var body = new SourceMethodBody(this.host, this.sourceLocationProvider);
                var cctor = new Microsoft.Cci.MutableCodeModel.MethodDefinition
                {
                    IsExternal = false,
                    IsAbstract = false,
                    IsStatic = true,
                    IsSpecialName = true,
                    IsRuntimeSpecial = true,
                    Name = this.Host.NameTable.Cctor,
                    Body = body,
                    ContainingTypeDefinition = typeDefinition
                };
                body.MethodDefinition = cctor;
                typeDefinition.Methods.Add(cctor);
                return cctor;
            }
        }
    }
}
