
using UnityEngine;

public class PlayerVirtualCamera : MonoBehaviour {
  public static Unity.Cinemachine.CinemachineVirtualCamera Instance;

  void Awake() => Instance = GetComponent<Unity.Cinemachine.CinemachineVirtualCamera>();
  void OnDestroy() => Instance = null;
}
