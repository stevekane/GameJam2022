using System.Linq;
using System.Collections;
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
      Abilities.ForEach(a => a.AbilityManager = AbilityManager);
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
      var scores = Abilities.Select(a => a.Score()).ToArray();

      // Strategy
      var bestScore = 0f;
      var index = 0;
      for (var i = 0; i < scores.Length; i++) {
        if (scores[i] > bestScore) {
          bestScore = scores[i];
          index = i;
        }
      }
      yield return TryRun(Abilities[index]);

      // Cooldown
      var cooldown = Fiber.Wait(ActionCooldown);
      var lookAt = Fiber.Repeat(GetComponent<Mover>().TryLookAt, BlackBoard.Target);
      yield return Fiber.Any(cooldown, lookAt);
    }

    IEnumerator TryRun(FiberAbility ability) {
      ability.AbilityManager = AbilityManager;
      AbilityManager.TryInvoke(ability.Routine);
      return ability;
    }
  }
}