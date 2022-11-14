using System;
using System.Collections;
using UnityEngine;

public class HollowKnightMeleeAttack : IEnumerator, IStoppable {
  public ConditionAccum Condition;

  public bool IsRunning => FramesRemaining > 0;

  int FramesRemaining;
  Animator Saw;

  public HollowKnightMeleeAttack(
  Transform owner,
  ConditionAccum condition,
  Animator sawPrefab,
  Vector3 offset,
  Timeval duration) {
    Condition = condition;
    FramesRemaining = duration.Frames;
    Saw = GameObject.Instantiate(sawPrefab, owner);
    Saw.transform.localPosition = offset;
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
    FramesRemaining--;
    if (FramesRemaining == 0) {
      Stop();
    }
    return IsRunning;
  }
}