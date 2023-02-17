using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public enum NavStatus { OffMesh, DirectPath, OnLink, HasLink }

public static class NavigationExtensions {
  public static NavStatus NavigationStatus(this NavMeshAgent agent) => agent switch {
    { isOnNavMesh : false } => NavStatus.OffMesh,
    { isOnNavMesh : true, isOnOffMeshLink : true, currentOffMeshLinkData : { valid: true } } => NavStatus.OnLink,
    { isOnNavMesh : true, nextOffMeshLinkData : { valid : true } } => NavStatus.HasLink,
    _ => NavStatus.DirectPath
  };

  public static bool OnNavMesh(this Transform transform, out NavMeshHit hit, float maxDistance = 1) {
    return NavMesh.SamplePosition(transform.position, out hit, maxDistance, NavMesh.AllAreas);
  }

  public static bool OnNavMesh(this Transform transform, float maxDistance = 1) {
    return NavMesh.SamplePosition(transform.position, out var hit, maxDistance, NavMesh.AllAreas);
  }
}

public class SoulWarriorBehavior : MonoBehaviour {
  [SerializeField] NavMeshAgent NavMeshAgent;
  [SerializeField] AttackAbility HardAttack;
  [SerializeField] Throw Throw;
  [SerializeField] Teleport Teleport;
  [SerializeField] Dive Dive;
  [SerializeField] float DesiredDistance = 1f;
  [SerializeField] float DiveHeight = 15f;
  [SerializeField] float MinDiveHeight = 5f;
  [SerializeField] float MaxDiveRadius = 10f;

  Vector3 SpawnOrigin;
  Mover Mover;
  Status Status;
  AbilityManager AbilityManager;
  Transform Target;
  TaskScope MainScope = new();
  public NavStatus NavStatus;

  void Awake() {
    SpawnOrigin = transform.position;
    NavMeshAgent.updatePosition = false;
    NavMeshAgent.updateRotation = false;
    Mover = GetComponent<Mover>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }
  void Start() => MainScope.Start(Waiter.Repeat(Behavior));
  void OnEnable() => SpawnOrigin = transform.position;
  void OnDestroy() => MainScope.Dispose();
  void OnLand() => NavMeshAgent.Warp(transform.position);

  void Aim() {
    Mover.SetAim((Target.position-transform.position).normalized);
  }

  void Pursue() {
    Mover.Move(Time.fixedDeltaTime * NavMeshAgent.velocity);
    Mover.SetAim((Target.position-transform.position).normalized);
  }

  async Task DiveOn(TaskScope scope) {
    await AbilityManager.TryRun(scope, Dive.MainAction);
  }

  TaskFunc TeleportTo(Vector3 destination) => async scope => {
    Teleport.Destination = destination;
    await AbilityManager.TryRun(scope, Teleport.MainAction);
  };

  Vector3 DesiredPosition() {
    return Target.position + DesiredDistance*Target.forward;
  }

  async Task Behavior(TaskScope scope) {
    Target = FindObjectOfType<Player>().transform;
    NavMeshAgent.nextPosition = transform.position;
    NavMeshAgent.destination = DesiredPosition();
    NavStatus = NavMeshAgent.NavigationStatus();
    var grounded = Status.IsGrounded;
    var overGround = PhysicsQuery.GroundCheck(transform.position, out var groundHit, 1000);
    var toTargetXZ = Vector3.Distance(Target.position.XZ(), transform.position.XZ());
    var targetOnNavMesh = Target.OnNavMesh();
    if (!grounded && !overGround) {
      await scope.Run(TeleportTo(SpawnOrigin+DiveHeight*Vector3.up));
    } else if (!grounded && overGround && toTargetXZ < MaxDiveRadius && groundHit.distance > MinDiveHeight) {
      await scope.Run(DiveOn);
    } else if (grounded && targetOnNavMesh && NavStatus == NavStatus.HasLink) {
      await scope.Run(TeleportTo(Target.position+DiveHeight*Vector3.up));
    } else if (grounded && targetOnNavMesh && NavStatus == NavStatus.OnLink) {
      await scope.Run(TeleportTo(Target.position+DiveHeight*Vector3.up));
    } else if (grounded && !targetOnNavMesh && NavStatus == NavStatus.HasLink) {
      Pursue();
    } else if (grounded && NavStatus == NavStatus.DirectPath) {
      Pursue();
    } else {
      Aim();
    }
    await scope.Tick();
  }
}