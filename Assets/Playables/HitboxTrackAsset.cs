using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackClipType(typeof(HitboxClipAsset))]
[TrackBindingType(typeof(Hitbox))]
public class HitboxTrackAsset : TrackAsset {
  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    var hitbox = (Hitbox)director.GetGenericBinding(this);
    var collider = hitbox.Collider;
    driver.AddFromName<Hitbox>(hitbox.gameObject, "HitboxParams");
    driver.AddFromComponent(collider.gameObject, collider);
  }
}