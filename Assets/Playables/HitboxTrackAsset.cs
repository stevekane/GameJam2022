using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class HitBoxTrackMixer : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var hitbox = (Hitbox)playerData;
    if (!hitbox)
      return;
    var inputCount = playable.GetInputCount();
    hitbox.Collider.enabled = false;
    for (var i = 0; i < inputCount; i++) {
      if (playable.GetInputWeight(i) > 0) {
        var clipPlayable = (ScriptPlayable<HitboxClipBehavior>)playable.GetInput(i);
        var behavior = clipPlayable.GetBehaviour();
        hitbox.Collider.enabled = true;
        hitbox.HitboxParams = behavior.HitboxParams;
      }
    }
  }
}

[TrackClipType(typeof(HitboxClipAsset))]
[TrackBindingType(typeof(Hitbox))]
public class HitboxTrackAsset : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<HitBoxTrackMixer>.Create(graph, inputCount);
  }
}