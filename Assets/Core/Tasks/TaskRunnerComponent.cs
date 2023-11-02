using System.Threading.Tasks;
using UnityEngine;

// Ehh not sure if I like this.
[RequireComponent(typeof(LocalTime))]
public class TaskRunnerComponent : MonoBehaviour {
  TaskRunner Scheduler;

  protected virtual void Awake() {
    Scheduler = new();
  }

  protected virtual void OnDestroy() {
    Scheduler.Dispose();
  }

  protected virtual void FixedUpdate() {
    Scheduler.FixedUpdate();
  }

  public Task WaitForFixedUpdate() {
    return Scheduler.WaitForFixedUpdate();
  }

  public void StopAllTasks() {
    Scheduler.StopAllTasks();
  }

  public void RunTask(TaskFunc f) {
    Scheduler.RunTask(f);
  }
}