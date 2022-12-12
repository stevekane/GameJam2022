using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AttackAbilityTask : Ability {
  [SerializeField] Timeval WindupEnd;
  [SerializeField] Timeval ActiveEnd;
  [SerializeField] Timeval RecoveryEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] AnimationJobConfig AttackAnimation;
  [SerializeField] TriggerEvent TriggerEvent;
  [SerializeField] Collider HitBox;
  [SerializeField] Vector3 AttackVFXOffset;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  [NonSerialized] Collider[] Hits = new Collider[16];
  [NonSerialized] HashSet<Collider> PhaseHits = new();
  [NonSerialized] AnimationJobTask Animation = null;

  public async Task Attack(TaskScope scope) {
    HitBox.enabled = false;
    await scope.Any(PlayAnim, Waiter.Repeat(OnHit));
  }

  async Task PlayAnim(TaskScope scope) {
    try {
      Animation = AnimationDriver.Play(scope, AttackAnimation);
      await Animation.WaitFrame(WindupEnd.AnimFrames)(scope);
      PhaseHits.Clear();
      HitBox.enabled = true;
      var rotation = AbilityManager.transform.rotation;
      var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
      SFXManager.Instance.TryPlayOneShot(AttackSFX);
      VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
      await Animation.WaitFrame(ActiveEnd.AnimFrames+1)(scope);
      HitBox.enabled = false;
      Tags.AddFlags(AbilityTag.Cancellable);
      await Animation.WaitDone()(scope);
    } finally {
      HitBox.enabled = false;
      Debug.Assert(!Animation.IsRunning);
    }
  }

  async Task OnHit(TaskScope scope) {
    var hitCount = await scope.ListenForAll(TriggerEvent.OnTriggerStaySource, Hits);
    for (var i = 0; i < hitCount; i++) {
      var hit = Hits[i];
      if (!PhaseHits.Contains(hit) && hit.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(new HitParams(HitConfig, Attributes));
        PhaseHits.Add(hit);
      }
    }
  }
}