using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PigMoss {
  public class PigMoss : MonoBehaviour {
    [SerializeField] AbilityManager AbilityManager;
    [SerializeField] FiberAbility[] Abilities;
    [SerializeField] TargetingConfig TargetingConfig;
    [SerializeField] Transform CenterOfArena;
    [SerializeField] Timeval ActionCooldown;
    [SerializeField] BlackBoard BlackBoard;

    Fiber Behavior;
    int AbilityIndex;

    void Start() => Behavior = new Fiber(Fiber.Repeat(MakeBehavior));
    void OnDestroy() => Behavior.Stop();
    void FixedUpdate() => Behavior.MoveNext();

    IEnumerator MakeBehavior() {
      // Targeting
      var acquireTargets = new AcquireTargets(transform, TargetingConfig, PhysicsQuery.Colliders);
      yield return acquireTargets;

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
      yield return TryRun(Abilities[AbilityIndex]);

      // Cooldown
      var cooldown = Fiber.Wait(ActionCooldown);
      var pursue = Fiber.Repeat(PursueTarget);
      yield return Fiber.Any(cooldown, pursue);
      // TODO: This seems hacky? must be a nicer way to stop trying to move...
      // I kinda feel like it should be set every frame or something
      Mover.UpdateAxes(AbilityManager, Vector3.zero, transform.forward.XZ());
    }

    void PursueTarget() {
      if (BlackBoard.Target) {
        var delta = BlackBoard.Target.transform.position - transform.position;
        var toTarget = delta.XZ().normalized;
        if (delta.magnitude > 10) {
          Mover.UpdateAxes(AbilityManager, toTarget, toTarget);
        } else {
          Mover.UpdateAxes(AbilityManager, Vector3.zero, transform.forward.XZ());
        }
      }
    }

    IEnumerator TryRun(FiberAbility ability) {
      ability.AbilityManager = AbilityManager;
      AbilityManager.TryInvoke(ability.Routine);
      return ability;
    }
  }
}