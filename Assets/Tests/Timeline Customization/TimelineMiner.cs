using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;

/*
Today's goals:

  Play(Timeline, Duration)
  Stop(Timeline, Duration)
  Pause(Timeline) -- built into playables API
  Resume(Timeline) -- built into playables API
  Setup system that connects timelines to a graph for various statically-known outputs
  Create three separate timelines for the phases of the test attack
  Play them in sequence with small blending transitions and evaluate the result
  Create and test OnPlayableCreate/OnPlayableDestroy hooks for custom TrackMixers like Hitbox that do setup/teardown
  Create and test custom AnimationTrack with own mixer that does not include built-in tracks AbsoluteToRoot transform
  Make the slot for an AnimationTrack an exposed Attribute on the Track
  Try to set custom background color / icon for custom tracks
  Try to make the default length of new hitbox tracks / weapontrail tracks shorter
*/

public class TimelineAudioMixer : PlayableBehaviour {
  Playable Playable;
  PlayableGraph Graph;
  AudioMixerPlayable AudioMixerPlayable;
  int port;

  public AudioMixerPlayable AudioMixer => AudioMixerPlayable;

  public int AddInput(Playable sourcePlayable, int sourcePort, float weight) {
    var connectedPort = port;
    var noop = ScriptPlayable<NoopBehavior>.Create(Graph);
    noop.AddInput(sourcePlayable, sourcePort, weight);
    AudioMixer.AddInput(noop, 0, 1);
    port++;
    return connectedPort;
  }

  public override void OnPlayableCreate(Playable playable) {
    Playable = playable;
    Graph = playable.GetGraph();
    AudioMixerPlayable = AudioMixerPlayable.Create(Graph);
  }

  public override void OnPlayableDestroy(Playable playable) {
    AudioMixerPlayable.Destroy();
  }
}

public class TimelineMiner : MonoBehaviour {
  [SerializeField] TimelineAsset TimelineAsset;
  [SerializeField] Animator Animator;
  [SerializeField] AudioSource AudioSource;
  [SerializeField] Collider Hitbox;
  [SerializeField] WeaponTrail WeaponTrail;

  PlayableGraph FixedGraph;
  AnimationMixerPlayable AnimationMixer;
  AnimationPlayableOutput AnimationOutput;
  ScriptPlayable<HitboxOutputMixer> HitboxMixer;
  ScriptPlayableOutput HitboxOutput;
  ScriptPlayable<WeaponTrailOutputMixer> WeaponTrailMixer;
  ScriptPlayableOutput WeaponTrailOutput;

  PlayableGraph AudioGraph;
  ScriptPlayable<TimelineAudioMixer> AudioMixer;
  AudioPlayableOutput AudioOutput;

  ScriptPlayable<TimelinePlayable> CurrentFixedTimeline;
  ScriptPlayable<TimelinePlayable> CurrentAudioTimeline;

  void Start() {
    // Fixed update graph
    FixedGraph = PlayableGraph.Create("Fixed Graph");
    FixedGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    FixedGraph.Play();
    AnimationMixer = AnimationMixerPlayable.Create(FixedGraph);
    AnimationOutput = AnimationPlayableOutput.Create(FixedGraph, "Animator", Animator);
    AnimationOutput.SetSourcePlayable(AnimationMixer);
    HitboxMixer = ScriptPlayable<HitboxOutputMixer>.Create(FixedGraph);
    HitboxOutput = ScriptPlayableOutput.Create(FixedGraph, "Hitbox");
    HitboxOutput.SetUserData(Hitbox);
    HitboxOutput.SetSourcePlayable(HitboxMixer);
    WeaponTrailMixer = ScriptPlayable<WeaponTrailOutputMixer>.Create(FixedGraph);
    WeaponTrailOutput = ScriptPlayableOutput.Create(FixedGraph, "WeaponTrail");
    WeaponTrailOutput.SetUserData(WeaponTrail);
    WeaponTrailOutput.SetSourcePlayable(WeaponTrailMixer);

    // Audio graph
    AudioGraph = PlayableGraph.Create("Audio Graph");
    AudioGraph.SetTimeUpdateMode(DirectorUpdateMode.DSPClock);
    AudioGraph.Play();
    AudioMixer = ScriptPlayable<TimelineAudioMixer>.Create(AudioGraph);
    AudioOutput = AudioPlayableOutput.Create(AudioGraph, "Audio", AudioSource);
    AudioOutput.SetSourcePlayable(AudioMixer.GetBehaviour().AudioMixer);

    // Current Timeline
    CurrentFixedTimeline = ScriptPlayable<TimelinePlayable>.Null;
  }

