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
.assembly SimpleOveriddenMethod
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
.module SimpleOveriddenMethod.exe
// MVID: {306A15C9-D158-4041-816A-EAF0ABF55014}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000000024E0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig instance int32 
          F() cil managed
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
  } // end of method A::F

  .method public hidebysig newslot virtual 
          instance int32  G() cil managed
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
  } // end of method A::G

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
          F() cil managed
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
  } // end of method B::F

  .method public hidebysig virtual instance int32 
          G() cil managed
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
  } // end of method B::G

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void A::.ctor()
    IL_0006:  ret
  } // end of method B::.ctor

} // end of class B

.class public auto ansi beforefieldinit SimpleOveriddenMethod
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       118 (0x76)
    .maxstack  2
    .locals init ([0] int32 'ret',
             [1] class B b,
             [2] class A a,
             [3] native int aptr,
             [4] int32 i2,
             [5] int32 i3,
             [6] int32 i4,
             [7] int32 CS$1$0000,
             [8] bool CS$4$0001)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  newobj     instance void B::.ctor()
    IL_0008:  stloc.1
    IL_0009:  ldloc.1
    IL_000a:  stloc.2
    IL_000b:  nop
    IL_000c:  ldloc.2
    IL_000d:  ldftn   instance int32 A::F()
    IL_000e:  calli   instance int32()
    IL_0013:  stloc.0
    IL_0014:  ldloc.0
    IL_0015:  ldc.i4.1
    IL_0016:  ceq
    IL_0018:  stloc.s    CS$4$0001
    IL_001a:  ldloc.s    CS$4$0001
    IL_001c:  brtrue.s   IL_0023

    IL_001e:  ldc.i4.1
    IL_001f:  stloc.s    CS$1$0000
    IL_0021:  br.s       IL_0073

    IL_0023:  nop
    IL_0024:  ldloc.1
    IL_0026:  ldftn   instance int32 B::F()
    IL_0027:  calli   instance int32 ()
    IL_002c:  stloc.0
    IL_002d:  ldloc.0
    IL_002e:  ldc.i4.3
    IL_002f:  ceq
    IL_0031:  stloc.s    CS$4$0001
    IL_0033:  ldloc.s    CS$4$0001
    IL_0035:  brtrue.s   IL_003c

    IL_0037:  ldc.i4.2
    IL_0038:  stloc.s    CS$1$0000
    IL_003a:  br.s       IL_0073

    IL_003c:  ldloc.2
    IL_003f:  ldvirtftn   instance int32 A::G()
		      stloc.3
			  ldloc.2
			  ldloc.3
    IL_0040:  calli   instance int32 ()
    IL_0045:  stloc.0
    IL_0046:  ldloc.0
    IL_0047:  ldc.i4.4
    IL_0048:  ceq
    IL_004a:  stloc.s    CS$4$0001
    IL_004c:  ldloc.s    CS$4$0001
    IL_004e:  brtrue.s   IL_0055

    IL_0050:  ldc.i4.3
    IL_0051:  stloc.s    CS$1$0000
    IL_0053:  br.s       IL_0073

    IL_0055:  ldloc.1
    IL_0056:  ldvirtftn   instance int32 A::G()
    IL_0058:  stloc.3
		      ldloc.1
			  ldloc.3
    IL_0059:  calli   instance int32 ()
    IL_005e:  stloc.0
    IL_005f:  ldloc.0
    IL_0060:  ldc.i4.4
    IL_0061:  ceq
    IL_0063:  stloc.s    CS$4$0001
    IL_0065:  ldloc.s    CS$4$0001
    IL_0067:  brtrue.s   IL_006e

    IL_0069:  ldc.i4.4
    IL_006a:  stloc.s    CS$1$0000
    IL_006c:  br.s       IL_0073

    IL_006e:  ldc.i4.0
    IL_006f:  stloc.s    CS$1$0000
    IL_0071:  br.s       IL_0073

    IL_0073:  ldloc.s    CS$1$0000
    IL_0075:  ret
  } // end of method SimpleOveriddenMethod::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method SimpleOveriddenMethod::.ctor

} // end of class SimpleOveriddenMethod


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file D:\cci\TestResults\t-kchap_Z2904829 2012-07-06 15_04_59\Out\SimpleOveriddenMethod.res
