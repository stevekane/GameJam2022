using System;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityRunState {
  Always,
  AbilityIsRunning,
  AbilityIsNotRunning,
}

[Serializable]
public class TriggerCondition {
  public AbilityMethodReferenceSelf Method;
  public AbilityRunState ActiveIf = AbilityRunState.Always;
  public AbilityTag Tags = 0;
}

[Serializable]
public abstract class Ability : MonoBehaviour {
  static TriggerCondition EmptyCondition = new();
  protected Bundle Bundle = new();
  public AbilityManager AbilityManager { get; set; }
  public List<TriggerCondition> TriggerConditions = new();
  Dictionary<AbilityMethod, TriggerCondition> TriggerConditionsMap = new();
  public AbilityTag CancelledBy;
  public AbilityTag Blocks;
  public bool IsRunning { get => Bundle.IsRunning; }
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Bundle.StopRoutine(routine);
  public virtual void Stop() => Bundle.StopAll();
  public void Init() => TriggerConditions.ForEach(c => TriggerConditionsMap[c.Method.GetMethod(this)] = c);
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerConditionsMap.GetValueOrDefault(method, EmptyCondition);
  void FixedUpdate() => Bundle.Run();
}