using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public enum AxisTag {
  Move,
  Aim,
}

public class AbilityManager : MonoBehaviour {
  [HideInInspector] public IAbility[] Abilities;
  public Optional<Energy> Energy;
  public Bundle Bundle = new();

  public IEnumerable<IAbility> Running { get => Abilities.Where(a => a.IsRunning); }

  Dictionary<AxisTag, AxisState> TagToAxis = new();
  // TODO(Task): object => AbilityMethodTask
  Dictionary<object, EventRouter> MethodToEvent = new();

  public void InitAbilities(IAbility[] abilities) {
    Abilities = abilities;
    Abilities.ForEach(a => a.AbilityManager = this);
  }

  public void InterruptAbilities() => Abilities.Where(a => a.IsRunning && !a.Tags.HasAllFlags(AbilityTag.Uninterruptible)).ForEach(a => a.Stop());
  public void CancelOthers() => Abilities.Where(a => a.IsRunning && a.Tags.HasAllFlags(AbilityTag.Cancellable)).ForEach(a => a.Stop());
  public void CancelOthers(IAbility except) => Abilities.Where(a => a.IsRunning && a != except && a.Tags.HasAllFlags(AbilityTag.Cancellable)).ForEach(a => a.Stop());

  public void RegisterAxis(AxisTag tag, AxisState axis) {
    TagToAxis[tag] = axis;
  }
  public void RegisterEvent(AbilityMethod method, IEventSource evt) {
    var router = CreateRouter(method);
    router.ConnectSource(evt);
  }
  public void RegisterEvent(AbilityMethodTask method, IEventSource evt) {
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
  public IEventSource GetEvent(AbilityMethodTask method) {
    if (!MethodToEvent.TryGetValue(method, out EventRouter evt))
      evt = CreateRouter(method);
    return evt;
  }
  public void TryInvoke(AbilityMethod method) => GetEvent(method).Fire();
  public void TryInvoke(AbilityMethodTask method) => GetEvent(method).Fire();
  public IEnumerator TryRun(AbilityMethod method) {
    var ability = (IAbility)method.Target;
    GetEvent(method).Fire();
    yield return Fiber.While(() => ability.IsRunning);
  }
  public IEnumerator TryRun(AbilityMethodTask method) {
    var ability = (IAbility)method.Target;
    GetEvent(method).Fire();
    yield return Fiber.While(() => ability.IsRunning);
  }
  public async Task TryRun(TaskScope scope, AbilityMethodTask method) {
    var ability = (IAbility)method.Target;
    GetEvent(method).Fire();
    await scope.While(() => ability.IsRunning);
  }
  EventRouter CreateRouter(AbilityMethod method) => MethodToEvent[method] = new EventRouter((IAbility)method.Target, method);
  EventRouter CreateRouter(AbilityMethodTask method) => MethodToEvent[method] = new EventRouter((IAbility)method.Target, method);

  void Awake() {
    InitAbilities(GetComponentsInChildren<Ability>());
    Energy = GetComponent<Energy>();
  }
  void OnDestroy() {
    Bundle.Stop();
    Abilities.ForEach(a => a.Stop());
    Abilities.ForEach(a => a.AbilityManager = null);
    MethodToEvent.ForEach(kv => kv.Value.DisconnectSource());
  }
  void FixedUpdate() => Bundle.MoveNext();

  // All ability events route through this event source. Input-related event sources that connect to abilities
  // are actually instances of this class via InputManager.RegisterButton(code, type, EventRouterMaker);
  class EventRouter : IEventSource {
    IEventSource EventSource;
    Action Action;
    IAbility Ability;
    AbilityMethod Method;
    AbilityMethodTask MethodTask;
    TriggerCondition Trigger;
    public EventRouter(IAbility ability, AbilityMethod method) =>
      (Ability, Method, Trigger) = (ability, method, ability.GetTriggerCondition(method));
    public EventRouter(IAbility ability, AbilityMethodTask method) =>
      (Ability, MethodTask, Trigger) = (ability, method, ability.GetTriggerCondition(method));
    public void ConnectSource(IEventSource evt) => (EventSource = evt).Listen(Fire);
    public void DisconnectSource() => EventSource?.Unlisten(Fire);
    public void Listen(Action handler) => Action += handler;
    public void Unlisten(Action handler) => Action -= handler;
    public void Fire() {
      if (ShouldFire()) {
        if (Trigger.Tags.HasAllFlags(AbilityTag.CancelOthers))
          Ability.AbilityManager.CancelOthers();

        Ability.AbilityManager.Energy?.Value.Consume(Trigger.EnergyCost);

        var tags = Ability.Tags;
        Ability.Tags = tags.AddFlags(Trigger.Tags);
        Action?.Invoke();
        if (Method != null) {
          var enumerator = Method();
          if (enumerator != null) {  // Can be null if used only for event listeners.
            Ability.StartRoutine(new Fiber(enumerator));
          }
        } else if (MethodTask != null) {
          Ability.MaybeStartTask(MethodTask);
        } else {
          Debug.LogError($"{Ability} has no Method!");
        }
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
}
