//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Collections;

namespace Microsoft.Cci.ReflectionImporter {
  /// <summary>
  /// An object that provides methods to map CCI metadata references to corresponding System.Type and System.Reflection.* objects.
  /// The object maintains a cache of mappings and should typically be used for doing many mappings.
  /// </summary>
  public class ReflectionMapper {

    /// <summary>
    /// An object that provides methods to map CCI metadata references to corresponding System.Type and System.Reflection.* objects.
    /// The object maintains a cache of mappings and should typically be used for doing many mappings.
    /// </summary>
    public ReflectionMapper(IMetadataHost host) {
      this.host = host;
    }

    internal IMetadataHost host;

    Dictionary<object, object> cache = new Dictionary<object, object>();

    internal IAliasForType GetAliasForType(Type type) {
      if (type.IsNested) {
        var containingAlias = this.GetAliasForType(type.DeclaringType);
        return new NestedAliasForType(this, containingAlias, type);
      }
      return new NamespaceAliasForType(this, type);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="assembly"></param>
    /// <returns></returns>
    public IAssembly GetAssembly(Assembly assembly) {
      object wrapper = null;
      if (!this.cache.TryGetValue(assembly, out wrapper)) {
        wrapper = new AssemblyWrapper(this, assembly);
        this.cache.Add(assembly, wrapper);
      }
      return (IAssembly)wrapper;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customAttributeData"></param>
    /// <returns></returns>
    public ICustomAttribute GetCustomAttribute(CustomAttributeData customAttributeData) {
      return new CustomAttributeWrapper(this, customAttributeData);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="customAttributeData"></param>
    /// <returns></returns>
    public IEnumerable<ICustomAttribute> GetCustomAttributes(IList<CustomAttributeData> customAttributeData) {
      int n = customAttributeData.Count;
      ICustomAttribute[] customAttributes = new ICustomAttribute[n];
      for (int i = 0; i < n; i++)
        customAttributes[i] = this.GetCustomAttribute(customAttributeData[i]);
      return IteratorHelper.GetReadonly(customAttributes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="eventInfo"></param>
    /// <returns></returns>
    public IEventDefinition GetEvent(EventInfo eventInfo) {
      return new EventWrapper(this, eventInfo);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public IMetadataExpression GetExpression(CustomAttributeTypedArgument argument) {
      var type = this.GetType(argument.ArgumentType);
      var arrayType = type as IArrayTypeReference;
      if (arrayType != null) return new MetadataCreateArrayWrapper(this, arrayType, (ICollection<CustomAttributeTypedArgument>)argument.Value);
      if (argument.Value is Type) return new MetadataTypeOfWrapper(this, type, (Type)argument.Value);
      return new MetadataConstantWrapper(type, argument.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public IMetadataNamedArgument GetExpression(CustomAttributeNamedArgument argument) {
      return new MetadataNamedArgumentWrapper(this, argument);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <returns></returns>
    public IFieldDefinition GetField(FieldInfo fieldInfo) {
      return new FieldWrapper(this, fieldInfo);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="containingAssembly"></param>
    /// <param name="hasMetadata"></param>
    /// <returns></returns>
    public IFileReference GetFileReference(string fileName, IAssembly containingAssembly, bool hasMetadata) {
      return new FileReferenceWrapper(this.host.NameTable.GetNameFor(fileName), containingAssembly, hasMetadata);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="constructorInfo"></param>
    /// <returns></returns>
    public IMethodReference GetMethod(ConstructorInfo constructorInfo) {
      throw new NotImplementedException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    public IMethodDefinition GetMethod(MethodInfo methodInfo) {
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="module"></param>
    /// <returns></returns>
    public IModule GetModule(Module module) {
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parameterInfo"></param>
    /// <returns></returns>
    public IParameterDefinition GetParameter(ParameterInfo parameterInfo) {
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    public IPropertyDefinition GetProperty(PropertyInfo propertyInfo) {
      return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public ITypeDefinition GetType(Type type) {
      return null;
    }


    internal static TypeMemberVisibility TypeMemberVisibilityFor(Type type) {
      throw new NotImplementedException();
    }

    internal static TypeMemberVisibility TypeMemberVisibilityFor(MemberInfo member) {
      throw new NotImplementedException();
    }

    internal INamespaceDefinition GetNamespace(Type type) {
      throw new NotImplementedException();
    }

    internal IResource GetResource(string resName, ManifestResourceInfo resInfo, System.IO.Stream bytes) {
      var data = new byte[bytes.Length];
      bytes.Read(data, 0, data.Length);
      var isInExternalFile = (resInfo.ResourceLocation & ResourceLocation.Embedded) != 0;
      var externalFile = Dummy.FileReference;
      var definingAssembly = this.GetAssembly(resInfo.ReferencedAssembly);
      if (isInExternalFile) externalFile = this.GetFileReference(resInfo.FileName, definingAssembly, false);
      var name = this.host.NameTable.GetNameFor(resName);
      return new ResourceWrapper(IteratorHelper.GetReadonly(data), isInExternalFile, externalFile, definingAssembly, name);
    }

    internal IEnumerable<ICustomModifier> GetCustomModifiers(Type[] optional, Type[] required) {
      throw new NotImplementedException();
    }

    internal IMetadataConstant GetMetadataConstant(object rawConst) {
      throw new NotImplementedException();
    }

    internal IMethodBody GetBody(MethodBody methodBody) {
      throw new NotImplementedException();
    }

    internal IEnumerable<IParameterDefinition> GetParameters(ParameterInfo[] parameterInfo) {
      throw new NotImplementedException();
    }

    internal IEnumerable<IGenericMethodParameter> GetMethodGenericParameters(Type[] type) {
      throw new NotImplementedException();
    }

    internal IEnumerable<ITypeReference> GetTypes(Type[] type) {
      throw new NotImplementedException();
    }
  }

}