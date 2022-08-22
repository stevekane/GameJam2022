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

public class Bundle {
  List<Fiber> Fibers = new();
  List<Fiber> Added = new();
  List<Fiber> Removed = new();

  public bool IsRunning { get => Fibers.Count > 0 || Added.Count > 0; }
  public bool IsRoutineRunning(Fiber f) => Fibers.Contains(f) || Added.Contains(f);
  public void StartRoutine(Fiber fiber) => Added.Add(fiber);
  public void StopRoutine(Fiber fiber) => Removed.Add(fiber);
  public void StopAll() => Removed.AddRange(Fibers);
  public void Run() {
    Fibers.AddRange(Added);
    Added.Clear();
    Removed.ForEach((f) => Fibers.Remove(f));
    Fibers.ForEach((f) => { if (!f.MoveNext()) StopRoutine(f); });
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

  public static IEnumerator NTimes(int n, Func<IEnumerator> f) {
    for (var i = 0; i < n; i++) {
      yield return f();
    }
  }

  public static Any Any(IEnumerator a, IEnumerator b) => new Any(a, b);
  public static Any Any(IEnumerator a, IEnumerator b, params IEnumerator[] xs) => xs.Aggregate(new Any(a, b), Any);
  public static All All(IEnumerator a, IEnumerator b) => new All(a, b);
  public static All All(IEnumerator a, IEnumerator b, params IEnumerator[] xs) => xs.Aggregate(new All(a, b), All);
  public static Selector Select(IEnumerator a, IEnumerator b) => new Selector(a, b);
  public static Listener ListenFor(IEventSource source) => new Listener(source);
  public static Listener<T> ListenFor<T>(IEventSource<T> source) => new Listener<T>(source);
  public static ScopedRunner Scoped(Bundle bundle, IEnumerator routine) => new ScopedRunner(bundle, routine);

  Stack<IEnumerator> Stack;
  public Fiber(IEnumerator enumerator) {
    Stack = new();
    Stack.Push(enumerator);
  }
  public void Stop() {
    Stack.ForEach(s => {
      if (s is IStoppable) {
        (s as IStoppable).Stop();
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
        // TODO: Possibly check if Stoppable and if Stopped and Stop?
        Stack.Pop();
      } else {
        if (top.Current is IEnumerator) {
          Stack.Push(top.Current as IEnumerator);
        } else {
          return true;
        }
      }
    }
    return false;
  }
}