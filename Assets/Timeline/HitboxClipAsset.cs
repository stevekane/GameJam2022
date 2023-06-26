using UnityEngine;
using UnityEngine.Playables;

public class HitboxClipBehavior : TaskBehavior {
  public HitboxParams HitboxParams;

  public override void Setup(Playable playable) {
    var hitbox = (Hitbox)UserData;
    if (!hitbox)
      return;
    hitbox.HitboxParams = HitboxParams;
    hitbox.Collider.enabled = true;
    if (Application.isPlaying)
      hitbox.Targets.Clear();
  }

  public override void Cleanup(Playable playable) {
    var hitbox = (Hitbox)UserData;
    if (!hitbox)
      return;
    hitbox.Collider.enabled = false;
    if (Application.isPlaying)
      hitbox.Targets.Clear();
  }
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