using System;
using System.Collections.Generic;
using UnityEngine;

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
    var InputManager = GetComponent<InputManager>();
    void ConnectButtonToAction(AbilityMethod method, ButtonCode buttonCode, ButtonPressType pressType) {
      InputManager.ButtonEvent(buttonCode, pressType).Listen(() => {
        if (AbilityManager.TryInvoke(method))
          InputManager.Consume(buttonCode, pressType);
      });
    }
    ButtonMaps.ForEach(b => {
      ConnectButtonToAction(b.Ability.MainAction, b.ButtonCode, ButtonPressType.JustDown);
      ConnectButtonToAction(b.Ability.MainRelease, b.ButtonCode, ButtonPressType.JustUp);
    });
    AxisMaps.ForEach(a => {
      AbilityManager.RegisterAxis(a.AxisTag, InputManager.Axis(a.AxisCode));
    });
  }
}