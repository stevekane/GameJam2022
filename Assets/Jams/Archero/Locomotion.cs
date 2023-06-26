using UnityEngine;

namespace Archero {
  public class Locomotion : SimpleAbility {
    [SerializeField] MovementSpeed MovementSpeed;
    [SerializeField] CharacterController CharacterController;

    public AbilityAction<Vector3> Move;

    Vector3 V;

    void Awake() {
      IsRunning = true; // Steve: This is not "correct". Need to think about how to handle these stateless abilities
      Move.Listen(CaptureDirection);
    }

    void CaptureDirection(Vector3 v) {
      V = v;
    }

    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
      currentVelocity = MovementSpeed.Value * V.normalized;
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
      if (V.sqrMagnitude > 0) {
        currentRotation = Quaternion.LookRotation(V);
      }
    }
  }
}