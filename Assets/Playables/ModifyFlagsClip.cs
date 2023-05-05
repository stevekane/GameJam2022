using UnityEngine;
using UnityEngine.Playables;

public class ModifyFlagsBehavior : TaskBehavior {
  public AbilityTag Add;

  public override void Setup(Playable playable) {
    var ability = (SimpleAbility)UserData;
    if (!ability)
      return;
    ability.Tags.AddFlags(Add);
  }

  public override void Cleanup(Playable playable) {
    var ability = (SimpleAbility)UserData;
    if (!ability)
      return;
    ability.Tags.ClearFlags(Add);
  }
}

public class ModifyFlagsClip : PlayableAsset {
  public AbilityTag Add;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<ModifyFlagsBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.Add = Add;
    return playable;
  }
}