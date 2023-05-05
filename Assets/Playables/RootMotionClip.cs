using UnityEngine;
using UnityEngine.Playables;

public class RootMotionBehavior : TaskBehavior {
  public override void Setup(Playable playable) {
    var controller = (SimpleCharacterController)UserData;
    if (!controller)
      return;
    controller.AllowRootMotion = true;
    controller.AllowRootRotation = true;
  }

  public override void Cleanup(Playable playable) {
    var controller = (SimpleCharacterController)UserData;
    if (!controller)
      return;
    controller.AllowRootMotion = false;
    controller.AllowRootRotation = false;
  }
}

public class RootMotionClip : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<RootMotionBehavior>.Create(graph);
  }
}