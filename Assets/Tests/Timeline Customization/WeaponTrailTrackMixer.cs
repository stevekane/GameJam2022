using UnityEngine.Playables;

public class WeaponTrailTrackMixer : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var weaponTrail = (WeaponTrail)playerData;
    if (!weaponTrail)
      return;
    var inputCount = playable.GetInputCount();
    var active = false;
    for (var i = 0; i < inputCount; i++) {
      active = active || playable.GetInputWeight(i) > 0;
    }
    weaponTrail.Emitting = active;
  }
}