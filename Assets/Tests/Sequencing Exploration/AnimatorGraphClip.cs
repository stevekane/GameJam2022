using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AnimatorGraphClipBehavior : PlayableBehaviour {
  public AnimationClip Clip;
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

  public ClipCaps clipCaps => ClipCaps.SpeedMultiplier| ClipCaps.Blending;
}