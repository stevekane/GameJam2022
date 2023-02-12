using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Audio;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace PlayablesGraphVisualization {
  public class PlayablesGraphEditorWindow : EditorWindow {
    [MenuItem("PlayableGraph/Visualizer")]
    public static void OpenPlayablesGraphEditorWindow() {
      var window = GetWindow<PlayablesGraphEditorWindow>();
      window.titleContent = new GUIContent("Playables Graph");
    }

    PlayableGraph SampleGraph;
    List<PlayableGraph> Graphs;
    PlayablesGraphView GraphView;

    PlayableGraph CreateSampleGraph() {
      var graph = PlayableGraph.Create("Visualization Test Graph");
      var animClipPlayable1 = AnimationClipPlayable.Create(graph, null);
      var animClipPlayable2 = AnimationClipPlayable.Create(graph, null);
      var crossedPassthrough = ScriptPlayable<NoopBehavior>.Create(graph);
      var animationMixer = AnimationMixerPlayable.Create(graph);
      var noopPlayable = ScriptPlayable<NoopBehavior>.Create(graph);
      var orphanedAnimationClipPlayable = AnimationClipPlayable.Create(graph, null);
      var animationOutput = AnimationPlayableOutput.Create(graph, "Animation Output", null);
      crossedPassthrough.AddInput(animClipPlayable1, 0, 1);
      crossedPassthrough.AddInput(animClipPlayable2, 0, 1);
      crossedPassthrough.SetOutputCount(2);
      animationMixer.AddInput(crossedPassthrough, 1, 1);
      animationMixer.AddInput(crossedPassthrough, 0, 1);
      noopPlayable.AddInput(animationMixer, 0, 1);
      animationOutput.SetSourcePlayable(noopPlayable, 0);
      return graph;
    }

    void OnEnable() {
      UnityEditor.Playables.Utility.graphCreated += OnGraphCreated;
      UnityEditor.Playables.Utility.destroyingGraph += OnGraphDestroyed;
      Graphs = new(UnityEditor.Playables.Utility.GetAllGraphs());
      GraphView = new PlayablesGraphView();
      GraphView.name = "Playables Graph";
      GraphView.Render(Graphs[Graphs.Count-1]);
      rootVisualElement.Add(GraphView);
    }

    void Update() {
      // Immediate-mode style... extremely slow
      // GraphView.graphElements.ForEach(GraphView.RemoveElement);
      // GraphView.Render(Graphs[Graphs.Count-1]);
    }

    void OnDisable() {
      UnityEditor.Playables.Utility.graphCreated -= OnGraphCreated;
      UnityEditor.Playables.Utility.destroyingGraph -= OnGraphDestroyed;
      Graphs = null;
      rootVisualElement.Remove(GraphView);
      GraphView = null;
    }

    void OnGraphCreated(PlayableGraph graph) {
      if (!Graphs.Contains(graph)) {
        Graphs.Add(graph);
      }
    }

    void OnGraphDestroyed(PlayableGraph graph) {
      Graphs.Remove(graph);
    }
  }
}