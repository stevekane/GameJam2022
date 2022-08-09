using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttackAbility : ChargedAbility {
  public int Index;
  AbilityUser AbilityUser;
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
  public AbilityRunner ChargeStart = new();
  public AbilityRunner ChargeRelease = new();

  public bool Chargeable { get => ChargeRelease.EventTag.Length > 0; }

  protected override IEnumerator MakeRoutine() {
    Owner = GetComponentInParent<AbilityUser>().transform;
    Animator = GetComponentInParent<Animator>();
    if (Chargeable) {
      yield return Windup.StartWithCharge(Animator, Index);
    } else {
      yield return Windup.Start(Animator, Index);
    }
    yield return Active.Start(Animator, Index, OnHit);
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  public override void Stop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
    base.Stop();
  }

  protected IEnumerator OnHit(List<Transform> targets, int stopFrames) {
    Owner.GetComponent<Status>()?.Add(new HitStopEffect(Owner.forward, HitStopVibrationAmplitude, stopFrames));
    SFXManager.Instance.TryPlayOneShot(HitSFX);
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    targets.ForEach(target => {
      var hitParams = new HitParams {
        HitStopDuration = Active.HitFreezeDuration,
        Damage = HitDamage,
        KnockbackStrength = HitTargetKnockbackStrength,
        KnockbackType = KnockBackType.Delta
      };
      target.GetComponent<Defender>()?.OnHit(hitParams, Owner);
      VFXManager.Instance.TrySpawnEffect(HitVFX, target.transform.position+HitVFXOffset);
    });

    yield return Fiber.Wait(stopFrames);

    Owner.GetComponent<Status>()?.Add(new RecoilEffect(HitRecoilStrength * -Owner.forward));
  }

  public override void ReleaseCharge() {
    Windup.OnChargeEnd();
  }

  protected IEnumerator ChargeStartRoutine() {
    yield return MakeRoutine();
  }
  protected IEnumerator ChargeReleaseRoutine() {
    Windup.OnChargeEnd();
    yield return null;
  }

  void Start() {
    AbilityUser = GetComponentInParent<AbilityUser>();
    Owner = GetComponentInParent<AbilityUser>().transform;
    Animator = GetComponentInParent<Animator>();
    ChargeStart.Init(AbilityUser, this, ChargeStartRoutine, () => !ChargeStart.IsRunning);
    if (Chargeable) {
      ChargeRelease.Init(AbilityUser, this, ChargeReleaseRoutine, () => ChargeStart.IsRunning);
    }
  }
}