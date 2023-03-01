using UnityEngine;
using UnityEngine.Playables;

public class HitboxClipAsset : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<HitBoxTrackBehavior>.Create(graph);
  }
}