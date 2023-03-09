using UnityEngine;
using UnityEngine.Playables;

public class HitboxClipBehavior : PlayableBehaviour {
  public HitboxParams HitboxParams;
}

public class HitboxClipAsset : PlayableAsset {
  public HitboxParams HitboxParams;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<HitboxClipBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.HitboxParams = HitboxParams;
    return playable;
  }
}