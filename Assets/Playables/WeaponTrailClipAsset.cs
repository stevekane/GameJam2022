using UnityEngine;
using UnityEngine.Playables;

public class WeaponTrailTrackClip : TaskBehavior {
  public override void Setup(Playable playable) {
    var weaponTrail = (WeaponTrail)UserData;
    if (!weaponTrail)
      return;
    weaponTrail.Emitting = true;
  }

  public override void Cleanup(Playable playable) {
    var weaponTrail = (WeaponTrail)UserData;
    if (!weaponTrail)
      return;
    weaponTrail.Emitting = false;
  }
}

public class WeaponTrailClipAsset : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<WeaponTrailTrackClip>.Create(graph);
  }
}