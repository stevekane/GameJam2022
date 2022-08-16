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
  public AxisTag AxisTag;
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
  [SerializeField] List<ButtonTriggerMap> ButtonMaps;
  [SerializeField] List<AxisTriggerMap> AxisMaps;

  void Start() {
    var AbilityManager = GetComponent<AbilityManager>();
    ButtonMaps.ForEach(b => {
      var methodInfo = b.Entry.Ability.GetType().GetMethod(b.Entry.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      var method = (AbilityMethod)Delegate.CreateDelegate(typeof(AbilityMethod), b.Entry.Ability, methodInfo);
      AbilityManager.RegisterEvent(method, InputManager.Instance.ButtonEvent(b.ButtonCode, b.ButtonPressType, () => AbilityManager.GetEvent(method)));
    });
    AxisMaps.ForEach(a => {
      AbilityManager.RegisterAxis(a.AxisTag, InputManager.Instance.Axis(a.AxisCode));
    });
  }
}