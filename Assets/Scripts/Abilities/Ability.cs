using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public class AbilityTrigger {
  public EventTag EventTag;
  public string MethodName;
  Action Handler;
  EventSource EventSource;

  public void Init(AbilityManager user, Ability ability) {
    EventSource = user.GetEvent(EventTag);
    EventSource.Action += EventAction;
    var method = ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
    Handler = () => ability.StartRoutine(new Fiber((IEnumerator)method.Invoke(ability, null)));
  }

  public void Destroy() {
    EventSource.Action -= EventAction;
  }

  void EventAction() {
    Handler?.Invoke();
  }
}

public abstract class Ability : MonoBehaviour {
  Bundle Routines = new();
  public List<AbilityTrigger> Triggers;
  public AbilityTag Tags;
  public AbilityTag Cancels;
  public AbilityTag Blocks;
  public bool IsRunning { get => Routines.IsRunning; }
  public bool IsFiberRunning(Fiber f) => Routines.IsFiberRunning(f);
  public virtual void Activate() => Routines.StartRoutine(new Fiber(MakeRoutine()));
  public virtual void Stop() => Routines.StopAll();
  protected virtual IEnumerator MakeRoutine() { yield return null; } // TODO remove
  public void StartRoutine(Fiber routine) => Routines.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Routines.StopRoutine(routine);
  void FixedUpdate() => Routines.Run();
}
