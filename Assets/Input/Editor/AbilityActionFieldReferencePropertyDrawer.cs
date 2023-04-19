using System;
using UnityEngine;
using UnityEditor;

[Serializable]
[CustomPropertyDrawer(typeof(AbilityActionFieldReference), true)]
public class AbilityActionFieldReferencePropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    FieldReferencePropertyDrawer.Render<AbilityAction>(position, property, label);
  }
}