using System.Collections.Generic;

public class Assem {
  public Module1.Foo Foo;
  public Module1.Foo.Nested FooNested;
  public Module2.Bar Bar;
  public Generic1<int> Generic1;
  public Generic2<int> Generic2;
  public Generic3 Generic3;
  public Generic4<int> Generic4;
  public Generic1<int>.Nested Generic1Nested;
  public Generic2<object>.Nested<float> Generic2Nested;
  public Generic3.Nested<float> Generic3Nested;
  public T GenMethod<T>(T t, List<T> l, T[] ta, T[,] tm, T[,] tn) where T : class {
    this.GenMethod<object>(null, null, null, null, null);
    return t;
  }
}

public delegate int GenericDelegate<T>(T t);

public interface IGen1<T> {
}

public interface IGen2<T, U> {
}

public class Generic1<T> : IGen1<T> {
  public T fieldT;
  public T propT {
    get {
      return default(T);
    }
    set {
    }
  }
  public event GenericDelegate<T> GenericEvent;
  public class Nested : IGen1<T> {
    public T fieldT;
    public T propT {
      get {
        return default(T);
      }
      set {
      }
    }
  }
}

public class Generic2<T> : IGen1<T> {
  public T fieldT;
  public T propT {
    get {
      return default(T);
    }
    set {
    }
  }
  public class Nested<U> : IGen2<T, U> where U : T {
    public T fieldT;
    public T propT {
      get {
        return default(T);
      }
      set {
      }
    }
    public U fieldU;
    public U propU {
      get {
        return default(U);
      }
      set {
      }
    }
    public U Meth<V>(IGen1<T> t, V v) where V : T { return default(U); }
  }
}

public class Generic3 {
  public class Nested<U> : IGen1<U> {
    public U fieldU;
    public U propU {
      get {
        return default(U);
      }
      set {
      }
    }
  }
}

public unsafe class Generic4<T> where T : struct {
  public T field1;
  public T[] field2;
  public int* field3;
  public T[,] field4;
}
