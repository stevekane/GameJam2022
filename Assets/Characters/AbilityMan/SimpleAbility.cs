using System;
using System.Collections;
using UnityEngine;

public abstract class SimpleTask {
  public Coroutine Coroutine { get; set; }
  public MonoBehaviour Owner { get; set; }
  public abstract IEnumerator Routine();
  public IEnumerator Start() {
    return Routine();
  }
}

public class AimAt : SimpleTask {
  public Transform Aimer;
  public Transform Target;
  public float TurnSpeed;
  public override IEnumerator Routine() {
    while (true) {
      var current = Aimer.forward.XZ();
      var desired = Target.position-Aimer.position.XZ();
      Aimer.forward = Vector3.RotateTowards(current, desired, Time.fixedDeltaTime*TurnSpeed, 0);
      yield return null;
    }
  }
}

public class Wait : SimpleTask {
  public float Seconds;
  public Wait(float seconds) {
    Seconds = seconds;
  }
  public override IEnumerator Routine() {
    yield return new WaitForSeconds(Seconds);
  }
}

public class WaitFrames : SimpleTask {
  public int Frames;
  public WaitFrames(int frames) {
    Frames = frames;
  }
  public override IEnumerator Routine() {
    for (var i = 0; i < Frames; i++) {
      yield return new WaitForFixedUpdate();
    }
  }
}

abstract public class SimpleAbility : MonoBehaviour {
  [NonSerialized]
  public bool IsComplete = true;
  public abstract IEnumerator Routine();
  public IEnumerator Wrapper() {
    yield return Routine();
    Stop();
  }

  public IEnumerator Begin() {
    var instance = Wrapper();
    IsComplete = false;
    StopAllCoroutines();
    StartCoroutine(instance);
    return instance;
  }

  public void Stop() {
    StopAllCoroutines();
    IsComplete = true;
  }
}