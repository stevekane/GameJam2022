using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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
    }
    // Final LinkLength is always 0 since that last bone in some sense has zero length... very stupid
    LinkLengths[LinkLengths.Length-1] = 0;
  }

  /*
  For each bone, compute the local rotation by comparing it to
  its parents forward vector.

  For the zeroeth bone, use the identity.

  THIS IS WHERE TO PICK UP. The TESTROTATIONS are obtained by logging the angular values
  then plugging themback into this setup to verify that the result matches what I would expect
  intuitively. This is the case, so what remains then is to use the previous bones forward,
  your forward, to build a localRotation for each bone.
  */
  void FixedUpdate() {
    CurrentLinkPositions.CopyFrom(ReferencePositions);
    float[] TESTROTATIONS = new float[5] { 30.8f, 8, 12, 9, 30 };
    AnimationRuntimeUtils.SolveFABRIK(ref CurrentLinkPositions, ref LinkLengths, Goal.position, Tolerance, Reach, Iterations);
    for(int i = 0; i < Bones.Length; i++) {
      var refDir = Vector3.forward; // Not sure but this would be world space forward
      var newDir = (i < Bones.Length - 1)
        ? (CurrentLinkPositions[i + 1] - CurrentLinkPositions[i]).normalized
        : Goal.forward;
      var refDirLocal = ReferenceBones[i].InverseTransformDirection(refDir);
      var newDirLocal = ReferenceBones[i].InverseTransformDirection(newDir);

      Debug.Log($"{Vector3.Angle(refDir, newDir)}");
      Debug.DrawRay(CurrentLinkPositions[i], newDir * LinkLengths[i], Color.white);
      // var rotation = Quaternion.FromToRotation(refDirLocal, newDirLocal);
      var rotation = Quaternion.Euler(0, TESTROTATIONS[i], 0);
      Bones[i].localRotation = rotation * AnimationPose[i];
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