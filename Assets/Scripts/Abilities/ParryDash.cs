using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParryDash : Ability {
  public float MoveSpeed = 100f;
  public Timeval BlockDuration = Timeval.FromAnimFrames(15, 30);
  public Timeval DashDuration = Timeval.FromSeconds(.3f);
  public AnimationClip DashWindupClip;
  public AnimationClip DashingClip;
  public AnimationClip DoneClip;
  public MeleeAbility RiposteAbility;
  Animator Animator;
  Defender Defender;

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

  public IEnumerator Execute() {
    using (AddStatusEffect(Invulnerable)) {
      Animator.SetBool("Blocking", true);
      var hitEvent = Fiber.ListenFor(Defender.HitEvent);
      yield return Fiber.Any(hitEvent, new CountdownTimer(BlockDuration), Fiber.ListenFor(AbilityManager.GetEvent(Release)), Fiber.Until(() => AbilityManager.GetAxis(AxisTag.Move).XZ != Vector3.zero));
      Animator.SetBool("Blocking", false);
      if (hitEvent.IsCompleted) {
        AbilityManager.Bundle.Run(Riposte());
        yield break;
      }
    }
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    if (dir != Vector3.zero) {
      yield return Dash(dir);
    } else {
      yield break;
    }
  }

  public IEnumerator Release() => null;

  public IEnumerator Riposte() {
    using (Status.Add(Invulnerable)) {
      AbilityManager.TryInvoke(RiposteAbility.AttackStart);
      yield return Fiber.While(() => RiposteAbility.IsRunning);
    }
  }

  public IEnumerator Dash(Vector3 dir) {
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

  public override void OnStop() {
    Animator.SetBool("Blocking", false);
  }

  public override void FixedUpdate() {
    base.FixedUpdate();
    // TODO: This is bad. Need a more generic gesture system.
    if (Input.GetButton("L1") && AbilityManager.GetAxis(AxisTag.Move).XZ != Vector3.zero && !IsRunning) {
      AbilityManager.TryInvoke(this.Execute);
    }
  }
  public override void Awake() {
    base.Awake();
    Animator = GetComponentInParent<Animator>();
    Defender = GetComponentInParent<Defender>();
  }
}
