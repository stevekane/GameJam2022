using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraSubject : MonoBehaviour {
  public float Radius = 3;

  bool SpawnGhost = false;

  void Awake() {
    SpawnGhost = true;
    CameraManager.Instance.AddTarget(this);
  }

  #if UNITY_EDITOR
  void OnApplicationQuit() {
    SpawnGhost = false;
  }
  #endif

  void OnDestroy() {
    CameraManager.Instance.RemoveTarget(this, SpawnGhost);
  }
}