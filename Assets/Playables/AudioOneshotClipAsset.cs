using UnityEngine;
using UnityEngine.Playables;

public class AudioOneshotClipBehavior : TaskBehavior {
  public AudioClip Clip;

  public override void Setup(Playable playable) {
    var audioSource = (AudioSource)UserData;
    audioSource.PlayOneShot(Clip);
  }
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