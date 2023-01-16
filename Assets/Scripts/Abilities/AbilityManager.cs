using System;
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
  Dictionary<AbilityMethod, EventRouter> MethodToEvent = new();

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
  public IEventSource GetEvent(AbilityMethod method) => GetRouter(method);
  public bool CanInvoke(AbilityMethod method) => GetRouter(method).ShouldFire();
  public bool TryInvoke(AbilityMethod method) {
    if (GetRouter(method) is var r && r.ShouldFire() is var shouldFire && shouldFire)
      r.Fire();
    return shouldFire;
  }
  public TaskFunc TryRun(AbilityMethod method) => s => TryRun(s, method);
  public async Task TryRun(TaskScope scope, AbilityMethod method) {
    if (!TryInvoke(method))
      return;
    var ability = (Ability)method.Target;
    await scope.While(() => ability.IsRunning);
  }

  EventRouter CreateRouter(AbilityMethod method) => MethodToEvent[method] = new EventRouter((Ability)method.Target, method);
  EventRouter GetRouter(AbilityMethod method) {
    if (!MethodToEvent.TryGetValue(method, out EventRouter evt))
      evt = CreateRouter(method);
    return evt;
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

  // All ability events route through this event source. Input-related event sources that connect to abilities
  // are actually instances of this class via InputManager.RegisterButton(code, type, EventRouterMaker);
  class EventRouter : IEventSource {
    //IEventSource EventSource;
    Action Action;
    Ability Ability;
    AbilityMethod Method;
    TriggerCondition Trigger;
    public EventRouter(Ability ability, AbilityMethod method) =>
      (Ability, Method, Trigger) = (ability, method, ability.GetTriggerCondition(method));
    public void Listen(Action handler) => Action += handler;
    public void Unlisten(Action handler) => Action -= handler;
    public void Fire() {
      Debug.Assert(ShouldFire(), $"{Ability} event fired when it shouldn't - all code paths should have checked ShouldFire");
      Action?.Invoke();
      if (Trigger.Tags.HasAllFlags(AbilityTag.CancelOthers))
        Ability.AbilityManager.CancelAbilities();
      Ability.AbilityManager.Energy?.Value.Consume(Trigger.EnergyCost);
      Ability.MaybeStartTask(Method);
    }
    public bool ShouldFire() {
      var am = Ability.AbilityManager;
      var status = am.Status;
      var canRun = 0 switch {
        //_ when !Ability.CanStart(Method) => false,
        _ when !status.CanAttack => false,
        _ when !Ability.Status.Tags.HasAllFlags(Trigger.RequiredOwnerTags) => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && am.Running.Any(a => !CanCancel(a)) => false,
        //_ when Trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && am.Running.Any(a => a.Tags.HasAllFlags(AbilityTag.OnlyOne) && !CanCancel(a)) => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.BlockIfRunning) && Ability.IsRunning => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.BlockIfNotRunning) && !Ability.IsRunning => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.Grounded) && !status.IsGrounded => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.Airborne) && status.IsGrounded => false,
        _ when Trigger.EnergyCost > Ability.AbilityManager.Energy?.Value.Points => false,
        _ => true,
      };
      return canRun;
    }
    bool CanCancel(Ability other) => Trigger.Tags.HasAllFlags(AbilityTag.CancelOthers) && other.ActiveTags.HasAllFlags(AbilityTag.Cancellable);
  }
}