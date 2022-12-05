using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/*
Suspending functions are state machines which react to an event, do something, then
yield a value to their owning state machine.

Let us use a task-like syntax to illustrate the equivalence:

Task f(bool flipflop)
  await g()
  print flipflop
  f(!flipflop)

Task g()
  sleep(10)

f is infinitely recursive so we expect the following behavior from f(true)
  repeat
    sleep(10)
    print(true)
    sleep(10)
    print(false)

f and g can also be thought of as a function/state pairs:
sleep is a platform builtin state

Status : Running + Complete
SleepState : Status
Fstate : Running × bool × GState
Gstate = Status × SleepState

f (Running * flipflop Running) ≡
  Running * flipflop (g Running)
f (Running * flipflop * Complete) ≡
  Log flipflop
  Running * ¬flipflop * Running

g (Running * Complete) ≡ Complete * Complete
g state ≡ state

Repeatedly running Run(f) recursively should produce the same behavior as the above Tasks.
*/

public class JobsTests : MonoBehaviour {
  interface ITask {
    public bool Complete { get; }
  }

  class Sleep : ITask {
    int Milliseconds;
    public bool Complete { get; private set; }
    public Sleep(int milliseconds) {
      Milliseconds = milliseconds;
      Complete = false;
      Task.Run(Run);
    }
    async Task Run() {
      await Task.Delay(Milliseconds);
      Complete = true;
    }
  }

  class G : ITask {
    public bool Complete { get; private set; }
    public Sleep Sleep;
    public G() => (Complete, Sleep) = (false, new Sleep(1000));
    public G(bool complete, Sleep sleep) => (Complete, Sleep) = (complete, sleep);
    public static G Run(G state) {
      switch (state) {
        case G { Complete: false, Sleep: Sleep { Complete: true } }:
          return new (true, state.Sleep);
        default:
          return state;
      }
    }
  }

  class F : ITask {
    public bool Complete { get; private set; }
    public bool FlipFlop;
    public G G;
    public F(bool ff) => (Complete, FlipFlop, G) = (false, ff, new());
    public F(bool complete, bool ff, G g) => (Complete, FlipFlop, G) = (complete, ff, g);
    public static F Run(F state) {
      switch (state) {
        case F { G: G { Complete: true } }: {
          Debug.Log(state.FlipFlop);
          return new(!state.FlipFlop);
        }
        default: {
          return new(false, state.FlipFlop, G.Run(state.G));
        }
      }
    }
  }

  // IEnumerator Start() {
  //   var f = new F(false);
  //   do {
  //     f = F.Run(f);
  //     yield return null;
  //   } while (!f.Complete);
  // }

  /*
  Is there a fluent API possible for building up Task-like structures that all run in
  some central pool. A key behavior of this system would be that there are a few primitives
  that, when yielded, become somehow enqueued for observation. These are like leaf nodes
  that touch the environment and therefore can be seen as recievers of events.

  What we want is to build a programming language inside C# that has the properties we
  want in our async coroutines.

    1. All tasks execute in a context
    2. All tasks context is passed down to child jobs implicitly
    3. All tasks in a context must complete/cancel before the context completes or cancels

  Context()
    .Wait(10)
    .Do(() => Debug.log("Done"))
  */

  enum BehaviorState { Active, Completed, Canceled }
  abstract class Behavior {
    public BehaviorState State { get; set; }
    public bool Active => State == BehaviorState.Active;
    public abstract void Cancel();
    public abstract BehaviorState Update();
  }

  class Context {}

  class WaitBehavior : Behavior {
    int Ticks;
    public WaitBehavior(int ticks) => Ticks = ticks;
    public override void Cancel() => State = BehaviorState.Canceled;
    public override BehaviorState Update() {
      if (Ticks-- > 0) {
        return State = BehaviorState.Active;
      } else {
        return State = BehaviorState.Completed;
      }
    }
  }

