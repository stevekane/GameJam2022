using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SerializableEnumBase), true)]
public class SerializableEnumPropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var valueProp = property.FindPropertyRelative("Value");
    var strProp = property.FindPropertyRelative("String");
    var newIndex = EditorGUI.Popup(position, label.text, valueProp.enumValueIndex, valueProp.enumDisplayNames);
    if (newIndex == valueProp.enumValueIndex)
      return;
    valueProp.enumValueIndex = newIndex;
    strProp.stringValue = valueProp.enumNames[newIndex];
    property.serializedObject.ApplyModifiedProperties();
    property.serializedObject.Update();
  }
}