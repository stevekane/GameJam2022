using UnityEngine;

namespace ActionsAndAI {
  public class AirControl : MonoBehaviour {
    [SerializeField] CharacterController Controller;
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] MovementSpeed MovementSpeed;
    [SerializeField] Velocity Velocity;

    public bool CanStart() => !Controller.isGrounded;
    public void Move(AxisState axisState) {
      var direction = MovementSpeed.Value * axisState.XZFrom(PersonalCamera.Current);
      if (direction.magnitude > 0)
        transform.forward = direction;
    }
  }
}