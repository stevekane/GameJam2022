using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace PigMoss {
  [Serializable]
  public class SwordStrike : FiberAbility {
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

    public override IEnumerator Routine() {
      var animation = Animator.Run(Clip);
      var windup = animation.PlayUntil(ActiveFrameStart.Ticks);
      var lookAt = Fiber.Repeat(Mover.TryLookAt, BlackBoard.Target);
      yield return Fiber.Any(windup, lookAt);
      var slashPosition = AbilityManager.transform.position;
      var slashRotation = AbilityManager.transform.rotation;
      SFXManager.Instance.TryPlayOneShot(SlashSFX);
      VFXManager.Instance.TrySpawn2DEffect(SlashVFX, slashPosition, slashRotation);
      Collider.enabled = true;
      var contact = Fiber.ListenFor(Contact.OnTriggerStaySource);
      var endActive = animation.PlayUntil(ActiveFrameEnd.Ticks);
      var activeOutcome = Fiber.SelectTask(contact, endActive);
      yield return activeOutcome;
      if (activeOutcome.Value == contact && contact.Value.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(new HitParams(HitConfig, Attributes.serialized, Attributes.gameObject));
      }
      Collider.enabled = false;
      yield return animation;
    }
  }
}