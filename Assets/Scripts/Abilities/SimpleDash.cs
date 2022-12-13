using System;
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

  public static InlineEffect ScriptedMove => new(s => {
    s.HasGravity = false;
    s.AddAttributeModifier(AttributeTag.MoveSpeed, AttributeModifier.TimesZero);
    s.AddAttributeModifier(AttributeTag.TurnSpeed, AttributeModifier.TimesZero);
  }, "DashMove");
  public static InlineEffect Invulnerable => new(s => {
    s.IsDamageable = false;
    s.IsHittable = false;
  }, "DashInvulnerable");

  // Button press/release.
  public async Task Activate(TaskScope scope) {
    try {
      var dir = AbilityManager.GetAxis(AxisTag.Move).XZ;
      if (dir == Vector3.zero) {
        dir = AbilityManager.transform.forward;
      }
      using var moveEffect = Status.Add(ScriptedMove);
      await AnimationDriver.Play(scope, DashWindupClip).WaitDone(scope);
      using var invulnEffect = Status.Add(Invulnerable);
      AnimationDriver.Play(scope, DashingClip); // don't wait
      await scope.Any(
        s => s.Delay(DashDuration),
        s => Move(s, dir.normalized),
        s => MakeCancellable(s));
    } finally {
      // TODO: is this needed?
      //await AnimationDriver.Play(new(), DoneClip).WaitDone(scope);
    }
  }

  async Task Move(TaskScope scope, Vector3 dir) {
    while (true) {
      var desiredDir = AbilityManager.GetAxis(AxisTag.Move).XZ;
      var desiredSpeed = Mathf.SmoothStep(MinMoveSpeed, MaxMoveSpeed, desiredDir.magnitude);
      var targetDir = desiredDir.TryGetDirection() ?? dir;
      dir = Vector3.RotateTowards(dir, targetDir.normalized, TurnSpeed/360f, 0f);
      Status.transform.forward = dir;
      Mover.Move(desiredSpeed * Time.fixedDeltaTime * dir);
      await scope.Tick();
    }
  }

  async Task MakeCancellable(TaskScope scope) {
    await scope.Millis((int)(DashDuration.Millis / 3));
    Tags.AddFlags(AbilityTag.Cancellable);
    await scope.Forever();
  }

  public override void Awake() {
    base.Awake();
  }
}