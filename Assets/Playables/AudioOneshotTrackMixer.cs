using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

public class AudioOneshotTrackMixer : PlayableBehaviour {
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var audioSource = (AudioSource)playerData;
    if (!audioSource)
      return;
    var inputCount = playable.GetInputCount();
    for (var i = 0; i < inputCount; i++) {
      if (playable.GetInputWeight(i) > 0) {
        var clipPlayable = (ScriptPlayable<AudioOneshotClipBehavior>)playable.GetInput(i);
        var behavior = clipPlayable.GetBehaviour();
        audioSource.PlayOneShot(behavior.Clip);
      }
    }
  }
}