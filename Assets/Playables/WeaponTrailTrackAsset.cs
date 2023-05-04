using UnityEngine.Timeline;
using UnityEngine.Playables;

[TrackClipType(typeof(WeaponTrailClipAsset))]
[TrackBindingType(typeof(WeaponTrail))]
public class WeaponTrailTrackAsset : TrackAsset {
  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    var weaponTrail = (WeaponTrail)director.GetGenericBinding(this);
    driver.AddFromName<WeaponTrail>(weaponTrail.gameObject, "Emitting");
    base.GatherProperties(director, driver);
  }
}