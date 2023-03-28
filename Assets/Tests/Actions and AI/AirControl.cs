using UnityEngine;

namespace ActionsAndAI {
  public class AirControl : AbstractAxisActionBehavior {
    [field:SerializeField] public override string Name { get; set; } = "Air Control";
    [field:SerializeField] public override AxisCode AxisCode { get; set; }
    [SerializeField] CharacterController Controller;
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] Aiming Aiming;

    public override bool CanStart() => !Controller.isGrounded && !Aiming.Value;
    public override void OnStart(AxisState axisState) {
      var direction = axisState.XZFrom(PersonalCamera.Current);
      if (direction.magnitude > 0)
        Controller.transform.forward = direction;
    }
  }
}