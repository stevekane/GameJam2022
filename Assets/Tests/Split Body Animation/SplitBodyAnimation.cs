using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[Serializable]
public struct SplitBodySpec {
  public Transform LowerRoot;
  public Transform UpperRoot;
}

public class SplitBodyAnimation : MonoBehaviour {
  public Animator Animator;
  public SplitBodySpec SplitBodySpec;
  public AnimationClip LowerClip;
  public AnimationClip UpperClip;

  PlayableGraph Graph;
  SplitBodyMixer SplitBodyMixer;

  void Start() {
    Graph = PlayableGraph.Create("Split Body Animation");
    var lowerClip = AnimationClipPlayable.Create(Graph, LowerClip);
    var upperClip = AnimationClipPlayable.Create(Graph, UpperClip);
    SplitBodyMixer = new SplitBodyMixer(Graph, Animator, SplitBodySpec.LowerRoot, SplitBodySpec.UpperRoot);
    SplitBodyMixer.Playable.ConnectInput(0, lowerClip, 0, 1);
    SplitBodyMixer.Playable.ConnectInput(1, upperClip, 0, 1);
    var output = AnimationPlayableOutput.Create(Graph, "Animation", Animator);
    output.SetSourcePlayable(SplitBodyMixer.Playable);
    Graph.Play();
  }

  void OnDestroy() {
    Graph.Destroy();
    SplitBodyMixer.Dispose();
  }
}