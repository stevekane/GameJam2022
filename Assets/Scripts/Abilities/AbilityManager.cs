using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AxisTag {
  Move,
  Aim,
}

public class AbilityManager : MonoBehaviour {
  [HideInInspector] public Ability[] Abilities;
  [HideInInspector] public List<Ability> Running = new();
  [HideInInspector] public List<Ability> ToAdd = new();

  Dictionary<AxisTag, AxisState> TagToAxis = new();
  Dictionary<AbilityMethod, IEventSource> MethodToEvent = new();

  // All ability events route through this event source. Input-related event sources that connect to abilities
  // are actually instances of this class via InputManager.RegisterButton(code, type, EventRouterMaker);
  class EventRouter : IEventSource {
    Action Action;
    Ability Ability;
    AbilityMethod Method;
    AbilityTag TriggerTags;
    public EventRouter(Ability ability, AbilityMethod method) => (Ability, Method, TriggerTags) = (ability, method, ability.GetTriggerTags(method));
    public void Listen(Action handler) => Action += handler;
    public void Unlisten(Action handler) => Action -= handler;
    public void Fire() {
      if (ShouldFire()) {
        Ability.Tags.AddFlags(TriggerTags);
        Action?.Invoke();
        var enumerator = Method();
        if (enumerator != null) {  // Can be null for events that listen temporarily.
          Ability.StartRoutine(new Fiber(enumerator));
          Ability.AbilityManager.ToAdd.Add(Ability);
        }
        if (TriggerTags.HasAllFlags(AbilityTag.CancelOthers))
          Ability.AbilityManager.Running.Where(a => a.Tags.HasAllFlags(AbilityTag.Cancellable)).ForEach(a => a.Stop());
      }
    }
    bool ShouldFire() {
      var am = Ability.AbilityManager;
      var canRun = 0 switch {
        _ when TriggerTags.HasAllFlags(AbilityTag.OnlyOne) && am.Running.Any(a => a.Tags.HasAllFlags(AbilityTag.OnlyOne) && !CanCancel(a)) => false,
        _ when TriggerTags.HasAllFlags(AbilityTag.BlockIfRunning) && Ability.IsRunning => false,
        _ when TriggerTags.HasAllFlags(AbilityTag.BlockIfNotRunning) && !Ability.IsRunning => false,
        _ => true,
      };
      return canRun;
    }
    bool CanCancel(Ability other) => TriggerTags.HasAllFlags(AbilityTag.CancelOthers) && other.Tags.HasAllFlags(AbilityTag.Cancellable);
  }

  public void InterruptAbilities() => Abilities.Where(a => a.IsRunning && !a.Tags.HasAllFlags(AbilityTag.Uninterruptible)).ForEach(a => a.Stop());

  public void RegisterAxis(AxisTag tag, AxisState axis) {
    TagToAxis[tag] = axis;
  }
  public void RegisterEvent(AbilityMethod method, IEventSource evt) {
    MethodToEvent[method] = evt;
  }
  public AxisState GetAxis(AxisTag tag) {
    if (!TagToAxis.TryGetValue(tag, out AxisState axis))
      TagToAxis[tag] = axis = new();
    return axis;
  }
  public IEventSource GetEvent(AbilityMethod method) {
    if (!MethodToEvent.TryGetValue(method, out IEventSource evt))
      RegisterEvent(method, evt = new EventRouter((Ability)method.Target, method));
    return evt;
  }
  public void TryInvoke(AbilityMethod method) => GetEvent(method).Fire();

  void StackAdd<T>(List<T> target, List<T> additions) {
    target.Reverse();
    target.AddRange(additions);
    target.Reverse();
  }

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
    Abilities.ForEach(a => { a.AbilityManager = this; a.Init(); });
  }
  void OnDestroy() {
    Abilities.ForEach(a => a.AbilityManager = null);
  }
  // TODO: Should this be manually set high in Script Execution Order instead?
  void LateUpdate() {
    Running.RemoveAll(a => !a.IsRunning);
    StackAdd(Running, ToAdd);
    ToAdd.Clear();
  }
}
