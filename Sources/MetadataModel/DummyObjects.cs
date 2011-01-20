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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
//^ using Microsoft.Contracts;

//  TODO: Sometime make the methods and properties of dummy objects Explicit impls so
//  that we can track addition and removal of methods and properties.
namespace Microsoft.Cci {

#pragma warning disable 1591

  public abstract class Dummy {

    public static IAliasForType AliasForType {
      [DebuggerNonUserCode]
      get {
        if (Dummy.aliasForType == null)
          Interlocked.CompareExchange(ref Dummy.aliasForType, new DummyAliasForType(), null);
        return Dummy.aliasForType;
      }
    }
    private static IAliasForType/*?*/ aliasForType;

    public static IMetadataHost CompilationHostEnvironment {
      [DebuggerNonUserCode]
      get {
        if (Dummy.compilationHostEnvironment == null)
          Interlocked.CompareExchange(ref Dummy.compilationHostEnvironment, new DummyMetadataHost(), null);
        return Dummy.compilationHostEnvironment;
      }
    }
    private static IMetadataHost/*?*/ compilationHostEnvironment;

    public static IMetadataConstant Constant {
      [DebuggerNonUserCode]
      get {
        if (Dummy.constant == null)
          Interlocked.CompareExchange(ref Dummy.constant, new DummyMetadataConstant(), null);
        return Dummy.constant;
      }
    }
    private static IMetadataConstant/*?*/ constant;

    public static ICustomModifier CustomModifier {
      [DebuggerNonUserCode]
      get {
        if (Dummy.customModifier == null)
          Interlocked.CompareExchange(ref Dummy.customModifier, new DummyCustomModifier(), null);
        return Dummy.customModifier;
      }
    }
    private static ICustomModifier/*?*/ customModifier;

    public static IEventDefinition Event {
      [DebuggerNonUserCode]
      get {
        if (Dummy.@event == null)
          Dummy.@event = new DummyEventDefinition();
        return Dummy.@event;
      }
    }
    private static IEventDefinition/*?*/ @event;

    public static IFieldDefinition Field {
      [DebuggerNonUserCode]
      get {
        if (Dummy.field == null)
          Interlocked.CompareExchange(ref Dummy.field, new DummyFieldDefinition(), null);
        return Dummy.field;
      }
    }
    private static IFieldDefinition/*?*/ field;

    public static IMetadataExpression Expression {
      [DebuggerNonUserCode]
      get {
        if (Dummy.expression == null)
          Interlocked.CompareExchange(ref Dummy.expression, new DummyMetadataExpression(), null);
        return Dummy.expression;
      }
    }
    private static IMetadataExpression/*?*/ expression;

    public static IFunctionPointer FunctionPointer {
      [DebuggerNonUserCode]
      get {
        if (Dummy.functionPointer == null)
          Interlocked.CompareExchange(ref Dummy.functionPointer, new DummyFunctionPointerType(), null);
        return Dummy.functionPointer;
      }
    }
    private static IFunctionPointer/*?*/ functionPointer;

    public static IGenericMethodParameter GenericMethodParameter {
      [DebuggerNonUserCode]
      get {
        if (Dummy.genericMethodParameter == null)
          Interlocked.CompareExchange(ref Dummy.genericMethodParameter, new DummyGenericMethodParameter(), null);
        return Dummy.genericMethodParameter;
      }
    }
    private static DummyGenericMethodParameter/*?*/ genericMethodParameter;

    public static IGenericTypeInstance GenericTypeInstance {
      [DebuggerNonUserCode]
      get
        //^ ensures !result.IsGeneric;
      {
        if (Dummy.genericTypeInstance == null)
          Interlocked.CompareExchange(ref Dummy.genericTypeInstance, new DummyGenericTypeInstance(), null);
        DummyGenericTypeInstance result = Dummy.genericTypeInstance;
        //^ assume !result.IsGeneric; //the post condition says so
        return result;
      }
    }
    private static DummyGenericTypeInstance/*?*/ genericTypeInstance;

    public static IGenericTypeParameter GenericTypeParameter {
      get {
        if (Dummy.genericTypeParameter == null)
          Interlocked.CompareExchange(ref Dummy.genericTypeParameter, new DummyGenericTypeParameter(), null);
        return Dummy.genericTypeParameter;
      }
    }
    private static IGenericTypeParameter/*?*/ genericTypeParameter;

    public static IMethodDefinition Method {
      [DebuggerNonUserCode]
      get {
        if (Dummy.method == null)
          Interlocked.CompareExchange(ref Dummy.method, new DummyMethodDefinition(), null);
        return Dummy.method;
      }
    }
    private static IMethodDefinition/*?*/ method;

    public static IMethodBody MethodBody {
      [DebuggerNonUserCode]
      get {
        if (Dummy.methodBody == null)
          Interlocked.CompareExchange(ref Dummy.methodBody, new DummyMethodBody(), null);
        return Dummy.methodBody;
      }
    }
    private static IMethodBody/*?*/ methodBody;

    public static IName Name {
      [DebuggerNonUserCode]
      get {
        if (Dummy.name == null)
          Interlocked.CompareExchange(ref Dummy.name, new DummyName(), null);
        return Dummy.name;
      }
    }
    private static IName/*?*/ name;

    public static IMetadataNamedArgument NamedArgument {
      [DebuggerNonUserCode]
      get {
        if (Dummy.namedArgument == null)
          Interlocked.CompareExchange(ref Dummy.namedArgument, new DummyNamedArgument(), null);
        return Dummy.namedArgument;
      }
    }
    private static IMetadataNamedArgument/*?*/ namedArgument;

    public static INameTable NameTable {
      [DebuggerNonUserCode]
      get {
        if (Dummy.nameTable == null)
          Interlocked.CompareExchange(ref Dummy.nameTable, new DummyNameTable(), null);
        return Dummy.nameTable;
      }
    }
    private static INameTable/*?*/ nameTable;

    public static INestedTypeDefinition NestedType {
      [DebuggerNonUserCode]
      get {
        if (Dummy.nestedType == null)
          Interlocked.CompareExchange(ref Dummy.nestedType, new DummyNestedType(), null);
        return Dummy.nestedType;
      }
    }
    private static INestedTypeDefinition/*?*/ nestedType;

    public static IPlatformType PlatformType {
      [DebuggerNonUserCode]
      get {
        if (Dummy.platformType == null)
          Interlocked.CompareExchange(ref Dummy.platformType, new DummyPlatformType(), null);
        return Dummy.platformType;
      }
    }
    private static IPlatformType/*?*/ platformType;

    public static IPropertyDefinition Property {
      [DebuggerNonUserCode]
      get {
        if (Dummy.property == null)
          Interlocked.CompareExchange(ref Dummy.property, new DummyPropertyDefinition(), null);
        return Dummy.property;
      }
    }
    private static IPropertyDefinition/*?*/ property;

    public static ITypeDefinition Type {
      [DebuggerNonUserCode]
      get {
        if (Dummy.type == null)
          Interlocked.CompareExchange(ref Dummy.type, new DummyType(), null);
        return Dummy.type;
      }
    }
    private static ITypeDefinition/*?*/ type;

    public static ITypeReference TypeReference {
      [DebuggerNonUserCode]
      get {
        if (Dummy.typeReference == null)
          Interlocked.CompareExchange(ref Dummy.typeReference, new DummyTypeReference(), null);
        return Dummy.typeReference;
      }
    }
    private static ITypeReference/*?*/ typeReference;

    public static IUnit Unit {
      [DebuggerNonUserCode]
      get {
        if (Dummy.unit == null)
          Interlocked.CompareExchange(ref Dummy.unit, new DummyUnit(), null);
        return Dummy.unit;
      }
    }
    private static IUnit/*?*/ unit;

    public static IRootUnitNamespace RootUnitNamespace {
      [DebuggerNonUserCode]
      get {
        if (Dummy.rootUnitNamespace == null)
          Interlocked.CompareExchange(ref Dummy.rootUnitNamespace, new DummyRootUnitNamespace(), null);
        return Dummy.rootUnitNamespace;
      }
    }
    private static IRootUnitNamespace/*?*/ rootUnitNamespace;

    public static INestedUnitNamespace NestedUnitNamespace {
      [DebuggerNonUserCode]
      get {
        if (Dummy.nestedUnitNamespace == null)
          Interlocked.CompareExchange(ref Dummy.nestedUnitNamespace, new DummyNestedUnitNamespace(), null);
        return Dummy.nestedUnitNamespace;
      }
    }
    private static INestedUnitNamespace/*?*/ nestedUnitNamespace;

    public static IUnitSet UnitSet {
      [DebuggerNonUserCode]
      get {
        if (Dummy.unitSet == null)
          Interlocked.CompareExchange(ref Dummy.unitSet, new DummyUnitSet(), null);
        return Dummy.unitSet;
      }
    }
    private static IUnitSet/*?*/ unitSet;

    public static IRootUnitSetNamespace RootUnitSetNamespace {
      [DebuggerNonUserCode]
      get {
        if (Dummy.rootUnitSetNamespace == null)
          Interlocked.CompareExchange(ref Dummy.rootUnitSetNamespace, new DummyRootUnitSetNamespace(), null);
        return Dummy.rootUnitSetNamespace;
      }
    }
    private static IRootUnitSetNamespace/*?*/ rootUnitSetNamespace;

    public static IModule Module {
      [DebuggerNonUserCode]
      get {
        if (Dummy.module == null)
          Interlocked.CompareExchange(ref Dummy.module, new DummyModule(), null);
        return Dummy.module;
      }
    }
    private static IModule/*?*/ module;

    public static IAssembly Assembly {
      [DebuggerNonUserCode]
      get {
        if (Dummy.assembly == null)
          Interlocked.CompareExchange(ref Dummy.assembly, new DummyAssembly(), null);
        return Dummy.assembly;
      }
    }
    private static IAssembly/*?*/ assembly;

    public static IMethodReference MethodReference {
      [DebuggerNonUserCode]
      get {
        if (Dummy.methodReference == null)
          Interlocked.CompareExchange(ref Dummy.methodReference, new DummyMethodReference(), null);
        return Dummy.methodReference;
      }
    }
    private static IMethodReference/*?*/ methodReference;

    public static Version Version {
      [DebuggerNonUserCode]
      get {
        if (Dummy.version == null)
          Dummy.version = new Version(0, 0);
        return Dummy.version;
      }
    }
    private static Version/*?*/ version;

    public static ICustomAttribute CustomAttribute {
      [DebuggerNonUserCode]
      get {
        if (Dummy.customAttribute == null)
          Interlocked.CompareExchange(ref Dummy.customAttribute, new DummyCustomAttribute(), null);
        return Dummy.customAttribute;
      }
    }
    private static ICustomAttribute/*?*/ customAttribute;

