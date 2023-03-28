using UnityEngine;

namespace ActionsAndAI {
  public class Jump : AbstractAction {
    [SerializeField] ButtonCode StartButtonCode;
    [SerializeField] ButtonPressType StartButtonPressType;
    [SerializeField] CharacterController Controller;
    [SerializeField] Velocity Velocity;
    [SerializeField] float Strength = 15;

    public override string Name => "Jump";
    public override ButtonCode ButtonCode => StartButtonCode;
    public override ButtonPressType ButtonPressType => StartButtonPressType;
    public override bool CanStart() => Controller.isGrounded;
    public override void OnStart() {
      Debug.Log($"Jump fired with strength {Strength}");
      Velocity.Value.y = Strength;
    }
  }
}