// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Microsoft.Cci;
using Microsoft.Cci.UtilityDataStructures;
using Microsoft.Cci.Immutable;
using System.IO;
using Microsoft.Cci.MetadataReader.ObjectModelImplementation;
using Microsoft.Cci.MetadataReader.PEFileFlags;
using Microsoft.Cci.MetadataReader.PEFile;

namespace Microsoft.Cci.MetadataReader {

  /// <summary>
  /// A simple host environment using default settings inherited from WindowsRuntimeMetadataReaderHost and that
  /// uses PeReader as its metadata reader.
  /// </summary>
  public class DefaultWindowsRuntimeHost : WindowsRuntimeMetadataReaderHost {
    PeReader peReader;

    /// <summary>
    /// Allocates a simple host environment using default settings inherited from WindowsRuntimeMetadataReaderHost and that
    /// uses PeReader as its metadata reader.
    /// </summary>
    /// <param name="projectToCLRTypes">True if the host should project references to certain Windows Runtime types and methods
    /// to corresponding CLR types and methods, in order to emulate the runtime behavior of the CLR.</param>
    public DefaultWindowsRuntimeHost(bool projectToCLRTypes = true)
      : base(new NameTable(), new InternFactory(), 0, null, true, projectToCLRTypes) {
      this.peReader = new PeReader(this);
    }

    /// <summary>
    /// Allocates a simple host environment using default settings inherited from MetadataReaderHost and that
    /// uses PeReader as its metadata reader.
    /// </summary>
    /// <param name="nameTable">
    /// A collection of IName instances that represent names that are commonly used during compilation.
    /// This is a provided as a parameter to the host environment in order to allow more than one host
    /// environment to co-exist while agreeing on how to map strings to IName instances.
    /// </param>
    /// <param name="projectToCLRTypes">True if the host should project references to certain Windows Runtime types and methods
    /// to corresponding CLR types and methods, in order to emulate the runtime behavior of the CLR.</param>
    public DefaultWindowsRuntimeHost(INameTable nameTable, bool projectToCLRTypes = true)
      : base(nameTable, new InternFactory(), 0, null, false, projectToCLRTypes) {
      this.peReader = new PeReader(this);
    }

    /// <summary>
    /// Returns the unit that is stored at the given location, or a dummy unit if no unit exists at that location or if the unit at that location is not accessible.
    /// </summary>
    /// <param name="location">A path to the file that contains the unit of metdata to load.</param>
    public override IUnit LoadUnitFrom(string location) {
      IUnit result = this.peReader.OpenModule(
        BinaryDocument.GetBinaryDocumentForFile(location, this));
      this.RegisterAsLatest(result);
      return result;
    }
  }

  /// <summary>
  /// This interface defines the contract between metadata reader and windows runtime host.
  /// Certain projection related fix ups (type and method renaming operations) within winmds
  /// can't be handled via the metadata reader host interface's Redirect() / Rewrite() functionality.
  /// This interace basically defines the functions that the metadata reader can invoke to perform
  /// these fix ups as the metadata is being read off the disk (if and only if projection suport is
  /// enabled in the host).
  /// </summary>
  internal interface IWindowsRuntimeMetadataReaderHost {
    /// <summary>
    /// Returns true if projection support is enabled in the host, false otherwise.
    /// </summary>
    bool ProjectToCLRTypes { get; }

    /// <summary>
    /// Fixes up names and flags for managed winmd classes if projection support is enabled in the host.
    /// - CLR view classes and enums in managed winmds lose the '&lt;CLR&gt;' prefix in their name and become public.
    /// - WinRT view classes and enums in managed winmds get a '&lt;WINRT&gt;' prefix in their name and become private.
    /// </summary>
    /// <remarks>
    /// This is identical to the behavior one sees when one uses ildasm's "/project" option to view the contents
    /// of a managed winmd.
    /// </remarks>
    void FixUpNameAndFlagsForManagedWinMDClassOrEnum(
      PEFileToObjectModel peFileToObjectModel,
      uint typeDefRowId,
      IUnit containingUnit,
      ref IName typeName,
      ref TypeDefFlags flags
    );
    
    /// <summary>
    /// Changes the name of any method that's defined in a winmd that implements IClosable.Close() to 'Dispose'
    /// so that managed consumers can call type.Dispose() directly (without casting to IDisposable).
    /// </summary>
    /// <remarks>
    /// This is identical to the behavior one sees when one uses ildasm's "/project" option to view the contents
    /// of a winmd.
    /// </remarks>
    void FixUpNameForMethodThatImplementsIClosable(
      PEFileToObjectModel peFileToObjectModel,
      uint methodDefRowId,
      MethodFlags flags,
      TypeBase parentModuleType,
      ref IName methodName
    );
  }

  /// <summary>
  /// A base class for an object provided by the application hosting the metadata reader. The object allows the host application
  /// to control how assembly references are unified, where files are found, how Windows Runtime types and methods are projected to CLR types and methods
  /// and so on. The object also controls the lifetime of things such as memory mapped files and blocks of unmanaged memory. Be sure to call Dispose on the object when
  /// it is no longer needed and the associated locks and/or memory must be released immediately.
  /// </summary>
  public abstract class WindowsRuntimeMetadataReaderHost : MetadataReaderHost, IWindowsRuntimeMetadataReaderHost {

    /// <summary>
    /// A base class for an object provided by the application hosting the metadata reader. The object allows the host application
    /// to control how assembly references are unified, where files are found, how Windows Runtime types and methods are projected to CLR types and methods
    /// and so on. The object also controls the lifetime of things such as memory mapped files and blocks of unmanaged memory. Be sure to call Dispose on the object when
    /// it is no longer needed and the associated locks and/or memory must be released immediately.
    /// </summary>
    /// <param name="nameTable">
    /// A collection of IName instances that represent names that are commonly used during compilation.
    /// This is a provided as a parameter to the host environment in order to allow more than one host
    /// environment to co-exist while agreeing on how to map strings to IName instances.
    /// </param>
    /// <param name="factory">
    /// The intern factory to use when generating keys. When comparing two or more assemblies using
    /// TypeHelper, MemberHelper, etc. it is necessary to make the hosts use the same intern factory.
    /// </param>
    /// <param name="pointerSize">The size of a pointer on the runtime that is the target of the metadata units to be loaded
    /// into this metadta host. This parameter only matters if the host application wants to work out what the exact layout
    /// of a struct will be on the target runtime. The framework uses this value in methods such as TypeHelper.SizeOfType and
    /// TypeHelper.TypeAlignment. If the host application does not care about the pointer size it can provide 0 as the value
    /// of this parameter. In that case, the first reference to IMetadataHost.PointerSize will probe the list of loaded assemblies
    /// to find an assembly that either requires 32 bit pointers or 64 bit pointers. If no such assembly is found, the default is 32 bit pointers.
    /// </param>
    /// <param name="searchPaths">
    /// A collection of strings that are interpreted as valid paths which are used to search for units.
    /// </param>
    /// <param name="searchInGAC">
    /// Whether the GAC (Global Assembly Cache) should be searched when resolving references.
    /// </param>
    /// <param name="projectToCLRTypes">True if the host should project references to certain Windows Runtime types and methods
    /// to corresponding CLR types and methods, in order to emulate the runtime behavior of the CLR.</param>
    protected WindowsRuntimeMetadataReaderHost(INameTable nameTable, IInternFactory factory, byte pointerSize, IEnumerable<string> searchPaths,
      bool searchInGAC, bool projectToCLRTypes)
      : base(nameTable, factory, pointerSize, searchPaths, searchInGAC) {
      Contract.Requires(pointerSize == 0 || pointerSize == 4 || pointerSize == 8);
      this.projectToCLRTypes = projectToCLRTypes;
      this.AllowMultiple = nameTable.GetNameFor("AllowMultiple");
      this.AllowMultipleAttribute = nameTable.GetNameFor("AllowMultipleAttribute");
      this.Animation = nameTable.GetNameFor("Animation");
      this.Collections = nameTable.GetNameFor("Collections");
      this.Controls = nameTable.GetNameFor("Controls");
      this.Data = nameTable.GetNameFor("Data");
      this.Foundation = nameTable.GetNameFor("Foundation");
      this.HResult = nameTable.GetNameFor("HResult");
      this.IBindableIterable = nameTable.GetNameFor("IBindableIterable");
      this.IBindableVector = nameTable.GetNameFor("IBindableVector");
      this.IClosable = nameTable.GetNameFor("IClosable");
      this.IIterable = nameTable.GetNameFor("IIterable");
      this.IKeyValuePair = nameTable.GetNameFor("IKeyValuePair");
      this.IMap = nameTable.GetNameFor("IMap");
      this.IMapView = nameTable.GetNameFor("IMapView");
      this.INotifyCollectionChanged = nameTable.GetNameFor("INotifyCollectionChanged");
      this.INotifyPropertyChanged = nameTable.GetNameFor("INotifyPropertyChanged");
      this.Input = nameTable.GetNameFor("Input");
      this.Interop = nameTable.GetNameFor("Interop");
      this.IReference = nameTable.GetNameFor("IReference");
      this.IVector = nameTable.GetNameFor("IVector");
      this.IVectorView = nameTable.GetNameFor("IVectorView");
      this.Media = nameTable.GetNameFor("Media");
      this.Media3D = nameTable.GetNameFor("Media3D");
      this.Metadata = nameTable.GetNameFor("Metadata");
      this.NotifyCollectionChangedAction = nameTable.GetNameFor("NotifyCollectionChangedAction");
      this.NotifyCollectionChangedEventArgs = nameTable.GetNameFor("NotifyCollectionChangedEventArgs");
      this.NotifyCollectionChangedEventHandler = nameTable.GetNameFor("NotifyCollectionChangedEventHandler");
      this.Primitives = nameTable.GetNameFor("Primitives");
      this.PropertyChangedEventArgs = nameTable.GetNameFor("PropertyChangedEventArgs");
      this.PropertyChangedEventHandler = nameTable.GetNameFor("PropertyChangedEventHandler");
      this.TypeName = nameTable.GetNameFor("TypeName");
      this.UI = nameTable.GetNameFor("UI");
      this.Windows = nameTable.GetNameFor("Windows");
      this.Xaml = nameTable.GetNameFor("Xaml");
      this.Numerics = nameTable.GetNameFor("Numerics");
      this.Vector2 = nameTable.GetNameFor("Vector2");
      this.Vector3 = nameTable.GetNameFor("Vector3");
      this.Vector4 = nameTable.GetNameFor("Vector4");
      this.Matrix3x2 = nameTable.GetNameFor("Matrix3x2");
      this.Matrix4x4 = nameTable.GetNameFor("Matrix4x4");
      this.Plane = nameTable.GetNameFor("Plane");
      this.Quaternion = nameTable.GetNameFor("Quaternion");
    }

