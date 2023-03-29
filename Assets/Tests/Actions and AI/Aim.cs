using UnityEngine;

namespace ActionsAndAI {
  public class Aim : AbstractVectorActionBehavior {
    [SerializeField] CharacterController Controller;
    [SerializeField] Aiming Aiming;
    public override bool CanStart() => Aiming.Value;
    public override void OnStart(Vector3 direction) {
      if (direction.magnitude > 0)
        Controller.transform.forward = direction;
    }
  }
}