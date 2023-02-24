using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;

[TrackClipType(typeof(AbilityClipAsset))]
[TrackBindingType(typeof(Ability))]
public class AbilityTrackAsset : TrackAsset {
  public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
    return ScriptPlayable<AbilityTrackMixer>.Create(graph, inputCount);
  }
}

public class AbilityTrackMixer : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var ability = playerData as Ability;
    if (!ability)
      return;
    var inputCount = playable.GetInputCount();
    var active = false;
    for (var i = 0; i < inputCount; i++)
      active = active || playable.GetInputWeight(i) > 0;
    if (active)
      ability.SetCancellable();
  }
}

public class AbilityClipAsset : PlayableAsset {
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    return ScriptPlayable<AbilityTrackBehavior>.Create(graph);
  }
}

public class AbilityTrackBehavior : PlayableBehaviour { }