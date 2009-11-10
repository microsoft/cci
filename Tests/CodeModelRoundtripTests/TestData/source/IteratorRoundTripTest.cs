
using System.Collections.Generic;


/// <summary>
/// Dimensions used in designing the test cases:
/// 
/// 1) Generic/NonGeneric classes
/// 2) Generic/NonGeneric methods
/// 3) Static/Instance methods
/// 4) nested classes + generic/non
/// 5) Type constraints: none simple, generic with different parameters
/// 
/// Coverage: Generic class * generic method * (3) * nested with generic * (5)
///           generic class * non-generic method * static * nested with generic * generic with different parameters
///           Generic class * non-generic method * instance * (4) * generic with different parameters
///           non-generic class * non generic method * instance * non-nested * (5)
///           non-generic class * generic method * instance * (5)
/// </summary>
/// <typeparam name="T1"></typeparam>

interface Valuable<T> {
  T Value {
    get;
  }
  int IntValue {
    get;
  }
}

class Value1 : Valuable<Value1> {
  int v;
  public Value1(int x) {
    v = x;
  }
  public int IntValue {
    get {
      return v;
    }
  }
  public Value1 Value {
    get {
      return this;
    }
  }
}

class Test1<T1> 
  where T1: Valuable<T1>
{
  int start =0;
  static int START = 2;
  public IEnumerable<string> foo1(T1 t1, string t2) {
    for (int i = start; i < start + 4; i++) {
      yield return t2;
    }
  }

  public static IEnumerable<string> foo2(T1 t1, string t2) {
    for (int i = START; i < START + 4; i++) {
      yield return t2;
    }
  }

  class Inner1<T2> {
    public static IEnumerable<T2> foo3(T1 t1, T2 t2) {
      yield return t2;
    }

    public IEnumerable<T2> foo4(T1 t1, T2 t2) {
      yield return t2;
    }
  }

  class Inner2<T2> where T2 : Valuable<T2> {
    public static IEnumerable<T2> foo5(T1 t1, T2 t2) {
      yield return t2;
    }

    public IEnumerable<T2> foo6(T1 t1, T2 t2) {
      for (int i=0; i< t1.IntValue; i++) {
        yield return t2;
      }
    }
  }

  class Inner3 {
    class Inner4<T2> where T2 : Valuable<T2> {
      public static IEnumerable<T2> foo7(T1 t1, T2 t2) {
        yield return t2;
      }

      public IEnumerable<T2> foo8(T1 t1, T2 t2) {
        for (int i = 0; i < t1.IntValue; i++) {
          yield return t2;
        }
      }
    }

    IEnumerable<T2> foo9<T2>(T1 t1, T2 t2) 
      where T2: Valuable<T2>
    {
      for (int i = 0; i < t1.IntValue; i++) {
        yield return t2;
      }
    }
  }

  public Test1(int start) {
    this.start = start;
  }
}

class Test2 {
  int start;
  static int START = 2;
  public IEnumerable<string> foo1(string t2) {
    for (int i = start; i < start + 4; i++) {
      yield return t2;
    }
  }

  public static IEnumerable<string> foo2(string t2) {
    for (int i = START; i < START + 4; i++) {
      yield return t2;
    }
  }

  class Inner1<T2> {
    public static IEnumerable<T2> foo3(T2 t2) {
      yield return t2;
    }

    public IEnumerable<T2> foo4(T2 t2) {
      yield return t2;
    }
  }

  class Inner2<T2> where T2 : Valuable<T2> {
    public static IEnumerable<T2> foo5(T2 t2) {
      yield return t2;
    }

    public IEnumerable<T2> foo6(T2 t2) {
      for (int i = 0; i < t2.IntValue; i++) {
        yield return t2;
      }
    }

    public IEnumerable<T3> foo10<T3>(T2 t2, T3 t3)
      where T3: Valuable<T3> {
      for (int i = 0; i < t2.IntValue; i++) {
        yield return t3;
      }
    }
  }

  class Inner3 {
    class Inner4<T2> where T2 : Valuable<T2> {
      public static IEnumerable<T2> foo7(T2 t2) {
        yield return t2;
      }

      public IEnumerable<T2> foo8(T2 t2) {
        for (int i = 0; i < t2.IntValue; i++) {
          yield return t2;
        }
      }
    }

    IEnumerable<T2> foo9<T2>(T2 t2)
      where T2 : Valuable<T2> {
      for (int i = 0; i < t2.IntValue; i++) {
        yield return t2;
      }
    }
  }
}

