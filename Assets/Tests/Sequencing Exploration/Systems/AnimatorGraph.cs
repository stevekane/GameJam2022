using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[ExecuteInEditMode]
[DefaultExecutionOrder(ScriptExecutionGroups.Animation)]
public class AnimatorGraph : MonoBehaviour {
  AnimationPlayableOutput Output;
  AnimationClipPlayable CurrentPlayable;

  int MixerIndex;
  int LastMixerIndex;
  float LastMixerWeight;
  float MixerDuration;
  float MixerRemaining;

  public PlayableGraph Graph;
  public AnimationLayerMixerPlayable LayerMixer;
  public Playable Mixer;
  public List<CharacterState> CharacterStates = new();

  void OnEnable() {
    RebuildGraph();
  }

  void OnDisable() {
    if (Graph.IsValid())
      Graph.Destroy();
  }

  void OnValidate() {
    RebuildGraph();
  }

  void FixedUpdate() {
    var count = Mixer.GetInputCount();
    var fractionOn = Mathf.InverseLerp(0, MixerDuration, MixerDuration-MixerRemaining);
    var fractionOff = 1-fractionOn;
    for (var i = 0; i < count; i++) {
      if (i == MixerIndex) {
        Mixer.SetInputWeight(i, MixerDuration > 0 ? fractionOn : 1);
      } else if (i == LastMixerIndex) {
        Mixer.SetInputWeight(i, MixerDuration > 0 ? LastMixerWeight * fractionOff : 0);
      } else {
        Mixer.SetInputWeight(i, 0);
      }
    }
    MixerRemaining = Mathf.Max(0, MixerRemaining-Time.fixedDeltaTime);
    Graph.Evaluate(Time.fixedDeltaTime);
  }

  public void CrossFade(int index, float duration = .2f) {
    var input = Mixer.GetInput(index);
    input.SetTime(0);
    LastMixerIndex = MixerIndex;
    LastMixerWeight = Mixer.GetInputWeight(MixerIndex);
    MixerIndex = index;
    MixerDuration = duration;
    MixerRemaining = duration;
  }

  public void RebuildGraph() {
    if (Graph.IsValid())
      Graph.Destroy();
    Graph = PlayableGraph.Create(name);
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    CurrentPlayable = AnimationClipPlayable.Create(Graph, null);
    Mixer = AnimationMixerPlayable.Create(Graph);
    foreach (var characterState in CharacterStates) {
      Mixer.AddInput(characterState.CreatePlayable(Graph, characterState.gameObject), 0, 0);
    }
    LayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    LayerMixer.SetInputCount(2);
    LayerMixer.ConnectInput(0, Mixer, 0, 1);
    LayerMixer.ConnectInput(1, CurrentPlayable, 0, 0);
    Output = AnimationPlayableOutput.Create(Graph, name, GetComponent<Animator>());
    Output.SetSourcePlayable(LayerMixer);
  }

  public void Connect(Playable playable, int outputPort) {
    Output.SetSourcePlayable(playable, outputPort);
  }

  public void Disconnect() {
    if (Graph.IsValid()) {
      Graph.DestroySubgraph(CurrentPlayable);
      CurrentPlayable = AnimationClipPlayable.Create(Graph, null);
      LayerMixer.SetInputWeight(1, 0);
    }
  }

  public void Evaluate(AnimationClip clip, double time, double duration, float weight) {
    if (CurrentPlayable.IsNull() || CurrentPlayable.GetAnimationClip() != clip) {
      Graph.DestroySubgraph(CurrentPlayable);
      CurrentPlayable = AnimationClipPlayable.Create(Graph, clip);
      CurrentPlayable.SetDuration(duration);
      LayerMixer.ConnectInput(1, CurrentPlayable, 0, 1);
    }
    LayerMixer.SetInputWeight(1, weight);
    CurrentPlayable.SetTime(time);
    #if UNITY_EDITOR
    if (!Application.isPlaying) {
      Graph.Evaluate();
    }
    #endif
  }
}