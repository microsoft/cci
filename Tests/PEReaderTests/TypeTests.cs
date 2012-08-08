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
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci;

namespace ModuleReaderTests {
  public class TypeTests {
    readonly ModuleReaderTestClass ModuleReaderTest;
    readonly IName System;
    readonly IName Collections;
    readonly IName Generic;
    readonly IName List;
    readonly IName Assem;
    readonly IName Generic1;
    readonly IName Generic2;
    readonly IName Generic3;
    readonly IName Generic4;
    readonly IName Nested;
    readonly IName Generic1Nested;
    readonly IName Generic2Nested;
    readonly IName Generic3Nested;
    readonly IName Foo;
    readonly IName Bar;
    readonly IName FooNested;
    readonly IName TypeTest;
    readonly IName foo;
    readonly IName bar;
    readonly IName field1;
    readonly IName field2;
    readonly IName GenMethod;
    readonly IName IntList;

    public TypeTests(ModuleReaderTestClass mrTest) {
      this.ModuleReaderTest = mrTest;
      this.System = mrTest.NameTable.GetNameFor("System");
      this.Collections = mrTest.NameTable.GetNameFor("Collections");
      this.Generic = mrTest.NameTable.GetNameFor("Generic");
      this.List = mrTest.NameTable.GetNameFor("List");
      this.Assem = mrTest.NameTable.GetNameFor("Assem");
      this.Generic1 = mrTest.NameTable.GetNameFor("Generic1");
      this.Generic2 = mrTest.NameTable.GetNameFor("Generic2");
      this.Generic3 = mrTest.NameTable.GetNameFor("Generic3");
      this.Generic4 = mrTest.NameTable.GetNameFor("Generic4");
      this.Nested = mrTest.NameTable.GetNameFor("Nested");
      this.Generic1Nested = mrTest.NameTable.GetNameFor("Generic1Nested");
      this.Generic2Nested = mrTest.NameTable.GetNameFor("Generic2Nested");
      this.Generic3Nested = mrTest.NameTable.GetNameFor("Generic3Nested");
      this.Foo = mrTest.NameTable.GetNameFor("Foo");
      this.Bar = mrTest.NameTable.GetNameFor("Bar");
      this.FooNested = mrTest.NameTable.GetNameFor("FooNested");
      this.TypeTest = mrTest.NameTable.GetNameFor("TypeTest");
      this.foo = mrTest.NameTable.GetNameFor("foo");
      this.bar = mrTest.NameTable.GetNameFor("bar");
      this.field1 = mrTest.NameTable.GetNameFor("field1");
      this.field2 = mrTest.NameTable.GetNameFor("field2");
      this.GenMethod = mrTest.NameTable.GetNameFor("GenMethod");
      this.IntList = mrTest.NameTable.GetNameFor("IntList");
    }

    public bool RunTypeTests() {
      bool ret = true;
      if (!this.TestMscorlibList1()) {
        Console.WriteLine("TestMscorlibList1 - Failed");
        ret = false;
      }
      if (!this.TestGeneric1FieldType()) {
        Console.WriteLine("TestGeneric1FieldType - Failed");
        ret = false;
      }
      if (!this.TestGeneric1NestedFieldType()) {
        Console.WriteLine("TestGeneric1NestedFieldType - Failed");
        ret = false;
      }
      if (!this.TestGeneric2FieldType()) {
        Console.WriteLine("TestGeneric2FieldType - Failed");
        ret = false;
      }
      if (!this.TestGeneric2NestedFieldType()) {
        Console.WriteLine("TestGeneric2NestedFieldType - Failed");
        ret = false;
      }
      if (!this.TestGeneric3FieldType()) {
        Console.WriteLine("TestGeneric3FieldType - Failed");
        ret = false;
      }
      if (!this.TestGeneric3NestedFieldType()) {
        Console.WriteLine("TestGeneric3NestedFieldType - Failed");
        ret = false;
      }
      if (!this.TestGeneric4()) {
        Console.WriteLine("TestGeneric4 - Failed");
        ret = false;
      }
      if (!this.TestGeneric4FieldType()) {
        Console.WriteLine("TestGeneric4FieldType - Failed");
        ret = false;
      }
      if (!this.TestGeneric1FieldTypeNestedType()) {
        Console.WriteLine("TestGeneric1FieldTypeNestedType - Failed");
        ret = false;
      }
      if (!this.TestGeneric2FieldTypeNestedType()) {
        Console.WriteLine("TestGeneric2FieldTypeNestedType - Failed");
        ret = false;
      }
      if (!this.TestGeneric3FieldTypeNestedType()) {
        Console.WriteLine("TestGeneric3FieldTypeNestedType - Failed");
        ret = false;
      }
      if (!this.TestAssemblyExportedTypeReference()) {
        Console.WriteLine("TestAssemblyExportedTypeReference - Failed");
        ret = false;
      }
      if (!this.TestAssemblyExportedNestedTypeReference()) {
        Console.WriteLine("TestAssemblyExportedNestedTypeReference - Failed");
        ret = false;
      }
      if (!this.TestCurrentExportedTypeReference()) {
        Console.WriteLine("TestCurrentExportedTypeReference - Failed");
        ret = false;
      }
      if (!this.TestCurrentExportedNestedTypeReference()) {
        Console.WriteLine("TestCurrentExportedNestedTypeReference - Failed");
        ret = false;
      }
      if (!this.TestCppFunctionFooPointer()) {
        Console.WriteLine("TestCppFunctionFooPointer - Failed");
        ret = false;
      }
      if (!this.TestCppFunctionBarPointer()) {
        Console.WriteLine("TestCppFunctionBarPointer - Failed");
        ret = false;
      }
      if (!this.TestTypeTest()) {
        Console.WriteLine("TestTypeTest - Failed");
        ret = false;
      }
      if (!this.TestGeneric4Instance()) {
        Console.WriteLine("TestGeneric4Instance - Failed");
        ret = false;
      }
      if (!this.TestGeneric2NestedInstance()) {
        Console.WriteLine("TestGeneric2NestedInstance - Failed");
        ret = false;
      }
      if (!this.TestGenericType()) {
        Console.WriteLine("TestGenericType - Failed");
        ret = false;
      }
      if (!this.TestGenericInstfield1Type()) {
        Console.WriteLine("TestGenericInstfield1Type - Failed");
        ret = false;
      }
      if (!this.TestListInstType()) {
        Console.WriteLine("TestListInstType - Failed");
        ret = false;
      }
      return ret;
    }

