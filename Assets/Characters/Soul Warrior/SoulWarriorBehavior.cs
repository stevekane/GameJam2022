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
  [SerializeField] float OutOfRangeDistance = 6f;
  [SerializeField] float DiveHeight = 15f;
  [SerializeField] float MaxDiveHeight = 100f;

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

  void TrySetTeleportBehindTarget() {
    if (Target) {
      var delta = (Target.position-transform.position).XZ();
      var toTarget = delta.normalized;
      Teleport.Destination = Target.position + DiveHeight * toTarget;
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
        if (distanceToPlayer < OutOfRangeDistance) {
          var diceRoll = Random.Range(0f, 1f);
          if (diceRoll < .5f) {
            await AbilityManager.TryRun(scope, BackDash.MainAction);
          } else {
            await scope.Any(
              Waiter.Repeat(TryLookAtTarget),
              Waiter.Repeat(TryMoveTowardsTarget),
              AbilityManager.TryRun(HardAttack.MainAction));
          }
        } else {
          var diceRoll = Random.Range(0f, 1f);
          if (diceRoll < .01f) {
            await scope.Any(
              Waiter.Repeat(TrySetTeleportOverTarget),
              AbilityManager.TryRun(Teleport.MainAction));
          } else if (diceRoll < .02f) {
            await scope.Any(
              Waiter.Repeat(TrySetTeleportBehindTarget),
              AbilityManager.TryRun(Teleport.MainAction));
          } else if (diceRoll < .03f) {
            Throw.OnThrow = SetHomingSphereHitParams;
            await scope.Any(
              Waiter.Repeat(TryLookAtTarget),
              AbilityManager.TryRun(Throw.MainAction));
          } else {
            TryLookAtTarget();
            TryMoveTowardsTarget();
          }
        }
      } else {
        if (PhysicsQuery.GroundCheck(transform.position, MaxDiveHeight)) {
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