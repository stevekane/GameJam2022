using System;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;

[Serializable]
public class BoundTimeline {
  public TimelineAsset TimelineAsset;
  public Animator Animator;
  public AudioSource AudioSource;
  public Collider Hitbox;
  public WeaponTrail WeaponTrail;
}

public class TimelineMiner : MonoBehaviour {
  [SerializeField] BoundTimeline BoundTimeline;

  PlayableGraph Graph;
  ScriptPlayable<TimelinePlayable> Timeline;

  void Start() {
    Graph = PlayableGraph.Create("Timeline Stripper");
    Timeline = TimelinePlayable.Create(Graph, BoundTimeline.TimelineAsset.GetRootTracks(), gameObject, false, false);
    Timeline.SetTime(0);
    Timeline.SetDuration(BoundTimeline.TimelineAsset.duration);
    Timeline.SetOutputCount(BoundTimeline.TimelineAsset.outputTrackCount);
    for (var i = 0; i < BoundTimeline.TimelineAsset.outputTrackCount; i++) {
      var track = BoundTimeline.TimelineAsset.GetOutputTrack(i);
      if (!track.CanCreateTrackMixer())
        continue;
      if (track.isEmpty)
        continue;
      foreach (var output in track.outputs) {
        var playableOutput = output.outputTargetType switch {
          Type type when type == typeof(Animator) => AnimationPlayableOutput.Create(Graph, track.name, BoundTimeline.Animator),
          Type type when type == typeof(AudioSource) => AudioPlayableOutput.Create(Graph, track.name, BoundTimeline.AudioSource),
          Type type when type == typeof(Collider) => ObjectPlayableOutput(Graph, track.name, BoundTimeline.Hitbox),
          Type type when type == typeof(WeaponTrail) => ObjectPlayableOutput(Graph, track.name, BoundTimeline.WeaponTrail),
          _ => PlayableOutput.Null
        };
        playableOutput.SetSourcePlayable(Timeline, i);
      }
    }
    Graph.Play();
  }

  PlayableOutput ObjectPlayableOutput(PlayableGraph graph, string name, UnityEngine.Object obj) {
    var output = ScriptPlayableOutput.Create(graph, name);
    output.SetUserData(obj);
    output.SetReferenceObject(obj);
    return output;
  }

  void OnDestroy() {
    Graph.Destroy();
  }
}