using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.UIElements;
using UnityEngine.Audio;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

namespace PlayablesGraphVisualization {
  public class PlayablesGraphEditorWindow : EditorWindow {
    [MenuItem("PlayableGraph/Visualizer")]
    public static void OpenPlayablesGraphEditorWindow() {
      var window = GetWindow<PlayablesGraphEditorWindow>();
      window.titleContent = new GUIContent("Playables Graph");
    }

    PlayableGraph SampleGraph;
    List<PlayableGraph> Graphs;
    Button ChangeGraphButton;
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

    void OnChangeGraphClick() {
      var menu = new GenericMenu();
      for (var i = 0; i < Graphs.Count; i++) {
        var graph = Graphs[i];
        var name = graph.GetEditorName();
        var index = i;
        menu.AddItem(new GUIContent(name), false, delegate {
          GraphView.graphElements.ForEach(GraphView.RemoveElement);
          GraphView.Render(Graphs[index]);
        });
      }
      menu.ShowAsContext();
    }

    void OnEnable() {
      UnityEditor.Playables.Utility.graphCreated += OnGraphCreated;
      UnityEditor.Playables.Utility.destroyingGraph += OnGraphDestroyed;
      ChangeGraphButton = new Button { text = "Change Graph" };
      ChangeGraphButton.clicked += OnChangeGraphClick;
      Graphs = new(UnityEditor.Playables.Utility.GetAllGraphs());
      GraphView = new PlayablesGraphView();
      GraphView.name = "Playables Graph";
      if (Graphs.Count > 0)
        GraphView.Render(Graphs[0]);
      // TODO: This is clumsy manual ordering. Seems like it should be possible to lay out the elements
      // in a smarter way using USS and flex grow and all that shit... good enough for now?
      rootVisualElement.Add(ChangeGraphButton);
      rootVisualElement.Add(GraphView);
      GraphView.PlaceBehind(ChangeGraphButton);
      GraphView.StretchToParentSize();
    }

    void Update() {
      // Immediate-mode style... extremely slow
      // GraphView.graphElements.ForEach(GraphView.RemoveElement);
      // GraphView.Render(Graphs[Graphs.Count-1]);
    }

    void OnDisable() {
      UnityEditor.Playables.Utility.graphCreated -= OnGraphCreated;
      UnityEditor.Playables.Utility.destroyingGraph -= OnGraphDestroyed;
      ChangeGraphButton.clicked -= OnChangeGraphClick;
      rootVisualElement.Remove(ChangeGraphButton);
      rootVisualElement.Remove(GraphView);
      ChangeGraphButton = null;
      GraphView = null;
      Graphs = null;
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