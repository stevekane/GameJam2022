using System.Collections;
using UnityEngine;

namespace PigMoss {
  public class PigMoss : MonoBehaviour {
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
      if (AbilityIndex == 0) {
        var bumRush = new BumRush(AbilityManager, BumRushConfig, Target);
        AbilityManager.TryInvoke(bumRush.Routine);
        yield return bumRush;
      } else if (AbilityIndex == 1) {
        var burst = new RadialBurst(AbilityManager, RadialBurstConfig);
        AbilityManager.TryInvoke(burst.Routine); // TODO: awkward.
        yield return burst;
      } else if (AbilityIndex == 2) {
        var buzzSaw = new BuzzSaw(AbilityManager, BuzzSawConfig);
        AbilityManager.TryInvoke(buzzSaw.Routine); // TODO: awkward.
        yield return buzzSaw;
      } else if (AbilityIndex == 3) {
        Bombard.AbilityManager = AbilityManager;
        AbilityManager.TryInvoke(Bombard.Routine);
        yield return Bombard;
      }
      AbilityIndex = (AbilityIndex+1)%4;
    }
  }
}