using UnityEngine;

namespace ActionsAndAI {
  public class AirControl : MonoBehaviour {
    [SerializeField] CharacterController Controller;
    public void OnStart(Vector3 direction) {
      if (direction.magnitude > 0)
        Controller.transform.forward = direction;
    }
  }
}