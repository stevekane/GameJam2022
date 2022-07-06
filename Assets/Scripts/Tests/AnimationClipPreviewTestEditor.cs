using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnimationClipPreviewTest))]
public class AnimationClipPreviewTestEditor : Editor {
  public Transform preview;
  float frame;
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
    frame = GUILayout.HorizontalSlider(frame,0,clip.length);
    // TODO: FUCK THIS FUCKING STUPID FUCKING CODE
    // If this box has almost ANY height besides this magical value 48
    // then the code does not work properly. 
    // I got this magic value from using GUILayoutUtility.GetRect(246,256)
    // What the fuck is going on here? I hope everyone that worked on this is gone
    var previewRect = new Rect(18,48,256,256);
    AnimationMode.StartAnimationMode();
    AnimationMode.BeginSampling();
    AnimationMode.SampleAnimationClip(preview.gameObject,clip,frame);
    AnimationMode.EndSampling();
    AnimationMode.StopAnimationMode();
    clipEditor.OnPreviewSettings();
    clipEditor.OnInteractivePreviewGUI(previewRect,EditorStyles.whiteLabel);
    clipEditor.ReloadPreviewInstances();
  }
}