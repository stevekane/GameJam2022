using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackBindingType(typeof(SimpleCharacterController))]
[TrackClipType(typeof(RootMotionClip))]
[TrackClipType(typeof(MotionWarpingClip))]
public class SimpleCharacterControllerTrack : TrackAsset {
  public override void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
    var controller = (SimpleCharacterController)director.GetGenericBinding(this);
    if (!controller)
      return;
    driver.AddFromComponent(controller.gameObject, controller.transform);
    driver.AddFromComponent(controller.gameObject, controller.GetComponent<SimpleCharacterController>());
  }
}