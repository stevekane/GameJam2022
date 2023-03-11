using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class AnimatorGraphTrackBehavior : TaskBehavior {
  /*
  TODO:

  I don't think the mixer should be doing this work. I think it'd make more sense
  to move this logic to the clips and make use of Setup/Cleanup to ensure that
  any running clip is appropriately disconnected from the AnimatorGraph.

  1. Use OnPlayableDestroy to see if we can disconnect correctly.
  2. Use TaskBehavior to use Cleanup to disconnect
  3. Move this logic to the clips?
  */
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    base.ProcessFrame(playable, info, playerData);
    var animatorGraph = playerData as AnimatorGraph;
    var inputCount = playable.GetInputCount();
    var activeClip = Playable.Null;
    var weight = 0f;
    for (var i = 0; i < inputCount; i++) {
      weight = playable.GetInputWeight(i);
      if (weight > 0) {
        activeClip = playable.GetInput(i);
        break;
      }
    }
    if (!activeClip.IsNull()) {
      var clipPlayable = (ScriptPlayable<AnimatorGraphClipBehavior>)activeClip;
      var clipBehavior = clipPlayable.GetBehaviour();
      var time = clipPlayable.GetTime();
      var duration = clipPlayable.GetDuration();
      var clipTime = clipPlayable.GetTime();
      animatorGraph.Evaluate(clipBehavior.Clip, clipTime, duration, weight);
    } else {
      animatorGraph.Disconnect();
    }
  }

  public override void Cleanup(Playable playable) {
    var animatorGraph = (AnimatorGraph)UserData;
    animatorGraph.Disconnect();
  }
}

[TrackBindingType(typeof(AnimatorGraph))]
[TrackClipType(typeof(AnimatorGraphClip))]
public class AnimatorGraphTrack : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<AnimatorGraphTrackBehavior>.Create(graph, inputCount);
  }
}