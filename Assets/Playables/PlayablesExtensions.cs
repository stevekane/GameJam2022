using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public static class PlayablesExtensions  {
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
}