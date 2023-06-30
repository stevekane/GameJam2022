using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Archero {
  public enum MoveStyle {
    Stationary,
    Wander,
    Chase
  }

  public class AI : CharacterController {
    [SerializeField] MoveStyle MoveStyle;
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

    async Task Idle(TaskScope scope) {
      await scope.Ticks(MoveTicks);
    }

    async Task Wander(TaskScope scope) {
      var targetPosition = transform.position + 10 * Random.onUnitSphere.XZ();
      for (var i = 0; i < MoveTicks; i++) {
        if (AbilityManager.CanRun(PathMove.Move)) {
          PathMove.IsRunning = true;
          AbilityManager.Run(PathMove.Move, targetPosition);
        } else {
          Velocity = Vector3.zero;
          PathMove.IsRunning = false;
        }
        await scope.Tick();
      }
    }

    async Task Chase(TaskScope scope) {
      for (var i = 0; i < MoveTicks; i++) {
        if (AbilityManager.CanRun(PathMove.Move)) {
          PathMove.IsRunning = true;
          AbilityManager.Run(PathMove.Move, Player.Instance.transform.position);
        } else {
          Velocity = Vector3.zero;
          PathMove.IsRunning = false;
        }
        await scope.Tick();
      }
    }

    async Task Behavior(TaskScope scope) {
      while (true) {
        var moveBehavior = MoveStyle switch {
          MoveStyle.Wander => Wander(scope),
          MoveStyle.Chase => Chase(scope),
          _ => Idle(scope),
        };
        await moveBehavior;
        Velocity = Vector3.zero;
        PathMove.IsRunning = false;
        if (AbilityActionRef.Target && AbilityManager.CanRun(AbilityActionRef.Value)) {
          AbilityManager.Run(AbilityActionRef.Value);
          await scope.Until(() => !AbilityActionRef.Target.IsRunning);
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