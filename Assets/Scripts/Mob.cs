using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Mob : MonoBehaviour {
  public MobConfig Config;
  public MobAI AI;

  void Update() {
    if (AI == null) {
      // TODO: this is dumb
      if (Config.AI == MobAIType.Wander) AI = new MobAIStationary(this);
      else if (Config.AI == MobAIType.Wander) AI = new MobAIWander(this);
    } else {
      AI.Update();
    }
  }
}

public class MobAI {
  public Mob Mob;

  virtual public void Update() { }
}

public class MobAIStationary : MobAI {
  public MobAIStationary(Mob mob) {
    Mob = mob;
  }

  override public void Update() {
  }
}

public class MobAIWander : MobAI {
  enum WanderState { Moving, Waiting };
  WanderState State = WanderState.Waiting;
  NavMeshAgent Agent;
  double Timer;

  public MobAIWander(Mob mob) {
    Mob = mob;

    State = WanderState.Waiting;
    Timer = 1;

    Agent = Mob.GetComponent<NavMeshAgent>();
    Agent.speed = Mob.Config.MoveSpeed;
  }

  override public void Update() {
    Timer -= Time.deltaTime;

    switch (State) {
    case WanderState.Waiting:
      if (Timer <= 0) {
        State = WanderState.Moving;
        Agent.SetDestination(new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15)));
      }
      break;
    case WanderState.Moving:
      Vector3 delta = Agent.destination - Mob.transform.position;
      if (delta.sqrMagnitude < .1) {
        State = WanderState.Waiting;
        Timer = 1;
      }
      break;
    }
  }
}