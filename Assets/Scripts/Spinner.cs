using UnityEngine;

public class Spinner : MonoBehaviour {
  public float DegreesPerSecond;

  void Update() {
    var origin = transform.position;
    var axis = Vector3.up;
    var degrees = DegreesPerSecond * Time.deltaTime;
    transform.RotateAround(origin,axis,degrees);
  }
}