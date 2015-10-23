namespace Properties {
  interface IProp {
    int IFaceProp {
      get;
      set;
    }

    string this[int index] {
      get;
    }
  }

  abstract class PropClass : Properties.IProp {
    protected PropClass() {
    }

    public abstract int getOnly {
      get;
    }

    public static int setOnly {
      set {
      }
    }

    internal abstract int getAndSet {
      get;
      set;
    }

    public abstract int restrictSet {
      get;
      protected set;
    }

    protected extern int restrictGet {
      private get;
      set;
    }

    extern int Properties.IProp.IFaceProp {
      get;
      set;
    }

    private extern int this[int index] {
      get;
    }

    private extern int this[int index1, string index2] {
      set;
    }

    extern string Properties.IProp.this[int index] {
      get;
    }
  }
}
