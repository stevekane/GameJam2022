using UnityEngine;
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
    // Transform for root motion
    driver.AddFromComponent(controller.gameObject, controller.transform);
    driver.PushActiveGameObject(controller.gameObject);
    // Character behavior stuff
    driver.AddFromName<SimpleCharacterController>("AllowRootMotion");
    driver.AddFromName<SimpleCharacterController>("AllowRootRotation");
    driver.AddFromName<SimpleCharacterController>("AllowWarping");
    driver.AddFromName<SimpleCharacterController>("AllowMoving");
    driver.AddFromName<SimpleCharacterController>("AllowRotating");
    driver.AddFromName<SimpleCharacterController>("AllowExternalForces");
    driver.AddFromName<SimpleCharacterController>("AllowPhysics");
    // Motion-warping stuff
    driver.AddFromName<SimpleCharacterController>("MotionWarpingActive");
    driver.AddFromName<SimpleCharacterController>("Total");
    driver.AddFromName<SimpleCharacterController>("Frame");
    driver.AddFromName<SimpleCharacterController>("TargetPosition.x");
    driver.AddFromName<SimpleCharacterController>("TargetPosition.y");
    driver.AddFromName<SimpleCharacterController>("TargetPosition.z");
    driver.AddFromName<SimpleCharacterController>("TargetRotation.x");
    driver.AddFromName<SimpleCharacterController>("TargetRotation.y");
    driver.AddFromName<SimpleCharacterController>("TargetRotation.z");
    driver.AddFromName<SimpleCharacterController>("TargetRotation.w");
    driver.PopActiveGameObject();
  }
}