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
public static class ExceptionHandling {
  public static int Main() {
    int ret;

    ret = Rethrow();
    if (ret != 0) return 100 + ret;

    ret = CallThrowNull();
    if (ret != 0) return 200 + ret;

    ret = CallThrowSimple();
    if (ret != 0) return 300 + ret;

    ret = CallThrowNested();
    if (ret != 0) return 400 + ret;

    ret = ThrowCatchSimple();
    if (ret != 0) return 500 + ret;

    ret = ThrowCatchSimple2();
    if (ret != 0) return 600 + ret;

    ret = CallThrowCatchSimple3();
    if (ret != 0) return 700 + ret;

    ret = ThrowCatchSimple4();
    if (ret != 0) return 800 + ret;

    ret = ThrowCatchSimple5();
    if (ret != 0) return 900 + ret;

    ret = ThrowCatchSimple6();
    if (ret != 0) return 1000 + ret;

    ret = ThrowCatchSimple7();
    if (ret != 0) return 1100 + ret;

    ret = CallThrowAcrossMethods1();
    if (ret != 0) return 1200 + ret;

    ret = CallThrowAcrossMethods2();
    if (ret != 0) return 1300 + ret;

    ret = CallThrowAcrossMethods3();
    if (ret != 0) return 1400 + ret;

    ret = TryFinallySimple();
    if (ret != 0) return 1500 + ret;

    ret = TryFinallySimpleWithGoto();
    if (ret != 0) return 1600 + ret;

    ret = TryFinallyNested();
    if (ret != 0) return 1700 + ret;

    ret = TryFinallyNested2();
    if (ret != 0) return 1800 + ret;

    ret = TryFinallyNestedWithGoto();
    if (ret != 0) return 1900 + ret;

    return 0;
  }

