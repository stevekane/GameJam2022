using UnityEngine;

public class LocomotionAbility : SimpleAbility {
  [Header("Reads From")]
  [SerializeField] MovementSpeed MovementSpeed;
  [Header("Writes To")]
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] PhysicsMotion PhysicsMotion;
  [SerializeField] Animator Animator;

  public AbilityAction<Vector3> Move;

  void Awake() {
    Move.Listen(Main);
  }

  void Main(Vector3 value) {
    if (value.sqrMagnitude > 0) {
      AbilityManager.transform.rotation = Quaternion.LookRotation(value);
    }
    var currentVelocity = PhysicsMotion.PhysicsVelocity.XZ();
    var desiredVelocity = MovementSpeed.Value * value.normalized;
    var deltaVelocity = desiredVelocity - currentVelocity;
    var requiredForce = deltaVelocity / Time.fixedDeltaTime;
    var maxForceMagnitude = 2 * MovementSpeed.Value / Time.fixedDeltaTime;
    var steeringForce = Mathf.Min(maxForceMagnitude, requiredForce.magnitude) * requiredForce.normalized;
    var velocity = currentVelocity + Time.fixedDeltaTime * steeringForce;
    // var velocity = MovementSpeed.Value * Value.normalized;
    // var motion = Time.deltaTime * velocity;
    // DirectMotion.IsActive(true, 0);
    // DirectMotion.AddMotion(motion);
    PhysicsMotion.AddVelocity(Time.fixedDeltaTime * steeringForce);
    Animator.SetFloat("Speed", Mathf.Round(velocity.magnitude));
  }
}