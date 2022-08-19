using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Status), typeof(AbilityManager))]
public class Mover : MonoBehaviour {
  [SerializeField] float MoveSpeed;
  [SerializeField] float TurnSpeed;
  [SerializeField] float Gravity;

  Vector3 Velocity;

  CharacterController Controller;
  Status Status;
  AbilityManager AbilityManager;

  void Awake() {
    Controller = GetComponent<CharacterController>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }

  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  void FixedUpdate() {
    var desiredMoveDir = AbilityManager.GetAxis(AxisTag.Move).XZ;
    var moveVelocity = MoveSpeed * Status.MoveSpeedFactor * desiredMoveDir;
    Velocity.SetXZ(moveVelocity);
    var gravity = Time.fixedDeltaTime * Gravity;
    Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
    if (!Status.HasGravity)
      Velocity.y = 0f;
    Controller.Move(Time.fixedDeltaTime * Velocity);

    var desiredFacing = AbilityManager.GetAxis(AxisTag.Aim).XZ.TryGetDirection() ?? transform.forward;
    transform.rotation = RotationFromDesired(transform, TurnSpeed * Status.RotateSpeedFactor, desiredFacing);
  }
}
