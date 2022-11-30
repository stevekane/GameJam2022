using System;
using System.Collections;
using UnityEngine;

public class Interactable : MonoBehaviour {
  public class Open : IEnumerator, IStoppable {
    IEventSource StopSource;
    ConditionAccum Condition;
    Animator Animator;
    public bool IsRunning { get; internal set; }
    public Open(ConditionAccum condition, IEventSource stopSource, Animator animator) {
      IsRunning = true;
      Animator = animator;
      Animator.Play("Open");
      Condition = condition;
      StopSource = stopSource;
      StopSource.Listen(Stop);
    }
    public void Stop() {
      IsRunning = false;
      Animator.Play("Close");
      StopSource.Unlisten(Stop);
    }
    public object Current => null;
    public void Reset() => throw new NotSupportedException();
    public bool MoveNext() {
      Condition.CanAct.Set(!IsRunning);
      Condition.CanMove.Set(!IsRunning);
      Condition.CanJump.Set(!IsRunning);
      return IsRunning;
    }
  }

  public string InteractMessage;
  public string StopMessage;
  public Bundle Bundle;
  public Animator Openable;

  void OnTriggerEnter2D(Collider2D c) {
    if (c.TryGetComponent(out Interactor i)) {
      i.SetTarget(this);
    }
  }

  void OnTriggerExit2D(Collider2D c) {
    if (c.TryGetComponent(out Interactor i)) {
      i.SetTarget(null);
    }
  }

  public void Interact(ConditionAccum condition, IEventSource stopSource) {
    Bundle.StartRoutine(new Open(condition, stopSource, Openable));
  }

  void OnDestroy() {
    Bundle.Stop();
  }

  void FixedUpdate() {
    Bundle.MoveNext();
  }
}