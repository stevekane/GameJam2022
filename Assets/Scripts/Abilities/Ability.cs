using System;
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
}

[Serializable]
[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public abstract class Ability : MonoBehaviour {
  public Character Character { get; set; }
  protected TaskRunner TaskRunner = new();
  public AbilityManager AbilityManager => Character?.AbilityManager;
  public AvatarAttacher AvatarAttacher => Character?.AvatarAttacher;
  public AnimationDriver AnimationDriver => Character?.AnimationDriver;
  public BlackBoard BlackBoard => Character?.GetComponent<BlackBoard>();
  public Attributes Attributes => Character?.Attributes;
  public Status Status => Character?.Status;
  public Mover Mover => Character?.Mover;
  public TriggerCondition TriggerCondition = new();
  [HideInInspector] protected AbilityTag Tags;  // Inherited from the Trigger when started
  List<AbilityMethod> ActiveTasks = new();  // TODO(Task): could just be a refcount instead?

  public virtual AbilityTag ActiveTags => Tags;
  public virtual float Score() => 0;
  public virtual bool IsRunning => ActiveTasks.Count > 0;
  public virtual HitConfig HitConfigData => null;

  public virtual bool CanStart(AbilityMethod func) => true;
  public void Run(AbilityMethod func) => TaskRunner.RunTask(s => Runner(s, func));
  public Task Run(TaskScope scope, AbilityMethod func) => Runner(scope, func);
  async Task Runner(TaskScope scope, AbilityMethod func) {
    try {
      ActiveTasks.Add(func);
      var trigger = GetTriggerCondition(func);
      Tags.AddFlags(trigger.Tags);
      scope.ThrowIfCancelled();
      AbilityManager.GetEvent(func).Fire();
      if (func(scope) is var task && task != null) { // Can return null if used only as a key for the event.
        // Well this is hella specific. Is there a more general way to handle this?
        var uninterruptible = trigger.Tags.HasFlag(AbilityTag.Uninterruptible);
        using var uninterruptibleEffect = uninterruptible ? Status.Add(new UninterruptibleEffect()) : null;
        if (task.IsFaulted) {
          throw task.Exception; // Not sure why this sometimes doesn't happen simply by awaiting the task.
        } else {
          await task;
        }
      }
    } catch (OperationCanceledException) {
    } catch (Exception ex) {
      // TODO: Figure out how to make it so these exceptions aren't swallowed. Run() drops the Task on the floor, but I
      // don't know how to pull the exception out.
      Debug.LogException(ex, this);
      throw ex;
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
  public void SetCancellable() => Tags.AddFlags(AbilityTag.Cancellable);
  // TODO(HACK): InteractAbility needs to override this to provide the Release action with the Interact tag.
  public virtual TriggerCondition GetTriggerCondition(AbilityMethod method) =>
      method == MainAction ? TriggerCondition : TriggerCondition.Empty;

  public void Stop() {
    Tags = 0;
    TaskRunner.StopAllTasks();
  }
  protected virtual void OnDestroy() {
    TaskRunner.Dispose();
  }
  protected virtual void FixedUpdate() {
    TaskRunner.FixedUpdate();
  }
}