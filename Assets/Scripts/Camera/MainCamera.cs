using UnityEngine;

public class MainCamera : MonoBehaviour {
  public static Camera Instance;

  void Awake() => Instance = GetComponent<Camera>();
  void OnDestroy() => Instance = null;
}