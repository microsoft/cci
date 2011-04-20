using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System;
using Microsoft.Cci.UtilityDataStructures;
using System.Diagnostics.Contracts;

namespace Microsoft.Cci.ReflectionEmitter {
  /// <summary>
  /// An object that provides methods to map CCI metadata references to corresponding System.Type and System.Reflection.* objects.
  /// The object maintains a cache of mappings and should typically be used for doing many mappings.
  /// </summary>
  public class ReflectionMapper {

    /// <summary>
    /// An object that provides methods to map CCI metadata references to corresponding System.Type and System.Reflection.* objects.
    /// The object maintains a cache of mappings and should typically be used for doing many mappings.
    /// </summary>
    public ReflectionMapper() {
      this.mappingVisitor = new MappingVisitorForTypes(this);
    }

    Dictionary<AssemblyIdentity, Assembly> assemblyMap = new Dictionary<AssemblyIdentity, Assembly>(16);
    Dictionary<IModule, Module> moduleMap = new Dictionary<IModule, Module>(16);
    DoubleHashtable<FieldInfo> fieldMap = new DoubleHashtable<FieldInfo>(2048*4); //TODO: use a hashtable and the newly defined InternKey
    Hashtable<MethodBase> methodMap = new Hashtable<MethodBase>(2048*8);
    DoubleHashtable<MemberInfo[]> membersMap = new DoubleHashtable<MemberInfo[]>(2048*8);
    Hashtable<Type> typeMap = new Hashtable<Type>(2048);
    MappingVisitorForTypes mappingVisitor;

    /// <summary>
    /// Returns a "live" System.Reflection.Assembly instance that provides reflective access to the referenced assembly. 
    /// If the assembly cannot be found or cannot be loaded, the result is null.
    /// </summary>
    public Assembly/*?*/ GetAssembly(IAssemblyReference/*?*/ assemblyReference) {
      if (assemblyReference == null) return null;
      var ident = assemblyReference.AssemblyIdentity;
      Assembly result = null;
      if (!this.assemblyMap.TryGetValue(ident, out result)) {
        AssemblyName name = new AssemblyName();
        if (!String.IsNullOrEmpty(ident.Location))
          name.CodeBase = new Uri(ident.Location).ToString();
        name.CultureInfo = new System.Globalization.CultureInfo(ident.Culture);
        name.Name = ident.Name.Value;
        name.SetPublicKeyToken(new List<byte>(ident.PublicKeyToken).ToArray());
        name.Version = ident.Version;
        try {
          result = Assembly.Load(name);
        } catch (System.UriFormatException) {
        } catch (System.IO.FileNotFoundException) {
        } catch (System.IO.FileLoadException) {
        } catch (System.BadImageFormatException) {
        }
        this.assemblyMap.Add(ident, result);
      }
      return result;
    }

    /// <summary>
    /// Returns a "live" System.Reflection.Module instance that provides reflective access to the referenced module.
    /// If the module cannot be found or cannot be loaded, the result is null.
    /// </summary>
    public Module/*?*/ GetModule(IModuleReference/*?*/ moduleReference) {
      if (moduleReference == null) return null;
      if (moduleReference.ContainingAssembly == null) return null;
      var assembly = this.GetAssembly(moduleReference.ContainingAssembly);
      if (assembly == null) return null;
      return assembly.GetModule(moduleReference.Name.Value);
    }

