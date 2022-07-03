using System;
using UnityEngine;
using UnityEditor;

[Serializable]
public class AttackFrameData {
  public static int RunTimeFPS = Timeval.FramesPerSecond;
  public static int AuthoredFPS = 30;

  [SerializeField] int RunTimeFrames;

  public static int ToRunTimeFrames(int AuthoredFrames) {
    return (int)((float)AuthoredFrames/(float)AuthoredFPS*(float)RunTimeFPS);
  }

  public static int ToAuthoredFrames(int RunTimeFrames) {
    return (int)((float)RunTimeFrames/(float)RunTimeFPS*(float)AuthoredFPS);
  }

  public int Frames {
    get { return RunTimeFrames; }
  }

  public int AuthoredFrames {
    get { return ToAuthoredFrames(RunTimeFrames); }
  }
}

[CustomPropertyDrawer(typeof(AttackFrameData))]
public class AttackFrameDataDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position,label,property);
    position = EditorGUI.PrefixLabel(position,GUIUtility.GetControlID(FocusType.Passive),label);
    EditorGUI.BeginChangeCheck();
    var runtimeFramesProp = property.FindPropertyRelative("RunTimeFrames");
    var runtimeFrames = runtimeFramesProp.intValue;
    var previousAuthoredFrames = AttackFrameData.ToAuthoredFrames(runtimeFrames);
    var info = $"Frames ({AttackFrameData.AuthoredFPS}fps)";
    var newAuthoredFrames = EditorGUI.IntField(position,info,previousAuthoredFrames);
    if (EditorGUI.EndChangeCheck()) {
      runtimeFramesProp.intValue = AttackFrameData.ToRunTimeFrames(newAuthoredFrames);
    }
    EditorGUI.EndProperty();
  }
}