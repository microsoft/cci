using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Diagnostics;
using Microsoft.Cci;
using Microsoft.Cci.MutableCodeModel;
using ILGarbageCollect;
using ILGarbageCollect.Mark;
using System.Text.RegularExpressions;
using ILGarbageCollect.Summaries;



namespace TestILGarbageCollector {



  [TestClass]
  public class RapidTypeAnalysisTests {


    #region RapidTypeAnalysis tests

    [TestMethod]
    public void TestDeadField() {
      RunRTAOnSourceResource("DeadField.cs", (compilerResult, rta) => {
        // find liveField in the set of reachable fields
        Assert.IsTrue(RapidTypeAnalysisTests.IsFieldReachable(compilerResult.Host, rta, "liveField"));

        // do not find deadField in the set of reachable fields
        Assert.IsFalse(RapidTypeAnalysisTests.IsFieldReachable(compilerResult.Host, rta, "deadField"));
      });
    }

    [TestMethod]
    public void TestDeadMethod() {
      RunRTAOnSourceResource("DeadMethod.cs", (compilerResult, rta) => {
        // find liveField in the set of reachable fields
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "DeadMethod", "LiveMethodFoo"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "DeadMethod", "DeadMethodFoo"));
      });
    }

    [TestMethod]
    public void TestStaticMethodReachability() {
      String source =
@"class MainClass {
    class S {
        // A calls B calls C calls D, which calls C
        // Unreachable1 calls Unreachable2 and A
        static void B() { C();}
        internal static void A() { B();}
        static void C() {D();}
        static void D() {C();}
        static void Unreachable2() {}
        static void Unreachable1() {Unreachable2();A();}
    }
    
    static void Main(string[] argv) {
        S.A();
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "A"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "B"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "C"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "D"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable1"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable2"));
      });
    }



    [TestMethod]
    public void TestInstanceMethodReachability() {
      String source =
@"class MainClass {
    class S {
        // A calls B calls C calls D, which calls C
        // Unreachable1 calls Unreachable2 and A
        internal void B() { C();}
        internal void A() { B();}
        internal void C() {D();}
        internal void D() {C();}
        internal void Unreachable2() {}
        internal void Unreachable1() {Unreachable2();A();}
    }
    
    static void Main(string[] argv) {
        var o = new S();
        o.A();
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "A"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "B"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "C"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "D"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable1"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable2"));
      });
    }

    [TestMethod]
    public void TestStructInstanceMethodReachability() {
      String source =
@"class MainClass {
    struct S {
        // A calls B calls C calls D, which calls C
        // Unreachable1 calls Unreachable2 and A
        internal void B() { C();}
        internal void A() { B();}
        internal void C() {D();}
        internal void D() {C();}
        internal void Unreachable2() {}
        internal void Unreachable1() {Unreachable2();A();}
    }
    
    static void Main(string[] argv) {
        S o;
        o.A();
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "A"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "B"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "C"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "D"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable1"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable2"));
      });
    }

    [TestMethod]
    public void TestVirtualMethodChainReachability() {
      String source =
@"class MainClass {
    class S {
        // A calls B calls C calls D, which calls C
        // Unreachable1 calls Unreachable2 and A
        internal virtual void B() { C();}
        internal virtual void A() { B();}
        internal virtual void C() {D();}
        internal virtual void D() {C();}
        internal virtual void Unreachable2() {}
        internal virtual void Unreachable1() {Unreachable2();A();}
    }
    
    static void Main(string[] argv) {
        var o = new S();
        o.A();
    } 
}";
      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "A"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "B"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "C"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "D"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable1"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "S", "Unreachable2"));
      });
    }

    [TestMethod]
    public void TestConstructorReachability() {
      String source =
@"class MainClass {
    // A calls B calls C calls D, which calls C
    // Unreachable1 calls Unreachable2 and A   
    class A {
        public A() {
            new B();
        }
    }
    class B {
        public B() {
            new C();
        }
    }
    class C {
        public C() {
            new D();
        }
    }
    class D {
        public D() {
            new C();
        }
    }
    class Unreachable1 {
        public Unreachable1() {
            new Unreachable2();
        }
    }
    class Unreachable2 {
        public Unreachable2() {
            new A();
        }
    } 
    static void Main(string[] argv) {
        var o = new A();
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "A", ".ctor"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "B", ".ctor"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "C", ".ctor"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "D", ".ctor"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "Unreachable1", ".ctor"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "Unreachable2", ".ctor"));
      });
    }

    [TestMethod]
    public void TestVirtualCallToBaseBringsInBase() {
      // Even though SuperClass is not constructed, SuperClass.M() can be called
      // via the call to base.M();
      String source =
@"class MainClass {
    class SuperClass {
        public virtual void M() {

        }
    }
    class SubClass : SuperClass {
        public override void M() {
            base.M();
        }
    }
    
    static void Main(string[] argv) {
        SubClass oSub = new SubClass();
        oSub.M(); // Both SubClass.M() and SuperClass.M() should be reachable.
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
      });
    }


    [TestMethod]
    public void TestVirtualCallViaSuperClass() {
      String source =
@"class MainClass {
    class SuperClass {
        public virtual void M() {

        }
    }
    class SubClass : SuperClass {
        public override void M() {
        }
    }
    
    static void Main(string[] argv) {
        SuperClass oSub = new SubClass();
        oSub.M(); // SubClass.M() should be reachable.
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));
      });
    }

    [TestMethod]
    public void TestVirtualCallViaSuperClassWithoutOverride() {
      String source =
@"class MainClass {
    class SuperClass {
        public virtual void M() {

        }
    }
    class SubClass : SuperClass {
       
    }
    
    static void Main(string[] argv) {
        SuperClass oSub = new SubClass();
        oSub.M(); // SuperClass.M() should be reachable.
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
      });
    }

    /*
     * 
     * PRECISION:
     * 
     * We would expect this to work, but it turns out the compiler generates the call to M as:
     * 
     * callvirt   instance void MainClass/SuperClass::M()
     * 
     * that is, it doesn't tell us that this is a call on a *compile-time* variable of type SubClass.
     * which would allow us know that it will definitely be dispatched SubClass.M.
     * 
     * This is really dumb. We could get around this by doing a simple flow analysis to figure out that
     * the compile-time type of the the receiver at the call is SubClass.
     *
     * This is REALLY dumb.
     

    [TestMethod]
    public void TestVirtualCallOnSubClassDoesNotBringInSuperClass() {
        // Even though the super class is constructed, there is no way
        // that the call to oSub.M() can invoke it.

        String source =
@"class MainClass {
    class SuperClass {
        public virtual void M() {

        }
    }
    class SubClass : SuperClass {
        public override void M() {

        }
    }
    
    static void Main(string[] argv) {
        SuperClass oSup = new SuperClass();
        SubClass oSub = new SubClass();
        oSub.M(); // There is no way this can call SuperClass.M()
    } 
}";

        RunRTAOnSource(source, (compilerResult, rta) => {
            Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));
           
            Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
        });
    }
    */

    [TestMethod]
    public void TestVirtualCallViaInterfaceToStruct() {
      String source =
@"class MainClass {
  interface HasM {
    void M();
  }
    
  public struct StructHasM : HasM {
    public void M() {}
  }
    static void Main(string[] argv) {
        StructHasM sm;
        HasM hasM = sm; // boxes sm
        hasM.M(); // StructHasM.M() should be reachable.
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "StructHasM", "M"));
      });
    }


    [TestMethod]
    public void TestVirtualCallViaRetroactiveInterface() {
      String source =
@"class MainClass {
  interface HasM {
    void M();
  }
    
  public class SuperClass {
    public void M() {}
  }

  public class SubClass : SuperClass, HasM {}

  static void Main(string[] argv) {      
        HasM hasM = new SubClass();
        hasM.M(); // SuperClass.M() should be reachable.
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
      });
    }

    [TestMethod]
    public void TestVirtualCallWithMultipleSpecializedInterfaces() {
      String source =
@"class MainClass {
  interface IHasM<T> {
    void M(T t);
  }
    
  public class SuperClass : IHasM<string> {
    public void M(string s) {}    
  }

  public class SubClass : SuperClass, IHasM<int> {
    public void M(int i) {}
  }

  static void Main(string[] argv) {      
        SuperClass s = new SubClass();
        ((IHasM<int>)s).M(5); // SubClass.M(int) should be reachable.
        ((IHasM<string>)s).M(""Hi1""); // SubClass.M(int) should be reachable.
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
      });
    }

    [TestMethod]
    public void TestVirtualMethodCallBeforeConstructor() {
      // Test to ensure M is reachable, even though it has not
      // been constructed yet when the analysis sees a.M()

      String source =
@"class MainClass {
    class A { internal virtual void M() { } }
    static void Main(string[] argv) {
        A a = null;
        for (int i = 0; i < 2; i++) {
            if (a != null) {
                a.M(); // First time through not executed
            }
            a = new A();
        }
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "A", "M"));
      });
    }

    [TestMethod]
    public void TestVirtualMethodNoSuperClassIfNotConstructed() {
      // Test to make sure SubClass.M is reachable but SuperClass.M
      // is not when no instance of the SuperClass has been
      // instantiated

      String source =
@"class MainClass {
    class SuperClass { internal virtual void M() { } }
    class SubClass : SuperClass { internal virtual void M() { } }
    static void Main(string[] argv) {
        SuperClass a = new SubClass();
        a.M();  // This calls SubClass's version of M
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));

        //PRECISION
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
      });
    }


    [TestMethod]
    public void TestVirtualMethodOnlyConstructed() {
      // Test to ensure that only those versions of M in constructed classes are
      // reachable.

      String source =
@"class MainClass {
    class SuperClass { internal virtual void M() { } }
    class SubClass1 : SuperClass { internal virtual void M() { } }
    class SubClass2 : SuperClass { internal virtual void M() { } }
    class SubClassUnused : SuperClass { internal virtual void M() { } }

    static void Main(string[] argv) {
        SuperClass a = null;
        if (argv.Length == 0) {
            a = new SuperClass();
        } else if (argv.Length == 1) {
            a = new SubClass1();
        } else {
            a = new SubClass2();
        }
        a.M();  // No way this can call SubClassUnused
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass1", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass2", "M"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClassUnused", "M"));
      });
    }

    [TestMethod]
    public void TestVirtualCallOnSubClassOfSpecialized() {
      String source =
@"public class MainClass {
  public class SuperClass<T> {
    public virtual void M(T t) {}
    public virtual void F(T t) {InternalM(t);}

    public virtual void InternalM(T f) {}
  }
    
  public class SubClass<T> : SuperClass<T> {
    public override void M(T t) {}
  }

  public class SubSubClass : SubClass<string> {
    public override void InternalM(string f) {}
  }

  static void Main(string[] argv) {      
        SubSubClass s = new SubSubClass();
        s.M(""Hi!""); // SubClass.M should be reachable 
        s.F(""Bye!""); // SuperClass.F should be reachable and SubSubClass.InternalM should be reachable
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "F"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubSubClass", "InternalM"));
      });
    }

    [TestMethod]
    public void TestVirtualCallOnSubClassOfSpecializedViaSuperClass() {
      String source =
@"public class MainClass {
  public class SuperClass<T> {
    public virtual void M(T t) {}
    public virtual void F(T t) {}
  }
    
  public class SubClass<T> : SuperClass<T> {
    public override void M(T t) {}
  }

  public class SubSubClass : SubClass<string> {
  }

  static void Main(string[] argv) {      
        SuperClass<string> s = new SubSubClass();
        s.M(""Hi!""); // SubClass.M should be reachable
        s.F(""Bye!""); // SuperClass.F should be reachable
    } 
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "F"));
      });
    }


    [TestMethod]
    public void TestNonVirtualInterfaceDispatch() {
      // Test to ensure that methods implementing interfaces are reachable.

      String source =
@"class MainClass {
    interface IM { void M(); }
    class M1 : IM { public void M() { } }
    class M2 : IM { public void M() { } }
    class MUnused : IM { public  void M() { } }

    static void Main(string[] argv) {
        IM a = null;
        if (argv.Length == 0) {
            a = new M1();
        } else {
            a = new M2();
        } 
        a.M();  // No way this can call MUnused
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "M1", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "M2", "M"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "MUnused", "M"));
      });
    }

    [TestMethod]
    public void TestNonVirtualViaSubClass() {
      // Test to ensure that methods implementing interfaces are reachable.

      String source =
@"class MainClass {
  public class SuperClass {
   public void F() {}
  }
  public class SubClass : SuperClass {}

  static void Main(string[] argv) {
         SubClass s = new SubClass();

        s.F();
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "F"));
      });
    }

    [TestMethod]
    public void TestCallStaticDelegate() {
      // Test to ensure that static methods called via delegates are reachable

      String source =
@"class MainClass {
  delegate void DoSomethingDelegate();
 
  class M {
      public static void S() {}
      public static void Unreachable() {}
   }

    static void Main(string[] argv) {
       DoSomethingDelegate d = M.S;

       d();
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "M", "S"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "M", "Unreachable"));
      });
    }

    [TestMethod]
    public void TestCallVirtualDelegate() {
      // Test to ensure that virtual methods called via delegates are reachable

      String source =
@"class MainClass {
  delegate void DoSomethingDelegate();
  class SuperClass {
     public virtual void S() {
     }
  }
  class SubClass : SuperClass {
      public override void S() {}
      public void Unreachable() {}
   }

    static void Main(string[] argv) {
      SuperClass s = new SubClass();
  
       DoSomethingDelegate d = s.S;

       d();
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "S"));

        //PRECISION
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "S"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "Unreachable"));
      });
    }

    [TestMethod]
    public void TestCallInterfaceDelegate() {
      // Test to ensure that interface methods called via interfaces are reachable

      String source =
@"class MainClass {
  delegate void DoSomethingDelegate();
  interface HasS {
     void S();
  }
  class SubClass : HasS {
      public void S() {}
      public void Unreachable() {}
   }

    static void Main(string[] argv) {
      HasS s = new SubClass();
  
       DoSomethingDelegate d = s.S;

       d();
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "S"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "Unreachable"));
      });
    }


    [TestMethod]
    public void TestCallNonVirtualDelegate() {
      // Test to ensure that non-virtual methods called via delegates are reachable

      String source =
@"class MainClass {
  delegate void DoSomethingDelegate();
  class SuperClass {
     public void S() {
     }
  }
  class SubClass : SuperClass {
      public void S() {}
      public void Unreachable() {}
   }

    static void Main(string[] argv) {
      SuperClass s = new SubClass();
  
       DoSomethingDelegate d = s.S;

       d();
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "S"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "S"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "Unreachable"));
      });
    }

    [TestMethod]
    public void TestCallVirtualDelegateBeforeConstructor() {
      // Test to ensure M is reachable, even though it has not
      // been constructed yet when the analysis sees a.M()

      String source =
@"class MainClass {
    delegate void DoSomethingDelegate();

    class A { internal virtual void M() { } }
    static void Main(string[] argv) {
        A a = null;
        for (int i = 0; i < 2; i++) {
            if (a != null) {
                 // First time through not executed
                DoSomethingDelegate d = a.M;
                d();
            }
            a = new A();
        }
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "A", "M"));
      });
    }

    [TestMethod]
    public void TestCallsAcrossAssemblyBoundary() {
      // Break up source into library and application assemblies
      string librarySource =
@"public class SuperClass {
    public virtual void M() {
    }

   public void F() {

   }
}
public class SubClass : SuperClass {
  public override void M() {

  }
}
";

      string applicationSource = @"
