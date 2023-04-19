using System;
using System.Collections.Generic;
using UnityEngine;

/*
InputToTriggerMap subscribes to ButtonCode and PressType combinations.

When the associated event fires, all corresponding presses are invoked
and if they are successful the associated input is consumed from the
input buffer.

We would like to incrementally add a new feature:

  You can specify how the events that are subscribed to the button event
  should be handled.

The is an irrelevant detail for the typical case when actions are mutually exclusive.

However, sometimes, logically, actions may not be mutually-exclusive but they
are mapped to the same button. In such cases, you would like to be able to
specify how to determine what actions should actually fire.

The illustrating case that makes this most clear is the following:

  North
    interact and BLOCK
    spike and PASS
    heavy and PASS

spike and heavy are mutually exclusive.
interact may be possible when heavy is also possible.
we want to block heavy by placing interact higher in the priority list and flagging it as BLOCK
*/

[Serializable]
public struct ButtonEventActionMap {
  public bool Block;
  public FieldReference<SimpleAbility, AbilityAction> ActionReference;
  public ButtonCode ButtonCode;
  public ButtonPressType ButtonPressType;
}

[Serializable]
public class AbilityActionReference {
  public SimpleAbility Ability;
  public string ActionName;
  public AbilityAction Action {
    get {
      if (!Ability || ActionName.Length == 0) return null;
      return (AbilityAction)Ability.GetType().GetField(ActionName).GetValue(Ability);
    }
  }
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
              if (map.Block)
                break;
            }
          }
        });
      }
    }
  }
}