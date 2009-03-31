//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  public sealed class EventDefinition : TypeDefinitionMember, IEventDefinition, ICopyFrom<IEventDefinition> {

    public EventDefinition() {
      this.accessors = new List<IMethodReference>();
      this.adder = Dummy.MethodReference;
      this.caller = null;
      this.remover = Dummy.MethodReference;
      this.type = Dummy.TypeReference;
    }

    public void Copy(IEventDefinition eventDefinition, IInternFactory internFactory) {
      ((ICopyFrom<ITypeDefinitionMember>)this).Copy(eventDefinition, internFactory);
      this.accessors = new List<IMethodReference>(eventDefinition.Accessors);
      this.adder = eventDefinition.Adder;
      this.caller = eventDefinition.Caller;
      this.IsRuntimeSpecial = eventDefinition.IsRuntimeSpecial;
      this.IsSpecialName = eventDefinition.IsSpecialName;
      this.remover = eventDefinition.Remover;
      this.type = eventDefinition.Type;
    }

    public List<IMethodReference> Accessors {
      get { return this.accessors; }
      set { this.accessors = value; }
    }
    List<IMethodReference> accessors;

    public IMethodReference Adder {
      get { return this.adder; }
      set { this.adder = value; }
    }
    IMethodReference adder;

    public IMethodReference/*?*/ Caller {
      get { return this.caller; }
      set { this.caller = value; }
    }
    IMethodReference/*?*/ caller;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public bool IsRuntimeSpecial {
      get { return (this.flags & 0x40000000) != 0; }
      set {
        if (value)
          this.flags |= 0x40000000;
        else
          this.flags &= ~0x40000000;
      }
    }

    public bool IsSpecialName {
      get { return (this.flags & 0x20000000) != 0; }
      set {
        if (value)
          this.flags |= 0x20000000;
        else
          this.flags &= ~0x20000000;
      }
    }

    public IMethodReference Remover {
      get { return this.remover; }
      set { this.remover = value; }
    }
    IMethodReference remover;

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IEventDefinition Members

    IEnumerable<IMethodReference> IEventDefinition.Accessors {
      get { return this.accessors.AsReadOnly(); }
    }

    #endregion
  }

  public class FieldDefinition : TypeDefinitionMember, IFieldDefinition, ICopyFrom<IFieldDefinition> {

    public FieldDefinition() {
      this.compileTimeValue = Dummy.Constant;
      this.fieldMapping = Dummy.SectionBlock;
      this.marshallingInformation = Dummy.MarshallingInformation;
      this.bitLength = -1;
      this.offset = 0;
      this.sequenceNumber = 0;
      this.type = Dummy.TypeReference;
    }

    public void Copy(IFieldDefinition fieldDefinition, IInternFactory internFactory) {
      ((ICopyFrom<ITypeDefinitionMember>)this).Copy(fieldDefinition, internFactory);
      if (fieldDefinition.IsBitField)
        this.bitLength = (int)fieldDefinition.BitLength;
      else
        this.bitLength = -1;
      if (fieldDefinition.IsCompileTimeConstant)
        this.compileTimeValue = fieldDefinition.CompileTimeValue;
      else
        this.compileTimeValue = Dummy.Constant;
      if (fieldDefinition.IsMapped)
        this.fieldMapping = fieldDefinition.FieldMapping;
      else
        this.fieldMapping = Dummy.SectionBlock;
      if (fieldDefinition.IsMarshalledExplicitly)
        this.marshallingInformation = fieldDefinition.MarshallingInformation;
      else
        this.marshallingInformation = Dummy.MarshallingInformation;
      if (fieldDefinition.ContainingTypeDefinition.Layout == LayoutKind.Explicit)
        this.offset = fieldDefinition.Offset;
      else
        this.offset = 0;
      if (fieldDefinition.ContainingTypeDefinition.Layout == LayoutKind.Sequential)
        this.sequenceNumber = fieldDefinition.SequenceNumber;
      else
        this.sequenceNumber = 0;
      this.type = fieldDefinition.Type;
      //^ base;
      this.IsNotSerialized = fieldDefinition.IsNotSerialized;
      this.IsReadOnly = fieldDefinition.IsReadOnly;
      this.IsSpecialName = fieldDefinition.IsSpecialName;
      if (fieldDefinition.IsRuntimeSpecial) {
        //^ assume this.IsSpecialName;
        this.IsRuntimeSpecial = fieldDefinition.IsRuntimeSpecial;
      } else
        this.IsRuntimeSpecial = false;
      this.IsStatic = fieldDefinition.IsStatic;
    }

    public uint BitLength {
      get { return (uint)this.bitLength; }
      set { this.bitLength = (int)value;  }
    }
    int bitLength;

    public IMetadataConstant CompileTimeValue {
      get { return this.compileTimeValue; }
      set { this.compileTimeValue = value; }
    }
    IMetadataConstant compileTimeValue;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ISectionBlock FieldMapping {
      get { return this.fieldMapping; }
      set
        //^ requires this.IsStatic;
      {
        this.fieldMapping = value;
      }
    }
    ISectionBlock fieldMapping;

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    public bool IsBitField {
      get { return this.bitLength >= 0; }
    }

    public bool IsCompileTimeConstant {
      get { return this.compileTimeValue != Dummy.Constant; }
    }

    public bool IsMapped {
      get { return this.fieldMapping != Dummy.SectionBlock; }
    }

    public bool IsMarshalledExplicitly {
      get { return this.marshallingInformation != Dummy.MarshallingInformation; }
    }

    public bool IsNotSerialized {
      get { return (this.flags & 0x40000000) != 0; }
      set {
        if (value)
          this.flags |= 0x40000000;
        else
          this.flags &= ~0x40000000;
      }
    }

    public bool IsReadOnly {
      get { return (this.flags & 0x20000000) != 0; }
      set {
        if (value)
          this.flags |= 0x20000000;
        else
          this.flags &= ~0x20000000;
      }
    }

    public bool IsRuntimeSpecial {
      get { return (this.flags & 0x10000000) != 0; }
      set
        //^ requires !value || this.IsSpecialName;
      {
        if (value)
          this.flags |= 0x10000000;
        else
          this.flags &= ~0x10000000;
      }
    }

    public bool IsSpecialName {
      get { return (this.flags & 0x08000000) != 0; }
      set {
        if (value)
          this.flags |= 0x08000000;
        else
          this.flags &= ~0x08000000;
      }
    }

    public bool IsStatic {
      get { return (this.flags & 0x04000000) != 0; }
      set {
        if (value)
          this.flags |= 0x04000000;
        else
          this.flags &= ~0x04000000;
      }
    }

    public IMarshallingInformation MarshallingInformation {
      get { return this.marshallingInformation; }
      set { this.marshallingInformation = value; }
    }
    IMarshallingInformation marshallingInformation;

    public uint Offset {
      get { return this.offset; }
      set { this.offset = value; }
    }
    uint offset;

    public int SequenceNumber {
      get { return this.sequenceNumber; }
      set { this.sequenceNumber = value; }
    }
    int sequenceNumber;

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

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
  }

  public class FieldReference : IFieldReference, ICopyFrom<IFieldReference> {

    public FieldReference() {
      this.attributes = new List<ICustomAttribute>();
      this.containingType = Dummy.TypeReference;
      this.locations = new List<ILocation>();
      this.name = Dummy.Name;
      this.type = Dummy.TypeReference;
    }

    public void Copy(IFieldReference fieldReference, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(fieldReference.Attributes);
      this.containingType = fieldReference.ContainingType;
      this.locations = new List<ILocation>(fieldReference.Locations);
      this.name = fieldReference.Name;
      this.type = fieldReference.Type;
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public ITypeReference ContainingType {
      get { return this.containingType; }
      set { this.containingType = value; }
    }
    ITypeReference containingType;

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    public IFieldDefinition ResolvedField {
      get { return TypeHelper.GetField(this.ContainingType.ResolvedType, this); }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedField; }
    }

    public override string ToString() {
      return MemberHelper.GetMemberSignature(this, NameFormattingOptions.None);
    }

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;


    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IReference.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class GlobalFieldDefinition : FieldDefinition, IGlobalFieldDefinition, ICopyFrom<IGlobalFieldDefinition> {

    public GlobalFieldDefinition() {
      this.containingNamespace = Dummy.RootUnitNamespace;
    }

    public void Copy(IGlobalFieldDefinition globalFieldDefinition, IInternFactory internFactory) {
      ((ICopyFrom<IFieldDefinition>)this).Copy(globalFieldDefinition, internFactory);
      this.containingNamespace = globalFieldDefinition.ContainingNamespace;
    }

    public INamespaceDefinition ContainingNamespace {
      get { return this.containingNamespace; }
      set { this.containingNamespace = value; }
    }
    INamespaceDefinition containingNamespace;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IContainerMember<INamespaceDefinition> Members

    INamespaceDefinition IContainerMember<INamespaceDefinition>.Container {
      get { return this.ContainingNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    IScope<INamespaceMember> IScopeMember<IScope<INamespaceMember>>.ContainingScope {
      get { return this.ContainingNamespace; }
    }

    #endregion
  }

  public sealed class GlobalMethodDefinition : MethodDefinition, IGlobalMethodDefinition, ICopyFrom<IGlobalMethodDefinition> {

    public GlobalMethodDefinition() {
      this.containingNamespace = Dummy.RootUnitNamespace;
    }

    public void Copy(IGlobalMethodDefinition globalMethodDefinition, IInternFactory internFactory) {
      ((ICopyFrom<IMethodDefinition>)this).Copy(globalMethodDefinition, internFactory);
      this.containingNamespace = globalMethodDefinition.ContainingNamespace;
    }

    public INamespaceDefinition ContainingNamespace {
      get { return this.containingNamespace; }
      set { this.containingNamespace = value; }
    }
    INamespaceDefinition containingNamespace;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }    

    #region IContainerMember<INamespaceDefinition> Members

    INamespaceDefinition IContainerMember<INamespaceDefinition>.Container {
      get { return this.ContainingNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    IScope<INamespaceMember> IScopeMember<IScope<INamespaceMember>>.ContainingScope {
      get { return this.ContainingNamespace; }
    }

    #endregion
  }

  public sealed class GenericMethodInstanceReference : MethodReference, IGenericMethodInstanceReference, ICopyFrom<IGenericMethodInstanceReference> {

    public GenericMethodInstanceReference() {
      this.genericArguments = new List<ITypeReference>();
      this.genericMethod = Dummy.MethodReference;
    }

    public void Copy(IGenericMethodInstanceReference genericMethodInstanceReference, IInternFactory internFactory) {
      ((ICopyFrom<IMethodReference>)this).Copy(genericMethodInstanceReference, internFactory);
      this.genericArguments = new List<ITypeReference>(genericMethodInstanceReference.GenericArguments);
      this.genericMethod = genericMethodInstanceReference.GenericMethod;
    }

    public List<ITypeReference> GenericArguments {
      get { return this.genericArguments; }
      set { this.genericArguments = value; }
    }
    List<ITypeReference> genericArguments;

    public IMethodReference GenericMethod {
      get { return this.genericMethod; }
      set { this.genericMethod = value; }
    }
    IMethodReference genericMethod;

    public override IMethodDefinition ResolvedMethod {
      get { return new GenericMethodInstance(this.GenericMethod.ResolvedMethod, this.GenericArguments.AsReadOnly(), this.InternFactory); }
    }

    #region IGenericMethodInstanceReference Members

    IEnumerable<ITypeReference> IGenericMethodInstanceReference.GenericArguments {
      get { return this.genericArguments.AsReadOnly(); }
    }

    #endregion

  }

  public sealed class GenericMethodParameter : GenericParameter, IGenericMethodParameter, ICopyFrom<IGenericMethodParameter> {

    //^ [NotDelayed]
    public GenericMethodParameter() {
      this.definingMethod = Dummy.Method;
      //^ base;
      //^ assume this.definingMethod.IsGeneric; //TODO: define a dummy generic method
    }

    public void Copy(IGenericMethodParameter genericMehodParameter, IInternFactory internFactory) {
      ((ICopyFrom<IGenericParameter>)this).Copy(genericMehodParameter, internFactory);
      this.definingMethod = genericMehodParameter.DefiningMethod;
    }

    public IMethodDefinition DefiningMethod {
      get { 
        return this.definingMethod; 
      }
      set
        //^ requires value.IsGeneric;
      { 
        this.definingMethod = value; 
      }
    }
    IMethodDefinition definingMethod;
    //^ invariant definingMethod.IsGeneric;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IGenericMethodParameterReference Members

    IMethodReference IGenericMethodParameterReference.DefiningMethod {
      get { return this.DefiningMethod; }
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion

  }

  public class LocalDefinition : ILocalDefinition, ICopyFrom<ILocalDefinition> {

    public LocalDefinition() {
      this.compileTimeValue = Dummy.Constant;
      this.customModifiers = new List<ICustomModifier>();
      this.isModified = false;
      this.isPinned = false;
      this.isReference = false;
      this.locations = new List<ILocation>();
      this.name = Dummy.Name;
      this.type = Dummy.TypeReference;
    }

    public void Copy(ILocalDefinition localVariableDefinition, IInternFactory internFactory) {
      if (localVariableDefinition.IsConstant)
        this.compileTimeValue = localVariableDefinition.CompileTimeValue;
      else
        this.compileTimeValue = Dummy.Constant;
      if (localVariableDefinition.IsModified)
        this.customModifiers = new List<ICustomModifier>(localVariableDefinition.CustomModifiers);
      else
        this.customModifiers = new List<ICustomModifier>();
      this.isModified = localVariableDefinition.IsModified;
      this.isPinned = localVariableDefinition.IsPinned;
      this.isReference = localVariableDefinition.IsReference;
      this.locations = new List<ILocation>(localVariableDefinition.Locations);
      this.name = localVariableDefinition.Name;
      this.type = localVariableDefinition.Type;
    }

    public IMetadataConstant CompileTimeValue {
      get { return this.compileTimeValue; }
      set { this.compileTimeValue = value; }
    }
    IMetadataConstant compileTimeValue;

    public List<ICustomModifier> CustomModifiers {
      get { return this.customModifiers; }
      set { this.customModifiers = value; }
    }
    List<ICustomModifier> customModifiers;

    public bool IsConstant {
      get { return this.compileTimeValue != Dummy.Constant; }
    }

    public bool IsModified {
      get { return this.isModified; }
      set { this.isModified = value; }
    }
    bool isModified;

    public bool IsPinned {
      get { return this.isPinned; }
      set { this.isPinned = value; }
    }
    bool isPinned;

    public bool IsReference {
      get { return this.isReference; }
      set { this.isReference = value; }
    }
    bool isReference;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region ILocalDefinition Members

    IEnumerable<ICustomModifier> ILocalDefinition.CustomModifiers {
      get { return this.customModifiers.AsReadOnly(); }
    }

    IEnumerable<ILocation> ILocalDefinition.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  public class MethodBody : IMethodBody, ICopyFrom<IMethodBody> {

    public MethodBody() {
      this.localsAreZeroed = true;
      this.localVariables = new List<ILocalDefinition>();
      this.maxStack = 0;
      this.methodDefinition = Dummy.Method;
      this.operationExceptionInformation = new List<IOperationExceptionInformation>();
      this.operations = new List<IOperation>();
      this.privateHelperTypes = new List<ITypeDefinition>(0);
    }

    public void Copy(IMethodBody methodBody, IInternFactory internFactory) {
      this.localsAreZeroed = methodBody.LocalsAreZeroed;
      this.localVariables = new List<ILocalDefinition>(methodBody.LocalVariables);
      this.maxStack = methodBody.MaxStack;
      this.methodDefinition = methodBody.MethodDefinition;
      if (!methodBody.MethodDefinition.IsAbstract && !methodBody.MethodDefinition.IsExternal && methodBody.MethodDefinition.IsCil) {
        //^ assume false;
        this.operationExceptionInformation = new List<IOperationExceptionInformation>(methodBody.OperationExceptionInformation);
      } else
        this.operationExceptionInformation = new List<IOperationExceptionInformation>(0);
      if (!methodBody.MethodDefinition.IsAbstract && !methodBody.MethodDefinition.IsExternal && methodBody.MethodDefinition.IsCil) {
        //^ assume false;
        this.operations = new List<IOperation>(methodBody.Operations);
      } else
        this.operations = new List<IOperation>(0);
      this.privateHelperTypes = new List<ITypeDefinition>(0);
    }

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public bool LocalsAreZeroed {
      get { return this.localsAreZeroed; }
      set { this.localsAreZeroed = value; }
    }
    bool localsAreZeroed;

    public List<ILocalDefinition> LocalVariables {
      get { return this.localVariables; }
      set { this.localVariables = value; }
    }
    List<ILocalDefinition> localVariables;

    public ushort MaxStack {
      get { return this.maxStack; }
      set { this.maxStack = value; }
    }
    ushort maxStack;

    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
      set { this.methodDefinition = value; }
    }
    IMethodDefinition methodDefinition;

    public List<IOperation> Operations {
      get { return this.operations; }
      set { this.operations = value; }
    }
    List<IOperation> operations;

    public List<IOperationExceptionInformation> OperationExceptionInformation {
      get { return this.operationExceptionInformation; }
      set { this.operationExceptionInformation = value; }
    }
    List<IOperationExceptionInformation> operationExceptionInformation;

    public List<ITypeDefinition> PrivateHelperTypes {
      get { return this.privateHelperTypes; }
      set { this.privateHelperTypes = value; }
    }
    List<ITypeDefinition> privateHelperTypes;


    #region IMethodBody Members

    IEnumerable<IOperationExceptionInformation> IMethodBody.OperationExceptionInformation {
      get { return this.operationExceptionInformation.AsReadOnly(); }
    }

    IEnumerable<ILocalDefinition> IMethodBody.LocalVariables {
      get { return this.localVariables.AsReadOnly(); }
    }

    IEnumerable<IOperation> IMethodBody.Operations {
      get { return this.operations.AsReadOnly(); }
    }

    IEnumerable<ITypeDefinition> IMethodBody.PrivateHelperTypes {
      get { return this.privateHelperTypes.AsReadOnly(); }
    }

    #endregion
  }

  public class MethodDefinition : TypeDefinitionMember, IMethodDefinition, ICopyFrom<IMethodDefinition> {

    public MethodDefinition() {
      this.body = Dummy.MethodBody;
      this.callingConvention = CallingConvention.Default;
      this.genericParameters = new List<IGenericMethodParameter>();
      this.internFactory = Dummy.InternFactory;
      this.parameters = new List<IParameterDefinition>();
      this.platformInvokeData = Dummy.PlatformInvokeInformation;
      this.returnValueAttributes = new List<ICustomAttribute>();
      this.returnValueCustomModifiers = new List<ICustomModifier>();
      this.returnValueMarshallingInformation = Dummy.MarshallingInformation;
      this.securityAttributes = new List<ISecurityAttribute>();
      this.type = Dummy.TypeReference;
    }

    public void Copy(IMethodDefinition methodDefinition, IInternFactory internFactory) {
      ((ICopyFrom<ITypeDefinitionMember>)this).Copy(methodDefinition, internFactory);
      if (!methodDefinition.IsAbstract && !methodDefinition.IsExternal)
        this.body = methodDefinition.Body;
      else
        this.body = Dummy.MethodBody;
      this.callingConvention = methodDefinition.CallingConvention;
      if (methodDefinition.IsGeneric)
        this.genericParameters = new List<IGenericMethodParameter>(methodDefinition.GenericParameters);
      else
        this.genericParameters = new List<IGenericMethodParameter>(0);
      this.internFactory = internFactory;
      this.parameters = new List<IParameterDefinition>(methodDefinition.Parameters);
      if (methodDefinition.IsPlatformInvoke)
        this.platformInvokeData = methodDefinition.PlatformInvokeData;
      else
        this.platformInvokeData = Dummy.PlatformInvokeInformation;
      this.returnValueAttributes = new List<ICustomAttribute>(methodDefinition.ReturnValueAttributes);
      if (methodDefinition.ReturnValueIsModified)
        this.returnValueCustomModifiers = new List<ICustomModifier>(methodDefinition.ReturnValueCustomModifiers);
      else
        this.returnValueCustomModifiers = new List<ICustomModifier>(0);
      if (methodDefinition.ReturnValueIsMarshalledExplicitly)
        this.returnValueMarshallingInformation = methodDefinition.ReturnValueMarshallingInformation;
      else
        this.returnValueMarshallingInformation = Dummy.MarshallingInformation;
      this.securityAttributes = new List<ISecurityAttribute>(methodDefinition.SecurityAttributes);
      this.type = methodDefinition.Type;
      //^ base;
      this.AcceptsExtraArguments = methodDefinition.AcceptsExtraArguments;
      this.HasDeclarativeSecurity = methodDefinition.HasDeclarativeSecurity;
      this.HasExplicitThisParameter = methodDefinition.HasExplicitThisParameter;
      this.IsAbstract = methodDefinition.IsAbstract;
      this.IsAccessCheckedOnOverride = methodDefinition.IsAccessCheckedOnOverride;
      this.IsCil = methodDefinition.IsCil;
      this.IsExternal = methodDefinition.IsExternal;
      this.IsForwardReference = methodDefinition.IsForwardReference;
      this.IsHiddenBySignature = methodDefinition.IsHiddenBySignature;
      this.IsNativeCode = methodDefinition.IsNativeCode;
      this.IsNewSlot = methodDefinition.IsNewSlot;
      this.IsNeverInlined = methodDefinition.IsNeverInlined;
      this.IsNeverOptimized = methodDefinition.IsNeverOptimized;
      this.IsPlatformInvoke = methodDefinition.IsPlatformInvoke;
      this.IsRuntimeImplemented = methodDefinition.IsRuntimeImplemented;
      this.IsRuntimeInternal = methodDefinition.IsRuntimeInternal;
      this.IsRuntimeSpecial = methodDefinition.IsRuntimeSpecial;
      this.IsSealed = methodDefinition.IsSealed;
      this.IsSpecialName = methodDefinition.IsSpecialName;
      this.IsStatic = methodDefinition.IsStatic;
      this.IsSynchronized = methodDefinition.IsSynchronized;
      this.IsUnmanaged = methodDefinition.IsUnmanaged;
      if (this.IsStatic)
        this.IsVirtual = false;
      else
        this.IsVirtual = methodDefinition.IsVirtual;
      this.PreserveSignature = methodDefinition.PreserveSignature;
      this.RequiresSecurityObject = methodDefinition.RequiresSecurityObject;
      this.ReturnValueIsByRef = methodDefinition.ReturnValueIsByRef;
      this.ReturnValueIsMarshalledExplicitly = methodDefinition.ReturnValueIsMarshalledExplicitly;
    }

    public bool AcceptsExtraArguments {
      get { return (this.flags & 0x40000000) != 0; }
      set {
        if (value)
          this.flags |= 0x40000000;
        else
          this.flags &= ~0x40000000;
      }
    }

    public IMethodBody Body {
      get { return this.body; }
      set { this.body = value; }
    }
    IMethodBody body;

    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
      set { this.callingConvention = value; }
    }
    CallingConvention callingConvention;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public List<IGenericMethodParameter> GenericParameters {
      get { return this.genericParameters; }
      set { this.genericParameters = value; }
    }
    List<IGenericMethodParameter> genericParameters;

    //^ [Pure]
    public ushort GenericParameterCount {
      get { return (ushort)this.genericParameters.Count; }
    }

    public bool HasDeclarativeSecurity {
      get { return (this.flags & 0x20000000) != 0; }
      set {
        if (value)
          this.flags |= 0x20000000;
        else
          this.flags &= ~0x20000000;
      }
    }

    public bool HasExplicitThisParameter {
      get { return (this.flags & 0x10000000) != 0; }
      set {
        if (value)
          this.flags |= 0x10000000;
        else
          this.flags &= ~0x10000000;
      }
    }

    public IInternFactory InternFactory {
      get { return this.internFactory; }
      set { this.internFactory = value; }
    }
    IInternFactory internFactory;

    public uint InternedKey {
      get { return this.internFactory.GetMethodInternedKey(this); }
    }

    public bool IsAbstract {
      get { return (this.flags & 0x08000000) != 0; }
      set {
        if (value)
          this.flags |= 0x08000000;
        else
          this.flags &= ~0x08000000;
      }
    }

    public bool IsAccessCheckedOnOverride {
      get { return (this.flags & 0x04000000) != 0; }
      set {
        if (value)
          this.flags |= 0x04000000;
        else
          this.flags &= ~0x04000000;
      }
    }

    public bool IsCil {
      get { return (this.flags & 0x02000000) != 0; }
      set {
        if (value)
          this.flags |= 0x02000000;
        else
          this.flags &= ~0x02000000;
      }
    }

    public bool IsConstructor {
      get { return this.IsSpecialName && this.Name.Value.Equals(".ctor"); }
    }

    public bool IsExternal {
      get { return (this.flags & 0x00800000) != 0; }
      set {
        if (value)
          this.flags |= 0x00800000;
        else
          this.flags &= ~0x00800000;
      }
    }

    public bool IsForwardReference {
      get { return (this.flags & 0x00400000) != 0; }
      set {
        if (value)
          this.flags |= 0x00400000;
        else
          this.flags &= ~0x00400000;
      }
    }

    public bool IsGeneric {
      get { return this.genericParameters.Count > 0; }
    }

    public bool IsHiddenBySignature {
      get { return (this.flags & 0x00200000) != 0; }
      set {
        if (value)
          this.flags |= 0x00200000;
        else
          this.flags &= ~0x00200000;
      }
    }

    public bool IsNativeCode {
      get { return (this.flags & 0x00100000) != 0; }
      set {
        if (value)
          this.flags |= 0x00100000;
        else
          this.flags &= ~0x00100000;
      }
    }

    public bool IsNewSlot {
      get { return (this.flags & 0x00080000) != 0; }
      set {
        if (value)
          this.flags |= 0x00080000;
        else
          this.flags &= ~0x00080000;
      }
    }

    public bool IsNeverInlined {
      get { return (this.flags & 0x00040000) != 0; }
      set {
        if (value)
          this.flags |= 0x00040000;
        else
          this.flags &= ~0x00040000;
      }
    }

    public bool IsNeverOptimized {
      get { return (this.flags & 0x00020000) != 0; }
      set {
        if (value)
          this.flags |= 0x00020000;
        else
          this.flags &= ~0x00020000;
      }
    }

    public bool IsPlatformInvoke {
      get { return (this.flags & 0x00010000) != 0; }
      set {
        if (value)
          this.flags |= 0x00010000;
        else
          this.flags &= ~0x00010000;
      }
    }

    public bool IsRuntimeImplemented {
      get { return (this.flags & 0x00008000) != 0; }
      set {
        if (value)
          this.flags |= 0x00008000;
        else
          this.flags &= ~0x00008000;
      }
    }

    public bool IsRuntimeInternal {
      get { return (this.flags & 0x00004000) != 0; }
      set {
        if (value)
          this.flags |= 0x00004000;
        else
          this.flags &= ~0x00004000;
      }
    }

    public bool IsRuntimeSpecial {
      get { return (this.flags & 0x00002000) != 0; }
      set {
        if (value)
          this.flags |= 0x00002000;
        else
          this.flags &= ~0x00002000;
      }
    }

    public bool IsSealed {
      get { return (this.flags & 0x00001000) != 0; }
      set {
        if (value)
          this.flags |= 0x00001000;
        else
          this.flags &= ~0x00001000;
      }
    }

    public bool IsSpecialName {
      get { return (this.flags & 0x00000800) != 0; }
      set {
        if (value)
          this.flags |= 0x00000800;
        else
          this.flags &= ~0x00000800;
      }
    }

    public bool IsStatic {
      get { return (this.flags & 0x00000400) != 0; }
      set {
        if (value)
          this.flags |= 0x00000400;
        else
          this.flags &= ~0x00000400;
      }
    }

    public bool IsStaticConstructor {
      get { return this.IsSpecialName && this.Name.Value.Equals(".cctor"); } 
    }

    public bool IsSynchronized {
      get { return (this.flags & 0x00000200) != 0; }
      set {
        if (value)
          this.flags |= 0x00000200;
        else
          this.flags &= ~0x00000200;
      }
    }

    public bool IsUnmanaged {
      get { return (this.flags & 0x00000100) != 0; }
      set {
        if (value)
          this.flags |= 0x00000100;
        else
          this.flags &= ~0x00000100;
      }
    }

    public bool IsVirtual {
      get { 
        //^ assume !this.IsStatic;
        return (this.flags & 0x00000080) != 0; 
      }
      set 
        //^ requires value ==> !this.IsStatic;
      {
        if (value)
          this.flags |= 0x00000080;
        else
          this.flags &= ~0x00000080;
      }
    }

    public List<IParameterDefinition> Parameters {
      get { return this.parameters; }
      set { this.parameters = value; }
    }
    List<IParameterDefinition> parameters;

    public ushort ParameterCount {
      get { return (ushort)this.parameters.Count; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.platformInvokeData; }
      set { this.platformInvokeData = value; }
    }
    IPlatformInvokeInformation platformInvokeData;

    public bool PreserveSignature {
      get { return (this.flags & 0x00000040) != 0; }
      set {
        if (value)
          this.flags |= 0x00000040;
        else
          this.flags &= ~0x00000040;
      }
    }

    public bool RequiresSecurityObject {
      get { return (this.flags & 0x00000020) != 0; }
      set {
        if (value)
          this.flags |= 0x00000020;
        else
          this.flags &= ~0x00000020;
      }
    }

    public List<ICustomAttribute> ReturnValueAttributes {
      get { return this.returnValueAttributes; }
      set { this.returnValueAttributes = value; }
    }
    List<ICustomAttribute> returnValueAttributes;

    public List<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers; }
      set { this.returnValueCustomModifiers = value; }
    }
    List<ICustomModifier> returnValueCustomModifiers;

    public bool ReturnValueIsByRef {
      get { return (this.flags & 0x00000010) != 0; }
      set {
        if (value)
          this.flags |= 0x00000010;
        else
          this.flags &= ~0x00000010;
      }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return (this.flags & int.MinValue) != 0; }
      set {
        if (value)
          this.flags |= int.MinValue;
        else
          this.flags &= ~int.MinValue;
      }
    }

    public bool ReturnValueIsModified {
      get { return this.returnValueCustomModifiers.Count > 0; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.returnValueMarshallingInformation; }
      set { this.returnValueMarshallingInformation = value; }
    }
    IMarshallingInformation returnValueMarshallingInformation;

    public List<ISecurityAttribute> SecurityAttributes {
      get { return this.securityAttributes; }
      set { this.securityAttributes = value; }
    }
    List<ISecurityAttribute> securityAttributes;

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IMethodDefinition Members

    IEnumerable<IGenericMethodParameter> IMethodDefinition.GenericParameters {
      get { return this.genericParameters.AsReadOnly(); }
    }

    IEnumerable<ISecurityAttribute> IMethodDefinition.SecurityAttributes {
      get { return this.securityAttributes.AsReadOnly(); }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.parameters); }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers.AsReadOnly(); }
    }

    #endregion

    #region IMethodDefinition Members

    IEnumerable<IParameterDefinition> IMethodDefinition.Parameters {
      get { return this.parameters.AsReadOnly(); }
    }

    IEnumerable<ICustomAttribute> IMethodDefinition.ReturnValueAttributes {
      get { return this.returnValueAttributes.AsReadOnly(); }
    }

    #endregion

    #region IMethodReference Members

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

  }

  public class MethodReference : IMethodReference, ICopyFrom<IMethodReference> {

    public MethodReference() {
      this.attributes = new List<ICustomAttribute>();
      this.callingConvention = (CallingConvention)0;
      this.containingType = Dummy.TypeReference;
      this.extraParameters = new List<IParameterTypeInformation>();
      this.genericParameterCount = 0;
      this.internFactory = Dummy.InternFactory;
      this.isGeneric = false;
      this.locations = new List<ILocation>();
      this.name = Dummy.Name;
      this.parameters = new List<IParameterTypeInformation>();
      this.returnValueCustomModifiers = new List<ICustomModifier>();
      this.returnValueIsByRef = false;
      this.returnValueIsModified = false;
      this.type = Dummy.TypeReference;
    }

    public void Copy(IMethodReference methodReference, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(methodReference.Attributes);
      this.callingConvention = methodReference.CallingConvention;
      this.containingType = methodReference.ContainingType;
      this.extraParameters = new List<IParameterTypeInformation>(methodReference.ExtraParameters);
      this.genericParameterCount = methodReference.GenericParameterCount;
      this.internFactory = internFactory;
      this.isGeneric = methodReference.IsGeneric;
      this.locations = new List<ILocation>(methodReference.Locations);
      this.name = methodReference.Name;
      this.parameters = new List<IParameterTypeInformation>(methodReference.Parameters);
      if (methodReference.ReturnValueIsModified)
        this.returnValueCustomModifiers = new List<ICustomModifier>(methodReference.ReturnValueCustomModifiers);
      else
        this.returnValueCustomModifiers = new List<ICustomModifier>();
      this.returnValueIsByRef = methodReference.ReturnValueIsByRef;
      this.returnValueIsModified = methodReference.ReturnValueIsModified;
      this.type = methodReference.Type;
    }

    public bool AcceptsExtraArguments {
      get { return (this.callingConvention & (CallingConvention)0x7) == CallingConvention.ExtraArguments; }
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
      set { this.callingConvention = value; }
    }
    CallingConvention callingConvention;

    public ITypeReference ContainingType {
      get { return this.containingType; }
      set { this.containingType = value; }
    }
    ITypeReference containingType;

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public List<IParameterTypeInformation> ExtraParameters {
      get { return this.extraParameters; }
      set { this.extraParameters = value; }
    }
    List<IParameterTypeInformation> extraParameters;

    public ushort GenericParameterCount {
      get { return this.genericParameterCount; }
      set { this.genericParameterCount = value; }
    }
    ushort genericParameterCount;

    public IInternFactory InternFactory {
      get { return this.internFactory; }
      set { this.internFactory = value; }
    }
    IInternFactory internFactory;

    public uint InternedKey {
      get { return this.internFactory.GetMethodInternedKey(this); }
    }

    public bool IsGeneric {
      get { return this.isGeneric; }
      set { this.isGeneric = value; }
    }
    bool isGeneric;

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    public List<IParameterTypeInformation> Parameters {
      get { return this.parameters; }
      set { this.parameters = value; }
    }
    List<IParameterTypeInformation> parameters;

    public ushort ParameterCount {
      get { return (ushort)this.Parameters.Count; }
    }

    public virtual IMethodDefinition ResolvedMethod {
      get { return TypeHelper.GetMethod(this.ContainingType.ResolvedType, this); }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

    public List<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers; }
      set { this.returnValueCustomModifiers = value; }
    }
    List<ICustomModifier> returnValueCustomModifiers;

    public bool ReturnValueIsByRef {
      get { return this.returnValueIsByRef; }
      set { this.returnValueIsByRef = value; }
    }
    bool returnValueIsByRef;

    public bool ReturnValueIsModified {
      get { return this.returnValueIsModified; }
      set { this.returnValueIsModified = value; }
    }
    bool returnValueIsModified;

    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, 
        NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName);
    }

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;


    #region IMethodReference Members


    IEnumerable<IParameterTypeInformation> IMethodReference.ExtraParameters {
      get { return this.extraParameters.AsReadOnly(); }
    }

    #endregion

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return this.parameters.AsReadOnly(); }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers.AsReadOnly(); }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IReference.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class ParameterDefinition : IParameterDefinition, ICopyFrom<IParameterDefinition> {

    public ParameterDefinition() {
      this.attributes = new List<ICustomAttribute>();
      this.containingSignature = Dummy.Method;
      this.customModifiers = new List<ICustomModifier>();
      this.defaultValue = Dummy.Constant;
      this.index = 0;
      this.locations = new List<ILocation>();
      this.marshallingInformation = Dummy.MarshallingInformation;
      this.paramArrayElementType = Dummy.Type;
      this.name = Dummy.Name;
      this.type = Dummy.TypeReference;
    }

    public void Copy(IParameterDefinition parameterDefinition, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(parameterDefinition.Attributes);
      this.containingSignature = parameterDefinition.ContainingSignature;
      if (parameterDefinition.IsModified)
        this.customModifiers = new List<ICustomModifier>(parameterDefinition.CustomModifiers);
      else
        this.customModifiers = new List<ICustomModifier>(0);
      if (parameterDefinition.HasDefaultValue)
        this.defaultValue = parameterDefinition.DefaultValue;
      else
        this.defaultValue = Dummy.Constant;
      this.index = parameterDefinition.Index;
      this.locations = new List<ILocation>(parameterDefinition.Locations);
      if (parameterDefinition.IsMarshalledExplicitly)
        this.marshallingInformation = parameterDefinition.MarshallingInformation;
      else
        this.marshallingInformation = Dummy.MarshallingInformation;
      if (parameterDefinition.IsParameterArray)
        this.paramArrayElementType = parameterDefinition.ParamArrayElementType;
      else
        this.paramArrayElementType = Dummy.Type;
      this.name = parameterDefinition.Name;
      this.type = parameterDefinition.Type;
      this.IsByReference = parameterDefinition.IsByReference;
      this.IsIn = parameterDefinition.IsIn;
      this.IsOptional = parameterDefinition.IsOptional;
      this.IsOut = parameterDefinition.IsOut;
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public ISignature ContainingSignature {
      get { return this.containingSignature; }
      set { this.containingSignature = value; }
    }
    ISignature containingSignature;

    public List<ICustomModifier> CustomModifiers {
      get { return this.customModifiers; }
      set { this.customModifiers = value; }
    }
    List<ICustomModifier> customModifiers;

    public IMetadataConstant DefaultValue {
      get { return this.defaultValue; }
      set { this.defaultValue = value; }
    }
    IMetadataConstant defaultValue;

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    private int flags;

    public bool HasDefaultValue {
      get { return this.defaultValue != Dummy.Constant; }
    }

    public ushort Index {
      get { return this.index; }
      set { this.index = value; }
    }
    ushort index;

    public bool IsByReference {
      get { return (this.flags & 0x40000000) != 0; }
      set {
        if (value)
          this.flags |= 0x40000000;
        else
          this.flags &= ~0x40000000;
      }
    }

    public bool IsIn {
      get { return (this.flags & 0x20000000) != 0; }
      set {
        if (value)
          this.flags |= 0x20000000;
        else
          this.flags &= ~0x20000000;
      }
    }

    public bool IsMarshalledExplicitly {
      get { return this.marshallingInformation != Dummy.MarshallingInformation; }
    }

    public bool IsModified {
      get { return this.customModifiers.Count > 0; }
    }

    public bool IsOptional {
      get { return (this.flags & 0x10000000) != 0; }
      set {
        if (value)
          this.flags |= 0x10000000;
        else
          this.flags &= ~0x10000000;
      }
    }

    public bool IsOut {
      get { return (this.flags & 0x08000000) != 0; }
      set {
        if (value)
          this.flags |= 0x08000000;
        else
          this.flags &= ~0x08000000;
      }
    }

    public bool IsParameterArray {
      get { return this.paramArrayElementType != Dummy.Type; }
    }

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public IMarshallingInformation MarshallingInformation {
      get { return this.marshallingInformation; }
      set { this.marshallingInformation = value; }
    }
    IMarshallingInformation marshallingInformation;

    public ITypeReference ParamArrayElementType {
      get { return this.paramArrayElementType; }
    }
    ITypeReference paramArrayElementType;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IParameterDefinition Members

    IEnumerable<ICustomModifier> IParameterTypeInformation.CustomModifiers {
      get { return this.customModifiers.AsReadOnly(); }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IReference.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion
  }

  public sealed class ParameterTypeInformation : IParameterTypeInformation, ICopyFrom<IParameterTypeInformation> {

    public ParameterTypeInformation() {
      this.containingSignature = Dummy.Method;
      this.customModifiers = new List<ICustomModifier>();
      this.index = 0;
      this.isByReference = false;
      this.type = Dummy.TypeReference;
    }

    public void Copy(IParameterTypeInformation parameterTypeInformation, IInternFactory internFactory) {
      this.containingSignature = parameterTypeInformation.ContainingSignature;
      if (parameterTypeInformation.IsModified)
        this.customModifiers = new List<ICustomModifier>(parameterTypeInformation.CustomModifiers);
      else
        this.customModifiers = new List<ICustomModifier>(0);
      this.index = parameterTypeInformation.Index;
      this.isByReference = parameterTypeInformation.IsByReference;
      this.type = parameterTypeInformation.Type;
    }

    public ISignature ContainingSignature {
      get { return this.containingSignature; }
      set { this.containingSignature = value; }
    }
    ISignature containingSignature;

    public List<ICustomModifier> CustomModifiers {
      get { return this.customModifiers; }
      set { this.customModifiers = value; }
    }
    List<ICustomModifier> customModifiers;

    public ushort Index {
      get { return this.index; }
      set { this.index = value; }
    }
    ushort index;

    public bool IsByReference {
      get { return this.isByReference; }
      set { this.isByReference = value; }
    }
    bool isByReference;

    public bool IsModified {
      get { return this.customModifiers.Count > 0; }
    }

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IParameterTypeInformation Members

    IEnumerable<ICustomModifier> IParameterTypeInformation.CustomModifiers {
      get { return this.customModifiers.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class PropertyDefinition : TypeDefinitionMember, IPropertyDefinition, ICopyFrom<IPropertyDefinition> {

    public PropertyDefinition() {
      this.accessors = new List<IMethodReference>();
      this.callingConvention = CallingConvention.Default;
      this.defaultValue = Dummy.Constant;
      this.getter = null;
      this.parameters = new List<IParameterDefinition>();
      this.returnValueAttributes = new List<ICustomAttribute>();
      this.returnValueCustomModifiers = new List<ICustomModifier>();
      this.setter = null;
      this.type = Dummy.TypeReference;
    }

    public void Copy(IPropertyDefinition propertyDefinition, IInternFactory internFactory) {
      ((ICopyFrom<ITypeDefinitionMember>)this).Copy(propertyDefinition, internFactory);
      this.accessors = new List<IMethodReference>(propertyDefinition.Accessors);
      this.callingConvention = propertyDefinition.CallingConvention;
      if (propertyDefinition.HasDefaultValue)
        this.defaultValue = propertyDefinition.DefaultValue;
      else
        this.defaultValue = Dummy.Constant;
      this.getter = propertyDefinition.Getter;
      this.parameters = new List<IParameterDefinition>(propertyDefinition.Parameters);
      this.returnValueAttributes = new List<ICustomAttribute>(propertyDefinition.ReturnValueAttributes);
      if (propertyDefinition.ReturnValueIsModified)
        this.returnValueCustomModifiers = new List<ICustomModifier>(propertyDefinition.ReturnValueCustomModifiers);
      else
        this.returnValueCustomModifiers = new List<ICustomModifier>(0);
      this.setter = propertyDefinition.Setter;
      this.type = propertyDefinition.Type;
      //^ base;
      this.IsRuntimeSpecial = propertyDefinition.IsRuntimeSpecial;
      this.IsSpecialName = propertyDefinition.IsSpecialName;
      this.ReturnValueIsByRef = propertyDefinition.ReturnValueIsByRef;
    }

    public List<IMethodReference> Accessors {
      get { return this.accessors; }
      set { this.accessors = value; }
    }
    List<IMethodReference> accessors;

    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
      set { this.callingConvention = value; }
    }
    CallingConvention callingConvention;

    public IMetadataConstant DefaultValue {
      get { return this.defaultValue; }
      set { this.defaultValue = value; }
    }
    IMetadataConstant defaultValue;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IMethodReference/*?*/ Getter {
      get { return this.getter; }
      set { this.getter = value; }
    }
    IMethodReference/*?*/ getter;

    public bool HasDefaultValue {
      get { return this.defaultValue != Dummy.Constant; }
    }

    public bool IsRuntimeSpecial {
      get { return (this.flags & 0x40000000) != 0; }
      set {
        if (value)
          this.flags |= 0x40000000;
        else
          this.flags &= ~0x40000000;
      }
    }

    public bool IsSpecialName {
      get { return (this.flags & 0x20000000) != 0; }
      set {
        if (value)
          this.flags |= 0x20000000;
        else
          this.flags &= ~0x20000000;
      }
    }

    public List<IParameterDefinition> Parameters {
      get { return this.parameters; }
      set { this.parameters = value; }
    }
    List<IParameterDefinition> parameters;

    public List<ICustomAttribute> ReturnValueAttributes {
      get { return this.returnValueAttributes; }
      set { this.returnValueAttributes = value; }
    }
    List<ICustomAttribute> returnValueAttributes;

    public List<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers; }
      set { this.returnValueCustomModifiers = value; }
    }
    List<ICustomModifier> returnValueCustomModifiers;

    public bool ReturnValueIsByRef {
      get { return (this.flags & 0x10000000) != 0; }
      set {
        if (value)
          this.flags |= 0x10000000;
        else
          this.flags &= ~0x10000000;
      }
    }

    public bool ReturnValueIsModified {
      get { return this.returnValueCustomModifiers.Count > 0; }
    }

    public IMethodReference/*?*/ Setter {
      get { return this.setter; }
      set { this.setter = value; }
    }
    IMethodReference/*?*/ setter;

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region IPropertyDefinition Members

    IEnumerable<IMethodReference> IPropertyDefinition.Accessors {
      get { return this.accessors.AsReadOnly(); }
    }

    IEnumerable<IParameterDefinition> IPropertyDefinition.Parameters {
      get { return this.parameters.AsReadOnly(); }
    }

    IEnumerable<ICustomAttribute> IPropertyDefinition.ReturnValueAttributes {
      get { return this.returnValueAttributes.AsReadOnly(); }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.parameters); }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers.AsReadOnly(); }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion
  }

  public sealed class SignatureDefinition : ISignature, ICopyFrom<ISignature> {

    public SignatureDefinition() {
      this.callingConvention = CallingConvention.Default;
      this.parameters = new List<IParameterTypeInformation>();
      this.returnValueCustomModifiers = new List<ICustomModifier>();
      this.returnValueIsByRef = false;
      this.type = Dummy.TypeReference;
    }

    public void Copy(ISignature signatureDefinition, IInternFactory internFactory) {
      this.callingConvention = signatureDefinition.CallingConvention;
      this.parameters = new List<IParameterTypeInformation>(signatureDefinition.Parameters);
      if (signatureDefinition.ReturnValueIsModified)
        this.returnValueCustomModifiers = new List<ICustomModifier>(signatureDefinition.ReturnValueCustomModifiers);
      else
        this.returnValueCustomModifiers = new List<ICustomModifier>(0);
      this.returnValueIsByRef = signatureDefinition.ReturnValueIsByRef;
      this.type = signatureDefinition.Type;
    }

    public CallingConvention CallingConvention {
      get { return this.callingConvention; }
      set { this.callingConvention = value; }
    }
    CallingConvention callingConvention;

    public List<IParameterTypeInformation> Parameters {
      get { return this.parameters; }
      set { this.parameters = value; }
    }
    List<IParameterTypeInformation> parameters;

    public List<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers; }
      set { this.returnValueCustomModifiers = value; }
    }
    List<ICustomModifier> returnValueCustomModifiers;

    public bool ReturnValueIsByRef {
      get { return this.returnValueIsByRef; }
      set { this.returnValueIsByRef = value; }
    }
    bool returnValueIsByRef;

    public bool ReturnValueIsModified {
      get { return this.returnValueCustomModifiers.Count > 0; }
    }

    public ITypeReference Type {
      get { return this.type; }
      set { this.type = value; }
    }
    ITypeReference type;

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return this.parameters.AsReadOnly(); }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get { return this.returnValueCustomModifiers.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class SpecializedFieldReference : FieldReference, ISpecializedFieldReference, ICopyFrom<ISpecializedFieldReference> {

    public SpecializedFieldReference() {
      this.unspecializedVersion = Dummy.FieldReference;
    }

    public void Copy(ISpecializedFieldReference specializedFieldReference, IInternFactory internFactory) {
      ((ICopyFrom<IFieldReference>)this).Copy(specializedFieldReference, internFactory);
      this.unspecializedVersion = specializedFieldReference.UnspecializedVersion;
    }

    public IFieldReference UnspecializedVersion {
      get { return this.unspecializedVersion; }
      set { this.unspecializedVersion = value; }
    }
    IFieldReference unspecializedVersion;
  }

  public sealed class SpecializedMethodReference : MethodReference, ISpecializedMethodReference, ICopyFrom<ISpecializedMethodReference> {

    public SpecializedMethodReference() {
      this.unspecializedVersion = Dummy.MethodReference;
    }

    public void Copy(ISpecializedMethodReference specializedMethodReference, IInternFactory internFactory) {
      ((ICopyFrom<IMethodReference>)this).Copy(specializedMethodReference, internFactory);
      this.unspecializedVersion = specializedMethodReference.UnspecializedVersion;
    }

    public IMethodReference UnspecializedVersion {
      get { return this.unspecializedVersion; }
      set { this.unspecializedVersion = value; }
    }
    IMethodReference unspecializedVersion;

  }

  public abstract class TypeDefinitionMember : ITypeDefinitionMember, ICopyFrom<ITypeDefinitionMember> {

    internal TypeDefinitionMember() {
      this.attributes = new List<ICustomAttribute>();
      this.containingType = Dummy.TypeReference;
      this.locations = new List<ILocation>();
      this.name = Dummy.Name;
      this.flags = 0;
    }

    public void Copy(ITypeDefinitionMember typeDefinitionMember, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(typeDefinitionMember.Attributes);
      this.containingType = typeDefinitionMember.ContainingType;
      this.locations = new List<ILocation>(typeDefinitionMember.Locations);
      this.name = typeDefinitionMember.Name;
      this.flags = (int)typeDefinitionMember.Visibility;
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public ITypeReference ContainingType {
      get { return this.containingType; }
      set { this.containingType = value; }
    }
    ITypeReference containingType;

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    internal int flags;

    public List<ILocation> Locations {
      get { return this.locations; }
      set { this.locations = value; }
    }
    List<ILocation> locations;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    public override string ToString() {
      return MemberHelper.GetMemberSignature(this, 
        NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName|NameFormattingOptions.ReturnType|NameFormattingOptions.Signature);
    }

    public TypeMemberVisibility Visibility {
      get { return (TypeMemberVisibility)this.flags & TypeMemberVisibility.Mask; }
      set { 
        this.flags &= (int)~TypeMemberVisibility.Mask;
        this.flags |= (int)(value & TypeMemberVisibility.Mask);
      }
    }

    #region ITypeMemberReference Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.ContainingType.ResolvedType; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IReference.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    ITypeDefinition IContainerMember<ITypeDefinition>.Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    IScope<ITypeDefinitionMember> IScopeMember<IScope<ITypeDefinitionMember>>.ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion
  }

}
