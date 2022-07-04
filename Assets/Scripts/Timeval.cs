using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Timeval {
  public static int FramesPerSecond = 500;

  [SerializeField] public float Millis = 1;
  [SerializeField] public int TicksPerSecond = 30;

  public static Timeval FromMillis(float millis, int ticksPerSecond = 30) {
    return new Timeval { Millis = millis, TicksPerSecond = ticksPerSecond };
  }
  public static Timeval FromTicks(int ticks, int ticksPerSecond) {
    return new Timeval { Millis = (float)ticks * 1000f / ticksPerSecond, TicksPerSecond = ticksPerSecond };
  }
  public int Frames {
    set { Millis = value * 1000f / FramesPerSecond; } 
    get { return Mathf.RoundToInt(Millis * FramesPerSecond / 1000f); } 
  }
  public int Ticks {
    set { Millis = value * 1000f / TicksPerSecond; }
    get { return Mathf.RoundToInt(Millis * TicksPerSecond / 1000f); }
  }
  public float Seconds { 
    get { return Millis * .0001f; } 
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
    var tpsProp = property.FindPropertyRelative("TicksPerSecond");
    var timeval = Timeval.FromMillis(millisProp.floatValue, tpsProp.intValue);

    p.width /= 2;
    var labels = new[] { new GUIContent("Ticks"), new GUIContent("tps") };
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(labels[0]).x;
    var newTicks = EditorGUI.IntField(p, labels[0], timeval.Ticks);
    p.x += p.width;
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(labels[1]).x;
    var newTps = EditorGUI.IntField(p, labels[1], tpsProp.intValue);

    if (EditorGUI.EndChangeCheck()) {
      timeval = Timeval.FromTicks(newTicks, newTps);
      Debug.Log($"Millis change {millisProp.floatValue} => {timeval.Millis}");
      millisProp.floatValue = timeval.Millis;
      tpsProp.intValue = timeval.TicksPerSecond;
    }

    EditorGUI.indentLevel = indent;

    EditorGUI.EndProperty();
  }
}
