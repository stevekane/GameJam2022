using UnityEngine;
using UnityEngine.AI;

namespace Archero {
  public class PathMove : SimpleAbility {
    [SerializeField] AI AI;
    [SerializeField] NavMeshAgent NavMeshAgent;

    public AbilityAction<Vector3> Move;

    void Start() {
      Move.Listen(OnMove);
    }

    void OnMove(Vector3 v) {
      if (NavMeshAgent) {
        NavMeshAgent.SetDestination(v);
        AI.Velocity = NavMeshAgent.desiredVelocity;
      } else {
        AI.Velocity = Vector3.zero;
      }
    }
  }
}