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
public static class CheckedMultiplyInt32 {
  public static int Main() {
    int result = 1;
    Int32 a, b, c = 0;

    a = 10;
    b = 20;

    try {
      c = checked(a * b);
      result = 0;
    } catch (System.OverflowException) {
      result = 1;
    }

    if (result == 1 || c != 200) {
      return 1;
    }    
    return result;
  }
}