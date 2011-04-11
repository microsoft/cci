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
  public class MethodBodyTests {
    readonly ModuleReaderTestClass ModuleReaderTest;
    readonly IName MethodBodyTest;
    readonly IName TryCatchFinallyHandlerMethod;

    public MethodBodyTests(ModuleReaderTestClass mrTest) {
      this.ModuleReaderTest = mrTest;
      this.MethodBodyTest = mrTest.NameTable.GetNameFor("MethodBodyTest");
      this.TryCatchFinallyHandlerMethod = mrTest.NameTable.GetNameFor("TryCatchFinallyHandlerMethod");
    }

    public bool RunMethodBodyTests() {
      bool ret = true;
      if (!this.TestTryCatchFinallyIL()) {
        Console.WriteLine("TestTryCatchFinallyIL - Failed");
        ret = false;
      }
      if (!this.TestTryCatchFinallyIR()) {
        Console.WriteLine("TestTryCatchFinallyIR - Failed");
        ret = false;
      }
      return ret;
    }

    public bool TestTryCatchFinallyIL() {
      ITypeDefinition type = Helper.GetNamespaceType((IUnitNamespace)this.ModuleReaderTest.TestAssembly.NamespaceRoot, this.MethodBodyTest);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.TryCatchFinallyHandlerMethod);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method private hidebysig instance explicit default void TryCatchFinallyHandlerMethod()cil managed
{
  .maxstack 1
  .locals init(
    [mscorlib]System.Exception  V_0
  )
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  newobj instance void[mscorlib]System.Object::.ctor()
  IL_0007:  pop
  IL_0008:  nop
  IL_0009:  leave.s IL_001c
  IL_000b:  stloc.0
  IL_000c:  nop
  IL_000d:  ldloc.0
  IL_000e:  callvirt instance[mscorlib]System.String[mscorlib]System.Object::ToString()
  IL_0013:  call static void[mscorlib]System.Console::WriteLine([mscorlib]System.String)
  IL_0018:  nop
  IL_0019:  nop
  IL_001a:  leave.s IL_001c
  IL_001c:  nop
  IL_001d:  leave.s IL_002d
  IL_001f:  nop
  IL_0020:  ldstr ""In Finally""
  IL_0025:  call static void[mscorlib]System.Console::WriteLine([mscorlib]System.String)
  IL_002a:  nop
  IL_002b:  nop
  IL_002c:  endfinally
  IL_002d:  nop
  IL_002e:  ret
  .try IL_0001 to IL_000b catch[mscorlib]System.Exception handler IL_000b to IL_001c
  .try IL_0001 to IL_001f finally handler IL_001f to IL_002d
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestTryCatchFinallyIR() {
      ITypeDefinition type = Helper.GetNamespaceType((IUnitNamespace)this.ModuleReaderTest.TestAssembly.NamespaceRoot, this.MethodBodyTest);
      IMethodDefinition method = Helper.GetMethodNamed(type, this.TryCatchFinallyHandlerMethod);
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.MethodDefinition(method);
      string result =
@".method private hidebysig instance explicit default void TryCatchFinallyHandlerMethod()cil managed
{
  .maxstack 1
  .locals init(
    [mscorlib]System.Exception  V_0
  )
  IL_0000:  nop
  IL_0001:  nop
  IL_0002:  newobj instance void[mscorlib]System.Object::.ctor()
  IL_0007:  pop
  IL_0008:  nop
  IL_0009:  leave.s IL_001c
  IL_000b:  stloc.0
  IL_000c:  nop
  IL_000d:  ldloc.0
  IL_000e:  callvirt instance[mscorlib]System.String[mscorlib]System.Object::ToString()
  IL_0013:  call static void[mscorlib]System.Console::WriteLine([mscorlib]System.String)
  IL_0018:  nop
  IL_0019:  nop
  IL_001a:  leave.s IL_001c
  IL_001c:  nop
  IL_001d:  leave.s IL_002d
  IL_001f:  nop
  IL_0020:  ldstr ""In Finally""
  IL_0025:  call static void[mscorlib]System.Console::WriteLine([mscorlib]System.String)
  IL_002a:  nop
  IL_002b:  nop
  IL_002c:  endfinally
  IL_002d:  nop
  IL_002e:  ret
  .try IL_0001 to IL_000b catch[mscorlib]System.Exception handler IL_000b to IL_001c
  .try IL_0001 to IL_001f finally handler IL_001f to IL_002d
}
";
      return result.Equals(stringPaper.Content);
    }


  }
}
