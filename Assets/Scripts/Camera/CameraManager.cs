using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour {
  [SerializeField] CinemachineVirtualCamera[] Cameras;
  [SerializeField] ButtonCode CycleForwardButtonCode;
  [SerializeField] ButtonCode CycleBackwardButtonCode;

  int Index;

  void Awake() {
    InputManager.Instance.ButtonEvent(CycleForwardButtonCode, ButtonPressType.JustDown).Listen(CycleForward);
    InputManager.Instance.ButtonEvent(CycleBackwardButtonCode, ButtonPressType.JustDown).Listen(CycleBackward);
  }

  void CycleForward() {
    Cameras[Index].Priority = 0;
    Index = Index == 0 ? Cameras.Length-1 : Index-1;
    Cameras[Index].Priority = 1;
  }

  void CycleBackward() {
    Cameras[Index].Priority = 0;
    Index = (Index+1)%Cameras.Length;
    Cameras[Index].Priority = 1;
  }
}