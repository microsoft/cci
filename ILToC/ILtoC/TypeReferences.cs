// ==++==
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
using System.Diagnostics.Contracts;
using Microsoft.Cci;
using Microsoft.Cci.Immutable;
using System.Collections.Generic;

namespace ILtoC {
  partial class Translator {

    private void EmitTypeReference(ITypeReference typeReference, bool storageLocation = false) {
      Contract.Requires(typeReference != null);

      switch (typeReference.TypeCode) {
        case PrimitiveTypeCode.Boolean: this.sourceEmitter.EmitString("uint8_t"); break;
        case PrimitiveTypeCode.Char: this.sourceEmitter.EmitString("wchar_t"); break;
        case PrimitiveTypeCode.Float32: this.sourceEmitter.EmitString("float"); break;
        case PrimitiveTypeCode.Float64: this.sourceEmitter.EmitString("double"); break;
        case PrimitiveTypeCode.Int16: this.sourceEmitter.EmitString("int16_t"); break;
        case PrimitiveTypeCode.Int32: this.sourceEmitter.EmitString("int32_t"); break;
        case PrimitiveTypeCode.Int64: this.sourceEmitter.EmitString("int64_t"); break;
        case PrimitiveTypeCode.Int8: this.sourceEmitter.EmitString("int8_t"); break;
        case PrimitiveTypeCode.IntPtr: this.sourceEmitter.EmitString("intptr_t"); break;
        case PrimitiveTypeCode.UInt16: this.sourceEmitter.EmitString("uint16_t"); break;
        case PrimitiveTypeCode.UInt32: this.sourceEmitter.EmitString("uint32_t"); break;
        case PrimitiveTypeCode.UInt64: this.sourceEmitter.EmitString("uint64_t"); break;
        case PrimitiveTypeCode.UInt8: this.sourceEmitter.EmitString("uint8_t"); break;
        case PrimitiveTypeCode.UIntPtr: this.sourceEmitter.EmitString("uintptr_t"); break;
        case PrimitiveTypeCode.Void: this.sourceEmitter.EmitString("void"); break;
        case PrimitiveTypeCode.Pointer:
          if (storageLocation)
            this.sourceEmitter.EmitString("uintptr_t");
          else {
            var ptr = typeReference as IPointerTypeReference;
            Contract.Assume(ptr != null);
            this.EmitTypeReference(ptr.TargetType);
            this.sourceEmitter.EmitString("*");
          }
          break;
        case PrimitiveTypeCode.Reference:
          if (storageLocation)
            this.sourceEmitter.EmitString("uintptr_t");
          else {
            var mptr = typeReference as IManagedPointerTypeReference;
            Contract.Assume(mptr != null);
            this.EmitTypeReference(mptr.TargetType);
            this.sourceEmitter.EmitString("*");
          }
          break;
        case PrimitiveTypeCode.String:
          if (storageLocation)
            this.sourceEmitter.EmitString("uintptr_t");
          else {
            this.sourceEmitter.EmitString("struct ");
            this.sourceEmitter.EmitString(this.GetMangledTypeName(typeReference));
            this.sourceEmitter.EmitString("*");
          }
          break;
        default:
          Contract.Assume(typeReference.TypeCode == PrimitiveTypeCode.NotPrimitive);
          if (typeReference.ResolvedType.IsEnum) {
            this.EmitTypeReference(typeReference.ResolvedType.UnderlyingType, storageLocation);
            return;
          }
          if (typeReference.IsValueType) {
            if (typeReference.ResolvedType == this.vaListType)
              this.sourceEmitter.EmitString("va_list");
            else {
              this.EmitNonPrimitiveTypeReference(typeReference);
              if (storageLocation)
                this.sourceEmitter.EmitString("_unboxed");
            }
          } else {
            if (storageLocation)
              this.sourceEmitter.EmitString("uintptr_t");
            else {
              this.EmitNonPrimitiveTypeReference(typeReference);
              this.sourceEmitter.EmitString("*");
            }
          }
          break;
      }
    }

    private void EmitNonPrimitiveTypeReference(ITypeReference type) {
      IArrayTypeReference/*?*/ arrayType = type as IArrayTypeReference;
      if (arrayType != null) {
        this.sourceEmitter.EmitString("struct ");
        this.sourceEmitter.EmitString(this.GetMangledTypeName(arrayType));
        return; 
      }
      IFunctionPointerTypeReference/*?*/ functionPointerType = type as IFunctionPointerTypeReference;
      if (functionPointerType != null) {
        this.sourceEmitter.EmitString("uintptr_t (*)(");
        bool first = true;
        this.EmitParametersOfFunctionPointer(first, functionPointerType.Parameters, functionPointerType.Type.TypeCode);
        return; 
      }
      var genericParam = type as IGenericParameterReference;
      if (genericParam != null) {
        this.sourceEmitter.EmitString("uintptr_t");
        return;
      }
      var genericInstance = type as IGenericTypeInstanceReference;
      if (genericInstance != null) {
        this.sourceEmitter.EmitString("struct ");
        this.sourceEmitter.EmitString(this.GetMangledTypeName(genericInstance));
        return;
      }
      INestedTypeReference/*?*/ ntTypeRef = type as INestedTypeReference;
      if (ntTypeRef != null) {
        this.sourceEmitter.EmitString("struct ");
        this.sourceEmitter.EmitString(this.GetMangledTypeName(ntTypeRef));
        return;
      }
      INamespaceTypeReference/*?*/ nsTypeRef = type as INamespaceTypeReference;
      if (nsTypeRef != null) {
        this.sourceEmitter.EmitString("struct ");
        this.sourceEmitter.EmitString(this.GetMangledTypeName(nsTypeRef));
        return;
      }
      IModifiedTypeReference/*?*/ modifiedType = type as IModifiedTypeReference;
      if (modifiedType != null) {
        this.EmitTypeReference(modifiedType.UnmodifiedType);
        return;
      }
    }

    private void EmitParametersOfFunctionPointer(bool first, IEnumerable<IParameterTypeInformation> parameters, PrimitiveTypeCode typeCode) {
      Contract.Requires(parameters != null);
      foreach (var par in parameters) {
        Contract.Assume(par != null);
        var parType = par.Type.ResolvedType;
        if (first) first = false;
        else this.sourceEmitter.EmitString(", ");
        if (par.IsByReference || (parType.IsValueType && !IsScalarInC(parType)))
          this.sourceEmitter.EmitString("uintptr_t ");
        else
          this.EmitTypeReference(parType, storageLocation: true);
      }
      if (typeCode != PrimitiveTypeCode.Void) {
        if (first) first = false;
        else this.sourceEmitter.EmitString(", ");
        this.sourceEmitter.EmitString("uintptr_t");
      }
      this.sourceEmitter.EmitString(")");
    }

    private uint GetTypeDepth(ITypeReference type) {
      Contract.Requires(type != null);

      var key = type.InternedKey;
      var result = this.depthMap[key];
      if (result > 0) return result-1;
      foreach (var baseClass in type.ResolvedType.BaseClasses) {
        Contract.Assume(baseClass != null);
        result = GetTypeDepth(baseClass)+1;
        this.depthMap[key] = result+1;
      }
      return result;
    }

  }
}