using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AbilityManager))]
[RequireComponent(typeof(AnimationDriver))]
[RequireComponent(typeof(Attributes))]
[RequireComponent(typeof(Status))]
public class Mover : MonoBehaviour {
  public static Quaternion RotationFromDesired(Quaternion rotation, float speed, Vector3 desiredForward) {
    var desiredRotation = Quaternion.LookRotation(desiredForward);
    var degrees = speed * Time.fixedDeltaTime;
    return Quaternion.RotateTowards(rotation, desiredRotation, degrees);
  }

  CharacterController Controller;
  AbilityManager AbilityManager;
  AnimationDriver AnimationDriver;
  Attributes Attributes;
  Status Status;
  Vector3 MoveDelta;

  public Vector3 InputVelocity { get; private set; }
  public Vector3 GravitationalVelocity { get; private set; }
  public Vector3 MoveVelocity { get; private set; }
  public Vector3 Velocity { get; private set; }

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

  public void SetMoveAim(Vector3 move, Vector3 aim) {
    SetMove(move);
    SetAim(aim);
  }

  public void SetMove(Vector3 v) => AbilityManager.GetAxis(AxisTag.Move).Update(0, new Vector2(v.x, v.z));
  public void SetAim(Vector3 v) => AbilityManager.GetAxis(AxisTag.Aim).Update(0, new Vector2(v.x, v.z));
  public Vector3 GetMove() => AbilityManager.GetAxis(AxisTag.Move).XZ;
  public Vector3 GetAim() => AbilityManager.GetAxis(AxisTag.Aim).XZ.TryGetDirection() ?? transform.forward;
  public void Move(Vector3 delta) => MoveDelta += delta;
  public void ResetVelocity() {
    InputVelocity = Vector3.zero;
    GravitationalVelocity = Vector3.zero;
    MoveVelocity = Vector3.zero;
    Velocity = Vector3.zero;
  }

  void FixedUpdate() {
    var desiredMoveDir = GetMove();
    var desiredFacing = GetAim();
    var localTimeScale = Attributes.GetValue(AttributeTag.LocalTimeScale, 1);
    var dt = localTimeScale * Time.fixedDeltaTime;

    // Move
    var moveSpeed = Attributes.GetValue(AttributeTag.MoveSpeed);
    var gravity = Vector3.up * dt * Attributes.GetValue(AttributeTag.Gravity);
    InputVelocity = localTimeScale * moveSpeed * desiredMoveDir;
    GravitationalVelocity = Status switch {
      Status { HasGravity: true, IsGrounded: true } => gravity,
      Status { HasGravity: true, IsGrounded: false } => GravitationalVelocity + gravity,
      _ => Vector3.zero
    };
    MoveVelocity = MoveDelta / Time.fixedDeltaTime;
    Velocity = InputVelocity + GravitationalVelocity + MoveVelocity;
    var inputDelta = dt * InputVelocity;
    var gravitationalDelta = dt * GravitationalVelocity;
    Controller.Move(inputDelta + gravitationalDelta + MoveDelta);
    MoveDelta = Vector3.zero;

    // Turn
    var turnSpeed = Attributes.GetValue(AttributeTag.TurnSpeed);
    var localTurnSpeed = localTimeScale * turnSpeed;
    transform.rotation = RotationFromDesired(transform.rotation, localTurnSpeed, desiredFacing);

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