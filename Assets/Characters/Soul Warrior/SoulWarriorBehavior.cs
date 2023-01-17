using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class SoulWarriorBehavior : MonoBehaviour {
  [SerializeField] AIMover AIMover;
  [SerializeField] NavMeshAgent NavMeshAgent;
  [SerializeField] AttackAbility HardAttack;
  [SerializeField] Throw Throw;
  [SerializeField] Teleport Teleport;
  [SerializeField] Dive Dive;
  [SerializeField] float DesiredDistance = 1f;
  [SerializeField] float OutOfRangeDistance = 6f;
  [SerializeField] float DiveHeight = 15f;
  [SerializeField] float MaxDiveHeight = 100f;

  Vector3 SpawnOrigin;
  Mover Mover;
  Status Status;
  AbilityManager AbilityManager;
  Transform Target;
  TaskScope MainScope = new();

  void Awake() {
    Mover = GetComponent<Mover>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }
  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnEnable() => SpawnOrigin = transform.position;
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
    }
  }

  void TryMoveTowardsTarget() {
    var delta = (Target.position-transform.position).XZ();
    var toTarget = delta.normalized;
    NavMeshAgent.destination = Target.position+Target.forward * DesiredDistance;
    AIMover.SetMoveFromNavMeshAgent();
  }

  async Task Behavior(TaskScope scope) {
    Mover.SetMove(Vector3.zero);
    if (!Target) {
      Target = FindObjectOfType<Player>().transform;
    } else {
      var delta = (Target.position-transform.position).XZ();
      var toPlayer = delta.normalized;
      var distanceToPlayer = delta.magnitude;
      if (Status.IsGrounded) {
        if (distanceToPlayer < OutOfRangeDistance) {
          await scope.Any(
            Waiter.Repeat(TryLookAtTarget),
            Waiter.Repeat(TryMoveTowardsTarget),
            AbilityManager.TryRun(HardAttack.MainAction));
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
          Teleport.Destination = SpawnOrigin + DiveHeight * Vector3.up;
          await AbilityManager.TryRun(scope, Teleport.MainAction);
        }
      }
    }
    await scope.Tick();
  }
}