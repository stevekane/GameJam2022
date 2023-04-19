using System;
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
    var p1 = new Rect(position); p1.width /= 2;
    EditorGUI.ObjectField(p1, targetProp, GUIContent.none);
    var p2 = new Rect(position); p2.width /= 2; p2.x += p2.width;
    if (target && EditorGUI.DropdownButton(p2, new GUIContent(dropDownText), FocusType.Keyboard)) {
      var menu = new GenericMenu();
      var fields = target.GetType().GetFields();
      foreach (var field in fields) {
        if (field.GetType() == target.GetType()) {
          menu.AddItem(new GUIContent(field.Name), fieldProp.stringValue == field.Name, delegate {
            fieldProp.stringValue = field.Name;
            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
          });
        }
      }
      if (menu.GetItemCount() == 0) {
        menu.AddDisabledItem(new GUIContent("No valid methods found."), false);
      }
      menu.ShowAsContext();
    }
  }
}