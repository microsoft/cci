using System;
using System.Collections.Generic;

class MarkedForCopyAttribute : System.Attribute {
}

class NamedAttribute : System.Attribute {
  public string Name {
    get { return name; }
    set { name = value; }
  }
  string name;
}

namespace Resolution {

  class Outer<A> {

    public class Mid1<T> {
      public class Inner1 { }
    }
    public class Mid<T> {
      // Fields of SpecializedNestedType
      public Inner2 f0;
      public Mid1<int>.Inner1 f1;
      public Mid<int>.Inner2 f4;
      public Outer<T>.Mid<T>.Inner2 f5;

      //// Fields of GenericTypeInstance
      public Mid<int>.Inner<T> f2;
      public Outer<int>.Mid<T>.Inner<T> f3;
      public Outer<T>.Mid<int>.Inner2.InnerMost<T> f6;

      public T f7;

      // Method with type references mentioned above
      public Outer<int>.Mid<T>.Inner<T> bar1(Mid1<int>.Inner1 p1, Mid<int>.Inner2 p2, Inner2 p3, Outer<T>.Mid<int>.Inner2.InnerMost<T> p4) {
        return null;
      }

      public class Inner<T2> {
        // Fields with types that refer to T2
        public T2 f1;
        public Inner<T2> f3;
        public T2[] f4;
        public Mid<T2> f5;
        public Outer<T2> f6;
        public Outer<T2>.Mid<T2>.Inner2 f7;
        public Outer<T2>.Mid<T2>.Inner<T2> f8;

        // Fields with types that refer to outer type parameters
        public T f9;
        public A f10;
        public Outer<T>.Mid<A> f11;
        public Mid<int>.Inner2.InnerMost<T2> f12;
        public Mid<T2>.Inner<T> f13;
        public Mid<int>.Inner3<T> f14;

        public Inner<int> f2;

        [MarkedForCopy]
        public void bar1(T2 t) {
        }
        [MarkedForCopy]
        public void bar2(Inner<T2> t) {
        }
        [MarkedForCopy]
        public void bar3(Mid<T2> t) {
        }
        public void bar4(Mid<T> t) {
        }
        public void bar5(Mid<T2[]> t) {
        }
        [MarkedForCopy]
        public void bar6(Inner<T2[]> t) {
          int[] x;
          string[] y;
          System.DateTime dateTime = System.DateTime.Now;
        }
        [MarkedForCopy]
        public T3 bar7<T3>(T3 t3) {
          return t3;
        }

        [MarkedForCopy]
        public Inner<T3> bar8<T3>(Inner<T3> t3) {
          return t3;
        }

        public T3 bar9<T3>(T3[] t3s) {
          return t3s[0];
        }

        public Inner<T3> bar10<T3>(Inner<T3>[] innert3s) {
          return innert3s[0];
        }

        [MarkedForCopy]
        public T3 bar11<T3>(T3 t3, T2 t2) {
          return t3;
        }
        [MarkedForCopy]
        public Outer<int>.Mid<T3>.Inner2 bar12<T3>(Outer<T3>.Mid<T3>.Inner2 p0, Outer<T3>.Mid<T3>.Inner2.InnerMost<T3> p1, Inner<T3> p2, Outer<A>.Mid<T2>.Inner<T3> p3) {
          return null;
        }
      }

      public struct Inner3<T4> {
        // Fields with types that refer to T2
        public T4 f1;
        public Inner<T4> f3;
        public T4[] f4;
        public Mid<T4> f5;
        public Outer<T4> f6;
        public Outer<T4>.Mid<T4>.Inner2 f7;
        public Outer<T4>.Mid<T4>.Inner<T4> f8;

        // Fields with types that refer to outer type parameters
        public T f9;
        public A f10;
        public Outer<T>.Mid<A> f11;
        public Mid<int>.Inner2.InnerMost<T4> f12;
        public Mid<T4>.Inner<T> f13;
      }

      public class Inner2 {
        //refers to type parameters outside
        [MarkedForCopy]
        public A f1;
        [MarkedForCopy]
        public Outer<T> f2;
        [MarkedForCopy]
        public T f3;
        [MarkedForCopy]
        public Mid<A>.Inner<T> f4;
        [MarkedForCopy]
        public Mid<int>.Inner2.InnerMost<T> f5;

        public T3 bar7<T3>(T3 t3) {
          return t3;
        }

        public Inner<T3> bar8<T3>(Inner<T3> t3) {
          return t3;
        }

        [MarkedForCopy]
        public T3 bar9<T3>(T3[] t3s) {
          return t3s[0];
        }

        public Inner<T3> bar10<T3>(Inner<T3>[] innert3s) {
          return innert3s[0];
        }

