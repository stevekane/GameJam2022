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

public interface IAbility : IStoppable {
  public AbilityManager AbilityManager { get; set; }
  public Attributes Attributes { get; }
  public Status Status { get; }
  public Mover Mover { get; }
  public AbilityTag Tags { get; set; }
  public void StartRoutine(Fiber routine);
  public void StopRoutine(Fiber routine);
  public TriggerCondition GetTriggerCondition(AbilityMethod method);
}

interface IScorable {
  public float Score();
}

// Mechanism to simulate try/finally with using statements. Intended for Ability cleanup code.
// Given an IEnumerator routine, yields that same routine in normal exeuction, or runs it asynchronously
// in a bundle when disposed. Example usage:
//   AbilityMethod() {
//     var cleanup = Finally(() => Animator.Run(ResetClip));
//     ...
//     yield return cleanup;
//   }
// This will run the cleanup code synchronously in the normal case, or asynchronously in Ability.OnStop
// using the parent (AbilityManager) Bundle.
public interface IFinally : IDisposable, IEnumerator { }
class CFinally : IFinally {
  public Bundle Bundle;
  public Func<IEnumerator> OnDispose;
  IEnumerator Routine;
  IEnumerator MakeRoutine() {
    Routine = OnDispose();
    OnDispose = null;
    return Routine;
  }
  public void Dispose() {
    if (OnDispose != null) Bundle.StartRoutine(MakeRoutine);
  }
  public object Current => null;
  public void Reset() => throw new NotImplementedException();
  public bool MoveNext() {
    if (OnDispose != null) MakeRoutine();
    return Routine.MoveNext();
  }
}

[Serializable]
public abstract class Ability : MonoBehaviour, IAbility {
  protected Bundle Bundle = new();
  protected List<IDisposable> Disposables = new();
  public AbilityManager AbilityManager { get; set; }
  public Attributes Attributes { get => AbilityManager.GetComponent<Attributes>(); }
  public Status Status { get => AbilityManager.GetComponent<Status>(); }
  public Mover Mover { get => AbilityManager.GetComponent<Mover>(); }
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
  public IFinally Finally(Func<IEnumerator> func) => Using(new CFinally() { OnDispose = func, Bundle = AbilityManager.Bundle });
  public StatusEffect AddStatusEffect(StatusEffect effect, OnEffectComplete onComplete = null) => Status.Add(Using(effect), onComplete);
  public Listener ListenFor(IEventSource evt) => Fiber.ListenFor(evt);
  public Listener<T> ListenFor<T>(IEventSource<T> evt) => Fiber.ListenFor(evt);
  public Listener ListenFor(AbilityMethod method) => Fiber.ListenFor(AbilityManager.GetEvent(method));
  public void Stop() {
    OnStop();
    Tags = 0;
    Bundle.Stop();
    Disposables.ForEach(s => s.Dispose());
    Disposables.Clear();
  }
  public virtual void OnStop() { }
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerConditionsMap.GetValueOrDefault(method, TriggerCondition.Empty);
  public virtual void FixedUpdate() {
    var isRunning = Bundle.IsRunning;
    var stillRunning = Bundle.MoveNext();
    if (isRunning && !stillRunning) {
      Stop();
    }
  }
  public virtual void Awake() => TriggerConditions.ForEach(c => TriggerConditionsMap[c.Method.GetMethod(this)] = c);
  public virtual void OnDestroy() => Stop();
}

public abstract class FiberAbility : MonoBehaviour, IAbility, IEnumerator, IScorable {
  public AbilityManager AbilityManager { get; set; }
  public BlackBoard BlackBoard => AbilityManager.GetComponent<BlackBoard>();
  public Attributes Attributes => AbilityManager.GetComponent<Attributes>();
  public Status Status => AbilityManager.GetComponent<Status>();
  public Mover Mover => AbilityManager.GetComponent<Mover>();
  public AbilityTag Tags { get; set; }
  public void StartRoutine(Fiber routine) => Enumerator = routine;
  public void StopRoutine(Fiber routine) => Enumerator = null;
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerCondition.Empty;
  public object Current { get => Enumerator.Current; }
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    if (Enumerator != null) {
      if (Enumerator.MoveNext()) {
        return true;
      } else {
        Stop();
        return false;
      }
    } else {
      return false;
    }
  }
  public void Stop() {
    Enumerator = null;
    OnStop();
  }
  public bool IsRunning { get; set; }
  public abstract void OnStop();
  public IEnumerator Enumerator;
  public abstract IEnumerator Routine();
  public abstract float Score();
}

// TODO: Experiment in-progress... possibly delete
public abstract class CoroutineJob : IEnumerator, IStoppable {
  public abstract void OnStart();
  public abstract void OnStop();
  public abstract void OnCancel();
  public object Current => Coroutine?.Current;
  public void Reset() => throw new NotSupportedException();
  public bool MoveNext() {
    if (Coroutine != null) {
      if (Coroutine.MoveNext()) {
        return true;
      } else {
        Stop();
        return false;
      }
    } else {
      return false;
    }
  }
  public bool IsRunning => Coroutine != null;
  public void Stop() {
    Coroutine = null;
    OnStop();
  }
  public void Cancel() {
    Coroutine = null;
    OnCancel();
  }
  IEnumerator Coroutine;
}