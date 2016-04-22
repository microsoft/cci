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
using System.Diagnostics.Contracts;
using Microsoft.Cci.Immutable;

namespace Microsoft.Cci.ReflectionImporter {

  internal abstract class AliasForType : IAliasForType {

    protected AliasForType(ReflectionMapper mapper, Type aliasedType) {
      this.mapper = mapper;
      this.aliasedType = aliasedType;
    }

    protected ReflectionMapper mapper;
    protected Type aliasedType;

    #region IAliasForType Members

    public INamedTypeReference AliasedType {
      get { return (INamedTypeReference)this.mapper.GetType(this.aliasedType); }
    }

    public ushort GenericParameterCount {
      get { return this.AliasedType.GenericParameterCount; }
    }

    public IEnumerable<IAliasMember> Members {
      get {
        if (this.members == null) {
          var nestedTypes = this.aliasedType.GetNestedTypes();
          var n = nestedTypes.Length;
          IAliasMember[] members = new IAliasMember[n];
          for (int i = 0; i < n; i++) members[i] = new NestedAliasForType(this.mapper, this, nestedTypes[i]);
          this.members = IteratorHelper.GetReadonly(members);
        }
        return this.members;
      }
    }
    IEnumerable<IAliasMember> members;

    public IName Name {
      get { return this.AliasedType.Name; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    public abstract void Dispatch(IMetadataVisitor visitor);

    public abstract void DispatchAsReference(IMetadataVisitor visitor);

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion

    #region IScope<IAliasMember> Members

    public bool Contains(IAliasMember member) {
      return IteratorHelper.EnumerableContains(this.Members, member);
    }

    public IEnumerable<IAliasMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<IAliasMember, bool> predicate) {
      var key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      foreach (var member in this.Members) {
        if (key == (ignoreCase ? member.Name.UniqueKeyIgnoringCase : member.Name.UniqueKey) && predicate(member)) yield return member;
      }
    }

    public IEnumerable<IAliasMember> GetMatchingMembers(Function<IAliasMember, bool> predicate) {
      foreach (var member in this.Members) {
        if (predicate(member)) yield return member;
      }
    }

    public IEnumerable<IAliasMember> GetMembersNamed(IName name, bool ignoreCase) {
      var key = ignoreCase ? name.UniqueKeyIgnoringCase : name.UniqueKey;
      foreach (var member in this.Members) {
        if (key == (ignoreCase ? member.Name.UniqueKeyIgnoringCase : member.Name.UniqueKey)) yield return member;
      }
    }

    #endregion
  }

  internal sealed class NamespaceAliasForType : AliasForType, INamespaceAliasForType {

    internal NamespaceAliasForType(ReflectionMapper mapper, Type aliasedType)
      : base(mapper, aliasedType) {
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region INamespaceAliasForType Members

    public bool IsPublic {
      get { return this.aliasedType.IsPublic; }
    }

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return this.mapper.GetNamespace(this.aliasedType); }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get { return this.ContainingNamespace; }
    }

    IName IContainerMember<INamespaceDefinition>.Name {
      get { return this.AliasedType.Name; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return this.ContainingNamespace; }
    }

    #endregion

  }

  internal sealed class NestedAliasForType : AliasForType, INestedAliasForType {

    internal NestedAliasForType(ReflectionMapper mapper, IAliasForType containingAlias, Type aliasedType)
      : base(mapper, aliasedType) {
      this.containingAlias = containingAlias;
    }

    IAliasForType containingAlias;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IAliasMember Members

    public IAliasForType ContainingAlias {
      get { return this.containingAlias; }
    }

    public TypeMemberVisibility Visibility {
      get { return ReflectionMapper.TypeMemberVisibilityFor(this.aliasedType); }
    }

    #endregion

    #region IContainerMember<IAliasForType> Members

    public IAliasForType Container {
      get { return this.containingAlias; }
    }

    IName IContainerMember<IAliasForType>.Name {
      get { return this.AliasedType.Name; }
    }

    #endregion

    #region IScopeMember<IScope<IAliasMember>> Members

    public IScope<IAliasMember> ContainingScope {
      get { return this.ContainingAlias; }
    }

    #endregion
  }

  internal sealed class AssemblyWrapper : IAssembly {

    internal AssemblyWrapper(ReflectionMapper mapper, Assembly assembly) {
      this.mapper = mapper;
      this.assembly = assembly;
    }

    Assembly assembly;
    ReflectionMapper mapper;

    #region IAssembly Members

    public IEnumerable<ICustomAttribute> AssemblyAttributes {
      get {
        if (this.assemblyAttributes == null)
          this.assemblyAttributes = this.mapper.GetCustomAttributes(this.assembly.GetCustomAttributesData());
        return this.assemblyAttributes;
      }
    }
    IEnumerable<ICustomAttribute>/*?*/ assemblyAttributes;

    public IEnumerable<IAliasForType> ExportedTypes {
      get {
        if (this.exportedTypes == null) {
          var exportedTypes = this.assembly.GetExportedTypes();
          var localTypes = this.GetLocalTypes();
          if (exportedTypes.Length != localTypes.Count) {
            List<IAliasForType> aliases = new List<IAliasForType>();
            foreach (var exportedType in exportedTypes) {
              if (localTypes.ContainsKey(exportedType)) continue;
              aliases.Add(this.mapper.GetAliasForType(exportedType));
            }
            this.exportedTypes = IteratorHelper.GetReadonly(aliases.ToArray());
          } else
            this.exportedTypes = Enumerable<IAliasForType>.Empty;
        }
        return this.exportedTypes;
      }
    }
    IEnumerable<IAliasForType> exportedTypes;

