using System;
using System.Threading.Tasks;
using UnityEngine;

public class SwipeDash : Ability {
  public float MoveSpeed = 30f;
  public Timeval DashDuration = Timeval.FromMillis(300);
  public HitConfig HitConfig;
  public AnimationJobConfig Animation;
  public AudioClip SFX;
  public GameObject VFX;
  public Vector3 VFXOffset;
  public TriggerEvent Hitbox;

  public Action<Hurtbox> OnHit { get; set; }
  public Transform Target { get; set; }

  public static InlineEffect ScriptedMove => new(s => {
    s.HasGravity = false;
    s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
    s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.TimesZero);
  }, "SwipeDashMove");

  public override async Task MainAction(TaskScope scope) {
    try {
      var dir = AbilityManager.transform.position.TryGetDirection(Target.position) ?? AbilityManager.transform.forward;
      using var moveEffect = Status.Add(ScriptedMove);
      SFXManager.Instance.TryPlayOneShot(SFX);
      VFXManager.Instance.TrySpawnEffect(VFX, transform.position + VFXOffset, transform.rotation);
      AnimationDriver.Play(scope, Animation);
      await scope.Any(
        HitHandler.Loop(Hitbox, new HitParams(HitConfig, Attributes), OnHit),
        Waiter.Delay(DashDuration),
        Waiter.Repeat(Move(dir.normalized)));
    } finally {
    }
  }

  TaskFunc Move(Vector3 dir) => async (TaskScope scope) => {
    Status.transform.forward = dir;
    Mover.Move(MoveSpeed * Time.fixedDeltaTime * dir);
    await scope.Tick();
  };
}