using System.Threading.Tasks;
using UnityEngine;

public class SoulWarriorBehavior : MonoBehaviour {
  [SerializeField] AttackAbility HardAttack;
  [SerializeField] Throw Throw;
  [SerializeField] HitConfig ThrowHitConfig;
  [SerializeField] Teleport Teleport;
  [SerializeField] BackDash BackDash;
  [SerializeField] Dive Dive;
  [SerializeField] float DesiredDistance = 1f;
  [SerializeField] float DangerDistance = 3f;
  [SerializeField] float OutOfRangeDistance = 6f;
  [SerializeField] float DiveHeight = 15f;

  Mover Mover;
  Status Status;
  Attributes Attributes;
  AbilityManager AbilityManager;
  Transform Target;
  TaskScope MainScope = new();

  public void SetHomingSphereHitParams(GameObject go) {
    if (go.TryGetComponent(out HomingMissile homingMissile)) {
      homingMissile.HitParams = new HitParams(ThrowHitConfig, Attributes.serialized, gameObject);
    }
  }

  void Awake() {
    Mover = GetComponent<Mover>();
    Status = GetComponent<Status>();
    Attributes = GetComponent<Attributes>();
    AbilityManager = GetComponent<AbilityManager>();
  }
  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnDestroy() => MainScope.Dispose();

  void TrySetTeleportOverTarget() {
    if (Target) {
      Teleport.Destination = Target.position + DiveHeight * Vector3.up;
    }
  }

  void TryLookAtTarget() {
    if (Target) {
      var delta = (Target.position-transform.position).XZ();
      var toTarget = delta.normalized;
      var distanceToTarget = delta.magnitude;
      Mover.SetAim(toTarget);
      Mover.SetMove(toTarget);
    }
  }

  void TryMoveTowardsTarget() {
    var delta = (Target.position-transform.position).XZ();
    var toTarget = delta.normalized;
    var distanceToTarget = delta.magnitude;
    // TODO: Smooth this out starting 1 unit away from desired
    // slow down to zero as we get to desired distance
    if (distanceToTarget < DesiredDistance) {
      Mover.SetMove(Vector3.zero);
    } else {
      Mover.SetMove(toTarget);
    }
  }

  async Task Behavior(TaskScope scope) {
    if (!Target) {
      Target = FindObjectOfType<Player>().transform;
    } else {
      var delta = (Target.position-transform.position).XZ();
      var toPlayer = delta.normalized;
      var distanceToPlayer = delta.magnitude;
      if (Status.IsGrounded) {
        if (distanceToPlayer < DangerDistance) {
          await AbilityManager.TryRun(scope, BackDash.MainAction);
        } else if (distanceToPlayer < OutOfRangeDistance) {
          await scope.Any(
            Waiter.Repeat(TryLookAtTarget),
            Waiter.Repeat(TryMoveTowardsTarget),
            s => AbilityManager.TryRun(s, HardAttack.MainAction));
        } else {
          var diceRoll = Random.Range(0f, 1f);
          if (diceRoll < .01f) {
            await scope.Any(
              s => s.Repeat(TrySetTeleportOverTarget),
              s => AbilityManager.TryRun(s, Teleport.MainAction)
            );
          } else if (diceRoll < .02f) {
            Throw.OnThrow = SetHomingSphereHitParams;
            await scope.Any(
              s => s.Repeat(TryLookAtTarget),
              s => AbilityManager.TryRun(s, Throw.MainAction)
            );
          } else {
            TryLookAtTarget();
            TryMoveTowardsTarget();
          }
        }
      } else {
        var groundRay = new Ray(transform.position, Vector3.down);
        var overGround = Physics.Raycast(groundRay.origin, groundRay.direction, 1000, Defaults.Instance.EnvironmentLayerMask);
        if (overGround) {
          await AbilityManager.TryRun(scope, Dive.MainAction);
        } else {
          Teleport.Destination = DiveHeight * Vector3.up;
          await AbilityManager.TryRun(scope, Teleport.MainAction);
        }
      }
    }
    await scope.Tick();
  }
}