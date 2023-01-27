using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

/*
Construct playable graph.

Loop the running animation.
Add an attack animation.
Read data from the attack animation stream.
Mix attack animation with the looping running.
Create mixer out of the looping running cycles.
Use data from attack animation stream to blend between running cycles.
Blend result of blended running cycles with attack.
*/
[Serializable]
public struct BlendTreeNode {
  public AnimationClip Clip;
  public float Value;
  public float YRotation;
}

public class MotionMatching : MonoBehaviour {
  public Animator Animator;
  public AvatarMask LowerBodyMask;
  public BlendTreeNode[] LowerBodyNodes;
  public AnimationCurve BlendCurve;
  [Range(.1f,10)]
  public float CycleSpeed = 1;
  [Range(.1f, 10)]
  public float TwistPeriod = 1;
  public float MinTwist = -90;
  public float MaxTwist = 90;
  [Range(-90,90)]
  public float TorsoTwist;
  [HideInInspector]
  public PlayableGraph Graph;
  [HideInInspector]
  public AnimationPlayableOutput Output;
  [HideInInspector]
  public AnimationMixerPlayable LowerBodyMixer;
  [HideInInspector]
  public AnimationLayerMixerPlayable FinalMixer;

  void Start() {
    Graph = PlayableGraph.Create("Motion Matching");
    LowerBodyMixer = AnimationMixerPlayable.Create(Graph, 0);
    FinalMixer = AnimationLayerMixerPlayable.Create(Graph, 0);
    for (var i = 0; i < LowerBodyNodes.Length; i++) {
      var node = LowerBodyNodes[i];
      var playable = AnimationClipPlayable.Create(Graph, node.Clip);
      playable.SetSpeed(node.Clip.length / CycleSpeed);
      LowerBodyMixer.AddInput(playable, 0, 0);
    }
    FinalMixer.AddInput(LowerBodyMixer, 0, 1);
    if (LowerBodyMask) {
      FinalMixer.SetLayerMaskFromAvatarMask(0, LowerBodyMask);
    }
    Output = AnimationPlayableOutput.Create(Graph, "Motion Matching", Animator);
    Output.SetSourcePlayable(FinalMixer);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
  }

  float BoundedInverseLerp(float a, float b, float v) => v < a || v > b ? 0 : BlendCurve.Evaluate(Mathf.InverseLerp(a, b ,v));
  float OneMinusBoundedInverseLerp(float a, float b, float v) => v < a || v > b ? 0 : 1-BlendCurve.Evaluate(Mathf.InverseLerp(a, b ,v));

  float Weight(int index, float value, BlendTreeNode[] nodes) {
    var minIndex = 0;
    var maxIndex = nodes.Length-1;
    if (index < minIndex || index > maxIndex) {
      return 0;
    }
    var weight = 0f;
    if (maxIndex == 0) {
      weight = 1;
    }
    if (index > minIndex) {
      weight = Mathf.Max(weight, BoundedInverseLerp(nodes[index-1].Value, nodes[index].Value, value));
    }
    if (index < maxIndex) {
      weight = Mathf.Max(weight, OneMinusBoundedInverseLerp(nodes[index].Value, nodes[index+1].Value, value));
    }
    return weight;
  }

  void Update() {
    var delta = (MaxTwist-MinTwist) / 2 * Mathf.Sin(2 * Mathf.PI * Time.time / TwistPeriod);
    var midpoint = (MaxTwist + MinTwist) / 2;
    TorsoTwist = midpoint + delta;

    var rotation = 0f;
    for (var i = 0; i < LowerBodyNodes.Length; i++) {
      var weight = Weight(i, TorsoTwist, LowerBodyNodes);
      rotation += weight * LowerBodyNodes[i].YRotation;
      LowerBodyMixer.SetInputWeight(i, Weight(i, TorsoTwist, LowerBodyNodes));
    }
    transform.rotation = Quaternion.Euler(0, rotation, 0);

    for (var i = 0; i < LowerBodyMixer.GetInputCount(); i++) {
      var playable = (AnimationClipPlayable)LowerBodyMixer.GetInput(i);
      playable.SetSpeed(playable.GetAnimationClip().length / CycleSpeed);
    }
  }
}