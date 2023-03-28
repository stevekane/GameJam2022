using UnityEngine;

namespace ActionsAndAI {
  [DefaultExecutionOrder(ScriptExecutionGroups.Early)]
  public class PlayerController : MonoBehaviour {
    [SerializeField] ActionManager ActionManager;
    [SerializeField] InputManager InputManager;

    void FixedUpdate() {
      InputManager.ClearAllAxisEvents();
      InputManager.ClearAllButtonEvents();
      foreach (var action in ActionManager.Actions) {
        if (action.CanStart()) {
          InputManager.ButtonEvent(action.ButtonCode, action.ButtonPressType).Set(delegate {
            InputManager.Consume(action.ButtonCode, action.ButtonPressType);
            action.OnStart();
          });
        }
      }
      foreach (var axisAction in ActionManager.AxisActions) {
        if (axisAction.CanStart()) {
          InputManager.AxisEvent(axisAction.AxisCode).Set(axisAction.OnStart);
        }
      }
    }
  }
}