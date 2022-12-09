using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public delegate IEnumerator AbilityMethod();
public delegate Task AbilityMethodTask(TaskScope scope);

[Serializable]
public class AbilityMethodReference {
  public Ability Ability;
  public string MethodName;

  public AbilityMethod GetMethod() {
    var methodInfo = Ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethod)Delegate.CreateDelegate(typeof(AbilityMethod), Ability, methodInfo);
  }
  public AbilityMethodTask GetMethodTask() {
    var methodInfo = Ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethodTask)Delegate.CreateDelegate(typeof(AbilityMethodTask), Ability, methodInfo);
  }
}

// Same as above but used if the Ability is the owning class.
[Serializable]
public class AbilityMethodReferenceSelf {
  public string MethodName;

  public AbilityMethod GetMethod(Ability ability) {
    var methodInfo = ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethod)Delegate.CreateDelegate(typeof(AbilityMethod), ability, methodInfo);
  }
  public AbilityMethodTask GetMethodTask(Ability ability) {
    var methodInfo = ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (AbilityMethodTask)Delegate.CreateDelegate(typeof(AbilityMethodTask), ability, methodInfo);
  }
  public bool IsTask(Ability ability) {
    var methodInfo = ability.GetType().GetMethod(MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    return (methodInfo.ReturnType == typeof(Task));
  }
}

[Serializable]
[CustomPropertyDrawer(typeof(AbilityMethodReference))]
public class AbilityMethodReferencePropertyDrawer : PropertyDrawer {
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
        if (method.GetParameters().Length == 1 && method.ReturnType == typeof(Task)) {
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

[Serializable]
[CustomPropertyDrawer(typeof(AbilityMethodReferenceSelf))]
public class AbilityMethodReferenceSelfPropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    var methodName = property.FindPropertyRelative("MethodName");
    var dropDownText = methodName.stringValue == "" ? "Select method" : methodName.stringValue;
    if (EditorGUI.DropdownButton(position, new GUIContent(dropDownText), FocusType.Keyboard)) {
      var menu = new GenericMenu();
      var target = property.serializedObject.targetObject;
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
        if (method.GetParameters().Length == 1 && method.ReturnType == typeof(Task)) {
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
