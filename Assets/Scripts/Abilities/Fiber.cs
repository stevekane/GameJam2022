using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public interface IValue<T> {
  public T Value { get; }
}

public interface IStoppable {
  public bool IsRunning { get; }
  public void Stop();
}

public interface IStoppableValue<T> : IEnumerator, IStoppable, IValue<T> {}

public class Bundle : IEnumerator, IStoppable {
  List<Fiber> Fibers = new();
  List<Fiber> Added = new();
  List<Fiber> Removed = new();

  public object Current { get => null; }
  public void Reset() => throw new NotSupportedException();
  public bool IsRunning { get => Fibers.Count > 0 || Added.Count > 0; }
  public bool IsRoutineRunning(Fiber f) {
    return Fibers.Contains(f) || Added.Contains(f);
  }
  public void StartRoutine(Fiber fiber) {
    Added.Add(fiber);
  }
  public void StopRoutine(Fiber fiber) {
    Removed.Add(fiber);
    fiber.Stop();
  }
  public void Stop() {
    Removed.AddRange(Fibers);
    Fibers.ForEach(f => f.Stop());
  }
  public bool MoveNext() {
    Fibers.AddRange(Added);
    Added.Clear();
    Removed.ForEach(f => Fibers.Remove(f));
    Fibers.ForEach(f => { if (!f.MoveNext()) StopRoutine(f); });
    return IsRunning;
  }
}

public class Timer : IEnumerator, IValue<Timeval> {
  public Timeval Value { get; } = Timeval.FromMillis(0);
  public bool MoveNext() {
    Value.Frames++;
    return true;
  }
  public object Current { get => Value; }
  public void Reset() => Value.Millis = 0;
}

public class CountdownTimer : IEnumerator, IValue<Timeval> {
  public Timeval Value { get; }
  public CountdownTimer(Timeval duration) { Value = Timeval.FromMillis(duration.Millis); }
  public bool MoveNext() => (--Value.Frames > 0);
  public object Current { get => Value; }
  public void Reset() => Value.Millis = 0;
}

public class Listener : IEnumerator, IStoppable {
  IEventSource Source;

  public Listener(IEventSource source) {
    IsRunning = true;
    Source = source;
    Source.Listen(Callback);
  }
  ~Listener() {
    Source.Unlisten(Callback);
  }
  public void Callback() {
    IsRunning = false;
    Source.Unlisten(Callback);
  }
  public void Stop() {
    IsRunning = false;
    Source.Unlisten(Callback);
  }
  public bool IsRunning { get; set; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() => IsRunning;
  public object Current { get => null; }
}

public class Listener<T> : IEnumerator, IStoppable, IValue<T> {
  IEventSource<T> Source;

  public Listener(IEventSource<T> source) {
    IsRunning = true;
    Source = source;
    Source.Listen(Callback);
  }
  ~Listener() {
    IsRunning = false;
    Source.Unlisten(Callback);
  }
  public void Callback(T t) {
    Value = t;
    IsRunning = false;
    Source.Unlisten(Callback);
  }
  public void Stop() {
    IsRunning = false;
    Source.Unlisten(Callback);
  }
  public bool IsRunning { get; set; }
  public void Reset() => IsRunning = true;
  public bool MoveNext() => IsRunning;
  public object Current { get => null; }
  public T Value { get; internal set; }
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

// TODO: Take a look at this in light of the recent addition of the notion
// of "runners" as well as IStoppable
public class ScopedRunner : IDisposable {
  Bundle Bundle;
  Fiber Fiber;

  // TODO: could just test Fiber.IsRunning?
  public bool IsRunning { get => Bundle.IsRoutineRunning(Fiber); }
  public ScopedRunner(Bundle bundle, IEnumerator routine) {
    Bundle = bundle;
    Bundle.StartRoutine((Fiber = new Fiber(routine)));
  }
  public void Dispose() {
    Fiber.Stop();
    Bundle.StopRoutine(Fiber);
    Fiber = null;
  }
}

public class Any : IEnumerator, IStoppable {
  Fiber A;
  Fiber B;
  public Any(IEnumerator a, IEnumerator b) {
    IsRunning = true;
    A = a as Fiber ?? new Fiber(a);
    B = b as Fiber ?? new Fiber(b);
  }
  public void Stop() {
    if (A.IsRunning) A.Stop();
    if (B.IsRunning) B.Stop();
  }
  public bool MoveNext() {
    if (IsRunning) {
      IsRunning = A.MoveNext() & B.MoveNext();
      if (!IsRunning) {
        if (A.IsRunning) {
          A.Stop();
        }
        if (B.IsRunning) {
          B.Stop();
        }
      }
      return IsRunning;
    } else {
      return false;
    }
  }
  public void Reset() => throw new NotSupportedException();
  public object Current { get => null; }
  public bool IsRunning { get; internal set; }
}

public class All: IEnumerator, IStoppable {
  Fiber A;
  Fiber B;
  public All(IEnumerator a, IEnumerator b) {
    IsRunning = true;
    A = a as Fiber ?? new Fiber(a);
    B = b as Fiber ?? new Fiber(b);
  }
  public void Stop() {
    if (A.IsRunning) A.Stop();
    if (B.IsRunning) B.Stop();
  }
  public bool MoveNext() {
    if (IsRunning) {
      IsRunning = A.MoveNext() | B.MoveNext();
      if (!IsRunning) {
        if (A.IsRunning) {
          A.Stop();
        }
        if (B.IsRunning) {
          B.Stop();
        }
      }
      return IsRunning;
    } else {
      return false;
    }
  }
  public void Reset() => throw new NotSupportedException();
  public object Current { get => null; }
  public bool IsRunning { get; internal set; }
}

public class Fiber : IEnumerator, IStoppable {
  public static IEnumerator Wait(int n) {
    for (var i = 0; i < n; i++) {
      yield return null;
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
  public static Selector Select(IEnumerator a, IEnumerator b) => new Selector(a, b);
  public static TaskSelector SelectTask(IEnumerator a, IEnumerator b) => new TaskSelector(a, b);
  public static Listener ListenFor(IEventSource source) => new Listener(source);
  public static Listener<T> ListenFor<T>(IEventSource<T> source) => new Listener<T>(source);
  public static ScopedRunner Scoped(Bundle bundle, IEnumerator routine) => new ScopedRunner(bundle, routine);
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

  Stack<IEnumerator> Stack;
  public Fiber(IEnumerator enumerator) {
    Stack = new();
    Stack.Push(enumerator);
  }
  public void Stop() {
    Stack.ForEach(s => {
      if (s is IStoppable ss) {
        ss.Stop();
      }
    });
    Stack.Clear();
  }
  public bool IsRunning { get => Stack.Count > 0; }
  public object Current { get => Stack.Peek(); }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    while (Stack.TryPeek(out IEnumerator top)) {
      if (!top.MoveNext()) {
        if (top is IStoppable ts && ts.IsRunning) {
          ts.Stop();
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
    return false;
  }
}