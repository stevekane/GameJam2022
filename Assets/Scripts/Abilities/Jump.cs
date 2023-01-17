using System.Threading.Tasks;
using UnityEngine;

public class Jump : Ability {
  public float Speed = 60f;
  public float Drag = 5f;
  public Timeval MinDuration = Timeval.FromSeconds(.1f);
  public Timeval MaxDuration = Timeval.FromSeconds(.5f);
  public AnimationJobConfig Animation;
  public AudioClip LaunchSFX;
  public GameObject LaunchVFX;

  // Button press/release.
  bool Holding = true;
  int AirJumpsRemaining = 1;

  public override bool CanStart(AbilityMethod func) => Status.IsGrounded || AirJumpsRemaining > 0;

  public override async Task MainAction(TaskScope scope) {
    try {
      Holding = true;

      if (Status.IsGrounded) {
        await AnimationDriver.Play(scope, Animation).WaitDone(scope);
      } else {
        // TOOD: play an aerial variant of the windup animation here
      }
      SFXManager.Instance.TryPlayOneShot(LaunchSFX);
      VFXManager.Instance.TrySpawnEffect(LaunchVFX, transform.position, transform.rotation);
      var velocity = Speed * Vector3.up;
      using var effect = Status.Add(new InlineEffect(s => {
        s.HasGravity = false;
        velocity = velocity * Mathf.Exp(-Time.fixedDeltaTime * Drag);
        Mover.Move(Time.fixedDeltaTime * velocity);
      }, "Jumping"));

      AirJumpsRemaining--;
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

  void FixedUpdate() {
    if (Status.IsGrounded)
      AirJumpsRemaining = 1;
  }
}