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
public static class CheckedAddInt32 {
  public static int Main() {
    int result = -1;
    Int32 a, b, c = 0;

    a = 10;
    b = 20;

    try {
      c = checked(a + b);
      result = 0;
    } catch (System.OverflowException) {
      result = 1;
    }

    if (result == 1 || c != 30) {
      return 1;
    }

    a = Int32.MinValue;
    b = 10;

    try {
      c = checked(a + b);
      result = 0;
    } catch (System.OverflowException) {
      result = 1;
    }

    if (result == 1 || c != -2147483638) {
      return 2;
    }

    a = Int32.MinValue;
    b = Int32.MaxValue;

    try {
      c = checked(a + b);
      result = 0;
    } catch (System.OverflowException) {
      result = 1;
    }

    if (result == 1 || c != -1) {
      return 3;
    }

    a = Int32.MinValue;
    b = 0;

    try {
      c = checked(a + b);
      result = 0;
    } catch (System.OverflowException) {
      result = 1;
    }

    if (result == 1 || c != Int32.MinValue) {
      return 4;
    }

    a = Int32.MaxValue;
    b = 0;

    try {
      c = checked(a + b);
      result = 0;
    } catch (System.OverflowException) {
      result = 1;
    }

    if (result == 1 || c != Int32.MaxValue) {
      return 5;
    }

    a = Int32.MinValue;
    b = -1;

    try {
      c = checked(a + b);
      result = 0;
    } catch (System.OverflowException) {
      result = 1;
    }

    if (result != 1) {
      return 4;
    }

    a = Int32.MaxValue;
    b = 1;
    try {
      c = checked(a + b);
      result = 5;
    } catch (System.OverflowException){
      result = 0;
    }
    return result;
  }
}