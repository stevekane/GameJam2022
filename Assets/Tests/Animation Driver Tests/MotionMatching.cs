using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/*
Construct playable graph.

+ Loop the running animation.
+ Create mixer out of the looping running cycles.
Add an attack animation.
Read data from the attack animation stream.
Mix attack animation with the looping running.
Use data from attack animation stream to blend between running cycles.
Blend result of blended running cycles with attack.
*/
[Serializable]
public struct BlendTreeNode : IComparable<BlendTreeNode> {
  public AnimationClip Clip;
  public float Value;
  public float YRotation;
  public int CompareTo(BlendTreeNode b) => Value.CompareTo(b.Value);
}

public class BlendTreeBehaviour : PlayableBehaviour {
  public AnimationCurve BlendCurve;
  public Transform Transform;
  public float CycleSpeed = 1;
  public float Value = 0;

  Playable Playable;
  AnimationMixerPlayable Mixer;
  AnimationClipPlayable[] ClipPlayables;
  BlendTreeNode[] Nodes;

  public override void OnPlayableCreate(Playable playable) {
    Mixer = AnimationMixerPlayable.Create(playable.GetGraph(), 0);
    Playable = playable;
    Playable.ConnectInput(0, Mixer, 0);
  }

  public override void OnPlayableDestroy(Playable playable) {
    Mixer.Destroy();
    foreach (var clipPlayable in ClipPlayables) {
      clipPlayable.Destroy();
    }
  }

  public override void PrepareFrame(Playable playable, FrameData info) {
    var rotation = 0f;
    for (var i = 0; i < Nodes.Length; i++) {
      var weight = Weight(i, Value, Nodes);
      rotation += weight * Nodes[i].YRotation;
      Mixer.SetInputWeight(i, weight);
      ClipPlayables[i].SetSpeed(Nodes[i].Clip.length / CycleSpeed);
    }
    Transform.rotation = Quaternion.Euler(0, rotation, 0);
  }

  public void SetNodes(BlendTreeNode[] nodes) {
    Nodes = nodes.SortedCopy();
    ClipPlayables = new AnimationClipPlayable[Nodes.Length];
    Mixer.SetInputCount(Nodes.Length);
    for (var i = 0; i < Nodes.Length; i++) {
      var node = Nodes[i];
      var playable = AnimationClipPlayable.Create(Playable.GetGraph(), node.Clip);
      playable.SetSpeed(node.Clip.length / CycleSpeed);
      Mixer.ConnectInput(i, playable, 0, 0);
      ClipPlayables[i] = playable;
    }
  }

  float Weight(int index, float value, BlendTreeNode[] nodes) {
    var minIndex = 0;
    var maxIndex = nodes.Length-1;
    if (index < minIndex || index > maxIndex) {
      return 0;
    } else if (maxIndex == 0) {
      return 1;
    } else if (index > minIndex && value >= nodes[index-1].Value && value <= nodes[index].Value) {
      return BlendCurve.Evaluate(Mathf.InverseLerp(nodes[index-1].Value, nodes[index].Value, value));
    } else if (index < maxIndex && value >= nodes[index].Value && value <= nodes[index+1].Value) {
      return 1-BlendCurve.Evaluate(Mathf.InverseLerp(nodes[index].Value, nodes[index+1].Value, value));
    } else {
      return 0;
    }
  }
}

public class MotionMatching : MonoBehaviour {
  public Animator Animator;
  public AvatarMask LowerBodyMask;
  public BlendTreeNode[] LowerBodyNodes;
  public AnimationCurve BlendCurve;
  [Range(.1f, 10)]
  public float CycleSpeed = 1;
  [Range(.1f, 10)]
  public float TwistPeriod = 1;
  public float MinTwist = -90;
  public float MaxTwist = 90;
  [Range(-90, 90)]
  public float TorsoTwist;
  public PlayableGraph Graph;
  public AnimationPlayableOutput Output;
  public AnimationLayerMixerPlayable FinalMixer;
  public ScriptPlayable<BlendTreeBehaviour> BlendTreePlayable;
  public BlendTreeBehaviour BlendTree;

  void Start() {
    Graph = PlayableGraph.Create("Motion Matching");
    BlendTreePlayable = ScriptPlayable<BlendTreeBehaviour>.Create(Graph, 1);
    BlendTree = BlendTreePlayable.GetBehaviour();
    BlendTree.BlendCurve = BlendCurve;
    BlendTree.CycleSpeed = CycleSpeed;
    BlendTree.Value = TorsoTwist;
    BlendTree.Transform = transform;
    BlendTree.SetNodes(LowerBodyNodes);
    FinalMixer = AnimationLayerMixerPlayable.Create(Graph, 1);
    FinalMixer.TryAddLayerMaskFromAvatarMask(0, LowerBodyMask);
    Output = AnimationPlayableOutput.Create(Graph, "Motion Matching", Animator);
    Graph.Connect(BlendTreePlayable, 0, FinalMixer, 0);
    Output.SetSourcePlayable(FinalMixer);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  void Update() {
    var delta = (MaxTwist-MinTwist) / 2 * Mathf.Sin(2 * Mathf.PI * Time.time / TwistPeriod);
    var midpoint = (MaxTwist + MinTwist) / 2;
    TorsoTwist = delta + midpoint;
    BlendTree.Value = TorsoTwist;
    BlendTree.CycleSpeed = CycleSpeed;
    BlendTree.BlendCurve = BlendCurve;
  }
}