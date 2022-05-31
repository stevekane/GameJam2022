using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mob : MonoBehaviour {
  public MobConfig Config;
  public MobAI AI;

  void Update() {
    if (AI == null) {
      // TODO: this is dumb
      if (Config.AI == MobAIType.Wander)
        AI = new MobAIWander(this);
    } else {
      AI.Update();
    }
  }
}

public class MobAI {
  public Mob Mob;

  virtual public void Update() { }
}

public class MobAIWander : MobAI {
  enum WanderState { Moving, Waiting };
  WanderState State = WanderState.Waiting;
  double Timer;
  Vector3 Target;

  public MobAIWander(Mob mob) {
    Mob = mob;

    State = WanderState.Waiting;
    Timer = 1;
  }

  // TODO: NavMesh
  override public void Update() {
    Timer -= Time.deltaTime;

    switch (State) {
    case WanderState.Waiting:
      if (Timer <= 0) {
        Target = new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15));
        Mob.transform.rotation.SetLookRotation(Target - Mob.transform.position, Vector3.up);
        State = WanderState.Moving;
      }
      break;
    case WanderState.Moving:
      Vector3 delta = Target - Mob.transform.position;
      if (delta.sqrMagnitude < .1) {
        State = WanderState.Waiting;
        Timer = 1;
      } else {
        Mob.transform.position += Time.deltaTime * Mob.Config.MoveSpeed * delta.normalized;
      }
      break;
    }
  }
}