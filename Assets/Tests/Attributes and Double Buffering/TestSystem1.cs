using System.Threading.Tasks;
using UnityEngine;

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

  /*
  If you want to apply a persistent effect which can be a convenient means
  of describing something that should happen and then be reversed later on,
  you must know that the applied effect you are intending to reverse was actually
  applied.

  There are generally two possible ways to do this:

    1. Get a handle to the modification you have applied
       Remove the modification by this handle.
    2. Design your code such that you only apply
       cleanup when you have actually applied a modification.

  The first approach will

  The second approach will have faster execution as you do not
  need to find the modification by handle.
  */

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