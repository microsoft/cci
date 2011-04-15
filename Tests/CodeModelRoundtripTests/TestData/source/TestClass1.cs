using System;
using System.Collections.Generic;
using System.Text;

namespace RoundtripTests.TestData.source {
  class TestClass1<T> {
    T t;
    public TestClass1(T t)
      : base() {
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

      int[] A = new int[] { 3, 4, 5 };
      int[] B = new int[] { 6, 7 };
      var e = Iterator(A, B);
      var s = "";
      while (e.MoveNext()) {
        var x = e.Current;
        s += x;
      }
      Console.WriteLine("Iterator returned '{0}'", s);

      // Test to make sure that loops within anonymous delegates are properly preserved
      Console.Write("LoopInAnonymousDelegate: ");
      DoActionOnThree(
        i => { for (int j = 0; j < i; j++) Console.Write(j.ToString()); }
      );
      Console.WriteLine();

      // Test for nested closures
      NestedClosure<int, string>(new List<int> { 3, 4, 5 },
                      i => { var xs = new List<string>(); for (int j = 0; j < i; j++) xs.Add(i.ToString()); return xs; },
                      ncs => Console.WriteLine(ncs)
                     );

      // Test for code that creates "ldarg0; dup" IL within a closure, such as ++
      Console.WriteLine("++ in closure returned {0}", Count(A));

      // Test to make sure that signed comparisons remain signed
      Console.WriteLine("8 >= -1 is: {0}", XAtLeastNegativeOne(8));
      Console.WriteLine("8 >= -1 is: {0}", XAtLeastNegativeOneAndLessThanNinetyNine(8));

      try {
        Console.WriteLine(Difference('a', 'd'));
      } catch (OverflowException) {
        Console.WriteLine("Difference('a','d') caused an overflow exception");
      }
      try {
        Console.WriteLine(Difference((byte)3, (byte)4));
      } catch (OverflowException) {
        Console.WriteLine("Difference((byte)3, (byte)4)) caused an overflow exception");
      }

      List<string> strings = new List<string> { "cat", "dog" };
      var closure = ReturnClosureWhoseReturnTypeIsGenericMethodParameter(strings, 1);
      var elementFromStringsOffsetByOne = closure(0);
      Console.WriteLine("Testing ReturnClosureWhoseReturnTypeIsGenericMethodParameter: {0}", elementFromStringsOffsetByOne);

      var yieldFooResults = "";
      foreach (var yfr in YieldFooOrBar(true)) {
        yieldFooResults += yfr;
      }
      Console.WriteLine("IEnumerable test: '{0}'", yieldFooResults);

      Console.WriteLine("Nested closure where inner closure captures a parameter of the outer.");
      NestedClosureCapturingOuterParameter(new List<List<int>>() { new List<int>() { 3, 4, 5 }, new List<int>() { 10, 11, 12 } });

      Console.WriteLine("Test for nested anonymous delegate that is within a statement");
      NestedClosureInStatementCapturingLocal(3);

    }
    // Not to execute, just to go through decompilation and then peverify.
    public IntPtr Method22(object o) {
      return (IntPtr)o;
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
      if ((x>0) || (x>= y) && !(x<3) && (x<=3) || (x ==0))
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
      T1[] arr = new T1[1] { t };
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

    public IEnumerator<U> Iterator<U>(IEnumerable<U> first, IEnumerable<U> second) {
      foreach (var x in first)
        yield return x;
      foreach (var x in second)
        yield return x;
    }

    public void DoActionOnThree(Action<int> action) {
      action(3);
    }

    public static void NestedClosure<NC1, NC2>(
                    List<NC1> ts,
                    Func<NC1, List<NC2>> generateListOfUFromT,
                    Action<NC2> actionOnU) {
      ts.ForEach(outerElt =>
           generateListOfUFromT(outerElt).ForEach(innerElt => actionOnU(innerElt))
           );
    }

    public static bool ForEach<U>(IEnumerable<U> source, Func<U, bool> pred) {
      foreach (U item in source) {
        if (!pred(item))
          return false;
      }
      return true;
    }
    public static int Count<U>(IEnumerable<U> source) {
      int count = 0;
      ForEach(source, delegate(U item) {
        count++;
        return true;
      });
      return count;
    }

    public static string ConvertBoolToTrueFalse(bool b) {
      return b ? "true" : "false";
    }
    public static string XAtLeastNegativeOne(int x) {
      // Need to pass the comparison to a method for the bug to show itself
      // This one leads to a clt (signed) instruction
      return ConvertBoolToTrueFalse(x >= -1);
    }
    public static string XAtLeastNegativeOneAndLessThanNinetyNine(int x) {
      // Need to pass the comparison to a method for the bug to show itself
      // This one leads to a blt (signed) instruction
      return ConvertBoolToTrueFalse(x >= -1 && x < 99);
    }

    public static int Difference(char x, char y) {
      return checked(x - y);
    }
    public static int Difference(byte x, byte y) {
      return checked(x - y);
    }

    public Func<int, U> ReturnClosureWhoseReturnTypeIsGenericMethodParameter<U>(List<U> xs, int j) {
      return i => xs[i + j];
    }

    public IEnumerable<string> YieldFooOrBar(bool yieldFoo) {
      if (yieldFoo)
        yield return "foo";
      else
        yield return "bar";
    }

    public int NestedClosureCapturingOuterParameter<U>(List<List<U>> xss) {
      xss.ForEach(xs => xs.ForEach(x => Console.WriteLine("{0} is at index {1}", x, xs.IndexOf(x))));
      return xss.Count;
    }

    public void DoAction(Action a) { a(); }
    public void NestedClosureInStatementCapturingLocal(int x) {
      var outerAction = new Action(delegate() {
        try {
          var y = x + 10;
          var innerAction = new Action(() => Console.WriteLine("inner delegate sez x: " + x + " y: " + y));
          DoAction(innerAction);
        } catch (Exception exception) {
          var innerAction = new Action(() => Console.WriteLine("inner delegate caught an exception" + exception.Message));
          DoAction(innerAction);
        }
      });
      DoAction(outerAction);
    }
  }
  class C {
    public static void Main(string[] args) {
      TestClass1<int> obj = new TestClass1<int>(10);
      obj.Print();
    }
  }
}
