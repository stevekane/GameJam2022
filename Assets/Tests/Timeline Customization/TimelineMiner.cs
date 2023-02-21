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

/*
Today's goals:

  Play(Timeline, Duration)
  Stop(Timeline, Duration)
  Pause(Timeline)
  Setup system that connects timelines to a graph for various statically-known outputs
  Create three separate timelines for the phases of the test attack
  Play them in sequence with small blending transitions and evaluate the result
  Create and test OnPlayableCreate/OnPlayableDestroy hooks for custom TrackMixers like Hitbox that do setup/teardown
  Create and test custom AnimationTrack with own mixer that does not include built-in tracks AbsoluteToRoot transform
  Make the slot for an AnimationTrack an exposed Attribute on the Track
  Try to set custom background color / icon for custom tracks
  Try to make the default length of new hitbox tracks / weapontrail tracks shorter
*/

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