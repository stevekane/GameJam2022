using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TriggerCondition {
  internal static TriggerCondition Empty = new();
  public AbilityMethodReferenceSelf Method;
  public AbilityTag Tags = 0;
  public AbilityTag RequiredOwnerTags = 0;
  public float EnergyCost = 0f;
  public HashSet<AttributeTag> RequiredOwnerAttribs = new();
  //public AttributeTag[] RequiredOwnerAttribs = { };
}

public interface IAbility : IStoppable, IEquatable<object> {
  public AbilityManager AbilityManager { get; set; }
  public Attributes Attributes { get; }
  public Status Status { get; }
  public AbilityTag Tags { get; set; }
  public void StartRoutine(Fiber routine);
  public void StopRoutine(Fiber routine);
  public TriggerCondition GetTriggerCondition(AbilityMethod method);
}

[Serializable]
public abstract class Ability : MonoBehaviour, IAbility {
  protected Bundle Bundle = new();
  protected List<IDisposable> Disposables = new();
  public AbilityManager AbilityManager { get; set; }
  public Attributes Attributes { get => AbilityManager.GetComponent<Attributes>(); }
  public Status Status { get => AbilityManager.GetComponent<Status>(); }
  public List<TriggerCondition> TriggerConditions = new();
  Dictionary<AbilityMethod, TriggerCondition> TriggerConditionsMap = new();
  [HideInInspector] public AbilityTag Tags { get; set; } // Inherited from the Trigger when started
  public bool IsRunning { get => Bundle.IsRunning; }
  public IEnumerator Running => Bundle;
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Bundle.StopRoutine(routine);
  public T Using<T>(T d) where T : IDisposable {
    Disposables.Add(d);
    return d;
  }
  public T Dispose<T>(T d) where T : IDisposable {
    d.Dispose();
    Disposables.Remove(d);
    return d;
  }
  public StatusEffect AddStatusEffect(StatusEffect effect, OnEffectComplete onComplete = null) {
    Status.Add(Using(effect), onComplete);
    return effect;
  }
  public void Stop() {
    OnStop();
    Tags = 0;
    Bundle.Stop();
    Disposables.ForEach(s => s.Dispose());
    Disposables.Clear();
  }
  public virtual void OnStop() { }
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerConditionsMap.GetValueOrDefault(method, TriggerCondition.Empty);
  void FixedUpdate() {
    var isRunning = Bundle.IsRunning;
    var stillRunning = Bundle.MoveNext();
    if (isRunning && !stillRunning) {
      Stop();
    }
  }
  void Awake() => TriggerConditions.ForEach(c => TriggerConditionsMap[c.Method.GetMethod(this)] = c);
  void OnDestroy() => Stop();
  public override bool Equals(object obj) {
    if (obj == null || GetType() != obj.GetType()) {
      return false;
    } else {
      return base.Equals(obj);
    }
  }
  public override int GetHashCode() {
    return base.GetHashCode();
  }
}