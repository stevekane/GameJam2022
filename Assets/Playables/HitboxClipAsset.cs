using UnityEngine;
using UnityEngine.Playables;

public class HitboxClipBehavior : TaskBehavior {
  public HitboxParams HitboxParams;

  public override void Setup(Playable playable) {
    var hitbox = (Hitbox)UserData;
    hitbox.Targets.Clear();
  }

  public override void Cleanup(Playable playable) {
    var hitbox = (Hitbox)UserData;
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