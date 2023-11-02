using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(HitboxClipAsset))]
[TrackBindingType(typeof(HitboxSteve))]
public class HitboxTrackAsset : TrackAsset {
  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    var hitbox = (HitboxSteve)director.GetGenericBinding(this);
    if (!hitbox)
      return;
    var collider = hitbox.Collider;
    if (!collider) {
      Debug.LogError($"No collider found on hitbox", hitbox);
      return;
    }
    driver.AddFromName<HitboxSteve>(hitbox.gameObject, "HitboxParams");
    driver.AddFromComponent(collider.gameObject, collider);
  }
}