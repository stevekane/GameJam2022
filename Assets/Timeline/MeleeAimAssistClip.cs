using UnityEngine;
using UnityEngine.Playables;

public class MeleeAimAssistBehavior : TaskBehavior {
  public int TotalTicks;
  public float IdealDistance;
  public override void Setup(Playable playable) {
    var aimAssist = (MeleeAimAssist)UserData;
    aimAssist.IdealDistance = IdealDistance;
    aimAssist.Ticks = 0;
    aimAssist.TotalTicks = TotalTicks;
  }

  public override void Cleanup(Playable playable) {
    var aimAssist = (MeleeAimAssist)UserData;
    aimAssist.Ticks = 0;
    aimAssist.TotalTicks = 0;
  }
}

public class MeleeAimAssistClip : PlayableAsset {
  public float IdealDistance = 1;
  public int TotalTicks = 1;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<MeleeAimAssistBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.TotalTicks = TotalTicks;
    behavior.IdealDistance = IdealDistance;
    return playable;
  }
}