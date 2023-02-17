using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[TrackClipType(typeof(HitboxClipAsset))]
[TrackBindingType(typeof(Collider))]
public class HitboxTrackAsset : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<HitBoxTrackMixer>.Create(graph, inputCount);
  }
}