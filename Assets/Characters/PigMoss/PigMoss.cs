using System.Collections;
using UnityEngine;

namespace PigMoss {
  public class PigMoss : MonoBehaviour {
    [SerializeField] SwordStrike SwordStrike;
    [SerializeField] Bombard Bombard;
    [SerializeField] RadialBurstConfig RadialBurstConfig;
    [SerializeField] BumRushConfig BumRushConfig;
    [SerializeField] BuzzSawConfig BuzzSawConfig;
    [SerializeField] TargetingConfig TargetingConfig;
    [SerializeField] Transform CenterOfArena;
    [SerializeField] Timeval ActionCooldown;

    Fiber Behavior;
    Transform Target;
    Mover Mover;
    AbilityManager AbilityManager;
    int AbilityIndex;

    void Awake() {
      Mover = GetComponent<Mover>();
      AbilityManager = GetComponent<AbilityManager>();
    }
    void Start() => Behavior = new Fiber(Fiber.Repeat(MakeBehavior));
    void OnDestroy() => Behavior.Stop();
    void FixedUpdate() => Behavior.MoveNext();

    IEnumerator TryRun(FiberAbility ability) {
      ability.AbilityManager = AbilityManager;
      AbilityManager.TryInvoke(ability.Routine);
      return ability;
    }

    IEnumerator MakeBehavior() {
      yield return Fiber.Wait(ActionCooldown);
      var acquireTargets = new AcquireTargets(transform, TargetingConfig, PhysicsQuery.Colliders);
      yield return acquireTargets;
      Target = PhysicsQuery.Colliders[acquireTargets.Value-1].transform;
      yield return ChooseAction();
    }

    IEnumerator LookAround() {
      var direction = UnityEngine.Random.insideUnitSphere.XZ().normalized;
      Mover.GetAxes(AbilityManager, out var move, out var forward);
      Mover.UpdateAxes(AbilityManager, move, direction);
      yield return Fiber.Wait(Timeval.FixedUpdatePerSecond);
    }

    IEnumerator ChooseAction() {
      // var index = 4;
      var index = (int)UnityEngine.Random.Range(0,5);
      yield return index switch {
        0 => TryRun(new BumRush(AbilityManager, BumRushConfig, Target)),
        1 => TryRun(new RadialBurst(AbilityManager, RadialBurstConfig)),
        2 => TryRun(new BuzzSaw(AbilityManager, BuzzSawConfig)),
        3 => TryRun(Bombard),
        4 => TryRun(SwordStrike),
        _ => TryRun(SwordStrike)
      };
    }
  }
}