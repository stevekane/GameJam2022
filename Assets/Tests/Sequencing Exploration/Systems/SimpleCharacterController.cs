using UnityEngine;

[DefaultExecutionOrder(ScriptExecutionGroups.Physics)]
public class SimpleCharacterController : MonoBehaviour {
  [SerializeField] CharacterController Controller;
  [SerializeField] Animator Animator;

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
  public bool AllowReferenceFrameMotion = true;
  public bool AllowExternalForces = true;
  public bool AllowPhysics = true;

  [Header("Physics")]
  public Vector3 PhysicsVelocity;

  void OnAnimatorMove() {
    if (AllowRootMotion) {
      if (MotionWarpingActive) {
        Controller.Move(WarpMotion(transform.position, TargetPosition, Animator.deltaPosition, Frame, Total));
      } else {
        Controller.Move(Animator.deltaPosition);
      }
    }

    if (AllowRootRotation) {
      if (MotionWarpingActive) {
        Controller.transform.rotation *= WarpRotation(transform.rotation, TargetRotation, Animator.deltaRotation, Frame, Total);
      } else {
        Controller.transform.rotation *= Animator.deltaRotation;
      }
    }
    Frame = Mathf.Min(Total, Frame+1);
  }

  void FixedUpdate() {
    if (AllowPhysics)
      Controller.Move(Time.deltaTime * PhysicsVelocity);
  }

  public void Warp(Vector3 position) {
    if (AllowWarping)
      Controller.transform.position = position;
  }

  public CollisionFlags Move(Vector3 v) {
    if (AllowMoving)
      return Controller.Move(v);
    else
      return default;
  }

  public CollisionFlags MoveWithReferenceFrame(Vector3 v) {
    if (AllowReferenceFrameMotion)
      return Controller.Move(v);
    else
      return default;
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
}