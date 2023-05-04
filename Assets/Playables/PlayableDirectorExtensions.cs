using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor.Timeline;
public static class TimelineEditorManager {
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  public static void OnApplicationStartup() {
    var timelineEditorWindow = TimelineEditorWindow.GetWindow<TimelineEditorWindow>();
    var timelineEditorWindowType = timelineEditorWindow.GetType();
    var concreteType = timelineEditorWindow.GetType();
    if (!timelineEditorWindow)
      return;
    var statePropertyName = "state";
    var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    var stateProperty = timelineEditorWindowType.GetProperty(statePropertyName, bindingFlags);
    if (stateProperty == null) {
      Debug.LogError($"Could not find {statePropertyName} on TimelineEditorWindow");
      return;
    }
    var state = stateProperty.GetValue(timelineEditorWindow, null);
    var stateType = state.GetType();
    var previewModeName = "previewMode";
    var previewModeProperty = stateType.GetProperty(previewModeName, bindingFlags);
    if (previewModeProperty == null) {
      Debug.LogError($"Could not find {previewModeName} on {stateType}");
      return;
    }
    previewModeProperty.SetValue(state, false);
  }
}
#endif

public static class PlayableDirectorExtensions {
  public static async Task PlayTask(this PlayableDirector director, TaskScope scope, LocalTime localTime) {
    try {
      director.timeUpdateMode = DirectorUpdateMode.Manual;
      director.extrapolationMode = DirectorWrapMode.None;
      director.time = 0;
      director.Evaluate();
      do {
        await scope.Tick();
        director.time += localTime.FixedDeltaTime;
        director.Evaluate();
      } while (director.time < director.duration);
    } catch (OperationCanceledException) {
    } catch (Exception e) {
      Debug.LogError(e.Message);
    } finally {
      director.RebuildGraph();
    }
  }
}