  class DoBehavior : Behavior {
    Action Action;
    public DoBehavior(Action action) => Action = action;
    public override void Cancel() => State = BehaviorState.Canceled;
    public override BehaviorState Update() {
      Action.Invoke();
      return BehaviorState.Completed;
    }
  }

  class AnyBehavior : Behavior {
    Behavior A;
    Behavior B;
    public AnyBehavior(Behavior a, Behavior b) => (A,B) = (a,b);
    public override void Cancel() {
      A.Cancel();
      B.Cancel();
      State = BehaviorState.Canceled;
    }
    public override BehaviorState Update() {
      var astate = A.Active ? A.Update() : A.State;
      var bstate = B.Active ? B.Update() : B.State;
      switch (astate, bstate) {
        case (BehaviorState.Active, BehaviorState.Active):
          return BehaviorState.Active;
        case (BehaviorState.Canceled, BehaviorState.Canceled):
          return BehaviorState.Canceled;
        case (BehaviorState.Completed, BehaviorState.Completed):
          return BehaviorState.Completed;
        case (BehaviorState.Completed, BehaviorState.Canceled):
          return BehaviorState.Completed;
        case (BehaviorState.Canceled, BehaviorState.Completed):
          return BehaviorState.Completed;
        case (BehaviorState.Completed, BehaviorState.Active):
          B.Cancel();
          return BehaviorState.Completed;
        case (BehaviorState.Active, BehaviorState.Completed):
          A.Cancel();
          return BehaviorState.Completed;
        default:
          return BehaviorState.Active;
      }
    }
  }

  class ProcessBehavior : Behavior {
    Process Process;
    public ProcessBehavior(Process process) => Process = process;
    public override void Cancel() => State = BehaviorState.Canceled;
    public override BehaviorState Update() {
      if (Process.MoveNext()) {
        return BehaviorState.Active;
      } else {
        return Process.State;
      }
    }
  }

  class Process : IEnumerator {
    Context Context;
    List<Behavior> Behaviors = new();
    public BehaviorState State;
    public Process(Context context) => Context = context;
    public Process Wait(int ms) {
      Behaviors.Add(new WaitBehavior(ms));
      return this;
    }
    public Process Do(Action f) {
      Behaviors.Add(new DoBehavior(f));
      return this;
    }
    public Process Any(Behavior a, Behavior b) {
      Behaviors.Add(new AnyBehavior(a,b));
      return this;
    }
    public Process Spawn(Process p) {
      Behaviors.Add(new ProcessBehavior(p));
      return this;
    }
    public object Current => new WaitForFixedUpdate();
    public void Reset() => throw new NotSupportedException();
    public bool MoveNext() {
      if (Behaviors.Count > 0) {
        var behavior = Behaviors[0];
        var state = behavior.Update();
        if (state != BehaviorState.Active) {
          Behaviors.RemoveAt(0);
        }
      }
      return Behaviors.Count > 0;
    }
  }

  Process Proc;

  // TODO: Subtle thing: currently, everytime you change to the next job
  // you then wait one tick. This means that if you wait 3 seconds then print
  // the time printed will be 3seconds + 1tick
  // Ideally, these syncronous continuations would be... syncronous
  void Start() {
    Proc = new Process(new Context())
    .Wait(Timeval.FromSeconds(3).Ticks)
    .Do(delegate { Debug.Log($"{Time.time}"); })
    .Any(
      new WaitBehavior(Timeval.FromSeconds(3).Ticks),
      new WaitBehavior(Timeval.FromSeconds(8).Ticks))
    .Do(delegate { Debug.Log($"{Time.time}"); })
    .Spawn(new Process(new Context())
      .Wait(Timeval.FromSeconds(3).Ticks)
      .Do(delegate { Debug.Log($"{Time.time}"); }));
  }

  void FixedUpdate() {
    Proc.MoveNext();
  }
}