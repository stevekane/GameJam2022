using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(AnimationDriver))]
[RequireComponent(typeof(Attributes))]
[RequireComponent(typeof(Status))]
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

  CharacterController Controller;
  AbilityManager AbilityManager;
  AnimationDriver AnimationDriver;
  Attributes Attributes;
  Status Status;
  Vector3 Velocity;
  Vector3 MoveAccum;

  void Awake() {
    Controller = GetComponent<CharacterController>();
    AbilityManager = GetComponent<AbilityManager>();
    AnimationDriver = GetComponent<AnimationDriver>();
    Attributes = GetComponent<Attributes>();
    Status = GetComponent<Status>();
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
    return AbilityManager.GetAxis(AxisTag.Aim).XZ.TryGetDirection() ?? transform.forward;
  }

  public void SetMove(Vector3 v) {
    AbilityManager.GetAxis(AxisTag.Move).Update(0, new Vector2(v.x, v.z));
  }

  public void SetAim(Vector3 v) {
    AbilityManager.GetAxis(AxisTag.Aim).Update(0, new Vector2(v.x, v.z));
  }

  public void Move(Vector3 delta) {
    MoveAccum += delta;
  }

  void FixedUpdate() {
    var desiredMoveDir = GetMove();
    var desiredFacing = GetAim();
    var localTimeScale = Attributes.GetValue(AttributeTag.LocalTimeScale, 1);

    // Move
    var moveSpeed = Attributes.GetValue(AttributeTag.MoveSpeed);
    var gravity = localTimeScale * Time.fixedDeltaTime * Attributes.GetValue(AttributeTag.Gravity);
    Velocity.x = localTimeScale * moveSpeed * desiredMoveDir.x;
    Velocity.z = localTimeScale * moveSpeed * desiredMoveDir.z;
    Velocity.y = Status switch {
      Status { HasGravity: true, IsGrounded: true } => gravity,
      Status { HasGravity: true, IsGrounded: false } => Velocity.y + gravity,
      _ => 0
    };
    Controller.Move(localTimeScale * Time.fixedDeltaTime * Velocity + MoveAccum);
    MoveAccum = Vector3.zero;

    // Turn
    var turnSpeed = Attributes.GetValue(AttributeTag.TurnSpeed);
    var localTurnSpeed = localTimeScale * turnSpeed;
    transform.rotation = RotationFromDesired(transform.forward, localTurnSpeed, desiredFacing);

    // Animation
    var animator = AnimationDriver.Animator;
    var orientedVelocity = Quaternion.Inverse(transform.rotation)*desiredMoveDir;
    animator.SetFloat("RightVelocity", orientedVelocity.x);
    animator.SetFloat("ForwardVelocity", orientedVelocity.z);
    animator.SetBool("IsGrounded", Status.IsGrounded);
    animator.SetBool("IsHurt", Status.IsHurt);
    AnimationDriver.SetSpeed(localTimeScale < 1 ? localTimeScale : AnimationDriver.BaseSpeed);
  }
}