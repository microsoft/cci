//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  public interface ICopyFrom<ImmutableObject> {
    void Copy(ImmutableObject objectToCopy, IInternFactory internFactory);
  }

  public class MetadataMutator  {

    public MetadataMutator(IMetadataHost host) {
      this.host = host;
    }

    public MetadataMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable) {
      this.host = host;
      this.copyOnlyIfNotAlreadyMutable = copyOnlyIfNotAlreadyMutable;
    }

    /// <summary>
    /// Duplicates are cached, both to save space and to detect when the traversal of a cycle should stop.
    /// </summary>
    protected Dictionary<object, object> cache = new Dictionary<object, object>();

    protected readonly bool copyOnlyIfNotAlreadyMutable;

    /// <summary>
    ///Since definitions are also references, it can happen that a definition is visited as both a definition and as a reference.
    ///If so, the cache may contain a duplicated definition when a reference is expected, or vice versa.
    ///To prevent this, reference duplicates are always cached separately.
    /// </summary>
    protected Dictionary<object, object> referenceCache = new Dictionary<object, object>();

    protected List<INamedTypeDefinition> flatListOfTypes = new List<INamedTypeDefinition>();

    protected IMetadataHost host;

    protected System.Collections.Stack path = new System.Collections.Stack();

    protected bool stopTraversal;

    public IMethodDefinition GetCurrentMethod() {
      foreach (object parent in this.path) {
        IMethodDefinition/*?*/ method = parent as IMethodDefinition;
        if (method != null) return method;
      }
      return Dummy.Method;
    }

    public IUnitNamespace GetCurrentNamespace() {
      foreach (object parent in this.path) {
        IUnitNamespace/*?*/ ns = parent as IUnitNamespace;
        if (ns != null) return ns;
      }
      return Dummy.RootUnitNamespace;
    }

    public ISignature GetCurrentSignature() {
      foreach (object parent in this.path) {
        ISignature/*?*/ signature = parent as ISignature;
        if (signature != null) return signature;
      }
      return Dummy.Method;
    }

    public ITypeDefinition GetCurrentType() {
      foreach (object parent in this.path) {
        ITypeDefinition/*?*/ type = parent as ITypeDefinition;
        if (type != null) return type;
      }
      return Dummy.Type;
    }

    public IUnit GetCurrentUnit() {
      foreach (object parent in this.path) {
        IUnit/*?*/ unit = parent as IUnit;
        if (unit != null) return unit;
      }
      return Dummy.Unit;
    }

    public MutableObject GetMutableCopy<MutableObject, ImmutableObject>(ImmutableObject objectToCopy)
      where MutableObject : class, ImmutableObject, ICopyFrom<ImmutableObject>, new() {
      MutableObject/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable){
        result = objectToCopy as MutableObject;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(objectToCopy, out cachedValue);
      result = cachedValue as MutableObject;
      if (result != null) return result;
      result = new MutableObject();
      this.cache.Add(objectToCopy, result);
      this.cache.Add(result, result);
      result.Copy(objectToCopy, this.host.InternFactory);
      return result;
    }

    public MutableObject GetMutableCopyOfReference<MutableObject, ImmutableObject>(ImmutableObject objectToCopy)
      where MutableObject : class, ImmutableObject, ICopyFrom<ImmutableObject>, new() {
      MutableObject/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = objectToCopy as MutableObject;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(objectToCopy, out cachedValue);
      result = cachedValue as MutableObject;
      if (result != null) return result;
      result = new MutableObject();
      if (objectToCopy is ISpecializedMethodReference && !(result is ISpecializedMethodReference)) {
      }
      this.referenceCache.Add(objectToCopy, result);
      this.referenceCache.Add(result, result);
      result.Copy(objectToCopy, this.host.InternFactory);
      return result;
    }

    public virtual Assembly GetMutableCopy(IAssembly assembly) {
      return this.GetMutableCopy<Assembly, IAssembly>(assembly);
    }

    public virtual AssemblyReference GetMutableCopy(IAssemblyReference assemblyReference) {
      return this.GetMutableCopyOfReference<AssemblyReference, IAssemblyReference>(assemblyReference);
    }

    public virtual CustomAttribute GetMutableCopy(ICustomAttribute customAttribute) {
      return this.GetMutableCopy<CustomAttribute, ICustomAttribute>(customAttribute);
    }

    public virtual CustomModifier GetMutableCopy(ICustomModifier customModifier) {
      return this.GetMutableCopy<CustomModifier, ICustomModifier>(customModifier);
    }

    public virtual EventDefinition GetMutableCopy(IEventDefinition eventDefinition) {
      return this.GetMutableCopy<EventDefinition, IEventDefinition>(eventDefinition);
    }

    public virtual FieldDefinition GetMutableCopy(IFieldDefinition fieldDefinition) {
      return this.GetMutableCopy<FieldDefinition, IFieldDefinition>(fieldDefinition);
    }

    public virtual FieldReference GetMutableCopy(IFieldReference fieldReference) {
      return this.GetMutableCopyOfReference<FieldReference, IFieldReference>(fieldReference);
    }

    public virtual FileReference GetMutableCopy(IFileReference fileReference) {
      return this.GetMutableCopyOfReference<FileReference, IFileReference>(fileReference);
    }

    public virtual FunctionPointerTypeReference GetMutableCopy(IFunctionPointerTypeReference functionPointerTypeReference) {
      return this.GetMutableCopyOfReference<FunctionPointerTypeReference, IFunctionPointerTypeReference>(functionPointerTypeReference);
    }

    public virtual GenericMethodInstanceReference GetMutableCopy(IGenericMethodInstanceReference genericMethodInstanceReference) {
      return this.GetMutableCopyOfReference<GenericMethodInstanceReference, IGenericMethodInstanceReference>(genericMethodInstanceReference);
    }

    public virtual GenericMethodParameter GetMutableCopy(IGenericMethodParameter genericMethodParameter) {
      return this.GetMutableCopy<GenericMethodParameter, IGenericMethodParameter>(genericMethodParameter);
    }

    public virtual GenericMethodParameterReference GetMutableCopy(IGenericMethodParameterReference genericMethodParameterReference) {
      return this.GetMutableCopyOfReference<GenericMethodParameterReference, IGenericMethodParameterReference>(genericMethodParameterReference);
    }

    public virtual GenericTypeInstanceReference GetMutableCopy(IGenericTypeInstanceReference genericTypeInstanceReference) {
      return this.GetMutableCopyOfReference<GenericTypeInstanceReference, IGenericTypeInstanceReference>(genericTypeInstanceReference);
    }

    public virtual GenericTypeParameter GetMutableCopy(IGenericTypeParameter genericTypeParameter) {
      return this.GetMutableCopy<GenericTypeParameter, IGenericTypeParameter>(genericTypeParameter);
    }

    public virtual GenericTypeParameterReference GetMutableCopy(IGenericTypeParameterReference genericTypeParameterReference) {
      return this.GetMutableCopyOfReference<GenericTypeParameterReference, IGenericTypeParameterReference>(genericTypeParameterReference);
    }

    public virtual GlobalFieldDefinition GetMutableCopy(IGlobalFieldDefinition globalFieldDefinition) {
      return this.GetMutableCopy<GlobalFieldDefinition, IGlobalFieldDefinition>(globalFieldDefinition);
    }

    public virtual GlobalMethodDefinition GetMutableCopy(IGlobalMethodDefinition globalMethodDefinition) {
      return this.GetMutableCopy<GlobalMethodDefinition, IGlobalMethodDefinition>(globalMethodDefinition);
    }

    public virtual LocalDefinition GetMutableCopy(ILocalDefinition localDefinition) {
      return this.GetMutableCopy<LocalDefinition, ILocalDefinition>(localDefinition);
    }

    public virtual MarshallingInformation GetMutableCopy(IMarshallingInformation marshallingInformation) {
      return this.GetMutableCopy<MarshallingInformation, IMarshallingInformation>(marshallingInformation);
    }

    public virtual MetadataConstant GetMutableCopy(IMetadataConstant metadataConstant) {
      return this.GetMutableCopy<MetadataConstant, IMetadataConstant>(metadataConstant);
    }

    public virtual MetadataCreateArray GetMutableCopy(IMetadataCreateArray metadataCreateArray) {
      return this.GetMutableCopy<MetadataCreateArray, IMetadataCreateArray>(metadataCreateArray);
    }

    public virtual MetadataNamedArgument GetMutableCopy(IMetadataNamedArgument metadataNamedArgument) {
      return this.GetMutableCopy<MetadataNamedArgument, IMetadataNamedArgument>(metadataNamedArgument);
    }

    public virtual MetadataTypeOf GetMutableCopy(IMetadataTypeOf metadataTypeOf) {
      return this.GetMutableCopy<MetadataTypeOf, IMetadataTypeOf>(metadataTypeOf);
    }

    public virtual MethodDefinition GetMutableCopy(IMethodDefinition methodDefinition) {
      return this.GetMutableCopy<MethodDefinition, IMethodDefinition>(methodDefinition);
    }

    public virtual MethodBody GetMutableCopy(IMethodBody methodBody) {
      return this.GetMutableCopy<MethodBody, IMethodBody>(methodBody);
    }

    public virtual MethodImplementation GetMutableCopy(IMethodImplementation methodImplementation) {
      return this.GetMutableCopy<MethodImplementation, IMethodImplementation>(methodImplementation);
    }

    public virtual MethodReference GetMutableCopy(IMethodReference methodReference) {
      return this.GetMutableCopyOfReference<MethodReference, IMethodReference>(methodReference);
    }

    public virtual ModifiedTypeReference GetMutableCopy(IModifiedTypeReference modifiedTypeReference) {
      return this.GetMutableCopyOfReference<ModifiedTypeReference, IModifiedTypeReference>(modifiedTypeReference);
    }

    public virtual Module GetMutableCopy(IModule module) {
      return this.GetMutableCopy<Module, IModule>(module);
    }

    public virtual ModuleReference GetMutableCopy(IModuleReference moduleReference) {
      return this.GetMutableCopyOfReference<ModuleReference, IModuleReference>(moduleReference);
    }

    public virtual NamespaceAliasForType GetMutableCopy(INamespaceAliasForType namespaceAliasForType) {
      return this.GetMutableCopy<NamespaceAliasForType, INamespaceAliasForType>(namespaceAliasForType);
    }

    public virtual NamespaceTypeDefinition GetMutableCopy(INamespaceTypeDefinition namespaceTypeDefinition) {
      return this.GetMutableCopy<NamespaceTypeDefinition, INamespaceTypeDefinition>(namespaceTypeDefinition);
    }

    public virtual NamespaceTypeReference GetMutableCopy(INamespaceTypeReference namespaceTypeReference) {
      return this.GetMutableCopyOfReference<NamespaceTypeReference, INamespaceTypeReference>(namespaceTypeReference);
    }

    public virtual NestedAliasForType GetMutableCopy(INestedAliasForType nestedAliasForType) {
      return this.GetMutableCopy<NestedAliasForType, INestedAliasForType>(nestedAliasForType);
    }

    public virtual NestedTypeDefinition GetMutableCopy(INestedTypeDefinition nestedTypeDefinition) {
      return this.GetMutableCopy<NestedTypeDefinition, INestedTypeDefinition>(nestedTypeDefinition);
    }

    public virtual NestedTypeReference GetMutableCopy(INestedTypeReference nestedTypeReference) {
      return this.GetMutableCopyOfReference<NestedTypeReference, INestedTypeReference>(nestedTypeReference);
    }

    public virtual NestedUnitNamespace GetMutableCopy(INestedUnitNamespace nestedUnitNamespace) {
      return this.GetMutableCopy<NestedUnitNamespace, INestedUnitNamespace>(nestedUnitNamespace);
    }

    public virtual NestedUnitNamespaceReference GetMutableCopy(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      return this.GetMutableCopyOfReference<NestedUnitNamespaceReference, INestedUnitNamespaceReference>(nestedUnitNamespaceReference);
    }

    public virtual Operation GetMutableCopy(IOperation operation) {
      return this.GetMutableCopy<Operation, IOperation>(operation);
    }

    public virtual OperationExceptionInformation GetMutableCopy(IOperationExceptionInformation operationExceptionInformation) {
      return this.GetMutableCopy<OperationExceptionInformation, IOperationExceptionInformation>(operationExceptionInformation);
    }

    public virtual ParameterDefinition GetMutableCopy(IParameterDefinition parameterDefinition) {
      return this.GetMutableCopy<ParameterDefinition, IParameterDefinition>(parameterDefinition);
    }

    public virtual ParameterTypeInformation GetMutableCopy(IParameterTypeInformation parameterTypeInformation) {
      return this.GetMutableCopyOfReference<ParameterTypeInformation, IParameterTypeInformation>(parameterTypeInformation);
    }

    public virtual PlatformInvokeInformation GetMutableCopy(IPlatformInvokeInformation platformInvokeInformation) {
      return this.GetMutableCopy<PlatformInvokeInformation, IPlatformInvokeInformation>(platformInvokeInformation);
    }

    public virtual PointerTypeReference GetMutableCopy(IPointerTypeReference pointerTypeReference) {
      return this.GetMutableCopyOfReference<PointerTypeReference, IPointerTypeReference>(pointerTypeReference);
    }

    public virtual PropertyDefinition GetMutableCopy(IPropertyDefinition propertyDefinition) {
      return this.GetMutableCopy<PropertyDefinition, IPropertyDefinition>(propertyDefinition);
    }

    public virtual ResourceReference GetMutableCopy(IResourceReference resourceReference) {
      return this.GetMutableCopy<ResourceReference, IResourceReference>(resourceReference);
    }

    public virtual RootUnitNamespace GetMutableCopy(IRootUnitNamespace rootUnitNamespace) {
      return this.GetMutableCopy<RootUnitNamespace, IRootUnitNamespace>(rootUnitNamespace);
    }

    public virtual RootUnitNamespaceReference GetMutableCopy(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      return this.GetMutableCopyOfReference<RootUnitNamespaceReference, IRootUnitNamespaceReference>(rootUnitNamespaceReference);
    }

    public virtual SecurityAttribute GetMutableCopy(ISecurityAttribute securityAttribute) {
      return this.GetMutableCopy<SecurityAttribute, ISecurityAttribute>(securityAttribute);
    }

    public virtual SpecializedFieldReference GetMutableCopy(ISpecializedFieldReference specializedFieldReference) {
      return this.GetMutableCopyOfReference<SpecializedFieldReference, ISpecializedFieldReference>(specializedFieldReference);
    }

    public virtual SpecializedMethodReference GetMutableCopy(ISpecializedMethodReference specializedMethodReference) {
      return this.GetMutableCopyOfReference<SpecializedMethodReference, ISpecializedMethodReference>(specializedMethodReference);
    }

    public virtual SpecializedNestedTypeReference GetMutableCopy (ISpecializedNestedTypeReference specializedNestedTypeReference) {
      return this.GetMutableCopyOfReference<SpecializedNestedTypeReference, ISpecializedNestedTypeReference>(specializedNestedTypeReference);
    }

    public virtual MatrixTypeReference GetMutableMatrixCopy(IArrayTypeReference matrixTypeReference) {
      return this.GetMutableCopyOfReference<MatrixTypeReference, IArrayTypeReference>(matrixTypeReference);
    }

    public virtual VectorTypeReference GetMutableVectorCopy(IArrayTypeReference vectorTypeReference) {
      return this.GetMutableCopyOfReference<VectorTypeReference, IArrayTypeReference>(vectorTypeReference);
    }

    public virtual IMethodReference GetTypeSpecificMutableCopy(IMethodReference methodReference) {
      ISpecializedMethodReference/*?*/ specializedMethodReference = methodReference as ISpecializedMethodReference;
      if (specializedMethodReference != null)
        return this.GetMutableCopy(specializedMethodReference);
      IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null)
        return this.GetMutableCopy(genericMethodInstanceReference);
      else {
        IGlobalMethodDefinition/*?*/ globalMethodDefinition = methodReference as IGlobalMethodDefinition;
        if (globalMethodDefinition != null)
          return this.GetMutableCopy(globalMethodDefinition);
        IMethodDefinition/*?*/ methodDefinition = methodReference as IMethodDefinition;
        if (methodDefinition != null) {
          return this.GetMutableCopy(methodDefinition);
        }
        return this.GetMutableCopy(methodReference);
      }
    }

    public virtual List<IAliasForType> Visit(List<IAliasForType> aliasesForTypes) {
      if (this.stopTraversal) return aliasesForTypes;
      for (int i = 0, n = aliasesForTypes.Count; i < n; i++)
        aliasesForTypes[i] = this.Visit(aliasesForTypes[i]);
      return aliasesForTypes;
    }

    public virtual IAliasForType Visit(IAliasForType aliasForType) {
      if (this.stopTraversal) return aliasForType;
      INamespaceAliasForType/*?*/ namespaceAliasForType = aliasForType as INamespaceAliasForType;
      if (namespaceAliasForType != null) return this.Visit(this.GetMutableCopy(namespaceAliasForType));
      INestedAliasForType/*?*/ nestedAliasForType = aliasForType as INestedAliasForType;
      if (nestedAliasForType != null) return this.Visit(this.GetMutableCopy(nestedAliasForType));
      //TODO: error
      return aliasForType;
    }

    public virtual Assembly Visit(Assembly assembly) {
      if (this.stopTraversal) return assembly;
      this.path.Push(assembly);
      assembly.AssemblyAttributes = this.Visit(assembly.AssemblyAttributes);
      assembly.ExportedTypes = this.Visit(assembly.ExportedTypes);
      assembly.Files = this.Visit(assembly.Files);
      assembly.MemberModules = this.Visit(assembly.MemberModules);
      assembly.Resources = this.Visit(assembly.Resources);
      assembly.SecurityAttributes = this.Visit(assembly.SecurityAttributes);
      this.path.Pop();
      this.Visit((Module)assembly);
      return assembly;
    }

    public virtual List<IAssemblyReference> Visit(List<IAssemblyReference> assemblyReferences) {
      if (this.stopTraversal) return assemblyReferences;
      for (int i = 0, n = assemblyReferences.Count; i < n; i++)
        assemblyReferences[i] = this.Visit(assemblyReferences[i]);
      return assemblyReferences;
    }

    public virtual IAssemblyReference Visit(IAssemblyReference assemblyReference) {
      return this.Visit(this.GetMutableCopy(assemblyReference));
    }

    public virtual AssemblyReference Visit(AssemblyReference assemblyReference) {
      if (assemblyReference.ResolvedAssembly != Dummy.Assembly) {
        object/*?*/ mutatedResolvedAssembly = null;
        if (this.cache.TryGetValue(assemblyReference.ResolvedAssembly, out mutatedResolvedAssembly))
          assemblyReference.ResolvedAssembly = (IAssembly)mutatedResolvedAssembly;
      }      
      return assemblyReference;
    }

    public virtual List<ICustomAttribute> Visit(List<ICustomAttribute> customAttributes) {
      if (this.stopTraversal) return customAttributes;
      for (int i = 0, n = customAttributes.Count; i < n; i++)
        customAttributes[i] = this.Visit(this.GetMutableCopy(customAttributes[i]));
      return customAttributes;
    }

    public virtual CustomAttribute Visit(CustomAttribute customAttribute) {
      if (this.stopTraversal) return customAttribute;
      this.path.Push(customAttribute);
      customAttribute.Arguments = this.Visit(customAttribute.Arguments);
      customAttribute.Constructor = this.Visit(customAttribute.Constructor);
      customAttribute.NamedArguments = this.Visit(customAttribute.NamedArguments);
      this.path.Pop();
      return customAttribute;
    }

    public virtual List<ICustomModifier> Visit(List<ICustomModifier> customModifiers) {
      if (this.stopTraversal) return customModifiers;
      for (int i = 0, n = customModifiers.Count; i < n; i++)
        customModifiers[i] = this.Visit(this.GetMutableCopy(customModifiers[i]));
      return customModifiers;
    }

    public virtual CustomModifier Visit(CustomModifier customModifier) {
      if (this.stopTraversal) return customModifier;
      this.path.Push(customModifier);
      customModifier.Modifier = this.Visit(customModifier.Modifier);
      this.path.Pop();
      return customModifier;
    }

    public virtual List<IEventDefinition> Visit(List<IEventDefinition> eventDefinitions) {
      if (this.stopTraversal) return eventDefinitions;
      for (int i = 0, n = eventDefinitions.Count; i < n; i++)
        eventDefinitions[i] = this.Visit(this.GetMutableCopy(eventDefinitions[i]));
      return eventDefinitions;
    }

    public virtual IEventDefinition Visit(IEventDefinition eventDefinition) {
      return this.Visit(this.GetMutableCopy(eventDefinition));
    }

    public virtual EventDefinition Visit(EventDefinition eventDefinition) {
      if (this.stopTraversal) return eventDefinition;
      this.Visit((TypeDefinitionMember)eventDefinition);
      this.path.Push(eventDefinition);
      eventDefinition.Accessors = this.Visit(eventDefinition.Accessors);
      eventDefinition.Adder = this.Visit(eventDefinition.Adder);
      if (eventDefinition.Caller != null)
        eventDefinition.Caller = this.Visit(eventDefinition.Caller);
      eventDefinition.Remover = this.Visit(eventDefinition.Remover);
      eventDefinition.Type = this.Visit(eventDefinition.Type);
      this.path.Pop();
      return eventDefinition;
    }

    public virtual List<IFieldDefinition> Visit(List<IFieldDefinition> fieldDefinitions) {
      if (this.stopTraversal) return fieldDefinitions;
      for (int i = 0, n = fieldDefinitions.Count; i < n; i++)
        fieldDefinitions[i] = this.Visit(this.GetMutableCopy(fieldDefinitions[i]));
      return fieldDefinitions;
    }

    public virtual FieldDefinition Visit(FieldDefinition fieldDefinition) {
      if (this.stopTraversal) return fieldDefinition;
      this.Visit((TypeDefinitionMember)fieldDefinition);
      this.path.Push(fieldDefinition);
      if (fieldDefinition.IsCompileTimeConstant)
        fieldDefinition.CompileTimeValue = this.Visit(this.GetMutableCopy(fieldDefinition.CompileTimeValue));
      if (fieldDefinition.IsMarshalledExplicitly)
        fieldDefinition.MarshallingInformation = this.Visit(this.GetMutableCopy(fieldDefinition.MarshallingInformation));
      fieldDefinition.Type = this.Visit(fieldDefinition.Type);
      this.path.Pop();
      return fieldDefinition;
    }

    public virtual IFieldDefinition Visit(IFieldDefinition fieldDefinition) {
      return this.Visit(this.GetMutableCopy(fieldDefinition));
    }

    public virtual IFieldReference Visit(IFieldReference fieldReference) {
      if (this.stopTraversal) return fieldReference;
      if (fieldReference == Dummy.FieldReference || fieldReference == Dummy.Field) return Dummy.FieldReference;
      ISpecializedFieldReference/*?*/ specializedFieldReference = fieldReference as ISpecializedFieldReference;
      if (specializedFieldReference != null)
        return this.Visit(this.GetMutableCopy(specializedFieldReference));
      IGlobalFieldDefinition/*?*/ globalFieldDefinition = fieldReference as IGlobalFieldDefinition;
      if (globalFieldDefinition != null)
        return this.GetMutableCopy(globalFieldDefinition);
      IFieldDefinition/*?*/ fieldDefinition = fieldReference as IFieldDefinition;
      if (fieldDefinition != null)
        return this.GetMutableCopy(fieldDefinition);
      return this.Visit(this.GetMutableCopy(fieldReference));
    }

    public virtual FieldReference Visit(FieldReference fieldReference) {
      if (this.stopTraversal) return fieldReference;
      this.path.Push(fieldReference);
      fieldReference.Attributes = this.Visit(fieldReference.Attributes);
      fieldReference.ContainingType = this.Visit(fieldReference.ContainingType);
      fieldReference.Locations = this.Visit(fieldReference.Locations);
      fieldReference.Type = this.Visit(fieldReference.Type);
      this.path.Pop();
      return fieldReference;
    }

    public virtual List<IFileReference> Visit(List<IFileReference> fileReferences) {
      if (this.stopTraversal) return fileReferences;
      for (int i = 0, n = fileReferences.Count; i < n; i++)
        fileReferences[i] = this.Visit(this.GetMutableCopy(fileReferences[i]));
      return fileReferences;
    }

    public virtual FileReference Visit(FileReference fileReference) {
      return fileReference;
    }

    public virtual FunctionPointerTypeReference Visit(FunctionPointerTypeReference functionPointerTypeReference) {
      if (this.stopTraversal) return functionPointerTypeReference;
      this.Visit((TypeReference)functionPointerTypeReference);
      this.path.Push(functionPointerTypeReference);
      functionPointerTypeReference.ExtraArgumentTypes = this.Visit(functionPointerTypeReference.ExtraArgumentTypes);
      functionPointerTypeReference.Parameters = this.Visit(functionPointerTypeReference.Parameters);
      if (functionPointerTypeReference.ReturnValueIsModified)
        functionPointerTypeReference.ReturnValueCustomModifiers = this.Visit(functionPointerTypeReference.ReturnValueCustomModifiers);
      functionPointerTypeReference.Type = this.Visit(functionPointerTypeReference.Type);
      this.path.Pop();
      return functionPointerTypeReference;
    }

    public virtual GenericMethodInstanceReference Visit(GenericMethodInstanceReference genericMethodInstanceReference) {
      if (this.stopTraversal) return genericMethodInstanceReference;
      this.Visit((MethodReference)genericMethodInstanceReference);
      this.path.Push(genericMethodInstanceReference);
      genericMethodInstanceReference.GenericArguments = this.Visit(genericMethodInstanceReference.GenericArguments);
      genericMethodInstanceReference.GenericMethod = this.Visit(genericMethodInstanceReference.GenericMethod);
      this.path.Pop();
      return genericMethodInstanceReference;
    }

    public virtual List<IGenericMethodParameter> Visit(List<IGenericMethodParameter> genericMethodParameters, IMethodDefinition declaringMethod) {
      if (this.stopTraversal) return genericMethodParameters;
      for (int i = 0, n = genericMethodParameters.Count; i < n; i++)
        genericMethodParameters[i] = this.Visit(this.GetMutableCopy(genericMethodParameters[i]));
      return genericMethodParameters;
    }

    public virtual GenericMethodParameter Visit(GenericMethodParameter genericMethodParameter) {
      if (this.stopTraversal) return genericMethodParameter;
      this.Visit((GenericParameter)genericMethodParameter);
      genericMethodParameter.DefiningMethod = this.GetCurrentMethod();
      return genericMethodParameter;
    }

    public virtual GenericMethodParameterReference Visit(GenericMethodParameterReference genericMethodParameterReference) {
      if (this.stopTraversal) return genericMethodParameterReference;
      this.Visit((TypeReference)genericMethodParameterReference);
      this.path.Push(genericMethodParameterReference);
      var definingMethod = this.GetTypeSpecificMutableCopy(genericMethodParameterReference.DefiningMethod);
      if (definingMethod != genericMethodParameterReference.DefiningMethod) {
        genericMethodParameterReference.DefiningMethod = definingMethod;
        genericMethodParameterReference.DefiningMethod = this.Visit(definingMethod);
      }
      this.path.Pop();
      return genericMethodParameterReference;
    }

    public virtual GenericTypeParameterReference Visit(GenericTypeParameterReference genericTypeParameterReference) {
      if (this.stopTraversal) return genericTypeParameterReference;
      this.Visit((TypeReference)genericTypeParameterReference);
      this.path.Push(genericTypeParameterReference);
      genericTypeParameterReference.DefiningType = this.Visit(genericTypeParameterReference.DefiningType);
      this.path.Pop();
      return genericTypeParameterReference;
    }

    public virtual GlobalFieldDefinition Visit(GlobalFieldDefinition globalFieldDefinition) {
      if (this.stopTraversal) return globalFieldDefinition;
      this.path.Push(this.Visit(globalFieldDefinition.ContainingType));
      this.Visit((FieldDefinition)globalFieldDefinition);
      this.path.Pop();
      globalFieldDefinition.ContainingNamespace = this.GetCurrentNamespace();
      return globalFieldDefinition;
    }

    public virtual GlobalMethodDefinition Visit(GlobalMethodDefinition globalMethodDefinition) {
      if (this.stopTraversal) return globalMethodDefinition;
      this.path.Push(this.Visit(globalMethodDefinition.ContainingType));
      this.Visit((MethodDefinition)globalMethodDefinition);
      this.path.Pop();
      globalMethodDefinition.ContainingNamespace = this.GetCurrentNamespace();
      return globalMethodDefinition;
    }

    public virtual GenericTypeInstanceReference Visit(GenericTypeInstanceReference genericTypeInstanceReference) {
      if (this.stopTraversal) return genericTypeInstanceReference;
      this.Visit((TypeReference)genericTypeInstanceReference);
      this.path.Push(genericTypeInstanceReference);
      genericTypeInstanceReference.GenericArguments = this.Visit(genericTypeInstanceReference.GenericArguments);
      genericTypeInstanceReference.GenericType = this.Visit(genericTypeInstanceReference.GenericType);
      this.path.Pop();
      return genericTypeInstanceReference;
    }

    public virtual GenericParameter Visit(GenericParameter genericParameter) {
      if (this.stopTraversal) return genericParameter;
      this.path.Push(genericParameter);
      genericParameter.Attributes = this.Visit(genericParameter.Attributes);
      genericParameter.Constraints = this.Visit(genericParameter.Constraints);
      this.path.Pop();
      return genericParameter;
    }

    public virtual List<IGenericTypeParameter> Visit(List<IGenericTypeParameter> genericTypeParameters) {
      if (this.stopTraversal) return genericTypeParameters;
      for (int i = 0, n = genericTypeParameters.Count; i < n; i++)
        genericTypeParameters[i] = this.Visit(this.GetMutableCopy(genericTypeParameters[i]));
      return genericTypeParameters;
    }

    public virtual GenericTypeParameter Visit(GenericTypeParameter genericTypeParameter) {
      if (this.stopTraversal) return genericTypeParameter;
      this.Visit((GenericParameter)genericTypeParameter);
      genericTypeParameter.DefiningType = this.GetCurrentType();
      return genericTypeParameter;
    }

    public virtual List<ILocation> Visit(List<ILocation> locations) {
      if (this.stopTraversal) return locations;
      for (int i = 0, n = locations.Count; i < n; i++)
        locations[i] = this.Visit(locations[i]);
      return locations;
    }

    public virtual ILocation Visit(ILocation location){
      return location;
    }

    public virtual LocalDefinition Visit(LocalDefinition localDefinition) {
      if (this.stopTraversal) return localDefinition;
      this.path.Push(localDefinition);
      localDefinition.CustomModifiers = this.Visit(localDefinition.CustomModifiers);
      localDefinition.Type = this.Visit(localDefinition.Type);
      this.path.Pop();
      return localDefinition;
    }

    public virtual MarshallingInformation Visit(MarshallingInformation marshallingInformation) {
      if (this.stopTraversal) return marshallingInformation;
      this.path.Push(marshallingInformation);
      if (marshallingInformation.UnmanagedType == UnmanagedType.CustomMarshaler)
        marshallingInformation.CustomMarshaller = this.Visit(marshallingInformation.CustomMarshaller);
      if (marshallingInformation.UnmanagedType == UnmanagedType.SafeArray && 
      (marshallingInformation.SafeArrayElementSubType == VarEnum.VT_DISPATCH || 
      marshallingInformation.SafeArrayElementSubType == VarEnum.VT_UNKNOWN || 
      marshallingInformation.SafeArrayElementSubType == VarEnum.VT_RECORD))
        marshallingInformation.SafeArrayElementUserDefinedSubType = this.Visit(marshallingInformation.SafeArrayElementUserDefinedSubType);
      this.path.Pop();
      return marshallingInformation;
    }

    public virtual MetadataConstant Visit(MetadataConstant constant) {
      if (this.stopTraversal) return constant;
      this.path.Push(constant);
      constant.Locations = this.Visit(constant.Locations);
      constant.Type = this.Visit(constant.Type);
      this.path.Pop();
      return constant;
    }

    public virtual MetadataCreateArray Visit(MetadataCreateArray createArray) {
      if (this.stopTraversal) return createArray;
      this.path.Push(createArray);
      createArray.ElementType = this.Visit(createArray.ElementType);
      createArray.Initializers = this.Visit(createArray.Initializers);
      createArray.Locations = this.Visit(createArray.Locations);
      createArray.Type = this.Visit(createArray.Type);
      this.path.Pop();
      return createArray;
    }

    public virtual List<IMetadataExpression> Visit(List<IMetadataExpression> metadataExpressions) {
      if (this.stopTraversal) return metadataExpressions;
      for (int i = 0, n = metadataExpressions.Count; i < n; i++)
        metadataExpressions[i] = this.Visit(metadataExpressions[i]);
      return metadataExpressions;
    }

    public virtual IMetadataExpression Visit(IMetadataExpression expression) {
      if (this.stopTraversal) return expression;
      IMetadataConstant/*?*/ metadataConstant = expression as IMetadataConstant;
      if (metadataConstant != null) return this.Visit(this.GetMutableCopy(metadataConstant));
      IMetadataCreateArray/*?*/ metadataCreateArray = expression as IMetadataCreateArray;
      if (metadataCreateArray != null) return this.Visit(this.GetMutableCopy(metadataCreateArray));
      IMetadataTypeOf/*?*/ metadataTypeOf = expression as IMetadataTypeOf;
      if (metadataTypeOf != null) return this.Visit(this.GetMutableCopy(metadataTypeOf));
      return expression;
    }

    public virtual List<IMetadataNamedArgument> Visit(List<IMetadataNamedArgument> namedArguments) {
      if (this.stopTraversal) return namedArguments;
      for (int i = 0, n = namedArguments.Count; i < n; i++)
        namedArguments[i] = this.Visit(this.GetMutableCopy(namedArguments[i]));
      return namedArguments;
    }

    public virtual MetadataNamedArgument Visit(MetadataNamedArgument namedArgument) {
      if (this.stopTraversal) return namedArgument;
      this.path.Push(namedArgument);
      namedArgument.ArgumentValue = this.Visit(namedArgument.ArgumentValue);
      namedArgument.Locations = this.Visit(namedArgument.Locations);
      namedArgument.Type = this.Visit(namedArgument.Type);
      this.path.Pop();
      return namedArgument;
    }

    public virtual MetadataTypeOf Visit(MetadataTypeOf typeOf) {
      if (this.stopTraversal) return typeOf;
      this.path.Push(typeOf);
      typeOf.Locations = this.Visit(typeOf.Locations);
      typeOf.Type = this.Visit(typeOf.Type);
      typeOf.TypeToGet = this.Visit(typeOf.TypeToGet);
      this.path.Pop();
      return typeOf;
    }

    public virtual MatrixTypeReference Visit(MatrixTypeReference matrixTypeReference) {
      if (this.stopTraversal) return matrixTypeReference;
      this.Visit((TypeReference)matrixTypeReference);
      this.path.Push(matrixTypeReference);
      matrixTypeReference.ElementType = this.Visit(matrixTypeReference.ElementType);
      this.path.Pop();
      return matrixTypeReference;
    }

    public virtual MethodBody Visit(MethodBody methodBody) {
      if (this.stopTraversal) return methodBody;
      this.path.Push(methodBody);
      methodBody.MethodDefinition = this.GetCurrentMethod();
      methodBody.LocalVariables = this.Visit(methodBody.LocalVariables);
      methodBody.Operations = this.Visit(methodBody.Operations);
      methodBody.OperationExceptionInformation = this.Visit(methodBody.OperationExceptionInformation);
      this.path.Pop();
      return methodBody;
    }

    public virtual List<IOperationExceptionInformation> Visit(List<IOperationExceptionInformation> exceptionInformations) {
      if (this.stopTraversal) return exceptionInformations;
      for (int i = 0, n = exceptionInformations.Count; i < n; i++)
        exceptionInformations[i] = this.Visit(this.GetMutableCopy(exceptionInformations[i]));
      return exceptionInformations;
    }

    public virtual OperationExceptionInformation Visit(OperationExceptionInformation operationExceptionInformation) {
      if (this.stopTraversal) return operationExceptionInformation;
      this.path.Push(operationExceptionInformation);
      operationExceptionInformation.ExceptionType = this.Visit(operationExceptionInformation.ExceptionType);
      this.path.Pop();
      return operationExceptionInformation;
    }

    public virtual List<IOperation> Visit(List<IOperation> operations) {
      if (this.stopTraversal) return operations;
      for (int i = 0, n = operations.Count; i < n; i++)
        operations[i] = this.Visit(this.GetMutableCopy(operations[i]));
      return operations;
    }

    public virtual Operation Visit(Operation operation) {
      if (this.stopTraversal) return operation;
      this.path.Push(operation);
      ITypeReference/*?*/ typeReference = operation.Value as ITypeReference;
      if (typeReference != null)
        operation.Value = this.Visit(typeReference);
      else {
        IFieldReference/*?*/ fieldReference = operation.Value as IFieldReference;
        if (fieldReference != null)
          operation.Value = this.Visit(fieldReference);
        else {
          IMethodReference/*?*/ methodReference = operation.Value as IMethodReference;
          if (methodReference != null)
            operation.Value = this.Visit(methodReference);
          else {
            IParameterDefinition/*?*/ parameterDefinition = operation.Value as IParameterDefinition;
            if (parameterDefinition != null)
              operation.Value = this.GetMutableCopyIfItExists(parameterDefinition);
            else {
              ILocalDefinition/*?*/ localDefinition = operation.Value as ILocalDefinition;
              if (localDefinition != null)
                operation.Value = this.GetMutableCopyIfItExists(localDefinition);
            }
          }
        }
      }
      this.path.Pop();
      return operation;
    }

    public virtual object GetMutableCopyIfItExists(IParameterDefinition parameterDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(parameterDefinition, out cachedValue);
      return cachedValue != null ? cachedValue : parameterDefinition;
    }

    public virtual object GetMutableCopyIfItExists(ILocalDefinition localDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(localDefinition, out cachedValue);
      return cachedValue != null ? cachedValue : localDefinition;
    }

    public virtual List<ILocalDefinition> Visit(List<ILocalDefinition> locals) {
      if (this.stopTraversal) return locals;
      for (int i = 0, n = locals.Count; i < n; i++)
        locals[i] = this.Visit(this.GetMutableCopy(locals[i]));
      return locals;
    }

    public virtual List<IMethodDefinition> Visit(List<IMethodDefinition> methodDefinitions) {
      if (this.stopTraversal) return methodDefinitions;
      for (int i = 0, n = methodDefinitions.Count; i < n; i++)
        methodDefinitions[i] = this.Visit(methodDefinitions[i]);
      return methodDefinitions;
    }

    public virtual IGlobalFieldDefinition Visit(IGlobalFieldDefinition globalFieldDefinition) {
      return this.Visit(this.GetMutableCopy(globalFieldDefinition));
    }

    public virtual IGlobalMethodDefinition Visit(IGlobalMethodDefinition globalMethodDefinition) {
      return this.Visit(this.GetMutableCopy(globalMethodDefinition));
    }

    public virtual IMethodDefinition Visit(IMethodDefinition methodDefinition) {
      return this.Visit(this.GetMutableCopy(methodDefinition));
    }

    public virtual MethodDefinition Visit(MethodDefinition methodDefinition) {
      if (this.stopTraversal) return methodDefinition;
      if (methodDefinition == Dummy.Method) return methodDefinition;
      this.Visit((TypeDefinitionMember)methodDefinition);
      this.path.Push(methodDefinition);
      if (methodDefinition.IsGeneric)
        methodDefinition.GenericParameters = this.Visit(methodDefinition.GenericParameters, methodDefinition);
      methodDefinition.Parameters = this.Visit(methodDefinition.Parameters);
      if (methodDefinition.IsPlatformInvoke)
        methodDefinition.PlatformInvokeData = this.Visit(this.GetMutableCopy(methodDefinition.PlatformInvokeData));
      methodDefinition.ReturnValueAttributes = this.VisitMethodReturnValueAttributes(methodDefinition.ReturnValueAttributes);
      if (methodDefinition.ReturnValueIsModified)
        methodDefinition.ReturnValueCustomModifiers = this.VisitMethodReturnValueCustomModifiers(methodDefinition.ReturnValueCustomModifiers);
      if (methodDefinition.ReturnValueIsMarshalledExplicitly)
        methodDefinition.ReturnValueMarshallingInformation = this.VisitMethodReturnValueMarshallingInformation(this.GetMutableCopy(methodDefinition.ReturnValueMarshallingInformation));
      if (methodDefinition.HasDeclarativeSecurity)
        methodDefinition.SecurityAttributes = this.Visit(methodDefinition.SecurityAttributes);
      methodDefinition.Type = this.Visit(methodDefinition.Type);
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal)
        methodDefinition.Body = this.Visit(methodDefinition.Body);
      this.path.Pop();
      return methodDefinition;
    }

    public virtual List<IMethodImplementation> Visit(List<IMethodImplementation> methodImplementations) {
      if (this.stopTraversal) return methodImplementations;
      for (int i = 0, n = methodImplementations.Count; i < n; i++)
        methodImplementations[i] = this.Visit(this.GetMutableCopy(methodImplementations[i]));
      return methodImplementations;
    }

    public virtual MethodImplementation Visit(MethodImplementation methodImplementation) {
      if (this.stopTraversal) return methodImplementation;
      this.path.Push(methodImplementation);
      methodImplementation.ContainingType = this.GetCurrentType();
      methodImplementation.ImplementedMethod = this.Visit(methodImplementation.ImplementedMethod);
      methodImplementation.ImplementingMethod = this.Visit(methodImplementation.ImplementingMethod);
      this.path.Pop();
      return methodImplementation;
    }

    public virtual List<IMethodReference> Visit(List<IMethodReference> methodReferences) {
      if (this.stopTraversal) return methodReferences;
      for (int i = 0, n = methodReferences.Count; i < n; i++)
        methodReferences[i] = this.Visit(methodReferences[i]);
      return methodReferences;
    }

    public virtual IMethodBody Visit(IMethodBody methodBody) {
      return this.Visit(this.GetMutableCopy(methodBody));
    }

    public virtual IMethodReference Visit(IMethodReference methodReference) {
      if (this.stopTraversal) return methodReference;
      if (methodReference == Dummy.MethodReference || methodReference == Dummy.Method) return Dummy.MethodReference;
      ISpecializedMethodReference/*?*/ specializedMethodReference = methodReference as ISpecializedMethodReference;
      if (specializedMethodReference != null)
        return this.Visit(this.GetMutableCopy(specializedMethodReference));
      IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null)
        return this.Visit(this.GetMutableCopy(genericMethodInstanceReference));
      else {
        IGlobalMethodDefinition/*?*/ globalMethodDefinition = methodReference as IGlobalMethodDefinition;
        if (globalMethodDefinition != null)
          return this.GetMutableCopy(globalMethodDefinition);
        IMethodDefinition/*?*/ methodDefinition = methodReference as IMethodDefinition;
        if (methodDefinition != null) {
          return this.GetMutableCopy(methodDefinition);
        }
        return this.Visit(this.GetMutableCopy(methodReference));
      }
    }

    public virtual MethodReference Visit(MethodReference methodReference) {
      if (this.stopTraversal) return methodReference;
      this.path.Push(methodReference);
      methodReference.Attributes = this.Visit(methodReference.Attributes);
      methodReference.ContainingType = this.Visit(methodReference.ContainingType);
      methodReference.ExtraParameters = this.Visit(methodReference.ExtraParameters);
      methodReference.Locations = this.Visit(methodReference.Locations);
      methodReference.Parameters = this.Visit(methodReference.Parameters);
      if (methodReference.ReturnValueIsModified)
        methodReference.ReturnValueCustomModifiers = this.Visit(methodReference.ReturnValueCustomModifiers);
      methodReference.Type = this.Visit(methodReference.Type);
      this.path.Pop();
      return methodReference;
    }

    public virtual List<IModule> Visit(List<IModule> modules) {
      if (this.stopTraversal) return modules;
      for (int i = 0, n = modules.Count; i < n; i++) {
        modules[i] = this.Visit(this.GetMutableCopy(modules[i]));
        this.flatListOfTypes.Clear();
      }
      return modules;
    }

    public virtual ModifiedTypeReference Visit(ModifiedTypeReference modifiedTypeReference) {
      if (this.stopTraversal) return modifiedTypeReference;
      this.Visit((TypeReference)modifiedTypeReference);
      this.path.Push(modifiedTypeReference);
      modifiedTypeReference.CustomModifiers = this.Visit(modifiedTypeReference.CustomModifiers);
      modifiedTypeReference.UnmodifiedType = this.Visit(modifiedTypeReference.UnmodifiedType);
      this.path.Pop();
      return modifiedTypeReference;
    }

    public virtual Module Visit(Module module) {
      if (this.stopTraversal) return module;
      this.path.Push(module);
      module.AssemblyReferences = this.Visit(module.AssemblyReferences);
      module.Locations = this.Visit(module.Locations);
      module.ModuleAttributes = this.Visit(module.ModuleAttributes);
      module.ModuleReferences = this.Visit(module.ModuleReferences);
      module.Win32Resources = this.Visit(module.Win32Resources);
      module.UnitNamespaceRoot = this.Visit(this.GetMutableCopy((IRootUnitNamespace)module.UnitNamespaceRoot));
      this.path.Push(module.UnitNamespaceRoot);
      if (module.AllTypes.Count > 0)
        this.Visit(this.GetMutableCopy((INamespaceTypeDefinition)module.AllTypes[0]));
      if (module.AllTypes.Count > 1) {
        INamespaceTypeDefinition globalsType = module.AllTypes[1] as INamespaceTypeDefinition;
        if (globalsType != null && globalsType.Name.Value == "__Globals__")
          this.Visit(this.GetMutableCopy(globalsType));
      }
      this.path.Pop();
      if (module.EntryPoint != Dummy.MethodReference)
        module.EntryPoint = this.GetMutableCopy(module.EntryPoint.ResolvedMethod);
      this.VisitPrivateHelperMembers(this.flatListOfTypes);
      this.flatListOfTypes.Sort(new TypeOrderPreserver(module.AllTypes));
      module.AllTypes = this.flatListOfTypes;
      this.flatListOfTypes = new List<INamedTypeDefinition>();
      this.path.Pop();
      return module;
    }

    class TypeOrderPreserver : Comparer<INamedTypeDefinition> {

      Dictionary<string, int> oldOrder = new Dictionary<string, int>();

      internal TypeOrderPreserver(List<INamedTypeDefinition> oldTypeList) {
        for (int i = 0, n = oldTypeList.Count; i < n; i++)
          this.oldOrder.Add(TypeHelper.GetTypeName(oldTypeList[i], NameFormattingOptions.TypeParameters), i);
      }

      public override int Compare(INamedTypeDefinition x, INamedTypeDefinition y) {
        int xi = 0;
        int yi = int.MaxValue;
        string xn = TypeHelper.GetTypeName(x, NameFormattingOptions.TypeParameters);
        string yn = TypeHelper.GetTypeName(y, NameFormattingOptions.TypeParameters);
        if (!this.oldOrder.TryGetValue(xn, out xi)) xi = int.MaxValue;
        if (!this.oldOrder.TryGetValue(yn, out yi)) yi = int.MaxValue;
        return xi - yi;
      }
    }

    public virtual List<IModuleReference> Visit(List<IModuleReference> moduleReferences) {
      if (this.stopTraversal) return moduleReferences;
      for (int i = 0, n = moduleReferences.Count; i < n; i++)
        moduleReferences[i] = this.Visit(this.GetMutableCopy(moduleReferences[i]));
      return moduleReferences;
    }

    public virtual ModuleReference Visit(ModuleReference moduleReference) {
      if (moduleReference.ResolvedModule != Dummy.Module) {
        object/*?*/ mutatedResolvedModule = null;
        if (this.cache.TryGetValue(moduleReference.ResolvedModule, out mutatedResolvedModule))
          moduleReference.ResolvedModule = (IModule)mutatedResolvedModule;
      }
      return moduleReference;
    }

    public virtual List<INamespaceMember> Visit(List<INamespaceMember> namespaceMembers) {
      if (this.stopTraversal) return namespaceMembers;
      for (int i = 0, n = namespaceMembers.Count; i < n; i++)
        namespaceMembers[i] = this.Visit(namespaceMembers[i]);
      return namespaceMembers;
    }

    public virtual INamespaceMember Visit(INamespaceMember namespaceMember) {
      if (this.stopTraversal) return namespaceMember;
      INamespaceTypeDefinition/*?*/ namespaceTypeDefinition = namespaceMember as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null) return this.Visit(namespaceTypeDefinition);
      INestedUnitNamespace/*?*/ nestedUnitNamespace = namespaceMember as INestedUnitNamespace;
      if (nestedUnitNamespace != null) return this.Visit(nestedUnitNamespace);
      IGlobalMethodDefinition/*?*/ globalMethodDefinition = namespaceMember as IGlobalMethodDefinition;
      if (globalMethodDefinition != null) return this.Visit(globalMethodDefinition);
      IGlobalFieldDefinition/*?*/ globalFieldDefinition = namespaceMember as IGlobalFieldDefinition;
      if (globalFieldDefinition != null) return this.Visit(globalFieldDefinition);
      return namespaceMember;
    }

    public virtual NamespaceAliasForType Visit(NamespaceAliasForType namespaceAliasForType) {
      if (this.stopTraversal) return namespaceAliasForType;
      this.path.Push(namespaceAliasForType);
      namespaceAliasForType.AliasedType = this.Visit(namespaceAliasForType.AliasedType);
      namespaceAliasForType.Attributes = this.Visit(namespaceAliasForType.Attributes);
      namespaceAliasForType.Locations = this.Visit(namespaceAliasForType.Locations);
      //TODO: what about the containing namespace? Should that be a reference?
      this.path.Pop();
      return namespaceAliasForType;
    }

    public virtual NamespaceTypeDefinition Visit(NamespaceTypeDefinition namespaceTypeDefinition) {
      if (this.stopTraversal) return namespaceTypeDefinition;
      this.Visit((TypeDefinition)namespaceTypeDefinition);
      namespaceTypeDefinition.ContainingUnitNamespace = this.GetCurrentNamespace();
      return namespaceTypeDefinition;
    }

    public virtual NamespaceTypeReference Visit(NamespaceTypeReference namespaceTypeReference) {
      if (this.stopTraversal) return namespaceTypeReference;
      this.Visit((TypeReference)namespaceTypeReference);
      this.path.Push(namespaceTypeReference);
      namespaceTypeReference.ContainingUnitNamespace = this.Visit(namespaceTypeReference.ContainingUnitNamespace);
      this.path.Pop();
      return namespaceTypeReference;
    }

    public virtual NestedAliasForType Visit(NestedAliasForType nestedAliasForType) {
      if (this.stopTraversal) return nestedAliasForType;
      this.path.Push(nestedAliasForType);
      nestedAliasForType.AliasedType = this.Visit(nestedAliasForType.AliasedType);
      nestedAliasForType.Attributes = this.Visit(nestedAliasForType.Attributes);
      nestedAliasForType.Locations = this.Visit(nestedAliasForType.Locations);
      //TODO: what about the containing type? Should that be a reference?
      this.path.Pop();
      return nestedAliasForType;
    }

    public virtual List<INestedTypeDefinition> Visit(List<INestedTypeDefinition> nestedTypeDefinitions) {
      if (this.stopTraversal) return nestedTypeDefinitions;
      for (int i = 0, n = nestedTypeDefinitions.Count; i < n; i++)
        nestedTypeDefinitions[i] = this.Visit(this.GetMutableCopy(nestedTypeDefinitions[i]));
      return nestedTypeDefinitions;
    }

    public virtual NestedTypeDefinition Visit(NestedTypeDefinition nestedTypeDefinition) {
      if (this.stopTraversal) return nestedTypeDefinition;
      this.Visit((TypeDefinition)nestedTypeDefinition);
      nestedTypeDefinition.ContainingTypeDefinition = this.GetCurrentType();
      return nestedTypeDefinition;
    }

    public virtual NestedTypeReference Visit(NestedTypeReference nestedTypeReference) {
      if (this.stopTraversal) return nestedTypeReference;
      this.Visit((TypeReference)nestedTypeReference);
      this.path.Push(nestedTypeReference);
      nestedTypeReference.ContainingType = this.Visit(nestedTypeReference.ContainingType);
      this.path.Pop();
      return nestedTypeReference;
    }

    public virtual SpecializedFieldReference Visit(SpecializedFieldReference specializedFieldReference) {
      if (this.stopTraversal) return specializedFieldReference;
      this.Visit((FieldReference)specializedFieldReference);
      this.path.Push(specializedFieldReference);
      specializedFieldReference.UnspecializedVersion = this.Visit(specializedFieldReference.UnspecializedVersion);
      this.path.Pop();
      return specializedFieldReference;
    }

    public virtual SpecializedMethodReference Visit(SpecializedMethodReference specializedMethodReference) {
      if (this.stopTraversal) return specializedMethodReference;
      this.Visit((MethodReference)specializedMethodReference);
      this.path.Push(specializedMethodReference);
      specializedMethodReference.UnspecializedVersion = this.Visit(specializedMethodReference.UnspecializedVersion);
      this.path.Pop();
      return specializedMethodReference;
    }

    public virtual SpecializedNestedTypeReference Visit(SpecializedNestedTypeReference specializedNestedTypeReference) {
      if (this.stopTraversal) return specializedNestedTypeReference;
      this.Visit((NestedTypeReference)specializedNestedTypeReference);
      this.path.Push(specializedNestedTypeReference);
      specializedNestedTypeReference.UnspecializedVersion = (INestedTypeReference)this.Visit(specializedNestedTypeReference.UnspecializedVersion);
      this.path.Pop();
      return specializedNestedTypeReference;
    }

    /// <summary>
    /// Replaces the child nodes of the given mutable type definition with the results of running the mutator over them. 
    /// Note that when overriding this method, care must be taken to add the given mutable type definition to this.flatListOfTypes.
    /// </summary>
    /// <param name="typeDefinition">A mutable type definition.</param>
    protected virtual void Visit(TypeDefinition typeDefinition) {
      if (this.stopTraversal) return;
      this.flatListOfTypes.Add(typeDefinition);
      this.path.Push(typeDefinition);
      typeDefinition.Attributes = this.Visit(typeDefinition.Attributes);
      typeDefinition.BaseClasses = this.Visit(typeDefinition.BaseClasses);
      typeDefinition.ExplicitImplementationOverrides = this.Visit(typeDefinition.ExplicitImplementationOverrides);
      typeDefinition.GenericParameters = this.Visit(typeDefinition.GenericParameters);
      typeDefinition.Interfaces = this.Visit(typeDefinition.Interfaces);
      typeDefinition.Locations = this.Visit(typeDefinition.Locations);
      typeDefinition.Events = this.Visit(typeDefinition.Events);
      typeDefinition.Fields = this.Visit(typeDefinition.Fields);
      typeDefinition.Methods = this.Visit(typeDefinition.Methods);
      typeDefinition.NestedTypes = this.Visit(typeDefinition.NestedTypes);
      typeDefinition.Properties = this.Visit(typeDefinition.Properties);
      if (typeDefinition.HasDeclarativeSecurity)
        typeDefinition.SecurityAttributes = this.Visit(typeDefinition.SecurityAttributes);
      if (typeDefinition.IsEnum)
        typeDefinition.UnderlyingType = this.Visit(typeDefinition.UnderlyingType);
      this.path.Pop();
    }

    public virtual void VisitPrivateHelperMembers(List<INamedTypeDefinition> typeDefinitions) {
      if (this.stopTraversal) return;
      for (int i = 0, n = typeDefinitions.Count; i < n; i++) {
        TypeDefinition/*?*/ typeDef = typeDefinitions[i] as TypeDefinition;
        if (typeDef == null) continue;
        this.path.Push(typeDef);
        typeDef.PrivateHelperMembers = this.Visit(typeDef.PrivateHelperMembers);
        this.path.Pop();
      }
    }

    public virtual List<ITypeDefinitionMember> Visit(List<ITypeDefinitionMember> typeDefinitionMembers) {
      if (this.stopTraversal) return typeDefinitionMembers;
      for (int i = 0, n = typeDefinitionMembers.Count; i < n; i++)
        typeDefinitionMembers[i] = this.Visit(typeDefinitionMembers[i]);
      return typeDefinitionMembers;
    }

    public virtual ITypeDefinitionMember Visit(ITypeDefinitionMember typeDefinitionMember) {
      IEventDefinition/*?*/ eventDef = typeDefinitionMember as IEventDefinition;
      if (eventDef != null) return this.Visit(eventDef);
      IFieldDefinition/*?*/ fieldDef = typeDefinitionMember as IFieldDefinition;
      if (fieldDef != null) return this.Visit(fieldDef);
      IMethodDefinition/*?*/ methodDef = typeDefinitionMember as IMethodDefinition;
      if (methodDef != null) return this.Visit(methodDef);
      INestedTypeDefinition/*?*/ nestedTypeDef = typeDefinitionMember as INestedTypeDefinition;
      if (nestedTypeDef != null) return this.Visit(nestedTypeDef);
      IPropertyDefinition/*?*/ propertyDef = typeDefinitionMember as IPropertyDefinition;
      if (propertyDef != null) return this.Visit(propertyDef);
      Debug.Assert(false);
      return typeDefinitionMember;
    }

    public virtual List<ITypeReference> Visit(List<ITypeReference> typeReferences) {
      if (this.stopTraversal) return typeReferences;
      for (int i = 0, n = typeReferences.Count; i < n; i++)
        typeReferences[i] = this.Visit(typeReferences[i]);
      return typeReferences;
    }

    public virtual INamespaceTypeReference Visit(INamespaceTypeReference namespaceTypeReference) {
      INamespaceTypeDefinition/*?*/ namespaceTypeDefinition = namespaceTypeReference as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null)
        return this.GetMutableCopy(namespaceTypeDefinition);
      return this.Visit(this.GetMutableCopy(namespaceTypeReference));
    }

    public virtual INestedTypeReference Visit(INestedTypeReference nestedTypeReference) {
      ISpecializedNestedTypeReference/*?*/ specializedNestedTypeReference = nestedTypeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null)
        return this.Visit(this.GetMutableCopy(specializedNestedTypeReference));
      INestedTypeDefinition/*?*/ nestedTypeDefinition = nestedTypeReference as INestedTypeDefinition;
      if (nestedTypeDefinition != null)
        return this.GetMutableCopy(nestedTypeDefinition);
      return this.Visit(this.GetMutableCopy(nestedTypeReference));
    }

    public virtual IGenericMethodParameterReference Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      IGenericMethodParameter/*?*/ genericMethodParameter = genericMethodParameterReference as IGenericMethodParameter;
      if (genericMethodParameter != null)
        return this.GetMutableCopy(genericMethodParameter);
      return this.Visit(this.GetMutableCopy(genericMethodParameterReference));
    }

    /// <summary>
    /// Array types are not nominal types, so always visit the reference, even if
    /// it is a definition.
    /// </summary>
    public virtual IArrayTypeReference Visit(IArrayTypeReference arrayTypeReference) {
      if (arrayTypeReference.IsVector)
        return this.Visit(this.GetMutableVectorCopy(arrayTypeReference));
      else
        return this.Visit(this.GetMutableMatrixCopy(arrayTypeReference));
    }

    public virtual IGenericTypeParameterReference Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      IGenericTypeParameter/*?*/ genericTypeParameter = genericTypeParameterReference as IGenericTypeParameter;
      if (genericTypeParameter != null)
        return this.GetMutableCopy(genericTypeParameter);
      return this.Visit(this.GetMutableCopy(genericTypeParameterReference));
    }

    public virtual IGenericTypeInstanceReference Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      return this.Visit(this.GetMutableCopy(genericTypeInstanceReference));
    }

    /// <summary>
    /// Pointer types are not nominal types, so always visit the reference, even if
    /// it is a definition.
    /// </summary>
    public virtual IPointerTypeReference Visit (IPointerTypeReference pointerTypeReference) {
      return this.Visit(this.GetMutableCopy(pointerTypeReference));
    }

    public virtual IFunctionPointerTypeReference Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      IFunctionPointer/*?*/ functionPointer = functionPointerTypeReference as IFunctionPointer;
      if (functionPointer != null)
        return this.Visit(this.GetMutableCopy(functionPointer));
      return this.Visit(this.GetMutableCopy(functionPointerTypeReference));
    }

    public virtual IModifiedTypeReference Visit(IModifiedTypeReference modifiedTypeReference) {
      return this.Visit(this.GetMutableCopy(modifiedTypeReference));
    }

    public virtual IModuleReference Visit(IModuleReference moduleReference) {
      return this.Visit(this.GetMutableCopy(moduleReference));
    }

    public virtual INamespaceTypeDefinition Visit(INamespaceTypeDefinition typeDefinition) {
      return this.Visit(this.GetMutableCopy(typeDefinition));
    }

    public virtual INestedTypeDefinition Visit(INestedTypeDefinition nestedTypeDefinition) {
      return this.Visit(this.GetMutableCopy(nestedTypeDefinition));
    }

    public virtual ITypeReference Visit(ITypeReference typeReference) {
      INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference as INamespaceTypeReference;
      if (namespaceTypeReference != null)
        return this.Visit(namespaceTypeReference);
      INestedTypeReference/*?*/ nestedTypeReference = typeReference as INestedTypeReference;
      if (nestedTypeReference != null)
        return this.Visit(nestedTypeReference);
      IGenericMethodParameterReference/*?*/ genericMethodParameterReference = typeReference as IGenericMethodParameterReference;
      if (genericMethodParameterReference != null)
        return this.Visit(genericMethodParameterReference);
      IArrayTypeReference/*?*/ arrayTypeReference = typeReference as IArrayTypeReference;
      if (arrayTypeReference != null)
        return this.Visit(arrayTypeReference);
      IGenericTypeParameterReference/*?*/ genericTypeParameterReference = typeReference as IGenericTypeParameterReference;
      if (genericTypeParameterReference != null)
        return this.Visit(genericTypeParameterReference);
      IGenericTypeInstanceReference/*?*/ genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null)
        return this.Visit(genericTypeInstanceReference);
      IPointerTypeReference/*?*/ pointerTypeReference = typeReference as IPointerTypeReference;
      if (pointerTypeReference != null)
        return this.Visit(pointerTypeReference);
      IFunctionPointerTypeReference/*?*/ functionPointerTypeReference = typeReference as IFunctionPointerTypeReference;
      if (functionPointerTypeReference != null)
        return this.Visit(functionPointerTypeReference);
      IModifiedTypeReference/*?*/ modifiedTypeReference = typeReference as IModifiedTypeReference;
      if (modifiedTypeReference != null)
        return this.Visit(modifiedTypeReference);
      //TODO: error
      return typeReference;
    }

    public virtual IUnitNamespaceReference Visit(IUnitNamespaceReference unitNamespaceReference) {
      IRootUnitNamespaceReference/*?*/ rootUnitNamespaceReference = unitNamespaceReference as IRootUnitNamespaceReference;
      if (rootUnitNamespaceReference != null)
        return this.Visit(rootUnitNamespaceReference);
      INestedUnitNamespaceReference/*?*/ nestedUnitNamespaceReference = unitNamespaceReference as INestedUnitNamespaceReference;
      if (nestedUnitNamespaceReference != null)
        return this.Visit(nestedUnitNamespaceReference);
      //TODO: error
      return unitNamespaceReference;
    }

    public virtual INestedUnitNamespace Visit(INestedUnitNamespace nestedUnitNamespace) {
      return this.Visit(this.GetMutableCopy(nestedUnitNamespace));
    }

    public virtual INestedUnitNamespaceReference Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      INestedUnitNamespace/*?*/ nestedUnitNamespace = nestedUnitNamespaceReference as INestedUnitNamespace;
      if (nestedUnitNamespace != null)
        return this.GetMutableCopy(nestedUnitNamespace);
      return this.Visit(this.GetMutableCopy(nestedUnitNamespaceReference));
    }

    public virtual NestedUnitNamespace Visit(NestedUnitNamespace nestedUnitNamespace) {
      if (this.stopTraversal) return nestedUnitNamespace;
      this.Visit((UnitNamespace)nestedUnitNamespace);
      nestedUnitNamespace.ContainingUnitNamespace = this.GetCurrentNamespace();
      return nestedUnitNamespace;
    }

    public virtual NestedUnitNamespaceReference Visit(NestedUnitNamespaceReference nestedUnitNamespaceReference) {
      if (this.stopTraversal) return nestedUnitNamespaceReference;
      this.Visit((UnitNamespaceReference)nestedUnitNamespaceReference);
      nestedUnitNamespaceReference.ContainingUnitNamespace = this.Visit(nestedUnitNamespaceReference.ContainingUnitNamespace);
      return nestedUnitNamespaceReference;
    }

    public virtual IUnitReference Visit(IUnitReference unitReference) {
      IAssemblyReference/*?*/ assemblyReference = unitReference as IAssemblyReference;
      if (assemblyReference != null)
        return this.Visit(assemblyReference);
      IModuleReference/*?*/ moduleReference = unitReference as IModuleReference;
      if (moduleReference != null)
        return this.Visit(moduleReference);
      //TODO: error
      return unitReference;
    }

    public virtual List<IParameterDefinition> Visit(List<IParameterDefinition> parameterDefinitions) {
      if (this.stopTraversal) return parameterDefinitions;
      for (int i = 0, n = parameterDefinitions.Count; i < n; i++)
        parameterDefinitions[i] = this.Visit(this.GetMutableCopy(parameterDefinitions[i]));
      return parameterDefinitions;
    }

    public virtual ParameterDefinition Visit(ParameterDefinition parameterDefinition) {
      if (this.stopTraversal) return parameterDefinition;
      this.path.Push(parameterDefinition);
      parameterDefinition.Attributes = this.Visit(parameterDefinition.Attributes);
      parameterDefinition.ContainingSignature = this.GetCurrentSignature();
      if (parameterDefinition.IsModified)
        parameterDefinition.CustomModifiers = this.Visit(parameterDefinition.CustomModifiers);
      parameterDefinition.Locations = this.Visit(parameterDefinition.Locations);
      if (parameterDefinition.IsMarshalledExplicitly)
        parameterDefinition.MarshallingInformation = this.Visit(this.GetMutableCopy(parameterDefinition.MarshallingInformation));
      parameterDefinition.Type = this.Visit(parameterDefinition.Type);
      this.path.Pop();
      return parameterDefinition;
    }

    public virtual List<IParameterTypeInformation> Visit(List<IParameterTypeInformation> parameterTypeInformationList) {
      if (this.stopTraversal) return parameterTypeInformationList;
      for (int i = 0, n = parameterTypeInformationList.Count; i < n; i++)
        parameterTypeInformationList[i] = this.Visit(this.GetMutableCopy(parameterTypeInformationList[i]));
      return parameterTypeInformationList;
    }

    public virtual ParameterTypeInformation Visit(ParameterTypeInformation parameterTypeInformation) {
      if (this.stopTraversal) return parameterTypeInformation;
      this.path.Push(parameterTypeInformation);
      if (parameterTypeInformation.IsModified)
        parameterTypeInformation.CustomModifiers = this.Visit(parameterTypeInformation.CustomModifiers);
      parameterTypeInformation.Type = this.Visit(parameterTypeInformation.Type);
      this.path.Pop();
      return parameterTypeInformation;
    }

    public virtual PlatformInvokeInformation Visit(PlatformInvokeInformation platformInvokeInformation) {
      if (this.stopTraversal) return platformInvokeInformation;
      this.path.Push(platformInvokeInformation);
      platformInvokeInformation.ImportModule = this.Visit(this.GetMutableCopy(platformInvokeInformation.ImportModule));
      this.path.Pop();
      return platformInvokeInformation;
    }

    public virtual List<IPropertyDefinition> Visit(List<IPropertyDefinition> propertyDefinitions) {
      if (this.stopTraversal) return propertyDefinitions;
      for (int i = 0, n = propertyDefinitions.Count; i < n; i++)
        propertyDefinitions[i] = this.Visit(this.GetMutableCopy(propertyDefinitions[i]));
      return propertyDefinitions;
    }

    public virtual IPropertyDefinition Visit(IPropertyDefinition propertyDefinition) {
      return this.Visit(this.GetMutableCopy(propertyDefinition));
    }

    public virtual PropertyDefinition Visit(PropertyDefinition propertyDefinition) {
      if (this.stopTraversal) return propertyDefinition;
      this.Visit((TypeDefinitionMember)propertyDefinition);
      this.path.Push(propertyDefinition);
      propertyDefinition.Accessors = this.Visit(propertyDefinition.Accessors);
      if (propertyDefinition.HasDefaultValue)
        propertyDefinition.DefaultValue = this.Visit(this.GetMutableCopy(propertyDefinition.DefaultValue));
      if (propertyDefinition.Getter != null)
        propertyDefinition.Getter = this.Visit(propertyDefinition.Getter);
      propertyDefinition.Parameters = this.Visit(propertyDefinition.Parameters);
      propertyDefinition.ReturnValueAttributes = this.VisitPropertyReturnValueAttributes(propertyDefinition.ReturnValueAttributes);
      if (propertyDefinition.ReturnValueIsModified)
        propertyDefinition.ReturnValueCustomModifiers = this.Visit(propertyDefinition.ReturnValueCustomModifiers);
      if (propertyDefinition.Setter != null)
        propertyDefinition.Setter = this.Visit(propertyDefinition.Setter);
      propertyDefinition.Type = this.Visit(propertyDefinition.Type);
      this.path.Pop();
      return propertyDefinition;
    }

    public virtual PointerTypeReference Visit(PointerTypeReference pointerTypeReference) {
      if (this.stopTraversal) return pointerTypeReference;
      this.Visit((TypeReference)pointerTypeReference);
      this.path.Push(pointerTypeReference);
      pointerTypeReference.TargetType = this.Visit(pointerTypeReference.TargetType);
      this.path.Pop();
      return pointerTypeReference;
    }

    public virtual List<IResourceReference> Visit(List<IResourceReference> resourceReferences) {
      if (this.stopTraversal) return resourceReferences;
      for (int i = 0, n = resourceReferences.Count; i < n; i++)
        resourceReferences[i] = this.Visit(this.GetMutableCopy(resourceReferences[i]));
      return resourceReferences;
    }

    public virtual IResourceReference Visit(ResourceReference resourceReference) {
      if (this.stopTraversal) return resourceReference;
      resourceReference.Attributes = this.Visit(resourceReference.Attributes);
      resourceReference.DefiningAssembly = this.Visit(this.GetMutableCopy(resourceReference.DefiningAssembly));
      return resourceReference;
    }

    public virtual List<ISecurityAttribute> Visit(List<ISecurityAttribute> securityAttributes) {
      if (this.stopTraversal) return securityAttributes;
      for (int i = 0, n = securityAttributes.Count; i < n; i++)
        securityAttributes[i] = this.Visit(this.GetMutableCopy(securityAttributes[i]));
      return securityAttributes;
    }

    public virtual IRootUnitNamespaceReference Visit(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      IRootUnitNamespace/*?*/ rootUnitNamespace = rootUnitNamespaceReference as IRootUnitNamespace;
      if (rootUnitNamespace != null)
        return this.GetMutableCopy(rootUnitNamespace);
      return this.Visit(this.GetMutableCopy(rootUnitNamespaceReference));
    }
    public virtual RootUnitNamespace Visit(RootUnitNamespace rootUnitNamespace) {
      if (this.stopTraversal) return rootUnitNamespace;
      this.Visit((UnitNamespace)rootUnitNamespace);
      return rootUnitNamespace;
    }

    public virtual RootUnitNamespaceReference Visit(RootUnitNamespaceReference rootUnitNamespaceReference) {
      if (this.stopTraversal) return rootUnitNamespaceReference;
      rootUnitNamespaceReference.Unit = this.Visit(rootUnitNamespaceReference.Unit);
      return rootUnitNamespaceReference;
    }

    public virtual SecurityAttribute Visit(SecurityAttribute securityAttribute) {
      if (this.stopTraversal) return securityAttribute;
      this.path.Push(securityAttribute);
      securityAttribute.Attributes = this.Visit(securityAttribute.Attributes);
      this.path.Pop();
      return securityAttribute;
    }

    public virtual ITypeDefinitionMember Visit(TypeDefinitionMember typeDefinitionMember) {
      if (this.stopTraversal) return typeDefinitionMember;
      this.path.Push(typeDefinitionMember);
      typeDefinitionMember.Attributes = this.Visit(typeDefinitionMember.Attributes);
      typeDefinitionMember.ContainingType = this.GetCurrentType();
      typeDefinitionMember.Locations = this.Visit(typeDefinitionMember.Locations);
      this.path.Pop();
      return typeDefinitionMember;
    }

    public virtual TypeReference Visit(TypeReference typeReference) {
      if (this.stopTraversal) return typeReference;
      this.path.Push(typeReference);
      typeReference.Attributes = this.Visit(typeReference.Attributes);
      typeReference.Locations = this.Visit(typeReference.Locations);
      this.path.Pop();
      return typeReference;
    }

    public virtual Unit Visit(Unit unit) {
      if (this.stopTraversal) return unit;
      this.path.Push(unit);
      unit.Attributes = this.Visit(unit.Attributes);
      unit.Locations = this.Visit(unit.Locations);
      unit.UnitNamespaceRoot = this.Visit(this.GetMutableCopy((IRootUnitNamespace)unit.UnitNamespaceRoot));
      this.path.Pop();
      return unit;
    }

    public virtual UnitNamespace Visit(UnitNamespace unitNamespace) {
      if (this.stopTraversal) return unitNamespace;
      this.path.Push(unitNamespace);
      unitNamespace.Attributes = this.Visit(unitNamespace.Attributes);
      unitNamespace.Locations = this.Visit(unitNamespace.Locations);
      unitNamespace.Members = this.Visit(unitNamespace.Members);
      unitNamespace.Unit = this.GetCurrentUnit();
      this.path.Pop();
      return unitNamespace;
    }

    public virtual UnitNamespaceReference Visit(UnitNamespaceReference unitNamespaceReference) {
      if (this.stopTraversal) return unitNamespaceReference;
      this.path.Push(unitNamespaceReference);
      unitNamespaceReference.Attributes = this.Visit(unitNamespaceReference.Attributes);
      unitNamespaceReference.Locations = this.Visit(unitNamespaceReference.Locations);
      this.path.Pop();
      return unitNamespaceReference;
    }

    public virtual VectorTypeReference Visit(VectorTypeReference vectorTypeReference) {
      if (this.stopTraversal) return vectorTypeReference;
      this.Visit((TypeReference)vectorTypeReference);
      this.path.Push(vectorTypeReference);
      vectorTypeReference.ElementType = this.Visit(vectorTypeReference.ElementType);
      this.path.Pop();
      return vectorTypeReference;
    }

    public virtual List<IWin32Resource> Visit(List<IWin32Resource> win32Resources) {
      if (this.stopTraversal) return win32Resources;
      for (int i = 0, n = win32Resources.Count; i < n; i++)
        win32Resources[i] = this.Visit(win32Resources[i]);
      return win32Resources;
    }

    public virtual IWin32Resource Visit(IWin32Resource win32Resource) {
      return win32Resource;
    }

    public virtual List<ICustomAttribute> VisitPropertyReturnValueAttributes(List<ICustomAttribute> customAttributes) {
      return this.Visit(customAttributes);
    }

    public virtual List<ICustomAttribute> VisitMethodReturnValueAttributes(List<ICustomAttribute> customAttributes) {
      return this.Visit(customAttributes);
    }

    public virtual List<ICustomModifier> VisitMethodReturnValueCustomModifiers(List<ICustomModifier> customModifers) {
      return this.Visit(customModifers);
    }

    public virtual IMarshallingInformation VisitMethodReturnValueMarshallingInformation(MarshallingInformation marshallingInformation) {
      return this.Visit(marshallingInformation);
    }

  }
}
