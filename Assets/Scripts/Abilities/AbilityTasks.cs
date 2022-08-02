using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class AimAt : AbilityTask {
  public Transform Aimer;
  public Transform Target;
  public float TurnSpeed;
  public AimAt(Transform aimer, Transform target, float turnSpeed) {
    Aimer = aimer;
    Target = target;
    TurnSpeed = turnSpeed;
    Enumerator = Routine();
  }
  public override IEnumerator Routine() {
    while (true) {
      var current = Aimer.rotation;
      var desired = Quaternion.LookRotation(Target.position.XZ()-Aimer.position.XZ(), Vector3.up);
      Aimer.rotation = Quaternion.RotateTowards(current, desired, Time.fixedDeltaTime*TurnSpeed);
      yield return null;
    }
  }
}

[Serializable]
public class InactiveAttackPhase : AbilityTask {
  int Index;
  Animator Animator;
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public IEnumerator Start(Animator animator, int index) {
    Reset();
    Animator = animator;
    Index = index;
    return this;
  }
  public override IEnumerator Routine() {
    for (var i = 0; i < Duration.Frames; i++) {
      yield return null;
      var attackSpeed = ClipDuration.Millis/Duration.Millis;
      Animator.SetFloat("AttackSpeed", attackSpeed);
      Animator.SetBool("Attacking", true);
      Animator.SetInteger("AttackIndex", Index);
    }
  }
}

[Serializable]
public class ChargedAttackPhase : AbilityTask {
  public int Index;
  public Animator Animator;
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public int ChargeFrameMultiplier = 1;
  public int FramesRemaining;
  public bool IsCharging;

  public void OnChargeBegin() {
    IsCharging = true;
    FramesRemaining *= ChargeFrameMultiplier;
  }

  public void OnChargeEnd() {
    IsCharging = false;
    FramesRemaining /= ChargeFrameMultiplier;
  }

  public override IEnumerator Routine() {
    while (FramesRemaining > 0) {
      yield return null;
      var multiplier = IsCharging ? ChargeFrameMultiplier : 1;
      var attackSpeed = ClipDuration.Millis/Duration.Millis;
      Animator.SetFloat("AttackSpeed", multiplier*attackSpeed);
      Animator.SetBool("Attacking", true);
      Animator.SetInteger("AttackIndex", Index);
      FramesRemaining--;
    }
  }
}

[Serializable]
public class HitboxAttackPhase : AbilityTask {
  public delegate IEnumerator OnHitFunc(List<Transform> hits, int stopFrames);

  int Index;
  Animator Animator;
  public AttackHitbox Hitbox;
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public Timeval HitFreezeDuration = Timeval.FromMillis(3, 30);
  public OnHitFunc OnHit;
  List<Transform> Hits = new List<Transform>(capacity: 4);
  List<Transform> PhaseHits = new List<Transform>(capacity: 4);

  public IEnumerator Start(Animator animator, int index, OnHitFunc onHit) {
    Reset();
    Animator = animator;
    Index = index;
    OnHit = onHit;
    return this;
  }
  public void OnContact(Transform target) {
    if (!PhaseHits.Contains(target)) {
      Hits.Add(target);
      PhaseHits.Add(target);
    }
  }
  public override IEnumerator Routine() {
    PhaseHits.Clear();
    Hitbox.Collider.enabled = true;
    Hitbox.TriggerStay = OnContact;
    for (var i = 0; i < Duration.Frames; i++) {
      yield return null;
      Animator.SetFloat("AttackSpeed", ClipDuration.Millis/Duration.Millis);
      Animator.SetBool("Attacking", true);
      Animator.SetInteger("AttackIndex", Index);
      if (Hits.Count > 0) {
        Hitbox.Collider.enabled = false;
        Hitbox.TriggerStay = null;
        yield return OnHit(Hits, HitFreezeDuration.Frames);
        Hitbox.Collider.enabled = true;
        Hitbox.TriggerStay = OnContact;
        Hits.Clear();
      }
    }
    Hitbox.Collider.enabled = false;
    Hitbox.TriggerStay = null;
  }
}
