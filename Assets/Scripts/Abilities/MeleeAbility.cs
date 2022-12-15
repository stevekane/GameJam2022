using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class MeleeAbility : Ability {
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
  public AudioClip AttackSFX;
  public GameObject AttackVFX;
  public Vector3 VFXOffset = Vector3.up;
  public AttackHitbox Hitbox;
  public bool Chargeable = false;

  GameObject AttackVFXInstance;
  AnimationJobTask Animation;
  List<Hurtbox> Hits = new(capacity: 16);
  HashSet<Hurtbox> PhaseHits = new(capacity: 16);

  void Start() {
    Owner = Status.transform;
    Hitbox.TriggerStay = OnContact;
  }

  public override Task MainRelease(TaskScope scope) {
    Animation?.SetSpeed(1);
    return null;
  }

  public override async Task MainAction(TaskScope scope) {
    Animation = AnimationDriver.Play(scope, Clip);

    // Windup
    HitConfig hitConfig = HitConfig;
    if (Chargeable) {
      Animation.SetSpeed(ChargeSpeedFactor);
      var startFrame = Timeval.TickCount;
      await Animation.WaitFrame(WindupDuration.AnimFrames)(scope);
      var numFrames = Timeval.TickCount - startFrame;
      var extraFrames = numFrames - WindupDuration.Ticks;
      var maxExtraFrames = WindupDuration.Ticks / ChargeSpeedFactor - WindupDuration.Ticks;
      var chargeScaling = ChargeScaling.Evaluate(extraFrames / maxExtraFrames);
      Animation.SetSpeed(1);
      hitConfig = hitConfig.Scale(chargeScaling);
    } else {
      await Animation.WaitFrame(scope, WindupDuration.AnimFrames);
    }
    // Active
    Hitbox.Collider.enabled = true;
    PhaseHits.Clear();
    Hits.Clear();
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    AttackVFXInstance = VFXManager.Instance.TrySpawn2DEffect(AttackVFX, Owner.position + VFXOffset, Owner.rotation, ActiveDuration.Seconds);
    await scope.Any(
      Animation.WaitFrame(WindupDuration.AnimFrames + ActiveDuration.AnimFrames+1),
      Waiter.Repeat(HandleHits(hitConfig)));
    Hitbox.Collider.enabled = false;

    // Hitstop -- this code sucks and is probably not necessary
    //yield return Fiber.Until(() => Animator.speed != 0);
    //if (AttackVFXInstance && AttackVFXInstance.GetComponent<ParticleSystem>().main is var m)
    //  m.simulationSpeed = 1f;

    // Recovery
    await Animation.WaitDone()(scope);
  }

  //public override void OnStop() {
  //  Animation?.Stop();
  //  Animation = null;
  //}

  TaskFunc HandleHits(HitConfig config) => async (TaskScope scope) => {
    if (Hits.Count != 0) {
      Hits.ForEach(target => {
        target.TryAttack(new HitParams(config, Attributes.serialized, Attributes.gameObject));
        Owner.transform.forward = (target.transform.position - Owner.transform.position).XZ().normalized;
      });
      AbilityManager.Energy?.Value.Add(HitEnergyGain * Hits.Count);
      Hits.Clear();
    }
    await scope.Tick();
  };

  void OnContact(Hurtbox hurtbox) {
    if (!PhaseHits.Contains(hurtbox)) {
      Hits.Add(hurtbox);
      PhaseHits.Add(hurtbox);
    }
  }
}