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
    var timeval = Timeval.FromMillis(millisProp.floatValue);

    //p.width /= 2;
    var labels = new[] { new GUIContent("Seconds") };
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(labels[0]).x;
    var newSeconds = EditorGUI.FloatField(p, labels[0], timeval.Seconds);

    if (EditorGUI.EndChangeCheck()) {
      timeval = Timeval.FromSeconds(newSeconds);
      millisProp.floatValue = timeval.Millis;
    }

    EditorGUI.indentLevel = indent;

    EditorGUI.EndProperty();
  }
}