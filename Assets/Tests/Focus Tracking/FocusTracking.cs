using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Animations;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct FocusTrackingJob : IAnimationJob {
  public NativeArray<Vector3> CurrentLinkPositions;
  public NativeArray<float> LinkLengths;
  public NativeArray<TransformStreamHandle> Bones;
  public Vector3 GoalPosition;
  public Quaternion GoalRotation;
  public float Tolerance;
  public float Reach;
  public int Iterations;
  public int ReferenceStreamIndex; // 0
  public int AnimationStreamIndex; // 1

  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    AnimationRuntimeUtils.SolveFABRIK(ref CurrentLinkPositions, ref LinkLengths, GoalPosition, Tolerance, Reach, Iterations);
    var referenceStream = stream.GetInputStream(ReferenceStreamIndex);
    var animationStream = stream.GetInputStream(AnimationStreamIndex);
    var referenceRotation = Bones[0].GetRotation(referenceStream);
    var animationLocalRotation = Bones[0].GetLocalRotation(animationStream);
    var refDir = referenceRotation * new float3(0,0,1); // world space reference direction
    var newDir = (CurrentLinkPositions[1] - CurrentLinkPositions[0]).normalized; // world space new direction
    var IKRotation = Quaternion.FromToRotation(refDir, newDir); // TODO: degenerae when vectors co-linear
    Bones[0].SetLocalRotation(stream, animationLocalRotation * IKRotation);
    Debug.DrawRay(CurrentLinkPositions[0], newDir * LinkLengths[0], Color.white);

    for (int i = 1; i < Bones.Length; i++) {
      animationLocalRotation = Bones[i].GetLocalRotation(animationStream);
      refDir = (CurrentLinkPositions[i] - CurrentLinkPositions[i - 1]).normalized;
      newDir = (i < Bones.Length - 1)
        ? (CurrentLinkPositions[i + 1] - CurrentLinkPositions[i]).normalized
        : GoalRotation * new Vector3(0,0,1);
      IKRotation = Quaternion.FromToRotation(refDir, newDir); // TODO: degenerate when vectors co-linear
      Bones[i].SetLocalRotation(stream, animationLocalRotation * IKRotation);
      Debug.DrawRay(CurrentLinkPositions[i], newDir * LinkLengths[i], Color.white);
    }
  }
}

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