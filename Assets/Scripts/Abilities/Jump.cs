using System.Threading.Tasks;
using UnityEngine;

public class Jump : Ability {
  public float Speed = 60f;
  public float WallJumpHorizontalSpeed = 30f;
  public float Drag = 5f;
  public Timeval MinDuration = Timeval.FromSeconds(.1f);
  public Timeval MaxDuration = Timeval.FromSeconds(.5f);
  public AnimationJobConfig Animation;
  public AnimationJobConfig WallJumpAnimation;
  public AudioClip LaunchSFX;
  public GameObject LaunchVFX;
  public Timeval CoyoteTime = Timeval.FromTicks(6);

  bool Holding = true;
  int AirJumpsRemaining = 1;
  // Coyote-time: We stay "grounded" for N ticks after falling off a ledge. Jumping makes us immediately not grounded.
  int TicksSinceGrounded = 0, TicksSinceJump = 0;
  bool IsConsideredGrounded => TicksSinceGrounded <= CoyoteTime.Ticks && TicksSinceJump > TicksSinceGrounded;

  public override bool CanStart(AbilityMethod func) =>
    func == MainRelease ? true :
    IsConsideredGrounded || Status.IsWallSliding || AirJumpsRemaining > 0;

  public override async Task MainAction(TaskScope scope) {
    try {
      Holding = true;

      var velocity = Speed * Vector3.up;
      if (IsConsideredGrounded) {
        await AnimationDriver.Play(scope, Animation).WaitDone(scope);
      } else if (Status.IsWallSliding) {
        await AnimationDriver.Play(scope, WallJumpAnimation).WaitDone(scope);
        velocity += -transform.forward * WallJumpHorizontalSpeed;
      } else {
        // TOOD: play an aerial variant of the windup animation here
      }
      SFXManager.Instance.TryPlayOneShot(LaunchSFX);
      VFXManager.Instance.TrySpawnEffect(LaunchVFX, transform.position, transform.rotation);
      using var effect = Status.Add(new InlineEffect(s => {
        s.HasGravity = false;
        velocity = velocity * Mathf.Exp(-Time.fixedDeltaTime * Drag);
        Mover.Move(Time.fixedDeltaTime * velocity);
      }, "Jumping"));
      if (!IsConsideredGrounded)
        AirJumpsRemaining--;
      TicksSinceJump = -1; // Set to 0 next FixedUpdate

      await scope.Any(
        Waiter.All(
          Waiter.While(() => Holding),
          Waiter.Delay(MinDuration)),
        Waiter.Delay(MaxDuration));
    } finally {
    }
  }

  public override Task MainRelease(TaskScope scope) {
    Holding = false;
    return null;
  }

  protected override void FixedUpdate() {
    base.FixedUpdate();
    if (Status.IsGrounded) {
      AirJumpsRemaining = 1;
      TicksSinceGrounded = 0;
    } else {
      TicksSinceGrounded++;
    }
    TicksSinceJump++;
  }
}