class Program {
    static void Main(string[] argv) {
        SubClass s = new SubClass();
        s.M();
        s.F();
    }
}";

      RunRTAOnSources(new string[] { librarySource, applicationSource }, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "F"));

        //PRECISION
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
      });
    }

    [TestMethod]
    public void TestCallViaSuperClassAcrossAssemblyBoundary() {
      // Break up source into library and application assemblies
      string librarySource =
@"public class SuperClass {
    public virtual void M() {
    }
}
public class SubClass : SuperClass {
  public override void M() {

  }
}
";

      string applicationSource = @"
class Program {
    static void Main(string[] argv) {
        SuperClass s = new SubClass();
        s.M();
    }
}";

      RunRTAOnSources(new string[] { librarySource, applicationSource }, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "M"));

        //PRECISION
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "M"));
      });
    }


    [TestMethod]
    public void TestGenericTypeReachability() {
      // Test to ensure that generic methods called via a
      // an instantiated class are reachable.

      String librarySource =
@"public class GenericWrapper<T> {
    T value;
    public GenericWrapper(T v) { this.value = v;}

    public static void StaticMethod(T t, T x) {}

    public virtual void VirtualMethod(T t, T x) {}

    public void NonVirtualMethod(T t, T x) {}
  }";

      string applicationSource =
    @" public class MainClass {
  public class Foo {}

  static void Main(string[] argv) {
         Foo f = new Foo();
         GenericWrapper<Foo> fW = new GenericWrapper<Foo>(f);

         GenericWrapper<Foo>.StaticMethod(f,f);
         fW.VirtualMethod(f,f);
         fW.NonVirtualMethod(f,f);
    }
}";

      RunRTAOnSources(new string[] { librarySource, applicationSource }, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", ".ctor"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", "StaticMethod"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", "VirtualMethod"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", "NonVirtualMethod"));

        AssertRTATypesNotDummy(rta);
      });
    }

    [TestMethod]
    public void TestGenericMethodReachability() {
      // Test to ensure that generic methods called via a
      // an instantiated class are reachable.

      String librarySource =
@"public class GenericWrapper<T> {
    T value;
    public GenericWrapper(T v) { this.value = v;}

    public static void StaticMethod<X>(T t, X x) {}

    public virtual void VirtualMethod<X>(T t, X x) {}

    public void NonVirtualMethod<X>(T t, X x) {}
  }";

      string applicationSource =
    @" public class MainClass {
  public class Foo {}

  static void Main(string[] argv) {
         Foo f = new Foo();
         GenericWrapper<Foo> fW = new GenericWrapper<Foo>(f);

         GenericWrapper<Foo>.StaticMethod(f,f);
         fW.VirtualMethod<Foo>(f,f);
         fW.NonVirtualMethod<Foo>(f,f);
    }
}";

      RunRTAOnSources(new string[] { librarySource, applicationSource }, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", ".ctor"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", "StaticMethod"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", "VirtualMethod"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "GenericWrapper", "NonVirtualMethod"));

        AssertRTATypesNotDummy(rta);
      });
    }

    [TestMethod]
    public void TestEvilGenericAllocationViaClassTypeVariable() {
      // Need to make sure new T() causes T to be
      // marked as constructed.

      String source =
@"public class GenericFactory<ALLOC> where ALLOC : new(){
    
    public ALLOC AllocateInstance() {
       return new ALLOC();
    }

    public static ALLOC AllocateStatic() {
      return new ALLOC();
    }
  }

  public class Super {
    public virtual void M() {}
  }

  public class SubClass1 : Super {
    public override void M() {}
  }

  public class SubClass2 : Super {
    public override void M() {}
  }

 public class MainClass {

  static void Main(string[] argv) {
     GenericFactory<SubClass1> factory = new GenericFactory<SubClass1>();
     Super s1 = factory.AllocateInstance();

     s1.M();

     Super s2 = GenericFactory<SubClass2>.AllocateStatic();

     s2.M(); 
    }
}";

      // Also need to test class type variables via static methods

      RunRTAOnSource(source, (compilerResult, rta) => {

        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass1", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass2", "M"));

        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass1", ".ctor"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass2", ".ctor"));

        AssertRTATypesNotDummy(rta);
      });
    }

    [TestMethod]
    public void TestEvilGenericAllocationViaMethodTypeVariable() {
      // Need to make sure new T() causes T to be
      // marked as constructed.

      String source =
@"public class GenericFactory {
    
    public static ALLOC1 AllocateStatic<ALLOC1>() where ALLOC1 : new() {
       return new ALLOC1();
    }

   public ALLOC2 AllocateInstance<ALLOC2>() where ALLOC2 : new() {
       return new ALLOC2();
    }
  }

  public class Super {
    public virtual void M() {}
  }

  public class SubClass1 : Super {
    public override void M() {}
  }

  public class SubClass2 : Super {
    public override void M() {}
  }

 public class MainClass {

  static void Main(string[] argv) {
    Super s1 = GenericFactory.AllocateStatic<SubClass1>();

     GenericFactory factory = new GenericFactory();
     Super s2 = factory.AllocateInstance<SubClass2>();

     s1.M();

     s2.M();  
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {

        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass1", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass2", "M"));

        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass1", ".ctor"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass2", ".ctor"));

        AssertRTATypesNotDummy(rta);
      });
    }

    [TestMethod]
    public void TestEnumeratorYield() {
      // Test to ensure that the code in a generated enumerator is reachable
      String source =
@"using System.Collections.Generic;
class MainClass { 
   static IEnumerable<string> EnumerateStrings() {
      for (int i = 0; i < 100; i++) {
        yield return GenerateString(i);
      }
      yield break;
    }

   static string GenerateString(int i) {
      return ""Foo"" + i;
   }

   static void Main(string[] argv) {
       foreach (string s in EnumerateStrings()) {
       }
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "MainClass", "GenerateString"));
      });
    }

    [TestMethod]
    public void TestCallViaInstantiatedGenericInterface() {
      // Test to ensure that implementations of interface methods called
      // via an instantiated generic interface are reachable.
      String source =
@"using System.Collections.Generic;
class MainClass { 
 public interface HasM<T> {
   void M(T t);
 }

  public class Foo : HasM<string> {
     public void M(string s) {
     }
  }

   static void Main(string[] argv) {
       HasM<string> hasM = new Foo();

        hasM.M(""stuff"");
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "Foo", "M"));
      });
    }

    [TestMethod]
    public void TestFinalizersReachable() {
      // Test to ensure that finalizers of constructed classes are
      // reachable

      String source =
@"
class MainClass { 
 
 class SuperClass {
    ~SuperClass() {
      DoCleanup();
    }

    void DoCleanup() {}
  }

  class SubClass : SuperClass {
    ~SubClass() {
       DoMoreCleanup();
     }

    void DoMoreCleanup() {}
  }

  class SubSubClass : SubClass {
    ~SubSubClass() {}
  }

  class Unrelated {
    ~Unrelated() {}

  }
   static void Main(string[] argv) {
       SubClass s = new SubClass();
    }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "Finalize"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "DoCleanup"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "Finalize"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubClass", "DoMoreCleanup"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SubSubClass", "Finalize"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "Unrelated", "Finalize"));
      });
    }

    [TestMethod]
    public void TestFinalizerInSuperClass() {
      // Test to ensure that the finalizer of a super class of a constructed class
      // without a finalizer itself is reachable.

      String source =
@"
class MainClass { 
 
 class SuperClass {
    ~SuperClass() {
      DoCleanup();
    }

    void DoCleanup() {}
  }

  class SubClass : SuperClass {
}

  static void Main(string[] argv) {
       SubClass s = new SubClass();
  }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "Finalize"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "SuperClass", "DoCleanup"));
      });
    }

    [TestMethod]
    public void TestFinalizerChain() {
      // Test to ensure that the types constructed in a finalizer have their finalizers called.

      String source =
@"
class MainClass { 
 
 class A {
    ~A() {
      new B();
    }
  }

  class B {
   ~B() {
    new C();
   }
  }

  class C {
    ~C() {}
  }

  static void Main(string[] argv) {
       A s = new A();
  }
}";

      RunRTAOnSource(source, (compilerResult, rta) => {
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "A", "Finalize"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "B", "Finalize"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResult.Host, rta, "C", "Finalize"));
      });
    }

    [TestMethod]
    public void TestReachabilityWithMultipleEntryPoints() {
      // Tests for running the RTA with multiple entry points.

      string source =
@"
class A {
  public static void AEntry() {
    F();
  }

  public static void F() {}

  public static void Main(string[] args) {}
}

class B {
  public static void BEntry() {
    G();
  }

  public static void G() {}
}
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        RapidTypeAnalysis rta = new RapidTypeAnalysis(wholeProgram, TargetProfile.Desktop);

        IMethodDefinition[] entryPoints = new IMethodDefinition[] { compilerResults.FindMethodWithName("A", "AEntry"), 
                                                                    compilerResults.FindMethodWithName("B", "BEntry") };

        rta.Run(entryPoints);

        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "A", "AEntry"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "A", "F"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "B", "BEntry"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "B", "G"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "A", "Main"));
      });
    }

    [TestMethod]
    public void TestReachabilityWithConstructorEntryPoint() {
      // Tests for running the RTA with multiple entry points.

      string source =
@"
class A {
  public static void AEntry(SuperClass s) {
    s.M();
  }
}

class SuperClass {
  public virtual void M() {}
}

class SubClass1 : SuperClass {
  public override void M() {}
}

class SubClass2 : SuperClass {
  public override void M() {}
}
 
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        RapidTypeAnalysis rta = new RapidTypeAnalysis(wholeProgram, TargetProfile.Desktop);

        IMethodDefinition[] entryPoints = new IMethodDefinition[] { compilerResults.FindMethodWithName("A", "AEntry"), 
                                                                    compilerResults.FindMethodWithName("SubClass1", ".ctor") };

        rta.Run(entryPoints);

        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "A", "AEntry"));

        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "SubClass1", "M"));
        Assert.IsTrue(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "SubClass1", ".ctor"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "SubClass2", "M"));
        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "SubClass2", ".ctor"));

        Assert.IsFalse(RapidTypeAnalysisTests.IsMethodReachable(compilerResults.Host, rta, "SuperClass", "M"));

      });
    }


    /// <summary>
    /// Searches the analysis for a field by the name of f and returns a bool depending on whether f is reachable
    /// </summary>
    static bool IsFieldReachable(PeReader.DefaultHost host, RapidTypeAnalysis analysis, string f) {
      var fieldName = host.NameTable.GetNameFor(f);
      bool found = false;
      foreach (var field in analysis.ReachableFields()) {
        if (fieldName.UniqueKey == field.Name.UniqueKey) {
          found = true;
          break;
        }
      }
      return found;
    }

    /// <summary>
    /// Searches the analysis for a method by the name of f and returns a bool depending on whether f is reachable
    /// </summary>
    static bool IsMethodReachable(PeReader.DefaultHost host, RapidTypeAnalysis analysis, string className, string methodName) {
      var methodIName = host.NameTable.GetNameFor(methodName);
      var classIName = host.NameTable.GetNameFor(className);


      // t-devinc: perhaps should change this to use TestCompilerResult.FindMethod()
      // This would allow us to be more clear about specialized vs. not
      // in our tests.

      bool found = false;
      foreach (var reachableMethod in analysis.ReachableMethods()) {

        var containingTypeReference = reachableMethod.ContainingType; //Going to fail if we have a specialized method (which we will -- will have to make sure to get the unspecialized in the RTA).

        INamedTypeReference containingNamedTypeReference;

        if (containingTypeReference is IGenericTypeInstanceReference) {
          containingNamedTypeReference = ((IGenericTypeInstanceReference)containingTypeReference).GenericType;
        }
        else {
          containingNamedTypeReference = ((INamedTypeReference)containingTypeReference);
        }

        IName reachableClassIName = containingNamedTypeReference.Name;

        if (methodIName.UniqueKey == reachableMethod.Name.UniqueKey && classIName.UniqueKey == reachableClassIName.UniqueKey) {
          found = true;
          break;
        }
      }
      return found;
    }

    static void AssertRTATypesNotDummy(RapidTypeAnalysis rta) {
      foreach (var typeDefinition in rta.ReachableTypes()) {
        Assert.IsTrue(!(typeDefinition is Dummy));
      }
    }

    #endregion

    #region WholeProgram tests

    [TestMethod]
    public void TestWholeProgramAllTypes() {
      // This is a weak test for the basics.
      string librarySource =
@"namespace Lib {
    public class LibraryList {
       private class LibraryListEntry { }
    }
  }

  public class OutsideLib { }
