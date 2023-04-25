using UnityEngine;

/*
Describe several ways a character might move:

When on the ground, the character can control its direct motion with the control stick.
If it stops controlling its direct motion that should be treated as a force affecting
its current velocity.

How would you describe the total velocity of a character?

*/

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
    // var currentVelocity = PhysicsMotion.PhysicsVelocity.XZ();
    // var desiredVelocity = MovementSpeed.Value * value.normalized;
    // var deltaVelocity = desiredVelocity - currentVelocity;
    // var requiredForce = deltaVelocity / Time.fixedDeltaTime;
    // var maxForceMagnitude = 2 * MovementSpeed.Value / Time.fixedDeltaTime;
    // var steeringForce = Mathf.Min(maxForceMagnitude, requiredForce.magnitude) * requiredForce.normalized;
    // var velocity = currentVelocity + Time.fixedDeltaTime * steeringForce;
    // PhysicsMotion.AddVelocity(Time.fixedDeltaTime * steeringForce);
    var velocity = MovementSpeed.Value * value.normalized;
    var motion = Time.deltaTime * velocity;
    DirectMotion.IsActive(true, 0);
    DirectMotion.AddMotion(motion);
    Animator.SetFloat("Speed", Mathf.Round(velocity.magnitude));
  }
}