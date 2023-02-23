using UnityEngine;
using UnityEngine.Playables;

public class HitboxClipAsset : PlayableAsset {
  // TODO: This is a test to try to better understand what exposed references
  // and the graph resolver are
  public ExposedReference<GameObject> Owner;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var boundOwner = Owner.Resolve(graph.GetResolver());
    return ScriptPlayable<HitBoxTrackBehavior>.Create(graph);
  }
}