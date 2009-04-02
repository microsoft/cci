//-----------------------------------------------------------------------------
//
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
//
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Cci.Contracts;

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
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
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
      [DebuggerNonUserCode]
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
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomModifier>(); }
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

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    //^ [Confined]
    public override string ToString() {
      if (this.Name == Dummy.Name)
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
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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
      get { return IteratorHelper.GetConversionEnumerable<IParameterDefinition, IParameterTypeInformation>(this.Parameters); }
    }

    #endregion

    #region IMethodReference Members

    uint IMethodReference.InternedKey {
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
    public BuiltinMethods(Compilation compilation) {
      this.nameTable = compilation.NameTable;
      this.platformType = compilation.PlatformType;
    }

    readonly INameTable nameTable;
    readonly PlatformType platformType;

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
      if (this.enumPlusNumMethodFor.TryGetValue(enumType, out result)){
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

    public EventDefinition(EventDeclaration declaration)
      : base() {
      this.declaration = declaration;
    }

    public IEnumerable<MethodDefinition> Accessors {
      get {
        foreach (MethodDeclaration accessor in this.declaration.Accessors)
          yield return accessor.MethodDefinition;
      }
    }

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

    public IMethodDefinition Adder {
      get {
        MethodDeclaration/*?*/ adder = this.declaration.Adder;
        if (adder == null) return Dummy.Method; //TODO: get a default implementation from the declaration
        return adder.MethodDefinition; 
      }
    }

    public MethodDefinition/*?*/ Caller {
      get {
        MethodDeclaration/*?*/ callerDeclaration = this.declaration.Caller;
        if (callerDeclaration == null) return null;
        return callerDeclaration.MethodDefinition;
      }
    }

    public IEnumerable<EventDeclaration> EventDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<EventDeclaration>(this.declaration); }
    }

    /// <summary>
    /// True if the event gets special treatment from the runtime.
    /// </summary>
    public bool IsRuntimeSpecial {
      get { return this.declaration.IsRuntimeSpecial; }
    }

    public bool IsSpecialName {
      get { return this.declaration.IsSpecialName; }
    }

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

  public class FieldDefinition : TypeDefinitionMember, IFieldDefinition {

    protected internal FieldDefinition(FieldDeclaration declaration) {
      this.fieldDeclaration = declaration;
    }

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

    public override ITypeDeclarationMember Declaration {
      get { return this.FieldDeclaration; }
    }

    /// <summary>
    /// Calls the visitor.Visit(IFieldDefinition) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public FieldDeclaration FieldDeclaration {
      get { return this.fieldDeclaration; }
    }
    readonly FieldDeclaration fieldDeclaration;

    public IEnumerable<FieldDeclaration> FieldDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<FieldDeclaration>(this.FieldDeclaration); }
    }

    /// <summary>
    /// The field is aligned on a bit boundary and uses only the BitLength number of least significant bits of the representation of a Type value.
    /// </summary>
    public bool IsBitField {
      get { return this.FieldDeclaration.IsBitField; }
    }

    public bool IsCompileTimeConstant {
      get
        //^ ensures result == this.FieldDeclaration.IsCompileTimeConstant;
      {
        return this.FieldDeclaration.IsCompileTimeConstant; 
      }
    }

    public bool IsMapped {
      get {
        bool result = this.FieldDeclaration.IsMapped;
        //^ assume this.IsStatic == this.FieldDeclaration.IsStatic; //post condition of this.IsStatic;
        return result;
      }
    }

    public bool IsMarshalledExplicitly {
      get
        //^ ensures result == this.FieldDeclaration.IsMarshalledExplicitly;
      {
        return this.FieldDeclaration.IsMarshalledExplicitly; 
      }
    }

    public bool IsNotSerialized {
      get { return this.FieldDeclaration.IsNotSerialized; }
    }

    public bool IsReadOnly {
      get { return this.FieldDeclaration.IsReadOnly; }
    }

    public virtual bool IsRuntimeSpecial {
      get { return this.FieldDeclaration.IsRuntimeSpecial; }
    }

    public bool IsSpecialName {
      get { return this.FieldDeclaration.IsSpecialName; }
    }

    public bool IsStatic {
      get
        //^ ensures result == this.FieldDeclaration.IsStatic;
      {
        return this.FieldDeclaration.IsStatic; 
      }
    }

    public uint Offset {
      get { return this.FieldDeclaration.Offset; }  //  TODO: Implement this...
    }

    public int SequenceNumber {
      get { return -1; }  //  TODO: Implement this...
    }

    public IMarshallingInformation MarshallingInformation {
      get
        //^^ requires this.IsMarshalledExplicitly;
      {
        //^ assume this.FieldDeclaration.IsMarshalledExplicitly; //follows from post condition on this.IsMarshalledExplicitly
        return this.FieldDeclaration.MarshallingInformation; 
      }
    }

    public ITypeReference Type {
      get {
        if (this.type == null) {
          if (this.FieldDeclaration.IsVolatile) {
            ICustomModifier volatileModifier = new CustomModifier(true, this.FieldDeclaration.PlatformType.SystemRuntimeCompilerServicesIsVolatile);
            this.type = new ModifiedTypeReference(this.fieldDeclaration.Compilation.HostEnvironment, this.fieldDeclaration.Type.ResolvedType,
              IteratorHelper.GetSingletonEnumerable(volatileModifier));
          }else
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
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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

  public class FunctionPointerMethod : IMethodDefinition {

    public FunctionPointerMethod(IFunctionPointerTypeReference functionPointer) {
      this.functionPointer = functionPointer;
    }

    public IFunctionPointerTypeReference FunctionPointer {
      get { return this.functionPointer; }
    }
    readonly IFunctionPointerTypeReference functionPointer;

    #region IMethodDefinition Members

    public bool AcceptsExtraArguments {
      get { return (this.functionPointer.CallingConvention & CallingConvention.ExtraArguments) != 0; }
    }

    public virtual IMethodBody Body {
      get {
        //^ assume false;
        return Dummy.MethodBody;
      }
    }

    public CallingConvention CallingConvention {
      get { return this.functionPointer.CallingConvention; }
    }

    public IEnumerable<IGenericMethodParameter> GenericParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IGenericMethodParameter>(); }
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
      [DebuggerNonUserCode]
      get {
        foreach (IParameterTypeInformation ptInfo in this.FunctionPointer.Parameters)
          yield return new FunctionPointerParameter(ptInfo);
        //if (this.AcceptsExtraArguments) {
        //  foreach (IParameterTypeInformation ptInfo in this.FunctionPointer.ExtraArgumentTypes)
        //    yield return new FunctionPointerParameter(ptInfo);
        //}
      }
    }

    public ushort ParameterCount {
      get { return (ushort)IteratorHelper.EnumerableCount(this.FunctionPointer.Parameters); }
    }

    public bool PreserveSignature {
      get { return false; }
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

    public IEnumerable<ISecurityAttribute> SecurityAttributes {
      get { return IteratorHelper.GetEmptyEnumerable<ISecurityAttribute>(); }
    }

    //^ [Confined]
    public override string ToString() {
      return this.functionPointer.ToString();
    }

    public ITypeReference Type {
      get { return this.functionPointer.Type; }
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

    public virtual ITypeDefinition ContainingTypeDefinition {
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
      get { return Dummy.Name; }
    }

    #endregion

    #region IDefinition Members

    public IEnumerable<ICustomAttribute> Attributes {
      get { return IteratorHelper.GetEmptyEnumerable<ICustomAttribute>(); }
    }

    public IEnumerable<ILocation> Locations {
      get { return IteratorHelper.GetEmptyEnumerable<ILocation>(); }
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
      get { return this.functionPointer.Parameters; }
    }

    #endregion

    #region IMethodReference Members

    uint IMethodReference.InternedKey {
      get { return 0; }
    }

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return this.functionPointer.ExtraArgumentTypes; }
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

  public class GenericMethodParameter : GenericParameter, IGenericMethodParameter {

    //^ [NotDelayed]
    public GenericMethodParameter(MethodDefinition definingMethod, GenericMethodParameterDeclaration declaration)
      : base(declaration.Name, declaration.Index, declaration.Variance, declaration.MustBeReferenceType, declaration.MustBeValueType, declaration.MustHaveDefaultConstructor, declaration.Compilation.HostEnvironment.InternFactory)
      //^ requires definingMethod.IsGeneric;
    {
      this.declaration = declaration;
      this.definingMethod = definingMethod;
      //^ base;
    }

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
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    protected override IEnumerable<GenericParameterDeclaration> GetDeclarations() {
      foreach (GenericMethodParameterDeclaration parameterDeclaration in this.ParameterDeclarations)
        yield return parameterDeclaration;
    }

    public IEnumerable<GenericMethodParameterDeclaration> ParameterDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<GenericMethodParameterDeclaration>(this.declaration); }
    }
    readonly GenericMethodParameterDeclaration declaration;

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
    /// Calls the visitor.Visit(IGlobalFieldDefinition) method.
    /// </summary>
    public void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
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

    public IEnumerable<GlobalFieldDeclaration> GlobalFieldDeclarations {
      get {
        if (this.globalFieldDeclarations != null) return this.globalFieldDeclarations.AsReadOnly();
        //^ assert this.globalFieldDeclaration != null;
        return IteratorHelper.GetSingletonEnumerable<GlobalFieldDeclaration>(this.globalFieldDeclaration);
      }
    }
    List<GlobalFieldDeclaration>/*?*/ globalFieldDeclarations;
    //^ invariant globalFieldDeclaration == null <==> globalFieldDeclarations != null && globalFieldDeclarations.Count > 1;

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
      : base(globalMethodDeclaration)
    {
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

  public class MethodBody : ISourceMethodBody {

    public MethodBody(IMethodDefinition methodDefinition, BlockStatement/*?*/ block, bool localsAreZeroed)
      //^ requires block == null <==> methodDefinition.IsAbstract;
    {
      this.block = block;
      this.localsAreZeroed = localsAreZeroed;
      this.methodDefinition = methodDefinition;
    }

    public BlockStatement Block {
      get {
        //^ assume this.block != null; //implied by the precondition
        return this.block;
      }
    }
    BlockStatement/*?*/ block;

    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public virtual void Dispatch(SourceVisitor visitor) {
      visitor.Visit(this);
    }

    private void GenerateIL() 
      //^ ensures this.privateHelperTypes != null;
    {
      this.ilWasGenerated = true;
      if (this.block == null) {
        this.localVariables = IteratorHelper.GetEmptyEnumerable<ILocalDefinition>();
        this.maxStack = 0;
        this.operations = IteratorHelper.GetEmptyEnumerable<IOperation>();
        this.operationExceptionInformation = IteratorHelper.GetEmptyEnumerable<IOperationExceptionInformation>();
        this.localVariables = IteratorHelper.GetEmptyEnumerable<ILocalDefinition>();
        this.privateHelperTypes = IteratorHelper.GetEmptyEnumerable<ITypeDefinition>();
        return;
      }

      MethodBodyNormalizer normalizer = new MethodBodyNormalizer(this.Block.Compilation.HostEnvironment, null, ProvideSourceToILConverter, 
        this.Block.Compilation.SourceLocationProvider, this.Block.Compilation.ContractProvider);
      ISourceMethodBody normalizedBody = normalizer.GetNormalizedSourceMethodBodyFor(this.MethodDefinition, this.Block);

      CodeModelToILConverter converter = new CodeModelToILConverter(this.Block.Compilation.HostEnvironment, 
        this.Block.Compilation.SourceLocationProvider, this.Block.Compilation.ContractProvider);
      converter.ConvertToIL(this.MethodDefinition, normalizedBody.Block);

      this.localVariables = converter.GetLocalVariables();
      this.maxStack = converter.MaximumStackSizeNeeded;
      this.operations = converter.GetOperations();
      this.operationExceptionInformation = converter.GetOperationExceptionInformation();
      this.privateHelperTypes = normalizedBody.PrivateHelperTypes;
    }

    static ISourceToILConverter ProvideSourceToILConverter(IMetadataHost host, ISourceLocationProvider/*?*/ sourceLocationProvider, IContractProvider/*?*/ contractProvider) {
      return new CodeModelToILConverter(host, sourceLocationProvider, contractProvider);
    }

    bool ilWasGenerated;

    public bool LocalsAreZeroed {
      get { return this.localsAreZeroed; }
    }
    bool localsAreZeroed;

    public IEnumerable<ILocalDefinition> LocalVariables {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.localVariables;
      }
    }
    IEnumerable<ILocalDefinition>/*?*/ localVariables;

    // Do we want shallow copy of method body or do we want to reparse it?
    public virtual MethodBody MakeShallowCopyFor(Compilation targetCompilation) {
      return this; //TODO: implement this
    }

    public ushort MaxStack {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.maxStack; 
      }
    }
    ushort maxStack;

    public IMethodDefinition MethodDefinition {
      get { return this.methodDefinition; }
    }
    readonly IMethodDefinition methodDefinition;

    public IEnumerable<IOperation> Operations {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operations; 
      }
    }
    IEnumerable<IOperation>/*?*/ operations;

    public IEnumerable<IOperationExceptionInformation> OperationExceptionInformation {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.operationExceptionInformation;
      }
    }
    IEnumerable<IOperationExceptionInformation> operationExceptionInformation;

    public IEnumerable<ITypeDefinition> PrivateHelperTypes {
      get {
        if (!this.ilWasGenerated) this.GenerateIL();
        return this.privateHelperTypes;
      }
    }
    IEnumerable<ITypeDefinition>/*?*/ privateHelperTypes;

    #region ISourceMethodBody Members

    IBlockStatement ISourceMethodBody.Block {
      get { return this.Block; }
    }

    #endregion
  }

  public class MethodDefinition : TypeDefinitionMember, IMethodDefinition {

    public MethodDefinition(MethodDeclaration declaration) {
      this.declaration = declaration;
    }

    public bool AcceptsExtraArguments {
      get { return this.declaration.AcceptsExtraArguments; }
    }

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


    public CallingConvention CallingConvention {
      get { return this.declaration.CallingConvention; }
    }

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

    public static IEnumerable<IMethodDefinition> EmptyCollection {
      get { return MethodDefinition.emptyCollection; }
    }
    static readonly IEnumerable<IMethodDefinition> emptyCollection = new List<IMethodDefinition>(0).AsReadOnly();

    public IEnumerable<GenericMethodParameter> GenericParameters {
      get {
        foreach (GenericMethodParameterDeclaration parameterDeclaration in this.declaration.GenericParameters)
          yield return parameterDeclaration.GenericMethodParameterDefinition;
      }
    }

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

    public uint InternedKey {
      get {
        if (this.internedKey == 0) {
          this.internedKey = this.Declaration.CompilationPart.Compilation.HostEnvironment.InternFactory.GetMethodInternedKey(this);
        }
        return this.internedKey;
      }
    }
    uint internedKey;

    public bool IsAbstract {
      get { return this.declaration.IsAbstract; }
    }

    /// <summary>
    /// True if the method can only be overridden when it is also accessible. 
    /// </summary>
    public bool IsAccessCheckedOnOverride {
      get { return this.declaration.IsAccessCheckedOnOverride; }
    }

    public bool IsCil {
      get { return this.declaration.IsCil; }
    }

    public bool IsExternal {
      get { return this.declaration.IsExternal; }
    }

    public bool IsForwardReference {
      get { return this.declaration.IsForwardReference; }
    }

    public bool IsGeneric {
      get 
        //^ ensures result == this.declaration.IsGeneric;
      { 
        return this.declaration.IsGeneric; 
      }
    }

    public bool IsHiddenBySignature {
      get { return this.declaration.IsHiddenBySignature; }
    }

    public bool IsNativeCode {
      get { return this.declaration.IsNativeCode; }
    }

    public bool IsNewSlot {
      get { return this.declaration.IsVirtual && !this.declaration.IsOverride; }
    }

    public bool IsNeverInlined {
      get { return this.declaration.IsNeverInlined; }
    }

    public bool IsNeverOptimized {
      get { return this.declaration.IsNeverOptimized; }
    }

    public bool IsPlatformInvoke {
      get { return this.declaration.IsPlatformInvoke; }
    }

    public bool IsRuntimeImplemented {
      get { return this.declaration.IsRuntimeImplemented; }
    }

    public bool IsRuntimeInternal {
      get { return this.declaration.IsRuntimeInternal; }
    }

    public bool IsRuntimeSpecial {
      get { return this.declaration.IsRuntimeSpecial; }
    }

    public bool IsSealed {
      get { return this.declaration.IsSealed; }
    }

    public bool IsSpecialName {
      get { return this.declaration.IsSpecialName; }
    }

    public bool IsStatic {
      get { return this.declaration.IsStatic; }
    }

    public bool IsSynchronized {
      get { return this.declaration.IsSynchronized; }
    }

    public bool IsVirtual {
      get { 
        bool result = this.declaration.IsVirtual || this.declaration.IsOverride || this.declaration.IsAbstract; 
        //^ assume result ==> !this.IsStatic;
        return result;
      }
    }

    public bool IsUnmanaged {
      get { return this.declaration.IsUnmanaged; }
    }

    public override IName Name {
      get { return this.declaration.QualifiedName; }
    }

    public bool PreserveSignature {
      get { return this.declaration.PreserveSignature; }
    }

    public bool RequiresSecurityObject {
      get { return this.declaration.RequiresSecurityObject; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.declaration.ReturnValueAttributes; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.declaration.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.declaration.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.declaration.ReturnValueIsModified; }
    }

    public bool ReturnValueIsMarshalledExplicitly {
      get { return this.declaration.ReturnValueIsMarshalledExplicitly; }
    }

    public IMarshallingInformation ReturnValueMarshallingInformation {
      get { return this.declaration.ReturnValueMarshallingInformation; }
    }

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

    public ushort ParameterCount {
      get {
        if (this.declaration.parameters == null) return 0;
        return (ushort)this.declaration.parameters.Count;
      }
    }

    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    public bool IsConstructor {
      get { return this.Name.Value.Equals(".ctor"); } //  TODO: Implement this properly
    }

    public bool IsStaticConstructor {
      get { return this.Name.Value.Equals(".cctor"); }  //  TODO: Implement this properly
    }

    public IPlatformInvokeInformation PlatformInvokeData {
      get { return this.declaration.PlatformInvokeData; }
    }

    //^ [Confined]
    public override string ToString() {
      return MemberHelper.GetMethodSignature(this, 
        NameFormattingOptions.ReturnType|NameFormattingOptions.Signature|NameFormattingOptions.ParameterModifiers|NameFormattingOptions.ParameterName);
    }

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
      [DebuggerNonUserCode]
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

    public IMethodDefinition ResolvedMethod {
      get { return this; }
    }

    public IEnumerable<IParameterTypeInformation> ExtraParameters {
      get { return IteratorHelper.GetEmptyEnumerable<IParameterTypeInformation>(); }
    }

    #endregion

  }

  public class ParameterDefinition : IParameterDefinition {

    protected internal ParameterDefinition(ParameterDeclaration declaration) {
      this.declaration = declaration;
    }

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.declaration.Attributes; }
    }

    public ISignature ContainingSignature {
      get { 
        return this.declaration.ContainingSignature.SignatureDefinition; 
      }
    }

    public IEnumerable<CustomModifier> CustomModifiers {
      get { return this.declaration.CustomModifiers; }
    }

    //^ [SpecPublic]
    private ParameterDeclaration declaration;

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

    public virtual void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public bool HasDefaultValue {
      get
        //^ ensures result == this.declaration.HasDefaultValue;
      { 
        return this.declaration.HasDefaultValue; 
      }
    }

    public ushort Index {
      get { return this.declaration.Index; }
    }

    public bool IsByReference {
      get { return this.declaration.IsOut || this.declaration.IsRef; }
    }

    public bool IsIn {
      get { return this.declaration.IsIn; }
    }

    public bool IsMarshalledExplicitly {
      get { return this.declaration.IsMarshalledExplicitly; }
    }

    public bool IsModified {
      get { return this.declaration.IsModified; }
    }

    public bool IsOptional {
      get { return this.declaration.IsOptional; }
    }

    public bool IsOut {
      get { return this.declaration.IsOut; }
    }

    public bool IsParameterArray {
      get { return this.declaration.IsParameterArray; }
    }

    public IMarshallingInformation MarshallingInformation {
      get { throw new Exception("The method or operation is not implemented."); }
    }

    public ITypeReference ParamArrayElementType {
      get
        //^^ requires this.IsParameterArray;
      { 
        return this.declaration.ParamArrayElementType; 
      }
    }

    public IEnumerable<ParameterDeclaration> ParameterDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<ParameterDeclaration>(this.declaration); }
    }

    public IName Name {
      get { return this.declaration.Name; }
    }

    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    #region IParameterDefinition Members

    IEnumerable<ICustomModifier> IParameterTypeInformation.CustomModifiers {
      get {
        return IteratorHelper.GetConversionEnumerable<CustomModifier, ICustomModifier>(this.CustomModifiers);
      }
    }

    IMetadataConstant IParameterDefinition.DefaultValue {
      get { return this.DefaultValue; }
    }

    #endregion

    #region IReference Members

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

  public class PropertyDefinition : TypeDefinitionMember, IPropertyDefinition {

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

    public override ITypeDeclarationMember Declaration {
      get { return this.declaration; }
    }
    readonly PropertyDeclaration declaration;

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
    /// Calls the visitor.Visit(xxxxx) method.
    /// </summary>
    public override void Dispatch(IMetadataVisitor visitor) {
      visitor.Visit(this);
    }

    public IMethodDefinition/*?*/ Getter {
      get {
        MethodDeclaration/*?*/ getter = this.declaration.Getter;
        if (getter == null) return null;
        return getter.MethodDefinition;
      }
    }

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

    public IEnumerable<ParameterDefinition> Parameters {
      get {
        foreach (ParameterDeclaration parameter in this.declaration.Parameters)
          yield return parameter.ParameterDefinition;
      }
    }

    public IEnumerable<PropertyDeclaration> PropertyDeclarations {
      get { return IteratorHelper.GetSingletonEnumerable<PropertyDeclaration>(this.declaration); }
    }

    public override IName Name {
      get { return this.declaration.QualifiedName; }
    }

    public IEnumerable<ICustomAttribute> ReturnValueAttributes {
      get { return this.declaration.ReturnValueAttributes; }
    }

    public IEnumerable<ICustomModifier> ReturnValueCustomModifiers {
      get { return this.declaration.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.declaration.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.declaration.ReturnValueIsModified; }
    }

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

    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get {
        return this.ReturnValueCustomModifiers;
      }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
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

  public class SignatureDefinition : ISignature {

    public SignatureDefinition(SignatureDeclaration declaration) {
      this.declaration = declaration;
    }

    readonly SignatureDeclaration declaration;

    public IEnumerable<ParameterDefinition> Parameters {
      get {
        foreach (ParameterDeclaration parameterDeclaration in this.declaration.Parameters)
          yield return parameterDeclaration.ParameterDefinition;
      }
    }

    public IEnumerable<CustomAttribute> ReturnValueAttributes {
      get { return this.declaration.ReturnValueAttributes; }
    }

    public IEnumerable<CustomModifier> ReturnValueCustomModifiers {
      get { return this.declaration.ReturnValueCustomModifiers; }
    }

    public bool ReturnValueIsByRef {
      get { return this.declaration.ReturnValueIsByRef; }
    }

    public bool ReturnValueIsModified {
      get { return this.declaration.ReturnValueIsModified; }
    }

    public IEnumerable<SignatureDeclaration> SignatureDeclarations {
      get {
        return IteratorHelper.GetSingletonEnumerable<SignatureDeclaration>(this.declaration);
      }
    }

    public ITypeReference Type {
      get { return this.declaration.Type.ResolvedType; }
    }

    public CallingConvention CallingConvention {
      get { return CallingConvention.Default; }
    }

    #region ISignature Members

    IEnumerable<IParameterTypeInformation> ISignature.Parameters {
      get {
        return IteratorHelper.GetConversionEnumerable<ParameterDefinition, IParameterTypeInformation>(this.Parameters);
      }
    }

    IEnumerable<ICustomModifier> ISignature.ReturnValueCustomModifiers {
      get {
        return IteratorHelper.GetConversionEnumerable<CustomModifier, ICustomModifier>(this.ReturnValueCustomModifiers);
      }
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

  public abstract class TypeDefinitionMember : ITypeDefinitionMember {

    public IEnumerable<ICustomAttribute> Attributes {
      get { return this.Declaration.Attributes; }
    }

    public ITypeDefinition ContainingTypeDefinition {
      get { return this.Declaration.ContainingTypeDeclaration.TypeDefinition; }
    }

    public abstract ITypeDeclarationMember Declaration { get; }

    /// <summary>
    /// Calls the visitor.Visit(T) method where T is the most derived object model node interface type implemented by the concrete type
    /// of the object implementing IDoubleDispatcher. The dispatch method does not invoke Dispatch on any child objects. If child traversal
    /// is desired, the implementations of the Visit methods should do the subsequent dispatching.
    /// </summary>
    public abstract void Dispatch(IMetadataVisitor visitor);

    public virtual IName Name {
      get { return this.Declaration.Name; }
    }

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

    public ITypeReference ContainingType {
      get { return this.ContainingTypeDefinition; }
    }

    public ITypeDefinitionMember ResolvedTypeDefinitionMember {
      get { return this; }
    }

    #endregion

    #region IReference Members

    IEnumerable<ICustomAttribute> IReference.Attributes {
      get {
        return this.Attributes;
      }
    }

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