    Dictionary<Type, bool> GetLocalTypes() {
      var result = new Dictionary<Type, bool>();
      foreach (var type in this.assembly.ManifestModule.GetTypes())
        result.Add(type, true);
      return result;
    }

    public IEnumerable<IFileReference> Files {
      get {
        if (this.files == null) {
          var modules = this.assembly.GetModules(false);
          Dictionary<string, bool> moduleMap = new Dictionary<string, bool>();
          foreach (var module in modules) moduleMap[module.Name] = true;
          var fileStreams = this.assembly.GetFiles(true);
          var n = fileStreams.Length;
          IFileReference[] fileReferences = new IFileReference[n];
          for (int i = 0; i < n; i++) {
            bool hasMetadata = moduleMap.ContainsKey(fileStreams[i].Name);
            fileReferences[i] = this.mapper.GetFileReference(fileStreams[i].Name, this, hasMetadata);
          }
          this.files = IteratorHelper.GetReadonly(fileReferences);
        }
        return this.files;
      }
    }
    IEnumerable<IFileReference> files;

    public IEnumerable<byte> HashValue {
      get { return Enumerable<byte>.Empty; }
    }

    public uint Flags {
      get { return (uint)this.assembly.GetName().Flags; }
    }

    public IEnumerable<IModule> MemberModules {
      get {
        if (this.memberModules == null) {
          var modules = this.assembly.GetModules();
          var n = modules.Length;
          var memberModules = new IModule[n];
          for (int i = 0; i < n; i++)
            memberModules[i] = this.mapper.GetModule(modules[i]);
          this.memberModules = IteratorHelper.GetReadonly(memberModules);
        }
        return this.memberModules;
      }
    }
    IEnumerable<IModule> memberModules;

    public IEnumerable<byte> PublicKey {
      get { return this.assembly.GetName().GetPublicKey(); }
    }

    public IEnumerable<IResourceReference> Resources {
      get {
        if (this.resources == null) {
          var resourceNames = this.assembly.GetManifestResourceNames();
          var n = resourceNames.Length;
          var resRefs = new IResourceReference[n];
          for (int i = 0; i < n; i++) {
            var resName = resourceNames[i];
            var resInfo = this.assembly.GetManifestResourceInfo(resName);
            var bytes = this.assembly.GetManifestResourceStream(resName);
            resRefs[i] = this.mapper.GetResource(resName, resInfo, bytes);
          }
          this.resources = IteratorHelper.GetReadonly(resRefs);
        }
        return this.resources;
      }
    }
    IEnumerable<IResourceReference> resources;

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IModule Members

    public IEnumerable<IAssemblyReference> AssemblyReferences {
      get { throw new NotImplementedException(); }
    }

    public ulong BaseAddress {
      get { throw new NotImplementedException(); }
    }

    public IAssembly ContainingAssembly {
      get { throw new NotImplementedException(); }
    }

    public string DebugInformationLocation {
      get { throw new NotImplementedException(); }
    }

    public string DebugInformationVersion {
      get { throw new NotImplementedException(); }
    }

    public ushort DllCharacteristics {
      get { throw new NotImplementedException(); }
    }

    public IMethodReference EntryPoint {
      get { throw new NotImplementedException(); }
    }

