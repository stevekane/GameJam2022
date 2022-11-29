using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmashDash : Ability {
  public float MoveSpeed = 120f;
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
    });
  }
  public static InlineEffect Invulnerable { get => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    });
  }

  public IEnumerator Pressed() {
    yield return Fiber.Any(ListenFor(Release), Loop());
  }

  public IEnumerator Release() => null;

  public IEnumerator Loop() {
    yield return WaitForMoveAxisChange();
    while (true) {
      yield return Fiber.All(Dash(), WaitForMoveAxisChange());
    }
  }

  // Detect when the move axis is released and pressed again. This sort of thing probably belongs in a lower level system.
  public IEnumerator WaitForMoveAxisChange() {
    const float releaseThreshold = .1f;
    const float activeThreshold = .5f;
    yield return Fiber.Until(() => AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude < releaseThreshold*releaseThreshold);
    yield return Fiber.Until(() => AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude > activeThreshold*activeThreshold);
  }

  public IEnumerator Dash() {
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    AddStatusEffect(ScriptedMove);
    yield return Animator.Run(DashWindupClip);
    AddStatusEffect(Invulnerable);
    yield return Fiber.Any(new CountdownTimer(DashDuration), Animator.Run(DashingClip), Move(dir.normalized));
    yield return Animator.Run(DoneClip);
  }

  public IEnumerator Move(Vector3 dir) {
    while (true) {
      // TODO: steering
      Status.transform.forward = dir;
      var move = MoveSpeed * Time.fixedDeltaTime * dir;
      Status.Move(move);
      yield return null;
    }
  }

  public override void Awake() {
    base.Awake();
    Animator = GetComponentInParent<Animator>();
  }
}
