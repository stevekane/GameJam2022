using UnityEngine;
using UnityEngine.Playables;

public class WeaponTrailClipAsset : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<WeaponTrailTrackClip>.Create(graph);
  }
}