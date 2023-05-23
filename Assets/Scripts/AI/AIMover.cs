using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

public class AIMover : MonoBehaviour {
  AbilityManager AbilityManager;
  Mover Mover;
  Status Status;
  NavMeshAgent NavMeshAgent;
  Jump Jump;
  Teleport Teleport;

  void Awake() {
    this.InitComponent(out AbilityManager);
    this.InitComponent(out Mover);
    this.InitComponent(out Status);
    this.InitComponent(out NavMeshAgent);
    this.InitComponentFromChildren(out Jump, true);
    this.InitComponentFromChildren(out Teleport, true);
    NavMeshAgent.updatePosition = false;
    NavMeshAgent.updateRotation = false;
  }

  bool TraversingLink = false;
  bool WasGrounded = true;
  public bool UseDesiredVelocity = false;
  Vector3 OffMeshMoveDir;
  Vector3 AgentMoveDir =>
    NavMeshAgent.isOnOffMeshLink ? OffMeshMoveDir :
    UseDesiredVelocity ? NavMeshAgent.desiredVelocity.normalized :
    NavMeshAgent.velocity.normalized;

  public void SetMoveFromNavMeshAgent() => Mover.SetMove(AgentMoveDir);
  public void SetAimFromNavMeshAgent() => Mover.SetAim(AgentMoveDir);
  public void SetMove(Vector3 dir) => Mover.SetMove(dir);
  public void SetAim(Vector3 dir) => Mover.SetAim(dir);
  public void SetDestination(Vector3 pos) => NavMeshAgent.SetDestination(pos);

  void FixedUpdate() {
    // Hacks to update navmesh agent state to behave correctly.
    if (!WasGrounded && Status.IsGrounded)
      NavMeshAgent.Warp(transform.position);
    WasGrounded = Status.IsGrounded;

    if (NavMeshAgent.isOnOffMeshLink && Status.CanMove) {
      if (!TraversingLink) {
        TraversingLink = true;
        AbilityManager.MainScope.Start(TraverseOffLink);
      }
    } else {
      NavMeshAgent.nextPosition = transform.position;
    }
  }

  async Task TraverseOffLink(TaskScope scope) {
    var linkData = NavMeshAgent.currentOffMeshLinkData;
    var toStart = Vector3.Distance(transform.position, linkData.startPos);
    var toEnd = Vector3.Distance(transform.position, linkData.endPos);
    var dest = toStart < toEnd ? linkData.endPos : linkData.startPos;
    const float MaxHorizontalFall = 5f;

    if (dest.y < transform.position.y && transform.position.XZ().SqrDistance(dest.XZ()) < MaxHorizontalFall*MaxHorizontalFall) {
      await FallOffLink(scope, dest);
    } else if (Jump) {
      await JumpOffLink(scope, dest);
    } else if (Teleport) {
      await TeleportOffLink(scope, dest);
    } else {
      // Stand here and be sad.
      NavMeshAgent.Warp(transform.position);
      await scope.Tick();
    }
    TraversingLink = false;
    NavMeshAgent.CompleteOffMeshLink();
  }

  async Task FallOffLink(TaskScope scope, Vector3 dest) {
    var dir = transform.position.TryGetDirection(dest) ?? OffMeshMoveDir;
    OffMeshMoveDir = dir.XZ().normalized;
    await scope.Until(() => Status.IsGrounded);
  }

  async Task JumpOffLink(TaskScope scope, Vector3 dest) {
    OffMeshMoveDir = Vector3.zero;
    await scope.Any(
      async s => {
        await AbilityManager.TryRun(s, Jump.MainAction);
        await AbilityManager.TryRun(s, Jump.MainAction);
        await s.Until(() => Status.IsGrounded);
      },
      Waiter.Repeat(() => {
        var dir = transform.position.TryGetDirection(dest) ?? OffMeshMoveDir;
        OffMeshMoveDir = dir.XZ().normalized;
      }));
  }

  async Task TeleportOffLink(TaskScope scope, Vector3 dest) {
    OffMeshMoveDir = Vector3.zero;
    Teleport.Destination = dest;
    await scope.Until(() => AbilityManager.CanInvoke(Teleport.MainAction));
    await AbilityManager.TryRun(scope, Teleport.MainAction);
  }

  //void OnDrawGizmos() {
  //  if (NavMeshAgent != null) {
  //    Gizmos.color = NavMeshAgent.isOnOffMeshLink ? Color.green : Color.red;
  //    Gizmos.DrawRay(transform.position + 4 * Vector3.up, 4 * Vector3.down);
  //    Gizmos.DrawRay(transform.position + 3 * Vector3.up, NavMeshAgent.desiredVelocity);
  //    Gizmos.color = Color.blue;
  //    Gizmos.DrawRay(transform.position + 4 * Vector3.up, NavMeshAgent.velocity);
  //  }
  //}
}