";
      string applicationSource =
@"namespace App {
    class Program { }
    class AppList : Lib.LibraryList {
      GenericClass<string> gc = new GenericClass<string>();
    }

    class GenericClass<T> {}
  }
";

      ConstructWholeProgramForSources(new string[] { librarySource, applicationSource }, (compilerResults, wholeProgram) => {
        IEnumerable<ITypeDefinition> allTypes = wholeProgram.AllDefinedTypes();

        Assert.IsFalse(allTypes.Contains(Dummy.Type));

        // AllTypes should contain System.Object, etc.
        Assert.IsTrue(allTypes.Contains(compilerResults.Host.PlatformType.SystemObject.ResolvedType));
        Assert.IsTrue(allTypes.Contains(compilerResults.Host.PlatformType.SystemString.ResolvedType));

        // User types
        Assert.IsTrue(allTypes.Contains(compilerResults.FindTypeWithName("OutsideLib")));
        Assert.IsTrue(allTypes.Contains(compilerResults.FindTypeWithName("Lib.LibraryList.LibraryListEntry")));
        Assert.IsTrue(allTypes.Contains(compilerResults.FindTypeWithName("Lib.LibraryList")));
        Assert.IsTrue(allTypes.Contains(compilerResults.FindTypeWithName("App.GenericClass`1")));
        Assert.IsTrue(allTypes.Contains(compilerResults.FindTypeWithName("App.AppList")));
      });
    }

    [TestMethod]
    public void TestWholeProgramAllDefinedMethods() {
      // This is a weak test for the basics.
      string librarySource =
@"namespace Lib {
    public class LibraryList {
       private class LibraryListEntry { 
           private LibraryListEntry() {}
           ~LibraryListEntry() {}
           private void Foo() {}

           static void StaticMethod() {}
       }
    }
  }

  public class OutsideLib { }
