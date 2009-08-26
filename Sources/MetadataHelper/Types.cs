//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

//^ using Microsoft.Contracts;
#pragma warning disable 1591

namespace Microsoft.Cci {

  public abstract class ArrayType : SystemDefinedStructuralType, IArrayType {

    internal ArrayType(ITypeReference elementType, IInternFactory internFactory)
      : base(internFactory) {
      this.elementType = elementType;
    }

    public override IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetSingletonEnumerable<ITypeReference>(this.PlatformType.SystemArray); }
    }

    //^ [Pure]
    public override bool Contains(ITypeDefinitionMember member) {
      foreach (ITypeDefinitionMember mem in this.Members)
        if (mem == member) return true;
      return false;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference ElementType {
      get { return this.elementType; }
    }
    readonly ITypeReference elementType;

    //  Issue: Array type does not have to give these as they are indirectly inherited from System.Array?!?
    protected virtual IEnumerable<ITypeReference> GetInterfaceList() {
      List<ITypeReference> interfaces = new List<ITypeReference>(4);
      interfaces.Add(this.PlatformType.SystemICloneable);
      interfaces.Add(this.PlatformType.SystemCollectionsIEnumerable);
      interfaces.Add(this.PlatformType.SystemCollectionsICollection);
      interfaces.Add(this.PlatformType.SystemCollectionsIList);
      return interfaces.AsReadOnly();
    }

    public override IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      foreach (ITypeDefinitionMember member in this.Members) {
        if (name.UniqueKey != member.Name.UniqueKey || (ignoreCase && name.UniqueKeyIgnoringCase == member.Name.UniqueKeyIgnoringCase)) {
          if (predicate(member)) yield return member;
        }
      }
    }

    public override IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      foreach (ITypeDefinitionMember member in this.Members) {
        if (predicate(member)) yield return member;
      }
    }

    public override IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      foreach (ITypeDefinitionMember member in this.Members) {
        if (name.UniqueKey != member.Name.UniqueKey || (ignoreCase && name.UniqueKeyIgnoringCase == member.Name.UniqueKeyIgnoringCase)) {
          yield return member;
        }
      }
    }

    public override IEnumerable<ITypeReference> Interfaces {
      get {
        if (this.interfaces == null) {
          lock (GlobalLock.LockingObject) {
            if (this.interfaces == null) {
              this.interfaces = this.GetInterfaceList();
            }
          }
        }
        return this.interfaces;
      }
    }
    IEnumerable<ITypeReference>/*?*/ interfaces;

    public override bool IsReferenceType {
      get { return true; }
    }

    public virtual bool IsVector {
      get { return this.Rank == 1; }
    }

    public virtual IEnumerable<int> LowerBounds {
      get { return IteratorHelper.GetEmptyEnumerable<int>(); }
    }

    public override IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public override IPlatformType PlatformType {
      get { return this.ElementType.PlatformType; }
    }

    public virtual uint Rank {
      get { return 1; }
    }

    public virtual IEnumerable<ulong> Sizes {
      get { return IteratorHelper.GetEmptyEnumerable<ulong>(); }
    }

    //^ [Confined]
    public override string ToString() {
      return TypeHelper.GetTypeName(this, NameFormattingOptions.None);
    }

    #region ITypeDefinition Members

    IEnumerable<ITypeReference> ITypeDefinition.BaseClasses {
      get {
        return this.BaseClasses;
      }
    }

    IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    ushort ITypeDefinition.GenericParameterCount {
      get {
        //^ assume this.IsGeneric == ((ITypeDefinition)this).IsGeneric;
        return 0;
      }
    }

    #endregion

    #region IContainer<ITypeDefinitionMember> Members

    IEnumerable<ITypeDefinitionMember> IContainer<ITypeDefinitionMember>.Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    #endregion

    #region IDefinition Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    //^ [Pure]
    IEnumerable<ITypeDefinitionMember> IScope<ITypeDefinitionMember>.GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    IEnumerable<ITypeDefinitionMember> IScope<ITypeDefinitionMember>.GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    IEnumerable<ITypeDefinitionMember> IScope<ITypeDefinitionMember>.GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    IEnumerable<ITypeDefinitionMember> IScope<ITypeDefinitionMember>.Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    #endregion
  }

  public class CustomModifier : ICustomModifier {

    public CustomModifier(bool isOptional, ITypeReference modifier) {
      this.isOptional = isOptional;
      this.modifier = modifier;
    }

    public bool IsOptional {
      get { return this.isOptional; }
    }
    readonly bool isOptional;

    public ITypeReference Modifier {
      get { return this.modifier; }
    }
    readonly ITypeReference modifier;

  }

  public class FunctionPointerType : SystemDefinedStructuralType, IFunctionPointer {

    public FunctionPointerType(ISignature signature, IInternFactory internFactory) 
      : base (internFactory) {
      this.callingConvention = signature.CallingConvention;
      if (signature.ReturnValueIsModified)
        this.returnValueCustomModifiers = signature.ReturnValueCustomModifiers;
      this.returnValueIsByRef = signature.ReturnValueIsByRef;
      this.type = signature.Type;
      this.parameters = signature.Parameters;
      this.extraArgumentTypes = IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>();
    }

    public FunctionPointerType(CallingConvention callingConvention, bool returnValueIsByRef, ITypeReference type,
      IEnumerable<ICustomModifier>/*?*/ returnValueCustomModifiers, IEnumerable<IParameterTypeInformation> parameters, IEnumerable<IParameterTypeInformation>/*?*/ extraArgumentTypes, 
      IInternFactory internFactory) 
      : base(internFactory) {
      this.callingConvention = callingConvention;
      this.returnValueCustomModifiers = returnValueCustomModifiers;
      this.returnValueIsByRef = returnValueIsByRef;
      this.type = type;
      this.parameters = parameters;
      if (extraArgumentTypes == null)
        this.extraArgumentTypes = IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>();
      else
        this.extraArgumentTypes = extraArgumentTypes;
    }

    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
    }
    readonly CallingConvention callingConvention;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IEnumerable<IParameterTypeInformation> ExtraArgumentTypes {
      get { return this.extraArgumentTypes; }
    }
    readonly IEnumerable<IParameterTypeInformation> extraArgumentTypes;

    public override IPlatformType PlatformType {
      get { return this.Type.PlatformType; }
    }

    public IEnumerable<IParameterTypeInformation> Parameters {
      get { return this.parameters; }
    }
    readonly IEnumerable<IParameterTypeInformation> parameters;

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get
        //^^ requires this.ReturnValueIsModified;
      {
        //^ assume this.returnValueCustomModifiers != null;
        return this.returnValueCustomModifiers;
      }
    }
    readonly IEnumerable<ICustomModifier>/*?*/ returnValueCustomModifiers;

    public bool ReturnValueIsByRef {
      get { return this.returnValueIsByRef; }
    }
    readonly bool returnValueIsByRef;

    public bool ReturnValueIsModified {
      get { return this.returnValueCustomModifiers != null; }
    }

    public ITypeReference Type {
      get { return this.type; }
    }
    readonly ITypeReference type;

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Pointer; }
    }

    #region ISignature Members

    ITypeReference ISignature.Type {
      get { return this.Type; }
    }

    #endregion

  }

  public class GenericTypeInstance : Scope<ITypeDefinitionMember>, IGenericTypeInstance {

    public static GenericTypeInstance GetGenericTypeInstance(ITypeReference genericType, IEnumerable<ITypeReference> genericArguments, IInternFactory internFactory)
      //^ requires genericType.ResolvedType.IsGeneric;
      //^ ensures !result.IsGeneric;
    {
      return new GenericTypeInstance(genericType, genericArguments, internFactory);
    }

    private GenericTypeInstance(ITypeReference genericType, IEnumerable<ITypeReference> genericArguments, IInternFactory internFactory)
      //^ requires genericType.ResolvedType.IsGeneric;
    {
      this.genericType = genericType;
      this.genericArguments = genericArguments;
      this.internFactory = internFactory;
    }

    public ushort Alignment {
      get { return this.GenericType.ResolvedType.Alignment; }
    }

    public virtual IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get {
        foreach (ITypeReference baseClassRef in this.GenericType.ResolvedType.BaseClasses) {
          ITypeReference specializedBaseClass = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(baseClassRef, this, this.InternFactory);
          yield return specializedBaseClass;
        }
      }
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IEventDefinition>(this.Members); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IFieldDefinition>(this.Members); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.genericArguments; }
    }
    readonly IEnumerable<ITypeReference> genericArguments;

    public ITypeReference GenericType {
      get { return this.genericType; }
    }
    readonly ITypeReference genericType; //^ invariant genericType.ResolvedType.IsGeneric;

    protected override void InitializeIfNecessary() {
      if (this.initialized) return;
      lock (GlobalLock.LockingObject) {
        if (this.initialized) return;
        foreach (ITypeDefinitionMember unspecializedMember in this.GenericType.ResolvedType.Members) {
          //^ assume unspecializedMember is IEventDefinition || unspecializedMember is IFieldDefinition || unspecializedMember is IMethodDefinition ||
          //^   unspecializedMember is IPropertyDefinition || unspecializedMember is INestedTypeDefinition; //follows from informal post condition on Members property.
          this.AddMemberToCache(this.SpecializeMember(unspecializedMember, this.InternFactory));
        }
        this.initialized = true;
      }
    }
    private bool initialized;

    public IGenericTypeInstanceReference InstanceType {
      get { return this; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get {
        foreach (ITypeReference ifaceRef in this.GenericType.ResolvedType.Interfaces) {
          ITypeReference specializedIface = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(ifaceRef, this, this.InternFactory);
          yield return specializedIface;
        }
      }
    }

    public bool IsAbstract {
      get { return this.GenericType.ResolvedType.IsAbstract; }
    }

    public bool IsClass {
      get { return this.GenericType.ResolvedType.IsClass; }
    }

    public bool IsDelegate {
      get { return this.GenericType.ResolvedType.IsDelegate; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
    }

    public bool IsInterface {
      get { return this.GenericType.ResolvedType.IsInterface; }
    }

    public bool IsReferenceType {
      get { return this.GenericType.ResolvedType.IsReferenceType; }
    }

    public bool IsSealed {
      get { return this.GenericType.ResolvedType.IsSealed; }
    }

    public bool IsStatic {
      get { return this.GenericType.ResolvedType.IsStatic; }
    }

    public bool IsValueType {
      get { return this.GenericType.ResolvedType.IsValueType; }
    }

    public bool IsStruct {
      get { return this.GenericType.ResolvedType.IsStruct; }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IMethodDefinition>(this.Members); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, INestedTypeDefinition>(this.Members); }
    }

    public IPlatformType PlatformType {
      get { return this.GenericType.ResolvedType.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { 
        //TODO: specialize and cache the private helper members of the generic type template.
        return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); 
      }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IPropertyDefinition>(this.Members); }
    }

    /// <summary>
    /// Specialize the type arguments of genericTypeIntance and (if necessary) return a new instance of genericTypeInstance.GenericType using
    /// the specialized type arguments. Specialization means replacing any references to the type parameters of containingMethodInstance.GenericMethod with the
    /// corresponding values of containingMethodInstance.GenericArguments.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IGenericTypeInstanceReference genericTypeInstance, IGenericMethodInstanceReference containingMethodInstance, IInternFactory internFactory) {
      List<ITypeReference>/*?*/ specializedArguments = null;
      int i = 0;
      foreach (ITypeReference argType in genericTypeInstance.GenericArguments) {
        ITypeReference specializedArgType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(argType, containingMethodInstance, internFactory);
        if (argType != specializedArgType) {
          if (specializedArguments == null) specializedArguments = new List<ITypeReference>(genericTypeInstance.GenericArguments);
          //^ assume 0 <= i && i < specializedArguments.Count; //Since genericTypeInstance.GenericArguments is immutable
          specializedArguments[i] = specializedArgType;
        }
        i++;
      }
      if (specializedArguments == null) return genericTypeInstance;
      return GetGenericTypeInstance(genericTypeInstance.GenericType, specializedArguments, internFactory);
    }

    /// <summary>
    /// Specialize the type arguments of genericTypeIntance and (if necessary) return a new instance of containingTypeInstance.GenericType using
    /// the specialized type arguments. Specialization means replacing any references to the type parameters of containingTypeInstance.GenericType with the
    /// corresponding values of containingTypeInstance.GenericArguments.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IGenericTypeInstanceReference genericTypeInstance, IGenericTypeInstanceReference containingTypeInstance, IInternFactory internFactory) {
      List<ITypeReference>/*?*/ specializedArguments = null;
      int i = 0;
      foreach (ITypeReference argType in genericTypeInstance.GenericArguments) {
        ITypeReference specializedArgType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(argType, containingTypeInstance, internFactory);
        if (argType != specializedArgType) {
          if (specializedArguments == null) specializedArguments = new List<ITypeReference>(genericTypeInstance.GenericArguments);
          //^ assume 0 <= i && i < specializedArguments.Count;  //Since genericTypeInstance.GenericArguments is immutable
          specializedArguments[i] = specializedArgType;
        }
        i++;
      }
      if (specializedArguments == null) return genericTypeInstance;
      return GetGenericTypeInstance(genericTypeInstance.GenericType, specializedArguments, internFactory);
    }

    public ITypeDefinitionMember SpecializeMember(ITypeDefinitionMember unspecializedMember, IInternFactory internFactory)
      //^ requires unspecializedMember is IEventDefinition || unspecializedMember is IFieldDefinition || unspecializedMember is IMethodDefinition ||
      //^   unspecializedMember is IPropertyDefinition || unspecializedMember is INestedTypeDefinition;
      //^ ensures unspecializedMember is IEventDefinition ==> result is IEventDefinition;
      //^ ensures unspecializedMember is IFieldDefinition ==> result is IFieldDefinition;
      //^ ensures unspecializedMember is IMethodDefinition ==> result is IMethodDefinition;
      //^ ensures unspecializedMember is IPropertyDefinition ==> result is IPropertyDefinition;
      //^ ensures unspecializedMember is INestedTypeDefinition ==> result is INestedTypeDefinition;
    {
      IEventDefinition/*?*/ eventDef = unspecializedMember as IEventDefinition;
      if (eventDef != null) return new SpecializedEventDefinition(eventDef, this);
      IFieldDefinition/*?*/ fieldDef = unspecializedMember as IFieldDefinition;
      if (fieldDef != null) return new SpecializedFieldDefinition(fieldDef, this);
      IMethodDefinition/*?*/ methodDef = unspecializedMember as IMethodDefinition;
      if (methodDef != null) return new SpecializedMethodDefinition(methodDef, this);
      IPropertyDefinition/*?*/ propertyDef = unspecializedMember as IPropertyDefinition;
      if (propertyDef != null) return new SpecializedPropertyDefinition(propertyDef, this);
      //^ assert unspecializedMember is INestedTypeDefinition;
      INestedTypeDefinition nestedTypeDef = (INestedTypeDefinition)unspecializedMember;
      return new SpecializedNestedTypeDefinition(nestedTypeDef, this, internFactory);
    }

    public uint SizeOf {
      get { return this.GenericType.ResolvedType.SizeOf; }
    }

    //^ [Confined]
    public override string ToString() {
      StringBuilder sb = new StringBuilder();
      sb.Append(this.GenericType.ResolvedType.ToString());
      sb.Append('<');
      foreach (ITypeReference arg in this.GenericArguments) {
        if (sb[sb.Length - 1] != '<') sb.Append(',');
        sb.Append(arg.ResolvedType.ToString());
      }
      sb.Append('>');
      return sb.ToString();
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    public ITypeReference UnderlyingType {
      get { return this; }
    }

    public LayoutKind Layout {
      get
        //^ ensures result == this.GenericType.ResolvedType.Layout;
      { 
        return this.GenericType.ResolvedType.Layout; 
      }
    }

    public bool IsSpecialName {
      get { return this.GenericType.ResolvedType.IsSpecialName; }
    }

    public bool IsComObject {
      get { return this.GenericType.ResolvedType.IsComObject; }
    }

    public bool IsSerializable {
      get { return this.GenericType.ResolvedType.IsSerializable; }
    }

    public bool IsBeforeFieldInit {
      get { return this.GenericType.ResolvedType.IsBeforeFieldInit; }
    }

    public StringFormatKind StringFormat {
      get { return this.GenericType.ResolvedType.StringFormat; }
    }

    public bool IsRuntimeSpecial {
      get { return this.GenericType.ResolvedType.IsRuntimeSpecial; }
    }

    public bool HasDeclarativeSecurity {
      get { return this.GenericType.ResolvedType.HasDeclarativeSecurity; }
    }

    #region ITypeDefinition Members

    IEnumerable<IGenericTypeParameter> ITypeDefinition.GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    ushort ITypeDefinition.GenericParameterCount {
      get {
        return 0;
      }
    }

    IEnumerable<ITypeDefinitionMember> ITypeDefinition.Members {
      get {
        return this.Members;
      }
    }

    IEnumerable<ISecurityAttribute> ITypeDefinition.SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
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

  internal static class GenericParameter {
    /// <summary>
    /// If the given generic parameter is a generic parameter of the generic method of which the given method is an instance, then return the corresponding type argument that
    /// was used to create the method instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IGenericMethodParameterReference genericMethodParameter, IGenericMethodInstanceReference containingMethodInstance) {
      if (genericMethodParameter.DefiningMethod.InternedKey == containingMethodInstance.GenericMethod.InternedKey) {
        ushort i = 0;
        ushort n = genericMethodParameter.Index;
        IEnumerator<ITypeReference> genericArguments = containingMethodInstance.GenericArguments.GetEnumerator();
        while (genericArguments.MoveNext()) {
          if (i++ == n) return genericArguments.Current;
        }
      }
      return genericMethodParameter;
    }

    /// <summary>
    /// If the given generic parameter is a generic parameter of the generic type of which the given type is an instance, then return the corresponding type argument that
    /// was used to create the type instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IGenericTypeParameterReference genericTypeParameter, IGenericTypeInstanceReference containingTypeInstance) {
      if (genericTypeParameter.DefiningType.InternedKey == containingTypeInstance.GenericType.InternedKey) {
        ushort i = 0;
        ushort n = genericTypeParameter.Index;
        IEnumerator<ITypeReference> genericArguments = containingTypeInstance.GenericArguments.GetEnumerator();
        while (genericArguments.MoveNext()) {
          if (i++ == n) return genericArguments.Current;
        }
      }
      return genericTypeParameter;
    }

  }

  public class ManagedPointerType : SystemDefinedStructuralType, IManagedPointerType {

    private ManagedPointerType(ITypeReference targetType, IInternFactory internFactory)
      : base(internFactory) {
      this.targetType = targetType;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public static ManagedPointerType GetManagedPointerType(ITypeReference targetType, IInternFactory internFactory) {
      ManagedPointerType result = new ManagedPointerType(targetType, internFactory);
      return result;
    }

    public override IPlatformType PlatformType {
      get { return this.TargetType.ResolvedType.PlatformType; }
    }

    /// <summary>
    /// If the given managed pointer has a target type that involves a type parameter from the generic method from which the given method was instantiated,
    /// then return a new pointer using a target type that has been specialized with the type arguments of the given generic method instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IManagedPointerTypeReference pointer, IGenericMethodInstanceReference containingMethodInstance, IInternFactory internFactory) {
      ITypeReference targetType = pointer.TargetType;
      ITypeReference specializedtargetType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(targetType, containingMethodInstance, internFactory);
      if (targetType == specializedtargetType) return pointer;
      return GetManagedPointerType(specializedtargetType, internFactory);
    }

    /// <summary>
    /// If the given managed pointer has a target type that involves a type parameter from the generic type from which the given type was instantiated,
    /// then return a new pointer using a target type that has been specialized with the type arguments of the given generic type instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IManagedPointerTypeReference pointer, IGenericTypeInstanceReference containingTypeInstance, IInternFactory internFactory) {
      ITypeReference targetType = pointer.TargetType;
      ITypeReference specializedtargetType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(targetType, containingTypeInstance, internFactory);
      if (targetType == specializedtargetType) return pointer;
      return GetManagedPointerType(specializedtargetType, internFactory);
    }

    //^ [Confined]
    public override string ToString() {
      return this.TargetType.ToString() + "&";
    }

    public ITypeReference TargetType {
      get { return this.targetType; }
    }
    readonly ITypeReference targetType;

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Reference; }
    }

  }

  public class Matrix : ArrayType {

    private Matrix(ITypeReference elementType, uint rank, IEnumerable<int>/*?*/ lowerBounds, IEnumerable<ulong>/*?*/ sizes, IInternFactory internFactory)
      : base(elementType, internFactory) {
      this.rank = rank;
      this.lowerBounds = lowerBounds;
      this.sizes = sizes;
    }

    public static Matrix GetMatrix(ITypeReference elementType, uint rank, IInternFactory internFactory) {
      return new Matrix(elementType, rank, null, null, internFactory);
    }

    public static Matrix GetMatrix(ITypeReference elementType, uint rank, IEnumerable<int>/*?*/ lowerBounds, IEnumerable<ulong>/*?*/ sizes, IInternFactory internFactory) {
      return new Matrix(elementType, rank, lowerBounds, sizes, internFactory);
    }

    public override bool IsVector {
      get { return false; }
    }

    public override IEnumerable<int> LowerBounds {
      get {
        if (this.lowerBounds == null) return base.LowerBounds;
        return this.lowerBounds;
      }
    }
    IEnumerable<int>/*?*/ lowerBounds;

    public override uint Rank {
      get { return this.rank; }
    }
    readonly uint rank;

    public override IEnumerable<ulong> Sizes {
      get {
        if (this.sizes == null) return base.Sizes;
        return this.sizes;
      }
    }
    IEnumerable<ulong>/*?*/ sizes;

    /// <summary>
    /// If the given matrix has an element type that involves a type parameter from the generic method from which the given method was instantiated,
    /// then return a new matrix using an element type that has been specialized with the type arguments of the given generic method instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IArrayTypeReference array, IGenericMethodInstanceReference containingMethodInstance, IInternFactory internFactory)
      //^ requires !array.IsVector;
    {
      ITypeReference elementType = array.ElementType;
      ITypeReference specializedElementType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(elementType, containingMethodInstance, internFactory);
      if (elementType == specializedElementType) return array;
      return GetMatrix(specializedElementType, array.Rank, array.LowerBounds, array.Sizes, internFactory);
    }

    /// <summary>
    /// If the given matrix has an element type that involves a type parameter from the generic type from which the given type was instantiated,
    /// then return a new matrix using an element type that has been specialized with the type arguments of the given generic type instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IArrayTypeReference array, IGenericTypeInstanceReference containingTypeInstance, IInternFactory internFactory)
      //^ requires !array.IsVector;
    {
      ITypeReference elementType = array.ElementType;
      ITypeReference specializedElementType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(elementType, containingTypeInstance, internFactory);
      if (elementType == specializedElementType) return array;
      return GetMatrix(specializedElementType, array.Rank, array.LowerBounds, array.Sizes, internFactory);
    }

  }

  public class PointerType : SystemDefinedStructuralType, IPointerType {

    internal PointerType(ITypeReference targetType, IInternFactory internFactory)
      : base(internFactory) {
      this.targetType = targetType;
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public static PointerType GetPointerType(ITypeReference targetType, IInternFactory internFactory) {
      return new PointerType(targetType, internFactory);
    }

    public override IPlatformType PlatformType {
      get { return this.TargetType.ResolvedType.PlatformType; }
    }

    /// <summary>
    /// If the given pointer has a target type that involves a type parameter from the generic method from which the given method was instantiated,
    /// then return a new pointer using a target type that has been specialized with the type arguments of the given generic method instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IPointerTypeReference pointer, IGenericMethodInstanceReference containingMethodInstance, IInternFactory internFactory) {
      ITypeReference targetType = pointer.TargetType;
      ITypeReference specializedtargetType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(targetType, containingMethodInstance, internFactory);
      if (targetType == specializedtargetType) return pointer;
      return GetPointerType(specializedtargetType, internFactory);
    }

    /// <summary>
    /// If the given pointer has a target type that involves a type parameter from the generic type from which the given type was instantiated,
    /// then return a new pointer using a target type that has been specialized with the type arguments of the given generic type instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IPointerTypeReference pointer, IGenericTypeInstanceReference containingTypeInstance, IInternFactory internFactory) {
      ITypeReference targetType = pointer.TargetType;
      ITypeReference specializedtargetType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(targetType, containingTypeInstance, internFactory);
      if (targetType == specializedtargetType) return pointer;
      return GetPointerType(specializedtargetType, internFactory);
    }

    public ITypeReference TargetType {
      get { return this.targetType; }
    }
    readonly ITypeReference targetType;

    //^ [Confined]
    public override string ToString() {
      return this.TargetType.ResolvedType.ToString() + "*";
    }

    public override PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Pointer; }
    }

  }

  public class ModifiedTypeReference : IModifiedTypeReference {

    public ModifiedTypeReference(IMetadataHost host, ITypeReference unmodifiedType, IEnumerable<ICustomModifier> customModifiers) {
      this.host = host;
      this.unmodifiedType = unmodifiedType;
      this.customModifiers = customModifiers;
    }

    IMetadataHost host;

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.customModifiers; }
    }
    IEnumerable<ICustomModifier> customModifiers;

    public ITypeReference UnmodifiedType {
      get { return this.unmodifiedType; }
    }
    readonly ITypeReference unmodifiedType;

    #region ITypeReference Members

    public IAliasForType AliasForType {
      get { return null; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.host.InternFactory.GetTypeReferenceInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsAlias {
      get { return false; }
    }

    public bool IsEnum {
      get { return this.UnmodifiedType.IsEnum; }
    }

    public bool IsValueType {
      get { return this.UnmodifiedType.IsValueType; }
    }

    public IPlatformType PlatformType {
      get { return this.host.PlatformType; }
    }

    public ITypeDefinition ResolvedType {
      get { return this.UnmodifiedType.ResolvedType; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return this.UnmodifiedType.TypeCode; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion
  }

  /// <summary>
  /// A collection of named members, with routines to search and maintain the collection. The search routines have sublinear complexity, typically close to constant time.
  /// </summary>
  /// <typeparam name="MemberType">The type of the members of this scope.</typeparam>
  public abstract class Scope<MemberType> : IScope<MemberType>
    where MemberType : class, INamedEntity {

    private Dictionary<int, List<MemberType>> caseSensitiveMemberNameToMemberListMap = new Dictionary<int, List<MemberType>>();
    private Dictionary<int, List<MemberType>> caseInsensitiveMemberNameToMemberListMap = new Dictionary<int, List<MemberType>>();
    //TODO: replace BCL Dictionary with a private implementation that is thread safe and does not need a new list to be allocated for each name

    /// <summary>
    /// Adds a member to the scope. Does nothing if the member is already in the scope.
    /// </summary>
    /// <param name="member">The member to add to the scope.</param>
    protected void AddMemberToCache(MemberType/*!*/ member)
      //^ ensures this.Contains(member);
    {
      List<MemberType>/*?*/ members;
      if (this.caseInsensitiveMemberNameToMemberListMap.TryGetValue(member.Name.UniqueKeyIgnoringCase, out members)) {
        //^ assume members != null; //Follows from the way Dictionary is instantiated, but the verifier is ignorant of this.
        if (!members.Contains(member)) members.Add(member);
      } else {
        this.caseInsensitiveMemberNameToMemberListMap[member.Name.UniqueKeyIgnoringCase] = members = new List<MemberType>();
        members.Add(member);
      }
      if (this.caseSensitiveMemberNameToMemberListMap.TryGetValue(member.Name.UniqueKey, out members)) {
        //^ assume members != null; //Follows from the way Dictionary is instantiated, but the verifier is ignorant of this.
        if (!members.Contains(member)) members.Add(member);
      } else {
        this.caseSensitiveMemberNameToMemberListMap[member.Name.UniqueKey] = members = new List<MemberType>();
        members.Add(member);
      }
      //^ assume this.Contains(member);
    }

    /// <summary>
    /// Return true if the given member instance is a member of this scope.
    /// </summary>
    //^ [Pure]
    public bool Contains(MemberType/*!*/ member)
      // ^ ensures result == exists{MemberType mem in this.Members; mem == member};
    {
      foreach (MemberType mem in this.GetMembersNamed(member.Name, false))
        if (mem == member) return true;
      return false;
    }

    /// <summary>
    /// Returns the list of members with the given name that also satisfy the given predicate.
    /// </summary>
    //^ [Pure]
    public IEnumerable<MemberType> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<MemberType, bool> predicate) {
      foreach (MemberType member in this.GetMembersNamed(name, ignoreCase))
        if (predicate(member)) yield return member;
    }

    /// <summary>
    /// Returns the list of members that satisfy the given predicate.
    /// </summary>
    //^ [Pure]
    public IEnumerable<MemberType> GetMatchingMembers(Function<MemberType, bool> predicate)
      // ^ ensures forall{MemberType member in result; member.Name.UniqueKey == name.UniqueKey && predicate(member) && this.Contains(member)};
      // ^ ensures forall{MemberType member in this.Members; member.Name.UniqueKey == name.UniqueKey && predicate(member) ==> 
      // ^                                                            exists{INamespaceMember mem in result; mem == member}};
    {
      foreach (MemberType member in this.Members)
        if (predicate(member)) yield return member;
    }

    /// <summary>
    /// Returns the list of members with the given name.
    /// </summary>
    /// <param name="name">The name of the members to retrieve.</param>
    /// <param name="ignoreCase">True if the case of the name must be ignored when retrieving the members.</param>
    //^ [Pure]
    public IEnumerable<MemberType> GetMembersNamed(IName name, bool ignoreCase)
      // ^ ensures forall{MemberType member in result; member.Name.UniqueKey == name.UniqueKey && this.Contains(member)};
      // ^ ensures forall{MemberType member in this.Members; member.Name.UniqueKey == name.UniqueKey ==> 
      // ^                                                            exists{INamespaceMember mem in result; mem == member}};
    {
      this.InitializeIfNecessary();
      Dictionary<int, List<MemberType>> nameToMemberListMap = ignoreCase ? this.caseInsensitiveMemberNameToMemberListMap : this.caseSensitiveMemberNameToMemberListMap;
      int key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      List<MemberType>/*?*/ members;
      if (!nameToMemberListMap.TryGetValue(key, out members)) return emptyList;
      //^ assume members != null; //Follows from the way Dictionary is instantiated, but the verifier is ignorant of this.
      return members.AsReadOnly();
    }
    private static readonly IEnumerable<MemberType> emptyList = (new List<MemberType>(0)).AsReadOnly();

    /// <summary>
    /// Provides a derived class with an opportunity to lazily initialize the scope's data structures via calls to AddMemberToCache.
    /// </summary>
    protected virtual void InitializeIfNecessary() { }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    public virtual IEnumerable<MemberType> Members {
      get {
        this.InitializeIfNecessary();
        foreach (IEnumerable<MemberType> namedMemberList in this.caseSensitiveMemberNameToMemberListMap.Values)
          foreach (MemberType member in namedMemberList)
            yield return member;
      }
    }

  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="ParameterType"></typeparam>
  public abstract class SpecializedGenericParameter<ParameterType> : IGenericParameter
    where ParameterType : IGenericParameter {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedParameter"></param>
    /// <param name="internFactory"></param>
    protected SpecializedGenericParameter(ParameterType/*!*/ unspecializedParameter, IInternFactory internFactory) {
      this.unspecializedParameter = unspecializedParameter;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// Zero or more classes from which this type is derived.
    /// For CLR types this collection is empty for interfaces and System.Object and populated with exactly one base type for all other types.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    /// <value></value>
    public abstract IEnumerable<ITypeReference> Constraints { get; }

    /// <summary>
    /// Zero or more parameters that can be used as type annotations.
    /// </summary>
    /// <value></value>
    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    /// <summary>
    /// Zero or more interfaces implemented by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    /// <summary>
    /// An instance of this generic type that has been obtained by using the generic parameters as the arguments.
    /// Use this instance to look up members
    /// </summary>
    /// <value></value>
    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    /// <summary>
    /// A way to get to platform types such as System.Object.
    /// </summary>
    /// <value></value>
    public IPlatformType PlatformType {
      get { return this.UnspecializedParameter.PlatformType; }
    }

    public ParameterType/*!*/ UnspecializedParameter {
      get {
        return this.unspecializedParameter;
      }
    }
    readonly ParameterType/*!*/ unspecializedParameter;

    /// <summary>
    /// Zero or more implementation overrides provided by the class.
    /// </summary>
    /// <value></value>
    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    #region IGenericParameter Members

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be reference types.
    /// </summary>
    /// <value></value>
    public bool MustBeReferenceType {
      get { return this.UnspecializedParameter.MustBeReferenceType; }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types.
    /// </summary>
    /// <value></value>
    public bool MustBeValueType {
      get { return this.UnspecializedParameter.MustBeValueType; }
    }

    /// <summary>
    /// True if all type arguments matching this parameter are constrained to be value types or concrete classes with visible default constructors.
    /// </summary>
    /// <value></value>
    public bool MustHaveDefaultConstructor {
      get { return this.UnspecializedParameter.MustHaveDefaultConstructor; }
    }

    /// <summary>
    /// Indicates if the generic type or method with this type parameter is co-, contra-, or non variant with respect to this type parameter.
    /// </summary>
    /// <value></value>
    public TypeParameterVariance Variance {
      get { return this.UnspecializedParameter.Variance; }
    }

    #endregion

    #region ITypeDefinition Members

    /// <summary>
    /// The byte alignment that values of the given type ought to have. Must be a power of 2. If zero, the alignment is decided at runtime.
    /// </summary>
    /// <value></value>
    public ushort Alignment {
      get { return this.UnspecializedParameter.Alignment; }
    }

    /// <summary>
    /// Zero or more events defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    /// <summary>
    /// Zero or more fields defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    /// <summary>
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return this.UnspecializedParameter.GenericParameterCount; }
    }

    /// <summary>
    /// True if the type may not be instantiated.
    /// </summary>
    /// <value></value>
    public bool IsAbstract {
      get { return this.UnspecializedParameter.IsAbstract; }
    }

    /// <summary>
    /// True if the type is a class (it is not an interface or type parameter and does not extend a special base class).
    /// Corresponds to C# class.
    /// </summary>
    /// <value></value>
    public bool IsClass {
      get { return this.UnspecializedParameter.IsClass; }
    }

    /// <summary>
    /// True if the type is a delegate (it extends System.MultiCastDelegate). Corresponds to C# delegate
    /// </summary>
    /// <value></value>
    public bool IsDelegate {
      get { return this.UnspecializedParameter.IsDelegate; }
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    /// <value></value>
    public bool IsEnum {
      get { return this.UnspecializedParameter.IsEnum; }
    }

    /// <summary>
    /// True if this type is parameterized (this.GenericParameters is a non empty collection).
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get { return this.UnspecializedParameter.IsGeneric; }
    }

    /// <summary>
    /// True if the type is an interface.
    /// </summary>
    /// <value></value>
    public bool IsInterface {
      get { return this.UnspecializedParameter.IsInterface; }
    }

    /// <summary>
    /// True if the type is a reference type. A reference type is non static class or interface or a suitably constrained type parameter.
    /// A type parameter for which MustBeReferenceType (the class constraint in C#) is true returns true for this property
    /// as does a type parameter with a constraint that is a class.
    /// </summary>
    /// <value></value>
    public bool IsReferenceType {
      get { return this.UnspecializedParameter.IsReferenceType; }
    }

    /// <summary>
    /// True if the type may not be subtyped.
    /// </summary>
    /// <value></value>
    public bool IsSealed {
      get { return this.UnspecializedParameter.IsSealed; }
    }

    /// <summary>
    /// True if the type is an abstract sealed class that directly extends System.Object and declares no constructors.
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get { return this.UnspecializedParameter.IsStatic; }
    }

    /// <summary>
    /// True if the type is a value type.
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    /// <value></value>
    public bool IsValueType {
      get { return this.UnspecializedParameter.IsValueType; }
    }

    /// <summary>
    /// True if the type is a struct (its not Primitive, is sealed and base is System.ValueType).
    /// </summary>
    /// <value></value>
    public bool IsStruct {
      get { return this.UnspecializedParameter.IsStruct; }
    }

    /// <summary>
    /// The collection of member instances that are members of this scope.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    /// <summary>
    /// Zero or more methods defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    /// <summary>
    /// Zero or more nested types defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    /// <summary>
    /// Zero or more properties defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    /// <summary>
    /// Size of an object of this type. In bytes. If zero, the size is unspecified and will be determined at runtime.
    /// </summary>
    /// <value></value>
    public uint SizeOf {
      get { return this.UnspecializedParameter.SizeOf; }
    }

    /// <summary>
    /// Declarative security actions for this type. Will be empty if this.HasSecurity is false.
    /// </summary>
    /// <value></value>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.UnspecializedParameter.SecurityAttributes; }
    }

    /// <summary>
    /// Returns a reference to the underlying (integral) type on which this (enum) type is based.
    /// </summary>
    /// <value></value>
    public ITypeReference UnderlyingType {
      get { return this.UnspecializedParameter.UnderlyingType; }
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive" CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    /// <value></value>
    public PrimitiveTypeCode TypeCode {
      get { return this.UnspecializedParameter.TypeCode; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return this.UnspecializedParameter.Locations; }
    }

    /// <summary>
    /// Layout of the type.
    /// </summary>
    /// <value></value>
    public LayoutKind Layout {
      get { return this.UnspecializedParameter.Layout; }
    }

    /// <summary>
    /// True if the type has special name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.UnspecializedParameter.IsSpecialName; }
    }

    /// <summary>
    /// Is this imported from COM type library
    /// </summary>
    /// <value></value>
    public bool IsComObject {
      get { return this.UnspecializedParameter.IsComObject; }
    }

    /// <summary>
    /// True if this type is serializable.
    /// </summary>
    /// <value></value>
    public bool IsSerializable {
      get { return this.UnspecializedParameter.IsSerializable; }
    }

    /// <summary>
    /// Is type initialized anytime before first access to static field
    /// </summary>
    /// <value></value>
    public bool IsBeforeFieldInit {
      get { return this.UnspecializedParameter.IsBeforeFieldInit; }
    }

    /// <summary>
    /// Default marshalling of the Strings in this class.
    /// </summary>
    /// <value></value>
    public StringFormatKind StringFormat {
      get { return this.UnspecializedParameter.StringFormat; }
    }

    /// <summary>
    /// True if this type gets special treatment from the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get { return this.UnspecializedParameter.IsRuntimeSpecial; }
    }

    /// <summary>
    /// True if this type has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    /// <value></value>
    public bool HasDeclarativeSecurity {
      get { return this.UnspecializedParameter.HasDeclarativeSecurity; }
    }

    /// <summary>
    /// Zero or more private type members generated by the compiler for implementation purposes. These members
    /// are only available after a complete visit of all of the other members of the type, including the bodies of methods.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.UnspecializedParameter.PrivateHelperMembers; }
    }

    #endregion

    #region IDefinition Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.UnspecializedParameter.Attributes; }
    }

    #endregion

    #region IDoubleDispatcher Members

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    #endregion

    #region IParameterListEntry Members

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.UnspecializedParameter.Index; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.UnspecializedParameter.Name; }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    //^ [Pure]
    /// <summary>
    /// Return true if the given member instance is a member of this scope.
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members with the given name that also satisfy the given predicate.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignoreCase"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members that satisfy the given predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    /// <summary>
    /// Returns the list of members with the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
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

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool IsModified {
      get { return false; }
    }

    /// <summary>
    /// The type definition being referred to.
    /// In case this type was alias, this is also the type of the aliased type
    /// </summary>
    /// <value></value>
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

    #region INamedTypeReference Members

    /// <summary>
    /// If true, the persisted type name is mangled by appending "`n" where n is the number of type parameters, if the number of type parameters is greater than 0.
    /// </summary>
    /// <value></value>
    public bool MangleName {
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

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class SpecializedGenericTypeParameter : SpecializedGenericParameter<IGenericTypeParameter>, IGenericTypeParameter {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedGenericParameter"></param>
    /// <param name="definingTypeInstance"></param>
    /// <param name="internFactory"></param>
    public SpecializedGenericTypeParameter(IGenericTypeParameter unspecializedGenericParameter, IGenericTypeInstanceReference definingTypeInstance, IInternFactory internFactory)
      : base(unspecializedGenericParameter, internFactory) {
      this.definingType = definingTypeInstance;
    }

    /// <summary>
    /// A list of classes or interfaces. All type arguments matching this parameter must be derived from all of the classes and implement all of the interfaces.
    /// </summary>
    /// <value></value>
    public override IEnumerable<ITypeReference> Constraints {
      get {
        foreach (ITypeReference unspecializedConstraint in this.Constraints)
          yield return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedConstraint.ResolvedType, this.DefiningType, this.InternFactory);
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IGenericTypeParameter) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The generic type that defines this type parameter.
    /// </summary>
    /// <value></value>
    public IGenericTypeInstanceReference DefiningType {
      get { return this.definingType; }
    }
    readonly IGenericTypeInstanceReference definingType;

    #region IGenericTypeParameter Members

    ITypeDefinition IGenericTypeParameter.DefiningType {
      get { return this.DefiningType.ResolvedType; }
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
  public class SpecializedNestedTypeDefinition : Scope<ITypeDefinitionMember>, ISpecializedNestedTypeDefinition, ISpecializedNestedTypeReference {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedVersion"></param>
    /// <param name="containingGenericTypeInstance"></param>
    /// <param name="internFactory"></param>
    public SpecializedNestedTypeDefinition(INestedTypeDefinition unspecializedVersion, IGenericTypeInstanceReference containingGenericTypeInstance, IInternFactory internFactory) {
      this.unspecializedVersion = unspecializedVersion;
      this.containingGenericTypeInstance = containingGenericTypeInstance;
      this.internFactory = internFactory;
    }

    /// <summary>
    /// Zero or more classes from which this type is derived.
    /// For CLR types this collection is empty for interfaces and System.Object and populated with exactly one base type for all other types.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeReference> BaseClasses {
      get {
        foreach (ITypeReference unspecializedBaseClassRef in this.unspecializedVersion.BaseClasses)
          yield return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedBaseClassRef.ResolvedType, this.containingGenericTypeInstance, this.InternFactory);
      }
    }

    /// <summary>
    /// Zero or more parameters that can be used as type annotations.
    /// </summary>
    /// <value></value>
    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get {
        foreach (IGenericTypeParameter unspecializedTypeParameter in this.unspecializedVersion.GenericParameters)
          yield return new SpecializedGenericTypeParameter(unspecializedTypeParameter, this.containingGenericTypeInstance, this.InternFactory);
      }
      //TODO: cache this
    }

    public IGenericTypeInstanceReference ContainingGenericTypeInstance {
      get { return this.containingGenericTypeInstance; }
    }
    readonly IGenericTypeInstanceReference containingGenericTypeInstance;

    /// <summary>
    /// Zero or more implementation overrides provided by the class.
    /// </summary>
    /// <value></value>
    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    /// <summary>
    /// Zero or more interfaces implemented by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeReference> Interfaces {
      get {
        foreach (ITypeReference unspecializedInterfaceRef in this.unspecializedVersion.Interfaces)
          yield return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedInterfaceRef.ResolvedType, this.containingGenericTypeInstance, this.InternFactory);
      }
    }

    /// <summary>
    /// An instance of this generic type that has been obtained by using the generic parameters as the arguments.
    /// Use this instance to look up members
    /// </summary>
    /// <value></value>
    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    /// <summary>
    /// Zero or more methods defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IMethodDefinition>(this.Members); }
    }

    /// <summary>
    /// A way to get to platform types such as System.Object.
    /// </summary>
    /// <value></value>
    public IPlatformType PlatformType {
      get { return this.UnspecializedVersion.PlatformType; }
    }

    /// <summary>
    /// Zero or more nested types defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, INestedTypeDefinition>(this.Members); }
    }

    /// <summary>
    /// Zero or more private type members generated by the compiler for implementation purposes. These members
    /// are only available after a complete visit of all of the other members of the type, including the bodies of methods.
    /// </summary>
    /// <value></value>
    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    /// <summary>
    /// Zero or more properties defined by this type.
    /// </summary>
    /// <value></value>
    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetFilterEnumerable<ITypeDefinitionMember, IPropertyDefinition>(this.Members); }
    }

    /// <summary>
    /// Returns a reference to the underlying (integral) type on which this (enum) type is based.
    /// </summary>
    /// <value></value>
    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    /// <summary>
    /// The nested type that has been specialized to obtain this nested type. When the containing type is an instance of type which is itself a specialized member (i.e. it is a nested
    /// type of a generic type instance), then the unspecialized member refers to a member from the unspecialized containing type. (I.e. the unspecialized member always
    /// corresponds to a definition that is not obtained via specialization.)
    /// </summary>
    /// <value></value>
    public INestedTypeDefinition UnspecializedVersion {
      get { return this.unspecializedVersion; }
    }
    readonly INestedTypeDefinition unspecializedVersion;

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get {
        if (this.visibility == TypeMemberVisibility.Default) {
          this.visibility = TypeHelper.VisibilityIntersection(this.UnspecializedVersion.Visibility,
            TypeHelper.TypeVisibilityAsTypeMemberVisibility(this.ContainingGenericTypeInstance.ResolvedType));
        }
        return this.visibility;
      }
    }
    TypeMemberVisibility visibility = TypeMemberVisibility.Default;

    #region INestedTypeDefinition Members

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingTypeDefinition {
      get { return this.ContainingGenericTypeInstance.ResolvedType; }
    }

    #endregion

    #region ITypeDefinition Members

    /// <summary>
    /// The byte alignment that values of the given type ought to have. Must be a power of 2. If zero, the alignment is decided at runtime.
    /// </summary>
    /// <value></value>
    public ushort Alignment {
      get { return this.UnspecializedVersion.Alignment; }
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
    /// The number of generic parameters. Zero if the type is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount {
      get { return this.UnspecializedVersion.GenericParameterCount; }
    }

    /// <summary>
    /// True if the type may not be instantiated.
    /// </summary>
    /// <value></value>
    public bool IsAbstract {
      get { return this.UnspecializedVersion.IsAbstract; }
    }

    /// <summary>
    /// True if the type is a class (it is not an interface or type parameter and does not extend a special base class).
    /// Corresponds to C# class.
    /// </summary>
    /// <value></value>
    public bool IsClass {
      get { return this.UnspecializedVersion.IsClass; }
    }

    /// <summary>
    /// True if the type is a delegate (it extends System.MultiCastDelegate). Corresponds to C# delegate
    /// </summary>
    /// <value></value>
    public bool IsDelegate {
      get { return this.UnspecializedVersion.IsDelegate; }
    }

    /// <summary>
    /// True if the type is an enumeration (it extends System.Enum and is sealed). Corresponds to C# enum.
    /// </summary>
    /// <value></value>
    public bool IsEnum {
      get { return this.UnspecializedVersion.IsEnum; }
    }

    /// <summary>
    /// True if this type is parameterized (this.GenericParameters is a non empty collection).
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get { return this.UnspecializedVersion.IsGeneric; }
    }

    /// <summary>
    /// True if the type is an interface.
    /// </summary>
    /// <value></value>
    public bool IsInterface {
      get { return this.UnspecializedVersion.IsInterface; }
    }

    /// <summary>
    /// True if the type is a reference type. A reference type is non static class or interface or a suitably constrained type parameter.
    /// A type parameter for which MustBeReferenceType (the class constraint in C#) is true returns true for this property
    /// as does a type parameter with a constraint that is a class.
    /// </summary>
    /// <value></value>
    public bool IsReferenceType {
      get { return this.UnspecializedVersion.IsReferenceType; }
    }

    /// <summary>
    /// True if the type may not be subtyped.
    /// </summary>
    /// <value></value>
    public bool IsSealed {
      get { return this.UnspecializedVersion.IsSealed; }
    }

    /// <summary>
    /// True if the type is an abstract sealed class that directly extends System.Object and declares no constructors.
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get { return this.UnspecializedVersion.IsStatic; }
    }

    /// <summary>
    /// True if the type is a value type.
    /// Value types are sealed and extend System.ValueType or System.Enum.
    /// A type parameter for which MustBeValueType (the struct constraint in C#) is true also returns true for this property.
    /// </summary>
    /// <value></value>
    public bool IsValueType {
      get { return this.UnspecializedVersion.IsValueType; }
    }

    /// <summary>
    /// True if the type is a struct (its not Primitive, is sealed and base is System.ValueType).
    /// </summary>
    /// <value></value>
    public bool IsStruct {
      get { return this.UnspecializedVersion.IsStruct; }
    }

    /// <summary>
    /// Size of an object of this type. In bytes. If zero, the size is unspecified and will be determined at runtime.
    /// </summary>
    /// <value></value>
    public uint SizeOf {
      get { return this.UnspecializedVersion.SizeOf; }
    }

    /// <summary>
    /// Declarative security actions for this type. Will be empty if this.HasSecurity is false.
    /// </summary>
    /// <value></value>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get
        //^^ requires this.HasSecurityAttributes;
      {
        return this.UnspecializedVersion.SecurityAttributes; 
      }
    }

    /// <summary>
    /// Unless the value of TypeCode is PrimitiveTypeCode.NotPrimitive, the type corresponds to a "primitive" CLR type (such as System.Int32) and
    /// the type code identifies which of the primitive types it corresponds to.
    /// </summary>
    /// <value></value>
    public PrimitiveTypeCode TypeCode {
      get { return this.UnspecializedVersion.TypeCode; }
    }

    public LayoutKind Layout {
      get { return this.UnspecializedVersion.Layout; }
    }

    public bool IsSpecialName {
      get { return this.UnspecializedVersion.IsSpecialName; }
    }

    public bool IsComObject {
      get { return this.UnspecializedVersion.IsComObject; }
    }

    public bool IsSerializable {
      get { return this.UnspecializedVersion.IsSerializable; }
    }

    public bool IsBeforeFieldInit {
      get { return this.UnspecializedVersion.IsBeforeFieldInit; }
    }

    public StringFormatKind StringFormat {
      get { return this.UnspecializedVersion.StringFormat; }
    }

    public bool IsRuntimeSpecial {
      get { return this.UnspecializedVersion.IsRuntimeSpecial; }
    }

    public bool HasDeclarativeSecurity {
      get
        //^ ensures result == this.UnspecializedVersion.HasDeclarativeSecurity;
      { 
        return this.UnspecializedVersion.HasDeclarativeSecurity; 
      }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.UnspecializedVersion.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return this.UnspecializedVersion.Locations; }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.UnspecializedVersion.Name; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    ITypeReference ITypeMemberReference.ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }
    #endregion

    #region INestedTypeReference Members

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region ITypeReference Members

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

    #region ISpecializedNestedTypeReference Members

    INestedTypeReference ISpecializedNestedTypeReference.UnspecializedVersion {
      get { return this.UnspecializedVersion; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return this.unspecializedVersion.MangleName; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion

  }

  public abstract class SystemDefinedStructuralType : ITypeDefinition {

    protected SystemDefinedStructuralType(IInternFactory internFactory) {
      this.internFactory = internFactory;
    }

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public virtual IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get { return 0; }
    }

    public bool HasDeclarativeSecurity {
      get { return true; }
    }

    public virtual IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
    }

    public bool IsInterface {
      get { return false; }
    }

    public virtual bool IsReferenceType {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public virtual IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public abstract IPlatformType PlatformType { get; }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.AutoChar; }
    }

    public virtual PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public abstract void Dispatch(IMetadataVisitor visitor);

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    //^ [Pure]
    public virtual bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public virtual IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public virtual IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public virtual IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    IEnumerable<ITypeDefinitionMember> IScope<ITypeDefinitionMember>.GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    IEnumerable<ITypeDefinitionMember> IScope<ITypeDefinitionMember>.GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    //^ [Pure]
    IEnumerable<ITypeDefinitionMember> IScope<ITypeDefinitionMember>.GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public virtual IEnumerable<ICustomModifier> CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public virtual bool IsModified {
      get { return false; }
    }

    public ITypeDefinition ResolvedType {
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

  internal static class TypeDefinition {

    /// <summary>
    /// If the given unspecialized type reference is a constructed type, such as an instance of IArrayTypeReference or IPointerTypeReference or IGenericTypeInstanceReference,
    /// then return a new instance (if necessary) in which all refererences to the type parameters of containingMethodInstance.GenericType have been replaced 
    /// with the corresponding values from containingMethodInstance.GenericArguments. If the type is not a constructed type the method just returns the type.
    /// For the purpose of this method, an instance of IGenericParameter is regarded as a constructed type.
    /// </summary>
    internal static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(ITypeReference unspecializedType, IGenericMethodInstanceReference containingMethodInstance, IInternFactory internFactory) {
      IArrayTypeReference/*?*/ arrayType = unspecializedType as IArrayTypeReference;
      if (arrayType != null) {
        if (arrayType.IsVector) return Vector.SpecializeIfConstructedFromApplicableTypeParameter(arrayType, containingMethodInstance, internFactory);
        return Matrix.SpecializeIfConstructedFromApplicableTypeParameter(arrayType, containingMethodInstance, internFactory);
      }
      IGenericMethodParameterReference/*?*/ genericMethodParameter = unspecializedType as IGenericMethodParameterReference;
      if (genericMethodParameter != null) return GenericParameter.SpecializeIfConstructedFromApplicableTypeParameter(genericMethodParameter, containingMethodInstance);
      IGenericTypeInstanceReference/*?*/ genericTypeInstance = unspecializedType as IGenericTypeInstanceReference;
      if (genericTypeInstance != null) return GenericTypeInstance.SpecializeIfConstructedFromApplicableTypeParameter(genericTypeInstance, containingMethodInstance, internFactory);
      IManagedPointerTypeReference/*?*/ managedPointerType = unspecializedType as IManagedPointerTypeReference;
      if (managedPointerType != null) return ManagedPointerType.SpecializeIfConstructedFromApplicableTypeParameter(managedPointerType, containingMethodInstance, internFactory);
      IPointerTypeReference/*?*/ pointerType = unspecializedType as IPointerTypeReference;
      if (pointerType != null) return PointerType.SpecializeIfConstructedFromApplicableTypeParameter(pointerType, containingMethodInstance, internFactory);
      return unspecializedType;
    }

    /// <summary>
    /// If the given unspecialized type definition is a constructed type, such as an instance of IArrayType or IPointerType or IGenericTypeInstance, then return a new instance (if necessary)
    /// in which all refererences to the type parameters of containingTypeInstance.GenericType have been replaced with the corresponding values
    /// from containingTypeInstance.GenericArguments. If the type is not a constructed type the method just returns the type.
    /// For the purpose of this method, an instance of IGenericParameter is regarded as a constructed type.
    /// </summary>
    internal static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(ITypeReference unspecializedType, IGenericTypeInstanceReference containingTypeInstance, IInternFactory internFactory) {
      IArrayTypeReference/*?*/ arrayType = unspecializedType as IArrayTypeReference;
      if (arrayType != null) {
        if (arrayType.IsVector) return Vector.SpecializeIfConstructedFromApplicableTypeParameter(arrayType, containingTypeInstance, internFactory);
        return Matrix.SpecializeIfConstructedFromApplicableTypeParameter(arrayType, containingTypeInstance, internFactory);
      }
      IGenericTypeParameterReference/*?*/ genericTypeParameter = unspecializedType as IGenericTypeParameterReference;
      if (genericTypeParameter != null) return GenericParameter.SpecializeIfConstructedFromApplicableTypeParameter(genericTypeParameter, containingTypeInstance);
      IGenericTypeInstanceReference/*?*/ genericTypeInstance = unspecializedType as IGenericTypeInstanceReference;
      if (genericTypeInstance != null) return GenericTypeInstance.SpecializeIfConstructedFromApplicableTypeParameter(genericTypeInstance, containingTypeInstance, internFactory);
      IManagedPointerTypeReference/*?*/ managedPointerType = unspecializedType as IManagedPointerTypeReference;
      if (managedPointerType != null) return ManagedPointerType.SpecializeIfConstructedFromApplicableTypeParameter(managedPointerType, containingTypeInstance, internFactory);
      IPointerTypeReference/*?*/ pointerType = unspecializedType as IPointerTypeReference;
      if (pointerType != null) return PointerType.SpecializeIfConstructedFromApplicableTypeParameter(pointerType, containingTypeInstance, internFactory);
      return unspecializedType;
    }

  }

  public class Vector : ArrayType {

    private Vector(ITypeReference elementType, IInternFactory internFactory)
      : base(elementType, internFactory) {
    }

    //  Issue: Does this have to give non generic interfaces since they come from System.Array?!?
    protected override IEnumerable<ITypeReference> GetInterfaceList() {
      List<ITypeReference> interfaces = new List<ITypeReference>(7);
      List<ITypeReference> argTypes = new List<ITypeReference>(1);
      argTypes.Add(this.ElementType);
      interfaces.Add(GenericTypeInstance.GetGenericTypeInstance(this.PlatformType.SystemCollectionsGenericIList, argTypes.AsReadOnly(), this.InternFactory));
      interfaces.Add(this.PlatformType.SystemICloneable);
      interfaces.Add(this.PlatformType.SystemCollectionsIEnumerable);
      interfaces.Add(this.PlatformType.SystemCollectionsICollection);
      interfaces.Add(this.PlatformType.SystemCollectionsIList);
      interfaces.Add(GenericTypeInstance.GetGenericTypeInstance(this.PlatformType.SystemCollectionsGenericIEnumerable, argTypes.AsReadOnly(), this.InternFactory));
      interfaces.Add(GenericTypeInstance.GetGenericTypeInstance(this.PlatformType.SystemCollectionsGenericICollection, argTypes.AsReadOnly(), this.InternFactory));
      return interfaces.AsReadOnly();
    }

    public static Vector GetVector(ITypeReference elementType, IInternFactory internFactory) {
      return new Vector(elementType, internFactory);
    }

    /// <summary>
    /// If the given vector has an element type that involves a type parameter from the generic method from which the given method was instantiated,
    /// then return a new vector using an element type that has been specialized with the type arguments of the given generic method instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IArrayTypeReference array, IGenericMethodInstanceReference method, IInternFactory internFactory)
      //^ requires array.IsVector;
    {
      ITypeReference elementType = array.ElementType;
      ITypeReference specializedElementType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(elementType, method, internFactory);
      if (elementType == specializedElementType) return array;
      return GetVector(specializedElementType, internFactory);
    }

    /// <summary>
    /// If the given vector has an element type that involves a type parameter from the generic type from which the given type was instantiated,
    /// then return a new vector using an element type that has been specialized with the type arguments of the given generic type instance.
    /// </summary>
    public static ITypeReference SpecializeIfConstructedFromApplicableTypeParameter(IArrayTypeReference array, IGenericTypeInstanceReference type, IInternFactory internFactory)
      //^ requires array.IsVector;
    {
      ITypeReference elementType = array.ElementType;
      ITypeReference specializedElementType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(elementType, type, internFactory);
      if (elementType == specializedElementType) return array;
      return GetVector(specializedElementType, internFactory);
    }

  }

}
#pragma warning restore 1591
