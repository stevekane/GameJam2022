using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[BurstCompile]
public struct MeshSpaceBlendPerBoneJob : IAnimationJob {
  public NativeList<ReadWriteTransformHandle> BaseHandles;
  public NativeList<ReadWriteTransformHandle> BlendHandles;

  public void ProcessRootMotion(AnimationStream stream) {
    var baseStream = stream.GetInputStream(0);
    stream.velocity = baseStream.velocity;
    stream.angularVelocity = baseStream.angularVelocity;
  }

  public void ProcessAnimation(AnimationStream stream) {
    var baseStream = stream.GetInputStream(0);
    var blendStream = stream.GetInputStream(1);
    var blendWeight = stream.GetInputWeight(1);
    var baseHandleCount = BaseHandles.Length;
    for (var i = 0; i < baseHandleCount; i++) {
      BaseHandles[i].CopyTRS(baseStream, stream);
    }
    var blendHandleCount = BlendHandles.Length;
    for (var i = 0; i < blendHandleCount; i++) {
      var handle = BlendHandles[i];
      handle.GetGlobalTR(baseStream, out var basePosition, out var baseRotation);
      handle.GetGlobalTR(blendStream, out var blendPosition, out var blendRotation);
      var position = math.lerp(basePosition, blendPosition, blendWeight);
      var rotation = math.slerp(baseRotation, blendRotation, blendWeight);
      handle.SetGlobalTR(stream, position, rotation);
    }
  }
}
