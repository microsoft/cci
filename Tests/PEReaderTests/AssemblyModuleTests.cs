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
using System.Text;
using Microsoft.Cci.MetadataReader;
using Microsoft.Cci;

namespace ModuleReaderTests {
  public class AssemblyModuleTests {
    readonly ModuleReaderTestClass ModuleReaderTest;

    public AssemblyModuleTests(ModuleReaderTestClass mrTest) {
      this.ModuleReaderTest = mrTest;
    }

    public bool RunAssemblyTests() {
      bool ret = true;
      if (!this.TestCurrentAssembly()) {
        Console.WriteLine("TestCurrentAssembly - Failed");
        ret = false;
      }
      if (!this.TestCurrentModule()) {
        Console.WriteLine("TestCurrentModule - Failed");
        ret = false;
      }
      //if (!this.TestMsCorlibModuleReferences()) {
      //  Console.WriteLine("TestMsCorlibModuleReferences - Failed");
      //  ret = false;
      //}
      if (!this.TestVjslibAssemblyReferences()) {
        Console.WriteLine("TestVjslibAssemblyReferences - Failed");
        ret = false;
      }
      if (!this.TestSystemFileSystemWatcherResource()) {
        Console.WriteLine("TestSystemFileSystemWatcherResource - Failed");
        ret = false;
      }
      if (!this.TestMscorlibsorttblsResource()) {
        Console.WriteLine("TestMscorlibsorttblsResource - Failed");
        ret = false;
      }
      if (!this.TestMscorlibFileReferences()) {
        Console.WriteLine("TestMscorlibFileReferences - Failed");
        ret = false;
      }
      if (!this.TestVjslibModule()) {
        Console.WriteLine("TestVjslibModule - Failed");
        ret = false;
      }
      if (!this.TestAssemblyExports()) {
        Console.WriteLine("TestAssemblyExports - Failed");
        ret = false;
      }
      //if (!this.TestMscorlibWin32Resources()) {
      //  Console.WriteLine("TestMscorlibWin32Resources - Failed");
      //  ret = false;
      //}
      if (!this.TestMscorlibSecurityAttributes()) {
        Console.WriteLine("TestMscorlibSecurityAttributes - Failed");
        ret = false;
      }
      return ret;
    }

    public bool TestCurrentAssembly() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.Assembly(this.ModuleReaderTest.TestAssembly);
      string result =
@".assembly MRW_TestAssembly
{
  .custom instance void[mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32)
  {
    .argument const(8,int32)
  }
  .custom instance void[mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor()
  {
    .argument .property WrapNonExceptionThrows : bool()=const(True,bool)
  }
  .publickey = (00 24 00 00 04 80 00 00 94 00 00 00 06 02 00 00   // .$..............
                00 24 00 00 52 53 41 31 00 04 00 00 01 00 01 00   // .$..RSA1........
                2B 96 12 82 73 B1 F0 B2 89 A1 53 81 A7 A1 1A BF   // +...s.....S.....
                07 40 A0 08 21 51 DE DF 0D 8C 66 0D 61 9A 97 19   // .@..!Q....f.a...
                07 08 76 E4 94 44 5A AB 22 BC B3 97 D7 B4 FF 97   // ..v..DZ."".......
                CA 80 ED 49 B3 FC 2B 87 BB 76 7B 60 CA FB F9 49   // ...I..+..v{`...I
                AA 43 5F CF 17 DE B3 19 01 BE 16 49 3C 87 DF E6   // .C_........I<...
                1D 71 F5 18 5F 06 97 A7 0A B5 E0 F1 E0 5C 70 46   // .q.._........\pF
                DB 0D 28 C1 BE 6D 83 DA 3F AC 58 16 1C 56 3C A5   // ..(..m..?.X..V<.
                9D C2 EF 9C E3 02 30 D9 37 7A A6 3D D2 76 CD BF ) // ......0.7z.=.v..
  .hash algorithm 0x00008004
  .ver 0:0:0:0
  .flags 0x00000001
  .permissionset reqmin
  {
    .custom instance void[mscorlib]System.Security.Permissions.FileIOPermissionAttribute::.ctor([mscorlib]System.Security.Permissions.SecurityAction)
    {
      .argument .property Write : [mscorlib]System.String()=const(""C:\AnotherDirectoryAltogether"",[mscorlib]System.String)
    }
  }
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestCurrentModule() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.TestAssembly);
      prettyPrinter.Module(this.ModuleReaderTest.TestAssembly);
      string result =
@".module MRW_TestAssembly {
  .flags ilonly dll
  .mdversion 2:0
  .guid c688ea21-04ad-49a7-931b-119f749453a6
  .runtime v2.0.50727
  .assembly[MRW_TestAssembly]
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestMsCorlibModuleReferences() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.ModuleReferences(this.ModuleReaderTest.MscorlibAssembly);
      string result =
@".module extern kernel32.dll
.module extern mscorwks.dll
.module extern oleaut32.dll
.module extern advapi32.dll
.module extern ole32.dll
.module extern user32.dll
.module extern shfolder.dll
.module extern secur32.dll
.module extern mscoree.dll
";
      string resultVista =
@".module extern kernel32.dll
.module extern mscorwks.dll
.module extern oleaut32.dll
.module extern advapi32.dll
.module extern ole32.dll
.module extern user32.dll
.module extern shfolder.dll
.module extern secur32.dll
.module extern bcrypt.dll
.module extern mscoree.dll
";
      return result.Equals(stringPaper.Content) || resultVista.Equals(stringPaper.Content);
    }

