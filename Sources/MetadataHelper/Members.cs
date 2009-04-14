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

  public class GenericMethodInstance : IGenericMethodInstance {

    public GenericMethodInstance(IMethodDefinition genericMethod, IEnumerable<ITypeReference> genericArguments, IInternFactory internFactory)
      //^ requires genericMethod.IsGeneric;
    {
      this.genericMethod = genericMethod;
      this.genericArguments = genericArguments;
      this.internFactory = internFactory;
    }

    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    public CallingConvention CallingConvention {
      get { return this.GenericMethod.CallingConvention; }
    }

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.GenericMethod.ContainingTypeDefinition; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit((IGenericMethodInstanceReference)this);
    }

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.genericArguments; }
    }
    readonly IEnumerable<ITypeReference> genericArguments;

    public IMethodDefinition GenericMethod {
      get { return this.genericMethod; }
    }
    readonly IMethodDefinition genericMethod;
    //^ invariant genericMethod.IsGeneric;

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    //^ [Pure]
    public ushort GenericParameterCount {
      get { return 0; }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get {
        foreach (IParameterDefinition parameter in this.genericMethod.Parameters)
          yield return new SpecializedParameterDefinition(parameter, this, this.InternFactory);
      }
    }

    public ushort ParameterCount {
      get { return this.genericMethod.ParameterCount; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.GenericMethod.ReturnValueAttributes; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.GenericMethod.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.GenericMethod.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.GenericMethod.ReturnValueIsMarshalledExplicitly; }
    }

    public bool ReturnValueIsModified {
      get { return this.GenericMethod.ReturnValueIsModified; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.GenericMethod.ReturnValueMarshallingInformation; }
    }

    public override string ToString() {
      return MemberHelper.GetMethodSignature(this,
        NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName);
    }

    public ITypeReference Type {
      get {
        if (this.type == null) {
          ITypeReference unspecializedType = this.genericMethod.Type;
          ITypeReference specializedType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this, this.InternFactory);
          if (unspecializedType == specializedType)
            this.type = this.genericMethod.Type;
          else
            this.type = specializedType;
        }
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    public TypeMemberVisibility Visibility {
      get {
        return TypeHelper.GenericInstanceVisibilityAsTypeMemberVisibility(this.GenericMethod.Visibility, this.GenericArguments);
      }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return Dummy.PlatformInvokeInformation; }
    }

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return this.GenericMethod.ResolvedMethod.AcceptsExtraArguments; }
    }

    public bool HasDeclarativeSecurity {
      get { return this.GenericMethod.HasDeclarativeSecurity; }
    }

    public bool HasExplicitThisParameter {
      get { return this.GenericMethod.HasExplicitThisParameter; }
    }

    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    public bool IsAbstract {
      get { return this.GenericMethod.IsAbstract; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return this.GenericMethod.IsAccessCheckedOnOverride; }
    }

    public bool IsCil {
      get { return this.GenericMethod.IsCil; }
    }

    public bool IsExternal {
      get { return this.GenericMethod.IsExternal; }
    }

    public bool IsForwardReference {
      get { return this.GenericMethod.IsForwardReference; }
    }

    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
    }

    public bool IsHiddenBySignature {
      get { return this.GenericMethod.IsHiddenBySignature; }
    }

    public bool IsNativeCode {
      get { return this.GenericMethod.IsNativeCode; }
    }

    public bool IsNewSlot {
      get { return this.GenericMethod.IsNewSlot; }
    }

    public bool IsNeverInlined {
      get { return this.GenericMethod.IsNeverInlined; }
    }

    public bool IsNeverOptimized {
      get { return this.GenericMethod.IsNeverOptimized; }
    }

    public bool IsPlatformInvoke {
      get { return this.GenericMethod.IsPlatformInvoke; }
    }

    public bool IsRuntimeImplemented {
      get { return this.GenericMethod.IsRuntimeImplemented; }
    }

    public bool IsRuntimeInternal {
      get { return this.GenericMethod.IsRuntimeInternal; }
    }

    public bool IsRuntimeSpecial {
      get { return this.GenericMethod.IsRuntimeSpecial; }
    }

    public bool IsSealed {
      get { return this.GenericMethod.IsSealed; }
    }

    public bool IsSpecialName {
      get { return this.GenericMethod.IsSpecialName; }
    }

    public bool IsStatic {
      get { return this.GenericMethod.IsStatic; }
    }

    public bool IsSynchronized {
      get { return this.GenericMethod.IsSynchronized; }
    }

    public bool IsUnmanaged {
      get { return this.GenericMethod.IsUnmanaged; }
    }

    public bool IsVirtual {
      get {
        bool result = this.GenericMethod.IsVirtual;
        //^ assume result ==> !this.IsStatic;
        return result;
      }
    }

    public bool PreserveSignature {
      get { return this.GenericMethod.PreserveSignature; }
    }

    public bool RequiresSecurityObject {
      get { return this.GenericMethod.RequiresSecurityObject; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.GenericMethod.SecurityAttributes; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.GenericMethod.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.GenericMethod.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return ((IDefinition)this.GenericMethod).Locations; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region IMethodDefinition Members


    public bool IsConstructor {
      get { return this.GenericMethod.IsConstructor; }
    }

    public bool IsStaticConstructor {
      get { return this.GenericMethod.IsStaticConstructor; }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region IMethodReference Members

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IGenericMethodInstanceReference Members

    IMethodReference IGenericMethodInstanceReference.GenericMethod {
      get { return this.GenericMethod; }
    }

    #endregion
  }

  public class GenericMethodInstanceReference : IGenericMethodInstanceReference {

    public GenericMethodInstanceReference(IMethodReference genericMethod, IEnumerable<ITypeReference> genericArguments, IInternFactory internFactory)
      //^ requires genericMethod.IsGeneric;
    {
      this.genericMethod = genericMethod;
      this.genericArguments = genericArguments;
      this.internFactory = internFactory;
    }

    public bool AcceptsExtraArguments {
      get { return this.genericMethod.AcceptsExtraArguments; }
    }

    public CallingConvention CallingConvention {
      get { return this.GenericMethod.CallingConvention; }
    }

    public ITypeReference ContainingType {
      get { return this.GenericMethod.ContainingType; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit((IGenericMethodInstanceReference)this);
    }

    public IEnumerable<ITypeReference> GenericArguments {
      get { return this.genericArguments; }
    }
    readonly IEnumerable<ITypeReference> genericArguments;

    public IMethodReference GenericMethod {
      get { return this.genericMethod; }
    }
    readonly IMethodReference genericMethod;
    //^ invariant genericMethod.IsGeneric;

    //^ [Pure]
    public ushort GenericParameterCount {
      get { return 0; }
    }

    public IEnumerable<IParameterTypeInformation> Parameters {
      get {
        foreach (IParameterTypeInformation parameter in this.genericMethod.Parameters)
          yield return new SpecializedParameterTypeInformation(parameter, this, this.InternFactory);
      }
    }

    public ushort ParameterCount {
      get { return this.genericMethod.ParameterCount; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.GenericMethod.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.GenericMethod.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.GenericMethod.ReturnValueIsModified; }
    }

    public override string ToString() {
      return MemberHelper.GetMethodSignature(this,
        NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName);
    }

    public ITypeReference Type {
      get {
        if (this.type == null) {
          ITypeReference unspecializedType = this.genericMethod.Type;
          ITypeReference specializedType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this, this.InternFactory);
          if (unspecializedType == specializedType)
            this.type = this.genericMethod.Type;
          else
            this.type = specializedType;
        }
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
    }

    #region INamedEntity Members

    public IName Name {
      get { return this.GenericMethod.Name; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.GenericMethod.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return this.GenericMethod.Locations; }
    }

    #endregion

    #region IMethodReference Members

    public IMethodDefinition ResolvedMethod {
      get { return new GenericMethodInstance(this.GenericMethod.ResolvedMethod, this.GenericArguments, this.InternFactory); }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

    #endregion
  }

  public class GenericMethodParameterReference : IGenericMethodParameterReference {

    public GenericMethodParameterReference(IName name, ushort index, IMetadataHost host) {
      this.name = name;
      this.index = index;
      this.host = host;
    }

    IMetadataHost host;

    #region IGenericMethodParameterReference Members

    public IMethodReference DefiningMethod {
      get { return this.defininingMethod; }
      set { this.defininingMethod = value; }
    }
    IMethodReference defininingMethod = Dummy.MethodReference;

    public IGenericMethodParameter ResolvedType {
      get {
        IMethodDefinition definingMethodDef = this.DefiningMethod.ResolvedMethod;
        if (!definingMethodDef.IsGeneric) return Dummy.GenericMethodParameter;
        ushort i = 0;
        foreach (IGenericMethodParameter genericParameter in definingMethodDef.GenericParameters) {
          if (i++ == this.index) return genericParameter;
        }
        return Dummy.GenericMethodParameter; 
      }
    }

    #endregion

    #region ITypeReference Members

    public IAliasForType/*?*/ AliasForType {
      get { return null; }
    }

    public uint InternedKey {
      get { return this.host.InternFactory.GetGenericMethodParameterReferenceInternedKey(this.defininingMethod, this.index); }
    }

    public bool IsAlias {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public IPlatformType PlatformType {
      get { return this.host.PlatformType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this.ResolvedType; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.NotPrimitive; }
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

    #region INamedEntity Members

    public IName Name {
      get { return this.name; }
    }
    readonly IName name;

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return this.index; }
    }
    readonly ushort index;

    #endregion
  }

  public class SpecializedEventDefinition : SpecializedTypeDefinitionMember<IEventDefinition>, ISpecializedEventDefinition {

    public SpecializedEventDefinition(IEventDefinition unspecializedVersion, GenericTypeInstance containingGenericTypeInstance) 
      : base(unspecializedVersion, containingGenericTypeInstance){
    }

    public IEnumerable<IMethodReference> Accessors {
      get {
        foreach (IMethodReference accessor in this.UnspecializedVersion.Accessors) {
          ITypeDefinitionMember result = this.ContainingGenericTypeInstance.SpecializeMember(accessor.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
          yield return (IMethodReference)result;
        }
      }
    }

    public IMethodReference Adder {
      get {
        ITypeDefinitionMember result = this.ContainingGenericTypeInstance.SpecializeMember(this.UnspecializedVersion.Adder.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
        return (IMethodReference)result;
      }
    }

    public IMethodReference/*?*/ Caller {
      get {
        IMethodReference/*?*/ caller = this.UnspecializedVersion.Caller;
        if (caller == null) return null;
        ITypeDefinitionMember result = this.ContainingGenericTypeInstance.SpecializeMember(caller.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
        return (IMethodReference)result;
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IEventDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IMethodReference Remover {
      get {
        ITypeDefinitionMember result = this.ContainingGenericTypeInstance.SpecializeMember(this.UnspecializedVersion.Remover.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
        return (IMethodReference)result;
      }
    }

    public ITypeReference Type {
      get {
        ITypeReference unspecializedType = this.UnspecializedVersion.Type.ResolvedType;
        ITypeReference specializedType =  TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
        if (unspecializedType == specializedType)
          return this.UnspecializedVersion.Type;
        else
          return specializedType;
      }
    }

    #region IEventDefinition Members

    /// <summary>
    /// True if the event gets special treatment from the runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return this.UnspecializedVersion.IsRuntimeSpecial; }
    }

    public bool IsSpecialName {
      get { return this.UnspecializedVersion.IsSpecialName; }
    }

    #endregion
  }

  public class SpecializedFieldDefinition : SpecializedTypeDefinitionMember<IFieldDefinition>, ISpecializedFieldDefinition, ISpecializedFieldReference {

    public SpecializedFieldDefinition(IFieldDefinition unspecializedVersion, GenericTypeInstance containingGenericTypeInstance) 
      : base(unspecializedVersion, containingGenericTypeInstance){
    }

    /// <summary>
    /// Calls the visitor.Visit(IFieldDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference Type {
      get {
        if (this.type == null) {
          lock (GlobalLock.LockingObject) {
            if (this.type == null) {
              ITypeReference unspecializedType = this.UnspecializedVersion.Type;
              ITypeReference specializedType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
              if (unspecializedType == specializedType)
                this.type = this.UnspecializedVersion.Type;
              else
                this.type = specializedType;
            }
          }
        }
        return this.type; 
      }
    }
    ITypeReference/*?*/ type;

    public uint BitLength {
      get { return this.UnspecializedVersion.BitLength; }
    }

    public bool IsBitField {
      get { return this.UnspecializedVersion.IsBitField; }
    }

    public bool IsCompileTimeConstant {
      get { return this.UnspecializedVersion.IsCompileTimeConstant; }
    }

    public bool IsMapped {
      get { 
        bool result = this.UnspecializedVersion.IsMapped;
        //^ assume this.IsStatic == this.UnspecializedVersion.IsStatic; //it is the post condition of this.IsStatic;
        return result;
      }
    }

    public bool IsMarshalledExplicitly {
      get { return this.UnspecializedVersion.IsMarshalledExplicitly; }
    }

    public bool IsNotSerialized {
      get { return this.UnspecializedVersion.IsNotSerialized; }
    }

    public bool IsReadOnly {
      get { return this.UnspecializedVersion.IsReadOnly; }
    }

    public bool IsRuntimeSpecial {
      get { 
        bool result = this.UnspecializedVersion.IsRuntimeSpecial;
        //^ assume this.IsSpecialName == this.UnspecializedVersion.IsSpecialName; //it is the post condition of this.IsSpecialName;
        return result;
      }
    }

    public bool IsSpecialName {
      get
        //^ ensures result == this.UnspecializedVersion.IsSpecialName;
      { 
        return this.UnspecializedVersion.IsSpecialName; 
      }
    }

    public bool IsStatic {
      get 
        //^ ensures result == this.UnspecializedVersion.IsStatic;
      { 
        return this.UnspecializedVersion.IsStatic; 
      }
    }

    public uint Offset {
      get
        //^^ requires this.ContainingTypeDefinition.Layout == LayoutKind.Explicit;
      {
        //^ assume this.UnspecializedVersion.ContainingTypeDefinition == this.ContainingGenericTypeInstance.GenericType.ResolvedType;
        //^ assume this.ContainingTypeDefinition.Layout == this.UnspecializedVersion.ContainingTypeDefinition.Layout;
        return this.UnspecializedVersion.Offset; 
      }
    }

    public int SequenceNumber {
      get { return this.UnspecializedVersion.SequenceNumber; }
    }

    public IMetadataConstant CompileTimeValue {
      get { return this.UnspecializedVersion.CompileTimeValue; }
    }

    public IMarshallingInformation MarshallingInformation {
      get { return this.UnspecializedVersion.MarshallingInformation; }
    }

    public ISectionBlock FieldMapping {
      get { return this.UnspecializedVersion.FieldMapping; }
    }

    #region IFieldDefinition Members

    IMetadataConstant IFieldDefinition.CompileTimeValue {
      get { return this.CompileTimeValue; }
    }

    #endregion

    #region IFieldReference Members


    public IFieldDefinition ResolvedField {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.CompileTimeValue; }
    }

    #endregion


    #region ISpecializedFieldReference Members

    IFieldReference ISpecializedFieldReference.UnspecializedVersion {
      get { return this.UnspecializedVersion; }
    }

    #endregion
  }

  public class SpecializedGenericMethodParameter : SpecializedGenericParameter<IGenericMethodParameter>, IGenericMethodParameter {

    public SpecializedGenericMethodParameter(IGenericMethodParameter unspecializedParameter, SpecializedMethodDefinition definingMethod)
      : base(unspecializedParameter, definingMethod.ContainingGenericTypeInstance.InternFactory) {
      this.definingMethod = definingMethod;
    }

    public override IEnumerable<ITypeReference> Constraints {
      get {
        foreach (ITypeReference unspecializedConstraint in this.Constraints)
          yield return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedConstraint.ResolvedType, this.DefiningMethod.ContainingGenericTypeInstance, this.DefiningMethod.ContainingGenericTypeInstance.InternFactory);
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IGenericMethodParameter) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public SpecializedMethodDefinition DefiningMethod {
      get { return this.definingMethod; }
    }
    readonly SpecializedMethodDefinition definingMethod;

    #region IGenericMethodParameter Members

    IMethodDefinition IGenericMethodParameter.DefiningMethod {
      get { return this.UnspecializedParameter.DefiningMethod; }
    }

    #endregion

    #region IGenericMethodParameterReference Members

    IMethodReference IGenericMethodParameterReference.DefiningMethod {
      get { return this.DefiningMethod; }
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion
  }

  public class SpecializedMethodDefinition : SpecializedTypeDefinitionMember<IMethodDefinition>, ISpecializedMethodDefinition, ISpecializedMethodReference {

    public SpecializedMethodDefinition(IMethodDefinition unspecializedVersion, GenericTypeInstance containingGenericTypeInstance) 
      : base(unspecializedVersion, containingGenericTypeInstance){
    }

    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    public CallingConvention CallingConvention {
      get { return this.UnspecializedVersion.CallingConvention; }
    }

    /// <summary>
    /// Calls the visitor.Visit(IMethodDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get {
        foreach (IGenericMethodParameter parameter in this.UnspecializedVersion.GenericParameters)
          yield return new SpecializedGenericMethodParameter(parameter, this);
      }
    }

    //^ [Pure] 
    public ushort GenericParameterCount {
      get { return this.UnspecializedVersion.GenericParameterCount; }
    }

    public bool IsConstructor {
      get { return this.UnspecializedVersion.IsConstructor; }
    }

    public bool IsStaticConstructor {
      get { return this.UnspecializedVersion.IsStaticConstructor; }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get {
        foreach (IParameterDefinition parameter in this.UnspecializedVersion.Parameters)
          yield return new SpecializedParameterDefinition(parameter, this, this.ContainingGenericTypeInstance.InternFactory);
      }
    }

    public ushort ParameterCount {
      get { return this.UnspecializedVersion.ParameterCount; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.UnspecializedVersion.PlatformInvokeData; }
    }

    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.TypeParameters);    
    }

    public ITypeReference Type {
      get {
        ITypeReference unspecializedType = this.UnspecializedVersion.Type;
        ITypeReference specializedType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
        if (unspecializedType == specializedType)
          return this.UnspecializedVersion.Type;
        else
          return specializedType;
      }
    }

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return this.UnspecializedVersion.AcceptsExtraArguments; }
    }

    public bool HasDeclarativeSecurity {
      get { return this.UnspecializedVersion.HasDeclarativeSecurity; }
    }

    public bool HasExplicitThisParameter {
      get { return this.UnspecializedVersion.HasExplicitThisParameter; }
    }

    public bool IsAbstract {
      get { return this.UnspecializedVersion.IsAbstract; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return this.UnspecializedVersion.IsAccessCheckedOnOverride; }
    }

    public bool IsCil {
      get { return this.UnspecializedVersion.IsCil; }
    }

    public bool IsExternal {
      get { return this.UnspecializedVersion.IsExternal; }
    }

    public bool IsForwardReference {
      get { return this.UnspecializedVersion.IsForwardReference; }
    }

    public bool IsGeneric {
      get { return this.UnspecializedVersion.IsGeneric; }
    }

    public bool IsHiddenBySignature {
      get { return this.UnspecializedVersion.IsHiddenBySignature; }
    }

    public bool IsNativeCode {
      get { return this.UnspecializedVersion.IsNativeCode; }
    }

    public bool IsNewSlot {
      get { return this.UnspecializedVersion.IsNewSlot; }
    }

    public bool IsNeverInlined {
      get { return this.UnspecializedVersion.IsNeverInlined; }
    }

    public bool IsNeverOptimized {
      get { return this.UnspecializedVersion.IsNeverOptimized; }
    }

    public bool IsPlatformInvoke {
      get { return this.UnspecializedVersion.IsPlatformInvoke; }
    }

    public bool IsRuntimeImplemented {
      get { return this.UnspecializedVersion.IsRuntimeImplemented; }
    }

    public bool IsRuntimeInternal {
      get { return this.UnspecializedVersion.IsRuntimeInternal; }
    }

    public bool IsRuntimeSpecial {
      get { return this.UnspecializedVersion.IsRuntimeSpecial; }
    }

    public bool IsSealed {
      get { return this.UnspecializedVersion.IsSealed; }
    }

    public bool IsSpecialName {
      get { return this.UnspecializedVersion.IsSpecialName; }
    }

    public bool IsStatic {
      get { return this.UnspecializedVersion.IsStatic; }
    }

    public bool IsSynchronized {
      get { return this.UnspecializedVersion.IsSynchronized; }
    }

    public bool IsUnmanaged {
      get { return this.UnspecializedVersion.IsUnmanaged; }
    }

    public bool IsVirtual {
      get { 
        bool result = this.UnspecializedVersion.IsVirtual; 
        //^ assume result ==> !this.IsStatic;
        return result;
      }
    }

    public bool PreserveSignature {
      get { return this.UnspecializedVersion.PreserveSignature; }
    }

    public bool RequiresSecurityObject {
      get { return this.UnspecializedVersion.RequiresSecurityObject; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.UnspecializedVersion.SecurityAttributes; }
    }

    #endregion

    #region IMethodReference Members

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.ContainingGenericTypeInstance.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.UnspecializedVersion.ReturnValueAttributes; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.UnspecializedVersion.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.UnspecializedVersion.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.UnspecializedVersion.ReturnValueIsMarshalledExplicitly; }
    }

    public bool ReturnValueIsModified {
      get { return this.UnspecializedVersion.ReturnValueIsModified; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.UnspecializedVersion.ReturnValueMarshallingInformation; }
    }

    #endregion

    #region ISpecializedMethodReference Members

    IMethodReference ISpecializedMethodReference.UnspecializedVersion {
      get { return this.UnspecializedVersion; }
    }

    #endregion
  }

  public class SpecializedParameterDefinition : IParameterDefinition {

    protected internal SpecializedParameterDefinition(IParameterDefinition unspecializedParameter, IGenericMethodInstanceReference containingSignature, IInternFactory internFactory) {
      this.unspecializedParameter = unspecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    protected internal SpecializedParameterDefinition(IParameterDefinition unspecializedParameter, SpecializedMethodDefinition containingSignature, IInternFactory internFactory) {
      this.unspecializedParameter = unspecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    protected internal SpecializedParameterDefinition(IParameterDefinition unspecializedParameter, SpecializedPropertyDefinition containingSignature, IInternFactory internFactory) {
      this.unspecializedParameter = unspecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    public ISignature ContainingSignature {
      get
        //^ ensures result is IGenericMethodInstance || result is SpecializedMethodDefinition || result is SpecializedPropertyDefinition;
      { 
        return this.containingSignature; 
      }
    }
    readonly ISignature containingSignature;
    //^ invariant containingSignature is IGenericMethodInstance || containingSignature is SpecializedMethodDefinition || containingSignature is SpecializedPropertyDefinition;

    private IParameterDefinition unspecializedParameter;

    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference ParamArrayElementType {
      get
        //^^ requires this.IsParameterArray;
      {
        //^ assume this.unspecializedParameter.IsParameterArray; //postcondition of this.IsParameterArray
        ITypeReference unspecializedType = this.unspecializedParameter.ParamArrayElementType;
        return this.SpecializeIfConstructed(unspecializedType);
      }
    }

    //^ [Confined]
    private ITypeReference SpecializeIfConstructed(ITypeReference unspecializedType) {
      IGenericMethodInstanceReference/*?*/ genericMethodInstance = this.ContainingSignature as IGenericMethodInstanceReference;
      if (genericMethodInstance != null) return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, genericMethodInstance, this.InternFactory);
      SpecializedMethodDefinition/*?*/ specializedMethodDefinition = this.ContainingSignature as SpecializedMethodDefinition;
      if (specializedMethodDefinition != null) return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, specializedMethodDefinition.ContainingGenericTypeInstance, this.InternFactory);
      SpecializedPropertyDefinition specializedPropertyDefinition = (SpecializedPropertyDefinition)this.ContainingSignature;
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, specializedPropertyDefinition.ContainingGenericTypeInstance, this.InternFactory);
    }

    public ITypeReference Type {
      get {
        ITypeReference unspecializedType = this.unspecializedParameter.Type;
        ITypeReference specializedType = this.SpecializeIfConstructed(unspecializedType);
        if (unspecializedType == specializedType)
          return this.unspecializedParameter.Type;
        else
          return specializedType;
      }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.unspecializedParameter.CustomModifiers; }
    }

    public IMetadataConstant DefaultValue {
      get
        //^^ requires this.HasDefaultValue;
      {
        return this.unspecializedParameter.DefaultValue;
      }
    }

    public bool HasDefaultValue {
      get { return this.unspecializedParameter.HasDefaultValue; }
    }

    public ushort Index {
      get { return this.unspecializedParameter.Index; }
    }

    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    public bool IsByReference {
      get { return this.unspecializedParameter.IsByReference; }
    }

    public bool IsIn {
      get { return this.unspecializedParameter.IsIn; }
    }

    public bool IsMarshalledExplicitly {
      get { return this.unspecializedParameter.IsMarshalledExplicitly; }
    }

    public bool IsModified {
      get { return this.unspecializedParameter.IsModified; }
    }

    public bool IsOptional {
      get { return this.unspecializedParameter.IsOptional; }
    }

    public bool IsOut {
      get { return this.unspecializedParameter.IsOut; }
    }

    public IMarshallingInformation MarshallingInformation {
      get { return this.unspecializedParameter.MarshallingInformation; }
    }

    public bool IsParameterArray {
      get
        //^ ensures result == this.unspecializedParameter.IsParameterArray;
      { 
        return this.unspecializedParameter.IsParameterArray; 
      }
    }

    #region IParameterDefinition Members

    IMetadataConstant IParameterDefinition.DefaultValue {
      get { return this.DefaultValue; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { return this.unspecializedParameter.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.unspecializedParameter.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return this.unspecializedParameter.Locations; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  public class SpecializedParameterTypeInformation : IParameterTypeInformation {

    protected internal SpecializedParameterTypeInformation(IParameterTypeInformation unspecializedParameter, IGenericMethodInstanceReference containingSignature, IInternFactory internFactory) {
      this.unspecializedParameter = unspecializedParameter;
      this.containingSignature = containingSignature;
      this.internFactory = internFactory;
    }

    public ISignature ContainingSignature {
      get
        //^ ensures result is IGenericMethodInstance || result is SpecializedMethodDefinition || result is SpecializedPropertyDefinition;
      {
        return this.containingSignature;
      }
    }
    readonly ISignature containingSignature;
    //^ invariant containingSignature is IGenericMethodInstance || containingSignature is SpecializedMethodDefinition || containingSignature is SpecializedPropertyDefinition;

    private IParameterTypeInformation unspecializedParameter;

    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    //^ [Confined]
    private ITypeReference SpecializeIfConstructed(ITypeReference unspecializedType) {
      IGenericMethodInstance/*?*/ genericMethodInstance = this.ContainingSignature as IGenericMethodInstance;
      if (genericMethodInstance != null) return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, genericMethodInstance, this.InternFactory);
      SpecializedMethodDefinition/*?*/ specializedMethodDefinition = this.ContainingSignature as SpecializedMethodDefinition;
      if (specializedMethodDefinition != null) return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, specializedMethodDefinition.ContainingGenericTypeInstance, this.InternFactory);
      SpecializedPropertyDefinition specializedPropertyDefinition = (SpecializedPropertyDefinition)this.ContainingSignature;
      return TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, specializedPropertyDefinition.ContainingGenericTypeInstance, this.InternFactory);
    }

    public ITypeReference Type {
      get {
        ITypeReference unspecializedType = this.unspecializedParameter.Type;
        ITypeReference specializedType = this.SpecializeIfConstructed(unspecializedType);
        if (unspecializedType == specializedType)
          return this.unspecializedParameter.Type;
        else
          return specializedType;
      }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.unspecializedParameter.CustomModifiers; }
    }

    public ushort Index {
      get { return this.unspecializedParameter.Index; }
    }

    public IInternFactory InternFactory {
      get { return this.internFactory; }
    }
    readonly IInternFactory internFactory;

    public bool IsByReference {
      get { return this.unspecializedParameter.IsByReference; }
    }

    public bool IsModified {
      get { return this.unspecializedParameter.IsModified; }
    }


  }

  public class SpecializedPropertyDefinition : SpecializedTypeDefinitionMember<IPropertyDefinition>, ISpecializedPropertyDefinition {

    public SpecializedPropertyDefinition(IPropertyDefinition unspecializedVersion, GenericTypeInstance containingGenericTypeInstance) 
      : base(unspecializedVersion, containingGenericTypeInstance){
    }

    public IEnumerable<IMethodReference> Accessors {
      get {
        foreach (IMethodReference accessor in this.UnspecializedVersion.Accessors) {
          ITypeDefinitionMember result = this.ContainingGenericTypeInstance.SpecializeMember(accessor.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
          yield return (IMethodReference)result;
        }
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IPropertyDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    private IMethodReference/*?*/ getter;
    public IMethodReference/*?*/ Getter {
      get {
        if (this.getter != null) return this.getter;
        IMethodReference/*?*/ getter = this.UnspecializedVersion.Getter;
        if (getter == null) return null;
        ITypeDefinitionMember result = this.ContainingGenericTypeInstance.SpecializeMember(getter.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
        this.getter = (IMethodReference)result;
        return this.getter;
      }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get {
        foreach (IParameterDefinition parameter in this.UnspecializedVersion.Parameters)
          yield return new SpecializedParameterDefinition(parameter, this, this.ContainingGenericTypeInstance.InternFactory);
      }
    }

    public IMethodReference/*?*/ Setter {
      get {
        IMethodReference/*?*/ setter = this.UnspecializedVersion.Setter;
        if (setter == null) return null;
        ITypeDefinitionMember result = this.ContainingGenericTypeInstance.SpecializeMember(setter.ResolvedMethod, this.ContainingGenericTypeInstance.InternFactory);
        return (IMethodReference)result;
      }
    }

    public ITypeReference Type {
      get {
        ITypeReference unspecializedType = this.UnspecializedVersion.Type;
        ITypeReference specializedType = TypeDefinition.SpecializeIfConstructedFromApplicableTypeParameter(unspecializedType, this.ContainingGenericTypeInstance, this.ContainingGenericTypeInstance.InternFactory);
        if (unspecializedType == specializedType)
          return this.UnspecializedVersion.Type;
        else
          return specializedType;
      }
    }

    public IMetadataConstant DefaultValue {
      get { return this.UnspecializedVersion.DefaultValue; }
    }

    public bool HasDefaultValue {
      get { return this.UnspecializedVersion.HasDefaultValue; }
    }

    /// <summary>
    /// True if the property gets special treatment from the runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return this.UnspecializedVersion.IsRuntimeSpecial; }
    }

    public bool IsSpecialName {
      get { return this.UnspecializedVersion.IsSpecialName; }
    }

    #region IPropertyDefinition Members

    IMetadataConstant IPropertyDefinition.DefaultValue {
      get { return this.DefaultValue; }
    }

    #endregion

    #region ISignature Members

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.UnspecializedVersion.ReturnValueAttributes; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.UnspecializedVersion.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.UnspecializedVersion.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.UnspecializedVersion.ReturnValueIsModified; }
    }

    public CallingConvention CallingConvention {
      get { return this.UnspecializedVersion.CallingConvention; }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  public abstract class SpecializedTypeDefinitionMember<MemberType> : ITypeDefinitionMember
    where MemberType : ITypeDefinitionMember  //  This constraint should also require the MemberType to be unspecialized.
  {

    protected SpecializedTypeDefinitionMember(MemberType/*!*/ unspecializedVersion, GenericTypeInstance containingGenericTypeInstance) {
      this.unspecializedVersion = unspecializedVersion;
      this.containingGenericTypeInstance = containingGenericTypeInstance;
    }

    public GenericTypeInstance ContainingGenericTypeInstance {
      get { return this.containingGenericTypeInstance; }
    }
    readonly GenericTypeInstance containingGenericTypeInstance;

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    public TypeMemberVisibility Visibility {
      get {
        ITypeDefinitionMember unspecializedVersion = this.UnspecializedVersion;
        //^ assume unspecializedVersion != null; //The type system guarantees this
        return TypeHelper.VisibilityIntersection(unspecializedVersion.Visibility, 
          TypeHelper.TypeVisibilityAsTypeMemberVisibility(this.ContainingGenericTypeInstance));
      }
    }

    public MemberType/*!*/ UnspecializedVersion {
      get { return this.unspecializedVersion; }
    }
    readonly MemberType/*!*/ unspecializedVersion;

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get
        //^ ensures result == this.ContainingGenericTypeInstance;
      { 
        return this.ContainingGenericTypeInstance; 
      }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
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

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.UnspecializedVersion.Attributes; }
    }

    public IEnumerable<ILocation> Locations {
      get { return this.UnspecializedVersion.Locations; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

  }

}

#pragma warning restore 1591
