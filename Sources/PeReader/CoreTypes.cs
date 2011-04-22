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
using Microsoft.Cci.MetadataReader.PEFile;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.MetadataReader {
  using Microsoft.Cci.MetadataReader.ObjectModelImplementation;
  using Microsoft.Cci.MetadataReader.PEFileFlags;

  /// <summary>
  /// These types can all be implicitly referenced in IL and metadata and hence need special treatment.
  /// </summary>
  internal sealed class CoreTypes {
    internal readonly IMetadataReaderNamedTypeReference SystemVoid;
    internal readonly IMetadataReaderNamedTypeReference SystemBoolean;
    internal readonly IMetadataReaderNamedTypeReference SystemChar;
    internal readonly IMetadataReaderNamedTypeReference SystemByte;
    internal readonly IMetadataReaderNamedTypeReference SystemSByte;
    internal readonly IMetadataReaderNamedTypeReference SystemInt16;
    internal readonly IMetadataReaderNamedTypeReference SystemUInt16;
    internal readonly IMetadataReaderNamedTypeReference SystemInt32;
    internal readonly IMetadataReaderNamedTypeReference SystemUInt32;
    internal readonly IMetadataReaderNamedTypeReference SystemInt64;
    internal readonly IMetadataReaderNamedTypeReference SystemUInt64;
    internal readonly IMetadataReaderNamedTypeReference SystemString;
    internal readonly IMetadataReaderNamedTypeReference SystemIntPtr;
    internal readonly IMetadataReaderNamedTypeReference SystemUIntPtr;
    internal readonly IMetadataReaderNamedTypeReference SystemObject;
    internal readonly IMetadataReaderNamedTypeReference SystemSingle;
    internal readonly IMetadataReaderNamedTypeReference SystemDouble;
    internal readonly IMetadataReaderNamedTypeReference SystemDecimal;
    internal readonly IMetadataReaderNamedTypeReference SystemTypedReference;
    internal readonly IMetadataReaderNamedTypeReference SystemEnum;
    internal readonly IMetadataReaderNamedTypeReference SystemValueType;
    internal readonly IMetadataReaderNamedTypeReference SystemMulticastDelegate;
    internal readonly IMetadataReaderNamedTypeReference SystemType;
    internal readonly IMetadataReaderNamedTypeReference SystemArray;
    internal readonly IMetadataReaderNamedTypeReference SystemParamArrayAttribute;

    //  Caller should lock peFileToObjectModel
    internal CoreTypes(PEFileToObjectModel peFileToObjectModel) {
      INameTable nameTable = peFileToObjectModel.NameTable;
      PEFileReader peFileReader = peFileToObjectModel.PEFileReader;
      PeReader peReader = peFileToObjectModel.ModuleReader;
      Module module = peFileToObjectModel.Module;
      AssemblyIdentity/*?*/ assemblyIdentity = module.ModuleIdentity as AssemblyIdentity;

      int systemName = nameTable.System.UniqueKey;
      int voidName = nameTable.Void.UniqueKey;
      int booleanName = nameTable.Boolean.UniqueKey;
      int charName = nameTable.Char.UniqueKey;
      int byteName = nameTable.Byte.UniqueKey;
      int sByteName = nameTable.SByte.UniqueKey;
      int int16Name = nameTable.Int16.UniqueKey;
      int uint16Name = nameTable.UInt16.UniqueKey;
      int int32Name = nameTable.Int32.UniqueKey;
      int uint32Name = nameTable.UInt32.UniqueKey;
      int int64Name = nameTable.Int64.UniqueKey;
      int uint64Name = nameTable.UInt64.UniqueKey;
      int stringName = nameTable.String.UniqueKey;
      int intPtrName = nameTable.IntPtr.UniqueKey;
      int uintPtrName = nameTable.UIntPtr.UniqueKey;
      int objectName = nameTable.Object.UniqueKey;
      int singleName = nameTable.Single.UniqueKey;
      int doubleName = nameTable.Double.UniqueKey;
      int decimalName = nameTable.Decimal.UniqueKey;
      int typedReference = nameTable.TypedReference.UniqueKey;
      int enumName = nameTable.Enum.UniqueKey;
      int valueTypeName = nameTable.ValueType.UniqueKey;
      int multicastDelegateName = nameTable.MulticastDelegate.UniqueKey;
      int typeName = nameTable.Type.UniqueKey;
      int arrayName = nameTable.Array.UniqueKey;
      int paramArrayAttributeName = peReader.ParamArrayAttribute.UniqueKey;
      if (assemblyIdentity != null && assemblyIdentity.Equals(peReader.metadataReaderHost.CoreAssemblySymbolicIdentity)) {
        peReader.RegisterCoreAssembly(module as Assembly);
        uint numberOfTypeDefs = peFileReader.TypeDefTable.NumberOfRows;
        for (uint i = 1; i <= numberOfTypeDefs; ++i) {
          TypeDefRow typeDefRow = peFileReader.TypeDefTable[i];
          if (!typeDefRow.IsNested) {
            int namespaceName = peFileToObjectModel.GetNameFromOffset(typeDefRow.Namespace).UniqueKey;
            if (namespaceName == systemName) {
              int typeDefName = peFileToObjectModel.GetNameFromOffset(typeDefRow.Name).UniqueKey;
              if (typeDefName == voidName)
                this.SystemVoid = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Void);
              else if (typeDefName == booleanName)
                this.SystemBoolean = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Boolean);
              else if (typeDefName == charName)
                this.SystemChar = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Char);
              else if (typeDefName == byteName)
                this.SystemByte = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Byte);
              else if (typeDefName == sByteName)
                this.SystemSByte = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.SByte);
              else if (typeDefName == int16Name)
                this.SystemInt16 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Int16);
              else if (typeDefName == uint16Name)
                this.SystemUInt16 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.UInt16);
              else if (typeDefName == int32Name)
                this.SystemInt32 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Int32);
              else if (typeDefName == uint32Name)
                this.SystemUInt32 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.UInt32);
              else if (typeDefName == int64Name)
                this.SystemInt64 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Int64);
              else if (typeDefName == uint64Name)
                this.SystemUInt64 = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.UInt64);
              else if (typeDefName == stringName)
                this.SystemString = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.String);
              else if (typeDefName == intPtrName)
                this.SystemIntPtr = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.IntPtr);
              else if (typeDefName == uintPtrName)
                this.SystemUIntPtr = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.UIntPtr);
              else if (typeDefName == objectName)
                this.SystemObject = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Object);
              else if (typeDefName == singleName)
                this.SystemSingle = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Single);
              else if (typeDefName == doubleName)
                this.SystemDouble = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.Double);
              else if (typeDefName == decimalName)
                this.SystemDecimal = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == typedReference)
                this.SystemTypedReference = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.TypedReference);
              else if (typeDefName == enumName)
                this.SystemEnum = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == valueTypeName)
                this.SystemValueType = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == multicastDelegateName)
                this.SystemMulticastDelegate = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == typeName)
                this.SystemType = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == arrayName)
                this.SystemArray = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
              else if (typeDefName == paramArrayAttributeName)
                this.SystemParamArrayAttribute = peFileToObjectModel.GetPredefinedTypeDefinitionAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
            }
          }
        }
      } else {
        uint numberOfTypeRefs = peFileReader.TypeRefTable.NumberOfRows;
        AssemblyReference/*?*/ coreAssemblyRef = peFileToObjectModel.FindAssemblyReference(peReader.metadataReaderHost.CoreAssemblySymbolicIdentity);
        if (coreAssemblyRef == null) {
          //  Error...
          coreAssemblyRef = new AssemblyReference(peFileToObjectModel, 0, peReader.metadataReaderHost.CoreAssemblySymbolicIdentity, AssemblyFlags.Retargetable);
        }
        uint coreAssemblyRefToken = coreAssemblyRef.TokenValue;
        for (uint i = 1; i <= numberOfTypeRefs; ++i) {
          TypeRefRow typeRefRow = peFileReader.TypeRefTable[i];
          if (typeRefRow.ResolutionScope != coreAssemblyRefToken)
            continue;
          int namespaceName = peFileToObjectModel.GetNameFromOffset(typeRefRow.Namespace).UniqueKey;
          if (namespaceName == systemName) {
            int typeDefName = peFileToObjectModel.GetNameFromOffset(typeRefRow.Name).UniqueKey;
            if (typeDefName == voidName)
              this.SystemVoid = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Void);
            else if (typeDefName == booleanName)
              this.SystemBoolean = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Boolean);
            else if (typeDefName == charName)
              this.SystemChar = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Char);
            else if (typeDefName == byteName)
              this.SystemByte = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Byte);
            else if (typeDefName == sByteName)
              this.SystemSByte = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.SByte);
            else if (typeDefName == int16Name)
              this.SystemInt16 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Int16);
            else if (typeDefName == uint16Name)
              this.SystemUInt16 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.UInt16);
            else if (typeDefName == int32Name)
              this.SystemInt32 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Int32);
            else if (typeDefName == uint32Name)
              this.SystemUInt32 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.UInt32);
            else if (typeDefName == int64Name)
              this.SystemInt64 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Int64);
            else if (typeDefName == uint64Name)
              this.SystemUInt64 = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.UInt64);
            else if (typeDefName == stringName)
              this.SystemString = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.String);
            else if (typeDefName == intPtrName)
              this.SystemIntPtr = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.IntPtr);
            else if (typeDefName == uintPtrName)
              this.SystemUIntPtr = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.UIntPtr);
            else if (typeDefName == objectName)
              this.SystemObject = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Object);
            else if (typeDefName == singleName)
              this.SystemSingle = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Single);
            else if (typeDefName == doubleName)
              this.SystemDouble = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.Double);
            else if (typeDefName == decimalName)
              this.SystemDecimal = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == typedReference)
              this.SystemTypedReference = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.TypedReference);
            else if (typeDefName == enumName)
              this.SystemEnum = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == valueTypeName)
              this.SystemValueType = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == multicastDelegateName)
              this.SystemMulticastDelegate = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == typeName)
              this.SystemType = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == arrayName)
              this.SystemArray = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
            else if (typeDefName == paramArrayAttributeName)
              this.SystemParamArrayAttribute = peFileToObjectModel.GetPredefinedTypeRefReferenceAtRowWorker(i, MetadataReaderSignatureTypeCode.NotModulePrimitive);
          }
        }
        NamespaceReference systemNSR = peFileToObjectModel.GetNamespaceReferenceForString(coreAssemblyRef, nameTable.System);
        if (this.SystemVoid == null)
          this.SystemVoid = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Void, MetadataReaderSignatureTypeCode.Void);
        if (this.SystemBoolean == null)
          this.SystemBoolean = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Boolean, MetadataReaderSignatureTypeCode.Boolean);
        if (this.SystemChar == null)
          this.SystemChar = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Char, MetadataReaderSignatureTypeCode.Char);
        if (this.SystemByte == null)
          this.SystemByte = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Byte, MetadataReaderSignatureTypeCode.Byte);
        if (this.SystemSByte == null)
          this.SystemSByte = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.SByte, MetadataReaderSignatureTypeCode.SByte);
        if (this.SystemInt16 == null)
          this.SystemInt16 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Int16, MetadataReaderSignatureTypeCode.Int16);
        if (this.SystemUInt16 == null)
          this.SystemUInt16 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UInt16, MetadataReaderSignatureTypeCode.UInt16);
        if (this.SystemInt32 == null)
          this.SystemInt32 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Int32, MetadataReaderSignatureTypeCode.Int32);
        if (this.SystemUInt32 == null)
          this.SystemUInt32 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UInt32, MetadataReaderSignatureTypeCode.UInt32);
        if (this.SystemInt64 == null)
          this.SystemInt64 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Int64, MetadataReaderSignatureTypeCode.Int64);
        if (this.SystemUInt64 == null)
          this.SystemUInt64 = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UInt64, MetadataReaderSignatureTypeCode.UInt64);
        if (this.SystemString == null)
          this.SystemString = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.String, MetadataReaderSignatureTypeCode.String);
        if (this.SystemIntPtr == null)
          this.SystemIntPtr = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.IntPtr, MetadataReaderSignatureTypeCode.IntPtr);
        if (this.SystemUIntPtr == null)
          this.SystemUIntPtr = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.UIntPtr, MetadataReaderSignatureTypeCode.UIntPtr);
        if (this.SystemObject == null)
          this.SystemObject = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Object, MetadataReaderSignatureTypeCode.Object);
        if (this.SystemSingle == null)
          this.SystemSingle = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Single, MetadataReaderSignatureTypeCode.Single);
        if (this.SystemDouble == null)
          this.SystemDouble = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Double, MetadataReaderSignatureTypeCode.Double);
        if (this.SystemDecimal == null)
          this.SystemDecimal = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Decimal, MetadataReaderSignatureTypeCode.NotModulePrimitive);
        if (this.SystemTypedReference == null)
          this.SystemTypedReference = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.TypedReference, MetadataReaderSignatureTypeCode.NotModulePrimitive);
        if (this.SystemEnum == null)
          this.SystemEnum = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Enum, MetadataReaderSignatureTypeCode.NotModulePrimitive);
        if (this.SystemValueType == null)
          this.SystemValueType = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.ValueType, MetadataReaderSignatureTypeCode.NotModulePrimitive);
        if (this.SystemMulticastDelegate == null)
          this.SystemMulticastDelegate = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.MulticastDelegate, MetadataReaderSignatureTypeCode.NotModulePrimitive);
        if (this.SystemType == null)
          this.SystemType = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Type, MetadataReaderSignatureTypeCode.NotModulePrimitive);
        if (this.SystemArray == null)
          this.SystemArray = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, nameTable.Array, MetadataReaderSignatureTypeCode.NotModulePrimitive);
        if (this.SystemParamArrayAttribute == null)
          this.SystemParamArrayAttribute = peFileToObjectModel.typeCache.CreateCoreTypeReference(coreAssemblyRef, systemNSR, peReader.ParamArrayAttribute, MetadataReaderSignatureTypeCode.NotModulePrimitive);
      }
    }
  }

}