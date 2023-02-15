using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;

[BurstCompile]
struct SampleLocalRotation : IAnimationJob {
  ReadOnlyTransformHandle Handle;
  NativeArray<quaternion> Value;
  public SampleLocalRotation(ReadOnlyTransformHandle handle, NativeArray<quaternion> value) {
    Handle = handle;
    Value = value;
  }
  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    Value[0] = Handle.GetLocalRotation(stream);
  }
}

[BurstCompile]
struct CorrectSpineRotationJob : IAnimationJob {
  public NativeArray<quaternion> LowerRoot;
  public NativeArray<quaternion> UpperRoot;
  public NativeArray<quaternion> LowerSpine;
  public NativeArray<quaternion> UpperSpine;
  public ReadWriteTransformHandle SpineHandle;
  public void ProcessRootMotion(AnimationStream stream) {}
  public void ProcessAnimation(AnimationStream stream) {
    var hipsLowerRotation = LowerRoot[0];
    var hipsUpperRotation = UpperRoot[0];
    var spineLowerRotation = LowerSpine[0];
    var spineUpperRotation = UpperSpine[0];
    spineUpperRotation = math.mul(hipsUpperRotation, spineUpperRotation);
    spineUpperRotation = math.mul(math.inverse(hipsLowerRotation), spineUpperRotation);
    spineUpperRotation = math.slerp(spineLowerRotation, spineUpperRotation, 1);
    SpineHandle.SetLocalRotation(stream, spineUpperRotation);
  }
}

public class PlayablesTests : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] AvatarMask UpperBodyMask;
  [SerializeField] Transform Root;
  [SerializeField] Transform Spine;
  [SerializeField] AnimationClip LowerClip;
  [SerializeField] AnimationClip UpperClip;

  PlayableGraph Graph;
  NativeArray<quaternion> LowerRoot;
  NativeArray<quaternion> UpperRoot;
  NativeArray<quaternion> LowerSpine;
  NativeArray<quaternion> UpperSpine;

  AnimationScriptPlayable Sampler(Transform t, NativeArray<quaternion> value) {
    var job = new SampleLocalRotation(ReadOnlyTransformHandle.Bind(Animator, t), value);
    var playable = AnimationScriptPlayable.Create(Graph, job);
    return playable;
  }

  AnimationScriptPlayable SpineCorrector() {
    var job = new CorrectSpineRotationJob {
      LowerRoot = LowerRoot,
      UpperRoot = UpperRoot,
      LowerSpine = LowerSpine,
      UpperSpine = UpperSpine,
      SpineHandle = ReadWriteTransformHandle.Bind(Animator, Spine)
    };
    var playable = AnimationScriptPlayable.Create(Graph, job);
    return playable;
  }

  NativeArray<quaternion> ValueArray() {
    return new(1, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
  }

  void Start() {
    Graph = PlayableGraph.Create("Playables Tests");
    var lowerClip = AnimationClipPlayable.Create(Graph, LowerClip);
    var upperClip = AnimationClipPlayable.Create(Graph, UpperClip);
    var lowerRootSampler = Sampler(Root, LowerRoot = ValueArray());
    var upperRootSampler = Sampler(Root, UpperRoot = ValueArray());
    var lowerSpineSampler = Sampler(Spine, LowerSpine = ValueArray());
    var upperSpineSampler = Sampler(Spine, UpperSpine = ValueArray());
    var spineUpMask = new AvatarMask();
    var mixer = AnimationLayerMixerPlayable.Create(Graph);
    var spineCorrector = SpineCorrector();
    var output = AnimationPlayableOutput.Create(Graph, "Output", Animator);
    lowerRootSampler.AddInput(lowerClip, 0, 1);
    lowerSpineSampler.AddInput(lowerRootSampler, 0, 1);
    mixer.AddInput(lowerSpineSampler, 0, 1);
    upperRootSampler.AddInput(upperClip, 0, 1);
    upperSpineSampler.AddInput(upperRootSampler, 0, 1);
    mixer.AddInput(upperSpineSampler, 0, 1);
    mixer.SetLayerMaskFromAvatarMask(1, UpperBodyMask);
    spineCorrector.AddInput(mixer, 0, 1);
    output.SetSourcePlayable(spineCorrector);
    Graph.Play();
  }

  void Update() {

  }

  void OnDestroy() {
    LowerSpine.Dispose();
    LowerRoot.Dispose();
    UpperSpine.Dispose();
    UpperRoot.Dispose();
    Graph.Destroy();
  }
}