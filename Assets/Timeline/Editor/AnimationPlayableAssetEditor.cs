using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine;
using System;
using System.Reflection;

[CustomEditor(typeof(AnimationSegmentClipAsset), true)]
public class AnimationSegmentClipAssetEditor : Editor {
  Editor DefaultEditor;

  void OnEnable() {
    var defaultEditorType = Type.GetType("UnityEditor.Timeline.AnimationPlayableAssetInspector, Unity.Timeline.Editor");
    Debug.Assert(defaultEditorType != null, "Bad things will happen now");
    DefaultEditor = Editor.CreateEditor(targets, defaultEditorType);
  }

  void OnDisable() {
    MethodInfo onDisable = DefaultEditor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    if (onDisable != null)
      onDisable.Invoke(DefaultEditor, null);
    DestroyImmediate(DefaultEditor);
  }

  public override void OnInspectorGUI() {
    DefaultEditor.OnInspectorGUI();

    var editorClip = Selection.activeObject;
    var clipProp = editorClip.GetType().GetField("m_Clip", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
    var timelineClip = clipProp.GetValue(editorClip) as TimelineClip;
    var clip = timelineClip.animationClip;
    var frameRate = GetFrameRate(timelineClip);

    EditorGUILayout.Space();
    EditorGUI.indentLevel--;
    EditorGUILayout.LabelField($"Clip Segment Timings (total is {clip.length}s, {clip.length * frameRate} frames)");
    EditorGUI.indentLevel++;
    EditorGUI.BeginChangeCheck();
    var newClipStart = TimeField("Clip Start", timelineClip.clipIn, frameRate);
    var clipEndProp = serializedObject.FindProperty("ClipEnd");
    var newClipEnd = TimeField("Clip End", clipEndProp.doubleValue, frameRate);
    if (EditorGUI.EndChangeCheck()) {
      timelineClip.clipIn = newClipStart;
      clipEndProp.doubleValue = newClipEnd;
      serializedObject.ApplyModifiedProperties();
    }
    var newDuration = newClipEnd - newClipStart;
    timelineClip.timeScale = newDuration / timelineClip.duration;
  }

  double GetFrameRate(TimelineClip clip) => clip.GetParentTrack()?.timelineAsset?.editorSettings.frameRate ?? 60f;
  double TimeField(string labelText, double seconds, double frameRate) {
    const float kSpacingSubLabel = 4f;
    var rect = EditorGUILayout.GetControlRect();
    rect = EditorGUI.PrefixLabel(rect, new GUIContent(labelText));
    var secondsRect = new Rect(rect.xMin, rect.yMin, rect.width / 2 - kSpacingSubLabel, rect.height);
    var framesRect = new Rect(rect.xMin + rect.width / 2, rect.yMin, rect.width / 2, rect.height);
    var newSeconds = DelayedDoubleField(secondsRect, "secs", seconds);
    EditorGUI.BeginChangeCheck();
    var newFrames = DelayedDoubleField(framesRect, "frames", seconds * frameRate);
    if (EditorGUI.EndChangeCheck()) {
      newSeconds = newFrames / frameRate;
    }
    return newSeconds;
  }

  static double DelayedDoubleField(Rect rect, string labelText, double value) {
    var label = new GUIContent(labelText);
    var old = EditorGUIUtility.labelWidth;
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(label).x;
    // WHY THE FUCK DOESNT THIS DISPLAY THE FULL LABEL???
    var rv = EditorGUI.DelayedDoubleField(rect, label, value);
    EditorGUIUtility.labelWidth = old;
    return rv;
  }
}