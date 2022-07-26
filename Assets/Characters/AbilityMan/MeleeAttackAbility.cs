using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class InactiveAttackPhase : SimpleTask {
  [Header("Components")]
  public Animator Animator;
  [Header("Frame Data")]
  public Timeval Duration = Timeval.FromMillis(0,30);
  public Timeval ClipDuration = Timeval.FromMillis(0,30);
  [Header("Events")]
  public UnityEvent OnBegin;
  public UnityEvent OnEnd;
  public UnityEvent OnTick;
  public override IEnumerator Routine() {
    OnBegin.Invoke();
    Animator.SetFloat("AttackSpeed", ClipDuration.Millis/Duration.Millis);
    for (var i = 0; i < Duration.Frames; i++) {
      yield return null;
      OnTick.Invoke();
    }
    OnEnd.Invoke();
  }
}

[Serializable]
public class HitboxAttackPhase : SimpleTask {
  static int INITIAL_HIT_BUFFER_SIZE = 4;

  [Header("Components")]
  public Transform Owner;
  public Animator Animator;
  public AttackHitbox Hitbox;
  [Header("Buffers")]
  public List<Transform> Hits = new List<Transform>(INITIAL_HIT_BUFFER_SIZE);
  public List<Transform> PhaseHits = new List<Transform>(INITIAL_HIT_BUFFER_SIZE);
  [Header("Frame Data")]
  public Timeval Duration = Timeval.FromMillis(0, 30);
  public Timeval ClipDuration = Timeval.FromMillis(0, 30);
  public Timeval HitFreezeDuration = Timeval.FromMillis(3, 30);
  [Header("Events")]
  public UnityEvent OnBegin;
  public UnityEvent OnEnd;
  public UnityEvent OnTick;
  public UnityEvent<Transform, Transform> OnContactStart;
  public UnityEvent<Transform, Transform> OnContactEnd;

  /*
  TODO : Status now found on target currently because transform probably transform of some hurtbox
         but not the actual entity
  TODO : 2 frames windup causes hitpause to happen too late...
  TODO : Move all optional reactions to hitstop to an effect on target/attacker
  DONE : First punch never detected. I am not sure why as all subsequent punches work...
  DONE : Animator.speed does not seem to slow down the attack... why?
  DONE : Punches fire 3 ContactStart and 3 ContactEnd events... why?
  */
  public override IEnumerator Routine() {
    Hits.Clear();
    PhaseHits.Clear();
    OnBegin.Invoke();
    Hitbox.Collider.enabled = false;
    Hitbox.Collider.enabled = true;
    Hitbox.TriggerEnter = OnContact;
    Animator.SetFloat("AttackSpeed", ClipDuration.Millis/Duration.Millis);
    for (var j = 0; j < Duration.Frames; j++) {
      yield return null;
      OnTick.Invoke();
      // TODO: use the axis determined by the attack as per attacker
      var axis = Vector3.up;
      var amplitude = .25f;
      if (Hits.Count > 0) {
        Debug.Log("Here");
        if (Owner.TryGetComponent(out Status status)) {
          status.Add(new HitStopEffect(axis, amplitude, HitFreezeDuration.Frames));
        }
        Hits.ForEach(target => {
          if (target.TryGetComponent(out Status targetStatus))  {
            targetStatus.Add(new HitStopEffect(axis, amplitude, HitFreezeDuration.Frames));
          }
        });
        Hits.ForEach(target => OnContactStart.Invoke(Owner, target));
        yield return new WaitFrames(HitFreezeDuration.Frames);
        Hits.ForEach(target => OnContactEnd.Invoke(Owner, target));
        Hits.Clear();
      }
    }
    Hitbox.Collider.enabled = false;
    Hitbox.TriggerEnter = null;
    OnEnd.Invoke();
    PhaseHits.Clear();
    Hits.Clear();
  }

  public void OnContact(Transform target) {
    if (!PhaseHits.Contains(target)) {
      Hits.Add(target);
      PhaseHits.Add(target);
    }
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
  public InactiveAttackPhase Windup;
  public HitboxAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public AttackAnimatorParams AnimatorParams;
  public GameObject HitVFX;
  public AudioClip HitSFX;
  public float HitCameraShakeIntensity;
  public float HitDamage;
  public float HitTargetKnockbackStrength;
  public float HitAttackerKnockbackStrength;
  public int Index;

  public override void BeforeBegin() {
    Owner.GetComponent<Animator>().SetBool(AnimatorParams.Attacking, true);
    Owner.GetComponent<Animator>().SetInteger(AnimatorParams.AttackIndex, Index);
    Owner.GetComponent<Animator>().SetFloat(AnimatorParams.AttackSpeed, 1);
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
    Owner.GetComponent<Animator>().SetBool(AnimatorParams.Attacking, false);
    Owner.GetComponent<Animator>().SetInteger(AnimatorParams.AttackIndex, -1);
    Owner.GetComponent<Animator>().SetFloat(AnimatorParams.AttackSpeed, 1);
  }

  // TODO: Not sure this should live on this most basic class
  public void OnHitStopStart(Transform attacker, Transform target) {
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    VFXManager.Instance.TrySpawnEffect(HitVFX, target.transform.position+Vector3.up);
    SFXManager.Instance.TryPlayOneShot(HitSFX);
  }

  // TODO: Not sure this should live on this most basic class
  public void OnHitStopEnd(Transform attacker, Transform target) {
    var toTargetDelta = target.position-attacker.position;
    var toTarget = toTargetDelta.XZ().normalized;
    if (attacker.TryGetComponent(out Status attackerStatus)) {
      attackerStatus.Add(new KnockbackEffect(HitAttackerKnockbackStrength*-toTarget));
    }
    if (target.TryGetComponent(out Status targetStatus)) {
      targetStatus.Add(new KnockbackEffect(HitTargetKnockbackStrength*toTarget));
    }
  }
}