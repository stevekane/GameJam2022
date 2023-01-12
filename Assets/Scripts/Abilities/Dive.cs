using System;
using System.Threading.Tasks;
using UnityEngine;

public class Dive : Ability {
  [SerializeField] float FallSpeed;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] AnimationJobConfig AnimationConfig;
  [SerializeField] SerializableEnum<AvatarBone> WindupVFXAttachment;
  [SerializeField] GameObject WindupVFX;
  [SerializeField] AudioClip WindupSFX;
  [SerializeField] SerializableEnum<AvatarBone> LandVFXAttachment;
  [SerializeField] GameObject LandVFX;
  [SerializeField] AudioClip LandSFX;
  [SerializeField] TriggerEvent Hitbox;

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
  }, "DiveRecovery");

  public override async Task MainAction(TaskScope scope) {
    // Windup
    using var diveEffect = Status.Add(DiveEffect);
    SFXManager.Instance.TryPlayOneShot(WindupSFX);
    VFXManager.Instance.TrySpawnWithParent(WindupVFX, AvatarAttacher.GetBoneTransform(WindupVFXAttachment), 1);
    Animation = AnimationDriver.Play(scope, AnimationConfig);
    await Animation.WaitPhase(scope, 0);
    // Fall
    await scope.All(Animation.PauseAfterPhase(1), Fall);
    // Attack
    SFXManager.Instance.TryPlayOneShot(LandSFX);
    VFXManager.Instance.TrySpawnWithParent(LandVFX, AvatarAttacher.GetBoneTransform(LandVFXAttachment));
    Animation.Resume();
    await scope.Any(Animation.WaitPhase(2), HitHandler.Loop(Hitbox, new HitParams(HitConfig, Attributes)));
    // Recovery
    diveEffect.Dispose();
    using var recoveryEffect = Status.Add(RecoveryEffect);
    Tags.AddFlags(AbilityTag.Cancellable);
    await Animation.WaitDone(scope);
  }

  async Task Fall(TaskScope scope) {
    while (!Status.IsGrounded) {
      Mover.Move(FallSpeed * Time.fixedDeltaTime * Vector3.down);
      await scope.Tick();
    }
  }
}