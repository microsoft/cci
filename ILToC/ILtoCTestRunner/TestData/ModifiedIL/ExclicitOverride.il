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

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.30319.1
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly ExclicitOverride
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .permissionset reqmin
             = {[mscorlib]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module ExclicitOverride.exe
// MVID: {619A6F4A-02FE-4DF2-A7FA-41F120AFE2BC}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000000003B0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig instance int32 
          Foo() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A::Foo

  .method public hidebysig newslot virtual 
          instance int32  Bar() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.2
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method A::Bar

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method A::.ctor

} // end of class A

.class private auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig instance int32 
          Foo() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.3
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method B::Foo

  .method public hidebysig newslot virtual 
          instance int32  Baz() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    .locals init ([0] int32 CS$1$0000)
    IL_0000:  nop
    IL_0001:  ldc.i4.4
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0005

    IL_0005:  ldloc.0
    IL_0006:  ret
  } // end of method B::Baz

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor
  .override A::Bar with instance int32  B::Baz()
} // end of class B

.class public auto ansi beforefieldinit ExclicitOverride
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       101 (0x65)
    .maxstack  2
    .locals init ([0] int32 'ret',
             [1] class B b,
             [2] class A a,
             [3] int32 CS$1$0000,
             [4] bool CS$4$0001)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  newobj     instance void B::.ctor()
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  stloc.2
    IL_000b:  ldloc.2
    IL_000c:  callvirt   instance int32 A::Foo()
    IL_0011:  stloc.0
    IL_0012:  ldloc.0
    IL_0013:  ldc.i4.1
    IL_0014:  ceq
    IL_0016:  stloc.s    CS$4$0001
    IL_0018:  ldloc.s    CS$4$0001
    IL_001a:  brtrue.s   IL_0020

    IL_001c:  ldc.i4.1
    IL_001d:  stloc.3
    IL_001e:  br.s       IL_0063

    IL_0020:  ldloc.1
    IL_0021:  callvirt   instance int32 B::Foo()
    IL_0026:  stloc.0
    IL_0027:  ldloc.0
    IL_0028:  ldc.i4.3
    IL_0029:  ceq
    IL_002b:  stloc.s    CS$4$0001
    IL_002d:  ldloc.s    CS$4$0001
    IL_002f:  brtrue.s   IL_0035

    IL_0031:  ldc.i4.2
    IL_0032:  stloc.3
    IL_0033:  br.s       IL_0063

    IL_0035:  ldloc.2
    IL_0036:  callvirt   instance int32 A::Bar()
    IL_003b:  stloc.0
    IL_003c:  ldloc.0
    IL_003d:  ldc.i4.4
    IL_003e:  ceq
    IL_0040:  stloc.s    CS$4$0001
    IL_0042:  ldloc.s    CS$4$0001
    IL_0044:  brtrue.s   IL_004a

    IL_0046:  ldc.i4.3
    IL_0047:  stloc.3
    IL_0048:  br.s       IL_0063

    IL_004a:  ldloc.1
    IL_004b:  callvirt   instance int32 A::Bar()
    IL_0050:  stloc.0
    IL_0051:  ldloc.0
    IL_0052:  ldc.i4.4
    IL_0053:  ceq
    IL_0055:  stloc.s    CS$4$0001
    IL_0057:  ldloc.s    CS$4$0001
    IL_0059:  brtrue.s   IL_005f

    IL_005b:  ldc.i4.4
    IL_005c:  stloc.3
    IL_005d:  br.s       IL_0063

    IL_005f:  ldc.i4.0
    IL_0060:  stloc.3
    IL_0061:  br.s       IL_0063

    IL_0063:  ldloc.3
    IL_0064:  ret
  } // end of method ExclicitOverride::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method ExclicitOverride::.ctor

} // end of class ExclicitOverride


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file D:\cci\Tests\ILtoCTestRunner\TestData\ModifiedIL\ExclicitOverride.res
