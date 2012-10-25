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

public static class VirtualCalls {

  public static int Main() {
    int ret;
    ret = InterfaceInvocation();
    if (ret != 0) return ret;

    ret = InvokeOveriddenMethods1();
    if (ret != 0) return ret;

    ret = InvokeOveriddenMethods2();
    if (ret != 0) return ret;

    ret = ExplicitInterfaceMemberImplementations();
    if (ret != 0) return ret;

    return 0;
  }

  private static int InterfaceInvocation() {

    // Return values 1 - 15
    int ret;

    IPoint p = new MyPoint();
    if (!InterfaceInvocation1(p)) return 1;
    if (!InterfaceInvocation2(p)) return 2;
    if (!InterfaceInvocation3(p)) return 3;
    if (!InterfaceInvocation4(p)) return 4;
    if (!InterfaceInvocation5(p)) return 5;

    IPoint1 point1 = new MyPoint2();

    if (!InterfaceInvocation6(point1)) return 6;
    if (!InterfaceInvocation7(point1)) return 7;
    if (!InterfaceInvocation8(point1)) return 8;
    if (!InterfaceInvocation9(point1)) return 9;
    if (!InterfaceInvocation10(point1)) return 10;

    IPoint9 point9 = (IPoint9)point1;

    if (!InterfaceInvocation11(point9)) return 11;
    if (!InterfaceInvocation12(point9)) return 12;
    if (!InterfaceInvocation13(point9)) return 13;
    if (!InterfaceInvocation14(point9)) return 14;
    if (!InterfaceInvocation15(point9)) return 15;


    if (!InterfaceInvocation16()) return 16;

    return 0;
  }

  private static int InvokeOveriddenMethods1() {

    // Return values 20 - 23
    int ret = 0;
    DerivedClass derivedClass = new DerivedClass();
    BaseClass baseClass = derivedClass;
    ret = baseClass.F();
    if (ret != 1)
      return 20;
    ret = derivedClass.F();
    if (ret != 3)
      return 21;
    ret = baseClass.G();
    if (ret != 4)
      return 22;
    ret = derivedClass.G();
    if (ret != 4)
      return 23;
    return 0;
  }

  private static int InvokeOveriddenMethods2() {

    // Return values 30 - 33
    int ret = 0;
    D d = new D();
    A a = d;
    B b = d;
    C c = d;
    ret = a.F();
    if (ret != 2)
      return 30;
    ret = b.F();
    if (ret != 2)
      return 31;
    ret = c.F();
    if (ret != 4)
      return 32;
    ret = d.F();
    if (ret != 4)
      return 33;
    return 0;
  }

  private static int ExplicitInterfaceMemberImplementations() {

    // Return values 30 - 33
    FilterInterface f = new FilterImplementation(5);
    bool result = f.Filter();
    if (!result)
      return 40;
    f = new FilterImplementation(-5);
    result = f.Filter();
    if (result)
      return 41;
    return 0;
  }



  private static bool InterfaceInvocation1(IPoint p) {
    int ret = p.getX();
    if (ret != 5) return false;
    return true;
  }

  private static bool InterfaceInvocation2(IPoint p) {
    int ret = p.getY();
    if (ret != 2) return false;
    return true;
  }

  private static bool InterfaceInvocation3(IPoint p) {
    int ret = p.getXPlusY();
    if (ret != 7) return false;
    return true;
  }

  private static bool InterfaceInvocation4(IPoint p) {
    int ret = p.getXMinusY();
    if (ret != 3) return false;
    return true;
  }

  private static bool InterfaceInvocation5(IPoint p) {
    int ret = p.getXMultY();
    if (ret != 10) return false;
    return true;
  }

  private static bool InterfaceInvocation6(IPoint1 p) {
    int ret = p.getX1();
    if (ret != 5) return false;
    return true;
  }

  private static bool InterfaceInvocation7(IPoint1 p) {
    int ret = p.getY1();
    if (ret != 2) return false;
    return true;
  }

  private static bool InterfaceInvocation8(IPoint1 p) {
    int ret = p.getXPlusY1();
    if (ret != 7) return false;
    return true;
  }

  private static bool InterfaceInvocation9(IPoint1 p) {
    int ret = p.getXMinusY1();
    if (ret != 3) return false;
    return true;
  }

  private static bool InterfaceInvocation10(IPoint1 p) {
    int ret = p.getXMultY1();
    if (ret != 10) return false;
    return true;
  }

