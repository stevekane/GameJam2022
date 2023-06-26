using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

namespace CoreAnimation {
  [CreateAssetMenu(menuName = "AnimationGraph/Slot")]
  public class SlotAsset : PlayableAsset {
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
      return ScriptPlayable<SelectBehavior>.Create(graph, 1);
    }
  }

  public class SlotBehavior : PlayableBehaviour {
    const float DEFAULT_SPEED = 4;

    Playable Playable;
    AnimationMixerPlayable Mixer;
    float Speed = DEFAULT_SPEED;
    int Index;

    public float FadeInFraction = .15f;
    public float FadeOutFraction = .85f;
    public float Weight { get; private set; }

    public void CrossFade(Playable playable, float speed = DEFAULT_SPEED) {
      if (Mixer.GetInputCount() > 0) {
        var existing = Mixer.GetInput(0);
        Mixer.DisconnectInput(0);
        Mixer.GetGraph().DestroySubgraph(existing);
        Mixer.SetInputCount(0);
      }
      Mixer.AddInput(playable, 0, 1);
      Index = 0;
      Speed = speed;
    }

    public void Disconnect(Playable playable) {
      var count = Mixer.GetInputCount();
      var lastIndex = count-1;
      for (var i = 0; i < count; i++) {
        var input = Mixer.GetInput(i);
        if (input.Equals(playable)) {
          var lastPlayable = Mixer.GetInput(lastIndex);
          Mixer.DisconnectInput(i);
          Mixer.DisconnectInput(lastIndex);
          Mixer.ConnectInput(i, lastPlayable, 0);
          Mixer.SetInputCount(lastIndex);
          playable.GetGraph().DestroySubgraph(lastPlayable);
          break;
        }
      }
    }

    public override void OnPlayableCreate(Playable playable) {
      Mixer = AnimationMixerPlayable.Create(playable.GetGraph(), 0);
      Playable = playable;
      Playable.ConnectInput(0, Mixer, 0, 1);
    }

    public override void OnPlayableDestroy(Playable playable) {
      playable.GetGraph().DestroySubgraph(Mixer);
    }

    public override void PrepareFrame(Playable playable, FrameData info) {
      {
        var count = Mixer.GetInputCount();
        var lastIndex = Mixer.GetInputCount() - 1;
        for (var i = 0; i < count; i++) {
          var input = Mixer.GetInput(i);
          if (input.IsDone()) {
            Mixer.DisconnectInput(i);
            Mixer.SetInputCount(0);
            Mixer.GetGraph().DestroySubgraph(input);
          }
        }
      }
      {
        var count = Mixer.GetInputCount();
        if (count == 0)
          Weight = 0;
        for (var i = 0; i < count; i++) {
          var input = Mixer.GetInput(i);
          var normalizedTime = (float)(input.GetTime() / input.GetDuration());
          var weight = 0f;
          if (normalizedTime <= FadeInFraction) {
            weight = Mathf.InverseLerp(0, FadeInFraction, normalizedTime);
          } else if (normalizedTime >= FadeOutFraction) {
            weight = 1-Mathf.InverseLerp(FadeOutFraction, 1, normalizedTime);
          } else {
            weight = 1;
          }
          Weight = weight;
        }
      }
    }
  }
}