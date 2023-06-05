using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorkerManager : MonoBehaviour {
  public static WorkerManager Instance;
  public bool DebugDraw = false;

  List<Worker> Workers = new();
  List<Worker> IdleWorkers = new();
  List<Worker.Job> PendingJobs = new();
  List<Worker.Job> AssignedJobs = new();

  public int NumWorkers => Workers.Count;

  public void OnWorkerCreated(Worker worker) {
    Workers.Add(worker);
    IdleWorkers.Add(worker);
    AssignJobs();
  }
  public void OnWorkerIdle(Worker worker) {
    IdleWorkers.Add(worker);
    AssignJobs();
  }
  public void OnWorkerDestroyed(Worker worker) {
    Workers.Remove(worker);
    IdleWorkers.Remove(worker);
  }
  public void OnWorkerJobDone(Worker.Job job) {
    AssignedJobs.Remove(job);
  }

  public void OnWorkerJobCancelled(Worker.Job job) {
    // Might be pending or in progress.
    PendingJobs.Remove(job);
    AssignedJobs.Remove(job);
  }

  public void OnContainerChanged(Container container) {
    AssignJobs();
  }

  public void AddJob(Worker.Job job) {
    PendingJobs.Add(job);
    AssignJobs();
  }
  public IEnumerable<Worker.Job> GetAllJobs() => PendingJobs.Concat(AssignedJobs);

  void AssignJobs() {
    while (IdleWorkers.Count != 0 && StartableJob() is var job && job != null) {
      var worker = IdleWorkers[0];
      IdleWorkers.RemoveAt(0);
      PendingJobs.Remove(job);
      AssignedJobs.Add(job);
      worker.AssignJob(job);
    }
  }

  Worker.Job StartableJob() => PendingJobs.FirstOrDefault(j => j.CanStart());

  void OnGUI() {
    if (!DebugDraw)
      return;
    foreach (var job in GetAllJobs())
      job.OnGUI();
  }
}