    public bool TestVjslibAssemblyReferences() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.VjslibAssembly);
      prettyPrinter.AssemblyReferences(this.ModuleReaderTest.VjslibAssembly);
      string result =
@".assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern vjscor
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 2:0:0:0
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern System.Xml
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern vjsvwaux
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 2:0:0:0
}
.assembly extern vjslibcw
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 2:0:0:0
}
.assembly extern System.Data
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly extern System.Drawing
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 2:0:0:0
}
.assembly extern System.Web
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 2:0:0:0
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestSystemFileSystemWatcherResource() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.SystemAssembly);
      IResourceReference resourceReference = UnitHelper.FindResourceNamed(this.ModuleReaderTest.SystemAssembly, this.ModuleReaderTest.NameTable.GetNameFor("System.IO.FileSystemWatcher.bmp"));
      prettyPrinter.ResourceReference(resourceReference);
      string result =
@".mresource public System.IO.FileSystemWatcher.bmp
{
  (42 4D 38 03 00 00 00 00 00 00 36 00 00 00 28 00   // BM8.......6...(.
   00 00 10 00 00 00 10 00 00 00 01 00 18 00 00 00 
   00 00 00 00 00 00 12 0B 00 00 12 0B 00 00 00 00 
   00 00 00 00 00 00 FF 00 FF 7F 7F 7F 46 43 41 34   // ............FCA4
   2E 2A 5A 56 53 FF 00 FF FF 00 FF FF 00 FF 54 54   // .*ZVS.........TT
   54 33 33 33 60 60 5F 60 60 5F FF 00 FF FF 00 FF   // T333``_``_......
   FF 00 FF FF 00 FF 6E 6E 6E FF DA BF FF C0 99 FF   // ......nnn.......
   BC 8A 37 32 30 31 2F 2D 57 55 54 46 44 41 FF DA   // ..7201/-WUTFDA..
   BE FF C2 95 FF B0 7A 32 2C 28 60 60 5F FF 00 FF   // ......z2,(``_...
   FF 00 FF FF 00 FF 61 5F 5E F2 F2 F2 FF D5 C1 FF   // ......a_^.......
   C0 9C 57 55 53 3B 35 32 6A 66 62 6C 69 67 FA FA   // ..WUS;52jfblig..
   FA FF D9 BF FF C1 9A 3D 3C 3C 3C 38 34 FF 00 FF   // .......=<<<84...
   FF 00 FF FF 00 FF 6E 6D 6D 60 5F 5F 62 59 53 4E   // ......nmm`__bYSN
   47 41 64 5D 58 A3 9C 95 B8 AF A6 9A 95 90 69 66   // GAd]X.........if
   65 5E 59 55 50 47 40 33 2B 26 2E 27 22 A4 95 89   // e^YUPG@3+&.'""...
   63 49 35 63 49 35 CF CF CF 61 61 61 46 3F 39 9D   // cI5cI5...aaaF?9.
   98 96 E4 E0 DC E3 DB D7 E2 D7 CF E2 D3 C9 E0 CE 
   C2 E0 C9 BB DF C6 B5 DB BB A7 2F 28 23 DB BB A7   // ........../(#...
   CF B4 A3 63 49 35 FF 00 FF B7 B6 B6 61 61 61 47   // ...cI5......aaaG
   41 3C F5 F0 EE FC F5 F2 FA F2 ED FA EF E9 F9 EA   // A<..............
   E4 F7 E8 E0 F7 E4 DB 98 89 82 2E 27 22 F4 DB CE   // ...........'""...
   CF B4 A3 63 49 35 FF 00 FF FF 00 FF A3 9E 9B 61   // ...cI5.........a
   61 61 A7 A4 A2 60 5B 58 FB F4 F1 63 49 35 63 49   // aa...`[X...cI5cI
   35 63 49 35 F7 E7 DE 7D 71 6A 2E 28 23 F5 DE D2   // 5cI5...}qj.(#...
   CF B4 A3 63 49 35 FF 00 FF FF 00 FF D5 C9 C0 DD   // ...cI5..........
   DC DC 58 52 4D BD BA B7 FC F6 F3 C0 A9 9B 00 CC   // ..XRM...........
   FF 63 49 35 F9 EA E2 8D 77 6B 3A 35 31 A3 93 8B   // .cI5....wk:51...
   CF B4 A3 63 49 35 FF 00 FF FF 00 FF BA A5 96 FF   // ...cI5..........
   FF FF F8 F7 F7 F2 F0 EF FA F6 F4 C0 A9 9B C0 A9 
   9B C0 A9 9B FA EC E7 F8 E9 E2 F7 E6 DD F6 E2 D9 
   D0 B9 AB 63 49 35 FF 00 FF FF 00 FF BE A9 9A FF   // ...cI5..........
   FF FF 63 49 35 63 49 35 63 49 35 FD F8 F7 FC F6   // ..cI5cI5cI5.....
   F3 FB F3 EF FB EF EB F9 EC E6 F8 E8 E1 F7 E5 DC 
   D1 C1 B6 63 49 35 FF 00 FF FF 00 FF C3 AE 9E FF   // ...cI5..........
   FF FF C0 A9 9B 00 CC FF 63 49 35 FE FB FA AE 93   // ........cI5.....
   84 A6 8D 7C 9F 85 74 99 7E 6B F9 EB E5 F8 E8 E1   // ...|..t.~k......
   D1 C1 B6 63 49 35 FF 00 FF FF 00 FF C8 B2 A3 FF   // ...cI5..........
   FF FF C0 A9 9B C0 A9 9B C0 A9 9B FE FD FC FD FB 
   F9 FD F8 F5 FC F5 F1 FB F2 ED FA EE E9 F9 EA E4 
   D1 C1 B6 63 49 35 FF 00 FF FF 00 FF CC B6 A7 FF   // ...cI5..........
   FF FF FF FF FF FF FF FF FF FF FF FF FF FE FE FC 
   FC FE FA F9 FD F7 F5 FB F4 F1 FB F1 ED FA EE E8 
   F9 EA E3 63 49 35 FF 00 FF FF 00 FF EA AA 8B EA   // ...cI5..........
   AA 8B EA AA 8B E9 A5 84 E9 9F 7A E7 97 6E E6 8E   // ..........z..n..
   62 E5 86 56 E3 7D 4A E3 76 40 E2 72 39 E2 72 39   // b..V.}J.v@.r9.r9
   E2 72 39 C8 62 2F FF 00 FF FF 00 FF EA AA 8B FF   // .r9.b/..........
   C2 A2 FE C0 9F FD BD 9A FC B9 96 FB B5 90 FA B0 
   8B F9 AB 84 F8 A7 7D F6 A2 77 F5 9D 71 F5 99 6A   // ......}..w..q..j
   F3 95 65 CD 65 31 FF 00 FF FF 00 FF EA AA 8B EA   // ..e.e1..........
   AA 8B EA AA 8B EA AA 8B EA A6 86 E9 A1 7F E8 9B 
   76 E7 94 6C E6 8E 62 E5 87 58 E4 81 4E E4 7B 46   // v..l..b..X..N.{F
   E3 76 3E E2 72 39 00 00 )                         // .v>.r9..
}
";
      return result.Equals(stringPaper.Content);
    }

    public bool TestMscorlibsorttblsResource() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      IResourceReference resourceReference = UnitHelper.FindResourceNamed(this.ModuleReaderTest.MscorlibAssembly, this.ModuleReaderTest.NameTable.GetNameFor("sorttbls.nlp"));
      prettyPrinter.ResourceReference(resourceReference);
      string[] sampleStrings = new string[]{
@".mresource public sorttbls.nlp
{
  .file sorttbls.nlp at 0x00000000",
@"  (06 00 00 00 0C 04 00 00 0C 08 00 00 0C 0C 00 00",
@"   1C 0E 02 12 44 00 5A 00 1C 0E 02 1A 67 00 79 00   // ....D.Z.....g.y.",
@"   4C 0E 00 00 51 1E 02 02 42 0E 0D 0E 4C 0E 00 00   // L...Q...B...L...",
@"   4C 0E 00 00 17 1E 02 02 44 0E 03 0E 4C 0E 00 00   // L.......D...L..."};
      int[] sampleIndices = new int[] { 0, 71, 4480, 20168, 17504 };

      return Helper.CompareStringContents(stringPaper.Content, sampleStrings, sampleIndices);
    }

    public bool TestMscorlibFileReferences() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.FileReferences(this.ModuleReaderTest.MscorlibAssembly.Files);
      string result =
