using UnityEngine;

public class Spinner : MonoBehaviour {
  public float DegreesPerSecond;

  void FixedUpdate() {
    var origin = transform.position;
    var axis = Vector3.up;
    var degrees = DegreesPerSecond * Time.fixedDeltaTime;
    transform.RotateAround(origin, axis, degrees);
  }
}