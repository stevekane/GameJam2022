using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Animations.Rigging;

[BurstCompile]
public struct BlendPerBoneJob : IAnimationJob {
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

public class MeshSpaceRotationMixer: MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] AnimationClip SlotClip;
  [SerializeField] Transform RootBone;
  [SerializeField] Transform SpineBone;
  [Range(0,1)]
  [SerializeField] float MixerBlend;

  PlayableGraph Graph;
  AnimationScriptPlayable BlendPerBone;
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
    Graph = PlayableGraph.Create("MeshSpaceRotation");
    var animController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    var slotClip = AnimationClipPlayable.Create(Graph, SlotClip);
    var blendPerBoneJob = new BlendPerBoneJob {
      BaseHandles = BaseHandles,
      BlendHandles = BlendHandles
    };
    BlendPerBone = AnimationScriptPlayable.Create(Graph, blendPerBoneJob);
    var animOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
    BlendPerBone.SetProcessInputs(false);
    BlendPerBone.AddInput(animController, 0, 1);
    BlendPerBone.AddInput(slotClip, 0, MixerBlend);
    animOutput.SetSourcePlayable(BlendPerBone, 0);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
    BaseHandles.Dispose();
    BlendHandles.Dispose();
  }

  void Update() {
    BlendPerBone.SetInputWeight(1, MixerBlend);
  }
}