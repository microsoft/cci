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
public struct SimpleStruct {
  public int i;
  public int j;
}

public class Delegates {
  public static int Main() {
    if (!InstanceDelegateNoArguments()) return 1;
    if (!InstanceDelegate()) return 2;
    if (!StaticDelegate()) return 3;
    if (!InstanceDelegateWithStruct()) return 4;
    return 0;
  }

  public delegate int Calculate(int value1, int value2);
  public delegate int NoArgDel();
  public delegate int StructDel(SimpleStruct simpleStruct);

  public static bool InstanceDelegateWithStruct() {
    Delegates delegates = new Delegates();
    SimpleStruct simpleStruct = new SimpleStruct();
    simpleStruct.i = 2;
    simpleStruct.j = 3;
    StructDel sd = new StructDel(delegates.StructAsArgument);
    int result = sd(simpleStruct);
    return result == 5 ? true : false;
  }


  public static bool InstanceDelegateNoArguments() {
    Delegates delegates = new Delegates();
    NoArgDel c = new NoArgDel(delegates.NoArguments);
    int result = c();
    return result == 0 ? true : false;
  }

  public static bool InstanceDelegate() {
    Delegates delegates = new Delegates();
    Calculate addDelegate = new Calculate(delegates.Add);
    int result = addDelegate(2, 3);
    return result == 5 ? true : false;
  }

  public static bool StaticDelegate() {
    Calculate staticAddDelegate = new Calculate(Delegates.StaticSub);
    int result = staticAddDelegate(5, 1);
    return result == 4 ? true : false;
  }

  public int StructAsArgument(SimpleStruct simpleStruct) {
    return simpleStruct.i + simpleStruct.j;
  }

  public int NoArguments() {
    return 0;
  }

  public int Add(int a, int b) {
    return a + b;
  }

  public static int StaticSub(int a, int b) {
    return a - b;
  }
}