@".file nometadata sortkey.nlp
    .hash = (6E 30 2E 50 36 FB 60 2C 8E 50 C0 83 54 8B CC EA   // n0.P6.`,.P..T...
             A8 91 B6 CC )
.file nometadata sorttbls.nlp
    .hash = (F9 04 A8 31 CD FB 23 72 95 63 5D 14 9A A0 66 8E   // ...1..#r.c]...f.
             7F 44 93 7A )                                     // .D.z
.file nometadata big5.nlp
    .hash = (92 17 03 A1 8D 8F B0 F3 EC 4C 0A 7B 61 E7 E6 53   // .........L.{a..S
             27 05 80 F2 )                                     // '...
.file nometadata bopomofo.nlp
    .hash = (F9 79 3B 18 A3 F7 8E 58 65 60 8B B6 F6 FF 02 5D   // .y;....Xe`.....]
             AD 95 54 82 )                                     // ..T.
.file nometadata ksc.nlp
    .hash = (92 AF F5 B9 0D 49 84 F5 52 21 1B DD 3D D4 68 A2   // .....I..R!..=.h.
             5A 54 35 19 )                                     // ZT5.
.file nometadata prc.nlp
    .hash = (65 52 D7 6B 73 26 23 F2 80 D1 B6 34 87 38 27 B1   // eR.ks&#....4.8'.
             E8 C3 F6 69 )                                     // ...i
