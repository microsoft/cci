using System;

class B {
  protected internal static int intI {
    get {
      return 2;
    }
  }
}

class D : B {
  public static int F() {
    return B.intI;
  }
}

class MyTest {
  static void foo() {
    return;
  }

  static int Main() {
    if (D.F() == 2) {
      return 0;
    } else {
      return 1;
    }
  }
}