    bool projectToCLRTypes;
    IName AllowMultiple;
    IName AllowMultipleAttribute;
    IName Animation;
    IName Collections;
    IName Controls;
    IName Data;
    IName Foundation;
    IName HResult;
    IName IBindableIterable;
    IName IBindableVector;
    IName IClosable;
    IName IIterable;
    IName IKeyValuePair;
    IName IMap;
    IName IMapView;
    IName INotifyCollectionChanged;
    IName INotifyPropertyChanged;
    IName Input;
    IName Interop;
    IName IReference;
    IName IVector;
    IName IVectorView;
    IName Media;
    IName Media3D;
    IName Metadata;
    IName NotifyCollectionChangedAction;
    IName NotifyCollectionChangedEventArgs;
    IName NotifyCollectionChangedEventHandler;
    IName Primitives;
    IName PropertyChangedEventArgs;
    IName PropertyChangedEventHandler;
    IName TypeName;
    IName UI;
    IName Windows;
    IName Xaml;
    IName Numerics;
    IName Vector2;
    IName Vector3;
    IName Vector4;
    IName Matrix3x2;
    IName Matrix4x4;
    IName Plane;
    IName Quaternion;
    new WindowsRuntimePlatform platformType;

    /// <summary>
    /// Returns an object that provides a collection of references to types from the core platform, such as System.Object and System.String.
    /// </summary>
    /// <returns></returns>
    protected override IPlatformType GetPlatformType() {
      return this.platformType = new WindowsRuntimePlatform(this);
    }

    /// <summary>
    /// Provides the host with an opportunity to substitute one method definition for another during metadata reading.
    /// This avoids the cost of rewriting the entire unit in order to make such changes.
    /// </summary>
    /// <param name="containingUnit">The unit that is defines the method.</param>
    /// <param name="methodDefinition">A method definition encountered during metadata reading.</param>
    /// <returns>
    /// Usually the value in methodDefinition, but occassionally something else.
    /// </returns>
    public override IMethodDefinition Rewrite(IUnit containingUnit, IMethodDefinition methodDefinition) {
      if (!this.projectToCLRTypes) return methodDefinition;
      var methodDef = methodDefinition as MethodDefinition;
      if (methodDef == null) return methodDefinition;
      var containingType = methodDefinition.ContainingTypeDefinition as INamespaceTypeDefinition;
      if (containingType == null || !containingType.IsForeignObject) return methodDefinition;
      if (containingType.IsDelegate) {
        methodDef.MethodFlags &= ~MethodFlags.AccessMask;
        methodDef.MethodFlags |= MethodFlags.PublicAccess;
        methodDef.MethodImplFlags |= MethodImplFlags.RuntimeCodeType;
      } else {
        methodDef.MethodImplFlags |= MethodImplFlags.RuntimeCodeType|MethodImplFlags.InternalCall;
      }
      return methodDefinition;
    }

    /// <summary>
    /// Provides the host with an opportunity to add, remove or substitute assembly references in the given list.
    /// This avoids the cost of rewriting the entire unit in order to make such changes.
    /// </summary>
    /// <param name="referringUnit">The unit that contains these references.</param>
    /// <param name="assemblyReferences">The assembly references to substitute.</param>
    /// <returns>Usually assemblyReferences, but occasionally a modified enumeration.</returns>
    public override IEnumerable<IAssemblyReference> Redirect(IUnit referringUnit, IEnumerable<IAssemblyReference> assemblyReferences) {
      if (!this.projectToCLRTypes) return assemblyReferences;
      var referringModule = referringUnit as IModule;
      if (referringModule == null || referringModule.ContainingAssembly == null || !(referringModule.ContainingAssembly.ContainsForeignTypes)) return assemblyReferences;
      var platformType = (WindowsRuntimePlatform)this.PlatformType;
      var standardRefs = new SetOfObjects();
      standardRefs.Add(platformType.SystemRuntime.AssemblyIdentity);
      standardRefs.Add(platformType.SystemObjectModel.AssemblyIdentity);
      standardRefs.Add(platformType.SystemRuntimeInteropServicesWindowsRuntime.AssemblyIdentity);
      standardRefs.Add(platformType.SystemRuntimeWindowsRuntime.AssemblyIdentity);
      standardRefs.Add(platformType.SystemRuntimeWindowsRuntimeUIXaml.AssemblyIdentity);
      standardRefs.Add(platformType.SystemNumericsVectors.AssemblyIdentity);
      var result = new List<IAssemblyReference>();
      foreach (var aref in assemblyReferences) {
        if (string.Equals(aref.Name.Value, "mscorlib", StringComparison.OrdinalIgnoreCase)) continue;
        result.Add(aref);
        standardRefs.Remove(aref.AssemblyIdentity);
      }
      if (standardRefs.Contains(platformType.SystemRuntime.AssemblyIdentity)) result.Add(platformType.SystemRuntime);
      if (standardRefs.Contains(platformType.SystemObjectModel.AssemblyIdentity)) result.Add(platformType.SystemObjectModel);
      if (standardRefs.Contains(platformType.SystemRuntimeInteropServicesWindowsRuntime.AssemblyIdentity)) result.Add(platformType.SystemRuntimeInteropServicesWindowsRuntime);
      if (standardRefs.Contains(platformType.SystemRuntimeWindowsRuntime.AssemblyIdentity)) result.Add(platformType.SystemRuntimeWindowsRuntime);
      if (standardRefs.Contains(platformType.SystemRuntimeWindowsRuntimeUIXaml.AssemblyIdentity)) result.Add(platformType.SystemRuntimeWindowsRuntimeUIXaml);
      if (standardRefs.Contains(platformType.SystemNumericsVectors.AssemblyIdentity)) result.Add(platformType.SystemNumericsVectors);
      return IteratorHelper.GetReadonly(result.ToArray());
    }

