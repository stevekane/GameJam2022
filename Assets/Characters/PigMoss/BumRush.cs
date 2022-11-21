using System;
using System.Collections;
using UnityEngine;

namespace PigMoss {
  [Serializable]
  class BumRushConfig {
    public Animator Animator;
    public Timeval WindupDuration = Timeval.FromSeconds(1);
    public Timeval RushDuration = Timeval.FromSeconds(.5f);
    public Timeval RecoveryDuration = Timeval.FromSeconds(.5f);
    public TriggerEvent SpikeTriggerEvent;
    public HitParams SpikeHitParams;
    public ParticleSystem Trail;
    public AudioClip RushSFX;
    public float RushSpeed = 100;
  }

  class BumRush : FiberAbility {
    new AbilityTag Tags = AbilityTag.Uninterruptible;

    BumRushConfig Config;
    Transform Target;
    StatusEffect RushStatusEffect;
    bool WindupSuccessful;

    public BumRush(AbilityManager manager, BumRushConfig config, Transform target) {
      AbilityManager = manager;
      Config = config;
      Target = target;
      WindupSuccessful = false;
    }
    public override void OnStop() {
      Status.Remove(RushStatusEffect);
      Config.Animator.SetBool("Extended", false);
      Config.Trail.Stop();
      WindupSuccessful = false;
    }
    IEnumerator Rush() {
      RushStatusEffect = new ScriptedMovementEffect();
      Status.Add(RushStatusEffect);
      SFXManager.Instance.TryPlayOneShot(Config.RushSFX);
      Config.Trail.Play();
      var delta = Target.position-Mover.transform.position;
      var direction = delta.normalized;
      for (var tick = 0; tick < Config.RushDuration.Ticks; tick++) {
        Status.Move(direction*Config.RushSpeed*Time.fixedDeltaTime);
        yield return null;
      }
      Config.Trail.Stop();
      Status.Remove(RushStatusEffect);
    }
    public override IEnumerator Routine() {
      // TODO: This is awkwardly-encoded.
      // Ideally, you would say
      // Windup.Success => Rush
      // Windup.Failure = Stop
      yield return Windup();
      if (WindupSuccessful) {
        var rush = Rush();
        var contact = Fiber.ListenFor(Config.SpikeTriggerEvent.OnTriggerEnterSource);
        var outcome = Fiber.Select(contact, rush);
        yield return outcome;
        // hit target
        if (outcome.Value == 0 && contact.Value.TryGetComponent(out Hurtbox hurtbox)) {
          hurtbox.Defender.OnHit(Config.SpikeHitParams, AbilityManager.transform);
        }
        yield return Fiber.Wait(Config.RecoveryDuration);
        Config.Animator.SetBool("Extended", false);
      }
    }
    public IEnumerator Windup() {
      Config.Animator.SetBool("Extended", true);
      var windup = Fiber.Wait(Config.WindupDuration);
      var lookAt = Fiber.Repeat(Mover.TryLookAt, Target);
      var success = Fiber.Any(windup, lookAt);
      var failure = Fiber.Until(() => Target == null);
      var windupOutcome = Fiber.SelectTask(success, failure);
      yield return windupOutcome;
      WindupSuccessful = windupOutcome.Value == success;
    }
  }
}