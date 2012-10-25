// ==++==
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Cci;
using Microsoft.Cci.UtilityDataStructures;
using System;
using System.Reflection;

namespace ILtoC {
  partial class Translator {

    [ContractVerification(false)]
    private void EmitStructs(Hashtable<ITypeReference> closedStructuralTypeInstances) {
      var emittedTypes = new SetOfUints();
      foreach (var type in this.module.GetAllTypes()) {
        Contract.Assume(type != null);
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue;
        this.EmitStruct(type, emittedTypes);
      }
      if (closedStructuralTypeInstances != null) {
        foreach (var structuralTypeInstance in closedStructuralTypeInstances.Values) {
          Contract.Assume(structuralTypeInstance != null);
          this.EmitStruct(structuralTypeInstance.ResolvedType, emittedTypes);
        }
      }
    }

    private void EmitStaticVariables(Hashtable<ITypeReference> closedStructuralTypeInstances, IEnumerable<string> strings, bool doingHeader) {
      if (doingHeader) {
        this.sourceEmitter.EmitString("extern tls_type appdomain_static_block_tlsIndex;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("extern uint32_t interfaceMethodIDCounter;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("extern uint8_t* statics;");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString("extern tls_type thread_static_block_tlsIndex;");
        this.sourceEmitter.EmitNewLine();
      } else {
        this.sourceEmitter.EmitString("uint32_t ");
        this.sourceEmitter.EmitString(this.TypeLoaderName);
        this.sourceEmitter.EmitString("_is_initialized;");
        this.sourceEmitter.EmitNewLine();
      }
      foreach (var type in this.module.GetAllTypes()) {
        Contract.Assume(type != null);
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue;
        this.EmitTypeObjectVariable(type, doingHeader);
        this.EmitStaticFields(type, doingHeader);
        this.EmitInterfaceMethodIDsForType(type, doingHeader);
        this.sourceEmitter.EmitNewLine();
      }
      if (closedStructuralTypeInstances != null) {
        foreach (var structuralTypeInstance in closedStructuralTypeInstances.Values) {
          Contract.Assume(structuralTypeInstance != null);
          this.EmitTypeObjectVariable(structuralTypeInstance.ResolvedType, doingHeader);
          var genericTypeInstance = structuralTypeInstance as IGenericTypeInstanceReference;
          if (genericTypeInstance != null)
            this.EmitStaticFields(genericTypeInstance.ResolvedType, doingHeader);
          this.sourceEmitter.EmitNewLine();
        }
      }
      if (strings != null) {
        foreach (var str in strings) {
          Contract.Assume(str != null);
          if (doingHeader) this.sourceEmitter.EmitString("extern ");
          this.sourceEmitter.EmitString("uintptr_t ");
          this.sourceEmitter.EmitString(new Mangler().Mangle(str));
          this.sourceEmitter.EmitString(";");
          this.sourceEmitter.EmitNewLine();
        }
      }
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitAllocatorForStaticVariables(IEnumerable<INamedTypeDefinition> allNominalTypes, Hashtable<ITypeReference>/*?*/ closedStructuralTypeInstances) {
      Contract.Requires(allNominalTypes != null);

      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("void allocateStatics()");
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("void* appdomain_static_block;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("void* thread_static_block;");
      this.sourceEmitter.EmitNewLine();

      foreach (var type in allNominalTypes) {
        Contract.Assume(type != null);
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue;
        this.EmitStaticFieldsOffsetInitializers(type);
      }
      if (closedStructuralTypeInstances != null) {
        foreach (var structuralTypeInstance in closedStructuralTypeInstances.Values) {
          Contract.Assume(structuralTypeInstance != null);
          var genericTypeInstance = structuralTypeInstance as IGenericTypeInstanceReference;
          if (genericTypeInstance != null)
            this.EmitStaticFieldsOffsetInitializers(genericTypeInstance.ResolvedType);
        }
      }
      this.sourceEmitter.EmitString("AllocateThreadLocal(&appdomain_static_block_tlsIndex);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("appdomain_static_block = calloc(1, appdomain_static_block_size);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("SetThreadLocalValue(appdomain_static_block_tlsIndex, appdomain_static_block);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("AllocateThreadLocal(&thread_static_block_tlsIndex);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("thread_static_block = calloc(1, thread_static_block_size);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("SetThreadLocalValue(thread_static_block_tlsIndex, appdomain_static_block);");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitStaticFieldsOffsetInitializers(ITypeDefinition type) {
      Contract.Requires(type != null);

      foreach (var field in type.Fields) {
        Contract.Assume(field != null);
        if (!field.IsStatic) continue;
        if (field.IsCompileTimeConstant) continue;
        var sizeField = "appdomain_static_block_size";
        if (AttributeHelper.Contains(field.Attributes, this.threadStaticAttribute)) sizeField = "thread_static_block_size";
        this.sourceEmitter.EmitString(this.GetMangledFieldName(field));
        this.sourceEmitter.EmitString(" = ");
        this.sourceEmitter.EmitString(sizeField);
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        this.sourceEmitter.EmitString(sizeField);
        this.sourceEmitter.EmitString(" = Increment_and_align(");
        this.sourceEmitter.EmitString(sizeField);
        this.sourceEmitter.EmitString(", sizeof(");
        this.EmitTypeReference(field.Type);
        this.sourceEmitter.EmitString("))");
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
      }
      foreach (var method in type.Methods) {
        if (method.IsStaticConstructor) {
          this.sourceEmitter.EmitString(this.GetMangledTypeName(type));
          this.sourceEmitter.EmitString("_isInitialized = appdomain_static_block_size;");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("appdomain_static_block_size = Increment_and_align(appdomain_static_block_size, 1);");
          this.sourceEmitter.EmitNewLine();
        }
      }
    }

    private void EmitInterfaceMethodIDsForType(ITypeDefinition type, bool doingHeader) {
      Contract.Requires(type != null);
      if (!type.IsInterface) return;
      foreach (var method in type.Methods) {
        Contract.Assume(method != null);
        if (doingHeader) this.sourceEmitter.EmitString("extern ");
        this.sourceEmitter.EmitString("uint32_t ");
        this.sourceEmitter.EmitString(this.GetMangledMethodName(method));
        this.sourceEmitter.EmitString("_id;");
        this.sourceEmitter.EmitNewLine();
      }
    }

    private Hashtable<ITypeReference> GetAllClosedStructuralTypeInstanceReferencesInThisModule(out Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances) {
      var closedStructuralTypeReferences = new Hashtable<ITypeReference>();
      closedGenericMethodInstances = new Hashtable<IGenericMethodInstanceReference>();
      this.FindAllClosedStructuralReferencesIn(this.module, closedStructuralTypeReferences, closedGenericMethodInstances, followBaseClassReferencesIntoOtherModules: true);
      return closedStructuralTypeReferences;
    }

    private Hashtable<ITypeReference> GetAllClosedStructuralTypeInstanceReferencesIn(SetOfObjects modules, out Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances) {
      Contract.Requires(modules != null);

      var closedStructuralTypeReferences = new Hashtable<ITypeReference>();
      closedGenericMethodInstances = new Hashtable<IGenericMethodInstanceReference>();
      foreach (var module in modules.Values) {
        IModule mod = module as IModule;
        Contract.Assume(mod != null);
        this.FindAllClosedStructuralReferencesIn(mod, closedStructuralTypeReferences, closedGenericMethodInstances, followBaseClassReferencesIntoOtherModules: false);
      }
      return closedStructuralTypeReferences;
    }

    private void FindAllClosedStructuralReferencesIn(IModule module, Hashtable<ITypeReference> closedStructuralTypeReferences,
      Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances, bool followBaseClassReferencesIntoOtherModules) {
      Contract.Requires(module != null);
      Contract.Requires(closedStructuralTypeReferences != null);
      Contract.Requires(closedGenericMethodInstances != null);

      var finder = new StructuralReferenceFinder(closedStructuralTypeReferences, closedGenericMethodInstances, followBaseClassReferencesIntoOtherModules);
      foreach (var type in module.GetAllTypes()) {
        Contract.Assume(type != null);
        if (type is INestedTypeDefinition) continue; //These are traversed via their containers
        finder.Traverse(type);
      }
    }

    class StructuralReferenceFinder : MetadataTraverser {

      internal StructuralReferenceFinder(Hashtable<ITypeReference> closedStructuralTypeReferences, Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances,
        bool followBaseClassReferencesIntoOtherModules) {
        Contract.Requires(closedStructuralTypeReferences != null);
        Contract.Requires(closedGenericMethodInstances != null);

        this.closedStructuralTypeReferences = closedStructuralTypeReferences;
        this.closedGenericMethodInstances = closedGenericMethodInstances;
        this.followBaseClassReferencesIntoOtherModules = followBaseClassReferencesIntoOtherModules;
        this.TraverseIntoMethodBodies = true;
      }

      private readonly Hashtable<ITypeReference> closedStructuralTypeReferences;
      private readonly Hashtable<IGenericMethodInstanceReference> closedGenericMethodInstances;
      private bool followBaseClassReferencesIntoOtherModules;

      [ContractInvariantMethod]
      private void ObjectInvariant() {
        Contract.Invariant(this.closedGenericMethodInstances != null);
        Contract.Invariant(this.closedStructuralTypeReferences != null);
      }

      public override void TraverseChildren(ITypeReference typeReference) {
        if (typeReference is INamedTypeReference && !(typeReference is ISpecializedNestedTypeReference)) return;
        if (this.closedStructuralTypeReferences[typeReference.InternedKey] != null) return;
        if (!TypeHelper.IsOpen(typeReference)) {
          this.closedStructuralTypeReferences[typeReference.InternedKey] = typeReference;
          this.TraverseChildren(typeReference.ResolvedType);
        }
      }

      public override void TraverseChildren(IGenericMethodInstanceReference genericMethodInstanceReference) {
        if (this.closedGenericMethodInstances[genericMethodInstanceReference.InternedKey] != null) return;
        if (!IsOpen(genericMethodInstanceReference)) {
          this.closedGenericMethodInstances[genericMethodInstanceReference.InternedKey] = genericMethodInstanceReference;
          this.TraverseChildren(genericMethodInstanceReference.ResolvedMethod);
        }
      }

      public override void TraverseChildren(ITypeDefinition typeDefinition) {
        if (this.followBaseClassReferencesIntoOtherModules)
          this.CollectInstancesFromBaseClassChain(typeDefinition);
        base.TraverseChildren(typeDefinition);
      }

      private void CollectInstancesFromBaseClassChain(ITypeDefinition typeDefinition) {
        Contract.Requires(typeDefinition != null);
        foreach (var bc in typeDefinition.BaseClasses) {
          Contract.Assume(bc != null);
          this.TraverseChildren((ITypeReference)bc); //If bc is a closed generic type instance, add it to this.closedStructuralTypeReferences.
          this.CollectInstancesFromBaseClassChain(bc.ResolvedType); //this will eventually leave the module we are visiting
          //but since we chase down all base class chains when setting up the base classes of type objects, we have to consider
          //generic references that appear in the base class chain to be references made by the module we are visiting.
        }
      }
    }

    public static bool IsOpen(IGenericMethodInstanceReference genericMethodInstanceReference) {
      Contract.Requires(genericMethodInstanceReference != null);

      if (TypeHelper.IsOpen(genericMethodInstanceReference.ContainingType)) return true;
      foreach (var genArg in genericMethodInstanceReference.GenericArguments) {
        Contract.Assume(genArg != null);
        if (TypeHelper.IsOpen(genArg)) return true;
      }
      return false;
    }

    private void EmitTypeObjectVariable(ITypeDefinition type, bool doingHeader) {
      Contract.Requires(type != null);

      if (doingHeader) this.sourceEmitter.EmitString("extern ");
      var mangledName = this.GetMangledTypeName(type);
      this.sourceEmitter.EmitString("uintptr_t "+mangledName+"_typeObject;");
      this.sourceEmitter.EmitNewLine();
      if (type.IsInterface) {
        foreach (var method in type.Methods) {
          Contract.Assume(method != null);
          this.sourceEmitter.EmitString("uint32_t ");
          this.sourceEmitter.EmitString(this.GetMangledMethodName(method));
          this.sourceEmitter.EmitString("_id;");
          this.sourceEmitter.EmitNewLine();
        }
      }
    }

    private void EmitStaticFields(ITypeDefinition type, bool doingHeader) {
      Contract.Requires(type != null);

      foreach (var field in type.Fields) {
        Contract.Assume(field != null);
        if (!field.IsStatic) continue;
        if (field.IsCompileTimeConstant) continue;
        if (doingHeader) this.sourceEmitter.EmitString("extern ");
        this.sourceEmitter.EmitString("uint32_t ");
        this.sourceEmitter.EmitString(this.GetMangledFieldName(field));
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
      }
      foreach (var method in type.Methods) {
        if (method.IsStaticConstructor) {
          if (doingHeader) this.sourceEmitter.EmitString("extern ");
          this.sourceEmitter.EmitString("uint32_t ");
          this.sourceEmitter.EmitString(this.GetMangledTypeName(type));
          this.sourceEmitter.EmitString("_isInitialized;");
          this.sourceEmitter.EmitNewLine();
        }
      }
    }

    private string StaticFieldOffsetLoaderName {
      get {
        Contract.Ensures(Contract.Result<string>() != null);
        if (this.staticFieldOffsetLoaderName == null)
          this.staticFieldOffsetLoaderName = new Mangler().Mangle(this.module)+"_static_field_offset_loader";
        return this.staticFieldOffsetLoaderName;
      }
    }
    private string staticFieldOffsetLoaderName;

    private string TypeLoaderName {
      get {
        Contract.Ensures(Contract.Result<string>() != null);
        if (this.typeLoaderName == null)
          this.typeLoaderName = new Mangler().Mangle(this.module)+"_type_loader";
        return this.typeLoaderName;
      }
    }
    private string typeLoaderName;

    private void EmitIMTLoader(ITypeDefinition type, string mangledName, string mangledRuntimeTypeName) {
      Contract.Requires(type != null);
      Contract.Requires(mangledName != null);
      Contract.Requires(mangledRuntimeTypeName != null);

      // An interface will not have an IMT
      if (type.IsInterface) return;

      bool firstTime = true;
      foreach (var method in type.Methods) {
        Contract.Assume(method != null);
        var mangledMethodName = this.GetMangledMethodName(method);
        foreach (var interfaceMethod in MemberHelper.GetImplicitlyImplementedInterfaceMethods(method)) {
          Contract.Assume(interfaceMethod != null);
          firstTime = this.FillInImtSlot(mangledName, mangledRuntimeTypeName, mangledMethodName, interfaceMethod, firstTime);
        }
      }

      foreach (var explicitOverride in type.ExplicitImplementationOverrides) {
        Contract.Assume(explicitOverride != null);
        // We only care about overridden interface methods here
        if (explicitOverride.ImplementedMethod.ContainingType.ResolvedType.IsInterface) {
          var mangledMethodName = this.GetMangledMethodName(explicitOverride.ImplementingMethod);
          var methodReference = explicitOverride.ImplementedMethod;
          var interfaceMethod = methodReference.ResolvedMethod;
          firstTime = this.FillInImtSlot(mangledName, mangledRuntimeTypeName, mangledMethodName, interfaceMethod,
                                         firstTime);
        }
      }
    }

    private bool FillInImtSlot(string mangledName, string mangledRuntimeTypeName, string mangledMethodName,
                               IMethodDefinition interfaceMethod, bool firstTime) {
      Contract.Requires(mangledName != null);
      Contract.Requires(mangledRuntimeTypeName != null);
      Contract.Requires(mangledMethodName != null);
      Contract.Requires(interfaceMethod != null);

      if (firstTime) {
        // Get the address to the location that the IMT starts, therefore (baseAddress-1) would point to the hashmap that keeps track of 
        // all function pointers that caused an IMT slot to overflow.
        this.sourceEmitter.EmitString("baseAddress = (void **)(");
        this.sourceEmitter.EmitString(mangledName);
        this.sourceEmitter.EmitString("_typeObject");
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(" + sizeof(struct ");
        this.sourceEmitter.EmitString(mangledRuntimeTypeName);
        this.sourceEmitter.EmitString(") + sizeof(uintptr_t));");
        this.sourceEmitter.EmitNewLine();
        firstTime = false;
      }
      // Get the offset into the IMT
      this.sourceEmitter.EmitString("IMTOffset = ");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(interfaceMethod));
      this.sourceEmitter.EmitString("_id % IMTSIZE;");
      this.sourceEmitter.EmitNewLine();
      // If the value at the slot is 0 (When the object is allocated all IMT slots are zero-initialized) this slot has not been used yet. 
      // Therefore we can add the function pointer to the slot directly
      this.sourceEmitter.EmitString("if (*(baseAddress + IMTOffset) == 0 ) ");
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("*(baseAddress + IMTOffset) = (void *)&");
      this.sourceEmitter.EmitString(mangledMethodName);
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
      // We keep track of each interfaceID stored at each location in the IMT. This information is needed in the case of an overflow. 
      // In the case of an overflow we need to move the function pointer stored in the slot to the hashmap along with the function pointer 
      // that caused the overflow.
      this.sourceEmitter.EmitString("offsetToID[IMTOffset] = ");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(interfaceMethod));
      this.sourceEmitter.EmitString("_id;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("} ");
      // If the value at the slot is 1 (When an overflow occures we set the value at a slot the 1) we need to add the entry to the hashmap. 
      // This is done by a helper routine.
      this.sourceEmitter.EmitString("else if (*(baseAddress + IMTOffset) == (void *)1 ) ");
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("AddToIMTTable((uintptr_t)(*(baseAddress - 1)), ");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(interfaceMethod));
      this.sourceEmitter.EmitString("_id, (uintptr_t)&");
      this.sourceEmitter.EmitString(mangledMethodName);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("} ");
      this.sourceEmitter.EmitString("else ");
      // If the value at the slot is not 0 or 1 this means that the slot is currently in use and has a function pointer stored in it. 
      // In such a case we need to move the function stored in the slot to the hashmap along with the function pointer to be added. 
      // This is done by a helper routine. Once the function pointer is moved to the hashmap we set the value at the slot to 1.
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString(
        "UpdateIMTTable((uintptr_t)(*(baseAddress - 1)), offsetToID[IMTOffset], (uintptr_t)(*(baseAddress + IMTOffset)), ");
      this.sourceEmitter.EmitString(this.GetMangledMethodName(interfaceMethod));
      this.sourceEmitter.EmitString("_id, (uintptr_t)&");
      this.sourceEmitter.EmitString(mangledMethodName);
      this.sourceEmitter.EmitString(", (uintptr_t)(baseAddress - 1));");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("*(baseAddress + IMTOffset) = (void *)1;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitBlockClosingDelimiter("} ");
      this.sourceEmitter.EmitNewLine();
      return firstTime;
    }

    private void EmitTypeLoader(Hashtable<ITypeReference> closedStructuralTypeInstancesUsedInThisModule, IEnumerable<string>/*?*/ strings) {
      Contract.Requires(closedStructuralTypeInstancesUsedInThisModule != null);

      this.sourceEmitter.EmitString("void ");
      this.sourceEmitter.EmitString(this.TypeLoaderName);
      this.sourceEmitter.EmitString("()");
      this.sourceEmitter.EmitMethodBodyOpeningDelimiter("{");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("uintptr_t _module;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("void ** baseAddress;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uint32_t IMTOffset;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("uint32_t offsetToID[IMTSIZE];");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("if (");
      this.sourceEmitter.EmitString(this.TypeLoaderName);
      this.sourceEmitter.EmitString("_is_initialized) return;");
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString(typeLoaderName);
      this.sourceEmitter.EmitString("_is_initialized = 1;");
      this.sourceEmitter.EmitNewLine();

      this.LoadReferencedModules();
      this.LoadTypesDefinedInThisModule(closedStructuralTypeInstancesUsedInThisModule);
      this.LoadVmtsOfReferencedModules();

      //The collections referenced below will be null unless this module is the executable.
      if (strings != null) this.LoadStrings(strings);

      this.sourceEmitter.EmitBlockClosingDelimiter("}");
      this.sourceEmitter.EmitNewLine();
    }

    private void LoadReferencedModules() {
      var arefs = new SetOfObjects();
      foreach (var referencedAssembly in this.module.AssemblyReferences) {
        Contract.Assume(referencedAssembly != null);
        var unifiedIdentity = referencedAssembly.UnifiedAssemblyIdentity;
        if (!arefs.Add(unifiedIdentity)) continue;
        this.sourceEmitter.EmitString(new Mangler().Mangle(referencedAssembly.ResolvedAssembly));
        this.sourceEmitter.EmitString("_type_loader();");
        this.sourceEmitter.EmitNewLine();
      }
      foreach (var referencedModule in this.module.ModuleReferences) {
        Contract.Assume(referencedModule != null);
        this.sourceEmitter.EmitString(new Mangler().Mangle(referencedModule.ResolvedModule));
        this.sourceEmitter.EmitString("_type_loader();");
        this.sourceEmitter.EmitNewLine();
      }
    }

    private void LoadVmtsOfReferencedModules() {
      var types = new SetOfUints();
      foreach (var referencedAssembly in this.module.AssemblyReferences) {
        Contract.Assume(referencedAssembly != null);
        foreach (var type in referencedAssembly.ResolvedAssembly.GetAllTypes()) {
          Contract.Assume(type != null);
          if (!types.Add(type.InternedKey)) continue;
          if (type.InternedKey != this.host.PlatformType.SystemObject.InternedKey)
            this.GetVmtForType(type);
        }
      }
      foreach (var referencedModule in this.module.ModuleReferences) {
        Contract.Assume(referencedModule != null);
        foreach (var type in referencedModule.ResolvedModule.GetAllTypes()) {
          Contract.Assume(type != null);
          if (!types.Add(type.InternedKey)) continue;
          if (type.InternedKey != this.host.PlatformType.SystemObject.InternedKey)
            this.GetVmtForType(type);
        }
      }
    }

    [ContractVerification(false)]
    private void LoadTypesDefinedInThisModule(Hashtable<ITypeReference> closedStructuralTypeInstancesUsedInThisModule) {
      Contract.Requires(closedStructuralTypeInstancesUsedInThisModule != null);

      this.sourceEmitter.EmitString("GetNewModule((uintptr_t)&_module);");
      this.sourceEmitter.EmitNewLine();
      this.CreateNewVmtForType(this.host.PlatformType.SystemObject.ResolvedType);
      List<IMethodReference> systemObjectVmt = this.vmts[this.host.PlatformType.SystemObject.ResolvedType.InternedKey];
      Hashtable systemObjectVmtHashTable = this.vmtHashTable[this.host.PlatformType.SystemObject.ResolvedType.InternedKey];
      var mangledRuntimeTypeName = this.GetMangledTypeName(this.runtimeType);

      //First create objects for all types in this module (referenced modules already have had their types loaded).
      foreach (var type in this.module.GetAllTypes()) {
        Contract.Assume(type != null);        
        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue; // We do not generate type objects for generic templates
        List<IMethodReference> vmt;
        if (type.InternedKey != this.host.PlatformType.SystemObject.InternedKey)
          vmt = this.GetVmtForType(type);
        else
          vmt = systemObjectVmt;
        this.EmitCreateNewTypeObject(vmt, mangledRuntimeTypeName, type);
      }

      //We also need objects for the structural type instances used in this module since some of them might be base classes.
      foreach (var type in closedStructuralTypeInstancesUsedInThisModule.Values) {
        Contract.Assume(type != null);
        List<IMethodReference> vmt = this.GetVmtForType(type.ResolvedType);
        this.EmitCreateNewTypeObject(vmt, mangledRuntimeTypeName, type.ResolvedType, isStructuralType: true);
      }

      this.sourceEmitter.EmitNewLine();
      //Now set the base class of each of the type objects created above.
      //A type cannot be set as the base class of another type before its own base class has been set,
      //so EmitSetBaseClass will walk the base class chain to ensure this.
      var alreadyProcessed = new SetOfUints(); //hence the need for this set.
      foreach (var type in this.module.GetAllTypes()) {
        Contract.Assume(type != null);

        if (TypeHelper.HasOwnOrInheritedTypeParameters(type)) continue; // We do not generate type objests for generic templates
        var typeObjectName = this.GetMangledTypeName(type)+"_typeObject";
        this.EmitTypeLoadingCallsFor(type, typeObjectName, alreadyProcessed);
      }
      foreach (var type in closedStructuralTypeInstancesUsedInThisModule.Values) {
        Contract.Assume(type != null);
        var typeObjectName = this.GetMangledTypeName(type)+"_typeObject";
        this.EmitTypeLoadingCallsFor(type, typeObjectName, alreadyProcessed);
      }
    }

    private void EmitTypeLoadingCallsFor(ITypeReference typeRef, string typeObjectName, SetOfUints alreadyLoadedTypes) {
      Contract.Requires(typeRef != null);
      Contract.Requires(typeObjectName != null);
      Contract.Requires(alreadyLoadedTypes != null);

      var genericTypeInstance = typeRef as IGenericTypeInstanceReference;
      if (genericTypeInstance != null)
        this.EmitTypeLoadingCallsFor(genericTypeInstance, typeObjectName, alreadyLoadedTypes);
      else {
        var nestedType = typeRef as INestedTypeReference;
        if (nestedType != null)
          this.EmitTypeLoadingCallsFor(nestedType, typeObjectName, alreadyLoadedTypes);
        else {
          var arrayType = typeRef as IArrayTypeReference;
          if (arrayType != null)
            this.EmitTypeLoadingCallsFor(arrayType, typeObjectName, alreadyLoadedTypes);
          else {
            var pointerTypeRef = typeRef as IPointerTypeReference;
            if (pointerTypeRef != null)
              this.EmitTypeLoadingCallsFor(pointerTypeRef, typeObjectName, alreadyLoadedTypes);
            else {
              var managedPointerTypeRef = typeRef as IManagedPointerTypeReference;
              if (managedPointerTypeRef != null)
                this.EmitTypeLoadingCallsFor(managedPointerTypeRef, typeObjectName, alreadyLoadedTypes);
              else {
                this.EmitSetTypeAndSetBaseClass(typeRef, typeObjectName, alreadyLoadedTypes);
              }
            }
          }
        }
      }
    }

    [ContractVerification(false)]
    private void EmitTypeLoadingCallsFor(IGenericTypeInstanceReference genericTypeInstance, string typeObjectName, SetOfUints alreadyLoadedTypes) {
      Contract.Requires(genericTypeInstance != null);
      Contract.Requires(typeObjectName != null);
      Contract.Requires(alreadyLoadedTypes != null);

      this.EmitSetTypeAndSetBaseClass(genericTypeInstance.ResolvedType, typeObjectName, alreadyLoadedTypes);
      this.sourceEmitter.EmitString("AllocateForGenericArguments(");
      this.sourceEmitter.EmitString(typeObjectName);
      this.sourceEmitter.EmitString(", " + genericTypeInstance.GenericType.GenericParameterCount + ");");
      this.sourceEmitter.EmitNewLine();
      int count = 0;
      foreach (var genericArgument in genericTypeInstance.GenericArguments) {
        Contract.Assume(genericArgument != null);
        this.sourceEmitter.EmitString("SetGenericArgument(");
        this.sourceEmitter.EmitString(typeObjectName);
        this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString(this.GetMangledTypeName(genericArgument)+"_typeObject");
        this.sourceEmitter.EmitString(", " + count + ");");
        this.sourceEmitter.EmitNewLine();
        count++;
      }
    }

    private void EmitTypeLoadingCallsFor(INestedTypeReference nestedType, string typeObjectName, SetOfUints alreadyLoadedTypes) {
      Contract.Requires(nestedType != null);
      Contract.Requires(typeObjectName != null);
      Contract.Requires(alreadyLoadedTypes != null);

      var containingTypeObjectName = this.GetMangledTypeName(nestedType.ContainingType)+"_typeObject";
      this.EmitTypeLoadingCallsFor(nestedType.ContainingType, containingTypeObjectName, alreadyLoadedTypes);
      this.EmitSetTypeAndSetBaseClass(nestedType.ResolvedType, typeObjectName, alreadyLoadedTypes);
      this.sourceEmitter.EmitString("SetDeclaringType(");
      this.sourceEmitter.EmitString(typeObjectName);
      this.sourceEmitter.EmitString(", ");
      this.sourceEmitter.EmitString(containingTypeObjectName);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitTypeLoadingCallsFor(IArrayTypeReference arrayType, string typeObjectName, SetOfUints alreadyLoadedTypes) {
      Contract.Requires(arrayType != null);
      Contract.Requires(typeObjectName != null);
      Contract.Requires(alreadyLoadedTypes != null);

      var elementTypeObjectName = this.GetMangledTypeName(arrayType.ElementType)+"_typeObject";
      this.EmitTypeLoadingCallsFor(arrayType.ElementType, elementTypeObjectName, alreadyLoadedTypes);
      if (arrayType.IsVector) {
        this.sourceEmitter.EmitString("GetVectorType(");
        this.sourceEmitter.EmitString(elementTypeObjectName);
      } else {
        this.sourceEmitter.EmitString("GetMatrixType(");
        this.sourceEmitter.EmitString(elementTypeObjectName);
        this.sourceEmitter.EmitString(", "+arrayType.Rank);
      }
      this.sourceEmitter.EmitString(", (uintptr_t)&");
      this.sourceEmitter.EmitString(typeObjectName);
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitTypeLoadingCallsFor(IPointerTypeReference pointerType, string mangledName, SetOfUints alreadyLoadedTypes) {
      Contract.Requires(pointerType != null);
      Contract.Requires(mangledName != null);
      Contract.Requires(alreadyLoadedTypes != null);
      //TODO: implement this
      this.EmitSetTypeAndSetBaseClass((ITypeReference)pointerType, mangledName, alreadyLoadedTypes);
    }

    private void EmitTypeLoadingCallsFor(IManagedPointerTypeReference managedPointerType, string mangledName, SetOfUints alreadyLoadedTypes) {
      Contract.Requires(managedPointerType != null);
      Contract.Requires(mangledName != null);
      Contract.Requires(alreadyLoadedTypes != null);
      //TODO: implement this
      this.EmitSetTypeAndSetBaseClass((ITypeReference)managedPointerType, mangledName, alreadyLoadedTypes);
    }

    private void LoadStrings(IEnumerable<string> strings) {
      Contract.Requires(strings != null); 

      foreach (var str in strings) {
        Contract.Assume(str != null);
        this.sourceEmitter.EmitString("CtorCharPtr((uintptr_t)&L\"");
        this.sourceEmitter.EmitString(this.Escaped(str));
        this.sourceEmitter.EmitString("\", (uintptr_t)&");
        this.sourceEmitter.EmitString(new Mangler().Mangle(str));
        this.sourceEmitter.EmitString(");");
        this.sourceEmitter.EmitNewLine();
      }
    }

    private string Escaped(string str) {
      Contract.Requires(str != null);
      Contract.Ensures(Contract.Result<string>() != null);

      var n = str.Length;
      var i = 0;
      while (i < n) {
        var ch = str[i++];
        if (ch < ' ' || ch > '~' || ch == '\\' || ch == '"') { i--; break; }
      }
      if (i == n) return str;
      var sb = new StringBuilder();
      sb.Append(str, 0, i);
      while (i < n) {
        var ch = str[i++];
        if (ch < ' ' || ch > '~')
          if (ch > 0xA0)
            sb.Append("\\u"+((uint)ch).ToString("x4"));
          else
            sb.Append("\\x"+((uint)ch).ToString("x2"));
        else {
          if (ch == '\\' || ch == '"') sb.Append('\\');
          sb.Append(ch);
        }
      }
      return sb.ToString();
    }

    private void EmitTypeObjectReference(ITypeReference type) {
      Contract.Requires(type != null);

      this.sourceEmitter.EmitString(this.GetMangledTypeName(type));
      this.sourceEmitter.EmitString("_typeObject");
    }

    /*  The structure of the type object
            ---------------
            | RuntimeType |
            |-------------|
            |Pointer to   |
            |IMT HashTable|
            |--------------
            |  IMT slots  |
            |(fixed size) |
            |-------------|
            |  VMTslots   |
            |(var size)   |
            ---------------
     */
    [ContractVerification(false)] //timeout
    private void EmitCreateNewTypeObject(List<IMethodReference> vmt, string mangledRuntimeTypeName, ITypeDefinition type, bool isStructuralType = false) {
      Contract.Requires(mangledRuntimeTypeName != null);
      Contract.Requires(type != null);

      this.sourceEmitter.EmitNewLine();
      var mangledName = this.GetMangledTypeName(type);
      if (isStructuralType) {
        this.sourceEmitter.EmitString("if (");
        this.sourceEmitter.EmitString(mangledName);
        this.sourceEmitter.EmitString("_typeObject == 0) ");
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
      }

      int vmtSize = (vmt == null ? 0 : vmt.Count);
      this.sourceEmitter.EmitString(mangledName);
      this.sourceEmitter.EmitString("_typeObject = (uintptr_t)calloc(1, sizeof(struct ");
      this.sourceEmitter.EmitString(mangledRuntimeTypeName);
      this.sourceEmitter.EmitString(")");
      if (!type.IsInterface) {
        // If the type is an interface there is no point allocating space for an IMT
        this.sourceEmitter.EmitString(" + sizeof(uintptr_t) * (" + vmtSize);
        this.sourceEmitter.EmitString(" + IMTSIZE + 1)");
      }
      // The extra slot is to hold a pointer to the hashtable that handles overflows
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();

      this.EmitAdjustPointerToDataFromHeader(mangledName + "_typeObject");
      this.sourceEmitter.EmitString("InitializeRuntimeType(");
      this.sourceEmitter.EmitString(mangledName);
      this.sourceEmitter.EmitString("_typeObject, _module, ");
      var objectWithToken = type as IMetadataObjectWithToken;
      if (objectWithToken != null)
        this.sourceEmitter.EmitString(objectWithToken.TokenValue.ToString());
      else
        this.sourceEmitter.EmitString("0");
      if (type.IsInterface)
        this.sourceEmitter.EmitString(", 0, ");
      else {
        this.sourceEmitter.EmitString(", sizeof(struct ");
        this.sourceEmitter.EmitString(mangledName);
        this.sourceEmitter.EmitString("), ");
      }
      this.sourceEmitter.EmitString(IteratorHelper.EnumerableCount(type.ResolvedType.Interfaces) + ", ");
      this.sourceEmitter.EmitString(GetTypeAttributesFor(type)+", ");
      this.sourceEmitter.EmitString(this.GetFlagsFor(type)+", ");
      if (TypeHelper.TypesAreEquivalent(type, this.host.PlatformType.SystemObject))
        this.sourceEmitter.EmitString("99, ");
      else
        this.sourceEmitter.EmitString(((uint)TypeHelper.GetSytemTypeCodeFor(type)) + ", ");
      var defaultConstructor = TypeHelper.GetMethod(type, this.host.NameTable.GetNameFor(".ctor"));
      if (defaultConstructor == Dummy.MethodDefinition)
        this.sourceEmitter.EmitString("0");
      else
        this.sourceEmitter.EmitString("(uintptr_t)&" + this.GetMangledMethodName(defaultConstructor));
      this.sourceEmitter.EmitString(");");
      this.sourceEmitter.EmitNewLine();

      if (type.IsInterface) {
        foreach (var method in type.Methods) {
          Contract.Assume(method != null);
          this.sourceEmitter.EmitString(this.GetMangledMethodName(method));
          this.sourceEmitter.EmitString("_id = interfaceMethodIDCounter;");
          this.sourceEmitter.EmitNewLine();
          this.sourceEmitter.EmitString("interfaceMethodIDCounter++;");
          this.sourceEmitter.EmitNewLine();
        }
      }

      uint count = 0;
      foreach (var iface in type.Interfaces) {
        Contract.Assume(iface != null);
        this.sourceEmitter.EmitString("((uintptr_t*)((");
        this.EmitTypeReference(this.host.PlatformType.SystemType, storageLocation: false);
        this.sourceEmitter.EmitString(") ");
        this.sourceEmitter.EmitString("(");
        this.sourceEmitter.EmitString(mangledName);
        this.sourceEmitter.EmitString("_typeObject");
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString("))->");
        this.sourceEmitter.EmitString(this.GetMangledFieldName(this.directInterfacesField));
        this.sourceEmitter.EmitString(")[" + count + "] = ");
        this.sourceEmitter.EmitString(this.GetMangledTypeName(iface) + "_typeObject");
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
        count++;
      }

      this.EmitIMTLoader(type, mangledName, mangledRuntimeTypeName);

      if (vmt != null) {
        this.sourceEmitter.EmitString("baseAddress = (void **)(");
        this.sourceEmitter.EmitString(mangledName);
        this.sourceEmitter.EmitString("_typeObject");
        this.EmitAdjustPointerToHeaderFromData();
        this.sourceEmitter.EmitString(" + sizeof(struct ");
        this.sourceEmitter.EmitString(mangledRuntimeTypeName);
        this.sourceEmitter.EmitString(") + ((1 + IMTSIZE) * sizeof(uintptr_t)));");
        this.sourceEmitter.EmitNewLine();

        this.EmitVmtEntries(vmt, type);
      }
      if (isStructuralType) {
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitNewLine();
      }
    }

    private static uint GetTypeAttributesFor(ITypeDefinition typeDefinition) {
      Contract.Requires(typeDefinition != null);
      var attributes = (TypeAttributes)0;
      var nestedType = typeDefinition as INestedTypeDefinition;
      if (nestedType != null)
        attributes = GetNestedTypeVisibility(nestedType);
      else
        attributes = TypeHelper.TypeVisibilityAsTypeMemberVisibility(typeDefinition) == TypeMemberVisibility.Public ? TypeAttributes.Public : TypeAttributes.NotPublic;
      if (typeDefinition.Layout == LayoutKind.Sequential) attributes |= TypeAttributes.SequentialLayout;
      if (typeDefinition.Layout == LayoutKind.Explicit) attributes |= TypeAttributes.ExplicitLayout;
      if (typeDefinition.IsInterface) attributes |= TypeAttributes.Interface;
      if (typeDefinition.IsAbstract) attributes |= TypeAttributes.Abstract;
      if (typeDefinition.IsSealed) attributes |= TypeAttributes.Sealed;
      if (typeDefinition.IsSpecialName) attributes |= TypeAttributes.SpecialName;
      if (typeDefinition.IsRuntimeSpecial) attributes |= TypeAttributes.RTSpecialName;
      if (typeDefinition.IsComObject) attributes |= TypeAttributes.Import;
      if (typeDefinition.IsSerializable) attributes |= TypeAttributes.Serializable;
      if (typeDefinition.StringFormat == StringFormatKind.Unicode) attributes |= TypeAttributes.UnicodeClass;
      if (typeDefinition.StringFormat == StringFormatKind.AutoChar) attributes |= TypeAttributes.AutoClass;
      if (typeDefinition.HasDeclarativeSecurity) attributes |= TypeAttributes.HasSecurity;
      if (typeDefinition.IsBeforeFieldInit) attributes |= TypeAttributes.BeforeFieldInit;
      return (uint)attributes;
    }

    private static TypeAttributes GetNestedTypeVisibility(ITypeDefinitionMember typeDefinitionMember) {
      Contract.Requires(typeDefinitionMember != null);
      switch (typeDefinitionMember.Visibility) {
        case TypeMemberVisibility.Assembly: return TypeAttributes.NestedAssembly;
        case TypeMemberVisibility.Family: return TypeAttributes.NestedFamily;
        case TypeMemberVisibility.FamilyAndAssembly: return TypeAttributes.NestedFamANDAssem;
        case TypeMemberVisibility.FamilyOrAssembly: return TypeAttributes.NestedFamORAssem;
        case TypeMemberVisibility.Private: return TypeAttributes.NestedPrivate;
        case TypeMemberVisibility.Public: return TypeAttributes.NestedPublic;
      }
      return 0;
    }


    //Keep this in sync with the definition in MinCore.
    [Flags]
    internal enum TypeFlags {
      AttributesAreValid = 0x00001,
      ElementTypeIsValid = 0x00002,
      FlagsAreValid      = 0x00004,
      IsArray            = 0x00008,
      IsByRef            = 0x00010,
      IsCOMObject        = 0x00020,
      IsContextful       = 0x00040,
      IsDelegate         = 0x00080,
      IsEnum             = 0x00100,
      IsGenericParameter = 0x00200,
      IsGenericTemplate  = 0x00400,
      IsMarhalByRef      = 0x00800,
      IsPointer          = 0x01000,
      IsPrimitive        = 0x02000,
      IsValueType        = 0x04000,
      IsVector           = 0x08000,
      TypeCodeIsValid    = 0x10000,
    }

    [ContractVerification(false)]
    private uint GetFlagsFor(ITypeDefinition type) {
      Contract.Requires(type != null); 

      TypeFlags flags = TypeFlags.AttributesAreValid|TypeFlags.FlagsAreValid;
      var arrayType = type as IArrayType;
      if (arrayType != null) {
        flags |= TypeFlags.IsArray;
        if (arrayType.IsVector) flags |= TypeFlags.IsVector;
      } else if (type is IManagedPointerType)
        flags |= TypeFlags.IsByRef;
      else if (type.IsDelegate)
        flags |= TypeFlags.IsDelegate;
      else if (type.IsEnum)
        flags |= TypeFlags.IsEnum;
      else if (type is IGenericParameter)
        flags |= TypeFlags.IsGenericParameter;
      else if (type.IsGeneric)
        flags |= TypeFlags.IsGenericTemplate;
      else if (type is IPointerType)
        flags |= TypeFlags.IsPointer;
      if (type.IsComObject)
        flags |= TypeFlags.IsCOMObject;
      if (TypeHelper.Type1DerivesFromType2(type, this.contextBoundObject, resolveTypes: true))
        flags |= TypeFlags.IsContextful;
      if (TypeHelper.Type1DerivesFromType2(type, this.marshalByRefObject, resolveTypes: true))
        flags |= TypeFlags.IsMarhalByRef;
      var namespaceTypeDefinition = type as INamespaceTypeDefinition;
      if (type.IsValueType)
        flags |= TypeFlags.IsValueType;
      return (uint)flags;
    }

    private void EmitVmtEntries(List<IMethodReference> vmt, ITypeDefinition type) {
      Contract.Requires(vmt != null);
      Contract.Requires(type != null);

      foreach (IMethodReference method in vmt) {
        Contract.Assume(method != null);
        this.sourceEmitter.EmitString("*(baseAddress++) = (void *)&");
        this.sourceEmitter.EmitString(this.GetMangledMethodName(method));
        this.sourceEmitter.EmitString(";");
        this.sourceEmitter.EmitNewLine();
      }
    }

    private List<IMethodReference> GetVmtForType(ITypeDefinition type) {
      Contract.Requires(type != null);

      List<IMethodReference> vmt = null;
      vmt = this.vmts[type.InternedKey];
      if (vmt != null)
        return vmt;
      foreach (var baseType in type.BaseClasses) {
        List<IMethodReference> baseVmt;
        Hashtable baseVmtHashtable;
        if (baseType.InternedKey == this.host.PlatformType.SystemObject.InternedKey) {
          baseVmt = this.vmts[this.host.PlatformType.SystemObject.ResolvedType.InternedKey];
          baseVmtHashtable = this.vmtHashTable[this.host.PlatformType.SystemObject.ResolvedType.InternedKey];
        } else {
          baseVmt = this.GetVmtForType(baseType.ResolvedType);
          baseVmtHashtable = this.vmtHashTable[baseType.ResolvedType.InternedKey];
        }
        Contract.Assume(baseVmt != null);
        Contract.Assume(baseVmtHashtable != null);
        vmt = this.UpdateVmtForType(baseVmt, baseVmtHashtable, type, baseType);
        break;
      }

      Hashtable hashtable = this.vmtHashTable[type.InternedKey];
      if (hashtable == null) {
        Contract.Assume(vmt == null);
        vmt = new List<IMethodReference>(0);
      } else {
        foreach (var explicitOverride in type.ExplicitImplementationOverrides) {
          // We skip over interfaces here, they will be handled seperatly when the IMT is emitted.
          if (!explicitOverride.ImplementedMethod.ContainingType.ResolvedType.IsInterface) {
            Contract.Assume(vmt != null);
            uint index = hashtable[explicitOverride.ImplementedMethod.InternedKey];
            Contract.Assume(index < vmt.Count);
            vmt[(int)index] = explicitOverride.ImplementingMethod;
            hashtable[explicitOverride.ImplementingMethod.InternedKey] = index;
          }
        }
      }
      this.vmts.Add(type.InternedKey, vmt);
      return vmt;
    }

    private void CreateNewVmtForType(ITypeReference type) {
      Contract.Requires(type != null);

      List<IMethodReference> vmt = new List<IMethodReference>();
      Hashtable hashTable = new Hashtable();
      uint count = 0;
      foreach (var method in type.ResolvedType.Methods) {
        if (method.IsVirtual) {
          vmt.Add(method.ResolvedMethod);
          hashTable[method.ResolvedMethod.InternedKey] = count;
          count++;
        }
      }
      this.vmts[type.InternedKey] = vmt;
      this.vmtHashTable[type.InternedKey] = hashTable;
    }

    private List<IMethodReference> UpdateVmtForType(List<IMethodReference> baseVmt, Hashtable baseVmtHashtable, ITypeReference type, ITypeReference baseClass) {
      Contract.Requires(baseVmt != null);
      Contract.Requires(baseVmtHashtable != null);
      Contract.Requires(type != null);
      Contract.Requires(baseClass != null);

      List<IMethodReference> vmt = new List<IMethodReference>(baseVmt);
      Hashtable hashtable = new Hashtable();
      uint count = 0;
      foreach (var value in baseVmt) {
        Contract.Assume(value != null);
        hashtable[value.InternedKey] = count;
        count++;
      }
      foreach (var method in type.ResolvedType.Methods) {
        Contract.Assume(method != null);
        if (method.IsVirtual) {
          IMethodDefinition baseMethod;
          if (method.IsNewSlot || (baseMethod = MemberHelper.GetImplicitlyOverriddenBaseClassMethod(method)) == Dummy.MethodDefinition) {
            vmt.Add(method.ResolvedMethod);
            hashtable[method.ResolvedMethod.InternedKey] = count;
            count++;
          } else {
            var bm = baseMethod;
            uint index = hashtable[bm.InternedKey];
            Contract.Assume(index < vmt.Count);
            vmt[(int)index] = method;
            hashtable[method.InternedKey] = index;
          }
        }
      }
      this.vmtHashTable[type.InternedKey] = hashtable;
      return vmt;
    }

    private void EmitSetTypeAndSetBaseClass(ITypeReference type, string mangledName, SetOfUints alreadyProcessed) {
      Contract.Requires(type != null);
      Contract.Requires(mangledName != null);
      Contract.Requires(alreadyProcessed != null);

      if (!alreadyProcessed.Add(type.InternedKey)) return;

      bool typeMayAlreadyBeInitialized = TypeHelper.GetDefiningUnit(type.ResolvedType) != this.module;
      if (typeMayAlreadyBeInitialized) {
        this.sourceEmitter.EmitString("if (*((uintptr_t*)(");
        this.sourceEmitter.EmitString(mangledName);
        this.sourceEmitter.EmitString("+sizeof(void*)*2)) == 0) ");
        this.sourceEmitter.EmitBlockOpeningDelimiter("{");
        this.sourceEmitter.EmitNewLine();
      }

      this.sourceEmitter.EmitString("SetType(");
      this.sourceEmitter.EmitString(mangledName);
      this.sourceEmitter.EmitString(", ");
      this.sourceEmitter.EmitString(this.GetMangledTypeName(this.host.PlatformType.SystemType));
      this.sourceEmitter.EmitString("_typeObject);");
      this.sourceEmitter.EmitNewLine();

      foreach (var baseClass in type.ResolvedType.BaseClasses) {
        var mangledBaseClassName = this.GetMangledTypeName(baseClass)+"_typeObject";
        this.EmitSetTypeAndSetBaseClass(baseClass, mangledBaseClassName, alreadyProcessed);
        this.sourceEmitter.EmitString("SetBaseClass(");
        this.sourceEmitter.EmitString(mangledName);
        this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString(mangledBaseClassName);
        this.sourceEmitter.EmitString(");");
        this.sourceEmitter.EmitNewLine();
        break;
      }

      if (typeMayAlreadyBeInitialized) {
        this.sourceEmitter.EmitBlockClosingDelimiter("}");
        this.sourceEmitter.EmitNewLine();
      }

    }

    private void EmitSetInterface(ITypeReference type, string mangledName, SetOfUints alreadyProcessed) {
      Contract.Requires(type != null);
      Contract.Requires(mangledName != null);
      Contract.Requires(alreadyProcessed != null);

      if (!alreadyProcessed.Add(type.InternedKey)) return;
    }

    private List<IFieldDefinition> GetInstanceFields(ITypeDefinition type) {
      Contract.Requires(type != null);

      List<IFieldDefinition> result = new List<IFieldDefinition>();
      this.AddFields(type, result);

      //TODO: order the fields, either as specified in the metadata (i.e. sequential or explicit)
      //or by looking at alignment boundaries.
      return result;
    }

    private void AddFields(ITypeDefinition type, List<IFieldDefinition> fields) {
      Contract.Requires(type != null);
      Contract.Requires(fields != null);

      foreach (var baseClassRef in type.BaseClasses) {
        Contract.Assume(baseClassRef != null);
        var baseClass = baseClassRef.ResolvedType;
        this.AddFields(baseClass, fields);
      }
      foreach (var field in type.Fields) {
        if (field.IsStatic) continue;
        fields.Add(field);
      }
    }

    private void EmitStruct(ITypeDefinition type, SetOfUints emittedTypes) {
      Contract.Requires(type != null);
      Contract.Requires(emittedTypes != null);

      if (!emittedTypes.Add(type.InternedKey)) return;

      var instanceType = type;
      var instanceFields = this.GetInstanceFields(instanceType);
      foreach (var field in instanceFields) {
        Contract.Assume(field != null);
        var ft = field.Type.ResolvedType;
        if (ft is IGenericParameter) continue;
        if (ft.IsValueType && (ft is IGenericTypeInstance || TypeHelper.GetDefiningUnit(ft) == this.module) && !emittedTypes.Contains(ft.InternedKey)) {
          this.EmitStruct(ft, emittedTypes);
        }
      }
      if (this.sourceEmitter.LeaveBlankLinesBetweenNamespaceMembers) this.sourceEmitter.EmitNewLine();
      var mangledName = this.GetMangledTypeName(type);
      this.sourceEmitter.EmitString("extern uintptr_t "+mangledName+"_typeObject;");     
      this.sourceEmitter.EmitNewLine();
      if (type.IsInterface) return;
      this.sourceEmitter.EmitString("#ifndef struct_");
      this.sourceEmitter.EmitString(mangledName);
      this.sourceEmitter.EmitNewLine();
      this.sourceEmitter.EmitString("#define struct_");
      this.sourceEmitter.EmitString(mangledName);
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("struct ");
      this.sourceEmitter.EmitString(mangledName);
      this.sourceEmitter.EmitTypeBodyOpeningDelimiter(" {");
      this.sourceEmitter.EmitNewLine();
      foreach (var field in instanceFields) {
        Contract.Assume(field != null);
        this.EmitInstanceField(field);
      }
      if (instanceFields.Count == 0) {
        var size = type.SizeOf;
        if (size == 0) size = 1;
        this.sourceEmitter.EmitString("char dummy["+size+"];");
        this.sourceEmitter.EmitNewLine();
      }
      this.sourceEmitter.EmitBlockClosingDelimiter("};");
      this.sourceEmitter.EmitNewLine();
      if (!type.IsValueType) {
        this.sourceEmitter.EmitString("#endif");
        this.sourceEmitter.EmitNewLine();
        return;
      }

      //Emit unboxed version of type.
      uint fieldCount = 0;
      this.sourceEmitter.EmitString("struct ");
      this.sourceEmitter.EmitString(mangledName);
      this.sourceEmitter.EmitString("_unboxed");
      this.sourceEmitter.EmitTypeBodyOpeningDelimiter(" {");
      this.sourceEmitter.EmitNewLine();
      foreach (var field in instanceFields) {
        Contract.Assume(field != null);
        if (field.ContainingTypeDefinition != instanceType) continue;
        fieldCount++;
        this.EmitInstanceField(field);
      }
      if (fieldCount == 0) {
        var size = type.SizeOf;
        if (size == 0) size = 1;
        this.sourceEmitter.EmitString("char dummy["+size+"];");
        this.sourceEmitter.EmitNewLine();
      }
      this.sourceEmitter.EmitBlockClosingDelimiter("};");
      this.sourceEmitter.EmitNewLine();

      this.sourceEmitter.EmitString("#endif");
      this.sourceEmitter.EmitNewLine();
    }

    private void EmitInstanceField(IFieldDefinition field) {
      Contract.Requires(field != null);

      this.EmitTypeReference(field.Type, storageLocation: true);
      this.sourceEmitter.EmitString(" ");
      this.sourceEmitter.EmitString(this.GetMangledFieldName(field));
      this.sourceEmitter.EmitString(";");
      this.sourceEmitter.EmitNewLine();
    }

    private bool HasStaticConstructor(ITypeReference type) {
      Contract.Requires(type != null);
      var t = this.typesWithStaticConstructors[type.InternedKey];
      if (t != null) return !(t is Dummy);
      var td = type.ResolvedType;
      foreach (var mem in td.GetMembersNamed(this.host.NameTable.Cctor, ignoreCase: false)) {
        var meth = mem as IMethodDefinition;
        if (meth != null && meth.IsStaticConstructor) {
          this.typesWithStaticConstructors[type.InternedKey] = td;
          return true;
        }
      }
      this.typesWithStaticConstructors[type.InternedKey] = Dummy.TypeDefinition;
      return false;
    }

    private string GetMangledFieldName(IFieldReference field) {
      Contract.Requires(field != null);
      var result = this.mangledFieldName[field.InternedKey];
      if (result == null) {
        result = new Mangler().Mangle(field);
        this.mangledFieldName[field.InternedKey] = result;
      }
      return result;
    }

    private string GetMangledTypeName(ITypeReference type) {
      Contract.Requires(type != null);

      var result = this.mangledTypeName[type.InternedKey];
      if (result == null) {
        result = new Mangler().Mangle(type);
        this.mangledTypeName[type.InternedKey] = result;
      }
      return result;
    }

    private string GetSanitizedName(IName name) {
      Contract.Requires(name != null);

      var result = this.sanitizedName[(uint)name.UniqueKey];
      if (result == null) {
        var sb = new StringBuilder();
        this.AppendSanitizedName(sb, name.Value);
        result = sb.ToString();
        this.sanitizedName[(uint)name.UniqueKey] = result;
      }
      return result;
    }

    private void AppendMangledName(StringBuilder sb, ITypeReference type) {
      Contract.Requires(sb != null);
      Contract.Requires(type != null);

      if (sb.Length == 0) { sb.Append('_'); sb.Append(type.InternedKey); }
      var nestedType = type as INestedTypeReference;
      if (nestedType != null) {
        this.AppendMangledName(sb, nestedType.ContainingType);
        sb.Append('_');
        this.AppendSanitizedName(sb, nestedType.Name.Value);
        return;
      }
      var namespaceType = type as INamespaceTypeReference;
      Contract.Assume(namespaceType != null);
      this.AppendMangledName(sb, namespaceType.ContainingUnitNamespace);
      sb.Append('_');
      this.AppendSanitizedName(sb, namespaceType.Name.Value);
    }

    private void AppendMangledName(StringBuilder sb, IUnitNamespaceReference unitNamespace) {
      Contract.Requires(sb != null);
      Contract.Requires(unitNamespace != null);

      var nestedNamespace = unitNamespace as INestedUnitNamespaceReference;
      if (nestedNamespace == null) return;
      this.AppendMangledName(sb, nestedNamespace.ContainingUnitNamespace);
      sb.Append('_');
      this.AppendSanitizedName(sb, nestedNamespace.Name.Value);
    }

    private void AppendSanitizedName(StringBuilder sb, string name) {
      Contract.Requires(sb != null);
      Contract.Requires(name != null);

      if (name.Length < 1) return;
      var firstChar = name[0];
      if (firstChar == '_' || '0' <= firstChar && firstChar <= '9')
        sb.Append('_');
      foreach (var ch in name) {
        if (('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || ('A' <= ch && ch <= 'Z') || ch == '_')
          sb.Append(ch);
        else if (char.IsLetterOrDigit(ch))
          sb.Append(ch);
        else
          sb.Append('_');
      }
    }

  }
}
