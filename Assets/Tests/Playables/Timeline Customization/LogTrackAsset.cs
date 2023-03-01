using UnityEngine;
using UnityEngine.Playables;

public class LogTrackAsset : PlayableAsset {
  public string Message;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<LogTrackBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.Message = Message;
    return playable;
  }
}