using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

// TODO: REMOVED FOR AI TESTING
// [RequireComponent(typeof(CharacterController))]
// [RequireComponent(typeof(AbilityManager))]
// [RequireComponent(typeof(AnimationDriver))]
// [RequireComponent(typeof(Attributes))]
// [RequireComponent(typeof(Status))]
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
  Vector3? TeleportDestination;
  Vector3 MoveDelta;
  Quaternion RotationDelta;

  public Vector3 InputVelocity { get; private set; }
  public Vector3 FallVelocity => new(0, -FallSpeed, 0);
  public Vector3 MoveVelocity { get; private set; }
  public Vector3 Velocity { get; private set; }
  public float FallSpeed { get; private set; }

  public void Awake() {
    this.InitComponent(out Controller);
    this.InitComponent(out AbilityManager);
    this.InitComponent(out AnimationDriver);
    this.InitComponent(out Attributes);
    this.InitComponent(out Status);
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
  public void Teleport(Vector3 destination) => TeleportDestination = destination;
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
    AbilityManager.Running.ForEach(a => { if (a is Grapple) a.Stop(); });
  }

  Vector3 WallSlideNormal;
  void OnControllerColliderHit(ControllerColliderHit hit) {
    if ((Defaults.Instance.EnvironmentLayerMask & (1<<hit.gameObject.layer)) == 0) return;
    WallSlideNormal = hit.normal;
  }

  public void FixedUpdate() {
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
    var maxFallSpeed = Attributes.GetValue(AttributeTag.MaxFallSpeed);
    if (Status.IsWallSliding)
      maxFallSpeed *= .2f;
    FallSpeed = Mathf.Min(FallSpeed, maxFallSpeed);
    MoveVelocity = MoveDelta / Time.fixedDeltaTime;
    Velocity = InputVelocity + FallVelocity + MoveVelocity;
    var inputDelta = dt * InputVelocity;
    var fallDelta = dt * FallVelocity;
    Controller.Move(inputDelta + fallDelta + MoveDelta);
    MoveDelta = Vector3.zero;

    if (TeleportDestination.HasValue) {
      transform.position = TeleportDestination.Value;
      ResetVelocity();
    }
    TeleportDestination = null;

    // Turn
    if (Status.IsWallSliding) {
      transform.rotation = Quaternion.LookRotation(-WallSlideNormal);
    } else {
      var turnSpeed = Attributes.GetValue(AttributeTag.TurnSpeed);
      var localTurnSpeed = localTimeScale * turnSpeed;
      var desiredRotation = RotationFromDesired(transform.rotation, localTurnSpeed, desiredFacing);
      transform.rotation = desiredRotation * RotationDelta;
    }
    RotationDelta = Quaternion.identity;

    // Animation
    var animator = AnimationDriver.Animator;
    var orientedVelocity = Quaternion.Inverse(transform.rotation)*Velocity.XZ().normalized;
    var inputSpeed = InputVelocity.magnitude;
    const float MOVE_CYCLE_DISTANCE = 5; // distance moved by the walk cycle at full speed... very bullshit
    animator.SetFloat("TorsoRotation", AnimationDriver.TorsoRotation);
    animator.SetFloat("RightVelocity", orientedVelocity.x);
    animator.SetFloat("ForwardVelocity", orientedVelocity.z);
    animator.SetFloat("Speed", inputSpeed / MOVE_CYCLE_DISTANCE);
    animator.SetBool("IsGrounded", Status.IsGrounded);
    animator.SetBool("IsWallSliding", Status.IsWallSliding);
    animator.SetBool("IsHurt", Status.IsHurt);
    animator.SetBool("IsFallen", Status.IsFallen);
    AnimationDriver.SetSpeed(localTimeScale < 1 ? localTimeScale : AnimationDriver.BaseSpeed);
  }
}