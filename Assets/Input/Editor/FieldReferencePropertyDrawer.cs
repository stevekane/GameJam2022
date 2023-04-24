using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[Serializable]
[CustomPropertyDrawer(typeof(FieldReference), true)]
public class FieldReferencePropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var fieldProp = property.FindPropertyRelative("FieldName");
    var targetProp = property.FindPropertyRelative("Target");
    var target = targetProp.objectReferenceValue;
    var dropDownText = fieldProp.stringValue == "" ? "Select method" : fieldProp.stringValue;
    var partWidth = position.width / 2;
    var p1 = new Rect(position.x, position.y, partWidth, position.height);
    var p2 = new Rect(position.x + partWidth, position.y, partWidth, position.height);
    var fieldType = property.boxedValue.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).PropertyType;

    EditorGUI.ObjectField(p1, targetProp, GUIContent.none);
    if (target && EditorGUI.DropdownButton(p2, new GUIContent(dropDownText), FocusType.Keyboard)) {
      var menu = new GenericMenu();
      var fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
      foreach (var field in fields) {
        if (field.FieldType == fieldType) {
          menu.AddItem(new GUIContent(field.Name), fieldProp.stringValue == field.Name, delegate {
            fieldProp.stringValue = field.Name;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
          });
        }
      }
      if (menu.GetItemCount() == 0) {
        menu.AddDisabledItem(new GUIContent("No valid fields found."), false);
      }
      menu.ShowAsContext();
    }
  }
}