    /// <summary>
    /// Returns a "live" System.Reflection.FieldInfo object that provides reflective access to the referenced field.
    /// If the field cannot be found or cannot be loaded, the result is null.
    /// </summary>
    public FieldInfo/*?*/ GetField(IFieldReference/*?*/ fieldReference) {
      if (fieldReference == null) return null;
      var result = this.fieldMap.Find(fieldReference.ContainingType.InternedKey, (uint)fieldReference.Name.UniqueKey);
      if (result == null) {
        var containingType = this.GetType(fieldReference.ContainingType);
        if (containingType == null) return null;
        var fieldType = this.GetType(fieldReference.Type);
        if (fieldType == null) return null;
        foreach (var member in containingType.GetMember(fieldReference.Name.Value, BindingFlags.NonPublic|BindingFlags.DeclaredOnly)) {
          var field = (FieldInfo)member as FieldInfo;
          if (field == null) continue;
          if (field.FieldType != fieldType) continue;
          if (fieldReference.IsModified) {
            if (!this.ModifiersMatch(field.GetOptionalCustomModifiers(), field.GetRequiredCustomModifiers(), fieldReference.CustomModifiers)) continue;
          }
        }
        this.fieldMap.Add(fieldReference.ContainingType.InternedKey, (uint)fieldReference.Name.UniqueKey, result);
      }
      return result;
    }

