using System;
using System.Collections;
using UnityEngine;

public abstract class SimpleTask : IEnumerator {
  public abstract IEnumerator Routine();
  public IEnumerator Enumerator;
  public IEnumerator GetEnumerator() => Enumerator;
  public object Current { get => Enumerator.Current; }
  public bool MoveNext() => Enumerator.MoveNext();
  public void Dispose() => Enumerator = null;
  public void Reset() => Enumerator = Routine();
}

public class AimAt : SimpleTask {
  public Transform Aimer;
  public Transform Target;
  public float TurnSpeed;
  public AimAt(Transform aimer, Transform target, float turnSpeed) {
    Aimer = aimer;
    Target = target;
    TurnSpeed = turnSpeed;
    Enumerator = Routine();
  }
  public override IEnumerator Routine() {
    while (true) {
      var current = Aimer.rotation;
      var desired = Quaternion.LookRotation(Target.position.XZ()-Aimer.position.XZ(), Vector3.up);
      Aimer.rotation = Quaternion.RotateTowards(current, desired, Time.fixedDeltaTime*TurnSpeed);
      yield return null;
    }
  }
}

public class Wait : SimpleTask {
  public float Seconds;
  public Wait(float seconds) {
    Seconds = seconds;
    Enumerator = Routine();
  }
  public override IEnumerator Routine() {
    yield return new WaitForSeconds(Seconds);
  }
}

public class WaitFrames : SimpleTask {
  public int Frames;
  public WaitFrames(int frames) {
    Frames = frames;
    Enumerator = Routine();
  }
  public override IEnumerator Routine() {
    for (var i = 0; i < Frames; i++) {
      yield return new WaitForFixedUpdate();
    }
  }
}

abstract public class SimpleAbility : MonoBehaviour {
  public bool IsComplete { get; private set; } = true;
  public IEnumerator Wrapper() {
    IsComplete = false;
    BeforeBegin();
    AfterBegin();
    yield return Routine();
    BeforeEnd();
    StopAllCoroutines();
    AfterEnd();
    IsComplete = true;
  }
  public void Begin() {
    StopAllCoroutines();
    StartCoroutine(Wrapper());
  }
  public void End() {
    BeforeEnd();
    StopAllCoroutines();
    AfterEnd();
    IsComplete = true;
  }
  public abstract IEnumerator Routine();
  public virtual void BeforeBegin() {}
  public virtual void AfterBegin() {}
  public virtual void BeforeEnd() {}
  public virtual void AfterEnd() {}
}