";
      string applicationSource =
@"namespace App {
    class Program { }
    class AppList : Lib.LibraryList {
      GenericClass<string> gc = new GenericClass<string>();

      void M() {}
    }

    class GenericClass<T> {
      public void F() {}
    }
  }
";

      ConstructWholeProgramForSources(new string[] { librarySource, applicationSource }, (compilerResults, wholeProgram) => {
        IEnumerable<IMethodDefinition> allMethods = wholeProgram.AllDefinedMethods();

        Assert.IsFalse(allMethods.Contains(Dummy.Method));

        // User methods

        Assert.IsTrue(allMethods.Contains(compilerResults.FindMethodWithName("Lib.LibraryList.LibraryListEntry", ".ctor")));
        Assert.IsTrue(allMethods.Contains(compilerResults.FindMethodWithName("Lib.LibraryList.LibraryListEntry", "Finalize")));
        Assert.IsTrue(allMethods.Contains(compilerResults.FindMethodWithName("Lib.LibraryList.LibraryListEntry", "Foo")));
        Assert.IsTrue(allMethods.Contains(compilerResults.FindMethodWithName("Lib.LibraryList.LibraryListEntry", "StaticMethod")));
        Assert.IsTrue(allMethods.Contains(compilerResults.FindMethodWithName("App.AppList", "M")));
        Assert.IsTrue(allMethods.Contains(compilerResults.FindMethodWithName("App.GenericClass`1", "F")));
      });
    }

    #endregion

    #region ID string tests

    [TestMethod]
    public void TestStaticMethodIDStrings() {
      // Tests for static method IDs. These are methods
      // that can be actual entry points

      string source =
@"
namespace Ns {
  class Foo {
    static void Main(string[] args) {}
    static void Main() {}
    static void MyMain(int i) {}
    static int OtherEntry() { return 0;}
  }

  class Bar {
    static void Main(string[] args) {

    }
  }
}
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        DocumentationCommentDefinitionIdStringMap idStringMap = new DocumentationCommentDefinitionIdStringMap(wholeProgram.AllAssemblies());

        IMethodReference Ns_Foo_Main_strings = idStringMap.GetMethodDefinitionsWithIdentifier("M:Ns.Foo.Main(System.String[])").First();
        Assert.IsNotNull(Ns_Foo_Main_strings);
        Assert.AreEqual("Main", Ns_Foo_Main_strings.Name.Value);
        Assert.AreEqual("Foo", ((INamedTypeReference)Ns_Foo_Main_strings.ContainingType).Name.Value);
        Assert.AreEqual(1, Ns_Foo_Main_strings.ParameterCount);


        IMethodReference Ns_Foo_Main_void = idStringMap.GetMethodDefinitionsWithIdentifier("M:Ns.Foo.Main").First();
        Assert.IsNotNull(Ns_Foo_Main_void);
        Assert.AreEqual("Main", Ns_Foo_Main_void.Name.Value);
        Assert.AreEqual("Foo", ((INamedTypeReference)Ns_Foo_Main_void.ContainingType).Name.Value);
        Assert.AreEqual(0, Ns_Foo_Main_void.ParameterCount);

        IMethodReference Ns_Foo_MyMain_int = idStringMap.GetMethodDefinitionsWithIdentifier("M:Ns.Foo.MyMain(System.Int32)").First();
        Assert.IsNotNull(Ns_Foo_MyMain_int);
        Assert.AreEqual("MyMain", Ns_Foo_MyMain_int.Name.Value);
        Assert.AreEqual("Foo", ((INamedTypeReference)Ns_Foo_MyMain_int.ContainingType).Name.Value);
        Assert.AreEqual(1, Ns_Foo_MyMain_int.ParameterCount);

        IMethodReference Ns_Foo_OtherEntry_void = idStringMap.GetMethodDefinitionsWithIdentifier("M:Ns.Foo.OtherEntry").First();
        Assert.IsNotNull(Ns_Foo_OtherEntry_void);
        Assert.AreEqual("OtherEntry", Ns_Foo_OtherEntry_void.Name.Value);
        Assert.AreEqual("Foo", ((INamedTypeReference)Ns_Foo_OtherEntry_void.ContainingType).Name.Value);
        Assert.AreEqual(0, Ns_Foo_OtherEntry_void.ParameterCount);

        IMethodReference Ns_Bar_Main_strings = idStringMap.GetMethodDefinitionsWithIdentifier("M:Ns.Bar.Main(System.String[])").First();
        Assert.IsNotNull(Ns_Bar_Main_strings);
        Assert.AreEqual("Main", Ns_Bar_Main_strings.Name.Value);
        Assert.AreEqual("Bar", ((INamedTypeReference)Ns_Bar_Main_strings.ContainingType).Name.Value);
        Assert.AreEqual(1, Ns_Bar_Main_strings.ParameterCount);
      });
    }

    [TestMethod]
    public void TestCreateEntryPointListFromString() {
      // Test parsing whitespace separated entrypoint string.

      string source =
@"
namespace Ns {
  class Foo {
    static void A() {}
    static void B() {}
    static void C() {}
  }
}
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        DocumentationCommentDefinitionIdStringMap idStringMap = new DocumentationCommentDefinitionIdStringMap(wholeProgram.AllAssemblies());

        IEnumerable<IMethodReference> emptyList = idStringMap.CreateEntryPointListFromString("");
        Assert.AreEqual(0, emptyList.Count());

        IEnumerable<IMethodReference> singletonList = idStringMap.CreateEntryPointListFromString("M:Ns.Foo.A");
        Assert.AreEqual(1, singletonList.Count());
        Assert.AreEqual("A", singletonList.ElementAt(0).Name.Value);

        IEnumerable<IMethodReference> singletonListWithTerminatingNewline = idStringMap.CreateEntryPointListFromString("M:Ns.Foo.A\n");
        Assert.AreEqual(1, singletonListWithTerminatingNewline.Count());
        Assert.AreEqual("A", singletonListWithTerminatingNewline.ElementAt(0).Name.Value);

        IEnumerable<IMethodReference> newlineSeparatedList = idStringMap.CreateEntryPointListFromString("M:Ns.Foo.A\nM:Ns.Foo.B\nM:Ns.Foo.C\n");
        Assert.AreEqual(3, newlineSeparatedList.Count());
        Assert.AreEqual("A", newlineSeparatedList.ElementAt(0).Name.Value);
        Assert.AreEqual("B", newlineSeparatedList.ElementAt(1).Name.Value);
        Assert.AreEqual("C", newlineSeparatedList.ElementAt(2).Name.Value);

        IEnumerable<IMethodReference> spaceSeparatedList = idStringMap.CreateEntryPointListFromString("M:Ns.Foo.A M:Ns.Foo.B M:Ns.Foo.C");
        Assert.AreEqual(3, newlineSeparatedList.Count());
        Assert.AreEqual("A", newlineSeparatedList.ElementAt(0).Name.Value);
        Assert.AreEqual("B", newlineSeparatedList.ElementAt(1).Name.Value);
        Assert.AreEqual("C", newlineSeparatedList.ElementAt(2).Name.Value);
      });
    }


    #endregion

    #region ClassHierarchy tests

    [TestMethod]
    public void TestClassHierarchyDirectSubclasses() {
      string source =
@"public class SuperClass {}

  public interface HasM {
    void M();
  }

  public interface HasMAndF : HasM {
    void F();
  }

  public class SubClass1 : SuperClass, HasM {
    public void M() {}
  }
  
  public class SubClass2 : SuperClass {}

  public class SubSubClass1 : SubClass1 {}

  public class Unrelated {}
  
  public class FooWithM : HasM {
    public void  M() {}
  }
";

      ConstructClassHierarchyForSource(source, (compilerResults, hierarchy) => {
        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SubClass1", "SuperClass"));
        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SubClass2", "SuperClass"));
        Assert.IsFalse(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SuperClass", "SubClass1"));

        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SubSubClass1", "SubClass1"));
        Assert.IsFalse(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SubSubClass1", "SuperClass"));

        Assert.IsFalse(ClassDirectlySubclassesClass(compilerResults, hierarchy, "Unrelated", "SuperClass"));
        Assert.IsFalse(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SuperClass", "Unrelated"));

        // A class is not a subclass of itself
        Assert.IsFalse(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SuperClass", "SuperClass"));

        // Interfaces
        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SubClass1", "HasM"));
        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "FooWithM", "HasM"));

        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "HasMAndF", "HasM"));

        Assert.IsFalse(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SubSubClass1", "HasM"));
        Assert.IsFalse(ClassDirectlySubclassesClass(compilerResults, hierarchy, "Unrelated", "HasM"));
      });
    }

    [TestMethod]
    public void TestClassHierarchyDirectSubclassesSystem() {
      string source =
@"public class Foo {}
  public interface HasM {
    void M();
  }  
";

      ConstructClassHierarchyForSource(source, (compilerResults, hierarchy) => {
        Assert.IsTrue(hierarchy.DirectSubClassesOfClass(compilerResults.Host.PlatformType.SystemObject.ResolvedType).Contains(compilerResults.FindTypeWithName("Foo")));
        Assert.IsTrue(hierarchy.DirectSubClassesOfClass(compilerResults.Host.PlatformType.SystemObject.ResolvedType).Contains(compilerResults.FindTypeWithName("HasM")));

        Assert.IsTrue(hierarchy.DirectSubClassesOfClass(compilerResults.Host.PlatformType.SystemObject.ResolvedType).Contains(compilerResults.Host.PlatformType.SystemString.ResolvedType));
      });
    }


    [TestMethod]
    public void TestClassHierarchyGenerics() {
      string source =
@"
  public interface HasM<T> {
    void M(T t);
  }

  public class GenericSuperClass<T> {}
  public class GenericSubClassOfGenericSuperClass<T> : GenericSuperClass<T> {}

  public class SpecializedSubClassOfGenericSuperClass : GenericSuperClass<string> {}

  public class FooHasMGeneric<T> : HasM<T> {
    public void M(T t) {}
  }

  public class FooHasMSpecialized : HasM<string> {
    public void M(string t) {}
  }
";

      ConstructClassHierarchyForSource(source, (compilerResults, hierarchy) => {


        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "GenericSubClassOfGenericSuperClass`1", "GenericSuperClass`1"));

        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "SpecializedSubClassOfGenericSuperClass", "GenericSuperClass`1"));

        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "FooHasMGeneric`1", "HasM`1"));

        Assert.IsTrue(ClassDirectlySubclassesClass(compilerResults, hierarchy, "FooHasMSpecialized", "HasM`1"));

      });
    }


    [TestMethod]
    public void TestClassHierarchyAllSubclasses() {
      string source =
@"public class SuperClass {}

  public interface HasM {
    void M();
  }

  public interface HasMAndF : HasM {
    void F();
  }

  public class SubClass1 : SuperClass, HasM {
    public void M() {}
  }
  
  public class SubClass2 : SuperClass {}

  public class SubSubClass1 : SubClass1 {}

  public class Unrelated {}
  
  public class FooWithM : HasM {
    public void  M() {}
  }

  public class FooWithMAndF : FooWithM, HasMAndF {
    public void F() { }
  }
";

      ConstructClassHierarchyForSource(source, (compilerResults, hierarchy) => {
        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "SubClass1", "SuperClass"));
        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "SubClass2", "SuperClass"));
        Assert.IsFalse(ClassIsSubClassOfClass(compilerResults, hierarchy, "SuperClass", "SubClass1"));

        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "SubSubClass1", "SubClass1"));
        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "SubSubClass1", "SuperClass"));

        Assert.IsFalse(ClassIsSubClassOfClass(compilerResults, hierarchy, "Unrelated", "SuperClass"));
        Assert.IsFalse(ClassIsSubClassOfClass(compilerResults, hierarchy, "SuperClass", "Unrelated"));

        // A class is not a subclass of itself
        Assert.IsFalse(ClassIsSubClassOfClass(compilerResults, hierarchy, "SuperClass", "SuperClass"));

        // Interfaces
        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "SubClass1", "HasM"));
        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "FooWithM", "HasM"));

        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "HasMAndF", "HasM"));
        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "FooWithMAndF", "HasM"));

        Assert.IsTrue(ClassIsSubClassOfClass(compilerResults, hierarchy, "SubSubClass1", "HasM"));
        Assert.IsFalse(ClassIsSubClassOfClass(compilerResults, hierarchy, "Unrelated", "HasM"));
      });
    }

    static bool ClassDirectlySubclassesClass(TestCompilerResults compilerResults, ClassHierarchy hierarchy, String subClassName, String superClassName) {
      ITypeDefinition subClassDefinition = compilerResults.FindTypeWithName(subClassName);
      Assert.AreNotEqual(null, subClassDefinition, "Couldn't find definition for {0}", subClassName);

      ITypeDefinition superClassDefinition = compilerResults.FindTypeWithName(superClassName);
      Assert.AreNotEqual(null, superClassDefinition, "Couldn't find definition for {0}", superClassName);

      return hierarchy.DirectSubClassesOfClass(superClassDefinition).Contains(subClassDefinition);
    }

    static bool ClassIsSubClassOfClass(TestCompilerResults compilerResults, ClassHierarchy hierarchy, String subClassName, String superClassName) {
      ITypeDefinition subClassDefinition = compilerResults.FindTypeWithName(subClassName);
      ITypeDefinition superClassDefinition = compilerResults.FindTypeWithName(superClassName);

      return hierarchy.AllSubClassesOfClass(superClassDefinition).Contains(subClassDefinition);
    }

    #endregion

    #region GarbageCollectHelper.Implements tests

    [TestMethod]
    public void TestFindVirtualImplementations() {
      String source =
@"class SuperClass {
   public virtual void M() {}
 }

 class SubClass1 : SuperClass {
  public override void M() {}
 }

 class SubSubClass1 : SubClass1 {
  public override void M() {}
 }

 class SubClass2 : SuperClass {
 }

 class SubSubClass2 : SubClass2 {
 }

 ";

      CompileSourceAndRun(source, ".dll", (compiledResult) => {
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass::M", "SuperClass", "SuperClass", "SuperClass::M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass::M", "SubClass1", "SuperClass", "SubClass1::M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass::M", "SubSubClass1", "SuperClass", "SubSubClass1::M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass::M", "SubSubClass2", "SuperClass", "SuperClass::M"));


        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SubClass1::M", "SubClass1", "SuperClass", "SubClass1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SubClass1::M", "SubSubClass1", "SuperClass", "SubSubClass1::M"));

      });

    }

    [TestMethod]
    public void TestFindGenericVirtualImplementations() {
      String source =
@"class SuperClass<T> {
   public virtual void M(T t) {}
   public virtual void F(T t) {}
 }

 class SubClass<T> : SuperClass<T> {
  public override void M(T t) {}
 }

 class SubSubClass<T> : SubClass<T> {
  public override void M(T t) {}
 }

  class SubSpecializedSubClass : SubClass<string> {
  public override void M(string t) {}
 }
 ";

      CompileSourceAndRun(source, ".dll", (compiledResult) => {

        // Find implementations of SuperClass::M in its hierarchy
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::M", "SuperClass`1", "SuperClass`1", "SuperClass`1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::M", "SubClass`1", "SuperClass`1", "SubClass`1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::M", "SubSubClass`1", "SuperClass`1", "SubSubClass`1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::M", "SubSpecializedSubClass", "SuperClass`1", "SubSpecializedSubClass::M"));

        // Find implementations of SuperClass::F in its hierarchy
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::F", "SuperClass`1", "SuperClass`1", "SuperClass`1::F"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::F", "SubClass`1", "SuperClass`1", "SuperClass`1::F"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::F", "SubSubClass`1", "SuperClass`1", "SuperClass`1::F"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass`1::F", "SubSpecializedSubClass", "SuperClass`1", "SuperClass`1::F"));

        // Find implementations of SubClass::M in its hierarchy
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SubClass`1::M", "SubClass`1", "SuperClass`1", "SubClass`1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SubClass`1::M", "SubSubClass`1", "SuperClass`1", "SubSubClass`1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SubClass`1::M", "SubSpecializedSubClass", "SuperClass`1", "SubSpecializedSubClass::M"));


        // Find implementations of SubSpecializedSubClass::M in its hierarchy
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SubSpecializedSubClass::M", "SubSpecializedSubClass", "SuperClass`1", "SubSpecializedSubClass::M"));
      });

    }

    [TestMethod]
    public void TestFindGenericInterfaceImplementations() {
      String source =
@"
  public interface IHasM<T> {
    void M(T t);
  }

  public interface IHasF<T> {
    void F(T t);
  }

  public interface IHasFAndM<T> : IHasM<T> {
    void F(T t);
  }

  public class HasFAndM<T> : IHasFAndM<T> {
   public void M(T t) {}
   public virtual void F(T f) {}
  }

  public class SubHasFAndM<T> : HasFAndM<T> {
   public override void F(T f) {}
  }

  public class MultipleMs : IHasFAndM<string>, IHasFAndM<int> {
    public virtual void M(string s) {}
    public virtual void M(int i) {}

    public virtual void F(string s) {}
    public virtual void F(int i) {}
  }

  public class SubMultipleMs1 : MultipleMs {
    public override void M(int s) {}
  }

  public class SubMultipleMs2 : MultipleMs  {
    public override void M(string s) {}
  }

  // This extra level of overriding is because we don't have a way (currently) of referring to 
  // two different methods with the same name in the same class in TestCompilerResults.FindMethod().
  // We should add this.

  public class SubSubMultipleMs1 : SubMultipleMs1 {
    public override void M(string s) {}
  }

  public class SubSubMultipleMs2 : SubMultipleMs2  {
    public override void M(int s) {}
  }

 ";

      CompileSourceAndRun(source, ".dll", (compiledResult) => {

        // Find implementations of IHasM::M in its hierarchy
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "IHasM`1::M", "HasFAndM`1", "HasFAndM`1", "HasFAndM`1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "IHasM`1::M", "SubHasFAndM`1", "HasFAndM`1", "HasFAndM`1::M"));

        // Find implementations of IHasFAndM::F in its hierarchy
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "IHasFAndM`1::F", "HasFAndM`1", "HasFAndM`1", "HasFAndM`1::F"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "IHasFAndM`1::F", "SubHasFAndM`1", "HasFAndM`1", "SubHasFAndM`1::F"));

        // Test a class implementing an interface multiple times
        // Since we've unspecialized the target method, we can't tell the intent was to call the string version or the int version
        // so we have to assume it could have been either.

        Assert.IsTrue(ImplementationOfMethodForClassMayBeMethods(compiledResult, "IHasM`1::M", "SubSubMultipleMs1", "MultipleMs", "SubSubMultipleMs1::M", "SubMultipleMs1::M"));
        Assert.IsTrue(ImplementationOfMethodForClassMayBeMethods(compiledResult, "IHasM`1::M", "SubSubMultipleMs2", "MultipleMs", "SubMultipleMs2::M", "SubSubMultipleMs2::M"));

      });

    }

    [TestMethod]
    public void TestFindImplicitInterfaceImplementations() {
      String source =
@"
 interface HasM1 {
   void M();
 }

 interface HasM2 {
    void M();
 }

 class SuperClass : HasM1 {
   public virtual void M() {}
 }

 class SubClass : SuperClass {
   public override void M() {}
 }

  // Retroactive interface
 class SubSubClass : SubClass, HasM2 {
 }
 ";

      CompileSourceAndRun(source, ".dll", (compiledResult) => {
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SuperClass", "SuperClass", "SuperClass::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SubClass", "SuperClass", "SubClass::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SubSubClass", "SuperClass", "SubClass::M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM2::M", "SubSubClass", "SuperClass", "SubClass::M"));
      });
    }

    [TestMethod]
    public void TestFindExplicitInterfaceImplementations() {
      String source =
@"
 interface HasM1 {
   void M();
 }

 interface HasM2 {
    void M();
 }

 interface SubHasM1 : HasM1 {
    void C();
 }

 class SuperClass : HasM1 {
   public virtual void M() {}
 }

 class SubClass : SuperClass { }

 class SubSubClass : SubClass, HasM2 {
   public override void M() {}
   void HasM2.M() {}
 }

 class SubSubSubClass : SubSubClass, HasM1 {
   void HasM1.M() { }
 }

 class SubSubSubSubClass : SubSubSubClass {}

 class SubHasM1Implementation : SubHasM1 {
   void HasM1.M() {}

   void SubHasM1.C() {}
 }
 ";

      CompileSourceAndRun(source, ".dll", (compiledResult) => {
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SuperClass", "SuperClass", "SuperClass::M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SubSubClass", "SuperClass", "SubSubClass::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM2::M", "SubSubClass", "SuperClass", "SubSubClass::HasM2.M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SubSubSubClass", "SuperClass", "SubSubSubClass::HasM1.M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM2::M", "SubSubSubClass", "SuperClass", "SubSubClass::HasM2.M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SubSubSubSubClass", "SuperClass", "SubSubSubClass::HasM1.M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM2::M", "SubSubSubSubClass", "SuperClass", "SubSubClass::HasM2.M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SuperClass::M", "SubSubSubSubClass", "SuperClass", "SubSubClass::M"));
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "SubSubClass::M", "SubSubSubSubClass", "SuperClass", "SubSubClass::M"));


        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "HasM1::M", "SubHasM1Implementation", "SubHasM1Implementation", "SubHasM1Implementation::HasM1.M"));

      });
    }

    [TestMethod]
    public void TestFindGenericExplicitInterfaceImplementations() {
      String source =
@"
  interface IHasM<T> {
    T M(T t);
  }

  class GenericSuperClass<T> : IHasM<T> {
     T IHasM<T>.M(T t) { return t; }
  }

  class SpecializedSubClass : GenericSuperClass<string> {}

 ";

      CompileSourceAndRun(source, ".dll", (compiledResult) => {
        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "IHasM`1::M", "GenericSuperClass`1", "GenericSuperClass`1", "GenericSuperClass`1::IHasM<T>.M"));

        Assert.IsTrue(ImplementationOfMethodForClassIsMethod(compiledResult, "IHasM`1::M", "SpecializedSubClass", "GenericSuperClass`1", "GenericSuperClass`1::IHasM<T>.M"));

      });
    }

    static bool ImplementationOfMethodForClassIsMethod(TestCompilerResults compiledResults, string methodToImplementName,
        string lookupClassName,
        string uptoClassName,
        string expectedImplementationName) {

      return ImplementationOfMethodForClassMayBeMethods(compiledResults, methodToImplementName, lookupClassName, uptoClassName, expectedImplementationName);
    }

    static bool ImplementationOfMethodForClassMayBeMethods(TestCompilerResults compiledResults, string methodToImplementName,
    string lookupClassName,
    string uptoClassName,
    params string[] expectedImplementationNames) {

      IMethodDefinition methodToImplement = compiledResults.FindMethodWithName(methodToImplementName);
      Assert.IsNotNull(methodToImplement, "Couldn't find method {0}", methodToImplementName);

      ITypeDefinition lookupClass = compiledResults.FindTypeWithName(lookupClassName);
      Assert.IsNotNull(lookupClass, "Couldn't find class {0}", lookupClassName);

      ITypeDefinition uptoClass = compiledResults.FindTypeWithName(uptoClassName);
      Assert.IsNotNull(uptoClass, "Couldn't find class {0}", uptoClassName);


      IMethodDefinition[] expectedImplementations = expectedImplementationNames.Select(name => {
        IMethodDefinition expectedImplementation = compiledResults.FindMethodWithName(name);
        Assert.IsNotNull(expectedImplementation, "Couldn't find method {0}", name);
        return expectedImplementation;
      }).ToArray();


      IMethodDefinition[] actualImplementations = ((IEnumerable<IMethodDefinition>)GarbageCollectHelper.Implements(lookupClass, uptoClass, methodToImplement)).ToArray();

      if (expectedImplementations.Count() == actualImplementations.Count()) {
        foreach (IMethodDefinition expectedImplementation in expectedImplementations) {
          if (!actualImplementations.Contains(expectedImplementation)) {
            return false;
          }
        }
      }
      else {
        return false;
      }

      return true;
    }

    #endregion

    #region LocalFlowSummarizerTests

    [TestMethod]
    public void TestStraightLineOperandStackLocalFlow() {

      string source =
@"internal class Super {
    internal virtual int M(bool f) {return 17;}
  }

  internal class Sub1 : Super {
    internal Sub1(int param) {
    }

    internal override int M(bool f) {
      return 32;
    }
  }

 class Foo {
   void Run() {
      (new Sub1(17)).M(false);
   }
 }
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        IMethodDefinition fooRunMethod = compilerResults.FindMethodWithName("Foo::Run");
        Assert.IsNotNull(fooRunMethod);

        LocalFlowMethodSummarizer summarizer = new TypesLocalFlowMethodSummarizer();

        Assert.IsTrue(summarizer.CanSummarizeMethod(fooRunMethod));

        ReachabilitySummary summary = summarizer.SummarizeMethod(fooRunMethod, wholeProgram);
        Assert.IsNotNull(summary);

        Assert.IsTrue(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub1::M")));
        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Super::M")));

      });
    }

    [TestMethod]
    public void TestJoinedOperandStackLocalFlow() {

      string source =
@"internal class Super {
    public Super() {}
    internal virtual int M(bool f) {return 17;}
  }

  internal class Sub1 : Super {
    internal Sub1(int param) {}

    internal override int M(bool f) {
      return 32;
    }
  }

  internal class SubSub1 : Sub1 {
    internal SubSub1(int param) :base(param) {}

    internal override int M(bool f) {
      return 66;
    }
  }

 class Foo {
   void Run(bool flag) {
      (flag? new Sub1(17) : new SubSub1(3)).M(false);

      (flag? new SubSub1(3) : new Sub1(17)).M(false);
   }
 }
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        IMethodDefinition fooRunMethod = compilerResults.FindMethodWithName("Foo::Run");
        Assert.IsNotNull(fooRunMethod);

        LocalFlowMethodSummarizer summarizer = new TypesLocalFlowMethodSummarizer();

        Assert.IsTrue(summarizer.CanSummarizeMethod(fooRunMethod));

        ReachabilitySummary summary = summarizer.SummarizeMethod(fooRunMethod, wholeProgram);
        Assert.IsNotNull(summary);

        Assert.IsTrue(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub1::M")));

        // Note: although this method could be called, we don't actually want to dispatch on it, but rather Sub1::M
        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("SubSub1::M")));

        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Super::M")));

      });
    }

    [TestMethod]
    public void TestReturnFromStaticMethodLocalFlow() {

      string source =
@"internal class Super {
    public Super() {}
    internal virtual int M(bool f) {return 17;}
  }

  internal class Sub1 : Super {
    internal Sub1(int param) {}

    internal override int M(bool f) {
      return 32;
    }
  }

 class Foo {
   static Sub1 GetSub1(int p) { return new Sub1(p); }
   void Run(bool flag) {
      GetSub1(12).M(flag);
   }
 }
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        IMethodDefinition fooRunMethod = compilerResults.FindMethodWithName("Foo::Run");
        Assert.IsNotNull(fooRunMethod);

        LocalFlowMethodSummarizer summarizer = new TypesLocalFlowMethodSummarizer();

        Assert.IsTrue(summarizer.CanSummarizeMethod(fooRunMethod));

        ReachabilitySummary summary = summarizer.SummarizeMethod(fooRunMethod, wholeProgram);
        Assert.IsNotNull(summary);

        Assert.IsTrue(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub1::M")));

        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Super::M")));

      });
    }

    [TestMethod]
    public void TestStraightLineParameterLocalFlow() {

      string source =
@"internal class Super {
    internal virtual int M(bool f) {return 17;}
  }

  internal class Sub1 : Super {
    internal Sub1(int param) {
    }

    internal override int M(bool f) {
      return 32;
    }
  }

 class Foo {
   void Run(Super s1, Super s2) {
      Foo f = this;

      s1 = new Sub1(17);
      s2 = new Super();
      
      s1.M(false);
   }
 }
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        IMethodDefinition fooRunMethod = compilerResults.FindMethodWithName("Foo::Run");
        Assert.IsNotNull(fooRunMethod);

        LocalFlowMethodSummarizer summarizer = new TypesLocalFlowMethodSummarizer();

        Assert.IsTrue(summarizer.CanSummarizeMethod(fooRunMethod));

        ReachabilitySummary summary = summarizer.SummarizeMethod(fooRunMethod, wholeProgram);
        Assert.IsNotNull(summary);

        Assert.IsTrue(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub1::M")));
        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Super::M")));

      });
    }

    [TestMethod]
    public void TestJoinedParameterLocalFlow() {

      string source =
@"internal class Super {
    internal virtual int M(bool f) {return 17;}
  }

  internal class Sub1 : Super {
    internal Sub1(int param) {
    }

    internal override int M(bool f) {
      return 32;
    }
  }

  internal class Sub2 : Super {
    internal Sub2(int param) {
    }

    internal override int M(bool f) {
      return 3622;
    }
  }

 class Foo {
   void Run(Super s, bool flag) {
      if (flag) {
         s = new Sub1(17);
      } else {
         s = new Sub2(32);
      }
      
      s.M(false);
   }
 }
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        IMethodDefinition fooRunMethod = compilerResults.FindMethodWithName("Foo::Run");
        Assert.IsNotNull(fooRunMethod);

        LocalFlowMethodSummarizer summarizer = new TypesLocalFlowMethodSummarizer();

        Assert.IsTrue(summarizer.CanSummarizeMethod(fooRunMethod));

        ReachabilitySummary summary = summarizer.SummarizeMethod(fooRunMethod, wholeProgram);
        Assert.IsNotNull(summary);

        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub1::M")));
        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub2::M")));

        Assert.IsTrue(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Super::M")));

      });
    }

    [TestMethod]
    public void TestStraightLineLocalVariableLocalFlow() {

      string source =
@"internal class Super {
    internal virtual int M(bool f) {return 17;}
  }

  internal class Sub1 : Super {
    internal Sub1(int param) {
    }

    internal override int M(bool f) {
      return 32;
    }
  }

 class Foo {
   void Run() {
      Super s1;
      Super s2;

      s1 = new Sub1(17);
      s2 = new Super();
      
      s1.M(false);
   }
 }
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        IMethodDefinition fooRunMethod = compilerResults.FindMethodWithName("Foo::Run");
        Assert.IsNotNull(fooRunMethod);

        LocalFlowMethodSummarizer summarizer = new TypesLocalFlowMethodSummarizer();

        Assert.IsTrue(summarizer.CanSummarizeMethod(fooRunMethod));

        ReachabilitySummary summary = summarizer.SummarizeMethod(fooRunMethod, wholeProgram);
        Assert.IsNotNull(summary);

        Assert.IsTrue(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub1::M")));
        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Super::M")));

      });
    }

    [TestMethod]
    public void TestJoinedLocalVariableLocalFlow() {

      string source =
@"internal class Super {
    internal virtual int M(bool f) {return 17;}
  }

  internal class Sub1 : Super {
    internal Sub1(int param) {
    }

    internal override int M(bool f) {
      return 32;
    }
  }

  internal class Sub2 : Super {
    internal Sub2(int param) {
    }

    internal override int M(bool f) {
      return 3622;
    }
  }

 class Foo {
   void Run(bool flag) {
    Super s;

      if (flag) {
         s = new Sub1(17);
      } else {
         s = new Sub2(32);
      }
      
      s.M(false);
   }
 }
