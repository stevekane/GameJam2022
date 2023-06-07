using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Worker : MonoBehaviour {
  [ShowInInspector] Job CurrentJob;
  TaskScope Scope = new();

  AIMover Mover;
  [NonSerialized] public Inventory Inventory;

  public abstract class Job {
    public abstract bool CanStart();
    public abstract TaskFunc<Job> Run(Worker worker);
    public virtual void OnGUI() { }
    public void Cancel() {
      RunScope?.Cancel();
      WorkerManager.Instance.OnWorkerJobCancelled(this);
    }
    internal TaskScope RunScope;  // only valid once it is started.
  }
  bool TargetInRange(Transform target, float range) {
    var delta = (target.position - transform.position);
    return delta.y < range && delta.XZ().sqrMagnitude < range.Sqr();
  }
  Vector3 ChaseTarget(Transform target, float desiredDist) {
    var delta = target.position.XZ() - transform.position.XZ();
    return transform.position + delta - (desiredDist - Mover.stoppingDistance) * delta.normalized;
  }
  public async Task MoveTo(TaskScope scope, Transform target) {
    const float desiredDist = 3.5f;
    await scope.Any(
      Waiter.Until(() => TargetInRange(target, desiredDist)),
      Waiter.Repeat(async s => {
        Mover.SetDestination(ChaseTarget(target, desiredDist));
        await s.Millis(200);
      }));
  }

  public void AssignJob(Job job) {
    Debug.Assert(CurrentJob == null);
    CurrentJob = job;
    Scope.Start(RunCurrentJob);
  }
  async Task RunCurrentJob(TaskScope scope) {
    while (CurrentJob != null) {
      try {
        WorkerManager.Instance.OnWorkerJobStarted(CurrentJob);
        await scope.RunChild(async s => {
          CurrentJob.RunScope = s;
          var nextJob = await CurrentJob.Run(this)(s);
          WorkerManager.Instance.OnWorkerJobDone(CurrentJob);
          CurrentJob = nextJob;
        });
      } catch {
        Debug.Log($"Worker: Running job was cancelled.");
        CurrentJob = null;
      }
    }
    WorkerManager.Instance.OnWorkerIdle(this);
  }

  void Awake() {
    this.InitComponent(out Mover);
    this.InitComponent(out Inventory);
  }
  void Start() {
    WorkerManager.Instance.OnWorkerCreated(this);
  }
  void OnDestroy() {
    WorkerManager.Instance.OnWorkerDestroyed(this);
    Scope.Dispose();
  }

  void FixedUpdate() {
    if (CurrentJob != null) {
      Mover.SetMoveFromNavMeshAgent();
      Mover.SetAimFromNavMeshAgent();
    } else {
      Mover.SetMove(Vector3.zero);
    }
  }

#if UNITY_EDITOR
  void OnGUI() {
    if (!WorkerManager.Instance.DebugDraw)
      return;
    string ToString(Dictionary<ItemProto, int> queue) => string.Join("\n", queue.Select(kvp => $"{kvp.Key.name}:{kvp.Value}"));
    if (CurrentJob != null) {
      GUIExtensions.DrawLabel(transform.position, ToString(Inventory.Contents));
    }
  }
#else
  public string DebugState { set { } }
#endif
}