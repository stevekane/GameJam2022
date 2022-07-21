using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class AbilityMan : MonoBehaviour {
  /*
  This class my be sub-classed to create arbitrary abilities by providing
  overrides for CanStart, CanStop, OnStart, OnStop, and Step.
  */
  abstract class Ability {
    public bool IsComplete { get; private set; }
    public virtual void Start() {
      IsComplete = false;
    }
    public virtual void Stop() {
      IsComplete = true;
    }
    public bool TryStart() {
      if (CanStart()) {
        Start();
        return true;
      } else {
        return false;
      }
    }
    public bool TryStop() {
      if (CanStop()) {
        Stop();
        return true;
      } else {
        return false;
      }
    }
    public virtual bool CanStart() => true;
    public virtual bool CanStop() => true;
    public virtual void Step(Action action, float dt) {}
  }

  /*
  This class may be sub-classed to provide an IEnumerator "Graph" which
  will be run as a co-routine. Note, you must provide an "Owning" Monobehavior
  for now... very gross... very gross...
  Eventually, these tasks will themselves be Monobehaviors and this requirement
  will not exist.
  */
  abstract class TaskAbility : Ability {
    public abstract IEnumerator Graph();
    public MonoBehaviour Owner;
    public override void Start() {
      Owner.StartCoroutine(Graph());
    }
  }

  /*
  Extends from Ability and this uses Steps explicitly to check L1 presses
  as long as L2 is continually held down.
  */
  class ShoutKey : Ability {
    public override void Start() {
      Debug.Log("Begin shouting");
      base.Start();
    }
    public override void Stop() {
      Debug.Log("End shouting");
      base.Stop();
    }
    public override void Step(Action action, float dt) {
      if (action.L1.JustDown) {
        Debug.Log("L1");
      } else if (action.L2.JustUp) {
        Stop();
      }
    }
  }

  /*
  Extends from TaskAbility and provides an IEnumerator Graph which prints a message,
  waits 3 seconds, then prints a final message before Stopping.
  */
  class WaitNShout : TaskAbility {
    public override IEnumerator Graph() {
      Debug.Log("Preparing to shout in 3 seconds...");
      yield return new WaitForSeconds(3);
      Debug.Log("SHOUT");
      Stop();
    }
  }

  /*
  What is an async task graph?

  Async tasks are constructed with Constructors based on the data they need.
  Once constructed, an async task has internal logic that runs until it decides
  to exit by calling one of several possible continuations.
  */
  abstract class AbilityTask {
    public bool IsComplete { get; private set; }
    public virtual void Step(AbilityGraph graph) {}
    public virtual void Start(AbilityGraph graph) {
      graph.Tasks.Add(this);
      IsComplete = false;
    }
    public virtual void Stop(AbilityGraph graph) {
      IsComplete = true;
    }
  }

  abstract class AbilityGraph : Ability {
    public List<AbilityTask> Tasks = new List<AbilityTask>();

    public override void Start() {
      base.Start();
    }

    public override void Stop() {
      Tasks.ForEach(t => t.Stop(this));
      base.Stop();
    }

    public override void Step(Action action, float dt) {
      if (IsComplete) {
        Debug.LogError("Tried to step a completed task");
      }

      for (var i = Tasks.Count-1; i >= 0; i--) {
        var t = Tasks[i];
        t.Step(this);
        if (t.IsComplete) {
          Tasks.RemoveAt(i);
        }
      }
      if (Tasks.Count <= 0) {
        Stop();
      }
    }
  }

  public delegate void Continuation();

  class FartAbilityTask : AbilityTask {
    public Continuation PostFart;
    public override void Step(AbilityGraph graph) {
      Debug.Log("Fart");
      Stop(graph);
      PostFart?.Invoke();
    }
  }

  class BurpAbilityTask : AbilityTask {
    public Continuation PostBurp;
    public override void Step(AbilityGraph graph) {
      Debug.Log("Burp");
      Stop(graph);
      PostBurp?.Invoke();
    }
  }

  class WaitAbilityTask : AbilityTask {
    public int Frames;
    public Continuation PostWait;
    public override void Step(AbilityGraph graph) {
      base.Step(graph);
      if (Frames <= 0) {
        Stop(graph);
        PostWait?.Invoke();
      } else {
        Frames--;
      }
    }
  }

  class FartWaitBurpAbility : AbilityGraph {
    public override void Start() {
      void PostFart() {
        void PostWait() {
          new BurpAbilityTask().Start(this);
        }
        new WaitAbilityTask { Frames = 1000, PostWait = PostWait }.Start(this);
      }
      base.Start();
      new FartAbilityTask { PostFart = PostFart }.Start(this);
    }
  }

  Ability[] Abilities;
  Ability CurrentAbility;

  bool TryStartAbility(Ability ability) {
    if (ability.TryStart()) {
      CurrentAbility = ability;
      return true;
    } else {
      return false;
    }
  }

  void Start() {
    Abilities = new Ability[3] {
      new ShoutKey(),
      new WaitNShout { Owner = this },
      new FartWaitBurpAbility()
    };
  }

  void FixedUpdate() {
    var action = Inputs.Action;

    if (CurrentAbility != null && CurrentAbility.IsComplete) {
      CurrentAbility = null;
    }
    if (CurrentAbility == null) {
      if (action.L2.JustDown) {
        TryStartAbility(Abilities[0]);
      } else if (action.R2.JustDown) {
        TryStartAbility(Abilities[1]);
      } else if (action.R1.JustDown) {
        TryStartAbility(Abilities[2]);
      }
    }
    CurrentAbility?.Step(Inputs.Action, Time.fixedDeltaTime);
  }
}