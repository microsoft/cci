public delegate T Foo<T>();

public class Repro5 {

    public Foo<int> GetFive() {
        return () => 5;
    }

} // class
