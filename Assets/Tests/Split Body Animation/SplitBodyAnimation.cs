using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public class SplitBodyAnimation : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] Transform RootBone;
  [SerializeField] Transform SpineBone;
  [SerializeField] AnimationClip UpperClip;
  [SerializeField, Range(0,1)] float MixerBlend = 1;

  PlayableGraph Graph;
  AnimationScriptPlayable SplitBodyMixer;
  NativeList<ReadWriteTransformHandle> BaseHandles;
  NativeList<ReadWriteTransformHandle> BlendHandles;

  void CollectBoneHandles(Transform t, NativeList<ReadWriteTransformHandle> list) {
    if (t == SpineBone)
      list = BlendHandles;
    list.Add(ReadWriteTransformHandle.Bind(Animator, t));
    var childCount = t.childCount;
    for (var i = 0; i < childCount; i++) {
      CollectBoneHandles(t.GetChild(i), list);
    }
  }

  void Start() {
    BaseHandles = new(Allocator.Persistent);
    BlendHandles = new(Allocator.Persistent);
    CollectBoneHandles(RootBone, BaseHandles);
    Graph = PlayableGraph.Create("Split Body Animation");
    var animController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    var upperClip = AnimationClipPlayable.Create(Graph, UpperClip);
    var splitBodyMixerJob = new SplitBodyMixerJob {
      LowerRootBone = ReadWriteTransformHandle.Bind(Animator, RootBone),
      UpperRootBone = ReadWriteTransformHandle.Bind(Animator, SpineBone),
      LowerBones = BaseHandles,
      UpperBones = BlendHandles
    };
    SplitBodyMixer = AnimationScriptPlayable.Create(Graph, splitBodyMixerJob);
    SplitBodyMixer.SetProcessInputs(false);
    SplitBodyMixer.AddInput(animController, 0, 1);
    SplitBodyMixer.AddInput(upperClip, 0, MixerBlend);
    var output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    output.SetSourcePlayable(SplitBodyMixer);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
    BaseHandles.Dispose();
    BlendHandles.Dispose();
  }

  void Update() {
    SplitBodyMixer.SetInputWeight(1, MixerBlend);
  }
}