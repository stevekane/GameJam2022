using System.Collections;
using UnityEngine;

interface IValue<T> {
  public T Value { get; }
}

class ListenFor : IEnumerator {
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

class ListenFor<T> : IEnumerator, IValue<T> {
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

class Select: AbilityTask, IValue<int> {
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

public class AbilityFibered {
  Fiber Routine;

  public AbilityFibered(GameObject owner) {
    Routine = new Fiber(MakeRoutine(owner));
  }

  public bool Run() {
    return Routine.Run();
  }

  IEnumerator Wait(int n) {
    for (var i = 0; i < n; i++) {
      yield return null;
    }
  }

  IEnumerator MakeRoutine(GameObject owner) {
    var MAX_CHARGE_DURATION = Timeval.FromMillis(3000);
    var chargeWait = Wait(MAX_CHARGE_DURATION.Frames);
    var release = new ListenFor(InputManager.Instance.L1.JustUp);
    var select = new Select(chargeWait, release);
    yield return select;
    if (select.Value == 0) {
      Debug.Log("You waited the max duration");
    } else {
      Debug.Log("You released the charge button");
    }
  }
}