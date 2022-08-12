using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[Serializable]
public class ButtonTriggerMap {
  public ButtonCode ButtonCode;
  public ButtonPressType ButtonPressType;
  public AbilityMethodReference Entry;
}

[Serializable]
public class AxisTriggerMap {
  public AxisCode AxisCode;
  public AbilityMethodReference Entry;
}

[Serializable]
public class AbilityMethodReference {
  public Ability Ability;
  public string MethodName;
}

[Serializable]
[CustomPropertyDrawer(typeof(AbilityMethodReference))]
public class AbilityEventHandlerPropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var methodName = property.FindPropertyRelative("MethodName");
    var ability = property.FindPropertyRelative("Ability");
    var dropDownText = methodName.stringValue == "" ? "Select method" : methodName.stringValue;
    var p1 = new Rect(position); p1.width /= 2;
    EditorGUI.ObjectField(p1, ability, GUIContent.none);
    var p2 = new Rect(position); p2.width /= 2; p2.x += p2.width;
    if (EditorGUI.DropdownButton(p2, new GUIContent(dropDownText), FocusType.Keyboard)) {
      var menu = new GenericMenu();
      var target = ability.objectReferenceValue;
      var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
      var methods = target.GetType().GetMethods(flags);
      foreach (var method in methods) {
        if (method.GetParameters().Length <= 0 && method.ReturnType == typeof(IEnumerator)) {
          menu.AddItem(new GUIContent(method.Name), methodName.stringValue == method.Name, delegate {
            methodName.stringValue = method.Name;
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

[RequireComponent(typeof(AbilityManager))]
public class InputToTriggerMap : MonoBehaviour {
  [SerializeField] List<ButtonTriggerMap> ButtonEventTagMaps;
  [SerializeField] List<AxisTriggerMap> AxisEventTagMaps;

  // TODO: This is where we should grab actual eventsources or axes by reference from InputManager.Instance
  // The AbilityManager should have no idea where these things are coming from
  void Start() {
    var AbilityManager = GetComponent<AbilityManager>();

    ButtonEventTagMaps.ForEach(btm => {
      var method = btm.Entry.Ability.GetType().GetMethod(btm.Entry.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      AbilityManager.RegisterTrigger(InputManager.Instance.ButtonEvent(btm.ButtonCode, btm.ButtonPressType), btm.Entry.Ability, method);
    });
    //AxisEventTagMaps.ForEach(atm => {
    //  var method = btm.Ability.GetType().GetMethod(btm.Entry.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    //  //AbilityManager.RegisterTrigger(InputManager.Instance.Axis(atm.AxisCode), btm.Ability, method);
    //}); 
    //AbilityManager.RegisterTag(atm.EventTag, atm.AxisCode));
  }
}