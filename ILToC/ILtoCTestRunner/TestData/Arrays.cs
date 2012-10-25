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
public static class Arrays {
  public static int Main() {
    if (!ArrayTest1()) return 1;
    if (!ArrayTest2()) return 2;
    if (!ArrayTest3()) return 3;
    if (!ArrayTest4()) return 4;
    if (!ArrayTest5()) return 5;
    if (!ArrayTest6()) return 6;
    return 0;
  }

  static bool ArrayTest1() {
    var arr = new int[100];
    return arr.Length == 100 && arr.LongLength == 100 && arr.Rank == 1;
  }

  static bool ArrayTest2() {
    var arr = new int[100];
    if (arr[50] != 0) return false;
    arr[50] = 150;
    return arr[50] == 150;
  }

  static bool ArrayTest3() {
    try {
      var i = -100;
      var arr = new int[i];
    } catch (System.OverflowException) {
      return true;
    }
    return false;
  }

  static bool ArrayTest4() {
    try {
      var i = 100;
      var arr = new int[i];
      var bogus = arr[i];
    } catch (System.IndexOutOfRangeException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool ArrayTest5() {
    try {
      var i = 100;
      var arr = new int[i];
      var bogus = arr[-i];
    } catch (System.IndexOutOfRangeException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool ArrayTest6() {
    var arr = new BaseClass[10];
    arr[0] = new DerivedClass();
    return arr is BaseClass[];
  }
}

class BaseClass {
}

class DerivedClass : BaseClass {
}