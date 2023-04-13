using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class Gravity : MonoBehaviour {
  [Header("Reads From")]
  [SerializeField] LocalTime LocalTime;
  [SerializeField] GroundCheck GroundCheck;
  [SerializeField] float RisingStrength;
  [SerializeField] float FallingStrength;

  [Header("Writes To")]
  [SerializeField] PhysicsMotion PhysicsMotion;

  void FixedUpdate() {
    var strength = PhysicsMotion.PhysicsVelocity.y <= 0 ? FallingStrength : RisingStrength;
    var yVelocity = LocalTime.FixedDeltaTime * strength;
    if (GroundCheck.IsGrounded) {
      PhysicsMotion.OverrideVelocityY(yVelocity, 0);
    } else {
      PhysicsMotion.AddVelocity(yVelocity * Vector3.up);
    }
  }
}