    public uint FileAlignment {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<string> GetStrings() {
      throw new NotImplementedException();
    }

    public IEnumerable<INamedTypeDefinition> GetAllTypes() {
      throw new NotImplementedException();
    }

    public IEnumerable<IGenericMethodInstanceReference> GetGenericMethodInstances() {
      throw new NotImplementedException();
    }

    public IEnumerable<ITypeReference> GetStructuralTypeInstances() {
      throw new NotImplementedException();
    }

    public IEnumerable<ITypeMemberReference> GetStructuralTypeInstanceMembers() {
      throw new NotImplementedException();
    }

    public IEnumerable<ITypeReference> GetTypeReferences() {
      throw new NotImplementedException();
    }

    public IEnumerable<ITypeMemberReference> GetTypeMemberReferences() {
      throw new NotImplementedException();
    }

    public bool ILOnly {
      get { throw new NotImplementedException(); }
    }

    public ModuleKind Kind {
      get { throw new NotImplementedException(); }
    }

    public byte LinkerMajorVersion {
      get { throw new NotImplementedException(); }
    }

    public byte LinkerMinorVersion {
      get { throw new NotImplementedException(); }
    }

    public byte MetadataFormatMajorVersion {
      get { throw new NotImplementedException(); }
    }

    public byte MetadataFormatMinorVersion {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<ICustomAttribute> ModuleAttributes {
      get { throw new NotImplementedException(); }
    }

    public IName ModuleName {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<IModuleReference> ModuleReferences {
      get { throw new NotImplementedException(); }
    }

    public Guid PersistentIdentifier {
      get { throw new NotImplementedException(); }
    }

    public Machine Machine {
      get { throw new NotImplementedException(); }
    }

    public bool Prefers32bits {
      get { return false; }
    }

    public bool RequiresAmdInstructionSet {
      get { throw new NotImplementedException(); }
    }

    public bool RequiresStartupStub {
      get { throw new NotImplementedException(); }
    }

    public bool Requires32bits {
      get { throw new NotImplementedException(); }
    }

    public bool Requires64bits {
      get { throw new NotImplementedException(); }
    }

    public ulong SizeOfHeapCommit {
      get { throw new NotImplementedException(); }
    }

    public ulong SizeOfHeapReserve {
      get { throw new NotImplementedException(); }
    }

    public ulong SizeOfStackCommit {
      get { throw new NotImplementedException(); }
    }

    public ulong SizeOfStackReserve {
      get { throw new NotImplementedException(); }
    }

    public bool StrongNameSigned {
      get { throw new NotImplementedException(); }
    }

    public ushort SubsystemMajorVersion {
      get { throw new NotImplementedException(); }
    }

    public ushort SubsystemMinorVersion {
      get { throw new NotImplementedException(); }
    }

    public string TargetRuntimeVersion {
      get { throw new NotImplementedException(); }
    }

    public bool TrackDebugData {
      get { throw new NotImplementedException(); }
    }

    public bool UsePublicKeyTokensForAssemblyReferences {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<IWin32Resource> Win32Resources {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IUnit Members

    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { throw new NotImplementedException(); }
    }

    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { throw new NotImplementedException(); }
    }

    public IPlatformType PlatformType {
      get { throw new NotImplementedException(); }
    }

    public string Location {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<IPESection> UninterpretedSections {
      get { throw new NotImplementedException(); }
    }

    public IRootUnitNamespace UnitNamespaceRoot {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<IUnitReference> UnitReferences {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region INamespaceRootOwner Members

    public INamespaceDefinition NamespaceRoot {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IUnitReference Members

    public IUnit ResolvedUnit {
      get { throw new NotImplementedException(); }
    }

    public UnitIdentity UnitIdentity {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { throw new NotImplementedException(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
      throw new NotImplementedException();
    }

    public void DispatchAsReference(IMetadataVisitor visitor) {
      throw new NotImplementedException();
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IModuleReference Members

    IAssemblyReference IModuleReference.ContainingAssembly {
      get { throw new NotImplementedException(); }
    }

    public IModule ResolvedModule {
      get { throw new NotImplementedException(); }
    }

    public ModuleIdentity ModuleIdentity {
      get { throw new NotImplementedException(); }
    }

    #endregion

    #region IAssemblyReference Members

    public IEnumerable<IName> Aliases {
      get { throw new NotImplementedException(); }
    }

    public IAssembly ResolvedAssembly {
      get { throw new NotImplementedException(); }
    }

    public string Culture {
      get { throw new NotImplementedException(); }
    }

    public bool IsRetargetable {
      get { throw new NotImplementedException(); }
    }

    public bool ContainsForeignTypes {
      get { throw new NotImplementedException(); }
    }

    public IEnumerable<byte> PublicKeyToken {
      get { throw new NotImplementedException(); }
    }

    public Version Version {
      get { throw new NotImplementedException(); }
    }

    public AssemblyIdentity AssemblyIdentity {
      get { throw new NotImplementedException(); }
    }

    public AssemblyIdentity UnifiedAssemblyIdentity {
      get { throw new NotImplementedException(); }
    }

    #endregion
  }

  internal sealed class ConstructorWrapper : MethodBaseWrapper<ConstructorInfo> {

    internal ConstructorWrapper(ReflectionMapper mapper, ConstructorInfo info)
      : base(mapper, info) {
    }

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return Enumerable<IGenericMethodParameter>.Empty; }
    }

    public override IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    public override IName ReturnValueName {
      get { return Dummy.Name; }
    }

    public override IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    public override bool ReturnValueIsByRef {
      get { return false; }
    }

    public override bool ReturnValueIsModified {
      get { return false; }
    }

    public override ITypeReference Type {
      get { return this.mapper.host.PlatformType.SystemVoid; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IMethodReference)this);
    }
  }

  internal sealed class CustomAttributeWrapper : ICustomAttribute {

    internal CustomAttributeWrapper(ReflectionMapper mapper, CustomAttributeData customAttributeData) {
      this.mapper = mapper;
      this.customAttributeData = customAttributeData;
    }

    CustomAttributeData customAttributeData;
    ReflectionMapper mapper;

    #region ICustomAttribute Members

    public IEnumerable<IMetadataExpression> Arguments {
      get {
        foreach (var argument in this.customAttributeData.ConstructorArguments)
          yield return this.mapper.GetExpression(argument);
      }
    }

    public IMethodReference Constructor {
      get {
        return this.mapper.GetMethod(this.customAttributeData.Constructor);
      }
    }

    public IEnumerable<IMetadataNamedArgument> NamedArguments {
      get {
        foreach (var argument in this.customAttributeData.NamedArguments)
          yield return this.mapper.GetExpression(argument);
      }
    }

    public ushort NumberOfNamedArguments {
      get {
        return (ushort)this.customAttributeData.NamedArguments.Count;
      }
    }

    public ITypeReference Type {
      get {
        return this.mapper.GetType(this.customAttributeData.Constructor.DeclaringType);
      }
    }

    #endregion
  }

  internal sealed class EventWrapper : MemberInfoWrapper<EventInfo>, IEventDefinition {

    internal EventWrapper(ReflectionMapper mapper, EventInfo info)
      : base(mapper, info) {
    }

    #region IEventDefinition Members

    public IEnumerable<IMethodReference> Accessors {
      get {
        if (this.accessors == null) {
          var addMethod = this.info.GetAddMethod(true);
          var raiseMethod = this.info.GetRaiseMethod(true);
          var removeMethod = this.info.GetRemoveMethod(true);
          var otherMethods = this.info.GetOtherMethods();
          var n = otherMethods.Length;
          if (addMethod != null) n++;
          if (raiseMethod != null) n++;
          if (removeMethod != null) n++;
          var accessors = new IMethodReference[n];
          if (removeMethod != null) accessors[--n] = this.mapper.GetMethod(removeMethod);
          if (raiseMethod != null) accessors[--n] = this.mapper.GetMethod(raiseMethod);
          if (addMethod != null) accessors[--n] = this.mapper.GetMethod(addMethod);
          for (; n >= 0; n--) accessors[n] = this.mapper.GetMethod(otherMethods[n]);
          this.accessors = IteratorHelper.GetReadonly(accessors);
        }
        return this.accessors;
      }
    }
    IEnumerable<IMethodReference> accessors;

    public IMethodReference Adder {
      get {
        if (this.adder == null) {
          var addMethod = this.info.GetAddMethod(true);
          if (addMethod == null)
            this.adder = Dummy.MethodReference;
          else
            this.adder = this.mapper.GetMethod(addMethod);
        }
        return this.adder;
      }
    }
    IMethodReference adder;

    public IMethodReference Caller {
      get {
        if (this.caller == null) {
          var raiseMethod = this.info.GetRaiseMethod(true);
          if (raiseMethod == null)
            this.caller = Dummy.MethodReference;
          else
            this.caller = this.mapper.GetMethod(raiseMethod);
        }
        return this.caller;
      }
    }
    IMethodReference caller;

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return this.info.IsSpecialName; }
    }

    public IMethodReference Remover {
      get {
        if (this.remover == null) {
          var removeMethod = this.info.GetRemoveMethod(true);
          if (removeMethod == null)
            this.remover = Dummy.MethodReference;
          else
            this.remover = this.mapper.GetMethod(removeMethod);
        }
        return this.remover;
      }
    }
    IMethodReference remover;

    public ITypeReference Type {
      get {
        if (this.type == null) {
          var type = this.info.EventHandlerType;
          if (type == null)
            this.type = Dummy.TypeReference;
          else
            this.type = this.mapper.GetType(type);
        }
        return this.type;
      }
    }
    ITypeReference type;

    #endregion

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }
  }

  internal sealed class FieldWrapper : MemberInfoWrapper<FieldInfo>, IFieldDefinition {

    internal FieldWrapper(ReflectionMapper mapper, FieldInfo info)
      : base(mapper, info) {
    }

    #region IFieldDefinition Members

    public uint BitLength {
      get { return 0; }
    }

    public IMetadataConstant CompileTimeValue {
      get {
        if (this.compileTimeValue == null) {
          var rawConst = this.info.GetRawConstantValue();
          this.compileTimeValue = this.mapper.GetMetadataConstant(rawConst);
        }
        return this.compileTimeValue;
      }
    }
    IMetadataConstant compileTimeValue;

    public ISectionBlock FieldMapping {
      get {
        return Dummy.SectionBlock; //TODO: can this be obtained via Reflection? Perhaps via a pseudo custom attribute.
      }
    }

    public bool IsBitField {
      get { return false; }
    }

    public bool IsCompileTimeConstant {
      get { return (this.info.Attributes & FieldAttributes.Literal) != 0; }
    }

    public bool IsMapped {
      get { return (this.info.Attributes & FieldAttributes.HasFieldRVA) != 0; }
    }

    public bool IsMarshalledExplicitly {
      get { return (this.info.Attributes & FieldAttributes.HasFieldMarshal) != 0; }
    }

    public bool IsNotSerialized {
      get { return this.info.IsNotSerialized; }
    }

    public bool IsReadOnly {
      get { return this.info.IsInitOnly; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return this.info.IsSpecialName; }
    }

    public IMarshallingInformation MarshallingInformation {
      get {
        return Dummy.MarshallingInformation; //TODO: figure out how to look at marshalling information. Via custom attributes?
      }
    }

    public uint Offset {
      get {
        return 0; //TODO: is there a way to find the field offset value via Reflection?
      }
    }

    public int SequenceNumber {
      get {
        if (this.sequenceNumber == -1) {
          var declaringType = this.info.DeclaringType;
          var fields = declaringType.GetFields(BindingFlags.DeclaredOnly|BindingFlags.Public|BindingFlags.NonPublic);
          for (int i = 0, n = fields.Length; i < n; i++) {
            if (fields[i] == this.info) { this.sequenceNumber = i; break; }
          }
        }
        return this.sequenceNumber;
      }
    }
    int sequenceNumber = -1;

    #endregion

    #region IFieldReference Members

    public IEnumerable<ICustomModifier> CustomModifiers {
      get {
        if (this.customModifiers == null) {
          var optional = this.info.GetOptionalCustomModifiers();
          var required = this.info.GetRequiredCustomModifiers();
          this.customModifiers = this.mapper.GetCustomModifiers(optional, required);
        }
        return this.customModifiers;
      }
    }
    IEnumerable<ICustomModifier> customModifiers;

    public uint InternedKey {
      get {
        if (this.internedKey == 0)
          this.internedKey = this.mapper.host.InternFactory.GetFieldInternedKey(this);
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsModified {
      get {
        return IteratorHelper.EnumerableIsNotEmpty(this.CustomModifiers);
      }
    }

    public bool IsStatic {
      get { return this.info.IsStatic; }
    }

    public ITypeReference Type {
      get {
        if (this.type != null)
          this.type = this.mapper.GetType(this.info.FieldType);
        return this.type;
      }
    }
    ITypeReference type;

    public IFieldDefinition ResolvedField {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer Members

    public IMetadataConstant Constant {
      get { return this.CompileTimeValue; }
    }

    #endregion

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IFieldReference)this);
    }
  }

  internal sealed class FileReferenceWrapper : IFileReference {

    internal FileReferenceWrapper(IName fileName, IAssembly containingAssembly, bool hasMetadata) {
      this.fileName = fileName;
      this.containingAssembly = containingAssembly;
      this.hasMetadata = hasMetadata;
    }

    #region IFileReference Members

    public IAssembly ContainingAssembly {
      get { return this.containingAssembly; }
    }
    IAssembly containingAssembly;

    public bool HasMetadata {
      get { return this.hasMetadata; }
    }
    bool hasMetadata;

    public IName FileName {
      get { return this.fileName; }
    }
    IName fileName;

    public IEnumerable<byte> HashValue {
      get { return Enumerable<byte>.Empty; }
    }

    #endregion
  }

  internal abstract class MemberInfoWrapper<Info> : MemberInfoReferenceWrapper<Info>, ITypeDefinitionMember where Info : MemberInfo {

    internal MemberInfoWrapper(ReflectionMapper mapper, Info info)
      : base(mapper, info) {
    }

    #region ITypeDefinitionMember Members

    public TypeMemberVisibility Visibility {
      get { return ReflectionMapper.TypeMemberVisibilityFor(this.info); }
    }

    #endregion

    public override ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

  }

  internal abstract class MemberInfoReferenceWrapper<Info> : ITypeMemberReference where Info : MemberInfo {

    internal MemberInfoReferenceWrapper(ReflectionMapper mapper, Info info) {
      this.mapper = mapper;
      this.info = info;
    }

    protected ReflectionMapper mapper;
    protected Info info;

    public ITypeDefinition ContainingTypeDefinition {
      get {
        if (this.containingTypeDefinition == null)
          this.containingTypeDefinition = this.mapper.GetType(this.info.DeclaringType);
        return this.containingTypeDefinition;
      }
    }
    ITypeDefinition containingTypeDefinition;

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get {
        return this.ContainingTypeDefinition;
      }
    }

    public abstract ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get;
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get {
        if (this.attributes == null)
          this.attributes = this.mapper.GetCustomAttributes(this.info.GetCustomAttributesData());
        return this.attributes;
      }
    }
    IEnumerable<ICustomAttribute> attributes;

    public abstract void Dispatch(IMetadataVisitor visitor);

    public abstract void DispatchAsReference(IMetadataVisitor visitor);

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
      get {
        if (this.name == null) {
          this.name = this.mapper.host.NameTable.GetNameFor(this.info.Name);
        }
        return this.name;
      }
    }
    IName name;

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion
  }

