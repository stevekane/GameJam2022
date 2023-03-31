using UnityEngine;

namespace ActionsAndAI {
  public class GroundControl : SimpleAbilityVector3 {
    [SerializeField] CharacterController Controller;
    [SerializeField] MovementSpeed MovementSpeed;
    [SerializeField] Velocity Velocity;

    public override void OnRun() {
      var velocity = MovementSpeed.Value * Value;
      Velocity.Value.x = velocity.x;
      Velocity.Value.z = velocity.z;
      if (velocity.magnitude > 0)
        Controller.transform.forward = velocity;
      Stop();
    }
  }
}