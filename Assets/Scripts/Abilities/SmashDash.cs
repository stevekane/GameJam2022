using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmashDash : Ability {
  public float MaxMoveSpeed = 120f;
  public float MinMoveSpeed = 60f;
  public float TurnSpeed = 60f;
  public Timeval DashDuration = Timeval.FromSeconds(.3f);
  public AnimationClip DashWindupClip;
  public AnimationClip DashingClip;
  public AnimationClip DoneClip;
  Animator Animator;

  public static InlineEffect ScriptedMove { get => new(s => {
      s.HasGravity = false;
      s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
      s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.TimesZero);
    }, "DashMove");
  }
  public static InlineEffect Invulnerable { get => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "DashInvulnerable");
  }

  // Button press/release.
  bool IsPressed = false;
  public IEnumerator Pressed() {
    IsPressed = true;
    yield return InputLoop();
  }
  public IEnumerator Release() {
    IsPressed = false;
    yield return null;
  }

  //public override void OnStop() {
  //  AbilityManager.Bundle.StartRoutine(Animator.Run(DoneClip));
  //}

  // Detect when the move axis is released and pressed again. This sort of thing probably belongs in a lower level system.
  IEnumerator InputLoop() {
    while (IsPressed) {
      var outcome = Fiber.Select(ListenForMoveAction(), FiberListenFor(Release));
      yield return outcome;
      if (outcome.Value == 0)
        yield return Dash();
    }
  }

  IEnumerator ListenForMoveAction() {
    const float ReleaseThreshold = .1f, ActiveThreshold = .5f;
    bool MoveAxisReleased() => AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude < ReleaseThreshold*ReleaseThreshold;
    bool MoveAxisActive() => AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude > ActiveThreshold*ActiveThreshold;
    yield return Fiber.Until(MoveAxisReleased);
    yield return Fiber.Until(MoveAxisActive);
  }

  IEnumerator Dash() {
    AbilityManager.CancelOthers(this);
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    AddStatusEffect(ScriptedMove);
    yield return Animator.Run(DashWindupClip);
    AddStatusEffect(Invulnerable);
    yield return Fiber.Any(new CountdownTimer(DashDuration), Animator.Run(DashingClip), Move(dir.normalized));
    yield return Animator.Run(DoneClip);
  }

  IEnumerator Move(Vector3 dir) {
    while (true) {
      var desiredDir = AbilityManager.GetAxis(AxisTag.Move).XZ;
      var desiredSpeed = Mathf.SmoothStep(MinMoveSpeed, MaxMoveSpeed, desiredDir.magnitude);
      var targetDir = desiredDir.TryGetDirection() ?? dir;
      dir = Vector3.RotateTowards(dir, targetDir.normalized, TurnSpeed/360f, 0f);
      Status.transform.forward = dir;
      Status.Move(desiredSpeed * Time.fixedDeltaTime * dir);
      yield return null;
    }
  }

  public override void Awake() {
    base.Awake();
    Animator = GetComponentInParent<Animator>();
  }
}
