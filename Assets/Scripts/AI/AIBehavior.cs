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
abstract class AIBehavior {
  abstract public IMobComponents Mob { get; }
  // A weighting mechanism, for use by agents.
  abstract public int Score { get; }
  // If the target is within this range of distances, we can start the behavior and any abilities within.
  abstract public Range StartRange { get; }
  // The behavior continues as long as the target remains within this range - it is immediately cancelled otherwise.
  abstract public Range DuringRange { get; }
  // TODO: desired range?
  virtual public float DesiredDistance => .5f * (StartRange.Item1 + StartRange.Item2);
  virtual public Timeval Cooldown => null;

  protected abstract Task Behavior(TaskScope scope);

  public virtual bool CanStart() => TargetInRange(StartRange.Item1, StartRange.Item2);
  public async Task Run(TaskScope scope) {
    //Debug.Log($"Sequence: {this}");
    await scope.Any(Behavior, Waiter.While(() => TargetInXZRange(DuringRange.Item1, DuringRange.Item2) && !Mob.Status.IsHurt));
    if (Cooldown != null)
      await scope.Delay(Cooldown);
  }

  // Helper functions for behaviors:

  protected async Task StartAbility(TaskScope scope, Ability ability) {
    await scope.Until(() => CanInvoke(ability));
    Mob.AbilityManager.TryInvoke(ability.MainAction);
  }
  protected async Task StartAbilityWhenInRange(TaskScope scope, Ability ability) {
    await scope.Until(() => TargetInRange(StartRange.Item1, StartRange.Item2) && CanInvoke(ability));
    Mob.AbilityManager.TryInvoke(ability.MainAction);
  }
  protected async Task TelegraphThenAttack(TaskScope scope) => await Mob.Flash.RunStrobe(scope, Color.red, Timeval.FromMillis(150), 3);
  protected async Task TelegraphDuringAttack(TaskScope scope) { _ = TelegraphThenAttack(scope); await scope.Yield(); }

  // If we can never invoke an ability given our state, just return true and let the invocation fail.
  protected bool CanInvoke(Ability ability) => Mob.AbilityManager.CanInvoke(ability.MainAction) || !CanEverInvoke(ability);
  protected bool CanEverInvoke(Ability ability) {
    var trigger = ability.GetTriggerCondition(ability.MainAction);
    return 0 switch {
      _ when trigger.Tags.HasAllFlags(AbilityTag.Grounded) && !Mob.Status.IsGrounded => false,
      _ when trigger.Tags.HasAllFlags(AbilityTag.Airborne) && Mob.Status.IsGrounded => false,
      _ when trigger.EnergyCost > Mob.AbilityManager.Energy?.Value.Points => false,
      _ => true,
    };
  }

  // Target is in a cylindrical volume.
  protected bool TargetInRange(float min, float max) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var sm = delta.XZ().sqrMagnitude;
    return sm >= min*min && sm <= max*max && Mathf.Abs(delta.y) <= max;
  }

  // Target is in a cylindrical volume of infinite height.
  protected bool TargetInXZRange(float min, float max) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var sm = delta.XZ().sqrMagnitude;
    return sm >= min*min && sm <= max*max;
  }

  protected bool FacingTarget(float minDot = .8f) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var dot = Vector3.Dot(Mob.transform.forward.XZ().normalized, delta.XZ().normalized);
    return dot >= minDot;
  }

  protected bool MovingTowardsTarget(float minDot = .8f) {
    var delta = (Mob.Target.position - Mob.transform.position);
    var dot = Vector3.Dot(Mob.Mover.GetMove().normalized, delta.XZ().normalized);
    return dot >= minDot;
  }
}