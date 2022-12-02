using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IValue<T> {
  public T Value { get; }
}

public interface IStoppable {
  public void Stop();
  public void Complete() => Stop();
  public bool IsRunning { get; }
}

public interface IStoppableValue<T> : IEnumerator, IStoppable, IValue<T> {}

public class Stoppable : IStoppable {
  protected enum States { Running, Cancelled, Completed }
  protected virtual States State { get; set; } = States.Running;
  public void Stop() {
    State = States.Cancelled;
    OnStop();
  }
  public void Complete() {
    State = States.Completed;
    OnStop();
  }

  public virtual void OnStop() { }
  public bool IsRunning { get => State == States.Running; }
  public bool IsCompleted { get => State == States.Completed; }
  public bool IsCancelled { get => State == States.Cancelled; }
}

[Serializable]
public class Bundle : Stoppable, IEnumerator {
  public List<Fiber> Fibers = new();
  public List<Fiber> Added = new();
  public List<Fiber> Removed = new();

  public object Current { get => null; }
  public void Reset() => throw new NotSupportedException();
  // TODO: support Cancelled state?
  protected override States State { get => Fibers.Count > 0 || Added.Count > 0 ? States.Running : States.Completed; }

  public Fiber StartRoutine(Func<IEnumerator> continuation) => StartRoutine(new Fiber(continuation()));
  public Fiber StartRoutine(IEnumerator routine) => StartRoutine(new Fiber(routine));
  public Fiber StartRoutine(Fiber fiber) {
    Added.Add(fiber);
    return fiber;
  }
  public void StopRoutine(Fiber fiber) {
    Removed.Add(fiber);
    fiber.Stop();
  }
  public override void OnStop() {
    Removed.AddRange(Fibers);
    Fibers.ForEach(f => f.Stop());
  }
  public bool MoveNext() {
    Fibers.AddRange(Added);
    Added.Clear();
    Removed.ForEach(f => Fibers.Remove(f));
    Removed.Clear();
    Fibers.ForEach(f => { if (!f.MoveNext()) StopRoutine(f); });
    return IsRunning;
  }
}

public class Timer : IEnumerator, IValue<Timeval> {
  public Timeval Value { get; } = Timeval.FromMillis(0);
  public bool MoveNext() {
    Value.Ticks++;
    return true;
  }
  public object Current { get => Value; }
  public void Reset() => Value.Millis = 0;
}

public class CountdownTimer : IEnumerator, IValue<Timeval> {
  public Timeval Value { get; }
  public CountdownTimer(Timeval duration) { Value = Timeval.FromMillis(duration.Millis); }
  public bool MoveNext() => (--Value.Ticks > 0);
  public object Current { get => Value; }
  public void Reset() => Value.Millis = 0;
}

public class Listener : Stoppable, IEnumerator {
  IEventSource Source;

  public Listener(IEventSource source) {
    Source = source;
    Source.Listen(Callback);
  }
  ~Listener() => Stop();
  public void Callback() => Complete();
  public override void OnStop() => Source.Unlisten(Callback);
  public bool MoveNext() => IsRunning;
  public void Reset() => throw new NotImplementedException();
  public object Current { get => null; }
}

public class Listener<T> : Stoppable, IEnumerator, IValue<T> {
  IEventSource<T> Source;

  public Listener(IEventSource<T> source) {
    Source = source;
    Source.Listen(Callback);
  }
  ~Listener() => Stop();
  public void Callback(T t) {
    Value = t;
    Complete();
  }
  public override void OnStop() => Source.Unlisten(Callback);
  public void Reset() => throw new NotImplementedException();
  public bool MoveNext() => IsRunning;
  public object Current { get => null; }
  public T Value { get; private set; }
}

public class ConcurrentListener<T> : Stoppable, IEnumerator, IValue<int> {
  IEventSource<T> Source;
  T[] Array;

  public ConcurrentListener(IEventSource<T> source, T[] array) {
    Source = source;
    Array = array;
    Source.Listen(Callback);
  }
  ~ConcurrentListener() {
    Stop();
  }
  public override void OnStop() => Source.Unlisten(Callback);
  public void Reset() {
    Source.Listen(Callback);
    Value = 0;
  }
  public bool MoveNext() => Value == 0;
  public object Current => Value;
  public int Value { get; private set; }
  void Callback(T t) => Array[Value++] = t;
}

// TODO: This does not need to be Ability Task. Just implement this as
// a custom IEnumerator + IValue + IStoppable
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

// TODO: This does not need to be Ability Task. Just implement this as
// a custom IEnumerator + IValue + IStoppable
public class TaskSelector : AbilityTask, IValue<IEnumerator> {
  IEnumerator A;
  IEnumerator B;
  public TaskSelector(IEnumerator a, IEnumerator b) {
    A = a;
    B = b;
    Enumerator = Routine();
  }
  ~TaskSelector() {
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
        Value = A;
        yield break;
      } else if (!bActive) {
        Value = B;
        yield break;
      } else {
        yield return null;
      }
    }
  }
  public IEnumerator Value { get; internal set; }
}

