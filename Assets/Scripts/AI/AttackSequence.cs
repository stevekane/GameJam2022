using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class AttackSequence : MonoBehaviour {
  [Tooltip("Used by agent to choose a sequence: chance = Score/Sum(Scores)")]
  public int Score;
  [Tooltip("Distance to target must be more than this before this sequence can be chosen")]
  public float StartDistanceNear;
  [Tooltip("Distance to target must be less than this before this sequence can be chosen")]
  public float StartDistanceFar;

  [Serializable]
  public struct GapCloserData {
    public Ability Ability;
    public float DesiredDistance;
    public Timeval Cooldown;
  }
  [Tooltip("Optional gap closer to get in range")]
  public GapCloserData GapCloser;

  [Serializable]
  public struct SequenceData {
    [Tooltip("The whole sequence will end when the target gets this far away")]
    public float CancelIfDistanceExceeds;
    [Tooltip("List of abilities to execute, one after the other")]
    public List<MonoBehaviour> Sequence;
  }
  public SequenceData Setup;
  public SequenceData Finisher;

  public float DesiredDistance => 2f;

  AbilityManager AbilityManager;
  Status Status;
  Transform Self;
  Transform Target;

  void Awake() {
    this.InitComponentFromParent(out AbilityManager);
    this.InitComponentFromParent(out Status);
    Self = AbilityManager.transform;
    Debug.Assert(Setup.Sequence.All(b => b is Ability || b is ScriptTask));
    Debug.Assert(Finisher.Sequence.All(b => b is Ability || b is ScriptTask));
  }

  public bool CanStart(Transform target) {
    Target = target;
    return !TargetInRange(StartDistanceNear) && TargetInRange(StartDistanceFar);
  }
  public async Task Perform(TaskScope scope, Transform target) {
    //Debug.Log($"AttackSequence {this}");

    Target = target;

    // Gap close.
    //DesiredDistance = Setup.StartWithinDistance;
    if (GapCloser.Ability) {
      await scope.Any(
        Waiter.Until(() => TargetInRange(GapCloser.DesiredDistance)),
        Waiter.Repeat(TryGapCloser));
    }

    // Setup.
    var which = await scope.Any(
      Waiter.Until(() => !TargetInRange(Setup.CancelIfDistanceExceeds) || Status.IsHurt),
      PerformSubSequence(Setup));
    //if (which == 0) Debug.Log($"Setup cancelled: dist={Vector3.Distance(Target.position, Self.position)}");
    if (which == 0) // cancelled or got hit
      return;

    // Finisher.
    //DesiredDistance = Finisher.StartWithinDistance;
    which = await scope.Any(
      Waiter.Until(() => !TargetInRange(Finisher.CancelIfDistanceExceeds) || Status.IsHurt),
      Waiter.Sequence(PerformSubSequence(Finisher), Waiter.While(() => AbilityManager.Running.Any())));
    //if (which == 0) Debug.Log($"Finisher cancelled: dist={Vector3.Distance(Target.position, Self.position)}");
  }

  async Task TryGapCloser(TaskScope scope) {
    if (!GapCloser.Ability)
      await scope.Forever();
    await scope.Until(() => FacingTarget());
    await AbilityManager.TryRun(scope, GapCloser.Ability.MainAction);
    await scope.Delay(GapCloser.Cooldown);
  }

  TaskFunc PerformSubSequence(SequenceData sequence) => async (TaskScope scope) => {
    foreach (var script in sequence.Sequence) {
      if (script is Ability ability) {
        await scope.Until(() => AbilityManager.CanInvoke(ability.MainAction));
        if (ability is Throw t)  // TODO: generic target?
          t.Target = Target;
        //Debug.Log($"AttackSequence a={ability} {Timeval.TickCount}");
        // Don't wait for the ability to finish - makes use of recovery cancellation. CanInvoke will tell us when we can start the next ability.
        AbilityManager.TryInvoke(ability.MainAction);
      } else if (script is ScriptTask st) {
        //Debug.Log($"AttackSequence s={st} {Timeval.TickCount}");
        await st.Run(scope, Self, Target);
        //Debug.Log($"AttackSequence DONE {Timeval.TickCount}");
      }
    }
  };

  bool FacingTarget() {
    var delta = (Target.position - Self.position);
    return Vector3.Dot(Self.forward.XZ().normalized, delta.XZ().normalized) > .8f;
  }
  bool TargetInRange(float range) {
    var delta = (Target.position - Self.position);
    return delta.y < range && delta.XZ().sqrMagnitude < range.Sqr();
  }
}