        public T3 bar11<T3>(T3 t3, T t) {
          return t3;
        }

        public class InnerMost<T2> {
          public T2 f1;
          public Inner<int> f2;
          public InnerMost<T2> f3;
          public T2[] f4;
          [MarkedForCopy]
          public Inner<T2> f5;
          [MarkedForCopy]
          public Outer<T2>.Mid<T>.Inner2 f6;
          [MarkedForCopy]
          public Outer<T>.Mid<T2>.Inner<int> f7;
          [MarkedForCopy]
          public Outer<T2>.Mid<T>.Inner2.InnerMost<int> f8;

          public void bar1(T2 t) {
          }
          public void bar2(InnerMost<T2> t) {
          }
          public void bar3(Mid<T2> t) {
          }
          public void bar4(Mid<T> t) {
          }
          public void bar5(Mid<T2[]> t) {
          }
          public void bar6(InnerMost<T2[]> t) {
          }
          public T3 bar7<T3>(T3 t3) {
            return t3;
          }
          public InnerMost<T3> bar8<T3>(InnerMost<T3> t3) {
            return t3;
          }

          [MarkedForCopy]
          public T3 bar9<T3>(T3[] t3s) {
            return t3s[0];
          }

          [MarkedForCopy]
          public InnerMost<T3> bar10<T3>(InnerMost<T3>[] innert3s) {
            return innert3s[0];
          }

          public T3 bar11<T3>(T3 t3, T2 t2) {
            return t3;
          }

          [MarkedForCopy]
          public Outer<int>.Mid<T3>.Inner2 bar12<T3>(Outer<T3>.Mid<T3>.Inner2.InnerMost<T3> p1, InnerMost<T3> p2, Outer<A>.Mid<T2>.Inner<T3> p3) {
            return null;
          }

          // Similar to test1a, just in the inner class.
          [MarkedForCopy]
          public void test2g<T1>(T1 t) {
            InnerMost<T1> x = new InnerMost<T1>();
            x.bar1(t);
            x.bar2(x);
            Mid<T1> y = new Mid<T1>();
            x.bar3(y);
            Mid<T> z = new Mid<T>();
            x.bar4(null);
            x.bar5(null);
            x.bar6(null);
            T1 a = x.bar7(t);
            x.bar8(x);
            x.bar9(new T1[1] { t });
            x.bar10(new InnerMost<T1>[1] { x });
            x.bar11(t, t);
            x.bar12<T1>(null, null, null);
            var v1 = x.f1;
            var v2 = x.f2;
            var v3 = x.f3;
            var v4 = x.f4;
            var v5 = x.f5;
            var v6 = x.f6;
            var v7 = x.f7;
            var v8 = x.f8;
          }
        }
      }

      // Basic tests: generic instance, different forms of type references, combined with different nested
      // generic/nongeneric classes. Resolution of fields and methods.
      public void test1a<T1>(T1 t, T tt) {
        Inner<T1> x = new Inner<T1>();
        x.bar1(t);
        x.bar2(x);
        Mid<T1> y = new Mid<T1>();
        x.bar3(y);
        x.bar4(null);
        x.bar5(null);
        x.bar6(null);
        T1 a = x.bar7(t);
        x.bar8(x);
        x.bar9(new T1[1] { t });
        x.bar10(new Inner<T1>[1] { x });
        x.bar11(t, t);
        x.bar12<T1>(null, null, null, null);

        var v1 = x.f1;
        var v2 = x.f2;
        var v3 = x.f3;
        var v4 = x.f4;
        var v5 = x.f5;
        var v6 = x.f6;
        var v7 = x.f7;
        var v8 = x.f8;
        var v9 = x.f9;
        var v10 = x.f10;
        var v11 = x.f11;
        var v12 = x.f12;
        var v13 = x.f13;
        var v14 = x.f14;

        //Inner2 x2 = new Inner2();
        //x2.bar7(t);
        //Inner<T1> x3 = x2.bar8(x);
        //x2.bar9(new T1[1] { t });
        //x2.bar10(new Inner<T1>[] { x });
        //x2.bar11(t, tt);
      }

      // From Mid<T> referring to nested classes. Testing specialization of specialized nested type references. 
      [MarkedForCopy]
      void test1b(Mid<int> z) {
        var v00 = z.f0;
        var v01 = z.f1;
        var v02 = z.f2;
        var v03 = z.f3;
        var v04 = z.f4;
        var v05 = z.f5;
        var v06 = z.f6;
        z.bar1(null, null, null, null);
      }