    public static IFileReference FileReference {
      [DebuggerNonUserCode]
      get {
        if (Dummy.fileReference == null)
          Interlocked.CompareExchange(ref Dummy.fileReference, new DummyFileReference(), null);
        return Dummy.fileReference;
      }
    }
    private static IFileReference/*?*/ fileReference;

    public static IResource Resource {
      [DebuggerNonUserCode]
      get {
        if (Dummy.resource == null)
          Interlocked.CompareExchange(ref Dummy.resource, new DummyResource(), null);
        return Dummy.resource;
      }
    }
    private static IResource/*?*/ resource;

    public static IModuleReference ModuleReference {
      [DebuggerNonUserCode]
      get {
        if (Dummy.moduleReference == null)
          Interlocked.CompareExchange(ref Dummy.moduleReference, new DummyModuleReference(), null);
        return Dummy.moduleReference;
      }
    }
    private static IModuleReference/*?*/ moduleReference;

    public static IAssemblyReference AssemblyReference {
      [DebuggerNonUserCode]
      get {
        if (Dummy.assemblyReference == null)
          Interlocked.CompareExchange(ref Dummy.assemblyReference, new DummyAssemblyReference(), null);
        return Dummy.assemblyReference;
      }
    }
    private static IAssemblyReference/*?*/ assemblyReference;

    public static IMarshallingInformation MarshallingInformation {
      [DebuggerNonUserCode]
      get {
        if (Dummy.marshallingInformation == null)
          Interlocked.CompareExchange(ref Dummy.marshallingInformation, new DummyMarshallingInformation(), null);
        return Dummy.marshallingInformation;
      }
    }
    private static IMarshallingInformation/*?*/ marshallingInformation;

    public static ISecurityAttribute SecurityAttribute {
      [DebuggerNonUserCode]
      get {
        if (Dummy.securityAttribute == null)
          Interlocked.CompareExchange(ref Dummy.securityAttribute, new DummySecurityAttribute(), null);
        return Dummy.securityAttribute;
      }
    }
    private static ISecurityAttribute/*?*/ securityAttribute;

    public static IParameterTypeInformation ParameterTypeInformation {
      [DebuggerNonUserCode]
      get {
        if (Dummy.parameterTypeInformation == null)
          Interlocked.CompareExchange(ref Dummy.parameterTypeInformation, new DummyParameterTypeInformation(), null);
        return Dummy.parameterTypeInformation;
      }
    }
    private static IParameterTypeInformation/*?*/ parameterTypeInformation;

    public static INamespaceTypeDefinition NamespaceTypeDefinition {
      [DebuggerNonUserCode]
      get {
        if (Dummy.namespaceTypeDefinition == null)
          Interlocked.CompareExchange(ref Dummy.namespaceTypeDefinition, new DummyNamespaceTypeDefinition(), null);
        return Dummy.namespaceTypeDefinition;
      }
    }
    private static INamespaceTypeDefinition/*?*/ namespaceTypeDefinition;

    public static INamespaceTypeReference NamespaceTypeReference {
      [DebuggerNonUserCode]
      get {
        if (Dummy.namespaceTypeReference == null)
          Interlocked.CompareExchange(ref Dummy.namespaceTypeReference, new DummyNamespaceTypeReference(), null);
        return Dummy.namespaceTypeReference;
      }
    }
    private static INamespaceTypeReference/*?*/ namespaceTypeReference;

    public static ISpecializedNestedTypeDefinition SpecializedNestedTypeDefinition {
      [DebuggerNonUserCode]
      get {
        if (Dummy.specializedNestedTypeDefinition == null)
          Interlocked.CompareExchange(ref Dummy.specializedNestedTypeDefinition, new DummySpecializedNestedTypeDefinition(), null);
        return Dummy.specializedNestedTypeDefinition;
      }
    }
    private static ISpecializedNestedTypeDefinition/*?*/ specializedNestedTypeDefinition;

    public static ISpecializedFieldDefinition SpecializedFieldDefinition {
      [DebuggerNonUserCode]
      get {
        if (Dummy.specializedFieldDefinition == null)
          Interlocked.CompareExchange(ref Dummy.specializedFieldDefinition, new DummySpecializedFieldDefinition(), null);
        return Dummy.specializedFieldDefinition;
      }
    }
    private static ISpecializedFieldDefinition/*?*/ specializedFieldDefinition;

    public static ISpecializedMethodDefinition SpecializedMethodDefinition {
      [DebuggerNonUserCode]
      get {
        if (Dummy.specializedMethodDefinition == null)
          Interlocked.CompareExchange(ref Dummy.specializedMethodDefinition, new DummySpecializedMethodDefinition(), null);
        return Dummy.specializedMethodDefinition;
      }
    }
    private static ISpecializedMethodDefinition/*?*/ specializedMethodDefinition;

    public static ISpecializedPropertyDefinition SpecializedPropertyDefinition {
      [DebuggerNonUserCode]
      get {
        if (Dummy.specializedPropertyDefinition == null)
          Interlocked.CompareExchange(ref Dummy.specializedPropertyDefinition, new DummySpecializedPropertyDefinition(), null);
        return Dummy.specializedPropertyDefinition;
      }
    }
    private static ISpecializedPropertyDefinition/*?*/ specializedPropertyDefinition;

    public static ILocalDefinition LocalVariable {
      [DebuggerNonUserCode]
      get {
        if (Dummy.localVariable == null)
          Interlocked.CompareExchange(ref Dummy.localVariable, new DummyLocalVariable(), null);
        return Dummy.localVariable;
      }
    }
    private static ILocalDefinition/*?*/ localVariable;

    public static IFieldReference FieldReference {
      [DebuggerNonUserCode]
      get {
        if (Dummy.fieldReference == null)
          Interlocked.CompareExchange(ref Dummy.fieldReference, new DummyFieldReference(), null);
        return Dummy.fieldReference;
      }
    }
    private static IFieldReference/*?*/ fieldReference;

    public static IParameterDefinition ParameterDefinition {
      [DebuggerNonUserCode]
      get {
        if (Dummy.parameterDefinition == null)
          Interlocked.CompareExchange(ref Dummy.parameterDefinition, new DummyParameterDefinition(), null);
        return Dummy.parameterDefinition;
      }
    }
    private static IParameterDefinition/*?*/ parameterDefinition;

    public static ISectionBlock SectionBlock {
      [DebuggerNonUserCode]
      get {
        if (Dummy.sectionBlock == null)
          Interlocked.CompareExchange(ref Dummy.sectionBlock, new DummySectionBlock(), null);
        return Dummy.sectionBlock;
      }
    }
    private static ISectionBlock/*?*/ sectionBlock;

    public static IPlatformInvokeInformation PlatformInvokeInformation {
      [DebuggerNonUserCode]
      get {
        if (Dummy.platformInvokeInformation == null)
          Interlocked.CompareExchange(ref Dummy.platformInvokeInformation, new DummyPlatformInvokeInformation(), null);
        return Dummy.platformInvokeInformation;
      }
    }
    private static IPlatformInvokeInformation/*?*/ platformInvokeInformation;

    public static IGlobalMethodDefinition GlobalMethod {
      [DebuggerNonUserCode]
      get {
        if (Dummy.globalMethodDefinition == null)
          Interlocked.CompareExchange(ref Dummy.globalMethodDefinition, new DummyGlobalMethodDefinition(), null);
        return Dummy.globalMethodDefinition;
      }
    }
    private static IGlobalMethodDefinition/*?*/ globalMethodDefinition;

    public static IGlobalFieldDefinition GlobalField {
      [DebuggerNonUserCode]
      get {
        if (Dummy.globalFieldDefinition == null)
          Interlocked.CompareExchange(ref Dummy.globalFieldDefinition, new DummyGlobalFieldDefinition(), null);
        return Dummy.globalFieldDefinition;
      }
    }
    private static IGlobalFieldDefinition/*?*/ globalFieldDefinition;

    public static IOperation Operation {
      [DebuggerNonUserCode]
      get {
        if (Dummy.operation == null)
          Interlocked.CompareExchange(ref Dummy.operation, new DummyOperation(), null);
        return Dummy.operation;
      }
    }
    private static IOperation/*?*/ operation;

    public static ILocation Location {
      [DebuggerNonUserCode]
      get {
        if (Dummy.location == null)
          Interlocked.CompareExchange(ref Dummy.location, new DummyLocation(), null);
        return Dummy.location;
      }
    }
    private static ILocation/*?*/ location;

    public static IDocument Document {
      [DebuggerNonUserCode]
      get {
        if (Dummy.document == null)
          Interlocked.CompareExchange(ref Dummy.document, new DummyDocument(), null);
        return Dummy.document;
      }
    }
    private static IDocument/*?*/ document;

    public static IOperationExceptionInformation OperationExceptionInformation {
      [DebuggerNonUserCode]
      get {
        if (Dummy.operationExceptionInformation == null)
          Interlocked.CompareExchange(ref Dummy.operationExceptionInformation, new DummyOperationExceptionInformation(), null);
        return Dummy.operationExceptionInformation;
      }
    }
    private static IOperationExceptionInformation/*?*/ operationExceptionInformation;

    public static IInternFactory InternFactory {
      [DebuggerNonUserCode]
      get {
        if (Dummy.internFactory == null)
          Interlocked.CompareExchange(ref Dummy.internFactory, new DummyInternFactory(), null);
        return Dummy.internFactory;
      }
    }
    private static IInternFactory/*?*/ internFactory;

    public static IArrayType ArrayType {
      [DebuggerNonUserCode]
      get {
        if (Dummy.arrayType == null)
          Interlocked.CompareExchange(ref Dummy.arrayType, new DummyArrayType(), null);
        return Dummy.arrayType;
      }
    }
    private static IArrayType/*?*/ arrayType;
  }

  internal sealed class DummyAliasForType : Dummy, IAliasForType {
    #region IAliasForType Members

    public ITypeReference AliasedType {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region IContainer<IAliasMember> Members

    public IEnumerable<IAliasMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<IAliasMember>(); }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IScope<IAliasMember> Members

    public bool Contains(IAliasMember member) {
      return false;
    }

    public IEnumerable<IAliasMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<IAliasMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<IAliasMember>();
    }

    public IEnumerable<IAliasMember> GetMatchingMembers(Function<IAliasMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<IAliasMember>();
    }