.file nometadata prcp.nlp
    .hash = (6D C0 7D 9B 0F D2 DA BB 67 D9 41 A0 25 E4 06 2E   // m.}.....g.A.%...
             5F 40 38 D6 )                                     // _@8.
.file nometadata xjis.nlp
    .hash = (47 F7 D9 5A E3 F3 2A D1 90 E3 C2 D2 AA C4 ED 81   // G..Z..*.........
             62 00 A3 89 )                                     // b...
.file nometadata normidna.nlp
    .hash = (FD 94 72 EF FD AC 8D A9 88 02 AC D4 2C D6 82 F2   // ..r.........,...
             51 AF B9 4C )                                     // Q..L
.file nometadata normnfc.nlp
    .hash = (24 31 FE A5 60 F8 93 A7 BA F1 A0 1A 42 1C 94 4D   // $1..`.......B..M
             51 68 87 B8 )                                     // Qh..
.file nometadata normnfd.nlp
    .hash = (21 9F 44 9D B1 57 39 86 B1 9B 28 97 43 46 38 4D   // !.D..W9...(.CF8M
             3E BD AD BF )                                     // >...
.file nometadata normnfkc.nlp
    .hash = (2D 46 82 A3 F1 2D 1B 7D C2 80 87 ED 1D 56 D3 1D   // -F...-.}.....V..
             86 03 A2 FE )
.file nometadata normnfkd.nlp
    .hash = (C0 C4 85 52 9D 70 43 90 7C 3E 48 02 D2 43 49 01   // ...R.pC.|>H..CI.
             1E 80 0A E1 )
";
      string vistaResult = 
@".file nometadata sortkey.nlp
    .hash = (6E 30 2E 50 36 FB 60 2C 8E 50 C0 83 54 8B CC EA   // n0.P6.`,.P..T...
             A8 91 B6 CC )
.file nometadata sorttbls.nlp
    .hash = (F9 04 A8 31 CD FB 23 72 95 63 5D 14 9A A0 66 8E   // ...1..#r.c]...f.
             7F 44 93 7A )                                     // .D.z
.file nometadata big5.nlp
    .hash = (92 17 03 A1 8D 8F B0 F3 EC 4C 0A 7B 61 E7 E6 53   // .........L.{a..S
             27 05 80 F2 )                                     // '...
.file nometadata bopomofo.nlp
    .hash = (F9 79 3B 18 A3 F7 8E 58 65 60 8B B6 F6 FF 02 5D   // .y;....Xe`.....]
             AD 95 54 82 )                                     // ..T.
.file nometadata ksc.nlp
    .hash = (92 AF F5 B9 0D 49 84 F5 52 21 1B DD 3D D4 68 A2   // .....I..R!..=.h.
             5A 54 35 19 )                                     // ZT5.
.file nometadata prc.nlp
    .hash = (65 52 D7 6B 73 26 23 F2 80 D1 B6 34 87 38 27 B1   // eR.ks&#....4.8'.
             E8 C3 F6 69 )                                     // ...i
.file nometadata prcp.nlp
    .hash = (6D C0 7D 9B 0F D2 DA BB 67 D9 41 A0 25 E4 06 2E   // m.}.....g.A.%...
             5F 40 38 D6 )                                     // _@8.
.file nometadata xjis.nlp
    .hash = (47 F7 D9 5A E3 F3 2A D1 90 E3 C2 D2 AA C4 ED 81   // G..Z..*.........
             62 00 A3 89 )                                     // b...
.file nometadata normidna.nlp
    .hash = (C1 EE 35 98 B0 1C 15 D6 E7 72 97 14 16 EE F2 F5   // ..5......r......
             1E 8C 04 82 )
