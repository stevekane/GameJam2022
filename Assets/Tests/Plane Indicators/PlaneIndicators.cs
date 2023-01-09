using UnityEngine;

public class PlaneIndicators : MonoBehaviour {
  [SerializeField] Transform Target;
  void LateUpdate() {
    transform.position = Target.position;
  }
}