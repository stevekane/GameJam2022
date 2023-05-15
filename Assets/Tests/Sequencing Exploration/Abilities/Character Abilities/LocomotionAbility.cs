using UnityEngine;

public class LocomotionAbility : SimpleAbility {
  [Header("Reads From")]
  [SerializeField] MovementSpeed MovementSpeed;
  [Header("Writes To")]
  [SerializeField] SimpleCharacterController CharacterController;
  [SerializeField] Animator Animator;

  public AbilityAction<Vector3> Move;

  void Awake() {
    Move.Listen(Main);
  }

  void Main(Vector3 value) {
    if (value.sqrMagnitude > 0) {
      CharacterController.Rotate(Quaternion.LookRotation(value));
    }
    var velocity = MovementSpeed.Value * value.normalized;
    var steeringVector = velocity - CharacterController.KinematicCharacterMotor.BaseVelocity.XZ();
    var steeringForce = steeringVector / Time.fixedDeltaTime;
    var steeringDirection = steeringForce.normalized;
    var steeringMagnitude = steeringForce.magnitude;
    // how much grip i gots?
    var grounded = CharacterController.KinematicCharacterMotor.GroundingStatus.IsStableOnGround;
    var maxSteeringMagnitude = (grounded ? 2 : .025f) * MovementSpeed.Value / Time.fixedDeltaTime;
    var boundedSteeringForce = Mathf.Min(steeringMagnitude, maxSteeringMagnitude) * steeringDirection.XZ();
    CharacterController.ApplyExternalForce(boundedSteeringForce);
    Animator.SetFloat("Speed", Mathf.Round(velocity.magnitude));
  }
}