using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TriggerCondition {
  public AbilityMethodReferenceSelf Method;
  public AbilityTag Tags = 0;
}

[Serializable]
public abstract class Ability : MonoBehaviour {
  protected Bundle Bundle = new();
  protected List<IDisposable> Disposables = new();
  public AbilityManager AbilityManager { get; set; }
  public Status Status { get => AbilityManager.GetComponent<Status>(); }
  public List<TriggerCondition> TriggerConditions = new();
  Dictionary<AbilityMethod, TriggerCondition> TriggerConditionsMap = new();
  [HideInInspector] public AbilityTag Tags; // Inherited from the Trigger when started
  public bool IsRunning { get => Bundle.IsRunning; }
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Bundle.StopRoutine(routine);
  public T Using<T>(T d) where T : IDisposable {
    Disposables.Add(d);
    return d;
  }
  public StatusEffect AddStatusEffect(StatusEffect effect, OnEffectComplete onComplete = null) {
    Status.Add(Using(effect), onComplete);
    return effect;
  }
  public virtual void Stop() {
    Tags = 0;
    Bundle.StopAll();
    Disposables.ForEach(s => s.Dispose());
  }
  public void Init() => TriggerConditions.ForEach(c => TriggerConditionsMap[c.Method.GetMethod(this)] = c);
  public AbilityTag GetTriggerTags(AbilityMethod method) => TriggerConditionsMap.GetValueOrDefault(method, null)?.Tags ?? 0;
  void FixedUpdate() => Bundle.Run();
}