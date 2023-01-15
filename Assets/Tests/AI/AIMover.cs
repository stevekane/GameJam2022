using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.XR;

public class AIMover : MonoBehaviour {
  AbilityManager AbilityManager;
  Mover Mover;
  Status Status;
  NavMeshAgent NavMeshAgent;
  Jump Jump;

  void Awake() {
    this.InitComponent(out AbilityManager);
    this.InitComponent(out Mover);
    this.InitComponent(out Status);
    this.InitComponent(out NavMeshAgent);
    this.InitComponentFromChildren(out Jump, true);
    NavMeshAgent.updatePosition = false;
    NavMeshAgent.updateRotation = false;
  }

  bool TraversingLink = false;
  bool WasGrounded = true;
  public bool UseDesiredVelocity = false;
  Vector3 OffMeshVelocity;
  public void SetMoveFromNavMeshAgent() {
    if (NavMeshAgent.isOnOffMeshLink) {
      Mover.SetMove(OffMeshVelocity);
      return;
    }
    if (UseDesiredVelocity) {
      Mover.SetMove(NavMeshAgent.desiredVelocity.normalized);
    } else {
      Mover.SetMove(NavMeshAgent.velocity.normalized);
    }
  }

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
    } else {
      // Stand here and be sad.
      NavMeshAgent.Warp(transform.position);
      await scope.Tick();
    }
    TraversingLink = false;
    NavMeshAgent.CompleteOffMeshLink();
  }

  async Task FallOffLink(TaskScope scope, Vector3 dest) {
    var dir = transform.position.TryGetDirection(dest) ?? OffMeshVelocity;
    OffMeshVelocity = dir.XZ().normalized;
    await scope.Until(() => Status.IsGrounded);
  }

  async Task JumpOffLink(TaskScope scope, Vector3 dest) {
    OffMeshVelocity = Vector3.zero;
    await scope.Any(
      async s => {
        await AbilityManager.TryRun(s, Jump.MainAction);
        await AbilityManager.TryRun(s, Jump.MainAction);
        await s.Until(() => Status.IsGrounded);
      },
      Waiter.Repeat(() => {
        var dir = transform.position.TryGetDirection(dest) ?? OffMeshVelocity;
        OffMeshVelocity = dir.XZ().normalized;
      }));
  }

  void OnDrawGizmos() {
    if (NavMeshAgent != null) {
      Gizmos.color = NavMeshAgent.isOnOffMeshLink ? Color.green : Color.red;
      Gizmos.DrawRay(transform.position + 4 * Vector3.up, 4 * Vector3.down);
      Gizmos.DrawRay(transform.position + 3 * Vector3.up, NavMeshAgent.desiredVelocity);
      Gizmos.color = Color.blue;
      Gizmos.DrawRay(transform.position + 4 * Vector3.up, NavMeshAgent.velocity);
    }
  }
}