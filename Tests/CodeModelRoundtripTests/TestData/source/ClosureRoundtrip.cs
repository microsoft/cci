using System;
using System.Collections.Generic;

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
  where T : class {
  static List<T> list = new List<T>();
  /// <summary>
  /// Generic class + Generic Method + Static + Capture Locals + no nested closure + single + non embedded class
  /// </summary>
  /// <returns></returns>
  public static bool Method4_1<T1>(T1 p1)
    where T1 : class {
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
    where T1 : class {
    return list.TrueForAll((T t) => t == null);
  }

  /// <summary>
  /// Generic class + Generic Method + Static + Capture Both + no nested closure + single + non embedded class
  /// </summary>
  /// <returns></returns>
  public static bool Method4_4<T1>(T1 p1)
    where T1 : class {
    int j = 1;
    return list.TrueForAll((T t) => t.GetHashCode() == p1.GetHashCode() + j);
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
  public bool Method4_9<T1>(T1 p1) where T1 : class {
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
  public bool Method4_10<T1>(T1 p1) where T1 : class {
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

  class Class4Inner2 {
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
  public static bool Method4_13() {
    int local = 3;
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
  public static bool Method4_15(int p1) {
    return list.TrueForAll((T t) => t == null);
  }

  /// <summary>
  /// Generic class + NoneGeneric Method + Static + Capture Both + no nested closure + single + non embedded class
  /// </summary>
  /// <returns></returns>
  public static bool Method4_16(int p1) {
    int j = 1;
    return list.TrueForAll((T t) => t.GetHashCode() == p1 + j);
  }

  /// <summary>
  /// Generic class + NoneGeneric Method + instance + Capture Locals + no nested closure + single + non embedded class
  /// </summary>
  /// <returns></returns>
  public bool Method4_17(int p1) {
    int local = 3;
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
  public bool Method4_19(int p1) {
    return list.TrueForAll((T t) => t == null);
  }

  /// <summary>
  /// Generic class + NoneGeneric Method + Instance + Capture Both + no nested closure + single + non embedded class
  /// </summary>
  /// <returns></returns>
  public bool Method4_20(int p1) {
    int j = 1;
    return list.TrueForAll((T t) => t.GetHashCode() == p1 + j);
  }
  /// <summary>
  /// Generic class + NoneGeneric Method + Instance + Capture Both + nested closure + single + non embedded class
  /// </summary>
  /// <returns></returns>
  public bool Method4_21(int p1) {
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
  public bool Method4_22(int p1) {
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

/// <summary>
/// Test case for an issue reported by ckitching:
/// http://cciast.codeplex.com/workitem/4662
/// </summary>
public class ckitching4662 {
  public void test1() {
    List<string> la = new List<string>();
    List<uint> lb = new List<uint>();
    Action<string> a;
    Action<uint> b;

    a = o => {
      string va = "Hi";

      la.Add(va);
    };
    b = o => {
      uint vb = 42;

      lb.Add(vb);
    };
  }
}

/// <summary>
/// Test case for an issue reported by ckitching:
/// http://cciast.codeplex.com/workitem/4673
/// </summary>
public class ckitching4673 {
  public void t1() {
    for (int i = 0; i < 1; i++) {
      Action t = () => i.Equals(i);
    }
  }
}

public static class Repro8 {
  public static void ObserveFirst<T>(Func<T, bool> discriminator) {
    discriminator = e => discriminator((T)e);
  }
}
