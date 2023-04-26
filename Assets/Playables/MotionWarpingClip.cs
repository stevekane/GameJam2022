using UnityEngine;
using UnityEngine.Playables;

public class MotionWarpingPlayable : TaskBehavior {
  public RootMotionProvider RootMotionProvider;
  public int Ticks;

  public override void Setup(Playable playable) {
    var controller = (SimpleCharacterController)UserData;
    controller.Total = Ticks;
    controller.Frame = 0;
  }

  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    base.ProcessFrame(playable, info, playerData);
    var controller = (SimpleCharacterController)UserData;
    controller.MotionWarpingActive = RootMotionProvider.Active(controller.gameObject);
    if (controller.MotionWarpingActive) {
      controller.TargetPosition = RootMotionProvider.Position(controller.gameObject);
      controller.TargetRotation = RootMotionProvider.Rotation(controller.gameObject);
    }
  }

  public override void Cleanup(Playable playable) {
    var controller = (SimpleCharacterController)UserData;
    controller.MotionWarpingActive = false;
    controller.Total = Ticks;
    controller.Frame = Ticks;
  }
}

public class MotionWarpingClip : PlayableAsset {
  public RootMotionProvider RootMotionProvider;
  public int Ticks;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<MotionWarpingPlayable>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.RootMotionProvider = RootMotionProvider;
    behavior.Ticks = Ticks;
    return playable;
  }
}