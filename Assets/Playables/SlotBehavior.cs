using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;

public class SlotBehavior : PlayableBehaviour {
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
    Graph.DestroySubgraph(AnimationMixer);
    Graph.DestroySubgraph(AudioMixer);
  }

  public void PlayAnimation(Playable playable, int outputIndex = 0) {
    AnimationMixer.AddInput(playable, outputIndex, 1);
  }

  public void PlayAudio(Playable playable, int outputIndex = 0) {
    AudioMixer.AddInput(playable, outputIndex, 1);
  }
}