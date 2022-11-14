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
  public AnimationCurve ChargeScaling = AnimationCurve.Linear(0f, .5f, 1f, 1f);  // go from .5 to 1 over the range
  public float HitStopVibrationAmplitude;
  public float HitCameraShakeIntensity;
  public float HitRecoilStrength;
  public float HitEnergyGain;

  void Start() {
    Owner = AbilityManager.transform;
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
    var chargeScaling = 1f;
    if (chargeable) {
      var frameCounter = new Timer();
      yield return Fiber.Any(frameCounter, Windup.StartWithCharge(Animator, Index));
      chargeScaling = ChargeScaling.Evaluate(((float)frameCounter.Value.Frames) / (Windup.Duration.Frames * Windup.ChargeFrameMultiplier));
    } else {
      yield return Windup.Start(Animator, Index);
    }
    yield return Active.Start(Animator, Index, ts => OnHit(ts, chargeScaling));
    yield return Recovery.Start(Animator, Index);
    Stop();
  }

  public override void OnStop() {
    Animator.SetBool("Attacking", false);
    Animator.SetInteger("AttackIndex", -1);
    Animator.SetFloat("AttackSpeed", 1);
  }

  protected IEnumerator OnHit(List<Transform> targets, float chargeScaling) {
    Status.Add(new HitStopEffect(Owner.forward, HitStopVibrationAmplitude, Active.HitFreezeDuration.Frames));
    CameraShaker.Instance.Shake(HitCameraShakeIntensity);
    var hitParams = HitConfig.ComputeParamsScaled(Attributes, chargeScaling);
    targets.ForEach(target => {
      target.GetComponent<Defender>()?.OnHit(hitParams, Owner);
      Owner.transform.forward = (target.transform.position - Owner.transform.position).XZ().normalized;
    });
    AbilityManager.Energy?.Value.Add(HitEnergyGain * targets.Count);

    yield return Fiber.Wait(Active.HitFreezeDuration.Frames);

    Status.Add(new RecoilEffect(HitRecoilStrength * -Owner.forward));
  }
}