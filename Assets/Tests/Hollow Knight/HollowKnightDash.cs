using System;
using System.Collections;
using UnityEngine;

public class HollowKnightDash : IEnumerator, IStoppable {
  CharacterController2D Controller;
  ConditionAccum Condition;
  Vector2 Direction;
  int FramesRemaining;

  public HollowKnightDash(
  Vector2 direction,
  ConditionAccum condition,
  CharacterController2D controller) {
    Direction = direction;
    Condition = condition;
    Controller = controller;
    FramesRemaining = Timeval.FromMillis(100).Frames;
  }

  public void Stop() {
    FramesRemaining = 0;
  }
  public bool IsRunning => FramesRemaining > 0;
  public object Current => null;
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    Condition.CanAct.Set(!IsRunning);
    Condition.CanMove.Set(!IsRunning);
    Condition.CanJump.Set(!IsRunning);
    Condition.CanFall.Set(!IsRunning);
    if (FramesRemaining > 0) {
      var moveSpeed = 20f;
      Controller.Move(moveSpeed*Time.fixedDeltaTime*Direction);
      FramesRemaining--;
      if (FramesRemaining == 0) {
        Stop();
      }
    }
    return IsRunning;
  }
}