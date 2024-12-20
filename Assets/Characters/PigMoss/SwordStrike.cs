using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace PigMoss {
  [Serializable]
  public class SwordStrike : Ability {
    [FormerlySerializedAs("HitParams")]
    public HitConfig HitConfig;
    public Timeval ActiveFrameStart;
    public Timeval ActiveFrameEnd;
    public AnimationClip Clip;
    public Animator Animator;
    public Collider Collider;
    public TriggerEvent Contact;
    public GameObject SlashVFX;
    public AudioClip SlashSFX;
    public new AnimationDriver AnimationDriver;

    public override float Score() {
      var distanceScore = BlackBoard.DistanceScore < 5 ? 1 : 0;
      if (BlackBoard.AngleScore <= 0) {
        return 0;
      } else {
        return (BlackBoard.AngleScore+distanceScore)/2;
      }
    }

    public override async Task MainAction(TaskScope scope) {
      try {
        var animation = AnimationDriver.Play(scope, new() { Clip = Clip });  // TODO: FIXME!
        var windup = animation.WaitFrame(ActiveFrameStart.Ticks);
        var lookAt = Waiter.Repeat(Mover.TryLookAt, BlackBoard.Target);
        await scope.Any(windup, lookAt);
        var slashPosition = AbilityManager.transform.position;
        var slashRotation = AbilityManager.transform.rotation;
        SFXManager.Instance.TryPlayOneShot(SlashSFX);
        VFXManager.Instance.TrySpawn2DEffect(SlashVFX, slashPosition, slashRotation);
        Collider.enabled = true;
        var contact = Waiter.ListenFor(Contact.OnTriggerStaySource);
        var endActive = animation.WaitFrame(ActiveFrameEnd.Ticks);
        var activeOutcome = await scope.Any(contact, Waiter.Return<Collider>(endActive, null));
        if (activeOutcome != null && activeOutcome.TryGetComponent(out Hurtbox hurtbox)) {
          hurtbox.TryAttack(new HitParams(HitConfig, Attributes));
        }
        Collider.enabled = false;
        await animation.WaitDone(scope);
      } finally {
        Collider.enabled = false;
      }
    }
  }
}