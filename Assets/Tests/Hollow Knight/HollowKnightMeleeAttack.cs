using System;
using System.Collections;
using UnityEngine;

public class HollowKnightMeleeAttack : IEnumerator, IStoppable {
  ConditionAccum Condition;
  Vector3 Destination;
  Animator Saw;
  int TotalFrames;
  int FramesRemaining;

  public HollowKnightMeleeAttack(
  Transform owner,
  ConditionAccum condition,
  Animator sawPrefab,
  Vector3 destination) {
    var duration = Timeval.FromMillis(200);
    Condition = condition;
    TotalFrames = duration.Ticks;
    FramesRemaining = duration.Ticks;
    Destination = destination;
    Saw = GameObject.Instantiate(sawPrefab, owner);
    Saw.SetBool("Attacking", true);
  }
  public void Stop() {
    FramesRemaining = 0;
    Condition.CanAct.Set(true);
    Saw.SetBool("Attacking", false);
    Saw.gameObject.Destroy();
    Saw = null;
  }
  public bool IsRunning => FramesRemaining > 0;
  public object Current => null;
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    Condition.CanAct.Set(!IsRunning);
    if (FramesRemaining > 0) {
      FramesRemaining--;
      var interpolant = (float)FramesRemaining/(float)TotalFrames;
      Saw.transform.localPosition = Vector3.Lerp(Destination, Vector3.zero, interpolant);
      if (FramesRemaining == 0) {
        Stop();
      }
    }
    return IsRunning;
  }
}