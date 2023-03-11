using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[ExecuteInEditMode]
public class AnimatorGraph : MonoBehaviour {
  [SerializeField] RuntimeAnimatorController AnimatorController;

  AnimationPlayableOutput Output;
  PlayableGraph Graph;
  AnimationClipPlayable CurrentPlayable;
  AnimationLayerMixerPlayable LayerMixer;
  AnimatorControllerPlayable AnimatorControllerPlayable;

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
    if (CurrentPlayable.IsDone())
      Debug.Log("Done");
    Graph.Evaluate(Time.fixedDeltaTime);
  }

  public void RebuildGraph() {
    if (Graph.IsValid())
      Graph.Destroy();
    Graph = PlayableGraph.Create(name);
    Graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    Graph.Play();
    CurrentPlayable = AnimationClipPlayable.Create(Graph, null);
    AnimatorControllerPlayable = AnimatorControllerPlayable.Create(Graph, AnimatorController);
    LayerMixer = AnimationLayerMixerPlayable.Create(Graph);
    LayerMixer.SetInputCount(2);
    LayerMixer.ConnectInput(0, AnimatorControllerPlayable, 0, 1);
    LayerMixer.ConnectInput(1, CurrentPlayable, 0, 0);
    Output = AnimationPlayableOutput.Create(Graph, name, GetComponent<Animator>());
    Output.SetSourcePlayable(LayerMixer);
  }

  public void Connect(Playable playable, int outputPort) {
    Output.SetSourcePlayable(playable, outputPort);
  }

  public void Disconnect() {
    Graph.DestroySubgraph(CurrentPlayable);
    CurrentPlayable = AnimationClipPlayable.Create(Graph, null);
    LayerMixer.SetInputWeight(1, 0);
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