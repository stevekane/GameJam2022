using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Timeval {
  public static int FixedUpdatePerSecond = 60;
  public static int TickCount = 0;

  [SerializeField] public float Millis = 1;
  [SerializeField] public int FramesPerSecond = 30;

  public static Timeval FromSeconds(float seconds, int fps = 30) {
    return new Timeval { Millis = seconds*1000f, FramesPerSecond = fps };
  }
  public static Timeval FromMillis(float millis, int fps = 30) {
    return new Timeval { Millis = millis, FramesPerSecond = fps };
  }
  public static Timeval FromAnimFrames(int ticks, int fps) {
    return new Timeval { Millis = (float)ticks * 1000f / fps, FramesPerSecond = fps };
  }
  public int Ticks {
    set { Millis = value * 1000f / FixedUpdatePerSecond; }
    get { return Mathf.RoundToInt(Millis * FixedUpdatePerSecond / 1000f); }
  }
  public int AnimFrames {
    set { Millis = value * 1000f / FramesPerSecond; }
    get { return Mathf.RoundToInt(Millis * FramesPerSecond / 1000f); }
  }
  public float Seconds {
    get { return Millis * .001f; }
  }
}

[CustomPropertyDrawer(typeof(Timeval))]
public class TimevalDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    Rect p = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

    var indent = EditorGUI.indentLevel;
    EditorGUI.indentLevel = 0;

    EditorGUI.BeginChangeCheck();
    var millisProp = property.FindPropertyRelative("Millis");
    var fpsProp = property.FindPropertyRelative("FramesPerSecond");
    var timeval = Timeval.FromMillis(millisProp.floatValue, fpsProp.intValue);

    p.width /= 2;
    var labels = new[] { new GUIContent("Frames"), new GUIContent("fps") };
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(labels[0]).x;
    var mewFrames = EditorGUI.IntField(p, labels[0], timeval.AnimFrames);
    p.x += p.width;
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(labels[1]).x;
    var newFps = EditorGUI.IntField(p, labels[1], fpsProp.intValue);

    if (EditorGUI.EndChangeCheck()) {
      timeval = Timeval.FromAnimFrames(mewFrames, newFps);
      millisProp.floatValue = timeval.Millis;
      fpsProp.intValue = timeval.FramesPerSecond;
    }

    EditorGUI.indentLevel = indent;

    EditorGUI.EndProperty();
  }
}
