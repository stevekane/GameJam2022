using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace PigMoss {
    public class PigMoss : MonoBehaviour {
    [SerializeField] AbilityManager AbilityManager;
    [SerializeField] Ability[] Abilities;
    [SerializeField] TargetingConfig TargetingConfig;
    [SerializeField] Transform CenterOfArena;
    [SerializeField] Timeval ActionCooldown;
    [SerializeField] BlackBoard BlackBoard;

    Mover Mover;
    int AbilityIndex;
    TaskScope MainScope = new();

    private void Awake() {
      Mover = GetComponent<Mover>();
    }

    void Start() => MainScope.Start(Waiter.Repeat(Behavior));
    void OnDestroy() => MainScope.Dispose();

    async Task Behavior(TaskScope scope) {
      // Targeting
      var acquireTargets = new AcquireTargets(transform, TargetingConfig, PhysicsQuery.Colliders);
      await scope.RunFiber(acquireTargets);

      // Scoring
      AbilityManager.InitAbilities(Abilities);
      BlackBoard.Target = PhysicsQuery.Colliders[acquireTargets.Value-1].transform;
      if (BlackBoard.Target) {
        var delta = BlackBoard.Target.transform.position-transform.position;
        var toTarget = delta.normalized;
        BlackBoard.DistanceScore = delta.magnitude;
        BlackBoard.AngleScore = Vector3.Dot(transform.forward, toTarget);
      } else {
        BlackBoard.DistanceScore = 0;
        BlackBoard.AngleScore = 0;
      }

      // Strategy (like a shitty histogram)
      var scores = Abilities.Select(a => a.Score()).ToArray();
      List<int> indices = new();
      for (var i = 0; i < scores.Length; i++) {
        var score = scores[i];
        var count = score switch {
          > .75f => 4,
          > .50f => 3,
          > .25f => 2,
          _ => 1
        };
        for (var j = 0; j < count; j++) {
          indices.Add(i);
        }
      }
      AbilityIndex = indices[UnityEngine.Random.Range(0, indices.Count)];
      await AbilityManager.TryRun(scope, Abilities[AbilityIndex].MainAction);

      // Cooldown
      await scope.Any(Waiter.Delay(ActionCooldown), Waiter.Repeat(PursueTarget));
      // TODO: This seems hacky? must be a nicer way to stop trying to move...
      // I kinda feel like it should be set every frame or something
      Mover.SetMoveAim(Vector3.zero, transform.forward.XZ());
    }

    void PursueTarget() {
      if (BlackBoard.Target) {
        var delta = BlackBoard.Target.transform.position - transform.position;
        var toTarget = delta.XZ().normalized;
        if (delta.magnitude > 10) {
          Mover.SetMoveAim(toTarget, toTarget);
        } else {
          Mover.SetMoveAim(Vector3.zero, transform.forward.XZ());
        }
      }
    }
  }
}