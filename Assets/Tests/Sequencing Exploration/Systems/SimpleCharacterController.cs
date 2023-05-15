using UnityEngine;
using KinematicCharacterController;

[ExecuteInEditMode]
[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleCharacterController : MonoBehaviour, ICharacterController {
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] Animator Animator;

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

  public Vector3 PhysicsVelocity { get; private set; }

  Vector3 AnimationVelocity;
  Quaternion AnimationRotation;
  Vector3 DirectVelocity;
  Quaternion DirectRotation;

  void Start() {
    KinematicCharacterMotor.CharacterController = this;
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

  // Only place you set rotation
  public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
    currentRotation = DirectRotation;
    currentRotation *= AnimationRotation;
    DirectRotation = currentRotation;
  }

  // Only place you set velocity
  public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
    var totalVelocity = DirectVelocity + AnimationVelocity + PhysicsVelocity;
    currentVelocity = totalVelocity;
  }

  // Run code prior to update
  public void BeforeCharacterUpdate(float deltaTime) {
  }

  // Run code after grounding check (regardless of outcome)
  public void PostGroundingUpdate(float deltaTime) {
    // TODO: Where to put "take off and land events" if anywhere?
    if (KinematicCharacterMotor.GroundingStatus.IsStableOnGround) {
      SimpleAbilityManager.AddTag(AbilityTag.CanJump);
      SimpleAbilityManager.AddTag(AbilityTag.Grounded);
    } else {
      SimpleAbilityManager.RemoveTag(AbilityTag.Grounded);
    }
    Animator.SetBool("Grounded", KinematicCharacterMotor.GroundingStatus.IsStableOnGround);
  }

  // Run code after update
  public void AfterCharacterUpdate(float deltaTime) {
    DirectVelocity = Vector3.zero;
    AnimationVelocity = Vector3.zero;
    AnimationRotation = Quaternion.identity;
  }

  // Evaluate colliders to determine if collision should happen
  public bool IsColliderValidForCollisions(Collider coll) {
    return true;
  }

  // Callback for contacting the ground
  public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
  }

  // Callback for contact from the sides or top
  public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {
  }

  // Not sure yet. Something to do with a chance to modify the stability report
  public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {
  }

  // Fired on discrete collision. Not sure about details yet
  public void OnDiscreteCollisionDetected(Collider hitCollider) {
  }
}