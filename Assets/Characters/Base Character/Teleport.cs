using System.Threading.Tasks;
using UnityEngine;

public class Teleport : Ability {
  [SerializeField] Timeval Windup = Timeval.FromMillis(500);

  public Vector3 Destination { get; set; }

  static InlineEffect TeleportEffect => new(s => {
    s.CanMove = false;
    s.CanRotate = false;
    s.CanAttack = false;
  }, "Teleport Windup");

  public override async Task MainAction(TaskScope scope) {
    using var effect = Status.Add(TeleportEffect);
    await scope.Delay(Windup);
    Mover.Move(Destination-AbilityManager.transform.position);
  }
}