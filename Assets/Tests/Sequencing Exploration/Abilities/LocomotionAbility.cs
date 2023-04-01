using UnityEngine;

public class LocomotionAbility : SimpleAbilityVector3 {
  [SerializeField] Velocity Velocity;
  [SerializeField] MovementSpeed MovementSpeed;

  public override void OnRun() {
    if (Value.sqrMagnitude > 0) {
      SimpleAbilityManager.transform.rotation = Quaternion.LookRotation(Value);
    }
    var velocityXZ = MovementSpeed.Value * Value.normalized;
    Velocity.Value.x = velocityXZ.x;
    Velocity.Value.z = velocityXZ.z;
  }
}