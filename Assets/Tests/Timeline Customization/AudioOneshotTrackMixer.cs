using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Playables;

public class AudioOneshotTrackMixer : PlayableBehaviour {
  bool[] InputsHavePlayed;
  public override void OnBehaviourPlay(Playable playable, FrameData info) {
    base.OnBehaviourPlay(playable, info);
    // TODO: This doesn't work great in preview mode because this is only called at start.
    InputsHavePlayed = new bool[playable.GetInputCount()];
  }
  public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
    var audioSource = (AudioSource)playerData;
    if (!audioSource)
      return;
    var inputCount = playable.GetInputCount();
    for (var i = 0; i < inputCount; i++) {
      if (playable.GetInputWeight(i) > 0 && !InputsHavePlayed[i]) {
        var audio = (AudioClipPlayable)playable.GetInput(i);
        SFXManager.Instance.TryPlayOneShot(audio.GetClip());
        InputsHavePlayed[i] = true;
      }
    }
  }
}