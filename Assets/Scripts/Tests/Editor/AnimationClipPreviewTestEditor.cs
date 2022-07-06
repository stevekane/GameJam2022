using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimationClipPreviewTest))]
public class AnimationClipPreviewTestEditor : Editor {
  public Transform preview;
  float time;
  Editor clipEditor;

  public override void OnInspectorGUI() {
    var obj = serializedObject.targetObject as AnimationClipPreviewTest;
    var clip = obj.AnimationClip;

    if (!preview) {
      preview = new GameObject().transform;
    }
    if (!clipEditor) {
      clipEditor = Editor.CreateEditor(preview.gameObject);
      clipEditor.HasPreviewGUI();
    }
    base.OnInspectorGUI();
    EditorGUILayout.BeginVertical();
    var frame = (int)(clip.frameRate * time);
    GUILayout.Label($"Frame: {frame}");
    time = EditorGUILayout.Slider(time, 0, clip.length);
    var previewRect = GUILayoutUtility.GetRect(256, 256);
    AnimationMode.StartAnimationMode();
    AnimationMode.BeginSampling();
    AnimationMode.SampleAnimationClip(preview.gameObject, clip, time);
    AnimationMode.EndSampling();
    AnimationMode.StopAnimationMode();
    clipEditor.OnPreviewSettings();
    clipEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.whiteLabel);
    clipEditor.ReloadPreviewInstances();
    EditorGUILayout.EndVertical();
  }
}