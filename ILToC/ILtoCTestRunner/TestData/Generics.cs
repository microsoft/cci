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
public class Generics {
  public static int Main() {
    int ret = 0;
    if ((ret = InstanceOfCheck()) != 0) return 100 + ret;
    if ((ret = InvokeBaseClassField()) != 0) return 200 + ret;
    if ((ret = InvokeBaseClassMethod()) != 0) return 300 + ret;
    if ((ret = InvokeInterfaceMethodOnGenericClass()) != 0) return 400 + ret;
    if ((ret = InvokeInterfaceMethodOnGenericTypeParameter()) != 0) return 500 + ret;
    if ((ret = InvokeOveriddenMethodOnGenericClass()) != 0) return 600 + ret;
    if ((ret = BasicLinkedList()) != 0) return 600 + ret;
    if ((ret = CallLinkedListWithBaseClassConstraints()) != 0) return 700 + ret;
    if ((ret = CallLinkedListWithComplexConstraints()) != 0) return 800 + ret;
    if ((ret = CallLinkedListWithInterfaceConstraints()) != 0) return 900 + ret;
    if ((ret = CallLinkedListWithNewConstraints()) != 0) return 1000 + ret;
    if ((ret = NewGenericInstance()) != 0) return 1100 + ret;
    if ((ret = CallStack()) != 0) return 1200 + ret;
    if ((ret = StaticInstanceOfCheck()) != 0) return 1300 + ret;
    if ((ret = ValueTypes()) != 0) return 1400 + ret;
    if ((ret = VirtualCallOptimization()) != 0) return 1500 + ret;
    if ((ret = GenericMethods()) != 0) return 1600 + ret;
    return ret;
  }

  public static int InstanceOfCheck() {
    int result = 1;
    Test<MyClass2> s = new Test<MyClass2>();
    result = s.InstanceMethod(new MyClass());
    if (result != 0) {
      return 1;
    }
    Test<MyClass> ss = new Test<MyClass>();
    result = ss.InstanceMethod(new MyClass2());
    return result == 1 ? 0 : 2;
  }

  public static int InvokeBaseClassField() {
    int result = 1;
    Test3<MyClass> s = new Test3<MyClass>(new MyClass());
    result = s.GetBaseClassField();
    if (result != 0) {
      return 1;
    }
    Test3<MyClass2> ss = new Test3<MyClass2>(new MyClass2());
    result = ss.GetBaseClassField();
    if (result != 0) {
      return 2;
    }
    return 0;
  }

  public static int InvokeBaseClassMethod() {
    int result = 1;
    Test3<MyClass> s = new Test3<MyClass>(new MyClass());
    result = s.CallBaseClassMethod(new MyClass());
    if (result != 0) {
      return 1;
    }
    result = s.CallBaseClassMethod(new MyClass2());
    return result;
  }

  public static int InvokeInterfaceMethodOnGenericClass() {
    int ret;
    IPoint1 p = new MyPoint<MyClass>();
    ret = p.getX1();
    if (ret != 5) {
      return 1;
    }
    ret = p.getY1();
    if (ret != 2) {
      return 2;
    }
    ret = p.getXPlusY1();
    if (ret != 7) {
      return 3;
    }
    ret = p.getXMinusY1();
    if (ret != 3) {
      return 4;
    }
    ret = p.getXMultY1();
    if (ret != 10) {
      return 5;
    }

    IPoint9 pp = (IPoint9)p;

    ret = pp.getX9();
    if (ret != 2) {
      return 6;
    }
    ret = pp.getY9();
    if (ret != 5) {
      return 7;
    }
    ret = pp.getXPlusY9();
    if (ret != 7) {
      return 8;
    }
    ret = pp.getXMinusY9();
    if (ret != -3) {
      return 9;
    }
    ret = pp.getXMultY9();
    if (ret != 10) {
      return 10;
    }
    return 0;
  }

  public static int InvokeInterfaceMethodOnGenericTypeParameter() {
    TestClass<MyKeyWithFilter> tc = new TestClass<MyKeyWithFilter>();
    return tc.CallFilter(new MyKeyWithFilter()) == true ? 0 : 1;
  }

