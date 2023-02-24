using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[TrackClipType(typeof(AbilityClipAsset))]
[TrackBindingType(typeof(Ability))]
public class AbilityTrackAsset : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<AbilityTrackMixer>.Create(graph, inputCount);
  }
}