using UnityEngine;

namespace ActionsAndAI {
  public class DoubleJump : AbstractActionBehavior {
    [SerializeField] CharacterController Controller;
    [SerializeField] Aiming Aiming;
    [SerializeField] Velocity Velocity;
    [SerializeField] JumpCount JumpCount;
    [SerializeField] float Strength = 35;
    public override bool CanStart() => !Controller.isGrounded && !Aiming.Value && JumpCount.Value > 0;
    public override void OnStart() {
      JumpCount.Value--;
      Velocity.Value.y = Strength;
    }
  }
}