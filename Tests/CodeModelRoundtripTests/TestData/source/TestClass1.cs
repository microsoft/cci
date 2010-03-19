using System;
using System.Collections.Generic;
using System.Text;

namespace RoundtripTests.TestData.source {
  class TestClass1<T> {
    T t;
    public TestClass1(T t): base() {
      this.t = t;
    }
    public void Print() {
      ControlFlowTest1();
      ControlFlowTest2();
      ExpressionTest1();
      ExpressionTest2();
      ExpressionTest3();
      MethodCallTest1(this.t);
      T t1 = this.t;
      T t2;
      MethodCallTest2(this.t, ref t1, out t2);
      Console.WriteLine("Back from method call test2, we have t2 as {0}.", t2);
      Console.WriteLine("I have {0}.", this.t);
    }
    private bool IsSame(T t) {
      return true;
    }
    private int GetInt(T t) {
      return 2;
    }
    /// <summary>
    /// Test if, for, while and switch
    /// </summary>
    private void ControlFlowTest1() {
      if (this.IsSame(this.t)) {
        Console.WriteLine("If test passed");
      }
      for (int i = 0; i < 2; i++) {
        if (i == 0) continue;
        if (i == 3) break;
        Console.WriteLine("While test pass {0} I have {1}.", i, this.t);
      }
      switch (this.GetInt(this.t)) {
        case 1: Console.WriteLine("Switch test hits case 1."); break;
        default: Console.WriteLine("Switch test hits default case we have {0}.", this.t);
          break;
      }
      int y=0;
      int x = 0;
    }
    /// <summary>
    /// Test try catch finally
    /// </summary>
    private void ControlFlowTest2() {
      try {
        Console.WriteLine("Test Try. We have {0}.", this.t);
        throw new Exception();
      } catch {
        Console.WriteLine("Test Catch. We have {0}.", this.t);
        //rethrow?
      } finally {
        Console.WriteLine("Test Finally. We have {0}.", this.t);
      }
    }
    /// <summary>
    /// Test arrays, initializers, 
    /// </summary>
    private void ExpressionTest1() {
      T[] arr = new T[1] { this.t };
      Console.WriteLine("Test array. We have {0}.", arr[0]);
    }

    /// <summary>
    /// Test arithmetics
    /// </summary>
    private void ExpressionTest2() {
      byte a=1, b=2;
      int x=3, y=4;
      long l1=1L, l2=2L;
      float f1=3.0F, f2=1.0F;
      double d1=3.0, d2=3.0;
      char c1='1', c2='1';
      var r = (a + b) + (a + x) - (l1 - l2) * (f1 / f2) + (d1 * x) + (c1 / c2) + (a - d1);
      Console.WriteLine("Test arithmetic. Result is {1}. We have {0}.", this.t, r);
      r = +x;
      r += x;
      r -= x;
      r++;
      r--;
      r = ~x;
      r = -r;
      r = x >> 2;
      r = x << 2;
      Console.WriteLine("Test arithmetic. Result is {0}.", r);
      r = x | 0xFFFF;
      r = x & 0xFFFF;
      r = x % 2;
      if ((x>0) || (x>= y) && !(x<3) && (x<=3) || (x ==0) )
        x++;
      else
        x--;
    }

    int i;
    private unsafe void foo(int* p) {
      int** pp = &p;
    }
    private void bar() {
    }
    /// <summary>
    /// Test misc expressions, cast, de-ref, etc.
    /// </summary>
    private unsafe void ExpressionTest3() {
      this.i = 4;
      int[] arr = new int[3];
      fixed (int* px = &this.i)
      fixed (int* px1 = &arr[2]) {
        Console.WriteLine("Test fixed. We have {0}.", *px);
      }
    }

    /// <summary>
    /// Test is/as default, sizeof, typeof
    /// </summary>
    private void ExpressionTest4() {
      var x = typeof(int);
      var y = sizeof(int);
      var t = default(T);
      string t2 = t as string;
      if (y is string)
        return;
    }

    /// <summary>
    /// Test generic method parameters
    /// </summary>
    private void MethodCallTest1<T1>(T1 t) {
      T1 [] arr = new T1[1] {t};
      Console.WriteLine("Test array whose element type is generic method parameter. We have {0}.", arr[0]);
    }

    private void MethodCallTest2(T t, ref T t1, out T t2) {
      Console.WriteLine("Test method calls, in parameter we have {0}.", t);
      Console.WriteLine("Test method calls, ref parameter we have {0}.", t1);
      t2 = t;
      t1 = t;
    }

    //private void AnonymousDelegateTest1(int t) {
    //  List<int> list = new List<int>();
    //  list.Add(t);
    //  var b = list.TrueForAll((int t1) => t1==t);
    //  Console.WriteLine("Test anonymous delegate, b = {0}.", b);
    //}
  }
  class C {
    public static void Main(string[] args) {
      TestClass1<int> obj = new TestClass1<int>(10);
      obj.Print();
    }
  }
}
