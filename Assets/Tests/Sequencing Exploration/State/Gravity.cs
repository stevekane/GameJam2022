using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class Gravity : MonoBehaviour {
  [Header("Reads From")]
  [SerializeField] LocalTime LocalTime;
  [SerializeField] GroundCheck GroundCheck;

  public float RisingStrength;
  public float FallingStrength;

  [Header("Writes To")]
  [SerializeField] SimpleCharacterController CharacterController;

  void FixedUpdate() {
    var strength = CharacterController.PhysicsVelocity.y <= 0 ? FallingStrength : RisingStrength;
    var yVelocity = LocalTime.FixedDeltaTime * strength;
    if (GroundCheck.IsGrounded && CharacterController.PhysicsVelocity.y <= 0) {
      CharacterController.PhysicsVelocity.y = 0;
    } else {
      CharacterController.ApplyExternalForce(strength * Vector3.up);
    }
  }
}