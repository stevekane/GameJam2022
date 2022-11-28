using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AxisTag {
  Move,
  Aim,
}

public class AbilityManager : MonoBehaviour {
  [HideInInspector] public IAbility[] Abilities;
  [HideInInspector] public List<IAbility> Running = new();
  [HideInInspector] public List<IAbility> ToAdd = new();
  public Optional<Energy> Energy;
  public Bundle Bundle = new();

  Dictionary<AxisTag, AxisState> TagToAxis = new();
  Dictionary<AbilityMethod, EventRouter> MethodToEvent = new();

  // All ability events route through this event source. Input-related event sources that connect to abilities
  // are actually instances of this class via InputManager.RegisterButton(code, type, EventRouterMaker);
  class EventRouter : IEventSource {
    IEventSource EventSource;
    Action Action;
    IAbility Ability;
    AbilityMethod Method;
    TriggerCondition Trigger;
    public EventRouter(IAbility ability, AbilityMethod method) =>
      (Ability, Method, Trigger) = (ability, method, ability.GetTriggerCondition(method));
    public void ConnectSource(IEventSource evt) => (EventSource = evt).Listen(Fire);
    public void DisconnectSource() => EventSource?.Unlisten(Fire);
    public void Listen(Action handler) => Action += handler;
    public void Unlisten(Action handler) => Action -= handler;
    public void Fire() {
      if (ShouldFire()) {
        var tags = Ability.Tags;
        Ability.AbilityManager.Energy?.Value.Consume(Trigger.EnergyCost);
        Ability.Tags = tags.AddFlags(Trigger.Tags);
        Action?.Invoke();
        var enumerator = Method();
        if (enumerator != null) {  // Can be null for events that listen temporarily.
          Ability.StartRoutine(new Fiber(enumerator));
          Ability.AbilityManager.ToAdd.Add(Ability);
        }
        if (Trigger.Tags.HasAllFlags(AbilityTag.CancelOthers))
          Ability.AbilityManager.Running.Where(a => a.Tags.HasAllFlags(AbilityTag.Cancellable)).ForEach(a => a.Stop());
      }
    }
    bool ShouldFire() {
      var am = Ability.AbilityManager;
      var canRun = 0 switch {
        _ when !Ability.Status.Tags.HasAllFlags(Trigger.RequiredOwnerTags) => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.OnlyOne) && am.Running.Any(a => a.Tags.HasAllFlags(AbilityTag.OnlyOne) && !CanCancel(a)) => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.BlockIfRunning) && Ability.IsRunning => false,
        _ when Trigger.Tags.HasAllFlags(AbilityTag.BlockIfNotRunning) && !Ability.IsRunning => false,
        _ when Trigger.EnergyCost > Ability.AbilityManager.Energy?.Value.Points => false,
        _ => true,
      };
      return canRun;
    }
    bool CanCancel(IAbility other) => Trigger.Tags.HasAllFlags(AbilityTag.CancelOthers) && other.Tags.HasAllFlags(AbilityTag.Cancellable);
  }

  public void InterruptAbilities() => Abilities.Where(a => a.IsRunning && !a.Tags.HasAllFlags(AbilityTag.Uninterruptible)).ForEach(a => a.Stop());

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
  EventRouter CreateRouter(AbilityMethod method) => MethodToEvent[method] = new EventRouter((IAbility)method.Target, method);

  void StackAdd<T>(List<T> target, List<T> additions) {
    target.Reverse();
    target.AddRange(additions);
    target.Reverse();
  }

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
    Abilities.ForEach(a => a.AbilityManager = this);
    Energy = GetComponent<Energy>();
  }
  void OnDestroy() {
    Bundle.Stop();
    Abilities.ForEach(a => a.Stop());
    Abilities.ForEach(a => a.AbilityManager = null);
    MethodToEvent.ForEach(kv => kv.Value.DisconnectSource());
  }
  // TODO: Should this be manually set high in Script Execution Order instead?
  void LateUpdate() {
    Running.RemoveAll(a => !a.IsRunning);
    StackAdd(Running, ToAdd);
    ToAdd.Clear();
  }
  void FixedUpdate() => Bundle.MoveNext();
}
