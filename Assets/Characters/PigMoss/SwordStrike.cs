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

  AnimationTask AnimationTask;

  public override void OnStop() {
    AnimationTask?.Stop();
    Collider.enabled = false;
  }

  public override IEnumerator Routine() {
    AnimationTask = Animator.Run(Clip);
    yield return AnimationTask.PlayUntil(ActiveFrameStart.Ticks);
    var slashPosition = AbilityManager.transform.position;
    var slashRotation = AbilityManager.transform.rotation;
    SFXManager.Instance.TryPlayOneShot(SlashSFX);
    VFXManager.Instance.TrySpawn2DEffect(SlashVFX, slashPosition, slashRotation);
    Collider.enabled = true;
    var contact = Fiber.ListenFor(Contact.OnTriggerStaySource);
    var endActive = AnimationTask.PlayUntil(ActiveFrameEnd.Ticks);
    var activeOutcome = Fiber.SelectTask(contact, endActive);
    if (activeOutcome.Value == contact && contact.Value.TryGetComponent(out Hurtbox hurtbox)) {
      hurtbox.Defender.OnHit(HitParams, AbilityManager.transform);
    }
    Collider.enabled = false;
    yield return AnimationTask;
  }
}