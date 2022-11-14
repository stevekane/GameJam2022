using System;
using System.Collections;
using UnityEngine;

public class HollowKnightMeleeAttack : IEnumerator, IStoppable {
  public ConditionAccum Condition;

  public bool IsRunning => FramesRemaining > 0;

  Vector3 Destination;
  int TotalFrames;
  int FramesRemaining;
  Animator Saw;

  public HollowKnightMeleeAttack(
  Transform owner,
  ConditionAccum condition,
  Animator sawPrefab,
  Vector3 destination,
  Timeval duration) {
    Condition = condition;
    TotalFrames = duration.Frames;
    FramesRemaining = duration.Frames;
    Destination = destination;
    Saw = GameObject.Instantiate(sawPrefab, owner);
    Saw.SetBool("Attacking", true);
  }
  public void Stop() {
    FramesRemaining = 0;
    Condition.CanAttack = true;
    Saw.SetBool("Attacking", false);
    Saw.gameObject.Destroy();
    Saw = null;
  }
  public object Current => null;
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    Condition.CanAttack = !IsRunning;
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