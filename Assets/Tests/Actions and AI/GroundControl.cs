using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Ability)]
  public class GroundControl : AbstractVectorActionBehavior {
    [SerializeField] CharacterController Controller;
    [SerializeField] MovementSpeed MovementSpeed;
    [SerializeField] Aiming Aiming;
    [SerializeField] Velocity Velocity;

    public override bool CanStart() => Controller.isGrounded && !Aiming.Value;
    public override void OnStart(Vector3 direction) {
      var velocity = MovementSpeed.Value * direction;
      Velocity.Value.x = velocity.x;
      Velocity.Value.z = velocity.z;
      if (velocity.magnitude > 0)
        Controller.transform.forward = velocity;
    }
  }
}