  static int InvokeOveriddenMethodOnGenericClass() {
    int ret = 0;
    D<MyClass> d = new D<MyClass>();
    A<MyClass> a = d;
    B<MyClass> b = d;
    C<MyClass> c = d;
    ret = a.F();
    if (ret != 2)
      return 1;
    ret = b.F();
    if (ret != 2)
      return 2;
    ret = c.F();
    if (ret != 4)
      return 3;
    ret = d.F();
    if (ret != 4)
      return 4;
    return 0;
  }

  public static int BasicLinkedList() {
    LinkedList<MyKey, MyClass> ll = new LinkedList<MyKey, MyClass>();
    MyClass c1 = new MyClass(5);
    MyKey k1 = new MyKey(5);
    ll.AddHead(k1, c1);
    MyClass c2 = new MyClass(2);
    MyKey k2 = new MyKey(5);
    ll.AddHead(k2, c2);
    return 0;
  }

  public static int CallLinkedListWithBaseClassConstraints() {
    LinkedListWithBaseClassConstraints<MyKey, MyClass> ll = new LinkedListWithBaseClassConstraints<MyKey, MyClass>();
    MyClass c1 = new MyClass(5);
    MyKey k1 = new MyKey(5);
    ll.AddHead(k1, c1);
    MyClass c2 = new MyClass(2);
    MyKey k2 = new MyKey(5);
    ll.AddHead(k2, c2);
    return 0;
  }

  public static int CallLinkedListWithComplexConstraints() {
    LinkedListWithComplexConstraints<MyKeyWithComparable, MyClass> ll = new LinkedListWithComplexConstraints<MyKeyWithComparable, MyClass>();
    MyClass c1 = new MyClass(5);
    MyKeyWithComparable k1 = new MyKeyWithComparable(5);
    ll.AddHead(k1, c1);
    MyClass c2 = new MyClass(2);
    MyKeyWithComparable k2 = new MyKeyWithComparable(5);
    ll.AddHead(k2, c2);
    return 0;
  }

  public static int CallLinkedListWithInterfaceConstraints() {
    LinkedListWithInterfaceConstraints<MyKeyWithFilter2, MyClass> ll = new LinkedListWithInterfaceConstraints<MyKeyWithFilter2, MyClass>();
    MyClass c1 = new MyClass(5);
    MyKeyWithFilter2 k1 = new MyKeyWithFilter2(5);
    bool result = ll.AddHead(k1, c1);
    if (!result)
      return 1;
    MyClass c2 = new MyClass(2);
    MyKeyWithFilter2 k2 = new MyKeyWithFilter2(-5);
    result = ll.AddHead(k2, c2);
    if (result)
      return 2;
    return 0;
  }

  public static int CallLinkedListWithNewConstraints() {
    LinkedListWithNewConstraints<MyKey, MyClass> ll = new LinkedListWithNewConstraints<MyKey, MyClass>();
    MyClass c1 = new MyClass(5);
    MyKey k1 = new MyKey(5);
    ll.AddHead(k1, c1);
    MyClass c2 = new MyClass(2);
    MyKey k2 = new MyKey(5);
    ll.AddHead(k2, c2);
    return 0;
  }

  public static int NewGenericInstance() {
    TestWithNew<MyClass2> s = new TestWithNew<MyClass2>();
    return 0;
  }

  public static int CallStack() {
    Stack<MyClass> s = new Stack<MyClass>();
    MyClass c1 = new MyClass(5);
    s.Push(c1);
    MyClass c2 = new MyClass(2);
    s.Push(c2);
    MyClass c3 = s.Pop();
    if (c3.value != c2.value) {
      return 1;
    }
    MyClass c4 = s.Pop();
    if (c4.value != c1.value) {
      return 2;
    }
    return 0;
  }

  public static int StaticInstanceOfCheck() {
    int result = 1;
    result = Test<MyClass2>.StaticMethod(new MyClass());
    if (result != 0) {
      return 1;
    }
    result = Test<MyClass>.StaticMethod(new MyClass2());
    if (result != 1) {
      return 2;
    }
    result = Test<MyClass2>.StaticMethod2(new MyClass());
    if (result != 0) {
      return 3;
    }
    result = Test<MyClass2>.StaticMethod3(new MyClass());
    if (result != 0) {
      return 4;
    }
    result = Test<MyClass2>.StaticMethod4<MyClass>(new MyClass());
    if (result != 1) {
      return 5;
    }
    return 0;
  }

