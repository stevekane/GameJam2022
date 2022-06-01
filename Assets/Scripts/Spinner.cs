using UnityEngine;

public class Spinner : MonoBehaviour {
  public float DegreesPerSecond;

  void Update() {
    transform.RotateAround(transform.position,Vector3.up,DegreesPerSecond * Time.deltaTime);
  }
}