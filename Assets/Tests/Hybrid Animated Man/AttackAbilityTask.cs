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
  [NonSerialized] AnimationJobFacade Animation = null;

  public override void OnStop() {
    HitBox.enabled = false;
    PhaseHits.Clear();
    Animation?.OnFrame.Unlisten(OnFrame);
  }

  // Fiber -> Task adapter, ignore this.
  public IEnumerator Attack() => RunTask(AttackTask);
  async Task AttackTask(TaskScope scope) {
    Animation = AnimationDriver.Play(AttackAnimation);
    Animation.OnFrame.Listen(OnFrame);
    await scope.Any(While(() => Animation.IsRunning), Repeat(OnHit));
  }

  void OnFrame(int frame) {
    HitBox.enabled = frame >= WindupEnd.AnimFrames && frame <= ActiveEnd.AnimFrames;
    if (frame == WindupEnd.AnimFrames) {
      var rotation = AbilityManager.transform.rotation;
      var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
      SFXManager.Instance.TryPlayOneShot(AttackSFX);
      VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
    }
    if (frame >= ActiveEnd.AnimFrames) {
      CurrentTags.AddFlags(AbilityTag.Cancellable);
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