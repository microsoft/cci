//-----------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.  All Rights Reserved.
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
using Microsoft.Cci.Contracts;
using Microsoft.Cci.MutableCodeModel;

//^ using Microsoft.Contracts;

namespace Microsoft.Cci.Ast {

  internal sealed class BuiltinMethodDefinition : IMethodDefinition {

    internal BuiltinMethodDefinition(IName name, ITypeDefinition resultType, params ITypeDefinition[] parameterTypes) {
      this.name = name;
      this.resultType = resultType;
      List<IParameterDefinition> parameters = new List<IParameterDefinition>(parameterTypes.Length);
      ushort i = 0;
      NameDeclaration dummyName = new NameDeclaration(Dummy.Name, SourceDummy.SourceLocation);
      foreach (ITypeDefinition parameterType in parameterTypes) {
        ParameterDeclaration pdecl = new ParameterDeclaration(null, TypeExpression.For(parameterType), dummyName, null, i++, false, false, false, false, SourceDummy.SourceLocation);
        parameters.Add(new ParameterDefinition(pdecl));
      }
      this.parameters = parameters;
    }

    IName name;
    ITypeDefinition resultType;

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return false; }
    }

    public IMethodBody Body {
      get {
        //^ assume false;
        return Dummy.MethodBody;
      }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.Standard; }
    }

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return Enumerable<IGenericMethodParameter>.Empty; }
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

    public bool IsAggressivelyInlined {
      get { return false; }
    }

    public bool IsCil {
      get { return false; }
    }

    public bool IsExternal {
      get { return false; }
    }

    public bool IsForwardReference {
      get { return false; }
    }

    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
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

    public IEnumerable<IParameterDefinition> Parameters {
      get { return this.parameters; }
    }
    readonly List<IParameterDefinition> parameters;

    public ushort ParameterCount {
      get { return (ushort)this.parameters.Count; }
    }

    public bool PreserveSignature {
      get { return false; }
    }

    public bool RequiresSecurityObject {
      get { return false; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    public bool ReturnValueIsByRef {
      get { return false; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return false; }
    }

    public bool ReturnValueIsModified {
      get { return false; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public IName ReturnValueName {
      get { return Dummy.Name; }
    }

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return Enumerable<ISecurityAttribute>.Empty; }
    }

    public override string ToString() {
      if (this.Name is Dummy)
        return MemberHelper.GetMethodSignature(this, NameFormattingOptions.ReturnType|NameFormattingOptions.Signature);
      else
        return this.Name.Value;
    }

    public ITypeReference Type {
      get { return this.resultType; }
    }

    public bool IsConstructor {
      get { return false; }
    }

    public bool IsStaticConstructor {
      get { return false; }
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return Dummy.PlatformInvokeInformation; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    public ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    //^ [Pure]
    public ushort GenericParameterCount {
      get { return 0; }
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

    public IName Name {
      get { return this.name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion

    #region IDoubleDispatcher Members

    public void Dispatch(IMetadataVisitor visitor) {
    }

    public void DispatchAsReference(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region IMethodReference Members

    uint IInternedKey.InternedKey {
      get { return 0; }
    }

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return Enumerable<IParameterTypeInformation>.Empty; }
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
  }

  /// <summary>
  /// Methods that correspond to operations that are built into the platform.
  /// These methods exist only to allow the use of overload resolution as an aid to determining how operands should be converted.
  /// </summary>
  public class BuiltinMethods {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compilation"></param>
    public BuiltinMethods(Compilation compilation) {
      this.nameTable = compilation.NameTable;
      this.platformType = compilation.PlatformType;
    }

    readonly INameTable nameTable;
    readonly Immutable.PlatformType platformType;

    /// <summary>
    /// bool operator op(bool x, bool y)
    /// </summary>
    public IMethodDefinition BoolOpBool {
      get {
        if (this.boolOpBool == null) {
          lock (GlobalLock.LockingObject) {
            if (this.boolOpBool == null)
              this.boolOpBool = new BuiltinMethodDefinition(this.nameTable.BoolOpBool, this.platformType.SystemBoolean.ResolvedType, this.platformType.SystemBoolean.ResolvedType, this.platformType.SystemBoolean.ResolvedType);
          }
        }
        return this.boolOpBool;
      }
    }
    IMethodDefinition/*?*/ boolOpBool;

    /// <summary>
    /// short operator op(short x, short y)
    /// </summary>
    public IMethodDefinition Int16opInt16 {
      get {
        if (this.int16opInt16 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int16opInt16 == null)
              this.int16opInt16 = new BuiltinMethodDefinition(this.nameTable.Int16OpInt16, this.platformType.SystemInt16.ResolvedType, this.platformType.SystemInt16.ResolvedType, this.platformType.SystemInt16.ResolvedType);
          }
        }
        return this.int16opInt16;
      }
    }
    IMethodDefinition/*?*/ int16opInt16;

    /// <summary>
    /// int operator op(int x, int y)
    /// </summary>
    public IMethodDefinition Int32opInt32 {
      get {
        if (this.int32opInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int32opInt32 == null)
              this.int32opInt32 = new BuiltinMethodDefinition(this.nameTable.Int32OpInt32, this.platformType.SystemInt32.ResolvedType, this.platformType.SystemInt32.ResolvedType, this.platformType.SystemInt32.ResolvedType);
          }
        }
        return this.int32opInt32;
      }
    }
    IMethodDefinition/*?*/ int32opInt32;

    /// <summary>
    /// int operator op(int x, uint y)
    /// </summary>
    public IMethodDefinition Int32opUInt32 {
      get {
        if (this.int32opUInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int32opUInt32 == null)
              this.int32opUInt32 = new BuiltinMethodDefinition(this.nameTable.Int32OpUInt32, this.platformType.SystemInt32.ResolvedType, this.platformType.SystemInt32.ResolvedType, this.platformType.SystemUInt32.ResolvedType);
          }
        }
        return this.int32opUInt32;
      }
    }
    IMethodDefinition/*?*/ int32opUInt32;

    /// <summary>
    /// long operator op(long x, int y)
    /// </summary>
    public IMethodDefinition Int64opInt32 {
      get {
        if (this.int64opInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int64opInt32 == null)
              this.int64opInt32 = new BuiltinMethodDefinition(this.nameTable.Int64OpInt32, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemInt32.ResolvedType);
          }
        }
        return this.int64opInt32;
      }
    }
    IMethodDefinition/*?*/ int64opInt32;

    /// <summary>
    /// long operator op(long x, long y)
    /// </summary>
    public IMethodDefinition Int64opInt64 {
      get {
        if (this.int64opInt64 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int64opInt64 == null)
              this.int64opInt64 = new BuiltinMethodDefinition(this.nameTable.Int64OpInt64, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemInt64.ResolvedType);
          }
        }
        return this.int64opInt64;
      }
    }
    IMethodDefinition/*?*/ int64opInt64;

    /// <summary>
    /// long operator op(long x, uint y)
    /// </summary>
    public IMethodDefinition Int64opUInt32 {
      get {
        if (this.int64opUInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int64opUInt32 == null)
              this.int64opUInt32 = new BuiltinMethodDefinition(this.nameTable.Int64OpUInt32, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemUInt32.ResolvedType);
          }
        }
        return this.int64opUInt32;
      }
    }
    IMethodDefinition/*?*/ int64opUInt32;

    /// <summary>
    /// long operator op(long x, ulong y)
    /// </summary>
    public IMethodDefinition Int64opUInt64 {
      get {
        if (this.int64opUInt64 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int64opUInt64 == null)
              this.int64opUInt64 = new BuiltinMethodDefinition(this.nameTable.Int64OpUInt64, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemUInt64.ResolvedType);
          }
        }
        return this.int64opUInt64;
      }
    }
    IMethodDefinition/*?*/ int64opUInt64;

    /// <summary>
    /// sbyte operator op(sbyte x, sbyte y)
    /// </summary>
    public IMethodDefinition Int8opInt8 {
      get {
        if (this.int8opInt8 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.int8opInt8 == null)
              this.int8opInt8 = new BuiltinMethodDefinition(this.nameTable.Int8OpInt8, this.platformType.SystemInt8.ResolvedType, this.platformType.SystemInt8.ResolvedType, this.platformType.SystemInt8.ResolvedType);
          }
        }
        return this.int8opInt8;
      }
    }
    IMethodDefinition/*?*/ int8opInt8;

    /// <summary>
    /// uint operator op(uint x, int y)
    /// </summary>
    public IMethodDefinition UInt32opInt32 {
      get {
        if (this.uint32opInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.uint32opInt32 == null)
              this.uint32opInt32 = new BuiltinMethodDefinition(this.nameTable.UInt32OpInt32, this.platformType.SystemUInt32.ResolvedType, this.platformType.SystemUInt32.ResolvedType, this.platformType.SystemInt32.ResolvedType);
          }
        }
        return this.uint32opInt32;
      }
    }
    IMethodDefinition/*?*/ uint32opInt32;

    /// <summary>
    /// uint operator op(uint x, uint y)
    /// </summary>
    public IMethodDefinition UInt32opUInt32 {
      get {
        if (this.uint32opUInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.uint32opUInt32 == null)
              this.uint32opUInt32 = new BuiltinMethodDefinition(this.nameTable.UInt32OpUInt32, this.platformType.SystemUInt32.ResolvedType, this.platformType.SystemUInt32.ResolvedType, this.platformType.SystemUInt32.ResolvedType);
          }
        }
        return this.uint32opUInt32;
      }
    }
    IMethodDefinition/*?*/ uint32opUInt32;

    /// <summary>
    /// ulong operator op(ulong x, int y)
    /// </summary>
    public IMethodDefinition UInt64opInt32 {
      get {
        if (this.uint64opInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.uint64opInt32 == null)
              this.uint64opInt32 = new BuiltinMethodDefinition(this.nameTable.UInt64OpInt32, this.platformType.SystemUInt64.ResolvedType, this.platformType.SystemUInt64.ResolvedType, this.platformType.SystemInt32.ResolvedType);
          }
        }
        return this.uint64opInt32;
      }
    }
    IMethodDefinition/*?*/ uint64opInt32;

    /// <summary>
    /// ulong operator op(ulong x, int y)
    /// </summary>
    public IMethodDefinition UInt64opUInt32 {
      get {
        if (this.uint64opUInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.uint64opUInt32 == null)
              this.uint64opUInt32 = new BuiltinMethodDefinition(this.nameTable.UInt64OpInt32, this.platformType.SystemUInt64.ResolvedType, this.platformType.SystemUInt64.ResolvedType, this.platformType.SystemUInt32.ResolvedType);
          }
        }
        return this.uint64opUInt32;
      }
    }
    IMethodDefinition/*?*/ uint64opUInt32;

    /// <summary>
    /// ulong operator op(ulong x, ulong y)
    /// </summary>
    public IMethodDefinition UInt64opUInt64 {
      get {
        if (this.uint64opUInt64 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.uint64opUInt64 == null)
              this.uint64opUInt64 = new BuiltinMethodDefinition(this.nameTable.UInt64OpUInt64, this.platformType.SystemUInt64.ResolvedType, this.platformType.SystemUInt64.ResolvedType, this.platformType.SystemUInt64.ResolvedType);
          }
        }
        return this.uint64opUInt64;
      }
    }
    IMethodDefinition/*?*/ uint64opUInt64;

    /// <summary>
    /// UIntPtr operator op(UIntPtr x, UIntPtr y)
    /// </summary>
    public IMethodDefinition UIntPtrOpUIntPtr {
      get {
        if (this.uIntPtrOpUIntPtr == null) {
          lock (GlobalLock.LockingObject) {
            if (this.uIntPtrOpUIntPtr == null)
              this.uIntPtrOpUIntPtr = new BuiltinMethodDefinition(this.nameTable.UIntPtrOpUIntPtr, this.platformType.SystemUIntPtr.ResolvedType, this.platformType.SystemUIntPtr.ResolvedType, this.platformType.SystemUIntPtr.ResolvedType);
          }
        }
        return this.uIntPtrOpUIntPtr;
      }
    }
    IMethodDefinition/*?*/ uIntPtrOpUIntPtr;

    /// <summary>
    /// void* operator op(void* x, void* y)
    /// </summary>
    public IMethodDefinition VoidPtrOpVoidPtr {
      get {
        if (this.voidPtrOpVoidPtr == null) {
          lock (GlobalLock.LockingObject) {
            if (this.voidPtrOpVoidPtr == null)
              this.voidPtrOpVoidPtr = new BuiltinMethodDefinition(this.nameTable.VoidPtrOpVoidPtr, this.platformType.SystemVoidPtr.ResolvedType, this.platformType.SystemVoidPtr.ResolvedType, this.platformType.SystemVoidPtr.ResolvedType);
          }
        }
        return this.voidPtrOpVoidPtr;
      }
    }
    IMethodDefinition/*?*/ voidPtrOpVoidPtr;

    /// <summary>
    /// float operator op(float x, float y)
    /// </summary>
    public IMethodDefinition Float32opFloat32 {
      get {
        if (this.float32opFloat32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.float32opFloat32 == null)
              this.float32opFloat32 = new BuiltinMethodDefinition(this.nameTable.Float32OpFloat32, this.platformType.SystemFloat32.ResolvedType, this.platformType.SystemFloat32.ResolvedType, this.platformType.SystemFloat32.ResolvedType);
          }
        }
        return this.float32opFloat32;
      }
    }
    IMethodDefinition/*?*/ float32opFloat32;

    /// <summary>
    /// double operator op(double x, double y)
    /// </summary>
    public IMethodDefinition Float64opFloat64 {
      get {
        if (this.float64opFloat64 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.float64opFloat64 == null)
              this.float64opFloat64 = new BuiltinMethodDefinition(this.nameTable.Float64OpFloat64, this.platformType.SystemFloat64.ResolvedType, this.platformType.SystemFloat64.ResolvedType, this.platformType.SystemFloat64.ResolvedType);
          }
        }
        return this.float64opFloat64;
      }
    }
    IMethodDefinition/*?*/ float64opFloat64;

    /// <summary>
    /// decimal operator op(decimal x, decimal y)
    /// </summary>
    public IMethodDefinition DecimalOpDecimal {
      get {
        if (this.decimalOpDecimal == null) {
          lock (GlobalLock.LockingObject) {
            if (this.decimalOpDecimal == null)
              this.decimalOpDecimal = new BuiltinMethodDefinition(this.nameTable.DecimalOpDecimal, this.platformType.SystemDecimal.ResolvedType, this.platformType.SystemDecimal.ResolvedType, this.platformType.SystemDecimal.ResolvedType);
          }
        }
        return this.decimalOpDecimal;
      }
    }
    IMethodDefinition/*?*/ decimalOpDecimal;

    /// <summary>
    /// bool operator op(object x, object y)
    /// </summary>
    public IMethodDefinition ObjectOpObject {
      get {
        if (this.objectOpObject == null) {
          lock (GlobalLock.LockingObject) {
            if (this.objectOpObject == null)
              this.objectOpObject = new BuiltinMethodDefinition(this.nameTable.ObjectOpObject, this.platformType.SystemBoolean.ResolvedType, this.platformType.SystemObject.ResolvedType, this.platformType.SystemObject.ResolvedType);
          }
        }
        return this.objectOpObject;
      }
    }
    IMethodDefinition/*?*/ objectOpObject;

    /// <summary>
    /// string operator op(object x, string y)
    /// </summary>
    public IMethodDefinition ObjectOpString {
      get {
        if (this.objectOpString == null) {
          lock (GlobalLock.LockingObject) {
            if (this.objectOpString == null)
              this.objectOpString = new BuiltinMethodDefinition(this.nameTable.ObjectOpString, this.platformType.SystemString.ResolvedType, this.platformType.SystemObject.ResolvedType, this.platformType.SystemString.ResolvedType);
          }
        }
        return this.objectOpString;
      }
    }
    IMethodDefinition/*?*/ objectOpString;

    /// <summary>
    /// bool operator op(bool x)
    /// </summary>
    public IMethodDefinition OpBoolean {
      get {
        if (this.opBoolean == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opBoolean == null)
              this.opBoolean = new BuiltinMethodDefinition(this.nameTable.OpBoolean, this.platformType.SystemBoolean.ResolvedType, this.platformType.SystemBoolean.ResolvedType);
          }
        }
        return this.opBoolean;
      }
    }
    IMethodDefinition/*?*/ opBoolean;

    /// <summary>
    /// char operator op(char x)
    /// </summary>
    public IMethodDefinition OpChar {
      get {
        if (this.opChar == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opChar == null)
              this.opChar = new BuiltinMethodDefinition(this.nameTable.OpChar, this.platformType.SystemChar.ResolvedType, this.platformType.SystemChar.ResolvedType);
          }
        }
        return this.opChar;
      }
    }
    IMethodDefinition/*?*/ opChar;

    /// <summary>
    /// decimal operator op(decimal x)
    /// </summary>
    public IMethodDefinition OpDecimal {
      get {
        if (this.opDecimal == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opDecimal == null)
              this.opDecimal = new BuiltinMethodDefinition(this.nameTable.OpDecimal, this.platformType.SystemDecimal.ResolvedType, this.platformType.SystemDecimal.ResolvedType);
          }
        }
        return this.opDecimal;
      }
    }
    IMethodDefinition/*?*/ opDecimal;

    /// <summary>
    /// float operator op(float x)
    /// </summary>
    public IMethodDefinition OpFloat32 {
      get {
        if (this.opFloat32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opFloat32 == null)
              this.opFloat32 = new BuiltinMethodDefinition(this.nameTable.OpFloat32, this.platformType.SystemFloat32.ResolvedType, this.platformType.SystemFloat32.ResolvedType);
          }
        }
        return this.opFloat32;
      }
    }
    IMethodDefinition/*?*/ opFloat32;

    /// <summary>
    /// double operator op(double x)
    /// </summary>
    public IMethodDefinition OpFloat64 {
      get {
        if (this.opFloat64 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opFloat64 == null)
              this.opFloat64 = new BuiltinMethodDefinition(this.nameTable.OpFloat64, this.platformType.SystemFloat64.ResolvedType, this.platformType.SystemFloat64.ResolvedType);
          }
        }
        return this.opFloat64;
      }
    }
    IMethodDefinition/*?*/ opFloat64;

    /// <summary>
    /// sbyte operator op(sbyte x)
    /// </summary>
    public IMethodDefinition OpInt8 {
      get {
        if (this.opInt8 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opInt8 == null)
              this.opInt8 = new BuiltinMethodDefinition(this.nameTable.OpInt8, this.platformType.SystemInt8.ResolvedType, this.platformType.SystemInt8.ResolvedType);
          }
        }
        return this.opInt8;
      }
    }
    IMethodDefinition/*?*/ opInt8;

    /// <summary>
    /// short operator op(short x)
    /// </summary>
    public IMethodDefinition OpInt16 {
      get {
        if (this.opInt16 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opInt16 == null)
              this.opInt16 = new BuiltinMethodDefinition(this.nameTable.OpInt16, this.platformType.SystemInt16.ResolvedType, this.platformType.SystemInt16.ResolvedType);
          }
        }
        return this.opInt16;
      }
    }
    IMethodDefinition/*?*/ opInt16;

    /// <summary>
    /// int operator op(int x)
    /// </summary>
    public IMethodDefinition OpInt32 {
      get {
        if (this.opInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opInt32 == null)
              this.opInt32 = new BuiltinMethodDefinition(this.nameTable.OpInt32, this.platformType.SystemInt32.ResolvedType, this.platformType.SystemInt32.ResolvedType);
          }
        }
        return this.opInt32;
      }
    }
    IMethodDefinition/*?*/ opInt32;

    /// <summary>
    /// long operator op(long x)
    /// </summary>
    public IMethodDefinition OpInt64 {
      get {
        if (this.opInt64 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opInt64 == null)
              this.opInt64 = new BuiltinMethodDefinition(this.nameTable.OpInt64, this.platformType.SystemInt64.ResolvedType, this.platformType.SystemInt64.ResolvedType);
          }
        }
        return this.opInt64;
      }
    }
    IMethodDefinition/*?*/ opInt64;

    /// <summary>
    /// byte operator op(byte x)
    /// </summary>
    public IMethodDefinition OpUInt8 {
      get {
        if (this.opUInt8 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opUInt8 == null)
              this.opUInt8 = new BuiltinMethodDefinition(this.nameTable.OpUInt8, this.platformType.SystemUInt8.ResolvedType, this.platformType.SystemUInt8.ResolvedType);
          }
        }
        return this.opUInt8;
      }
    }
    IMethodDefinition/*?*/ opUInt8;

    /// <summary>
    /// ushort operator op(ushort x)
    /// </summary>
    public IMethodDefinition OpUInt16 {
      get {
        if (this.opUInt16 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opUInt16 == null)
              this.opUInt16 = new BuiltinMethodDefinition(this.nameTable.OpUInt16, this.platformType.SystemUInt16.ResolvedType, this.platformType.SystemUInt16.ResolvedType);
          }
        }
        return this.opUInt16;
      }
    }
    IMethodDefinition/*?*/ opUInt16;

    /// <summary>
    /// uint operator op(uint x)
    /// </summary>
    public IMethodDefinition OpUInt32 {
      get {
        if (this.opUInt32 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opUInt32 == null)
              this.opUInt32 = new BuiltinMethodDefinition(this.nameTable.OpUInt32, this.platformType.SystemUInt32.ResolvedType, this.platformType.SystemUInt32.ResolvedType);
          }
        }
        return this.opUInt32;
      }
    }
    IMethodDefinition/*?*/ opUInt32;

    /// <summary>
    /// ulong operator op(ulong x)
    /// </summary>
    public IMethodDefinition OpUInt64 {
      get {
        if (this.opUInt64 == null) {
          lock (GlobalLock.LockingObject) {
            if (this.opUInt64 == null)
              this.opUInt64 = new BuiltinMethodDefinition(this.nameTable.OpUInt64, this.platformType.SystemUInt64.ResolvedType, this.platformType.SystemUInt64.ResolvedType);
          }
        }
        return this.opUInt64;
      }
    }
    IMethodDefinition/*?*/ opUInt64;

    /// <summary>
    /// string operator op(string x, object y)
    /// </summary>
    public IMethodDefinition StringOpObject {
      get {
        if (this.stringOpObject == null) {
          lock (GlobalLock.LockingObject) {
            if (this.stringOpObject == null)
              this.stringOpObject = new BuiltinMethodDefinition(this.nameTable.StringOpObject, this.platformType.SystemString.ResolvedType, this.platformType.SystemString.ResolvedType, this.platformType.SystemObject.ResolvedType);
          }
        }
        return this.stringOpObject;
      }
    }
    IMethodDefinition/*?*/ stringOpObject;

    /// <summary>
    /// string operator op(string x, string y)
    /// </summary>
    public IMethodDefinition StringOpString {
      get {
        if (this.stringOpString == null) {
          lock (GlobalLock.LockingObject) {
            if (this.stringOpString == null)
              this.stringOpString = new BuiltinMethodDefinition(this.nameTable.StringOpString, this.platformType.SystemString.ResolvedType, this.platformType.SystemString.ResolvedType, this.platformType.SystemString.ResolvedType);
          }
        }
        return this.stringOpString;
      }
    }
    IMethodDefinition/*?*/ stringOpString;

    /// <summary>
    /// E operator op(E x, E y)
    /// </summary>
    public IMethodDefinition GetDummyEnumOpEnum(ITypeDefinition enumType)
      //^ requires enumType.IsEnum;
    {
      if (this.enumOpEnumMethodFor == null)
        this.enumOpEnumMethodFor = new Dictionary<ITypeDefinition, IMethodDefinition>();
      IMethodDefinition/*?*/ result;
      if (this.enumOpEnumMethodFor.TryGetValue(enumType, out result)) {
        //^ assume result != null;
        return result;
      }
      //^ assume enumType.IsEnum;
      result = new BuiltinMethodDefinition(this.nameTable.EnumOpEnum, enumType, enumType, enumType);
      //TODO: thread safety
      this.enumOpEnumMethodFor.Add(enumType, result);
      return result;
    }
    Dictionary<ITypeDefinition, IMethodDefinition>/*?*/ enumOpEnumMethodFor;

    /// <summary>
    /// U operator -(E x, E y)
    /// </summary>
    internal IMethodDefinition GetDummyEnumMinusEnum(ITypeDefinition enumType)
      //^ requires enumType.IsEnum;
    {
      if (this.enumMinusEnumMethodFor == null)
        this.enumMinusEnumMethodFor = new Dictionary<ITypeDefinition, IMethodDefinition>();
      IMethodDefinition/*?*/ result;
      if (this.enumMinusEnumMethodFor.TryGetValue(enumType, out result)) {
        //^ assume result != null;
        return result;
      }
      //^ assume enumType.IsEnum;
      result = new BuiltinMethodDefinition(this.nameTable.EnumOpNum, enumType.UnderlyingType.ResolvedType, enumType, enumType);
      //TODO: thread safety
      this.enumMinusEnumMethodFor.Add(enumType, result);
      return result;
    }
    Dictionary<ITypeDefinition, IMethodDefinition>/*?*/ enumMinusEnumMethodFor;

    /// <summary>
    /// E operator op(E x, U y)
    /// </summary>
    internal IMethodDefinition GetDummyEnumOpNum(ITypeDefinition enumType)
      //^ requires enumType.IsEnum;
    {
      if (this.enumPlusNumMethodFor == null)
        this.enumPlusNumMethodFor = new Dictionary<ITypeDefinition, IMethodDefinition>();
      IMethodDefinition/*?*/ result;
      if (this.enumPlusNumMethodFor.TryGetValue(enumType, out result)) {
        //^ assume result != null;
        return result;
      }
      //^ assume enumType.IsEnum;
      result = new BuiltinMethodDefinition(this.nameTable.EnumOpNum, enumType, enumType, enumType.UnderlyingType.ResolvedType);
      //TODO: thread safety
      this.enumPlusNumMethodFor.Add(enumType, result);
      return result;
    }
    Dictionary<ITypeDefinition, IMethodDefinition>/*?*/ enumPlusNumMethodFor;


    /// <summary>
    /// E operator op(U x, E y)
    /// </summary>
    internal IMethodDefinition GetDummyNumOpEnum(ITypeDefinition enumType)
      //^ requires enumType.IsEnum;
    {
      if (this.numOpEnumMethodFor == null)
        this.numOpEnumMethodFor = new Dictionary<ITypeDefinition, IMethodDefinition>();
      IMethodDefinition/*?*/ result;
      if (this.numOpEnumMethodFor.TryGetValue(enumType, out result)) {
        //^ assume result != null;
        return result;
      }
      //^ assume enumType.IsEnum;
      result = new BuiltinMethodDefinition(this.nameTable.NumOpEnum, enumType, enumType.UnderlyingType.ResolvedType, enumType);
      this.numOpEnumMethodFor.Add(enumType, result);
      //TODO: thread safety
      return result;
    }
    Dictionary<ITypeDefinition, IMethodDefinition>/*?*/ numOpEnumMethodFor;

    /// <summary>
    /// E operator op(E x)
    /// </summary>
    internal IMethodDefinition GetDummyOpEnum(ITypeDefinition enumType)
      //^ requires enumType.IsEnum;
    {
      if (this.opEnumMethodFor == null)
        this.opEnumMethodFor = new Dictionary<ITypeDefinition, IMethodDefinition>();
      IMethodDefinition/*?*/ result;
      if (this.opEnumMethodFor.TryGetValue(enumType, out result)) {
        //^ assume result != null;
        return result;
      }
      //^ assume enumType.IsEnum;
      result = new BuiltinMethodDefinition(this.nameTable.OpEnum, enumType, enumType);
      //TODO: thread safety
      this.opEnumMethodFor.Add(enumType, result);
      return result;
    }
    Dictionary<ITypeDefinition, IMethodDefinition>/*?*/ opEnumMethodFor;

    /// <summary>
    /// D operator op(D x, D y)
    /// </summary>
    internal IMethodDefinition GetDummyDelegateOpDelegate(ITypeDefinition delegateType)
      //^ requires delegateType.IsDelegate;
    {
      if (this.delegateOpDelegateMethodFor == null)
        this.delegateOpDelegateMethodFor = new Dictionary<ITypeDefinition, IMethodDefinition>();
      IMethodDefinition/*?*/ result;
      if (this.delegateOpDelegateMethodFor.TryGetValue(delegateType, out result)) {
        //^ assume result != null;
        return result;
      }
      result = new BuiltinMethodDefinition(this.nameTable.DelegateOpDelegate, delegateType, delegateType, delegateType);
      this.delegateOpDelegateMethodFor.Add(delegateType, result);
      //TODO: thread safety
      return result;
    }
    Dictionary<ITypeDefinition, IMethodDefinition>/*?*/ delegateOpDelegateMethodFor;

    /// <summary>
    /// For an array type with element type T and rank 1, returns the following methods
    /// T Get(int)
    /// T Get(uint)
    /// T Get(long)
    /// T Get(ulong)
    /// </summary>
    public IEnumerable<IMethodDefinition> GetDummyArrayGetters(IArrayTypeReference arrayType) {
      if (this.arrayGettersFor == null)
        this.arrayGettersFor = new Dictionary<IArrayTypeReference, IEnumerable<IMethodDefinition>>();
      IEnumerable<IMethodDefinition>/*?*/ result;
      if (this.arrayGettersFor.TryGetValue(arrayType, out result)) {
        //^ assume result != null;
        return result;
      }
      List<IMethodDefinition> methods = new List<IMethodDefinition>(4);
      uint rank = arrayType.Rank;
      ITypeDefinition[] parameterTypes = new ITypeDefinition[rank];
      for (int i = 0; i < rank; i++) parameterTypes[i] = this.platformType.SystemInt32.ResolvedType;
      //^ NonNullType.AssertInitialized(parameterTypes);
      methods.Add(new BuiltinMethodDefinition(this.nameTable.Get, arrayType.ElementType.ResolvedType, parameterTypes));
      for (int i = 0; i < rank; i++) parameterTypes[i] = this.platformType.SystemUInt32.ResolvedType;
      methods.Add(new BuiltinMethodDefinition(this.nameTable.Get, arrayType.ElementType.ResolvedType, parameterTypes));
      for (int i = 0; i < rank; i++) parameterTypes[i] = this.platformType.SystemInt64.ResolvedType;
      methods.Add(new BuiltinMethodDefinition(this.nameTable.Get, arrayType.ElementType.ResolvedType, parameterTypes));
      for (int i = 0; i < rank; i++) parameterTypes[i] = this.platformType.SystemUInt64.ResolvedType;
      methods.Add(new BuiltinMethodDefinition(this.nameTable.Get, arrayType.ElementType.ResolvedType, parameterTypes));
      result = methods.AsReadOnly();
      this.arrayGettersFor.Add(arrayType, result);
      //TODO: thread safety
      return result;
    }
    Dictionary<IArrayTypeReference, IEnumerable<IMethodDefinition>>/*?*/ arrayGettersFor;

    /// <summary>
    /// Returns resultType Op(operandType).
    /// </summary>
    public IMethodDefinition GetDummyOp(ITypeDefinition resultType, ITypeDefinition operandType) {
      return new BuiltinMethodDefinition(Dummy.Name, resultType, operandType);
    }

    /// <summary>
    /// Returns resultType Op(operand1Type, operand2Type).
    /// </summary>
    public IMethodDefinition GetDummyOp(ITypeDefinition resultType, ITypeDefinition operand1Type, ITypeDefinition operand2Type) {
      return new BuiltinMethodDefinition(Dummy.Name, resultType, operand1Type, operand2Type);
    }

    /// <summary>
    /// Returns resultType Op(operandType).
    /// </summary>
    public IMethodDefinition GetDummyIndexerOp(ITypeDefinition resultType, ITypeDefinition operandType) {
      return new BuiltinMethodDefinition(Dummy.Name, resultType, operandType);
    }
  }

  /// <summary>
  /// An event is a member that enables an object or class to provide notifications. Clients can attach executable code for events by supplying event handlers.
  /// This interface models the source representation of an event.
  /// </summary>
  public class EventDefinition : TypeDefinitionMember, IEventDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaration"></param>
    public EventDefinition(EventDeclaration declaration)
      : base() {
      this.declaration = declaration;
    }

    /// <summary>
    /// A list of methods that are associated with the event.
    /// </summary>
    /// <value></value>
    public IEnumerable<MethodDefinition> Accessors {
      get {
        foreach (MethodDeclaration accessor in this.declaration.Accessors)
          yield return accessor.MethodDefinition;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public override ITypeDeclarationMember Declaration {
      get { return this.declaration; }
    }
    readonly EventDeclaration declaration;

    /// <summary>
    /// Calls the visitor.Visit(IEventDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Throws an InvalidOperation exception since valid Metadata never refers directly to an event.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      throw new InvalidOperationException();
    }

    /// <summary>
    /// The method used to add a handler to the event.
    /// </summary>
    /// <value></value>
    public IMethodDefinition Adder {
      get {
        MethodDeclaration/*?*/ adder = this.declaration.Adder;
        if (adder == null) return Dummy.Method; //TODO: get a default implementation from the declaration
        return adder.MethodDefinition;
      }
    }

    /// <summary>
    /// The method used to call the event handlers when the event occurs. May be null.
    /// </summary>
    /// <value></value>
    public MethodDefinition/*?*/ Caller {
      get {
        MethodDeclaration/*?*/ callerDeclaration = this.declaration.Caller;
        if (callerDeclaration == null) return null;
        return callerDeclaration.MethodDefinition;
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<EventDeclaration> EventDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<EventDeclaration>(this.declaration); }
    }

    /// <summary>
    /// True if the event gets special treatment from the runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return this.declaration.IsRuntimeSpecial; }
    }

    /// <summary>
    /// This event is special in some way, as specified by the name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.declaration.IsSpecialName; }
    }

    /// <summary>
    /// The method used to add a handler to the event.
    /// </summary>
    /// <value></value>
    public IMethodDefinition Remover {
      get {
        MethodDeclaration/*?*/ remover = this.declaration.Remover;
        if (remover == null) return Dummy.Method; //TODO: get a default implementation from the declaration
        return remover.MethodDefinition;
      }
    }

    //public TypeReference Type {
    //  get { return new TypeReference(this.declaration.Type); }
    //}

    #region IEventDefinition Members

    IEnumerable<IMethodReference> IEventDefinition.Accessors {
      get {
        return IteratorHelper.GetConversionEnumerable<MethodDefinition, IMethodReference>(this.Accessors);
      }
    }

    IMethodReference IEventDefinition.Adder {
      get { return this.Adder; }
    }

    IMethodReference/*?*/ IEventDefinition.Caller {
      get { return this.Caller; }
    }

    IMethodReference IEventDefinition.Remover {
      get { return this.Remover; }
    }

    ITypeReference IEventDefinition.Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class FieldDefinition : TypeDefinitionMember, IFieldDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaration"></param>
    protected internal FieldDefinition(FieldDeclaration declaration) {
      this.fieldDeclaration = declaration;
    }

    /// <summary>
    /// The number of bits that form part of the value of the field.
    /// </summary>
    /// <value></value>
    public uint BitLength {
      get { return this.FieldDeclaration.BitLength; }
    }

    /// <summary>
    /// The compile time value of the field. This value should be used directly in IL, rather than a reference to the field.
    /// If the field does not have a valid compile time value, Dummy.Constant is returned.
    /// </summary>
    public CompileTimeConstant CompileTimeValue {
      get {
        return this.FieldDeclaration.CompileTimeValue;
      }
    }

    /// <summary>
    /// The list of custom modifiers, if any, associated with the field. Evaluate this property only if IsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.FieldDeclaration.CustomModifiers; }
    }

    /// <summary>
    /// The declaration that corresponds to this definition.
    /// </summary>
    /// <value></value>
    public override ITypeDeclarationMember Declaration {
      get { return this.FieldDeclaration; }
    }

    /// <summary>
    /// Calls the visitor.Visit(IFieldDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(IFieldReference) method.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IFieldReference)this);
    }

    /// <summary>
    /// 
    /// </summary>
    public FieldDeclaration FieldDeclaration {
      get { return this.fieldDeclaration; }
    }
    readonly FieldDeclaration fieldDeclaration;

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<FieldDeclaration> FieldDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<FieldDeclaration>(this.FieldDeclaration); }
    }

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that distinguishes
    /// this.ResolvedField from all other fields obtained from the same metadata host.
    /// </summary>
    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.Declaration.CompilationPart.Compilation.HostEnvironment.InternFactory.GetFieldInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    public bool IsBitField {
      get { return this.FieldDeclaration.IsBitField; }
    }

    /// <summary>
    /// This field is a compile-time constant. The field has no runtime location and cannot be directly addressed from IL.
    /// </summary>
    /// <value></value>
    public bool IsCompileTimeConstant {
      get
        //^ ensures result == this.FieldDeclaration.IsCompileTimeConstant;
      {
        return this.FieldDeclaration.IsCompileTimeConstant;
      }
    }

    /// <summary>
    /// This field is mapped to an explicitly initialized (static) memory location.
    /// </summary>
    /// <value></value>
    public bool IsMapped {
      get {
        bool result = this.FieldDeclaration.IsMapped;
        //^ assume this.IsStatic == this.FieldDeclaration.IsStatic; //post condition of this.IsStatic;
        return result;
      }
    }

    /// <summary>
    /// This field has associated field marshalling information.
    /// </summary>
    /// <value></value>
    public bool IsMarshalledExplicitly {
      get
        //^ ensures result == this.FieldDeclaration.IsMarshalledExplicitly;
      {
        return this.FieldDeclaration.IsMarshalledExplicitly;
      }
    }

    /// <summary>
    /// This field has custom modifiers.
    /// </summary>
    public bool IsModified {
      get { return this.FieldDeclaration.IsModified; }
    }

    /// <summary>
    /// The field does not have to be serialized when its containing instance is serialized.
    /// </summary>
    /// <value></value>
    public bool IsNotSerialized {
      get { return this.FieldDeclaration.IsNotSerialized; }
    }

    /// <summary>
    /// This field can only be read. Initialization takes place in a constructor.
    /// </summary>
    /// <value></value>
    public bool IsReadOnly {
      get { return this.FieldDeclaration.IsReadOnly; }
    }

    /// <summary>
    /// True if the field gets special treatment from the runtime.
    /// </summary>
    /// <value></value>
    public virtual bool IsRuntimeSpecial {
      get { return this.FieldDeclaration.IsRuntimeSpecial; }
    }

    /// <summary>
    /// This field is special in some way, as specified by the name.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.FieldDeclaration.IsSpecialName; }
    }

    /// <summary>
    /// This field is static (shared by all instances of its declaring type).
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get
        //^ ensures result == this.FieldDeclaration.IsStatic;
      {
        return this.FieldDeclaration.IsStatic;
      }
    }

    /// <summary>
    /// Offset of the field.
    /// </summary>
    /// <value></value>
    public uint Offset {
      get { return this.FieldDeclaration.Offset; }  //  TODO: Implement this...
    }

    /// <summary>
    /// The position of the field starting from 0 within the class.
    /// </summary>
    /// <value></value>
    public int SequenceNumber {
      get { return -1; }  //  TODO: Implement this...
    }

    /// <summary>
    /// Specifies how this field is marshalled when it is accessed from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation MarshallingInformation {
      get
        //^^ requires this.IsMarshalledExplicitly;
      {
        //^ assume this.FieldDeclaration.IsMarshalledExplicitly; //follows from post condition on this.IsMarshalledExplicitly
        return this.FieldDeclaration.MarshallingInformation;
      }
    }

    /// <summary>
    /// The type of value that is stored in this field.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get {
        if (this.type == null) {
          if (this.FieldDeclaration.IsVolatile) {
            ICustomModifier volatileModifier = new Immutable.CustomModifier(true, this.FieldDeclaration.PlatformType.SystemRuntimeCompilerServicesIsVolatile);
            this.type = Immutable.ModifiedTypeReference.GetModifiedTypeReference(this.FieldDeclaration.Type.ResolvedType,
              IteratorHelper.GetSingletonEnumerable(volatileModifier), this.fieldDeclaration.Compilation.HostEnvironment.InternFactory);
          } else
            this.type = this.FieldDeclaration.Type.ResolvedType;
        }
        return this.type;
      }
    }
    ITypeReference/*?*/ type;

    /// <summary>
    /// Information of the location where this field is mapped to
    /// </summary>
    public ISectionBlock FieldMapping {
      get
        //^^ requires this.IsMapped;
      {
        return this.FieldDeclaration.FieldMapping;
      }
    }

    #region IFieldDefinition Members

    IMetadataConstant IFieldDefinition.CompileTimeValue {
      get {
        if (this.CompileTimeValue is DummyConstant) return Dummy.Constant;
        return this.CompileTimeValue;
      }
    }

    IMarshallingInformation IFieldDefinition.MarshallingInformation {
      get { return this.MarshallingInformation; }
    }

    #endregion

    #region IFieldReference Members

    /// <summary>
    /// The Field being referred to.
    /// </summary>
    /// <value></value>
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

  internal sealed class FunctionPointerParameter : IParameterDefinition {

    internal FunctionPointerParameter(IParameterTypeInformation parameterTypeInformation) {
      this.parameterTypeInformation = parameterTypeInformation;
    }

    readonly IParameterTypeInformation parameterTypeInformation;

    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.VisitReference(this);
    }


    #region IParameterDefinition Members

    public ISignature ContainingSignature {
      get { return this.parameterTypeInformation.ContainingSignature; }
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

    public IMarshallingInformation MarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    public ITypeReference ParamArrayElementType {
      get { return Dummy.TypeReference; }
    }

    #endregion

    #region IReference Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion

    #region INamedEntity Members

    public IName Name {
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
      get { return this.parameterTypeInformation.CustomModifiers; }
    }

    public bool IsByReference {
      get { return this.parameterTypeInformation.IsByReference; }
    }

    public bool IsModified {
      get { return this.parameterTypeInformation.IsModified; }
    }

    public ITypeReference Type {
      get { return this.parameterTypeInformation.Type; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class FunctionPointerMethod : IMethodDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="functionPointer"></param>
    public FunctionPointerMethod(IFunctionPointerTypeReference functionPointer) {
      this.functionPointer = functionPointer;
    }

    /// <summary>
    /// 
    /// </summary>
    public IFunctionPointerTypeReference FunctionPointer {
      get { return this.functionPointer; }
    }
    readonly IFunctionPointerTypeReference functionPointer;

    #region IMethodDefinition Members

    /// <summary>
    /// True if the call sites that references the method with this object supply extra arguments.
    /// </summary>
    /// <value></value>
    public bool AcceptsExtraArguments {
      get { return (this.functionPointer.CallingConvention & CallingConvention.ExtraArguments) != 0; }
    }

    /// <summary>
    /// A container for a list of IL instructions providing the implementation (if any) of this method.
    /// </summary>
    /// <value></value>
    public virtual IMethodBody Body {
      get {
        //^ assume false;
        return Dummy.MethodBody;
      }
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return this.functionPointer.CallingConvention; }
    }

    /// <summary>
    /// If the method is generic then this list contains the type parameters.
    /// </summary>
    /// <value></value>
    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return Enumerable<IGenericMethodParameter>.Empty; }
    }

    /// <summary>
    /// True if this method has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    /// <value></value>
    public bool HasDeclarativeSecurity {
      get { return false; }
    }

    /// <summary>
    /// True if this an instance method that explicitly declares the type and name of its first parameter (the instance).
    /// </summary>
    /// <value></value>
    public bool HasExplicitThisParameter {
      get { return false; }
    }

    /// <summary>
    /// True if the method does not provide an implementation.
    /// </summary>
    /// <value></value>
    public bool IsAbstract {
      get { return false; }
    }

    /// <summary>
    /// True if the method can only be overridden when it is also accessible.
    /// </summary>
    /// <value></value>
    public bool IsAccessCheckedOnOverride {
      get { return false; }
    }

    /// <summary>
    /// True if the the runtime is requested to inline this method.
    /// </summary>
    public bool IsAggressivelyInlined {
      get { return false; }
    }

    /// <summary>
    /// True if the method is implemented in the CLI Common Intermediate Language.
    /// </summary>
    /// <value></value>
    public bool IsCil {
      get { return false; }
    }

    /// <summary>
    /// True if the method has an external implementation (i.e. not supplied by this definition).
    /// </summary>
    /// <value></value>
    public bool IsExternal {
      get { return false; }
    }

    /// <summary>
    /// True if the method implementation is defined by another method definition (to be supplied at a later time).
    /// </summary>
    /// <value></value>
    public bool IsForwardReference {
      get { return false; }
    }

    /// <summary>
    /// True if the method has generic parameters;
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get
        //^ ensures result == false;
      {
        return false;
      }
    }

    /// <summary>
    /// True if this method is hidden if a derived type declares a method with the same name and signature.
    /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
    /// </summary>
    /// <value></value>
    public bool IsHiddenBySignature {
      get { return false; }
    }

    /// <summary>
    /// True if the method is implemented in native (platform-specific) code.
    /// </summary>
    /// <value></value>
    public bool IsNativeCode {
      get { return false; }
    }

    /// <summary>
    /// The method always gets a new slot in the virtual method table.
    /// This means the method will hide (not override) a base type method with the same name and signature.
    /// </summary>
    /// <value></value>
    public bool IsNewSlot {
      get { return false; }
    }

    /// <summary>
    /// True if the the runtime is not allowed to inline this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverInlined {
      get { return false; }
    }

    /// <summary>
    /// True if the runtime is not allowed to optimize this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverOptimized {
      get { return false; }
    }

    /// <summary>
    /// True if the method is implemented via the invocation of an underlying platform method.
    /// </summary>
    /// <value></value>
    public bool IsPlatformInvoke {
      get { return false; }
    }

    /// <summary>
    /// True if the implementation of this method is supplied by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeImplemented {
      get { return false; }
    }

    /// <summary>
    /// True if the method is an internal part of the runtime and must be called in a special way.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeInternal {
      get { return false; }
    }

    /// <summary>
    /// True if the method gets special treatment from the runtime. For example, it might be a constructor.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get { return false; }
    }

    /// <summary>
    /// True if the method may not be overridden.
    /// </summary>
    /// <value></value>
    public bool IsSealed {
      get { return false; }
    }

    /// <summary>
    /// True if the method is special in some way for tools. For example, it might be a property getter or setter.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return false; }
    }

    /// <summary>
    /// True if the method does not require an instance of its declaring type as its first argument.
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get { return false; }
    }

    /// <summary>
    /// True if only one thread at a time may execute this method.
    /// </summary>
    /// <value></value>
    public bool IsSynchronized {
      get { return false; }
    }

    /// <summary>
    /// True if the method may be overridden (or if it is an override).
    /// </summary>
    /// <value></value>
    public bool IsVirtual {
      get { return false; }
    }

    /// <summary>
    /// True if the implementation of this method is not managed by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsUnmanaged {
      get { return false; }
    }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterDefinition> Parameters {
      get {
        foreach (IParameterTypeInformation ptInfo in this.FunctionPointer.Parameters)
          yield return new FunctionPointerParameter(ptInfo);
        //if (this.AcceptsExtraArguments) {
        //  foreach (IParameterTypeInformation ptInfo in this.FunctionPointer.ExtraArgumentTypes)
        //    yield return new FunctionPointerParameter(ptInfo);
        //}
      }
    }

    /// <summary>
    /// The number of required parameters of the method.
    /// </summary>
    /// <value></value>
    public ushort ParameterCount {
      get { return (ushort)IteratorHelper.EnumerableCount(this.FunctionPointer.Parameters); }
    }

    /// <summary>
    /// True if the method signature must not be mangled during the interoperation with COM code.
    /// </summary>
    /// <value></value>
    public bool PreserveSignature {
      get { return false; }
    }

    /// <summary>
    /// True if the method calls another method containing security code. If this flag is set, the method
    /// should have System.Security.DynamicSecurityMethodAttribute present in its list of custom attributes.
    /// </summary>
    /// <value></value>
    public bool RequiresSecurityObject {
      get { return false; }
    }

    /// <summary>
    /// Custom attributes associated with the method's return value.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return Enumerable<ICustomModifier>.Empty; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return false; }
    }

    /// <summary>
    /// The return value has associated marshalling information.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsMarshalledExplicitly {
      get { return false; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return false; }
    }

    /// <summary>
    /// Specifies how the return value is marshalled when the method is called from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    /// <summary>
    /// The name of the parameter to which the return value is marshalled. Returns Dummy.Name if the name has not been specified.
    /// </summary>
    public IName ReturnValueName {
      get { return Dummy.Name; }
    }

    /// <summary>
    /// Declarative security actions for this method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return Enumerable<ISecurityAttribute>.Empty; }
    }

    /// <summary>
    /// 
    /// </summary>
    public override string ToString() {
      return this.functionPointer.ToString();
    }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    public ITypeReference Type {
      get { return this.functionPointer.Type; }
    }

    /// <summary>
    /// True if the method is a constructor.
    /// </summary>
    /// <value></value>
    public bool IsConstructor {
      get { return false; }
    }

    /// <summary>
    /// True if the method is a static constructor.
    /// </summary>
    public bool IsStaticConstructor {
      get { return false; }
    }

    /// <summary>
    /// Detailed information about the PInvoke stub. Identifies which method to call, which module has the method and the calling convention among other things.
    /// </summary>
    public IPlatformInvokeInformation PlatformInvokeData {
      get { return Dummy.PlatformInvokeInformation; }
    }

    #endregion

    #region ITypeDefinitionMember Members

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    public virtual ITypeDefinition ContainingTypeDefinition {
      get { return Dummy.Type; }
    }

    /// <summary>
    /// The number of generic parameters of the method. Zero if the referenced method is not generic.
    /// </summary>
    public ushort GenericParameterCount {
      get { return 0; }
    }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    public TypeMemberVisibility Visibility {
      get { return TypeMemberVisibility.Other; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    public ITypeDefinition Container {
      get { return Dummy.Type; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    public IName Name {
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return Enumerable<ICustomAttribute>.Empty; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return Enumerable<ILocation>.Empty; }
    }

    #endregion

    #region IDoubleDispatcher Members

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void Dispatch(IMetadataVisitor visitor) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public void DispatchAsReference(IMetadataVisitor visitor) {
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return Dummy.Type; }
    }

    #endregion

    #region ISignature Members

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get { return this.functionPointer.Parameters; }
    }

    #endregion

    #region IMethodReference Members

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that uniquely identifies
    /// this.ResolvedMethod.
    /// </summary>
    uint IInternedKey.InternedKey {
      get { return 0; }
    }

    /// <summary>
    /// The method being referred to.
    /// </summary>
    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    /// <summary>
    /// Information about this types of the extra arguments supplied at the call sites that references the method with this object.
    /// </summary>
    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return this.functionPointer.ExtraArgumentTypes; }
    }

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    public ITypeReference ContainingType {
      get { return Dummy.TypeReference; }
    }

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class GenericMethodParameter : GenericParameter, IGenericMethodParameter {

    //^ [NotDelayed]
    /// <summary>
    /// 
    /// </summary>
    /// <param name="definingMethod"></param>
    /// <param name="declaration"></param>
    public GenericMethodParameter(MethodDefinition definingMethod, GenericMethodParameterDeclaration declaration)
      : base(declaration.Name, declaration.Index, declaration.Variance, declaration.MustBeReferenceType, declaration.MustBeValueType, declaration.MustHaveDefaultConstructor, declaration.Compilation.HostEnvironment.InternFactory)
      //^ requires definingMethod.IsGeneric;
    {
      this.declaration = declaration;
      this.definingMethod = definingMethod;
      //^ base;
    }

    /// <summary>
    /// The generic method that defines this type parameter.
    /// </summary>
    /// <value></value>
    public MethodDefinition DefiningMethod {
      get
        //^ ensures result.IsGeneric;
      {
        return this.definingMethod;
      }
    }
    readonly MethodDefinition definingMethod;
    //^ invariant definingMethod.IsGeneric;

    /// <summary>
    /// Calls the visitor.Visit(IGenericMethodParameter) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(IGenericMethodParameterReference) method.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IGenericMethodParameterReference)this);
    }

    /// <summary>
    /// Returns the list of generic parameter declarations that collectively define this generic parameter definition.
    /// </summary>
    /// <returns></returns>
    protected override IEnumerable<GenericParameterDeclaration> GetDeclarations() {
      foreach (GenericMethodParameterDeclaration parameterDeclaration in this.ParameterDeclarations)
        yield return parameterDeclaration;
    }

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<GenericMethodParameterDeclaration> ParameterDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<GenericMethodParameterDeclaration>(this.declaration); }
    }
    readonly GenericMethodParameterDeclaration declaration;

    /// <summary>
    /// A collection of well known types that must be part of every target platform and that are fundamental to modeling compiled code.
    /// The types are obtained by querying the unit set of the compilation and thus can include types that are defined by the compilation itself.
    /// </summary>
    /// <value></value>
    public override IPlatformType PlatformType {
      get { return this.DefiningMethod.ContainingTypeDefinition.PlatformType; }
    }

    #region IGenericMethodParameter Members

    IMethodDefinition IGenericMethodParameter.DefiningMethod {
      get {
        IMethodDefinition result = this.DefiningMethod;
        //^ assume result == ((IGenericMethodParameter)this).DefiningMethod; //the next statement makes this true
        return result;
      }
    }

    #endregion

    #region IReference Members


    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetSingletonEnumerable<ILocation>(this.declaration.SourceLocation); }
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

  /// <summary>
  /// Represents a global field in symbol table.
  /// </summary>
  public class GlobalFieldDefinition : IGlobalFieldDefinition {

    /// <summary>
    /// Allocates a global field definition to correspond to a given global field declaration.
    /// </summary>
    /// <param name="globalFieldDeclaration">The global field declaration that corresponds to the definition being allocated.</param>
    protected internal GlobalFieldDefinition(GlobalFieldDeclaration globalFieldDeclaration) {
      this.globalFieldDeclaration = globalFieldDeclaration;
    }

    /// <summary>
    /// Adds an additional global field declaration to the list of declarations that aggregate into this definition.
    /// Does nothting if the declaration is already a member of the list.
    /// </summary>
    protected internal void AddGlobalFieldDeclaration(GlobalFieldDeclaration fieldDeclaration) {
      if (this.globalFieldDeclaration == fieldDeclaration) return;
      if (this.globalFieldDeclarations == null) {
        //^ assert this.globalFieldDeclaration != null;
        this.globalFieldDeclarations = new List<GlobalFieldDeclaration>();
        this.globalFieldDeclarations.Add(this.globalFieldDeclaration);
        this.globalFieldDeclaration = null;
      }
      if (!this.globalFieldDeclarations.Contains(fieldDeclaration))
        this.globalFieldDeclarations.Add(fieldDeclaration);
    }


    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.GlobalFieldDeclaration.Attributes; }
    }

    /// <summary>
    /// The number of least significant bits that form part of the value of the field.
    /// </summary>
    public uint BitLength {
      get { return this.GlobalFieldDeclaration.BitLength; }
    }

    /// <summary>
    /// The compile time value of the field. This value should be used directly in IL, rather than a reference to the field.
    /// If the field does not have a valid compile time value, an instance of DummyConstant is returned.
    /// </summary>
    public CompileTimeConstant CompileTimeValue {
      get { return this.GlobalFieldDeclaration.CompileTimeValue; }
    }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    public INamespaceDefinition ContainingNamespace {
      get { return this.GlobalFieldDeclaration.ContainingNamespaceDeclaration.UnitNamespace; }
    }

    /// <summary>
    /// The type reference of the containing type.
    /// </summary>
    public ITypeReference ContainingType {
      get { return this.GlobalFieldDeclaration.GlobalDefinitionsContainerType; }
    }

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    public ITypeDefinition ContainingTypeDefinition {
      get { return this.GlobalFieldDeclaration.GlobalDefinitionsContainerType; }
    }

    /// <summary>
    /// The list of custom modifiers, if any, associated with the field. Evaluate this property only if IsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.GlobalFieldDeclaration.CustomModifiers; }
    }

    /// <summary>
    /// Calls the visitor.Visit(IGlobalFieldDefinition) method.
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(IFieldReference) method.
    /// </summary>
    public void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IFieldReference)this);
    }

    /// <summary>
    /// The global field declaration that projects onto this global field definition.
    /// </summary>
    public GlobalFieldDeclaration GlobalFieldDeclaration {
      get {
        if (this.globalFieldDeclaration == null)
          this.globalFieldDeclaration = this.GetGlobalFieldDeclaration();
        return this.globalFieldDeclaration;
      }
    }
    //^ [SpecPublic]
    GlobalFieldDeclaration/*?*/ globalFieldDeclaration;

    /// <summary>
    /// Information of the location where this field is mapped to
    /// </summary>
    public ISectionBlock FieldMapping {
      get { return this.GlobalFieldDeclaration.FieldMapping; }
    }

    /// <summary>
    /// Gets the global field declaration.
    /// </summary>
    /// <returns></returns>
    protected virtual GlobalFieldDeclaration GetGlobalFieldDeclaration()
      //^ requires this.globalFieldDeclaration == null;
    {
      GlobalFieldDeclaration/*?*/ result = null;
      //^ assert this.globalFieldDeclarations != null && this.globalFieldDeclarations.Count > 1;
      foreach (GlobalFieldDeclaration gfdecl in this.globalFieldDeclarations) {
        if (result == null)
          result = gfdecl;
        else if (result.Initializer == null)
          result = gfdecl;
        else if (result.IsPublic && !gfdecl.IsPublic)
          result = gfdecl;
        break;
      }
      //^ assert result != null;
      return result;
    }

    /// <summary>
    /// Gets the global field declarations.
    /// </summary>
    /// <value>The global field declarations.</value>
    public IEnumerable<GlobalFieldDeclaration> GlobalFieldDeclarations {
      get {
        if (this.globalFieldDeclarations != null) return this.globalFieldDeclarations.AsReadOnly();
        //^ assert this.globalFieldDeclaration != null;
        return IteratorHelper.GetSingletonEnumerable<GlobalFieldDeclaration>(this.globalFieldDeclaration);
      }
    }
    List<GlobalFieldDeclaration>/*?*/ globalFieldDeclarations;
    //^ invariant globalFieldDeclaration == null <==> globalFieldDeclarations != null && globalFieldDeclarations.Count > 1;

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that distinguishes
    /// this.ResolvedField from all other fields obtained from the same metadata host.
    /// </summary>
    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.GlobalFieldDeclaration.CompilationPart.Compilation.HostEnvironment.InternFactory.GetFieldInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    /// <value></value>
    public bool IsBitField {
      get { return this.GlobalFieldDeclaration.IsBitField; }
    }

    /// <summary>
    /// This field is a compile-time constant. The field has no runtime location and cannot be directly addressed from IL.
    /// </summary>
    public bool IsCompileTimeConstant {
      get { return this.GlobalFieldDeclaration.IsCompileTimeConstant; }
    }

    /// <summary>
    /// This field is mapped to an explicitly initialized (static) memory location.
    /// </summary>
    public bool IsMapped {
      get { return this.GlobalFieldDeclaration.IsMapped; }
    }

    /// <summary>
    /// This field has associated field marshalling information.
    /// </summary>
    public bool IsMarshalledExplicitly {
      get { return this.GlobalFieldDeclaration.IsMarshalledExplicitly; }
    }

    /// <summary>
    /// This field has custom modifiers.
    /// </summary>
    public bool IsModified {
      get { return this.GlobalFieldDeclaration.IsModified; }
    }

    /// <summary>
    /// The field does not have to be serialized when its containing instance is serialized.
    /// </summary>
    public bool IsNotSerialized {
      get { return this.GlobalFieldDeclaration.IsNotSerialized; }
    }

    /// <summary>
    /// This field can only be read. Initialization takes place in a constructor.
    /// </summary>
    public bool IsReadOnly {
      get { return this.GlobalFieldDeclaration.IsReadOnly; }
    }

    /// <summary>
    /// This field has a special name reserved for the internal use of the Common Language Runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return false; }
    }

    /// <summary>
    /// This field is special in some way, as specified by the name.
    /// </summary>
    public bool IsSpecialName {
      get { return this.GlobalFieldDeclaration.IsSpecialName; }
    }

    /// <summary>
    /// This field is static (shared by all instances of its declaring type).
    /// </summary>
    public bool IsStatic {
      get { return true; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this IGlobalFieldDefinition instance.
    /// </summary>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetSingletonEnumerable<ILocation>(this.GlobalFieldDeclaration.SourceLocation); }
    }

    /// <summary>
    /// Specifies how this field is marshalled when it is accessed from unmanaged code.
    /// </summary>
    public IMarshallingInformation MarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    /// <summary>
    /// The name of the global field.
    /// </summary>
    public IName Name {
      get { return this.GlobalFieldDeclaration.Name; }
    }

    /// <summary>
    /// Offset of the field.
    /// </summary>
    public uint Offset {
      get { return 0; }
    }

    /// <summary>
    /// The position of the field starting from 0 within the class.
    /// </summary>
    public int SequenceNumber {
      get { return 0; }
    }

    /// <summary>
    /// The type of value that is stored in this field.
    /// </summary>
    public ITypeReference Type {
      get { return this.GlobalFieldDeclaration.Type.ResolvedType; }
    }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    public TypeMemberVisibility Visibility {
      get { return this.GlobalFieldDeclaration.IsPublic ? TypeMemberVisibility.Public : TypeMemberVisibility.Assembly; }
    }

    #region IFieldDefinition Members

    IMetadataConstant IFieldDefinition.CompileTimeValue {
      get { return this.CompileTimeValue; }
    }

    #endregion

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

    #region IFieldReference Members

    IFieldDefinition IFieldReference.ResolvedField {
      get { return this; }
    }

    #endregion

    #region ITypeMemberReference Members

    ITypeDefinitionMember ITypeMemberReference.ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.CompileTimeValue; }
    }

    #endregion

  }

  /// <summary>
  /// Represents a global method in symbol table.
  /// </summary>
  public class GlobalMethodDefinition : MethodDefinition, IGlobalMethodDefinition {

    /// <summary>
    /// Allocates a global method definition to correspond to a given global method declaration.
    /// </summary>
    /// <param name="globalMethodDeclaration">The global method declaration that corresponds to the definition being allocated.</param>
    protected internal GlobalMethodDefinition(GlobalMethodDeclaration globalMethodDeclaration)
      : base(globalMethodDeclaration) {
    }

    /// <summary>
    /// The namespace that contains this member.
    /// </summary>
    public INamespaceDefinition ContainingNamespace {
      get { return this.GlobalMethodDeclaration.ContainingNamespaceDeclaration.UnitNamespace; }
    }

    /// <summary>
    /// Calls the visitor.Visit(IGlobalMethodDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// The global method declaration that projects onto this global method definition.
    /// </summary>
    public GlobalMethodDeclaration GlobalMethodDeclaration {
      get {
        //^ assume this.Declaration is GlobalMethodDeclaration; //guaranteed by constructor
        return (GlobalMethodDeclaration)this.Declaration;
      }
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

  /// <summary>
  /// 
  /// </summary>
  public class MethodBody : ISourceMethodBody {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="methodDefinition"></param>
    /// <param name="block"></param>
    /// <param name="localsAreZeroed"></param>
    public MethodBody(IMethodDefinition methodDefinition, BlockStatement/*?*/ block, bool localsAreZeroed)
      //^ requires block == null <==> methodDefinition.IsAbstract;
    {
      this.block = block;
      this.localsAreZeroed = localsAreZeroed;
      this.methodDefinition = methodDefinition;
    }

    /// <summary>
    /// The collection of statements making up the body.
    /// This is produced by either language parser or through decompilation of the Instructions.
    /// </summary>
    /// <value></value>
    public BlockStatement Block {
      get {
        //^ assume this.block != null; //implied by the precondition
        return this.block;
      }
    }
    BlockStatement/*?*/ block;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    private void GenerateIL()
      //^ ensures this.privateHelperTypes != null;
    {
      this.ilWasGenerated = true;
      if (this.block == null) {
        this.localVariables = Enumerable<ILocalDefinition>.Empty;
        this.maxStack = 0;
        this.operations = Enumerable<IOperation>.Empty;
        this.operationExceptionInformation = Enumerable<IOperationExceptionInformation>.Empty;
        this.localVariables = Enumerable<ILocalDefinition>.Empty;
        this.privateHelperTypes = Enumerable<ITypeDefinition>.Empty;
        return;
      }

      MethodBodyNormalizer normalizer = new MethodBodyNormalizer(this.Block.Compilation.HostEnvironment,
        this.Block.Compilation.SourceLocationProvider);
      ISourceMethodBody normalizedBody = normalizer.GetNormalizedSourceMethodBodyFor(this.MethodDefinition, this.Block);
      this.isIteratorBody = normalizer.IsIteratorBody;

      CodeModelToILConverter converter = new CodeModelToILConverter(this.Block.Compilation.HostEnvironment,
        this.MethodDefinition,
        this.Block.Compilation.SourceLocationProvider);
      //TODO: if /optimize has not been specified, disable converter.MinimizeCodeSize
      converter.ConvertToIL(normalizedBody.Block);

      this.localScopes = converter.GetLocalScopes();
      this.localVariables = converter.GetLocalVariables();
      this.maxStack = converter.MaximumStackSizeNeeded;
      this.operations = converter.GetOperations();
      this.operationExceptionInformation = converter.GetOperationExceptionInformation();
      this.privateHelperTypes = normalizedBody.PrivateHelperTypes;
      this.size = converter.GetBodySize();
    }

    bool ilWasGenerated;

    /// <summary>
    /// Returns zero or more local (block) scopes, each defining an IL range in which an iterator local is defined.
    /// The scopes are returned by the MoveNext method of the object returned by the iterator method.
    /// The index of the scope corresponds to the index of the local. Specifically local scope i corresponds
    /// to the local stored in field &lt;localName&gt;x_i of the class used to store the local values in between
    /// calls to MoveNext.
    /// </summary>
    public IEnumerable<ILocalScope> GetIteratorScopes() {
      return LocalScopeProvider.emptyLocalScopes;
    }

    /// <summary>
    /// Returns zero or more local (block) scopes into which the CLR IL operations in the given method body is organized.
    /// </summary>
    public IEnumerable<ILocalScope> GetLocalScopes() {
      if (!this.ilWasGenerated) this.GenerateIL();
      return this.localScopes;
    }
    IEnumerable<ILocalScope>/*?*/ localScopes;

    /// <summary>
    /// Returns zero or more namespace scopes into which the namespace type containing the given method body has been nested.
    /// These scopes determine how simple names are looked up inside the method body. There is a separate scope for each dotted
    /// component in the namespace type name. For istance namespace type x.y.z will have two namespace scopes, the first is for the x and the second
    /// is for the y.
    /// </summary>
    public IEnumerable<INamespaceScope> GetNamespaceScopes() {
      var containingNamespace = this.Block.ContainingNamespaceDeclaration;
      var bodyThatProvides = containingNamespace.methodBodyThatWillProvideNonEmptyNamespaceScopes;
      if (bodyThatProvides != null && bodyThatProvides != this) yield break;
      containingNamespace.methodBodyThatWillProvideNonEmptyNamespaceScopes = this;
      var nestedContainingNamespace = containingNamespace as NestedNamespaceDeclaration;
      while (nestedContainingNamespace != null) {
        yield return nestedContainingNamespace;
        containingNamespace = nestedContainingNamespace.ContainingNamespaceDeclaration;
        nestedContainingNamespace = containingNamespace as NestedNamespaceDeclaration;
      }
      yield return containingNamespace;
    }

    /// <summary>
    /// True if the method body is an iterator.
    /// </summary>
    public bool IsIteratorBody {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.isIteratorBody;
      }
    }
    bool isIteratorBody;

    /// <summary>
    /// True if the locals are initialized by zeroeing the stack upon method entry.
    /// </summary>
    public bool LocalsAreZeroed {
      get { return this.localsAreZeroed; }
    }
    bool localsAreZeroed;

    /// <summary>
    /// The local variables of the method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocalDefinition> LocalVariables {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.localVariables;
      }
    }
    IEnumerable<ILocalDefinition>/*?*/ localVariables;

    // Do we want shallow copy of method body or do we want to reparse it?
    /// <summary>
    /// 
    /// </summary>
    /// <param name="targetCompilation"></param>
    /// <returns></returns>
    public virtual MethodBody MakeShallowCopyFor(Compilation targetCompilation) {
      return this; //TODO: implement this
    }

    /// <summary>
    /// The maximum number of elements on the evaluation stack during the execution of the method.
    /// </summary>
    public ushort MaxStack {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.maxStack;
      }
    }
    ushort maxStack;

    /// <summary>
    /// The definition of the method whose body this is.
    /// If this is the body of an event or property accessor, this will hold the corresponding adder/remover/setter or getter method.
    /// </summary>
    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
    }
    readonly IMethodDefinition methodDefinition;

    /// <summary>
    /// A list CLR IL operations that implement this method body.
    /// </summary>
    public IEnumerable<IOperation> Operations {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operations;
      }
    }
    IEnumerable<IOperation>/*?*/ operations;

    /// <summary>
    /// A list exception data within the method body IL.
    /// </summary>
    public IEnumerable<IOperationExceptionInformation> OperationExceptionInformation {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operationExceptionInformation;
      }
    }
    IEnumerable<IOperationExceptionInformation> operationExceptionInformation;

    /// <summary>
    /// Any types that are implicitly defined in order to implement the body semantics.
    /// In case of AST to instructions conversion this lists the types produced.
    /// In case of instructions to AST decompilation this should ideally be list of all types
    /// which are local to method.
    /// </summary>
    public IEnumerable<ITypeDefinition> PrivateHelperTypes {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.privateHelperTypes;
      }
    }
    IEnumerable<ITypeDefinition>/*?*/ privateHelperTypes;

    /// <summary>
    /// The size in bytes of the method body when serialized.
    /// </summary>
    public uint Size {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.size;
      }
    }
    uint size;

    #region ISourceMethodBody Members

    IBlockStatement ISourceMethodBody.Block {
      get { return this.Block; }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  public class MethodDefinition : TypeDefinitionMember, IMethodDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaration"></param>
    public MethodDefinition(MethodDeclaration declaration) {
      this.declaration = declaration;
    }

    /// <summary>
    /// True if the call sites that references the method with this object supply extra arguments.
    /// </summary>
    /// <value></value>
    public bool AcceptsExtraArguments {
      get { return this.declaration.AcceptsExtraArguments; }
    }

    /// <summary>
    /// A container for a list of IL instructions providing the implementation (if any) of this method.
    /// </summary>
    /// <value></value>
    public MethodBody Body {
      get
        //^ requires !this.IsAbstract && !this.IsExternal;
      {
        if (this.body == null)
          this.body = new MethodBody(this, this.declaration.Body, true);
        return this.body;
      }
    }
    MethodBody/*?*/ body;


    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return this.declaration.CallingConvention; }
    }

    /// <summary>
    /// The declaration that corresponds to this definition.
    /// </summary>
    /// <value></value>
    public override ITypeDeclarationMember Declaration {
      get { return this.declaration; }
    }
    //^ [SpecPublic]
    readonly MethodDeclaration declaration;

    /// <summary>
    /// Calls the visitor.Visit(IMethodDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls the visitor.Visit(IMethodReference) method.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.Visit((IMethodReference)this);
    }

    /// <summary>
    /// An empty collection of method definitions.
    /// </summary>
    public static IEnumerable<IMethodDefinition> EmptyCollection {
      get { return MethodDefinition.emptyCollection; }
    }
    static readonly IEnumerable<IMethodDefinition> emptyCollection = new List<IMethodDefinition>(0).AsReadOnly();

    /// <summary>
    /// If the method is generic then this list contains the type parameters.
    /// </summary>
    /// <value></value>
    public IEnumerable<GenericMethodParameter> GenericParameters {
      get {
        foreach (GenericMethodParameterDeclaration parameterDeclaration in this.declaration.GenericParameters)
          yield return parameterDeclaration.GenericMethodParameterDefinition;
      }
    }

    /// <summary>
    /// The number of generic parameters of the method. Zero if the referenced method is not generic.
    /// </summary>
    /// <value></value>
    public ushort GenericParameterCount
      //^^ ensures result >= 0;
      //^^ ensures !this.IsGeneric ==> result == 0;
      //^^ ensures this.IsGeneric ==> result > 0;
    {
      get {
        ushort result = (ushort)this.declaration.GenericParameterCount;
        //^ assume this.declaration.IsGeneric == this.IsGeneric;
        return result;
      }
    }

    /// <summary>
    /// True if this method has a non empty collection of SecurityAttributes or the System.Security.SuppressUnmanagedCodeSecurityAttribute.
    /// </summary>
    /// <value></value>
    public bool HasDeclarativeSecurity {
      get { return this.declaration.HasDeclarativeSecurity; }
    }

    /// <summary>
    /// True if this is an instance method that explicitly declares the type and name of its first parameter (the instance).
    /// </summary>
    public bool HasExplicitThisParameter {
      get { return this.declaration.HasExplicitThisParameter; }
    }

    /// <summary>
    /// A list of interfaces whose corresponding abstract methods are implemented by this method.
    /// </summary>
    public IEnumerable<ITypeDefinition> ImplementedInterfaces {
      get {
        foreach (TypeExpression texpr in this.declaration.ImplementedInterfaces)
          yield return texpr.ResolvedType;
      }
    }

    /// <summary>
    /// Returns a key that is computed from the information in this reference and that uniquely identifies
    /// this.ResolvedMethod.
    /// </summary>
    /// <value></value>
    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.Declaration.CompilationPart.Compilation.HostEnvironment.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    /// <summary>
    /// True if the method does not provide an implementation.
    /// </summary>
    /// <value></value>
    public bool IsAbstract {
      get { return this.declaration.IsAbstract; }
    }

    /// <summary>
    /// True if the method can only be overridden when it is also accessible. 
    /// </summary>
    public bool IsAccessCheckedOnOverride {
      get { return this.declaration.IsAccessCheckedOnOverride; }
    }

    /// <summary>
    /// True if the the runtime is requested to inline this method.
    /// </summary>
    public bool IsAggressivelyInlined {
      get { return false; }
    }

    /// <summary>
    /// True if the method is implemented in the CLI Common Intermediate Language.
    /// </summary>
    /// <value></value>
    public bool IsCil {
      get { return this.declaration.IsCil; }
    }

    /// <summary>
    /// True if the method has an external implementation (i.e. not supplied by this definition).
    /// </summary>
    /// <value></value>
    public bool IsExternal {
      get { return this.declaration.IsExternal; }
    }

    /// <summary>
    /// True if the method implementation is defined by another method definition (to be supplied at a later time).
    /// </summary>
    /// <value></value>
    public bool IsForwardReference {
      get { return this.declaration.IsForwardReference; }
    }

    /// <summary>
    /// True if the method has generic parameters;
    /// </summary>
    /// <value></value>
    public bool IsGeneric {
      get
        //^ ensures result == this.declaration.IsGeneric;
      {
        return this.declaration.IsGeneric;
      }
    }

    /// <summary>
    /// True if this method is hidden if a derived type declares a method with the same name and signature.
    /// If false, any method with the same name hides this method. This flag is ignored by the runtime and is only used by compilers.
    /// </summary>
    /// <value></value>
    public bool IsHiddenBySignature {
      get { return this.declaration.IsHiddenBySignature; }
    }

    /// <summary>
    /// True if the method is implemented in native (platform-specific) code.
    /// </summary>
    /// <value></value>
    public bool IsNativeCode {
      get { return this.declaration.IsNativeCode; }
    }

    /// <summary>
    /// The method always gets a new slot in the virtual method table.
    /// This means the method will hide (not override) a base type method with the same name and signature.
    /// </summary>
    /// <value></value>
    public bool IsNewSlot {
      get { return this.declaration.IsVirtual && !this.declaration.IsOverride; }
    }

    /// <summary>
    /// True if the the runtime is not allowed to inline this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverInlined {
      get { return this.declaration.IsNeverInlined; }
    }

    /// <summary>
    /// True if the runtime is not allowed to optimize this method.
    /// </summary>
    /// <value></value>
    public bool IsNeverOptimized {
      get { return this.declaration.IsNeverOptimized; }
    }

    /// <summary>
    /// True if the method is implemented via the invocation of an underlying platform method.
    /// </summary>
    /// <value></value>
    public bool IsPlatformInvoke {
      get { return this.declaration.IsPlatformInvoke; }
    }

    /// <summary>
    /// True if the implementation of this method is supplied by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeImplemented {
      get { return this.declaration.IsRuntimeImplemented; }
    }

    /// <summary>
    /// True if the method is an internal part of the runtime and must be called in a special way.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeInternal {
      get { return this.declaration.IsRuntimeInternal; }
    }

    /// <summary>
    /// True if the method gets special treatment from the runtime. For example, it might be a constructor.
    /// </summary>
    /// <value></value>
    public bool IsRuntimeSpecial {
      get { return this.declaration.IsRuntimeSpecial; }
    }

    /// <summary>
    /// True if the method may not be overridden.
    /// </summary>
    /// <value></value>
    public bool IsSealed {
      get { return this.declaration.IsSealed; }
    }

    /// <summary>
    /// True if the method is special in some way for tools. For example, it might be a property getter or setter.
    /// </summary>
    /// <value></value>
    public bool IsSpecialName {
      get { return this.declaration.IsSpecialName; }
    }

    /// <summary>
    /// True if the method does not require an instance of its declaring type as its first argument.
    /// </summary>
    /// <value></value>
    public bool IsStatic {
      get { return this.declaration.IsStatic; }
    }

    /// <summary>
    /// True if this method is a static method that can be called as an instance method on another class because it has an explicit this parameter.
    /// In other words, the class defining this static method is effectively extending another class, but doing so without subclassing it and
    /// without requiring client code to instantiate the subclass.
    /// </summary>
    public bool IsExtensionMethod {
      get { return this.declaration.IsExtensionMethod; }
    }

    /// <summary>
    /// True if only one thread at a time may execute this method.
    /// </summary>
    /// <value></value>
    public bool IsSynchronized {
      get { return this.declaration.IsSynchronized; }
    }

    /// <summary>
    /// True if the method may be overridden (or if it is an override).
    /// </summary>
    /// <value></value>
    public bool IsVirtual {
      get {
        bool result = this.declaration.IsVirtual || this.declaration.IsOverride || this.declaration.IsAbstract;
        //^ assume result ==> !this.IsStatic;
        return result;
      }
    }

    /// <summary>
    /// True if the implementation of this method is not managed by the runtime.
    /// </summary>
    /// <value></value>
    public bool IsUnmanaged {
      get { return this.declaration.IsUnmanaged; }
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public override IName Name {
      get { return this.declaration.QualifiedName; }
    }

    /// <summary>
    /// True if the method signature must not be mangled during the interoperation with COM code.
    /// </summary>
    /// <value></value>
    public bool PreserveSignature {
      get { return this.declaration.PreserveSignature; }
    }

    /// <summary>
    /// True if the method calls another method containing security code. If this flag is set, the method
    /// should have System.Security.DynamicSecurityMethodAttribute present in its list of custom attributes.
    /// </summary>
    /// <value></value>
    public bool RequiresSecurityObject {
      get { return this.declaration.RequiresSecurityObject; }
    }

    /// <summary>
    /// Custom attributes associated with the method's return value.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.declaration.ReturnValueAttributes; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.declaration.ReturnValueCustomModifiers; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.declaration.ReturnValueIsByRef; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.declaration.ReturnValueIsModified; }
    }

    /// <summary>
    /// The return value has associated marshalling information.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.declaration.ReturnValueIsMarshalledExplicitly; }
    }

    /// <summary>
    /// Specifies how the return value is marshalled when the method is called from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.declaration.ReturnValueMarshallingInformation; }
    }

    /// <summary>
    /// The name of the parameter to which the return value is marshalled. Returns Dummy.Name if the name has not been specified.
    /// </summary>
    public IName ReturnValueName {
      get { return Dummy.Name; }
    }

    /// <summary>
    /// Declarative security actions for this method.
    /// </summary>
    /// <value></value>
    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return this.declaration.SecurityAttributes; }
    }

    /// <summary>
    /// The parameters of this method.
    /// </summary>
    public virtual IEnumerable<ParameterDefinition> Parameters {
      get {
        foreach (ParameterDeclaration parameter in this.declaration.Parameters)
          yield return parameter.ParameterDefinition;
      }
    }

    /// <summary>
    /// The number of required parameters of the method.
    /// </summary>
    /// <value></value>
    public ushort ParameterCount {
      get {
        if (this.declaration.parameters == null) return 0;
        return (ushort)this.declaration.parameters.Count;
      }
    }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    /// <summary>
    /// True if the method is a constructor.
    /// </summary>
    /// <value></value>
    public bool IsConstructor {
      get { return this.Name.Value.Equals(".ctor"); } //  TODO: Implement this properly
    }

    /// <summary>
    /// True if the method is a static constructor.
    /// </summary>
    /// <value></value>
    public bool IsStaticConstructor {
      get { return this.Name.Value.Equals(".cctor"); }  //  TODO: Implement this properly
    }

    /// <summary>
    /// Detailed information about the PInvoke stub. Identifies which method to call, which module has the method and the calling convention among other things.
    /// </summary>
    /// <value></value>
    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.declaration.PlatformInvokeData; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this,
        NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName);
    }

    /// <summary>
    /// 
    /// </summary>
    public IName UnqualifiedName {
      get { return this.declaration.Name; }
    }

    #region IMethodDefinition Members

    IMethodBody IMethodDefinition.Body {
      get
        //^^ requires !this.IsAbstract && !this.IsExternal;
      {
        //^ assume !this.IsAbstract && !this.IsExternal;
        return this.Body;
      }
    }

    IEnumerable<IGenericMethodParameter> IMethodDefinition.GenericParameters {
      get {
        return IteratorHelper.GetConversionEnumerable<GenericMethodParameter, IGenericMethodParameter>(this.GenericParameters);
      }
    }
    IEnumerable<IParameterDefinition> IMethodDefinition.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<ParameterDefinition, IParameterDefinition>(this.Parameters); }
    }


    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        return IteratorHelper.GetConversionEnumerable<ParameterDefinition, IParameterTypeInformation>(this.Parameters);
      }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get {
        return this.ReturnValueCustomModifiers;
      }
    }

    #endregion

    #region IMethodReference Members

    /// <summary>
    /// The method being referred to.
    /// </summary>
    /// <value></value>
    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    /// <summary>
    /// Information about this types of the extra arguments supplied at the call sites that references the method with this object.
    /// </summary>
    /// <value></value>
    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return Enumerable<IParameterTypeInformation>.Empty; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class ParameterDefinition : IParameterDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaration"></param>
    protected internal ParameterDefinition(ParameterDeclaration declaration) {
      this.declaration = declaration;
    }

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.declaration.Attributes; }
    }

    /// <summary>
    /// The method or property that defines this parameter.
    /// </summary>
    public ISignature ContainingSignature {
      get { return this.declaration.ContainingSignature.SignatureDefinition; }
    }

    /// <summary>
    /// The list of custom modifiers, if any, associated with the parameter. Evaluate this property only if IsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> CustomModifiers {
      get { return this.declaration.CustomModifiers; }
    }

    //^ [SpecPublic]
    private ParameterDeclaration declaration;

    /// <summary>
    /// A compile time constant value that should be supplied as the corresponding argument value by callers that do not explicitly specify an argument value for this parameter.
    /// </summary>
    /// <value></value>
    public CompileTimeConstant DefaultValue {
      get
        //^^ requires this.HasDefaultValue;
      {
        //^ assume this.HasDefaultValue == ((IParameterDefinition)this).HasDefaultValue;
        CompileTimeConstant/*?*/ result = this.declaration.DefaultValue as CompileTimeConstant;
        //^ assume result != null; //follows from above assumption and the precondition.
        return result;
      }
    }

    /// <summary>
    /// Calls visitor.Visit(IParameterDefinition);
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Calls visitor.VisitReference(IParameterDefinition);
    /// </summary>
    /// <param name="visitor"></param>
    public virtual void DispatchAsReference(IMetadataVisitor visitor) {
      visitor.VisitReference(this);
    }

    /// <summary>
    /// True if the parameter has a default value that should be supplied as the argument value by a caller for which the argument value has not been explicitly specified.
    /// </summary>
    /// <value></value>
    public bool HasDefaultValue {
      get
        //^ ensures result == this.declaration.HasDefaultValue;
      {
        return this.declaration.HasDefaultValue;
      }
    }

    /// <summary>
    /// The position in the parameter list where this instance can be found.
    /// </summary>
    /// <value></value>
    public ushort Index {
      get { return this.declaration.Index; }
    }

    /// <summary>
    /// True if the parameter is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool IsByReference {
      get { return this.declaration.IsOut || this.declaration.IsRef; }
    }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee.
    /// </summary>
    /// <value></value>
    public bool IsIn {
      get { return this.declaration.IsIn; }
    }

    /// <summary>
    /// This parameter has associated marshalling information.
    /// </summary>
    /// <value></value>
    public bool IsMarshalledExplicitly {
      get { return this.declaration.IsMarshalledExplicitly; }
    }

    /// <summary>
    /// This parameter has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool IsModified {
      get { return this.declaration.IsModified; }
    }

    /// <summary>
    /// True if the argument value must be included in the marshalled arguments passed to a remote callee only if it is different from the default value (if there is one).
    /// </summary>
    /// <value></value>
    public bool IsOptional {
      get { return this.declaration.IsOptional; }
    }

    /// <summary>
    /// True if the final value assigned to the parameter will be marshalled with the return values passed back from a remote callee.
    /// </summary>
    /// <value></value>
    public bool IsOut {
      get { return this.declaration.IsOut; }
    }

    /// <summary>
    /// True if the parameter has the ParamArrayAttribute custom attribute.
    /// </summary>
    /// <value></value>
    public bool IsParameterArray {
      get { return this.declaration.IsParameterArray; }
    }

    /// <summary>
    /// Specifies how this parameter is marshalled when it is accessed from unmanaged code.
    /// </summary>
    /// <value></value>
    public IMarshallingInformation MarshallingInformation {
      get { return Dummy.MarshallingInformation; }
    }

    /// <summary>
    /// The element type of the parameter array.
    /// </summary>
    /// <value></value>
    public ITypeReference ParamArrayElementType {
      get
        //^^ requires this.IsParameterArray;
      {
        return this.declaration.ParamArrayElementType;
      }
    }

    /// <summary>
    /// Gets the parameter declarations.
    /// </summary>
    /// <value>The parameter declarations.</value>
    public IEnumerable<ParameterDeclaration> ParameterDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<ParameterDeclaration>(this.declaration); }
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.declaration.Name; }
    }

    /// <summary>
    /// The type of argument value that corresponds to this parameter.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    #region IParameterDefinition Members

    IMetadataConstant IParameterDefinition.DefaultValue {
      get { return this.DefaultValue; }
    }

    #endregion

    #region IReference Members

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get {
        foreach (ParameterDeclaration parameterDeclaration in this.ParameterDeclarations)
          yield return parameterDeclaration.SourceLocation;
      }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class PropertyDefinition : TypeDefinitionMember, IPropertyDefinition {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaration"></param>
    public PropertyDefinition(PropertyDeclaration declaration) {
      this.declaration = declaration;
    }

    /// <summary>
    /// A list of methods that are associated with the property.
    /// </summary>
    public IEnumerable<IMethodReference> Accessors {
      get {
        foreach (MethodDeclaration accessor in this.declaration.Accessors)
          yield return accessor.MethodDefinition;
      }
    }

    /// <summary>
    /// The declaration that corresponds to this definition.
    /// </summary>
    /// <value></value>
    public override ITypeDeclarationMember Declaration {
      get { return this.declaration; }
    }
    readonly PropertyDeclaration declaration;

    /// <summary>
    /// A compile time constant value that provides the default value for the property. (Who uses this and why?)
    /// </summary>
    /// <value></value>
    public CompileTimeConstant DefaultValue {
      get
        //^^ requires this.HasDefaultValue;
      {
        //^ assume this.declaration.HasDefaultValue; //follows from precondition
        CompileTimeConstant/*?*/ defaultValue = this.declaration.DefaultValue as CompileTimeConstant;
        if (defaultValue == null) defaultValue = new DummyConstant(this.declaration.DefaultValue.SourceLocation);
        return defaultValue;
      }
    }

    /// <summary>
    /// Calls the visitor.Visit(IPropertyDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    /// <summary>
    /// Throws an InvalidOperation exception since valid Metadata never refers directly to a property.
    /// </summary>
    public override void DispatchAsReference(IMetadataVisitor visitor) {
      throw new InvalidOperationException();
    }

    /// <summary>
    /// The method used to get the value of this property. May be absent (null).
    /// </summary>
    /// <value></value>
    public IMethodDefinition/*?*/ Getter {
      get {
        MethodDeclaration/*?*/ getter = this.declaration.Getter;
        if (getter == null) return null;
        return getter.MethodDefinition;
      }
    }

    /// <summary>
    /// True if this property has a compile time constant associated with that serves as a default value for the property. (Who uses this and why?)
    /// </summary>
    /// <value></value>
    public bool HasDefaultValue {
      get { return this.declaration.HasDefaultValue; }
    }

    /// <summary>
    /// True if the property gets special treatment from the runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return this.declaration.IsRuntimeSpecial; }
    }

    /// <summary>
    /// True if this property is special in some way, as specified by the name.
    /// </summary>
    public bool IsSpecialName {
      get { return this.declaration.IsSpecialName; }
    }

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<ParameterDefinition> Parameters {
      get {
        foreach (ParameterDeclaration parameter in this.declaration.Parameters)
          yield return parameter.ParameterDefinition;
      }
    }

    /// <summary>
    /// Gets the property declarations.
    /// </summary>
    /// <value>The property declarations.</value>
    public IEnumerable<PropertyDeclaration> PropertyDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<PropertyDeclaration>(this.declaration); }
    }

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public override IName Name {
      get { return this.declaration.QualifiedName; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.declaration.ReturnValueCustomModifiers; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.declaration.ReturnValueIsByRef; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.declaration.ReturnValueIsModified; }
    }

    /// <summary>
    /// The method used to set the value of this property. May be absent (null).
    /// </summary>
    /// <value></value>
    public IMethodReference/*?*/ Setter {
      get {
        MethodDeclaration/*?*/ setter = this.declaration.Setter;
        if (setter == null) return null;
        return setter.MethodDefinition;
      }
    }

    #region IPropertyDefinition Members

    IMetadataConstant IPropertyDefinition.DefaultValue {
      get { return this.DefaultValue; }
    }

    IMethodReference/*?*/ IPropertyDefinition.Getter {
      get { return this.Getter; }
    }

    #endregion

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        return IteratorHelper.GetConversionEnumerable<ParameterDefinition, IParameterTypeInformation>(this.Parameters);
      }
    }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get {
        return this.ReturnValueCustomModifiers;
      }
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
    }

    /// <summary>
    /// True if the referenced method or property does not require an instance of its declaring type as its first argument.
    /// </summary>
    public bool IsStatic {
      get { return true; }
    }

    #endregion

    #region IPropertyDefinition Members


    IEnumerable<IParameterDefinition> IPropertyDefinition.Parameters {
      get { return IteratorHelper.GetConversionEnumerable<ParameterDefinition, IParameterDefinition>(this.Parameters); }
    }

    #endregion

    #region IMetadataConstantContainer

    IMetadataConstant IMetadataConstantContainer.Constant {
      get { return this.DefaultValue; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public class SignatureDefinition : ISignature {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="declaration"></param>
    public SignatureDefinition(SignatureDeclaration declaration) {
      this.declaration = declaration;
    }

    readonly SignatureDeclaration declaration;

    /// <summary>
    /// The parameters forming part of this signature.
    /// </summary>
    /// <value></value>
    public IEnumerable<ParameterDefinition> Parameters {
      get {
        foreach (ParameterDeclaration parameterDeclaration in this.declaration.Parameters)
          yield return parameterDeclaration.ParameterDefinition;
      }
    }

    /// <summary>
    /// Gets the return value attributes.
    /// </summary>
    /// <value>The return value attributes.</value>
    public IEnumerable<CustomAttribute> ReturnValueAttributes {
      get { return this.declaration.ReturnValueAttributes; }
    }

    /// <summary>
    /// Returns the list of custom modifiers, if any, associated with the returned value. Evaluate this property only if ReturnValueIsModified is true.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.declaration.ReturnValueCustomModifiers; }
    }

    /// <summary>
    /// True if the return value is passed by reference (using a managed pointer).
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsByRef {
      get { return this.declaration.ReturnValueIsByRef; }
    }

    /// <summary>
    /// True if the return value has one or more custom modifiers associated with it.
    /// </summary>
    /// <value></value>
    public bool ReturnValueIsModified {
      get { return this.declaration.ReturnValueIsModified; }
    }

    /// <summary>
    /// Gets the signature declarations.
    /// </summary>
    /// <value>The signature declarations.</value>
    public IEnumerable<SignatureDeclaration> SignatureDeclarations {
      get {
        return IteratorHelper.GetSingletonEnumerable<SignatureDeclaration>(this.declaration);
      }
    }

    /// <summary>
    /// The return type of the method or type of the property.
    /// </summary>
    /// <value></value>
    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    /// <summary>
    /// Calling convention of the signature.
    /// </summary>
    /// <value></value>
    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
    }

    /// <summary>
    /// True if the referenced method or property does not require an instance of its declaring type as its first argument.
    /// </summary>
    public bool IsStatic {
      get { return true; }
    }

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        return IteratorHelper.GetConversionEnumerable<ParameterDefinition, IParameterTypeInformation>(this.Parameters);
      }
    }

    #endregion
  }

  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="MemberType"></typeparam>
  public abstract class SpecializedTypeDefinitionMember<MemberType> : ITypeDefinitionMember
    where MemberType : ITypeDefinitionMember  //  This constraint should also require the MemberType to be unspecialized.
  {

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unspecializedVersion"></param>
    /// <param name="containingGenericTypeInstance"></param>
    protected SpecializedTypeDefinitionMember(MemberType/*!*/ unspecializedVersion, IGenericTypeInstance containingGenericTypeInstance) {
      this.unspecializedVersion = unspecializedVersion;
      this.containingGenericTypeInstance = containingGenericTypeInstance;
    }

    /// <summary>
    /// 
    /// </summary>
    public IGenericTypeInstance ContainingGenericTypeInstance {
      get { return this.containingGenericTypeInstance; }
    }
    readonly IGenericTypeInstance containingGenericTypeInstance;

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void DispatchAsReference(IMetadataVisitor visitor);

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get {
        ITypeDefinitionMember unspecializedVersion = this.UnspecializedVersion;
        //^ assume unspecializedVersion != null; //The type system guarantees this
        return TypeHelper.VisibilityIntersection(unspecializedVersion.Visibility,
          TypeHelper.TypeVisibilityAsTypeMemberVisibility(this.ContainingGenericTypeInstance));
      }
    }

    /// <summary>
    /// 
    /// </summary>
    public MemberType/*!*/ UnspecializedVersion {
      get { return this.unspecializedVersion; }
    }
    readonly MemberType/*!*/ unspecializedVersion;

    #region ITypeDefinitionMember Members

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingTypeDefinition {
      get
        //^ ensures result == this.ContainingGenericTypeInstance;
      {
        return this.ContainingGenericTypeInstance;
      }
    }

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    /// <value></value>
    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IContainerMember<ITypeDefinition> Members

    /// <summary>
    /// The container instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public ITypeDefinition Container {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region INamedEntity Members

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public IName Name {
      get { return this.UnspecializedVersion.Name; }
    }

    #endregion

    #region IDefinition Members

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.UnspecializedVersion.Attributes; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return this.UnspecializedVersion.Locations; }
    }

    #endregion

    #region IScopeMember<IScope<ITypeDefinitionMember>> Members

    /// <summary>
    /// The scope instance with a Members collection that includes this instance.
    /// </summary>
    /// <value></value>
    public IScope<ITypeDefinitionMember> ContainingScope {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

  }

  /// <summary>
  /// 
  /// </summary>
  public abstract class TypeDefinitionMember : ITypeDefinitionMember {

    /// <summary>
    /// A collection of metadata custom attributes that are associated with this definition.
    /// </summary>
    /// <value></value>
    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.Declaration.Attributes; }
    }

    /// <summary>
    /// The type definition that contains this member.
    /// </summary>
    /// <value></value>
    public ITypeDefinition ContainingTypeDefinition {
      get { return this.Declaration.ContainingTypeDeclaration.TypeDefinition; }
    }

    /// <summary>
    /// The declaration that corresponds to this definition.
    /// </summary>
    public abstract ITypeDeclarationMember Declaration { get; }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void DispatchAsReference(IMetadataVisitor visitor);

    /// <summary>
    /// The name of the entity.
    /// </summary>
    /// <value></value>
    public virtual IName Name {
      get { return this.Declaration.Name; }
    }

    /// <summary>
    /// Indicates if the member is public or confined to its containing type, derived types and/or declaring assembly.
    /// </summary>
    /// <value></value>
    public TypeMemberVisibility Visibility {
      get {
        TypeMemberVisibility result = this.Declaration.Visibility;
        if (result == TypeMemberVisibility.Default)
          result = this.Declaration.GetDefaultVisibility();
        return result;
      }
    }


    #region ITypeDefinitionMember Members

    ITypeDefinition ITypeDefinitionMember.ContainingTypeDefinition {
      get { return this.ContainingTypeDefinition; }
    }

    #endregion

    #region ITypeMemberReference Members

    /// <summary>
    /// A reference to the containing type of the referenced type member.
    /// </summary>
    /// <value></value>
    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    /// <summary>
    /// The type definition member this reference resolves to.
    /// </summary>
    /// <value></value>
    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get { return this.Attributes; }
    }

    /// <summary>
    /// A potentially empty collection of locations that correspond to this instance.
    /// </summary>
    /// <value></value>
    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetSingletonEnumerable<ILocation>(this.Declaration.SourceLocation); }
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
