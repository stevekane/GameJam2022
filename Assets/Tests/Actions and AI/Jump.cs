using UnityEngine;

namespace ActionsAndAI {
  public class Jump : AbstractActionBehavior {
    [SerializeField] CharacterController Controller;
    [SerializeField] Velocity Velocity;
    [SerializeField] Aiming Aiming;
    [SerializeField] float Strength = 15;
    public override bool CanStart() => Controller.isGrounded && !Aiming.Value;
    public override void OnStart() => Velocity.Value.y = Strength;
  }
}