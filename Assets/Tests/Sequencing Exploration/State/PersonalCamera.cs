using UnityEngine;

public class PersonalCamera : MonoBehaviour {
  public Camera Current;

  void Start() {
    Current = Camera.main;
  }

  public Vector3 CameraScreenToWorldXZ(Vector2 v) {
    var length = v.magnitude;
    var xzWorld = Current.transform.TransformDirection(v);
    xzWorld.y = 0;
    return length * xzWorld.normalized;
  }
}