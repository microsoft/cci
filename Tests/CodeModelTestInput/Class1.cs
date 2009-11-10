using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Contracts;

namespace CodeModelTestInput {
  public class Class1 {
    public void Method1() {
      byte[] Bytes = new byte[] { 1, 2, byte.MaxValue };
      ushort[] UShorts = new ushort[] { 1, byte.MaxValue, ushort.MaxValue };
      uint[] UInts = new uint[] { byte.MaxValue, ushort.MaxValue, uint.MaxValue };
      ulong[] ULongs = new ulong[] { byte.MaxValue, ushort.MaxValue, uint.MaxValue, ulong.MaxValue };
      sbyte[] SBytes = new sbyte[] { sbyte.MinValue, 1, 2, 3, sbyte.MaxValue };
      short[] Shorts = new short[] { sbyte.MinValue, short.MinValue, short.MaxValue };
      int[] Ints = new int[] { sbyte.MinValue, short.MinValue, int.MinValue };
      long[] Longs = new long[] { sbyte.MinValue, short.MinValue, int.MinValue, long.MinValue };
      char[] Chars = new char[] { 'a', 'b', 'c' };
    }

    public void Method2() {
      byte[] Bytes = new byte[] {byte.MaxValue };
      ushort[] UShorts = new ushort[] { ushort.MaxValue };
      uint[] UInts = new uint[] { uint.MaxValue };
      ulong[] ULongs = new ulong[] { ulong.MaxValue };
      sbyte[] SBytes = new sbyte[] { sbyte.MinValue };
      short[] Shorts = new short[] { short.MinValue };
      int[] Ints = new int[] { int.MinValue };
      long[] Longs = new long[] { long.MinValue };
      char[] Chars = new char[] { 'a'};
      bool[] Bools = new bool[] { true};
      decimal[] Decimals = new decimal[] { 1.1m };
    }

    void Method3() {
      int i = 1;
      int j;
      if (i == 1)
        j = 2;
      else
        j = 3;
      int k = i+j;
    }

    bool Method4(int[] xs, int x) {
      return Contract.ForAll<int>(xs, delegate(int i) { return i < x; });
    }

    void Method5(int[] xs, int x) {
      if (x < xs.Length)
        xs[x] = 3;
    }

    void Method6(int[] xs, int x) {
      if (x < xs.Length)
        xs[x] = 3;
      if (0 < xs.Length)
        xs[0] = x;
    }

    void Method7(int[] xs, int x) {
      if (x < xs.Length)
        xs[x] = 3;
      if (0 < xs.Length)
        xs[0] = x;
      else
        xs = new int[3];
    }

    void Method8(int[] xs, int x) {
      if (0 < xs.Length)
        xs[0] = x;
      else
        xs = new int[3];
      if (x < xs.Length)
        xs[x] = 3;
    }

    void Method9(int[] xs, int x) {
      if (0 < xs.Length && x < xs.Length)
        xs[x] = 3;
    }

    static void Method10(int x) {
      int[] a = new int[x > 0 ? x : 5];
    }

    static void Method11(int x) {
      int[][] a = null;
      a[0] = new int[x > 0 ? x : 5];
    }

    static bool Method12(int x) {
      switch (x) {
        case 1:
          return false;
        case 2:
        case 3:
        case 4:
          return true;
      }
      return false;
    }

    int c=0;
    string Method13() {
      try {
        switch (c) {
          case 0: return "1";
          case 1: try {
              return "2";
            } catch {
              return ("3");
            }
          default: int x = 0;
            try {
              switch (x + c) {
                case 0: return "4";
                case 1: return "5";
                default: return "6";
              }
            } catch {
              return "7";
            }
        }
      } catch {
        return "8";
      }
    }

