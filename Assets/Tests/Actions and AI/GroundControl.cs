using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
  public class GroundControl : MonoBehaviour {
    [SerializeField] CharacterController Controller;
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] MovementSpeed MovementSpeed;
    [SerializeField] Velocity Velocity;

    public bool CanStart() => Controller.isGrounded;
    public void Move(AxisState axisState) {
      var velocity = MovementSpeed.Value * axisState.XZFrom(PersonalCamera.Current);
      Velocity.Value.x = velocity.x;
      Velocity.Value.z = velocity.z;
      if (velocity.magnitude > 0)
        transform.forward = velocity;
    }
  }
}