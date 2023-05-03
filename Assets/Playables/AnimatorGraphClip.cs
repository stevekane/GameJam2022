using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AnimatorGraphClipBehavior : TaskBehavior {
  public AnimationClip Clip;

  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    base.ProcessFrame(playable, info, playerData);
    var animatorGraph = playerData as AnimatorGraph;
    // TODO: Unsure about this weight.... will the mixer somehow handle weight?... we need to know
    // how much weight we are being played with to pass that along to AnimatorGraph
    animatorGraph.Evaluate(Clip, playable.GetTime(), playable.GetDuration(), 1);
  }
}

public class AnimatorGraphClip : PlayableAsset, ITimelineClipAsset {
  public AnimationClip Clip;
  public float Speed = 1;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<AnimatorGraphClipBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.Clip = Clip;
    return playable;
  }

  public override double duration => Clip ? Clip.length : base.duration;

  public ClipCaps clipCaps => ClipCaps.SpeedMultiplier | ClipCaps.Blending;
}