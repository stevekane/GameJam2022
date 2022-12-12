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
  protected Bundle Bundle = new();
  protected List<IDisposable> Disposables = new();
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
  public bool IsRunning { get => Bundle.IsRunning || ActiveTasks.Count > 0; }
  public IEnumerator Running => Bundle;
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Bundle.StopRoutine(routine);
  // Adapter to run a Task using Fiber tech. Temporary.
  // TODO: if/when this goes away, we'll need a Task-only notion of IsRunning. Should we keep track of active tasks somehow?
  TaskScope MainScope = new();
  List<AbilityMethodTask> ActiveTasks = new();  // TODO(task): could just be a refcount instead?
  public void MaybeStartTask(AbilityMethodTask func) {
    var task = new Task(async () => {
      using TaskScope scope = new(MainScope);
      try {
        scope.ThrowIfCancelled();
        var result = func(scope);
        if (result != null)
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
  public T Using<T>(T d) where T : IDisposable {
    Disposables.Add(d);
    return d;
  }
  public StatusEffect AddStatusEffect(StatusEffect effect, OnEffectComplete onComplete = null) => Status.Add(Using(effect), onComplete);
  public Listener FiberListenFor(IEventSource evt) => Fiber.ListenFor(evt);
  public Listener<T> FiberListenFor<T>(IEventSource<T> evt) => Fiber.ListenFor(evt);
  public Listener FiberListenFor(AbilityMethod method) => Fiber.ListenFor(AbilityManager.GetEvent(method));
  public void Stop() {
    OnStop();
    Tags = 0;
    Bundle.Stop();
    Disposables.ForEach(s => s.Dispose());
    Disposables.Clear();
    MainScope.Dispose();
    MainScope = new();
  }
  public virtual void OnStop() { }
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerConditionsMap.GetValueOrDefault(method, TriggerCondition.Empty);
  public TriggerCondition GetTriggerCondition(AbilityMethodTask method) => TriggerConditionsMap.GetValueOrDefault(method, TriggerCondition.Empty);
  void Start() {
    MainScope = new();
  }
  public virtual void FixedUpdate() {
    var isRunning = Bundle.IsRunning;
    var stillRunning = Bundle.MoveNext();
    if (isRunning && !stillRunning) {
      Stop();
    }
  }
  public virtual void Awake() => TriggerConditions.ForEach(c => TriggerConditionsMap[c.Method.IsTask(this) ? c.Method.GetMethodTask(this) : c.Method.GetMethod(this)] = c);
  public virtual void OnDestroy() => Stop();
}