    public bool TestMscorlibList1() {
      ITypeDefinition type = Helper.GetNamespaceType(Helper.GetNamespace(this.ModuleReaderTest.MscorlibAssembly, this.System, this.Collections, this.Generic), this.List);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class public auto ansi serializable beforefieldinit System.Collections.Generic.List`1<T>
  extends  System.Object
  implements  System.Collections.Generic.IList`1<!0>,
              System.Collections.Generic.ICollection`1<!0>,
              System.Collections.Generic.IEnumerable`1<!0>,
              System.Collections.IList,
              System.Collections.ICollection,
              System.Collections.IEnumerable
{
  .custom instance void System.Reflection.DefaultMemberAttribute::.ctor(System.String)
  {
    .argument const(""Item"",System.String)
  }
  .custom instance void System.Diagnostics.DebuggerDisplayAttribute::.ctor(System.String)
  {
    .argument const(""Count = {Count}"",System.String)
  }
  .custom instance void System.Diagnostics.DebuggerTypeProxyAttribute::.ctor(System.Type)
  {
    .argument typeof(System.Collections.Generic.Mscorlib_CollectionDebugView`1)
  }
  .class Enumerator
  .field _defaultCapacity : int32
  .field _emptyArray : !0[]
  .field _items : !0[]
  .field _size : int32
  .field _syncRoot : System.Object
  .field _version : int32
  .method .cctor : void()
  .method .ctor : void()
  .method .ctor : void(int32)
  .method .ctor : void(System.Collections.Generic.IEnumerable`1<!0>)
  .method Add : void(!0)
  .method AddRange : void(System.Collections.Generic.IEnumerable`1<!0>)
  .method AsReadOnly : System.Collections.ObjectModel.ReadOnlyCollection`1<!0>()
  .method BinarySearch : int32(!0)
  .method BinarySearch : int32(!0,System.Collections.Generic.IComparer`1<!0>)
  .method BinarySearch : int32(int32,int32,!0,System.Collections.Generic.IComparer`1<!0>)
  .method Clear : void()
  .method Contains : bool(!0)
  .method ConvertAll : System.Collections.Generic.List`1<!!0>(System.Converter`2<!0,!!0>)
  .method CopyTo : void(!0[])
  .method CopyTo : void(!0[],int32)
  .method CopyTo : void(int32,!0[],int32,int32)
  .method EnsureCapacity : void(int32)
  .method Exists : bool(System.Predicate`1<!0>)
  .method Find : !0(System.Predicate`1<!0>)
  .method FindAll : System.Collections.Generic.List`1<!0>(System.Predicate`1<!0>)
  .method FindIndex : int32(int32,int32,System.Predicate`1<!0>)
  .method FindIndex : int32(int32,System.Predicate`1<!0>)
  .method FindIndex : int32(System.Predicate`1<!0>)
  .method FindLast : !0(System.Predicate`1<!0>)
  .method FindLastIndex : int32(int32,int32,System.Predicate`1<!0>)
  .method FindLastIndex : int32(int32,System.Predicate`1<!0>)
  .method FindLastIndex : int32(System.Predicate`1<!0>)
  .method ForEach : void(System.Action`1<!0>)
  .method get_Capacity : int32()
  .method get_Count : int32()
  .method get_Item : !0(int32)
  .method GetEnumerator : System.Collections.Generic.List`1<!0>/Enumerator()
  .method GetRange : System.Collections.Generic.List`1<!0>(int32,int32)
  .method IndexOf : int32(!0)
  .method IndexOf : int32(!0,int32)
  .method IndexOf : int32(!0,int32,int32)
  .method Insert : void(int32,!0)
  .method InsertRange : void(int32,System.Collections.Generic.IEnumerable`1<!0>)
  .method IsCompatibleObject : bool(System.Object)
  .method LastIndexOf : int32(!0)
  .method LastIndexOf : int32(!0,int32)
  .method LastIndexOf : int32(!0,int32,int32)
  .method Remove : bool(!0)
  .method RemoveAll : int32(System.Predicate`1<!0>)
  .method RemoveAt : void(int32)
  .method RemoveRange : void(int32,int32)
  .method Reverse : void()
  .method Reverse : void(int32,int32)
  .method set_Capacity : void(int32)
  .method set_Item : void(int32,!0)
  .method Sort : void()
  .method Sort : void(int32,int32,System.Collections.Generic.IComparer`1<!0>)
  .method Sort : void(System.Collections.Generic.IComparer`1<!0>)
  .method Sort : void(System.Comparison`1<!0>)
  .method System.Collections.Generic.ICollection<T>.get_IsReadOnly : bool()
  .method System.Collections.Generic.IEnumerable<T>.GetEnumerator : System.Collections.Generic.IEnumerator`1<!0>()
  .method System.Collections.ICollection.CopyTo : void(System.Array,int32)
  .method System.Collections.ICollection.get_IsSynchronized : bool()
  .method System.Collections.ICollection.get_SyncRoot : System.Object()
  .method System.Collections.IEnumerable.GetEnumerator : System.Collections.IEnumerator()
  .method System.Collections.IList.Add : int32(System.Object)
  .method System.Collections.IList.Contains : bool(System.Object)
  .method System.Collections.IList.get_IsFixedSize : bool()
  .method System.Collections.IList.get_IsReadOnly : bool()
  .method System.Collections.IList.get_Item : System.Object(int32)
  .method System.Collections.IList.IndexOf : int32(System.Object)
  .method System.Collections.IList.Insert : void(int32,System.Object)
  .method System.Collections.IList.Remove : void(System.Object)
  .method System.Collections.IList.set_Item : void(int32,System.Object)
  .method ToArray : !0[]()
  .method TrimExcess : void()
  .method TrueForAll : bool(System.Predicate`1<!0>)
  .method VerifyValueType : void(System.Object)
  .property Capacity : int32()
  .property Count : int32()
  .property Item : !0(int32)
  .property System.Collections.Generic.ICollection<T>.IsReadOnly : bool()
  .property System.Collections.ICollection.IsSynchronized : bool()
  .property System.Collections.ICollection.SyncRoot : System.Object()
  .property System.Collections.IList.IsFixedSize : bool()
  .property System.Collections.IList.IsReadOnly : bool()
  .property System.Collections.IList.Item : System.Object(int32)
  .flags class reftype
  .pack 0
  .size 0
  .override instance bool System.Collections.IList::Contains(System.Object) with instance bool System.Collections.Generic.List`1::System.Collections.IList.Contains(System.Object)
  .override instance bool System.Collections.Generic.ICollection`1<!0>::get_IsReadOnly() with instance bool System.Collections.Generic.List`1::System.Collections.Generic.ICollection<T>.get_IsReadOnly()
  .override instance bool System.Collections.IList::get_IsReadOnly() with instance bool System.Collections.Generic.List`1::System.Collections.IList.get_IsReadOnly()
  .override instance bool System.Collections.ICollection::get_IsSynchronized() with instance bool System.Collections.Generic.List`1::System.Collections.ICollection.get_IsSynchronized()
  .override instance System.Object System.Collections.ICollection::get_SyncRoot() with instance System.Object System.Collections.Generic.List`1::System.Collections.ICollection.get_SyncRoot()
  .override instance System.Object System.Collections.IList::get_Item(int32) with instance System.Object System.Collections.Generic.List`1::System.Collections.IList.get_Item(int32)
  .override instance void System.Collections.IList::set_Item(int32,System.Object) with instance void System.Collections.Generic.List`1::System.Collections.IList.set_Item(int32,System.Object)
  .override instance int32 System.Collections.IList::Add(System.Object) with instance int32 System.Collections.Generic.List`1::System.Collections.IList.Add(System.Object)
  .override instance bool System.Collections.IList::get_IsFixedSize() with instance bool System.Collections.Generic.List`1::System.Collections.IList.get_IsFixedSize()
  .override instance void System.Collections.ICollection::CopyTo(System.Array,int32) with instance void System.Collections.Generic.List`1::System.Collections.ICollection.CopyTo(System.Array,int32)
  .override instance System.Collections.Generic.IEnumerator`1<!0>System.Collections.Generic.IEnumerable`1<!0>::GetEnumerator() with instance System.Collections.Generic.IEnumerator`1<!0>System.Collections.Generic.List`1::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
  .override instance System.Collections.IEnumerator System.Collections.IEnumerable::GetEnumerator() with instance System.Collections.IEnumerator System.Collections.Generic.List`1::System.Collections.IEnumerable.GetEnumerator()
  .override instance int32 System.Collections.IList::IndexOf(System.Object) with instance int32 System.Collections.Generic.List`1::System.Collections.IList.IndexOf(System.Object)
  .override instance void System.Collections.IList::Insert(int32,System.Object) with instance void System.Collections.Generic.List`1::System.Collections.IList.Insert(int32,System.Object)
  .override instance void System.Collections.IList::Remove(System.Object) with instance void System.Collections.Generic.List`1::System.Collections.IList.Remove(System.Object)
}
";
      string vistaResult =
@".class public auto ansi serializable beforefieldinit System.Collections.Generic.List`1<T>
  extends  System.Object
  implements  System.Collections.Generic.IList`1<!0>,
              System.Collections.Generic.ICollection`1<!0>,
              System.Collections.Generic.IEnumerable`1<!0>,
              System.Collections.IList,
              System.Collections.ICollection,
              System.Collections.IEnumerable
{
  .custom instance void System.Diagnostics.DebuggerDisplayAttribute::.ctor(System.String)
  {
    .argument const(""Count = {Count}"",System.String)
  }
  .custom instance void System.Diagnostics.DebuggerTypeProxyAttribute::.ctor(System.Type)
  {
    .argument typeof(System.Collections.Generic.Mscorlib_CollectionDebugView`1)
  }
  .custom instance void System.Reflection.DefaultMemberAttribute::.ctor(System.String)
  {
    .argument const(""Item"",System.String)
  }
  .class Enumerator
  .field _defaultCapacity : int32
  .field _emptyArray : !0[]
  .field _items : !0[]
  .field _size : int32
  .field _syncRoot : System.Object
  .field _version : int32
  .method .cctor : void()
  .method .ctor : void()
  .method .ctor : void(int32)
  .method .ctor : void(System.Collections.Generic.IEnumerable`1<!0>)
  .method Add : void(!0)
  .method AddRange : void(System.Collections.Generic.IEnumerable`1<!0>)
  .method AsReadOnly : System.Collections.ObjectModel.ReadOnlyCollection`1<!0>()
  .method BinarySearch : int32(!0)
  .method BinarySearch : int32(!0,System.Collections.Generic.IComparer`1<!0>)
  .method BinarySearch : int32(int32,int32,!0,System.Collections.Generic.IComparer`1<!0>)
  .method Clear : void()
  .method Contains : bool(!0)
  .method ConvertAll : System.Collections.Generic.List`1<!!0>(System.Converter`2<!0,!!0>)
  .method CopyTo : void(!0[])
  .method CopyTo : void(!0[],int32)
  .method CopyTo : void(int32,!0[],int32,int32)
  .method EnsureCapacity : void(int32)
  .method Exists : bool(System.Predicate`1<!0>)
  .method Find : !0(System.Predicate`1<!0>)
  .method FindAll : System.Collections.Generic.List`1<!0>(System.Predicate`1<!0>)
  .method FindIndex : int32(int32,int32,System.Predicate`1<!0>)
  .method FindIndex : int32(int32,System.Predicate`1<!0>)
  .method FindIndex : int32(System.Predicate`1<!0>)
  .method FindLast : !0(System.Predicate`1<!0>)
  .method FindLastIndex : int32(int32,int32,System.Predicate`1<!0>)
  .method FindLastIndex : int32(int32,System.Predicate`1<!0>)
  .method FindLastIndex : int32(System.Predicate`1<!0>)
  .method ForEach : void(System.Action`1<!0>)
  .method get_Capacity : int32()
  .method get_Count : int32()
  .method get_Item : !0(int32)
  .method GetEnumerator : System.Collections.Generic.List`1<!0>/Enumerator()
  .method GetRange : System.Collections.Generic.List`1<!0>(int32,int32)
  .method IndexOf : int32(!0)
  .method IndexOf : int32(!0,int32)
  .method IndexOf : int32(!0,int32,int32)
  .method Insert : void(int32,!0)
  .method InsertRange : void(int32,System.Collections.Generic.IEnumerable`1<!0>)
  .method IsCompatibleObject : bool(System.Object)
  .method LastIndexOf : int32(!0)
  .method LastIndexOf : int32(!0,int32)
  .method LastIndexOf : int32(!0,int32,int32)
  .method Remove : bool(!0)
  .method RemoveAll : int32(System.Predicate`1<!0>)
  .method RemoveAt : void(int32)
  .method RemoveRange : void(int32,int32)
  .method Reverse : void()
  .method Reverse : void(int32,int32)
  .method set_Capacity : void(int32)
  .method set_Item : void(int32,!0)
  .method Sort : void()
  .method Sort : void(int32,int32,System.Collections.Generic.IComparer`1<!0>)
  .method Sort : void(System.Collections.Generic.IComparer`1<!0>)
  .method Sort : void(System.Comparison`1<!0>)
  .method System.Collections.Generic.ICollection<T>.get_IsReadOnly : bool()
  .method System.Collections.Generic.IEnumerable<T>.GetEnumerator : System.Collections.Generic.IEnumerator`1<!0>()
  .method System.Collections.ICollection.CopyTo : void(System.Array,int32)
  .method System.Collections.ICollection.get_IsSynchronized : bool()
  .method System.Collections.ICollection.get_SyncRoot : System.Object()
  .method System.Collections.IEnumerable.GetEnumerator : System.Collections.IEnumerator()
  .method System.Collections.IList.Add : int32(System.Object)
  .method System.Collections.IList.Contains : bool(System.Object)
  .method System.Collections.IList.get_IsFixedSize : bool()
  .method System.Collections.IList.get_IsReadOnly : bool()
  .method System.Collections.IList.get_Item : System.Object(int32)
  .method System.Collections.IList.IndexOf : int32(System.Object)
  .method System.Collections.IList.Insert : void(int32,System.Object)
  .method System.Collections.IList.Remove : void(System.Object)
  .method System.Collections.IList.set_Item : void(int32,System.Object)
  .method ToArray : !0[]()
  .method TrimExcess : void()
  .method TrueForAll : bool(System.Predicate`1<!0>)
  .method VerifyValueType : void(System.Object)
  .property Capacity : int32()
  .property Count : int32()
  .property Item : !0(int32)
  .property System.Collections.Generic.ICollection<T>.IsReadOnly : bool()
  .property System.Collections.ICollection.IsSynchronized : bool()
  .property System.Collections.ICollection.SyncRoot : System.Object()
  .property System.Collections.IList.IsFixedSize : bool()
  .property System.Collections.IList.IsReadOnly : bool()
  .property System.Collections.IList.Item : System.Object(int32)
  .flags class reftype
  .pack 0
  .size 0
  .override instance bool System.Collections.IList::Contains(System.Object) with instance bool System.Collections.Generic.List`1::System.Collections.IList.Contains(System.Object)
  .override instance bool System.Collections.Generic.ICollection`1<!0>::get_IsReadOnly() with instance bool System.Collections.Generic.List`1::System.Collections.Generic.ICollection<T>.get_IsReadOnly()
  .override instance bool System.Collections.IList::get_IsReadOnly() with instance bool System.Collections.Generic.List`1::System.Collections.IList.get_IsReadOnly()
  .override instance bool System.Collections.ICollection::get_IsSynchronized() with instance bool System.Collections.Generic.List`1::System.Collections.ICollection.get_IsSynchronized()
  .override instance System.Object System.Collections.ICollection::get_SyncRoot() with instance System.Object System.Collections.Generic.List`1::System.Collections.ICollection.get_SyncRoot()
  .override instance System.Object System.Collections.IList::get_Item(int32) with instance System.Object System.Collections.Generic.List`1::System.Collections.IList.get_Item(int32)
  .override instance void System.Collections.IList::set_Item(int32,System.Object) with instance void System.Collections.Generic.List`1::System.Collections.IList.set_Item(int32,System.Object)
  .override instance int32 System.Collections.IList::Add(System.Object) with instance int32 System.Collections.Generic.List`1::System.Collections.IList.Add(System.Object)
  .override instance bool System.Collections.IList::get_IsFixedSize() with instance bool System.Collections.Generic.List`1::System.Collections.IList.get_IsFixedSize()
  .override instance void System.Collections.ICollection::CopyTo(System.Array,int32) with instance void System.Collections.Generic.List`1::System.Collections.ICollection.CopyTo(System.Array,int32)
  .override instance System.Collections.Generic.IEnumerator`1<!0>System.Collections.Generic.IEnumerable`1<!0>::GetEnumerator() with instance System.Collections.Generic.IEnumerator`1<!0>System.Collections.Generic.List`1::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
  .override instance System.Collections.IEnumerator System.Collections.IEnumerable::GetEnumerator() with instance System.Collections.IEnumerator System.Collections.Generic.List`1::System.Collections.IEnumerable.GetEnumerator()
  .override instance int32 System.Collections.IList::IndexOf(System.Object) with instance int32 System.Collections.Generic.List`1::System.Collections.IList.IndexOf(System.Object)
  .override instance void System.Collections.IList::Insert(int32,System.Object) with instance void System.Collections.Generic.List`1::System.Collections.IList.Insert(int32,System.Object)
  .override instance void System.Collections.IList::Remove(System.Object) with instance void System.Collections.Generic.List`1::System.Collections.IList.Remove(System.Object)
}
";
      return result.Equals(stringPaper.Content) || vistaResult.Equals(stringPaper.Content);
    }

    public bool TestGeneric1FieldType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic1);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi beforefieldinit Generic1`1<int32>
  extends [mscorlib]System.Object
  implements  IGen1`1<int32>
{
  .class Nested
  .event GenericEvent : GenericDelegate`1<int32>
  .field fieldT : int32
  .field GenericEvent : GenericDelegate`1<int32>
  .method .ctor : void()
  .method add_GenericEvent : void(GenericDelegate`1<int32>)
  .method get_propT : int32()
  .method remove_GenericEvent : void(GenericDelegate`1<int32>)
  .method set_propT : void(int32)
  .property propT : int32()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric1NestedFieldType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic1Nested);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi nested public beforefieldinit Generic1`1<int32>/Nested
  extends [mscorlib]System.Object
  implements  IGen1`1<int32>
{
  .field fieldT : int32
  .method .ctor : void()
  .method get_propT : int32()
  .method set_propT : void(int32)
  .property propT : int32()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric2FieldType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic2);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi beforefieldinit Generic2`1<int32>
  extends [mscorlib]System.Object
  implements  IGen1`1<int32>
{
  .class Nested
  .field fieldT : int32
  .method .ctor : void()
  .method get_propT : int32()
  .method set_propT : void(int32)
  .property propT : int32()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric2NestedFieldType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic2Nested);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi beforefieldinit Generic2`1<[mscorlib]System.Object>/Nested`1<float32>
  extends [mscorlib]System.Object
  implements  IGen2`2<[mscorlib]System.Object,float32>
{
  .field fieldT : [mscorlib]System.Object
  .field fieldU : float32
  .method .ctor : void()
  .method get_propT : [mscorlib]System.Object()
  .method get_propU : float32()
  .method Meth : float32(IGen1`1<[mscorlib]System.Object>,!!0)
  .method set_propT : void([mscorlib]System.Object)
  .method set_propU : void(float32)
  .property propT : [mscorlib]System.Object()
  .property propU : float32()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric3FieldType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic3);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class public auto ansi beforefieldinit Generic3
  extends [mscorlib]System.Object
{
  .class Nested
  .method .ctor : void()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric3NestedFieldType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic3Nested);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi beforefieldinit Generic3/Nested`1<float32>
  extends [mscorlib]System.Object
  implements  IGen1`1<float32>
{
  .field fieldU : float32
  .method .ctor : void()
  .method get_propU : float32()
  .method set_propU : void(float32)
  .property propU : float32()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric4() {
      ITypeDefinition type = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Generic4);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class public auto ansi beforefieldinit Generic4`1<valuetype .ctor([mscorlib]System.ValueType) T>
  extends [mscorlib]System.Object
{
  .field field1 : !0
  .field field2 : !0[]
  .field field3 : int32*
  .field field4 : !0[,]
  .method .ctor : void()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric4FieldType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic4);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi beforefieldinit Generic4`1<int32>
  extends [mscorlib]System.Object
{
  .field field1 : int32
  .field field2 : int32[]
  .field field3 : int32*
  .field field4 : int32[,]
  .method .ctor : void()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric1FieldTypeNestedType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic1);
      ITypeDefinition type = Helper.GetNestedType(fld.Type.ResolvedType, this.Nested);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi nested public beforefieldinit Generic1`1<int32>/Nested
  extends [mscorlib]System.Object
  implements  IGen1`1<int32>
{
  .field fieldT : int32
  .method .ctor : void()
  .method get_propT : int32()
  .method set_propT : void(int32)
  .property propT : int32()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric2FieldTypeNestedType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic2);
      ITypeDefinition type = Helper.GetNestedType(fld.Type.ResolvedType, this.Nested);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi nested public beforefieldinit Generic2`1<int32>/Nested`1<(int32) U>
  extends [mscorlib]System.Object
  implements  IGen2`2<int32,!0>
{
  .field fieldT : int32
  .field fieldU : !0
  .method .ctor : void()
  .method get_propT : int32()
  .method get_propU : !0()
  .method Meth : !0(IGen1`1<int32>,!!0)
  .method set_propT : void(int32)
  .method set_propU : void(!0)
  .property propT : int32()
  .property propU : !0()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric3FieldTypeNestedType() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Generic3);
      ITypeDefinition type = Helper.GetNestedType(fld.Type.ResolvedType, this.Nested);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi nested public beforefieldinit Generic3/Nested`1<U>
  extends [mscorlib]System.Object
  implements  IGen1`1<!0>
{
  .field fieldU : !0
  .method .ctor : void()
  .method get_propU : !0()
  .method set_propU : void(!0)
  .property propU : !0()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestAssemblyExportedTypeReference() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.Foo);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class public auto ansi beforefieldinit Module1.Foo
  extends [mscorlib]System.Object
{
  .class Nested
  .method .ctor : void()
  .method Bar : [.module MRW_Module1.netmodule]Module1.Foo()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestAssemblyExportedNestedTypeReference() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Assem);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.FooNested);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi nested public beforefieldinit Module1.Foo/Nested
  extends [mscorlib]System.Object
{
  .method .ctor : void()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestCurrentExportedTypeReference() {
      ITypeDefinition testType = Helper.GetNamespaceType(this.ModuleReaderTest.TestAssembly.UnitNamespaceRoot, this.TypeTest);
      IFieldDefinition fld = Helper.GetFieldNamed(testType, this.Foo);
      if (!fld.Type.IsAlias)
        return false;
      IAliasForType alias = fld.Type.AliasForType;
      ITypeDefinition type = alias.AliasedType.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class public auto ansi beforefieldinit[MRW_Assembly]Module1.Foo
  extends [mscorlib]System.Object
{
  .class Nested
  .method .ctor : void()
  .method Bar : [.module MRW_Module1.netmodule]Module1.Foo()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestCurrentExportedNestedTypeReference() {
      ITypeDefinition testType = Helper.GetNamespaceType(this.ModuleReaderTest.TestAssembly.UnitNamespaceRoot, this.TypeTest);
      IFieldDefinition fld = Helper.GetFieldNamed(testType, this.FooNested);
      if (!fld.Type.IsAlias)
        return false;
      IAliasForType alias = fld.Type.AliasForType;
      ITypeDefinition type = alias.AliasedType.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi nested public beforefieldinit[MRW_Assembly]Module1.Foo/Nested
  extends [mscorlib]System.Object
{
  .method .ctor : void()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestCppFunctionFooPointer() {
      IGlobalFieldDefinition globalField = Helper.GetGlobalField(Helper.GetNamespace(this.ModuleReaderTest.CppAssembly, this.Foo), this.foo);
      ITypeDefinition type = globalField.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto autochar void*(int32,float32)
{
  .flags
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestCppFunctionBarPointer() {
      IGlobalFieldDefinition globalField = Helper.GetGlobalField(Helper.GetNamespace(this.ModuleReaderTest.CppAssembly, this.Foo), this.bar);
      ITypeDefinition type = globalField.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto autochar void*(int32,float32)
{
  .flags
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestTypeTest() {
      ITypeDefinition type = Helper.GetNamespaceType(this.ModuleReaderTest.TestAssembly.UnitNamespaceRoot, this.TypeTest);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class public auto ansi beforefieldinit TypeTest
  extends [mscorlib]System.Object
{
  .custom instance void DAttribute::.ctor([mscorlib]System.String,int32,float64,[mscorlib]System.Type,TestEnum)
  {
    .argument const(""DDD"",[mscorlib]System.String)
    .argument const(666,int32)
    .argument const(666.666,float64)
    .argument typeof(TestEnum)
    .argument const(0,TestEnum)
    .argument .field Array : [mscorlib]System.Object[]=array([mscorlib]System.Object[]){const(null,[mscorlib]System.String), const(null,[mscorlib]System.String)}
    .argument .field IntArray : int32[]=array(int32[]){const(6,int32), const(6,int32), const(6,int32)}
  }
  .custom instance void TestAttribute::.ctor([mscorlib]System.Type)
  {
    .argument typeof(int32[,][])
  }
  .custom instance void TestAttribute::.ctor([mscorlib]System.Type)
  {
    .argument typeof(float32**)
  }
  .custom instance void TestAttribute::.ctor([mscorlib]System.Type)
  {
    .argument typeof(Faa`1<TestAttribute>/Baa`1<int32>)
  }
  .field Foo : [.module MRW_Module1.netmodule]Module1.Foo
  .field FooNested : [.module MRW_Module1.netmodule]Module1.Foo/Nested
  .field IntList : [mscorlib]System.Collections.Generic.List`1<int32>
  .method .ctor : void()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric4Instance() {
      ITypeDefinition type = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Generic4);
      type = type.InstanceType.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi beforefieldinit Generic4`1<!0>
  extends [mscorlib]System.Object
{
  .field field1 : !0
  .field field2 : !0[]
  .field field3 : int32*
  .field field4 : !0[,]
  .method .ctor : void()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGeneric2NestedInstance() {
      ITypeDefinition type = Helper.GetNamespaceType(this.ModuleReaderTest.AssemblyAssembly.UnitNamespaceRoot, this.Generic2);
      type = Helper.GetNestedType(type, this.Nested);
      type = type.InstanceType.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi beforefieldinit Generic2`1<!0>/Nested`1<!0>
  extends [mscorlib]System.Object
  implements  IGen2`2<!0,!0>
{
  .field fieldT : !0
  .field fieldU : !0
  .method .ctor : void()
  .method get_propT : !0()
  .method get_propU : !0()
  .method Meth : !0(IGen1`1<!0>,!!0)
  .method set_propT : void(!0)
  .method set_propU : void(!0)
  .property propT : !0()
  .property propU : !0()
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGenericType() {
      ITypeDefinition type = Helper.GetNamespaceType(this.ModuleReaderTest.ILAsmAssembly.UnitNamespaceRoot, this.Generic);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.ILAsmAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class public auto ansi beforefieldinit Generic`1<T>
  extends [mscorlib]System.Object
{
  .custom instance void TestAttribute1::.ctor([mscorlib]System.Type)
  {
    .argument typeof(float32*&)
  }
  .field field1 : !0*
  .field field2 : Generic`1<[mscorlib]System.Object>
  .method GenMethod : !!0(!!0,!!0*)
  .flags class reftype
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestGenericInstfield1Type() {
      ITypeDefinition assemType = Helper.GetNamespaceType(this.ModuleReaderTest.ILAsmAssembly.UnitNamespaceRoot, this.Generic);
      IFieldDefinition fld = Helper.GetFieldNamed(assemType, this.field2);
      ITypeDefinition type = fld.Type.ResolvedType;
      fld = Helper.GetFieldNamed(type, this.field1);
      type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.ILAsmAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto autochar[mscorlib]System.Object modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*
{
  .flags
  .pack 0
  .size 0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestListInstType() {
      ITypeDefinition testType = Helper.GetNamespaceType(this.ModuleReaderTest.TestAssembly.UnitNamespaceRoot, this.TypeTest);
      IFieldDefinition fld = Helper.GetFieldNamed(testType, this.IntList);
      ITypeDefinition type = fld.Type.ResolvedType;
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.ILAsmAssembly);
      prettyPrinter.TypeDefinition(type);
      string result =
@".class auto ansi serializable beforefieldinit[mscorlib]System.Collections.Generic.List`1<int32>
  extends [mscorlib]System.Object
  implements [mscorlib]System.Collections.Generic.IList`1<int32>,
             [mscorlib]System.Collections.Generic.ICollection`1<int32>,
             [mscorlib]System.Collections.Generic.IEnumerable`1<int32>,
             [mscorlib]System.Collections.IList,
             [mscorlib]System.Collections.ICollection,
             [mscorlib]System.Collections.IEnumerable
{
  .custom instance void[mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor([mscorlib]System.String)
  {
    .argument const(""Count = {Count}"",[mscorlib]System.String)
  }
  .custom instance void[mscorlib]System.Diagnostics.DebuggerTypeProxyAttribute::.ctor([mscorlib]System.Type)
  {
    .argument typeof([mscorlib]System.Collections.Generic.Mscorlib_CollectionDebugView`1)
  }
  .custom instance void[mscorlib]System.Reflection.DefaultMemberAttribute::.ctor([mscorlib]System.String)
  {
    .argument const(""Item"",[mscorlib]System.String)
  }
  .class Enumerator
  .field _defaultCapacity : int32
  .field _emptyArray : int32[]
  .field _items : int32[]
  .field _size : int32
  .field _syncRoot : [mscorlib]System.Object
  .field _version : int32
  .method .cctor : void()
  .method .ctor : void()
  .method .ctor : void([mscorlib]System.Collections.Generic.IEnumerable`1<int32>)
  .method .ctor : void(int32)
  .method Add : void(int32)
  .method AddRange : void([mscorlib]System.Collections.Generic.IEnumerable`1<int32>)
  .method AsReadOnly : [mscorlib]System.Collections.ObjectModel.ReadOnlyCollection`1<int32>()
  .method BinarySearch : int32(int32)
  .method BinarySearch : int32(int32,[mscorlib]System.Collections.Generic.IComparer`1<int32>)
  .method BinarySearch : int32(int32,int32,int32,[mscorlib]System.Collections.Generic.IComparer`1<int32>)
  .method Clear : void()
  .method Contains : bool(int32)
  .method ConvertAll : [mscorlib]System.Collections.Generic.List`1<!!0>([mscorlib]System.Converter`2<int32,!!0>)
  .method CopyTo : void(int32,int32[],int32,int32)
  .method CopyTo : void(int32[])
  .method CopyTo : void(int32[],int32)
  .method EnsureCapacity : void(int32)
  .method Exists : bool([mscorlib]System.Predicate`1<int32>)
  .method Find : int32([mscorlib]System.Predicate`1<int32>)
  .method FindAll : [mscorlib]System.Collections.Generic.List`1<int32>([mscorlib]System.Predicate`1<int32>)
  .method FindIndex : int32([mscorlib]System.Predicate`1<int32>)
  .method FindIndex : int32(int32,[mscorlib]System.Predicate`1<int32>)
  .method FindIndex : int32(int32,int32,[mscorlib]System.Predicate`1<int32>)
  .method FindLast : int32([mscorlib]System.Predicate`1<int32>)
  .method FindLastIndex : int32([mscorlib]System.Predicate`1<int32>)
  .method FindLastIndex : int32(int32,[mscorlib]System.Predicate`1<int32>)
  .method FindLastIndex : int32(int32,int32,[mscorlib]System.Predicate`1<int32>)
  .method ForEach : void([mscorlib]System.Action`1<int32>)
  .method get_Capacity : int32()
  .method get_Count : int32()
  .method get_Item : int32(int32)
  .method GetEnumerator : [mscorlib]System.Collections.Generic.List`1<int32>/Enumerator()
  .method GetRange : [mscorlib]System.Collections.Generic.List`1<int32>(int32,int32)
  .method IndexOf : int32(int32)
  .method IndexOf : int32(int32,int32)
  .method IndexOf : int32(int32,int32,int32)
  .method Insert : void(int32,int32)
  .method InsertRange : void(int32,[mscorlib]System.Collections.Generic.IEnumerable`1<int32>)
  .method IsCompatibleObject : bool([mscorlib]System.Object)
  .method LastIndexOf : int32(int32)
  .method LastIndexOf : int32(int32,int32)
  .method LastIndexOf : int32(int32,int32,int32)
  .method Remove : bool(int32)
  .method RemoveAll : int32([mscorlib]System.Predicate`1<int32>)
  .method RemoveAt : void(int32)
  .method RemoveRange : void(int32,int32)
  .method Reverse : void()
  .method Reverse : void(int32,int32)
  .method set_Capacity : void(int32)
  .method set_Item : void(int32,int32)
  .method Sort : void()
  .method Sort : void([mscorlib]System.Collections.Generic.IComparer`1<int32>)
  .method Sort : void([mscorlib]System.Comparison`1<int32>)
  .method Sort : void(int32,int32,[mscorlib]System.Collections.Generic.IComparer`1<int32>)
  .method System.Collections.Generic.ICollection<T>.get_IsReadOnly : bool()
  .method System.Collections.Generic.IEnumerable<T>.GetEnumerator : [mscorlib]System.Collections.Generic.IEnumerator`1<int32>()
  .method System.Collections.ICollection.CopyTo : void([mscorlib]System.Array,int32)
  .method System.Collections.ICollection.get_IsSynchronized : bool()
  .method System.Collections.ICollection.get_SyncRoot : [mscorlib]System.Object()
  .method System.Collections.IEnumerable.GetEnumerator : [mscorlib]System.Collections.IEnumerator()
  .method System.Collections.IList.Add : int32([mscorlib]System.Object)
  .method System.Collections.IList.Contains : bool([mscorlib]System.Object)
  .method System.Collections.IList.get_IsFixedSize : bool()
  .method System.Collections.IList.get_IsReadOnly : bool()
  .method System.Collections.IList.get_Item : [mscorlib]System.Object(int32)
  .method System.Collections.IList.IndexOf : int32([mscorlib]System.Object)
  .method System.Collections.IList.Insert : void(int32,[mscorlib]System.Object)
  .method System.Collections.IList.Remove : void([mscorlib]System.Object)
  .method System.Collections.IList.set_Item : void(int32,[mscorlib]System.Object)
  .method ToArray : int32[]()
  .method TrimExcess : void()
  .method TrueForAll : bool([mscorlib]System.Predicate`1<int32>)
  .method VerifyValueType : void([mscorlib]System.Object)
  .property Capacity : int32()
  .property Count : int32()
  .property Item : int32(int32)
  .property System.Collections.Generic.ICollection<T>.IsReadOnly : bool()
  .property System.Collections.ICollection.IsSynchronized : bool()
  .property System.Collections.ICollection.SyncRoot : [mscorlib]System.Object()
  .property System.Collections.IList.IsFixedSize : bool()
  .property System.Collections.IList.IsReadOnly : bool()
  .property System.Collections.IList.Item : [mscorlib]System.Object(int32)
  .flags class reftype
  .pack 0
  .size 0
  .override instance bool[mscorlib]System.Collections.IList::Contains([mscorlib]System.Object) with instance bool[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.Contains([mscorlib]System.Object)
  .override instance bool[mscorlib]System.Collections.Generic.ICollection`1<int32>::get_IsReadOnly() with instance bool[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.Generic.ICollection<T>.get_IsReadOnly()
  .override instance bool[mscorlib]System.Collections.IList::get_IsReadOnly() with instance bool[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.get_IsReadOnly()
  .override instance bool[mscorlib]System.Collections.ICollection::get_IsSynchronized() with instance bool[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.ICollection.get_IsSynchronized()
  .override instance[mscorlib]System.Object[mscorlib]System.Collections.ICollection::get_SyncRoot() with instance[mscorlib]System.Object[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.ICollection.get_SyncRoot()
  .override instance[mscorlib]System.Object[mscorlib]System.Collections.IList::get_Item(int32) with instance[mscorlib]System.Object[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.get_Item(int32)
  .override instance void[mscorlib]System.Collections.IList::set_Item(int32,[mscorlib]System.Object) with instance void[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.set_Item(int32,[mscorlib]System.Object)
  .override instance int32[mscorlib]System.Collections.IList::Add([mscorlib]System.Object) with instance int32[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.Add([mscorlib]System.Object)
  .override instance bool[mscorlib]System.Collections.IList::get_IsFixedSize() with instance bool[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.get_IsFixedSize()
  .override instance void[mscorlib]System.Collections.ICollection::CopyTo([mscorlib]System.Array,int32) with instance void[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.ICollection.CopyTo([mscorlib]System.Array,int32)
  .override instance[mscorlib]System.Collections.Generic.IEnumerator`1<int32>[mscorlib]System.Collections.Generic.IEnumerable`1<int32>::GetEnumerator() with instance[mscorlib]System.Collections.Generic.IEnumerator`1<int32>[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.Generic.IEnumerable<T>.GetEnumerator()
  .override instance[mscorlib]System.Collections.IEnumerator[mscorlib]System.Collections.IEnumerable::GetEnumerator() with instance[mscorlib]System.Collections.IEnumerator[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IEnumerable.GetEnumerator()
  .override instance int32[mscorlib]System.Collections.IList::IndexOf([mscorlib]System.Object) with instance int32[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.IndexOf([mscorlib]System.Object)
  .override instance void[mscorlib]System.Collections.IList::Insert(int32,[mscorlib]System.Object) with instance void[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.Insert(int32,[mscorlib]System.Object)
  .override instance void[mscorlib]System.Collections.IList::Remove([mscorlib]System.Object) with instance void[mscorlib]System.Collections.Generic.List`1<int32>::System.Collections.IList.Remove([mscorlib]System.Object)
}
";
      return result.Equals(stringPaper.Content);
    }
  }
}
