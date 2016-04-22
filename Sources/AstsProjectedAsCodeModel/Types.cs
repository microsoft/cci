//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// This code is licensed under the Microsoft Public License.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.UtilityDataStructures;
using System.Diagnostics;
using Microsoft.Cci.Immutable;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  //  Issue: Why inherit from TypeDefinition instead of SystemDefinedStructuralType?
  /// <summary>
  /// 
  /// </summary>
  public abstract class GenericParameter : NamedTypeDefinition, IGenericParameter {

    //^ [NotDelayed]
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <param name="variance"></param>
    /// <param name="mustBeReferenceType"></param>
    /// <param name="mustBeValueType"></param>
    /// <param name="mustHaveDefaultConstructor"></param>
    /// <param name="internFactory"></param>
    protected GenericParameter(IName name, ushort index, TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor, IInternFactory internFactory)
      : base(name, internFactory) {
      int flags = (int)variance << 4;
      if (mustBeReferenceType) flags |= 0x00200000;
      if (mustBeValueType) flags |= 0x00010000;
      if (mustHaveDefaultConstructor) flags |= 0x00008000;
      flags |= 0x00004000; //IsReferenceType and IsValueType still have to be computed
      this.index = index;
      //^ base;
      this.flags |= (NamedTypeDefinition.Flags)flags;
    }

    /// <summary>
    /// Gets the base classes.
    /// </summary>
    /// <value>The base classes.</value>
    public override IEnumerable<ITypeReference> BaseClasses {
      get { return Enumerable<ITypeReference>.Empty; }
    }

    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeReference> Constraints {
      get {
        bool hadConstraints = false;
        foreach (GenericParameterDeclaration parameterDeclaration in this.GetDeclarations()) {
          foreach (TypeExpression constraint in parameterDeclaration.Constraints) {
            yield return constraint.ResolvedType;
            hadConstraints = true;
          }
          if (hadConstraints) yield break;
        }
      }
    }

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    /// <summary>
    /// True if the type is a reference type. A reference type is non static class or interface or a suitably constrained type parameter.
    /// A type parameter for which MustBeReferenceType (the class constraint in C#) is true returns true for this property
    /// as does a type parameter with a constraint that is a class.
    /// </summary>
    /// <value></value>
    public override bool IsReferenceType {
      get {
        if (((int)this.flags & 0x00004000) != 0) {
          this.flags &= (NamedTypeDefinition.Flags)~0x00004000;
          if (this.MustBeReferenceType)
            this.flags |= (NamedTypeDefinition.Flags)0x00002000;
          else {
            ITypeDefinition baseClass = TypeHelper.EffectiveBaseClass(this);
            if (!TypeHelper.TypesAreEquivalent(baseClass, this.PlatformType.SystemObject)) {
              if (baseClass.IsClass)
                this.flags |= (NamedTypeDefinition.Flags)0x00002000;
              else if (baseClass.IsValueType)
                this.flags |= (NamedTypeDefinition.Flags)0x00001000;
            }
          }
        }
        return ((int)this.flags & 0x00002000) != 0;
      }
    }

    /// <summary>
    /// True if the type is a value type.
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    /// <value></value>
    public override bool IsValueType {
      get {
        if (((int)this.flags & 0x00004000) != 0) {
          this.flags &= (NamedTypeDefinition.Flags)~0x00004000;
          if (this.MustBeReferenceType)
            this.flags |= (NamedTypeDefinition.Flags)0x00002000;
          else {
            ITypeDefinition baseClass = TypeHelper.EffectiveBaseClass(this);
            if (!TypeHelper.TypesAreEquivalent(baseClass, this.PlatformType.SystemObject)) {
              if (baseClass.IsClass)
                this.flags |= (NamedTypeDefinition.Flags)0x00002000;
              else if (baseClass.IsValueType)
                this.flags |= (NamedTypeDefinition.Flags)0x00001000;
            }
          }
        }
        return ((int)this.flags & 0x00001000) != 0;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be reference types.
    /// </summary>
    /// <value></value>
    public bool MustBeReferenceType {
      get
        //^ ensures result == (((int)this.flags & 0x00200000) != 0);
      {
        bool result = ((int)this.flags & 0x00200000) != 0;
        //^ assume result ==> !this.MustBeValueType;
        return result;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types.
    /// </summary>
    /// <value></value>
    public bool MustBeValueType {
      get
        //^ ensures result == (((int)this.flags & 0x00010000) != 0);
      {
        bool result = ((int)this.flags & 0x00010000) != 0;
        //^ assume result ==> !this.MustBeReferenceType;
        return result;
      }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types or concrete classes with visible default constructors.
    /// </summary>
    /// <value></value>
    public bool MustHaveDefaultConstructor {
      get { return ((int)this.flags & 0x00008000) != 0; }
    }

    /// <summary>
    /// If the given generic parameter is a generic parameter of the generic method of which the given method is an instance, then return the corresponding type argument that
    /// was used to create the method instance.
    /// </summary>
    public static ITypeDefinition SpecializeIfConstructedFromApplicableTypeParameter(IGenericParameter genericParameter, IGenericMethodInstance containingMethodInstance) {
      IMethodDefinition genericMethod = containingMethodInstance.GenericMethod.ResolvedMethod;
      //^ assume genericMethod.IsGeneric;
      IEnumerator<IGenericMethodParameter> genericParameters = genericMethod.GenericParameters.GetEnumerator();
      IEnumerator<ITypeReference> genericArguments = containingMethodInstance.GenericArguments.GetEnumerator();
      while (genericParameters.MoveNext() && genericArguments.MoveNext()) {
        if (genericParameter == genericParameters.Current) return genericArguments.Current.ResolvedType;
      }
      return genericParameter;
    }

    /// <summary>
    /// If the given generic parameter is a generic parameter of the generic type of which the given type is an instance, then return the corresponding type argument that
    /// was used to create the type instance.
    /// </summary>
    public static ITypeDefinition SpecializeIfConstructedFromApplicableTypeParameter(IGenericParameter genericParameter, IGenericTypeInstanceReference containingTypeInstance) {
      ITypeDefinition genericType = containingTypeInstance.GenericType.ResolvedType;
      //^ assume genericType.IsGeneric;
      IEnumerator<IGenericTypeParameter> genericParameters = genericType.GenericParameters.GetEnumerator();
      IEnumerator<ITypeReference> genericArguments = containingTypeInstance.GenericArguments.GetEnumerator();
      while (genericParameters.MoveNext() && genericArguments.MoveNext()) {
        if (genericParameter == genericParameters.Current) return genericArguments.Current.ResolvedType;
      }
      return genericParameter;
    }

    /// <summary>
    /// Indicates if the generic type or method with this type parameter is co-, contra-, or non variant with respect to this type parameter.
    /// </summary>
    /// <value></value>
    public TypeParameterVariance Variance {
      get { return ((TypeParameterVariance)((int)this.flags >> 4)) & TypeParameterVariance.Mask; }
    }

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get {
        return this.Attributes;
      }
    }

    /// <summary>
    /// Returns the list of generic parameter declarations that collectively define this generic parameter definition.
    /// </summary>
    protected abstract IEnumerable<GenericParameterDeclaration> GetDeclarations();

    #endregion

    #region INamedTypeReference Members

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class GenericTypeParameter : GenericParameter, IGenericTypeParameter {

    //^ [NotDelayed]
    /// <summary>
    /// 
    /// </summary>
    /// <param name="definingType"></param>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <param name="variance"></param>
    /// <param name="mustBeReferenceType"></param>
    /// <param name="mustBeValueType"></param>
    /// <param name="mustHaveDefaultConstructor"></param>
    public GenericTypeParameter(NamedTypeDefinition definingType, IName name, ushort index, TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor)
      : base(name, index, variance, mustBeReferenceType, mustBeValueType, mustHaveDefaultConstructor, definingType.InternFactory)
      //^ requires definingType.IsGeneric;
    {
      this.definingType = definingType;
      //^ base;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericTypeParameterDeclaration"></param>
    protected internal void AddDeclaration(GenericTypeParameterDeclaration genericTypeParameterDeclaration) {
      lock (GlobalLock.LockingObject) {
        this.parameterDeclarations.Add(genericTypeParameterDeclaration);
      }
    }

    /// <summary>
    /// The generic type that defines this type parameter.
    /// </summary>
    /// <value></value>
    public NamedTypeDefinition DefiningType {
      get
        //^ ensures result.IsGeneric;
      {
        //^ assume this.definingType.IsGeneric; //TODO: there should be an invariant on this.definingType;
        return this.definingType;
      }
    }
    readonly NamedTypeDefinition definingType;
    // ^ invariant this.definingType.IsGeneric; //TODO Boogie: It should be possible to use non owned objects in invariants, provided that they are immutable.

    /// <summary>
    /// Calls the visitor.Visit(IGenericTypeParameter) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(IGenericTypeParameterReference) method.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IGenericTypeParameterReference)this);
    }

    /// <summary>
    /// Returns the list of generic parameter declarations that collectively define this generic parameter definition.
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<GenericParameterDeclaration> GetDeclarations() {
      return IteratorHelper.GetConversionEnumerable<GenericTypeParameterDeclaration, GenericParameterDeclaration>(this.ParameterDeclarations);
    }

    /// <summary>
    /// Gets the parameter declarations.
    /// </summary>
    /// <value>The parameter declarations.</value>
    public IEnumerable<GenericTypeParameterDeclaration> ParameterDeclarations {
      get { return this.parameterDeclarations.AsReadOnly(); }
    }
    //^ [Owned]    
    List<GenericTypeParameterDeclaration> parameterDeclarations = new List<GenericTypeParameterDeclaration>();

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public override IPlatformType PlatformType {
      get { return this.DefiningType.PlatformType; }
    }

    #region IGenericTypeParameter Members

    ITypeDefinition IGenericTypeParameter.DefiningType {
      get { return this.DefiningType; }
    }

    #endregion

    #region IReference Members

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get {
        foreach (GenericParameterDeclaration parameterDeclaration in this.ParameterDeclarations)
          yield return parameterDeclaration.SourceLocation;
      }
    }

    #endregion

    #region IGenericTypeParameterReference Members

    ITypeReference IGenericTypeParameterReference.DefiningType {
      get { return this.DefiningType; }
    }

    IGenericTypeParameter IGenericTypeParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class GlobalsClass : NamespaceTypeDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilation"></param>
    public GlobalsClass(Compilation compilation)
      : base(compilation.Result.UnitNamespaceRoot, compilation.NameTable.GetNameFor("__Globals__"), compilation.HostEnvironment.InternFactory) {
      this.compilation = compilation;
    }

    Compilation compilation;

    private void FillInWithGlobalFieldsAndMethods(List<ITypeDefinitionMember> members, IUnitNamespace unitNamespace) {
      foreach (INamespaceMember namespaceMember in unitNamespace.Members) {
        INestedUnitNamespace/*?*/ nestedUnitNamespace = namespaceMember as INestedUnitNamespace;
        if (nestedUnitNamespace != null)
          this.FillInWithGlobalFieldsAndMethods(members, nestedUnitNamespace);
        else {
          ITypeDefinitionMember/*?*/ typeMember = namespaceMember as ITypeDefinitionMember;
          if (typeMember != null) members.Add(typeMember);
        }
      }
    }

    /// <summary>
    /// Gets a value indicating whether this instance is public.
    /// </summary>
    /// <value><c>true</c> if this instance is public; otherwise, <c>false</c>.</value>
    public override bool IsPublic {
      get { return true; }
    }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    /// <value></value>
    public override IEnumerable<ITypeDefinitionMember> Members {
      get {
        if (this.members == null) {
          var memberList = new List<ITypeDefinitionMember>();
          this.FillInWithGlobalFieldsAndMethods(memberList, this.compilation.Result.UnitNamespaceRoot);
          foreach (ITypeDefinitionMember member in base.Members) {
            IMethodDefinition/*?*/ method = member as IMethodDefinition;
            if (method == null || !method.IsStaticConstructor) continue;
            memberList.Add(method);
            break;
          }
          this.members = memberList.AsReadOnly();
        }
        return this.members;
      }
    }
    IEnumerable<ITypeDefinitionMember>/*?*/ members;

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public override IPlatformType PlatformType {
      get { return this.compilation.PlatformType; }
    }

    //^ [Confined]
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return "__Globals__";
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class MethodImplementation : IMethodImplementation {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingType"></param>
    /// <param name="implementedMethod"></param>
    /// <param name="implementingMethod"></param>
    public MethodImplementation(ITypeDefinition containingType, IMethodReference implementedMethod, IMethodReference implementingMethod) {
      this.containingType = containingType;
      this.implementedMethod = implementedMethod;
      this.implementingMethod = implementingMethod;
    }

    /// <summary>
    /// The type that is explicitly implementing or overriding the base class virtual method or explicitly implementing an interface method.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingType {
      get { return this.containingType; }
    }
    readonly ITypeDefinition containingType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// A reference to the method whose implementation is being provided or overridden.
    /// </summary>
    /// <value></value>
    public IMethodReference ImplementedMethod {
      get { return this.implementedMethod; }
    }
    readonly IMethodReference implementedMethod;

    /// <summary>
    /// A reference to the method that provides the implementation.
    /// </summary>
    /// <value></value>
    public IMethodReference ImplementingMethod {
      get { return this.implementingMethod; }
    }
    readonly IMethodReference implementingMethod;

  }

  /// <summary>
  /// 
  /// </summary>
  public sealed class ModuleClass : NamespaceTypeDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilation"></param>
    public ModuleClass(Compilation compilation)
      : base(compilation.Result.UnitNamespaceRoot, compilation.NameTable.GetNameFor("<Module>"), compilation.HostEnvironment.InternFactory) {
      this.compilation = compilation;
    }

    Compilation compilation;

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public override IPlatformType PlatformType {
      get { return this.compilation.PlatformType; }
    }

    /// <summary>
    /// True if the type can be accessed from other assemblies.
    /// </summary>
    /// <value></value>
    public override bool IsPublic {
      get { return true; }
    }

    //^ [Confined]
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return "<Module>";
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class NamespaceTypeDefinition : NamedTypeDefinition, INamespaceTypeDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingUnitNamespace"></param>
    /// <param name="name"></param>
    /// <param name="internFactory"></param>
    public NamespaceTypeDefinition(IUnitNamespace containingUnitNamespace, IName name, IInternFactory internFactory)
      : base(name, internFactory) {
      this.containingUnitNamespace = containingUnitNamespace;
    }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    /// <value></value>
    public IUnitNamespace ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
    }
    readonly IUnitNamespace containingUnitNamespace;

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public override IPlatformType PlatformType {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations)
          return typeDeclaration.Compilation.PlatformType;
        return Dummy.PlatformType;
      }
    }

    //^ [Confined]
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      if (this.ContainingUnitNamespace is RootUnitNamespace)
        return this.Name.Value;
      else
        return this.ContainingUnitNamespace + "." + this.Name.Value;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDeclaration"></param>
    protected override void UpdateFlags(TypeDeclaration typeDeclaration) {
      int flags = 0;
      //^ assume typeDeclaration is NamespaceTypeDeclaration;
      if (((NamespaceTypeDeclaration)typeDeclaration).IsPublic) flags = (int)TypeMemberVisibility.Public;
      this.flags = (NamedTypeDefinition.Flags)flags;
      base.UpdateFlags(typeDeclaration);
    }

    #region INamespaceTypeDefinition Members

    /// <summary>
    /// True if the type can be accessed from other assemblies.
    /// </summary>
    /// <value></value>
    public virtual bool IsPublic {
      get {
        TypeMemberVisibility result = ((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask;
        if (result == TypeMemberVisibility.Default) {
          foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
            result = typeDeclaration.GetDefaultVisibility();
            break;
          }
        }
        return result == TypeMemberVisibility.Public;
      }
    }

    bool INamespaceTypeDefinition.IsForeignObject {
      get { return false; }
    }

    IEnumerable<ICustomAttribute> INamespaceTypeDefinition.AttributesFor(ITypeReference implementedInterface) {
      return Enumerable<ICustomAttribute>.Empty;
    }

    #endregion

    #region INamespaceMember Members

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    /// <value></value>
    public INamespaceDefinition ContainingNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IContainerMember<INamespace> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public INamespaceDefinition Container {
      get { return this.containingUnitNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<INamespaceMember> ContainingScope {
      get { return this.containingUnitNamespace; }
    }

    #endregion

    #region IDoubleDispatcher Members

    /// <summary>
    /// Calls the visitor.Visit(INamespaceTypeDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(INamespaceTypeReference) method.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((INamespaceTypeReference)this);
    }

    #endregion

    #region IReference Members


    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations)
          yield return typeDeclaration.SourceLocation;
      }
    }

    #endregion

    #region INamespaceTypeReference Members

    IUnitNamespaceReference INamespaceTypeReference.ContainingUnitNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    bool INamespaceTypeReference.KeepDistinctFromDefinition {
      get { return false; }
    }

    INamespaceTypeDefinition INamespaceTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region INamedTypeReference Members

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class NestedTypeDefinition : NamedTypeDefinition, INestedTypeDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="containingTypeDefinition"></param>
    /// <param name="name"></param>
    /// <param name="internFactory"></param>
    public NestedTypeDefinition(ITypeDefinition containingTypeDefinition, IName name, IInternFactory internFactory)
      : base(name, internFactory) {
      this.containingTypeDefinition = containingTypeDefinition;
    }

    /// <summary>
    /// If true, the type does not inherit generic parameters from its containing type.
    /// </summary>
    public bool DoesNotInheritGenericParameters {
      get { return false; }
    }

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public override IPlatformType PlatformType {
      get { return this.ContainingTypeDefinition.PlatformType; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDeclaration"></param>
    protected override void UpdateFlags(TypeDeclaration typeDeclaration) {
      //^ assume typeDeclaration is NestedTypeDeclaration;
      this.flags = (NamedTypeDefinition.Flags)((NestedTypeDeclaration)typeDeclaration).Visibility;
      base.UpdateFlags(typeDeclaration);
    }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get {
        TypeMemberVisibility result = ((TypeMemberVisibility)this.flags) & TypeMemberVisibility.Mask;
        if (result == TypeMemberVisibility.Default) {
          foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
            result = typeDeclaration.GetDefaultVisibility();
            break;
          }
        }
        return result;
      }
    }

    #region ITypeDefinitionMember Members

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingTypeDefinition {
      get { return this.containingTypeDefinition; }
    }
    ITypeDefinition containingTypeDefinition;

    #endregion

    #region IContainerMember<ITypeDefinitionMember> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public ITypeDefinition Container {
      get { return this.containingTypeDefinition; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.containingTypeDefinition; }
    }

    #endregion

    #region IDoubleDispatcher Members

    /// <summary>
    /// Calls the visitor.Visit(INestedTypeDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(INestedTypeReference) method.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((INestedTypeReference)this);
    }

    #endregion

    #region ITypeMemberReference Members

    ITypeReference ITypeMemberReference.ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INestedTypeReference Members

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region IReference Members

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations)
          yield return typeDeclaration.SourceLocation;
      }
    }

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region INamedTypeReference Members

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class NamedTypeDefinition : AggregatedScope<ITypeDefinitionMember, TypeDeclaration, IAggregatableTypeDeclarationMember>, INamedTypeDefinition {
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    protected enum Flags {
      /// <summary>
      /// 
      /// </summary>
      Abstract=0x40000000,
      /// <summary>
      /// 
      /// </summary>
      Class=0x20000000,
      /// <summary>
      /// 
      /// </summary>
      Delegate=0x10000000,
      /// <summary>
      /// 
      /// </summary>
      Enum=0x08000000,
      /// <summary>
      /// 
      /// </summary>
      Interface=0x04000000,
      /// <summary>
      /// 
      /// </summary>
      Sealed=0x02000000,
      /// <summary>
      /// 
      /// </summary>
      Static=0x01000000,
      /// <summary>
      /// 
      /// </summary>
      Struct=0x00800000,
      /// <summary>
      /// 
      /// </summary>
      ValueType=0x00400000,
      /// <summary>
      /// This type contains at least one definition method.
      /// </summary>
      HasExtensionMethod,
      /// <summary>
      /// 
      /// </summary>
      None=0x00000000,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="internFactory"></param>
    protected NamedTypeDefinition(IName name, IInternFactory internFactory) {
      this.internFactory = internFactory;
      this.name = name;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDeclaration"></param>
    protected internal void AddTypeDeclaration(TypeDeclaration typeDeclaration) {
      lock (GlobalLock.LockingObject) {
        if (this.typeDeclarations.Contains(typeDeclaration)) {
          return;
        }
        this.typeDeclarations.Add(typeDeclaration);
        this.AddContainer(typeDeclaration);
        this.UpdateFlags(typeDeclaration);
        this.UpdateGenericParameters(typeDeclaration);
      }
    }

    /// <summary>
    /// The byte alignment that values of the given type ought to have. Must be a power of 2. If zero, the alignment is decided at runtime.
    /// </summary>
    /// <value></value>
    public virtual ushort Alignment {
      get {
        if (this.alignment == 0) {
          foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
            ushort align = typeDeclaration.Alignment;
            if (align > 0) { this.alignment = (ushort)(align + 1); break; }
          }

          if (this.alignment == 0) this.alignment = 1;
        }

        return (ushort)(this.alignment - 1);
      }
    }
    ushort alignment = 0;

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get {
        if (this.attributes == null) {
          List<ICustomAttribute> attrs = this.GetAttributes();
          attrs.TrimExcess();
          this.attributes = attrs.AsReadOnly();
        }
        return this.attributes;
      }
    }
    IEnumerable<ICustomAttribute>/*?*/ attributes;

    /// <summary>
    /// Returns a list of custom attributes that describes this type declaration member.
    /// Typically, these will be derived from this.SourceAttributes. However, some source attributes
    /// might instead be persisted as metadata bits and other custom attributes may be synthesized
    /// from information not provided in the form of source custom attributes.
    /// The list is not trimmed to size, since an override of this method may call the base method
    /// and then add more attributes.
    /// </summary>
    protected virtual List<ICustomAttribute> GetAttributes() {
      List<ICustomAttribute> result = new List<ICustomAttribute>();
      LanguageSpecificCompilationHelper/*?*/ helper = null;
      foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
        if (helper == null) helper = typeDeclaration.Helper;
        result.AddRange(typeDeclaration.Attributes);
      }
      if (this.HasExtensionMethod) {
        var eattr = new Microsoft.Cci.MutableCodeModel.CustomAttribute();
        eattr.Constructor = helper.Compilation.ExtensionAttributeCtor;
        result.Add(eattr);
      }
      return result;
    }

    /// <summary>
    /// Zero or more classes from which this type is derived.
    /// For CLR types this collection is empty for interfaces and System.Object and populated with exactly one base type for all other types.
    /// </summary>
    /// <value></value>
    public virtual IEnumerable<ITypeReference> BaseClasses {
      get {
        if (this.baseClasses == null) {
          lock (GlobalLock.LockingObject) {
            if (this.baseClasses == null) {
              List<ITypeReference> baseTypes = new List<ITypeReference>();
              this.baseClasses = baseTypes.AsReadOnly(); //In case base type expression calls back here while we are busy
              this.ResolveBaseTypes(baseTypes);
            }
          }
        }
        return this.baseClasses;
      }
    }
    IEnumerable<ITypeReference>/*?*/ baseClasses;

    private MultiHashtable<IMethodImplementation> ComputeImplementationMap() {
      MultiHashtable<IMethodImplementation> result = new MultiHashtable<IMethodImplementation>();
      foreach (ITypeDefinitionMember member in base.Members) {
        MethodDefinition/*?*/ method = member as MethodDefinition;
        if (method == null) continue;
        foreach (ITypeDefinition implementedInterface in method.ImplementedInterfaces) {
          IEnumerable<ITypeDefinitionMember> interfaceMembers = implementedInterface.GetMembersNamed(method.UnqualifiedName, false);
          IMethodDefinition interfaceMethod = TypeHelper.GetMethod(interfaceMembers, method);
          if (interfaceMethod is Dummy) {
            //TODO: error
            continue;
          }
          result.Add(method.InternedKey, new MethodImplementation(this, interfaceMethod, method));
        }
      }
      foreach (IMethodReference abstractBaseClassMethod in this.GetAbstractBaseClassMehods()) {
        IMethodDefinition method = TypeHelper.GetMethod(this, abstractBaseClassMethod);
        if (method.Visibility != TypeMemberVisibility.Public) {
          //TODO: error
          continue;
        }
        result.Add(method.InternedKey, new MethodImplementation(this, abstractBaseClassMethod, method));
      }
      foreach (IMethodReference interfaceMethod in this.GetInterfaceMehods()) {
        IMethodDefinition method = TypeHelper.GetMethod(this, interfaceMethod);
        if (method.Visibility != TypeMemberVisibility.Public) {
          //TODO: error
          continue;
        }
        result.Add(method.InternedKey, new MethodImplementation(this, interfaceMethod, method));
      }
      return result;
    }

    private IEnumerable<IMethodReference> GetAbstractBaseClassMehods() {
      foreach (ITypeReference baseClassRef in this.BaseClasses) {
        foreach (IMethodDefinition abstractMethod in GetAbstractMethods(baseClassRef.ResolvedType)) {
          IMethodDefinition method = TypeHelper.GetMethod(this, abstractMethod);
          if (method is Dummy || method.Visibility != abstractMethod.Visibility)
            yield return method;
        }
      }
    }

    private static IEnumerable<IMethodDefinition> GetAbstractMethods(ITypeDefinition typeDefinition) {
      foreach (IMethodDefinition method in typeDefinition.Methods) {
        if (method.IsAbstract) yield return method;
      }
      foreach (ITypeReference baseClassRef in typeDefinition.BaseClasses) {
        foreach (IMethodDefinition abstractMethod in GetAbstractMethods(baseClassRef.ResolvedType)) {
          IMethodDefinition method = TypeHelper.GetMethod(baseClassRef.ResolvedType, abstractMethod);
          if (method is Dummy || !method.IsVirtual || method.Visibility != abstractMethod.Visibility)
            yield return abstractMethod;
        }
      }
    }

    private List<IMethodDefinition> GetInterfaceMehods() {
      List<IMethodDefinition> result = new List<IMethodDefinition>();
      foreach (ITypeReference iface in this.Interfaces)
        this.FillInInterfaceMethods(iface.ResolvedType, result);
      return result;
    }

    private void FillInInterfaceMethods(ITypeDefinition typeDefinition, List<IMethodDefinition> result) {
      result.AddRange(typeDefinition.Methods);
      foreach (ITypeReference iface in typeDefinition.Interfaces)
        this.FillInInterfaceMethods(iface.ResolvedType, result);
    }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model reference node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void DispatchAsReference(IMetadataVisitor visitor);

    /// <summary>
    /// Zero or more implementation overrides provided by the class.
    /// </summary>
    /// <value></value>
    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get {
        foreach (IMethodImplementation implementation in this.ImplementationMap.Values) {
          if (implementation.ImplementingMethod.ResolvedMethod.Visibility != TypeMemberVisibility.Public)
            yield return implementation;
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    protected Flags flags;

    /// <summary>
    /// Zero or more parameters that can be used as type annotations.
    /// </summary>
    /// <value></value>
    public virtual IEnumerable<GenericTypeParameter> GenericParameters {
      get { return this.genericParameters.AsReadOnly(); }
    }
    readonly List<GenericTypeParameter> genericParameters = new List<GenericTypeParameter>();

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    /// <value></value>
    public virtual ushort GenericParameterCount {
      get {
        int result = this.genericParameters.Count;
        //^ assume !this.IsGeneric ==> result == 0;
        //^ assume this.IsGeneric ==> result > 0;
        return (ushort)result;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    protected override ITypeDefinitionMember GetAggregatedMember(IAggregatableTypeDeclarationMember member) {
      return member.AggregatedMember;
    }

    /// <summary>
    /// 
    /// </summary>
    MultiHashtable<IMethodImplementation> ImplementationMap {
      get {
        if (this.implementationMap == null)
          this.implementationMap = this.ComputeImplementationMap();
        return this.implementationMap;
      }
    }
    MultiHashtable<IMethodImplementation>/*?*/ implementationMap;

    /// <summary>
    /// 
    /// </summary>
    protected override void InitializeIfNecessary() {
      if (!this.initialized) {
        lock (GlobalLock.LockingObject) {
          if (this.initialized) return;
          this.ProvideStaticConstructor();
          this.ProvideDefaultConstructor();
          this.ProvideDelegateMembers();
          this.initialized = true;
        }
      }
    }
    private bool initialized;

    /// <summary>
    /// True if the type name must be mangled with the generic parameter count when emitting it to a PE file.
    /// </summary>
    public bool MangleName { get { return true; } }

    private void ProvideDefaultConstructor() {
      if (!this.IsClass) return;
      TypeDeclaration/*?*/ tDecl = null;
      foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
        if (tDecl == null) tDecl = typeDeclaration as TypeDeclaration;
        foreach (ITypeDeclarationMember member in typeDeclaration.TypeDeclarationMembers) {
          MethodDeclaration/*?*/ method = member as MethodDeclaration;
          if (method == null) continue;
          if (!method.IsSpecialName || method.Name.Value != ".ctor") continue;
          return; //There is a declared constructor
        }
      }
      if (tDecl == null) return;
      List<Statement> statements = new List<Statement>();
      BlockStatement body = new BlockStatement(statements, SourceDummy.SourceLocation);
      statements.Add(new FieldInitializerStatement());
      ThisReference thisArg = new ThisReference(body, SourceDummy.SourceLocation);
      INameTable nametable = tDecl.Helper.Compilation.NameTable;
      NameDeclaration ctor = new NameDeclaration(nametable.Ctor, SourceDummy.SourceLocation);
      List<ParameterDeclaration> ctorParameters = new List<ParameterDeclaration>(0);
      TypeExpression voidType = TypeExpression.For(this.PlatformType.SystemVoid);
      ITypeDefinition/*?*/ baseType = TypeHelper.BaseClass(this);
      if (baseType == null) baseType = tDecl.Compilation.PlatformType.SystemObject.ResolvedType;
      IMethodDefinition baseCtor = TypeHelper.GetMethod(baseType, nametable.Ctor);
      ResolvedMethodCall baseCtorCall = new ResolvedMethodCall(baseCtor, thisArg, new List<Expression>(0), SourceDummy.SourceLocation);
      ExpressionStatement callBaseCtor = new ExpressionStatement(baseCtorCall);
      statements.Add(callBaseCtor);
      List<SourceCustomAttribute> attrs = new List<SourceCustomAttribute>(1);
      MethodDeclaration ctorDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.SpecialName|MethodDeclaration.Flags.IsCompilerGenerated,
          TypeMemberVisibility.Public, voidType, null, ctor, null, ctorParameters, null, body, SourceDummy.SourceLocation);
      ctorDeclaration.SetContainingTypeDeclaration(tDecl, true);
      this.AddMemberToCache(new MethodDefinition(ctorDeclaration));
    }

    private void ProvideDelegateMembers() {
      if (!this.IsDelegate) return;
      NamespaceDelegateDeclaration/*?*/ nsDelegateDeclaration = null;
      NestedDelegateDeclaration/*?*/ nestedDelegateDeclaration = null;
      foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
        nsDelegateDeclaration = typeDeclaration as NamespaceDelegateDeclaration;
        if (nsDelegateDeclaration != null) break;
        nestedDelegateDeclaration = typeDeclaration as NestedDelegateDeclaration;
        if (nestedDelegateDeclaration != null) break;
      }
      TypeDeclaration delegateDeclaration;
      SignatureDeclaration delegateSignature;
      if (nsDelegateDeclaration != null) {
        delegateDeclaration = nsDelegateDeclaration;
        delegateSignature = nsDelegateDeclaration.Signature;
      } else if (nestedDelegateDeclaration != null) {
        delegateDeclaration = nestedDelegateDeclaration;
        delegateSignature = nestedDelegateDeclaration.Signature;
      } else {
        //^ assume false; //this.IsDelegate should never be true when none of the type declarations is a delegate declaration.
        return;
      }
      INameTable nametable = delegateDeclaration.Helper.Compilation.NameTable;

      NameDeclaration ctor = new NameDeclaration(nametable.Ctor, SourceDummy.SourceLocation);
      List<ParameterDeclaration> ctorParameters = new List<ParameterDeclaration>();
      TypeExpression voidType = TypeExpression.For(this.PlatformType.SystemVoid);
      BlockStatement emptyBody = BlockStatement.CreateDummyFor(SourceDummy.SourceLocation);
      MethodDeclaration ctorDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.SpecialName|MethodDeclaration.Flags.IsCompilerGenerated,
          TypeMemberVisibility.Public, voidType, null, ctor, null, ctorParameters, null, emptyBody, SourceDummy.SourceLocation);
      TypeExpression objectType = TypeExpression.For(this.PlatformType.SystemObject);
      NameDeclaration objectName = new NameDeclaration(nametable.GetNameFor("object"), SourceDummy.SourceLocation);
      ctorParameters.Add(new ParameterDeclaration(null, objectType, objectName, null, 0, false, false, false, false, SourceDummy.SourceLocation));
      TypeExpression intPtrType = TypeExpression.For(this.PlatformType.SystemIntPtr);
      NameDeclaration methodName = new NameDeclaration(nametable.GetNameFor("method"), SourceDummy.SourceLocation);
      ctorParameters.Add(new ParameterDeclaration(null, intPtrType, methodName, null, 1, false, false, false, false, SourceDummy.SourceLocation));
      ctorDeclaration.SetContainingTypeDeclaration(delegateDeclaration, true);

      NameDeclaration invoke = new NameDeclaration(nametable.Invoke, SourceDummy.SourceLocation);
      List<ParameterDeclaration> invokeParameters = new List<ParameterDeclaration>();
      emptyBody = BlockStatement.CreateDummyFor(SourceDummy.SourceLocation);
      MethodDeclaration invokeDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.Virtual|MethodDeclaration.Flags.IsCompilerGenerated,
          TypeMemberVisibility.Public, delegateSignature.Type, null, invoke, null, invokeParameters, null, emptyBody, SourceDummy.SourceLocation);
      foreach (ParameterDeclaration delPar in delegateSignature.Parameters)
        invokeParameters.Add(delPar.MakeShallowCopyFor(invokeDeclaration, delegateDeclaration.OuterDummyBlock));
      invokeDeclaration.SetContainingTypeDeclaration(delegateDeclaration, true);

      NameDeclaration beginInvoke = new NameDeclaration(nametable.GetNameFor("BeginInvoke"), SourceDummy.SourceLocation);
      List<ParameterDeclaration> beginInvokeParameters = new List<ParameterDeclaration>();
      TypeExpression iasyncResultType = TypeExpression.For(this.PlatformType.SystemIAsyncResult);
      emptyBody = BlockStatement.CreateDummyFor(SourceDummy.SourceLocation);
      MethodDeclaration beginInvokeDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.Virtual|MethodDeclaration.Flags.IsCompilerGenerated,
          TypeMemberVisibility.Public, iasyncResultType, null, beginInvoke, null, beginInvokeParameters, null, emptyBody, SourceDummy.SourceLocation);
      foreach (ParameterDeclaration delPar in delegateSignature.Parameters)
        beginInvokeParameters.Add(delPar.MakeShallowCopyFor(beginInvokeDeclaration, delegateDeclaration.OuterDummyBlock));
      TypeExpression asyncCallbackType = TypeExpression.For(this.PlatformType.SystemAsyncCallback);
      NameDeclaration callBackName = new NameDeclaration(nametable.GetNameFor("callback"), SourceDummy.SourceLocation);
      beginInvokeParameters.Add(new ParameterDeclaration(null, asyncCallbackType, callBackName, null, (ushort)beginInvokeParameters.Count, false, false, false, false, SourceDummy.SourceLocation));
      beginInvokeParameters.Add(new ParameterDeclaration(null, objectType, objectName, null, (ushort)beginInvokeParameters.Count, false, false, false, false, SourceDummy.SourceLocation));
      beginInvokeDeclaration.SetContainingTypeDeclaration(delegateDeclaration, true);

      NameDeclaration endInvoke = new NameDeclaration(nametable.GetNameFor("EndInvoke"), SourceDummy.SourceLocation);
      List<ParameterDeclaration> endInvokeParameters = new List<ParameterDeclaration>();
      emptyBody = BlockStatement.CreateDummyFor(SourceDummy.SourceLocation);
      MethodDeclaration endInvokeDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.Virtual|MethodDeclaration.Flags.IsCompilerGenerated,
          TypeMemberVisibility.Public, voidType, null, endInvoke, null, endInvokeParameters, null, emptyBody, SourceDummy.SourceLocation);
      NameDeclaration resultName = new NameDeclaration(nametable.GetNameFor("result"), SourceDummy.SourceLocation);
      endInvokeParameters.Add(new ParameterDeclaration(null, iasyncResultType, resultName, null, 0, false, false, false, false, SourceDummy.SourceLocation));
      endInvokeDeclaration.SetContainingTypeDeclaration(delegateDeclaration, true);

      this.AddMemberToCache(new MethodDefinition(ctorDeclaration));
      this.AddMemberToCache(new MethodDefinition(invokeDeclaration));
      this.AddMemberToCache(new MethodDefinition(beginInvokeDeclaration));
      this.AddMemberToCache(new MethodDefinition(endInvokeDeclaration));
    }

    private void ProvideStaticConstructor() {
      if (this.IsEnum) return;
      TypeDeclaration/*?*/ tDecl = null;
      List<FieldDeclaration> fieldsToIntialize = new List<FieldDeclaration>();
      foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
        if (tDecl == null) tDecl = typeDeclaration as TypeDeclaration;
        foreach (ITypeDeclarationMember member in typeDeclaration.TypeDeclarationMembers) {
          FieldDeclaration/*?*/ fieldDecl = member as FieldDeclaration;
          if (fieldDecl != null && fieldDecl.IsStatic && !fieldDecl.IsCompileTimeConstant && fieldDecl.Initializer != null) {
            fieldsToIntialize.Add(fieldDecl); continue;
          }
          MethodDeclaration/*?*/ method = member as MethodDeclaration;
          if (method == null) continue;
          if (!method.IsSpecialName || method.Name.Value != ".cctor") continue;
          if (IteratorHelper.EnumerableIsNotEmpty(method.Parameters)) continue;
          return; //There is a declared static constructor
        }
      }
      if (tDecl == null || fieldsToIntialize.Count == 0) return;
      INameTable nametable = tDecl.Helper.Compilation.NameTable;
      NameDeclaration cctor = new NameDeclaration(nametable.Cctor, SourceDummy.SourceLocation);
      List<ParameterDeclaration> cctorParameters = new List<ParameterDeclaration>(0);
      TypeExpression voidType = TypeExpression.For(this.PlatformType.SystemVoid);
      List<Statement> statements = new List<Statement>();
      foreach (FieldDeclaration field in fieldsToIntialize)
        field.AddInitializingAssignmentsTo(statements);
      BlockStatement body = new BlockStatement(statements, SourceDummy.SourceLocation);
      MethodDeclaration cctorDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.SpecialName|MethodDeclaration.Flags.Static|MethodDeclaration.Flags.IsCompilerGenerated,
          TypeMemberVisibility.Public, voidType, null, cctor, null, cctorParameters, null, body, SourceDummy.SourceLocation);
      cctorDeclaration.SetContainingTypeDeclaration(tDecl, true);
      this.AddMemberToCache(new MethodDefinition(cctorDeclaration));
    }

    /// <summary>
    /// Zero or more interfaces implemented by this type.
    /// </summary>
    /// <value></value>
    public virtual IEnumerable<ITypeReference> Interfaces {
      get {
        if (this.interfaces == null) {
          lock (GlobalLock.LockingObject) {
            if (this.interfaces == null) {
              List<ITypeReference> interfaces = new List<ITypeReference>();
              this.interfaces = interfaces.AsReadOnly(); //In case interface expression calls back here while we are busy
              this.ResolveInterfaces(interfaces);
            }
          }
        }
        return this.interfaces;
      }
    }
    IEnumerable<ITypeReference>/*?*/ interfaces;

    /// <summary>
    /// An instance of this generic type that has been obtained by using the generic parameters as the arguments.
    /// Use this instance to look up members
    /// </summary>
    /// <value></value>
    public IGenericTypeInstanceReference InstanceType {
      get
        //^^ requires this.IsGeneric;
        //^^ ensures !result.IsGeneric;
      {
        if (this.instanceType == null) {
          lock (GlobalLock.LockingObject) {
            if (this.instanceType == null) {
              List<ITypeReference> arguments = new List<ITypeReference>();
              foreach (GenericTypeParameter gpar in this.GenericParameters) arguments.Add(gpar);
              //^ assume this.ResolvedType.IsGeneric;
              this.instanceType = GenericTypeInstance.GetGenericTypeInstance(this, arguments, this.InternFactory); //unsatisfied precondition: requires genericType.ResolvedType.IsGeneric;
            }
          }
        }
        //^ assume !this.instanceType.IsGeneric;
        return this.instanceType;
      }
    }
    IGenericTypeInstanceReference/*?*/ instanceType;
    //^ invariant instanceType != null ==> !instanceType.IsGeneric;

    /// <summary>
    /// True if the type may not be instantiated.
    /// </summary>
    /// <value></value>
    public virtual bool IsAbstract {
      get {
        return (this.flags & (Flags.Abstract | Flags.Interface)) != 0;
      }
    }

    /// <summary>
    /// True if the type is a class (it is not an interface or type parameter and does not extend a special base class).
    /// Corresponds to C# class.
    /// </summary>
    /// <value></value>
    public bool IsClass {
      get {
        return (this.flags & Flags.Class) != 0;
      }
    }

    /// <summary>
    /// True if the type is a delegate (it extends System.MultiCastDelegate). Corresponds to C# delegate
    /// </summary>
    /// <value></value>
    public bool IsDelegate {
      get {
        return (this.flags & Flags.Delegate) != 0;
      }
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    /// <value></value>
    public bool IsEnum {
      get {
        return (this.flags & Flags.Enum) != 0;
      }
    }

    /// <summary>
    /// True if this type is parameterized (this.GenericParameters is a non empty collection).
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get
        //^ ensures result == this.genericParameters.Count > 0;
      {
        return this.genericParameters.Count > 0;
      }
    }

    /// <summary>
    /// True if the type is an interface.
    /// </summary>
    /// <value></value>
    public virtual bool IsInterface {
      get {
        return (this.flags & Flags.Interface) != 0;
      }
    }

    /// <summary>
    /// True if the type is a reference type. A reference type is non static class or interface or a suitably constrained type parameter.
    /// A type parameter for which MustBeReferenceType (the class constraint in C#) is true returns true for this property
    /// as does a type parameter with a constraint that is a class.
    /// </summary>
    /// <value></value>
    public virtual bool IsReferenceType {
      get { return (this.flags & (Flags.Enum | Flags.ValueType | Flags.Static)) == 0; }
    }

    /// <summary>
    /// True if the type may not be subtyped.
    /// </summary>
    /// <value></value>
    public virtual bool IsSealed {
      get {
        return (this.flags & Flags.Sealed) != 0;
      }
    }

    /// <summary>
    /// True if the type is an abstract sealed class that directly extends System.Object and declares no constructors.
    /// </summary>
    /// <value></value>
    public virtual bool IsStatic {
      get {
        return (this.flags & Flags.Static) != 0;
      }
    }

    /// <summary>
    /// True if this type contains one or more extension method.
    /// </summary>
    public bool HasExtensionMethod {
      get {
        foreach (ITypeDefinitionMember member in this.Members) {
          MethodDefinition method = member as MethodDefinition;
          if (method != null && method.IsExtensionMethod)
            return true;
        }
        return false;
      }
    }

    /// <summary>
    /// True if the type is a value type.
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    /// <value></value>
    public virtual bool IsValueType {
      get {
        return (this.flags & Flags.ValueType) != 0;
      }
    }

    /// <summary>
    /// True if the type is a struct (its not Primitive, is sealed and base is System.ValueType).
    /// </summary>
    /// <value></value>
    public virtual bool IsStruct {
      get {
        return (this.flags & Flags.Struct) != 0;
      }
    }

    /// <summary>
    /// The name of the type.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public abstract IPlatformType PlatformType { get; }

    /// <summary>
    /// Zero or more private type members generated by the compiler for implementation purposes. These members
    /// are only available after a complete visit of all of the other members of the type, including the bodies of methods.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
          foreach (ITypeDefinitionMember member in typeDeclaration.PrivateHelperMembers)
            yield return member;
        }
      }
    }

    private void ResolveBaseTypes(List<ITypeReference> baseTypes) {
      if (this.IsDelegate) { baseTypes.Add(this.PlatformType.SystemMulticastDelegate); return; }
      if (this.IsEnum) { baseTypes.Add(this.PlatformType.SystemEnum); return; }
      if (this.IsInterface || this == this.PlatformType.SystemObject) { return; }
      if (this.IsValueType) { baseTypes.Add(this.PlatformType.SystemValueType); return; }
      foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
        int i = 0, n = baseTypes.Count;
        foreach (TypeExpression typeExpression in typeDeclaration.BaseTypes) {
          ITypeDefinition referencedType = typeExpression.ResolvedType;
          if (referencedType.IsInterface) break;
          if (i >= n) baseTypes.Add(referencedType);
        }
      }
      if (baseTypes.Count == 0) baseTypes.Add(this.PlatformType.SystemObject);
    }

    private void ResolveInterfaces(List<ITypeReference> interfaces) {
      //TODO: should interface list be flattened here to match requirements of metadata?
      foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
        int i = 0, n = interfaces.Count;
        foreach (TypeExpression typeExpression in typeDeclaration.BaseTypes) {
          ITypeDefinition referencedType = typeExpression.ResolvedType;
          if (!referencedType.IsInterface) continue;
          if (i >= n) interfaces.Add(referencedType);
        }
      }
    }

    /// <summary>
    /// Declarative security actions for this type. Will be empty if this.HasSecurity is false.
    /// </summary>
    /// <value></value>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
          foreach (ISecurityAttribute securityAttribute in typeDeclaration.SecurityAttributes)
            yield return securityAttribute;
        }
      }
    }

    /// <summary>
    /// Size of an object of this type. In bytes. If zero, the size is unspecified and will be determined at runtime.
    /// </summary>
    public virtual uint SizeOf {
      get {
        if (this.sizeOf == 0) {
          foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
            uint size = typeDeclaration.SizeOf;
            if (size > 0) { this.sizeOf = size+1; break; }
          }
          if (this.sizeOf == 0) {
            if (this.IsStruct && IteratorHelper.EnumerableIsEmpty(this.Fields))
              this.sizeOf = 2;
            else
              this.sizeOf = 1;
          }
        }
        return this.sizeOf-1;
      }
    }
    uint sizeOf;

    //^ [Confined]
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive" CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    /// <value></value>
    public virtual PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
      //TODO: need to return something else when compiling mscorlib and this is a special type
      //get a map from the compilation's target platform
    }

    /// <summary>
    /// A list of the type declarations that collectively define this type definition.
    /// </summary>
    public IEnumerable<TypeDeclaration> TypeDeclarations {
      get { return this.typeDeclarations.AsReadOnly(); }
    }
    readonly List<TypeDeclaration> typeDeclarations = new List<TypeDeclaration>();

    /// <summary>
    /// Returns a reference to the underlying (integral) type on which this (enum) type is based.
    /// </summary>
    /// <value></value>
    public ITypeReference UnderlyingType {
      get
        //^^ requires this.IsEnum;
      {
        //^ assume this.underlyingType != null; //Follows from the precondition and the implementation of UpdateFlags
        return this.underlyingType;
      }
    }
    ITypeReference/*?*/ underlyingType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeDeclaration"></param>
    protected virtual void UpdateFlags(TypeDeclaration typeDeclaration) {
      if (typeDeclaration.IsAbstract) this.flags |= Flags.Abstract;
      IClassDeclaration/*?*/ classDeclaration = typeDeclaration as IClassDeclaration;
      if (classDeclaration != null) {
        this.flags |= Flags.Class;
        if (classDeclaration.IsStatic) this.flags |= Flags.Static;
      }
      if (typeDeclaration is IDelegateDeclaration) this.flags |= Flags.Delegate | Flags.Sealed;
      if (typeDeclaration is IInterfaceDeclaration) this.flags |= Flags.Interface;
      if (typeDeclaration is IStructDeclaration) this.flags |= Flags.ValueType | Flags.Struct;
      if (typeDeclaration.IsSealed) this.flags |= Flags.Sealed;
      IEnumDeclaration/*?*/ enumDeclaration = typeDeclaration as IEnumDeclaration;
      if (enumDeclaration != null) {
        this.flags |= Flags.ValueType | Flags.Enum;
        if (enumDeclaration.UnderlyingType == null) {
          if (this.underlyingType == null)
            this.underlyingType = typeDeclaration.Compilation.PlatformType.SystemInt32.ResolvedType;
        } else
          this.underlyingType = enumDeclaration.UnderlyingType.ResolvedType;
      }
    }

    private void UpdateGenericParameters(TypeDeclaration typeDeclaration) {
      ushort i = 0, n = (ushort)this.genericParameters.Count;
      foreach (GenericTypeParameterDeclaration genericTypeParameterDeclaration in typeDeclaration.GenericParameters) {
        if (i == n) {
          //^ assume this.IsGeneric; //unsatisfied precondition: requires definingType.IsGeneric;
          this.genericParameters.Add(
            new GenericTypeParameter(this, genericTypeParameterDeclaration.Name, i, genericTypeParameterDeclaration.Variance,
            genericTypeParameterDeclaration.MustBeReferenceType, genericTypeParameterDeclaration.MustBeValueType, genericTypeParameterDeclaration.MustHaveDefaultConstructor));
          n++;
        }
        this.genericParameters[i].AddDeclaration(genericTypeParameterDeclaration);
        i++;
      }
    }

    /// <summary>
    /// True if this type uses the specified method to implement an interface method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns></returns>
    public bool UsesToImplementAnInterfaceMethod(MethodDefinition method) {
      return this.ImplementationMap.GetValuesFor(method.InternedKey).GetEnumerator().MoveNext();
    }

    #region ITypeDefinition Members

    IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters {
      get {
        return IteratorHelper.GetConversionEnumerable<GenericTypeParameter, IGenericTypeParameter>(this.GenericParameters);
      }
    }

    /// <summary>
    /// Layout of the type.
    /// </summary>
    public LayoutKind Layout {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations) {
          LayoutKind layout = typeDeclaration.Layout;
          if (layout != LayoutKind.Auto) return layout;
        }
        return LayoutKind.Auto;
      }
    }

    /// <summary>
    /// True if the type has special name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return false; } //TODO: when could it be special?
    }

    /// <summary>
    /// Is this imported from COM type library
    /// </summary>
    /// <value></value>
    public bool IsComObject {
      get { return false; } //TODO: when could this be true?
    }

    /// <summary>
    /// True if this type is serializable.
    /// </summary>
    /// <value></value>
    public bool IsSerializable {
      get { return false; } //TODO: get this from a custom attribute
    }

    /// <summary>
    /// Is type initialized anytime before first access to static field
    /// </summary>
    /// <value></value>
    public bool IsBeforeFieldInit {
      get { return !this.IsDelegate; } //TODO: when could this be false?
    }

    /// <summary>
    /// Default marshalling of the Strings in this class.
    /// </summary>
    /// <value></value>
    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; } //TODO: get this from a custom attribute
    }

    /// <summary>
    /// True if this type gets special treatment from the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get { return false; } //TODO: when could this be true?
    }

    /// <summary>
    /// True if this type has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    /// <value></value>
    public bool HasDeclarativeSecurity {
      get { return false; } //TODO: get this from the attributes
    }

    /// <summary>
    /// Zero or more events defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IEventDefinition>(this.Members); }
    }

    /// <summary>
    /// Zero or more fields defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IFieldDefinition>(this.Members); }
    }

    /// <summary>
    /// Needed because base.Members cannot be referred to directly from inside an iterator.
    /// </summary>
    private IEnumerable<ITypeDefinitionMember> GetBaseMembers() {
      return base.Members;
    }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    /// <value></value>
    public override IEnumerable<ITypeDefinitionMember> Members {
      get {
        foreach (ITypeDefinitionMember member in this.GetBaseMembers()) {
          yield return member;
          IPropertyDefinition/*?*/ propertyDef = member as IPropertyDefinition;
          if (propertyDef != null) {
            foreach (IMethodReference accessor in propertyDef.Accessors) yield return accessor.ResolvedMethod;
            continue;
          }
          IEventDefinition/*?*/ eventDef = member as IEventDefinition;
          if (eventDef != null) {
            foreach (IMethodReference accessor in eventDef.Accessors) yield return accessor.ResolvedMethod;
          }
        }
      }
    }

    /// <summary>
    /// Zero or more methods defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IMethodDefinition>(this.Members); }
    }

    /// <summary>
    /// Zero or more nested types defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, INestedTypeDefinition>(this.Members); }
    }

    /// <summary>
    /// Zero or more properties defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IPropertyDefinition>(this.Members); }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get {
        return this.Attributes;
      }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get {
        foreach (TypeDeclaration declaration in this.TypeDeclarations)
          yield return declaration.SourceLocation;
      }
    }

    #endregion

    #region ITypeReference Members

    /// <summary>
    /// Indicates if this type reference resolved to an alias rather than a type
    /// </summary>
    /// <value></value>
    public bool IsAlias {
      get { return false; }
    }

    /// <summary>
    /// Gives the alias for the type
    /// </summary>
    /// <value></value>
    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    /// <summary>
    /// The list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
    /// </summary>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    /// <summary>
    /// This type has one or more custom modifiers associated with it.
    /// </summary>
    public bool IsModified {
      get { return false; }
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    /// <summary>
    /// A collection of methods that associate unique integers with metadata model entities.
    /// The association is based on the identities of the entities and the factory does not retain
    /// references to the given metadata model objects.
    /// </summary>
    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    /// <summary>
    /// Returns the unique interned key associated with the type. This takes unification/aliases/custom modifiers into account.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    #endregion
  }

}
