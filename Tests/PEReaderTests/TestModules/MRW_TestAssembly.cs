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
using Module1;
using System.Runtime.InteropServices;
using com.ms.win32;
using System.Collections.Generic;
using System.Security.Permissions;

[assembly: FileIOPermission(SecurityAction.RequestMinimum, Write = @"C:\AnotherDirectoryAltogether")]

public enum TestEnum {
  Bar,
  Tar,
}

public class DAttribute : Attribute {
  public DAttribute(
    string s,
    int i,
    double f,
    Type t,
    TestEnum fe
  ) {
  }

  public object[] Array;
  public int[] IntArray;
}

public class Foo<T> {
}

public class Bar<T> : Foo<T> {
  class Tar<V> where V : T {
  }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class TestAttribute : Attribute {
  public TestAttribute(Type t) {
  }
}

public class Faa<T> {
  public class Baa<U> {
  }
}

[D("DDD", 666, 666.666, typeof(TestEnum), TestEnum.Bar, Array = new object[] { null, null }, IntArray = new int[] { 6, 6, 6 })]
[Test(typeof(Faa<TestAttribute>.Baa<int>))]
[Test(typeof(int[][,]))]
[Test(typeof(float**))]
public class TypeTest {
  public Foo Foo;
  public Foo.Nested FooNested;
  public List<int> IntList;
}

public class MarshalTest {
  [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
  public static extern IntPtr CreateFontIndirect(
        [In, MarshalAs(UnmanagedType.LPStruct)]
            LOGFONT lplf   // characteristics
        );
  [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
  public static extern IntPtr CreateFontIndirectArray(
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStruct, SizeParamIndex = 1)]
            LOGFONT[] lplf   // characteristics
        );
}

class MethodBodyTest {
  void TryCatchFinallyHandlerMethod() {
    try {
      new object();
    } catch (System.Exception se) {
      Console.WriteLine(se.ToString());
    } finally {
      Console.WriteLine("In Finally");
    }
  }
}
