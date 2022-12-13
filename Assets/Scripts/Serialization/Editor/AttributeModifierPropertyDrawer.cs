using System;
using UnityEngine;
using UnityEditor;

[Serializable]
[CustomPropertyDrawer(typeof(AttributeModifier))]
public class AttributeModifierPropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    Rect p = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

    var indent = EditorGUI.indentLevel;
    EditorGUI.indentLevel = 0;

    EditorGUI.BeginChangeCheck();
    var baseProp = property.FindPropertyRelative("Base");
    var multProp = property.FindPropertyRelative("Mult");

    p.width /= 2;
    p.width -= 4;
    var labels = new[] { new GUIContent("+"), new GUIContent("x") };
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(labels[0]).x;
    var newBase = EditorGUI.FloatField(p, labels[0], baseProp.floatValue);
    p.x += p.width + 4;
    EditorGUIUtility.labelWidth = EditorStyles.label.CalcSize(labels[1]).x;
    var newMult = EditorGUI.FloatField(p, labels[1], multProp.floatValue);

    if (EditorGUI.EndChangeCheck()) {
      baseProp.floatValue = newBase;
      multProp.floatValue = newMult;
    }

    EditorGUI.indentLevel = indent;

    EditorGUI.EndProperty();
  }
}
