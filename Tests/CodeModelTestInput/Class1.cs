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
  }
}