  public static int ValueTypes() {
    Stack<int> s = new Stack<int>();
    s.Push(5);
    s.Push(10);
    int i = s.Pop();
    if (i != 10) {
      return 1;
    }
    i = s.Pop();
    if (i != 5) {
      return 1;
    }
    return 0;
  }

  public static int VirtualCallOptimization() {
    TestClass<MyKeyWithFilter, int> tc = new TestClass<MyKeyWithFilter, int>();
    return tc.CallFilter(new MyKeyWithFilter()) == true ? 0 : 1;
  }

  public static int GenericMethods() {
    int result = 1;
    Test t = new Test();
    result = t.GenericMethod<MyClass>(new MyClass());
    if (result != 0) {
      return 1;
    }
    result = t.GenericMethod<MyClass2>(new MyClass());
    if (result != 1) {
      return 2;
    }
    Test<MyClass> tt = new Test<MyClass>();
    result = tt.GenericMethod<MyClass>(new MyClass());
    if (result != 0) {
      return 3;
    }
    result = tt.GenericMethod<MyClass2>(new MyClass());
    if (result != 1) {
      return 4;
    }

    result = t.GenericMethodCreateInstance<MyClass>();
    if (result != 5) {
      return 5;
    }
    result = tt.GenericMethodCreateInstance<MyClass2>();
    if (result != 5) {
      return 6;
    }

    result = tt.callGenericMethod();
    if (result != 0) {
      return 7;
    }
    result = tt.callGenericMethod<MyClass>();
    if (result != 0) {
      return 8;
    }
    result = Test<MyClass>.StaticGenericMethod<MyClass2>(new MyClass2());
    if (result != 0) {
      return 9;
    }
    result = Test<MyClass>.StaticGenericMethod<MyClass2>(new MyClass());
    if (result != 1) {
      return 10;
    }
    result = Test<MyClass>.CallStaticGenericMethod<MyClass2>(new MyClass2());
    if (result != 0) {
      return 11;
    }
    result = Test<MyClass>.CallStaticGenericMethod<MyClass2>(new MyClass());
    if (result != 1) {
      return 12;
    }
    result = tt.CreateOpenInstance<MyClass2>();
    if (result != 0) {
      return 13;
    }
    result = tt.CreateOpenInstance<MyClass>();
    if (result != 0) {
      return 14;
    }
    return 0;
  }
}

public class MyClass {
  public int num = 0;
  public int value;

  public MyClass(int i) {
    this.value = i;
  }

  public MyClass() {
    this.value = 0;
  }

  public int BaseClassMethod() {
    return 0;
  }

  public int GetNum() {
    return 5;
  }
}
public class MyClass2 : MyClass {
  public int num = 1;

  public int GetNum() {
    return 6;
  }
}

public class MyKey {
  public int value;
  public MyKey(int i) {
    this.value = i;
  }
  public MyKey() {
    this.value = 0;
  }
}

public class Test3<T> where T : MyClass {
  private T t;
  public Test3(T t) {
    this.t = t;
  }

  public int GetBaseClassField() {
    return t.num;
  }

  public int CallBaseClassMethod(T t) {
    return t.BaseClassMethod();
  }
}

public class Test2<K, T> {
}

public class Test2<K> {
  public static int StaticMethod(MyClass obj) {
    if (obj is K)
      return 1;
    return 0;
  }

  public static int StaticMethod<T>(MyClass obj) {
    if (obj is T)
      return 1;
    return 0;
  }
}

public class Test2 {
  public int GenericMethod<T>(MyClass obj) { //TODO: there should be tests with more than one generic method type parameter.
    if (obj is T)
      return 0;
    return 1;
  }

  public int GenericMethodCreateInstance<T>() //TODO: there should be tests with methods that reference structural types that invole generic method parameters.
    where T : MyClass, new() {
    T t = new T();
    return t.GetNum();
  }

  public static int StaticGenericMethod<T>(System.Object obj) {
    if (obj is T)
      return 0;
    return 1;
  }
}

public class Test<K> {
  public int GenericMethod<T>(MyClass obj) {
    if (obj is T)
      return 0;
    return 1;
  }

  public int GenericMethodCreateInstance<T>()
    where T : MyClass, new() {
    T t = new T();
    return t.GetNum();
  }

