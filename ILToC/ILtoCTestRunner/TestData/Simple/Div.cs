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
using System;

public static class Div {
  public static int Main() {
    int i = 0;
    int ret = 0;
    bool error = true;
    try {
      ret = DoDiv1();
      if (ret != 0) {
        return ret;
      }
      i++;
      ret = DoDiv2();
      if (ret != 0) {
        return ret;
      }
      i++;
      ret = DoDiv3();
      if (ret != 0) {
        return ret;
      }
      i++;
      ret = DoDiv4();
      if (ret != 0) {
        return ret;
      }
      i++;
    } catch (ArithmeticException) {
      if (i == 3) {
        error = false;
      }
    }
    if (error) {
      return 100;
    }
    error = true;


    try {
      ret = DoDiv5();
      if (ret != 0) {
        return ret;
      }
      i++;
      ret = DoDiv6();
      if (ret != 0) {
        return ret;
      }
      i++;
      ret = DoDiv7();
      if (ret != 0) {
        return ret;
      }
      i++;
    } catch (ArithmeticException) {
      if (i == 5) {
        error = false;
      }
    }
    if (error) {
      return 200;
    }
    error = true;

    try {
      DoDiv8();
    } catch (DivideByZeroException) {
      error = false;
    }
    if (error) {
      return 300;
    }

    try {
      DoDiv9();
    } catch (DivideByZeroException) {
      error = false;
    }
    if (error) {
      return 400;
    }

    try {
      DoDiv10();
    } catch (DivideByZeroException) {
      error = false;
    }
    if (error) {
      return 500;
    }

    return 0;
  }

  // Should not return a exception
  static int DoDiv1() {
    int x = 100;
    int y = 5;
    int z = x / y;
    return z == 20 ? 0 : 1;
  }

  // Should not return a exception
  static int DoDiv2() {
    Int32 x = Int32.MaxValue;
    Int32 y = 1;
    Int32 z = x / y;
    return z == Int32.MaxValue ? 0 : 2;
  }

  // Should not return a exception
  static int DoDiv3() {
    Int32 x = Int32.MinValue;
    Int32 y = 1;
    Int32 z = x / y;
    return z == Int32.MinValue ? 0 : 3;
  }

  // Should not return a ArithmeticException
  static int DoDiv4() {
    Int32 x = Int32.MinValue;
    Int32 y = -1;
    Int32 z = x / y;
    return z == Int32.MinValue ? 0 : 2;
  }

  // Should not return a exception
  static int DoDiv5() {
    Int64 x = Int64.MaxValue;
    Int64 y = 1;
    Int64 z = x / y;
    return z == Int64.MaxValue ? 0 : 5;
  }

  // Should not return a exception
  static int DoDiv6() {
    Int64 x = Int64.MinValue;
    Int64 y = 1;
    Int64 z = x / y;
    return z == Int64.MinValue ? 0 : 6;
  }

  // Should not return a ArithmeticException
  static int DoDiv7() {
    Int64 x = Int64.MinValue;
    Int64 y = -1;
    Int64 z = x / y;
    return z == Int64.MinValue ? 0 : 7;
  }

  // Should not return a DivideByZeroException
  static int DoDiv8() {
    int x = 5;
    int y = 0;
    int z = x / y;
    return 1;
  }

  // Should not return a DivideByZeroException
  static int DoDiv9() {
    Int32 x = 5;
    Int32 y = 0;
    Int32 z = x / y;
    return 1;
  }

  // Should not return a DivideByZeroException
  static int DoDiv10() {
    Int64 x = 5;
    Int64 y = 0;
    Int64 z = x / y;
    return 1;
  }
}
