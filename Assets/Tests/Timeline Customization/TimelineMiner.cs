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
  Create and test movement track which is sort of like root motion except driven through code
  Make the slot for an AnimationTrack an exposed Attribute on the Track
  Try to set custom background color / icon for custom tracks
  Try to make the default length of new hitbox tracks / weapontrail tracks shorter

  TESTS
    + Confirm that a single node connected to multiple outputs processes two unique playerDatas per frame.
*/

/*
What about three fundamental graphs:

  Animation (GameTime)
  Audio (DSP)
  Logic (Manual in FixedUpdate)

Each graph may support playing various subgraphs by type:

  Animation supports anything that targets an Animator
  Audio supports anything targeting an AudioSource
  Logic supports anything targeting a UnityObject
*/

/*
Thinking hard about how this would work.

  Animator behavior can be dictated solely by interacting with a runtimeAnimatorController which in turn drives
  the behavior of the graph generated from that controller.

  This is sort of confusing because Animator itself is somehow the "target" of some eventual AnimationStream.
  This means that if you want your own means of driving an animator you must give that thing a name.
  For example, you could have an AnimationGraph which can play Animations directly but also can play tracks
  from a pure animation timeline.

  These animation timelines can contain tracks that target different slots in the graph which the AnimationGraph
  will use to determine where to play a given AnimationTrack.

  You tell the AnimationGraph what TimelineAsset to play and it will strip that asset for suitable tracks
  which it then uses to construst a TimelinePlayable which can be connected to the AnimationGraph.
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

  public (ScriptPlayable<TimelinePlayable>, ScriptPlayable<TimelinePlayable>) Play(TimelineAsset timelineAsset, float fadeInDuration) {
    // TODO: Fade in
    var fixedTracks = timelineAsset.Tracks(type => type != typeof(AudioSource));
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
    var audioTracks = timelineAsset.Tracks(type => type == typeof(AudioSource));
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