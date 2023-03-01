using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[BurstCompile]
public struct SplitBodyMixerJob : IAnimationJob {
  public NativeList<ReadWriteTransformHandle> LowerBones;
  public NativeList<ReadWriteTransformHandle> UpperBones;
  public ReadWriteTransformHandle LowerRootBone;
  public ReadWriteTransformHandle UpperRootBone;

  public void ProcessRootMotion(AnimationStream stream) {
    var lower = stream.GetInputStream(0);
    stream.velocity = lower.velocity;
    stream.angularVelocity = lower.angularVelocity;
  }

  public void ProcessAnimation(AnimationStream stream) {
    var lower = stream.GetInputStream(0);
    var upper = stream.GetInputStream(1);
    var inputWeight = stream.GetInputWeight(1);
    for (var i = 0; i < LowerBones.Length; i++) {
      LowerBones[i].CopyTRS(lower, stream);
    }
    for (var i = 0; i < UpperBones.Length; i++) {
      UpperBones[i].BlendTRS(lower, upper, stream, inputWeight);
    }
    var spineLowerRotation = UpperRootBone.GetLocalRotation(lower);
    var spineUpperRotation = UpperRootBone.GetLocalRotation(upper);
    var hipsLowerRotation = LowerRootBone.GetLocalRotation(lower); // effectively mesh-space since root
    var hipsUpperRotation = LowerRootBone.GetLocalRotation(upper); // effectively mesh-space since root
    spineUpperRotation = hipsUpperRotation * spineUpperRotation;
    spineUpperRotation = math.inverse(hipsLowerRotation) * spineUpperRotation;
    spineUpperRotation = math.slerp(spineLowerRotation, spineUpperRotation, inputWeight);
    UpperRootBone.SetLocalRotation(stream, spineUpperRotation);
  }
}