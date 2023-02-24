using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class AbilityClipAsset : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<AbilityTrackBehavior>.Create(graph);
  }
}