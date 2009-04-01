using System.Collections.Generic;

public delegate T Foo<T>();

public class Repro6 {

    public IEnumerable<int> Count() {
        int i = 0;
        yield return i++;
    }

} // class