  internal sealed class MetadataConstantWrapper : IMetadataConstant {

    internal MetadataConstantWrapper(ITypeReference type, object value) {
      this.type = type;
      this.value = value;
    }

    ITypeReference type;
    object value;

    #region IMetadataConstant Members

    public object Value {
      get { return this.value; }
    }

    #endregion

    #region IMetadataExpression Members

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference Type {
      get { return this.type; }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal sealed class MetadataCreateArrayWrapper : IMetadataCreateArray {

    public MetadataCreateArrayWrapper(ReflectionMapper mapper, IArrayTypeReference arrayType, ICollection<CustomAttributeTypedArgument> initializers) {
      this.mapper = mapper;
      this.arrayType = arrayType;
      this.initializers = initializers;
    }

    ReflectionMapper mapper;
    IArrayTypeReference arrayType;
    ICollection<CustomAttributeTypedArgument> initializers;

    #region IMetadataCreateArray Members

    public ITypeReference ElementType {
      get { return this.arrayType.ElementType; }
    }

    public IEnumerable<IMetadataExpression> Initializers {
      get {
        foreach (var arrayValue in this.initializers)
          yield return this.mapper.GetExpression(arrayValue);
      }
    }

    public IEnumerable<int> LowerBounds {
      get { return IteratorHelper.GetSingletonEnumerable<int>(0); }
    }

    public uint Rank {
      get { return 1; }
    }

    public IEnumerable<ulong> Sizes {
      get { return IteratorHelper.GetSingletonEnumerable<ulong>((ulong)this.initializers.Count); }
    }

    #endregion

    #region IMetadataExpression Members

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference Type {
      get { return this.arrayType; }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal sealed class MetadataNamedArgumentWrapper : IMetadataNamedArgument {

    internal MetadataNamedArgumentWrapper(ReflectionMapper mapper, CustomAttributeNamedArgument customAttributeNamedArgument) {
      this.mapper = mapper;
      this.customAttributeNamedArgument = customAttributeNamedArgument;
    }

    CustomAttributeNamedArgument customAttributeNamedArgument;
    ReflectionMapper mapper;

    #region IMetadataNamedArgument Members

    public IName ArgumentName {
      get {
        return this.mapper.host.NameTable.GetNameFor(this.customAttributeNamedArgument.MemberInfo.Name);
      }
    }

    public IMetadataExpression ArgumentValue {
      get {
        return this.mapper.GetExpression(this.customAttributeNamedArgument.TypedValue);
      }
    }

    public bool IsField {
      get { return this.customAttributeNamedArgument.MemberInfo is FieldInfo; }
    }

    public object ResolvedDefinition {
      get {
        var fieldInfo = this.customAttributeNamedArgument.MemberInfo as FieldInfo;
        if (fieldInfo != null) return this.mapper.GetField(fieldInfo);
        return this.mapper.GetProperty((PropertyInfo)this.customAttributeNamedArgument.MemberInfo);
      }
    }

    #endregion

    #region IMetadataExpression Members

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference Type {
      get {
        return this.mapper.GetType(this.customAttributeNamedArgument.TypedValue.ArgumentType);
      }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal sealed class MetadataTypeOfWrapper : IMetadataTypeOf {

    internal MetadataTypeOfWrapper(ReflectionMapper mapper, ITypeReference type, Type typeToGet) {
      this.mapper = mapper;
      this.type = type;
      this.typeToGet = typeToGet;
    }

    ReflectionMapper mapper;
    ITypeReference type;
    Type typeToGet;

    #region IMetadataTypeOf Members

    public ITypeReference TypeToGet {
      get {
        return this.mapper.GetType(this.typeToGet);
      }
    }

    #endregion

    #region IMetadataExpression Members

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ITypeReference Type {
      get { return this.type; }
    }

    #endregion

    #region IObjectWithLocations Members

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion
  }

  internal abstract class MethodBaseWrapper<Info> : MemberInfoWrapper<Info>, IMethodDefinition where Info : MethodBase {

    internal MethodBaseWrapper(ReflectionMapper mapper, Info info)
      : base(mapper, info) {
    }

    #region IMethodDefinition Members

    public IMethodBody Body {
      get {
        if (this.body == null)
          this.body = this.mapper.GetBody(this.info.GetMethodBody());
        return this.body;
      }
    }
    IMethodBody body;

    public abstract IEnumerable<IGenericMethodParameter> GenericParameters {
      get;
    }

    public bool HasDeclarativeSecurity {
      get {
        return (this.info.Attributes & MethodAttributes.HasSecurity) != 0;
      }
    }

    public bool HasExplicitThisParameter {
      get {
        return (this.info.CallingConvention & CallingConventions.ExplicitThis) != 0;
      }
    }

    public bool IsAbstract {
      get {
        return (this.info.Attributes & MethodAttributes.Abstract) != 0;
      }
    }

    public bool IsAccessCheckedOnOverride {
      get {
        return (this.info.Attributes & MethodAttributes.CheckAccessOnOverride) != 0;
      }
    }

    public bool IsAggressivelyInlined {
      get {
        return false; //Cannot get this from Reflection
      }
    }

    public bool IsCil {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL;
      }
    }

    public bool IsConstructor {
      get { return this.info is ConstructorInfo; }
    }

    public bool IsExternal {
      get {
        return this.IsPlatformInvoke || this.IsRuntimeInternal || this.IsRuntimeImplemented;
      }
    }

    public bool IsForwardReference {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.ForwardRef) != 0;
      }
    }

    public bool IsHiddenBySignature {
      get {
        return (this.info.Attributes & MethodAttributes.HideBySig) != 0;
      }
    }

    public bool IsNativeCode {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.Native) != 0;
      }
    }

    public bool IsNewSlot {
      get {
        return (this.info.Attributes & MethodAttributes.NewSlot) != 0;
      }
    }

    public bool IsNeverInlined {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.NoInlining) != 0;
      }
    }

