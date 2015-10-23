namespace MethodModifiers {
  public abstract class Base {
    protected Base() {
    }

    public virtual void VirtualM() {
    }

    public abstract void AbstractM();
  }

  public sealed class Derived : MethodModifiers.Base {
    public Derived() {
    }

    public override void VirtualM() {
    }

    public override sealed void AbstractM() {
    }
  }

  interface IInterface {
    void IMethod();

    void IMethod2();
  }

  public abstract class Derived2 : MethodModifiers.Base, MethodModifiers.IInterface {
    protected Derived2() {
    }

    public void IMethod() {
    }

    void MethodModifiers.IInterface.IMethod2() {
    }

    public virtual void IMethod2() {
    }

    public new void VirtualM() {
    }
  }
}
