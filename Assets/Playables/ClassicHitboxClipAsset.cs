using UnityEngine;
using UnityEngine.Playables;

public class ClassicHitboxClipBehavior : TaskBehavior {
  public override void Setup(Playable playable) {
    var hitbox = (TriggerEvent)UserData;
    if (!hitbox)
      return;
    hitbox.EnableCollision = true;
    //if (Application.isPlaying)
    //  hitbox.Targets.Clear();
  }

  public override void Cleanup(Playable playable) {
    var hitbox = (TriggerEvent)UserData;
    if (!hitbox)
      return;
    hitbox.EnableCollision = false;
    //if (Application.isPlaying)
    //  hitbox.Targets.Clear();
  }
}

public class ClassicHitboxClipAsset : PlayableAsset {
  //public HitboxParams HitboxParams;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<ClassicHitboxClipBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    return playable;
  }
}