using UnityEngine;
using KinematicCharacterController;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class Gravity : MonoBehaviour {
  [Header("Reads From")]
  [SerializeField] LocalTime LocalTime;

  public float RisingStrength;
  public float FallingStrength;

  [Header("Writes To")]
  [SerializeField] KinematicCharacterMotor KinematicCharacterMotor;
  [SerializeField] SimpleCharacterController CharacterController;

  void FixedUpdate() {
    var strength = CharacterController.PhysicsVelocity.y <= 0 ? FallingStrength : RisingStrength;
    var yVelocity = LocalTime.FixedDeltaTime * strength;
    var grounded = KinematicCharacterMotor.GroundingStatus.IsStableOnGround;
    if (grounded && CharacterController.PhysicsVelocity.y <= 0) {
      var v = CharacterController.PhysicsVelocity;
      v.y = 0;
      CharacterController.SetPhysicsVelocity(v);
    } else {
      CharacterController.ApplyExternalForce(strength * Vector3.up);
    }
  }
}