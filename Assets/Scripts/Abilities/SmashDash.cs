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
    });
  }
  public static InlineEffect Invulnerable { get => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    });
  }

  // Button press/release.
  public IEnumerator Pressed() {
    Bundle bundle = new();
    bundle.Run(Fiber.Any(ListenFor(Release), WatchMoveAxis(bundle)));
    yield return bundle;
  }
  public IEnumerator Release() => null;

  // Detect when the move axis is released and pressed again. This sort of thing probably belongs in a lower level system.
  IEnumerator WatchMoveAxis(Bundle bundle) {
    const float ReleaseThreshold = .1f, ActiveThreshold = .5f;
    bool MoveAxisReleased() => AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude < ReleaseThreshold*ReleaseThreshold;
    bool MoveAxisActive() => AbilityManager.GetAxis(AxisTag.Move).XZ.sqrMagnitude > ActiveThreshold*ActiveThreshold;

    if (!MoveAxisReleased())
      yield break;

    while (true) {
      yield return Fiber.Until(MoveAxisActive);
      var dash = bundle.Run(Dash());
      yield return Fiber.All(Fiber.WhileRunning(dash), Fiber.Until(MoveAxisReleased));
    }
  }

  IEnumerator Dash() {
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    AddStatusEffect(ScriptedMove);
    using var final = Finally(() => Animator.Run(DoneClip));
    yield return Animator.Run(DashWindupClip);
    AddStatusEffect(Invulnerable);
    yield return Fiber.Any(new CountdownTimer(DashDuration), Animator.Run(DashingClip), Move(dir.normalized));
    yield return final;
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
