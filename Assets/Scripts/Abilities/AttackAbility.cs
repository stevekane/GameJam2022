using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AttackAbility : Ability {
  [SerializeField] bool Chargeable;
  [SerializeField] Timeval WindupEnd;
  [SerializeField] Timeval ChargeEnd;
  [SerializeField] Timeval ActiveEnd;
  [SerializeField] Timeval RecoveryEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] AnimationCurve ChargeScaling = AnimationCurve.Linear(0f, .5f, 1f, 1f);
  [SerializeField] Vibrator Vibrator;
  [SerializeField] AnimationJobConfig AttackAnimation;
  [SerializeField] TriggerEvent Hitbox;
  [SerializeField] Vector3 AttackVFXOffset;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  [NonSerialized] Collider[] Hits = new Collider[16];
  [NonSerialized] HashSet<Collider> PhaseHits = new();
  [NonSerialized] AnimationJob Animation = null;

  public override async Task MainAction(TaskScope scope) {
    Animation = AnimationDriver.Play(scope, AttackAnimation);
    HitConfig hitConfig = HitConfig;
    if (Chargeable) {
      var startFrame = Timeval.TickCount;
      await scope.Any(Charge, ListenFor(MainRelease));
      var numFrames = Timeval.TickCount - startFrame;
      var chargeScaling = ChargeScaling.Evaluate((float)numFrames / ChargeEnd.Ticks);
      await Animation.WaitFrame(WindupEnd.AnimFrames)(scope);
      hitConfig = hitConfig.Scale(chargeScaling);
    } else {
      await Animation.WaitFrame(WindupEnd.AnimFrames)(scope);
    }
    PhaseHits.Clear();
    var rotation = AbilityManager.transform.rotation;
    var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
    await scope.Any(Animation.WaitFrame(ActiveEnd.AnimFrames+1), Waiter.Repeat(OnHit(hitConfig)));
    await scope.Any(MakeCancellable, Animation.WaitDone());
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

  async Task MakeCancellable(TaskScope scope) {
    await scope.Delay(RecoveryEnd);
    Tags.AddFlags(AbilityTag.Cancellable);
    await scope.Forever();
  }

  TaskFunc OnHit(HitConfig hitConfig) => async (TaskScope scope) => {
    try {
      Hitbox.enableCollision = true;
      var hitCount = await scope.ListenForAll(Hitbox.OnTriggerStaySource, Hits);
      for (var i = 0; i < hitCount; i++) {
        var hit = Hits[i];
        if (!PhaseHits.Contains(hit) && hit.TryGetComponent(out Hurtbox hurtbox)) {
          hurtbox.TryAttack(new HitParams(hitConfig, Attributes));
          PhaseHits.Add(hit);
        }
      }
    } finally {
      Hitbox.enableCollision = false;
    }
  };
}