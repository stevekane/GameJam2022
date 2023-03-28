using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
  public class GroundControl : AbstractAxisActionBehavior {
    [field:SerializeField] public override string Name { get; set; } = "Ground Control";
    [field:SerializeField] public override AxisCode AxisCode { get; set; }
    [SerializeField] CharacterController Controller;
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] MovementSpeed MovementSpeed;
    [SerializeField] Aiming Aiming;
    [SerializeField] Velocity Velocity;

    public override bool CanStart() => Controller.isGrounded && !Aiming.Value;
    public override void OnStart(AxisState axisState) {
      var velocity = MovementSpeed.Value * axisState.XZFrom(PersonalCamera.Current);
      Velocity.Value.x = velocity.x;
      Velocity.Value.z = velocity.z;
      if (velocity.magnitude > 0)
        Controller.transform.forward = velocity;
    }
  }
}