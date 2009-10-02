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
      byte[] Bytes = new byte[] { byte.MaxValue };
      ushort[] UShorts = new ushort[] { ushort.MaxValue };
      uint[] UInts = new uint[] { uint.MaxValue };
      ulong[] ULongs = new ulong[] { ulong.MaxValue };
      sbyte[] SBytes = new sbyte[] { sbyte.MinValue };
      short[] Shorts = new short[] { short.MinValue };
      int[] Ints = new int[] { int.MinValue };
      long[] Longs = new long[] { long.MinValue };
      char[] Chars = new char[] { 'a' };
      bool[] Bools = new bool[] { true };
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

    int c;
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
}
