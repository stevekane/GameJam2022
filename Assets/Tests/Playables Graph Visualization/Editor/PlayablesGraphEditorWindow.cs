using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace PlayblesGraphVisualization {
  public class PlayablesGraphEditorWindow : EditorWindow {
    [MenuItem("PlayableGraph/Visualizer")]
    public static void OpenPlayablesGraphEditorWindow() {
      var window = GetWindow<PlayablesGraphEditorWindow>();
      window.titleContent = new GUIContent("Playables Graph");
    }

    PlayablesGraphView GraphView;

    void OnEnable() {
      GraphView?.Teardown();
      GraphView = new PlayablesGraphView();
      GraphView.name = "Playables Graph";
      GraphView.Setup();
      rootVisualElement.Add(GraphView);
    }

    void OnDisable() {
      rootVisualElement.Remove(GraphView);
      GraphView.Teardown();
      GraphView = null;
    }
  }
}