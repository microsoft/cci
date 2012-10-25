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
public static class CheckedConvert {
  public static int Main() {
    int j = 1;
    Int16 r1;
    Int32 r2;

    Int64 a = Int16.MaxValue;
    Int64 b = Int32.MaxValue;
    Int32 c = Int16.MaxValue;
    
    r1 = checked((Int16)a);
    r1 = checked((Int16)c);

    r2 = checked((Int32)a);
    r2 = checked((Int32)b);

    a++;
    b++;
    c++;

    try {
      r1 = checked((Int16)a);
    } catch (OverflowException) {
      j = 0;
    }
    if (j == 1) {
      return 2;
    }

    j = 1;

    try {
      r2 = checked((Int16)b);
    } catch (OverflowException) {
      j = 0;
    }
    if (j == 1) {
      return 3;
    }

    return 0;   
  }
}

