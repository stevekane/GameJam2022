using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(1)]
public class FocusTracking : MonoBehaviour {
  [SerializeField] Transform[] Bones;
  [SerializeField] Transform[] ReferenceBones;
  [SerializeField] Transform Goal;
  [SerializeField] float Tolerance = 1;
  [SerializeField] float Reach = 2.5f;
  [SerializeField] int Iterations = 10;

  NativeArray<Quaternion> AnimationPose;
  NativeArray<Quaternion> ReferencePose;
  NativeArray<Vector3> ReferencePositions;
  NativeArray<float> LinkLengths;
  NativeArray<Vector3> CurrentLinkPositions;

  void Start() {
    var count = Bones.Length;
    AnimationPose = new (count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    ReferencePose = new (count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    ReferencePositions = new(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    CurrentLinkPositions = new(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    LinkLengths = new (count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    for (var i = 0; i < count; i++) {
      AnimationPose[i] = Bones[i].localRotation; // cache the animation pose
      ReferencePose[i] = ReferenceBones[i].localRotation;
      ReferencePositions[i] = Bones[0].position + i * .5f * Vector3.forward; // bones in reference pose
      LinkLengths[i] = .5f; // TODO: hardcoded because lazy but correct for example
      CurrentLinkPositions[i] = ReferenceBones[i].position; // TODO: This produces what looks correct. Not 100% sure it's logical?
    }
    // Final LinkLength is always 0 since that last bone in some sense has zero length... very stupid
    LinkLengths[LinkLengths.Length-1] = 0;
  }

  void FixedUpdate() {
    // CurrentLinkPositions.CopyFrom(ReferencePositions);
    AnimationRuntimeUtils.SolveFABRIK(ref CurrentLinkPositions, ref LinkLengths, Goal.position, Tolerance, Reach, Iterations);
    var refDir = ReferenceBones[0].forward;
    var newDir = (CurrentLinkPositions[1] - CurrentLinkPositions[0]).normalized;
    Quaternion rotation;
    if (Mathf.Approximately(Vector3.Dot(refDir, newDir), -1)) {
      rotation = Quaternion.AngleAxis(180f, ReferenceBones[0].up);
    } else {
      rotation = Quaternion.FromToRotation(refDir, newDir);
    }
    Bones[0].localRotation = AnimationPose[0] * rotation;
    Debug.DrawRay(CurrentLinkPositions[0], newDir * LinkLengths[0], Color.white);
    for (int i = 1; i < Bones.Length; i++) {
      refDir = (CurrentLinkPositions[i] - CurrentLinkPositions[i - 1]).normalized;
      newDir = (i < Bones.Length - 1)
        ? (CurrentLinkPositions[i + 1] - CurrentLinkPositions[i]).normalized
        : Goal.forward;
      Debug.DrawRay(CurrentLinkPositions[i], newDir * LinkLengths[i], Color.white);
      // TODO: This case probably also needs to solve the degenerate case though it is way less obvious
      // how to obtain a rotation axis... This sort of highlights why the full version of
      // this system might need to do parallel-transport to build proper continuous
      // local reference frames for all the bones
      if (Mathf.Approximately(Vector3.Dot(refDir, newDir), -1)) {  // almost opposite
        rotation = Quaternion.AngleAxis(180f, Vector3.Cross(refDir, Vector3.up));
      } else {
        rotation = Quaternion.FromToRotation(refDir, newDir);
      }
      Bones[i].localRotation = AnimationPose[i] * rotation;
    }
  }

  void OnDestroy() {
    AnimationPose.Dispose();
    ReferencePose.Dispose();
    CurrentLinkPositions.Dispose();
    ReferencePositions.Dispose();
    LinkLengths.Dispose();
  }
}