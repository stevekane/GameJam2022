using UnityEngine;
using KinematicCharacterController;

[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleCharacterController : MonoBehaviour, ICharacterController {
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] AnimatorGraph AnimatorGraph;
  [SerializeField] Animator Animator;
  [SerializeField] CharacterState InitialState;

  [Header("States")]
  public CharacterState State;

  [Header("Motor")]
  public KinematicCharacterMotor KinematicCharacterMotor;

  [Header("Motion Warping")]
  public bool MotionWarpingActive;
  public int Frame;
  public int Total;
  public Vector3 TargetPosition;
  public Quaternion TargetRotation;

  [Header("State")]
  public bool AllowRootMotion;
  public bool AllowRootRotation;
  public bool AllowWarping = true;
  public bool AllowMoving = true;
  public bool AllowRotating = true;
  public bool AllowExternalForces = true;
  public bool AllowPhysics = true;

  public Collider WallCollider;
  public Vector3 WallNormal;
  public PhysicsMover GroundPhysicsMover => KinematicCharacterMotor.GroundingStatus.GroundCollider?.GetComponent<PhysicsMover>();
  public Vector3 PhysicsVelocity { get; private set; }

  public Vector3 AnimationVelocity;
  public Quaternion AnimationRotation;
  public Vector3 DirectVelocity;
  public Quaternion DirectRotation;

  void Start() {
    KinematicCharacterMotor.CharacterController = this;
    ChangeState(InitialState);
  }

  // TODO: Should this happen right away?
  // Maybe should happen at the start of the next update or something?
  public void ChangeState(CharacterState nextState) {
    if (State) {
      SimpleAbilityManager.RemoveTag(State.ActiveTags);
      State.OnExit();
    }
    State = nextState;
    AnimatorGraph.CrossFade(AnimatorGraph.CharacterStates.IndexOf(State), .1f);
    SimpleAbilityManager.AddTag(State.ActiveTags);
    State.OnEnter();
  }

  public void Move(Vector3 v) {
    if (AllowMoving)
      DirectVelocity += v;
  }

  public void Rotate(Quaternion q) {
    if (AllowRotating)
      DirectRotation = q;
  }

  public void SetPhysicsVelocity(Vector3 v) {
    PhysicsVelocity = v;
  }

  public void ApplyExternalForce(Vector3 a) {
    if (AllowExternalForces)
      PhysicsVelocity += a * Time.fixedDeltaTime;
  }

  public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
    State.UpdateRotation(ref currentRotation, deltaTime);
  }

  public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
    State.UpdateVelocity(ref currentVelocity, deltaTime);
  }

  public void BeforeCharacterUpdate(float deltaTime) {
    // WallCollider = null;
    // WallNormal = Vector3.zero;
    // SimpleAbilityManager.RemoveTag(AbilityTag.OnWall);
    // Animator.SetBool("WallSlide", false);
    State.BeforeCharacterUpdate(deltaTime);
  }

  public void PostGroundingUpdate(float deltaTime) {
    State.PostGroundingUpdate(deltaTime);
  }

  public void AfterCharacterUpdate(float deltaTime) {
    DirectVelocity = Vector3.zero;
    AnimationVelocity = Vector3.zero;
    AnimationRotation = Quaternion.identity;
    State.AfterCharacterUpdate(deltaTime);
  }

  public bool IsColliderValidForCollisions(Collider coll) {
    return State.IsColliderValidForCollisions(coll);
  }

  public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
    State.OnGroundHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
  }

  public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
    State.OnMovementHit(hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
  }

  public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {
    State.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref hitStabilityReport);
  }

  public void OnDiscreteCollisionDetected(Collider hitCollider) {
    State.OnDiscreteCollisionDetected(hitCollider);
  }

  /*
  NOTE: One thing that won't work anymore is using this to cause root motion to be applied
  during Timeline preview (by having UpdateInEditMode and having this component update the
  position and rotation of the character directly) .

  If we want to support Animation in this way we probably need to do something else tbd.
  */

  void OnAnimatorMove() {
    if (AllowRootMotion) {
      if (MotionWarpingActive) {
        AnimationVelocity += WarpMotion(transform.position, TargetPosition, Animator.deltaPosition, Frame, Total) / Time.fixedDeltaTime;
      } else {
        AnimationVelocity += Animator.deltaPosition / Time.fixedDeltaTime;
      }
    }

    if (AllowRootRotation) {
      if (MotionWarpingActive) {
        AnimationRotation = WarpRotation(transform.rotation, TargetRotation, Animator.deltaRotation, Frame, Total);
      } else {
        AnimationRotation = Animator.deltaRotation;
      }
    }
    Frame = Mathf.Min(Total, Frame+1);
  }

  Vector3 WarpMotion(Vector3 position, Vector3 target, Vector3 deltaPosition, int frame, int total) {
    var fraction = (float)frame/(float)total;
    var warpDelta = (target-position) / (total-frame);
    return Vector3.Lerp(deltaPosition, warpDelta, fraction);
  }

  Quaternion WarpRotation(Quaternion rotation, Quaternion target, Quaternion deltaRotation, int frame, int total) {
    var fraction = (float)frame/(float)total;
    var warpDelta = Quaternion.Slerp(Quaternion.identity, target * Quaternion.Inverse(rotation), fraction);
    var xyzRotation = Quaternion.Slerp(deltaRotation, warpDelta, fraction);
    var xyzEuler = xyzRotation.eulerAngles;
    return Quaternion.Euler(0, xyzEuler.y, 0);
  }

  /*
  // Run code prior to update
  public void BeforeCharacterUpdate(float deltaTime) {
    WallCollider = null;
    WallNormal = Vector3.zero;
    SimpleAbilityManager.RemoveTag(AbilityTag.OnWall);
    Animator.SetBool("WallSlide", false);
  }

  // Callback for contact from the sides or top
  public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
    // if we hit a wall then record that we hit the wall for this frame
    if (!KinematicCharacterMotor.GroundingStatus.IsStableOnGround && !hitStabilityReport.IsStable) {
      Animator.SetBool("WallSlide", true);
      WallCollider = hitCollider;
      WallNormal = hitNormal;
      SimpleAbilityManager.AddTag(AbilityTag.OnWall);
    }
  }
  */
}