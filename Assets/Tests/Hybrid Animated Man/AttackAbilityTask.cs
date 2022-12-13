using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AttackAbilityTask : Ability {
  [SerializeField] Timeval WindupEnd;
  [SerializeField] Timeval ChargeEnd;
  [SerializeField] Timeval ActiveEnd;
  [SerializeField] Timeval RecoveryEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] AnimationCurve ChargeScaling = AnimationCurve.Linear(0f, .5f, 1f, 1f);
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

  public Task Attack(TaskScope scope) => Main(scope, false);
  public Task ChargeAttack(TaskScope scope) => Main(scope, true);
  public Task ChargeRelease(TaskScope scope) => null;

  async Task Main(TaskScope scope, bool chargeable) {
    try {
      Animation = AnimationDriver.Play(scope, AttackAnimation);
      HitConfig hitConfig = HitConfig;
      if (chargeable) {
        var startFrame = Timeval.TickCount;
        await scope.Any(Charge, ListenFor(ChargeRelease));
        var numFrames = Timeval.TickCount - startFrame;
        var chargeScaling = ChargeScaling.Evaluate((float)numFrames / ChargeEnd.Ticks);
        await Animation.WaitFrame(WindupEnd.AnimFrames)(scope);
        hitConfig = hitConfig.Scale(chargeScaling);
      } else {
        await Animation.WaitFrame(WindupEnd.AnimFrames)(scope);
      }
      PhaseHits.Clear();
      HitBox.enabled = true;
      var rotation = AbilityManager.transform.rotation;
      var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
      SFXManager.Instance.TryPlayOneShot(AttackSFX);
      VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
      await scope.Any(Animation.WaitFrame(ActiveEnd.AnimFrames+1), Waiter.Repeat(OnHit(hitConfig)));
      HitBox.enabled = false;
      Tags.AddFlags(AbilityTag.Cancellable);
      await Animation.WaitDone()(scope);
    } finally {
      HitBox.enabled = false;
      Debug.Assert(!Animation.IsRunning);
    }
  }

  async Task Charge(TaskScope scope) {
    try {
      await Animation.WaitFrame(1)(scope);
      Animation.Pause();
      await scope.Delay(ChargeEnd);
    } finally {
      Animation.Resume();
    }
  }

  TaskFunc OnHit(HitConfig hitConfig) => async (TaskScope scope) => {
    var hitCount = await scope.ListenForAll(TriggerEvent.OnTriggerStaySource, Hits);
    for (var i = 0; i < hitCount; i++) {
      var hit = Hits[i];
      if (!PhaseHits.Contains(hit) && hit.TryGetComponent(out Hurtbox hurtbox)) {
        hurtbox.TryAttack(new HitParams(hitConfig, Attributes));
        PhaseHits.Add(hit);
      }
    }
  };
}