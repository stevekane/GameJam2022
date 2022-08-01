using System.Linq;
using System.Collections;
using System.Collections.Generic;

public struct Fiber {
  public static IEnumerator Wait(int n) {
    for (var i = 0; i < n; i++) {
      yield return null;
    }
  }

  public static IEnumerator Any(IEnumerator a, IEnumerator b) {
    var aFiber = new Fiber(a);
    var bFiber = new Fiber(b);
    while (aFiber.Run() & bFiber.Run()) {
      yield return null;
    }
  }

  public static IEnumerator All(IEnumerator a, IEnumerator b) {
    var aFiber = new Fiber(a);
    var bFiber = new Fiber(b);
    while (aFiber.Run() | bFiber.Run()) {
      yield return null;
    }
  }

  public static IEnumerator Any(IEnumerator a, IEnumerator b, IEnumerator c) => Any(a, Any(b,c));
  public static IEnumerator Any(IEnumerator a, IEnumerator b, IEnumerator c, IEnumerator d) => Any(Any(a, b), Any(b, c));
  public static IEnumerator Any(IEnumerable<IEnumerator> xs) => xs.Aggregate(Any);

  public static IEnumerator All(IEnumerator a, IEnumerator b, IEnumerator c) => All(a, All(b,c));
  public static IEnumerator All(IEnumerator a, IEnumerator b, IEnumerator c, IEnumerator d) => All(All(a, b), All(b, c));
  public static IEnumerator All(IEnumerable<IEnumerator> xs) => xs.Aggregate(All);

  public interface IValue<T> {
    public T Value { get; }
  }

  public class ListenFor : IEnumerator {
    EventSource Source;
    bool Waiting = true;

    public ListenFor(EventSource source) {
      Waiting = true;
      Source = source;
      Source.Action += Callback;
    }
    ~ListenFor() {
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

  public class ListenFor<T> : IEnumerator, IValue<T> {
    EventSource<T> Source;
    bool Waiting = true;

    public ListenFor(EventSource<T> source) {
      Waiting = true;
      Source = source;
      Source.Action += Callback;
    }
    ~ListenFor() {
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

  public class Select: AbilityTask, IValue<int> {
    IEnumerator A;
    IEnumerator B;
    public Select(IEnumerator a, IEnumerator b) {
      A = a;
      B = b;
      Enumerator = Routine();
    }
    ~Select() {
      A = null;
      B = null;
      Enumerator = null;
    }
    public override IEnumerator Routine() {
      var aFiber = new Fiber(A);
      var bFiber = new Fiber(B);
      while (true) {
        var aActive = aFiber.Run();
        var bActive = bFiber.Run();
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

  Stack<IEnumerator> Stack;
  public Fiber(Stack<IEnumerator> stack) {
    Stack = stack;
  }
  public Fiber(IEnumerator enumerator) {
    Stack = new();
    Stack.Push(enumerator);
  }

  public bool Run() {
    while (Stack.TryPeek(out IEnumerator top)) {
      if (!top.MoveNext()) {
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