    void Method14() {
      try {
        return;
      } catch (ApplicationException e) {
        try {
          int x = 4;
          Console.WriteLine(x);
          Console.WriteLine(e);
          Console.WriteLine(e);
        } catch (Exception e1) {
          Console.WriteLine(e1);
          return;
        }
        return;
      } catch (Exception ex) {
        Console.WriteLine(ex);
        return;
      }
    }

    Type Method15() {
      return typeof(Class1);
    }

    int[] Method16(int[] xs) {
      xs[0]++;
      return xs;
    }

    bool Method17(bool A, bool B, bool C, bool D) {
      return A && B || C && D;
    }

    int Method18(int x, int y, int z) {
      int midVal;
      midVal = z;
      if (y < z) {
        if (x < y)
          midVal = y;
        else if (x < z)
          midVal = x;
      }
      return midVal;
    }

    public IEnumerable<int> Method19(int x) {
      for (int i = 0; i < 10; i++) {
        if (i == 8) {
          if (x == i)
            yield return x;
          else
            yield return 8;
        } else {
          if (x > 0) {
            yield break;
          } else {
            if (x < -2) {
              yield break;
            } else yield return i;
          }
        }
      }
    }

  }

  public class Class2 {
    public interface IIncrementable<T> {
      T IncrementBy(int i);
      int Value();
    }
    public class A : IIncrementable<A> {
      public A(string s) {
        this.s = s;
      }
      string s;
      public int Value() {
        return s.Length;
      }
      public A IncrementBy(int i) {
        return this;
      }
    }
    public class Test2 {

