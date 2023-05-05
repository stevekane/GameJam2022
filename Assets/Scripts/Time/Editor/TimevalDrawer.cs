using UnityEngine;
using UnityEditor;

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