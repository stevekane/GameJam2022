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
      // Strategy
      var index = UnityEngine.Random.Range(0, Abilities.Length);
      yield return TryRun(Abilities[index]);
      // Cooldown
      yield return Fiber.Wait(ActionCooldown);
    }

    IEnumerator TryRun(FiberAbility ability) {
      ability.AbilityManager = AbilityManager;
      AbilityManager.TryInvoke(ability.Routine);
      return ability;
    }
  }
}