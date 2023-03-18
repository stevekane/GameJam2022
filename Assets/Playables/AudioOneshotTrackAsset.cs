using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class AudioOneshotTrackMixer : TaskBehavior {}

[TrackBindingType(typeof(AudioSource))]
[TrackClipType(typeof(AudioOneshotClipAsset))]
public class AudioOneshotTrackAsset : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<AudioOneshotTrackMixer>.Create(graph, inputCount);
  }
}