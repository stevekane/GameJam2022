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
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    targets.ForEach(target => {
      var hitParams = new HitParams {
        HitStopDuration = Active.HitFreezeDuration,
        Damage = Attributes.GetValue(AttributeTag.Damage, HitDamage),
        KnockbackStrength = Attributes.GetValue(AttributeTag.Knockback, HitTargetKnockbackStrength),
        KnockbackType = KnockBackType.Delta
      };
      target.GetComponent<Defender>()?.OnHit(hitParams, Owner);
      VFXManager.Instance.TrySpawnEffect(HitVFX, target.transform.position+HitVFXOffset);
      Owner.transform.forward = (target.transform.position - Owner.transform.position).normalized;
    });

    yield return Fiber.Wait(stopFrames);

    Owner.GetComponent<Status>()?.Add(new RecoilEffect(HitRecoilStrength * -Owner.forward));
  }
}