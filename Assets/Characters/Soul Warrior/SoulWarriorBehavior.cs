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

  async Task Aim(TaskScope scope) {
    Mover.SetAim((Target.position-transform.position).normalized);
    await scope.Tick();
  }

  async Task Pursue(TaskScope scope) {
    Mover.Move(Time.fixedDeltaTime * NavMeshAgent.velocity);
    Mover.SetAim((Target.position-transform.position).normalized);
    await scope.Tick();
  }

  async Task DiveOn(TaskScope scope) {
    await scope.Until(() => AbilityManager.CanInvoke(Dive.MainAction));
    await AbilityManager.TryRun(scope, Dive.MainAction);
  }

  TaskFunc TeleportTo(Vector3 destination) => async scope => {
    Teleport.Destination = destination;
    await scope.Until(() => AbilityManager.CanInvoke(Teleport.MainAction));
    await AbilityManager.TryRun(scope, Teleport.MainAction);
    NavMeshAgent.Warp(transform.position);
  };

  Vector3 DesiredPosition() {
    return Target.position + DesiredDistance*Target.forward;
  }

  async Task Behavior(TaskScope scope) {
    Target = FindObjectOfType<Player>().transform;
    NavMeshAgent.nextPosition = transform.position;
    NavMeshAgent.destination = DesiredPosition();
    NavStatus = NavMeshAgent.NavigationStatus();
    if (!Status.IsGrounded && !PhysicsQuery.GroundCheck(transform.position, out var groundHit, 100)) {
      await scope.Run(TeleportTo(SpawnOrigin+DiveHeight*Vector3.up));
      await scope.Tick();
      await scope.Run(DiveOn);
    } else if (Status.IsGrounded && NavStatus == NavStatus.HasLink && Target.OnNavMesh()) {
      await scope.Run(TeleportTo(Target.position+DiveHeight*Vector3.up));
      await scope.Run(DiveOn);
    } else if (Status.IsGrounded && NavStatus == NavStatus.HasLink && !Target.OnNavMesh()) {
      await scope.Run(Pursue);
    } else if (Status.IsGrounded && NavStatus == NavStatus.DirectPath) {
      await scope.Run(Pursue);
    } else if (Status.IsGrounded && NavStatus == NavStatus.OnLink && !Target.OnNavMesh()) {
      await scope.Run(TeleportTo(NavMeshAgent.currentOffMeshLinkData.endPos+NavMeshAgent.baseOffset*Vector3.up));
    } else if (Status.IsGrounded && NavStatus == NavStatus.OnLink && Target.OnNavMesh()) {
      await scope.Run(TeleportTo(Target.position+DiveHeight*Vector3.up));
      await scope.Run(DiveOn);
    } else {
      await scope.Run(Aim);
    }
  }
}