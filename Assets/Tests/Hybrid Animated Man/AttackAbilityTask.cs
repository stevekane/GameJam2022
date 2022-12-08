using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static TaskExtensionsForOlSteve;

public class AttackAbilityTask : Ability {
  [SerializeField] Timeval WindupEnd;
  [SerializeField] Timeval ActiveEnd;
  [SerializeField] Timeval RecoveryEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] Vibrator Vibrator;
  [SerializeField] Animator Animator;
  [SerializeField] AnimationDriver AnimationDriver;
  [SerializeField] AnimationJobConfig AttackAnimation;
  [SerializeField] TriggerEvent TriggerEvent;
  [SerializeField] Collider HitBox;
  [SerializeField] Vector3 AttackVFXOffset;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  [NonSerialized] HashSet<Collider> PhaseHits = new();
  [NonSerialized] Collider[] Hits = new Collider[16];
  [NonSerialized] AnimationJobTask Animation = null;

  public override void OnStop() {
    HitBox.enabled = false;
    PhaseHits.Clear();
  }

  // Fiber -> Task adapter, ignore this.
  public IEnumerator Attack() => RunTask(AttackTask);
  async Task AttackTask(TaskScope scope) {
    HitBox.enabled = false;
    await scope.Any(PlayAnim, Repeat(OnHit));
  }

  async Task PlayAnim(TaskScope scope) {
    try {
      Animation = AnimationDriver.Play(scope, AttackAnimation);
      await Animation.WaitFrame(scope, WindupEnd.AnimFrames);
      HitBox.enabled = true;
      var rotation = AbilityManager.transform.rotation;
      var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
      SFXManager.Instance.TryPlayOneShot(AttackSFX);
      VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
      await Animation.WaitFrame(scope, ActiveEnd.AnimFrames);
      HitBox.enabled = false;
      CurrentTags.AddFlags(AbilityTag.Cancellable);
      await Animation.WaitDone(scope);
    } finally {
      Animation.Stop();  // TODO: I think this is not needed since the job itself does this
    }
  }

  async Task OnHit(TaskScope scope) {
    var hitCount = await scope.ListenForAll(TriggerEvent.OnTriggerStaySource, Hits);
    var attacker = AbilityManager.transform;
    var newHits = false;
    for (var i = 0; i < hitCount; i++) {
      var hit = Hits[i];
      var contact = hit.transform.position;
      var rotation = AbilityManager.transform.rotation;
      if (!PhaseHits.Contains(hit) && hit.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(Attributes, HitConfig);
        PhaseHits.Add(hit);
        newHits = true;
      }
    }
    // TODO: Does this belong here? Should this happen in HurtBox or something after hit is confirmed?
    if (newHits) {
      CameraShaker.Instance.Shake(HitConfig.CameraShakeStrength);
      Status.Add(new HitStopEffect(attacker.forward, .1f, HitConfig.HitStopDuration.Ticks), s => {
        s.Add(new RecoilEffect(HitConfig.RecoilStrength * -attacker.forward));
      });
    }
  }
}