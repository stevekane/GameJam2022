using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[TrackClipType(typeof(WeaponTrailClipAsset))]
[TrackBindingType(typeof(WeaponTrail))]
public class WeaponTrailTrackAsset : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<WeaponTrailTrackMixer>.Create(graph, inputCount);
  }
}