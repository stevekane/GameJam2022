using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace PigMoss {
  class BumRush : Ability {
    public Animator Animator;
    public Timeval WindupDuration = Timeval.FromSeconds(1);
    public Timeval RushDuration = Timeval.FromSeconds(.5f);
    public Timeval RecoveryDuration = Timeval.FromSeconds(.5f);
    public TriggerEvent SpikeTriggerEvent;
    [FormerlySerializedAs("SpikeHitParams")]
    public HitConfig HitConfig;
    public HitParams Foo;
    public ParticleSystem Trail;
    public AudioClip RushSFX;
    public float RushSpeed = 100;

    public override float Score() {
      if (BlackBoard.AngleScore <= .5) {
        return 0;
      } else {
        var distanceScore = BlackBoard.DistanceScore < 25 && BlackBoard.DistanceScore > 10 ? 1 : 0;
        return (BlackBoard.AngleScore+distanceScore)/2;
      }
    }

    public async Task Routine(TaskScope scope) {
      try {
        Animator.SetBool("Extended", true);
        var windup = Waiter.Delay(WindupDuration);
        var lookAt = Waiter.Repeat(Mover.TryLookAt, BlackBoard.Target);
        await scope.Any(windup, lookAt);
        if (BlackBoard.Target) {
          var contact = Waiter.ListenFor(SpikeTriggerEvent.OnTriggerEnterSource);
          var outcome = await scope.Any(contact, Waiter.Return<Collider>(Rush));
          if (outcome != null && outcome.TryGetComponent(out Hurtbox hurtbox)) {
            hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject));
          }
          await scope.Delay(RecoveryDuration);
        }
      } finally {
        Animator.SetBool("Extended", false);
      }
    }

    public async Task Rush(TaskScope scope) {
      try {
        using var effect = Status.Add(new ScriptedMovementEffect());
        SFXManager.Instance.TryPlayOneShot(RushSFX);
        Trail.Play();
        var direction = AbilityManager.transform.forward.XZ();
        var wait = Waiter.Delay(RushDuration);
        var move = Waiter.Repeat(Mover.Move, direction*RushSpeed*Time.fixedDeltaTime);
        await scope.Any(wait, move);
      } finally {
        Trail.Stop();
      }
    }
  }
}