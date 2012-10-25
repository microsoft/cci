// ==++==
// 
//   
//    Copyright (c) 2012 Microsoft Corporation.  All rights reserved.
//   
//    The use and distribution terms for this software are contained in the file
//    named license.txt, which can be found in the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by the
//    terms of this license.
//   
//    You must not remove this notice, or any other, from this software.
//   
// 
// ==--==
public static class Casts {
  public static int Main() {
    if (!Cast1()) return 1;
    if (!Cast2()) return 2;
    if (!Cast3()) return 3;
    if (!Cast4()) return 4;
    if (!Cast5()) return 5;
    if (!Cast6()) return 6;
    if (!Cast7()) return 7;
    if (!Cast8()) return 8;
    if (!Cast9()) return 9;
    if (!Cast10()) return 10;
    if (!Cast11()) return 11;
    if (!Cast12()) return 12;
    if (!Cast13()) return 13;
    if (!Cast14()) return 14;
    if (!Cast15()) return 15;
    if (!Cast16()) return 16;
    if (!Cast17()) return 17;
    if (!Cast18()) return 18;
    if (!Cast19()) return 19;
    if (!Cast20()) return 20;
    if (!Cast21()) return 21;
    if (!Cast22()) return 22;
    if (!Cast23()) return 23;
    if (!Cast24()) return 24;
    if (!Cast25()) return 25;
    if (!Cast26()) return 26;
    if (!Cast27()) return 27;
    if (!Cast28()) return 28;
    return 0;
  }

  static bool Cast1() {
    try {
      var instance = GetBaseClassAsObject();
      var derivedInstance = (DerivedClass)instance;
    } catch (System.InvalidCastException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool Cast2() {
    try {
      var instance = GetBaseClassAsObject();
      var baseInstance = (BaseClass)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast3() {
    try {
      var instance = GetDerivedClassAsObject();
      var derivedInstance = (DerivedClass)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast4() {
    try {
      var instance = GetDerivedClassAsObject();
      var baseInstance = (BaseClass)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast5() {
    try {
      var instance = GetNullObject();
      var baseInstance = (BaseClass)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast6() {
    try {
      var instance = GetSomeOtherClassAsObject();
      var baseInstance = (BaseClass)instance;
    } catch (System.InvalidCastException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool Cast7() {
    try {
      var instance = GetSomeOtherClassAsObject();
      var derivedInstance = (DerivedClass)instance;
    } catch (System.InvalidCastException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool Cast8() {
    var instance = GetBaseClassAsObject();
    var derivedInstance = instance as DerivedClass;
    return derivedInstance == null;
  }

  static bool Cast9() {
    var instance = GetBaseClassAsObject();
    var baseInstance = instance as BaseClass;
    return baseInstance != null;
  }

  static bool Cast10() {
    var instance = GetDerivedClassAsObject();
    var derivedInstance = instance as DerivedClass;
    return derivedInstance != null;
  }

  static bool Cast11() {
    var instance = GetDerivedClassAsObject();
    var baseInstance = instance as BaseClass;
    return baseInstance != null;
  }

  static bool Cast12() {
    var instance = GetNullObject();
    var baseInstance = instance as BaseClass;
    return baseInstance == null;
  }

  static bool Cast13() {
    var instance = GetSomeOtherClassAsObject();
    var baseInstance = instance as BaseClass;
    return baseInstance == null;
  }

  static bool Cast14() {
    var instance = GetSomeOtherClassAsObject();
    var derivedInstance = instance as DerivedClass;
    return derivedInstance == null;
  }

  static bool Cast15() {
    try {
      var instance = GetBaseClassAsObject();
      var derivedInstance = (IDerived)instance;
    } catch (System.InvalidCastException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool Cast16() {
    try {
      var instance = GetBaseClassAsObject();
      var baseInstance = (IBase)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast17() {
    try {
      var instance = GetDerivedClassAsObject();
      var derivedInstance = (IDerived)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast18() {
    try {
      var instance = GetDerivedClassAsObject();
      var baseInstance = (IBase)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast19() {
    try {
      var instance = GetNullObject();
      var baseInstance = (IBase)instance;
    } catch {
      return false;
    }
    return true;
  }

  static bool Cast20() {
    try {
      var instance = GetSomeOtherClassAsObject();
      var baseInstance = (IBase)instance;
    } catch (System.InvalidCastException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool Cast21() {
    try {
      var instance = GetSomeOtherClassAsObject();
      var derivedInstance = (IDerived)instance;
    } catch (System.InvalidCastException) {
      return true;
    } catch {
      return false;
    }
    return false;
  }

  static bool Cast22() {
    var instance = GetBaseClassAsObject();
    var derivedInstance = instance as IDerived;
    return derivedInstance == null;
  }

  static bool Cast23() {
    var instance = GetBaseClassAsObject();
    var baseInstance = instance as IBase;
    return baseInstance != null;
  }

  static bool Cast24() {
    var instance = GetDerivedClassAsObject();
    var derivedInstance = instance as IDerived;
    return derivedInstance != null;
  }

  static bool Cast25() {
    var instance = GetDerivedClassAsObject();
    var baseInstance = instance as IBase;
    return baseInstance != null;
  }

  static bool Cast26() {
    var instance = GetNullObject();
    var baseInstance = instance as IBase;
    return baseInstance == null;
  }

  static bool Cast27() {
    var instance = GetSomeOtherClassAsObject();
    var baseInstance = instance as IBase;
    return baseInstance == null;
  }

  static bool Cast28() {
    var instance = GetSomeOtherClassAsObject();
    var derivedInstance = instance as IDerived;
    return derivedInstance == null;
  }

  static object GetDerivedClassAsObject() {
    return new DerivedClass();
  }

  static object GetBaseClassAsObject() {
    return new BaseClass();
  }

  static object GetSomeOtherClassAsObject() {
    return new SomeOtherClass();
  }

  static object GetNullObject() {
    return null;
  }
}

class BaseClass : IBase {
}

class DerivedClass : BaseClass, IDerived {
}

class SomeOtherClass : ISomeOther {
}

interface IBase {
}

interface IDerived : IBase {
}

interface ISomeOther {
}
