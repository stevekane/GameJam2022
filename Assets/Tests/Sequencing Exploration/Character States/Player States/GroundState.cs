using UnityEngine;

public class GroundState : CharacterState {
  [SerializeField] CharacterState AirborneState;

  public override void PostGroundingUpdate(float deltaTime) {
    if (!Motor.GroundingStatus.FoundAnyGround) {
      Controller.ChangeState(AirborneState);
    }
  }

  public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
    if (Controller.AllowRootRotation) {
      currentRotation = Controller.AnimationRotation * currentRotation;
    } else if (Controller.AllowRotating) {
      currentRotation = Controller.DirectRotation;
    }
  }

  public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
    if (Controller.AllowRootMotion) {
      currentVelocity = Controller.AnimationVelocity;
    } else if (Controller.AllowMoving) {
      currentVelocity = Controller.DirectVelocity;
    } else {
      currentVelocity = Controller.PhysicsVelocity;
    }
  }
}