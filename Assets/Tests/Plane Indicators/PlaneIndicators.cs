using UnityEngine;

public class PlaneIndicators : MonoBehaviour {
  [SerializeField] Transform Target;
  void Start() {
    transform.parent = null;
  }
  void LateUpdate() {
    transform.position = Target.position;
  }
}