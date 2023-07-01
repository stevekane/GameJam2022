using UnityEngine;

public class Rotator : MonoBehaviour {
  public bool UseLocal;
  public Vector3 Axis = Vector3.up;
  public float DegreesPerSecond;

  void FixedUpdate() {
    var axis = UseLocal ? transform.TransformVector(Axis) : Axis;
    transform.RotateAround(transform.position, axis, DegreesPerSecond*Time.deltaTime);
  }
}