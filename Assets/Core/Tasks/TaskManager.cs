using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class TaskManager : SingletonBehavior<TaskManager> {
  public static TaskRunner Scheduler => Instance._Scheduler;

  TaskRunner _Scheduler;

  protected override void AwakeSingleton() {
    _Scheduler = new();
  }

  void FixedUpdate() {
    Scheduler.FixedUpdate();
  }
}

public class TaskRunner : TaskScheduler, IDisposable {
  ConcurrentQueue<Task> Tasks = new ConcurrentQueue<Task>();
  TaskCompletionSource<bool> NextTick = new TaskCompletionSource<bool>(TaskCreationOptions.AttachedToParent);
  bool ProcessingItems;
  TaskScope MainScope = new();

  public TaskRunner() {}

  public void Dispose() {
    MainScope.Dispose();
  }

  public void FixedUpdate() {
    ProcessingItems = true;
    try {
      while (Tasks.TryDequeue(out var task))
        WrapExecuteTask(task);
      NextTick.TrySetResult(true);
    } finally {
      ProcessingItems = false;
    }
    NextTick = new TaskCompletionSource<bool>(TaskCreationOptions.AttachedToParent);
  }

  public Task WaitForFixedUpdate() {
    return NextTick.Task;
  }

  // Queues up a task to begin on the next FixedUpdate tick.
  public void PostTask(TaskFunc f) {
    MainScope.Start(f, this);
  }
  // Starts a task immediately, with continuations handled by this TaskRunner.
  public void RunTask(TaskFunc f) {
    var task = Task.CompletedTask.ContinueWith(t => MainScope.Run(f), this);
    WrapExecuteTask(task);
  }

  public void StopAllTasks() {
    MainScope?.Dispose();
    MainScope = new();
  }

  protected override void QueueTask(Task task) {
    Tasks.Enqueue(task);
  }

  protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
    // If this thread isn't already processing a task, we don't support inlining
    if (!ProcessingItems) return false;

    // If the task was previously queued, remove it from the queue.
    if (taskWasPreviouslyQueued) {
      Debug.Assert(false, "Matt needs to handle this case apparently");
      //if (Tasks.Remove(task))
      //  return TryExecuteTask(task);
      //else
      return false;
    } else {
      return WrapExecuteTask(task);
    }
  }

  protected override IEnumerable<Task> GetScheduledTasks() {
    return Tasks.ToArray();
  }

  bool WrapExecuteTask(Task task) {
    var old = SynchronizationContext.Current;
    try {
      // This dumb hack is necessary because .NET will ignore the current TaskScheduler if there's a SynchronizationContext
      // (which Unity controls). If it's null, .NET will use our current TaskScheduler when awaiting. Another option
      // is to replace Unity's SynchronizationContext with a custom one, but I don't know how to do that.
      SynchronizationContext.SetSynchronizationContext(null);
      return TryExecuteTask(task);
    } catch (Exception e) {
      Debug.LogException(e);
    } finally {
      SynchronizationContext.SetSynchronizationContext(old);
    }
    return false;
  }
}