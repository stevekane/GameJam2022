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

    public override void OnStop() {
      Collider.enabled = false;
    }

    public async Task Routine(TaskScope scope) {
      var animation = AnimationDriver.Play(scope, Clip);
      var windup = animation.WaitFrame(ActiveFrameStart.AnimFrames);
      var lookAt = Waiter.Repeat(Mover.TryLookAt, BlackBoard.Target);
      await scope.Any(windup, lookAt);
      var slashPosition = AbilityManager.transform.position;
      var slashRotation = AbilityManager.transform.rotation;
      SFXManager.Instance.TryPlayOneShot(SlashSFX);
      VFXManager.Instance.TrySpawn2DEffect(SlashVFX, slashPosition, slashRotation);
      Collider.enabled = true;
      var contact = Waiter.ListenFor(Contact.OnTriggerStaySource);
      var endActive = animation.WaitFrame(ActiveFrameEnd.AnimFrames);
      var activeOutcome = await scope.Any(contact, Waiter.ReturnDefault<Collider>(endActive));
      if (activeOutcome != null && activeOutcome.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject));
      }
      Collider.enabled = false;
      await animation.WaitDone(scope);
    }
  }
}