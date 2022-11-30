using System;
using System.Collections;
using UnityEngine;

namespace PigMoss {
  class BumRush : FiberAbility {
    public Animator Animator;
    public Timeval WindupDuration = Timeval.FromSeconds(1);
    public Timeval RushDuration = Timeval.FromSeconds(.5f);
    public Timeval RecoveryDuration = Timeval.FromSeconds(.5f);
    public TriggerEvent SpikeTriggerEvent;
    public HitConfig SpikeHitParams;
    public HitParams Foo;
    public ParticleSystem Trail;
    public AudioClip RushSFX;
    public float RushSpeed = 100;

    StatusEffect RushStatusEffect;

    public override float Score() {
      if (BlackBoard.AngleScore <= .5) {
        return 0;
      } else {
        var distanceScore = BlackBoard.DistanceScore < 25 && BlackBoard.DistanceScore > 10 ? 1 : 0;
        return (BlackBoard.AngleScore+distanceScore)/2;
      }
    }

    public override void OnStop() {
      Status.Remove(RushStatusEffect);
      Animator.SetBool("Extended", false);
      Trail.Stop();
    }

    public override IEnumerator Routine() {
      Animator.SetBool("Extended", true);
      var windup = Fiber.Wait(WindupDuration);
      var lookAt = Fiber.Repeat(Mover.TryLookAt, BlackBoard.Target);
      yield return Fiber.Any(windup, lookAt);
      if (BlackBoard.Target) {
        var rush = Rush();
        var contact = Fiber.ListenFor(SpikeTriggerEvent.OnTriggerEnterSource);
        var outcome = Fiber.SelectTask(contact, rush);
        yield return outcome;
        if (outcome.Value == contact && contact.Value.TryGetComponent(out Hurtbox hurtbox)) {
          hurtbox.Defender.OnHit(SpikeHitParams.ComputeParams(Attributes), AbilityManager.transform);
        }
        yield return Fiber.Wait(RecoveryDuration);
        Animator.SetBool("Extended", false);
      } else {
        Animator.SetBool("Extended", false);
      }
    }

    IEnumerator Rush() {
      RushStatusEffect = new ScriptedMovementEffect();
      Status.Add(RushStatusEffect);
      SFXManager.Instance.TryPlayOneShot(RushSFX);
      Trail.Play();
      var direction = AbilityManager.transform.forward.XZ();
      var wait = Fiber.Wait(RushDuration);
      var move = Fiber.Repeat(Status.Move, direction*RushSpeed*Time.fixedDeltaTime);
      yield return Fiber.Any(wait, move);
      Trail.Stop();
      Status.Remove(RushStatusEffect);
    }
  }
}