.file nometadata normnfc.nlp
    .hash = (A8 84 12 62 EC 4E B2 EA 16 06 AE F5 22 48 E2 17   // ...b.N......""H..
             B4 81 95 3A )                                     // ...:
.file nometadata normnfd.nlp
    .hash = (73 84 41 3B 1E 97 D3 86 B9 8E C9 EF D3 2C 03 C0   // s.A;.........,..
             C7 2A CC 8B )                                     // .*..
.file nometadata normnfkc.nlp
    .hash = (A0 64 1D 07 6D 7A 65 A5 AD 56 97 18 35 35 B8 86   // .d..mze..V..55..
             AA 73 B8 60 )                                     // .s.`
.file nometadata normnfkd.nlp
    .hash = (84 F8 E2 04 00 3A EC F8 1D 72 9D A1 D5 E0 F6 AB   // .....:...r......
             1F 69 CB 7E )                                     // .i.~
";
      return result.Equals(stringPaper.Content) || vistaResult.Equals(stringPaper.Content);
    }

    bool TestVjslibModule() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.Module(this.ModuleReaderTest.VjslibAssembly);
      string result =
@".module vjslib {
  .flags ilonly bit32 dll
  .mdversion 2:0
  .guid 771d4164-d938-4f80-b109-375580583fea
  .runtime v2.0.50727
  .assembly[vjslib]
}
";
      return result.Equals(stringPaper.Content);
    }

    bool TestAssemblyExports() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      prettyPrinter.ExportedTypes(this.ModuleReaderTest.AssemblyAssembly.ExportedTypes);
      string result =
@".class extern public Module1.Foo
{
  .file MRW_Module1.netmodule
}
.class extern nested public Nested
{
  .class extern Module1.Foo
}
.class extern public Module2.Bar
{
  .file MRW_Module2.netmodule
}
";
      return result.Equals(stringPaper.Content);
    }

    bool TestMscorlibWin32Resources() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.AssemblyAssembly);
      IEnumerator<IWin32Resource> win32ResourceEnumerator = this.ModuleReaderTest.MscorlibAssembly.Win32Resources.GetEnumerator();
      if (!win32ResourceEnumerator.MoveNext() || !win32ResourceEnumerator.MoveNext())
        return false;
      prettyPrinter.Win32Resource(win32ResourceEnumerator.Current);
      string result =
