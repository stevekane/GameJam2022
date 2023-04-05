using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

using Range = System.ValueTuple<float, float>;

// Have your mob implement this so we can access common components.
public interface IMobComponents {
  AbilityManager AbilityManager { get; }
  Status Status { get; }
  Flash Flash { get; }
  Mover Mover { get; }
  Transform Target { get; }
  Transform transform { get; }
}

// Base class for scripted behaviors. Override all the abstract stuff and optionally the virtual stuff at the top.
// Basic idea is: collect all behaviors that CanStart, pick one, then Run it.
// Behaviors have preferred ranges for starting and ending. See below.
class AIBehavior {
  public IMobComponents Mob;
  // A weighting mechanism, for use with ChooseBehavior.
  public int Score;
  // If the target is within this range of distances, we can start the behavior and any abilities within.
  public Range StartRange;
  // The behavior continues as long as the target remains within this range - it is immediately cancelled otherwise.
  public Range DuringRange;
  // TODO: desired range?
  public float DesiredDistance;
  public Timeval Cooldown;
  public Func<bool> CanStart;
  public Func<bool> CanContinue;
  public TaskFunc Behavior;

  // Choose one of the startable behaviors using a weighted random pick.
  public static AIBehavior ChooseBehavior(IEnumerable<AIBehavior> behaviors) {
    var usableBehaviors = behaviors.Where(b => b.CanStartInternal());
    var totalScore = usableBehaviors.Sum(b => b.Score);
    var chosenScore = UnityEngine.Random.Range(0f, totalScore);
    var score = 0f;
    foreach (var b in usableBehaviors) {
      score += b.Score;
      if (chosenScore <= score)
        return b;
    }
    return null;
  }

  public AIBehavior(IMobComponents mob) => Mob = mob;

  // Convenience method for chaining together initialization routines.
  public AIBehavior With(Action<AIBehavior> init) {
    init(this);
    return this;
  }

  // Run this behavior and wait until it's finished + a whatever cooldown.
  public async Task Run(TaskScope scope) {
    //Debug.Log($"Sequence START: {this} {Timeval.TickCount}");
    await scope.Any(Behavior, Waiter.While(() => CanContinueInternal()));
    await scope.While(() => Mob.AbilityManager.Running.Any());
    if (Cooldown != null)
      await scope.Delay(Cooldown);
    //Debug.Log($"Sequence END: {this} {Timeval.TickCount}");
  }

  bool CanStartInternal() => TargetInRange(StartRange.Item1, StartRange.Item2) && (CanStart?.Invoke() ?? true);
  bool CanContinueInternal() => TargetInXZRange(DuringRange.Item1, DuringRange.Item2) && !Mob.Status.IsHurt && (CanContinue?.Invoke() ?? true);

  // Helper functions for behaviors:

  public async Task StartAbility(TaskScope scope, Ability ability) {
    await scope.Until(() => CanInvoke(ability));
    await TryInvoke(scope, ability.MainAction);
  }
  public async Task StartAbilityWhenInRange(TaskScope scope, Ability ability) {
    await scope.Until(() => TargetInRange(StartRange.Item1, StartRange.Item2) && CanInvoke(ability));
    await TryInvoke(scope, ability.MainAction);
  }
  public async Task ReleaseAbilityWhenInRange(TaskScope scope, Ability ability) {
    await scope.Until(() => CanInvoke(ability));
    await TryInvoke(scope, ability.MainAction);
    await scope.Until(() => TargetInRange(StartRange.Item1, StartRange.Item2) && CanInvoke(ability.MainRelease));
    await TryInvoke(scope, ability.MainRelease);
  }
  async Task TryInvoke(TaskScope scope, AbilityMethod method) {
    Mob.AbilityManager.TryInvoke(method);
    await scope.Tick();  // Need this for the ability's Task to Start.
  }
  public async Task TelegraphThenAttack(TaskScope scope) => await Mob.Flash.RunStrobe(scope, Color.red, Timeval.FromMillis(150), 3);
  public async Task TelegraphDuringAttack(TaskScope scope) { _ = TelegraphThenAttack(scope); await scope.Tick(); }

  // If we can never invoke an ability given our state, just return true and let the invocation fail.
  public bool CanInvoke(Ability ability) => CanInvoke(ability.MainAction);
  public bool CanInvoke(AbilityMethod method) => Mob.AbilityManager.CanInvoke(method) || !CanEverInvoke(method);
  public bool CanEverInvoke(AbilityMethod method) {
    var trigger = ((Ability)method.Target).GetTriggerCondition(method);
    return 0 switch {
      _ when trigger.Tags.HasAllFlags(AbilityTag.Grounded) && !Mob.Status.IsGrounded => false,
      _ when trigger.Tags.HasAllFlags(AbilityTag.Airborne) && Mob.Status.IsGrounded => false,
      _ when trigger.EnergyCost > Mob.AbilityManager.Energy?.Value.Points => false,
      _ => true,
    };
  }

  // Target is in a cylindrical volume.
  public bool TargetInRange(float min, float max) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var sm = delta.XZ().sqrMagnitude;
    return sm >= min*min && sm <= max*max && Mathf.Abs(delta.y) <= max;
  }

  // Target is in a cylindrical volume of infinite height.
  public bool TargetInXZRange(float min, float max) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var sm = delta.XZ().sqrMagnitude;
    return sm >= min*min && sm <= max*max;
  }

  public bool FacingTarget(float minDot = .8f) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var dot = Vector3.Dot(Mob.transform.forward.XZ().normalized, delta.XZ().normalized);
    return dot >= minDot;
  }

  public bool MovingTowardsTarget(float minDot = .8f) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var dot = Vector3.Dot(Mob.Mover.GetMove().normalized, delta.XZ().normalized);
    return dot >= minDot;
  }
}