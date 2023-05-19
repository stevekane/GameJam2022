using UnityEngine;
using UnityEngine.Playables;

public class AbilityTrackBehavior : PlayableBehaviour { }

public class AbilityClipAsset : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<AbilityTrackBehavior>.Create(graph);
  }
}