@".win32resource 00000001
{
  .type 00000010
  .language 0x00000409
  .codepage 0x000004E4
  .data = (DC 03 34 00 00 00 56 00 53 00 5F 00 56 00 45 00   // ..4...V.S._.V.E.
           52 00 53 00 49 00 4F 00 4E 00 5F 00 49 00 4E 00   // R.S.I.O.N._.I.N.
           46 00 4F 00 00 00 00 00 BD 04 EF FE 00 00 01 00   // F.O.............
           00 00 02 00 2A 00 27 C6 00 00 02 00 2A 00 27 C6   // ....*.'.....*.'.
           3F 00 00 00 00 00 00 00 04 00 00 00 02 00 00 00   // ?...............
           00 00 00 00 00 00 00 00 00 00 00 00 3C 03 00 00   // ............<...
           01 00 53 00 74 00 72 00 69 00 6E 00 67 00 46 00   // ..S.t.r.i.n.g.F.
           69 00 6C 00 65 00 49 00 6E 00 66 00 6F 00 00 00   // i.l.e.I.n.f.o...
           18 03 00 00 01 00 30 00 34 00 30 00 39 00 30 00   // ......0.4.0.9.0.
           34 00 42 00 30 00 00 00 4C 00 16 00 01 00 43 00   // 4.B.0...L.....C.
           6F 00 6D 00 70 00 61 00 6E 00 79 00 4E 00 61 00   // o.m.p.a.n.y.N.a.
           6D 00 65 00 00 00 00 00 4D 00 69 00 63 00 72 00   // m.e.....M.i.c.r.
           6F 00 73 00 6F 00 66 00 74 00 20 00 43 00 6F 00   // o.s.o.f.t. .C.o.
           72 00 70 00 6F 00 72 00 61 00 74 00 69 00 6F 00   // r.p.o.r.a.t.i.o.
           6E 00 00 00 88 00 30 00 01 00 46 00 69 00 6C 00   // n.....0...F.i.l.
           65 00 44 00 65 00 73 00 63 00 72 00 69 00 70 00   // e.D.e.s.c.r.i.p.
           74 00 69 00 6F 00 6E 00 00 00 00 00 4D 00 69 00   // t.i.o.n.....M.i.
           63 00 72 00 6F 00 73 00 6F 00 66 00 74 00 20 00   // c.r.o.s.o.f.t. .
           43 00 6F 00 6D 00 6D 00 6F 00 6E 00 20 00 4C 00   // C.o.m.m.o.n. .L.
           61 00 6E 00 67 00 75 00 61 00 67 00 65 00 20 00   // a.n.g.u.a.g.e. .
           52 00 75 00 6E 00 74 00 69 00 6D 00 65 00 20 00   // R.u.n.t.i.m.e. .
           43 00 6C 00 61 00 73 00 73 00 20 00 4C 00 69 00   // C.l.a.s.s. .L.i.
           62 00 72 00 61 00 72 00 79 00 00 00 5E 00 1F 00   // b.r.a.r.y...^...
           01 00 46 00 69 00 6C 00 65 00 56 00 65 00 72 00   // ..F.i.l.e.V.e.r.
           73 00 69 00 6F 00 6E 00 00 00 00 00 32 00 2E 00   // s.i.o.n.....2...
           30 00 2E 00 35 00 30 00 37 00 32 00 37 00 2E 00   // 0...5.0.7.2.7...
           34 00 32 00 20 00 28 00 52 00 54 00 4D 00 2E 00   // 4.2. .(.R.T.M...
           30 00 35 00 30 00 37 00 32 00 37 00 2D 00 34 00   // 0.5.0.7.2.7.-.4.
           32 00 30 00 30 00 29 00 00 00 00 00 3A 00 0D 00   // 2.0.0.).....:...
           01 00 49 00 6E 00 74 00 65 00 72 00 6E 00 61 00   // ..I.n.t.e.r.n.a.
           6C 00 4E 00 61 00 6D 00 65 00 00 00 6D 00 73 00   // l.N.a.m.e...m.s.
           63 00 6F 00 72 00 6C 00 69 00 62 00 2E 00 64 00   // c.o.r.l.i.b...d.
           6C 00 6C 00 00 00 00 00 82 00 2F 00 01 00 4C 00   // l.l......./...L.
           65 00 67 00 61 00 6C 00 43 00 6F 00 70 00 79 00   // e.g.a.l.C.o.p.y.
           72 00 69 00 67 00 68 00 74 00 00 00 A9 00 20 00   // r.i.g.h.t..... .
           4D 00 69 00 63 00 72 00 6F 00 73 00 6F 00 66 00   // M.i.c.r.o.s.o.f.
           74 00 20 00 43 00 6F 00 72 00 70 00 6F 00 72 00   // t. .C.o.r.p.o.r.
           61 00 74 00 69 00 6F 00 6E 00 2E 00 20 00 20 00   // a.t.i.o.n... . .
           41 00 6C 00 6C 00 20 00 72 00 69 00 67 00 68 00   // A.l.l. .r.i.g.h.
           74 00 73 00 20 00 72 00 65 00 73 00 65 00 72 00   // t.s. .r.e.s.e.r.
           76 00 65 00 64 00 2E 00 00 00 00 00 42 00 0D 00   // v.e.d.......B...
           01 00 4F 00 72 00 69 00 67 00 69 00 6E 00 61 00   // ..O.r.i.g.i.n.a.
           6C 00 46 00 69 00 6C 00 65 00 6E 00 61 00 6D 00   // l.F.i.l.e.n.a.m.
           65 00 00 00 6D 00 73 00 63 00 6F 00 72 00 6C 00   // e...m.s.c.o.r.l.
           69 00 62 00 2E 00 64 00 6C 00 6C 00 00 00 00 00   // i.b...d.l.l.....
           54 00 1A 00 01 00 50 00 72 00 6F 00 64 00 75 00   // T.....P.r.o.d.u.
           63 00 74 00 4E 00 61 00 6D 00 65 00 00 00 00 00   // c.t.N.a.m.e.....
           4D 00 69 00 63 00 72 00 6F 00 73 00 6F 00 66 00   // M.i.c.r.o.s.o.f.
           74 00 AE 00 20 00 2E 00 4E 00 45 00 54 00 20 00   // t... ...N.E.T. .
           46 00 72 00 61 00 6D 00 65 00 77 00 6F 00 72 00   // F.r.a.m.e.w.o.r.
           6B 00 00 00 3E 00 0D 00 01 00 50 00 72 00 6F 00   // k...>.....P.r.o.
           64 00 75 00 63 00 74 00 56 00 65 00 72 00 73 00   // d.u.c.t.V.e.r.s.
           69 00 6F 00 6E 00 00 00 32 00 2E 00 30 00 2E 00   // i.o.n...2...0...
           35 00 30 00 37 00 32 00 37 00 2E 00 34 00 32 00   // 5.0.7.2.7...4.2.
           00 00 00 00 34 00 0E 00 01 00 43 00 6F 00 6D 00   // ....4.....C.o.m.
           6D 00 65 00 6E 00 74 00 73 00 00 00 46 00 6C 00   // m.e.n.t.s...F.l.
           61 00 76 00 6F 00 72 00 3D 00 52 00 65 00 74 00   // a.v.o.r.=.R.e.t.
           61 00 69 00 6C 00 00 00 44 00 00 00 01 00 56 00   // a.i.l...D.....V.
           61 00 72 00 46 00 69 00 6C 00 65 00 49 00 6E 00   // a.r.F.i.l.e.I.n.
           66 00 6F 00 00 00 00 00 24 00 04 00 00 00 54 00   // f.o.....$.....T.
           72 00 61 00 6E 00 73 00 6C 00 61 00 74 00 69 00   // r.a.n.s.l.a.t.i.
           6F 00 6E 00 00 00 00 00 09 04 B0 04 )             // o.n.........
}
";
      string vistaResult =
