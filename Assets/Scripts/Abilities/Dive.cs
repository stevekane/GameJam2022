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
  [NonSerialized] AnimationJobTask Animation = null;

  public static InlineEffect ScriptedMove => new(s => {
    s.HasGravity = false;
    s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
    s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.TimesZero);
  }, "DiveMove");

  public override async Task MainAction(TaskScope scope) {
    // Windup
    HitConfig hitConfig = HitConfig;
    using var effect = Status.Add(ScriptedMove);
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
    VFXManager.Instance.TrySpawnEffect(LandVFX, AbilityManager.transform.position, Quaternion.LookRotation(Vector3.down));
    await scope.Any(Waiter.Delay(LandDuration), Waiter.Repeat(OnHit(hitConfig)));
    // Recovery
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