using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class InactiveAttackPhase : SimpleTask {
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
      yield return new WaitForFixedUpdate();
      var attackSpeed = ClipDuration.Millis/Duration.Millis;
      Animator.SetFloat("AttackSpeed", attackSpeed);
      Animator.SetBool("Attacking", true);
      Animator.SetInteger("AttackIndex", Index);
    }
  }
}

[Serializable]
public class ChargedAttackPhase : SimpleTask {
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
      yield return new WaitForFixedUpdate();
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
public class HitboxAttackPhase : SimpleTask {
  int Index;
  Animator Animator;
  public Transform Owner;
  public AttackHitbox Hitbox;
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public Timeval HitFreezeDuration = Timeval.FromMillis(3, 30);
  public List<Transform> Hits = new List<Transform>(capacity: 4);
  public List<Transform> PhaseHits = new List<Transform>(capacity: 4);
  public UnityEvent<Transform, List<Transform>, int> OnContactStart;
  public UnityEvent<Transform, List<Transform>> OnContactEnd;

  public IEnumerator Start(Animator animator, int index) {
    Reset();
    Animator = animator;
    Index = index;
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
      yield return new WaitForFixedUpdate();
      Animator.SetFloat("AttackSpeed", ClipDuration.Millis/Duration.Millis);
      Animator.SetBool("Attacking", true);
      Animator.SetInteger("AttackIndex", Index);
      if (Hits.Count > 0) {
        Hitbox.Collider.enabled = false;
        Hitbox.TriggerStay = null;
        OnContactStart.Invoke(Owner, Hits, HitFreezeDuration.Frames);
        yield return new WaitFrames(HitFreezeDuration.Frames);
        OnContactEnd.Invoke(Owner, Hits);
        Hitbox.Collider.enabled = true;
        Hitbox.TriggerStay = OnContact;
        Hits.Clear();
      }
    }
    Hitbox.Collider.enabled = false;
    Hitbox.TriggerStay = null;
  }
}

public class MeleeAttackAbility : SimpleAbility {
  public int Index;
  public Transform Owner;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public HitboxAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public GameObject HitVFX;
  public AudioClip HitSFX;
  public Vector3 HitVFXOffset = Vector3.up;
  public float HitStopVibrationAmplitude;
  public float HitCameraShakeIntensity;
  public float HitDamage;
  public float HitTargetKnockbackStrength;
  public float HitAttackerKnockbackStrength;

public override IEnumerator Routine() {
    yield return Windup.Start(Animator, Index);
    yield return Active.Start(Animator, Index);
    yield return Recovery.Start(Animator, Index);
  }

  public override void AfterEnd() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
  }

  public void OnHitStopStart(Transform attacker, List<Transform> targets, int stopFrames) {
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    Owner.GetComponent<Status>()?.Add(new HitStopEffect(attacker.forward, HitStopVibrationAmplitude, stopFrames));
    targets.ForEach(target => {
      // TODO: this is gross
      target.GetComponent<Defender>()?.OnHit(new HitParams { HitStopDuration = Active.HitFreezeDuration, Damage = HitDamage, KnockbackStrength = HitTargetKnockbackStrength, KnockbackType = KnockBackType.Delta }, attacker);
      VFXManager.Instance.TrySpawnEffect(HitVFX, target.transform.position+HitVFXOffset);
    });
  }

  public void OnHitStopEnd(Transform attacker, List<Transform> targets) {
    attacker.GetComponent<Status>()?.Add(new KnockbackEffect(HitAttackerKnockbackStrength*-attacker.forward));
  }
}