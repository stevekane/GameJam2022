using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AttackConfig))]
public class AttackConfigEditor : Editor {
  public GameObject Preview;

  Editor ClipEditor;
  float Time;

  public override void OnInspectorGUI() {
    if (!ClipEditor) {
      ClipEditor = Editor.CreateEditor(Preview);
      ClipEditor.HasPreviewGUI();
    }
    var attackConfig = (AttackConfig)target;
    var clip = attackConfig.Clip;
    var frame = (int)(clip.frameRate * Time);

    EditorGUILayout.BeginVertical();
    GUILayout.Label($"Frame: {frame}");
    Time = EditorGUILayout.Slider(Time, 0, clip.length);
    AnimationMode.StartAnimationMode();
    AnimationMode.BeginSampling();
    AnimationMode.SampleAnimationClip(Preview, clip, Time);
    AnimationMode.EndSampling();
    AnimationMode.StopAnimationMode();
    ClipEditor.OnPreviewSettings();
    ClipEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(256,256), EditorStyles.whiteLabel);
    ClipEditor.ReloadPreviewInstances();
    EditorGUILayout.EndVertical();
    base.OnInspectorGUI();

  }
}