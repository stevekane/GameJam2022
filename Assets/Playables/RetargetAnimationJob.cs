using Unity.Collections;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public struct RetargetingJob : IAnimationJob {
  public NativeArray<ReadWriteTransformHandle> SourceHandles;
  public NativeArray<ReadWriteTransformHandle> TargetHandles;

  public RetargetingJob(
  NativeArray<ReadWriteTransformHandle> sourceHandles,
  NativeArray<ReadWriteTransformHandle> targetHandles) {
    SourceHandles = sourceHandles;
    TargetHandles = targetHandles;
  }

  public void ProcessRootMotion(AnimationStream stream) {
    // TODO: Should something happen here?
  }

  public void ProcessAnimation(AnimationStream stream) {
    var sourceStream = stream.GetInputStream(0);
    for (var i = 0; i < SourceHandles.Length; i++) {
      SourceHandles[i].CopyTRS(sourceStream, TargetHandles[i], stream);
    }
  }
}