using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Timeval {
  public static int FramesPerSecond = 500;

  [SerializeField] int Millis = 1;

  public static Timeval FromMillis(int millis) {
    return new Timeval { Millis = millis };
  }
  public int Frames { 
    set { Millis = value * 1000 / FramesPerSecond; } 
    get { return Millis * FramesPerSecond / 1000; } 
  }
  public float Seconds { 
    get { return Millis * .0001f; } 
  }
}

[CustomPropertyDrawer(typeof(Timeval))]
public class TimevalDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

    EditorGUI.BeginChangeCheck();
    var millisProp = property.FindPropertyRelative("Millis");
    var timeval = Timeval.FromMillis(millisProp.intValue);
    // TODO: should we input in frames, millis, or seconds? Can use PropertyAttribute to customize per-object.
    var newValue = EditorGUI.IntField(position, $"Millis (frames={timeval.Frames})", millisProp.intValue);
    if (EditorGUI.EndChangeCheck())
      millisProp.intValue = newValue;

    EditorGUI.EndProperty();
  }
}
