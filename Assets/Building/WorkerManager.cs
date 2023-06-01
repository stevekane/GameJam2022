using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorkerManager : MonoBehaviour {
  public static WorkerManager Instance;
  public bool DebugDraw = false;

  List<Worker> Workers = new();
  List<Worker> IdleWorkers = new();
  List<Worker.Job> JobQueue = new();

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

  public void OnContainerChanged(Container container) {
    AssignJobs();
  }

  public void AddDeliveryJob(IContainer from, IContainer to, ItemAmount request) {
    JobQueue.Add(new Worker.DeliveryJob { From = from, To = to, Request = request });
    AssignJobs();
  }

  void AssignJobs() {
    while (IdleWorkers.Count != 0 && StartableJob() is var job && job != null) {
      var worker = IdleWorkers[0];
      IdleWorkers.RemoveAt(0);
      worker.AssignJob(job);
      JobQueue.Remove(job);
    }
  }

  Worker.Job StartableJob() => JobQueue.FirstOrDefault(j => j.CanStart());

  void OnGUI() {
    if (!DebugDraw)
      return;
    foreach (var job in JobQueue)
      job.OnGUI();
  }
}