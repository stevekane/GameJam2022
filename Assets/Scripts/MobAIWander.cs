using UnityEngine;
using UnityEngine.AI;

public class MobAIWander : MonoBehaviour {
  enum WanderState { Moving, Waiting };
  WanderState State = WanderState.Waiting;
  NavMeshAgent Agent;
  double Timer;

  void Start() {
    State = WanderState.Waiting;
    Timer = 1;

    Agent = GetComponent<NavMeshAgent>();
    Agent.speed = GetComponent<Mob>().Config.MoveSpeed;
  }

  void Update() {
    Timer -= Time.deltaTime;

    switch (State) {
    case WanderState.Waiting:
      if (Timer <= 0) {
        State = WanderState.Moving;
        Agent.SetDestination(new Vector3(Random.Range(-15, 15), 0, Random.Range(-15, 15)));
      }
      break;
    case WanderState.Moving:
      Vector3 delta = Agent.destination - transform.position;
      if (delta.sqrMagnitude < .1) {
        State = WanderState.Waiting;
        Timer = 1;
      }
      break;
    }
  }
}