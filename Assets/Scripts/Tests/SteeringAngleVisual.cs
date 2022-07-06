using UnityEngine;

public class SteeringAngleVisual : MonoBehaviour {
  public Transform Target;

  Vector3 Steer(Vector3 forward, Vector3 right, Vector3 acceleration) {
    var aSteer = Vector3.Project(acceleration, right);
    var aPull = Vector3.Project(acceleration, -forward);
    var dot = Vector3.Dot(forward, aPull);
    aPull = dot > 0 ? Vector3.zero : aPull;
    return aSteer+aPull;
  }

  void OnDrawGizmos() {
    var acceleration = Target.transform.position-transform.position;
    var aSteer = Steer(transform.forward, transform.right, acceleration);
    Debug.DrawRay(transform.position, transform.forward, Color.blue);
    Debug.DrawRay(transform.position, acceleration, Color.red);
    Debug.DrawRay(Vector3.up+transform.position, aSteer, Color.white);
  }
}