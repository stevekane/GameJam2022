using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

public static class PlayablesExtensions {
  public static void CopyTRS(
  this ReadWriteTransformHandle handle,
  AnimationStream source,
  AnimationStream target) {
    handle.SetLocalTRS(
      target,
      handle.GetLocalPosition(source),
      handle.GetLocalRotation(source),
      handle.GetLocalScale(source));
  }

  public static void CopyTRS(
  this ReadWriteTransformHandle sourceHandle,
  AnimationStream sourceStream,
  ReadWriteTransformHandle targetHandle,
  AnimationStream targetStream) {
    targetHandle.SetLocalTRS(
      targetStream,
      sourceHandle.GetLocalPosition(sourceStream),
      sourceHandle.GetLocalRotation(sourceStream),
      sourceHandle.GetLocalScale(sourceStream));
  }

  public static void BlendTRS(
  this ReadWriteTransformHandle handle,
  AnimationStream baseStream,
  AnimationStream blendStream,
  AnimationStream outStream,
  float blendWeight) {
    handle.GetLocalTRS(baseStream, out var basePosition, out var baseRotation, out var baseScale);
    handle.GetLocalTRS(blendStream, out var blendPosition, out var blendRotation, out var blendScale);
    handle.SetLocalTRS(
      outStream,
      math.lerp(basePosition, blendPosition, blendWeight),
      math.slerp(baseRotation, blendRotation, blendWeight),
      math.lerp(baseScale, blendScale, blendWeight)
    );
  }

  public static bool Connect<U, V>(this PlayableGraph graph, U source, int sourceOutputPort, V destination, int destinationInputPort, float weight) where U : struct, IPlayable where V : struct, IPlayable {
    if (!graph.Connect(source, sourceOutputPort, destination, destinationInputPort))
      return false;
    destination.SetInputWeight(destinationInputPort, weight);
    return true;
  }
}