  public int callGenericMethod() {
    Test2 t = new Test2();
    return t.GenericMethod<K>(new MyClass2());
  }

  public int callGenericMethod<T>() {
    Test2 t = new Test2();
    return t.GenericMethod<T>(new MyClass2());
  }

  public static int StaticGenericMethod<T>(System.Object obj) {
    if (obj is T)
      return 0;
    if (obj is K)
      return 1;
    return 2;
  }

  public static int CallStaticGenericMethod<T>(System.Object obj) {
    return Test2.StaticGenericMethod<T>(obj);
  }

  public int CreateOpenInstance<T>() {
    Test2<K, T> t = new Test2<K, T>();
    return 0;
  }

  public int InstanceMethod(MyClass obj) {
    if (obj is K)
      return 1;
    return 0;
  }

  public static int StaticMethod(MyClass obj) {
    if (obj is K)
      return 1;
    return 0;
  }

  public static int StaticMethod2(MyClass obj) {
    return Test<K>.StaticMethod(obj);
  }

  public static int StaticMethod3(MyClass obj) {
    return Test2<K>.StaticMethod(obj);
  }

  public static int StaticMethod4<T>(MyClass obj) {
    return Test2<K>.StaticMethod<T>(obj);
  }
}

public class TestWithNew<T> where T : new() {
  private T t;
  public TestWithNew() {
    t = new T();
  }
}

public class Test {
  public int GenericMethod<T>(MyClass obj) {
    if (obj is T)
      return 0;
    return 1;
  }

  public int GenericMethodCreateInstance<T>()
    where T : MyClass, new() {
    T t = new T();
    return t.GetNum();
  }
}

interface IPoint1 {
  int getX1();
  int getY1();
  int getXPlusY1();
  int getXMinusY1();
  int getXMultY1();
}

interface IPoint2 {
  int getX2();
  int getY2();
  int getXPlusY2();
  int getXMinusY2();
  int getXMultY2();
}

interface IPoint3 {
  int getX3();
  int getY3();
  int getXPlusY3();
  int getXMinusY3();
  int getXMultY3();
}

interface IPoint4 {
  int getX4();
  int getY4();
  int getXPlusY4();
  int getXMinusY4();
  int getXMultY4();
}

interface IPoint5 {
  int getX5();
  int getY5();
  int getXPlusY5();
  int getXMinusY5();
  int getXMultY5();
}

interface IPoint6 {
  int getX6();
  int getY6();
  int getXPlusY6();
  int getXMinusY6();
  int getXMultY6();
}

interface IPoint7 {
  int getX7();
  int getY7();
  int getXPlusY7();
  int getXMinusY7();
  int getXMultY7();
}

interface IPoint8 {
  int getX8();
  int getY8();
  int getXPlusY8();
  int getXMinusY8();
  int getXMultY8();
}

interface IPoint9 {
  int getX9();
  int getY9();
  int getXPlusY9();
  int getXMinusY9();
  int getXMultY9();
}

interface IPoint10 {
  int getX10();
  int getY10();
  int getXPlusY10();
  int getXMinusY10();
  int getXMultY10();
}

class MyPoint<k> : IPoint1, IPoint9 {
  private int x1 = 5;
  private int y1 = 2;
  public virtual int getX1() {
    return x1;
  }

  public virtual int getY1() {
    return y1;
  }

  public virtual int getXPlusY1() {
    return x1 + y1;
  }

  public virtual int getXMinusY1() {
    return x1 - y1;
  }

  public virtual int getXMultY1() {
    return x1 * y1;
  }

  private int x9 = 2;
  private int y9 = 5;

  public virtual int getX9() {
    return x9;
  }

  public virtual int getY9() {
    return y9;
  }

  public virtual int getXPlusY9() {
    return x9 + y9;
  }

  public virtual int getXMinusY9() {
    return x9 - y9;
  }

  public virtual int getXMultY9() {
    return x9 * y9;
  }
}

public interface FilterInterface {
  bool Filter();
}

public class MyKeyWithFilter : FilterInterface {
  public bool Filter() {
    return true;
  }
}

public class MyKeyWithFilter2 : FilterInterface {
  public int value;
  public MyKeyWithFilter2(int i) {
    this.value = i;
  }
  public bool Filter() {
    if (this.value < 0)
      return false;
    return true;
  }
}

