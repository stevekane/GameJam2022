using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class SoulWarriorBehavior : MonoBehaviour {
  [SerializeField] NavMeshAgent NavMeshAgent;
  [SerializeField] AttackAbility HardAttack;
  [SerializeField] Throw Throw;
  [SerializeField] Teleport Teleport;
  [SerializeField] Dive Dive;
  [SerializeField] float DesiredDistance = 1f;
  [SerializeField] float OutOfRangeDistance = 6f;
  [SerializeField] float DiveHeight = 15f;
  [SerializeField] float MinDiveHeight = 5f;
  [SerializeField] float MaxDiveHeight = 100f;

  Vector3 SpawnOrigin;
  Mover Mover;
  Status Status;
  AbilityManager AbilityManager;
  Transform Target;
  TaskScope MainScope = new();

  void Awake() {
    NavMeshAgent.updatePosition = false;
    NavMeshAgent.updateRotation = false;
    Mover = GetComponent<Mover>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }
  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnEnable() => SpawnOrigin = transform.position;
  void OnDestroy() => MainScope.Dispose();

  void TryMoveTowardsTarget() {
    var delta = (Target.position-transform.position).XZ();
    var toTarget = delta.normalized;
    NavMeshAgent.nextPosition = transform.position;
    if (NavMesh.SamplePosition(Target.position, out var hit, 1, NavMesh.AllAreas)) {
      NavMeshAgent.destination = hit.position;
    }
    Mover.Move(Time.fixedDeltaTime * NavMeshAgent.velocity);
    Mover.SetAim(toTarget);
  }

  async Task TeleportTo(TaskScope scope, Vector3 destination) {
    Teleport.Destination = destination;
    await scope.Until(() => AbilityManager.CanInvoke(Teleport.MainAction));
    await AbilityManager.TryRun(scope, Teleport.MainAction);
    NavMeshAgent.Warp(transform.position);
  }

  bool WasGrounded = true;
  async Task Behavior(TaskScope scope) {
    Mover.SetMove(Vector3.zero);
    if (!Target) {
      Target = FindObjectOfType<Player>().transform;
    } else {
      // Hacks to update navmesh agent state to behave correctly.
      if (!WasGrounded && Status.IsGrounded)
        NavMeshAgent.Warp(transform.position);
      WasGrounded = Status.IsGrounded;

      var delta = (Target.position-transform.position).XZ();
      var toPlayer = delta.normalized;
      var distanceToPlayer = delta.magnitude;
      if (Status.IsGrounded) {
        if (NavMeshAgent.isOnOffMeshLink && NavMeshAgent.currentOffMeshLinkData.valid) {
          var linkData = NavMeshAgent.currentOffMeshLinkData;
          var toStart = Vector3.Distance(transform.position, linkData.startPos);
          var toEnd = Vector3.Distance(transform.position, linkData.endPos);
          var dest = toStart < toEnd ? linkData.endPos : linkData.startPos;
          await TeleportTo(scope, dest + 3 * Vector3.up);
          NavMeshAgent.CompleteOffMeshLink();
        }
        TryMoveTowardsTarget();
      }
      // if (Status.IsGrounded) {
      //   if (NavMeshAgent.currentOffMeshLinkData.valid) {
      //     var linkData = NavMeshAgent.currentOffMeshLinkData;
      //     var toStart = Vector3.Distance(transform.position, linkData.startPos);
      //     var toEnd = Vector3.Distance(transform.position, linkData.endPos);
      //     var dest = toStart < toEnd ? linkData.endPos : linkData.startPos;
      //     Debug.Log($"Teleporting to CURRENT {dest}");
      //     Teleport.Destination = dest + 2 * Vector3.up;
      //     await scope.Until(() => AbilityManager.CanInvoke(Teleport.MainAction));
      //     await AbilityManager.TryRun(scope, Teleport.MainAction);
      //     NavMeshAgent.Warp(transform.position);
      //   } else if (NavMeshAgent.nextOffMeshLinkData.valid) {
      //     var linkData = NavMeshAgent.nextOffMeshLinkData;
      //     var toStart = Vector3.Distance(transform.position, linkData.startPos);
      //     var toEnd = Vector3.Distance(transform.position, linkData.endPos);
      //     var dest = toStart < toEnd ? linkData.endPos : linkData.startPos;
      //     Debug.Log($"Teleporting to NEXT {dest}");
      //     Teleport.Destination = dest + 2 * Vector3.up;
      //     await scope.Until(() => AbilityManager.CanInvoke(Teleport.MainAction));
      //     await AbilityManager.TryRun(scope, Teleport.MainAction);
      //     NavMeshAgent.Warp(transform.position);
      //   // } else if (distanceToPlayer < DesiredDistance) {
      //     // await scope.Any(
      //       // Waiter.Repeat(TryMoveTowardsTarget),
      //       // AbilityManager.TryRun(HardAttack.MainAction));
      //   } else {
      //     TryMoveTowardsTarget();
      //     // await scope.Any(
      //     //   Waiter.Repeat(TryMoveTowardsTarget),
      //     //   AbilityManager.TryRun(Throw.MainAction));
      //   }
      // } else {
      //   if (PhysicsQuery.GroundCheck(transform.position, out var hit, MaxDiveHeight)) {
      //     if (hit.distance > MinDiveHeight) {
      //       await AbilityManager.TryRun(scope, Dive.MainAction);
      //     }
      //   } else {
      //     Teleport.Destination = SpawnOrigin + DiveHeight * Vector3.up;
      //     await scope.Until(() => AbilityManager.CanInvoke(Teleport.MainAction));
      //     await AbilityManager.TryRun(scope, Teleport.MainAction);
      //   }
      // }
    }
    await scope.Tick();
  }
}