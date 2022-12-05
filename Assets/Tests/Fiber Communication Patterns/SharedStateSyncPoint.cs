using System;
using System.Collections;
using UnityEngine;
using static Fiber;

class Dodge : IEnumerator {
  IEnumerator Enumerator;
  public IEventSource Release;
  public IEventSource Action;
  public object Current => Enumerator.Current;
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() => Enumerator.MoveNext();
  public Dodge(IEventSource release, IEventSource action) {
    Release = release;
    Action = action;
    Enumerator = Routine();
  }
  public IEnumerator Routine() {
    bool held = true;
    IEnumerator Dash() {
      while (held) {
        var onRelease = Until(() => !held);
        var onDash = ListenFor(Action);
        var outcome = SelectTask(onRelease, onDash);
        yield return outcome;
        if (outcome.Value == onDash) {
          Debug.Log("Dash begun");
          yield return Wait(Timeval.FromSeconds(5f));
          Debug.Log("Dash complete");
        }
      }
    }
    IEnumerator HandleRelease() {
      yield return ListenFor(Release);
      Debug.Log("Release");
      held = false;
    }
    yield return All(Dash(), HandleRelease());
    Debug.Log("Ability ended");
  }
}