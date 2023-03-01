using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Timeline;

public class AnimationGraph : MonoBehaviour {
  [SerializeField] Animator Animator;
  [SerializeField] AnimationClip AnimationClip;

  PlayableGraph Graph;
  ScriptPlayable<TimelinePlayable> CurrentTimeline;
  AnimationLayerMixerPlayable LayerMixer;
  AnimatorControllerPlayable AnimatorController;
  AnimationMixerPlayable Mixer;
  AnimationPlayableOutput Output;

  void Awake() {
    Graph = PlayableGraph.Create($"AnimationGraph ({name})");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
    Graph.Play();
    AnimatorController = AnimatorControllerPlayable.Create(Graph, Animator.runtimeAnimatorController);
    Mixer = AnimationMixerPlayable.Create(Graph);
    LayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    LayerMixer.AddInput(AnimatorController, 0, 1);
    LayerMixer.AddInput(Mixer, 0, 1);
    Output = AnimationPlayableOutput.Create(Graph, $"Output ({Animator.name})", Animator);
    Output.SetSourcePlayable(LayerMixer);
  }

  void Update() {
    if (!CurrentTimeline.IsNull() && CurrentTimeline.IsDone()) {
      Stop();
    }
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  public ScriptPlayable<TimelinePlayable> PlayTimeline(TimelineAsset timelineAsset) {
    var tracks = timelineAsset.Tracks(type => type == typeof(Animator));
    var playable = TimelinePlayable.Create(Graph, tracks, gameObject, false, false);
    playable.SetTime(0);
    playable.SetDuration(timelineAsset.duration);
    playable.SetOutputCount(playable.GetInputCount());
    if (!CurrentTimeline.IsNull()) {
      Stop();
    }
    LayerMixer.SetInputWeight(1, 1);
    foreach (var (track, port) in tracks.WithIndex()) {
      var noop = AnimationScriptPlayable.Create(Graph, new AnimationNoopJob());
      noop.SetProcessInputs(true);
      noop.AddInput(playable, port, 1);
      Mixer.AddInput(noop, port, 1);
    }
    CurrentTimeline = playable;
    return playable;
  }

  public void Stop() {
    var inputCount = Mixer.GetInputCount();
    for (var i = 0; i < inputCount; i++) {
      Mixer.GetInput(i).Destroy();
    }
    Mixer.SetInputCount(0);
    Graph.DestroySubgraph(CurrentTimeline);
    CurrentTimeline = ScriptPlayable<TimelinePlayable>.Null;
  }
}