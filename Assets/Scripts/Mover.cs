using UnityEngine;
using UnityEngine.AI;

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

  NavMeshAgent NavMeshAgent;
  CharacterController Controller;
  AbilityManager AbilityManager;
  AnimationDriver AnimationDriver;
  Attributes Attributes;
  Status Status;
  Vector3 MoveDelta;
  Quaternion RotationDelta;

  public Vector3 InputVelocity { get; private set; }
  public Vector3 FallVelocity => new(0, -FallSpeed, 0);
  public Vector3 MoveVelocity { get; private set; }
  public Vector3 Velocity { get; private set; }
  public float FallSpeed { get; private set; }

  void Awake() {
    this.InitComponent(out Controller);
    this.InitComponent(out AbilityManager);
    this.InitComponent(out AnimationDriver);
    this.InitComponent(out Attributes);
    this.InitComponent(out Status);
    this.InitComponent(out NavMeshAgent, true);
    if (NavMeshAgent) {
      NavMeshAgent.updatePosition = false;
      NavMeshAgent.updateRotation = false;
    }
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

  public void SetMoveFromNavMeshAgent() {
    NavMeshAgent.nextPosition = transform.position;
    SetMove(NavMeshAgent.desiredVelocity.normalized);
  }
  public void SetMove(Vector3 v) => AbilityManager.GetAxis(AxisTag.Move).Update(0, new Vector2(v.x, v.z));
  public void SetAim(Vector3 v) => AbilityManager.GetAxis(AxisTag.Aim).Update(0, new Vector2(v.x, v.z));
  public Vector3 GetMove() => AbilityManager.GetAxis(AxisTag.Move).XZ;
  public Vector3 GetAim() => AbilityManager.GetAxis(AxisTag.Aim).XZ.TryGetDirection() ?? transform.forward;
  public void Move(Vector3 delta) => MoveDelta += delta;
  public void Rotate(Quaternion delta) => RotationDelta *= delta;
  public void ResetVelocity() {
    InputVelocity = Vector3.zero;
    FallSpeed = 0;
    MoveVelocity = Vector3.zero;
    Velocity = Vector3.zero;
  }
  // TODO HACK: better way to cancel movement effects
  public void ResetVelocityAndMovementEffects() {
    ResetVelocity();
    Status.Remove(Status.Get<KnockbackEffect>());
    Status.Remove(Status.Get<VaultEffect>());
  }

  void FixedUpdate() {
    var desiredMoveDir = GetMove();
    var desiredFacing = GetAim();
    var localTimeScale = Attributes.GetValue(AttributeTag.LocalTimeScale, 1);
    var dt = localTimeScale * Time.fixedDeltaTime;

    // Move
    var moveSpeed = Attributes.GetValue(AttributeTag.MoveSpeed);
    var gravity = dt * Attributes.GetValue(AttributeTag.Gravity);
    InputVelocity = localTimeScale * moveSpeed * desiredMoveDir;
    FallSpeed = Status switch {
      Status { HasGravity: true, IsGrounded: true } => gravity,
      Status { HasGravity: true, IsGrounded: false } => FallSpeed + gravity,
      _ => 0
    };
    FallSpeed = Mathf.Min(FallSpeed, Attributes.GetValue(AttributeTag.MaxFallSpeed));
    MoveVelocity = MoveDelta / Time.fixedDeltaTime;
    Velocity = InputVelocity + FallVelocity + MoveVelocity;
    var inputDelta = dt * InputVelocity;
    var fallDelta = dt * FallVelocity;
    Controller.Move(inputDelta + fallDelta + MoveDelta);
    MoveDelta = Vector3.zero;

    // Turn
    var turnSpeed = Attributes.GetValue(AttributeTag.TurnSpeed);
    var localTurnSpeed = localTimeScale * turnSpeed;
    var desiredRotation = RotationFromDesired(transform.rotation, localTurnSpeed, desiredFacing);
    transform.rotation = desiredRotation * RotationDelta;
    RotationDelta = Quaternion.identity;

    // Animation
    var animator = AnimationDriver.Animator;
    var orientedVelocity = Quaternion.Inverse(transform.rotation)*InputVelocity;
    animator.SetFloat("RightVelocity", orientedVelocity.x);
    animator.SetFloat("ForwardVelocity", orientedVelocity.z);
    animator.SetBool("IsGrounded", Status.IsGrounded);
    animator.SetBool("IsHurt", Status.IsHurt);
    AnimationDriver.SetSpeed(localTimeScale < 1 ? localTimeScale : AnimationDriver.BaseSpeed);
  }
}