using UnityEngine;

namespace ActionsAndAI {
  public class GroundControl : MonoBehaviour {
    [SerializeField] CharacterController Controller;
    [SerializeField] MovementSpeed MovementSpeed;
    [SerializeField] Velocity Velocity;

    public void OnStart(Vector3 direction) {
      var velocity = MovementSpeed.Value * direction;
      Velocity.Value.x = velocity.x;
      Velocity.Value.z = velocity.z;
      if (velocity.magnitude > 0)
        Controller.transform.forward = velocity;
    }
  }
}