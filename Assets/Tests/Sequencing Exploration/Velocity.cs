using UnityEngine;

public class Velocity : MonoBehaviour {
  public Vector3 Value;

  void OnDrawGizmosSelected() {
    var origin = transform.position + transform.up;
    var extent = transform.position + transform.up + Value;
    var color = Color.blue;
    Gizmos.DrawLine(origin, extent);
  }
}