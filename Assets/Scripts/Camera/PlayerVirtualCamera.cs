using UnityEngine;
using Cinemachine;

public class PlayerVirtualCamera : MonoBehaviour {
  public static CinemachineVirtualCamera Instance;

  void Awake() => Instance = GetComponent<CinemachineVirtualCamera>();
  void OnDestroy() => Instance = null;
}
