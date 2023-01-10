using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;

[Serializable]
public class ButtonTriggerMap {
  public ButtonCode ButtonCode;
  public Ability Ability;
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
      AbilityManager.RegisterEvent(b.Ability.MainAction, InputManager.Instance.ButtonEvent(b.ButtonCode, ButtonPressType.JustDown));
      AbilityManager.RegisterEvent(b.Ability.MainRelease, InputManager.Instance.ButtonEvent(b.ButtonCode, ButtonPressType.JustUp));
    });
    AxisMaps.ForEach(a => {
      AbilityManager.RegisterAxis(a.AxisTag, InputManager.Instance.Axis(a.AxisCode));
    });
  }
}