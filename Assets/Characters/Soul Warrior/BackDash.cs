using System.Threading.Tasks;
using UnityEngine;

public class BackDash : Ability {
  [SerializeField] float Distance = 5f;
  [SerializeField] Timeval Duration = Timeval.FromSeconds(.5f);
  [SerializeField] AnimationJobConfig Animation;

  public static InlineEffect ScriptedMove => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.IsDamageable = false;
    s.IsHittable = false;
  }, "DashMove");

  public override async Task MainAction(TaskScope scope) {
    try {
      using var scriptedMove = Status.Add(ScriptedMove);
      AnimationDriver.Play(scope, Animation);
      await scope.Any(
        s => s.Delay(Duration),
        s => s.Repeat(Move)
      );
    } finally {
    }
  }

  async Task Move(TaskScope scope) {
    var speed = Distance / (float)Duration.Ticks;
    var inPlane = speed * -transform.forward.XZ();
    Mover.Move(inPlane);
    await scope.Tick();
  }
}