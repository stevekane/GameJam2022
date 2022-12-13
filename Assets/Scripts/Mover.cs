using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Status), typeof(AbilityManager))]
public class Mover : MonoBehaviour {
  public static void UpdateAxes(AbilityManager manager, Vector3 desiredMoveDir, Vector3 desiredFacing) {
    SetMove(manager, desiredMoveDir);
    SetAim(manager, desiredFacing);
  }
  public static void SetMove(AbilityManager manager, Vector3 desiredMoveDir) {
    manager.GetAxis(AxisTag.Move).Update(0f, new Vector2(desiredMoveDir.x, desiredMoveDir.z));
  }
  public static void SetAim(AbilityManager manager, Vector3 desiredFacing) {
    manager.GetAxis(AxisTag.Aim).Update(0f, new Vector2(desiredFacing.x, desiredFacing.z));
  }
  public static void GetAxes(AbilityManager manager, out Vector3 desiredMoveDir, out Vector3 desiredFacing) {
    desiredMoveDir = manager.GetAxis(AxisTag.Move).XZ;
    desiredFacing = manager.GetAxis(AxisTag.Aim).XZ.TryGetDirection() ?? manager.transform.forward;
  }

  public static Quaternion RotationFromDesired(Vector3 forward, float speed, Vector3 desiredForward) =>
    RotationFromDesired(Quaternion.LookRotation(forward), speed, desiredForward);
  public static Quaternion RotationFromDesired(Quaternion rotation, float speed, Vector3 desiredForward) {
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(rotation, desiredRotation, degrees);
  }
  static Quaternion RotationFromDesired(Transform t, float speed, Vector3 desiredForward) {
    var currentRotation = t.rotation;
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(currentRotation, desiredRotation, degrees);
  }

  public Vector3 Velocity;

  [SerializeField] Animator Animator;

  CharacterController Controller;
  Attributes Attributes;
  Status Status;
  AbilityManager AbilityManager;

  void Awake() {
    Controller = GetComponent<CharacterController>();
    Attributes = GetComponent<Attributes>();
    Status = GetComponent<Status>();
    AbilityManager = GetComponent<AbilityManager>();
  }

  public IEnumerator TryAimAt(Vector3 desired, Timeval MaxDuration, float tolerance = .95f) {
    Mover.GetAxes(AbilityManager, out var desiredMove, out var desiredFacing);
    Mover.UpdateAxes(AbilityManager, desiredMove, desired);
    var aimingTimeout = Fiber.Wait(MaxDuration.Ticks);
    var aimed = Fiber.Until(() => Vector3.Dot(transform.forward, desired) >= tolerance);
    yield return Fiber.Any(aimingTimeout, aimed);
  }

  public void TryLookAt(Transform target) {
    if (target) {
      SetAim((target.position-transform.position).normalized);
    }
  }

  public Vector3 GetMove() {
    return AbilityManager.GetAxis(AxisTag.Move).XZ;
  }

  public Vector3 GetAim() {
    return AbilityManager.GetAxis(AxisTag.Aim).XZ;
  }

  public void SetMove(Vector3 v) {
    AbilityManager.GetAxis(AxisTag.Move).Update(0, new Vector2(v.x, v.z));
  }

  public void SetAim(Vector3 v) {
    AbilityManager.GetAxis(AxisTag.Aim).Update(0, new Vector2(v.x, v.z));
  }

  bool WasGrounded = true;
  void FixedUpdate() {
    GetAxes(AbilityManager, out var desiredMoveDir, out var desiredFacing);
    var localTimeScale = Attributes.GetValue(AttributeTag.LocalTimeScale, 1);

    // Move
    var desiredVelocity = Attributes.GetValue(AttributeTag.MoveSpeed) * desiredMoveDir;
    Velocity.SetXZ(desiredVelocity);
    desiredVelocity.y = WasGrounded ? -.1f : 0f;  // hack to force the controller to keep us grounded
    Controller.Move(localTimeScale * Time.fixedDeltaTime * desiredVelocity);
    if (!Controller.isGrounded) {
      if (Status.HasGravity) {
        Velocity.y += (localTimeScale * Time.fixedDeltaTime * Attributes.GetValue(AttributeTag.Gravity));
      } else {
        Velocity.y = 0;
      }
      Controller.Move(localTimeScale * Time.fixedDeltaTime * Vector3.up*Velocity.y);
    }
    WasGrounded = Controller.isGrounded;
    Status.IsGrounded = Controller.isGrounded;

    // Turn
    var turnSpeed = Attributes.GetValue(AttributeTag.TurnSpeed);
    var localTurnSpeed = localTimeScale * turnSpeed;
    transform.rotation = RotationFromDesired(transform.forward, localTurnSpeed, desiredFacing);
  }
}