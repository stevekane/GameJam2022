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
  public MethodInfo Method;
  Action Handler;
  Ability Ability;
  Fiber? Fiber;

  public void Init(Ability ability) {
    Ability = ability;
    EventSource = ability.AbilityManager.GetEvent(EventTag);
    EventSource.Action += EventAction;
    var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    Method = ability.GetType().GetMethod(MethodReference.MethodName, bindingFlags);
    Handler = () => ability.StartRoutine((Fiber = new Fiber((IEnumerator)Method.Invoke(ability, null))).Value);
  }

  public void Destroy() {
    EventSource.Action -= EventAction;
  }

  void EventAction() {
    // TODO: This should be per-trigger not per-ability
    var alreadyRunning = Fiber.HasValue && Ability.IsRoutineRunning(Fiber.Value);
    var notBlocked = Ability.AbilityManager.Running.TrueForAll(a => (a.Blocks & Ability.Tags) == 0);
    if (!alreadyRunning && notBlocked) {
      Handler?.Invoke();
      Ability.AbilityManager.ToAdd.Add(Ability);
      foreach (var activeAbility in Ability.AbilityManager.Running) {
        if ((activeAbility.Cancels & Ability.Tags) != 0) {
          activeAbility.Stop();
        }
      }
    }
  }
}

[Serializable]
public class TaskMethodReference {
  public string MethodName;
}

[Serializable]
[CustomPropertyDrawer(typeof(TaskMethodReference))]
public class TaskMethodReferencePropertyDrawer : PropertyDrawer {
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
  public bool IsRoutineRunning(Fiber f) => Bundle.IsRoutineRunning(f);
  public void StartRoutine(Fiber routine) => Bundle.StartRoutine(routine);
  public void StopRoutine(Fiber routine) => Bundle.StopRoutine(routine);
  public virtual void Stop() => Bundle.StopAll();
  void FixedUpdate() => Bundle.Run();
}