  void OnDestroy() {
    FixedGraph.Destroy();
  }

  void Update() {
    if (Input.anyKey && !CurrentFixedTimeline.IsNull()) {
      Stop((CurrentFixedTimeline, CurrentAudioTimeline), 0);
    }
  }

  void FixedUpdate() {
    FixedGraph.Evaluate(Time.fixedDeltaTime);
    if (CurrentFixedTimeline.IsNull()) {
      (CurrentFixedTimeline, CurrentAudioTimeline) = Play(TimelineAsset, 0);
    } else if (!CurrentFixedTimeline.IsValid()) {
      Stop((CurrentFixedTimeline, CurrentAudioTimeline), 0);
      (CurrentFixedTimeline, CurrentAudioTimeline) = Play(TimelineAsset, 0);
    } else if (CurrentFixedTimeline.IsDone()) {
      Stop((CurrentFixedTimeline, CurrentAudioTimeline), 0);
      (CurrentFixedTimeline, CurrentAudioTimeline) = Play(TimelineAsset, 0);
    }
  }

  IEnumerable<TrackAsset> Tracks(TimelineAsset timelineAsset, Predicate<Type> predicate) {
    foreach (var track in timelineAsset.GetOutputTracks()) {
      foreach (var output in track.outputs) {
        if (predicate(output.outputTargetType)) {
          yield return track;
        }
      }
    }
  }

  public (ScriptPlayable<TimelinePlayable>, ScriptPlayable<TimelinePlayable>) Play(TimelineAsset timelineAsset, float fadeInDuration) {
    // TODO: Fade in
    var fixedTracks = Tracks(timelineAsset, type => type != typeof(AudioSource));
    var fixedTimeline = TimelinePlayable.Create(FixedGraph, fixedTracks, gameObject, false, false);
    fixedTimeline.SetTime(0);
    fixedTimeline.SetDuration(timelineAsset.duration);
    fixedTimeline.SetOutputCount(fixedTimeline.GetInputCount());
    foreach (var (track, port) in fixedTracks.WithIndex()) {
      foreach (var output in track.outputs) {
        var type = output.outputTargetType;
        if (type == typeof(Animator)) {
          AnimationMixer.AddInput(fixedTimeline, port, 1);
        } else if (type == typeof(Collider)) {
          HitboxMixer.AddInput(fixedTimeline, port, 1);
        } else if (type == typeof(WeaponTrail)) {
          WeaponTrailMixer.AddInput(fixedTimeline, port, 1);
        }
      }
    }
    var audioTracks = Tracks(timelineAsset, type => type == typeof(AudioSource));
    var audioTimeline = TimelinePlayable.Create(AudioGraph, audioTracks, gameObject, false, false);
    audioTimeline.SetTime(0);
    audioTimeline.SetDuration(timelineAsset.duration);
    audioTimeline.SetOutputCount(audioTimeline.GetInputCount());
    foreach (var (track, port) in audioTracks.WithIndex()) {
      foreach (var output in track.outputs) {
        AudioMixer.GetBehaviour().AddInput(audioTimeline, port, 1);
      }
    }
    return (fixedTimeline, audioTimeline);
  }

  public void Stop((ScriptPlayable<TimelinePlayable>, ScriptPlayable<TimelinePlayable>) timelines, float fadeOutDuration) {
    // TODO: Fade out
    FixedGraph.DestroySubgraph(timelines.Item1);
    AudioGraph.DestroySubgraph(timelines.Item2);
    CurrentFixedTimeline = ScriptPlayable<TimelinePlayable>.Null;
    CurrentAudioTimeline = ScriptPlayable<TimelinePlayable>.Null;
  }
}