    /// <summary>
    /// Returns a "live" System.Reflection.MethodBase object that provides reflective access to the referenced method.
    /// If the method cannot be found or cannot be loaded, the result is null.
    /// </summary>
    public MethodBase/*?*/ GetMethod(IMethodReference/*?*/ methodReference) {
      if (methodReference == null) return null;
      MethodBase result = this.methodMap.Find(methodReference.InternedKey);
      if (result == null) {
        MemberInfo[] members = this.membersMap.Find(methodReference.ContainingType.InternedKey, (uint)methodReference.Name.UniqueKey);
        if (members == null) {
          Type containingType = this.GetType(methodReference.ContainingType);
          if (containingType == null) return null;
          members = containingType.GetMember(methodReference.Name.Value, BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
          this.membersMap.Add(methodReference.ContainingType.InternedKey, (uint)methodReference.Name.UniqueKey, members);
        }
        var methodReturnType = this.GetType(methodReference.Type);
        if (methodReturnType == null) return null;
        if (methodReference.ReturnValueIsByRef) methodReturnType = methodReturnType.MakeByRefType();
        var parameterCount = methodReference.ParameterCount;
        var parameters = new IParameterTypeInformation[parameterCount];
        var parameterTypes = parameterCount == 0 ? null : new Type[parameterCount];
        int parameterIndex = 0;
        foreach (var parameter in methodReference.Parameters) {
          parameters[parameterIndex] = parameter;
          var ptype = this.GetType(parameter.Type);
          if (ptype == null) return null;
          if (parameter.IsByReference) ptype = ptype.MakeByRefType();
          parameterTypes[parameterIndex++] = ptype;
        }
        var referencedMethodIsStatic = (methodReference.CallingConvention & CallingConvention.HasThis) == 0;
        foreach (var member in members) {
          var methodBase = member as MethodBase;
          if (methodBase == null) continue;
          if (methodBase.IsStatic && !referencedMethodIsStatic) continue;
          switch (methodBase.CallingConvention&(CallingConventions)3) {
            case CallingConventions.Any:
              if ((methodReference.CallingConvention&(CallingConvention)7) != CallingConvention.Standard && 
                (methodReference.CallingConvention&(CallingConvention)7) != CallingConvention.ExtraArguments)
                continue;
              break;
            case CallingConventions.Standard:
              if ((methodReference.CallingConvention&(CallingConvention)7) != CallingConvention.Standard) continue;
              break;
            case CallingConventions.VarArgs:
              if ((methodReference.CallingConvention&(CallingConvention)7) != CallingConvention.ExtraArguments) continue;
              break;
          }
          if ((methodBase.CallingConvention & CallingConventions.HasThis) != 0 &&
            (methodReference.CallingConvention & CallingConvention.HasThis) == 0) continue;
          if ((methodBase.CallingConvention & CallingConventions.ExplicitThis) != 0 &&
            (methodReference.CallingConvention & CallingConvention.ExplicitThis) == 0) continue;
          if (methodBase.IsConstructor) {
            if (methodReference.Type.TypeCode != PrimitiveTypeCode.Void) continue;
          } else {
            var method = (MethodInfo)methodBase;
            if (methodReturnType != method.ReturnType) continue;
            if (methodReference.ReturnValueIsModified) {
              if (!this.ModifiersMatch(method.ReturnParameter.GetOptionalCustomModifiers(), method.ReturnParameter.GetRequiredCustomModifiers(), methodReference.ReturnValueCustomModifiers)) continue;
            }
            if (method.IsGenericMethod) {
              if (methodReference.GenericParameterCount != method.GetGenericArguments().Length) continue;
            }
          }
          var memberParameterInfos = methodBase.GetParameters();
          if (parameterCount != memberParameterInfos.Length) continue;
          bool matched = true;
          for (int i = 0; i < parameterCount; i++) {
            var mparInfo = memberParameterInfos[i];
            var ipar = parameters[i];
            var part = parameterTypes[i];
            if (mparInfo.ParameterType == part) { matched = false; break; }
            if (ipar.IsModified) {
              if (!this.ModifiersMatch(mparInfo.GetOptionalCustomModifiers(), mparInfo.GetRequiredCustomModifiers(), ipar.CustomModifiers)) continue;
            }
          }
          if (!matched) continue;
          result = methodBase;
          break;
        }
        //if resuslt is null, the reference might be to a dummy array access method
        //call GetArrayMethod on a ModuleBuilder. (this means that a module builder must be supplied to the constructor of this class).        
        this.methodMap.Add(methodReference.InternedKey, result);
      }
      return result;
    }

    private bool ModifiersMatch(Type[] optional, Type[] required, IEnumerable<ICustomModifier> modifiers) {
      int optionalCount = 0;
      int requiredCount = 0;
      int modifierCount = 0;
      foreach (var modifier in modifiers) {        
        var modifierType = this.GetType(modifier.Modifier);
        if (modifierType == null) return false;
        if (modifier.IsOptional) {
          if (optionalCount == optional.Length) return false;
          if (optional[optionalCount++] != modifierType) return false;
        } else {
          if (requiredCount == required.Length) return false;
          if (required[requiredCount++] != modifierType) return false;
        }
        modifierCount++;
      }
      return modifierCount == optional.Length + required.Length;
    }

    /// <summary>
    /// Returns a "live" System.Type object that provides reflective access to the referenced typeBuilder.
    /// If the typeBuilder cannot be found or cannot be loaded, the result is null.
    /// </summary>
    public Type/*?*/ GetType(ITypeReference/*?*/ type) {
      if (type == null) return null;
      var result = this.typeMap.Find(type.InternedKey);
      if (result == null) {
        type.DispatchAsReference(this.mappingVisitor);
        result = this.mappingVisitor.result;
        this.typeMap.Add(type.InternedKey, result);
      }
      return result;
    }


    internal void DefineMapping(ITypeReference typeReference, Type type) {
      this.typeMap.Add(typeReference.InternedKey, type);
    }

    internal void DefineMapping(IFieldDefinition fieldDefinition, FieldInfo fieldBuilder) {
      this.fieldMap.Add(fieldDefinition.ContainingType.InternedKey, (uint)fieldDefinition.Name.UniqueKey, fieldBuilder);
    }

    internal void DefineMapping(IMethodDefinition method, MethodBase methodBuilder) {
      this.methodMap.Add(method.InternedKey, methodBuilder);
    }

    internal MethodInfo GetArrayAddrMethod(IArrayTypeReference arrayTypeReference, ModuleBuilder moduleBuilder) {
      var type = this.GetType(arrayTypeReference);
      var parameterTypes = new Type[arrayTypeReference.Rank];
      for (int i = 0; i < arrayTypeReference.Rank; i++) parameterTypes[i] = typeof(int);
      return moduleBuilder.GetArrayMethod(type, "Address", CallingConventions.HasThis, type.GetElementType().MakeByRefType(), parameterTypes);
    }

    internal MethodInfo GetArrayGetMethod(IArrayTypeReference arrayTypeReference, ModuleBuilder moduleBuilder) {
      var type = this.GetType(arrayTypeReference);
      var parameterTypes = new Type[arrayTypeReference.Rank];
      for (int i = 0; i < arrayTypeReference.Rank; i++) parameterTypes[i] = typeof(int);
      return moduleBuilder.GetArrayMethod(type, "Get", CallingConventions.HasThis, type.GetElementType(), parameterTypes);
    }

    internal MethodInfo GetArraySetMethod(IArrayTypeReference arrayTypeReference, ModuleBuilder moduleBuilder) {
      var type = this.GetType(arrayTypeReference);
      var parameterTypes = new Type[arrayTypeReference.Rank+1];
      for (int i = 0; i < arrayTypeReference.Rank; i++) parameterTypes[i] = typeof(int);
      parameterTypes[arrayTypeReference.Rank] = type.GetElementType();
      return moduleBuilder.GetArrayMethod(type, "Set", CallingConventions.HasThis, typeof(void), parameterTypes);
    }

    internal MethodInfo GetArrayCreateMethod(IArrayTypeReference arrayTypeReference, ModuleBuilder moduleBuilder) {
      var type = this.GetType(arrayTypeReference);
      var parameterTypes = new Type[arrayTypeReference.Rank];
      for (int i = 0; i < arrayTypeReference.Rank; i++) parameterTypes[i] = typeof(int);
      return moduleBuilder.GetArrayMethod(type, ".ctor", CallingConventions.HasThis, typeof(void), parameterTypes);
    }

    internal MethodInfo GetArrayCreateWithLowerBoundsMethod(IArrayTypeReference arrayTypeReference, ModuleBuilder moduleBuilder) {
      var type = this.GetType(arrayTypeReference);
      var parameterTypes = new Type[arrayTypeReference.Rank*2];
      for (int i = 0; i < arrayTypeReference.Rank*2; i++) parameterTypes[i] = typeof(int);
      return moduleBuilder.GetArrayMethod(type, ".ctor", CallingConventions.HasThis, typeof(void), parameterTypes);
    }
  }

