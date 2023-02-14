using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Animations.Rigging;

public class MeshSpaceRotationMixer: MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] Transform RootBone;
  [SerializeField] Transform SpineBone;
  [SerializeField] AnimationClip SlotClip;
  [SerializeField, Range(0,1)] float MixerBlend;

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
    Graph = PlayableGraph.Create("Mesh-Space Rotation Blending");
    var animController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    var slotClip = AnimationClipPlayable.Create(Graph, SlotClip);
    var blendPerBoneJob = new MeshSpaceBlendPerBoneJob {
      BaseHandles = BaseHandles,
      BlendHandles = BlendHandles
    };
    BlendPerBone = AnimationScriptPlayable.Create(Graph, blendPerBoneJob);
    BlendPerBone.SetProcessInputs(false);
    BlendPerBone.AddInput(animController, 0, 1);
    BlendPerBone.AddInput(slotClip, 0, MixerBlend);
    var animOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
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