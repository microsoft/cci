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
public class MyClass {
  public static int staticNum = 2;
}
public class Stack {
  public int m() {
    return MyClass.staticNum;
  }
}

public class StaticFieldAccess {
  public static int Main() {
    int result = 1;
    Stack s = new Stack();
    result = s.m();
    return result == 2 ? 0 : 1;
  }
}