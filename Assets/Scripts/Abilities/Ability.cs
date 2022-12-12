using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class TriggerCondition {
  internal static TriggerCondition Empty = new();
  public AbilityMethodReferenceSelf Method;
  public AbilityTag Tags = 0;
  public AbilityTag RequiredOwnerTags = 0;
  public float EnergyCost = 0f;
  public HashSet<AttributeTag> RequiredOwnerAttribs = new();
}

[Serializable]
public abstract class Ability : MonoBehaviour {
  public AbilityManager AbilityManager { get; set; }
  public AnimationDriver AnimationDriver => AbilityManager.GetComponent<AnimationDriver>();
  public BlackBoard BlackBoard => AbilityManager.GetComponent<BlackBoard>();
  public Attributes Attributes => AbilityManager.GetComponent<Attributes>();
  public Status Status => AbilityManager.GetComponent<Status>();
  public Mover Mover => AbilityManager.GetComponent<Mover>();
  public List<TriggerCondition> TriggerConditions = new();
  // TODO(Task): object => AbilityMethodTask
  Dictionary<object, TriggerCondition> TriggerConditionsMap = new();
  [HideInInspector] public AbilityTag Tags;  // Inherited from the Trigger when started
  public virtual float Score() => 0;
  public virtual bool IsRunning { get => ActiveTasks.Count > 0; }
  TaskScope MainScope = new();
  List<AbilityMethodTask> ActiveTasks = new();  // TODO(task): could just be a refcount instead?
  public void MaybeStartTask(AbilityMethodTask func) {
    var task = new Task(async () => {
      using TaskScope scope = new(MainScope);
      try {
        scope.ThrowIfCancelled();
        var result = func(scope);
        if (result != null)  // Can return null if used only as a key for the event.
          await result;
      } catch (OperationCanceledException) {
      } catch (Exception e) {
        Debug.LogError($"Exception during Ability {this}: {e}");
      } finally {
        ActiveTasks.Remove(func);
        if (ActiveTasks.Count == 0)
          Stop(); // TODO: Don't think this is right.. in the cancellation case, this will be called twice.
      }
    });
    ActiveTasks.Add(func);
    TaskScope.Start(task);
  }
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerConditionsMap.GetValueOrDefault(method, TriggerCondition.Empty);
  public TriggerCondition GetTriggerCondition(AbilityMethodTask method) => TriggerConditionsMap.GetValueOrDefault(method, TriggerCondition.Empty);
  public void Stop() {
    LegacyStop();
    Tags = 0;
    MainScope.Dispose();
    MainScope = new();
  }
  protected virtual void LegacyStop() { }
  public virtual void Awake() {
    TriggerConditions.ForEach(c => TriggerConditionsMap[c.Method.IsTask(this) ? c.Method.GetMethodTask(this) : c.Method.GetMethod(this)] = c);
    MainScope = new();
  }
  public virtual void OnDestroy() => Stop();
}

// Legacy support for old Fiber-based Abilities.
[Serializable]
public abstract class LegacyAbility : Ability {
  protected Bundle Bundle = new();
  public override bool IsRunning { get => Bundle.IsRunning; }
  public IEnumerator Running => Bundle;
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public Listener FiberListenFor(IEventSource evt) => Fiber.ListenFor(evt);
  public Listener<T> FiberListenFor<T>(IEventSource<T> evt) => Fiber.ListenFor(evt);
  public Listener FiberListenFor(AbilityMethod method) => Fiber.ListenFor(AbilityManager.GetEvent(method));
  protected List<IDisposable> Disposables = new();
  public T Using<T>(T d) where T : IDisposable {
    Disposables.Add(d);
    return d;
  }
  public StatusEffect AddStatusEffect(StatusEffect effect, OnEffectComplete onComplete = null) => Status.Add(Using(effect), onComplete);
  public virtual void OnStop() { }
  protected override void LegacyStop() {
    OnStop();
    Bundle.Stop();
    Disposables.ForEach(s => s.Dispose());
    Disposables.Clear();
  }
  public virtual void FixedUpdate() {
    var isRunning = Bundle.IsRunning;
    var stillRunning = Bundle.MoveNext();
    if (isRunning && !stillRunning) {
      Stop();
    }
  }
}