@".win32resource 00000001
{
  .type 00000010
  .language 0x00000409
  .codepage 0x000004E4
  .data = (EC 03 34 00 00 00 56 00 53 00 5F 00 56 00 45 00   // ..4...V.S._.V.E.
           52 00 53 00 49 00 4F 00 4E 00 5F 00 49 00 4E 00   // R.S.I.O.N._.I.N.
           46 00 4F 00 00 00 00 00 BD 04 EF FE 00 00 01 00   // F.O.............
           00 00 02 00 9A 05 27 C6 00 00 02 00 9A 05 27 C6   // ......'.......'.
           3F 00 00 00 00 00 00 00 04 00 00 00 02 00 00 00   // ?...............
           00 00 00 00 00 00 00 00 00 00 00 00 4C 03 00 00   // ............L...
           01 00 53 00 74 00 72 00 69 00 6E 00 67 00 46 00   // ..S.t.r.i.n.g.F.
           69 00 6C 00 65 00 49 00 6E 00 66 00 6F 00 00 00   // i.l.e.I.n.f.o...
           28 03 00 00 01 00 30 00 34 00 30 00 39 00 30 00   // (.....0.4.0.9.0.
           34 00 42 00 30 00 00 00 4C 00 16 00 01 00 43 00   // 4.B.0...L.....C.
           6F 00 6D 00 70 00 61 00 6E 00 79 00 4E 00 61 00   // o.m.p.a.n.y.N.a.
           6D 00 65 00 00 00 00 00 4D 00 69 00 63 00 72 00   // m.e.....M.i.c.r.
           6F 00 73 00 6F 00 66 00 74 00 20 00 43 00 6F 00   // o.s.o.f.t. .C.o.
           72 00 70 00 6F 00 72 00 61 00 74 00 69 00 6F 00   // r.p.o.r.a.t.i.o.
           6E 00 00 00 88 00 30 00 01 00 46 00 69 00 6C 00   // n.....0...F.i.l.
           65 00 44 00 65 00 73 00 63 00 72 00 69 00 70 00   // e.D.e.s.c.r.i.p.
           74 00 69 00 6F 00 6E 00 00 00 00 00 4D 00 69 00   // t.i.o.n.....M.i.
           63 00 72 00 6F 00 73 00 6F 00 66 00 74 00 20 00   // c.r.o.s.o.f.t. .
           43 00 6F 00 6D 00 6D 00 6F 00 6E 00 20 00 4C 00   // C.o.m.m.o.n. .L.
           61 00 6E 00 67 00 75 00 61 00 67 00 65 00 20 00   // a.n.g.u.a.g.e. .
           52 00 75 00 6E 00 74 00 69 00 6D 00 65 00 20 00   // R.u.n.t.i.m.e. .
           43 00 6C 00 61 00 73 00 73 00 20 00 4C 00 69 00   // C.l.a.s.s. .L.i.
           62 00 72 00 61 00 72 00 79 00 00 00 6A 00 25 00   // b.r.a.r.y...j.%.
           01 00 46 00 69 00 6C 00 65 00 56 00 65 00 72 00   // ..F.i.l.e.V.e.r.
           73 00 69 00 6F 00 6E 00 00 00 00 00 32 00 2E 00   // s.i.o.n.....2...
           30 00 2E 00 35 00 30 00 37 00 32 00 37 00 2E 00   // 0...5.0.7.2.7...
           31 00 34 00 33 00 34 00 20 00 28 00 52 00 45 00   // 1.4.3.4. .(.R.E.
           44 00 42 00 49 00 54 00 53 00 2E 00 30 00 35 00   // D.B.I.T.S...0.5.
           30 00 37 00 32 00 37 00 2D 00 31 00 34 00 30 00   // 0.7.2.7.-.1.4.0.
           30 00 29 00 00 00 00 00 3A 00 0D 00 01 00 49 00   // 0.).....:.....I.
           6E 00 74 00 65 00 72 00 6E 00 61 00 6C 00 4E 00   // n.t.e.r.n.a.l.N.
           61 00 6D 00 65 00 00 00 6D 00 73 00 63 00 6F 00   // a.m.e...m.s.c.o.
           72 00 6C 00 69 00 62 00 2E 00 64 00 6C 00 6C 00   // r.l.i.b...d.l.l.
           00 00 00 00 82 00 2F 00 01 00 4C 00 65 00 67 00   // ....../...L.e.g.
           61 00 6C 00 43 00 6F 00 70 00 79 00 72 00 69 00   // a.l.C.o.p.y.r.i.
           67 00 68 00 74 00 00 00 A9 00 20 00 4D 00 69 00   // g.h.t..... .M.i.
           63 00 72 00 6F 00 73 00 6F 00 66 00 74 00 20 00   // c.r.o.s.o.f.t. .
           43 00 6F 00 72 00 70 00 6F 00 72 00 61 00 74 00   // C.o.r.p.o.r.a.t.
           69 00 6F 00 6E 00 2E 00 20 00 20 00 41 00 6C 00   // i.o.n... . .A.l.
           6C 00 20 00 72 00 69 00 67 00 68 00 74 00 73 00   // l. .r.i.g.h.t.s.
           20 00 72 00 65 00 73 00 65 00 72 00 76 00 65 00   //  .r.e.s.e.r.v.e.
           64 00 2E 00 00 00 00 00 42 00 0D 00 01 00 4F 00   // d.......B.....O.
           72 00 69 00 67 00 69 00 6E 00 61 00 6C 00 46 00   // r.i.g.i.n.a.l.F.
           69 00 6C 00 65 00 6E 00 61 00 6D 00 65 00 00 00   // i.l.e.n.a.m.e...
           6D 00 73 00 63 00 6F 00 72 00 6C 00 69 00 62 00   // m.s.c.o.r.l.i.b.
           2E 00 64 00 6C 00 6C 00 00 00 00 00 54 00 1A 00   // ..d.l.l.....T...
           01 00 50 00 72 00 6F 00 64 00 75 00 63 00 74 00   // ..P.r.o.d.u.c.t.
           4E 00 61 00 6D 00 65 00 00 00 00 00 4D 00 69 00   // N.a.m.e.....M.i.
           63 00 72 00 6F 00 73 00 6F 00 66 00 74 00 AE 00   // c.r.o.s.o.f.t...
           20 00 2E 00 4E 00 45 00 54 00 20 00 46 00 72 00   //  ...N.E.T. .F.r.
           61 00 6D 00 65 00 77 00 6F 00 72 00 6B 00 00 00   // a.m.e.w.o.r.k...
           42 00 0F 00 01 00 50 00 72 00 6F 00 64 00 75 00   // B.....P.r.o.d.u.
           63 00 74 00 56 00 65 00 72 00 73 00 69 00 6F 00   // c.t.V.e.r.s.i.o.
           6E 00 00 00 32 00 2E 00 30 00 2E 00 35 00 30 00   // n...2...0...5.0.
           37 00 32 00 37 00 2E 00 31 00 34 00 33 00 34 00   // 7.2.7...1.4.3.4.
           00 00 00 00 34 00 0E 00 01 00 43 00 6F 00 6D 00   // ....4.....C.o.m.
           6D 00 65 00 6E 00 74 00 73 00 00 00 46 00 6C 00   // m.e.n.t.s...F.l.
           61 00 76 00 6F 00 72 00 3D 00 52 00 65 00 74 00   // a.v.o.r.=.R.e.t.
           61 00 69 00 6C 00 00 00 44 00 00 00 01 00 56 00   // a.i.l...D.....V.
           61 00 72 00 46 00 69 00 6C 00 65 00 49 00 6E 00   // a.r.F.i.l.e.I.n.
           66 00 6F 00 00 00 00 00 24 00 04 00 00 00 54 00   // f.o.....$.....T.
           72 00 61 00 6E 00 73 00 6C 00 61 00 74 00 69 00   // r.a.n.s.l.a.t.i.
           6F 00 6E 00 00 00 00 00 09 04 B0 04 )             // o.n.........
}
";
      return result.Equals(stringPaper.Content) || vistaResult.Equals(stringPaper.Content);
    }

    public bool TestMscorlibSecurityAttributes() {
      StringILDasmPaper stringPaper = new StringILDasmPaper(2);
      ILDasmPrettyPrinter prettyPrinter = new ILDasmPrettyPrinter(stringPaper, this.ModuleReaderTest.MscorlibAssembly);
      prettyPrinter.SecurityAttributes(this.ModuleReaderTest.MscorlibAssembly.SecurityAttributes);
      string result =
@".permissionset reqmin
{
  .custom instance void System.Security.Permissions.SecurityPermissionAttribute::.ctor(System.Security.Permissions.SecurityAction)
  {
    .argument .property SkipVerification : bool()=const(True,bool)
  }
}
";
      return result.Equals(stringPaper.Content);
    }
  }
}