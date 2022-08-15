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
  Dictionary<AbilityMethod, EventSource> MethodToEvent = new();
  //Dictionary<EventSource, EventIntercept> s = new();

  //class EventIntercept : IEventSource {
  //  EventSource Source;
  //  public System.Action Action;
  //  public List<IEventSource> Connected { get => null; set { } }
  //  public void Fire() {
  //    Action?.Invoke();
  //  }
  //}

  void Awake() {
    Abilities = GetComponentsInChildren<Ability>();
  }
  void Start() {
    Abilities.ForEach(a => a.AbilityManager = this);
  }
  void OnDestroy() {
    Abilities.ForEach(a => a.AbilityManager = null);
  }

  public void StopAllAbilities() => Abilities.ForEach((a) => a.Stop());

  public void RegisterAxis(AxisTag tag, AxisState axis) {
    TagToAxis[tag] = axis;
  }
  public void RegisterTrigger(AbilityMethod method, EventSource evt) {
    ConnectEvent(method, evt);
  }
  public AxisState GetAxis(AxisTag tag) {
    if (!TagToAxis.TryGetValue(tag, out AxisState axis))
      TagToAxis[tag] = axis = new();
    return axis;
  }
  public EventSource GetEvent(AbilityMethod method) {
    if (!MethodToEvent.TryGetValue(method, out EventSource evt))
      ConnectEvent(method, evt = new());
    return evt;
  }
  void ConnectEvent(AbilityMethod method, EventSource evt) {
    MethodToEvent[method] = evt;
    // Hmmmm... This only works for dispatching events. ListenFor (and other things that connect directly to the EventSource)
    // bypass all this machinery.
    Action handler = () => DispatchEvent((Ability)method.Target, method);
    evt.Listen(handler); // TODO: how to remove?
  }
  void DispatchEvent(Ability ability, AbilityMethod method) {
    // TODO: This should be per-trigger not per-ability
    // TODO: how to detect already running? the fiber is recreated for each dispatch
    //var alreadyRunning = Fiber != null && Ability.IsRoutineRunning(Fiber);
    var notBlocked = Running.TrueForAll(a => (a.Blocks & ability.Tags) == 0);
    if (notBlocked) {
      var enumerator = method();
      if (enumerator == null)
        return;  // Just a method tag
      ability.StartRoutine(new Fiber(enumerator));
      ToAdd.Add(ability);
      foreach (var activeAbility in Running) {
        if ((activeAbility.Cancels & ability.Tags) != 0) {
          activeAbility.Stop();
        }
      }
    }
  }
  // TODO: Should this be manually set high in Script Execution Order instead?
  void LateUpdate() {
    Running.RemoveAll(a => !a.IsRunning);
    StackAdd(Running, ToAdd);
    ToAdd.Clear();
  }

  void StackAdd<T>(List<T> target, List<T> additions) {
    target.Reverse();
    target.AddRange(additions);
    target.Reverse();
  }
}
