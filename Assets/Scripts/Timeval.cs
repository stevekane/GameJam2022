using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class Timeval {
  [SerializeField] int value = 1;

  public int Frames { get { return value; } }
  public float Seconds { get { return value / 60f; } }
}

[CustomPropertyDrawer(typeof(Timeval))]
public class TimevalDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
    EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, position.height), property.FindPropertyRelative("value"), new GUIContent("Frames"));
    EditorGUI.EndProperty();
  }
}
