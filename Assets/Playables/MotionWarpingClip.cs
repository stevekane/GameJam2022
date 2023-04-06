using UnityEngine;
using UnityEngine.Playables;

public class MotionWarpingPlayable : TaskBehavior {
  public TransformProvider TransformProvider;
  public RotationMatch RotationMatch;
  public Vector3 TargetOffset;
  public float TargetDistance;
  public int Ticks;

  public override void Setup(Playable playable) {
    var motionWarping = (MotionWarping)UserData;
    motionWarping.Target = TransformProvider.Evaluate(motionWarping.gameObject);
    motionWarping.RotationMatch = RotationMatch;
    motionWarping.TargetOffset = TargetOffset;
    motionWarping.TargetDistance = TargetDistance;
    motionWarping.Total = Ticks;
    motionWarping.Frame = 0;
  }

  public override void Cleanup(Playable playable) {
    var motionWarping = (MotionWarping)UserData;
    motionWarping.Target = null;
    motionWarping.TargetOffset = Vector3.zero;
    motionWarping.TargetDistance = 0;
    motionWarping.Total = 0;
    motionWarping.Frame = 0;
  }
}

public class MotionWarpingClip : PlayableAsset {
  public TransformProvider TransformProvider;
  public RotationMatch RotationMatch;
  public Vector3 TargetOffset;
  public float TargetDistance;
  public int Ticks;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<MotionWarpingPlayable>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.TransformProvider = TransformProvider;
    behavior.RotationMatch = RotationMatch;
    behavior.TargetOffset = TargetOffset;
    behavior.TargetDistance = TargetDistance;
    behavior.Ticks = Ticks;
    return playable;
  }
}
