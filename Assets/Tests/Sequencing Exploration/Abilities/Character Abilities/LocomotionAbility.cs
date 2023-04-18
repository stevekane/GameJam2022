using UnityEngine;

public class LocomotionAbility : SimpleAbilityVector3 {
  [Header("Reads From")]
  [SerializeField] MovementSpeed MovementSpeed;
  [Header("Writes To")]
  [SerializeField] DirectMotion DirectMotion;
  [SerializeField] PhysicsMotion PhysicsMotion;
  [SerializeField] Animator Animator;

  public AbilityAction Move;

  void Awake() {
    Move.Ability = this;
    Move.CanRun = true;
    Move.Source.Listen(Main);
  }

  void Main() {
    if (Value.sqrMagnitude > 0) {
      SimpleAbilityManager.transform.rotation = Quaternion.LookRotation(Value);
    }
    var currentVelocity = PhysicsMotion.PhysicsVelocity.XZ();
    var desiredVelocity = MovementSpeed.Value * Value.normalized;
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