      // Fields of nested structs.
      void test1c(Inner3<T> x) {
        var v1 = x.f1;
        var v3 = x.f3;
        var v4 = x.f4;
        var v5 = x.f5;
        var v6 = x.f6;
        var v7 = x.f7;
        var v8 = x.f8;
        var v9 = x.f9;
        var v10 = x.f10;
        var v11 = x.f11;
        var v12 = x.f12;
        var v13 = x.f13;
      }

      // Test super classes and constraints
      void test1d(Mid2<Mid<int>, Mid<Mid<int>>.Inner<Mid<int>>> y1) {
        var v001 = y1.g1;
        var v002 = y1.g2;
        var v003 = y1.g3;
        var v004 = y1.g4;
        var v0041 = y1.g4.f1;
        var v0042 = y1.g4.f2;
        var v0043 = y1.g4.f3;
        var v005 = y1.g5;
        var v0051 = y1.g5.f1;
        var v0052 = y1.g5.f2;
        var v0053 = y1.g5.f3;
      }

      // A bug
      void test1e(Mid<Mid<int>.Inner2> y2) {
        var v0001 = y2.f7.f3;
      }

      // Test Outer<A>.Mid.Inner { T }
      [MarkedForCopy]
      void test1f(Mid<int>.Inner2 x) {
        var v1 = x.f1;
        var v2 = x.f2;
        var v3 = x.f3;
        var v4 = x.f4;
        var v5 = x.f5;
      }
    }

    // super classes and constraints
    public class Mid2<T1, T2> : Outer<T2>.Mid1<T1>.Inner1
      where T1 : Mid<int>
      where T2 : Mid<T1>.Inner<T1> {
      public Mid<T1>.Inner2 g1;
      public Mid<T2>.Inner2.InnerMost<T2> g2;
      public Outer<T2>.Mid<T1>.Inner<int> g3;
      public T1 g4;
      public T2 g5;

      [MarkedForCopy]
      string foo(Outer<string>[][] p, Outer<int>[] q) {
        return "";
      }

      [MarkedForCopy]
      int Foo {
        get { return 1; }
        set { }
      }
    }

    [MarkedForCopy]
    [System.Diagnostics.DebuggerTypeProxy(typeof(string))]
    public class Metronome {
      public event TickHandler Tick;
      public delegate void TickHandler(Metronome m);
      public void Start() {
      }
    }
  }

  namespace Nested1 {
    [MarkedForCopy]
    class D {
      Resolution.Nested2.D d;
      void foo() {
        unsafe {
          char* foo;
        }
      }
    }
  }
  namespace Nested2 {
    [MarkedForCopy]
    [Named(Name = "hello")]
    class D {
    }
  }
}

namespace N {

  class A<T> {
    T f<T>(T[] ts) {
      return ts[0];
    }

    void g<T1>(T1 t) {
      A<T> a = new A<T>();
      a.f(new T1[1] { t });
    }
  }
}

namespace Test1 {
  class A<T> {
    private void AnonymousDelegateTest1(int t) {
      List<int> list = new List<int>();
      list.Add(t);
      var b = list.TrueForAll((int t1) => t1 == t);
      Console.WriteLine("Test anonymous delegate, b = {0}.", b);
    }
  }
}

/// Test nested anonymous delegate. 
namespace Test2 {
  class A {
    private void AnonymousDelegateTest1(int t) {
      List<int> list = new List<int>();
      list.Add(t);
      var b = list.TrueForAll(delegate(int t1) {
        List<int> list1 = new List<int>();
        return list1.TrueForAll(delegate(int t2) {
          return (t1 == t) || (t1 == t2);
        });
        //return true;
      });
      Console.WriteLine("Test anonymous delegate, b = {0}.", b);
    }
  }
}

class MatrixInitialization {
  private void MatrixInitializationTest0() {
    int[,] matrix = new int[2, 3] { { 10, 11, 12 }, { 13, 14, 15 } };
  }
  private void MatrixInitializationTest1() {
    char[, ,] matrix = new char[3, 2, 3]
    {
    { { 'a', 'b', 'c' }, { 'd', 'e', 'f' } },
    { { 'g', 'h', 'i' }, { 'j', 'k', 'l' } },
    { { 'm', 'n', 'o' }, { 'p', 'q', 'r' } }
    };
  }
  private void MatrixInitializationTest2() {
    char[, ,] matrix = new char[2, 3, 4]
    {
    { { 'a', 'b', 'c', 'd' }, { 'e', 'f', 'g', 'h' }, { 'i', 'j', 'k', 'l' } },
    { { 'm', 'n', 'o', 'p' }, { 'q', 'r', 's', 't' }, { 'u', 'v', 'w', 'x' } },
    };
  }
}


