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
using System.Text;
using Microsoft.Cci;

namespace ModuleReaderTests {
  public class TypeMemberTests {
    readonly ModuleReaderTestClass ModuleReaderTest;
    readonly IName System;
    readonly IName Collections;
    readonly IName Generic;
    readonly IName List;
    readonly IName Add;
    readonly IName _emptyArray;
    readonly IName _defaultCapacity;
    readonly IName Item;
    readonly IName GC;
    readonly IName ClearCache;
    readonly IName MarshalTest;
    readonly IName CreateFontIndirect;
    readonly IName CreateFontIndirectArray;
    readonly IName Assem;
    readonly IName GenMethod;
    readonly IName Generic1;
    readonly IName fieldT;
    readonly IName propT;
    readonly IName get_propT;
    readonly IName GenericEvent;
    readonly IName free;
    readonly IName PtrToStringChars;
    readonly IName _A0x8179e609;
    readonly IName _InitializedPerProcess_initializer__CurrentDomain__CrtImplementationDetails_____Q2P6MXXZEA;

    public TypeMemberTests(ModuleReaderTestClass mrTest) {
      this.ModuleReaderTest = mrTest;
      this.System = mrTest.NameTable.GetNameFor("System");
      this.Collections = mrTest.NameTable.GetNameFor("Collections");
      this.Generic = mrTest.NameTable.GetNameFor("Generic");
      this.List = mrTest.NameTable.GetNameFor("List");
      this.Add = mrTest.NameTable.GetNameFor("Add");
      this._emptyArray = mrTest.NameTable.GetNameFor("_emptyArray");
      this._defaultCapacity = mrTest.NameTable.GetNameFor("_defaultCapacity");
      this.Item = mrTest.NameTable.GetNameFor("Item");
      this.GC = mrTest.NameTable.GetNameFor("GC");
      this.ClearCache = mrTest.NameTable.GetNameFor("ClearCache");
      this.MarshalTest = mrTest.NameTable.GetNameFor("MarshalTest");
      this.CreateFontIndirect = mrTest.NameTable.GetNameFor("CreateFontIndirect");
      this.CreateFontIndirectArray = mrTest.NameTable.GetNameFor("CreateFontIndirectArray");
      this.Assem = mrTest.NameTable.GetNameFor("Assem");
      this.GenMethod = mrTest.NameTable.GetNameFor("GenMethod");
      this.Generic1 = mrTest.NameTable.GetNameFor("Generic1");
      this.fieldT = mrTest.NameTable.GetNameFor("fieldT");
      this.propT = mrTest.NameTable.GetNameFor("propT");
      this.get_propT = mrTest.NameTable.GetNameFor("get_propT");
      this.GenericEvent = mrTest.NameTable.GetNameFor("GenericEvent");
      this.free = mrTest.NameTable.GetNameFor("free");
      this.PtrToStringChars = mrTest.NameTable.GetNameFor("PtrToStringChars");
      this._A0x8179e609 = mrTest.NameTable.GetNameFor("?A0x8179e609");
      this._InitializedPerProcess_initializer__CurrentDomain__CrtImplementationDetails_____Q2P6MXXZEA = mrTest.NameTable.GetNameFor("?InitializedPerProcess$initializer$@CurrentDomain@<CrtImplementationDetails>@@$$Q2P6MXXZEA");
    }

