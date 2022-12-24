using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PigMoss {
  class BuzzSaw : Ability {
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

    public override async Task MainAction(TaskScope scope) {
      try {
        Animator.SetInteger("State", (int)BladeState.Revealed);
        SFXManager.Instance.TryPlayOneShot(RevealedSFX);
        await scope.Delay(RevealedDuration);
        SFXManager.Instance.TryPlayOneShot(ExtendedSFX);
        VFXManager.Instance.TrySpawn2DEffect(
          ExtendedVFX,
          AbilityManager.transform.position,
          AbilityManager.transform.rotation,
          ExtendedDuration.Seconds);
        Animator.SetInteger("State", (int)BladeState.Extended);
        await scope.Any(Waiter.Delay(ExtendedDuration), Waiter.Repeat(OnHit));
        Hits.ForEach(ProcessHit);
        SFXManager.Instance.TryPlayOneShot(HiddenSFX);
      } finally {
        Animator.SetInteger("State", (int)BladeState.Hidden);
      }
    }

    async Task OnHit(TaskScope scope) {
      var hit = await scope.ListenFor(BladeTriggerEvent.OnTriggerStaySource);
      var position = hit.transform.position;
      var direction = (position-AbilityManager.transform.position).XZ().normalized;
      var rotation = Quaternion.LookRotation(direction, Vector3.up);
      SFXManager.Instance.TryPlayOneShot(BladeContactSFX);
      VFXManager.Instance.TrySpawnEffect(BladeContactVFX, position, rotation);
      Hits.Add(hit);
    }

    void ProcessHit(Collider c) {
      if (c.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(new HitParams(BladeHitParams, Attributes));
      }
    }
  }
}