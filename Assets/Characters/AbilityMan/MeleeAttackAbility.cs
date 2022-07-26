using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class InactiveAttackPhase : SimpleTask {
  public Animator Animator;
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public override IEnumerator Routine() {
    Animator.SetFloat("AttackSpeed", ClipDuration.Millis/Duration.Millis);
    yield return new WaitFrames(Duration.Frames);
  }
}

[Serializable]
public class HitboxAttackPhase : SimpleTask {
  public Transform Owner;
  public Animator Animator;
  public AttackHitbox Hitbox;
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public Timeval HitFreezeDuration = Timeval.FromMillis(3, 30);
  public List<Transform> Hits = new List<Transform>(capacity: 4);
  public List<Transform> PhaseHits = new List<Transform>(capacity: 4);
  public UnityEvent<Transform, List<Transform>, int> OnContactStart;
  public UnityEvent<Transform, List<Transform>> OnContactEnd;

  public void OnContact(Transform target) {
    if (!PhaseHits.Contains(target)) {
      Hits.Add(target);
      PhaseHits.Add(target);
    }
  }

  public override IEnumerator Routine() {
    PhaseHits.Clear();
    Hitbox.Collider.enabled = false;
    Hitbox.Collider.enabled = true;
    Hitbox.TriggerStay = OnContact;
    Animator.SetFloat("AttackSpeed", ClipDuration.Millis/Duration.Millis);
    for (var j = 0; j < Duration.Frames; j++) {
      Hits.Clear();
      yield return null;
      if (Hits.Count > 0) {
        OnContactStart.Invoke(Owner, Hits, HitFreezeDuration.Frames);
        Hitbox.Collider.enabled = false;
        yield return new WaitFrames(HitFreezeDuration.Frames);
        Hitbox.Collider.enabled = true;
        OnContactEnd.Invoke(Owner, Hits);
      }
    }
    Hitbox.Collider.enabled = false;
    Hitbox.TriggerStay = null;
  }
}

[Serializable]
public class AttackAnimatorParams {
  public string Attacking = "Attacking";
  public string AttackIndex = "AttackIndex";
  public string AttackSpeed = "AttackSpeed";
}

public class MeleeAttackAbility : SimpleAbility {
  public Transform Owner;
  public Animator Animator;
  public InactiveAttackPhase Windup;
  public HitboxAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public AttackAnimatorParams AnimatorParams;
  public GameObject HitVFX;
  public AudioClip HitSFX;
  public Vector3 HitVFXOffset = Vector3.up;
  public float HitStopVibrationAmplitude;
  public float HitCameraShakeIntensity;
  public float HitDamage;
  public float HitTargetKnockbackStrength;
  public float HitAttackerKnockbackStrength;
  public int Index;

  public override void BeforeBegin() {
    Animator.SetBool(AnimatorParams.Attacking, true);
    Animator.SetInteger(AnimatorParams.AttackIndex, Index);
    Animator.SetFloat(AnimatorParams.AttackSpeed, 1);
  }

  public override IEnumerator Routine() {
    Windup.Reset();
    yield return Windup;
    Active.Reset();
    yield return Active;
    Recovery.Reset();
    yield return Recovery;
  }

  public override void AfterEnd() {
    Animator.SetBool(AnimatorParams.Attacking, false);
    Animator.SetInteger(AnimatorParams.AttackIndex, -1);
    Animator.SetFloat(AnimatorParams.AttackSpeed, 1);
  }

  public void OnHitStopStart(Transform attacker, List<Transform> targets, int stopFrames) {
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    Owner.GetComponent<Status>()?.Add(new HitStopEffect(attacker.forward, HitStopVibrationAmplitude, stopFrames));
    targets.ForEach(target => {
      var delta = attacker.position-target.position;
      var axis = delta.normalized;
      target.GetComponent<Status>()?.Add(new HitStopEffect(axis, HitStopVibrationAmplitude, stopFrames));
      VFXManager.Instance.TrySpawnEffect(HitVFX, target.transform.position+HitVFXOffset);
    });
  }

  public void OnHitStopEnd(Transform attacker, List<Transform> targets) {
    attacker.GetComponent<Status>()?.Add(new KnockbackEffect(HitAttackerKnockbackStrength*-attacker.forward));
    targets.ForEach(target => {
      var delta = target.position-attacker.position;
      var direction = delta.normalized;
      target.GetComponent<Status>()?.Add(new KnockbackEffect(HitTargetKnockbackStrength*direction));
    });
  }
}