    public bool RunTypeMemberTests() {
      bool ret = true;
      if (!this.TestListAddMethod()) {
        Console.WriteLine("TestListAddMethod - Failed");
        ret = false;
      }
      if (!this.TestListEmptyArrayField()) {
        Console.WriteLine("TestListEmptyArrayField - Failed");
        ret = false;
      }
      if (!this.TestListDefaultCapacityField()) {
        Console.WriteLine("TestListDefaultCapacityField - Failed");
        ret = false;
      }
      if (!this.TestListItemProperty()) {
        Console.WriteLine("TestListItemProperty - Failed");
        ret = false;
      }
      if (!this.TestGCClearCacheEvent()) {
        Console.WriteLine("TestGCClearCacheEvent - Failed");
        ret = false;
      }
      if (!this.TestGeneric1Event()) {
        Console.WriteLine("TestGeneric1Event - Failed");
        ret = false;
      }
      if (!this.TestListAddMethodInstructions()) {
        Console.WriteLine("TestListAddMethodInstructions - Failed");
        ret = false;
      }
      if (!this.TestCreateFontIndirect()) {
        Console.WriteLine("TestCreateFontIndirect - Failed");
        ret = false;
      }
      if (!this.TestCreateFontIndirectArray()) {
        Console.WriteLine("TestCreateFontIndirectArray - Failed");
        ret = false;
      }
      if (!this.TestAssemGenericMethod()) {
        Console.WriteLine("TestAssemGenericMethod - Failed");
        ret = false;
      }
      if (!this.TestAssemGenericMethodCall()) {
        Console.WriteLine("TestAssemGenericMethodCall - Failed");
        ret = false;
      }
      if (!this.TestGenericTypeField()) {
        Console.WriteLine("TestGenericTypeField - Failed");
        ret = false;
      }
      if (!this.TestGenericTypeMethod()) {
        Console.WriteLine("TestGenericTypeMethod - Failed");
        ret = false;
      }
      if (!this.TestGenericTypeProperty()) {
        Console.WriteLine("TestGenericTypeProperty - Failed");
        ret = false;
      }
      if (!this.TestGenericTypeEvent()) {
        Console.WriteLine("TestGenericTypeEvent - Failed");
        ret = false;
      }
      if (!this.TestfreeMethod()) {
        Console.WriteLine("TestfreeMethod - Failed");
        ret = false;
      }
      if (!this.TestPtrToStringCharsMethod()) {
        Console.WriteLine("TestPtrToStringCharsMethod - Failed");
        ret = false;
      }
      if (!this.TestGenMethodType()) {
        Console.WriteLine("TestGenMethodType - Failed");
        ret = false;
      }
      if (!this.TestPhxPointerGlobalField()) {
        Console.WriteLine("TestPhxPointerGlobalField - Failed");
        ret = false;
      }
      return ret;
    }

