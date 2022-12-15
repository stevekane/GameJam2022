using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public delegate Task AbilityMethod(TaskScope scope);

[Serializable]
public class TriggerCondition {
  internal static TriggerCondition Empty = new();
  internal static TriggerCondition BlockIfNotRunning = new() { Tags = AbilityTag.BlockIfNotRunning };
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
  Dictionary<AbilityMethod, TriggerCondition> TriggerConditionsMap = new();
  [HideInInspector] public AbilityTag Tags;  // Inherited from the Trigger when started
  public virtual float Score() => 0;
  public virtual bool IsRunning { get => ActiveTasks.Count > 0; }
  TaskScope MainScope = new();
  List<AbilityMethod> ActiveTasks = new();  // TODO(Task): could just be a refcount instead?
  public void MaybeStartTask(AbilityMethod func) => MainScope.Start(s => TaskRunner(s, func));
  public async Task TaskRunner(TaskScope scope, AbilityMethod func) {
    try {
      ActiveTasks.Add(func);
      scope.ThrowIfCancelled();
      if (func(scope) is var task && task != null)  // Can return null if used only as a key for the event.
        await task;
    } catch (OperationCanceledException) {
    } finally {
      ActiveTasks.Remove(func);
      if (ActiveTasks.Count == 0)
        Stop(); // TODO: Don't think this is right.. in the cancellation case, this will be called twice.
    }
  }
  public TaskFunc ListenFor(AbilityMethod method) => s => s.ListenFor(AbilityManager.GetEvent(method));
  // Main entry points, bound to JustDown/JustUp for player.
  public virtual Task MainAction(TaskScope scope) => null;
  public virtual Task MainRelease(TaskScope scope) => null;
  public TriggerCondition GetTriggerCondition(AbilityMethod method) => TriggerConditionsMap.GetValueOrDefault(method, TriggerCondition.Empty);
  public void Stop() {
    LegacyStop();
    Tags = 0;
    MainScope.Dispose();
    MainScope = new();
  }
  protected virtual void LegacyStop() { }
  public virtual void Awake() {
    TriggerConditionsMap[MainAction] = TriggerConditions.Count > 0 ? TriggerConditions[0] : new();
    TriggerConditionsMap[MainRelease] = TriggerCondition.BlockIfNotRunning;
    MainScope = new();
  }
  public virtual void OnDestroy() => Stop();
}