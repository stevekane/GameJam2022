using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Dive : Ability {
  [SerializeField] float FallSpeed;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] Timeval WindupDuration;
  [SerializeField] Timeval LandDuration;
  [SerializeField] Timeval RecoveryDuration;
  [SerializeField] AnimationJobConfig WindupAnimation;
  [SerializeField] AnimationJobConfig FallAnimation;
  [SerializeField] Transform WindupVFXTransform;
  [SerializeField] GameObject WindupVFX;
  [SerializeField] AudioClip WindupSFX;
  [SerializeField] Transform LandVFXTransform;
  [SerializeField] GameObject LandVFX;
  [SerializeField] AudioClip LandSFX;
  [SerializeField] TriggerEvent Hitbox;

  [NonSerialized] Collider[] Hits = new Collider[16];
  [NonSerialized] HashSet<Collider> PhaseHits = new();
  [NonSerialized] AnimationJob Animation = null;

  public override HitConfig HitConfigData => HitConfig;

  public static InlineEffect DiveEffect => new(s => {
    s.IsHittable = false;
    s.HasGravity = false;
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "DiveMove");

  public static InlineEffect RecoveryEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "DiveRecovery");

  public override async Task MainAction(TaskScope scope) {
    // Windup
    using var diveEffect = Status.Add(DiveEffect);
    SFXManager.Instance.TryPlayOneShot(WindupSFX);
    VFXManager.Instance.TrySpawnWithParent(WindupVFX, WindupVFXTransform, 1);
    Animation = AnimationDriver.Play(scope, WindupAnimation);
    await scope.All(Animation.PauseAtFrame(Animation.NumFrames-1), Waiter.Delay(WindupDuration));
    Animation.Stop();
    // Fall
    Animation = AnimationDriver.Play(scope, FallAnimation);
    await scope.All(Animation.PauseAtFrame(Animation.NumFrames-1), Fall);
    // Attack
    PhaseHits.Clear();
    SFXManager.Instance.TryPlayOneShot(LandSFX);
    VFXManager.Instance.TrySpawnEffect(LandVFX, AbilityManager.transform.position);
    await scope.Any(Waiter.Delay(LandDuration), HitHandler.Loop(Hitbox, new HitParams(HitConfig, Attributes)));
    // Recovery
    diveEffect.Dispose();
    using var recoveryEffect = Status.Add(RecoveryEffect);
    Tags.AddFlags(AbilityTag.Cancellable);
    await scope.Delay(RecoveryDuration);
    Animation.Stop();
  }

  async Task Fall(TaskScope scope) {
    while (!Status.IsGrounded) {
      Mover.Move(FallSpeed * Time.fixedDeltaTime * Vector3.down);
      await scope.Tick();
    }
  }
}