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

  /// <summary>
  /// Implemented by mutable objects that provide a method that makes the mutable object a copy of a given immutable object.
  /// </summary>
  public interface ICopyFrom<ImmutableObject> {
    /// <summary>
    /// Makes this mutable object a copy of the given immutable object.
    /// </summary>
    /// <param name="objectToCopy">An immutable object that implements the same object model interface as this mutable object.</param>
    /// <param name="internFactory">The intern factory to use for computing the interned identity (if applicable) of this mutable object.</param>
    void Copy(ImmutableObject objectToCopy, IInternFactory internFactory);
  }

  /// <summary>
  /// A visitor that produces a mutable copy a given metadata model. Derived classes can override the visit methods
  /// to intervene in the copying process, usually resulting in a copy that is not equivalent to the original model.
  /// For instance, the new copy might have additional types and methods, or additional calls to instrumentation routines.
  /// </summary>
  /// <remarks>While the model is being copied, the resulting model is incomplete and or inconsistent. It should not be traversed
  /// independently nor should any of its computed properties, such as ResolvedType be evaluated. Scenarios that need such functionality
  /// should be implemented by first making a mutable copy of the entire assembly and then running a second pass over the mutable result.</remarks>
  public class MetadataMutator {

    //TODO: perhaps have three classes: MetadataCopier, MetadataMutator and MetadataCopyingMutator?

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    public MetadataMutator(IMetadataHost host) {
      this.host = host;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="host">An object representing the application that is hosting this mutator. It is used to obtain access to some global
    /// objects and services such as the shared name table and the table for interning references.</param>
    /// <param name="copyOnlyIfNotAlreadyMutable">True if the mutator should try and perform mutations in place, rather than mutating new copies.</param>
    public MetadataMutator(IMetadataHost host, bool copyOnlyIfNotAlreadyMutable) {
      this.host = host;
      this.copyOnlyIfNotAlreadyMutable = copyOnlyIfNotAlreadyMutable;
    }

    /// <summary>
    /// Duplicates are cached, both to save space and to detect when the traversal of a cycle should stop.
    /// </summary>
    protected Dictionary<object, object> cache = new Dictionary<object, object>();

    /// <summary>
    /// True if the mutator should try and perform mutations in place, rather than mutating new copies.
    /// </summary>
    protected readonly bool copyOnlyIfNotAlreadyMutable;

    /// <summary>
    ///Since definitions are also references, it can happen that a definition is visited as both a definition and as a reference.
    ///If so, the cache may contain a duplicated definition when a reference is expected, or vice versa.
    ///To prevent this, reference duplicates are always cached separately.
    /// </summary>
    protected Dictionary<object, object> referenceCache = new Dictionary<object, object>();

    /// <summary>
    /// 
    /// </summary>
    protected List<INamedTypeDefinition> flatListOfTypes = new List<INamedTypeDefinition>();

    /// <summary>
    /// 
    /// </summary>
    protected IMetadataHost host;

    /// <summary>
    /// 
    /// </summary>
    protected System.Collections.Stack path = new System.Collections.Stack();

    /// <summary>
    /// 
    /// </summary>
    protected bool stopTraversal;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IMethodDefinition GetCurrentMethod() {
      foreach (object parent in this.path) {
        IMethodDefinition/*?*/ method = parent as IMethodDefinition;
        if (method != null) return method;
      }
      return Dummy.Method;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IUnitNamespace GetCurrentNamespace() {
      foreach (object parent in this.path) {
        IUnitNamespace/*?*/ ns = parent as IUnitNamespace;
        if (ns != null) return ns;
      }
      return Dummy.RootUnitNamespace;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ISignature GetCurrentSignature() {
      foreach (object parent in this.path) {
        ISignature/*?*/ signature = parent as ISignature;
        if (signature != null) return signature;
      }
      return Dummy.Method;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ITypeDefinition GetCurrentType() {
      foreach (object parent in this.path) {
        ITypeDefinition/*?*/ type = parent as ITypeDefinition;
        if (type != null) return type;
      }
      return Dummy.Type;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IUnit GetCurrentUnit() {
      foreach (object parent in this.path) {
        IUnit/*?*/ unit = parent as IUnit;
        if (unit != null) return unit;
      }
      return Dummy.Unit;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public virtual Assembly GetMutableCopy(IAssembly assembly) {
      Assembly/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = assembly as Assembly;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(assembly, out cachedValue);
      result = cachedValue as Assembly;
      if (result != null) return result;
      result = new Assembly();
      this.cache.Add(assembly, result);
      this.cache.Add(result, result);
      result.Copy(assembly, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assemblyReference"></param>
    /// <returns></returns>
    public virtual AssemblyReference GetMutableCopy(IAssemblyReference assemblyReference) {
      AssemblyReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = assemblyReference as AssemblyReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(assemblyReference, out cachedValue);
      result = cachedValue as AssemblyReference;
      if (result != null) return result;
      result = new AssemblyReference();
      this.referenceCache.Add(assemblyReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(assemblyReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customAttribute"></param>
    /// <returns></returns>
    public virtual CustomAttribute GetMutableCopy(ICustomAttribute customAttribute) {
      CustomAttribute result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = customAttribute as CustomAttribute;
        if (result != null) return result;
      }
      result = new CustomAttribute();
      result.Copy(customAttribute, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customModifier"></param>
    /// <returns></returns>
    public virtual CustomModifier GetMutableCopy(ICustomModifier customModifier) {
      CustomModifier result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = customModifier as CustomModifier;
        if (result != null) return result;
      }
      result = new CustomModifier();
      result.Copy(customModifier, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventDefinition"></param>
    /// <returns></returns>
    public virtual EventDefinition GetMutableCopy(IEventDefinition eventDefinition) {
      EventDefinition result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = eventDefinition as EventDefinition;
        if (result != null) return result;
      }
      result = new EventDefinition();
      result.Copy(eventDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fieldDefinition"></param>
    /// <returns></returns>
    public virtual FieldDefinition GetMutableCopy(IFieldDefinition fieldDefinition) {
      FieldDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = fieldDefinition as FieldDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(fieldDefinition, out cachedValue);
      result = cachedValue as FieldDefinition;
      if (result != null) return result;
      result = new FieldDefinition();
      this.cache.Add(fieldDefinition, result);
      this.cache.Add(result, result);
      result.Copy(fieldDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sectionBlock"></param>
    /// <returns></returns>
    public virtual SectionBlock GetMutableCopy(ISectionBlock sectionBlock) {
      SectionBlock/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = sectionBlock as SectionBlock;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(sectionBlock, out cachedValue);
      result = cachedValue as SectionBlock;
      if (result != null) return result;
      result = new SectionBlock();
      this.cache.Add(sectionBlock, result);
      this.cache.Add(result, result);
      result.Copy(sectionBlock, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fieldReference"></param>
    /// <returns></returns>
    public virtual FieldReference GetMutableCopy(IFieldReference fieldReference) {
      FieldReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = fieldReference as FieldReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(fieldReference, out cachedValue);
      result = cachedValue as FieldReference;
      if (result != null) return result;
      result = new FieldReference();
      this.referenceCache.Add(fieldReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(fieldReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileReference"></param>
    /// <returns></returns>
    public virtual FileReference GetMutableCopy(IFileReference fileReference) {
      FileReference result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = fileReference as FileReference;
        if (result != null) return result;
      }
      result = new FileReference();
      result.Copy(fileReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="functionPointerTypeReference"></param>
    /// <returns></returns>
    public virtual FunctionPointerTypeReference GetMutableCopy(IFunctionPointerTypeReference functionPointerTypeReference) {
      FunctionPointerTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = functionPointerTypeReference as FunctionPointerTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(functionPointerTypeReference, out cachedValue);
      result = cachedValue as FunctionPointerTypeReference;
      if (result != null) return result;
      result = new FunctionPointerTypeReference();
      this.referenceCache.Add(functionPointerTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(functionPointerTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethodInstanceReference"></param>
    /// <returns></returns>
    public virtual GenericMethodInstanceReference GetMutableCopy(IGenericMethodInstanceReference genericMethodInstanceReference) {
      GenericMethodInstanceReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = genericMethodInstanceReference as GenericMethodInstanceReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(genericMethodInstanceReference, out cachedValue);
      result = cachedValue as GenericMethodInstanceReference;
      if (result != null) return result;
      result = new GenericMethodInstanceReference();
      this.referenceCache.Add(genericMethodInstanceReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(genericMethodInstanceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethodParameter"></param>
    /// <returns></returns>
    public virtual GenericMethodParameter GetMutableCopy(IGenericMethodParameter genericMethodParameter) {
      GenericMethodParameter/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = genericMethodParameter as GenericMethodParameter;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericMethodParameter, out cachedValue);
      result = cachedValue as GenericMethodParameter;
      if (result != null) return result;
      result = new GenericMethodParameter();
      this.cache.Add(genericMethodParameter, result);
      this.cache.Add(result, result);
      result.Copy(genericMethodParameter, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericMethodParameterReference"></param>
    /// <returns></returns>
    public virtual GenericMethodParameterReference GetMutableCopy(IGenericMethodParameterReference genericMethodParameterReference) {
      GenericMethodParameterReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = genericMethodParameterReference as GenericMethodParameterReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(genericMethodParameterReference, out cachedValue);
      result = cachedValue as GenericMethodParameterReference;
      if (result != null) return result;
      result = new GenericMethodParameterReference();
      this.referenceCache.Add(genericMethodParameterReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(genericMethodParameterReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeInstanceReference"></param>
    /// <returns></returns>
    public virtual GenericTypeInstanceReference GetMutableCopy(IGenericTypeInstanceReference genericTypeInstanceReference) {
      GenericTypeInstanceReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = genericTypeInstanceReference as GenericTypeInstanceReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(genericTypeInstanceReference, out cachedValue);
      result = cachedValue as GenericTypeInstanceReference;
      if (result != null) return result;
      result = new GenericTypeInstanceReference();
      this.referenceCache.Add(genericTypeInstanceReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(genericTypeInstanceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeParameter"></param>
    /// <returns></returns>
    public virtual GenericTypeParameter GetMutableCopy(IGenericTypeParameter genericTypeParameter) {
      GenericTypeParameter/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = genericTypeParameter as GenericTypeParameter;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(genericTypeParameter, out cachedValue);
      result = cachedValue as GenericTypeParameter;
      if (result != null) return result;
      result = new GenericTypeParameter();
      this.cache.Add(genericTypeParameter, result);
      this.cache.Add(result, result);
      result.Copy(genericTypeParameter, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeParameterReference"></param>
    /// <returns></returns>
    public virtual GenericTypeParameterReference GetMutableCopy(IGenericTypeParameterReference genericTypeParameterReference) {
      GenericTypeParameterReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = genericTypeParameterReference as GenericTypeParameterReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(genericTypeParameterReference, out cachedValue);
      result = cachedValue as GenericTypeParameterReference;
      if (result != null) return result;
      result = new GenericTypeParameterReference();
      this.referenceCache.Add(genericTypeParameterReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(genericTypeParameterReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="globalFieldDefinition"></param>
    /// <returns></returns>
    public virtual GlobalFieldDefinition GetMutableCopy(IGlobalFieldDefinition globalFieldDefinition) {
      GlobalFieldDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = globalFieldDefinition as GlobalFieldDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(globalFieldDefinition, out cachedValue);
      result = cachedValue as GlobalFieldDefinition;
      if (result != null) return result;
      result = new GlobalFieldDefinition();
      this.cache.Add(globalFieldDefinition, result);
      this.cache.Add(result, result);
      result.Copy(globalFieldDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="globalMethodDefinition"></param>
    /// <returns></returns>
    public virtual GlobalMethodDefinition GetMutableCopy(IGlobalMethodDefinition globalMethodDefinition) {
      GlobalMethodDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = globalMethodDefinition as GlobalMethodDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(globalMethodDefinition, out cachedValue);
      result = cachedValue as GlobalMethodDefinition;
      if (result != null) return result;
      result = new GlobalMethodDefinition();
      this.cache.Add(globalMethodDefinition, result);
      this.cache.Add(result, result);
      result.Copy(globalMethodDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="localDefinition"></param>
    /// <returns></returns>
    public virtual LocalDefinition GetMutableCopy(ILocalDefinition localDefinition) {
      LocalDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = localDefinition as LocalDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(localDefinition, out cachedValue);
      result = cachedValue as LocalDefinition;
      if (result != null) return result;
      result = new LocalDefinition();
      this.cache.Add(localDefinition, result);
      this.cache.Add(result, result);
      result.Copy(localDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="managedPointerTypeReference"></param>
    /// <returns></returns>
    public virtual ManagedPointerTypeReference GetMutableCopy(IManagedPointerTypeReference managedPointerTypeReference) {
      ManagedPointerTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = managedPointerTypeReference as ManagedPointerTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(managedPointerTypeReference, out cachedValue);
      result = cachedValue as ManagedPointerTypeReference;
      if (result != null) return result;
      result = new ManagedPointerTypeReference();
      this.referenceCache.Add(managedPointerTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(managedPointerTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="marshallingInformation"></param>
    /// <returns></returns>
    public virtual MarshallingInformation GetMutableCopy(IMarshallingInformation marshallingInformation) {
      MarshallingInformation result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = marshallingInformation as MarshallingInformation;
        if (result != null) return result;
      }
      result = new MarshallingInformation();
      result.Copy(marshallingInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataConstant"></param>
    /// <returns></returns>
    public virtual MetadataConstant GetMutableCopy(IMetadataConstant metadataConstant) {
      MetadataConstant result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = metadataConstant as MetadataConstant;
        if (result != null) return result;
      }
      result = new MetadataConstant();
      result.Copy(metadataConstant, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataCreateArray"></param>
    /// <returns></returns>
    public virtual MetadataCreateArray GetMutableCopy(IMetadataCreateArray metadataCreateArray) {
      MetadataCreateArray result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = metadataCreateArray as MetadataCreateArray;
        if (result != null) return result;
      }
      result = new MetadataCreateArray();
      result.Copy(metadataCreateArray, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataNamedArgument"></param>
    /// <returns></returns>
    public virtual MetadataNamedArgument GetMutableCopy(IMetadataNamedArgument metadataNamedArgument) {
      MetadataNamedArgument result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = metadataNamedArgument as MetadataNamedArgument;
        if (result != null) return result;
      }
      result = new MetadataNamedArgument();
      result.Copy(metadataNamedArgument, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="metadataTypeOf"></param>
    /// <returns></returns>
    public virtual MetadataTypeOf GetMutableCopy(IMetadataTypeOf metadataTypeOf) {
      MetadataTypeOf result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = metadataTypeOf as MetadataTypeOf;
        if (result != null) return result;
      }
      result = new MetadataTypeOf();
      result.Copy(metadataTypeOf, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <returns></returns>
    public virtual MethodDefinition GetMutableCopy(IMethodDefinition methodDefinition) {
      MethodDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = methodDefinition as MethodDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(methodDefinition, out cachedValue);
      result = cachedValue as MethodDefinition;
      if (result != null) return result;
      result = new MethodDefinition();
      this.cache.Add(methodDefinition, result);
      this.cache.Add(result, result);
      result.Copy(methodDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodBody"></param>
    /// <returns></returns>
    public virtual MethodBody GetMutableCopy(IMethodBody methodBody) {
      MethodBody result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = methodBody as MethodBody;
        if (result != null) return result;
      }
      result = new MethodBody();
      result.Copy(methodBody, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodImplementation"></param>
    /// <returns></returns>
    public virtual MethodImplementation GetMutableCopy(IMethodImplementation methodImplementation) {
      MethodImplementation result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = methodImplementation as MethodImplementation;
        if (result != null) return result;
      }
      result = new MethodImplementation();
      result.Copy(methodImplementation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodReference"></param>
    /// <returns></returns>
    public virtual MethodReference GetMutableCopy(IMethodReference methodReference) {
      MethodReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = methodReference as MethodReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(methodReference, out cachedValue);
      result = cachedValue as MethodReference;
      if (result != null) return result;
      result = new MethodReference();
      this.referenceCache.Add(methodReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(methodReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modifiedTypeReference"></param>
    /// <returns></returns>
    public virtual ModifiedTypeReference GetMutableCopy(IModifiedTypeReference modifiedTypeReference) {
      ModifiedTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = modifiedTypeReference as ModifiedTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(modifiedTypeReference, out cachedValue);
      result = cachedValue as ModifiedTypeReference;
      if (result != null) return result;
      result = new ModifiedTypeReference();
      this.referenceCache.Add(modifiedTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(modifiedTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    public virtual Module GetMutableCopy(IModule module) {
      Module/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = module as Module;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(module, out cachedValue);
      result = cachedValue as Module;
      if (result != null) return result;
      result = new Module();
      this.cache.Add(module, result);
      this.cache.Add(result, result);
      result.Copy(module, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="moduleReference"></param>
    /// <returns></returns>
    public virtual ModuleReference GetMutableCopy(IModuleReference moduleReference) {
      ModuleReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = moduleReference as ModuleReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(moduleReference, out cachedValue);
      result = cachedValue as ModuleReference;
      if (result != null) return result;
      result = new ModuleReference();
      this.referenceCache.Add(moduleReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(moduleReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceAliasForType"></param>
    /// <returns></returns>
    public virtual NamespaceAliasForType GetMutableCopy(INamespaceAliasForType namespaceAliasForType) {
      NamespaceAliasForType result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = namespaceAliasForType as NamespaceAliasForType;
        if (result != null) return result;
      }
      result = new NamespaceAliasForType();
      result.Copy(namespaceAliasForType, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceTypeDefinition"></param>
    /// <returns></returns>
    public virtual NamespaceTypeDefinition GetMutableCopy(INamespaceTypeDefinition namespaceTypeDefinition) {
      NamespaceTypeDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = namespaceTypeDefinition as NamespaceTypeDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(namespaceTypeDefinition, out cachedValue);
      result = cachedValue as NamespaceTypeDefinition;
      if (result != null) return result;
      result = new NamespaceTypeDefinition();
      this.cache.Add(namespaceTypeDefinition, result);
      this.cache.Add(result, result);
      result.Copy(namespaceTypeDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="namespaceTypeReference"></param>
    /// <returns></returns>
    public virtual NamespaceTypeReference GetMutableCopy(INamespaceTypeReference namespaceTypeReference) {
      NamespaceTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = namespaceTypeReference as NamespaceTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(namespaceTypeReference, out cachedValue);
      result = cachedValue as NamespaceTypeReference;
      if (result != null) return result;
      result = new NamespaceTypeReference();
      this.referenceCache.Add(namespaceTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(namespaceTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedAliasForType"></param>
    /// <returns></returns>
    public virtual NestedAliasForType GetMutableCopy(INestedAliasForType nestedAliasForType) {
      NestedAliasForType result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = nestedAliasForType as NestedAliasForType;
        if (result != null) return result;
      }
      result = new NestedAliasForType();
      result.Copy(nestedAliasForType, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedTypeDefinition"></param>
    /// <returns></returns>
    public virtual NestedTypeDefinition GetMutableCopy(INestedTypeDefinition nestedTypeDefinition) {
      NestedTypeDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = nestedTypeDefinition as NestedTypeDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(nestedTypeDefinition, out cachedValue);
      result = cachedValue as NestedTypeDefinition;
      if (result != null) return result;
      result = new NestedTypeDefinition();
      this.cache.Add(nestedTypeDefinition, result);
      this.cache.Add(result, result);
      result.Copy(nestedTypeDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedTypeReference"></param>
    /// <returns></returns>
    public virtual NestedTypeReference GetMutableCopy(INestedTypeReference nestedTypeReference) {
      NestedTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = nestedTypeReference as NestedTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(nestedTypeReference, out cachedValue);
      result = cachedValue as NestedTypeReference;
      if (result != null) return result;
      result = new NestedTypeReference();
      this.referenceCache.Add(nestedTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(nestedTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedUnitNamespace"></param>
    /// <returns></returns>
    public virtual NestedUnitNamespace GetMutableCopy(INestedUnitNamespace nestedUnitNamespace) {
      NestedUnitNamespace/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = nestedUnitNamespace as NestedUnitNamespace;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(nestedUnitNamespace, out cachedValue);
      result = cachedValue as NestedUnitNamespace;
      if (result != null) return result;
      result = new NestedUnitNamespace();
      this.cache.Add(nestedUnitNamespace, result);
      this.cache.Add(result, result);
      result.Copy(nestedUnitNamespace, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="nestedUnitNamespaceReference"></param>
    /// <returns></returns>
    public virtual NestedUnitNamespaceReference GetMutableCopy(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      NestedUnitNamespaceReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = nestedUnitNamespaceReference as NestedUnitNamespaceReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(nestedUnitNamespaceReference, out cachedValue);
      result = cachedValue as NestedUnitNamespaceReference;
      if (result != null) return result;
      result = new NestedUnitNamespaceReference();
      this.referenceCache.Add(nestedUnitNamespaceReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(nestedUnitNamespaceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operation"></param>
    /// <returns></returns>
    public virtual Operation GetMutableCopy(IOperation operation) {
      Operation result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = operation as Operation;
        if (result != null) return result;
      }
      result = new Operation();
      result.Copy(operation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="operationExceptionInformation"></param>
    /// <returns></returns>
    public virtual OperationExceptionInformation GetMutableCopy(IOperationExceptionInformation operationExceptionInformation) {
      OperationExceptionInformation result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = operationExceptionInformation as OperationExceptionInformation;
        if (result != null) return result;
      }
      result = new OperationExceptionInformation();
      result.Copy(operationExceptionInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameterDefinition"></param>
    /// <returns></returns>
    public virtual ParameterDefinition GetMutableCopy(IParameterDefinition parameterDefinition) {
      ParameterDefinition/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = parameterDefinition as ParameterDefinition;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(parameterDefinition, out cachedValue);
      result = cachedValue as ParameterDefinition;
      if (result != null) return result;
      result = new ParameterDefinition();
      this.cache.Add(parameterDefinition, result);
      this.cache.Add(result, result);
      result.Copy(parameterDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameterTypeInformation"></param>
    /// <returns></returns>
    public virtual ParameterTypeInformation GetMutableCopy(IParameterTypeInformation parameterTypeInformation) {
      ParameterTypeInformation result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = parameterTypeInformation as ParameterTypeInformation;
        if (result != null) return result;
      }
      result = new ParameterTypeInformation();
      result.Copy(parameterTypeInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="platformInvokeInformation"></param>
    /// <returns></returns>
    public virtual PlatformInvokeInformation GetMutableCopy(IPlatformInvokeInformation platformInvokeInformation) {
      PlatformInvokeInformation result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = platformInvokeInformation as PlatformInvokeInformation;
        if (result != null) return result;
      }
      result = new PlatformInvokeInformation();
      result.Copy(platformInvokeInformation, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pointerTypeReference"></param>
    /// <returns></returns>
    public virtual PointerTypeReference GetMutableCopy(IPointerTypeReference pointerTypeReference) {
      PointerTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = pointerTypeReference as PointerTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(pointerTypeReference, out cachedValue);
      result = cachedValue as PointerTypeReference;
      if (result != null) return result;
      result = new PointerTypeReference();
      this.referenceCache.Add(pointerTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(pointerTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyDefinition"></param>
    /// <returns></returns>
    public virtual PropertyDefinition GetMutableCopy(IPropertyDefinition propertyDefinition) {
      PropertyDefinition result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = propertyDefinition as PropertyDefinition;
        if (result != null) return result;
      }
      result = new PropertyDefinition();
      result.Copy(propertyDefinition, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceReference"></param>
    /// <returns></returns>
    public virtual ResourceReference GetMutableCopy(IResourceReference resourceReference) {
      ResourceReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = resourceReference as ResourceReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(resourceReference, out cachedValue);
      result = cachedValue as ResourceReference;
      if (result != null) return result;
      result = new ResourceReference();
      this.referenceCache.Add(resourceReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(resourceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootUnitNamespace"></param>
    /// <returns></returns>
    public virtual RootUnitNamespace GetMutableCopy(IRootUnitNamespace rootUnitNamespace) {
      RootUnitNamespace/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = rootUnitNamespace as RootUnitNamespace;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.cache.TryGetValue(rootUnitNamespace, out cachedValue);
      result = cachedValue as RootUnitNamespace;
      if (result != null) return result;
      result = new RootUnitNamespace();
      this.cache.Add(rootUnitNamespace, result);
      this.cache.Add(result, result);
      result.Copy(rootUnitNamespace, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="rootUnitNamespaceReference"></param>
    /// <returns></returns>
    public virtual RootUnitNamespaceReference GetMutableCopy(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      RootUnitNamespaceReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = rootUnitNamespaceReference as RootUnitNamespaceReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(rootUnitNamespaceReference, out cachedValue);
      result = cachedValue as RootUnitNamespaceReference;
      if (result != null) return result;
      result = new RootUnitNamespaceReference();
      this.referenceCache.Add(rootUnitNamespaceReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(rootUnitNamespaceReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="securityAttribute"></param>
    /// <returns></returns>
    public virtual SecurityAttribute GetMutableCopy(ISecurityAttribute securityAttribute) {
      SecurityAttribute result = null;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = securityAttribute as SecurityAttribute;
        if (result != null) return result;
      }
      result = new SecurityAttribute();
      result.Copy(securityAttribute, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="specializedFieldReference"></param>
    /// <returns></returns>
    public virtual SpecializedFieldReference GetMutableCopy(ISpecializedFieldReference specializedFieldReference) {
      SpecializedFieldReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = specializedFieldReference as SpecializedFieldReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(specializedFieldReference, out cachedValue);
      result = cachedValue as SpecializedFieldReference;
      if (result != null) return result;
      result = new SpecializedFieldReference();
      this.referenceCache.Add(specializedFieldReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(specializedFieldReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="specializedMethodReference"></param>
    /// <returns></returns>
    public virtual SpecializedMethodReference GetMutableCopy(ISpecializedMethodReference specializedMethodReference) {
      SpecializedMethodReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = specializedMethodReference as SpecializedMethodReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(specializedMethodReference, out cachedValue);
      result = cachedValue as SpecializedMethodReference;
      if (result != null) return result;
      result = new SpecializedMethodReference();
      this.referenceCache.Add(specializedMethodReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(specializedMethodReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="specializedNestedTypeReference"></param>
    /// <returns></returns>
    public virtual SpecializedNestedTypeReference GetMutableCopy(ISpecializedNestedTypeReference specializedNestedTypeReference) {
      SpecializedNestedTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = specializedNestedTypeReference as SpecializedNestedTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(specializedNestedTypeReference, out cachedValue);
      result = cachedValue as SpecializedNestedTypeReference;
      if (result != null) return result;
      result = new SpecializedNestedTypeReference();
      this.referenceCache.Add(specializedNestedTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(specializedNestedTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="matrixTypeReference"></param>
    /// <returns></returns>
    public virtual MatrixTypeReference GetMutableMatrixCopy(IArrayTypeReference matrixTypeReference) {
      MatrixTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = matrixTypeReference as MatrixTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(matrixTypeReference, out cachedValue);
      result = cachedValue as MatrixTypeReference;
      if (result != null) return result;
      result = new MatrixTypeReference();
      this.referenceCache.Add(matrixTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(matrixTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vectorTypeReference"></param>
    /// <returns></returns>
    public virtual VectorTypeReference GetMutableVectorCopy(IArrayTypeReference vectorTypeReference) {
      VectorTypeReference/*?*/ result;
      if (this.copyOnlyIfNotAlreadyMutable) {
        result = vectorTypeReference as VectorTypeReference;
        if (result != null) return result;
      }
      object/*?*/ cachedValue = null;
      this.referenceCache.TryGetValue(vectorTypeReference, out cachedValue);
      result = cachedValue as VectorTypeReference;
      if (result != null) return result;
      result = new VectorTypeReference();
      this.referenceCache.Add(vectorTypeReference, result);
      this.referenceCache.Add(result, result);
      result.Copy(vectorTypeReference, this.host.InternFactory);
      return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodReference"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified aliases for types.
    /// </summary>
    /// <param name="aliasesForTypes">The aliases for types.</param>
    /// <returns></returns>
    public virtual List<IAliasForType> Visit(List<IAliasForType> aliasesForTypes) {
      if (this.stopTraversal) return aliasesForTypes;
      for (int i = 0, n = aliasesForTypes.Count; i < n; i++)
        aliasesForTypes[i] = this.Visit(aliasesForTypes[i]);
      return aliasesForTypes;
    }


    /// <summary>
    /// Visits the specified alias for type.
    /// </summary>
    /// <param name="aliasForType">Type of the alias for.</param>
    /// <returns></returns>
    public virtual IAliasForType Visit(IAliasForType aliasForType) {
      if (this.stopTraversal) return aliasForType;
      INamespaceAliasForType/*?*/ namespaceAliasForType = aliasForType as INamespaceAliasForType;
      if (namespaceAliasForType != null) return this.Visit(this.GetMutableCopy(namespaceAliasForType));
      INestedAliasForType/*?*/ nestedAliasForType = aliasForType as INestedAliasForType;
      if (nestedAliasForType != null) return this.Visit(this.GetMutableCopy(nestedAliasForType));
      //TODO: error
      return aliasForType;
    }

    /// <summary>
    /// Makes a (shallow) mutable copy of the given assembly and then visits the copy, which generally
    /// results in a deep mutable copy, depending on how subclasses override the behavior of the methods
    /// of this base class.
    /// </summary>
    /// <param name="assembly">The assembly to copy.</param>
    public virtual Assembly Visit(IAssembly assembly) {
      return this.Visit(this.GetMutableCopy(assembly));
    }

    /// <summary>
    /// Visits the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified assembly references.
    /// </summary>
    /// <param name="assemblyReferences">The assembly references.</param>
    /// <returns></returns>
    public virtual List<IAssemblyReference> Visit(List<IAssemblyReference> assemblyReferences) {
      if (this.stopTraversal) return assemblyReferences;
      for (int i = 0, n = assemblyReferences.Count; i < n; i++)
        assemblyReferences[i] = this.Visit(assemblyReferences[i]);
      return assemblyReferences;
    }

    /// <summary>
    /// Visits the specified assembly reference.
    /// </summary>
    /// <param name="assemblyReference">The assembly reference.</param>
    /// <returns></returns>
    public virtual IAssemblyReference Visit(IAssemblyReference assemblyReference) {
      return this.Visit(this.GetMutableCopy(assemblyReference));
    }

    /// <summary>
    /// Visits the specified assembly reference.
    /// </summary>
    /// <param name="assemblyReference">The assembly reference.</param>
    /// <returns></returns>
    public virtual AssemblyReference Visit(AssemblyReference assemblyReference) {
      if (assemblyReference.ResolvedAssembly != Dummy.Assembly) {
        object/*?*/ mutatedResolvedAssembly = null;
        if (this.cache.TryGetValue(assemblyReference.ResolvedAssembly, out mutatedResolvedAssembly))
          assemblyReference.ResolvedAssembly = (IAssembly)mutatedResolvedAssembly;
      }
      return assemblyReference;
    }

    /// <summary>
    /// Visits the specified custom attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    /// <returns></returns>
    public virtual List<ICustomAttribute> Visit(List<ICustomAttribute> customAttributes) {
      if (this.stopTraversal) return customAttributes;
      for (int i = 0, n = customAttributes.Count; i < n; i++)
        customAttributes[i] = this.Visit(this.GetMutableCopy(customAttributes[i]));
      return customAttributes;
    }

    /// <summary>
    /// Visits the specified custom attribute.
    /// </summary>
    /// <param name="customAttribute">The custom attribute.</param>
    /// <returns></returns>
    public virtual CustomAttribute Visit(CustomAttribute customAttribute) {
      if (this.stopTraversal) return customAttribute;
      this.path.Push(customAttribute);
      customAttribute.Arguments = this.Visit(customAttribute.Arguments);
      customAttribute.Constructor = this.Visit(customAttribute.Constructor);
      customAttribute.NamedArguments = this.Visit(customAttribute.NamedArguments);
      this.path.Pop();
      return customAttribute;
    }

    /// <summary>
    /// Visits the specified custom modifiers.
    /// </summary>
    /// <param name="customModifiers">The custom modifiers.</param>
    /// <returns></returns>
    public virtual List<ICustomModifier> Visit(List<ICustomModifier> customModifiers) {
      if (this.stopTraversal) return customModifiers;
      for (int i = 0, n = customModifiers.Count; i < n; i++)
        customModifiers[i] = this.Visit(this.GetMutableCopy(customModifiers[i]));
      return customModifiers;
    }

    /// <summary>
    /// Visits the specified custom modifier.
    /// </summary>
    /// <param name="customModifier">The custom modifier.</param>
    /// <returns></returns>
    public virtual CustomModifier Visit(CustomModifier customModifier) {
      if (this.stopTraversal) return customModifier;
      this.path.Push(customModifier);
      customModifier.Modifier = this.Visit(customModifier.Modifier);
      this.path.Pop();
      return customModifier;
    }

    /// <summary>
    /// Visits the specified event definitions.
    /// </summary>
    /// <param name="eventDefinitions">The event definitions.</param>
    /// <returns></returns>
    public virtual List<IEventDefinition> Visit(List<IEventDefinition> eventDefinitions) {
      if (this.stopTraversal) return eventDefinitions;
      for (int i = 0, n = eventDefinitions.Count; i < n; i++)
        eventDefinitions[i] = this.Visit(eventDefinitions[i]);
      return eventDefinitions;
    }

    /// <summary>
    /// Visits the specified event definition.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <returns></returns>
    public virtual IEventDefinition Visit(IEventDefinition eventDefinition) {
      return this.Visit(this.GetMutableCopy(eventDefinition));
    }

    /// <summary>
    /// Visits the specified event definition.
    /// </summary>
    /// <param name="eventDefinition">The event definition.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified field definitions.
    /// </summary>
    /// <param name="fieldDefinitions">The field definitions.</param>
    /// <returns></returns>
    public virtual List<IFieldDefinition> Visit(List<IFieldDefinition> fieldDefinitions) {
      if (this.stopTraversal) return fieldDefinitions;
      for (int i = 0, n = fieldDefinitions.Count; i < n; i++)
        fieldDefinitions[i] = this.Visit(fieldDefinitions[i]);
      return fieldDefinitions;
    }

    /// <summary>
    /// Visits the specified field definition.
    /// </summary>
    /// <param name="fieldDefinition">The field definition.</param>
    /// <returns></returns>
    public virtual FieldDefinition Visit(FieldDefinition fieldDefinition) {
      if (this.stopTraversal) return fieldDefinition;
      this.Visit((TypeDefinitionMember)fieldDefinition);
      this.path.Push(fieldDefinition);
      if (fieldDefinition.IsCompileTimeConstant)
        fieldDefinition.CompileTimeValue = this.Visit(this.GetMutableCopy(fieldDefinition.CompileTimeValue));
      if (fieldDefinition.IsMapped)
        fieldDefinition.FieldMapping = this.Visit(this.GetMutableCopy(fieldDefinition.FieldMapping));
      if (fieldDefinition.IsMarshalledExplicitly)
        fieldDefinition.MarshallingInformation = this.Visit(this.GetMutableCopy(fieldDefinition.MarshallingInformation));
      fieldDefinition.Type = this.Visit(fieldDefinition.Type);
      this.path.Pop();
      return fieldDefinition;
    }

    /// <summary>
    /// Visits the specified field definition.
    /// </summary>
    /// <param name="fieldDefinition">The field definition.</param>
    /// <returns></returns>
    public virtual IFieldDefinition Visit(IFieldDefinition fieldDefinition) {
      return this.Visit(this.GetMutableCopy(fieldDefinition));
    }

    /// <summary>
    /// Visits the specified field reference.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified field reference.
    /// </summary>
    /// <param name="fieldReference">The field reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified file references.
    /// </summary>
    /// <param name="fileReferences">The file references.</param>
    /// <returns></returns>
    public virtual List<IFileReference> Visit(List<IFileReference> fileReferences) {
      if (this.stopTraversal) return fileReferences;
      for (int i = 0, n = fileReferences.Count; i < n; i++)
        fileReferences[i] = this.Visit(this.GetMutableCopy(fileReferences[i]));
      return fileReferences;
    }

    /// <summary>
    /// Visits the specified file reference.
    /// </summary>
    /// <param name="fileReference">The file reference.</param>
    /// <returns></returns>
    public virtual FileReference Visit(FileReference fileReference) {
      return fileReference;
    }

    /// <summary>
    /// Visits the specified function pointer type reference.
    /// </summary>
    /// <param name="functionPointerTypeReference">The function pointer type reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified generic method instance reference.
    /// </summary>
    /// <param name="genericMethodInstanceReference">The generic method instance reference.</param>
    /// <returns></returns>
    public virtual GenericMethodInstanceReference Visit(GenericMethodInstanceReference genericMethodInstanceReference) {
      if (this.stopTraversal) return genericMethodInstanceReference;
      this.Visit((MethodReference)genericMethodInstanceReference);
      this.path.Push(genericMethodInstanceReference);
      genericMethodInstanceReference.GenericArguments = this.Visit(genericMethodInstanceReference.GenericArguments);
      genericMethodInstanceReference.GenericMethod = this.Visit(genericMethodInstanceReference.GenericMethod);
      this.path.Pop();
      return genericMethodInstanceReference;
    }

    /// <summary>
    /// Visits the specified generic method parameters.
    /// </summary>
    /// <param name="genericMethodParameters">The generic method parameters.</param>
    /// <param name="declaringMethod">The declaring method.</param>
    /// <returns></returns>
    public virtual List<IGenericMethodParameter> Visit(List<IGenericMethodParameter> genericMethodParameters, IMethodDefinition declaringMethod) {
      if (this.stopTraversal) return genericMethodParameters;
      for (int i = 0, n = genericMethodParameters.Count; i < n; i++)
        genericMethodParameters[i] = this.Visit(this.GetMutableCopy(genericMethodParameters[i]));
      return genericMethodParameters;
    }

    /// <summary>
    /// Visits the specified generic method parameter.
    /// </summary>
    /// <param name="genericMethodParameter">The generic method parameter.</param>
    /// <returns></returns>
    public virtual GenericMethodParameter Visit(GenericMethodParameter genericMethodParameter) {
      if (this.stopTraversal) return genericMethodParameter;
      this.Visit((GenericParameter)genericMethodParameter);
      genericMethodParameter.DefiningMethod = this.GetCurrentMethod();
      return genericMethodParameter;
    }

    /// <summary>
    /// Visits the specified generic method parameter reference.
    /// </summary>
    /// <param name="genericMethodParameterReference">The generic method parameter reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified generic type parameter reference.
    /// </summary>
    /// <param name="genericTypeParameterReference">The generic type parameter reference.</param>
    /// <returns></returns>
    public virtual GenericTypeParameterReference Visit(GenericTypeParameterReference genericTypeParameterReference) {
      if (this.stopTraversal) return genericTypeParameterReference;
      this.Visit((TypeReference)genericTypeParameterReference);
      this.path.Push(genericTypeParameterReference);
      genericTypeParameterReference.DefiningType = this.Visit(genericTypeParameterReference.DefiningType);
      this.path.Pop();
      return genericTypeParameterReference;
    }

    /// <summary>
    /// Visits the specified global field definition.
    /// </summary>
    /// <param name="globalFieldDefinition">The global field definition.</param>
    /// <returns></returns>
    public virtual GlobalFieldDefinition Visit(GlobalFieldDefinition globalFieldDefinition) {
      if (this.stopTraversal) return globalFieldDefinition;
      this.path.Push(this.Visit(globalFieldDefinition.ContainingType));
      this.Visit((FieldDefinition)globalFieldDefinition);
      this.path.Pop();
      globalFieldDefinition.ContainingNamespace = this.GetCurrentNamespace();
      return globalFieldDefinition;
    }

    /// <summary>
    /// Visits the specified global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition">The global method definition.</param>
    /// <returns></returns>
    public virtual GlobalMethodDefinition Visit(GlobalMethodDefinition globalMethodDefinition) {
      if (this.stopTraversal) return globalMethodDefinition;
      this.path.Push(this.Visit(globalMethodDefinition.ContainingType));
      this.Visit((MethodDefinition)globalMethodDefinition);
      this.path.Pop();
      globalMethodDefinition.ContainingNamespace = this.GetCurrentNamespace();
      return globalMethodDefinition;
    }

    /// <summary>
    /// Visits the specified generic type instance reference.
    /// </summary>
    /// <param name="genericTypeInstanceReference">The generic type instance reference.</param>
    /// <returns></returns>
    public virtual GenericTypeInstanceReference Visit(GenericTypeInstanceReference genericTypeInstanceReference) {
      if (this.stopTraversal) return genericTypeInstanceReference;
      this.Visit((TypeReference)genericTypeInstanceReference);
      this.path.Push(genericTypeInstanceReference);
      genericTypeInstanceReference.GenericArguments = this.Visit(genericTypeInstanceReference.GenericArguments);
      genericTypeInstanceReference.GenericType = this.Visit(genericTypeInstanceReference.GenericType);
      this.path.Pop();
      return genericTypeInstanceReference;
    }

    /// <summary>
    /// Visits the specified generic parameter.
    /// </summary>
    /// <param name="genericParameter">The generic parameter.</param>
    /// <returns></returns>
    public virtual GenericParameter Visit(GenericParameter genericParameter) {
      if (this.stopTraversal) return genericParameter;
      this.path.Push(genericParameter);
      genericParameter.Attributes = this.Visit(genericParameter.Attributes);
      genericParameter.Constraints = this.Visit(genericParameter.Constraints);
      this.path.Pop();
      return genericParameter;
    }

    /// <summary>
    /// Visits the specified generic type parameters.
    /// </summary>
    /// <param name="genericTypeParameters">The generic type parameters.</param>
    /// <returns></returns>
    public virtual List<IGenericTypeParameter> Visit(List<IGenericTypeParameter> genericTypeParameters) {
      if (this.stopTraversal) return genericTypeParameters;
      for (int i = 0, n = genericTypeParameters.Count; i < n; i++)
        genericTypeParameters[i] = this.Visit(this.GetMutableCopy(genericTypeParameters[i]));
      return genericTypeParameters;
    }

    /// <summary>
    /// Visits the specified generic type parameter.
    /// </summary>
    /// <param name="genericTypeParameter">The generic type parameter.</param>
    /// <returns></returns>
    public virtual GenericTypeParameter Visit(GenericTypeParameter genericTypeParameter) {
      if (this.stopTraversal) return genericTypeParameter;
      this.Visit((GenericParameter)genericTypeParameter);
      genericTypeParameter.DefiningType = this.GetCurrentType();
      return genericTypeParameter;
    }

    /// <summary>
    /// Visits the specified locations.
    /// </summary>
    /// <param name="locations">The locations.</param>
    /// <returns></returns>
    public virtual List<ILocation> Visit(List<ILocation> locations) {
      if (this.stopTraversal) return locations;
      for (int i = 0, n = locations.Count; i < n; i++)
        locations[i] = this.Visit(locations[i]);
      return locations;
    }

    /// <summary>
    /// Visits the specified location.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <returns></returns>
    public virtual ILocation Visit(ILocation location) {
      return location;
    }

    /// <summary>
    /// Visits the specified local definition.
    /// </summary>
    /// <param name="localDefinition">The local definition.</param>
    /// <returns></returns>
    public virtual LocalDefinition Visit(LocalDefinition localDefinition) {
      if (this.stopTraversal) return localDefinition;
      this.path.Push(localDefinition);
      localDefinition.CustomModifiers = this.Visit(localDefinition.CustomModifiers);
      localDefinition.Type = this.Visit(localDefinition.Type);
      this.path.Pop();
      return localDefinition;
    }

    /// <summary>
    /// Visits the specified managed pointer type reference.
    /// </summary>
    /// <param name="managedPointerTypeReference">The pointer type reference.</param>
    /// <returns></returns>
    public virtual ManagedPointerTypeReference Visit(ManagedPointerTypeReference managedPointerTypeReference) {
      if (this.stopTraversal) return managedPointerTypeReference;
      this.Visit((TypeReference)managedPointerTypeReference);
      this.path.Push(managedPointerTypeReference);
      managedPointerTypeReference.TargetType = this.Visit(managedPointerTypeReference.TargetType);
      this.path.Pop();
      return managedPointerTypeReference;
    }

    /// <summary>
    /// Visits the specified marshalling information.
    /// </summary>
    /// <param name="marshallingInformation">The marshalling information.</param>
    /// <returns></returns>
    public virtual MarshallingInformation Visit(MarshallingInformation marshallingInformation) {
      if (this.stopTraversal) return marshallingInformation;
      this.path.Push(marshallingInformation);
      if (marshallingInformation.UnmanagedType == UnmanagedType.CustomMarshaler)
        marshallingInformation.CustomMarshaller = this.Visit(marshallingInformation.CustomMarshaller);
      if (marshallingInformation.UnmanagedType == UnmanagedType.SafeArray && 
      (marshallingInformation.SafeArrayElementSubtype == VarEnum.VT_DISPATCH || 
      marshallingInformation.SafeArrayElementSubtype == VarEnum.VT_UNKNOWN || 
      marshallingInformation.SafeArrayElementSubtype == VarEnum.VT_RECORD))
        marshallingInformation.SafeArrayElementUserDefinedSubtype = this.Visit(marshallingInformation.SafeArrayElementUserDefinedSubtype);
      this.path.Pop();
      return marshallingInformation;
    }

    /// <summary>
    /// Visits the specified constant.
    /// </summary>
    /// <param name="constant">The constant.</param>
    /// <returns></returns>
    public virtual MetadataConstant Visit(MetadataConstant constant) {
      if (this.stopTraversal) return constant;
      this.path.Push(constant);
      constant.Locations = this.Visit(constant.Locations);
      constant.Type = this.Visit(constant.Type);
      this.path.Pop();
      return constant;
    }

    /// <summary>
    /// Visits the specified create array.
    /// </summary>
    /// <param name="createArray">The create array.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified metadata expressions.
    /// </summary>
    /// <param name="metadataExpressions">The metadata expressions.</param>
    /// <returns></returns>
    public virtual List<IMetadataExpression> Visit(List<IMetadataExpression> metadataExpressions) {
      if (this.stopTraversal) return metadataExpressions;
      for (int i = 0, n = metadataExpressions.Count; i < n; i++)
        metadataExpressions[i] = this.Visit(metadataExpressions[i]);
      return metadataExpressions;
    }

    /// <summary>
    /// Visits the specified expression.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified named arguments.
    /// </summary>
    /// <param name="namedArguments">The named arguments.</param>
    /// <returns></returns>
    public virtual List<IMetadataNamedArgument> Visit(List<IMetadataNamedArgument> namedArguments) {
      if (this.stopTraversal) return namedArguments;
      for (int i = 0, n = namedArguments.Count; i < n; i++)
        namedArguments[i] = this.Visit(this.GetMutableCopy(namedArguments[i]));
      return namedArguments;
    }

    /// <summary>
    /// Visits the specified named argument.
    /// </summary>
    /// <param name="namedArgument">The named argument.</param>
    /// <returns></returns>
    public virtual MetadataNamedArgument Visit(MetadataNamedArgument namedArgument) {
      if (this.stopTraversal) return namedArgument;
      this.path.Push(namedArgument);
      namedArgument.ArgumentValue = this.Visit(namedArgument.ArgumentValue);
      namedArgument.Locations = this.Visit(namedArgument.Locations);
      namedArgument.Type = this.Visit(namedArgument.Type);
      this.path.Pop();
      return namedArgument;
    }

    /// <summary>
    /// Visits the specified type of.
    /// </summary>
    /// <param name="typeOf">The type of.</param>
    /// <returns></returns>
    public virtual MetadataTypeOf Visit(MetadataTypeOf typeOf) {
      if (this.stopTraversal) return typeOf;
      this.path.Push(typeOf);
      typeOf.Locations = this.Visit(typeOf.Locations);
      typeOf.Type = this.Visit(typeOf.Type);
      typeOf.TypeToGet = this.Visit(typeOf.TypeToGet);
      this.path.Pop();
      return typeOf;
    }

    /// <summary>
    /// Visits the specified matrix type reference.
    /// </summary>
    /// <param name="matrixTypeReference">The matrix type reference.</param>
    /// <returns></returns>
    public virtual MatrixTypeReference Visit(MatrixTypeReference matrixTypeReference) {
      if (this.stopTraversal) return matrixTypeReference;
      this.Visit((TypeReference)matrixTypeReference);
      this.path.Push(matrixTypeReference);
      matrixTypeReference.ElementType = this.Visit(matrixTypeReference.ElementType);
      this.path.Pop();
      return matrixTypeReference;
    }

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified exception informations.
    /// </summary>
    /// <param name="exceptionInformations">The exception informations.</param>
    /// <returns></returns>
    public virtual List<IOperationExceptionInformation> Visit(List<IOperationExceptionInformation> exceptionInformations) {
      if (this.stopTraversal) return exceptionInformations;
      for (int i = 0, n = exceptionInformations.Count; i < n; i++)
        exceptionInformations[i] = this.Visit(this.GetMutableCopy(exceptionInformations[i]));
      return exceptionInformations;
    }

    /// <summary>
    /// Visits the specified operation exception information.
    /// </summary>
    /// <param name="operationExceptionInformation">The operation exception information.</param>
    /// <returns></returns>
    public virtual OperationExceptionInformation Visit(OperationExceptionInformation operationExceptionInformation) {
      if (this.stopTraversal) return operationExceptionInformation;
      this.path.Push(operationExceptionInformation);
      operationExceptionInformation.ExceptionType = this.Visit(operationExceptionInformation.ExceptionType);
      this.path.Pop();
      return operationExceptionInformation;
    }

    /// <summary>
    /// Visits the specified operations.
    /// </summary>
    /// <param name="operations">The operations.</param>
    /// <returns></returns>
    public virtual List<IOperation> Visit(List<IOperation> operations) {
      if (this.stopTraversal) return operations;
      for (int i = 0, n = operations.Count; i < n; i++)
        operations[i] = this.Visit(this.GetMutableCopy(operations[i]));
      return operations;
    }

    /// <summary>
    /// Visits the specified operation.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Gets the mutable copy if it exists.
    /// </summary>
    /// <param name="parameterDefinition">The parameter definition.</param>
    /// <returns></returns>
    public virtual object GetMutableCopyIfItExists(IParameterDefinition parameterDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(parameterDefinition, out cachedValue);
      return cachedValue != null ? cachedValue : parameterDefinition;
    }

    /// <summary>
    /// Gets the mutable copy if it exists.
    /// </summary>
    /// <param name="localDefinition">The local definition.</param>
    /// <returns></returns>
    public virtual object GetMutableCopyIfItExists(ILocalDefinition localDefinition) {
      object/*?*/ cachedValue;
      this.cache.TryGetValue(localDefinition, out cachedValue);
      return cachedValue != null ? cachedValue : localDefinition;
    }

    /// <summary>
    /// Visits the specified locals.
    /// </summary>
    /// <param name="locals">The locals.</param>
    /// <returns></returns>
    public virtual List<ILocalDefinition> Visit(List<ILocalDefinition> locals) {
      if (this.stopTraversal) return locals;
      for (int i = 0, n = locals.Count; i < n; i++)
        locals[i] = this.Visit(this.GetMutableCopy(locals[i]));
      return locals;
    }

    /// <summary>
    /// Visits the specified method definitions.
    /// </summary>
    /// <param name="methodDefinitions">The method definitions.</param>
    /// <returns></returns>
    public virtual List<IMethodDefinition> Visit(List<IMethodDefinition> methodDefinitions) {
      if (this.stopTraversal) return methodDefinitions;
      for (int i = 0, n = methodDefinitions.Count; i < n; i++)
        methodDefinitions[i] = this.Visit(methodDefinitions[i]);
      return methodDefinitions;
    }

    /// <summary>
    /// Visits the specified global field definition.
    /// </summary>
    /// <param name="globalFieldDefinition">The global field definition.</param>
    /// <returns></returns>
    public virtual IGlobalFieldDefinition Visit(IGlobalFieldDefinition globalFieldDefinition) {
      return this.Visit(this.GetMutableCopy(globalFieldDefinition));
    }

    /// <summary>
    /// Visits the specified global method definition.
    /// </summary>
    /// <param name="globalMethodDefinition">The global method definition.</param>
    /// <returns></returns>
    public virtual IGlobalMethodDefinition Visit(IGlobalMethodDefinition globalMethodDefinition) {
      return this.Visit(this.GetMutableCopy(globalMethodDefinition));
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
    public virtual IMethodDefinition Visit(IMethodDefinition methodDefinition) {
      return this.Visit(this.GetMutableCopy(methodDefinition));
    }

    /// <summary>
    /// Visits the specified method definition.
    /// </summary>
    /// <param name="methodDefinition">The method definition.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified method implementations.
    /// </summary>
    /// <param name="methodImplementations">The method implementations.</param>
    /// <returns></returns>
    public virtual List<IMethodImplementation> Visit(List<IMethodImplementation> methodImplementations) {
      if (this.stopTraversal) return methodImplementations;
      for (int i = 0, n = methodImplementations.Count; i < n; i++)
        methodImplementations[i] = this.Visit(this.GetMutableCopy(methodImplementations[i]));
      return methodImplementations;
    }

    /// <summary>
    /// Visits the specified method implementation.
    /// </summary>
    /// <param name="methodImplementation">The method implementation.</param>
    /// <returns></returns>
    public virtual MethodImplementation Visit(MethodImplementation methodImplementation) {
      if (this.stopTraversal) return methodImplementation;
      this.path.Push(methodImplementation);
      methodImplementation.ContainingType = this.GetCurrentType();
      methodImplementation.ImplementedMethod = this.Visit(methodImplementation.ImplementedMethod);
      methodImplementation.ImplementingMethod = this.Visit(methodImplementation.ImplementingMethod);
      this.path.Pop();
      return methodImplementation;
    }

    /// <summary>
    /// Visits the specified method references.
    /// </summary>
    /// <param name="methodReferences">The method references.</param>
    /// <returns></returns>
    public virtual List<IMethodReference> Visit(List<IMethodReference> methodReferences) {
      if (this.stopTraversal) return methodReferences;
      for (int i = 0, n = methodReferences.Count; i < n; i++)
        methodReferences[i] = this.Visit(methodReferences[i]);
      return methodReferences;
    }

    /// <summary>
    /// Visits the specified method body.
    /// </summary>
    /// <param name="methodBody">The method body.</param>
    /// <returns></returns>
    public virtual IMethodBody Visit(IMethodBody methodBody) {
      return this.Visit(this.GetMutableCopy(methodBody));
    }

    /// <summary>
    /// Visits the specified method reference.
    /// </summary>
    /// <param name="methodReference">The method reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified method reference.
    /// </summary>
    /// <param name="methodReference">The method reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified modules.
    /// </summary>
    /// <param name="modules">The modules.</param>
    /// <returns></returns>
    public virtual List<IModule> Visit(List<IModule> modules) {
      if (this.stopTraversal) return modules;
      for (int i = 0, n = modules.Count; i < n; i++) {
        modules[i] = this.Visit(this.GetMutableCopy(modules[i]));
        this.flatListOfTypes.Clear();
      }
      return modules;
    }

    /// <summary>
    /// Visits the specified modified type reference.
    /// </summary>
    /// <param name="modifiedTypeReference">The modified type reference.</param>
    /// <returns></returns>
    public virtual ModifiedTypeReference Visit(ModifiedTypeReference modifiedTypeReference) {
      if (this.stopTraversal) return modifiedTypeReference;
      this.Visit((TypeReference)modifiedTypeReference);
      this.path.Push(modifiedTypeReference);
      modifiedTypeReference.CustomModifiers = this.Visit(modifiedTypeReference.CustomModifiers);
      modifiedTypeReference.UnmodifiedType = this.Visit(modifiedTypeReference.UnmodifiedType);
      this.path.Pop();
      return modifiedTypeReference;
    }

    /// <summary>
    /// Makes a (shallow) mutable copy of the given module and then visits the copy, which generally
    /// results in a deep mutable copy, depending on how subclasses override the behavior of the methods
    /// of this base class.
    /// </summary>
    /// <param name="module">The module to copy.</param>
    public virtual Module Visit(IModule module) {
      var assembly = module as IAssembly;
      if (assembly != null) return this.Visit(assembly);
      return this.Visit(this.GetMutableCopy(module));
    }

    /// <summary>
    /// Visits the specified module.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns></returns>
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

      /// <summary>
      /// 
      /// </summary>
      /// <param name="x"></param>
      /// <param name="y"></param>
      /// <returns></returns>
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

    /// <summary>
    /// Visits the specified module references.
    /// </summary>
    /// <param name="moduleReferences">The module references.</param>
    /// <returns></returns>
    public virtual List<IModuleReference> Visit(List<IModuleReference> moduleReferences) {
      if (this.stopTraversal) return moduleReferences;
      for (int i = 0, n = moduleReferences.Count; i < n; i++)
        moduleReferences[i] = this.Visit(moduleReferences[i]);
      return moduleReferences;
    }

    /// <summary>
    /// Visits the specified module reference.
    /// </summary>
    /// <param name="moduleReference">The module reference.</param>
    /// <returns></returns>
    public virtual ModuleReference Visit(ModuleReference moduleReference) {
      if (moduleReference.ResolvedModule != Dummy.Module) {
        object/*?*/ mutatedResolvedModule = null;
        if (this.cache.TryGetValue(moduleReference.ResolvedModule, out mutatedResolvedModule))
          moduleReference.ResolvedModule = (IModule)mutatedResolvedModule;
      }
      return moduleReference;
    }

    /// <summary>
    /// Visits the specified namespace members.
    /// </summary>
    /// <param name="namespaceMembers">The namespace members.</param>
    /// <returns></returns>
    public virtual List<INamespaceMember> Visit(List<INamespaceMember> namespaceMembers) {
      if (this.stopTraversal) return namespaceMembers;
      for (int i = 0, n = namespaceMembers.Count; i < n; i++)
        namespaceMembers[i] = this.Visit(namespaceMembers[i]);
      return namespaceMembers;
    }

    /// <summary>
    /// Visits the specified namespace member.
    /// </summary>
    /// <param name="namespaceMember">The namespace member.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified namespace alias for type.
    /// </summary>
    /// <param name="namespaceAliasForType">Type of the namespace alias for.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition">The namespace type definition.</param>
    /// <returns></returns>
    public virtual NamespaceTypeDefinition Visit(NamespaceTypeDefinition namespaceTypeDefinition) {
      if (this.stopTraversal) return namespaceTypeDefinition;
      this.Visit((TypeDefinition)namespaceTypeDefinition);
      namespaceTypeDefinition.ContainingUnitNamespace = this.GetCurrentNamespace();
      return namespaceTypeDefinition;
    }

    /// <summary>
    /// Visits the specified namespace type reference.
    /// </summary>
    /// <param name="namespaceTypeReference">The namespace type reference.</param>
    /// <returns></returns>
    public virtual NamespaceTypeReference Visit(NamespaceTypeReference namespaceTypeReference) {
      if (this.stopTraversal) return namespaceTypeReference;
      this.Visit((TypeReference)namespaceTypeReference);
      this.path.Push(namespaceTypeReference);
      namespaceTypeReference.ContainingUnitNamespace = this.Visit(namespaceTypeReference.ContainingUnitNamespace);
      this.path.Pop();
      return namespaceTypeReference;
    }

    /// <summary>
    /// Visits the specified nested alias for type.
    /// </summary>
    /// <param name="nestedAliasForType">Type of the nested alias for.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified nested type definitions.
    /// </summary>
    /// <param name="nestedTypeDefinitions">The nested type definitions.</param>
    /// <returns></returns>
    public virtual List<INestedTypeDefinition> Visit(List<INestedTypeDefinition> nestedTypeDefinitions) {
      if (this.stopTraversal) return nestedTypeDefinitions;
      for (int i = 0, n = nestedTypeDefinitions.Count; i < n; i++)
        nestedTypeDefinitions[i] = this.Visit(nestedTypeDefinitions[i]);
      return nestedTypeDefinitions;
    }

    /// <summary>
    /// Visits the specified nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition">The nested type definition.</param>
    /// <returns></returns>
    public virtual NestedTypeDefinition Visit(NestedTypeDefinition nestedTypeDefinition) {
      if (this.stopTraversal) return nestedTypeDefinition;
      this.Visit((TypeDefinition)nestedTypeDefinition);
      nestedTypeDefinition.ContainingTypeDefinition = this.GetCurrentType();
      return nestedTypeDefinition;
    }

    /// <summary>
    /// Visits the specified nested type reference.
    /// </summary>
    /// <param name="nestedTypeReference">The nested type reference.</param>
    /// <returns></returns>
    public virtual NestedTypeReference Visit(NestedTypeReference nestedTypeReference) {
      if (this.stopTraversal) return nestedTypeReference;
      this.Visit((TypeReference)nestedTypeReference);
      this.path.Push(nestedTypeReference);
      nestedTypeReference.ContainingType = this.Visit(nestedTypeReference.ContainingType);
      this.path.Pop();
      return nestedTypeReference;
    }

    /// <summary>
    /// Visits the specified specialized field reference.
    /// </summary>
    /// <param name="specializedFieldReference">The specialized field reference.</param>
    /// <returns></returns>
    public virtual SpecializedFieldReference Visit(SpecializedFieldReference specializedFieldReference) {
      if (this.stopTraversal) return specializedFieldReference;
      this.Visit((FieldReference)specializedFieldReference);
      this.path.Push(specializedFieldReference);
      specializedFieldReference.UnspecializedVersion = this.Visit(specializedFieldReference.UnspecializedVersion);
      this.path.Pop();
      return specializedFieldReference;
    }

    /// <summary>
    /// Visits the specified specialized method reference.
    /// </summary>
    /// <param name="specializedMethodReference">The specialized method reference.</param>
    /// <returns></returns>
    public virtual SpecializedMethodReference Visit(SpecializedMethodReference specializedMethodReference) {
      if (this.stopTraversal) return specializedMethodReference;
      this.Visit((MethodReference)specializedMethodReference);
      this.path.Push(specializedMethodReference);
      specializedMethodReference.UnspecializedVersion = this.Visit(specializedMethodReference.UnspecializedVersion);
      this.path.Pop();
      return specializedMethodReference;
    }

    /// <summary>
    /// Visits the specified specialized nested type reference.
    /// </summary>
    /// <param name="specializedNestedTypeReference">The specialized nested type reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the private helper members.
    /// </summary>
    /// <param name="typeDefinitions">The type definitions.</param>
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

    /// <summary>
    /// Visits the specified type definition members.
    /// </summary>
    /// <param name="typeDefinitionMembers">The type definition members.</param>
    /// <returns></returns>
    public virtual List<ITypeDefinitionMember> Visit(List<ITypeDefinitionMember> typeDefinitionMembers) {
      if (this.stopTraversal) return typeDefinitionMembers;
      for (int i = 0, n = typeDefinitionMembers.Count; i < n; i++)
        typeDefinitionMembers[i] = this.Visit(typeDefinitionMembers[i]);
      return typeDefinitionMembers;
    }

    /// <summary>
    /// Visits the specified type definition member.
    /// </summary>
    /// <param name="typeDefinitionMember">The type definition member.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified type references.
    /// </summary>
    /// <param name="typeReferences">The type references.</param>
    /// <returns></returns>
    public virtual List<ITypeReference> Visit(List<ITypeReference> typeReferences) {
      if (this.stopTraversal) return typeReferences;
      for (int i = 0, n = typeReferences.Count; i < n; i++)
        typeReferences[i] = this.Visit(typeReferences[i]);
      return typeReferences;
    }

    /// <summary>
    /// Visits the specified namespace type reference.
    /// </summary>
    /// <param name="namespaceTypeReference">The namespace type reference.</param>
    /// <returns></returns>
    public virtual INamespaceTypeReference Visit(INamespaceTypeReference namespaceTypeReference) {
      INamespaceTypeDefinition/*?*/ namespaceTypeDefinition = namespaceTypeReference as INamespaceTypeDefinition;
      if (namespaceTypeDefinition != null)
        return this.GetMutableCopy(namespaceTypeDefinition);
      return this.Visit(this.GetMutableCopy(namespaceTypeReference));
    }

    /// <summary>
    /// Visits the specified nested type reference.
    /// </summary>
    /// <param name="nestedTypeReference">The nested type reference.</param>
    /// <returns></returns>
    public virtual INestedTypeReference Visit(INestedTypeReference nestedTypeReference) {
      ISpecializedNestedTypeReference/*?*/ specializedNestedTypeReference = nestedTypeReference as ISpecializedNestedTypeReference;
      if (specializedNestedTypeReference != null)
        return this.Visit(this.GetMutableCopy(specializedNestedTypeReference));
      INestedTypeDefinition/*?*/ nestedTypeDefinition = nestedTypeReference as INestedTypeDefinition;
      if (nestedTypeDefinition != null)
        return this.GetMutableCopy(nestedTypeDefinition);
      return this.Visit(this.GetMutableCopy(nestedTypeReference));
    }

    /// <summary>
    /// Visits the specified generic method parameter reference.
    /// </summary>
    /// <param name="genericMethodParameterReference">The generic method parameter reference.</param>
    /// <returns></returns>
    public virtual IGenericMethodParameterReference Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      IGenericMethodParameter/*?*/ genericMethodParameter = genericMethodParameterReference as IGenericMethodParameter;
      if (genericMethodParameter != null)
        return this.GetMutableCopy(genericMethodParameter);
      return this.Visit(this.GetMutableCopy(genericMethodParameterReference));
    }

    /// <summary>
    /// Visits the specified array type reference.
    /// </summary>
    /// <param name="arrayTypeReference">The array type reference.</param>
    /// <returns></returns>
    /// <remarks>Array types are not nominal types, so always visit the reference, even if it is a definition.</remarks>
    public virtual IArrayTypeReference Visit(IArrayTypeReference arrayTypeReference) {
      if (arrayTypeReference.IsVector)
        return this.Visit(this.GetMutableVectorCopy(arrayTypeReference));
      else
        return this.Visit(this.GetMutableMatrixCopy(arrayTypeReference));
    }

    /// <summary>
    /// Visits the specified generic type parameter reference.
    /// </summary>
    /// <param name="genericTypeParameterReference">The generic type parameter reference.</param>
    /// <returns></returns>
    public virtual IGenericTypeParameterReference Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      IGenericTypeParameter/*?*/ genericTypeParameter = genericTypeParameterReference as IGenericTypeParameter;
      if (genericTypeParameter != null)
        return this.GetMutableCopy(genericTypeParameter);
      return this.Visit(this.GetMutableCopy(genericTypeParameterReference));
    }

    /// <summary>
    /// Visits the specified generic type instance reference.
    /// </summary>
    /// <param name="genericTypeInstanceReference">The generic type instance reference.</param>
    /// <returns></returns>
    public virtual IGenericTypeInstanceReference Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      return this.Visit(this.GetMutableCopy(genericTypeInstanceReference));
    }

    /// <summary>
    /// Visits the specified pointer type reference.
    /// </summary>
    /// <param name="pointerTypeReference">The pointer type reference.</param>
    /// <returns></returns>
    /// <remarks>
    /// Pointer types are not nominal types, so always visit the reference, even if
    /// it is a definition.
    /// </remarks>
    public virtual IPointerTypeReference Visit(IPointerTypeReference pointerTypeReference) {
      return this.Visit(this.GetMutableCopy(pointerTypeReference));
    }

    /// <summary>
    /// Visits the specified function pointer type reference.
    /// </summary>
    /// <param name="functionPointerTypeReference">The function pointer type reference.</param>
    /// <returns></returns>
    public virtual IFunctionPointerTypeReference Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      IFunctionPointer/*?*/ functionPointer = functionPointerTypeReference as IFunctionPointer;
      if (functionPointer != null)
        return this.Visit(this.GetMutableCopy(functionPointer));
      return this.Visit(this.GetMutableCopy(functionPointerTypeReference));
    }

    /// <summary>
    /// Visits the specified managed pointer type reference.
    /// </summary>
    /// <param name="managedPointerTypeReference">The managed pointer type reference.</param>
    /// <returns></returns>
    /// <remarks>
    /// Managed pointer types are not nominal types, so always visit the reference, even if
    /// it is a definition.
    /// </remarks>
    public virtual IManagedPointerTypeReference Visit(IManagedPointerTypeReference managedPointerTypeReference) {
      return this.Visit(this.GetMutableCopy(managedPointerTypeReference));
    }

    /// <summary>
    /// Visits the specified modified type reference.
    /// </summary>
    /// <param name="modifiedTypeReference">The modified type reference.</param>
    /// <returns></returns>
    public virtual IModifiedTypeReference Visit(IModifiedTypeReference modifiedTypeReference) {
      return this.Visit(this.GetMutableCopy(modifiedTypeReference));
    }

    /// <summary>
    /// Visits the specified module reference.
    /// </summary>
    /// <param name="moduleReference">The module reference.</param>
    /// <returns></returns>
    public virtual IModuleReference Visit(IModuleReference moduleReference) {
      return this.Visit(this.GetMutableCopy(moduleReference));
    }

    /// <summary>
    /// Visits the specified namespace type definition.
    /// </summary>
    /// <param name="namespaceTypeDefinition">The namespace type definition.</param>
    /// <returns></returns>
    public virtual INamespaceTypeDefinition Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
      return this.Visit(this.GetMutableCopy(namespaceTypeDefinition));
    }

    /// <summary>
    /// Visits the specified nested type definition.
    /// </summary>
    /// <param name="nestedTypeDefinition">The nested type definition.</param>
    /// <returns></returns>
    public virtual INestedTypeDefinition Visit(INestedTypeDefinition nestedTypeDefinition) {
      return this.Visit(this.GetMutableCopy(nestedTypeDefinition));
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    /// <returns></returns>
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
      IManagedPointerTypeReference/*?*/ managedPointerTypeReference = typeReference as IManagedPointerType;
      if (managedPointerTypeReference != null)
        return this.Visit(managedPointerTypeReference);
      //TODO: error
      return typeReference;
    }

    /// <summary>
    /// Visits the specified unit namespace reference.
    /// </summary>
    /// <param name="unitNamespaceReference">The unit namespace reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified nested unit namespace.
    /// </summary>
    /// <param name="nestedUnitNamespace">The nested unit namespace.</param>
    /// <returns></returns>
    public virtual INestedUnitNamespace Visit(INestedUnitNamespace nestedUnitNamespace) {
      return this.Visit(this.GetMutableCopy(nestedUnitNamespace));
    }

    /// <summary>
    /// Visits the specified nested unit namespace reference.
    /// </summary>
    /// <param name="nestedUnitNamespaceReference">The nested unit namespace reference.</param>
    /// <returns></returns>
    public virtual INestedUnitNamespaceReference Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
      INestedUnitNamespace/*?*/ nestedUnitNamespace = nestedUnitNamespaceReference as INestedUnitNamespace;
      if (nestedUnitNamespace != null)
        return this.GetMutableCopy(nestedUnitNamespace);
      return this.Visit(this.GetMutableCopy(nestedUnitNamespaceReference));
    }

    /// <summary>
    /// Visits the specified nested unit namespace.
    /// </summary>
    /// <param name="nestedUnitNamespace">The nested unit namespace.</param>
    /// <returns></returns>
    public virtual NestedUnitNamespace Visit(NestedUnitNamespace nestedUnitNamespace) {
      if (this.stopTraversal) return nestedUnitNamespace;
      this.Visit((UnitNamespace)nestedUnitNamespace);
      nestedUnitNamespace.ContainingUnitNamespace = this.GetCurrentNamespace();
      return nestedUnitNamespace;
    }

    /// <summary>
    /// Visits the specified nested unit namespace reference.
    /// </summary>
    /// <param name="nestedUnitNamespaceReference">The nested unit namespace reference.</param>
    /// <returns></returns>
    public virtual NestedUnitNamespaceReference Visit(NestedUnitNamespaceReference nestedUnitNamespaceReference) {
      if (this.stopTraversal) return nestedUnitNamespaceReference;
      this.Visit((UnitNamespaceReference)nestedUnitNamespaceReference);
      nestedUnitNamespaceReference.ContainingUnitNamespace = this.Visit(nestedUnitNamespaceReference.ContainingUnitNamespace);
      return nestedUnitNamespaceReference;
    }

    /// <summary>
    /// Visits the specified unit reference.
    /// </summary>
    /// <param name="unitReference">The unit reference.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified parameter definitions.
    /// </summary>
    /// <param name="parameterDefinitions">The parameter definitions.</param>
    /// <returns></returns>
    public virtual List<IParameterDefinition> Visit(List<IParameterDefinition> parameterDefinitions) {
      if (this.stopTraversal) return parameterDefinitions;
      for (int i = 0, n = parameterDefinitions.Count; i < n; i++)
        parameterDefinitions[i] = this.Visit(this.GetMutableCopy(parameterDefinitions[i]));
      return parameterDefinitions;
    }

    /// <summary>
    /// Visits the specified parameter definition.
    /// </summary>
    /// <param name="parameterDefinition">The parameter definition.</param>
    /// <returns></returns>
    public virtual ParameterDefinition Visit(ParameterDefinition parameterDefinition) {
      if (this.stopTraversal) return parameterDefinition;
      this.path.Push(parameterDefinition);
      parameterDefinition.Attributes = this.Visit(parameterDefinition.Attributes);
      parameterDefinition.ContainingSignature = this.GetCurrentSignature();
      if (parameterDefinition.HasDefaultValue)
        parameterDefinition.DefaultValue = this.Visit(this.GetMutableCopy(parameterDefinition.DefaultValue));
      if (parameterDefinition.IsModified)
        parameterDefinition.CustomModifiers = this.Visit(parameterDefinition.CustomModifiers);
      parameterDefinition.Locations = this.Visit(parameterDefinition.Locations);
      if (parameterDefinition.IsMarshalledExplicitly)
        parameterDefinition.MarshallingInformation = this.Visit(this.GetMutableCopy(parameterDefinition.MarshallingInformation));
      parameterDefinition.Type = this.Visit(parameterDefinition.Type);
      this.path.Pop();
      return parameterDefinition;
    }

    /// <summary>
    /// Visits the specified parameter type information list.
    /// </summary>
    /// <param name="parameterTypeInformationList">The parameter type information list.</param>
    /// <returns></returns>
    public virtual List<IParameterTypeInformation> Visit(List<IParameterTypeInformation> parameterTypeInformationList) {
      if (this.stopTraversal) return parameterTypeInformationList;
      for (int i = 0, n = parameterTypeInformationList.Count; i < n; i++)
        parameterTypeInformationList[i] = this.Visit(this.GetMutableCopy(parameterTypeInformationList[i]));
      return parameterTypeInformationList;
    }

    /// <summary>
    /// Visits the specified parameter type information.
    /// </summary>
    /// <param name="parameterTypeInformation">The parameter type information.</param>
    /// <returns></returns>
    public virtual ParameterTypeInformation Visit(ParameterTypeInformation parameterTypeInformation) {
      if (this.stopTraversal) return parameterTypeInformation;
      this.path.Push(parameterTypeInformation);
      if (parameterTypeInformation.IsModified)
        parameterTypeInformation.CustomModifiers = this.Visit(parameterTypeInformation.CustomModifiers);
      parameterTypeInformation.Type = this.Visit(parameterTypeInformation.Type);
      this.path.Pop();
      return parameterTypeInformation;
    }

    /// <summary>
    /// Visits the specified platform invoke information.
    /// </summary>
    /// <param name="platformInvokeInformation">The platform invoke information.</param>
    /// <returns></returns>
    public virtual PlatformInvokeInformation Visit(PlatformInvokeInformation platformInvokeInformation) {
      if (this.stopTraversal) return platformInvokeInformation;
      this.path.Push(platformInvokeInformation);
      platformInvokeInformation.ImportModule = this.Visit(this.GetMutableCopy(platformInvokeInformation.ImportModule));
      this.path.Pop();
      return platformInvokeInformation;
    }

    /// <summary>
    /// Visits the specified property definitions.
    /// </summary>
    /// <param name="propertyDefinitions">The property definitions.</param>
    /// <returns></returns>
    public virtual List<IPropertyDefinition> Visit(List<IPropertyDefinition> propertyDefinitions) {
      if (this.stopTraversal) return propertyDefinitions;
      for (int i = 0, n = propertyDefinitions.Count; i < n; i++)
        propertyDefinitions[i] = this.Visit(propertyDefinitions[i]);
      return propertyDefinitions;
    }

    /// <summary>
    /// Visits the specified property definition.
    /// </summary>
    /// <param name="propertyDefinition">The property definition.</param>
    /// <returns></returns>
    public virtual IPropertyDefinition Visit(IPropertyDefinition propertyDefinition) {
      return this.Visit(this.GetMutableCopy(propertyDefinition));
    }

    /// <summary>
    /// Visits the specified property definition.
    /// </summary>
    /// <param name="propertyDefinition">The property definition.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified pointer type reference.
    /// </summary>
    /// <param name="pointerTypeReference">The pointer type reference.</param>
    /// <returns></returns>
    public virtual PointerTypeReference Visit(PointerTypeReference pointerTypeReference) {
      if (this.stopTraversal) return pointerTypeReference;
      this.Visit((TypeReference)pointerTypeReference);
      this.path.Push(pointerTypeReference);
      pointerTypeReference.TargetType = this.Visit(pointerTypeReference.TargetType);
      this.path.Pop();
      return pointerTypeReference;
    }

    /// <summary>
    /// Visits the specified resource references.
    /// </summary>
    /// <param name="resourceReferences">The resource references.</param>
    /// <returns></returns>
    public virtual List<IResourceReference> Visit(List<IResourceReference> resourceReferences) {
      if (this.stopTraversal) return resourceReferences;
      for (int i = 0, n = resourceReferences.Count; i < n; i++)
        resourceReferences[i] = this.Visit(this.GetMutableCopy(resourceReferences[i]));
      return resourceReferences;
    }

    /// <summary>
    /// Visits the specified resource reference.
    /// </summary>
    /// <param name="resourceReference">The resource reference.</param>
    /// <returns></returns>
    public virtual IResourceReference Visit(ResourceReference resourceReference) {
      if (this.stopTraversal) return resourceReference;
      resourceReference.Attributes = this.Visit(resourceReference.Attributes);
      resourceReference.DefiningAssembly = this.Visit(this.GetMutableCopy(resourceReference.DefiningAssembly));
      return resourceReference;
    }

    /// <summary>
    /// Visits the specified security attributes.
    /// </summary>
    /// <param name="securityAttributes">The security attributes.</param>
    /// <returns></returns>
    public virtual List<ISecurityAttribute> Visit(List<ISecurityAttribute> securityAttributes) {
      if (this.stopTraversal) return securityAttributes;
      for (int i = 0, n = securityAttributes.Count; i < n; i++)
        securityAttributes[i] = this.Visit(this.GetMutableCopy(securityAttributes[i]));
      return securityAttributes;
    }

    /// <summary>
    /// Visits the specified root unit namespace reference.
    /// </summary>
    /// <param name="rootUnitNamespaceReference">The root unit namespace reference.</param>
    /// <returns></returns>
    public virtual IRootUnitNamespaceReference Visit(IRootUnitNamespaceReference rootUnitNamespaceReference) {
      IRootUnitNamespace/*?*/ rootUnitNamespace = rootUnitNamespaceReference as IRootUnitNamespace;
      if (rootUnitNamespace != null)
        return this.GetMutableCopy(rootUnitNamespace);
      return this.Visit(this.GetMutableCopy(rootUnitNamespaceReference));
    }

    /// <summary>
    /// Visits the specified root unit namespace.
    /// </summary>
    /// <param name="rootUnitNamespace">The root unit namespace.</param>
    /// <returns></returns>
    public virtual RootUnitNamespace Visit(RootUnitNamespace rootUnitNamespace) {
      if (this.stopTraversal) return rootUnitNamespace;
      this.Visit((UnitNamespace)rootUnitNamespace);
      return rootUnitNamespace;
    }

    /// <summary>
    /// Visits the specified root unit namespace reference.
    /// </summary>
    /// <param name="rootUnitNamespaceReference">The root unit namespace reference.</param>
    /// <returns></returns>
    public virtual RootUnitNamespaceReference Visit(RootUnitNamespaceReference rootUnitNamespaceReference) {
      if (this.stopTraversal) return rootUnitNamespaceReference;
      rootUnitNamespaceReference.Unit = this.Visit(rootUnitNamespaceReference.Unit);
      return rootUnitNamespaceReference;
    }

    /// <summary>
    /// Visits the specified security attribute.
    /// </summary>
    /// <param name="securityAttribute">The security attribute.</param>
    /// <returns></returns>
    public virtual SecurityAttribute Visit(SecurityAttribute securityAttribute) {
      if (this.stopTraversal) return securityAttribute;
      this.path.Push(securityAttribute);
      securityAttribute.Attributes = this.Visit(securityAttribute.Attributes);
      this.path.Pop();
      return securityAttribute;
    }

    /// <summary>
    /// Visits the specified section block.
    /// </summary>
    /// <param name="sectionBlock">The section block.</param>
    /// <returns></returns>
    public virtual SectionBlock Visit(SectionBlock sectionBlock) {
      return sectionBlock;
    }

    /// <summary>
    /// Visits the specified type definition member.
    /// </summary>
    /// <param name="typeDefinitionMember">The type definition member.</param>
    /// <returns></returns>
    public virtual ITypeDefinitionMember Visit(TypeDefinitionMember typeDefinitionMember) {
      if (this.stopTraversal) return typeDefinitionMember;
      this.path.Push(typeDefinitionMember);
      typeDefinitionMember.Attributes = this.Visit(typeDefinitionMember.Attributes);
      typeDefinitionMember.ContainingType = this.GetCurrentType();
      typeDefinitionMember.Locations = this.Visit(typeDefinitionMember.Locations);
      this.path.Pop();
      return typeDefinitionMember;
    }

    /// <summary>
    /// Visits the specified type reference.
    /// </summary>
    /// <param name="typeReference">The type reference.</param>
    /// <returns></returns>
    public virtual TypeReference Visit(TypeReference typeReference) {
      if (this.stopTraversal) return typeReference;
      this.path.Push(typeReference);
      typeReference.Attributes = this.Visit(typeReference.Attributes);
      typeReference.Locations = this.Visit(typeReference.Locations);
      this.path.Pop();
      return typeReference;
    }

    /// <summary>
    /// Visits the specified unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns></returns>
    public virtual Unit Visit(Unit unit) {
      if (this.stopTraversal) return unit;
      this.path.Push(unit);
      unit.Attributes = this.Visit(unit.Attributes);
      unit.Locations = this.Visit(unit.Locations);
      unit.UnitNamespaceRoot = this.Visit(this.GetMutableCopy((IRootUnitNamespace)unit.UnitNamespaceRoot));
      this.path.Pop();
      return unit;
    }

    /// <summary>
    /// Visits the specified unit namespace.
    /// </summary>
    /// <param name="unitNamespace">The unit namespace.</param>
    /// <returns></returns>
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

    /// <summary>
    /// Visits the specified unit namespace reference.
    /// </summary>
    /// <param name="unitNamespaceReference">The unit namespace reference.</param>
    /// <returns></returns>
    public virtual UnitNamespaceReference Visit(UnitNamespaceReference unitNamespaceReference) {
      if (this.stopTraversal) return unitNamespaceReference;
      this.path.Push(unitNamespaceReference);
      unitNamespaceReference.Attributes = this.Visit(unitNamespaceReference.Attributes);
      unitNamespaceReference.Locations = this.Visit(unitNamespaceReference.Locations);
      this.path.Pop();
      return unitNamespaceReference;
    }

    /// <summary>
    /// Visits the specified vector type reference.
    /// </summary>
    /// <param name="vectorTypeReference">The vector type reference.</param>
    /// <returns></returns>
    public virtual VectorTypeReference Visit(VectorTypeReference vectorTypeReference) {
      if (this.stopTraversal) return vectorTypeReference;
      this.Visit((TypeReference)vectorTypeReference);
      this.path.Push(vectorTypeReference);
      vectorTypeReference.ElementType = this.Visit(vectorTypeReference.ElementType);
      this.path.Pop();
      return vectorTypeReference;
    }

    /// <summary>
    /// Visits the specified win32 resources.
    /// </summary>
    /// <param name="win32Resources">The win32 resources.</param>
    /// <returns></returns>
    public virtual List<IWin32Resource> Visit(List<IWin32Resource> win32Resources) {
      if (this.stopTraversal) return win32Resources;
      for (int i = 0, n = win32Resources.Count; i < n; i++)
        win32Resources[i] = this.Visit(win32Resources[i]);
      return win32Resources;
    }

    /// <summary>
    /// Visits the specified win32 resource.
    /// </summary>
    /// <param name="win32Resource">The win32 resource.</param>
    /// <returns></returns>
    public virtual IWin32Resource Visit(IWin32Resource win32Resource) {
      return win32Resource;
    }

    /// <summary>
    /// Visits the property return value attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    /// <returns></returns>
    public virtual List<ICustomAttribute> VisitPropertyReturnValueAttributes(List<ICustomAttribute> customAttributes) {
      return this.Visit(customAttributes);
    }

    /// <summary>
    /// Visits the method return value attributes.
    /// </summary>
    /// <param name="customAttributes">The custom attributes.</param>
    /// <returns></returns>
    public virtual List<ICustomAttribute> VisitMethodReturnValueAttributes(List<ICustomAttribute> customAttributes) {
      return this.Visit(customAttributes);
    }

    /// <summary>
    /// Visits the method return value custom modifiers.
    /// </summary>
    /// <param name="customModifers">The custom modifers.</param>
    /// <returns></returns>
    public virtual List<ICustomModifier> VisitMethodReturnValueCustomModifiers(List<ICustomModifier> customModifers) {
      return this.Visit(customModifers);
    }

    /// <summary>
    /// Visits the method return value marshalling information.
    /// </summary>
    /// <param name="marshallingInformation">The marshalling information.</param>
    /// <returns></returns>
    public virtual IMarshallingInformation VisitMethodReturnValueMarshallingInformation(MarshallingInformation marshallingInformation) {
      return this.Visit(marshallingInformation);
    }

  }
}