public class TestClass<K, T> where K : FilterInterface {
  public bool CallFilter(K k) {
    return k.Filter();
  }
}

public class TestClass<K> where K : FilterInterface {
  public bool CallFilter(K k) {
    return k.Filter();
  }
}

class A<T> {
  public virtual int F() { return 1; }
}
class B<K> : A<K> {
  public override int F() { return 2; }
}
class C<K> : B<K> {
  new public virtual int F() { return 3; }
}
class D<K> : C<K> {
  public override int F() { return 4; }
}

public class Node<K, T> {
  public K Key;
  public T Item;
  public Node<K, T> NextNode;
  public Node() {
    Key = default(K);
    Item = default(T);
    NextNode = null;
  }
  public Node(K key, T item, Node<K, T> nextNode) {
    Key = key;
    Item = item;
    NextNode = nextNode;
  }
}

public class LinkedList<K, T> {
  Node<K, T> head;
  public LinkedList() {
    head = new Node<K, T>();
  }
  public void AddHead(K key, T item) {
    Node<K, T> newNode = new Node<K, T>(key, item, head.NextNode);
    head.NextNode = newNode;
  }
}

public class LinkedListWithBaseClassConstraints<K, T> where T : MyClass {
  Node<K, T> head;
  public LinkedListWithBaseClassConstraints() {
    head = new Node<K, T>();
  }
  public void AddHead(K key, T item) {
    Node<K, T> newNode = new Node<K, T>(key, item, head.NextNode);
    head.NextNode = newNode;
  }
}

public interface MyComparable {
  int CompareTo(MyKeyWithComparable obj);
}

public class MyKeyWithComparable : MyComparable {
  public int value;
  public MyKeyWithComparable(int i) {
    this.value = i;
  }
  public MyKeyWithComparable() {
    this.value = 0;
  }

  public int CompareTo(MyKeyWithComparable obj) {
    if (this.value < obj.value)
      return -1;
    if (this.value > obj.value)
      return 1;
    return 0;
  }
}

public class LinkedListWithComplexConstraints<K, T>
  where K : MyComparable
  where T : MyClass {
  Node<K, T> head;
  public LinkedListWithComplexConstraints() {
    head = new Node<K, T>();
  }
  public void AddHead(K key, T item) {
    Node<K, T> newNode = new Node<K, T>(key, item, head.NextNode);
    head.NextNode = newNode;
  }
}

public class LinkedListWithInterfaceConstraints<K, T> where K : FilterInterface {
  Node<K, T> head;
  public LinkedListWithInterfaceConstraints() {
    head = new Node<K, T>();
  }
  public bool AddHead(K key, T item) {
    if (key.Filter()) {
      Node<K, T> newNode = new Node<K, T>(key, item, head.NextNode);
      head.NextNode = newNode;
      return true;
    }
    return false;
  }
}

public class NodeWithNewConstraints<K, T> where T : new() {
  public K Key;
  public T Item;
  public NodeWithNewConstraints<K, T> NextNode;
  public NodeWithNewConstraints() {
    Key = default(K);
    Item = new T();
    NextNode = null;
  }
  public NodeWithNewConstraints(K key, T item, NodeWithNewConstraints<K, T> nextNode) {
    Key = key;
    Item = item;
    NextNode = nextNode;
  }
}

public class LinkedListWithNewConstraints<K, T> where T : new() {
  NodeWithNewConstraints<K, T> head;
  public LinkedListWithNewConstraints() {
    head = new NodeWithNewConstraints<K, T>();
  }
  public void AddHead(K key, T item) {
    NodeWithNewConstraints<K, T> newNode = new NodeWithNewConstraints<K, T>(key, item, head.NextNode);
    head.NextNode = newNode;
  }
}

public class Stack<T> {
  int size;
  int sp = 0;
  T[] items;
  public Stack()
    : this(100) { }
  public Stack(int size) {
    this.size = size;
    this.items = new T[size];
  }
  public void Push(T item) {
    if (sp >= size)
      throw new System.Exception();
    items[sp] = item;
    sp++;
  }
  public T Pop() {
    sp--;
    if (sp >= 0) {
      return items[sp];
    } else {
      sp = 0;
      throw new System.Exception();
    }
  }
}