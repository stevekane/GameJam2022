using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class AnimatorGraphClipBehavior : TaskBehavior {
  public AnimationClip Clip;
  public double Speed;

  public override void Setup(Playable playable) {
    var animatorGraph = (AnimatorGraph)UserData;
    animatorGraph.Play(Clip, Speed);
  }

  public override void Cleanup(Playable playable) {
    var animatorGraph = (AnimatorGraph)UserData;
    animatorGraph.Disconnect(Clip);
  }

  #if UNITY_EDITOR
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    base.ProcessFrame(playable, info, playerData);
    if (Application.isPlaying)
      return;
    var animatorGraph = (AnimatorGraph)UserData;
    animatorGraph.Evaluate(playable.GetTime(), playable.GetDuration());
  }
  #endif
}

public class AnimatorGraphClip : PlayableAsset, ITimelineClipAsset {
  public AnimationClip Clip;
  public double Speed = 1;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<AnimatorGraphClipBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.Clip = Clip;
    behavior.Speed = Speed;
    return playable;
  }

  public override double duration => Clip ? Clip.length : base.duration;

  public ClipCaps clipCaps => ClipCaps.SpeedMultiplier | ClipCaps.Blending;
}