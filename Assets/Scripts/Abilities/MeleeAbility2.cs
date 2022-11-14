using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAbility2 : Ability {
  Animator Animator;
  Transform Owner;
  public AnimationClip Clip;
  public Timeval WindupDuration;
  public Timeval ActiveDuration;
  public Timeval HitFreezeDuration;
  public Timeval RecoveryDuration;
  public HitConfig HitConfig;
  public AnimationCurve ChargeScaling = AnimationCurve.Linear(0f, .5f, 1f, 1f);  // go from .5 to 1 over the range
  public float HitStopVibrationAmplitude;
  public float HitCameraShakeIntensity;
  public float HitRecoilStrength;
  public float HitEnergyGain;
  public float ChargeSpeedFactor = .3f;
  public AttackHitbox Hitbox;

  AnimationTask Animation;
  List<Transform> Hits = new List<Transform>(capacity: 4);
  List<Transform> PhaseHits = new List<Transform>(capacity: 4);

  void Start() {
    Animator = GetComponentInParent<Animator>();
    Owner = Status.transform;
    Hitbox.TriggerStay = OnContact;
  }

  public IEnumerator AttackStart() {
    yield return Routine(false);
  }
  public IEnumerator ChargeStart() {
    yield return Routine(true);
  }
  public IEnumerator ChargeRelease() {
    Animation?.SetSpeed(1);
    yield return null;
  }

  IEnumerator Routine(bool chargeable) {
    Animation = new AnimationTask(Animator, Clip);

    // Windup
    var chargeScaling = 1f;
    if (chargeable) {
      Animation.SetSpeed(ChargeSpeedFactor);
      var frameCounter = new Timer();
      yield return Fiber.Any(frameCounter, Animation.PlayUntil(WindupDuration.Ticks));
      var extraFrames = frameCounter.Value.Frames - WindupDuration.Frames;
      var maxExtraFrames = WindupDuration.Frames / ChargeSpeedFactor - WindupDuration.Frames;
      chargeScaling = ChargeScaling.Evaluate(extraFrames / maxExtraFrames);
      Debug.Log($"Charged for: {extraFrames} / {maxExtraFrames} = {chargeScaling}");
      Animation.SetSpeed(1);
    } else {
      yield return Animation.PlayUntil(WindupDuration.Ticks);
    }
    // Active
    Hitbox.Collider.enabled = true;
    PhaseHits.Clear();
    Hits.Clear();
    yield return Fiber.Any(Animation.PlayUntil(WindupDuration.Ticks + ActiveDuration.Ticks), HandleHits(chargeScaling));
    Hitbox.Collider.enabled = false;
    // Recovery
    yield return Animation;
    Stop();
  }
  public override void OnStop() {
    Animation = null;
  }

  IEnumerator HandleHits(float chargeScaling) {
    while (true) {
      if (Hits.Count != 0) {
        Status.Add(new HitStopEffect(Owner.forward, HitStopVibrationAmplitude, HitFreezeDuration.Frames));
        CameraShaker.Instance.Shake(HitCameraShakeIntensity);
        var hitParams = HitConfig.ComputeParamsScaled(Attributes, chargeScaling);
        Hits.ForEach(target => {
          target.GetComponent<Defender>()?.OnHit(hitParams, Owner);
          Owner.transform.forward = (target.transform.position - Owner.transform.position).XZ().normalized;
        });
        AbilityManager.Energy?.Value.Add(HitEnergyGain * Hits.Count);

        Status.Add(new RecoilEffect(HitRecoilStrength * -Owner.forward));
        Hits.Clear();
      }
      yield return null;
    }
  }

  void OnContact(Transform target) {
    if (!PhaseHits.Contains(target)) {
      Hits.Add(target);
      PhaseHits.Add(target);
    }
  }
}