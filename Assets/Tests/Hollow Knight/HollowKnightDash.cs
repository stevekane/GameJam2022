using System;
using System.Collections;
using UnityEngine;

public class HollowKnightDash : IEnumerator, IStoppable {
  ConditionAccum Condition;
  int FramesRemaining;

  public HollowKnightDash(ConditionAccum condition) {
    Condition = condition;
    FramesRemaining = Timeval.FromMillis(400).Frames;
  }

  public void Stop() {
    FramesRemaining = 0;
  }
  public bool IsRunning => FramesRemaining > 0;
  public object Current => null;
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    var moveSpeedMultiplier = 2.5f;
    Condition.CanAct.Set(!IsRunning);
    Condition.CanMove.Set(!IsRunning);
    Condition.CanJump.Set(!IsRunning);
    Condition.MoveSpeed.Mul(moveSpeedMultiplier);
    if (FramesRemaining > 0) {
      FramesRemaining--;
      if (FramesRemaining == 0) {
        Stop();
      }
    }
    return IsRunning;
  }
}