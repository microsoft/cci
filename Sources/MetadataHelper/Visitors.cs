//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci;

//^ using Microsoft.Contracts;
#pragma warning disable 1591

namespace Microsoft.Cci {

  /// <summary>
  /// A visitor base class that traverses the object model in depth first, left to right order.
  /// </summary>
  public class BaseMetadataTraverser : IMetadataVisitor {

    public BaseMetadataTraverser() {
    }

    //^ [SpecPublic]
    protected System.Collections.Stack path = new System.Collections.Stack();

    protected bool stopTraversal;

    #region IMetadataVisitor Members

    public virtual void Visit(IEnumerable<IAliasForType> aliasesForTypes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IAliasForType aliasForType in aliasesForTypes) {
        this.Visit(aliasForType);
        if (this.stopTraversal) return;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    }

    public virtual void Visit(IAliasForType aliasForType)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(aliasForType);
      this.Visit(aliasForType.AliasedType);
      this.Visit(aliasForType.Attributes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      aliasForType.Dispatch(this);
    }

    public virtual void Visit(IArrayTypeReference arrayTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(arrayTypeReference);
      this.Visit(arrayTypeReference.ElementType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IAssembly assembly)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      this.Visit((IModule)assembly);
      this.Visit(assembly.AssemblyAttributes);
      this.Visit(assembly.ExportedTypes);
      this.Visit(assembly.Files);
      this.Visit(assembly.MemberModules);
      this.Visit(assembly.Resources);
      this.Visit(assembly.SecurityAttributes);
    }

    public virtual void Visit(IEnumerable<IAssemblyReference> assemblyReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IAssemblyReference assemblyReference in assemblyReferences) {
        this.Visit((IUnitReference)assemblyReference);
        if (this.stopTraversal) return;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
    }

    public virtual void Visit(IAssemblyReference assemblyReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IEnumerable<ICustomAttribute> customAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ICustomAttribute customAttribute in customAttributes) {
        this.Visit(customAttribute);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(ICustomAttribute customAttribute)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(customAttribute);
      this.Visit(customAttribute.Arguments);
      this.Visit(customAttribute.Constructor);
      this.Visit(customAttribute.NamedArguments);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<ICustomModifier> customModifiers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ICustomModifier customModifier in customModifiers) {
        this.Visit(customModifier);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(ICustomModifier customModifier)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(customModifier);
      this.Visit(customModifier.Modifier);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IEventDefinition> events)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IEventDefinition eventDef in events) {
        this.Visit((ITypeDefinitionMember)eventDef);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IEventDefinition eventDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(eventDefinition);
      this.Visit(eventDefinition.Accessors);
      this.Visit(eventDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IFieldDefinition> fields)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IFieldDefinition field in fields) {
        this.Visit((ITypeDefinitionMember)field);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IFieldDefinition fieldDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(fieldDefinition);
      if (fieldDefinition.IsCompileTimeConstant)
        this.Visit((IMetadataExpression)fieldDefinition.CompileTimeValue);
      if (fieldDefinition.IsMarshalledExplicitly)
        this.Visit(fieldDefinition.MarshallingInformation);
      this.Visit(fieldDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IFieldReference fieldReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      this.Visit((ITypeMemberReference)fieldReference);
    }

    public virtual void Visit(IEnumerable<IFileReference> fileReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IFileReference fileReference in fileReferences) {
        this.Visit(fileReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IFileReference fileReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IFunctionPointerTypeReference functionPointerTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(functionPointerTypeReference);
      this.Visit(functionPointerTypeReference.Type);
      this.Visit(functionPointerTypeReference.Parameters);
      this.Visit(functionPointerTypeReference.ExtraArgumentTypes);
      if (functionPointerTypeReference.ReturnValueIsModified)
        this.Visit(functionPointerTypeReference.ReturnValueCustomModifiers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IGenericMethodInstanceReference genericMethodInstanceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IEnumerable<IGenericMethodParameter> genericParameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IGenericMethodParameter genericParameter in genericParameters) {
        this.Visit((IGenericParameter)genericParameter);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IGenericMethodParameter genericMethodParameter)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IGenericMethodParameterReference genericMethodParameterReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IGenericParameter genericParameter) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericParameter);
      this.Visit(genericParameter.Attributes);
      this.Visit(genericParameter.Constraints);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      genericParameter.Dispatch(this);
    }

