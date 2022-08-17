using UnityEngine;

public class Rotator : MonoBehaviour {
  public float DegreesPerSecond;

  void Update() {
    transform.Rotate(transform.up, DegreesPerSecond*Time.deltaTime);
  }
}