    public bool TestListAddMethod() {
      ITypeDefinition type = Helper.GetNamespaceType(Helper.GetNamespace(this.ModuleReaderTest.MscorlibAssembly, this.System, this.Collections, this.Generic), this.List);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.Add);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public hidebysig newslot virtual final instance explicit default void Add(
  !0  item
)cil managed
{
  .maxstack 4
  .locals init(
    int32  V_0
  )
  IL_0000:  ldarg.0
  IL_0001:  ldfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_0006:  ldarg.0
  IL_0007:  ldfld!0[]System.Collections.Generic.List`1<!0>::_items
  IL_000c:  ldlen
  IL_000d:  conv.i4
  IL_000e:  bne.un.s IL_001e
  IL_0010:  ldarg.0
  IL_0011:  ldarg.0
  IL_0012:  ldfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  call instance void System.Collections.Generic.List`1<!0>::EnsureCapacity(int32)
  IL_001e:  ldarg.0
  IL_001f:  ldfld!0[]System.Collections.Generic.List`1<!0>::_items
  IL_0024:  ldarg.0
  IL_0025:  dup
  IL_0026:  ldfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_002b:  dup
  IL_002c:  stloc.0
  IL_002d:  ldc.i4.1
  IL_002e:  add
  IL_002f:  stfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_0034:  ldloc.0
  IL_0035:  ldarg.1
  IL_0036:  stelem!0
  IL_003b:  ldarg.0
  IL_003c:  dup
  IL_003d:  ldfld int32 System.Collections.Generic.List`1<!0>::_version
  IL_0042:  ldc.i4.1
  IL_0043:  add
  IL_0044:  stfld int32 System.Collections.Generic.List`1<!0>::_version
  IL_0049:  ret
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestListEmptyArrayField() {
      ITypeDefinition type = Helper.GetNamespaceType(Helper.GetNamespace(this.ModuleReaderTest.MscorlibAssembly, this.System, this.Collections, this.Generic), this.List);
      IFieldDefinition field = Helper.GetFieldNamed(type, this._emptyArray);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.FieldDefinition(field);
      string result =
@".field private static!0[]_emptyArray
{
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestListDefaultCapacityField() {
      ITypeDefinition type = Helper.GetNamespaceType(Helper.GetNamespace(this.ModuleReaderTest.MscorlibAssembly, this.System, this.Collections, this.Generic), this.List);
      IFieldDefinition field = Helper.GetFieldNamed(type, this._defaultCapacity);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.FieldDefinition(field);
      string result =
@".field private static literal int32 _defaultCapacity=const(4,int32)
{
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestListItemProperty() {
      ITypeDefinition type = Helper.GetNamespaceType(Helper.GetNamespace(this.ModuleReaderTest.MscorlibAssembly, this.System, this.Collections, this.Generic), this.List);
      IPropertyDefinition property = Helper.GetPropertyNamed(type, this.Item);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.PropertyDefinition(property);
      string result =
@".property public instance default!0 Item(
  int32
)
{
  .get instance!0 System.Collections.Generic.List`1::get_Item(int32)
  .set instance void System.Collections.Generic.List`1::set_Item(int32,!0)
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGCClearCacheEvent() {
      ITypeDefinition type = Helper.GetNamespaceType(Helper.GetNamespace(this.ModuleReaderTest.MscorlibAssembly, this.System), this.GC);
      IEventDefinition eventDef = Helper.GetEventNamed(type, this.ClearCache);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.EventDefinition(eventDef);
      string result =
@".event assembly System.Reflection.Cache.ClearCacheHandler ClearCache
{
  .addon static void System.GC::add_ClearCache(System.Reflection.Cache.ClearCacheHandler)
  .removeon static void System.GC::remove_ClearCache(System.Reflection.Cache.ClearCacheHandler)
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric1Event() {
      ITypeDefinition genType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Generic1);
      IEventDefinition eventDef = Helper.GetEventNamed(genType, this.GenericEvent);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.EventDefinition(eventDef);
      string result =
@".event public[MRW_Assembly]GenericDelegate`1<!0>GenericEvent
{
  .addon instance void[MRW_Assembly]Generic1`1::add_GenericEvent([MRW_Assembly]GenericDelegate`1<!0>)
  .removeon instance void[MRW_Assembly]Generic1`1::remove_GenericEvent([MRW_Assembly]GenericDelegate`1<!0>)
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestListAddMethodInstructions() {
      ITypeDefinition type = Helper.GetNamespaceType(Helper.GetNamespace(this.ModuleReaderTest.MscorlibAssembly, this.System, this.Collections, this.Generic), this.List);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.Add);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public hidebysig newslot virtual final instance explicit default void Add(
  !0  item
)cil managed
{
  .maxstack 4
  .locals init(
    int32  V_0
  )
  IL_0000:  ldarg.0
  IL_0001:  ldfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_0006:  ldarg.0
  IL_0007:  ldfld!0[]System.Collections.Generic.List`1<!0>::_items
  IL_000c:  ldlen
  IL_000d:  conv.i4
  IL_000e:  bne.un.s IL_001e
  IL_0010:  ldarg.0
  IL_0011:  ldarg.0
  IL_0012:  ldfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  call instance void System.Collections.Generic.List`1<!0>::EnsureCapacity(int32)
  IL_001e:  ldarg.0
  IL_001f:  ldfld!0[]System.Collections.Generic.List`1<!0>::_items
  IL_0024:  ldarg.0
  IL_0025:  dup
  IL_0026:  ldfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_002b:  dup
  IL_002c:  stloc.0
  IL_002d:  ldc.i4.1
  IL_002e:  add
  IL_002f:  stfld int32 System.Collections.Generic.List`1<!0>::_size
  IL_0034:  ldloc.0
  IL_0035:  ldarg.1
  IL_0036:  stelem!0
  IL_003b:  ldarg.0
  IL_003c:  dup
  IL_003d:  ldfld int32 System.Collections.Generic.List`1<!0>::_version
  IL_0042:  ldc.i4.1
  IL_0043:  add
  IL_0044:  stfld int32 System.Collections.Generic.List`1<!0>::_version
  IL_0049:  ret
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestCreateFontIndirect() {
      ITypeDefinition type = Helper.GetNamespaceType((IUnitNamespace)this.ModuleReaderTest.TestAssembly.NamespaceRoot, this.MarshalTest);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.CreateFontIndirect);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public hidebysig static pinvokeimpl(gdi32.dll as CreateFontIndirect autochar winapi)explicit default native int CreateFontIndirect(
  [in][vjslib]com.ms.win32.LOGFONT marshal(lpstruct) lplf
)cil managed preservesig
{
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestCreateFontIndirectArray() {
      ITypeDefinition type = Helper.GetNamespaceType((IUnitNamespace)this.ModuleReaderTest.TestAssembly.NamespaceRoot, this.MarshalTest);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.CreateFontIndirectArray);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public hidebysig static pinvokeimpl(gdi32.dll as CreateFontIndirectArray autochar winapi)explicit default native int CreateFontIndirectArray(
  [vjslib]com.ms.win32.LOGFONT[]marshal(lpstruct,0,1) lplf
)cil managed preservesig
{
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestAssemGenericMethod() {
      ITypeDefinition type = Helper.GetNamespaceType((IUnitNamespace)this.ModuleReaderTest.AssemblyAssembly.NamespaceRoot, this.Assem);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.GenMethod);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public hidebysig instance explicit default!!0 GenMethod<class T>(
  !!0  t,
  [mscorlib]System.Collections.Generic.List`1<!!0> l,
  !!0[] ta,
  !!0[,] tm,
  !!0[,] tn
)cil managed
{
  .maxstack 6
  .locals init(
    !!0  V_0
  )
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  ldnull
  IL_0004:  ldnull
  IL_0005:  ldnull
  IL_0006:  ldnull
  IL_0007:  call instance!!0[MRW_Assembly]Assem::GenMethod<[mscorlib]System.Object>(!!0,[mscorlib]System.Collections.Generic.List`1<!!0>,!!0[],!!0[,],!!0[,])
  IL_000c:  pop
  IL_000d:  ldarg.1
  IL_000e:  stloc.0
  IL_000f:  br.s IL_0011
  IL_0011:  ldloc.0
  IL_0012:  ret
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestAssemGenericMethodCall() {
      ITypeDefinition type = Helper.GetNamespaceType((IUnitNamespace)this.ModuleReaderTest.AssemblyAssembly.NamespaceRoot, this.Assem);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.GenMethod);
      IOperation op = Helper.GetOperation(method, 7);
      IMethodReference methodRef = op.Value as IMethodReference;
      if (methodRef == null)
        return false;
      IGenericMethodInstance gmi = methodRef.ResolvedMethod as IGenericMethodInstance;
      if (gmi == null)
        return false;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.MethodDefinition(gmi);
      string result =
@".method public hidebysig instance explicit default[mscorlib]System.Object GenMethod<[mscorlib]System.Object>(
  [mscorlib]System.Object  t,
  [mscorlib]System.Collections.Generic.List`1<[mscorlib]System.Object> l,
  [mscorlib]System.Object[] ta,
  [mscorlib]System.Object[,] tm,
  [mscorlib]System.Object[,] tn
)cil managed
{
  .maxstack 6
  .locals init(
    [mscorlib]System.Object  V_0
  )
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  ldnull
  IL_0004:  ldnull
  IL_0005:  ldnull
  IL_0006:  ldnull
  IL_0007:  call instance!!0[MRW_Assembly]Assem::GenMethod<[mscorlib]System.Object>(!!0,[mscorlib]System.Collections.Generic.List`1<!!0>,!!0[],!!0[,],!!0[,])
  IL_000c:  pop
  IL_000d:  ldarg.1
  IL_000e:  stloc.0
  IL_000f:  br.s IL_0011
  IL_0011:  ldloc.0
  IL_0012:  ret
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGenericTypeField() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic1);
      ITypeDefinition type = fld.Type.ResolvedType;
      IFieldDefinition field = Helper.GetFieldNamed(type, this.fieldT);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.FieldDefinition(field);
      string result =
@".field public int32 fieldT
{
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGenericTypeMethod() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic1);
      ITypeDefinition type = fld.Type.ResolvedType;
      IMethodDefinition method = Helper.GetMethodNamed(type, this.get_propT);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public hidebysig specialname instance explicit default int32 get_propT()cil managed
{
  .maxstack 1
  .locals init(
    int32  V_0,
    int32  V_1
  )
  IL_0000:  nop
  IL_0001:  ldloca.s V_1
  IL_0003:  initobj int32
  IL_0009:  ldloc.1
  IL_000a:  stloc.0
  IL_000b:  br.s IL_000d
  IL_000d:  ldloc.0
  IL_000e:  ret
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGenericTypeProperty() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic1);
      ITypeDefinition type = fld.Type.ResolvedType;
      IPropertyDefinition prop = Helper.GetPropertyNamed(type, this.propT);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.PropertyDefinition(prop);
      string result =
@".property public instance default int32 propT()
{
  .get instance int32[MRW_Assembly]Generic1`1<int32>::get_propT()
  .set instance void[MRW_Assembly]Generic1`1<int32>::set_propT(int32)
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGenericTypeEvent() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic1);
      ITypeDefinition type = fld.Type.ResolvedType;
      IEventDefinition eventDef = Helper.GetEventNamed(type, this.GenericEvent);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.EventDefinition(eventDef);
      string result =
@".event public[MRW_Assembly]GenericDelegate`1<int32>GenericEvent
{
  .addon instance void[MRW_Assembly]Generic1`1<int32>::add_GenericEvent([MRW_Assembly]GenericDelegate`1<int32>)
  .removeon instance void[MRW_Assembly]Generic1`1<int32>::remove_GenericEvent([MRW_Assembly]GenericDelegate`1<int32>)
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestfreeMethod() {
      IMethodDefinition method = Helper.GetGlobalMethod((IUnitNamespace)this.ModuleReaderTest.CppAssembly.NamespaceRoot, this.free);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.CppAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public static pinvokeimpl(MSVCR80.dll as free cdecl lasterr)explicit default void modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl)free(
  void*
)cil managed preservesig
{
  .custom instance void[mscorlib]System.Security.SuppressUnmanagedCodeSecurityAttribute::.ctor()
  {
  }
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestPtrToStringCharsMethod() {
      IMethodDefinition method = Helper.GetGlobalMethod((IUnitNamespace)this.ModuleReaderTest.CppAssembly.NamespaceRoot, this.PtrToStringChars);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.CppAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method assembly static explicit default char modopt([mscorlib]System.Runtime.CompilerServices.IsConst)&modopt([mscorlib]System.Runtime.CompilerServices.IsExplicitlyDereferenced)PtrToStringChars(
  [mscorlib]System.String modopt([mscorlib]System.Runtime.CompilerServices.IsConst) s
)cil managed
{
  .maxstack 2
  .locals(
    unsigned int8&modopt([mscorlib]System.Runtime.CompilerServices.IsExplicitlyDereferenced) V_0
  )
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  brfalse.s IL_000d
  IL_0005:  call static int32[mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::get_OffsetToStringData()
  IL_000a:  ldloc.0
  IL_000b:  add
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ret
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGenMethodType() {
      ITypeDefinition type = Helper.GetNamespaceType(this.ModuleReaderTest.ILAsmAssembly.UnitNamespaceRoot, this.Generic);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.GenMethod);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.ILAsmAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method public hidebysig instance explicit default!!0 GenMethod<class T>(
  !!0  t,
  !!0 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* tp
)cil managed
{
  .maxstack 3
  .locals init(
    !!0  V_0
  )
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  ldc.i4.0
  IL_0004:  call instance!!0 Generic`1::GenMethod<[mscorlib]System.Object>(!!0,!!0 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*)
  IL_0009:  pop
  IL_000a:  ldarg.1
  IL_000b:  stloc.0
  IL_000c:  br.s IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestPhxPointerGlobalField() {
      IFieldDefinition field = Helper.GetGlobalField(Helper.GetNamespace(this.ModuleReaderTest.PhxArchMsil, this._A0x8179e609), this._InitializedPerProcess_initializer__CurrentDomain__CrtImplementationDetails_____Q2P6MXXZEA);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.CppAssembly);
      prettyPrinter.FieldDefinition(field);
      string result =
@".field assembly static void()?A0x8179e609.?InitializedPerProcess$initializer$@CurrentDomain@<CrtImplementationDetails>@@$$Q2P6MXXZEA at{0x00000058,0x00000004}.data=(78 00 00 06 )                                     // x...
{
}
";
      return result.Equals(stringPaper.Content);
    }
  }
}