    /// <summary>
    /// Provides the host with an opportunity to substitute one type reference for another during metadata reading.
    /// This avoids the cost of rewriting the entire unit in order to make such changes.
    /// </summary>
    /// <param name="referringUnit">The unit that contains the reference.</param>
    /// <param name="typeReference">A type reference encountered during metadata reading.</param>
    /// <returns>
    /// Usually the value in typeReference, but occassionally something else.
    /// </returns>
    public override INamedTypeReference Redirect(IUnit referringUnit, INamedTypeReference typeReference) {
      // Redirect type references but not type defintions.
      if (!this.projectToCLRTypes || (typeReference is ITypeDefinition)) return typeReference;
      var referringModule = referringUnit as IModule;
      if (referringModule == null || referringModule.ContainingAssembly == null || !(referringModule.ContainingAssembly.ContainsForeignTypes)) return typeReference;
      var platformType = (WindowsRuntimePlatform)this.PlatformType;
      var namespaceTypeReference = typeReference as INamespaceTypeReference;
      if (namespaceTypeReference == null) return typeReference;
      var namespaceReference = namespaceTypeReference.ContainingUnitNamespace as INestedUnitNamespaceReference;
      if (namespaceReference == null) return typeReference;
      if (this.IsWindowsFoundationMetadata(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.SystemAttributeUsageAttribute.Name) return platformType.SystemAttributeUsageAttribute;
        if (namespaceTypeReference.Name == platformType.SystemAttributeTargets.Name) return platformType.SystemAttributeTargets;
      } else if (this.IsWindowsUI(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.WindowsUIColor.Name) return platformType.WindowsUIColor;
      } else if (this.IsWindowsFoundation(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.SystemDateTime.Name) return platformType.SystemDateTimeOffset;
        if (namespaceTypeReference.Name == platformType.SystemEventHandler1.Name && namespaceTypeReference.GenericParameterCount == 1)
          return platformType.SystemEventHandler1;
        if (namespaceTypeReference.Name == platformType.SystemRuntimeInteropServicesWindowsRuntimeEventRegistrationToken.Name)
          return platformType.SystemRuntimeInteropServicesWindowsRuntimeEventRegistrationToken;
        if (namespaceTypeReference.Name == this.HResult) return platformType.SystemException;
        if (namespaceTypeReference.Name == this.IReference && namespaceTypeReference.GenericParameterCount == 1)
          return platformType.SystemNullable1;
        if (namespaceTypeReference.Name == platformType.WindowsFoundationPoint.Name) return platformType.WindowsFoundationPoint;
        if (namespaceTypeReference.Name == platformType.WindowsFoundationRect.Name) return platformType.WindowsFoundationRect;
        if (namespaceTypeReference.Name == platformType.WindowsFoundationSize.Name) return platformType.WindowsFoundationSize;
        if (namespaceTypeReference.Name == platformType.SystemTimeSpan.Name) return platformType.SystemTimeSpan;
        if (namespaceTypeReference.Name == platformType.SystemUri.Name) return platformType.SystemUri;
        if (namespaceTypeReference.Name == this.IClosable) return platformType.SystemIDisposable;
      } else if (this.IsWindowsFoundationCollections(namespaceReference)) {
        if (namespaceTypeReference.Name == this.IIterable) return platformType.SystemCollectionsGenericIEnumerable;
        if (namespaceTypeReference.Name == this.IVector) return platformType.SystemCollectionsGenericIList;
        if (namespaceTypeReference.Name == this.IVectorView) return platformType.SystemCollectionsGenericIReadOnlyList;
        if (namespaceTypeReference.Name == this.IMap) return platformType.SystemCollectionsGenericIDictionary;
        if (namespaceTypeReference.Name == this.IMapView) return platformType.SystemCollectionsGenericIReadOnlyDictionary;
        if (namespaceTypeReference.Name == this.IKeyValuePair) return platformType.SystemCollectionsGenericKeyValuePair;
      } else if (this.IsWindowsUIXamlInput(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.SystemWindowsInputICommand.Name) return platformType.SystemWindowsInputICommand;
      } else if (this.IsWindowsUIXamlInterop(namespaceReference)) {
        if (namespaceTypeReference.Name == this.IBindableIterable) return platformType.SystemCollectionsIEnumerable;
        if (namespaceTypeReference.Name == this.IBindableVector) return platformType.SystemCollectionsIList;
        if (namespaceTypeReference.Name == this.INotifyCollectionChanged) return platformType.SystemCollectionsSpecializedINotifyCollectionChanged;
        if (namespaceTypeReference.Name == this.NotifyCollectionChangedEventHandler) return platformType.SystemCollectionsSpecializedNotifyCollectionChangedEventHandler;
        if (namespaceTypeReference.Name == this.NotifyCollectionChangedEventArgs) return platformType.SystemCollectionsSpecializedNotifyCollectionChangedEventArgs;
        if (namespaceTypeReference.Name == this.NotifyCollectionChangedAction) return platformType.SystemCollectionsSpecializedNotifyCollectionChangedAction;
        if (namespaceTypeReference.Name == this.TypeName) return platformType.SystemType;
      } else if (this.IsWindowsUIXamlData(namespaceReference)) {
        if (namespaceTypeReference.Name == this.INotifyPropertyChanged) return platformType.SystemComponentModelINotifyPropertyChanged;
        if (namespaceTypeReference.Name == this.PropertyChangedEventArgs) return platformType.SystemComponentModelPropertyChangedEventArgs;
        if (namespaceTypeReference.Name == this.PropertyChangedEventHandler) return platformType.SystemComponentModelPropertyChangedEventHandler;
      } else if (this.IsWindowsUIXaml(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlCornerRadius.Name) return platformType.WindowsUIXamlCornerRadius;
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlDuration.Name) return platformType.WindowsUIXamlDuration;
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlDurationType.Name) return platformType.WindowsUIXamlDurationType;
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlGridLength.Name) return platformType.WindowsUIXamlGridLength;
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlGridUnitType.Name) return platformType.WindowsUIXamlGridUnitType;
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlThickness.Name) return platformType.WindowsUIXamlThickness;
      } else if (this.IsWindowsUIXamlControlsPrimitives(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlControlsPrimitivesGeneratorPosition.Name)
          return platformType.WindowsUIXamlControlsPrimitivesGeneratorPosition;
      } else if (this.IsWindowsUIXamlMedia(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlMediaMatrix.Name) return platformType.WindowsUIXamlMediaMatrix;
      } else if (this.IsWindowsUIXamlMediaAnimation(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlMediaAnimationKeyTime.Name)
          return platformType.WindowsUIXamlMediaAnimationKeyTime;
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlMediaAnimationRepeatBehavior.Name)
          return platformType.WindowsUIXamlMediaAnimationRepeatBehavior;
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlMediaAnimationRepeatBehaviorType.Name)
          return platformType.WindowsUIXamlMediaAnimationRepeatBehaviorType;
      } else if (this.IsWindowsUIXamlMediaMedia3D(namespaceReference)) {
        if (namespaceTypeReference.Name == platformType.WindowsUIXamlMediaMedia3DMatrix3D.Name)
          return platformType.WindowsUIXamlMediaMedia3DMatrix3D;
      }
      else if (this.IsWindowsFoundationNumerics(namespaceReference)) {
        if (namespaceTypeReference.Name == this.Vector2) return platformType.SystemNumericsVector2;
        if (namespaceTypeReference.Name == this.Vector3) return platformType.SystemNumericsVector3;
        if (namespaceTypeReference.Name == this.Vector4) return platformType.SystemNumericsVector4;
        if (namespaceTypeReference.Name == this.Matrix3x2) return platformType.SystemNumericsMatrix3x2;
        if (namespaceTypeReference.Name == this.Matrix4x4) return platformType.SystemNumericsMatrix4x4;
        if (namespaceTypeReference.Name == this.Plane) return platformType.SystemNumericsPlane;
        if (namespaceTypeReference.Name == this.Quaternion) return platformType.SystemNumericsQuaternion;
      }

      return typeReference;
    }
    
    /// <summary />
    public const string PREFIX_CLR = "<CLR>";
    /// <summary />
    public const int PREFIX_CLR_LENGTH = 5;
    /// <summary />
    public const string PREFIX_WINRT = "<WinRT>";
    /// <summary />
    public const int PREFIX_WINRT_LENGTH = 7;

    bool IWindowsRuntimeMetadataReaderHost.ProjectToCLRTypes { 
      get {
        return this.projectToCLRTypes;
      }
    }

    // Fixes up names and flags for managed winmd classes if projection support is enabled in the host.
    // - CLR view classes and enums in managed winmds lose the '<CLR>' prefix in their name and become public.
    // - WinRT view classes and enums in managed winmds get a '<WinRT>' prefix in their name and become private.
    // This is identical to the behavior one sees when one uses ildasm's "/project" option to view the contents
    // of a managed winmd.
    void IWindowsRuntimeMetadataReaderHost.FixUpNameAndFlagsForManagedWinMDClassOrEnum(
      PEFileToObjectModel peFileToObjectModel,
      uint typeDefRowId,
      IUnit containingUnit,
      ref IName typeName,
      ref TypeDefFlags flags
    ) {
      IWindowsRuntimeMetadataReaderHost host = peFileToObjectModel.ModuleReader.metadataReaderHost as WindowsRuntimeMetadataReaderHost;
      if (this.projectToCLRTypes) {
        IName baseTypeNamespaceName = null;
        IName baseTypeName = null;
        uint baseTypeToken = peFileToObjectModel.PEFileReader.TypeDefTable.GetExtends(typeDefRowId);
        uint baseTypeRowId = baseTypeToken & TokenTypeIds.RIDMask;
        if (baseTypeRowId != 0) {
          uint tokenType = baseTypeToken & TokenTypeIds.TokenTypeMask;
          switch(tokenType) {
            case TokenTypeIds.TypeDef:
              TypeDefRow baseTypeDefRow = peFileToObjectModel.PEFileReader.TypeDefTable[baseTypeRowId];
              baseTypeNamespaceName = peFileToObjectModel.GetNameFromOffset(baseTypeDefRow.Namespace);
              baseTypeName = peFileToObjectModel.GetNameFromOffset(baseTypeDefRow.Name);
              break;
            case TokenTypeIds.TypeRef:
              TypeRefRow baseTypeRefRow = peFileToObjectModel.PEFileReader.TypeRefTable[baseTypeRowId];
              baseTypeNamespaceName = peFileToObjectModel.GetNameFromOffset(baseTypeRefRow.Namespace);
              baseTypeName = peFileToObjectModel.GetNameFromOffset(baseTypeRefRow.Name);
              break;
            case TokenTypeIds.TypeSpec:
              // We don't care about TypeSpecs because managed winmd types can never inherit generic types.
            default:
              break;
          }
          if (baseTypeName != null) {
            IAssemblyReference containingUnitMscorlibReference = peFileToObjectModel.GetMscorlibReference();
            this.FixUpNameAndFlagsForManagedWinMDClassOrEnum(containingUnit, containingUnitMscorlibReference, baseTypeNamespaceName, baseTypeName, ref typeName, ref flags);
          }
        }
      }
    }

    private void FixUpNameAndFlagsForManagedWinMDClassOrEnum(
      IUnit containingUnit,
      IAssemblyReference containingUnitMscorlibReference,
      IName baseTypeNamespaceName,
      IName baseTypeName,
      ref IName typeName,
      ref TypeDefFlags typeDefFlags
    ) {
      if (this.projectToCLRTypes && IsManagedWinMD(containingUnit)) {
        if (ShouldFixUpWinRTViewClassOrEnum(containingUnitMscorlibReference, baseTypeNamespaceName, baseTypeName, typeDefFlags)) {
          typeName = NameTable.GetNameFor(PREFIX_WINRT + typeName.Value);
          typeDefFlags = (typeDefFlags & ~TypeDefFlags.AccessMask) | TypeDefFlags.PrivateAccess;
          // NOTE: Ildasm /project also adds the following "import" flag on <WinRT> types - however, we omit this since
          // this is not required so far as .net native toolchain and mcg goes. This also avoids having to add special
          // case code to avoid treating this type as a real COM type in the reduced copy transform (see isComImport
          // check in ReducedCopyReferenceEngine.cs).
          // typeDefFlags = typeDefFlags | TypeDefFlags.ImportImplementation;
        } else if (ShouldFixUpCLRViewClassOrEnum(typeName, typeDefFlags)) {
          typeName = NameTable.GetNameFor(typeName.Value.Substring(PREFIX_CLR_LENGTH));
          typeDefFlags = (typeDefFlags & ~TypeDefFlags.AccessMask) | TypeDefFlags.PublicAccess;
          typeDefFlags = typeDefFlags & ~TypeDefFlags.SpecialNameSemantics;
        }
      }
    }
    
    private bool ShouldFixUpWinRTViewClassOrEnum(
      IAssemblyReference containingUnitMscorlibReference,
      IName baseTypeNamespaceName,
      IName baseTypeName,
      TypeDefFlags typeDefFlags
    ) {
      if ((typeDefFlags & TypeDefFlags.PublicAccess) == TypeDefFlags.PublicAccess &&
          // By design, managed winmd classes should always be sealed - however, a bug in winmdexp.exe allows non-sealed
          // managed winmd classes to be created if the class does not have a public constructor - see bug 188518. So we
          // skip the check for the sealed flag here in order to avoid breaking any apps that already depend on this
          // winmdexp.exe bug. This matches the CLR which also doesn't check for the sealed flag when fixing up
          // managed winmd types (see ndp\clr\src\md\winmd\adapter.cpp).
          // (typeDefFlags & TypeDefFlags.SealedSemantics) == TypeDefFlags.SealedSemantics &&
          (typeDefFlags & TypeDefFlags.IsForeign) == TypeDefFlags.IsForeign &&
          (typeDefFlags & TypeDefFlags.InterfaceSemantics) != TypeDefFlags.InterfaceSemantics && // Not an interface
          !(baseTypeNamespaceName.Value == "System" && baseTypeName.Value == "ValueType") && // Not a struct
          !(baseTypeNamespaceName.Value == "System" && baseTypeName.Value == "MulticastDelegate") && // Not a delegate
          !(baseTypeNamespaceName.Value == "System" && baseTypeName.Value == "Attribute")) { // Not an attribute

        // We have a managed winmd class or an enum in WinRT view.
        bool isEnum = baseTypeNamespaceName.Value == "System" && baseTypeName.Value == "Enum";
        bool referencesMscorlibv4 = 
                    containingUnitMscorlibReference != null && !(containingUnitMscorlibReference is Dummy) &&
                    containingUnitMscorlibReference.Version.Major == 4;
        bool isSpecialName = (typeDefFlags & TypeDefFlags.SpecialNameSemantics) == TypeDefFlags.SpecialNameSemantics;

        if (isEnum && referencesMscorlibv4 && !isSpecialName) {
          // This is a back-compat quirk. Managed winmd enums exported with an older WinMDExp.exe have only one view (i.e. no
          // corresponding <CLR> enum). These enums should *not* be mangled with a <WinRT> prefix and flipped to private.
          // The only way to check whether the older WinMDExp was used is to check the version of the mscorlib referenced
          // by the managed winmd (if referenced mscorlib has version 4.0 then the winmd was produced using the older WinMDExp -
          // otherwise (if the referenced mscorlib has version 255.255.255.255) then the winmd was produced using newer WinMDExp).
          // These checks are basically a copy of the checks in the CLR (see ndp\clr\src\md\winmd\adapter.cpp).
          return false;
        }

        return true;
      }

      return false;
    }
    
    private static bool ShouldFixUpCLRViewClassOrEnum(
      IName typeName,
      TypeDefFlags typeDefFlags
    ) {
      return (typeDefFlags & TypeDefFlags.PrivateAccess) == TypeDefFlags.PrivateAccess &&
             // By design, managed winmd classes should always be sealed - however, a bug in winmdexp.exe allows non-sealed
             // managed winmd classes to be created if the class does not have a public constructor - see bug 188518. So we
             // skip the check for the sealed flag here in order to avoid breaking any apps that already depend on this
             // winmdexp.exe bug. This matches the CLR which also doesn't check for the sealed flag when fixing up
             // managed winmd types (see ndp\clr\src\md\winmd\adapter.cpp).
             // (typeDefFlags & TypeDefFlags.SealedSemantics) == TypeDefFlags.SealedSemantics &&
             (typeDefFlags & TypeDefFlags.SpecialNameSemantics) == TypeDefFlags.SpecialNameSemantics &&
             typeName.Value.StartsWith(PREFIX_CLR, StringComparison.Ordinal);
    }

    // Changes the name of any method that's defined in a winmd that implements IClosable.Close() to 'Dispose'
    // so that managed consumers can call type.Dispose() directly (without casting to IDisposable).
    // This is identical to the behavior one sees when one uses ildasm's "/project" option to view the contents
    // of a winmd.
    void IWindowsRuntimeMetadataReaderHost.FixUpNameForMethodThatImplementsIClosable(
      PEFileToObjectModel peFileToObjectModel,
      uint methodDefRowId,
      MethodFlags flags,
      TypeBase parentModuleType,
      ref IName methodName
    ) {
      if (projectToCLRTypes) {
        INamespaceTypeDefinition parentNamespaceType = parentModuleType as INamespaceTypeDefinition;
        if (parentNamespaceType != null && IsWinMD(parentNamespaceType.ContainingUnitNamespace.Unit) && parentNamespaceType.IsClass) {
          // Is the method part of a class that is defined in a winmd?
          // Note: We don't care about structs since WinRT structs can't implement interfaces.
          uint paramCount;
          peFileToObjectModel.PEFileReader.GetParamInformation(methodDefRowId, out paramCount);
          if (paramCount == 0) {
            // Is this a method that takes 0 parameters?
            // Note: Desktop CLR doesn't check whether the method is public (or whether it is named Close) - so we don't either.
            uint methodImplStart, methodImplEnd;
            peFileToObjectModel.GetMethodImplInfoForType(parentModuleType, out methodImplStart, out methodImplEnd);
            // Does this method implement IClosable.Close()?
            for (uint methodImplIter = methodImplStart; methodImplIter < methodImplEnd; ++methodImplIter) {
              MethodImplRow methodImplRow = peFileToObjectModel.PEFileReader.MethodImplTable[methodImplIter];
              uint tokenKind = methodImplRow.MethodBody & TokenTypeIds.TokenTypeMask;
              uint rowId = methodImplRow.MethodBody & TokenTypeIds.RIDMask;
              IMethodReference implementedMethod = peFileToObjectModel.GetMethodReferenceForToken(parentModuleType, methodImplRow.MethodDeclaration);
              INamespaceTypeReference implementedInterface = implementedMethod.ContainingType as INamespaceTypeReference;
              if (implementedInterface != null &&
                  tokenKind == TokenTypeIds.MethodDef && rowId == methodDefRowId) {
                string implementedInterfaceName = TypeHelper.GetTypeName(implementedInterface);
                // Note: We will see redirected type IDisposable here (instead of IClosable) since projection support is enabled.
                if (implementedInterfaceName == "System.IDisposable") {
                  // Note: We have a bug here. We should be checking whether the class (or one of its base classes) already has a method
                  // named Dispose before we introduce a new one with the same signature. However, CLR has the same bug - in this case,
                  // the class ends up with two methods named Dispose with the same signature. If someone were to try calling class.Dispose()
                  // in this case, they would see an error from the C# compiler (i.e. C# cannot resolve the ambiguity between the two identical
                  // methods) - but calling either of the Dispose() methods via their respective interfaces (e.g. ((IDisposable)class).Dispose())
                  // should work. The case where we would end up with two Dispose() methods as described above should be very rare for a winmd
                  // class. For now, we replicate the buggy CLR behavior since we want to avoid looking at all methods in the class / in all its
                  // base classes (some of which may be in a different assembly) here.
                  methodName = NameTable.GetNameFor("Dispose");
                  break;
                }
              }
            }
          }
        }
      }
    }

    /// <summary />
    public const string VERSION_PREFIX_CLR = "CLR";
    /// <summary />
    public const string VERSION_PREFIX_WINRT = "WindowsRuntime";

    private static bool IsManagedWinMD(IUnitReference unitRef) {
      IAssembly assembly = unitRef.ResolvedUnit as IAssembly;
      if (assembly == null || assembly is Dummy || !assembly.ContainsForeignTypes)
        return false;

      foreach (string version in assembly.TargetRuntimeVersion.Split(';')) {
        if (version.StartsWith(VERSION_PREFIX_CLR, StringComparison.OrdinalIgnoreCase)) {
          return true;
        }
      }

      return false;
    }
        
    private static bool IsWinMD(IUnitReference unitRef) {
      IAssembly assembly = unitRef.ResolvedUnit as IAssembly;
      if (assembly == null || assembly is Dummy || !assembly.ContainsForeignTypes)
        return false;

      foreach (string version in assembly.TargetRuntimeVersion.Split(';')) {
        if (version.StartsWith(VERSION_PREFIX_WINRT, StringComparison.OrdinalIgnoreCase)) {
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// Provides the host with an opportunity to substitute a custom attribute with another during metadata reading.
    /// This avoids the cost of rewriting the entire unit in order to make such changes.
    /// </summary>
    /// <param name="referringUnit">The unit that contains the custom attribute.</param>
    /// <param name="customAttribute">The custom attribute to rewrite (fix up).</param>
    /// <returns>
    /// Usually the value in customAttribute, but occassionally another custom attribute.
    /// </returns>
    public override ICustomAttribute Rewrite(IUnit referringUnit, ICustomAttribute customAttribute) {
      if (!this.projectToCLRTypes) return customAttribute;
      CustomAttribute customAttr = customAttribute as CustomAttribute;
      if (customAttr == null) return customAttribute;
      var referringModule = referringUnit as IModule;
      if (referringModule == null || referringModule.ContainingAssembly == null || !(referringModule.ContainingAssembly.ContainsForeignTypes)) return customAttribute;
      if (!TypeHelper.TypesAreEquivalent(customAttribute.Type, this.PlatformType.SystemAttributeUsageAttribute)) return customAttribute;
      //The custom attribute constructor has been redirected from Windows.Foundation.AttributeUsageAttribute, which has a different
      //set of flags from System.AttributeUsageAttribute for its first and only constructor parameter and also does not have an AllowMultiple property. 
      var argArray = customAttr.Arguments;
      if (argArray == null || argArray.Length != 1) return customAttribute;
      var argConst = argArray[0] as ConstantExpression;
      if (argConst == null || !(argConst.value is int)) return customAttribute;
      int clrEnumValue = 0;
      switch ((int)argConst.Value) {
        case 0x00000001: clrEnumValue = 0x00001000; break;
        case 0x00000002: clrEnumValue = 0x00000010; break;
        case 0x00000004: clrEnumValue = 0x00000200; break;
        case 0x00000008: clrEnumValue = 0x00000100; break;
        case 0x00000010: clrEnumValue = 0x00000400; break;
        case 0x00000020: clrEnumValue = 0x00000000; break;
        case 0x00000040: clrEnumValue = 0x00000040; break;
        case 0x00000080: clrEnumValue = 0x00000800; break;
        case 0x00000100: clrEnumValue = 0x00000080; break;
        case 0x00000200: clrEnumValue = 0x00000004; break;
        case 0x00000400: clrEnumValue = 0x00000008; break;
        case 0x00000800: clrEnumValue = 0x00000000; break;
        case -1: clrEnumValue = 0x00007FFF; break;
      }
      argConst.value = clrEnumValue;
      if (this.FellowCustomAttributeIncludeAllowMultiple(customAttr)) {
        if (customAttr.NamedArguments != null) return customAttribute;
        var trueVal = new ConstantExpression(this.PlatformType.SystemBoolean, true);
        var namedArgArray = new IMetadataNamedArgument[1];
        namedArgArray[0] = new FieldOrPropertyNamedArgumentExpression(this.AllowMultiple, Dummy.Type, false, this.PlatformType.SystemBoolean, trueVal);
        customAttr.NamedArguments = namedArgArray;
      }
      return customAttribute;
    }

    private bool FellowCustomAttributeIncludeAllowMultiple(CustomAttribute customAttr) {
      foreach (var customAttribute in customAttr.PEFileToObjectModel.GetAttributesForSameParentAs(customAttr.TokenValue)) {
        var caType = customAttribute.Type as INamespaceTypeReference;
        if (caType != null && caType.Name == this.AllowMultipleAttribute && this.IsWindowsFoundation(caType.ContainingUnitNamespace as INestedUnitNamespaceReference))
          return true;
      }
      return false;
    }

    /// <summary>
    /// Default implementation of UnifyAssembly. Override this method to change the behavior.
    /// </summary>
    public override AssemblyIdentity UnifyAssembly(AssemblyIdentity assemblyIdentity) {
      if (!this.projectToCLRTypes) return assemblyIdentity;
      if (assemblyIdentity.Name.UniqueKeyIgnoringCase == this.CoreAssemblySymbolicIdentity.Name.UniqueKeyIgnoringCase &&
        assemblyIdentity.Culture == this.CoreAssemblySymbolicIdentity.Culture &&
        IteratorHelper.IEquatableEnumerablesAreEqual(assemblyIdentity.PublicKeyToken, this.CoreAssemblySymbolicIdentity.PublicKeyToken))
        return this.CoreAssemblySymbolicIdentity;
      if (this.CoreIdentities.Contains(assemblyIdentity)) return this.CoreAssemblySymbolicIdentity;
      if (string.Equals(assemblyIdentity.Name.Value, "mscorlib", StringComparison.OrdinalIgnoreCase) && assemblyIdentity.Version == new Version(255, 255, 255, 255))
        return this.CoreAssemblySymbolicIdentity;
      return assemblyIdentity;
    }

    private bool IsWindows(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Windows) return false;
      return namespaceReference.ContainingUnitNamespace is IRootUnitNamespaceReference;
    }

    private bool IsWindowsFoundation(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Foundation) return false;
      return IsWindows(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsFoundationCollections(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Collections) return false;
      return IsWindowsFoundation(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsFoundationMetadata(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Metadata) return false;
      return IsWindowsFoundation(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUI(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.UI) return false;
      return IsWindows(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXaml(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Xaml) return false;
      return IsWindowsUI(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlControls(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Controls) return false;
      return IsWindowsUIXaml(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlControlsPrimitives(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Primitives) return false;
      return IsWindowsUIXamlControls(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlData(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Data) return false;
      return IsWindowsUIXaml(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlInput(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Input) return false;
      return IsWindowsUIXaml(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlInterop(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Interop) return false;
      return IsWindowsUIXaml(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlMedia(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Media) return false;
      return IsWindowsUIXaml(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlMediaAnimation(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Animation) return false;
      return IsWindowsUIXamlMedia(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsUIXamlMediaMedia3D(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Media3D) return false;
      return IsWindowsUIXamlMedia(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }

    private bool IsWindowsFoundationNumerics(INestedUnitNamespaceReference/*?*/ namespaceReference) {
      if (namespaceReference == null || namespaceReference.Name != this.Numerics) return false;
      return IsWindowsFoundation(namespaceReference.ContainingUnitNamespace as INestedUnitNamespaceReference);
    }
  }

  class WindowsRuntimePlatform : PlatformType {

    /// <summary>
    /// Allocates a collection of references to types from the core platform, such as System.Object and System.String.
    /// </summary>
    /// <param name="host">
    /// An object that provides a standard abstraction over the applications that host components that provide or consume objects from the metadata model.
    /// </param>
    internal WindowsRuntimePlatform(IMetadataHost host) 
      : base(host) {
    }

    /// <summary>
    /// A reference to the CLR System.Runtime contract assembly
    /// </summary>
    internal IAssemblyReference SystemRuntime {
      get {
        if (this.systemRuntime == null)
          this.systemRuntime = new Microsoft.Cci.Immutable.AssemblyReference(this.host, this.GetSystemRuntimeSymbolicIdentity());
        return this.systemRuntime;
      }
    }
    private IAssemblyReference/*?*/ systemRuntime;

    /// <summary>
    /// Returns an identity that is the same as CoreAssemblyIdentity, except that the name is "System.Runtime".
    /// </summary>
    private AssemblyIdentity GetSystemRuntimeSymbolicIdentity() {
      var core = this.host.CoreAssemblySymbolicIdentity;
      var name = this.host.NameTable.GetNameFor("System.Runtime");
      var location = "";
      if (core.Location.Length > 0)
        location = Path.Combine(Path.GetDirectoryName(core.Location)??"", "System.Runtime.dll");
      return new AssemblyIdentity(name, core.Culture, core.Version, core.PublicKeyToken, location);
    }

    /// <summary>
    /// A reference to the CLR System.ObjectModel contract assembly
    /// </summary>
    internal IAssemblyReference SystemObjectModel {
      get {
        if (this.systemObjectModel == null)
          this.systemObjectModel = new Microsoft.Cci.Immutable.AssemblyReference(this.host, this.GetSystemObjectModelSymbolicIdentity());
        return this.systemObjectModel;
      }
    }
    private IAssemblyReference/*?*/ systemObjectModel;

    /// <summary>
    /// Returns an identity that is the same as CoreAssemblyIdentity, except that the name is "System.ObjectModel".
    /// </summary>
    private AssemblyIdentity GetSystemObjectModelSymbolicIdentity() {
      var core = this.host.CoreAssemblySymbolicIdentity;
      var name = this.host.NameTable.GetNameFor("System.ObjectModel");
      var location = "";
      if (core.Location.Length > 0)
        location = Path.Combine(Path.GetDirectoryName(core.Location)??"", "System.ObjectModel.dll");
      return new AssemblyIdentity(name, core.Culture, core.Version, core.PublicKeyToken, location);
    }

    /// <summary>
    /// A reference to the assembly that contains CLR types to substitute for Windows Runtime interop types, i.e. System.Runtime.InteropServices.WindowsRuntime.
    /// </summary>
    internal IAssemblyReference SystemRuntimeInteropServicesWindowsRuntime {
      get {
        if (this.systemRuntimeInteropServicesWindowsRuntime == null)
          this.systemRuntimeInteropServicesWindowsRuntime = new Microsoft.Cci.Immutable.AssemblyReference(this.host, this.GetSystemRuntimeInteropServicesWindowsRuntimeSymbolicIdentity());
        return this.systemRuntimeInteropServicesWindowsRuntime;
      }
    }
    private IAssemblyReference/*?*/ systemRuntimeInteropServicesWindowsRuntime;

    /// <summary>
    /// Returns an identity that is the same as CoreAssemblyIdentity, except that the name is "System.Runtime.InteropServices.WindowsRuntime" and the version is at least 4.0.
    /// </summary>
    private AssemblyIdentity GetSystemRuntimeInteropServicesWindowsRuntimeSymbolicIdentity() {
      var core = this.host.CoreAssemblySymbolicIdentity;
      var name = this.host.NameTable.GetNameFor("System.Runtime.InteropServices.WindowsRuntime");
      var location = "";
      if (core.Location.Length > 0)
        location = Path.Combine(Path.GetDirectoryName(core.Location)??"", "System.Runtime.InteropServices.WindowsRuntime.dll");
      var version = new Version(4, 0, 0, 0);
      if (version < core.Version) version = core.Version;
      return new AssemblyIdentity(name, core.Culture, version, core.PublicKeyToken, location);
    }

    /// <summary>
    /// A reference to the assembly that contains CLR types to substitute for Windows Runtime types.
    /// </summary>
    internal IAssemblyReference SystemRuntimeWindowsRuntime {
      get {
        if (this.systemRuntimeWindowsRuntime == null)
          this.systemRuntimeWindowsRuntime = new Microsoft.Cci.Immutable.AssemblyReference(this.host, this.GetSystemRuntimeWindowsRuntimeSymbolicIdentity());
        return this.systemRuntimeWindowsRuntime;
      }
    }
    private IAssemblyReference/*?*/ systemRuntimeWindowsRuntime;

    /// <summary>
    /// Returns an identity that is the same as CoreAssemblyIdentity, except that the name is "System.Runtime.WindowsRuntime" and the version is at least 4.0.
    /// </summary>
    private AssemblyIdentity GetSystemRuntimeWindowsRuntimeSymbolicIdentity() {
      var core = this.host.CoreAssemblySymbolicIdentity;
      var name = this.host.NameTable.GetNameFor("System.Runtime.WindowsRuntime");
      var location = "";
      if (core.Location.Length > 0)
        location = Path.Combine(Path.GetDirectoryName(core.Location)??"", "System.Runtime.WindowsRuntime.dll");
      var version = new Version(4, 0, 0, 0);
      if (version < core.Version) version = core.Version;
      return new AssemblyIdentity(name, core.Culture, version, core.PublicKeyToken, location);
    }

    /// <summary>
    /// A reference to the assembly that contains Xaml types to substitute for Windows Runtime types.
    /// </summary>
    internal IAssemblyReference SystemRuntimeWindowsRuntimeUIXaml {
      get {
        if (this.systemRuntimeWindowsRuntimeUIXaml == null)
          this.systemRuntimeWindowsRuntimeUIXaml = new Microsoft.Cci.Immutable.AssemblyReference(this.host, this.GetSystemRuntimeWindowsRuntimeUIXamlSymbolicIdentity());
        return this.systemRuntimeWindowsRuntimeUIXaml;
      }
    }
    private IAssemblyReference/*?*/ systemRuntimeWindowsRuntimeUIXaml;

    /// <summary>
    /// Returns an identity that is the same as CoreAssemblyIdentity, except that the name is "System.Runtime.WindowsRuntime.UI.Xaml" and the version is at least 4.0.
    /// </summary>
    private AssemblyIdentity GetSystemRuntimeWindowsRuntimeUIXamlSymbolicIdentity() {
      var core = this.host.CoreAssemblySymbolicIdentity;
      var name = this.host.NameTable.GetNameFor("System.Runtime.WindowsRuntime.UI.Xaml");
      var location = "";
      if (core.Location.Length > 0)
        location = Path.Combine(Path.GetDirectoryName(core.Location)??"", "System.Runtime.WindowsRuntime.UI.Xaml.dll");
      var version = new Version(4, 0, 0, 0);
      if (version < core.Version) version = core.Version;
      return new AssemblyIdentity(name, core.Culture, version, core.PublicKeyToken, location);
    }

    /// <summary>
    /// A reference to the assembly that contains CLR numeric types (like System.Numerics.Matrix3x2) to substitute for corresponding Windows Runtime types.
    /// </summary>
    internal IAssemblyReference SystemNumericsVectors {
      get {
        if (this.systemNumericsVectors == null)
          this.systemNumericsVectors = new Microsoft.Cci.Immutable.AssemblyReference(this.host, this.GetSystemNumericsVectorsSymbolicIdentity());
        return this.systemNumericsVectors;
      }
    }
    private IAssemblyReference/*?*/ systemNumericsVectors;

    /// <summary>
    /// Returns an identity that is the same as CoreAssemblyIdentity, except that the name is "System.Numerics.Vectors".
    /// </summary>
    private AssemblyIdentity GetSystemNumericsVectorsSymbolicIdentity() {
      var core = this.host.CoreAssemblySymbolicIdentity;
      var name = this.host.NameTable.GetNameFor("System.Numerics.Vectors");
      var location = "";
      if (core.Location.Length > 0)
        location = Path.Combine(Path.GetDirectoryName(core.Location)??"", "System.Numerics.Vectors.dll");
      return new AssemblyIdentity(name, core.Culture, core.Version, core.PublicKeyToken, location);
    }

    /// <summary>
    /// System.AttributeTargets
    /// </summary>
    internal INamespaceTypeReference SystemAttributeTargets {
      get {
        if (this.systemAttributeTargets == null) {
          this.systemAttributeTargets = this.CreateReference(this.SystemRuntime, true, "System", "AttributeTargets");
        }
        return this.systemAttributeTargets;
      }
    }
    INamespaceTypeReference/*?*/ systemAttributeTargets;

    /// <summary>
    /// System.Collections.Generic.IDictionary
    /// </summary>
    internal INamespaceTypeReference SystemCollectionsGenericIDictionary {
      get {
        if (this.systemCollectionsGenericIDictionary == null) {
          this.systemCollectionsGenericIDictionary = this.CreateReference(this.SystemRuntime, 2, "System", "Collections", "Generic", "IDictionary");
        }
        return this.systemCollectionsGenericIDictionary;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericIDictionary;

    /// <summary>
    /// System.Collections.Generic.KeyValuePair
    /// </summary>
    internal INamespaceTypeReference SystemCollectionsGenericKeyValuePair {
      get {
        if (this.systemCollectionsGenericKeyValuePair == null) {
          this.systemCollectionsGenericKeyValuePair = 
            this.CreateReference(this.SystemRuntime, true, 2, PrimitiveTypeCode.NotPrimitive, "System", "Collections", "Generic", "KeyValuePair");
        }
        return this.systemCollectionsGenericKeyValuePair;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericKeyValuePair;

    /// <summary>
    /// System.Collections.Generic.IReadOnlyDictionary
    /// </summary>
    internal INamespaceTypeReference SystemCollectionsGenericIReadOnlyDictionary {
      get {
        if (this.systemCollectionsGenericIReadOnlyDictionary == null) {
          this.systemCollectionsGenericIReadOnlyDictionary = this.CreateReference(this.SystemRuntime, 2, "System", "Collections", "Generic", "IReadOnlyDictionary");
        }
        return this.systemCollectionsGenericIReadOnlyDictionary;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericIReadOnlyDictionary;

    /// <summary>
    /// System.Collections.Generic.ReadOnlyList
    /// </summary>
    public INamespaceTypeReference SystemCollectionsGenericIReadOnlyList {
      get {
        if (this.systemCollectionsGenericIReadOnlyList == null) {
          this.systemCollectionsGenericIReadOnlyList = this.CreateReference(this.SystemRuntime, 1, "System", "Collections", "Generic", "IReadOnlyList");
        }
        return this.systemCollectionsGenericIReadOnlyList;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsGenericIReadOnlyList;

    /// <summary>
    /// System.Collections.Specialized.INotifyCollectionChanged
    /// </summary>
    public INamespaceTypeReference SystemCollectionsSpecializedINotifyCollectionChanged {
      get {
        if (this.systemCollectionsSpecializedINotifyCollectionChanged == null) {
          this.systemCollectionsSpecializedINotifyCollectionChanged = this.CreateReference(this.SystemObjectModel, "System", "Collections", "Specialized", "INotifyCollectionChanged");
        }
        return this.systemCollectionsSpecializedINotifyCollectionChanged;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsSpecializedINotifyCollectionChanged;

    /// <summary>
    /// System.Collections.Specialized.NotifyCollectionChangedAction
    /// </summary>
    public INamespaceTypeReference SystemCollectionsSpecializedNotifyCollectionChangedAction {
      get {
        if (this.systemCollectionsSpecializedNotifyCollectionChangedAction == null) {
          this.systemCollectionsSpecializedNotifyCollectionChangedAction = this.CreateReference(this.SystemObjectModel, "System", "Collections", "Specialized", "NotifyCollectionChangedAction");
        }
        return this.systemCollectionsSpecializedNotifyCollectionChangedAction;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsSpecializedNotifyCollectionChangedAction;

    /// <summary>
    /// System.Collections.Specialized.NotifyCollectionChangedEventArgs
    /// </summary>
    public INamespaceTypeReference SystemCollectionsSpecializedNotifyCollectionChangedEventArgs {
      get {
        if (this.systemCollectionsSpecializedNotifyCollectionChangedEventArgs == null) {
          this.systemCollectionsSpecializedNotifyCollectionChangedEventArgs = this.CreateReference(this.SystemObjectModel, "System", "Collections", "Specialized", "NotifyCollectionChangedEventArgs");
        }
        return this.systemCollectionsSpecializedNotifyCollectionChangedEventArgs;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsSpecializedNotifyCollectionChangedEventArgs;

    /// <summary>
    /// System.Collections.Specialized.NotifyCollectionChangedEventHandler
    /// </summary>
    public INamespaceTypeReference SystemCollectionsSpecializedNotifyCollectionChangedEventHandler {
      get {
        if (this.systemCollectionsSpecializedNotifyCollectionChangedEventHandler == null) {
          this.systemCollectionsSpecializedNotifyCollectionChangedEventHandler = this.CreateReference(this.SystemObjectModel, "System", "Collections", "Specialized", "NotifyCollectionChangedEventHandler");
        }
        return this.systemCollectionsSpecializedNotifyCollectionChangedEventHandler;
      }
    }
    INamespaceTypeReference/*?*/ systemCollectionsSpecializedNotifyCollectionChangedEventHandler;

    /// <summary>
    /// System.ComponentModel.INotifyPropertyChanged
    /// </summary>
    public INamespaceTypeReference SystemComponentModelINotifyPropertyChanged {
      get {
        if (this.systemComponentModelINotifyPropertyChanged == null) {
          this.systemComponentModelINotifyPropertyChanged = this.CreateReference(this.SystemObjectModel, "System", "ComponentModel", "INotifyPropertyChanged");
        }
        return this.systemComponentModelINotifyPropertyChanged;
      }
    }
    INamespaceTypeReference/*?*/ systemComponentModelINotifyPropertyChanged;

    /// <summary>
    /// System.ComponentModel.PropertyChangedEventArgs
    /// </summary>
    public INamespaceTypeReference SystemComponentModelPropertyChangedEventArgs {
      get {
        if (this.systemComponentModelPropertyChangedEventArgs == null) {
          this.systemComponentModelPropertyChangedEventArgs = this.CreateReference(this.SystemObjectModel, "System", "ComponentModel", "PropertyChangedEventArgs");
        }
        return this.systemComponentModelPropertyChangedEventArgs;
      }
    }
    INamespaceTypeReference/*?*/ systemComponentModelPropertyChangedEventArgs;

    /// <summary>
    /// System.ComponentModel.PropertyChangedEventHandler
    /// </summary>
    public INamespaceTypeReference SystemComponentModelPropertyChangedEventHandler {
      get {
        if (this.systemComponentModelPropertyChangedEventHandler == null) {
          this.systemComponentModelPropertyChangedEventHandler = this.CreateReference(this.SystemObjectModel, "System", "ComponentModel", "PropertyChangedEventHandler");
        }
        return this.systemComponentModelPropertyChangedEventHandler;
      }
    }
    INamespaceTypeReference/*?*/ systemComponentModelPropertyChangedEventHandler;

    /// <summary>
    /// System.EventHandler`1
    /// </summary>
    public INamespaceTypeReference SystemEventHandler1 {
      get {
        if (this.systemEventHandler1 == null) {
          this.systemEventHandler1 = this.CreateReference(this.SystemRuntime, true, 1, PrimitiveTypeCode.NotPrimitive, "System", "EventHandler");
        }
        return this.systemEventHandler1;
      }
    }
    INamespaceTypeReference/*?*/ systemEventHandler1;

    /// <summary>
    /// System.IDisposable
    /// </summary>
    internal INamespaceTypeReference SystemIDisposable {
      get {
        if (this.systemIDisposable == null) {
          this.systemIDisposable = this.CreateReference(this.SystemRuntime, "System", "IDisposable");
        }
        return this.systemIDisposable;
      }
    }
    INamespaceTypeReference/*?*/ systemIDisposable;

    /// <summary>
    /// System.Nullable`1
    /// </summary>
    internal INamespaceTypeReference SystemNullable1 {
      get {
        if (this.systemNullable1 == null) {
          this.systemNullable1 = this.CreateReference(this.SystemRuntime, true, 1, PrimitiveTypeCode.NotPrimitive, "System", "Nullable");
        }
        return this.systemNullable1;
      }
    }
    INamespaceTypeReference/*?*/ systemNullable1;

    /// <summary>
    /// System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken
    /// </summary>
    internal INamespaceTypeReference SystemRuntimeInteropServicesWindowsRuntimeEventRegistrationToken {
      get {
        if (this.systemRuntimeInteropServicesWindowsRuntimeEventRegistrationToken == null) {
          this.systemRuntimeInteropServicesWindowsRuntimeEventRegistrationToken =
            this.CreateReference(this.SystemRuntimeInteropServicesWindowsRuntime, true, "System", "Runtime", "InteropServices", "WindowsRuntime", "EventRegistrationToken");
        }
        return this.systemRuntimeInteropServicesWindowsRuntimeEventRegistrationToken;
      }
    }
    INamespaceTypeReference/*?*/ systemRuntimeInteropServicesWindowsRuntimeEventRegistrationToken;

    /// <summary>
    /// System.TimeSpan
    /// </summary>
    internal INamespaceTypeReference SystemTimeSpan {
      get {
        if (this.systemTimeSpan == null) {
          this.systemTimeSpan = this.CreateReference(this.SystemRuntime, true, "System", "TimeSpan");
        }
        return this.systemTimeSpan;
      }
    }
    INamespaceTypeReference/*?*/ systemTimeSpan;

    /// <summary>
    /// System.Uri
    /// </summary>
    internal INamespaceTypeReference SystemUri {
      get {
        if (this.systemUri == null) {
          this.systemUri = this.CreateReference(this.SystemRuntime, "System", "Uri");
        }
        return this.systemUri;
      }
    }
    INamespaceTypeReference/*?*/ systemUri;

    /// <summary>
    /// System.Windows.Input.ICommand
    /// </summary>
    internal INamespaceTypeReference SystemWindowsInputICommand {
      get {
        if (this.systemWindowsInputICommand == null) {
          this.systemWindowsInputICommand = this.CreateReference(this.SystemObjectModel, "System", "Windows", "Input", "ICommand");
        }
        return this.systemWindowsInputICommand;
      }
    }
    INamespaceTypeReference/*?*/ systemWindowsInputICommand;

    /// <summary>
    /// Windows.UI.Color
    /// </summary>
    public INamespaceTypeReference WindowsUIColor {
      get {
        if (this.windowsFoundationColor == null) {
          this.windowsFoundationColor = this.CreateReference(this.SystemRuntimeWindowsRuntime, true, "Windows", "UI", "Color");
        }
        return this.windowsFoundationColor;
      }
    }
    INamespaceTypeReference/*?*/ windowsFoundationColor;

    /// <summary>
    /// Windows.Foundation.Point
    /// </summary>
    public INamespaceTypeReference WindowsFoundationPoint {
      get {
        if (this.windowsFoundationPoint == null) {
          this.windowsFoundationPoint = this.CreateReference(this.SystemRuntimeWindowsRuntime, true, "Windows", "Foundation", "Point");
        }
        return this.windowsFoundationPoint;
      }
    }
    INamespaceTypeReference/*?*/ windowsFoundationPoint;

    /// <summary>
    /// Windows.Foundation.Rect
    /// </summary>
    public INamespaceTypeReference WindowsFoundationRect {
      get {
        if (this.windowsFoundationRect == null) {
          this.windowsFoundationRect = this.CreateReference(this.SystemRuntimeWindowsRuntime, true, "Windows", "Foundation", "Rect");
        }
        return this.windowsFoundationRect;
      }
    }
    INamespaceTypeReference/*?*/ windowsFoundationRect;

    /// <summary>
    /// Windows.Foundation.Size
    /// </summary>
    public INamespaceTypeReference WindowsFoundationSize {
      get {
        if (this.windowsFoundationSize == null) {
          this.windowsFoundationSize = this.CreateReference(this.SystemRuntimeWindowsRuntime, true, "Windows", "Foundation", "Size");
        }
        return this.windowsFoundationSize;
      }
    }
    INamespaceTypeReference/*?*/ windowsFoundationSize;

    /// <summary>
    /// Windows.UI.Xaml.CornerRadius
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlCornerRadius {
      get {
        if (this.windowsUIXamlCornerRadius == null) {
          this.windowsUIXamlCornerRadius = this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "CornerRadius");
        }
        return this.windowsUIXamlCornerRadius;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlCornerRadius;

    /// <summary>
    /// Windows.UI.Xaml.Duration
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlDuration {
      get {
        if (this.windowsUIXamlDuration == null) {
          this.windowsUIXamlDuration = this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Duration");
        }
        return this.windowsUIXamlDuration;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlDuration;

    /// <summary>
    /// Windows.UI.Xaml.DurationType
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlDurationType {
      get {
        if (this.windowsUIXamlDurationType == null) {
          this.windowsUIXamlDurationType = this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "DurationType");
        }
        return this.windowsUIXamlDurationType;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlDurationType;

    /// <summary>
    /// Windows.UI.Xaml.GridLength
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlGridLength {
      get {
        if (this.windowsUIXamlGridLength == null) {
          this.windowsUIXamlGridLength = this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "GridLength");
        }
        return this.windowsUIXamlGridLength;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlGridLength;

    /// <summary>
    /// Windows.UI.Xaml.GridUnitType
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlGridUnitType {
      get {
        if (this.windowsUIXamlGridUnitType == null) {
          this.windowsUIXamlGridUnitType = this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "GridUnitType");
        }
        return this.windowsUIXamlGridUnitType;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlGridUnitType;

    /// <summary>
    /// Windows.UI.Xaml.Thickness
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlThickness {
      get {
        if (this.windowsUIXamlThickness == null) {
          this.windowsUIXamlThickness = this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Thickness");
        }
        return this.windowsUIXamlThickness;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlThickness;

    /// <summary>
    /// Windows.UI.Xaml.Controls.Primitives.GeneratorPosition
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlControlsPrimitivesGeneratorPosition {
      get {
        if (this.windowsUIXamlControlsPrimitivesGeneratorPosition == null) {
          this.windowsUIXamlControlsPrimitivesGeneratorPosition = 
            this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Controls", "Primitives", "GeneratorPosition");
        }
        return this.windowsUIXamlControlsPrimitivesGeneratorPosition;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlControlsPrimitivesGeneratorPosition;

    /// <summary>
    /// Windows.UI.Xaml.Media.Matrix
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlMediaMatrix {
      get {
        if (this.windowsUIXamlMediaMatrix == null) {
          this.windowsUIXamlMediaMatrix = this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Media", "Matrix");
        }
        return this.windowsUIXamlMediaMatrix;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlMediaMatrix;

    /// <summary>
    /// Windows.UI.Xaml.Media.Animation.KeyTime
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlMediaAnimationKeyTime {
      get {
        if (this.windowsUIXamlMediaAnimationKeyTime == null) {
          this.windowsUIXamlMediaAnimationKeyTime = 
            this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Media", "Animation", "KeyTime");
        }
        return this.windowsUIXamlMediaAnimationKeyTime;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlMediaAnimationKeyTime;

    /// <summary>
    /// Windows.UI.Xaml.Media.Animation.RepeatBehavior
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlMediaAnimationRepeatBehavior {
      get {
        if (this.windowsUIXamlMediaAnimationRepeatBehavior == null) {
          this.windowsUIXamlMediaAnimationRepeatBehavior = 
            this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Media", "Animation", "RepeatBehavior");
        }
        return this.windowsUIXamlMediaAnimationRepeatBehavior;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlMediaAnimationRepeatBehavior;

    /// <summary>
    /// Windows.UI.Xaml.Media.Animation.RepeatBehaviorType
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlMediaAnimationRepeatBehaviorType {
      get {
        if (this.windowsUIXamlMediaAnimationRepeatBehaviorType == null) {
          this.windowsUIXamlMediaAnimationRepeatBehaviorType = 
            this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Media", "Animation", "RepeatBehaviorType");
        }
        return this.windowsUIXamlMediaAnimationRepeatBehaviorType;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlMediaAnimationRepeatBehaviorType;

    /// <summary>
    /// Windows.UI.Xaml.Media.Media3D.Matrix3D
    /// </summary>
    public INamespaceTypeReference WindowsUIXamlMediaMedia3DMatrix3D {
      get {
        if (this.windowsUIXamlMediaMedia3DMatrix3D == null) {
          this.windowsUIXamlMediaMedia3DMatrix3D = 
            this.CreateReference(this.SystemRuntimeWindowsRuntimeUIXaml, true, "Windows", "UI", "Xaml", "Media", "Media3D", "Matrix3D");
        }
        return this.windowsUIXamlMediaMedia3DMatrix3D;
      }
    }
    INamespaceTypeReference/*?*/ windowsUIXamlMediaMedia3DMatrix3D;

    /// <summary>
    /// System.Numerics.Vector2
    /// </summary>
    public INamespaceTypeReference SystemNumericsVector2
    {
      get {
        if (this.systemNumericsVector2 == null) {
          this.systemNumericsVector2 =
            this.CreateReference(this.SystemNumericsVectors, true, "System", "Numerics", "Vector2");
        }
        return this.systemNumericsVector2;
      }
    }
    INamespaceTypeReference/*?*/ systemNumericsVector2;

    /// <summary>
    /// System.Numerics.Vector3
    /// </summary>
    public INamespaceTypeReference SystemNumericsVector3
    {
      get {
        if (this.systemNumericsVector3 == null) {
          this.systemNumericsVector3 =
            this.CreateReference(this.SystemNumericsVectors, true, "System", "Numerics", "Vector3");
        }
        return this.systemNumericsVector3;
      }
    }
    INamespaceTypeReference/*?*/ systemNumericsVector3;

    /// <summary>
    /// System.Numerics.Vector4
    /// </summary>
    public INamespaceTypeReference SystemNumericsVector4
    {
      get {
        if (this.systemNumericsVector4 == null) {
          this.systemNumericsVector4 =
            this.CreateReference(this.SystemNumericsVectors, true, "System", "Numerics", "Vector4");
        }
        return this.systemNumericsVector4;
      }
    }
    INamespaceTypeReference/*?*/ systemNumericsVector4;

    /// <summary>
    /// System.Numerics.Matrix3x2
    /// </summary>
    public INamespaceTypeReference SystemNumericsMatrix3x2
    {
      get {
        if (this.systemNumericsMatrix3x2 == null) {
          this.systemNumericsMatrix3x2 =
            this.CreateReference(this.SystemNumericsVectors, true, "System", "Numerics", "Matrix3x2");
        }
        return this.systemNumericsMatrix3x2;
      }
    }
    INamespaceTypeReference/*?*/ systemNumericsMatrix3x2;

    /// <summary>
    /// System.Numerics.Matrix4x4
    /// </summary>
    public INamespaceTypeReference SystemNumericsMatrix4x4
    {
      get {
        if (this.systemNumericsMatrix4x4 == null) {
          this.systemNumericsMatrix4x4 =
            this.CreateReference(this.SystemNumericsVectors, true, "System", "Numerics", "Matrix4x4");
        }
        return this.systemNumericsMatrix4x4;
      }
    }
    INamespaceTypeReference/*?*/ systemNumericsMatrix4x4;

    /// <summary>
    /// System.Numerics.Plane
    /// </summary>
    public INamespaceTypeReference SystemNumericsPlane
    {
      get {
        if (this.systemNumericsPlane == null) {
          this.systemNumericsPlane =
            this.CreateReference(this.SystemNumericsVectors, true, "System", "Numerics", "Plane");
        }
        return this.systemNumericsPlane;
      }
    }
    INamespaceTypeReference/*?*/ systemNumericsPlane;

    /// <summary>
    /// System.Numerics.Quaternion
    /// </summary>
    public INamespaceTypeReference SystemNumericsQuaternion
    {
      get {
        if (this.systemNumericsQuaternion == null) {
          this.systemNumericsQuaternion =
            this.CreateReference(this.SystemNumericsVectors, true, "System", "Numerics", "Quaternion");
        }
        return this.systemNumericsQuaternion;
      }
    }
    INamespaceTypeReference/*?*/ systemNumericsQuaternion;
  }

  internal sealed partial class PEFileToObjectModel {

    internal IEnumerable<ICustomAttribute> GetAttributesForSameParentAs(uint tokenValue) {
      //We get here after being called from this.GetCustomAttributeInfo via the host, hence this.currentOwningObject
      //is the owning object of the custom attribute identified by tokenValue.
      var index = tokenValue & TokenTypeIds.RIDMask;
      var table = this.PEFileReader.CustomAttributeTable;
      var parent = table[index].Parent;
      var i = index;
      var len = table.NumberOfRows;
      while (i > 1 && table[i-1].Parent == parent) i--;
      while (i < len) {
        if (i == index) { i++; continue; }
        if (table[i].Parent != parent) yield break;
        yield return this.GetCustomAttributeAtRow(this.currentOwningObject, parent, i);
        i++;
      }
    }

    private IAssemblyReference mscorlibReference;
    internal IAssemblyReference GetMscorlibReference() {
      if (mscorlibReference == null) {
        lock (GlobalLock.LockingObject) {
          if (mscorlibReference == null) {
            for (int i = 0; i < this.AssemblyReferenceArray.Length; ++i) {
              IAssemblyReference reference = this.AssemblyReferenceArray[i];
              if (reference != null && reference.Name.UniqueKey == this.ModuleReader.Mscorlib.UniqueKey) {
                mscorlibReference = this.AssemblyReferenceArray[i];
                break;
              }
            }
    
            if (mscorlibReference == null) {
              mscorlibReference = Dummy.AssemblyReference;
            }
          }
        }
      }
      
      return mscorlibReference;
    }
  }


}
