using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttackAbility : Ability {
  public int Index;
  Transform Owner;
  Animator Animator;
  public ChargedAttackPhase Windup;
  public HitboxAttackPhase Active;
  public InactiveAttackPhase Recovery;
  public HitConfig HitConfig;
  public GameObject HitVFX;
  public AudioClip HitSFX;
  public Vector3 HitVFXOffset = Vector3.up;
  public float HitStopVibrationAmplitude;
  public float HitCameraShakeIntensity;
  public float HitDamage;
  public float HitTargetKnockbackStrength;
  public float HitRecoilStrength;

  void Start() {
    Owner = GetComponentInParent<AbilityManager>().transform;
    Animator = GetComponentInParent<Animator>();
  }

  public IEnumerator AttackStart() {
    yield return Routine(false);
  }
  public IEnumerator ChargeStart() {
    yield return Routine(true);
  }
  public IEnumerator ChargeRelease() {
    Windup.OnChargeEnd();
    yield return null;
  }

  IEnumerator Routine(bool chargeable) {
    if (chargeable) {
      yield return Windup.StartWithCharge(Animator, Index);
    } else {
      yield return Windup.Start(Animator, Index);
    }
    yield return Active.Start(Animator, Index, OnHit);
    yield return Recovery.Start(Animator, Index);
    Done();
  }

  public void Done() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
  }

  protected IEnumerator OnHit(List<Transform> targets, int stopFrames) {
    Owner.GetComponent<Status>()?.Add(new HitStopEffect(Owner.forward, HitStopVibrationAmplitude, stopFrames));
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    var hitParams = HitConfig.ComputeParams(Attributes);
    targets.ForEach(target => {
      target.GetComponent<Defender>()?.OnHit(hitParams, Owner);
      Owner.transform.forward = (target.transform.position - Owner.transform.position).XZ().normalized;
    });

    yield return Fiber.Wait(stopFrames);

    Owner.GetComponent<Status>()?.Add(new RecoilEffect(HitRecoilStrength * -Owner.forward));
  }
}