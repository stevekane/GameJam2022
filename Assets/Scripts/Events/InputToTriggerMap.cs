using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

[RequireComponent(typeof(AbilityManager))]
public class InputToTriggerMap : MonoBehaviour {
  [SerializeField] List<ButtonTriggerMap> ButtonMaps;
  [SerializeField] List<AxisTriggerMap> AxisMaps;

  void Start() {
    var AbilityManager = GetComponent<AbilityManager>();
    ButtonMaps.ForEach(b => {
      var methodInfo = b.Entry.Ability.GetType().GetMethod(b.Entry.MethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      if (methodInfo.ReturnType == typeof(IEnumerator)) {
        var method = (AbilityMethod)Delegate.CreateDelegate(typeof(AbilityMethod), b.Entry.Ability, methodInfo);
        AbilityManager.RegisterEvent(method, InputManager.Instance.ButtonEvent(b.ButtonCode, b.ButtonPressType));
      } else {
        var method = (AbilityMethodTask)Delegate.CreateDelegate(typeof(AbilityMethodTask), b.Entry.Ability, methodInfo);
        AbilityManager.RegisterEvent(method, InputManager.Instance.ButtonEvent(b.ButtonCode, b.ButtonPressType));
      }
    });
    AxisMaps.ForEach(a => {
      AbilityManager.RegisterAxis(a.AxisTag, InputManager.Instance.Axis(a.AxisCode));
    });
  }
}