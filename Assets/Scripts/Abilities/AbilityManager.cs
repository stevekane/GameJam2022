using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public enum AxisTag {
  Move,
  Aim,
  ReallyAim,
}

public class AbilityManager : MonoBehaviour {
  [HideInInspector] public Ability[] Abilities;
  public Optional<Energy> Energy;
  public TaskScope MainScope = new();

  public IEnumerable<Ability> Running => Abilities.Where(a => a.IsRunning);

  Status Status;

  Dictionary<AxisTag, AxisState> TagToAxis = new();
  Dictionary<AbilityMethod, EventSource> MethodToEvent = new();

  public void InitAbilities(Ability[] abilities) {
    Abilities = abilities;
    Abilities.ForEach(a => a.Character = GetComponent<Character>());
  }

  IEnumerable<Ability> Interruptible => Abilities.Where(a => a.IsRunning && !a.ActiveTags.HasAllFlags(AbilityTag.Uninterruptible));
  public void InterruptAbilities() => Interruptible.ForEach(a => a.Stop());
  IEnumerable<Ability> Cancellable => Abilities.Where(a => a.IsRunning && a.ActiveTags.HasAllFlags(AbilityTag.Cancellable | AbilityTag.OnlyOne));
  public void CancelAbilities() => Cancellable.ForEach(a => a.Stop());

  public void RegisterAxis(AxisTag tag, AxisState axis) {
    TagToAxis[tag] = axis;
  }
  public AxisState GetAxis(AxisTag tag) {
    if (!TagToAxis.TryGetValue(tag, out AxisState axis))
      TagToAxis[tag] = axis = new();
    return axis;
  }
  public AxisState CaptureAxis(AxisTag tag) {
    var realAxis = GetAxis(tag);
    TagToAxis[tag] = new();
    return realAxis;
  }
  public void UncaptureAxis(AxisTag tag, AxisState oldAxis) {
    TagToAxis[tag] = oldAxis;
  }
  public IEventSource GetEvent(AbilityMethod method) {
    if (!MethodToEvent.TryGetValue(method, out EventSource evt))
      MethodToEvent[method] = evt = new();
    return evt;
  }
  public bool TryInvoke(AbilityMethod method) {
    if (Status.IsFallen) {
      if (Status.Get<FallenEffect>() is var fallen && fallen != null)
        _ = fallen.RecoverySequence(MainScope);
      return false;
    }
    if (CanInvoke(method) is var can && can)
      Invoke(method);
    return can;
  }
  public TaskFunc TryRun(AbilityMethod method) => s => TryRun(s, method);
  public async Task TryRun(TaskScope scope, AbilityMethod method) {
    if (!TryInvoke(method))
      return;
    var ability = (Ability)method.Target;
    await scope.While(() => ability.IsRunning);
  }

  public bool CanInvoke(AbilityMethod method) {
    var ability = (Ability)method.Target;
    var trigger = ability.GetTriggerCondition(method);
    bool CanCancel(Ability other) => trigger.Tags.HasAllFlags(AbilityTag.CancelOthers) && other.ActiveTags.HasAllFlags(AbilityTag.Cancellable);

    var failReason = 0 switch {
      _ when !ability.CanStart(method) => 1,
      _ when !Status.CanAttack => 2,
      _ when !ability.Status.Tags.HasAllFlags(trigger.RequiredOwnerTags) => 3,
      _ when ability.Status.Tags.HasAllFlags(AbilityTag.Interact) && !trigger.Tags.HasAllFlags(AbilityTag.Interact) => 3.5,
      //_ when trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && Running.Any(a => !CanCancel(a)) => 4,
      _ when trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && Running.Any(a => a.ActiveTags.HasAllFlags(AbilityTag.OnlyOne) && !CanCancel(a)) => 4,
      _ when trigger.Tags.HasAllFlags(AbilityTag.BlockIfRunning) && ability.IsRunning => 5,
      _ when trigger.Tags.HasAllFlags(AbilityTag.BlockIfNotRunning) && !ability.IsRunning => 6,
      _ when trigger.Tags.HasAllFlags(AbilityTag.Grounded) && !Status.IsGrounded => 7,
      _ when trigger.Tags.HasAllFlags(AbilityTag.Airborne) && Status.IsGrounded => 8,
      _ when trigger.EnergyCost > Energy?.Value.Points => 9,
      _ => 0,
    };
    //if (failReason > 0)
    //  Debug.Log($"Trying to start {ability}.{method.Method.Name} but cant because {failReason}");
    return failReason == 0;
  }
  void Invoke(AbilityMethod method) {
    var ability = (Ability)method.Target;
    var trigger = ability.GetTriggerCondition(method);
    Debug.Assert(CanInvoke(method), $"{ability} event fired when it shouldn't - all code paths should have checked ShouldFire");
    if (trigger.Tags.HasAllFlags(AbilityTag.CancelOthers)) {
      //Debug.Log($"{ability}.{method.Method.Name} starting, will cancel <{string.Join(",", Cancellable)}>");
      CancelAbilities();
    }
    Energy?.Value.Consume(trigger.EnergyCost);
    ability.Run(method);
  }

  void Awake() {
    InitAbilities(GetComponentsInChildren<Ability>());
    this.InitComponent(out Status);
    Energy = GetComponentInChildren<Energy>();
  }
  void OnDestroy() {
    Abilities.ForEach(a => a.Stop());
    Abilities.ForEach(a => a.Character = null);
    MainScope.Dispose();
  }
  void FixedUpdate() {
    if (Status.IsHurt)
      InterruptAbilities();
  }
}