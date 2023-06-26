using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(SimpleAbility))]
[TrackClipType(typeof(ModifyFlagsClip))]
public class SimpleAbilityTrack : TrackAsset {
  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    var ability = (SimpleAbility)director.GetGenericBinding(this);
    if (!ability)
      return;
    driver.PushActiveGameObject(ability.gameObject);
    driver.AddFromName<SimpleAbility>("Tags");
    driver.PopActiveGameObject();
  }
}