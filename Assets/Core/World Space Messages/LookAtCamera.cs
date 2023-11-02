using UnityEngine;

public class LookAtCamera : MonoBehaviour {
  Camera Camera;

  void Start() {
    Camera = Camera.main;
  }

  void LateUpdate() {
    transform.LookAt(transform.position + Camera.transform.forward);
  }
}