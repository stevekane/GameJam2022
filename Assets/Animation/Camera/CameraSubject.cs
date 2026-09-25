using UnityEngine;

public class CameraSubject : MonoBehaviour {
  public float Radius = 3;

  void Awake() {
    CameraManager.Instance.AddTarget(this);
  }

  void OnDestroy() {
    CameraManager.Instance.RemoveTarget(this, false);
  }

  void OnDeath() {
    CameraManager.Instance.RemoveTarget(this, true);
  }
}