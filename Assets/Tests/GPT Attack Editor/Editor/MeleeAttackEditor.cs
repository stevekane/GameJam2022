using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

/*
Known bugs:

When user changes clip to "none" the correct behavior happens but the propertifield continues to display
the last assigned value instead of displaying None (AnimationClip) which is the normal or default behaviuor.
This is corrected when the editor is recreated by selecting a different object and then back to this editor.
*/

[CustomEditor(typeof(MeleeAttack))]
public class MeleeAttackEditor : Editor {
  public override VisualElement CreateInspectorGUI() {
    var meleeAttack = (MeleeAttack)target;
    var container = new VisualElement();
    var totalFramesProp = serializedObject.FindProperty("TotalFrames");
    var totalFramesField = new PropertyField(totalFramesProp);
    var clipProperty = serializedObject.FindProperty("Clip");
    var clipField = new PropertyField(clipProperty);
    var trackProperty = serializedObject.FindProperty("Track");
    var track = new TrackView(totalFramesProp, trackProperty);
    var animationPreview = new AnimationPreview();
    animationPreview.SetClip((AnimationClip)clipProperty.objectReferenceValue);
    totalFramesField.RegisterValueChangeCallback(e => track.RenderClip());
    clipField.RegisterValueChangeCallback(e => animationPreview.SetClip((AnimationClip)clipProperty.objectReferenceValue));
    container.Add(totalFramesField);
    container.Add(clipField);
    container.Add(animationPreview);
    container.Add(track);
    return container;
  }
}

public class AnimationPreview : VisualElement {
  public int StartFrame;
  public int EndFrame;
  public int Frame;
  public Slider slider;

  Label frameLabel;

  public AnimationPreview() {
    Frame = 0;
    StartFrame = 0;
    frameLabel = new Label($"{Frame} / {EndFrame}");
    slider = new Slider(0, EndFrame);
    slider.RegisterValueChangedCallback(OnSliderChange);
    Add(frameLabel);
    Add(slider);
  }

  void OnSliderChange(ChangeEvent<float> changeEvent) {
    Frame = Mathf.RoundToInt(changeEvent.newValue);
    frameLabel.text = $"{Frame} / {EndFrame}";
  }

  public void SetClip(AnimationClip clip) {
    Debug.Log($"Set Clip {clip}");
    if (clip) {
      EndFrame = Mathf.RoundToInt(clip.frameRate * clip.length);
    } else {
      EndFrame = 0;
    }
    visible = clip;
    Frame = 0;
    StartFrame = 0;
    frameLabel.text = $"{Frame} / {EndFrame}";
    slider.value = Frame;
    slider.lowValue = StartFrame;
    slider.highValue = EndFrame;
  }
}

public enum DragType {
  Start,
  End,
  Whole
}

public class TrackView : VisualElement {
  VisualElement trackBlock;
  VisualElement clip;
  SerializedProperty totalFramesProperty;
  SerializedProperty trackProperty;
  SerializedProperty startFrameProperty;
  SerializedProperty endFrameProperty;
  bool dragging;
  DragType dragType;
  int startFrameDragStart;
  int endFrameDragStart;
  Vector2 dragStart;

  public TrackView(SerializedProperty totalFrames, SerializedProperty trackProp) {
    totalFramesProperty = totalFrames;
    trackProperty = trackProp;
    startFrameProperty = trackProperty.FindPropertyRelative("StartFrame");
    endFrameProperty = trackProperty.FindPropertyRelative("EndFrame");
    var startFrame = new PropertyField(startFrameProperty);
    var endFrame = new PropertyField(endFrameProperty);
    var trackContainer = new VisualElement();
    trackContainer.style.height = 32;
    trackContainer.style.flexDirection = FlexDirection.Row;
    clip = new VisualElement();
    startFrame.RegisterValueChangeCallback(e => RenderClip());
    endFrame.RegisterValueChangeCallback(e => RenderClip());
    clip.RegisterCallback<MouseDownEvent>(e => {
      var position = e.localMousePosition;
      var width = clip.worldBound.width;
      var fraction = position / width;
      startFrameDragStart = startFrameProperty.intValue;
      endFrameDragStart = endFrameProperty.intValue;
      dragging = true;
      dragType =
        fraction.x <= .2f ? DragType.Start :
        fraction.x >= .8f ? DragType.End :
        DragType.Whole;
    });
    trackContainer.RegisterCallback<FocusOutEvent>(e => {
      dragging = false;
    });
    trackContainer.RegisterCallback<MouseLeaveEvent>(e => {
      dragging = false;
    });
    trackContainer.RegisterCallback<MouseUpEvent>(e => {
      dragging = false;
    });
    trackContainer.RegisterCallback<MouseDownEvent>(e => {
      dragStart = e.localMousePosition;
    });
    trackContainer.RegisterCallback<MouseMoveEvent>(e => {
      if (!dragging)
        return;
      var dragDelta = e.localMousePosition - dragStart;
      var width = trackContainer.worldBound.width;
      var totalFrames = totalFramesProperty.intValue;
      var frameDelta = Mathf.RoundToInt((float)dragDelta.x / width * totalFrames);
      var nextStartFrame = startFrameDragStart + frameDelta;
      var nextEndFrame = endFrameDragStart + frameDelta;
      switch (dragType) {
        case DragType.Start:
          if (nextStartFrame >= 0 && nextStartFrame <= endFrameDragStart && nextStartFrame <= totalFrames) {
            startFrameProperty.intValue = startFrameDragStart + frameDelta;
            startFrameProperty.serializedObject.ApplyModifiedProperties();
          }
        break;
        case DragType.End:
          if (nextEndFrame >= 0 && nextEndFrame >= startFrameDragStart && nextEndFrame <= totalFrames) {
            endFrameProperty.intValue = endFrameDragStart + frameDelta;
            endFrameProperty.serializedObject.ApplyModifiedProperties();
          }
        break;
        case DragType.Whole:
          if (nextStartFrame >= 0 && nextStartFrame <= totalFrames && nextEndFrame >= 0 && nextEndFrame <= totalFrames) {
            startFrameProperty.intValue = startFrameDragStart + frameDelta;
            startFrameProperty.serializedObject.ApplyModifiedProperties();
            endFrameProperty.intValue = endFrameDragStart + frameDelta;
            endFrameProperty.serializedObject.ApplyModifiedProperties();
          }
        break;
      }
    });
    RenderClip();
    Add(startFrame);
    Add(endFrame);
    Add(trackContainer);
    trackContainer.Add(clip);
  }

  public void RenderClip() {
    var frames = totalFramesProperty.intValue;
    var start = (float)startFrameProperty.intValue / frames;
    var width = (float)(endFrameProperty.intValue - startFrameProperty.intValue) / frames;
    clip.style.backgroundColor = Color.blue;
    clip.style.left = new StyleLength(new Length(start * 100, LengthUnit.Percent));
    clip.style.width = new StyleLength(new Length(width * 100, LengthUnit.Percent));
    clip.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
  }
}