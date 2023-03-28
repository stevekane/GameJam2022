using UnityEngine;

namespace ActionsAndAI {
  public class Jump : AbstractActionBehavior {
    [field:SerializeField] public override string Name { get; set; } = "Jump";
    [field:SerializeField] public override ButtonCode ButtonCode { get; set; }
    [field:SerializeField] public override ButtonPressType ButtonPressType { get; set; }
    [SerializeField] CharacterController Controller;
    [SerializeField] Velocity Velocity;
    [SerializeField] Aiming Aiming;
    [SerializeField] float Strength = 15;
    public override bool CanStart() => Controller.isGrounded && !Aiming.Value;
    public override void OnStart() => Velocity.Value.y = Strength;
  }
}