    public virtual void Visit(IGenericTypeInstanceReference genericTypeInstanceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(genericTypeInstanceReference);
      this.Visit(genericTypeInstanceReference.GenericType);
      this.Visit(genericTypeInstanceReference.GenericArguments);
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IGenericTypeParameter> genericParameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IGenericTypeParameter genericParameter in genericParameters) {
        this.Visit((IGenericParameter)genericParameter);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IGenericTypeParameter genericTypeParameter)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IGenericTypeParameterReference genericTypeParameterReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IGlobalFieldDefinition globalFieldDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      this.Visit((IFieldDefinition)globalFieldDefinition);
    }

    public virtual void Visit(IGlobalMethodDefinition globalMethodDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      this.Visit((IMethodDefinition)globalMethodDefinition);
    }

    public virtual void Visit(IEnumerable<ILocalDefinition> localDefinitions) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ILocalDefinition localDefinition in localDefinitions) {
        this.Visit(localDefinition);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(ILocalDefinition localDefinition) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(localDefinition);
      this.Visit(localDefinition.CustomModifiers);
      this.Visit(localDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IManagedPointerTypeReference managedPointerTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IMarshallingInformation marshallingInformation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(marshallingInformation);
      if (marshallingInformation.UnmanagedType == System.Runtime.InteropServices.UnmanagedType.CustomMarshaler)
        this.Visit(marshallingInformation.CustomMarshaller);
      if (marshallingInformation.UnmanagedType == System.Runtime.InteropServices.UnmanagedType.SafeArray && 
      (marshallingInformation.SafeArrayElementSubtype == System.Runtime.InteropServices.VarEnum.VT_DISPATCH ||
      marshallingInformation.SafeArrayElementSubtype == System.Runtime.InteropServices.VarEnum.VT_UNKNOWN ||
      marshallingInformation.SafeArrayElementSubtype == System.Runtime.InteropServices.VarEnum.VT_RECORD))
        this.Visit(marshallingInformation.SafeArrayElementUserDefinedSubtype);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IMetadataConstant constant)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IMetadataCreateArray createArray)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(createArray);
      this.Visit(createArray.ElementType);
      this.Visit(createArray.Initializers);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.      this.path.Pop();
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IMetadataExpression> expressions)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMetadataExpression expression in expressions) {
        this.Visit(expression);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IMetadataExpression expression) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(expression);
      this.Visit(expression.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
      expression.Dispatch(this);
    }

    public virtual void Visit(IEnumerable<IMetadataNamedArgument> namedArguments)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMetadataNamedArgument namedArgument in namedArguments) {
        this.Visit((IMetadataExpression)namedArgument);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IMetadataNamedArgument namedArgument)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namedArgument);
      this.Visit(namedArgument.ArgumentValue);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IMetadataTypeOf typeOf)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeOf);
      this.Visit(typeOf.TypeToGet);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not to decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IMethodBody methodBody)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodBody);
      this.Visit(methodBody.LocalVariables);
      this.Visit(methodBody.Operations);
      this.Visit(methodBody.OperationExceptionInformation);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IMethodDefinition> methods)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMethodDefinition method in methods) {
        this.Visit((ITypeDefinitionMember)method);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IMethodDefinition method)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(method);
      this.VisitMethodReturnAttributes(method.ReturnValueAttributes);
      if (method.ReturnValueIsModified)
        this.Visit(method.ReturnValueCustomModifiers);
      if (method.HasDeclarativeSecurity)
        this.Visit(method.SecurityAttributes);
      if (method.IsGeneric) this.Visit(method.GenericParameters);
      this.Visit(method.Type);
      this.Visit(method.Parameters);
      if (method.IsPlatformInvoke)
        this.Visit(method.PlatformInvokeData);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IMethodImplementation> methodImplementations)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMethodImplementation methodImplementation in methodImplementations) {
        this.Visit(methodImplementation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IMethodImplementation methodImplementation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(methodImplementation);
      this.Visit(methodImplementation.ImplementedMethod);
      this.Visit(methodImplementation.ImplementingMethod);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IMethodReference> methodReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IMethodReference methodReference in methodReferences) {
        this.Visit(methodReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IMethodReference methodReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      IGenericMethodInstanceReference/*?*/ genericMethodInstanceReference = methodReference as IGenericMethodInstanceReference;
      if (genericMethodInstanceReference != null) 
        this.Visit(genericMethodInstanceReference);
      else
        this.Visit((ITypeMemberReference)methodReference);
    }

    public virtual void Visit(IModifiedTypeReference modifiedTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(modifiedTypeReference);
      this.Visit(modifiedTypeReference.CustomModifiers);
      this.Visit(modifiedTypeReference.UnmodifiedType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IModule module)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(module);
      this.Visit(module.ModuleAttributes);
      this.Visit(module.AssemblyReferences);
      this.Visit(module.NamespaceRoot);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IModule> modules)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IModule module in modules) {
        this.Visit((IUnit)module);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IEnumerable<IModuleReference> moduleReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IModuleReference moduleReference in moduleReferences) {
        this.Visit((IUnitReference)moduleReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IModuleReference moduleReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IEnumerable<INamedTypeDefinition> types)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (INamedTypeDefinition type in types) {
        this.Visit(type);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IEnumerable<INamespaceMember> namespaceMembers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (INamespaceMember namespaceMember in namespaceMembers) {
        this.Visit(namespaceMember);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(INamespaceAliasForType namespaceAliasForType) {
    }

    public virtual void Visit(INamespaceMember namespaceMember) {
      if (this.stopTraversal) return;
      INamespaceDefinition/*?*/ nestedNamespace = namespaceMember as INamespaceDefinition;
      if (nestedNamespace != null)
        this.Visit(nestedNamespace);
      else {
        ITypeDefinition/*?*/ namespaceType = namespaceMember as ITypeDefinition;
        if (namespaceType != null)
          this.Visit(namespaceType);
        else {
          ITypeDefinitionMember/*?*/ globalFieldOrMethod = namespaceMember as ITypeDefinitionMember;
          if (globalFieldOrMethod != null)
            this.Visit(globalFieldOrMethod);
          else {
            INamespaceAliasForType/*?*/ namespaceAlias = namespaceMember as INamespaceAliasForType;
            if (namespaceAlias != null)
              this.Visit(namespaceAlias);
            else {
              //TODO: error
              namespaceMember.Dispatch(this);
            }
          }
        }
      }
    }

    public virtual void Visit(INamespaceTypeDefinition namespaceTypeDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(INamespaceTypeReference namespaceTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(INestedAliasForType nestedAliasForType) {
    }

    public virtual void Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IEnumerable<INestedTypeDefinition> nestedTypes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (INestedTypeDefinition nestedType in nestedTypes) {
        this.Visit((ITypeDefinitionMember)nestedType);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(INestedTypeDefinition nestedTypeDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(INestedTypeReference nestedTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(nestedTypeReference);
      this.Visit(nestedTypeReference.ContainingType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(INestedUnitNamespace nestedUnitNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(INestedUnitSetNamespace nestedUnitSetNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IEnumerable<IOperation> operations) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IOperation operation in operations) {
        this.Visit(operation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IOperation operation) {
      ITypeReference/*?*/ typeReference = operation.Value as ITypeReference;
      if (typeReference != null) {
        if (operation.OperationCode == OperationCode.Newarr) {
          //^ assume operation.Value is IArrayTypeReference;
          this.Visit(((IArrayTypeReference)operation.Value).ElementType);
        } else
          this.Visit(typeReference);
      } else {
        IFieldReference/*?*/ fieldReference = operation.Value as IFieldReference;
        if (fieldReference != null)
          this.Visit(fieldReference);
        else {
          IMethodReference/*?*/ methodReference = operation.Value as IMethodReference;
          if (methodReference != null)
            this.Visit(methodReference);
        }
      }
    }

    public virtual void Visit(IEnumerable<IOperationExceptionInformation> operationExceptionInformations) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IOperationExceptionInformation operationExceptionInformation in operationExceptionInformations) {
        this.Visit(operationExceptionInformation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IOperationExceptionInformation operationExceptionInformation) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(operationExceptionInformation);
      this.Visit(operationExceptionInformation.ExceptionType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IParameterDefinition> parameters)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IParameterDefinition parameter in parameters) {
        this.Visit(parameter);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IParameterDefinition parameterDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(parameterDefinition);
      this.Visit(parameterDefinition.Attributes);
      if (parameterDefinition.IsModified)
        this.Visit(parameterDefinition.CustomModifiers);
      if (parameterDefinition.HasDefaultValue)
        this.Visit((IMetadataExpression)parameterDefinition.DefaultValue);
      if (parameterDefinition.IsMarshalledExplicitly)
        this.Visit(parameterDefinition.MarshallingInformation);
      this.Visit(parameterDefinition.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IParameterTypeInformation> parameterTypeInformations)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IParameterTypeInformation parameterTypeInformation in parameterTypeInformations) {
        this.Visit(parameterTypeInformation);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IParameterTypeInformation parameterTypeInformation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(parameterTypeInformation);
      if (parameterTypeInformation.IsModified)
        this.Visit(parameterTypeInformation.CustomModifiers);
      this.Visit(parameterTypeInformation.Type);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IPlatformInvokeInformation platformInvokeInformation)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(platformInvokeInformation);
      this.Visit(platformInvokeInformation.ImportModule);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IPointerTypeReference pointerTypeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(pointerTypeReference);
      this.Visit(pointerTypeReference.TargetType);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IPropertyDefinition> properties)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IPropertyDefinition property in properties) {
        this.Visit((ITypeDefinitionMember)property);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IPropertyDefinition propertyDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(propertyDefinition);
      this.Visit(propertyDefinition.Accessors);
      this.Visit(propertyDefinition.Parameters);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<IResourceReference> resourceReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IResourceReference resourceReference in resourceReferences) {
        this.Visit(resourceReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IResourceReference resourceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IRootUnitNamespace rootUnitNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IRootUnitSetNamespace rootUnitSetNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(ISecurityAttribute securityAttribute)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(securityAttribute);
      this.Visit(securityAttribute.Attributes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<ISecurityAttribute> securityAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ISecurityAttribute securityAttribute in securityAttributes) {
        this.Visit(securityAttribute);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IEnumerable<ITypeDefinitionMember> typeMembers)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ITypeDefinitionMember typeMember in typeMembers) {
        this.Visit(typeMember);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IEnumerable<ITypeDefinition> types)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ITypeDefinition type in types) {
        this.Visit(type);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(ITypeDefinition typeDefinition) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeDefinition);
      this.Visit(typeDefinition.Attributes);
      this.Visit(typeDefinition.BaseClasses);
      this.Visit(typeDefinition.ExplicitImplementationOverrides);
      if (typeDefinition.HasDeclarativeSecurity)
        this.Visit(typeDefinition.SecurityAttributes);
      this.Visit(typeDefinition.Interfaces);
      if (typeDefinition.IsGeneric)
        this.Visit(typeDefinition.GenericParameters);
      this.Visit(typeDefinition.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      typeDefinition.Dispatch(this);
    }

    public virtual void Visit(ITypeDefinitionMember typeMember) {
      if (this.stopTraversal) return;
      ITypeDefinition/*?*/ nestedType = typeMember as ITypeDefinition;
      if (nestedType != null)
        this.Visit(nestedType);
      else {
        //^ int oldCount = this.path.Count;
        this.path.Push(typeMember);
        this.Visit(typeMember.Attributes);
        //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
        this.path.Pop();
        typeMember.Dispatch(this);
      }
    }

    public virtual void Visit(ITypeMemberReference typeMemberReference) {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(typeMemberReference);
      this.Visit(typeMemberReference.Attributes);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IEnumerable<ITypeReference> typeReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ITypeReference typeReference in typeReferences) {
        this.Visit(typeReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(ITypeReference typeReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      this.DispatchAsReference(typeReference);
    }

    /// <summary>
    /// Use this routine, rather than ITypeReference.Dispatch, to call the appropriate derived overload of an ITypeReference.
    /// The former routine will call Visit(INamespaceTypeDefinition) rather than Visit(INamespaceTypeReference), etc., 
    /// in the case where a definition is used as a reference to itself.
    /// </summary>
    /// <param name="typeReference">A reference to a type definition. Note that a type definition can serve as a reference to itself.</param>
    protected void DispatchAsReference(ITypeReference typeReference) {
      INamespaceTypeReference/*?*/ namespaceTypeReference = typeReference as INamespaceTypeReference;
      if (namespaceTypeReference != null) {
        this.Visit(namespaceTypeReference);
        return;
      }
      INestedTypeReference/*?*/ nestedTypeReference = typeReference as INestedTypeReference;
      if (nestedTypeReference != null) {
        this.Visit(nestedTypeReference);
        return;
      }
      IArrayTypeReference/*?*/ arrayTypeReference = typeReference as IArrayTypeReference;
      if (arrayTypeReference != null) {
        this.Visit(arrayTypeReference);
        return;
      }
      IGenericTypeInstanceReference/*?*/ genericTypeInstanceReference = typeReference as IGenericTypeInstanceReference;
      if (genericTypeInstanceReference != null) {
        this.Visit(genericTypeInstanceReference);
        return;
      }
      IGenericTypeParameterReference/*?*/ genericTypeParameterReference = typeReference as IGenericTypeParameterReference;
      if (genericTypeParameterReference != null) {
        this.Visit(genericTypeParameterReference);
        return;
      }
      IGenericMethodParameterReference/*?*/ genericMethodParameterReference = typeReference as IGenericMethodParameterReference;
      if (genericMethodParameterReference != null) {
        this.Visit(genericMethodParameterReference);
        return;
      }
      IPointerTypeReference/*?*/ pointerTypeReference = typeReference as IPointerTypeReference;
      if (pointerTypeReference != null) {
        this.Visit(pointerTypeReference);
        return;
      }
      IFunctionPointerTypeReference/*?*/ functionPointerTypeReference = typeReference as IFunctionPointerTypeReference;
      if (functionPointerTypeReference != null) {
        this.Visit(functionPointerTypeReference);
        return;
      }
      IModifiedTypeReference/*?*/ modifiedTypeReference = typeReference as IModifiedTypeReference;
      if (modifiedTypeReference != null) {
        this.Visit(modifiedTypeReference);
        return;
      }
    }

    public virtual void Visit(IUnit unit)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unit);
      this.Visit(unit.NamespaceRoot);
      this.Visit(unit.UnitReferences);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      unit.Dispatch(this);
    }

    public virtual void Visit(IEnumerable<IUnitReference> unitReferences)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (IUnitReference unitReference in unitReferences) {
        this.Visit(unitReference);
        if (this.stopTraversal) break;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    public virtual void Visit(IUnitReference unitReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      this.DispatchAsReference(unitReference);
    }

    /// <summary>
    /// Use this routine, rather than IUnitReference.Dispatch, to call the appropriate derived overload of an IUnitReference.
    /// The former routine will call Visit(IAssembly) rather than Visit(IAssemblyReference), etc.
    /// in the case where a definition is used as the reference to itself.
    /// </summary>
    /// <param name="unitReference">A reference to a unit. Note that a unit can serve as a reference to itself.</param>
    private void DispatchAsReference(IUnitReference unitReference) {
      IAssemblyReference/*?*/ assemblyReference = unitReference as IAssemblyReference;
      if (assemblyReference != null) {
        this.Visit(assemblyReference);
        return;
      }
      IModuleReference/*?*/ moduleReference = unitReference as IModuleReference;
      if (moduleReference != null) {
        this.Visit(moduleReference);
        return;
      }
    }

    public virtual void Visit(INamespaceDefinition namespaceDefinition)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(namespaceDefinition);
      this.Visit(namespaceDefinition.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      namespaceDefinition.Dispatch(this);
    }

    public virtual void Visit(IRootUnitNamespaceReference rootUnitNamespaceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void Visit(IUnitNamespaceReference unitNamespaceReference)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      unitNamespaceReference.Dispatch(this);
    }

    public virtual void Visit(IUnitSet unitSet)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unitSet);
      this.Visit(unitSet.UnitSetNamespaceRoot);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
    }

    public virtual void Visit(IUnitSetNamespace unitSetNamespace)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      this.path.Push(unitSetNamespace);
      this.Visit(unitSetNamespace.Members);
      //^ assume this.path.Count == oldCount+1; //True because all of the virtual methods of this class promise not decrease this.path.Count.
      this.path.Pop();
      unitSetNamespace.Dispatch(this);
    }

    public virtual void Visit(IWin32Resource win32Resource)
      //^ ensures this.path.Count == old(this.path.Count);
    {
    }

    public virtual void VisitMethodReturnAttributes(IEnumerable<ICustomAttribute> customAttributes)
      //^ ensures this.path.Count == old(this.path.Count);
    {
      if (this.stopTraversal) return;
      //^ int oldCount = this.path.Count;
      foreach (ICustomAttribute customAttribute in customAttributes) {
        this.Visit(customAttribute);
        if (this.stopTraversal) return;
      }
      //^ assume this.path.Count == oldCount; //True because all of the virtual methods of this class promise not decrease this.path.Count.
    }

    #endregion
  }

  /// <summary>
  /// A visitor base class that provides a dummy body for each method of IVisit.
  /// </summary>
  public class BaseMetadataVisitor : IMetadataVisitor {

    public BaseMetadataVisitor() {
    }

    #region IMetadataVisitor Members

    public virtual void Visit(IAliasForType aliasForType) {
      aliasForType.Dispatch(this);
    }

    public virtual void Visit(IArrayTypeReference arrayTypeReference) {
    }

    public virtual void Visit(IAssembly assembly) {
    }

    public virtual void Visit(IAssemblyReference assemblyReference) {
    }

    public virtual void Visit(ICustomAttribute customAttribute) {
    }

    public virtual void Visit(ICustomModifier customModifier) {
    }

    public virtual void Visit(IEventDefinition eventDefinition) {
    }

    public virtual void Visit(IFieldDefinition fieldDefinition) {
    }

    public virtual void Visit(IFieldReference fieldReference) {
    }

    public virtual void Visit(IFileReference fileReference) {
    }

    public virtual void Visit(IFunctionPointerTypeReference functionPointerTypeReference) {
    }

    public virtual void Visit(IGenericMethodInstanceReference genericMethodInstanceReference) {
    }

    public virtual void Visit(IGenericMethodParameter genericMethodParameter) {
    }

    public virtual void Visit(IGenericMethodParameterReference genericMethodParameterReference) {
    }

    public virtual void Visit(IGenericTypeInstanceReference genericTypeInstanceReference) {
    }

    public virtual void Visit(IGenericTypeParameter genericTypeParameter) {
    }

    public virtual void Visit(IGenericTypeParameterReference genericTypeParameterReference) {
    }

    public virtual void Visit(IGlobalFieldDefinition globalFieldDefinition) {
    }

    public virtual void Visit(IGlobalMethodDefinition globalMethodDefinition) {
    }
    
    public virtual void Visit(IManagedPointerTypeReference managedPointerTypeReference) {
    }

    public virtual void Visit(IMarshallingInformation marshallingInformation) {
    }

    public virtual void Visit(IMetadataConstant constant) {
    }

    public virtual void Visit(IMetadataCreateArray createArray) {
    }

    public virtual void Visit(IMetadataExpression expression) {
      expression.Dispatch(this);
    }

    public virtual void Visit(IMetadataNamedArgument namedArgument) {
    }

    public virtual void Visit(IMetadataTypeOf typeOf) {
    }

    public virtual void Visit(IMethodBody methodBody) {
    }

    public virtual void Visit(IMethodDefinition method) {
    }

    public virtual void Visit(IMethodImplementation methodImplementation) {
    }

    public virtual void Visit(IMethodReference methodReference) {
    }

    public virtual void Visit(IModifiedTypeReference modifiedTypeReference) {
    }

    public virtual void Visit(IModule module) {
    }

    public virtual void Visit(IModuleReference moduleReference) {
    }

    public virtual void Visit(INamespaceAliasForType namespaceAliasForType) {
    }

    public virtual void Visit(INamespaceDefinition namespaceDefinition) {
      namespaceDefinition.Dispatch(this);
    }

    public virtual void Visit(INamespaceMember namespaceMember) {
      namespaceMember.Dispatch(this);
    }

    public virtual void Visit(INamespaceTypeDefinition namespaceTypeDefinition) {
    }

    public virtual void Visit(INamespaceTypeReference namespaceTypeReference) {
    }

    public virtual void Visit(INestedAliasForType nestedAliasForType) {
    }

    public virtual void Visit(INestedTypeDefinition nestedTypeDefinition) {
    }

    public virtual void Visit(INestedTypeReference nestedTypeReference) {
    }

    public virtual void Visit(INestedUnitNamespace nestedUnitNamespace) {
    }

    public virtual void Visit(INestedUnitNamespaceReference nestedUnitNamespaceReference) {
    }

    public virtual void Visit(INestedUnitSetNamespace nestedUnitSetNamespace) {
    }

    public virtual void Visit(IParameterDefinition parameterDefinition) {
    }

    public virtual void Visit(IPropertyDefinition propertyDefinition) {
    }

    public virtual void Visit(IParameterTypeInformation parameterTypeInformation){
    }

    public virtual void Visit(IPointerTypeReference pointerTypeReference) {
    }

    public virtual void Visit(IResourceReference resourceReference) {
    }

    public virtual void Visit(IRootUnitNamespace rootUnitNamespace) {
    }

    public virtual void Visit(IRootUnitNamespaceReference rootUnitNamespaceReference) {
    }

    public virtual void Visit(IRootUnitSetNamespace rootUnitSetNamespace) {
    }

    public virtual void Visit(ISecurityAttribute securityAttribute) {
    }

    public virtual void Visit(ITypeDefinitionMember typeMember) {
      typeMember.Dispatch(this);
    }

    public virtual void Visit(ITypeReference typeReference) {
      typeReference.Dispatch(this);
    }

    public virtual void Visit(IUnit unit) {
      unit.Dispatch(this);
    }

    public virtual void Visit(IUnitReference unitReference) {
      unitReference.Dispatch(this);
    }

    public virtual void Visit(IUnitNamespaceReference unitNamespaceReference) {
      unitNamespaceReference.Dispatch(this);
    }

    public virtual void Visit(IUnitSet unitSet) {
    }

    public virtual void Visit(IUnitSetNamespace unitSetNamespace) {
      unitSetNamespace.Dispatch(this);
    }

    public virtual void Visit(IWin32Resource win32Resource) {
    }

    #endregion
  }

#pragma warning restore 1591
}

