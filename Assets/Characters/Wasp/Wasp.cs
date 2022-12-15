using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Wasp : MonoBehaviour {
  public float ShootRadius = 20f;
  public float MoveSpeed = 15f;
  public Timeval ShootDelay = Timeval.FromMillis(1000);
  Status Status;
  Transform Target;
  AbilityManager Abilities;
  Mover Mover;
  PelletAbility Pellet;
  TaskScope MainScope = new();

  public void Awake() {
    Target = GameObject.FindObjectOfType<Player>().transform;
    Status = GetComponent<Status>();
    Mover = GetComponent<Mover>();
    Abilities = GetComponent<AbilityManager>();
    Pellet = GetComponentInChildren<PelletAbility>();
  }
  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnDestroy() => MainScope.Dispose();

  async Task Behavior(TaskScope scope) {
    if (Target == null) {
      MainScope.Cancel();
      return;
    }

    var KiteRadius = ShootRadius - 5f;
    bool TargetInRange(float dist, out Vector3 delta) {
      delta = (Target.transform.position - transform.position);
      return delta.sqrMagnitude < dist*dist;
    }
    async Task Chase(TaskScope scope) {
      while (true) {
        if (TargetInRange(ShootRadius, out var targetDelta) && Status.CanAttack) {
          Mover.UpdateAxes(Abilities, Vector3.zero, transform.forward.XZ());
          await scope.Any(
            s => Abilities.TryRun(s, Pellet.MainAction),
            Waiter.Repeat(() => Mover.TryLookAt(Target)));
          await scope.Delay(ShootDelay);
          return;
        }
        var dir = targetDelta.normalized;
        Mover.UpdateAxes(Abilities, dir, dir);
        await scope.Tick();
      }
    }
    async Task Kite(TaskScope scope) {
      while (true) {
        if (TargetInRange(KiteRadius, out var targetDelta)) {
          var dir = -targetDelta.normalized;
          Mover.UpdateAxes(Abilities, dir, dir);
        } else {
          Mover.UpdateAxes(Abilities, Vector3.zero, transform.forward.XZ());
          return;
        }
        await scope.Tick();
      }
    }

    if (TargetInRange(KiteRadius, out var delta))
      await Kite(scope);
    else
      await Chase(scope);
  }
}
