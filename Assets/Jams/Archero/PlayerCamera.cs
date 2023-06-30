using UnityEngine;
using Cinemachine;

namespace Archero {
  public class PlayerCamera : MonoBehaviour {
    [SerializeField] CinemachineVirtualCamera VirtualCamera;

    void Start() {
      if (!Camera.main.GetComponent<CinemachineBrain>()) {
        Camera.main.gameObject.AddComponent<CinemachineBrain>();
      }
      VirtualCamera.GetComponent<ZAxisCameraFollow>().Target = Player.Instance?.transform;
    }
  }
}