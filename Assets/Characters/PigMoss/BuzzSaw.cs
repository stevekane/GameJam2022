using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PigMoss {
  class BuzzSaw : FiberAbility {
    enum BladeState { Hidden = 0, Revealed = 1, Extended = 2 }

    public AudioClip RevealedSFX;
    public AudioClip ExtendedSFX;
    public GameObject ExtendedVFX;
    public AudioClip HiddenSFX;
    public Animator Animator;
    public Timeval RevealedDuration;
    public Timeval ExtendedDuration;
    public TriggerEvent BladeTriggerEvent;
    public HitConfig BladeHitParams;
    public AudioClip BladeContactSFX;
    public GameObject BladeContactVFX;

    HashSet<Collider> Hits = new();

    public override float Score() {
      if (BlackBoard.AngleScore < 0 && BlackBoard.DistanceScore < 5) {
        return 1;
      } else {
        return 0;
      }
    }

    public override void OnStop() {
      Animator.SetInteger("State", (int)BladeState.Hidden);
    }

    public override IEnumerator Routine() {
      Animator.SetInteger("State", (int)BladeState.Revealed);
      SFXManager.Instance.TryPlayOneShot(RevealedSFX);
      yield return Fiber.Wait(RevealedDuration);
      SFXManager.Instance.TryPlayOneShot(ExtendedSFX);
      VFXManager.Instance.TrySpawn2DEffect(
        ExtendedVFX,
        AbilityManager.transform.position,
        AbilityManager.transform.rotation,
        ExtendedDuration.Seconds);
      Animator.SetInteger("State", (int)BladeState.Extended);
      yield return Fiber.Any(Fiber.Wait(ExtendedDuration), Fiber.Repeat(OnHit));
      Hits.ForEach(ProcessHit);
      SFXManager.Instance.TryPlayOneShot(HiddenSFX);
      Animator.SetInteger("State", (int)BladeState.Hidden);
    }

    IEnumerator OnHit() {
      var hitEvent = Fiber.ListenFor(BladeTriggerEvent.OnTriggerStaySource);
      yield return hitEvent;
      var position = hitEvent.Value.transform.position;
      var direction = (position-AbilityManager.transform.position).XZ().normalized;
      var rotation = Quaternion.LookRotation(direction, Vector3.up);
      SFXManager.Instance.TryPlayOneShot(BladeContactSFX);
      VFXManager.Instance.TrySpawnEffect(BladeContactVFX, position, rotation);
      Hits.Add(hitEvent.Value);
    }

    void ProcessHit(Collider c) {
      if (c.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(Attributes, BladeHitParams);
      }
    }
  }
}