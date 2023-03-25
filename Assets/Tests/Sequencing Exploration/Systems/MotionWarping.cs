using UnityEngine;

public enum RotationMatch {
  Target,
  LookAt
}

[DefaultExecutionOrder(ScriptExecutionGroups.Late)]
public class MotionWarping : MonoBehaviour {
  public int Frame;
  public int Total;
  public RootMotion RootMotion;
  public RotationMatch RotationMatch;
  public Transform Target;
  public Vector3 TargetOffset;
  public float TargetDistance;

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

  void FixedUpdate() {
    if (Frame < Total && Target) {
      if (RotationMatch == RotationMatch.Target) {
        var targetPosition = Target.position + Target.TransformVector(TargetOffset);
        var targetRotation = Target.rotation;
        RootMotion.DeltaPosition = WarpMotion(transform.position, targetPosition, RootMotion.DeltaPosition, Frame, Total);
        RootMotion.DeltaRotation = WarpRotation(transform.rotation, targetRotation, RootMotion.DeltaRotation, Frame, Total);
      } else {
        var toTarget = (Target.position - transform.position).normalized;
        var targetPosition = Target.position - toTarget * TargetDistance;
        var targetRotation = toTarget.magnitude > 0 ? Quaternion.LookRotation(toTarget) : transform.rotation;
        RootMotion.DeltaPosition = WarpMotion(transform.position, targetPosition, RootMotion.DeltaPosition, Frame, Total);
        RootMotion.DeltaRotation = WarpRotation(transform.rotation, targetRotation, RootMotion.DeltaRotation, Frame, Total);
      }
      Frame++;
    }
  }
}