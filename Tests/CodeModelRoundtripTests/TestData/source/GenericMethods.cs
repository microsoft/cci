using System;
using System.Collections.Generic;


namespace N {
  /// <summary>
  /// Generic methods to test adding a generic method parameter. 
  /// </summary>
  class GenericMethods {
    void f1<T>(T t) {
      f2(t,t);
      f1(t);
    }
    void f2<T1>(T1 t1, T1 t2) {
      f1(t1);
      f1(t2);
      f2(t1, t2);
    }
  }
}