    public bool IsNeverOptimized {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.NoOptimization) != 0;
      }
    }

    public bool IsPlatformInvoke {
      get {
        return (this.info.Attributes & MethodAttributes.PinvokeImpl) != 0;
      }
    }

    public bool IsRuntimeImplemented {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.Runtime) != 0;
      }
    }

    public bool IsRuntimeInternal {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.InternalCall) != 0;
      }
    }

    public bool IsRuntimeSpecial {
      get {
        return (this.info.Attributes & MethodAttributes.RTSpecialName) != 0;
      }
    }

    public bool IsSealed {
      get {
        return (this.info.Attributes & MethodAttributes.Final) != 0;
      }
    }

    public bool IsSpecialName {
      get {
        return (this.info.Attributes & MethodAttributes.SpecialName) != 0;
      }
    }

    public bool IsStaticConstructor {
      get { return this.IsStatic && this.IsConstructor; }
    }

    public bool IsSynchronized {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.Synchronized) != 0;
      }
    }

    public bool IsVirtual {
      get {
        return (this.info.Attributes & MethodAttributes.Virtual) != 0;
      }
    }

    public bool IsUnmanaged {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.Unmanaged) != 0;
      }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get {
        if (this.parameters == null)
          this.parameters = this.mapper.GetParameters(this.info.GetParameters());
        return this.parameters;
      }
    }
    IEnumerable<IParameterDefinition> parameters;

    public bool PreserveSignature {
      get {
        return (this.info.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;
      }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get {
        return Dummy.PlatformInvokeInformation; //Can't get this via Reflection
      }
    }

    public bool RequiresSecurityObject {
      get {
        return (this.info.Attributes & MethodAttributes.RequireSecObject) != 0;
      }
    }

    public abstract IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get;
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get {
        return false; //Can't get this via Reflection
      }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get {
        return Dummy.MarshallingInformation; //Can't get this via Reflection
      }
    }

    public abstract IName ReturnValueName {
      get;
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get {
        return Enumerable<ISecurityAttribute>.Empty; //Can't get security attributes via Reflection
      }
    }

    #endregion

    #region IMethodReference Members

    public bool AcceptsExtraArguments {
      get {
        return (this.info.CallingConvention & CallingConventions.VarArgs) != 0;
      }
    }

    public ushort GenericParameterCount {
      get {
        if (!this.info.IsGenericMethodDefinition) return 0;
        return (ushort)IteratorHelper.EnumerableCount(this.GenericParameters);
      }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0)
          this.internedKey = this.mapper.host.InternFactory.GetMethodInternedKey(this);
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsGeneric {
      get { return this.info.IsGenericMethodDefinition; }
    }

    public ushort ParameterCount {
      get { return (ushort)IteratorHelper.EnumerableCount(this.Parameters); }
    }

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return Enumerable<IParameterTypeInformation>.Empty; }
    }

    #endregion

    #region ISignature Members

    public CallingConvention CallingConvention {
      get {
        CallingConvention result = (Cci.CallingConvention)0;
        var cc = this.info.CallingConvention;
        if ((cc & CallingConventions.ExplicitThis) != 0) result |= Cci.CallingConvention.ExplicitThis;
        if ((cc & CallingConventions.HasThis) != 0) result |= Cci.CallingConvention.HasThis;
        if ((cc & CallingConventions.VarArgs) != 0) result |= Cci.CallingConvention.ExtraArguments;
        if (this.info.IsGenericMethodDefinition) result |= Cci.CallingConvention.Generic;
        return result;
      }
    }

    public bool IsStatic {
      get { return this.info.IsStatic; }
    }

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters);
      }
    }

    public abstract IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get;
    }

    public abstract bool ReturnValueIsByRef {
      get;
    }

    public abstract bool ReturnValueIsModified {
      get;
    }

    public abstract ITypeReference Type {
      get;
    }

    #endregion
  }

  internal abstract class MethodBaseReferenceWrapper<Info> : MemberInfoReferenceWrapper<Info>, IMethodReference where Info : MethodBase {

    internal MethodBaseReferenceWrapper(ReflectionMapper mapper, Info info)
      : base(mapper, info) {
    }

    public abstract IEnumerable<IGenericMethodParameter> GenericParameters {
      get;
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get {
        if (this.parameters == null)
          this.parameters = this.mapper.GetParameters(this.info.GetParameters());
        return this.parameters;
      }
    }
    IEnumerable<IParameterDefinition> parameters;

    #region IMethodReference Members

    public bool AcceptsExtraArguments {
      get {
        return (this.info.CallingConvention & CallingConventions.VarArgs) != 0;
      }
    }

    public ushort GenericParameterCount {
      get {
        if (!this.info.IsGenericMethodDefinition) return 0;
        return (ushort)IteratorHelper.EnumerableCount(this.GenericParameters);
      }
    }

    public uint InternedKey {
      get {
        if (this.internedKey == 0)
          this.internedKey = this.mapper.host.InternFactory.GetMethodInternedKey(this);
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsGeneric {
      get { return this.info.IsGenericMethodDefinition; }
    }

    public ushort ParameterCount {
      get { return (ushort)IteratorHelper.EnumerableCount(this.Parameters); }
    }

    public abstract IMethodDefinition ResolvedMethod {
      get;
    }

    public virtual IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return Enumerable<IParameterTypeInformation>.Empty; }
    }

    #endregion

    #region ISignature Members

    public CallingConvention CallingConvention {
      get {
        CallingConvention result = (Cci.CallingConvention)0;
        var cc = this.info.CallingConvention;
        if ((cc & CallingConventions.ExplicitThis) != 0) result |= Cci.CallingConvention.ExplicitThis;
        if ((cc & CallingConventions.HasThis) != 0) result |= Cci.CallingConvention.HasThis;
        if ((cc & CallingConventions.VarArgs) != 0) result |= Cci.CallingConvention.ExtraArguments;
        if (this.info.IsGenericMethodDefinition) result |= Cci.CallingConvention.Generic;
        return result;
      }
    }

    public bool IsStatic {
      get { return this.info.IsStatic; }
    }

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters);
      }
    }

    public abstract IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get;
    }

    public abstract bool ReturnValueIsByRef {
      get;
    }

    public abstract bool ReturnValueIsModified {
      get;
    }

    public abstract ITypeReference Type {
      get;
    }

    #endregion
  }

  internal sealed class MethodInfoWrapper : MethodBaseWrapper<MethodInfo> {

    internal MethodInfoWrapper(ReflectionMapper mapper, MethodInfo info)
      : base(mapper, info) {
    }

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get {
        if (this.genericParameters == null)
          this.genericParameters = this.mapper.GetMethodGenericParameters(this.info.GetGenericArguments());
        return this.genericParameters;
      }
    }
    IEnumerable<IGenericMethodParameter> genericParameters;

    public override IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get {
        if (this.returnValueAttributes == null) {
          var rpar = this.info.ReturnParameter;
          this.returnValueAttributes = this.mapper.GetCustomAttributes(rpar.GetCustomAttributesData());
        }
        return this.returnValueAttributes;
      }
    }
    IEnumerable<ICustomAttribute> returnValueAttributes;

    public override IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get {
        if (this.returnValueCustomModifiers == null) {
          var rpar = this.info.ReturnParameter;
          this.returnValueCustomModifiers = this.mapper.GetCustomModifiers(rpar.GetOptionalCustomModifiers(), rpar.GetRequiredCustomModifiers());
        }
        return this.returnValueCustomModifiers;
      }
    }
    IEnumerable<ICustomModifier> returnValueCustomModifiers;

    public override bool ReturnValueIsByRef {
      get { return this.info.ReturnType.IsByRef; }
    }

    public override bool ReturnValueIsModified {
      get {
        return IteratorHelper.EnumerableIsNotEmpty(this.ReturnValueCustomModifiers);
      }
    }

    public override IName ReturnValueName {
      get {
        if (this.returnValueName == null) {
          var rpar = this.info.ReturnParameter;
          this.returnValueName = this.mapper.host.NameTable.GetNameFor(rpar.Name);
        }
        return this.returnValueName;
      }
    }
    IName returnValueName;

    public override ITypeReference Type {
      get {
        if (this.type == null) {
          var rtype = this.info.ReturnType;
          if (rtype.IsByRef)
            this.type = this.mapper.GetType(rtype.GetElementType());
          else
            this.type = this.mapper.GetType(rtype);
        }
        return this.type;
      }
    }
    ITypeReference type;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IMethodReference)this);
    }
  }

  /// <summary>
  /// This is for methodinfos obtained at call sites that call vararg methods with extra parameters
  /// </summary>
  internal sealed class MethodInfoReferenceWrapper : MethodBaseReferenceWrapper<MethodInfo> {

    internal MethodInfoReferenceWrapper(ReflectionMapper mapper, MethodInfo info)
      : base(mapper, info) {
      Contract.Requires((info.CallingConvention & CallingConventions.VarArgs) != 0);
    }

    public override IEnumerable<IParameterTypeInformation> ExtraParameters {
      get {
        //TODO: figure out how to extract them
        return base.ExtraParameters;
      }
    }

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get {
        if (this.genericParameters == null)
          this.genericParameters = this.mapper.GetMethodGenericParameters(this.info.GetGenericArguments());
        return this.genericParameters;
      }
    }
    IEnumerable<IGenericMethodParameter> genericParameters;

    public override IMethodDefinition ResolvedMethod {
      get { throw new NotImplementedException(); }
    }
    //IMethodDefinition resolvedMethod;

    public override IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get {
        if (this.returnValueCustomModifiers == null) {
          var rpar = this.info.ReturnParameter;
          this.returnValueCustomModifiers = this.mapper.GetCustomModifiers(rpar.GetOptionalCustomModifiers(), rpar.GetRequiredCustomModifiers());
        }
        return this.returnValueCustomModifiers;
      }
    }
    IEnumerable<ICustomModifier> returnValueCustomModifiers;

    public override bool ReturnValueIsByRef {
      get { return this.info.ReturnType.IsByRef; }
    }

    public override bool ReturnValueIsModified {
      get {
        return IteratorHelper.EnumerableIsNotEmpty(this.ReturnValueCustomModifiers);
      }
    }

    public override ITypeReference Type {
      get {
        if (this.type == null) {
          var rtype = this.info.ReturnType;
          if (rtype.IsByRef)
            this.type = this.mapper.GetType(rtype.GetElementType());
          else
            this.type = this.mapper.GetType(rtype);
        }
        return this.type;
      }
    }
    ITypeReference type;

    public override ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }
  }

  internal sealed class GenericMethodInstanceWrapper : MethodBaseReferenceWrapper<MethodInfo>, IGenericMethodInstanceReference {

    internal GenericMethodInstanceWrapper(ReflectionMapper mapper, MethodInfo info)
      : base(mapper, info) {
    }

    public override IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return Enumerable<IGenericMethodParameter>.Empty; }
    }

    public override IMethodDefinition ResolvedMethod {
      get {
        if (this.resolvedMethod == null)
          this.resolvedMethod = new GenericMethodInstance(this.GenericMethod.ResolvedMethod, this.GenericArguments, this.mapper.host.InternFactory);
        return this.resolvedMethod;
      }
    }
    IMethodDefinition resolvedMethod;

    public override IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.GenericMethod.ReturnValueCustomModifiers; }
    }

    public override bool ReturnValueIsByRef {
      get { return this.GenericMethod.ReturnValueIsByRef; }
    }

    public override bool ReturnValueIsModified {
      get { return this.GenericMethod.ReturnValueIsModified; }
    }

    public override ITypeReference Type {
      get {
        if (this.type == null)
          this.type = this.mapper.GetType(this.info.ReturnType);
        return this.type;
      }
    }
    ITypeReference type;

    public override ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this.ResolvedMethod; }
    }

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    #region IGenericMethodInstanceReference Members

    public IEnumerable<ITypeReference> GenericArguments {
      get {
        if (this.genericArguments == null)
          this.genericArguments = this.mapper.GetTypes(this.info.GetGenericArguments());
        return this.genericArguments;
      }
    }
    IEnumerable<ITypeReference> genericArguments;

    public IMethodReference GenericMethod {
      get {
        if (this.genericMethod == null)
          this.genericMethod = this.mapper.GetMethod(this.info.GetGenericMethodDefinition());
        return this.genericMethod;
      }
    }
    IMethodReference genericMethod;

    #endregion

  }

  internal sealed class ResourceWrapper : IResource {

    internal ResourceWrapper(IEnumerable<byte> data, bool isInExternalFile, IFileReference externalFile, IAssemblyReference definingAssembly, IName name) {
      this.data = data;
      this.isInExternalFile = isInExternalFile;
      this.externalFile = externalFile;
      this.definingAssembly = definingAssembly;
      this.name = name;
    }

    #region IResource Members

    public IEnumerable<byte> Data {
      get { return this.data; }
    }
    IEnumerable<byte> data;

    public bool IsInExternalFile {
      get { return this.isInExternalFile; }
    }
    bool isInExternalFile;

    public IFileReference ExternalFile {
      get { return this.externalFile; }
    }
    IFileReference externalFile;

    #endregion

    #region IResourceReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    public IAssemblyReference DefiningAssembly {
      get { return this.definingAssembly; }
    }
    IAssemblyReference definingAssembly;

    public bool IsPublic {
      get { return true; }
    }

    public IName Name {
      get { return this.name; }
    }
    IName name;

    public IResource Resource {
      get { return this; }
    }

    #endregion
  }

}