using Unity.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Audio;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

public class CharacterAnimationGraph : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Transform RootBone;
  [SerializeField] Transform SpineBone;

  NativeList<ReadWriteTransformHandle> BaseHandles;
  NativeList<ReadWriteTransformHandle> BlendHandles;
  AnimatorControllerPlayable AnimatorController;
  AnimationLayerMixerPlayable FullBodyLayerMixer;
  AnimationScriptPlayable SplitBodyMixer;
  AnimationPlayableOutput AnimationOutput;
  ScriptPlayable<SlotBehavior> DefaultSlotPlayable;
  ScriptPlayable<SlotBehavior> UpperBodySlotPlayable;
  AudioMixerPlayable AudioMixer;
  AudioPlayableOutput AudioOutput;

  public PlayableGraph Graph { get; private set; }
  public SlotBehavior DefaultSlot { get; private set; }
  public SlotBehavior UpperBodySlot { get; private set; }

  void Awake() {
    Graph = PlayableGraph.Create("Character Animation Graph");
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    // Default Slot
    DefaultSlotPlayable = ScriptPlayable<SlotBehavior>.Create(Graph);
    DefaultSlotPlayable.SetTraversalMode(PlayableTraversalMode.Passthrough);
    DefaultSlot = DefaultSlotPlayable.GetBehaviour();
    // Upper Body Slot
    UpperBodySlotPlayable = ScriptPlayable<SlotBehavior>.Create(Graph);
    UpperBodySlotPlayable.SetTraversalMode(PlayableTraversalMode.Passthrough);
    UpperBodySlot = UpperBodySlotPlayable.GetBehaviour();
    // Full Body Layer Mixer
    FullBodyLayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    FullBodyLayerMixer.AddInput(AnimatorController, 0, 1);
    FullBodyLayerMixer.AddInput(DefaultSlotPlayable, 0, 1);
    // Split Body Mixer
    BaseHandles = new(Allocator.Persistent);
    BlendHandles = new(Allocator.Persistent);
    CollectBoneHandles(RootBone, BaseHandles);
    var splitBodyMixerJob = new SplitBodyMixerJob {
      LowerRootBone = ReadWriteTransformHandle.Bind(Animator, RootBone),
      UpperRootBone = ReadWriteTransformHandle.Bind(Animator, SpineBone),
      LowerBones = BaseHandles,
      UpperBones = BlendHandles
    };
    SplitBodyMixer = AnimationScriptPlayable.Create(Graph, splitBodyMixerJob);
    SplitBodyMixer.SetProcessInputs(false);
    SplitBodyMixer.AddInput(FullBodyLayerMixer, 0, 1);
    SplitBodyMixer.AddInput(UpperBodySlotPlayable, 0, 1);
    // Audio Mixer
    AudioMixer = AudioMixerPlayable.Create(Graph);
    AudioMixer.AddInput(DefaultSlotPlayable, 1, 1);
    AudioMixer.AddInput(UpperBodySlotPlayable, 1, 1);
    //Animation Output
    AnimationOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
    AnimationOutput.SetSourcePlayable(SplitBodyMixer);
    // Audio Output
    AudioOutput = AudioPlayableOutput.Create(Graph, "Audio Output", AudioSource);
    AudioOutput.SetSourcePlayable(AudioMixer);
    Graph.Play();
  }

  void OnDestroy() {
    BaseHandles.Dispose();
    BlendHandles.Dispose();
    Graph.Destroy();
  }

  void CollectBoneHandles(Transform t, NativeList<ReadWriteTransformHandle> list) {
    if (t == SpineBone)
      list = BlendHandles;
    list.Add(ReadWriteTransformHandle.Bind(Animator, t));
    var childCount = t.childCount;
    for (var i = 0; i < childCount; i++) {
      CollectBoneHandles(t.GetChild(i), list);
    }
  }
}