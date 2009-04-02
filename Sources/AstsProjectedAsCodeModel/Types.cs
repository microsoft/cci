//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Cci.UtilityDataStructures;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  //  Issue: Why inherit from TypeDefinition instead of SystemDefinedStructuralType?
  public abstract class GenericParameter : TypeDefinition, IGenericParameter {

    //^ [NotDelayed]
    protected GenericParameter(IName name, ushort index, TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor, IInternFactory internFactory)
      : base(internFactory) {
      int flags = (int)variance << 4;
      if (mustBeReferenceType) flags |= 0x00200000;
      if (mustBeValueType) flags |= 0x00010000;
      if (mustHaveDefaultConstructor) flags |= 0x00008000;
      flags |= 0x00004000; //IsReferenceType and IsValueType still have to be computed
      this.index = index;
      this.name = name;
      //^ base;
      this.flags |= (TypeDefinition.Flags)flags;
    }

    public override IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

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

    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    public override bool IsReferenceType {
      get {
        if (((int)this.flags & 0x00004000) != 0) {
          this.flags &= (TypeDefinition.Flags)~0x00004000;
          if (this.MustBeReferenceType)
            this.flags |= (TypeDefinition.Flags)0x00002000;
          else {
            ITypeDefinition baseClass = TypeHelper.EffectiveBaseClass(this);
            if (!TypeHelper.TypesAreEquivalent(baseClass, this.PlatformType.SystemObject)) {
              if (baseClass.IsClass)
                this.flags |= (TypeDefinition.Flags)0x00002000;
              else if (baseClass.IsValueType)
                this.flags |= (TypeDefinition.Flags)0x00001000;
            }
          }
        }
        return ((int)this.flags & 0x00002000) != 0;
      }
    }

    public override bool IsValueType {
      get {
        if (((int)this.flags & 0x00004000) != 0) {
          this.flags &= (TypeDefinition.Flags)~0x00004000;
          if (this.MustBeReferenceType)
            this.flags |= (TypeDefinition.Flags)0x00002000;
          else {
            ITypeDefinition baseClass = TypeHelper.EffectiveBaseClass(this);
            if (!TypeHelper.TypesAreEquivalent(baseClass, this.PlatformType.SystemObject)) {
              if (baseClass.IsClass)
                this.flags |= (TypeDefinition.Flags)0x00002000;
              else if (baseClass.IsValueType)
                this.flags |= (TypeDefinition.Flags)0x00001000;
            }
          }
        }
        return ((int)this.flags & 0x00001000) != 0;
      }
    }

    public bool MustBeReferenceType {
      get
        //^ ensures result == (((int)this.flags & 0x00200000) != 0);
      {
        bool result = ((int)this.flags & 0x00200000) != 0;
        //^ assume result ==> !this.MustBeValueType;
        return result;
      }
    }

    public bool MustBeValueType {
      get
        //^ ensures result == (((int)this.flags & 0x00010000) != 0);
      {
        bool result = ((int)this.flags & 0x00010000) != 0;
        //^ assume result ==> !this.MustBeReferenceType;
        return result;
      }
    }

    public bool MustHaveDefaultConstructor {
      get { return ((int)this.flags & 0x00008000) != 0; }
    }

    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

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

    public TypeParameterVariance Variance {
      get { return ((TypeParameterVariance)((int)this.flags >> 4)) & TypeParameterVariance.Mask; }
    }

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get {
        return this.Attributes;
      }
    }

    protected abstract IEnumerable<GenericParameterDeclaration> GetDeclarations();

    #endregion

    #region INamedTypeReference Members

    INamedTypeDefinition INamedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  public class GenericTypeParameter : GenericParameter, IGenericTypeParameter {

    //^ [NotDelayed]
    public GenericTypeParameter(TypeDefinition definingType, IName name, ushort index, TypeParameterVariance variance, bool mustBeReferenceType, bool mustBeValueType, bool mustHaveDefaultConstructor)
      : base(name, index, variance, mustBeReferenceType, mustBeValueType, mustHaveDefaultConstructor, definingType.InternFactory)
      //^ requires definingType.IsGeneric;
    {
      this.definingType = definingType;
      //^ base;
    }

    protected internal void AddDeclaration(GenericTypeParameterDeclaration genericTypeParameterDeclaration) {
      lock (GlobalLock.LockingObject) {
        this.parameterDeclarations.Add(genericTypeParameterDeclaration);
      }
    }

    public TypeDefinition DefiningType {
      get
        //^ ensures result.IsGeneric;
      {
        //^ assume this.definingType.IsGeneric; //TODO: there should be an invariant on this.definingType;
        return this.definingType;
      }
    }
    readonly TypeDefinition definingType;
    // ^ invariant this.definingType.IsGeneric; //TODO Boogie: It should be possible to use non owned objects in invariants, provided that they are immutable.

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    protected override IEnumerable<GenericParameterDeclaration> GetDeclarations() {
      return IteratorHelper.GetConversionEnumerable<GenericTypeParameterDeclaration, GenericParameterDeclaration>(this.ParameterDeclarations);
    }

    public IEnumerable<GenericTypeParameterDeclaration> ParameterDeclarations {
      get { return this.parameterDeclarations.AsReadOnly(); }
    }
    //^ [Owned]    
    List<GenericTypeParameterDeclaration> parameterDeclarations = new List<GenericTypeParameterDeclaration>();

    public override IPlatformType PlatformType {
      get { return this.DefiningType.PlatformType; }
    }

    #region IGenericTypeParameter Members

    ITypeDefinition IGenericTypeParameter.DefiningType {
      get { return this.DefiningType; }
    }

    #endregion

    #region IReference Members

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

  public sealed class GlobalsClass : NamespaceTypeDefinition {

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

    public override bool IsPublic {
      get { return true; }
    }

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

    public override IPlatformType PlatformType {
      get { return this.compilation.PlatformType; }
    }

    //^ [Confined]
    public override string ToString() {
      return "__Globals__";
    }
  }

  public sealed class MethodImplementation : IMethodImplementation {

    public MethodImplementation(ITypeDefinition containingType, IMethodReference implementedMethod, IMethodReference implementingMethod) {
      this.containingType = containingType;
      this.implementedMethod = implementedMethod;
      this.implementingMethod = implementingMethod;
    }

    public ITypeDefinition ContainingType {
      get { return this.containingType; }
    }
    readonly ITypeDefinition containingType;

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IMethodReference ImplementedMethod {
      get { return this.implementedMethod; }
    }
    readonly IMethodReference implementedMethod;

    public IMethodReference ImplementingMethod {
      get { return this.implementingMethod; }
    }
    readonly IMethodReference implementingMethod;

  }

  public sealed class ModuleClass : NamespaceTypeDefinition {

    public ModuleClass(Compilation compilation)
      : base(compilation.Result.UnitNamespaceRoot, compilation.NameTable.GetNameFor("<Module>"), compilation.HostEnvironment.InternFactory) {
      this.compilation = compilation;
    }

    Compilation compilation;

    public override IPlatformType PlatformType {
      get { return this.compilation.PlatformType; }
    }

    public override bool IsPublic {
      get { return true; }
    }

    //^ [Confined]
    public override string ToString() {
      return "<Module>";
    }
  }

  public class NamespaceTypeDefinition : TypeDefinition, INamespaceTypeDefinition {

    public NamespaceTypeDefinition(IUnitNamespace containingUnitNamespace, IName name, IInternFactory internFactory)
      : base(internFactory) {
      this.containingUnitNamespace = containingUnitNamespace;
      this.name = name;
    }

    public IUnitNamespace ContainingUnitNamespace {
      get { return this.containingUnitNamespace; }
    }
    readonly IUnitNamespace containingUnitNamespace;

    public override IPlatformType PlatformType {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations)
          return typeDeclaration.Compilation.PlatformType;
        return Dummy.PlatformType;
      }
    }

    //^ [Confined]
    public override string ToString() {
      if (this.ContainingUnitNamespace is RootUnitNamespace)
        return this.Name.Value;
      else
        return this.ContainingUnitNamespace + "." + this.Name.Value;
    }

    protected override void UpdateFlags(TypeDeclaration typeDeclaration) {
      int flags = 0;
      //^ assume typeDeclaration is NamespaceTypeDeclaration;
      if (((NamespaceTypeDeclaration)typeDeclaration).IsPublic) flags = (int)TypeMemberVisibility.Public;
      this.flags = (TypeDefinition.Flags)flags;
      base.UpdateFlags(typeDeclaration);
    }

    #region INamespaceTypeDefinition Members

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

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return this.ContainingUnitNamespace; }
    }

    #endregion

    #region IContainerMember<INamespace> Members

    public INamespaceDefinition Container {
      get { return this.containingUnitNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return this.containingUnitNamespace; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    #endregion

    #region IDoubleDispatcher Members

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #endregion

    #region IReference Members


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

  public class NestedTypeDefinition : TypeDefinition, INestedTypeDefinition {

    public NestedTypeDefinition(ITypeDefinition containingTypeDefinition, IName name, IInternFactory internFactory)
      : base(internFactory) {
      this.containingTypeDefinition = containingTypeDefinition;
      this.name = name;
    }

    public override IPlatformType PlatformType {
      get { return this.ContainingTypeDefinition.PlatformType; }
    }

    protected override void UpdateFlags(TypeDeclaration typeDeclaration) {
      //^ assume typeDeclaration is NestedTypeDeclaration;
      this.flags = (TypeDefinition.Flags)((NestedTypeDeclaration)typeDeclaration).Visibility;
      base.UpdateFlags(typeDeclaration);
    }

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

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.containingTypeDefinition; }
    }
    ITypeDefinition containingTypeDefinition;

    #endregion

    #region IContainerMember<ITypeDefinitionMember> Members

    public ITypeDefinition Container {
      get { return this.containingTypeDefinition; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.containingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    #endregion

    #region IDoubleDispatcher Members

    /// <summary>
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
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

    public IEnumerable<ILocation> Locations {
      get {
        foreach (TypeDeclaration typeDeclaration in this.TypeDeclarations)
          yield return typeDeclaration.SourceLocation;
      }
    }

    #endregion

    #region ITypeMemberReference Members

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

  public abstract class TypeDefinition : AggregatedScope<ITypeDefinitionMember, TypeDeclaration, IAggregatableTypeDeclarationMember>, ITypeDefinition {
    [Flags]
    protected enum Flags {
      Abstract=0x40000000,
      Class=0x20000000,
      Delegate=0x10000000,
      Enum=0x08000000,
      Interface=0x04000000,
      Sealed=0x02000000,
      Static=0x01000000,
      Struct=0x00800000,
      ValueType=0x00400000,
      None=0x00000000,
    }

    protected TypeDefinition(IInternFactory internFactory) {
      this.internFactory = internFactory;
    }

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

    public IEnumerable<ICustomAttribute> Attributes {
      get {
        foreach (TypeDeclaration typeDeclaration in this.typeDeclarations) {
          foreach (ICustomAttribute customAttribute in typeDeclaration.Attributes)
            yield return customAttribute;
        }
      }
    }

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
          if (interfaceMethod == Dummy.Method) {
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
          if (method == Dummy.Method || method.Visibility != abstractMethod.Visibility)
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
          if (method == Dummy.Method || !method.IsVirtual || method.Visibility != abstractMethod.Visibility)
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

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get {
        foreach (IMethodImplementation implementation in this.ImplementationMap.Values) {
          if (implementation.ImplementingMethod.ResolvedMethod.Visibility != TypeMemberVisibility.Public)
            yield return implementation;
        }
      }
    }

    protected Flags flags;

    public virtual IEnumerable<GenericTypeParameter> GenericParameters {
      get { return this.genericParameters.AsReadOnly(); }
    }
    readonly List<GenericTypeParameter> genericParameters = new List<GenericTypeParameter>();

    public virtual ushort GenericParameterCount {
      get {
        int result = this.genericParameters.Count;
        //^ assume !this.IsGeneric ==> result == 0;
        //^ assume this.IsGeneric ==> result > 0;
        return (ushort)result;
      }
    }

    protected override ITypeDefinitionMember GetAggregatedMember(IAggregatableTypeDeclarationMember member) {
      return member.AggregatedMember;
    }

    MultiHashtable<IMethodImplementation> ImplementationMap {
      get {
        if (this.implementationMap == null)
          this.implementationMap = this.ComputeImplementationMap();
        return this.implementationMap;
      }
    }
    MultiHashtable<IMethodImplementation>/*?*/ implementationMap;

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
      MethodDeclaration ctorDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.SpecialName,
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
      BlockStatement emptyBody = new BlockStatement(new List<Statement>(0), SourceDummy.SourceLocation);
      MethodDeclaration ctorDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.SpecialName,
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
      emptyBody = new BlockStatement(new List<Statement>(0), SourceDummy.SourceLocation);
      MethodDeclaration invokeDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.Virtual,
          TypeMemberVisibility.Public, delegateSignature.Type, null, invoke, null, invokeParameters, null, emptyBody, SourceDummy.SourceLocation);
      foreach (ParameterDeclaration delPar in delegateSignature.Parameters)
        invokeParameters.Add(delPar.MakeShallowCopyFor(invokeDeclaration, delegateDeclaration.OuterDummyBlock));
      invokeDeclaration.SetContainingTypeDeclaration(delegateDeclaration, true);

      NameDeclaration beginInvoke = new NameDeclaration(nametable.GetNameFor("BeginInvoke"), SourceDummy.SourceLocation);
      List<ParameterDeclaration> beginInvokeParameters = new List<ParameterDeclaration>();
      TypeExpression iasyncResultType = TypeExpression.For(this.PlatformType.SystemIAsyncResult);
      emptyBody = new BlockStatement(new List<Statement>(0), SourceDummy.SourceLocation);
      MethodDeclaration beginInvokeDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.Virtual,
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
      emptyBody = new BlockStatement(new List<Statement>(0), SourceDummy.SourceLocation);
      MethodDeclaration endInvokeDeclaration =
        new MethodDeclaration(null, MethodDeclaration.Flags.Virtual,
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
        new MethodDeclaration(null, MethodDeclaration.Flags.SpecialName|MethodDeclaration.Flags.Static,
          TypeMemberVisibility.Public, voidType, null, cctor, null, cctorParameters, null, body, SourceDummy.SourceLocation);
      cctorDeclaration.SetContainingTypeDeclaration(tDecl, true);
      this.AddMemberToCache(new MethodDefinition(cctorDeclaration));
    }

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

    public virtual bool IsAbstract {
      get {
        return (this.flags & (Flags.Abstract | Flags.Interface)) != 0;
      }
    }

    public bool IsClass {
      get {
        return (this.flags & Flags.Class) != 0;
      }
    }

    public bool IsDelegate {
      get {
        return (this.flags & Flags.Delegate) != 0;
      }
    }

    public bool IsEnum {
      get {
        return (this.flags & Flags.Enum) != 0;
      }
    }

    public bool IsGeneric {
      get
        //^ ensures result == this.genericParameters.Count > 0;
      {
        return this.genericParameters.Count > 0;
      }
    }

    public virtual bool IsInterface {
      get {
        return (this.flags & Flags.Interface) != 0;
      }
    }

    public virtual bool IsReferenceType {
      get { return (this.flags & (Flags.Enum | Flags.ValueType | Flags.Static)) == 0; }
    }

    public virtual bool IsSealed {
      get {
        return (this.flags & Flags.Sealed) != 0;
      }
    }

    public virtual bool IsStatic {
      get {
        return (this.flags & Flags.Static) != 0;
      }
    }

    public virtual bool IsValueType {
      get {
        return (this.flags & Flags.ValueType) != 0;
      }
    }

    public virtual bool IsStruct {
      get {
        return (this.flags & Flags.Struct) != 0;
      }
    }

    public abstract IPlatformType PlatformType { get; }

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
    public override string ToString() {
      return TypeHelper.GetTypeName(this);
    }

    public virtual PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
      //TODO: need to return something else when compiling mscorlib and this is a special type
      //get a map from the compilation's target platform
    }

    public IEnumerable<TypeDeclaration> TypeDeclarations {
      get { return this.typeDeclarations.AsReadOnly(); }
    }
    readonly List<TypeDeclaration> typeDeclarations = new List<TypeDeclaration>();

    public ITypeReference UnderlyingType {
      get
        //^^ requires this.IsEnum;
      {
        //^ assume this.underlyingType != null; //Follows from the precondition and the implementation of UpdateFlags
        return this.underlyingType;
      }
    }
    ITypeReference/*?*/ underlyingType;

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

    public bool IsSpecialName {
      get { return false; } //TODO: when could it be special?
    }

    public bool IsComObject {
      get { return false; } //TODO: when could this be true?
    }

    public bool IsSerializable {
      get { return false; } //TODO: get this from a custom attribute
    }

    public bool IsBeforeFieldInit {
      get { return !this.IsDelegate; } //TODO: when could this be false?
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; } //TODO: get this from a custom attribute
    }

    public bool IsRuntimeSpecial {
      get { return false; } //TODO: when could this be true?
    }

    public bool HasDeclarativeSecurity {
      get { return false; } //TODO: get this from the attributes
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IEventDefinition>(this.Members); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IFieldDefinition>(this.Members); }
    }

    /// <summary>
    /// Needed because base.Members cannot be referred to directly from inside an iterator.
    /// </summary>
    private IEnumerable<ITypeDefinitionMember> GetBaseMembers() {
      return base.Members;
    }

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

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IMethodDefinition>(this.Members); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, INestedTypeDefinition>(this.Members); }
    }

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

    IEnumerable<ILocation> IReference.Locations {
      get {
        foreach (TypeDeclaration declaration in this.TypeDeclarations)
          yield return declaration.SourceLocation;
      }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool IsModified {
      get { return false; }
    }

    public ITypeDefinition ResolvedType {
      get { return this; }
    }

    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

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