";

      ConstructWholeProgramForSources(new string[] { source }, (compilerResults, wholeProgram) => {
        IMethodDefinition fooRunMethod = compilerResults.FindMethodWithName("Foo::Run");
        Assert.IsNotNull(fooRunMethod);

        LocalFlowMethodSummarizer summarizer = new TypesLocalFlowMethodSummarizer();

        Assert.IsTrue(summarizer.CanSummarizeMethod(fooRunMethod));

        ReachabilitySummary summary = summarizer.SummarizeMethod(fooRunMethod, wholeProgram);
        Assert.IsNotNull(summary);

        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub1::M")));
        Assert.IsFalse(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Sub2::M")));

        Assert.IsTrue(summary.VirtuallyCalledMethods.Contains(compilerResults.FindMethodWithName("Super::M")));

      });
    }
    #endregion

    #region Testing Infrastructure

    delegate void RTAResultDelegate(TestCompilerResults compilerResults, RapidTypeAnalysis rta);

    delegate void ClassHierarchyResultDelegate(TestCompilerResults compilerResults, ClassHierarchy classHierarchy);

    delegate void WholeProgramResultDelegate(TestCompilerResults compilerResults, WholeProgram wholeProgram);

    delegate void CompiledResultDelegate(TestCompilerResults compilerResults);


    class TestCompilerResults {
      internal IAssembly MainAssembly { get; set; }


      internal PeReader.DefaultHost Host { get; set; }

      internal WholeProgram WholeProgram { get; set; }


      public TestCompilerResults(IAssembly mainAssembly, IAssembly[] libraryAssemblies, PeReader.DefaultHost host) {
        this.MainAssembly = mainAssembly;

        this.Host = host;

        this.WholeProgram = new WholeProgram(new IAssembly[] { mainAssembly }, host);
      }

      internal ITypeDefinition FindTypeWithName(string typeName, int genericParameterCount) {
        foreach (IAssembly assembly in WholeProgram.AllAssemblies()) {
          INamedTypeDefinition foundType = UnitHelper.FindType(this.Host.NameTable, assembly, typeName, genericParameterCount);

          if (!(foundType is Dummy)) {
            return foundType;
          }
        }

        return null;


        /* var desiredTypeIName = this.Host.NameTable.GetNameFor(typeName);

         foreach (ITypeDefinition typeDefinition in AllTypeDefinitions()) {

           if (typeDefinition is INamedTypeDefinition) {
             INamedTypeDefinition namedTypeDefinition = (INamedTypeDefinition)typeDefinition;

             if (namedTypeDefinition.Name.UniqueKey == desiredTypeIName.UniqueKey) {
               if (namedTypeDefinition is IGenericTypeInstance) {
                 return ((IGenericTypeInstance)namedTypeDefinition).GenericType.ResolvedType;
               }
               return namedTypeDefinition;
             }
           }
         }

         return null;*/
      }



      internal ITypeDefinition FindTypeWithName(string typeName) {

        Match match = Regex.Match(typeName, @"(.+)`([0-9]+)");

        if (match.Success) {

          string bareTypeName = match.Groups[1].Value;

          string genericParameterCountAsString = match.Groups[2].Value;
          int genericParameterCount = int.Parse(genericParameterCountAsString);

          Console.WriteLine("Successful match! Type=[{0}] Count=[{1}]", bareTypeName, genericParameterCount);

          ITypeDefinition genericTypeDefinition = FindTypeWithName(bareTypeName, genericParameterCount);

          return genericTypeDefinition;
        }
        else {
          return FindTypeWithName(typeName, 0);
        }
      }


      internal IMethodDefinition FindMethodWithName(string methodSpecifier) {
        // ClassName::MethodName

        Match match = Regex.Match(methodSpecifier, @"([^:]+)::(.+)");

        Assert.IsTrue(match.Success, "Couldn't parse method specifier {0}", methodSpecifier);

        string typeName = match.Groups[1].Value;
        string methodName = match.Groups[2].Value;

        Console.WriteLine("Successful match! Type=[{0}] Method=[{1}]", typeName, methodName);

        return FindMethodWithName(typeName, methodName);

      }

      // For now we expect tests to not have multiple methods in a given type with
      // the same name.
      internal IMethodDefinition FindMethodWithName(string definingTypeName, string methodName) {
        ITypeDefinition definingType = FindTypeWithName(definingTypeName);

        Assert.AreNotEqual(null, definingType, "Couldn't find defining type {0}", definingTypeName);

        var desiredMethodIName = this.Host.NameTable.GetNameFor(methodName);
        var foundMethod = definingType.Methods.FirstOrDefault(method => method.Name.UniqueKey == desiredMethodIName.UniqueKey);

        return foundMethod;
      }
    }

    struct TemporaryFile : IDisposable {
      internal string Path { get; set; }

      public void Dispose() {
        File.Delete(this.Path);
      }
    }


    static void CompileSourceAndRun(string source, string extension, CompiledResultDelegate resultDelegate) {
      CompileSourcesAndRun(new string[] { source }, extension, resultDelegate);
    }

    static void CompileSourcesAndRun(string[] sources, string extension, CompiledResultDelegate resultDelegate) {
      TemporaryFile[] compiledAssemblyPaths = CompileSources(sources, extension);

      using (var host = new PeReader.DefaultHost()) {

        IAssembly[] loadedAssemblies = compiledAssemblyPaths.Select(pathToAssembly => {
          var assembly = host.LoadUnitFrom(pathToAssembly.Path) as IAssembly;
          Assert.IsNotNull(assembly);
          return assembly;
        }).ToArray();

        IAssembly mainAssembly = loadedAssemblies.Last();
        IAssembly[] referencedAssemblies = loadedAssemblies.Take(loadedAssemblies.Length - 1).ToArray();

        TestCompilerResults results = new TestCompilerResults(mainAssembly, referencedAssemblies, host);

        resultDelegate(results);
      }

      foreach (var tempFile in compiledAssemblyPaths) {
        tempFile.Dispose();
      }
    }


    static void ConstructClassHierarchyForSource(string source, ClassHierarchyResultDelegate resultDelegate) {
      ConstructClassHierarchyForSources(new string[] { source }, resultDelegate);
    }

    static void ConstructClassHierarchyForSources(string[] sources, ClassHierarchyResultDelegate resultDelegate) {
      // Each source is assumed to refer to all the sources before it

      CompileSourcesAndRun(sources, ".dll", compilerResult => {
        WholeProgram wholeProgram = new WholeProgram(new IAssembly[] { compilerResult.MainAssembly }, compilerResult.Host);

        ClassHierarchy classHierarchy = new ClassHierarchy(wholeProgram.AllDefinedTypes(), compilerResult.Host);

        resultDelegate(compilerResult, classHierarchy);
      });
    }

    static void ConstructWholeProgramForSources(string[] sources, WholeProgramResultDelegate resultDelegate) {
      // Each source is assumed to refer to all the sources before it

      CompileSourcesAndRun(sources, ".dll", compilerResult => {
        WholeProgram wholeProgram = new WholeProgram(new IAssembly[] { compilerResult.MainAssembly }, compilerResult.Host);

        resultDelegate(compilerResult, wholeProgram);
      });
    }

    static void RunRTAOnSourceResource(string sourceResourceName, RTAResultDelegate runDelegate) {
      // This step unfortunately loses the *name* of the resource, which would
      // perhaps be helpful for error messages.
      string resourceAsString = ExtractResourceToString(sourceResourceName);

      RunRTAOnSource(resourceAsString, runDelegate);
    }

    static void RunRTAOnSource(string source, RTAResultDelegate runDelegate) {
      RunRTAOnSources(new string[] { source }, runDelegate);
    }

    static void RunRTAOnSources(string[] sources, RTAResultDelegate runDelegate) {

      //The last source in the array must have the Main entrypoint

      CompileSourcesAndRun(sources, ".exe", compilerResult => {
        var mainAssembly = compilerResult.MainAssembly;

        WholeProgram wholeProgram = new WholeProgram(new IAssembly[] { compilerResult.MainAssembly }, compilerResult.Host);

        var rta = new RapidTypeAnalysis(wholeProgram, TargetProfile.Desktop);

        rta.Run(new IMethodDefinition[1] { mainAssembly.EntryPoint.ResolvedMethod });

        runDelegate(compilerResult, rta);
      });
    }

    static string ExtractResourceToString(string resource) {
      System.Reflection.Assembly a = System.Reflection.Assembly.GetExecutingAssembly();

      string fullyQualifiedResource = "TestILGarbageCollector.SourceInputs." + resource;

      using (Stream srcStream = a.GetManifestResourceStream(fullyQualifiedResource)) {
        Assert.IsNotNull(srcStream, "Couldn't load source resource -- did you set its Build Action to 'Embedded Source'?");

        using (StreamReader streamReader = new StreamReader(srcStream)) {
          string resourceText = streamReader.ReadToEnd(); // Assumes UTF-8 encoding

          return resourceText;
        }
      }
    }



    /// <summary>
    /// Write the passed in source to a temporary file, then compile that source to an assembly and return
    /// a path to that assembly.
    /// </summary>
    /// <param name="source">The source to compile.</param>
    /// <param name="extension">The extension of the produced assembly; either ".exe" or ".dll".</param>
    /// <returns>A path the compiled assembly.</returns>
    static TemporaryFile CompileSource(string source, string extension) {
      return CompileSources(new string[] { source }, extension)[0];
    }

    static TemporaryFile[] CompileSources(string[] sources, string extension) {

      string[] sourceFiles = sources.Select(source => {
        string sourceFile = Path.GetRandomFileName() + ".cs"; // Hard-code C# for now.

        using (StreamWriter outfile = new StreamWriter(sourceFile)) {
          outfile.Write(source);
        }

        return sourceFile;
      }).ToArray();

      return CompileFiles(sourceFiles, extension);
    }


    static TemporaryFile[] CompileFiles(string[] sourceFiles, string extension) {
      // Each subsequent file may refer to the previous one

      var compiledPathNames = sourceFiles.Select((sourceFile, i) => {
        if (i == sourceFiles.Length - 1) {
          // The main (final entry) source is compiled according to the passed in extension
          return Path.ChangeExtension(sourceFile, extension);
        }
        else {
          // All dependencies are compiled as dlls
          return Path.ChangeExtension(sourceFile, ".dll");
        }

      }).ToArray();

      TemporaryFile[] compiledFiles = sourceFiles.Select((sourceFile, i) => {
        string[] alreadyCompiledDependencies = compiledPathNames.Take(i).ToArray(); // get the sequence [0, i - 1] inclusive

        var assemblyPath = CompileFileWithDependencies(sourceFile, compiledPathNames[i], alreadyCompiledDependencies);

        var temp = new TemporaryFile();
        temp.Path = assemblyPath;

        return temp;
      }).ToArray();

      return compiledFiles;
    }

    static TemporaryFile CompileFile(string sourceFile, string extension) {
      return CompileFiles(new string[] { sourceFile }, extension)[0];
    }

    static string CompileFileWithDependencies(string sourceFile, string destinationAssemblyName, string[] dependencies) {
      string[] assembliesToReference = dependencies;


      /* new string[] {"System.dll", "System.Core.dll", @"C:\Users\t-devinc\Documents\Visual Studio 2010\Projects\ConsoleApplication1\ConsoleApplication1\bin\Debug\ConsoleApplication1.exe"
}; //For collection classes
*/

      CompilerParameters parameters = new CompilerParameters(assembliesToReference);
      parameters.GenerateExecutable = Path.GetExtension(destinationAssemblyName) == ".exe";
      parameters.IncludeDebugInformation = true;
      parameters.OutputAssembly = destinationAssemblyName;
      parameters.CompilerOptions += " -unsafe";

      CompilerResults results;
      using (CodeDomProvider icc = new CSharpCodeProvider()) {
        results = icc.CompileAssemblyFromFile(parameters, sourceFile);
      }

      string accumulatedErrors = "Compile Errors: ";
      foreach (var s in results.Errors) {
        accumulatedErrors += s + "\n";
        Debug.WriteLine(s);
      }
      Assert.AreEqual(0, results.Errors.Count, accumulatedErrors);
      Assert.IsTrue(File.Exists(destinationAssemblyName), string.Format("Failed to compile {0} from {1}", destinationAssemblyName, sourceFile));

      return destinationAssemblyName;
    }

    #endregion

  }
}

