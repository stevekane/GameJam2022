using UnityEngine;

public class Pushable : MonoBehaviour {
  public Vector3 Impulse;

  public void Push(Vector3 velocity) {
    Impulse += velocity;
  }

  void FixedUpdate() {
    Impulse = Vector3.zero;
  }
}