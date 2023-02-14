using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;
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
    for (var i = 0; i < LowerBones.Length; i++) {
      LowerBones[i].CopyTRS(lower, stream);
    }
    for (var i = 0; i < UpperBones.Length; i++) {
      UpperBones[i].CopyTRS(upper, stream);
    }
    var hipsLowerRotation = LowerRootBone.GetRotation(lower);
    var hipsUpperRotation = LowerRootBone.GetRotation(upper);
    var spineUpperRotation = UpperRootBone.GetRotation(upper);
    var rotation = math.inverse(hipsLowerRotation) * hipsUpperRotation * spineUpperRotation;
    UpperRootBone.SetLocalRotation(stream, rotation);
  }
}

public struct SplitBodyMixer {
  public AnimationScriptPlayable Playable { get; private set; }

  NativeList<ReadWriteTransformHandle> LowerBones;
  NativeList<ReadWriteTransformHandle> UpperBones;
  ReadWriteTransformHandle LowerRootBone;
  ReadWriteTransformHandle UpperRootBone;

  public SplitBodyMixer(PlayableGraph graph, Animator animator, Transform lowerRoot, Transform upperRoot) {
    var lowerBones = new NativeList<ReadWriteTransformHandle>(Allocator.Persistent);
    var upperBones = new NativeList<ReadWriteTransformHandle>(Allocator.Persistent);
    LowerBones = lowerBones;
    UpperBones = upperBones;
    LowerRootBone = ReadWriteTransformHandle.Bind(animator, lowerRoot);
    UpperRootBone = ReadWriteTransformHandle.Bind(animator, upperRoot);
    CollectBoneHandles(animator, upperRoot, lowerRoot, LowerBones);
    void CollectBoneHandles(Animator animator, Transform upperRoot, Transform t, NativeList<ReadWriteTransformHandle> list) {
      list = t == upperRoot ? upperBones : list;
      list.Add(ReadWriteTransformHandle.Bind(animator, t));
      var childCount = t.childCount;
      for (var i = 0; i < childCount; i++) {
        CollectBoneHandles(animator, upperRoot, t.GetChild(i), list);
      }
    }
    var job = new SplitBodyMixerJob {
      LowerBones = LowerBones,
      UpperBones = UpperBones,
      LowerRootBone = LowerRootBone,
      UpperRootBone = UpperRootBone
    };
    Playable = AnimationScriptPlayable.Create(graph, job, 2);
    Playable.SetProcessInputs(false);
  }

  public void Dispose() {
    LowerBones.Dispose();
    UpperBones.Dispose();
  }
}