using UnityEngine;
using UnityEngine.AI;

public class StupidWanderer : MonoBehaviour {
  public LayerMask LayerMask;
  public Transform Prey;
  public float StalkingDistance = 5;
  public float MaxVisibilityDistance = 1000;
  public NavMeshAgent Agent;

  bool InLineOfSight(Vector3 p, Transform t, float maxDistance, float height = 0) {
    var origin = new Vector3(p.x, height, p.y);
    var isInFront = p.IsInFrontOf(t);
    var delta = t.position-p;
    var direction = delta.normalized;
    var didHit = Physics.Raycast(p, direction, out RaycastHit hit, 1000, LayerMask);
    var isVisible = didHit && hit.transform == t;
    if (didHit && isVisible) {

    } else {
      Debug.DrawRay(p, 1000*direction, didHit ? Color.green : Color.red);
    }
    return isInFront && isVisible;
  }

  void FixedUpdate() {
    if (InLineOfSight(transform.position, Prey, MaxVisibilityDistance, 1)) {
      Agent.SetDestination(transform.position);
    } else {
      var destination = Prey.transform.position-StalkingDistance*Prey.transform.forward;
      Agent.SetDestination(destination);
    }
  }
}