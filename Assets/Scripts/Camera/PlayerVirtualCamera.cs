using Cinemachine;
using UnityEngine;

public class PlayerVirtualCamera : MonoBehaviour {
  public static CinemachineVirtualCamera Instance;

  void Awake() => Instance = GetComponent<CinemachineVirtualCamera>();
  void OnDestroy() => Instance = null;
}
