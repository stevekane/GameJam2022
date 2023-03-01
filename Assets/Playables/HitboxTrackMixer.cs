using UnityEngine;
using UnityEngine.Playables;

public class HitBoxTrackMixer : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var hitbox = (Collider)playerData;
    if (!hitbox)
      return;
    var inputCount = playable.GetInputCount();
    var active = false;
    for (var i = 0; i < inputCount; i++) {
      active = active || playable.GetInputWeight(i) > 0;
    }
    hitbox.enabled = active;
  }
}