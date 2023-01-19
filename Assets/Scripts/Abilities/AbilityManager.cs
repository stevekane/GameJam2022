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

  public IEnumerable<Ability> Running { get => Abilities.Where(a => a.IsRunning); }

  Status Status;

  Dictionary<AxisTag, AxisState> TagToAxis = new();
  Dictionary<AbilityMethod, EventSource> MethodToEvent = new();

  public void InitAbilities(Ability[] abilities) {
    Abilities = abilities;
    Abilities.ForEach(a => a.AbilityManager = this);
  }

  public void InterruptAbilities() => Abilities.Where(a => a.IsRunning && !a.ActiveTags.HasAllFlags(AbilityTag.Uninterruptible)).ForEach(a => a.Stop());
  public void CancelAbilities() => Abilities.Where(a => a.IsRunning && a.ActiveTags.HasAllFlags(AbilityTag.Cancellable)).ForEach(a => a.Stop());

  public void RegisterAxis(AxisTag tag, AxisState axis) {
    TagToAxis[tag] = axis;
  }
  public AxisState GetAxis(AxisTag tag) {
    if (!TagToAxis.TryGetValue(tag, out AxisState axis))
      TagToAxis[tag] = axis = new();
    return axis;
  }
  public IEventSource GetEvent(AbilityMethod method) {
    if (!MethodToEvent.TryGetValue(method, out EventSource evt))
      MethodToEvent[method] = evt = new();
    return evt;
  }
  public bool TryInvoke(AbilityMethod method) {
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

    var canRun = 0 switch {
      _ when !ability.CanStart(method) => false,
      _ when !Status.CanAttack => false,
      _ when !ability.Status.Tags.HasAllFlags(trigger.RequiredOwnerTags) => false,
      _ when trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && Running.Any(a => !CanCancel(a)) => false,
      //_ when Trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && am.Running.Any(a => a.Tags.HasAllFlags(AbilityTag.OnlyOne) && !CanCancel(a)) => false,
      _ when trigger.Tags.HasAllFlags(AbilityTag.BlockIfRunning) && ability.IsRunning => false,
      _ when trigger.Tags.HasAllFlags(AbilityTag.BlockIfNotRunning) && !ability.IsRunning => false,
      _ when trigger.Tags.HasAllFlags(AbilityTag.Grounded) && !Status.IsGrounded => false,
      _ when trigger.Tags.HasAllFlags(AbilityTag.Airborne) && Status.IsGrounded => false,
      _ when trigger.EnergyCost > Energy?.Value.Points => false,
      _ => true,
    };
    return canRun;
  }

  void Invoke(AbilityMethod method) {
    var ability = (Ability)method.Target;
    var trigger = ability.GetTriggerCondition(method);
    Debug.Assert(CanInvoke(method), $"{ability} event fired when it shouldn't - all code paths should have checked ShouldFire");
    GetEvent(method).Fire();
    if (trigger.Tags.HasAllFlags(AbilityTag.CancelOthers))
      CancelAbilities();
    Energy?.Value.Consume(trigger.EnergyCost);
    ability.MaybeStartTask(method);
  }

  void Awake() {
    InitAbilities(GetComponentsInChildren<Ability>());
    this.InitComponent(out Status);
    Energy = GetComponentInChildren<Energy>();
  }
  void OnDestroy() {
    Abilities.ForEach(a => a.Stop());
    Abilities.ForEach(a => a.AbilityManager = null);
    MainScope.Dispose();
  }
  void FixedUpdate() {
    if (Status.IsHurt)
      InterruptAbilities();
  }
}