using System;
using System.Collections.Generic;
using UnityEngine;

public enum ButtonEventFlowBehavior {
  Pass,
  Block
}

[Serializable]
public class AbilityActionFieldReference : FieldReference<SimpleAbility, AbilityAction> {}

[Serializable]
public struct ButtonEventActionMap {
  public ButtonEventFlowBehavior FlowBehavior;
  public AbilityActionFieldReference ActionReference;
  public ButtonCode ButtonCode;
  public ButtonPressType ButtonPressType;
}

public class ActionController : MonoBehaviour {
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] InputManager InputManager;
  [SerializeField] List<ButtonEventActionMap> ButtonEventActions;

  void Start() {
    foreach (ButtonCode buttonCode in Enum.GetValues(typeof(ButtonCode))) {
      foreach (ButtonPressType buttonPressType in Enum.GetValues(typeof(ButtonPressType))) {
        InputManager.ButtonEvent(buttonCode, buttonPressType).Listen(delegate {
          foreach (var map in ButtonEventActions) {
            if (map.ButtonCode != buttonCode || map.ButtonPressType != buttonPressType)
              continue;
            var action = map.ActionReference.Value;
            if (SimpleAbilityManager.CanRun(action)) {
              SimpleAbilityManager.Run(action);
              InputManager.Consume(buttonCode, buttonPressType);
              if (map.FlowBehavior.Equals(ButtonEventFlowBehavior.Block))
                break;
            }
          }
        });
      }
    }
  }
}