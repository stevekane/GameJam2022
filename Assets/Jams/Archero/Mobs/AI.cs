using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Archero {
  public class AI : CharacterController {
    [SerializeField] NavMeshAgent NavMeshAgent;
    [SerializeField] PathMove PathMove;
    [SerializeField] AbilityActionFieldReference AbilityActionRef;
    [SerializeField] int MoveTicks = 60;
    [SerializeField] string AreaName = "Walkable";

    TaskScope Scope;

    public Vector3 Velocity;

    public override void Start() {
      base.Start();
      NavMeshAgent.updatePosition = false;
      NavMeshAgent.updateRotation = false;
      NavMeshAgent.updateUpAxis = false;
      Scope = new();
      Scope.Start(Behavior);
    }

    async Task Behavior(TaskScope scope) {
      while (true) {
        for (var i = 0; i < MoveTicks; i++) {
          if (AbilityManager.CanRun(PathMove.Move)) {
            var targetPosition = Player.Instance.transform.position;
            var areaMask = 1 << NavMesh.GetAreaFromName(AreaName);
            var foundPoint = NavMesh.SamplePosition(targetPosition, out var hit, 1, areaMask);
            PathMove.IsRunning = true;
            AbilityManager.Run(PathMove.Move, hit.position);
          } else {
            Velocity = Vector3.zero;
            PathMove.IsRunning = false;
          }
          await scope.Tick();
        }
        if (AbilityActionRef.Target && AbilityManager.CanRun(AbilityActionRef.Value)) {
          AbilityManager.Run(AbilityActionRef.Value);
        } else {
          // Debug.Log("No Attack Ability Found");
        }
        await scope.Tick();
      }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
      base.UpdateRotation(ref currentRotation, deltaTime);
      if (Velocity.sqrMagnitude > 0) {
        currentRotation = Quaternion.LookRotation(Velocity);
      }
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
      base.UpdateVelocity(ref currentVelocity, deltaTime);
      currentVelocity = Velocity;
    }
  }
}