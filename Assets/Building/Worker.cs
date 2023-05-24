using UnityEngine;

public class Worker : MonoBehaviour {
  AIMover Mover;
  Job CurrentJob;
  TaskScope Scope = new();

  public abstract class Job {
    public abstract bool CanStart();
    public abstract TaskFunc Run(Worker worker);
  }

  public class DeliveryJob : Job {
    public IContainer From;
    public IContainer To;
    public ItemAmount Request;

    public override bool CanStart() => From.GetExtractCount(Request.Item) >= Request.Count;
    public override TaskFunc Run(Worker worker) => async scope => {
#if UNITY_EDITOR
      worker.DebugCarry = new ItemAmount { Item = Request.Item, Count = -Request.Count };
#endif
      var dist = 5f;
      worker.Mover.SetDestination(From.Transform.position);
      await scope.Until(() => (worker.transform.position - From.Transform.position).sqrMagnitude < dist.Sqr());
      if (!From.ExtractItem(Request.Item, Request.Count))
        return;
#if UNITY_EDITOR
      worker.DebugCarry = Request;
#endif
      worker.Mover.SetDestination(To.Transform.position);
      await scope.Until(() => (worker.transform.position - To.Transform.position).sqrMagnitude < dist.Sqr());
      To.InsertItem(Request.Item, Request.Count);
      worker.OnJobDone(this);
    };
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
  void OnGUI() {
    if (!WorkerManager.Instance.DebugDraw)
      return;
    if (CurrentJob != null && DebugCarry?.Item != null) {
      if (DebugCarry.Count < 0) return; // TODO?
      string ToString(ItemAmount a) => $"{a.Item.name}:{a.Count}";
      GUIExtensions.DrawLabel(transform.position, ToString(DebugCarry));
    }
  }
#endif
}