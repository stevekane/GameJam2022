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

  public float Gravity;

  [SerializeField] Animator Animator;
  [SerializeField] float IdleThreshold = 0.1f;

  Vector3 Velocity;
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

  public void SetMove(Vector3 v) {
    AbilityManager.GetAxis(AxisTag.Move).Update(0, new Vector2(v.x, v.z));
  }

  public void SetAim(Vector3 v) {
    AbilityManager.GetAxis(AxisTag.Aim).Update(0, new Vector2(v.x, v.z));
  }

  void FixedUpdate() {
    GetAxes(AbilityManager, out var desiredMoveDir, out var desiredFacing);
    var localTimeScale = Attributes.GetValue(AttributeTag.LocalTimeScale, 1);
    var velocity = Attributes.GetValue(AttributeTag.MoveSpeed) * desiredMoveDir;
    var localVelocity = localTimeScale * velocity;
    Velocity.SetXZ(localVelocity);
    var gravity = Time.fixedDeltaTime * Gravity;
    var localGravity = localTimeScale * gravity;
    Velocity.y = Controller.isGrounded ? gravity : Velocity.y+gravity;
    if (!Status.HasGravity)
      Velocity.y = 0f;
    Controller.Move(Time.fixedDeltaTime * Velocity);
    Status.IsGrounded = Controller.isGrounded;
    var turnSpeed = Attributes.GetValue(AttributeTag.TurnSpeed);
    var localTurnSpeed = localTimeScale * turnSpeed;
    transform.rotation = RotationFromDesired(transform.forward, localTurnSpeed, desiredFacing);
    if (Animator) {
      var orientedVelocity = Quaternion.Inverse(transform.rotation)*Velocity;
      var moveSpeed = Attributes.GetValue(AttributeTag.MoveSpeed);
      // THIS IS NONSCALED VELOCITY!
      // We want to continue to animate as if we were not slowed
      if (moveSpeed > 0 && velocity.XZ().magnitude > IdleThreshold) {
        Animator.SetBool("Moving", true);
        Animator.SetFloat("RightVelocity", orientedVelocity.x/moveSpeed);
        Animator.SetFloat("ForwardVelocity", orientedVelocity.z/moveSpeed);
      } else {
        Animator.SetBool("Moving", false);
        Animator.SetFloat("RightVelocity", 0);
        Animator.SetFloat("ForwardVelocity", 0);
      }
      Animator.SetBool("IsGrounded", Status.IsGrounded);
      Animator.SetBool("IsHurt", Status.IsHurt);
    }
  }
}