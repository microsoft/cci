//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MutableCodeModel {

  public sealed class CustomAttribute : ICustomAttribute, ICopyFrom<ICustomAttribute> {

    public CustomAttribute() {
      this.arguments = new List<IMetadataExpression>();
      this.constructor = Dummy.MethodReference;
      this.namedArguments = new List<IMetadataNamedArgument>();
    }

    public void Copy(ICustomAttribute customAttribute, IInternFactory internFactory) {
      this.arguments = new List<IMetadataExpression>(customAttribute.Arguments);
      this.constructor = customAttribute.Constructor;
      this.namedArguments = new List<IMetadataNamedArgument>(customAttribute.NamedArguments);
    }

    public List<IMetadataExpression> Arguments {
      get { return this.arguments; }
      set { this.arguments = value; }
    }
    List<IMetadataExpression> arguments;

    public IMethodReference Constructor {
      get { return this.constructor; }
      set { this.constructor = value; }
    }
    IMethodReference constructor;

    public List<IMetadataNamedArgument> NamedArguments {
      get { return this.namedArguments; }
      set { this.namedArguments = value; }
    }
    List<IMetadataNamedArgument> namedArguments;

    public ushort NumberOfNamedArguments {
      get { return (ushort)this.namedArguments.Count; }
    }

    public ITypeReference Type {
      get { return this.Constructor.ContainingType; }
    }

    #region ICustomAttribute Members

    IEnumerable<IMetadataExpression> ICustomAttribute.Arguments {
      get { return this.arguments.AsReadOnly(); }
    }

    IEnumerable<IMetadataNamedArgument> ICustomAttribute.NamedArguments {
      get { return this.namedArguments.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class FileReference : IFileReference, ICopyFrom<IFileReference> {

    public FileReference() {
      this.containingAssembly = Dummy.Assembly;
      this.fileName = Dummy.Name;
      this.hashValue = new List<byte>();
      this.hasMetadata = false;
    }

    public void Copy(IFileReference fileReference, IInternFactory internFactory) {
      this.containingAssembly = fileReference.ContainingAssembly;
      this.fileName = fileReference.FileName;
      this.hashValue = new List<byte>(fileReference.HashValue);
      this.hasMetadata = fileReference.HasMetadata;
    }

    public IAssembly ContainingAssembly {
      get { return this.containingAssembly; }
      set { this.containingAssembly = value; }
    }
    IAssembly containingAssembly;

    public IName FileName {
      get { return this.fileName; }
      set { this.fileName = value; }
    }
    IName fileName;

    public List<byte> HashValue {
      get { return this.hashValue; }
      set { this.hashValue = value; }
    }
    List<byte> hashValue;

    public bool HasMetadata {
      get { return this.hasMetadata; }
      set { this.hasMetadata = value; }
    }
    bool hasMetadata;

    #region IFileReference Members

    IEnumerable<byte> IFileReference.HashValue {
      get { return this.hashValue.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class MarshallingInformation : IMarshallingInformation, ICopyFrom<IMarshallingInformation> {

    public MarshallingInformation() {
      this.customMarshaller = Dummy.TypeReference;
      this.customMarshallerRuntimeArgument = "";
      this.elementSize = 0;
      this.elementSizeMultiplier = 0;
      this.elementType = (UnmanagedType)0;
      this.iidParameterIndex = 0;
      this.numberOfElements = 0;
      this.paramIndex = 0;
      this.safeArrayElementSubType = (VarEnum)0;
      this.safeArrayElementUserDefinedSubType = Dummy.TypeReference;
      this.unmanagedType = (UnmanagedType)0;
    }

    public void Copy(IMarshallingInformation marshallingInformation, IInternFactory internFactory) {
      if (marshallingInformation.UnmanagedType == UnmanagedType.CustomMarshaler)
        this.customMarshaller = marshallingInformation.CustomMarshaller;
      else
        this.customMarshaller = Dummy.TypeReference;
      if (marshallingInformation.UnmanagedType == UnmanagedType.CustomMarshaler)
        this.customMarshallerRuntimeArgument = marshallingInformation.CustomMarshallerRuntimeArgument;
      else
        this.customMarshallerRuntimeArgument = "";
      if (marshallingInformation.UnmanagedType == UnmanagedType.LPArray)
        this.elementSize = marshallingInformation.ElementSize;
      else
        this.elementSize = 0;
      if (marshallingInformation.UnmanagedType == UnmanagedType.LPArray && marshallingInformation.ParamIndex != null)
        this.elementSizeMultiplier = marshallingInformation.ElementSizeMultiplier;
      else
        this.elementSizeMultiplier = 0;
      if (marshallingInformation.UnmanagedType == UnmanagedType.ByValArray || marshallingInformation.UnmanagedType == UnmanagedType.LPArray)
        this.elementType = marshallingInformation.ElementType;
      else
        this.elementType = (UnmanagedType)0;
      if (marshallingInformation.UnmanagedType == UnmanagedType.Interface)
        this.iidParameterIndex = marshallingInformation.IidParameterIndex;
      else
        this.iidParameterIndex = 0;
      if (marshallingInformation.UnmanagedType == UnmanagedType.ByValArray || marshallingInformation.UnmanagedType == UnmanagedType.ByValTStr || 
        marshallingInformation.UnmanagedType == UnmanagedType.LPArray)
        this.numberOfElements = marshallingInformation.NumberOfElements;
      else
        this.numberOfElements = 0;
      if (marshallingInformation.UnmanagedType == UnmanagedType.LPArray)
        this.paramIndex = marshallingInformation.ParamIndex;
      else
        this.paramIndex = 0;
      if (marshallingInformation.UnmanagedType == UnmanagedType.SafeArray)
        this.safeArrayElementSubType = marshallingInformation.SafeArrayElementSubType;
      else
        this.safeArrayElementSubType = (VarEnum)0;
      if (marshallingInformation.UnmanagedType == UnmanagedType.SafeArray && 
      (marshallingInformation.SafeArrayElementSubType == VarEnum.VT_DISPATCH || marshallingInformation.SafeArrayElementSubType == VarEnum.VT_UNKNOWN || 
      marshallingInformation.SafeArrayElementSubType == VarEnum.VT_RECORD))
        this.safeArrayElementUserDefinedSubType = marshallingInformation.SafeArrayElementUserDefinedSubType;
      else
        this.safeArrayElementUserDefinedSubType = Dummy.TypeReference;
      this.unmanagedType = marshallingInformation.UnmanagedType;
    }

    public ITypeReference CustomMarshaller {
      get { return this.customMarshaller; }
      set { this.customMarshaller = value; }
    }
    ITypeReference customMarshaller;

    public string CustomMarshallerRuntimeArgument {
      get { return this.customMarshallerRuntimeArgument; }
      set { this.customMarshallerRuntimeArgument = value; }
    }
    string customMarshallerRuntimeArgument;

    public uint ElementSize {
      get { return this.elementSize; }
      set { this.elementSize = value; }
    }
    uint elementSize;

    public uint ElementSizeMultiplier {
      get { return this.elementSizeMultiplier; }
      set { this.elementSizeMultiplier = value; }
    }
    uint elementSizeMultiplier;

    public UnmanagedType ElementType {
      get { return this.elementType; }
      set { this.elementType = value; }
    }
    UnmanagedType elementType;

    public uint IidParameterIndex {
      get { return this.iidParameterIndex; }
      set { this.iidParameterIndex = value; }
    }
    uint iidParameterIndex;

    public UnmanagedType UnmanagedType {
      get { return this.unmanagedType; }
      set { this.unmanagedType = value; }
    }
    UnmanagedType unmanagedType;

    public uint NumberOfElements {
      get { return this.numberOfElements; }
      set { this.numberOfElements = value; }
    }
    uint numberOfElements;

    public uint? ParamIndex {
      get { return this.paramIndex; }
      set { this.paramIndex = value; }
    }
    uint? paramIndex;

    public VarEnum SafeArrayElementSubType {
      get { return this.safeArrayElementSubType; }
      set { this.safeArrayElementSubType = value; }
    }
    VarEnum safeArrayElementSubType;

    public ITypeReference SafeArrayElementUserDefinedSubType {
      get { return this.safeArrayElementUserDefinedSubType; }
      set { this.safeArrayElementUserDefinedSubType = value; }
    }
    ITypeReference safeArrayElementUserDefinedSubType;

  }

  public sealed class Operation : IOperation, ICopyFrom<IOperation> {

    public Operation() {
      this.location = Dummy.Location;
      this.offset = 0;
      this.operationCode = (OperationCode)0;
      this.value = null;
    }

    public void Copy(IOperation operation, IInternFactory internFactory) {
      this.location = operation.Location;
      this.offset = operation.Offset;
      this.operationCode = operation.OperationCode;
      this.value = operation.Value;
    }

    public ILocation Location {
      get { return this.location; }
      set { this.location = value; }
    }
    ILocation location;

    public uint Offset {
      get { return this.offset; }
      set { this.offset = value; }
    }
    uint offset;

    public OperationCode OperationCode {
      get { return this.operationCode; }
      set { this.operationCode = value; }
    }
    OperationCode operationCode;

    public object Value {
      get { return this.value; }
      set { this.value = value; }
    }
    object value;

  }

  public sealed class OperationExceptionInformation : IOperationExceptionInformation, ICopyFrom<IOperationExceptionInformation> {

    public OperationExceptionInformation() {
      this.exceptionType = Dummy.TypeReference;
      this.filterDecisionStartOffset = 0;
      this.handlerEndOffset = 0;
      this.handlerKind = (HandlerKind)0;
      this.handlerStartOffset = 0;
      this.tryEndOffset = 0;
      this.tryStartOffset = 0;
    }

    public void Copy(IOperationExceptionInformation operationExceptionInformation, IInternFactory internFactory) {
      this.exceptionType = operationExceptionInformation.ExceptionType;
      this.filterDecisionStartOffset = operationExceptionInformation.FilterDecisionStartOffset;
      this.handlerEndOffset = operationExceptionInformation.HandlerEndOffset;
      this.handlerKind = operationExceptionInformation.HandlerKind;
      this.handlerStartOffset = operationExceptionInformation.HandlerStartOffset;
      this.tryEndOffset = operationExceptionInformation.TryEndOffset;
      this.tryStartOffset = operationExceptionInformation.TryStartOffset;
    }

    public ITypeReference ExceptionType {
      get { return this.exceptionType; }
      set { this.exceptionType = value; }
    }
    ITypeReference exceptionType;

    public uint FilterDecisionStartOffset {
      get { return this.filterDecisionStartOffset; }
      set { this.filterDecisionStartOffset = value; }
    }
    uint filterDecisionStartOffset;

    public uint HandlerEndOffset {
      get { return this.handlerEndOffset; }
      set { this.handlerEndOffset = value; }
    }
    uint handlerEndOffset;

    public HandlerKind HandlerKind {
      get { return this.handlerKind; }
      set { this.handlerKind = value; }
    }
    HandlerKind handlerKind;

    public uint HandlerStartOffset {
      get { return this.handlerStartOffset; }
      set { this.handlerStartOffset = value; }
    }
    uint handlerStartOffset;

    public uint TryEndOffset {
      get { return this.tryEndOffset; }
      set { this.tryEndOffset = value; }
    }
    uint tryEndOffset;

    public uint TryStartOffset {
      get { return this.tryStartOffset; }
      set { this.tryStartOffset = value; }
    }
    uint tryStartOffset;

  }

  public sealed class PlatformInvokeInformation : IPlatformInvokeInformation, ICopyFrom<IPlatformInvokeInformation> {

    public PlatformInvokeInformation() {
      this.importModule = Dummy.ModuleReference;
      this.importName = Dummy.Name;
      this.noMangle = false;
      this.pinvokeCallingConvention = (PInvokeCallingConvention)0;
      this.stringFormat = StringFormatKind.Unspecified;
      this.supportsLastError = false;
      this.useBestFit = null;
      this.throwExceptionForUnmappableChar = null;
    }

    public void Copy(IPlatformInvokeInformation platformInvokeInformation, IInternFactory internFactory) {
      this.importModule = platformInvokeInformation.ImportModule;
      this.importName = platformInvokeInformation.ImportName;
      this.noMangle = platformInvokeInformation.NoMangle;
      this.pinvokeCallingConvention = platformInvokeInformation.PInvokeCallingConvention;
      this.stringFormat = platformInvokeInformation.StringFormat;
      this.supportsLastError = platformInvokeInformation.SupportsLastError;
      this.useBestFit = platformInvokeInformation.UseBestFit;
      this.throwExceptionForUnmappableChar = platformInvokeInformation.ThrowExceptionForUnmappableChar;
    }

    public IModuleReference ImportModule {
      get { return this.importModule; }
      set { this.importModule = value; }
    }
    IModuleReference importModule;

    public IName ImportName {
      get { return this.importName; }
      set { this.importName = value; }
    }
    IName importName;

    public bool NoMangle {
      get { return this.noMangle; }
      set { this.noMangle = value; }
    }
    bool noMangle;

    public PInvokeCallingConvention PInvokeCallingConvention {
      get { return this.pinvokeCallingConvention; }
      set { this.pinvokeCallingConvention = value; }
    }
    PInvokeCallingConvention pinvokeCallingConvention;

    public StringFormatKind StringFormat {
      get { return this.stringFormat; }
      set { this.stringFormat = value; }
    }
    StringFormatKind stringFormat;

    public bool SupportsLastError {
      get { return this.supportsLastError; }
      set { this.supportsLastError = value; }
    }
    bool supportsLastError;

    public bool? UseBestFit {
      get { return this.useBestFit; }
      set { this.useBestFit = value; }
    }
    bool? useBestFit;

    public bool? ThrowExceptionForUnmappableChar {
      get { return this.throwExceptionForUnmappableChar; }
      set { this.throwExceptionForUnmappableChar = value; }
    }
    bool? throwExceptionForUnmappableChar;

  }

  public sealed class Resource : IResource, ICopyFrom<IResource> {

    public Resource() {
      this.attributes = new List<ICustomAttribute>();
      this.data = new List<byte>();
      this.definingAssembly = Dummy.Assembly;
      this.externalFile = Dummy.FileReference;
      this.isInExternalFile = false;
      this.isPublic = false;
      this.name = Dummy.Name;
    }

    public void Copy(IResource resource, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(resource.Attributes);
      this.data = new List<byte>(resource.Data);
      this.definingAssembly = resource.DefiningAssembly;
      if (resource.IsInExternalFile)
        this.externalFile = resource.ExternalFile;
      else
        this.externalFile = Dummy.FileReference;
      this.isInExternalFile = resource.IsInExternalFile;
      this.isPublic = resource.IsPublic;
      this.name = resource.Name;
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public List<byte> Data {
      get { return this.data; }
      set { this.data = value; }
    }
    List<byte> data;

    public IAssemblyReference DefiningAssembly {
      get { return this.definingAssembly; }
      set { this.definingAssembly = value; }
    }
    IAssemblyReference definingAssembly;

    public IFileReference ExternalFile {
      get { return this.externalFile; }
      set { this.externalFile = value; }
    }
    IFileReference externalFile;

    public bool IsInExternalFile {
      get { return this.isInExternalFile; }
      set { this.isInExternalFile = value; }
    }
    bool isInExternalFile;

    public bool IsPublic {
      get { return this.isPublic; }
      set { this.isPublic = value; }
    }
    bool isPublic;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    #region IResource Members

    IEnumerable<byte> IResource.Data {
      get { return this.data.AsReadOnly(); }
    }

    #endregion

    #region IResourceReference Members

    IEnumerable<ICustomAttribute> IResourceReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IResource IResourceReference.Resource {
      get { return this; }
    }

    #endregion
  }

  public sealed class ResourceReference : IResourceReference, ICopyFrom<IResourceReference> {

    public ResourceReference() {
      this.attributes = new List<ICustomAttribute>();
      this.definingAssembly = Dummy.Assembly;
      this.isPublic = false;
      this.name = Dummy.Name;
      this.resource = Dummy.Resource;
    }

    public void Copy(IResourceReference resourceReference, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(resourceReference.Attributes);
      this.definingAssembly = resourceReference.DefiningAssembly;
      this.isPublic = resourceReference.IsPublic;
      this.name = resourceReference.Name;
      this.resource = resourceReference.Resource;
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public IAssemblyReference DefiningAssembly {
      get { return this.definingAssembly; }
      set { this.definingAssembly = value; }
    }
    IAssemblyReference definingAssembly;

    public bool IsPublic {
      get { return this.isPublic; }
      set { this.isPublic = value; }
    }
    bool isPublic;

    public IName Name {
      get { return this.name; }
      set { this.name = value; }
    }
    IName name;

    public IResource Resource {
      get { return this.resource; }
      set { this.resource = value; }
    }
    IResource resource;

    #region IResourceReference Members

    IEnumerable<ICustomAttribute> IResourceReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class SecurityAttribute : ISecurityAttribute, ICopyFrom<ISecurityAttribute> {

    public SecurityAttribute() {
      this.action = (SecurityAction)0;
      this.attributes = new List<ICustomAttribute>();
    }

    public void Copy(ISecurityAttribute securityAttribute, IInternFactory internFactory) {
      this.action = securityAttribute.Action;
      this.attributes = new List<ICustomAttribute>(securityAttribute.Attributes);
    }

    public SecurityAction Action {
      get { return this.action; }
      set { this.action = value; }
    }
    SecurityAction action;

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    #region ISecurityAttribute Members


    IEnumerable<ICustomAttribute> ISecurityAttribute.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    #endregion
  }

  public sealed class Win32Resource : IWin32Resource, ICopyFrom<IWin32Resource> {

    public Win32Resource() {
      this.codePage = 0;
      this.data = new List<byte>();
      this.id = 0;
      this.languageId = 0;
      this.name = "";
      this.typeId = 0;
      this.typeName = "";
    }

    public void Copy(IWin32Resource win32Resource, IInternFactory internFactory) {
      this.codePage = win32Resource.CodePage;
      this.data = new List<byte>(win32Resource.Data);
      this.id = win32Resource.Id;
      this.languageId = win32Resource.LanguageId;
      this.name = win32Resource.Name;
      this.typeId = win32Resource.TypeId;
      this.typeName = win32Resource.TypeName;
    }

    public uint CodePage {
      get { return this.codePage; }
      set { this.codePage = value; }
    }
    uint codePage;

    public List<byte> Data {
      get { return this.data; }
      set { this.data = value; }
    }
    List<byte> data;

    public int Id {
      get { return this.id; }
      set { this.id = value; }
    }
    int id;

    public uint LanguageId {
      get { return this.languageId; }
      set { this.languageId = value; }
    }
    uint languageId;

    public string Name {
      get { return this.name; }
      set { this.name = value; }
    }
    string name;

    public int TypeId {
      get { return this.typeId; }
      set { this.typeId = value; }
    }
    int typeId;

    public string TypeName {
      get { return this.typeName; }
      set { this.typeName = value; }
    }
    string typeName;

    #region IWin32Resource Members


    IEnumerable<byte> IWin32Resource.Data {
      get { return this.data.AsReadOnly(); }
    }

    #endregion
  }
}