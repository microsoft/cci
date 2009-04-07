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

  /// <summary>
  /// Represents a .NET assembly.
  /// </summary>
  public sealed class Assembly : Module, IAssembly, ICopyFrom<IAssembly> {

    public Assembly() {
      this.assemblyAttributes = new List<ICustomAttribute>();
      this.culture = "";
      this.exportedTypes = new List<IAliasForType>();
      this.flags = 0;
      this.files = new List<IFileReference>();
      this.memberModules = new List<IModule>();
      this.moduleName = Dummy.Name;
      this.publicKey = new byte[0];
      this.resources = new List<IResourceReference>();
      this.securityAttributes = new List<ISecurityAttribute>();
      this.version = new Version();
    }

    public void Copy(IAssembly assembly, IInternFactory internFactory) {
      ((ICopyFrom<IModule>)this).Copy(assembly, internFactory);
      this.assemblyAttributes = new List<ICustomAttribute>(assembly.AssemblyAttributes);
      this.culture = assembly.Culture;
      this.exportedTypes = new List<IAliasForType>(assembly.ExportedTypes);
      this.flags = assembly.Flags;
      this.files = new List<IFileReference>(assembly.Files);
      this.memberModules = new List<IModule>(assembly.MemberModules);
      this.moduleName = assembly.ModuleName;
      this.publicKey = assembly.PublicKey;
      this.resources = new List<IResourceReference>(assembly.Resources);
      this.securityAttributes = new List<ISecurityAttribute>(assembly.SecurityAttributes);
      this.version = assembly.Version;
    }

    public List<ICustomAttribute> AssemblyAttributes {
      get { return this.assemblyAttributes; }
      set { this.assemblyAttributes = value; }
    }
    List<ICustomAttribute> assemblyAttributes;

    public override IAssembly/*?*/ ContainingAssembly {
      get { return this; }
    }

    /// <summary>
    /// Identifies the culture associated with the assembly. Typically specified for sattelite assemblies with localized resources.
    /// Empty if not specified.
    /// </summary>
    public string Culture {
      get { return this.culture; }
      set { this.culture = value; }
    }
    string culture;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Public types defined in other modules making up this assembly and to which other assemblies may refer to via this assembly.
    /// </summary>
    public List<IAliasForType> ExportedTypes {
      get { return this.exportedTypes; }
      set { this.exportedTypes = value; }
    }
    List<IAliasForType> exportedTypes;

    /// <summary>
    /// A set of bits and bit ranges representing properties of the assembly. The value of <see cref="Flags"/> can be set
    /// from source code via the AssemblyFlags assembly custom attribute. The interpretation of the property depends on the target platform.
    /// </summary>
    public uint Flags {
      get { return this.flags; }
      set { this.flags = value; }
    }
    uint flags;

    public List<IFileReference> Files {
      get { return this.files; }
      set { this.files = value; }
    }
    List<IFileReference> files;

    public List<IModule> MemberModules {
      get { return this.memberModules; }
      set { this.memberModules = value; }
    }
    List<IModule> memberModules;

    /// <summary>
    /// The name of the module containing the assembly manifest. This can be different from the name of the assembly itself.
    /// </summary>
    public override IName ModuleName {
      get { return this.moduleName; }
      set { this.moduleName = value; }
    }
    IName moduleName;

    public override ModuleIdentity ModuleIdentity {
      get {
        return this.AssemblyIdentity;
      }
    }

    /// <summary>
    /// The public part of the key used to encrypt the SHA1 hash over the persisted form of this assembly . Empty if not specified.
    /// This value is used by the loader to decrypt HashValue which it then compares with a freshly computed hash value to verify the
    /// integrity of the assembly.
    /// </summary>
    public IEnumerable<byte> PublicKey {
      get { return this.publicKey; }
      set { this.publicKey = value; }
    }
    IEnumerable<byte> publicKey;

    public List<IResourceReference> Resources {
      get { return this.resources; }
      set { this.resources = value; }
    }
    List<IResourceReference> resources;

    public List<ISecurityAttribute> SecurityAttributes {
      get { return this.securityAttributes; }
      set { this.securityAttributes = value; }
    }
    List<ISecurityAttribute> securityAttributes;

    /// <summary>
    /// The version of the assembly.
    /// </summary>
    public Version Version {
      get { return this.version; }
      set { this.version = value; }
    }
    Version version;

    #region IAssembly Members

    IEnumerable<ICustomAttribute> IAssembly.AssemblyAttributes {
      get { return this.assemblyAttributes.AsReadOnly(); }
    }

    IEnumerable<IAliasForType> IAssembly.ExportedTypes {
      get { return this.exportedTypes.AsReadOnly(); }
    }

    IEnumerable<IResourceReference> IAssembly.Resources {
      get { return this.resources.AsReadOnly(); }
    }

    IEnumerable<IFileReference> IAssembly.Files {
      get { return this.files.AsReadOnly(); }
    }

    IEnumerable<IModule> IAssembly.MemberModules {
      get { return this.memberModules.AsReadOnly(); }
    }

    IEnumerable<ISecurityAttribute> IAssembly.SecurityAttributes {
      get { return this.securityAttributes.AsReadOnly(); }
    }

    #endregion

    #region IModuleReference Members

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return this; }
    }

    #endregion

    #region IAssemblyReference Members

    public AssemblyIdentity AssemblyIdentity {
      get {
        if (this.assemblyIdentity == null) {
          this.assemblyIdentity = UnitHelper.GetAssemblyIdentity(this);
        }
        return this.assemblyIdentity;
      }
    }
    AssemblyIdentity/*?*/ assemblyIdentity;

    public AssemblyIdentity UnifiedAssemblyIdentity {
      get { return this.AssemblyIdentity; }
    }

    public IEnumerable<IName> Aliases {
      get { return IteratorHelper.GetEmptyEnumerable<IName>(); }
    }

    public IAssembly ResolvedAssembly {
      get { return this; }
    }

    public IEnumerable<byte> PublicKeyToken {
      get {
        return UnitHelper.ComputePublicKeyToken(this.PublicKey);
      }
    }

    #endregion
  }

  public sealed class AssemblyReference : ModuleReference, IAssemblyReference, ICopyFrom<IAssemblyReference> {

    public AssemblyReference() {
      this.aliases = new List<IName>();
      this.ResolvedModule = this.resolvedAssembly = Dummy.Assembly;
      this.culture = string.Empty;
      this.publicKeyToken = new List<byte>();
      this.version = new Version();
      this.ModuleIdentity = this.assemblyIdentity = Dummy.Assembly.AssemblyIdentity;
      this.unifiedAssemblyIdentity = this.assemblyIdentity;
    }

    public void Copy(IAssemblyReference assemblyReference, IInternFactory internFactory) {
      ((ICopyFrom<IModuleReference>)this).Copy(assemblyReference, internFactory);
      this.aliases = new List<IName>(assemblyReference.Aliases);
      this.ResolvedModule = this.resolvedAssembly = assemblyReference.ResolvedAssembly;
      this.culture = assemblyReference.Culture;
      this.publicKeyToken = new List<byte>(assemblyReference.PublicKeyToken);
      this.version = assemblyReference.Version;
      this.ModuleIdentity = this.assemblyIdentity = assemblyReference.AssemblyIdentity;
      this.unifiedAssemblyIdentity = assemblyReference.UnifiedAssemblyIdentity;
    }

    public List<IName> Aliases {
      get { return this.aliases; }
      set { this.aliases = value; }
    }
    List<IName> aliases;

    public IAssembly ResolvedAssembly {
      get { return this.resolvedAssembly; }
      set { this.ResolvedModule = this.resolvedAssembly = value; }
    }
    IAssembly resolvedAssembly;

    public string Culture {
      get { return this.culture; }
      set { this.culture = value; }
    }
    string culture;

    public List<byte> PublicKeyToken {
      get { return this.publicKeyToken; }
      set { this.publicKeyToken = value; }
    }
    List<byte> publicKeyToken;

    public Version Version {
      get { return this.version; }
      set { this.version = value; }
    }
    Version version;

    public AssemblyIdentity AssemblyIdentity {
      get { return this.assemblyIdentity; }
      set { this.ModuleIdentity = this.assemblyIdentity = value; }
    }
    AssemblyIdentity assemblyIdentity;

    public AssemblyIdentity UnifiedAssemblyIdentity {
      get { return this.unifiedAssemblyIdentity; }
      set { this.unifiedAssemblyIdentity = value; }
    }
    AssemblyIdentity unifiedAssemblyIdentity;

    #region IAssemblyReference Members

    IEnumerable<IName> IAssemblyReference.Aliases {
      get { return this.aliases.AsReadOnly(); }
    }

    IEnumerable<byte> IAssemblyReference.PublicKeyToken {
      get { return this.publicKeyToken.AsReadOnly(); }
    }

    #endregion

  }

  public class Module : Unit, IModule, ICopyFrom<IModule> {

    public Module() {
      this.allTypes = new List<INamedTypeDefinition>();
      this.assemblyReferences = new List<IAssemblyReference>();
      this.baseAddress = 0x400000;
      this.containingAssembly = Dummy.Assembly;
      this.dllCharacteristics = 0;
      this.entryPoint = Dummy.MethodReference;
      this.fileAlignment = 512;
      this.ilOnly = true;
      this.kind = ModuleKind.DynamicallyLinkedLibrary;
      this.linkerMajorVersion = 6;
      this.linkerMinorVersion = 0;
      this.metadataFormatMajorVersion = 1;
      this.metadataFormatMinorVersion = 0;
      this.moduleAttributes = new List<ICustomAttribute>();
      this.moduleReferences = new List<IModuleReference>();
      this.persistentIdentifier = Guid.NewGuid();
      this.requiresAmdInstructionSet = false;
      this.requires32bits = false;
      this.requires64bits = false;
      this.sizeOfHeapCommit = 0x1000;
      this.sizeOfHeapReserve = 0x100000;
      this.sizeOfStackCommit = 0x1000;
      this.sizeOfStackReserve = 0x100000;
      this.targetRuntimeVersion = "";
      this.trackDebugData = false;
      this.usePublicKeyTokensForAssemblyReferences = false;
      this.win32Resources = new List<IWin32Resource>();
    }

    public void Copy(IModule module, IInternFactory internFactory) {
      ((ICopyFrom<IUnit>)this).Copy(module, internFactory);
      this.strings = new List<string>(module.GetStrings());
      this.allTypes = new List<INamedTypeDefinition>(module.GetAllTypes());
      this.assemblyReferences = new List<IAssemblyReference>(module.AssemblyReferences);
      this.baseAddress = module.BaseAddress;
      this.containingAssembly = module.ContainingAssembly;
      this.dllCharacteristics = module.DllCharacteristics;
      if (module.Kind == ModuleKind.ConsoleApplication || module.Kind == ModuleKind.WindowsApplication)
        this.entryPoint = module.EntryPoint;
      else
        this.entryPoint = Dummy.MethodReference;
      this.fileAlignment = module.FileAlignment;
      this.ilOnly = module.ILOnly;
      this.kind = module.Kind;
      this.linkerMajorVersion = module.LinkerMajorVersion;
      this.linkerMinorVersion = module.LinkerMinorVersion;
      this.metadataFormatMajorVersion = module.MetadataFormatMajorVersion;
      this.metadataFormatMinorVersion = module.MetadataFormatMinorVersion;
      this.moduleAttributes = new List<ICustomAttribute>(module.ModuleAttributes);
      this.moduleReferences = new List<IModuleReference>(module.ModuleReferences);
      this.persistentIdentifier = Guid.NewGuid();
      this.requiresAmdInstructionSet = module.RequiresAmdInstructionSet;
      this.requires32bits = module.Requires32bits;
      this.requires64bits = module.Requires64bits;
      this.sizeOfHeapCommit = module.SizeOfHeapCommit;
      this.sizeOfHeapReserve = module.SizeOfHeapReserve;
      this.sizeOfStackCommit = module.SizeOfStackCommit;
      this.sizeOfStackReserve = module.SizeOfStackReserve;
      this.targetRuntimeVersion = module.TargetRuntimeVersion;
      this.trackDebugData = module.TrackDebugData;
      this.usePublicKeyTokensForAssemblyReferences = module.UsePublicKeyTokensForAssemblyReferences;
      this.win32Resources = new List<IWin32Resource>(module.Win32Resources);
    }

    public List<INamedTypeDefinition> AllTypes {
      get { return this.allTypes; }
      set { this.allTypes = value; }
    }
    List<INamedTypeDefinition> allTypes;

    public List<IAssemblyReference> AssemblyReferences {
      get { return this.assemblyReferences; }
      set { this.assemblyReferences = value; }
    }
    List<IAssemblyReference> assemblyReferences;

    public ulong BaseAddress {
      get { return this.baseAddress; }
      set { this.baseAddress = value; }
    }
    ulong baseAddress;

    public virtual IAssembly/*?*/ ContainingAssembly {
      get { return this.containingAssembly; }
      set { this.containingAssembly = value; }
    }
    IAssembly/*?*/ containingAssembly;

    public virtual ushort DllCharacteristics {
      get { return this.dllCharacteristics; }
      set { this.dllCharacteristics = value; }
    }
    ushort dllCharacteristics;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IMethodReference EntryPoint {
      get { return this.entryPoint; }
      set { this.entryPoint = value; }
    }
    IMethodReference entryPoint;

    public uint FileAlignment {
      get { return this.fileAlignment; }
      set { this.fileAlignment = value; }
    }
    uint fileAlignment;

    public bool ILOnly {
      get { return this.ilOnly; }
      set { this.ilOnly = value; }
    }
    bool ilOnly;

    public ModuleKind Kind {
      get { return this.kind; }
      set { this.kind = value; }
    }
    ModuleKind kind;

    public byte LinkerMajorVersion {
      get { return this.linkerMajorVersion; }
      set { this.linkerMajorVersion = value; }
    }
    byte linkerMajorVersion;

    public byte LinkerMinorVersion {
      get { return this.linkerMinorVersion; }
      set { this.linkerMinorVersion = value; }
    }
    byte linkerMinorVersion;

    public byte MetadataFormatMajorVersion {
      get { return this.metadataFormatMajorVersion; }
      set { this.metadataFormatMajorVersion = value; }
    }
    byte metadataFormatMajorVersion;

    public byte MetadataFormatMinorVersion {
      get { return this.metadataFormatMinorVersion; }
      set { this.metadataFormatMinorVersion = value; }
    }
    byte metadataFormatMinorVersion = 0;

    public List<ICustomAttribute> ModuleAttributes {
      get { return this.moduleAttributes; }
      set { this.moduleAttributes = value; }
    }
    List<ICustomAttribute> moduleAttributes;

    public virtual IName ModuleName {
      get { return this.Name; }
      set { this.Name = value; }
    }

    public List<IModuleReference> ModuleReferences {
      get { return this.moduleReferences; }
      set { this.moduleReferences = value; }
    }
    List<IModuleReference> moduleReferences;

    public Guid PersistentIdentifier {
      get { return this.persistentIdentifier; }
      set { this.persistentIdentifier = value; }
    }
    Guid persistentIdentifier;

    public bool RequiresAmdInstructionSet {
      get { return this.requiresAmdInstructionSet; }
      set { this.requiresAmdInstructionSet = value; }
    }
    bool requiresAmdInstructionSet;

    public bool Requires32bits {
      get { return this.requires32bits; }
      set { this.requires32bits = value; }
    }
    bool requires32bits;

    public bool Requires64bits {
      get { return this.requires64bits; }
      set { this.requires64bits = value; }
    }
    bool requires64bits;

    public ulong SizeOfHeapCommit {
      get { return this.sizeOfHeapCommit; }
      set { this.sizeOfHeapCommit = value; }
    }
    ulong sizeOfHeapCommit;

    public ulong SizeOfHeapReserve {
      get { return this.sizeOfHeapReserve; }
      set { this.sizeOfHeapReserve = value; }
    }
    ulong sizeOfHeapReserve;

    public ulong SizeOfStackCommit {
      get { return this.sizeOfStackCommit; }
      set { this.sizeOfStackCommit = value; }
    }
    ulong sizeOfStackCommit;

    public ulong SizeOfStackReserve {
      get { return this.sizeOfStackReserve; }
      set { this.sizeOfStackReserve = value; }
    }
    ulong sizeOfStackReserve;

    public List<string> Strings {
      get { return this.strings; }
      set { this.strings = value; }
    }
    List<string> strings;

    public string TargetRuntimeVersion {
      get { return this.targetRuntimeVersion; }
      set { this.targetRuntimeVersion = value; }
    }
    string targetRuntimeVersion;

    public bool TrackDebugData {
      get { return this.trackDebugData; }
      set { this.trackDebugData = value; }
    }
    bool trackDebugData;

    public override IEnumerable<IUnitReference> UnitReferences {
      get {
        foreach (IAssemblyReference assemblyReference in this.AssemblyReferences)
          yield return assemblyReference;
        foreach (IModuleReference moduleReference in this.ModuleReferences)
          yield return moduleReference;
      }
    }

    public bool UsePublicKeyTokensForAssemblyReferences {
      get { return this.usePublicKeyTokensForAssemblyReferences; }
      set { this.usePublicKeyTokensForAssemblyReferences = value; }
    }
    bool usePublicKeyTokensForAssemblyReferences;

    public List<IWin32Resource> Win32Resources {
      get { return this.win32Resources; }
      set { this.win32Resources = value; }
    }
    List<IWin32Resource> win32Resources;

    #region IModule Members


    IEnumerable<IAssemblyReference> IModule.AssemblyReferences {
      get { return this.assemblyReferences.AsReadOnly(); }
    }

    IEnumerable<string> IModule.GetStrings() {
      return this.strings.AsReadOnly();
    }

    IEnumerable<INamedTypeDefinition> IModule.GetAllTypes() {
      return this.allTypes.AsReadOnly();
    }

    IEnumerable<ICustomAttribute> IModule.ModuleAttributes {
      get { return this.moduleAttributes.AsReadOnly(); }
    }

    IEnumerable<IModuleReference> IModule.ModuleReferences {
      get { return this.moduleReferences.AsReadOnly(); }
    }

    IEnumerable<IWin32Resource> IModule.Win32Resources {
      get { return this.win32Resources.AsReadOnly(); }
    }

    #endregion

    public override UnitIdentity UnitIdentity {
      get {
        return this.ModuleIdentity;
      }
    }

    #region IModuleReference Members

    public virtual ModuleIdentity ModuleIdentity {
      get {
        if (this.moduleIdentity == null) {
          this.moduleIdentity = UnitHelper.GetModuleIdentity(this);
        }
        return this.moduleIdentity;
      }
    }
    ModuleIdentity/*?*/ moduleIdentity;

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return this.ContainingAssembly; }
    }

    public IModule ResolvedModule {
      get { return this; }
    }

    #endregion

    public override IUnit ResolvedUnit {
      get { return this; }
    }
  }

  public class ModuleReference : UnitReference, IModuleReference, ICopyFrom<IModuleReference> {

    public ModuleReference() {
      this.containingAssembly = Dummy.Assembly;
      this.moduleIdentity = Dummy.ModuleReference.ModuleIdentity;
      this.resolvedModule = Dummy.Module;
    }

    public void Copy(IModuleReference moduleReference, IInternFactory internFactory) {
      ((ICopyFrom<IUnitReference>)this).Copy(moduleReference, internFactory);
      this.containingAssembly = moduleReference.ContainingAssembly;
      this.moduleIdentity = moduleReference.ModuleIdentity;
      this.resolvedModule = moduleReference.ResolvedModule;
    }

    public IAssemblyReference ContainingAssembly {
      get { return this.containingAssembly; }
      set { this.containingAssembly = value; }
    }
    IAssemblyReference containingAssembly;

    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public ModuleIdentity ModuleIdentity {
      get { return this.moduleIdentity; }
      set { this.moduleIdentity = value; }
    }
    ModuleIdentity moduleIdentity;

    public IModule ResolvedModule {
      get { return this.resolvedModule; }
      set { this.resolvedModule = value; }
    }
    IModule resolvedModule;

    public override IUnit ResolvedUnit {
      get { return this.ResolvedModule; }
    }

    public override UnitIdentity UnitIdentity {
      get { return this.ModuleIdentity; }
    }

  }

  public abstract class Unit : UnitReference, IUnit, ICopyFrom<IUnit> {

    internal Unit() {
      this.contractAssemblySymbolicIdentity = Dummy.Assembly.AssemblyIdentity;
      this.coreAssemblySymbolicIdentity = Dummy.Assembly.AssemblyIdentity;
      this.location = "";
      this.platformType = Dummy.PlatformType;
      this.unitNamespaceRoot = Dummy.RootUnitNamespace;
    }

    public void Copy(IUnit unit, IInternFactory internFactory) {
      ((ICopyFrom<IUnitReference>)this).Copy(unit, internFactory);
      this.contractAssemblySymbolicIdentity = unit.CoreAssemblySymbolicIdentity;
      this.coreAssemblySymbolicIdentity = unit.CoreAssemblySymbolicIdentity;
      this.location = unit.Location;
      this.platformType = unit.PlatformType;
      this.unitNamespaceRoot = unit.UnitNamespaceRoot;
    }

    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { return this.contractAssemblySymbolicIdentity; }
      set { this.contractAssemblySymbolicIdentity = value; }
    }
    AssemblyIdentity contractAssemblySymbolicIdentity;

    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { return this.coreAssemblySymbolicIdentity; }
      set { this.coreAssemblySymbolicIdentity = value; }
    }
    AssemblyIdentity coreAssemblySymbolicIdentity;

    public string Location {
      get { return this.location; }
      set { this.location = value; }
    }
    string location;

    public IPlatformType PlatformType {
      get { return this.platformType; }
      set { this.platformType = value; }
    }
    IPlatformType platformType;

    public IRootUnitNamespace UnitNamespaceRoot {
      get { return this.unitNamespaceRoot; }
      set
        //^ requires value.Unit == this;
        //^ requires value.RootOwner == this;
      {
        this.unitNamespaceRoot = value;
      }
    }
    IRootUnitNamespace unitNamespaceRoot;

    public abstract IEnumerable<IUnitReference> UnitReferences {
      get;
    }

    #region INamespaceRootOwner Members

    public INamespaceDefinition NamespaceRoot {
      get { return this.UnitNamespaceRoot; }
    }

    #endregion

  }

  public abstract class UnitReference : IUnitReference, ICopyFrom<IUnitReference> {

    internal UnitReference() {
      this.attributes = new List<ICustomAttribute>();
      this.locations = new List<ILocation>();
      this.name = Dummy.Name;
    }

    public void Copy(IUnitReference unitReference, IInternFactory internFactory) {
      this.attributes = new List<ICustomAttribute>(unitReference.Attributes);
      this.locations = new List<ILocation>(unitReference.Locations);
      this.name = unitReference.Name;
    }

    public List<ICustomAttribute> Attributes {
      get { return this.attributes; }
      set { this.attributes = value; }
    }
    List<ICustomAttribute> attributes;

    public abstract void Dispatch(IMetadataVisitor visitor);

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

    public abstract IUnit ResolvedUnit {
      get;
    }

    public abstract UnitIdentity UnitIdentity {
      get;
    }

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.attributes.AsReadOnly(); }
    }

    IEnumerable<ILocation> IObjectWithLocations.Locations {
      get { return this.locations.AsReadOnly(); }
    }

    #endregion
  }

}
