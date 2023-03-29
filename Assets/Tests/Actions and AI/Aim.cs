using UnityEngine;

namespace ActionsAndAI {
  public class Aim : AbstractAxisActionBehavior {
    [SerializeField] PersonalCamera PersonalCamera;
    [SerializeField] CharacterController Controller;
    [SerializeField] Aiming Aiming;
    public override bool CanStart() => Aiming.Value;
    public override void OnStart(AxisState axisState) {
      var direction = axisState.XZFrom(PersonalCamera.Current);
      if (direction.magnitude > 0)
        Controller.transform.forward = direction;
    }
  }
}