using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Audio;
using UnityEngine.Animations;

public static class PlayableExtensions {
  public static IEnumerable<Playable> Inputs<U>(this U playable) where U : struct, IPlayable {
    if (!playable.IsNull() && playable.IsValid()) {
      var inputCount = playable.GetInputCount();
      for (var i = 0; i < inputCount; i++) {
        yield return playable.GetInput(i);
      }
    }
  }
  public static void SafeDestroy<U>(this U playable) where U : struct, IPlayable {
    if (!playable.IsNull() && playable.IsValid()) {
      playable.Destroy();
      foreach (var input in playable.Inputs()) {
        SafeDestroy(input);
      }
    }
  }
}

public class SlotBehavior : PlayableBehaviour {
  List<Playable> Playables = new();
  PlayableGraph Graph;
  Playable Playable;
  AnimationMixerPlayable AnimationMixer;
  AudioMixerPlayable AudioMixer;

  public override void OnPlayableCreate(Playable playable) {
    Playable = playable;
    Graph = playable.GetGraph();
    AnimationMixer = AnimationMixerPlayable.Create(Graph);
    AudioMixer = AudioMixerPlayable.Create(Graph);
    Playable.SetOutputCount(2);
    Playable.AddInput(AnimationMixer, 0, 1);
    Playable.AddInput(AudioMixer, 0, 1);
  }

  public override void OnPlayableDestroy(Playable playable) {
    AnimationMixer.SafeDestroy();
    AudioMixer.SafeDestroy();
    Playables.ForEach(PlayableExtensions.SafeDestroy);
  }

  public void PlayAnimation(Playable playable, int outputIndex = 0) {
    AnimationMixer.AddInput(playable, outputIndex, 1);
  }

  public void PlayAudio(Playable playable, int outputIndex = 0) {
    AudioMixer.AddInput(playable, outputIndex, 1);
  }
}

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

  void Start() {
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