  /// <summary>
  /// A visitor that maps CCI typeBuilder references to System.Type instances using method on System.Type.
  /// It uses a provided ReflectionMapper object to map element types to System.Type instances so
  /// that the caches maintained by ReflectionMapper can be used.
  /// </summary>
  class MappingVisitorForTypes : MetadataVisitor {

    internal MappingVisitorForTypes(ReflectionMapper mapper) {
      this.mapper = mapper;
    }

    private readonly ReflectionMapper mapper;
    internal Type result;

    public override void Visit(IArrayTypeReference arrayTypeReference) {
      if (arrayTypeReference.IsVector)
        this.result = this.mapper.GetType(arrayTypeReference.ElementType).MakeArrayType();
      else
        this.result = this.mapper.GetType(arrayTypeReference.ElementType).MakeArrayType((int)arrayTypeReference.Rank);
    }

    public override void Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
      //System.Reflection has no way to construct an actual function pointer.
      //What it returns when reflecting on a function pointer typeBuilder is System.IntPtr.
      //We'll do the same. Since function pointers are unverifiable, the typeBuilder mismatch at the call site does
      //not actually matter.
      this.result = typeof(System.IntPtr);
    }

    public override void Visit(IGenericMethodParameterReference genericMethodParameterReference) {
      var genericMethod = this.mapper.GetMethod(genericMethodParameterReference.DefiningMethod);
      this.result = genericMethod.GetGenericArguments()[genericMethodParameterReference.Index];
    }

    public override void Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
      var template = genericTypeInstanceReference.GenericType;
      var specializedNestedType = template as ISpecializedNestedTypeReference;
      if (specializedNestedType != null) template = specializedNestedType.UnspecializedVersion;
      var templateType = this.mapper.GetType(template);
      var consolidatedArguments = new List<Type>();
      this.GetConsolidatedTypeArguments(consolidatedArguments, genericTypeInstanceReference);
      this.result = templateType.MakeGenericType(consolidatedArguments.ToArray());
    }

