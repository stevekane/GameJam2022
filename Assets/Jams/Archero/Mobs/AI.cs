using UnityEngine;
using UnityEngine.AI;

namespace Archero {
  public class AI : MonoBehaviour {
    [SerializeField] NavMeshAgent NavMeshAgent;
    [SerializeField] SimpleAbilityManager AbilityManager;
    [SerializeField] PathMove PathMove;
    [SerializeField] string AreaName = "Walking";

    void Start() {
      NavMeshAgent.updatePosition = false;
      NavMeshAgent.updateRotation = false;
      NavMeshAgent.updateUpAxis = false;
    }

    void FixedUpdate() {
      var targetPosition = Player.Instance.transform.position;
      var areaMask = 1 << NavMesh.GetAreaFromName(AreaName);
      var foundPoint = NavMesh.SamplePosition(targetPosition, out var hit, 1, areaMask);
      PathMove.IsRunning = true;
      AbilityManager.Run(PathMove.Move, hit.position);
    }
  }
}