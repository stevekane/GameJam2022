using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventFlowBehavior {
  Pass,
  Block
}

public enum AxisProcessor {
  XY,
  PersonalCameraXZ
}

[Serializable]
public class AbilityActionFieldReference : FieldReference<SimpleAbility, AbilityAction> {}
[Serializable]
public class AbilityActionVector3FieldReference : FieldReference<SimpleAbility, AbilityAction<Vector3>> {}

[Serializable]
public struct ButtonEventActionMap {
  public ButtonCode ButtonCode;
  public ButtonPressType ButtonPressType;
  public AbilityActionFieldReference ActionReference;
  public EventFlowBehavior FlowBehavior;
}

[Serializable]
public struct AxisEventActionMap {
  public AxisCode AxisCode;
  public AxisProcessor AxisProcessor;
  public AbilityActionVector3FieldReference ActionReference;
  public EventFlowBehavior FlowBehavior;
}

public class ActionController : MonoBehaviour {
  [SerializeField] SimpleAbilityManager SimpleAbilityManager;
  [SerializeField] InputManager InputManager;
  [SerializeField] PersonalCamera PersonalCamera;
  [SerializeField] List<ButtonEventActionMap> ButtonEventActions;
  [SerializeField] List<AxisEventActionMap> AxisEventActions;

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
              if (map.FlowBehavior.Equals(EventFlowBehavior.Block))
                break;
            }
          }
        });
      }
    }

    foreach (AxisCode axisCode in Enum.GetValues(typeof(AxisCode))) {
      InputManager.AxisEvent(axisCode).Listen(code => {
        foreach (var map in AxisEventActions) {
          if (map.AxisCode != axisCode)
            continue;
          var action = map.ActionReference.Value;
          if (SimpleAbilityManager.CanRun(action)) {
            var axis = map.AxisProcessor switch {
              AxisProcessor.PersonalCameraXZ => PersonalCamera.CameraScreenToWorldXZ(code.XY),
              _ => (Vector3)code.XY
            };
            SimpleAbilityManager.Run(action, axis);
            if (map.FlowBehavior.Equals(EventFlowBehavior.Block))
              break;
          }
        }
      });
    }
  }
}