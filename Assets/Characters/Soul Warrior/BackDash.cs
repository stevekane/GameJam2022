using System.Threading.Tasks;
using UnityEngine;

public class BackDash : Ability {
  [SerializeField] float Distance = 5f;
  [SerializeField] AnimationJobConfig Animation;

  public static InlineEffect ScriptedMove => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.IsDamageable = false;
    s.IsHittable = false;
  }, "DashMove");

  public override async Task MainAction(TaskScope scope) {
    using var scriptedMove = Status.Add(ScriptedMove);
    var animation = AnimationDriver.Play(scope, Animation);
    await scope.Any(animation.WaitDone, Waiter.Repeat(Move));
  }

  async Task Move(TaskScope scope) {
    var speed = Distance / Timeval.FromSeconds(Animation.Clip.length).Ticks;
    var inPlane = speed * -transform.forward.XZ();
    Mover.Move(inPlane);
    await scope.Tick();
  }
}