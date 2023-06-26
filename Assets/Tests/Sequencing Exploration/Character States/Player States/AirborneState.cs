using UnityEngine;

public class AirborneState : CharacterState {
  [SerializeField] CharacterState GroundState;
  [SerializeField] LocalTime LocalTime;
  [SerializeField] Gravity Gravity;

  public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
    currentRotation = Controller.DirectRotation;
  }

  public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
    currentVelocity = Controller.PhysicsVelocity;
  }

  public override void PostGroundingUpdate(float deltaTime) {
    if (Motor.GroundingStatus.FoundAnyGround) {
      Controller.ChangeState(GroundState);
    }
  }
}