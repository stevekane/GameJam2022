using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
public class TestSystem1 : SampleAbility {
  [SerializeField] BooleanAttribute CanMove;
  [SerializeField] SampleAction StartAction;
  [SerializeField] SampleAction ReleaseAction;

  TaskScope Scope;

  public override bool IsRunning { get; protected set; }

  public void Perform() {
    Scope = new();
    Scope.Run(Run);
  }

  public override void Stop() {
    Scope.Dispose();
    Scope = null;
  }

  async Task Run(TaskScope scope) {
    try {
      IsRunning = true;
      Debug.Log("System locking Motion");
      CanMove.Next.Set(false);
      StartAction.Satisfied = false;
      ReleaseAction.Satisfied = true;
      await scope.ListenFor(ReleaseAction.EventSource);
      ReleaseAction.Satisfied = false;
      Debug.Log("Released!!!!!");
    } finally {
      Debug.Log("System stopped. freeing Motion");
      StartAction.Satisfied = true;
      ReleaseAction.Satisfied = false;
      IsRunning = false;
      CanMove.Next.Set(true);
    }
  }
}