using UnityEngine;
using UnityEngine.Playables;

public class AudioOneshotClipBehavior : PlayableBehaviour {
  public AudioClip Clip;
}

public class AudioOneshotClipAsset : PlayableAsset {
  public AudioClip Clip;
  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<AudioOneshotClipBehavior>.Create(graph);
    var behavior = playable.GetBehaviour();
    behavior.Clip = Clip;
    return playable;
  }
}