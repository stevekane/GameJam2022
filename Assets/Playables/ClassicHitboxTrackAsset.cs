using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(ClassicHitboxClipAsset))]
[TrackBindingType(typeof(TriggerEvent))]
public class ClassicHitboxTrackAsset : TrackAsset {
  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    var hitbox = (TriggerEvent)director.GetGenericBinding(this);
    if (!hitbox)
      return;
    var collider = hitbox.GetComponent<Collider>();
    if (!collider) {
      Debug.LogError($"No collider found on hitbox", hitbox);
      return;
    }
    //driver.AddFromName<Hitbox>(hitbox.gameObject, "HitboxParams");
    driver.AddFromComponent(collider.gameObject, collider);
  }
}