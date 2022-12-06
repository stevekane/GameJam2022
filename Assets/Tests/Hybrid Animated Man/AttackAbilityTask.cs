using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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
  [SerializeField] GameObject AttackHitVFX;
  [SerializeField] AudioClip AttackHitSFX;

  [NonSerialized] HashSet<Collider> PhaseHits = new();
  [NonSerialized] Collider[] Hits = new Collider[16];
  [NonSerialized] AnimationJobFacade Animation = null;

  public override void OnStop() {
    HitBox.enabled = false;
    PhaseHits.Clear();
    Animation?.OnFrame.Unlisten(OnFrame);
  }

  // Fiber -> Task adapter, ignore this.
  public IEnumerator Attack() {
    bool done = false;
    var task = new Task(async () => {
      using TaskScope scope = new();
      await AttackTask(scope);
      done = true;
    });
    task.Start(TaskScheduler.FromCurrentSynchronizationContext());
    while (!done)
      yield return null;
  }

  // Task entry point.
  async Task AttackTask(TaskScope scope) {
    Animation = AnimationDriver.Play(AttackAnimation);
    Animation.OnFrame.Listen(OnFrame);
    await scope.Any(c => c.While(() => Animation.IsRunning), c => c.Repeat(OnHit));
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
    var hitParams = HitConfig.ComputeParams(Attributes);
    for (var i = 0; i < hitCount; i++) {
      var hit = Hits[i];
      var contact = hit.transform.position;
      var rotation = AbilityManager.transform.rotation;
      if (!PhaseHits.Contains(hit)) {
        VFXManager.Instance.TrySpawn2DEffect(AttackHitVFX, contact+Vector3.up, rotation);
        hit.GetComponent<Hurtbox>()?.Defender.OnHit(hitParams, attacker);
        PhaseHits.Add(hit);
        newHits = true;
      }
    }
    if (newHits) {
      SFXManager.Instance.TryPlayOneShot(AttackHitSFX);
      CameraShaker.Instance.Shake(HitConfig.CameraShakeStrength);
      Status.Add(new HitStopEffect(attacker.forward, .1f, hitParams.HitStopDuration.Ticks));
      await scope.Ticks(hitParams.HitStopDuration.Ticks);
      Status.Add(new RecoilEffect(HitConfig.RecoilStrength * -attacker.forward));
    }
  }
}