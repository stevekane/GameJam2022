using System.Threading.Tasks;
using UnityEngine;

public class Jump : Ability {
  public float Speed = 60f;
  public float Drag = 5f;
  public Timeval MinDuration = Timeval.FromSeconds(.1f);
  public Timeval MaxDuration = Timeval.FromSeconds(.5f);
  public AnimationClip WindupClip;

  // Button press/release.
  bool Holding = true;
  int JumpsRemaining = 2;
  public override async Task MainAction(TaskScope scope) {
    try {
      Holding = true;

      if (JumpsRemaining <= 0)
        return;

      //await AnimationDriver.Play(scope, WindupClip).WaitDone(scope);
      await scope.Ticks(3); // anim placeholder

      var velocity = Speed * Vector3.up;
      using var effect = Status.Add(new InlineEffect(s => {
        s.HasGravity = false;
        velocity = velocity * Mathf.Exp(-Time.fixedDeltaTime * Drag);
        Mover.Move(Time.fixedDeltaTime * velocity);
      }, "Jumping"));

      // Double-jump.
      JumpsRemaining--;
      // Reset jumps when grounded.
      AbilityManager.MainScope.Start(async s => {
        if (Status.IsGrounded)  // May not have started jumping before this runs.
          await s.Until(() => !Status.IsGrounded);
        await s.Until(() => Status.IsGrounded);
        JumpsRemaining = 2;
      });

      await scope.Any(Waiter.All(Waiter.While(() => Holding), Waiter.Delay(MinDuration)), Waiter.Delay(MaxDuration));
    } finally {
    }
  }

  public override Task MainRelease(TaskScope scope) {
    Holding = false;
    return null;
  }
}