  private static bool InterfaceInvocation11(IPoint9 p) {
    int ret = p.getX9();
    if (ret != 2) return false;
    return true;
  }

  private static bool InterfaceInvocation12(IPoint9 p) {
    int ret = p.getY9();
    if (ret != 5) return false;
    return true;
  }

  private static bool InterfaceInvocation13(IPoint9 p) {
    int ret = p.getXPlusY9();
    if (ret != 7) return false;
    return true;
  }

  private static bool InterfaceInvocation14(IPoint9 p) {
    int ret = p.getXMinusY9();
    if (ret != -3) return false;
    return true;
  }

  private static bool InterfaceInvocation15(IPoint9 p) {
    int ret = p.getXMultY9();
    if (ret != 10) return false;
    return true;
  }

  private static bool InterfaceInvocation16() {
    IPoint p = null;
    try {
      p.getX();
    } catch (Exception e) {
      return true;
    }
    return false;
  }
}

interface IPoint {
  int getX();
  int getY();
  int getXPlusY();
  int getXMinusY();
  int getXMultY();
}

class MyPoint : IPoint {
  private int x = 5;
  private int y = 2;
  public virtual int getX() {
    return x;
  }

  public virtual int getY() {
    return y;
  }

  public virtual int getXPlusY() {
    return x + y;
  }

  public virtual int getXMinusY() {
    return x - y;
  }

  public virtual int getXMultY() {
    return x * y;
  }
}

interface IPoint1 {
  int getX1();
  int getY1();
  int getXPlusY1();
  int getXMinusY1();
  int getXMultY1();
}

interface IPoint2 {
  int getX2();
  int getY2();
  int getXPlusY2();
  int getXMinusY2();
  int getXMultY2();
}

interface IPoint3 {
  int getX3();
  int getY3();
  int getXPlusY3();
  int getXMinusY3();
  int getXMultY3();
}

interface IPoint4 {
  int getX4();
  int getY4();
  int getXPlusY4();
  int getXMinusY4();
  int getXMultY4();
}

interface IPoint5 {
  int getX5();
  int getY5();
  int getXPlusY5();
  int getXMinusY5();
  int getXMultY5();
}

interface IPoint6 {
  int getX6();
  int getY6();
  int getXPlusY6();
  int getXMinusY6();
  int getXMultY6();
}

interface IPoint7 {
  int getX7();
  int getY7();
  int getXPlusY7();
  int getXMinusY7();
  int getXMultY7();
}

interface IPoint8 {
  int getX8();
  int getY8();
  int getXPlusY8();
  int getXMinusY8();
  int getXMultY8();
}

interface IPoint9 {
  int getX9();
  int getY9();
  int getXPlusY9();
  int getXMinusY9();
  int getXMultY9();
}

interface IPoint10 {
  int getX10();
  int getY10();
  int getXPlusY10();
  int getXMinusY10();
  int getXMultY10();
}



class MyPoint2 : IPoint1, IPoint9 {
  private int x1 = 5;
  private int y1 = 2;
  public virtual int getX1() {
    return x1;
  }

  public virtual int getY1() {
    return y1;
  }

  public virtual int getXPlusY1() {
    return x1 + y1;
  }

  public virtual int getXMinusY1() {
    return x1 - y1;
  }

  public virtual int getXMultY1() {
    return x1 * y1;
  }

  private int x9 = 2;
  private int y9 = 5;

  public virtual int getX9() {
    return x9;
  }

  public virtual int getY9() {
    return y9;
  }

  public virtual int getXPlusY9() {
    return x9 + y9;
  }

  public virtual int getXMinusY9() {
    return x9 - y9;
  }

  public virtual int getXMultY9() {
    return x9 * y9;
  }
}

class BaseClass {
  public int F() { return 1; }
  public virtual int G() { return 2; }
}
class DerivedClass : BaseClass {
  new public int F() { return 3; }
  public override int G() { return 4; }
}

class A {
  public virtual int F() { return 1; }
}
class B : A {
  public override int F() { return 2; }
}
class C : B {
  new public virtual int F() { return 3; }
}
class D : C {
  public override int F() { return 4; }
}

public interface FilterInterface {
  bool Filter();
}

public class FilterImplementation : FilterInterface {

  public int value;
  public FilterImplementation(int i) {
    this.value = i;
  }

  bool FilterInterface.Filter() {
    return FilterImplementationFilter();
  }

  public bool FilterImplementationFilter() {
    if (this.value < 0)
      return false;
    return true;
  }

  public bool Filter() {
    if (this.value < 0)
      return true;
    return false;
  }
}