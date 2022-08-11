using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

[Serializable]
public class AbilityTrigger {
  public TaskMethodReference MethodReference;
  public EventTag EventTag;
  public EventSource EventSource;
  Action Handler;

  public void Init(AbilityManager user, Ability ability) {
    EventSource = user.GetEvent(EventTag);
    EventSource.Action += EventAction;
    var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    var method = ability.GetType().GetMethod(MethodReference.MethodName, bindingFlags);
    Handler = () => ability.StartRoutine(new Fiber((IEnumerator)method.Invoke(ability, null)));
  }

  public void Destroy() {
    EventSource.Action -= EventAction;
  }

  void EventAction() {
    Handler?.Invoke();
  }
}

[Serializable]
public class TaskMethodReference {
  public string MethodName;
}

[Serializable]
[CustomPropertyDrawer(typeof(TaskMethodReference))]
public class AbilityEventHandlerPropertyDrawer : PropertyDrawer {
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
      }
      if (menu.GetItemCount() == 0) {
        menu.AddDisabledItem(new GUIContent("No valid methods found."), false);
      }
      menu.ShowAsContext();
    }
  }
}

[Serializable]

public abstract class Ability : MonoBehaviour {
  protected Bundle Bundle = new();
  public AbilityManager AbilityManager { get; set; }
  public List<AbilityTrigger> Triggers;
  public AbilityTag Tags;
  public AbilityTag Cancels;
  public AbilityTag Blocks;
  public bool IsRunning { get => Bundle.IsRunning; }
  public bool IsFiberRunning(Fiber f) => Bundle.IsFiberRunning(f);
  public virtual void Activate() => Bundle.StartRoutine(new Fiber(MakeRoutine()));
  public virtual void Stop() => Bundle.StopAll();
  protected virtual IEnumerator MakeRoutine() { yield return null; } // TODO remove
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Bundle.StopRoutine(routine);
  void FixedUpdate() => Bundle.Run();
}