    public IEnumerable<IAliasMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<IAliasMember>();
    }

    #endregion
  }

  internal sealed class DummyAssembly : Dummy, IAssembly {
    #region IAssembly Members

    public IEnumerable<ICustomAttribute> AssemblyAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public string Culture {
      get { return string.Empty; }
    }

    public IEnumerable<IAliasForType> ExportedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<IAliasForType>(); }
    }

    public IEnumerable<IResourceReference> Resources {
      get { return IteratorHelper.GetEmptyEnumerable<IResourceReference>(); }
    }

    public IEnumerable<IFileReference> Files {
      get { return IteratorHelper.GetEmptyEnumerable<IFileReference>(); }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public IEnumerable<IModule> MemberModules {
      get { return IteratorHelper.GetEmptyEnumerable<IModule>(); }
    }

    public uint Flags {
      get { return 0; }
    }

    public IEnumerable<byte> PublicKey {
      get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
    }

    public new Version Version {
      get { return Dummy.Version; }
    }

    public AssemblyIdentity AssemblyIdentity {
      get {
        return new AssemblyIdentity(Dummy.Name, string.Empty, new Version(0, 0), new byte[0], string.Empty);
      }
    }

    #endregion

    #region IModule Members

    public IName ModuleName {
      get {
        return Dummy.Name;
      }
    }

    public IAssembly/*?*/ ContainingAssembly {
      get {
        return this;
      }
    }

    public IEnumerable<IAssemblyReference> AssemblyReferences {
      get { return IteratorHelper.GetEmptyEnumerable<IAssemblyReference>(); }
    }

    public ulong BaseAddress {
      get { return 0; }
    }

    public ushort DllCharacteristics {
      get { return 0; }
    }

    public IMethodReference EntryPoint {
      get { return Dummy.MethodReference; }
    }

    public uint FileAlignment {
      get { return 0; }
    }

    public bool ILOnly {
      get { return false; }
    }

    public ModuleKind Kind {
      get { return ModuleKind.ConsoleApplication; }
    }

    public byte LinkerMajorVersion {
      get { return 0; }
    }

    public byte LinkerMinorVersion {
      get { return 0; }
    }

    public byte MetadataFormatMajorVersion {
      get { return 0; }
    }

    public byte MetadataFormatMinorVersion {
      get { return 0; }
    }

    public IEnumerable<ICustomAttribute> ModuleAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<IModuleReference> ModuleReferences {
      get { return IteratorHelper.GetEmptyEnumerable<IModuleReference>(); }
    }

    public Guid PersistentIdentifier {
      get { return Guid.Empty; }
    }

    public Machine Machine {
      get { return Machine.Unknown; }
    }

    public bool RequiresAmdInstructionSet {
      get { return false; }
    }

    public bool RequiresStartupStub {
      get { return false; }
    }

    public bool Requires32bits {
      get { return false; }
    }

    public bool Requires64bits {
      get { return false; }
    }

    public ulong SizeOfHeapReserve {
      get { return 0; }
    }

    public ulong SizeOfHeapCommit {
      get { return 0; }
    }

    public ulong SizeOfStackReserve {
      get { return 0; }
    }

    public ulong SizeOfStackCommit {
      get { return 0; }
    }

    public string TargetRuntimeVersion {
      get { return string.Empty; }
    }

    public bool TrackDebugData {
      get { return false; }
    }

    public bool UsePublicKeyTokensForAssemblyReferences {
      get { return false; }
    }

    public IEnumerable<IWin32Resource> Win32Resources {
      get { return IteratorHelper.GetEmptyEnumerable<IWin32Resource>(); }
    }

    public IEnumerable<string> GetStrings() {
      return IteratorHelper.GetEmptyEnumerable<string>();
    }

    public IEnumerable<INamedTypeDefinition> GetAllTypes() {
      return IteratorHelper.GetEmptyEnumerable<INamedTypeDefinition>();
    }

    public ModuleIdentity ModuleIdentity {
      get {
        return this.AssemblyIdentity;
      }
    }

    #endregion

    #region IUnit Members

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public new string Location {
      get { return string.Empty; }
    }

    public new IName Name {
      get { return Dummy.Name; }
    }

    public IRootUnitNamespace UnitNamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitNamespace;
      }
    }

    public IEnumerable<IUnitReference> UnitReferences {
      get { return IteratorHelper.GetEmptyEnumerable<IUnitReference>(); }
    }

    public UnitIdentity UnitIdentity {
      get {
        return this.AssemblyIdentity;
      }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region INamespaceRootOwner Members

    public INamespaceDefinition NamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitNamespace;
      }
    }

    #endregion

    #region IUnitReference Members

    public IUnit ResolvedUnit {
      get { return this; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IModuleReference Members

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return null; }
    }

    public IModule ResolvedModule {
      get { return this; }
    }

    #endregion

    #region IAssemblyReference Members

    public IEnumerable<IName> Aliases {
      get { return IteratorHelper.GetEmptyEnumerable<IName>(); }
    }

    public bool IsRetargetable {
      get { return false; }
    }

    public IAssembly ResolvedAssembly {
      get { return this; }
    }

    public IEnumerable<byte> PublicKeyToken {
      get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
    }

    public AssemblyIdentity UnifiedAssemblyIdentity {
      get { return this.AssemblyIdentity; }
    }

    #endregion

  }

  internal sealed class DummyMetadataHost : Dummy, IMetadataHost {

    #region ICompilationHostEnvironment Members

    public event EventHandler<ErrorEventArgs> Errors;

    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public AssemblyIdentity SystemCoreAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public IAssembly FindAssembly(AssemblyIdentity assemblyIdentity) {
      return Dummy.Assembly;
    }

    public IModule FindModule(ModuleIdentity moduleIdentity) {
      return Dummy.Module;
    }

    public IUnit FindUnit(UnitIdentity unitIdentity) {
      return Dummy.Unit;
    }

    public IAssembly LoadAssembly(AssemblyIdentity assemblyIdentity) {
      return Dummy.Assembly;
    }

    public IModule LoadModule(ModuleIdentity moduleIdentity) {
      return Dummy.Module;
    }

    public IUnit LoadUnit(UnitIdentity unitIdentity) {
      return Dummy.Unit;
    }

    public IUnit LoadUnitFrom(string location) {
      return Dummy.Unit;
    }

    public new INameTable NameTable {
      get { return Dummy.NameTable; }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public byte PointerSize {
      get { return 4; }
    }

    public void ReportErrors(ErrorEventArgs errorEventArguments) {
      if (this.Errors != null)
        this.Errors(this, errorEventArguments); //Do this only to shut up warning about not using this.Errors
    }

    public void ReportError(IErrorMessage error) {
    }

    //^ [Pure]
    public AssemblyIdentity ProbeAssemblyReference(IUnit unit, AssemblyIdentity referedAssemblyIdentity) {
      return referedAssemblyIdentity;
    }

    //^ [Pure]
    public ModuleIdentity ProbeModuleReference(IUnit unit, ModuleIdentity referedModuleIdentity) {
      return referedModuleIdentity;
    }

    //^ [Pure]
    public AssemblyIdentity UnifyAssembly(AssemblyIdentity assemblyIdentity) {
      return assemblyIdentity;
    }

    public IEnumerable<IUnit> LoadedUnits {
      get { return IteratorHelper.GetEmptyEnumerable<IUnit>(); }
    }

    public new IInternFactory InternFactory {
      get { return Dummy.InternFactory; }
    }

    public bool PreserveILLocations {
      get { return false; }
    }

    #endregion
  }

  internal sealed class DummyMetadataConstant : Dummy, IMetadataConstant {

    #region IMetadataConstant Members

    public object/*?*/ Value {
      get { return null; }
    }

    #endregion

    #region IMetadataExpression Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

  }

  internal sealed class DummyCustomAttribute : Dummy, ICustomAttribute {
    #region ICustomAttribute Members

    public IEnumerable<IMetadataExpression> Arguments {
      get { return IteratorHelper.GetEmptyEnumerable<IMetadataExpression>(); }
    }

    public IMethodReference Constructor {
      get { return Dummy.MethodReference; }
    }

    public IEnumerable<IMetadataNamedArgument> NamedArguments {
      get { return IteratorHelper.GetEmptyEnumerable<IMetadataNamedArgument>(); }
    }

    public ushort NumberOfNamedArguments {
      get { return 0; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

  }

  internal sealed class DummyCustomModifier : Dummy, ICustomModifier {

    #region ICustomModifier Members

    public bool IsOptional {
      get { return false; }
    }

    public ITypeReference Modifier {
      get { return Dummy.TypeReference; }
    }

    #endregion

  }

  internal sealed class DummyEventDefinition : Dummy, IEventDefinition {

    #region IEventDefinition Members

    public IEnumerable<IMethodReference> Accessors {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodReference>(); }
    }

    public IMethodReference Adder {
      get { return Dummy.MethodReference; }
    }

    public IMethodReference/*?*/ Caller {
      get { return null; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public IMethodReference Remover {
      get { return Dummy.MethodReference; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Other; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return Dummy.Event; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion
  }

  internal sealed class DummyMetadataExpression : Dummy, IMetadataExpression {

    #region IMetadataExpression Members

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion
  }

  internal sealed class DummyFieldDefinition : Dummy, IFieldDefinition {

    #region IFieldDefinition Members

    public uint BitLength {
      get { return 0; }
    }

    public bool IsBitField {
      get { return false; }
    }

    public bool IsCompileTimeConstant {
      get { return false; }
    }

    public bool IsMapped {
      get { return false; }
    }

    public bool IsMarshalledExplicitly {
      get { return false; }
    }

    public bool IsNotSerialized {
      get { return true; }
    }

    public bool IsReadOnly {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public ISectionBlock FieldMapping {
      get { return Dummy.SectionBlock; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public uint Offset {
      get { return 0; }
    }

    public int SequenceNumber {
      get { return 0; }
    }

    public IMetadataConstant CompileTimeValue {
      get { return Dummy.Constant; }
    }

    public new IMarshallingInformation MarshallingInformation {
      get {
        //^ assume false;
        IMarshallingInformation/*?*/ dummyValue = null;
        //^ assume dummyValue != null;
        return dummyValue;
      }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Other; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region IFieldReference Members

    public uint InternedKey {
      get { return 0; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public IFieldDefinition ResolvedField {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer

    public new IMetadataConstant Constant {
      get { return Dummy.Constant; }
    }

    #endregion
  }

  internal sealed class DummyFileReference : Dummy, IFileReference {
    #region IFileReference Members

    public IAssembly ContainingAssembly {
      get { return Dummy.Assembly; }
    }

    public bool HasMetadata {
      get { return false; }
    }

    public IName FileName {
      get { return Dummy.Name; }
    }

    public IEnumerable<byte> HashValue {
      get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
    }

    #endregion

  }

  internal sealed class DummyGenericTypeInstance : Dummy, IGenericTypeInstance {

    #region IGenericTypeInstance Members

    public IEnumerable<ITypeReference> GenericArguments {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public ITypeReference GenericType {
      get {
        //^ assume false;
        return Dummy.TypeReference;
      }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
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

    public bool IsSealed {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region ITypeReference Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public bool IsAlias {
      get { return false; }
    }

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class DummyGenericTypeParameter : Dummy, IGenericTypeParameter {

    #region IGenericTypeParameter Members

    public ITypeDefinition DefiningType {
      get { return Dummy.Type; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IGenericParameter Members

    public IEnumerable<ITypeReference> Constraints {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool MustBeReferenceType {
      get { return false; }
    }

    public bool MustBeValueType {
      get { return false; }
    }

    public bool MustHaveDefaultConstructor {
      get { return false; }
    }

    public TypeParameterVariance Variance {
      get { return TypeParameterVariance.NonVariant; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return 0; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion

    #region IGenericTypeParameterReference Members

    ITypeReference IGenericTypeParameterReference.DefiningType {
      get { return Dummy.TypeReference; }
    }

    IGenericTypeParameter IGenericTypeParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyGenericMethodParameter : Dummy, IGenericMethodParameter {
    #region IGenericMethodParameter Members

    public IMethodDefinition DefiningMethod {
      get {
        //^ assume false; //TODO; need a dummy generic method
        return Dummy.Method;
      }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IGenericParameter Members

    public IEnumerable<ITypeReference> Constraints {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool MustBeReferenceType {
      get { return false; }
    }

    public bool MustBeValueType {
      get { return false; }
    }

    public bool MustHaveDefaultConstructor {
      get { return false; }
    }

    public TypeParameterVariance Variance {
      get { return TypeParameterVariance.NonVariant; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return 0; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion

    #region IGenericMethodParameterReference Members

    IMethodReference IGenericMethodParameterReference.DefiningMethod {
      get { return Dummy.MethodReference; }
    }

    IGenericMethodParameter IGenericMethodParameterReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyMethodBody : Dummy, IMethodBody {

    #region IMethodBody Members

    public IMethodDefinition MethodDefinition {
      get { return Dummy.Method; }
    }

    //public IBlockStatement Block {
    //  get { return Dummy.Block; }
    //}

    //public IOperation GetOperationAt(int offset, out int offsetOfNextOperation) {
    //  offsetOfNextOperation = -1;
    //  return Dummy.Operation;
    //}

    public IEnumerable<ILocalDefinition> LocalVariables {
      get { return IteratorHelper.GetEmptyEnumerable<ILocalDefinition>(); }
    }

    public bool LocalsAreZeroed {
      get { return false; }
    }

    public IEnumerable<IOperation> Operations {
      get { return IteratorHelper.GetEmptyEnumerable<IOperation>(); }
    }

    public IEnumerable<ITypeDefinition> PrivateHelperTypes {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinition>(); }
    }

    public ushort MaxStack {
      get { return 0; }
    }

    public new IEnumerable<IOperationExceptionInformation> OperationExceptionInformation {
      get { return IteratorHelper.GetEmptyEnumerable<IOperationExceptionInformation>(); }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

  }

  internal sealed class DummyMethodDefinition : Dummy, IMethodDefinition {

    #region IMethodDefinition Members

    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    public bool HasExplicitThisParameter {
      get { return false; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return false; }
    }

    public bool IsCil {
      get { return false; }
    }

    public bool IsConstructor {
      get { return false; }
    }

    public bool IsStaticConstructor {
      get { return false; }
    }

    public bool IsExternal {
      get { return false; }
    }

    public bool IsForwardReference {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsHiddenBySignature {
      get { return false; }
    }

    public bool IsNativeCode {
      get { return false; }
    }

    public bool IsNewSlot {
      get { return false; }
    }

    public bool IsNeverInlined {
      get { return false; }
    }

    public bool IsNeverOptimized {
      get { return false; }
    }

    public bool IsPlatformInvoke {
      get { return false; }
    }

    public bool IsRuntimeImplemented {
      get { return false; }
    }

    public bool IsRuntimeInternal {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsSynchronized {
      get { return false; }
    }

    public bool IsVirtual {
      get { return false; }
    }

    public bool IsUnmanaged {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
    }

    public bool PreserveSignature {
      get { return false; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return Dummy.PlatformInvokeInformation; }
    }

    public bool RequiresSecurityObject {
      get { return false; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return false; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    #endregion

    #region ISignature Members

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterDefinition>(); }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Default; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region IMethodReference Members

    public bool AcceptsExtraArguments {
      get { return false; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    public ushort ParameterCount {
      get { return 0; }
    }

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyMethodReference : Dummy, IMethodReference {
    #region IMethodReference Members

    public bool AcceptsExtraArguments {
      get { return false; }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public uint InternedKey {
      get { return 0; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public ushort ParameterCount {
      get { return 0; }
    }

    public IMethodDefinition ResolvedMethod {
      get {
        //^ assume false;
        return Dummy.Method;
      }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ISignature Members

    public CallingConvention CallingConvention {
      get { return CallingConvention.C; }
    }

    public IEnumerable<IParameterTypeInformation> Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return Dummy.Method; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion
  }

  internal sealed class DummyModule : Dummy, IModule {
    #region IModule Members

    public IName ModuleName {
      get {
        return Dummy.Name;
      }
    }

    public IAssembly/*?*/ ContainingAssembly {
      get {
        return null;
      }
    }

    public IEnumerable<IAssemblyReference> AssemblyReferences {
      get { return IteratorHelper.GetEmptyEnumerable<IAssemblyReference>(); }
    }

    public ulong BaseAddress {
      get { return 0; }
    }

    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public ushort DllCharacteristics {
      get { return 0; }
    }

    public IMethodReference EntryPoint {
      get { return Dummy.MethodReference; }
    }

    public uint FileAlignment {
      get { return 0; }
    }

    public bool ILOnly {
      get { return false; }
    }

    public ModuleKind Kind {
      get { return ModuleKind.ConsoleApplication; }
    }

    public byte LinkerMajorVersion {
      get { return 0; }
    }

    public byte LinkerMinorVersion {
      get { return 0; }
    }

    public byte MetadataFormatMajorVersion {
      get { return 0; }
    }

    public byte MetadataFormatMinorVersion {
      get { return 0; }
    }

    public IEnumerable<ICustomAttribute> ModuleAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<IModuleReference> ModuleReferences {
      get { return IteratorHelper.GetEmptyEnumerable<IModuleReference>(); }
    }

    public Guid PersistentIdentifier {
      get { return Guid.Empty; }
    }

    public Machine Machine {
      get { return Machine.Unknown; }
    }

    public bool RequiresAmdInstructionSet {
      get { return false; }
    }

    public bool RequiresStartupStub {
      get { return false; }
    }

    public bool Requires32bits {
      get { return false; }
    }

    public bool Requires64bits {
      get { return false; }
    }

    public ulong SizeOfHeapReserve {
      get { return 0; }
    }

    public ulong SizeOfHeapCommit {
      get { return 0; }
    }

    public ulong SizeOfStackReserve {
      get { return 0; }
    }

    public ulong SizeOfStackCommit {
      get { return 0; }
    }

    public string TargetRuntimeVersion {
      get { return string.Empty; }
    }

    public bool TrackDebugData {
      get { return false; }
    }

    public bool UsePublicKeyTokensForAssemblyReferences {
      get { return false; }
    }

    public IEnumerable<IWin32Resource> Win32Resources {
      get { return IteratorHelper.GetEmptyEnumerable<IWin32Resource>(); }
    }

    public IEnumerable<string> GetStrings() {
      return IteratorHelper.GetEmptyEnumerable<string>();
    }

    public IEnumerable<INamedTypeDefinition> GetAllTypes() {
      return IteratorHelper.GetEmptyEnumerable<INamedTypeDefinition>();
    }

    public ModuleIdentity ModuleIdentity {
      get {
        return new ModuleIdentity(Dummy.Name, this.Location);
      }
    }

    #endregion

    #region IUnit Members

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public new string Location {
      get { return string.Empty; }
    }

    public new IName Name {
      get { return Dummy.Name; }
    }

    public IRootUnitNamespace UnitNamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitNamespace;
      }
    }

    public IEnumerable<IUnitReference> UnitReferences {
      get { return IteratorHelper.GetEmptyEnumerable<IUnitReference>(); }
    }

    public UnitIdentity UnitIdentity {
      get {
        return this.ModuleIdentity;
      }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region INamespaceRootOwner Members

    public INamespaceDefinition NamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitNamespace;
      }
    }

    #endregion

    #region IUnitReference Members

    public IUnit ResolvedUnit {
      get { return this; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IModuleReference Members

    IAssemblyReference/*?*/ IModuleReference.ContainingAssembly {
      get { return null; }
    }

    public IModule ResolvedModule {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyModuleReference : Dummy, IModuleReference {

    #region IUnitReference Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IModuleReference Members

    public IAssemblyReference/*?*/ ContainingAssembly {
      get { return null; }
    }

    public IModule ResolvedModule {
      get { return Dummy.Module; }
    }

    #endregion

    #region IUnitReference Members

    public IUnit ResolvedUnit {
      get { return Dummy.Unit; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IModuleReference Members

    public ModuleIdentity ModuleIdentity {
      get { return Dummy.Module.ModuleIdentity; }
    }

    #endregion

    #region IUnitReference Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public UnitIdentity UnitIdentity {
      get { return this.ModuleIdentity; }
    }

    #endregion
  }

  internal sealed class DummyName : Dummy, IName {

    #region IName Members

    public int UniqueKey {
      get { return 1; }
    }

    public int UniqueKeyIgnoringCase {
      get { return 1; }
    }

    public string Value {
      get { return string.Empty; }
    }

    #endregion

  }

  internal sealed class DummyNamedArgument : Dummy, IMetadataNamedArgument {
    #region IMetadataNamedArgument Members

    public IName ArgumentName {
      get { return Dummy.Name; }
    }

    public IMetadataExpression ArgumentValue {
      get { return Dummy.Expression; }
    }

    public bool IsField {
      get { return false; }
    }

    public object ResolvedDefinition {
      get { return Dummy.Property; }
    }

    #endregion

    #region IMetadataExpression Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion
  }

  internal sealed class DummyNamespaceTypeDefinition : Dummy, INamespaceTypeDefinition {
    #region INamespaceTypeDefinition Members

    public IUnitNamespace ContainingUnitNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    public bool IsPublic {
      get { return false; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return true; }
    }

    public bool IsStatic {
      get { return true; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }


    public uint InternedKey {
      get { return 0; }
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

    public bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyNamespaceTypeReference : Dummy, INamespaceTypeReference {

    #region INamespaceTypeReference Members

    public ushort GenericParameterCount {
      get { return 0; }
    }

    public IUnitNamespaceReference ContainingUnitNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    INamespaceTypeDefinition INamespaceTypeReference.ResolvedType {
      get { return Dummy.NamespaceTypeDefinition; }
    }

    #endregion

    #region ITypeReference Members

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get {
        //^ assume false;
        return Dummy.Type;
      }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public uint InternedKey {
      get { return 0; }
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

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return Dummy.NamespaceTypeDefinition; }
    }

    #endregion
  }

  internal sealed class DummyNameTable : Dummy, INameTable {

    #region INameTable Members

    public IName Address {
      get { return Dummy.Name; }
    }

    public IName AllowMultiple {
      get { return Dummy.Name; }
    }

    public IName BoolOpBool {
      get { return Dummy.Name; }
    }

    public IName DecimalOpDecimal {
      get { return Dummy.Name; }
    }

    public IName DelegateOpDelegate {
      get { return Dummy.Name; }
    }

    public IName Cctor {
      get { return Dummy.Name; }
    }

    public IName Ctor {
      get { return Dummy.Name; }
    }

    public IName EmptyName {
      get { return Dummy.Name; }
    }

    public IName EnumOpEnum {
      get { return Dummy.Name; }
    }

    public IName EnumOpNum {
      get { return Dummy.Name; }
    }

    public new IName Equals {
      get { return Dummy.Name; }
    }

    public IName Float32OpFloat32 {
      get { return Dummy.Name; }
    }

    public IName Float64OpFloat64 {
      get { return Dummy.Name; }
    }

    public IName Get {
      get { return Dummy.Name; }
    }

    //^ [Pure]
    public IName GetNameFor(string name) {
      //^ assume false;
      return Dummy.Name;
    }

    public IName global {
      get { return Dummy.Name; }
    }

    public IName HasValue {
      get { return Dummy.Name; }
    }

    public IName Inherited {
      get { return Dummy.Name; }
    }

    public IName Invoke {
      get { return Dummy.Name; }
    }

    public IName Int16OpInt16 {
      get { return Dummy.Name; }
    }

    public IName Int32OpInt32 {
      get { return Dummy.Name; }
    }

    public IName Int32OpUInt32 {
      get { return Dummy.Name; }
    }

    public IName Int64OpInt32 {
      get { return Dummy.Name; }
    }

    public IName Int64OpUInt32 {
      get { return Dummy.Name; }
    }

    public IName Int64OpUInt64 {
      get { return Dummy.Name; }
    }

    public IName Int64OpInt64 {
      get { return Dummy.Name; }
    }

    public IName Int8OpInt8 {
      get { return Dummy.Name; }
    }

    public IName NullCoalescing {
      get { return Dummy.Name; }
    }

    public IName NumOpEnum {
      get { return Dummy.Name; }
    }

    public IName ObjectOpObject {
      get { return Dummy.Name; }
    }

    public IName ObjectOpString {
      get { return Dummy.Name; }
    }

    public IName OpAddition {
      get { return Dummy.Name; }
    }

    public IName OpBoolean {
      get { return Dummy.Name; }
    }

    public IName OpChar {
      get { return Dummy.Name; }
    }

    public IName OpDecimal {
      get { return Dummy.Name; }
    }

    public IName OpEnum {
      get { return Dummy.Name; }
    }

    public IName OpEquality {
      get { return Dummy.Name; }
    }

    public IName OpInequality {
      get { return Dummy.Name; }
    }

    public IName OpInt8 {
      get { return Dummy.Name; }
    }

    public IName OpInt16 {
      get { return Dummy.Name; }
    }

    public IName OpInt32 {
      get { return Dummy.Name; }
    }

    public IName OpInt64 {
      get { return Dummy.Name; }
    }

    public IName OpBitwiseAnd {
      get { return Dummy.Name; }
    }

    public IName OpBitwiseOr {
      get { return Dummy.Name; }
    }

    public IName OpComma {
      get { return Dummy.Name; }
    }

    public IName OpConcatentation {
      get { return Dummy.Name; }
    }

    public IName OpDivision {
      get { return Dummy.Name; }
    }

    public IName OpExclusiveOr {
      get { return Dummy.Name; }
    }

    public IName OpExplicit {
      get { return Dummy.Name; }
    }

    public IName OpExponentiation {
      get { return Dummy.Name; }
    }

    public IName OpFalse {
      get { return Dummy.Name; }
    }

    public IName OpFloat32 {
      get { return Dummy.Name; }
    }

    public IName OpFloat64 {
      get { return Dummy.Name; }
    }

    public IName OpGreaterThan {
      get { return Dummy.Name; }
    }

    public IName OpGreaterThanOrEqual {
      get { return Dummy.Name; }
    }

    public IName OpImplicit {
      get { return Dummy.Name; }
    }

    public IName OpIntegerDivision {
      get { return Dummy.Name; }
    }

    public IName OpLeftShift {
      get { return Dummy.Name; }
    }

    public IName OpLessThan {
      get { return Dummy.Name; }
    }

    public IName OpLessThanOrEqual {
      get { return Dummy.Name; }
    }

    public IName OpLike {
      get { return Dummy.Name; }
    }

    public IName OpLogicalNot {
      get { return Dummy.Name; }
    }

    public IName OpLogicalOr {
      get { return Dummy.Name; }
    }

    public IName OpModulus {
      get { return Dummy.Name; }
    }

    public IName OpMultiply {
      get { return Dummy.Name; }
    }

    public IName OpOnesComplement {
      get { return Dummy.Name; }
    }

    public IName OpDecrement {
      get { return Dummy.Name; }
    }

    public IName OpIncrement {
      get { return Dummy.Name; }
    }

    public IName OpRightShift {
      get { return Dummy.Name; }
    }

    public IName OpSubtraction {
      get { return Dummy.Name; }
    }

    public IName OpTrue {
      get { return Dummy.Name; }
    }

    public IName OpUInt8 {
      get { return Dummy.Name; }
    }

    public IName OpUInt16 {
      get { return Dummy.Name; }
    }

    public IName OpUInt32 {
      get { return Dummy.Name; }
    }

    public IName OpUInt64 {
      get { return Dummy.Name; }
    }

    public IName OpUnaryNegation {
      get { return Dummy.Name; }
    }

    public IName OpUnaryPlus {
      get { return Dummy.Name; }
    }

    public IName StringOpObject {
      get { return Dummy.Name; }
    }

    public IName StringOpString {
      get { return Dummy.Name; }
    }

    public IName value {
      get { return Dummy.Name; }
    }

    public IName UIntPtrOpUIntPtr {
      get { return Dummy.Name; }
    }

    public IName UInt32OpInt32 {
      get { return Dummy.Name; }
    }

    public IName UInt32OpUInt32 {
      get { return Dummy.Name; }
    }

    public IName UInt64OpInt32 {
      get { return Dummy.Name; }
    }

    public IName UInt64OpUInt32 {
      get { return Dummy.Name; }
    }

    public IName UInt64OpUInt64 {
      get { return Dummy.Name; }
    }

    public IName System {
      get { return Dummy.Name; }
    }

    public IName Void {
      get { return Dummy.Name; }
    }

    public IName VoidPtrOpVoidPtr {
      get { return Dummy.Name; }
    }

    public IName Boolean {
      get { return Dummy.Name; }
    }

    public IName Char {
      get { return Dummy.Name; }
    }

    public IName Byte {
      get { return Dummy.Name; }
    }

    public IName SByte {
      get { return Dummy.Name; }
    }

    public IName Int16 {
      get { return Dummy.Name; }
    }

    public IName UInt16 {
      get { return Dummy.Name; }
    }

    public IName Int32 {
      get { return Dummy.Name; }
    }

    public IName UInt32 {
      get { return Dummy.Name; }
    }

    public IName Int64 {
      get { return Dummy.Name; }
    }

    public IName UInt64 {
      get { return Dummy.Name; }
    }

    public IName String {
      get { return Dummy.Name; }
    }

    public IName IntPtr {
      get { return Dummy.Name; }
    }

    public IName UIntPtr {
      get { return Dummy.Name; }
    }

    public IName Object {
      get { return Dummy.Name; }
    }

    public IName Set {
      get { return Dummy.Name; }
    }

    public IName Single {
      get { return Dummy.Name; }
    }

    public IName Double {
      get { return Dummy.Name; }
    }

    public IName TypedReference {
      get { return Dummy.Name; }
    }

    public IName Enum {
      get { return Dummy.Name; }
    }

    public IName MulticastDelegate {
      get { return Dummy.Name; }
    }

    public IName ValueType {
      get { return Dummy.Name; }
    }

    public new IName Type {
      get { return Dummy.Name; }
    }

    public IName Array {
      get { return Dummy.Name; }
    }

    public IName AttributeUsageAttribute {
      get { return Dummy.Name; }
    }

    public IName Attribute {
      get { return Dummy.Name; }
    }

    public IName Combine {
      get { return Dummy.Name; }
    }

    public IName Concat {
      get { return Dummy.Name; }
    }

    public IName DateTime {
      get { return Dummy.Name; }
    }

    public IName DebuggerHiddenAttribute {
      get { return Dummy.Name; }
    }

    public IName Decimal {
      get { return Dummy.Name; }
    }

    public IName Delegate {
      get { return Dummy.Name; }
    }

    public IName Diagnostics {
      get { return Dummy.Name; }
    }

    public IName DBNull {
      get { return Dummy.Name; }
    }

    public IName Length {
      get { return Dummy.Name; }
    }

    public IName LongLength {
      get { return Dummy.Name; }
    }

    public IName Nullable {
      get { return Dummy.Name; }
    }

    public IName Remove {
      get { return Dummy.Name; }
    }

    public IName Result {
      get { return Dummy.Name; }
    }

    #endregion
  }

  internal sealed class DummyNestedType : Dummy, INestedTypeDefinition {

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return true; }
    }

    public bool IsStatic {
      get { return true; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Public; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
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

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion

    #region ITypeMemberReference Members

    ITypeReference ITypeMemberReference.ContainingType {
      get { return Dummy.TypeReference; }
    }
    #endregion

    #region INestedTypeReference Members

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyPlatformType : Dummy, IPlatformType {

    #region IPlatformType Members

    public INamespaceTypeReference SystemDiagnosticsContractsContract {
      get { return Dummy.NamespaceTypeReference; }
    }

    public byte PointerSize {
      get { return 4; }
    }

    public INamespaceTypeReference SystemArgIterator {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemArray {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemAttributeUsageAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemAsyncCallback {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemBoolean {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemChar {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsGenericDictionary {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsGenericICollection {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsGenericIEnumerable {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsGenericIEnumerator {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsGenericIList {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsICollection {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsIEnumerable {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsIEnumerator {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsIList {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsIStructuralComparable {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemCollectionsIStructuralEquatable {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemIAsyncResult {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemICloneable {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemDateTime {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemDecimal {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemDelegate {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemDBNull {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemEnum {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemFloat32 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemFloat64 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemInt16 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemInt32 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemInt64 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemInt8 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemIntPtr {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemMulticastDelegate {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemNullable {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemObject {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeArgumentHandle {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeFieldHandle {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeMethodHandle {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeTypeHandle {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeCompilerServicesCallConvCdecl {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeCompilerServicesCompilerGeneratedAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeCompilerServicesFriendAccessAllowedAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeCompilerServicesIsConst {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeCompilerServicesIsVolatile {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeCompilerServicesReferenceAssemblyAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemRuntimeInteropServicesDllImportAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemSecurityPermissionsSecurityAction {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemSecuritySecurityCriticalAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemSecuritySecuritySafeCriticalAttribute {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemString {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemType {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemTypedReference {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemUInt16 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemUInt32 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemUInt64 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemUInt8 {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemUIntPtr {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemValueType {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference SystemVoid {
      get { return Dummy.NamespaceTypeReference; }
    }

    public INamespaceTypeReference GetTypeFor(PrimitiveTypeCode typeCode) {
      return Dummy.NamespaceTypeReference;
    }

    #endregion
  }

  internal sealed class DummyPropertyDefinition : Dummy, IPropertyDefinition {

    #region IPropertyDefinition Members

    public IEnumerable<IMethodReference> Accessors {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodReference>(); }
    }

    public IMetadataConstant DefaultValue {
      get { return Dummy.Constant; }
    }

    public IMethodReference/*?*/ Getter {
      get { return null; }
    }

    public bool HasDefaultValue {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public IMethodReference/*?*/ Setter {
      get { return null; }
    }

    #endregion

    #region ISignature Members

    public IEnumerable<IParameterDefinition> Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterDefinition>(); }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.C; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Other; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return Dummy.Property; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get {
        return Dummy.Type;
      }
    }

    #endregion

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region IMetadataConstantContainer

    public new IMetadataConstant Constant {
      get { return Dummy.Constant; }
    }

    #endregion
  }

  internal sealed class DummyType : Dummy, ITypeDefinition {

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return true; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return true; }
    }

    public bool IsStatic {
      get { return true; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public ITypeDefinition ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class DummyTypeReference : Dummy, ITypeReference {

    #region ITypeReference Members

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get {
        //^ assume false;
        return Dummy.Type;
      }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public uint InternedKey {
      get { return 0; }
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

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion
  }

  internal sealed class DummyUnit : Dummy, IUnit {

    #region IUnit Members

    public AssemblyIdentity ContractAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public AssemblyIdentity CoreAssemblySymbolicIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public new string Location {
      get { return string.Empty; }
    }

    public new IName Name {
      get { return Dummy.Name; }
    }

    public IRootUnitNamespace UnitNamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitNamespace;
      }
    }

    public IEnumerable<IUnitReference> UnitReferences {
      get { return IteratorHelper.GetEmptyEnumerable<IUnitReference>(); }
    }

    public UnitIdentity UnitIdentity {
      get {
        return new ModuleIdentity(Dummy.Name, string.Empty);
      }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region INamespaceRootOwner Members

    public INamespaceDefinition NamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitNamespace;
      }
    }

    #endregion

    #region IUnitReference Members

    public IUnit ResolvedUnit {
      get { return Dummy.Unit; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion
  }

  internal sealed class DummyRootUnitNamespace : Dummy, IRootUnitNamespace {

    #region IUnitNamespace Members

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public new IUnit Unit {
      get { return Dummy.Unit; }
    }

    #endregion

    #region INamespaceDefinition Members

    public INamespaceRootOwner RootOwner {
      get { return Dummy.Unit; }
    }

    public IEnumerable<INamespaceMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<INamespaceMember>(); }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IScope<INamespaceMember> Members

    public bool Contains(INamespaceMember member) {
      return false;
    }

    public IEnumerable<INamespaceMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<INamespaceMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    public IEnumerable<INamespaceMember> GetMatchingMembers(Function<INamespaceMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    public IEnumerable<INamespaceMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    #endregion

    #region IUnitNamespaceReference Members

    IUnitReference IUnitNamespaceReference.Unit {
      get { return Dummy.Unit; }
    }

    public IUnitNamespace ResolvedUnitNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion
  }

  internal sealed class DummyNestedUnitNamespace : Dummy, INestedUnitNamespace {

    #region IUnitNamespace Members

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public new IUnit Unit {
      get { return Dummy.Unit; }
    }

    #endregion

    #region INamespaceDefinition Members

    public INamespaceRootOwner RootOwner {
      get { return Dummy.Unit; }
    }

    public IEnumerable<INamespaceMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<INamespaceMember>(); }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IScope<INamespaceMember> Members

    public bool Contains(INamespaceMember member) {
      return false;
    }

    public IEnumerable<INamespaceMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<INamespaceMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    public IEnumerable<INamespaceMember> GetMatchingMembers(Function<INamespaceMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    public IEnumerable<INamespaceMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    #endregion

    #region IUnitNamespaceReference Members

    IUnitReference IUnitNamespaceReference.Unit {
      get { return Dummy.Unit; }
    }

    public IUnitNamespace ResolvedUnitNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region INestedUnitNamespace Members

    public IUnitNamespace ContainingUnitNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region INestedUnitNamespaceReference Members

    IUnitNamespaceReference INestedUnitNamespaceReference.ContainingUnitNamespace {
      get { return this; }
    }

    public INestedUnitNamespace ResolvedNestedUnitNamespace {
      get { return this; }
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyUnitSet : Dummy, IUnitSet {

    #region IUnitSet Members

    public bool Contains(IUnit unit) {
      return false;
    }

    public IEnumerable<IUnit> Units {
      get { return IteratorHelper.GetEmptyEnumerable<IUnit>(); }
    }

    public IUnitSetNamespace UnitSetNamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitSetNamespace;
      }
    }

    #endregion

    #region INamespaceRootOwner Members

    public INamespaceDefinition NamespaceRoot {
      get {
        //^ assume false;
        return Dummy.RootUnitSetNamespace;
      }
    }

    #endregion

  }

  internal sealed class DummyRootUnitSetNamespace : Dummy, IRootUnitSetNamespace {

    #region IUnitSetNamespace Members

    public new IUnitSet UnitSet {
      get { return Dummy.UnitSet; }
    }

    #endregion

    #region INamespaceDefinition Members

    public INamespaceRootOwner RootOwner {
      get { return Dummy.UnitSet; }
    }

    public IEnumerable<INamespaceMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<INamespaceMember>(); }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IScope<INamespaceMember> Members

    public bool Contains(INamespaceMember member) {
      return false;
    }

    public IEnumerable<INamespaceMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<INamespaceMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    public IEnumerable<INamespaceMember> GetMatchingMembers(Function<INamespaceMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    public IEnumerable<INamespaceMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<INamespaceMember>();
    }

    #endregion
  }

  internal sealed class DummyResource : Dummy, IResource {
    #region IResource Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<byte> Data {
      get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
    }

    public IAssemblyReference DefiningAssembly {
      get { return Dummy.AssemblyReference; }
    }

    public bool IsInExternalFile {
      get { return false; }
    }

    public IFileReference ExternalFile {
      get { return Dummy.FileReference; }
    }

    public bool IsPublic {
      get { return false; }
    }

    public new IName Name {
      get { return Dummy.Name; }
    }

    public new IResource Resource {
      get { return this; }
    }

    #endregion

  }

  internal sealed class DummyAssemblyReference : Dummy, IAssemblyReference {
    #region IAssemblyReference Members

    public IEnumerable<IName> Aliases {
      get { return IteratorHelper.GetEmptyEnumerable<IName>(); }
    }

    public string Culture {
      get { return string.Empty; }
    }

    public IEnumerable<byte> PublicKeyToken {
      get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
    }

    public new Version Version {
      get { return new Version(0, 0); }
    }

    #endregion

    #region IUnitReference Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IAssemblyReference Members

    public IAssembly ResolvedAssembly {
      get { return Dummy.Assembly; }
    }

    public AssemblyIdentity AssemblyIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    public bool IsRetargetable {
      get { return false; }
    }

    public AssemblyIdentity UnifiedAssemblyIdentity {
      get { return Dummy.Assembly.AssemblyIdentity; }
    }

    #endregion

    #region IModuleReference Members

    public ModuleIdentity ModuleIdentity {
      get { return this.AssemblyIdentity; }
    }

    public IAssemblyReference/*?*/ ContainingAssembly {
      get { return null; }
    }

    public IModule ResolvedModule {
      get { return Dummy.Module; }
    }

    #endregion

    #region IUnitReference Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public UnitIdentity UnitIdentity {
      get { return this.AssemblyIdentity; }
    }

    public IUnit ResolvedUnit {
      get { return Dummy.Unit; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion
  }

  internal sealed class DummyMarshallingInformation : Dummy, IMarshallingInformation {
    #region IMarshallingInformation Members

    public ITypeReference CustomMarshaller {
      get { return Dummy.TypeReference; }
    }

    public string CustomMarshallerRuntimeArgument {
      get { return string.Empty; }
    }

    public uint ElementSize {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType ElementType {
      get { return System.Runtime.InteropServices.UnmanagedType.Error; }
    }

    public uint IidParameterIndex {
      get { return 0; }
    }

    public System.Runtime.InteropServices.UnmanagedType UnmanagedType {
      get { return System.Runtime.InteropServices.UnmanagedType.Error; }
    }

    public uint NumberOfElements {
      get { return 0; }
    }

    public uint? ParamIndex {
      get { return 0; }
    }

    public System.Runtime.InteropServices.VarEnum SafeArrayElementSubtype {
      get { return System.Runtime.InteropServices.VarEnum.VT_VOID; }
    }

    public ITypeReference SafeArrayElementUserDefinedSubtype {
      get { return Dummy.TypeReference; }
    }

    public uint ElementSizeMultiplier {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class DummySecurityAttribute : Dummy, ISecurityAttribute {
    #region ISecurityAttribute Members

    public SecurityAction Action {
      get { return SecurityAction.LinkDemand; }
    }

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion
  }

  internal sealed class DummyParameterTypeInformation : Dummy, IParameterTypeInformation {
    #region IParameterTypeInformation Members

    public ISignature ContainingSignature {
      get { return Dummy.Method; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool IsByReference {
      get { return false; }
    }

    public bool IsModified {
      get { return false; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class DummySpecializedNestedTypeDefinition : Dummy, ISpecializedNestedTypeDefinition {
    #region ISpecializedNestedTypeDefinition Members

    public INestedTypeDefinition UnspecializedVersion {
      get { return Dummy.NestedType; }
    }

    #endregion

    #region ISpecializedNestedTypeReference Members

    INestedTypeReference ISpecializedNestedTypeReference.UnspecializedVersion {
      get { return Dummy.NestedType; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return true; }
    }

    public bool IsStatic {
      get { return true; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Public; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
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

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion

    #region ITypeMemberReference Members

    ITypeReference ITypeMemberReference.ContainingType {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region INestedTypeReference Members

    INestedTypeDefinition INestedTypeReference.ResolvedType {
      get { return Dummy.NestedType; }
    }

    #endregion

    #region ITypeMemberReference Members


    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region INamedTypeReference Members

    public bool MangleName {
      get { return false; }
    }

    public INamedTypeDefinition ResolvedType {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummySpecializedFieldDefinition : Dummy, ISpecializedFieldDefinition {

    #region IFieldDefinition Members

    public uint BitLength {
      get { return 0; }
    }

    public bool IsBitField {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public bool IsCompileTimeConstant {
      get { return false; }
    }

    public bool IsMapped {
      get { return false; }
    }

    public bool IsMarshalledExplicitly {
      get { return false; }
    }

    public bool IsNotSerialized {
      get { return false; }
    }

    public bool IsReadOnly {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public ISectionBlock FieldMapping {
      get { return Dummy.SectionBlock; }
    }

    public uint Offset {
      get { return 0; }
    }

    public int SequenceNumber {
      get { return 0; }
    }

    public IMetadataConstant CompileTimeValue {
      get { return Dummy.Constant; }
    }

    public new IMarshallingInformation MarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Default; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region ISpecializedFieldDefinition Members

    public IFieldDefinition UnspecializedVersion {
      get { return Dummy.Field; }
    }

    #endregion

    #region ISpecializedFieldReference Members

    IFieldReference ISpecializedFieldReference.UnspecializedVersion {
      get { return Dummy.Field; }
    }

    #endregion

    #region IFieldReference Members

    public uint InternedKey {
      get { return 0; }
    }

    public IFieldDefinition ResolvedField {
      get { return Dummy.Field; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer

    public new IMetadataConstant Constant {
      get { return Dummy.Constant; }
    }

    #endregion

  }

  internal sealed class DummySpecializedMethodDefinition : Dummy, ISpecializedMethodDefinition {
    #region ISpecializedMethodDefinition Members

    public IMethodDefinition UnspecializedVersion {
      get { return Dummy.Method; }
    }

    #endregion

    #region ISpecializedMethodReference Members

    IMethodReference ISpecializedMethodReference.UnspecializedVersion {
      get { return Dummy.MethodReference; }
    }

    #endregion

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return false; }
    }

    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    //^ [Pure]
    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    public bool HasExplicitThisParameter {
      get { return false; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return false; }
    }

    public bool IsCil {
      get { return false; }
    }

    public bool IsConstructor {
      get { return false; }
    }

    public bool IsStaticConstructor {
      get { return false; }
    }

    public bool IsExternal {
      get { return false; }
    }

    public bool IsForwardReference {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsHiddenBySignature {
      get { return false; }
    }

    public bool IsNativeCode {
      get { return false; }
    }

    public bool IsNewSlot {
      get { return false; }
    }

    public bool IsNeverInlined {
      get { return false; }
    }

    public bool IsNeverOptimized {
      get { return false; }
    }

    public bool IsPlatformInvoke {
      get { return false; }
    }

    public bool IsRuntimeImplemented {
      get { return false; }
    }

    public bool IsRuntimeInternal {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsSynchronized {
      get { return false; }
    }

    public bool IsVirtual {
      get { return false; }
    }

    public bool IsUnmanaged {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
    }

    public bool PreserveSignature {
      get { return false; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return Dummy.PlatformInvokeInformation; }
    }

    public bool RequiresSecurityObject {
      get { return false; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return false; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    #endregion

    #region ISignature Members

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterDefinition>(); }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Default; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region IMethodReference Members

    public uint InternedKey {
      get { return 0; }
    }

    public ushort ParameterCount {
      get { return 0; }
    }

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

  }

  internal sealed class DummySpecializedPropertyDefinition : Dummy, ISpecializedPropertyDefinition {
    #region ISpecializedPropertyDefinition Members

    public IPropertyDefinition UnspecializedVersion {
      get { return Dummy.Property; }
    }

    #endregion

    #region IPropertyDefinition Members

    public IEnumerable<IMethodReference> Accessors {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodReference>(); }
    }

    public IMetadataConstant DefaultValue {
      get { return Dummy.Constant; }
    }

    public IMethodReference/*?*/ Getter {
      get { return null; }
    }

    public bool HasDefaultValue {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public IMethodReference/*?*/ Setter {
      get { return null; }
    }

    #endregion

    #region ISignature Members

    public IEnumerable<IParameterDefinition> Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterDefinition>(); }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.C; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Other; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return Dummy.Property; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get {
        return Dummy.Type;
      }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region IMetadataConstantContainer

    public new IMetadataConstant Constant {
      get { return Dummy.Constant; }
    }

    #endregion

  }

  internal sealed class DummyFunctionPointerType : Dummy, IFunctionPointer {
    #region IFunctionPointer Members

    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
    }

    public IEnumerable<IParameterTypeInformation> Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    public IEnumerable<IParameterTypeInformation> ExtraArgumentTypes {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return false; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return true; }
    }

    public bool IsStatic {
      get { return true; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region IFunctionPointerTypeReference Members


    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    ITypeDefinition ITypeReference.ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class DummyLocalVariable : Dummy, ILocalDefinition {

    #region ILocalDefinition Members

    public IMetadataConstant CompileTimeValue {
      get { return Dummy.Constant; }
    }

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool IsConstant {
      get { return false; }
    }

    public bool IsModified {
      get { return false; }
    }

    public bool IsPinned {
      get { return false; }
    }

    public bool IsReference {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public IMethodDefinition MethodDefinition {
      get { return Dummy.Method; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

  }

  internal sealed class DummyFieldReference : Dummy, IFieldReference {
    #region IFieldReference Members

    public uint InternedKey {
      get { return 0; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public IFieldDefinition ResolvedField {
      get { return Dummy.Field; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return Dummy.Field; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion
  }

  internal sealed class DummyParameterDefinition : Dummy, IParameterDefinition {
    #region IParameterDefinition Members

    public ISignature ContainingSignature {
      get { return Dummy.Method; }
    }

    public IMetadataConstant DefaultValue {
      get { return Dummy.Constant; }
    }

    public bool HasDefaultValue {
      get { return false; }
    }

    public bool IsIn {
      get { return false; }
    }

    public bool IsMarshalledExplicitly {
      get { return false; }
    }

    public bool IsOptional {
      get { return false; }
    }

    public bool IsOut {
      get { return false; }
    }

    public bool IsParameterArray {
      get { return false; }
    }

    public new IMarshallingInformation MarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public ITypeReference ParamArrayElementType {
      get { return Dummy.TypeReference; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IParameterListEntry Members

    public ushort Index {
      get { return 0; }
    }

    #endregion

    #region IParameterTypeInformation Members

    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool IsByReference {
      get { return false; }
    }

    public bool IsModified {
      get { return false; }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region IMetadataConstantContainer

    public new IMetadataConstant Constant {
      get { return Dummy.Constant; }
    }

    #endregion

  }

  internal sealed class DummySectionBlock : Dummy, ISectionBlock {
    #region ISectionBlock Members

    public PESectionKind PESectionKind {
      get { return PESectionKind.Illegal; }
    }

    public uint Offset {
      get { return 0; }
    }

    public uint Size {
      get { return 0; }
    }

    public IEnumerable<byte> Data {
      get { return IteratorHelper.GetEmptyEnumerable<byte>(); }
    }

    #endregion
  }

  internal sealed class DummyPlatformInvokeInformation : Dummy, IPlatformInvokeInformation {

    #region IPlatformInvokeInformation Members

    public IName ImportName {
      get { return Dummy.Name; }
    }

    public IModuleReference ImportModule {
      get { return Dummy.ModuleReference; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Unspecified; }
    }

    public bool NoMangle {
      get { return false; }
    }

    public bool SupportsLastError {
      get { return false; }
    }

    public PInvokeCallingConvention PInvokeCallingConvention {
      get { return PInvokeCallingConvention.CDecl; }
    }

    public bool? UseBestFit {
      get { return null; }
    }

    public bool? ThrowExceptionForUnmappableChar {
      get { return null; }
    }

    #endregion
  }

  internal sealed class DummyGlobalMethodDefinition : Dummy, IGlobalMethodDefinition {

    #region ISignature Members

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public IEnumerable<IParameterDefinition> Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterDefinition>(); }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    #endregion

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return false; }
    }

    public IMethodBody Body {
      get { return Dummy.MethodBody; }
    }

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
    }

    //^ [Pure]
    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    public bool HasExplicitThisParameter {
      get { return false; }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsAccessCheckedOnOverride {
      get { return false; }
    }

    public bool IsCil {
      get { return false; }
    }

    public bool IsConstructor {
      get { return false; }
    }

    public bool IsStaticConstructor {
      get { return false; }
    }

    public bool IsExternal {
      get { return false; }
    }

    public bool IsForwardReference {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsHiddenBySignature {
      get { return false; }
    }

    public bool IsNativeCode {
      get { return false; }
    }

    public bool IsNewSlot {
      get { return false; }
    }

    public bool IsNeverInlined {
      get { return false; }
    }

    public bool IsNeverOptimized {
      get { return false; }
    }

    public bool IsPlatformInvoke {
      get { return false; }
    }

    public bool IsRuntimeImplemented {
      get { return false; }
    }

    public bool IsRuntimeInternal {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSealed {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public bool IsSynchronized {
      get { return false; }
    }

    public bool IsVirtual {
      get { return false; }
    }

    public bool IsUnmanaged {
      get { return false; }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
    }

    public bool PreserveSignature {
      get { return false; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return Dummy.PlatformInvokeInformation; }
    }

    public bool RequiresSecurityObject {
      get { return false; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return false; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Other; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    ITypeDefinition IContainerMember<ITypeDefinition>.Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    IScope<ITypeDefinitionMember> IScopeMember<IScope<ITypeDefinitionMember>>.ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region ISignature Members


    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region IMethodReference Members

    public uint InternedKey {
      get { return 0; }
    }

    public ushort ParameterCount {
      get { return 0; }
    }

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion
  }

  internal sealed class DummyGlobalFieldDefinition : Dummy, IGlobalFieldDefinition {

    #region INamedEntity Members

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region INamespaceMember Members

    public INamespaceDefinition ContainingNamespace {
      get { return Dummy.RootUnitNamespace; }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IContainerMember<INamespaceDefinition> Members

    public INamespaceDefinition Container {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    #endregion

    #region IScopeMember<IScope<INamespaceMember>> Members

    public IScope<INamespaceMember> ContainingScope {
      get { return Dummy.RootUnitNamespace; }
    }

    #endregion

    #region IFieldDefinition Members

    public uint BitLength {
      get { return 0; }
    }

    public bool IsBitField {
      get { return false; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public bool IsCompileTimeConstant {
      get { return false; }
    }

    public bool IsMapped {
      get { return false; }
    }

    public bool IsMarshalledExplicitly {
      get { return false; }
    }

    public bool IsNotSerialized {
      get { return true; }
    }

    public bool IsReadOnly {
      get { return false; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsStatic {
      get { return false; }
    }

    public ISectionBlock FieldMapping {
      get { return Dummy.SectionBlock; }
    }

    public uint Offset {
      get { return 0; }
    }

    public int SequenceNumber {
      get { return 0; }
    }

    public IMetadataConstant CompileTimeValue {
      get { return Dummy.Constant; }
    }

    public new IMarshallingInformation MarshallingInformation {
      get {
        //^ assume false;
        IMarshallingInformation/*?*/ dummyValue = null;
        //^ assume dummyValue != null;
        return dummyValue;
      }
    }

    public new ITypeReference Type {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Other; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    ITypeDefinition IContainerMember<ITypeDefinition>.Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    IScope<ITypeDefinitionMember> IScopeMember<IScope<ITypeDefinitionMember>>.ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region IFieldReference Members

    public uint InternedKey {
      get { return 0; }
    }

    public IFieldDefinition ResolvedField {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer

    public new IMetadataConstant Constant {
      get { return Dummy.Constant; }
    }

    #endregion

  }

  internal sealed class DummyOperation : Dummy, IOperation {
    #region IOperation Members

    public OperationCode OperationCode {
      get { return OperationCode.Nop; }
    }

    public uint Offset {
      get { return 0; }
    }

    public new ILocation Location {
      get { return Dummy.Location; }
    }

    public object/*?*/ Value {
      get { return null; }
    }

    #endregion
  }

  internal sealed class DummyDocument : Dummy, IDocument {
    #region IDocument Members

    public new string Location {
      get { return string.Empty; }
    }

    public new IName Name {
      get { return Dummy.Name; }
    }

    #endregion
  }

  internal sealed class DummyLocation : Dummy, ILocation {
    #region ILocation Members

    public new IDocument Document {
      get { return Dummy.Document; }
    }

    #endregion
  }

  internal sealed class DummyOperationExceptionInformation : Dummy, IOperationExceptionInformation {
    #region IOperationExceptionInformation Members

    public HandlerKind HandlerKind {
      get { return HandlerKind.Illegal; }
    }

    public ITypeReference ExceptionType {
      get { return Dummy.TypeReference; }
    }

    public uint TryStartOffset {
      get { return 0; }
    }

    public uint TryEndOffset {
      get { return 0; }
    }

    public uint FilterDecisionStartOffset {
      get { return 0; }
    }

    public uint HandlerStartOffset {
      get { return 0; }
    }

    public uint HandlerEndOffset {
      get { return 0; }
    }

    #endregion
  }

  internal sealed class DummyInternFactory : Dummy, IInternFactory {
    #region IInternFactory Members

    public uint GetAssemblyInternedKey(AssemblyIdentity assemblyIdentity) {
      return 0;
    }

    public uint GetModuleInternedKey(ModuleIdentity moduleIdentity) {
      return 0;
    }

    public uint GetFieldInternedKey(IFieldReference fieldReference) {
      return 0;
    }

    public uint GetMethodInternedKey(IMethodReference methodReference) {
      return 0;
    }

    public uint GetVectorTypeReferenceInternedKey(ITypeReference elementTypeReference) {
      return 0;
    }

    public uint GetMatrixTypeReferenceInternedKey(ITypeReference elementTypeReference, int rank, IEnumerable<ulong> sizes, IEnumerable<int> lowerBounds) {
      return 0;
    }

    public uint GetGenericTypeInstanceReferenceInternedKey(ITypeReference genericTypeReference, IEnumerable<ITypeReference> genericArguments) {
      return 0;
    }

    public uint GetPointerTypeReferenceInternedKey(ITypeReference targetTypeReference) {
      return 0;
    }

    public uint GetManagedPointerTypeReferenceInternedKey(ITypeReference targetTypeReference) {
      return 0;
    }

    public uint GetFunctionPointerTypeReferenceInternedKey(CallingConvention callingConvention, IEnumerable<IParameterTypeInformation> parameters, IEnumerable<IParameterTypeInformation> extraArgumentTypes, IEnumerable<ICustomModifier> returnValueCustomModifiers, bool returnValueIsByRef, ITypeReference returnType) {
      return 0;
    }

    public uint GetTypeReferenceInternedKey(ITypeReference typeReference) {
      return 0;
    }

    public uint GetNamespaceTypeReferenceInternedKey(IUnitNamespaceReference containingUnitNamespace, IName typeName, uint genericParameterCount) {
      return 0;
    }

    public uint GetNestedTypeReferenceInternedKey(ITypeReference containingTypeReference, IName typeName, uint genericParameterCount) {
      return 0;
    }

    public uint GetGenericTypeParameterReferenceInternedKey(ITypeReference definingTypeReference, int index) {
      return 0;
    }

    public uint GetModifiedTypeReferenceInternedKey(ITypeReference typeReference, IEnumerable<ICustomModifier> customModifiers) {
      return 0;
    }

    public uint GetGenericMethodParameterReferenceInternedKey(IMethodReference definingMethodReference, int index) {
      return 0;
    }

    #endregion
  }

  internal sealed class DummyArrayType : Dummy, IArrayType {

    #region ITypeDefinition Members

    public ushort Alignment {
      get { return 0; }
    }

    public IEnumerable<ITypeReference> BaseClasses {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public IEnumerable<IEventDefinition> Events {
      get { return IteratorHelper.GetEmptyEnumerable<IEventDefinition>(); }
    }

    public IEnumerable<IFieldDefinition> Fields {
      get { return IteratorHelper.GetEmptyEnumerable<IFieldDefinition>(); }
    }

    public IEnumerable<IMethodDefinition> Methods {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodDefinition>(); }
    }

    public IEnumerable<INestedTypeDefinition> NestedTypes {
      get { return IteratorHelper.GetEmptyEnumerable<INestedTypeDefinition>(); }
    }

    public IEnumerable<IPropertyDefinition> Properties {
      get { return IteratorHelper.GetEmptyEnumerable<IPropertyDefinition>(); }
    }

    public IEnumerable<IMethodImplementation> ExplicitImplementationOverrides {
      get { return IteratorHelper.GetEmptyEnumerable<IMethodImplementation>(); }
    }

    public IEnumerable<IGenericTypeParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericTypeParameter>(); }
    }

    public ushort GenericParameterCount {
      get {
        //^ assume false;
        return 0;
      }
    }

    public IGenericTypeInstanceReference InstanceType {
      get { return Dummy.GenericTypeInstance; }
    }

    public IEnumerable<ITypeReference> Interfaces {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeReference>(); }
    }

    public bool IsAbstract {
      get { return false; }
    }

    public bool IsClass {
      get { return true; }
    }

    public bool IsDelegate {
      get { return false; }
    }

    public bool IsEnum {
      get { return false; }
    }

    public bool IsGeneric {
      get { return false; }
    }

    public bool IsInterface {
      get { return false; }
    }

    public bool IsReferenceType {
      get { return false; }
    }

    public bool IsSealed {
      get { return true; }
    }

    public bool IsStatic {
      get { return true; }
    }

    public bool IsValueType {
      get { return false; }
    }

    public bool IsStruct {
      get { return false; }
    }

    public IEnumerable<ITypeDefinitionMember> Members {
      get { return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>(); }
    }

    public new IPlatformType PlatformType {
      get { return Dummy.PlatformType; }
    }

    public IEnumerable<ITypeDefinitionMember> PrivateHelperMembers {
      get { return this.Members; }
    }

    public uint SizeOf {
      get { return 0; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    public ITypeReference UnderlyingType {
      get { return Dummy.TypeReference; }
    }

    public PrimitiveTypeCode TypeCode {
      get { return PrimitiveTypeCode.Invalid; }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
    }

    public LayoutKind Layout {
      get { return LayoutKind.Auto; }
    }

    public bool IsSpecialName {
      get { return false; }
    }

    public bool IsComObject {
      get { return false; }
    }

    public bool IsSerializable {
      get { return false; }
    }

    public bool IsBeforeFieldInit {
      get { return false; }
    }

    public StringFormatKind StringFormat {
      get { return StringFormatKind.Ansi; }
    }

    public bool IsRuntimeSpecial {
      get { return false; }
    }

    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public void Dispatch(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScope<ITypeDefinitionMember> Members

    public bool Contains(ITypeDefinitionMember member) {
      return false;
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembersNamed(IName name, bool ignoreCase, Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMatchingMembers(Function<ITypeDefinitionMember, bool> predicate) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    public IEnumerable<ITypeDefinitionMember> GetMembersNamed(IName name, bool ignoreCase) {
      return IteratorHelper.GetEmptyEnumerable<ITypeDefinitionMember>();
    }

    #endregion

    #region ITypeReference Members

    public bool IsAlias {
      get { return false; }
    }

    public new IAliasForType AliasForType {
      get { return Dummy.AliasForType; }
    }

    public ITypeDefinition ResolvedType {
      get { return this; }
    }

    public uint InternedKey {
      get { return 0; }
    }

    #endregion

    #region IArrayTypeReference Members

    public ITypeReference ElementType {
      get { return Dummy.TypeReference; }
    }

    public bool IsVector {
      get { return true; }
    }

    public IEnumerable<int> LowerBounds {
      get { return IteratorHelper.GetEmptyEnumerable<int>(); }
    }

    public uint Rank {
      get { return 0; }
    }

    public IEnumerable<ulong> Sizes {
      get { return IteratorHelper.GetEmptyEnumerable<ulong>(); }
    }

    #endregion
  }
#pragma warning restore 1591
}
