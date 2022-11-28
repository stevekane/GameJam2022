using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class SwordStrike : FiberAbility {
  public HitParams HitParams;
  public Timeval ActiveFrameStart;
  public Timeval ActiveFrameEnd;
  public AnimationClip Clip;
  public Animator Animator;
  public Collider Collider;
  public TriggerEvent Contact;
  public GameObject SlashVFX;
  public AudioClip SlashSFX;

  public override void OnStop() {
    Collider.enabled = false;
  }

  public override IEnumerator Routine() {
    var animation = Animator.Run(Clip);
    yield return animation.PlayUntil(ActiveFrameStart.Ticks);
    var slashPosition = AbilityManager.transform.position;
    var slashRotation = AbilityManager.transform.rotation;
    SFXManager.Instance.TryPlayOneShot(SlashSFX);
    VFXManager.Instance.TrySpawn2DEffect(SlashVFX, slashPosition, slashRotation);
    Collider.enabled = true;
    var contact = Fiber.ListenFor(Contact.OnTriggerStaySource);
    var endActive = animation.PlayUntil(ActiveFrameEnd.Ticks);
    yield return Fiber.Any(contact, endActive);
    if (!contact.IsRunning && contact.Value && contact.Value.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.Defender.OnHit(HitParams, AbilityManager.transform);
    }
    Collider.enabled = false;
    yield return animation;
  }
}