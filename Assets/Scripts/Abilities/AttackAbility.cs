using System;
using System.Threading.Tasks;
using UnityEngine;

public class AttackAbility : Ability {
  [SerializeField] bool Chargeable;
  [SerializeField] bool InPlace = false;
  [SerializeField] bool RecoveryCancelable = true;
  [SerializeField] Timeval ChargeEnd;
  [SerializeField] HitConfig HitConfig;
  [SerializeField] AnimationCurve ChargeScaling = AnimationCurve.Linear(0f, .5f, 1f, 1f);
  [SerializeField] AnimationJobConfig AttackAnimation;
  [SerializeField] TriggerEvent Hitbox;
  [SerializeField] Parrybox Parrybox;
  [SerializeField] Vector3 AttackVFXOffset;
  [SerializeField] GameObject AttackVFX;
  [SerializeField] AudioClip AttackSFX;

  [NonSerialized] AnimationJob Animation = null;

  public override HitConfig HitConfigData => HitConfig;

  public static InlineEffect InPlaceEffect = new(s => {
    s.HasGravity = false;
    s.CanMove = false;
    s.CanRotate = false;
  }, "InPlace");

  public override async Task MainAction(TaskScope scope) {
    Animation = AnimationDriver.Play(scope, AttackAnimation);
    HitConfig hitConfig = HitConfig;
    using var effect = InPlace ? Status.Add(InPlaceEffect) : null;
    if (Chargeable) {
      var startFrame = Timeval.TickCount;
      await scope.Any(Charge, ListenFor(MainRelease));
      var numFrames = Timeval.TickCount - startFrame;
      var chargeScaling = ChargeScaling.Evaluate((float)numFrames / ChargeEnd.Ticks);
      await scope.Run(Animation.WaitPhase(0));
      hitConfig = hitConfig.Scale(chargeScaling);
    } else {
      await scope.Run(Animation.WaitPhase(0));
    }
    var rotation = AbilityManager.transform.rotation;
    var vfxOrigin = AbilityManager.transform.TransformPoint(AttackVFXOffset);
    SFXManager.Instance.TryPlayOneShot(AttackSFX);
    VFXManager.Instance.TrySpawn2DEffect(AttackVFX, vfxOrigin, rotation);
    await scope.Any(Animation.WaitPhase(1), HitHandler.Loop(Hitbox, Parrybox, new HitParams(HitConfig, Attributes), OnHit));
    await scope.Run(Animation.WaitPhase(2));
    if (RecoveryCancelable)
      Tags.AddFlags(AbilityTag.Cancellable);
    await scope.Run(Animation.WaitDone);
  }

  void OnHit(Hurtbox target) {
    AbilityManager.Energy?.Value.Add(1);
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
}