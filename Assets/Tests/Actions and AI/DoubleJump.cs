using UnityEngine;

namespace ActionsAndAI {
  public class DoubleJump : AbstractActionBehavior {
    [field:SerializeField] public override string Name { get; set; } = "Double Jump";
    [field:SerializeField] public override ButtonCode ButtonCode { get; set; }
    [field:SerializeField] public override ButtonPressType ButtonPressType { get; set; }
    [SerializeField] CharacterController Controller;
    [SerializeField] Aiming Aiming;
    [SerializeField] Velocity Velocity;
    [SerializeField] float Strength = 35;
    public override bool CanStart() => !Controller.isGrounded && !Aiming.Value;
    public override void OnStart() => Velocity.Value.y = Strength;
  }
}