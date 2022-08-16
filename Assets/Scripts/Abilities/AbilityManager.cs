using System;
using System.Collections.Generic;
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
    public EventRouter(Ability ability, AbilityMethod method) => (Ability, Method) = (ability, method);
    public void Listen(Action handler) => Action += handler;
    public void Unlisten(Action handler) => Action -= handler;
    public void Fire() {
      if (ShouldFire()) {
        Action?.Invoke();
        var enumerator = Method();
        if (enumerator != null) {  // Can be null for events that listen temporarily.
          Ability.StartRoutine(new Fiber(enumerator));
          Ability.AbilityManager.ToAdd.Add(Ability);
        }
        var tags = Ability.GetTriggerCondition(Method).Tags;
        foreach (var activeAbility in Ability.AbilityManager.Running) {
          if ((activeAbility.CancelledBy & tags) != 0) {
            activeAbility.Stop();
          }
        }
      }
    }
    bool ShouldFire() {
      var conditions = Ability.GetTriggerCondition(Method);
      var runStateOk = conditions.ActiveIf switch {
        AbilityRunState.Always => true,
        AbilityRunState.AbilityIsRunning => Ability.IsRunning,
        AbilityRunState.AbilityIsNotRunning => !Ability.IsRunning,
        _ => false,
      };
      var isBlocked = Ability.AbilityManager.Running.Exists(a => (a.Blocks & conditions.Tags) != 0);
      return runStateOk && !isBlocked;
    }
  }

  public void StopAllAbilities() => Abilities.ForEach((a) => { if (a.IsRunning) a.Stop(); });

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

  void StackAdd<T>(List<T> target, List<T> additions) {
    target.Reverse();
    target.AddRange(additions);
    target.Reverse();
  }

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
  }
  void Start() {
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
