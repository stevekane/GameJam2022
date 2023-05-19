using UnityEngine;
using UnityEngine.Playables;

public class TagsClipBehavior : TaskBehavior {
  public AbilityTag AddedTags;

  public override void Setup(Playable playable) {
    var tags = (Tags)UserData;
    tags.Current.AddFlags(AddedTags);
  }

  public override void Cleanup(Playable playable) {
    var tags = (Tags)UserData;
    tags.Current.ClearFlags(AddedTags);
  }
}

public class TagsClip : PlayableAsset {
  public AbilityTag AddedTags;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<TagsClipBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.AddedTags = AddedTags;
    return playable;
  }
}