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
using System;
using System.Threading;

public class Threads {

  public static int first = 0;
  public static int second = 0;
  public static int numIterations = 10;
  public static int numThreads = 10;

  public static int Main() {
    if (!MultipleThreadsLockingSameObject()) return 1;
    ResetCounters();
    if (!SingleThreadGetHashcodeAndLock()) return 2;
    ResetCounters();
    if (!SingleThreadLockAndGetHashcode()) return 3;
    ResetCounters();
    if (!MultipleThreadsLockingAndGettingHashcode()) return 4;
    return 0;
  }

  private static void ResetCounters() {
    first = 0;
    second = 0;
  }

  public static bool MultipleThreadsLockingSameObject() {
    Thread[] threads = new Thread[numThreads];
    MyClass myClass = new MyClass();
    for (int i = 0; i < numThreads; i++) {
      Thread t = new Thread(new ThreadStart(myClass.RunInSeperateThread));
      threads[i] = t;
    }
    for (int i = 0; i < numThreads; i++) {
      threads[i].Start();
    }
    for (int i = 0; i < numThreads; i++) {
      threads[i].Join();
    }
    if (first == (numThreads * numIterations) && second == (numThreads * numIterations))
      return true;
    return false;
  }

  public static bool SingleThreadGetHashcodeAndLock() {
    MyClass myClass = new MyClass();
    int hashCode = myClass.thisLock.GetHashCode();
    Thread t = new Thread(new ThreadStart(myClass.RunInSeperateThread));
    t.Start();
    t.Join();
    if (first == numIterations && second == numIterations && hashCode == myClass.thisLock.GetHashCode())
      return true;
    return false;
  }

  public static bool SingleThreadLockAndGetHashcode() {
    MyClass myClass = new MyClass();
    Thread t = new Thread(new ThreadStart(myClass.RunInSeperateThread));
    t.Start();
    t.Join();
    myClass.thisLock.GetHashCode();
    if (first == numIterations && second == numIterations)
      return true;
    return false;
  }

  public static bool MultipleThreadsLockingAndGettingHashcode() {
    Thread[] threads = new Thread[numThreads];
    MyClass myClass = new MyClass();
    for (int i = 0; i < numThreads; i++) {
      Thread t;
      if (i % 2 == 0) {
        t = new Thread(new ThreadStart(myClass.RunInSeperateThread));
      } else {
        t = new Thread(new ThreadStart(myClass.RunGetHashcode));
      }
      threads[i] = t;
    }
    for (int i = 0; i < numThreads; i++) {
      threads[i].Start();
    }
    for (int i = 0; i < numThreads; i++) {
      threads[i].Join();
    }
    if (first == (numThreads / 2 * numIterations) && second == (numThreads / 2 * numIterations))
      return true;
    return false;
  }
}

public class MyClass {
  public Object thisLock = new Object();
  private int numIterations = 10;

  public void RunInSeperateThread() {
    for (int i = 0; i < Threads.numIterations; i++)
      DoSomething();
  }

  public void RunGetHashcode() {
    for (int i = 0; i < Threads.numIterations; i++)
      thisLock.GetHashCode();
  }

  private void DoSomething() {
    lock (thisLock) {
      var first = Threads.first;
      first++;
      Threads.first = first;
      var second = Threads.second;
      second++;
      Threads.second = second;
    }
  }
}
