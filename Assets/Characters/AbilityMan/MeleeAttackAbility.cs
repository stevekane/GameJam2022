using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttackAbility : Ability {
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