using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Audio;
using UnityEngine.Timeline;

public class AudioGraph : MonoBehaviour {
  [SerializeField] AudioSource AudioSource;

  PlayableGraph Graph;
  ScriptPlayable<TimelinePlayable> CurrentTimeline;
  AudioMixerPlayable Mixer;
  AudioPlayableOutput Output;

  void Awake() {
    Graph = PlayableGraph.Create($"AudioGraph ({name})");
    Graph.SetTimeUpdateMode(DirectorUpdateMode.DSPClock);
    Graph.Play();
    Mixer = AudioMixerPlayable.Create(Graph);
    Output = AudioPlayableOutput.Create(Graph, $"Output ({AudioSource.name})", AudioSource);
    Output.SetSourcePlayable(Mixer);
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
    var tracks = timelineAsset.Tracks(type => type == typeof(AudioSource));
    var playable = TimelinePlayable.Create(Graph, tracks, gameObject, false, false);
    playable.SetTime(0);
    playable.SetDuration(timelineAsset.duration);
    playable.SetOutputCount(playable.GetInputCount());
    if (!CurrentTimeline.IsNull()) {
      Stop();
    }
    foreach (var (track, port) in tracks.WithIndex()) {
      var noop = ScriptPlayable<NoopBehavior>.Create(Graph);
      noop.AddInput(playable, port, 1);
      Mixer.AddInput(noop, 0, 1);
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