      public IEnumerable<string> Test1a(string s) {
        Contract.Requires(s != null);
        Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);
        Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<string>>(), (string s1) => s1 != null));
        yield return "hello";
      }
      public IEnumerable<T> Test1c<T>(T t, IEnumerable<T> ts) {
        if (t.Equals(default(T))) throw new ArgumentException("");
        Contract.Requires(Contract.ForAll(ts, (T t1) => t1.Equals(t)));
        yield return t;
      }
      public IEnumerable<T> Test1d<T>(IEnumerable<T> input) {
        Contract.Requires(input != null);
        Contract.Requires(Contract.ForAll(input, (T s1) => s1 != null));
        Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<T>>(), (T s1) => s1 != null));
        foreach (T t in input)
          yield return t;
      }
      public IEnumerable<T> Test1e<T>(IEnumerable<T> input) {
        Contract.Requires(Contract.ForAll(input, (T s) => s != null));
        foreach (T t in input) {
          yield return t;
        }
      }
      public IEnumerable<int> Test1f(IEnumerable<int> inputArray, int max) {
        Contract.Requires(Contract.ForAll(inputArray, (int x) => x < max));
        Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<int>>(), (int y) => y > 0));
        foreach (int i in inputArray) {
          yield return (max - i - 1);
        }
      }
      public IEnumerable<T> Test1g<T>(IEnumerable<T> ts, T x) {
        Contract.Requires(Contract.ForAll(ts, (T y) => foo(y, x)));
        yield return x;
      }
      bool foo(object y, object x) {
        return y == x;
      }
      public IEnumerable<T> Test1h<T>(IEnumerable<T> input, int x, int y)
        where T : IIncrementable<T> {
        Contract.Requires(Contract.ForAll(input, (T t) => t.Value() > x));
        Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<T>>(), (T t) => t.Value() > (x + y)));
        foreach (T t in input) {
          yield return t.IncrementBy(y);
        }
      }
    }
    public class Test3<T>
      where T : class, IIncrementable<T> {
      public Test3(T t) {
        tfield = t;
      }
      public IEnumerable<T> Test1a(T t) {
        Contract.Requires(t != null);
        Contract.Ensures(Contract.Result<IEnumerable<T>>() != null);
        Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<T>>(), (T s1) => s1 != null));
        yield return t;
      }
      public IEnumerable<T> Test1b(T s) {
        if (s == null) throw new ArgumentException("");
        yield return s;
      }
      public IEnumerable<T> Test1d(IEnumerable<T> input) {
        Contract.Requires(input != null);
        Contract.Requires(Contract.ForAll(input, (T s1) => s1 != null));
        Contract.Ensures(Contract.ForAll(Contract.Result<IEnumerable<T>>(), (T s1) => s1 != null));
        foreach (T t in input)
          yield return t;
      }
      public IEnumerable<T1> Test1e<T1>(IEnumerable<T> input, T1 t)
        where T1 : IIncrementable<T1> {
        Contract.Requires(Contract.ForAll(input, (T s) => s.Value() == t.Value()));
        yield return t;
      }
      public IEnumerable<T1> Test1g<T1>(IEnumerable<T1> ts, T x)
        where T1 : IIncrementable<T1> {
        Contract.Requires(Contract.ForAll(ts, (T1 y) => foo<T1>(y, x)));
        foreach (T1 t1 in ts) yield return t1;
      }
      bool foo<S>(IIncrementable<S> y, T x) {
        return y.Value() == x.Value();
      }
      T tfield;
      public T TField {
        get {
          return tfield;
        }
      }
    }
  }

  #region Test cases for closure decompilation
  /// <summary>
  /// Dimensions used in designing the test cases:
  /// 
  /// 1) Generic/NonGeneric classes
  /// 2) Generic/NonGeneric methods
  /// 3) Static/Instance methods
  /// 4) Capture of locals/parameters/both/None
  /// 5) Nested closures
  /// 6) Multiple closures
  /// 7) Embedded classes
  /// 7-1) embedded classes maybe generic/none
  /// 
  /// Coverage: Generic class * generic method * (3) * (4) * None-nested closures * single closures * non embedded class
  ///           Generic class * generic method * instance method * both * nested * multiple * embedded/Non embedded class/generic/none
  ///           Generic class * non-generic method * (3) * (4) * non-nested * single * embedded/Non embeded class
  ///           NonGeneric class * generic method * (3) * (4) * nested * multiple * embedded/Non embedded class
  ///           non-generic class * non generic method * (3) * (4) * nonnested closures * single * non embedded class 
  ///           non-generic class * non generic method * instance * both * non/nested * single * non embedded * (8)
  /// </summary>
  public class Class3 {
    /// <summary>
    /// None-Generic class + Generic Method + Static + Capture Locals + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method3_1<T>(T p1)
      where T : class {
      List<T> list = new List<T>();
      T tmp = p1;
      return list.TrueForAll((T t) => t.Equals(tmp));
    }

    /// <summary>
    /// None-Generic class + Generic Method + Static + Capture Parameters + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method3_2<T>(T p1) {
      List<T> list = new List<T>();
      return list.TrueForAll((T t) => t.Equals(p1));
    }

    /// <summary>
    /// None-Generic class + Generic Method + Static + Capture None + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method3_3<T1>(T1 p1)
      where T1 : class {
      List<T1> list = new List<T1>();
      return list.TrueForAll((T1 t) => t == null);
    }

    /// <summary>
    /// None Generic class + Generic Method + Static + Capture Both + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method3_4<T1>(T1 p1)
      where T1 : class {
      int j = 1;
      var list = new List<T1>();
      return list.TrueForAll((T1 t) => t.GetHashCode() == p1.GetHashCode() + j);
    }

    /// <summary>
    /// NoneGeneric class + Generic Method + instance + Capture Locals + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_5<T1>(T1 p1)
      where T1 : class {
      T1 tmp = p1;
      List<T1> list = new List<T1>();
      return list.TrueForAll((T1 t) => t.Equals(tmp));
    }

    /// <summary>
    /// NoneGeneric class + Generic Method + Instance + Capture Parameters + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_6<T1>(T1 p1) {
      List<T1> list = new List<T1>();
      return list.TrueForAll((T1 t) => t.GetHashCode() == p1.GetHashCode());
    }

    /// <summary>
    /// None Generic class + Generic Method + Instance + Capture None + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_7<T1>(T1 p1)
      where T1 : class {
      List<T1> list = new List<T1>();
      return list.TrueForAll((T1 t) => t == null);
    }

    /// <summary>
    /// NoneGeneric class + Generic Method + Instance + Capture Both + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_8<T1>(T1 p1)
      where T1 : class {
      int j = 1;
      List<T1> list = new List<T1>();
      return list.TrueForAll((T1 t) => t.GetHashCode() == p1.GetHashCode() + j);
    }
    /// <summary>
    /// NoneGeneric class + Generic Method + Instance + Capture Both + nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_9<T1>(T1 p1) where T1 : class {
      int j = 1;
      List<T1> list = new List<T1>();
      return list.TrueForAll(delegate(T1 t) {
        List<T1> newList = list;
        int k = 12;
        return newList.TrueForAll(delegate(T1 t1) {
          return t.GetHashCode() == t1.GetHashCode() + p1.GetHashCode() + j + k;
        });
      }
      );
    }

    /// <summary>
    /// NoneGeneric class + Generic Method + Instance + Capture Both + nested closure + multiple + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_10<T1>(T1 p1) where T1 : class {
      int j = 1;
      List<T1> list = new List<T1>();
      return list.TrueForAll(delegate(T1 t) {
        List<T1> newList = list;
        int k = 12;
        bool b1 = newList.TrueForAll(delegate(T1 t1) {
          return t.GetHashCode() == t1.GetHashCode() + p1.GetHashCode() + j + k;
        });
        bool b2 = list.TrueForAll(delegate(T1 t1) {
          return t1.GetHashCode() == t.GetHashCode() + j + k;
        });
        return b1 && b2;
      }
      );
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    class Class3Inner1<T2> where T2 : class {
      List<T2> list_inner = new List<T2>();
      /// <summary>
      /// NoneGeneric class + Generic Method + Instance + Capture Both + nested closure + single + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method3_11(int i) {
        int j = 1;
        return list_inner.TrueForAll(delegate(T2 t) {
          List<T2> newList = list_inner;
          int k = 12;
          return newList.TrueForAll(delegate(T2 t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
        }
        );
      }

      /// <summary>
      /// NoneGeneric class + Generic Method + Instance + Capture Both + nested closure + multiple + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method3_12(int i) {
        int j = 1;
        return list_inner.TrueForAll(delegate(T2 t) {
          List<T2> newList = list_inner;
          int k = 12;
          bool b1 = newList.TrueForAll(delegate(T2 t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
          bool b2 = list_inner.TrueForAll(delegate(T2 t1) {
            return t1.GetHashCode() == t.GetHashCode() + i + j + k;
          });
          return b1 && b2;
        }
        );
      }
    }

    class Class3Inner1 {
      List<int> list_inner = new List<int>();
      /// <summary>
      /// NoneGeneric class + NoneGeneric Method + Instance + Capture Both + nested closure + single + embedded nongeneric class
      /// </summary>
      /// <returns></returns>
      public bool Method3_13(int i) {
        int j = 1;
        return list_inner.TrueForAll(delegate(int t) {
          List<int> newList = list_inner;
          int k = 12;
          return newList.TrueForAll(delegate(int t1) {
            return t == t1 + i + j + k;
          });
        }
        );
      }

      /// <summary>
      /// NoneGeneric class + NoneGeneric Method + Instance + Capture Both + nested closure + multiple + embedded nongeneric class
      /// </summary>
      /// <returns></returns>
      public bool Method3_14(int i) {
        int j = 1;
        return list_inner.TrueForAll(delegate(int t) {
          List<int> newList = list_inner;
          int k = 12;
          bool b1 = newList.TrueForAll(delegate(int t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
          bool b2 = list_inner.TrueForAll(delegate(int t1) {
            return t1.GetHashCode() == t.GetHashCode() + i + j + k;
          });
          return b1 && b2;
        }
        );
      }
    }

    List<int> list = new List<int>();

    /// <summary>
    /// NoneGeneric class + NoneGeneric Method + instance + Capture Locals + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_17(int p1) {
      int local = 3;
      return list.TrueForAll((int t) => t.GetHashCode() == local);
    }

    /// <summary>
    /// Non3Generic class + NoneGeneric Method + Instance + Capture Parameters + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_18(int p1) {
      return list.TrueForAll((int t) => t.GetHashCode() == p1);
    }

    /// <summary>
    /// NoneGeneric class + NoneGeneric Method + Instance + Capture None + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_19(int p1) {
      return list.TrueForAll((int t) => t == 10);
    }

    /// <summary>
    /// NoneGeneric class + NoneGeneric Method + Instance + Capture Both + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_20(int p1) {
      int j = 1;
      return list.TrueForAll((int t) => t == p1 + j);
    }
    /// <summary>
    /// NoneGeneric class + NoneGeneric Method + Instance + Capture Both + nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_21(int p1) {
      int j = 1;
      return list.TrueForAll(delegate(int t) {
        List<int> newList = list;
        int k = 12;
        return newList.TrueForAll(delegate(int t1) {
          return t == t1 + p1 + j + k;
        });
      }
      );
    }

    /// <summary>
    /// NoneGeneric class + NoneGeneric Method + Instance + Capture Both + nested closure + multiple + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method3_22(int p1) {
      int j = 1;
      return list.TrueForAll(delegate(int t) {
        List<int> newList = list;
        int k = 12;
        bool b1 = newList.TrueForAll(delegate(int t1) {
          return t.GetHashCode() == t1 + p1 + j + k;
        });
        bool b2 = list.TrueForAll(delegate(int t1) {
          return t1.GetHashCode() == t.GetHashCode() + j + k;
        });
        return b1 && b2;
      }
      );
    }

    // Generic class + NoneGeneric Method + Instance + Capture None + nested closure + multiple + embedded generic class
    public bool Method3_25(int i) {
      return list.TrueForAll(delegate(int t) {
        List<int> newList = list;
        return true;
      });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    class Class3Inner2<T2> where T2 : class {
      List<T2> list_inner = new List<T2>();
      /// <summary>
      /// NoneGeneric class + NoneGeneric Method + Instance + Capture Both + nested closure + single + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method3_23(int i) {
        int j = 1;
        return list_inner.TrueForAll(delegate(T2 t) {
          List<T2> newList = list_inner;
          int k = 12;
          return newList.TrueForAll(delegate(T2 t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
        }
        );
      }

      /// <summary>
      /// Generic class + NoneGeneric Method + Instance + Capture Both + nested closure + multiple + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method3_24(int i) {
        int j = 1;
        return list_inner.TrueForAll(delegate(T2 t) {
          List<T2> newList = list_inner;
          int k = 12;
          bool b1 = newList.TrueForAll(delegate(T2 t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
          bool b2 = list_inner.TrueForAll(delegate(T2 t1) {
            return t1.GetHashCode() == t.GetHashCode() + i + j + k;
          });
          return b1 && b2;
        }
        );
      }
    }
  }

  public class Class4<T> 
    where T: class
  {
    static List<T> list = new List<T>();
    /// <summary>
    /// Generic class + Generic Method + Static + Capture Locals + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_1<T1>(T1 p1) 
      where T1: class
    {
      T1 tmp = p1;
      return list.TrueForAll((T t) => t.GetHashCode() == tmp.GetHashCode());
    }

    /// <summary>
    /// Generic class + Generic Method + Static + Capture Parameters + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_2<T1>(T1 p1) {
      return list.TrueForAll((T t) => t.GetHashCode() == p1.GetHashCode());
    }

    /// <summary>
    /// Generic class + Generic Method + Static + Capture None + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_3<T1>(T1 p1) 
      where T1: class
    {
      return list.TrueForAll((T t) => t == null);
    }

    /// <summary>
    /// Generic class + Generic Method + Static + Capture Both + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_4<T1>(T1 p1) 
      where T1: class
    {
      int j = 1;
      return list.TrueForAll((T t) => t.GetHashCode() == p1.GetHashCode() +j);
    }

    /// <summary>
    /// Generic class + Generic Method + instance + Capture Locals + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_5<T1>(T1 p1)
      where T1 : class {
      T1 tmp = p1;
      return list.TrueForAll((T t) => t.GetHashCode() == tmp.GetHashCode());
    }

    /// <summary>
    /// Generic class + Generic Method + Instance + Capture Parameters + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_6<T1>(T1 p1) {
      return list.TrueForAll((T t) => t.GetHashCode() == p1.GetHashCode());
    }

    /// <summary>
    /// Generic class + Generic Method + Instance + Capture None + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_7<T1>(T1 p1)
      where T1 : class {
      return list.TrueForAll((T t) => t == null);
    }

    /// <summary>
    /// Generic class + Generic Method + Instance + Capture Both + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_8<T1>(T1 p1)
      where T1 : class {
      int j = 1;
      return list.TrueForAll((T t) => t.GetHashCode() == p1.GetHashCode() + j);
    }
    /// <summary>
    /// Generic class + Generic Method + Instance + Capture Both + nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_9<T1>(T1 p1) where T1: class
    {
      int j = 1;
      return list.TrueForAll(delegate(T t) {
        List<T> newList = list;
        int k = 12;
        return newList.TrueForAll(delegate(T t1) {
          return t.GetHashCode() == t1.GetHashCode() + p1.GetHashCode() + j + k;
        });
      }
      );
    }

    /// <summary>
    /// Generic class + Generic Method + Instance + Capture Both + nested closure + multiple + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_10<T1>(T1 p1) where T1: class
    {
      int j = 1;
      return list.TrueForAll(delegate(T t) {
        List<T> newList = list;
        int k = 12;
        bool b1 = newList.TrueForAll(delegate(T t1) {
          return t.GetHashCode() == t1.GetHashCode() + p1.GetHashCode() + j + k;
        });
        bool b2 = list.TrueForAll(delegate(T t1) {
          return t1.GetHashCode() == t.GetHashCode() + j + k;
        });
        return b1 && b2;
      }
      );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    class Class4Inner1<T2> where T2 : class {
      List<T2> list_inner = new List<T2>();
      /// <summary>
      /// Generic class + Generic Method + Instance + Capture Both + nested closure + single + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method4_11(int i) {
        int j = 1;
        return list.TrueForAll(delegate(T t) {
          List<T> newList = list;
          int k = 12;
          return newList.TrueForAll(delegate(T t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
        }
        );
      }

      /// <summary>
      /// Generic class + Generic Method + Instance + Capture Both + nested closure + multiple + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method4_12(int i) {
        int j = 1;
        return list.TrueForAll(delegate(T t) {
          List<T> newList = list;
          int k = 12;
          bool b1 = newList.TrueForAll(delegate(T t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
          bool b2 = list_inner.TrueForAll(delegate(T2 t1) {
            return t1.GetHashCode() == t.GetHashCode() + i + j + k;
          });
          return b1 && b2;
        }
        );
      }
    }
    
    class Class4Inner2{
      List<T> list_inner = new List<T>();
      /// <summary>
      /// Generic class + Generic Method + Instance + Capture Both + nested closure + single + embedded nongeneric class
      /// </summary>
      /// <returns></returns>
      public bool Method4_11(int i) {
        int j = 1;
        return list.TrueForAll(delegate(T t) {
          List<T> newList = list;
          int k = 12;
          return newList.TrueForAll(delegate(T t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
        }
        );
      }

      /// <summary>
      /// Generic class + Generic Method + Instance + Capture Both + nested closure + multiple + embedded nongeneric class
      /// </summary>
      /// <returns></returns>
      public bool Method4_12(int i) {
        int j = 1;
        return list.TrueForAll(delegate(T t) {
          List<T> newList = list;
          int k = 12;
          bool b1 = newList.TrueForAll(delegate(T t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
          bool b2 = list_inner.TrueForAll(delegate(T t1) {
            return t1.GetHashCode() == t.GetHashCode() + i + j + k;
          });
          return b1 && b2;
        }
        );
      }
    }
        /// <summary>
    /// Generic class + non generic Method + Static + Capture Locals + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_13() 
    {
      int local=3;
      return list.TrueForAll((T t) => t.GetHashCode() == local);
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + Static + Capture Parameters + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_14(int p1) {
      return list.TrueForAll((T t) => t.GetHashCode() == p1);
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + Static + Capture None + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_15(int p1) 
    {
      return list.TrueForAll((T t) => t == null);
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + Static + Capture Both + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public static bool Method4_16(int p1) 
    {
      int j = 1;
      return list.TrueForAll((T t) => t.GetHashCode() == p1 +j);
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + instance + Capture Locals + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_17(int p1)
    {
      int local =3;
      return list.TrueForAll((T t) => t.GetHashCode() == local);
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + Instance + Capture Parameters + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_18(int p1) {
      return list.TrueForAll((T t) => t.GetHashCode() == p1);
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + Instance + Capture None + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_19(int p1)
    {
      return list.TrueForAll((T t) => t == null);
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + Instance + Capture Both + no nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_20(int p1)
    {
      int j = 1;
      return list.TrueForAll((T t) => t.GetHashCode() == p1 + j);
    }
    /// <summary>
    /// Generic class + NoneGeneric Method + Instance + Capture Both + nested closure + single + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_21(int p1) 
    {
      int j = 1;
      return list.TrueForAll(delegate(T t) {
        List<T> newList = list;
        int k = 12;
        return newList.TrueForAll(delegate(T t1) {
          return t.GetHashCode() == t1.GetHashCode() + p1 + j + k;
        });
      }
      );
    }

    /// <summary>
    /// Generic class + NoneGeneric Method + Instance + Capture Both + nested closure + multiple + non embedded class
    /// </summary>
    /// <returns></returns>
    public bool Method4_22(int p1) 
    {
      int j = 1;
      return list.TrueForAll(delegate(T t) {
        List<T> newList = list;
        int k = 12;
        bool b1 = newList.TrueForAll(delegate(T t1) {
          return t.GetHashCode() == t1.GetHashCode() + p1.GetHashCode() + j + k;
        });
        bool b2 = list.TrueForAll(delegate(T t1) {
          return t1.GetHashCode() == t.GetHashCode() + j + k;
        });
        return b1 && b2;
      }
      );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T2"></typeparam>
    class Class4Inner3<T2> where T2 : class {
      List<T2> list_inner = new List<T2>();
      /// <summary>
      /// Generic class + NoneGeneric Method + Instance + Capture Both + nested closure + single + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method4_23(int i) {
        int j = 1;
        return list.TrueForAll(delegate(T t) {
          List<T> newList = list;
          int k = 12;
          return newList.TrueForAll(delegate(T t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
        }
        );
      }

      /// <summary>
      /// Generic class + NoneGeneric Method + Instance + Capture Both + nested closure + multiple + embedded generic class
      /// </summary>
      /// <returns></returns>
      public bool Method4_24(int i) {
        int j = 1;
        return list.TrueForAll(delegate(T t) {
          List<T> newList = list;
          int k = 12;
          bool b1 = newList.TrueForAll(delegate(T t1) {
            return t.GetHashCode() == t1.GetHashCode() + i + j + k;
          });
          bool b2 = list_inner.TrueForAll(delegate(T2 t1) {
            return t1.GetHashCode() == t.GetHashCode() + i + j + k;
          });
          return b1 && b2;
        }
        );
      }
    }
  }
  #endregion 
}
