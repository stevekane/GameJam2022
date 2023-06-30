using UnityEngine;
using UnityEngine.AI;

namespace Archero {
  public class PathMove : SimpleAbility {
    [SerializeField] NavMeshAgent NavMeshAgent;

    public AbilityAction<Vector3> Move;

    void Start() {
      Move.Listen(OnMove);
    }

    void OnMove(Vector3 v) {
      NavMeshAgent.SetDestination(v);
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
      if (NavMeshAgent.desiredVelocity.sqrMagnitude > 0) {
        currentRotation = Quaternion.LookRotation(NavMeshAgent.desiredVelocity.XZ().normalized);
      }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
      currentVelocity = NavMeshAgent.desiredVelocity;
    }
  }
}