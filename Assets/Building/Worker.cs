using Sirenix.OdinInspector;
using UnityEngine;

public class Worker : MonoBehaviour {
  AIMover Mover;
  [ShowInInspector] Job CurrentJob;
  TaskScope Scope = new();

  public abstract class Job {
    public abstract bool CanStart();
    public abstract TaskFunc Run(Worker worker);
    public virtual void OnGUI() { }
  }
  bool TargetInRange(Transform target, float range) {
    var delta = (target.position - transform.position);
    return delta.y < range && delta.XZ().sqrMagnitude < range.Sqr();
  }
  Vector3 ChaseTarget(Transform target, float desiredDist) {
    var delta = target.position.XZ() - transform.position.XZ();
    return transform.position + delta - (desiredDist - Mover.stoppingDistance) * delta.normalized;
  }
  public class DeliveryJob : Job {
    public IContainer From;
    public IContainer To;
    public ItemAmount Request;

    public override bool CanStart() => From.GetExtractCount(Request.Item) >= Request.Count;
    public override TaskFunc Run(Worker worker) => async scope => {
#if UNITY_EDITOR
      worker.DebugCarry = new ItemAmount { Item = Request.Item, Count = -Request.Count };
      worker.DebugState = $"Pickup {From}->{To} {Request.Item}:{Request.Count}";
#endif
      var dist = 3f;
      worker.Mover.SetDestination(worker.ChaseTarget(From.Transform, dist));
      await scope.Until(() => (worker.TargetInRange(From.Transform, dist)));
      worker.DebugState = $"Arrived {From}->{To} {Request.Item}:{Request.Count}";
      if (!From.ExtractItem(Request.Item, Request.Count)) {
        worker.DebugState = $"Notenough {From}->{To} {Request.Item}:{Request.Count}";
        worker.OnJobDone(this);
        return;
      }
#if UNITY_EDITOR
      worker.DebugState = $"Dropoff {From}->{To} {Request.Item}:{Request.Count}";
      worker.DebugCarry = Request;
#endif
      worker.Mover.SetDestination(worker.ChaseTarget(To.Transform, dist));
      await scope.Until(() => (worker.TargetInRange(To.Transform, dist)));
      worker.DebugState = $"Insert {From}->{To} {Request.Item}:{Request.Count}";
      To.InsertItem(Request.Item, Request.Count);
      worker.OnJobDone(this);
      worker.DebugState = $"Inserted {From}->{To} {Request.Item}:{Request.Count}";
    };
    public override void OnGUI() {
      string ToString(ItemAmount a) => $"{a.Item.name}:{a.Count}";
      var delta = To.Transform.position - From.Transform.position;
      var pos = From.Transform.position + delta*.2f;
      GUIExtensions.DrawLine(From.Transform.position, To.Transform.position, 2);
      GUIExtensions.DrawLabel(pos, ToString(Request));
    }
  }

  public void AssignJob(Job job) {
    Debug.Assert(CurrentJob == null);
    CurrentJob = job;
    Scope.Start(job.Run(this));
  }

  void OnJobDone(Job job) {
    Debug.Assert(CurrentJob == job);
    CurrentJob = null;
    WorkerManager.Instance.OnWorkerIdle(this);
  }

  void Awake() {
    this.InitComponent(out Mover);
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
  public ItemAmount DebugCarry;
  public string DebugState = "";
  void OnGUI() {
    if (!WorkerManager.Instance.DebugDraw)
      return;
    if (CurrentJob != null && DebugCarry?.Item != null) {
      if (DebugCarry.Count < 0) return; // TODO?
      string ToString(ItemAmount a) => $"{a.Item.name}:{a.Count}";
      GUIExtensions.DrawLabel(transform.position, ToString(DebugCarry));
    }
  }
#else
  public string DebugState { set { } }
#endif
}