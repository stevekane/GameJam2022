using UnityEngine;
using UnityEngine.Playables;

public class SendMessageBehavior : TaskBehavior {
  public string Message;
  public SendMessageOptions SendMessageOptions;

  public override void Setup(Playable playable) {
    var go = (GameObject)UserData;
    if (!Application.isPlaying)
      return;
    if (!go)
      return;
    go.SendMessage(Message, SendMessageOptions);
  }
}

public class SendMessageClip : PlayableAsset {
  public string Message;
  public SendMessageOptions SendMessageOptions;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<SendMessageBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.Message = Message;
    behavior.SendMessageOptions = SendMessageOptions;
    return playable;
  }
}