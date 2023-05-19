using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AnimationGraph/Select")]
public class SelectAsset : PlayableAsset {
  [SerializeField] PlayableAsset[] Cases;

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<SelectBehavior>.Create(graph, 1);
    var select = playable.GetBehaviour();
    foreach (var c in Cases) {
      select.Add(c.CreatePlayable(graph, owner));
    }
    return playable;
  }
}

public class SelectBehavior : PlayableBehaviour {
  public float Speed = .1f;
  public int Index;

  Playable Playable;
  AnimationMixerPlayable Mixer;

  public void CrossFade(int index, float speed = .1f) {
    if (Index != index) {
      var input = Mixer.GetInput(index);
      input.SetTime(0);
      input.Play();
    }
    Index = index;
    Speed = speed;
  }

  public void Add(Playable playable) {
    Mixer.AddInput(playable, sourceOutputIndex: 0, weight: 0);
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
    var count = Mixer.GetInputCount();
    for (var i = 0; i < count; i++) {
      var target = i == Index ? 1 : 0;
      var weight = Mixer.GetInputWeight(i);
      var input = Mixer.GetInput(i);
      weight = Mathf.MoveTowards(weight, target, Speed);
      Mixer.SetInputWeight(i, weight);
      if (weight == 0) {
        input.Pause();
        input.SetTime(0);
      }
    }
  }
}