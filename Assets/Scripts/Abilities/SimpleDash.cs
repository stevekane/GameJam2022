using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SimpleDash : Ability {
  public float MaxMoveSpeed = 120f;
  public float MinMoveSpeed = 60f;
  public float TurnSpeed = 60f;
  public Timeval DashDuration = Timeval.FromSeconds(.3f);
  public AnimationClip DashWindupClip;
  public AnimationClip DashingClip;
  public AnimationClip DoneClip;
  public Animator Animator;

  public static InlineEffect ScriptedMove { get => new(s => {
      s.HasGravity = false;
      s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
      s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.TimesZero);
    }, "DashMove");
  }
  public static InlineEffect Invulnerable { get => new(s => {
      s.IsDamageable = false;
      s.IsHittable = false;
    }, "DashInvulnerable");
  }

  /*
  public override void OnStop() {
    // TODO: gross
    AbilityManager.Bundle.StartRoutine(Animator.Run(DoneClip));
  }
  */

  // Button press/release.
  public IEnumerator Activate() => RunTask(ActivateTask);
  async Task ActivateTask(TaskScope scope) {
    var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    if (dir == Vector3.zero) {
      dir = AbilityManager.transform.forward;
    }
    AddStatusEffect(ScriptedMove);
    await scope.RunFiber(Animator.Run(DashWindupClip));
    AddStatusEffect(Invulnerable);
    await scope.Any(s => s.Delay(DashDuration), s => s.RunFiber(Animator.Run(DashingClip)), s => Move(s, dir.normalized));
    await scope.RunFiber(Animator.Run(DoneClip));
  }

  async Task Move(TaskScope scope, Vector3 dir) {
    while (true) {
      var desiredDir = AbilityManager.GetAxis(AxisTag.Move).XZ;
      var desiredSpeed = Mathf.SmoothStep(MinMoveSpeed, MaxMoveSpeed, desiredDir.magnitude);
      var targetDir = desiredDir.TryGetDirection() ?? dir;
      dir = Vector3.RotateTowards(dir, targetDir.normalized, TurnSpeed/360f, 0f);
      Status.transform.forward = dir;
      Status.Move(desiredSpeed * Time.fixedDeltaTime * dir);
      await scope.Tick();
    }
  }

  public override void Awake() {
    base.Awake();
  }
}