using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using Unity.Collections;
using Unity.Burst;

[BurstCompile]
public struct FocusTrackingJob : IAnimationJob {
  public NativeArray<TransformStreamHandle> Bones;
  public Quaternion EffectorTargetRotation;
  public float Weight;
  public int ReferenceStreamIndex; // 0
  public int AnimationStreamIndex; // 1

  public void ProcessRootMotion(AnimationStream stream) {
    stream.velocity = stream.GetInputStream(AnimationStreamIndex).velocity;
    stream.angularVelocity = stream.GetInputStream(AnimationStreamIndex).angularVelocity;
  }

  public void ProcessAnimation(AnimationStream stream) {
    var lastBoneIndex = Bones.Length-1;
    var referenceStream = stream.GetInputStream(ReferenceStreamIndex);
    var animationStream = stream.GetInputStream(AnimationStreamIndex);
    var effectorReferenceRotation = Bones[lastBoneIndex].GetRotation(referenceStream);
    for (var i = 0; i < Bones.Length; i++) {
      var twistWeight = (float)i/lastBoneIndex;
      var referenceRotation = Bones[i].GetRotation(referenceStream);
      var animationRotation = Bones[i].GetRotation(animationStream);
      var additiveRotation = Quaternion.Lerp(effectorReferenceRotation, EffectorTargetRotation, twistWeight);
      var boneRotation = Quaternion.Lerp(animationRotation, referenceRotation * additiveRotation, Weight);
      Bones[i].SetRotation(stream, boneRotation);
    }
  }
}

[Serializable]
public struct FocusTrackingData : IAnimationJobData {
  [Range(0,1)]
  public float Weight;
  public Transform Root;
  public Transform Tip;
  public Transform Effector;
  public bool IsValid() => true;
  public void SetDefaultValues() {
    Root = null;
    Tip = null;
    Effector = null;
  }
}

public class FocusTrackingDataBinder : AnimationJobBinder<FocusTrackingJob, FocusTrackingData> {
  public NativeArray<TransformStreamHandle> Bones;
  public int ReferenceStreamIndex = 0;
  public int AnimationStreamIndex = 1;

  public override FocusTrackingJob Create(Animator animator, ref FocusTrackingData data, Component component) {
    var bones = ConstraintsUtils.ExtractChain(data.Root, data.Tip);
    Bones = new NativeArray<TransformStreamHandle>(bones.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    for (var i = 0; i < bones.Length; i++) {
      Bones[i] = animator.BindStreamTransform(bones[i]);
    }
    return new() {
      Bones = Bones,
      EffectorTargetRotation = data.Effector.rotation,
      ReferenceStreamIndex = ReferenceStreamIndex,
      AnimationStreamIndex = AnimationStreamIndex
    };
  }
  public override void Destroy(FocusTrackingJob job) {
    Bones.Dispose();
  }
}

public class FocusTrackingGraph : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] FocusTrackingData FocusTrackingData;
  [SerializeField] AnimationClipAsset ReferenceClipAsset;
  [SerializeField] AnimationClipAsset AnimationClipAsset;
  [SerializeField] Transform Target;

  PlayableGraph Graph;
  FocusTrackingJob FocusTrackingJob;
  FocusTrackingDataBinder FocusTrackingDataBinder;
  AnimationClipPlayable AnimationClip;
  AnimationClipPlayable ReferenceClip;
  AnimationScriptPlayable FocusTracking;
  AnimationPlayableOutput Output;

  void Start() {
    Graph = PlayableGraph.Create("Focus Tracking");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    ReferenceClip = (AnimationClipPlayable)ReferenceClipAsset.CreatePlayable(Graph, gameObject);
    AnimationClip = (AnimationClipPlayable)AnimationClipAsset.CreatePlayable(Graph, gameObject);
    FocusTrackingDataBinder = new();
    FocusTrackingJob = FocusTrackingDataBinder.Create(Animator, ref FocusTrackingData, null);
    FocusTracking = AnimationScriptPlayable.Create(Graph, FocusTrackingJob);
    FocusTracking.AddInput(ReferenceClip, 0, 1);
    FocusTracking.AddInput(AnimationClip, 0, 1);
    Output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    Output.SetSourcePlayable(FocusTracking);
  }

  void OnDestroy() {
    FocusTracking.Destroy();
    Graph.Destroy();
  }

  void FixedUpdate() {
    // simple look-at behavior for testing
    FocusTrackingData.Effector.LookAt(Target.position, Vector3.up);
    FocusTrackingJob.Weight = FocusTrackingData.Weight;
    FocusTrackingJob.EffectorTargetRotation = FocusTrackingData.Effector.rotation;
    FocusTracking.SetJobData(FocusTrackingJob);
    Graph.Evaluate(Time.fixedDeltaTime);
  }
}