using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct LeanJob : IAnimationJob {
  public TransformSceneHandle RootHandle;
  public TransformStreamHandle HipHandle;
  public float LeanForwardRadians;
  public float LeanSidewaysRadians;

  public static LeanJob From(
  Animator animator,
  Transform root,
  Transform hip,
  float leanForwardRadians,
  float leanSidewaysRadians) {
    return new LeanJob {
      HipHandle = animator.BindStreamTransform(hip),
      RootHandle = animator.BindSceneTransform(root),
      LeanForwardRadians = leanForwardRadians,
      LeanSidewaysRadians = leanSidewaysRadians
    };
  }

  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    var rootPosition = RootHandle.GetPosition(stream);
    var rootRotation = RootHandle.GetRotation(stream);
    var rootForward = rootRotation * new float3(0,0,1);
    var rootRight = rootRotation * new float3(1,0,0);
    var hipPosition = HipHandle.GetPosition(stream);
    var hipLocalRotation = HipHandle.GetLocalRotation(stream);
    var leanRotation = quaternion.Euler(LeanForwardRadians, 0, -LeanSidewaysRadians);
    var hipLeanRotation = rootRotation * leanRotation;
    hipPosition -= rootPosition;
    hipPosition = math.mul(hipLeanRotation, hipPosition);
    hipPosition += rootPosition;
    HipHandle.SetLocalRotation(stream, leanRotation * hipLocalRotation);
    HipHandle.SetPosition(stream, hipPosition);
  }
}