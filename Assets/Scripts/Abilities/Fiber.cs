using System.Linq;
using System.Collections;
using System.Collections.Generic;

public struct Fiber {
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

  public static IEnumerator Wait(int f) {
    for (var i = 0; i < f; i++) {
      yield return null;
    }
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
