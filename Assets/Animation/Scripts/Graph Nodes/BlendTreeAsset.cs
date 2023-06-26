using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[CreateAssetMenu(menuName = "AnimationGraph/BlendTree")]
public class BlendTreeAsset : PlayableAsset {
  public BlendTreeNodeSpec[] Nodes;
  public AnimationCurve BlendCurve = AnimationCurve.Linear(0,0,1,1);

  public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
    var playable = ScriptPlayable<BlendTreeBehaviour>.Create(graph, 1);
    var blendTree = playable.GetBehaviour();
    blendTree.BlendCurve = BlendCurve;
    foreach (var node in Nodes)
      blendTree.Add(node.Asset.CreatePlayable(graph, owner), node.Value);
    return playable;
  }
}

[Serializable]
public struct BlendTreeNodeSpec {
  public PlayableAsset Asset;
  public float Value;
}

public struct BlendTreeNode : IComparable<BlendTreeNode> {
  public Playable Playable;
  public float Value;
  public int CompareTo(BlendTreeNode b) => Value.CompareTo(b.Value);
}

public class BlendTreeBehaviour : PlayableBehaviour {
  const float DEFAULT_SPEED = 40;

  Playable Playable;
  AnimationMixerPlayable Mixer;
  List<float> Values = new();
  float TargetValue = 0;
  float Value = 0;
  float Speed = DEFAULT_SPEED;

  public AnimationCurve BlendCurve = AnimationCurve.Linear(0,0,1,1);

  public void Set(float value, float speed = DEFAULT_SPEED) {
    TargetValue = value;
    Speed = speed;
  }

  public void Add(Playable playable, float value) {
    Values.Add(value);
    Mixer.AddInput(playable, 0, 0);
  }

  public override void OnPlayableCreate(Playable playable) {
    Mixer = AnimationMixerPlayable.Create(playable.GetGraph(), 0);
    Playable = playable;
    Playable.DisconnectInput(0);
    Playable.ConnectInput(0, Mixer, 0, 1);
  }

  public override void OnPlayableDestroy(Playable playable) {
    playable.GetGraph().DestroySubgraph(Mixer);
  }

  public override void PrepareFrame(Playable playable, FrameData info) {
    Value = Mathf.MoveTowards(Value, TargetValue, info.deltaTime * Speed);
    var count = Mixer.GetInputCount();
    for (var i = 0; i < count; i++) {
      var weight = Weight(i, Value, Values);
      Mixer.SetInputWeight(i, Weight(i, Value, Values));
    }
  }

  float Weight(int index, float value, List<float> values) {
    var minIndex = 0;
    var maxIndex = values.Count-1;
    if (index < minIndex || index > maxIndex) {
      return 0;
    } else if (maxIndex == 0) {
      return 1;
    } else if (index == minIndex && value < values[minIndex]) {
      return 1;
    } else if (index == maxIndex && value > values[maxIndex]) {
      return 1;
    } else if (index > minIndex && value >= values[index-1]&& value <= values[index]) {
      return BlendCurve.Evaluate(Mathf.InverseLerp(values[index-1], values[index], value));
    } else if (index < maxIndex && value >= values[index] && value <= values[index+1]) {
      return 1-BlendCurve.Evaluate(Mathf.InverseLerp(values[index], values[index+1], value));
    } else {
      return 0;
    }
  }
}
