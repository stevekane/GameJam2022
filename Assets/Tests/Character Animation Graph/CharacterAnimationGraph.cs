using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Audio;
using UnityEngine.Animations;

public class CharacterAnimationGraph : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;

  AnimatorControllerPlayable AnimatorController;
  AnimationLayerMixerPlayable AnimationLayerMixer;
  AnimationPlayableOutput AnimationOutput;
  ScriptPlayable<SlotBehavior> DefaultSlotPlayable;

  AudioMixerPlayable AudioMixer;
  AudioPlayableOutput AudioOutput;

  public PlayableGraph Graph { get; private set; }
  public SlotBehavior DefaultSlot { get; private set; }

  void Awake() {
    Graph = PlayableGraph.Create("Character Animation Graph");
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    AnimationLayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    AnimationOutput = AnimationPlayableOutput.Create(Graph, "Animation Output", Animator);
    AudioMixer = AudioMixerPlayable.Create(Graph);
    AudioOutput = AudioPlayableOutput.Create(Graph, "Audio Output", AudioSource);
    DefaultSlotPlayable = ScriptPlayable<SlotBehavior>.Create(Graph);
    DefaultSlotPlayable.SetTraversalMode(PlayableTraversalMode.Passthrough);
    DefaultSlot = DefaultSlotPlayable.GetBehaviour();
    Debug.LogWarning("Reconnect animator controller to animation mixer of characteranimationgraph");
    // AnimationLayerMixer.AddInput(AnimatorController, 0, 1);
    AnimationLayerMixer.AddInput(DefaultSlotPlayable, sourceOutputIndex: 0, 1);
    AudioMixer.AddInput(DefaultSlotPlayable, sourceOutputIndex: 1, 1);
    AnimationOutput.SetSourcePlayable(AnimationLayerMixer);
    AudioOutput.SetSourcePlayable(AudioMixer);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }
}