  public static int Rethrow() {
    int i = 0;
    int j = 0;
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      try {
        i = i + 1;
        throw appExp;
      } catch (System.ApplicationException e) {
        if (i != 2) {
          j = 1;
        }
        i = i + 5;
        throw;
      } catch (System.Exception) {
        j = 1;
      } finally {
        if (i != 7) {
          j = 1;
        }
        i = i + 3;
      }
      i = i + 100;
    } catch (System.ApplicationException) {
      if (i != 10) {
        j = 1;
      }
      i = i + 8;
    } catch (System.Exception) {
      j = 1;
    } finally {
      if (i != 18) {
        j = 1;
      }
      i = i + 10;
    }
    if (j == 1) {
      return 1;
    }
    return i == 28 ? 0 : i;
  }

  public static int CallThrowNull() {
    int result = 1;
    try {
      result = ThrowNull();
    } catch (System.NullReferenceException) {
      result = 0;
    }
    return result;
  }

  public static int ThrowNull() {
    System.Exception exp = null;
    int i = 0;
    int j = 0;
    try {
      i = i + 1;
      throw exp;
    } finally {
      if (i != 1) {
        j = 1;
      }
      i = i + 3;
    }
    if (j == 1) {
      return 1;
    }
    return i == 4 ? 0 : 1;
  }

  public static int CallThrowSimple() {
    int result = 1;
    try {
      result = ThrowSimple();
    } catch {
      result = 0;
    }
    return result;
  }

  public static int ThrowSimple() {
    System.Exception exp = new System.Exception();
    int i = 0;
    int j = 0;
    try {
      i = i + 1;
      throw exp;
    } finally {
      if (i != 1) {
        j = 1;
      }
      i = i + 3;
    }
    if (j == 1) {
      return 1;
    }
    return i == 4 ? 0 : 1;
  }

  public static int CallThrowNested() {
    int result = 1;
    try {
      result = ThrowNested();
    } catch {
      result = 0;
    }
    return result;
  }

  public static int ThrowNested() {
    System.Exception exp = new System.Exception();
    int i = 0;
    int j = 0;
    try {
      i = i + 1;
    } finally {
      if (i != 1) {
        j = 1;
      }
      i = i + 3;
      try {
        if (i != 4) {
          j = 1;
        }
        i = i + 6;
        throw exp;
      } finally {
        if (i != 10) {
          j = 1;
        }
        i = i + 15;
      }
    }
    if (j == 1) {
      return 1;
    }
    return i == 25 ? 0 : 1;
  }

  public static int ThrowCatchSimple() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      throw exp;
    } catch (System.Exception) {
      if (i != 1) {
        j = 1;
      }
      i = i + 2;
    }
    if (j == 1) {
      return 1;
    }
    return i == 3 ? 0 : 1;
  }

  public static int ThrowCatchSimple2() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      throw exp;
    } catch (System.Exception) {
      if (i != 1) {
        j = 1;
      }
      i = i + 2;
    } finally {
      if (i != 3) {
        j = 1;
      }
      i = i + 5;
    }
    if (j == 1) {
      return 1;
    }
    return i == 8 ? 0 : 1;
  }

  public static int CallThrowCatchSimple3() {
    int result = 1;
    try {
      ThrowCatchSimple3();
    } catch (System.ApplicationException) {
      result = 0;
    }
    return result;
  }

  public static int ThrowCatchSimple3() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      try {
        i = i + 1;
        throw appExp;
      } finally {
        if (i != 1) {
          j = 1;
        }
        i = i + 2;
      }
    } finally {
      if (i != 3) {
        j = 1;
      }
      i = i + 5;
    }
    if (j == 1) {
      return 1;
    }
    return i == 8 ? 0 : 1;
  }

  public static int ThrowCatchSimple4() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      try {
        i = i + 1;
        throw appExp;
      } catch (System.ApplicationException) {
        if (i != 2) {
          j = 1;
        }
        i = i + 5;
      } catch (System.Exception) {
        j = 1;
      } finally {
        if (i != 7) {
          j = 1;
        }
        i = i + 3;
      }
    } catch (System.ApplicationException) {
      j = 1;
    } catch (System.Exception) {
      j = 1;
    } finally {
      if (i != 10) {
        j = 1;
      }
      i = i + 8;
    }
    if (j == 1) {
      return 1;
    }
    return i == 18 ? 0 : 1;
  }

  public static int ThrowCatchSimple5() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      try {
        i = i + 1;
        throw exp;
      } catch (System.ApplicationException) {
        j = 1;
      } catch (System.Exception) {
        if (i != 2) {
          j = 1;
        }
        i = i + 5;
      } finally {
        if (i != 7) {
          j = 1;
        }
        i = i + 3;
      }
    } catch (System.ApplicationException) {
      j = 1;
    } catch (System.Exception) {
      j = 1;
    } finally {
      if (i != 10) {
        j = 1;
      }
      i = i + 8;
    }
    if (j == 1) {
      return 1;
    }
    return i == 18 ? 0 : i;
  }

  public static int ThrowCatchSimple6() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      try {
        i = i + 1;
        throw exp;
      } catch (System.ApplicationException) {
        j = 1;
      } finally {
        if (i != 2) {
          j = 1;
        }
        i = i + 3;
      }
      i = i + 100;
    } catch (System.ApplicationException) {
      j = 1;
    } catch (System.Exception) {
      if (i != 5) {
        j = 1;
      }
      i = i + 5;
    } finally {
      if (i != 10) {
        j = 1;
      }
      i = i + 8;
    }
    if (j == 1) {
      return 1;
    }
    return i == 18 ? 0 : i;
  }

  public static int ThrowCatchSimple7() {
    int i = 0;
    int j = 0;
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      try {
        i = i + 1;
        throw appExp;
      } catch (System.ApplicationException e) {
        if (i != 2) {
          j = 1;
        }
        i = i + 5;
        throw e;
      } catch (System.Exception) {
        j = 1;
      } finally {
        if (i != 7) {
          j = 1;
        }
        i = i + 3;
      }
      i = i + 100;
    } catch (System.ApplicationException) {
      if (i != 10) {
        j = 1;
      }
      i = i + 8;
    } catch (System.Exception) {
      j = 1;
    } finally {
      if (i != 18) {
        j = 1;
      }
      i = i + 10;
    }
    if (j == 1) {
      return 1;
    }
    return i == 28 ? 0 : 1;
  }

  public static int CallThrowAcrossMethods1() {
    int i = 0;
    int j = 0;
    try {
      i = i + 1;
      ThrowAcrossMethods1();
    } catch (System.Exception) {
      if (i != 1) {
        j = 1;
      }
      i = i + 2;
    }
    if (j == 1) {
      return 1;
    }
    return i == 3 ? 0 : 1;
  }

  public static int ThrowAcrossMethods1() {
    System.Exception exp = new System.Exception();
    throw exp;
  }

  public static int CallThrowAcrossMethods2() {
    return ThrowAcrossMethods2();
  }

  public static int ThrowAcrossMethods2() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      throw exp;
    } catch (System.Exception) {
      if (i != 1) {
        j = 1;
      }
      i = i + 2;
    }
    if (j == 1) {
      return 1;
    }
    return i == 3 ? 0 : 1;
  }

  public static int CallThrowAcrossMethods3() {
    int result = 1;
    try {
      ThrowAcrossMethods3();
    } catch (System.Exception) {
      result = 0;
    }
    return result;
  }

  public static int ThrowAcrossMethods3() {
    int i = 0;
    int j = 0;
    System.Exception exp = new System.Exception();
    System.ApplicationException appExp = new System.ApplicationException();
    try {
      i = i + 1;
      throw exp;
      j = 1;
    } catch (System.ApplicationException) {
      j = 1;
    } finally {
      if (i != 1) {
        j = 1;
      }
      i = i + 2;
    }
    if (j == 1) {
      return 1;
    }
    return i == 1 ? 0 : 1;
  }

  public static int TryFinallySimple() {
    int i = 0;
    try {
      i = i + 1;
    } finally {
      i = i + 3;
    }
    if (i != 4) {
      return 1;
    }
    i = i + 5;
    return i == 9 ? 0 : 1;
  }

  public static int TryFinallySimpleWithGoto() {
    int i = 0;
    int j = 0;
    try {
      i = i + 1;
      // This makes sure the unreachabke i = i + 5 get into the IL
      if (i > 0)
        goto label;
    } finally {
      if (i != 1) {
        j = 1;
      }
      i = i + 3;
    }
    i = i + 5;
  label:
    if (j != 0) {
      return 1;
    }
    return i == 4 ? 0 : 1;
  }

  public static int TryFinallyNested() {
    int i = 0;
    int j = 0;
    try {
      i = i + 1;
    } finally {
      i = i + 3;
      if (i != 4) {
        j = 1;
      }
      try {
        i = i + 6;
      } finally {
        if (i != 10) {
          j = 1;
        }
        i = i + 15;
      }
    }
    if (i != 25) {
      return 1;
    }
    if (j != 0) {
      return 1;
    }
    i = i + 20;
    return i == 45 ? 0 : 1;
  }

  public static int TryFinallyNested2() {
    int i = 0;
    int j = 0;
    try {
      try {
        i = i + 6;
      } finally {
        if (i != 6) {
          j = 1;
        }
        i = i + 15;
      }
      if (i != 21) {
        return 1;
      }
      i = i + 1;
    } finally {
      if (i != 22) {
        j = 1;
      }
      i = i + 3;
    }
    if (j != 0) {
      return 1;
    }
    return i == 25 ? 0 : 1;
  }

  public static int TryFinallyNestedWithGoto() {
    int i = 0;
    int j = 0;
    try {
      try {
        i = i + 6;
        // This makes sure the unreachabke i = i + 5 get into the IL
        if (i > 0)
          goto label;
      } finally {
        if (i != 6) {
          j = 1;
        }
        i = i + 15;
      }
      if (i != 21) {
        return 1;
      }
      i = i + 1;
    } finally {
      if (i != 21) {
        j = 1;
      }
      i = i + 3;
    }
    i = i + 9;
  label:
    if (j != 0) {
      return 1;
    }
    return i == 24 ? 0 : 1;
  }
}

