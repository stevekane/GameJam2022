using UnityEngine;

public class Rotator : MonoBehaviour {
  public Vector3 Axis = Vector3.up;
  public float DegreesPerSecond;

  void FixedUpdate() {
    transform.RotateAround(transform.position, transform.TransformVector(Axis), DegreesPerSecond*Time.deltaTime);
  }
}