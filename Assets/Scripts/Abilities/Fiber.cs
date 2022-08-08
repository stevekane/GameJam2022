using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Bundle {
  List<Fiber> Fibers = new();
  List<Fiber> Added = new();
  List<Fiber> Removed = new();

  public bool IsFiberRunning(Fiber f) {
    //Debug.Log($"IsFiberRunning? {f} vs {Fibers}");
    return Fibers.Contains(f);
  }
  public bool IsRunning { get => Fibers.Count > 0; }
  public void Run() {
    Fibers.AddRange(Added);
    Added.Clear();
    Removed.ForEach((f) => Fibers.Remove(f));
    Fibers.ForEach((f) => { if (!f.MoveNext()) StopRoutine(f); });
  }
  public void StartRoutine(Fiber fiber) {
    Added.Add(fiber);
  }
  public void StopRoutine(Fiber fiber) {
    Removed.Add(fiber);
  }
  public void StopAll() {
    Removed.AddRange(Fibers);
  }
}

public class Fiber : IEnumerator {
  public interface IValue<T> {
    public T Value { get; }
  }

  public static IEnumerator Wait(int n) {
    for (var i = 0; i < n; i++) {
      yield return null;
    }
  }

  public static IEnumerator NTimes(int n, Func<IEnumerator> f) {
    for (var i = 0; i < n; i++) {
      yield return f();
    }
  }

  public static IEnumerator Any(IEnumerator a, IEnumerator b) {
    var aFiber = new Fiber(a);
    var bFiber = new Fiber(b);
    while (aFiber.MoveNext() & bFiber.MoveNext()) {
      yield return null;
    }
  }

  public static IEnumerator All(IEnumerator a, IEnumerator b) {
    var aFiber = new Fiber(a);
    var bFiber = new Fiber(b);
    while (aFiber.MoveNext() | bFiber.MoveNext()) {
      yield return null;
    }
  }

  public static IEnumerator Any(IEnumerable<IEnumerator> xs) => xs.Aggregate(Any);
  public static IEnumerator All(IEnumerable<IEnumerator> xs) => xs.Aggregate(All);

  public static Selector Select(IEnumerator a, IEnumerator b) => new Selector(a, b);

  public static Listener ListenFor(EventSource source) => new Listener(source);
  public static Listener<T> ListenFor<T>(EventSource<T> source) => new Listener<T>(source);

  public class Listener : IEnumerator {
    EventSource Source;
    bool Waiting = true;

    public Listener(EventSource source) {
      Waiting = true;
      Source = source;
      Source.Action += Callback;
    }
    ~Listener() {
      Source.Action -= Callback;
    }
    public void Callback() {
      Waiting = false;
      Source.Action -= Callback;
    }
    public void Reset() => Waiting = true;
    public bool MoveNext() => Waiting;
    public object Current { get => null; }
  }

  public class Listener<T> : IEnumerator, IValue<T> {
    EventSource<T> Source;
    bool Waiting = true;

    public Listener(EventSource<T> source) {
      Waiting = true;
      Source = source;
      Source.Action += Callback;
    }
    ~Listener() {
      Source.Action -= Callback;
    }
    public void Callback(T t) {
      Value = t;
      Waiting = false;
      Source.Action -= Callback;
    }
    public void Reset() => Waiting = true;
    public bool MoveNext() => Waiting;
    public object Current { get => null; }
    public T Value { get; internal set; }
  }

  public class Selector : AbilityTask, IValue<int> {
    IEnumerator A;
    IEnumerator B;
    public Selector(IEnumerator a, IEnumerator b) {
      A = a;
      B = b;
      Enumerator = Routine();
    }
    ~Selector() {
      A = null;
      B = null;
      Enumerator = null;
    }
    public override IEnumerator Routine() {
      var aFiber = new Fiber(A);
      var bFiber = new Fiber(B);
      while (true) {
        var aActive = aFiber.MoveNext();
        var bActive = bFiber.MoveNext();
        if (!aActive) {
          Value = 0;
          yield break;
        } else if (!bActive) {
          Value = 1;
          yield break;
        } else {
          yield return null;
        }
      }
    }
    public int Value { get; internal set; }
  }

  IEnumerator Enumerator;
  Stack<IEnumerator> Stack;
  public Fiber(Stack<IEnumerator> stack) {
    Current = null;
    Enumerator = stack.Peek();
    Stack = stack;
  }
  public Fiber(IEnumerator enumerator) {
    Current = null;
    Enumerator = enumerator;
    Stack = new();
    Stack.Push(enumerator);
  }
  public void Reset() {
    Current = null;
    Stack.Clear();
    Stack.Push(Enumerator);
  }
  public object Current { get; internal set; }
  public bool MoveNext() {
    while (Stack.TryPeek(out IEnumerator top)) {
      if (!top.MoveNext()) {
        Stack.Pop();
      } else {
        if (top.Current is IEnumerator) {
          Stack.Push(top.Current as IEnumerator);
        } else {
          Current = top.Current;
          return true;
        }
      }
    }
    return false;
  }
}