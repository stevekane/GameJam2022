using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NavmeshWayPointer : MonoBehaviour {
  [SerializeField] Transform[] Waypoints;
  [SerializeField] NavMeshAgent Agent;
  [SerializeField] Timeval Period = Timeval.FromSeconds(10);
  int i;

  IEnumerator Start() {
    while (true) {
      Agent.SetDestination(Waypoints[i%Waypoints.Length].position);
      i++;
      yield return new WaitForSeconds(Period.Seconds);
    }
  }

  void Update() {
    Debug.DrawRay(transform.position + 3 * Vector3.up, 3 * Vector3.down, Agent.isOnOffMeshLink ? Color.green : Color.red);
  }
}