public class Capture<T> : Stoppable, IValue<T> {
  Fiber Routine;
  public Capture(IEnumerator routine) {
    Routine = routine as Fiber ?? new Fiber(routine);
  }
  public override void OnStop() => Routine.Stop();
  public bool MoveNext() {
    if (IsRunning) {
      if (!Routine.MoveNext()) {
        Complete();
        // Should Fiber.Current do this unpacking of the enumerator at the top of its stack?
        // Also: Does it make more sense to capture the value before MoveNext or after?
      } else if (Routine.Current is IEnumerator e && e.Current is T result) {
        Value = result;
      }
    }
    return IsRunning && Routine.IsRunning;
  }
  public void Reset() => throw new NotSupportedException();
  public object Current { get => null; }
  public T Value { get; private  set; }
}

public class Any : Stoppable, IEnumerator {
  Fiber A;
  Fiber B;
  public Any(IEnumerator a, IEnumerator b) {
    A = a as Fiber ?? new Fiber(a);
    B = b as Fiber ?? new Fiber(b);
  }
  public override void OnStop() {
    if (A.IsRunning) A.Stop();
    if (B.IsRunning) B.Stop();
  }
  public bool MoveNext() {
    if (IsRunning) {
      if (!(A.MoveNext() & B.MoveNext()))
        Complete();
    }
    return IsRunning;
  }
  public void Reset() => throw new NotSupportedException();
  public object Current { get => null; }
}

public class All : Stoppable, IEnumerator {
  Fiber A;
  Fiber B;
  public All(IEnumerator a, IEnumerator b) {
    A = a as Fiber ?? new Fiber(a);
    B = b as Fiber ?? new Fiber(b);
  }
  public override void OnStop() {
    if (A.IsRunning) A.Stop();
    if (B.IsRunning) B.Stop();
  }
  public bool MoveNext() {
    if (IsRunning) {
      if (!(A.MoveNext() | B.MoveNext()))
        Complete();
    }
    return IsRunning;
  }
  public void Reset() => throw new NotSupportedException();
  public object Current { get => null; }
}

[Serializable]
public class Fiber : Stoppable, IEnumerator {
  public static IEnumerator Noop => null;
  public static Fiber Done => new Fiber(Noop);
  public static IEnumerator Wait(int n) {
    for (var i = 0; i < n; i++) {
      yield return null;
    }
  }
  public static IEnumerator Wait(Timeval t) => Wait(t.Ticks);
  public static IEnumerator WhileRunning(IStoppable stoppable) {
    while (stoppable.IsRunning)
      yield return null;
  }

  public static IEnumerator While(Func<bool> pred) {
    while (pred())
      yield return null;
  }

  public static IEnumerator While(Func<bool> pred, Func<IEnumerator> body) {
    while (pred()) {
      yield return body();
    }
  }

  public static IEnumerator Until(Func<bool> pred) {
    while (!pred())
      yield return null;
  }

  public static IEnumerator NTimes(int n, Func<IEnumerator> f) {
    for (var i = 0; i < n; i++) {
      yield return f();
    }
  }

  public static Fiber From(IEnumerator e) => new Fiber(e);
  public static Any Any(IEnumerator a, IEnumerator b) => new Any(a, b);
  public static Any Any(IEnumerator a, IEnumerator b, params IEnumerator[] xs) => xs.Aggregate(Any(a, b), Any);
  public static All All(IEnumerator a, IEnumerator b) => new All(a, b);
  public static All All(IEnumerator a, IEnumerator b, params IEnumerator[] xs) => xs.Aggregate(All(a, b), All);
  public static Capture<T> Capture<T>(IEnumerator routine) => new Capture<T>(routine);
  public static Capture<T> Capture<T>(out Capture<T> result, IEnumerator routine) => result = new Capture<T>(routine);
  public static Selector Select(IEnumerator a, IEnumerator b) => new Selector(a, b);
  public static TaskSelector SelectTask(IEnumerator a, IEnumerator b) => new TaskSelector(a, b);
  public static Listener ListenFor(IEventSource source) => new Listener(source);
  public static Listener<T> ListenFor<T>(IEventSource<T> source) => new Listener<T>(source);
  public static ConcurrentListener<T> ListenForAll<T>(IEventSource<T> source, T[] array) => new ConcurrentListener<T>(source, array);
  public static IEnumerator Repeat(Func<IEnumerator> continuation) {
    while (true) {
      yield return continuation();
    }
  }
  public static IEnumerator Repeat<A>(Func<A, IEnumerator> continuation, A a) {
    while (true) {
      yield return continuation(a);
    }
  }
  public static IEnumerator Repeat(Action action) {
    while (true) {
      action();
      yield return null;
    }
  }
  public static IEnumerator Repeat<A>(Action<A> action, A a) {
    while (true) {
      action(a);
      yield return null;
    }
  }

  Stack<IEnumerator> Stack;
  public Fiber(IEnumerator enumerator) {
    Stack = new();
    Stack.Push(enumerator);
  }
  public override void OnStop() {
    Stack.ForEach(s => {
      if (s is IStoppable ss) {
        ss.Stop();
      }
    });
    Stack.Clear();
  }
  public object Current { get => Stack.Peek(); }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    while (Stack.TryPeek(out IEnumerator top)) {
      if (!top.MoveNext()) {
        if (top is IStoppable ts && ts.IsRunning) {
          ts.Complete();
        }
        if (Stack.Count > 0) {
          Stack.Pop();
        }
      } else {
        if (top.Current is IEnumerator current) {
          Stack.Push(current);
        } else {
          return true;
        }
      }
    }
    Complete();
    return false;
  }
}