    private void GetConsolidatedTypeArguments(List<Type> consolidatedTypeArguments, ITypeReference typeReference) {
      var genTypeInstance = typeReference as IGenericTypeInstanceReference;
      if (genTypeInstance != null) {
        GetConsolidatedTypeArguments(consolidatedTypeArguments, genTypeInstance.GenericType);
        foreach (var genArg in genTypeInstance.GenericArguments)
          consolidatedTypeArguments.Add(this.mapper.GetType(genArg));
        return;
      }
      var nestedTypeReference = typeReference as INestedTypeReference;
      if (nestedTypeReference != null) GetConsolidatedTypeArguments(consolidatedTypeArguments, nestedTypeReference.ContainingType);
    }

    public override void Visit(IGenericTypeParameterReference genericTypeParameterReference) {
      var definingType = genericTypeParameterReference.DefiningType;
      int index = genericTypeParameterReference.Index;
      //This index does not account for any inherited generic parameters. See if any containing types contain typeBuilder parameters and adjust the index.
      while (true) {
        var nestedType = definingType as INestedTypeReference;
        if (nestedType == null) break;
        definingType = nestedType.ContainingType;
        var genericTypeInstance = definingType as IGenericTypeInstanceReference;
        if (genericTypeInstance != null) index += (int)IteratorHelper.EnumerableCount(genericTypeInstance.GenericArguments);
      }
      //The defining typeBuilder may actually be a containing typeBuilder of the typeBuilder that contains the reference we are mapping here.
      //In that case, the System.Type object obtained below will probably be a different object than the one that would
      //be obtained by looking at the (consolidated) typeBuilder parameter list of the System.Type object that contains the reference.
      //We assume that this does not matter, however, since a Reflection typeBuilder parameter does not keep (visible) track of the typeBuilder which
      //it parameterizes.
      var genericType = this.mapper.GetType(definingType);
      this.result = genericType.GetGenericArguments()[genericTypeParameterReference.Index];
    }

    public override void Visit(IManagedPointerTypeReference managedPointerTypeReference) {
      this.result = this.mapper.GetType(managedPointerTypeReference.TargetType).MakeByRefType();
    }

    public override void Visit(IModifiedTypeReference modifiedTypeReference) {
      //Sytem.Reflection cannot model modified typeBuilder references. Just strip the modifiers.
      modifiedTypeReference.UnmodifiedType.Dispatch(this);
    }

    public override void Visit(INamespaceTypeReference namespaceTypeReference) {
      var assemblyReference = namespaceTypeReference.ContainingUnitNamespace.Unit as IAssemblyReference;
      if (assemblyReference != null) {
        var assembly = this.mapper.GetAssembly(assemblyReference);
        this.result = assembly.GetType(TypeHelper.GetTypeName(namespaceTypeReference, NameFormattingOptions.UseGenericTypeNameSuffix));
        if (this.result == null && namespaceTypeReference.GenericParameterCount > 0)
          this.result = assembly.GetType(TypeHelper.GetTypeName(namespaceTypeReference));
      } else {
        var mod = (IModuleReference)namespaceTypeReference.ContainingUnitNamespace.Unit;
        var module = this.mapper.GetModule(mod);
        this.result = module.GetType(TypeHelper.GetTypeName(namespaceTypeReference, NameFormattingOptions.UseGenericTypeNameSuffix));
        if (this.result == null && namespaceTypeReference.GenericParameterCount > 0)
          this.result = module.GetType(TypeHelper.GetTypeName(namespaceTypeReference));
      }
    }

    public override void Visit(INestedTypeReference nestedTypeReference) {
      var containingType = this.mapper.GetType(nestedTypeReference.ContainingType);
      var name = nestedTypeReference.Name.Value;
      this.result = containingType.GetNestedType(name, BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
      if (this.result == null && nestedTypeReference.GenericParameterCount > 0) {
        name = name + "'" + nestedTypeReference.GenericParameterCount;
        this.result = containingType.GetNestedType(name, BindingFlags.NonPublic|BindingFlags.DeclaredOnly);
      }
    }

    public override void Visit(IPointerTypeReference pointerTypeReference) {
      this.result = this.mapper.GetType(pointerTypeReference.TargetType).MakePointerType();
    }

  }

}
