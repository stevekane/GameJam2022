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

  // This is the original implementation that uses a timeline and all that.
  // I am trying a solution where I compile my own playable for each animation track
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

  /*
  public Playable PlayTimeline(TimelineAsset timelineAsset) {
    var clip = AnimationClipPlayable.Create(Graph, AnimationClip);
    clip.SetApplyPlayableIK(true);
    clip.SetApplyFootIK(true);
    LayerMixer.AddInput(clip, 0, 1);
    return clip;
    // var tracks = timelineAsset.Tracks(type => type == typeof(Animator));
    // foreach (var track in tracks) {
    //   if (track.muted)
    //     continue;
    //   var mixer = AnimationMixerPlayable.Create(Graph, 0);
    //   var animTrack = track;
    //   foreach (var clip in track.GetClips()) {
    //     var clipPlayable = AnimationClipPlayable.Create(Graph, AnimationClip);
    //     // var clipPlayable = AnimationClipPlayable.Create(Graph, clip.animationClip);
    //     // clipPlayable.SetApplyFootIK(true);
    //     mixer.AddInput(clipPlayable, 0, 1);
    //   }
    //   var layerMixer = AnimationLayerMixerPlayable.Create(Graph, 0);
    //   layerMixer.AddInput(mixer, 0, 1);
    //   Mixer.AddInput(layerMixer, 0, 1);
    // }
    // return LayerMixer;
  }
  */

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