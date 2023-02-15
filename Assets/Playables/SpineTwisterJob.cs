using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public struct SpineTwisterJob : IAnimationJob {
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