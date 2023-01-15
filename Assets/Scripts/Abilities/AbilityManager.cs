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
  public void RegisterEvent(AbilityMethod method, IEventSource evt) {
    var router = CreateRouter(method);
    router.ConnectSource(evt);
  }
  public AxisState GetAxis(AxisTag tag) {
    if (!TagToAxis.TryGetValue(tag, out AxisState axis))
      TagToAxis[tag] = axis = new();
    return axis;
  }
  public IEventSource GetEvent(AbilityMethod method) {
    if (!MethodToEvent.TryGetValue(method, out EventRouter evt))
      evt = CreateRouter(method);
    return evt;
  }
  public void TryInvoke(AbilityMethod method) => GetEvent(method).Fire();
  public async Task TryRun(TaskScope scope, AbilityMethod method) {
    var ability = (Ability)method.Target;
    GetEvent(method).Fire();
    await scope.While(() => ability.IsRunning);
  }
  public TaskFunc TryRun(AbilityMethod method) => async (TaskScope scope) => {
    var ability = (Ability)method.Target;
    GetEvent(method).Fire();
    await scope.While(() => ability.IsRunning);
  };
  EventRouter CreateRouter(AbilityMethod method) => MethodToEvent[method] = new EventRouter((Ability)method.Target, method);

  void Awake() {
    InitAbilities(GetComponentsInChildren<Ability>());
    Energy = GetComponentInChildren<Energy>();
    Status = GetComponent<Status>();
  }
  void OnDestroy() {
    Abilities.ForEach(a => a.Stop());
    Abilities.ForEach(a => a.AbilityManager = null);
    MethodToEvent.ForEach(kv => kv.Value.DisconnectSource());
    MainScope.Dispose();
  }
  void FixedUpdate() {
    if (Status.IsHurt)
      InterruptAbilities();
  }

  // All ability events route through this event source. Input-related event sources that connect to abilities
  // are actually instances of this class via InputManager.RegisterButton(code, type, EventRouterMaker);
  class EventRouter : IEventSource {
    IEventSource EventSource;
    Action Action;
    Ability Ability;
    AbilityMethod Method;
    TriggerCondition Trigger;
    public EventRouter(Ability ability, AbilityMethod method) =>
      (Ability, Method, Trigger) = (ability, method, ability.GetTriggerCondition(method));
    public void ConnectSource(IEventSource evt) => (EventSource = evt).Listen(Fire);
    public void DisconnectSource() => EventSource?.Unlisten(Fire);
    public void Listen(Action handler) => Action += handler;
    public void Unlisten(Action handler) => Action -= handler;
    public void Fire() {
      // Always invoke the event - if no one is listening this will noop.
      Action?.Invoke();
      if (ShouldFire()) {
        if (Trigger.Tags.HasAllFlags(AbilityTag.CancelOthers))
          Ability.AbilityManager.CancelAbilities();
        Ability.AbilityManager.Energy?.Value.Consume(Trigger.EnergyCost);
        Ability.MaybeStartTask(Method);
      }
    }
    public bool ShouldFire() {
      var am = Ability.AbilityManager;
      var Status = am.GetComponent<Status>();
      var canRun = 0 switch {
        _ when !Ability.Status.Tags.HasAllFlags(Trigger.RequiredOwnerTags) => false,
        _ when !Status.CanAttack => false, // TODO: Is this needed? It sure feels necessary
        _ when Trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && am.Running.Any(a => !CanCancel(a)) => false,
        //_ when Trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && am.Running.Any(a => a.Tags.HasAllFlags(AbilityTag.OnlyOne) && !CanCancel(a)) => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.BlockIfRunning) && Ability.IsRunning => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.BlockIfNotRunning) && !Ability.IsRunning => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.Grounded) && !Status.IsGrounded => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.Airborne) && Status.IsGrounded => false,
        _ when Trigger.EnergyCost > Ability.AbilityManager.Energy?.Value.Points => false,
        _ => true,
      };
      return canRun;
    }
    bool CanCancel(Ability other) => Trigger.Tags.HasAllFlags(AbilityTag.CancelOthers) && other.ActiveTags.HasAllFlags(AbilityTag.Cancellable);
  }
}