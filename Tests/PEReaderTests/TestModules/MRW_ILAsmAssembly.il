.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

.assembly MRW_ILAsmAssembly
{
}

.class public auto ansi beforefieldinit Generic`1<T>
{
  .custom instance void TestAttribute1::.ctor(class [mscorlib]System.Type) = ( 01 00 5C 53 79 73 74 65 6D 2E 53 69 6E 67 6C 65   // ..\System.Single
                                                                              2A 26 2C 20 6D 73 63 6F 72 6C 69 62 2C 20 56 65   // *&, mscorlib, Ve
                                                                              72 73 69 6F 6E 3D 32 2E 30 2E 30 2E 30 2C 20 43   // rsion=2.0.0.0, C
                                                                              75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C 20   // ulture=neutral, 
                                                                              50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 62   // PublicKeyToken=b
                                                                              37 37 61 35 63 35 36 31 39 33 34 65 30 38 39 00   // 77a5c561934e089.
                                                                              00 )
.field public class Generic`1<object> field2
.field public !T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* field1
.method public hidebysig instance !!T  GenMethod<class T>(!!T t,
                                                          !!T modopt([mscorlib]System.Runtime.CompilerServices.IsConst)* tp) cil managed
{
  // Code size       16 (0x10)
  .maxstack  3
  .locals init (!!T V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldnull
  IL_0003:  ldc.i4.0
  IL_0004:  call       instance !!0 Generic`1::GenMethod<object>(!!0,
                                                             !!0 modopt([mscorlib]System.Runtime.CompilerServices.IsConst)*)
  IL_0009:  pop
  IL_000a:  ldarg.1
  IL_000b:  stloc.0
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.0
  IL_000f:  ret
}
}

.class public auto ansi beforefieldinit TestAttribute1
       extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 FF 7F 00 00 01 00 54 02 0D 41 6C 6C 6F 77   // ........T..Allow
                                                                                                                         4D 75 6C 74 69 70 6C 65 01 )                      // Multiple.
.method public hidebysig specialname rtspecialname 
        instance void  .ctor(class [mscorlib]System.Type t) cil managed
{
  // Code size       10 (0xa)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void [mscorlib]System.Attribute::.ctor()
  IL_0006:  nop
  IL_0007:  nop
  IL_0008:  nop
  IL_0009:  ret
}
}

