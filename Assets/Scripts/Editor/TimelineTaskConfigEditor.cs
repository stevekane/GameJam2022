using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Timeline;

[CustomPropertyDrawer(typeof(TimelineTaskConfig))]
public class TimelineTaskConfigConfigDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EnsureTracksMatchAsset(property);
    EditorGUI.PropertyField(position, property, label, true);
    property.serializedObject.ApplyModifiedProperties();
  }

  void EnsureTracksMatchAsset(SerializedProperty property) {
    var config = (TimelineTaskConfig)property.boxedValue;
    var asset = config.Asset;
    var realBindings = asset.GetOutputTracks().Select(t => new TimelineTrackBinding { Track = t }).ToArray();
    var missingTrack = false;
    for (int i = 0; i < realBindings.Length; i++) {
      var configIdx = Array.FindIndex(config.Bindings, c => c.Track?.GetInstanceID() == realBindings[i].Track.GetInstanceID());
      if (configIdx != -1) {
        realBindings[i].Binding = config.Bindings[configIdx].Binding;
      } else {
        missingTrack = true;
      }
    }
    if (missingTrack || config.Bindings.Length != realBindings.Length) {
      //Debug.Log($"Updated old bindings: {missingTrack} vs {config.Bindings.Length}");
      config.Bindings = realBindings;
      property.boxedValue = config;
    }
  }

  public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
    return EditorGUI.GetPropertyHeight(property) + (property.isExpanded ? 20f : 0f);
  }
}

[CustomPropertyDrawer(typeof(TimelineTrackBinding))]
public class TimelineTrackBindingDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.indentLevel++;
    var binding = (TimelineTrackBinding)property.boxedValue;
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.ObjectField(property.FindPropertyRelative("Track"), binding.Track.GetType(), GUIContent.none);
    if (binding.Track.GetType().IsDefined(typeof(TrackBindingTypeAttribute), false)) {
      var bindingType = binding.Track.GetType().GetAttribute<TrackBindingTypeAttribute>();
      EditorGUILayout.ObjectField(property.FindPropertyRelative("Binding"), bindingType.type, GUIContent.none);
    }
    EditorGUILayout.EndHorizontal();
    property.serializedObject.ApplyModifiedProperties();
    EditorGUI.indentLevel--;
  }
  public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
    return 0f;
  }
}