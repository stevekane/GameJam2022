using System.Collections;
using UnityEngine;

public class AbilityMan : MonoBehaviour {
  /*
  This class my be sub-classed to create arbitrary abilities by providing
  overrides for CanStart, CanStop, OnStart, OnStop, and Step.
  */
  abstract class Ability {
    public bool IsComplete { get; private set; }
    public void Start() {
      OnStart();
      IsComplete = false;
    }
    public void Stop() {
      OnStop();
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
    public virtual void OnStart() {}
    public virtual void OnStop() {}
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
    public override void OnStart() {
      Owner.StartCoroutine(Graph());
    }
  }

  /*
  Extends from Ability and this uses Steps explicitly to check L1 presses
  as long as L2 is continually held down.
  */
  class ShoutKey : Ability {
    public override void OnStart() {
      Debug.Log("Begin shouting");
    }
    public override void OnStop() {
      Debug.Log("End shouting");
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
      Debug.Log("Preparing to should in 3 seconds...");
      yield return new WaitForSeconds(3);
      Debug.Log("SHOUT");
      Stop();
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
    Abilities = new Ability[2] {
      new ShoutKey(),
      new WaitNShout { Owner = this }
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
      }
    }
    CurrentAbility?.Step(Inputs.Action, Time.fixedDeltaTime);
  }
}